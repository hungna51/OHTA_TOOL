using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace CsvTool.Views
{
    /// <summary>Hỏi tên công ty khi bấm Tính giá, điền sẵn tên đã nhập của đơn.</summary>
    public partial class CsvNhapCongTyWindow : Window
    {
        public string CongTy { get; private set; }

        public CsvNhapCongTyWindow(IEnumerable<string> dsCongTy, string daNhap, Brush mauTab)
        {
            InitializeComponent();
            OCongTy.ItemsSource = dsCongTy;
            OCongTy.Text = daNhap ?? "";
            if (mauTab != null) NutTinh.Background = mauTab;
            Loaded += (s, e) => OCongTy.Focus();
        }

        private void Tinh(object sender, RoutedEventArgs e)
        {
            string ten = (OCongTy.Text ?? "").Trim();
            if (ten == "")
            {
                Loi.Text = "Nhập tên công ty.";
                Loi.Visibility = Visibility.Visible;
                return;
            }
            CongTy = ten;
            DialogResult = true;
        }
    }
}
