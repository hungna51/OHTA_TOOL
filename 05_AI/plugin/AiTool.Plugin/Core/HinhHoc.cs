using System;
using System.Globalization;

namespace AiTool.Core
{
    public static class HinhHoc
    {
        /// <summary>Làm tròn 2 chữ số thập phân, bỏ số 0 thừa, dấu chấm thập phân; -0 thành "0".</summary>
        public static string Tron(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v) || Math.Abs(v) > 1e15)
                return v.ToString("0.##", CultureInfo.InvariantCulture);
            // Qua decimal (giữ 15 chữ số có nghĩa) để 1.005 làm tròn thành 1.01 như người đọc mong đợi,
            // không bị 1.00499999... của double kéo xuống.
            decimal r = Math.Round((decimal)v, 2, MidpointRounding.AwayFromZero);
            if (r == 0m) r = 0m; // bỏ -0
            return r.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Đoạn polyline từ (x1,y1) tới (x2,y2) với bulge = tan(góc ôm / 4); bulge dương = cung
        /// đi ngược chiều kim đồng hồ. bulge 0 = đoạn thẳng.
        /// </summary>
        public static DoanPl TuBulge(double x1, double y1, double x2, double y2, double bulge)
        {
            var d = new DoanPl { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
            double dx = x2 - x1, dy = y2 - y1;
            double c = Math.Sqrt(dx * dx + dy * dy);
            if (Math.Abs(bulge) < 1e-9 || c < 1e-12)
                return d;

            // Tâm = trung điểm dây cung + h theo pháp tuyến trái; h = c(1 - b^2) / (4b) (có dấu).
            double h = c * (1 - bulge * bulge) / (4 * bulge);
            d.LaCung = true;
            d.Cx = (x1 + x2) / 2 - dy / c * h;
            d.Cy = (y1 + y2) / 2 + dx / c * h;
            d.R = Math.Abs(c * (1 + bulge * bulge) / (4 * bulge));
            return d;
        }
    }
}
