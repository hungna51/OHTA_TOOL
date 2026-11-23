using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CsvTool.ViewModels;

namespace CsvTool.Views
{
    public partial class CsvTabControl : UserControl
    {
        private readonly CsvTabViewModel _vm;
        private DongTam _dangChon;

        public CsvTabControl(CsvTabViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            Resources["Mau"] = vm.AccentBrush;
            vm.DongHien.CollectionChanged += (s, e) => _dangChon = null;
            Focusable = true;   // bấm thẻ / dòng lỗi xong, Esc trên tab xoá được cloud
            KeyDown += (s, e) =>
            {
                if (e.Key != Key.Escape) return;
                _vm.XoaCloud();
                _dangChon = null;
                e.Handled = true;
            };
        }

        /// <summary>Bấm vào thẻ tấm (kể cả vào ô để sửa) → zoom tới khung; bấm lại cùng thẻ thì không zoom lại.</summary>
        private void TheBam(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is DongTam d) || d == _dangChon) return;
            _dangChon = d;
            Focus();
            _vm.ChonDong(d);
        }

        private void MuiBam(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DongTam d) d.MoRong = !d.MoRong;
        }

        private void LoiBam(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is DongLoi l)) return;
            _dangChon = null;   // bấm lại thẻ của tấm này vẫn zoom về cả tấm
            Focus();
            _vm.ChonLoi(l);
        }

        /// <summary>Lăn chuột qua ô chọn đang đóng không được đổi giá trị (làm đổi giá tấm mà không hay).</summary>
        private void ComboKhongLan(object sender, MouseWheelEventArgs e)
        {
            if (sender is ComboBox c && !c.IsDropDownOpen) e.Handled = true;
        }

        private void GiaTuDongBam(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DongTam d) _vm.VeGiaTuDong(d);
        }

        private void NhapGiaBam(object sender, RoutedEventArgs e) => _vm.TabDangChon = 3;

        private void ThemBeBam(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DongTam d) d.ThemBe();
        }

        private void XoaBeBam(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement f) || !(f.DataContext is DongBe b)) return;
            if (f.Tag is DongTam d) d.XoaBe(b);
        }

        private void VeBeBam(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DongTam d) d.VeBeBanVe();
        }

        // Ô công ty gõ được: chỉ ghi khi đã chọn xong, không tính lại giá theo từng ký tự
        private static void GhiCongTy(object sender) =>
            (sender as ComboBox)?.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();

        private void CongTyDong(object sender, System.EventArgs e) => GhiCongTy(sender);

        private void CongTyRoi(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is ComboBox c && !c.IsKeyboardFocusWithin && !c.IsDropDownOpen) GhiCongTy(c);
        }

        private void CongTyPhim(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) GhiCongTy(sender);
        }

        private void XoaTim(object sender, RoutedEventArgs e)
        {
            _vm.TimKiem = "";
            OTim.Focus();
        }

        private void TimPhim(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || _vm.TimKiem == "") return;   // ô trống: Esc đi tiếp để xoá cloud
            _vm.TimKiem = "";
            e.Handled = true;
        }

        /// <summary>Enter trong ô = ghi ngay (mặc định TextBox chỉ ghi khi rời ô).</summary>
        private void OPhim(object sender, KeyEventArgs e)
        {
            if ((e.Key != Key.Enter && e.Key != Key.Tab) || !(sender is TextBox t) || t.IsReadOnly) return;
            var b = t.GetBindingExpression(TextBox.TextProperty);
            b?.UpdateSource();
            if (b?.Status == System.Windows.Data.BindingStatus.Active) b.UpdateTarget();
            e.Handled = e.Key == Key.Enter;   // Tab vẫn chuyển sang ô kế tiếp
        }

        /// <summary>DllTabLoader của OHTATOOL gọi bằng phản chiếu để lăn chuột trên PaletteSet.</summary>
        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (wheelDelta == 0) return;
            ScrollViewer c = _vm.TabDangChon == 0 ? CuonChung : _vm.TabDangChon == 2 ? CuonLoi : _vm.TabDangChon == 3 ? CuonGia : Cuon;
            c.ScrollToVerticalOffset(c.VerticalOffset - wheelDelta / 120.0 * 48.0);
        }
    }
}
