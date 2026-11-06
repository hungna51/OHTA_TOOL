using System;
using System.Collections.Generic;
using System.IO;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Liệt kê / đánh dấu đã xử lý báo lỗi (gửi: BugSender) qua Apps Script chung của Fname (ADR 0007).
    /// Mọi hàm có gọi mạng: chỉ gọi từ luồng nền.
    /// </summary>
    public static class BugReportService
    {
        public static string FnameDir => Path.Combine(CadCompat.GetRootDirectory() ?? "", "01_Fname");

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
