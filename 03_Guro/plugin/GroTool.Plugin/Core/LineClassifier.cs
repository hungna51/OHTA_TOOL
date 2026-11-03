using System;
using System.Collections.Generic;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Thông tin thô của một đối tượng nét vẽ trên layer gốc.
    /// </summary>
    public sealed class RawLineItem
    {
        public string Linetype { get; set; } = "Continuous";
        public int ColorIndex { get; set; } = 7;
        public object EntityId { get; set; }
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
    }

    /// <summary>
    /// Kết quả gom nhóm các nét chưa đổi theo Kiểu nét (Linetype).
    /// </summary>
    public sealed class LineGroupResult
    {
        public string Linetype { get; set; } = string.Empty;
        public int ColorIndex { get; set; } = 7;
        public string ColorName { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<object> EntityIds { get; set; } = new List<object>();
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }

        public string DisplayText => $"{Linetype} — {Count} đối tượng";
    }

    /// <summary>
    /// Xử lý phân loại và gom nhóm nét vẽ. Không phụ thuộc ZWCAD để kiểm thử Unit Test.
    /// </summary>
    public static class LineClassifier
    {
        /// <summary>
        /// Chuẩn hoá tên kiểu nét.
        /// </summary>
        public static string ChuanHoaLinetype(string lt)
        {
            if (string.IsNullOrWhiteSpace(lt)) return "Continuous";
            return lt.Trim();
        }

        /// <summary>
        /// Gom nhóm các đối tượng nét theo Kiểu nét (Linetype), bỏ qua màu sắc.
        /// Sắp xếp giảm dần theo số lượng đối tượng.
        /// </summary>
        public static List<LineGroupResult> GomNhom(IEnumerable<RawLineItem> items)
        {
            if (items == null) return new List<LineGroupResult>();

            var list = items.ToList();
            if (list.Count == 0) return new List<LineGroupResult>();

            return list
                .GroupBy(x => ChuanHoaLinetype(x.Linetype), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    string lt = g.First().Linetype;
                    if (string.IsNullOrWhiteSpace(lt)) lt = "Continuous";
                    else lt = lt.Trim();

                    return new LineGroupResult
                    {
                        Linetype = lt,
                        ColorIndex = 7,
                        ColorName = string.Empty,
                        Count = g.Count(),
                        EntityIds = g.Select(x => x.EntityId).Where(id => id != null).ToList(),
                        MinX = g.Min(x => x.MinX),
                        MinY = g.Min(x => x.MinY),
                        MaxX = g.Max(x => x.MaxX),
                        MaxY = g.Max(x => x.MaxY)
                    };
                })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Linetype, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Chuỗi thống kê tóm tắt một dòng ở chân tab DSN.
        /// </summary>
        public static string ThongKeDsn(int tongSoNet, int soNhom)
        {
            if (tongSoNet <= 0) return "Không còn nét nào chưa đổi";
            return $"Tổng {tongSoNet} nét chưa đổi │ {soNhom} nhóm kiểu nét";
        }
    }
}
