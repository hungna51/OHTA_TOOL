using System.Windows;
using TeamsTool.Views;

namespace TeamsTool
{
    /// <summary>
    /// Cửa vào của tab Teams trên bảng OHTA (ADR 0008, 0009). OHTATOOL.dll nạp TEAMSTOOL.dll rồi gọi
    /// TaoGiaoDien bằng phản chiếu (DllTabLoader) — cùng hợp đồng 4 tham số chuỗi với tab Guro / AI.
    /// Tab chỉ hiện đơn đang làm; duyệt đơn nằm ở ứng dụng OhtaTeams.exe.
    /// </summary>
    public class TeamsOhtaTab
    {
        public static FrameworkElement TaoGiaoDien(string tieuDe, string moTa, string mauNhan, string thuMucModule) =>
            new DangLamControl(tieuDe, moTa, mauNhan, thuMucModule);
    }
}
