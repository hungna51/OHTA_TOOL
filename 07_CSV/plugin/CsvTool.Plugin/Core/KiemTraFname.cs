using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OhtaTool.Plugin.Core;

namespace CsvTool.Core
{
    /// <summary>Một chỗ F-name lệch dữ liệu Fname. DongMoi khác null: sửa được bằng 1 nút; BoSung: cần gửi bổ sung vật liệu.</summary>
    public sealed class GoiYFname
    {
        public string NoiDung, DongCu, DongMoi, M, T;
        public bool BoSung;
    }

    /// <summary>
    /// Đối chiếu F-name của tấm với dữ liệu tab Fname (catalog + bảng TraV, cùng nguồn Google Sheet):
    /// M= và T= phải có trong catalog, Z= phải đúng Z của mác, mỗi dòng V= phải đúng dao + denta tra từ TraV.
    /// </summary>
    public static class KiemTraFname
    {
        private static readonly Regex RxV = new Regex(@"^V=(?<dao>\d+(?:\.\d+)?)-(?<d>-?\d+(?:\.\d+)?)\((?<g>\d+(?:\.\d+)?)°\)$", RegexOptions.Compiled);
        private static readonly Regex RxSima = new Regex(@"^シマ目[外内]\s*V(?<dao>\d+(?:\.\d+)?)引代\s*(?<d>-?\d+(?:\.\d+)?)\s*\((?<g>\d+(?:\.\d+)?)°\)$", RegexOptions.Compiled);

        /// <summary>Dòng bẻ dạng "V=6-1.2(90°)" hoặc "シマ目外 V6引代 1.2(90°)"; dòng khác → false.</summary>
        public static bool DocV(string dong, out string dao, out double denta, out string goc)
        {
            Match m = RxV.Match((dong ?? "").Trim());
            if (!m.Success) m = RxSima.Match((dong ?? "").Trim());
            dao = m.Groups["dao"].Value; goc = m.Groups["g"].Value; denta = 0;
            return m.Success && double.TryParse(m.Groups["d"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out denta);
        }

        public static List<GoiYFname> Chay(IList<string> dong, FnameCatalogData cat, Func<string, string, double, List<TraVCandidate>> traV)
        {
            var kq = new List<GoiYFname>();
            if (cat?.Materials == null || cat.Materials.Count == 0 || dong == null) return kq;   // chưa có dữ liệu Fname trên máy
            string Lay(string dau) => dong.Select(x => (x ?? "").Trim()).FirstOrDefault(x => x.StartsWith(dau, StringComparison.Ordinal));
            string m = Lay("M=")?.Substring(2).Trim(), t = Lay("T=")?.Substring(2).Trim(), dongZ = Lay("Z=");
            if (string.IsNullOrEmpty(m)) return kq;

            FnameMaterialSpec spec = cat.FindMaterial(m);
            if (spec == null)
            {
                kq.Add(new GoiYFname { NoiDung = "Vật liệu M=" + m + " chưa có trong dữ liệu Fname.", BoSung = true, M = m, T = t });
                return kq;
            }
            bool coT = !string.IsNullOrEmpty(t) && FnameCatalogData.ContainsThickness(spec.Thicknesses, t);
            if (!coT) kq.Add(new GoiYFname { NoiDung = "Độ dày T=" + t + " chưa có cho " + m + " trong dữ liệu Fname.", BoSung = true, M = m, T = t });

            string zDung = (spec.Z ?? "").Trim();
            if (zDung != "")
            {
                if (dongZ == null) kq.Add(new GoiYFname { NoiDung = "Thiếu Z=. Theo dữ liệu Fname là Z=" + zDung + "." });
                else if (!string.Equals(dongZ.Substring(2).Trim(), zDung, StringComparison.OrdinalIgnoreCase))
                    kq.Add(new GoiYFname { NoiDung = dongZ + " khác dữ liệu Fname (Z=" + zDung + ").", DongCu = dongZ, DongMoi = "Z=" + zDung });
            }
            if (!coT) return kq;   // không có độ dày thì TraV cũng không tra được, tránh báo trùng

            foreach (string v in dong.Select(x => (x ?? "").Trim()))
            {
                if (!DocV(v, out string dao, out double denta, out string goc)) continue;
                double gocSo = double.Parse(goc, CultureInfo.InvariantCulture);
                List<TraVCandidate> cands = traV(m, t, gocSo) ?? new List<TraVCandidate>();
                bool Khop(TraVCandidate c, bool soDenta) => DocV(c.FormattedText, out string dao2, out double d2, out _) && dao2 == dao && (!soDenta || Math.Abs(d2 - denta) < 0.005);
                if (cands.Any(c => Khop(c, true))) continue;
                TraVCandidate cung = cands.FirstOrDefault(c => Khop(c, false));
                if (cung != null)
                {
                    // số nội suy / ngoại suy phải qua quản lý ở tab Fname, không sửa 1 chạm
                    bool tuDong = (cung.Source == "" || cung.Source == TraVThuRecord.BeThu) && !cung.NeedsManagerConfirm && cung.NewTrialRecord == null;
                    kq.Add(new GoiYFname
                    {
                        NoiDung = v + " lệch bảng TraV, đúng là " + cung.FormattedText + "." + (tuDong ? "" : " Số nội suy, sửa ở tab Fname để hỏi quản lý."),
                        DongCu = v, DongMoi = tuDong ? cung.FormattedText : null,
                    });
                }
                else if (cands.Count > 0)
                    kq.Add(new GoiYFname { NoiDung = v + ": dao V" + dao + " không có trong bảng TraV cho " + m + " dày " + t + " góc " + goc + "°. Bảng có: " + string.Join(", ", cands.Select(c => c.DieLabel).Distinct()) + "." });
                else
                    kq.Add(new GoiYFname { NoiDung = v + ": bảng TraV không tra được " + m + " dày " + t + " góc " + goc + "°." });
            }
            return kq;
        }
    }
}
