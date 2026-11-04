using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    /// <summary>
    /// Đóng dữ liệu một khung thành chữ gọn gửi AI (mỗi đối tượng một dòng; định dạng giải thích
    /// trong lời dặn). Text/MText trên layer F-name chỉ vào dòng FNAME, không lặp thành dòng T.
    /// </summary>
    public static class DongChu
    {
        public const string LayerFname = "2-F-NAME";

        public static string Lo(IList<KhungDuLieu> lo) => string.Join("\n\n", lo.Select(Khung));

        public static string Khung(KhungDuLieu k)
        {
            var sb = new StringBuilder();
            sb.Append("#KHUNG ").Append(k.SoThuTu)
              .Append(" rong=").Append(T(k.MaxX - k.MinX))
              .Append(" cao=").Append(T(k.MaxY - k.MinY)).Append('\n');

            List<string> fname = DongFname(k);
            sb.Append("FNAME: ").Append(fname.Count == 0 ? "(khong co)" : string.Join(" | ", fname)).Append('\n');
            if (DemN(fname) == 1)
            {
                FnameRecord r = FnameParser.Parse(fname);
                sb.Append("FNAME_TACH: N=").Append(r.N).Append("; M=").Append(r.M).Append("; T=").Append(r.T)
                  .Append("; Q=").Append(r.Q).Append("; Z=").Append(r.Z)
                  .Append("; BE=").Append(string.Join(" ; ", r.BendLines)).Append('\n');
            }

            foreach (DoiTuong d in k.DoiTuong)
            {
                if (LaFname(d)) continue;
                string dong = Dong(d);
                if (dong != null) sb.Append(dong).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>Tên hiện trên cảnh báo: "N=..." khi khung có đúng một dòng N=, ngược lại "Khung n".</summary>
        public static string TenKhung(KhungDuLieu k)
        {
            List<string> fname = DongFname(k);
            if (DemN(fname) == 1)
            {
                string n = fname.First(LaDongN).Trim();
                return "N=" + n.Substring(2).Trim();
            }
            return "Khung " + k.SoThuTu;
        }

        private static bool LaFname(DoiTuong d) =>
            d.Loai == LoaiDoiTuong.Text && string.Equals(d.Layer, LayerFname, StringComparison.OrdinalIgnoreCase);

        private static bool LaDongN(string s) => s.TrimStart().StartsWith("N=", StringComparison.OrdinalIgnoreCase);

        private static int DemN(List<string> dong) => dong.Count(LaDongN);

        /// <summary>Các dòng F-name sạch, theo thứ tự trên xuống (Y giảm) rồi trái sang (X tăng).</summary>
        private static List<string> DongFname(KhungDuLieu k) =>
            k.DoiTuong.Where(LaFname)
             .OrderByDescending(d => Math.Round(d.Y1, 2)).ThenBy(d => d.X1)
             .SelectMany(d => FnameParser.SplitAndCleanMText(d.NoiDung))
             .ToList();

        private static string Dong(DoiTuong d)
        {
            switch (d.Loai)
            {
                case LoaiDoiTuong.Line:
                    return "L " + d.Layer + " " + P(d.X1, d.Y1) + " " + P(d.X2, d.Y2);
                case LoaiDoiTuong.Circle:
                    return "C " + d.Layer + " " + P(d.Cx, d.Cy) + " d=" + T(2 * d.R);
                case LoaiDoiTuong.Arc:
                    return "A " + d.Layer + " " + P(d.Cx, d.Cy) + " r=" + T(d.R) + " " + T(d.GocDau) + "->" + T(d.GocCuoi);
                case LoaiDoiTuong.Polyline:
                    return DongPl(d);
                case LoaiDoiTuong.Text:
                    string chu = string.Join(" / ", FnameParser.SplitAndCleanMText(d.NoiDung));
                    return "T " + d.Layer + " " + P(d.X1, d.Y1) + " h=" + T(d.ChieuCao) + " " + Q(chu);
                case LoaiDoiTuong.Dim:
                    string gd = string.IsNullOrEmpty(d.GhiDe) ? "" : " ghide=" + Q(d.GhiDe);
                    return "D " + d.Layer + " " + d.LoaiDim + " do=" + T(d.GiaTriDo) + gd + " " + P(d.X1, d.Y1) + " " + P(d.X2, d.Y2);
                default:
                    return null;
            }
        }

        private static string DongPl(DoiTuong d)
        {
            if (d.Doan == null || d.Doan.Count == 0) return null;
            var sb = new StringBuilder("P ").Append(d.Layer).Append(d.Kin ? " kin " : " ho ")
                .Append(P(d.Doan[0].X1, d.Doan[0].Y1));
            foreach (DoanPl s in d.Doan)
            {
                if (s.LaCung) sb.Append(" ~").Append(P(s.Cx, s.Cy)).Append(" r=").Append(T(s.R));
                sb.Append(' ').Append(P(s.X2, s.Y2));
            }
            return sb.ToString();
        }

        private static string T(double v) => HinhHoc.Tron(v);
        private static string P(double x, double y) => T(x) + "," + T(y);
        private static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
