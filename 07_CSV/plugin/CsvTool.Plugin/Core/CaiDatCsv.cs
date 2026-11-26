using System;
using System.Globalization;

namespace CsvTool.Core
{
    /// <summary>
    /// Cài đặt tab CSV. Lưu ở HKCU\Software\OFT_Lisp\CSV; máy chưa có khoá mới thì đọc 1 lần
    /// các khoá cũ của lisp ở HKCU\Software\Autodesk (csvmode, pop_1..3, ...).
    /// </summary>
    public sealed class CaiDatCsv
    {
        public const string LopKhungMacDinh = "Khung_IN", LopDimMacDinh = "1-XY", LopTapuMacDinh = "TAPU_TMT";

        public KieuSapXep SapXep = KieuSapXep.TraiSangPhai;
        public bool DungLopMacDinh = true;
        public string LopKhung = LopKhungMacDinh, LopDim = LopDimMacDinh, LopTapu = LopTapuMacDinh;
        public double BuocSpline = 1.0, DungSaiKheHo = 0.01;
        public int CheDoTinh;
        public bool KhongToiThieuKg;   // tấm tính theo kg (khối lượng × đơn giá) không áp giá tối thiểu; chỉ lưu trên máy, web để riêng

        /// <summary>Lớp thực dùng khi chạy (bật "ưu tiên layer mặc định" thì bỏ qua lớp đã lưu).</summary>
        public string LopKhungDung => DungLopMacDinh ? LopKhungMacDinh : LopKhung;
        public string LopDimDung => DungLopMacDinh ? LopDimMacDinh : LopDim;
        public string LopTapuDung => DungLopMacDinh ? LopTapuMacDinh : LopTapu;

        public static CaiDatCsv Doc(Func<string, object> docMoi, Func<string, object> docCu)
        {
            var c = new CaiDatCsv();
            if (docMoi("SapXep") != null)
            {
                c.SapXep = SapXepHopLe(SoNguyen(docMoi("SapXep"), 1));
                c.DungLopMacDinh = SoNguyen(docMoi("DungLopMacDinh"), 1) == 1;
                c.LopKhung = Chuoi(docMoi("LopKhung"), LopKhungMacDinh);
                c.LopDim = Chuoi(docMoi("LopDim"), LopDimMacDinh);
                c.LopTapu = Chuoi(docMoi("LopTapu"), LopTapuMacDinh);
                c.BuocSpline = SoThuc(docMoi("BuocSpline"), 1.0);
                c.DungSaiKheHo = SoThuc(docMoi("DungSaiKheHo"), 0.01);
                c.CheDoTinh = SoNguyen(docMoi("CheDoTinh"), 0);
                c.KhongToiThieuKg = SoNguyen(docMoi("KhongToiThieuKg"), 0) == 1;
                return c;
            }
            c.SapXep = SapXepHopLe(SoNguyen(docCu("csvmode"), 1));
            c.DungLopMacDinh = Chuoi(docCu("use_default_layers"), "1") == "1";
            c.LopKhung = LopCu(docCu("pop_1"), LopKhungMacDinh);
            c.LopDim = LopCu(docCu("pop_2"), LopDimMacDinh);
            c.LopTapu = LopCu(docCu("pop_3"), LopTapuMacDinh);
            c.BuocSpline = SoThuc(docCu("spline_precision"), 1.0);
            c.DungSaiKheHo = SoThuc(docCu("gap_tolerance"), 0.01);
            c.CheDoTinh = SoNguyen(docCu("calc_mode"), 0);
            return c;
        }

        public void Ghi(Action<string, object> ghiMoi)
        {
            ghiMoi("SapXep", (int)SapXep);
            ghiMoi("DungLopMacDinh", DungLopMacDinh ? 1 : 0);
            ghiMoi("LopKhung", LopKhung);
            ghiMoi("LopDim", LopDim);
            ghiMoi("LopTapu", LopTapu);
            ghiMoi("BuocSpline", BuocSpline.ToString(CultureInfo.InvariantCulture));
            ghiMoi("DungSaiKheHo", DungSaiKheHo.ToString(CultureInfo.InvariantCulture));
            ghiMoi("CheDoTinh", CheDoTinh);
            ghiMoi("KhongToiThieuKg", KhongToiThieuKg ? 1 : 0);
        }

        private static KieuSapXep SapXepHopLe(int n) => n >= 1 && n <= 3 ? (KieuSapXep)n : KieuSapXep.TraiSangPhai;

        // lisp lưu "0" khi chưa chọn lớp → coi như chưa có
        private static string LopCu(object v, string md) { string s = Chuoi(v, md); return s == "0" ? md : s; }

        private static string Chuoi(object v, string md) => v == null || v.ToString().Trim() == "" ? md : v.ToString().Trim();

        private static int SoNguyen(object v, int md) =>
            v is int i ? i : int.TryParse(v?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : md;

        private static double SoThuc(object v, double md) =>
            double.TryParse(v?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : md;
    }
}
