using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một tab của kênh. ListUrl = URL danh sách Microsoft Lists, null = loại tab chưa hỗ trợ.</summary>
    public sealed class TabTeams
    {
        public string Id, Ten, ListUrl;
    }

    /// <summary>Một cột của danh sách: Ten = tên nội bộ (khoá trong fields), TieuDe = tên hiển thị.</summary>
    public sealed class CotList
    {
        public string Ten, TieuDe, Loai; // Loai: personOrGroup | dateTime | choice | lookup | boolean | number | text | khac
        public List<string> LuaChon = new List<string>(); // choice: các lựa chọn
        public bool ChonNhieu;  // choice hiện dạng ô tích (checkBoxes) = chọn nhiều giá trị
        public bool CoGio;      // dateTime có giờ (format = dateTime)
        public bool ChuHtml;    // text dạng richText — chưa sửa được (sẽ mất định dạng)
        public bool NhieuDong;  // text nhiều dòng

        /// <summary>Cột OhtaTeams sửa được. Người / tra cứu / chữ có định dạng: chỉ xem (sửa trên Teams).</summary>
        public bool SuaDuoc =>
            Loai == "number" || Loai == "choice" || Loai == "boolean" || Loai == "dateTime" || (Loai == "text" && !ChuHtml);
    }

    /// <summary>Tên nội bộ các cột chính của danh sách đơn (null = danh sách không có cột đó).</summary>
    public sealed class CotChinh
    {
        public string Han, TrangThai, StatusJp, TrienKhai, Check;
    }

    /// <summary>Một đơn = một dòng của danh sách Lists.</summary>
    public sealed class DonTeams
    {
        public string Id, WebUrl, Title, TrangThai, StatusJp, TrienKhai, Check;
        public string ETag; // phiên bản dòng — gửi kèm khi sửa để không ghi đè sửa của người khác
        public DateTime? Han;
        public bool DaXong;
        /// <summary>Giá trị thô theo tên nội bộ cột (chuỗi / số / bool / mảng) — điền sẵn vào form sửa.</summary>
        public Dictionary<string, object> Tho = new Dictionary<string, object>();
        /// <summary>Mọi cột có giá trị, theo thứ tự cột của danh sách: (tên hiển thị, giá trị).</summary>
        public List<KeyValuePair<string, string>> TatCa = new List<KeyValuePair<string, string>>();

        public bool CuaToi(string ten) =>
            !string.IsNullOrEmpty(ten) && ((TrienKhai ?? "").Contains(ten) || (Check ?? "").Contains(ten));
    }

    /// <summary>Phân tích JSON trả về của Microsoft Graph (logic thuần, test không cần CAD / mạng).</summary>
    public static class PhanTichTeams
    {
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

        /// <summary>@odata.nextLink của một trang kết quả (null = hết).</summary>
        public static string TrangSau(string json) => (JsonLite.Parse(json) as JsonObject)?.GetString("@odata.nextLink");

        /// <summary>Tabs của kênh; tab nào có URL .../Lists/... trong cấu hình là tab Lists.</summary>
        public static List<TabTeams> DocTab(string json)
        {
            var kq = new List<TabTeams>();
            foreach (JsonObject t in Mang(JsonLite.Parse(json), "value").OfType<JsonObject>())
            {
                var cfg = t.Get("configuration") as JsonObject;
                string chuoi = string.Join(" ", cfg?.GetString("contentUrl"), cfg?.GetString("websiteUrl"), cfg?.GetString("entityId"), t.GetString("webUrl"));
                kq.Add(new TabTeams { Id = t.GetString("id"), Ten = t.GetString("displayName") ?? "?", ListUrl = TimUrlList(chuoi) });
            }
            return kq;
        }

        private static readonly Regex UrlList = new Regex(@"https://[^\s""'&?#]+?/Lists/[^/\s""'&?#]+", RegexOptions.IgnoreCase);

        private static readonly Regex ThamSoFileBrowser = new Regex(@"[?&]fileBrowser=([^&]+)", RegexOptions.IgnoreCase);

        /// <summary>URL danh sách SharePoint ("https://x.sharepoint.com/sites/A/Lists/B") trong chuỗi cấu hình tab (có thể bị mã hoá URL nhiều lớp).</summary>
        public static string TimUrlList(string s)
        {
            // Tab Lists của Teams: ...FileBrowser.aspx?fileBrowser={"entry":{"sharePoint":{"byPath":{"list":"/sites/A/Lists/B","web":"https://x/sites/A"}}}}
            // "list" có thể là đường dẫn tương đối và có dấu cách
            Match fb = ThamSoFileBrowser.Match(s ?? "");
            if (fb.Success)
            {
                try
                {
                    var o = JsonLite.Parse(Uri.UnescapeDataString(fb.Groups[1].Value)) as JsonObject;
                    var byPath = ((((o?.Get("entry") as JsonObject)?.Get("sharePoint")) as JsonObject)?.Get("byPath")) as JsonObject;
                    string list = byPath?.GetString("list"), web = byPath?.GetString("web");
                    if (!string.IsNullOrEmpty(list) && list.IndexOf("/Lists/", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (list.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return list;
                        if (Uri.TryCreate(web, UriKind.Absolute, out Uri w)) return w.Scheme + "://" + w.Host + list;
                    }
                }
                catch (Exception) { } // không phải JSON → thử cách dưới
            }
            for (int i = 0; i < 4 && !string.IsNullOrEmpty(s); i++)
            {
                Match m = UrlList.Match(s);
                if (m.Success) return m.Value;
                string giai = Uri.UnescapeDataString(s);
                if (giai == s) break;
                s = giai;
            }
            return null;
        }

        /// <summary>Tách URL danh sách thành (host, đường dẫn site) để gọi /sites/{host}:{path}.</summary>
        public static (string Host, string DuongSite) TachSite(string listUrl)
        {
            var u = new Uri(listUrl);
            string p = Uri.UnescapeDataString(u.AbsolutePath);
            int i = p.IndexOf("/Lists/", StringComparison.OrdinalIgnoreCase);
            return (u.Host, (i > 0 ? p.Substring(0, i) : "").TrimEnd('/'));
        }

        private static string ChuanUrl(string s) => Uri.UnescapeDataString(s ?? "").TrimEnd('/').ToLowerInvariant();

        /// <summary>Id của danh sách có webUrl trùng URL tab (trong /sites/{id}/lists).</summary>
        public static string TimListId(string json, string listUrl)
        {
            string can = ChuanUrl(listUrl);
            return Mang(JsonLite.Parse(json), "value").OfType<JsonObject>()
                .FirstOrDefault(l => ChuanUrl(l.GetString("webUrl")) == can)?.GetString("id");
        }

        /// <summary>Id danh sách người dùng của site (User Information List, URL .../_catalogs/users) trong /lists?$select=id,webUrl,system.</summary>
        public static string TimListNguoi(string json) =>
            Mang(JsonLite.Parse(json), "value").OfType<JsonObject>()
                .FirstOrDefault(l => (l.GetString("webUrl") ?? "").TrimEnd('/').EndsWith("/_catalogs/users", StringComparison.OrdinalIgnoreCase))?.GetString("id");

        private static readonly string[] CotHeThong ={ "Attachments", "ContentType", "_ColorTag", "ComplianceAssetId", "Edit", "LinkTitle", "LinkTitleNoMenu", "DocIcon", "ItemChildCount", "FolderChildCount", "AppAuthor", "AppEditor" };
        private static readonly string[] LoaiCot ={ "personOrGroup", "dateTime", "choice", "lookup", "boolean", "number", "text" };

        /// <summary>Cột hiển thị được của danh sách (bỏ cột ẩn và cột hệ thống chỉ đọc trừ Title).</summary>
        public static List<CotList> DocCot(string json)
        {
            var kq = new List<CotList>();
            foreach (JsonObject c in Mang(JsonLite.Parse(json), "value").OfType<JsonObject>())
            {
                string ten = c.GetString("name");
                if (string.IsNullOrEmpty(ten) || Equals(c.Get("hidden"), true) || CotHeThong.Contains(ten)
                    || ten.StartsWith("teamsConversationMetadata", StringComparison.Ordinal) || ten.StartsWith("_FormSourceId", StringComparison.Ordinal)) continue;
                if (Equals(c.Get("readOnly"), true) && ten != "Title") continue;
                var cot = new CotList { Ten = ten, TieuDe = c.GetString("displayName") ?? ten,
                                        Loai = LoaiCot.FirstOrDefault(l => c.Get(l) != null) ?? "khac" };
                if (c.Get("choice") is JsonObject ch)
                {
                    cot.LuaChon = (ch.Get("choices") as List<object> ?? new List<object>()).OfType<string>().ToList();
                    cot.ChonNhieu = Equals(ch.Get("allowMultiple"), true) || ch.GetString("displayAs") == "checkBoxes";
                }
                if (c.Get("dateTime") is JsonObject dt) cot.CoGio = dt.GetString("format") == "dateTime";
                if (c.Get("text") is JsonObject tx)
                {
                    cot.ChuHtml = tx.GetString("textType") == "richText";
                    cot.NhieuDong = Equals(tx.Get("allowMultipleLines"), true);
                }
                kq.Add(cot);
            }
            return kq;
        }

        /// <summary>Chữ thường, bỏ dấu tiếng Việt (đ → d) để so tên cột.</summary>
        public static string BoDau(string s)
        {
            var sb = new StringBuilder();
            foreach (char ch in (s ?? "").Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant().Trim();
        }

        /// <summary>Tìm cột chính theo tên hiển thị (Hạn hoàn thành, Trạng thái, ステータス, Người triển khai, Người check…).</summary>
        public static CotChinh KhopCot(List<CotList> cots)
        {
            string Tim(Func<string, bool> khop, string uuTienLoai = null) =>
                (cots.FirstOrDefault(c => khop(BoDau(c.TieuDe)) && (uuTienLoai == null || c.Loai == uuTienLoai))
                 ?? cots.FirstOrDefault(c => khop(BoDau(c.TieuDe))))?.Ten;
            return new CotChinh
            {
                Han = Tim(t => t.Contains("han hoan thanh"), "dateTime"),
                TrangThai = Tim(t => t == "trang thai" || t.StartsWith("trang thai")),
                StatusJp = Tim(t => t.Contains("ステータス")),
                TrienKhai = Tim(t => t.Contains("trien khai"), "personOrGroup"),
                Check = Tim(t => t.Contains("check"), "personOrGroup"),
            };
        }

        /// <summary>Giá trị một cột của dòng thành chữ. tenNguoi: LookupId → tên (cột người).</summary>
        public static string GiaTri(JsonObject f, CotList c, Func<string, string> tenNguoi)
        {
            object v = f.Get(c.Ten);
            if (v == null)
            {
                object id = f.Get(c.Ten + "LookupId");
                if (id is List<object> ds) return string.Join(", ", ds.Select(x => tenNguoi(x?.ToString())));
                return id == null ? "" : tenNguoi(id.ToString());
            }
            switch (v)
            {
                case List<object> ds:
                    return string.Join(", ", ds.Select(x => x is JsonObject o ? o.GetString("LookupValue") ?? o.GetString("Email") : x?.ToString())
                                               .Where(x => !string.IsNullOrEmpty(x)));
                case JsonObject o: // cột người một giá trị: {LookupId, LookupValue, Email}
                    return o.GetString("LookupValue") ?? o.GetString("Email") ?? tenNguoi(o.Get("LookupId")?.ToString());
                case bool b:
                    return b ? "Có" : "Không";
                case string s when c.Loai == "dateTime":
                    return DocNgay(s)?.ToString(c.CoGio ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy") ?? s;
                case string s:
                    return s.Contains("<") ? BoHtml(s) : s.Trim();
                default:
                    return v.ToString();
            }
        }

        /// <summary>Ngày ISO UTC của SharePoint → ngày giờ máy.</summary>
        public static DateTime? DocNgay(string s) =>
            DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime d)
                ? d.ToLocalTime() : (DateTime?)null;

        /// <summary>Một trang /items?$expand=fields → đơn.</summary>
        public static List<DonTeams> DocDon(string json, List<CotList> cots, CotChinh chinh, Func<string, string> tenNguoi)
        {
            var kq = new List<DonTeams>();
            foreach (JsonObject it in Mang(JsonLite.Parse(json), "value").OfType<JsonObject>())
            {
                var f = it.Get("fields") as JsonObject;
                if (f == null) continue;
                string Gt(string ten) { CotList c = cots.FirstOrDefault(x => x.Ten == ten); return c == null ? "" : GiaTri(f, c, tenNguoi); }

                var d = new DonTeams
                {
                    Id = it.GetString("id"),
                    ETag = it.GetString("eTag"),
                    WebUrl = it.GetString("webUrl"),
                    Title = f.GetString("Title") ?? "",
                    TrangThai = Gt(chinh.TrangThai),
                    StatusJp = Gt(chinh.StatusJp),
                    TrienKhai = Gt(chinh.TrienKhai),
                    Check = Gt(chinh.Check),
                    Han = chinh.Han == null ? null : DocNgay(f.GetString(chinh.Han)),
                };
                // TRẠNG THÁI chọn nhiều giá trị ("Đã hoàn thành, Đã nhập giá") → xong nếu có "Đã hoàn thành" / "STOP";
                // ステータス "完了" / "キャンセル" cũng ẩn như đơn xong
                d.DaXong = d.TrangThai.Split(',').Any(x => BoDau(x) == "da hoan thanh" || BoDau(x) == "stop")
                        || d.StatusJp.Split(',').Any(x => x.Trim() == "完了" || x.Trim() == "キャンセル");
                foreach (CotList c in cots)
                {
                    if (c.SuaDuoc && f.Get(c.Ten) != null) d.Tho[c.Ten] = f.Get(c.Ten);
                    string g = GiaTri(f, c, tenNguoi);
                    if (g.Length > 0) d.TatCa.Add(new KeyValuePair<string, string>(c.TieuDe, g));
                }
                kq.Add(d);
            }
            return kq;
        }

        /// <summary>JSON thô của một danh sách: cột, các trang người dùng (User Information List), các trang dòng.</summary>
        public sealed class ThoList
        {
            public string Cot = "";
            public List<string> Nguoi = new List<string>(), Dong = new List<string>();
        }

        /// <summary>
        /// LookupId người mà các dòng chỉ trả id (cột người không kèm LookupValue) — chỉ tra những người này,
        /// không tải cả User Information List (~17.000 người, 18 trang).
        /// </summary>
        public static List<string> IdNguoiCan(ThoList t)
        {
            var cotNguoi = new HashSet<string>(DocCot(t.Cot).Where(c => c.Loai == "personOrGroup").Select(c => c.Ten + "LookupId"));
            var kq = new HashSet<string>();
            foreach (string trang in t.Dong)
                foreach (JsonObject it in Mang(JsonLite.Parse(trang), "value").OfType<JsonObject>())
                    foreach (var kv in (it.Get("fields") as JsonObject)?.Items ?? new List<KeyValuePair<string, object>>())
                    {
                        if (!cotNguoi.Contains(kv.Key)) continue;
                        if (kv.Value is List<object> ds) foreach (object x in ds) { if (x != null) kq.Add(x.ToString()); }
                        else if (kv.Value != null) kq.Add(kv.Value.ToString());
                    }
            return kq.OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        /// <summary>Phân tích trọn danh sách (chạy được ở luồng nền: không đụng giao diện).</summary>
        public static List<DonTeams> DocToanBo(ThoList t)
        {
            var cots = DocCot(t.Cot);
            var chinh = KhopCot(cots);
            var nguoi = new Dictionary<string, string>();
            foreach (string trang in t.Nguoi)
                foreach (var kv in DocNguoi(trang)) nguoi[kv.Key] = kv.Value;
            var kq = new List<DonTeams>();
            foreach (string trang in t.Dong)
                kq.AddRange(DocDon(trang, cots, chinh, id => id != null && nguoi.TryGetValue(id, out string ten) ? ten : "#" + id));
            return kq;
        }

        /// <summary>User Information List của site: LookupId → tên hiển thị (cột người chỉ trả LookupId).</summary>
        public static Dictionary<string, string> DocNguoi(string json)
        {
            var kq = new Dictionary<string, string>();
            foreach (JsonObject it in Mang(JsonLite.Parse(json), "value").OfType<JsonObject>())
            {
                string ten = (it.Get("fields") as JsonObject)?.GetString("Title");
                if (it.GetString("id") != null && ten != null) kq[it.GetString("id")] = ten;
            }
            return kq;
        }

        /// <summary>HTML (cột chữ nhiều dòng) → chữ thường (bỏ thẻ, giải mã &amp;, gộp khoảng trắng).</summary>
        public static string BoHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            string s = Regex.Replace(html, @"<(br|/p|/div)\s*/?>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "<[^>]+>", "");
            s = WebUtility.HtmlDecode(s);
            s = Regex.Replace(s, @"[ \t ]+", " ");
            s = Regex.Replace(s, @"\s*\n\s*", "\n");
            return s.Trim();
        }

        /// <summary>Đọc access_token / refresh_token / expires_in từ phản hồi token của Entra ID.</summary>
        public static (string Access, string Refresh, int HetSau) DocToken(string json)
        {
            var o = JsonLite.Parse(json) as JsonObject;
            int.TryParse(o?.Get("expires_in")?.ToString(), out int het);
            return (o?.GetString("access_token"), o?.GetString("refresh_token"), het);
        }
    }
}
