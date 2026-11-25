using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OhtaTool.Plugin.Core;
using TeamsTool.Core;
using TeamsTool.Views;

namespace TeamsTool.Services
{
    /// <summary>
    /// 🧪 Thử nhanh chạy ẩn cho tab "Tạo dữ liệu DXF từ ảnh" trên CAD (05_AI): cùng luồng với ThuNhanhWindow
    /// (AiDon.TaoThuNhanh → DocThuNhanh → VeThuNhanh, cùng thư mục lần thử). Giao thức stdout UTF-8, mỗi dòng:
    ///   "@ chữ"   tiến độ · "#TM thư mục" lần thử vừa tạo · "#KQ json" kết quả · "#LOI chữ" lỗi (mã thoát 1).
    /// Lệnh: --thu-nhanh-moi TEP · --thu-nhanh-doc-lai TM · --thu-nhanh-ve TM · --thu-nhanh-ds.
    /// Thêm "--ghi-chu-tep TEP" (moi / doc-lai): tệp UTF-8 chứa thông tin bổ sung người dùng gõ — gửi kèm ảnh, thắng hình (25/11).
    /// </summary>
    public static class ThuNhanhCli
    {
        private static StreamWriter _ra;
        private static string _ghiChu; // null = giữ ghi chú đã lưu

        public static int Chay(string[] args)
        {
            _ra = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
            try
            {
                string lenh = args[0], ts = args.Length > 1 ? args[1] : null;
                int iGc = Array.IndexOf(args, "--ghi-chu-tep");
                _ghiChu = iGc > 0 && iGc + 1 < args.Length ? File.ReadAllText(args[iGc + 1], Encoding.UTF8) : null;
                switch (lenh)
                {
                    case "--thu-nhanh-ds":
                        In("#KQ " + JsonLite.Serialize(AiDon.CacLanThu().Select(tm => (object)MoTa(tm, null)).ToList()).Replace("\n", " "));
                        return 0;
                    case "--thu-nhanh-moi":
                        return KetQua(Moi(CanTs(ts)).GetAwaiter().GetResult());
                    case "--thu-nhanh-doc-lai":
                        return KetQua(DocLaiVaVe(CanTs(ts)).GetAwaiter().GetResult());
                    case "--thu-nhanh-ve":
                        return KetQua(Ve(CanTs(ts)).GetAwaiter().GetResult());
                    case "--thu-nhanh-viet-mau":
                        return KetQua(VietMau(CanTs(ts)).GetAwaiter().GetResult());
                    default:
                        In("#LOI Lệnh không hiểu: " + lenh);
                        return 1;
                }
            }
            catch (Exception ex)
            {
                In("#LOI " + (ex is AggregateException ag ? ag.GetBaseException() : ex).Message.Replace("\r", "").Replace("\n", " — "));
                return 1;
            }
        }

        private static string CanTs(string ts) => string.IsNullOrWhiteSpace(ts) ? throw new ArgumentException("Thiếu tham số (tệp / thư mục lần thử).") : ts;

        private static void In(string dong) => _ra.WriteLine(dong);

        private static void Bao(string s) => In("@ " + (s ?? "").Replace("\r", "").Replace("\n", " — "));

        private static int KetQua(JsonObject kq)
        {
            In("#KQ " + JsonLite.Serialize(kq).Replace("\n", " "));
            return 0;
        }

        private static async Task<JsonObject> Moi(string tep)
        {
            Bao("Đang chuẩn bị " + Path.GetFileName(tep) + "…");
            string tm = await AiDon.TaoThuNhanh(ThuNhanhWindow.VePng(tep), CancellationToken.None);
            In("#TM " + tm);
            AiDon.DatGhiChuThuNhanh(tm, _ghiChu);
            await AiDon.DocThuNhanh(tm, Bao, CancellationToken.None);
            Bao("AI đã đọc hình — đang vẽ…");
            return await Ve(tm);
        }

        private static async Task<JsonObject> DocLaiVaVe(string tm)
        {
            AiDon.DatGhiChuThuNhanh(tm, _ghiChu);
            await AiDon.DocLaiThuNhanh(tm, Bao, CancellationToken.None);
            return await Ve(tm);
        }

        private static async Task<JsonObject> Ve(string tm)
        {
            var (_, _, ds, ve) = await AiDon.VeThuNhanh(tm, Bao, CancellationToken.None);
            AiDon.KetQuaVe kv = ve.FirstOrDefault(v => v.Bango == Bango);
            JsonObject kq = MoTa(tm, kv);
            if (ds.Count == 0) kq.Set("loi", "Không có tấm nào để vẽ — ✎ Sửa thông số.");
            else if (kv == null && kq.Get("loi") == null)
                kq.Set("loi", ds[0].Tk.LamTay ? "Hình làm tay (lam_tay) — không có mẫu vẽ."
                    : "Chưa có kích thước trải phẳng (kich_thuoc / td_x, td_y trống) — ✎ Sửa thông số rồi 🔁 Vẽ lại.");
            return kq;
        }

