using System;
using System.Collections.Generic;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Tests
{
    internal static partial class Program
    {
        private static FrameRect R(string handle, double xmin, double ymin, double xmax, double ymax) =>
            new FrameRect { Handle = handle, Xmin = xmin, Ymin = ymin, Xmax = xmax, Ymax = ymax };

        private static void TestPrintCore()
        {
            Console.WriteLine("--- Tab PRINT: khung in, ten PDF ---");

            var f = PrintCore.SortFrames(new[] {
                R("a", 0, 0, 10, 50), R("b", 20, 0, 30, 100), R("c", 40, 0, 50, 100.5), R("d", -10, 0, 0, 100.4) });
            Check("SortFrames_TopDown_ThenLeft", string.Join(",", f.Select(x => x.Handle)) == "d,b,c,a",
                string.Join(",", f.Select(x => x.Handle)));

            var g = new[] { R("p", 0, -5, 1, 0), R("q", 5, -5, 6, 0.8), R("r", 10, -5, 11, 1.6) };
            string g1 = string.Join(",", PrintCore.SortFrames(g).Select(x => x.Handle));
            string g2 = string.Join(",", PrintCore.SortFrames(g.Reverse()).Select(x => x.Handle));
            Check("SortFrames_ChainedTolerance_Deterministic", g1 == "q,r,p" && g1 == g2, g1 + " / " + g2);

            Check("IsPortrait_Square", PrintCore.IsPortrait(100, 100));
            Check("IsPortrait_Wide", !PrintCore.IsPortrait(420, 297));

            Check("PdfBaseName_Cases",
                PrintCore.PdfBaseName("ABC-123-S.dwg") == "ABC-123" && PrintCore.PdfBaseName("abc-a.DWG") == "abc" &&
                PrintCore.PdfBaseName("DATA.dwg") == "DATA" && PrintCore.PdfBaseName("X-SA.dwg") == "X-SA" &&
                PrintCore.PdfBaseName("Y-S-A.dwg") == "Y-S");

            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\d\A (1).pdf", @"C:\d\A (2).pdf" };
            Check("NextFreeName_Skips", PrintCore.NextFreeName(@"C:\d\A.pdf", existing.Contains) == @"C:\d\A (3).pdf");
            Check("PerFrameNames", string.Join("|", PrintCore.PerFrameNames(@"C:\d\A.pdf", 2)) == @"C:\d\A-1.pdf|C:\d\A-2.pdf");
            Check("PickFrameLayer_CaseInsensitive", PrintCore.PickFrameLayer(new[] { "0", "khung_in" }, new[] { "Khung_IN" }) == "khung_in");
            Check("PickFrameLayer_None", PrintCore.PickFrameLayer(new[] { "0" }, new[] { "Khung_IN" }) == null);
            Check("IsWarningLayer", PrintCore.IsWarningLayer("canh_bao_lech") && PrintCore.IsWarningLayer("GRO_XEP_NHOM") &&
                !PrintCore.IsWarningLayer("0-KAKOU"));

            // Review: in từ tab Layout (TILEMODE=0) cho vùng in sai -> chặn; bản vẽ chưa lưu -> chặn
            Check("Preflight_LayoutTab_Blocked", PrintCore.PreflightError(tileMode: 0, dwgTitled: 1) != null);
            Check("Preflight_Untitled_Blocked", PrintCore.PreflightError(tileMode: 1, dwgTitled: 0) == "Hãy lưu bản vẽ trước khi in.");
            Check("Preflight_ModelSaved_Ok", PrintCore.PreflightError(tileMode: 1, dwgTitled: 1) == null);
            // Lỗi thật 04/11: in nhiều trang một lần PlotEngine ra file DWF đội tên .pdf -> phải nhận ra
            Check("IsPdfHeader", PrintCore.IsPdfHeader(System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n")) &&
                !PrintCore.IsPdfHeader(System.Text.Encoding.ASCII.GetBytes("(DWF V06.00)PK")) &&
                !PrintCore.IsPdfHeader(new byte[0]) && !PrintCore.IsPdfHeader(null));

            // Chọn tên file đích: có / không hỏi ghi đè
            var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\d\A.pdf" };
            int asked = 0;
            Func<string, int, OverwriteDecision> askOverwrite = (f, n) => { asked++; return OverwriteDecision.Overwrite; };
            var t1 = PrintCore.PlanTargets(@"C:\d\A.pdf", 1, true, true, have.Contains, f => false, askOverwrite);
            Check("PlanTargets_SkipPrompt_Numbered", t1 != null && t1.Targets.Single() == @"C:\d\A (1).pdf" && asked == 0);
            var t2 = PrintCore.PlanTargets(@"C:\d\B.pdf", 1, true, false, have.Contains, f => false, askOverwrite);
            Check("PlanTargets_NoConflict_Base", t2.Targets.Single() == @"C:\d\B.pdf" && asked == 0 && t2.Notice == null);
            var t3 = PrintCore.PlanTargets(@"C:\d\A.pdf", 1, true, false, have.Contains, f => false, askOverwrite);
            Check("PlanTargets_Ask_Overwrite", t3.Targets.Single() == @"C:\d\A.pdf" && asked == 1);
            var t4 = PrintCore.PlanTargets(@"C:\d\A.pdf", 1, true, false, have.Contains, f => true, askOverwrite);
            Check("PlanTargets_OverwriteLocked_Numbered", t4.Targets.Single() == @"C:\d\A (1).pdf" && t4.Notice != null);
            var t5 = PrintCore.PlanTargets(@"C:\d\A.pdf", 1, true, false, have.Contains, f => false, (f, n) => OverwriteDecision.Cancel);
            Check("PlanTargets_Cancel_Null", t5 == null);
            var have2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\d\A-2.pdf", @"C:\d\A (1)-1.pdf" };
            var t6 = PrintCore.PlanTargets(@"C:\d\A.pdf", 2, false, true, have2.Contains, f => false, askOverwrite);
            Check("PlanTargets_PerFrame_SharedNumber", string.Join("|", t6.Targets) == @"C:\d\A (2)-1.pdf|C:\d\A (2)-2.pdf",
                string.Join("|", t6.Targets));

            // Thanh tiến độ
            Check("ProgressText_Page", PrintCore.ProgressText(2, 8) == "Đang in trang 3/8");
            Check("ProgressText_Merge", PrintCore.ProgressText(8, 8) == "Đang gộp PDF…");
            Check("ProgressText_Single", PrintCore.ProgressText(0, 1) == "Đang in trang 1/1" && PrintCore.ProgressText(1, 1) == "Đang lưu PDF…");
            Check("CancelledText", PrintCore.CancelledText(3, 8) == "Đã huỷ in (đã in 3/8 trang).");

            // Review: bản vẽ STB (named plot styles) không nhận .ctb và ngược lại
            Check("StyleSheetMatchesMode", PrintCore.StyleSheetMatchesMode("monochrome.ctb", colorDependent: true) &&
                !PrintCore.StyleSheetMatchesMode("monochrome.ctb", colorDependent: false) &&
                PrintCore.StyleSheetMatchesMode("acad.STB", colorDependent: false));
        }

        private sealed class FakeCatalog : IPlotCatalog
        {
            public IList<string> Devices { get; } = new List<string> { "DWG To PDF.pc3", "Foo.pc3" };
            public IList<string> StyleSheets { get; } = new List<string> { "monochrome.ctb", "acad.ctb" };
            public IList<string> MediaFor(string device) =>
                string.Equals(device, "Foo.pc3", StringComparison.OrdinalIgnoreCase)
                    ? new List<string> { "Letter" }
                    : new List<string> { "A3", "A4" };
        }

        private static void TestPrintSettings()
        {
            Console.WriteLine("--- Tab PRINT: cau hinh in, uu tien ban ve ---");
            var cat = new FakeCatalog();

            var d = new PrintParams { Device = "DWG To PDF.pc3", Media = "A4", StyleSheet = "acad.ctb", Lineweights = false };
            var def = new PrintParams { Device = "DWG To PDF.pc3", Media = "A3", StyleSheet = "monochrome.ctb", Lineweights = true, Merge = true, OpenAfter = true };
            var r1 = PrintSettingsResolver.Resolve(null, d, def, cat);
            Check("Resolve_FromDrawing", r1.Values.Media == "A4" && r1.Values.StyleSheet == "acad.ctb" &&
                r1.Values.Lineweights == false && r1.Values.Merge == true && r1.Warnings.Count == 0,
                string.Join(" | ", r1.Warnings));

            var bw = new PrintProfile { Name = "Đen trắng", Params = new PrintParams { StyleSheet = "bw.ctb" } };
            var r2 = PrintSettingsResolver.Resolve(bw, d, def, cat);
            Check("Resolve_ProfileMissing_WarnsDrawing", r2.Values.StyleSheet == "acad.ctb" && r2.Warnings.Count == 1 &&
                r2.Warnings[0] == "Không có bw.ctb → đang dùng acad.ctb (theo bản vẽ)", string.Join(" | ", r2.Warnings));

            var r3 = PrintSettingsResolver.Resolve(new PrintProfile { Name = "x", Params = new PrintParams { StyleSheet = "monochrome.ctb" } }, d, def, cat);
            Check("Resolve_ProfileBeatsDrawing", r3.Values.StyleSheet == "monochrome.ctb" && r3.Warnings.Count == 0);

            var r4 = PrintSettingsResolver.Resolve(new PrintProfile { Name = "y", Params = new PrintParams { Device = "Foo.pc3" } }, d, def, cat);
            Check("Resolve_MediaCheckedAgainstResolvedDevice", r4.Values.Device == "Foo.pc3" && r4.Values.Media == "Letter",
                r4.Values.Device + " / " + r4.Values.Media);

            var none = new PrintParams { Device = "X.pc3", Media = "Z", StyleSheet = "z.ctb" };
            var r5 = PrintSettingsResolver.Resolve(new PrintProfile { Name = "z", Params = none }, none, none, cat);
            Check("Resolve_AllMissing_FallsBackToFirstAvailable", r5.Values.Device == "DWG To PDF.pc3" &&
                r5.Values.Media == "A3" && r5.Values.StyleSheet == "monochrome.ctb" && r5.Warnings.Count == 3 &&
                r5.Values.Lineweights == true && r5.Values.OpenAfter == true, string.Join(" | ", r5.Warnings));

            // Bỏ cài đặt nét in: lineweight chỉ theo page setup của bản vẽ, bản vẽ không đặt -> bật
            var lwOff = new PrintParams { Lineweights = false };
            var rLw1 = PrintSettingsResolver.Resolve(new PrintProfile { Name = "lw", Params = lwOff }, new PrintParams(), lwOff, cat);
            var rLw2 = PrintSettingsResolver.Resolve(null, new PrintParams { Lineweights = false }, new PrintParams { Lineweights = true }, cat);
            Check("Resolve_Lineweights_FollowDrawingOnly", rLw1.Values.Lineweights == true && rLw2.Values.Lineweights == false);

            var r6 = PrintSettingsResolver.Resolve(null, new PrintParams { Media = "a4" }, def, cat);
            Check("Resolve_CaseInsensitive_ReturnsCatalogName", r6.Values.Media == "A4" && r6.Warnings.Count == 0);

            var r7 = PrintSettingsResolver.Resolve(null, null, null, new EmptyCatalog());
            Check("Resolve_EmptyCatalog_Warns", r7.Values.Device == "" && r7.Values.StyleSheet == "" && r7.Warnings.Count == 3,
                string.Join(" | ", r7.Warnings));

            var rt = PrintSettingsCodec.ProfilesFromJson(PrintSettingsCodec.ProfilesToJson(new[] { bw }));
            Check("Codec_RoundTrip", rt.Count == 1 && rt[0].Name == "Đen trắng" && rt[0].Params.StyleSheet == "bw.ctb" &&
                rt[0].Params.Device == null && rt[0].Params.Merge == null);
            var pr = PrintSettingsCodec.ParamsFromJson(PrintSettingsCodec.ParamsToJson(def));
            Check("Codec_ParamsRoundTrip", pr.Device == "DWG To PDF.pc3" && pr.Lineweights == true && pr.OpenAfter == true);
            Check("Codec_LayersRoundTrip", string.Join("|", PrintSettingsCodec.LayersFromJson(PrintSettingsCodec.LayersToJson(new[] { "Khung_IN", "KHUNG" }))) == "Khung_IN|KHUNG");
            Check("Codec_CorruptJson_ReturnsDefaults", PrintSettingsCodec.ParamsFromJson("{oops").Device == null &&
                PrintSettingsCodec.ProfilesFromJson(null).Count == 0 && PrintSettingsCodec.LayersFromJson("[1,").Count == 0);

            var zw = PrintSettingsData.CreateDefault(true);
            var ac = PrintSettingsData.CreateDefault(false);
            Check("Defaults_PerPlatform", zw.Defaults.Device == "DWG to PDF.pc5" && ac.Defaults.Device == "DWG To PDF.pc3" &&
                ac.Defaults.Media == "ISO_full_bleed_A3_(420.00_x_297.00_MM)" && ac.Defaults.StyleSheet == "monochrome.ctb" &&
                ac.FrameLayers.Single() == "Khung_IN" && ac.LastProfile == null && ac.Profiles.Count == 0 &&
                ac.AddCheckBox && ac.CheckBoxWidthCad == 150.0 && ac.CheckBoxHeightCad == 50.0);
        }

        private static void TestPdfMerge()
        {
            Console.WriteLine("--- Tab PRINT: gop PDF & dong dau o kiem tra (PdfSharp) ---");
            double a3W = 420.0 * PrintCheckBoxLayout.MmToPt;
            double a3H = 297.0 * PrintCheckBoxLayout.MmToPt;

            // 1. Khung 1:2 (2100x1485 CAD -> o mac dinh 150x50 thanh 30x10 mm) o goc tren-trai, khong de hinh goc
            var l1 = PrintCheckBoxLayout.Compute(a3W, a3H, 2100, 1485, 150, 50);
            Check("CheckBoxLayout_Scale1to5",
                Math.Abs(l1.BoxWidthPt - 30.0 * PrintCheckBoxLayout.MmToPt) < 0.01 &&
                Math.Abs(l1.BoxHeightPt - 10.0 * PrintCheckBoxLayout.MmToPt) < 0.01 &&
                Math.Abs(l1.BoxLeftPt - 5.0 * PrintCheckBoxLayout.MmToPt) < 0.01 &&
                l1.ContentTopPt > l1.BoxTopPt + l1.BoxHeightPt &&
                l1.ContentTopPt + l1.ContentHeightPt <= a3H + 1e-6 &&
                l1.ContentLeftPt >= -1e-6 &&
                l1.ContentLeftPt + l1.ContentWidthPt <= a3W + 1e-6,
                $"w={l1.BoxWidthPt / PrintCheckBoxLayout.MmToPt:0.00}mm, left={l1.BoxLeftPt / PrintCheckBoxLayout.MmToPt:0.00}mm, contentTop={l1.ContentTopPt:0.0}");

            // 2. Khung nho hon (1050x742.5 CAD -> scale 0.4 -> o to gap doi: 60x20 mm)
            var l2 = PrintCheckBoxLayout.Compute(a3W, a3H, 1050, 742.5, 150, 50);
            Check("CheckBoxLayout_SmallerFrame_LargerBox",
                Math.Abs(l2.BoxWidthPt - 60.0 * PrintCheckBoxLayout.MmToPt) < 0.01 &&
                Math.Abs(l2.BoxHeightPt - 20.0 * PrintCheckBoxLayout.MmToPt) < 0.01);

            // 3. Khung cuc lon -> kep toi thieu 30x10 mm (giu ty le 150:50)
            var lMin = PrintCheckBoxLayout.Compute(a3W, a3H, 30000, 20000, 150, 50);
            Check("CheckBoxLayout_MinClamp",
                Math.Abs(lMin.BoxWidthPt - 30.0 * PrintCheckBoxLayout.MmToPt) < 0.01 &&
                Math.Abs(lMin.BoxHeightPt - 10.0 * PrintCheckBoxLayout.MmToPt) < 0.01);

            // 4. Khung cuc nho -> kep toi da <= 45% rong va <= 18% cao trang
            var lMax = PrintCheckBoxLayout.Compute(a3W, a3H, 200, 140, 150, 50);
            Check("CheckBoxLayout_MaxClamp",
                lMax.BoxWidthPt <= a3W * 0.45 + 1e-6 &&
                lMax.BoxHeightPt <= a3H * 0.18 + 1e-6 &&
                lMax.ContentTopPt > lMax.BoxTopPt + lMax.BoxHeightPt);

            // 5. Trang doc (Portrait)
            var lPort = PrintCheckBoxLayout.Compute(a3H, a3W, 1050, 1485, 150, 50);
            Check("CheckBoxLayout_Portrait_NoOverlap",
                lPort.ContentTopPt > lPort.BoxTopPt + lPort.BoxHeightPt &&
                lPort.BoxLeftPt > 0 && lPort.BoxLeftPt + lPort.BoxWidthPt < a3H);

            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ohta_print_test_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                var inputs = new List<string>();
                for (int i = 1; i <= 2; i++)
                {
                    string raw = System.IO.Path.Combine(dir, i + "_raw.pdf");
                    string stamped = System.IO.Path.Combine(dir, i + ".pdf");
                    using (var doc = new PdfSharp.Pdf.PdfDocument())
                    {
                        var pg = doc.AddPage();
                        pg.Width = PdfSharp.Drawing.XUnit.FromMillimeter(i == 1 ? 420 : 297);
                        pg.Height = PdfSharp.Drawing.XUnit.FromMillimeter(i == 1 ? 297 : 420);
                        using (var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(pg))
                        {
                            gfx.DrawRectangle(PdfSharp.Drawing.XPens.Black, 10, 10, pg.Width.Point - 20, pg.Height.Point - 20);
                        }
                        doc.Save(raw);
                    }
                    OhtaTool.Plugin.Services.PdfSharpMerge.StampCheckBox(
                        raw, stamped, i == 1 ? 4200 : 2970, i == 1 ? 2970 : 4200, 700, 140);
                    inputs.Add(stamped);
                }
                string output = System.IO.Path.Combine(dir, "gop.pdf");
                OhtaTool.Plugin.Services.PdfSharpMerge.Merge(inputs, output);
                int pages;
                double p0W, p1W;
                using (var merged = PdfSharp.Pdf.IO.PdfReader.Open(output, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                {
                    pages = merged.PageCount;
                    p0W = merged.Pages[0].Width.Millimeter;
                    p1W = merged.Pages[1].Width.Millimeter;
                }
                Check("PdfMerge_StampedTwoPages", pages == 2 && Math.Abs(p0W - 420) < 0.5 && Math.Abs(p1W - 297) < 0.5,
                    $"pages={pages}, p0W={p0W:0.0}, p1W={p1W:0.0}");
                string jpFont = OhtaTool.Plugin.Services.PdfSharpMerge.ResolveJapaneseFontFamily();
                Check("PdfMerge_JapaneseFontResolved", !string.IsNullOrEmpty(jpFont) && jpFont != "Arial",
                    "font=" + (jpFont ?? "null"));

                // Trang co /Rotate = 90 (297x420 xoay 90 -> hien thi ngang 420x297) va /Rotate = 270
                string rot90Raw = System.IO.Path.Combine(dir, "rot90_raw.pdf");
                string rot90Out = System.IO.Path.Combine(dir, "rot90_out.pdf");
                using (var doc = new PdfSharp.Pdf.PdfDocument())
                {
                    var pg = doc.AddPage();
                    pg.Width = PdfSharp.Drawing.XUnit.FromMillimeter(297);
                    pg.Height = PdfSharp.Drawing.XUnit.FromMillimeter(420);
                    pg.Rotate = 90;
                    doc.Save(rot90Raw);
                }
                OhtaTool.Plugin.Services.PdfSharpMerge.StampCheckBox(rot90Raw, rot90Out, 4200, 2970, 700, 140);
                using (var outDoc = PdfSharp.Pdf.IO.PdfReader.Open(rot90Out, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                {
                    var p = outDoc.Pages[0];
                    Check("PdfStamp_Rotate90_VisualLandscape",
                        Math.Abs(p.Width.Millimeter - 420) < 0.5 && Math.Abs(p.Height.Millimeter - 297) < 0.5 && p.Rotate == 0,
                        $"w={p.Width.Millimeter:0.0}, h={p.Height.Millimeter:0.0}, rot={p.Rotate}");
                }

                string rot270Raw = System.IO.Path.Combine(dir, "rot270_raw.pdf");
                string rot270Out = System.IO.Path.Combine(dir, "rot270_out.pdf");
                using (var doc = new PdfSharp.Pdf.PdfDocument())
                {
                    var pg = doc.AddPage();
                    pg.Width = PdfSharp.Drawing.XUnit.FromMillimeter(420);
                    pg.Height = PdfSharp.Drawing.XUnit.FromMillimeter(297);
                    pg.Rotate = 270;
                    doc.Save(rot270Raw);
                }
                OhtaTool.Plugin.Services.PdfSharpMerge.StampCheckBox(rot270Raw, rot270Out, 2970, 4200, 700, 140);
                using (var outDoc = PdfSharp.Pdf.IO.PdfReader.Open(rot270Out, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                {
                    var p = outDoc.Pages[0];
                    Check("PdfStamp_Rotate270_VisualPortrait",
                        Math.Abs(p.Width.Millimeter - 297) < 0.5 && Math.Abs(p.Height.Millimeter - 420) < 0.5 && p.Rotate == 0,
                        $"w={p.Width.Millimeter:0.0}, h={p.Height.Millimeter:0.0}, rot={p.Rotate}");
                }
            }
            finally
            {
                try { System.IO.Directory.Delete(dir, true); } catch (Exception) { }
            }
        }

        private sealed class EmptyCatalog : IPlotCatalog
        {
            public IList<string> Devices { get; } = new List<string>();
            public IList<string> StyleSheets { get; } = new List<string>();
            public IList<string> MediaFor(string device) => new List<string>();
        }
    }
}
