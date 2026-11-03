using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 171 (GĐ 0): file dump chung cho mọi bước — tổng quát hoá cơ chế đã cứu mục 170.
    /// Mỗi bước ghi <c>%TEMP%\gro_&lt;buoc&gt;_dump.txt</c> (ghi đè mỗi lần chạy): các DÒNG,
    /// mỗi dòng = loại (1 chữ / 1 từ) + TAB + các trường cách TAB. Phát lại ngoài CAD:
    /// <c>dotnet run --project ZwCadPlugin.Tests -- phatlai &lt;buoc&gt; &lt;file&gt; [tham so]</c>.
    /// </summary>
    public sealed class DumpBuoc
    {
        public string Buoc { get; }
        private readonly StringBuilder _sb = new StringBuilder();

        public DumpBuoc(string buoc) { Buoc = buoc; }

        public static string DuongDan(string buoc) =>
            Path.Combine(Path.GetTempPath(), "gro_" + GroCongTac.ChuanMa(buoc) + "_dump.txt");

        public DumpBuoc Dong(string loai, params string[] truong)
        {
            _sb.Append(loai);
            foreach (string t in truong) _sb.Append('\t').Append((t ?? "").Replace('\t', ' ').Replace('\n', ' '));
            _sb.Append('\n');
            return this;
        }

        /// <summary>Ghi file; trả đường dẫn hoặc null nếu lỗi (không được làm hỏng lệnh).</summary>
        public string Ghi(string duongDan = null)
        {
            try
            {
                string p = duongDan ?? DuongDan(Buoc);
                File.WriteAllText(p, "#buoc\t" + Buoc + "\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + _sb, new UTF8Encoding(false));
                return p;
            }
            catch { return null; }
        }

        /// <summary>Đọc file dump: danh sách mảng trường (phần tử 0 = loại). Bỏ dòng "#".</summary>
        public static List<string[]> Doc(string duongDan)
        {
            var ds = new List<string[]>();
            foreach (string l in File.ReadAllLines(duongDan, Encoding.UTF8))
            {
                if (l.Length == 0 || l[0] == '#') continue;
                ds.Add(l.Split('\t'));
            }
            return ds;
        }
    }
}
