using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 175 (GĐ 3b): chép <c>gro--geom-match-linear-dims</c> — nhận nét Dim dài ở bước xử lý nét
    /// (đổi layer 1-SUNPOU). Khác bản thu thập Dim (mục 174):
    /// <list type="bullet">
    /// <item>chọn đường dim theo khoảng cách tâm chữ → ĐOẠN (gro--dist-pt-seg), song song nới 0.28 rad
    ///   hoặc cùng gần đứng / gần ngang;</item>
    /// <item>chân dim so phương với chính đường dim (không phải chữ), nhận cả tick chéo 45° ngắn; nối
    ///   thêm đoạn chân dim thẳng hàng (≤ 3.5 mm từ đầu mút);</item>
    /// <item>không có đường dim ≈ V thì dò CẶP tick có khoảng chiếu ≈ V rồi gom mảnh đường dim giữa;</item>
    /// <item>kết quả là MỘT danh sách tích luỹ res-dim-ents dùng chung cho mọi chữ số (chữ sau thấy
    ///   phần tử chữ trước đã thêm) — giữ nguyên trạng thái đó, thứ tự cuối như cons của lisp.</item>
    /// </list>
    /// </summary>
    public static class DimDai3b
    {
        public sealed class KetQua
        {
            /// <summary>res-dim-ents đúng thứ tự lisp (cái thêm sau đứng trước).</summary>
            public List<DoanThang> Net = new List<DoanThang>();
            /// <summary>(chỉ số chữ, số LINE) của các chữ có đường dim ghép chuỗi (mục 169) — lisp in thông báo.</summary>
            public List<int[]> Chuoi = new List<int[]>();
        }

        private static bool SongSong(double da, double rot, double ang) =>
            da < 0.28 ||
            (Math.Abs(Math.Cos(rot)) < 0.28 && Math.Abs(Math.Cos(ang)) < 0.28) ||
            (Math.Abs(Math.Sin(rot)) < 0.28 && Math.Abs(Math.Sin(ang)) < 0.28);

        private sealed class Tick
        {
            public DoanThang D;
            public double Mx, My;
        }

        public static KetQua Chay(IList<TextDim> texts, IList<DoanThang> lines)
        {
            var kq = new KetQua();
            var res = new List<DoanThang>();          // thu tu THEM VAO
            var co = new HashSet<DoanThang>();
            void Them(DoanThang d) { if (co.Add(d)) res.Add(d); }

            for (int ti = 0; ti < texts.Count; ti++)
            {
                TextDim t = texts[ti];
                if (t.V == null || !(t.V.Value > 0.1)) continue;
                double v = t.V.Value, rot = t.Rot, h = t.H;
                Hop bb15 = DimDai.NoHop15(t.Bb, h);
                double tol = Math.Max(2.0, 0.02 * v);
                double uCos = Math.Cos(rot), uSin = Math.Sin(rot), nCos = -uSin, nSin = uCos;
                double minDist = 1e9;
                List<DoanThang> bestMem = null;
                double p1x = 0, p1y = 0, p2x = 0, p2y = 0;

                // 1. duong dim chinh ~V (hoac chuoi thang hang muc 169), gan tam chu nhat (toi DOAN)
                foreach (DoanThang l in lines)
                {
                    if (!DimDai.DoanChamHop(l.X1, l.Y1, l.X2, l.Y2, bb15)) continue;
                    double len = DimDai.Kc(l.X1, l.Y1, l.X2, l.Y2);
                    double ang = DimDai.Goc(l.X1, l.Y1, l.X2, l.Y2);
                    double da = DimDai.LechGoc(ang, rot);
                    if (!(len > 0.1 && SongSong(da, rot, ang))) continue;
                    List<DoanThang> cMem = null;
                    double ax = 0, ay = 0, bx = 0, by = 0;
                    if (Math.Abs(len - v) <= tol)
                    {
                        cMem = new List<DoanThang> { l };
                        ax = l.X1; ay = l.Y1; bx = l.X2; by = l.Y2;
                    }
                    else
                    {
                        List<DoanThang> ch = DimDai.ChuoiThangHang(l, lines, out double cax, out double cay, out double cbx, out double cby);
                        if (ch.Count > 1 && Math.Abs(DimDai.Kc(cax, cay, cbx, cby) - v) <= tol)
                        {
                            cMem = ch; ax = cax; ay = cay; bx = cbx; by = cby;
                        }
                    }
                    if (cMem == null) continue;
                    double d = DimDai.KcDiemDoan(t.Px, t.Py, ax, ay, bx, by);
                    if (d < minDist)
                    {
                        minDist = d; bestMem = cMem;
                        p1x = ax; p1y = ay; p2x = bx; p2y = by;
                    }
                }

                if (bestMem != null)
                {
                    // 2. dua duong dim + chan dim (vuong goc duong dim, hoac tick cheo 45 ngan) + doan noi chan dim
                    foreach (DoanThang m in bestMem) Them(m);
                    if (bestMem.Count > 1) kq.Chuoi.Add(new[] { ti, bestMem.Count });
                    var laMem = new HashSet<DoanThang>(bestMem);
                    double angDim = DimDai.Goc(p1x, p1y, p2x, p2y);
                    foreach (DoanThang c in lines)
                    {
                        if (laMem.Contains(c)) continue;
                        double cAng = DimDai.Goc(c.X1, c.Y1, c.X2, c.Y2);
                        double diff = DimDai.LechGoc(cAng, angDim);
                        double cLen = DimDai.Kc(c.X1, c.Y1, c.X2, c.Y2);
                        if (!(Math.Abs(diff - Math.PI / 2.0) < 0.28 || (Math.Abs(diff - Math.PI / 4.0) < 0.20 && cLen < 15.0))) continue;
                        double d1 = DimDai.KcDiemDoan(p1x, p1y, c.X1, c.Y1, c.X2, c.Y2);
                        double d2 = DimDai.KcDiemDoan(p2x, p2y, c.X1, c.Y1, c.X2, c.Y2);
                        if (!(d1 <= 4.0 || d2 <= 4.0)) continue;
                        Them(c);
                        foreach (DoanThang n in lines)
                        {
                            if (laMem.Contains(n) || ReferenceEquals(n, c) || co.Contains(n)) continue;
                            double nAng = DimDai.Goc(n.X1, n.Y1, n.X2, n.Y2);
                            if (DimDai.LechGoc(nAng, cAng) < 0.15 &&
                                (DimDai.Kc(n.X1, n.Y1, c.X1, c.Y1) <= 3.5 || DimDai.Kc(n.X1, n.Y1, c.X2, c.Y2) <= 3.5 ||
                                 DimDai.Kc(n.X2, n.Y2, c.X1, c.Y1) <= 3.5 || DimDai.Kc(n.X2, n.Y2, c.X2, c.Y2) <= 3.5))
                                Them(n);
                        }
                    }
                    continue;
                }

                // 3. du phong: cap tick co khoang chieu ~V
                var ticks = new List<Tick>();             // thu tu THEM; lisp cons -> dao nguoc
                foreach (DoanThang c in lines)
                {
                    double cLen = DimDai.Kc(c.X1, c.Y1, c.X2, c.Y2);
                    if (!(cLen > 0.1 && cLen <= Math.Max(30.0, 5.0 * h))) continue;
                    double diff = DimDai.LechGoc(DimDai.Goc(c.X1, c.Y1, c.X2, c.Y2), rot);
                    if (!(Math.Abs(diff - Math.PI / 2.0) < 0.28 || (Math.Abs(diff - Math.PI / 4.0) < 0.20 && cLen <= 15.0))) continue;
                    double mx = (c.X1 + c.X2) / 2.0, my = (c.Y1 + c.Y2) / 2.0;
                    if (DimDai.Kc(mx, my, t.Px, t.Py) <= Math.Max(50.0, v / 2.0 + 25.0))
                        ticks.Add(new Tick { D = c, Mx = mx, My = my });
                }
                ticks.Reverse();
                if (ticks.Count < 2) continue;
                Tick b1 = null, b2 = null;
                double bestP = 1e9;
                for (int i = 0; i < ticks.Count; i++)
                    for (int j = i + 1; j < ticks.Count; j++)
                    {
                        Tick a = ticks[i], b = ticks[j];
                        double span = Math.Abs((b.Mx - a.Mx) * uCos + (b.My - a.My) * uSin);
                        double off = Math.Abs((b.Mx - a.Mx) * nCos + (b.My - a.My) * nSin);
                        if (off <= 8.0 && Math.Abs(span - v) <= tol)
                        {
                            double pd = DimDai.Kc((a.Mx + b.Mx) / 2.0, (a.My + b.My) / 2.0, t.Px, t.Py);
                            if (pd < bestP) { bestP = pd; b1 = a; b2 = b; }
                        }
                    }
                if (b1 == null) continue;
                Them(b1.D);
                Them(b2.D);
                double u1 = b1.Mx * uCos + b1.My * uSin, u2 = b2.Mx * uCos + b2.My * uSin;
                double uMin = Math.Min(u1, u2), uMax = Math.Max(u1, u2);
                double nDim = ((b1.Mx * nCos + b1.My * nSin) + (b2.Mx * nCos + b2.My * nSin)) / 2.0;
                foreach (DoanThang s in lines)
                {
                    if (ReferenceEquals(s, b1.D) || ReferenceEquals(s, b2.D)) continue;
                    double sLen = DimDai.Kc(s.X1, s.Y1, s.X2, s.Y2);
                    if (!(sLen > 0.1 && sLen <= v + tol)) continue;
                    double sAng = DimDai.Goc(s.X1, s.Y1, s.X2, s.Y2);
                    if (!SongSong(DimDai.LechGoc(sAng, rot), rot, sAng)) continue;
                    double su1 = s.X1 * uCos + s.Y1 * uSin, su2 = s.X2 * uCos + s.Y2 * uSin;
                    double sn1 = s.X1 * nCos + s.Y1 * nSin, sn2 = s.X2 * nCos + s.Y2 * nSin;
                    if (Math.Abs(sn1 - nDim) <= 4.0 && Math.Abs(sn2 - nDim) <= 4.0 &&
                        Math.Max(su1, su2) >= uMin - 3.0 && Math.Min(su1, su2) <= uMax + 3.0)
                        Them(s);
                }
            }
            res.Reverse();
            kq.Net = res;
            return kq;
        }
    }
}
