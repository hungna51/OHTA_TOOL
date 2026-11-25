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
    /// Chèn DXF Thử nhanh vẽ (chỉ hình + dim, không F-name / Khung_IN) vào Model của bản vẽ đang mở. Mẫu ve_dxf luôn đặt tấm
    /// từ gốc (0,0) nên dời gốc → điểm chọn: góc dưới trái biên dạng nằm đúng điểm bấm. Layer / kiểu trùng tên giữ bản của bản vẽ.
    /// </summary>
    public static class ChenDxf
    {
        public static void Chen(Document doc, string dxf, Point3d diemWcs)
        {
            using (doc.LockDocument())
            using (var nguon = new Database(false, true))
            {
                nguon.DxfIn(dxf, null);
                doc.Database.Insert(Matrix3d.Displacement(diemWcs - Point3d.Origin), nguon, false);
            }
        }
    }
}
