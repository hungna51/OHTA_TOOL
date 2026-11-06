using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.Views
{
    public partial class GoogleSheetSyncSetupWindow : Window
    {
        private readonly IEnumerable<NoteDictionaryItem> _items;

        public bool DaCapNhatThanhCong { get; private set; } = false;

        public GoogleSheetSyncSetupWindow(IEnumerable<NoteDictionaryItem> items)
        {
            InitializeComponent();
            _items = items;
            TxtScriptUrl.Text = JapanNoteTranslator.GetScriptUrl();
        }

        private void MoSheet_Click(object sender, RoutedEventArgs e)
        {
            string url = JapanNoteTranslator.GetSheetUrl();
            if (string.IsNullOrWhiteSpace(url))
                url = JapanNoteTranslator.DefaultGoogleSheetUrl;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GoogleSheetSyncSetupWindow.xaml", ex);
                MessageBox.Show(this, "Không thể mở trình duyệt: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CopyScriptCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(JapanNoteTranslator.AppsScriptTemplate);
                BtnCopyCode.Content = "✔ Đã sao chép mã!";
            }
            catch (Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GoogleSheetSyncSetupWindow.xaml", ex);
                MessageBox.Show(this, "Không thể sao chép vào Clipboard: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LuuScriptVaDay_Click(object sender, RoutedEventArgs e)
        {
            string scriptUrl = TxtScriptUrl.Text?.Trim();
            if (string.IsNullOrWhiteSpace(scriptUrl) || !scriptUrl.Contains("script.google.com"))
            {
                MessageBox.Show(this, "Vui lòng dán đường link Web App hợp lệ (bắt đầu bằng https://script.google.com/macros/s/...)!",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtScriptUrl.Focus();
                return;
            }

            JapanNoteTranslator.SetScriptUrl(scriptUrl);
            LblStatus.Text = "Đang tải dữ liệu lên Google Sheet...";

            try
            {
                var kq = await JapanNoteTranslator.UploadToGoogleSheetAsync(_items, scriptUrl);
                if (kq.Success)
                {
                    DaCapNhatThanhCong = true;
                    LblStatus.Text = kq.Message;
                    MessageBox.Show(this, kq.Message, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    LblStatus.Text = kq.Message;
                    MessageBox.Show(this, kq.Message, "Lỗi khi tải lên", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GoogleSheetSyncSetupWindow.xaml", ex);
                string loi = "Lỗi khi gửi dữ liệu: " + ex.Message;
                LblStatus.Text = loi;
                MessageBox.Show(this, loi, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyVaMoSheet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tsv = JapanNoteTranslator.ToTsv(_items);
                Clipboard.SetText(tsv);

                string url = JapanNoteTranslator.GetSheetUrl();
                if (string.IsNullOrWhiteSpace(url))
                    url = JapanNoteTranslator.DefaultGoogleSheetUrl;

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

                MessageBox.Show(this,
                    "Đã sao chép toàn bộ bảng từ khóa vào bộ nhớ tạm (Clipboard)!\n\nGoogle Sheet đang được mở trên trình duyệt. Bạn chỉ cần chọn ô A1 và ấn tổ hợp phím Ctrl+V để dán dữ liệu vào bảng.",
                    "Đã sao chép thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GoogleSheetSyncSetupWindow.xaml", ex);
                MessageBox.Show(this, "Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Dong_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = DaCapNhatThanhCong;
            Close();
        }
    }
}
