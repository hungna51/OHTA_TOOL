using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>
    /// Sửa một đơn (logic thuần, test không cần mạng). Giá trị trong form theo loại cột:
    /// text / choice một giá trị → string; choice nhiều → List&lt;string&gt;; number → double?; dateTime → DateTime? (giờ máy); boolean → bool.
    /// null / "" = để trống.
    /// </summary>
    public static class SuaDon
    {
        /// <summary>Phần tử thô của cột người → (LookupId, tên hoặc null nếu chỉ có id).</summary>
        private static IEnumerable<(string Id, string Ten)> NguoiTho(object tho)
        {
            foreach (object x in tho as List<object> ?? (tho == null ? new List<object>() : new List<object> { tho }))
            {
                if (x is JsonObject o && o.Get("LookupId") != null) yield return (o.Get("LookupId").ToString(), o.GetString("LookupValue"));
                else if (x != null && !(x is JsonObject) && x.ToString().Length > 0) yield return (x.ToString(), null);
            }
        }

        /// <summary>
        /// Danh bạ nhân viên: mọi người từng có trong cột người của các đơn (LookupId → tên), xếp theo tên.
        /// shortcut: người chưa từng được giao đơn nào không có ở đây — giao lần đầu trên Teams; cần đủ thì tải User Information List của site.
        /// </summary>
        public static List<KeyValuePair<string, string>> DanhBa(IEnumerable<DonTeams> don, IEnumerable<CotList> cots)
        {
            var cotNguoi = cots.Where(c => c.Loai == "personOrGroup").Select(c => c.Ten).ToList();
            var kq = new Dictionary<string, string>();
            foreach (DonTeams d in don)
                foreach (string ten in cotNguoi)
                    if (d.Tho.TryGetValue(ten, out object tho))
                        foreach (var (id, tenNguoi) in NguoiTho(tho))
                            if (!string.IsNullOrEmpty(tenNguoi)) kq[id] = tenNguoi;
            return kq.OrderBy(kv => kv.Value, StringComparer.CurrentCulture).ToList();
        }

        /// <summary>Tiền tố khoá danh bạ của người chưa có mã trên site: "upn:&lt;email&gt;" → lúc lưu xin mã (ensureuser) rồi thay.</summary>
        public const string ChuaCoMa = "upn:";

        /// <summary>Graph /users → (tên, email đăng nhập) của tài khoản đang bật.</summary>
        public static List<KeyValuePair<string, string>> DocNguoiCongTy(string json) =>
            ((JsonLite.Parse(json) as JsonObject)?.Get("value") as List<object> ?? new List<object>()).OfType<JsonObject>()
                .Where(u => !Equals(u.Get("accountEnabled"), false))
                .Select(u => new KeyValuePair<string, string>(u.GetString("displayName") ?? "", u.GetString("userPrincipalName") ?? u.GetString("mail") ?? ""))
                .Where(kv => kv.Key.Length > 0 && kv.Value.Length > 0).ToList();

        /// <summary>SharePoint /_api/web/siteusers (nometadata) → (LookupId, email). Email trống bỏ qua.</summary>
        public static List<KeyValuePair<string, string>> DocNguoiSite(string json) =>
            ((JsonLite.Parse(json) as JsonObject)?.Get("value") as List<object> ?? new List<object>()).OfType<JsonObject>()
                .Select(u => new KeyValuePair<string, string>(u.Get("Id")?.ToString() ?? "", u.GetString("Email") ?? ""))
                .Where(kv => kv.Key.Length > 0 && kv.Value.Length > 0).ToList();

        /// <summary>
        /// Danh bạ đầy đủ: mọi người trong công ty (khoá = LookupId của site nếu email khớp, không thì "upn:&lt;email&gt;")
        /// + người đã có trong đơn mà không thuộc công ty (khách, tài khoản cũ). Xếp theo tên.
        /// </summary>
        public static List<KeyValuePair<string, string>> GopDanhBa(List<KeyValuePair<string, string>> tuDon,
            List<KeyValuePair<string, string>> congTy, List<KeyValuePair<string, string>> site)
        {
            var maTheoEmail = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in site) maTheoEmail[kv.Value] = kv.Key;
            var kq = new Dictionary<string, string>();
            foreach (var kv in tuDon) kq[kv.Key] = kv.Value;
            foreach (var ng in congTy)
                kq[maTheoEmail.TryGetValue(ng.Value, out string id) ? id : ChuaCoMa + ng.Value] = ng.Key;
            return kq.OrderBy(kv => kv.Value, StringComparer.CurrentCulture).ToList();
        }

        /// <summary>Đơn có người (LookupId) ở bất kỳ cột người nào — lọc danh sách theo nhân viên.</summary>
        public static bool CoNguoi(DonTeams d, IEnumerable<CotList> cots, string id) =>
            cots.Where(c => c.Loai == "personOrGroup")
                .Any(c => d.Tho.TryGetValue(c.Ten, out object tho) && NguoiTho(tho).Any(x => x.Id == id));

        /// <summary>Giá trị thô của Graph → giá trị form.</summary>
        public static object GiaTriForm(CotList c, object tho)
        {
            switch (c.Loai)
            {
                case "personOrGroup": return NguoiTho(tho).Select(x => x.Id).ToList(); // danh sách LookupId (cột 1 người: 0–1 phần tử)
                case "dateTime": return PhanTichTeams.DocNgay(tho as string);
                case "number": return tho is JsonNumber n && double.TryParse(n.Raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double so) ? so : (double?)null;
                case "boolean": return tho is bool b && b;
                case "choice" when c.ChonNhieu: return (tho as List<object> ?? new List<object>()).Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                default: return tho as string ?? "";
            }
        }

        /// <summary>Hai giá trị form như nhau (không cần gửi). Ngày so tới phút, chữ bỏ khoảng trắng đầu cuối.</summary>
        public static bool GiongNhau(object a, object b)
        {
            if (a is List<string> la || b is List<string>)
                return (a as List<string> ?? new List<string>()).OrderBy(x => x).SequenceEqual((b as List<string> ?? new List<string>()).OrderBy(x => x));
            if (a is DateTime || b is DateTime)
                return (a as DateTime?)?.ToString("yyyyMMddHHmm") == (b as DateTime?)?.ToString("yyyyMMddHHmm");
            if (a is string || b is string || a == null || b == null)
                return ((a as string) ?? a?.ToString() ?? "").Trim() == ((b as string) ?? b?.ToString() ?? "").Trim();
            return Equals(a, b);
        }

        /// <summary>"1,5" / "1.5" / "" → số; chữ lạ → false.</summary>
        public static bool DocSo(string s, out double? so)
        {
            so = null;
            s = (s ?? "").Trim().Replace(',', '.');
            if (s.Length == 0) return true;
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return false;
            so = d;
            return true;
        }

        /// <summary>"8:30" / "08:30" / "" (= 00:00) → giờ trong ngày.</summary>
        public static bool DocGio(string s, out TimeSpan gio)
        {
            gio = TimeSpan.Zero;
            s = (s ?? "").Trim();
            if (s.Length == 0) return true;
            return TimeSpan.TryParseExact(s, new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out gio) && gio < TimeSpan.FromDays(1);
        }

        /// <summary>Lỗi của giá trị (null = hợp lệ): choice ngoài danh sách lựa chọn.</summary>
        public static string KiemTra(CotList c, object v)
        {
            if (c.Loai == "personOrGroup")
            {
                var ids = v as List<string> ?? new List<string>();
                if (!c.ChonNhieu && ids.Count > 1) return c.TieuDe + ": chỉ chọn được 1 người";
                return ids.Any(x => !int.TryParse(x, out _) && !x.StartsWith(ChuaCoMa, StringComparison.Ordinal)) ? c.TieuDe + ": mã người không hợp lệ" : null;
            }
            if (c.Loai != "choice" || c.LuaChon.Count == 0) return null;
            var ds = v as List<string> ?? (string.IsNullOrEmpty(v as string) ? new List<string>() : new List<string> { (string)v });
            string la = ds.FirstOrDefault(x => !c.LuaChon.Contains(x));
            return la == null ? null : c.TieuDe + ": \"" + la + "\" không có trong danh sách lựa chọn";
        }

        /// <summary>
        /// Body PATCH …/items/{id}/fields: chỉ các cột đổi. Ngày gửi UTC (cột chỉ ngày = 0 giờ máy).
        /// shortcut: cột chỉ ngày coi múi giờ site = múi giờ máy (cùng VN); khác múi thì ngày lệch 1, khi đó đọc regionalSettings của site.
        /// </summary>
        public static string Body(IEnumerable<KeyValuePair<CotList, object>> thayDoi)
        {
            var o = new JsonObject();
            foreach (var kv in thayDoi)
            {
                CotList c = kv.Key;
                if (c.Loai == "personOrGroup") // ghi theo LookupId (mã người trong site), không theo tên
                {
                    var ids = kv.Value as List<string> ?? new List<string>();
                    if (c.ChonNhieu)
                    {
                        o.Set(c.Ten + "LookupId@odata.type", "Collection(Edm.String)");
                        o.Set(c.Ten + "LookupId", ids.Cast<object>().ToList());
                    }
                    else o.Set(c.Ten + "LookupId", ids.FirstOrDefault());
                    continue;
                }
                object v = kv.Value is string s && s.Trim().Length == 0 && c.Loai != "text" ? null : kv.Value;
                switch (v)
                {
                    case null:
                        if (c.ChonNhieu) { o.Set(c.Ten + "@odata.type", "Collection(Edm.String)"); o.Set(c.Ten, new List<object>()); }
                        else o.Set(c.Ten, null);
                        break;
                    case List<string> ds:
                        o.Set(c.Ten + "@odata.type", "Collection(Edm.String)");
                        o.Set(c.Ten, ds.Cast<object>().ToList());
                        break;
                    case DateTime t:
                        o.Set(c.Ten, (c.CoGio ? t : t.Date).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
                        break;
                    case double n:
                        o.Set(c.Ten, new JsonNumber(n.ToString("R", CultureInfo.InvariantCulture)));
                        break;
                    case string chu:
                        o.Set(c.Ten, c.Loai == "text" ? chu.TrimEnd() : chu);
                        break;
                    default: // bool
                        o.Set(c.Ten, v);
                        break;
                }
            }
            return JsonLite.Serialize(o);
        }
    }
}
