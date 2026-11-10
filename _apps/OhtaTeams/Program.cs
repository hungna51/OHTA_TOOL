using System;
using System.IO;
using System.Windows;
using TeamsTool.Services;
using TeamsTool.Views;

namespace OhtaTeams
{
    /// <summary>Cửa vào OhtaTeams.exe: một cửa sổ chứa màn hình duyệt đơn của tab Teams.</summary>
    public static class Program
    {
        [STAThread]
        public static int Main()
        {
            string thuMuc = TimThuMucTeams(AppDomain.CurrentDomain.BaseDirectory);
            if (thuMuc == null)
            {
                MessageBox.Show("Không tìm thấy thư mục 06_Teams.\nOhtaTeams.exe phải nằm trong thư mục OHTA_TOOL (_apps\\OhtaTeams\\bin).",
                                "OHTA Teams", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
            CaiDatTeams.ThuMucModule = thuMuc;

            var app = new Application();
            var cuaSo = new Window
            {
                Title = "OHTA Teams",
                Width = 520,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new TeamsTabControl("Teams", "Nhận đơn từ Microsoft Teams", "#4B53BC"),
            };
            return app.Run(cuaSo);
        }

        /// <summary>Đi ngược từ thư mục exe lên tới khi gặp thư mục con 06_Teams (chứa data\teams.json).</summary>
        public static string TimThuMucTeams(string tu)
        {
            for (var d = new DirectoryInfo(tu); d != null; d = d.Parent)
            {
                string p = Path.Combine(d.FullName, "06_Teams");
                if (Directory.Exists(p)) return p;
            }
            return null;
        }
    }
}
