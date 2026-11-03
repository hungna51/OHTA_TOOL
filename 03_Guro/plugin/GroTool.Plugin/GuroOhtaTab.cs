using System;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
#endif
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Core;
using ZwCadPlugin.ViewModels;
using ZwCadPlugin.Views;

[assembly: CommandClass(typeof(ZwCadPlugin.GuroOhtaTab))]

namespace ZwCadPlugin
{
    /// <summary>
    /// Cửa vào của tab Guro trên bảng OHTA (mục 192, ADR 0003). OHTATOOL.dll KHÔNG tham
    /// chiếu GROTOOL.dll: nó nạp DLL này rồi gọi TaoGiaoDien bằng phản chiếu, nên chữ
    /// ký (tên lớp, tên hàm, 4 tham số chuỗi) là hợp đồng giữa hai DLL — đổi thì sửa cả
    /// ModuleViewRegistry bên OHTA.
    /// </summary>
    public class GuroOhtaTab
    {
        public static System.Windows.FrameworkElement TaoGiaoDien(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            return new GuroTabControl(new GuroTabViewModel(tieuDe, moTa, mauNhan, thuMucModule));
        }

        /// <summary>
        /// Nút "Pick nét" của tab: chọn một nét Dim mẫu, lấy màu (như hộp thoại GRO),
        /// lưu registry rồi báo tab đọc lại. Là lệnh vì palette không giữ được chuột.
        /// </summary>
        [CommandMethod("GROPICKMAU")]
        public static void PickMauNet()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            PromptEntityResult r = ed.GetEntity(new PromptEntityOptions("\nChon 1 net Dim mau (LINE / PLINE / ARC) tren ban ve: "));
            if (r.Status != PromptStatus.OK) return;
            int c = GroDialogCommands.MauCuaDoiTuong(doc.Database, r.ObjectId);
            GroRegistry.Ghi("mau_dim", c);
            GroRegistry.Ghi("auto_dim", false);
            ed.WriteMessage("\nDa nhan dien mau net Dim: " + GroText.TenMau(c));
            GuroTabViewModel.BaoDaDoiThietLap();
        }
    }
}
