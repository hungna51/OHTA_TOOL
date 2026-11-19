using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
#endif

namespace CsvTool.CadServices
{
    /// <summary>Đọc bản vẽ thành KhungTho cho Core (thay ssget "_WP" của lisp bằng duyệt Database).</summary>
    public static class QuetKhungCad
    {
        private const double Eps = 1e-6;

        /// <summary>
        /// Chọn khung giống c:csv: lấy chọn sẵn (pickfirst) hoặc hỏi chọn *POLYLINE trên lớp khung;
        /// không được thì hỏi lại không lọc lớp. Chỉ giữ khung hợp lệ (CSV--valid-frame-p). Huỷ → rỗng.
        /// </summary>
        public static List<ObjectId> ChonKhung(Document doc, string lopKhung)
        {
            Editor ed = doc.Editor;
            bool coLop;
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                coLop = !string.IsNullOrEmpty(lopKhung) && lt.Has(lopKhung);
                tr.Commit();
            }
            // chọn sẵn trước khi bấm nút = ssget "_I" (lệnh cần CommandFlags.UsePickSet)
            PromptSelectionResult r = ed.SelectImplied();
            if (r.Status == PromptStatus.OK && r.Value != null)
            {
                ed.SetImpliedSelection(new ObjectId[0]);
                var san = HopLe(doc, r.Value.GetObjectIds(), coLop ? lopKhung : null);
                if (san.Count > 0) return san;
            }
            var kq = HopLe(doc, Chon(ed, coLop ? lopKhung : null, "\n[CSV] Chon khung in: "), null);
            if (kq.Count == 0 && coLop)
                kq = HopLe(doc, Chon(ed, null, "\n[CSV] Chua chon duoc khung. Chon lai khung in (moi layer): "), null);
            return kq;
        }

        private static ObjectId[] Chon(Editor ed, string lop, string loiNhac)
        {
            var dk = new List<TypedValue> { new TypedValue((int)DxfCode.Start, "*POLYLINE") };
            if (lop != null) dk.Add(new TypedValue((int)DxfCode.LayerName, lop));
            PromptSelectionResult r = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = loiNhac },
                                                      new SelectionFilter(dk.ToArray()));
            return r.Status == PromptStatus.OK && r.Value != null ? r.Value.GetObjectIds() : new ObjectId[0];
        }

        /// <summary>Giữ polyline đúng lớp (nếu có) và hợp lệ: ≥ 4 đỉnh, kín hoặc đỉnh đầu ≈ đỉnh cuối.</summary>
        private static List<ObjectId> HopLe(Document doc, ObjectId[] ids, string lop)
        {
            var kq = new List<ObjectId>();
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e == null || (lop != null && !string.Equals(e.Layer, lop, StringComparison.OrdinalIgnoreCase))) continue;
                    var dinh = DinhKhung(e, out bool kin);
                    if (dinh.Count >= 4 && (kin || dinh[0].GetDistanceTo(dinh[dinh.Count - 1]) < 0.1)) kq.Add(id);
                }
                tr.Commit();
            }
            return kq;
        }

        private static List<Point2d> DinhKhung(Entity e, out bool kin)
        {
            var kq = new List<Point2d>();
            kin = false;
            if (e is Polyline p)
            {
                kin = p.Closed;
                for (int i = 0; i < p.NumberOfVertices; i++) kq.Add(p.GetPoint2dAt(i));
            }
            else if (e is Polyline2d p2)
            {
                kin = p2.Closed;
                foreach (ObjectId vid in p2)
                    if (vid.GetObject(OpenMode.ForRead) is Vertex2d v) kq.Add(new Point2d(v.Position.X, v.Position.Y));
            }
            return kq;
        }

        /// <summary>Đọc từng khung đã chọn + mọi TEXT/MTEXT/DIMENSION nằm trọn trong hộp khung.</summary>
        public static (List<KhungTho> Khung, int Dimzin) Quet(Document doc, IEnumerable<ObjectId> khung)
        {
            Database db = doc.Database;
            var ketQua = new List<KhungTho>();
            int dimzin;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                dimzin = db.Dimzin;
                var doiTuong = new List<(Extents3d Hop, object Du)>();
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                foreach (ObjectId id in space)
                {
                    var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    object du = null;
                    if (e is MText mt) du = new TextTho { Lop = mt.Layer, NoiDung = mt.Contents, LaMtext = true };
                    else if (e is DBText t) du = new TextTho { Lop = t.Layer, NoiDung = t.TextString, LaMtext = false };
                    else if (e is Dimension d)
                        du = new DimTho { Lop = d.Layer, Co70 = MaDxf70(d), DoDai = d.Measurement, Dimdec = d.Dimdec };
                    if (du == null) continue;
                    Extents3d hop;
                    try { hop = e.GeometricExtents; } catch { continue; }
                    doiTuong.Add((hop, du));
                }

                foreach (ObjectId id in khung)
                {
                    var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    var dinh = DinhKhung(e, out _);
                    if (dinh.Count == 0) continue;
                    var k = new KhungTho
                    {
                        MinX = dinh.Min(p => p.X), MaxX = dinh.Max(p => p.X),
                        MinY = dinh.Min(p => p.Y), MaxY = dinh.Max(p => p.Y),
                        Handle = e.Handle.Value,
                    };
                    foreach (var (hop, du) in doiTuong)
                    {
                        if (hop.MinPoint.X < k.MinX - Eps || hop.MaxPoint.X > k.MaxX + Eps ||
                            hop.MinPoint.Y < k.MinY - Eps || hop.MaxPoint.Y > k.MaxY + Eps) continue;
                        if (du is TextTho t) k.Text.Add(t); else k.Dim.Add((DimTho)du);
                    }
                    ketQua.Add(k);
                }
                tr.Commit();
            }
            return (ketQua, dimzin);
        }

        /// <summary>Mã DXF 70 lisp lọc: dim thẳng 32, dim phi 3, còn lại mã loại khác (bị bỏ qua).</summary>
        private static int MaDxf70(Dimension d) =>
            d is RotatedDimension ? 32 : d is DiametricDimension ? 3 : d is AlignedDimension ? 33 : 0;
    }
}
