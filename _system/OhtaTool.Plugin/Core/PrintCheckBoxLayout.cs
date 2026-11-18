using System;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Kết quả bố cục một trang PDF khi chèn ô 材料寸法チェック ở góc trên-trái.</summary>
    public sealed class CheckBoxPageLayout
    {
        public double PageWidthPt;
        public double PageHeightPt;
        public double BoxLeftPt;
        public double BoxTopPt;
        public double BoxWidthPt;
        public double BoxHeightPt;
        public double DividerXPt;
        public double ContentLeftPt;
        public double ContentTopPt;
        public double ContentWidthPt;
        public double ContentHeightPt;
        public double ContentScale;
    }

    /// <summary>
    /// Tính vị trí và tỷ lệ ô 材料寸法チェック theo kích thước từng khung CAD (Khung_IN)
    /// và thu nhỏ đồng dạng nội dung bản in gốc xuống dưới để không bao giờ bị đè.
    /// </summary>
    public static class PrintCheckBoxLayout
    {
        public const double MmToPt = 72.0 / 25.4;
        public const double DefaultBoxWidthCad = 150.0;
        public const double DefaultBoxHeightCad = 50.0;
        public const string CheckBoxText = "材料寸法チェック";
        public const double LeftCellRatio = 0.55;

        private const double MinWidthMm = 30.0;
        private const double MinHeightMm = 6.0;
        private const double MaxWidthRatio = 0.45;
        private const double MaxHeightRatio = 0.18;
        private const double MarginMm = 5.0;
        private const double GapMm = 2.0;

        public static CheckBoxPageLayout Compute(
            double pageWidthPt,
            double pageHeightPt,
            double frameWidthCad,
            double frameHeightCad,
            double boxWidthCad,
            double boxHeightCad)
        {
            if (pageWidthPt <= 0 || pageHeightPt <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageWidthPt), "Kích thước trang PDF phải > 0.");
            if (frameWidthCad <= 0 || frameHeightCad <= 0)
                throw new ArgumentOutOfRangeException(nameof(frameWidthCad), "Kích thước khung CAD phải > 0.");
            if (boxWidthCad <= 0)
                boxWidthCad = DefaultBoxWidthCad;
            if (boxHeightCad <= 0)
                boxHeightCad = DefaultBoxHeightCad;

            double cadToPdfScale = Math.Min(pageWidthPt / frameWidthCad, pageHeightPt / frameHeightCad);
            double w = boxWidthCad * cadToPdfScale;
            double h = boxHeightCad * cadToPdfScale;

            // Kẹp tối thiểu trên giấy để đọc rõ khi khung CAD quá lớn
            double minW = MinWidthMm * MmToPt;
            double minH = MinHeightMm * MmToPt;
            double upScale = Math.Max(1.0, Math.Max(minW / w, minH / h));
            w *= upScale;
            h *= upScale;

            // Kẹp tối đa theo trang PDF để không tràn trang khi khung CAD quá nhỏ
            double maxW = pageWidthPt * MaxWidthRatio;
            double maxH = pageHeightPt * MaxHeightRatio;
            double downScale = Math.Min(1.0, Math.Min(maxW / w, maxH / h));
            w *= downScale;
            h *= downScale;

            double marginPt = Math.Min(MarginMm * MmToPt, Math.Min(pageWidthPt * 0.04, pageHeightPt * 0.04));
            double gapPt = Math.Min(GapMm * MmToPt, pageHeightPt * 0.02);

            double boxLeft = marginPt;
            double boxTop = marginPt;
            double boxBottom = boxTop + h;

            double reservedTop = boxBottom + gapPt;
            double availH = Math.Max(1.0, pageHeightPt - reservedTop);
            double contentScale = availH / pageHeightPt;
            double contentW = pageWidthPt * contentScale;
            double contentH = availH;
            double contentLeft = (pageWidthPt - contentW) / 2.0;

            return new CheckBoxPageLayout
            {
                PageWidthPt = pageWidthPt,
                PageHeightPt = pageHeightPt,
                BoxLeftPt = boxLeft,
                BoxTopPt = boxTop,
                BoxWidthPt = w,
                BoxHeightPt = h,
                DividerXPt = boxLeft + w * LeftCellRatio,
                ContentLeftPt = contentLeft,
                ContentTopPt = reservedTop,
                ContentWidthPt = contentW,
                ContentHeightPt = contentH,
                ContentScale = contentScale
            };
        }
    }
}
