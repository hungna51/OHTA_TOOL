using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiTool.Core;

namespace AiTool.Services
{
    /// <summary>
    /// Gọi thẳng POST /v1/messages của Claude API bằng HttpClient (ADR 0005: không kéo gói NuGet vào CAD).
    /// 429 / 5xx / 529 thử lại tối đa 2 lần (theo retry-after nếu có, không thì 2 s rồi 4 s).
    /// Trả body response kể cả khi là JSON lỗi để PhanHoiApi đọc message.
    /// </summary>
    public sealed class ClaudeHttp : IGuiYeuCau
    {
        private static readonly HttpClient Http = TaoHttp();
        private const int SoLanThuLai = 2;
        private readonly string _apiKey;

        public ClaudeHttp(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        private static HttpClient TaoHttp()
        {
#if ZWCAD
            // .NET Framework 4.8 mặc định có thể chưa bật TLS 1.2
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
            return new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
        }

        public async Task<string> Gui(string body, CancellationToken ct)
        {
            for (int lan = 0; ; lan++)
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, YeuCauApi.Url))
                {
                    req.Headers.Add("x-api-key", _apiKey);
                    req.Headers.Add("anthropic-version", YeuCauApi.PhienBanApi);
                    req.Headers.Add("anthropic-beta", YeuCauApi.BetaFallback);
                    req.Content = new StringContent(body, new UTF8Encoding(false), "application/json");

                    using (HttpResponseMessage res = await Http.SendAsync(req, ct).ConfigureAwait(false))
                    {
                        string text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                        int ma = (int)res.StatusCode;
                        bool thuLai = ma == 429 || ma >= 500;
                        if (!thuLai || lan >= SoLanThuLai)
                            return text;

                        TimeSpan cho = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2 << lan);
                        if (cho > TimeSpan.FromSeconds(60)) cho = TimeSpan.FromSeconds(60);
                        await Task.Delay(cho, ct).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
