using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiTool.Core;

namespace AiTool.Services
{
    /// <summary>
    /// API Ling (inclusionAI / Ant, giao thức tương thích OpenAI) cho màn hình chat của tab AI.
    /// Key lưu ở data\ling.key; tên model đổi được trong ⚙ (registry ling_model).
    /// </summary>
    public static class LingHttp
    {
        public const string UrlMacDinh = "https://api.tbox.cn/api/llm/v1/chat/completions";
        public const string ModelMacDinh = "Ling-3.2-flash";

        // Timeout vô hạn: streaming có thể dài; huỷ bằng CancellationToken (nút Dừng)
        private static readonly HttpClient Http = TaoHttp();

        private static HttpClient TaoHttp()
        {
#if ZWCAD
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
            return new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        }

        private static HttpRequestMessage TaoYeuCau(string apiKey, string body)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, UrlMacDinh);
            // TryAdd: Headers.Add ném lỗi chứa nguyên giá trị key
            if (!req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + (apiKey ?? "").Trim()))
            {
                req.Dispose();
                throw new InvalidOperationException("Key Ling có ký tự không hợp lệ");
            }
            req.Content = new StringContent(body, new UTF8Encoding(false), "application/json");
            return req;
        }

        private static async Task<string> LoiHttp(HttpResponseMessage res)
        {
            string text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (text.Length > 200) text = text.Substring(0, 200) + "…";
            return "HTTP " + (int)res.StatusCode + ": " + text;
        }

        /// <summary>Gửi một câu "hi" ngắn để xem key + model có dùng được không.</summary>
        public static async Task<(bool Ok, string ThongTin)> KiemTraKey(string apiKey, string model)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return (false, "chưa có key");
            string body = ChatLing.TaoBody(model, "", new[] { new TinChat { Vai = "user", NoiDung = "hi" } }, stream: false);
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                using (HttpRequestMessage req = TaoYeuCau(apiKey, body))
                using (HttpResponseMessage res = await Http.SendAsync(req, cts.Token).ConfigureAwait(false))
                    return res.IsSuccessStatusCode ? (true, model) : (false, await LoiHttp(res).ConfigureAwait(false));
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        /// <summary>
        /// Gửi body (stream=true), mỗi mẩu chữ / suy nghĩ mới gọi moiPhan(chu, suyNghi) trên luồng nền.
        /// Lỗi HTTP ném InvalidOperationException có mã + nội dung lỗi.
        /// </summary>
        public static async Task ChatStream(string apiKey, string body, Action<string, string> moiPhan, CancellationToken ct)
        {
            using (HttpRequestMessage req = TaoYeuCau(apiKey, body))
            using (HttpResponseMessage res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                if (!res.IsSuccessStatusCode)
                    throw new InvalidOperationException(await LoiHttp(res).ConfigureAwait(false));
                using (Stream s = await res.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var r = new StreamReader(s, Encoding.UTF8))
                using (ct.Register(() => r.Dispose())) // ReadLineAsync không nhận token → đóng stream để thoát
                {
                    string dong;
                    while ((dong = await r.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        ct.ThrowIfCancellationRequested();
                        var (chu, suyNghi, xong) = ChatLing.DocDongSse(dong);
                        if (xong) return;
                        if (chu != null || suyNghi != null) moiPhan(chu, suyNghi);
                    }
                }
            }
        }
    }
}
