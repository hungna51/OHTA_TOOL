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
        public int SoTep; // tệp / thẻ đính kèm trong tin
    }

    public sealed class TepDinhKem
    {
        public string Ten, DuongDan; // DuongDan = ServerRelativeUrl trên SharePoint
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
            if (body?.GetString("contentType") == "html") chu = PhanTichTeams.BoHtml(chu);
            return new TinNhan
            {
                Id = m.GetString("id"),
                Nguoi = nguoi,
                Chu = chu,
                Luc = PhanTichTeams.DocNgay(m.GetString("createdDateTime")),
                // chỉ tệp thật (contentType "reference"); thẻ "Về: <đơn>" Teams tự gắn vào tin gốc không tính
                SoTep = (m.Get("attachments") as List<object> ?? new List<object>()).OfType<JsonObject>().Count(a => a.GetString("contentType") == "reference"),
            };
        }

        /// <summary>Một tin (object) hoặc một trang {"value":[...]} → các tin, cũ trước. Tin đã xoá (deletedDateTime) bỏ qua.</summary>
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
