using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>Một LINE trong danh sách lines-cont của lisp (đầu mút group 10 / 11, chỉ dùng X Y).</summary>
    public sealed class DoanThang
    {
        public string Handle;
        public double X1, Y1, X2, Y2;
        public DoanThang(string h, double x1, double y1, double x2, double y2) { Handle = h; X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; }
    }

    /// <summary>Một chữ số đo (phần tử texts-lin): (ename v pt rot h bb).</summary>
    public sealed class TextDim
    {
        public string Handle;
        public double? V;
        public double Px, Py, Rot, H;
        /// <summary>null = lisp bb nil (gro--bbox-scale-15 khi đó lấy hộp 20x15 quanh gốc toạ độ).</summary>
        public Hop Bb;
    }

    /// <summary>Kết quả một chữ số: (best-mem cand-ticks p-mid ray-dir) như lisp.</summary>
    public sealed class KqDimDai
    {
        public List<DoanThang> Mem = new List<DoanThang>();
        public List<DoanThang> Ticks = new List<DoanThang>();
        public double PmX, PmY, RdX, RdY;
    }

    /// <summary>
    /// Mục 174 (GĐ 5c-a): chép bước 3 "DONG GOI LINEAR DIM UNITS" của <c>gro--thu-thap-dim-units</c>
    /// — với mỗi chữ số đo: tìm đường dim chính (LINE song song chữ, chạm hộp chữ nới 1.5, dài ≈ V,
    /// hoặc chuỗi LINE thẳng hàng mục 169) gần tâm chữ nhất, rồi gom chân dim vuông góc ở 2 đầu và
    /// hướng chiếu tia. Thuần, không gọi ZWCAD. Thứ tự danh sách giữ y như lisp (cons = đảo ngược).
    /// </summary>
    public static class DimDai
    {
        /// <summary>(angle a b) của lisp: atan2 đưa về [0, 2π).</summary>
        public static double Goc(double x1, double y1, double x2, double y2)
        {
            double a = Math.Atan2(y2 - y1, x2 - x1);
            if (a < 0) a += 2.0 * Math.PI;
            return a;
        }

        /// <summary>gro--angle-diff-rad: lệch phương (không phân biệt chiều) trong [0, π/2].</summary>
        public static double LechGoc(double a1, double a2)
        {
            double d = Math.Abs(a1 - a2);
            while (d >= Math.PI) d -= Math.PI;
            if (d > Math.PI / 2.0) d = Math.PI - d;
            return d;
        }

        /// <summary>gro--bbox-scale-15</summary>
        public static Hop NoHop15(Hop bb, double h)
        {
            double cx, cy, w15, h15;
            if (bb != null)
            {
                cx = (bb.X1 + bb.X2) / 2.0; cy = (bb.Y1 + bb.Y2) / 2.0;
                double w = bb.X2 - bb.X1, ht = bb.Y2 - bb.Y1;
                w15 = Math.Max(1.5 * w, w + 2.0 * h);
                h15 = Math.Max(1.5 * ht, 2.5 * h);
            }
            else { w15 = 20.0; h15 = 15.0; cx = 0.0; cy = 0.0; }
            return new Hop(cx - w15 / 2.0, cy - h15 / 2.0, cx + w15 / 2.0, cy + h15 / 2.0);
        }

        /// <summary>(inters p1 p2 q1 q2 T): hai đoạn cắt nhau (song song = không).</summary>
        public static bool CatNhau(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
        {
            double rx = bx - ax, ry = by - ay, sx = dx - cx, sy = dy - cy;
            double den = rx * sy - ry * sx;
            if (Math.Abs(den) < 1e-12) return false;
            double t = ((cx - ax) * sy - (cy - ay) * sx) / den;
            double u = ((cx - ax) * ry - (cy - ay) * rx) / den;
            const double e = 1e-10;
            return t >= -e && t <= 1 + e && u >= -e && u <= 1 + e;
        }

        /// <summary>gro--line-bbox-collide-p</summary>
        public static bool DoanChamHop(double x1, double y1, double x2, double y2, Hop b)
        {
            if (!(Math.Min(x1, x2) <= b.X2 && Math.Max(x1, x2) >= b.X1 && Math.Min(y1, y2) <= b.Y2 && Math.Max(y1, y2) >= b.Y1))
                return false;
            if (x1 >= b.X1 && x1 <= b.X2 && y1 >= b.Y1 && y1 <= b.Y2) return true;
            if (x2 >= b.X1 && x2 <= b.X2 && y2 >= b.Y1 && y2 <= b.Y2) return true;
            return CatNhau(x1, y1, x2, y2, b.X1, b.Y1, b.X2, b.Y1) || CatNhau(x1, y1, x2, y2, b.X2, b.Y1, b.X2, b.Y2) ||
                   CatNhau(x1, y1, x2, y2, b.X2, b.Y2, b.X1, b.Y2) || CatNhau(x1, y1, x2, y2, b.X1, b.Y2, b.X1, b.Y1);
        }

        /// <summary>gro--dist-pt-seg</summary>
        public static double KcDiemDoan(double px, double py, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1, dy = y2 - y1, l2 = dx * dx + dy * dy;
            if (l2 <= 1e-8) return Kc(px, py, x1, y1);
            double t = ((px - x1) * dx + (py - y1) * dy) / l2;
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;
            return Kc(px, py, x1 + t * dx, y1 + t * dy);
        }

        /// <summary>gro--dist-pt-infinite-line</summary>
        public static double KcDiemDuongThang(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1, dy = y2 - y1, len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 1e-6) return Math.Abs((dy * x0 - dx * y0) + (x2 * y1 - x1 * y2)) / len;
            return Kc(x0, y0, x1, y1);
        }

        public static double Kc(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1, dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// gro--chuoi-thang-hang (mục 169): ghép LINE thẳng hàng nối tiếp với e. mem theo thứ tự lisp
        /// (cons: cái tìm sau đứng trước, e ở cuối); 2 đầu = polar từ đầu a của e.
        /// </summary>
        public static List<DoanThang> ChuoiThangHang(DoanThang e, IList<DoanThang> ds, out double ax, out double ay, out double bx, out double by)
        {
            double a0x = e.X1, a0y = e.Y1;
            double ang = Goc(e.X1, e.Y1, e.X2, e.Y2), ux = Math.Cos(ang), uy = Math.Sin(ang);
            double lo = 0.0, hi = Kc(e.X1, e.Y1, e.X2, e.Y2);
            var mem = new List<DoanThang> { e };          // mem[0] = phan tu CUOI cua list lisp
            var co = new HashSet<DoanThang> { e };
            bool doi = true;
            while (doi)
            {
                doi = false;
                foreach (DoanThang q in ds)
                {
                    if (co.Contains(q)) continue;
                    if (Kc(q.X1, q.Y1, q.X2, q.Y2) > 0.1 &&
                        LechGoc(Goc(q.X1, q.Y1, q.X2, q.Y2), ang) < 0.02 &&
                        KcDiemDuongThang(q.X1, q.Y1, e.X1, e.Y1, e.X2, e.Y2) <= 0.5 &&
                        KcDiemDuongThang(q.X2, q.Y2, e.X1, e.Y1, e.X2, e.Y2) <= 0.5)
                    {
                        double t1 = (q.X1 - a0x) * ux + (q.Y1 - a0y) * uy;
                        double t2 = (q.X2 - a0x) * ux + (q.Y2 - a0y) * uy;
                        if (Math.Min(t1, t2) <= hi + 1.0 && Math.Max(t1, t2) >= lo - 1.0)
                        {
                            mem.Add(q); co.Add(q);
                            lo = Math.Min(lo, Math.Min(t1, t2));
                            hi = Math.Max(hi, Math.Max(t1, t2));
                            doi = true;
                        }
                    }
                }
            }
            mem.Reverse();
            ax = a0x + lo * Math.Cos(ang); ay = a0y + lo * Math.Sin(ang);
            bx = a0x + hi * Math.Cos(ang); by = a0y + hi * Math.Sin(ang);
            return mem;
        }

        /// <summary>Một chữ số -> kết quả, hoặc null (không có đường dim chính / V không hợp lệ).</summary>
        public static KqDimDai Khop(TextDim t, IList<DoanThang> lines)
        {
            if (t.V == null || !(t.V.Value > 0.1)) return null;
            double v = t.V.Value, rot = t.Rot;
            Hop bb15 = NoHop15(t.Bb, t.H);
            double tol = Math.Max(2.0, 0.02 * v);
            List<DoanThang> bestMem = null;
            double p1x = 0, p1y = 0, p2x = 0, p2y = 0, minDist = 1e9;

            foreach (DoanThang l in lines)
            {
                double len = Kc(l.X1, l.Y1, l.X2, l.Y2);
                double da = LechGoc(Goc(l.X1, l.Y1, l.X2, l.Y2), rot);
                List<DoanThang> cMem = null;
                double ax = 0, ay = 0, bx = 0, by = 0;
                if (len > 0.1 && (da < 0.15 || Math.Abs(da - Math.PI) < 0.15) && DoanChamHop(l.X1, l.Y1, l.X2, l.Y2, bb15))
                {
                    if (Math.Abs(len - v) <= tol)
                    {
                        cMem = new List<DoanThang> { l };
                        ax = l.X1; ay = l.Y1; bx = l.X2; by = l.Y2;
                    }
                    else
                    {
                        List<DoanThang> ch = ChuoiThangHang(l, lines, out double cax, out double cay, out double cbx, out double cby);
                        if (ch.Count > 1 && Math.Abs(Kc(cax, cay, cbx, cby) - v) <= tol)
                        {
                            cMem = ch; ax = cax; ay = cay; bx = cbx; by = cby;
                        }
                    }
                }
                if (cMem != null)
                {
                    double d1 = Kc(t.Px, t.Py, (ax + bx) / 2.0, (ay + by) / 2.0);
                    if (d1 < minDist)
                    {
                        minDist = d1; bestMem = cMem;
                        p1x = ax; p1y = ay; p2x = bx; p2y = by;
                    }
                }
            }
            if (bestMem == null) return null;

            var kq = new KqDimDai { Mem = bestMem, PmX = (p1x + p2x) / 2.0, PmY = (p1y + p2y) / 2.0 };
            var laMem = new HashSet<DoanThang>(bestMem);
            double vdx = 0.0, vdy = 0.0;
            foreach (DoanThang c in lines)
            {
                if (laMem.Contains(c)) continue;
                double diff = LechGoc(Goc(c.X1, c.Y1, c.X2, c.Y2), rot);
                if (Math.Abs(diff - Math.PI / 2.0) < 0.25)
                {
                    double d1 = KcDiemDoan(p1x, p1y, c.X1, c.Y1, c.X2, c.Y2);
                    double d2 = KcDiemDoan(p2x, p2y, c.X1, c.Y1, c.X2, c.Y2);
                    if (d1 <= 4.5 || d2 <= 4.5)
                    {
                        kq.Ticks.Add(c);
                        double px = d1 <= 4.5 ? p1x : p2x, py = d1 <= 4.5 ? p1y : p2y;
                        bool gan1 = Kc(px, py, c.X1, c.Y1) < Kc(px, py, c.X2, c.Y2);
                        double nx = gan1 ? c.X1 : c.X2, ny = gan1 ? c.Y1 : c.Y2;
                        double fx = gan1 ? c.X2 : c.X1, fy = gan1 ? c.Y2 : c.Y1;
                        vdx += fx - nx; vdy += fy - ny;
                    }
                }
            }
            kq.Ticks.Reverse();
            double lenV = Math.Sqrt(vdx * vdx + vdy * vdy);
            if (lenV > 1e-4) { kq.RdX = vdx / lenV; kq.RdY = vdy / lenV; }
            else { kq.RdX = -Math.Sin(rot); kq.RdY = Math.Cos(rot); }
            return kq;
        }

        public static List<KqDimDai> KhopHet(IList<TextDim> texts, IList<DoanThang> lines)
        {
            var ds = new List<KqDimDai>(texts.Count);
            foreach (TextDim t in texts) ds.Add(Khop(t, lines));
            return ds;
        }
    }
}
