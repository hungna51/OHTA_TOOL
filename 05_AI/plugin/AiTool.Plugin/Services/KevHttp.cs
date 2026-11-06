using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiTool.Core;

namespace AiTool.Services
{
    /// <summary>
    /// Gọi POST /v1/systemone và GET /v1/rules của Kev Server (chạy local hoặc trên máy chủ mạng LAN).
    /// - Trên máy chủ có cài sẵn %LOCALAPPDATA%\OHTA_TOOL\kev: tự động bật ngầm ohta_kev_server.py nếu chưa chạy.
    /// - Trên các máy con trong mạng LAN: tự động trỏ tới máy chủ LAN (http://NGUYEN-A-HUNG:8008)
    ///   và tự động đồng bộ bộ file .md (05_AI\data\kev\*.md + huong_dan_ai.md) từ máy chủ về trước khi kiểm tra.
    /// </summary>
    public sealed class KevHttp : IGuiYeuCau
    {
        private static readonly HttpClient Http = TaoHttp();
        private static readonly SemaphoreSlim KhoaKhoiDong = new SemaphoreSlim(1, 1);
        private readonly string _baseUrl;
        private readonly string _endpoint;
        private readonly string _thuMucModule;

        public KevHttp(string baseUrl, string thuMucModule = null)
        {
            _thuMucModule = thuMucModule;
            string url = ChuanHoaBaseUrl(baseUrl);
            _baseUrl = url;
            if (url.EndsWith("/v1/systemone", StringComparison.OrdinalIgnoreCase))
            {
                _endpoint = url;
                _baseUrl = url.Substring(0, url.Length - "/v1/systemone".Length);
            }
            else
            {
                _endpoint = url + "/v1/systemone";
            }
        }

        public static bool CoMayChuLocalTrenMay()
        {
            try
            {
                string kevDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "kev");
                string pyExe = Path.Combine(kevDir, ".venv", "Scripts", "python.exe");
                return File.Exists(pyExe);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Nếu máy hiện tại là máy con trong LAN (chưa cài môi trường Kev local) mà URL đang để mặc định 127.0.0.1,
        /// tự động chuyển sang địa chỉ máy chủ LAN (ví dụ http://NGUYEN-A-HUNG:8008).
        /// </summary>
        public static string ChonUrlHieuLuc(string urlHienTai, string mayChuLan = null)
        {
            string macDinhLan = string.IsNullOrWhiteSpace(mayChuLan) ? "http://NGUYEN-A-HUNG:8008" : mayChuLan.Trim();
            if (string.IsNullOrWhiteSpace(urlHienTai))
                return CoMayChuLocalTrenMay() ? KevApi.UrlMacDinh : macDinhLan;

            string u = urlHienTai.Trim();
            if (LaDiaChiLocal(u) && !CoMayChuLocalTrenMay())
                return macDinhLan;
            return u;
        }

        private static string ChuanHoaBaseUrl(string baseUrl)
        {
            string url = string.IsNullOrWhiteSpace(baseUrl) ? KevApi.UrlMacDinh : baseUrl.Trim().TrimEnd('/');
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }
            return url;
        }

