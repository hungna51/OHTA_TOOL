using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>Loại một dòng F-name, theo hai ký tự đầu "X=".</summary>
    public enum LoaiDongFname { Khac, N, M, T, Q, V, Z }

    /// <summary>Một dòng của bảng, chỉ phần cần để kiểm tra trước khi lưu.</summary>
    public sealed class DongBang
    {
        /// <summary>Bango LÚC QUÉT — dùng để gọi tên tấm trong thông báo (bảng không có STT).</summary>
        public string GocBango { get; set; }
        public string Bango { get; set; }
        public string T { get; set; }
        public string Q { get; set; }
        public string M { get; set; }
        public string GC { get; set; }
        public bool LaCoDon { get; set; }
        public bool DaSua { get; set; }
    }

    /// <summary>
    /// Luật đọc / ghi của bảng số liệu (palette DST), viết theo đúng định dạng lệnh GRO
    /// ghi ra bản vẽ:
    ///   bango   TEXT layer "0" màu đỏ, chuỗi số nguyên
    ///   mã khắc MTEXT layer "3-KEGAKI": &lt;mã&gt;-&lt;bango&gt;
    ///   F-name  layer "2-F-NAME", mỗi dòng một TEXT (GRO) hoặc một MTEXT nối bằng \P (FN cũ):
    ///           N=&lt;tên&gt;-&lt;bango&gt;  M=  T=  Q=  [V= chỉ khi bẻ]  Z=
    /// Không phụ thuộc ZWCAD.
    /// </summary>
    public static class BangSoLieu
    {
        public static LoaiDongFname PhanLoai(string dong)
        {
            if (dong == null) return LoaiDongFname.Khac;
            string s = dong.Trim();
            if (s.Length < 2 || s[1] != '=') return LoaiDongFname.Khac;
            switch (char.ToUpperInvariant(s[0]))
            {
                case 'N': return LoaiDongFname.N;
                case 'M': return LoaiDongFname.M;
                case 'T': return LoaiDongFname.T;
                case 'Q': return LoaiDongFname.Q;
                case 'V': return LoaiDongFname.V;
                case 'Z': return LoaiDongFname.Z;
                default: return LoaiDongFname.Khac;
            }
        }

        /// <summary>Phần sau "X=" (đã Trim), hoặc "" nếu không phải dòng F-name.</summary>
        public static string GiaTri(string dong)
        {
            if (PhanLoai(dong) == LoaiDongFname.Khac) return string.Empty;
            return dong.Trim().Substring(2).Trim();
        }

        /// <summary>F-name dạng MTEXT của lệnh FN cũ: các dòng nối bằng mã "\P".</summary>
        public static string[] TachDongMText(string contents)
        {
            return (contents ?? string.Empty).Split(new[] { "\\P" }, StringSplitOptions.None);
        }

        public static string NoiDongMText(IEnumerable<string> dong) => string.Join("\\P", dong);

        /// <summary>
        /// Viết lại MỘT dòng F-name. Tham số null = giữ nguyên phần đó. Giữ nguyên tiền tố
        /// gốc ("T=" / "t=") và mọi dòng không thuộc N/T/Q/M (V=, Z=, dòng lạ).
        /// N= chỉ thay ĐOẠN SỐ CUỐI — tên bản vẽ có thể chứa số và dấu gạch.
        /// </summary>
        public static string VietLaiDong(string dong, string bango, string t, string q, string m)
        {
            if (dong == null) return null;
            LoaiDongFname loai = PhanLoai(dong);
            if (loai == LoaiDongFname.Khac) return dong;
            int i = dong.IndexOf('=');
            string tienTo = dong.Substring(0, i + 1);
            switch (loai)
            {
                case LoaiDongFname.N: return bango == null ? dong : GroText.ThayBangoCuoi(dong, bango);
                case LoaiDongFname.T: return t == null ? dong : tienTo + t;
                case LoaiDongFname.Q: return q == null ? dong : tienTo + q;
                case LoaiDongFname.M: return m == null ? dong : tienTo + m;
                default: return dong;
            }
        }

        /// <summary>
        /// Chuẩn hoá độ dày GIỐNG fn--fmt-t của lisp: số nguyên -> "6.0", còn lại một chữ
        /// số thập phân "4.5". Nhận cả dấu phẩy. Không phải số / không dương -> null.
        /// </summary>
        public static string ChuanHoaT(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return null;
            string s = t.Trim().Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) return null;
            if (v <= 0 || double.IsNaN(v) || double.IsInfinity(v)) return null;
            if (Math.Abs(v - Math.Truncate(v)) < 1e-6)
                return ((long)Math.Truncate(v)).ToString(CultureInfo.InvariantCulture) + ".0";
            return v.ToString("0.0", CultureInfo.InvariantCulture);
        }

        /// <summary>Số lượng: số nguyên dương, không số 0 đứng đầu.</summary>
        public static bool LaSoLuongHopLe(string q) => GroText.LaBangoHopLe(q);

        /// <summary>Các bango xuất hiện từ hai lần trở lên (bỏ qua rỗng và "?").</summary>
        public static HashSet<string> BangoTrung(IEnumerable<string> bangos)
        {
            return new HashSet<string>(
                bangos.Select(b => (b ?? string.Empty).Trim())
                      .Where(b => b.Length > 0 && b != "?")
                      .GroupBy(b => b, StringComparer.Ordinal)
                      .Where(g => g.Count() > 1)
                      .Select(g => g.Key),
                StringComparer.Ordinal);
        }

        /// <summary>
        /// Lỗi chặn việc lưu. Chỉ kiểm dòng ĐÃ SỬA về định dạng, nhưng kiểm TRÙNG trên mọi
        /// tấm: sửa một dòng có thể đụng số của một dòng chưa sửa.
        /// </summary>
        public static List<string> KiemTraTruocKhiLuu(IList<DongBang> dong)
        {
            var loi = new List<string>();
            var tam = dong.Where(d => !d.LaCoDon).ToList();
            foreach (DongBang d in tam.Where(d => d.DaSua))
            {
                string ten = "Tấm " + d.GocBango;
                if (!GroText.LaBangoHopLe(d.Bango))
                    loi.Add(ten + ": bango \"" + d.Bango + "\" không hợp lệ (số nguyên dương, không có số 0 đầu).");
                if (ChuanHoaT(d.T) == null)
                    loi.Add(ten + ": độ dày T \"" + d.T + "\" không phải số dương.");
                if (!LaSoLuongHopLe(d.Q))
                    loi.Add(ten + ": số lượng Q \"" + d.Q + "\" không phải số nguyên dương.");
                if (string.IsNullOrWhiteSpace(d.M))
                    loi.Add(ten + ": vật liệu M đang rỗng.");
                if (!CsvGro.LaGCHopLe(d.GC))
                    loi.Add(ten + ": GC \"" + d.GC + "\" không hợp lệ (" + CsvGro.GoiYGC + ").");
            }
            foreach (string b in BangoTrung(tam.Select(d => d.Bango)).OrderBy(b => int.TryParse(b, out int n) ? n : int.MaxValue))
            {
                var cung = tam.Where(d => (d.Bango ?? "").Trim() == b).ToList();
                loi.Add("Bango " + b + " bị TRÙNG ở " + cung.Count + " tấm (bango lúc quét: " +
                        string.Join(", ", cung.Select(d => d.GocBango)) + ").");
            }
            return loi;
        }

        /// <summary>
        /// Hai hộp [xmin, ymin, xmax, ymax] có chạm nhau không — phép "giao" của
        /// ssget "C" mà lisp dùng để lấy text của khung (tính cả chạm mép).
        /// </summary>
        /// <summary>Một dòng TEXT F-name để gom cột: điểm chèn, chiều cao, có phải dòng "N=" không.</summary>
        public struct DongChu
        {
            public double X, Y, Cao;
            public bool LaN;
        }

        /// <summary>
        /// Gom cột F-name bắt đầu từ dòng N= (chỉ số iN), xét TRONG HỆ TRỤC của dòng N= (góc xoay
        /// xoay, radian): cùng x' (dung sai), đi xuống theo -y', dừng ở dòng N= kế tiếp hoặc một
        /// khoảng hở lớn. GRO đặt F-name thẳng cột ở 0°, nhưng từ mục 117 cụm có thể bị xoay
        /// (vd 90°) khi đặt vào trong hình -> so theo X/Y gốc thì không gom được.
        /// </summary>
        public static List<int> GomCotFname(IList<DongChu> ds, int iN, double xoay)
        {
            var kq = new List<int>();
            if (ds == null || iN < 0 || iN >= ds.Count) return kq;
            DongChu n = ds[iN];
            double cao = n.Cao > 0 ? n.Cao : 2.5;
            double dungSaiX = Math.Max(0.5, 0.1 * cao);
            double cs = Math.Cos(xoay), sn = Math.Sin(xoay);
            var cot = new List<KeyValuePair<int, double>>();
            for (int i = 0; i < ds.Count; i++)
            {
                double dx = ds[i].X - n.X, dy = ds[i].Y - n.Y;
                double lx = dx * cs + dy * sn;    // quay (dx, dy) một góc -xoay
                double ly = -dx * sn + dy * cs;
                if (Math.Abs(lx) <= dungSaiX && ly <= 1e-6) cot.Add(new KeyValuePair<int, double>(i, ly));
            }
            cot.Sort((a, b) => b.Value.CompareTo(a.Value));
            kq.Add(iN);
            double yTruoc = 0.0;
            foreach (var c in cot)
            {
                if (c.Key == iN) continue;
                if (ds[c.Key].LaN) break;
                if (yTruoc - c.Value > 2.5 * Math.Max(cao, ds[c.Key].Cao)) break;
                kq.Add(c.Key);
                yTruoc = c.Value;
            }
            return kq;
        }

        /// <summary>
        /// Cờ mã khắc trong trường flags XData GRO_METADATA (mục 117) -> (cảnh báo, ghi chú thường).
        ///   MK_CHUA_DAT  : chưa đặt được mã khắc vào hình -> cảnh báo, người dùng move tay
        ///   MK_DE_KEGAKI : đã đặt nhưng phải bỏ qua ケガキ -> cảnh báo
        ///   MK_THU_NHO   : đã thu nhỏ 1/2 -> ghi chú thường
        /// </summary>
        public static void DocCoMaKhac(string co, out List<string> canhBao, out List<string> ghiChu)
        {
            canhBao = new List<string>();
            ghiChu = new List<string>();
            if (string.IsNullOrEmpty(co)) return;
            Func<string, bool> co_ = k => co.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0;
            if (co_("MK_CHUA_DAT")) canhBao.Add("[!] Mã khắc chưa vào hình — move thủ công");
            if (co_("MK_DE_KEGAKI")) canhBao.Add("Mã khắc đè ケガキ");
            if (co_("MK_THU_NHO")) ghiChu.Add("Mã khắc thu nhỏ 1/2");
            // Mục 190: chữ khắc (tấm 文字ケガキ, mục 156/189) đã đổi sang ケガキ nhưng cần kiểm tra
            if (co_("KC_NGOAI_HINH")) canhBao.Add("Chữ khắc nằm một phần ngoài hình — kiểm tra");
            if (co_("KC_CHAM_NET")) canhBao.Add("Chữ khắc chạm nét cắt 0-KAKOU — kiểm tra");
        }

        public static bool HopGiao(double[] a, double[] b)
        {
            return a[0] <= b[2] && b[0] <= a[2] && a[1] <= b[3] && b[1] <= a[3];
        }

        public static bool DiemTrongHop(double x, double y, double[] h)
        {
            return x >= h[0] && x <= h[2] && y >= h[1] && y <= h[3];
        }

        /// <summary>
        /// Mục 127: một chữ ghi chú chỉ thuộc MỘT tấm. Trong các hộp tấm chạm chữ, ưu tiên hộp
        /// chứa tâm chữ, rồi hộp có tâm gần tâm chữ nhất; không hộp nào chứa tâm thì lấy hộp
        /// gần tâm chữ nhất. Trả -1 nếu không hộp nào chạm chữ.
        /// </summary>
        public static int ChonTamSoHuu(double[] hopChu, IList<double[]> hopTams)
        {
            if (hopChu == null || hopTams == null) return -1;
            double cx = (hopChu[0] + hopChu[2]) / 2.0, cy = (hopChu[1] + hopChu[3]) / 2.0;
            int tot = -1;
            bool totChua = false;
            double totKc = double.MaxValue, totTam = double.MaxValue;
            for (int i = 0; i < hopTams.Count; i++)
            {
                double[] h = hopTams[i];
                if (h == null || !HopGiao(hopChu, h)) continue;
                bool chua = DiemTrongHop(cx, cy, h);
                double dx = Math.Max(0, Math.Max(h[0] - cx, cx - h[2]));
                double dy = Math.Max(0, Math.Max(h[1] - cy, cy - h[3]));
                double kc = Math.Sqrt(dx * dx + dy * dy);
                double tx = (h[0] + h[2]) / 2.0 - cx, ty = (h[1] + h[3]) / 2.0 - cy;
                double kcTam = Math.Sqrt(tx * tx + ty * ty);
                bool hon = tot < 0
                        || (chua && !totChua)
                        || (chua == totChua && (kc < totKc - 1e-9 || (Math.Abs(kc - totKc) <= 1e-9 && kcTam < totTam)));
                if (hon) { tot = i; totChua = chua; totKc = kc; totTam = kcTam; }
            }
            return tot;
        }
    }
}
