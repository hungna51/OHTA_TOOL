using System;
using System.Collections.Generic;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Action nào của Apps Script bị khoá khi máy chạy bản thấp hơn phien_ban_toi_thieu.</summary>
    public static class SheetActions
    {
        // Đọc dữ liệu và báo lỗi luôn được: máy bản cũ càng cần báo lỗi được.
        private static readonly HashSet<string> NoGate = new HashSet<string>(StringComparer.Ordinal)
        {
            "list_requests", "my_requests", "submit_bug", "list_bugs", "resolve_bug",
        };

        public static bool RequiresVersionGate(string action) => !NoGate.Contains(action ?? "");
    }

    /// <summary>1 dòng tab bug_report trên Sheet, cho cửa sổ admin.</summary>
    public sealed class BugAdminItem
    {
        public string Id { get; set; }
        public string ThoiGian { get; set; }
        public string MayGui { get; set; }
        public string NguoiGui { get; set; }
        public string MucDo { get; set; }
        public string TabLenh { get; set; }
        public string MoTa { get; set; }
        public string PhienBan { get; set; }
        public string Loi { get; set; }
        public string Anh { get; set; }
        public string ChiTiet { get; set; }
        public string DinhKem { get; set; }

        /// <summary>Đọc mảng "bugs" trong JSON trả về của list_bugs; JSON hỏng thì trả danh sách rỗng.</summary>
        public static List<BugAdminItem> ParseList(string json)
        {
            var list = new List<BugAdminItem>();
            try
            {
                if (string.IsNullOrWhiteSpace(json) || !(JsonLite.Parse(json) is JsonObject root) || !(root.Get("bugs") is List<object> bugs))
                    return list;
                foreach (object o in bugs)
                {
                    if (!(o is JsonObject b))
                        continue;
                    list.Add(new BugAdminItem
                    {
                        Id = S(b, "ID"),
                        ThoiGian = S(b, "Thoi_gian"),
                        MayGui = S(b, "May_gui"),
                        NguoiGui = S(b, "Nguoi_gui"),
                        MucDo = S(b, "Muc_do"),
                        TabLenh = S(b, "Tab_lenh"),
                        MoTa = S(b, "Mo_ta"),
                        PhienBan = S(b, "Phien_ban"),
                        Loi = S(b, "Loi"),
                        Anh = S(b, "Anh"),
                        ChiTiet = S(b, "Chi_tiet"),
                        DinhKem = S(b, "Dinh_kem"),
                    });
                }
            }
            catch
            {
                list.Clear();
            }
            return list;
        }

        private static string S(JsonObject o, string key) => o.Get(key)?.ToString() ?? "";
    }
}
