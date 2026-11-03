using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>Một entity của ss-all-dims (chỉ LINE / ARC / DIMENSION mới cần đủ dữ liệu).</summary>
    public sealed class DimNetVao
    {
        public string Handle;
        /// <summary>Tên DXF ("LINE", "ARC", "DIMENSION", ...).</summary>
        public string Loai;
        public string Layer;
        /// <summary>Ở không gian giấy (group 67 = 1).</summary>
        public bool Giay;
        /// <summary>Thuộc cụm bango / mã khắc / F-name (all-k-ents).</summary>
        public bool LaKhoi;
        /// <summary>Hộp bao; null = gro--bbox trả nil.</summary>
        public Hop Bb;
    }

    /// <summary>
    /// Mục 176 (GĐ 5c-b): chép vòng lọc đầu (bước 1) của <c>gro--thu-thap-dim-units</c> — chọn LINE / ARC /
    /// DIMENSION trên layer Dim làm nguyên liệu gom cụm Dim. Thuần, không gọi ZWCAD.
    /// Chỉ 3 loại đó được gom nên các phép lọc của lisp chỉ cần tính cho chúng; với 3 loại này
    /// <c>gro--bango-ent-p</c> luôn sai (không có group 7 / không phải TEXT) và ename ghi chú (TEXT)
    /// không thể trùng — hai phép đó bỏ được mà kết quả không đổi.
    /// </summary>
    public static class LocDimNet
    {
        public sealed class KetQua
        {
            public List<DimNetVao> Lines = new List<DimNetVao>();
            public List<DimNetVao> Arcs = new List<DimNetVao>();
            public List<DimNetVao> Natives = new List<DimNetVao>();
        }

        private static readonly string[] LayerDauRa =
            { "2-F-NAME", "3-KEGAKI", "CANH_BAO_CO_DON", "CANH_BAO_LECH", "CANH_BAO_SO_LUONG", "CANH_BAO_KHUNG_CAT", "KHUNG_IN" };

        private static readonly string[] LayerBo = { "KHUNG_IN", "CANH_BAO", "0-KAKOU", "2-F-NAME", "3-KEGAKI" };

        /// <summary>gro--bb-trong-ghi-chu-p: tâm hộp nằm trong (nới 1.0) một hộp ghi chú.</summary>
        public static bool TrongGhiChu(Hop bb, IList<Hop> noteBbs)
        {
            if (bb == null || noteBbs == null) return false;
            double cx = (bb.X1 + bb.X2) / 2.0, cy = (bb.Y1 + bb.Y2) / 2.0;
            foreach (Hop nb in noteBbs)
                if (nb != null && cx >= nb.X1 - 1.0 && cx <= nb.X2 + 1.0 && cy >= nb.Y1 - 1.0 && cy <= nb.Y2 + 1.0)
                    return true;
            return false;
        }

        public static bool QuaLoc(DimNetVao n, IList<Hop> noteBbs)
        {
            if (n.Giay || n.LaKhoi || n.Bb == null) return false;
            string lay = PhanLoaiNet.HoaAscii(n.Layer ?? "");
            if (Array.IndexOf(LayerDauRa, lay) >= 0) return false;
            foreach (string s in LayerBo) if (lay.Contains(s)) return false;
            if (TrongGhiChu(n.Bb, noteBbs)) return false;
            return n.Bb.W < 2000.0 && n.Bb.H < 2000.0;
        }

        /// <param name="ss">ss-all-dims đúng thứ tự ssname</param>
        public static KetQua Chay(IList<DimNetVao> ss, IList<Hop> noteBbs)
        {
            var kq = new KetQua();
            foreach (DimNetVao n in ss)
            {
                List<DimNetVao> dich = n.Loai == "LINE" ? kq.Lines : n.Loai == "ARC" ? kq.Arcs : n.Loai == "DIMENSION" ? kq.Natives : null;
                if (dich != null && QuaLoc(n, noteBbs)) dich.Add(n);
            }
            kq.Lines.Reverse(); kq.Arcs.Reverse(); kq.Natives.Reverse();   // lisp cons
            return kq;
        }
    }
}
