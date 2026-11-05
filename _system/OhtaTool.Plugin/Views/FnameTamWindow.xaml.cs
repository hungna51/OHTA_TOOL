using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Bổ sung T thực (T_hienthi) cho Loại + T chưa có trong tab tam, kèm người xác nhận, ghi chú và 1–3 ảnh bằng chứng.
    /// Ảnh được thu nhỏ (cạnh dài ≤ MaxSide, JPEG) trước khi gửi; Apps Script lưu ảnh lên Drive.
    /// </summary>
    public partial class FnameTamWindow : Window
    {
        public delegate Task<(bool ok, string message)> SubmitHandler(TamRecord record, List<byte[]> jpegs);

        private const int MaxImages = 3;
        private const int MaxSide = 1600;
        private const int JpegQuality = 85;

        private readonly SubmitHandler _submit;
        private readonly List<byte[]> _images = new List<byte[]>();

        public FnameTamWindow(string initialM, string initialT, SubmitHandler submit)
        {
            InitializeComponent();
            _submit = submit;

            var logo = IconLoader.GetSettingsLogo();
            if (logo != null)
                Icon = logo;

            foreach (string l in TraVCalculator.GetTamLoaiOptions())
                CmbLoai.Items.Add(l);

            // Loại theo mác đang nhập (SIMA bỏ chiều: "SS SIMA Trong" -> "SS SIMA")
            string loai = string.IsNullOrWhiteSpace(initialM) ? string.Empty : TraVCalculator.GetTraVMaterialKeys(initialM).FirstOrDefault() ?? string.Empty;
            if (loai.Length > 0)
                loai = CmbLoai.Items.Cast<string>().FirstOrDefault(x => TamData.SameLoai(x, loai)) ?? loai;
            CmbLoai.Text = loai;
            TxtT.Text = initialT ?? string.Empty;

            CmbLoai.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) => UpdateKeyState()));
            CmbLoai.SelectionChanged += (s, e) => Dispatcher.BeginInvoke(new Action(UpdateKeyState));
            TxtT.TextChanged += (s, e) => UpdateKeyState();
            PreviewKeyDown += OnPreviewKeyDown;
            UpdateKeyState();
            RefreshImages();
            Loaded += (s, e) => (CmbLoai.Text.Length == 0 ? (UIElement)CmbLoai : TxtTHienThi).Focus();
        }

        /// <summary>Báo ngay khi Loại + T đã có T thực (chỉ cho lấp chỗ trống).</summary>
        private void UpdateKeyState()
        {
            double? t = TraVThuData.ParseNumber(TxtT.Text);
            string loai = (CmbLoai.Text ?? string.Empty).Trim();
            TamRecord old = loai.Length > 0 && t.HasValue ? TraVCalculator.FindTam(loai, t.Value) : null;
            if (old == null)
            {
                TxtKeyState.Text = string.Empty;
                return;
            }
            TxtKeyState.Text = $"⚠ {old.Loai} T={old.T} đã có T thực = {old.THienThi} (nguồn {old.Nguon}{(old.ChoDuyet ? ", đang chờ duyệt" : string.Empty)}) — không bổ sung đè được.";
            TxtKeyState.Foreground = (Brush)new BrushConverter().ConvertFrom("#B45309");
        }

        // ---------- Ảnh ----------

        private void OnPickImagesClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn ảnh bằng chứng",
                Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff",
                Multiselect = true
            };
            if (dlg.ShowDialog(this) != true)
                return;
            foreach (string f in dlg.FileNames)
                AddImageFromFile(f);
        }

        private void OnPasteImageClick(object sender, RoutedEventArgs e) => PasteFromClipboard(showEmptyMessage: true);

        // Ctrl+V ở bất kỳ đâu trong cửa sổ: clipboard có ảnh / file ảnh thì thêm ảnh (ô chữ vẫn dán chữ như thường)
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && (Clipboard.ContainsImage() || Clipboard.ContainsFileDropList()))
            {
                PasteFromClipboard(showEmptyMessage: false);
                e.Handled = true;
            }
        }

        private void PasteFromClipboard(bool showEmptyMessage)
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    AddImage(Clipboard.GetImage());
                    return;
                }
                if (Clipboard.ContainsFileDropList())
                {
                    foreach (string f in Clipboard.GetFileDropList())
                        AddImageFromFile(f);
                    return;
                }
                if (showEmptyMessage)
                    ShowStatus("Clipboard không có ảnh. Chụp màn hình / copy ảnh rồi bấm lại.", false);
            }
            catch (Exception ex)
            {
                ShowStatus("Không đọc được ảnh trong clipboard: " + ex.GetBaseException().Message, false);
            }
        }

        private void AddImageFromFile(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad; // đọc xong nhả file
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                AddImage(bmp);
            }
            catch (Exception ex)
            {
                ShowStatus($"Không mở được ảnh \"{Path.GetFileName(path)}\": " + ex.GetBaseException().Message, false);
            }
        }

        private void AddImage(BitmapSource source)
        {
            if (source == null)
                return;
            if (_images.Count >= MaxImages)
            {
                ShowStatus($"Tối đa {MaxImages} ảnh — bỏ bớt ảnh trước khi thêm.", false);
                return;
            }
            _images.Add(ToJpeg(source));
            RefreshImages();
        }

        /// <summary>Thu nhỏ cạnh dài về ≤ MaxSide rồi nén JPEG (bỏ kênh alpha — ảnh chụp màn hình đôi khi alpha = 0).</summary>
        internal static byte[] ToJpeg(BitmapSource source)
        {
            BitmapSource img = source;
            double scale = Math.Min(1.0, (double)MaxSide / Math.Max(source.PixelWidth, source.PixelHeight));
            if (scale < 1.0)
                img = new TransformedBitmap(img, new ScaleTransform(scale, scale));
            img = new FormatConvertedBitmap(img, PixelFormats.Bgr24, null, 0);
            var enc = new JpegBitmapEncoder { QualityLevel = JpegQuality };
            enc.Frames.Add(BitmapFrame.Create(img));
            using (var ms = new MemoryStream())
            {
                enc.Save(ms);
                return ms.ToArray();
            }
        }

        private void RefreshImages()
        {
            PanelImages.Children.Clear();
            for (int i = 0; i < _images.Count; i++)
            {
                int index = i;
                var thumb = new BitmapImage();
                thumb.BeginInit();
                thumb.CacheOption = BitmapCacheOption.OnLoad;
                thumb.DecodePixelHeight = 120;
                thumb.StreamSource = new MemoryStream(_images[i]);
                thumb.EndInit();

                var remove = new Button
                {
                    Content = "✕",
                    Width = 20,
                    Height = 20,
                    Padding = new Thickness(0),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 2, 0),
                    ToolTip = "Bỏ ảnh này",
                    Cursor = Cursors.Hand
                };
                remove.Click += (s, e) => { _images.RemoveAt(index); RefreshImages(); };

                var grid = new Grid();
                grid.Children.Add(new Image { Source = thumb, Height = 64, Stretch = Stretch.Uniform });
                grid.Children.Add(remove);
                PanelImages.Children.Add(new Border
                {
                    Child = grid,
                    BorderBrush = (Brush)new BrushConverter().ConvertFrom("#CBD5E1"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 6, 6),
                    ToolTip = $"Ảnh {i + 1} — {_images[i].Length / 1024} KB sau khi thu nhỏ"
                });
            }
        }

        // ---------- Gửi ----------

        private async void OnSubmitClick(object sender, RoutedEventArgs e)
        {
            string loai = (CmbLoai.Text ?? string.Empty).Trim();
            double? t = TraVThuData.ParseNumber(TxtT.Text);
            double? th = TraVThuData.ParseNumber(TxtTHienThi.Text);
            string nguoi = (TxtNguoi.Text ?? string.Empty).Trim();
            string ghiChu = (TxtGhiChu.Text ?? string.Empty).Trim();

            if (loai.Length == 0) { ShowStatus("Chưa nhập loại vật liệu.", false); CmbLoai.Focus(); return; }
            if (!t.HasValue || t.Value <= 0) { ShowStatus("Độ dày T không hợp lệ (dùng dấu chấm, ví dụ 6.0).", false); TxtT.Focus(); return; }
            if (!th.HasValue || th.Value <= 0) { ShowStatus("T thực không hợp lệ (dùng dấu chấm, ví dụ 7.3).", false); TxtTHienThi.Focus(); return; }
            if (nguoi.Length == 0) { ShowStatus("Chưa nhập người xác nhận.", false); TxtNguoi.Focus(); return; }
            if (ghiChu.Length == 0) { ShowStatus("Chưa nhập ghi chú.", false); TxtGhiChu.Focus(); return; }
            if (_images.Count == 0) { ShowStatus("Cần ít nhất 1 ảnh bằng chứng.", false); return; }

            TamRecord old = TraVCalculator.FindTam(loai, t.Value);
            if (old != null)
            {
                ShowStatus($"{old.Loai} T={old.T} đã có T thực = {old.THienThi} (nguồn {old.Nguon}) — không bổ sung đè được.", false);
                return;
            }

            var record = new TamRecord
            {
                Loai = loai,
                T = TxtT.Text.Trim(),
                THienThi = TxtTHienThi.Text.Trim(),
                Nguon = TamRecord.NguonBoSung,
                NguoiXacNhan = nguoi,
                GhiChu = ghiChu
            };

            if (_submit == null)
                return;
            BtnSubmit.IsEnabled = false;
            ShowStatus($"Đang gửi yêu cầu và {_images.Count} ảnh...", true);
            try
            {
                var (ok, message) = await _submit(record, new List<byte[]>(_images));
                ShowStatus(message, ok);
                if (ok)
                {
                    _images.Clear();
                    RefreshImages();
                    UpdateKeyState();
                }
            }
            catch (Exception ex) // async void: lỗi lọt ra ngoài sẽ làm sập CAD
            {
                ShowStatus("Lỗi: " + ex.GetBaseException().Message, false);
            }
            finally
            {
                BtnSubmit.IsEnabled = true;
            }
        }

        private void ShowStatus(string text, bool ok)
        {
            StatusBorder.Visibility = Visibility.Visible;
            TxtStatus.Text = text ?? string.Empty;
            var conv = new BrushConverter();
            StatusBorder.Background = (Brush)conv.ConvertFrom(ok ? "#EFF6FF" : "#FEF2F2");
            StatusBorder.BorderBrush = (Brush)conv.ConvertFrom(ok ? "#BFDBFE" : "#FECACA");
            TxtStatus.Foreground = (Brush)conv.ConvertFrom(ok ? "#1E3A8A" : "#B91C1C");
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
