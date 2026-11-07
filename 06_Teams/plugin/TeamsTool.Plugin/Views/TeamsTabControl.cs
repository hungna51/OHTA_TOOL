using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TeamsTool.Core;
using TeamsTool.Services;
#if ZWCAD
using CadApp = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace TeamsTool.Views
{
    /// <summary>
    /// Tab Teams: đăng nhập Microsoft 365, chọn kênh, liệt kê đơn (tin nhắn có file DWG/PDF/Excel),
    /// tải + mở file, trả lời "Đã nhận đơn" vào luồng. Dựng bằng code (không XAML) cho gọn.
    /// </summary>
    public sealed class TeamsTabControl : UserControl
    {
        private readonly ScrollViewer _cuon = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        private readonly TextBlock _trangThai = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 6) };
        private readonly TextBox _ma = new TextBox { IsReadOnly = true, FontSize = 22, FontWeight = FontWeights.Bold, Visibility = Visibility.Collapsed, HorizontalContentAlignment = HorizontalAlignment.Center };
        private readonly StackPanel _khuKenh = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 4, 0, 4) };
        private readonly StackPanel _dsDon = new StackPanel();
        private readonly CheckBox _chiTagToi = new CheckBox { Content = "Chỉ đơn tag tôi", Margin = new Thickness(0, 4, 0, 4) };
        private readonly TextBox _tenant = new TextBox(), _client = new TextBox(), _thuMuc = new TextBox();
        private readonly Brush _mau;

        private List<DonTeams> _don = new List<DonTeams>();
        private string _toiId, _toiTen;
        private CancellationTokenSource _huyDangNhap;
        private bool _ban;

        public TeamsTabControl(string tieuDe, string moTa, string mauNhan)
        {
            try { _mau = (Brush)new BrushConverter().ConvertFromString(mauNhan); }
            catch { _mau = Brushes.SteelBlue; }

            var goc = new StackPanel { Margin = new Thickness(8) };
            goc.Children.Add(new TextBlock { Text = tieuDe, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = _mau });
            goc.Children.Add(new TextBlock { Text = moTa, TextWrapping = TextWrapping.Wrap, Opacity = 0.7 });
            goc.Children.Add(_trangThai);
            goc.Children.Add(_ma);

            var nut = new WrapPanel();
            nut.Children.Add(Nut("Đăng nhập", (s, e) => DangNhap()));
            nut.Children.Add(Nut("Chọn kênh", (s, e) => Chay(MoChonKenh)));
            nut.Children.Add(Nut("⟳ Làm mới đơn", (s, e) => Chay(LamMoi)));
            goc.Children.Add(nut);
            _chiTagToi.Click += (s, e) => VeDon();
            goc.Children.Add(_chiTagToi);
            goc.Children.Add(TaoCaiDat());
            goc.Children.Add(_khuKenh);
            goc.Children.Add(_dsDon);

            _cuon.Content = goc;
            Content = _cuon;

            BaoTrangThai(CaiDatTeams.TenantId.Length == 0 ? "Mở ⚙ Cài đặt, nhập Tenant ID + Client ID do admin cấp."
                       : !GraphTeams.DaCoDangNhap ? "Bấm Đăng nhập để kết nối Microsoft 365."
                       : CaiDatTeams.Kenh().Count == 0 ? "Bấm Chọn kênh để chọn các kênh nhận đơn."
                       : "Bấm ⟳ Làm mới đơn.");
        }

        /// <summary>PaletteSet chuyển lăn chuột vào đây (DllTabLoader.CuonHo).</summary>
        public void ScrollByMouseWheel(int wheelDelta) => _cuon.ScrollToVerticalOffset(_cuon.VerticalOffset - wheelDelta / 3.0);

        private static Button Nut(string chu, RoutedEventHandler bam)
        {
            var b = new Button { Content = chu, Margin = new Thickness(0, 2, 6, 2), Padding = new Thickness(8, 3, 8, 3) };
            b.Click += bam;
            return b;
        }

        private void BaoTrangThai(string s) => _trangThai.Text = s;

        /// <summary>Chạy một việc mạng: chặn bấm chồng, lỗi hiện ở dòng trạng thái thay vì ném vào CAD.</summary>
        private async void Chay(Func<Task> viec)
        {
            if (_ban) return;
            _ban = true;
            try { await viec(); }
            catch (Exception ex) { BaoTrangThai("Lỗi: " + ex.Message); }
            finally { _ban = false; }
        }

        private Expander TaoCaiDat()
        {
            _tenant.Text = CaiDatTeams.TenantId;
            _client.Text = CaiDatTeams.ClientId;
            _thuMuc.Text = CaiDatTeams.ThuMucDon;
            var p = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            p.Children.Add(new TextBlock { Text = "Tenant ID (mã công ty)" });
            p.Children.Add(_tenant);
            p.Children.Add(new TextBlock { Text = "Client ID (mã ứng dụng OHTA TOOL)" });
            p.Children.Add(_client);
            p.Children.Add(new TextBlock { Text = "Thư mục lưu đơn tải về" });
            p.Children.Add(_thuMuc);
            var hang = new WrapPanel();
            hang.Children.Add(Nut("Lưu", (s, e) =>
            {
                CaiDatTeams.Ghi("tenant_id", _tenant.Text.Trim());
                CaiDatTeams.Ghi("client_id", _client.Text.Trim());
                CaiDatTeams.Ghi("thu_muc", _thuMuc.Text.Trim());
                BaoTrangThai("Đã lưu cài đặt.");
            }));
            hang.Children.Add(Nut("Đăng xuất", (s, e) =>
            {
                GraphTeams.DangXuat();
                _toiId = _toiTen = null;
                BaoTrangThai("Đã đăng xuất.");
            }));
            p.Children.Add(hang);
            return new Expander { Header = "⚙ Cài đặt", Content = p, Margin = new Thickness(0, 2, 0, 2) };
        }

        private async void DangNhap()
        {
            if (_huyDangNhap != null) { _huyDangNhap.Cancel(); return; } // bấm lần 2 = huỷ
            _huyDangNhap = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            try
            {
                var m = await GraphTeams.BatDauDangNhap(_huyDangNhap.Token);
                _ma.Text = m.MaNguoiDung;
                _ma.Visibility = Visibility.Visible;
                try { Clipboard.SetText(m.MaNguoiDung); } catch { }
                BaoTrangThai("Trình duyệt sẽ mở " + m.Url + " — dán mã bên dưới (đã chép sẵn), rồi đăng nhập tài khoản công ty. Bấm Đăng nhập lần nữa để huỷ.");
                MoNgoai(m.Url);
                await GraphTeams.ChoDangNhap(m.MaThietBi, m.ChuKy, _huyDangNhap.Token);
                (_toiId, _toiTen) = await GraphTeams.Toi(CancellationToken.None);
                BaoTrangThai("Đã đăng nhập: " + _toiTen + ". " + (CaiDatTeams.Kenh().Count == 0 ? "Bấm Chọn kênh." : "Bấm ⟳ Làm mới đơn."));
            }
            catch (OperationCanceledException) { BaoTrangThai("Đã huỷ đăng nhập."); }
            catch (Exception ex) { BaoTrangThai("Lỗi đăng nhập: " + ex.Message); }
            finally
            {
                _ma.Visibility = Visibility.Collapsed;
                _huyDangNhap.Dispose();
                _huyDangNhap = null;
            }
        }

        private async Task MoChonKenh()
        {
            if (_khuKenh.Visibility == Visibility.Visible) { _khuKenh.Visibility = Visibility.Collapsed; return; }
            BaoTrangThai("Đang tải danh sách nhóm và kênh…");
            var daChon = new HashSet<string>(CaiDatTeams.Kenh().Select(k => k[0] + "|" + k[1]));
            _khuKenh.Children.Clear();
            var hop = new List<CheckBox>();
            foreach (var nhom in await GraphTeams.Nhom(CancellationToken.None))
            {
                _khuKenh.Children.Add(new TextBlock { Text = nhom.Value, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 0) });
                foreach (var kenh in await GraphTeams.KenhCuaNhom(nhom.Key, CancellationToken.None))
                {
                    string khoa = nhom.Key + "|" + kenh.Key;
                    var cb = new CheckBox { Content = kenh.Value, IsChecked = daChon.Contains(khoa), Margin = new Thickness(12, 1, 0, 1),
                                            Tag = khoa + "|" + nhom.Value + " / " + kenh.Value };
                    hop.Add(cb);
                    _khuKenh.Children.Add(cb);
                }
            }
            _khuKenh.Children.Add(Nut("Lưu kênh đã chọn", (s, e) =>
            {
                CaiDatTeams.Ghi("kenh", string.Join("\n", hop.Where(c => c.IsChecked == true).Select(c => (string)c.Tag)));
                _khuKenh.Visibility = Visibility.Collapsed;
                BaoTrangThai("Đã lưu " + hop.Count(c => c.IsChecked == true) + " kênh. Bấm ⟳ Làm mới đơn.");
            }));
            _khuKenh.Visibility = Visibility.Visible;
            BaoTrangThai("Tích các kênh nhận đơn rồi bấm Lưu.");
        }

        private async Task LamMoi()
        {
            var kenh = CaiDatTeams.Kenh();
            if (kenh.Count == 0) { BaoTrangThai("Chưa chọn kênh nào — bấm Chọn kênh."); return; }
            if (_toiId == null) (_toiId, _toiTen) = await GraphTeams.Toi(CancellationToken.None);

            var moi = new List<DonTeams>();
            var loi = new List<string>();
            foreach (string[] k in kenh)
            {
                BaoTrangThai("Đang đọc " + k[2] + "…");
                try { moi.AddRange(await GraphTeams.DonCuaKenh(k[0], k[1], k[2], CancellationToken.None)); }
                catch (Exception ex) { loi.Add(k[2] + ": " + ex.Message); }
            }
            _don = moi.OrderByDescending(d => d.Luc).ToList();
            VeDon();
            BaoTrangThai(_don.Count + " đơn từ " + kenh.Count + " kênh (" + DateTime.Now.ToString("HH:mm") + ")." +
                         (loi.Count > 0 ? "\nLỗi: " + string.Join("\n", loi) : ""));
        }

        private void VeDon()
        {
            _dsDon.Children.Clear();
            foreach (DonTeams d in _don.Where(d => _chiTagToi.IsChecked != true || d.CoTag(_toiId)))
                _dsDon.Children.Add(TheDon(d));
        }

        private Border TheDon(DonTeams d)
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = d.NguoiGui + " · " + d.Luc.ToString("dd/MM HH:mm"), FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap });
            p.Children.Add(new TextBlock { Text = d.TenKenh, Opacity = 0.6, FontSize = 11, TextWrapping = TextWrapping.Wrap });
            if (d.TagTen.Count > 0)
                p.Children.Add(new TextBlock { Text = "Phụ trách: " + string.Join(", ", d.TagTen.Distinct()), Foreground = _mau, TextWrapping = TextWrapping.Wrap,
                                               FontWeight = d.CoTag(_toiId) ? FontWeights.Bold : FontWeights.Normal });
            if (d.NoiDung.Length > 0)
                p.Children.Add(new TextBlock { Text = d.NoiDung.Length > 300 ? d.NoiDung.Substring(0, 300) + "…" : d.NoiDung, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) });

            var files = new WrapPanel();
            foreach (FileDon f in d.Files)
                files.Children.Add(Nut("📄 " + f.Ten, (s, e) => Chay(() => TaiVaMo(d, f, mo: true))));
            p.Children.Add(files);

            var hang = new WrapPanel();
            hang.Children.Add(Nut("Tải hết", (s, e) => Chay(async () =>
            {
                foreach (FileDon f in d.Files) await TaiVaMo(d, f, mo: false);
                MoNgoai(PhanTichTeams.ThuMucDon(CaiDatTeams.ThuMucDon, d));
            })));
            Button nhan = null;
            nhan = Nut("✔ Nhận đơn", (s, e) => Chay(async () =>
            {
                await GraphTeams.TraLoi(d, "Đã nhận đơn – " + _toiTen + " (OHTA TOOL)", CancellationToken.None);
                nhan.IsEnabled = false;
                nhan.Content = "✔ Đã báo nhận";
                BaoTrangThai("Đã trả lời vào luồng tin nhắn của " + d.NguoiGui + ".");
            }));
            hang.Children.Add(nhan);
            p.Children.Add(hang);

            return new Border
            {
                Child = p,
                BorderBrush = _mau,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(6, 4, 4, 4),
                Margin = new Thickness(0, 4, 0, 4),
            };
        }

        private async Task TaiVaMo(DonTeams d, FileDon f, bool mo)
        {
            string dich = Path.Combine(PhanTichTeams.ThuMucDon(CaiDatTeams.ThuMucDon, d), PhanTichTeams.TenAnToan(f.Ten));
            if (!File.Exists(dich))
            {
                BaoTrangThai("Đang tải " + f.Ten + "…");
                await GraphTeams.TaiFile(f.Url, dich, CancellationToken.None);
            }
            BaoTrangThai("Đã tải: " + dich);
            if (!mo) return;

            string duoi = Path.GetExtension(dich).ToLowerInvariant();
            if (duoi == ".dwg" || duoi == ".dxf")
                CadApp.DocumentManager.Open(dich, false);
            else
                MoNgoai(dich);
        }

        private static void MoNgoai(string duongDan)
        {
            try { Process.Start(new ProcessStartInfo(duongDan) { UseShellExecute = true }); }
            catch { }
        }
    }
}
