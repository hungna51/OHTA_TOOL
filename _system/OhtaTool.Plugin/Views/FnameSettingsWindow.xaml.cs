using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace OhtaTool.Plugin.Views
{
    public partial class FnameSettingsWindow : Window
    {
        private readonly string _dataFolder;
        private readonly Func<Task<string>> _updateData;

        public bool LockZInput { get; set; } = true;
        public bool AutoIncrementN { get; set; } = true;

        public FnameSettingsWindow() : this(true, true, null, null)
        {
        }

        public FnameSettingsWindow(bool lockZ, bool autoInc, string dataFolder, Func<Task<string>> updateData)
        {
            InitializeComponent();
            _dataFolder = dataFolder;
            _updateData = updateData;
            LockZInput = lockZ;
            AutoIncrementN = autoInc;

            ChkLockZ.IsChecked = lockZ;
            ChkAutoIncN.IsChecked = autoInc;

            var logo = IconLoader.GetSettingsLogo();
            if (logo != null)
            {
                Icon = logo;
                if (HeaderLogoImage != null)
                    HeaderLogoImage.Source = logo;
            }
        }

        private async void OnUpdateDataClick(object sender, RoutedEventArgs e)
        {
            if (_updateData == null)
                return;

            BtnUpdateData.IsEnabled = false;
            TxtUpdateStatus.Text = "Đang tải dữ liệu từ Google Sheet...";
            try
            {
                TxtUpdateStatus.Text = await _updateData();
            }
            catch (Exception ex) // async void: lỗi lọt ra ngoài sẽ làm sập CAD
            {
                TxtUpdateStatus.Text = "Lỗi cập nhật: " + ex.GetBaseException().Message;
            }
            finally
            {
                BtnUpdateData.IsEnabled = true;
            }
        }

        /// <summary>Mở cửa sổ thêm / cập nhật vật liệu (tham số = cửa sổ cha).</summary>
        public Action<Window> OpenMaterialEditor { get; set; }

        /// <summary>Điền tab Dữ liệu / Thông tin: trạng thái cấu hình ghi Sheet, đường dẫn cache, lần đồng bộ cuối.</summary>
        /// <summary>Cảnh báo liên kết giữa các bảng (catalog / trav_90 / tam) của lần tải dữ liệu gần nhất.</summary>
        public void SetDataWarnings(System.Collections.Generic.IList<string> warnings)
        {
            if (warnings == null || warnings.Count == 0)
            {
                TxtDataWarnings.Visibility = Visibility.Collapsed;
                return;
            }
            const int max = 8;
            string text = "⚠ Dữ liệu Sheet có " + warnings.Count + " chỗ cần xem lại:\n• "
                + string.Join("\n• ", System.Linq.Enumerable.Take(warnings, max))
                + (warnings.Count > max ? $"\n… và {warnings.Count - max} chỗ khác." : string.Empty);
            TxtDataWarnings.Text = text;
            TxtDataWarnings.Visibility = Visibility.Visible;
        }

        public void SetDataInfo(bool writeConfigured, string cacheFolder, string lastSync)
        {
            TxtWriteConfig.Text = writeConfigured
                ? "✔ Đã cấu hình ghi lên Google Sheet (write_url trong google_sheet.ini)."
                : "⚠ Chưa cấu hình ghi lên Google Sheet: làm theo hướng dẫn ở đầu file google_sheet_apps_script.gs (nút Mở thư mục dữ liệu) rồi điền write_url vào google_sheet.ini và write_key vào google_sheet.key.";
            TxtWriteConfig.Foreground = new System.Windows.Media.SolidColorBrush(writeConfigured
                ? System.Windows.Media.Color.FromRgb(0x04, 0x78, 0x57)
                : System.Windows.Media.Color.FromRgb(0xB4, 0x53, 0x09));
            TxtInfoLastSync.Text = string.IsNullOrEmpty(lastSync) ? "(chưa tải lần nào — đang dùng CSV mẫu)" : lastSync;
            TxtInfoCache.Text = cacheFolder ?? string.Empty;
            TxtInfoDataFolder.Text = _dataFolder ?? string.Empty;
        }

        // ---------- Tab Chờ phê duyệt ----------

        private OhtaTool.Plugin.ViewModels.FnameTabViewModel _approvals;

        /// <summary>Gắn ViewModel cho tab Chờ phê duyệt (danh sách, số đỏ, ẩn/hiện tab theo quyền admin).</summary>
        public void AttachApprovals(OhtaTool.Plugin.ViewModels.FnameTabViewModel vm)
        {
            _approvals = vm;
            DataContext = vm;
            RefreshApprovalLabels(null);
            // Máy admin mở Cài đặt mà danh sách chưa tải trong phiên này -> tự tải
            if (vm != null && vm.IsApprovalAdmin && vm.PendingRequests.Count == 0 && vm.PendingCount > 0)
                Loaded += (s, e) => OnReloadApprovalsClick(null, null);
        }

        private void RefreshApprovalLabels(string status)
        {
            if (_approvals == null)
                return;
            TxtInfoApproval.Text = _approvals.IsApprovalAdmin ? "Admin" : "Không phải admin";
            TxtNoRequests.Visibility = _approvals.PendingRequests.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (!string.IsNullOrEmpty(status))
                TxtApprovalStatus.Text = status;
        }

        private async void OnReloadApprovalsClick(object sender, RoutedEventArgs e)
        {
            if (_approvals == null)
                return;
            BtnReloadApprovals.IsEnabled = false;
            TxtApprovalStatus.Text = "Đang tải yêu cầu chờ duyệt...";
            TxtInfoApproval.Text = "Đang kiểm tra...";
            try
            {
                string msg = await _approvals.RefreshPendingAsync();
                RefreshApprovalLabels(msg);
                if (_approvals.IsApprovalAdmin && sender != null && !ReferenceEquals(sender, BtnReloadApprovals))
                    TabApprovals.IsSelected = true; // bấm Kiểm tra ở tab Thông tin -> chuyển sang tab duyệt
            }
            catch (Exception ex) // async void: lỗi lọt ra ngoài sẽ làm sập CAD
            {
                RefreshApprovalLabels("Lỗi: " + ex.GetBaseException().Message);
            }
            finally
            {
                BtnReloadApprovals.IsEnabled = true;
            }
        }

        private void OnApproveClick(object sender, RoutedEventArgs e) => ResolveFromButton(sender, approve: true);

        private void OnRejectClick(object sender, RoutedEventArgs e) => ResolveFromButton(sender, approve: false);

        private async void ResolveFromButton(object sender, bool approve)
        {
            if (_approvals == null || !(sender is FrameworkElement btn) || !(btn.Tag is OhtaTool.Plugin.Core.FnameUpdateRequest req))
                return;
            if (!approve && MessageBox.Show(this, "Từ chối và xoá yêu cầu này?\n\n" + req.Describe() + "\nMáy gửi: " + req.MayGui,
                    "Từ chối yêu cầu", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var panel = btn.Parent as UIElement;
            if (panel != null) panel.IsEnabled = false;
            TxtApprovalStatus.Text = approve ? "Đang duyệt..." : "Đang từ chối...";
            try
            {
                var (ok, message) = await _approvals.ResolveRequestAsync(req, approve);
                RefreshApprovalLabels(message);
                TxtApprovalStatus.Foreground = new System.Windows.Media.SolidColorBrush(ok
                    ? System.Windows.Media.Color.FromRgb(0x04, 0x78, 0x57)
                    : System.Windows.Media.Color.FromRgb(0xB9, 0x1C, 0x1C));
            }
            catch (Exception ex)
            {
                OhtaTool.Plugin.Core.BugLog.Write("OHTA/FnameSettingsWindow.xaml", ex);
                RefreshApprovalLabels("Lỗi: " + ex.GetBaseException().Message);
            }
            finally
            {
                if (panel != null) panel.IsEnabled = true;
            }
        }

        private void OnAddMaterialClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenMaterialEditor?.Invoke(this);
            }
            catch (Exception ex)
            {
                OhtaTool.Plugin.Core.BugLog.Write("OHTA/FnameSettingsWindow.xaml", ex);
                MessageBox.Show(this, "Không mở được cửa sổ vật liệu: " + ex.GetBaseException().Message,
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>Mở cửa sổ bổ sung góc bẻ thử (tham số = cửa sổ cha).</summary>
        public Action<Window> OpenTrialEditor { get; set; }

        private void OnAddTrialBendClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenTrialEditor?.Invoke(this);
            }
            catch (Exception ex)
            {
                OhtaTool.Plugin.Core.BugLog.Write("OHTA/FnameSettingsWindow.xaml", ex);
                MessageBox.Show(this, "Không mở được cửa sổ góc bẻ thử: " + ex.GetBaseException().Message,
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>Mở danh sách góc bẻ thử / nội suy (tham số = cửa sổ cha).</summary>
        public Action<Window> OpenTrialList { get; set; }

        /// <summary>Mở các ảnh bằng chứng của yêu cầu trong trình duyệt (tab Chờ phê duyệt).</summary>
        private void OnViewImagesClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement btn) || !(btn.Tag is OhtaTool.Plugin.Core.FnameUpdateRequest req))
                return;
            foreach (string url in req.ImageLinks)
            {
                try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
                catch { }
            }
        }

        private void OnTrialListClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenTrialList?.Invoke(this);
            }
            catch (Exception ex)
            {
                OhtaTool.Plugin.Core.BugLog.Write("OHTA/FnameSettingsWindow.xaml", ex);
                MessageBox.Show(this, "Không mở được danh sách góc bẻ thử: " + ex.GetBaseException().Message,
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnOpenDataFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_dataFolder) && Directory.Exists(_dataFolder))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _dataFolder,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Không tìm thấy thư mục dữ liệu tại:\n" + _dataFolder,
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            LockZInput = ChkLockZ.IsChecked == true;
            AutoIncrementN = ChkAutoIncN.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
