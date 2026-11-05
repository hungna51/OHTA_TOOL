using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Services;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Danh sách báo lỗi cho máy admin (tên máy trong tab admin của Sheet). Không modal: vừa xem vừa sửa trên CAD.
    /// "Đã xử lý" chuyển dòng sang tab bug_done (zip đính kèm vào Thùng rác Drive, giữ ảnh + nhật ký).
    /// </summary>
    public sealed class BugAdminWindow : Window
    {
        private static BugAdminWindow _open;
        private readonly DataGrid _grid;
        private readonly TextBox _detail;
        private readonly TextBlock _status;
        private readonly Button _anh, _nhatKy, _dinhKem, _xong, _refresh;
        private readonly Action<int> _onCountChanged;
        private List<BugAdminItem> _items = new List<BugAdminItem>();

        public static void ShowSingle(Action<int> onCountChanged)
        {
            if (_open != null)
            {
                _open.Activate();
                return;
            }
            _open = new BugAdminWindow(onCountChanged);
            try { new WindowInteropHelper(_open).Owner = Process.GetCurrentProcess().MainWindowHandle; } catch { }
            _open.Closed += (s, e) => _open = null;
            _open.Show();
        }

        private BugAdminWindow(Action<int> onCountChanged)
        {
            _onCountChanged = onCountChanged;
            Title = "OHTA TOOL — Báo lỗi từ người dùng";
            Width = 900;
            Height = 560;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Segoe UI, Meiryo UI, Yu Gothic UI");

            var root = new DockPanel { Margin = new Thickness(10) };

            var buttons = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            _anh = MakeButton("🖼  Mở ảnh", () => OpenUrl(Selected?.Anh));
            _nhatKy = MakeButton("📄  Mở nhật ký", () => OpenUrl(Selected?.ChiTiet));
            _dinhKem = MakeButton("📦  Mở file đính kèm", () => OpenUrl(Selected?.DinhKem));
            _dinhKem.ToolTip = "File bản vẽ / CSV không chia sẻ công khai: cần đăng nhập Google của chủ Drive.";
            _xong = MakeButton("✓  Đã xử lý", ResolveAsync);
            _refresh = MakeButton("⟳  Làm mới", LoadAsync);
            foreach (Button b in new[] { _anh, _nhatKy, _dinhKem, _xong, _refresh })
                buttons.Children.Add(b);
            _status = new TextBlock { Margin = new Thickness(0, 6, 0, 0), Foreground = Brushes.DimGray, TextWrapping = TextWrapping.Wrap };
            var bottom = new StackPanel();
            bottom.Children.Add(buttons);
            bottom.Children.Add(_status);
            DockPanel.SetDock(bottom, Dock.Bottom);
            root.Children.Add(bottom);

            _detail = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 150,
                Margin = new Thickness(0, 8, 0, 0)
            };
            DockPanel.SetDock(_detail, Dock.Bottom);
            root.Children.Add(_detail);

            _grid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                CanUserAddRows = false
            };
            AddColumn("Thời gian", nameof(BugAdminItem.ThoiGian), 130);
            AddColumn("Mức độ", nameof(BugAdminItem.MucDo), 80);
            AddColumn("Tab / lệnh", nameof(BugAdminItem.TabLenh), 110);
            AddColumn("Người gửi", nameof(BugAdminItem.NguoiGui), 150);
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Mô tả",
                Binding = new System.Windows.Data.Binding(nameof(BugAdminItem.MoTa)),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            _grid.SelectionChanged += (s, e) => ShowDetail();
            root.Children.Add(_grid);

            Content = root;
            Loaded += async (s, e) =>
            {
                try { await LoadAsync(); }
                catch (Exception ex) { BugLog.Write("BUGADMIN", ex); SetBusy(false, "Lỗi: " + ex.Message); }
            };
            ShowDetail();
        }

        private BugAdminItem Selected => _grid.SelectedItem as BugAdminItem;

        private async Task LoadAsync()
        {
            SetBusy(true, "Đang tải danh sách…");
            var res = await Task.Run(() => BugReportService.ListBugs());
            if (!res.ok || !res.isAdmin)
            {
                SetBusy(false, res.ok ? "Máy " + Environment.MachineName + " không có trong tab admin." : res.message);
                return;
            }
            // Chặn việc lên trước, trong cùng mức độ thì mới nhất trước (Apps Script đã trả mới nhất trước)
            _items = res.bugs.Select((b, i) => new { b, i })
                .OrderBy(x => x.b.MucDo == "Chặn việc" ? 0 : 1).ThenBy(x => x.i)
                .Select(x => x.b).ToList();
            _grid.ItemsSource = _items;
            _onCountChanged?.Invoke(_items.Count);
            SetBusy(false, _items.Count == 0 ? "Không còn báo lỗi nào. 🎉" : _items.Count + " báo lỗi chờ xử lý.");
        }

        private async Task ResolveAsync()
        {
            BugAdminItem item = Selected;
            if (item == null)
                return;
            string banSua = Prompt("Bản đã sửa lỗi này (vd 1.3.1) — để trống nếu không có:");
            if (banSua == null)
                return;
            SetBusy(true, "Đang đánh dấu đã xử lý…");
            var res = await Task.Run(() => BugReportService.Resolve(item.Id, banSua.Trim()));
            if (res.ok)
            {
                _items.Remove(item);
                _grid.ItemsSource = null;
                _grid.ItemsSource = _items;
                _onCountChanged?.Invoke(_items.Count);
            }
            SetBusy(false, res.message);
        }

        private void ShowDetail()
        {
            BugAdminItem b = Selected;
            _detail.Text = b == null
                ? ""
                : "Mô tả: " + b.MoTa + Environment.NewLine + Environment.NewLine
                  + "Lỗi: " + (b.Loi.Length == 0 ? "(không có)" : b.Loi) + Environment.NewLine
                  + "Phiên bản: " + b.PhienBan + Environment.NewLine
                  + "Máy gửi: " + b.MayGui + "   ID: " + b.Id;
            _anh.IsEnabled = b != null && b.Anh.Length > 0;
            _nhatKy.IsEnabled = b != null && b.ChiTiet.Length > 0;
            _dinhKem.IsEnabled = b != null && b.DinhKem.Length > 0;
            _xong.IsEnabled = b != null;
        }

        private void SetBusy(bool busy, string text)
        {
            _status.Text = text ?? "";
            _refresh.IsEnabled = !busy;
            if (busy)
                _xong.IsEnabled = false;
            else
                ShowDetail();
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { _status.Text = "Không mở được link: " + ex.Message; }
        }

        private string Prompt(string question)
        {
            var box = new TextBox { Margin = new Thickness(0, 6, 0, 10) };
            var ok = new Button { Content = "OK", Width = 80, IsDefault = true };
            var cancel = new Button { Content = "Huỷ", Width = 80, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            row.Children.Add(ok);
            row.Children.Add(cancel);
            var panel = new StackPanel { Margin = new Thickness(12) };
            panel.Children.Add(new TextBlock { Text = question, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(box);
            panel.Children.Add(row);
            var dlg = new Window
            {
                Title = "Đã xử lý",
                Owner = this,
                Content = panel,
                Width = 360,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = FontFamily
            };
            ok.Click += (s, e) => dlg.DialogResult = true;
            dlg.Loaded += (s, e) => box.Focus();
            return dlg.ShowDialog() == true ? box.Text : null;
        }

        private void AddColumn(string header, string path, double width) =>
            _grid.Columns.Add(new DataGridTextColumn { Header = header, Binding = new System.Windows.Data.Binding(path), Width = width });

        private Button MakeButton(string text, Action onClick) =>
            MakeButton(text, () => { onClick(); return Task.CompletedTask; });

        // async void trong handler: exception lọt ra sẽ làm sập CAD -> bắt hết ở đây
        private Button MakeButton(string text, Func<Task> onClick)
        {
            var b = new Button { Content = text, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 6, 0) };
            b.Click += async (s, e) =>
            {
                try { await onClick(); }
                catch (Exception ex)
                {
                    BugLog.Write("BUGADMIN", ex);
                    SetBusy(false, "Lỗi: " + ex.Message);
                }
            };
            return b;
        }
    }
}
