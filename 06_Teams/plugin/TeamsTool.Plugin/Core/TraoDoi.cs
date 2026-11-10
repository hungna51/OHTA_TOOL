using System;
using System.Collections.Generic;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một chuỗi chat Teams gắn với đơn (cột ẩn teamsConversationMetadata_*): tin gốc ConvId trong kênh ChannelId của nhóm TeamInternalId.</summary>
    public sealed class HoiThoai
    {
        public string ConvId, ChannelId, TeamInternalId;
    }

    public sealed class TinNhan
    {
        public string Id, Nguoi, Chu;
        public DateTime? Luc;
        public List<string> Anh = new List<string>();          // ảnh dán trong tin: URL hostedContents của Graph
        public List<TepDinhKem> Tep = new List<TepDinhKem>();  // tệp gửi kèm: DuongDan = URL SharePoint đầy đủ
        public int SoTep => Tep.Count;
    }

    public sealed class TepDinhKem
    {
        public string Ten, DuongDan; // tệp đính kèm của đơn: ServerRelativeUrl; tệp trong chat: URL SharePoint đầy đủ
    }

    /// <summary>Đọc chat + tệp đính kèm của đơn (logic thuần, test không cần mạng).</summary>
    public static class TraoDoi
    {
        /// <summary>{"v1":[{conversationId, channelId, teamId}]} → các chuỗi chat (bỏ phần tử thiếu). Chuỗi hỏng → rỗng.</summary>
        public static List<HoiThoai> DocHoiThoai(string meta)
        {
            var kq = new List<HoiThoai>();
            if (string.IsNullOrWhiteSpace(meta)) return kq;
            JsonObject o;
            try { o = JsonLite.Parse(meta) as JsonObject; }
            catch (Exception) { return kq; }
            foreach (JsonObject x in (o?.Get("v1") as List<object> ?? new List<object>()).OfType<JsonObject>())
            {
                var h = new HoiThoai { ConvId = x.Get("conversationId")?.ToString(), ChannelId = x.GetString("channelId"), TeamInternalId = x.GetString("teamId") };
                if (!string.IsNullOrEmpty(h.ConvId) && !string.IsNullOrEmpty(h.ChannelId) && !string.IsNullOrEmpty(h.TeamInternalId)) kq.Add(h);
            }
            return kq;
        }

        private static TinNhan DocMotTin(JsonObject m)
        {
            var from = m.Get("from") as JsonObject;
            string nguoi = (from?.Get("user") as JsonObject)?.GetString("displayName")
                        ?? (from?.Get("application") as JsonObject)?.GetString("displayName") ?? "";
            var body = m.Get("body") as JsonObject;
            string chu = body?.GetString("content") ?? "";
            bool html = body?.GetString("contentType") == "html";
            var t = new TinNhan
            {
                Id = m.GetString("id"),
                Nguoi = nguoi,
                Chu = html ? PhanTichTeams.BoHtml(chu) : chu,
                Luc = PhanTichTeams.DocNgay(m.GetString("createdDateTime")),
                Anh = html ? AnhTrongTin(chu) : new List<string>(),
                // chỉ tệp thật (contentType "reference"); thẻ "Về: <đơn>" (tabReference) Teams tự gắn vào tin gốc bỏ qua
                Tep = (m.Get("attachments") as List<object> ?? new List<object>()).OfType<JsonObject>()
                        .Where(a => a.GetString("contentType") == "reference" && !string.IsNullOrEmpty(a.GetString("contentUrl")))
                        .Select(a => new TepDinhKem { Ten = a.GetString("name") ?? "tệp", DuongDan = a.GetString("contentUrl") }).ToList(),
            };
            return t;
        }

        /// <summary>
        /// Ảnh dán trong tin: src của &lt;img&gt; trỏ tới hostedContents trên Graph (tải bằng quyền đọc chat).
        /// Ảnh khác (emoji của Teams trên CDN…) bỏ qua.
        /// </summary>
        public static List<string> AnhTrongTin(string html) =>
            System.Text.RegularExpressions.Regex.Matches(html ?? "", "<img[^>]*?\\ssrc=\"([^\"]+)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(x => System.Net.WebUtility.HtmlDecode(x.Groups[1].Value))
                .Where(u => u.StartsWith("https://graph.microsoft.com/", StringComparison.OrdinalIgnoreCase) && u.Contains("/hostedContents/"))
                .Distinct().ToList();

        /// <summary>Mã chia sẻ cho Graph /shares/{mã}/driveItem/content: "u!" + base64url(URL) — tải tệp gửi trong chat.</summary>
        public static string MaShare(string url) =>
            "u!" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url ?? "")).TrimEnd('=').Replace('/', '_').Replace('+', '-');

        /// <summary>Một tin (object) hoặc một trang {"value":[...]} → các tin, cũ trước. Tin đã xoá (deletedDateTime) bỏ qua.</summary>
        /// <summary>
        /// Các kênh chat dùng nhiều nhất trong các đơn (đếm theo kênh, nhiều trước) — trang chi tiết luôn hiện đủ các kênh này,
        /// kênh đơn chưa có chuỗi thì cho tạo. HoiThoai trả về chỉ có ChannelId + TeamInternalId (ConvId = null).
        /// </summary>
        public static List<HoiThoai> KenhPhoBien(IEnumerable<DonTeams> don, int soKenh = 2) =>
            don.SelectMany(d => DocHoiThoai(d.HoiThoaiMeta))
               .GroupBy(h => h.ChannelId + "|" + h.TeamInternalId)
               .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
               .Take(soKenh)
               .Select(g => new HoiThoai { ChannelId = g.First().ChannelId, TeamInternalId = g.First().TeamInternalId })
               .ToList();

        /// <summary>Metadata cũ + một chuỗi mới (giữ nguyên các chuỗi cũ) → chuỗi JSON để ghi lại cột ẩn của đơn.</summary>
        public static string ThemHoiThoai(string meta, HoiThoai moi)
        {
            var ds = new List<object>();
            foreach (HoiThoai h in DocHoiThoai(meta).Concat(new[] { moi }))
            {
                var o = new JsonObject();
                o.Set("conversationId", h.ConvId);
                o.Set("channelId", h.ChannelId);
                o.Set("teamId", h.TeamInternalId);
                ds.Add(o);
            }
            var goc = new JsonObject();
            goc.Set("v1", ds);
            return JsonLite.Serialize(goc).Trim();
        }

        /// <summary>HTML tin mở đầu trao đổi về đơn: "Về đơn &lt;Title&gt;" (link tới đơn trên Lists) + nội dung người gõ.</summary>
        public static string HtmlMoDau(string title, string webUrl, string chu)
        {
            string ve = string.IsNullOrEmpty(webUrl) ? "<b>Về đơn " + HtmlTraLoi(title) + "</b>"
                                                     : "<b>Về đơn <a href=\"" + webUrl.Replace("\"", "%22") + "\">" + HtmlTraLoi(title) + "</a></b>";
            return "<p>" + ve + "</p><p>" + HtmlTraLoi(chu) + "</p>";
        }

        /// <summary>
        /// Body POST tin mở trao đổi. tabId != null: kèm thẻ "Về: &lt;đơn&gt;" như Lists tạo — đính kèm loại tabReference,
        /// id "tab::&lt;id tab Lists&gt;", thẻ đặt ở cuối nội dung bằng thẻ &lt;attachment&gt;.
        /// </summary>
        public static string BodyMoDau(string html, string tabId, string title)
        {
            var b = new JsonObject();
            b.Set("contentType", "html");
            var o = new JsonObject();
            if (string.IsNullOrEmpty(tabId))
            {
                b.Set("content", html);
                o.Set("body", b);
                return JsonLite.Serialize(o);
            }
            string id = "tab::" + tabId;
            b.Set("content", html + "<attachment id=\"" + id + "\"></attachment>");
            var the = new JsonObject();
            the.Set("id", id);
            the.Set("contentType", "tabReference");
            the.Set("name", "Về: " + title);
            o.Set("body", b);
            o.Set("attachments", new List<object> { the });
            return JsonLite.Serialize(o);
        }

        public static List<TinNhan> DocTin(string json)
        {
            var goc = JsonLite.Parse(json) as JsonObject;
            if (goc == null) return new List<TinNhan>();
            var ds = goc.Get("value") is List<object> v ? v.OfType<JsonObject>() : new[] { goc };
            return ds.Where(m => m.Get("deletedDateTime") == null).Select(DocMotTin)
                     .OrderBy(t => t.Luc ?? DateTime.MinValue).ToList();
        }

        /// <summary>SharePoint …/AttachmentFiles (odata=nometadata) → tệp.</summary>
        public static List<TepDinhKem> DocTep(string json) =>
            ((JsonLite.Parse(json) as JsonObject)?.Get("value") as List<object> ?? new List<object>()).OfType<JsonObject>()
                .Select(t => new TepDinhKem { Ten = t.GetString("FileName"), DuongDan = t.GetString("ServerRelativeUrl") })
                .Where(t => !string.IsNullOrEmpty(t.Ten) && !string.IsNullOrEmpty(t.DuongDan)).ToList();

        /// <summary>
        /// URL tải nội dung tệp qua SharePoint REST. Đường dẫn trực tiếp (/sites/…/Attachments/…) trả 401 với token OAuth —
        /// phải đi qua /_api/…/$value. Dấu ' trong đường dẫn nhân đôi theo cú pháp OData.
        /// </summary>
        public static string UrlTaiTep(string web, string duongDan) =>
            web + "/_api/web/GetFileByServerRelativePath(decodedurl='" + Uri.EscapeDataString((duongDan ?? "").Replace("'", "''")) + "')/$value";

        /// <summary>Chữ người gõ → HTML của tin trả lời (mã hoá ký tự đặc biệt, xuống dòng = &lt;br&gt;).</summary>
        public static string HtmlTraLoi(string chu) =>
            // tự mã hoá 4 ký tự HTML: WebUtility.HtmlEncode của net48 đổi cả chữ có dấu thành &#227;
            string.Join("<br>", (chu ?? "").Trim().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;")
                                             .Replace("\r\n", "\n").Split('\n'));

        /// <summary>Body POST …/replies.</summary>
        public static string BodyTraLoi(string chu)
        {
            var b = new JsonObject();
            b.Set("contentType", "html");
            b.Set("content", HtmlTraLoi(chu));
            var o = new JsonObject();
            o.Set("body", b);
            return JsonLite.Serialize(o);
        }
    }
}
