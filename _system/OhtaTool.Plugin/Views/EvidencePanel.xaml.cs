using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Khối bằng chứng dùng chung (người xác nhận + ghi chú + 1–3 ảnh JPEG đã thu nhỏ) cho các số liệu người dùng tự nhập:
    /// T thực (cửa sổ Bổ sung vật liệu) và thông số dao (cửa sổ Bổ sung góc bẻ thử).
    /// </summary>
    public partial class EvidencePanel : UserControl
    {
        private const int MaxImages = 3;
        private const int MaxSide = 1600;
        private const int JpegQuality = 85;

        private readonly List<byte[]> _images = new List<byte[]>();

        public EvidencePanel()
        {
            InitializeComponent();
            RefreshImages();
        }

        /// <summary>Báo lỗi / trạng thái lên cửa sổ chứa (ok = false là lỗi).</summary>
        public Action<string, bool> ShowStatus { get; set; }

        public string Title { get => TxtTitle.Text; set => TxtTitle.Text = value; }

        public string NguoiXacNhan => (TxtNguoi.Text ?? string.Empty).Trim();

        public string GhiChu => (TxtGhiChu.Text ?? string.Empty).Trim();

        public List<byte[]> Images => new List<byte[]>(_images);

        /// <summary>Đủ người xác nhận + ghi chú + ít nhất 1 ảnh; thiếu thì trả câu báo lỗi và đặt focus.</summary>
        public string Validate(string what)
        {
            if (NguoiXacNhan.Length == 0) { TxtNguoi.Focus(); return what + " cần tên người xác nhận."; }
            if (GhiChu.Length == 0) { TxtGhiChu.Focus(); return what + " cần ghi chú (đo bằng gì, ở đâu...)."; }
            if (_images.Count == 0) return what + " cần ít nhất 1 ảnh bằng chứng.";
            return null;
        }

        public void ClearImages()
        {
            _images.Clear();
            RefreshImages();
        }

        /// <summary>Ctrl+V ở cửa sổ chứa khi khối đang hiện và clipboard có ảnh -> thêm ảnh (ô chữ vẫn dán chữ như thường).</summary>
        public bool TryHandlePasteKey(KeyEventArgs e)
        {
            if (IsVisible && e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control
                && (Clipboard.ContainsImage() || Clipboard.ContainsFileDropList()))
            {
                PasteFromClipboard(showEmptyMessage: false);
                return true;
            }
            return false;
        }

        private void OnPickImagesClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn ảnh bằng chứng",
                Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff",
                Multiselect = true
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) != true)
                return;
            foreach (string f in dlg.FileNames)
                AddImageFromFile(f);
        }

        private void OnPasteImageClick(object sender, RoutedEventArgs e) => PasteFromClipboard(showEmptyMessage: true);

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
                    ShowStatus?.Invoke("Clipboard không có ảnh. Chụp màn hình / copy ảnh rồi bấm lại.", false);
            }
            catch (Exception ex)
            {
                ShowStatus?.Invoke("Không đọc được ảnh trong clipboard: " + ex.GetBaseException().Message, false);
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
                ShowStatus?.Invoke($"Không mở được ảnh \"{Path.GetFileName(path)}\": " + ex.GetBaseException().Message, false);
            }
        }

        private void AddImage(BitmapSource source)
        {
            if (source == null)
                return;
            if (_images.Count >= MaxImages)
            {
                ShowStatus?.Invoke($"Tối đa {MaxImages} ảnh — bỏ bớt ảnh trước khi thêm.", false);
                return;
            }
            _images.Add(ToJpeg(source));
            RefreshImages();
        }

        /// <summary>Thu nhỏ cạnh dài về ≤ MaxSide rồi nén JPEG (bỏ kênh alpha — ảnh chụp màn hình đôi khi alpha = 0).</summary>
        private static byte[] ToJpeg(BitmapSource source)
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
    }
}
