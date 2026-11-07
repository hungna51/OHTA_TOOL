using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một file đính kèm (SharePoint) của tin nhắn đơn.</summary>
    public sealed class FileDon
    {
        public string Ten;
        public string Url;
    }

    /// <summary>Một đơn = một tin nhắn gốc trong kênh có ít nhất 1 file DWG/DXF/PDF/Excel/CSV.</summary>
    public sealed class DonTeams
    {
        public string Id, NhomId, KenhId, TenKenh, NguoiGui, NoiDung;
        public DateTime Luc;
        public List<FileDon> Files = new List<FileDon>();
        public List<string> TagId = new List<string>();
        public List<string> TagTen = new List<string>();

        public bool CoTag(string userId) =>
            !string.IsNullOrEmpty(userId) && TagId.Any(t => string.Equals(t, userId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Phân tích JSON trả về của Microsoft Graph (logic thuần, test không cần CAD / mạng).</summary>
    public static class PhanTichTeams
    {
        public static readonly string[] DuoiDon = { ".dwg", ".dxf", ".pdf", ".xlsx", ".xlsm", ".xls", ".csv" };

        public static bool LaFileDon(string ten) =>
            !string.IsNullOrEmpty(ten) && DuoiDon.Contains(Path.GetExtension(ten).ToLowerInvariant());

        private static JsonObject Obj(object o, string key) => (o as JsonObject)?.Get(key) as JsonObject;
        private static List<object> Mang(object o, string key) => (o as JsonObject)?.Get(key) as List<object> ?? new List<object>();

        /// <summary>Đọc teams.json {"tenantId","clientId"}; thiếu khoá = "" (bỏ khoảng trắng thừa khi dán).</summary>
        public static (string Tenant, string Client) DocMa(string json)
        {
            var o = JsonLite.Parse(json) as JsonObject;
            return ((o?.GetString("tenantId") ?? "").Trim(), (o?.GetString("clientId") ?? "").Trim());
        }

        public static string GhiMa(string tenant, string client)
        {
            var o = new JsonObject();
            o.Set("tenantId", (tenant ?? "").Trim());
            o.Set("clientId", (client ?? "").Trim());
            return JsonLite.Serialize(o);
        }

        /// <summary>Danh sách {id, displayName} (joinedTeams, channels).</summary>
        public static List<KeyValuePair<string, string>> DocDanhSach(string json)
        {
            return Mang(JsonLite.Parse(json), "value").OfType<JsonObject>()
                .Select(v => new KeyValuePair<string, string>(v.GetString("id"), v.GetString("displayName")))
                .Where(kv => !string.IsNullOrEmpty(kv.Key)).ToList();
        }

        /// <summary>Lọc tin nhắn kênh thành đơn: bỏ tin hệ thống, tin đã xoá, tin không có file đơn.</summary>
        public static List<DonTeams> DocDon(string json, string nhomId, string kenhId, string tenKenh)
        {
            var kq = new List<DonTeams>();
            foreach (JsonObject m in Mang(JsonLite.Parse(json), "value").OfType<JsonObject>())
            {
                if (m.GetString("messageType") != "message" || m.Get("deletedDateTime") != null)
                    continue;

                var files = Mang(m, "attachments").OfType<JsonObject>()
                    .Where(a => a.GetString("contentType") == "reference" && LaFileDon(a.GetString("name")))
                    .Select(a => new FileDon { Ten = a.GetString("name"), Url = a.GetString("contentUrl") })
                    .ToList();
                if (files.Count == 0)
                    continue;

                var don = new DonTeams
                {
                    Id = m.GetString("id"),
                    NhomId = nhomId,
                    KenhId = kenhId,
                    TenKenh = tenKenh,
                    NguoiGui = Obj(Obj(m, "from"), "user")?.GetString("displayName") ?? "?",
                    NoiDung = BoHtml(Obj(m, "body")?.GetString("content")),
                    Files = files,
                };
                DateTime.TryParse(m.GetString("createdDateTime"), CultureInfo.InvariantCulture,
                                  DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out don.Luc);
                don.Luc = don.Luc.ToLocalTime();

                foreach (JsonObject t in Mang(m, "mentions").OfType<JsonObject>())
                {
                    JsonObject u = Obj(Obj(t, "mentioned"), "user");
                    if (u == null) continue;
                    don.TagId.Add(u.GetString("id"));
                    don.TagTen.Add(u.GetString("displayName") ?? t.GetString("mentionText"));
                }
                kq.Add(don);
            }
            return kq;
        }

        /// <summary>HTML nội dung tin nhắn -> chữ thường (bỏ thẻ, giải mã &amp;, gộp khoảng trắng).</summary>
        public static string BoHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            string s = Regex.Replace(html, @"<(br|/p|/div)\s*/?>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "<[^>]+>", "");
            s = WebUtility.HtmlDecode(s);
            s = Regex.Replace(s, @"[ \t ]+", " ");
            s = Regex.Replace(s, @"\s*\n\s*", "\n");
            return s.Trim();
        }

        /// <summary>Mã chia sẻ của Graph cho URL SharePoint: "u!" + base64url (không dấu =).</summary>
        public static string MaShare(string url)
        {
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(url ?? ""));
            return "u!" + b64.TrimEnd('=').Replace('/', '_').Replace('+', '-');
        }

        /// <summary>Tên an toàn làm thư mục/file Windows.</summary>
        public static string TenAnToan(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "_";
            var bad = Path.GetInvalidFileNameChars();
            string r = new string(s.Select(c => bad.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
            return r.Length == 0 ? "_" : (r.Length > 60 ? r.Substring(0, 60) : r);
        }

        /// <summary>Thư mục lưu một đơn: &lt;gốc&gt;\&lt;kênh&gt;\yyyyMMdd_HHmm_&lt;người gửi&gt;.</summary>
        public static string ThuMucDon(string goc, DonTeams d) =>
            Path.Combine(goc, TenAnToan(d.TenKenh), d.Luc.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture) + "_" + TenAnToan(d.NguoiGui));

        /// <summary>Đọc access_token / refresh_token / expires_in từ phản hồi token của Entra ID.</summary>
        public static (string Access, string Refresh, int HetSau) DocToken(string json)
        {
            var o = JsonLite.Parse(json) as JsonObject;
            int.TryParse(o?.Get("expires_in")?.ToString(), out int het);
            return (o?.GetString("access_token"), o?.GetString("refresh_token"), het);
        }
    }
}
