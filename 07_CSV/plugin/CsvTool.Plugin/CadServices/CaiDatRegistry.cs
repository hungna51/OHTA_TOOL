using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CsvTool.Core;
using Microsoft.Win32;

namespace CsvTool.CadServices
{
    /// <summary>Đọc / ghi CaiDatCsv trong registry HKCU.</summary>
    public static class CaiDatRegistry
    {
        public const string KhoaMoi = @"Software\OFT_Lisp\CSV";
        public const string KhoaCu = @"Software\Autodesk";   // khoá lisp xuat_csv cũ dùng

        public static CaiDatCsv Doc()
        {
            using (RegistryKey moi = Registry.CurrentUser.OpenSubKey(KhoaMoi))
            using (RegistryKey cu = Registry.CurrentUser.OpenSubKey(KhoaCu))
                return CaiDatCsv.Doc(k => moi?.GetValue(k), k => cu?.GetValue(k));
        }

        public static void Ghi(CaiDatCsv c)
        {
            using (RegistryKey moi = Registry.CurrentUser.CreateSubKey(KhoaMoi))
                c.Ghi((k, v) => moi.SetValue(k, v));
        }

        // Tạo file A và PDF nhanh: nhớ riêng theo CAD đang dùng
#if ZWCAD
        private const string KhoaNhanh = KhoaMoi + @"\Nhanh\ZWCAD";
#else
        private const string KhoaNhanh = KhoaMoi + @"\Nhanh\AutoCAD";
#endif

        public static (bool TaoFileA, bool InPdf, string Profile) DocNhanh()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(KhoaNhanh))
                    return (k?.GetValue("TaoFileA") is int a && a == 1, k?.GetValue("InPdf") is int b && b == 1, k?.GetValue("ProfileIn") as string);
            }
            catch (Exception) { return (false, false, null); }
        }

        public static void GhiNhanh(bool taoFileA, bool inPdf, string profile)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(KhoaNhanh))
                {
                    k.SetValue("TaoFileA", taoFileA ? 1 : 0, RegistryValueKind.DWord);
                    k.SetValue("InPdf", inPdf ? 1 : 0, RegistryValueKind.DWord);
                    k.SetValue("ProfileIn", profile ?? "");
                }
            }
            catch (Exception) { }   // không nhớ được thì lần sau về mặc định, không chặn việc in
        }

        /// <summary>Tên profile in của tab PRINT (HKCU\Software\OFT_Lisp\IN, giá trị profiles dạng JSON).</summary>
        public static List<string> DocTenProfileIn()
        {
            var kq = new List<string>();
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\OFT_Lisp\IN"))
                    if (k?.GetValue("profiles") is string json && OhtaTool.Plugin.Core.JsonLite.Parse(json) is List<object> ds)
                        foreach (var o in ds.OfType<OhtaTool.Plugin.Core.JsonObject>())
                            if (!string.IsNullOrWhiteSpace(o.GetString("name"))) kq.Add(o.GetString("name"));
            }
            catch (Exception) { }
            return kq;
        }

        // chuỗi kết nối có mật khẩu: mã hoá DPAPI theo tài khoản Windows, không để trong CaiDatCsv
        private const string TenKetNoi = "KetNoiBangGia";
        private static readonly byte[] MuoiKetNoi = Encoding.ASCII.GetBytes("OHTA-CSV-KETNOI");

        public static string DocKetNoi()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(KhoaMoi))
                    return k?.GetValue(TenKetNoi) is string s
                        ? Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(s), MuoiKetNoi, DataProtectionScope.CurrentUser))
                        : null;
            }
            catch (Exception) { return null; }
        }

        public static void GhiKetNoi(string chuoi)
        {
            byte[] ma = ProtectedData.Protect(Encoding.UTF8.GetBytes(chuoi), MuoiKetNoi, DataProtectionScope.CurrentUser);
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(KhoaMoi))
                k.SetValue(TenKetNoi, Convert.ToBase64String(ma));
        }

        public static void XoaKetNoi()
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(KhoaMoi))
                k.DeleteValue(TenKetNoi, false);
            NguonGiaCoDem.XoaDem();
        }
    }
}
