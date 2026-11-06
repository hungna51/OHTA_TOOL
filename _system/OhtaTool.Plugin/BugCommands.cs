using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Services;
using OhtaTool.Plugin.Views;

[assembly: CommandClass(typeof(OhtaTool.Plugin.BugCommands))]

namespace OhtaTool.Plugin
{
    /// <summary>Báo lỗi (ADR 0007): lệnh OHTABUG và các hàm lisp ohta_net_log / ohta_net_bug_auto.</summary>
    public sealed class BugCommands
    {
        /// <summary>File lisp ghi khi bản vẽ không gọi được ohta_net_bug_auto (giống cách GROTHONGTIN, mục 114 GURO).</summary>
        public static string AutoFile =>
            System.IO.Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? System.IO.Path.GetTempPath(), "ohta_bug_auto.txt");

        /// <summary>
        /// Lệnh nội bộ: *error* của GRO gửi qua (vla-sendcommand "OHTABUGAUTO ") khi hàm lisp .NET không có
        /// trong bản vẽ này. Đọc %TEMP%\ohta_bug_auto.txt (dòng 1 = nguồn, phần còn lại = lỗi), xoá file, hỏi gửi báo lỗi.
        /// </summary>
        [CommandMethod("OHTABUGAUTO")]
        public static void CmdBugAuto()
        {
            try
            {
                string path = AutoFile;
                if (!System.IO.File.Exists(path))
                    return;
                string text = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8); // ADODB UTF-8 có BOM: ReadAllText tự bỏ
                System.IO.File.Delete(path);
                int nl = text.IndexOf('\n');
                string nguon = (nl > 0 ? text.Substring(0, nl) : "LISP").Trim();
                string loi = (nl > 0 ? text.Substring(nl + 1) : text).Trim();
                BugGuard.ReportLispError(nguon.Length == 0 ? "LISP" : nguon, loi);
            }
            catch (Exception ex)
            {
                BugLog.Write("BUG", ex);
            }
        }

        /// <summary>Mở form báo lỗi. Chụp cửa sổ CAD TRƯỚC khi form hiện để ảnh không bị form che.</summary>
        [CommandMethod("OHTABUG")]
        public static void CmdBugReport() => OpenForm(OhtaPaletteCommands.CurrentTabTitle() ?? "OHTA");

        /// <summary>Dùng chung cho lệnh OHTABUG và nút 🐞 Báo lỗi trên tab Info.</summary>
        public static void OpenForm(string tabLenh)
        {
            byte[] anh = CadScreenshot.CaptureMainWindowJpeg();
            BugLog.Write("BUG", "mo form bao loi (" + tabLenh + ")");
            BugReportWindow.Show(tabLenh, null, null, anh);
        }

        /// <summary>Gắn phần CAD/UI vào BugGuard (gọi 1 lần trong Initialize của OHTATOOL.dll).</summary>
        public static void WireGuard()
        {
            BugGuard.Capture = CadScreenshot.CaptureMainWindowJpeg;
            BugGuard.AskUser = (nguon, loi) => System.Windows.MessageBox.Show(
                "Lệnh " + nguon + " gặp lỗi:\n\n" + loi + "\n\nGửi báo lỗi cho admin (kèm ảnh cửa sổ CAD và nhật ký)?",
                "OHTA TOOL — Gặp lỗi", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
                == System.Windows.MessageBoxResult.Yes;
            BugGuard.ShowForm = BugReportWindow.Show;
            BugGuard.Notify = CadCompat.WriteMessage;
            BugSender.Init();
        }

        /// <summary>
        /// (ohta_net_bug_auto "GRO" "Loi: ... (buoc: ...)") -> T. *error* của lisp gọi khi gặp lỗi thật
        /// (không phải Esc/Cancel), qua vl-catch-all-apply.
        /// </summary>
        [LispFunction("OHTA_NET_BUG_AUTO")]
        public static object OhtaNetBugAuto(ResultBuffer rb)
        {
            List<string> args = StringArgs(rb);
            BugGuard.ReportLispError(args.Count > 0 && args[0].Length > 0 ? args[0] : "LISP", args.Count > 1 ? args[1] : "");
            return true;
        }

        /// <summary>(ohta_net_log "nguon" "noi dung") -> T. Lisp gọi qua vl-catch-all-apply để không hỏng khi DLL chưa nạp.</summary>
        [LispFunction("OHTA_NET_LOG")]
        public static object OhtaNetLog(ResultBuffer rb)
        {
            List<string> args = StringArgs(rb);
            BugLog.Write(args.Count > 0 ? args[0] : "", args.Count > 1 ? args[1] : "");
            return true;
        }

        internal static List<string> StringArgs(ResultBuffer rb)
        {
            var list = new List<string>();
            if (rb != null)
                foreach (var tv in rb.AsArray())
                    list.Add(tv.Value as string ?? tv.Value?.ToString() ?? "");
            return list;
        }
    }
}
