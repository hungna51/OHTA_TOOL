#if ZWCAD
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;
#elif AUTOCAD
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Services;
using OhtaTool.Plugin.Views;

[assembly: CommandClass(typeof(OhtaTool.Plugin.PrintCommands))]

namespace OhtaTool.Plugin
{
    /// <summary>Yêu cầu in do tab PRINT dựng, lệnh OIIN thực hiện.</summary>
    public sealed class PrintRequest
    {
        /// <summary>"frames" (tất cả khung) | "pick" (chọn khung) | "window" (vùng quét) | "extents".</summary>
        public string Mode;
        public string Layer;
        /// <summary>Thông số đã giải quyết (mọi trường khác null).</summary>
        public PrintParams Params;
    }

    public static class PrintRunner
    {
        public static PrintRequest Pending;
        /// <summary>Báo kết quả cho tab (dòng trạng thái).</summary>
        public static event Action<string> Finished;

        internal static void Report(string message)
        {
            CadCompat.WriteMessage("\n[OI] " + message + "\n");
            try { Finished?.Invoke(message); } catch (System.Exception) { }
        }
    }

    public sealed class PrintCommands
    {
        /// <summary>Lệnh nội bộ: tab PRINT ghi PrintRunner.Pending rồi gửi "_OIIN ".</summary>
        [CommandMethod("OIIN", CommandFlags.Modal)]
        public static void CmdPrint()
        {
            PrintRequest req = PrintRunner.Pending;
            PrintRunner.Pending = null;
            CadDocument doc = CadCompat.ActiveDocument;
            if (req == null || req.Params == null || doc == null)
            {
                if (req != null)
                    PrintRunner.Report("Không có bản vẽ để in.");
                return;
            }

            object oldBgPlot = null;
            string tempDir = null;
            try
            {
                try
                {
                    oldBgPlot = CadApp.GetSystemVariable("BACKGROUNDPLOT");
                    CadApp.SetSystemVariable("BACKGROUNDPLOT", (short)0);
                }
                catch (System.Exception) { oldBgPlot = null; }

                Run(doc, req, out tempDir);
            }
            catch (System.Exception ex)
            {
                PrintRunner.Report("Lỗi khi in: " + ex.Message);
            }
            finally
            {
                if (oldBgPlot != null)
                    try { CadApp.SetSystemVariable("BACKGROUNDPLOT", oldBgPlot); } catch (System.Exception) { }
                if (tempDir != null)
                    try { Directory.Delete(tempDir, true); } catch (System.Exception) { }
            }
        }

