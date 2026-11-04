using System;
using System.Collections.Generic;
using System.Linq;
using AiTool.Core;
using OhtaTool.Plugin.Core;

namespace AiTool.Tests
{
    internal static partial class Program
    {
        internal static string PhanHoiMau(string canhBaoJson, string stop = "end_turn") =>
            "{\"id\":\"msg_1\",\"type\":\"message\",\"role\":\"assistant\",\"stop_reason\":\"" + stop + "\"," +
            "\"content\":[{\"type\":\"thinking\",\"thinking\":\"\"},{\"type\":\"text\",\"text\":" +
            JsonLite.Serialize(canhBaoJson).Trim() + "}]," +
            "\"usage\":{\"input_tokens\":1000,\"cache_creation_input_tokens\":200,\"cache_read_input_tokens\":300,\"output_tokens\":50}}";

        private static void TestApi()
        {
            Console.WriteLine("=== YeuCauApi ===");
            var body = (JsonObject)JsonLite.Parse(YeuCauApi.Body("claude-sonnet-5-5", "LOI DAN", "VAT LIEU", "#KHUNG 1"));
            Kiem("Body: model", body.GetString("model"), "claude-sonnet-5-5");
            Kiem("Body: max_tokens 16000", body.Get("max_tokens")?.ToString(), "16000");
            Kiem("Body: fallbacks default", body.GetString("fallbacks"), "default");
            var haiku = (JsonObject)JsonLite.Parse(YeuCauApi.Body("claude-haiku-5-5", "LD", "VL", "#KHUNG 1"));
            Kiem("Body Haiku: KHONG gui fallbacks (Haiku 5.5 khong co fallback)", haiku.Get("fallbacks"), null);
            Kiem("CoFallback: Sonnet co, Opus co, Haiku khong",
                 YeuCauApi.CoFallback("claude-sonnet-5-5") + "," + YeuCauApi.CoFallback("claude-opus-5-5") + "," + YeuCauApi.CoFallback("claude-haiku-5-5"),
                 "True,True,False");
            var sys = (List<object>)body.Get("system");
            Kiem("Body: system[0] = loi dan", ((JsonObject)sys[0]).GetString("text"), "LOI DAN");
            Kiem("Body: system[0] khong cache_control", ((JsonObject)sys[0]).Get("cache_control"), null);
            Kiem("Body: system[1] = vat lieu", ((JsonObject)sys[1]).GetString("text"), "VAT LIEU");
            Kiem("Body: cache_control o khoi vat lieu",
                 ((JsonObject)((JsonObject)sys[1]).Get("cache_control"))?.GetString("type"), "ephemeral");
            var msg = (JsonObject)((List<object>)body.Get("messages"))[0];
            Kiem("Body: user = du lieu lo", msg.GetString("role") + ":" + msg.GetString("content"), "user:#KHUNG 1");
            var oc = (JsonObject)body.Get("output_config");
            Kiem("Body: effort medium", oc.GetString("effort"), "medium");
            var fmt = (JsonObject)oc.Get("format");
            Kiem("Body: format json_schema", fmt.GetString("type"), "json_schema");
            var schema = (JsonObject)fmt.Get("schema");
            Kiem("Schema goc: khong cho thuoc tinh la", schema.Get("additionalProperties"), false);
            var item = (JsonObject)((JsonObject)((JsonObject)schema.Get("properties")).Get("canh_bao")).Get("items");
            Kiem("Schema item: khong cho thuoc tinh la", item.Get("additionalProperties"), false);
            Kiem("Schema item: required du 5 khoa",
                 string.Join(",", ((List<object>)item.Get("required")).Cast<string>()), "khung,muc_do,loai_loi,ly_do,goi_y");
            var mucDo = (JsonObject)((JsonObject)item.Get("properties")).Get("muc_do");
            Kiem("Schema muc_do enum do/vang", string.Join(",", ((List<object>)mucDo.Get("enum")).Cast<string>()), "do,vang");
            Console.WriteLine();

            Console.WriteLine("=== PhanHoiApi ===");
            var hopLe = new HashSet<int> { 1, 2 };
            string hai = "{\"canh_bao\":[" +
                "{\"khung\":1,\"muc_do\":\"do\",\"loai_loi\":\"MTZ\",\"ly_do\":\"Z sai\",\"goi_y\":\"Z=SUS-N2\"}," +
                "{\"khung\":2,\"muc_do\":\"vang\",\"loai_loi\":\"Q\",\"ly_do\":\"Q nghi\",\"goi_y\":\"\"}]}";
            KetQuaLo kq = PhanHoiApi.Doc(PhanHoiMau(hai), hopLe);
            Kiem("Doc: khong loi", kq.Loi, null);
            Kiem("Doc: 2 canh bao", kq.CanhBao.Count, 2);
            Kiem("Doc: canh bao 1 do, MTZ, goi y", kq.CanhBao[0].Khung + "|" + kq.CanhBao[0].Do + "|" + kq.CanhBao[0].LoaiLoi + "|" + kq.CanhBao[0].GoiY, "1|True|MTZ|Z=SUS-N2");
            Kiem("Doc: canh bao 2 vang", kq.CanhBao[1].Do, false);
            Kiem("Doc: usage", kq.SuDung.Vao + "/" + kq.SuDung.GhiCache + "/" + kq.SuDung.DocCache + "/" + kq.SuDung.Ra, "1000/200/300/50");

            string la = "{\"canh_bao\":[{\"khung\":9,\"muc_do\":\"do\",\"loai_loi\":\"Q\",\"ly_do\":\"x\",\"goi_y\":\"\"}]}";
            Kiem("DocPhanHoi_KhungLa_BoQua", PhanHoiApi.Doc(PhanHoiMau(la), hopLe).CanhBao.Count, 0);
            KetQuaLo tc = PhanHoiApi.Doc(PhanHoiMau("", "refusal"), hopLe);
            Kiem("DocPhanHoi_Refusal_BaoLoi", tc.Loi, "AI từ chối lô này");
            Kiem("Refusal van giu usage", tc.SuDung.Vao, 1000L);
            Kiem("DocPhanHoi_MaxTokens_BaoLoi", PhanHoiApi.Doc(PhanHoiMau("{\"canh", "max_tokens"), hopLe).Loi, "Phản hồi bị cắt (lô quá dài)");
            Kiem("Phan hoi JSON hong", PhanHoiApi.Doc(PhanHoiMau("khong phai json"), hopLe).Loi, "Phản hồi không đọc được");
            Kiem("Body response hong", PhanHoiApi.Doc("<html>", hopLe).Loi, "Phản hồi không đọc được");
            Kiem("Loi API authentication -> API key sai (tieng Viet)",
                 PhanHoiApi.Doc("{\"type\":\"error\",\"error\":{\"type\":\"authentication_error\",\"message\":\"invalid x-api-key\"}}", hopLe).Loi,
                 "API key sai hoặc đã bị xoá (kiểm tra 05_AI\\data\\ai.key)");
            Kiem("Loi API rate_limit -> qua tai 429",
                 PhanHoiApi.Doc("{\"type\":\"error\",\"error\":{\"type\":\"rate_limit_error\",\"message\":\"x\"}}", hopLe).Loi,
                 "AI quá tải (429), thử lại sau ít phút");
            Kiem("Loi API overloaded",
                 PhanHoiApi.Doc("{\"type\":\"error\",\"error\":{\"type\":\"overloaded_error\",\"message\":\"x\"}}", hopLe).Loi,
                 "AI đang quá tải, thử lại sau ít phút");
            Kiem("Loi API khac -> giu message",
                 PhanHoiApi.Doc("{\"type\":\"error\",\"error\":{\"type\":\"invalid_request_error\",\"message\":\"bad field\"}}", hopLe).Loi,
                 "Yêu cầu không hợp lệ: bad field");
            Kiem("Loi HTTP khong JSON (ClaudeHttp boc lai) -> co ma HTTP",
                 PhanHoiApi.Doc(PhanHoiApi.BocLoiHttp(502, "<html>Bad gateway</html>"), hopLe).Loi,
                 "Máy chủ AI trả lỗi HTTP 502");
            Kiem("Body JSON giu nguyen khi BocLoiHttp gap JSON", PhanHoiApi.BocLoiHttp(429, "{\"a\":1}"), "{\"a\":1}");
            Console.WriteLine();

            Console.WriteLine("=== BangGia ===");
            Kiem("Tien Sonnet: 1M vao = 2", BangGia.Tien("claude-sonnet-5-5", new SuDung { Vao = 1000000 }), 2.00m);
            Kiem("Tien Sonnet: 1M ra = 10", BangGia.Tien("claude-sonnet-5-5", new SuDung { Ra = 1000000 }), 10.00m);
            Kiem("Tien Sonnet: 1M doc cache = 0.2", BangGia.Tien("claude-sonnet-5-5", new SuDung { DocCache = 1000000 }), 0.20m);
            Kiem("Tien Sonnet: 1M ghi cache = 2.5", BangGia.Tien("claude-sonnet-5-5", new SuDung { GhiCache = 1000000 }), 2.50m);
            Kiem("Tien Opus: 1M vao = 4", BangGia.Tien("claude-opus-5-5", new SuDung { Vao = 1000000 }), 4.00m);
            Kiem("Tien Haiku: 1M ra = 0.5", BangGia.Tien("claude-haiku-5-5", new SuDung { Ra = 1000000 }), 0.50m);
            Kiem("Tien model la = 0", BangGia.Tien("model-la", new SuDung { Vao = 1000000 }), 0m);
            Kiem("Danh sach model: Sonnet dau tien", BangGia.Model[0], "claude-sonnet-5-5");
            Console.WriteLine();
        }
    }
}
