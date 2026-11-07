using System;
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

        private const string TinMau = @"{
 ""@odata.nextLink"": ""https://graph.microsoft.com/v1.0/next"",
 ""value"": [
  { ""id"": ""1700000000001"", ""messageType"": ""message"", ""deletedDateTime"": null,
    ""createdDateTime"": ""2026-11-07T01:30:00Z"",
    ""from"": { ""user"": { ""id"": ""u-sep"", ""displayName"": ""Sato"" } },
    ""body"": { ""contentType"": ""html"", ""content"": ""<p>Đơn mới &amp; gấp</p><p><at id=\""0\"">Hung</at> làm giúp</p>"" },
    ""attachments"": [
      { ""contentType"": ""reference"", ""name"": ""A-101.DWG"", ""contentUrl"": ""https://oft.sharepoint.com/sites/x/A-101.DWG"" },
      { ""contentType"": ""reference"", ""name"": ""anh.png"", ""contentUrl"": ""https://oft.sharepoint.com/sites/x/anh.png"" },
      { ""contentType"": ""reference"", ""name"": ""bang.xlsx"", ""contentUrl"": ""https://oft.sharepoint.com/sites/x/bang.xlsx"" }
    ],
    ""mentions"": [ { ""id"": 0, ""mentionText"": ""Hung"", ""mentioned"": { ""user"": { ""id"": ""U-HUNG"", ""displayName"": ""Hung"" } } } ] },
  { ""id"": ""2"", ""messageType"": ""message"", ""deletedDateTime"": null, ""createdDateTime"": ""2026-11-07T02:00:00Z"",
    ""from"": { ""user"": { ""id"": ""u2"", ""displayName"": ""Ken"" } }, ""body"": { ""content"": ""chao"" }, ""attachments"": [], ""mentions"": [] },
  { ""id"": ""3"", ""messageType"": ""message"", ""deletedDateTime"": ""2026-11-07T03:00:00Z"", ""createdDateTime"": ""2026-11-07T02:00:00Z"",
    ""from"": null, ""body"": { ""content"": """" },
    ""attachments"": [ { ""contentType"": ""reference"", ""name"": ""x.pdf"", ""contentUrl"": ""u"" } ], ""mentions"": [] },
  { ""id"": ""4"", ""messageType"": ""systemEventMessage"", ""deletedDateTime"": null, ""createdDateTime"": ""2026-11-07T02:00:00Z"",
    ""from"": null, ""body"": { ""content"": """" },
    ""attachments"": [ { ""contentType"": ""reference"", ""name"": ""y.pdf"", ""contentUrl"": ""u"" } ], ""mentions"": [] },
  { ""id"": ""5"", ""messageType"": ""message"", ""deletedDateTime"": null, ""createdDateTime"": ""2026-11-07T04:00:00Z"",
    ""from"": { ""application"": { ""displayName"": ""Bot"" } }, ""body"": { ""content"": ""don"" },
    ""attachments"": [ { ""contentType"": ""reference"", ""name"": ""z.pdf"", ""contentUrl"": ""u5"" } ], ""mentions"": [] }
 ]
}";

        private static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var don = PhanTichTeams.DocDon(TinMau, "T1", "C1", "Nhom / Don hang");
            Kiem("Chi lay tin co file don (bo chat, da xoa, he thong)", don.Count, 2);
            var d = don[0];
            Kiem("Id + kenh giu nguyen", d.Id + "|" + d.NhomId + "|" + d.KenhId, "1700000000001|T1|C1");
            Kiem("Nguoi gui", d.NguoiGui, "Sato");
            Kiem("Bo file khong phai don (png)", string.Join(",", d.Files.ConvertAll(f => f.Ten)), "A-101.DWG,bang.xlsx");
            Kiem("Noi dung bo HTML, giai ma &amp;", d.NoiDung, "Đơn mới & gấp\nHung làm giúp");
            Kiem("Tag nguoi phu trach", string.Join(",", d.TagTen), "Hung");
            Kiem("CoTag khong phan biet hoa thuong", d.CoTag("u-hung"), true);
            Kiem("CoTag nguoi khac = false", d.CoTag("u-sep"), false);
            Kiem("Gio UTC -> gio may", d.Luc, new DateTime(2026, 11, 7, 1, 30, 0, DateTimeKind.Utc).ToLocalTime());
            Kiem("Tin tu app (khong co user) -> nguoi gui ?", don[1].NguoiGui, "?");

            Kiem("Danh sach nhom/kenh", PhanTichTeams.DocDanhSach(@"{""value"":[{""id"":""a"",""displayName"":""Xuong""},{""displayName"":""thieu id""}]}").Count, 1);
            Kiem("LaFileDon .DXF", PhanTichTeams.LaFileDon("b.DXF"), true);
            Kiem("LaFileDon .docx", PhanTichTeams.LaFileDon("b.docx"), false);
            // Vi du chinh thuc cua Graph: https://onedrive.live.com/redir?resid=1231244193912!12&authKey=1201919!12921!1
            Kiem("MaShare dung base64url khong '='", PhanTichTeams.MaShare("https://onedrive.live.com/redir?resid=1231244193912!12&authKey=1201919!12921!1"),
                 "u!aHR0cHM6Ly9vbmVkcml2ZS5saXZlLmNvbS9yZWRpcj9yZXNpZD0xMjMxMjQ0MTkzOTEyITEyJmF1dGhLZXk9MTIwMTkxOSExMjkyMSEx");
            Kiem("TenAnToan bo ky tu cam", PhanTichTeams.TenAnToan("Nhom / Don: \"A\"?"), "Nhom _ Don_ _A__");
            Kiem("TenAnToan rong", PhanTichTeams.TenAnToan("  "), "_");
            var t = PhanTichTeams.DocToken(@"{""token_type"":""Bearer"",""expires_in"":3599,""access_token"":""AT"",""refresh_token"":""RT""}");
            Kiem("DocToken", t.Access + "|" + t.Refresh + "|" + t.HetSau, "AT|RT|3599");

            Console.WriteLine();
            Console.WriteLine("Tong: {0} OK, {1} FAIL", _ok, _fail);
            return _fail == 0 ? 0 : 1;
        }
    }
}
