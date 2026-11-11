using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 225 (GĐ 3e): chép 1-1 phần khớp Dim của <c>gro--detect-geom-dims</c> sau bước cung bo góc —
    /// nâng dim góc (2.5), khớp lỗ / leader (<c>gro--geom-match-hole-leaders</c>), khớp Dim dài
    /// (<see cref="DimDai3b"/>), khớp dim góc (<c>gro--geom-match-angular-dims</c>, mục 223 / 224) rồi hợp nhất.
    /// Kết quả là TẬP handle (lisp chỉ dùng <c>member</c>), thứ tự texts-lin giữ như lisp vì DimDai3b phụ thuộc thứ tự.
    /// </summary>
    public static class DimKhop3b
    {
        /// <summary>Một text số: (e txt v pt rot h bb [is-phi]).</summary>
        public sealed class TextSo
        {
            public string Handle, Txt;
            public double V, Px, Py, Rot, H;
            public Hop Bb;
            public bool LaPhi;
        }

        public sealed class Cung
        {
            public string Handle;
            public double Cx, Cy, R, A0, A1;
        }

        public sealed class TronNho
        {
            public string Handle;
            public double X, Y;
        }

        public sealed class KetQua
        {
            /// <summary>Handle nét Dim (không trùng), thứ tự thêm.</summary>
            public List<string> Net = new List<string>();
            /// <summary>(txt, số LINE) của chữ có đường dim ghép chuỗi (mục 169) — để in thông báo.</summary>
            public List<KeyValuePair<string, int>> Chuoi = new List<KeyValuePair<string, int>>();
            public int SoNang, SoLeader, SoDai, SoGoc;
        }

        public static KetQua Chay(List<TextSo> lin, List<TextSo> hole, List<TextSo> ang,
                                  IList<DoanThang> lines, IList<Cung> arcsDim, IList<TronNho> tiny)
        {
            var kq = new KetQua();
            var co = new HashSet<string>();
            void Them(string h) { if (co.Add(h)) kq.Net.Add(h); }
            ang = new List<TextSo>(ang);

            // 2.5 nang dim goc: text so 5..175 co tron nho ky hieu do (<= 3h) hoac cung quet khop truc tiep (+-6, chi <= 95)
            var conLin = new List<TextSo>();
            foreach (TextSo t in lin)
            {
                bool nang = false;
                if (t.V >= 5.0 && t.V <= 175.0)
                {
                    foreach (TronNho tc in tiny) if (Kc(tc.X, tc.Y, t.Px, t.Py) <= 3.0 * t.H) { nang = true; break; }
                    if (!nang && t.V <= 95.0)
                        foreach (Cung a in arcsDim)
                            if (KcCung(t.Px, t.Py, a) <= Math.Max(50.0, 6.0 * t.H) && Math.Abs(QuetDo(a) - t.V) <= 6.0) { nang = true; break; }
                }
                if (nang) { ang.Insert(0, t); kq.SoNang++; } else conLin.Add(t);
            }
            lin = conLin;

            // 3. leader truoc; text phi khong khop leader -> thu nhu dim dai (dua len DAU texts-lin nhu cons)
            foreach (TextSo t in hole)
            {
                List<DoanThang> r = Leader(t, lines);
                if (r.Count > 0) { foreach (DoanThang d in r) Them(d.Handle); kq.SoLeader++; }
                else if (t.LaPhi) lin.Insert(0, t);
            }

            // khop dim dai (muc 175)
            var tds = new List<TextDim>();
            foreach (TextSo t in lin)
                tds.Add(new TextDim { Handle = t.Handle, V = t.V, Px = t.Px, Py = t.Py, Rot = t.Rot, H = t.H, Bb = t.Bb });
            DimDai3b.KetQua kd = DimDai3b.Chay(tds, lines);
            foreach (DoanThang d in kd.Net) Them(d.Handle);
            foreach (int[] c in kd.Chuoi) kq.Chuoi.Add(new KeyValuePair<string, int>(lin[c[0]].Txt, c[1]));
            kq.SoDai = kd.Net.Count;

            // khop dim goc
            int truoc = kq.Net.Count;
            foreach (TextSo t in ang) Goc(t, lines, arcsDim, tiny, Them);
            kq.SoGoc = kq.Net.Count - truoc;
            return kq;
        }

        // ---------------- leader (gro--geom-match-hole-leaders, 1 text) ----------------

        public static List<DoanThang> Leader(TextSo t, IList<DoanThang> lines)
        {
            var res = new List<DoanThang>();
            var co = new HashSet<DoanThang>();
            void Them(DoanThang d) { if (co.Add(d)) res.Add(d); }
            double v = t.V, h = t.H;
            if (!(v > 0.0)) return res;
            double tol = Math.Max(2.0, 0.05 * v);

            // 1. net do chu (shelf): ngan, song song chu, gan theo phuong doc, chong lan phuong ngang
            var shelf = new List<DoanThang>();
            foreach (DoanThang l in lines)
            {
                double len = Kc(l.X1, l.Y1, l.X2, l.Y2);
                if (!(len > 1.0 && len <= Math.Max(40.0, 5.0 * h))) continue;
                if (!(LechGoc(Goc(l.X1, l.Y1, l.X2, l.Y2), t.Rot) < 0.25)) continue;
                double midy = (l.Y1 + l.Y2) / 2.0;
                double dy = t.Bb != null ? Math.Abs(midy - t.Bb.Y1) : Math.Abs(midy - t.Py);
                if (!(dy <= Math.Max(20.0, 3.5 * h))) continue;
                double sMin = Math.Min(l.X1, l.X2), sMax = Math.Max(l.X1, l.X2);
                double tMin = t.Bb != null ? t.Bb.X1 : t.Px - 15.0, tMax = t.Bb != null ? t.Bb.X2 : t.Px + 15.0;
                if (!(sMin > tMax + 20.0 || sMax < tMin - 20.0)) shelf.Insert(0, l);
            }

            // 2. net 2 noi 1 dau net 1, dai ~ V hoac V/2; mui ten ngan noi dau kia
            foreach (DoanThang sh in shelf)
                foreach (DoanThang c in lines)
                {
                    if (ReferenceEquals(c, sh)) continue;
                    double len = Kc(c.X1, c.Y1, c.X2, c.Y2);
                    if (!(Math.Abs(len - v) <= tol || Math.Abs(len - v / 2.0) <= tol)) continue;
                    bool noi = false;
                    double ox = 0, oy = 0;
                    if (Kc(c.X1, c.Y1, sh.X1, sh.Y1) <= 3.5 || Kc(c.X1, c.Y1, sh.X2, sh.Y2) <= 3.5) { noi = true; ox = c.X2; oy = c.Y2; }
                    if (Kc(c.X2, c.Y2, sh.X1, sh.Y1) <= 3.5 || Kc(c.X2, c.Y2, sh.X2, sh.Y2) <= 3.5) { noi = true; ox = c.X1; oy = c.Y1; }
                    if (!noi) continue;
                    Them(sh);
                    Them(c);
                    foreach (DoanThang a in lines)
                    {
                        if (ReferenceEquals(a, c) || ReferenceEquals(a, sh)) continue;
                        double al = Kc(a.X1, a.Y1, a.X2, a.Y2);
                        if (al > 0.5 && al <= 4.5 && (Kc(a.X1, a.Y1, ox, oy) <= 3.0 || Kc(a.X2, a.Y2, ox, oy) <= 3.0)) Them(a);
                    }
                }
            return res;
        }

        // ---------------- dim goc (gro--geom-match-angular-dims, 1 text) ----------------

        private static void Goc(TextSo t, IList<DoanThang> lines, IList<Cung> arcs, IList<TronNho> tiny, Action<string> them)
        {
            double maxDist = Math.Max(65.0, 8.0 * t.H);
            foreach (Cung a in arcs)
            {
                if (!(KcCung(t.Px, t.Py, a) <= maxDist)) continue;
                double sw = QuetDo(a);
                if (!(Math.Abs(sw - t.V) <= 7.5 || Math.Abs(Math.Abs(180.0 - sw) - t.V) <= 7.5)) continue;
                them(a.Handle);
                double sx = a.Cx + a.R * Math.Cos(a.A0), sy = a.Cy + a.R * Math.Sin(a.A0);
                double ex = a.Cx + a.R * Math.Cos(a.A1), ey = a.Cy + a.R * Math.Sin(a.A1);
                foreach (DoanThang l in lines)
                {
                    double len = Kc(l.X1, l.Y1, l.X2, l.Y2);
                    if (!(len > 0.5 && len <= Math.Max(45.0, a.R + 10.0))) continue;
                    double g = Goc(l.X1, l.Y1, l.X2, l.Y2);
                    bool chamDau = ((Kc(l.X1, l.Y1, sx, sy) <= 4.0 || Kc(l.X2, l.Y2, sx, sy) <= 4.0) && LechGoc(g, a.A0) < 0.18)
                                || ((Kc(l.X1, l.Y1, ex, ey) <= 4.0 || Kc(l.X2, l.Y2, ex, ey) <= 4.0) && LechGoc(g, a.A1) < 0.18);
                    if (chamDau && (len <= 45.0 || ChanTuDo(l, sx, sy, ex, ey, lines, arcs))) them(l.Handle);
                }
            }
            foreach (TronNho tc in tiny) if (Kc(tc.X, tc.Y, t.Px, t.Py) <= 3.0 * t.H) them(tc.Handle);
        }

        /// <summary>gro--chan-dim-tu-do-p (mục 223): đầu XA cung không có đầu LINE / ARC nào khác trong 1 mm.</summary>
        public static bool ChanTuDo(DoanThang e, double pax, double pay, double pbx, double pby, IList<DoanThang> lines, IList<Cung> arcs)
        {
            bool xa2 = Math.Min(Kc(e.X1, e.Y1, pax, pay), Kc(e.X1, e.Y1, pbx, pby)) < Math.Min(Kc(e.X2, e.Y2, pax, pay), Kc(e.X2, e.Y2, pbx, pby));
            double x = xa2 ? e.X2 : e.X1, y = xa2 ? e.Y2 : e.Y1;
            foreach (DoanThang l in lines)
                if (!ReferenceEquals(l, e) && (Kc(x, y, l.X1, l.Y1) < 1.0 || Kc(x, y, l.X2, l.Y2) < 1.0)) return false;
            foreach (Cung a in arcs)
                if (Kc(x, y, a.Cx + a.R * Math.Cos(a.A0), a.Cy + a.R * Math.Sin(a.A0)) < 1.0
                    || Kc(x, y, a.Cx + a.R * Math.Cos(a.A1), a.Cy + a.R * Math.Sin(a.A1)) < 1.0) return false;
            return true;
        }

        // ---------------- hinh hoc nho ----------------

        public static double Kc(double x1, double y1, double x2, double y2) => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

        /// <summary>gro--dist-pt-arc: |khoảng cách tới tâm − R| (bỏ qua giới hạn góc).</summary>
        public static double KcCung(double x, double y, Cung a) => Math.Abs(Kc(x, y, a.Cx, a.Cy) - a.R);

        /// <summary>Góc quét (độ), a1 &lt; a0 thì cộng 2π như lisp.</summary>
        public static double QuetDo(Cung a) => (a.A1 >= a.A0 ? a.A1 - a.A0 : 2.0 * Math.PI + (a.A1 - a.A0)) * 180.0 / Math.PI;

        /// <summary>(angle p1 p2) của lisp: [0, 2π).</summary>
        public static double Goc(double x1, double y1, double x2, double y2)
        {
            double g = Math.Atan2(y2 - y1, x2 - x1);
            return g < 0 ? g + 2.0 * Math.PI : g;
        }

        /// <summary>gro--angle-diff-rad: lệch góc theo chu kỳ π, trong [0, π/2].</summary>
        public static double LechGoc(double a1, double a2)
        {
            double d = Math.Abs(a1 - a2);
            while (d >= Math.PI) d -= Math.PI;
            if (d > Math.PI / 2.0) d = Math.PI - d;
            return d;
        }
    }
}
