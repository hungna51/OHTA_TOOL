using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// 1 dòng bảng góc bẻ thử (tab "trav_thu" trên Google Sheet): số liệu đo thực tế khi bảng TraV không tính được.
    /// Cột: Loai,T,Goc,Dao,Denta,Ma_tam,Dinh_danh — Dinh_danh = "Bẻ thử" (đo thật), "Nội suy" / "Ngoại suy" (tool tính từ 2 góc bẻ thử).    /// </summary>
    public sealed class TraVThuRecord
    {
        public const string BeThu = "Bẻ thử";
        public const string NoiSuy = "Nội suy";
        public const string NgoaiSuy = "Ngoại suy";

        public string Loai { get; set; } = string.Empty;
        public string T { get; set; } = string.Empty;
        public double Goc { get; set; }
        public string Dao { get; set; } = string.Empty;
        public string Denta { get; set; } = string.Empty;
        public string MaTam { get; set; } = string.Empty;
        public string DinhDanh { get; set; } = BeThu;

        public bool IsNoiSuy => TraVThuData.Fold(DinhDanh).StartsWith("NOI", StringComparison.Ordinal);

        public bool IsNgoaiSuy => TraVThuData.Fold(DinhDanh).StartsWith("NGOAI", StringComparison.Ordinal);

        /// <summary>Chỉ dòng đo thật mới được làm gốc nội suy / ngoại suy.</summary>
        public bool IsBeThu => !IsNoiSuy && !IsNgoaiSuy;

        /// <summary>Định danh chuẩn hoá: "Bẻ thử" / "Nội suy" / "Ngoại suy".</summary>
        public string SourceLabel => IsNoiSuy ? NoiSuy : IsNgoaiSuy ? NgoaiSuy : BeThu;

        public double TValue => TraVThuData.ParseNumber(T) ?? 0.0;

        public double? DentaValue => TraVThuData.ParseNumber(Denta);

        /// <summary>Cùng Loai + T + Góc + Dao (không phân biệt hoa thường / dấu tiếng Việt) = cùng 1 dòng trên Sheet.</summary>
        public bool SameKey(TraVThuRecord o) =>
            o != null && TraVThuData.SameLoai(Loai, o.Loai) && Math.Abs(TValue - o.TValue) < 0.05
            && Math.Abs(Goc - o.Goc) < 1e-4 && string.Equals(TraVThuData.NormalizeDao(Dao), TraVThuData.NormalizeDao(o.Dao), StringComparison.OrdinalIgnoreCase);

        public static TraVThuRecord FromParams(IDictionary<string, string> p)
        {
            string Get(string k) => p != null && p.TryGetValue(k, out string v) ? v ?? string.Empty : string.Empty;
            return new TraVThuRecord
            {
                Loai = Get("Loai").Trim(),
                T = Get("T").Trim(),
                Goc = TraVThuData.ParseNumber(Get("Goc")) ?? 0.0,
                Dao = TraVThuData.NormalizeDao(Get("Dao")),
                Denta = Get("Denta").Trim(),
                MaTam = Get("Ma_tam").Trim(),
                DinhDanh = Get("Dinh_danh").Trim().Length > 0 ? Get("Dinh_danh").Trim() : BeThu
            };
        }
    }

    public static class TraVThuData
    {
        public const string Header = "Loai,T,Goc,Dao,Denta,Ma_tam,Dinh_danh";

        /// <summary>Khoảng cách 2 góc bẻ thử dùng để nội suy lớn hơn mức này -> phải xác nhận với quản lý.</summary>
        public const double MaxInterpolationGap = 10.0;

        /// <summary>Chỉ có góc bẻ thử 1 phía: ngoại suy tối đa bấy nhiêu độ ra ngoài góc đo gần nhất.</summary>
        public const double MaxExtrapolationDistance = 2.0;

        /// <summary>1 trong 2 góc gốc cách góc cần tra chưa tới bấy nhiêu độ -> không cần hỏi quản lý.</summary>
        public const double NoConfirmDistance = 1.0;

        /// <summary>
        /// Đọc CSV trav_thu. Cột tìm theo tên tiêu đề (thứ tự tùy ý); không có tiêu đề thì hiểu theo thứ tự Header.
        /// Dòng thiếu Loai / Dao hoặc T, Góc, Denta không phải số thì bỏ qua.
        /// </summary>
        public static List<TraVThuRecord> Parse(string[] lines)
        {
            var list = new List<TraVThuRecord>();
            int iLoai = 0, iT = 1, iGoc = 2, iDao = 3, iDenta = 4, iMa = 5, iDinh = 6;
            foreach (string line in lines ?? new string[0])
            {
                string[] p = line.Split(',');
                if (IsHeaderRow(p))
                {
                    iLoai = iT = iGoc = iDao = iDenta = iMa = iDinh = -1;
                    for (int i = 0; i < p.Length; i++)
                    {
                        switch (Fold(p[i]).Replace("_", string.Empty).Replace(" ", string.Empty))
                        {
                            case "LOAI": iLoai = i; break;
                            case "T": iT = i; break;
                            case "GOC": iGoc = i; break;
                            case "DAO": iDao = i; break;
                            case "DENTA": iDenta = i; break;
                            case "MATAM": iMa = i; break;
                            case "DINHDANH": iDinh = i; break;
                        }
                    }
                    continue;
                }

                string Cell(int i) => i >= 0 && i < p.Length ? p[i].Trim() : string.Empty;
                double? t = ParseNumber(Cell(iT));
                double? goc = ParseNumber(Cell(iGoc));
                double? denta = ParseNumber(Cell(iDenta));
                if (Cell(iLoai).Length == 0 || Cell(iDao).Length == 0 || !t.HasValue || !goc.HasValue || !denta.HasValue)
                    continue;

                list.Add(new TraVThuRecord
                {
                    Loai = Cell(iLoai),
                    T = Cell(iT),
                    Goc = goc.Value,
                    Dao = NormalizeDao(Cell(iDao)),
                    Denta = Cell(iDenta),
                    MaTam = Cell(iMa),
                    DinhDanh = Cell(iDinh).Length > 0 ? Cell(iDinh) : TraVThuRecord.BeThu
                });
            }
            return list;
        }

        /// <summary>Có dòng tiêu đề (đủ cột Loai và Goc) — bảng rỗng chỉ có tiêu đề vẫn hợp lệ.</summary>
        public static bool HasHeader(string[] lines)
        {
            return lines != null && lines.Length > 0 && IsHeaderRow(lines[0].Split(','));
        }

        private static bool IsHeaderRow(string[] p)
        {
            var heads = p.Select(h => Fold(h).Replace("_", string.Empty).Replace(" ", string.Empty)).ToList();
            return heads.Contains("LOAI") && heads.Contains("GOC");
        }

        public static string ToCsvLine(TraVThuRecord r) => string.Join(",", new[]
        {
            Clean(r.Loai), Clean(r.T), TraVCalculator.FormatAngle(r.Goc), Clean(r.Dao), Clean(r.Denta), Clean(r.MaTam), Clean(r.DinhDanh)
        });

        public static string[] ToCsvLines(IEnumerable<TraVThuRecord> records) =>
            new[] { Header }.Concat(records.Select(ToCsvLine)).ToArray();

        /// <summary>Thêm hoặc thay dòng cùng khoá (Loai + T + Góc + Dao) — cùng quy tắc applyTravThu_ của Apps Script.</summary>
        public static void Upsert(List<TraVThuRecord> list, TraVThuRecord record)
        {
            int i = list.FindIndex(x => x.SameKey(record));
            if (i >= 0)
                list[i] = record;
            else
                list.Add(record);
        }

        public static bool SameLoai(string a, string b) =>
            string.Equals(NormalizeLoai(a), NormalizeLoai(b), StringComparison.Ordinal);

        /// <summary>"SS SIMA NGOÀI" / "ss sima ngoai" -> "SS SIMA NGOAI" (bỏ dấu, viết hoa, gộp dấu cách).</summary>
        public static string NormalizeLoai(string s) =>
            string.Join(" ", Fold(s).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        /// <summary>"12" -> "V12", "u18" -> "U18".</summary>
        public static string NormalizeDao(string s)
        {
            string d = (s ?? string.Empty).Trim().Replace(" ", string.Empty).ToUpperInvariant();
            if (d.Length > 0 && char.IsDigit(d[0]))
                d = "V" + d;
            return d;
        }

        /// <summary>Viết hoa + bỏ dấu tiếng Việt (đ -> D) để so khớp.</summary>
        public static string Fold(string s)
        {
            string n = (s ?? string.Empty).Trim().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(n.Length);
            foreach (char c in n)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }

        public static double? ParseNumber(string s) =>
            double.TryParse((s ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : (double?)null;

        private static string Clean(string v) => (v ?? string.Empty).Replace(",", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
    }

    /// <summary>
    /// Góc bẻ thử / nội suy máy này đã gửi nhưng admin chưa duyệt: lưu trav_thu_local.csv trong cache máy và luôn cộng
    /// lên bảng trav_thu tải từ Sheet (giống catalog_local.csv). Đồng bộ Sheet thì bỏ dòng đã duyệt / bị từ chối.
    /// </summary>
    public static class TraVThuLocalAdditions
    {
        public const string FileName = "trav_thu_local.csv";

        public static string PathIn(string cacheDirectory) =>
            Path.Combine(cacheDirectory ?? FnameDataStore.DefaultCacheDirectory, FileName);

        public static List<TraVThuRecord> Load(string cacheDirectory)
        {
            string path = PathIn(cacheDirectory);
            if (!File.Exists(path))
                return new List<TraVThuRecord>();
            try
            {
                return TraVThuData.Parse(FnameDataStore.SplitLines(File.ReadAllText(path, Encoding.UTF8)));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return new List<TraVThuRecord>();
        }

        public static void Save(string cacheDirectory, IEnumerable<TraVThuRecord> records)
        {
            string dir = cacheDirectory ?? FnameDataStore.DefaultCacheDirectory;
            Directory.CreateDirectory(dir);
            File.WriteAllText(PathIn(dir), string.Join("\n", TraVThuData.ToCsvLines(records)) + "\n", new UTF8Encoding(true));
        }

        public static void Add(string cacheDirectory, TraVThuRecord record)
        {
            List<TraVThuRecord> list = Load(cacheDirectory);
            TraVThuData.Upsert(list, record);
            Save(cacheDirectory, list);
        }

        /// <summary>
        /// Sheet đã có dòng cùng khoá + cùng Denta (admin đã duyệt) -> bỏ; stillPending khác null mà không còn dòng cùng khoá
        /// -> bị từ chối -> bỏ; stillPending = null (không đọc được hàng chờ) -> giữ. Trả về số dòng đã bỏ.
        /// </summary>
        public static int Prune(string cacheDirectory, IList<TraVThuRecord> sheetRecords, ICollection<TraVThuRecord> stillPending = null)
        {
            List<TraVThuRecord> list = Load(cacheDirectory);
            if (list.Count == 0)
                return 0;
            int before = list.Count;
            list.RemoveAll(e =>
            {
                if (stillPending != null && !stillPending.Any(p => p.SameKey(e)))
                    return true;
                TraVThuRecord s = sheetRecords?.FirstOrDefault(x => x.SameKey(e));
                return s != null && s.DentaValue.HasValue && e.DentaValue.HasValue && Math.Abs(s.DentaValue.Value - e.DentaValue.Value) < 1e-6;
            });
            if (list.Count != before)
                Save(cacheDirectory, list);
            return before - list.Count;
        }
    }
}
