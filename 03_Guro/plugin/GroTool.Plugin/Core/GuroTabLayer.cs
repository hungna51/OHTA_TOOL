using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 192: danh sách layer cho các ô chọn trên tab Guro của bảng OHTA. Tab luôn mở
    /// nên phải giữ ĐÚNG layer người dùng đã lưu, kể cả khi bản vẽ đang mở không có nó;
    /// còn layer GRO thật sự dùng thì lisp chọn lại mỗi lần chạy (gro--tham-so-chay),
    /// cùng luật với hàm này.
    /// </summary>
    public static class GuroTabLayer
    {
        /// <summary>Tên giả của mục "Tự động" trong ô layer gốc — không phải layer thật.</summary>
        public const string TenTuDong = "";

        public sealed class KetQua
        {
            public List<LayerMuc> Ds { get; set; }
            /// <summary>Mục đang chọn (luôn là một phần tử của Ds).</summary>
            public LayerMuc Chon { get; set; }
            /// <summary>Layer GRO sẽ dùng trên bản vẽ này.</summary>
            public string GroDung { get; set; }
            /// <summary>Khác null khi layer đã lưu không có trên bản vẽ.</summary>
            public string CanhBao { get; set; }
        }

        /// <summary>
        /// Ô layer khung bao / vùng. Đã lưu và bản vẽ có -> chọn nó. Chưa lưu -> layer
        /// đứng đầu (như hộp thoại). Đã lưu mà bản vẽ không có -> thêm một mục đứng đầu
        /// "(không có trong bản vẽ)" để không mất lựa chọn; GRO dùng layer đứng đầu.
        /// </summary>
        public static KetQua ChoKhung(List<LayerMuc> ds, string daLuu)
        {
            ds = ds ?? new List<LayerMuc>();
            string dau = ds.Count > 0 ? ds[0].Ten : null;
            LayerMuc co = Tim(ds, daLuu);
            if (string.IsNullOrEmpty(daLuu) || co != null)
            {
                LayerMuc chon = co ?? (ds.Count > 0 ? ds[0] : null);
                return new KetQua { Ds = ds, Chon = chon, GroDung = chon?.Ten };
            }
            return ThieuLayer(ds, daLuu, dau);
        }

        /// <summary>
        /// Ô layer gốc (xử lý nét). Mục đầu là "Tự động" = layer nhiều đối tượng nhất
        /// (mục 75). Chọn tay thì GRO dùng đúng layer đó, kể cả khi ít đối tượng hơn.
        /// </summary>
        public static KetQua ChoXln(List<LayerMuc> ds, bool tuDong, string daLuu)
        {
            ds = ds ?? new List<LayerMuc>();
            string dau = ds.Count > 0 ? ds[0].Ten : null;
            var tuDongMuc = new LayerMuc(TenTuDong,
                                         "Tự động" + (dau != null ? " (" + dau + ")" : ""),
                                         ds.Count > 0 ? ds[0].SoLuong : 0);
            var dsDu = new List<LayerMuc> { tuDongMuc };
            dsDu.AddRange(ds);

            if (tuDong || string.IsNullOrEmpty(daLuu))
                return new KetQua { Ds = dsDu, Chon = tuDongMuc, GroDung = dau };

            LayerMuc co = Tim(ds, daLuu);
            if (co != null)
                return new KetQua { Ds = dsDu, Chon = co, GroDung = co.Ten };

            KetQua thieu = ThieuLayer(ds, daLuu, dau);
            thieu.Ds.Insert(0, tuDongMuc);
            return thieu;
        }

        private static KetQua ThieuLayer(List<LayerMuc> ds, string daLuu, string dau)
        {
            var thieu = new LayerMuc(daLuu, daLuu + "  (không có trong bản vẽ)", -1);
            var dsDu = new List<LayerMuc> { thieu };
            dsDu.AddRange(ds);
            return new KetQua
            {
                Ds = dsDu,
                Chon = thieu,
                GroDung = dau,
                CanhBao = "Bản vẽ này không có layer \"" + daLuu + "\" — GRO sẽ dùng \"" + (dau ?? "?") + "\".",
            };
        }

        private static LayerMuc Tim(List<LayerMuc> ds, string ten)
        {
            if (string.IsNullOrEmpty(ten)) return null;
            foreach (LayerMuc m in ds)
                if (string.Equals(m.Ten, ten, StringComparison.Ordinal)) return m;
            return null;
        }
    }
}