        private static void Run(CadDocument doc, PrintRequest req, out string tempDir)
        {
            tempDir = null;
            PrintParams p = req.Params;

            // A3: bản vẽ chưa lưu thì không có thư mục để đặt PDF
            string blocked = PrintCore.PreflightError(
                Convert.ToInt32(CadApp.GetSystemVariable("TILEMODE")),
                Convert.ToInt32(CadApp.GetSystemVariable("DWGTITLED")));
            if (blocked != null)
            {
                PrintRunner.Report(blocked);
                return;
            }

            List<PlotPage> areas = CollectAreas(doc, req);
            if (areas == null)
            {
                PrintRunner.Report("Đã huỷ in.");
                return; // người dùng huỷ
            }
            if (areas.Count == 0)
            {
                PrintRunner.Report(req.Mode == "frames" || req.Mode == "pick"
                    ? $"Không có khung in trên layer {req.Layer}."
                    : "Không có vùng in.");
                return;
            }

            string dir = (string)CadApp.GetSystemVariable("DWGPREFIX");
            string basePath = Path.Combine(dir, PrintCore.PdfBaseName((string)CadApp.GetSystemVariable("DWGNAME")) + ".pdf");
            // Một trang thì luôn là <tên>.pdf, không thêm -1
            bool merge = (p.Merge ?? true) || areas.Count == 1;
            List<string> targets = ResolveTargets(basePath, areas.Count, merge);
            if (targets == null)
            {
                PrintRunner.Report("Đã huỷ in.");
                return;
            }

            CadPrintService.DisablePlotOnWarningLayers(doc);

            tempDir = Path.Combine(Path.GetTempPath(), "OHTA_PRINT", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var watch = Stopwatch.StartNew();

            if (merge)
            {
                // Nhanh: mọi trang trong MỘT lần PlotEngine ra thẳng PDF nhiều trang
                string all = Path.Combine(tempDir, "all.pdf");
                bool done = false;
                if (areas.Count > 1)
                {
                    try
                    {
                        CadPrintService.PlotPagesToPdf(doc, p, areas, all);
                        done = File.Exists(all) && new FileInfo(all).Length > 0;
                    }
                    catch (System.Exception ex)
                    {
                        CadCompat.WriteMessage("\n[OI] Máy in không in được nhiều trang một lần (" + ex.Message + ") → in từng trang rồi gộp.\n");
                    }
                }
                if (!done)
                {
                    List<string> pages = PlotEachPage(doc, p, areas, tempDir);
                    if (pages.Count == 1)
                        File.Copy(pages[0], all, true);
                    else
                        PdfMerger.Merge(pages, all);
                }
                File.Copy(all, targets[0], true);
            }
            else
            {
                List<string> pages = PlotEachPage(doc, p, areas, tempDir);
                for (int i = 0; i < pages.Count; i++)
                    File.Copy(pages[i], targets[i], true);
            }
            watch.Stop();

            string shown = merge ? targets[0] : Path.GetDirectoryName(targets[0]);
            PrintRunner.Report($"Đã in {areas.Count} trang trong {watch.Elapsed.TotalSeconds:0.0} giây → {shown}");

            if (p.OpenAfter ?? true)
            {
                try { Process.Start(new ProcessStartInfo(shown) { UseShellExecute = true }); }
                catch (System.Exception ex) { CadCompat.WriteMessage("\n[OI] Không mở được: " + ex.Message + "\n"); }
            }
        }

        /// <summary>In từng trang thành file riêng (khi không gộp, hoặc máy in không in được nhiều trang một lần).</summary>
        private static List<string> PlotEachPage(CadDocument doc, PrintParams p, List<PlotPage> areas, string tempDir)
        {
            var files = new List<string>();
            for (int i = 0; i < areas.Count; i++)
            {
                string tmp = Path.Combine(tempDir, (i + 1).ToString("D3") + ".pdf");
                CadPrintService.PlotPagesToPdf(doc, p, new[] { areas[i] }, tmp);
                if (!File.Exists(tmp))
                    throw new IOException($"Không tạo được PDF cho trang {i + 1}.");
                files.Add(tmp);
            }
            return files;
        }

        private static List<PlotPage> CollectAreas(CadDocument doc, PrintRequest req)
        {
            var areas = new List<PlotPage>();
            switch (req.Mode)
            {
                case "frames":
                case "pick":
                    List<FrameRect> frames = CadPrintService.CollectFrames(doc, req.Layer, req.Mode == "pick");
                    if (frames == null)
                        return null;
                    foreach (FrameRect f in PrintCore.SortFrames(frames))
                        areas.Add(new PlotPage
                        {
                            LowerLeft = new Point2d(f.Xmin, f.Ymin),
                            UpperRight = new Point2d(f.Xmax, f.Ymax),
                            Portrait = PrintCore.IsPortrait(f.Width, f.Height)
                        });
                    return areas;

                case "window":
                    Editor ed = doc.Editor;
                    PromptPointResult p1 = ed.GetPoint("\nGóc thứ nhất vùng in: ");
                    if (p1.Status != PromptStatus.OK)
                        return null;
                    PromptPointResult p2 = ed.GetCorner("\nGóc đối diện: ", p1.Value);
                    if (p2.Status != PromptStatus.OK)
                        return null;
                    Matrix3d ucs = ed.CurrentUserCoordinateSystem;
                    Point3d a = p1.Value.TransformBy(ucs), b = p2.Value.TransformBy(ucs);
                    double w = Math.Abs(b.X - a.X), h = Math.Abs(b.Y - a.Y);
                    if (w < 1e-6 || h < 1e-6)
                        return areas;
                    areas.Add(new PlotPage
                    {
                        LowerLeft = new Point2d(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)),
                        UpperRight = new Point2d(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)),
                        Portrait = PrintCore.IsPortrait(w, h)
                    });
                    return areas;

                case "extents":
                    areas.Add(new PlotPage { Extents = true });
                    return areas;
            }
            return areas;
        }

        /// <summary>
        /// Tên file đích. Có file trùng -> hỏi Ghi đè / Lưu thành (n) / Huỷ (null). Ghi đè mà file đang
        /// bị khoá -> chuyển sang (n) và báo.
        /// </summary>
        private static List<string> ResolveTargets(string basePath, int count, bool merge)
        {
            List<string> targets = merge ? new List<string> { basePath } : PrintCore.PerFrameNames(basePath, count);
            string firstExisting = targets.FirstOrDefault(File.Exists);
            if (firstExisting == null)
                return targets;

            OverwriteChoice choice = PrintDialogs.AskOverwrite(firstExisting, targets.Count);
            if (choice == OverwriteChoice.Cancel)
                return null;

            if (choice == OverwriteChoice.Overwrite)
            {
                string locked = targets.FirstOrDefault(t => File.Exists(t) && IsLocked(t));
                if (locked == null)
                    return targets;
                PrintRunner.Report($"File {Path.GetFileName(locked)} đang mở, không ghi đè được → lưu thành tên mới.");
            }

            // Lưu thành (n): một số n chung cho cả bộ để các file đi cùng nhau
            string numberedBase = PrintCore.NextFreeName(basePath, candidate =>
                merge ? File.Exists(candidate) : PrintCore.PerFrameNames(candidate, count).Any(File.Exists));
            return merge ? new List<string> { numberedBase } : PrintCore.PerFrameNames(numberedBase, count);
        }

        private static bool IsLocked(string path)
        {
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }
    }
}
