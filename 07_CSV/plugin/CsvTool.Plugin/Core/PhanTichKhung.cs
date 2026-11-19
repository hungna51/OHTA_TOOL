using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CsvTool.Core
{
    /// <summary>TEXT/MTEXT thô nằm trọn trong khung. NoiDung = chuỗi thô (MText.Contents / DXF 3+1).</summary>
    public sealed class TextTho { public string Lop, NoiDung; public bool LaMtext; }

    /// <summary>DIMENSION thô: Co70 = mã DXF 70 (32/160 = dim thẳng, 3/35/163 = dim phi), DoDai = mã 42.</summary>
    public sealed class DimTho { public string Lop; public int Co70; public double DoDai; public int Dimdec; }

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

    /// <summary>Chép đúng hành vi CSV--analyze-one-frame (EXPORT, không chạy hình học).</summary>
    public static class PhanTichKhung
    {
        public const string LopFname = "2-F-NAME";
        public const string LopThongTin = "KHUNG_IN";

        private const string LoiKhongFname = "Không tìm thấy F-name!!!";
        private const string LoiSoDim = "Số lượng dim tổng không đúng!!!";

        public static (TamXuat Tam, string Loi) Chay(KhungTho k, string lopDim, int dimzin)
        {
            var fnMtext = k.Text.Where(t => CungLop(t.Lop, LopFname) && t.LaMtext).ToList();
            var fnText = k.Text.Where(t => CungLop(t.Lop, LopFname) && !t.LaMtext).ToList();
            if (fnMtext.Count > 0 && fnText.Count > 0)
                return (null, "Khung in có các loại Fname không hợp lệ! Vui lòng chỉ giữ lại 1 kiểu (MTEXT hoặc TEXT) trong mỗi khung in.");

            string n = null, m = null, t = null, q = null;
            if (fnMtext.Count > 0)
            {
                if (fnMtext.Count > 1) return (null, "Có nhiều hơn 1 Fname trong khung!!!");
                string c = ThongTinTamParser.BoBocMtext(fnMtext[0].NoiDung ?? "");
                if (!new[] { "N=", "M=", "Q=", "T=", "Z=" }.Any(c.Contains)) return (null, "ERROR: " + LoiKhongFname);
                if (c.Contains("\r")) c = c.Replace("\r", ",").Replace("\n", "").Replace("\\P", "");
                else if (c.Contains("\n")) c = c.Replace("\n", ",").Replace("\\P", "");
                else if (c.Contains("\\P")) c = c.Replace("\\P", ",");
                if (!new[] { "N=", "M=", "Q=", "T=" }.Any(c.Contains)) return (null, "F-name error(1)!!!");
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
            if (n == "" || m == "" || t == "" || q == "") return (null, "Lỗi Fname (2): Giá trị trường thông tin bị rỗng!");

            var thang = k.Dim.Where(d => CungLop(d.Lop, lopDim) && (d.Co70 == 32 || d.Co70 == 160)).ToList();
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
                if (phi.Count == 0) return (null, "Không tìm thấy dim tổng, kiểm tra lại dữ liệu!!!");
                if (phi.Count > 1) return (null, LoiSoDim);
                gx = gy = Rtos(phi[0].DoDai, phi[0].Dimdec, dimzin);
            }

            // CSV--pricing-text-raw-list gom dòng bằng cons → thứ tự đảo ngược toàn bộ
            var dong = new List<string>();
            foreach (var x in k.Text.Where(x => CungLop(x.Lop, LopThongTin)))
                dong.AddRange(ThongTinTamParser.TachDong(x.NoiDung, x.LaMtext));
            dong.Reverse();
            var tt = ThongTinTamParser.LaTextThongTin(dong)
                ? ThongTinTamParser.Doc(dong)
                : (null, ThongTinTamParser.LoiKhongCoText);
            if (tt.Loi != null) return (null, tt.Loi);

            string gc = tt.Tam.LoaiGiaCong ?? "";
            if (gc.ToUpperInvariant().Contains("K") && tt.Tam.Vat <= 0)
                return (null, "Chi tiết có gia công vát (k) nhưng chưa nhập chiều dài vát!");
            if (gc.ToUpperInvariant().Contains("PITCHI") && (tt.Tam.PitchiSoLan <= 0 || tt.Tam.PitchiDai <= 0))
                return (null, "Chi tiết có gia công m(pitchi) nhưng chưa nhập Pitchi - Số lần hoặc Pitchi - Chiều dài (mm)!");

            var thay = gc.Replace("m(pitchi)", "m").Split(new[] { ", " }, StringSplitOptions.None).Distinct();
            tt.Tam.LoaiGiaCong = string.Join(", ", thay).Trim(' ');

            return (new TamXuat
            {
                N = n, M = m, T = t, Q = q, X = gx, Y = gy, Tt = tt.Tam,
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

        private static bool CungLop(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static double Atof(string s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d :
            (s != null && s.StartsWith(".") && double.TryParse("0" + s, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0);
    }
}
