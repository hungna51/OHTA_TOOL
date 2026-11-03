using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Views;

namespace OhtaTool.Plugin.ViewModels
{
    /// <summary>Một thẻ nút lệnh trên tab (bọc ToolItem + ảnh đã nạp).</summary>
    public sealed class ToolCardViewModel
    {
        public ToolCardViewModel(ToolItem tool, string moduleDir)
        {
            Tool = tool;
            IconImage = LoadIcon(moduleDir, tool.Icon);
        }

        public ToolItem Tool { get; }
        public string Name => Tool.Name;
        public string Description => Tool.Description;
        public string Command => Tool.Command;
        public string Badge => Tool.Badge;
        public bool IsLocked => Tool.IsLocked;
        public ImageSource IconImage { get; }
        public Visibility IconVisibility => IconImage != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BadgeVisibility => IconImage == null ? Visibility.Visible : Visibility.Collapsed;

        private static ImageSource LoadIcon(string moduleDir, string relPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relPath) || string.IsNullOrWhiteSpace(moduleDir))
                    return null;
                string full = Path.Combine(moduleDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(full))
                    return null;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad; // không giữ khoá file trên OneDrive
                bmp.UriSource = new Uri(full, UriKind.Absolute);
                bmp.DecodePixelWidth = 72;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Một nhóm nút lệnh, thu gọn/mở được.</summary>
    public sealed class ToolGroupViewModel : INotifyPropertyChanged
    {
        private readonly Action<ToolGroupViewModel> _onToggle;
        private bool _isExpanded;

