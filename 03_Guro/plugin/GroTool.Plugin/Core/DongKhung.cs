using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>Một Dim đã gán cho hình của tấm: hộp + cờ "gán CHẮC CHẮN" (nth 2 d-pair, mục 116).</summary>
    public sealed class DimDk
    {
        public Hop Bb;
        public bool ChacChan;
    }

    /// <summary>Một text-rep (tấm) của vòng đóng khung, đúng thứ tự text-reps lisp.</summary>
    public sealed class TamDk
    {
        public int Tid;
        /// <summary>t-bb (nth 2 t-rep) — hộp cụm ghi chú.</summary>
        public Hop TBb;
        public List<Hop> HinhBb = new List<Hop>();
        /// <summary>dims-cua-tam ĐÚNG thứ tự lisp (append theo s-list).</summary>
        public List<DimDk> Dims = new List<DimDk>();
        /// <summary>Có mục trong cum-khois-map (dù hộp có thể nil) — như "(if k-list ...)" mục 172.</summary>
        public bool CoKhoi;
        /// <summary>Hộp cụm bango / mã khắc / F-name (gro--bbox-gop các entity của khối); null nếu không có.</summary>
        public Hop KhoiBb;
    }

    /// <summary>
    /// Mục 183 (GĐ 5d-b): chép vòng "đóng khung" của <c>gro--tao-khung-dong-thuan</c> — bỏ Dim (chưa gán chắc)
    /// chạm cụm ghi chú + bango của TẤM KHÁC; hộp = hình + Dim còn lại + ghi chú + cụm bango của chính tấm;
    /// lề 15 co lại theo khe tới tấm khác (khe 0..35 → max(2, khe/2 − 2)). Thuần, không gọi ZWCAD.
    /// </summary>
    public static class DongKhung
    {
        public sealed class KetQua
        {
            public Hop BbFinal;
            /// <summary>Cờ giữ từng Dim (song song TamDk.Dims).</summary>
            public List<bool> GiuDim = new List<bool>();
        }

        /// <summary>gro--bb-gop-2</summary>
        public static Hop Gop(Hop a, Hop b)
        {
            if (a == null) return b;
            if (b == null) return a;
            return new Hop(Math.Min(a.X1, b.X1), Math.Min(a.Y1, b.Y1), Math.Max(a.X2, b.X2), Math.Max(a.Y2, b.Y2));
        }

        /// <summary>gro--bb-overlap-p (chạm mép tính là chồng).</summary>
        public static bool Chong(Hop a, Hop b) => a.X1 <= b.X2 && a.X2 >= b.X1 && a.Y1 <= b.Y2 && a.Y2 >= b.Y1;

        public static List<KetQua> Chay(IList<TamDk> tams)
        {
            // hop "ghi chu + bango" cua moi tam (bb-khoi-tid chi co cho tam co khoi)
            Hop HopTamKhac(TamDk o) => o.CoKhoi ? Gop(o.TBb, o.KhoiBb) : o.TBb;

            var ds = new List<KetQua>();
            foreach (TamDk t in tams)
            {
                var kq = new KetQua();
                Hop cbbDims = null;
                foreach (DimDk d in t.Dims)
                {
                    bool bo = false;
                    if (!d.ChacChan)
                        foreach (TamDk ot in tams)
                        {
                            if (ot.Tid == t.Tid) continue;
                            Hop ob = HopTamKhac(ot);
                            if (ob != null && Chong(d.Bb, ob)) { bo = true; break; }
                        }
                    kq.GiuDim.Add(!bo);
                    if (!bo) cbbDims = Gop(cbbDims, d.Bb);
                }
                Hop cbbShapes = null;
                foreach (Hop h in t.HinhBb) cbbShapes = Gop(cbbShapes, h);
                Hop bbGeom = cbbShapes != null && cbbDims != null ? Gop(cbbShapes, cbbDims) : cbbShapes ?? cbbDims;
                Hop tot = bbGeom != null ? Gop(bbGeom, t.TBb) : t.TBb;
                if (t.CoKhoi && t.KhoiBb != null) tot = Gop(tot, t.KhoiBb);

                double padX = 15.0, padY = 15.0;
                foreach (TamDk ot in tams)
                {
                    if (ot.Tid == t.Tid) continue;
                    Hop ob = HopTamKhac(ot);
                    double gx = Math.Max(0.0, Math.Max(ob.X1 - tot.X2, tot.X1 - ob.X2));
                    if (gx > 0.0 && gx < 35.0) padX = Math.Min(padX, Math.Max(2.0, gx / 2.0 - 2.0));
                    double gy = Math.Max(0.0, Math.Max(ob.Y1 - tot.Y2, tot.Y1 - ob.Y2));
                    if (gy > 0.0 && gy < 35.0) padY = Math.Min(padY, Math.Max(2.0, gy / 2.0 - 2.0));
                }
                kq.BbFinal = new Hop(tot.X1 - padX, tot.Y1 - padY, tot.X2 + padX, tot.Y2 + padY);
                ds.Add(kq);
            }
            return ds;
        }
    }
}
