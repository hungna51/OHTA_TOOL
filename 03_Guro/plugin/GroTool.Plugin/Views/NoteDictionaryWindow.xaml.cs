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

        private void ThemTu_Click(object sender, RoutedEventArgs e) => Panel.ThemTu();

        private void KhoiPhuc_Click(object sender, RoutedEventArgs e) => Panel.KhoiPhuc();

        private async void Luu_Click(object sender, RoutedEventArgs e)
        {
            await Panel.LuuAsync();
            DialogResult = true;
            Close();
        }
    }
}
