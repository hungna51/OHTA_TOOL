using System;
using System.Collections.Generic;
using System.IO;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Ghi vào nhật ký báo lỗi (BugLog) mọi lệnh người dùng chạy — lệnh CAD, lệnh của tool, lệnh lisp như GRO —
    /// kèm khi lệnh bị huỷ / lỗi, để admin xem được người dùng đã làm gì trước khi gặp lỗi.
    /// </summary>
    public static class CommandLog
    {
        private static readonly HashSet<CadDocument> Hooked = new HashSet<CadDocument>();

        /// <summary>Gọi 1 lần trong Initialize (luồng CAD).</summary>
        public static void Hook()
        {
            try
            {
                CadApp.DocumentManager.DocumentCreated += (s, e) => Attach(e.Document);
                foreach (CadDocument d in CadApp.DocumentManager)
                    Attach(d);
            }
            catch (Exception ex)
            {
                BugLog.Write("LOG", ex);
            }
        }

        private static void Attach(CadDocument doc)
        {
            if (doc == null || !Hooked.Add(doc))
                return;
            try
            {
                string ten = Path.GetFileName(doc.Name ?? "");
                BugLog.Write("CAD", "ban ve: " + ten);
                doc.CommandWillStart += (s, e) => BugLog.Write("CAD", "lenh " + e.GlobalCommandName + "  [" + ten + "]");
                doc.CommandCancelled += (s, e) => BugLog.Write("CAD", "lenh " + e.GlobalCommandName + " bi huy");
                doc.CommandFailed += (s, e) => BugLog.Write("CAD", "lenh " + e.GlobalCommandName + " LOI");
                // Lệnh lisp (c:gro, c:xln...) không phát CommandWillStart
                doc.LispWillStart += (s, e) => BugLog.Write("CAD", "lisp " + e.FirstLine + "  [" + ten + "]");
                doc.BeginDocumentClose += (s, e) => Hooked.Remove(doc);
            }
            catch (Exception ex)
            {
                BugLog.Write("LOG", ex);
            }
        }
    }
}
