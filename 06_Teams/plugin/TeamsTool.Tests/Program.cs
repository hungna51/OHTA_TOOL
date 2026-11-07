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

            Kiem("Danh sach nhom/kenh", PhanTichTeams.DocDanhSach(@"{""value"":[{""id"":""a"",""displayName"":""Xuong""},{""displayName"":""thieu id""}]}").Count, 1);
            Kiem("BoDau", PhanTichTeams.BoDau(" Hạn Hoàn Thành Đơn "), "han hoan thanh don");
            var t = PhanTichTeams.DocToken(@"{""token_type"":""Bearer"",""expires_in"":3599,""access_token"":""AT"",""refresh_token"":""RT""}");
            Kiem("DocToken", t.Access + "|" + t.Refresh + "|" + t.HetSau, "AT|RT|3599");
            var ma = PhanTichTeams.DocMa(PhanTichTeams.GhiMa(" abc-tenant \n", "xyz-client"));
            Kiem("teams.json ghi roi doc lai, bo khoang trang", ma.Tenant + "|" + ma.Client, "abc-tenant|xyz-client");
            var thieu = PhanTichTeams.DocMa(@"{""tenantId"":""t""}");
            Kiem("teams.json thieu clientId -> rong", thieu.Tenant + "|" + thieu.Client, "t|");

            Console.WriteLine();
            Console.WriteLine("Tong: {0} OK, {1} FAIL", _ok, _fail);
            return _fail == 0 ? 0 : 1;
        }
    }
}
