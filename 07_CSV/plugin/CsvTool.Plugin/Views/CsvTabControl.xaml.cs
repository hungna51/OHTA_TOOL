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

        // Ô công ty: danh sách nằm ngay dưới ô (không dùng Popup vì chạy trong PaletteSet), con trỏ luôn ở ô gõ
        private void CongTyVao(object sender, KeyboardFocusChangedEventArgs e)
        {
            OCongTy.SelectAll();
            _vm.MoDsCongTy();
        }

        private void CongTyRoi(object sender, KeyboardFocusChangedEventArgs e) => _vm.XongCongTy();

        private void CongTyPhim(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                int n = DsCT.Items.Count;
                if (n == 0) return;
                DsCT.SelectedIndex = e.Key == Key.Down ? (DsCT.SelectedIndex + 1) % n : (DsCT.SelectedIndex <= 0 ? n : DsCT.SelectedIndex) - 1;
                DsCT.ScrollIntoView(DsCT.SelectedItem);
            }
            else if (e.Key == Key.Enter) _vm.XongCongTy(DsCT.SelectedItem as CsvTool.Core.MucCongTy);
            else if (e.Key == Key.Escape) _vm.HuyDsCongTy();
            else return;
            e.Handled = true;
        }

        private void CongTyBam(object sender, MouseButtonEventArgs e)
        {
            if (!((e.OriginalSource as FrameworkElement)?.DataContext is CsvTool.Core.MucCongTy m)) return;   // bấm thanh cuộn thì bỏ qua
            _vm.XongCongTy(m);
            e.Handled = true;
        }

        private static ScrollViewer TimCuon(DependencyObject o)
        {
            if (o is ScrollViewer c) return c;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(o); i++)
            {
                ScrollViewer kq = TimCuon(System.Windows.Media.VisualTreeHelper.GetChild(o, i));
                if (kq != null) return kq;
            }
            return null;
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
            ScrollViewer c = _vm.TabDangChon == 0 ? (DsCT.IsVisible && DsCT.IsMouseOver ? TimCuon(DsCT) ?? CuonChung : CuonChung) : _vm.TabDangChon == 2 ? CuonLoi : _vm.TabDangChon == 3 ? CuonGia : Cuon;
            c.ScrollToVerticalOffset(c.VerticalOffset - wheelDelta / 120.0 * 48.0);
        }
    }
}
