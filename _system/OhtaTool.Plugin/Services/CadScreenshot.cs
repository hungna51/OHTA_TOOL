using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Chụp TOÀN BỘ cửa sổ CAD (bản vẽ + bảng OHTA + dòng lệnh) cho báo lỗi — chụp trước khi form báo lỗi hiện.
    /// Dùng handle cửa sổ chính của tiến trình nên chạy giống nhau trên ZWCAD và AutoCAD. Lỗi gì cũng trả null.
    /// </summary>
    public static class CadScreenshot
    {
        private const int MaxSide = 1600;
        private const long JpegQuality = 70;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        public static byte[] CaptureMainWindowJpeg()
        {
            try
            {
                IntPtr hwnd = Process.GetCurrentProcess().MainWindowHandle;
                if (hwnd == IntPtr.Zero || IsIconic(hwnd) || !GetWindowRect(hwnd, out Rect r))
                    return null;
                int w = r.Right - r.Left, h = r.Bottom - r.Top;
                if (w <= 0 || h <= 0)
                    return null;

                using (var full = new Bitmap(w, h, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(full))
                        g.CopyFromScreen(r.Left, r.Top, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);

                    double scale = Math.Min(1.0, (double)MaxSide / Math.Max(w, h));
                    int sw = Math.Max(1, (int)(w * scale)), sh = Math.Max(1, (int)(h * scale));
                    using (var small = new Bitmap(sw, sh, PixelFormat.Format24bppRgb))
                    {
                        using (Graphics g = Graphics.FromImage(small))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(full, 0, 0, sw, sh);
                        }
                        ImageCodecInfo jpeg = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                        using (var ep = new EncoderParameters(1))
                        using (var ms = new MemoryStream())
                        {
                            ep.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
                            small.Save(ms, jpeg, ep);
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
