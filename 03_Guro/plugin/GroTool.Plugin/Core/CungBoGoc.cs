using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 218 (GĐ 3d, bước "cungbogoc"): chép lisp mục 120 (gro--cung-gan-text-p + gro--cung-bo-goc-p).
    /// Cung biên dạng gần text số mà nối TIẾP TUYẾN với nét khác là cung bo góc, không phải cung dim góc.
    /// Lisp so mỗi cung với mọi đầu nét; ở đây đầu nét nằm trong lưới ô 1 mm.
    /// </summary>
    public static class CungBoGoc
    {
        public sealed class Cung
        {
            public string Id;
            public double Cx, Cy, R, A0, A1;
        }

        /// <summary>Một đầu nét (điểm đầu hoặc cuối của đường cong) + đạo hàm tại tham số của điểm đó (null = không lấy được).</summary>
        public sealed class DauNet
        {
            public string Id;
            public double X, Y;
            public double? Dx, Dy;
        }

        /// <summary>Text số: điểm + ngưỡng max(65, 8h).</summary>
        public sealed class TextSo
        {
            public double X, Y, Nguong;
        }

        /// <summary>Trả Id các cung bị loại (giữ thứ tự cung đầu vào).</summary>
        public static List<string> Chay(IList<Cung> cungs, IList<DauNet> daus, IList<TextSo> texts)
        {
            var luoi = new Dictionary<long, List<DauNet>>();
            foreach (DauNet d in daus)
            {
                long k = Khoa((long)Math.Floor(d.X), (long)Math.Floor(d.Y));
                if (!luoi.TryGetValue(k, out var ds)) luoi[k] = ds = new List<DauNet>();
                ds.Add(d);
            }
            var kq = new List<string>();
            foreach (Cung a in cungs)
                if (GanText(a, texts) && BoGoc(a, luoi)) kq.Add(a.Id);
            return kq;
        }

        /// <summary>gro--cung-gan-text-p: |d(text, tâm) - r| ≤ ngưỡng.</summary>
        public static bool GanText(Cung a, IList<TextSo> texts)
        {
            foreach (TextSo t in texts)
                if (Math.Abs(Math.Sqrt((t.X - a.Cx) * (t.X - a.Cx) + (t.Y - a.Cy) * (t.Y - a.Cy)) - a.R) <= t.Nguong) return true;
            return false;
        }

        /// <summary>gro--cung-bo-goc-p: một đầu cung trùng (&lt; 0.5) đầu nét khác mà hướng nét ở đó gần tiếp tuyến cung (|sin| &lt; 0.17).</summary>
        public static bool BoGoc(Cung a, Dictionary<long, List<DauNet>> luoi)
        {
            foreach (double goc in new[] { a.A0, a.A1 })
            {
                double px = a.Cx + a.R * Math.Cos(goc), py = a.Cy + a.R * Math.Sin(goc), tt = goc + Math.PI / 2.0;
                long ix = (long)Math.Floor(px), iy = (long)Math.Floor(py);
                for (long gx = ix - 1; gx <= ix + 1; gx++)
                    for (long gy = iy - 1; gy <= iy + 1; gy++)
                    {
                        if (!luoi.TryGetValue(Khoa(gx, gy), out var ds)) continue;
                        foreach (DauNet q in ds)
                        {
                            if (q.Id == a.Id || q.Dx == null || q.Dy == null) continue;
                            if (Math.Sqrt((q.X - px) * (q.X - px) + (q.Y - py) * (q.Y - py)) >= 0.5) continue;
                            double dx = q.Dx.Value, dy = q.Dy.Value;
                            if (Math.Abs(dx) + Math.Abs(dy) <= 1e-9) continue;
                            if (Math.Abs(Math.Sin(Math.Atan2(dy, dx) - tt)) < 0.17) return true;
                        }
                    }
            }
            return false;
        }

        private static long Khoa(long x, long y) => (x << 32) ^ (y & 0xFFFFFFFFL);
    }
}
