using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AiTool.ViewModels;

namespace AiTool.Views
{
    public partial class AiTabControl : UserControl
    {
        private readonly AiTabViewModel _vm;

        public AiTabControl(AiTabViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            NutDan.Background = NutChen.Background = vm.AccentBrush;
            // Ctrl+V trên tab (PaletteSet chuyển phím khi tab có focus) + kéo thả tệp
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && _vm.KhongChay && !(e.OriginalSource is TextBox)) { _vm.Dan(); e.Handled = true; }
            };
            MouseDown += (s, e) => Focus();
            Drop += (s, e) => { if (_vm.KhongChay && e.Data.GetData(DataFormats.FileDrop) is string[] f && f.Length > 0) _vm.Nhan(f[0]); };
        }

        /// <summary>CuonHo của OHTATOOL gọi bằng phản chiếu để lăn chuột trên PaletteSet.</summary>
        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (KhungCuon == null || wheelDelta == 0) return;
            KhungCuon.ScrollToVerticalOffset(KhungCuon.VerticalOffset - wheelDelta / 120.0 * 48.0);
        }

        private void MoAnhGoc(object sender, MouseButtonEventArgs e) => MoTep((_vm.DongChon?.L)?.Anh);
        private void MoAnhVe(object sender, MouseButtonEventArgs e) => MoTep((_vm.DongChon?.L)?.Xem);

        private static void MoTep(string f)
        {
            if (string.IsNullOrEmpty(f)) return;
            try { Process.Start(new ProcessStartInfo(f) { UseShellExecute = true }); }
            catch (Exception) { }
        }
    }
}
