using System;
using System.Collections.Generic;
using System.Globalization;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Một báo lỗi người dùng gửi cho admin (spec _docs/specs/2026-11-05-bug-report.md).
    /// Id do máy sinh để gửi lại từ hàng chờ không tạo dòng trùng trên Sheet.
    /// </summary>
    public sealed class BugReport
    {
        public static readonly string[] MucDoList = { "Chặn việc", "Phiền", "Nhỏ" };
        private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

        public string Id { get; set; }
        public DateTime ThoiGian { get; set; }
        public string NguoiGui { get; set; }
        public string MucDo { get; set; }
        public string TabLenh { get; set; }
        public string MoTa { get; set; }
        public string PhienBan { get; set; }
        /// <summary>Tóm tắt lỗi (1 dòng, ≤ 1000 ký tự) — ghi vào ô Sheet.</summary>
        public string Loi { get; set; }
        /// <summary>Thông tin phiên bản + stack trace + nhật ký — lưu thành file .txt trên Drive (ô Sheet giới hạn 50.000 ký tự).</summary>
        public string ChiTiet { get; set; }
        public byte[] AnhJpeg { get; set; }
        public byte[] DinhKemZip { get; set; }
        public string DinhKemTen { get; set; }

        public static BugReport Create() => new BugReport
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 12),
            ThoiGian = DateTime.Now,
            NguoiGui = Environment.MachineName + " / " + Environment.UserName,
            MucDo = "Phiền",
        };

        /// <summary>null nếu hợp lệ; ngược lại câu lỗi hiện cho người dùng.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(MoTa))
                return "Hãy mô tả lỗi: đang làm gì, mong đợi gì, ra kết quả gì.";
            if (Array.IndexOf(MucDoList, MucDo) < 0)
                return "Hãy chọn mức độ: " + string.Join(" / ", MucDoList) + ".";
            return null;
        }

        /// <summary>Dòng đầu tiên, cắt còn tối đa 1000 ký tự.</summary>
        public static string ShortError(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            int nl = s.IndexOfAny(new[] { '\r', '\n' });
            string first = nl >= 0 ? s.Substring(0, nl) : s;
            return first.Length > 1000 ? first.Substring(0, 1000) : first;
        }

        /// <summary>Các trường gửi cho Apps Script (action submit_bug).</summary>
        public Dictionary<string, string> ToFields()
        {
            var f = new Dictionary<string, string>
            {
                ["id"] = Id ?? string.Empty,
                ["thoi_gian"] = ThoiGian.ToString(TimeFormat, CultureInfo.InvariantCulture),
                ["nguoi_gui"] = NguoiGui ?? string.Empty,
                ["muc_do"] = MucDo ?? string.Empty,
                ["tab_lenh"] = TabLenh ?? string.Empty,
                ["mo_ta"] = MoTa ?? string.Empty,
                ["phien_ban"] = PhienBan ?? string.Empty,
                ["loi"] = ShortError(Loi),
                ["chi_tiet"] = ChiTiet ?? string.Empty,
            };
            if (AnhJpeg != null && AnhJpeg.Length > 0)
                f["anh"] = Convert.ToBase64String(AnhJpeg);
            if (DinhKemZip != null && DinhKemZip.Length > 0)
            {
                f["dinh_kem"] = Convert.ToBase64String(DinhKemZip);
                f["dinh_kem_ten"] = string.IsNullOrEmpty(DinhKemTen) ? "dinh_kem.zip" : DinhKemTen;
            }
            return f;
        }

        public string ToJson()
        {
            var o = new JsonObject();
            o.Set("Id", Id);
            o.Set("ThoiGian", ThoiGian.ToString(TimeFormat, CultureInfo.InvariantCulture));
            o.Set("NguoiGui", NguoiGui);
            o.Set("MucDo", MucDo);
            o.Set("TabLenh", TabLenh);
            o.Set("MoTa", MoTa);
            o.Set("PhienBan", PhienBan);
            o.Set("Loi", Loi);
            o.Set("ChiTiet", ChiTiet);
            o.Set("AnhJpeg", AnhJpeg == null ? null : Convert.ToBase64String(AnhJpeg));
            o.Set("DinhKemZip", DinhKemZip == null ? null : Convert.ToBase64String(DinhKemZip));
            o.Set("DinhKemTen", DinhKemTen);
            return JsonLite.Serialize(o);
        }

        /// <summary>Đọc lại từ hàng chờ; JSON hỏng hoặc thiếu Id thì trả null.</summary>
        public static BugReport FromJson(string json)
        {
            try
            {
                if (!(JsonLite.Parse(json) is JsonObject o) || string.IsNullOrEmpty(o.GetString("Id")))
                    return null;
                DateTime.TryParseExact(o.GetString("ThoiGian"), TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t);
                string anh = o.GetString("AnhJpeg");
                string zip = o.GetString("DinhKemZip");
                return new BugReport
                {
                    Id = o.GetString("Id"),
                    ThoiGian = t,
                    NguoiGui = o.GetString("NguoiGui"),
                    MucDo = o.GetString("MucDo"),
                    TabLenh = o.GetString("TabLenh"),
                    MoTa = o.GetString("MoTa"),
                    PhienBan = o.GetString("PhienBan"),
                    Loi = o.GetString("Loi"),
                    ChiTiet = o.GetString("ChiTiet"),
                    AnhJpeg = string.IsNullOrEmpty(anh) ? null : Convert.FromBase64String(anh),
                    DinhKemZip = string.IsNullOrEmpty(zip) ? null : Convert.FromBase64String(zip),
                    DinhKemTen = o.GetString("DinhKemTen"),
                };
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Chống hỏi "Gửi báo lỗi?" nhiều lần: cùng nguồn + kiểu + dòng đầu thông báo chỉ hỏi 1 lần mỗi phiên CAD,
    /// và không hỏi lồng khi đang ở trong luồng báo lỗi (InReport).
    /// </summary>
    public static class BugDedup
    {
        private static readonly HashSet<string> Asked = new HashSet<string>(StringComparer.Ordinal);
        private static readonly object Gate = new object();

        public static bool InReport { get; set; }

        public static bool ShouldAsk(string nguon, Exception ex) =>
            ShouldAskKey(nguon + "|" + (ex == null ? "" : ex.GetType().FullName) + "|" + BugReport.ShortError(ex?.Message));

        public static bool ShouldAsk(string nguon, string loi) =>
            ShouldAskKey(nguon + "|lisp|" + BugReport.ShortError(loi));

        private static bool ShouldAskKey(string key)
        {
            if (InReport)
                return false;
            lock (Gate)
                return Asked.Add(key);
        }

        public static void ResetForTest()
        {
            lock (Gate)
                Asked.Clear();
            InReport = false;
        }
    }
}
