#if ZWCAD
global using CadApp = ZwSoft.ZwCAD.ApplicationServices.Application;
global using CadDocument = ZwSoft.ZwCAD.ApplicationServices.Document;
global using CadPreTranslateEventArgs = ZwSoft.ZwCAD.ApplicationServices.PreTranslateMessageEventArgs;
global using ResultBuffer = ZwSoft.ZwCAD.DatabaseServices.ResultBuffer;
global using IExtensionApplication = ZwSoft.ZwCAD.Runtime.IExtensionApplication;
global using ExtensionApplicationAttribute = ZwSoft.ZwCAD.Runtime.ExtensionApplicationAttribute;
global using CommandClassAttribute = ZwSoft.ZwCAD.Runtime.CommandClassAttribute;
global using CommandMethodAttribute = ZwSoft.ZwCAD.Runtime.CommandMethodAttribute;
global using LispFunctionAttribute = ZwSoft.ZwCAD.Runtime.LispFunctionAttribute;
global using PaletteSet = ZwSoft.ZwCAD.Windows.PaletteSet;
global using PaletteSetStyles = ZwSoft.ZwCAD.Windows.PaletteSetStyles;
global using DockSides = ZwSoft.ZwCAD.Windows.DockSides;
#elif AUTOCAD
global using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
global using CadDocument = Autodesk.AutoCAD.ApplicationServices.Document;
global using CadPreTranslateEventArgs = Autodesk.AutoCAD.ApplicationServices.PreTranslateMessageEventArgs;
global using ResultBuffer = Autodesk.AutoCAD.DatabaseServices.ResultBuffer;
global using IExtensionApplication = Autodesk.AutoCAD.Runtime.IExtensionApplication;
global using ExtensionApplicationAttribute = Autodesk.AutoCAD.Runtime.ExtensionApplicationAttribute;
global using CommandClassAttribute = Autodesk.AutoCAD.Runtime.CommandClassAttribute;
global using CommandMethodAttribute = Autodesk.AutoCAD.Runtime.CommandMethodAttribute;
global using LispFunctionAttribute = Autodesk.AutoCAD.Runtime.LispFunctionAttribute;
global using PaletteSet = Autodesk.AutoCAD.Windows.PaletteSet;
global using PaletteSetStyles = Autodesk.AutoCAD.Windows.PaletteSetStyles;
global using DockSides = Autodesk.AutoCAD.Windows.DockSides;
#endif

using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin
{
    /// <summary>
    /// Lớp cầu nối đồng nhất các thao tác nền tảng giữa ZWCAD 2026 và AutoCAD.
    /// </summary>
    public static class CadCompat
    {
        public const string RegistrySubKey = @"Software\OFT_Lisp\OhtaTool";

        public static string PlatformName =>
#if ZWCAD
            "ZWCAD 2026 (.NET 4.8)";
#elif AUTOCAD
            "AutoCAD (.NET 10.0)";
#else
            "CAD";
#endif

        public static CadDocument ActiveDocument =>
            CadApp.DocumentManager?.MdiActiveDocument;

        /// <summary>
        /// Khóa Document hiện hành khi thao tác Database từ giao diện Modeless PaletteSet.
        /// </summary>
        public static IDisposable LockActiveDocument()
        {
            return ActiveDocument?.LockDocument();
        }

        /// <summary>
        /// Mở hộp thoại WPF dạng Modal gắn đúng cửa sổ cha của ZWCAD / AutoCAD.
        /// </summary>
        public static bool? ShowModalDialog(System.Windows.Window window)
        {
            if (window == null)
                return null;

            try
            {
                return CadApp.ShowModalWindow(window);
            }
            catch
            {
                return window.ShowDialog();
            }
        }

        /// <summary>
        /// Ghi thông báo ra dòng lệnh CAD.
        /// </summary>
        public static void WriteMessage(string message)
        {
            try
            {
                ActiveDocument?.Editor?.WriteMessage(message);
            }
            catch
            {
                // Bỏ qua nếu chưa có bản vẽ đang mở
            }
        }

        /// <summary>
        /// Gửi chuỗi lệnh thực thi xuống dòng lệnh CAD (tự động đưa focus về vùng vẽ cho entsel/ssget).
        /// </summary>
        public static void SendCommand(string macro)
        {
            if (string.IsNullOrWhiteSpace(macro))
                return;

            try
            {
                CadDocument doc = ActiveDocument;
                if (doc == null)
                {
                    CadApp.ShowAlertDialog("Vui lòng mở ít nhất một bản vẽ trước khi chạy công cụ.");
                    return;
                }

                doc.SendStringToExecute(macro, true, false, true);
            }
            catch (System.Exception ex)
            {
                OhtaTool.Plugin.Core.BugLog.Write("OHTA/CadCompat", ex);
                WriteMessage("\n[OHTA TOOL] Lỗi gửi lệnh CAD: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// Xác định thư mục gốc OHTA_TOOL/ từ vị trí DLL hoặc khóa Registry do OHTA_Loader.lsp lưu.
        /// </summary>
        public static string GetRootDirectory()
        {
            string regRoot = null;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistrySubKey, writable: false))
                {
                    regRoot = key?.GetValue("RootDir") as string;
                }
            }
            catch
            {
                // Bỏ qua lỗi đọc Registry
            }

            string asmPath = null;
            try
            {
                asmPath = Assembly.GetExecutingAssembly().Location;
            }
            catch
            {
            }

            // Ưu tiên tìm ngược từ chính vị trí OHTATOOL.dll để nếu người dùng có nhiều bản sao vẫn lấy đúng bản đang chạy
            string fromAsm = ModuleDiscovery.ResolveRootDirectory(asmPath, null);
            if (!string.IsNullOrEmpty(fromAsm))
                return fromAsm;

            return ModuleDiscovery.ResolveRootDirectory(asmPath, regRoot);
        }
    }
}
