using System;
using System.Windows;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.ViewModels;

namespace ZwCadPlugin.Views
{
    /// <summary>Cửa sổ ⚙ Cài đặt GURO (mục 194, 195): tab Chung + tab Ghi chú; OK mới lưu cả hai.</summary>
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
