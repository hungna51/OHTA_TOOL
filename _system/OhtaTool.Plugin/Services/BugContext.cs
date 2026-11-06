using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>Thông tin máy gửi kèm báo lỗi — cùng nội dung lệnh OHTAINFO.</summary>
    public static class BugContext
    {
        /// <returns>phienBan: 1 dòng cho ô Sheet; info: nhiều dòng cho file chi tiết.</returns>
        public static (string phienBan, string info) Describe()
        {
            var sb = new StringBuilder();
            var tabs = new List<string>();
            string root = null;
            try
            {
                root = CadCompat.GetRootDirectory();
                if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                    foreach (ModuleManifest m in ModuleDiscovery.DiscoverModules(root))
                        tabs.Add(m.TabTitle + (m.VersionLabel.Length == 0 ? "" : " " + m.VersionLabel));
            }
            catch
            {
            }
            string tool = "OHTA v" + OhtaUpdateCommands.LoadedVersion;
            sb.AppendLine("Phien ban   : " + tool);
            string cad = CadCompat.PlatformName + " " + CadVersion();
            sb.AppendLine("Nen tang CAD: " + cad);
            sb.AppendLine("Thu muc goc : " + (root ?? "(khong tim thay)"));
            sb.AppendLine("May / user  : " + Environment.MachineName + " / " + Environment.UserName);
            sb.AppendLine("Windows     : " + Environment.OSVersion);
            sb.AppendLine("Ban ve      : " + SafeDocName());
            sb.AppendLine("Cac tab     : " + string.Join(", ", tabs));
            // DLL thật đang nạp (máy chạy DLL cũ / nạp nhầm thư mục là nguyên nhân hay gặp của "máy khác không chạy")
            foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                string n = a.GetName().Name;
                if (n == "OHTATOOL" || n == "GROTOOL" || n == "AITOOL")
                {
                    string loc;
                    try { loc = a.Location; } catch { loc = "?"; }
                    string time;
                    try { time = File.GetLastWriteTime(loc).ToString("yyyy-MM-dd HH:mm"); } catch { time = "?"; }
                    sb.AppendLine("DLL " + n.PadRight(8) + ": " + loc + " (" + time + ")");
                }
            }
            return (tool + " | " + cad + " | " + string.Join(", ", tabs), sb.ToString());
        }

        /// <summary>Bản CAD thật (sysvar ACADVER, vd "25.0s (LMS Tech)") — chỉ gọi trên luồng CAD.</summary>
        private static string CadVersion()
        {
            try { return "ACADVER " + CadApp.GetSystemVariable("ACADVER"); }
            catch { return ""; }
        }

        private static string SafeDocName()
        {
            try { return CadCompat.ActiveDocument?.Name ?? "(khong co)"; }
            catch { return "(khong doc duoc)"; }
        }
    }
}
