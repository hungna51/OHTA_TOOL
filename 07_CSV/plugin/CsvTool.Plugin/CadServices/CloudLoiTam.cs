using System.Collections.Generic;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.GraphicsInterface;
using Polyline = ZwSoft.ZwCAD.DatabaseServices.Polyline;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
#endif

namespace CsvTool.CadServices
{
    /// <summary>Cloud đỏ quanh đối tượng lỗi — chỉ vẽ tạm (transient), không nằm trong bản vẽ, không lưu, không in.</summary>
    public static class CloudLoiTam
    {
        private static readonly List<Polyline> _dangHien = new List<Polyline>();

        public static void Hien(Document doc, IEnumerable<List<(double X, double Y, double Bulge)>> cloud)
        {
            Xoa();
            if (doc == null) return;
            foreach (var dinh in cloud)
            {
                var p = new Polyline(dinh.Count);
                for (int i = 0; i < dinh.Count; i++)
                    p.AddVertexAt(i, new Point2d(dinh[i].X, dinh[i].Y), dinh[i].Bulge, 0, 0);
                p.Closed = true;
                p.ColorIndex = 1;
                TransientManager.CurrentTransientManager.AddTransient(p, TransientDrawingMode.DirectShortTerm, 128, new IntegerCollection());
                _dangHien.Add(p);
            }
            doc.Editor.UpdateScreen();
        }

        public static void Xoa()
        {
            foreach (Polyline p in _dangHien)
            {
                try { TransientManager.CurrentTransientManager.EraseTransient(p, new IntegerCollection()); } catch { }
                p.Dispose();
            }
            _dangHien.Clear();
        }
    }
}
