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

        private static void TestPrintSettings()
        {
        }
    }
}
