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
            Kiem("loc nhan vien: co trong cot nguoi bat ky", SuaDon.CoNguoi(dN[0], cs, "12") + "|" + SuaDon.CoNguoi(dN[1], cs, "55") + "|" + SuaDon.CoNguoi(dN[1], cs, "12"), "True|True|False");
            Kiem("nguoi body: 1 nguoi bo trong = null",bN.ContainsKey("Ng1LookupId") && bN.Get("Ng1LookupId") == null, true);

            Console.WriteLine();
            Console.WriteLine("Tong: {0} OK, {1} FAIL", _ok, _fail);
            return _fail == 0 ? 0 : 1;
        }
    }
}
