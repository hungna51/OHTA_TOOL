using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Bộ ô nhập một PrintParams. allowUnset = cấu hình có tên: mỗi ô có lựa chọn "(không đặt)" = null,
    /// ô tick 3 trạng thái (ô vuông đặc = không đặt).
    /// </summary>
    internal sealed class PrintParamsEditor
    {
        public const string Unset = "(không đặt)";

        private readonly IPlotCatalog _catalog;
        private readonly bool _allowUnset;
        private readonly ComboBox _device, _media, _style;
        private readonly CheckBox _merge, _openAfter;

        public StackPanel Panel { get; } = new StackPanel();

        public PrintParamsEditor(PrintParams p, IPlotCatalog catalog, bool allowUnset)
        {
            _catalog = catalog;
            _allowUnset = allowUnset;
            p = p ?? new PrintParams();

            _device = Combo(catalog.Devices, p.Device);
            _media = Combo(catalog.MediaFor(p.Device ?? catalog.Devices.FirstOrDefault()), p.Media);
            _style = Combo(catalog.StyleSheets, p.StyleSheet);
            _device.SelectionChanged += (s, e) => RefillMedia();

            _merge = Check("Gộp 1 file", p.Merge);
            _openAfter = Check("Mở sau khi in", p.OpenAfter);

            Panel.Children.Add(DialogKit.Label("Máy in"));
            Panel.Children.Add(_device);
            Panel.Children.Add(DialogKit.Label("Khổ giấy"));
            Panel.Children.Add(_media);
            Panel.Children.Add(DialogKit.Label("Bảng ctb"));
            Panel.Children.Add(_style);
            var checks = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            checks.Children.Add(_merge);
            checks.Children.Add(_openAfter);
            Panel.Children.Add(checks);
        }

        public PrintParams ToParams() => new PrintParams
        {
            Device = Value(_device),
            Media = Value(_media),
            StyleSheet = Value(_style),
            Merge = _merge.IsChecked,
            OpenAfter = _openAfter.IsChecked
        };

        private void RefillMedia()
        {
            string keep = Value(_media);
            string device = Value(_device) ?? _catalog.Devices.FirstOrDefault();
            Fill(_media, _catalog.MediaFor(device), keep);
        }

        private ComboBox Combo(IList<string> items, string current)
        {
            var cb = new ComboBox { Padding = new Thickness(4, 3, 4, 3) };
            Fill(cb, items, current);
            return cb;
        }

        private void Fill(ComboBox cb, IList<string> items, string current)
        {
            var list = new List<string>();
            if (_allowUnset)
                list.Add(Unset);
            list.AddRange(items ?? new List<string>());
            // Giá trị đã lưu mà máy này không có vẫn giữ lại để không mất cấu hình
            if (!string.IsNullOrWhiteSpace(current) && !list.Any(i => string.Equals(i, current, StringComparison.OrdinalIgnoreCase)))
                list.Add(current);
            cb.ItemsSource = list;
            cb.SelectedItem = list.FirstOrDefault(i => string.Equals(i, current, StringComparison.OrdinalIgnoreCase))
                              ?? (_allowUnset ? Unset : list.FirstOrDefault());
        }

        private CheckBox Check(string text, bool? value) => new CheckBox
        {
            Content = text,
            IsThreeState = _allowUnset,
            IsChecked = _allowUnset ? value : (value ?? true),
            Margin = new Thickness(0, 0, 14, 4)
        };

        private static string Value(ComboBox cb)
        {
            string v = cb.SelectedItem as string;
            return string.IsNullOrWhiteSpace(v) || v == Unset ? null : v;
        }
    }

    /// <summary>Cửa sổ ⚙ của tab PRINT: mặc định, layer khung ưu tiên, cấu hình in có tên.</summary>
    internal static class PrintSettingsWindow
    {
        public static bool Show(PrintSettingsData data, IPlotCatalog catalog, Brush accent, out PrintSettingsData saved)
        {
            saved = null;
            var profiles = data.Profiles.Select(p => new PrintProfile { Name = p.Name, Params = (p.Params ?? new PrintParams()).Clone() }).ToList();

            Window w = DialogKit.NewWindow("OI — Cài đặt in", 460);
            var root = new StackPanel { Margin = new Thickness(16) };

            root.Children.Add(Header("Mặc định (dùng khi bản vẽ không có)"));
            var defaults = new PrintParamsEditor(data.Defaults, catalog, allowUnset: false);
            root.Children.Add(defaults.Panel);

            var skipPrompt = new CheckBox
            {
                Content = "Không hỏi khi trùng tên file — tự lưu thành <tên> (1).pdf",
                IsChecked = data.SkipOverwritePrompt,
                Margin = new Thickness(0, 6, 0, 0)
            };
            root.Children.Add(skipPrompt);

            root.Children.Add(Header("Layer khung ưu tiên (mỗi dòng một tên)"));
            TextBox layers = DialogKit.Input(string.Join(Environment.NewLine, data.FrameLayers));
            layers.AcceptsReturn = true;
            layers.Height = 60;
            layers.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            root.Children.Add(layers);

            root.Children.Add(Header("Cấu hình in"));
            var list = new ListBox { Height = 90, BorderBrush = DialogKit.BorderBrush };
            Action refreshList = () =>
            {
                list.ItemsSource = null;
                list.ItemsSource = profiles.Select(p => p.Name).ToList();
            };
            refreshList();
            root.Children.Add(list);

            var profileButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            profileButtons.Children.Add(SmallBtn("Thêm", (s, e) =>
            {
                PrintProfile created = EditProfile(null, profiles, catalog, accent);
                if (created != null) { profiles.Add(created); refreshList(); list.SelectedItem = created.Name; }
            }));
            profileButtons.Children.Add(SmallBtn("Sửa", (s, e) =>
            {
                int i = list.SelectedIndex;
                if (i < 0) return;
                PrintProfile edited = EditProfile(profiles[i], profiles, catalog, accent);
                if (edited != null) { profiles[i] = edited; refreshList(); list.SelectedIndex = i; }
            }));
            profileButtons.Children.Add(SmallBtn("Xoá", (s, e) =>
            {
                int i = list.SelectedIndex;
                if (i < 0) return;
                if (MessageBox.Show(w, $"Xoá cấu hình \"{profiles[i].Name}\"?", "OI", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    profiles.RemoveAt(i);
                    refreshList();
                }
            }));
            root.Children.Add(profileButtons);

            bool ok = false;
            root.Children.Add(DialogKit.ButtonRow(
                DialogKit.Btn("Lưu", (s, e) => { ok = true; w.Close(); }, primary: true, accent: accent),
                DialogKit.Btn("Huỷ", (s, e) => w.Close())));

            w.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            CadCompat.ShowModalDialog(w);
            if (!ok)
                return false;

            var frameLayers = layers.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            saved = new PrintSettingsData
            {
                Defaults = defaults.ToParams(),
                FrameLayers = frameLayers.Count > 0 ? frameLayers : new List<string> { "Khung_IN" },
                Profiles = profiles,
                SkipOverwritePrompt = skipPrompt.IsChecked == true,
                LastProfile = profiles.Any(p => string.Equals(p.Name, data.LastProfile, StringComparison.OrdinalIgnoreCase)) ? data.LastProfile : null
            };
            return true;
        }

        /// <summary>Thêm (existing = null) hoặc sửa một cấu hình. Trả null nếu huỷ.</summary>
        private static PrintProfile EditProfile(PrintProfile existing, IList<PrintProfile> all, IPlotCatalog catalog, Brush accent)
        {
            Window w = DialogKit.NewWindow(existing == null ? "Thêm cấu hình in" : "Sửa cấu hình in", 420);
            var root = new StackPanel { Margin = new Thickness(16) };
            root.Children.Add(DialogKit.Label("Tên cấu hình", 0));
            TextBox name = DialogKit.Input(existing?.Name ?? string.Empty);
            root.Children.Add(name);
            root.Children.Add(new TextBlock
            {
                Text = "Ô để \"(không đặt)\" / ô tick đặc sẽ lấy theo bản vẽ hoặc mặc định.",
                Foreground = DialogKit.MutedBrush,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
            var editor = new PrintParamsEditor(existing?.Params, catalog, allowUnset: true);
            root.Children.Add(editor.Panel);
            TextBlock error = DialogKit.ErrorText();
            root.Children.Add(error);

            PrintProfile result = null;
            root.Children.Add(DialogKit.ButtonRow(
                DialogKit.Btn("OK", (s, e) =>
                {
                    string n = (name.Text ?? string.Empty).Trim();
                    var errors = new List<string>();
                    if (n.Length == 0)
                        errors.Add("Tên cấu hình không được trống.");
                    else if (string.Equals(n, PrintSettingsResolver.FromDrawingName, StringComparison.OrdinalIgnoreCase))
                        errors.Add($"Tên \"{PrintSettingsResolver.FromDrawingName}\" đã dành sẵn.");
                    else if (all.Any(p => p != existing && string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)))
                        errors.Add("Đã có cấu hình tên này.");
                    DialogKit.ShowErrors(error, errors);
                    if (errors.Count > 0)
                        return;
                    result = new PrintProfile { Name = n, Params = editor.ToParams() };
                    w.Close();
                }, primary: true, accent: accent),
                DialogKit.Btn("Huỷ", (s, e) => w.Close())));

            w.Content = root;
            CadCompat.ShowModalDialog(w);
            return result;
        }

        private static TextBlock Header(string text) => new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            Foreground = DialogKit.TextBrush,
            Margin = new Thickness(0, 14, 0, 2)
        };

        private static Button SmallBtn(string text, RoutedEventHandler onClick)
        {
            Button b = DialogKit.Btn(text, onClick);
            b.Margin = new Thickness(0, 0, 6, 0);
            return b;
        }
    }
}
