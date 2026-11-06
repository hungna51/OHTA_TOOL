using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Services;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Form báo lỗi (spec _docs/specs/2026-11-05-bug-report.md): mô tả, tab/lệnh, mức độ, người gửi,
    /// ảnh cửa sổ CAD (chụp sẵn trước khi form hiện), file đính kèm tuỳ chọn. Viết bằng code như InfoTabControl.
    /// </summary>
    public sealed class BugReportWindow : Window
    {
        private readonly BugReport _report = BugReport.Create();
        private readonly string _stackTrace;
        private readonly byte[] _anh;
        private readonly TextBox _moTa, _tabLenh, _nguoiGui;
        private readonly ComboBox _mucDo;
        private readonly CheckBox _guiAnh, _dwg, _csv, _thongTin;
        private readonly string _dwgPath, _csvPath, _thongTinPath;
        private readonly TextBlock _status;
        private readonly Button _send;
        private bool _sending;

        /// <summary>Mở form (modal với cửa sổ CAD). anh = ảnh đã chụp trước, null nếu không chụp được.</summary>
        public static void Show(string tabLenh, string loi, string stackTrace, byte[] anh)
        {
            if (BugDedup.InReport)
                return;
            BugDedup.InReport = true;
            try
            {
                CadCompat.ShowModalDialog(new BugReportWindow(tabLenh, loi, stackTrace, anh));
            }
            catch (Exception ex)
            {
                BugLog.Write("BUG", ex);
            }
            finally
            {
                BugDedup.InReport = false;
            }
        }

        private BugReportWindow(string tabLenh, string loi, string stackTrace, byte[] anh)
        {
            _report.Loi = loi;
            _stackTrace = stackTrace;
            _anh = anh;
            Title = "OHTA TOOL — Báo lỗi cho admin";
            Width = 560;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Segoe UI, Meiryo UI, Yu Gothic UI");
            Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xFA));

            var root = new StackPanel { Margin = new Thickness(14) };

            if (!string.IsNullOrEmpty(loi))
                root.Children.Add(new TextBlock
                {
                    Text = "Lỗi: " + BugReport.ShortError(loi),
                    Foreground = Brushes.Firebrick,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });

            root.Children.Add(Label("Mô tả lỗi * (đang làm gì, mong đợi gì, ra kết quả gì)"));
            _moTa = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 90, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            root.Children.Add(_moTa);

            var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _tabLenh = new TextBox { Text = tabLenh ?? "OHTA" };
            _mucDo = new ComboBox { ItemsSource = BugReport.MucDoList, SelectedItem = _report.MucDo };
            _nguoiGui = new TextBox { Text = _report.NguoiGui };
            AddRow(grid, "Tab / lệnh", _tabLenh);
            AddRow(grid, "Mức độ", _mucDo);
            AddRow(grid, "Người gửi", _nguoiGui);
            root.Children.Add(grid);

            // Ảnh cửa sổ CAD
            _guiAnh = new CheckBox { Content = "Gửi kèm ảnh cửa sổ CAD (chụp lúc bấm báo lỗi)", IsChecked = anh != null, IsEnabled = anh != null, Margin = new Thickness(0, 10, 0, 4) };
            root.Children.Add(_guiAnh);
            if (anh != null)
            {
                var img = new Image { Source = ToBitmap(anh), MaxHeight = 200, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left };
                root.Children.Add(new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1), Child = img });
            }
            else
                root.Children.Add(new TextBlock { Text = "(Không chụp được ảnh cửa sổ CAD)", Foreground = Brushes.Gray });

            // File đính kèm (mặc định không gửi: bản vẽ của khách, dung lượng)
            root.Children.Add(Label("File đính kèm (tuỳ chọn, nén zip, tối đa 25 MB)", top: 10));
            _dwgPath = DwgPath();
            _csvPath = RecentCsv(_dwgPath);
            _thongTinPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(), "gro_thong_tin.txt");
            _dwg = Attach("Bản vẽ đang mở (.dwg — bản đã lưu gần nhất)", _dwgPath);
            _csv = Attach("CSV vừa xuất" + (_csvPath == null ? "" : " (" + Path.GetFileName(_csvPath) + ")"), _csvPath);
            _thongTin = Attach("Báo cáo GRO (gro_thong_tin.txt)", File.Exists(_thongTinPath) ? _thongTinPath : null);
            root.Children.Add(_dwg);
            root.Children.Add(_csv);
            root.Children.Add(_thongTin);
            root.Children.Add(new TextBlock
            {
                Text = "Luôn gửi kèm: phiên bản Tool / từng tab, nền tảng CAD, 200 dòng nhật ký gần nhất" + (string.IsNullOrEmpty(stackTrace) ? "." : ", chi tiết lỗi."),
                Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0)
            });

            _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };
            root.Children.Add(_status);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            _send = new Button { Content = "🐞  Gửi báo lỗi", Padding = new Thickness(14, 5, 14, 5), IsDefault = false };
            // async void: exception lọt ra làm sập CAD -> bắt hết
            _send.Click += async (s, e) =>
            {
                try { await SendAsync(); }
                catch (Exception ex)
                {
                    BugLog.Write("BUG", ex);
                    try { _send.IsEnabled = true; ShowStatus("Lỗi: " + ex.Message, error: true); } catch { }
                }
            };
            // Đang nén thì không cho đóng form: đóng giữa chừng rồi Close trên form đã đóng sẽ ném
            Closing += (s, e) =>
            {
                if (_sending)
                {
                    e.Cancel = true;
                    ShowStatus("Đang nén, chờ một chút…", error: false);
                }
            };
            var cancel = new Button { Content = "Huỷ", Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
            buttons.Children.Add(_send);
            buttons.Children.Add(cancel);
            root.Children.Add(buttons);

            Content = root;
            Loaded += (s, e) => _moTa.Focus();
        }

        private async Task SendAsync()
        {
            _report.MoTa = _moTa.Text.Trim();
            _report.TabLenh = _tabLenh.Text.Trim();
            _report.MucDo = _mucDo.SelectedItem as string;
            _report.NguoiGui = _nguoiGui.Text.Trim();
            string err = _report.Validate();
            if (err != null)
            {
                ShowStatus(err, error: true);
                return;
            }

            var files = new List<string>();
            if (_dwg.IsChecked == true) files.Add(_dwgPath);
            if (_csv.IsChecked == true) files.Add(_csvPath);
            if (_thongTin.IsChecked == true) files.Add(_thongTinPath);

            bool guiAnh = _guiAnh.IsChecked == true;
            // Đọc CAD (bản vẽ, ACADVER) trên luồng CAD — API CAD không an toàn khi gọi từ luồng nền
            (string phienBan, string info) = BugContext.Describe();
            _send.IsEnabled = false;
            _sending = true;
            ShowStatus(files.Count > 0 ? "Đang nén file đính kèm…" : "Đang chuẩn bị…", error: false);
            string zipError;
            try
            {
                // Chỉ nén ở form (lỗi "quá 25 MB" sửa được ngay); tải lên chạy ở nền, tiến trình ở tab Info
                zipError = await Task.Run(() =>
                {
                    (byte[] zip, string ze) = BugAttachments.Zip(files);
                    if (ze != null)
                        return ze;
                    _report.DinhKemZip = zip;
                    _report.DinhKemTen = zip == null ? null : "dinh_kem.zip";
                    _report.AnhJpeg = guiAnh ? _anh : null;
                    _report.PhienBan = phienBan;
                    _report.ChiTiet = BuildChiTiet(info);
                    return null;
                });
                if (zipError == null)
                    BugSender.Send(_report);
            }
            catch (Exception ex)
            {
                BugLog.Write("BUG", ex);
                zipError = "Lỗi: " + ex.Message;
            }
            finally
            {
                _sending = false;
            }

            if (zipError == null)
            {
                Close();
                return;
            }
            _send.IsEnabled = true;
            ShowStatus(zipError, error: true);
        }

        private string BuildChiTiet(string info)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID: " + _report.Id);
            sb.AppendLine(info);
            if (!string.IsNullOrEmpty(_stackTrace))
            {
                sb.AppendLine();
                sb.AppendLine("--- LOI ---");
                sb.AppendLine(_stackTrace);
            }
            sb.AppendLine();
            sb.AppendLine("--- NHAT KY (200 dong cuoi) ---");
            foreach (string l in BugLog.Tail(200))
                sb.AppendLine(l);
            return sb.ToString();
        }

        private void ShowStatus(string text, bool error)
        {
            _status.Text = text;
            _status.Foreground = error ? Brushes.Firebrick : Brushes.DimGray;
        }

        // ---------------------------------------------------------------- tiện ích

        private static string DwgPath()
        {
            try
            {
                string name = CadCompat.ActiveDocument?.Name;
                return !string.IsNullOrEmpty(name) && Path.IsPathRooted(name) && File.Exists(name) ? name : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>CSV GRO ghi cạnh bản vẽ (DWGPREFIX + tên đơn .csv): lấy file .csv mới nhất trong 24 giờ.</summary>
        private static string RecentCsv(string dwgPath)
        {
            try
            {
                if (dwgPath == null)
                    return null;
                return new DirectoryInfo(Path.GetDirectoryName(dwgPath)).GetFiles("*.csv")
                    .Where(f => f.LastWriteTime > DateTime.Now.AddHours(-24))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => f.FullName)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static CheckBox Attach(string text, string path) => new CheckBox
        {
            Content = text,
            IsChecked = false,
            IsEnabled = path != null,
            ToolTip = path ?? "Không có file",
            Margin = new Thickness(0, 2, 0, 2)
        };

        private static TextBlock Label(string text, double top = 0) => new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, top, 0, 4)
        };

        private static void AddRow(Grid grid, string label, UIElement value)
        {
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var l = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 3, 10, 3) };
            Grid.SetRow(l, row);
            grid.Children.Add(l);
            if (value is FrameworkElement fe)
                fe.Margin = new Thickness(0, 3, 0, 3);
            Grid.SetRow(value, row);
            Grid.SetColumn(value, 1);
            grid.Children.Add(value);
        }

        private static BitmapImage ToBitmap(byte[] jpeg)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = new MemoryStream(jpeg);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
