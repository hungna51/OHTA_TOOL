using System;
using System.Collections.Generic;
using AiTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
#endif

namespace AiTool.CadServices
{
    /// <summary>
    /// Đọc ModelSpace (chỉ đọc, không sửa gì) thành mô hình thuần: khung = polyline kín đúng 4 đỉnh
    /// trên layer khung; mọi entity khác đổi sang DoiTuong rồi GomKhung chia theo khung.
    /// </summary>
    public static class QuetKhung
    {
        private const double RadSangDo = 180.0 / Math.PI;

        public static List<KhungDuLieu> Quet(Document doc, string layerKhung, Extents3d? vung)
        {
            var khung = new List<(double MinX, double MinY, double MaxX, double MaxY)>();
            var doiTuong = new List<DoiTuong>();
            Database db = doc.Database;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    if (!(tr.GetObject(id, OpenMode.ForRead, false) is Entity ent))
                        continue;
                    Extents3d ext;
                    try { ext = ent.GeometricExtents; }
                    catch (Exception) { continue; } // text rỗng, entity lỗi... -> bỏ

                    if (LaKhung(ent, layerKhung))
                    {
                        if (!vung.HasValue || NamTrong(ext, vung.Value))
                            khung.Add((ext.MinPoint.X, ext.MinPoint.Y, ext.MaxPoint.X, ext.MaxPoint.Y));
                        continue; // polyline khung không bao giờ là đối tượng gửi đi
                    }

                    DoiTuong d = Doi(ent);
                    if (d == null) continue;
                    d.Layer = ent.Layer;
                    d.MinX = ext.MinPoint.X; d.MinY = ext.MinPoint.Y;
                    d.MaxX = ext.MaxPoint.X; d.MaxY = ext.MaxPoint.Y;
                    if (d.Loai == LoaiDoiTuong.Dim && double.IsNaN(d.X1))
                    {
                        d.X1 = d.MinX; d.Y1 = d.MinY; d.X2 = d.MaxX; d.Y2 = d.MaxY;
                    }
                    doiTuong.Add(d);
                }
                tr.Commit();
            }
            return GomKhung.Gom(khung, doiTuong);
        }

        private static bool LaKhung(Entity ent, string layerKhung) =>
            ent is Polyline pl && pl.Closed && pl.NumberOfVertices == 4 &&
            string.Equals(ent.Layer, layerKhung, StringComparison.OrdinalIgnoreCase);

        private static bool NamTrong(Extents3d e, Extents3d v) =>
            e.MinPoint.X >= v.MinPoint.X && e.MinPoint.Y >= v.MinPoint.Y &&
            e.MaxPoint.X <= v.MaxPoint.X && e.MaxPoint.Y <= v.MaxPoint.Y;

        private static DoiTuong Doi(Entity ent)
        {
            switch (ent)
            {
                case Line l:
                    return new DoiTuong { Loai = LoaiDoiTuong.Line, X1 = l.StartPoint.X, Y1 = l.StartPoint.Y, X2 = l.EndPoint.X, Y2 = l.EndPoint.Y };
                case Arc a:
                    return new DoiTuong
                    {
                        Loai = LoaiDoiTuong.Arc, Cx = a.Center.X, Cy = a.Center.Y, R = a.Radius,
                        GocDau = a.StartAngle * RadSangDo, GocCuoi = a.EndAngle * RadSangDo
                    };
                case Circle c:
                    return new DoiTuong { Loai = LoaiDoiTuong.Circle, Cx = c.Center.X, Cy = c.Center.Y, R = c.Radius };
                case Polyline p:
                    return DoiPolyline(p);
                case DBText t:
                    return new DoiTuong { Loai = LoaiDoiTuong.Text, X1 = t.Position.X, Y1 = t.Position.Y, NoiDung = t.TextString, ChieuCao = t.Height };
                case MText m:
                    return new DoiTuong { Loai = LoaiDoiTuong.Text, X1 = m.Location.X, Y1 = m.Location.Y, NoiDung = m.Contents, ChieuCao = m.TextHeight };
                case Dimension dim:
                    return DoiDim(dim);
                case MLeader ml when ml.ContentType == ContentType.MTextContent && ml.MText != null:
                    // Ghi chú tiếng Nhật (タップ M6, 逆図, 曲げ…) hay ghi bằng MLeader
                    MText mt = ml.MText;
                    return new DoiTuong { Loai = LoaiDoiTuong.Text, X1 = mt.Location.X, Y1 = mt.Location.Y, NoiDung = mt.Contents, ChieuCao = mt.TextHeight };
                default:
                    return null;
            }
        }

        private static DoiTuong DoiPolyline(Polyline p)
        {
            var d = new DoiTuong { Loai = LoaiDoiTuong.Polyline, Kin = p.Closed, Doan = new List<DoanPl>() };
            int n = p.NumberOfVertices;
            int soDoan = p.Closed ? n : n - 1;
            for (int i = 0; i < soDoan; i++)
            {
                Point2d a = p.GetPoint2dAt(i), b = p.GetPoint2dAt((i + 1) % n);
                d.Doan.Add(HinhHoc.TuBulge(a.X, a.Y, b.X, b.Y, p.GetBulgeAt(i)));
            }
            return d.Doan.Count == 0 ? null : d;
        }

        private static DoiTuong DoiDim(Dimension dim)
        {
            var d = new DoiTuong { Loai = LoaiDoiTuong.Dim, LoaiDim = dim.GetType().Name };
            try { d.GiaTriDo = dim.Measurement; } catch (Exception) { }
            string ghiDe = dim.DimensionText;
            if (!string.IsNullOrEmpty(ghiDe) && ghiDe != "<>") d.GhiDe = ghiDe;

            Point3d? p1 = null, p2 = null;
            switch (dim)
            {
                case RotatedDimension r: p1 = r.XLine1Point; p2 = r.XLine2Point; break;
                case AlignedDimension al: p1 = al.XLine1Point; p2 = al.XLine2Point; break;
                case RadialDimension rd: p1 = rd.Center; p2 = rd.ChordPoint; break;
                case DiametricDimension dd: p1 = dd.FarChordPoint; p2 = dd.ChordPoint; break;
            }
            if (p1.HasValue && p2.HasValue)
            {
                d.X1 = p1.Value.X; d.Y1 = p1.Value.Y; d.X2 = p2.Value.X; d.Y2 = p2.Value.Y;
            }
            else
            {
                // Kiểu dim khác: hai góc hộp bao; gán sau khi biết extents ở Quet() -> đánh dấu bằng NaN
                d.X1 = d.Y1 = d.X2 = d.Y2 = double.NaN;
            }
            return d;
        }
    }
}
