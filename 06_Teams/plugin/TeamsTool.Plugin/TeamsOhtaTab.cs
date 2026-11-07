using System.Windows;
using TeamsTool.Views;

namespace TeamsTool
{
    /// <summary>
    /// Cửa vào của tab Teams trên bảng OHTA (ADR 0008). OHTATOOL.dll nạp TEAMSTOOL.dll rồi gọi
    /// TaoGiaoDien bằng phản chiếu (DllTabLoader) — cùng hợp đồng 4 tham số chuỗi với tab Guro / AI.
    /// </summary>
    public class TeamsOhtaTab
    {
        public static FrameworkElement TaoGiaoDien(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            return new TeamsTabControl(tieuDe, moTa, mauNhan);
        }
    }
}
