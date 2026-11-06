using System.Collections.Generic;
using System.Linq;

namespace AiTool.Core
{
    public static class GomKhung
    {
        /// <summary>
        /// Gán đối tượng vào khung theo tâm hộp bao (nằm trên cạnh vẫn tính là trong); khung chồng
        /// nhau thì đối tượng vào mọi khung chứa nó. Đối tượng được sao chép rồi đổi gốc về góc
        /// dưới trái khung. Khung đánh số 1.. theo thứ tự trên xuống (MaxY giảm), trái sang (MinX tăng).
        /// </summary>
        public static List<KhungDuLieu> Gom(IList<(double MinX, double MinY, double MaxX, double MaxY)> khung, IList<DoiTuong> doiTuong)
        {
            var kq = new List<KhungDuLieu>();
            int so = 0;
            // Khung bao trùm trọn một khung khác (vd. viền bản vẽ cùng layer) không phải một tấm:
            // bỏ, để không gửi cả bản vẽ thành một khung khổng lồ.
            var tam = khung.Where(k => !khung.Any(c => !c.Equals(k) &&
                c.MinX >= k.MinX && c.MinY >= k.MinY && c.MaxX <= k.MaxX && c.MaxY <= k.MaxY)).ToList();
            foreach (var k in tam.OrderByDescending(k => k.MaxY).ThenBy(k => k.MinX))
            {
                var kd = new KhungDuLieu { SoThuTu = ++so, MinX = k.MinX, MinY = k.MinY, MaxX = k.MaxX, MaxY = k.MaxY };
                foreach (DoiTuong d in doiTuong)
                {
                    double tx = d.TamX, ty = d.TamY;
                    if (tx >= k.MinX && tx <= k.MaxX && ty >= k.MinY && ty <= k.MaxY)
                        kd.DoiTuong.Add(DoiGoc(d, k.MinX, k.MinY));
                }
                kq.Add(kd);
            }
            return kq;
        }

        private static DoiTuong DoiGoc(DoiTuong d, double ox, double oy)
        {
            var m = new DoiTuong
            {
                Loai = d.Loai, Layer = d.Layer, KieuNet = d.KieuNet, LaMText = d.LaMText,
                MinX = d.MinX - ox, MinY = d.MinY - oy, MaxX = d.MaxX - ox, MaxY = d.MaxY - oy,
                X1 = d.X1 - ox, Y1 = d.Y1 - oy, X2 = d.X2 - ox, Y2 = d.Y2 - oy,
                Cx = d.Cx - ox, Cy = d.Cy - oy, R = d.R, GocDau = d.GocDau, GocCuoi = d.GocCuoi,
                Kin = d.Kin, NoiDung = d.NoiDung, ChieuCao = d.ChieuCao,
                LoaiDim = d.LoaiDim, GiaTriDo = d.GiaTriDo, GhiDe = d.GhiDe,
            };
            if (d.Doan != null)
            {
                m.Doan = d.Doan.Select(x =>
                {
                    DoanPl s = x.Sao();
                    s.X1 -= ox; s.Y1 -= oy; s.X2 -= ox; s.Y2 -= oy;
                    if (s.LaCung) { s.Cx -= ox; s.Cy -= oy; }
                    return s;
                }).ToList();
            }
            return m;
        }
    }
}
