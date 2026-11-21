using System;
using System.Collections.Generic;
using System.Linq;

namespace CsvTool.Core
{
    /// <summary>Tính hình cloud chữ nhật bao các vị trí lỗi; cloud chồng nhau được gộp thành một.</summary>
    public static class CloudLoi
    {
        private const double BulgeRaNgoai = 0.5;   // đi ngược chiều kim đồng hồ, bulge dương cong ra ngoài

        /// <summary>Mỗi cloud là danh sách đỉnh + bulge của 1 polyline kín. Khoảng cách và cỡ cung theo cỡ khung.</summary>
        public static List<List<(double X, double Y, double Bulge)>> Tao(
            IEnumerable<(double MinX, double MinY, double MaxX, double MaxY)> viTri, double coKhung)
        {
            double le = Math.Max(1.0, coKhung * 0.012);
            var hop = viTri.Select(h => (MinX: h.MinX - le, MinY: h.MinY - le, MaxX: h.MaxX + le, MaxY: h.MaxY + le)).ToList();
            for (bool coGop = true; coGop;)
            {
                coGop = false;
                for (int i = 0; i < hop.Count && !coGop; i++)
                    for (int j = i + 1; j < hop.Count; j++)
                    {
                        var a = hop[i];
                        var b = hop[j];
                        if (a.MaxX < b.MinX || b.MaxX < a.MinX || a.MaxY < b.MinY || b.MaxY < a.MinY) continue;
                        hop[i] = (Math.Min(a.MinX, b.MinX), Math.Min(a.MinY, b.MinY), Math.Max(a.MaxX, b.MaxX), Math.Max(a.MaxY, b.MaxY));
                        hop.RemoveAt(j);
                        coGop = true;
                        break;
                    }
            }
            return hop.Select(h => Vien(h.MinX, h.MinY, h.MaxX, h.MaxY, le * 1.5)).ToList();
        }

        private static List<(double X, double Y, double Bulge)> Vien(double x0, double y0, double x1, double y1, double cung)
        {
            var goc = new[] { (X: x0, Y: y0), (X: x1, Y: y0), (X: x1, Y: y1), (X: x0, Y: y1) };
            var kq = new List<(double, double, double)>();
            for (int c = 0; c < 4; c++)
            {
                var p = goc[c];
                var q = goc[(c + 1) % 4];
                double dai = Math.Abs(q.X - p.X) + Math.Abs(q.Y - p.Y);
                int n = Math.Max(2, (int)Math.Round(dai / cung));
                for (int i = 0; i < n; i++)
                    kq.Add((p.X + (q.X - p.X) * i / n, p.Y + (q.Y - p.Y) * i / n, BulgeRaNgoai));
            }
            return kq;
        }
    }
}
