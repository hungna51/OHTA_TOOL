using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace CsvTool.CadServices
{
    /// <summary>Một tấm đưa vào file A: khung + phép biến đổi dời / thu nhỏ cụm F-name.</summary>
    public sealed class TamFileA { public long HandleKhung; public double GocX, GocY, TiLe, DenX, DenY; }

    /// <summary>
    /// File A = bản vẽ DXF mới chỉ gồm 0-KAKOU, 3-KEGAKI, 4-SHT, 2-F-NAME của các tấm, giữ nguyên toạ độ bản vẽ S.
    /// Chỉ cụm F-name bị thu nhỏ và dời vào trong tấm; MTEXT tách thành TEXT rời. Không sửa bản vẽ S.
    /// </summary>
    public static class TaoFileACad
    {
        private static readonly string[] LopGiu = { "0-KAKOU", "3-KEGAKI", "4-SHT", PhanTichKhung.LopFname };
        private static readonly string[] LopTachChu = { "3-KEGAKI", "4-SHT" };

        private static bool Trong(string lop, string[] ds) => ds.Any(l => string.Equals(l, lop, StringComparison.OrdinalIgnoreCase));

        /// <summary>Phải gọi trong ngữ cảnh lệnh CAD (nút Quét / Xuất): TXTEXP chạy bằng Editor.Command.</summary>
        public static void Ghi(Document doc, string duong, IList<TamFileA> ds)
        {
            Database db = doc.Database;
            var ids = new ObjectIdCollection();
            var fname = new Dictionary<ObjectId, Matrix3d>();
            var chu = new List<ObjectId>();
            var netTam = new List<ObjectId>();   // nét TXTEXP tạo tạm trên bản vẽ S, xoá sau khi chép sang file A
            bool nhomUndo = false;
            // không LockDocument: lệnh CAD đang chạy đã giữ khoá, khoá thêm thì Editor.Command (TXTEXP) có thể bị từ chối
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (TamFileA t in ds)
                    {
                        Matrix3d m = Matrix3d.Displacement(new Vector3d(t.DenX - t.GocX, t.DenY - t.GocY, 0)) *
                                     Matrix3d.Scaling(t.TiLe, new Point3d(t.GocX, t.GocY, 0));
                        foreach (Entity e in QuetKhungCad.TrongKhung(tr, db, t.HandleKhung, false))
                        {
                            if (!e.Visible || !Trong(e.Layer, LopGiu) || ids.Contains(e.ObjectId) || chu.Contains(e.ObjectId)) continue;
                            // chữ khắc / chữ slit phải thành nét mới cắt được: TXTEXP rồi chép nét, không chép chữ
                            if ((e is DBText || e is MText) && Trong(e.Layer, LopTachChu)) { chu.Add(e.ObjectId); continue; }
                            ids.Add(e.ObjectId);
                            if (string.Equals(e.Layer, PhanTichKhung.LopFname, StringComparison.OrdinalIgnoreCase)) fname[e.ObjectId] = m;
                        }
                    }
                    tr.Commit();
                }
                // chép chữ + TXTEXP + xoá nét tạm gói trong 1 nhóm Undo: Undo một lần là bỏ hết, không hiện lại nét tạm
                if (chu.Count > 0) { doc.Editor.Command("_.UNDO", "_BE"); nhomUndo = true; }
                netTam.AddRange(TachChu(doc, chu));
                foreach (ObjectId id in netTam) ids.Add(id);
                if (ids.Count == 0) throw new InvalidOperationException("không có đối tượng nào để chép.");

                using (var dbA = new Database(true, true))
                {
                    ObjectId ms;
                    using (Transaction trA = dbA.TransactionManager.StartTransaction())
                    {
                        ms = ((BlockTable)trA.GetObject(dbA.BlockTableId, OpenMode.ForRead))[BlockTableRecord.ModelSpace];
                        trA.Commit();
                    }
                    var map = new IdMapping();
                    db.WblockCloneObjects(ids, ms, map, DuplicateRecordCloning.Ignore, false);
                    using (Transaction trA = dbA.TransactionManager.StartTransaction())
                    {
                        var kg = (BlockTableRecord)trA.GetObject(ms, OpenMode.ForWrite);
                        foreach (var f in fname)
                        {
                            if (!map.Contains(f.Key)) continue;
                            var e = (Entity)trA.GetObject(map[f.Key].Value, OpenMode.ForWrite);
                            e.TransformBy(f.Value);
                            if (!(e is MText mt)) continue;
                            mt.Contents = ThongTinTamParser.MtextPhang(mt.Contents);   // tách đúng 1 TEXT mỗi dòng
                            var manh = new DBObjectCollection();
                            mt.Explode(manh);
                            foreach (DBObject o in manh)
                                if (o is Entity c) { kg.AppendEntity(c); trA.AddNewlyCreatedDBObject(c, true); }
                                else o.Dispose();
                            mt.Erase();
                        }
                        // mọi đối tượng về ByLayer; đường bẻ (LINE 0-KAKOU nét DASHED*) giữ nguyên kiểu nét
                        foreach (ObjectId id in kg.Cast<ObjectId>().ToList())
                        {
                            if (id.IsErased || !(trA.GetObject(id, OpenMode.ForWrite) is Entity e)) continue;
                            e.ColorIndex = 256;
                            e.LineWeight = LineWeight.ByLayer;
                            bool laBe = e is Line && string.Equals(e.Layer, "0-KAKOU", StringComparison.OrdinalIgnoreCase) &&
                                        (e.Linetype ?? "").StartsWith("DASHED", StringComparison.OrdinalIgnoreCase);
                            if (!laBe) e.Linetype = "ByLayer";
                        }
                        trA.Commit();
                    }
                    dbA.DxfOut(duong, 16, DwgVersion.Current);
                }
            }
            finally
            {
                if (netTam.Count > 0)
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId id in netTam)
                            if (!id.IsErased) tr.GetObject(id, OpenMode.ForWrite).Erase();
                        tr.Commit();
                    }
                if (nhomUndo) doc.Editor.Command("_.UNDO", "_E");
            }
        }

        /// <summary>
        /// Như lisp xt (TXTEXP_nhieu_doi_tuong ZW): với từng chữ, chép một bản, zoom tới, TXTEXP bản chép, gom nét mới,
        /// đặt lại layer của chữ. Chữ gốc trên bản vẽ S giữ nguyên; trả về nét mới (người gọi xoá sau khi chép).
        /// ZWCAD có lệnh TXTEXP sẵn; AutoCAD chỉ có khi cài Express Tools.
        /// </summary>
        private static List<ObjectId> TachChu(Document doc, List<ObjectId> chu)
        {
            var kq = new List<ObjectId>();
            if (chu.Count == 0) return kq;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            ViewTableRecord nhin = ed.GetCurrentView();
            object echo = CadApplication.GetSystemVariable("CMDECHO");
            CadApplication.SetSystemVariable("CMDECHO", 0);
            try
            {
                foreach (ObjectId goc in chu)
                {
                    ObjectId ban; string lop; HashSet<ObjectId> truoc;
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        var e = (Entity)tr.GetObject(goc, OpenMode.ForRead);
                        lop = e.Layer;
                        var kg = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        var c = (Entity)e.Clone();
                        ban = kg.AppendEntity(c);
                        tr.AddNewlyCreatedDBObject(c, true);
                        truoc = new HashSet<ObjectId>(kg.Cast<ObjectId>());
                        tr.Commit();
                    }
                    try
                    {
                        ed.Command("_.ZOOM", "_O", SelectionSet.FromObjectIds(new[] { ban }), "");
                        ed.Command("_.TXTEXP", SelectionSet.FromObjectIds(new[] { ban }), "");
                    }
                    catch (Exception ex) { throw new InvalidOperationException("không chạy được TXTEXP (" + ex.Message + ")."); }
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        var kg = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                        var moi = kg.Cast<ObjectId>().Where(id => !truoc.Contains(id) && !id.IsErased).ToList();
                        foreach (ObjectId id in moi)
                            if (tr.GetObject(id, OpenMode.ForWrite) is Entity n) { n.Layer = lop; n.ColorIndex = 256; n.LineWeight = LineWeight.ByLayer; n.Linetype = "ByLayer"; }
                        if (!ban.IsErased) tr.GetObject(ban, OpenMode.ForWrite).Erase();
                        tr.Commit();
                        kq.AddRange(moi);
                        if (moi.Count == 0)
                        {
                            string noi = ((Entity)tr.GetObject(goc, OpenMode.ForRead)) is MText m ? m.Text : ((DBText)tr.GetObject(goc, OpenMode.ForRead)).TextString;
                            throw new InvalidOperationException("TXTEXP không tạo ra nét cho chữ \"" + noi + "\" trên " + lop + ". Thử gõ TXTEXP trên dòng lệnh xem có chạy không.");
                        }
                    }
                }
            }
            catch
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId id in kq) if (!id.IsErased) tr.GetObject(id, OpenMode.ForWrite).Erase();
                    tr.Commit();
                }
                throw;
            }
            finally
            {
                try { ed.SetCurrentView(nhin); } catch (Exception) { }
                CadApplication.SetSystemVariable("CMDECHO", echo);
            }
            return kq;
        }
    }
}
