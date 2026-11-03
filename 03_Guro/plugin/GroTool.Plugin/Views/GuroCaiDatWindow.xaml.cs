using System;
using System.Windows;
using System.Windows.Controls;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.ViewModels;

namespace ZwCadPlugin.Views
{
    /// <summary>Cửa sổ ⚙ Cài đặt GURO (mục 194–196): tab Chung + tab Ghi chú; OK mới lưu cả hai.</summary>
    public partial class GuroCaiDatWindow : Window
    {
        private readonly GuroCaiDatViewModel _vm;

        public bool DaLuu { get; private set; }

        public GuroCaiDatWindow(GuroCaiDatViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
        }

        /// <summary>Nút Khôi phục / Thêm từ chỉ hiện ở tab Ghi chú.</summary>
        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, Tabs)) return;   // bỏ sự kiện nổi lên từ DataGrid trong tab
            var hien = Tabs.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
            BtnKhoiPhuc.Visibility = hien;
            BtnThemTu.Visibility = hien;
        }

        private void ThemTu_Click(object sender, RoutedEventArgs e) => PanelGhiChu.ThemTu();

        private void KhoiPhuc_Click(object sender, RoutedEventArgs e) => PanelGhiChu.KhoiPhuc();

        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.DuocLuu) return;
            try
            {
                GroRegistry.GhiCaiDat(_vm.KetQua());
                await PanelGhiChu.LuuAsync();   // tab Ghi chú chưa mở thì bỏ qua
                DaLuu = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không lưu được cài đặt:\n" + ex.Message, Title,
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
