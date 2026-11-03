using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Lỗi DỮ LIỆU của một tấm — để bảng DST tô nổi bật. Cùng luật với lệnh GRO:
    ///   "Error T!!!"  T không thuộc bảng độ dày chuẩn (gro: mã khắc đổi màu vàng)
    ///   "Error Q!!!"  Q rỗng hoặc không phải số nguyên (gro: mã khắc đổi màu xanh)
    /// cộng thêm: thiếu M, M không khớp T theo luật F-name, Q/T lệch với CSV.
    /// Không phụ thuộc ZWCAD.
    /// </summary>
    public static class KiemTraTam
    {
        /// <summary>Bảng độ dày chuẩn — y hệt biến `dst` của c:gro / c:FN.</summary>
        public static readonly double[] DoDayChuan =
            { 0.6, 0.8, 1.0, 1.2, 1.6, 2.3, 3.2, 4.5, 6.0, 9.0, 12.0, 16.0, 19.0 };

        public const string SS400 = "SS400";
        public const string SPHC = "SPHC";

        /// <summary>So có sai số như fn--member-num (1e-6), không so chuỗi.</summary>
        public static bool LaDoDayChuan(double t) => DoDayChuan.Any(x => Math.Abs(x - t) < 1e-6);

        /// <summary>Vật liệu F-name theo luật của GRO: T &gt;= 4.5 -> SS400, còn lại SPHC.</summary>
        public static string MTheoT(double t) => t + 1e-6 >= 4.5 ? SS400 : SPHC;

        public static bool TryDocSo(string s, out double v)
        {
            return double.TryParse((s ?? string.Empty).Trim().Replace(',', '.'),
                                   NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        /// <summary>
        /// Danh sách lỗi (nhãn ngắn để hiện trong cột Trạng thái). Rỗng = không lỗi.
        /// csvQ / csvT = giá trị trong CSV để đối chiếu; null = không đối chiếu (không có dòng
        /// CSV, hoặc người dùng đang sửa ô đó — lưu xong CSV sẽ được ghi theo).
        /// </summary>
        public static List<string> Loi(string q, string t, string m, string csvQ, string csvT)
        {
            var loi = new List<string>();
            string qq = (q ?? string.Empty).Trim();
            string tt = (t ?? string.Empty).Trim();
            string mm = (m ?? string.Empty).Trim();

            // Q - giong "Error Q!!!" cua gro
            if (qq.Length == 0) loi.Add("Thiếu Q");
            else if (!GroText.LaBangoHopLe(qq)) loi.Add("Q sai \"" + qq + "\"");

            // T - giong "Error T!!!" cua gro
            bool coT = TryDocSo(tt, out double vT);
            if (tt.Length == 0) loi.Add("Thiếu T");
            else if (!coT) loi.Add("T sai \"" + tt + "\"");
            else if (!LaDoDayChuan(vT)) loi.Add("T=" + tt + " không chuẩn");

            // M - chi xet SS400 / SPHC; vat lieu khac (CPL...) do nguoi dung / lenh dat rieng
            if (mm.Length == 0) loi.Add("Thiếu M");
            else if (coT && (string.Equals(mm, SS400, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(mm, SPHC, StringComparison.OrdinalIgnoreCase)))
            {
                string dung = MTheoT(vT);
                if (!string.Equals(mm, dung, StringComparison.OrdinalIgnoreCase))
                    loi.Add("M không khớp T (T=" + tt + " phải " + dung + ")");
            }

            // Doi chieu CSV
            if (csvQ != null && csvQ.Trim() != qq)
                loi.Add("Q lệch CSV (" + csvQ.Trim() + ")");
            if (csvT != null && coT && TryDocSo(csvT, out double vCsv) && Math.Abs(vCsv - vT) > 1e-6)
                loi.Add("T lệch CSV (" + csvT.Trim() + ")");
            return loi;
        }
    }
}
