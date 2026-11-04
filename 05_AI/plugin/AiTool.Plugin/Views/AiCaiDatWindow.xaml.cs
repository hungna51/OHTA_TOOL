using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using AiTool.Core;

namespace AiTool.Views
{
    /// <summary>
    /// Cửa sổ cài đặt tab AI (nút ⚙): API key (data\ai.key) và file hướng dẫn AI chạy
    /// (data\huong_dan_ai.md). Chỉ ghi file trong thư mục tab, không đụng bản vẽ.
    /// </summary>
    public partial class AiCaiDatWindow : Window
    {
        private readonly string _thuMuc;
        private readonly string _macDinh;

        public AiCaiDatWindow(string thuMucModule, string huongDanMacDinh, Brush mauTab)
        {
            InitializeComponent();
            _thuMuc = thuMucModule;
            _macDinh = huongDanMacDinh ?? string.Empty;
            if (mauTab != null) NutLuu.Background = mauTab;

            DuongDanKey.Text = Path.Combine(_thuMuc, "data", "ai.key");
            DuongDanHuongDan.Text = DuLieuTinh.FileHuongDan(_thuMuc);
            HienTrangThaiKey();
            try { HuongDan.Text = DuLieuTinh.DocHuongDan(_thuMuc, _macDinh); }
            catch (Exception ex) { HuongDan.Text = _macDinh; MessageBox.Show(this, "Không đọc được file hướng dẫn: " + ex.Message, "AI"); }
        }

        private void HienTrangThaiKey()
        {
            string k = DuLieuTinh.DocKey(_thuMuc);
            TrangThaiKey.Text = k == null ? "🔴 Chưa có API key" : "✅ Đã có key: " + DuLieuTinh.AnKey(k);
            NutXoaKey.IsEnabled = k != null;
        }

        private void Luu(object sender, RoutedEventArgs e)
        {
            try
            {
                string moi = KeyMoi.Password?.Trim();
                if (!string.IsNullOrEmpty(moi))
                {
                    if (!moi.StartsWith("sk-ant-", StringComparison.Ordinal) &&
                        MessageBox.Show(this, "Key không bắt đầu bằng \"sk-ant-\". Vẫn lưu?", "AI",
                                        MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        return;
                    DuLieuTinh.GhiKey(_thuMuc, moi);
                }
                DuLieuTinh.GhiHuongDan(_thuMuc, HuongDan.Text);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không lưu được: " + ex.Message, "AI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void XoaKey(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Xoá API key khỏi máy này?", "AI", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try { DuLieuTinh.XoaKey(_thuMuc); }
            catch (Exception ex) { MessageBox.Show(this, "Không xoá được: " + ex.Message, "AI"); }
            KeyMoi.Clear();
            HienTrangThaiKey();
        }

        private void KhoiPhuc(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Thay nội dung đang sửa bằng bản mặc định? (Bấm Lưu mới ghi vào file.)", "AI",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                HuongDan.Text = _macDinh;
        }

        private void MoNotepad(object sender, RoutedEventArgs e)
        {
            try
            {
                string f = DuLieuTinh.FileHuongDan(_thuMuc);
                if (!File.Exists(f)) DuLieuTinh.GhiHuongDan(_thuMuc, HuongDan.Text);
                Process.Start("notepad.exe", "\"" + f + "\"");
                MessageBox.Show(this, "Sửa xong trong Notepad thì lưu file, rồi đóng cửa sổ này bằng Huỷ\n(bấm Lưu ở đây sẽ ghi đè bằng nội dung trong ô).", "AI");
            }
            catch (Exception ex) { MessageBox.Show(this, "Không mở được Notepad: " + ex.Message, "AI"); }
        }

        private void MoConsole(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://console.anthropic.com/settings/keys") { UseShellExecute = true }); }
            catch (Exception) { }
        }
    }
}
