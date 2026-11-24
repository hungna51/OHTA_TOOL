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
using System.Windows.Threading;
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
        /// <summary>Cài đặt ⚙: trùng tên thì tự lưu "(n)", không hỏi ghi đè.</summary>
        public bool SkipOverwritePrompt;
        /// <summary>Cài đặt ⚙: tự thêm ô 材料寸法チェック khi in Theo khung.</summary>
        public bool AddCheckBox;
        public double CheckBoxWidthCad;
        public double CheckBoxHeightCad;
        /// <summary>Tab CSV: danh sách trang đã xếp sẵn (thay cho gom khung) và file PDF cố định, ghi đè không hỏi.</summary>
        public List<PlotPage> Pages;
        public string TargetPath;
    }

    public static class PrintRunner
    {
        public static PrintRequest Pending;
        /// <summary>Báo kết quả cho tab (dòng trạng thái).</summary>
        public static event Action<string> Finished;

        /// <summary>Tiến độ: (số trang đã xong, tổng số trang, chữ hiển thị). Gọi trên luồng chính của CAD.</summary>
        public static event Action<int, int, string> Progress;

        /// <summary>Đang chạy lệnh OIIN (palette khoá ô, không tự đọc lại bản vẽ).</summary>
        public static bool IsBusy { get; internal set; }

        /// <summary>Nút Huỷ trên palette đặt cờ này; lệnh in dừng ở chỗ giữa hai trang.</summary>
        public static volatile bool CancelRequested;

        internal static void Report(string message)
        {
            CadCompat.WriteMessage("\n[OI] " + message + "\n");
            try { Finished?.Invoke(message); } catch (System.Exception) { }
        }

        internal static void ReportProgress(int done, int total)
        {
            try { Progress?.Invoke(done, total, PrintCore.ProgressText(done, total)); } catch (System.Exception) { }
            PumpUi();
        }

        /// <summary>
        /// Cho palette vẽ lại và nhận cú bấm (nút Huỷ) giữa hai trang: luồng chính đang bận in nên
        /// WPF không tự vẽ. Đẩy một khung dispatcher tới mức Background (sau Render và Input).
        /// </summary>
        private static void PumpUi()
        {
            try
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(f =>
                    {
                        ((DispatcherFrame)f).Continue = false;
                        return null;
                    }), frame);
                Dispatcher.PushFrame(frame);
            }
            catch (System.Exception)
            {
            }
        }
    }

    public sealed class PrintCommands
    {
        /// <summary>Lệnh nội bộ: tab PRINT ghi PrintRunner.Pending rồi gửi "_OIIN ".</summary>
        [CommandMethod("OIIN", CommandFlags.Modal)]
        public static void CmdPrint()
        {
            BugGuard.Run("OIIN", () =>
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
                object oldPlQuiet = null;
                string tempDir = null;
                try
                {
                    try
                    {
                        oldBgPlot = CadApp.GetSystemVariable("BACKGROUNDPLOT");
                        CadApp.SetSystemVariable("BACKGROUNDPLOT", (short)0);
                    }
                    catch (System.Exception) { oldBgPlot = null; }
                    try
                    {
                        oldPlQuiet = CadApp.GetSystemVariable("PLQUIET");
                        CadApp.SetSystemVariable("PLQUIET", (short)1);
                    }
                    catch (System.Exception) { oldPlQuiet = null; }

                    PrintRunner.IsBusy = true;
                    Run(doc, req, out tempDir);
                }
                catch (System.Exception ex)
                {
                    OhtaTool.Plugin.Core.BugLog.Write("OHTA/PrintCommands", ex);
                    PrintRunner.Report("Lỗi khi in: " + ex.Message);
                }
                finally
                {
                    PrintRunner.IsBusy = false;
                    PrintRunner.CancelRequested = false;
                    if (oldBgPlot != null)
                        try { CadApp.SetSystemVariable("BACKGROUNDPLOT", oldBgPlot); } catch (System.Exception) { }
                    if (oldPlQuiet != null)
                        try { CadApp.SetSystemVariable("PLQUIET", oldPlQuiet); } catch (System.Exception) { }
                    if (tempDir != null)
                        try { Directory.Delete(tempDir, true); } catch (System.Exception) { }
                }
            });
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

            List<PlotPage> areas = req.Pages ?? CollectAreas(doc, req);
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
            bool merge = (p.Merge ?? true) || areas.Count == 1 || req.TargetPath != null;
            if (req.TargetPath != null && File.Exists(req.TargetPath) && IsLocked(req.TargetPath))
            {
                PrintRunner.Report(Path.GetFileName(req.TargetPath) + " đang mở, chưa in được. Đóng file rồi quét lại.");
                return;
            }
            List<string> targets = req.TargetPath != null ? new List<string> { req.TargetPath } : ResolveTargets(basePath, areas.Count, merge, req.SkipOverwritePrompt);
            if (targets == null)
            {
                PrintRunner.Report("Đã huỷ in.");
                return;
            }

            CadPrintService.DisablePlotOnWarningLayers(doc);

            tempDir = Path.Combine(Path.GetTempPath(), "OHTA_PRINT", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var watch = Stopwatch.StartNew();

            // Mỗi trang một file PDF tạm (driver máy in nạp một lần), gộp thì nối bằng PdfSharp
            var pages = new List<string>();
            for (int i = 0; i < areas.Count; i++)
                pages.Add(Path.Combine(tempDir, (i + 1).ToString("D3") + ".pdf"));

            int done = 0;
            PrintRunner.CancelRequested = false;
            PrintRunner.ReportProgress(0, areas.Count);
            try
            {
                CadPrintService.PlotPagesToPdfFiles(doc, p, areas, pages, n =>
                {
                    done = n;
                    CadCompat.WriteMessage($"\n[OI] Trang {n}/{areas.Count}");
                    PrintRunner.ReportProgress(n, areas.Count);
                    // Huỷ sau trang cuối thì vẫn lưu (đã in xong hết)
                    return !PrintRunner.CancelRequested || n == areas.Count;
                });
            }
            catch (OperationCanceledException)
            {
                PrintRunner.Report(PrintCore.CancelledText(done, areas.Count));
                return;
            }

            if (req.AddCheckBox && (req.Mode == "frames" || req.Mode == "pick"))
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    if (areas[i].FrameWidthCad > 0 && areas[i].FrameHeightCad > 0)
                    {
                        string stamped = Path.Combine(tempDir, (i + 1).ToString("D3") + "_box.pdf");
                        PdfMerger.StampCheckBox(pages[i], stamped,
                            areas[i].FrameWidthCad, areas[i].FrameHeightCad,
                            req.CheckBoxWidthCad, req.CheckBoxHeightCad);
                        pages[i] = stamped;
                    }
                }
            }

            if (merge)
            {
                if (pages.Count == 1)
                    File.Copy(pages[0], targets[0], true);
                else
                    PdfMerger.Merge(pages, targets[0]);
            }
            else
            {
                for (int i = 0; i < pages.Count; i++)
                    File.Copy(pages[i], targets[i], true);
            }
            watch.Stop();

            string shown = merge ? targets[0] : Path.GetDirectoryName(targets[0]);
            PrintRunner.Report($"Đã in {areas.Count} trang trong {watch.Elapsed.TotalSeconds:0.0} giây → {shown}");

            if (p.OpenAfter ?? true)
            {
                try { Process.Start(new ProcessStartInfo(shown) { UseShellExecute = true }); }
                catch (System.Exception ex) { OhtaTool.Plugin.Core.BugLog.Write("OHTA/PrintCommands", ex); CadCompat.WriteMessage("\n[OI] Không mở được: " + ex.Message + "\n"); }
            }
        }

        /// <summary>
        /// Tab CSV (CSVTOOL.dll) gọi sau khi quét: in các khung tấm theo thứ tự đã xếp ra một file PDF.
        /// Dữ liệu qua AppDomain "OHTA_CSV_PDF": dòng 1 tên profile, dòng 2 file PDF, mỗi dòng sau "xmin,ymin,xmax,ymax".
        /// </summary>
        [CommandMethod("OHTA_CSV_PDF", CommandFlags.Modal)]
        public static void CmdCsvPdf()
        {
            BugGuard.Run("OHTA_CSV_PDF", () =>
            {
                string[] dong = (AppDomain.CurrentDomain.GetData("OHTA_CSV_PDF") as string ?? "").Split('\n');
                AppDomain.CurrentDomain.SetData("OHTA_CSV_PDF", null);
                CadDocument doc = CadCompat.ActiveDocument;
                if (dong.Length < 3 || doc == null)
                    return;

                PrintSettingsData s = PrintSettingsStore.Load();
                PrintParams drawing;
                using (CadCompat.LockActiveDocument())
                    drawing = CadPrintService.ReadDrawingParams(doc);
                PrintProfile prof = s.Profiles.FirstOrDefault(x => string.Equals(x.Name, dong[0], StringComparison.OrdinalIgnoreCase));
                ResolvedPrint r = PrintSettingsResolver.Resolve(prof, drawing, s.Defaults, new CadPlotCatalog());
                if (string.IsNullOrEmpty(r.Values.Device) || string.IsNullOrEmpty(r.Values.Media))
                {
                    PrintRunner.Report("Profile " + dong[0] + " chưa có máy in / khổ giấy. Chọn lại ở tab PRINT.");
                    return;
                }

                var pages = new List<PlotPage>();
                for (int i = 2; i < dong.Length; i++)
                {
                    double[] v = dong[i].Split(',').Select(x => double.TryParse(x, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : double.NaN).ToArray();
                    if (v.Length != 4 || v.Any(double.IsNaN))
                        continue;
                    double w = v[2] - v[0], h = v[3] - v[1];
                    pages.Add(new PlotPage
                    {
                        LowerLeft = new Point2d(v[0], v[1]),
                        UpperRight = new Point2d(v[2], v[3]),
                        Portrait = PrintCore.IsPortrait(w, h),
                        FrameWidthCad = w,
                        FrameHeightCad = h
                    });
                }
                if (pages.Count == 0)
                    return;

                PrintRunner.Pending = new PrintRequest
                {
                    Mode = "frames",
                    Pages = pages,
                    TargetPath = dong[1],
                    SkipOverwritePrompt = true,
                    AddCheckBox = s.AddCheckBox,
                    CheckBoxWidthCad = s.CheckBoxWidthCad,
                    CheckBoxHeightCad = s.CheckBoxHeightCad,
                    Params = new PrintParams
                    {
                        Device = r.Values.Device,
                        Media = r.Values.Media,
                        StyleSheet = r.Values.StyleSheet ?? string.Empty,
                        Lineweights = r.Values.Lineweights ?? true,
                        Merge = true,
                        OpenAfter = r.Values.OpenAfter ?? true
                    }
                };
                CmdPrint();
            });
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
                            Portrait = PrintCore.IsPortrait(f.Width, f.Height),
                            FrameWidthCad = f.Width,
                            FrameHeightCad = f.Height
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

        /// <summary>Tên file đích (logic ở PrintCore.PlanTargets). null = người dùng huỷ.</summary>
        private static List<string> ResolveTargets(string basePath, int count, bool merge, bool skipPrompt)
        {
            TargetPlan plan = PrintCore.PlanTargets(basePath, count, merge, skipPrompt,
                File.Exists, IsLocked, PrintDialogs.AskOverwrite);
            if (plan == null)
                return null;
            if (plan.Notice != null)
                PrintRunner.Report(plan.Notice);
            return plan.Targets;
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
