using System;
using System.Collections.Generic;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 194: ô chọn layer trên tab Guro. Layer chọn RIÊNG từng bản vẽ (nhớ khi bản vẽ
    /// đang mở); chưa chọn thì lấy mặc định. Lisp gro--tham-so-chay chọn mặc định theo
    /// đúng luật này khi chạy:
    ///   khung bao : layer có ÍT khung nhất (khung kín, đúng 4 đỉnh)
    ///   vùng      : layer có NHIỀU khung nhất (kín, từ 3 đỉnh)
    ///   layer gốc : layer nhiều đối tượng nhất (mục 75)
    /// Bằng số lượng thì theo tên.
    /// </summary>
    public static class GuroTabLayer
    {
        /// <summary>
        /// Danh sách ô khung bao: layer ít khung đứng đầu. Không lọc được khung nào
        /// (coLoc = false, danh sách là mọi layer theo tên) thì giữ nguyên.
        /// </summary>
        public static List<LayerMuc> KhungItTruoc(List<LayerMuc> ds, bool coLoc)
        {
            if (ds == null) return new List<LayerMuc>();
            if (!coLoc) return ds;
            return ds.OrderBy(m => m.SoLuong).ThenBy(m => m.Ten, StringComparer.Ordinal).ToList();
        }

        /// <summary>Layer đã chọn cho bản vẽ nếu còn trong danh sách, không thì mục đầu.</summary>
        public static LayerMuc Chon(List<LayerMuc> ds, string daChon)
        {
            if (ds == null || ds.Count == 0) return null;
            if (!string.IsNullOrEmpty(daChon))
                foreach (LayerMuc m in ds)
                    if (string.Equals(m.Ten, daChon, StringComparison.Ordinal)) return m;
            return ds[0];
        }
    }

    /// <summary>Tên layer khung tấm người dùng đặt trong ⚙ Cài đặt GURO (mục 194).</summary>
    public static class TenKhungTam
    {
        public const string MacDinh = "Khung_IN";

        /// <summary>Ký tự CAD không cho trong tên layer.</summary>
        private const string KyTuCam = "<>/\\\":;?*|=,`";

        /// <summary>Bỏ khoảng trắng hai đầu; rỗng thì Khung_IN.</summary>
        public static string ChuanHoa(string ten)
        {
            string t = (ten ?? "").Trim();
            return t.Length == 0 ? MacDinh : t;
        }

        /// <summary>Câu báo lỗi nếu tên có ký tự CAD không cho, null nếu được.</summary>
        public static string Loi(string ten)
        {
            string t = ChuanHoa(ten);
            foreach (char c in t)
                if (KyTuCam.IndexOf(c) >= 0)
                    return "Tên layer không được có ký tự  " + c + "  (cấm: < > / \\ \" : ; ? * | = , `).";
            return null;
        }
    }
}
