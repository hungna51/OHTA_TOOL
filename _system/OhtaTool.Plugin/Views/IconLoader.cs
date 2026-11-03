using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Nạp ảnh nhúng trực tiếp từ ManifestResourceStream của DLL (miễn nhiễm 100% lỗi Pack URI khi NETLOAD trên ZWCAD/AutoCAD).
    /// </summary>
    public static class IconLoader
    {
        private static ImageSource _settingsLogo;

        public static ImageSource GetSettingsLogo()
        {
            if (_settingsLogo != null)
                return _settingsLogo;

            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream stream = asm.GetManifestResourceStream("OhtaTool.Plugin.Assets.settings_logo.png"))
                {
                    if (stream != null)
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = stream;
                        bmp.EndInit();
                        bmp.Freeze();
                        _settingsLogo = bmp;
                    }
                }
            }
            catch
            {
            }

            return _settingsLogo;
        }
    }
}

