using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    /// <summary>
    /// Phần dữ liệu cố định gửi kèm mỗi lô (bảng vật liệu, lời dặn) và key. Khối vật liệu phải
    /// ổn định từng byte giữa các lần gọi để prompt cache của Claude trúng.
    /// </summary>
    public static class DuLieuTinh
    {
        private static readonly UTF8Encoding Utf8KhongBom = new UTF8Encoding(false);

        public static string KhoiVatLieu(string[] dongSheet, IList<FnameLocalAdditions.Entry> choDuyet)
        {
            var sb = new StringBuilder("## BANG VAT LIEU CHUAN (M,Loai,Z,T_list)\n");
            foreach (string d in (dongSheet ?? new string[0]).Skip(1)) // dòng đầu là tiêu đề CSV
                sb.Append(d.Trim()).Append('\n');
            if (choDuyet != null && choDuyet.Count > 0)
            {
                sb.Append("\n## VAT LIEU MAY TU THEM - CHUA DUYET (chi bao vang neu M chi co o day)\n");
                foreach (FnameLocalAdditions.Entry e in choDuyet)
                    sb.Append(e.M).Append(',').Append(e.Loai).Append(',').Append(e.Z).Append(',')
                      .Append(string.Join(" ", e.Thicknesses ?? new List<string>())).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>Thư mục tab Fname nằm cạnh thư mục tab AI trong OHTA_TOOL.</summary>
        public static string ThuMucFname(string thuMucModuleAi) =>
            Path.Combine(Directory.GetParent(thuMucModuleAi.TrimEnd('\\', '/')).FullName, "01_Fname");

        /// <summary>Đọc bảng vật liệu thật mà tab Fname đang dùng: cache Google Sheet (hoặc CSV mẫu) + phần chờ duyệt.</summary>
        public static string KhoiVatLieuTrenMay(string thuMucModuleAi)
        {
            string[] sheet = FnameDataStore.ReadLines(ThuMucFname(thuMucModuleAi), "catalog");
            List<FnameLocalAdditions.Entry> cho;
            try { cho = FnameLocalAdditions.Load(FnameDataStore.DefaultCacheDirectory); }
            catch (Exception) { cho = new List<FnameLocalAdditions.Entry>(); }
            return KhoiVatLieu(sheet, cho);
        }

        /// <summary>File hướng dẫn AI chạy (system prompt), sửa trong cửa sổ cài đặt hoặc bằng Notepad.</summary>
        public static string FileHuongDan(string thuMucModule) => Path.Combine(thuMucModule, "data", "huong_dan_ai.md");

        private static string FileKey(string thuMucModule) => Path.Combine(thuMucModule, "data", "ai.key");

        public static string DocHuongDan(string thuMucModule, string macDinh)
        {
            string f = FileHuongDan(thuMucModule);
            if (File.Exists(f))
                return File.ReadAllText(f, Encoding.UTF8);
            GhiHuongDan(thuMucModule, macDinh);
            return macDinh;
        }

        public static void GhiHuongDan(string thuMucModule, string noiDung)
        {
            string f = FileHuongDan(thuMucModule);
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            File.WriteAllText(f, noiDung ?? string.Empty, Utf8KhongBom);
        }

        /// <summary>Ghi key vào data\ai.key (một dòng). File nằm trong .gitignore.</summary>
        public static void GhiKey(string thuMucModule, string key)
        {
            string f = FileKey(thuMucModule);
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            File.WriteAllText(f, (key ?? string.Empty).Trim() + "\n", Utf8KhongBom);
            if (File.Exists(f + ".txt")) File.Delete(f + ".txt"); // bản Notepad cũ không còn được dùng
        }

        public static void XoaKey(string thuMucModule)
        {
            string f = FileKey(thuMucModule);
            if (File.Exists(f)) File.Delete(f);
            if (File.Exists(f + ".txt")) File.Delete(f + ".txt");
        }

        /// <summary>Hiện key đã che: phần đầu "sk-ant-" + 4 ký tự cuối. Key quá ngắn thì chỉ "…".</summary>
        public static string AnKey(string key)
        {
            if (key == null) return null;
            const string dau = "sk-ant-";
            if (key.Length < 12) return "…";
            return (key.StartsWith(dau, StringComparison.Ordinal) ? dau : "") + "…" + key.Substring(key.Length - 4);
        }

        /// <summary>Dòng đầu không rỗng, không bắt đầu bằng # của data\ai.key; không có file thì null.</summary>
        public static string DocKey(string thuMucModule)
        {
            string f = FileKey(thuMucModule);
            if (!File.Exists(f))
                f = f + ".txt"; // Notepad hay lưu thành ai.key.txt (đuôi bị ẩn)
            if (!File.Exists(f))
                return null;
            return File.ReadAllLines(f, Encoding.UTF8)
                .Select(l => l.Trim().TrimStart('﻿'))
                .FirstOrDefault(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal));
        }
    }
}
