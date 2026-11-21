using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace CsvTool.Views
{
    /// <summary>Hiện trước mỗi lần quét: chọn cách tính giá, rồi bấm 1 trong 2 nút quét là chạy luôn.</summary>
    public partial class CsvThietLapQuetWindow : Window
    {
        private readonly Func<string> _tepDangMo;

        public bool ToanBanVe { get; private set; }
        public bool TinhGia => OTinhGia.IsChecked == true;
        public string CongTy => (OCongTy.Text ?? "").Trim();
        public int CheDo => CheDo2.IsChecked == true ? 2 : CheDo1.IsChecked == true ? 1 : 0;

        public CsvThietLapQuetWindow(string tenBanVe, int soKhung, IEnumerable<string> dsCongTy, string congTy, bool tinhGia, int cheDo, Brush mauTab, Func<string> tepDangMo)
        {
            InitializeComponent();
            _tepDangMo = tepDangMo;
            if (mauTab != null) Resources["Mau"] = mauTab;
            ThongTin.Text = "Bản vẽ " + tenBanVe + " · " + soKhung + " khung tấm";
            OCongTy.ItemsSource = dsCongTy;
            OCongTy.Text = congTy ?? "";
            (cheDo == 2 ? CheDo2 : cheDo == 1 ? CheDo1 : CheDo0).IsChecked = true;
            OTinhGia.IsChecked = tinhGia;
            CapNhatKhoiGia();
            BaoTepDangMo();
        }

        /// <summary>File CSV / data đang mở thì chưa cho quét; đóng file rồi bấm lại là chạy, không cần mở lại dialog.</summary>
        private bool BaoTepDangMo()
        {
            string f = _tepDangMo?.Invoke();
            if (f == null) return false;
            Loi.Text = Path.GetFileName(f) + " đang mở. Đóng file rồi bấm quét.";
            Loi.Visibility = Visibility.Visible;
            return true;
        }

        private void DoiTinhGia(object sender, RoutedEventArgs e) => CapNhatKhoiGia();

        private void CapNhatKhoiGia()
        {
            if (KhoiGia == null || GhiChuGia == null) return;
            KhoiGia.IsEnabled = TinhGia;
            KhoiGia.Opacity = TinhGia ? 1 : 0.45;
            GhiChuGia.Text = TinhGia ? "Bảng giá đọc mới từ database khi quét." : "Chỉ quét thông số, giữ nguyên giá đang có.";
            if (!BaoTepDangMo()) Loi.Visibility = Visibility.Collapsed;
        }

        private void DoiCheDo(object sender, RoutedEventArgs e)
        {
            if (MoTaCheDo == null) return;
            MoTaCheDo.Text = CheDo == 2 ? "Vật liệu do khách cấp, tính 15% tiền vật liệu."
                           : CheDo == 1 ? "Chỉ tính tiền gia công." : "Tính đủ vật liệu, cắt và gia công.";
        }

        private void QuetVung(object sender, RoutedEventArgs e) => Chay(false);
        private void QuetToan(object sender, RoutedEventArgs e) => Chay(true);

        private void Chay(bool toan)
        {
            if (BaoTepDangMo()) return;
            if (TinhGia && CongTy == "")
            {
                Loi.Text = "Chọn công ty, hoặc bỏ tích Tính giá.";
                Loi.Visibility = Visibility.Visible;
                OCongTy.Focus();
                return;
            }
            ToanBanVe = toan;
            DialogResult = true;
        }
    }
}
