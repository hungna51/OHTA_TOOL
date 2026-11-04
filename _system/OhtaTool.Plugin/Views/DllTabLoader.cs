using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using OhtaTool.Plugin.Core;
#if ZWCAD
using CadExtensionLoader = ZwSoft.ZwCAD.Runtime.ExtensionLoader;
#elif AUTOCAD
using CadExtensionLoader = Autodesk.AutoCAD.Runtime.ExtensionLoader;
#endif

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Tab có giao diện nằm trong DLL riêng của mục con (Guro: GROTOOL.dll — ADR 0003;
    /// AI: AITOOL.dll — ADR 0005), không nằm trong OHTATOOL.dll. Ở đây chỉ nạp DLL đó
    /// (thư mục bin\&lt;ZwCAD|AutoCAD&gt; của mục con) và gọi
    /// &lt;tenLop&gt;.TaoGiaoDien(tieuDe, moTa, mauNhan, thuMucModule) bằng phản chiếu — các DLL
    /// không tham chiếu nhau khi build. Không nạp được thì trả null để ModuleViewRegistry
    /// dùng danh sách nút của module.json.
    /// </summary>
    internal static class DllTabLoader
    {
        private const string TenHam = "TaoGiaoDien";

        private static string ThuMucNenTang =>
#if ZWCAD
            "ZwCAD";
#else
            "AutoCAD";
#endif

        public static FrameworkElement TaoGiaoDien(ModuleManifest manifest, string tenDll, string tenLop, string tenTab)
        {
            try
            {
                string dll = Path.Combine(manifest.DirectoryPath, "bin", ThuMucNenTang, tenDll + ".dll");
                Assembly asm = TimHoacNap(dll, tenDll, tenTab);
                if (asm == null)
                    return null;

                MethodInfo ham = asm.GetType(tenLop)?.GetMethod(TenHam, BindingFlags.Public | BindingFlags.Static);
                if (ham == null)
                {
                    CadCompat.WriteMessage("\n[OHTA TOOL] " + tenDll + ".dll chua co " + tenLop + "." + TenHam +
                                           " (ban cu?) -> tab " + tenTab + " dung danh sach nut.\n");
                    return null;
                }

                var view = ham.Invoke(null, new object[]
                {
                    manifest.DisplayTitle, manifest.Description, manifest.AccentColor, manifest.DirectoryPath
                }) as FrameworkElement;
                return view == null ? null : new CuonHo(view);
            }
            catch (Exception ex)
            {
                Exception goc = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                CadCompat.WriteMessage("\n[OHTA TOOL] Khong dung duoc tab " + tenTab + " tu " + tenDll + ".dll: " + goc.Message +
                                       " -> dung danh sach nut.\n");
                return null;
            }
        }

        /// <summary>
        /// DLL có thể đã được NETLOAD trước (vd. lisp GRO nạp GROTOOL.dll) -> dùng lại bản đó.
        /// Chưa có thì nạp bằng ExtensionLoader (như NETLOAD) để CAD đăng ký luôn lệnh và hàm lisp của nó.
        /// </summary>
        private static Assembly TimHoacNap(string dll, string tenDll, string tenTab)
        {
            Assembly daNap = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, tenDll, StringComparison.OrdinalIgnoreCase));
            if (daNap != null)
                return daNap;

            if (!File.Exists(dll))
            {
                CadCompat.WriteMessage("\n[OHTA TOOL] Khong thay " + dll + " -> tab " + tenTab + " dung danh sach nut.\n");
                return null;
            }

            CadExtensionLoader.Load(dll);
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, tenDll, StringComparison.OrdinalIgnoreCase))
                ?? Assembly.LoadFrom(dll);
        }

        /// <summary>Chuyển lăn chuột của PaletteSet sang hàm ScrollByMouseWheel của giao diện trong DLL của tab.</summary>
        private sealed class CuonHo : ContentControl, IOhtaScrollableTabView
        {
            private readonly MethodInfo _cuon;
            private readonly object _view;

            public CuonHo(FrameworkElement view)
            {
                _view = view;
                _cuon = view.GetType().GetMethod("ScrollByMouseWheel", new[] { typeof(int) });
                Content = view;
            }

            public void ScrollByMouseWheel(int wheelDelta)
            {
                try { _cuon?.Invoke(_view, new object[] { wheelDelta }); }
                catch { }
            }
        }
    }
}
