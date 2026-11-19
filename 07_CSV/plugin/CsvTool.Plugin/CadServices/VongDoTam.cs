#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.GraphicsInterface;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
#endif

namespace CsvTool.CadServices
{
    /// <summary>Vòng tròn đỏ đánh dấu tấm lỗi — chỉ vẽ tạm (transient), không nằm trong bản vẽ, không lưu.</summary>
    public static class VongDoTam
    {
        private static Circle _vong;

        public static void Hien(Document doc, double cx, double cy, double banKinh)
        {
            Xoa();
            if (doc == null || banKinh <= 0) return;
            _vong = new Circle(new Point3d(cx, cy, 0), Vector3d.ZAxis, banKinh) { ColorIndex = 1 };
            TransientManager.CurrentTransientManager.AddTransient(_vong, TransientDrawingMode.DirectShortTerm, 128, new IntegerCollection());
            doc.Editor.UpdateScreen();
        }

        public static void Xoa()
        {
            if (_vong == null) return;
            try { TransientManager.CurrentTransientManager.EraseTransient(_vong, new IntegerCollection()); } catch { }
            _vong.Dispose();
            _vong = null;
        }
    }
}
