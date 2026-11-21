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
    }
}
