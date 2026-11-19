using System;
using System.IO;

namespace CsvTool.Core
{
    public static class MaDon
    {
        /// <summary>Mã đơn = tên file bỏ đuôi và đúng 1 hậu tố -a / -s (không phân biệt hoa thường).</summary>
        public static string TuTenFile(string dwgName)
        {
            string s = Path.GetFileNameWithoutExtension(dwgName ?? "");
            if (s.EndsWith("-a", StringComparison.OrdinalIgnoreCase) || s.EndsWith("-s", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - 2);
            return s;
        }
    }
}
