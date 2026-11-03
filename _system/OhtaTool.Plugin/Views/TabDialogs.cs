using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.ViewModels;

namespace OhtaTool.Plugin.Views
{
    // Các cửa sổ của tab chung (ADR 0002): thêm lisp, sửa nút, cài đặt tab, hỏi tên.
    // Dựng bằng code (không XAML) cho gọn và chạy giống nhau trên ZWCAD/AutoCAD.

    internal static class DialogKit
    {
        public static readonly Brush TextBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x26, 0x32, 0x38)));
        public static readonly Brush MutedBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x61, 0x6E, 0x7C)));
        public static readonly Brush BorderBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xCF, 0xD8, 0xDC)));
        public static readonly Brush ErrorBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)));

        private static Brush Freeze(SolidColorBrush b)
        {
            b.Freeze();
            return b;
        }

        public static Window NewWindow(string title, double width)
        {
            return new Window
            {
                Title = title,
                Width = width,
                SizeToContent = SizeToContent.Height,
                MaxHeight = 720,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                FontFamily = new FontFamily("Segoe UI, Meiryo UI, Yu Gothic UI"),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xFA))
            };
        }

        public static TextBlock Label(string text, double top = 8)
        {
            return new TextBlock { Text = text, Foreground = MutedBrush, FontSize = 11.5, Margin = new Thickness(0, top, 0, 3) };
        }

        public static TextBox Input(string text = "")
        {
            return new TextBox { Text = text ?? string.Empty, Padding = new Thickness(4, 3, 4, 3), BorderBrush = BorderBrush };
        }

        public static Button Btn(string text, RoutedEventHandler onClick, bool primary = false, Brush accent = null)
        {
            var b = new Button
            {
                Content = text,
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(6, 0, 0, 0),
                MinWidth = 70,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            if (primary)
            {
                b.Background = accent ?? new SolidColorBrush(Color.FromRgb(0x1E, 0x88, 0xE5));
                b.Foreground = Brushes.White;
                b.FontWeight = FontWeights.SemiBold;
                b.IsDefault = true;
            }
            if (onClick != null)
                b.Click += onClick;
            return b;
        }

        public static StackPanel ButtonRow(params UIElement[] buttons)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            foreach (UIElement b in buttons)
                row.Children.Add(b);
            return row;
        }

        public static TextBlock ErrorText()
        {
            return new TextBlock { Foreground = ErrorBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), Visibility = Visibility.Collapsed };
        }

        public static void ShowErrors(TextBlock target, IList<string> errors)
        {
            target.Text = string.Join("\n", errors);
            target.Visibility = errors.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public static string PickFile(string title, string filter, string initialDir = null)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true };
            if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
                dlg.InitialDirectory = initialDir;
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }

    /// <summary>Bộ ô nhập thông tin một nút lệnh (dùng chung cho cửa sổ thêm và sửa).</summary>
    internal sealed class ToolFieldsEditor
    {
        private readonly TextBox _name;
        private readonly TextBox _command;
        private readonly TextBox _badge;
        private readonly ComboBox _group;
        private readonly TextBox _description;
        private readonly TextBox _icon;
        private readonly string _moduleDir;

        public ToolFieldsEditor(ToolItem tool, IEnumerable<string> groups, string moduleDir, bool commandEditable)
        {
            _moduleDir = moduleDir;
            Panel = new StackPanel();

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            _name = DialogKit.Input(tool.Name);
            _command = DialogKit.Input(tool.Command);
            _command.FontFamily = new FontFamily("Consolas");
            _command.IsReadOnly = !commandEditable;
            if (!commandEditable)
                _command.Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF1));
            _badge = DialogKit.Input(tool.Badge);
            _badge.MaxLength = 4;
            _badge.CharacterCasing = CharacterCasing.Upper;

            AddCell(grid, 0, "Tên hiển thị", _name);
            AddCell(grid, 2, "Lệnh CAD", _command);
            AddCell(grid, 4, "Chữ tắt", _badge);
            Panel.Children.Add(grid);

            Panel.Children.Add(DialogKit.Label("Nhóm (chọn hoặc gõ tên nhóm mới; để trống = Khác)"));
            _group = new ComboBox { IsEditable = true, Text = tool.Category ?? string.Empty, Padding = new Thickness(4, 3, 4, 3) };
            foreach (string g in groups ?? Enumerable.Empty<string>())
                _group.Items.Add(g);
            _group.Text = tool.Category ?? string.Empty;
            Panel.Children.Add(_group);

            Panel.Children.Add(DialogKit.Label("Mô tả"));
            _description = DialogKit.Input(tool.Description);
            _description.TextWrapping = TextWrapping.Wrap;
            _description.AcceptsReturn = false;
            Panel.Children.Add(_description);

            Panel.Children.Add(DialogKit.Label("Biểu tượng (.png / .ico — để trống thì dùng chữ tắt)"));
            var iconRow = new DockPanel();
            var browse = DialogKit.Btn("Chọn ảnh...", (s, e) =>
            {
                string start = string.IsNullOrEmpty(_moduleDir) ? null : Path.Combine(_moduleDir, "icons");
                string path = DialogKit.PickFile("Chọn ảnh biểu tượng", "Ảnh (*.png;*.ico)|*.png;*.ico", start);
                if (path != null)
                    _icon.Text = path;
            });
            var clear = DialogKit.Btn("Bỏ", (s, e) => _icon.Text = string.Empty);
            clear.MinWidth = 40;
            DockPanel.SetDock(clear, Dock.Right);
            DockPanel.SetDock(browse, Dock.Right);
            iconRow.Children.Add(clear);
            iconRow.Children.Add(browse);
            _icon = DialogKit.Input(tool.Icon);
            _icon.IsReadOnly = true;
            iconRow.Children.Add(_icon);
            Panel.Children.Add(iconRow);
        }

        public StackPanel Panel { get; }

        public string Command => (_command.Text ?? string.Empty).Trim().ToUpperInvariant();

        public ToolItem ToToolItem()
        {
            string cmd = Command;
            string name = (_name.Text ?? string.Empty).Trim();
            string badge = (_badge.Text ?? string.Empty).Trim();
            return new ToolItem
            {
                Name = name.Length > 0 ? name : cmd,
                Command = cmd,
                Badge = badge.Length > 0 ? badge : LispImport.SuggestBadge(cmd),
                Category = (_group.Text ?? string.Empty).Trim(),
                Description = (_description.Text ?? string.Empty).Trim(),
                Icon = (_icon.Text ?? string.Empty).Trim()
            };
        }

        private static void AddCell(Grid grid, int column, string label, Control input)
        {
            var cell = new StackPanel();
            cell.Children.Add(DialogKit.Label(label, 0));
            cell.Children.Add(input);
            Grid.SetColumn(cell, column);
            grid.Children.Add(cell);
        }
    }

    public sealed class AddLispResult
    {
        public string SourcePath { get; set; }
        public bool LoadOnStartup { get; set; }
        public List<ToolItem> Tools { get; set; }
    }

    /// <summary>Cửa sổ nút ＋: chọn file .lsp, tích các lệnh c:XXX muốn thành nút, điền thông tin.</summary>
    public sealed class AddLispWindow : Window
    {
        private readonly List<string> _groups;
        private readonly TextBox _filePath;
        private readonly TextBlock _info;
        private readonly StackPanel _rows;
        private readonly CheckBox _startup;
        private readonly TextBlock _error;
        private readonly List<Tuple<CheckBox, ToolFieldsEditor>> _editors = new List<Tuple<CheckBox, ToolFieldsEditor>>();

        public AddLispWindow(List<string> groups, Brush accent)
        {
            _groups = groups ?? new List<string>();
            Window w = DialogKit.NewWindow("Thêm lisp vào tab", 500);
            Title = w.Title; Width = w.Width; SizeToContent = w.SizeToContent; MaxHeight = w.MaxHeight;
            WindowStartupLocation = w.WindowStartupLocation; ResizeMode = w.ResizeMode; ShowInTaskbar = false;
            FontFamily = w.FontFamily; FontSize = w.FontSize; Background = w.Background;

            var root = new DockPanel { Margin = new Thickness(14) };

            var top = new StackPanel();
            DockPanel.SetDock(top, Dock.Top);
            top.Children.Add(DialogKit.Label("File lisp (.lsp) — sẽ được chép vào thư mục lisp/ của tab", 0));
            var fileRow = new DockPanel();
            var browse = DialogKit.Btn("Chọn file...", (s, e) => PickLisp());
            DockPanel.SetDock(browse, Dock.Right);
            fileRow.Children.Add(browse);
            _filePath = DialogKit.Input();
            _filePath.IsReadOnly = true;
            fileRow.Children.Add(_filePath);
            top.Children.Add(fileRow);
            _info = new TextBlock { Foreground = DialogKit.MutedBrush, Margin = new Thickness(0, 8, 0, 4), TextWrapping = TextWrapping.Wrap, Text = "Chưa chọn file." };
            top.Children.Add(_info);
            root.Children.Add(top);

            var bottom = new StackPanel();
            DockPanel.SetDock(bottom, Dock.Bottom);
            _startup = new CheckBox { Content = "Nạp khi mở CAD (gõ lệnh trên dòng lệnh được, không cần bấm nút)", IsChecked = true, Margin = new Thickness(0, 10, 0, 0) };
            bottom.Children.Add(_startup);
            _error = DialogKit.ErrorText();
            bottom.Children.Add(_error);
            bottom.Children.Add(DialogKit.ButtonRow(
                DialogKit.Btn("Thêm", (s, e) => Confirm(), true, accent),
                DialogKit.Btn("Huỷ", (s, e) => { DialogResult = false; })));
            root.Children.Add(bottom);

            _rows = new StackPanel();
            root.Children.Add(new ScrollViewer { Content = _rows, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 440 });

            Content = root;
            Loaded += (s, e) => PickLisp();
        }

        /// <summary>Kiểm tra trùng lệnh; trả về danh sách lỗi (rỗng = hợp lệ).</summary>
        public Func<IList<string>, List<string>> ValidateCommands { get; set; }

        public AddLispResult Result { get; private set; }

        private void PickLisp()
        {
            string path = DialogKit.PickFile("Chọn file lisp", "AutoLISP (*.lsp)|*.lsp");
            if (path == null)
                return;

            List<string> commands;
            try
            {
                commands = LispImport.ScanCommandsInFile(path);
            }
            catch (Exception ex)
            {
                DialogKit.ShowErrors(_error, new[] { "Không đọc được file: " + ex.Message });
                return;
            }

            _filePath.Text = path;
            _rows.Children.Clear();
            _editors.Clear();
            DialogKit.ShowErrors(_error, new string[0]);

            string stem = Path.GetFileNameWithoutExtension(path);
            if (commands.Count == 0)
            {
                _info.Text = "Không tìm thấy lệnh (defun c:...) trong file. Nhập tên lệnh bằng tay bên dưới.";
                AddRow(new ToolItem { Name = stem }, true, true);
            }
            else
            {
                _info.Text = $"Tìm thấy {commands.Count} lệnh. Tích lệnh muốn thêm thành nút.";
                foreach (string cmd in commands)
                    AddRow(new ToolItem { Name = cmd, Command = cmd, Badge = LispImport.SuggestBadge(cmd) }, false, true);
            }
        }

        private void AddRow(ToolItem tool, bool commandEditable, bool isChecked)
        {
            var editor = new ToolFieldsEditor(tool, _groups, null, commandEditable);
            var check = new CheckBox
            {
                Content = string.IsNullOrEmpty(tool.Command) ? "Lệnh nhập tay" : "Lệnh " + tool.Command,
                IsChecked = isChecked,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            editor.Panel.IsEnabled = isChecked;
            check.Checked += (s, e) => editor.Panel.IsEnabled = true;
            check.Unchecked += (s, e) => editor.Panel.IsEnabled = false;

            var card = new StackPanel();
            card.Children.Add(check);
            card.Children.Add(editor.Panel);
            _rows.Children.Add(new Border
            {
                Child = card,
                Background = Brushes.White,
                BorderBrush = DialogKit.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 10),
                Margin = new Thickness(0, 0, 0, 8)
            });
            _editors.Add(Tuple.Create(check, editor));
        }

        private void Confirm()
        {
            if (string.IsNullOrEmpty(_filePath.Text))
            {
                DialogKit.ShowErrors(_error, new[] { "Chưa chọn file lisp." });
                return;
            }

            List<ToolItem> tools = _editors.Where(t => t.Item1.IsChecked == true).Select(t => t.Item2.ToToolItem()).ToList();
            if (tools.Count == 0)
            {
                DialogKit.ShowErrors(_error, new[] { "Chưa tích lệnh nào." });
                return;
            }

            List<string> errors = ValidateCommands?.Invoke(tools.Select(t => t.Command).ToList()) ?? new List<string>();
            DialogKit.ShowErrors(_error, errors);
            if (errors.Count > 0)
                return;

            Result = new AddLispResult { SourcePath = _filePath.Text, LoadOnStartup = _startup.IsChecked == true, Tools = tools };
            DialogResult = true;
        }
    }

    /// <summary>Cửa sổ sửa một nút lệnh (menu chuột phải → Sửa).</summary>
    public sealed class ToolEditWindow : Window
    {
        private readonly ToolFieldsEditor _editor;
        private readonly TextBlock _error;

        public ToolEditWindow(ToolItem tool, string moduleDir, List<string> groups, Brush accent)
        {
            Window w = DialogKit.NewWindow("Sửa nút lệnh", 460);
            Title = w.Title; Width = w.Width; SizeToContent = w.SizeToContent; MaxHeight = w.MaxHeight;
            WindowStartupLocation = w.WindowStartupLocation; ResizeMode = w.ResizeMode; ShowInTaskbar = false;
            FontFamily = w.FontFamily; FontSize = w.FontSize; Background = w.Background;

            var root = new StackPanel { Margin = new Thickness(14) };
            _editor = new ToolFieldsEditor(tool, groups, moduleDir, true);
            root.Children.Add(_editor.Panel);
            if (!string.IsNullOrEmpty(tool.LispFile))
                root.Children.Add(new TextBlock { Text = "File lisp: " + tool.LispFile, Foreground = DialogKit.MutedBrush, Margin = new Thickness(0, 8, 0, 0) });
            _error = DialogKit.ErrorText();
            root.Children.Add(_error);
            root.Children.Add(DialogKit.ButtonRow(
                DialogKit.Btn("Lưu", (s, e) => Confirm(), true, accent),
                DialogKit.Btn("Huỷ", (s, e) => { DialogResult = false; })));
            Content = root;
        }

        public Func<IList<string>, List<string>> ValidateCommands { get; set; }

        public ToolItem Result { get; private set; }

        private void Confirm()
        {
            ToolItem item = _editor.ToToolItem();
            List<string> errors = ValidateCommands?.Invoke(new[] { item.Command }) ?? new List<string>();
            DialogKit.ShowErrors(_error, errors);
            if (errors.Count > 0)
                return;
            Result = item;
            DialogResult = true;
        }
    }

    public sealed class TabSettingsResult
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string AccentColor { get; set; }
        public List<string> DeletedGroups { get; } = new List<string>();
        public List<KeyValuePair<string, string>> Renames { get; } = new List<KeyValuePair<string, string>>();
        public List<string> GroupOrder { get; } = new List<string>();
    }

    /// <summary>Cửa sổ ⚙ cài đặt tab: màu, tiêu đề, mô tả, nhóm, mở thư mục.</summary>
    public sealed class TabSettingsWindow : Window
    {
        private static readonly string[] Swatches =
        {
            "#1E88E5", "#2E7D32", "#EF6C00", "#6A1B9A", "#00838F", "#C62828", "#323639", "#0277BD"
        };

        private sealed class GroupEntry
        {
            public string Original;
            public string Current;
            public override string ToString() => Current;
        }

        private readonly TextBox _title;
        private readonly TextBox _description;
        private readonly TextBox _hex;
        private readonly Border _preview;
        private readonly TextBlock _previewTitle;
        private readonly TextBlock _previewDesc;
        private readonly ListBox _groupList;
        private readonly TextBlock _error;
        private readonly List<string> _deleted = new List<string>();

        public TabSettingsWindow(ModuleManifest manifest, List<string> groups, Action openFolder)
        {
            Window w = DialogKit.NewWindow("Cài đặt tab " + manifest.TabTitle, 440);
            Title = w.Title; Width = w.Width; SizeToContent = w.SizeToContent; MaxHeight = w.MaxHeight;
            WindowStartupLocation = w.WindowStartupLocation; ResizeMode = w.ResizeMode; ShowInTaskbar = false;
            FontFamily = w.FontFamily; FontSize = w.FontSize; Background = w.Background;

            var root = new StackPanel { Margin = new Thickness(14) };

            // Xem trước header
            _previewTitle = new TextBlock { Foreground = Brushes.White, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            _previewDesc = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFD)), FontSize = 11.5, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap };
            var previewStack = new StackPanel();
            previewStack.Children.Add(_previewTitle);
            previewStack.Children.Add(_previewDesc);
            _preview = new Border { Child = previewStack, Padding = new Thickness(12, 10, 12, 10), CornerRadius = new CornerRadius(4) };
            root.Children.Add(_preview);

            root.Children.Add(DialogKit.Label("Tiêu đề", 10));
            _title = DialogKit.Input(manifest.Title);
            _title.TextChanged += (s, e) => UpdatePreview();
            root.Children.Add(_title);

            root.Children.Add(DialogKit.Label("Mô tả"));
            _description = DialogKit.Input(manifest.Description);
            _description.TextChanged += (s, e) => UpdatePreview();
            root.Children.Add(_description);

            root.Children.Add(DialogKit.Label("Màu chủ đạo"));
            var colorRow = new DockPanel();
            _hex = DialogKit.Input(manifest.AccentColor);
            _hex.Width = 80;
            _hex.FontFamily = new FontFamily("Consolas");
            _hex.TextChanged += (s, e) => UpdatePreview();
            DockPanel.SetDock(_hex, Dock.Right);
            colorRow.Children.Add(_hex);
            var swatchPanel = new WrapPanel();
            foreach (string hex in Swatches)
            {
                string h = hex;
                var sw = new Button
                {
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(0, 0, 6, 0),
                    Background = ModuleTabViewModel.ParseColorBrush(h, "#1E88E5"),
                    BorderBrush = DialogKit.BorderBrush,
                    ToolTip = h,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                sw.Click += (s, e) => _hex.Text = h;
                swatchPanel.Children.Add(sw);
            }
            colorRow.Children.Add(swatchPanel);
            root.Children.Add(colorRow);

            // Nhóm
            root.Children.Add(DialogKit.Label("Nhóm lệnh (thứ tự hiển thị trên tab)", 12));
            var groupRow = new DockPanel();
            var groupButtons = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
            DockPanel.SetDock(groupButtons, Dock.Right);
            groupButtons.Children.Add(SmallButton("▲ Lên", (s, e) => MoveSelected(-1)));
            groupButtons.Children.Add(SmallButton("▼ Xuống", (s, e) => MoveSelected(1)));
            groupButtons.Children.Add(SmallButton("Đổi tên", (s, e) => RenameSelected()));
            groupButtons.Children.Add(SmallButton("Thêm", (s, e) => AddGroup()));
            groupButtons.Children.Add(SmallButton("Xoá", (s, e) => DeleteSelected()));
            groupRow.Children.Add(groupButtons);
            _groupList = new ListBox { Height = 130, BorderBrush = DialogKit.BorderBrush };
            foreach (string g in groups ?? new List<string>())
                _groupList.Items.Add(new GroupEntry { Original = g, Current = g });
            groupRow.Children.Add(_groupList);
            root.Children.Add(groupRow);
            root.Children.Add(new TextBlock
            {
                Text = "Xoá nhóm: các lệnh trong nhóm chuyển về nhóm \"Khác\".",
                Foreground = DialogKit.MutedBrush,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0)
            });

            var folder = DialogKit.Btn("📁 Mở thư mục tab", (s, e) => openFolder?.Invoke());
            folder.Margin = new Thickness(0, 12, 0, 0);
            folder.HorizontalAlignment = HorizontalAlignment.Left;
            root.Children.Add(folder);

            _error = DialogKit.ErrorText();
            root.Children.Add(_error);
            root.Children.Add(DialogKit.ButtonRow(
                DialogKit.Btn("Lưu", (s, e) => Confirm(), true),
                DialogKit.Btn("Huỷ", (s, e) => { DialogResult = false; })));

            Content = root;
            UpdatePreview();
        }

        public TabSettingsResult Result { get; private set; }

        private static Button SmallButton(string text, RoutedEventHandler onClick)
        {
            Button b = DialogKit.Btn(text, onClick);
            b.Margin = new Thickness(0, 0, 0, 4);
            b.MinWidth = 76;
            return b;
        }

        private static bool IsValidHex(string hex) => Regex.IsMatch((hex ?? string.Empty).Trim(), "^#[0-9A-Fa-f]{6}$");

        private void UpdatePreview()
        {
            if (_preview == null || _hex == null)
                return;
            _previewTitle.Text = _title?.Text;
            _previewDesc.Text = _description?.Text;
            if (IsValidHex(_hex.Text))
                _preview.Background = ModuleTabViewModel.ParseColorBrush(_hex.Text.Trim(), "#1E88E5");
        }

        private void MoveSelected(int delta)
        {
            int i = _groupList.SelectedIndex;
            int j = i + delta;
            if (i < 0 || j < 0 || j >= _groupList.Items.Count)
                return;
            object item = _groupList.Items[i];
            _groupList.Items.RemoveAt(i);
            _groupList.Items.Insert(j, item);
            _groupList.SelectedIndex = j;
        }

        private void RenameSelected()
        {
            if (!(_groupList.SelectedItem is GroupEntry entry))
                return;
            string name = InputDialog.Ask("Đổi tên nhóm", "Tên mới:", entry.Current, null, this);
            if (!TryValidateName(name, entry))
                return;
            entry.Current = name.Trim();
            _groupList.Items.Refresh();
        }

        private void AddGroup()
        {
            string name = InputDialog.Ask("Thêm nhóm", "Tên nhóm mới:", string.Empty, null, this);
            if (!TryValidateName(name, null))
                return;
            _groupList.Items.Add(new GroupEntry { Original = null, Current = name.Trim() });
            _groupList.SelectedIndex = _groupList.Items.Count - 1;
        }

        private void DeleteSelected()
        {
            if (!(_groupList.SelectedItem is GroupEntry entry))
                return;
            if (entry.Original != null)
                _deleted.Add(entry.Original);
            _groupList.Items.Remove(entry);
        }

        private bool TryValidateName(string name, GroupEntry self)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            string n = name.Trim();
            string err = null;
            if (ModuleGrouping.IsUngroupedName(n))
                err = "\"Khác\" là nhóm mặc định, không dùng làm tên nhóm.";
            else if (_groupList.Items.OfType<GroupEntry>().Any(g => g != self && string.Equals(g.Current, n, StringComparison.OrdinalIgnoreCase)))
                err = $"Đã có nhóm \"{n}\".";
            DialogKit.ShowErrors(_error, err == null ? new string[0] : new[] { err });
            return err == null;
        }

        private void Confirm()
        {
            if (!IsValidHex(_hex.Text))
            {
                DialogKit.ShowErrors(_error, new[] { "Mã màu phải có dạng #RRGGBB, ví dụ #2E7D32." });
                return;
            }
            if (string.IsNullOrWhiteSpace(_title.Text))
            {
                DialogKit.ShowErrors(_error, new[] { "Tiêu đề không được để trống." });
                return;
            }

            var r = new TabSettingsResult
            {
                Title = _title.Text.Trim(),
                Description = (_description.Text ?? string.Empty).Trim(),
                AccentColor = _hex.Text.Trim().ToUpperInvariant()
            };
            r.DeletedGroups.AddRange(_deleted);
            foreach (GroupEntry g in _groupList.Items.OfType<GroupEntry>())
            {
                if (g.Original != null && !string.Equals(g.Original, g.Current, StringComparison.Ordinal))
                    r.Renames.Add(new KeyValuePair<string, string>(g.Original, g.Current));
                r.GroupOrder.Add(g.Current);
            }
            Result = r;
            DialogResult = true;
        }
    }

    /// <summary>Hộp hỏi một dòng chữ.</summary>
    public static class InputDialog
    {
        public static string Ask(string title, string prompt, string defaultValue, Brush accent, Window owner = null)
        {
            Window w = DialogKit.NewWindow(title, 340);
            if (owner != null)
            {
                w.Owner = owner;
                w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(DialogKit.Label(prompt, 0));
            TextBox input = DialogKit.Input(defaultValue);
            root.Children.Add(input);
            string result = null;
            root.Children.Add(DialogKit.ButtonRow(
                DialogKit.Btn("OK", (s, e) => { result = input.Text; w.DialogResult = true; }, true, accent),
                DialogKit.Btn("Huỷ", (s, e) => { w.DialogResult = false; })));
            w.Content = root;
            w.Loaded += (s, e) => { input.Focus(); input.SelectAll(); };

            bool? ok = owner != null ? w.ShowDialog() : CadCompat.ShowModalDialog(w);
            return ok == true ? result : null;
        }
    }
}
