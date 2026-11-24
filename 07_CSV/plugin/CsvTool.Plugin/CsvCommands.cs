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
    /// CSVTHIETLAP: lệnh CSV gọi để mở tab Home. CSVQUETVUNG / CSVQUETTOAN: 2 nút quét gọi,
    /// chạy trong ngữ cảnh lệnh để chọn vùng và hỏi số trên dòng lệnh được.
    /// Phân tích hình học bằng C#, kết quả lưu vào data CSV, không thay đổi bản vẽ.
    /// </summary>
    public class CsvCommands
    {
        [CommandMethod("CSVTHIETLAP", CommandFlags.UsePickSet)]
        public void ThietLap()
        {
            Chay((vm, doc) => vm.MoThietLap(doc));
        }

        [CommandMethod("CSVQUETVUNG", CommandFlags.UsePickSet)]
        public void QuetVung() => Chay((vm, doc) => vm.QuetTuLenh(doc, false));

        [CommandMethod("CSVQUETTOAN", CommandFlags.UsePickSet)]
        public void QuetToan() => Chay((vm, doc) => vm.QuetTuLenh(doc, true));

        [CommandMethod("CSVXUATA")]
        public void XuatA() => Chay((vm, doc) => vm.XuatNgay(true, false));

        [CommandMethod("CSVXUATPDF")]
        public void XuatPdf() => Chay((vm, doc) => vm.XuatNgay(false, true));

        private static void Chay(System.Action<CsvTabViewModel, Document> viec)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            CsvTabViewModel vm = CsvTabViewModel.HienTai;
            if (vm == null)
            {
                doc.Editor.WriteMessage("\n[CSV] Mo tab CSV tren bang OHTA truoc (lenh CSV).");
                return;
            }
            try { viec(vm, doc); }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\n[CSV] Loi doc ban ve: " + ex.Message); }
        }
    }
}
