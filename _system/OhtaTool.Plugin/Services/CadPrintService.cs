#if ZWCAD
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.PlottingServices;
using PlotType = ZwSoft.ZwCAD.DatabaseServices.PlotType;
#elif AUTOCAD
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using PlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>Máy in / khổ giấy / bảng ctb đang có trên máy (PlotSettingsValidator).</summary>
    public sealed class CadPlotCatalog : IPlotCatalog
    {
        private readonly Dictionary<string, IList<string>> _mediaCache =
            new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

        public IList<string> Devices { get; }
        public IList<string> StyleSheets { get; }

        public CadPlotCatalog()
        {
            Devices = new List<string>();
            StyleSheets = new List<string>();
            try
            {
                PlotSettingsValidator psv = PlotSettingsValidator.Current;
                using (var ps = new PlotSettings(true))
                {
                    psv.RefreshLists(ps);
                    foreach (string d in psv.GetPlotDeviceList())
                        if (!string.IsNullOrWhiteSpace(d) && !string.Equals(d, "None", StringComparison.OrdinalIgnoreCase))
                            Devices.Add(d);
                }
                foreach (string s in psv.GetPlotStyleSheetList())
                    if (!string.IsNullOrWhiteSpace(s))
                        StyleSheets.Add(s);
            }
            catch (Exception ex)
            {
                CadCompat.WriteMessage("\n[OI] Không đọc được danh sách máy in: " + ex.Message + "\n");
            }
        }

        public IList<string> MediaFor(string device)
        {
            if (string.IsNullOrWhiteSpace(device))
                return new List<string>();
            if (_mediaCache.TryGetValue(device, out var cached))
                return cached;

            var list = new List<string>();
            try
            {
                PlotSettingsValidator psv = PlotSettingsValidator.Current;
                using (var ps = new PlotSettings(true))
                {
                    psv.SetPlotConfigurationName(ps, device, null);
                    psv.RefreshLists(ps);
                    foreach (string m in psv.GetCanonicalMediaNameList(ps))
                        if (!string.IsNullOrWhiteSpace(m))
                            list.Add(m);
                }
            }
            catch (Exception)
            {
                // Máy in lỗi / không mở được -> không có khổ nào
            }
            _mediaCache[device] = list;
            return list;
        }
    }

    /// <summary>Các thao tác CAD của tab PRINT: đọc page setup, gom khung, in PlotEngine ra PDF.</summary>
    public static class CadPrintService
    {
        /// <summary>Page setup của layout Model. Thông số trống / máy in "None" -> null.</summary>
        public static PrintParams ReadDrawingParams(CadDocument doc)
        {
            var p = new PrintParams();
            if (doc == null)
                return p;
            try
            {
                Database db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                    var lo = (Layout)tr.GetObject(ms.LayoutId, OpenMode.ForRead);
                    string device = lo.PlotConfigurationName;
                    if (!string.IsNullOrWhiteSpace(device) && !string.Equals(device, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        p.Device = device;
                        p.Media = string.IsNullOrWhiteSpace(lo.CanonicalMediaName) ? null : lo.CanonicalMediaName;
                    }
                    p.StyleSheet = string.IsNullOrWhiteSpace(lo.CurrentStyleSheet) ? null : lo.CurrentStyleSheet;
                    p.Lineweights = lo.PrintLineweights;
                    tr.Commit();
                }
            }
            catch (Exception)
            {
            }
            return p;
        }

        public static IList<string> ListLayers(CadDocument doc)
        {
            var names = new List<string>();
            if (doc == null)
                return names;
            try
            {
                Database db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    foreach (ObjectId id in lt)
                        names.Add(((LayerTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name);
                    tr.Commit();
                }
            }
            catch (Exception)
            {
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>
        /// Khung in: LWPOLYLINE kín, 4 đỉnh, trên layer khung, trong Model. pickByUser = người dùng quét chọn,
        /// ngược lại lấy hết. Trả null nếu người dùng huỷ chọn.
        /// </summary>
        public static List<FrameRect> CollectFrames(CadDocument doc, string layer, bool pickByUser)
        {
            Editor ed = doc.Editor;
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.LayerName, EscapeWildcards(layer)),
                new TypedValue(90, 4),
                new TypedValue(410, "Model")
            });

            PromptSelectionResult res;
            if (pickByUser)
            {
                var opts = new PromptSelectionOptions { MessageForAdding = "\nChọn khung in: " };
                res = ed.GetSelection(opts, filter);
                if (res.Status == PromptStatus.Cancel)
                    return null;
            }
            else
            {
                res = ed.SelectAll(filter);
            }

            var frames = new List<FrameRect>();
            if (res.Status != PromptStatus.OK || res.Value == null)
                return frames;

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in res.Value.GetObjectIds())
                {
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Polyline pl) || !pl.Closed || pl.NumberOfVertices != 4)
                        continue;
                    var r = new FrameRect { Handle = pl.Handle.ToString(), Xmin = double.MaxValue, Ymin = double.MaxValue, Xmax = double.MinValue, Ymax = double.MinValue };
                    for (int i = 0; i < 4; i++)
                    {
                        Point3d pt = pl.GetPoint3dAt(i);
                        r.Xmin = Math.Min(r.Xmin, pt.X);
                        r.Ymin = Math.Min(r.Ymin, pt.Y);
                        r.Xmax = Math.Max(r.Xmax, pt.X);
                        r.Ymax = Math.Max(r.Ymax, pt.Y);
                    }
                    frames.Add(r);
                }
                tr.Commit();
            }
            return frames;
        }

        /// <summary>Tắt plottable các layer CANH_BAO_* và GRO_XEP_NHOM (giống GRO, không bật lại).</summary>
        public static void DisablePlotOnWarningLayers(CadDocument doc)
        {
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (PrintCore.IsWarningLayer(ltr.Name) && ltr.IsPlottable)
                    {
                        ltr.UpgradeOpen();
                        ltr.IsPlottable = false;
                    }
                }
                tr.Commit();
            }
        }

        /// <summary>In một cửa sổ (toạ độ WCS) của Model ra một file PDF một trang.</summary>
        public static void PlotWindowToPdf(CadDocument doc, PrintParams p, Point2d lowerLeftWcs, Point2d upperRightWcs,
            bool portrait, string pdfPath)
        {
            Matrix3d wcsToDcs = WcsToDcs(doc);
            Point3d a = new Point3d(lowerLeftWcs.X, lowerLeftWcs.Y, 0).TransformBy(wcsToDcs);
            Point3d b = new Point3d(upperRightWcs.X, upperRightWcs.Y, 0).TransformBy(wcsToDcs);
            var window = new Extents2d(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
            Plot(doc, p, portrait, pdfPath, (psv, ps) =>
            {
                psv.SetPlotWindowArea(ps, window);
                psv.SetPlotType(ps, PlotType.Window);
            });
        }

        /// <summary>In toàn bộ bản vẽ (Extents) ra một trang ngang.</summary>
        public static void PlotExtentsToPdf(CadDocument doc, PrintParams p, string pdfPath)
        {
            Plot(doc, p, false, pdfPath, (psv, ps) => psv.SetPlotType(ps, PlotType.Extents));
        }

        private static void Plot(CadDocument doc, PrintParams p, bool portrait, string pdfPath,
            Action<PlotSettingsValidator, PlotSettings> setArea)
        {
            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
                throw new InvalidOperationException("CAD đang in bản khác, hãy thử lại sau.");

            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                var lo = (Layout)tr.GetObject(ms.LayoutId, OpenMode.ForRead);

                var pi = new PlotInfo { Layout = ms.LayoutId };
                using (var ps = new PlotSettings(lo.ModelType))
                {
                    ps.CopyFrom(lo);
                    PlotSettingsValidator psv = PlotSettingsValidator.Current;
                    psv.SetPlotConfigurationName(ps, p.Device, p.Media);
                    psv.RefreshLists(ps);
                    setArea(psv, ps);
                    psv.SetUseStandardScale(ps, true);
                    psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
                    psv.SetPlotCentered(ps, true);
                    if (!string.IsNullOrWhiteSpace(p.StyleSheet))
                        psv.SetCurrentStyleSheet(ps, p.StyleSheet);
                    ps.ShowPlotStyles = true;
                    ps.PrintLineweights = p.Lineweights ?? true;

                    // PlotPaperSize là kích thước khổ chưa xoay: khổ ngang mà cần dọc (hoặc ngược lại) thì xoay 90°
                    Point2d paper = ps.PlotPaperSize;
                    bool paperIsLandscape = paper.X > paper.Y;
                    psv.SetPlotRotation(ps, paperIsLandscape == portrait ? PlotRotation.Degrees090 : PlotRotation.Degrees000);

                    pi.OverrideSettings = ps;
                    var piv = new PlotInfoValidator { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
                    piv.Validate(pi);

                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        pe.BeginPlot(null, null);
                        pe.BeginDocument(pi, doc.Name, null, 1, true, pdfPath);
                        var ppi = new PlotPageInfo();
                        pe.BeginPage(ppi, pi, true, null);
                        pe.BeginGenerateGraphics(null);
                        pe.EndGenerateGraphics(null);
                        pe.EndPage(null);
                        pe.EndDocument(null);
                        pe.EndPlot(null);
                    }
                }
                tr.Commit();
            }
        }

        /// <summary>Ma trận WCS -> DCS của khung nhìn hiện hành (SetPlotWindowArea nhận toạ độ DCS).</summary>
        private static Matrix3d WcsToDcs(CadDocument doc)
        {
            using (ViewTableRecord vtr = doc.Editor.GetCurrentView())
            {
                Matrix3d dcsToWcs =
                    Matrix3d.Rotation(-vtr.ViewTwist, vtr.ViewDirection, vtr.Target) *
                    Matrix3d.Displacement(vtr.Target - Point3d.Origin) *
                    Matrix3d.PlaneToWorld(vtr.ViewDirection);
                return dcsToWcs.Inverse();
            }
        }

        /// <summary>Tên layer trong SelectionFilter là mẫu wildcard: thoát các ký tự đặc biệt bằng `.</summary>
        private static string EscapeWildcards(string layer)
        {
            const string special = "#@.*?~[]-,`";
            var sb = new System.Text.StringBuilder();
            foreach (char c in layer ?? string.Empty)
            {
                if (special.IndexOf(c) >= 0)
                    sb.Append('`');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
