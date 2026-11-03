using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using ZwCadPlugin.ViewModels;

namespace ZwCadPlugin.Views
{
    /// <summary>Cách dialog đóng lại — quyết định bước tiếp theo của vòng lặp gọi nó.</summary>
    public enum KetThucDialog
    {
        Huy,       // Cancel / nút X
        Ok,
        PickNet,   // cần đóng tạm để pick một nét trên bản vẽ rồi mở lại
        XemDsn,    // trả về lisp để mở danh sách nét chưa đổi (dữ liệu nằm bên lisp)
    }

    public partial class GroSettingsWindow : Window
    {
        private readonly GroSettingsViewModel _vm;

        public KetThucDialog KetThuc { get; private set; } = KetThucDialog.Huy;

        public GroSettingsWindow(GroSettingsViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            vm.PropertyChanged += Vm_PropertyChanged;
            CapNhatOMau();
            Loaded += (s, e) => CboKhung.Focus();
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GroSettingsViewModel.MauDim) || e.PropertyName == nameof(GroSettingsViewModel.AutoDim))
            {
                CapNhatOMau();
            }
        }

        /// <summary>Ô màu nhỏ cạnh nhãn, lấy đúng màu RGB của chỉ số ACI (chỉ hiện khi chọn thủ công).</summary>
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
                var c = ZwSoft.ZwCAD.Colors.Color.FromColorIndex(ZwSoft.ZwCAD.Colors.ColorMethod.ByAci,
                                                                (short)_vm.MauDim);
                System.Drawing.Color rgb = c.ColorValue;
                OMau.Background = new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
            }
            catch
            {
                OMau.Background = Brushes.Transparent;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            KetThuc = KetThucDialog.Ok;
            DialogResult = true;
        }

        private void PickNet_Click(object sender, RoutedEventArgs e)
        {
            KetThuc = KetThucDialog.PickNet;
            DialogResult = true;
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new NoteDictionaryWindow();
                try
                {
                    if (IsLoaded) dlg.Owner = this;
                }
                catch
                {
                    // Bỏ qua lỗi gán Owner nếu môi trường CAD không cho phép
                }
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể mở hộp thoại cài đặt ghi chú:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Hộp chọn màu của chính ZWCAD, mở chồng lên dialog — không cần đóng dialog
        /// như bản DCL. Không cho chọn ByLayer/ByBlock, giống (acad_colordlg c nil).
        /// </summary>
        private void ChonMau_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cd = new ZwSoft.ZwCAD.Windows.ColorDialog
                {
                    IncludeByBlockByLayer = false,
                    Color = ZwSoft.ZwCAD.Colors.Color.FromColorIndex(ZwSoft.ZwCAD.Colors.ColorMethod.ByAci,
                                                                    (short)_vm.MauDim),
                };
                if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    short aci = cd.Color.ColorIndex;
                    // Hộp màu cho chọn cả True Color; chỉ số ACI ngoài 1..255 thì bỏ qua.
                    if (aci >= 1 && aci <= 255) _vm.DatMauTay(aci);
                    else MessageBox.Show(this, "Hãy chọn một màu trong bảng màu chỉ số (ACI 1-255).",
                                         Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không mở được hộp chọn màu của ZWCAD:\n" + ex.Message,
                                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
