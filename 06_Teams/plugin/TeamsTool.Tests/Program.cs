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

            Console.WriteLine();
            Console.WriteLine("Tong: {0} OK, {1} FAIL", _ok, _fail);
            return _fail == 0 ? 0 : 1;
        }
    }
}
