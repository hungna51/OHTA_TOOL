using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    /// <summary>
    /// Cho Ling chạy phần ▶ Kiểm tra mà không đổi KiemTraRunner / PhanHoiApi: đổi body Claude
    /// (/v1/messages) sang body Ling (chat/completions, kiểu OpenAI) và đổi phản hồi Ling về dạng Claude.
    /// Ling không có json_schema bắt buộc → schema được đưa vào lời dặn hệ thống.
    /// </summary>
    public static class LingDich
    {
        /// <summary>Mục chọn trong ô Model của tab; tên model thật lấy từ ⚙ (registry ling_model).</summary>
        public const string MucModelLing = "Ling (model trong ⚙)";

        public static string ChuyenBody(string bodyClaude, string modelLing)
        {
            var goc = (JsonObject)JsonLite.Parse(bodyClaude);
            var sys = new StringBuilder();
            foreach (JsonObject k in (goc.Get("system") as List<object> ?? new List<object>()).OfType<JsonObject>())
                sys.Append(k.GetString("text")).Append("\n\n");
            object schema = ((goc.Get("output_config") as JsonObject)?.Get("format") as JsonObject)?.Get("schema");
            if (schema != null)
                sys.Append("# DINH DANG TRA LOI (BAT BUOC)\nChi tra ve DUY NHAT mot doi tuong JSON dung schema duoi, ")
                   .Append("khong giai thich, khong boc ```. Khong co loi thi tra {\"canh_bao\":[]}.\n")
                   .Append(JsonLite.Serialize(schema));

            var tin = new List<object>();
            var s = new JsonObject();
            s.Set("role", "system");
            s.Set("content", sys.ToString().Trim());
            tin.Add(s);
            foreach (JsonObject m in (goc.Get("messages") as List<object> ?? new List<object>()).OfType<JsonObject>())
            {
                var o = new JsonObject();
                o.Set("role", m.GetString("role"));
                o.Set("content", m.Get("content") as string ?? "");
                tin.Add(o);
            }
            var body = new JsonObject();
            body.Set("model", modelLing);
            body.Set("stream", false);
            body.Set("messages", tin);
            return JsonLite.Serialize(body);
        }

        /// <summary>Phản hồi chat/completions → dạng Claude {content:[{type:text,text}], usage, stop_reason}.</summary>
        public static string ChuyenPhanHoi(string json)
        {
            JsonObject goc;
            try { goc = JsonLite.Parse(json) as JsonObject; }
            catch (FormatException) { return Loi("Phản hồi Ling không đọc được"); }
            if (goc == null) return Loi("Phản hồi Ling không đọc được");
            if (goc.Get("error") is JsonObject e)
                return Loi("Ling: " + (e.GetString("message") ?? "lỗi không rõ"));

            var choice = (goc.Get("choices") as List<object>)?.OfType<JsonObject>().FirstOrDefault();
            string text = (choice?.Get("message") as JsonObject)?.GetString("content") ?? "";

            var kq = new JsonObject();
            kq.Set("content", new List<object> { KhoiText(CatJson(text)) });
            if (choice?.GetString("finish_reason") == "length") kq.Set("stop_reason", "max_tokens");
            if (goc.Get("usage") is JsonObject u)
            {
                var us = new JsonObject();
                us.Set("input_tokens", u.Get("prompt_tokens"));
                us.Set("output_tokens", u.Get("completion_tokens"));
                kq.Set("usage", us);
            }
            return JsonLite.Serialize(kq);
        }

        /// <summary>Lỗi HTTP của Ling (body thường là chữ trơn, vd. "The Model not exists") → JSON lỗi dạng Claude.</summary>
        public static string LoiHttp(int ma, string body)
        {
            string t = (body ?? "").Trim();
            if (t.Length > 200) t = t.Substring(0, 200) + "…";
            return Loi("Ling trả lỗi HTTP " + ma.ToString(CultureInfo.InvariantCulture) + (t.Length > 0 ? ": " + t : ""));
        }

        /// <summary>Bỏ ```json … ``` và chữ thừa: lấy từ '{' đầu tới '}' cuối.</summary>
        public static string CatJson(string text)
        {
            int a = text.IndexOf('{'), b = text.LastIndexOf('}');
            return a >= 0 && b > a ? text.Substring(a, b - a + 1) : text;
        }

        private static JsonObject KhoiText(string text)
        {
            var o = new JsonObject();
            o.Set("type", "text");
            o.Set("text", text);
            return o;
        }

        private static string Loi(string msg)
        {
            var err = new JsonObject();
            err.Set("type", "ling_error");
            err.Set("message", msg);
            var o = new JsonObject();
            o.Set("type", "error");
            o.Set("error", err);
            return JsonLite.Serialize(o);
        }
    }
}
