using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OhtaTool.Plugin.Views
{
    public enum OverwriteChoice { Overwrite, SaveAsNumbered, Cancel }

    /// <summary>Hộp thoại của tab PRINT dựng bằng code (giống DialogKit).</summary>
    internal static class PrintDialogs
    {
        public static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));

        /// <summary>File PDF đã có: Ghi đè / Lưu thành (1) / Huỷ in. Đóng cửa sổ = Huỷ in.</summary>
        public static OverwriteChoice AskOverwrite(string existingPath, int fileCount)
        {
            var choice = OverwriteChoice.Cancel;
            Window w = DialogKit.NewWindow("OI — File PDF đã có", 440);
            var panel = new StackPanel { Margin = new Thickness(16) };
            string what = fileCount > 1
                ? $"Đã có file PDF trùng tên (vd {Path.GetFileName(existingPath)}) trong bộ {fileCount} file."
                : $"Đã có file {Path.GetFileName(existingPath)}.";
            panel.Children.Add(new TextBlock { Text = what, TextWrapping = TextWrapping.Wrap, Foreground = DialogKit.TextBrush });
            panel.Children.Add(new TextBlock
            {
                Text = Path.GetDirectoryName(existingPath),
                TextWrapping = TextWrapping.Wrap,
                Foreground = DialogKit.MutedBrush,
                Margin = new Thickness(0, 4, 0, 0)
            });
            panel.Children.Add(DialogKit.ButtonRow(
                DialogKit.Btn("Ghi đè", (s, e) => { choice = OverwriteChoice.Overwrite; w.Close(); }, primary: true, accent: Accent),
                DialogKit.Btn("Lưu thành (1)", (s, e) => { choice = OverwriteChoice.SaveAsNumbered; w.Close(); }),
                DialogKit.Btn("Huỷ in", (s, e) => { choice = OverwriteChoice.Cancel; w.Close(); })));
            w.Content = panel;
            CadCompat.ShowModalDialog(w);
            return choice;
        }
    }
}
