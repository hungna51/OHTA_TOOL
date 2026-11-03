using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.ViewModels
{
    /// <summary>
    /// ViewModel điều khiển nội dung một Tab mục con trên bảng OHTA Tool Palettes.
    /// </summary>
    public sealed class ModuleTabViewModel : INotifyPropertyChanged
    {
        private readonly ModuleManifest _manifest;
        private readonly Action _reloadAllCallback;
        private string _searchText = string.Empty;

        public ModuleTabViewModel(ModuleManifest manifest, Action reloadAllCallback)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _reloadAllCallback = reloadAllCallback;

            FilteredTools = new ObservableCollection<ToolItem>();
            AccentBrush = ParseColorBrush(_manifest.AccentColor, "#1E88E5");

            ExecuteToolCommand = new RelayCommand(OnExecuteTool);
            ReloadTabsCommand = new RelayCommand(_ => _reloadAllCallback?.Invoke());
            OpenReadmeCommand = new RelayCommand(_ => OpenPathSafe(_manifest.ReadmePath));
            OpenRulesFolderCommand = new RelayCommand(_ => OpenPathSafe(_manifest.RulesDirectoryPath));
            OpenModuleFolderCommand = new RelayCommand(_ => OpenPathSafe(_manifest.DirectoryPath));
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);

            ApplyFilter();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ModuleManifest Manifest => _manifest;
        public string Title => _manifest.Title;
        public string FolderName => _manifest.FolderName;
        public string Description => _manifest.Description;
        public string LoadWarning => _manifest.LoadWarning;
        public SolidColorBrush AccentBrush { get; }
        public ObservableCollection<ToolItem> FilteredTools { get; }

        public int TotalToolCount => _manifest.Tools != null ? _manifest.Tools.Count : 0;

        public string StatusSummary =>
            TotalToolCount == 0
                ? "Chưa khai báo lệnh • Sẵn sàng bổ sung vào " + _manifest.FolderName
                : (FilteredTools.Count == TotalToolCount
                    ? $"Đang hiển thị {TotalToolCount} công cụ • {_manifest.FolderName}"
                    : $"Lọc {FilteredTools.Count}/{TotalToolCount} công cụ • {_manifest.FolderName}");

        public Visibility EmptyStateVisibility =>
            TotalToolCount == 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility NoFilterMatchVisibility =>
            TotalToolCount > 0 && FilteredTools.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

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
        public ICommand OpenReadmeCommand { get; }
        public ICommand OpenRulesFolderCommand { get; }
        public ICommand OpenModuleFolderCommand { get; }
        public ICommand ClearSearchCommand { get; }

        private void ApplyFilter()
        {
            FilteredTools.Clear();
            if (_manifest.Tools != null)
            {
                string kw = (_searchText ?? string.Empty).Trim();
                foreach (ToolItem item in _manifest.Tools)
                {
                    if (MatchesFilter(item, kw))
                        FilteredTools.Add(item);
                }
            }

            OnPropertyChanged(nameof(StatusSummary));
            OnPropertyChanged(nameof(EmptyStateVisibility));
            OnPropertyChanged(nameof(NoFilterMatchVisibility));
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

        private void OnExecuteTool(object parameter)
        {
            if (!(parameter is ToolItem tool))
                return;

            string macro = MacroBuilder.BuildToolMacro(_manifest.DirectoryPath, tool);
            if (string.IsNullOrWhiteSpace(macro))
            {
                CadCompat.WriteMessage($"\n[OHTA TOOL] Công cụ '{tool.Name}' chưa khai báo thuộc tính 'command' trong module.json.\n");
                return;
            }

            CadCompat.SendCommand(macro);
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

        private static SolidColorBrush ParseColorBrush(string hex, string fallbackHex)
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

