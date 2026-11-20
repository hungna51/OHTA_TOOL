using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CsvTool.Core;

namespace CsvTool.Views
{
    /// <summary>Cửa sổ ⚙ của tab CSV: sửa trên ô, chỉ ghi vào CaiDatCsv khi bấm Lưu (Huỷ = không đổi gì).</summary>
    public partial class CsvCaiDatWindow : Window
    {
        private readonly CaiDatCsv _c;

        public CsvCaiDatWindow(CaiDatCsv c, Brush mauTab)
        {
            InitializeComponent();
            _c = c;
            if (mauTab != null) NutLuu.Background = mauTab;
            SxTraiPhai.IsChecked = c.SapXep == KieuSapXep.TraiSangPhai;
            SxTrenDuoi.IsChecked = c.SapXep == KieuSapXep.TrenXuongDuoi;
            SxChuCai.IsChecked = c.SapXep == KieuSapXep.ChuCai;
            LopKhung.Text = c.LopKhung; LopDim.Text = c.LopDim; LopTapu.Text = c.LopTapu;
            DungMacDinh.IsChecked = c.DungLopMacDinh;
            LopTuChon.IsEnabled = !c.DungLopMacDinh;
            BuocSpline.Text = c.BuocSpline.ToString(CultureInfo.InvariantCulture);
            DungSai.Text = c.DungSaiKheHo.ToString(CultureInfo.InvariantCulture);
        }

        private void DoiMacDinh(object sender, RoutedEventArgs e) => LopTuChon.IsEnabled = DungMacDinh.IsChecked != true;

        private void Luu(object sender, RoutedEventArgs e)
        {
            if (!So(BuocSpline.Text, out double buoc) || !So(DungSai.Text, out double dungSai))
            {
                Loi.Text = "Bước spline và dung sai khe hở phải là số lớn hơn 0.";
                Loi.Visibility = Visibility.Visible;
                return;
            }
            _c.SapXep = SxTrenDuoi.IsChecked == true ? KieuSapXep.TrenXuongDuoi
                      : SxChuCai.IsChecked == true ? KieuSapXep.ChuCai : KieuSapXep.TraiSangPhai;
            _c.DungLopMacDinh = DungMacDinh.IsChecked == true;
            _c.LopKhung = Gon(LopKhung.Text, CaiDatCsv.LopKhungMacDinh);
            _c.LopDim = Gon(LopDim.Text, CaiDatCsv.LopDimMacDinh);
            _c.LopTapu = Gon(LopTapu.Text, CaiDatCsv.LopTapuMacDinh);
            _c.BuocSpline = buoc;
            _c.DungSaiKheHo = dungSai;
            DialogResult = true;
        }

        private static string Gon(string s, string macDinh) => string.IsNullOrWhiteSpace(s) ? macDinh : s.Trim();

        private static bool So(string s, out double d) =>
            double.TryParse((s ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d > 0;
    }
}
