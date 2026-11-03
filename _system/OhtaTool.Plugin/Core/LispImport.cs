using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Hỗ trợ nút ＋ thêm lisp: quét lệnh c:XXX trong file .lsp và chép file vào thư mục tab.
    /// Chỉ đọc file để lấy tên lệnh (luôn là ASCII), không đổi encoding;
    /// và được chép nguyên vẹn, không ghi lại nội dung.
    /// </summary>
    public static class LispImport
    {
        private static readonly Regex DefunRegex =
            new Regex(@"\(\s*defun\s+c:([A-Za-z0-9_\-\.\$\*@]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Danh sách lệnh (in hoa, không trùng, theo thứ tự xuất hiện) có trong file lisp.</summary>
        public static List<string> ScanCommands(byte[] lispBytes)
        {
            var result = new List<string>();
            if (lispBytes == null || lispBytes.Length == 0)
                return result;

            // File UTF-8 hợp lệ -> giải mã UTF-8. Còn lại coi là CP932: đọc Latin1 (1 byte = 1 ký tự, ASCII giữ nguyên)
            // và nhảy qua byte đuôi của ký tự 2 byte (byte đuôi có thể là 0x5C dấu gạch chéo ngược, không được hiểu là ký tự thoát).
            string text;
            bool cp932;
            try
            {
                text = new UTF8Encoding(false, true).GetString(lispBytes);
                cp932 = false;
            }
            catch (DecoderFallbackException)
            {
                text = Encoding.GetEncoding(28591).GetString(lispBytes);
                cp932 = true;
            }

            text = StripCommentsAndStrings(text, cp932);
            foreach (Match m in DefunRegex.Matches(text))
            {
                string cmd = m.Groups[1].Value.ToUpperInvariant();
                if (!result.Contains(cmd))
                    result.Add(cmd);
            }
            return result;
        }

        public static List<string> ScanCommandsInFile(string path) => ScanCommands(File.ReadAllBytes(path));

        private static bool IsCp932LeadByte(char c) => (c >= 0x81 && c <= 0x9F) || (c >= 0xE0 && c <= 0xFC);

        /// <summary>Thay chú thích ";...", ";| ... |;" và nội dung chuỗi "..." bằng khoảng trắng.</summary>
        private static string StripCommentsAndStrings(string s, bool cp932)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"')
                {
                    sb.Append(' ');
                    for (i++; i < s.Length; i++)
                    {
                        char d = s[i];
                        if (cp932 && IsCp932LeadByte(d))
                            i++;
                        else if (d == '\\')
                            i++;
                        else if (d == '"')
                            break;
                    }
                    sb.Append(' ');
                }
                else if (c == ';' && i + 1 < s.Length && s[i + 1] == '|')
                {
                    int end = s.IndexOf("|;", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? s.Length : end + 1;
                    sb.Append(' ');
                }
                else if (c == ';')
                {
                    while (i < s.Length && s[i] != '\n')
                        i++;
                    sb.Append('\n');
                }
                else if (cp932 && IsCp932LeadByte(c))
                {
                    i++;
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>Badge gợi ý từ tên lệnh: tối đa 4 ký tự chữ/số in hoa.</summary>
        public static string SuggestBadge(string command)
        {
            string clean = new string((command ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (clean.Length == 0)
                return "TL";
            return clean.Length > 4 ? clean.Substring(0, 4) : clean;
        }

        /// <summary>File nguồn đã nằm sẵn trong thư mục đích thì không cần chép.</summary>
        public static bool IsInsideDirectory(string filePath, string directory)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(directory))
                return false;
            string f = Path.GetFullPath(filePath);
            string d = Path.GetFullPath(directory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            return f.StartsWith(d, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Tên file chưa bị dùng trong thư mục: name.lsp, name_2.lsp, name_3.lsp...</summary>
        public static string NextFreeFileName(string directory, string fileName)
        {
            string stem = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            string candidate = fileName;
            for (int i = 2; File.Exists(Path.Combine(directory, candidate)); i++)
                candidate = stem + "_" + i + ext;
            return candidate;
        }

        /// <summary>
        /// Chép file vào &lt;moduleDir&gt;/&lt;subFolder&gt;/&lt;targetFileName&gt; (byte nguyên vẹn) và trả về đường dẫn tương đối
        /// dạng "lisp/ten.lsp". Nếu file nguồn đã nằm trong thư mục tab thì không chép, chỉ trả đường dẫn tương đối.
        /// </summary>
        public static string CopyIntoModule(string moduleDir, string sourcePath, string subFolder, string targetFileName, bool overwrite)
        {
            if (IsInsideDirectory(sourcePath, moduleDir))
                return MakeRelative(moduleDir, sourcePath);

            string destDir = Path.Combine(moduleDir, subFolder);
            Directory.CreateDirectory(destDir);
            string dest = Path.Combine(destDir, targetFileName);
            File.Copy(sourcePath, dest, overwrite);
            return subFolder + "/" + targetFileName;
        }

        public static string MakeRelative(string moduleDir, string fullPath)
        {
            string d = Path.GetFullPath(moduleDir).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            string f = Path.GetFullPath(fullPath);
            return f.StartsWith(d, StringComparison.OrdinalIgnoreCase)
                ? f.Substring(d.Length).Replace('\\', '/')
                : f.Replace('\\', '/');
        }
    }
}
