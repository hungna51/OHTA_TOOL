using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 225 (GĐ 3e): chép bước 2 "Phan loai tung Text" của <c>gro--geom-parse-dim-texts</c> — một chuỗi
    /// TEXT / MTEXT (nhóm 1) -> dim góc / lỗ (phi, R, TAP, DRILL) / dài + số đo. C# đọc chuỗi Unicode thật nên
    /// không có dấu "?" của máy byte CP932 (bg--codes); ký hiệu \U+XXXX trong chuỗi vẫn được giải như lisp.
    /// </summary>
    public static class DocText3b
    {
        public const int KhongPhai = 0, Dai = 1, Lo = 2, Goc = 3;

        public sealed class KetQua
        {
            public int Loai;
            public double V;
            public string Txt;
            public bool LaPhi;
        }

        private static readonly int[] MaDo = { 176, 65439, 223, 12444, 186, 730 };
        private static readonly int[] MaPhiRaw = { 966, 934, 981, 8709, 8960, 248, 216, 1060, 1092 };
        private static readonly int[] MaPhiTxt = { 966, 934, 981, 8709, 8960, 248, 216 };
        // gro--extract-num-after-sym: thu tu tim (ky hieu dau tien CO MAT thang, khong phai vi tri som nhat)
        private static readonly string[] KyHieuPhi =
        {
            "%%C", "φ", "ϕ", "Φ", "ø", "Ø", "⌀", "∅", "DIA.", "DIA", "R",
        };

        public static KetQua PhanLoai(string raw)
        {
            if (raw == null) return null;
            string rawU = GiaiEscape(raw);
            string txt = BoMaMtext(rawU);
            string up = txt.ToUpperInvariant(), rawUp = rawU.ToUpperInvariant();
            if (txt.Length == 0 || txt.Length > 35 || !txt.Any(c => c >= '0' && c <= '9')) return null;
            foreach (string k in new[] { "T=", "T =", "JASM", "NO.", "SCALE", "DWG", "REV", "KOKUIN", "DATE" })
                if (up.Contains(k)) return null;
            if (txt.IndexOf('枚') >= 0 || txt.IndexOf('本') >= 0) return null;   // MAI / HON

            // A. dim goc
            if (up.Contains("%%D") || up.Contains("DEG") || txt.Any(c => MaDo.Contains(c)))
            {
                string s = txt;
                foreach (int c in new[] { 176, 65439, 12444, 186, 730 }) s = s.Replace(((char)c).ToString(), "");
                foreach (string k in new[] { "%%d", "%%D", "DEG", "deg", " " }) s = s.Replace(k, "");
                double? v = SoDo(s);
                if (v != null && v > 0.0 && v < 360.0) return new KetQua { Loai = Goc, V = v.Value, Txt = txt };
            }

            // B. lo / phi / R
            bool laQ = CoQ(txt);
            bool laPhi = laQ || rawUp.Contains("%%C") || rawUp.Contains("DIA") || up.Contains("%%C") || up.Contains("DIA")
                         || rawU.Any(c => MaPhiRaw.Contains(c)) || txt.Any(c => MaPhiTxt.Contains(c));
            if (laPhi || rawUp.Contains("TAP") || rawUp.Contains("DRILL") || up.Contains("TAP") || up.Contains("DRILL")
                || (CoR(up) && !up.Contains("REV")) || (CoR(rawUp) && !rawUp.Contains("REV")))
            {
                double? v = SoSauKyHieu(txt) ?? SoSauKyHieu(rawU) ?? SoDo(txt);
                if (v != null && v > 0.0) return new KetQua { Loai = Lo, V = v.Value, Txt = txt, LaPhi = laPhi };
            }

            // C. dim dai
            double? vd = SoDo(txt) ?? SoDo(rawU);
            if (vd != null && vd > 0.0) return new KetQua { Loai = Dai, V = vd.Value, Txt = txt };
            return null;
        }

        /// <summary>wcmatch "*`?#*,*`? #*".</summary>
        private static bool CoQ(string s)
        {
            for (int i = 0; i + 1 < s.Length; i++)
                if (s[i] == '?' && (char.IsDigit(s[i + 1]) && s[i + 1] < 128 || (s[i + 1] == ' ' && i + 2 < s.Length && s[i + 2] >= '0' && s[i + 2] <= '9')))
                    return true;
            return false;
        }

        /// <summary>wcmatch "*R#*,*R #*,*R.*" trên chuỗi đã viết hoa.</summary>
        private static bool CoR(string up)
        {
            for (int i = 0; i + 1 < up.Length; i++)
            {
                if (up[i] != 'R') continue;
                char c = up[i + 1];
                if ((c >= '0' && c <= '9') || c == '.') return true;
                if (c == ' ' && i + 2 < up.Length && up[i + 2] >= '0' && up[i + 2] <= '9') return true;
            }
            return false;
        }

        /// <summary>gro--extract-num-after-sym.</summary>
        public static double? SoSauKyHieu(string txt)
        {
            string up = txt.ToUpperInvariant();
            foreach (string k in KyHieuPhi)
            {
                int pos = up.IndexOf(k.ToUpperInvariant(), StringComparison.Ordinal);
                if (pos >= 0 && (k != "R" || !up.Contains("REV"))) return SoDo(txt.Substring(pos + k.Length));
            }
            return SoDo(txt);
        }

        /// <summary>gro--extract-dim-num: bỏ "," và " ", lấy phần sau "-" đầu tiên, rồi cụm [0-9.] đầu tiên.</summary>
        public static double? SoDo(string txt)
        {
            txt = txt.Replace(",", "").Replace(" ", "");
            int g = txt.IndexOf('-');
            if (g >= 0) txt = txt.Substring(g + 1);
            var sb = new StringBuilder();
            foreach (char c in txt)
            {
                if ((c >= '0' && c <= '9') || c == '.') sb.Append(c);
                else if (sb.Length > 0) break;
            }
            string s = sb.ToString();
            if (s.Length == 0 || s == ".") return null;
            return double.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double v) ? v : (double?)null;
        }

        /// <summary>Phần \U+XXXX của bg--codes.</summary>
        public static string GiaiEscape(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 6 < s.Length && s[i + 1] == 'U' && s[i + 2] == '+'
                    && int.TryParse(s.Substring(i + 3, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int u))
                {
                    sb.Append((char)u);
                    i += 6;
                }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        /// <summary>bg--strip-codes: bỏ mã định dạng MTEXT (\P \~ -> cách, \\ \{ \} giữ ký tự, \S lấy chữ, {} bỏ).</summary>
        public static string BoMaMtext(string s)
        {
            var sb = new StringBuilder();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '\\')
                {
                    if (i >= s.Length) break;
                    char n = s[i];
                    if (n == 'P' || n == '~') { sb.Append(' '); i++; }
                    else if (n == '\\' || n == '{' || n == '}') { sb.Append(n); i++; }
                    else if ("LlOoKkX".IndexOf(n) >= 0) i++;
                    else if ("ACFfHQTWpS".IndexOf(n) >= 0)
                    {
                        i++;
                        while (i < s.Length && s[i] != ';')
                        {
                            char c2 = s[i++];
                            if (n == 'S' && c2 != '^' && c2 != '#' && c2 != '/') sb.Append(c2);
                        }
                        if (i < s.Length) i++;
                    }
                    else sb.Append('\\');
                }
                else if (c == '{' || c == '}') { }
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
