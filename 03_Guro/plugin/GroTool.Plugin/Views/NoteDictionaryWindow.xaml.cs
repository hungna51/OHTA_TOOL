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

        private async void Luu_Click(object sender, RoutedEventArgs e)
        {
            await Panel.LuuAsync();
            DialogResult = true;
            Close();
        }
    }
}
