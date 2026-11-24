using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsvTool.Core
{
    /// <summary>Thông tin gia công của 1 tấm, lưu ở data\&lt;đơn&gt;-data.csv.</summary>
    public sealed class ThongTinTam
    {
        public int LoNho, LoLon, Tappu, Sara, PitchiSoLan, PitchiDai, Vat;
        public List<(int Dai, int SoLuong)> DuongBe = new List<(int, int)>();
        // 5 trường giữ nguyên chuỗi người dùng gõ (không ép số)
        public string LoaiGiaCong = "", CheDoTinh = "", GiaBaoGia = "", TienVatLieu = "", UuDai = "";
    }

    /// <summary>Tách dòng MText và chuẩn hoá chuỗi, chép hành vi CSV--mtext-plain của lisp.</summary>
    public static class ThongTinTamParser
    {
        private static readonly Regex EscapeU = new Regex(@"\\U\+([0-9A-Fa-f]{4})");

        /// <summary>Tách nội dung thô của 1 TEXT/MTEXT thành các dòng đã trim.</summary>
        public static List<string> TachDong(string noiDung, bool laMtext)
        {
            var kq = new List<string>();
            if (noiDung == null) return kq;
            string txt = laMtext ? BoBocMtext(MtextPhang(noiDung)) : noiDung;
            foreach (string p in txt.Replace("\\P", "\n").Split('\n'))
                kq.Add(p.Trim(' ', '\r'));
            return kq;
        }

        /// <summary>
        /// Nội dung MTEXT đồng một định dạng: bỏ mọi mã giữa dòng (font, màu, cao chữ, gạch chân…) và ngoặc nhóm,
        /// chỉ giữ font đầu tiên cho cả khối (chữ Nhật vẫn hiện). Tách MTEXT đã làm phẳng → mỗi dòng đúng 1 TEXT
        /// (trước đó "M=SUS30" + "4 No.1" bị tách đôi chỗ đổi định dạng).
        /// </summary>
        public static string MtextPhang(string noiDung)
        {
            string s = noiDung ?? "", font = null;
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '{' || c == '}') continue;
                if (c != '\\' || i + 1 >= s.Length) { sb.Append(c); continue; }
                char n = s[i + 1];
                if (n == 'P') { sb.Append("\\P"); i++; }
                else if (n == '\\' || n == '{' || n == '}') { sb.Append('\\').Append(n); i++; }
                else if (n == '~') { sb.Append(' '); i++; }
                else if (n == 'U' || n == 'u') sb.Append(c);                    // \U+XXXX giữ nguyên để CAD giải mã
                else if ("LlOoKkNn".IndexOf(n) >= 0) i++;
                else
                {
                    int ket = s.IndexOf(';', i);
                    if (ket < 0) { i++; continue; }
                    string ma = s.Substring(i, ket - i + 1);
                    if (n == 'S') sb.Append(ma.Substring(2, ma.Length - 3).Replace('^', '/').Replace('#', '/'));   // phân số xếp chồng
                    else if ((n == 'f' || n == 'F') && font == null) font = ma;
                    i = ket;
                }
            }
            return font == null ? sb.ToString() : "{" + font + sb + "}";
        }

        /// <summary>CSV--mtext-plain: \P thành xuống dòng, bỏ khối "{\...;" đầu và "}" cuối.</summary>
        public static string BoBocMtext(string txt)
        {
            txt = txt.Replace("\\P", "\n");
            if (txt.Length > 2 && txt.StartsWith("{\\"))
            {
                int c = txt.IndexOf(';');
                if (c >= 0) txt = txt.Substring(c + 1);
                if (txt.EndsWith("}")) txt = txt.Substring(0, txt.Length - 1);
            }
            return txt;
        }

        /// <summary>Giải mã \U+XXXX, bỏ dấu tiếng Việt, viết hoa — để cả 2 cách CAD lưu chữ cho cùng kết quả.</summary>
        public static string ChuanHoa(string s)
        {
            s = EscapeU.Replace(s ?? "", m => ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString());
            var sb = new StringBuilder();
            foreach (char ch in s.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(ch == 'đ' ? 'd' : ch == 'Đ' ? 'D' : ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }

        /// <summary>atoi của AutoLISP: dấu + chữ số ở đầu chuỗi, không có → 0.</summary>
        public static int Atoi(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            Match m = Regex.Match(s, @"^\s*[+-]?\d+");
            return m.Success && int.TryParse(m.Value.Trim(), out int n) ? n : 0;
        }

    }
}
