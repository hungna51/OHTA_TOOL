using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>
    /// Nội dung %LOCALAPPDATA%\OHTA_TOOL\teams\dang_lam.json: ứng dụng OhtaTeams ghi, tab Teams trên CAD đọc
    /// (_docs/specs/2026-11-10-ung-dung-teams-rieng.md). Đơn đã tính sẵn tên người / ngày → CAD không cần biết cột SharePoint.
    /// </summary>
    public sealed class DangLam
    {
        public DateTime Luc;
        public string ThuMucGoc = "", DonChon = "";
        public List<DonTeams> Don = new List<DonTeams>();
    }

    public static class PhanTichDangLam
    {
        /// <summary>%LOCALAPPDATA%\OHTA_TOOL\teams\dang_lam.json (ngoài OneDrive / repo).</summary>
        public static string FileMacDinh => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "teams", "dang_lam.json");

        private const string KieuNgay = "yyyy-MM-dd";
        private const string KieuLuc = "yyyy-MM-ddTHH:mm:ss";

        /// <summary>Chỉ ghi đơn chưa xong + đơn đang chọn (dù đã xong).</summary>
        public static List<DonTeams> ChonGhi(IEnumerable<DonTeams> tatCa, string donChon) =>
            tatCa.Where(d => !d.DaXong || (!string.IsNullOrEmpty(donChon) && d.Id == donChon)).ToList();

        public static string Ghi(DangLam dl)
        {
            var ds = new List<object>();
            foreach (DonTeams d in dl.Don)
            {
                var o = new JsonObject();
                o.Set("id", d.Id ?? "");
                o.Set("title", d.Title ?? "");
                o.Set("han", d.Han?.ToString(KieuNgay, CultureInfo.InvariantCulture) ?? "");
                o.Set("trang_thai", d.TrangThai ?? "");
                o.Set("status_jp", d.StatusJp ?? "");
                o.Set("trien_khai", d.TrienKhai ?? "");
                o.Set("check", d.Check ?? "");
                o.Set("da_xong", d.DaXong);
                o.Set("url", d.WebUrl ?? "");
                o.Set("cot", d.TatCa.Select(kv => (object)new List<object> { kv.Key, kv.Value }).ToList());
                ds.Add(o);
            }
            var goc = new JsonObject();
            goc.Set("luc", dl.Luc.ToString(KieuLuc, CultureInfo.InvariantCulture));
            goc.Set("thu_muc_goc", dl.ThuMucGoc ?? "");
            goc.Set("don_chon", dl.DonChon ?? "");
            goc.Set("don", ds);
            return JsonLite.Serialize(goc);
        }

        public const int SauToiDa = 3;  // thư mục đơn nằm tối đa 3 cấp dưới thư mục gốc
        public const int TitleToiThieu = 3; // Title quá ngắn ("1", "A") khớp bừa mọi thư mục

        /// <summary>Tên thư mục có chứa Title đơn (bỏ dấu, không phân biệt hoa thường).</summary>
        public static bool TenKhop(string tenThuMuc, string title)
        {
            string t = PhanTichTeams.BoDau(title ?? "");
            return t.Length >= TitleToiThieu && PhanTichTeams.BoDau(tenThuMuc ?? "").Contains(t);
        }

        /// <summary>
        /// Đơn mà bản vẽ thuộc về: một trong ≤ 3 thư mục đầu tiên dưới thư mục gốc (trên đường tới file) có tên chứa Title đơn.
        /// Nhiều đơn khớp → Title dài nhất. Bản vẽ ngoài thư mục gốc / chưa lưu → null.
        /// </summary>
        public static DonTeams KhopBanVe(string banVe, DangLam dl)
        {
            if (dl == null || string.IsNullOrWhiteSpace(banVe) || string.IsNullOrWhiteSpace(dl.ThuMucGoc)) return null;
            string goc = dl.ThuMucGoc.Replace('/', '\\').TrimEnd('\\') + "\\";
            string f = banVe.Replace('/', '\\');
            if (!f.StartsWith(goc, StringComparison.OrdinalIgnoreCase)) return null;
            string[] phan = f.Substring(goc.Length).Split('\\');
            var thuMuc = phan.Take(phan.Length - 1).Take(SauToiDa).ToList(); // bỏ tên file
            return dl.Don.Where(d => thuMuc.Any(t => TenKhop(t, d.Title)))
                         .OrderByDescending(d => PhanTichTeams.BoDau(d.Title).Length)
                         .FirstOrDefault();
        }

        /// <summary>Đơn tab CAD hiện: khớp bản vẽ trước, không có thì đơn 📌; TheoBanVe cho biết lấy theo cách nào.</summary>
        public static (DonTeams Don, bool TheoBanVe) DonDangLam(string banVe, DangLam dl)
        {
            DonTeams khop = KhopBanVe(banVe, dl);
            if (khop != null) return (khop, true);
            if (dl == null || string.IsNullOrEmpty(dl.DonChon)) return (null, false);
            return (dl.Don.FirstOrDefault(d => d.Id == dl.DonChon), false);
        }

        /// <summary>File hỏng / không phải dạng dang_lam → null (CAD hiện "chưa có đơn đang làm").</summary>
        public static DangLam Doc(string json)
        {
            JsonObject goc;
            try { goc = JsonLite.Parse(json) as JsonObject; }
            catch (Exception) { return null; }
            if (goc == null || !(goc.Get("don") is List<object> ds)) return null;

            var dl = new DangLam
            {
                ThuMucGoc = goc.GetString("thu_muc_goc") ?? "",
                DonChon = goc.GetString("don_chon") ?? "",
            };
            DateTime.TryParseExact(goc.GetString("luc") ?? "", KieuLuc, CultureInfo.InvariantCulture, DateTimeStyles.None, out dl.Luc);
            foreach (JsonObject o in ds.OfType<JsonObject>())
            {
                var d = new DonTeams
                {
                    Id = o.GetString("id") ?? "",
                    Title = o.GetString("title") ?? "",
                    TrangThai = o.GetString("trang_thai") ?? "",
                    StatusJp = o.GetString("status_jp") ?? "",
                    TrienKhai = o.GetString("trien_khai") ?? "",
                    Check = o.GetString("check") ?? "",
                    DaXong = o.Get("da_xong") is bool b && b,
                    WebUrl = o.GetString("url") ?? "",
                };
                if (DateTime.TryParseExact(o.GetString("han") ?? "", KieuNgay, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime han)) d.Han = han;
                if (o.Get("cot") is List<object> cot)
                    foreach (List<object> c in cot.OfType<List<object>>())
                        if (c.Count == 2) d.TatCa.Add(new KeyValuePair<string, string>(c[0] as string ?? "", c[1] as string ?? ""));
                dl.Don.Add(d);
            }
            return dl;
        }
    }
}
