using System;
using System.Collections.Generic;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 215 (GĐ 5b, bước "ghephc"): ghép hình chiếu / mặt cắt còn lẻ vào cụm tấm bẻ — chép 1-1
    /// lisp: bước 3A Hungarian 1-1 theo vùng (gro--ghep-hinh-chieu-hungarian) rồi bước 3B ghép nốt
    /// cặp chiếu tia gần nhất (gro--ghep-hinh-chieu-phu, mục 214). Khoảng cách = gro--d-hinh-cum,
    /// tia = gro--ray-chieu-goc, tách đôi theo nét cắt = gro--tach-tu-nhau-p.
    /// Không đụng bản vẽ: trả danh sách lần ghép theo đúng thứ tự lisp áp dụng.
    /// </summary>
    public static class GhepHinhChieu
    {
        public sealed class Hinh
        {
            public int Id;
            public double X1, Y1, X2, Y2;
            /// <summary>Chỉ số vùng (khungs-pham-vi), null = ngoài mọi vùng.</summary>
            public int? Vung;
        }

        public sealed class Cum
        {
            public int Tid;
            public int? Vung;
            /// <summary>Id hình của cụm, MỚI GHÉP ĐỨNG ĐẦU (như cons của lisp).</summary>
            public List<int> HinhIds = new List<int>();
        }

        /// <summary>Một lần nét cắt tấm gộp (gro--ds-cat): (x-div gap sx1 sy1 sx2 sy2).</summary>
        public sealed class VetCat
        {
            public double XDiv, Gap, X1, Y1, X2, Y2;
        }

        public sealed class LanGhep
        {
            public int Hinh, Tam;
            public double D;
            /// <summary>1 = Hungarian 1-1 (3A), 2 = chiếu tia (3B).</summary>
            public int Loai;
        }

        public const double MacDinhTia = 400.0;
        private const double ChanVatCan = 10000000.0, Gia = 1000000.0, NguongGhep = 990000.0;

        public static List<LanGhep> Chay(IList<Hinh> tatCa, IList<int> cho, IList<Cum> cum, double maxTia,
                                         bool coVung, int soVung, IList<VetCat> dsCat)
        {
            var theoId = tatCa.ToDictionary(h => h.Id);
            var pending = cho.Select(id => theoId[id]).ToList();
            var bent = cum.Select(c => new Cum { Tid = c.Tid, Vung = c.Vung, HinhIds = new List<int>(c.HinhIds) }).ToList();
            var kq = new List<LanGhep>();

            // 3A: Hungarian 1-1 từng vùng (không vùng: một lượt cho tất cả)
            if (pending.Count > 0 && bent.Count > 0)
            {
                IEnumerable<int?> dsVung = coVung ? Enumerable.Range(0, Math.Max(0, soVung)).Select(v => (int?)v)
                                                  : new int?[] { null };
                foreach (int? v in dsVung)
                {
                    List<Cum> clCur = coVung ? bent.Where(c => c.Vung == v).ToList() : bent.ToList();
                    List<Hinh> sCur = coVung ? pending.Where(s => s.Vung == v).ToList() : pending.ToList();
                    if (clCur.Count == 0 || sCur.Count == 0) continue;
                    int m = clCur.Count, k = sCur.Count, n = Math.Max(m, k);
                    var mat = new double[n, n];
                    for (int r = 0; r < n; r++)
                        for (int c = 0; c < n; c++)
                            mat[r, c] = (r < m && c < k)
                                ? (DHinhCum(sCur[c], clCur[r].HinhIds, tatCa, theoId, dsCat, maxTia) ?? ChanVatCan)
                                : Gia;
                    int[] ans = Hungarian(mat, n);
                    for (int r = 0; r < m; r++)
                    {
                        int c = ans[r];
                        if (c >= 0 && c < k && mat[r, c] < NguongGhep)
                        {
                            Hinh s = sCur[c];
                            clCur[r].HinhIds.Insert(0, s.Id);   // cùng đối tượng trong bent
                            pending.RemoveAll(x => x.Id == s.Id);
                            kq.Add(new LanGhep { Hinh = s.Id, Tam = clCur[r].Tid, D = mat[r, c], Loai = 1 });
                        }
                    }
                }
            }

            // 3B: bảng khoảng cách 1 lần; mỗi lượt lấy cặp nhỏ nhất, chỉ tính lại cụm vừa nhận hình
            if (pending.Count > 0 && bent.Count > 0)
            {
                var bang = new List<KeyValuePair<Hinh, List<KeyValuePair<int, double>>>>();
                foreach (Hinh b in pending)
                {
                    var hang = new List<KeyValuePair<int, double>>();
                    if (!coVung || b.Vung != null)
                        foreach (Cum cl in bent)
                        {
                            if (coVung && b.Vung != cl.Vung) continue;
                            double? d = DHinhCum(b, cl.HinhIds, tatCa, theoId, dsCat, maxTia);
                            if (d != null) hang.Add(new KeyValuePair<int, double>(cl.Tid, d.Value));
                        }
                    bang.Add(new KeyValuePair<Hinh, List<KeyValuePair<int, double>>>(b, hang));
                }
                while (true)
                {
                    Hinh bestB = null;
                    int bestT = 0;
                    double bestD = 0;
                    foreach (var r in bang)
                        foreach (var p in r.Value)
                            if (bestB == null || p.Value < bestD) { bestB = r.Key; bestT = p.Key; bestD = p.Value; }
                    if (bestB == null) break;

                    pending.RemoveAll(x => x.Id == bestB.Id);
                    Cum moi = bent.First(c => c.Tid == bestT);
                    moi.HinhIds.Insert(0, bestB.Id);
                    bang.RemoveAll(r => r.Key.Id == bestB.Id);
                    kq.Add(new LanGhep { Hinh = bestB.Id, Tam = bestT, D = bestD, Loai = 2 });

                    foreach (var r in bang)
                    {
                        r.Value.RemoveAll(p => p.Key == bestT);
                        bool cungVung = !coVung || (r.Key.Vung != null && r.Key.Vung == moi.Vung);
                        double? d2 = cungVung ? DHinhCum(r.Key, moi.HinhIds, tatCa, theoId, dsCat, maxTia) : null;
                        if (d2 != null) r.Value.Add(new KeyValuePair<int, double>(bestT, d2.Value));
                    }
                }
            }
            return kq;
        }

        /// <summary>gro--d-hinh-cum: khoảng cách chiếu tia ngắn nhất từ b tới các hình của cụm, null nếu không chiếu được.</summary>
        public static double? DHinhCum(Hinh b, IList<int> cumIds, IList<Hinh> tatCa, IDictionary<int, Hinh> theoId,
                                       IList<VetCat> dsCat, double maxTia)
        {
            var cumHinh = cumIds.Select(id => theoId[id]).ToList();
            if (!cumHinh.Any(a => ChieuDuoc(b, a, null, dsCat, maxTia) != null)) return null;
            var ids = new HashSet<int>(cumIds);
            var vatCan = tatCa.Where(s => s.Id != b.Id && !ids.Contains(s.Id)).ToList();
            double? m = null;
            foreach (Hinh a in cumHinh)
            {
                double? d = ChieuDuoc(b, a, vatCan, dsCat, maxTia);
                if (d != null && (m == null || d < m)) m = d;
            }
            return m;
        }

        /// <summary>gro--ray-chieu-duoc-p.</summary>
        public static double? ChieuDuoc(Hinh b, Hinh a, IList<Hinh> vatCan, IList<VetCat> dsCat, double maxTia)
        {
            if (dsCat != null && dsCat.Count > 0 && TachTuNhau(b, a, dsCat)) return null;
            return ChieuTia(b, a, vatCan, maxTia);
        }

        /// <summary>gro--tach-tu-nhau-p (mục 152): b1, b2 là hai nửa của cùng một lần cắt tấm gộp.</summary>
        public static bool TachTuNhau(Hinh b1, Hinh b2, IList<VetCat> dsCat)
        {
            foreach (VetCat c in dsCat)
            {
                double xd = c.XDiv, g = c.Gap, x1 = c.X1, y1 = c.Y1, x2 = c.X2 + c.Gap, y2 = c.Y2;
                if (b1.Y1 >= y1 - 2.0 && b1.Y2 <= y2 + 2.0 && b2.Y1 >= y1 - 2.0 && b2.Y2 <= y2 + 2.0
                    && ((b1.X1 >= x1 - 2.0 && b1.X2 <= xd + 1.0 && b2.X1 >= xd + g - 1.0 && b2.X2 <= x2 + 2.0)
                        || (b2.X1 >= x1 - 2.0 && b2.X2 <= xd + 1.0 && b1.X1 >= xd + g - 1.0 && b1.X2 <= x2 + 2.0)))
                    return true;
            }
            return false;
        }

        /// <summary>gro--ray-chieu-goc: tia ngang / dọc từ b tới a không bị hình khác chắn; trả khoảng cách nhỏ nhất.</summary>
        public static double? ChieuTia(Hinh b, Hinh a, IList<Hinh> vatCan, double maxTia)
        {
            if (maxTia <= 0.0) maxTia = MacDinhTia;
            var hopLe = new List<double>();
            double yMin = Math.Max(b.Y1, a.Y1), yMax = Math.Min(b.Y2, a.Y2);
            if (yMax - yMin >= 2.0)
            {
                if (a.X1 >= b.X2 - 1.0)
                {
                    double d = Math.Max(0.0, a.X1 - b.X2);
                    if (d <= maxTia && !Chan(vatCan, b, a, c => Math.Min(c.Y2, yMax) - Math.Max(c.Y1, yMin) > 1.0
                                                                && c.X1 < a.X1 - 0.5 && c.X2 > b.X2 + 0.5))
                        hopLe.Add(d);
                }
                if (b.X1 >= a.X2 - 1.0)
                {
                    double d = Math.Max(0.0, b.X1 - a.X2);
                    if (d <= maxTia && !Chan(vatCan, b, a, c => Math.Min(c.Y2, yMax) - Math.Max(c.Y1, yMin) > 1.0
                                                                && c.X1 < b.X1 - 0.5 && c.X2 > a.X2 + 0.5))
                        hopLe.Add(d);
                }
            }
            double xMin = Math.Max(b.X1, a.X1), xMax = Math.Min(b.X2, a.X2);
            if (xMax - xMin >= 2.0)
            {
                if (a.Y1 >= b.Y2 - 1.0)
                {
                    double d = Math.Max(0.0, a.Y1 - b.Y2);
                    if (d <= maxTia && !Chan(vatCan, b, a, c => Math.Min(c.X2, xMax) - Math.Max(c.X1, xMin) > 1.0
                                                                && c.Y1 < a.Y1 - 0.5 && c.Y2 > b.Y2 + 0.5))
                        hopLe.Add(d);
                }
                if (b.Y1 >= a.Y2 - 1.0)
                {
                    double d = Math.Max(0.0, b.Y1 - a.Y2);
                    if (d <= maxTia && !Chan(vatCan, b, a, c => Math.Min(c.X2, xMax) - Math.Max(c.X1, xMin) > 1.0
                                                                && c.Y1 < b.Y1 - 0.5 && c.Y2 > a.Y2 + 0.5))
                        hopLe.Add(d);
                }
            }
            return hopLe.Count > 0 ? hopLe.Min() : (double?)null;
        }

        /// <summary>Có vật cản chắn tia không. Bỏ vật cản trùng hộp với b hoặc a (equal ... 0.001 của lisp).</summary>
        private static bool Chan(IList<Hinh> vatCan, Hinh b, Hinh a, Func<Hinh, bool> chan)
        {
            if (vatCan == null) return false;
            foreach (Hinh c in vatCan)
            {
                if (TrungHop(c, b) || TrungHop(c, a)) continue;
                if (chan(c)) return true;
            }
            return false;
        }

        private static bool TrungHop(Hinh p, Hinh q) =>
            Math.Abs(p.X1 - q.X1) <= 0.001 && Math.Abs(p.Y1 - q.Y1) <= 0.001 &&
            Math.Abs(p.X2 - q.X2) <= 0.001 && Math.Abs(p.Y2 - q.Y2) <= 0.001;

        /// <summary>gro--hungarian (gán cực tiểu O(n^3), chỉ số 1..n như lisp). Trả cột gán cho từng hàng (-1 = không).</summary>
        public static int[] Hungarian(double[,] a, int n)
        {
            const double Inf = 10000000000.0;
            var u = new double[n + 1];
            var v = new double[n + 1];
            var p = new int[n + 1];
            var way = new int[n + 1];
            for (int i = 1; i <= n; i++)
            {
                p[0] = i;
                int j0 = 0;
                var minv = Enumerable.Repeat(Inf, n + 1).ToArray();
                var used = new bool[n + 1];
                do
                {
                    used[j0] = true;
                    int i0 = p[j0], j1 = 0;
                    double delta = Inf;
                    for (int j = 1; j <= n; j++)
                    {
                        if (used[j]) continue;
                        double cur = a[i0 - 1, j - 1] - (u[i0] + v[j]);
                        if (cur < minv[j]) { minv[j] = cur; way[j] = j0; }
                        if (minv[j] < delta) { delta = minv[j]; j1 = j; }
                    }
                    for (int j = 0; j <= n; j++)
                    {
                        if (used[j]) { u[p[j]] += delta; v[j] -= delta; }
                        else minv[j] -= delta;
                    }
                    j0 = j1;
                } while (p[j0] != 0);
                do
                {
                    int j1 = way[j0];
                    p[j0] = p[j1];
                    j0 = j1;
                } while (j0 != 0);
            }
            var ans = Enumerable.Repeat(-1, n).ToArray();
            for (int j = 1; j <= n; j++)
                if (p[j] != 0) ans[p[j] - 1] = j - 1;
            return ans;
        }
    }
}
