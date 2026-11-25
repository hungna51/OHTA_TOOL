using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiTool.Core;

namespace AiTool.Services
{
    /// <summary>
    /// Chạy OhtaTeams.exe --thu-nhanh-* ẩn, chuyển từng dòng in ra về cho tab (DongExe đọc). Huỷ = giết tiến trình.
    /// CAD không chờ AI trên luồng của mình; mọi việc đọc / vẽ nằm trong OhtaTeams (một nguồn code với cửa sổ Thử nhanh).
    /// </summary>
    public static class ChayExe
    {
        /// <returns>Mã thoát của exe.</returns>
        public static Task<int> Chay(string exe, string thamSo, Action<string> moiDong, CancellationToken huy) => Task.Run(() =>
        {
            if (!File.Exists(exe))
                throw new FileNotFoundException("Không thấy " + exe + " — chạy build_ohta.ps1 hoặc cập nhật OHTA TOOL.");
            var psi = new ProcessStartInfo(exe, thamSo)
            {
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(exe),
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
            };
            using (var p = Process.Start(psi))
            using (huy.Register(() => { try { p.Kill(); } catch (InvalidOperationException) { } }))
            {
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) moiDong(e.Data); };
                p.BeginErrorReadLine();
                string dong;
                while ((dong = p.StandardOutput.ReadLine()) != null) moiDong(dong);
                p.WaitForExit();
                huy.ThrowIfCancellationRequested();
                return p.ExitCode;
            }
        });

        /// <summary>Mở cửa sổ (không đợi): exe có tham số → cửa sổ Thử nhanh; không tham số → OhtaTeams đầy đủ.</summary>
        public static void Mo(string exe, string thamSo)
        {
            if (!File.Exists(exe)) throw new FileNotFoundException("Không thấy " + exe);
            Process.Start(new ProcessStartInfo(exe, thamSo ?? "") { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe) });
        }
    }
}
