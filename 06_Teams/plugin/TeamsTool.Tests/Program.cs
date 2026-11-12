using System;
using System.Collections.Generic;
using System.Linq;
using TeamsTool.Core;

namespace TeamsTool.Tests
{
    /// <summary>Test PhanTichTeams bằng JSON mẫu dạng Microsoft Graph. Mã thoát 1 nếu có ca hỏng.</summary>
    internal static class Program
    {
        private static int _ok, _fail;

        private static void Kiem(string ten, object duoc, object mongDoi)
        {
            bool dung = Equals(duoc, mongDoi);
            if (dung) _ok++; else _fail++;
            Console.WriteLine("  {0} {1,-58} -> {2}{3}", dung ? "OK  " : "FAIL", ten, duoc ?? "null",
                              dung ? "" : "   (mong doi " + (mongDoi ?? "null") + ")");
        }

        private const string Tabs = @"{ ""value"": [
  { ""id"": ""t1"", ""displayName"": ""Bài đăng"", ""configuration"": { ""contentUrl"": null } },
  { ""id"": ""t2"", ""displayName"": ""注文書2026.1-VN"", ""webUrl"": ""https://teams.microsoft.com/l/entity/x"",
    ""configuration"": { ""entityId"": ""abc"", ""contentUrl"": ""https://kohtavn.sharepoint.com/sites/OHTA/_layouts/15/teamslogon.aspx?spfx=true&dest=https%3A%2F%2Fkohtavn.sharepoint.com%2Fsites%2FOHTA%2FLists%2F2026%2FAllItems.aspx%3Fp%3D11"" } },
  { ""id"": ""t3"", ""displayName"": ""Wiki"", ""configuration"": { ""websiteUrl"": ""https://example.com/page"" } }
]}";

        private const string Cots = @"{ ""value"": [
  { ""name"": ""Title"", ""displayName"": ""Title"", ""readOnly"": false, ""text"": {} },
  { ""name"": ""NguoiTK"", ""displayName"": ""Người triển khai"", ""personOrGroup"": {} },
  { ""name"": ""NguoiCheck1"", ""displayName"": ""Người check 1"", ""personOrGroup"": {} },
  { ""name"": ""TrangThai"", ""displayName"": ""Trạng thái"", ""choice"": {} },
  { ""name"": ""Han"", ""displayName"": ""Hạn hoàn thành"", ""dateTime"": {} },
  { ""name"": ""Status"", ""displayName"": ""ステータス"", ""choice"": {} },
  { ""name"": ""GiaCong"", ""displayName"": ""加工 (Gia Công)"", ""choice"": { ""allowMultiple"": true } },
  { ""name"": ""ChuY"", ""displayName"": ""注意 (Chú Ý)"", ""text"": {} },
  { ""name"": ""Created"", ""displayName"": ""Created"", ""readOnly"": true, ""dateTime"": {} },
  { ""name"": ""Attachments"", ""displayName"": ""Attachments"", ""boolean"": {} },
  { ""name"": ""An"", ""displayName"": ""Ẩn"", ""hidden"": true, ""text"": {} }
]}";

