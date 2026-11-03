using System;
using System.Windows;
using System.Windows.Controls;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.ViewModels;

namespace OhtaTool.Plugin.Views
{
    public partial class ModuleTabControl : UserControl, IOhtaScrollableTabView
    {
        public ModuleTabControl(ModuleTabViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public void ScrollByMouseWheel(int wheelDelta)
        {
            if (MainScroller == null || wheelDelta == 0)
                return;

            double step = (wheelDelta / 120.0) * 48.0;
            MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset - step);
        }

        /// <summary>Dựng menu chuột phải của thẻ lệnh: Sửa / Chuyển nhóm / Lên / Xuống / Xoá.</summary>
        private void OnCardContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var element = sender as FrameworkElement;
            var card = element?.DataContext as ToolCardViewModel;
            var vm = DataContext as ModuleTabViewModel;
            ContextMenu menu = element?.ContextMenu;
            if (card == null || vm == null || menu == null)
            {
                e.Handled = true;
                return;
            }

            menu.Items.Clear();
            menu.Items.Add(Item("✎  Sửa...", () => vm.EditTool(card)));

            var move = new MenuItem { Header = "⇄  Chuyển nhóm" };
            string current = (card.Tool.Category ?? string.Empty).Trim();
            foreach (string g in vm.GroupNames)
            {
                string group = g;
                MenuItem mi = Item(group, () => vm.MoveToGroup(card, group));
                mi.IsChecked = string.Equals(group, current, StringComparison.OrdinalIgnoreCase);
                move.Items.Add(mi);
            }
            MenuItem other = Item(ModuleGrouping.UngroupedName, () => vm.MoveToGroup(card, string.Empty));
            other.IsChecked = current.Length == 0 || ModuleGrouping.IsUngroupedName(current);
            move.Items.Add(other);
            move.Items.Add(new Separator());
            move.Items.Add(Item("Nhóm mới...", () => vm.MoveToNewGroup(card)));
            menu.Items.Add(move);

            menu.Items.Add(Item("▲  Lên", () => vm.MoveTool(card, -1)));
            menu.Items.Add(Item("▼  Xuống", () => vm.MoveTool(card, 1)));
            menu.Items.Add(new Separator());
            if (card.IsLocked)
                menu.Items.Add(new MenuItem { Header = "🔒  Nút gốc — không xoá được", IsEnabled = false });
            else
                menu.Items.Add(Item("✕  Xoá khỏi tab", () => vm.RemoveTool(card)));
        }

        private static MenuItem Item(string header, Action onClick)
        {
            var mi = new MenuItem { Header = header };
            mi.Click += (s, e) => onClick();
            return mi;
        }
    }
}
