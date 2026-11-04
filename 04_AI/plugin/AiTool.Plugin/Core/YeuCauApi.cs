using System.Collections.Generic;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    /// <summary>
    /// Dựng body JSON cho POST /v1/messages của Claude API. Lời dặn + bảng vật liệu nằm ở
    /// system (khối vật liệu đánh dấu cache), dữ liệu lô nằm ở user; AI phải trả JSON đúng schema.
    /// </summary>
    public static class YeuCauApi
    {
        public const string Url = "https://api.anthropic.com/v1/messages";
        public const string PhienBanApi = "2026-10-21";
        public const string BetaFallback = "server-side-fallback-2026-07-01";
        public const int MaxTokens = 16000;

        public static string Body(string model, string loiDan, string khoiVatLieu, string duLieuLo)
        {
            var body = new JsonObject();
            body.Set("model", model);
            body.Set("max_tokens", MaxTokens);
            body.Set("fallbacks", "default");

            var format = new JsonObject();
            format.Set("type", "json_schema");
            format.Set("schema", Schema());
            var oc = new JsonObject();
            oc.Set("effort", "medium");
            oc.Set("format", format);
            body.Set("output_config", oc);

            var cache = new JsonObject();
            cache.Set("type", "ephemeral");
            body.Set("system", new List<object>
            {
                KhoiText(loiDan, null),
                KhoiText(khoiVatLieu, cache),
            });

            var msg = new JsonObject();
            msg.Set("role", "user");
            msg.Set("content", duLieuLo);
            body.Set("messages", new List<object> { msg });

            return JsonLite.Serialize(body);
        }

        private static JsonObject KhoiText(string text, JsonObject cache)
        {
            var o = new JsonObject();
            o.Set("type", "text");
            o.Set("text", text ?? string.Empty);
            if (cache != null) o.Set("cache_control", cache);
            return o;
        }

        private static JsonObject Kieu(string type)
        {
            var o = new JsonObject();
            o.Set("type", type);
            return o;
        }

        private static JsonObject Enum(params string[] gt)
        {
            JsonObject o = Kieu("string");
            o.Set("enum", new List<object>(gt));
            return o;
        }

        private static JsonObject Schema()
        {
            var thuocTinh = new JsonObject();
            thuocTinh.Set("khung", Kieu("integer"));
            thuocTinh.Set("muc_do", Enum("do", "vang"));
            thuocTinh.Set("loai_loi", Enum("MTZ", "Q", "GHI_CHU", "KHAC"));
            thuocTinh.Set("ly_do", Kieu("string"));
            thuocTinh.Set("goi_y", Kieu("string"));
            JsonObject item = Kieu("object");
            item.Set("properties", thuocTinh);
            item.Set("required", new List<object> { "khung", "muc_do", "loai_loi", "ly_do", "goi_y" });
            item.Set("additionalProperties", false);

            JsonObject mang = Kieu("array");
            mang.Set("items", item);
            var goc = new JsonObject();
            goc.Set("canh_bao", mang);
            JsonObject schema = Kieu("object");
            schema.Set("properties", goc);
            schema.Set("required", new List<object> { "canh_bao" });
            schema.Set("additionalProperties", false);
            return schema;
        }
    }
}
