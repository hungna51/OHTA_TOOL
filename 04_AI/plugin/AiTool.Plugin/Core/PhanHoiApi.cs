using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    public sealed class CanhBao
    {
        public int Khung;
        /// <summary>true = 🔴 chắc chắn sai; false = 🟡 nghi ngờ.</summary>
        public bool Do;
        public string LoaiLoi, LyDo, GoiY;
    }

    public sealed class SuDung
    {
        public long Vao, GhiCache, DocCache, Ra;

        public void Cong(SuDung s)
        {
            if (s == null) return;
            Vao += s.Vao; GhiCache += s.GhiCache; DocCache += s.DocCache; Ra += s.Ra;
        }
    }

    public sealed class KetQuaLo
    {
        public List<CanhBao> CanhBao = new List<CanhBao>();
        public SuDung SuDung = new SuDung();
        /// <summary>null = lô chạy được; khác null = lý do lô hỏng (không có cảnh báo dùng được).</summary>
        public string Loi;
    }

    public static class PhanHoiApi
    {
        // Thông báo chỉ hiện trên tab (WPF) và nhật ký -> tiếng Việt có dấu.
        public const string LoiTuChoi = "AI từ chối lô này";
        public const string LoiBiCat = "Phản hồi bị cắt (lô quá dài)";
        public const string LoiKhongDoc = "Phản hồi không đọc được";

        /// <summary>
        /// Body lỗi HTTP không phải JSON (vd. trang HTML của proxy / cổng 502) -> bọc thành JSON lỗi
        /// có mã HTTP để Doc() báo đúng. Body JSON thì giữ nguyên.
        /// </summary>
        public static string BocLoiHttp(int ma, string body)
        {
            if (!string.IsNullOrEmpty(body) && body.TrimStart().StartsWith("{", StringComparison.Ordinal))
                return body;
            var err = new JsonObject();
            err.Set("type", "http_error");
            err.Set("message", "Máy chủ AI trả lỗi HTTP " + ma.ToString(CultureInfo.InvariantCulture));
            var o = new JsonObject();
            o.Set("type", "error");
            o.Set("error", err);
            return JsonLite.Serialize(o);
        }

        private static string DichLoi(JsonObject err)
        {
            string msg = err.GetString("message") ?? string.Empty;
            switch (err.GetString("type"))
            {
                case "authentication_error": return "API key sai hoặc đã bị xoá (kiểm tra 04_AI\\data\\ai.key)";
                case "permission_error": return "API key không có quyền dùng model này";
                case "rate_limit_error": return "AI quá tải (429), thử lại sau ít phút";
                case "overloaded_error": return "AI đang quá tải, thử lại sau ít phút";
                case "billing_error": return "Tài khoản API hết tiền / chưa nạp tiền";
                case "invalid_request_error": return "Yêu cầu không hợp lệ: " + msg;
                default: return msg.Length > 0 ? msg : LoiKhongDoc;
            }
        }

        public static KetQuaLo Doc(string json, ISet<int> khungHopLe)
        {
            var kq = new KetQuaLo();
            JsonObject goc;
            try { goc = JsonLite.Parse(json) as JsonObject; }
            catch (Exception) { goc = null; }
            if (goc == null)
            {
                kq.Loi = LoiKhongDoc;
                return kq;
            }

            if (goc.Get("error") is JsonObject err)
            {
                kq.Loi = DichLoi(err);
                return kq;
            }

            if (goc.Get("usage") is JsonObject u)
            {
                kq.SuDung.Vao = So(u, "input_tokens");
                kq.SuDung.GhiCache = So(u, "cache_creation_input_tokens");
                kq.SuDung.DocCache = So(u, "cache_read_input_tokens");
                kq.SuDung.Ra = So(u, "output_tokens");
            }

            string stop = goc.GetString("stop_reason");
            if (stop == "refusal") { kq.Loi = LoiTuChoi; return kq; }
            if (stop == "max_tokens") { kq.Loi = LoiBiCat; return kq; }

            string text = (goc.Get("content") as List<object>)?
                .OfType<JsonObject>().FirstOrDefault(b => b.GetString("type") == "text")?.GetString("text");
            JsonObject noiDung;
            try { noiDung = text == null ? null : JsonLite.Parse(text) as JsonObject; }
            catch (Exception) { noiDung = null; }
            if (!(noiDung?.Get("canh_bao") is List<object> ds))
            {
                kq.Loi = LoiKhongDoc;
                return kq;
            }

            foreach (JsonObject o in ds.OfType<JsonObject>())
            {
                int khung = (int)So(o, "khung");
                if (khungHopLe != null && !khungHopLe.Contains(khung))
                    continue; // AI nói tới khung không có trong lô -> bỏ
                kq.CanhBao.Add(new CanhBao
                {
                    Khung = khung,
                    Do = o.GetString("muc_do") == "do",
                    LoaiLoi = o.GetString("loai_loi") ?? "KHAC",
                    LyDo = o.GetString("ly_do") ?? string.Empty,
                    GoiY = o.GetString("goi_y") ?? string.Empty,
                });
            }
            return kq;
        }

        private static long So(JsonObject o, string khoa)
        {
            object v = o.Get(khoa);
            string raw = v is JsonNumber n ? n.Raw : v?.ToString();
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? (long)d : 0;
        }
    }
}
