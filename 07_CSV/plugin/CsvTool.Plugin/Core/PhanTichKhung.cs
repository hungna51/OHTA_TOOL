using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CsvTool.Core
{
    /// <summary>TEXT/MTEXT thô nằm trọn trong khung. NoiDung = chuỗi thô (MText.Contents / DXF 3+1).</summary>
    public sealed class TextTho { public string Lop, NoiDung; public bool LaMtext; }

    /// <summary>DIMENSION thô: Co70 = mã DXF 70 (32/160 = dim thẳng, 33/161 = dim Aligned, 3/35/163 = dim phi), DoDai = mã 42, DX / DY = hướng 2 điểm gióng (mã 13 → 14).</summary>
    public sealed class DimTho { public string Lop; public int Co70; public double DoDai; public int Dimdec; public double DX, DY; }

    /// <summary>Một khung tấm cùng mọi TEXT/MTEXT/DIMENSION nằm trọn trong nó (theo thứ tự CAD trả về).</summary>
    public sealed class KhungTho
    {
        public double MinX, MinY, MaxX, MaxY;
        public long Handle;
        public List<TextTho> Text = new List<TextTho>();
        public List<DimTho> Dim = new List<DimTho>();
    }

    /// <summary>Một tấm đã đọc đủ dữ liệu để ghi CSV.</summary>
    public sealed class TamXuat
    {
        public string N, M, T, Q, X, Y;
        public ThongTinTam Tt;
        public double DiemX, DiemY;   // pointss của lisp: (MinX, MaxY) — dùng để sắp xếp
        public long HandleKhung;
    }

    /// <summary>Một lỗi (chặn xuất) hoặc cảnh báo của lượt quét, kèm các vị trí trên bản vẽ (WCS).</summary>
    public sealed class LoiQuet
    {
        public long Handle;
        public bool LaLoi;
        public string NoiDung;
        public List<(double MinX, double MinY, double MaxX, double MaxY)> ViTri = new List<(double, double, double, double)>();
    }

    /// <summary>Chép đúng hành vi CSV--analyze-one-frame (EXPORT, không chạy hình học).</summary>
    public static class PhanTichKhung
    {
        public const string LopFname = "2-F-NAME";

        private const string LoiKhongFname = "Không tìm thấy F-name.";
        private const string LoiAlignedXien = "2 dim Aligned không theo phương X và Y (cần 1 dim ngang, 1 dim đứng).";
        private const string LoiSoDim = "Sai số dim tổng (cần 2 dim, hoặc 1 dim phi).";

        public const string LoiChuaCoThongTin = "Chưa có thông tin tấm. Bấm Quét.";

        /// <summary>
        /// Luồng quét của tab: F-name + dim lấy từ bản vẽ; thông tin tấm theo thứ tự: kết quả vừa quét
        /// (theo handle khung) → data CSV (theo tên N).
        /// Thiếu vát / pitchi → tấm vẫn có (sửa được) kèm cảnh báo trong Loi.
        /// </summary>
        public static (TamXuat Tam, string Loi) Ghep(KhungTho k, string lopDim, int dimzin,
                                                     IReadOnlyDictionary<string, ThongTinTam> cu,
                                                     IReadOnlyDictionary<long, ThongTinTam> quet)
        {
            var (tam, loi) = DocFnameDim(k, lopDim, dimzin);
            if (loi != null) return (null, loi);
            cu.TryGetValue(tam.N, out ThongTinTam daCo);
            if (quet.TryGetValue(k.Handle, out ThongTinTam moi)) tam.Tt = GiuGiaTriNhapTay(moi, daCo);
            else if (daCo != null) tam.Tt = daCo;
            else return (null, LoiChuaCoThongTin);
            return (tam, KiemTra(tam.Tt));
        }

        /// <summary>CSV là nơi lưu duy nhất: quét lại không được xoá số người dùng đã nhập mà lisp không đo được.</summary>
        private static ThongTinTam GiuGiaTriNhapTay(ThongTinTam moi, ThongTinTam cu)
        {
            if (cu == null) return moi;
            // r1 / r2 là loại cuộn người dùng đã ghi, không đo được từ hình
            string cuonCu = CsvXuat.LoaiCuon(cu.LoaiGiaCong);
            if (cuonCu != "0" && CsvXuat.LoaiCuon(moi.LoaiGiaCong) == "0") ThemLoai(moi, "r" + cuonCu);
            moi.PitchiSoLan = cu.PitchiSoLan;
            moi.PitchiDai = cu.PitchiDai;
            moi.GiaBaoGia = cu.GiaBaoGia;
            moi.TienVatLieu = cu.TienVatLieu;
            moi.UuDai = cu.UuDai;
            if (moi.Vat == 0) moi.Vat = cu.Vat;
            // tổng Tapu + Sara không đổi thì giữ cách chia người dùng đã sửa
            if (cu.Tappu + cu.Sara == moi.Tappu + moi.Sara) { moi.Tappu = cu.Tappu; moi.Sara = cu.Sara; }
            else
            {
                // bản vẽ không có ghi chú / tô màu: giữ số đã nhập
                if (moi.Sara == 0) moi.Sara = cu.Sara;
                if (moi.Tappu == 0) moi.Tappu = cu.Tappu;
            }
            return moi;
        }

        private static void ThemLoai(ThongTinTam tt, string loai) =>
            tt.LoaiGiaCong = string.IsNullOrEmpty(tt.LoaiGiaCong) ? loai : tt.LoaiGiaCong + ", " + loai;

        /// <summary>Tấm vừa quét chưa có r1 / r2: thêm nếu trong khung có mặt cắt cuộn khớp kích thước tấm.</summary>
        public static void ThemCuonTuHinh(TamXuat t, IReadOnlyList<CungHinh> cung, IReadOnlyList<NetBe> doan = null)
        {
            if (CsvXuat.LoaiCuon(t.Tt.LoaiGiaCong) != "0") return;
            var so = CultureInfo.InvariantCulture;
            if (!double.TryParse(t.X, NumberStyles.Float, so, out double x) || !double.TryParse(t.Y, NumberStyles.Float, so, out double y) ||
                !double.TryParse((t.T ?? "").Trim(), NumberStyles.Float, so, out double day)) return;
            string r = PhanTichHinh.LoaiCuonTuHinh(cung, x, y, day, doan);
            if (r != null) ThemLoai(t.Tt, r);
        }

        /// <summary>Tên N để hiện ở danh sách lỗi, kể cả khi F-name chưa đủ trường; không có → null.</summary>
        public static string TenKhung(KhungTho k) =>
            k.Text.Where(t => CungLop(t.Lop, LopFname))
                  .SelectMany(t => t.LaMtext ? ThongTinTamParser.TachDong(t.NoiDung, true) : new List<string> { t.NoiDung ?? "" })
                  .Where(d => d.StartsWith("N=") && d.Length > 2).Select(d => d.Substring(2)).FirstOrDefault();

        /// <summary>Khung có text trên layer F-name (khung chú thích không có → không phải tấm).</summary>
        public static bool CoFname(KhungTho k) => k.Text.Any(t => CungLop(t.Lop, LopFname));

        public const string LoiKhungChuThich = "Bỏ qua: khung chú thích, không tính vào CSV.";

        /// <summary>
        /// Khung chú thích của bản vẽ mẫu: có từ 2 dòng "N=" trên layer F-name (một tấm chỉ có 1 F-name).
        /// Tab vẫn hiện nó (để không lặng lẽ bỏ sót) nhưng không chặn Xuất CSV.
        /// </summary>
        public static bool LaKhungChuThich(KhungTho k) => SoDongN(k) >= 2;

        /// <summary>Số dòng "N=" trên layer F-name; chỉ khung có đúng 1 dòng mới được phân tích hình học.</summary>
        public static int SoDongN(KhungTho k) =>
            k.Text.Where(t => CungLop(t.Lop, LopFname))
                  .Sum(t => t.LaMtext
                      ? ThongTinTamParser.TachDong(t.NoiDung, true).Count(d => d.StartsWith("N="))
                      : (t.NoiDung ?? "").StartsWith("N=") ? 1 : 0);

        public const string LoiThieuPitchi = "Có gia công pitchi, chưa nhập số lần hoặc chiều dài.";

        /// <summary>Đưa ds về đúng thứ tự của moi mà giữ phần tử còn lại, để ô đang nhập trên giao diện không bị dựng lại.</summary>
        public static void DongBoDs<T>(System.Collections.ObjectModel.ObservableCollection<T> ds, IList<T> moi) where T : class
        {
            for (int i = ds.Count - 1; i >= 0; i--) if (!moi.Contains(ds[i])) ds.RemoveAt(i);
            for (int i = 0; i < moi.Count; i++)
            {
                if (i < ds.Count && ds[i] == moi[i]) continue;
                int cu = ds.IndexOf(moi[i]);
                if (cu >= 0) ds.Move(cu, i); else ds.Insert(i, moi[i]);
            }
        }

        /// <summary>Có gia công k mà chưa có vát / m(pitchi) mà thiếu số lần hoặc chiều dài → lời cảnh báo.</summary>
        public static string KiemTra(ThongTinTam tt)
        {
            string gc = (tt.LoaiGiaCong ?? "").ToUpperInvariant();
            if (gc.Contains("K") && tt.Vat <= 0)
                return "Có gia công vát (k), chưa nhập chiều dài vát.";
            if (gc.Contains("PITCHI") && (tt.PitchiSoLan <= 0 || tt.PitchiDai <= 0))
                return LoiThieuPitchi;
            return null;
        }

        /// <summary>Như lisp trước khi ghi CSV: m(pitchi) → m, bỏ loại trùng. Gọi lại nhiều lần vẫn ra như nhau.</summary>
        public static string ChuanHoaGiaCong(string gc) =>
            string.Join(", ", (gc ?? "").Replace("m(pitchi)", "m").Split(new[] { ", " }, StringSplitOptions.None).Distinct()).Trim(' ');

        /// <summary>F-name (N, M, T, Q) + dim tổng (X ≥ Y) của khung; Tt để trống.</summary>
        public static (TamXuat Tam, string Loi) DocFnameDim(KhungTho k, string lopDim, int dimzin)
        {
            var fnMtext = k.Text.Where(t => CungLop(t.Lop, LopFname) && t.LaMtext).ToList();
            var fnText = k.Text.Where(t => CungLop(t.Lop, LopFname) && !t.LaMtext).ToList();
            if (fnMtext.Count > 0 && fnText.Count > 0)
                return (null, "F-name trộn TEXT và MTEXT. Chỉ giữ 1 kiểu.");

            string n = null, m = null, t = null, q = null;
            if (fnMtext.Count > 0)
            {
                if (fnMtext.Count > 1) return (null, "Khung có nhiều hơn 1 F-name.");
                string c = ThongTinTamParser.BoBocMtext(fnMtext[0].NoiDung ?? "");
                if (!new[] { "N=", "M=", "Q=", "T=", "Z=" }.Any(c.Contains)) return (null, LoiKhongFname);
                if (c.Contains("\r")) c = c.Replace("\r", ",").Replace("\n", "").Replace("\\P", "");
                else if (c.Contains("\n")) c = c.Replace("\n", ",").Replace("\\P", "");
                else if (c.Contains("\\P")) c = c.Replace("\\P", ",");
                if (!new[] { "N=", "M=", "Q=", "T=" }.Any(c.Contains)) return (null, "F-name không có N, M, T, Q.");
                n = ExportA2b(c, "N=", ","); m = ExportA2b(c, "M=", ",");
                t = ExportA2b(c, "T=", ","); q = ExportA2b(c, "Q=", ",");
            }
            else if (fnText.Count >= 4)   // lisp bỏ qua cả vòng khi dưới 4 TEXT
            {
                foreach (var x in fnText)
                {
                    string s = x.NoiDung ?? "";
                    if (s.StartsWith("N=")) n = s.TrimStart('N', '=');   // vl-string-left-trim cắt theo tập ký tự
                    if (s.StartsWith("M=")) m = s.TrimStart('M', '=');
                    if (s.StartsWith("T=")) t = s.TrimStart('T', '=');
                    if (s.StartsWith("Q=")) q = s.TrimStart('Q', '=');
                }
            }
            if (n == null || t == null || q == null || m == null) return (null, LoiKhongFname);
            if (n == "" || m == "" || t == "" || q == "") return (null, "F-name có trường bị trống.");

            var thang = k.Dim.Where(d => CungLop(d.Lop, lopDim) && (d.Co70 == 32 || d.Co70 == 160)).ToList();
            if (thang.Count == 0)   // bản vẽ chỉ dùng dim Aligned: đúng 2 cái mới là X, Y, để không lấn nhánh dim phi
            {
                var canh = k.Dim.Where(d => CungLop(d.Lop, lopDim) && (d.Co70 == 33 || d.Co70 == 161)).ToList();
                // Aligned đo theo đường nối 2 điểm, nên chỉ là X, Y khi 1 cái nằm ngang và 1 cái thẳng đứng (lệch tối đa ~0.1°)
                bool ngang(DimTho d) => Math.Abs(d.DY) <= 0.002 * Math.Abs(d.DX);
                bool dung(DimTho d) => Math.Abs(d.DX) <= 0.002 * Math.Abs(d.DY);
                if (canh.Count == 2 && canh.Any(ngang) && canh.Any(dung)) thang = canh;
                else if (canh.Count == 2 && !k.Dim.Any(d => CungLop(d.Lop, lopDim) && (d.Co70 == 163 || d.Co70 == 35 || d.Co70 == 3)))
                    return (null, LoiAlignedXien);
            }
            string gx, gy;
            if (thang.Count > 0)
            {
                if (thang.Count != 2) return (null, LoiSoDim);
                string g1 = Rtos(thang[0].DoDai, thang[0].Dimdec, dimzin), g2 = Rtos(thang[1].DoDai, thang[1].Dimdec, dimzin);
                bool dung = Atof(g1) >= Atof(g2);
                gx = dung ? g1 : g2; gy = dung ? g2 : g1;
            }
            else
            {
                var phi = k.Dim.Where(d => CungLop(d.Lop, lopDim) && (d.Co70 == 163 || d.Co70 == 35 || d.Co70 == 3)).ToList();
                if (phi.Count == 0) return (null, "Không tìm thấy dim tổng.");
                if (phi.Count > 1) return (null, LoiSoDim);
                gx = gy = Rtos(phi[0].DoDai, phi[0].Dimdec, dimzin);
            }
            return (new TamXuat
            {
                N = n, M = m, T = t, Q = q, X = gx, Y = gy,
                DiemX = k.MinX, DiemY = k.MaxY, HandleKhung = k.Handle,
            }, null);
        }

        /// <summary>
        /// rtos mode 2 với soLe chữ số; bit 8 của DIMZIN bỏ số 0 cuối, bit 4 bỏ số 0 đầu (rtos chịu ảnh hưởng DIMZIN).
        /// </summary>
        public static string Rtos(double v, int soLe, int dimzin)
        {
            if (soLe < 0) soLe = 0;
            string s = Math.Round(v, soLe, MidpointRounding.AwayFromZero).ToString("F" + soLe, CultureInfo.InvariantCulture);
            if ((dimzin & 8) != 0 && s.Contains(".")) s = s.TrimEnd('0').TrimEnd('.');
            if ((dimzin & 4) != 0)
            {
                if (s.StartsWith("0.")) s = s.Substring(1);
                else if (s.StartsWith("-0.")) s = "-" + s.Substring(2);
            }
            return s;
        }

        /// <summary>export-string-a2b: chuỗi giữa lần gặp đầu của a và lần gặp b kế tiếp (tìm b từ sau ký tự đầu của a).</summary>
        public static string ExportA2b(string str, string a, string b)
        {
            int i = a == "" ? 0 : str.IndexOf(a, StringComparison.Ordinal);
            if (i < 0) return null;
            int batDau = i + a.Length;
            int j = str.IndexOf(b, Math.Min(i + 1, str.Length), StringComparison.Ordinal);
            if (j < 0 || j < batDau) return batDau <= str.Length ? str.Substring(batDau) : "";
            return str.Substring(batDau, j - batDau);
        }

        /// <summary>Độ dày người dùng gõ → ít nhất 1 số lẻ (6 → 6.0, 3,2 → 3.2), không làm tròn số lẻ có sẵn (1.25). Sai → null.</summary>
        public static string ChuanDoDay(string s)
        {
            s = (s ?? "").Trim().Replace(',', '.');
            return double.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double d) && d > 0
                ? d.ToString("0.0#####", CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Thay giá trị trường F-name (N/M/T/Q) trong nội dung thô của 1 TEXT/MTEXT; trường không có
        /// trong text này → null. MTEXT giữ nguyên khối định dạng "{\...;" đầu và "}" cuối.
        /// </summary>
        public static string ThayTruongFname(string noiDung, bool laMtext, char truong, string giaTri)
        {
            string dau = truong + "=";
            noiDung = noiDung ?? "";
            if (!laMtext) return noiDung.StartsWith(dau) ? dau + giaTri : null;
            string[] doan = noiDung.Split(new[] { "\\P" }, StringSplitOptions.None);
            bool boc = noiDung.StartsWith("{\\") && noiDung.EndsWith("}");
            for (int i = 0; i < doan.Length; i++)
            {
                int batDau = 0;
                if (i == 0 && boc) batDau = doan[0].IndexOf(';') + 1;
                if (string.CompareOrdinal(doan[i], batDau, dau, 0, dau.Length) != 0) continue;
                bool cuoi = boc && i == doan.Length - 1;
                doan[i] = doan[i].Substring(0, batDau) + dau + giaTri + (cuoi ? "}" : "");
                return string.Join("\\P", doan);
            }
            return null;
        }

        private static bool CungLop(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static double Atof(string s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d :
            (s != null && s.StartsWith(".") && double.TryParse("0" + s, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0);
    }
}
