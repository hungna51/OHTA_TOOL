using System;
using System.Collections.Generic;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 225: phần trăm thanh tiến độ GRO trên tab Guro theo tên mốc (gro--moc). Trọng số = thời gian
    /// từng bước trên 130837-1 (giây, đo 11/11/2026); mốc không có trong bảng giữ nguyên phần trăm cũ.
    /// </summary>
    public static class TienDoGro
    {
        private static readonly KeyValuePair<string, double>[] Buoc =
        {
            P("khoi dong", 0.1), P("chon doi tuong", 5.3), P("xu ly net (3b)", 0.1),
            P("3b dim: phan loai net", 0.6), P("3b dim: vong kin", 0.15), P("3b dim: doc text so", 0.2),
            P("3b dim: cung bo goc", 0.05), P("3b dim: nang dim goc", 0.2), P("3b dim: khop lo / leader", 0.25),
            P("3b dim: khop dim dai", 0.1), P("3b dim: khop dim goc", 0.5), P("3b dim: hop nhat", 0.05),
            P("3b dim: khop dim (C#)", 0.3),
            P("3b: chuyen layer tung doi tuong", 0.7), P("doc phan loai text (4)", 0.2), P("ma khac + ghep so luong (4b)", 0.7),
            P("4d: cat tam gop", 0.1), P("4d: quet hinh (cat tam gop)", 0.4), P("4d: ghep hinh (Hungarian)", 0.5),
            P("4d: tim tam gop", 0.1), P("4d: cat + doi CAD", 0.7), P("tao entity", 0.2), P("dat vi tri", 0.6),
            P("tao khung Khung_IN", 0.15), P("khung: quet hinh", 0.1), P("khung: ghep hinh chinh", 0.1),
            P("khung: ghep hinh chieu", 0.1), P("khung: thu thap dim", 0.1), P("khung: dim doc text so", 1.3),
            P("khung: dim 2 net", 0.2), P("khung: dim dai", 0.13), P("khung: dim goc", 0.3),
            P("khung: dim native + net le", 0.8), P("khung: gan dim cho hinh", 0.85), P("khung: dong khung + ve Khung_IN", 0.5),
            P("khac chu", 0.25), P("danh sach chi tiet", 0.8), P("tong hop ds tam", 0.5),
            P("tao ban chup bango ben trai", 0.1), P("xep cum: quet hinh hoc", 0.1), P("xep cum: gom entity", 0.5),
            P("xep cum: cap nhat ds", 0.6), P("tach cap mirror", 1.2), P("tach va cham khung in tam don", 0.9),
            P("dat ma khac vao hinh", 1.6), P("in bao cao", 5.0),
        };

        private static KeyValuePair<string, double> P(string ten, double s) => new KeyValuePair<string, double>(ten, s);

        /// <summary>Bỏ phần đuôi động: "tao entity - bango 12" -> "tao entity", "khac chu (156)" -> "khac chu".</summary>
        public static string Chuan(string ten)
        {
            ten = ten ?? "";
            int k = ten.IndexOf(" - ", StringComparison.Ordinal);
            if (k >= 0) ten = ten.Substring(0, k);
            if (ten.EndsWith(")"))
            {
                int m = ten.LastIndexOf(" (", StringComparison.Ordinal);
                if (m > 0 && Array.FindIndex(Buoc, b => b.Key == ten) < 0) ten = ten.Substring(0, m);
            }
            return ten.Trim();
        }

        /// <summary>Phần trăm đã xong KHI BẮT ĐẦU mốc này (0..100); "ket thuc" = 100; mốc lạ -> null.</summary>
        public static int? PhanTram(string ten)
        {
            string c = Chuan(ten);
            if (c == "ket thuc") return 100;
            double tong = 0, truoc = -1;
            foreach (KeyValuePair<string, double> b in Buoc)
            {
                if (b.Key == c && truoc < 0) truoc = tong;
                tong += b.Value;
            }
            return truoc < 0 ? (int?)null : (int)Math.Round(100.0 * truoc / tong);
        }
    }
}
