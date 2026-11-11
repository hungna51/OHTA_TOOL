using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TeamsTool.Core;
using TeamsTool.Services;

namespace TeamsTool.Views
{
    /// <summary>
    /// Cửa sổ ⚙ Cài đặt tab Teams: Tenant/Client ID (ghi 06_Teams\data\teams.json), thư mục đơn (registry thu_muc_goc), Đăng xuất.
    /// DaDangXuat = true khi người dùng bấm Đăng xuất (tab quay về màn hình đăng nhập).
    /// </summary>
    internal sealed class TeamsCaiDatWindow : Window
    {
        private readonly TextBox _tenant = O(), _client = O(), _thuMuc = O();
        private readonly TextBlock _baoLoi = new TextBlock { Foreground = KieuTeams.Mau("#B42318"), TextWrapping = TextWrapping.Wrap };
        public bool DaDangXuat { get; private set; }

        private static TextBox O() => new TextBox { Padding = new Thickness(4, 3, 4, 3), Margin = new Thickness(0, 2, 0, 8) };

        private readonly List<CheckBox> _nhanVien = new List<CheckBox>(); // ô "hiện" từng người — bỏ tích = ẩn khỏi ô chọn người
        private readonly CheckBox _tuChuyen = new CheckBox { Content = "Máy này tự chuyển tiếp chat OHTA-受注 → OHTA-日本 (5 phút một lần, khi OhtaTeams đang mở)",
                                                             Margin = new Thickness(0, 0, 0, 4) };

        /// <param name="danhBa">Tên mọi người (danh bạ công ty + người trong đơn) để chọn ẩn / hiện; rỗng = chưa mở danh sách đơn.</param>
        public TeamsCaiDatWindow(Brush mau, List<string> danhBa = null)
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

