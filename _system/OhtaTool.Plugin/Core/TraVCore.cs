using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OhtaTool.Plugin.Core
{
    public sealed class TraVCandidate
    {
        public string FormattedText { get; set; } = string.Empty;
        public string DieLabel { get; set; } = string.Empty;
        /// <summary>Từng cụm "tên + giá trị" của dòng thông tin phụ (UI xuống dòng theo cụm, không cắt giữa cụm).</summary>
        public List<string> DetailParts { get; set; } = new List<string>();

        public string DetailInfo => string.Join(" - ", DetailParts);

        /// <summary>Cảnh báo (chữ đỏ dưới dòng bẻ), vd. góc nhọn nhỏ hơn góc tối thiểu trong bảng. Rỗng = không cảnh báo.</summary>
        public string Warning { get; set; } = string.Empty;

        /// <summary>Nguồn số liệu khi bảng TraV không tính được: "Bẻ thử" / "Nội suy". Rỗng = tính từ bảng TraV.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Dòng tham khảo sau nhãn Source, vd. "tham khảo tấm T-023" / "từ góc 70° (tấm A1) và 80° (tấm B2)".</summary>
        public string SourceInfo { get; set; } = string.Empty;

        /// <summary>Góc nội suy vừa tính (chưa có trong trav_thu) -> gửi lên Sheet chờ admin duyệt khi đặt / cập nhật Fname.</summary>
        public TraVThuRecord NewTrialRecord { get; set; }

        /// <summary>2 góc dùng nội suy cách nhau quá TraVThuData.MaxInterpolationGap -> phải xác nhận với quản lý.</summary>
        public bool NeedsManagerConfirm { get; set; }

        public override string ToString() => FormattedText;
    }

    public static class TraVCalculator
    {
        // Rỗng cho tới khi Load() đọc CSV (FnameTabViewModel gọi khi mở tab / sau khi cập nhật dữ liệu)
        private static List<TraV90Row> Table90 = new List<TraV90Row>();
        private static TraVNho90Table TableNho90 = new TraVNho90Table();
        private static TraVNho90RTable TableNho90R = new TraVNho90RTable();
        private static List<TraVThuRecord> TableThu = new List<TraVThuRecord>();
        private static List<TamRecord> TableTam = new List<TamRecord>();
        private static Dictionary<string, string> SheetConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static List<TraVCandidate> GetBendOptionsForFname(string fnameM, string fnameT, double angle)
        {
            var results = new List<TraVCandidate>();
            if (string.IsNullOrWhiteSpace(fnameT))
                return results;

            if (!double.TryParse(fnameT.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double targetT))
                return results;

            List<string> matKeys = GetTraVMaterialKeys(fnameM);
            foreach (string matKey in matKeys)
            {
                var cands = LookupCandidates(matKey, targetT, angle);
                // Bảng TraV không tính được góc này -> tra bảng góc bẻ thử (khớp góc, hoặc nội suy 2 góc gần nhất)
                if (cands.Count == 0)
                    cands = LookupTrialCandidates(matKey, targetT, angle);
                foreach (var c in cands)
                {
                    if (!results.Any(x => x.FormattedText == c.FormattedText))
                        results.Add(c);
                }
            }

            return results;
        }

        /// <summary>
        /// Dòng bảng tam đang cấp T thực cho M= / T= (để hiện "(bổ sung)" + người xác nhận); null khi không có
        /// hoặc T thực lấy từ cột cũ của trav_90.
        /// </summary>
        public static TamRecord GetRealThicknessSource(string fnameM, string fnameT)
        {
            double? t = TraVThuData.ParseNumber(fnameT);
            if (!t.HasValue)
                return null;
            foreach (string matKey in GetTraVMaterialKeys(fnameM))
            {
                TamRecord r = TamData.Find(TableTam, matKey, t.Value);
                if (r != null)
                    return r;
            }
            return null;
        }

        /// <summary>Dòng tam đang có (Sheet hoặc chờ duyệt) của Loai + T; null nếu chưa có T hiển thị.</summary>
        public static TamRecord FindTam(string loai, double t) => TamData.Find(TableTam, loai, t);


        public static string GetRealThickness(string fnameM, string fnameT)
        {
            if (string.IsNullOrWhiteSpace(fnameT))
                return string.Empty;

            if (!double.TryParse(fnameT.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double targetT))
                return string.Empty;

            List<string> matKeys = GetTraVMaterialKeys(fnameM);

            // Bảng tam (Loai + T) là nơi chính; cột T_hienthi cũ của trav_90 chỉ còn dùng khi Sheet chưa chuyển xong
            foreach (string matKey in matKeys)
            {
                TamRecord tam = TamData.Find(TableTam, matKey, targetT);
                if (tam != null)
                    return tam.THienThi;
            }

            foreach (string matKey in matKeys)
            {
                var row = Table90.FirstOrDefault(r => string.Equals(r.Mat, matKey, StringComparison.OrdinalIgnoreCase) &&
                                                      Math.Abs(r.T - targetT) < 0.05 && (!string.IsNullOrEmpty(r.C15) || r.Tthuc > 0));
                if (row != null)
                {
                    if (!string.IsNullOrEmpty(row.C15))
                        return row.C15;
                    if (row.Tthuc > 0)
                        return row.Tthuc.ToString("0.##", CultureInfo.InvariantCulture);
                }
            }

            return string.Empty;
        }

        public static List<TraVCandidate> LookupCandidates(string matKey, double thick, double angle)
        {
            var results = new List<TraVCandidate>();
            if (angle <= 0.0 || angle >= 180.0)
                return results;

            bool isSimaNgoai = matKey.IndexOf("SIMA Ngoài", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               matKey.IndexOf("SIMA Ngo\u00E0i", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isSimaTrong = matKey.IndexOf("SIMA Trong", StringComparison.OrdinalIgnoreCase) >= 0;

            if (Math.Abs(angle - 90.0) < 1e-4)
            {
                var rows = Table90.Where(r => string.Equals(r.Mat, matKey, StringComparison.OrdinalIgnoreCase) &&
                                              Math.Abs(r.T - thick) < 0.05).ToList();

                foreach (var r in rows)
                {
                    string fmt;
                    if (isSimaNgoai)
                        fmt = $"シマ目外 V{r.V}引代 {r.V2} (90°)";
                    else if (isSimaTrong)
                        fmt = $"シマ目内 V{r.V}引代 {r.V2} (90°)";
                    else
                        fmt = $"V={r.V}-{r.V2}(90°)";

                    results.Add(new TraVCandidate
                    {
                        FormattedText = fmt,
                        DieLabel = "V" + r.V,
                        DetailParts = BuildDetailParts(r, includeR: true)
                    });
                }
            }
            else if (angle > 90.0)
            {
                var rows = Table90.Where(r => string.Equals(r.Mat, matKey, StringComparison.OrdinalIgnoreCase) &&
                                              Math.Abs(r.T - thick) < 0.05 &&
                                              r.Tthuc > 0).ToList();

                string angStr = FormatAngle(angle);
                foreach (var r in rows)
                {
                    double? denta = CalcDenta(r.Ri, r.Tthuc, angle, r.Denta90);
                    if (!denta.HasValue)
                        continue;

                    string dentaStr = FormatDenta(denta.Value);
                    string fmt;
                    if (isSimaNgoai)
                        fmt = $"シマ目外 V{r.V}引代 {dentaStr}({angStr}°)";
                    else if (isSimaTrong)
                        fmt = $"シマ目内 V{r.V}引代 {dentaStr}({angStr}°)";
                    else
                        fmt = $"V={r.V}-{dentaStr}({angStr}°)";

                    results.Add(new TraVCandidate
                    {
                        FormattedText = fmt,
                        DieLabel = "V" + r.V,
                        DetailParts = BuildDetailParts(r, includeR: true)
                    });
                }
            }
            else // angle < 90.0
            {
                string matCode = MapToLt90MatCode(matKey);
                if (matCode != null)
                {
                    List<int> colIndices = TableNho90.GetColIndices(matCode, thick);
                    string angStr = FormatAngle(angle);

                    if (colIndices.Count > 0)
                    {
                        {
                            foreach (int col in colIndices)
                            {
                                // Biên nội suy lấy trong các góc CÓ số liệu của chính cột dao/độ dày này,
                                // để góc nhỏ hơn góc tối thiểu vẫn ra dòng V (kèm cảnh báo đỏ) thay vì biến mất.
                                if (!TableNho90.FindColumnAngleBounds(col, angle, out double a1, out double a2))
                                    continue;
                                double? d1 = TableNho90.GetValue(a1, col);
                                double? d2 = TableNho90.GetValue(a2, col);
                                if (!d1.HasValue || !d2.HasValue)
                                    continue;

                                string rawVLabel = TableNho90.VList[col];
                                string vLabel = FormatVLabel(rawVLabel);
                                TraV90Row r90 = FindRow90(matKey, thick, rawVLabel);

                                // Góc nằm giữa góc lớn nhất của bảng góc nhọn (vd. 88°) và 90° -> nội suy với số liệu 90° của bảng TraV
                                double? v90 = Denta90Of(r90);
                                if (angle > a1 + 1e-6 && a1 < 90.0 && v90.HasValue)
                                {
                                    a2 = a1; d2 = d1;
                                    a1 = 90.0; d1 = v90;
                                }

                                double d = Math.Abs(a1 - a2) < 1e-6 ? d1.Value : d1.Value + (angle - a1) * (d2.Value - d1.Value) / (a2 - a1);
                                string dentaStr = FormatDenta(d);

                                string fmt;
                                if (isSimaNgoai)
                                    fmt = $"シマ目外 {rawVLabel}引代 {dentaStr}({angStr}°)";
                                else if (isSimaTrong)
                                    fmt = $"シマ目内 {rawVLabel}引代 {dentaStr}({angStr}°)";
                                else
                                    fmt = $"{vLabel}-{dentaStr}({angStr}°)";

                                // Thông số dao giống trường hợp 90° nhưng bỏ các thông số R (giữ Rend)
                                var parts = new List<string>();
                                if (r90 != null)
                                    parts.AddRange(BuildDetailParts(r90, includeR: false));

                                double? rOut = TableNho90R.GetNearestROut(matCode, thick, rawVLabel, angle);
                                if (rOut.HasValue)
                                    parts.Add($"R ngoài({angStr}°) {FormatDenta(rOut.Value)}");

                                double? minAngle = TableNho90.GetMinAngle(col);
                                string warning = minAngle.HasValue && angle < minAngle.Value - 1e-6
                                    ? $"⚠ Góc {angStr}° nhỏ hơn góc nhỏ nhất trong bảng là {FormatAngle(minAngle.Value)}°"
                                    : string.Empty;

                                results.Add(new TraVCandidate
                                {
                                    FormattedText = fmt,
                                    DieLabel = rawVLabel,
                                    DetailParts = parts,
                                    Warning = warning
                                });
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Tra bảng góc bẻ thử (trav_thu) theo loại vật liệu / độ dày, từng dao (góc giữ nguyên số lẻ, không làm tròn):
        /// - có dòng đúng góc -> dùng luôn (nhãn theo Dinh_danh: Bẻ thử / Nội suy / Ngoại suy);
        /// - không có -> nội suy tuyến tính Denta từ 2 góc gần nhất kẹp 2 bên;
        ///   2 góc cách nhau &gt; MaxInterpolationGap thì cảnh báo đỏ + bắt xác nhận với quản lý khi xuất Fname;
        /// - chỉ có góc 1 phía -> ngoại suy từ 2 góc gần nhất phía đó, chỉ khi cách góc gần nhất
        ///   &lt;= MaxExtrapolationDistance; bắt xác nhận với quản lý;
        /// - miễn xác nhận khi 1 trong 2 góc gốc cách góc cần tra chưa tới NoConfirmDistance (số liệu gần như đo trực tiếp);
        /// - gốc ưu tiên chỉ lấy dòng BẺ THỬ; không ra kết quả mới cho dùng cả dòng Nội suy / Ngoại suy làm gốc,
        ///   khi đó LUÔN bắt xác nhận với quản lý.
        /// </summary>
        public static List<TraVCandidate> LookupTrialCandidates(string matKey, double thick, double angle)
        {
            var results = new List<TraVCandidate>();
            if (angle <= 0.0 || angle >= 180.0)
                return results;

            var rows = TableThu.Where(r => TraVThuData.SameLoai(r.Loai, matKey) && Math.Abs(r.TValue - thick) < 0.05 && r.DentaValue.HasValue);
            foreach (var dieGroup in rows.GroupBy(r => r.Dao, StringComparer.OrdinalIgnoreCase))
            {
                // Góc 90° của bảng TraV (cùng dao / độ dày) cũng là số liệu chuẩn -> dùng làm gốc như 1 góc bẻ thử
                var die = dieGroup.ToList();
                TraVThuRecord base90 = Table90Base(matKey, thick, dieGroup.Key, die[0]);
                if (base90 != null && !die.Any(r => Math.Abs(r.Goc - 90.0) < 1e-4))
                    die.Add(base90);

                var exact = die.Where(r => Math.Abs(r.Goc - angle) < 1e-4).ToList();
                if (exact.Count > 0)
                {
                    foreach (TraVThuRecord r in exact)
                    {
                        string info = r.IsBeThu
                            ? "tham khảo tấm " + RefTam(r.MaTam)
                            : "từ " + RefTam(r.MaTam);
                        results.Add(BuildTrialCandidate(matKey, thick, angle, r.Dao, r.DentaValue.Value, r.SourceLabel, info, r));
                    }
                    continue;
                }

                if (!TryDerive(die.Where(r => r.IsBeThu).ToList(), angle, out TraVThuRecord a, out TraVThuRecord b, out double d, out bool extrap)
                    && !TryDerive(die, angle, out a, out b, out d, out extrap))
                    continue;

                string kind = extrap ? TraVThuRecord.NgoaiSuy : TraVThuRecord.NoiSuy;
                string sourceInfo = $"từ góc {DescribeBase(a)} và {DescribeBase(b)}";
                // Thông số dao lấy theo góc gốc có nhập (ưu tiên góc gần hơn: b)
                TraVThuRecord dieSource = new[] { b, a }.FirstOrDefault(r => r.HasDieParams);
                TraVCandidate c = BuildTrialCandidate(matKey, thick, angle, dieGroup.Key, d, kind, sourceInfo, dieSource);
                c.NewTrialRecord = new TraVThuRecord
                {
                    Loai = a.Loai,
                    T = a.T,
                    Goc = angle,
                    Dao = dieGroup.Key,
                    Denta = FormatDenta(d),
                    MaTam = BaseToken(a) + " / " + BaseToken(b),
                    DinhDanh = kind
                };
                c.NewTrialRecord.CopyDieParams(dieSource);

                var derived = new[] { a, b }.Where(r => !r.IsBeThu).ToList();
                if (derived.Count > 0)
                {
                    // Gốc là số liệu tính ra, không phải đo thật -> luôn hỏi quản lý
                    c.NeedsManagerConfirm = true;
                    c.Warning = "⚠ Gốc tính có góc " + string.Join(", ", derived.Select(r => FormatAngle(r.Goc) + "° (" + r.SourceLabel.ToLowerInvariant() + ")"))
                        + " không phải bẻ thử — cần xác nhận với quản lý";
                }
                else
                {
                    string warning = extrap
                        ? $"⚠ Ngoại suy ngoài khoảng đo {FormatAngle(Math.Min(Math.Abs(angle - a.Goc), Math.Abs(angle - b.Goc)))}° — cần xác nhận với quản lý"
                        : Math.Abs(b.Goc - a.Goc) > TraVThuData.MaxInterpolationGap + 1e-6
                            ? $"⚠ 2 góc nội suy cách nhau quá {FormatAngle(TraVThuData.MaxInterpolationGap)}° — cần xác nhận với quản lý"
                            : string.Empty;
                    // 1 trong 2 góc gốc cách góc cần tra < NoConfirmDistance -> không cần hỏi quản lý
                    bool closeEnough = Math.Min(Math.Abs(a.Goc - angle), Math.Abs(b.Goc - angle)) < TraVThuData.NoConfirmDistance - 1e-9;
                    if (warning.Length > 0 && !closeEnough)
                    {
                        c.NeedsManagerConfirm = true;
                        c.Warning = warning;
                    }
                }
                results.Add(c);
            }
            return results;
        }

        /// <summary>
        /// 2 góc gốc + Denta tại góc cần tra: kẹp 2 bên -> nội suy (a &lt; góc &lt; b); chỉ 1 phía -> ngoại suy từ 2 góc gần nhất
        /// phía đó (a = xa, b = gần), góc gần phải cách &lt;= MaxExtrapolationDistance.
        /// </summary>
        private static bool TryDerive(List<TraVThuRecord> bases, double angle, out TraVThuRecord a, out TraVThuRecord b, out double d, out bool extrap)
        {
            a = b = null;
            d = 0.0;
            extrap = false;
            TraVThuRecord lo = bases.Where(r => r.Goc < angle).OrderByDescending(r => r.Goc).FirstOrDefault();
            TraVThuRecord hi = bases.Where(r => r.Goc > angle).OrderBy(r => r.Goc).FirstOrDefault();
            if (lo != null && hi != null)
            {
                a = lo; b = hi;
            }
            else
            {
                var side = (lo != null ? bases.Where(r => r.Goc < angle).OrderByDescending(r => r.Goc)
                                       : bases.Where(r => r.Goc > angle).OrderBy(r => r.Goc)).ToList();
                if (side.Count < 2 || Math.Abs(side[0].Goc - angle) > TraVThuData.MaxExtrapolationDistance + 1e-6)
                    return false;
                b = side[0];
                TraVThuRecord near = b;
                a = side.FirstOrDefault(r => Math.Abs(r.Goc - near.Goc) > 1e-6);
                if (a == null)
                    return false;
                extrap = true;
            }
            d = a.DentaValue.Value + (angle - a.Goc) * (b.DentaValue.Value - a.DentaValue.Value) / (b.Goc - a.Goc);
            return true;
        }

        /// <summary>Ma_tam của gốc 90° lấy từ bảng TraV (không phải tấm bẻ thử).</summary>
        private const string Table90Ref = "bảng TraV";

        /// <summary>Denta 90° của bảng TraV: giá trị hiện trên dòng V= 90° (HikiShiro_90), trống thì lấy Denta90.</summary>
        private static double? Denta90Of(TraV90Row r)
        {
            if (r == null) return null;
            if (double.TryParse(r.V2, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v > 0) return v;
            return r.Denta90 > 0 ? r.Denta90 : (double?)null;
        }

        /// <summary>Dòng gốc 90° ảo cho bảng bẻ thử, lấy từ bảng TraV cùng vật liệu / độ dày / dao.</summary>
        private static TraVThuRecord Table90Base(string matKey, double thick, string dao, TraVThuRecord sample)
        {
            double? v90 = Denta90Of(FindRow90(matKey, thick, dao));
            if (!v90.HasValue) return null;
            return new TraVThuRecord
            {
                Loai = sample.Loai,
                T = sample.T,
                Goc = 90.0,
                Dao = dao,
                Denta = v90.Value.ToString("0.##", CultureInfo.InvariantCulture),
                MaTam = Table90Ref,
                DinhDanh = TraVThuRecord.BeThu
            };
        }

        /// <summary>"70° (tấm A1)", "90° (bảng TraV)" hoặc "72° (nội suy: A1 (70°) / B2 (80°))".</summary>
        private static string DescribeBase(TraVThuRecord r) =>
            r.MaTam == Table90Ref
                ? $"{FormatAngle(r.Goc)}° ({Table90Ref})"
            : r.IsBeThu
                ? $"{FormatAngle(r.Goc)}° (tấm {RefTam(r.MaTam)})"
                : $"{FormatAngle(r.Goc)}° ({r.SourceLabel.ToLowerInvariant()}: {RefTam(r.MaTam)})";

        /// <summary>Ma_tam ghi lên Sheet cho góc tính ra: "A1 (70°)" hoặc "nội suy 72°".</summary>
        private static string BaseToken(TraVThuRecord r) =>
            r.IsBeThu ? $"{RefTam(r.MaTam)} ({FormatAngle(r.Goc)}°)" : $"{r.SourceLabel.ToLowerInvariant()} {FormatAngle(r.Goc)}°";

        private static string RefTam(string maTam) => string.IsNullOrWhiteSpace(maTam) ? "?" : maTam.Trim();

        private static TraVCandidate BuildTrialCandidate(string matKey, double thick, double angle, string dao, double denta, string source, string sourceInfo,
            TraVThuRecord dieSource = null)
        {
            string loai = TraVThuData.NormalizeLoai(matKey);
            bool isSimaNgoai = loai.EndsWith("SIMA NGOAI", StringComparison.Ordinal);
            bool isSimaTrong = loai.EndsWith("SIMA TRONG", StringComparison.Ordinal);
            string angStr = FormatAngle(angle);
            string dentaStr = FormatDenta(denta);

            string fmt;
            if (isSimaNgoai)
                fmt = $"シマ目外 {dao}引代 {dentaStr}({angStr}°)";
            else if (isSimaTrong)
                fmt = $"シマ目内 {dao}引代 {dentaStr}({angStr}°)";
            else
                fmt = $"{FormatVLabel(dao)}-{dentaStr}({angStr}°)";

            // Thông số dao (không có R): nhập kèm lúc bẻ thử; không có thì lấy dòng trav_90 đúng loại + T + dao nếu có
            var parts = new List<string>();
            if (dieSource != null && dieSource.HasDieParams)
                parts.AddRange(dieSource.DieParts());
            else
            {
                TraV90Row r90 = FindRow90(matKey, thick, dao);
                if (r90 != null)
                    parts.AddRange(BuildDetailParts(r90, includeR: false));
            }

            return new TraVCandidate
            {
                FormattedText = fmt,
                DieLabel = dao,
                DetailParts = parts,
                Source = source,
                SourceInfo = sourceInfo
            };
        }

        /// <summary>Loại vật liệu cho bảng góc bẻ thử: mọi Mat của trav_90 (kể cả SIMA Trong/Ngoài) + loại đã có trong trav_thu.</summary>
        public static List<string> GetTrialMaterialTypeOptions()
        {
            var result = new List<string>();
            foreach (string mat in Table90.Select(r => r.Mat?.Trim()).Concat(TableThu.Select(r => r.Loai)))
            {
                if (!string.IsNullOrEmpty(mat) && !result.Any(x => TraVThuData.SameLoai(x, mat)))
                    result.Add(mat);
            }
            return result;
        }

        /// <summary>Danh sách dao (V8, V12, U18...) từ bảng 90 + bảng góc nhọn, xếp theo số.</summary>
        public static List<string> GetDieOptions()
        {
            var all = Table90.Select(r => TraVThuData.NormalizeDao(r.V)).Concat(TableNho90.VList.Select(TraVThuData.NormalizeDao))
                .Where(v => v.Length > 1).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return all.OrderBy(v => TraVThuData.ParseNumber(v.Substring(1)) ?? double.MaxValue).ThenBy(v => v, StringComparer.Ordinal).ToList();
        }

        /// <summary>Bảng góc bẻ thử đang nạp (Sheet + bổ sung chờ duyệt của máy này).</summary>
        public static List<TraVThuRecord> GetTrialRecords() => TableThu.ToList();

        // Danh mục vật liệu (cột Loai) — FnameTabViewModel gán sau khi đọc catalog.csv
        private static FnameCatalogData Catalog;

        public static void SetCatalog(FnameCatalogData catalog)
        {
            Catalog = catalog;
        }

        /// <summary>Các loại vật liệu có trong bảng trav_90 (bỏ biến thể "... SIMA Trong/Ngoài") — gợi ý cho cột Loai.</summary>
        public static List<string> GetMaterialTypeOptions()
        {
            var result = new List<string>();
            foreach (TraV90Row r in Table90)
            {
                string mat = r.Mat?.Trim();
                if (string.IsNullOrEmpty(mat) || mat.IndexOf("SIMA", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (!result.Contains(mat, StringComparer.OrdinalIgnoreCase))
                    result.Add(mat);
            }
            return result;
        }

        /// <summary>
        /// Loại vật liệu của mác M: lấy cột Loai trong catalog; mác không có trong catalog / Loai rỗng -> rỗng.
        /// </summary>
        public static string GetMaterialType(string fnameM)
        {
            return Catalog?.FindMaterial(fnameM)?.Loai?.Trim() ?? string.Empty;
        }

        public static List<string> GetTraVMaterialKeys(string fnameM)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(fnameM))
            {
                list.Add("SS");
                return list;
            }

            string m = fnameM.Trim();
            bool isSima = m.IndexOf("縞板", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("SIMA", StringComparison.OrdinalIgnoreCase) >= 0;

            string loai = GetMaterialType(m);
            if (loai.Length > 0)
            {
                if (isSima)
                {
                    list.Add(loai + " SIMA Trong");
                    list.Add(loai + " SIMA Ngoài");
                }
                else
                {
                    list.Add(loai);
                }
                return list;
            }

            if (isSima)
            {
                if (m.IndexOf("SUS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.Add("SUS SIMA Trong");
                    list.Add("SUS SIMA Ngoài");
                }
                else if (m.IndexOf("AL", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.Add("AL SIMA Trong");
                    list.Add("AL SIMA Ngoài");
                }
                else
                {
                    list.Add("SS SIMA Trong");
                    list.Add("SS SIMA Ngoài");
                }
            }
            else if (m.IndexOf("SUS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                list.Add("SUS");
            }
            else if (m.IndexOf("BON", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                list.Add("BON");
            }
            else if (m.IndexOf("AL", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                list.Add("AL");
            }
            else
            {
                list.Add("SS");
            }

            return list;
        }

        public static double? CalcDenta(double ri, double tthuc, double angle, double denta90)
        {
            if (angle <= 0.0 || angle >= 180.0) return null;
            double angleRad = (90.0 - angle / 2.0) * Math.PI / 180.0;
            double term1 = (ri + tthuc) * Math.Tan(angleRad);
            double term2 = ((ri + tthuc) - denta90) * ((180.0 - angle) / 90.0);
            double denta = term1 - term2;
            return Math.Floor(denta * 100.0) / 100.0;
        }

        public static string FormatAngle(double angle)
        {
            int i = (int)Math.Round(angle);
            if (Math.Abs(angle - i) < 1e-4)
                return i.ToString(CultureInfo.InvariantCulture);
            return angle.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string FormatDenta(double d)
        {
            double truncated = Math.Floor(d * 100.0) / 100.0;
            return truncated.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string FormatPressOneTimeLine(string innerR, string angle)
        {
            string r = (innerR ?? string.Empty).Trim();
            string a = (angle ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(a)) a = "110";
            if (!a.EndsWith("°", StringComparison.Ordinal) && !a.EndsWith("%%D", StringComparison.OrdinalIgnoreCase))
                a += "°";

            if (!string.IsNullOrEmpty(r))
            {
                if (r.StartsWith("内R", StringComparison.OrdinalIgnoreCase))
                    r = r.Substring(2).Trim();
                else if (r.StartsWith("R", StringComparison.OrdinalIgnoreCase))
                    r = r.Substring(1).Trim();
                return $"内R{r}-発押し{a}";
            }
            return $"発押し{a}";
        }

        public static bool TryParsePressOneTimeLine(string line, out string innerR, out string angle)
        {
            innerR = string.Empty;
            angle = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;
            string s = line.Trim();
            if (s.IndexOf("発押し", StringComparison.Ordinal) < 0) return false;

            int posPoshi = s.IndexOf("発押し", StringComparison.Ordinal);
            string left = s.Substring(0, posPoshi).TrimEnd('-', ' ');
            string right = s.Substring(posPoshi + "発押し".Length).Trim().TrimEnd('°', '%', 'D', 'd');

            angle = right;
            if (!string.IsNullOrEmpty(left))
            {
                if (left.StartsWith("内R", StringComparison.OrdinalIgnoreCase))
                    innerR = left.Substring(2).Trim();
                else if (left.StartsWith("R", StringComparison.OrdinalIgnoreCase))
                    innerR = left.Substring(1).Trim();
                else
                    innerR = left;
            }
            return true;
        }

        private static string FormatVLabel(string v)
        {
            if (string.IsNullOrEmpty(v)) return "V=";
            string c = v.Substring(0, 1).ToUpperInvariant();
            string rest = v.Substring(1);
            if (c == "V" || c == "U")
                return c + "=" + rest;
            return "V=" + v;
        }

        /// <summary>
        /// Các cụm thông số dao (không lặp lại tên dao V vì đã hiện ở ô chọn V).
        /// includeR = false (góc nhọn): bỏ 実内R / 内R / 外R, vẫn giữ Rend.
        /// </summary>
        private static List<string> BuildDetailParts(TraV90Row r, bool includeR)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(r.C8)) parts.Add("最長 " + r.C8);
            if (!string.IsNullOrEmpty(r.C9)) parts.Add("Ｌ最低 " + r.C9);
            if (!string.IsNullOrEmpty(r.C10)) parts.Add("Ｚ最低 " + r.C10);
            if (includeR)
            {
                if (!string.IsNullOrEmpty(r.C11)) parts.Add("実内R " + r.C11);
                if (!string.IsNullOrEmpty(r.C12)) parts.Add("内R " + r.C12);
                if (!string.IsNullOrEmpty(r.C13)) parts.Add("外R " + r.C13);
            }
            if (!string.IsNullOrEmpty(r.C14)) parts.Add("Rend " + r.C14);
            return parts;
        }

        /// <summary>
        /// Dòng bảng 90° ứng với dao của bảng góc nhọn ("V12" / "U18" -> cột V = "12" / "18").
        /// </summary>
        /// <summary>
        /// Gợi ý 最長 / Ｌ最低 / Ｚ最低 / Rend cho form góc bẻ thử: góc bẻ thử khác cùng loại + T + dao đã có số,
        /// rồi trav_90 cùng dao (ưu tiên cùng loại, T gần nhất), rồi loại khác. Dao không có ở đâu -> null.
        /// </summary>
        public static TraVThuRecord SuggestDieParams(string loai, double thick, string dao, out string source)
        {
            source = string.Empty;
            string d = TraVThuData.NormalizeDao(dao);
            TraVThuRecord thu = TableThu.FirstOrDefault(r => r.HasDieParams && TraVThuData.SameLoai(r.Loai, loai)
                && Math.Abs(r.TValue - thick) < 0.05 && string.Equals(r.Dao, d, StringComparison.OrdinalIgnoreCase));
            if (thu != null)
            {
                source = $"góc bẻ thử {thu.Loai} T={thu.T} {thu.Dao} (tấm {thu.MaTam})";
                var copy = new TraVThuRecord();
                copy.CopyDieParams(thu);
                return copy;
            }

            string v = d.Length > 1 && char.IsLetter(d[0]) ? d.Substring(1) : d;
            TraV90Row row = Table90.Where(r => string.Equals(r.V, v, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => TraVThuData.SameLoai(r.Mat, loai) ? 0 : 1)
                .ThenBy(r => Math.Abs(r.T - thick))
                .FirstOrDefault();
            if (row == null)
                return null;
            source = $"bảng TraV {row.Mat} T={FormatDenta(row.T)} V{row.V}";
            return new TraVThuRecord { Saichou = row.C8, LSaitei = row.C9, ZSaitei = row.C10, Rend = row.C14 };
        }

        private static TraV90Row FindRow90(string matKey, double thick, string dieLabel)
        {
            string v = (dieLabel ?? string.Empty).Trim();
            if (v.Length > 0 && char.IsLetter(v[0]))
                v = v.Substring(1);
            return Table90.FirstOrDefault(r => string.Equals(r.Mat, matKey, StringComparison.OrdinalIgnoreCase) &&
                                               Math.Abs(r.T - thick) < 0.05 &&
                                               string.Equals(r.V, v, StringComparison.OrdinalIgnoreCase));
        }

        private static string MapToLt90MatCode(string mat)
        {
            if (string.IsNullOrEmpty(mat)) return "SS";
            string s = mat.ToUpperInvariant();
            if (s.IndexOf("SIMA", StringComparison.Ordinal) >= 0) return null;
            if (s.IndexOf("SUS", StringComparison.Ordinal) >= 0) return "SUS";
            if (s.IndexOf("AL", StringComparison.Ordinal) >= 0) return "AL";
            if (s.IndexOf("SS", StringComparison.Ordinal) >= 0 || s.IndexOf("BON", StringComparison.Ordinal) >= 0) return "SS";
            return null;
        }

        internal sealed class TraV90Row
        {
            public string Mat { get; set; } = string.Empty;
            public double T { get; set; }
            public double Tthuc { get; set; }
            public string V { get; set; } = string.Empty;
            public string V2 { get; set; } = string.Empty;
            public double Denta90 { get; set; }
            public double Ri { get; set; }
            public string C8 { get; set; } = string.Empty;
            public string C9 { get; set; } = string.Empty;
            public string C10 { get; set; } = string.Empty;
            public string C11 { get; set; } = string.Empty;
            public string C12 { get; set; } = string.Empty;
            public string C13 { get; set; } = string.Empty;
            public string C14 { get; set; } = string.Empty;
            public string C15 { get; set; } = string.Empty;
        }

        private sealed class TraVNho90Table
        {
            public List<string> Materials { get; set; } = new List<string>();
            public List<string> VList { get; set; } = new List<string>();
            public List<double> Thicknesses { get; set; } = new List<double>();
            public Dictionary<double, List<double?>> Rows { get; set; } = new Dictionary<double, List<double?>>();

            public List<int> GetColIndices(string mat, double thick)
            {
                var list = new List<int>();
                for (int i = 0; i < Materials.Count && i < Thicknesses.Count; i++)
                {
                    if (string.Equals(Materials[i], mat, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(Thicknesses[i] - thick) < 0.05)
                    {
                        list.Add(i);
                    }
                }
                return list;
            }

            public bool FindAngleBounds(double angle, out double a1, out double a2)
            {
                var angles = Rows.Keys.OrderByDescending(x => x).ToList();
                if (angles.Count == 0)
                {
                    a1 = 88.0; a2 = 86.0;
                    return false;
                }

                if (angle >= angles.First())
                {
                    a1 = angles.First();
                    a2 = angles.Count > 1 ? angles[1] : angles.First();
                    return true;
                }

                if (angle <= angles.Last())
                {
                    a1 = angles.Count > 1 ? angles[angles.Count - 2] : angles.Last();
                    a2 = angles.Last();
                    return true;
                }

                for (int i = 0; i < angles.Count - 1; i++)
                {
                    if (angle <= angles[i] && angle >= angles[i + 1])
                    {
                        a1 = angles[i];
                        a2 = angles[i + 1];
                        return true;
                    }
                }

                a1 = angles[0];
                a2 = angles[1];
                return true;
            }

            public double? GetValue(double angleKey, int colIndex)
            {
                if (Rows.TryGetValue(angleKey, out var row) && colIndex >= 0 && colIndex < row.Count)
                    return row[colIndex];
                return null;
            }

            /// <summary>
            /// 2 góc kẹp góc cần tính, chỉ xét các góc có số liệu trong cột; ngoài khoảng thì lấy 2 góc ở đầu mút (ngoại suy).
            /// </summary>
            public bool FindColumnAngleBounds(int colIndex, double angle, out double a1, out double a2)
            {
                var angles = Rows.Where(kvp => colIndex < kvp.Value.Count && kvp.Value[colIndex].HasValue)
                                 .Select(kvp => kvp.Key).OrderByDescending(x => x).ToList();
                a1 = a2 = 0.0;
                if (angles.Count == 0)
                    return false;
                if (angles.Count == 1)
                {
                    a1 = a2 = angles[0];
                    return true;
                }
                if (angle >= angles[0])
                {
                    a1 = angles[0]; a2 = angles[1];
                    return true;
                }
                for (int i = 0; i < angles.Count - 1; i++)
                {
                    if (angle <= angles[i] && angle >= angles[i + 1])
                    {
                        a1 = angles[i]; a2 = angles[i + 1];
                        return true;
                    }
                }
                a1 = angles[angles.Count - 2]; a2 = angles[angles.Count - 1];
                return true;
            }

            /// <summary>Góc nhỏ nhất còn có số liệu trong cột (= góc nhọn nhỏ nhất bẻ được với dao/độ dày đó).</summary>
            public double? GetMinAngle(int colIndex)
            {
                double? min = null;
                foreach (var kvp in Rows)
                {
                    if (colIndex < kvp.Value.Count && kvp.Value[colIndex].HasValue && (!min.HasValue || kvp.Key < min.Value))
                        min = kvp.Key;
                }
                return min;
            }
        }

        private sealed class TraVNho90RTable
        {
            public List<string> Materials { get; set; } = new List<string>();
            public List<string> VList { get; set; } = new List<string>();
            public List<double> Thicknesses { get; set; } = new List<double>();
            public Dictionary<double, List<double?>> Rows { get; set; } = new Dictionary<double, List<double?>>();

            public double? GetNearestROut(string mat, double thick, string vLabel, double angle)
            {
                int col = -1;
                for (int i = 0; i < Materials.Count && i < Thicknesses.Count && i < VList.Count; i++)
                {
                    if (string.Equals(Materials[i], mat, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(Thicknesses[i] - thick) < 0.05 &&
                        string.Equals(VList[i], vLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        col = i;
                        break;
                    }
                }

                if (col < 0) return null;

                double bestDist = double.MaxValue;
                double? bestVal = null;
                foreach (var kvp in Rows)
                {
                    if (col < kvp.Value.Count && kvp.Value[col].HasValue)
                    {
                        double dist = Math.Abs(kvp.Key - angle);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestVal = kvp.Value[col];
                        }
                    }
                }

                return bestVal;
            }
        }

        /// <summary>
        /// Nạp lại 3 bảng TraV từ CSV (cache Google Sheet hoặc bản mẫu 01_Fname/data/).
        /// </summary>
        public static void Load(string moduleDirectory, string cacheDirectory = null)
        {
            Table90 = ParseTable90(FnameDataStore.ReadLines(moduleDirectory, "trav_90", cacheDirectory));

            var nho = new TraVNho90Table();
            ParseGrid(FnameDataStore.ReadLines(moduleDirectory, "trav_nho90", cacheDirectory), nho.Materials, nho.VList, nho.Thicknesses, nho.Rows);
            TableNho90 = nho;

            var nhoR = new TraVNho90RTable();
            ParseGrid(FnameDataStore.ReadLines(moduleDirectory, "trav_nho90r", cacheDirectory), nhoR.Materials, nhoR.VList, nhoR.Thicknesses, nhoR.Rows);
            TableNho90R = nhoR;

            // Góc bẻ thử: bảng Sheet + dòng máy này đã gửi nhưng admin chưa duyệt (trav_thu_local.csv)
            var thu = TraVThuData.Parse(FnameDataStore.ReadLines(moduleDirectory, "trav_thu", cacheDirectory));
            foreach (TraVThuRecord r in TraVThuLocalAdditions.Load(cacheDirectory))
                TraVThuData.Upsert(thu, r);
            TableThu = thu;

            // Thông số tấm: Sheet + dòng máy này đã gửi bổ sung nhưng admin chưa duyệt (tam_local.csv, đánh dấu ChoDuyet)
            var tam = TamData.Parse(FnameDataStore.ReadLines(moduleDirectory, "tam", cacheDirectory));
            foreach (TamRecord r in TamLocalAdditions.Load(cacheDirectory))
                TamData.Upsert(tam, r);
            TableTam = tam;
            SheetConfig = FnameSheetConfig.Parse(FnameDataStore.ReadLines(moduleDirectory, "cau_hinh", cacheDirectory));
        }

        /// <summary>Cấu hình tab cau_hinh trên Sheet (vd phien_ban_toi_thieu).</summary>
        public static IDictionary<string, string> GetSheetConfig() => SheetConfig;

        /// <summary>Cảnh báo liên kết giữa catalog / trav_90 / tam (hiện trong Cài đặt).</summary>
        public static List<string> GetDataWarnings(FnameCatalogData catalog) =>
            FnameDataCheck.Check(catalog, Table90.Select(r => r.Mat), TableTam);

        // Tên cột trav_90 trên Sheet (đã bỏ dấu, viết hoa, bỏ "_") -> vị trí mặc định khi file không có dòng tiêu đề
        private static readonly string[] Table90Columns =
        {
            "MAT", "T", "TTHUC", "V", "HIKISHIRO90", "DENTA90", "RI", "SAICHOU", "LSAITEI", "ZSAITEI",
            "JITSUNAIR", "NAIR", "GAIR", "REND", "THIENTHI"
        };

        /// <summary>
        /// Đọc trav_90 theo tên cột (dòng tiêu đề), nên thêm / bớt / đổi thứ tự cột trên Sheet không làm đọc lệch.
        /// Không có dòng tiêu đề thì hiểu theo thứ tự Table90Columns. Thiếu cột T_hienthi (đã chuyển sang tab tam) vẫn đọc được.
        /// </summary>
        internal static List<TraV90Row> ParseTable90(string[] rawLines)
        {
            var list = new List<TraV90Row>();
            var idx = new Dictionary<string, int>();
            for (int i = 0; i < Table90Columns.Length; i++)
                idx[Table90Columns[i]] = i;

            foreach (string line in rawLines)
            {
                string[] p = line.Split(',');
                if (p.Length < 2) continue;

                // dòng tiêu đề (có cột Mat và T): lấy vị trí theo tên cột
                var head = p.Select(h => TraVThuData.Fold(h).Replace("_", string.Empty).Replace(" ", string.Empty)).ToList();
                if (head.Contains("MAT") && head.Contains("T"))
                {
                    idx.Clear();
                    for (int i = 0; i < head.Count; i++)
                        if (!idx.ContainsKey(head[i]))
                            idx[head[i]] = i;
                    continue;
                }

                string Cell(string name) => idx.TryGetValue(name, out int i) && i < p.Length ? p[i].Trim() : string.Empty;
                double Num(string name) => double.TryParse(Cell(name), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0.0;
                if (!double.TryParse(Cell("T"), NumberStyles.Float, CultureInfo.InvariantCulture, out double t))
                    continue; // dòng tiêu đề lạ / dòng rỗng
                if (Cell("MAT").Length == 0 || Cell("V").Length == 0)
                    continue;

                list.Add(new TraV90Row
                {
                    Mat = Cell("MAT"),
                    T = t,
                    Tthuc = Num("TTHUC"),
                    V = Cell("V"),
                    V2 = Cell("HIKISHIRO90"),
                    Denta90 = Num("DENTA90"),
                    Ri = Num("RI"),
                    C8 = Cell("SAICHOU"),
                    C9 = Cell("LSAITEI"),
                    C10 = Cell("ZSAITEI"),
                    C11 = Cell("JITSUNAIR"),
                    C12 = Cell("NAIR"),
                    C13 = Cell("GAIR"),
                    C14 = Cell("REND"),
                    C15 = Cell("THIENTHI")
                });
            }

            return list;
        }

        internal static int ParseGridRowCount(string[] raw)
        {
            var rows = new Dictionary<double, List<double?>>();
            ParseGrid(raw, new List<string>(), new List<string>(), new List<double>(), rows);
            return rows.Count;
        }

        /// <summary>
        /// Bảng lưới góc nhọn: 3 dòng đầu = vật liệu / dao V / độ dày theo cột, các dòng sau = góc, giá trị...
        /// </summary>
        private static void ParseGrid(string[] raw, List<string> materials, List<string> vList, List<double> thicknesses, Dictionary<double, List<double?>> rows)
        {
            if (raw.Length < 3)
                return;

            materials.AddRange(raw[0].Split(',').Skip(1).Select(s => s.Trim()));
            vList.AddRange(raw[1].Split(',').Skip(1).Select(s => s.Trim()));
            thicknesses.AddRange(raw[2].Split(',').Skip(1).Select(s => double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0.0));

            for (int r = 3; r < raw.Length; r++)
            {
                string[] parts = raw[r].Split(',');
                if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double ang))
                    continue;

                var vals = new List<double?>();
                for (int c = 1; c < parts.Length; c++)
                {
                    string cell = parts[c].Trim();
                    if (!string.IsNullOrEmpty(cell) && double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                        vals.Add(v);
                    else
                        vals.Add(null);
                }
                rows[ang] = vals;
            }
        }
    }
}
