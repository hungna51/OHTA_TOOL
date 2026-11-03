using System;
using System.Collections.Generic;
using System.Globalization;

namespace ZwCadPlugin.Core
{
    /// <summary>Một nét đầu vào của dò vòng kín (thứ tự = thứ tự ssname lisp).</summary>
    public sealed class NetVk
    {
        public string Handle;
        /// <summary>Tên DXF ("LINE", "ARC", "LWPOLYLINE", "POLYLINE", "SPLINE", ...).</summary>
        public string Loai;
        public string Layer;
        /// <summary>Polyline có cờ đóng (group 70 bit 1).</summary>
        public bool Kin;
        /// <summary>Lấy được điểm đầu / cuối (vlax-curve-getStartPoint / getEndPoint không lỗi).</summary>
        public bool CoDau;
        public double X1, Y1, X2, Y2;
    }

    /// <summary>
    /// Mục 177 (GĐ 3c): chép bước 1b của <c>gro--detect-geom-dims</c> + <c>gro--es-vong-kin</c> (mục 143/145) —
    /// tìm nét LINE / ARC / polyline HỞ / SPLINE trên layer gốc ghép thành VÒNG KÍN biên dạng (tỉa dần nét có
    /// đầu mút treo bậc 1), rồi bỏ chúng khỏi lines-cont / arcs-cont. Thuần, không gọi ZWCAD.
    /// Thứ tự kết quả giữ y lisp: vk-geo dựng bằng cons (đảo ss), mỗi lượt tỉa lại cons (đảo thêm một lần).
    /// </summary>
    public static class VongKin
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly string[] LoaiNhan = { "LINE", "ARC", "LWPOLYLINE", "POLYLINE", "SPLINE" };

        /// <summary>
        /// gro--khoa-diem: (rtos x 2 1) "," (rtos y 2 1). Làm tròn 1 số lẻ trên dạng 15 chữ số có nghĩa, nửa
        /// ra xa 0 (kiểu làm tròn "nhìn thấy" của CAD). Chỉ cần: hai điểm trùng nhau -> cùng khoá.
        /// </summary>
        public static string Khoa(double x, double y) => Tron1(x) + "," + Tron1(y);

        public static string Tron1(double v)
        {
            decimal d;
            if (!decimal.TryParse(v.ToString("G15", Inv), NumberStyles.Float, Inv, out d)) return v.ToString("F1", Inv);
            d = Math.Round(d, 1, MidpointRounding.AwayFromZero);
            if (d == 0m) d = 0m;      // bo dau am cua -0.0
            return d.ToString("F1", Inv);
        }

        private sealed class Rec
        {
            public NetVk N;
            public string K1, K2;
        }

        /// <summary>gro--es-vong-kin: es theo đúng thứ tự lisp truyền vào. Trả các nét thuộc vòng kín, thứ tự lisp.</summary>
        public static List<NetVk> EsVongKin(IList<NetVk> es)
        {
            var recs = new List<Rec>();
            foreach (NetVk n in es)
            {
                if (Array.IndexOf(LoaiNhan, n.Loai) < 0) continue;
                if ((n.Loai == "LWPOLYLINE" || n.Loai == "POLYLINE") && n.Kin) continue;
                if (!n.CoDau) continue;
                recs.Add(new Rec { N = n, K1 = Khoa(n.X1, n.Y1), K2 = Khoa(n.X2, n.Y2) });
            }
            recs.Reverse();                                   // cons
            bool doi = true;
            while (doi && recs.Count > 0)
            {
                doi = false;
                var deg = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (Rec r in recs)
                {
                    deg[r.K1] = deg.TryGetValue(r.K1, out int a) ? a + 1 : 1;
                    deg[r.K2] = deg.TryGetValue(r.K2, out int b) ? b + 1 : 1;
                }
                var outl = new List<Rec>();
                foreach (Rec r in recs)
                {
                    if (deg[r.K1] > 1 && deg[r.K2] > 1) outl.Add(r);
                    else doi = true;
                }
                outl.Reverse();                               // cons
                recs = outl;
            }
            var kq = new List<NetVk>(recs.Count);
            foreach (Rec r in recs) kq.Add(r.N);
            return kq;
        }

        /// <summary>Bước 1b: lọc nét của ss trên layer gốc (vk-geo = cons -> đảo), dò vòng kín.</summary>
        public static List<NetVk> TrenLayer(string layGoc, IList<NetVk> ss)
        {
            string lh = PhanLoaiNet.HoaAscii(layGoc ?? "");
            var es = new List<NetVk>();
            foreach (NetVk n in ss) if (n.Layer != null && PhanLoaiNet.HoaAscii(n.Layer) == lh) es.Add(n);
            es.Reverse();
            return EsVongKin(es);
        }
    }
}