            var ma = new StackPanel();
            ma.Children.Add(new TextBlock { Text = "Microsoft 365", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            ma.Children.Add(new TextBlock { Text = "Tenant ID (mã công ty)" });
            ma.Children.Add(_tenant);
            ma.Children.Add(new TextBlock { Text = "Client ID (mã ứng dụng OHTA TOOL)" });
            ma.Children.Add(_client);
            ma.Children.Add(new TextBlock { Text = "Lưu trong " + CaiDatTeams.FileMa + " (không lên GitHub).",
                                            Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap });

            _thuMuc.Text = CaiDatTeams.ThuMucGoc;
            var tm = new StackPanel();
            tm.Children.Add(new TextBlock { Text = "Thư mục đơn", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            tm.Children.Add(new TextBlock { Text = "Thư mục chứa các thư mục đơn (tên thư mục có Title đơn). Tab Teams trên CAD dùng để biết bản vẽ đang mở thuộc đơn nào.",
                                            TextWrapping = TextWrapping.Wrap });
            var hangTm = new DockPanel();
            var chon = KieuTeams.Nut("Chọn…", mau, (s, e) => ChonThuMuc(), chinh: false);
            chon.Margin = new Thickness(6, 2, 0, 8);
            DockPanel.SetDock(chon, Dock.Right);
            hangTm.Children.Add(chon);
            hangTm.Children.Add(_thuMuc);
            tm.Children.Add(hangTm);

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
            goc.Children.Add(KieuTeams.The(tm));
            goc.Children.Add(KieuTeams.The(KhoiNhanVien(mau, danhBa ?? new List<string>())));
            // Chuyển tiếp chat: bật trên 1–2 máy; chỉ chuyển tin từ 0 giờ ngày bật lần đầu, kiểm tra trùng nên nhiều máy bật cũng không gửi đôi
            _tuChuyen.IsChecked = CaiDatTeams.TuChuyenTiep;
            var ct = new StackPanel();
            ct.Children.Add(new TextBlock { Text = "Chuyển tiếp chat", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            ct.Children.Add(_tuChuyen);
            string kq = CaiDatTeams.KetQuaChuyen;
            ct.Children.Add(new TextBlock { Text = "Lần chuyển gần nhất: " + (kq.Length > 0 ? kq : "chưa có")
                                                   + (CaiDatTeams.MocChuyen is DateTime moc ? " · chuyển tin từ " + moc.ToString("dd/MM HH:mm") : ""),
                                            Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap });
            ct.Children.Add(new TextBlock { Text = "Chuột phải vào tin / tiêu đề nhóm OHTA-受注 trong chi tiết đơn để chuyển tiếp ngay (không cần bật ô này).",
                                            Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap });
            goc.Children.Add(KieuTeams.The(ct));
            goc.Children.Add(KieuTeams.The(tk));
            goc.Children.Add(_baoLoi);
            goc.Children.Add(nut);
            Content = goc;
        }

        /// <summary>
        /// Danh sách nhân viên: tích = hiện trong ô chọn người (giao đơn) và ô lọc Nhân viên; bỏ tích = ẩn (công ty khách, Administrator…).
        /// Người đã được giao trên đơn vẫn hiện tên ở bảng / chi tiết.
        /// </summary>
        private StackPanel KhoiNhanVien(Brush mau, List<string> danhBa)
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = "Danh sách nhân viên", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            if (danhBa.Count == 0)
            {
                p.Children.Add(new TextBlock { Text = "Mở một danh sách đơn trước để tải danh bạ công ty.", Foreground = KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap });
                return p;
            }
            p.Children.Add(new TextBlock { Text = "Bỏ tích người không phải nhân viên (công ty khách, Administrator…) để ẩn khỏi ô chọn người khi giao đơn và ô lọc Nhân viên.",
                                           TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            HashSet<string> an = CaiDatTeams.NhanVienAn;
            var ds = new StackPanel();
            foreach (string ten in danhBa.OrderBy(x => x, StringComparer.CurrentCulture))
            {
                var o = new CheckBox { Content = ten, IsChecked = !an.Contains(ten), Margin = new Thickness(0, 2, 0, 2) };
                _nhanVien.Add(o);
                ds.Children.Add(o);
            }
            var dem = new TextBlock { Foreground = KieuTeams.ChuPhu, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            void Dem() => dem.Text = "Hiện " + _nhanVien.Count(x => x.IsChecked == true) + " / " + _nhanVien.Count + " người";
            _nhanVien.ForEach(o => { o.Checked += (s, e) => Dem(); o.Unchecked += (s, e) => Dem(); });
            Dem();

            var loc = new TextBox { Width = 180, Padding = new Thickness(4, 2, 4, 2), ToolTip = "Gõ tên để lọc" };
            loc.TextChanged += (s, e) =>
            {
                string k = PhanTichTeams.BoDau(loc.Text);
                foreach (CheckBox o in _nhanVien)
                    o.Visibility = k.Length == 0 || PhanTichTeams.BoDau((string)o.Content).Contains(k) ? Visibility.Visible : Visibility.Collapsed;
            };
            var hang = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            hang.Children.Add(new TextBlock { Text = "🔍 ", VerticalAlignment = VerticalAlignment.Center });
            hang.Children.Add(loc);
            hang.Children.Add(KieuTeams.Nut("Hiện hết", mau, (s, e) => _nhanVien.Where(o => o.Visibility == Visibility.Visible).ToList().ForEach(o => o.IsChecked = true), chinh: false));
            hang.Children.Add(KieuTeams.Nut("Ẩn hết", mau, (s, e) => _nhanVien.Where(o => o.Visibility == Visibility.Visible).ToList().ForEach(o => o.IsChecked = false), chinh: false));
            hang.Children.Add(dem);
            p.Children.Add(hang);
            p.Children.Add(new Border
            {
                BorderBrush = KieuTeams.Mau("#D0D5DD"), BorderThickness = new Thickness(1), Padding = new Thickness(6, 2, 6, 2),
                Child = new ScrollViewer { Content = ds, MaxHeight = 240, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            });
            return p;
        }

        private void Luu()
        {
            try
            {
                CaiDatTeams.TuChuyenTiep = _tuChuyen.IsChecked == true;
                if (_nhanVien.Count > 0)
                    CaiDatTeams.NhanVienAn = new HashSet<string>(_nhanVien.Where(o => o.IsChecked != true).Select(o => (string)o.Content));
                string tm = _thuMuc.Text.Trim().Trim('"');
                if (tm.Length > 0 && !Directory.Exists(tm)) { _baoLoi.Text = "Thư mục đơn không tồn tại: " + tm; return; }
                CaiDatTeams.GhiMa(_tenant.Text, _client.Text);
                CaiDatTeams.ThuMucGoc = tm;
                DialogResult = true;
            }
            catch (Exception ex) { _baoLoi.Text = "Không lưu được: " + ex.Message; }
        }

        private void ChonThuMuc()
        {
            using (var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Chọn thư mục chứa các thư mục đơn", SelectedPath = _thuMuc.Text })
                if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) _thuMuc.Text = d.SelectedPath;
        }
    }
}
