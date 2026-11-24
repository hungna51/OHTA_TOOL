using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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

        public static void Ghi(Document doc, string duong, IList<TamFileA> ds)
        {
            Database db = doc.Database;
            var ids = new ObjectIdCollection();
            var fname = new Dictionary<ObjectId, Matrix3d>();
            using (doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (TamFileA t in ds)
                    {
                        Matrix3d m = Matrix3d.Displacement(new Vector3d(t.DenX - t.GocX, t.DenY - t.GocY, 0)) *
                                     Matrix3d.Scaling(t.TiLe, new Point3d(t.GocX, t.GocY, 0));
                        foreach (Entity e in QuetKhungCad.TrongKhung(tr, db, t.HandleKhung, false))
                        {
                            if (!e.Visible || !LopGiu.Any(l => string.Equals(l, e.Layer, StringComparison.OrdinalIgnoreCase)) || ids.Contains(e.ObjectId)) continue;
                            ids.Add(e.ObjectId);
                            if (string.Equals(e.Layer, PhanTichKhung.LopFname, StringComparison.OrdinalIgnoreCase)) fname[e.ObjectId] = m;
                        }
                    }
                    tr.Commit();
                }
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
                            var manh = new DBObjectCollection();
                            mt.Explode(manh);
                            foreach (DBObject o in manh)
                                if (o is Entity c) { kg.AppendEntity(c); trA.AddNewlyCreatedDBObject(c, true); }
                                else o.Dispose();
                            mt.Erase();
                        }
                        trA.Commit();
                    }
                    dbA.DxfOut(duong, 16, DwgVersion.Current);
                }
            }
        }
    }
}
