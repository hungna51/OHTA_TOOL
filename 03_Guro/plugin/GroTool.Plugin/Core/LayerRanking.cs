using System;
using System.Collections.Generic;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>Một mục của popup layer: tên THẬT để dùng, chuỗi HIỂN THỊ để xem.</summary>
    public sealed class LayerMuc
    {
        public string Ten { get; }
        public string HienThi { get; }
        public int SoLuong { get; }

        public LayerMuc(string ten, string hienThi, int soLuong)
        {
            Ten = ten;
            HienThi = hienThi;
            SoLuong = soLuong;
        }

        public override string ToString() => HienThi;
    }

    /// <summary>
    /// Luật đếm và sắp xếp layer cho các popup của dialog gro — viết lại từ lisp
    /// (gro--dem-khung-layer, gro--layer-khung-ds, gro--layer-xln-ds). Không phụ thuộc
    /// ZWCAD.
    /// </summary>
    public static class LayerRanking
    {
        /// <summary>
        /// Số đỉnh "thật" của polyline, giống gro--pl-gon: bỏ đỉnh cuối nếu trùng đỉnh
        /// đầu (hình chữ nhật vẽ kiểu 5 đỉnh thay vì đặt cờ closed).
        /// </summary>
        public static int SoDinhGon(IList<double[]> dinh, double dungSai = 1e-6)
        {
            if (dinh == null) return 0;
            int n = dinh.Count;
            if (n > 1)
            {
                double[] a = dinh[0], b = dinh[n - 1];
                if (Math.Abs(a[0] - b[0]) <= dungSai && Math.Abs(a[1] - b[1]) <= dungSai) n--;
            }
            return n;
        }

        /// <summary>
        /// Khung bao (popup "Layer khung bao"): kín, phẳng, ĐÚNG 4 đỉnh — mục 51.
        /// Vùng (popup "Layer vùng"): kín, phẳng, TỪ 3 đỉnh — mục 61, 64.
        /// </summary>
        public static bool LaKhung(int soDinhGon, bool kin, bool phang, bool chi4)
        {
            if (!kin || !phang || soDinhGon < 3) return false;
            return chi4 ? soDinhGon == 4 : soDinhGon >= 3;
        }

        /// <summary>
        /// Sắp (số lượng GIẢM, tên TĂNG). Tên layer là duy nhất nên đây là thứ tự toàn
        /// phần — không có chuyện hai mục "bằng nhau" bị rơi mất như vl-sort (mục 22).
        /// So tên theo mã ký tự (Ordinal), giống phép &lt; trên chuỗi của AutoLISP.
        /// </summary>
        public static List<KeyValuePair<string, int>> SapXep(IEnumerable<KeyValuePair<string, int>> dem)
        {
            return dem.OrderByDescending(kv => kv.Value)
                      .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                      .ToList();
        }

        /// <summary>
        /// Danh sách cho popup khung bao / vùng. Chỉ lấy layer có ít nhất một khung.
        /// Không layer nào có khung thì trả về TẤT CẢ layer (sắp theo tên) và
        /// coLoc = false — để gọi nơi báo cho người dùng, giống lisp.
        /// </summary>
        public static List<LayerMuc> DanhSachKhung(IDictionary<string, int> soKhung,
                                                   IEnumerable<string> tatCaLayer,
                                                   out bool coLoc)
        {
            var co = SapXep(soKhung.Where(kv => kv.Value > 0));
            if (co.Count > 0)
            {
                coLoc = true;
                return co.Select(kv => new LayerMuc(kv.Key, kv.Key + "  (" + kv.Value + " khung)", kv.Value))
                         .ToList();
            }
            coLoc = false;
            var ten = tatCaLayer.OrderBy(t => t, StringComparer.Ordinal).ToList();
            if (ten.Count == 0) ten.Add("0");
            return ten.Select(t => new LayerMuc(t, t, 0)).ToList();
        }

        /// <summary>
        /// Danh sách cho popup "Layer gốc bản vẽ" (xử lý nét): MỌI layer, kèm số đối
        /// tượng (layer trống = 0), sắp số lượng giảm — giống gro--layer-xln-ds.
        /// </summary>
        public static List<LayerMuc> DanhSachXln(IDictionary<string, int> soDoiTuong,
                                                 IEnumerable<string> tatCaLayer)
        {
            var dem = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string t in tatCaLayer) dem[t] = 0;
            foreach (var kv in soDoiTuong) dem[kv.Key] = kv.Value;
            if (dem.Count == 0) dem["0"] = 0;
            return SapXep(dem).Select(kv => new LayerMuc(kv.Key, kv.Key + "  (" + kv.Value + " doi tuong)", kv.Value))
                              .ToList();
        }
    }
}
