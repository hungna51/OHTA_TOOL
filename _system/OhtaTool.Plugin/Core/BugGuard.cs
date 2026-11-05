using System;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Bắt lỗi ở đầu lệnh và hỏi "Gửi báo lỗi?" (ADR 0007). Phần CAD/UI (chụp ảnh, hộp hỏi, form) gắn vào qua
    /// Capture / AskUser / ShowForm khi OHTATOOL.dll nạp (BugCommands.WireGuard) — lõi không phụ thuộc CAD nên test được.
    /// HỢP ĐỒNG phản chiếu với GROTOOL.dll: kiểu "OhtaTool.Plugin.Core.BugGuard", hàm tĩnh
    /// ReportException(string, Exception) — đổi thì sửa cả 03_Guro/plugin/GroTool.Plugin/OhtaBugBridge.cs.
    /// </summary>
    public static class BugGuard
    {
        public static Func<byte[]> Capture { get; set; }
        /// <summary>(nguồn, tóm tắt lỗi) -> true nếu người dùng muốn gửi báo lỗi.</summary>
        public static Func<string, string, bool> AskUser { get; set; }
        /// <summary>(tab/lệnh, tóm tắt lỗi, chi tiết lỗi, ảnh).</summary>
        public static Action<string, string, string, byte[]> ShowForm { get; set; }
        /// <summary>In 1 dòng ra dòng lệnh CAD — luôn gọi, kể cả khi không hỏi (lỗi trùng / chưa gắn UI).</summary>
        public static Action<string> Notify { get; set; }

        /// <summary>Chạy thân lệnh; exception thoát ra được ghi nhật ký + hỏi gửi báo lỗi, không ném tiếp.</summary>
        public static void Run(string lenh, Action body)
        {
            BugLog.Write(lenh, "bat dau");
            try
            {
                body();
            }
            catch (Exception ex)
            {
                if (IsUserCancel(ex))
                {
                    BugLog.Write(lenh, "nguoi dung huy");
                    return;
                }
                ReportException(lenh, ex);
            }
        }

        public static void ReportException(string nguon, Exception ex)
        {
            try
            {
                BugLog.Write(nguon, ex);
                if (ex == null || IsUserCancel(ex))
                    return;
                try { Notify?.Invoke("\n[OHTA TOOL] Loi lenh " + nguon + ": " + BugReport.ShortError(ex.Message) + "\n"); } catch { }
                if (!BugDedup.ShouldAsk(nguon, ex))
                    return;
                Ask(nguon, BugReport.ShortError(ex.Message), ex.ToString());
            }
            catch
            {
                // luồng báo lỗi không bao giờ được làm sập CAD
            }
        }

        /// <summary>Lỗi thật từ *error* của lisp (đã lọc Esc/Cancel bên lisp).</summary>
        public static void ReportLispError(string nguon, string loi)
        {
            try
            {
                BugLog.Write(nguon, "LOI LISP: " + loi);
                if (!BugDedup.ShouldAsk(nguon, loi))
                    return;
                Ask(nguon, BugReport.ShortError(loi), loi);
            }
            catch
            {
            }
        }

        private static void Ask(string nguon, string tomTat, string chiTiet)
        {
            byte[] anh = Capture?.Invoke(); // chụp ngay lúc lỗi, trước khi hộp hỏi che màn hình
            if (AskUser == null)
                return;
            bool gui;
            BugDedup.InReport = true; // lỗi khác nổ ra trong lúc hộp hỏi đang mở thì không hỏi chồng
            try
            {
                gui = AskUser(nguon, tomTat);
            }
            finally
            {
                BugDedup.InReport = false;
            }
            if (gui)
                ShowForm?.Invoke(nguon, tomTat, chiTiet, anh);
        }

        /// <summary>Esc / huỷ của CAD: Runtime.Exception có ErrorStatus = UserBreak (đọc bằng phản chiếu, không cần DLL CAD).</summary>
        public static bool IsUserCancel(Exception ex)
        {
            try
            {
                object status = ex?.GetType().GetProperty("ErrorStatus")?.GetValue(ex, null);
                return status != null && string.Equals(status.ToString(), "UserBreak", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
