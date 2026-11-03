using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>Một khung in (phần tử kq-tk của lisp): tid + hộp.</summary>
    public sealed class KhungTg
    {
        public int Tid;
        public Hop Bb;
        /// <summary>Entity sẽ dời theo khung (handle) — lisp tính từ gro--danh-sach-chi-tiet / khung.</summary>
        public List<string> Ents = new List<string>();
    }

    /// <summary>
    /// Mục 180 (GĐ 6f): chép <c>gro--tach-va-cham-khung-in</c> (mục 111 / 121) — lặp: tìm cặp khung in chồng
    /// nhau có diện tích giao LỚN NHẤT (bỏ cặp cả hai ở cột xếp phải), tấm nhỏ hơn dời theo hướng ngắn nhất
    /// trong 4 hướng mà tách khỏi MỌI khung (khe gap, <c>gro--tg-chon-huong</c>), không hướng nào sạch thì dời
    /// theo cạnh ngắn của HCN giao. Thuần, không gọi ZWCAD. Trả chuỗi lần dời (vị trí trong danh sách
    /// hiện tại, tid, dx, dy) đúng thứ tự để lisp cập nhật dữ liệu và lớp CAD dời entity.
    /// </summary>
    public static class TachVaCham
    {
        public sealed class LanDoi { public int ViTri, Tid; public double Dx, Dy; public List<string> Ents; }

        public sealed class KetQua
        {
            public List<LanDoi> Doi = new List<LanDoi>();
            public int ConChong;
            public List<KhungTg> Cuoi;
        }

        /// <summary>gro--bb-hcn-giao (giao phải rộng hơn 0.1 cả hai chiều).</summary>
        public static Hop HcnGiao(Hop a, Hop b)
        {
            if (a == null || b == null) return null;
            double x1 = Math.Max(a.X1, b.X1), y1 = Math.Max(a.Y1, b.Y1), x2 = Math.Min(a.X2, b.X2), y2 = Math.Min(a.Y2, b.Y2);
            return x1 + 0.1 < x2 && y1 + 0.1 < y2 ? new Hop(x1, y1, x2, y2) : null;
        }

        public static double DienTich(Hop b) => b == null ? 0.0 : Math.Max(0.0, b.X2 - b.X1) * Math.Max(0.0, b.Y2 - b.Y1);

        private static Hop Dich(Hop b, double dx, double dy) => new Hop(b.X1 + dx, b.Y1 + dy, b.X2 + dx, b.Y2 + dy);

        /// <summary>gro--tg-can-dich</summary>
        public static double CanDich(Hop m, Hop o, double hx, double hy, double gap)
        {
            double v;
            if (hx > 0.5) v = o.X2 + gap - m.X1;
            else if (hx < -0.5) v = m.X2 - (o.X1 - gap);
            else if (hy > 0.5) v = o.Y2 + gap - m.Y1;
            else v = m.Y2 - (o.Y1 - gap);
            return Math.Max(0.0, v);
        }

        private static readonly double[][] Huong = { new[] { 1.0, 0.0 }, new[] { -1.0, 0.0 }, new[] { 0.0, 1.0 }, new[] { 0.0, -1.0 } };

        /// <summary>gro--tg-chon-huong: (dx dy) ngắn nhất, hoặc null.</summary>
        public static double[] ChonHuong(KhungTg mov, KhungTg fix, IList<KhungTg> khungs, double gap)
        {
            Hop bbM = mov.Bb;
            double bestS = 0; double[] bestDir = null;
            foreach (double[] dir in Huong)
            {
                double s = CanDich(bbM, fix.Bb, dir[0], dir[1], gap);
                int lan = 0; bool ok = false;
                while (!ok && lan < 40)
                {
                    Hop bbm = Dich(bbM, dir[0] * s, dir[1] * s);
                    KhungTg o = null;
                    foreach (KhungTg k in khungs)
                        if (!ReferenceEquals(k, mov) && HcnGiao(bbm, k.Bb.No(gap - 0.01)) != null) { o = k; break; }
                    if (o != null) { s += Math.Max(0.01, CanDich(bbm, o.Bb, dir[0], dir[1], gap)); lan++; }
                    else ok = true;
                }
                if (ok && (bestDir == null || s < bestS)) { bestS = s; bestDir = dir; }
            }
            return bestDir == null ? null : new[] { bestDir[0] * bestS, bestDir[1] * bestS };
        }

        /// <summary>gro--tg-dem-chong</summary>
        public static int DemChong(IList<KhungTg> khungs, ISet<int> boQua)
        {
            int n = 0;
            for (int i = 0; i < khungs.Count; i++)
                for (int j = i + 1; j < khungs.Count; j++)
                {
                    KhungTg a = khungs[i], b = khungs[j];
                    if (!(boQua.Contains(a.Tid) && boQua.Contains(b.Tid)) && HcnGiao(a.Bb, b.Bb) != null) n++;
                }
            return n;
        }

        public static KetQua Chay(IList<KhungTg> khungs, ISet<int> boQua, double gap, int maxIter)
        {
            var kq = new KetQua();
            var cur = new List<KhungTg>(khungs);
            gap = gap > 0.0 ? gap : 50.0;
            maxIter = Math.Min(500, Math.Max(maxIter > 0 ? maxIter : 20, 10 * khungs.Count));
            int iter = 0;
            bool tiep = true;
            while (tiep && iter < maxIter && cur.Count > 1)
            {
                iter++;
                double maxArea = 0.0;
                KhungTg ka = null, kb = null; Hop hg = null;
                for (int i = 0; i < cur.Count - 1; i++)
                    for (int j = i + 1; j < cur.Count; j++)
                    {
                        KhungTg a = cur[i], b = cur[j];
                        if (boQua.Contains(a.Tid) && boQua.Contains(b.Tid)) continue;
                        Hop g = HcnGiao(a.Bb, b.Bb);
                        if (g == null) continue;
                        double ar = DienTich(g);
                        if (ar > maxArea) { maxArea = ar; ka = a; kb = b; hg = g; }
                    }
                if (ka == null) { tiep = false; break; }

                double areaA = DienTich(ka.Bb), areaB = DienTich(kb.Bb);
                double wI = hg.X2 - hg.X1, hI = hg.Y2 - hg.Y1;
                KhungTg fix, mov;
                if (boQua.Contains(ka.Tid)) { fix = ka; mov = kb; }
                else if (boQua.Contains(kb.Tid)) { fix = kb; mov = ka; }
                else if (areaA > areaB || (Math.Abs(areaA - areaB) <= 1e-4 && ka.Tid < kb.Tid)) { fix = ka; mov = kb; }
                else { fix = kb; mov = ka; }

                Hop bbM = mov.Bb, bbF = fix.Bb;
                double cxM = (bbM.X1 + bbM.X2) / 2.0, cyM = (bbM.Y1 + bbM.Y2) / 2.0;
                double cxF = (bbF.X1 + bbF.X2) / 2.0, cyF = (bbF.Y1 + bbF.Y2) / 2.0;
                double dx, dy;
                double[] h = ChonHuong(mov, fix, cur, gap);
                if (h != null) { dx = h[0]; dy = h[1]; }
                else if (wI < hI) { dy = 0.0; dx = cxM < cxF ? 0.0 - (wI + gap) : wI + gap; }
                else { dx = 0.0; dy = cyM < cyF ? 0.0 - (hI + gap) : hI + gap; }

                kq.Doi.Add(new LanDoi { ViTri = cur.IndexOf(mov), Tid = mov.Tid, Dx = dx, Dy = dy, Ents = mov.Ents });
                // moi khung cung tid -> CUNG mot phan tu moi (nhu mapcar cua lisp)
                var moi = new KhungTg { Tid = mov.Tid, Bb = Dich(bbM, dx, dy), Ents = mov.Ents };
                for (int i = 0; i < cur.Count; i++) if (cur[i].Tid == mov.Tid) cur[i] = moi;
            }
            kq.ConChong = DemChong(cur, boQua);
            kq.Cuoi = cur;
            return kq;
        }
    }
}
