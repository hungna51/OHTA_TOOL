using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Tab Info (dựng sẵn trong OHTATOOL.dll, luôn nằm cuối): nhà phát triển, phiên bản Tool,
    /// phiên bản riêng + số lisp/lệnh của từng tab, nút kiểm tra cập nhật / quay về bản trước.
    /// Viết bằng code (không XAML) vì giao diện chỉ đọc, ít trạng thái.
    /// </summary>
    public sealed class InfoTabControl : UserControl, IOhtaScrollableTabView
    {
        private static readonly Brush Accent = Hex(ToolInfoCore.InfoAccentColor);
        private static readonly Brush TextMain = Hex("#263238");
        private static readonly Brush TextSub = Hex("#607D8B");
        private static readonly Brush CardBorder = Hex("#E0E6ED");

        private readonly ScrollViewer _scroller;
        private readonly TextBlock _updateStatus;
        private readonly Func<string> _getUpdateStatus;

        /// <param name="modules">Các tab đang nạp (không gồm tab Info).</param>
        /// <param name="loadedVersion">Phiên bản DLL đang chạy trong phiên CAD.</param>
        /// <param name="installedVersion">VERSION trên đĩa (khác loadedVersion = đã cài bản mới, chờ khởi động lại CAD).</param>
        public InfoTabControl(
            IList<ModuleManifest> modules,
            string loadedVersion,
            string installedVersion,
            string platformName,
            string rootDir,
            Func<string> getUpdateStatus,
            Action checkUpdate,
            Action rollback)
        {
            _getUpdateStatus = getUpdateStatus;
            Background = Hex("#F5F7FA");
            FontFamily = new FontFamily("Segoe UI, Meiryo UI, Yu Gothic UI");
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            var stack = new StackPanel();

            // 1. Đầu tab
            var header = new StackPanel();
            header.Children.Add(new TextBlock
            {
                Text = "OHTA TOOL · v" + loadedVersion,
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold
            });
            header.Children.Add(new TextBlock
            {
                Text = "Bộ công cụ ZWCAD / AutoCAD cho gia công kim loại tấm.",
                Foreground = Hex("#ECEFF1"),
                FontSize = 11.5,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            stack.Children.Add(new Border { Background = Accent, Padding = new Thickness(12, 10, 12, 10), Child = header });

            // 2. Thông tin chung
            var info = new Grid();
            info.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            string versionText = "v" + loadedVersion;
            if (UpdateVersion.IsValid(installedVersion) && UpdateVersion.Compare(installedVersion, loadedVersion) != 0)
                versionText += "  (đã cài v" + installedVersion + " — khởi động lại CAD)";
            AddRow(info, "Nhà phát triển", Text(ToolInfoCore.Developer, bold: true));
            AddRow(info, "Phiên bản", Text(versionText, bold: true));
            AddRow(info, "Nền tảng CAD", Text(platformName));
            AddRow(info, "Thư mục gốc", Text(rootDir ?? "(không tìm thấy)"));
            AddRow(info, "GitHub", Link(ToolInfoCore.RepoUrl));
            stack.Children.Add(Card("THÔNG TIN", info));

            // 3. Phiên bản từng tab
            var tabs = new Grid();
            tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            foreach (ModuleManifest m in modules ?? new List<ModuleManifest>())
            {
                int row = tabs.RowDefinitions.Count;
                tabs.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                string version = m.VersionLabel;
                Place(tabs, Text(m.TabTitle, bold: true), row, 0);
                Place(tabs, Text(version.Length == 0 ? "—" : version), row, 1);
                Place(tabs, Text(ToolInfoCore.DescribeContents(m), sub: true), row, 2);
            }
            stack.Children.Add(Card("CÁC TAB", tabs));

            // 4. Cập nhật
            var update = new StackPanel();
            _updateStatus = Text(string.Empty, sub: true);
            _updateStatus.Margin = new Thickness(0, 0, 0, 8);
            update.Children.Add(_updateStatus);
            var buttons = new WrapPanel();
            Button check = MakeButton("⟳  Kiểm tra cập nhật", primary: true);
            check.Click += (s, e) => { checkUpdate?.Invoke(); RefreshStatus(); };
            Button back = MakeButton("↶  Quay về bản trước", primary: false);
            back.ToolTip = "Quay về bản trước lần tự cập nhật gần nhất (OHTAROLLBACK) — có hỏi lại trước khi chạy.";
            back.Click += (s, e) => rollback?.Invoke();
            buttons.Children.Add(check);
            buttons.Children.Add(back);
            update.Children.Add(buttons);
            stack.Children.Add(Card("CẬP NHẬT", update));

            // 5. Có gì mới (mục đầu CHANGELOG.md)
            string notes = ToolInfoCore.ReadLatestChangelogEntry(rootDir, out string notesVersion);
            if (notes.Length > 0)
                stack.Children.Add(Card("CÓ GÌ MỚI · v" + notesVersion, Text(notes)));

            _scroller = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = stack
            };
            Content = _scroller;

            RefreshStatus();
            IsVisibleChanged += (s, e) => { if (IsVisible) RefreshStatus(); };
        }

        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (wheelDelta == 0)
                return;
            _scroller.ScrollToVerticalOffset(_scroller.VerticalOffset - (wheelDelta / 120.0) * 48.0);
        }

        private void RefreshStatus()
        {
            try { _updateStatus.Text = _getUpdateStatus?.Invoke() ?? string.Empty; }
            catch { _updateStatus.Text = string.Empty; }
        }

        // ---------------------------------------------------------------- dựng giao diện

        private static Border Card(string title, UIElement body)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Accent,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            });
            panel.Children.Add(body);
            return new Border
            {
                Background = Brushes.White,
                BorderBrush = CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 10),
                Margin = new Thickness(8, 8, 8, 0),
                Child = panel
            };
        }

        private static void AddRow(Grid grid, string label, UIElement value)
        {
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            TextBlock l = Text(label, sub: true);
            l.Margin = new Thickness(0, 2, 10, 2);
            Place(grid, l, row, 0);
            Place(grid, value, row, 1);
        }

        private static void Place(Grid grid, UIElement e, int row, int col)
        {
            if (e is FrameworkElement fe && fe.Margin == default(Thickness))
                fe.Margin = new Thickness(0, 2, col == 0 ? 0 : 10, 2);
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            grid.Children.Add(e);
        }

        private static TextBlock Text(string text, bool bold = false, bool sub = false) => new TextBlock
        {
            Text = text ?? string.Empty,
            Foreground = sub ? TextSub : TextMain,
            FontSize = 11.5,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap
        };

        private static TextBlock Link(string url)
        {
            var link = new Hyperlink(new Run(url.Replace("https://", string.Empty)));
            link.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
                catch { }
            };
            var tb = Text(string.Empty);
            tb.Inlines.Add(link);
            return tb;
        }

        private static Button MakeButton(string text, bool primary)
        {
            return new Button
            {
                Content = text,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 4),
                FontSize = 11.5,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = primary ? Accent : Brushes.White,
                Foreground = primary ? Brushes.White : TextMain,
                BorderBrush = primary ? Accent : Hex("#CFD8DC")
            };
        }

        private static Brush Hex(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
