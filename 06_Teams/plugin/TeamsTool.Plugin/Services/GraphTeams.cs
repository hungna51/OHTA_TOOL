using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using OhtaTool.Plugin.Core;
using TeamsTool.Core;

namespace TeamsTool.Services
{
    /// <summary>Cài đặt tab Teams: HKCU\Software\OFT_Lisp\OhtaTool\Teams (riêng từng máy).</summary>
    public static class CaiDatTeams
    {
        private const string Khoa = @"Software\OFT_Lisp\OhtaTool\Teams";

        public static string Doc(string ten, string macDinh = "")
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Khoa))
                return k?.GetValue(ten) as string ?? macDinh;
        }

        public static void Ghi(string ten, string giaTri)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Khoa))
                k.SetValue(ten, giaTri ?? "");
        }

        public static string TenantId => Doc("tenant_id").Trim();
        public static string ClientId => Doc("client_id").Trim();

        public static string ThuMucDon => Doc("thu_muc",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OHTA_DonTeams"));

        /// <summary>Kênh đã chọn, mỗi dòng "nhomId|kenhId|Tên nhóm / Tên kênh".</summary>
        public static List<string[]> Kenh()
        {
            var kq = new List<string[]>();
            foreach (string dong in Doc("kenh").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = dong.Trim().Split(new[] { '|' }, 3);
                if (p.Length == 3) kq.Add(p);
            }
            return kq;
        }
    }

    /// <summary>
    /// Đăng nhập Microsoft 365 bằng mã thiết bị (device code) + gọi Microsoft Graph bằng HttpClient thẳng
    /// (không NuGet, như ADR 0006). Refresh token mã hoá DPAPI theo tài khoản Windows ở
    /// %LOCALAPPDATA%\OHTA_TOOL\teams\token.bin — không nằm trong thư mục OHTA (repo public, OneDrive).
    /// </summary>
    public static class GraphTeams
    {
        private const string Scope =
            "offline_access User.Read Team.ReadBasic.All Channel.ReadBasic.All ChannelMessage.Read.All ChannelMessage.Send Files.Read.All";
        private const string Graph = "https://graph.microsoft.com/v1.0";

        private static readonly HttpClient Http = TaoHttp();
        private static string _access;
        private static DateTime _hetHan = DateTime.MinValue;

        private static HttpClient TaoHttp()
        {
#if ZWCAD
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
            return new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        private static string FileToken => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "teams", "token.bin");

        private static string UrlDangNhap(string duoi) =>
            "https://login.microsoftonline.com/" + Uri.EscapeDataString(CaiDatTeams.TenantId) + "/oauth2/v2.0/" + duoi;

        public static bool DaCoDangNhap => File.Exists(FileToken);

        private static string DocRefresh()
        {
            try
            {
                return File.Exists(FileToken)
                    ? Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(FileToken), null, DataProtectionScope.CurrentUser))
                    : null;
            }
            catch { return null; }
        }

        private static void GhiRefresh(string refresh)
        {
            if (string.IsNullOrEmpty(refresh)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(FileToken));
            File.WriteAllBytes(FileToken, ProtectedData.Protect(Encoding.UTF8.GetBytes(refresh), null, DataProtectionScope.CurrentUser));
        }

        public static void DangXuat()
        {
            _access = null;
            try { File.Delete(FileToken); } catch { }
        }

        private static async Task<(bool Ok, string Json)> PostForm(string url, Dictionary<string, string> form, CancellationToken ct)
        {
            using (var content = new FormUrlEncodedContent(form))
            using (HttpResponseMessage res = await Http.PostAsync(url, content, ct))
                return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        }

        private static string LoiOAuth(string json)
        {
            var o = SafeParse(json);
            return o?.GetString("error_description") ?? o?.GetString("error") ?? json;
        }

        private static JsonObject SafeParse(string json)
        {
            try { return JsonLite.Parse(json) as JsonObject; } catch { return null; }
        }

        private static void KiemCaiDat()
        {
            if (CaiDatTeams.TenantId.Length == 0 || CaiDatTeams.ClientId.Length == 0)
                throw new InvalidOperationException("Chưa nhập Tenant ID / Client ID trong ⚙ Cài đặt.");
        }

        /// <summary>Bước 1: xin mã. Trả mã để người dùng nhập ở trang verification_uri.</summary>
        public static async Task<(string MaNguoiDung, string Url, string MaThietBi, int ChuKy)> BatDauDangNhap(CancellationToken ct)
        {
            KiemCaiDat();
            var (ok, json) = await PostForm(UrlDangNhap("devicecode"),
                new Dictionary<string, string> { ["client_id"] = CaiDatTeams.ClientId, ["scope"] = Scope }, ct);
            if (!ok) throw new InvalidOperationException(LoiOAuth(json));
            var o = (JsonObject)JsonLite.Parse(json);
            int.TryParse(o.Get("interval")?.ToString(), out int chuKy);
            return (o.GetString("user_code"), o.GetString("verification_uri"), o.GetString("device_code"), Math.Max(chuKy, 5));
        }

        /// <summary>Bước 2: chờ người dùng nhập mã xong (tối đa ~15 phút, huỷ bằng ct).</summary>
        public static async Task ChoDangNhap(string maThietBi, int chuKy, CancellationToken ct)
        {
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["client_id"] = CaiDatTeams.ClientId,
                ["device_code"] = maThietBi,
            };
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(chuKy), ct);
                var (ok, json) = await PostForm(UrlDangNhap("token"), form, ct);
                if (ok) { NhanToken(json); return; }
                string loi = SafeParse(json)?.GetString("error");
                if (loi == "authorization_pending") continue;
                if (loi == "slow_down") { chuKy += 5; continue; }
                throw new InvalidOperationException(LoiOAuth(json));
            }
        }

        private static void NhanToken(string json)
        {
            var t = PhanTichTeams.DocToken(json);
            _access = t.Access;
            _hetHan = DateTime.UtcNow.AddSeconds(Math.Max(t.HetSau - 120, 60));
            GhiRefresh(t.Refresh);
        }

        private static async Task<string> LayAccess(CancellationToken ct)
        {
            if (_access != null && DateTime.UtcNow < _hetHan) return _access;
            KiemCaiDat();
            string refresh = DocRefresh();
            if (refresh == null) throw new InvalidOperationException("Chưa đăng nhập Microsoft 365 — bấm Đăng nhập.");
            var (ok, json) = await PostForm(UrlDangNhap("token"), new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = CaiDatTeams.ClientId,
                ["refresh_token"] = refresh,
                ["scope"] = Scope,
            }, ct);
            if (!ok)
            {
                DangXuat();
                throw new InvalidOperationException("Phiên đăng nhập hết hạn, bấm Đăng nhập lại. (" + LoiOAuth(json) + ")");
            }
            NhanToken(json);
            return _access;
        }

        private static async Task<HttpResponseMessage> Gui(HttpMethod method, string url, string body, CancellationToken ct)
        {
            var req = new HttpRequestMessage(method, url.StartsWith("http") ? url : Graph + url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await LayAccess(ct));
            if (body != null) req.Content = new StringContent(body, new UTF8Encoding(false), "application/json");
            HttpResponseMessage res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode)
            {
                string text = await res.Content.ReadAsStringAsync();
                string loi = SafeParse(text) is JsonObject o && o.Get("error") is JsonObject e ? e.GetString("message") : text;
                res.Dispose();
                if (loi != null && loi.Length > 300) loi = loi.Substring(0, 300) + "…";
                throw new InvalidOperationException("Graph " + (int)res.StatusCode + ": " + loi);
            }
            return res;
        }

        private static async Task<string> GetJson(string url, CancellationToken ct)
        {
            using (HttpResponseMessage res = await Gui(HttpMethod.Get, url, null, ct))
                return await res.Content.ReadAsStringAsync();
        }

        /// <summary>(id, tên hiển thị) của người đang đăng nhập.</summary>
        public static async Task<(string Id, string Ten)> Toi(CancellationToken ct)
        {
            var o = (JsonObject)JsonLite.Parse(await GetJson("/me?$select=id,displayName", ct));
            return (o.GetString("id"), o.GetString("displayName"));
        }

        public static async Task<List<KeyValuePair<string, string>>> Nhom(CancellationToken ct) =>
            PhanTichTeams.DocDanhSach(await GetJson("/me/joinedTeams?$select=id,displayName", ct));

        public static async Task<List<KeyValuePair<string, string>>> KenhCuaNhom(string nhomId, CancellationToken ct) =>
            PhanTichTeams.DocDanhSach(await GetJson("/teams/" + nhomId + "/channels?$select=id,displayName", ct));

        // shortcut: chỉ đọc 50 tin gốc mới nhất mỗi kênh, bỏ file trong tin trả lời; cần hơn thì đi theo @odata.nextLink
        public static async Task<List<DonTeams>> DonCuaKenh(string nhomId, string kenhId, string tenKenh, CancellationToken ct) =>
            PhanTichTeams.DocDon(await GetJson("/teams/" + nhomId + "/channels/" + Uri.EscapeDataString(kenhId) + "/messages?$top=50", ct),
                                 nhomId, kenhId, tenKenh);

        /// <summary>Tải file đính kèm (link SharePoint) về đường dẫn đích qua /shares.</summary>
        public static async Task TaiFile(string urlSharePoint, string dich, CancellationToken ct)
        {
            using (HttpResponseMessage res = await Gui(HttpMethod.Get, "/shares/" + PhanTichTeams.MaShare(urlSharePoint) + "/driveItem/content", null, ct))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dich));
                string tam = dich + ".tai";
                using (Stream src = await res.Content.ReadAsStreamAsync())
                using (FileStream dst = File.Create(tam))
                    await src.CopyToAsync(dst);
                if (File.Exists(dich)) File.Delete(dich);
                File.Move(tam, dich);
            }
        }

        /// <summary>Trả lời vào luồng của tin nhắn đơn.</summary>
        public static async Task TraLoi(DonTeams d, string noiDung, CancellationToken ct)
        {
            var body = new JsonObject();
            var b = new JsonObject();
            b.Set("contentType", "text");
            b.Set("content", noiDung);
            body.Set("body", b);
            string url = "/teams/" + d.NhomId + "/channels/" + Uri.EscapeDataString(d.KenhId) + "/messages/" + d.Id + "/replies";
            using (await Gui(HttpMethod.Post, url, JsonLite.Serialize(body), ct)) { }
        }
    }
}
