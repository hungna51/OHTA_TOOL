using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Gộp module.json khi cập nhật (ADR 0004): bản phát hành thắng ở nút nó có; nút máy tự thêm (nút ＋)
    /// được giữ; nút máy đã gỡ không hiện lại; groups và startupLisp lấy hợp; tiêu đề / màu tab máy đã đổi thì giữ.
    /// baseJson = module.json của bản phát hành lần cài trước (null nếu chưa có).
    /// </summary>
    public static class ModuleJsonMerge
    {
        private static readonly string[] HeaderKeys = { "tabTitle", "title", "description", "accentColor" };

        public static JsonObject Merge(JsonObject baseJson, JsonObject local, JsonObject release)
        {
            if (local == null)
                return Clone(release);
            if (baseJson != null && Same(baseJson, local))
                return Clone(release);

            JsonObject result = Clone(release);

            if (baseJson != null)
            {
                foreach (string key in HeaderKeys)
                {
                    object lv = local.Get(key);
                    if (lv != null && !Same(lv, baseJson.Get(key)))
                        result.Set(key, Clone(lv));
                }
            }

            var releaseIds = IdSet(release);
            var baseIds = baseJson != null ? IdSet(baseJson) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var localIds = IdSet(local);
            List<object> tools = ModuleJsonEditor.GetList(result, "tools");

            // Nút có ở bản trước + bản mới nhưng máy đã gỡ -> không hiện lại (trừ nút locked)
            tools.RemoveAll(o => o is JsonObject t && baseIds.Contains(Id(t)) && !localIds.Contains(Id(t)) &&
                                 !(t.Get("locked") is bool b && b));

            // Nút máy tự thêm (không có ở bản trước lẫn bản mới) -> giữ, nối cuối theo thứ tự trên máy
            foreach (JsonObject t in ModuleJsonEditor.Tools(local))
            {
                string id = Id(t);
                if (id.Length > 0 && !releaseIds.Contains(id) && !baseIds.Contains(id))
                    tools.Add(Clone(t));
            }

            MergeStringList(result, local, "groups");
            MergeStringList(result, local, "startupLisp");
            return result;
        }

        private static void MergeStringList(JsonObject result, JsonObject local, string key)
        {
            if (!(local.Get(key) is List<object> localList))
                return;
            List<object> list = result.Get(key) as List<object>;
            if (list == null)
            {
                list = new List<object>();
                result.Set(key, list);
            }
            foreach (string s in localList.OfType<string>())
            {
                if (!list.OfType<string>().Contains(s, StringComparer.OrdinalIgnoreCase))
                    list.Add(s);
            }
        }

        private static HashSet<string> IdSet(JsonObject root) =>
            new HashSet<string>(ModuleJsonEditor.Tools(root).Select(Id).Where(s => s.Length > 0), StringComparer.OrdinalIgnoreCase);

        private static string Id(JsonObject t) => (t.GetString("id") ?? string.Empty).Trim();

        private static bool Same(object a, object b) => JsonLite.Serialize(a) == JsonLite.Serialize(b);

        private static T Clone<T>(T value) => value == null ? value : (T)JsonLite.Parse(JsonLite.Serialize(value));
    }

    /// <summary>Kết quả một lần cài cập nhật / quay lại.</summary>
    public sealed class UpdateReport
    {
        public int Written { get; set; }
        public int Deleted { get; set; }
        public int Merged { get; set; }
        /// <summary>Số file đang bị CAD khoá (DLL) đã đổi tên .old_* rồi đặt bản mới vào -> cần khởi động lại CAD.</summary>
        public int RenamedLocked { get; set; }
    }

    /// <summary>
    /// Cài bản phát hành đã giải nén vào thư mục gốc OHTA_TOOL (ADR 0004). Không gọi mạng, không đụng CAD.
    /// Thứ tự: kiểm tra hash từng file → sao lưu các file sắp bị đụng → ghi / gộp / xoá → ghi manifest + VERSION sau cùng.
    /// </summary>
    public static class UpdateInstaller
    {
        public const string BackupAddedEntry = "__ohta_added.txt";
        public const string BackupVersionEntry = "__ohta_version.txt";

        /// <summary>Thư mục giữ module.json của bản phát hành lần cài trước (làm gốc để gộp).</summary>
        public static string BaseStoreDirectory(string stateDir) => Path.Combine(stateDir, "base");

        /// <summary>Kiểm tra mọi file trong manifest của bản giải nén khớp sha256. Trả về manifest; sai thì ném lỗi.</summary>
        public static ReleaseManifest VerifyExtracted(string extractedDir)
        {
            string mPath = Path.Combine(extractedDir, ReleaseManifest.RelativePath);
            if (!File.Exists(mPath))
                throw new InvalidDataException("Gói cập nhật thiếu " + ReleaseManifest.RelativePath + ".");
            ReleaseManifest m = ReleaseManifest.Parse(File.ReadAllText(mPath, Encoding.UTF8));
            if (!m.Files.ContainsKey(UpdateVersion.FileName))
                throw new InvalidDataException("Gói cập nhật thiếu file VERSION.");

            foreach (var kv in m.Files)
            {
                string f = Path.Combine(extractedDir, kv.Key);
                if (!File.Exists(f))
                    throw new InvalidDataException("Gói cập nhật thiếu file: " + kv.Key);
                if (!string.Equals(ReleaseManifest.Sha256OfFile(f), kv.Value, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Sai mã kiểm tra (sha256) file: " + kv.Key);
            }
            return m;
        }

        public static UpdateReport Install(string rootDir, string extractedDir, string backupZipPath, string baseStoreDir)
        {
            ReleaseManifest newM = VerifyExtracted(extractedDir);
            ReleaseManifest oldM = ReleaseManifest.LoadFrom(rootDir);
            string oldVersion = UpdateVersion.ReadInstalled(rootDir);

            // File bản cũ có mà bản mới bỏ: chỉ xoá khi máy chưa sửa
            var toDelete = oldM.Files
                .Where(kv => !newM.Files.ContainsKey(kv.Key))
                .Where(kv => File.Exists(Path.Combine(rootDir, kv.Key)) &&
                             string.Equals(ReleaseManifest.Sha256OfFile(Path.Combine(rootDir, kv.Key)), kv.Value, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToList();

            // manifest + VERSION ghi sau cùng: hỏng giữa chừng thì máy vẫn nhận là bản cũ
            var last = new[] { ReleaseManifest.RelativePath, UpdateVersion.FileName };
            List<string> toWrite = newM.Files.Keys
                .Where(p => !last.Contains(p, StringComparer.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Concat(last)
                .ToList();

            WriteBackup(rootDir, backupZipPath, toWrite.Concat(toDelete), oldVersion);

            var report = new UpdateReport();
            Directory.CreateDirectory(baseStoreDir);
            foreach (string rel in toWrite)
            {
                string src = Path.Combine(extractedDir, rel);
                string dst = Path.Combine(rootDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));

                string moduleFolder = ModuleJsonFolder(rel);
                if (moduleFolder != null)
                {
                    string basePath = Path.Combine(baseStoreDir, moduleFolder + ".module.json");
                    if (File.Exists(dst))
                    {
                        JsonObject merged = ModuleJsonMerge.Merge(
                            File.Exists(basePath) ? ReadJson(basePath) : null, ReadJson(dst), ReadJson(src));
                        File.WriteAllText(dst, JsonLite.Serialize(merged), new UTF8Encoding(false));
                        report.Merged++;
                    }
                    else
                    {
                        File.Copy(src, dst, true);
                    }
                    File.Copy(src, basePath, true);
                    report.Written++;
                    continue;
                }

                if (ReplaceFile(src, dst))
                    report.RenamedLocked++;
                report.Written++;
            }

            foreach (string rel in toDelete)
            {
                try
                {
                    File.Delete(Path.Combine(rootDir, rel));
                    report.Deleted++;
                }
                catch
                {
                    // File đang khoá: để lại, lần sau không còn trong manifest nên không ai đụng
                }
            }
            return report;
        }

        /// <summary>Quay lại bản trước từ file sao lưu: chép lại các file cũ, xoá file mà lần cập nhật đã thêm.</summary>
        public static UpdateReport Rollback(string rootDir, string backupZipPath)
        {
            var report = new UpdateReport();
            string tmp = Path.Combine(Path.GetTempPath(), "ohta_rollback_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tmp);
                var restore = new List<string>();
                var added = new List<string>();
                using (ZipArchive zip = ZipFile.OpenRead(backupZipPath))
                {
                    foreach (ZipArchiveEntry e in zip.Entries)
                    {
                        if (e.FullName == BackupVersionEntry || e.FullName.EndsWith("/"))
                            continue;
                        if (e.FullName == BackupAddedEntry)
                        {
                            using (var r = new StreamReader(e.Open(), Encoding.UTF8))
                                added.AddRange(r.ReadToEnd().Split('\n').Select(s => ReleaseManifest.NormalizePath(s.Trim())).Where(s => s != null));
                            continue;
                        }
                        string rel = ReleaseManifest.NormalizePath(e.FullName);
                        if (rel == null)
                            continue;
                        string f = Path.Combine(tmp, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(f));
                        e.ExtractToFile(f, true);
                        restore.Add(rel);
                    }
                }

                // VERSION + manifest chép sau cùng (giống lúc cài)
                foreach (string rel in restore.OrderBy(r => IsLast(r) ? 1 : 0))
                {
                    string dst = Path.Combine(rootDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));
                    if (ReplaceFile(Path.Combine(tmp, rel), dst))
                        report.RenamedLocked++;
                    report.Written++;
                }
                foreach (string rel in added)
                {
                    try
                    {
                        string f = Path.Combine(rootDir, rel);
                        if (File.Exists(f))
                        {
                            File.Delete(f);
                            report.Deleted++;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
            return report;
        }

        /// <summary>Đọc phiên bản ghi trong file sao lưu; không đọc được thì null.</summary>
        public static string ReadBackupVersion(string backupZipPath)
        {
            try
            {
                using (ZipArchive zip = ZipFile.OpenRead(backupZipPath))
                {
                    ZipArchiveEntry e = zip.GetEntry(BackupVersionEntry);
                    if (e == null)
                        return null;
                    using (var r = new StreamReader(e.Open(), Encoding.UTF8))
                        return r.ReadToEnd().Trim();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Xoá các file *.old_* do lần cập nhật trước đổi tên (CAD đã đóng thì xoá được).</summary>
        public static int CleanupOldFiles(string rootDir)
        {
            int n = 0;
            foreach (string sub in new[] { "_system", "03_Guro" })
            {
                string dir = Path.Combine(rootDir, sub);
                if (!Directory.Exists(dir))
                    continue;
                foreach (string f in Directory.GetFiles(dir, "*.old_*", SearchOption.AllDirectories))
                {
                    try { File.Delete(f); n++; } catch { }
                }
            }
            return n;
        }

        /// <summary>
        /// Chép đè src lên dst. dst đang bị khoá (DLL CAD đang dùng) thì đổi tên dst thành *.old_&lt;giờ&gt; rồi chép.
        /// Trả về true nếu phải đổi tên.
        /// </summary>
        public static bool ReplaceFile(string src, string dst)
        {
            try
            {
                File.Copy(src, dst, true);
                return false;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                if (!File.Exists(dst))
                    throw;
                string old = Path.Combine(Path.GetDirectoryName(dst),
                    Path.GetFileNameWithoutExtension(dst) + ".old_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(dst));
                File.Move(dst, old);
                File.Copy(src, dst, true);
                return true;
            }
        }

        private static void WriteBackup(string rootDir, string backupZipPath, IEnumerable<string> touched, string oldVersion)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backupZipPath));
            string tmp = backupZipPath + ".tmp";
            if (File.Exists(tmp))
                File.Delete(tmp);

            var added = new List<string>();
            using (ZipArchive zip = ZipFile.Open(tmp, ZipArchiveMode.Create))
            {
                foreach (string rel in touched.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string f = Path.Combine(rootDir, rel);
                    if (File.Exists(f))
                        zip.CreateEntryFromFile(f, rel, CompressionLevel.Optimal);
                    else
                        added.Add(rel);
                }
                WriteEntry(zip, BackupAddedEntry, string.Join("\n", added));
                WriteEntry(zip, BackupVersionEntry, oldVersion ?? "0");
            }

            if (File.Exists(backupZipPath))
                File.Delete(backupZipPath);
            File.Move(tmp, backupZipPath);
        }

        private static void WriteEntry(ZipArchive zip, string name, string text)
        {
            using (var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)))
                w.Write(text);
        }

        /// <summary>"03_Guro/module.json" -> "03_Guro"; file khác -> null.</summary>
        private static string ModuleJsonFolder(string rel)
        {
            string[] parts = rel.Split('/');
            return parts.Length == 2 && string.Equals(parts[1], ModuleJsonEditor.FileName, StringComparison.OrdinalIgnoreCase)
                ? parts[0]
                : null;
        }

        private static JsonObject ReadJson(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            return JsonLite.Parse(Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset)) as JsonObject ?? new JsonObject();
        }

        private static bool IsLast(string rel) =>
            string.Equals(rel, ReleaseManifest.RelativePath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rel, UpdateVersion.FileName, StringComparison.OrdinalIgnoreCase);
    }
}
