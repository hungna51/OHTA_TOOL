using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Models;
using ZwCadPlugin.ViewModels;

namespace ZwCadPlugin.Views
{
    /// <summary>Tab Guro hợp nhất trên bảng OHTA TOOL (Home | Tấm | Error).</summary>
    public partial class GuroTabControl : UserControl
    {
        private readonly GuroTabViewModel _vm;

        public GuroTabControl(GuroTabViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            Focusable = true;
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible) _vm.NapLayer();
                else CloudLoiCad.Xoa();
            };
            KeyDown += (s, e) =>
            {
                if (e.Key != Key.Escape) return;
                CloudLoiCad.Xoa();
                e.Handled = true;
            };
        }

        /// <summary>OHTA gọi (qua phản chiếu) khi lăn chuột trên tab — PaletteSet không tự cuộn.</summary>
        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (wheelDelta == 0) return;
            ScrollViewer sv = _vm.SubTabIndex == GuroTabViewModel.TabHome ? HomeScroll
                            : _vm.SubTabIndex == GuroTabViewModel.TabError ? ErrorScroll
                            : TimScrollViewer(PlatesDataGrid);
            if (sv == null) return;
            if (_vm.SubTabIndex == GuroTabViewModel.TabTam)
            {
                int nac = Math.Max(1, Math.Abs(wheelDelta) / 120);
                int dong = nac * Math.Max(1, SystemParameters.WheelScrollLines);
                for (int i = 0; i < dong; i++)
                {
                    if (wheelDelta > 0) sv.LineUp(); else sv.LineDown();
                }
            }
            else
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset - wheelDelta / 120.0 * 48.0);
            }
        }

        private static ScrollViewer TimScrollViewer(DependencyObject o)
        {
            if (o == null) return null;
            if (o is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                ScrollViewer kq = TimScrollViewer(VisualTreeHelper.GetChild(o, i));
                if (kq != null) return kq;
            }
            return null;
        }

        private void OChon_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ComboBox cb && !cb.IsDropDownOpen) _vm.NapLayer();
        }

        private void ComboKhongLan(object sender, MouseWheelEventArgs e)
        {
            if (sender is ComboBox c && !c.IsDropDownOpen) e.Handled = true;
        }

        private void MoTamBam(object sender, MouseButtonEventArgs e) => _vm.MoTabTamCmd.Execute(null);
        private void MoErrorBam(object sender, MouseButtonEventArgs e) => _vm.MoTabErrorCmd.Execute(null);

        private void XoaTimTam(object sender, RoutedEventArgs e)
        {
            _vm.Dst.SearchText = string.Empty;
            OTimTam.Focus();
        }

        private void TimTamPhim(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || string.IsNullOrEmpty(_vm.Dst.SearchText)) return;
            _vm.Dst.SearchText = string.Empty;
            e.Handled = true;
        }

        private void PlatesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlatesDataGrid.SelectedItem != null)
                PlatesDataGrid.ScrollIntoView(PlatesDataGrid.SelectedItem);
        }

        private void PlatesDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is PlateItem plate && plate.IsOrphan)
                e.Cancel = true;
        }

        private void SaiSoPhim(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !(sender is TextBox tb)) return;
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            _vm.ApDungSaiSoCmd.Execute(null);
            e.Handled = true;
        }

        private void NhomNetBam(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is UnconvertedGroupItem grp)
                _vm.Dsn.ZoomNhom(grp);
        }

        private void MucErrorBam(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is MucErrorItem item)
                _vm.BamMucErrorCmd.Execute(item);
        }

        private void ChanBam(object sender, MouseButtonEventArgs e) => e.Handled = true;

        private void DuaVeByLayerBam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is MucErrorItem item) _vm.DuaVeByLayerCmd.Execute(item);
        }

        private void XemTamTuErrorBam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is MucErrorItem item) _vm.XemTamLoiTuErrorCmd.Execute(item);
        }

        private void MoTuDienErrorBam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _vm.MoTuDienTuErrorCmd.Execute(null);
        }

        private void ChonZoomErrorBam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is MucErrorItem item) _vm.ChonZoomErrorCmd.Execute(item);
        }

        private void DanhDauXuLyBam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is MucErrorItem item) _vm.DanhDauDaXuLyCmd.Execute(item);
        }

        private void HoanTacXuLyBam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is MucErrorItem item) _vm.HoanTacDaXuLyCmd.Execute(item);
        }

        private void Scale05Bam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is MucErrorItem item) _vm.Scale05Cmd.Execute(item);
        }

        private void ThuScaleBam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is MucErrorItem item) _vm.ThuScaleCmd.Execute(item);
        }

        private void ScalePhim(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !(sender is TextBox tb)) return;
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            if (tb.Tag is MucErrorItem item) _vm.ThuScaleCmd.Execute(item);
            e.Handled = true;
        }
    }
}
