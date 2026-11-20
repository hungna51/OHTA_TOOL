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
            NutQuetVung.Background = vm.AccentBrush;
            NutQuetToan.Background = vm.AccentBrush;
            NutXuat.BorderBrush = vm.AccentBrush;
            NutXuat.Foreground = vm.AccentBrush;
            KhungCheDo.BorderBrush = vm.AccentBrush;
            vm.DongHien.CollectionChanged += (s, e) => _dangChon = null;
        }

        /// <summary>Bấm vào thẻ tấm (kể cả vào ô để sửa) → zoom tới khung; bấm lại cùng thẻ thì không zoom lại.</summary>
        private void TheBam(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is DongTam d) || d == _dangChon) return;
            _dangChon = d;
            _vm.ChonDong(d);
        }

        private void MuiBam(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DongTam d) d.MoRong = !d.MoRong;
        }

        /// <summary>Enter trong ô = ghi ngay (mặc định TextBox chỉ ghi khi rời ô).</summary>
        private void OPhim(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !(sender is TextBox t)) return;
            t.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            e.Handled = true;
        }

        /// <summary>DllTabLoader của OHTATOOL gọi bằng phản chiếu để lăn chuột trên PaletteSet.</summary>
        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (wheelDelta == 0) return;
            Cuon.ScrollToVerticalOffset(Cuon.VerticalOffset - wheelDelta / 120.0 * 48.0);
        }
    }
}
