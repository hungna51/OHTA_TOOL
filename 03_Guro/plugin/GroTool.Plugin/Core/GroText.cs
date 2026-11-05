using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Quy tắc chuỗi của lệnh gro, viết lại từ lisp. KHÔNG phụ thuộc ZWCAD — để test
    /// được ngoài CAD (dự án ZwCadPlugin.Tests biên dịch thẳng file này).
    /// </summary>
    public static class GroText
    {
        // Bango luôn là đoạn số CUỐI CÙNG sau dấu '-':
        //   mã khắc  "I-1-3"            (<mã>-<số bản vẽ>-<bango>, mục 32)
        //            "I-3"              (bản vẽ không có đoạn số)
        //   F-name   "N=xxxxxx-1-3"     (N=<don>-<bango>)
        // Chỉ được đụng vào đoạn CUỐI. Thay mọi cụm "-số" (bản cũ của palette) sẽ
        // ghi đè luôn số bản vẽ: "I-1-3" đổi bango 3->5 thành "I-5-5".
        private static readonly Regex DoanCuoi = new Regex(@"-(\s*)(\d+)(\s*)$", RegexOptions.Compiled);

        /// <summary>Thay đoạn số cuối (bango). Không có đoạn số cuối thì trả nguyên.</summary>
        public static string ThayBangoCuoi(string s, string bangoMoi)
        {
            if (string.IsNullOrEmpty(s) || bangoMoi == null) return s;
            Match m = DoanCuoi.Match(s);
            if (!m.Success) return s;
            Group g = m.Groups[2];
            return s.Substring(0, g.Index) + bangoMoi + s.Substring(g.Index + g.Length);
        }

        /// <summary>Đọc đoạn số cuối (bango). False nếu không có.</summary>
        public static bool TryDocBangoCuoi(string s, out string bango)
        {
            bango = null;
            if (string.IsNullOrEmpty(s)) return false;
            Match m = DoanCuoi.Match(s);
            if (!m.Success) return false;
            bango = m.Groups[2].Value;
            return true;
        }

        /// <summary>
        /// Bango hợp lệ: số nguyên dương, không đệm số 0. Lisp ghi bằng (rtos bango 2 0)
        /// nên luôn là "3", không bao giờ là "03".
        /// </summary>
        public static bool LaBangoHopLe(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            if (!Regex.IsMatch(s, @"^\d+$")) return false;
            return s[0] != '0';
        }

        /// <summary>
        /// Dòng F-name báo TẤM BẺ. gro chỉ ghi dòng "V=" khi cờ bẻ là "m" hoặc "*"
        /// (không ghi cho "k"). Bản cũ của palette dò chữ trong dòng "Z=" — sai: Z= là
        /// vật liệu (SS400 / SPC / SS400-SIMA), không bao giờ chứa dấu hiệu bẻ.
        /// </summary>
        public static bool LaDongBe(string dongFname)
        {
            if (dongFname == null) return false;
            return dongFname.Trim().StartsWith("V=", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Tên màu ACI — giống hệt gro--ten-mau để hai bản hiển thị như nhau.</summary>
        private static Encoding _cp932;

        /// <summary>
        /// Mục 203: 1 ký tự CP932 (1 hoặc 2 byte; b2 = 0 là 1 byte) -> mã Unicode, -1 nếu không
        /// giải được. Lisp trên máy ZWCAD kiểu byte chỉ có bảng ~60 ký tự (bg--uni-tbl), chữ Nhật
        /// ngoài bảng thành "?"; .NET có đủ bảng mã 932 nên lisp hỏi sang đây (gro_net_cp932).
        /// </summary>
        public static int Cp932SangUnicode(int b1, int b2)
        {
            try
            {
                if (_cp932 == null)
                {
#if !NETFRAMEWORK
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
                    _cp932 = Encoding.GetEncoding(932, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                }
                if (b1 < 0 || b1 > 255 || b2 < 0 || b2 > 255) return -1;
                byte[] by = b2 > 0 ? new[] { (byte)b1, (byte)b2 } : new[] { (byte)b1 };
                string s = _cp932.GetString(by);
                return s.Length == 1 ? s[0] : -1;
            }
            catch
            {
                return -1;
            }
        }

        public static string TenMau(int c)
        {
            if (c <= 0) return "Khong xac dinh";
            switch (c)
            {
                case 1: return "1 - Red (Do)";
                case 2: return "2 - Yellow (Vang)";
                case 3: return "3 - Green (Xanh la)";
                case 4: return "4 - Cyan (Da troi)";
                case 5: return "5 - Blue (Xanh duong)";
                case 6: return "6 - Magenta (Tim)";
                case 7: return "7 - White (Trang)";
                case 8: return "8 - Dark Gray";
                case 9: return "9 - Light Gray";
                default: return c + " (Mau CAD)";
            }
        }
    }
}
