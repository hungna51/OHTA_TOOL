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
    public partial class NoteDictionaryWindow : Window
    {
        private ObservableCollection<NoteDictionaryItem> _items;

        public NoteDictionaryWindow()
        {
            InitializeComponent();
            LoadData();
            Loaded += NoteDictionaryWindow_Loaded;
        }

        private async void NoteDictionaryWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
                    MessageBox.Show(this, "Không có từ khóa nào để tải lên!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show(this, "Vui lòng dán đường link Google Apps Script Web App (dạng https://script.google.com/macros/s/.../exec) vào ô trên!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtSheetUrl.Focus();
                    return;
                }

                if (kq.Success)
                {
                    LblStatus.Text = kq.Message;
                    if (hienThongBao)
                    {
                        MessageBox.Show(this, kq.Message, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    LblStatus.Text = kq.Message;
                    if (hienThongBao)
                    {
                        MessageBox.Show(this, kq.Message, "Lỗi khi tải lên", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                string loi = "Lỗi khi tải lên: " + ex.Message;
                LblStatus.Text = loi;
                if (hienThongBao)
                {
                    MessageBox.Show(this, loi, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show(this, "Vui lòng nhập đường link Google Sheet!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        MessageBox.Show(this, kq.Message, "Đồng bộ thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    LblStatus.Text = kq.Message;
                    if (hienThongBao)
                    {
                        MessageBox.Show(this, kq.Message, "Lỗi đồng bộ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                string loi = $"Lỗi kết nối Google Sheet: {ex.Message}";
                LblStatus.Text = loi;
                if (hienThongBao)
                {
                    MessageBox.Show(this, loi, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void Luu_Click(object sender, RoutedEventArgs e)
        {
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

            DialogResult = true;
            Close();
        }

        private void Dong_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
