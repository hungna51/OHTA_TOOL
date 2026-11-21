using System;
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
