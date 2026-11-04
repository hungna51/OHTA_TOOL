using System.Collections.Generic;

namespace AiTool.Core
{
    /// <summary>
    /// Giá Claude API (USD / 1 triệu token, bảng giá 10/2026) để ước tính tiền mỗi lần kiểm.
    /// Ghi cache (TTL 5 phút) = 1.25 × giá vào. Giá đổi thì sửa ở đây.
    /// </summary>
    public static class BangGia
    {
        /// <summary>Các model chọn được trên tab; phần tử đầu là mặc định.</summary>
        public static readonly string[] Model = { "claude-sonnet-5-5", "claude-opus-5-5", "claude-haiku-5-5" };

        private static readonly Dictionary<string, (decimal Vao, decimal Ra, decimal DocCache)> Gia =
            new Dictionary<string, (decimal, decimal, decimal)>
            {
                ["claude-sonnet-5-5"] = (2.00m, 10.00m, 0.20m),
                ["claude-opus-5-5"] = (4.00m, 20.00m, 0.20m),
                ["claude-haiku-5-5"] = (0.10m, 0.50m, 0.01m),
            };

        public static decimal Tien(string model, SuDung s)
        {
            if (s == null || model == null || !Gia.TryGetValue(model, out var g))
                return 0m;
            decimal trieu = 1000000m;
            return s.Vao * g.Vao / trieu
                 + s.GhiCache * g.Vao * 1.25m / trieu
                 + s.DocCache * g.DocCache / trieu
                 + s.Ra * g.Ra / trieu;
        }
    }
}