        private const string Items = @"{ ""@odata.nextLink"": ""https://graph.microsoft.com/v1.0/next"", ""value"": [
  { ""id"": ""1"", ""webUrl"": ""https://kohtavn.sharepoint.com/sites/OHTA/Lists/2026/1_.000"",
    ""fields"": { ""Title"": ""C129192"", ""NguoiTKLookupId"": ""12"", ""NguoiCheck1LookupId"": ""13"", ""TrangThai"": ""Đang triển khai"",
                ""Han"": ""2026-10-22T07:00:00Z"", ""GiaCong"": [""切"", ""曲""], ""ChuY"": ""<div>来月から&amp;</div>"" } },
  { ""id"": ""2"", ""fields"": { ""Title"": ""130575"", ""TrangThai"": ""Đã hoàn thành"" } },
  { ""id"": ""3"", ""fields"": { ""Title"": ""130651"", ""Status"": ""完了"", ""NguoiCheck1"": [ { ""LookupId"": 13, ""LookupValue"": ""Lê Gia Vinh"" } ] } }
]}";

        private const string Dxf = "  0\nSECTION\n  2\nHEADER\n  0\nENDSEC\n  0\nSECTION\n  2\nENTITIES\n" +
            "  0\nLINE\n  8\n0-KAKOU\n 10\n0\n 20\n0\n 11\n200\n 21\n0\n" +
            "  0\nLWPOLYLINE\n 90\n2\n 10\n0\n 20\n0\n 10\n200\n 20\n100\n" +
            "  0\nCIRCLE\n 10\n50\n 20\n50\n 40\n5.5\n" +
            "  0\nCIRCLE\n 10\n150\n 20\n50\n 40\n5.5\n" +
            "  0\nTEXT\n  1\nSS400 t=4.5 \\U+6570\\U+91CF 2\n" +
            "  0\nMTEXT\n  3\n{\\fArial|b0;\\U+66F2}\n  1\n\\PR5\n" +
            "  0\nENDSEC\n  0\nEOF\n";

        /// <summary>3 phòng (12/11): đọc một lượt (tiết diện + nghi vấn), tiết diện dạng chữ, luật tiết diện, bài học.</summary>
        private static void TestBaPhong()
        {
            Console.WriteLine("-- 3 phong: doc mot luot / tiet dien chu / bai hoc --");
            string loi;
            TietDien td = PhongTrienKhai.DocChuTd("50 b90 100 a90r10 40", out loi);
            Kiem("td chu: doc", td.Doan.Count + "|" + td.Uon.Count + "|" + td.Uon[1].Kieu + "|" + td.Uon[1].R + "|" + (loi ?? "null"), "3|2|an_1_lan|10|null");
            Kiem("td chu: ghi lai giong", PhongTrienKhai.ChuTd(td), "50 b90 100 a90r10 40");
            Kiem("td chu: cuon + pitchi + dau +", PhongTrienKhai.ChuTd(PhongTrienKhai.DocChuTd("0 + c360r50 + 0", out loi)) + "|" + PhongTrienKhai.DocChuTd("20 P90 20", out loi).Uon[0].Kieu, "0 c360r50 0|pitchi");
            Kiem("td chu: rong = khong uon", PhongTrienKhai.DocChuTd("", out loi).Doan.Count + "|" + PhongTrienKhai.DocChuTd("300", out loi).Doan.Count, "0|1");
            Kiem("td chu: sai ket thuc bang uon", PhongTrienKhai.DocChuTd("50 b90", out loi) == null && loi.Contains("kết thúc"), true);
            Kiem("td chu: sai ky hieu", PhongTrienKhai.DocChuTd("50 x90 50", out loi) == null && loi.Contains("x90"), true);

            // Phản hồi đọc bản vẽ một lượt: tiết diện cấu trúc → chữ, nghi vấn → VanDe nguồn "doc"
            string text = "{\"tam\":[{\"so_ban_ve\":\"A-1\",\"ten\":\"L\",\"M\":\"SPCC\",\"Z\":\"\",\"T\":\"1.6\",\"Q\":\"2\",\"mat\":\"\",\"kich_thuoc\":\"100x50\",\"dung_sai\":\"\",\"ghi_chu_jp\":\"\",\"ghi_chu_vi\":\"\",\"trang\":\"1\",\"canh\":[],\"lo\":[],\"khong_chac\":[],"
                 + "\"kieu_td\":\"be\",\"x\":{\"doan\":[50,30],\"uon\":[{\"kieu\":\"be\",\"goc\":90,\"r_trong\":0,\"cung\":0}]},\"y\":{\"doan\":[200],\"uon\":[]},\"ghi_chu_td\":\"bẻ V\",\"bien_dang\":\"be\",\"mo_ta_bien_dang\":\"\"}],"
                 + "\"van_de\":[{\"tam\":\"A-1\",\"muc\":\"do\",\"nhom\":\"khach\",\"muc_qc\":\"\",\"noi_dung\":\"Lỗ M5 cách mép bao nhiêu?\",\"lien_lac\":false}]}";
            var ph = PhongDocBanVe.DocPhanHoi("{\"stop_reason\":\"end_turn\",\"content\":[{\"type\":\"text\",\"text\":" + OhtaTool.Plugin.Core.JsonLite.Serialize(text) + "}]}", "A.pdf");
            TamAi t = ph.Tam[0];
            Kiem("doc 1 luot: tiet dien chu", t.KieuTd + "|" + t.TdX + "|" + t.TdY + "|" + t.BienDang, "be|50 b90 30|200|be");
            Kiem("doc 1 luot: nghi van", ph.VanDe.Count + "|" + ph.VanDe[0].Nguon + "|" + ph.VanDe[0].Ma.StartsWith("ai|a.pdf|") + "|" + ph.VanDe[0].Muc, "1|doc|True|do");
            Kiem("schema doc co van_de + x", PhongDocBanVe.Body("claude-opus-5-5", "d", "q", "A.pdf", null, null, "Thư viện").Contains("\"van_de\"")
                 && PhongDocBanVe.Body("claude-opus-5-5", "d", "q", "A.pdf", null, null, "Thư viện biên dạng").Contains("Thư viện biên dạng"), true);

            // Lưu / đọc chi_tiet.json giữ tiết diện, giá trị AI, nghi vấn, bài học
            t.Tep = "A.pdf";
            t.GiaTriAi["T"] = "1.6"; t.T = "2.0"; t.DaSua.Add("T"); t.LyDoSua = "khách sửa";
            var ct = new ChiTiet { Tam = { t }, NghiVan = ph.VanDe, BaiHoc = { new DeXuatBaiHoc { NoiDung = "Khi …", TrangThai = "" } } };
            ct.DaRutBaiHoc.Add("x");
            ChiTiet ct2 = PhongDocBanVe.Doc(PhongDocBanVe.Ghi(ct));
            TamAi t2 = ct2.Tam[0];
            Kiem("chi_tiet: giu td + gia tri AI", t2.TdX + "|" + t2.KieuTd + "|" + t2.GiaTriAi["T"] + "|" + t2.LyDoSua, "50 b90 30|be|1.6|khách sửa");
            Kiem("chi_tiet: giu nghi van + bai hoc", ct2.NghiVan.Count + "|" + ct2.NghiVan[0].Nguon + "|" + ct2.BaiHoc.Count + "|" + ct2.DaRutBaiHoc.Count, "1|doc|1|1");
            // Đọc lại: ô đã sửa giữ giá trị người + giá trị AI gốc
            var moi = PhongDocBanVe.Doc(PhongDocBanVe.Ghi(new ChiTiet { Tam = { PhongDocBanVe.DocPhanHoi("{\"stop_reason\":\"end_turn\",\"content\":[{\"type\":\"text\",\"text\":" + OhtaTool.Plugin.Core.JsonLite.Serialize(text) + "}]}").Tam[0] } })).Tam;
            moi[0].Tep = "A.pdf";
            var gop = PhongDocBanVe.Gop(ct2.Tam, moi, new HashSet<string> { "A.pdf" });
            Kiem("gop: giu o sua + gia tri AI", gop[0].T + "|" + gop[0].GiaTriAi["T"] + "|" + gop[0].LyDoSua, "2.0|1.6|khách sửa");

            // Chỗ sửa → bài học
            var cs = PhongBaiHoc.CacChoSua(ct2);
            Kiem("cho sua: T 1.6 -> 2.0", cs.Count + "|" + cs[0].O + "|" + cs[0].Ai + "|" + cs[0].Dung, "1|T|1.6|2.0");
            ct2.DaRutBaiHoc.Add(cs[0].ChuKy);
            Kiem("cho sua: da gui thi bo qua", PhongBaiHoc.CacChoSua(ct2).Count, 0);
            Kiem("dau vao bai hoc", PhongBaiHoc.DauVao(cs, "", new[] { "Cũ 1" }).Contains("độ dày T: AI đọc «1.6» → đúng «2.0» | lý do người sửa: khách sửa")
                 && PhongBaiHoc.DauVao(cs, "", new[] { "Cũ 1" }).Contains("- Cũ 1"), true);
            string tep = PhongBaiHoc.ApDung("", new DeXuatBaiHoc { NoiDung = "Bài A" });
            tep = PhongBaiHoc.ApDung(tep, new DeXuatBaiHoc { NoiDung = "Bài B" });
            tep = PhongBaiHoc.ApDung(tep, new DeXuatBaiHoc { NoiDung = "Bài A2", GopVoi = "Bài A" });
            Kiem("ap dung: them + gop thay dong", string.Join("|", PhongBaiHoc.CacBaiHoc(tep)) + "|" + tep.StartsWith("# "), "Bài A2|Bài B|True");
            var bh = PhongBaiHoc.DocPhanHoi("{\"stop_reason\":\"end_turn\",\"content\":[{\"type\":\"text\",\"text\":" +
                OhtaTool.Plugin.Core.JsonLite.Serialize("{\"bai_hoc\":[{\"noi_dung\":\"Khi ghi t= thì…\",\"gop_voi\":\"\",\"ly_do\":\"nhầm\"},{\"noi_dung\":\" \",\"gop_voi\":\"\",\"ly_do\":\"\"}]}") + "}]}");
            Kiem("phan hoi bai hoc: bo dong rong", bh.BaiHoc.Count + "|" + bh.BaiHoc[0].LyDo + "|" + (bh.Loi ?? "null"), "1|nhầm|null");

            // Luật tiết diện + dựng triển khai từ bảng
            var sai = new TamAi { SoBanVe = "S", M = "SPCC", T = "1.6", Q = "1", KieuTd = "be", TdX = "50 b90" };
            var thieu = new TamAi { SoBanVe = "K", M = "SPCC", T = "1.6", Q = "1", KieuTd = "be" };
            var cu = new TamAi { SoBanVe = "C", M = "SPCC", T = "1.6", Q = "1" };
            var luat = PhongKiemTra.Luat(new[] { sai, thieu, cu }, new List<PhongKiemTra.DongTv>(), "");
            Kiem("luat: td sai do / thieu td vang / ban cu khong bao",
                 luat.Count(v => v.Ma.EndsWith("|td_sai_X") && v.Muc == VanDe.Do) + "|" + luat.Count(v => v.Ma.EndsWith("|thieu_td")) + "|" + luat.Count(v => v.Ma.Contains("|c||") && v.Ma.Contains("td")), "1|1|0");
            Kiem("co tiet dien", PhongTrienKhai.CoTietDien(t2) + "|" + PhongTrienKhai.CoTietDien(cu) + "|" + PhongTrienKhai.CoTietDien(new TamAi { KieuTd = "lam_tay" }), "True|False|True");
            TamTrienKhai tk = PhongTrienKhai.TuTam(t2);
            Kiem("tu tam: tiet dien + bien dang", tk.Kieu + "|" + tk.X.Doan.Count + "|" + tk.X.Uon.Count + "|" + tk.Y.Doan[0] + "|" + tk.BienDang, "be|2|1|200|be");
            var chep = new TamAi();
            PhongTrienKhai.ChepVaoTam(tk, chep);
            Kiem("chep td vao bang (don cu)", chep.TdX + "|" + chep.KieuTd, "50 b90 30|be");

            // Bản chuẩn xưởng (12/11): ケガキ đọc + gửi ve.py, N= bango, T có phần thập phân
            var tamK = PhongDocBanVe.DocPhanHoi("{\"stop_reason\":\"end_turn\",\"content\":[{\"type\":\"text\",\"text\":" + OhtaTool.Plugin.Core.JsonLite.Serialize(
                text.Replace("\"mo_ta_bien_dang\":\"\"}", "\"mo_ta_bien_dang\":\"\",\"ke_gaki\":[{\"x\":120,\"y\":27.5},{\"x\":180}]}")) + "}]}").Tam[0];
            Kiem("ke gaki: doc + bo dau thieu y", PhongTrienKhai.KeGaki(tamK).Count + "|" + tamK.TomTatKeGaki + "|" + PhongDocBanVe.Doc(PhongDocBanVe.Ghi(new ChiTiet { Tam = { tamK } })).Tam[0].KeGakiJson.Contains("120"), "1|2 ケガキ|True");
            var tamF = new TamAi { M = "SS400", T = "12", Q = "2", Z = "SS400" };
            Kiem("fname: N= bango, T=12.0", string.Join(",", PhongTrienKhai.DongFname(tamF, new TamTrienKhai(), "test-1")), "N=test-1,M=SS400,T=12.0,Q=2,Z=SS400");
            tamF.T = "1.6"; tamF.SoBanVe = "A-1";
            Kiem("fname: so BV truoc bango, T=1.6 giu", string.Join(",", PhongTrienKhai.DongFname(tamF, new TamTrienKhai(), "x-1").Take(3)), "N=A-1,M=SS400,T=1.6");
            tamK.Moi = true;
            var tkK = PhongTrienKhai.TuTam(tamK);
            tkK.XPhang = 80; tkK.YPhang = 200;
            string viec = PhongTrienKhai.ViecVe("test", new List<(TamAi, TamTrienKhai)> { (tamK, tkK) }, "m.dxf", "ra", new[] { "tv" });
            Kiem("viec ve: ke_gaki + N=so BV", System.Text.RegularExpressions.Regex.Replace(viec, @"\s", "").Contains("\"ke_gaki\":[[120,27.5]]") + "|" + viec.Contains("N=A-1"), "True|True");
        }

        private static void TestAiCli()
        {
            Console.WriteLine("-- Nguon AI qua CLI --");
            string body = PhongDocBanVe.Body("claude-opus-5-5", "LOI DAN", "QUY CHUAN", "a.pdf", "UERG", null);
            var yc = AiCli.TachBody(body);
            Kiem("tach body API: loi dan + quy chuan + phan nguoi dung, pdf, schema, model, effort",
                 yc.HuongDan.StartsWith("LOI DAN") + "|" + yc.HuongDan.Contains("QUY CHUAN") + "|" + yc.HuongDan.Contains("Tệp: a.pdf") + "|" + yc.PdfBase64 + "|" + yc.Schema.Contains("\"tam\"") + "|" + yc.Model + "|" + yc.Effort,
                 "True|True|True|UERG|True|claude-opus-5-5|high");
            var cc = AiCli.ThamSo(AiCli.ClaudeCode, yc, "D:/tm");
            var ag = AiCli.ThamSo(AiCli.Antigravity, new AiCli.YeuCau { Schema = "{}", Model = "claude-opus-5-5" }, "D:/tm");
            Kiem("tham so: claude chi Read + model; agy add-dir, khong model; loi nhan nhac pdf",
                 cc.Contains("Read") + "|" + cc.Contains("--model") + "|" + cc.Contains("--add-dir") + "|" + ag.Contains("--add-dir") + "|" + ag.Contains("--model") + "|" + cc[1].Contains(AiCli.TepPdf) + "|" + ag[1].Contains(AiCli.TepPdf),
                 "True|True|False|True|False|True|False");
            Kiem("thoat tham so dong lenh Windows", AiCli.ThoatThamSo("a b") + " " + AiCli.ThoatThamSo("{\"x\":1}") + " " + AiCli.ThoatThamSo("C:\\a\\") + " " + AiCli.ThoatThamSo("x\\\"y"),
                 "\"a b\" \"{\\\"x\\\":1}\" \"C:\\a\\\\\" \"x\\\\\\\"y\"");
            string agyOk = "{\"conversation_id\":\"x\",\"status\":\"SUCCESS\",\"response\":\"{\\\"so\\\":5}\\n\",\"structured_output\":{\"chu\":\"xin chào\",\"so\":5},\"usage\":{\"input_tokens\":12403,\"output_tokens\":69}}";
            string ccLoi = "{\"type\":\"result\",\"is_error\":true,\"api_error_status\":429,\"result\":\"You've hit your monthly spend limit\",\"usage\":{\"input_tokens\":0}}";
            string ccRao = "dong log\n{\"type\":\"result\",\"is_error\":false,\"result\":\"```json\\n{\\\"tam\\\":[]}\\n```\"}";
            var p1 = PhongDocBanVe.DocPhanHoi(AiCli.DoiKetQua(agyOk));
            var p2 = PhongDocBanVe.DocPhanHoi(AiCli.DoiKetQua(ccLoi));
            var p3 = PhongDocBanVe.DocPhanHoi(AiCli.DoiKetQua(ccRao));
            var p4 = PhongDocBanVe.DocPhanHoi(AiCli.DoiKetQua("Error: not logged in"));
            Kiem("doi ket qua CLI: agy structured_output / claude loi 429 / rao ```json + dong log / khong JSON",
                 AiCli.DoiKetQua(agyOk).Contains("xin chào") + "|" + p1.Vao + "|" + p2.Loi + "|" + (p3.Loi ?? "null") + "|" + p3.Tam.Count + "|" + p4.Loi,
                 "True|0|You've hit your monthly spend limit|null|0|CLI không trả JSON: Error: not logged in");
        }

        private static void TestPhongTrienKhai()
        {
            Console.WriteLine("-- Phong 4 trien khai --");
            Kiem("delta tu chu TraV: 90 / sima / goc nhon / khong co",
                 PhongTrienKhai.DeltaTuChu("V=32-4(90°)") + "|" + PhongTrienKhai.DeltaTuChu("シマ目外 V32引代 4.1 (90°)") + "|" + PhongTrienKhai.DeltaTuChu("V12x-1.95(60°)") + "|" + (PhongTrienKhai.DeltaTuChu("abc") == null),
                 "4|4.1|1.95|True");

            // TraV giả: SS t4.5 (QC: V32 δ4)
            List<UngVienV> TraV(double goc) => goc == 90
                ? new List<UngVienV> { new UngVienV { V = "V25", Delta = 3.7 }, new UngVienV { V = "V32", Delta = 4 }, new UngVienV { V = "V40", Delta = 4.2 } }
                : new List<UngVienV>();
            double? Lmin(string v) => v == "V25" ? 15 : v == "V32" ? 20 : v == "V40" ? 25 : (double?)null;
            var tam = new TamAi { SoBanVe = "A-1", M = "SS400", T = "4.5", Q = "3", LoJson = "[{\"d\":5,\"taro\":\"M6\",\"sara\":false}]" };
            TietDien L(double a, double b, string kieu = "be", double goc = 90, double r = 0) =>
                new TietDien { Doan = { a, b }, Uon = { new Uon { Kieu = kieu, Goc = goc, R = r } } };
            TietDien Phang(double a) => new TietDien { Doan = { a } };

            var tk = new TamTrienKhai { Kieu = "be", X = L(50, 100), Y = Phang(200) };
            PhongTrienKhai.Tinh(tk, tam, TraV, 4.2, Lmin);
            Kiem("be L 50+100 tu chon V25 (Lmin 15 <= 50): X = 150 - 2x3.7", tk.VChon + "|" + tk.XPhang + "|" + tk.YPhang + "|" + tk.KyHieu, "V25|142.6|200|*");
            var tk2 = new TamTrienKhai { Kieu = "be", X = L(50, 100), Y = Phang(200), VChon = "V32" };
            PhongTrienKhai.Tinh(tk2, tam, TraV, 4.2, Lmin);
            Kiem("be L nguoi chon V32 (QC δ4): X = 142", tk2.VChon + "|" + tk2.XPhang + "|" + tk2.ChiTiet[0].Contains("V32 δ4 → −8"), "V32|142|True");
            Kiem("duong be tren tam phang: L V32 -> 46 (= 50 - δ4), doan 46 + 96", string.Join(",", tk2.BeX) + "|" + string.Join("+", tk2.DoanPhangX) + "|" + tk2.BeY.Count, "46|46+96|0");
            var hop = new TamTrienKhai { Kieu = "be", VChon = "V32",
                X = new TietDien { Doan = { 20, 100, 20 }, Uon = { new Uon(), new Uon() } }, Y = new TietDien { Doan = { 20, 60, 20 }, Uon = { new Uon(), new Uon() } } };
            PhongTrienKhai.Tinh(hop, tam, TraV, 4.2, Lmin);
            Kiem("hop: X x Y, duong be 2 huong, doan phang dau / cuoi 16 (cat goc)",
                 hop.XPhang + "x" + hop.YPhang + "|" + string.Join(",", hop.BeX) + "|" + string.Join(",", hop.BeY) + "|" + string.Join("+", hop.DoanPhangX), "124x84|16,108|16,68|16+92+16");

            var tk3 = new TamTrienKhai { Kieu = "be", X = L(18, 100), Y = Phang(200) };
            PhongTrienKhai.Tinh(tk3, tam, TraV, 4.2, Lmin);
            Kiem("be canh 18: bo V40 (Lmin 25) va giu V25 (15)", tk3.VChon, "V25");
            var tk4 = new TamTrienKhai { Kieu = "be", X = L(50, 100, "an_1_lan", 90, 10), Y = Phang(200) };
            PhongTrienKhai.Tinh(tk4, tam, TraV, 4.2, Lmin);
            Kiem("an 1 lan R10 90° T4.5 T thuc 4.2: 150 - 2x14.5 + (10+1.89)xπ/2; duong be giua cung", tk4.XPhang + "|" + string.Join(",", tk4.BeX), "139.7|44.84");
            var tk5 = new TamTrienKhai { Kieu = "be", X = new TietDien { Doan = { 0, 0 }, Uon = { new Uon { Kieu = "cuon", R = 100, Cung = 360 } } }, Y = Phang(500) };
            PhongTrienKhai.Tinh(tk5, new TamAi { T = "3.2", M = "SS400" }, TraV, 3, Lmin);
            Kiem("cuon tron R100 T thuc 3: 2π(100+1.5) + ky hieu r", tk5.XPhang + "|" + tk5.KyHieu, "637.7|r");
            var tk6 = new TamTrienKhai { Kieu = "be", X = L(50, 100, "pitchi"), Y = Phang(200) };
            PhongTrienKhai.Tinh(tk6, tam, TraV, 4.2, Lmin);
            Kiem("pitchi -> lam tay, khong tinh", tk6.LamTay + "|" + (tk6.XPhang == null), "True|True");
            var tk7 = new TamTrienKhai { Kieu = "be", X = new TietDien { Doan = { 50, 100, 30 }, Uon = { new Uon() } }, Y = Phang(200) };
            PhongTrienKhai.Tinh(tk7, tam, TraV, 4.2, Lmin);
            Kiem("doan / uon lech -> canh bao, X null", (tk7.XPhang == null) + "|" + tk7.CanhBao.Any(c => c.Contains("số đoạn")), "True|True");
            var tk8 = new TamTrienKhai { Kieu = "be", X = L(50, 100, "be", 120), Y = Phang(200) };
            PhongTrienKhai.Tinh(tk8, tam, TraV, 4.2, Lmin);
            Kiem("goc TV khong co -> canh bao tra tay", (tk8.XPhang == null) + "|" + tk8.CanhBao.Any(c => c.Contains("bảng TV không có góc 120")), "True|True");
            // loại biên dạng mặc định + F-name + tệp việc vẽ ezdxf
            var phangBd = new TamTrienKhai { X = Phang(300), Y = Phang(150) };
            PhongTrienKhai.Tinh(phangBd, tam, TraV, 4.2, Lmin);
            var cuonBd = new TamTrienKhai { X = new TietDien { Doan = { 0, 0 }, Uon = { new Uon { Kieu = "cuon", R = 100, Cung = 360 } } }, Y = Phang(500) };
            PhongTrienKhai.Tinh(cuonBd, tam, TraV, 4.2, Lmin);
            Kiem("bien dang mac dinh: be / hop / phang / cuon / lam tay; AI chon thi theo AI",
                 PhongTrienKhai.BienDangVe(tk2) + "|" + PhongTrienKhai.BienDangVe(hop) + "|" + PhongTrienKhai.BienDangVe(phangBd) + "|" + PhongTrienKhai.BienDangVe(cuonBd) + "|" +
                 PhongTrienKhai.BienDangVe(tk6) + "|" + PhongTrienKhai.BienDangVe(new TamTrienKhai { BienDang = "chu_u" }), "be|hop|phang|cuon||chu_u");
            Kiem("F-name: N M T Q + dong V cua TraV (khong lap) + an 1 lan + Z",
                 string.Join(";", PhongTrienKhai.DongFname(new TamAi { SoBanVe = "A-1", M = "SS400", T = "4.5", Q = "3", Z = "SS400" }, hop)) + "#" + string.Join(";", tk4.DongV),
                 "N=A-1;M=SS400;T=4.5;Q=3;V=32-4(90°);Z=SS400#内R10-発押し90°");
            string viec = PhongTrienKhai.ViecVe("130672", new List<(TamAi, TamTrienKhai)> { (tam, tk2), (tam, tk6), (tam, hop) }, "m.dxf", "ra", new[] { "tv1", "tv2" });
            var vj = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(viec);
            var vt = (List<object>)vj.Get("tam");
            var v0 = (OhtaTool.Plugin.Core.JsonObject)vt[0];
            var v1 = (OhtaTool.Plugin.Core.JsonObject)vt[1];
            Kiem("viec ve: bo tam lam tay, bango theo thu tu (3 cho tam thu 3), so + F-name + thu vien",
                 vt.Count + "|" + v0.GetString("bango") + "|" + v1.GetString("bango") + "|" + v1.GetString("bien_dang") + "|" + ((OhtaTool.Plugin.Core.JsonNumber)v0.Get("x")).Raw + "|" +
                 ((List<object>)v0.Get("fname")).Count + "|" + ((List<object>)vj.Get("thu_vien")).Count + "|" + vj.GetString("moi_truong"), "2|130672-1|130672-3|hop|142|6|2|m.dxf");

            // mẫu mới AI đề xuất: kiểm an toàn trước khi chạy
            const string maTot = "\"\"\"x\"\"\"\nimport math\nfrom moi_truong import LE_DIM\n\nMO_TA = \"Chu U\"\n\n\ndef ve(nen, t):\n    nen.chu_nhat(0, 0, t[\"x\"], t[\"y\"])\n";
            string[] daCo = { "be", "hop", "phang", "cuon" };
            Kiem("kiem mau: tot / ten sai / trung / thieu ve / import os / open / os. / subprocess",
                 (PhongTrienKhai.KiemMau("chu_u", maTot, daCo) ?? "ok") + "|" + (PhongTrienKhai.KiemMau("Chu U", maTot, daCo) != null) + "|" + (PhongTrienKhai.KiemMau("hop", maTot, daCo) != null) + "|" +
                 (PhongTrienKhai.KiemMau("chu_u", "MO_TA = 1", daCo) != null) + "|" + PhongTrienKhai.KiemMau("chu_u", maTot + "import os\n", daCo) + "|" +
                 (PhongTrienKhai.KiemMau("chu_u", maTot + "    f = open('a')\n", daCo) != null) + "|" + (PhongTrienKhai.KiemMau("chu_u", maTot.Replace("import math", "import math, subprocess"), daCo) != null) + "|" +
                 (PhongTrienKhai.KiemMau("chu_u", maTot + "    x = __import__('os')\n", daCo) != null),
                 "ok|True|True|True|Mã import 'os' — chỉ được import math, moi_truong.|True|True|True");
            string tl = OhtaTool.Plugin.Core.JsonLite.Serialize("{\"ten\":\"chu_u\",\"mo_ta\":\"Chu U\",\"ma\":" + OhtaTool.Plugin.Core.JsonLite.Serialize(maTot) + "}");
            var mm = PhongTrienKhai.DocMauMoi("{\"stop_reason\":\"end_turn\",\"usage\":{\"input_tokens\":9,\"output_tokens\":4},\"content\":[{\"type\":\"text\",\"text\":" + tl + "}]}", daCo);
            Kiem("doc mau moi tu phan hoi AI", mm.Ten + "|" + mm.MoTa + "|" + (mm.Loi ?? "null") + "|" + mm.Ma.Contains("def ve(nen, t)") + "|" + mm.Vao, "chu_u|Chu U|null|True|9");
            string bodyMau = PhongTrienKhai.BodyMauMoi("claude-opus-5-5", null, null, "dau vao");
            Kiem("body mau moi: schema ten/mo_ta/ma, khong khoi quy chuan", bodyMau.Contains("\"ma\"") + "|" + bodyMau.Contains("cache_control"), "True|False");
            var phang = new TamTrienKhai { Kieu = "phang", X = Phang(300), Y = Phang(150) };
            PhongTrienKhai.Tinh(phang, new TamAi { T = "3.2", M = "SS400", Q = "1" }, TraV, 3, Lmin);
            Kiem("tam phang: X x Y, ky hieu rong, CSV", phang.XPhang + "x" + phang.YPhang + "|" + phang.KyHieu + "|" + PhongTrienKhai.DongCsv("130672", 2, new TamAi { T = "3.2", M = "SS400", Q = "1" }, phang),
                 "300x150||130672-2,1,SS400,3.2,,300,150");
            string tt = PhongTrienKhai.TomTat("130672", new List<(TamAi, TamTrienKhai)> { (tam, tk2), (tam, tk6) });
            Kiem("tom tat gui chuoi don", tt.Contains("1. A-1 — SS400 t4.5 ×3 — 142 × 200 — V32 [*]") + "|" + tt.Contains("2. A-1 — SS400 t4.5 ×3 — LÀM TAY"), "True|True");

            // phản hồi AI + ghép + JSON
            string text = "{\"tam\":[{\"so_ban_ve\":\"B-2\",\"ten\":\"\",\"kieu\":\"be\",\"x\":{\"doan\":[15,210],\"uon\":[{\"kieu\":\"be\",\"goc\":90,\"r_trong\":0,\"cung\":0}]},\"y\":{\"doan\":[145],\"uon\":[]},\"ghi_chu\":\"bẻ V\"}," +
                          "{\"so_ban_ve\":\"A-1\",\"ten\":\"x\",\"kieu\":\"phang\",\"x\":{\"doan\":[300],\"uon\":[]},\"y\":{\"doan\":[150],\"uon\":[]},\"ghi_chu\":\"\"}]}";
            var ph = PhongTrienKhai.DocPhanHoi("{\"stop_reason\":\"end_turn\",\"usage\":{\"input_tokens\":7,\"output_tokens\":3},\"content\":[{\"type\":\"text\",\"text\":" + OhtaTool.Plugin.Core.JsonLite.Serialize(text) + "}]}");
            var dung = new HashSet<TamTrienKhai>();
            var g1 = PhongTrienKhai.Ghep(new TamAi { SoBanVe = "A-1" }, ph.Tam, dung);
            var g2 = PhongTrienKhai.Ghep(new TamAi { SoBanVe = "Z-9", Ten = "khac" }, ph.Tam, dung);
            Kiem("phan hoi AI: doc 2 tam, ghep theo so BV roi theo thu tu, token",
                 ph.Tam.Count + "|" + g1.Kieu + "|" + g2.X.Doan[1] + "|" + g2.Y.Doan[0] + "|" + ph.Vao + "|" + (ph.Loi ?? "null"), "2|phang|210|145|7|null");
            var kq = new KetQuaTrienKhai { TuXacNhan = "t", Tien = 0.07m };
            g2.Khoa = "k"; g2.VChon = "V12"; kq.Tam.Add(g2);
            var kq2 = PhongTrienKhai.Doc(PhongTrienKhai.Ghi(kq));
            Kiem("trien_khai.json ghi roi doc", kq2.TuXacNhan + "|" + kq2.Tien + "|" + kq2.Tam[0].Khoa + "|" + kq2.Tam[0].VChon + "|" + string.Join("+", kq2.Tam[0].X.Doan) + "|" + kq2.Tam[0].X.Uon[0].Goc + "|" + kq2.Tam[0].GhiChu,
                 "t|0.07|k|V12|15+210|90|bẻ V");
            Kiem("trien_khai.json hong -> null", PhongTrienKhai.Doc("[]") == null, true);
            string dv = PhongTrienKhai.DauVao("a.pdf", new[] { tam }, new[] { new VanDe { Tam = "A-1", NoiDung = "R?", TraLoi = "R10" }, new VanDe { NoiDung = "x" } });
            Kiem("dau vao AI phong 4: bang tam + cau tra loi phong 3", dv.Contains("so_ban_ve=A-1") + "|" + dv.Contains("trả lời: R10") + "|" + dv.Contains("| x |"), "True|True|False");
        }

        private static void TestPhongKiemTra()
        {
            Console.WriteLine("-- Phong 3 kiem tra --");
            const string tvCsv = "﻿Mat,T,T_thuc,V,HikiShiro_90,Denta90,Ri,Saichou,L_saitei,Z_saitei\n" +
                                 "SS,4.5,4.2,40,4.2,4.2,5.8,6000,25,32\nSS,4.5,4.2,25,3.7,3.7,3.3,5000,15,19.5\nSS,4.5,4.2,32,4,4,4.3,6000,20,32\n" +
                                 "SS SIMA Ngoài,4.5,5.9,32,4,4,4,6000,20,32\nSS,3.2,3,16,2.6,2.6,2,5500,10,11.5\nhong\n";
            const string catalog = "﻿M,Loai,Z,T_list\nSS400,SS,SS400,4.5\nSUS304 2B,SUS,SUS-N2,1.0\n";
            var tv = PhongKiemTra.DocTv(tvCsv);
            Kiem("TV: doc dong, bo dong hong; Lmin SS 4.5 theo V tang dan", tv.Count + "|" + string.Join(",", PhongKiemTra.Lmin(tv, "SS", 4.5).Select(r => r.V + "/" + r.Lmin)), "5|25/15,32/20,40/25");
            Kiem("loai TV: catalog / doan ten / sima / khong ro",
                 PhongKiemTra.LoaiTv("SS400", "", catalog, tv) + "|" + PhongKiemTra.LoaiTv("SUS304 No.1", "", catalog, tv) + "|" + PhongKiemTra.LoaiTv("A5052", "", catalog, tv) + "|" +
                 PhongKiemTra.LoaiTv("SS400", "縞板", catalog, tv) + "|" + (PhongKiemTra.LoaiTv("C2801P", "", catalog, tv) ?? "null"), "SS|SUS|AL|SS SIMA Ngoài|null");
            Kiem("taro: SS M6 t4.5 di dong / t12 xam / M10 t16 co dinh / SUS M6 t8 co dinh / SUS M3 t9 xam / M6x1.0 / AL ngoai bang",
                 "" + PhongKiemTra.Taro("SS", "M6", 4.5) + PhongKiemTra.Taro("SS", "M6", 12) + PhongKiemTra.Taro("SS", "M10", 16) + PhongKiemTra.Taro("SUS", "M6", 8) +
                 PhongKiemTra.Taro("SUS", "M3", 9) + PhongKiemTra.Taro("SS", "M6x1.0", 3.2) + (PhongKiemTra.Taro("AL", "M6", 3) == null), "xgccgxTrue");
            Kiem("so trong chu", PhongKiemTra.So("t2.3") + "|" + PhongKiemTra.So("10枚") + "|" + (PhongKiemTra.So("abc") == null), "2.3|10|True");

            var tam = new TamAi
            {
                Tep = "a.pdf", SoBanVe = "A-1", M = "SS400", T = "4.5", Q = "2", KichThuoc = "300x200",
                CanhJson = "[{\"dai_ngoai\":[18,100],\"goc\":90,\"r_trong\":0,\"kieu\":\"be\",\"huong\":\"V\"},{\"dai_ngoai\":[12,80],\"goc\":90,\"r_trong\":10,\"kieu\":\"be\",\"huong\":\"V\"},{\"dai_ngoai\":[],\"goc\":90,\"r_trong\":12,\"kieu\":\"be\",\"huong\":\"\"}]",
                LoJson = "[{\"d\":10,\"so_luong\":2,\"taro\":\"\",\"sara\":false,\"ghi_chu\":\"\",\"cach_duong_be\":10},{\"d\":6,\"so_luong\":1,\"taro\":\"\",\"sara\":false,\"ghi_chu\":\"\",\"cach_duong_be\":22}," +
                         "{\"d\":5,\"so_luong\":1,\"taro\":\"M6\",\"sara\":false,\"ghi_chu\":\"\",\"cach_duong_be\":-1},{\"d\":3,\"so_luong\":1,\"taro\":\"\",\"sara\":false,\"ghi_chu\":\"\",\"cach_duong_be\":-1}]",
            };
            tam.KhongChac.Add("M"); tam.KhongChac.Add("Q"); tam.DaSua.Add("Q");
            var cu = new TamAi { Tep = "b.pdf", SoBanVe = "B-1", M = "SS400", T = "3.2", Q = "", Moi = false };
            var cuon = new TamAi { Tep = "b.pdf", SoBanVe = "C-1", M = "SS400", T = "3.2", Q = "", CanhJson = "[{\"dai_ngoai\":[],\"goc\":180,\"r_trong\":50,\"kieu\":\"cuon\",\"huong\":\"\"}]" };
            var vd = PhongKiemTra.Luat(new[] { tam, cu, cuon }, tv, catalog);
            string Tim(string ma) { var v = vd.FirstOrDefault(x => x.Ma.EndsWith("|" + ma)); return v == null ? "-" : v.Muc + "/" + v.Nhom + (v.LienLac ? "/LL" : ""); }
            Kiem("luat: khong chac (bo o da sua) / canh 18 giua Lmin / canh 12 < Lmin nho nhat",
                 Tim("khong_chac") + "|" + vd.First(x => x.Ma.EndsWith("|khong_chac")).NoiDung.Contains("ô: M —") + "|" + Tim("canh_0") + "|" + Tim("canh_1"),
                 "vang/noi_bo|True|vang/tu_xu_ly|do/khach/LL");
            Kiem("luat: R10 an 1 lan / R12 pitchi / lo 10 < Lmin 15 / lo 22 giua / taro M6 t4.5 khong bao / lo 3 < t",
                 Tim("r_1") + "|" + Tim("r_2") + "|" + Tim("lo_be_0") + "|" + Tim("lo_be_1") + "|" + Tim("taro_2") + "|" + Tim("lo_nho_3"),
                 "info/tu_xu_ly|vang/tu_xu_ly|do/khach/LL|vang/tu_xu_ly|-|vang/noi_bo");
            Kiem("luat: tam khong moi bo qua / cuon R ngoai 53.2 < 100 / thieu Q / SS t3.2",
                 vd.Any(x => x.Tam.StartsWith("B-1")) + "|" + vd.Any(x => x.Tam.StartsWith("C-1") && x.MucQc.StartsWith("C-2") && x.NoiDung.Contains("53.2 < 100")) + "|" +
                 vd.Any(x => x.Tam.StartsWith("C-1") && x.Ma.EndsWith("|thieu_q") && x.Muc == VanDe.Do) + "|" + vd.Any(x => x.Tam.StartsWith("C-1") && x.Ma.EndsWith("|ss32")),
                 "False|True|True|True");
            var tamCu = new TamAi { Tep = "c.pdf", SoBanVe = "D-1", M = "SS400", T = "4.5", Q = "1", CanhJson = tam.CanhJson, LoJson = "[{\"d\":10,\"so_luong\":1,\"taro\":\"\",\"sara\":false,\"ghi_chu\":\"\"}]" };
            Kiem("luat: bang doc ban cu (lo chua co cach_duong_be) -> nhac doc lai; ban moi khong nhac",
                 PhongKiemTra.Luat(new[] { tamCu }, tv, catalog).Any(x => x.Ma.EndsWith("|thieu_cach")) + "|" + vd.Any(x => x.Ma.EndsWith("|thieu_cach")), "True|False");
            Kiem("luat: khong co bang TV -> 1 canh bao, bo Lmin", PhongKiemTra.Luat(new[] { tam }, new List<PhongKiemTra.DongTv>(), catalog).Count(x => x.Ma.Contains("canh_") || x.Ma.Contains("khong_bang_tv")), 1);

            // gộp: giữ trạng thái theo mã
            var lan1 = PhongKiemTra.Luat(new[] { tam }, tv, catalog);
            lan1.First(x => x.Ma.EndsWith("|canh_1")).TrangThai = VanDe.Xong;
            lan1.First(x => x.Ma.EndsWith("|canh_1")).DaGui = true;
            lan1.First(x => x.Ma.EndsWith("|canh_1")).TraLoi = "Khách đồng ý thêm phần thừa 5 mm";
            var lan2 = PhongKiemTra.Gop(lan1, PhongKiemTra.Luat(new[] { tam }, tv, catalog));
            Kiem("gop: giu trang thai + da gui + tra loi", lan2.First(x => x.Ma.EndsWith("|canh_1")).TrangThai + "|" + lan2.First(x => x.Ma.EndsWith("|canh_1")).DaGui + "|" + lan2.Count(x => x.ConMo) + "|" + lan2.First(x => x.Ma.EndsWith("|canh_1")).TraLoi,
                 "xong|True|" + (lan2.Count - 1) + "|Khách đồng ý thêm phần thừa 5 mm");
            string dv = PhongKiemTra.DauVao("a.pdf", "", new[] { tam }, new VanDe[0], lan2);
            Kiem("dau vao AI: gui cau tra loi da co, bo van de chua tra loi",
                 dv.Contains("KHÔNG hỏi lại") + "|" + dv.Contains("trả lời: Khách đồng ý thêm phần thừa 5 mm") + "|" + PhongKiemTra.DauVao("a.pdf", "", new[] { tam }, new VanDe[0], lan1.Where(x => x.TraLoi.Length == 0)).Contains("KHÔNG hỏi lại"),
                 "True|True|False");

            // bản nháp
            var khach = new List<VanDe>
            {
                new VanDe { Nhom = VanDe.Khach, Tam = "A-1", NoiDung = "thiếu R", CauHoiJp = "Rをご指示ください。" },
                new VanDe { Nhom = VanDe.Khach, Tam = "A-1", NoiDung = "rạch slit", CauHoiJp = "スリットを入れます。", LienLac = true },
                new VanDe { Nhom = VanDe.Khach, NoiDung = "đã xong", TrangThai = VanDe.Xong },
                new VanDe { Nhom = VanDe.NoiBo, Tam = "A-1", NoiDung = "xác nhận máy" },
            };
            Kiem("ban nhap お問い合わせ / 連絡します / noi bo",
                 PhongKiemTra.BanNhap("130672", khach, false) + "#" + PhongKiemTra.BanNhap("130672", khach, true).Split('\n')[1] + "#" + PhongKiemTra.CauHoiNoiBo("130672", khach).Split('\n')[1] + "#" + PhongKiemTra.BanNhap("x", new List<VanDe>(), false),
                 "【お問い合わせ】130672\n1. [A-1] thiếu R#1. [A-1] rạch slit#1. 🟡 [A-1] xác nhận máy#");

            // JSON + phản hồi AI
            var kt = new KiemTra { TuXacNhan = "2026-11-11 17:47", Tien = 0.05m, VanDe = lan2 };
            var kt2 = PhongKiemTra.Doc(PhongKiemTra.Ghi(kt));
            Kiem("kiem_tra.json ghi roi doc", kt2.TuXacNhan + "|" + kt2.Tien + "|" + kt2.VanDe.Count + "|" + kt2.VanDe.First(x => x.Ma.EndsWith("|canh_1")).TrangThai + "|" + kt2.DoConMo + "|" + kt2.VanDe.First(x => x.Ma.EndsWith("|canh_1")).TraLoi,
                 "2026-11-11 17:47|0.05|" + lan2.Count + "|xong|" + lan2.Count(x => x.Muc == VanDe.Do && x.ConMo) + "|Khách đồng ý thêm phần thừa 5 mm");
            Kiem("kiem_tra.json hong -> null", PhongKiemTra.Doc("[]") == null, true);
            string text = "{\"van_de\":[{\"tam\":\"A-1\",\"muc\":\"do\",\"nhom\":\"khach\",\"muc_qc\":\"B-2\",\"noi_dung\":\"Thiếu kích thước lỗ\",\"cau_hoi_jp\":\"穴位置をご指示ください。\",\"lien_lac\":false}," +
                          "{\"tam\":\"\",\"muc\":\"la\",\"nhom\":\"la\",\"muc_qc\":\"\",\"noi_dung\":\"x\",\"cau_hoi_jp\":\"\",\"lien_lac\":false},{\"tam\":\"\",\"muc\":\"info\",\"nhom\":\"tu_xu_ly\",\"muc_qc\":\"\",\"noi_dung\":\" \",\"cau_hoi_jp\":\"\",\"lien_lac\":false}]," +
                          "\"dich\":[{\"ma\":\"luat|a|canh_1\",\"cau_hoi_jp\":\"寸法変更可能でしょうか。\"}]}";
            string phanHoi = "{\"stop_reason\":\"end_turn\",\"usage\":{\"input_tokens\":100,\"output_tokens\":20},\"content\":[{\"type\":\"text\",\"text\":" + OhtaTool.Plugin.Core.JsonLite.Serialize(text) + "}]}";
            var ph = PhongKiemTra.DocPhanHoi(phanHoi, "a.pdf");
            Kiem("phan hoi AI: van de + muc/nhom la -> mac dinh + bo noi dung rong + khong lay JP + token",
                 ph.VanDe.Count + "|" + ph.VanDe[0].Muc + "/" + ph.VanDe[0].Nhom + "/" + ph.VanDe[0].Nguon + "|" + ph.VanDe[1].Muc + "/" + ph.VanDe[1].Nhom + "|" + ph.VanDe[0].CauHoiJp.Length + "|" + ph.Vao + "|" + (ph.Loi ?? "null"),
                 "2|do/khach/ai|vang/noi_bo|0|100|null");
            Kiem("phan hoi AI: loi key", PhongKiemTra.DocPhanHoi("{\"type\":\"error\",\"error\":{\"type\":\"authentication_error\",\"message\":\"x\"}}", "a").Loi.StartsWith("API key"), true);
            Kiem("ma AI on dinh (bo dau cau, hoa thuong)", PhongKiemTra.MaAi("A.pdf", "A-1", "Thiếu R!") == PhongKiemTra.MaAi("a.pdf", "a-1", "thiếu r"), true);
            string body = PhongKiemTra.Body("claude-opus-5-5", "QC3", "UERG", "dau vao", null);
            Kiem("body phong 3: schema + cache QC + pdf + fallback",
                 body.Contains("\"van_de\"") + "|" + body.Contains("\"cache_control\"") + "|" + body.Contains("\"application/pdf\"") + "|" + ((OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(body)).GetString("fallbacks"), "True|True|True|default");
        }

        private static void TestPhongAi()
        {
            Console.WriteLine("-- Phong AI (tiep nhan / doc ban ve) --");
            Kiem("loai tep: dxf / step / anh / la", PhongTiepNhan.XepLoai("A.DXF") + "|" + PhongTiepNhan.XepLoai("x.stp") + "|" + PhongTiepNhan.XepLoai("a.JPG") + "|" + PhongTiepNhan.XepLoai("a.zip"), "ban_ve|3d|don_hang|khac");
            Kiem("loai tep: pdf theo ten", PhongTiepNhan.XepLoai("注文書_081106.pdf") + "|" + PhongTiepNhan.XepLoai("081106-15.pdf") + "|" + PhongTiepNhan.XepLoai("Đơn Hàng 1.pdf"), "don_hang|ban_ve|don_hang");
            KeyValuePair<string, string> C2(string k, string v) => new KeyValuePair<string, string>(k, v);
            Kiem("loai don: repeat / nua / pitchi / moi",
                 PhongTiepNhan.DoanLoaiDon(new[] { C2("Loại đơn", "Repeat") }) + "|" + PhongTiepNhan.DoanLoaiDon(new[] { C2("Loại đơn", "Repeat + mới") }) + "|" +
                 PhongTiepNhan.DoanLoaiDon(new[] { C2("加工", "ピッチ曲げ") }) + "|" + PhongTiepNhan.DoanLoaiDon(new[] { C2("Loại đơn", "Mới") }), "repeat|nua_repeat|can_lien_lac|moi");
            var ctDs = new[] { "# khoa|ten", "abc co,ABC|ABC Co", "xyz|XYZ" };
            Kiem("cong ty: khop khoa khong dau / khong khop",
                 PhongTiepNhan.DoanCongTy(new[] { C2("Công ty", "Công ty ABC Cơ khí") }, ctDs) + "|" + PhongTiepNhan.DoanCongTy(new[] { C2("Công ty", "khác") }, ctDs), "ABC Co|");
            Kiem("dung sau tiep nhan chi khi repeat", PhongTiepNhan.DungSauTiepNhan("repeat") + "|" + PhongTiepNhan.DungSauTiepNhan("nua_repeat"), "True|False");
            var tepB = new List<TepDon> { new TepDon { Ten = "b.pdf", Loai = "ban_ve" }, new TepDon { Ten = "a.pdf", Loai = "ban_ve" }, new TepDon { Ten = "z.pdf", Loai = "don_hang" }, new TepDon { Ten = "c.dxf", Loai = "ban_ve" }, new TepDon { Ten = "k.pdf", Loai = "khac" } };
            Kiem("thu tu bango: don hang truoc, chi pdf", string.Join(",", PhongTiepNhan.ThuTuBango(tepB).Select(t => t.Ten)), "z.pdf,a.pdf,b.pdf");
            Kiem("ten bango", PhongTiepNhan.TenBango("081106/15"), "08110615-番号.PDF");

            string goc = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ohta_test_tm_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(goc, "2026", "11", "Don 081106-15 Kozai"));
            Kiem("thu muc don: tim san (3 cap)", PhongTiepNhan.ThuMucDon(goc, "081106-15"), System.IO.Path.Combine(goc, "2026", "11", "Don 081106-15 Kozai"));
            Kiem("thu muc don: chua co -> goc\\title", PhongTiepNhan.ThuMucDon(goc, "999999"), System.IO.Path.Combine(goc, "999999"));
            Kiem("thu muc don: goc rong / title ngan", (PhongTiepNhan.ThuMucDon("", "999999") == null) + "|" + (PhongTiepNhan.ThuMucDon(goc, "1") == null), "True|True");
            string fBam = System.IO.Path.Combine(goc, "a.txt");
            System.IO.File.WriteAllText(fBam, "abc");
            Kiem("ma bam sha256", PhongTiepNhan.MaBam(fBam), "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
            System.IO.Directory.Delete(goc, true);

            var tn = new TiepNhan { LoaiDon = "nua_repeat", CongTy = "Cty 合田" };
            tn.Tep.Add(new TepDon { Ten = "図面.pdf", Nguon = "chat", Loai = "ban_ve", MaBam = "ab" });
            var tn2 = PhongTiepNhan.Doc(PhongTiepNhan.Ghi(tn));
            Kiem("tiep_nhan.json ghi roi doc", tn2.LoaiDon + "|" + tn2.CongTy + "|" + tn2.Tep[0].Ten + "|" + tn2.Tep[0].Nguon + "|" + tn2.Tep[0].MaBam, "nua_repeat|Cty 合田|図面.pdf|chat|ab");
            Kiem("tiep_nhan.json hong -> null", PhongTiepNhan.Doc("{x") == null && PhongTiepNhan.Doc("") == null, true);

            KetQuaDxf dx = DocDxf.Doc(Dxf);
            Kiem("dxf: 2 lo O11", string.Join(",", dx.Lo.Select(l => l.D)), "11,11");
            Kiem("dxf: kich thuoc bao", (dx.MaxX - dx.MinX) + "x" + (dx.MaxY - dx.MinY), "200x100");
            Kiem("dxf: chu + giai ma \\U+ + bo ma mtext", string.Join(" / ", dx.Chu), "SS400 t=4.5 数量 2 / 曲\nR5");
            Kiem("dxf: tom tat gom lo", DocDxf.TomTat("a.dxf", dx).Contains("Lo O11: 2 cai"), true);
            Kiem("dxf: CP932", string.Join("", DocDxf.Doc(DocDxf.DocChu(new byte[] { 0x30, 0x0A })).Chu), "");
            bool nhiPhan = false;
            try { DocDxf.DocChu(System.Text.Encoding.ASCII.GetBytes("AutoCAD Binary DXF\r\n\u001a\0xxxxx")); } catch (System.IO.InvalidDataException) { nhiPhan = true; }
            Kiem("dxf nhi phan -> bao loi", nhiPhan, true);

            const string phanHoi = @"{""stop_reason"":""end_turn"",""usage"":{""input_tokens"":100,""cache_read_input_tokens"":2000,""output_tokens"":50},
 ""content"":[{""type"":""text"",""text"":""{\""tam\"":[{\""so_ban_ve\"":\""A-1\"",\""ten\"":\""ブラケット\"",\""M\"":\""SS400\"",\""Z\"":\""SS400\"",\""T\"":4.5,\""Q\"":\""2\"",\""mat\"":\""\"",\""kich_thuoc\"":\""200 x 100\"",\""dung_sai\"":\""\"",\""ghi_chu_jp\"":\""\"",\""ghi_chu_vi\"":\""\"",\""trang\"":\""1\"",\""canh\"":[{\""dai_ngoai\"":[100,50],\""goc\"":90,\""r_trong\"":0,\""kieu\"":\""be\"",\""huong\"":\""V\""}],\""lo\"":[],\""khong_chac\"":[\""M\""]}]}""}]}";
            var ph = PhongDocBanVe.DocPhanHoi(phanHoi);
            Kiem("phan hoi: 1 tam, T so -> chu, khong chac", ph.Loi + "|" + ph.Tam.Count + "|" + ph.Tam[0].T + "|" + ph.Tam[0].CanhBao + "|" + ph.Tam[0].TomTatCanh, "|1|4.5|⚠ M|1 chỗ bẻ");
            Kiem("phan hoi: token", ph.Vao + "/" + ph.DocCache + "/" + ph.Ra, "100/2000/50");
            Kiem("phan hoi: tu choi / cat / loi key",
                 PhongDocBanVe.DocPhanHoi(@"{""stop_reason"":""refusal"",""content"":[]}").Loi + "|" + PhongDocBanVe.DocPhanHoi(@"{""stop_reason"":""max_tokens"",""content"":[]}").Loi.StartsWith("Phản hồi bị cắt") + "|" +
                 PhongDocBanVe.DocPhanHoi(@"{""type"":""error"",""error"":{""type"":""authentication_error"",""message"":""x""}}").Loi.StartsWith("API key"), "AI từ chối đọc tệp này|True|True");
            Kiem("phan hoi hong", PhongDocBanVe.DocPhanHoi("<html>").Loi, "Phản hồi AI không đọc được");
            Kiem("tien opus: 1M vao + 1M ra", PhongDocBanVe.Tien("claude-opus-5-5", 1000000, 0, 0, 1000000), 24m);

            var body = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(PhongDocBanVe.Body("claude-opus-5-5", "dan", "qc", "a.pdf", "QUJD", null));
            var nd = (List<object>)((OhtaTool.Plugin.Core.JsonObject)((List<object>)body.Get("messages"))[0]).Get("content");
            Kiem("body: pdf truoc chu, fallback, cache quy chuan",
                 ((OhtaTool.Plugin.Core.JsonObject)nd[0]).GetString("type") + "|" + body.GetString("fallbacks") + "|" + ((OhtaTool.Plugin.Core.JsonObject)((List<object>)body.Get("system"))[1]).ContainsKey("cache_control"), "document|default|True");
            Kiem("body: haiku khong fallback, dxf chi co chu",
                 ((OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(PhongDocBanVe.Body("claude-haiku-5-5", "d", "q", "a.dxf", null, "DXF: a"))).ContainsKey("fallbacks"), false);

            // Gộp: giữ ô người dùng sửa, tấm của tệp không đọc lại giữ nguyên
            var cu1 = new TamAi { Tep = "a.pdf", SoBanVe = "A-1", Ten = "X", M = "SPCC", Moi = false };
            cu1.DaSua.Add("M");
            var cu2 = new TamAi { Tep = "b.pdf", SoBanVe = "B-1" };
            var cu3 = new TamAi { Tep = "a.pdf", SoBanVe = "A-9" };
            var moi1 = new TamAi { Tep = "a.pdf", SoBanVe = "A-1", Ten = "X", M = "SS400" };
            moi1.KhongChac.Add("M");
            var gop = PhongDocBanVe.Gop(new List<TamAi> { cu1, cu2, cu3 }, new List<TamAi> { moi1 }, new HashSet<string> { "a.pdf" });
            Kiem("gop: giu o da sua + tick Moi, bo tam cu da mat, giu tep khac",
                 string.Join(",", gop.Select(t => t.SoBanVe)) + "|" + gop.First(t => t.SoBanVe == "A-1").M + "|" + gop.First(t => t.SoBanVe == "A-1").Moi + "|" + gop.First(t => t.SoBanVe == "A-1").CanhBao, "B-1,A-1|SPCC|False|");

            var ct1 = new ChiTiet { TokenVao = 5, TokenRa = 7, Tien = 0.12m, XacNhan = "2026-11-11 17:50" };
            ct1.MaBamDaDoc["a.pdf"] = "h1";
            ct1.Tam.Add(gop[1]);
            var ct2 = PhongDocBanVe.Doc(PhongDocBanVe.Ghi(ct1));
            Kiem("chi_tiet.json ghi roi doc", ct2.Tam[0].SoBanVe + "|" + ct2.Tam[0].M + "|" + string.Join(",", ct2.Tam[0].DaSua) + "|" + ct2.Tam[0].Moi + "|" + ct2.MaBamDaDoc["a.pdf"] + "|" + ct2.Tien + "|" + ct2.Tam[0].Tep + "|" + ct2.XacNhan, "A-1|SPCC|M|False|h1|0.12|a.pdf|2026-11-11 17:50");
            Kiem("chi_tiet.json cu (chua co xac_nhan) -> chua xac nhan", PhongDocBanVe.Doc("{\"tam\":[]}").XacNhan, "");
            Kiem("chi_tiet.json hong -> null", PhongDocBanVe.Doc("[1]") == null, true);
        }

        private static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var tabs = PhanTichTeams.DocTab(Tabs);
            Kiem("3 tab", tabs.Count, 3);
            Kiem("Tab Bai dang khong phai Lists", tabs[0].ListUrl, null);
            Kiem("Tab Lists: URL giai ma tu dest=", tabs[1].ListUrl, "https://kohtavn.sharepoint.com/sites/OHTA/Lists/2026");
            Kiem("Tab web khong phai Lists", tabs[2].ListUrl, null);
            // Cấu hình thật của tab Lists trong Teams (11/2026): "list" tương đối, có dấu cách
            Kiem("Tab Lists that: list tuong doi + web",
                 PhanTichTeams.TimUrlList("https://kohtavn.sharepoint.com/sites/OHTA-OFT/_layouts/15/FileBrowser.aspx?fileBrowser=%7B%22sdk%22%3A%228%2E0%22%2C%22entry%22%3A%7B%22sharePoint%22%3A%7B%22byPath%22%3A%7B%22list%22%3A%22%2Fsites%2FOHTA%2DOFT%2FLists%2F2024%22%2C%22web%22%3A%22https%3A%2F%2Fkohtavn%2Esharepoint%2Ecom%2Fsites%2FOHTA%2DOFT%22%7D%7D%7D%7D&app=Teams&scenario=Lists&auth=none&teamId={teamId}"),
                 "https://kohtavn.sharepoint.com/sites/OHTA-OFT/Lists/2024");
            Kiem("Tab Lists that: ten list co dau cach",
                 PhanTichTeams.TimUrlList("https://kohtavn.sharepoint.com/sites/msteams_3bcaea/_layouts/15/FileBrowser.aspx?fileBrowser=%7B%22sdk%22%3A%228%2E0%22%2C%22entry%22%3A%7B%22sharePoint%22%3A%7B%22byPath%22%3A%7B%22list%22%3A%22%2Fsites%2Fmsteams%5F3bcaea%2FLists%2FTt%20c%20cc%20mc%22%2C%22web%22%3A%22https%3A%2F%2Fkohtavn%2Esharepoint%2Ecom%2Fsites%2Fmsteams%5F3bcaea%22%7D%7D%7D%7D&app=Teams&scenario=Lists"),
                 "https://kohtavn.sharepoint.com/sites/msteams_3bcaea/Lists/Tt c cc mc");
            Kiem("Tab Files (folder, khong co list) = chua ho tro",
                 PhanTichTeams.TimUrlList("https://{teamSiteDomain}{teamSitePath}/_layouts/15/filebrowser.aspx?fileBrowser=%7B%22sdk%22%3A%22v1.0%22%2C%22entry%22%3A%7B%22sharePoint%22%3A%7B%22byPath%22%3A%7B%22web%22%3A%22{teamSitePath}%22%2C%22folder%22%3A%22{channelRelativeUrl}%22%7D%7D%7D%7D&app=teamsfile"),
                 null);
            var coCach = PhanTichTeams.TachSite("https://kohtavn.sharepoint.com/sites/msteams_3bcaea/Lists/Tt c cc mc");
            Kiem("TachSite URL co dau cach", coCach.Host + "|" + coCach.DuongSite, "kohtavn.sharepoint.com|/sites/msteams_3bcaea");
            Kiem("TimListId khop webUrl %20 voi dau cach",
                 PhanTichTeams.TimListId(@"{""value"":[{""id"":""L9"",""webUrl"":""https://kohtavn.sharepoint.com/sites/msteams_3bcaea/Lists/Tt%20c%20cc%20mc""}]}",
                                          "https://kohtavn.sharepoint.com/sites/msteams_3bcaea/Lists/Tt c cc mc"), "L9");

            var site = PhanTichTeams.TachSite(tabs[1].ListUrl);
            Kiem("TachSite", site.Host + "|" + site.DuongSite, "kohtavn.sharepoint.com|/sites/OHTA");
            Kiem("TimListId theo webUrl (khong phan biet hoa thuong, /)",
                 PhanTichTeams.TimListId(@"{""value"":[{""id"":""L0"",""webUrl"":""https://kohtavn.sharepoint.com/sites/OHTA/Lists/Khac""},{""id"":""L1"",""webUrl"":""https://KOHTAVN.sharepoint.com/sites/OHTA/Lists/2026/""}]}",
                                          tabs[1].ListUrl), "L1");

            var cots = PhanTichTeams.DocCot(Cots);
            Kiem("Bo cot an / chi doc / he thong, giu Title", string.Join(",", cots.Select(c => c.Ten)), "Title,NguoiTK,NguoiCheck1,TrangThai,Han,Status,GiaCong,ChuY");
            var chinh = PhanTichTeams.KhopCot(cots);
            Kiem("Khop cot chinh theo ten co dau", string.Join(",", chinh.Han, chinh.TrangThai, chinh.StatusJp, chinh.TrienKhai, chinh.Check),
                 "Han,TrangThai,Status,NguoiTK,NguoiCheck1");

            var nguoi = new Dictionary<string, string> { ["12"] = "Mai Trung Nam", ["13"] = "Lê Gia Vinh" };
            var don = PhanTichTeams.DocDon(Items, cots, chinh, id => nguoi.TryGetValue(id ?? "", out string t) ? t : "#" + id);
            Kiem("3 don", don.Count, 3);
            var d = don[0];
            Kiem("Nguoi trien khai tu LookupId", d.TrienKhai, "Mai Trung Nam");
            Kiem("Han -> ngay gio may", d.Han, new DateTime(2026, 10, 22, 7, 0, 0, DateTimeKind.Utc).ToLocalTime());
            Kiem("Chua xong", d.DaXong, false);
            Kiem("Nhieu lua chon noi bang dau phay", d.TatCa.First(k => k.Key.StartsWith("加工")).Value, "切, 曲");
            Kiem("Chu nhieu dong bo HTML", d.TatCa.First(k => k.Key.StartsWith("注意")).Value, "来月から&");
            Kiem("CuaToi theo ten", d.CuaToi("Lê Gia Vinh") + "|" + d.CuaToi("Ai Khac"), "True|False");
            Kiem("Trang thai Da hoan thanh = xong", don[1].DaXong, true);
            Kiem("ステータス 完了 = xong", don[2].DaXong, true);
            Kiem("Nguoi nhieu gia tri lay LookupValue", don[2].Check, "Lê Gia Vinh");
            Kiem("TrangSau", PhanTichTeams.TrangSau(Items), "https://graph.microsoft.com/v1.0/next");
            Kiem("DocNguoi", PhanTichTeams.DocNguoi(@"{""value"":[{""id"":""12"",""fields"":{""Title"":""Mai Trung Nam""}}]}")["12"], "Mai Trung Nam");

            // Dạng thật của danh sách 注文書2026.1 (11/2026)
            var don2 = PhanTichTeams.DocDon(@"{""value"":[{""id"":""9"",""fields"":{""Title"":""126165"",
                ""NguoiTK"":{""LookupId"":296,""LookupValue"":""Đỗ Văn Quang"",""Email"":""q@x""},
                ""TrangThai"":[""Đã hoàn thành"",""Đã nhập giá""]}}]}", cots, chinh, id => "#" + id);
            Kiem("Nguoi 1 gia tri dang object -> LookupValue", don2[0].TrienKhai, "Đỗ Văn Quang");
            Kiem("Trang thai nhieu gia tri co Da hoan thanh = xong", don2[0].DaXong, true);
            var huy = PhanTichTeams.DocDon(@"{""value"":[{""id"":""a"",""fields"":{""Title"":""129487"",""Status"":""キャンセル""}},{""id"":""b"",""fields"":{""Title"":""129500"",""TrangThai"":""STOP""}},{""id"":""c"",""fields"":{""Title"":""x"",""TrangThai"":""Đang check""}}]}", cots, chinh, id => "#" + id);
            Kiem("キャンセル / STOP an nhu xong, Dang check thi khong", string.Join(",", huy.Select(x => x.DaXong)), "True,True,False");
            Kiem("Bo cot teamsConversationMetadata / _FormSourceId",
                 PhanTichTeams.DocCot(@"{""value"":[{""name"":""teamsConversationMetadata_D86AE2"",""displayName"":""x"",""text"":{}},{""name"":""_FormSourceId_0"",""displayName"":""y"",""text"":{}}]}").Count, 0);
            Kiem("TimListNguoi theo _catalogs/users",
                 PhanTichTeams.TimListNguoi(@"{""value"":[{""id"":""a"",""webUrl"":""https://k/sites/s/Lists/x""},{""id"":""u"",""webUrl"":""https://k/sites/s/_catalogs/users""}]}"), "u");

            var toanBo = PhanTichTeams.DocToanBo(new PhanTichTeams.ThoList
            {
                Cot = Cots,
                Nguoi = new List<string> { @"{""value"":[{""id"":""12"",""fields"":{""Title"":""Mai Trung Nam""}}]}", @"{""value"":[{""id"":""13"",""fields"":{""Title"":""Lê Gia Vinh""}}]}" },
                Dong = new List<string> { Items, @"{""value"":[{""id"":""4"",""fields"":{""Title"":""X4""}}]}" },
            });
            Kiem("DocToanBo: gop trang nguoi + trang dong", toanBo.Count + "|" + toanBo[0].TrienKhai + "|" + toanBo[0].Check, "4|Mai Trung Nam|Lê Gia Vinh");

            Kiem("IdNguoiCan: chi LookupId cua cot nguoi",
                 string.Join(",", PhanTichTeams.IdNguoiCan(new PhanTichTeams.ThoList { Cot = Cots, Dong = new List<string> { Items,
                     @"{""value"":[{""id"":""5"",""fields"":{""NguoiTKLookupId"":[""14"",""12""],""AuthorLookupId"":""99""}}]}" } })),
                 "12,13,14");

            Kiem("Danh sach nhom/kenh", PhanTichTeams.DocDanhSach(@"{""value"":[{""id"":""a"",""displayName"":""Xuong""},{""displayName"":""thieu id""}]}").Count, 1);
            Kiem("BoDau", PhanTichTeams.BoDau(" Hạn Hoàn Thành Đơn "), "han hoan thanh don");
            var t = PhanTichTeams.DocToken(@"{""token_type"":""Bearer"",""expires_in"":3599,""access_token"":""AT"",""refresh_token"":""RT""}");
            Kiem("DocToken", t.Access + "|" + t.Refresh + "|" + t.HetSau, "AT|RT|3599");
            var ma = PhanTichTeams.DocMa(PhanTichTeams.GhiMa(" abc-tenant \n", "xyz-client"));
            Kiem("teams.json ghi roi doc lai, bo khoang trang", ma.Tenant + "|" + ma.Client, "abc-tenant|xyz-client");
            var thieu = PhanTichTeams.DocMa(@"{""tenantId"":""t""}");
            Kiem("teams.json thieu clientId -> rong", thieu.Tenant + "|" + thieu.Client, "t|");

            // dang_lam.json (ung dung OhtaTeams ghi, tab CAD doc)
            var dA = new DonTeams { Id = "1", Title = "2026-1234 \"A\"", Han = new DateTime(2026, 11, 12), TrangThai = "Đang triển khai", TrienKhai = "Nam", WebUrl = "https://x" };
            dA.TatCa.Add(new KeyValuePair<string, string>("注意", "dòng 1\ndòng 2"));
            var dB = new DonTeams { Id = "2", Title = "xong", DaXong = true };
            var dC = new DonTeams { Id = "3", Title = "xong nhung dang chon", DaXong = true };
            var chon = PhanTichDangLam.ChonGhi(new[] { dA, dB, dC }, "3");
            Kiem("dang_lam: chi don chua xong + don dang chon", string.Join(",", chon.Select(x => x.Id)), "1,3");
            Kiem("dang_lam: khong chon -> bo het don da xong", PhanTichDangLam.ChonGhi(new[] { dA, dB }, "").Count, 1);
            var dl = PhanTichDangLam.Doc(PhanTichDangLam.Ghi(new DangLam { Luc = new DateTime(2026, 11, 10, 9, 30, 5), ThuMucGoc = @"D:\Don", DonChon = "3", Don = chon }));
            Kiem("dang_lam ghi roi doc: luc, thu muc, don chon", dl.Luc.ToString("s") + "|" + dl.ThuMucGoc + "|" + dl.DonChon, @"2026-11-10T09:30:05|D:\Don|3");
            var a2 = dl.Don[0];
            Kiem("dang_lam ghi roi doc: don", a2.Title + "|" + a2.Han?.ToString("dd/MM") + "|" + a2.TrangThai + "|" + a2.TrienKhai + "|" + a2.WebUrl + "|" + a2.DaXong,
                 "2026-1234 \"A\"|12/11|Đang triển khai|Nam|https://x|False");
            Kiem("dang_lam ghi roi doc: cot (xuong dong, chu Nhat)", a2.TatCa.Count == 1 ? a2.TatCa[0].Key + "=" + a2.TatCa[0].Value : "?", "注意=dòng 1\ndòng 2");
            Kiem("dang_lam ghi roi doc: don da xong, khong han", dl.Don[1].DaXong + "|" + (dl.Don[1].Han == null), "True|True");
            Kiem("dang_lam file hong -> null", PhanTichDangLam.Doc("{ hong") == null, true);
            Kiem("dang_lam thieu mang don -> null", PhanTichDangLam.Doc(@"{""luc"":""x""}") == null, true);

            // Khop ban ve <-> don
            var kh = new DangLam { ThuMucGoc = @"D:\Don\", DonChon = "9", Don = new List<DonTeams> {
                new DonTeams { Id = "1", Title = "C1291" }, new DonTeams { Id = "2", Title = "C129192" },
                new DonTeams { Id = "3", Title = "Tấm Đế" }, new DonTeams { Id = "4", Title = "A" }, new DonTeams { Id = "9", Title = "Don ghim" } } };
            string Khop(string f) => PhanTichDangLam.KhopBanVe(f, kh)?.Id ?? "-";
            Kiem("khop: thu muc cap 1 chua Title", Khop(@"D:\Don\C129192_ABC\ban ve.dwg"), "2");
            Kiem("khop: nhieu don khop -> Title dai nhat", Khop(@"d:/don/2026/c129192 x/a.dwg"), "2");
            Kiem("khop: bo dau, khong phan biet hoa thuong", Khop(@"D:\Don\tam de 01\a.dwg"), "3");
            Kiem("khop: cap 3 van khop", Khop(@"D:\Don\a\b\C1291\x.dwg"), "1");
            Kiem("khop: cap 4 khong khop", Khop(@"D:\Don\a\b\c\C1291\x.dwg"), "-");
            Kiem("khop: ten file khong tinh", Khop(@"D:\Don\khac\C129192.dwg"), "-");
            Kiem("khop: ngoai thu muc goc", Khop(@"D:\Donkhac\C129192\x.dwg"), "-");
            Kiem("khop: Title qua ngan bi bo", Khop(@"D:\Don\ABC\x.dwg"), "-");
            Kiem("khop: ban ve chua luu", Khop(""), "-");
            Kiem("dang lam: khop ban ve truoc", PhanTichDangLam.DonDangLam(@"D:\Don\C129192\x.dwg", kh).Don.Id + "|" + PhanTichDangLam.DonDangLam(@"D:\Don\C129192\x.dwg", kh).TheoBanVe, "2|True");
            Kiem("dang lam: khong khop -> don ghim", PhanTichDangLam.DonDangLam(@"E:\x.dwg", kh).Don.Id + "|" + PhanTichDangLam.DonDangLam(@"E:\x.dwg", kh).TheoBanVe, "9|False");
            kh.DonChon = "";
            Kiem("dang lam: khong khop, khong ghim -> null", PhanTichDangLam.DonDangLam(@"E:\x.dwg", kh).Don == null, true);
            Kiem("dang lam: chua co file -> null", PhanTichDangLam.DonDangLam(@"E:\x.dwg", null).Don == null, true);

            // Sua don: cot thuc te cua Lists (displayAs checkBoxes = chon nhieu, format dateTime/dateOnly)
            var cs = PhanTichTeams.DocCot(@"{ ""value"": [
  { ""name"": ""Title"", ""displayName"": ""Title"", ""text"": { ""allowMultipleLines"": false } },
  { ""name"": ""TT"", ""displayName"": ""Trạng thái đơn hàng"", ""choice"": { ""choices"": [""Đang triển khai"",""Đang check"",""Đã hoàn thành""], ""displayAs"": ""checkBoxes"" } },
  { ""name"": ""MD"", ""displayName"": ""Mức độ"", ""choice"": { ""choices"": [""Đơn thường"",""Đơn chất lượng cao""], ""displayAs"": ""dropDownMenu"" } },
  { ""name"": ""BD"", ""displayName"": ""Bắt đầu"", ""dateTime"": { ""format"": ""dateTime"" } },
  { ""name"": ""Han"", ""displayName"": ""Hạn"", ""dateTime"": { ""format"": ""dateOnly"" } },
  { ""name"": ""CY"", ""displayName"": ""Chú ý"", ""text"": { ""allowMultipleLines"": true, ""textType"": ""plain"" } },
  { ""name"": ""HT"", ""displayName"": ""Ghi chú HTML"", ""text"": { ""allowMultipleLines"": true, ""textType"": ""richText"" } },
  { ""name"": ""So"", ""displayName"": ""Số tấm"", ""number"": {} },
  { ""name"": ""Sao"", ""displayName"": ""Ngôi sao"", ""boolean"": {} },
  { ""name"": ""Ng"", ""displayName"": ""Người"", ""personOrGroup"": { ""allowMultipleSelection"": true } },
  { ""name"": ""Ng1"", ""displayName"": ""Người 1"", ""personOrGroup"": { ""allowMultipleSelection"": false } }
]}");
            CotList C(string ten) => cs.First(x => x.Ten == ten);
            Kiem("sua: checkBoxes = chon nhieu, dropDown = 1", C("TT").ChonNhieu + "|" + C("MD").ChonNhieu + "|" + C("TT").LuaChon.Count, "True|False|3");
            Kiem("sua: dateTime co gio, dateOnly khong", C("BD").CoGio + "|" + C("Han").CoGio, "True|False");
            Kiem("sua: cot sua duoc", string.Join(",", cs.Where(x => x.SuaDuoc).Select(x => x.Ten)), "Title,TT,MD,BD,Han,CY,So,Sao,Ng,Ng1");
            var dS = PhanTichTeams.DocDon(@"{""value"":[{""id"":""7"",""eTag"":""\""abc,24\"""",""fields"":{""Title"":""C1"",""TT"":[""Đang check""],""BD"":""2026-11-10T01:30:00Z"",""So"":12.5,""Sao"":true,""HT"":""<b>x</b>""}}]}",
                                            cs, PhanTichTeams.KhopCot(cs), id => id)[0];
            Kiem("sua: doc eTag va gia tri tho", dS.ETag + "|" + dS.Tho.Count + "|" + dS.Tho.ContainsKey("HT"), "\"abc,24\"|5|False");
            Kiem("sua: hien gio cho cot co gio", dS.TatCa.First(x => x.Key == "Bắt đầu").Value, PhanTichTeams.DocNgay("2026-11-10T01:30:00Z")?.ToString("dd/MM/yyyy HH:mm"));
            Kiem("sua: form chon nhieu = list", string.Join("+", (List<string>)SuaDon.GiaTriForm(C("TT"), dS.Tho["TT"])), "Đang check");
            Kiem("sua: form so", SuaDon.GiaTriForm(C("So"), dS.Tho["So"]), (object)12.5);
            Kiem("sua: form ngay = gio may", SuaDon.GiaTriForm(C("BD"), dS.Tho["BD"]), (object)PhanTichTeams.DocNgay("2026-11-10T01:30:00Z"));
            Kiem("sua: form cot trong", SuaDon.GiaTriForm(C("CY"), null), "");
            Kiem("sua: giong nhau list khac thu tu", SuaDon.GiongNhau(new List<string> { "a", "b" }, new List<string> { "b", "a" }), true);
            Kiem("sua: giong nhau chu bo khoang trang, null = rong", SuaDon.GiongNhau(" x ", "x") + "|" + SuaDon.GiongNhau(null, ""), "True|True");
            Kiem("sua: khac ngay", SuaDon.GiongNhau(new DateTime(2026, 1, 1, 8, 0, 0), new DateTime(2026, 1, 1, 8, 1, 0)), false);
            Kiem("sua: doc so 1,5 / rong / chu", (SuaDon.DocSo("1,5", out double? s1) ? s1?.ToString(System.Globalization.CultureInfo.InvariantCulture) : "x") + "|" + (SuaDon.DocSo("", out double? s2) && s2 == null) + "|" + SuaDon.DocSo("abc", out _), "1.5|True|False");
            Kiem("sua: doc gio", (SuaDon.DocGio("8:30", out TimeSpan g1) ? g1.ToString() : "x") + "|" + SuaDon.DocGio("25:00", out _) + "|" + SuaDon.DocGio("", out _), "08:30:00|False|True");
            Kiem("sua: choice ngoai lua chon -> loi", SuaDon.KiemTra(C("MD"), "Lạ") != null && SuaDon.KiemTra(C("TT"), new List<string> { "Đang check" }) == null && SuaDon.KiemTra(C("MD"), "") == null, true);
            var ngayHan = new DateTime(2026, 11, 20, 15, 45, 0);
            string body = SuaDon.Body(new[] {
                new KeyValuePair<CotList, object>(C("TT"), new List<string> { "Đang check", "Đã hoàn thành" }),
                new KeyValuePair<CotList, object>(C("Han"), ngayHan),
                new KeyValuePair<CotList, object>(C("So"), 3.0),
                new KeyValuePair<CotList, object>(C("Sao"), false),
                new KeyValuePair<CotList, object>(C("MD"), ""),
                new KeyValuePair<CotList, object>(C("CY"), "dong 1\ndong 2  ") });
            var b = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(body);
            Kiem("sua body: chon nhieu kem @odata.type", b.GetString("TT@odata.type") + "|" + string.Join("+", (List<object>)b.Get("TT")), "Collection(Edm.String)|Đang check+Đã hoàn thành");
            Kiem("sua body: cot chi ngay = 0 gio may doi UTC", b.GetString("Han"), new DateTime(2026, 11, 20).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"));
            Kiem("sua body: so, bool, choice rong = null, chu bo cach cuoi", b.Get("So") + "|" + b.Get("Sao") + "|" + (b.ContainsKey("MD") && b.Get("MD") == null) + "|" + b.GetString("CY"), "3|False|True|dong 1\ndong 2");
            Kiem("sua body: chi cot doi", b.Items.Count, 7);

            // Cot nguoi: danh ba nhan vien + giao don
            var dN = PhanTichTeams.DocDon(@"{""value"":[
  {""id"":""1"",""eTag"":""e1"",""fields"":{""Title"":""A"",""Ng"":[{""LookupId"":28,""LookupValue"":""Bùi Hoàng Vũ""},{""LookupId"":40,""LookupValue"":""Nguyễn Văn Hoàng""}],""Ng1"":{""LookupId"":12,""LookupValue"":""Mai Trung Nam""}}},
  {""id"":""2"",""eTag"":""e2"",""fields"":{""Title"":""B"",""NgLookupId"":[""28"",""55""]}}]}", cs, PhanTichTeams.KhopCot(cs), id => "#" + id);
            var db = SuaDon.DanhBa(dN, cs);
            Kiem("danh ba: moi nguoi co ten, xep theo ten", string.Join(",", db.Select(x => x.Key + "=" + x.Value)), "28=Bùi Hoàng Vũ,12=Mai Trung Nam,40=Nguyễn Văn Hoàng");
            Kiem("nguoi: form = danh sach LookupId", string.Join("+", (List<string>)SuaDon.GiaTriForm(C("Ng"), dN[0].Tho["Ng"])) + "|" + string.Join("+", (List<string>)SuaDon.GiaTriForm(C("Ng1"), dN[0].Tho["Ng1"])), "28+40|12");
            Kiem("nguoi: chi co LookupId van doc duoc", string.Join("+", (List<string>)SuaDon.GiaTriForm(C("Ng"), dN[1].Tho["Ng"])), "28+55");
            Kiem("nguoi: cot 1 nguoi chon 2 -> loi", SuaDon.KiemTra(C("Ng1"), new List<string> { "1", "2" }) != null && SuaDon.KiemTra(C("Ng"), new List<string> { "1", "2" }) == null, true);
            var bN = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(SuaDon.Body(new[] {
                new KeyValuePair<CotList, object>(C("Ng"), new List<string> { "12", "40" }),
                new KeyValuePair<CotList, object>(C("Ng1"), new List<string>()) }));
            Kiem("nguoi body: nhieu nguoi = LookupId collection", bN.GetString("NgLookupId@odata.type") + "|" + string.Join("+", (List<object>)bN.Get("NgLookupId")), "Collection(Edm.String)|12+40");
            // Bang don kieu Lists
            var homNay = new DateTime(2026, 11, 10);
            string Mau(string tt, DateTime? han, bool xong = false) => BangDon.MauDong(new DonTeams { TrangThai = tt, Han = han, DaXong = xong }, homNay) ?? "-";
            Kiem("mau dong: qua han chua xong = hong (ke ca cau hoi)", Mau("Đang triển khai", new DateTime(2026, 11, 9)) + "|" + Mau("Câu hỏi - xác nhận", new DateTime(2026, 11, 1)), "hong|hong");
            Kiem("mau dong: chua gan han theo trang thai", Mau("Đang check", null) + "|" + Mau("Chưa triển khai", null), "vang|-");
            Kiem("mau dong: dung han theo trang thai", Mau("Đang check", homNay) + "|" + Mau("Đang triển khai", homNay) + "|" + Mau("Câu hỏi - xác nhận", homNay) + "|" + Mau("Chưa triển khai", homNay), "vang|xanh|do|-");
            Kiem("mau dong: nhieu trang thai, cau hoi uu tien", Mau("Đang check, Câu hỏi - xác nhận", homNay), "do");
            Kiem("mau dong: da xong khong to", Mau("Đã hoàn thành", null, true), "-");
            Kiem("mau nhan: co dinh, trong khoang", BangDon.MauNhan("切板", 8) == BangDon.MauNhan("切板", 8) && BangDon.MauNhan("x", 8) < 8 && BangDon.MauNhan("", 8) >= 0, true);
            var dBang = new List<DonTeams> { new DonTeams { Han = new DateTime(2026, 11, 22) }, new DonTeams(), new DonTeams { Han = new DateTime(2026, 10, 22, 7, 0, 0) } };
            dBang[0].TatCa.Add(new KeyValuePair<string, string>("Mức độ", "x"));
            Kiem("nhom han: chua gan truoc, ngay tang", string.Join(",", BangDon.NhomTheoHan(dBang).Select(g => g.Key?.ToString("dd/MM") ?? "none")), "none,22/10,22/11");
            Kiem("cot mac dinh: Title + cot co gia tri, theo thu tu", string.Join(",", BangDon.CotMacDinh(cs, dBang)), "Title,TT,MD");
            var csView = PhanTichTeams.DocCot(@"{ ""value"": [
  { ""name"": ""Title"", ""displayName"": ""Title"", ""text"": {} },
  { ""name"": ""Cty"", ""displayName"": ""CÔNG TY"", ""choice"": {} },
  { ""name"": ""NK"", ""displayName"": ""納期 (Ngày Giao)"", ""dateTime"": {} },
  { ""name"": ""CD"", ""displayName"": ""切断スタート納期"", ""dateTime"": {} },
  { ""name"": ""TK"", ""displayName"": ""NGƯỜI TRIỂN KHAI"", ""personOrGroup"": {} },
  { ""name"": ""Khac"", ""displayName"": ""Khác"", ""text"": {} } ]}");
            Kiem("cot mac dinh: theo thu tu view tien do", string.Join(",", BangDon.CotMacDinh(csView, new List<DonTeams>())), "Title,TK,CD,NK,Cty");
            var cotK = new List<string> { "A", "B", "C", "D" };
            Kiem("keo cot: sang phai chiem cho cot dich", string.Join("", BangDon.DoiCho(cotK, "A", "C")), "BCAD");
            Kiem("keo cot: sang trai chiem cho cot dich", string.Join("", BangDon.DoiCho(cotK, "D", "B")), "ADBC");
            Kiem("keo cot: tha cho cu / cot la -> null", BangDon.DoiCho(cotK, "B", "B") == null && BangDon.DoiCho(cotK, "X", "A") == null, true);
            Kiem("mau trang thai: dung bang Lists, khong phan biet hoa", BangDon.MauTrangThai("Đang sửa")?.Nen + "|" + BangDon.MauTrangThai("STOP")?.Chu + "|" + BangDon.MauTrangThai("câu hỏi - xác nhận")?.Nen, "#A8262E|#FFFFFF|#F0908E");
            Kiem("mau trang thai: gia tri la -> null", BangDon.MauTrangThai("切板") == null && BangDon.MauTrangThai(null) == null, true);
            CotList Cf(string ten, string tieuDe) => new CotList { Ten = ten, TieuDe = tieuDe, Loai = "text" };
            Kiem("phan form: Title + ten chu Nhat = OHTA COMPANY", string.Join("|", new[] { Cf("Title", "Title"), Cf("a", "受注日"), Cf("b", "ステータス"), Cf("c", "注意 (Chú Ý)") }.Select(BangDon.PhanForm).Distinct()), "OHTA COMPANY");
            Kiem("phan form: ten Viet / Anh = OFT BY OHTA", string.Join("|", new[] { Cf("d", "HẠN HOÀN THÀNH"), Cf("e", "Note"), Cf("f", "GURO - TẤM ĐƠN") }.Select(BangDon.PhanForm).Distinct()), "OFT BY OHTA");
            // Trao doi: chat + tep dinh kem (du lieu dang that cua don 130819)
            var hts = TraoDoi.DocHoiThoai(@"{""v1"":[{""conversationId"":""1794293920171"",""channelId"":""19:771e@thread.tacv2"",""teamId"":""19:A20w@thread.tacv2""},{""conversationId"":""x""},{""conversationId"":1794292715769,""channelId"":""19:5o3@thread.tacv2"",""teamId"":""19:5o3@thread.tacv2""}]}");
            Kiem("chat: doc metadata, bo phan tu thieu, id so", string.Join(",", hts.Select(h => h.ConvId + "@" + h.ChannelId)), "1794293920171@19:771e@thread.tacv2,1794292715769@19:5o3@thread.tacv2");
            Kiem("chat: metadata rong / hong -> rong", TraoDoi.DocHoiThoai("").Count + TraoDoi.DocHoiThoai("{hong").Count, 0);
            var dCt = PhanTichTeams.DocDon(@"{""value"":[{""id"":""2758"",""fields"":{""Title"":""130819"",""Attachments"":true,""teamsConversationMetadata_D86AE2"":""{\""v1\"":[]}""}}]}", cs, PhanTichTeams.KhopCot(cs), id => id)[0];
            Kiem("chat: don giu metadata + co tep", dCt.HoiThoaiMeta + "|" + dCt.CoTep, "{\"v1\":[]}|True");
            var tins = TraoDoi.DocTin(@"{""value"":[
  {""id"":""2"",""createdDateTime"":""2026-11-10T07:00:00Z"",""from"":{""user"":{""displayName"":""谷本""}},""body"":{""contentType"":""text"",""content"":""OK""}},
  {""id"":""3"",""deletedDateTime"":""2026-11-10T08:00:00Z"",""from"":null,""body"":{""content"":""""}},
  {""id"":""1"",""createdDateTime"":""2026-11-10T06:58:00Z"",""from"":{""user"":{""displayName"":""Nguyễn A Hùng""}},""body"":{""contentType"":""html"",""content"":""<p><at id=\""0\"">Phạm Anh Tuấn</at>&nbsp;Làm giúp anh nhé!</p>""},""attachments"":[{""id"":""a"",""contentType"":""reference"",""name"":""130825.zip"",""contentUrl"":""https://h/sites/x/Shared Documents/130825.zip""},{""id"":""the"",""contentType"":""application/vnd.microsoft.card.adaptive""}]}]}");
            Kiem("chat: tin cu truoc, bo tin da xoa, bo HTML", string.Join(" / ", tins.Select(t => t.Nguoi + ": " + t.Chu + (t.SoTep > 0 ? " +" + t.SoTep : ""))), "Nguyễn A Hùng: Phạm Anh Tuấn Làm giúp anh nhé! +1 / 谷本: OK");
            Kiem("chat: mot tin (object)", TraoDoi.DocTin(@"{""id"":""9"",""from"":{""user"":{""displayName"":""A""}},""body"":{""contentType"":""text"",""content"":""x""}}")[0].Nguoi, "A");
            Kiem("chat: tep gui kem (bo the Ve: don)", string.Join(",", tins[0].Tep.Select(x => x.Ten + "=" + x.DuongDan)), "130825.zip=https://h/sites/x/Shared Documents/130825.zip");
            var anh = TraoDoi.AnhTrongTin(@"<p><img src=""https://graph.microsoft.com/v1.0/teams/t/channels/c/messages/1/replies/2/hostedContents/aWQ9eF8w/$value"" width=""340"" alt=""image""></p>
<img alt=""emoji"" src=""https://statics.teams.cdn.office.net/emojis/smile.png""><IMG SRC=""https://graph.microsoft.com/v1.0/x/hostedContents/b/$value?a=1&amp;b=2"">");
            Kiem("chat: anh dan trong tin, bo emoji CDN, giai ma &amp;", string.Join(" | ", anh),
                 "https://graph.microsoft.com/v1.0/teams/t/channels/c/messages/1/replies/2/hostedContents/aWQ9eF8w/$value | https://graph.microsoft.com/v1.0/x/hostedContents/b/$value?a=1&b=2");
            Kiem("chat: ma share cho Graph /shares", TraoDoi.MaShare("https://h/a b?"), "u!aHR0cHM6Ly9oL2EgYj8");
            string M(params string[] kenh) => "{\"v1\":[" + string.Join(",", kenh.Select((k, i) => "{\"conversationId\":\"" + i + "\",\"channelId\":\"" + k + "\",\"teamId\":\"T" + k + "\"}")) + "]}";
            var dKenh = new[] { new DonTeams { HoiThoaiMeta = M("jp", "ju") }, new DonTeams { HoiThoaiMeta = M("jp") }, new DonTeams { HoiThoaiMeta = M("khac", "ju") }, new DonTeams() };
            Kiem("kenh pho bien: 2 kenh nhieu nhat", string.Join(",", TraoDoi.KenhPhoBien(dKenh).Select(h => h.ChannelId + "@" + h.TeamInternalId)), "jp@Tjp,ju@Tju");
            string metaMoi = TraoDoi.ThemHoiThoai(M("jp"), new HoiThoai { ConvId = "999", ChannelId = "ju", TeamInternalId = "Tju" });
            Kiem("them hoi thoai: giu chuoi cu + chuoi moi", string.Join(",", TraoDoi.DocHoiThoai(metaMoi).Select(h => h.ConvId + "@" + h.ChannelId)), "0@jp,999@ju");
            Kiem("them hoi thoai: don chua co metadata", TraoDoi.DocHoiThoai(TraoDoi.ThemHoiThoai("", new HoiThoai { ConvId = "1", ChannelId = "c", TeamInternalId = "t" })).Count, 1);
            Kiem("tin mo dau: link don + ma hoa", TraoDoi.HtmlMoDau("130825", "https://x/y?a=1", "xin <xác nhận>"), "<p><b>Về đơn <a href=\"https://x/y?a=1\">130825</a></b></p><p>xin &lt;xác nhận&gt;</p>");
            var dCot = PhanTichTeams.DocDon(@"{""value"":[{""id"":""1"",""fields"":{""Title"":""a"",""teamsConversationMetadata_D86AE2"":""{}""}}]}", cs, PhanTichTeams.KhopCot(cs), id => id)[0];
            Kiem("don nho ten cot metadata", dCot.HoiThoaiCot, "teamsConversationMetadata_D86AE2");
            var bThe = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(TraoDoi.BodyMoDau("<p>x</p>", "009936cf", "130828"));
            var aThe = (OhtaTool.Plugin.Core.JsonObject)((List<object>)bThe.Get("attachments"))[0];
            Kiem("tin mo dau kem the: tabReference + the attachment trong noi dung",
                 aThe.GetString("id") + "|" + aThe.GetString("contentType") + "|" + aThe.GetString("name") + "|" + ((OhtaTool.Plugin.Core.JsonObject)bThe.Get("body")).GetString("content"),
                 "tab::009936cf|tabReference|Về: 130828|<p>x</p><attachment id=\"tab::009936cf\"></attachment>");
            var bCard = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(TraoDoi.BodyMoDauThe("<p>x</p>", "130808", "https://kohtavn.sharepoint.com/sites/OHTA/Lists/2026/2758_.000", "the1"));
            var aCard = (OhtaTool.Plugin.Core.JsonObject)((List<object>)bCard.Get("attachments"))[0];
            var card = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(aCard.GetString("content"));
            Kiem("the adaptive: loai, chu Ve: don, bam mo don, the trong noi dung",
                 aCard.GetString("contentType") + "|" + ((OhtaTool.Plugin.Core.JsonObject)((List<object>)card.Get("body"))[0]).GetString("text") + "|"
                 + ((OhtaTool.Plugin.Core.JsonObject)card.Get("selectAction")).GetString("url") + "|" + ((OhtaTool.Plugin.Core.JsonObject)bCard.Get("body")).GetString("content"),
                 "application/vnd.microsoft.card.adaptive|📋 Về: 130808|https://kohtavn.sharepoint.com/sites/OHTA/Lists/2026/DispForm.aspx?ID=2758|<p>x</p><attachment id=\"the1\"></attachment>");
            string urlDeep = PhanTichTeams.UrlTeamsMoDon(tabs[1], "19:kenh@thread.tacv2", "2770", "https://kohtavn.sharepoint.com/sites/msteams_3bcaea/Lists/Tt%20c%20cc%20mc/2770_.000", "4444444");
            Kiem("url teams mo don: deep link entity + subEntityId + channelId",
                 urlDeep,
                 "https://teams.microsoft.com/l/entity/x?webUrl=https%3A%2F%2Fkohtavn.sharepoint.com%2Fsites%2Fmsteams_3bcaea%2FLists%2FTt%2520c%2520cc%2520mc%2FDispForm.aspx%3FID%3D2770&label=4444444&context=%7B%22subEntityId%22%3A%222770%22%2C%22channelId%22%3A%2219%3Akenh%40thread.tacv2%22%7D");
            Kiem("url teams mo don: goi lai 2 lan khong long nhau", PhanTichTeams.UrlTeamsMoDon(tabs[1], "19:kenh@thread.tacv2", "2770", urlDeep, "4444444"), urlDeep);
            var tabKhongEntityUrl = new TabTeams { Id = "009936cf", Ten = "注文書", ListUrl = "https://kohtavn.sharepoint.com/sites/OHTA/Lists/2026" };
            Kiem("url teams mo don: tab khong co webUrl /l/entity/ -> dung appId Lists",
                 PhanTichTeams.UrlTeamsMoDon(tabKhongEntityUrl, "19:c@thread.tacv2", "99", "https://kohtavn.sharepoint.com/sites/OHTA/Lists/2026/99_.000", "C99"),
                 "https://teams.microsoft.com/l/entity/2a527703-1f6f-4559-a332-d8a7d288cd88/009936cf?webUrl=https%3A%2F%2Fkohtavn.sharepoint.com%2Fsites%2FOHTA%2FLists%2F2026%2FDispForm.aspx%3FID%3D99&label=C99&context=%7B%22subEntityId%22%3A%2299%22%2C%22channelId%22%3A%2219%3Ac%40thread.tacv2%22%7D");
            // Tag @ trong chat
            var tg = new List<NguoiTag> { new NguoiTag { Id = "u1", Ten = "Nguyễn Văn" }, new NguoiTag { Id = "u2", Ten = "Nguyễn Văn Khải" }, new NguoiTag { Id = "u3", Ten = "Lê Gia Vinh" } };
            var (hTag, mTag) = TraoDoi.TagHtml("@Nguyễn Văn Khải và @Lê Gia Vinh <xem>, @Lê Gia Vinh nhé", tg);
            Kiem("tag: ten dai truoc, lap lai cung id, ma hoa HTML", hTag, "<at id=\"0\">Nguyễn Văn Khải</at> và <at id=\"1\">Lê Gia Vinh</at> &lt;xem&gt;, <at id=\"1\">Lê Gia Vinh</at> nhé");
            Kiem("tag: chi nguoi co trong chu", string.Join(",", mTag.Cast<OhtaTool.Plugin.Core.JsonObject>().Select(m => m.GetString("mentionText") + "=" + ((OhtaTool.Plugin.Core.JsonObject)((OhtaTool.Plugin.Core.JsonObject)m.Get("mentioned")).Get("user")).GetString("id"))), "Nguyễn Văn Khải=u2,Lê Gia Vinh=u3");
            var bTag = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(TraoDoi.BodyTraLoi("@Lê Gia Vinh ok", tg));
            Kiem("tag: body tra loi co mentions", ((List<object>)bTag.Get("mentions")).Count + "|" + ((OhtaTool.Plugin.Core.JsonObject)bTag.Get("body")).GetString("content"), "1|<at id=\"0\">Lê Gia Vinh</at> ok");
            Kiem("tag: khong tag -> khong co mentions", ((OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(TraoDoi.BodyTraLoi("ok", tg))).ContainsKey("mentions"), false);
            Kiem("tag: doan dang go sau @", TraoDoi.DoanTag("chao @le g", 10).Tim + "|" + TraoDoi.DoanTag("a@b", 3).ViTri + "|" + TraoDoi.DoanTag("@", 1).Tim + "|" + TraoDoi.DoanTag("@ab\ncd", 6).ViTri, "le g|-1||-1");
            Kiem("tag: doc nguoi tag tu /users", string.Join(",", TraoDoi.DocNguoiTag(@"{""value"":[{""id"":""a"",""displayName"":""Vinh"",""accountEnabled"":true},{""id"":""b"",""displayName"":""Tắt"",""accountEnabled"":false},{""id"":""c"",""displayName"":""An""}]}").Select(n => n.Id + "=" + n.Ten)), "c=An,a=Vinh");
            // Chuyen tiep chat 受注 → 日本
            var kenhTin = ChuyenTiep.DocTinKenh(@"{""value"":[
  {""id"":""100"",""messageType"":""message"",""createdDateTime"":""2026-11-10T06:42:50Z"",""from"":{""user"":{""displayName"":""谷本""}},""body"":{""contentType"":""html"",""content"":""<p>OK</p><attachment id=\""tab::x\""></attachment>""},
   ""replies"":[{""id"":""101"",""replyToId"":""100"",""messageType"":""message"",""createdDateTime"":""2026-11-11T00:41:40Z"",""from"":{""user"":{""displayName"":""岡田""}},""body"":{""contentType"":""html"",""content"":""<p>データが届きました</p>""}},
                {""id"":""102"",""replyToId"":""100"",""messageType"":""systemEventMessage"",""createdDateTime"":""2026-11-11T00:50:00Z"",""from"":null,""body"":{""contentType"":""html"",""content"":""""}}]},
  {""id"":""200"",""messageType"":""message"",""createdDateTime"":""2026-11-11T01:00:00Z"",""from"":{""user"":{""displayName"":""山中""}},""body"":{""contentType"":""text"",""content"":""khong gan don""}}]}");
            Kiem("chuyen: doc tin kenh phang, goc id", string.Join(",", kenhTin.Select(t => t.Id + "@" + t.GocId)), "100@100,101@100,102@100,200@200");
            var mocCt = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc).ToLocalTime().Date;
            var canCt = ChuyenTiep.TinCanChuyen(kenhTin, PhanTichTeams.DocNgay("2026-11-10T12:00:00Z").Value, new HashSet<string> { "100" });
            Kiem("chuyen: chi tin nguoi, sau moc, thuoc don", string.Join(",", canCt.Select(t => t.Id)), "101");
            string dauCt = ChuyenTiep.DauChuyen("OHTA-受注", canCt[0]);
            Kiem("chuyen: dong dau", dauCt, "[OHTA-受注] 岡田 · " + PhanTichTeams.DocNgay("2026-11-11T00:41:40Z")?.ToString("dd/MM HH:mm"));
            var dichCt = new List<TinNhan> { new TinNhan { Chu = dauCt + "\nデータが届きました" } };
            Kiem("chuyen: da chuyen -> bo qua; tin khac cung phut -> chua", ChuyenTiep.DaChuyen(dichCt, "OHTA-受注", canCt[0]) + "|"
                 + ChuyenTiep.DaChuyen(dichCt, "OHTA-受注", new TinNhan { Nguoi = "岡田", Luc = canCt[0].Luc, Chu = "別の内容" }), "True|False");
            string anhG = "https://graph.microsoft.com/v1.0/teams/t/channels/c/messages/1/hostedContents/abc/$value";
            Kiem("chuyen: tag thanh chu, bo the dinh kem, doi src anh",
                 ChuyenTiep.HtmlChuyen("<p><at id=\"0\">Lê Gia Vinh</at> xem</p><img src=\"" + anhG + "\" alt=\"image\"><attachment id=\"a1\"></attachment>", new List<string> { anhG }),
                 "<p>@Lê Gia Vinh xem</p><img src=\"../hostedContents/1/$value\" alt=\"image\">");
            Kiem("chuyen: loai anh", ChuyenTiep.LoaiAnh(new byte[] { 0xFF, 0xD8, 0, 0 }) + "|" + ChuyenTiep.LoaiAnh(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), "image/jpeg|image/png");
            var bCt = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(ChuyenTiep.BodyChuyen(dauCt, "<p>x</p>",
                new[] { new AnhChuyen { TamId = "1", LoaiNoiDung = "image/png", Byte = new byte[] { 1, 2 } } }, new[] { new TepChuyen { Ten = "a.dwg", Url = "https://h/a.dwg" } }));
            var hc = (OhtaTool.Plugin.Core.JsonObject)((List<object>)bCt.Get("hostedContents"))[0];
            var tc = (OhtaTool.Plugin.Core.JsonObject)((List<object>)bCt.Get("attachments"))[0];
            Kiem("chuyen: body co anh + tep + the attachment", hc.GetString("@microsoft.graph.temporaryId") + "|" + hc.GetString("contentBytes") + "|" + tc.GetString("contentType") + "|" + tc.GetString("name")
                 + "|" + ((OhtaTool.Plugin.Core.JsonObject)bCt.Get("body")).GetString("content").Contains("<attachment id=\"" + tc.GetString("id") + "\">"), "1|AQI=|reference|a.dwg|True");
            Kiem("chuyen: ma dinh kem tu eTag tep", ChuyenTiep.GuidTuETag("\"{153FA47A-18C9-4179-BE08-9879815A9C90},1\"") + "|" + (ChuyenTiep.GuidTuETag("abc") == null), "153fa47a-18c9-4179-be08-9879815a9c90|True");
            var bEt = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(ChuyenTiep.BodyChuyen("d", "", null, new[] { new TepChuyen { Ten = "a", Url = "u", Id = "153fa47a-18c9-4179-be08-9879815a9c90" }, new TepChuyen { Ten = "b", Url = "v" } }));
            var dsEt = ((List<object>)bEt.Get("attachments")).Cast<OhtaTool.Plugin.Core.JsonObject>().Select(x => x.GetString("id")).ToList();
            Kiem("chuyen: dinh kem dung ma eTag, thieu thi GUID moi", dsEt[0] + "|" + Guid.TryParse(dsEt[1], out _), "153fa47a-18c9-4179-be08-9879815a9c90|True");
            var csTag = PhanTichTeams.DocCot(@"{ ""value"": [
  { ""name"": ""TK"", ""displayName"": ""NGƯỜI TRIỂN KHAI"", ""personOrGroup"": {} },
  { ""name"": ""C1"", ""displayName"": ""NGƯỜI CHECK 1"", ""personOrGroup"": {} },
  { ""name"": ""C2"", ""displayName"": ""NGƯỜI CHECK 2"", ""personOrGroup"": {} },
  { ""name"": ""PT"", ""displayName"": ""Người phụ trách"", ""personOrGroup"": {} } ]}");
            var dTag = new DonTeams();
            dTag.TatCa.Add(new KeyValuePair<string, string>("NGƯỜI TRIỂN KHAI", "Mai Trung Nam"));
            dTag.TatCa.Add(new KeyValuePair<string, string>("NGƯỜI CHECK 1", "Lê Gia Vinh, Mai Trung Nam"));
            dTag.TatCa.Add(new KeyValuePair<string, string>("NGƯỜI CHECK 2", "Người Ngoài"));
            dTag.TatCa.Add(new KeyValuePair<string, string>("Người phụ trách", "Đỗ Văn Quang"));
            var dbTag = new List<NguoiTag> { new NguoiTag { Id = "m", Ten = "Mai Trung Nam" }, new NguoiTag { Id = "v", Ten = "Lê Gia Vinh" }, new NguoiTag { Id = "q", Ten = "Đỗ Văn Quang" } };
            Kiem("chuyen tag: trien khai + check, trung 1 lan, bo nguoi ngoai danh ba, bo phu trach",
                 string.Join(",", ChuyenTiep.NguoiCanTag(dTag, csTag, dbTag).Select(n => n.Id)), "m,v");
            var bTagCt = (OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(ChuyenTiep.BodyChuyen("[OHTA-受注] 岡田 · 11/11 07:41", "<p>x</p>", null, null,
                ChuyenTiep.NguoiCanTag(dTag, csTag, dbTag)));
            Kiem("chuyen tag: dong tag duoi dong dau + mentions", ((OhtaTool.Plugin.Core.JsonObject)bTagCt.Get("body")).GetString("content") + "|" + ((List<object>)bTagCt.Get("mentions")).Count,
                 "<p><b>[OHTA-受注] 岡田 · 11/11 07:41</b></p><p><at id=\"0\">Mai Trung Nam</at> <at id=\"1\">Lê Gia Vinh</at></p><p>x</p>|2");
            Kiem("chuyen: thu muc tep", ChuyenTiep.ThuMucTep("OHTA-受注", "C081105/5"), "Chuyển từ OHTA-受注/C081105_5");
            Kiem("tin mo dau khong the", ((OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(TraoDoi.BodyMoDau("<p>x</p>", null, "a"))).ContainsKey("attachments"), false);
            Kiem("tep: doc AttachmentFiles", string.Join(",", TraoDoi.DocTep(@"{""value"":[{""FileName"":""130819.pdf"",""ServerRelativeUrl"":""/sites/x/Lists/y/Attachments/2758/130819.pdf""},{""FileName"":""""}]}").Select(t => t.Ten + "=" + t.DuongDan)), "130819.pdf=/sites/x/Lists/y/Attachments/2758/130819.pdf");
            Kiem("tep: URL tai qua _api, ma hoa dau cach + nhay don", TraoDoi.UrlTaiTep("https://h/sites/x", "/sites/x/Lists/Tt c/Attachments/1/a'b.pdf"),
                 "https://h/sites/x/_api/web/GetFileByServerRelativePath(decodedurl='%2Fsites%2Fx%2FLists%2FTt%20c%2FAttachments%2F1%2Fa%27%27b.pdf')/$value");
            Kiem("tra loi: ma hoa HTML + xuong dong", TraoDoi.HtmlTraLoi(" Đã xong <1>\r\ndòng 2 "), "Đã xong &lt;1&gt;<br>dòng 2");
            Kiem("tra loi: body", ((OhtaTool.Plugin.Core.JsonObject)((OhtaTool.Plugin.Core.JsonObject)OhtaTool.Plugin.Core.JsonLite.Parse(TraoDoi.BodyTraLoi("hi"))).Get("body")).GetString("content"), "hi");
            var dTim = new DonTeams { Title = "130819" };
            dTim.TatCa.Add(new KeyValuePair<string, string>("Người triển khai", "Mai Trung Nam"));
            dTim.TatCa.Add(new KeyValuePair<string, string>("注意", "赤丸箇所、3種とも"));
            Kiem("tim: so don / ten khong dau / chu Nhat", BangDon.KhopTim(dTim, "0819") + "|" + BangDon.KhopTim(dTim, "trung NAM") + "|" + BangDon.KhopTim(dTim, "赤丸"), "True|True|True");
            Kiem("tim: moi tu phai co, rong = khop het", BangDon.KhopTim(dTim, "130819 vinh") + "|" + BangDon.KhopTim(dTim, "   "), "False|True");
            Kiem("tach nhan", string.Join("|", BangDon.TachNhan("切板, 曲げ,  ")), "切板|曲げ");
            var ct = SuaDon.DocNguoiCongTy(@"{""value"":[{""displayName"":""Lê Gia Vinh"",""userPrincipalName"":""vinh@oft.com"",""accountEnabled"":true},
  {""displayName"":""Người Mới"",""userPrincipalName"":""moi@oft.com""},{""displayName"":""Nghỉ việc"",""userPrincipalName"":""x@oft.com"",""accountEnabled"":false}]}");
            Kiem("danh ba cong ty: bo tai khoan da tat", string.Join(",", ct.Select(x => x.Key)), "Lê Gia Vinh,Người Mới");
            var siteNg = SuaDon.DocNguoiSite(@"{""value"":[{""Id"":40,""Title"":""Lê Gia Vinh"",""Email"":""VINH@oft.com""},{""Id"":7,""Email"":""""}]}");
            var gop = SuaDon.GopDanhBa(new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("99", "Khách Cũ"), new KeyValuePair<string, string>("40", "Lê Gia Vinh") }, ct, siteNg);
            Kiem("danh ba gop: co ma site / chua co ma / nguoi cu", string.Join(",", gop.Select(x => x.Key + "=" + x.Value)), "99=Khách Cũ,40=Lê Gia Vinh,upn:moi@oft.com=Người Mới");
            Kiem("nguoi chua co ma hop le khi kiem tra", SuaDon.KiemTra(C("Ng"), new List<string> { "40", "upn:moi@oft.com" }) == null && SuaDon.KiemTra(C("Ng"), new List<string> { "abc" }) != null, true);
            Kiem("loc nhan vien: co trong cot nguoi bat ky", SuaDon.CoNguoi(dN[0], cs, "12") + "|" + SuaDon.CoNguoi(dN[1], cs, "55") + "|" + SuaDon.CoNguoi(dN[1], cs, "12"), "True|True|False");
            Kiem("nguoi body: 1 nguoi bo trong = null",bN.ContainsKey("Ng1LookupId") && bN.Get("Ng1LookupId") == null, true);

            TestPhongAi();
            TestPhongKiemTra();
            TestPhongTrienKhai();
            TestAiCli();
            TestBaPhong();

            Console.WriteLine();
            Console.WriteLine("Tong: {0} OK, {1} FAIL", _ok, _fail);
            return _fail == 0 ? 0 : 1;
        }
    }
}
