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
        public static int Main(string[] args)
        {
            string thuMuc = TimThuMucTeams(AppDomain.CurrentDomain.BaseDirectory);
            if (thuMuc == null)
            {
                MessageBox.Show("Không tìm thấy thư mục 06_Teams.\nOhtaTeams.exe phải nằm trong thư mục OHTA_TOOL (_apps\\OhtaTeams\\bin).",
                                "OHTA Teams", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
            CaiDatTeams.ThuMucModule = thuMuc;

            // Tab "Tạo dữ liệu DXF từ ảnh" trên CAD gọi Thử nhanh chạy ẩn (ThuNhanhCli); "--thu-nhanh" mở cửa sổ Thử nhanh
            bool moThuNhanh = args.Length > 0 && args[0] == "--thu-nhanh";
            if (args.Length > 0 && args[0].StartsWith("--thu-nhanh-", StringComparison.Ordinal)) return ThuNhanhCli.Chay(args);

            var app = new Application();
            // Lỗi không bắt được → hiện chữ thay vì tắt im lặng
            app.DispatcherUnhandledException += (s, e) => { BaoLoi(e.Exception); e.Handled = true; };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => BaoLoi(e.ExceptionObject as Exception);
            // Tạo màn hình trong Startup: lúc đó Dispatcher đã chạy nên các await (tải đơn ngay khi mở) quay về đúng luồng giao diện
            if (moThuNhanh)
                app.Startup += (s, e) => new ThuNhanhWindow(KieuTeams.Mau("#5B3FD1")).Show();
            else
            app.Startup += (s, e) => new Window
            {
                Title = "OHTA Teams",
                Width = 1400, // bảng đơn nhiều cột như view Lists
                Height = 860,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new TeamsTabControl("Teams", "Nhận đơn từ Microsoft Teams", "#4B53BC"),
            }.Show();
            return app.Run();
        }

        private static void BaoLoi(Exception ex) =>
            MessageBox.Show("Lỗi: " + ex, "OHTA Teams", MessageBoxButton.OK, MessageBoxImage.Error);

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
