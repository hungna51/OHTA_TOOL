using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZwCadPlugin.ViewModels;
#if ZWCAD
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
using CadColor = ZwSoft.ZwCAD.Colors.Color;
using CadColorMethod = ZwSoft.ZwCAD.Colors.ColorMethod;
using CadColorDialog = ZwSoft.ZwCAD.Windows.ColorDialog;
#else
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using CadColor = Autodesk.AutoCAD.Colors.Color;
using CadColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;
using CadColorDialog = Autodesk.AutoCAD.Windows.ColorDialog;
#endif

namespace ZwCadPlugin.Views
{
    /// <summary>Tab Guro trên bảng OHTA (mục 192). Logic nằm ở GuroTabViewModel.</summary>
    public partial class GuroTabControl : UserControl
    {
        private readonly GuroTabViewModel _vm;

        public GuroTabControl(GuroTabViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            vm.PropertyChanged += Vm_PropertyChanged;
            CapNhatOMau();
        }

        /// <summary>OHTA gọi (qua phản chiếu) khi lăn chuột trên tab — PaletteSet không tự cuộn.</summary>
        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (KhungCuon == null || wheelDelta == 0) return;
            KhungCuon.ScrollToVerticalOffset(KhungCuon.VerticalOffset - wheelDelta / 120.0 * 48.0);
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(GuroTabViewModel.MauDim) ||
                e.PropertyName == nameof(GuroTabViewModel.AutoDim))
                CapNhatOMau();
        }

        private void CapNhatOMau()
        {
            if (_vm.AutoDim)
            {
                OMau.Visibility = Visibility.Collapsed;
                return;
            }
            OMau.Visibility = Visibility.Visible;
            try
            {
                System.Drawing.Color rgb = CadColor.FromColorIndex(CadColorMethod.ByAci, (short)_vm.MauDim).ColorValue;
                OMau.Background = new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
            }
            catch
            {
                OMau.Background = Brushes.Transparent;
            }
        }

        private void ChonMau_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cd = new CadColorDialog
                {
                    IncludeByBlockByLayer = false,
                    Color = CadColor.FromColorIndex(CadColorMethod.ByAci, (short)_vm.MauDim),
                };
                if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    short aci = cd.Color.ColorIndex;
                    if (aci >= 1 && aci <= 255) _vm.DatMauTay(aci);
                    else MessageBox.Show("Hãy chọn một màu trong bảng màu chỉ số (ACI 1-255).", "GURO",
                                         MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không mở được hộp chọn màu:\n" + ex.Message, "GURO",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GhiChu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CadApplication.ShowModalWindow(new NoteDictionaryWindow());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở hộp thoại cài đặt ghi chú:\n" + ex.Message, "GURO",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
