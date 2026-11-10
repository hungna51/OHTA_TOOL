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
        /// <summary>Giá trị thô của Graph → giá trị form.</summary>
        public static object GiaTriForm(CotList c, object tho)
        {
            switch (c.Loai)
            {
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
