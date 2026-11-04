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
    /// Tab Guro (ADR 0003): giao diện nằm trong GROTOOL.dll của mục con 03_Guro, không
    /// nằm trong OHTATOOL.dll. Ở đây chỉ nạp DLL đó và gọi
    /// ZwCadPlugin.GuroOhtaTab.TaoGiaoDien(tieuDe, moTa, mauNhan, thuMucModule) bằng
    /// phản chiếu — hai DLL không tham chiếu nhau khi build. Không nạp được thì trả null
    /// để ModuleViewRegistry dùng danh sách nút của module.json.
    /// </summary>
    internal static class GuroTabLoader
    {
        private const string TenLop = "ZwCadPlugin.GuroOhtaTab";
        private const string TenHam = "TaoGiaoDien";

        private static string ThuMucNenTang =>
#if ZWCAD
            "ZwCAD";
#else
            "AutoCAD";
#endif

        public static FrameworkElement TaoGiaoDien(ModuleManifest manifest)
        {
            try
            {
                string dll = Path.Combine(manifest.DirectoryPath, "bin", ThuMucNenTang, "GROTOOL.dll");
                Assembly asm = TimHoacNap(dll);
                if (asm == null)
                    return null;

                MethodInfo ham = asm.GetType(TenLop)?.GetMethod(TenHam, BindingFlags.Public | BindingFlags.Static);
                if (ham == null)
                {
                    CadCompat.WriteMessage("\n[OHTA TOOL] GROTOOL.dll chua co " + TenLop + "." + TenHam +
                                           " (ban cu?) -> tab Guro dung danh sach nut.\n");
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
                CadCompat.WriteMessage("\n[OHTA TOOL] Khong dung duoc tab Guro tu GROTOOL.dll: " + goc.Message +
                                       " -> dung danh sach nut.\n");
                return null;
            }
        }

        /// <summary>
        /// Lisp GRO có thể đã NETLOAD GROTOOL.dll trước -> dùng lại bản đó. Chưa có thì nạp
        /// bằng ExtensionLoader (như NETLOAD) để CAD đăng ký luôn lệnh và hàm lisp của nó.
        /// </summary>
        private static Assembly TimHoacNap(string dll)
        {
            Assembly daNap = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "GROTOOL", StringComparison.OrdinalIgnoreCase));
            if (daNap != null)
                return daNap;

            if (!File.Exists(dll))
            {
                CadCompat.WriteMessage("\n[OHTA TOOL] Khong thay " + dll + " -> tab Guro dung danh sach nut.\n");
                return null;
            }

            CadExtensionLoader.Load(dll);
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "GROTOOL", StringComparison.OrdinalIgnoreCase))
                ?? Assembly.LoadFrom(dll);
        }

        /// <summary>Chuyển lăn chuột của PaletteSet sang hàm ScrollByMouseWheel của giao diện GROTOOL.</summary>
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
