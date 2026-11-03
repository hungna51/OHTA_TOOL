using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>Một entity Model Space (phần tử của ss-model lisp, đúng thứ tự ssname).</summary>
    public sealed class EntXc
    {
        public string H;
        public string Layer;
        /// <summary>Có XData GRO_CHUP_COPY (bản chụp).</summary>
        public bool ChupCopy;
        /// <summary>LWPOLYLINE / POLYLINE (phép thử khung phạm vi, mục 135).</summary>
        public bool LaPoly;
        /// <summary>Hộp bao TRƯỚC khi dời; null = gro--bbox nil.</summary>
        public Hop Bb;
    }

    /// <summary>Một cụm cần xếp (phần tử cands của lisp, đã sắp nhóm → bango).</summary>
    public sealed class CumXc
    {
        public int Tid, Nhom;
        /// <summary>(and (equal nhom 0) (= (length s-list) 1)) — cột phải chừa chỗ bản nghịch (mục 112).</summary>
        public bool Mirror1;
        public Hop Bb;
        public List<string> Ents = new List<string>();
    }

    /// <summary>
    /// Mục 178 (GĐ 6a): chép phần nặng của <c>gro--xep-cum-phai</c> — bố cục cột bên phải
    /// (<c>gro--bo-cuc-cot</c>: tránh vật cản + xếp dọc theo nhóm), dời các cụm (bước 7) và kiểm đối tượng
    /// còn sót ở chỗ cụm cũ / dời theo nét dim chưa gán (bước 9, mục 134 / 141). Thuần, không gọi ZWCAD:
    /// mọi phép dời được GHI vào bảng độ dời, hộp bao "hiện tại" = hộp gốc + độ dời — y như lisp đọc lại
    /// bản vẽ sau mỗi lần vla-move. Lớp CAD áp độ dời một lần ở cuối (phép tịnh tiến giao hoán).
    /// </summary>
    public static class XepCum
    {
        public const double KheNgoai = 1000.0, KheCum = 50.0, KheNhom = 300.0, HNhan = 20.0;

        public sealed class ODat { public double Dx, Dy; public Hop BbMoi; }
        public sealed class Nhan { public int Nhom; public double X, Y; }

        public sealed class KetQua
        {
            public double XL;
            public bool DaNe;
            public List<ODat> Dat = new List<ODat>();
            public List<Nhan> Nhans = new List<Nhan>();
            public int NSot, NDimTheo;
            /// <summary>ds-sot đúng thứ tự lisp (cons: cái tìm sau đứng trước).</summary>
            public List<string> Sot = new List<string>();
            /// <summary>Tổng độ dời từng entity (handle) — lớp CAD áp vào bản vẽ.</summary>
            public Dictionary<string, double[]> DoDoi = new Dictionary<string, double[]>(StringComparer.Ordinal);
        }

        private static string U(string s) => PhanLoaiNet.HoaAscii(s ?? "");

        /// <summary>Bộ lọc chung bước 4 / 6 / 9: có layer, không phải bản chụp, layer không *_CHUP / CANH_BAO_* / DEFPOINTS.</summary>
        public static bool LocChung(EntXc e)
        {
            if (e.Layer == null || e.ChupCopy) return false;
            string l = U(e.Layer);
            return !l.EndsWith("_CHUP", StringComparison.Ordinal) && !l.StartsWith("CANH_BAO_", StringComparison.Ordinal) && l != "DEFPOINTS";
        }

        private static Hop Dich(Hop b, double[] d) => b == null || d == null ? b : new Hop(b.X1 + d[0], b.Y1 + d[1], b.X2 + d[0], b.Y2 + d[1]);

        private static void Doi(KetQua kq, string h, double dx, double dy)
        {
            if (!kq.DoDoi.TryGetValue(h, out double[] d)) kq.DoDoi[h] = d = new double[2];
            d[0] += dx; d[1] += dy;
        }

        /// <summary>gro--la-khung-pham-vi-p (mục 135) trên hộp bao hiện tại.</summary>
        public static bool LaKhungPhamVi(EntXc e, Hop bbHienTai, ISet<string> khungBo, IList<Hop> hopKhungBo)
        {
            if (khungBo != null && khungBo.Contains(e.H)) return true;
            if (hopKhungBo == null || hopKhungBo.Count == 0 || !e.LaPoly || bbHienTai == null) return false;
            foreach (Hop k in hopKhungBo)
                if (k != null && k.Bang(bbHienTai, 0.5)) return true;
            return false;
        }

        public static KetQua Chay(IList<EntXc> ss, IList<CumXc> cums, Hop bbGoc, ISet<string> esKhac,
                                  ISet<string> khungBo, IList<Hop> hopKhungBo, double mirrorKhe, double mirrorLe)
        {
            var kq = new KetQua();
            double ymax = bbGoc.Y2;
            double xl = bbGoc.X2 + KheNgoai, wCol = 0.0, totalH = 0.0;

            // gro--bo-cuc-cot: be rong cot + tong chieu cao du kien
            foreach (CumXc c in cums)
            {
                double w = c.Bb.X2 - c.Bb.X1, h = c.Bb.Y2 - c.Bb.Y1;
                wCol = Math.Max(wCol, c.Nhom == 0 && c.Mirror1 ? 2.0 * w + mirrorKhe + 2.0 * mirrorLe : w);
                totalH += h + KheCum;
            }
            totalH += 4 * KheNhom;

            // tranh vat can trong vung cot (X_L tang dan theo thu tu ss -> giu dung thu tu)
            foreach (EntXc e in ss)
            {
                if (!LocChung(e) || e.Bb == null) continue;
                Hop b = e.Bb;
                if (b.X2 >= xl && b.X1 <= xl + wCol + 1000.0 && b.Y2 >= ymax - totalH - 500.0 && b.Y1 <= ymax + 500.0)
                    if (b.X2 + KheNgoai > xl) { xl = b.X2 + KheNgoai; kq.DaNe = true; }
            }
            kq.XL = xl;

            // xep doc tung cum tu tren xuong
            double curY = ymax;
            int curNhom = -1;
            foreach (CumXc c in cums)
            {
                double w = c.Bb.X2 - c.Bb.X1, h = c.Bb.Y2 - c.Bb.Y1;
                if (c.Nhom != curNhom)
                {
                    curY -= curNhom != -1 ? KheNhom : 10.0;
                    kq.Nhans.Add(new Nhan { Nhom = c.Nhom, X = xl, Y = curY });
                    curY -= HNhan + 30.0;
                    curNhom = c.Nhom;
                }
                else curY -= KheCum;
                double x1 = xl, y2 = curY, y1 = y2 - h;
                kq.Dat.Add(new ODat { Dx = x1 - c.Bb.X1, Dy = y1 - c.Bb.Y1, BbMoi = new Hop(x1, y1, x1 + w, y2) });
                curY = y1;
            }

            // 7. doi cac entity cua cum (moi lan xuat hien = mot lan vla-move)
            for (int i = 0; i < cums.Count; i++)
                foreach (string h in cums[i].Ents) Doi(kq, h, kq.Dat[i].Dx, kq.Dat[i].Dy);

            // 9. kiem con sot tai cho cum cu; dim 1-SUNPOU nam tron trong hop cu -> doi theo cum (muc 141)
            var sot = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < cums.Count; i++)
            {
                Hop old = cums[i].Bb;
                double cx1 = old.X1 + 5.0, cy1 = old.Y1 + 5.0, cx2 = old.X2 - 5.0, cy2 = old.Y2 - 5.0;
                if (!(cx1 < cx2 && cy1 < cy2)) continue;
                foreach (EntXc e in ss)
                {
                    if (!LocChung(e) || U(e.Layer) == "GRO_XEP_NHOM") continue;
                    kq.DoDoi.TryGetValue(e.H, out double[] d);
                    Hop b = Dich(e.Bb, d);
                    if (LaKhungPhamVi(e, b, khungBo, hopKhungBo)) continue;
                    if (esKhac != null && esKhac.Contains(e.H)) continue;
                    if (b == null) continue;
                    double cx = (b.X1 + b.X2) / 2.0, cy = (b.Y1 + b.Y2) / 2.0;
                    if (!(cx >= cx1 && cx <= cx2 && cy >= cy1 && cy <= cy2)) continue;
                    if (sot.Contains(e.H)) continue;
                    if (U(e.Layer) == "1-SUNPOU" &&
                        b.X1 >= old.X1 - 1.0 && b.Y1 >= old.Y1 - 1.0 && b.X2 <= old.X2 + 1.0 && b.Y2 <= old.Y2 + 1.0)
                    {
                        Doi(kq, e.H, kq.Dat[i].Dx, kq.Dat[i].Dy);
                        kq.NDimTheo++;
                    }
                    else
                    {
                        kq.NSot++;
                        sot.Add(e.H);
                        kq.Sot.Add(e.H);
                    }
                }
            }
            kq.Sot.Reverse();
            return kq;
        }
    }
}
