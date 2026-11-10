using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TeamsTool.Core;
using TeamsTool.Services;

namespace TeamsTool.Views
{
    /// <summary>
    /// Tab Teams: header màu tab + nút ⚙ (cùng kiểu tab AI / Guro). Hai màn hình:
    /// chưa đăng nhập = chỉ nút Đăng nhập giữa palette; đã đăng nhập = Nhóm → Kênh → Tab (Lists) → Đơn → Chi tiết.
    /// Chỉ xem. Dựng bằng code (không XAML) cho gọn.
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
        // Màn hình chính: vẽ lại _noiDung ở mỗi bước điều hướng
        private readonly ScrollViewer _cuon = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        private readonly StackPanel _noiDung = new StackPanel { Margin = new Thickness(8) };
        private readonly TextBlock _trangThai = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = KieuTeams.ChuPhu, Margin = new Thickness(0, 6, 0, 0) };
        private readonly CheckBox _caDaXong = new CheckBox { Content = "Xem cả đơn đã xong", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0) };
        private readonly CheckBox _cuaToi = new CheckBox { Content = "Chỉ đơn của tôi", VerticalAlignment = VerticalAlignment.Center };

        /// <summary>Tab Lists đang mở: nhómId, tên nhóm, kênhId, tên kênh, tên tab, siteId, listId, URL list.</summary>
        private string[] _tab;
        private List<DonTeams> _don = new List<DonTeams>();
        private string _toiTen;
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

            // Màn hình chính
            _caDaXong.Click += (s, e) => VeDon();
            _cuaToi.Click += (s, e) => VeDon();
            _cuon.Content = _noiDung;

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
            if (GraphTeams.DaCoDangNhap) Chay(SauDangNhap); // còn phiên cũ: vào thẳng tab đã chọn, tự tải đơn một lần
        }

        /// <summary>PaletteSet chuyển lăn chuột vào đây (DllTabLoader.CuonHo).</summary>
        public void ScrollByMouseWheel(int wheelDelta) => _cuon.ScrollToVerticalOffset(_cuon.VerticalOffset - wheelDelta / 3.0);

        private void BaoTrangThai(string s) => _trangThai.Text = _trangThaiDN.Text = s;

        /// <summary>Đã đăng nhập → màn hình chính; chưa → nút Đăng nhập giữa palette (mờ nếu thiếu teams.json).</summary>
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
                ? "Đăng nhập tài khoản Microsoft 365 công ty để xem đơn ở các kênh Teams."
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
                _toiTen = null;
                _don.Clear();
                _noiDung.Children.Clear();
                BaoTrangThai("Đã đăng xuất.");
            }
            else if (luu)
            {
                if (_don.Count > 0) GhiDangLam(); // thư mục gốc có thể vừa đổi
                BaoTrangThai("Đã lưu cài đặt." + _loiGhi);
            }
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

        /// <summary>Lấy tên người đăng nhập; có tab đã chọn lần trước thì mở thẳng, không thì danh sách nhóm.</summary>
        private async Task SauDangNhap()
        {
            (_, _toiTen) = await GraphTeams.Toi(CancellationToken.None);
            CapNhatMan();
            _tab = CaiDatTeams.TabChon();
            if (_tab != null) await TaiDon();
            else await HienNhom();
        }

        // ---------- Điều hướng ----------

        /// <summary>Gỡ control dùng lại (_trangThai, 2 ô lọc) khỏi khung cũ — WPF không cho một control có 2 cha.</summary>
        private static void Tach(FrameworkElement e) => (e.Parent as Panel)?.Children.Remove(e);

        /// <summary>Đầu màn hình: nút ← Quay lại (nếu có) + đường dẫn hiện tại + dòng trạng thái.</summary>
        private void DauTrang(string duongDan, string nutLui, Action lui)
        {
            _oDanhSach = false;
            _noiDung.Children.Clear();
            Tach(_trangThai);
            Tach(_caDaXong);
            Tach(_cuaToi);
            var hang = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            if (lui != null)
            {
                var b = KieuTeams.Nut("← " + nutLui, _mau, (s, e) => lui(), chinh: false);
                DockPanel.SetDock(b, Dock.Left);
                hang.Children.Add(b);
            }
            hang.Children.Add(new TextBlock { Text = duongDan, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center,
                                              TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 0, 0, 0) });
            _noiDung.Children.Add(hang);
            _noiDung.Children.Add(_trangThai);
            _cuon.ScrollToTop();
        }

        /// <summary>Một dòng bấm được (nhóm / kênh / tab). bam = null: mờ, ghiChu bên phải.</summary>
        private Border DongChon(string chu, Action bam, string ghiChu = null)
        {
            var dp = new DockPanel();
            var phai = new TextBlock { Text = ghiChu ?? "›", Foreground = KieuTeams.ChuPhu, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            DockPanel.SetDock(phai, Dock.Right);
            dp.Children.Add(phai);
            dp.Children.Add(new TextBlock { Text = chu, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
            var the = KieuTeams.The(dp);
            the.Padding = new Thickness(10, 8, 10, 8);
            the.Margin = new Thickness(0, 0, 0, 4);
            if (bam == null) { the.Opacity = 0.5; return the; }
            the.Cursor = Cursors.Hand;
            the.MouseLeftButtonUp += (s, e) => bam();
            return the;
        }

        private async Task HienNhom()
        {
            DauTrang("Chọn nhóm", null, null);
            BaoTrangThai("Đang tải danh sách nhóm…");
            var ds = await GraphTeams.Nhom(CancellationToken.None);
            foreach (var n in ds.OrderBy(x => x.Value))
                _noiDung.Children.Add(DongChon(n.Value, () => Chay(() => HienKenh(n.Key, n.Value))));
            BaoTrangThai(ds.Count == 0 ? "Tài khoản này chưa tham gia nhóm Teams nào." : "");
        }

        private async Task HienKenh(string nhomId, string nhomTen)
        {
            DauTrang(nhomTen, "Nhóm", () => Chay(HienNhom));
            BaoTrangThai("Đang tải kênh…");
            var ds = await GraphTeams.KenhCuaNhom(nhomId, CancellationToken.None);
            foreach (var k in ds)
                _noiDung.Children.Add(DongChon(k.Value, () => Chay(() => HienTabKenh(nhomId, nhomTen, k.Key, k.Value))));
            BaoTrangThai("");
        }

        private async Task HienTabKenh(string nhomId, string nhomTen, string kenhId, string kenhTen)
        {
            DauTrang(nhomTen + " / " + kenhTen, "Kênh", () => Chay(() => HienKenh(nhomId, nhomTen)));
            BaoTrangThai("Đang tải tab…");
            var ds = await GraphTeams.TabCuaKenh(nhomId, kenhId, CancellationToken.None);
            foreach (TabTeams t in ds)
            {
                if (t.ListUrl == null) { _noiDung.Children.Add(DongChon(t.Ten, null, "chưa hỗ trợ")); continue; }
                _noiDung.Children.Add(DongChon(t.Ten, () => Chay(async () =>
                {
                    BaoTrangThai("Đang mở danh sách " + t.Ten + "…");
                    var (siteId, listId) = await GraphTeams.MoList(t.ListUrl, CancellationToken.None);
                    _tab = new[] { nhomId, nhomTen, kenhId, kenhTen, t.Ten, siteId, listId, t.ListUrl };
                    CaiDatTeams.GhiTabChon(_tab);
                    await TaiDon();
                })));
            }
            BaoTrangThai(ds.Count == 0 ? "Kênh này không có tab nào." : "");
        }

        // ---------- Danh sách đơn ----------

        /// <summary>
        /// Mở danh sách đơn: có bản lưu tạm thì hiện ngay rồi cập nhật ở nền (không giữ khoá _ban — vẫn bấm xem / quay lại được);
        /// chưa có thì chờ tải.
        /// </summary>
        private async Task TaiDon()
        {
            HienKhungDon();
            string listId = _tab[6];
            var tam = GraphTeams.DocTam(listId);
            if (tam != null)
            {
                var dsTam = await Task.Run(() => PhanTichTeams.DocToanBo(tam.Value.Tho));
                if (_tab?[6] != listId) return;
                DatDon(dsTam);
                VeDon();
                CapNhatNen(_tab[5], listId, tam.Value.Luc);
                return;
            }
            BaoTrangThai("Đang tải đơn…");
            var tho = await GraphTeams.TaiList(_tab[5], listId, CancellationToken.None);
            var ds = await Task.Run(() => PhanTichTeams.DocToanBo(tho));
            if (_tab?[6] != listId) return;
            DatDon(ds);
            VeDon();
        }

        private string _loiGhi = ""; // lỗi ghi dang_lam.json lần gần nhất, hiện kèm dòng trạng thái

        /// <summary>Gán danh sách đơn mới và ghi dang_lam.json cho tab CAD.</summary>
        private void DatDon(List<DonTeams> moi)
        {
            _don = moi;
            GhiDangLam();
        }

        private void GhiDangLam()
        {
            try { CaiDatTeams.GhiDangLam(_don); _loiGhi = ""; }
            catch (Exception ex) { _loiGhi = " — không ghi được đơn đang làm: " + ex.Message; }
        }

        private bool _dangCapNhat, _oDanhSach; // _oDanhSach: đang ở màn hình danh sách đơn

        /// <summary>Tải lại ở nền sau khi đã hiện bản lưu tạm. Chỉ vẽ lại nếu người dùng vẫn đang ở danh sách đó.</summary>
        private async void CapNhatNen(string siteId, string listId, DateTime? lucTam)
        {
            if (_dangCapNhat) return;
            _dangCapNhat = true;
            string goc = _trangThai.Text + (lucTam == null ? "" : " — bản lưu lúc " + lucTam.Value.ToString("dd/MM HH:mm"));
            BaoTrangThai(goc + " — đang cập nhật…");
            try
            {
                var tho = await GraphTeams.TaiList(siteId, listId, CancellationToken.None);
                var moi = await Task.Run(() => PhanTichTeams.DocToanBo(tho));
                if (_tab?[6] != listId) return; // đã chuyển sang tab khác
                DatDon(moi);
                if (_oDanhSach) VeDon();
            }
            catch (Exception ex) { if (_oDanhSach) BaoTrangThai(goc + ". Không cập nhật được: " + ex.Message); }
            finally
            {
                _dangCapNhat = false;
                if (!GraphTeams.DaCoDangNhap) CapNhatMan();
            }
        }

        /// <summary>Đầu màn hình đơn: ← Tab, ⟳ Làm mới, 2 ô lọc, dòng trạng thái.</summary>
        private void HienKhungDon()
        {
            string nhomId = _tab[0], nhomTen = _tab[1], kenhId = _tab[2], kenhTen = _tab[3];
            DauTrang(nhomTen + " / " + kenhTen + " / " + _tab[4], "Tab", () => Chay(() => HienTabKenh(nhomId, nhomTen, kenhId, kenhTen)));
            _noiDung.Children.Remove(_trangThai);
            var hang = new WrapPanel();
            hang.Children.Add(KieuTeams.Nut("⟳ Làm mới", _mau, (s, e) => CapNhatNen(_tab[5], _tab[6], null)));
            hang.Children.Add(_caDaXong);
            hang.Children.Add(_cuaToi);
            var dk = new StackPanel();
            dk.Children.Add(hang);
            dk.Children.Add(_trangThai);
            _noiDung.Children.Add(KieuTeams.The(dk));
        }

        /// <summary>Vẽ danh sách đơn từ _don (không tải lại) — dùng khi lọc và khi quay về từ chi tiết.</summary>
        private void VeDon()
        {
            if (_tab == null) return;
            HienKhungDon();
            var loc = _don.Where(d => (_caDaXong.IsChecked == true || !d.DaXong) && (_cuaToi.IsChecked != true || d.CuaToi(_toiTen))).ToList();
            // Gom theo Hạn hoàn thành, ngày gần nhất trên cùng; đơn không có hạn xuống cuối
            foreach (var nhom in loc.GroupBy(d => d.Han?.Date).OrderBy(g => g.Key ?? DateTime.MaxValue))
            {
                _noiDung.Children.Add(new TextBlock
                {
                    Text = (nhom.Key == null ? "Không có hạn" : "Hạn hoàn thành: " + nhom.Key.Value.ToString("dd/MM/yyyy")) + " (" + nhom.Count() + ")",
                    FontWeight = FontWeights.SemiBold, Foreground = _mau, Margin = new Thickness(2, 6, 0, 4),
                });
                foreach (DonTeams d in nhom.OrderBy(d => d.Title))
                    _noiDung.Children.Add(TheDon(d));
            }
            BaoTrangThai(loc.Count + " / " + _don.Count + " đơn (" + DateTime.Now.ToString("HH:mm") + ")" + _loiGhi);
            _oDanhSach = true;
        }

        private Border NhanTrangThai(DonTeams d)
        {
            string chu = d.TrangThai.Length > 0 ? d.TrangThai : d.StatusJp;
            if (chu.Length == 0) return null;
            return new Border
            {
                Background = KieuTeams.Mau(d.DaXong ? "#E3F4EA" : "#EEF0FA"),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(8, 1, 8, 1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock { Text = chu, FontSize = 11, Foreground = d.DaXong ? KieuTeams.Mau("#1F7A4D") : _mau },
            };
        }

        private Border TheDon(DonTeams d)
        {
            var p = new StackPanel();
            var dau = new DockPanel();
            Border nhan = NhanTrangThai(d);
            if (nhan != null) { nhan.Margin = new Thickness(6, 0, 0, 0); DockPanel.SetDock(nhan, Dock.Right); dau.Children.Add(nhan); }
            dau.Children.Add(new TextBlock { Text = (d.Id == CaiDatTeams.DonChon ? "📌 " : "") + (d.Title.Length > 0 ? d.Title : "(không tên)"), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            p.Children.Add(dau);
            if (d.TrienKhai.Length > 0) p.Children.Add(new TextBlock { Text = "Triển khai: " + d.TrienKhai, Foreground = KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
            if (d.Check.Length > 0) p.Children.Add(new TextBlock { Text = "Check: " + d.Check, Foreground = KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap });

            var the = KieuTeams.The(p);
            the.Padding = new Thickness(10, 7, 10, 7);
            the.Margin = new Thickness(0, 0, 0, 4);
            the.Cursor = Cursors.Hand;
            if (d.CuaToi(_toiTen)) { the.BorderBrush = _mau; the.BorderThickness = new Thickness(3, 1, 1, 1); } // đơn của tôi nổi bật
            the.MouseLeftButtonUp += (s, e) => HienChiTiet(d);
            return the;
        }

        private void HienChiTiet(DonTeams d)
        {
            DauTrang(d.Title, "Danh sách đơn", VeDon);
            _noiDung.Children.Remove(_trangThai);
            Border nhan = NhanTrangThai(d);
            if (nhan != null) { nhan.Margin = new Thickness(0, 0, 0, 6); _noiDung.Children.Add(nhan); }

            var luoi = new Grid();
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.42, GridUnitType.Star) });
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.58, GridUnitType.Star) });
            int r = 0;
            foreach (var kv in d.TatCa)
            {
                luoi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var ten = new TextBlock { Text = kv.Key, Foreground = KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 8, 3) };
                var gt = new TextBox { Text = kv.Value, IsReadOnly = true, BorderThickness = new Thickness(0), Background = Brushes.Transparent,
                                       TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 3) }; // TextBox để chọn + chép được
                Grid.SetRow(ten, r);
                Grid.SetRow(gt, r);
                Grid.SetColumn(gt, 1);
                luoi.Children.Add(ten);
                luoi.Children.Add(gt);
                r++;
            }
            _noiDung.Children.Add(KieuTeams.The(luoi));
            var nut = new WrapPanel();
            bool dangChon = d.Id == CaiDatTeams.DonChon;
            nut.Children.Add(KieuTeams.Nut(dangChon ? "📌 Bỏ đang làm" : "📌 Đang làm", _mau, (s, e) =>
            {
                CaiDatTeams.DonChon = dangChon ? "" : d.Id;
                GhiDangLam();
                if (_loiGhi.Length > 0) MessageBox.Show(_loiGhi.TrimStart(' ', '—'), "Teams", MessageBoxButton.OK, MessageBoxImage.Warning);
                HienChiTiet(d);
            }, chinh: !dangChon));
            if (!string.IsNullOrEmpty(d.WebUrl)) nut.Children.Add(KieuTeams.Nut("Mở trên Teams", _mau, (s, e) => MoNgoai(d.WebUrl), chinh: dangChon));
            _noiDung.Children.Add(nut);
        }

        private static void MoNgoai(string duongDan)
        {
            try { Process.Start(new ProcessStartInfo(duongDan) { UseShellExecute = true }); }
            catch { }
        }
    }
}
