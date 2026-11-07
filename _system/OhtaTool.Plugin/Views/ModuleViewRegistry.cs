using System;
using System.Collections.Generic;
using System.Windows;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Services;
using OhtaTool.Plugin.ViewModels;

namespace OhtaTool.Plugin.Views
{
    /// <summary>
    /// Giao diện hỗ trợ cuộn chuột mượt mà khi con trỏ nằm trên Tab của PaletteSet.
    /// </summary>
    public interface IOhtaScrollableTabView
    {
        void ScrollByMouseWheel(int wheelDelta);
    }

    /// <summary>
    /// Điểm mở rộng cho các mục con về sau:
    /// Mặc định mọi thư mục con dùng ModuleTabControl (danh sách nút bấm kiểu Tool Palettes đọc từ module.json).
    /// Khi một mục con cần giao diện WPF chuyên biệt (bảng dữ liệu, form nhập thông số riêng),
    /// chỉ cần đăng ký vào ModuleViewRegistry theo module.Id hoặc module.FolderName.
    /// </summary>
    public static class ModuleViewRegistry
    {
        private static readonly Dictionary<string, Func<ModuleManifest, Action, FrameworkElement>> CustomFactories =
            new Dictionary<string, Func<ModuleManifest, Action, FrameworkElement>>(StringComparer.OrdinalIgnoreCase);

        static ModuleViewRegistry()
        {
            Func<ModuleManifest, Action, FrameworkElement> fnameFactory = (manifest, reloadCallback) =>
            {
                var vm = new FnameTabViewModel(manifest, reloadCallback);
                FnameCadCommands.ActiveViewModel = vm;
                return new FnameTabControl(vm);
            };

            Register("fname", fnameFactory);
            Register("01_Fname", fnameFactory);

            // Tab Guro: giao diện nằm trong GROTOOL.dll (ADR 0003); không nạp được thì dùng danh sách nút
            Func<ModuleManifest, Action, FrameworkElement> guroFactory = (manifest, reloadCallback) =>
                DllTabLoader.TaoGiaoDien(manifest, "GROTOOL", "ZwCadPlugin.GuroOhtaTab", "Guro") ??
                new ModuleTabControl(new ModuleTabViewModel(manifest, reloadCallback));

            Register("guro", guroFactory);
            Register("03_Guro", guroFactory);

            // Tab PRINT (lệnh OI v2.0) — ADR 0005
            Func<ModuleManifest, Action, FrameworkElement> printFactory = (manifest, reloadCallback) =>
                new PrintTabControl(new PrintTabViewModel(manifest));

            Register("print", printFactory);
            Register("04_In", printFactory);

            // Tab AI: giao diện nằm trong AITOOL.dll (ADR 0006, đang thử nghiệm)
            Func<ModuleManifest, Action, FrameworkElement> aiFactory = (manifest, reloadCallback) =>
                DllTabLoader.TaoGiaoDien(manifest, "AITOOL", "AiTool.AiOhtaTab", "AI") ??
                new ModuleTabControl(new ModuleTabViewModel(manifest, reloadCallback));

            Register("ai", aiFactory);
            Register("05_AI", aiFactory);

            // Tab Teams: nhận đơn từ kênh Microsoft Teams, giao diện trong TEAMSTOOL.dll (ADR 0008)
            Func<ModuleManifest, Action, FrameworkElement> teamsFactory = (manifest, reloadCallback) =>
                DllTabLoader.TaoGiaoDien(manifest, "TEAMSTOOL", "TeamsTool.TeamsOhtaTab", "Teams") ??
                new ModuleTabControl(new ModuleTabViewModel(manifest, reloadCallback));

            Register("teams", teamsFactory);
            Register("06_Teams", teamsFactory);
        }

        public static void Register(string moduleIdOrFolder, Func<ModuleManifest, Action, FrameworkElement> viewFactory)
        {
            if (string.IsNullOrWhiteSpace(moduleIdOrFolder) || viewFactory == null)
                return;

            CustomFactories[moduleIdOrFolder.Trim()] = viewFactory;
        }

        public static FrameworkElement CreateView(ModuleManifest manifest, Action reloadCallback)
        {
            if (manifest != null)
            {
                if (!string.IsNullOrWhiteSpace(manifest.Id) &&
                    CustomFactories.TryGetValue(manifest.Id, out var factoryById))
                {
                    return factoryById(manifest, reloadCallback);
                }

                if (!string.IsNullOrWhiteSpace(manifest.FolderName) &&
                    CustomFactories.TryGetValue(manifest.FolderName, out var factoryByFolder))
                {
                    return factoryByFolder(manifest, reloadCallback);
                }
            }

            var vm = new ModuleTabViewModel(manifest, reloadCallback);
            return new ModuleTabControl(vm);
        }
    }
}

