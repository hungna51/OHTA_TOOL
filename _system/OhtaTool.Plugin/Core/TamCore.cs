using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// 1 dòng bảng "tam" trên Google Sheet: thông số thuộc về tấm (không phụ thuộc dao), khoá = Loai + T.
    /// T_hienthi = T thực để người dùng xem / tính toán (khác T_thuc của trav_90 — số dùng trong công thức denta góc tù).
    /// SIMA ghi 1 dòng chung không kèm chiều ("SS SIMA"), dùng cho cả "SS SIMA Trong" lẫn "SS SIMA Ngoài".
    /// </summary>
    public sealed class TamRecord
    {
        public const string NguonOft = "OFT";
        public const string NguonBoSung = "Bổ sung";

        public string Loai { get; set; } = string.Empty;
        public string T { get; set; } = string.Empty;
        public string THienThi { get; set; } = string.Empty;
        public string Nguon { get; set; } = NguonOft;
        public string NguoiXacNhan { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public string Anh { get; set; } = string.Empty;
        public string Ngay { get; set; } = string.Empty;

        public double TValue => TraVThuData.ParseNumber(T) ?? 0.0;

        /// <summary>Dòng máy này đã gửi bổ sung nhưng admin chưa duyệt (đọc từ tam_local.csv, không có trên Sheet).</summary>
        public bool ChoDuyet { get; set; }

        public bool IsBoSung => TraVThuData.Fold(Nguon).StartsWith("BO SUNG", StringComparison.Ordinal);

        /// <summary>Link ảnh bằng chứng (cột Anh: nhiều link cách nhau dấu cách / ;).</summary>
        public List<string> AnhLinks => TamData.SplitLinks(Anh);

        public static TamRecord FromParams(IDictionary<string, string> p)
        {
            string Get(string k) => p != null && p.TryGetValue(k, out string v) ? (v ?? string.Empty).Trim() : string.Empty;
            return new TamRecord
            {
                Loai = Get("Loai"),
                T = Get("T"),
                THienThi = Get("T_hienthi"),
                Nguon = NguonBoSung,
                NguoiXacNhan = Get("Nguoi_xac_nhan"),
                GhiChu = Get("Ghi_chu"),
                Anh = Get("Anh"),
                Ngay = Get("Ngay")
            };
        }
    }

    public static class TamData
    {
        public const string Header = "Loai,T,T_hienthi,Nguon,Nguoi_xac_nhan,Ghi_chu,Anh,Ngay";

        /// <summary>Đọc CSV bảng tam theo tên cột (thứ tự cột tùy ý). Dòng thiếu Loai hoặc T không phải số thì bỏ.</summary>
        public static List<TamRecord> Parse(string[] lines)
        {
            var list = new List<TamRecord>();
            var col = new Dictionary<string, int>();
            foreach (string line in lines ?? new string[0])
            {
                string[] p = SplitCsvLine(line);
                if (IsHeaderRow(p))
                {
                    col.Clear();
                    for (int i = 0; i < p.Length; i++)
                        col[Key(p[i])] = i;
                    continue;
                }
                if (col.Count == 0)
                    continue; // chưa gặp dòng tiêu đề -> không đoán vị trí cột

                string Cell(string name) => col.TryGetValue(name, out int i) && i < p.Length ? p[i].Trim() : string.Empty;
                if (Cell("LOAI").Length == 0 || !TraVThuData.ParseNumber(Cell("T")).HasValue)
                    continue;
                list.Add(new TamRecord
                {
                    Loai = Cell("LOAI"),
                    T = Cell("T"),
                    THienThi = Cell("THIENTHI"),
                    Nguon = Cell("NGUON").Length > 0 ? Cell("NGUON") : TamRecord.NguonOft,
                    NguoiXacNhan = Cell("NGUOIXACNHAN"),
                    GhiChu = Cell("GHICHU"),
                    Anh = Cell("ANH"),
                    Ngay = Cell("NGAY")
                });
            }
            return list;
        }

        public static bool HasHeader(string[] lines) =>
            lines != null && lines.Length > 0 && IsHeaderRow(SplitCsvLine(lines[0]));

        private static bool IsHeaderRow(string[] p)
        {
            var keys = p.Select(Key).ToList();
            return keys.Contains("LOAI") && keys.Contains("THIENTHI");
        }

        /// <summary>
        /// Tách 1 dòng CSV có hỗ trợ ô trong ngoặc kép ("a, b" và "" bên trong) — cột Ghi_chu do người gõ có thể có dấu phẩy / ngoặc kép,
        /// Google Sheet xuất CSV sẽ bọc ô đó trong ngoặc kép.
        /// </summary>
        public static string[] SplitCsvLine(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            string s = line ?? string.Empty;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < s.Length && s[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else sb.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { cells.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            cells.Add(sb.ToString());
            return cells.ToArray();
        }

        public static List<string> SplitLinks(string anh) =>
            (anh ?? string.Empty).Split(new[] { ' ', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.StartsWith("http", StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary>Thêm hoặc thay dòng cùng khoá Loai (bỏ chiều SIMA) + T — cùng quy tắc applyTam_ của Apps Script.</summary>
        public static void Upsert(List<TamRecord> list, TamRecord record)
        {
            int i = list.FindIndex(x => SameLoai(x.Loai, record.Loai) && Math.Abs(x.TValue - record.TValue) < 0.05);
            if (i >= 0)
                list[i] = record;
            else
                list.Add(record);
        }

        public static string ToCsvLine(TamRecord r) => string.Join(",", new[]
        {
            Clean(r.Loai), Clean(r.T), Clean(r.THienThi), Clean(r.Nguon), Clean(r.NguoiXacNhan), Clean(r.GhiChu), Clean(r.Anh), Clean(r.Ngay)
        });

        public static string[] ToCsvLines(IEnumerable<TamRecord> rows) =>
            new[] { Header }.Concat(rows.Select(ToCsvLine)).ToArray();

        /// <summary>Bỏ ký tự phân cách CSV khỏi giá trị (dấu phẩy, ngoặc kép, xuống dòng) — cùng cleanText_ của Apps Script.</summary>
        public static string Clean(string v) =>
            (v ?? string.Empty).Replace(',', ' ').Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ').Trim();

        private static string Key(string h) => TraVThuData.Fold(h).Replace("_", string.Empty).Replace(" ", string.Empty);

        /// <summary>
        /// Loại dùng làm khoá bảng tam: chuẩn hoá (bỏ dấu, viết hoa) và bỏ chiều của SIMA —
        /// "SUS SIMA Ngoài" / "sus sima trong" / "SUS SIMA" -> "SUS SIMA".
        /// </summary>
        public static string BaseLoai(string loai)
        {
            string n = TraVThuData.NormalizeLoai(loai);
            if (n.Contains(" SIMA ") && (n.EndsWith(" TRONG", StringComparison.Ordinal) || n.EndsWith(" NGOAI", StringComparison.Ordinal)))
                n = n.Substring(0, n.LastIndexOf(' '));
            return n;
        }

        public static bool SameLoai(string a, string b) => string.Equals(BaseLoai(a), BaseLoai(b), StringComparison.Ordinal);

        /// <summary>Dòng tam của loại (tên trav_90, có thể kèm chiều SIMA) + độ dày; không có T_hienthi thì coi như chưa có.</summary>
        public static TamRecord Find(IEnumerable<TamRecord> rows, string loai, double t) =>
            rows?.FirstOrDefault(r => SameLoai(r.Loai, loai) && Math.Abs(r.TValue - t) < 0.05 && r.THienThi.Length > 0);
    }

    /// <summary>
    /// Tab "cau_hinh" trên Google Sheet: cặp Khoa / Gia_tri do admin sửa tay (vd phien_ban_toi_thieu = 1.3.0).
    /// </summary>
    public static class FnameSheetConfig
    {
        public const string MinVersionKey = "phien_ban_toi_thieu";

        public static Dictionary<string, string> Parse(string[] lines)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in lines ?? new string[0])
            {
                string[] p = line.Split(',');
                string k = p[0].Trim();
                if (k.Length == 0 || IsHeaderRow(p))
                    continue;
                d[k] = p.Length > 1 ? p[1].Trim() : string.Empty;
            }
            return d;
        }

        public static bool HasHeader(string[] lines) =>
            lines != null && lines.Length > 0 && IsHeaderRow(lines[0].Split(','));

        private static bool IsHeaderRow(string[] p) =>
            p.Length > 1 && TraVThuData.Fold(p[0]) == "KHOA" && TraVThuData.Fold(p[1]).Replace("_", string.Empty) == "GIATRI";
    }

    /// <summary>Kiểm tra liên kết giữa các bảng sau khi tải (chỉ cảnh báo, không chặn).</summary>
    public static class FnameDataCheck
    {
        /// <param name="table90Loai">Các giá trị Mat của trav_90.</param>
        public static List<string> Check(FnameCatalogData catalog, IEnumerable<string> table90Loai, IEnumerable<TamRecord> tam)
        {
            var warnings = new List<string>();
            var known = new HashSet<string>((table90Loai ?? Enumerable.Empty<string>()).Select(TamData.BaseLoai), StringComparer.Ordinal);

            foreach (FnameMaterialSpec m in catalog?.Materials ?? new List<FnameMaterialSpec>())
            {
                string loai = (m.Loai ?? string.Empty).Trim();
                if (loai.Length > 0 && !known.Contains(TamData.BaseLoai(loai)) && !known.Contains(TamData.BaseLoai(loai + " SIMA")))
                    warnings.Add($"catalog: mác \"{m.M}\" có Loai = \"{loai}\" không có trong trav_90.");
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (TamRecord r in tam ?? Enumerable.Empty<TamRecord>())
            {
                string key = TamData.BaseLoai(r.Loai) + " | " + r.TValue.ToString("0.###", CultureInfo.InvariantCulture);
                if (!seen.Add(key))
                    warnings.Add($"tam: trùng dòng {r.Loai} T={r.T} (chỉ dùng dòng đầu).");
                if (!known.Contains(TamData.BaseLoai(r.Loai)) && !(catalog?.Materials.Any(m => TamData.SameLoai(m.Loai, r.Loai)) ?? false))
                    warnings.Add($"tam: Loai \"{r.Loai}\" không có trong trav_90 / catalog.");
            }
            return warnings;
        }
    }

    /// <summary>
    /// Thông số tấm máy này đã gửi bổ sung nhưng admin chưa duyệt: lưu tam_local.csv trong cache máy, luôn cộng lên bảng tam
    /// tải từ Sheet (dòng đánh dấu ChoDuyet). Đồng bộ Sheet thì bỏ dòng đã duyệt (Sheet có cùng khoá) hoặc bị từ chối.
    /// </summary>
    public static class TamLocalAdditions
    {
        public const string FileName = "tam_local.csv";

        public static string PathIn(string cacheDirectory) =>
            Path.Combine(cacheDirectory ?? FnameDataStore.DefaultCacheDirectory, FileName);

        public static List<TamRecord> Load(string cacheDirectory)
        {
            string path = PathIn(cacheDirectory);
            if (!File.Exists(path))
                return new List<TamRecord>();
            try
            {
                var list = TamData.Parse(FnameDataStore.SplitLines(File.ReadAllText(path, Encoding.UTF8)));
                foreach (TamRecord r in list)
                    r.ChoDuyet = true;
                return list;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return new List<TamRecord>();
        }

        public static void Save(string cacheDirectory, IEnumerable<TamRecord> rows)
        {
            string dir = cacheDirectory ?? FnameDataStore.DefaultCacheDirectory;
            Directory.CreateDirectory(dir);
            File.WriteAllText(PathIn(dir), string.Join("\n", TamData.ToCsvLines(rows)) + "\n", new UTF8Encoding(true));
        }

        public static void Add(string cacheDirectory, TamRecord record)
        {
            List<TamRecord> list = Load(cacheDirectory);
            TamData.Upsert(list, record);
            Save(cacheDirectory, list);
        }

        /// <summary>
        /// Sheet đã có dòng cùng khoá có T_hienthi (admin đã duyệt) -> bỏ; stillPending khác null mà không còn dòng cùng khoá -> bị từ chối -> bỏ;
        /// stillPending = null (không đọc được hàng chờ) -> giữ. Trả về số dòng đã bỏ.
        /// </summary>
        public static int Prune(string cacheDirectory, IList<TamRecord> sheetRows, ICollection<TamRecord> stillPending = null)
        {
            List<TamRecord> list = Load(cacheDirectory);
            if (list.Count == 0)
                return 0;
            int before = list.Count;
            list.RemoveAll(e =>
                (stillPending != null && !stillPending.Any(p => TamData.SameLoai(p.Loai, e.Loai) && Math.Abs(p.TValue - e.TValue) < 0.05))
                || TamData.Find(sheetRows, e.Loai, e.TValue) != null);
            if (list.Count != before)
                Save(cacheDirectory, list);
            return before - list.Count;
        }
    }
}
