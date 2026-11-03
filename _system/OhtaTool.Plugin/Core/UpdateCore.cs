using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Số phiên bản dạng 1.2.3 (chấp nhận tiền tố v). So sánh từng số, thiếu số coi là 0.</summary>
    public static class UpdateVersion
    {
        public const string FileName = "VERSION";

        public static int[] Parse(string text)
        {
            string s = (text ?? string.Empty).Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(1);
            if (s.Length == 0)
                return null;

            string[] parts = s.Split('.');
            var nums = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out nums[i]) || nums[i] < 0)
                    return null;
            }
            return nums;
        }

        public static bool IsValid(string text) => Parse(text) != null;

        /// <summary>Âm nếu a cũ hơn b, 0 nếu bằng, dương nếu a mới hơn. Chuỗi sai định dạng coi là cũ nhất.</summary>
        public static int Compare(string a, string b)
        {
            int[] x = Parse(a);
            int[] y = Parse(b);
            if (x == null || y == null)
                return (x == null ? 0 : 1) - (y == null ? 0 : 1);
            for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
            {
                int xi = i < x.Length ? x[i] : 0;
                int yi = i < y.Length ? y[i] : 0;
                if (xi != yi)
                    return xi.CompareTo(yi);
            }
            return 0;
        }

        public static bool IsNewer(string candidate, string current) => IsValid(candidate) && Compare(candidate, current) > 0;

        /// <summary>Đọc file VERSION ở thư mục gốc; không có / sai thì "0".</summary>
        public static string ReadInstalled(string rootDir)
        {
            try
            {
                string path = Path.Combine(rootDir ?? string.Empty, FileName);
                if (File.Exists(path))
                {
                    string v = File.ReadAllText(path).Trim();
                    if (IsValid(v))
                        return v.TrimStart('v', 'V');
                }
            }
            catch
            {
            }
            return "0";
        }
    }

    /// <summary>
    /// Danh sách file của một bản phát hành (_system/manifest.txt): mỗi dòng "sha256  đường/dẫn/tương/đối".
    /// Trình cập nhật chỉ xoá file có trong manifest bản cũ mà bản mới đã bỏ.
    /// </summary>
    public sealed class ReleaseManifest
    {
        public const string RelativePath = "_system/manifest.txt";

        public Dictionary<string, string> Files { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static ReleaseManifest Parse(string text)
        {
            var m = new ReleaseManifest();
            foreach (string raw in (text ?? string.Empty).Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;
                int sp = line.IndexOf(' ');
                if (sp <= 0)
                    throw new FormatException("Dòng manifest sai: " + line);
                string hash = line.Substring(0, sp).Trim().ToLowerInvariant();
                string path = NormalizePath(line.Substring(sp).Trim());
                if (path == null)
                    throw new FormatException("Đường dẫn không hợp lệ trong manifest: " + line);
                m.Files[path] = hash;
            }
            return m;
        }

        /// <summary>Đọc manifest trong thư mục gốc; không có thì trả manifest rỗng (máy cài tay bản cũ).</summary>
        public static ReleaseManifest LoadFrom(string rootDir)
        {
            string path = Path.Combine(rootDir, RelativePath);
            return File.Exists(path) ? Parse(File.ReadAllText(path, Encoding.UTF8)) : new ReleaseManifest();
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            foreach (var kv in Files.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                sb.Append(kv.Value).Append("  ").Append(kv.Key).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Chuẩn hoá đường dẫn tương đối dùng dấu /. Trả null nếu tuyệt đối, có "..", hoặc rỗng
        /// (chặn zip / manifest ghi ra ngoài thư mục gốc).
        /// </summary>
        public static string NormalizePath(string path)
        {
            string p = (path ?? string.Empty).Replace('\\', '/').Trim();
            while (p.StartsWith("./"))
                p = p.Substring(2);
            if (p.Length == 0 || p.StartsWith("/") || p.Contains(":"))
                return null;
            string[] parts = p.Split('/');
            if (parts.Any(s => s.Length == 0 || s == "." || s == ".."))
                return null;
            return p;
        }

        public static string Sha256OfFile(string path)
        {
            using (var sha = SHA256.Create())
            using (FileStream fs = File.OpenRead(path))
                return ToHex(sha.ComputeHash(fs));
        }

        public static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>Thông tin bản phát hành mới nhất đọc từ GitHub API releases/latest.</summary>
    public sealed class ReleaseInfo
    {
        public string Version { get; set; }
        public string Notes { get; set; }
        public string AssetName { get; set; }
        public string AssetUrl { get; set; }
        /// <summary>sha256 hex của file zip (trường "digest" GitHub cấp), có thể trống.</summary>
        public string AssetSha256 { get; set; }

        /// <summary>Đọc JSON của releases/latest. Chọn asset OHTA_TOOL_v*.zip; không có thì null.</summary>
        public static ReleaseInfo ParseGitHub(string json)
        {
            if (!(JsonLite.Parse(json) is JsonObject root))
                return null;

            string tag = root.GetString("tag_name");
            if (!UpdateVersion.IsValid(tag))
                return null;

            JsonObject asset = (root.Get("assets") as List<object> ?? new List<object>())
                .OfType<JsonObject>()
                .FirstOrDefault(a =>
                {
                    string n = a.GetString("name") ?? string.Empty;
                    return n.StartsWith("OHTA_TOOL_", StringComparison.OrdinalIgnoreCase) &&
                           n.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
                });
            if (asset == null)
                return null;

            string digest = asset.GetString("digest") ?? string.Empty;
            return new ReleaseInfo
            {
                Version = tag.Trim().TrimStart('v', 'V'),
                Notes = (root.GetString("body") ?? string.Empty).Replace("\r\n", "\n").Trim(),
                AssetName = asset.GetString("name"),
                AssetUrl = asset.GetString("browser_download_url"),
                AssetSha256 = digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
                    ? digest.Substring(7).Trim().ToLowerInvariant()
                    : string.Empty
            };
        }
    }

    /// <summary>
    /// Trạng thái kiểm tra cập nhật của máy (%LOCALAPPDATA%\OHTA_TOOL\update\state.json):
    /// ngày kiểm tra gần nhất và bản mới nhất đã thấy, để các lần mở bản vẽ nhắc mà không gọi mạng.
    /// </summary>
    public sealed class UpdateState
    {
        public static string DefaultDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "update");

        public string LastCheckDate { get; set; }
        public ReleaseInfo Latest { get; set; }

        public bool CheckedToday(DateTime now) => LastCheckDate == now.ToString("yyyy-MM-dd");

        public static UpdateState Load(string path)
        {
            var s = new UpdateState();
            try
            {
                if (!File.Exists(path) || !(JsonLite.Parse(File.ReadAllText(path, Encoding.UTF8)) is JsonObject o))
                    return s;
                s.LastCheckDate = o.GetString("lastCheck");
                if (UpdateVersion.IsValid(o.GetString("version")))
                {
                    s.Latest = new ReleaseInfo
                    {
                        Version = o.GetString("version"),
                        Notes = o.GetString("notes"),
                        AssetName = o.GetString("assetName"),
                        AssetUrl = o.GetString("assetUrl"),
                        AssetSha256 = o.GetString("assetSha256")
                    };
                }
            }
            catch
            {
                // File hỏng -> coi như chưa kiểm tra
            }
            return s;
        }

        public void Save(string path)
        {
            var o = new JsonObject();
            o.Set("lastCheck", LastCheckDate ?? string.Empty);
            if (Latest != null)
            {
                o.Set("version", Latest.Version ?? string.Empty);
                o.Set("notes", Latest.Notes ?? string.Empty);
                o.Set("assetName", Latest.AssetName ?? string.Empty);
                o.Set("assetUrl", Latest.AssetUrl ?? string.Empty);
                o.Set("assetSha256", Latest.AssetSha256 ?? string.Empty);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonLite.Serialize(o), new UTF8Encoding(false));
        }
    }
}
