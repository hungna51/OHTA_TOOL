using System.Collections.Generic;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.GraphicsInterface;
using DbPolyline = ZwSoft.ZwCAD.DatabaseServices.Polyline;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using DbPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
#endif

namespace ZwCadPlugin.CadServices
{
    /// <summary>Cloud đỏ tạm quanh vị trí lỗi kín hở / ngoài hình / ByLayer — không ghi vào bản vẽ, không in.</summary>
    public static class CloudLoiCad
    {
        private static readonly List<DbPolyline> _dangHien = new List<DbPolyline>();

        public static void Hien(Document doc, IEnumerable<List<(double X, double Y, double Bulge)>> clouds)
        {
            Xoa();
            if (doc == null || clouds == null) return;
            try
            {
                foreach (var dinh in clouds)
                {
                    if (dinh == null || dinh.Count < 3) continue;
                    var p = new DbPolyline(dinh.Count);
                    for (int i = 0; i < dinh.Count; i++)
                        p.AddVertexAt(i, new Point2d(dinh[i].X, dinh[i].Y), dinh[i].Bulge, 0, 0);
                    p.Closed = true;
                    p.ColorIndex = 1;
                    TransientManager.CurrentTransientManager.AddTransient(p, TransientDrawingMode.DirectShortTerm, 128, new IntegerCollection());
                    _dangHien.Add(p);
                }
                doc.Editor?.UpdateScreen();
            }
            catch
            {
                // Bỏ qua nếu CAD chưa sẵn sàng vẽ transient
            }
        }

        public static void Xoa()
        {
            foreach (DbPolyline p in _dangHien)
            {
                try { TransientManager.CurrentTransientManager.EraseTransient(p, new IntegerCollection()); } catch { }
                try { p.Dispose(); } catch { }
            }
            _dangHien.Clear();
        }
    }
}
