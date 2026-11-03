using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>Một nét 0-KAKOU / 3-KEGAKI / TAPU_TMT trong ss (thứ tự ssname).</summary>
    public sealed class NetCat
    {
        public string H;
        /// <summary>Bị loại (không gian giấy / bản chụp / layer *_CHUP).</summary>
        public bool Bo;
        /// <summary>Hộp bao (hoặc ±5 quanh điểm chèn khi không có hộp); null = bỏ.</summary>
        public Hop Bb;
    }

    /// <summary>Một mục danh sách chi tiết: tid, khung in (bb-final), vùng nét cắt (bb-cut), pt9 ban đầu.</summary>
    public sealed class MucCt
    {
        public int Tid;
        public Hop BbF, BbC;
        public List<string> Pt9 = new List<string>();
    }

    /// <summary>
    /// Mục 181 (GĐ 5d-a): chép bước 2 + 4 của <c>gro--tao-danh-sach-chi-tiet</c> — chia MỌI nét cắt / kegaki
    /// về tấm: tâm nét trong khung in (nới 3) của đúng 1 tấm → tấm đó; nhiều tấm → gần vùng nét cắt nhất
    /// (trong nới 2 = 0); không tấm nào → vùng nét cắt gần nhất nếu ≤ 30. Kết quả = pt9 cuối cùng mỗi mục
    /// (nét gán thêm theo thứ tự ss, rồi pt9 ban đầu, bỏ trùng giữ lần đầu) — y như lisp cons + gro--remove-duplicates.
    /// Thuần, không gọi ZWCAD.
    /// </summary>
    public static class ChiaNetCat
    {
        public static bool TrongHop(double x, double y, Hop b, double tol) =>
            x >= b.X1 - tol && x <= b.X2 + tol && y >= b.Y1 - tol && y <= b.Y2 + tol;

        /// <summary>gro--dist-pt-to-bbox</summary>
        public static double KcToiHop(double x, double y, Hop b)
        {
            double dx = x < b.X1 ? b.X1 - x : x > b.X2 ? x - b.X2 : 0.0;
            double dy = y < b.Y1 ? b.Y1 - y : y > b.Y2 ? y - b.Y2 : 0.0;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public sealed class KetQua
        {
            public int SoNet;
            public List<List<string>> Pt9 = new List<List<string>>();
        }

        public static KetQua Chay(IList<NetCat> ss, IList<MucCt> ds)
        {
            var kq = new KetQua();
            // kakou-keg-items = cons theo ss -> duyet NGUOC ss
            var items = new List<NetCat>();
            foreach (NetCat n in ss) if (!n.Bo && n.Bb != null) items.Add(n);
            items.Reverse();
            kq.SoNet = items.Count;

            var gan = new List<List<string>>();
            foreach (MucCt m in ds) gan.Add(new List<string>());
            if (items.Count > 0 && ds.Count > 0)
                foreach (NetCat it in items)
                {
                    double x = (it.Bb.X1 + it.Bb.X2) / 2.0, y = (it.Bb.Y1 + it.Bb.Y2) / 2.0;
                    var cands = new List<MucCt>();
                    foreach (MucCt m in ds) if (m.BbF != null && TrongHop(x, y, m.BbF, 3.0)) cands.Add(m);
                    cands.Reverse();                                     // cons
                    MucCt best = null;
                    if (cands.Count == 1) best = cands[0];
                    else if (cands.Count > 1)
                    {
                        double min = 1e9;
                        foreach (MucCt c in cands)
                        {
                            if (c.BbC == null) continue;
                            double d = TrongHop(x, y, c.BbC, 2.0) ? 0.0 : KcToiHop(x, y, c.BbC);
                            if (d < min) { min = d; best = c; }
                        }
                    }
                    else
                    {
                        double min = 1e9;
                        foreach (MucCt c in ds)
                        {
                            if (c.BbC == null) continue;
                            double d = KcToiHop(x, y, c.BbC);
                            if (d < min) { min = d; best = c; }
                        }
                        if (min > 30.0) best = null;
                    }
                    if (best == null) continue;
                    for (int i = 0; i < ds.Count; i++) if (ds[i].Tid == best.Tid) gan[i].Add(it.H);
                }

            for (int i = 0; i < ds.Count; i++)
            {
                if (items.Count == 0) { kq.Pt9.Add(new List<string>(ds[i].Pt9)); continue; }
                var g = gan[i];
                g.Reverse();                                             // cons -> cai gan sau dung truoc
                var ra = new List<string>();
                var co = new HashSet<string>(StringComparer.Ordinal);
                foreach (string h in g) if (co.Add(h)) ra.Add(h);
                foreach (string h in ds[i].Pt9) if (co.Add(h)) ra.Add(h);
                kq.Pt9.Add(ra);
            }
            return kq;
        }
    }
}
