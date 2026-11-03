using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.Win32;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Tải các bảng dữ liệu Fname từ Google Sheet (xuất CSV công khai theo link) về cache của máy.
    /// Cấu hình: 01_Fname/data/google_sheet.ini (spreadsheet_id + gid của từng tab).
    /// </summary>
    public static class FnameDataSync
    {
        private const string RegSubKey = @"Software\OFT_Lisp\OhtaTool\Fname";
        private static bool _autoTriedThisSession;

        public static string ConfigPath(string moduleDirectory) =>
            Path.Combine(moduleDirectory ?? string.Empty, "data", "google_sheet.ini");

        /// <summary>Ngày tải Google Sheet thành công gần nhất (yyyy-MM-dd), rỗng nếu chưa từng tải.</summary>
        public static string LastSyncDate()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegSubKey, writable: false))
                    return key?.GetValue("LastDataSync") as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Lần mở tool đầu tiên trong ngày (mỗi phiên CAD chỉ thử 1 lần, lỗi mạng thì để hôm sau / bấm tay).
        /// </summary>
        public static bool ShouldAutoSync(string moduleDirectory)
        {
            if (_autoTriedThisSession || string.IsNullOrEmpty(ReadConfig(moduleDirectory).Get("spreadsheet_id")))
                return false;
            _autoTriedThisSession = true;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegSubKey, writable: false))
                return (key?.GetValue("LastDataSync") as string) != DateTime.Today.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Tải đủ các bảng (bảng tùy chọn chưa khai gid thì bỏ qua), kiểm tra hợp lệ hết rồi mới ghi cache (tất cả hoặc không gì cả). Chạy ở luồng nền.
        /// </summary>
        public static (bool ok, string message) Run(string moduleDirectory)
        {
            Dictionary<string, string> cfg = ReadConfig(moduleDirectory);
            string id = cfg.Get("spreadsheet_id");
            if (string.IsNullOrEmpty(id))
                return (false, "Chưa cấu hình spreadsheet_id trong " + ConfigPath(moduleDirectory));

            var downloaded = new Dictionary<string, string>();
            try
            {
#if NETFRAMEWORK
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
#endif
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) })
                {
                    foreach (string table in FnameDataStore.TableNames)
                    {
                        string gid = cfg.Get(table);
                        if (string.IsNullOrEmpty(gid) && FnameDataStore.IsOptionalTable(table))
                            continue;
                        if (string.IsNullOrEmpty(gid))
                            return (false, $"Chưa cấu hình gid cho bảng '{table}'.");

                        string url = $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={gid}";
                        byte[] bytes = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
                        string text = Encoding.UTF8.GetString(bytes);
                        if (!FnameDataStore.Validate(table, FnameDataStore.SplitLines(text), out string err))
                            return (false, $"Bảng '{table}' {err}. Giữ nguyên dữ liệu cũ.");
                        downloaded[table] = text;
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, "Không tải được Google Sheet (" + ex.GetBaseException().Message + "). Giữ nguyên dữ liệu cũ.");
            }

            try
            {
                // Ghi .tmp đủ các bảng trước rồi mới thay, để không bao giờ còn cache nửa cũ nửa mới.
                string dir = FnameDataStore.DefaultCacheDirectory;
                Directory.CreateDirectory(dir);
                foreach (var kv in downloaded)
                    File.WriteAllText(Path.Combine(dir, kv.Key + ".csv.tmp"), kv.Value, new UTF8Encoding(true));
                foreach (string table in downloaded.Keys)
                {
                    string target = Path.Combine(dir, table + ".csv");
                    File.Copy(target + ".tmp", target, overwrite: true);
                    File.Delete(target + ".tmp");
                }

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegSubKey))
                    key?.SetValue("LastDataSync", DateTime.Today.ToString("yyyy-MM-dd"));

                // Bổ sung cục bộ: Sheet đã có (admin duyệt) hoặc không còn trong hàng chờ (bị từ chối) thì bỏ
                List<FnameUpdateRequest> mine = FnameSheetWriter.ListMyPendingRequests(moduleDirectory);
                FnameLocalAdditions.Prune(dir, FnameCatalogData.Parse(FnameDataStore.SplitLines(downloaded["catalog"])),
                    mine?.Where(r => r.IsLoai(FnameUpdateRequest.LoaiCatalog)).Select(r => r.Get("M")).ToList());
                TraVThuLocalAdditions.Prune(dir, TraVThuData.Parse(FnameDataStore.ReadLines(moduleDirectory, "trav_thu", dir)),
                    mine?.Where(r => r.IsLoai(FnameUpdateRequest.LoaiTrialBend)).Select(r => TraVThuRecord.FromParams(r.Params)).ToList());
            }
            catch (Exception ex)
            {
                return (false, "Không lưu được dữ liệu vào " + FnameDataStore.DefaultCacheDirectory
                    + " (" + ex.GetBaseException().Message + "). Đóng Excel nếu đang mở file CSV rồi thử lại.");
            }

            return (true, $"Đã cập nhật dữ liệu Fname từ Google Sheet ({DateTime.Now:HH:mm dd/MM}).");
        }

        /// <summary>Khoá write_key để riêng (git bỏ qua) — cùng thư mục với google_sheet.ini.</summary>
        public static string KeyPath(string moduleDirectory) =>
            Path.Combine(moduleDirectory ?? string.Empty, "data", "google_sheet.key");

        /// <summary>Đọc google_sheet.ini rồi cộng google_sheet.key (nếu có) — key file ghi đè cùng khoá.</summary>
        internal static Dictionary<string, string> ReadConfig(string moduleDirectory)
        {
            var cfg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in new[] { ConfigPath(moduleDirectory), KeyPath(moduleDirectory) })
            {
                if (!File.Exists(path))
                    continue;
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string s = line.Trim();
                    int eq = s.IndexOf('=');
                    if (s.StartsWith(";", StringComparison.Ordinal) || eq <= 0)
                        continue;
                    cfg[s.Substring(0, eq).Trim()] = s.Substring(eq + 1).Trim();
                }
            }
            return cfg;
        }

        internal static string Get(this Dictionary<string, string> d, string key) =>
            d.TryGetValue(key, out string v) ? v : null;
    }
}
