using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TeamsTool.Views
{
    /// <summary>Kiểu dáng chung với tab AI / Guro (nền #F5F7FA, nút bo góc màu tab, thẻ trắng viền nhạt) — dựng bằng code.</summary>
    internal static class KieuTeams
    {
        public static readonly Brush Nen = Mau("#F5F7FA");
        public static readonly Brush ChuPhu = Mau("#5F6B7A");
        public static readonly FontFamily Font = new FontFamily("Segoe UI, Meiryo UI, Yu Gothic UI");

        public static Brush Mau(string hex) => (Brush)new BrushConverter().ConvertFromString(hex);

        /// <summary>Nút bo góc: chính = nền màu tab chữ trắng; phụ = nền xám nhạt.</summary>
        public static Button Nut(string chu, Brush nen, RoutedEventHandler bam, bool chinh = true)
        {
            var bd = new FrameworkElementFactory(typeof(Border), "Bd");
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            bd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            var t = new ControlTemplate(typeof(Button)) { VisualTree = bd };
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.9, "Bd"));
            var tat = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            tat.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45, "Bd"));
            t.Triggers.Add(hover);
            t.Triggers.Add(tat);

            var b = new Button
            {
                Content = chu,
                Template = t,
                Cursor = Cursors.Hand,
                Background = chinh ? nen : Mau("#E4E7EC"),
                Foreground = chinh ? Brushes.White : Mau("#323639"),
                FontWeight = chinh ? FontWeights.SemiBold : FontWeights.Normal,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 2, 6, 2),
            };
            if (bam != null) b.Click += bam;
            return b;
        }

        public static Border The(UIElement noiDung) => new Border
        {
            Child = noiDung,
            Background = Brushes.White,
            BorderBrush = Mau("#E1E4E8"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8),
        };

        /// <summary>Nút bánh răng trắng trên header (cùng ảnh tab AI), tô màu tab bằng OpacityMask.</summary>
        public static Button NutCaiDat(Brush mau, RoutedEventHandler bam)
        {
            var bd = new FrameworkElementFactory(typeof(Border), "Bd");
            bd.SetValue(Border.BackgroundProperty, Brushes.White);
            bd.SetValue(Border.BorderBrushProperty, Mau("#CFD8DC"));
            bd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            var t = new ControlTemplate(typeof(Button)) { VisualTree = bd };
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, Mau("#ECEFF1"), "Bd"));
            t.Triggers.Add(hover);

            ImageSource logo = Logo();
            UIElement hinh = logo == null
                ? (UIElement)new TextBlock { Text = "⚙", FontSize = 16, Foreground = mau }
                : new System.Windows.Shapes.Rectangle { Width = 18, Height = 18, Fill = mau, OpacityMask = new ImageBrush(logo) { Stretch = Stretch.Uniform } };
            var b = new Button { Template = t, Width = 30, Height = 30, Cursor = Cursors.Hand, Content = hinh,
                                 VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(6, 0, 0, 0), ToolTip = "Cài đặt Teams" };
            b.Click += bam;
            return b;
        }

        private static ImageSource _logo;
        private static ImageSource Logo()
        {
            if (_logo != null) return _logo;
            try
            {
                using (Stream s = typeof(KieuTeams).Assembly.GetManifestResourceStream("TeamsTool.Assets.settings_logo.png"))
                {
                    if (s == null) return null;
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = s;
                    bmp.EndInit();
                    bmp.Freeze();
                    return _logo = bmp;
                }
            }
            catch (Exception) { return null; }
        }
    }
}
