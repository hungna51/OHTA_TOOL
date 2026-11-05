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
        private static int _resent;

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

        /// <summary>Mở bảng OHTA: gửi lại báo lỗi còn trong hàng chờ ở luồng nền, xong báo ra dòng lệnh (ở luồng CAD).</summary>
        public static void ResendQueuedInBackground()
        {
            try
            {
                if (BugQueue.Pending().Count == 0)
                    return;
                Task.Run(() =>
                {
                    int n = BugReportService.ResendQueued();
                    if (n <= 0)
                        return;
                    _resent = n;
                    CadApp.Idle += OnIdleReportResent;
                });
            }
            catch (Exception ex)
            {
                BugLog.Write("BUG", ex);
            }
        }

        private static void OnIdleReportResent(object sender, EventArgs e)
        {
            CadApp.Idle -= OnIdleReportResent;
            CadCompat.WriteMessage("\n[OHTA TOOL] Da gui " + _resent + " bao loi con cho cho admin.\n");
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
