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
            _cuon.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled; // chi tiết / sửa: chữ tự xuống dòng
            Tach(_trangThai);
            Tach(_caDaXong);
            Tach(_cuaToi);
            Tach(_nhanVien);
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
            _danhBa = SuaDon.DanhBa(moi, _cots);
            string dangChon = _nhanVien.SelectedItem as string;
            _nhanVien.Items.Clear();
            _nhanVien.Items.Add(MoiNhanVien);
            foreach (var kv in _danhBa) _nhanVien.Items.Add(kv.Value);
            _nhanVien.SelectedItem = dangChon != null && _nhanVien.Items.Contains(dangChon) ? dangChon : MoiNhanVien;
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
            var loc = _don.Where(d => (_caDaXong.IsChecked == true || !d.DaXong) && (_cuaToi.IsChecked != true || d.CuaToi(_toiTen))
                                      && (idNv == null || SuaDon.CoNguoi(d, _cots, idNv))).ToList();
            _noiDung.Children.Add(VeBang(loc));
            BaoTrangThai(loc.Count + " / " + _don.Count + " đơn (" + DateTime.Now.ToString("HH:mm") + ")" + _loiGhi);
            _oDanhSach = true;
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
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            foreach (CotList c in cots) g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DoRong(c)) });
            return g;
        }

        private static void DatO(Grid g, UIElement o, int cot)
        {
            Grid.SetColumn(o, cot);
            g.Children.Add(o);
        }

        private static Border Nhan(string chu, CotList c)
        {
            bool nguoi = c.Loai == "personOrGroup";
            int i = BangDon.MauNhan(chu, MauNen.Length);
            Brush vien = nguoi ? KieuTeams.Mau(PhanTichTeams.BoDau(c.TieuDe).Contains("trien khai") ? "#D0528A" : "#2E9C76") : null;
            return new Border
            {
                Background = nguoi ? Brushes.White : KieuTeams.Mau(MauNen[i]),
                BorderBrush = vien,
                BorderThickness = new Thickness(nguoi ? 1 : 0),
                CornerRadius = new CornerRadius(nguoi ? 4 : 9),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(0, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = nguoi ? "👤 " + chu : chu, FontSize = 11, Foreground = nguoi ? vien : KieuTeams.Mau(MauChu[i]),
                                        FontWeight = nguoi ? FontWeights.SemiBold : FontWeights.Normal },
            };
        }

        private static FrameworkElement O(CotList c, DonTeams d)
        {
            string gt = c.Ten == "Title" ? (d.Id == CaiDatTeams.DonChon ? "📌 " : "") + d.Title
                                         : d.TatCa.FirstOrDefault(kv => kv.Key == c.TieuDe).Value ?? "";
            if (gt.Length > 0 && (c.Loai == "personOrGroup" || c.Loai == "choice"))
            {
                var p = new StackPanel { Orientation = Orientation.Horizontal, ToolTip = gt };
                foreach (string x in BangDon.TachNhan(gt)) p.Children.Add(Nhan(x, c));
                return p;
            }
            return new TextBlock
            {
                Text = gt.Replace('\n', ' '), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center,
                ToolTip = gt.Length > 20 ? gt : null, FontSize = 11.5, FontWeight = c.Ten == "Title" ? FontWeights.SemiBold : FontWeights.Normal,
            };
        }

        private UIElement VeBang(List<DonTeams> loc)
        {
            List<CotList> cots = CotHien();
            var bang = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };

            var tieu = HangBang(cots, KieuTeams.Mau("#E9ECF2"));
            for (int i = 0; i < cots.Count; i++)
                DatO(tieu, new TextBlock { Text = cots[i].TieuDe.ToUpperInvariant(), FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = KieuTeams.ChuPhu,
                                           Margin = new Thickness(6, 7, 6, 7), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = cots[i].TieuDe }, i + 1);
            bang.Children.Add(tieu);

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
                    if (d.CuaToi(_toiTen)) DatO(h, new Border { Background = _mau }, 0); // đơn của tôi: vạch màu trái
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

        private void HienChiTiet(DonTeams d, string thongBao = null)
        {
            DauTrang(d.Title, "Danh sách đơn", VeDon);
            _noiDung.Children.Remove(_trangThai);
            if (thongBao != null) _noiDung.Children.Add(new TextBlock { Text = thongBao, Foreground = KieuTeams.Mau("#1F7A4D"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
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
            if (_cots.Any(c => c.SuaDuoc) && !string.IsNullOrEmpty(d.ETag)) nut.Children.Add(KieuTeams.Nut("✏ Sửa đơn", _mau, (s, e) => HienSua(d), chinh: false));
            if (!string.IsNullOrEmpty(d.WebUrl)) nut.Children.Add(KieuTeams.Nut("Mở trên Teams", _mau, (s, e) => MoNgoai(d.WebUrl), chinh: dangChon));
            _noiDung.Children.Add(nut);
        }

        // ---------- Sửa đơn ----------

        private string TenNguoi(string id) => _danhBa.FirstOrDefault(kv => kv.Key == id).Value ?? "#" + id;

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

            string siteId = _tab[5], listId = _tab[6];
            string body = SuaDon.Body(doi.Select(x => new KeyValuePair<CotList, object>(x.Cot, x.Moi)));
            Chay(async () =>
            {
                BaoTrangThai("Đang lưu lên Teams…");
                string thongBao = "Đã lưu " + doi.Count + " thay đổi lên Teams (" + DateTime.Now.ToString("HH:mm") + ").";
                try { await GraphTeams.SuaDon(siteId, listId, d.Id, d.ETag, body, CancellationToken.None); }
                catch (DonDaDoiException ex) { thongBao = ex.Message; } // vẫn tải lại bên dưới để có bản mới
                BaoTrangThai("Đang tải lại đơn…");
                var tho = await GraphTeams.TaiList(siteId, listId, CancellationToken.None);
                var moi = await Task.Run(() => PhanTichTeams.DocToanBo(tho));
                if (_tab?[6] != listId) return;
                DatDon(tho, moi);
                DonTeams sau = _don.FirstOrDefault(x => x.Id == d.Id);
                if (sau != null) HienChiTiet(sau, thongBao); else VeDon();
            });
        }

        private static void MoNgoai(string duongDan)
        {
            try { Process.Start(new ProcessStartInfo(duongDan) { UseShellExecute = true }); }
            catch { }
        }
    }
}
