using System;
using System.Collections.Generic;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>Một nét 0-KAKOU đầu vào: chỉ số (do lớp CAD gán) + hộp bao.</summary>
    public sealed class NetKakou
    {
        public int Idx;
        public Hop Bb;
        public NetKakou(int idx, Hop bb) { Idx = idx; Bb = bb; }
    }

    /// <summary>Một hình (shape primitive): (id (thành viên) hộp tâm) như lisp.</summary>
    public sealed class HinhPrim
    {
        public int Id;
        public List<int> ThanhVien = new List<int>();
        public Hop Bb;
        public double DienTich;
        public double Cx => (Bb.X1 + Bb.X2) / 2.0;
        public double Cy => (Bb.Y1 + Bb.Y2) / 2.0;
    }

    /// <summary>
    /// Mục 172 (GĐ 5a): chép <c>gro--quet-hinh-primitives</c> (lisp) — gom nét 0-KAKOU thành hình.
    /// Thuần (không gọi ZWCAD). Lớp CAD: <c>CadServices/QuetHinhCad.cs</c>.
    ///
    /// Thứ tự đầu vào = thứ tự lisp duyệt = THỨ TỰ CSDL (ssget "X" của ZWCAD trả ngược CSDL, lisp
    /// cons lại -> xuôi CSDL). Kết quả gom KHÔNG phụ thuộc thứ tự (thành phần liên thông); thứ tự
    /// chỉ quyết định id các hình CÙNG diện tích (vl-sort ổn định) — đã kiểm bằng so A/B thật.
    /// </summary>
    public static class QuetHinh
    {
        public sealed class KetQua
        {
            public List<HinhPrim> Hinh = new List<HinhPrim>();
            /// <summary>Hình chứa ≥ 2 điểm chèn ghi chú = khung vẽ nhầm trên 0-KAKOU (gro--hinh-loi-khung).</summary>
            public List<HinhPrim> LoiKhung = new List<HinhPrim>();
            public int SoNetVao, SoNetSauLoc;
        }

        /// <summary>gro--khoang-cach-bb</summary>
        public static double KhoangCachBb(Hop a, Hop b)
        {
            double dx = Math.Max(0.0, Math.Max(a.X1 - b.X2, b.X1 - a.X2));
            double dy = Math.Max(0.0, Math.Max(a.Y1 - b.Y2, b.Y1 - a.Y2));
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static Hop Gop(IEnumerable<Hop> ds)
        {
            Hop r = null;
            foreach (Hop b in ds)
            {
                if (b == null) continue;
                r = r == null ? new Hop(b.X1, b.Y1, b.X2, b.Y2)
                              : new Hop(Math.Min(r.X1, b.X1), Math.Min(r.Y1, b.Y1), Math.Max(r.X2, b.X2), Math.Max(r.Y2, b.Y2));
            }
            return r;
        }

        /// <summary>Mục 222: điểm bị nét của hình chặn cả 4 phía (trên, dưới, trái, phải) — xét hộp bao từng nét.</summary>
        public static bool BiBaoQuanh(Diem p, HinhPrim s, IDictionary<int, Hop> bbTheoIdx)
        {
            bool tren = false, duoi = false, trai = false, phai = false;
            foreach (int i in s.ThanhVien)
            {
                if (!bbTheoIdx.TryGetValue(i, out Hop b)) continue;
                if (b.X1 <= p.X && p.X <= b.X2) { if (b.Y1 > p.Y) tren = true; if (b.Y2 < p.Y) duoi = true; }
                if (b.Y1 <= p.Y && p.Y <= b.Y2) { if (b.X2 < p.X) trai = true; if (b.X1 > p.X) phai = true; }
                if (tren && duoi && trai && phai) return true;
            }
            return false;
        }

        /// <param name="nets">nét 0-KAKOU (đã bỏ paper space), thứ tự như lisp duyệt</param>
        /// <param name="kBoxes">hộp các khung phạm vi (null / rỗng = quét tự do)</param>
        /// <param name="diemGhiChu">điểm chèn các ghi chú (null / rỗng = bỏ bước lọc khung nhầm)</param>
        public static KetQua Gom(IList<NetKakou> nets, IList<Hop> kBoxes, IList<Diem> diemGhiChu)
        {
            var kq = new KetQua { SoNetVao = nets.Count };
            bool coKhung = kBoxes != null && kBoxes.Count > 0;

            // B2. Gioi han trong khung (no 5mm) + bo chinh khung bao (4 mep lech <= 10mm)
            var items = new List<NetKakou>();
            foreach (NetKakou it in nets)
            {
                Hop bb = it.Bb;
                if (coKhung)
                {
                    bool trong = kBoxes.Any(kb => bb.X1 >= kb.X1 - 5.0 && bb.Y1 >= kb.Y1 - 5.0 &&
                                                  bb.X2 <= kb.X2 + 5.0 && bb.Y2 <= kb.Y2 + 5.0);
                    if (!trong) continue;
                    bool vien = kBoxes.Any(kb => Math.Abs(bb.X1 - kb.X1) <= 10.0 && Math.Abs(bb.Y1 - kb.Y1) <= 10.0 &&
                                                 Math.Abs(bb.X2 - kb.X2) <= 10.0 && Math.Abs(bb.Y2 - kb.Y2) <= 10.0);
                    if (vien) continue;
                }
                items.Add(it);
            }
            // Bo net rieng le la MEP khung bao (khung no thanh 4 LINE): dai > 40% canh, lech mep <= 5mm
            if (coKhung && items.Count > 0)
            {
                items = items.Where(it =>
                {
                    Hop ib = it.Bb;
                    double iw = ib.W, ih = ib.H;
                    foreach (Hop kb in kBoxes)
                    {
                        double kw = kb.W, kh = kb.H;
                        if (iw > 0.4 * kw && (Math.Abs(ib.Y1 - kb.Y1) <= 5.0 || Math.Abs(ib.Y2 - kb.Y2) <= 5.0)) return false;
                        if (ih > 0.4 * kh && (Math.Abs(ib.X1 - kb.X1) <= 5.0 || Math.Abs(ib.X2 - kb.X2) <= 5.0)) return false;
                    }
                    return true;
                }).ToList();
            }
            kq.SoNetSauLoc = items.Count;

            // Gom net cham nhau (khe hop <= 2.5mm) = THANH PHAN LIEN THONG. Lisp quet lap n^2 x nhom;
            // dieu kien "cham hop nhom" bi dieu kien "cham 1 thanh vien" bao ham -> union-find cho cung ket qua.
            int n = items.Count;
            var cha = new int[n];
            for (int i = 0; i < n; i++) cha[i] = i;
            int Goc(int x) { while (cha[x] != x) { cha[x] = cha[cha[x]]; x = cha[x]; } return x; }
            // sap theo X1 de cat tia quet (chi so cap co the cham)
            int[] thuTu = Enumerable.Range(0, n).OrderBy(i => items[i].Bb.X1).ToArray();
            for (int a = 0; a < n; a++)
            {
                Hop ba = items[thuTu[a]].Bb;
                for (int b = a + 1; b < n; b++)
                {
                    Hop bb = items[thuTu[b]].Bb;
                    if (bb.X1 - ba.X2 > 2.5) break;
                    if (KhoangCachBb(ba, bb) <= 2.5)
                    {
                        int ra = Goc(thuTu[a]), rb = Goc(thuTu[b]);
                        if (ra != rb) cha[ra] = rb;
                    }
                }
            }
            // Thu tu nhom = thu tu PHAN TU DAU TIEN cua nhom trong items (nhu hat giong lisp)
            var nhom = new Dictionary<int, List<int>>();
            var thuTuNhom = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int r = Goc(i);
                if (!nhom.TryGetValue(r, out List<int> l)) { nhom[r] = l = new List<int>(); thuTuNhom.Add(r); }
                l.Add(i);
            }

            // Shape so bo: bo hinh qua nho (w <= 3 va h <= 3)
            var shapes = new List<HinhPrim>();
            foreach (int r in thuTuNhom)
            {
                List<int> l = nhom[r];
                Hop gbb = Gop(l.Select(i => items[i].Bb));
                double w = gbb.W, h = gbb.H;
                if (w > 3.0 || h > 3.0)
                    shapes.Add(new HinhPrim { ThanhVien = l.Select(i => items[i].Idx).ToList(), Bb = gbb, DienTich = w * h });
            }

            // Bo hinh chua >= 2 diem chen ghi chu (khung ve nham tren 0-KAKOU)
            if (diemGhiChu != null && diemGhiChu.Count > 0)
            {
                var bbTheoIdx = new Dictionary<int, Hop>();
                foreach (NetKakou it in items) bbTheoIdx[it.Idx] = it.Bb;
                var con = new List<HinhPrim>();
                foreach (HinhPrim s in shapes)
                {
                    // Muc 222: ghi chu chi tinh la "trong" khi BI NET CUA HINH CHAN CA 4 PHIA (nhu nam trong
                    // khung that). Chi xet hop bao thi tam gop co phan nho cao o dau (130837-1: thanh 100mm,
                    // dau cao 318.8) nuot 2 ghi chu ghi PHIA TREN thanh -> tam bi bo, khong cat duoc.
                    int nIn = diemGhiChu.Count(p => p.X >= s.Bb.X1 && p.X <= s.Bb.X2 && p.Y >= s.Bb.Y1 && p.Y <= s.Bb.Y2
                                                    && BiBaoQuanh(p, s, bbTheoIdx));
                    if (nIn >= 2) kq.LoiKhung.Add(s); else con.Add(s);
                }
                shapes = con;
            }

            // Sap dien tich giam dan, ON DINH (vl-sort cua lisp cung on dinh - so A/B ban ve that 28/10,
            // muc 172: 3 cap hinh hoa dien tich khop het khi doc net THEO THU TU CSDL, xem QuetHinhCad).
            shapes = shapes.OrderByDescending(s => s.DienTich).ToList();
            var giu = new List<HinhPrim>();
            foreach (HinhPrim s in shapes)
                if (!giu.Any(m => m.Bb.Chua(s.Bb))) giu.Add(s);
            int id = 1;
            foreach (HinhPrim s in giu) { s.Id = id++; kq.Hinh.Add(s); }
            return kq;
        }
    }
}
