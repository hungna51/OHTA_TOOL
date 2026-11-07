using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AiTool.Core;
using OhtaTool.Plugin.Core;

namespace AiTool.Tests
{
    internal static partial class Program
    {
        private static void TestChatLing()
        {
            Console.WriteLine("=== ChatLing ===");
            string bv = Path.Combine("C:\\kh", "A", "BV-01.dwg");
            Kiem("ThuMucAi = <thu muc ban ve>\\OHTA_AI", ChatLing.ThuMucAi(bv), Path.Combine("C:\\kh", "A", "OHTA_AI"));
            Kiem("ThuMucMoTa = OHTA_AI\\mo-ta", ChatLing.ThuMucMoTa(bv), Path.Combine("C:\\kh", "A", "OHTA_AI", "mo-ta"));
            Kiem("ThuMucChat = OHTA_AI\\chat\\<ten>", ChatLing.ThuMucChat(bv, "C:\\L"), Path.Combine("C:\\kh", "A", "OHTA_AI", "chat", "BV-01"));
            Kiem("Ban ve chua luu -> ThuMucAi null", ChatLing.ThuMucAi("Drawing1.dwg"), null);
            Kiem("Ban ve chua luu -> chat vao LOCALAPPDATA", ChatLing.ThuMucChat("Drawing1.dwg", "C:\\L"),
                 Path.Combine("C:\\L", "OHTA_TOOL", "AI", "chat", "_chua-luu"));
            Kiem("Duoi la .dwt -> null", ChatLing.ThuMucMoTa("C:\\x\\mau.dwt"), null);
            Kiem("TenFileMoi", ChatLing.TenFileMoi(new DateTime(2026, 11, 7, 15, 30, 5)), "2026-11-07_153005.json");

            // Body: system + toi da 20 tin gan nhat, dinh kem dung truoc cau hoi
            var ls = new List<TinChat>();
            for (int i = 0; i < 25; i++)
                ls.Add(new TinChat { Vai = i % 2 == 0 ? "user" : "assistant", NoiDung = "t" + i });
            ls.Add(new TinChat { Vai = "assistant", NoiDung = "" }); // tin rong bi bo
            var body = (JsonObject)JsonLite.Parse(ChatLing.TaoBody("Ling-3.2-flash", "LOI DAN", ls));
            var msgs = (List<object>)body.Get("messages");
            Kiem("Body: model", body.GetString("model"), "Ling-3.2-flash");
            Kiem("Body: stream = true", body.Get("stream"), true);
            Kiem("Body: 1 system + 20 tin", msgs.Count, 21);
            Kiem("Body: tin dau la system", ((JsonObject)msgs[0]).GetString("content"), "LOI DAN");
            Kiem("Body: giu 20 tin cuoi (bat dau t5)", ((JsonObject)msgs[1]).GetString("content"), "t5");
            Kiem("Body: tin cuoi la t24", ((JsonObject)msgs[20]).GetString("content"), "t24");

            var dk = new TinChat { Vai = "user", NoiDung = "Tam nay be may lan?", DinhKem = "## Tam 1 — A\n- F-name: N=A" };
            string gui = ChatLing.NoiDungGui(dk);
            Kiem("Dinh kem dung truoc cau hoi", gui.IndexOf("## Tam 1", StringComparison.Ordinal) < gui.IndexOf("Tam nay be", StringComparison.Ordinal), true);
            Kiem("Khong dinh kem -> chi cau hoi", ChatLing.NoiDungGui(new TinChat { Vai = "user", NoiDung = "hi" }), "hi");

            // SSE
            var c = ChatLing.DocDongSse("data: {\"choices\":[{\"delta\":{\"content\":\"Xin\"}}]}");
            Kiem("SSE: lay content", c.Chu, "Xin");
            var r = ChatLing.DocDongSse("data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"nghi\"}}]}");
            Kiem("SSE: lay reasoning_content", r.SuyNghi, "nghi");
            Kiem("SSE: [DONE] -> Xong", ChatLing.DocDongSse("data: [DONE]").Xong, true);
            Kiem("SSE: tbox viet 'data:' khong cach", ChatLing.DocDongSse("data:{\"choices\":[{\"delta\":{\"content\":\"A\"}}]}").Chu, "A");
            Kiem("SSE: dong trong -> khong co gi", ChatLing.DocDongSse("").Chu == null && !ChatLing.DocDongSse("").Xong, true);
            Kiem("SSE: JSON hong -> khong nem", ChatLing.DocDongSse("data: {hong").Chu, null);
            Kiem("SSE: choices rong -> null", ChatLing.DocDongSse("data: {\"choices\":[]}").Chu, null);

            // Luu / doc lich su
            var goc = new List<TinChat>
            {
                new TinChat { Vai = "user", NoiDung = "Câu hỏi \"có\" dấu\nhai dòng", DinhKem = "## Tam 1", Luc = "2026-11-07 15:30:05" },
                new TinChat { Vai = "assistant", NoiDung = "Trả lời", SuyNghi = "nghĩ", Luc = "2026-11-07 15:30:09" },
            };
            List<TinChat> doc = ChatLing.DocLichSu(ChatLing.GhiLichSu("Ling-3.2-flash", goc));
            Kiem("Lich su: so tin", doc.Count, 2);
            Kiem("Lich su: giu dau, ngoac kep, xuong dong", doc[0].NoiDung, "Câu hỏi \"có\" dấu\nhai dòng");
            Kiem("Lich su: giu dinh kem", doc[0].DinhKem, "## Tam 1");
            Kiem("Lich su: giu suy nghi", doc[1].SuyNghi, "nghĩ");
            Kiem("Lich su: vai la assistant", doc[1].Vai, "assistant");

            Kiem("NhanCuoc: ngay gio + cau dau", ChatLing.NhanCuoc("2026-11-07_153005.json", goc), "2026-11-07 15:30 · Câu hỏi \"có\" dấu hai dòng");
            Kiem("NhanCuoc: cuoc rong -> chi ngay gio", ChatLing.NhanCuoc("2026-11-07_153005.json", new List<TinChat>()), "2026-11-07 15:30");

            string dk2 = ChatLing.GhepDinhKem("## Tam 1 — A\n", "{\"x\": 1}\n");
            Kiem("Dinh kem co ca md va json", dk2.Contains("## Tam 1") && dk2.Contains("```json\n{\"x\": 1}\n```"), true);
            Kiem("Dinh kem: dem khung khong tinh phan json", dk2.Split('\n').Count(l => l.StartsWith("## Tam ", StringComparison.Ordinal)), 1);

            // LingDich: body Claude -> body Ling
            string bodyClaude = YeuCauApi.Body("x", "LOI DAN", "VAT LIEU", "DU LIEU LO");
            var bl = (JsonObject)JsonLite.Parse(LingDich.ChuyenBody(bodyClaude, "Ling-3.2-flash"));
            var ml = (List<object>)bl.Get("messages");
            string sysL = ((JsonObject)ml[0]).GetString("content");
            Kiem("LingDich: model Ling", bl.GetString("model"), "Ling-3.2-flash");
            Kiem("LingDich: khong stream", bl.Get("stream"), false);
            Kiem("LingDich: system gom loi dan + vat lieu", sysL.Contains("LOI DAN") && sysL.Contains("VAT LIEU"), true);
            Kiem("LingDich: system co schema canh_bao", sysL.Contains("canh_bao") && sysL.Contains("muc_do"), true);
            Kiem("LingDich: tin user giu du lieu lo", ((JsonObject)ml[1]).GetString("content"), "DU LIEU LO");
            Kiem("LingDich: khong mang cache_control/output_config", LingDich.ChuyenBody(bodyClaude, "L").Contains("cache_control"), false);

            // LingDich: phan hoi Ling -> dang Claude, PhanHoiApi doc duoc
            string phLing = "{\"choices\":[{\"message\":{\"content\":\"```json\\n{\\\"canh_bao\\\":[{\\\"khung\\\":2,\\\"muc_do\\\":\\\"do\\\",\\\"loai_loi\\\":\\\"Q\\\",\\\"ly_do\\\":\\\"sai Q\\\",\\\"goi_y\\\":\\\"\\\"}]}\\n```\"},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":120,\"completion_tokens\":30}}";
            KetQuaLo kl = PhanHoiApi.Doc(LingDich.ChuyenPhanHoi(phLing), new HashSet<int> { 2 });
            Kiem("LingDich: doc duoc canh bao qua ```json", kl.Loi == null && kl.CanhBao.Count == 1 && kl.CanhBao[0].Do, true);
            Kiem("LingDich: usage vao/ra", kl.SuDung.Vao + "/" + kl.SuDung.Ra, "120/30");
            KetQuaLo kc = PhanHoiApi.Doc(LingDich.ChuyenPhanHoi("{\"choices\":[{\"message\":{\"content\":\"{\\\"canh_bao\\\":[]}\"},\"finish_reason\":\"length\"}]}"), null);
            Kiem("LingDich: finish length -> bi cat", kc.Loi, PhanHoiApi.LoiBiCat);
            KetQuaLo kh = PhanHoiApi.Doc(LingDich.LoiHttp(404, "The Model not exists"), null);
            Kiem("LingDich: loi HTTP chu tron -> bao ro", kh.Loi, "Ling trả lỗi HTTP 404: The Model not exists");
            Kiem("LingDich: CatJson bo chu thua", LingDich.CatJson("Day la ket qua: {\"a\":1} het"), "{\"a\":1}");

            string tam = Path.Combine(Path.GetTempPath(), "aitool_chat_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tam);
            try
            {
                File.WriteAllText(Path.Combine(tam, "b.md"), "LUAT B");
                File.WriteAllText(Path.Combine(tam, "a.md"), "LUAT A");
                File.WriteAllText(Path.Combine(tam, "x.txt"), "BO QUA");
                string luat = ChatLing.DocBoLuat(tam);
                Kiem("Bo luat: theo thu tu ten", luat.IndexOf("LUAT A", StringComparison.Ordinal) < luat.IndexOf("LUAT B", StringComparison.Ordinal), true);
                Kiem("Bo luat: chi file .md", luat.Contains("BO QUA"), false);
                Kiem("Loi dan: co kien thuc nghiep vu", ChatLing.LoiDan(luat).Contains("# Kien thuc nghiep vu"), true);
                Kiem("Bo luat: thu muc khong co -> rong", ChatLing.DocBoLuat(Path.Combine(tam, "khong")), "");

                DuLieuTinh.GhiKey(tam, "sk-studio-abcd1234", DuLieuTinh.FileKeyLing);
                Kiem("Key Ling: doc lai", DuLieuTinh.DocKey(tam, DuLieuTinh.FileKeyLing), "sk-studio-abcd1234");
                Kiem("Key Ling khong de len key Claude", DuLieuTinh.DocKey(tam), null);
                DuLieuTinh.XoaKey(tam, DuLieuTinh.FileKeyLing);
                Kiem("Key Ling: xoa", DuLieuTinh.DocKey(tam, DuLieuTinh.FileKeyLing), null);
            }
            finally
            {
                Directory.Delete(tam, true);
            }
            Console.WriteLine();
        }
    }
}
