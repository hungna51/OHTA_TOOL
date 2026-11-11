using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Ảnh đăng lại trong tin chuyển tiếp: TamId là số trong src "../hostedContents/&lt;TamId&gt;/$value".</summary>
    public sealed class AnhChuyen
    {
        public string TamId, LoaiNoiDung;
        public byte[] Byte;
    }

    /// <summary>Tệp đã chép sang kênh đích, đính kèm vào tin chuyển tiếp.</summary>
    public sealed class TepChuyen
    {
        public string Ten, Url;
        public string Id; // mã đính kèm = GUID trong eTag của tệp (Graph bắt buộc, sai thì 400 "Invalid GUID")
    }

    /// <summary>
    /// Chuyển tiếp chat OHTA-受注 → OHTA-日本 (logic thuần, test không cần mạng).
    /// Tin chuyển: dòng đầu "[&lt;nhóm nguồn&gt;] &lt;người&gt; · dd/MM HH:mm" + nội dung gốc (tag thành chữ thường, ảnh đăng lại, tệp chép sang).
    /// </summary>
    public static class ChuyenTiep
    {
        /// <summary>Danh sách tin của kênh (?$expand=replies): mỗi tin gốc + các trả lời, phẳng ra (GocId = id tin gốc).</summary>
        public static List<TinNhan> DocTinKenh(string json)
        {
            var kq = new List<TinNhan>();
            foreach (JsonObject goc in ((JsonLite.Parse(json) as JsonObject)?.Get("value") as List<object> ?? new List<object>()).OfType<JsonObject>())
            {
                var cacTin = new List<JsonObject> { goc };
                cacTin.AddRange((goc.Get("replies") as List<object> ?? new List<object>()).OfType<JsonObject>());
                foreach (TinNhan t in TraoDoi.DocTin("{\"value\":[" + string.Join(",", cacTin.Select(x => JsonLite.Serialize(x))) + "]}"))
                {
                    t.GocId = goc.GetString("id");
                    kq.Add(t);
                }
            }
            return kq;
        }

        /// <summary>Hoạt động mới nhất của một chuỗi trong trang (tin gốc hoặc trả lời mới nhất) — Graph xếp chuỗi theo mốc này, mới trước.</summary>
        public static DateTime? HoatDongCuoi(IEnumerable<TinNhan> chuoi) => chuoi.Max(t => t.Luc);

        /// <summary>
        /// Tin cần chuyển: tin người gửi (không phải tin hệ thống) sau mốc, thuộc chuỗi gắn với đơn (gocCuaDon: id tin gốc → đơn), cũ trước.
        /// </summary>
        public static List<TinNhan> TinCanChuyen(IEnumerable<TinNhan> tatCa, DateTime moc, ICollection<string> gocCuaDon) =>
            tatCa.Where(t => t.LaTinNguoi && t.Luc != null && t.Luc.Value > moc && gocCuaDon.Contains(t.GocId))
                 .OrderBy(t => t.Luc).ToList();

        /// <summary>Dòng đầu tin chuyển: "[OHTA-受注] 岡田 · 11/11 07:41".</summary>
        public static string DauChuyen(string nhomNguon, TinNhan t) =>
            "[" + nhomNguon + "] " + t.Nguoi + " · " + (t.Luc?.ToString("dd/MM HH:mm") ?? "");

        private static string Gon(string s) => Regex.Replace(s ?? "", @"\s+", " ").Trim();

        /// <summary>
        /// Tin này đã được chuyển sang chuỗi đích chưa: có tin đích chứa đúng dòng đầu và đoạn đầu nội dung
        /// (người gửi 2 tin trong cùng một phút vẫn phân biệt được). Dùng chống trùng khi nhiều máy cùng chuyển.
        /// </summary>
        public static bool DaChuyen(IEnumerable<TinNhan> dich, string nhomNguon, TinNhan t)
        {
            string dau = DauChuyen(nhomNguon, t), doan = Gon(t.Chu);
            if (doan.Length > 40) doan = doan.Substring(0, 40);
            return dich.Any(x => Gon(x.Chu).Contains(dau) && Gon(x.Chu).Contains(doan));
        }

        /// <summary>
        /// HTML gốc → HTML để đăng lại: tag &lt;at&gt; thành chữ "@Tên" (không báo lại), bỏ thẻ &lt;attachment&gt; (tệp đính kèm lại riêng),
        /// ảnh hostedContents đổi src sang "../hostedContents/&lt;n&gt;/$value" (n = 1, 2… theo thứ tự trong anh).
        /// </summary>
        public static string HtmlChuyen(string html, IList<string> anh)
        {
            string s = Regex.Replace(html ?? "", @"<at\b[^>]*>(.*?)</at>", "@$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            s = Regex.Replace(s, @"<attachment\b[^>]*>\s*</attachment>", "", RegexOptions.IgnoreCase);
            for (int i = 0; i < anh.Count; i++)
            {
                string goc = anh[i];
                s = Regex.Replace(s, "src=\"([^\"]+)\"", m =>
                    System.Net.WebUtility.HtmlDecode(m.Groups[1].Value) == goc ? "src=\"../hostedContents/" + (i + 1) + "/$value\"" : m.Value);
            }
            return s;
        }

        /// <summary>Loại ảnh theo vài byte đầu (PNG / JPEG / GIF), không rõ thì coi là PNG.</summary>
        public static string LoaiAnh(byte[] b)
        {
            if (b != null && b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8) return "image/jpeg";
            if (b != null && b.Length > 3 && b[0] == (byte)'G' && b[1] == (byte)'I' && b[2] == (byte)'F') return "image/gif";
            return "image/png";
        }

        /// <summary>Body POST tin chuyển tiếp: dòng đầu in đậm + nội dung (đã qua HtmlChuyen) + ảnh (hostedContents) + tệp (reference).</summary>
        public static string BodyChuyen(string dau, string htmlNoiDung, IEnumerable<AnhChuyen> anh, IEnumerable<TepChuyen> tep)
        {
            var dsTep = (tep ?? Enumerable.Empty<TepChuyen>()).Select(t => (Id: !string.IsNullOrEmpty(t.Id) ? t.Id : Guid.NewGuid().ToString(), t)).ToList();
            string html = "<p><b>" + TraoDoi.HtmlTraLoi(dau) + "</b></p>" + htmlNoiDung
                        + string.Concat(dsTep.Select(x => "<attachment id=\"" + x.Id + "\"></attachment>"));
            var b = new JsonObject();
            b.Set("contentType", "html");
            b.Set("content", html);
            var o = new JsonObject();
            o.Set("body", b);
            var dsAnh = (anh ?? Enumerable.Empty<AnhChuyen>()).ToList();
            if (dsAnh.Count > 0)
                o.Set("hostedContents", dsAnh.Select(a =>
                {
                    var h = new JsonObject();
                    h.Set("@microsoft.graph.temporaryId", a.TamId);
                    h.Set("contentBytes", Convert.ToBase64String(a.Byte));
                    h.Set("contentType", a.LoaiNoiDung);
                    return (object)h;
                }).ToList());
            if (dsTep.Count > 0)
                o.Set("attachments", dsTep.Select(x =>
                {
                    var a = new JsonObject();
                    a.Set("id", x.Id);
                    a.Set("contentType", "reference");
                    a.Set("contentUrl", x.t.Url);
                    a.Set("name", x.t.Ten);
                    return (object)a;
                }).ToList());
            return JsonLite.Serialize(o);
        }

        /// <summary>
        /// Mã đính kèm tệp cho tin Teams = GUID trong eTag của driveItem ("{153FA47A-…},1" → "153fa47a-…").
        /// Không đọc được → null (BodyChuyen tự tạo GUID mới).
        /// </summary>
        public static string GuidTuETag(string eTag)
        {
            Match m = Regex.Match(eTag ?? "", @"\{?([0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})\}?");
            return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
        }

        /// <summary>Thư mục chép tệp sang kênh đích: "Chuyển từ OHTA-受注/&lt;số đơn&gt;" (bỏ ký tự cấm trong tên thư mục SharePoint).</summary>
        public static string ThuMucTep(string nhomNguon, string title) =>
            Regex.Replace("Chuyển từ " + nhomNguon, "[\"*:<>?/\\\\|#%]", "_") + "/" + Regex.Replace(title ?? "don", "[\"*:<>?/\\\\|#%]", "_");
    }
}
