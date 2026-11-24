using System;
using System.Collections.Generic;
using System.Linq;

namespace CsvTool.Core
{
    /// <summary>Vị trí đặt cụm F-name trong file A: góc dưới-trái (X, Y) và chiều cao cả cụm.</summary>
    public sealed class ChoFname { public double X, Y, Cao, TiLe; }

    /// <summary>
    /// Tìm chỗ đặt cụm F-name (hộp chữ nhật nằm ngang) nằm trọn trong phần thịt tấm: trong biên ngoài, không chạm lỗ.
    /// Được đè nét ケガキ / スリット vì không phải nét cắt. Chiều cao cụm bắt đầu = Y / 10, không vừa thì giảm 1/2, tối đa 5 lần.
    /// Trượt từ trên xuống, mỗi hàng trái sang phải (như GRO đặt mã khắc).
    /// </summary>
    public static class FnameTrongTam
    {
        public const int SoLanThu = 5;
        private const int ToiDaViTri = 40000;   // giới hạn số vị trí thử mỗi cỡ chữ, để tấm lớn chữ nhỏ không treo CAD

        /// <summary>rong0 × cao0: hộp cụm F-name hiện tại; y: cạnh ngắn của tấm (Y trong CSV). Không có chỗ sau 5 lần → null.</summary>
        public static ChoFname TimCho(IList<(double X, double Y)> ngoai, IList<List<(double X, double Y)>> lo, double rong0, double cao0, double y)
        {
            if (ngoai == null || ngoai.Count < 3 || rong0 <= 0 || cao0 <= 0 || y <= 0) return null;
            var canh = Canh(ngoai);
            var canhLo = (lo ?? new List<List<(double X, double Y)>>()).Where(v => v.Count >= 3).Select(v => (Diem: v, Canh: Canh(v))).ToList();
            double minX = ngoai.Min(p => p.X), maxX = ngoai.Max(p => p.X), minY = ngoai.Min(p => p.Y), maxY = ngoai.Max(p => p.Y);
            double cao = y / 10;
            for (int lan = 0; lan < SoLanThu; lan++, cao /= 2)
            {
                double rong = rong0 * cao / cao0, le = 0.1 * cao;
                double buoc = cao / 4;
                double soViTri = Math.Max(1, (maxX - minX) / buoc) * Math.Max(1, (maxY - minY) / buoc);
                if (soViTri > ToiDaViTri) buoc *= Math.Sqrt(soViTri / ToiDaViTri);
                // hộp thử nới thêm le mỗi phía, bắt đầu cách mép 1.5 le để hộp nới không chạm biên
                for (double yb = maxY - 1.5 * le - cao; yb >= minY + 1.5 * le - 1e-9; yb -= buoc)
                    for (double x = minX + 1.5 * le; x + rong <= maxX - 1.5 * le + 1e-9; x += buoc)
                        if (TrongThit(x - le, yb - le, x + rong + le, yb + cao + le, ngoai, canh, canhLo))
                            return new ChoFname { X = x, Y = yb, Cao = cao, TiLe = cao / cao0 };
            }
            return null;
        }

        private static List<(double X1, double Y1, double X2, double Y2)> Canh(IList<(double X, double Y)> v)
        {
            var kq = new List<(double, double, double, double)>();
            for (int i = 0; i < v.Count; i++)
            {
                var a = v[i]; var b = v[(i + 1) % v.Count];
                if (a.X != b.X || a.Y != b.Y) kq.Add((a.X, a.Y, b.X, b.Y));
            }
            return kq;
        }

        private static bool TrongThit(double x1, double y1, double x2, double y2, IList<(double X, double Y)> ngoai,
                                      List<(double X1, double Y1, double X2, double Y2)> canh,
                                      List<(List<(double X, double Y)> Diem, List<(double X1, double Y1, double X2, double Y2)> Canh)> lo)
        {
            var goc = new[] { (x1, y1), (x2, y1), (x2, y2), (x1, y2) };
            if (!goc.All(p => TrongDaGiac(p, ngoai))) return false;
            if (canh.Any(c => CatHop(c, x1, y1, x2, y2))) return false;
            foreach (var l in lo)
            {
                if (l.Canh.Any(c => CatHop(c, x1, y1, x2, y2))) return false;
                var p0 = l.Diem[0];
                if (p0.X > x1 && p0.X < x2 && p0.Y > y1 && p0.Y < y2) return false;   // lỗ nằm gọn trong hộp
                if (TrongDaGiac(goc[0], l.Diem)) return false;                          // hộp nằm gọn trong lỗ
            }
            return true;
        }

        public static bool TrongDaGiac((double X, double Y) p, IList<(double X, double Y)> v)
        {
            bool trong = false;
            for (int i = 0, j = v.Count - 1; i < v.Count; j = i++)
                if ((v[i].Y > p.Y) != (v[j].Y > p.Y) && p.X < (v[j].X - v[i].X) * (p.Y - v[i].Y) / (v[j].Y - v[i].Y) + v[i].X)
                    trong = !trong;
            return trong;
        }

        /// <summary>Đoạn thẳng cắt hoặc nằm trong hộp (Liang-Barsky).</summary>
        private static bool CatHop((double X1, double Y1, double X2, double Y2) c, double x1, double y1, double x2, double y2)
        {
            if (Math.Max(c.X1, c.X2) < x1 || Math.Min(c.X1, c.X2) > x2 || Math.Max(c.Y1, c.Y2) < y1 || Math.Min(c.Y1, c.Y2) > y2) return false;
            double dx = c.X2 - c.X1, dy = c.Y2 - c.Y1, t0 = 0, t1 = 1;
            foreach (var (p, q) in new[] { (-dx, c.X1 - x1), (dx, x2 - c.X1), (-dy, c.Y1 - y1), (dy, y2 - c.Y1) })
            {
                if (Math.Abs(p) < 1e-12) { if (q < 0) return false; continue; }
                double r = q / p;
                if (p < 0) { if (r > t1) return false; if (r > t0) t0 = r; }
                else { if (r < t0) return false; if (r < t1) t1 = r; }
            }
            return true;
        }
    }
}