        public ToolGroupViewModel(string name, bool isUngrouped, bool showHeader, bool isExpanded, Action<ToolGroupViewModel> onToggle)
        {
            Name = name;
            IsUngrouped = isUngrouped;
            HeaderVisibility = showHeader ? Visibility.Visible : Visibility.Collapsed;
            _isExpanded = isExpanded;
            _onToggle = onToggle;
            Tools = new ObservableCollection<ToolCardViewModel>();
            ToggleCommand = new RelayCommand(_ => _onToggle?.Invoke(this));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; }
        public bool IsUngrouped { get; }
        public ObservableCollection<ToolCardViewModel> Tools { get; }
        public Visibility HeaderVisibility { get; }
        public ICommand ToggleCommand { get; }
        public string CountText => Tools.Count.ToString();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContentVisibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chevron)));
            }
        }

        public Visibility ContentVisibility => _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        public string Chevron => _isExpanded ? "▾" : "▸";
    }

    /// <summary>
    /// ViewModel điều khiển nội dung một Tab mục con trên bảng OHTA Tool Palettes.
    /// Nút ＋ thêm lisp, nhóm lệnh, menu chuột phải và cài đặt tab (ADR 0002).
    /// </summary>
    public sealed class ModuleTabViewModel : INotifyPropertyChanged
    {
        private static GroupCollapseStore _collapseStore;

        private readonly ModuleManifest _manifest;
        private readonly Action _reloadAllCallback;
        private readonly List<ToolGroup> _allGroups;
        private string _searchText = string.Empty;
        private int _filteredCount;

        public ModuleTabViewModel(ModuleManifest manifest, Action reloadAllCallback)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _reloadAllCallback = reloadAllCallback;
            _allGroups = ModuleGrouping.BuildGroups(_manifest.Tools, _manifest.Groups);

            Groups = new ObservableCollection<ToolGroupViewModel>();
            AccentBrush = ParseColorBrush(_manifest.AccentColor, "#1E88E5");
            SettingsLogo = IconLoader.GetSettingsLogo();

            ExecuteToolCommand = new RelayCommand(OnExecuteTool);
            ReloadTabsCommand = new RelayCommand(_ => _reloadAllCallback?.Invoke());
            OpenModuleFolderCommand = new RelayCommand(_ => OpenPathSafe(_manifest.DirectoryPath));
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
            AddLispCommand = new RelayCommand(_ => AddLisp());
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());

            ApplyFilter();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ModuleManifest Manifest => _manifest;
        public string Title => _manifest.Title;
        public string FolderName => _manifest.FolderName;
        public string Description => _manifest.Description;
        public string LoadWarning => _manifest.LoadWarning;
        public SolidColorBrush AccentBrush { get; }
        public ImageSource SettingsLogo { get; }
        public Visibility SettingsLogoVisibility => SettingsLogo != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SettingsTextVisibility => SettingsLogo == null ? Visibility.Visible : Visibility.Collapsed;
        public ObservableCollection<ToolGroupViewModel> Groups { get; }

        /// <summary>Tên các nhóm (không gồm "Khác") theo thứ tự hiển thị.</summary>
        public List<string> GroupNames => _allGroups.Where(g => !g.IsUngrouped).Select(g => g.Name).ToList();

        public int TotalToolCount => _manifest.Tools != null ? _manifest.Tools.Count : 0;

        public string StatusSummary =>
            TotalToolCount == 0
                ? "Chưa có lệnh • Bấm ＋ để thêm lisp"
                : (_filteredCount == TotalToolCount
                    ? $"Đang hiển thị {TotalToolCount} công cụ • {_manifest.FolderName}"
                    : $"Lọc {_filteredCount}/{TotalToolCount} công cụ • {_manifest.FolderName}");

        public Visibility EmptyStateVisibility =>
            TotalToolCount == 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility NoFilterMatchVisibility =>
            TotalToolCount > 0 && _filteredCount == 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility WarningVisibility =>
            string.IsNullOrWhiteSpace(_manifest.LoadWarning) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility ClearSearchVisibility =>
            string.IsNullOrEmpty(_searchText) ? Visibility.Collapsed : Visibility.Visible;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;
                _searchText = value ?? string.Empty;
                OnPropertyChanged(nameof(SearchText));
                OnPropertyChanged(nameof(ClearSearchVisibility));
                ApplyFilter();
            }
        }

        public ICommand ExecuteToolCommand { get; }
        public ICommand ReloadTabsCommand { get; }
        public ICommand OpenModuleFolderCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand AddLispCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        private static GroupCollapseStore CollapseStore =>
            _collapseStore ?? (_collapseStore = new GroupCollapseStore(GroupCollapseStore.DefaultPath));

        // ------------------------------------------------------------------
        // Nhóm & tìm kiếm
        // ------------------------------------------------------------------

        private void ApplyFilter()
        {
            Groups.Clear();
            string kw = (_searchText ?? string.Empty).Trim();
            bool searching = kw.Length > 0;
            // Tab chỉ có một nhóm "Khác" -> không cần tiêu đề nhóm (giống giao diện cũ)
            bool showHeaders = !(_allGroups.Count == 1 && _allGroups[0].IsUngrouped);
            _filteredCount = 0;

            foreach (ToolGroup group in _allGroups)
            {
                List<ToolItem> matches = group.Tools.Where(t => MatchesFilter(t, kw)).ToList();
                if (matches.Count == 0)
                    continue; // nhóm rỗng hoặc không khớp tìm kiếm thì ẩn

                bool expanded = searching || !CollapseStore.IsCollapsed(_manifest.Id, group.Name);
                var gvm = new ToolGroupViewModel(group.Name, group.IsUngrouped, showHeaders, expanded, OnToggleGroup);
                foreach (ToolItem t in matches)
                    gvm.Tools.Add(new ToolCardViewModel(t, _manifest.DirectoryPath));
                Groups.Add(gvm);
                _filteredCount += matches.Count;
            }

            OnPropertyChanged(nameof(StatusSummary));
            OnPropertyChanged(nameof(EmptyStateVisibility));
            OnPropertyChanged(nameof(NoFilterMatchVisibility));
        }

        private void OnToggleGroup(ToolGroupViewModel group)
        {
            group.IsExpanded = !group.IsExpanded;
            // Đang tìm kiếm thì chỉ đổi tạm, không lưu
            if (string.IsNullOrWhiteSpace(_searchText))
                CollapseStore.SetCollapsed(_manifest.Id, group.Name, !group.IsExpanded);
        }

        private static bool MatchesFilter(ToolItem item, string kw)
        {
            if (item == null)
                return false;
            if (string.IsNullOrEmpty(kw))
                return true;

            return ContainsIgnoreCase(item.Name, kw) ||
                   ContainsIgnoreCase(item.Command, kw) ||
                   ContainsIgnoreCase(item.Badge, kw) ||
                   ContainsIgnoreCase(item.Description, kw) ||
                   ContainsIgnoreCase(item.Category, kw);
        }

        private static bool ContainsIgnoreCase(string source, string keyword)
        {
            if (string.IsNullOrEmpty(source))
                return false;
            return source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ------------------------------------------------------------------
        // Chạy lệnh
        // ------------------------------------------------------------------

        private void OnExecuteTool(object parameter)
        {
            ToolItem tool = (parameter as ToolCardViewModel)?.Tool ?? parameter as ToolItem;
            if (tool == null)
                return;

            string macro = MacroBuilder.BuildToolMacro(_manifest.DirectoryPath, tool);
            if (string.IsNullOrWhiteSpace(macro))
            {
                CadCompat.WriteMessage($"\n[OHTA TOOL] Công cụ '{tool.Name}' chưa khai báo thuộc tính 'command' trong module.json.\n");
                return;
            }

            CadCompat.SendCommand(macro);
        }

        // ------------------------------------------------------------------
        // Nút ＋ thêm lisp
        // ------------------------------------------------------------------

        private void AddLisp()
        {
            var window = new AddLispWindow(GroupNames, AccentBrush);
            window.ValidateCommands = cmds => FindConflicts(cmds, null);
            if (CadCompat.ShowModalDialog(window) != true || window.Result == null)
                return;

            AddLispResult r = window.Result;
            try
            {
                string lispDir = Path.Combine(_manifest.DirectoryPath, "lisp");
                string relLisp;
                if (LispImport.IsInsideDirectory(r.SourcePath, _manifest.DirectoryPath))
                {
                    relLisp = LispImport.MakeRelative(_manifest.DirectoryPath, r.SourcePath);
                }
                else
                {
                    string fileName = Path.GetFileName(r.SourcePath);
                    string target = Path.Combine(lispDir, fileName);
                    bool overwrite = false;
                    if (File.Exists(target) && !SameBytes(target, r.SourcePath))
                    {
                        MessageBoxResult ans = MessageBox.Show(
                            $"Thư mục lisp/ của tab đã có file '{fileName}'.\n\nYes = ghi đè file cũ\nNo = giữ file cũ, lưu file mới với tên khác\nCancel = huỷ",
                            "OHTA TOOL — Trùng tên file", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        if (ans == MessageBoxResult.Cancel)
                            return;
                        if (ans == MessageBoxResult.Yes)
                            overwrite = true;
                        else
                            fileName = LispImport.NextFreeFileName(lispDir, fileName);
                    }

                    if (File.Exists(Path.Combine(lispDir, fileName)) && !overwrite)
                        relLisp = "lisp/" + fileName; // file giống hệt đã có sẵn
                    else
                        relLisp = LispImport.CopyIntoModule(_manifest.DirectoryPath, r.SourcePath, "lisp", fileName, overwrite);
                }

                foreach (ToolItem t in r.Tools)
                {
                    t.LispFile = relLisp;
                    t.Icon = ImportIcon(t.Icon);
                }

                ModuleJsonEditor.Update(_manifest.DirectoryPath, root =>
                {
                    foreach (ToolItem t in r.Tools)
                        ModuleJsonEditor.AddTool(root, t);
                    if (r.LoadOnStartup)
                        ModuleJsonEditor.AddStartupLisp(root, relLisp);
                    return true;
                });

                // Nạp ngay trong phiên CAD đang mở để gõ lệnh được luôn
                string loadExpr = MacroBuilder.BuildLoadLispExpression(_manifest.DirectoryPath, relLisp);
                if (!string.IsNullOrEmpty(loadExpr))
                    CadCompat.SendCommand(loadExpr + " (princ)\n");

                CadCompat.WriteMessage($"\n[OHTA TOOL] Đã thêm {r.Tools.Count} lệnh vào tab {_manifest.TabTitle}: {string.Join(", ", r.Tools.Select(t => t.Command))}\n");
                ReloadLater();
            }
            catch (Exception ex)
            {
                ShowError("Không thêm được lisp", ex);
            }
        }

        /// <summary>Chép ảnh vào icons/ của tab, trả về đường dẫn tương đối ("" nếu không có ảnh).</summary>
        private string ImportIcon(string iconPath)
        {
            if (string.IsNullOrWhiteSpace(iconPath))
                return string.Empty;
            if (!Path.IsPathRooted(iconPath))
                return iconPath; // đã là đường dẫn tương đối trong tab
            if (!File.Exists(iconPath))
                return string.Empty;
            if (LispImport.IsInsideDirectory(iconPath, _manifest.DirectoryPath))
                return LispImport.MakeRelative(_manifest.DirectoryPath, iconPath);

            string iconDir = Path.Combine(_manifest.DirectoryPath, "icons");
            string fileName = Path.GetFileName(iconPath);
            string existing = Path.Combine(iconDir, fileName);
            if (File.Exists(existing) && SameBytes(existing, iconPath))
                return "icons/" + fileName;
            fileName = Directory.Exists(iconDir) ? LispImport.NextFreeFileName(iconDir, fileName) : fileName;
            return LispImport.CopyIntoModule(_manifest.DirectoryPath, iconPath, "icons", fileName, false);
        }

        /// <summary>Lỗi trùng lệnh (trên mọi tab, và trùng nhau trong cùng lần thêm). Rỗng = hợp lệ.</summary>
        private List<string> FindConflicts(IList<string> commands, string exceptToolId)
        {
            var errors = new List<string>();
            List<ModuleManifest> modules = LoadAllModules();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string cmd in commands)
            {
                string c = (cmd ?? string.Empty).Trim();
                if (c.Length == 0)
                {
                    errors.Add("Có nút chưa nhập tên lệnh.");
                    continue;
                }
                if (!seen.Add(c))
                {
                    errors.Add($"Lệnh {c} bị nhập 2 lần.");
                    continue;
                }
                string owner = ModuleJsonEditor.FindCommandOwner(modules, c, _manifest.DirectoryPath, exceptToolId);
                if (owner != null)
                    errors.Add($"Lệnh {c} đã có ở tab {owner}.");
            }
            return errors;
        }

        private List<ModuleManifest> LoadAllModules()
        {
            try
            {
                string root = Directory.GetParent(_manifest.DirectoryPath)?.FullName;
                return root != null ? ModuleDiscovery.DiscoverModules(root) : new List<ModuleManifest>();
            }
            catch
            {
                return new List<ModuleManifest>();
            }
        }

        // ------------------------------------------------------------------
        // Menu chuột phải
        // ------------------------------------------------------------------

        public void EditTool(ToolCardViewModel card)
        {
            if (card == null)
                return;
            var window = new ToolEditWindow(card.Tool, _manifest.DirectoryPath, GroupNames, AccentBrush);
            window.ValidateCommands = cmds => FindConflicts(cmds, card.Tool.Id);
            if (CadCompat.ShowModalDialog(window) != true || window.Result == null)
                return;

            ToolItem values = window.Result;
            values.LispFile = card.Tool.LispFile;
            try
            {
                values.Icon = ImportIcon(values.Icon);
            }
            catch (Exception ex)
            {
                ShowError("Không chép được ảnh", ex);
                return;
            }
            EditJson("Không sửa được lệnh", root => ModuleJsonEditor.UpdateTool(root, card.Tool.Id, values));
        }

        public void MoveToGroup(ToolCardViewModel card, string group)
        {
            if (card != null)
                EditJson("Không chuyển được nhóm", root => ModuleJsonEditor.SetToolCategory(root, card.Tool.Id, group));
        }

        public void MoveToNewGroup(ToolCardViewModel card)
        {
            if (card == null)
                return;
            string name = InputDialog.Ask("Nhóm mới", "Tên nhóm mới:", string.Empty, AccentBrush);
            if (!string.IsNullOrWhiteSpace(name))
                MoveToGroup(card, name.Trim());
        }

        public void MoveTool(ToolCardViewModel card, int delta)
        {
            if (card != null)
                EditJson("Không đổi được thứ tự", root => ModuleJsonEditor.MoveToolInGroup(root, card.Tool.Id, delta));
        }

        public void RemoveTool(ToolCardViewModel card)
        {
            if (card == null || card.IsLocked)
                return;
            MessageBoxResult ans = MessageBox.Show(
                $"Gỡ nút '{card.Name}' ({card.Command}) khỏi tab?\nFile lisp vẫn giữ nguyên trong thư mục tab.",
                "OHTA TOOL — Xoá nút", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (ans == MessageBoxResult.OK)
                EditJson("Không xoá được nút", root => ModuleJsonEditor.RemoveTool(root, card.Tool.Id));
        }

        // ------------------------------------------------------------------
        // Cài đặt tab
        // ------------------------------------------------------------------

        private void OpenSettings()
        {
            var window = new TabSettingsWindow(_manifest, GroupNames, () => OpenPathSafe(_manifest.DirectoryPath));
            if (CadCompat.ShowModalDialog(window) != true || window.Result == null)
                return;

            TabSettingsResult s = window.Result;
            EditJson("Không lưu được cài đặt tab", root =>
            {
                ModuleJsonEditor.SetHeader(root, s.Title, s.Description, s.AccentColor);
                foreach (string deleted in s.DeletedGroups)
                    ModuleJsonEditor.DeleteGroup(root, deleted);
                foreach (KeyValuePair<string, string> rename in s.Renames)
                    ModuleJsonEditor.RenameGroup(root, rename.Key, rename.Value);
                ModuleJsonEditor.SetGroups(root, s.GroupOrder);
                return true;
            });
        }

        // ------------------------------------------------------------------
        // Tiện ích
        // ------------------------------------------------------------------

        private void EditJson(string errorTitle, Func<JsonObject, bool> mutate)
        {
            try
            {
                bool changed = ModuleJsonEditor.Update(_manifest.DirectoryPath, mutate);
                if (changed)
                    ReloadLater();
            }
            catch (Exception ex)
            {
                ShowError(errorTitle, ex);
            }
        }

        /// <summary>Làm mới palette sau khi xử lý xong sự kiện hiện tại (tránh huỷ control đang xử lý click).</summary>
        private void ReloadLater()
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => _reloadAllCallback?.Invoke()));
        }

        private static bool SameBytes(string a, string b)
        {
            try
            {
                var fa = new FileInfo(a);
                var fb = new FileInfo(b);
                return fa.Length == fb.Length && File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b));
            }
            catch
            {
                return false;
            }
        }

        private static void ShowError(string title, Exception ex)
        {
            MessageBox.Show(title + ":\n" + ex.Message, "OHTA TOOL", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static void OpenPathSafe(string targetPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(targetPath))
                    return;

                if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
                {
                    CadCompat.WriteMessage($"\n[OHTA TOOL] Đường dẫn chưa tồn tại: {targetPath}\n");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = targetPath,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                CadCompat.WriteMessage($"\n[OHTA TOOL] Không mở được '{targetPath}': {ex.Message}\n");
            }
        }

        public static SolidColorBrush ParseColorBrush(string hex, string fallbackHex)
        {
            try
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
                if (brush != null)
                {
                    brush.Freeze();
                    return brush;
                }
            }
            catch
            {
            }

            var fb = (SolidColorBrush)new BrushConverter().ConvertFromString(fallbackHex);
            fb?.Freeze();
            return fb;
        }

        private void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
