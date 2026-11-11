using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TeamsTool.Core
{
    /// <summary>Những gì phòng 2 cần từ một DXF: lỗ tròn, chữ, kích thước bao (đơn vị bản vẽ).</summary>
    public sealed class KetQuaDxf
    {
        public List<(double X, double Y, double D)> Lo = new List<(double, double, double)>();
        public List<string> Chu = new List<string>();
        public double MinX = double.MaxValue, MinY = double.MaxValue, MaxX = double.MinValue, MaxY = double.MinValue;
        public int SoNet;
        public bool CoHinh => MaxX >= MinX;

        internal void Bao(double x, double y)
        {
            if (x < MinX) MinX = x; if (x > MaxX) MaxX = x;
            if (y < MinY) MinY = y; if (y > MaxY) MaxY = y;
        }
    }

    /// <summary>
    /// Đọc DXF ASCII (không cần CAD): mục ENTITIES — CIRCLE → lỗ, TEXT / MTEXT → chữ, LINE / ARC / LWPOLYLINE / CIRCLE → khung bao.
    /// shortcut: không đọc BLOCK / INSERT (hình nằm trong block thì không thấy) — khi gặp đơn như vậy thì nổ block trước hoặc thêm đọc INSERT.
    /// </summary>
    public static class DocDxf
    {
        public static KetQuaDxf DocFile(string file) => Doc(DocChu(File.ReadAllBytes(file)));

        /// <summary>DXF 2007+ là UTF-8; cũ hơn là ANSI theo $DWGCODEPAGE (bản vẽ Nhật = CP932).</summary>
        public static string DocChu(byte[] b)
        {
            if (b.Length > 22 && Encoding.ASCII.GetString(b, 0, 22).StartsWith("AutoCAD Binary DXF", StringComparison.Ordinal))
                throw new InvalidDataException("DXF nhị phân chưa đọc được — lưu lại dạng DXF ASCII");
            try { return new UTF8Encoding(false, true).GetString(b); }
            catch (DecoderFallbackException) { }
            try { return Encoding.GetEncoding(932).GetString(b); }
            catch (Exception) { return Encoding.UTF8.GetString(b); } // .NET mới không có CP932 nếu chưa đăng ký provider
        }

        public static KetQuaDxf Doc(string text)
        {
            var kq = new KetQuaDxf();
            string[] d = (text ?? "").Replace("\r\n", "\n").Split('\n');
            int i = 0;
            bool trongEntities = false;
            // Mỗi cặp (mã nhóm, giá trị). Thực thể bắt đầu ở mã 0.
            while (i + 1 < d.Length)
            {
                string ma = d[i].Trim(), gt = d[i + 1].Trim();
                if (ma == "2" && gt == "ENTITIES") trongEntities = true;
                if (ma == "0" && gt == "ENDSEC") trongEntities = false;
                if (!(trongEntities && ma == "0" && gt != "SECTION"))
                {
                    i += 2;
                    continue;
                }
                string loai = gt;
                var nhom = new List<KeyValuePair<int, string>>();
                i += 2;
                while (i + 1 < d.Length && d[i].Trim() != "0")
                {
                    if (int.TryParse(d[i].Trim(), out int m)) nhom.Add(new KeyValuePair<int, string>(m, d[i + 1]));
                    i += 2;
                }
                ThemThucThe(kq, loai, nhom);
            }
            return kq;
        }

        private static double So(List<KeyValuePair<int, string>> n, int ma, int thu = 0)
        {
            var ds = n.Where(kv => kv.Key == ma).ToList();
            return thu < ds.Count && double.TryParse(ds[thu].Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
        }

        private static void ThemThucThe(KetQuaDxf kq, string loai, List<KeyValuePair<int, string>> n)
        {
            switch (loai)
            {
                case "CIRCLE":
                {
                    double x = So(n, 10), y = So(n, 20), r = So(n, 40);
                    if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(r)) return;
                    kq.Lo.Add((x, y, Math.Round(2 * r, 3)));
                    kq.Bao(x - r, y - r); kq.Bao(x + r, y + r);
                    kq.SoNet++;
                    break;
                }
                case "LINE":
                    BaoDiem(kq, So(n, 10), So(n, 20)); BaoDiem(kq, So(n, 11), So(n, 21));
                    kq.SoNet++;
                    break;
                case "ARC":
                {
                    // shortcut: bao cả đường tròn chứa cung (thừa một chút) — đủ cho kích thước bao gần đúng
                    double x = So(n, 10), y = So(n, 20), r = So(n, 40);
                    if (!double.IsNaN(r)) { BaoDiem(kq, x - r, y - r); BaoDiem(kq, x + r, y + r); }
                    kq.SoNet++;
                    break;
                }
                case "LWPOLYLINE":
                    for (int k = 0; ; k++)
                    {
                        double x = So(n, 10, k), y = So(n, 20, k);
                        if (double.IsNaN(x)) break;
                        BaoDiem(kq, x, y);
                    }
                    kq.SoNet++;
                    break;
                case "TEXT":
                case "MTEXT":
                {
                    // MTEXT dài tách ở mã 3 (các đoạn đầu) + mã 1 (đoạn cuối)
                    string s = string.Concat(n.Where(kv => kv.Key == 3).Select(kv => kv.Value)) + string.Concat(n.Where(kv => kv.Key == 1).Select(kv => kv.Value));
                    s = loai == "MTEXT" ? BoMaMtext(s) : GiaiMaUnicode(s);
                    if (s.Trim().Length > 0) kq.Chu.Add(s.Trim());
                    break;
                }
            }
        }

        private static void BaoDiem(KetQuaDxf kq, double x, double y)
        {
            if (!double.IsNaN(x) && !double.IsNaN(y)) kq.Bao(x, y);
        }

        /// <summary>\U+30BF → タ (chữ Nhật trong DXF ANSI).</summary>
        public static string GiaiMaUnicode(string s) =>
            Regex.Replace(s ?? "", @"\\U\+([0-9A-Fa-f]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());

        /// <summary>Bỏ mã định dạng MTEXT: \P xuống dòng, {\fArial|b0;…}, \H2.5x; … giữ chữ.</summary>
        public static string BoMaMtext(string s)
        {
            s = GiaiMaUnicode(s);
            s = s.Replace("\\P", "\n").Replace("\\~", " ");
            s = Regex.Replace(s, @"\\[ACcFfHhQqTtWw][^;]*;", "");
            s = Regex.Replace(s, @"\\[LlOoKk]", "");
            s = Regex.Replace(s, @"\\S([^;]*)\^([^;]*);", "$1/$2");
            return s.Replace("{", "").Replace("}", "").Replace("\\\\", "\\");
        }

        /// <summary>Tóm tắt gửi AI (phòng 2): kích thước bao, lỗ gom theo Ø, toàn bộ chữ.</summary>
        public static string TomTat(string ten, KetQuaDxf kq)
        {
            var sb = new StringBuilder();
            sb.Append("DXF: ").Append(ten).Append('\n');
            if (kq.CoHinh)
                sb.Append(string.Format(CultureInfo.InvariantCulture, "Kich thuoc bao (gom ca khung ve neu co): {0:0.##} x {1:0.##}\n", kq.MaxX - kq.MinX, kq.MaxY - kq.MinY));
            sb.Append("So net: ").Append(kq.SoNet).Append('\n');
            foreach (var g in kq.Lo.GroupBy(l => l.D).OrderBy(g => g.Key))
                sb.Append(string.Format(CultureInfo.InvariantCulture, "Lo O{0:0.###}: {1} cai\n", g.Key, g.Count()));
            sb.Append("Chu tren ban ve:\n");
            foreach (string c in kq.Chu) sb.Append("- ").Append(c.Replace("\n", " / ")).Append('\n');
            return sb.ToString();
        }
    }
}
