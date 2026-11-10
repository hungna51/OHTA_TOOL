using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// <summary>
    /// Cài đặt tab Teams. Tenant/Client ID: file 06_Teams\data\teams.json (gitignore, admin gửi riêng từng máy).
    /// Tab Lists đã chọn lần trước: HKCU\Software\OFT_Lisp\OhtaTool\Teams (riêng từng máy).
    /// </summary>
    public static class CaiDatTeams
    {
        private const string Khoa = @"Software\OFT_Lisp\OhtaTool\Teams";

        /// <summary>Thư mục 06_Teams, TeamsOhtaTab gán khi tạo tab.</summary>
        public static string ThuMucModule = "";

        public static string FileMa => Path.Combine(ThuMucModule, "data", "teams.json");
        public static bool CoFileMa => File.Exists(FileMa);

        private static (string Tenant, string Client) DocFileMa()
        {
            try { return CoFileMa ? PhanTichTeams.DocMa(File.ReadAllText(FileMa)) : ("", ""); }
            catch (Exception) { return ("", ""); } // file hỏng = như chưa có, tab nhắc nhập lại
        }

        public static void GhiMa(string tenant, string client)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FileMa));
            File.WriteAllText(FileMa, PhanTichTeams.GhiMa(tenant, client), new UTF8Encoding(false));
        }

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

        public static string TenantId => DocFileMa().Tenant;
        public static string ClientId => DocFileMa().Client;

        /// <summary>Tab Lists đã chọn lần trước: nhómId, tên nhóm, kênhId, tên kênh, tên tab, siteId, listId, URL list (mỗi dòng một giá trị).</summary>
        public static string[] TabChon()
        {
            string[] p = Doc("tab_chon").Split('\n');
            return p.Length == 8 ? p : null;
        }

        public static void GhiTabChon(params string[] giaTri) => Ghi("tab_chon", string.Join("\n", giaTri));

        /// <summary>Thư mục chứa các thư mục đơn (⚙ của OhtaTeams) — CAD dùng để khớp bản vẽ ↔ đơn.</summary>
        public static string ThuMucGoc
        {
            get => Doc("thu_muc_goc");
            set => Ghi("thu_muc_goc", value);
        }

        /// <summary>Id đơn người dùng bấm 📌 Đang làm ("" = chưa chọn).</summary>
        public static string DonChon
        {
            get => Doc("don_chon");
            set => Ghi("don_chon", value);
        }

        public static string FileDangLam => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "teams", "dang_lam.json");

        /// <summary>Ghi dang_lam.json: ghi file tạm rồi thay, CAD không bao giờ đọc phải file ghi dở.</summary>
        public static void GhiDangLam(IEnumerable<DonTeams> tatCa)
        {
            string chon = DonChon;
            string json = PhanTichDangLam.Ghi(new DangLam
            {
                Luc = DateTime.Now,
                ThuMucGoc = ThuMucGoc,
                DonChon = chon,
                Don = PhanTichDangLam.ChonGhi(tatCa, chon),
            });
            string f = FileDangLam, tam = f + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            File.WriteAllText(tam, json, new UTF8Encoding(false));
            if (File.Exists(f)) File.Replace(tam, f, null);
            else File.Move(tam, f);
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
            "offline_access User.Read Team.ReadBasic.All Channel.ReadBasic.All TeamsTab.Read.All Sites.Read.All";
        private const string Graph = "https://graph.microsoft.com/v1.0";

        private static readonly HttpClient Http = TaoHttp();
        private static string _access;
        private static DateTime _hetHan = DateTime.MinValue;

        private static HttpClient TaoHttp()
        {
#if ZWCAD
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
            // gzip: danh sách ~9 MB JSON tải nhanh gần gấp đôi (đo 11/2026)
            var hd = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            return new HttpClient(hd) { Timeout = TimeSpan.FromSeconds(120) };
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
            XoaTam(); // dữ liệu đơn lưu tạm theo phiên, đăng xuất thì xoá
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
                throw new InvalidOperationException("Chưa có Tenant ID / Client ID — chép teams.json vào " + Path.GetDirectoryName(CaiDatTeams.FileMa) + " hoặc nhập trong ⚙.");
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

        public static async Task<List<TabTeams>> TabCuaKenh(string nhomId, string kenhId, CancellationToken ct) =>
            PhanTichTeams.DocTab(await GetJson("/teams/" + nhomId + "/channels/" + Uri.EscapeDataString(kenhId) + "/tabs", ct));

        /// <summary>URL danh sách của tab → (siteId, listId).</summary>
        public static async Task<(string SiteId, string ListId)> MoList(string listUrl, CancellationToken ct)
        {
            var (host, duong) = PhanTichTeams.TachSite(listUrl);
            string url = "/sites/" + host + (duong.Length > 0 ? ":" + string.Join("/", Array.ConvertAll(duong.Split('/'), Uri.EscapeDataString)) + ":" : "") + "?$select=id";
            string siteId = ((JsonObject)JsonLite.Parse(await GetJson(url, ct))).GetString("id");
            string listId = PhanTichTeams.TimListId(await GetJson("/sites/" + siteId + "/lists?$select=id,displayName,webUrl&$top=999", ct), listUrl);
            if (listId == null) throw new InvalidOperationException("Không tìm thấy danh sách " + listUrl);
            return (siteId, listId);
        }

        /// <summary>Tải hết các trang theo @odata.nextLink (tối đa soTrang trang).</summary>
        private static async Task<List<string>> TaiTrang(string url, int soTrang, CancellationToken ct)
        {
            var kq = new List<string>();
            while (url != null && kq.Count < soTrang)
            {
                string json = await GetJson(url, ct);
                kq.Add(json);
                url = PhanTichTeams.TrangSau(json);
            }
            return kq;
        }

        /// <summary>Id User Information List của site (danh sách hệ thống: gọi theo tên bị 404, phải lấy id qua $select=system). Lỗi = null.</summary>
        private static async Task<string> TimListNguoi(string siteId, CancellationToken ct)
        {
            try { return PhanTichTeams.TimListNguoi(await GetJson("/sites/" + siteId + "/lists?$select=id,webUrl,system&$top=999", ct)); }
            catch (InvalidOperationException) { return null; }
        }

        /// <summary>Tra tên các LookupId cần (song song tối đa 8). Trả 1 trang dạng {"value":[...]} cho DocNguoi; người đã xoá bị bỏ qua (hiện "#id").</summary>
        private static async Task<List<string>> TaiNguoi(string siteId, string uilId, List<string> ids, CancellationToken ct)
        {
            if (uilId == null || ids.Count == 0) return new List<string>();
            using (var gioiHan = new SemaphoreSlim(8))
            {
                var viec = ids.Select(async id =>
                {
                    await gioiHan.WaitAsync(ct);
                    try { return await GetJson("/sites/" + siteId + "/lists/" + uilId + "/items/" + Uri.EscapeDataString(id) + "?$select=id&$expand=fields($select=Title)", ct); }
                    catch (InvalidOperationException) { return null; }
                    finally { gioiHan.Release(); }
                }).ToList();
                string[] kq = await Task.WhenAll(viec);
                return new List<string> { "{\"value\":[" + string.Join(",", kq.Where(x => x != null)) + "]}" };
            }
        }

        /// <summary>
        /// Tải thô cả danh sách: cột, người, dòng chạy song song; $top=5000 = thường 1 lần gọi (đo: 9 s → ~4 s).
        /// Xong thì ghi bộ nhớ tạm để lần sau hiện ngay.
        /// </summary>
        // shortcut: tải hết các dòng rồi lọc trên máy (tối đa 10 trang x 5000); danh sách lớn hơn thì lọc $filter trên cột có index
        public static async Task<PhanTichTeams.ThoList> TaiList(string siteId, string listId, CancellationToken ct)
        {
            Task<string> cot = GetJson("/sites/" + siteId + "/lists/" + listId + "/columns", ct);
            Task<string> uil = TimListNguoi(siteId, ct);
            Task<List<string>> dong = TaiTrang("/sites/" + siteId + "/lists/" + listId + "/items?$expand=fields&$top=5000", 10, ct);
            var t = new PhanTichTeams.ThoList { Cot = await cot, Dong = await dong };
            List<string> ids = await Task.Run(() => PhanTichTeams.IdNguoiCan(t));
            t.Nguoi = await TaiNguoi(siteId, await uil, ids, ct);
            GhiTam(listId, t);
            return t;
        }

        // ---------- Bộ nhớ tạm: %LOCALAPPDATA%\OHTA_TOOL\teams\tam\<listId>\ (ngoài OneDrive / repo) ----------

        private static string ThuMucTam(string listId) => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "teams", "tam", listId); // listId là GUID

        private static void GhiTam(string listId, PhanTichTeams.ThoList t)
        {
            try
            {
                string d = ThuMucTam(listId);
                if (Directory.Exists(d)) Directory.Delete(d, true);
                Directory.CreateDirectory(d);
                var utf8 = new UTF8Encoding(false);
                File.WriteAllText(Path.Combine(d, "cot.json"), t.Cot, utf8);
                for (int i = 0; i < t.Nguoi.Count; i++) File.WriteAllText(Path.Combine(d, "nguoi" + i + ".json"), t.Nguoi[i], utf8);
                for (int i = 0; i < t.Dong.Count; i++) File.WriteAllText(Path.Combine(d, "dong" + i + ".json"), t.Dong[i], utf8);
            }
            catch (Exception) { } // bộ nhớ tạm hỏng chỉ làm lần sau tải chậm, không chặn
        }

        /// <summary>Danh sách tải lần trước (null = chưa có). Kèm giờ tải.</summary>
        public static (PhanTichTeams.ThoList Tho, DateTime Luc)? DocTam(string listId)
        {
            try
            {
                string d = ThuMucTam(listId), fCot = Path.Combine(d, "cot.json");
                if (!File.Exists(fCot)) return null;
                List<string> Doc(string tien) => Enumerable.Range(0, 50).Select(i => Path.Combine(d, tien + i + ".json"))
                                                          .TakeWhile(File.Exists).Select(File.ReadAllText).ToList();
                return (new PhanTichTeams.ThoList { Cot = File.ReadAllText(fCot), Nguoi = Doc("nguoi"), Dong = Doc("dong") }, File.GetLastWriteTime(fCot));
            }
            catch (Exception) { return null; }
        }

        public static void XoaTam()
        {
            try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "teams", "tam"), true); }
            catch (Exception) { }
        }
    }
}
