using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Từ điển tra cứu và dịch thuật ghi chú tiếng Nhật sang tiếng Việt trong thông tin tấm gia công.
    /// Hỗ trợ cả ký tự Katakana nửa độ rộng (half-width, vd: ｹｶﾞｷ線), đủ độ rộng (full-width, vd: ケガキ線)
    /// và mã thoát Unicode DXF (vd: \U+FF79\U+FF9E\U+FF7F\U+7DDA).
    /// Có thể lưu trữ qua Windows Registry HKCU\Software\OFT_Lisp\JapanNoteDictionary
    /// và đồng bộ trực tuyến từ Google Sheet công khai (Cột 1: Tiếng Nhật, Cột 2: Tiếng Việt).
    /// Hỗ trợ đồng bộ 2 chiều (tải về từ Sheet và đẩy lên Sheet qua Apps Script Web App).
    /// </summary>
    public static class JapanNoteTranslator
    {
        /// <summary>
        /// Khoá registry của từ điển + cài đặt đồng bộ. Mục 158: KHÔNG còn là const — bộ test
        /// (ZwCadPlugin.Tests) đổi sang khoá riêng "..._Test" trước khi chạy, vì ResetToDefaults /
        /// SyncFromGoogleSheet / SetAutoUpload / SetScriptUrl trong test từng XOÁ từ điển thật và
        /// ghi đè cài đặt của người dùng.
        /// </summary>
        public static string RegSubKey { get; set; } = @"Software\OFT_Lisp\JapanNoteDictionary";
        public const string RegKeySheetUrl = "__SheetUrl";
        public const string RegKeyScriptUrl = "__ScriptUrl";
        public const string RegKeyAutoSync = "__AutoSync";
        public const string RegKeyAutoUpload = "__AutoUpload";
        /// <summary>Mục 197: ngày tải Google Sheet thành công gần nhất (yyyy-MM-dd).</summary>
        public const string RegKeyLastSync = "__LastSync";

        private static bool _daThuTuCapNhat;

        public const string DefaultGoogleScriptUrl = "https://script.google.com/macros/s/AKfycbxVfUlv-7QwxIrstU-PGCXNyp2qHxosniorVeLEBw-khSGKkQqLIE6E3BIVi7H0kp_UXg/exec";
        public const string DefaultGoogleSheetUrl = "https://script.google.com/macros/s/AKfycbxVfUlv-7QwxIrstU-PGCXNyp2qHxosniorVeLEBw-khSGKkQqLIE6E3BIVi7H0kp_UXg/exec";
        public const string LegacyDefaultGoogleSheetUrl = "https://docs.google.com/spreadsheets/d/1gmtbKIS56uaLkMNMWybrds9uJhyz3wIBuVrEbZWMtOw/edit?gid=0#gid=0";

        public const string AppsScriptTemplate = @"// ====== GOOGLE APPS SCRIPT ĐỒNG BỘ 2 CHIỀU CHO ZWCAD / AUTOCAD ======
// Hướng dẫn: Tiện ích mở rộng -> Apps Script -> Dán mã này -> Triển khai (Deploy) làm Web App (Quyền: Bất kỳ ai)

function doGet(e) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
  var values = sheet.getDataRange().getValues();
  var csv = """";
  for (var i = 0; i < values.length; i++) {
    var c0 = (values[i][0] != null ? values[i][0].toString() : """").replace(/""/g, '""""');
    var c1 = (values[i][1] != null ? values[i][1].toString() : """").replace(/""/g, '""""');
    csv += '""' + c0 + '"",""' + c1 + '""\r\n';
  }
  return ContentService.createTextOutput(csv).setMimeType(ContentService.MimeType.TEXT);
}

function doPost(e) {
  try {
    var sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
    var contents = e.postData.contents;
    var list = JSON.parse(contents);
    
    // Ghi đè toàn bộ dữ liệu cột A và B
    sheet.clearContents();
    sheet.appendRow([""Tiếng Nhật"", ""Tiếng Việt""]);
    for (var i = 0; i < list.length; i++) {
      var jp = list[i].Japanese || list[i].jp || """";
      var vn = list[i].Vietnamese || list[i].vn || """";
      if (jp.toString().trim() !== """") {
        sheet.appendRow([jp, vn]);
      }
    }
    return ContentService.createTextOutput(JSON.stringify({ success: true, count: list.length }))
      .setMimeType(ContentService.MimeType.JSON);
  } catch (err) {
    return ContentService.createTextOutput(JSON.stringify({ success: false, error: err.toString() }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}
";

        private static readonly Dictionary<string, string> _defaultDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ｹｶﾞｷ線", "khắc kegaki" },
            { "ケガキ線", "khắc kegaki" },
            { "\\U+FF79\\U+FF9E\\U+FF7F\\U+7DDA", "khắc kegaki" },
            { "\\U+FF79\\U+FF76\\U+FF9E\\U+FF77\\U+7DDA", "khắc kegaki" }
        };

        private static readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        static JapanNoteTranslator()
        {
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            lock (_lock)
            {
                if (_initialized) return;
                ResetDictionaryToDefaults();
                LoadFromRegistry();
                _initialized = true;
            }
        }

        private static void ResetDictionaryToDefaults()
        {
            _dictionary.Clear();
            foreach (var kvp in _defaultDictionary)
            {
                _dictionary[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Nạp các từ khóa bổ sung đã lưu trong Windows Registry.
        /// </summary>
        public static void LoadFromRegistry()
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        foreach (string valName in root.GetValueNames())
                        {
                            if (valName.StartsWith("__")) continue; // Bỏ qua khóa cấu hình
                            object val = root.GetValue(valName);
                            if (val != null && !string.IsNullOrWhiteSpace(valName))
                            {
                                _dictionary[valName.Trim()] = val.ToString().Trim();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Môi trường không có quyền Registry thì giữ nguyên in-memory
            }
        }

        /// <summary>
        /// Lấy URL Google Sheet đã lưu, nếu chưa có thì trả về URL mặc định.
        /// </summary>
        public static string GetSheetUrl()
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        object val = root.GetValue(RegKeySheetUrl);
                        if (val != null)
                        {
                            string s = val.ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(s) && s != LegacyDefaultGoogleSheetUrl)
                                return s;
                        }
                    }
                }
            }
            catch { }
            return DefaultGoogleScriptUrl;
        }

        /// <summary>
        /// Lưu URL Google Sheet vào Registry.
        /// </summary>
        public static void SetSheetUrl(string url)
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.CreateSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        string cleanUrl = url?.Trim() ?? string.Empty;
                        root.SetValue(RegKeySheetUrl, cleanUrl);
                        if (cleanUrl.Contains("script.google.com"))
                        {
                            root.SetValue(RegKeyScriptUrl, cleanUrl);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Kiểm tra cờ tự động đồng bộ khi mở.
        /// </summary>
        public static bool GetAutoSync()
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        object val = root.GetValue(RegKeyAutoSync);
                        if (val != null)
                            return val.ToString() == "1";
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Bật/tắt cờ tự động đồng bộ trong Registry.
        /// </summary>
        public static void SetAutoSync(bool enable)
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.CreateSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        root.SetValue(RegKeyAutoSync, enable ? "1" : "0");
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Lấy URL Google Apps Script Web App để đẩy dữ liệu lên.
        /// </summary>
        public static string GetScriptUrl()
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        object val = root.GetValue(RegKeyScriptUrl);
                        if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                            return val.ToString().Trim();
                    }
                }
            }
            catch { }
            return DefaultGoogleScriptUrl;
        }

        /// <summary>
        /// Lưu URL Google Apps Script Web App vào Registry.
        /// </summary>
        public static void SetScriptUrl(string url)
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.CreateSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        root.SetValue(RegKeyScriptUrl, url?.Trim() ?? string.Empty);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Kiểm tra cờ tự động đẩy lên Google Sheet khi Lưu.
        /// </summary>
        public static bool GetAutoUpload()
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        object val = root.GetValue(RegKeyAutoUpload);
                        if (val != null)
                            return val.ToString() == "1";
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Bật/tắt cờ tự động đẩy lên Google Sheet trong Registry.
        /// </summary>
        public static void SetAutoUpload(bool enable)
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.CreateSubKey(RegSubKey))
                {
                    if (root != null)
                    {
                        root.SetValue(RegKeyAutoUpload, enable ? "1" : "0");
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Chuyển đổi link Google Sheet thông thường (link share, edit, pubhtml) thành link xuất CSV trực tiếp.
        /// </summary>
        public static string ChuyenDoiUrlGoogleSheet(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return string.Empty;
            rawUrl = rawUrl.Trim();

            // Đã là link CSV trực tiếp
            if (rawUrl.Contains("format=csv") || rawUrl.Contains("output=csv"))
                return rawUrl;

            // Link publish to web
            if (rawUrl.Contains("/pubhtml"))
                return rawUrl.Replace("/pubhtml", "/pub?output=csv");

            // Link Google Sheets chuẩn: /spreadsheets/d/{ID}/edit...
            var match = Regex.Match(rawUrl, @"/spreadsheets/d/([a-zA-Z0-9-_]+)");
            if (match.Success)
            {
                string id = match.Groups[1].Value;
                string gid = "0";
                var gidMatch = Regex.Match(rawUrl, @"[#&?]gid=([0-9]+)");
                if (gidMatch.Success) gid = gidMatch.Groups[1].Value;

                return $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={gid}";
            }

            return rawUrl;
        }

        /// <summary>
        /// Tách nội dung CSV từ Google Sheet thành danh sách mục từ điển.
        /// </summary>
        public static List<NoteDictionaryItem> ParseCsv(string csvContent)
        {
            var list = new List<NoteDictionaryItem>();
            if (string.IsNullOrWhiteSpace(csvContent)) return list;

            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            bool isFirstLine = true;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = ParseCsvLine(line);
                if (cols.Count >= 2)
                {
                    string col1 = cols[0].Trim();
                    string col2 = cols[1].Trim();

                    // Bỏ qua dòng tiêu đề nếu có
                    if (isFirstLine && IsHeaderRow(col1, col2))
                    {
                        isFirstLine = false;
                        continue;
                    }
                    isFirstLine = false;

                    if (!string.IsNullOrEmpty(col1))
                    {
                        list.Add(new NoteDictionaryItem
                        {
                            Japanese = col1,
                            Vietnamese = col2
                        });
                    }
                }
                else if (cols.Count == 1 && !string.IsNullOrWhiteSpace(cols[0]))
                {
                    if (isFirstLine && IsHeaderRow(cols[0], string.Empty))
                    {
                        isFirstLine = false;
                        continue;
                    }
                    isFirstLine = false;

                    list.Add(new NoteDictionaryItem
                    {
                        Japanese = cols[0].Trim(),
                        Vietnamese = string.Empty
                    });
                }
            }
            return list;
        }

        private static bool IsHeaderRow(string col1, string col2)
        {
            string c1 = col1.ToLowerInvariant();
            string c2 = col2.ToLowerInvariant();
            return c1.Contains("tiếng nhật") || c1.Contains("japanese") || c1.Contains("từ khóa") || c1.Contains("cot 1") || c1.Contains("cột 1") || c1 == "jp" ||
                   c2.Contains("tiếng việt") || c2.Contains("vietnamese") || c2.Contains("ý nghĩa") || c2.Contains("cot 2") || c2.Contains("cột 2") || c2 == "vn";
        }

        /// <summary>
        /// Parse 1 dòng CSV hỗ trợ trường bọc dấu ngoặc kép chứa dấu phẩy.
        /// </summary>
        public static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++; // Bỏ qua quote kép
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result;
        }

        /// <summary>
        /// Đồng bộ từ điển từ link Google Sheet (Cột 1: Tiếng Nhật, Cột 2: Tiếng Việt).
        /// </summary>
        public static (bool Success, string Message, int Count) SyncFromGoogleSheet(string rawUrl = null)
        {
            string urlToUse = !string.IsNullOrWhiteSpace(rawUrl) ? rawUrl.Trim() : GetSheetUrl();
            if (string.IsNullOrWhiteSpace(urlToUse))
                urlToUse = DefaultGoogleSheetUrl;

            string csvUrl = ChuyenDoiUrlGoogleSheet(urlToUse);
            if (string.IsNullOrWhiteSpace(csvUrl))
                return (false, "Đường dẫn Google Sheet không hợp lệ!", 0);

            try
            {
                string csvContent;
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    csvContent = wc.DownloadString(csvUrl);
                }

                var items = ParseResponse(csvContent);
                if (items.Count == 0)
                {
                    return (false, "Không tìm thấy dữ liệu từ khóa nào trong file Google Sheet!", 0);
                }

                // Lưu vào từ điển và Registry
                SaveItems(items);
                SetSheetUrl(urlToUse);
                GhiNgayCapNhat(DateTime.Today);

                return (true, $"Đã đồng bộ thành công {items.Count} cụm từ từ Google Sheet!", items.Count);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi tải từ Google Sheet: {ex.Message}", 0);
            }
        }

        /// <summary>Ngày tải Google Sheet thành công gần nhất (yyyy-MM-dd), rỗng nếu chưa từng tải.</summary>
        public static string NgayCapNhat()
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(RegSubKey))
                    return root?.GetValue(RegKeyLastSync) as string ?? "";
            }
            catch { return ""; }
        }

        public static void GhiNgayCapNhat(DateTime ngay)
        {
            try
            {
                using (RegistryKey root = Registry.CurrentUser.CreateSubKey(RegSubKey))
                    root?.SetValue(RegKeyLastSync, ngay.ToString("yyyy-MM-dd"));
            }
            catch { }
        }

        /// <summary>
        /// Mục 197 (giống Fname): lần mở tab Guro đầu tiên trong ngày thì tự tải từ điển từ
        /// Google Sheet. Mỗi phiên CAD chỉ thử 1 lần — lỗi mạng thì để hôm sau / bấm ⬇ Tải.
        /// </summary>
        public static bool NenTuCapNhat(DateTime homNay)
        {
            if (_daThuTuCapNhat) return false;
            _daThuTuCapNhat = true;
            return NgayCapNhat() != homNay.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Phiên bản bất đồng bộ của SyncFromGoogleSheet cho giao diện WPF.
        /// </summary>
        public static Task<(bool Success, string Message, int Count)> SyncFromGoogleSheetAsync(string rawUrl = null)
        {
            return Task.Run(() => SyncFromGoogleSheet(rawUrl));
        }

        /// <summary>
        /// Parse danh sách mục từ chuỗi tải về (tự nhận diện định dạng CSV hay JSON từ Google Apps Script).
        /// </summary>
        public static List<NoteDictionaryItem> ParseResponse(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return new List<NoteDictionaryItem>();
            string trimmed = content.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                var jsonItems = ParseJsonArray(trimmed);
                if (jsonItems.Count > 0) return jsonItems;
            }
            return ParseCsv(content);
        }

        /// <summary>
        /// Parse mảng JSON [{"Japanese":"...", "Vietnamese":"..."}, ...] mà không cần thư viện bên ngoài.
        /// </summary>
        public static List<NoteDictionaryItem> ParseJsonArray(string json)
        {
            var list = new List<NoteDictionaryItem>();
            if (string.IsNullOrWhiteSpace(json)) return list;

            var pattern = @"\{\s*""(?:Japanese|jp)""\s*:\s*""((?:\\.|[^""\\])*)""\s*,\s*""(?:Vietnamese|vn)""\s*:\s*""((?:\\.|[^""\\])*)""\s*\}";
            var matches = Regex.Matches(json, pattern, RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                if (m.Success)
                {
                    string jp = UnescapeJson(m.Groups[1].Value).Trim();
                    string vn = UnescapeJson(m.Groups[2].Value).Trim();
                    if (!string.IsNullOrEmpty(jp))
                    {
                        list.Add(new NoteDictionaryItem { Japanese = jp, Vietnamese = vn });
                    }
                }
            }
            return list;
        }

        public static string UnescapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return Regex.Unescape(s);
        }

        /// <summary>
        /// Chuyển đổi danh sách từ khóa sang chuỗi JSON để gửi cho Google Apps Script.
        /// </summary>
        public static string ToJson(IEnumerable<NoteDictionaryItem> items)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            bool first = true;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item?.Japanese)) continue;
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append("{\"Japanese\":");
                    sb.Append(EscapeJson(item.Japanese.Trim()));
                    sb.Append(",\"Vietnamese\":");
                    sb.Append(EscapeJson((item.Vietnamese ?? string.Empty).Trim()));
                    sb.Append("}");
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Chuyển đổi danh sách sang dạng Tab-Separated Values (TSV) để sao chép vào Clipboard và dán vào Google Sheet.
        /// </summary>
        public static string ToTsv(IEnumerable<NoteDictionaryItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Tiếng Nhật\tTiếng Việt");
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item?.Japanese)) continue;
                    string jp = item.Japanese.Replace("\t", " ").Replace("\r", "").Replace("\n", " ").Trim();
                    string vn = (item.Vietnamese ?? string.Empty).Replace("\t", " ").Replace("\r", "").Replace("\n", " ").Trim();
                    sb.Append(jp);
                    sb.Append("\t");
                    sb.Append(vn);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        public static string EscapeJson(string s)
        {
            if (s == null) return "\"\"";
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.AppendFormat("\\u{0:X04}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>
        /// Đẩy danh sách từ khóa lên Google Sheet qua Apps Script Web App.
        /// Nếu chưa cấu hình Apps Script, trả về kết quả yêu cầu cấu hình ("NEED_SETUP").
        /// </summary>
        public static async Task<(bool Success, string Message, int Count)> UploadToGoogleSheetAsync(
            IEnumerable<NoteDictionaryItem> items,
            string targetUrl = null)
        {
            var list = items?.Where(x => !string.IsNullOrWhiteSpace(x?.Japanese)).ToList() ?? new List<NoteDictionaryItem>();
            if (list.Count == 0)
            {
                return (false, "Không có từ khóa nào để tải lên!", 0);
            }

            string url = !string.IsNullOrWhiteSpace(targetUrl) ? targetUrl.Trim() : GetScriptUrl();
            if (string.IsNullOrWhiteSpace(url))
            {
                url = GetSheetUrl();
            }

            if (string.IsNullOrWhiteSpace(url) || !url.Contains("script.google.com"))
            {
                return (false, "NEED_SETUP", list.Count);
            }

            string json = ToJson(list);

            return await Task.Run(() =>
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "POST";
                    req.ContentType = "application/json; charset=utf-8";
                    req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ZWCAD-Plugin";
                    req.AllowAutoRedirect = false; // Xử lý redirect thủ công để tránh lỗi 302 trên POST và phát hiện chặn quyền
                    req.Timeout = 45000;

                    byte[] data = Encoding.UTF8.GetBytes(json);
                    req.ContentLength = data.Length;

                    using (var stream = req.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }

                    HttpWebResponse resp;
                    try
                    {
                        resp = (HttpWebResponse)req.GetResponse();
                    }
                    catch (WebException wex) when (wex.Response is HttpWebResponse errResp)
                    {
                        resp = errResp;
                    }

                    using (resp)
                    {
                        int statusCode = (int)resp.StatusCode;
                        if (statusCode == 301 || statusCode == 302 || statusCode == 303 || statusCode == 307)
                        {
                            string redirectUrl = resp.Headers["Location"];
                            if (string.IsNullOrWhiteSpace(redirectUrl))
                            {
                                return (false, "Máy chủ trả về chuyển hướng nhưng thiếu địa chỉ chuyển hướng (Location)!", 0);
                            }

                            // Phát hiện trường hợp Web App chưa mở quyền 'Bất kỳ ai' (Anyone)
                            if (redirectUrl.Contains("accounts.google.com"))
                            {
                                return (false,
                                    "Google Apps Script đang yêu cầu đăng nhập tài khoản (bị chặn quyền truy cập)!\n\n" +
                                    "Nguyên nhân: Khi tạo hoặc cập nhật Web App, mục 'Ai có quyền truy cập' (Who has access) đang bị chọn là 'Chỉ mình tôi' hoặc chỉ nội bộ cơ quan.\n\n" +
                                    "Cách khắc phục (làm 1 lần trong 30 giây):\n" +
                                    "1. Trên Google Sheet, vào: Tiện ích mở rộng ➔ Apps Script.\n" +
                                    "2. Bấm 'Triển khai' (Deploy) ➔ 'Quản lý bản triển khai' (Manage deployments).\n" +
                                    "3. Bấm biểu tượng cây bút ✏ (Chỉnh sửa) ở góc trên.\n" +
                                    "4. Tại mục 'Ai có quyền truy cập' (Who has access): Chọn 'Bất kỳ ai' (Anyone).\n" +
                                    "5. Bấm 'Triển khai' (Deploy) là xong!", 0);
                            }

                            // Chuyển hướng hợp lệ của Google Apps Script sang script.googleusercontent.com:
                            var getReq = (HttpWebRequest)WebRequest.Create(redirectUrl);
                            getReq.Method = "GET";
                            getReq.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ZWCAD-Plugin";
                            getReq.AllowAutoRedirect = true;
                            getReq.Timeout = 45000;

                            using (var getResp = (HttpWebResponse)getReq.GetResponse())
                            using (var s = getResp.GetResponseStream())
                            using (var r = new StreamReader(s, Encoding.UTF8))
                            {
                                string body = r.ReadToEnd();
                                if (body.Contains("\"success\":false") || body.StartsWith("ERROR:"))
                                {
                                    return (false, $"Google Apps Script báo lỗi: {body}", 0);
                                }
                                return (true, $"Đã cập nhật thành công {list.Count} từ khóa lên Google Sheet!", list.Count);
                            }
                        }

                        using (var stream = resp.GetResponseStream())
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            if (body.Contains("\"success\":false") || body.StartsWith("ERROR:"))
                            {
                                return (false, $"Google Apps Script báo lỗi: {body}", 0);
                            }
                            return (true, $"Đã cập nhật thành công {list.Count} từ khóa lên Google Sheet!", list.Count);
                        }
                    }
                }
                catch (WebException wex)
                {
                    string errDetail = wex.Message;
                    try
                    {
                        if (wex.Response != null)
                        {
                            using (var s = wex.Response.GetResponseStream())
                            using (var r = new StreamReader(s, Encoding.UTF8))
                            {
                                errDetail += " - " + r.ReadToEnd();
                            }
                        }
                    }
                    catch { }
                    return (false, $"Lỗi kết nối khi tải lên: {errDetail}", 0);
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi tải lên: {ex.Message}", 0);
                }
            });
        }

        /// <summary>
        /// Lấy toàn bộ danh sách từ điển hiện hành phục vụ giao diện cài đặt (ẩn các mã \U+ kỹ thuật).
        /// </summary>
        public static List<NoteDictionaryItem> GetItems()
        {
            EnsureInitialized();
            lock (_lock)
            {
                var list = new List<NoteDictionaryItem>();
                foreach (var kvp in _dictionary)
                {
                    if (kvp.Key.StartsWith("\\U+", StringComparison.OrdinalIgnoreCase)) continue;
                    list.Add(new NoteDictionaryItem
                    {
                        Japanese = kvp.Key,
                        Vietnamese = kvp.Value
                    });
                }
                return list;
            }
        }

        /// <summary>
        /// Lưu danh mục từ điển mới từ giao diện vào bộ nhớ và Windows Registry.
        /// </summary>
        public static void SaveItems(IEnumerable<NoteDictionaryItem> items)
        {
            lock (_lock)
            {
                ResetDictionaryToDefaults();

                try
                {
                    using (RegistryKey root = Registry.CurrentUser.CreateSubKey(RegSubKey))
                    {
                        if (root != null)
                        {
                            // Xóa sạch các key từ điển cũ để hỗ trợ xóa (giữ lại các key cấu hình __)
                            foreach (string name in root.GetValueNames())
                            {
                                if (!name.StartsWith("__"))
                                    root.DeleteValue(name, false);
                            }

                            if (items != null)
                            {
                                foreach (var it in items)
                                {
                                    if (string.IsNullOrWhiteSpace(it.Japanese)) continue;
                                    string jp = it.Japanese.Trim();
                                    string vn = it.Vietnamese?.Trim() ?? string.Empty;
                                    _dictionary[jp] = vn;
                                    root.SetValue(jp, vn);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    if (items != null)
                    {
                        foreach (var it in items)
                        {
                            if (string.IsNullOrWhiteSpace(it.Japanese)) continue;
                            _dictionary[it.Japanese.Trim()] = it.Vietnamese?.Trim() ?? string.Empty;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Đăng ký thêm hoặc ghi đè một cặp từ khóa Nhật -> Việt.
        /// </summary>
        public static void Register(string jp, string vn)
        {
            if (string.IsNullOrWhiteSpace(jp)) return;
            EnsureInitialized();
            lock (_lock)
            {
                _dictionary[jp.Trim()] = vn ?? string.Empty;
            }
        }

        /// <summary>
        /// Giải mã chuỗi chứa mã thoát AutoCAD \U+XXXX về ký tự Unicode tương ứng.
        /// </summary>
        public static string DecodeUnicodeEscapes(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.IndexOf("\\U+", StringComparison.OrdinalIgnoreCase) < 0) return input;

            return Regex.Replace(input, @"\\U\+([0-9a-fA-F]{4})", m =>
            {
                try
                {
                    int code = Convert.ToInt32(m.Groups[1].Value, 16);
                    return ((char)code).ToString();
                }
                catch
                {
                    return m.Value;
                }
            });
        }

        /// <summary>
        /// Dịch văn bản ghi chú tiếng Nhật sang tiếng Việt.
        /// Duyệt qua các từ khóa trong từ điển và thay thế; nếu không khớp từ khóa nào thì giữ nguyên văn.
        /// </summary>
        public static string Translate(string jpText)
        {
            if (string.IsNullOrWhiteSpace(jpText)) return string.Empty;
            EnsureInitialized();
            string result = jpText;
            lock (_lock)
            {
                // Mục 154: khoá DÀI trước — "文字ｹｶﾞｷ線" (khắc chữ) phải thay trước "ｹｶﾞｷ線" (khắc kegaki),
                // nếu không khoá ngắn ăn mất phần đuôi và ra "文字khắc kegaki".
                foreach (var kvp in _dictionary.OrderByDescending(k => k.Key.Length).ToList())
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
                    if (result.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result = ReplaceIgnoreCase(result, kvp.Key, kvp.Value);
                    }
                }
            }
            return result.Trim();
        }

        /// <summary>
        /// Trích xuất và dịch ghi chú từ text thô trên bản vẽ (ví dụ text trong phạm vi tấm).
        /// Nếu text có chứa bất kỳ từ khóa tiếng Nhật nào trong từ điển thì trả về ý nghĩa tiếng Việt tương ứng.
        /// Nếu không chứa từ khóa tiếng Nhật nào thì trả về string.Empty (để không nhận nhầm kích thước/vật liệu).
        /// </summary>
        /// <summary>
        /// Chuẩn hoá ghi chú GRO ghi trong XData bango (trường thứ 3) trước khi dịch (mục 126):
        /// giải mã \U+XXXX (lisp ghi chuỗi mã, XData không tự giải mã) và sửa chuỗi lisp CŨ gõ
        /// nhầm "ｹﾞｿ線" (FF79 FF9E FF7F 7DDA) thành "ｹｶﾞｷ線" — các bản vẽ đã chạy GRO trước bản sửa vẫn mang chuỗi sai.
        /// </summary>
        public static string ChuanHoaGhiChuGro(string note)
        {
            if (string.IsNullOrEmpty(note)) return string.Empty;
            string s = DecodeUnicodeEscapes(note);
            return s.Replace("ｹﾞｿ線", "ｹｶﾞｷ線");
        }

        /// <summary>Ký tự tiếng Nhật: kana, kanji (CJK), katakana nửa độ rộng (mục 160).</summary>
        public static bool LaChuNhat(char c)
        {
            return (c >= '぀' && c <= 'ヿ') || (c >= 'ㇰ' && c <= 'ㇿ')
                || (c >= '㐀' && c <= '鿿') || (c >= 'ｦ' && c <= 'ﾟ');
        }

        /// <summary>
        /// Mục 160: các đoạn tiếng Nhật CÒN SÓT trong ghi chú ĐÃ DỊCH = chưa có trong từ điển
        /// (vd "スリット" → ["スリット"]; "khắc chữ" → []). Không trùng, giữ thứ tự xuất hiện.
        /// </summary>
        public static List<string> DoanChuaDich(string ghiChuDaDich)
        {
            var kq = new List<string>();
            if (string.IsNullOrEmpty(ghiChuDaDich)) return kq;
            var sb = new StringBuilder();
            foreach (char c in ghiChuDaDich + " ")
            {
                if (LaChuNhat(c)) { sb.Append(c); continue; }
                if (sb.Length == 0) continue;
                string d = sb.ToString();
                if (!kq.Contains(d)) kq.Add(d);
                sb.Clear();
            }
            return kq;
        }

        /// <summary>Cụm nghĩa tiếng Việt đánh dấu tấm "khắc chữ" trong từ điển ghi chú (mục 157).</summary>
        public const string NghiaKhacChu = "khắc chữ";

        /// <summary>
        /// Mục 157: ghi chú GRO (chuỗi XData bango, có thể chứa \U+XXXX) có NGHĨA "khắc chữ" không —
        /// dịch y hệt cột Ghi chú của Danh Sách Tấm (ChuanHoaGhiChuGro → Translate), rồi tìm cụm
        /// <see cref="NghiaKhacChu"/> (không phân biệt hoa thường, chuẩn hoá Unicode NFC).
        /// Lisp gọi qua (gro_net_khac_chu "ghi chu") để chuyển text trong hình sang 3-KEGAKI.
        /// </summary>
        public static bool LaKhacChu(string ghiChuGro)
        {
            if (string.IsNullOrWhiteSpace(ghiChuGro)) return false;
            string vn = Translate(ChuanHoaGhiChuGro(ghiChuGro));
            if (string.IsNullOrEmpty(vn)) return false;
            return vn.Normalize(System.Text.NormalizationForm.FormC)
                     .IndexOf(NghiaKhacChu.Normalize(System.Text.NormalizationForm.FormC), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Cụm nghĩa tiếng Việt đánh dấu tấm cần xử lý đổi nét sang 4-SHT trong từ điển ghi chú.</summary>
        public const string NghiaXuLyShtCoDau = "xử lý sht";
        public const string NghiaXuLyShtKhongDau = "xu ly sht";

        /// <summary>
        /// Kiểm tra ghi chú GRO (chuỗi XData bango hoặc chuỗi đã dịch) có NGHĨA "xử lý SHT" không —
        /// dịch y hệt cột Ghi chú của Danh Sách Tấm (ChuanHoaGhiChuGro → Translate), rồi tìm cụm
        /// "xử lý SHT" hoặc "xu ly SHT" (không phân biệt hoa thường, chuẩn hoá Unicode NFC).
        /// Lisp gọi qua (gro_net_xu_ly_sht "ghi chu") để cảnh báo tấm cần đổi nét sang 4-SHT.
        /// </summary>
        public static bool LaXuLySht(string ghiChuGro)
        {
            if (string.IsNullOrWhiteSpace(ghiChuGro)) return false;
            string s = ChuanHoaGhiChuGro(ghiChuGro);
            string vn = Translate(s);
            if (string.IsNullOrEmpty(vn)) vn = s;

            string norm = vn.Normalize(System.Text.NormalizationForm.FormC);
            return norm.IndexOf(NghiaXuLyShtCoDau.Normalize(System.Text.NormalizationForm.FormC), StringComparison.OrdinalIgnoreCase) >= 0
                || norm.IndexOf(NghiaXuLyShtKhongDau, StringComparison.OrdinalIgnoreCase) >= 0
                || (norm.IndexOf("sht", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (norm.IndexOf("xử lý".Normalize(System.Text.NormalizationForm.FormC), StringComparison.OrdinalIgnoreCase) >= 0
                     || norm.IndexOf("xu ly", StringComparison.OrdinalIgnoreCase) >= 0));
        }


        public static string ExtractAndTranslate(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
            EnsureInitialized();

            string decoded = DecodeUnicodeEscapes(rawText);
            lock (_lock)
            {
                // Ưu tiên từ khóa dài hơn trước để khớp chính xác
                var ordered = _dictionary.OrderByDescending(k => k.Key.Length);
                foreach (var kvp in ordered)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key)) continue;

                    if (rawText.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        decoded.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return kvp.Value;
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Khôi phục từ điển về danh mục mặc định ban đầu (dùng cho test hoặc reset).
        /// </summary>
        public static void ResetToDefaults()
        {
            lock (_lock)
            {
                ResetDictionaryToDefaults();
                try
                {
                    using (RegistryKey root = Registry.CurrentUser.OpenSubKey(RegSubKey, true))
                    {
                        if (root != null)
                        {
                            foreach (string name in root.GetValueNames())
                            {
                                if (!name.StartsWith("__"))
                                    root.DeleteValue(name, false);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private static string ReplaceIgnoreCase(string input, string pattern, string replacement)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern)) return input;
            int pos = 0;
            while ((pos = input.IndexOf(pattern, pos, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                input = input.Substring(0, pos) + replacement + input.Substring(pos + pattern.Length);
                pos += replacement.Length;
            }
            return input;
        }
    }
}
