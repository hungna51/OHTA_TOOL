using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Gọi Apps Script Web App chung (01_Fname/data/google_sheet_apps_script.gs) — dùng cho Fname (hàng chờ duyệt)
    /// và báo lỗi (ADR 0007). Cấu hình write_url trong google_sheet.ini, write_key trong google_sheet.key.
    /// Có gọi mạng: chỉ gọi từ luồng nền.
    /// </summary>
    public static class SheetWebApp
    {
        /// <summary>Trả JSON thô (body) và bản đọc sẵn FnameSheetReply; null + error khi không gửi được.</summary>
        public static FnameSheetReply Post(string fnameModuleDir, string action, Dictionary<string, string> fields,
            int timeoutSeconds, out string body, out string error)
        {
            body = null;
            error = null;
            // Bản thấp hơn phien_ban_toi_thieu (tab cau_hinh) không được ghi dữ liệu Fname; đọc và báo lỗi vẫn được
            if (SheetActions.RequiresVersionGate(action) && FnameSheetWriter.IsUpdateRequired())
            {
                error = FnameSheetWriter.UpdateRequiredMessage();
                return null;
            }
            Dictionary<string, string> cfg = FnameDataSync.ReadConfig(fnameModuleDir);
            string url = cfg.Get("write_url");
            string key = cfg.Get("write_key");
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                error = "Chưa cấu hình ghi lên Google Sheet: cần write_url trong " + FnameDataSync.ConfigPath(fnameModuleDir)
                    + " và write_key trong " + FnameDataSync.KeyPath(fnameModuleDir) + " (hướng dẫn ở đầu file google_sheet_apps_script.gs).";
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
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) })
                {
                    // Tự mã hoá form thay FormUrlEncodedContent: bản .NET Framework báo "Uri string is too long" khi giá trị > ~65k ký tự (ảnh base64)
                    string form = string.Join("&", fields.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + EscapeLong(kv.Value ?? string.Empty)));
                    var content = new StringContent(form, Encoding.UTF8, "application/x-www-form-urlencoded");
                    HttpResponseMessage resp = http.PostAsync(url, content).GetAwaiter().GetResult();
                    body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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

        /// <summary>Uri.EscapeDataString theo từng đoạn (bản .NET Framework giới hạn ~65k ký tự mỗi lần gọi).</summary>
        internal static string EscapeLong(string value)
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
    }
}
