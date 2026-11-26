using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Models;
using ZwCadPlugin.ViewModels;

namespace ZwCadPlugin.Views
{
    public partial class KtWindow : Window
    {
        private const string AppUserModelIdKt = "OhtaTool.GroTool.KtWindow";
        private static ImageSource _iconKt;
        private readonly KtViewModel _vm;

        public KtWindow(KtViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            Icon = _iconKt ?? (_iconKt = TaoIconKt());
            Closed += (s, e) => CloudLoiCad.Xoa();
            KeyDown += (s, e) =>
            {
                if (e.Key != Key.Escape) return;
                CloudLoiCad.Xoa();
                e.Handled = true;
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            DatAppUserModelId(new WindowInteropHelper(this).Handle, AppUserModelIdKt);
        }

        private static ImageSource TaoIconKt()
        {
            var dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                var nen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF6C00"));
                var vien = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C2410C"));
                dc.DrawRoundedRectangle(nen, new Pen(vien, 2.0), new Rect(2, 2, 60, 60), 12, 12);

                var tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
                var ft = new FormattedText("KT", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 30, Brushes.White, 1.0);
                dc.DrawText(ft, new Point((64 - ft.Width) / 2.0, (64 - ft.Height) / 2.0));
            }
            var bmp = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);
            bmp.Freeze();
            return bmp;
        }

        private static void DatAppUserModelId(IntPtr hwnd, string appId)
        {
            if (hwnd == IntPtr.Zero || string.IsNullOrEmpty(appId)) return;
            IPropertyStore store = null;
            var pv = new PropVariant();
            try
            {
                Guid iid = typeof(IPropertyStore).GUID;
                if (SHGetPropertyStoreForWindow(hwnd, ref iid, out store) != 0 || store == null) return;
                var key = new PropertyKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);
                pv.vt = 31; // VT_LPWSTR
                pv.pwszVal = Marshal.StringToCoTaskMemUni(appId);
                store.SetValue(ref key, ref pv);
                store.Commit();
            }
            catch
            {
                // Fallback neu he dieu hanh khong ho tro AppUserModelID theo cua so
            }
            finally
            {
                try { PropVariantClear(ref pv); } catch { }
                if (store != null) { try { Marshal.ReleaseComObject(store); } catch { } }
            }
        }

        [DllImport("shell32.dll")]
        private static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore propertyStore);

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant pvar);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
            public PropertyKey(Guid fmtid, uint pid) { this.fmtid = fmtid; this.pid = pid; }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pwszVal;
        }

        [ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig] int GetCount(out uint cProps);
            [PreserveSig] int GetAt(uint iProp, out PropertyKey pkey);
            [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant pv);
            [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant pv);
            [PreserveSig] int Commit();
        }

        private void SaiSoPhim(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !(sender is TextBox tb)) return;
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            _vm.ApDungSaiSoCmd.Execute(null);
            e.Handled = true;
        }

        private void LoiBam(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is MucErrorItem item)
                _vm.BamLoiCmd.Execute(item);
        }

        private void DuaVeByLayerBam(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.Tag is MucErrorItem item)
                _vm.DuaVeByLayerCmd.Execute(item);
        }
    }
}
