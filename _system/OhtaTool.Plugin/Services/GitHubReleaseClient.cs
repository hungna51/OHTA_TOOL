using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Đọc bản phát hành mới nhất trên GitHub và tải gói zip về (ADR 0004). Repo public nên không cần token.
    /// Mọi hàm gọi mạng -> chạy ở luồng nền.
    /// </summary>
    public static class GitHubReleaseClient
    {
        public const string Repo = "hungna51/OHTA_TOOL";
        public static string LatestApiUrl => "https://api.github.com/repos/" + Repo + "/releases/latest";

        private static HttpClient NewClient(int timeoutSeconds)
        {
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
            // GitHub API bắt buộc có User-Agent
            http.DefaultRequestHeaders.UserAgent.ParseAdd("OHTA_TOOL-Updater");
            return http;
        }

        /// <summary>Bản mới nhất; null nếu repo chưa có Release nào hợp lệ. Lỗi mạng thì ném ngoại lệ.</summary>
        public static ReleaseInfo FetchLatest()
        {
            using (HttpClient http = NewClient(15))
            {
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                HttpResponseMessage resp = http.GetAsync(LatestApiUrl).GetAwaiter().GetResult();
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                resp.EnsureSuccessStatusCode();
                return ReleaseInfo.ParseGitHub(resp.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        }

        /// <summary>
        /// Tải zip về %TEMP%, kiểm tra sha256 (nếu GitHub có cấp), giải nén ra thư mục tạm. Trả về thư mục đã giải nén.
        /// Người gọi xoá thư mục cha (workDir) khi xong.
        /// </summary>
        public static string DownloadAndExtract(ReleaseInfo release, string workDir)
        {
            if (string.IsNullOrWhiteSpace(release?.AssetUrl))
                throw new InvalidOperationException("Bản phát hành không có file zip.");

            Directory.CreateDirectory(workDir);
            string zipPath = Path.Combine(workDir, "package.zip");
            using (HttpClient http = NewClient(300))
            using (Stream s = http.GetStreamAsync(release.AssetUrl).GetAwaiter().GetResult())
            using (FileStream fs = File.Create(zipPath))
                s.CopyTo(fs);

            if (!string.IsNullOrEmpty(release.AssetSha256) &&
                !string.Equals(ReleaseManifest.Sha256OfFile(zipPath), release.AssetSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("File tải về bị lỗi (sai sha256). Hãy thử lại.");

            string outDir = Path.Combine(workDir, "extracted");
            Directory.CreateDirectory(outDir);
            using (ZipArchive zip = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    if (e.FullName.EndsWith("/") || e.FullName.EndsWith("\\"))
                        continue;
                    string rel = ReleaseManifest.NormalizePath(e.FullName);
                    if (rel == null)
                        throw new InvalidDataException("Gói cập nhật có đường dẫn không hợp lệ: " + e.FullName);
                    string dst = Path.Combine(outDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));
                    e.ExtractToFile(dst, true);
                }
            }

            // Zip đóng gói trong thư mục OHTA_TOOL/ (để cài tay giải nén cho gọn)
            string inner = Path.Combine(outDir, "OHTA_TOOL");
            return File.Exists(Path.Combine(inner, ReleaseManifest.RelativePath)) ? inner : outDir;
        }
    }
}
