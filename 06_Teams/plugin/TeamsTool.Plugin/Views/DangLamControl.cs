using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TeamsTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace TeamsTool.Views
{
    /// <summary>
    /// Tab Teams trên CAD: chỉ hiện ĐƠN ĐANG LÀM, đọc từ dang_lam.json do OhtaTeams.exe ghi (CAD không gọi Microsoft Graph).
    /// Bản vẽ đang mở nằm trong thư mục đơn → đơn đó; không thì đơn 📌. Tự vẽ lại khi file đổi hoặc chuyển bản vẽ.
    /// </summary>
    public sealed class DangLamControl : UserControl
    {
        private readonly Brush _mau;
        private readonly string _thuMucModule;
        private readonly ScrollViewer _cuon = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        private readonly StackPanel _noiDung = new StackPanel { Margin = new Thickness(8) };
        private readonly DispatcherTimer _hen = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) }; // gom nhiều sự kiện file thành 1 lần vẽ
        private FileSystemWatcher _xem;
        private DocumentCollectionEventHandler _doiBanVe;
        private DangLam _dl; // lần đọc được gần nhất (file đang bị ghi thì giữ bản cũ)

        public DangLamControl(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            try { _mau = (Brush)new BrushConverter().ConvertFromString(mauNhan); }
            catch { _mau = Brushes.SteelBlue; }
            _thuMucModule = thuMucModule ?? "";
            Background = KieuTeams.Nen;
            FontFamily = KieuTeams.Font;
            FontSize = 12;

            var tieuDeKhoi = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            tieuDeKhoi.Children.Add(new TextBlock { Text = tieuDe, Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            tieuDeKhoi.Children.Add(new TextBlock { Text = moTa, Foreground = KieuTeams.Mau("#DDE3F5"), FontSize = 10.5, Margin = new Thickness(0, 1, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });

            _cuon.Content = _noiDung;
            var goc = new Grid();
            goc.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            goc.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            goc.Children.Add(new Border { Background = _mau, Padding = new Thickness(12, 10, 12, 10), Child = tieuDeKhoi });
            Grid.SetRow(_cuon, 1);
            goc.Children.Add(_cuon);
            Content = goc;

            _hen.Tick += (s, e) => { _hen.Stop(); Ve(); };
            Loaded += (s, e) => BatDau();
            Unloaded += (s, e) => Dung();
        }

        /// <summary>PaletteSet chuyển lăn chuột vào đây (DllTabLoader.CuonHo).</summary>
        public void ScrollByMouseWheel(int wheelDelta) => _cuon.ScrollToVerticalOffset(_cuon.VerticalOffset - wheelDelta / 3.0);

        private void HenVe() => Dispatcher.BeginInvoke(new Action(() => { _hen.Stop(); _hen.Start(); }));

        private void BatDau()
        {
            Dung();
            try
            {
                string f = PhanTichDangLam.FileMacDinh;
                Directory.CreateDirectory(Path.GetDirectoryName(f));
                _xem = new FileSystemWatcher(Path.GetDirectoryName(f), Path.GetFileName(f)) { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName };
                _xem.Changed += (s, e) => HenVe();
                _xem.Created += (s, e) => HenVe();
                _xem.Renamed += (s, e) => HenVe();
                _xem.EnableRaisingEvents = true;
            }
            catch { _xem = null; } // không theo dõi được: vẫn vẽ khi mở tab / đổi bản vẽ
            _doiBanVe = (s, e) => HenVe();
            CadApplication.DocumentManager.DocumentActivated += _doiBanVe;
            Ve();
        }

        private void Dung()
        {
            _xem?.Dispose();
            _xem = null;
            if (_doiBanVe != null) CadApplication.DocumentManager.DocumentActivated -= _doiBanVe;
            _doiBanVe = null;
        }

        private static string BanVeDangMo()
        {
            try { return CadApplication.DocumentManager.MdiActiveDocument?.Database.Filename ?? ""; }
            catch { return ""; }
        }

        private void Ve()
        {
            try
            {
                string f = PhanTichDangLam.FileMacDinh;
                _dl = File.Exists(f) ? PhanTichDangLam.Doc(File.ReadAllText(f)) : null;
            }
            catch (IOException) { HenVe(); } // OhtaTeams đang thay file → thử lại, tạm giữ bản cũ

            string banVe = BanVeDangMo();
            var (don, theoBanVe) = PhanTichDangLam.DonDangLam(banVe, _dl);

            _noiDung.Children.Clear();
            _noiDung.Children.Add(DongCapNhat());
            if (don == null) VeTrong();
            else VeDon(don, theoBanVe, banVe);
            _noiDung.Children.Add(HangNut(don, theoBanVe, banVe));
        }

        private UIElement DongCapNhat()
        {
            if (_dl == null)
                return Chu("Chưa có dữ liệu đơn. Mở OhtaTeams để đăng nhập và tải đơn.", KieuTeams.ChuPhu);
            bool cu = DateTime.Now - _dl.Luc > TimeSpan.FromDays(1);
            var t = Chu("Đơn cập nhật lúc " + _dl.Luc.ToString("HH:mm dd/MM/yyyy") + (cu ? " — đã cũ, mở OhtaTeams để cập nhật" : ""), cu ? KieuTeams.Mau("#7A5B00") : KieuTeams.ChuPhu);
            return cu ? new Border { Background = KieuTeams.Mau("#FFF4CC"), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(0, 0, 0, 6), Child = t } : (UIElement)t;
        }

        private void VeTrong()
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = "Chưa có đơn đang làm", FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 0, 0, 4) });
            string goiY = _dl == null ? ""
                : _dl.ThuMucGoc.Length == 0 ? "Chưa đặt Thư mục đơn: mở OhtaTeams → ⚙ → Thư mục đơn. Hoặc mở một đơn trong OhtaTeams và bấm 📌 Đang làm."
                : "Bản vẽ đang mở không nằm trong thư mục của đơn chưa xong nào (" + _dl.ThuMucGoc + "). Mở một đơn trong OhtaTeams và bấm 📌 Đang làm để hiện ở đây.";
            if (goiY.Length > 0) p.Children.Add(Chu(goiY, KieuTeams.ChuPhu));
            _noiDung.Children.Add(KieuTeams.The(p));
        }

        private void VeDon(DonTeams d, bool theoBanVe, string banVe)
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = d.Title.Length > 0 ? d.Title : "(không tên)", FontWeight = FontWeights.Bold, FontSize = 15, TextWrapping = TextWrapping.Wrap });
            p.Children.Add(Chu(theoBanVe ? "Theo bản vẽ đang mở: " + Path.GetFileName(banVe) : "📌 Đơn đã chọn trong OhtaTeams", _mau));
            string trangThai = d.TrangThai.Length > 0 ? d.TrangThai : d.StatusJp;
            if (trangThai.Length > 0)
                p.Children.Add(new Border
                {
                    Background = KieuTeams.Mau(d.DaXong ? "#E3F4EA" : "#EEF0FA"), CornerRadius = new CornerRadius(9), Padding = new Thickness(8, 1, 8, 1),
                    HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 4),
                    Child = new TextBlock { Text = trangThai, FontSize = 11, Foreground = d.DaXong ? KieuTeams.Mau("#1F7A4D") : _mau },
                });
            if (d.Han != null) p.Children.Add(Chu("Hạn hoàn thành: " + d.Han.Value.ToString("dd/MM/yyyy"), Brushes.Black));
            if (d.TrienKhai.Length > 0) p.Children.Add(Chu("Triển khai: " + d.TrienKhai, KieuTeams.ChuPhu));
            if (d.Check.Length > 0) p.Children.Add(Chu("Check: " + d.Check, KieuTeams.ChuPhu));
            _noiDung.Children.Add(KieuTeams.The(p));

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
            if (r > 0) _noiDung.Children.Add(KieuTeams.The(luoi));
        }

        private WrapPanel HangNut(DonTeams d, bool theoBanVe, string banVe)
        {
            var nut = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            if (d != null && !string.IsNullOrEmpty(d.WebUrl)) nut.Children.Add(KieuTeams.Nut("Mở trên Teams", _mau, (s, e) => MoNgoai(d.WebUrl)));
            if (d != null && _dl != null && _dl.ThuMucGoc.Length > 0)
                nut.Children.Add(KieuTeams.Nut("📁 Mở thư mục đơn", _mau, (s, e) => MoThuMucDon(d, theoBanVe, banVe), chinh: false));
            nut.Children.Add(KieuTeams.Nut("Mở OhtaTeams", _mau, (s, e) => MoUngDung(), chinh: d == null));
            return nut;
        }

        private static TextBlock Chu(string s, Brush mau) =>
            new TextBlock { Text = s, Foreground = mau, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) };

        /// <summary>Khớp theo bản vẽ: thư mục trên đường dẫn có Title. Đơn 📌: tìm thư mục con (≤ 3 cấp) có Title, chạy nền vì có thể là ổ mạng.</summary>
        private async void MoThuMucDon(DonTeams d, bool theoBanVe, string banVe)
        {
            string goc = _dl.ThuMucGoc.TrimEnd('\\', '/');
            string thuMuc = null;
            try
            {
                if (theoBanVe)
                {
                    string[] phan = banVe.Substring(goc.Length).TrimStart('\\', '/').Split('\\', '/');
                    for (int i = 0; i < phan.Length - 1 && thuMuc == null; i++)
                        if (PhanTichDangLam.TenKhop(phan[i], d.Title)) thuMuc = Path.Combine(new[] { goc }.Concat(phan.Take(i + 1)).ToArray());
                }
                else thuMuc = await Task.Run(() => TimThuMuc(goc, d.Title, 1));
            }
            catch (Exception ex) { MessageBox.Show("Không tìm được thư mục đơn: " + ex.Message, "Teams", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (thuMuc == null) MessageBox.Show("Không thấy thư mục nào có tên chứa \"" + d.Title + "\" trong " + goc + " (sâu tối đa " + PhanTichDangLam.SauToiDa + " cấp).",
                                                "Teams", MessageBoxButton.OK, MessageBoxImage.Information);
            else MoNgoai(thuMuc);
        }

        private static string TimThuMuc(string thuMuc, string title, int cap)
        {
            string[] con;
            try { con = Directory.GetDirectories(thuMuc); }
            catch (UnauthorizedAccessException) { return null; }
            string khop = con.FirstOrDefault(c => PhanTichDangLam.TenKhop(Path.GetFileName(c), title));
            if (khop != null || cap >= PhanTichDangLam.SauToiDa) return khop;
            return con.Select(c => TimThuMuc(c, title, cap + 1)).FirstOrDefault(x => x != null);
        }

        private void MoUngDung()
        {
            // shortcut: mở thêm một cửa sổ nếu OhtaTeams đang chạy; làm "chỉ một bản" khi người dùng thấy phiền
            string exe = Path.GetFullPath(Path.Combine(_thuMucModule, "..", "_apps", "OhtaTeams", "bin", "OhtaTeams.exe"));
            if (File.Exists(exe)) MoNgoai(exe);
            else MessageBox.Show("Không thấy " + exe + ".\nCập nhật OHTA TOOL để có ứng dụng OhtaTeams.", "Teams", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static void MoNgoai(string duongDan)
        {
            try { Process.Start(new ProcessStartInfo(duongDan) { UseShellExecute = true }); }
            catch { }
        }
    }
}
