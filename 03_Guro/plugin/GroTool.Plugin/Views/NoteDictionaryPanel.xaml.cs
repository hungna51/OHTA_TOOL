using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.Views
{
    /// <summary>
    /// Nội dung "Cài đặt ghi chú" (từ điển Nhật → Việt + đồng bộ Google Sheet). Mục 195:
    /// dùng chung cho tab Ghi chú của cửa sổ ⚙ Cài đặt GURO và hộp thoại NoteDictionaryWindow
    /// (bảng DST). Mục 196: kiểu bảng "Danh sách góc bẻ thử" của Fname — ô Tìm, bảng chỉ xem,
    /// bấm đúp để sửa / xóa trong NoteTuWindow. Nơi chứa đặt nút ➕ Thêm (ThemTu), Khôi phục
    /// (KhoiPhuc) và Lưu (LuuAsync) ở thanh đáy.
    /// </summary>
    public partial class NoteDictionaryPanel : UserControl
    {
        private ObservableCollection<NoteDictionaryItem> _items;
        private ICollectionView _view;

        private bool _daMo;

        /// <summary>
        /// Panel đã hiện ít nhất một lần (ô link / cờ đã được nạp). Chưa hiện thì LuuAsync
        /// bỏ qua — nếu không sẽ ghi đè link Google Sheet bằng ô trống.
        /// </summary>
        public bool DaMo => _daMo;

        public NoteDictionaryPanel()
        {
            InitializeComponent();
            LoadData();
            Loaded += Panel_Loaded;
        }

        /// <summary>Cửa sổ chứa panel — làm chủ cho MessageBox.</summary>
        private Window Chu => Window.GetWindow(this);

        private async void Panel_Loaded(object sender, RoutedEventArgs e)
        {
            // Nằm trong TabControl thì Loaded chạy mỗi lần chọn tab -> chỉ khởi tạo lần đầu
            if (_daMo) return;
            _daMo = true;
            TxtSheetUrl.Text = JapanNoteTranslator.GetSheetUrl();
            ChkAutoSync.IsChecked = JapanNoteTranslator.GetAutoSync();
            ChkAutoUpload.IsChecked = JapanNoteTranslator.GetAutoUpload();
            TxtTim.Focus();

            if (ChkAutoSync.IsChecked == true)
            {
                await ThucHienDongBoAsync(hienThongBao: false);
            }
        }

        private void LoadData()
        {
            var rawList = JapanNoteTranslator.GetItems();
            _items = new ObservableCollection<NoteDictionaryItem>(rawList);
            DgrDictionary.ItemsSource = _items;
            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = KhopTim;
            CapNhatThongKe();
        }

        private void CapNhatThongKe()
        {
            int tong = _items?.Count ?? 0;
            int hien = _view?.Cast<object>().Count() ?? tong;
            LblStatus.Text = $"Tổng cộng: {tong} từ khóa" + (hien != tong ? $" (đang lọc: {hien})" : "") +
                             " — bấm đúp một dòng để sửa hoặc xóa.";
        }

        private async void DongBoSheet_Click(object sender, RoutedEventArgs e)
        {
            await ThucHienDongBoAsync(hienThongBao: true);
        }

        private async void DayLenSheet_Click(object sender, RoutedEventArgs e)
        {
            await ThucHienDayLenAsync(hienThongBao: true);
        }

        private async Task ThucHienDayLenAsync(bool hienThongBao)
        {
            var validItems = _items.Where(x => !string.IsNullOrWhiteSpace(x?.Japanese)).ToList();
            if (validItems.Count == 0)
            {
                if (hienThongBao)
                    MessageBox.Show(Chu, "Không có từ khóa nào để tải lên!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnUpload.IsEnabled = false;
            string oldText = BtnUpload.Content?.ToString();
            BtnUpload.Content = "⏳ Đang đẩy...";
            LblStatus.Text = "Đang tải dữ liệu lên Google Sheet...";

            try
            {
                string url = TxtSheetUrl.Text?.Trim();
                if (string.IsNullOrWhiteSpace(url))
                    url = JapanNoteTranslator.DefaultGoogleScriptUrl;

                var kq = await JapanNoteTranslator.UploadToGoogleSheetAsync(validItems, url);
                if (kq.Message == "NEED_SETUP")
                {
                    MessageBox.Show(Chu, "Vui lòng dán đường link Google Apps Script Web App (dạng https://script.google.com/macros/s/.../exec) vào ô trên!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtSheetUrl.Focus();
                    return;
                }

                if (kq.Success)
                {
                    LblStatus.Text = kq.Message;
                    if (hienThongBao)
                    {
                        MessageBox.Show(Chu, kq.Message, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    LblStatus.Text = kq.Message;
                    if (hienThongBao)
                    {
                        MessageBox.Show(Chu, kq.Message, "Lỗi khi tải lên", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                string loi = "Lỗi khi tải lên: " + ex.Message;
                LblStatus.Text = loi;
                if (hienThongBao)
                {
                    MessageBox.Show(Chu, loi, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                BtnUpload.Content = oldText ?? "⬆ Đẩy lên Sheet";
                BtnUpload.IsEnabled = true;
            }
        }

        private async Task ThucHienDongBoAsync(bool hienThongBao)
        {
            string url = TxtSheetUrl.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                if (hienThongBao)
                    MessageBox.Show(Chu, "Vui lòng nhập đường link Google Sheet!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnSync.IsEnabled = false;
            string oldText = BtnSync.Content?.ToString();
            BtnSync.Content = "⏳ Đang tải...";
            LblStatus.Text = "Đang kết nối và tải dữ liệu từ Google Sheet...";

            try
            {
                var kq = await JapanNoteTranslator.SyncFromGoogleSheetAsync(url);
                if (kq.Success)
                {
                    LoadData();
                    LblStatus.Text = kq.Message;
                    if (hienThongBao)
                    {
                        MessageBox.Show(Chu, kq.Message, "Đồng bộ thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    LblStatus.Text = kq.Message;
                    if (hienThongBao)
                    {
                        MessageBox.Show(Chu, kq.Message, "Lỗi đồng bộ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                string loi = $"Lỗi kết nối Google Sheet: {ex.Message}";
                LblStatus.Text = loi;
                if (hienThongBao)
                {
                    MessageBox.Show(Chu, loi, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                BtnSync.Content = oldText ?? "⬇ Tải từ Sheet";
                BtnSync.IsEnabled = true;
            }
        }

        private void AutoSync_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkAutoSync != null)
            {
                JapanNoteTranslator.SetAutoSync(ChkAutoSync.IsChecked == true);
            }
        }

        private void AutoUpload_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkAutoUpload != null)
            {
                JapanNoteTranslator.SetAutoUpload(ChkAutoUpload.IsChecked == true);
            }
        }

        /// <summary>Lọc theo ô Tìm: mọi từ (cách nhau dấu cách) phải có trong Nhật hoặc Việt.</summary>
        private bool KhopTim(object o)
        {
            string tim = TxtTim?.Text?.Trim();
            if (string.IsNullOrEmpty(tim) || !(o is NoteDictionaryItem it)) return true;
            string chu = ((it.Japanese ?? "") + " " + (it.Vietnamese ?? "")).ToLowerInvariant();
            return tim.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).All(chu.Contains);
        }

        private void TxtTim_TextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
            CapNhatThongKe();
        }

        /// <summary>Nút ➕ Thêm từ của nơi chứa: thêm mới, trùng cụm từ Nhật thì cập nhật nghĩa.</summary>
        public void ThemTu()
        {
            var w = new NoteTuWindow(null, null, false) { Owner = Chu };
            if (w.ShowDialog() != true || w.KetQua != NoteTuWindow.KetThuc.Luu) return;

            var existing = _items.FirstOrDefault(x => string.Equals(x.Japanese, w.Japanese, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Vietnamese = w.Vietnamese;
            }
            else
            {
                existing = new NoteDictionaryItem { Japanese = w.Japanese, Vietnamese = w.Vietnamese };
                _items.Add(existing);
            }
            _view?.Refresh();
            DgrDictionary.SelectedItem = existing;
            DgrDictionary.ScrollIntoView(existing);
            CapNhatThongKe();
        }

        /// <summary>Bấm đúp một dòng: sửa hoặc xóa.</summary>
        private void Dgr_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(DgrDictionary.SelectedItem is NoteDictionaryItem item)) return;
            var w = new NoteTuWindow(item.Japanese, item.Vietnamese, true) { Owner = Chu };
            if (w.ShowDialog() != true) return;
            if (w.KetQua == NoteTuWindow.KetThuc.Xoa)
            {
                _items.Remove(item);
            }
            else if (w.KetQua == NoteTuWindow.KetThuc.Luu)
            {
                item.Japanese = w.Japanese;
                item.Vietnamese = w.Vietnamese;
            }
            _view?.Refresh();
            CapNhatThongKe();
        }

        /// <summary>Nút ⟲ Khôi phục mặc định của nơi chứa.</summary>
        public void KhoiPhuc()
        {
            var res = MessageBox.Show(Chu,
                "Bạn có chắc muốn khôi phục từ điển về danh mục chuẩn ban đầu?\nCác từ khóa tự thêm sẽ bị đặt lại.",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                JapanNoteTranslator.ResetToDefaults();
                LoadData();
            }
        }

        /// <summary>Lưu link Sheet, các cờ và từ điển; bật tự đẩy lên thì đẩy lên Google Sheet.</summary>
        public async Task LuuAsync()
        {
            if (!_daMo) return;

            // Lưu URL Google Sheet và các cờ cấu hình
            JapanNoteTranslator.SetSheetUrl(TxtSheetUrl.Text?.Trim());
            JapanNoteTranslator.SetAutoSync(ChkAutoSync.IsChecked == true);
            JapanNoteTranslator.SetAutoUpload(ChkAutoUpload.IsChecked == true);

            // Lọc các dòng hợp lệ và lưu vào từ điển & Registry
            var validItems = _items.Where(x => !string.IsNullOrWhiteSpace(x.Japanese)).ToList();
            JapanNoteTranslator.SaveItems(validItems);

            // Nếu người dùng bật tự động đẩy lên Google Sheet khi Lưu
            if (ChkAutoUpload.IsChecked == true && validItems.Count > 0)
            {
                string scriptUrl = JapanNoteTranslator.GetScriptUrl();
                string sheetUrl = TxtSheetUrl.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(scriptUrl) || (!string.IsNullOrWhiteSpace(sheetUrl) && sheetUrl.Contains("script.google.com")))
                {
                    try
                    {
                        await JapanNoteTranslator.UploadToGoogleSheetAsync(validItems);
                    }
                    catch { }
                }
            }
        }
    }
}
