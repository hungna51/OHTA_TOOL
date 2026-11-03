using System.Collections.Generic;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Runtime;
using ZwCadPlugin.CadServices;

[assembly: CommandClass(typeof(ZwCadPlugin.DatMaKhacCommands))]

namespace ZwCadPlugin
{
    /// <summary>
    /// Mục 170: bước 6g của GRO (đặt mã khắc + F-name vào trong hình tấm đơn / mirror) chạy bằng C#.
    ///
    /// Lisp gọi:  (gro_net_dat_mk "T|bango|x1,y1,x2,y2|cx1,cy1,cx2,cy2|handle-bango|h1;h2;..." ... "V|x1,y1,x2,y2" ...)
    ///   T = một tấm cần đặt: hộp hình cắt, hộp cụm (MTEXT mã khắc + TEXT F-name), handle bango,
    ///       handle các entity của cụm. Thứ tự các dòng T = thứ tự đặt (tấm trước là vật cản tấm sau).
    ///   V = hộp text khắc chữ đã đổi sang 3-KEGAKI (mục 156) — vật cản.
    /// Trả về: chuỗi báo cáo "Dat ma khac vao hinh: a/b tam (...)" (+ dòng !!!), hoặc nil.
    /// </summary>
    public class DatMaKhacCommands
    {
        [LispFunction("GRO_NET_DAT_MK")]
        public static object LispDatMk(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return null;
            try
            {
                var dong = new List<string>();
                if (rb != null)
                    foreach (TypedValue tv in rb)
                        if (tv.Value is string s) dong.Add(s);
                return DatMaKhacCad.Chay(doc, dong);
            }
            catch (System.Exception ex)
            {
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi dat ma khac vao hinh: " + ex.Message);
                return "!!! Loi dat ma khac vao hinh (.NET): " + ex.Message + " - move thu cong.";
            }
        }
    }
}
