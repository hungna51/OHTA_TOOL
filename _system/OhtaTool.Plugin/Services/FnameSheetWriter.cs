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
    /// Hàng chờ phê duyệt trên Google Sheet qua Apps Script Web App (01_Fname/data/google_sheet_apps_script.gs):
    /// mọi máy gửi yêu cầu vào tab "update"; máy admin (tên máy có trong tab "admin") liệt kê, duyệt hoặc từ chối.
    /// Cấu hình: write_url + write_key trong 01_Fname/data/google_sheet.ini. Mọi hàm có gọi mạng -> chạy ở luồng nền.
    /// </summary>
    public static class FnameSheetWriter
    {
        private const string RegSubKey = @"Software\OFT_Lisp\OhtaTool\Fname";

        public static bool IsConfigured(string moduleDirectory)
        {
            var cfg = FnameDataSync.ReadConfig(moduleDirectory);
            return !string.IsNullOrEmpty(cfg.Get("write_url")) && !string.IsNullOrEmpty(cfg.Get("write_key"));
        }

        public static (bool ok, string message) SubmitCatalogRequest(string moduleDirectory, string m, string loai, string z, IList<string> thicknesses)
        {
            FnameSheetReply r = Post(moduleDirectory, "submit_request", new Dictionary<string, string>
            {
                ["loai"] = FnameUpdateRequest.LoaiCatalog,
                ["thong_so"] = FnameUpdateRequest.BuildCatalogParams(m, loai, z, thicknesses)
            }, out string error);
            return r == null ? (false, error) : (r.Ok, r.Message ?? string.Empty);
        }

        /// <summary>Gửi 1 dòng góc bẻ thử / nội suy vào hàng chờ (tab "update"), admin duyệt thì ghi vào tab trav_thu.</summary>
        public static (bool ok, string message) SubmitTrialRequest(string moduleDirectory, TraVThuRecord record, string ghiChu)
        {
            FnameSheetReply r = Post(moduleDirectory, "submit_request", new Dictionary<string, string>
            {
                ["loai"] = FnameUpdateRequest.LoaiTrialBend,
                ["thong_so"] = FnameUpdateRequest.BuildTrialParams(record, ghiChu)
            }, out string error);
            if (r == null)
                return (false, error);
            string msg = r.Message ?? string.Empty;
            // Apps Script trên Google còn bản cũ (chưa có bộ xử lý trav_thu)
            if (!r.Ok && msg.IndexOf("không hỗ trợ", StringComparison.OrdinalIgnoreCase) >= 0)
                msg += " — Apps Script trên Google chưa cập nhật: dán lại google_sheet_apps_script.gs rồi Triển khai > Quản lý các tùy chọn triển khai > Sửa > Phiên bản mới.";
            return (r.Ok, msg);
        }

        /// <summary>Lấy danh sách yêu cầu chờ duyệt; lưu IsAdmin + số yêu cầu vào registry để hiện số đỏ khi mở lại CAD.</summary>
        public static (bool ok, bool isAdmin, List<FnameUpdateRequest> requests, string message) ListRequests(string moduleDirectory)
        {
            FnameSheetReply r = Post(moduleDirectory, "list_requests", new Dictionary<string, string>(), out string error);
            if (r == null)
                return (false, LoadIsAdmin(), new List<FnameUpdateRequest>(), error);
            if (!r.Ok)
                return (false, LoadIsAdmin(), new List<FnameUpdateRequest>(), r.Message ?? "Google Sheet từ chối yêu cầu.");

            SaveState(r.IsAdmin, r.Requests.Count);
            return (true, r.IsAdmin, r.Requests, r.IsAdmin
                ? $"Có {r.Requests.Count} yêu cầu chờ duyệt ({DateTime.Now:HH:mm dd/MM})."
                : $"Máy {Environment.MachineName} không có trong danh sách admin.");
        }

        /// <summary>
        /// Các yêu cầu còn chờ duyệt do chính máy này gửi (mọi loại dữ liệu).
        /// null = không đọc được (chưa cấu hình / mất mạng / script cũ) -> không được bỏ bổ sung cục bộ.
        /// </summary>
        public static List<FnameUpdateRequest> ListMyPendingRequests(string moduleDirectory)
        {
            if (!IsConfigured(moduleDirectory))
                return null;
            FnameSheetReply r = Post(moduleDirectory, "my_requests", new Dictionary<string, string>(), out _);
            if (r == null || !r.Ok)
                return null;
            return r.Requests;
        }

        /// <summary>Duyệt (approve=true) hoặc từ chối 1 yêu cầu. Duyệt catalog thành công thì cập nhật luôn cache trên máy.</summary>
        public static (bool ok, string message) Resolve(string moduleDirectory, FnameUpdateRequest request, bool approve)
        {
            FnameSheetReply r = Post(moduleDirectory, approve ? "approve_request" : "reject_request",
                new Dictionary<string, string> { ["id"] = request?.Id ?? string.Empty }, out string error);
            if (r == null)
                return (false, error);
            if (!r.Ok)
                return (false, r.Message ?? "Google Sheet từ chối yêu cầu.");

            string note = string.Empty;
            if (approve && string.Equals(request.Loai, FnameUpdateRequest.LoaiCatalog, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    FnameCatalogData.TryParseThicknessInput(request.Get("T"), out List<string> ts, out _);
                    ApplyToLocalCache(moduleDirectory, request.Get("M"), request.Get("Loai"), request.Get("Z"), ts);
                }
                catch (Exception ex)
                {
                    note = " (Chưa cập nhật được dữ liệu trên máy: " + ex.GetBaseException().Message + " — bấm Cập nhật từ Google Sheet.)";
                }
            }
            else if (approve && request.IsLoai(FnameUpdateRequest.LoaiTrialBend))
            {
                try
                {
                    ApplyTrialToLocalCache(moduleDirectory, TraVThuRecord.FromParams(request.Params));
                }
                catch (Exception ex)
                {
                    note = " (Chưa cập nhật được dữ liệu trên máy: " + ex.GetBaseException().Message + " — bấm Cập nhật từ Google Sheet.)";
                }
            }
            else if (approve && request.IsLoai(FnameUpdateRequest.LoaiTam))
            {
                try
                {
                    TamRecord rec = TamRecord.FromParams(request.Params);
                    rec.Ngay = DateTime.Today.ToString("yyyy-MM-dd");
                    ApplyTamToLocalCache(moduleDirectory, rec);
                }
                catch (Exception ex)
                {
                    note = " (Chưa cập nhật được dữ liệu trên máy: " + ex.GetBaseException().Message + " — bấm Cập nhật từ Google Sheet.)";
                }
            }
            return (true, (r.Message ?? string.Empty) + note);
        }

        /// <summary>
        /// Gửi bổ sung T thực của tấm kèm 1–3 ảnh JPEG bằng chứng. Apps Script lưu ảnh vào thư mục Drive
        /// (ai có link được xem), ghi link vào khoá Anh của yêu cầu; admin duyệt thì ghi dòng vào tab tam.
        /// Trả về Thong_so đã có link ảnh (để máy gửi lưu tam_local.csv) qua message khi thành công.
        /// </summary>
        public static (bool ok, string message, string anh) SubmitTamRequest(string moduleDirectory, TamRecord record, IList<byte[]> jpegs)
        {
            var fields = new Dictionary<string, string>
            {
                ["loai"] = FnameUpdateRequest.LoaiTam,
                ["thong_so"] = FnameUpdateRequest.BuildTamParams(record)
            };
            for (int i = 0; i < (jpegs?.Count ?? 0) && i < 3; i++)
                fields["anh_" + (i + 1)] = Convert.ToBase64String(jpegs[i]);

            FnameSheetReply r = Post(moduleDirectory, "submit_request", fields, out string error);
            if (r == null)
                return (false, error, string.Empty);
            string msg = r.Message ?? string.Empty;
            if (!r.Ok && msg.IndexOf("không hỗ trợ", StringComparison.OrdinalIgnoreCase) >= 0)
                msg += " — Apps Script trên Google chưa cập nhật: dán lại google_sheet_apps_script.gs rồi Triển khai > Quản lý các tùy chọn triển khai > Sửa > Phiên bản mới.";
            return (r.Ok, msg, r.Anh ?? string.Empty);
        }

        /// <summary>Thêm / thay dòng thông số tấm trong tam.csv của cache máy (cùng khoá với Apps Script).</summary>
        public static void ApplyTamToLocalCache(string moduleDirectory, TamRecord record, string cacheDirectory = null)
        {
            string dir = cacheDirectory ?? FnameDataStore.DefaultCacheDirectory;
            List<TamRecord> rows = TamData.Parse(FnameDataStore.ReadLines(moduleDirectory, "tam", dir));
            TamData.Upsert(rows, record);
            Directory.CreateDirectory(dir);
            string target = Path.Combine(dir, "tam.csv");
            File.WriteAllText(target + ".tmp", string.Join("\n", TamData.ToCsvLines(rows)) + "\n", new UTF8Encoding(true));
            File.Copy(target + ".tmp", target, overwrite: true);
            File.Delete(target + ".tmp");
        }

        /// <summary>Gộp vật liệu vào catalog.csv trong cache máy (cùng quy tắc với Apps Script).</summary>
        public static void ApplyToLocalCache(string moduleDirectory, string m, string loai, string z, IList<string> thicknesses, string cacheDirectory = null)
        {
            string dir = cacheDirectory ?? FnameDataStore.DefaultCacheDirectory;
            string[] current = FnameDataStore.ReadLines(moduleDirectory, "catalog", dir);
            string[] updated = FnameCatalogData.Upsert(current, m, loai, z, thicknesses);
            Directory.CreateDirectory(dir);
            string target = Path.Combine(dir, "catalog.csv");
            File.WriteAllText(target + ".tmp", string.Join("\n", updated) + "\n", new UTF8Encoding(true));
            File.Copy(target + ".tmp", target, overwrite: true);
            File.Delete(target + ".tmp");
        }

        /// <summary>Thêm / thay dòng góc bẻ thử trong trav_thu.csv của cache máy (cùng khoá với Apps Script).</summary>
        public static void ApplyTrialToLocalCache(string moduleDirectory, TraVThuRecord record, string cacheDirectory = null)
        {
            string dir = cacheDirectory ?? FnameDataStore.DefaultCacheDirectory;
            List<TraVThuRecord> rows = TraVThuData.Parse(FnameDataStore.ReadLines(moduleDirectory, "trav_thu", dir));
            TraVThuData.Upsert(rows, record);
            Directory.CreateDirectory(dir);
            string target = Path.Combine(dir, "trav_thu.csv");
            File.WriteAllText(target + ".tmp", string.Join("\n", TraVThuData.ToCsvLines(rows)) + "\n", new UTF8Encoding(true));
            File.Copy(target + ".tmp", target, overwrite: true);
            File.Delete(target + ".tmp");
        }

        // ---------- Trạng thái nhớ trong registry (hiện số đỏ không cần gọi mạng) ----------

        public static bool LoadIsAdmin() => ReadInt("ApprovalIsAdmin") == 1;

        public static int LoadPendingCount() => Math.Max(0, ReadInt("ApprovalPending"));

        public static void SaveState(bool isAdmin, int pending)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegSubKey))
                {
                    key?.SetValue("ApprovalIsAdmin", isAdmin ? 1 : 0, RegistryValueKind.DWord);
                    key?.SetValue("ApprovalPending", Math.Max(0, pending), RegistryValueKind.DWord);
                }
            }
            catch
            {
            }
        }

        private static int ReadInt(string name)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegSubKey, writable: false))
                    return key?.GetValue(name) is int v ? v : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>Uri.EscapeDataString theo từng đoạn (bản .NET Framework giới hạn ~65k ký tự mỗi lần gọi).</summary>
        private static string EscapeLong(string value)
        {
            const int chunk = 30000;
            if (value.Length <= chunk)
                return Uri.EscapeDataString(value);
            var sb = new StringBuilder(value.Length + value.Length / 4);
            int i = 0;
            while (i < value.Length)
            {
                int len = Math.Min(chunk, value.Length - i);
                // không cắt giữa cặp surrogate (ký tự 4 byte)
                if (i + len < value.Length && char.IsHighSurrogate(value[i + len - 1]))
                    len--;
                sb.Append(Uri.EscapeDataString(value.Substring(i, len)));
                i += len;
            }
            return sb.ToString();
        }

        /// <summary>Máy đang chạy bản thấp hơn phien_ban_toi_thieu trên tab cau_hinh của Sheet.</summary>
        public static bool IsUpdateRequired() =>
            FnameVersionGate.IsOutdated(OhtaUpdateCommands.LoadedVersion, TraVCalculator.GetSheetConfig());

        public static string UpdateRequiredMessage()
        {
            TraVCalculator.GetSheetConfig().TryGetValue(FnameSheetConfig.MinVersionKey, out string min);
            return $"Cần cập nhật OHTA TOOL: máy đang chạy v{OhtaUpdateCommands.LoadedVersion}, dữ liệu Fname yêu cầu từ v{min} trở lên "
                + "(gõ OHTAUPDATE). Trong lúc chờ vẫn đặt / sửa Fname được, chỉ khoá gửi dữ liệu lên Google Sheet.";
        }

        // ---------- Gọi Apps Script ----------

        private static FnameSheetReply Post(string moduleDirectory, string action, Dictionary<string, string> fields, out string error)
        {
            error = null;
            // Bản thấp hơn phien_ban_toi_thieu (tab cau_hinh) không được ghi lên Sheet; đọc vẫn được
            if (action != "list_requests" && action != "my_requests" && IsUpdateRequired())
            {
                error = UpdateRequiredMessage();
                return null;
            }
            Dictionary<string, string> cfg = FnameDataSync.ReadConfig(moduleDirectory);
            string url = cfg.Get("write_url");
            string key = cfg.Get("write_key");
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                error = "Chưa cấu hình ghi lên Google Sheet: cần write_url trong " + FnameDataSync.ConfigPath(moduleDirectory)
                    + " và write_key trong " + FnameDataSync.KeyPath(moduleDirectory) + " (hướng dẫn ở đầu file google_sheet_apps_script.gs).";
                return null;
            }

            fields["key"] = key;
            fields["action"] = action;
            fields["gid"] = cfg.Get("catalog") ?? string.Empty;
            fields["machine"] = Environment.MachineName;
            try
            {
#if NETFRAMEWORK
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
#endif
                // Apps Script trả 302 sang link kết quả; HttpClient tự đi theo (đổi sang GET) để lấy JSON.
                // Có ảnh bằng chứng (base64 vài trăm KB) -> cho thêm thời gian
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(fields.ContainsKey("anh_1") ? 120 : 30) })
                {
                    // Tự mã hoá form thay FormUrlEncodedContent: bản .NET Framework báo "Uri string is too long" khi giá trị > ~65k ký tự (ảnh base64)
                    string form = string.Join("&", fields.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + EscapeLong(kv.Value ?? string.Empty)));
                    var content = new StringContent(form, Encoding.UTF8, "application/x-www-form-urlencoded");
                    HttpResponseMessage resp = http.PostAsync(url, content).GetAwaiter().GetResult();
                    string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    FnameSheetReply reply = FnameSheetReply.Parse(body);
                    if (reply == null)
                        error = "Apps Script không trả về kết quả hợp lệ (HTTP " + (int)resp.StatusCode
                            + "). Kiểm tra write_url, đã triển khai phiên bản mới và chọn \"Người có quyền truy cập: Bất kỳ ai\".";
                    return reply;
                }
            }
            catch (Exception ex)
            {
                error = "Không kết nối được Google Sheet (" + ex.GetBaseException().Message + ").";
                return null;
            }
        }
    }
}
