using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.Views
{
    /// <summary>
    /// Nội dung "Cài đặt ghi chú" (từ điển Nhật → Việt + đồng bộ Google Sheet). Mục 195:
    /// dùng chung cho tab Ghi chú của cửa sổ ⚙ Cài đặt GURO và hộp thoại NoteDictionaryWindow
    /// (bảng DST). Nơi chứa có nút Lưu gọi LuuAsync().
    /// </summary>
    public partial class NoteDictionaryPanel : UserControl
    {
        private ObservableCollection<NoteDictionaryItem> _items;

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
            TxtJapanese.Focus();

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
            CapNhatThongKe();
        }

        private void CapNhatThongKe()
        {
            LblStatus.Text = $"Tổng cộng: {_items?.Count ?? 0} từ khóa";
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

        private void ThemTu_Click(object sender, RoutedEventArgs e)
        {
            string jp = TxtJapanese.Text?.Trim();
            string vn = TxtVietnamese.Text?.Trim();

            if (string.IsNullOrWhiteSpace(jp))
            {
                MessageBox.Show("Vui lòng nhập cụm từ tiếng Nhật!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtJapanese.Focus();
                return;
            }

            var existing = _items.FirstOrDefault(x => string.Equals(x.Japanese, jp, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Vietnamese = vn;
                DgrDictionary.SelectedItem = existing;
                DgrDictionary.ScrollIntoView(existing);
            }
            else
            {
                var newItem = new NoteDictionaryItem
                {
                    Japanese = jp,
                    Vietnamese = vn
                };
                _items.Add(newItem);
                DgrDictionary.SelectedItem = newItem;
                DgrDictionary.ScrollIntoView(newItem);
            }

            TxtJapanese.Clear();
            TxtVietnamese.Clear();
            TxtJapanese.Focus();
            CapNhatThongKe();
        }

        private void XoaDong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is NoteDictionaryItem item)
            {
                _items.Remove(item);
                CapNhatThongKe();
            }
        }

        private void KhoiPhuc_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(
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
