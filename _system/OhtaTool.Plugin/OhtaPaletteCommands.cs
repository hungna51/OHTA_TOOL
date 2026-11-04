using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Views;

[assembly: ExtensionApplication(typeof(OhtaTool.Plugin.OhtaPaletteCommands))]
[assembly: CommandClass(typeof(OhtaTool.Plugin.OhtaPaletteCommands))]

namespace OhtaTool.Plugin
{
    /// <summary>
    /// Quản lý bảng Tool Palettes "OHTA TOOL" với dải Tab sườn dọc nguyên bản của CAD (1 Thư mục con = 1 Tab).
    /// Hoạt động đồng nhất trên cả ZWCAD 2026 (net48) và AutoCAD (net10.0-windows).
    /// </summary>
    public sealed class OhtaPaletteCommands : IExtensionApplication
    {
        private sealed class HostedModuleTab
        {
            public ModuleManifest Manifest { get; set; }
            public ElementHost Host { get; set; }
            public FrameworkElement View { get; set; }
        }

        private static readonly Guid PaletteGuid = new Guid("9B41E7C2-63F0-4D5A-8B1E-2F709A4C8D11");
        private static PaletteSet _paletteSet;
        private static readonly List<HostedModuleTab> _hostedTabs = new List<HostedModuleTab>();
        private static bool _mouseWheelHooked;

        public void Initialize()
        {
            try
            {
                OhtaUpdateCommands.OnInitialize();
                CadCompat.WriteMessage(
                    $"\n[OHTA TOOL] Đã nạp OHTATOOL.dll v{OhtaUpdateCommands.LoadedVersion} cho {CadCompat.PlatformName}. Gõ lệnh OHTA để mở Tool Palettes.\n");
            }
            catch
            {
                // Không để throw exception trong Initialize()
            }
        }

        public void Terminate()
        {
            if (_mouseWheelHooked)
            {
                try { CadApp.PreTranslateMessage -= OnPreTranslateMessage; } catch { }
                _mouseWheelHooked = false;
            }

            ClearHostedTabs();

            if (_paletteSet != null)
            {
                try
                {
                    _paletteSet.Visible = false;
                    _paletteSet.Dispose();
                }
                catch { }
                _paletteSet = null;
            }
        }

        // ====================================================================
        // Commands & LispFunctions (hỗ trợ cả gõ trực tiếp lẫn gọi từ OHTA_Loader.lsp)
        // ====================================================================

        [CommandMethod("OHTA")]
        [CommandMethod("OHTA_OPEN")]
        [CommandMethod("OHTAPALETTE")]
        public static void CmdOpenOhtaPalette()
        {
            ShowOrBuildPalette(forceRebuild: false);
        }

        [LispFunction("OHTA_NET_OPEN")]
        public static object LispOpenOhtaPalette(ResultBuffer rb)
        {
            return ShowOrBuildPalette(forceRebuild: false) ? (object)true : null;
        }

        /// <summary>
        /// Lệnh tắt FN: mở palette và chuyển thẳng sang tab Fname.
        /// Thay lệnh FN cũ của lisp GURO (lệnh .NET được CAD ưu tiên hơn c:FN của lisp).
        /// </summary>
        [CommandMethod("FN")]
        public static void CmdOpenFnameTab()
        {
            ActivateModuleTab("fname");
        }

        /// <summary>OI v2.0: mở bảng OHTA và chuyển sang tab PRINT (thay lisp OI cũ).</summary>
        [CommandMethod("OI")]
        public static void CmdOpenPrintTab()
        {
            ActivateModuleTab("print");
        }

        /// <summary>
        /// (ohta_net_tab "guro"): mở palette và chuyển sang tab có module.Id đó; T nếu được,
        /// nil nếu không. Lisp của mục con dùng để mở tab của mình (lệnh GRO — mục 193 GURO).
        /// </summary>
        [LispFunction("OHTA_NET_TAB")]
        public static object LispActivateModuleTab(ResultBuffer rb)
        {
            string id = null;
            if (rb != null)
                foreach (var tv in rb.AsArray())
                {
                    id = tv.Value as string;
                    break;
                }
            return !string.IsNullOrWhiteSpace(id) && ActivateModuleTab(id.Trim()) ? (object)true : null;
        }

