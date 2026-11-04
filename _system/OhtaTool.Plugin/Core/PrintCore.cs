using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Hình chữ nhật bao của một khung in (toạ độ WCS của Model).</summary>
    public sealed class FrameRect
    {
        public double Xmin, Ymin, Xmax, Ymax;
        public string Handle;

        public double Width => Xmax - Xmin;
        public double Height => Ymax - Ymin;
    }

    /// <summary>Trả lời khi file PDF đích đã có.</summary>
    public enum OverwriteDecision { Overwrite, SaveAsNumbered, Cancel }

    /// <summary>Tên file đích đã chọn; Notice = thông báo cho người dùng (vd file đang mở nên đổi tên).</summary>
    public sealed class TargetPlan
    {
        public List<string> Targets = new List<string>();
        public string Notice;
    }

    /// <summary>Logic thuần của tab PRINT (lệnh OI): thứ tự khung, hướng giấy, tên file PDF.</summary>
    public static class PrintCore
    {
        /// <summary>Hai khung có Y điểm cao nhất lệch không quá giá trị này (mm) coi như cùng hàng.</summary>
        public const double YTolerance = 1.0;

        /// <summary>
        /// Sắp khung: Y điểm cao nhất giảm dần; khung lệch Y ≤ YTolerance so với khung đầu nhóm thì
        /// xếp trái → phải. Gom nhóm tuần tự thay vì comparator có dung sai (không bắc cầu → Sort ném lỗi).
        /// </summary>
        public static List<FrameRect> SortFrames(IEnumerable<FrameRect> frames)
        {
            var byY = (frames ?? Enumerable.Empty<FrameRect>())
                .Where(r => r != null)
                .OrderByDescending(r => r.Ymax).ThenBy(r => r.Xmin)
                .ToList();
            var result = new List<FrameRect>(byY.Count);
            for (int i = 0; i < byY.Count;)
            {
                double top = byY[i].Ymax;
                int j = i;
                while (j < byY.Count && top - byY[j].Ymax <= YTolerance)
                    j++;
                result.AddRange(byY.GetRange(i, j - i).OrderBy(r => r.Xmin));
                i = j;
            }
            return result;
        }

        /// <summary>Khung cao ≥ rộng → in dọc (giống OIN_IN: góc đường chéo ≤ 135°).</summary>
        public static bool IsPortrait(double width, double height) => height >= width;

        /// <summary>Tên PDF từ tên bản vẽ: bỏ phần mở rộng, rồi bỏ đúng một hậu tố -S / -A cuối tên.</summary>
        public static string PdfBaseName(string dwgName)
        {
            string name = Path.GetFileNameWithoutExtension(dwgName ?? string.Empty);
            if (name.Length > 2 &&
                (name.EndsWith("-S", StringComparison.OrdinalIgnoreCase) ||
                 name.EndsWith("-A", StringComparison.OrdinalIgnoreCase)))
                name = name.Substring(0, name.Length - 2);
            return name;
        }

        /// <summary>"&lt;dir&gt;\&lt;base&gt; (n)&lt;ext&gt;" với n nhỏ nhất (từ 1) chưa tồn tại.</summary>
        public static string NextFreeName(string path, Func<string, bool> exists)
        {
            string dir = Path.GetDirectoryName(path) ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            for (int n = 1; ; n++)
            {
                string candidate = Path.Combine(dir, baseName + " (" + n + ")" + ext);
                if (exists == null || !exists(candidate))
                    return candidate;
            }
        }

        /// <summary>Tên từng file khi không gộp: &lt;base&gt;-1.pdf, &lt;base&gt;-2.pdf…</summary>
        public static List<string> PerFrameNames(string basePath, int count)
        {
            string dir = Path.GetDirectoryName(basePath) ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(basePath);
            string ext = Path.GetExtension(basePath);
            var list = new List<string>();
            for (int i = 1; i <= count; i++)
                list.Add(Path.Combine(dir, baseName + "-" + i + ext));
            return list;
        }

        /// <summary>
        /// Chọn tên file đích. Chưa có file trùng -> tên gốc. Có trùng: skipPrompt -> tự lưu "(n)" không hỏi;
        /// ngược lại hỏi (ask: file trùng đầu tiên, số file). Ghi đè mà file đang bị khoá -> "(n)" + Notice.
        /// Không gộp: một số n chung cho cả bộ &lt;tên&gt; (n)-1.pdf… Trả null nếu người dùng huỷ.
        /// </summary>
        public static TargetPlan PlanTargets(string basePath, int count, bool merge, bool skipPrompt,
            Func<string, bool> exists, Func<string, bool> isLocked, Func<string, int, OverwriteDecision> ask)
        {
            List<string> targets = merge ? new List<string> { basePath } : PerFrameNames(basePath, count);
            string firstExisting = targets.FirstOrDefault(exists);
            if (firstExisting == null)
                return new TargetPlan { Targets = targets };

            var plan = new TargetPlan();
            OverwriteDecision choice = skipPrompt ? OverwriteDecision.SaveAsNumbered : ask(firstExisting, targets.Count);
            if (choice == OverwriteDecision.Cancel)
                return null;

            if (choice == OverwriteDecision.Overwrite)
            {
                string locked = targets.FirstOrDefault(t => exists(t) && isLocked(t));
                if (locked == null)
                {
                    plan.Targets = targets;
                    return plan;
                }
                plan.Notice = $"File {Path.GetFileName(locked)} đang mở, không ghi đè được → lưu thành tên mới.";
            }

            string numberedBase = NextFreeName(basePath, candidate =>
                merge ? exists(candidate) : PerFrameNames(candidate, count).Any(exists));
            plan.Targets = merge ? new List<string> { numberedBase } : PerFrameNames(numberedBase, count);
            return plan;
        }

        /// <summary>Layer khung: mục ưu tiên đầu tiên bản vẽ có (không phân biệt hoa thường), trả tên như trong bản vẽ.</summary>
        public static string PickFrameLayer(IEnumerable<string> drawingLayers, IList<string> preferred)
        {
            if (drawingLayers == null || preferred == null)
                return null;
            var layers = drawingLayers.Where(l => !string.IsNullOrEmpty(l)).ToList();
            foreach (string want in preferred)
            {
                if (string.IsNullOrWhiteSpace(want))
                    continue;
                string hit = layers.FirstOrDefault(l => string.Equals(l, want.Trim(), StringComparison.OrdinalIgnoreCase));
                if (hit != null)
                    return hit;
            }
            return null;
        }

        /// <summary>
        /// Lý do không in được (null = in được). Tab Layout (TILEMODE=0): khung nhìn và toạ độ pick là
        /// của paper space nên vùng in sai -> bắt quay về Model. Bản vẽ chưa lưu: không có thư mục đặt PDF.
        /// </summary>
        public static string PreflightError(int tileMode, int dwgTitled)
        {
            if (tileMode == 0)
                return "Chuyển sang tab Model rồi in lại (OI chỉ in Model).";
            if (dwgTitled == 0)
                return "Hãy lưu bản vẽ trước khi in.";
            return null;
        }

        /// <summary>Chữ trên thanh tiến độ: done = số trang đã in xong. In xong hết -> bước gộp / lưu.</summary>
        public static string ProgressText(int done, int total)
        {
            if (done >= total)
                return total > 1 ? "Đang gộp PDF…" : "Đang lưu PDF…";
            return $"Đang in trang {done + 1}/{total}";
        }

        public static string CancelledText(int done, int total) => $"Đã huỷ in (đã in {done}/{total} trang).";

        /// <summary>File bắt đầu bằng "%PDF" (PlotEngine nhiều trang một lần ra DWF dù máy in là PDF).</summary>
        public static bool IsPdfHeader(byte[] head)
        {
            return head != null && head.Length >= 4 &&
                   head[0] == (byte)'%' && head[1] == (byte)'P' && head[2] == (byte)'D' && head[3] == (byte)'F';
        }

        /// <summary>Bản vẽ kiểu màu (PSTYLEMODE=1) chỉ nhận .ctb, kiểu đặt tên chỉ nhận .stb.</summary>
        public static bool StyleSheetMatchesMode(string styleSheet, bool colorDependent)
        {
            string ext = Path.GetExtension(styleSheet ?? string.Empty);
            return string.Equals(ext, colorDependent ? ".ctb" : ".stb", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Layer cảnh báo không được lên giấy: CANH_BAO_* và GRO_XEP_NHOM.</summary>
        public static bool IsWarningLayer(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return false;
            return layerName.StartsWith("CANH_BAO_", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(layerName, "GRO_XEP_NHOM", StringComparison.OrdinalIgnoreCase);
        }
    }
}
