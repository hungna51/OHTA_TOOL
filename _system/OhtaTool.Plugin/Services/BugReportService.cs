using System;
using System.Collections.Generic;
using System.IO;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Gửi / liệt kê / đánh dấu đã xử lý báo lỗi qua Apps Script chung của Fname (ADR 0007).
    /// Mọi hàm có gọi mạng: chỉ gọi từ luồng nền.
    /// </summary>
    public static class BugReportService
    {
        public static string FnameDir => Path.Combine(CadCompat.GetRootDirectory() ?? "", "01_Fname");

        private static readonly object ResendGate = new object();

        /// <summary>
        /// Gửi 1 báo lỗi. Lỗi mạng -> lưu hàng chờ (queued = true), lần mở bảng OHTA sau gửi lại.
        /// Apps Script từ chối (ok:false) -> không lưu: gửi lại cũng vậy, người dùng xem thông báo.
        /// </summary>
        public static (bool ok, bool queued, string message) Submit(BugReport r)
        {
            BugSendResult res = TrySend(r);
            if (res.Ok)
            {
                BugQueue.Remove(r.Id);
                BugLog.Write("BUG", "da gui bao loi " + r.Id);
                return (true, false, "Đã gửi báo lỗi cho admin. Cảm ơn bạn!");
            }
            if (res.RejectedByServer)
            {
                BugLog.Write("BUG", "may chu tu choi " + r.Id + ": " + res.Message);
                return (false, false, "Admin chưa nhận được báo lỗi: " + res.Message);
            }
            try
            {
                BugQueue.Enqueue(r);
            }
            catch (Exception ex)
            {
                BugLog.Write("BUG", ex);
                return (false, false, "Chưa gửi được (" + res.Message + ") và không lưu được hàng chờ: " + ex.Message);
            }
            BugLog.Write("BUG", "chua gui duoc " + r.Id + ": " + res.Message);
            return (false, true, "Chưa gửi được (" + res.Message + "). Báo lỗi đã lưu, sẽ tự gửi lại lần mở bảng OHTA sau.");
        }

        private static BugSendResult TrySend(BugReport r)
        {
            int timeout = r.DinhKemZip != null ? 300 : r.AnhJpeg != null ? 60 : 30;
            FnameSheetReply reply = SheetWebApp.Post(FnameDir, "submit_bug", r.ToFields(), timeout, out _, out string error);
            return BugSendResult.FromReply(reply, error);
        }

        /// <summary>Gửi lại các báo lỗi trong hàng chờ; trả số đã gửi được. Không ném.</summary>
        public static int ResendQueued()
        {
            try
            {
                // Mở bảng OHTA 2 lần liền nhau không được gửi song song 2 lượt
                if (!System.Threading.Monitor.TryEnter(ResendGate))
                    return 0;
                try
                {
                    return BugResender.Run(TrySend);
                }
                finally
                {
                    System.Threading.Monitor.Exit(ResendGate);
                }
            }
            catch (Exception ex)
            {
                BugLog.Write("BUG", ex);
                return 0;
            }
        }

        public static (bool ok, bool isAdmin, List<BugAdminItem> bugs, string message) ListBugs()
        {
            FnameSheetReply reply = SheetWebApp.Post(FnameDir, "list_bugs", new Dictionary<string, string>(), 30, out string body, out string error);
            if (reply == null)
                return (false, false, new List<BugAdminItem>(), error);
            return (reply.Ok, reply.IsAdmin, BugAdminItem.ParseList(body), reply.Message ?? "");
        }

        public static (bool ok, string message) Resolve(string id, string banSua)
        {
            FnameSheetReply reply = SheetWebApp.Post(FnameDir, "resolve_bug",
                new Dictionary<string, string> { ["id"] = id ?? "", ["ban_sua"] = banSua ?? "" }, 60, out _, out string error);
            if (reply == null)
                return (false, error);
            return (reply.Ok, reply.Message ?? "");
        }
    }
}