        /// <summary>Mở palette (giữ nguyên tab đã nạp) rồi kích hoạt tab có module.Id tương ứng.</summary>
        public static bool ActivateModuleTab(string moduleId)
        {
            if (!ShowOrBuildPalette(forceRebuild: false))
                return false;

            int index = _hostedTabs.FindIndex(t => string.Equals(t.Manifest?.Id, moduleId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                CadCompat.WriteMessage($"\n[OHTA TOOL] Không tìm thấy tab '{moduleId}' — gõ OHTARELOAD để nạp lại danh sách tab.\n");
                return false;
            }

            try { _paletteSet.Activate(index); } catch { }
            return true;
        }

        [CommandMethod("OHTARELOAD")]
        [CommandMethod("OHTA_RELOAD")]
        public static void CmdReloadOhtaPalette()
        {
            ShowOrBuildPalette(forceRebuild: true);
        }

        [LispFunction("OHTA_NET_RELOAD")]
        public static object LispReloadOhtaPalette(ResultBuffer rb)
        {
            return ShowOrBuildPalette(forceRebuild: true) ? (object)true : null;
        }

        [CommandMethod("OHTACLOSE")]
        [CommandMethod("OHTA_CLOSE")]
        public static void CmdToggleClosePalette()
        {
            if (_paletteSet == null)
            {
                ShowOrBuildPalette(forceRebuild: false);
                return;
            }

            _paletteSet.Visible = !_paletteSet.Visible;
            if (_paletteSet.Visible)
                UnrollIfAutoHidden();
        }

        [LispFunction("OHTA_NET_CLOSE")]
        public static object LispClosePalette(ResultBuffer rb)
        {
            CmdToggleClosePalette();
            return true;
        }

        [CommandMethod("OHTAINFO")]
        [CommandMethod("OHTA_INFO")]
        public static void CmdShowInfo()
        {
            string root = CadCompat.GetRootDirectory() ?? "(Không tìm thấy)";
            List<ModuleManifest> modules = Directory.Exists(root)
                ? ModuleDiscovery.DiscoverModules(root)
                : new List<ModuleManifest>();

            CadCompat.WriteMessage("\n================ OHTA TOOL INFO ================\n");
            CadCompat.WriteMessage($"  Nhà phát triển: {ToolInfoCore.Developer}\n");
            CadCompat.WriteMessage($"  Phiên bản    : v{OhtaUpdateCommands.LoadedVersion} (OHTAUPDATE: kiểm tra bản mới)\n");
            CadCompat.WriteMessage($"  Nền tảng CAD : {CadCompat.PlatformName}\n");
            CadCompat.WriteMessage($"  Thư mục gốc  : {root}\n");
            CadCompat.WriteMessage($"  Số Tab nhóm  : {modules.Count}\n");
            for (int i = 0; i < modules.Count; i++)
            {
                ModuleManifest m = modules[i];
                string version = m.VersionLabel.Length == 0 ? "" : " " + m.VersionLabel;
                CadCompat.WriteMessage($"    [{i + 1}] {m.TabTitle}{version} ({m.FolderName}) -> {ToolInfoCore.DescribeContents(m)}\n");
            }
            CadCompat.WriteMessage("================================================\n");
        }

        [LispFunction("OHTA_NET_INFO")]
        public static object LispShowInfo(ResultBuffer rb)
        {
            CmdShowInfo();
            return true;
        }

        // ====================================================================
        // PaletteSet Lifecycle & Dynamic Vertical Tabs
        // ====================================================================

        private static string PaletteTitle => "OHTA TOOL v" + OhtaUpdateCommands.LoadedVersion;

        public static bool ShowOrBuildPalette(bool forceRebuild)
        {
            try
            {
                string rootDir = CadCompat.GetRootDirectory();
                if (string.IsNullOrWhiteSpace(rootDir) || !Directory.Exists(rootDir))
                {
                    CadApp.ShowAlertDialog(
                        "Không xác định được thư mục gốc OHTA_TOOL!\nHãy nạp từ file OHTA_TOOL\\OHTA_Loader.lsp.");
                    return false;
                }

                bool createdNew = false;
                if (_paletteSet == null)
                {
                    _paletteSet = new PaletteSet(PaletteTitle, "OHTA", PaletteGuid)
                    {
                        Style = PaletteSetStyles.ShowAutoHideButton |
                                PaletteSetStyles.ShowCloseButton |
                                PaletteSetStyles.ShowPropertiesMenu |
                                PaletteSetStyles.Snappable |
                                PaletteSetStyles.ShowTabForSingle |
                                PaletteSetStyles.PauseAutoRollupForChildModalDialog,
                        MinimumSize = new System.Drawing.Size(340, 440),
                        Size = new System.Drawing.Size(380, 660),
                        Dock = DockSides.Right
                    };

                    EnsureMouseWheelHook();
                    createdNew = true;
                }

                if (createdNew || forceRebuild || _hostedTabs.Count == 0)
                {
                    RebuildModuleTabs(rootDir);
                }

                _paletteSet.Visible = true;
                // CAD khôi phục tên cũ đã lưu trong profile theo PaletteGuid khi hiện lần đầu -> gán lại sau khi hiện
                if (_paletteSet.Name != PaletteTitle)
                    _paletteSet.Name = PaletteTitle;
                UnrollIfAutoHidden();

                if (forceRebuild)
                {
                    CadCompat.WriteMessage(
                        $"\n[OHTA TOOL] Đã làm mới danh sách Tab ({_hostedTabs.Count} mục con từ '{rootDir}').\n");
                }

                return true;
            }
            catch (System.Exception ex)
            {
                CadCompat.WriteMessage($"\n[OHTA TOOL] Lỗi mở Tool Palettes: {ex.Message}\n{ex.StackTrace}\n");
                CadApp.ShowAlertDialog("Lỗi khi mở OHTA TOOL Palette:\n" + ex.Message);
                return false;
            }
        }

        private static void RebuildModuleTabs(string rootDir)
        {
            if (_paletteSet == null)
                return;

            // Xóa toàn bộ các Tab hiện tại từ cuối về đầu
            while (_paletteSet.Count > 0)
            {
                _paletteSet.Remove(_paletteSet.Count - 1);
            }
            ClearHostedTabs();

            List<ModuleManifest> modules = ModuleDiscovery.DiscoverModules(rootDir);
            if (modules.Count == 0)
            {
                modules.Add(new ModuleManifest
                {
                    Id = "empty_placeholder",
                    FolderName = "OHTA_TOOL",
                    DirectoryPath = rootDir,
                    ReadmePath = Path.Combine(rootDir, "README.md"),
                    RulesDirectoryPath = Path.Combine(rootDir, "rules"),
                    TabTitle = "Hướng Dẫn",
                    Title = "Chưa Có Thư Mục Mục Con",
                    Description = "Hãy nhân bản thư mục _Template_Module thành 01_Ten_Nhom rồi bấm ⟳ để tạo Tab đầu tiên.",
                    AccentColor = "#455A64",
                    Tools = new List<ToolItem>()
                });
            }

            foreach (ModuleManifest module in modules)
            {
                // Làm mới xong quay lại đúng tab đang dùng (sau khi thêm/sửa lệnh từ tab đó)
                string moduleId = module.Id;
                FrameworkElement view = ModuleViewRegistry.CreateView(
                    module,
                    () =>
                    {
                        if (ShowOrBuildPalette(forceRebuild: true))
                            ActivateModuleTab(moduleId);
                    });

                // Bắt buộc dùng ElementHost Dock=Fill (tránh lỗi cắt chiều cao ScrollViewer của AddVisual trên ZWCAD)
                var host = new ElementHost
                {
                    Dock = DockStyle.Fill,
                    Child = view
                };

                _paletteSet.Add(module.TabTitle, host);
                _hostedTabs.Add(new HostedModuleTab
                {
                    Manifest = module,
                    Host = host,
                    View = view
                });

                LoadStartupLispFiles(module);
            }

            AddInfoTab(modules, rootDir);

            if (_paletteSet.Count > 0)
            {
                try { _paletteSet.Activate(0); } catch { }
            }
        }

        /// <summary>Tab Info dựng sẵn, luôn nằm cuối (không phải thư mục mục con, không xoá / tắt được).</summary>
        private static void AddInfoTab(List<ModuleManifest> modules, string rootDir)
        {
            try
            {
                var view = new InfoTabControl(
                    modules,
                    OhtaUpdateCommands.LoadedVersion,
                    UpdateVersion.ReadInstalled(rootDir),
                    CadCompat.PlatformName,
                    rootDir,
                    OhtaUpdateCommands.DescribeStatus,
                    OhtaUpdateCommands.CmdUpdateNow,
                    OhtaUpdateCommands.CmdRollback);

                var host = new ElementHost { Dock = DockStyle.Fill, Child = view };
                _paletteSet.Add(ToolInfoCore.InfoTabTitle, host);
                _hostedTabs.Add(new HostedModuleTab
                {
                    Manifest = new ModuleManifest
                    {
                        Id = ToolInfoCore.InfoTabId,
                        TabTitle = ToolInfoCore.InfoTabTitle,
                        Title = "OHTA TOOL",
                        AccentColor = ToolInfoCore.InfoAccentColor,
                        DirectoryPath = rootDir,
                        Tools = new List<ToolItem>()
                    },
                    Host = host,
                    View = view
                });
            }
            catch (System.Exception ex)
            {
                CadCompat.WriteMessage("\n[OHTA TOOL] Khong dung duoc tab Info: " + ex.Message + "\n");
            }
        }

        private static void LoadStartupLispFiles(ModuleManifest module)
        {
            if (module?.StartupLisp == null || module.StartupLisp.Count == 0)
                return;

            CadDocument doc = CadCompat.ActiveDocument;
            if (doc == null)
                return;

            foreach (string relPath in module.StartupLisp)
            {
                string expr = MacroBuilder.BuildLoadLispExpression(module.DirectoryPath, relPath);
                if (!string.IsNullOrEmpty(expr))
                {
                    try
                    {
                        doc.SendStringToExecute(expr + " (princ)\n", false, false, false);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void ClearHostedTabs()
        {
            foreach (HostedModuleTab tab in _hostedTabs)
            {
                try
                {
                    if (tab.Host != null)
                    {
                        tab.Host.Child = null;
                        tab.Host.Dispose();
                    }
                }
                catch { }
            }
            _hostedTabs.Clear();
        }

        private static void UnrollIfAutoHidden()
        {
            try
            {
                if (_paletteSet != null && _paletteSet.RolledUp)
                    _paletteSet.RolledUp = false;
            }
            catch
            {
                // Phiên bản CAD không hỗ trợ RolledUp thì bỏ qua an toàn
            }
        }

        // ====================================================================
        // Hỗ trợ con lăn chuột mượt mà trên mọi Tab của PaletteSet
        // ====================================================================

        private const int WM_MOUSEWHEEL = 0x020A;

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Win32Point pt);

        [DllImport("user32.dll")]
        private static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        private static void EnsureMouseWheelHook()
        {
            if (_mouseWheelHooked)
                return;

            try
            {
                CadApp.PreTranslateMessage += OnPreTranslateMessage;
                _mouseWheelHooked = true;
            }
            catch
            {
            }
        }

        private static void OnPreTranslateMessage(object sender, CadPreTranslateEventArgs e)
        {
            try
            {
                System.Windows.Interop.MSG m = e.Message;
                if (m.message != WM_MOUSEWHEEL)
                    return;

                if (_paletteSet == null || !_paletteSet.Visible || _hostedTabs.Count == 0)
                    return;

                IntPtr targetHwnd = WindowFromPoint(new Win32Point { X = m.pt_x, Y = m.pt_y });
                if (targetHwnd == IntPtr.Zero)
                    return;

                int delta = (short)(((long)m.wParam >> 16) & 0xFFFF);
                if (delta == 0)
                    return;

                for (int i = 0; i < _hostedTabs.Count; i++)
                {
                    HostedModuleTab tab = _hostedTabs[i];
                    ElementHost host = tab.Host;
                    if (host == null || !host.Visible || !host.IsHandleCreated)
                        continue;

                    if (targetHwnd == host.Handle || IsChild(host.Handle, targetHwnd))
                    {
                        if (tab.View is IOhtaScrollableTabView scrollable)
                        {
                            scrollable.ScrollByMouseWheel(delta);
                            e.Handled = true;
                        }
                        return;
                    }
                }
            }
            catch
            {
                // Tuyệt đối không để ngoại lệ làm ảnh hưởng vòng lặp thông điệp của CAD
            }
        }
    }
}