        /// <summary>✨ AI viết mẫu Python cho hình chưa có mẫu (lưu là mẫu thử của lần thử này, như nút trong ThuNhanhWindow) rồi vẽ lại.</summary>
        private static async Task<JsonObject> VietMau(string tm)
        {
            var (ct, kq, ds, _) = await AiDon.VeThuNhanh(tm, Bao, CancellationToken.None);
            if (ds.Count == 0) throw new InvalidOperationException("Không có tấm nào để viết mẫu — ✎ Sửa thông số.");
            Bao("AI đang viết mẫu vẽ cho hình này…");
            PhongTrienKhai.MauMoi m = await AiDon.DeXuatMau(ct, ds[0].Tam, ds[0].Tk, kq, tm, AiDon.TenThuNhanh, Bao, CancellationToken.None);
            if (m.Loi != null) throw new InvalidOperationException("AI chưa viết được mẫu: " + m.Loi);
            Bao("AI đã viết mẫu '" + m.Ten + "' — đang vẽ…");
            return await Ve(tm);
        }

        private static string Bango => PhongTrienKhai.Bango(AiDon.TenThuNhanh, 1);

        /// <summary>Biên dạng của tấm chưa có mẫu vẽ: chua_co, trống, hoặc tên không có trong thư viện chung lẫn mẫu thử của lần thử.</summary>
        private static bool CanMau(string tm, TamAi t)
        {
            if (t == null || !t.DaPhongTo) return false;
            string bd = t.BienDang ?? "";
            return bd.Length == 0 || bd == PhongTrienKhai.ChuaCoMau
                || (AiDon.TepMauChung(bd) == null && !File.Exists(Path.Combine(AiDon.ThuVienThu(tm), bd + ".py")));
        }

        /// <summary>Một lần thử dạng JSON cho CAD: thư mục, giờ, mô tả, ảnh gửi vào, ảnh hình vẽ, DXF, lỗi / cảnh báo.</summary>
        public static JsonObject MoTa(string tm, AiDon.KetQuaVe kv)
        {
            TamAi t = null;
            try { t = AiDon.DocChiTiet(tm)?.Tam.FirstOrDefault(); } catch (Exception) { } // tệp hỏng: vẫn liệt kê để xoá được
            string ten = Path.GetFileName(tm);
            string gio = DateTime.TryParseExact(ten, "yyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime luc)
                ? luc.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture) : ten;
            string anh = AiDon.TepHoSo(tm, Bango, ".png");
            if (!File.Exists(anh)) anh = Path.Combine(AiDon.ThuMucHoSo(tm), Bango + ".png"); // lần thử trước 18/11
            string dxf = kv?.Tep;
            if (dxf == null && Directory.Exists(AiDon.ThuMucBanVe(tm)))
                dxf = Directory.GetFiles(AiDon.ThuMucBanVe(tm), Bango + "*.dxf").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            string xem = kv?.Anh ?? (dxf != null ? Path.ChangeExtension(dxf, ".png") : null);

            var o = new JsonObject();
            o.Set("thu_muc", tm);
            o.Set("gio", gio);
            o.Set("mo_ta", t == null ? "(chưa đọc)" : !t.DaPhongTo ? "AI chưa đọc được"
                : (t.BienDang.Length > 0 ? t.BienDang : "chưa có mẫu") + (t.KichThuoc.Length > 0 ? " · " + t.KichThuoc : ""));
            o.Set("chi_tiet", t == null ? "" : (t.M + " t" + t.T + (t.MoTa.Length > 0 ? " · " + t.MoTa : "")).Trim());
            o.Set("anh", File.Exists(anh) ? anh : null);
            o.Set("xem", xem != null && File.Exists(xem) ? xem : null);
            o.Set("dxf", dxf != null && File.Exists(dxf) ? dxf : null);
            o.Set("chi_tiet_json", AiDon.FileChiTiet(tm));
            o.Set("ghi_chu_nguoi", t?.GhiChuNguoi ?? "");
            o.Set("mau_thuan", t == null ? new List<object>() : PhongPhongTo.MauThuan(t).Cast<object>().ToList());
            o.Set("can_xac_nhan", t == null ? new List<object>() : PhongPhongTo.CanXacNhan(t).Cast<object>().ToList()); // phần vẽ vàng (25/11)
            bool canMau = CanMau(tm, t);
            o.Set("can_mau", canMau);
            if (kv?.Loi != null) o.Set("loi", kv.Loi);
            // danh sách (chưa vẽ lần này) vẫn phải nói vì sao không có hình
            else if (o.Get("dxf") == null && canMau) o.Set("loi", "Chưa có mẫu biên dạng cho hình này — bấm ✨ AI viết mẫu (hoặc ✎ Sửa thông số: bien_dang).");
            var cb = new List<object>();
            foreach (string s in kv?.ThieuDim ?? new List<string>()) cb.Add("Thiếu dim khách: " + s);
            foreach (string s in kv?.CanhBao ?? new List<string>()) cb.Add(s);
            o.Set("canh_bao", cb);
            return o;
        }
    }
}
