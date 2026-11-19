using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsvTool.Core
{
    /// <summary>Giá trị đọc từ MText thông tin tấm (layer KHUNG_IN) — xem CSV--pricing-text-content của lisp.</summary>
    public sealed class ThongTinTam
    {
        public int LoNho, LoLon, Tappu, Sara, PitchiSoLan, PitchiDai, Vat;
        public List<(int Dai, int SoLuong)> DuongBe = new List<(int, int)>();
        // 5 trường giữ nguyên chuỗi người dùng gõ (không ép số)
        public string LoaiGiaCong = "", CheDoTinh = "", GiaBaoGia = "", TienVatLieu = "", UuDai = "";
    }

    /// <summary>Chép đúng hành vi CSV--mtext-plain / CSV--pricing-text-lines / CSV--pricing-data-from-text.</summary>
    public static class ThongTinTamParser
    {
        public const string LoiKhongCoText = "Không tìm thấy text thông tin trên layer KHUNG_IN trong khung.";

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

        public static bool LaTextThongTin(IEnumerable<string> dong) =>
            dong.Any(l => { string u = l.ToUpperInvariant(); return u.Contains("TAPPU") || u.Contains("TAPU&SARA"); });

        /// <summary>
        /// Đọc toàn bộ dòng text thông tin của 1 khung. Thứ tự dòng = thứ tự lisp duyệt
        /// (người gọi quyết định); đường bẻ giữ đúng thứ tự gặp.
        /// </summary>
        public static (ThongTinTam Tam, string Loi) Doc(IReadOnlyList<string> tatCaDong)
        {
            if (tatCaDong == null || tatCaDong.Count == 0) return (null, LoiKhongCoText);

            var t = new ThongTinTam();
            bool coM = false;
            int maxBe = 0;
            foreach (string line in tatCaDong)
            {
                string u = ChuanHoa(line);
                string v = SauHaiCham(line);
                if (WcMatch(u, "L*TAPPU:*")) t.Tappu = Atoi(v);
                else if (WcMatch(u, "L*SARA:*")) t.Sara = Atoi(v);
                else if (WcMatch(u, "PITCHI*(MM)*")) t.PitchiDai = Atoi(v);
                else if (WcMatch(u, "PITCHI*")) t.PitchiSoLan = Atoi(v);
                else if (WcMatch(u, "CH*NH:*")) t.CheDoTinh = v ?? "";
                else if (WcMatch(u, "GI*")) t.GiaBaoGia = v ?? "";
                else if (WcMatch(u, "TI*")) t.TienVatLieu = v ?? "";
                else if (WcMatch(u, "*(%):*")) t.UuDai = v ?? "";
                else if (WcMatch(u, "L*NH*:*")) t.LoNho = Atoi(v);
                else if (WcMatch(u, "L*L*N:*")) t.LoLon = Atoi(v);
                else if (WcMatch(u, "VAT:*") || WcMatch(u, "*VAT:*") || u.StartsWith("V") || WcMatch(u, "*CHI*U D*I V*T:*"))
                    t.Vat = Atoi(v);
                else if (WcMatch(u, "*C*C LO*I GIA C*NG*"))
                {
                    t.LoaiGiaCong = v ?? "";
                    if (t.LoaiGiaCong.ToUpperInvariant().Contains("M")) coM = true;
                }
                else if (v != null && v.Contains("x"))
                {
                    int xp = v.IndexOf('x');
                    t.DuongBe.Add((Atoi(v.Substring(0, xp).Trim(' ')), Atoi(v.Substring(xp + 1).Trim(' '))));
                    int c = line.IndexOf(':');
                    int idx = SoCuoi(line.Substring(0, c).Trim(' '));
                    if (idx > maxBe) maxBe = idx;
                }
            }

            int canCo = 12 + (coM ? maxBe : 0);
            if (tatCaDong.Count != canCo)
                return (null, "Số lượng text thông tin không chính xác (Thực tế: " + tatCaDong.Count +
                              " text, Yêu cầu: " + canCo + " text).\n" +
                              "Tấm này có gia công m: " + (coM ? "Có" : "Không") +
                              ", số loại đường bẻ: " + maxBe + ".\n" +
                              "Vui lòng kiểm tra xem có bị xuất trùng đè lên nhau không!\n" +
                              "Nếu text này do phiên bản cũ tạo ra, hãy chạy lại \"Xuất thông tin tấm\".");
            return (t, null);
        }

        /// <summary>
        /// Thay giá trị sau dấu ":" của dòng có nhãn (không dấu, chữ hoa, vd "LO SARA") trong nội dung
        /// MText thô; các dòng khác giữ nguyên từng ký tự. Không thấy nhãn → InvalidOperationException.
        /// </summary>
        public static string ThayGiaTri(string noiDungMtext, string nhanKhongDau, string giaTriMoi)
        {
            string[] doan = (noiDungMtext ?? "").Split(new[] { "\\P" }, StringSplitOptions.None);
            string can = nhanKhongDau.Trim().ToUpperInvariant();
            for (int i = 0; i < doan.Length; i++)
            {
                int c = doan[i].IndexOf(':');
                if (c < 0) continue;
                string nhan = doan[i].Substring(0, c);
                if (i == 0) nhan = BoBocMtext(nhan + ":").TrimEnd(':');
                if (ChuanHoa(nhan).Trim() != can) continue;
                string sau = doan[i].Substring(c + 1);
                // giữ kiểu cũ: "Lỗ Sara: 0" có cách, "Chế độ tính:0" thì không
                string dem = sau.StartsWith(" ") || sau.Trim().Length == 0 ? " " : "";
                bool dongNgoac = i == doan.Length - 1 && doan[i].EndsWith("}") && noiDungMtext.StartsWith("{\\");
                doan[i] = doan[i].Substring(0, c + 1) + dem + giaTriMoi + (dongNgoac ? "}" : "");
                return string.Join("\\P", doan);
            }
            throw new InvalidOperationException("Không tìm thấy dòng \"" + nhanKhongDau + "\" trong MText thông tin tấm.");
        }

        /// <summary>wcmatch rút gọn: chỉ ký tự đại diện '*', so khớp nguyên chuỗi.</summary>
        public static bool WcMatch(string s, string mau)
        {
            string re = "^" + string.Join(".*", mau.Split('*').Select(Regex.Escape)) + "$";
            return Regex.IsMatch(s ?? "", re, RegexOptions.Singleline);
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

        /// <summary>CSV--line-value-after-colon: phần sau ":" đầu tiên đã trim; không có ":" → null.</summary>
        public static string SauHaiCham(string line)
        {
            int c = line.IndexOf(':');
            return c < 0 ? null : line.Substring(c + 1).Trim(' ');
        }

        /// <summary>atoi của AutoLISP: dấu + chữ số ở đầu chuỗi, không có → 0.</summary>
        public static int Atoi(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            Match m = Regex.Match(s, @"^\s*[+-]?\d+");
            return m.Success && int.TryParse(m.Value.Trim(), out int n) ? n : 0;
        }

        /// <summary>CSV--trailing-int: số nguyên ở cuối nhãn ("Đường bẻ 12" → 12), không có → 0.</summary>
        private static int SoCuoi(string s)
        {
            int i = s.Length;
            while (i > 0 && char.IsDigit(s[i - 1]) && s[i - 1] < 128) i--;
            return i == s.Length ? 0 : int.Parse(s.Substring(i));
        }
    }
}
