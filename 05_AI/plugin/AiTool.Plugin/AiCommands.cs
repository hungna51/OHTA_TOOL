using System;
using System.IO;
using AiTool.CadServices;
using AiTool.ViewModels;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

[assembly: CommandClass(typeof(AiTool.AiCommands))]

namespace AiTool
{
    /// <summary>Lệnh của tab "Tạo dữ liệu DXF từ ảnh". Dòng lệnh viết không dấu (ZWCAD không chắc hiện được dấu).</summary>
    public class AiCommands
    {
        /// <summary>Chèn DXF của lần thử đang chọn trên tab: bấm điểm = góc dưới trái biên dạng.</summary>
        [CommandMethod("OHTADXFCHEN")]
        public void ChenVaoBanVe()
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            string dxf = AiTabViewModel.HienTai?.DxfDangChon;
            if (dxf == null || !File.Exists(dxf))
            {
                ed.WriteMessage("\n[DXF] Chua co DXF: chon mot lan thu da ve xong tren tab (OHTA -> tab AI).");
                return;
            }
            PromptPointResult p = ed.GetPoint("\n[DXF] Diem dat goc duoi trai bien dang: ");
            if (p.Status != PromptStatus.OK) return;
            try
            {
                ChenDxf.Chen(doc, dxf, p.Value.TransformBy(ed.CurrentUserCoordinateSystem));
                ed.Regen();
                ed.WriteMessage("\n[DXF] Da chen " + Path.GetFileName(dxf) + " - tu them F-name.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[DXF] Khong chen duoc: " + ex.Message);
            }
        }
    }
}
