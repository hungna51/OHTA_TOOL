using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Bộ quét các thư mục con trong OHTA_TOOL/ để ánh xạ 1-1 thành các Tab trên PaletteSet.
    /// </summary>
    public static class ModuleDiscovery
    {
        private static readonly string[] DefaultPaletteColors = new[]
        {
            "#1E88E5",
            "#2E7D32",
            "#EF6C00",
            "#6A1B9A",
            "#00838F",
            "#C62828",
            "#455A64"
        };

        /// <summary>
        /// Kiểm tra một tên thư mục con trực tiếp trong OHTA_TOOL/ có phải thư mục hệ thống cần bỏ qua không.
        /// Bỏ qua: rules, bin, obj, các thư mục bắt đầu bằng "_" hoặc ".".
        /// </summary>
        public static bool ShouldIgnoreFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return true;

            string trimmed = folderName.Trim();
            if (trimmed.StartsWith("_", StringComparison.Ordinal) ||
                trimmed.StartsWith(".", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(trimmed, "rules", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "obj", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tìm thư mục gốc OHTA_TOOL/ từ đường dẫn ưu tiên (Registry) hoặc đi ngược từ vị trí file OHTATOOL.dll.
        /// </summary>
        public static string ResolveRootDirectory(string executingAssemblyPath, string overrideRootDir = null)
        {
            if (IsValidRootDirectory(overrideRootDir))
                return Path.GetFullPath(overrideRootDir);

            if (!string.IsNullOrWhiteSpace(executingAssemblyPath))
            {
                string current = File.Exists(executingAssemblyPath)
                    ? Path.GetDirectoryName(executingAssemblyPath)
                    : executingAssemblyPath;

                for (int depth = 0; depth < 7 && !string.IsNullOrWhiteSpace(current); depth++)
                {
                    if (IsValidRootDirectory(current))
                        return Path.GetFullPath(current);

                    DirectoryInfo parent = Directory.GetParent(current);
                    current = parent != null ? parent.FullName : null;
                }
            }

            return null;
        }

        public static bool IsValidRootDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return false;

            return File.Exists(Path.Combine(dir, "OHTA_Loader.lsp")) ||
                   (File.Exists(Path.Combine(dir, "GEMINI.md")) && Directory.Exists(Path.Combine(dir, "rules"))) ||
                   Directory.Exists(Path.Combine(dir, "_system"));
        }

        /// <summary>
        /// Quét toàn bộ thư mục con hợp lệ trong rootDir và trả về danh sách ModuleManifest đã chuẩn hóa & sắp xếp.
        /// Nếu includeAdminOnly = false thì ẩn các tab có adminOnly = true trên máy thường.
        /// </summary>
        public static List<ModuleManifest> DiscoverModules(string rootDir, bool includeAdminOnly = true)
        {
            var results = new List<ModuleManifest>();
            if (string.IsNullOrWhiteSpace(rootDir) || !Directory.Exists(rootDir))
                return results;

            string[] subDirs = Directory.GetDirectories(rootDir);
            Array.Sort(subDirs, StringComparer.OrdinalIgnoreCase);

            int colorIndex = 0;
            foreach (string subDir in subDirs)
            {
                string folderName = Path.GetFileName(subDir);
                if (ShouldIgnoreFolder(folderName))
                    continue;

                string fallbackColor = DefaultPaletteColors[colorIndex % DefaultPaletteColors.Length];
                colorIndex++;

                ModuleManifest manifest = LoadModuleFromDirectory(subDir, folderName, fallbackColor);
                if (manifest != null && manifest.IsEnabled && (includeAdminOnly || !manifest.IsAdminOnly))
                {
                    results.Add(manifest);
                }
            }

            return results
                .OrderBy(m => m.EffectiveOrder)
                .ThenBy(m => m.FolderName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static ModuleManifest LoadModuleFromDirectory(string subDir, string folderName, string fallbackColor)
        {
            string manifestPath = Path.Combine(subDir, "module.json");
            string readmePath = FindFirstMarkdownFile(subDir);
            string rulesDir = Path.Combine(subDir, "rules");

            ModuleManifest manifest = null;
            string loadWarning = null;
            bool hasManifest = File.Exists(manifestPath);

            if (hasManifest)
            {
                try
                {
                    byte[] rawBytes = File.ReadAllBytes(manifestPath);
                    string jsonText = DecodeUtf8StripBom(rawBytes);
                    byte[] cleanBytes = Encoding.UTF8.GetBytes(jsonText);
                    using (var ms = new MemoryStream(cleanBytes))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(ModuleManifest));
                        manifest = serializer.ReadObject(ms) as ModuleManifest;
                    }
                }
                catch (Exception ex)
                {
                    loadWarning = "Lỗi đọc module.json: " + ex.Message;
                    manifest = new ModuleManifest();
                }
            }
            else
            {
                manifest = new ModuleManifest();
            }

            if (manifest == null)
                manifest = new ModuleManifest();

            manifest.FolderName = folderName;
            manifest.DirectoryPath = subDir;
            manifest.ReadmePath = readmePath;
            manifest.RulesDirectoryPath = rulesDir;
            manifest.HasManifestFile = hasManifest;
            manifest.LoadWarning = loadWarning;

            string formattedFolder = FormatFolderNameForDisplay(folderName);
            string readmeHeading = ExtractFirstHeading(readmePath);

            if (string.IsNullOrWhiteSpace(manifest.Id))
                manifest.Id = folderName.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(manifest.TabTitle))
                manifest.TabTitle = formattedFolder;

            if (string.IsNullOrWhiteSpace(manifest.Title))
                manifest.Title = !string.IsNullOrWhiteSpace(readmeHeading) ? readmeHeading : manifest.TabTitle;

            if (string.IsNullOrWhiteSpace(manifest.Description))
            {
                manifest.Description = hasManifest
                    ? "Nhóm công cụ thuộc thư mục " + folderName + "."
                    : "Thư mục mục con mới tạo (chưa có module.json). Sẵn sàng bổ sung công cụ.";
            }

            if (string.IsNullOrWhiteSpace(manifest.AccentColor) || !manifest.AccentColor.Trim().StartsWith("#", StringComparison.Ordinal))
                manifest.AccentColor = fallbackColor;
            else
                manifest.AccentColor = manifest.AccentColor.Trim();

            if (manifest.StartupLisp == null)
                manifest.StartupLisp = new List<string>();

            if (manifest.Tools == null)
                manifest.Tools = new List<ToolItem>();

            if (manifest.Groups == null)
                manifest.Groups = new List<string>();

            foreach (ToolItem tool in manifest.Tools)
            {
                NormalizeToolItem(tool);
            }

            return manifest;
        }

        public static string FormatFolderNameForDisplay(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return "Module";

            string s = folderName.Trim();
            int idx = s.IndexOf('_');
            if (idx > 0 && idx <= 3 && s.Substring(0, idx).All(char.IsDigit))
            {
                string prefix = s.Substring(0, idx);
                string rest = s.Substring(idx + 1).Replace('_', ' ').Trim();
                if (!string.IsNullOrEmpty(rest))
                    return prefix + ". " + rest;
            }

            return s.Replace('_', ' ');
        }

        private static void NormalizeToolItem(ToolItem tool)
        {
            if (tool == null)
                return;

            tool.Command = (tool.Command ?? string.Empty).Trim();
            tool.Name = string.IsNullOrWhiteSpace(tool.Name)
                ? (!string.IsNullOrEmpty(tool.Command) ? tool.Command : "Công cụ chưa đặt tên")
                : tool.Name.Trim();

            // Trống = chưa có nhóm -> hiện ở nhóm "Khác" (ModuleGrouping)
            tool.Category = (tool.Category ?? string.Empty).Trim();
            tool.Icon = (tool.Icon ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(tool.Badge))
            {
                string src = !string.IsNullOrEmpty(tool.Command) ? tool.Command : tool.Name;
                string clean = new string(src.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
                tool.Badge = clean.Length > 3 ? clean.Substring(0, 3) : (clean.Length > 0 ? clean : "TL");
            }
            else
            {
                string b = tool.Badge.Trim().ToUpperInvariant();
                tool.Badge = b.Length > 4 ? b.Substring(0, 4) : b;
            }

            tool.Description = (tool.Description ?? string.Empty).Trim();
            tool.LispFile = (tool.LispFile ?? string.Empty).Trim();
        }

        private static string FindFirstMarkdownFile(string subDir)
        {
            string readme = Path.Combine(subDir, "README.md");
            if (File.Exists(readme))
                return readme;

            string[] mdFiles = Directory.GetFiles(subDir, "*.md", SearchOption.TopDirectoryOnly);
            if (mdFiles.Length > 0)
            {
                Array.Sort(mdFiles, StringComparer.OrdinalIgnoreCase);
                return mdFiles[0];
            }

            return readme;
        }

        private static string ExtractFirstHeading(string mdPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mdPath) || !File.Exists(mdPath))
                    return null;

                foreach (string line in File.ReadLines(mdPath, Encoding.UTF8))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("# ", StringComparison.Ordinal))
                    {
                        string heading = trimmed.Substring(2).Trim();
                        int parenIdx = heading.IndexOf("(`", StringComparison.Ordinal);
                        if (parenIdx > 0)
                            heading = heading.Substring(0, parenIdx).Trim();
                        if (!string.IsNullOrEmpty(heading))
                            return heading;
                    }
                }
            }
            catch
            {
                // Bỏ qua nếu file đang khóa hoặc lỗi đọc
            }
            return null;
        }

        private static string DecodeUtf8StripBom(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return "{}";

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

            return Encoding.UTF8.GetString(bytes);
        }
    }
}

