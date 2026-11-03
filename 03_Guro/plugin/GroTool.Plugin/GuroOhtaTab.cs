using System;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
#endif
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
        /// Mục 194: (gro_net_layer_ban_ve) -> ("khung" "vung" "goc") — layer người dùng chọn
        /// trên tab Guro cho BẢN VẼ ĐANG MỞ; phần tử nil = chưa chọn, lisp dùng mặc định.
        /// </summary>
        [LispFunction("GRO_NET_LAYER_BAN_VE")]
        public static ResultBuffer LayerBanVe(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            var kq = new ResultBuffer();
            foreach (string t in LayerTheoBanVe.Lay(doc))
                kq.Add(string.IsNullOrEmpty(t)
                    ? new TypedValue((int)LispDataType.Nil)
                    : new TypedValue((int)LispDataType.Text, t));
            return kq;
        }
    }

    /// <summary>
    /// Layer chọn riêng từng bản vẽ (mục 194): giữ trong Document.UserData nên chỉ sống
    /// khi bản vẽ đang mở, không ghi vào file .dwg, không làm bản vẽ thành "đã sửa".
    /// </summary>
    public static class LayerTheoBanVe
    {
        public const int Khung = 0, Vung = 1, Goc = 2;
        private const string Khoa = "GRO_LAYER_BAN_VE";

        public static string[] Lay(Document doc)
        {
            if (doc?.UserData == null) return new string[3];
            return doc.UserData[Khoa] is string[] ds && ds.Length == 3 ? (string[])ds.Clone() : new string[3];
        }

        public static void Dat(Document doc, int viTri, string ten)
        {
            if (doc?.UserData == null) return;
            string[] ds = Lay(doc);
            ds[viTri] = ten;
            doc.UserData[Khoa] = ds;
        }
    }
}