        public static bool LaDiaChiLocal(string url) =>
            !string.IsNullOrEmpty(url) &&
            (url.IndexOf("://127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0 ||
             url.IndexOf("://localhost", StringComparison.OrdinalIgnoreCase) >= 0);

        private static HttpClient TaoHttp()
        {
#if ZWCAD
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
            return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<string> Gui(string body, CancellationToken ct)
        {
            try
            {
                return await GuiMotLan(body, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (LaDiaChiLocal(_baseUrl) && !ct.IsCancellationRequested)
            {
                bool daBat = await DamBaoMayChuLocalChay(_baseUrl, _thuMucModule, ct).ConfigureAwait(false);
                if (!daBat) throw;
                return await GuiMotLan(body, ct).ConfigureAwait(false);
            }
        }

        private async Task<string> GuiMotLan(string body, CancellationToken ct)
        {
            using (var req = new HttpRequestMessage(HttpMethod.Post, _endpoint))
            {
                req.Content = new StringContent(body, new UTF8Encoding(false), "application/json");
                using (HttpResponseMessage res = await Http.SendAsync(req, ct).ConfigureAwait(false))
                {
                    return await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Tải bộ file luật .md mới nhất từ GET /v1/rules của Máy chủ Kev về máy con.
        /// Trả về số file .md đã đồng bộ (0 nếu đang chạy trên chính máy chủ hoặc server không hỗ trợ).
        /// </summary>
        public static async Task<int> DongBoLuatTuMayChu(string baseUrl, string thuMucModule, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(thuMucModule)) return 0;
            try
            {
                string url = ChuanHoaBaseUrl(baseUrl);
                if (url.EndsWith("/v1/systemone", StringComparison.OrdinalIgnoreCase))
                    url = url.Substring(0, url.Length - "/v1/systemone".Length);

                // Nếu đang trỏ thẳng 127.0.0.1 trên chính máy chủ thì file .md đã nằm sẵn tại chỗ
                if (LaDiaChiLocal(url)) return 0;

                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(2.5));
                    using (HttpResponseMessage res = await Http.GetAsync(url + "/v1/rules", cts.Token).ConfigureAwait(false))
                    {
                        if (!res.IsSuccessStatusCode) return 0;
                        string json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return CauHinhKev.ApDungGoiLuatJson(json, thuMucModule);
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static string TimThuMucModule(string goiY)
        {
            if (!string.IsNullOrWhiteSpace(goiY) && Directory.Exists(goiY))
                return goiY;
            try
            {
                string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(dllDir))
                {
                    DirectoryInfo d = new DirectoryInfo(dllDir);
                    if (d.Parent != null && d.Parent.Parent != null)
                        return d.Parent.Parent.FullName;
                }
            }
            catch (Exception) { }
            return null;
        }

        private static async Task<bool> DamBaoMayChuLocalChay(string baseUrl, string thuMucModule, CancellationToken ct)
        {
            await KhoaKhoiDong.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var (ok, _) = await ThuKetNoiNhanh(baseUrl).ConfigureAwait(false);
                if (ok) return true;

                string kevDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "kev");
                string pyExe = Path.Combine(kevDir, ".venv", "Scripts", "python.exe");
                if (!File.Exists(pyExe)) return false;

                int port = 8008;
                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri u) && u.Port > 0)
                    port = u.Port;

                string modDir = TimThuMucModule(thuMucModule);
                string serverScript = !string.IsNullOrEmpty(modDir)
                    ? Path.Combine(modDir, "kev", "ohta_kev_server.py")
                    : null;
                string args = (!string.IsNullOrEmpty(serverScript) && File.Exists(serverScript))
                    ? ("\"" + serverScript + "\" --run jaredpalmer/kev-0.8b --host 0.0.0.0 --port " + port)
                    : ("-m kev.serve --run jaredpalmer/kev-0.8b --host 0.0.0.0 --port " + port);

                var psi = new ProcessStartInfo
                {
                    FileName = pyExe,
                    Arguments = args,
                    WorkingDirectory = kevDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                psi.EnvironmentVariables["PYTHONUTF8"] = "1";
                psi.EnvironmentVariables["PYTHONPATH"] = kevDir;
                psi.EnvironmentVariables["HF_HUB_OFFLINE"] = "1";
                psi.EnvironmentVariables["HF_HUB_DISABLE_SYMLINKS_WARNING"] = "1";
                psi.EnvironmentVariables["KEV_DTYPE"] = "fp16";
                psi.EnvironmentVariables["KEV_CUDA_GRAPHS"] = "1";
                psi.EnvironmentVariables["KEV_FUSED"] = "0";
                psi.EnvironmentVariables["KEV_TEMPERATURE"] = "1.0";
                if (!string.IsNullOrEmpty(modDir))
                    psi.EnvironmentVariables["OHTA_AI_DATA_DIR"] = Path.Combine(modDir, "data");

                Process.Start(psi);

                for (int i = 0; i < 45; i++)
                {
                    await Task.Delay(500, ct).ConfigureAwait(false);
                    var (ready, _) = await ThuKetNoiNhanh(baseUrl).ConfigureAwait(false);
                    if (ready) return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                KhoaKhoiDong.Release();
            }
        }

        private static async Task<(bool Ok, string ThongTin)> ThuKetNoiNhanh(string baseUrl)
        {
            try
            {
                string url = ChuanHoaBaseUrl(baseUrl);
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.5)))
                using (HttpResponseMessage res = await Http.GetAsync(url + "/v1/models", cts.Token).ConfigureAwait(false))
                {
                    string txt = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!res.IsSuccessStatusCode)
                        return (false, "HTTP " + (int)res.StatusCode);
                    return (true, txt);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.GetBaseException().Message);
            }
        }

        public static async Task<(bool Ok, string ThongTin)> KiemTraMayChu(string baseUrl, string thuMucModule = null)
        {
            string url = ChuanHoaBaseUrl(baseUrl);
            var kq = await ThuKetNoiNhanh(url).ConfigureAwait(false);
            if (!kq.Ok && LaDiaChiLocal(url))
            {
                bool daBat = await DamBaoMayChuLocalChay(url, thuMucModule, CancellationToken.None).ConfigureAwait(false);
                if (daBat)
                    return await ThuKetNoiNhanh(url).ConfigureAwait(false);
            }
            return kq;
        }
    }
}
