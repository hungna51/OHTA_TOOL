using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Vật liệu người dùng đã gửi yêu cầu bổ sung nhưng admin chưa duyệt: lưu riêng trong cache máy
    /// (catalog_local.csv) và luôn được cộng lên catalog tải từ Google Sheet, để máy gửi dùng được ngay.
    /// Mỗi lần đồng bộ Sheet, dòng nào Sheet đã có đủ (admin đã duyệt) thì tự xoá.
    /// </summary>
    public static class FnameLocalAdditions
    {
        public const string FileName = "catalog_local.csv";
        private const string Header = "Ngay,M,Loai,Z,T_list";

        public sealed class Entry
        {
            public string Ngay { get; set; } = string.Empty;
            public string M { get; set; } = string.Empty;
            public string Loai { get; set; } = string.Empty;
            public string Z { get; set; } = string.Empty;
            public List<string> Thicknesses { get; set; } = new List<string>();
        }

        public static string PathIn(string cacheDirectory) =>
            Path.Combine(cacheDirectory ?? FnameDataStore.DefaultCacheDirectory, FileName);

        public static List<Entry> Load(string cacheDirectory)
        {
            var list = new List<Entry>();
            string path = PathIn(cacheDirectory);
            if (!File.Exists(path))
                return list;
            try
            {
                foreach (string line in FnameDataStore.SplitLines(File.ReadAllText(path, Encoding.UTF8)))
                {
                    string[] p = line.Split(',');
                    if (p.Length < 5 || p[0].Trim() == "Ngay" || p[1].Trim().Length == 0)
                        continue;
                    list.Add(new Entry
                    {
                        Ngay = p[0].Trim(),
                        M = p[1].Trim(),
                        Loai = p[2].Trim(),
                        Z = p[3].Trim(),
                        Thicknesses = p[4].Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    });
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return list;
        }

        public static void Save(string cacheDirectory, IEnumerable<Entry> entries)
        {
            string dir = cacheDirectory ?? FnameDataStore.DefaultCacheDirectory;
            Directory.CreateDirectory(dir);
            var lines = new List<string> { Header };
            lines.AddRange(entries.Select(e => string.Join(",", new[]
            {
                e.Ngay, Clean(e.M), Clean(e.Loai), Clean(e.Z), string.Join(" ", e.Thicknesses)
            })));
            File.WriteAllText(PathIn(dir), string.Join("\n", lines) + "\n", new UTF8Encoding(true));
        }

        /// <summary>Thêm 1 bổ sung (trùng M thì gộp độ dày, cập nhật Loai / Z nếu có nhập).</summary>
        public static void Add(string cacheDirectory, string m, string loai, string z, IList<string> thicknesses, DateTime? today = null)
        {
            List<Entry> list = Load(cacheDirectory);
            string key = Clean(m);
            Entry e = list.FirstOrDefault(x => string.Equals(x.M, key, StringComparison.OrdinalIgnoreCase));
            if (e == null)
            {
                e = new Entry { M = key };
                list.Add(e);
            }
            e.Ngay = (today ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            e.Loai = Clean(loai);
            if (!string.IsNullOrWhiteSpace(z))
                e.Z = Clean(z);
            e.Thicknesses = FnameCatalogData.MergeThicknesses(e.Thicknesses, thicknesses);
            Save(cacheDirectory, list);
        }

        /// <summary>Cộng các bổ sung lên dòng CSV catalog (dùng FnameCatalogData.Upsert).</summary>
        public static string[] ApplyTo(string[] catalogLines, IEnumerable<Entry> entries)
        {
            string[] lines = catalogLines ?? new string[0];
            foreach (Entry e in entries)
                lines = FnameCatalogData.Upsert(lines, e.M, e.Loai, e.Z, e.Thicknesses);
            return lines;
        }

        /// <summary>
        /// Dọn bổ sung cục bộ khi đồng bộ Sheet hằng ngày (Sheet là dữ liệu gốc):
        /// - catalog Sheet đã có đủ (cùng M, cùng Loai, đủ độ dày) -> admin đã duyệt -> bỏ;
        /// - stillPendingM khác null (đọc được hàng chờ của máy này) mà M không còn trong đó -> đã bị từ chối
        ///   (hoặc đã xử lý) -> bỏ, máy quay về đúng dữ liệu Sheet. stillPendingM = null (mất mạng...) thì giữ.
        /// Trả về số dòng đã bỏ.
        /// </summary>
        public static int Prune(string cacheDirectory, FnameCatalogData sheetCatalog, ICollection<string> stillPendingM = null)
        {
            List<Entry> list = Load(cacheDirectory);
            if (list.Count == 0)
                return 0;
            int before = list.Count;
            list.RemoveAll(e =>
            {
                if (stillPendingM != null && !stillPendingM.Any(m => string.Equals(m?.Trim(), e.M, StringComparison.OrdinalIgnoreCase)))
                    return true;
                FnameMaterialSpec spec = sheetCatalog?.FindMaterial(e.M);
                return spec != null
                    && string.Equals(spec.Loai ?? string.Empty, e.Loai, StringComparison.OrdinalIgnoreCase)
                    && e.Thicknesses.All(t => FnameCatalogData.ContainsThickness(spec.Thicknesses, t));
            });
            if (list.Count != before)
                Save(cacheDirectory, list);
            return before - list.Count;
        }

        private static string Clean(string v) => (v ?? string.Empty).Replace(",", " ").Trim();
    }
}
