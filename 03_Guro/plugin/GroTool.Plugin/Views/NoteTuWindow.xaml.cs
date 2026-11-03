using System.Windows;

namespace ZwCadPlugin.Views
{
    /// <summary>Thêm / sửa / xóa một từ khóa ghi chú (mục 196). Kết quả đọc ở KetQua.</summary>
    public partial class NoteTuWindow : Window
    {
        public enum KetThuc { Huy, Luu, Xoa }

        public KetThuc KetQua { get; private set; } = KetThuc.Huy;
        public string Japanese => TxtJapanese.Text?.Trim();
        public string Vietnamese => TxtVietnamese.Text?.Trim();

        /// <param name="sua">true = sửa dòng có sẵn (hiện nút Xóa).</param>
        public NoteTuWindow(string japanese, string vietnamese, bool sua)
        {
            InitializeComponent();
            TxtJapanese.Text = japanese ?? "";
            TxtVietnamese.Text = vietnamese ?? "";
            if (sua)
            {
                TxtTieuDe.Text = "Sửa từ khóa ghi chú";
                BtnXoa.Visibility = Visibility.Visible;
            }
            Loaded += (s, e) => TxtJapanese.Focus();
        }

        private void Luu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Japanese))
            {
                MessageBox.Show(this, "Vui lòng nhập cụm từ tiếng Nhật!", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtJapanese.Focus();
                return;
            }
            KetQua = KetThuc.Luu;
            DialogResult = true;
        }

        private void Xoa_Click(object sender, RoutedEventArgs e)
        {
            KetQua = KetThuc.Xoa;
            DialogResult = true;
        }
    }
}
