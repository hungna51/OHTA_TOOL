using System;
using System.Collections.Generic;
using CsvTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace CsvTool.CadServices
{
    /// <summary>Ghi ngược lên bản vẽ: sửa F-name của khung.</summary>
    public static class GhiNguocCad
    {
        /// <summary>truong ∈ N, M, T, Q: thay dòng "X=..." trong F-name (layer 2-F-NAME) của khung.</summary>
        public static void SuaFname(Document doc, long handleKhung, char truong, string giaTri) =>
            Sua(doc, handleKhung, PhanTichKhung.LopFname, (noi, laM) => PhanTichKhung.ThayTruongFname(noi, laM, truong, giaTri),
                "Không tìm thấy trường " + truong + "= trong F-name của khung.");

        private static void Sua(Document doc, long handleKhung, string lop, Func<string, bool, string> thay, string loi)
        {
            Database db = doc.Database;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (Entity e in QuetKhungCad.TrongKhung(tr, db, handleKhung))
                {
                    if (!string.Equals(e.Layer, lop, StringComparison.OrdinalIgnoreCase)) continue;
                    bool laM = e is MText;
                    string moi = thay(laM ? ((MText)e).Contents : ((DBText)e).TextString, laM);
                    if (moi == null) continue;
                    e.UpgradeOpen();
                    if (laM) ((MText)e).Contents = moi; else ((DBText)e).TextString = moi;
                    tr.Commit();
                    doc.Editor.UpdateScreen();   // gọi từ palette: không tự vẽ lại tới khi có lệnh khác
                    return;
                }
                throw new InvalidOperationException(loi);
            }
        }
    }
}
