using System;
using System.Windows;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.ViewModels;

namespace ZwCadPlugin.Views
{
    /// <summary>Cửa sổ ⚙ Cài đặt GURO của tab Guro (mục 194). OK mới ghi registry.</summary>
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

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.DuocLuu) return;
            try
            {
                GroRegistry.GhiCaiDat(_vm.KetQua());
                DaLuu = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không lưu được cài đặt:\n" + ex.Message, Title,
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GhiChu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new NoteDictionaryWindow { Owner = this };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể mở hộp thoại cài đặt ghi chú:\n" + ex.Message, Title,
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
