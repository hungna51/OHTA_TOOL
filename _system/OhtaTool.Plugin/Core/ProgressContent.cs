using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Nội dung request gửi từng đoạn 64 KB, báo số byte đã gửi (thanh tiến trình gửi báo lỗi) và dừng được giữa chừng.
    /// </summary>
    public sealed class ProgressContent : HttpContent
    {
        private const int Chunk = 64 * 1024;
        private readonly byte[] _data;
        private readonly Action<long, long> _progress;
        private readonly CancellationToken _ct;

        public ProgressContent(byte[] data, string mediaType, Action<long, long> progress, CancellationToken ct = default)
        {
            _data = data;
            _progress = progress;
            _ct = ct;
            Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            for (int off = 0; off < _data.Length; off += Chunk)
            {
                _ct.ThrowIfCancellationRequested();
                int len = Math.Min(Chunk, _data.Length - off);
                await stream.WriteAsync(_data, off, len, _ct).ConfigureAwait(false);
                _progress?.Invoke(off + len, _data.Length);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _data.Length;
            return true;
        }
    }
}
