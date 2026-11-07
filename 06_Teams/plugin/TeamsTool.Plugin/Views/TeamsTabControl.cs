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
    /// Tab Teams: header màu tab + nút ⚙ (cùng kiểu tab AI / Guro). Hai màn hình:
    /// chưa đăng nhập = chỉ nút Đăng nhập giữa palette; đã đăng nhập = Chọn kênh, ⟳ Làm mới, thẻ đơn.
    /// Dựng bằng code (không XAML) cho gọn.
    /// </summary>
    public sealed class TeamsTabControl : UserControl
    {
        private readonly Brush _mau;

        // Header
        private readonly TextBlock _tenToiHeader = new TextBlock { Foreground = Brushes.White, FontSize = 10.5, VerticalAlignment = VerticalAlignment.Center,
                                                                   TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 140 };
        // Màn hình đăng nhập
        private readonly StackPanel _manDangNhap = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
                                                                    Margin = new Thickness(16), MaxWidth = 320 };
        private readonly TextBlock _hdDangNhap = new TextBlock { TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
        private readonly Button _nutDangNhap;
        private readonly StackPanel _khuMa = new StackPanel { Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Center };
        private readonly TextBox _ma = new TextBox { IsReadOnly = true, FontSize = 24, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0),
                                                     Background = Brushes.Transparent, HorizontalContentAlignment = HorizontalAlignment.Center };
        private readonly TextBlock _trangThaiDN = new TextBlock { TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Foreground = KieuTeams.ChuPhu, Margin = new Thickness(0, 8, 0, 0) };
        // Màn hình đơn
        private readonly ScrollViewer _cuon = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        private readonly TextBlock _trangThai = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = KieuTeams.ChuPhu, Margin = new Thickness(0, 6, 0, 0) };
        private readonly Border _theKenh;
        private readonly StackPanel _dsKenh = new StackPanel();
        private readonly StackPanel _dsDon = new StackPanel();
        private readonly CheckBox _chiTagToi = new CheckBox { Content = "Chỉ đơn tag tôi", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };

        private List<DonTeams> _don = new List<DonTeams>();
        private string _toiId, _toiTen;
        private CancellationTokenSource _huyDangNhap;
        private bool _ban;

        public TeamsTabControl(string tieuDe, string moTa, string mauNhan)
        {
            try { _mau = (Brush)new BrushConverter().ConvertFromString(mauNhan); }
            catch { _mau = Brushes.SteelBlue; }
            Background = KieuTeams.Nen;
            FontFamily = KieuTeams.Font;
            FontSize = 12;

            // Header
            var tieuDeKhoi = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            tieuDeKhoi.Children.Add(new TextBlock { Text = tieuDe, Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            tieuDeKhoi.Children.Add(new TextBlock { Text = moTa, Foreground = KieuTeams.Mau("#DDE3F5"), FontSize = 10.5, Margin = new Thickness(0, 1, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });
            var header = new DockPanel { LastChildFill = true };
            var gear = KieuTeams.NutCaiDat(_mau, (s, e) => MoCaiDat());
            DockPanel.SetDock(gear, Dock.Right);
            DockPanel.SetDock(_tenToiHeader, Dock.Right);
            header.Children.Add(gear);
            header.Children.Add(_tenToiHeader);
            header.Children.Add(tieuDeKhoi);

            // Màn hình đăng nhập
            _nutDangNhap = KieuTeams.Nut("Đăng nhập Microsoft 365", _mau, (s, e) => DangNhap());
            _nutDangNhap.FontSize = 14;
            _nutDangNhap.Padding = new Thickness(22, 10, 22, 10);
            _nutDangNhap.HorizontalAlignment = HorizontalAlignment.Center;
            _khuMa.Children.Add(new TextBlock { Text = "Mã đăng nhập (đã chép sẵn — dán vào trang Microsoft):", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
            _khuMa.Children.Add(_ma);
            var huy = KieuTeams.Nut("Huỷ", _mau, (s, e) => _huyDangNhap?.Cancel(), chinh: false);
            huy.HorizontalAlignment = HorizontalAlignment.Center;
            _khuMa.Children.Add(huy);
            _manDangNhap.Children.Add(_hdDangNhap);
            _manDangNhap.Children.Add(_nutDangNhap);
            _manDangNhap.Children.Add(_khuMa);
            _manDangNhap.Children.Add(_trangThaiDN);

            // Màn hình đơn
            var hangNut = new WrapPanel();
            hangNut.Children.Add(KieuTeams.Nut("Chọn kênh", _mau, (s, e) => Chay(MoChonKenh)));
            hangNut.Children.Add(KieuTeams.Nut("⟳ Làm mới đơn", _mau, (s, e) => Chay(LamMoi)));
            hangNut.Children.Add(_chiTagToi);
            _chiTagToi.Click += (s, e) => VeDon();
            var dieuKhien = new StackPanel();
            dieuKhien.Children.Add(hangNut);
            dieuKhien.Children.Add(_trangThai);
            _theKenh = KieuTeams.The(_dsKenh);
            _theKenh.Visibility = Visibility.Collapsed;
            var noiDungDon = new StackPanel { Margin = new Thickness(8) };
            noiDungDon.Children.Add(KieuTeams.The(dieuKhien));
            noiDungDon.Children.Add(_theKenh);
            noiDungDon.Children.Add(_dsDon);
            _cuon.Content = noiDungDon;

            var than = new Grid();
            than.Children.Add(_manDangNhap);
            than.Children.Add(_cuon);

            var goc = new Grid();
            goc.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            goc.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var hdBorder = new Border { Background = _mau, Padding = new Thickness(12, 10, 12, 10), Child = header };
            Grid.SetRow(than, 1);
            goc.Children.Add(hdBorder);
            goc.Children.Add(than);
            Content = goc;

            CapNhatMan();
            if (GraphTeams.DaCoDangNhap) Chay(SauDangNhap); // còn phiên cũ: vào thẳng màn hình đơn, tự tải một lần
        }

        /// <summary>PaletteSet chuyển lăn chuột vào đây (DllTabLoader.CuonHo).</summary>
        public void ScrollByMouseWheel(int wheelDelta) => _cuon.ScrollToVerticalOffset(_cuon.VerticalOffset - wheelDelta / 3.0);

        private void BaoTrangThai(string s) => _trangThai.Text = _trangThaiDN.Text = s;

        /// <summary>Đã đăng nhập → màn hình đơn; chưa → nút Đăng nhập giữa palette (mờ nếu thiếu teams.json).</summary>
        private void CapNhatMan()
        {
            bool daVao = GraphTeams.DaCoDangNhap;
            _cuon.Visibility = daVao ? Visibility.Visible : Visibility.Collapsed;
            _manDangNhap.Visibility = daVao ? Visibility.Collapsed : Visibility.Visible;
            _tenToiHeader.Text = daVao && _toiTen != null ? _toiTen : "";
            if (daVao) return;

            bool coMa = CaiDatTeams.TenantId.Length > 0 && CaiDatTeams.ClientId.Length > 0;
            _nutDangNhap.IsEnabled = coMa;
            _hdDangNhap.Text = coMa
                ? "Đăng nhập tài khoản Microsoft 365 công ty để đọc đơn ở các kênh Teams."
                : "Chưa có file cài đặt Teams. Chép file teams.json admin gửi vào\n" + Path.GetDirectoryName(CaiDatTeams.FileMa) + "\nhoặc bấm ⚙ để nhập.";
        }

        /// <summary>Chạy một việc mạng: chặn bấm chồng, lỗi hiện ở dòng trạng thái thay vì ném vào CAD.</summary>
        private async void Chay(Func<Task> viec)
        {
            if (_ban) return;
            _ban = true;
            try { await viec(); }
            catch (Exception ex) { BaoTrangThai("Lỗi: " + ex.Message); }
            finally
            {
                _ban = false;
                if (!GraphTeams.DaCoDangNhap) CapNhatMan(); // phiên hết hạn (refresh hỏng) → về màn hình đăng nhập
            }
        }

        private void MoCaiDat()
        {
            var w = new TeamsCaiDatWindow(_mau);
            bool luu = w.ShowDialog() == true;
            if (w.DaDangXuat)
            {
                _huyDangNhap?.Cancel();
                _toiId = _toiTen = null;
                _don.Clear();
                VeDon();
                BaoTrangThai("Đã đăng xuất.");
            }
            else if (luu) BaoTrangThai("Đã lưu cài đặt.");
            CapNhatMan();
        }

        private async void DangNhap()
        {
            if (_huyDangNhap != null) return;
            _huyDangNhap = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            _nutDangNhap.Visibility = Visibility.Collapsed;
            BaoTrangThai("Đang lấy mã đăng nhập…");
            bool xong = false;
            try
            {
                var m = await GraphTeams.BatDauDangNhap(_huyDangNhap.Token);
                _ma.Text = m.MaNguoiDung;
                _khuMa.Visibility = Visibility.Visible;
                try { Clipboard.SetText(m.MaNguoiDung); } catch { }
                BaoTrangThai("Trình duyệt đã mở " + m.Url + " — dán mã, rồi đăng nhập tài khoản công ty. Mã dùng được 15 phút.");
                MoNgoai(m.Url);
                await GraphTeams.ChoDangNhap(m.MaThietBi, m.ChuKy, _huyDangNhap.Token);
                xong = true;
            }
            catch (OperationCanceledException) { BaoTrangThai("Đã huỷ đăng nhập."); }
            catch (Exception ex) { BaoTrangThai("Lỗi đăng nhập: " + ex.Message); }
            finally
            {
                _khuMa.Visibility = Visibility.Collapsed;
                _nutDangNhap.Visibility = Visibility.Visible;
                _huyDangNhap.Dispose();
                _huyDangNhap = null;
            }
            if (xong) Chay(SauDangNhap);
        }

        /// <summary>Lấy tên người đăng nhập, chuyển màn hình đơn, có kênh thì tải đơn luôn.</summary>
        private async Task SauDangNhap()
        {
            (_toiId, _toiTen) = await GraphTeams.Toi(CancellationToken.None);
            CapNhatMan();
            if (CaiDatTeams.Kenh().Count == 0) BaoTrangThai("Đã đăng nhập: " + _toiTen + ". Bấm Chọn kênh để chọn các kênh nhận đơn.");
            else await LamMoi();
        }

        private async Task MoChonKenh()
        {
            if (_theKenh.Visibility == Visibility.Visible) { _theKenh.Visibility = Visibility.Collapsed; return; }
            BaoTrangThai("Đang tải danh sách nhóm và kênh…");
            var daChon = new HashSet<string>(CaiDatTeams.Kenh().Select(k => k[0] + "|" + k[1]));
            _dsKenh.Children.Clear();
            _dsKenh.Children.Add(new TextBlock { Text = "Kênh nhận đơn", FontWeight = FontWeights.SemiBold });
            var hop = new List<CheckBox>();
            foreach (var nhom in await GraphTeams.Nhom(CancellationToken.None))
            {
                _dsKenh.Children.Add(new TextBlock { Text = nhom.Value, Foreground = _mau, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
                foreach (var kenh in await GraphTeams.KenhCuaNhom(nhom.Key, CancellationToken.None))
                {
                    string khoa = nhom.Key + "|" + kenh.Key;
                    var cb = new CheckBox { Content = kenh.Value, IsChecked = daChon.Contains(khoa), Margin = new Thickness(12, 2, 0, 2),
                                            Tag = khoa + "|" + nhom.Value + " / " + kenh.Value };
                    hop.Add(cb);
                    _dsKenh.Children.Add(cb);
                }
            }
            var luu = KieuTeams.Nut("Lưu kênh đã chọn", _mau, (s, e) =>
            {
                CaiDatTeams.Ghi("kenh", string.Join("\n", hop.Where(c => c.IsChecked == true).Select(c => (string)c.Tag)));
                _theKenh.Visibility = Visibility.Collapsed;
                BaoTrangThai("Đã lưu " + hop.Count(c => c.IsChecked == true) + " kênh.");
                Chay(LamMoi);
            });
            luu.Margin = new Thickness(0, 8, 0, 0);
            luu.HorizontalAlignment = HorizontalAlignment.Left;
            _dsKenh.Children.Add(luu);
            _theKenh.Visibility = Visibility.Visible;
            BaoTrangThai("Tích các kênh nhận đơn rồi bấm Lưu.");
        }

        private async Task LamMoi()
        {
            var kenh = CaiDatTeams.Kenh();
            if (kenh.Count == 0) { BaoTrangThai("Chưa chọn kênh nào — bấm Chọn kênh."); return; }
            if (_toiId == null) { (_toiId, _toiTen) = await GraphTeams.Toi(CancellationToken.None); CapNhatMan(); }

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
            p.Children.Add(new TextBlock { Text = d.NguoiGui + " · " + d.Luc.ToString("dd/MM HH:mm"), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            p.Children.Add(new TextBlock { Text = d.TenKenh, Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap });
            if (d.TagTen.Count > 0)
                p.Children.Add(new TextBlock { Text = "Phụ trách: " + string.Join(", ", d.TagTen.Distinct()), Foreground = _mau, TextWrapping = TextWrapping.Wrap,
                                               Margin = new Thickness(0, 2, 0, 0), FontWeight = d.CoTag(_toiId) ? FontWeights.Bold : FontWeights.Normal });
            if (d.NoiDung.Length > 0)
                p.Children.Add(new TextBlock { Text = d.NoiDung.Length > 300 ? d.NoiDung.Substring(0, 300) + "…" : d.NoiDung, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 4) });

            var files = new WrapPanel();
            foreach (FileDon f in d.Files)
                files.Children.Add(KieuTeams.Nut("📄 " + f.Ten, _mau, (s, e) => Chay(() => TaiVaMo(d, f, mo: true)), chinh: false));
            p.Children.Add(files);

            var hang = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            Button nhan = null;
            nhan = KieuTeams.Nut("✔ Nhận đơn", _mau, (s, e) => Chay(async () =>
            {
                await GraphTeams.TraLoi(d, "Đã nhận đơn – " + _toiTen + " (OHTA TOOL)", CancellationToken.None);
                nhan.IsEnabled = false;
                nhan.Content = "✔ Đã báo nhận";
                BaoTrangThai("Đã trả lời vào luồng tin nhắn của " + d.NguoiGui + ".");
            }));
            hang.Children.Add(nhan);
            hang.Children.Add(KieuTeams.Nut("Tải hết", _mau, (s, e) => Chay(async () =>
            {
                foreach (FileDon f in d.Files) await TaiVaMo(d, f, mo: false);
                MoNgoai(PhanTichTeams.ThuMucDon(CaiDatTeams.ThuMucDon, d));
            }), chinh: false));
            p.Children.Add(hang);

            var the = KieuTeams.The(p);
            if (d.CoTag(_toiId)) { the.BorderBrush = _mau; the.BorderThickness = new Thickness(3, 1, 1, 1); } // đơn tag tôi nổi bật
            return the;
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
