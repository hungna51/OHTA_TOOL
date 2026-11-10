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
        private readonly TextBox _tim = new TextBox { Width = 220, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center,
                                                     Padding = new Thickness(4, 2, 4, 2), ToolTip = "Tìm đơn: số đơn, tên, ghi chú… (mọi cột, không dấu). Đang tìm thì tìm cả đơn đã xong." };
        private StackPanel _timKhungTam;
        private StackPanel _timKhung => _timKhungTam ?? (_timKhungTam = TaoKhungTim());
        private StackPanel TaoKhungTim()
        {
            var k = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            k.Children.Add(new TextBlock { Text = "  🔍", VerticalAlignment = VerticalAlignment.Center });
            k.Children.Add(_tim);
            return k;
        }
        private readonly System.Windows.Threading.DispatcherTimer _henTim = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) }; // gõ xong mới lọc
        private readonly ComboBox _nhanVien = new ComboBox { MinWidth = 150, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Chỉ hiện đơn có nhân viên này ở cột người bất kỳ" };
        private const string MoiNhanVien = "(Mọi nhân viên)";
        private List<KeyValuePair<string, string>> _danhBa = new List<KeyValuePair<string, string>>(); // LookupId → tên, xếp theo tên

        /// <summary>Tab Lists đang mở: nhómId, tên nhóm, kênhId, tên kênh, tên tab, siteId, listId, URL list.</summary>
        private string[] _tab;
        private List<DonTeams> _don = new List<DonTeams>();
        private List<CotList> _cots = new List<CotList>(); // cột của danh sách đang mở (form sửa)
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
            _nhanVien.SelectionChanged += (s, e) => { if (_oDanhSach) VeDon(); };
            _tim.TextChanged += (s, e) => { _henTim.Stop(); _henTim.Start(); };
            _tim.KeyDown += (s, e) => { if (e.Key == Key.Escape) _tim.Clear(); };
            _henTim.Tick += (s, e) =>
            {
                _henTim.Stop();
                if (!_oDanhSach) return;
                int caret = _tim.CaretIndex;
                VeDon();
                _tim.Focus(); // vẽ lại gỡ ô tìm khỏi cây giao diện → trả lại con trỏ để gõ tiếp
                _tim.CaretIndex = caret;
            };
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
            var w = new TeamsCaiDatWindow(_mau, _danhBaDu.Select(kv => kv.Value).Distinct().ToList());
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
                GopDanhBa();                       // danh sách nhân viên ẩn / hiện có thể vừa đổi
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
            _cuon.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled; // chi tiết / sửa: chữ tự xuống dòng
            Tach(_trangThai);
            Tach(_caDaXong);
            Tach(_cuaToi);
            Tach(_nhanVien);
            Tach(_timKhung);
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
                    _chon.Clear();
                    _congTy = _nguoiSite = null; // site khác: mã người khác
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
                DatDon(tam.Value.Tho, dsTam);
                VeDon();
                CapNhatNen(_tab[5], listId, tam.Value.Luc);
                return;
            }
            BaoTrangThai("Đang tải đơn…");
            var tho = await GraphTeams.TaiList(_tab[5], listId, CancellationToken.None);
            var ds = await Task.Run(() => PhanTichTeams.DocToanBo(tho));
            if (_tab?[6] != listId) return;
            DatDon(tho, ds);
            VeDon();
        }

        private string _loiGhi = ""; // lỗi ghi dang_lam.json lần gần nhất, hiện kèm dòng trạng thái

        /// <summary>Gán danh sách đơn mới và ghi dang_lam.json cho tab CAD.</summary>
        private void DatDon(PhanTichTeams.ThoList tho, List<DonTeams> moi)
        {
            _don = moi;
            _cots = PhanTichTeams.DocCot(tho.Cot);
            _danhBaDon = SuaDon.DanhBa(moi, _cots);
            GopDanhBa();
            if (_congTy == null) TaiDanhBaCongTy();
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
                DatDon(tho, moi);
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
            hang.Children.Add(_nhanVien);
            hang.Children.Add(_timKhung);
            hang.Children.Add(KieuTeams.Nut("☰ Cột", _mau, (s2, e2) => ChonCot(), chinh: false));
            var dk = new StackPanel();
            dk.Children.Add(hang);
            dk.Children.Add(_trangThai);
            _noiDung.Children.Add(KieuTeams.The(dk));
        }

        /// <summary>Vẽ danh sách đơn từ _don (không tải lại) — dùng khi lọc và khi quay về từ chi tiết. Dạng bảng như view Lists trên Teams.</summary>
        private void VeDon()
        {
            if (_tab == null) return;
            HienKhungDon();
            _cuon.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto; // bảng rộng: kéo ngang
            string tenNv = _nhanVien.SelectedItem as string;
            string idNv = tenNv == null || tenNv == MoiNhanVien ? null : _danhBa.FirstOrDefault(kv => kv.Value == tenNv).Key;
            bool dangTim = _tim.Text.Trim().Length > 0; // đang tìm: tìm cả đơn đã xong (tìm đơn cũ theo số)
            var loc = _don.Where(d => (dangTim || _caDaXong.IsChecked == true || !d.DaXong) && (_cuaToi.IsChecked != true || d.CuaToi(_toiTen))
                                      && BangDon.KhopTim(d, _tim.Text)
                                      && (idNv == null || SuaDon.CoNguoi(d, _cots, idNv))).ToList();
            _noiDung.Children.Add(VeBang(loc));
            CapNhatThanhNhanh();
            BaoTrangThai(loc.Count + " / " + _don.Count + " đơn (" + DateTime.Now.ToString("HH:mm") + ")" + (dangTim ? " — kết quả tìm, gồm cả đơn đã xong (Esc để xoá)" : "") + _loiGhi);
            _oDanhSach = true;
        }

        // ---------- Danh bạ nhân viên đầy đủ ----------

        private List<KeyValuePair<string, string>> _danhBaDon = new List<KeyValuePair<string, string>>(); // người đã có trong các đơn
        private List<KeyValuePair<string, string>> _danhBaDu = new List<KeyValuePair<string, string>>();  // cả người bị ẩn: tra tên người đã giao
        private List<KeyValuePair<string, string>> _congTy, _nguoiSite; // (tên, email) công ty / (mã, email) site — null = chưa tải
        private bool _dangTaiDanhBa;

        /// <summary>_danhBa = người trong đơn + cả công ty (khi đã tải); nạp lại ô Nhân viên, giữ lựa chọn.</summary>
        private void GopDanhBa()
        {
            _danhBaDu = _congTy == null ? _danhBaDon : SuaDon.GopDanhBa(_danhBaDon, _congTy, _nguoiSite);
            HashSet<string> an = CaiDatTeams.NhanVienAn; // ⚙ → Danh sách nhân viên: ẩn công ty khách, Administrator…
            _danhBa = _danhBaDu.Where(kv => !an.Contains(kv.Value)).ToList();
            string dangChon = _nhanVien.SelectedItem as string;
            _nhanVien.Items.Clear();
            _nhanVien.Items.Add(MoiNhanVien);
            foreach (var kv in _danhBa) _nhanVien.Items.Add(kv.Value);
            _nhanVien.SelectedItem = dangChon != null && _nhanVien.Items.Contains(dangChon) ? dangChon : MoiNhanVien;
        }

        /// <summary>Tải danh bạ công ty (1 lần mỗi phiên) để giao đơn được cho cả người chưa từng có trong đơn. Lỗi → giữ danh bạ từ đơn.</summary>
        private async void TaiDanhBaCongTy()
        {
            if (_dangTaiDanhBa || _tab == null) return;
            _dangTaiDanhBa = true;
            try
            {
                var (congTy, site) = await GraphTeams.NguoiCongTyVaSite(_tab[7], CancellationToken.None);
                _congTy = congTy;
                _nguoiSite = site;
                GopDanhBa();
            }
            catch (Exception ex) { _loiGhi += " — chưa tải được danh bạ công ty: " + ex.Message.Split('\n')[0]; }
            finally { _dangTaiDanhBa = false; }
        }

        /// <summary>
        /// Người chọn từ danh bạ công ty mà chưa có mã trên site ("upn:…") → xin mã (ensureuser) trước khi ghi đơn;
        /// nhớ mã để lần sau không xin lại.
        /// </summary>
        private async Task<List<KeyValuePair<CotList, object>>> GiaiMaNguoi(List<KeyValuePair<CotList, object>> doi)
        {
            var kq = new List<KeyValuePair<CotList, object>>();
            foreach (var kv in doi)
            {
                if (kv.Key.Loai != "personOrGroup" || !(kv.Value is List<string> ids) || !ids.Any(x => x.StartsWith(SuaDon.ChuaCoMa, StringComparison.Ordinal)))
                { kq.Add(kv); continue; }
                var moi = new List<string>();
                foreach (string id in ids)
                {
                    if (!id.StartsWith(SuaDon.ChuaCoMa, StringComparison.Ordinal)) { moi.Add(id); continue; }
                    string email = id.Substring(SuaDon.ChuaCoMa.Length);
                    BaoTrangThai("Đang thêm " + TenNguoi(id) + " vào danh sách người của site…");
                    string ma = await GraphTeams.MaNguoi(_tab[7], email, CancellationToken.None);
                    _nguoiSite?.Add(new KeyValuePair<string, string>(ma, email));
                    moi.Add(ma);
                }
                kq.Add(new KeyValuePair<CotList, object>(kv.Key, moi));
            }
            if (_congTy != null) GopDanhBa();
            return kq;
        }

        // ---------- Sửa nhanh nhiều đơn ----------

        private readonly HashSet<string> _chon = new HashSet<string>();                       // id đơn đang tích (giữ qua lọc / vẽ lại)
        private readonly Dictionary<string, CheckBox> _oChon = new Dictionary<string, CheckBox>(); // ô tích của các dòng đang vẽ
        private readonly Border _thanhNhanh = new Border { Margin = new Thickness(0, 0, 0, 6), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 8, 10, 8),
                                                            Background = KieuTeams.Mau("#EEF0FA"), Visibility = Visibility.Collapsed };
        private string _cotNhanh;                 // cột đang chọn ở thanh sửa nhanh
        private Func<object> _docNhanh;           // đọc giá trị ô nhập của thanh
        private bool _dongBoChon;                 // đang tích / bỏ tích hàng loạt: chưa vẽ lại thanh

        /// <summary>Thanh "Đã chọn N đơn": chọn cột + giá trị → Áp dụng cho mọi đơn đã tích. Vẽ lại khi số đơn chọn đổi (không vẽ lại bảng).</summary>
        private void CapNhatThanhNhanh()
        {
            Tach(_thanhNhanh);
            if (_chon.Count == 0) { _thanhNhanh.Visibility = Visibility.Collapsed; return; }
            _thanhNhanh.Visibility = Visibility.Visible;
            var cotSua = _cots.Where(c => c.SuaDuoc && c.Ten != "Title").ToList();
            if (_cotNhanh == null || cotSua.All(c => c.Ten != _cotNhanh))
                _cotNhanh = (cotSua.FirstOrDefault(c => PhanTichTeams.BoDau(c.TieuDe).Contains("han hoan thanh")) ?? cotSua.FirstOrDefault())?.Ten;

            var hang = new WrapPanel();
            hang.Children.Add(new TextBlock { Text = "Đã chọn " + _chon.Count + " đơn — gán", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            var cb = new ComboBox { MinWidth = 180, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            foreach (CotList c in cotSua) cb.Items.Add(new ComboBoxItem { Content = c.TieuDe, Tag = c.Ten });
            cb.SelectedItem = cb.Items.Cast<ComboBoxItem>().FirstOrDefault(x => (string)x.Tag == _cotNhanh);
            hang.Children.Add(cb);
            hang.Children.Add(new TextBlock { Text = "=", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            CotList cot = cotSua.FirstOrDefault(c => c.Ten == _cotNhanh);
            if (cot != null)
            {
                var (o, doc) = TaoONhap(cot, SuaDon.GiaTriForm(cot, null));
                o.MinWidth = 200;
                o.VerticalAlignment = VerticalAlignment.Center;
                o.Margin = new Thickness(0, 0, 8, 0);
                _docNhanh = doc;
                hang.Children.Add(o);
            }
            hang.Children.Add(KieuTeams.Nut("Áp dụng", _mau, (s, e) => ApDungNhanh(cot)));
            hang.Children.Add(KieuTeams.Nut("Bỏ chọn", _mau, (s, e) =>
            {
                _dongBoChon = true;
                _chon.Clear();
                foreach (CheckBox x in _oChon.Values) x.IsChecked = false;
                _dongBoChon = false;
                CapNhatThanhNhanh();
            }, chinh: false));
            cb.SelectionChanged += (s, e) => { _cotNhanh = (cb.SelectedItem as ComboBoxItem)?.Tag as string; CapNhatThanhNhanh(); };
            _thanhNhanh.Child = hang;
            _noiDung.Children.Insert(Math.Min(2, _noiDung.Children.Count), _thanhNhanh); // ngay dưới đường dẫn + khung ⟳ / lọc, trên bảng
        }

        /// <summary>Gán cùng một giá trị cho cột của mọi đơn đã tích: xác nhận 1 lần, gửi từng đơn (kèm eTag), báo kết quả, tải lại danh sách.</summary>
        private void ApDungNhanh(CotList cot)
        {
            if (cot == null || _docNhanh == null) return;
            object moi;
            try { moi = _docNhanh(); }
            catch (FormatException ex) { BaoTrangThai(ex.Message); return; }
            string loi = SuaDon.KiemTra(cot, moi);
            if (loi != null) { BaoTrangThai(loi); return; }
            var don = _don.Where(d => _chon.Contains(d.Id)).ToList();
            if (don.Count == 0) return;
            if (MessageBox.Show("Gán " + cot.TieuDe + " = " + HienGiaTri(cot, moi) + "\ncho " + don.Count + " đơn: " + string.Join(", ", don.Take(15).Select(d => d.Title))
                                + (don.Count > 15 ? "…" : "") + "\n\nLưu lên Teams?", "Sửa nhanh", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;

            string siteId = _tab[5], listId = _tab[6];
            Chay(async () =>
            {
                string body = SuaDon.Body(await GiaiMaNguoi(new List<KeyValuePair<CotList, object>> { new KeyValuePair<CotList, object>(cot, moi) }));
                var hong = new List<string>();
                int xong = 0;
                foreach (DonTeams d in don)
                {
                    BaoTrangThai("Đang lưu " + (xong + hong.Count + 1) + " / " + don.Count + "…");
                    try { await GraphTeams.SuaDon(siteId, listId, d.Id, d.ETag, body, CancellationToken.None); xong++; }
                    catch (DonDaDoiException) { hong.Add(d.Title + " (vừa bị người khác sửa)"); }
                    catch (InvalidOperationException ex) when (!ex.Message.StartsWith("Chưa")) { hong.Add(d.Title + " (" + ex.Message + ")"); } // thiếu quyền / chưa đăng nhập: dừng hẳn, Chay báo lỗi
                }
                BaoTrangThai("Đang tải lại danh sách…");
                var tho = await GraphTeams.TaiList(siteId, listId, CancellationToken.None);
                var ds = await Task.Run(() => PhanTichTeams.DocToanBo(tho));
                if (_tab?[6] != listId) return;
                DatDon(tho, ds);
                _chon.Clear();
                VeDon();
                string kq = "Đã gán " + cot.TieuDe + " cho " + xong + " / " + don.Count + " đơn.";
                BaoTrangThai(kq + (hong.Count > 0 ? " Lỗi: " + string.Join("; ", hong) : ""));
                if (hong.Count > 0) MessageBox.Show(kq + "\n\nKhông lưu được:\n" + string.Join("\n", hong), "Sửa nhanh", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        // ---------- Bảng đơn ----------

        private readonly HashSet<string> _nhomDong = new HashSet<string>(); // nhóm hạn đang thu gọn ("" = Chưa được gán)
        // shortcut: vẽ tối đa 500 dòng (StackPanel không ảo hoá); nhiều hơn thì lọc / thu gọn nhóm — cần hơn thì chuyển sang ListView ảo hoá
        private const int DongToiDa = 500;
        private static readonly string[] MauNen = { "#DCEBFF", "#FFE3D6", "#DDF3DC", "#F1E1FF", "#FFF0C2", "#D6F2F2", "#FFDDE8", "#E6E6F5" };
        private static readonly string[] MauChu = { "#1F4E99", "#9A3B10", "#1F6B2A", "#6B2C91", "#7A5B00", "#0F6466", "#9B1E4A", "#3D3D7A" };

        /// <summary>Cột đang hiện: chọn ở ☰ Cột (registry cot_hien_&lt;listId&gt;), chưa chọn thì mặc định.</summary>
        private List<CotList> CotHien()
        {
            string luu = CaiDatTeams.Doc("cot_hien_" + _tab[6]);
            var kq = luu.Split('\n').Select(t => _cots.FirstOrDefault(c => c.Ten == t)).Where(c => c != null).ToList();
            return kq.Count > 0 ? kq : BangDon.CotMacDinh(_cots, _don).Select(t => _cots.First(c => c.Ten == t)).ToList();
        }

        private static double DoRong(CotList c) =>
            c.Ten == "Title" ? 110 : c.Loai == "personOrGroup" ? 150 : c.Loai == "dateTime" ? (c.CoGio ? 118 : 92)
            : c.Loai == "choice" ? 120 : c.Loai == "boolean" ? 64 : c.Loai == "number" ? 80 : 170;

        /// <summary>Một hàng bảng: cột 0 là vạch màu trái, các cột sau rộng cố định để mọi hàng thẳng cột.</summary>
        private static Grid HangBang(List<CotList> cots, Brush nen)
        {
            var g = new Grid { Background = nen, HorizontalAlignment = HorizontalAlignment.Left };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); // vạch "đơn của tôi" + ô tích chọn
            foreach (CotList c in cots) g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DoRong(c)) });
            return g;
        }

        private static void DatO(Grid g, UIElement o, int cot)
        {
            Grid.SetColumn(o, cot);
            g.Children.Add(o);
        }

        private static Border Nhan(string chu, CotList c, double rong = 0)
        {
            bool nguoi = c.Loai == "personOrGroup";
            int i = BangDon.MauNhan(chu, MauNen.Length);
            Brush vien = nguoi ? KieuTeams.Mau(PhanTichTeams.BoDau(c.TieuDe).Contains("trien khai") ? "#D0528A" : "#2E9C76") : null;
            var tt = nguoi ? null : BangDon.MauTrangThai(chu); // Trạng thái: màu cố định như Lists
            return new Border
            {
                Background = nguoi ? Brushes.White : KieuTeams.Mau(tt?.Nen ?? MauNen[i]),
                BorderBrush = vien ?? (tt != null ? KieuTeams.Mau("#59000000") : null), // viền mảnh: nhãn trạng thái nổi trên dòng cùng màu
                BorderThickness = new Thickness(nguoi || tt != null ? 1 : 0),
                CornerRadius = new CornerRadius(nguoi ? 4 : 9),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(0, 1, 3, 1),
                MaxWidth = rong > 0 ? rong : DoRong(c) - 12, // nhãn dài hơn cột: cắt "…", rê chuột xem đủ (ToolTip của ô)
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = nguoi ? "👤 " + chu : chu, FontSize = 11, Foreground = nguoi ? vien : KieuTeams.Mau(tt?.Chu ?? MauChu[i]),
                                        FontWeight = nguoi ? FontWeights.SemiBold : FontWeights.Normal, TextTrimming = TextTrimming.CharacterEllipsis },
            };
        }

        private static FrameworkElement O(CotList c, DonTeams d)
        {
            string gt = c.Ten == "Title" ? (d.Id == CaiDatTeams.DonChon ? "📌 " : "") + d.Title
                                         : d.TatCa.FirstOrDefault(kv => kv.Key == c.TieuDe).Value ?? "";
            if (gt.Length > 0 && (c.Loai == "personOrGroup" || c.Loai == "choice"))
            {
                // WrapPanel: nhãn không vừa bề rộng cột thì xuống dòng (vd "Đang check" + "Câu hỏi - xác nhận" = 2 dòng), dòng đơn tự cao lên
                var p = new WrapPanel { ToolTip = gt, VerticalAlignment = VerticalAlignment.Center };
                foreach (string x in BangDon.TachNhan(gt)) p.Children.Add(Nhan(x, c));
                return p;
            }
            return new TextBlock
            {
                Text = gt.Replace('\n', ' '), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center,
                ToolTip = gt.Length > 20 ? gt : null, FontSize = 11.5, FontWeight = c.Ten == "Title" ? FontWeights.SemiBold : FontWeights.Normal,
            };
        }

        /// <summary>
        /// Ô tiêu đề cột kéo thả được: nhấn giữ rồi kéo sang tiêu đề khác, thả = cột chiếm chỗ cột đó.
        /// Lúc kéo, vạch màu ở cạnh trái / phải ô đích cho biết cột sẽ nằm trước / sau. Thứ tự lưu như ☰ Cột.
        /// </summary>
        private Border OTieuDe(List<CotList> cots, int i)
        {
            CotList c = cots[i];
            var o = new Border
            {
                Background = Brushes.Transparent, // để nhận chuột trên cả ô
                BorderThickness = new Thickness(3, 0, 3, 0), BorderBrush = Brushes.Transparent,
                AllowDrop = true, Cursor = Cursors.SizeAll, ToolTip = c.TieuDe + " — kéo để đổi vị trí cột",
                Child = new TextBlock { Text = c.TieuDe.ToUpperInvariant(), FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = KieuTeams.ChuPhu,
                                        Margin = new Thickness(3, 7, 3, 7), TextTrimming = TextTrimming.CharacterEllipsis },
            };
            Point? batDau = null;
            o.PreviewMouseLeftButtonDown += (s, e) => batDau = e.GetPosition(o);
            o.PreviewMouseLeftButtonUp += (s, e) => batDau = null;
            o.MouseMove += (s, e) =>
            {
                if (batDau == null || e.LeftButton != MouseButtonState.Pressed) return;
                Vector d = e.GetPosition(o) - batDau.Value;
                if (Math.Abs(d.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(d.Y) < SystemParameters.MinimumVerticalDragDistance) return;
                batDau = null;
                o.Opacity = 0.5;
                DragDrop.DoDragDrop(o, new DataObject("ohta-cot", c.Ten), DragDropEffects.Move);
                o.Opacity = 1;
            };
            o.DragOver += (s, e) =>
            {
                string tu = e.Data.GetData("ohta-cot") as string;
                int a = cots.FindIndex(x => x.Ten == tu);
                e.Effects = a < 0 || a == i ? DragDropEffects.None : DragDropEffects.Move;
                o.BorderBrush = e.Effects == DragDropEffects.None ? Brushes.Transparent : _mau;
                o.BorderThickness = a < i ? new Thickness(0, 0, 3, 0) : new Thickness(3, 0, 0, 0); // kéo sang phải: cột sẽ nằm sau ô này
                e.Handled = true;
            };
            o.DragLeave += (s, e) => o.BorderBrush = Brushes.Transparent;
            o.Drop += (s, e) =>
            {
                o.BorderBrush = Brushes.Transparent;
                List<string> moi = BangDon.DoiCho(cots.Select(x => x.Ten).ToList(), e.Data.GetData("ohta-cot") as string, c.Ten);
                if (moi == null) return;
                CaiDatTeams.Ghi("cot_hien_" + _tab[6], string.Join("\n", moi));
                double ngang = _cuon.HorizontalOffset;
                Dispatcher.BeginInvoke(new Action(() => { VeDon(); _cuon.ScrollToHorizontalOffset(ngang); })); // vẽ lại sau khi kéo thả kết thúc
            };
            return o;
        }

        private UIElement VeBang(List<DonTeams> loc)
        {
            List<CotList> cots = CotHien();
            var bang = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };

            var tieu = HangBang(cots, KieuTeams.Mau("#E9ECF2"));
            for (int i = 0; i < cots.Count; i++)
                DatO(tieu, OTieuDe(cots, i), i + 1);
            var tatCa = new CheckBox { IsChecked = loc.Count > 0 && loc.All(d => _chon.Contains(d.Id)), HorizontalAlignment = HorizontalAlignment.Center,
                                       VerticalAlignment = VerticalAlignment.Center, ToolTip = "Chọn / bỏ chọn mọi đơn đang hiện" };
            void ChonHet(bool co)
            {
                _dongBoChon = true; // tích hàng loạt: vẽ thanh 1 lần ở cuối
                foreach (DonTeams d in loc) { if (co) _chon.Add(d.Id); else _chon.Remove(d.Id); }
                foreach (CheckBox o in _oChon.Values) o.IsChecked = _chon.Contains((string)o.Tag);
                _dongBoChon = false;
                CapNhatThanhNhanh();
            }
            tatCa.Checked += (s, e) => ChonHet(true);
            tatCa.Unchecked += (s, e) => ChonHet(false);
            DatO(tieu, tatCa, 0);
            bang.Children.Add(tieu);
            _oChon.Clear();

            DateTime homNay = DateTime.Today;
            int daVe = 0;
            foreach (var nhom in BangDon.NhomTheoHan(loc))
            {
                string khoa = nhom.Key?.ToString("dd/MM/yyyy") ?? "";
                bool dong = _nhomDong.Contains(khoa);
                var dau = new TextBlock
                {
                    Text = (dong ? "▸  " : "▾  ") + "HẠN HOÀN THÀNH: " + (khoa.Length == 0 ? "Chưa được gán" : khoa) + " (" + nhom.Count() + ")",
                    FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(4, 12, 0, 6), Cursor = Cursors.Hand,
                };
                dau.MouseLeftButtonUp += (s, e) => { if (!_nhomDong.Remove(khoa)) _nhomDong.Add(khoa); VeDon(); };
                bang.Children.Add(dau);
                if (dong) continue;

                foreach (DonTeams d in nhom.OrderBy(x => x.Title))
                {
                    if (daVe >= DongToiDa) break;
                    string mau = BangDon.MauDong(d, homNay);
                    Brush nen = KieuTeams.Mau(mau == "hong" ? "#F6D3DA" : mau == "do" ? "#F28B82" : mau == "vang" ? "#F7E2B0" : mau == "xanh" ? "#CBE3FA" : "#FFFFFF");
                    var h = HangBang(cots, nen);
                    h.MinHeight = 30;
                    h.Margin = new Thickness(0, 0, 0, 1);
                    h.Cursor = Cursors.Hand;
                    if (d.CuaToi(_toiTen)) DatO(h, new Border { Background = _mau, Width = 4, HorizontalAlignment = HorizontalAlignment.Left }, 0); // đơn của tôi: vạch màu trái
                    var chon = new CheckBox { IsChecked = _chon.Contains(d.Id), Tag = d.Id, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    // Checked/Unchecked (không dùng Click): ăn cả chuột, phím cách, công cụ trợ năng; bấm ô tích không mở chi tiết (nút tự Handled)
                    chon.Checked += (s, e) => { _chon.Add(d.Id); if (!_dongBoChon) CapNhatThanhNhanh(); };
                    chon.Unchecked += (s, e) => { _chon.Remove(d.Id); if (!_dongBoChon) CapNhatThanhNhanh(); };
                    _oChon[d.Id] = chon;
                    DatO(h, chon, 0);
                    for (int i = 0; i < cots.Count; i++)
                    {
                        FrameworkElement o = O(cots[i], d);
                        o.Margin = new Thickness(6, 3, 6, 3);
                        DatO(h, new Border { ClipToBounds = true, Child = o }, i + 1);
                    }
                    h.MouseLeftButtonUp += (s, e) => HienChiTiet(d);
                    bang.Children.Add(h);
                    daVe++;
                }
                bang.Children.Add(new TextBlock { Text = nhom.Count().ToString(), FontWeight = FontWeights.SemiBold, Margin = new Thickness(16, 4, 0, 4) });
            }
            if (daVe >= DongToiDa)
                bang.Children.Add(new TextBlock { Text = "Chỉ hiện " + DongToiDa + " dòng đầu — lọc theo nhân viên hoặc thu gọn nhóm để xem các đơn khác.",
                                                  Foreground = KieuTeams.Mau("#B42318"), Margin = new Thickness(4, 6, 0, 0) });
            bang.Children.Add(new TextBlock { Text = "Số lượng " + loc.Count, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 10, 0, 10) });
            return bang;
        }

        /// <summary>
        /// ☰ Cột: tích cột muốn hiện và sắp thứ tự bằng ▲ ▼ (cột đang hiện ở đầu, đúng thứ tự trên bảng),
        /// nhớ riêng từng danh sách.
        /// </summary>
        private void ChonCot()
        {
            List<CotList> hien = CotHien();
            var hop = hien.Concat(_cots.Where(c => !hien.Contains(c)))
                          .Select(c => new CheckBox { Content = c.TieuDe, Tag = c.Ten, IsChecked = hien.Contains(c), VerticalAlignment = VerticalAlignment.Center })
                          .ToList();
            var ds = new StackPanel();
            void Ve()
            {
                ds.Children.Clear();
                for (int i = 0; i < hop.Count; i++)
                {
                    int vt = i;
                    var hang = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };
                    Button Doi(string chu, int buoc, bool duoc)
                    {
                        var b = new Button { Content = chu, Width = 26, Height = 22, Margin = new Thickness(0, 0, 3, 0), IsEnabled = duoc, Padding = new Thickness(0),
                                             ToolTip = buoc < 0 ? "Đưa lên (sang trái trên bảng)" : "Đưa xuống (sang phải trên bảng)" };
                        b.Click += (s, e) =>
                        {
                            CheckBox h = hop[vt];
                            hop.RemoveAt(vt);
                            hop.Insert(vt + buoc, h);
                            Ve();
                        };
                        return b;
                    }
                    var len = Doi("▲", -1, vt > 0);
                    var xuong = Doi("▼", 1, vt < hop.Count - 1);
                    DockPanel.SetDock(len, Dock.Left);
                    DockPanel.SetDock(xuong, Dock.Left);
                    hang.Children.Add(len);
                    hang.Children.Add(xuong);
                    (hop[vt].Parent as Panel)?.Children.Remove(hop[vt]);
                    hang.Children.Add(hop[vt]);
                    ds.Children.Add(hang);
                }
            }
            Ve();
            var w = new Window
            {
                Title = "Chọn và sắp xếp cột", Width = 380, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ShowInTaskbar = false, Background = KieuTeams.Nen, FontFamily = KieuTeams.Font,
            };
            var nut = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            nut.Children.Add(KieuTeams.Nut("Mặc định", _mau, (s, e) => { CaiDatTeams.Ghi("cot_hien_" + _tab[6], ""); w.Close(); VeDon(); }, chinh: false));
            nut.Children.Add(KieuTeams.Nut("Huỷ", _mau, (s, e) => w.Close(), chinh: false));
            nut.Children.Add(KieuTeams.Nut("Lưu", _mau, (s, e) =>
            {
                var chon = hop.Where(h => h.IsChecked == true).Select(h => (string)h.Tag).ToList();
                if (chon.Count == 0) return;
                CaiDatTeams.Ghi("cot_hien_" + _tab[6], string.Join("\n", chon));
                w.Close();
                VeDon();
            }));
            var goc = new DockPanel { Margin = new Thickness(12) };
            DockPanel.SetDock(nut, Dock.Bottom);
            goc.Children.Add(nut);
            goc.Children.Add(new ScrollViewer { Content = ds, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            w.Content = goc;
            w.ShowDialog();
        }

        /// <summary>
        /// Chi tiết đơn dạng form Lists: 2 phần (OHTA COMPANY / OFT BY OHTA), mỗi phần lưới 3 cột.
        /// Bấm một ô sửa được → ô nhập + ✓ Lưu / ✕ Huỷ ngay tại chỗ; ✓ gửi riêng cột đó lên Teams. dangSua = tên cột đang mở ô nhập.
        /// </summary>
        private void HienChiTiet(DonTeams d, string thongBao = null, string dangSua = null)
        {
            DauTrang(d.Title, "Danh sách đơn", VeDon);
            BaoTrangThai("");
            if (thongBao != null) _noiDung.Children.Add(new TextBlock { Text = thongBao, Foreground = KieuTeams.Mau("#1F7A4D"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            _noiDung.Children.Add(NutChiTiet(d));

            // 2 cột: form + tệp đính kèm bên trái, chat bên phải (các chuỗi chat xếp ngang nhau)
            var trai = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var phai = new StackPanel();
            var hai = new Grid();
            hai.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.55, GridUnitType.Star) });
            hai.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.45, GridUnitType.Star), MinWidth = 360 });
            Grid.SetColumn(phai, 1);
            hai.Children.Add(trai);
            hai.Children.Add(phai);
            _noiDung.Children.Add(hai);

            bool sua = !string.IsNullOrEmpty(d.ETag);
            List<CotList> cots = _cots.Count > 0 ? _cots
                : d.TatCa.Select(kv => new CotList { Ten = kv.Key, TieuDe = kv.Key, Loai = "khac" }).ToList(); // chưa có cột (hiếm): chỉ xem
            foreach (string phan in new[] { BangDon.PhanNhat, BangDon.PhanViet })
            {
                var cotPhan = cots.Where(c => BangDon.PhanForm(c) == phan).ToList();
                if (cotPhan.Count == 0) continue;
                var khoi = new StackPanel();
                khoi.Children.Add(new TextBlock { Text = phan, FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) });
                var luoi = new Grid();
                for (int k = 0; k < 3; k++) luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                for (int i = 0; i < cotPhan.Count; i++)
                {
                    if (i % 3 == 0) luoi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    FrameworkElement o = OTruong(d, cotPhan[i], sua, cotPhan[i].Ten == dangSua);
                    Grid.SetRow(o, i / 3);
                    Grid.SetColumn(o, i % 3);
                    luoi.Children.Add(o);
                }
                khoi.Children.Add(luoi);
                var the = KieuTeams.The(khoi);
                the.Margin = new Thickness(0, 0, 0, 8);
                trai.Children.Add(the);
            }

            int phien = ++_phienChiTiet;
            trai.Children.Add(KhoiTep(d, phien));
            phai.Children.Add(KhoiChat(d, phien));
        }

        // ---------- Tệp đính kèm + chat kèm đơn (tải sau khi form đã hiện) ----------

        private int _phienChiTiet; // tăng mỗi lần vẽ chi tiết: kết quả tải về muộn của trang cũ thì bỏ

        private static TextBlock ChuPhu(string s, bool loi = false) =>
            new TextBlock { Text = s, Foreground = loi ? KieuTeams.Mau("#B42318") : KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap };

        private Border KhoiTep(DonTeams d, int phien)
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = "📎 Tệp đính kèm", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            var ds = new WrapPanel();
            p.Children.Add(ds);
            if (!d.CoTep) ds.Children.Add(ChuPhu("Đơn không có tệp đính kèm."));
            else { ds.Children.Add(ChuPhu("Đang tải danh sách tệp…")); TaiTepVao(ds, d, phien); }
            var the = KieuTeams.The(p);
            the.Margin = new Thickness(0, 0, 0, 8);
            return the;
        }

        private async void TaiTepVao(WrapPanel ds, DonTeams d, int phien)
        {
            string listUrl = _tab[7], listId = _tab[6];
            try
            {
                List<TepDinhKem> tep = await GraphTeams.TepCuaDon(listUrl, listId, d.Id, CancellationToken.None);
                if (phien != _phienChiTiet) return;
                ds.Children.Clear();
                if (tep.Count == 0) ds.Children.Add(ChuPhu("Đơn không có tệp đính kèm."));
                foreach (TepDinhKem t in tep)
                {
                    Button b = null;
                    b = KieuTeams.Nut("📄 " + t.Ten, _mau, async (s, e) =>
                    {
                        b.IsEnabled = false;
                        try { MoNgoai(await GraphTeams.TaiTep(listUrl, d.Id, t, CancellationToken.None)); }
                        catch (Exception ex) { MessageBox.Show("Không tải được " + t.Ten + ":\n" + ex.Message, "Tệp đính kèm", MessageBoxButton.OK, MessageBoxImage.Warning); }
                        finally { b.IsEnabled = true; }
                    }, chinh: false);
                    b.ToolTip = "Tải về máy và mở";
                    ds.Children.Add(b);
                }
            }
            catch (Exception ex) { if (phien == _phienChiTiet) { ds.Children.Clear(); ds.Children.Add(ChuPhu(ex.Message, true)); } }
        }

        // Ảnh chat đã tải trong phiên (URL → ảnh) — vẽ lại chi tiết / trả lời không tải lại
        private readonly Dictionary<string, System.Windows.Media.Imaging.BitmapImage> _anhChat = new Dictionary<string, System.Windows.Media.Imaging.BitmapImage>();

        /// <summary>Ảnh dán trong tin: hiện thu nhỏ (tải nền), bấm để mở ảnh cỡ thật bằng trình xem ảnh của Windows.</summary>
        private FrameworkElement AnhChat(string url)
        {
            var anh = new Image { MaxHeight = 260, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left, Cursor = Cursors.Hand, ToolTip = "Bấm để mở ảnh cỡ thật" };
            var khung = new Border { Margin = new Thickness(0, 4, 0, 2), HorizontalAlignment = HorizontalAlignment.Left, Child = ChuPhu("Đang tải ảnh…") };
            async void Tai()
            {
                try
                {
                    if (!_anhChat.TryGetValue(url, out var bmp))
                    {
                        byte[] b = await GraphTeams.TaiAnhChat(url, CancellationToken.None);
                        bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.StreamSource = new MemoryStream(b);
                        bmp.EndInit();
                        bmp.Freeze();
                        _anhChat[url] = bmp;
                    }
                    anh.Source = bmp;
                    khung.Child = anh;
                }
                catch (Exception ex) { khung.Child = ChuPhu("Không tải được ảnh: " + ex.Message.Split('\n')[0], true); }
            }
            anh.MouseLeftButtonUp += (s, e) =>
            {
                try
                {
                    if (!(anh.Source is System.Windows.Media.Imaging.BitmapImage bmp)) return;
                    string f = Path.Combine(Path.GetTempPath(), "OHTA_TOOL", "teams", "chat", "anh_" + Math.Abs(url.GetHashCode()) + ".png");
                    Directory.CreateDirectory(Path.GetDirectoryName(f));
                    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
                    using (var ra = File.Create(f)) enc.Save(ra);
                    MoNgoai(f);
                }
                catch (Exception ex) { MessageBox.Show("Không mở được ảnh:\n" + ex.Message, "Chat", MessageBoxButton.OK, MessageBoxImage.Warning); }
            };
            Tai();
            return khung;
        }

        /// <summary>Tệp gửi kèm trong tin: bấm → tải về máy (Graph /shares) rồi mở.</summary>
        private Button NutTepChat(TepDinhKem f)
        {
            Button b = null;
            b = KieuTeams.Nut("📄 " + f.Ten, _mau, async (s, e) =>
            {
                b.IsEnabled = false;
                try { MoNgoai(await GraphTeams.TaiTepChat(f, CancellationToken.None)); }
                catch (Exception ex) { MessageBox.Show("Không tải được " + f.Ten + ":\n" + ex.Message, "Chat", MessageBoxButton.OK, MessageBoxImage.Warning); }
                finally { b.IsEnabled = true; }
            }, chinh: false);
            b.ToolTip = "Tải về máy và mở";
            return b;
        }

        /// <summary>
        /// Trao đổi trên Teams: luôn hiện các kênh dùng nhiều nhất (2 kênh) + các kênh khác đơn có chuỗi chat; mỗi kênh một cột,
        /// thanh trượt riêng. Kênh đơn chưa có chuỗi → ô nhập + nút Tạo trao đổi.
        /// </summary>
        private Border KhoiChat(DonTeams d, int phien)
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = "💬 Trao đổi trên Teams", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            List<HoiThoai> cua = TraoDoi.DocHoiThoai(d.HoiThoaiMeta);
            var daDung = new HashSet<HoiThoai>();
            var cot = new List<(HoiThoai Kenh, HoiThoai Chuoi)>();
            foreach (HoiThoai k in TraoDoi.KenhPhoBien(_don).OrderBy(k => k.ChannelId == _tab?[2] ? 0 : 1)) // kênh có tab danh sách đơn đứng trước
            {
                HoiThoai c = cua.FirstOrDefault(h => !daDung.Contains(h) && h.ChannelId == k.ChannelId && h.TeamInternalId == k.TeamInternalId);
                if (c != null) daDung.Add(c);
                cot.Add((k, c));
            }
            foreach (HoiThoai c in cua.Where(h => !daDung.Contains(h))) cot.Add((c, c)); // chuỗi ở kênh khác: thêm cột
            if (cot.Count == 0) p.Children.Add(ChuPhu("Đơn chưa có chuỗi chat nào trên Teams."));

            var ngang = new System.Windows.Controls.Primitives.UniformGrid { Rows = 1, Columns = Math.Max(1, cot.Count) };
            for (int i = 0; i < cot.Count; i++)
            {
                var khung = new StackPanel { Margin = new Thickness(i == 0 ? 0 : 5, 0, i == cot.Count - 1 ? 0 : 5, 0) };
                khung.Children.Add(ChuPhu("Đang tải chat…"));
                ngang.Children.Add(khung);
                if (cot[i].Chuoi != null) TaiChuoiVao(khung, cot[i].Chuoi, phien);
                else KhungTaoChuoi(khung, d, cot[i].Kenh, phien);
            }
            p.Children.Add(ngang);
            var the = KieuTeams.The(p);
            the.Margin = new Thickness(0, 0, 0, 8);
            return the;
        }

        private TextBlock TieuDeNhom(string ten) =>
            new TextBlock { Text = "Nhóm " + ten, Foreground = _mau, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) };

        /// <summary>Khung tin có thanh trượt riêng (cao cố định), tự cuộn xuống tin mới nhất khi mở.</summary>
        private static ScrollViewer KhungCuonTin(UIElement ds)
        {
            var sv = new ScrollViewer { Content = ds, Height = 460, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            sv.Loaded += (s, e) => sv.ScrollToEnd();
            return sv;
        }

        private async void TaiChuoiVao(StackPanel khung, HoiThoai h, int phien)
        {
            try
            {
                var kq = await GraphTeams.TaiChuoi(h, CancellationToken.None);
                if (phien != _phienChiTiet) return;
                khung.Children.Clear();
                if (kq == null) { khung.Children.Add(ChuPhu("Chuỗi chat nằm ở nhóm Teams bạn chưa tham gia.")); return; }
                khung.Children.Add(TieuDeNhom(kq.Value.NhomTen));
                var ds = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };
                foreach (TinNhan t in kq.Value.Tin)
                {
                    var tin = new StackPanel();
                    tin.Children.Add(new TextBlock { Text = t.Nguoi + "   " + (t.Luc?.ToString("dd/MM HH:mm") ?? ""), FontWeight = FontWeights.SemiBold, FontSize = 11.5 });
                    if (t.Chu.Length > 0)
                        tin.Children.Add(new TextBox { Text = t.Chu, IsReadOnly = true, BorderThickness = new Thickness(0), Background = Brushes.Transparent, TextWrapping = TextWrapping.Wrap });
                    foreach (string url in t.Anh) tin.Children.Add(AnhChat(url));
                    if (t.Tep.Count > 0)
                    {
                        var tep = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                        foreach (TepDinhKem f in t.Tep) tep.Children.Add(NutTepChat(f));
                        tin.Children.Add(tep);
                    }
                    ds.Children.Add(new Border { Child = tin, Background = KieuTeams.Mau("#EEF0FA"), CornerRadius = new CornerRadius(6),
                                                 Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 4) });
                }
                khung.Children.Add(KhungCuonTin(ds));
                var o = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 44, Margin = new Thickness(0, 6, 0, 4), ToolTip = "Trả lời trong chuỗi hội thoại" };
                Button gui = null;
                gui = KieuTeams.Nut("Gửi trả lời", _mau, async (s, e) =>
                {
                    if (o.Text.Trim().Length == 0) return;
                    gui.IsEnabled = false;
                    try
                    {
                        await GraphTeams.TraLoi(kq.Value.NhomId, h, o.Text, CancellationToken.None);
                        if (phien != _phienChiTiet) return;
                        khung.Children.Clear();
                        khung.Children.Add(ChuPhu("Đã gửi. Đang tải lại chat…"));
                        TaiChuoiVao(khung, h, phien);
                    }
                    catch (Exception ex) { gui.IsEnabled = true; MessageBox.Show("Không gửi được:\n" + ex.Message, "Chat", MessageBoxButton.OK, MessageBoxImage.Warning); }
                });
                gui.HorizontalAlignment = HorizontalAlignment.Left;
                khung.Children.Add(o);
                khung.Children.Add(gui);
            }
            catch (Exception ex) { if (phien == _phienChiTiet) { khung.Children.Clear(); khung.Children.Add(ChuPhu(ex.Message, true)); } }
        }

        /// <summary>Kênh đơn chưa có chuỗi chat: tên nhóm + ô nhập + nút Tạo trao đổi (tin đầu chuỗi có link tới đơn).</summary>
        private async void KhungTaoChuoi(StackPanel khung, DonTeams d, HoiThoai kenh, int phien)
        {
            try
            {
                var nhom = await GraphTeams.NhomCuaInternal(kenh.TeamInternalId, CancellationToken.None);
                if (phien != _phienChiTiet) return;
                khung.Children.Clear();
                if (nhom == null) { khung.Children.Add(ChuPhu("Kênh nằm ở nhóm Teams bạn chưa tham gia.")); return; }
                khung.Children.Add(TieuDeNhom(nhom.Value.Ten));
                khung.Children.Add(new Border
                {
                    Height = 460, BorderBrush = KieuTeams.Mau("#D0D5DD"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
                    Child = new TextBlock { Text = "Chưa có trao đổi về đơn " + d.Title + " trong kênh này.\nGõ nội dung bên dưới rồi bấm Tạo trao đổi.",
                                            Foreground = KieuTeams.ChuPhu, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
                                            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12) },
                });
                var o = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 44, Margin = new Thickness(0, 6, 0, 4), ToolTip = "Nội dung tin đầu tiên" };
                var tao = KieuTeams.Nut("Tạo trao đổi", _mau, (s, e) => TaoTraoDoi(d, kenh, nhom.Value.Id, nhom.Value.Ten, o.Text));
                tao.HorizontalAlignment = HorizontalAlignment.Left;
                khung.Children.Add(o);
                khung.Children.Add(tao);
            }
            catch (Exception ex) { if (phien == _phienChiTiet) { khung.Children.Clear(); khung.Children.Add(ChuPhu(ex.Message, true)); } }
        }

        /// <summary>
        /// Tạo trao đổi: gửi tin đầu chuỗi vào kênh ("Về đơn …" + nội dung), rồi ghi chuỗi mới vào cột ẩn teamsConversationMetadata
        /// của đơn (giữ chuỗi cũ) để Lists và OhtaTeams cùng thấy; đơn vừa bị sửa → đọc lại rồi ghi lại 1 lần.
        /// </summary>
        private void TaoTraoDoi(DonTeams d, HoiThoai kenh, string nhomId, string nhomTen, string chu)
        {
            if (chu.Trim().Length == 0) { BaoTrangThai("Gõ nội dung trao đổi trước khi tạo."); return; }
            if (MessageBox.Show("Gửi tin mở trao đổi về đơn " + d.Title + " vào nhóm " + nhomTen + "?\n\n" + chu.Trim(), "Tạo trao đổi",
                                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            string siteId = _tab[5], listId = _tab[6];
            string cotMeta = d.HoiThoaiCot.Length > 0 ? d.HoiThoaiCot : _don.Select(x => x.HoiThoaiCot).FirstOrDefault(x => x.Length > 0);
            Chay(async () =>
            {
                BaoTrangThai("Đang tạo trao đổi…");
                string tabId = null;
                try { tabId = await GraphTeams.IdTabDon(_tab[0], _tab[2], _tab[4], CancellationToken.None); }
                catch (InvalidOperationException) { } // không đọc được tab: tạo tin không thẻ
                var (conv, kieu, loiThe) = await GraphTeams.TaoHoiThoai(nhomId, kenh.ChannelId, chu, d.Title, d.WebUrl, tabId, CancellationToken.None);
                var chuoi = new HoiThoai { ConvId = conv, ChannelId = kenh.ChannelId, TeamInternalId = kenh.TeamInternalId };
                string thongBao = "Đã tạo trao đổi trong nhóm " + nhomTen
                    + (kieu == "lists" ? " (kèm thẻ Về: " + d.Title + " của Lists)." : kieu == "the" ? " (kèm thẻ 📋 Về: " + d.Title + ")." : " (tin có link tới đơn).")
                    + (loiThe.Length > 0 ? "\nTeams không nhận: " + loiThe : "");
                DonTeams moi = d;
                if (cotMeta == null) thongBao += " Chưa gắn được vào đơn (danh sách chưa có cột chat ẩn) — xem trong kênh.";
                else
                    for (int lan = 0; lan < 2; lan++)
                    {
                        var body = new OhtaTool.Plugin.Core.JsonObject();
                        body.Set(cotMeta, TraoDoi.ThemHoiThoai(moi.HoiThoaiMeta, chuoi));
                        try { await GraphTeams.SuaDon(siteId, listId, d.Id, moi.ETag, OhtaTool.Plugin.Core.JsonLite.Serialize(body), CancellationToken.None); break; }
                        catch (DonDaDoiException) when (lan == 0) { moi = await TaiLaiMotDon(siteId, listId, d.Id) ?? moi; } // người khác vừa sửa: đọc bản mới rồi ghi lại
                    }
                DonTeams sau = await TaiLaiMotDon(siteId, listId, d.Id);
                if (_tab?[6] != listId) return;
                if (sau == null) { VeDon(); return; }
                HienChiTiet(sau, thongBao);
            });
        }

        /// <summary>Đọc lại một đơn từ Teams, thay vào danh sách đang có (ghi dang_lam.json). Không thấy → null.</summary>
        private async Task<DonTeams> TaiLaiMotDon(string siteId, string listId, string id)
        {
            string json = await GraphTeams.TaiMotDon(siteId, listId, id, CancellationToken.None);
            var ten = _danhBaDu.ToDictionary(kv => kv.Key, kv => kv.Value);
            DonTeams sau = PhanTichTeams.DocDon(json, _cots, PhanTichTeams.KhopCot(_cots), x => x != null && ten.TryGetValue(x, out string t) ? t : "#" + x).FirstOrDefault();
            if (sau == null || _tab?[6] != listId) return sau;
            int vt = _don.FindIndex(x => x.Id == id);
            if (vt >= 0) _don[vt] = sau; else _don.Add(sau);
            GhiDangLam();
            return sau;
        }

        /// <summary>Biểu tượng loại cột (font Segoe MDL2 Assets có sẵn trên Windows 10/11).</summary>
        private static string BieuTuong(CotList c) =>
            c.Ten == "Title" ? "\uE8A5" : c.Loai == "dateTime" ? "\uE787" : c.Loai == "personOrGroup" ? "\uE77B"
            : c.Loai == "choice" ? (c.ChonNhieu ? "\uE8FD" : "\uE73E") : c.Loai == "number" ? "\uE8EF" : c.Loai == "boolean" ? "\uE73A" : "\uE8D2";

        /// <summary>Một ô của form: tên cột + giá trị (bấm để sửa) hoặc ô nhập đang mở.</summary>
        private FrameworkElement OTruong(DonTeams d, CotList c, bool choSua, bool dangSua)
        {
            var p = new StackPanel { Margin = new Thickness(0, 0, 16, 14) };
            var nhan = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            nhan.Children.Add(new TextBlock { Text = BieuTuong(c), FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 11, Foreground = KieuTeams.ChuPhu,
                                              VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            nhan.Children.Add(new TextBlock { Text = c.TieuDe, FontWeight = FontWeights.SemiBold, FontSize = 11.5, TextWrapping = TextWrapping.Wrap });
            p.Children.Add(nhan);
            bool suaDuoc = choSua && c.SuaDuoc;

            if (dangSua && suaDuoc)
            {
                object cu = SuaDon.GiaTriForm(c, d.Tho.TryGetValue(c.Ten, out object tho) ? tho : null);
                var (oNhap, doc) = TaoONhap(c, cu);
                p.Children.Add(oNhap);
                var nut = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                nut.Children.Add(KieuTeams.Nut("✓ Lưu", _mau, (s, e) => LuuMotCot(d, c, cu, doc)));
                nut.Children.Add(KieuTeams.Nut("✕ Huỷ", _mau, (s, e) => HienChiTiet(d), chinh: false));
                p.Children.Add(nut);
                Dispatcher.BeginInvoke(new Action(() => (oNhap as Control ?? FindFirstControl(oNhap))?.Focus()));
                return p;
            }

            string gt = c.Ten == "Title" ? d.Title : d.TatCa.FirstOrDefault(kv => kv.Key == c.TieuDe).Value ?? "";
            FrameworkElement giaTri;
            if (gt.Length == 0)
                giaTri = new TextBlock { Text = suaDuoc ? "Nhập giá trị tại đây" : "", Foreground = KieuTeams.Mau("#9AA3AF") };
            else if (c.Loai == "personOrGroup" || c.Loai == "choice")
            {
                var w = new WrapPanel();
                foreach (string x in BangDon.TachNhan(gt)) w.Children.Add(Nhan(x, c, 400));
                giaTri = w;
            }
            else giaTri = new TextBlock { Text = gt, TextWrapping = TextWrapping.Wrap, FontWeight = c.Ten == "Title" ? FontWeights.SemiBold : FontWeights.Normal };

            if (!suaDuoc) { p.Children.Add(giaTri); return p; }
            var vung = new Border { Child = giaTri, Padding = new Thickness(4, 3, 4, 3), Margin = new Thickness(-4, 0, 0, 0), CornerRadius = new CornerRadius(4),
                                    Background = Brushes.Transparent, Cursor = Cursors.Hand, ToolTip = "Bấm để sửa" };
            vung.MouseEnter += (s, e) => vung.Background = KieuTeams.Mau("#EEF0F6");
            vung.MouseLeave += (s, e) => vung.Background = Brushes.Transparent;
            vung.MouseLeftButtonUp += (s, e) => HienChiTiet(d, null, c.Ten);
            p.Children.Add(vung);
            return p;
        }

        private static Control FindFirstControl(DependencyObject goc)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(goc); i++)
            {
                DependencyObject con = VisualTreeHelper.GetChild(goc, i);
                if (con is TextBox || con is ComboBox || con is DatePicker || con is CheckBox) return (Control)con;
                Control sau = FindFirstControl(con);
                if (sau != null) return sau;
            }
            return null;
        }

        /// <summary>✓ Lưu một ô: đọc ô nhập, không đổi thì đóng ô, sai thì báo, đúng thì gửi ngay cột đó lên Teams.</summary>
        private void LuuMotCot(DonTeams d, CotList c, object cu, Func<object> doc)
        {
            object moi;
            try { moi = doc(); }
            catch (FormatException ex) { BaoTrangThai(ex.Message); return; }
            if (SuaDon.GiongNhau(cu, moi)) { HienChiTiet(d); return; }
            string loi = SuaDon.KiemTra(c, moi);
            if (loi != null) { BaoTrangThai(loi); return; }
            GuiSua(d, new List<KeyValuePair<CotList, object>> { new KeyValuePair<CotList, object>(c, moi) });
        }

        /// <summary>Hàng nút trên đầu chi tiết: 📌, ✏ Sửa nhiều ô, Mở trên Teams.</summary>
        private WrapPanel NutChiTiet(DonTeams d)
        {
            var nut = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            bool dangChon = d.Id == CaiDatTeams.DonChon;
            nut.Children.Add(KieuTeams.Nut(dangChon ? "📌 Bỏ đang làm" : "📌 Đang làm", _mau, (s, e) =>
            {
                CaiDatTeams.DonChon = dangChon ? "" : d.Id;
                GhiDangLam();
                if (_loiGhi.Length > 0) MessageBox.Show(_loiGhi.TrimStart(' ', '—'), "Teams", MessageBoxButton.OK, MessageBoxImage.Warning);
                HienChiTiet(d);
            }, chinh: !dangChon));
            if (_cots.Any(c => c.SuaDuoc) && !string.IsNullOrEmpty(d.ETag)) nut.Children.Add(KieuTeams.Nut("✏ Sửa nhiều ô", _mau, (s, e) => HienSua(d), chinh: false));
            if (!string.IsNullOrEmpty(d.WebUrl)) nut.Children.Add(KieuTeams.Nut("Mở trên Teams", _mau, (s, e) => MoNgoai(d.WebUrl), chinh: dangChon));
            return nut;
        }

        // ---------- Sửa đơn ----------

        private string TenNguoi(string id) => _danhBaDu.FirstOrDefault(kv => kv.Key == id).Value ?? "#" + id;

        private string HienGiaTri(CotList c, object v)
        {
            if (c.Loai == "personOrGroup")
                return v is List<string> ids && ids.Count > 0 ? string.Join(", ", ids.Select(TenNguoi)) : "(trống)";
            switch (v)
            {
                case null: return "(trống)";
                case List<string> ds: return ds.Count == 0 ? "(trống)" : string.Join(", ", ds);
                case DateTime t: return t.ToString(t.TimeOfDay == TimeSpan.Zero ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm");
                case bool b: return b ? "Có" : "Không";
                case double n: return n.ToString(System.Globalization.CultureInfo.InvariantCulture);
                default: return v.ToString().Length == 0 ? "(trống)" : v.ToString();
            }
        }

        /// <summary>Ô nhập theo loại cột + hàm đọc lại giá trị (ném FormatException kèm lời báo khi nhập sai).</summary>
        private (FrameworkElement O, Func<object> Doc) TaoONhap(CotList c, object cu)
        {
            switch (c.Loai)
            {
                case "personOrGroup":
                    return ONguoi(c, cu as List<string> ?? new List<string>());
                case "boolean":
                {
                    var cb = new CheckBox { IsChecked = cu is bool b && b, VerticalAlignment = VerticalAlignment.Center };
                    return (cb, () => cb.IsChecked == true);
                }
                case "choice" when c.ChonNhieu:
                {
                    var p = new WrapPanel();
                    var dang = cu as List<string> ?? new List<string>();
                    var o = c.LuaChon.Concat(dang.Where(x => !c.LuaChon.Contains(x)))
                                     .Select(x => new CheckBox { Content = x, IsChecked = dang.Contains(x), Margin = new Thickness(0, 2, 10, 2) }).ToList();
                    o.ForEach(x => p.Children.Add(x));
                    return (p, () => o.Where(x => x.IsChecked == true).Select(x => (string)x.Content).ToList());
                }
                case "choice":
                {
                    var cb = new ComboBox { IsEditable = false };
                    cb.Items.Add("");
                    foreach (string x in c.LuaChon) cb.Items.Add(x);
                    if (cu is string s && s.Length > 0 && !c.LuaChon.Contains(s)) cb.Items.Add(s);
                    cb.SelectedItem = cu as string ?? "";
                    return (cb, () => cb.SelectedItem as string ?? "");
                }
                case "dateTime":
                {
                    var t = cu as DateTime?;
                    var dp = new DatePicker { SelectedDate = t?.Date, SelectedDateFormat = DatePickerFormat.Short };
                    if (!c.CoGio) return (dp, () => dp.SelectedDate?.Date);
                    var gio = new TextBox { Text = t?.ToString("HH:mm") ?? "", Width = 56, Margin = new Thickness(6, 0, 0, 0), VerticalContentAlignment = VerticalAlignment.Center, ToolTip = "Giờ, dạng HH:mm" };
                    var p = new StackPanel { Orientation = Orientation.Horizontal };
                    p.Children.Add(dp);
                    p.Children.Add(gio);
                    return (p, () =>
                    {
                        if (dp.SelectedDate == null) return null;
                        if (!SuaDon.DocGio(gio.Text, out TimeSpan g)) throw new FormatException(c.TieuDe + ": giờ phải dạng HH:mm (vd 08:30)");
                        return dp.SelectedDate.Value.Date + g;
                    });
                }
                case "number":
                {
                    var tb = new TextBox { Text = (cu as double?)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "" };
                    return (tb, () => SuaDon.DocSo(tb.Text, out double? so) ? (object)so : throw new FormatException(c.TieuDe + ": phải là số"));
                }
                default:
                {
                    var tb = new TextBox { Text = cu as string ?? "", TextWrapping = TextWrapping.Wrap, AcceptsReturn = c.NhieuDong, MinHeight = c.NhieuDong ? 60 : 0,
                                           VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 200 };
                    return (tb, () => tb.Text);
                }
            }
        }

        /// <summary>
        /// Chọn nhân viên: ô gõ tên để lọc (bỏ dấu) + danh sách tích chọn; người đang giao xếp đầu.
        /// Cột 1 người: tích người mới thì bỏ người cũ.
        /// </summary>
        private (FrameworkElement O, Func<object> Doc) ONguoi(CotList c, List<string> dang)
        {
            var ds = dang.Select(id => new KeyValuePair<string, string>(id, TenNguoi(id)))
                         .Concat(_danhBa.Where(kv => !dang.Contains(kv.Key))).ToList();
            var hop = ds.Select(kv => new CheckBox { Content = kv.Value, Tag = kv.Key, IsChecked = dang.Contains(kv.Key), Margin = new Thickness(0, 1, 0, 1) }).ToList();
            var tom = new TextBlock { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) };
            void CapNhatTom()
            {
                var chon = hop.Where(h => h.IsChecked == true).Select(h => (string)h.Content).ToList();
                tom.Text = chon.Count == 0 ? "(chưa giao ai)" : string.Join(", ", chon);
            }
            foreach (CheckBox h in hop)
            {
                h.Checked += (s, e) => { if (!c.ChonNhieu) foreach (CheckBox k in hop) if (k != h) k.IsChecked = false; CapNhatTom(); };
                h.Unchecked += (s, e) => CapNhatTom();
            }
            CapNhatTom();

            var loc = new TextBox { ToolTip = "Gõ tên để lọc", Margin = new Thickness(0, 0, 0, 3) };
            loc.TextChanged += (s, e) =>
            {
                string k = PhanTichTeams.BoDau(loc.Text);
                foreach (CheckBox h in hop)
                    h.Visibility = k.Length == 0 || h.IsChecked == true || PhanTichTeams.BoDau((string)h.Content).Contains(k) ? Visibility.Visible : Visibility.Collapsed;
            };
            var dsPanel = new StackPanel();
            hop.ForEach(h => dsPanel.Children.Add(h));
            var p = new StackPanel();
            p.Children.Add(tom);
            var hangLoc = new DockPanel();
            var nhan = new TextBlock { Text = "🔍 ", VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(nhan, Dock.Left);
            hangLoc.Children.Add(nhan);
            hangLoc.Children.Add(loc);
            p.Children.Add(hangLoc);
            p.Children.Add(new Border
            {
                BorderBrush = KieuTeams.Mau("#D0D5DD"), BorderThickness = new Thickness(1), Padding = new Thickness(4, 2, 4, 2),
                Child = new ScrollViewer { Content = dsPanel, MaxHeight = 150, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            });
            return (p, () => hop.Where(h => h.IsChecked == true).Select(h => (string)h.Tag).ToList());
        }

        private void HienSua(DonTeams d)
        {
            DauTrang("Sửa: " + d.Title, "Chi tiết đơn", () => HienChiTiet(d));
            var luoi = new Grid();
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.38, GridUnitType.Star) });
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.62, GridUnitType.Star) });
            var o = new List<(CotList Cot, object Cu, Func<object> Doc)>();
            int r = 0;
            foreach (CotList c in _cots)
            {
                luoi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var ten = new TextBlock { Text = c.TieuDe, Foreground = KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 8, 5), VerticalAlignment = VerticalAlignment.Center };
                FrameworkElement oNhap;
                if (c.SuaDuoc)
                {
                    object cu = SuaDon.GiaTriForm(c, d.Tho.TryGetValue(c.Ten, out object tho) ? tho : null);
                    var (oN, doc) = TaoONhap(c, cu);
                    oNhap = oN;
                    o.Add((c, cu, doc));
                }
                else // người / tra cứu / chữ có định dạng: chỉ xem, sửa trên Teams
                    oNhap = new TextBlock { Text = (d.TatCa.FirstOrDefault(x => x.Key == c.TieuDe).Value ?? "") + "  (sửa trên Teams)", Foreground = KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap };
                oNhap.Margin = new Thickness(0, 3, 0, 3);
                Grid.SetRow(ten, r);
                Grid.SetRow(oNhap, r);
                Grid.SetColumn(oNhap, 1);
                luoi.Children.Add(ten);
                luoi.Children.Add(oNhap);
                r++;
            }
            _noiDung.Children.Add(KieuTeams.The(luoi));

            var nut = new WrapPanel();
            nut.Children.Add(KieuTeams.Nut("💾 Lưu lên Teams", _mau, (s, e) => LuuSua(d, o)));
            nut.Children.Add(KieuTeams.Nut("Huỷ", _mau, (s, e) => HienChiTiet(d), chinh: false));
            _noiDung.Children.Add(nut);
            BaoTrangThai("Sửa xong bấm Lưu — chỉ các ô đã đổi được gửi lên Teams.");
        }

        private void LuuSua(DonTeams d, List<(CotList Cot, object Cu, Func<object> Doc)> o)
        {
            List<(CotList Cot, object Cu, object Moi)> doi;
            try { doi = o.Select(x => (x.Cot, x.Cu, Moi: x.Doc())).Where(x => !SuaDon.GiongNhau(x.Cu, x.Moi)).ToList(); }
            catch (FormatException ex) { BaoTrangThai(ex.Message); return; }
            if (doi.Count == 0) { BaoTrangThai("Chưa có ô nào thay đổi."); return; }
            string loi = doi.Select(x => SuaDon.KiemTra(x.Cot, x.Moi)).FirstOrDefault(x => x != null);
            if (loi != null) { BaoTrangThai(loi); return; }

            string ds = string.Join("\n", doi.Select(x => "• " + x.Cot.TieuDe + ": " + HienGiaTri(x.Cot, x.Cu) + "  →  " + HienGiaTri(x.Cot, x.Moi)));
            if (MessageBox.Show("Lưu " + doi.Count + " thay đổi của đơn " + d.Title + " lên Teams?\n\n" + ds, "Sửa đơn",
                                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;

            GuiSua(d, doi.Select(x => new KeyValuePair<CotList, object>(x.Cot, x.Moi)).ToList());
        }

        /// <summary>
        /// Gửi các cột đổi lên Teams (kèm eTag chống ghi đè), rồi tải lại đúng đơn đó (~0,3 s, không tải cả danh sách)
        /// và hiện lại chi tiết. Người khác vừa sửa đơn → không ghi, vẫn tải bản mới để sửa tiếp.
        /// </summary>
        private void GuiSua(DonTeams d, List<KeyValuePair<CotList, object>> doi)
        {
            string siteId = _tab[5], listId = _tab[6];
            Chay(async () =>
            {
                string body = SuaDon.Body(await GiaiMaNguoi(doi));
                BaoTrangThai("Đang lưu lên Teams…");
                string thongBao = "Đã lưu " + string.Join(", ", doi.Select(x => x.Key.TieuDe)) + " lên Teams (" + DateTime.Now.ToString("HH:mm") + ").";
                try { await GraphTeams.SuaDon(siteId, listId, d.Id, d.ETag, body, CancellationToken.None); }
                catch (DonDaDoiException ex) { thongBao = ex.Message; } // vẫn tải lại bên dưới để có bản mới
                BaoTrangThai("Đang tải lại đơn…");
                string json = await GraphTeams.TaiMotDon(siteId, listId, d.Id, CancellationToken.None);
                if (_tab?[6] != listId) return;
                var ten = _danhBa.ToDictionary(kv => kv.Key, kv => kv.Value);
                DonTeams sau = PhanTichTeams.DocDon(json, _cots, PhanTichTeams.KhopCot(_cots),
                                                    id => id != null && ten.TryGetValue(id, out string t) ? t : "#" + id).FirstOrDefault();
                if (sau == null) { VeDon(); return; }
                int vt = _don.FindIndex(x => x.Id == d.Id);
                if (vt >= 0) _don[vt] = sau; else _don.Add(sau);
                GhiDangLam();
                HienChiTiet(sau, thongBao);
            });
        }

        private static void MoNgoai(string duongDan)
        {
            try { Process.Start(new ProcessStartInfo(duongDan) { UseShellExecute = true }); }
            catch { }
        }
    }
}
