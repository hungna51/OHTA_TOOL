using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Services;
using OhtaTool.Plugin.Views;

namespace OhtaTool.Plugin.ViewModels
{
    /// <summary>ViewModel tab PRINT (lệnh OI v2.0).</summary>
    public sealed class PrintTabViewModel : INotifyPropertyChanged
    {
        private static PrintTabViewModel _active;
        private static bool _hooked;

        private readonly ModuleManifest _manifest;
        private readonly Dispatcher _dispatcher;
        private PrintSettingsData _settings = new PrintSettingsData();
        private PrintParams _drawing = new PrintParams();
        private IPlotCatalog _catalog;
        private bool _lineweights = true;
        private bool _suppress;

        private string _selectedLayer, _selectedProfile, _selectedDevice, _selectedMedia, _selectedStyleSheet;
        private string _mode = "frames";
        private bool _merge = true, _openAfter = true;
        private string _warningText = string.Empty, _statusText = string.Empty;

        public PrintTabViewModel(ModuleManifest manifest)
        {
            _manifest = manifest;
            _dispatcher = Dispatcher.CurrentDispatcher;
            AccentBrush = ParseBrush(manifest?.AccentColor, Color.FromRgb(0x2E, 0x7D, 0x32));

            PrintAllCommand = new RelayCommand(_ => SendPrint("frames"), _ => CanPrintFrames);
            PickFramesCommand = new RelayCommand(_ => SendPrint("pick"), _ => CanPrintFrames);
            PrintCommand = new RelayCommand(_ => SendPrint(_mode));
            SettingsCommand = new RelayCommand(_ => OpenSettings());
            RefreshCommand = new RelayCommand(_ => Refresh());

            _active = this;
            HookStaticEvents();
            Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title => _manifest?.DisplayTitle ?? "OI";
        public string Description => _manifest?.Description ?? string.Empty;
        public Brush AccentBrush { get; }
        public ImageSource SettingsLogo => IconLoader.GetSettingsLogo();

        public ObservableCollection<string> Layers { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ProfileNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Devices { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> MediaList { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> StyleSheets { get; } = new ObservableCollection<string>();

        public ICommand PrintAllCommand { get; }
        public ICommand PickFramesCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand RefreshCommand { get; }

        public string SelectedLayer
        {
            get => _selectedLayer;
            set { if (Set(ref _selectedLayer, value)) OnPropertyChanged(nameof(CanPrintFrames)); }
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (!Set(ref _selectedProfile, value) || _suppress)
                    return;
                string name = IsFromDrawing(value) ? null : value;
                _settings.LastProfile = name;
                PrintSettingsStore.SaveLastProfile(name);
                ApplyResolve();
            }
        }

        public string SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (!Set(ref _selectedDevice, value) || _suppress)
                    return;
                // Đổi máy in tay: nạp lại khổ, giữ khổ đang chọn nếu máy mới có
                string keep = _selectedMedia;
                FillMedia(value);
                SelectedMedia = MediaList.FirstOrDefault(m => string.Equals(m, keep, StringComparison.OrdinalIgnoreCase))
                                ?? MediaList.FirstOrDefault();
            }
        }

        public string SelectedMedia { get => _selectedMedia; set => Set(ref _selectedMedia, value); }
        public string SelectedStyleSheet { get => _selectedStyleSheet; set => Set(ref _selectedStyleSheet, value); }
        public bool Merge { get => _merge; set => Set(ref _merge, value); }
        public bool OpenAfter { get => _openAfter; set => Set(ref _openAfter, value); }

        public bool ModeFrames { get => _mode == "frames"; set { if (value) SetMode("frames"); } }
        public bool ModeWindow { get => _mode == "window"; set { if (value) SetMode("window"); } }
        public bool ModeExtents { get => _mode == "extents"; set { if (value) SetMode("extents"); } }
        public Visibility FramesButtonsVisibility => _mode == "frames" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PrintButtonVisibility => _mode == "frames" ? Visibility.Collapsed : Visibility.Visible;

        public bool CanPrintFrames => !string.IsNullOrEmpty(_selectedLayer);

        public string WarningText
        {
            get => _warningText;
            set { if (Set(ref _warningText, value)) OnPropertyChanged(nameof(WarningVisibility)); }
        }
        public Visibility WarningVisibility => string.IsNullOrEmpty(_warningText) ? Visibility.Collapsed : Visibility.Visible;

        public string StatusText
        {
            get => _statusText;
            set { if (Set(ref _statusText, value)) OnPropertyChanged(nameof(StatusVisibility)); }
        }
        public Visibility StatusVisibility => string.IsNullOrEmpty(_statusText) ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>Đọc lại bản vẽ đang mở, cài đặt và danh sách máy in, rồi áp quy tắc ưu tiên.</summary>
        public void Refresh()
        {
            try
            {
                _settings = PrintSettingsStore.Load();
                _catalog = new CadPlotCatalog();

                CadDocument doc = CadCompat.ActiveDocument;
                IList<string> layers;
                using (CadCompat.LockActiveDocument())
                {
                    layers = CadPrintService.ListLayers(doc);
                    _drawing = CadPrintService.ReadDrawingParams(doc);
                }

                _suppress = true;
                Replace(Layers, layers);
                SelectedLayer = PrintCore.PickFrameLayer(layers, _settings.FrameLayers);

                var names = new List<string> { PrintSettingsResolver.FromDrawingName };
                names.AddRange(_settings.Profiles.Select(p => p.Name));
                Replace(ProfileNames, names);
                string last = _settings.LastProfile;
                SelectedProfile = names.FirstOrDefault(n => !IsFromDrawing(n) && string.Equals(n, last, StringComparison.OrdinalIgnoreCase))
                                  ?? PrintSettingsResolver.FromDrawingName;
                _suppress = false;

                ApplyResolve();

                if (doc == null)
                    StatusText = "Chưa có bản vẽ nào đang mở.";
                else if (SelectedLayer == null)
                    StatusText = "Bản vẽ không có layer khung in (" + string.Join(", ", _settings.FrameLayers) + ").";
                else
                    StatusText = string.Empty;
            }
            catch (Exception ex)
            {
                _suppress = false;
                StatusText = "Lỗi đọc bản vẽ: " + ex.Message;
            }
        }

        private void ApplyResolve()
        {
            PrintProfile profile = IsFromDrawing(_selectedProfile)
                ? null
                : _settings.Profiles.FirstOrDefault(p => string.Equals(p.Name, _selectedProfile, StringComparison.OrdinalIgnoreCase));
            ResolvedPrint r = PrintSettingsResolver.Resolve(profile, _drawing, _settings.Defaults, _catalog);

            _suppress = true;
            Replace(Devices, _catalog.Devices);
            SelectedDevice = r.Values.Device;
            FillMedia(r.Values.Device);
            SelectedMedia = r.Values.Media;
            Replace(StyleSheets, _catalog.StyleSheets);
            SelectedStyleSheet = r.Values.StyleSheet;
            _suppress = false;

            _lineweights = r.Values.Lineweights ?? true;
            Merge = r.Values.Merge ?? true;
            OpenAfter = r.Values.OpenAfter ?? true;
            WarningText = string.Join("\n", r.Warnings.Select(w => "⚠ " + w));
        }

        private void FillMedia(string device) => Replace(MediaList, _catalog?.MediaFor(device) ?? new List<string>());

        private void SendPrint(string mode)
        {
            if (string.IsNullOrEmpty(_selectedDevice) || string.IsNullOrEmpty(_selectedMedia))
            {
                StatusText = "Chưa chọn được máy in / khổ giấy.";
                return;
            }
            if ((mode == "frames" || mode == "pick") && string.IsNullOrEmpty(_selectedLayer))
            {
                StatusText = "Chưa chọn layer khung in.";
                return;
            }

            PrintRunner.Pending = new PrintRequest
            {
                Mode = mode,
                Layer = _selectedLayer,
                SkipOverwritePrompt = _settings.SkipOverwritePrompt,
                Params = new PrintParams
                {
                    Device = _selectedDevice,
                    Media = _selectedMedia,
                    StyleSheet = _selectedStyleSheet ?? string.Empty,
                    Lineweights = _lineweights,
                    Merge = _merge,
                    OpenAfter = _openAfter
                }
            };
            StatusText = "Đang in…";
            CadCompat.SendCommand("_OIIN ");
        }

        private void OpenSettings()
        {
            if (PrintSettingsWindow.Show(PrintSettingsStore.Load(), _catalog ?? new CadPlotCatalog(), AccentBrush, out PrintSettingsData saved))
            {
                PrintSettingsStore.Save(saved);
                Refresh();
            }
        }

        private void SetMode(string mode)
        {
            if (_mode == mode)
                return;
            _mode = mode;
            OnPropertyChanged(nameof(ModeFrames));
            OnPropertyChanged(nameof(ModeWindow));
            OnPropertyChanged(nameof(ModeExtents));
            OnPropertyChanged(nameof(FramesButtonsVisibility));
            OnPropertyChanged(nameof(PrintButtonVisibility));
        }

        private static void HookStaticEvents()
        {
            if (_hooked)
                return;
            _hooked = true;
            PrintRunner.Finished += msg =>
            {
                PrintTabViewModel vm = _active;
                vm?._dispatcher.BeginInvoke(new Action(() => vm.StatusText = msg));
            };
            try
            {
                CadApp.DocumentManager.DocumentActivated += (s, e) =>
                {
                    PrintTabViewModel vm = _active;
                    vm?._dispatcher.BeginInvoke(new Action(vm.Refresh));
                };
            }
            catch (Exception)
            {
            }
        }

        private static bool IsFromDrawing(string name) =>
            string.IsNullOrEmpty(name) || name == PrintSettingsResolver.FromDrawingName;

        private static void Replace(ObservableCollection<string> target, IEnumerable<string> items)
        {
            target.Clear();
            foreach (string s in items ?? Enumerable.Empty<string>())
                target.Add(s);
        }

        private static Brush ParseBrush(string hex, Color fallback)
        {
            Color c = fallback;
            try
            {
                if (!string.IsNullOrWhiteSpace(hex))
                    c = (Color)ColorConverter.ConvertFromString(hex.Trim());
            }
            catch (Exception) { }
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
