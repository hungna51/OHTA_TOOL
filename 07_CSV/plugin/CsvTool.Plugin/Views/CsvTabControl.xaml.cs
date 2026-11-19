using System.Windows.Controls;
using CsvTool.ViewModels;

namespace CsvTool.Views
{
    public partial class CsvTabControl : UserControl
    {
        private readonly CsvTabViewModel _vm;

        public CsvTabControl(CsvTabViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            NutB1.Background = vm.AccentBrush;
            NutExport.Background = vm.AccentBrush;
        }

        private void DongChon(object sender, SelectionChangedEventArgs e) => _vm.ChonDong(Luoi.SelectedItem as DongTam);

        /// <summary>Dòng lỗi không có tấm để ghi ngược → không cho sửa ô.</summary>
        private void BatDauSua(object sender, DataGridBeginningEditEventArgs e)
        {
            if (!(e.Row.Item is DongTam d) || !d.SuaDuoc) e.Cancel = true;
        }
    }
}
