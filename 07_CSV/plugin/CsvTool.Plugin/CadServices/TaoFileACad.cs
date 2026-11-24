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
    public sealed class TamFileA
    {
        public long HandleKhung; public double GocX, GocY, TiLe, DenX, DenY;
        /// <summary>Các dòng F-name đọc từ bản vẽ S: file A phải có đúng các dòng này.</summary>
        public List<string> DongFname = new List<string>();
    }

    /// <summary>
    /// File A = bản vẽ DXF mới chỉ gồm 0-KAKOU, 3-KEGAKI, 4-SHT, 2-F-NAME của các tấm, giữ nguyên toạ độ bản vẽ S.
    /// Chỉ cụm F-name bị thu nhỏ và dời vào trong tấm; MTEXT tách thành TEXT rời. Không sửa bản vẽ S.
    /// </summary>
    public static class TaoFileACad
    {
        private static readonly string[] LopGiu = { "0-KAKOU", "3-KEGAKI", "4-SHT", PhanTichKhung.LopFname };
        private static readonly string[] LopTachChu = { "3-KEGAKI", "4-SHT" };

        private static bool Trong(string lop, string[] ds) => ds.Any(l => string.Equals(l, lop, StringComparison.OrdinalIgnoreCase));

        /// <summary>Loại được vào file A (trước khi explode): nét và chữ. Block, hatch, dim… trên 4 layer là lỗi.</summary>
        private static bool HopLe(Entity e) => e is Line || e is Arc || e is Circle || e is Ellipse || e is Spline || e is Polyline ||
                                               e is Polyline2d || e is Polyline3d || e is DBText || e is MText || e is DBPoint;

        /// <summary>Dạng đơn của file A: chỉ còn các loại này.</summary>
        private static bool LaDon(Entity e) => e is Line || e is Arc || e is Circle || e is Ellipse || e is Spline || e is DBText || e is DBPoint;

        /// <summary>
        /// Phải gọi trong ngữ cảnh lệnh CAD (nút Quét / Xuất): TXTEXP chạy bằng Editor.Command.
        /// Trả về lỗi theo tấm (đối tượng không hợp lệ, F-name sai); có lỗi thì không ghi file.
        /// </summary>
        public static List<LoiQuet> Ghi(Document doc, string duong, IList<TamFileA> ds)
        {
            Database db = doc.Database;
            var loi = new List<LoiQuet>();
            var ids = new ObjectIdCollection();
            var fname = new Dictionary<ObjectId, (Matrix3d M, TamFileA Tam)>();
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
                            if (!HopLe(e))
                            {
                                var hop = (0.0, 0.0, 0.0, 0.0);
                                try { var x = e.GeometricExtents; hop = (x.MinPoint.X, x.MinPoint.Y, x.MaxPoint.X, x.MaxPoint.Y); } catch (Exception) { }
                                loi.Add(new LoiQuet
                                {
                                    Handle = t.HandleKhung, LaLoi = true, ViTri = { hop },
                                    NoiDung = (e is BlockReference ? "Block (INSERT)" : e.GetRXClass().DxfName) + " không được tạo trên layer " + e.Layer +
                                              ". File A chỉ nhận nét và chữ, explode hoặc chuyển layer.",
                                });
                                continue;
                            }
                            // chữ khắc / chữ slit phải thành nét mới cắt được: TXTEXP rồi chép nét, không chép chữ
                            if ((e is DBText || e is MText) && Trong(e.Layer, LopTachChu)) { chu.Add(e.ObjectId); continue; }
                            ids.Add(e.ObjectId);
                            if (string.Equals(e.Layer, PhanTichKhung.LopFname, StringComparison.OrdinalIgnoreCase)) fname[e.ObjectId] = (m, t);
                        }
                    }
                    tr.Commit();
                }
                if (loi.Count > 0) return loi;
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
                        var chuTam = new Dictionary<TamFileA, List<string>>();
                        foreach (var f in fname)
                        {
                            if (!map.Contains(f.Key)) continue;
                            if (!chuTam.TryGetValue(f.Value.Tam, out var dsChu)) chuTam[f.Value.Tam] = dsChu = new List<string>();
                            var e = (Entity)trA.GetObject(map[f.Key].Value, OpenMode.ForWrite);
                            e.TransformBy(f.Value.M);
                            if (e is DBText dt) { dsChu.Add(dt.TextString); continue; }
                            if (!(e is MText mt)) continue;
                            mt.Contents = ThongTinTamParser.MtextPhang(mt.Contents);   // tách đúng 1 TEXT mỗi dòng
                            mt.Width = 0;   // không tự xuống dòng: font đầu rộng hơn làm "M=SUS304 No.1" bị ngắt ở dấu cách
                            var manh = new DBObjectCollection();
                            mt.Explode(manh);
                            foreach (DBObject o in manh)
                                if (o is Entity c) { kg.AppendEntity(c); trA.AddNewlyCreatedDBObject(c, true); if (c is DBText ct) dsChu.Add(ct.TextString); }
                                else o.Dispose();
                            mt.Erase();
                        }
                        foreach (TamFileA t in ds)
                        {
                            string sai = PhanTichKhung.SoDongFname(t.DongFname, chuTam.TryGetValue(t, out var co) ? co : new List<string>());
                            if (sai != null) loi.Add(new LoiQuet { Handle = t.HandleKhung, LaLoi = true, NoiDung = "F-name trong file A sai: " + sai });
                        }
                        // dạng đơn: polyline (kể cả nét TXTEXP) → LINE + ARC, MTEXT còn lại → TEXT
                        for (int vong = 0; vong < 5; vong++)
                        {
                            var con = kg.Cast<ObjectId>().Where(id => !id.IsErased).Select(id => (Entity)trA.GetObject(id, OpenMode.ForWrite))
                                        .Where(x => x is Polyline || x is Polyline2d || x is Polyline3d || x is MText).ToList();
                            if (con.Count == 0) break;
                            foreach (Entity x in con)
                            {
                                var manh = new DBObjectCollection();
                                x.Explode(manh);
                                foreach (DBObject o in manh)
                                    if (o is Entity c) { kg.AppendEntity(c); trA.AddNewlyCreatedDBObject(c, true); }
                                    else o.Dispose();
                                x.Erase();
                            }
                        }
                        var conLai = kg.Cast<ObjectId>().Where(id => !id.IsErased).Select(id => trA.GetObject(id, OpenMode.ForRead) as Entity)
                                       .Where(x => x != null && !LaDon(x)).Select(x => x.GetRXClass().DxfName).Distinct().ToList();
                        if (conLai.Count > 0)
                            loi.Add(new LoiQuet { Handle = ds[0].HandleKhung, LaLoi = true, NoiDung = "File A còn đối tượng chưa đưa về dạng đơn: " + string.Join(", ", conLai) + "." });
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
                    if (loi.Count > 0) return loi;
                    dbA.DxfOut(duong, 16, DwgVersion.Current);
                }
                return loi;
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
