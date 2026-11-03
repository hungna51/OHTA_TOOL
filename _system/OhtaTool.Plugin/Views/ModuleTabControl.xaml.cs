using System.Windows.Controls;
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
    }
}

