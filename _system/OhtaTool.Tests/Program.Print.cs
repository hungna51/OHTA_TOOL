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
                ac.FrameLayers.Single() == "Khung_IN" && ac.LastProfile == null && ac.Profiles.Count == 0);
        }

        private sealed class EmptyCatalog : IPlotCatalog
        {
            public IList<string> Devices { get; } = new List<string>();
            public IList<string> StyleSheets { get; } = new List<string>();
            public IList<string> MediaFor(string device) => new List<string>();
        }
    }
}
