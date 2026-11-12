using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>
    /// Hồ sơ tấm (12/11): mỗi tấm mới một tệp ai\ho_so\&lt;đơn&gt;-n.md (+ ảnh &lt;đơn&gt;-n.png cắt từ bản vẽ) gom thông số, mô tả chi tiết,
    /// tiết diện, câu hỏi + câu trả lời — sinh từ chi_tiet.json / kiem_tra.json (bản gốc là JSON), phòng 3 gửi cho AI.
    /// Ghi chú chung cả đơn: ho_so\_don.md.
    /// </summary>
    public static class PhongHoSo
    {
        public const string ThuMuc = "ho_so", TepDon = "_don.md";

        /// <summary>Tên tấm &lt;đơn&gt;-n: đánh liên tục các tấm Mới theo thứ tự bảng (bản vẽ rồi tấm) — trùng bango phòng 3, tên DXF, CSV.</summary>
        public static List<(TamAi Tam, string Ten)> DanhSo(ChiTiet ct, string title) =>
            ct.Tam.Where(t => t.Moi).Select((t, i) => (t, PhongTrienKhai.Bango(title, i + 1))).ToList();

        /// <summary>
        /// Vấn đề thuộc tấm: luật (mã chứa khoá tấm); AI ghi đúng số BV / tên tấm; hoặc vấn đề AI của tệp chỉ có một tấm.
        /// Không thuộc tấm nào → mục "Chung cả đơn".
        /// </summary>
        public static bool CuaTam(VanDe v, TamAi t, ChiTiet ct)
        {
            if (v.Ma.StartsWith("luat|", StringComparison.Ordinal)) return v.Ma.StartsWith("luat|" + t.Khoa + "|", StringComparison.Ordinal);
            string tam = (v.Tam ?? "").Trim();
            bool Bang(string a) => a.Trim().Length > 0 && string.Equals(a.Trim(), tam, StringComparison.OrdinalIgnoreCase);
            if (tam.Length > 0 && (Bang(t.SoBanVe) || Bang(t.Ten) || Bang(PhongKiemTra.TenTam(t)))) return true;
            string tep = "ai|" + (t.Tep ?? "").ToLowerInvariant() + "|";
            return tam.Length == 0 && v.Ma.StartsWith(tep, StringComparison.Ordinal) && ct.Tam.Count(x => x.Moi && x.Tep == t.Tep) == 1;
        }

        /// <summary>Hồ sơ md của một tấm. tk = kết quả trải phẳng (null khi chưa xác nhận).</summary>
        public static string Md(string ten, TamAi t, IEnumerable<VanDe> vanDe, TamTrienKhai tk, string tepAnh)
        {
            var sb = new StringBuilder();
            sb.Append("# ").Append(ten).Append("\n\n");
            sb.Append("## Tấm\n");
            Dong(sb, "Tên tấm", ten); Dong(sb, "Tệp bản vẽ", t.Tep); Dong(sb, "Trang", t.Trang);
            Dong(sb, "Số bản vẽ khách", t.SoBanVe); Dong(sb, "Tên trên bản vẽ", t.Ten);
            if (!string.IsNullOrEmpty(tepAnh)) sb.Append("\n![").Append(ten).Append("](").Append(tepAnh).Append(")\n");
            sb.Append("\n## Thông số\n");
            Dong(sb, "M (vật liệu)", t.M); Dong(sb, "Z", t.Z); Dong(sb, "T (độ dày)", t.T); Dong(sb, "Q (số lượng)", t.Q);
            Dong(sb, "Mặt", t.Mat); Dong(sb, "Kích thước bao", t.KichThuoc); Dong(sb, "Dung sai", t.DungSai);
            if (t.KhongChac.Count > 0) Dong(sb, "AI chưa chắc", string.Join(", ", t.KhongChac.OrderBy(x => x)));
            if (t.DaSua.Count > 0) Dong(sb, "Người đã sửa", string.Join(", ", t.DaSua.OrderBy(x => x)) + (t.LyDoSua.Length > 0 ? " — " + t.LyDoSua : ""));
            sb.Append("\n## Mô tả chi tiết\n").Append(t.MoTa.Trim().Length > 0 ? t.MoTa.Trim() : "(chưa có — đọc lại bản vẽ)").Append('\n');
            sb.Append("\n## Tiết diện\n");
            Dong(sb, "Kiểu", t.KieuTd); Dong(sb, "X", t.TdX); Dong(sb, "Y", t.TdY); Dong(sb, "Ghi chú", t.GhiChuTd);
            Dong(sb, "Mẫu biên dạng", t.BienDang + (t.MoTaBienDang.Length > 0 ? " — " + t.MoTaBienDang : ""));
            Dong(sb, "Thông số mẫu", t.ThamSo);
            Dong(sb, "Góc vát / bo", t.Goc);
            Dong(sb, "Khoét trong", t.Khoet);
            sb.Append("(cú pháp: ").Append(PhongTrienKhai.HuongDanTd).Append(")\n");
            Ds(sb, "Bẻ / nhấn / cuộn", t.CanhJson); Ds(sb, "Lỗ", t.LoJson); Ds(sb, "ケガキ (toạ độ trên hình trải phẳng, gốc trái dưới)", t.KeGakiJson);
            Ds(sb, "Kích thước khách (phải có đủ trên bản vẽ OHTA)", t.DimKhachJson);
            sb.Append("\n## Ghi chú đặc biệt\n");
            if (t.GhiChuJp.Trim().Length == 0 && t.GhiChuVi.Trim().Length == 0) sb.Append("(không có)\n");
            if (t.GhiChuJp.Trim().Length > 0) sb.Append("- JP: ").Append(t.GhiChuJp.Trim()).Append('\n');
            if (t.GhiChuVi.Trim().Length > 0) sb.Append("- VI: ").Append(t.GhiChuVi.Trim()).Append('\n');
            sb.Append("\n## Câu hỏi và trả lời\n");
            var ds = (vanDe ?? Enumerable.Empty<VanDe>()).ToList();
            if (ds.Count == 0) sb.Append("(không có)\n");
            foreach (VanDe v in ds) CauHoi(sb, v);
            if (tk != null)
            {
                sb.Append("\n## Trải phẳng (chương trình tính theo bảng TV)\n");
                Dong(sb, "X × Y", tk.LamTay ? "LÀM TAY" : (tk.XPhang.HasValue ? PhongTrienKhai.F(tk.XPhang.Value) : "?") + " × " + (tk.YPhang.HasValue ? PhongTrienKhai.F(tk.YPhang.Value) : "?"));
                Dong(sb, "V", tk.VChon); Dong(sb, "Ký hiệu 曲げ", tk.KyHieu);
                Dong(sb, "Đường bẻ X (mm từ mép trái)", string.Join(", ", tk.BeX.Select(PhongTrienKhai.F)));
                Dong(sb, "Đường bẻ Y (mm từ mép dưới)", string.Join(", ", tk.BeY.Select(PhongTrienKhai.F)));
                foreach (string c in tk.ChiTiet) sb.Append("- ").Append(c).Append('\n');
                foreach (string c in tk.CanhBao) sb.Append("- ⚠ ").Append(c).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Hồ sơ chung cả đơn: ghi chú chung theo tệp + vấn đề không thuộc tấm nào.</summary>
        public static string MdDon(string title, string congTy, ChiTiet ct, IEnumerable<VanDe> chung)
        {
            var sb = new StringBuilder("# " + title + " — chung cả đơn\n\n");
            Dong(sb, "Công ty đặc biệt", string.IsNullOrWhiteSpace(congTy) ? "(không)" : congTy);
            Dong(sb, "Các tấm", string.Join(", ", DanhSo(ct, title).Select(x => x.Ten)));
            sb.Append("\n## Ghi chú chung của khách\n");
            if (ct.GhiChuDon.All(kv => kv.Value.Trim().Length == 0)) sb.Append("(không có)\n");
            foreach (var kv in ct.GhiChuDon.Where(kv => kv.Value.Trim().Length > 0)) sb.Append("### ").Append(kv.Key).Append('\n').Append(kv.Value.Trim()).Append('\n');
            sb.Append("\n## Câu hỏi chung và trả lời\n");
            var ds = (chung ?? Enumerable.Empty<VanDe>()).ToList();
            if (ds.Count == 0) sb.Append("(không có)\n");
            foreach (VanDe v in ds) CauHoi(sb, v);
            return sb.ToString();
        }

        private static void CauHoi(StringBuilder sb, VanDe v)
        {
            string muc = v.Muc == VanDe.Do ? "đỏ" : v.Muc == VanDe.Vang ? "vàng" : "ghi chú";
            string nhom = v.Nhom == VanDe.Khach ? "hỏi khách" : v.Nhom == VanDe.NoiBo ? "hỏi nội bộ" : "tự xử lý";
            sb.Append("- [").Append(muc).Append(" · ").Append(nhom).Append(v.MucQc.Length > 0 ? " · QC " + v.MucQc : "").Append("] ").Append(v.NoiDung).Append('\n');
            sb.Append("  - Trả lời: ").Append(v.TraLoi.Trim().Length > 0 ? v.TraLoi.Trim() : "(chưa)").Append('\n');
            if (v.TrangThai == VanDe.BoQua) sb.Append("  - Bỏ qua: ").Append(v.LyDo).Append('\n');
            else if (v.TrangThai == VanDe.Xong) sb.Append("  - Đã xử lý\n");
        }

        private static void Dong(StringBuilder sb, string nhan, string giaTri)
        {
            if (!string.IsNullOrWhiteSpace(giaTri)) sb.Append("- ").Append(nhan).Append(": ").Append(giaTri.Trim()).Append('\n');
        }

        /// <summary>Mảng JSON đối tượng → mỗi phần tử một dòng "khoá=giá trị".</summary>
        private static void Ds(StringBuilder sb, string nhan, string json)
        {
            List<object> l;
            try { l = JsonLite.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json) as List<object> ?? new List<object>(); } catch (Exception) { l = new List<object>(); }
            if (l.Count == 0) return;
            sb.Append("- ").Append(nhan).Append(":\n");
            foreach (object o in l)
                sb.Append("  - ").Append(o is JsonObject j ? string.Join(", ", j.Items.Select(kv => kv.Key + "=" + Chu(kv.Value))) : Chu(o)).Append('\n');
        }

        private static string Chu(object v) => v is string s ? s : v is JsonNumber n ? n.Raw : v is bool b ? (b ? "có" : "không") : v is List<object> l ? "[" + string.Join(", ", l.Select(Chu)) + "]" : v == null ? "" : JsonLite.Serialize(v);
    }
}
