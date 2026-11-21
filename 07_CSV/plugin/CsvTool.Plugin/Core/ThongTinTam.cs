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
            string txt = laMtext ? BoBocMtext(noiDung) : noiDung;
            foreach (string p in txt.Replace("\\P", "\n").Split('\n'))
                kq.Add(p.Trim(' ', '\r'));
            return kq;
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
