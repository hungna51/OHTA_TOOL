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

            Console.WriteLine("=== KevApi ===");
            var kTapu = new KhungDuLieu
            {
                SoThuTu = 1, MinX = 0, MinY = 0, MaxX = 300, MaxY = 200,
                DoiTuong = new List<DoiTuong>
                {
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = DongChu.LayerFname, NoiDung = "N=PL1\nM=SS400\nT=9\nQ=2\nZ=SG-K\nV=1-90-100" },
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = "4-TEXT", NoiDung = "PL9x150x200 2枚 M12タップ" },
                    new DoiTuong { Loai = LoaiDoiTuong.Circle, Layer = "0-KAKOU", Cx = 50.0, Cy = 50.0, R = 5.0 },
                    new DoiTuong { Loai = LoaiDoiTuong.Circle, Layer = "3-KEGAKI", Cx = 50.0, Cy = 50.0, R = 6.0 },
                },
            };
            DacTrungKhung dt1 = KevApi.PhanTich(kTapu, "");
            Kiem("KevApi: phat hien lo kep dong tam + tapu + be", dt1.SoCapDongTam + "|" + dt1.CoTapu + "|" + dt1.CoBe, "1|True|True");
            string kevReq = KevApi.TaoBody(kTapu, "");
            var kevBody = (JsonObject)JsonLite.Parse(kevReq);
            var qObj = (JsonObject)kevBody.Get("questions");
            Kiem("KevApi: request co questions be/tapu/nghi_loi",
                 (qObj.Get("be") != null) + "," + (qObj.Get("tapu") != null) + "," + (qObj.Get("nghi_loi") != null),
                 "True,True,True");

            string fakeKevResp = "{\"answers\":{\"be\":{\"noul\":0.92},\"vat\":{\"noul\":0.10},\"cuon\":{\"noul\":0.08},\"sara\":{\"noul\":0.12},\"tapu\":{\"noul\":0.95},\"nghi_loi\":{\"noul\":0.04}},\"latency_ms\":15.5}";
            KetQuaKhungKev kqKev = KevApi.DocPhanHoi(fakeKevResp, kTapu, "");
            Kiem("KevApi: doc phan hoi BE - TAPU", kqKev.NhanGiaCong, "BE - TAPU");
            Kiem("KevApi: khung hop le khong nghi loi", kqKev.NghiLoi, false);

            var kSaiZ = new KhungDuLieu
            {
                SoThuTu = 44, MinX = 0, MinY = 0, MaxX = 318, MaxY = 144,
                DoiTuong = new List<DoiTuong>
                {
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = DongChu.LayerFname, NoiDung = "N=130304-2-44\nM=SS400\nT=6.0\nQ=2\nZ=SPHC" },
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = "4-TEXT", NoiDung = "ｹｶﾞｷ線 t=6mm\n本図　２枚" },
                },
            };
            string bangVlMau = "## BANG VAT LIEU CHUAN (M,Loai,Z,T_list)\nSS400,SS,SS400,3.2 4.5 6.0 9.0\nSPHC,SS,SPC,1.6 2.3 3.2";
            DacTrungKhung dtSaiZ = KevApi.PhanTich(kSaiZ, bangVlMau);
            Kiem("KevApi: phat hien Z=SPHC sai voi M=SS400 -> 1 canh bao MTZ",
                 dtSaiZ.CanhBaoCucBo.Count + "|" + dtSaiZ.CanhBaoCucBo[0].LoaiLoi + "|" + dtSaiZ.CanhBaoCucBo[0].GoiY,
                 "1|MTZ|Z=SS400");

            string tamKev = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aitool_kev_md_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tamKev);
            try
            {
                CauHinhKev chMacDinh = CauHinhKev.DocTuThuMuc(tamKev);
                Kiem("CauHinhKev: doc 00-index.md + 3 file con = 4 file", chMacDinh.DanhSachFileDaNap.Count, 4);
                Kiem("CauHinhKev: 5 loai gia cong mac dinh",
                     string.Join(",", chMacDinh.DanhSachLoai.Select(x => x.Ma)), "BE,VAT,CUON,SARA,TAPU");

                // Thu them 1 nhom gia cong moi [BARING] va 1 luat moi [LUAT: BAO_VE] vao file .md
                string f01 = System.IO.Path.Combine(CauHinhKev.DuongDanThuMucKev(tamKev), CauHinhKev.FilePhanLoai);
                System.IO.File.AppendAllText(f01, "\n## [BARING] Dap lo be mep\n- tu_khoa_ghi_chu: バーリング\n- cau_hoi_kev: Does this part have burring?\n");
                string f03 = System.IO.Path.Combine(CauHinhKev.DuongDanThuMucKev(tamKev), CauHinhKev.FileLuatGhiChu);
                System.IO.File.AppendAllText(f03, "\n## [LUAT: BAO_VE] Bao ve be mat\n- muc_do: vang\n- loai_loi: GHI_CHU\n- khi_ghi_chu_co: 保護シート\n- bat_buoc_fname_co: 保護シート\n- ly_do: Thiếu ghi chú bảo vệ bề mặt trong F-name.\n- goi_y: Thêm 保護シート\n");

                CauHinhKev chMoi = CauHinhKev.DocTuThuMuc(tamKev);
                var kCustom = new KhungDuLieu
                {
                    SoThuTu = 9, MaxX = 100, MaxY = 100,
                    DoiTuong = new List<DoiTuong>
                    {
                        new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = DongChu.LayerFname, NoiDung = "N=A1\nM=SS400\nT=6.0\nQ=1\nZ=SS400" },
                        new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = "4-TEXT", NoiDung = "バーリング加工 保護シート" },
                    },
                };
                DacTrungKhung dtCustom = KevApi.PhanTich(kCustom, "", chMoi);
                Kiem("CauHinhKev: tu nhan nhom moi BARING tu file .md", KevApi.GopNhanTheoCauHinh(dtCustom, chMoi), "BARING");
                Kiem("CauHinhKev: tu bat luat moi BAO_VE tu file .md",
                     dtCustom.CanhBaoCucBo.Count + "|" + dtCustom.CanhBaoCucBo[0].Do + "|" + dtCustom.CanhBaoCucBo[0].GoiY,
                     "1|False|Thêm 保護シート");

                // Test dong bo luat .md tu JSON GET /v1/rules cua May chu Kev ve may con
                string jsonSync = "{\"server_host\":\"NGUYEN-A-HUNG\",\"kev_files\":{\"04-luat-lan.md\":\"## [SON] Son tinh dien\\n- tu_khoa_ghi_chu: 塗装\\n\"},\"huong_dan_ai\":\"# HD MOI TU LAN\"}";
                int soFileSync = CauHinhKev.ApDungGoiLuatJson(jsonSync, tamKev);
                Kiem("CauHinhKev: ApDungGoiLuatJson ghi 2 file (.md con + huong_dan_ai.md)",
                     soFileSync + "|" + System.IO.File.Exists(System.IO.Path.Combine(CauHinhKev.DuongDanThuMucKev(tamKev), "04-luat-lan.md")),
                     "2|True");
            }
            finally
            {
                System.IO.Directory.Delete(tamKev, true);
            }

            // Kiểm tra các quy tắc đồng bộ Lisp Xuất CSV (xuat_csv_v5.0.1.lsp)
            var kCsv = new KhungDuLieu
            {
                SoThuTu = 50, MinX = 0, MinY = 0, MaxX = 400, MaxY = 300,
                DoiTuong = new List<DoiTuong>
                {
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = DongChu.LayerFname, LaMText = true, NoiDung = "N=PL50\nM=SS400\nT=6.0\nQ=1\nV=1-90-100\nZ=SS400\n内R10-発押し90°" },
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = DongChu.LayerFname, LaMText = false, NoiDung = "TEXT_ROI_TREN_FNAME" },
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = "4-TEXT", X1 = 20, Y1 = 20, NoiDung = "裏面CPL 開先施工範囲" },
                    new DoiTuong { Loai = LoaiDoiTuong.Line, Layer = "0-KAKOU", KieuNet = "DASHED", X1 = 100, Y1 = 10, X2 = 100, Y2 = 290 },
                    new DoiTuong
                    {
                        Loai = LoaiDoiTuong.Text, Layer = "KHUNG_IN", LaMText = true,
                        NoiDung = string.Join("\n", new[]
                        {
                            "N=PL50", "M=SS400", "T=6.0", "Q=1", "V=1-90-100", "Z=SS400",
                            "TAPPU: 0", "SARA: 0", "TAPU&SARA: 0",
                            "Các loại gia công: m, k",
                            "Chiều dài vát: 0",
                            "Pitchi - Số lần: 0",
                            "Pitchi - Chiều dài (mm): 0",
                            "1. Chiều dài: 280 - Số lần: 1"
                        })
                    }
                }
            };
            DacTrungKhung dtCsv = KevApi.PhanTich(kCsv, bangVlMau);
            Kiem("CSV: phan loai BE - VAT tu net DASHED + 開先施工範囲", KevApi.GopNhanTheoCauHinh(dtCsv, null), "BE - VAT");
            Kiem("CSV: phat hien tron TEXT/MTEXT tren 2-F-NAME",
                 dtCsv.CanhBaoCucBo.Any(c => c.LyDo.Contains("trộn lẫn cả TEXT và MTEXT")), true);
            Kiem("CSV: phat hien CPL nhung M/Z chua doi sang ムキ 縞板 / SS400-SIMA",
                 dtCsv.CanhBaoCucBo.Any(c => c.LoaiLoi == "MTZ" && c.GoiY.Contains("SS400-SIMA")), true);
            Kiem("CSV: phat hien MText KHUNG_IN co k nhung Chieu dai vat = 0",
                 dtCsv.CanhBaoCucBo.Any(c => c.LyDo.Contains("Chiều dài vát: 0")), true);
            Kiem("CSV: phat hien tam be hon hop (V= + 発押し) thieu chu chu thich quanh duong be",
                 dtCsv.CanhBaoCucBo.Any(c => c.LyDo.Contains("Tấm bẻ hỗn hợp")), true);

            // Kiểm tra liên khung: trùng N= và tự động bật kiểm tra dim 1-XY khi bản vẽ đã có layer 1-XY
            var kDup1 = new KhungDuLieu
            {
                SoThuTu = 1, MaxX = 200, MaxY = 100,
                DoiTuong = new List<DoiTuong>
                {
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = DongChu.LayerFname, NoiDung = "N=DUP-01\nM=SS400\nT=6.0\nQ=1\nZ=SS400" },
                    new DoiTuong { Loai = LoaiDoiTuong.Dim, Layer = "1-XY", GiaTriDo = 200 },
                }
            };
            var kDup2 = new KhungDuLieu
            {
                SoThuTu = 2, MaxX = 200, MaxY = 100,
                DoiTuong = new List<DoiTuong>
                {
                    new DoiTuong { Loai = LoaiDoiTuong.Text, Layer = DongChu.LayerFname, NoiDung = "N=DUP-01\nM=SS400\nT=6.0\nQ=1\nZ=SS400" },
                }
            };
            var cbLien = KevApi.KiemTraLienKhung(new List<KhungDuLieu> { kDup1, kDup2 }, bangVlMau);
            Kiem("CSV LienKhung: bao trung N=DUP-01 o ca 2 khung + khung #2 thieu dim 1-XY",
                 cbLien.Count(x => x.CB.LyDo.Contains("Trùng tên tấm N=DUP-01")) + "|" +
                 cbLien.Count(x => x.Khung.SoThuTu == 2 && x.CB.LyDo.Contains("layer 1-XY")),
                 "2|1");
            Console.WriteLine();
        }
    }
}
