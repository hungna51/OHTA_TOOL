using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ZwCadPlugin.ViewModels;

namespace ZwCadPlugin.Views
{
    /// <summary>Tab Guro trên bảng OHTA (mục 192, 194). Logic nằm ở GuroTabViewModel.</summary>
    public partial class GuroTabControl : UserControl
    {
        private readonly GuroTabViewModel _vm;

        public GuroTabControl(GuroTabViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            // Quay lại tab (vd. sau khi vẽ thêm khung) -> đếm lại layer của bản vẽ
            IsVisibleChanged += (s, e) => { if (IsVisible) _vm.NapLayer(); };
        }

        /// <summary>OHTA gọi (qua phản chiếu) khi lăn chuột trên tab — PaletteSet không tự cuộn.</summary>
        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (KhungCuon == null || wheelDelta == 0) return;
            KhungCuon.ScrollToVerticalOffset(KhungCuon.VerticalOffset - wheelDelta / 120.0 * 48.0);
        }

        /// <summary>Bấm mở ô chọn layer -> đếm lại trước để danh sách đúng bản vẽ lúc này.</summary>
        private void OChon_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ComboBox cb && !cb.IsDropDownOpen) _vm.NapLayer();
        }
    }
}
