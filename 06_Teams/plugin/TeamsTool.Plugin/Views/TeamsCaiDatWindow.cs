using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TeamsTool.Services;

namespace TeamsTool.Views
{
    /// <summary>
    /// Cửa sổ ⚙ Cài đặt tab Teams: Tenant/Client ID (ghi 06_Teams\data\teams.json), thư mục lưu đơn (registry), Đăng xuất.
    /// DaDangXuat = true khi người dùng bấm Đăng xuất (tab quay về màn hình đăng nhập).
    /// </summary>
    internal sealed class TeamsCaiDatWindow : Window
    {
        private readonly TextBox _tenant = O(), _client = O(), _thuMuc = O();
        private readonly TextBlock _baoLoi = new TextBlock { Foreground = KieuTeams.Mau("#B42318"), TextWrapping = TextWrapping.Wrap };
        public bool DaDangXuat { get; private set; }

        private static TextBox O() => new TextBox { Padding = new Thickness(4, 3, 4, 3), Margin = new Thickness(0, 2, 0, 8) };

        public TeamsCaiDatWindow(Brush mau)
        {
            Title = "Cài Đặt Teams";
            Width = 520;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = KieuTeams.Nen;
            FontFamily = KieuTeams.Font;
            FontSize = 12;

            _tenant.Text = CaiDatTeams.TenantId;
            _client.Text = CaiDatTeams.ClientId;
            _thuMuc.Text = CaiDatTeams.ThuMucDon;

            var ma = new StackPanel();
            ma.Children.Add(new TextBlock { Text = "Microsoft 365", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            ma.Children.Add(new TextBlock { Text = "Tenant ID (mã công ty)" });
            ma.Children.Add(_tenant);
            ma.Children.Add(new TextBlock { Text = "Client ID (mã ứng dụng OHTA TOOL)" });
            ma.Children.Add(_client);
            ma.Children.Add(new TextBlock { Text = "Lưu trong " + CaiDatTeams.FileMa + " (không lên GitHub).",
                                            Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap });

            var dong = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
            var chon = KieuTeams.Nut("Chọn…", mau, (s, e) => ChonThuMuc(), chinh: false);
            chon.Margin = new Thickness(6, 2, 0, 8);
            DockPanel.SetDock(chon, Dock.Right);
            dong.Children.Add(chon);
            dong.Children.Add(_thuMuc);
            var don = new StackPanel();
            don.Children.Add(new TextBlock { Text = "Thư mục lưu đơn tải về", FontWeight = FontWeights.SemiBold });
            don.Children.Add(dong);

            var tk = new DockPanel();
            var dx = KieuTeams.Nut("Đăng xuất", mau, (s, e) =>
            {
                GraphTeams.DangXuat();
                DaDangXuat = true;
                Close();
            }, chinh: false);
            dx.IsEnabled = GraphTeams.DaCoDangNhap;
            DockPanel.SetDock(dx, Dock.Right);
            tk.Children.Add(dx);
            tk.Children.Add(new TextBlock { Text = GraphTeams.DaCoDangNhap ? "Tài khoản Teams đang đăng nhập trên máy này." : "Chưa đăng nhập.",
                                            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });

            var nut = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            nut.Children.Add(KieuTeams.Nut("Huỷ", mau, (s, e) => Close(), chinh: false));
            nut.Children.Add(KieuTeams.Nut("Lưu", mau, (s, e) => Luu()));

            var goc = new StackPanel { Margin = new Thickness(12) };
            goc.Children.Add(KieuTeams.The(ma));
            goc.Children.Add(KieuTeams.The(don));
            goc.Children.Add(KieuTeams.The(tk));
            goc.Children.Add(_baoLoi);
            goc.Children.Add(nut);
            Content = goc;
        }

        private void ChonThuMuc()
        {
            using (var hop = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = _thuMuc.Text })
                if (hop.ShowDialog() == System.Windows.Forms.DialogResult.OK) _thuMuc.Text = hop.SelectedPath;
        }

        private void Luu()
        {
            try
            {
                CaiDatTeams.GhiMa(_tenant.Text, _client.Text);
                CaiDatTeams.Ghi("thu_muc", _thuMuc.Text.Trim());
                DialogResult = true;
            }
            catch (Exception ex) { _baoLoi.Text = "Không lưu được: " + ex.Message; }
        }
    }
}
