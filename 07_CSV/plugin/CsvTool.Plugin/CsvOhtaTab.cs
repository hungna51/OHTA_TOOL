using System.Windows;
using CsvTool.Views;

namespace CsvTool
{
    /// <summary>
    /// Cửa vào của tab CSV trên bảng OHTA. OHTATOOL.dll KHÔNG tham chiếu CSVTOOL.dll:
    /// nó nạp DLL này rồi gọi TaoGiaoDien bằng phản chiếu (DllTabLoader), nên chữ ký
    /// (tên lớp, tên hàm, 4 tham số chuỗi) là hợp đồng giữa hai DLL.
    /// </summary>
    public class CsvOhtaTab
    {
        public static FrameworkElement TaoGiaoDien(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            return new CsvTabControl(tieuDe, moTa);
        }
    }
}
