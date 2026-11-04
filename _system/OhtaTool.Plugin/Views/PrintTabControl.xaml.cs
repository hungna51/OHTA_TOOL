using System.Windows.Controls;
using OhtaTool.Plugin.ViewModels;

namespace OhtaTool.Plugin.Views
{
    /// <summary>Tab PRINT (lệnh OI v2.0).</summary>
    public partial class PrintTabControl : UserControl
    {
        public PrintTabControl(PrintTabViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
