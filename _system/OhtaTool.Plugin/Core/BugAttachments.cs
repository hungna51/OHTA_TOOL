using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Nén file đính kèm báo lỗi (dwg, CSV, gro_thong_tin.txt) thành 1 zip, chặn khi quá 25 MB.</summary>
    public static class BugAttachments
    {
        public const long MaxZipBytes = 25L * 1024 * 1024;

        /// <summary>
        /// (zip, null) khi được; (null, câu lỗi) khi quá giới hạn hoặc không đọc được; (null, null) khi không có file nào.
        /// File không tồn tại bị bỏ qua. Đọc chung với CAD (bản vẽ đang mở) bằng FileShare.ReadWrite.
        /// </summary>
        public static (byte[] zip, string error) Zip(IList<string> paths, long maxBytes = MaxZipBytes)
        {
            var files = new List<string>();
            if (paths != null)
                foreach (string p in paths)
                    if (!string.IsNullOrEmpty(p) && File.Exists(p))
                        files.Add(p);
            if (files.Count == 0)
                return (null, null);

            try
            {
                byte[] zip;
                using (var ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (string path in files)
                        {
                            ZipArchiveEntry entry = archive.CreateEntry(UniqueName(Path.GetFileName(path), used), CompressionLevel.Optimal);
                            using (var src = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            using (var dst = entry.Open())
                                src.CopyTo(dst);
                        }
                    }
                    zip = ms.ToArray();
                }
                if (zip.LongLength > maxBytes)
                {
                    string mb = (zip.LongLength / 1024.0 / 1024.0).ToString("0.0", CultureInfo.InvariantCulture);
                    return (null, "File đính kèm sau khi nén " + mb + " MB, quá giới hạn 25 MB. Hãy gửi tay qua Zalo/OneDrive.");
                }
                return (zip, null);
            }
            catch (Exception ex)
            {
                return (null, "Không đọc được file đính kèm (" + ex.GetBaseException().Message + ").");
            }
        }

        private static string UniqueName(string name, HashSet<string> used)
        {
            string stem = Path.GetFileNameWithoutExtension(name);
            string ext = Path.GetExtension(name);
            string candidate = name;
            for (int i = 2; !used.Add(candidate); i++)
                candidate = stem + "_" + i + ext;
            return candidate;
        }
    }

    /// <summary>Hàng chờ báo lỗi chưa gửi được (%LOCALAPPDATA%\OHTA_TOOL\bug_queue\&lt;id&gt;.json).</summary>
    public static class BugQueue
    {
        public static string Directory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "bug_queue");

        private static string PathOf(string id) => Path.Combine(Directory, id + ".json");

        public static void Enqueue(BugReport r)
        {
            if (r == null || string.IsNullOrEmpty(r.Id))
                return;
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(PathOf(r.Id), r.ToJson(), System.Text.Encoding.UTF8);
        }

        public static List<BugReport> Pending()
        {
            var list = new List<BugReport>();
            if (!System.IO.Directory.Exists(Directory))
                return list;
            foreach (string f in System.IO.Directory.GetFiles(Directory, "*.json"))
            {
                try
                {
                    BugReport r = BugReport.FromJson(File.ReadAllText(f, System.Text.Encoding.UTF8));
                    if (r != null)
                        list.Add(r);
                }
                catch
                {
                }
            }
            list.Sort((x, y) => x.ThoiGian.CompareTo(y.ThoiGian));
            return list;
        }

        public static void Remove(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id) && File.Exists(PathOf(id)))
                    File.Delete(PathOf(id));
            }
            catch
            {
            }
        }
    }
}
