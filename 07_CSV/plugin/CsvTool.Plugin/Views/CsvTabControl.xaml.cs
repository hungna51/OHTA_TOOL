using System.Windows.Controls;

namespace CsvTool.Views
{
    public partial class CsvTabControl : UserControl
    {
        public CsvTabControl(string tieuDe, string moTa)
        {
            InitializeComponent();
            TieuDe.Text = tieuDe;
            MoTa.Text = moTa;
        }
    }
}
