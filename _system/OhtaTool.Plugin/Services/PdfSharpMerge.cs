using System;
using System.Collections.Generic;
using System.IO;
using OhtaTool.Plugin.Core;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Nơi duy nhất tham chiếu kiểu PdfSharp. Gọi qua PdfMerger để resolver DLL được đăng ký
    /// trước khi JIT chạm tới các kiểu này (ADR 0005).
    /// </summary>
    internal static class PdfSharpMerge
    {
        private static readonly object FontGate = new object();
        private static bool _fontConfigured;
        private static readonly string[] JapaneseFontFiles =
        {
            "YuGothM.ttc", "YuGothR.ttc", "meiryo.ttc", "msgothic.ttc", "BIZ-UDGothicR.ttc"
        };

        private sealed class JapaneseFontResolver : IFontResolver
        {
            private readonly IFontResolver _inner;
            private readonly byte[] _jpFontBytes;
            private const string FaceName = "OhtaJapanese#Regular";

            public JapaneseFontResolver(IFontResolver inner, byte[] jpFontBytes)
            {
                _inner = inner;
                _jpFontBytes = jpFontBytes;
            }

            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                if (_jpFontBytes != null && string.Equals(familyName, "OhtaJapanese", StringComparison.OrdinalIgnoreCase))
                    return new FontResolverInfo(FaceName);
                return _inner?.ResolveTypeface(familyName, isBold, isItalic);
            }

            public byte[] GetFont(string faceName)
            {
                if (faceName == FaceName)
                    return _jpFontBytes;
                return _inner?.GetFont(faceName);
            }
        }

        public static void Merge(IList<string> inputs, string output)
        {
            using (var target = new PdfDocument())
            {
                foreach (string path in inputs)
                {
                    using (PdfDocument src = PdfReader.Open(path, PdfDocumentOpenMode.Import))
                    {
                        for (int i = 0; i < src.PageCount; i++)
                            target.AddPage(src.Pages[i]);
                    }
                }
                target.Save(output);
            }
        }

        /// <summary>
        /// Thu nhỏ nhẹ trang PDF gốc xuống dưới để chừa lề trên và vẽ ô 材料寸法チェック
        /// vào góc trên-trái theo kích thước khung CAD.
        /// </summary>
        public static void StampCheckBox(
            string inputPdfPath,
            string outputPdfPath,
            double frameWidthCad,
            double frameHeightCad,
            double boxWidthCad,
            double boxHeightCad)
        {
            EnsureFontSupport();

            double rawW = 0, rawH = 0;
            int rotate = 0;
            using (PdfDocument src = PdfReader.Open(inputPdfPath, PdfDocumentOpenMode.Import))
            {
                if (src.PageCount > 0)
                {
                    PdfPage sp = src.Pages[0];
                    rawW = sp.Width.Point;
                    rawH = sp.Height.Point;
                    rotate = ((sp.Rotate % 360) + 360) % 360;
                }
            }

            using (var form = XPdfForm.FromFile(inputPdfPath))
            {
                form.PageNumber = 1;
                if (rawW <= 0 || rawH <= 0)
                {
                    rawW = form.PointWidth;
                    rawH = form.PointHeight;
                }

                // XPdfForm.PointWidth/PointHeight luôn trả kích thước chưa xoay (/Rotate 90/270),
                // trong khi /Fm0 đã tự mang /Matrix xoay sẵn về hướng hiển thị thật -> phải đổi W/H
                // và bù chiều vẽ trong DrawImage để không bị cắt xén phần hình bị xoay.
                bool swapped = rotate == 90 || rotate == 270;
                double pageW = swapped ? rawH : rawW;
                double pageH = swapped ? rawW : rawH;

                CheckBoxPageLayout layout = PrintCheckBoxLayout.Compute(
                    pageW, pageH, frameWidthCad, frameHeightCad, boxWidthCad, boxHeightCad);

                using (var target = new PdfDocument())
                {
                    PdfPage page = target.AddPage();
                    page.Width = XUnit.FromPoint(layout.PageWidthPt);
                    page.Height = XUnit.FromPoint(layout.PageHeightPt);

                    using (XGraphics gfx = XGraphics.FromPdfPage(page))
                    {
                        double drawW = form.PointWidth * layout.ContentScale;
                        double drawH = form.PointHeight * layout.ContentScale;
                        double drawY = layout.ContentTopPt + layout.ContentHeightPt - drawH;
                        gfx.DrawImage(form, layout.ContentLeftPt, drawY, drawW, drawH);

                        double strokePt = Math.Max(0.7, Math.Min(1.6, layout.BoxHeightPt * 0.04));
                        var redPen = new XPen(XColor.FromArgb(255, 0, 0), strokePt);
                        gfx.DrawRectangle(redPen, XBrushes.White, layout.BoxLeftPt, layout.BoxTopPt, layout.BoxWidthPt, layout.BoxHeightPt);
                        gfx.DrawLine(redPen, layout.DividerXPt, layout.BoxTopPt, layout.DividerXPt, layout.BoxTopPt + layout.BoxHeightPt);

                        double leftCellW = layout.DividerXPt - layout.BoxLeftPt;
                        double fontSize = Math.Max(4.0, layout.BoxHeightPt * 0.50);
                        XFont font = CreateJapaneseFont(fontSize);
                        XSize measured = gfx.MeasureString(PrintCheckBoxLayout.CheckBoxText, font);
                        double maxTextW = leftCellW * 0.88;
                        if (measured.Width > maxTextW && measured.Width > 0.1)
                        {
                            fontSize = Math.Max(3.5, fontSize * (maxTextW / measured.Width));
                            font = CreateJapaneseFont(fontSize);
                        }

                        var textBrush = new XSolidBrush(XColor.FromArgb(40, 40, 40));
                        var rect = new XRect(layout.BoxLeftPt, layout.BoxTopPt, leftCellW, layout.BoxHeightPt);
                        gfx.DrawString(PrintCheckBoxLayout.CheckBoxText, font, textBrush, rect, XStringFormats.Center);
                    }

                    target.Save(outputPdfPath);
                }
            }
        }

        private static void EnsureFontSupport()
        {
            lock (FontGate)
            {
                if (_fontConfigured)
                    return;
                byte[] jpBytes = LoadJapaneseFontBytes();
                if (jpBytes != null && !(GlobalFontSettings.FontResolver is JapaneseFontResolver))
                {
                    if (GlobalFontSettings.FontResolver == null)
                        GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                    GlobalFontSettings.FontResolver = new JapaneseFontResolver(GlobalFontSettings.FontResolver, jpBytes);
                }
                else if (GlobalFontSettings.FontResolver == null)
                {
                    GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                }
                GlobalFontSettings.DefaultFontEncoding = PdfFontEncoding.Unicode;
                _fontConfigured = true;
            }
        }

        private static byte[] LoadJapaneseFontBytes()
        {
            string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            if (string.IsNullOrEmpty(fontsDir) || !Directory.Exists(fontsDir))
                return null;
            foreach (string file in JapaneseFontFiles)
            {
                string path = Path.Combine(fontsDir, file);
                if (!File.Exists(path))
                    continue;
                byte[] raw = File.ReadAllBytes(path);
                byte[] ttf = ExtractTtfIfCollection(raw, 0);
                if (ttf != null && ttf.Length > 0)
                    return ttf;
            }
            return null;
        }

        /// <summary>
        /// PDFsharp chỉ đọc font đơn (.ttf), không đọc trực tiếp TrueType Collection (.ttc).
        /// Hàm này tách font thứ fontIndex từ .ttc thành một file .ttf độc lập trong bộ nhớ.
        /// </summary>
        internal static byte[] ExtractTtfIfCollection(byte[] data, int fontIndex)
        {
            if (data == null || data.Length < 16)
                return data;
            // Magic "ttcf"
            if (data[0] != (byte)'t' || data[1] != (byte)'t' || data[2] != (byte)'c' || data[3] != (byte)'f')
                return data;

            int numFonts = (int)ReadU32BE(data, 8);
            if (fontIndex < 0 || fontIndex >= numFonts || 12 + (fontIndex + 1) * 4 > data.Length)
                return null;

            int sfntOffset = (int)ReadU32BE(data, 12 + fontIndex * 4);
            if (sfntOffset < 0 || sfntOffset + 12 > data.Length)
                return null;

            int numTables = ReadU16BE(data, sfntOffset + 4);
            int dirSize = 12 + numTables * 16;
            if (numTables <= 0 || sfntOffset + dirSize > data.Length)
                return null;

            int totalSize = dirSize;
            for (int i = 0; i < numTables; i++)
            {
                int rec = sfntOffset + 12 + i * 16;
                int len = (int)ReadU32BE(data, rec + 12);
                int padded = (len + 3) & ~3;
                totalSize += padded;
            }

            byte[] ttf = new byte[totalSize];
            Buffer.BlockCopy(data, sfntOffset, ttf, 0, dirSize);

            int writePos = dirSize;
            for (int i = 0; i < numTables; i++)
            {
                int srcRec = sfntOffset + 12 + i * 16;
                int dstRec = 12 + i * 16;
                int oldOffset = (int)ReadU32BE(data, srcRec + 8);
                int len = (int)ReadU32BE(data, srcRec + 12);
                if (oldOffset < 0 || len < 0 || oldOffset + len > data.Length)
                    return null;

                WriteU32BE(ttf, dstRec + 8, (uint)writePos);
                Buffer.BlockCopy(data, oldOffset, ttf, writePos, len);
                writePos += (len + 3) & ~3;
            }
            return ttf;
        }

        private static ushort ReadU16BE(byte[] b, int o) => (ushort)((b[o] << 8) | b[o + 1]);

        private static uint ReadU32BE(byte[] b, int o) =>
            ((uint)b[o] << 24) | ((uint)b[o + 1] << 16) | ((uint)b[o + 2] << 8) | b[o + 3];

        private static void WriteU32BE(byte[] b, int o, uint v)
        {
            b[o] = (byte)(v >> 24);
            b[o + 1] = (byte)(v >> 16);
            b[o + 2] = (byte)(v >> 8);
            b[o + 3] = (byte)v;
        }

        internal static string ResolveJapaneseFontFamily()
        {
            EnsureFontSupport();
            var opts = new XPdfFontOptions(PdfFontEncoding.Unicode);
            var probe = new XFont("OhtaJapanese", 10, XFontStyleEx.Regular, opts);
            using (var doc = new PdfDocument())
            using (var gfx = XGraphics.FromPdfPage(doc.AddPage()))
            {
                if (gfx.MeasureString(PrintCheckBoxLayout.CheckBoxText, probe).Width > 0)
                    return probe.FontFamily.Name;
            }
            return null;
        }

        private static XFont CreateJapaneseFont(double sizePt)
        {
            var opts = new XPdfFontOptions(PdfFontEncoding.Unicode);
            return new XFont("OhtaJapanese", sizePt, XFontStyleEx.Regular, opts);
        }
    }
}
