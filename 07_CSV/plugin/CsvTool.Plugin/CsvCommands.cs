using CsvTool.ViewModels;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Runtime;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

[assembly: CommandClass(typeof(CsvTool.CsvCommands))]

namespace CsvTool
{
    /// <summary>
    /// CSVQUET: lisp gọi khi phân tích hình học xong — đọc file kết quả, lưu vào data CSV
    /// rồi làm mới danh sách tấm. Không thay đổi bản vẽ.
    /// </summary>
    public class CsvCommands
    {
        [CommandMethod("CSVQUET")]
        public void Quet()
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            CsvTabViewModel vm = CsvTabViewModel.HienTai;
            if (vm == null)
            {
                doc.Editor.WriteMessage("\n[CSV] Mo tab CSV tren bang OHTA truoc (lenh CSV).");
                return;
            }
            try { vm.SauQuet(doc); }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\n[CSV] Loi doc ban ve: " + ex.Message); }
        }
    }
}
