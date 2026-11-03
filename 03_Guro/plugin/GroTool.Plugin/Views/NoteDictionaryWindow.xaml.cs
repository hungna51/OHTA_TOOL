using System.Windows;

namespace ZwCadPlugin.Views
{
    /// <summary>Hộp thoại "Cài đặt ghi chú" riêng (bảng DST mở). Nội dung: NoteDictionaryPanel.</summary>
    public partial class NoteDictionaryWindow : Window
    {
        public NoteDictionaryWindow()
        {
            InitializeComponent();
        }

        /// <summary>IsCancel chỉ tự đóng khi mở bằng ShowDialog — CAD thì không (mục 197).</summary>
        private void Huy_Click(object sender, RoutedEventArgs e) => Close();

        private void ThemTu_Click(object sender, RoutedEventArgs e) => Panel.ThemTu();

        private void KhoiPhuc_Click(object sender, RoutedEventArgs e) => Panel.KhoiPhuc();

        private async void Luu_Click(object sender, RoutedEventArgs e)
        {
            await Panel.LuuAsync();
            // Mở bằng Application.ShowModalWindow -> không gán DialogResult (mục 197)
            Close();
        }
    }
}
