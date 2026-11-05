using System.Collections.Generic;
using OhtaTool.Plugin.Core;

[assembly: CommandClass(typeof(OhtaTool.Plugin.BugCommands))]

namespace OhtaTool.Plugin
{
    /// <summary>Báo lỗi (ADR 0007): lệnh OHTABUG và các hàm lisp ohta_net_log / ohta_net_bug_auto.</summary>
    public sealed class BugCommands
    {
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
