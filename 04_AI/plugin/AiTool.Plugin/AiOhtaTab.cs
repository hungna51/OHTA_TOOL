using System.Windows;
using System.Windows.Controls;

namespace AiTool
{
    /// <summary>
    /// Cửa vào của tab AI trên bảng OHTA (ADR 0005). OHTATOOL.dll KHÔNG tham chiếu AITOOL.dll:
    /// nó nạp DLL này rồi gọi TaoGiaoDien bằng phản chiếu (DllTabLoader), nên chữ ký
    /// (tên lớp, tên hàm, 4 tham số chuỗi) là hợp đồng giữa hai DLL.
    /// </summary>
    public class AiOhtaTab
    {
        public static FrameworkElement TaoGiaoDien(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            return new TextBlock { Text = "Tab AI đang dựng", Margin = new Thickness(12) };
        }
    }
}
