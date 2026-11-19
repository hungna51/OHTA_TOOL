using CsvTool.CadServices;
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
    /// Lệnh nút "Làm mới" / "EXPORT" của tab CSV gửi qua SendStringToExecute: chọn khung cần ngữ cảnh
    /// lệnh (UsePickSet = nhận cả khung chọn sẵn như ssget "_I"). Thông báo dòng lệnh viết không dấu.
    /// </summary>
    public class CsvCommands
    {
        [CommandMethod("CSVLAMMOI", CommandFlags.UsePickSet)]
        public void LamMoi() => Chay(false);

        [CommandMethod("CSVEXPORT", CommandFlags.UsePickSet)]
        public void Export() => Chay(true);

        private static void Chay(bool export)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            CsvTabViewModel vm = CsvTabViewModel.HienTai;
            if (vm == null)
            {
                doc.Editor.WriteMessage("\n[CSV] Mo tab CSV tren bang OHTA truoc (lenh CSV).");
                return;
            }
            try
            {
                var ids = QuetKhungCad.ChonKhung(doc, vm.CaiDat.LopKhungDung);
                if (ids.Count == 0)
                {
                    doc.Editor.WriteMessage("\n[CSV] Khong co khung in hop le duoc chon.");
                    return;
                }
                var (khung, dimzin) = QuetKhungCad.Quet(doc, ids);
                vm.NhanKetQua(doc, khung, dimzin, export);
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("\n[CSV] Loi doc ban ve: " + ex.Message);
            }
        }
    }
}
