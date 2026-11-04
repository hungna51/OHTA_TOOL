using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Dữ liệu cho tab Info (tab dựng sẵn trong OHTATOOL.dll, luôn nằm cuối).
    /// Thuần C#, không phụ thuộc SDK CAD để kiểm thử độc lập.
    /// </summary>
    public static class ToolInfoCore
    {
        public const string Developer = "A Hùng";
        public const string RepoUrl = "https://github.com/hungna51/OHTA_TOOL";
        public const string InfoTabId = "ohta_info";
        public const string InfoTabTitle = "Info";
        public const string InfoAccentColor = "#455A64";
        public const string ChangelogFileName = "CHANGELOG.md";

        /// <summary>Số file lisp khác nhau mà các nút của tab dùng (lispFile), không tính file không gắn nút.</summary>
        public static int CountLispFiles(ModuleManifest manifest)
        {
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (manifest?.Tools != null)
            {
                foreach (ToolItem t in manifest.Tools)
                {
                    string f = (t?.LispFile ?? string.Empty).Trim().Replace('\\', '/');
                    while (f.StartsWith("./", StringComparison.Ordinal))
                        f = f.Substring(2);
                    if (f.Length > 0)
                        files.Add(f);
                }
            }
            return files.Count;
        }

        /// <summary>Số nút lệnh có lệnh CAD.</summary>
        public static int CountCommands(ModuleManifest manifest)
        {
            int n = 0;
            if (manifest?.Tools != null)
            {
                foreach (ToolItem t in manifest.Tools)
                    if (!string.IsNullOrWhiteSpace(t?.Command))
                        n++;
            }
            return n;
        }

        /// <summary>"2 lisp · 3 lệnh".</summary>
        public static string DescribeContents(ModuleManifest manifest) =>
            CountLispFiles(manifest) + " lisp · " + CountCommands(manifest) + " lệnh";

        /// <summary>
        /// Mục đầu tiên "## vX.Y.Z" của CHANGELOG.md: trả về nội dung (bỏ dòng tiêu đề), version ra qua out.
        /// Không có mục nào -> "".
        /// </summary>
        public static string LatestChangelogEntry(string changelogText, out string version)
        {
            version = string.Empty;
            if (string.IsNullOrEmpty(changelogText))
                return string.Empty;

            var body = new StringBuilder();
            bool inside = false;
            foreach (string raw in changelogText.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    if (inside)
                        break;
                    string head = line.Substring(3).Trim();
                    if (head.StartsWith("v", StringComparison.OrdinalIgnoreCase) && UpdateVersion.IsValid(head.Substring(1)))
                    {
                        inside = true;
                        version = head.Substring(1);
                    }
                    continue;
                }
                if (inside)
                    body.Append(line).Append('\n');
            }
            return body.ToString().Trim();
        }

        public static string ReadLatestChangelogEntry(string rootDir, out string version)
        {
            version = string.Empty;
            try
            {
                string path = Path.Combine(rootDir ?? string.Empty, ChangelogFileName);
                return File.Exists(path) ? LatestChangelogEntry(File.ReadAllText(path, Encoding.UTF8), out version) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>Một dòng trạng thái cập nhật cho tab Info, từ state.json của bộ tự cập nhật (ADR 0004).</summary>
        public static string DescribeUpdateStatus(UpdateState state, string currentVersion)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.LastCheckDate))
                return "Chưa kiểm tra bản mới lần nào.";

            string prefix = "Kiểm tra gần nhất " + state.LastCheckDate + ": ";
            if (state.Latest == null)
                return prefix + "chưa có bản phát hành trên GitHub.";
            if (UpdateVersion.IsNewer(state.Latest.Version, currentVersion))
                return prefix + "có bản mới v" + state.Latest.Version + ".";
            return prefix + "đang dùng bản mới nhất.";
        }
    }
}
