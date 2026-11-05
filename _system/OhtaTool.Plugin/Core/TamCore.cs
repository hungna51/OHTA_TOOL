using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
                string[] p = line.Split(',');
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
            lines != null && lines.Length > 0 && IsHeaderRow(lines[0].Split(','));

        private static bool IsHeaderRow(string[] p)
        {
            var keys = p.Select(Key).ToList();
            return keys.Contains("LOAI") && keys.Contains("THIENTHI");
        }

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
}
