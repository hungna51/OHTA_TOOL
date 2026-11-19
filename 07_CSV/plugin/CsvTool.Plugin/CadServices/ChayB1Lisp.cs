using System.Globalization;
using System.IO;
using CsvTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace CsvTool.CadServices
{
    /// <summary>
    /// Nút "Xuất thông tin tấm" (B1) giai đoạn 1: chạy chế độ 3 của lisp xuat_csv cũ qua csvtool_b1.lsp.
    /// </summary>
    public static class ChayB1Lisp
    {
        private const string KhoaDaNap = "CSVTOOL_DA_NAP_LISP_B1";

#if ZWCAD
        private const string FileLisp = "xuat_csv_zwcad.lsp";
#else
        private const string FileLisp = "xuat_csv_autocad.lsp";
#endif

        public static void Chay(CaiDatCsv c, string thuMucModule)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // shortcut: lisp cũ đọc cài đặt ở HKCU\Software\Autodesk — bỏ ở giai đoạn 2 (B1 sang C#)
            CaiDatRegistry.GhiKhoaCu("spline_precision", c.BuocSpline.ToString(CultureInfo.InvariantCulture));
            CaiDatRegistry.GhiKhoaCu("gap_tolerance", c.DungSaiKheHo.ToString(CultureInfo.InvariantCulture));
            CaiDatRegistry.GhiKhoaCu("calc_mode", c.CheDoTinh);
            CaiDatRegistry.GhiKhoaCu("zoom_on_error", 0);
            CaiDatRegistry.GhiKhoaCu("auto_compare", 0);

            bool daNap = false;
            try { daNap = doc.UserData?[KhoaDaNap] is bool b && b; } catch { }
            if (!daNap)
            {
                string thu = Path.Combine(thuMucModule, "lisp");
                string nap = "(load \"" + DuongLisp(Path.Combine(thu, FileLisp)) + "\") (load \"" +
                             DuongLisp(Path.Combine(thu, "csvtool_b1.lsp")) + "\") (princ)";
                doc.SendStringToExecute("\x03\x03" + nap + "\n", true, false, false);
                try { if (doc.UserData != null) doc.UserData[KhoaDaNap] = true; } catch { }
            }
            string lenh = "(csvtool-b1 \"" + c.LopKhungDung + "\" \"" + c.LopTapuDung + "\")";
            doc.SendStringToExecute((daNap ? "\x03\x03" : "") + lenh + "\n", true, false, true);
        }

        private static string DuongLisp(string p) => p.Replace('\\', '/');
    }
}
