using System;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Kết quả 1 lần gửi báo lỗi.</summary>
    public sealed class BugSendResult
    {
        public bool Ok { get; private set; }
        /// <summary>Apps Script đã trả lời ok:false (sai dữ liệu / sai khoá) — gửi lại cũng không được.</summary>
        public bool RejectedByServer { get; private set; }
        public string Message { get; private set; }

        public static BugSendResult Sent(string message = "") => new BugSendResult { Ok = true, Message = message };
        public static BugSendResult Rejected(string message) => new BugSendResult { RejectedByServer = true, Message = message };
        public static BugSendResult NetworkError(string message) => new BugSendResult { Message = message };

        /// <summary>
        /// Câu trả lời Apps Script -> kết quả: null = không gửi tới được (lỗi mạng); ok:false kèm retry (exception
        /// trong doPost, hết thời gian chờ khoá) = lỗi tạm; ok:false không retry = dữ liệu bị từ chối.
        /// </summary>
        public static BugSendResult FromReply(FnameSheetReply reply, string error)
        {
            if (reply == null)
                return NetworkError(error);
            if (reply.Ok)
                return Sent(reply.Message ?? "");
            return reply.Retry ? NetworkError(reply.Message ?? "") : Rejected(reply.Message ?? "");
        }
    }

    /// <summary>
    /// Gửi lại hàng chờ: gửi được hoặc máy chủ từ chối -> bỏ khỏi hàng chờ; lỗi mạng -> giữ lại cho lần sau.
    /// Không dừng ở một mục hỏng (một báo lỗi lỗi dữ liệu không được chặn các báo lỗi sau).
    /// </summary>
    public static class BugResender
    {
        public static int Run(Func<BugReport, BugSendResult> send)
        {
            int sent = 0;
            foreach (BugReport r in BugQueue.Pending())
            {
                BugSendResult res;
                try
                {
                    res = send(r) ?? BugSendResult.NetworkError("null");
                }
                catch (Exception ex)
                {
                    res = BugSendResult.NetworkError(ex.Message);
                }
                if (res.Ok)
                {
                    BugQueue.Remove(r.Id);
                    sent++;
                }
                else if (res.RejectedByServer)
                {
                    BugQueue.Remove(r.Id);
                    BugLog.Write("BUG", "may chu tu choi bao loi " + r.Id + ", bo khoi hang cho: " + res.Message);
                }
            }
            return sent;
        }
    }
}
