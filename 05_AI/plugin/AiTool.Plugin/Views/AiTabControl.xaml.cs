using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
            NutKiemTra.Background = vm.AccentBrush;
            NutGui.Background = vm.AccentBrush;
            vm.Chat.CuonXuong += () => ChatCuon.ScrollToEnd();
        }

        /// <summary>Enter = gửi, Shift+Enter = xuống dòng.</summary>
        private void CauHoiPhim(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || (Keyboard.Modifiers & ModifierKeys.Shift) != 0) return;
            e.Handled = true;
            if (_vm.Chat.KhongGui) _vm.Chat.GuiCmd.Execute(null);
        }

        /// <summary>Bấm dòng "Chưa có API key" -> mở cài đặt.</summary>
        private void MoCaiDat(object sender, MouseButtonEventArgs e) => _vm.CaiDatCmd.Execute(null);

        /// <summary>CuonHo của OHTATOOL gọi bằng phản chiếu để lăn chuột trên PaletteSet.</summary>
        public void ScrollByMouseWheel(int wheelDelta)
        {
            ScrollViewer sv = _vm.DangChat ? ChatCuon : KhungCuon;
            if (sv == null || wheelDelta == 0) return;
            sv.ScrollToVerticalOffset(sv.VerticalOffset - wheelDelta / 120.0 * 48.0);
        }

        /// <summary>Mở ô chọn layer -> đọc lại layer của bản vẽ đang mở.</summary>
        private void LayerMo(object sender, EventArgs e) => _vm.LamMoiLayer();

        private void CanhBaoBam(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DongCanhBao c)
                _vm.ZoomToi(c);
        }
    }
}
