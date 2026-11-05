using System;
using System.IO;
using System.Linq;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Nơi đọc các bảng dữ liệu Fname (catalog, trav_90, trav_nho90, trav_nho90r, trav_thu, tam, cau_hinh) dạng CSV.
    /// Ưu tiên bản tải từ Google Sheet trong cache máy, không có thì dùng bản mẫu 01_Fname/data/.
    /// </summary>
    public static class FnameDataStore
    {
        public static readonly string[] TableNames = { "catalog", "trav_90", "trav_nho90", "trav_nho90r", "trav_thu", "tam", "cau_hinh" };

        /// <summary>Bảng chưa khai gid trong google_sheet.ini thì bỏ qua khi tải (giữ cache cũ / CSV mẫu) thay vì báo lỗi.</summary>
        public static bool IsOptionalTable(string table) => table == "trav_thu" || table == "tam" || table == "cau_hinh";

        public static string DefaultCacheDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "fname_data");

        public static string[] ReadLines(string moduleDirectory, string table, string cacheDirectory = null)
        {
            string[] dirs =
            {
                cacheDirectory ?? DefaultCacheDirectory,
                Path.Combine(moduleDirectory ?? string.Empty, "data")
            };
            foreach (string dir in dirs)
            {
                string path = Path.Combine(dir, table + ".csv");
                if (!File.Exists(path))
                    continue;
                try
                {
                    // FileShare.ReadWrite: vẫn đọc được khi Excel đang mở file.
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                        return SplitLines(sr.ReadToEnd());
                }
                catch (IOException) { }               // bị khoá cứng → thử nguồn kế tiếp
                catch (UnauthorizedAccessException) { }
            }
            return new string[0];
        }

        public static string[] SplitLines(string text)
        {
            return (text ?? string.Empty).TrimStart('﻿').Replace("\r", string.Empty)
                .Split('\n').Where(l => l.Trim().Trim(',').Length > 0).ToArray();
        }

        /// <summary>
        /// Kiểm tra CSV tải về trước khi ghi đè cache: phải parse ra dữ liệu thật.
        /// </summary>
        public static bool Validate(string table, string[] lines, out string error)
        {
            error = null;
            if (lines.Length == 0 || lines[0].TrimStart().StartsWith("<", StringComparison.Ordinal))
                error = "không phải CSV (Sheet chưa chia sẻ 'Bất kỳ ai có link' hoặc sai gid)";
            // tab tam có cột Ghi_chu do người gõ -> ô có dấu phẩy được Sheet bọc ngoặc kép (TamData đọc được), không phải lỗi locale
            else if (table != "tam" && lines.Any(l => l.IndexOf('"') >= 0))
                error = "có dấu \" trong dữ liệu (đặt Locale của Sheet = United States để dùng dấu chấm thập phân)";
            else
            {
                bool ok;
                switch (table)
                {
                    case "catalog": ok = FnameCatalogData.Parse(lines).Materials.Count > 0; break;
                    case "trav_90": ok = TraVCalculator.ParseTable90(lines).Count > 0; break;
                    case "trav_thu": ok = TraVThuData.HasHeader(lines); break;
                    case "tam": ok = TamData.HasHeader(lines); break;
                    case "cau_hinh": ok = FnameSheetConfig.HasHeader(lines); break; // góc bẻ thử: chưa có dòng nào vẫn hợp lệ
                    default: ok = TraVCalculator.ParseGridRowCount(lines) > 0; break;
                }
                if (!ok)
                    error = "không đọc được dòng dữ liệu hợp lệ nào";
            }
            return error == null;
        }
    }
}
