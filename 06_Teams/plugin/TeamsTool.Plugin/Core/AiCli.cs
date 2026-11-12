using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>
    /// Nguồn AI thay cho key API: Claude Code CLI (claude -p) hoặc Antigravity CLI (agy -p), dùng gói của người đăng nhập trên máy.
    /// Đổi body /v1/messages (lời dặn, quy chuẩn, PDF, mẫu JSON) thành lệnh CLI và đổi kết quả CLI về dạng phản hồi API,
    /// nên phòng 2–4 đọc phản hồi y như khi dùng key.
    /// </summary>
    public static class AiCli
    {
        public const string Api = "api", ClaudeCode = "claude_code", Antigravity = "antigravity", Ling = "ling";

        // ---------- Ling (tbox, kiểu OpenAI chat/completions — như tab AI trên CAD) ----------

        public const string UrlLing = "https://api.tbox.cn/api/llm/v1/chat/completions";
        public const string ModelLingMacDinh = "Ling-3.2-flash";

        /// <summary>Body Claude có PDF / ảnh không (Ling chỉ đọc chữ → việc này phải dùng Claude).</summary>
        public static bool CoTep(string bodyClaude)
        {
            var o = JsonLite.Parse(bodyClaude) as JsonObject;
            return (o?.Get("messages") as List<object> ?? new List<object>()).OfType<JsonObject>()
                .SelectMany(m => (m.Get("content") as List<object> ?? new List<object>()).OfType<JsonObject>())
                .Any(k => k.GetString("type") == "document" || k.GetString("type") == "image");
        }

        /// <summary>
        /// Body Claude (/v1/messages) → body Ling: system = lời dặn + quy chuẩn + mẫu JSON bắt buộc (Ling không có json_schema),
        /// user = các khối chữ. PDF / ảnh bỏ (gọi CoTep trước để không gửi việc cần bản vẽ cho Ling).
        /// </summary>
        public static string BodyLing(string bodyClaude, string modelLing)
        {
            YeuCau yc = TachBody(bodyClaude);
            var goc = (JsonObject)JsonLite.Parse(bodyClaude);
            var sys = new StringBuilder();
            foreach (JsonObject k in (goc.Get("system") as List<object> ?? new List<object>()).OfType<JsonObject>()) sys.Append(k.GetString("text")).Append("\n\n");
            if (yc.Schema.Length > 0)
                sys.Append("# ĐỊNH DẠNG TRẢ LỜI (BẮT BUỘC)\nChỉ trả về DUY NHẤT một đối tượng JSON đúng JSON Schema dưới (đủ mọi trường required), không giải thích, không bọc ```.\n").Append(yc.Schema);
            var user = new StringBuilder();
            foreach (JsonObject m in (goc.Get("messages") as List<object> ?? new List<object>()).OfType<JsonObject>())
                foreach (JsonObject k in (m.Get("content") as List<object> ?? new List<object>()).OfType<JsonObject>())
                    if (k.GetString("type") == "text") user.Append(k.GetString("text")).Append("\n\n");
            var tin = new List<object>();
            var s = new JsonObject(); s.Set("role", "system"); s.Set("content", sys.ToString().Trim()); tin.Add(s);
            var u = new JsonObject(); u.Set("role", "user"); u.Set("content", user.ToString().Trim()); tin.Add(u);
            var body = new JsonObject();
            body.Set("model", string.IsNullOrWhiteSpace(modelLing) ? ModelLingMacDinh : modelLing.Trim());
            body.Set("stream", false);
            body.Set("messages", tin);
            return JsonLite.Serialize(body);
        }

        /// <summary>Phản hồi Ling → dạng phản hồi Claude (usage = 0: giá Ling khác Claude, không hiện $ sai). Lỗi → {"type":"error"}.</summary>
        public static string DoiPhanHoiLing(string json)
        {
            JsonObject goc;
            try { goc = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { goc = null; }
            if (goc == null) return Loi("Phản hồi Ling không đọc được: " + DongCuoi(json));
            if (goc.Get("error") is JsonObject e) return Loi("Ling: " + (e.GetString("message") ?? "lỗi không rõ"));
            var choice = (goc.Get("choices") as List<object>)?.OfType<JsonObject>().FirstOrDefault();
            string text = ((choice?.Get("message") as JsonObject)?.GetString("content") ?? "").Trim();
            int a = text.IndexOf('{'), b = text.LastIndexOf('}');
            if (a < 0 || b <= a) return Loi("Ling không trả JSON" + (text.Length > 0 ? ": " + DongCuoi(text) : ""));
            var kq = new JsonObject();
            kq.Set("stop_reason", choice?.GetString("finish_reason") == "length" ? "max_tokens" : "end_turn");
            var us = new JsonObject();
            us.Set("input_tokens", new JsonNumber("0")); us.Set("output_tokens", new JsonNumber("0"));
            kq.Set("usage", us);
            var k = new JsonObject();
            k.Set("type", "text"); k.Set("text", text.Substring(a, b - a + 1));
            kq.Set("content", new List<object> { k });
            return JsonLite.Serialize(kq);
        }

        /// <summary>Phần cần cho CLI tách từ body API.</summary>
        public sealed class YeuCau
        {
            public string Model = "", Effort = "high", Schema = "", HuongDan = "", PdfBase64, AnhBase64;
        }

        /// <summary>Body POST /v1/messages → lời dặn + quy chuẩn + phần người dùng (một văn bản), PDF base64, mẫu JSON, model, effort.</summary>
        public static YeuCau TachBody(string body)
        {
            var o = JsonLite.Parse(body) as JsonObject ?? throw new ArgumentException("body không phải JSON");
            var yc = new YeuCau { Model = o.GetString("model") ?? "" };
            var sb = new StringBuilder();
            foreach (JsonObject k in (o.Get("system") as List<object> ?? new List<object>()).OfType<JsonObject>())
                sb.Append(k.GetString("text")).Append("\n\n");
            sb.Append("---\n\n");
            foreach (JsonObject m in (o.Get("messages") as List<object> ?? new List<object>()).OfType<JsonObject>())
                foreach (JsonObject k in (m.Get("content") as List<object> ?? new List<object>()).OfType<JsonObject>())
                {
                    if (k.GetString("type") == "text") sb.Append(k.GetString("text")).Append("\n\n");
                    else if (k.GetString("type") == "document" && k.Get("source") is JsonObject src) yc.PdfBase64 = src.GetString("data");
                    else if (k.GetString("type") == "image" && k.Get("source") is JsonObject srcA) yc.AnhBase64 = srcA.GetString("data");
                }
            yc.HuongDan = sb.ToString().TrimEnd();
            if (o.Get("output_config") is JsonObject oc)
            {
                yc.Effort = oc.GetString("effort") ?? "high";
                if (oc.Get("format") is JsonObject f && f.Get("schema") is JsonObject s) yc.Schema = JsonLite.Serialize(s);
            }
            return yc;
        }

        public const string TepYeuCau = "yeu_cau.md", TepPdf = "ban_ve.pdf", TepAnh = "anh_tam.png";

        /// <summary>Lời nhắn ngắn trên dòng lệnh (lời dặn dài nằm trong yeu_cau.md — dòng lệnh Windows giới hạn ~32 nghìn ký tự).</summary>
        public static string LoiNhan(bool coPdf, bool coAnh = false) =>
            "Đọc tệp " + TepYeuCau + " trong thư mục làm việc và làm đúng theo hướng dẫn trong đó" + (coPdf ? "; bản vẽ cần đọc là tệp " + TepPdf + " cùng thư mục" : "") +
            (coAnh ? "; ảnh cần xem là tệp " + TepAnh + " cùng thư mục" : "") +
            ". Chỉ đọc tệp — không sửa, không tạo tệp, không chạy lệnh. Trả kết quả đúng JSON schema đã cho.";

        /// <summary>Tham số dòng lệnh cho claude -p / agy -p (thư mục làm việc = thuMuc).</summary>
        public static List<string> ThamSo(string nguon, YeuCau yc, string thuMuc)
        {
            var a = new List<string> { "-p", LoiNhan(yc.PdfBase64 != null, yc.AnhBase64 != null), "--output-format", "json", "--json-schema", yc.Schema, "--effort", yc.Effort };
            if (nguon == ClaudeCode)
            {
                a.AddRange(new[] { "--allowed-tools", "Read" }); // chỉ cho đọc tệp; công cụ khác bị từ chối trong chế độ -p
                if (yc.Model.StartsWith("claude-", StringComparison.Ordinal)) a.AddRange(new[] { "--model", yc.Model });
            }
            else a.AddRange(new[] { "--add-dir", thuMuc, "--print-timeout", "600s" }); // Antigravity: model mặc định của gói
            return a;
        }

        /// <summary>
        /// Một tham số theo luật CommandLineToArgvW: bọc ngoặc kép, \" cho ngoặc kép bên trong, nhân đôi \ đứng trước ngoặc kép.
        /// </summary>
        public static string ThoatThamSo(string s)
        {
            var sb = new StringBuilder("\"");
            int gach = 0;
            foreach (char c in s ?? "")
            {
                if (c == '\\') { gach++; continue; }
                if (c == '"') { sb.Append('\\', gach * 2 + 1).Append('"'); gach = 0; continue; }
                sb.Append('\\', gach).Append(c);
                gach = 0;
            }
            return sb.Append('\\', gach * 2).Append('"').ToString();
        }

        /// <summary>
        /// Kết quả JSON của CLI → phản hồi dạng API (content[0].text = JSON kết quả, stop_reason, usage = 0 vì tính trong gói).
        /// Lỗi (is_error, status khác SUCCESS, không có kết quả) → {"type":"error","error":{...}}.
        /// </summary>
        public static string DoiKetQua(string ra)
        {
            JsonObject o = null;
            foreach (string dong in Enumerable.Reverse((ra ?? "").Replace("\r", "").Split('\n'))) // net48: .Reverse() bị bắt nhầm sang Span
            {
                string d = dong.Trim();
                if (!d.StartsWith("{", StringComparison.Ordinal)) continue;
                try { o = JsonLite.Parse(d) as JsonObject; } catch (Exception) { o = null; }
                if (o != null) break;
            }
            if (o == null) return Loi("CLI không trả JSON: " + DongCuoi(ra));
            bool hong = o.Get("is_error") is bool e && e || (o.GetString("status") is string st && st != "SUCCESS");
            string chu = o.Get("structured_output") is JsonObject so ? JsonLite.Serialize(so) : BoRao(o.GetString("result") ?? o.GetString("response") ?? "");
            if (hong || chu.Trim().Length == 0)
                return Loi((o.GetString("result") ?? o.GetString("error") ?? o.GetString("message") ?? o.GetString("status") ?? "CLI báo lỗi").Trim());
            var kq = new JsonObject();
            kq.Set("stop_reason", "end_turn");
            var u = new JsonObject();
            u.Set("input_tokens", new JsonNumber("0"));
            u.Set("output_tokens", new JsonNumber("0"));
            kq.Set("usage", u);
            var k = new JsonObject();
            k.Set("type", "text");
            k.Set("text", chu);
            kq.Set("content", new List<object> { k });
            return JsonLite.Serialize(kq);
        }

        /// <summary>Bỏ rào ```json … ``` nếu CLI trả JSON trong khối mã.</summary>
        private static string BoRao(string s)
        {
            string t = (s ?? "").Trim();
            if (!t.StartsWith("```", StringComparison.Ordinal)) return t;
            int a = t.IndexOf('\n'), b = t.LastIndexOf("```", StringComparison.Ordinal);
            return a > 0 && b > a ? t.Substring(a + 1, b - a - 1).Trim() : t;
        }

        private static string Loi(string msg)
        {
            var e = new JsonObject();
            e.Set("type", "cli_error");
            e.Set("message", msg);
            var o = new JsonObject();
            o.Set("type", "error");
            o.Set("error", e);
            return JsonLite.Serialize(o);
        }

        private static string DongCuoi(string s) => (s ?? "").Trim().Split('\n').LastOrDefault()?.Trim() ?? "";
    }
}
