using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    /// <summary>Một tin trong cuộc chat. DinhKem = mô tả khung (.md gọn) gửi kèm câu hỏi.</summary>
    public sealed class TinChat
    {
        public string Vai;       // "user" | "assistant"
        public string NoiDung;
        public string SuyNghi;
        public string DinhKem;
        public string Luc;
    }

    /// <summary>
    /// Logic chat với Ling (không phụ thuộc CAD / WPF): nơi lưu trong thư mục OHTA_AI cạnh bản vẽ,
    /// đọc/ghi lịch sử .json, ghép body chat/completions và đọc từng dòng SSE khi streaming.
    /// </summary>
    public static class ChatLing
    {
        public const string TenThuMucAi = "OHTA_AI";
        public const int SoTinGui = 20;

        /// <summary>&lt;thư mục bản vẽ&gt;\OHTA_AI; bản vẽ chưa lưu (không phải .dwg/.dxf có đường dẫn) → null.</summary>
        public static string ThuMucAi(string fileBanVe)
        {
            if (string.IsNullOrWhiteSpace(fileBanVe) || !Path.IsPathRooted(fileBanVe)) return null;
            string duoi = Path.GetExtension(fileBanVe).ToLowerInvariant();
            if (duoi != ".dwg" && duoi != ".dxf") return null;
            return Path.Combine(Path.GetDirectoryName(fileBanVe), TenThuMucAi);
        }

        public static string ThuMucMoTa(string fileBanVe)
        {
            string ai = ThuMucAi(fileBanVe);
            return ai == null ? null : Path.Combine(ai, "mo-ta");
        }

        /// <summary>OHTA_AI\chat\&lt;tên bản vẽ&gt;; bản vẽ chưa lưu → %LOCALAPPDATA%\OHTA_TOOL\AI\chat\_chua-luu.</summary>
        public static string ThuMucChat(string fileBanVe, string localAppData)
        {
            string ai = ThuMucAi(fileBanVe);
            return ai != null
                ? Path.Combine(ai, "chat", Path.GetFileNameWithoutExtension(fileBanVe))
                : Path.Combine(localAppData, "OHTA_TOOL", "AI", "chat", "_chua-luu");
        }

        public static string TenFileMoi(DateTime luc) => luc.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture) + ".json";

        /// <summary>Lời dặn hệ thống + bộ luật nghiệp vụ (data\kev\*.md) làm kiến thức nền.</summary>
        public static string LoiDan(string boLuat)
        {
            var sb = new StringBuilder();
            sb.Append("Ban la tro ly AI cua xuong gia cong kim loai tam OHTA (cat laser, be, vat, cuon, taro). ")
              .Append("Tra loi bang tieng Viet, ngan gon, ro rang. Ban chi doc va giai thich, khong sua ban ve. ")
              .Append("Khi nguoi dung dinh kem mo ta khung in, moi khung la mot tam; thong tin tam lay tu F-name. ")
              .Append("Phan JSON dinh kem co toa do (mm) cua tung doi tuong kem layer: hay dung no de tu tinh kich thuoc ")
              .Append("(vd. kich thuoc tong X/Y = hop bao cac doi tuong tren layer 0-KAKOU) khi ban ve khong co dim.");
            if (!string.IsNullOrWhiteSpace(boLuat))
                sb.Append("\n\n# Kien thuc nghiep vu\n").Append(boLuat.Trim());
            return sb.ToString();
        }

        /// <summary>Gộp mọi file .md trong thư mục luật (theo tên), mỗi file có dòng tiêu đề tên file.</summary>
        public static string DocBoLuat(string thuMucKev)
        {
            if (string.IsNullOrEmpty(thuMucKev) || !Directory.Exists(thuMucKev)) return "";
            var sb = new StringBuilder();
            foreach (string f in Directory.GetFiles(thuMucKev, "*.md").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                sb.Append("\n<!-- ").Append(Path.GetFileName(f)).Append(" -->\n")
                  .Append(File.ReadAllText(f, Encoding.UTF8).Trim()).Append('\n');
            return sb.ToString();
        }

        /// <summary>Đính kèm của nút 📎 Gửi khung: bản .md gọn + bản .json đủ toạ độ (để AI tự đo kích thước).</summary>
        public static string GhepDinhKem(string md, string json) =>
            (md ?? "").TrimEnd() + "\n\n## JSON day du (toa do tung doi tuong)\n```json\n" + (json ?? "").Trim() + "\n```\n";

        /// <summary>Nội dung thật gửi cho model của một tin người dùng (đính kèm đứng trước câu hỏi).</summary>
        public static string NoiDungGui(TinChat t) =>
            string.IsNullOrWhiteSpace(t.DinhKem) ? t.NoiDung ?? "" : "# Mo ta khung dinh kem\n" + t.DinhKem.Trim() + "\n\n# Cau hoi\n" + t.NoiDung;

        /// <summary>Body POST chat/completions: system + tối đa SoTinGui tin gần nhất (tin rỗng bị bỏ).</summary>
        public static string TaoBody(string model, string loiDan, IList<TinChat> lichSu, bool stream = true)
        {
            var tin = new List<object>();
            var sys = new JsonObject();
            sys.Set("role", "system");
            sys.Set("content", loiDan ?? "");
            tin.Add(sys);
            List<TinChat> coChu = lichSu.Where(x => !string.IsNullOrEmpty(x.NoiDung)).ToList();
            foreach (TinChat t in coChu.Skip(Math.Max(0, coChu.Count - SoTinGui)))
            {
                var o = new JsonObject();
                o.Set("role", t.Vai);
                o.Set("content", t.Vai == "user" ? NoiDungGui(t) : t.NoiDung);
                tin.Add(o);
            }
            var goc = new JsonObject();
            goc.Set("model", model);
            goc.Set("stream", stream);
            goc.Set("messages", tin);
            return JsonLite.Serialize(goc);
        }

        /// <summary>
        /// Một dòng SSE: "data: {...}" → phần chữ / phần suy nghĩ mới; "data: [DONE]" → Xong.
        /// Dòng khác (trống, ": ping", JSON hỏng) → cả hai null.
        /// </summary>
        public static (string Chu, string SuyNghi, bool Xong) DocDongSse(string dong)
        {
            if (dong == null || !dong.StartsWith("data:", StringComparison.Ordinal)) return (null, null, false);
            string data = dong.Substring(5).Trim();
            if (data == "[DONE]") return (null, null, true);
            try
            {
                var o = JsonLite.Parse(data) as JsonObject;
                var ds = o?.Get("choices") as List<object>;
                var delta = (ds != null && ds.Count > 0 ? ds[0] as JsonObject : null)?.Get("delta") as JsonObject;
                return (delta?.GetString("content"), delta?.GetString("reasoning_content"), false);
            }
            catch (FormatException) { return (null, null, false); }
        }

        public static string GhiLichSu(string model, IList<TinChat> lichSu)
        {
            var goc = new JsonObject();
            goc.Set("model", model ?? "");
            goc.Set("tin", lichSu.Select(t =>
            {
                var o = new JsonObject();
                o.Set("vai", t.Vai);
                o.Set("noi_dung", t.NoiDung ?? "");
                if (!string.IsNullOrEmpty(t.SuyNghi)) o.Set("suy_nghi", t.SuyNghi);
                if (!string.IsNullOrEmpty(t.DinhKem)) o.Set("dinh_kem", t.DinhKem);
                o.Set("luc", t.Luc ?? "");
                return (object)o;
            }).ToList());
            return JsonLite.Serialize(goc);
        }

        public static List<TinChat> DocLichSu(string json)
        {
            var ds = (JsonLite.Parse(json) as JsonObject)?.Get("tin") as List<object> ?? new List<object>();
            return ds.OfType<JsonObject>().Select(o => new TinChat
            {
                Vai = o.GetString("vai") == "assistant" ? "assistant" : "user",
                NoiDung = o.GetString("noi_dung") ?? "",
                SuyNghi = o.GetString("suy_nghi"),
                DinhKem = o.GetString("dinh_kem"),
                Luc = o.GetString("luc"),
            }).ToList();
        }

        /// <summary>Nhãn của một cuộc trong ô chọn: "2026-11-07 15:30 · &lt;câu hỏi đầu&gt;".</summary>
        public static string NhanCuoc(string tenFile, IList<TinChat> lichSu)
        {
            string ten = Path.GetFileNameWithoutExtension(tenFile) ?? "";
            string luc = DateTime.TryParseExact(ten, "yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)
                ? d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : ten;
            string dau = lichSu?.FirstOrDefault(t => t.Vai == "user")?.NoiDung?.Replace('\n', ' ').Trim() ?? "";
            if (dau.Length > 40) dau = dau.Substring(0, 40) + "…";
            return dau.Length == 0 ? luc : luc + " · " + dau;
        }
    }
}
