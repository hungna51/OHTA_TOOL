using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>Trạng thái gửi báo lỗi cho khung BÁO LỖI trên tab Info.</summary>
    public sealed class BugSendStatus
    {
        public bool Busy { get; set; }
        /// <summary>0–100 khi đang tải lên; null = không đo được (đang chờ máy chủ) — thanh chạy qua lại.</summary>
        public double? Percent { get; set; }
        public string Text { get; set; } = "";
        public bool Error { get; set; }
    }

    /// <summary>
    /// Gửi báo lỗi ở nền để người dùng làm việc tiếp (ADR 0007). Báo lỗi vào hàng chờ TRƯỚC khi gửi — đóng CAD giữa chừng
    /// thì lần mở bảng OHTA sau gửi lại (máy chủ bỏ qua ID đã có). Một luồng gửi lần lượt; trạng thái phát qua Changed
    /// trên luồng CAD.
    /// </summary>
    public static class BugSender
    {
        public static event Action<BugSendStatus> Changed;
        public static BugSendStatus Status { get; private set; } = new BugSendStatus();

        private static Dispatcher _cad;
        private static int _running;
        private static volatile bool _dirty;
        private static CancellationTokenSource _cts;
        private static string _currentId;
        private static int _cancelled;
        private static DateTime _lastKick = DateTime.MinValue;

        /// <summary>Gọi 1 lần trên luồng CAD (Initialize).</summary>
        public static void Init() => _cad = Dispatcher.CurrentDispatcher;

        /// <summary>Form báo lỗi gọi khi đã nén xong: vào hàng chờ rồi gửi ở nền.</summary>
        public static void Send(BugReport r)
        {
            BugQueue.Enqueue(r);
            BugLog.Write("BUG", "dua vao hang cho gui " + r.Id);
            CommandLine("\n[OHTA TOOL] Dang gui bao loi cho admin... (xem tien trinh o tab Info)\n");
            _dirty = true;
            Start();
        }

        /// <summary>Mở bảng OHTA: gửi lại hàng chờ còn sót, tối đa 10 phút/lần (bảng mở rất thường xuyên).</summary>
        public static void Kick()
        {
            if (_running != 0 || DateTime.Now - _lastKick < TimeSpan.FromMinutes(10) || !BugQueue.HasPending())
                return;
            _lastKick = DateTime.Now;
            Start();
        }

        /// <summary>Huỷ = bỏ hẳn báo lỗi đang gửi (đã hỏi lại ở tab Info).</summary>
        public static void CancelCurrent()
        {
            string id = _currentId;
            if (id == null)
                return;
            BugQueue.Remove(id);
            BugLog.Write("BUG", "nguoi dung huy bao loi " + id);
            Interlocked.Increment(ref _cancelled);
            try { _cts?.Cancel(); } catch { }
        }

        private static void Start()
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return;
            Task.Run(Loop);
        }

        private static void Loop()
        {
            int sent = 0;
            BugSendResult lastFail = null;
            _cancelled = 0;
            try
            {
                do
                {
                    _dirty = false;
                    int total = BugQueue.Pending().Count, i = 0;
                    sent += BugResender.Run(r =>
                    {
                        BugSendResult res = SendOne(r, ++i, total);
                        if (!res.Ok)
                            lastFail = res;
                        return res;
                    });
                } while (_dirty);

                if (BugQueue.HasPending())
                {
                    Publish(false, null, "Chưa gửi được (" + lastFail?.Message + ") — sẽ tự gửi lại lần mở bảng OHTA sau.", true);
                    CommandLine("\n[OHTA TOOL] Chua gui duoc bao loi, se tu gui lai sau: " + lastFail?.Message + "\n");
                }
                else if (lastFail != null && lastFail.RejectedByServer)
                {
                    Publish(false, null, "Admin chưa nhận được báo lỗi: " + lastFail.Message, true);
                    CommandLine("\n[OHTA TOOL] Bao loi bi tu choi: " + lastFail.Message + "\n");
                }
                else if (sent > 0)
                {
                    Publish(false, null, "✓ Đã gửi " + sent + " báo lỗi cho admin lúc " + DateTime.Now.ToString("HH:mm") + ".", false);
                    CommandLine("\n[OHTA TOOL] Da gui " + sent + " bao loi cho admin.\n");
                }
                else if (_cancelled > 0)
                    Publish(false, null, "Đã huỷ báo lỗi.", false);
                else
                    Publish(false, null, "", false);
            }
            catch (Exception ex)
            {
                BugLog.Write("BUG", ex);
                Publish(false, null, "Lỗi khi gửi: " + ex.Message, true);
            }
            finally
            {
                _currentId = null;
                Interlocked.Exchange(ref _running, 0);
                if (_dirty) // Send() gọi đúng lúc vòng gửi vừa kết thúc
                    Start();
            }
        }

        private static BugSendResult SendOne(BugReport r, int i, int total)
        {
            _currentId = r.Id;
            _cts = new CancellationTokenSource();
            string prefix = total > 1 ? "Báo lỗi " + i + "/" + total + ": " : "";
            Publish(true, 0, prefix + "đang chuẩn bị…", false);
            int timeout = r.DinhKemZip != null ? 300 : r.AnhJpeg != null ? 60 : 30;
            int lastPct = -1;
            FnameSheetReply reply = SheetWebApp.Post(BugReportService.FnameDir, "submit_bug", r.ToFields(), timeout, out _, out string error,
                (done, all) =>
                {
                    int pct = all == 0 ? 100 : (int)(done * 100 / all);
                    if (pct == lastPct)
                        return; // tối đa ~100 lần cập nhật giao diện
                    lastPct = pct;
                    if (done >= all)
                        Publish(true, null, prefix + "đã tải lên, máy chủ đang lưu…", false);
                    else
                        Publish(true, pct, prefix + "đang tải lên " + Mb(done) + " / " + Mb(all) + " MB (" + pct + "%)", false);
                },
                _cts.Token);
            if (_cts.IsCancellationRequested)
                return BugSendResult.NetworkError("đã huỷ"); // file hàng chờ đã xoá ở CancelCurrent
            return BugSendResult.FromReply(reply, error);
        }

        private static string Mb(long bytes) => (bytes / 1024.0 / 1024.0).ToString("0.0", CultureInfo.InvariantCulture);

        private static void Publish(bool busy, double? percent, string text, bool error)
        {
            var s = new BugSendStatus { Busy = busy, Percent = percent, Text = text, Error = error };
            Status = s;
            OnCad(() => Changed?.Invoke(s));
        }

        private static void CommandLine(string text) => OnCad(() => CadCompat.WriteMessage(text));

        private static void OnCad(Action a)
        {
            try
            {
                if (_cad == null || _cad.CheckAccess())
                    a();
                else
                    _cad.BeginInvoke(a);
            }
            catch (Exception ex)
            {
                BugLog.Write("BUG", ex);
            }
        }
    }
}
