using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// 1 yêu cầu cập nhật dữ liệu nằm chờ duyệt trong tab "update" của Google Sheet.
    /// Cột: Loai_du_lieu | Thong_so | May_gui | Thoi_gian | ID — Thong_so là các cặp "key=value" cách nhau " | ".
    /// Thêm loại dữ liệu mới: thêm hằng Loai, hàm Build...Params và nhánh trong Describe (cùng HANDLERS trong Apps Script).
    /// </summary>
    [DataContract]
    public sealed class FnameUpdateRequest
    {
        public const string LoaiCatalog = "catalog";
        public const string LoaiTrialBend = "trav_thu";

        [DataMember(Name = "id")] public string Id { get; set; } = string.Empty;
        [DataMember(Name = "loai")] public string Loai { get; set; } = string.Empty;
        [DataMember(Name = "thongSo")] public string ThongSo { get; set; } = string.Empty;
        [DataMember(Name = "mayGui")] public string MayGui { get; set; } = string.Empty;
        [DataMember(Name = "thoiGian")] public string ThoiGian { get; set; } = string.Empty;

        public Dictionary<string, string> Params => ParseParams(ThongSo);

        public string Get(string key) =>
            Params.TryGetValue(key, out string v) ? v : string.Empty;

        public bool IsLoai(string loai) => string.Equals(Loai, loai, StringComparison.OrdinalIgnoreCase);

        /// <summary>Chuỗi hiển thị trong tab Chờ phê duyệt.</summary>
        public string Describe()
        {
            if (IsLoai(LoaiCatalog))
            {
                string z = Get("Z");
                return $"Vật liệu {Get("M")} ({Get("Loai")}) — T: {Get("T")}" + (z.Length > 0 ? $" — Z={z}" : " — Z: giữ nguyên / trống");
            }
            if (IsLoai(LoaiTrialBend))
            {
                string note = Get("Ghi_chu");
                return $"{Get("Dinh_danh")} {Get("Loai")} T={Get("T")} — {Get("Goc")}° — {Get("Dao")} — Denta {Get("Denta")} — tấm {Get("Ma_tam")}"
                    + (note.Length > 0 ? " — " + note : string.Empty);
            }
            return ThongSo;
        }

        public string Description => Describe();

        public string LoaiLabel =>
            IsLoai(LoaiCatalog) ? "Danh mục vật liệu (catalog)"
            : IsLoai(LoaiTrialBend) ? "Góc bẻ thử / nội suy (trav_thu)"
            : Loai;

        // ---------- Thong_so ----------

        public static Dictionary<string, string> ParseParams(string text)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in (text ?? string.Empty).Split('|'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0)
                    d[part.Substring(0, eq).Trim()] = part.Substring(eq + 1).Trim();
            }
            return d;
        }

        public static string FormatParams(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            return string.Join(" | ", pairs.Select(kv => kv.Key.Trim() + "=" + Clean(kv.Value)));
        }

        public static string BuildCatalogParams(string m, string loai, string z, IEnumerable<string> thicknesses)
        {
            return FormatParams(new[]
            {
                new KeyValuePair<string, string>("M", m),
                new KeyValuePair<string, string>("Loai", loai),
                new KeyValuePair<string, string>("Z", z),
                new KeyValuePair<string, string>("T", string.Join(" ", thicknesses ?? Enumerable.Empty<string>()))
            });
        }

        /// <summary>Thong_so 1 dòng góc bẻ thử / nội suy (khoá trùng tên cột tab trav_thu, Ghi_chu chỉ để admin đọc).</summary>
        public static string BuildTrialParams(TraVThuRecord r, string ghiChu = null)
        {
            return FormatParams(new[]
            {
                new KeyValuePair<string, string>("Loai", r.Loai),
                new KeyValuePair<string, string>("T", r.T),
                new KeyValuePair<string, string>("Goc", TraVCalculator.FormatAngle(r.Goc)),
                new KeyValuePair<string, string>("Dao", r.Dao),
                new KeyValuePair<string, string>("Denta", r.Denta),
                new KeyValuePair<string, string>("Ma_tam", r.MaTam),
                new KeyValuePair<string, string>("Dinh_danh", r.DinhDanh),
                new KeyValuePair<string, string>("Ghi_chu", ghiChu)
            });
        }

        // Dấu | và xuống dòng là ký tự phân cách -> thay bằng dấu cách
        private static string Clean(string v) =>
            (v ?? string.Empty).Replace('|', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
    }

    /// <summary>Kết quả JSON của Apps Script (ok/message, kèm isAdmin + requests khi list_requests).</summary>
    [DataContract]
    public sealed class FnameSheetReply
    {
        [DataMember(Name = "ok")] public bool Ok { get; set; }
        [DataMember(Name = "message")] public string Message { get; set; }
        [DataMember(Name = "isAdmin")] public bool IsAdmin { get; set; }
        [DataMember(Name = "requests")] public List<FnameUpdateRequest> Requests { get; set; }

        /// <summary>Đọc JSON trả về; không phải JSON hợp lệ (trang HTML lỗi, rỗng...) thì trả null.</summary>
        public static FnameSheetReply Parse(string body)
        {
            if (string.IsNullOrWhiteSpace(body) || !body.TrimStart().StartsWith("{", StringComparison.Ordinal))
                return null;
            try
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(body)))
                {
                    var r = (FnameSheetReply)new DataContractJsonSerializer(typeof(FnameSheetReply)).ReadObject(ms);
                    if (r != null && r.Requests == null)
                        r.Requests = new List<FnameUpdateRequest>();
                    return r;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
