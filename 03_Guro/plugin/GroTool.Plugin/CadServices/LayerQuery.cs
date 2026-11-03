using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Đếm dữ liệu cho các popup layer của dialog gro. Duyệt thẳng Model Space trong cơ
    /// sở dữ liệu — KHÔNG dùng phép chọn cửa sổ, nên không phụ thuộc tầm nhìn (mục 58).
    /// Luật đếm nằm ở Core.LayerRanking để test được ngoài CAD.
    /// </summary>
    public static class LayerQuery
    {
        public sealed class KetQuaDem
        {
            /// <summary>Số khung kín, phẳng, ĐÚNG 4 đỉnh trên từng layer (khung bao).</summary>
            public Dictionary<string, int> Khung4 { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
            /// <summary>Số khung kín, phẳng, TỪ 3 đỉnh trên từng layer (vùng).</summary>
            public Dictionary<string, int> Khung3 { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
            /// <summary>Số đối tượng Model Space trên từng layer (xử lý nét).</summary>
            public Dictionary<string, int> DoiTuong { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
            /// <summary>Mọi layer trong bảng layer.</summary>
            public List<string> TatCaLayer { get; } = new List<string>();
        }

        /// <summary>
        /// Một lượt duyệt cho cả ba popup — gộp lại thay vì quét ba lần (lisp quét
        /// riêng: gro--dem-khung-layer một lần, gro--dem-doi-tuong-layer một lần).
        /// </summary>
        public static KetQuaDem Dem(Database db)
        {
            var kq = new KetQuaDem();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    kq.TatCaLayer.Add(ltr.Name);
                }

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    if (id.IsErased) continue;
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;
                    string lay = ent.Layer ?? "0";
                    Tang(kq.DoiTuong, lay);

                    if (TryDocKhung(tr, ent, out int soDinh, out bool kin, out bool phang))
                    {
                        if (LayerRanking.LaKhung(soDinh, kin, phang, true)) Tang(kq.Khung4, lay);
                        if (LayerRanking.LaKhung(soDinh, kin, phang, false)) Tang(kq.Khung3, lay);
                    }
                }
                tr.Commit();
            }
            return kq;
        }

        private static void Tang(Dictionary<string, int> d, string k)
        {
            d.TryGetValue(k, out int n);
            d[k] = n + 1;
        }

        /// <summary>
        /// Đọc một polyline như gro--pl-dinh / gro--pl-kin-p / gro--oz-p: LWPOLYLINE và
        /// POLYLINE nặng (2D và 3D). False nếu không phải polyline.
        /// </summary>
        private static bool TryDocKhung(Transaction tr, Entity ent, out int soDinh, out bool kin, out bool phang)
        {
            var dinh = new List<double[]>();
            soDinh = 0; kin = false; phang = false;

            if (ent is Polyline lw)
            {
                for (int i = 0; i < lw.NumberOfVertices; i++)
                {
                    Point2d p = lw.GetPoint2dAt(i);
                    dinh.Add(new[] { p.X, p.Y });
                }
                kin = lw.Closed;
                phang = LaPhapTuyenZ(lw.Normal);
            }
            else if (ent is Polyline2d p2)
            {
                foreach (ObjectId vid in p2)
                {
                    var v = tr.GetObject(vid, OpenMode.ForRead) as Vertex2d;
                    if (v != null) dinh.Add(new[] { v.Position.X, v.Position.Y });
                }
                kin = p2.Closed;
                phang = LaPhapTuyenZ(p2.Normal);
            }
            else if (ent is Polyline3d p3)
            {
                foreach (ObjectId vid in p3)
                {
                    var v = tr.GetObject(vid, OpenMode.ForRead) as PolylineVertex3d;
                    if (v != null) dinh.Add(new[] { v.Position.X, v.Position.Y });
                }
                kin = p3.Closed;
                // POLYLINE 3D không có group 210 nên gro--oz-p coi là phẳng - giữ nguyên.
                phang = true;
            }
            else
            {
                return false;
            }

            if (dinh.Count < 3) { soDinh = dinh.Count; return true; }
            soDinh = LayerRanking.SoDinhGon(dinh);
            return true;
        }

        private static bool LaPhapTuyenZ(Vector3d n)
        {
            return Math.Abs(n.X) < 1e-9 && Math.Abs(n.Y) < 1e-9 && Math.Abs(n.Z - 1.0) < 1e-9;
        }
    }
}
