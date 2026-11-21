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
    /// Lệnh CSV và nút Quét... gọi lệnh này: chạy trong ngữ cảnh lệnh để chọn vùng và hỏi số trên dòng lệnh được.
    /// Phân tích hình học bằng C#, kết quả lưu vào data CSV, không thay đổi bản vẽ.
    /// </summary>
    public class CsvCommands
    {
        [CommandMethod("CSVTHIETLAP", CommandFlags.UsePickSet)]
        public void ThietLap()
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            CsvTabViewModel vm = CsvTabViewModel.HienTai;
            if (vm == null)
            {
                doc.Editor.WriteMessage("\n[CSV] Mo tab CSV tren bang OHTA truoc (lenh CSV).");
                return;
            }
            try { vm.MoThietLap(doc); }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\n[CSV] Loi doc ban ve: " + ex.Message); }
        }
    }
}
