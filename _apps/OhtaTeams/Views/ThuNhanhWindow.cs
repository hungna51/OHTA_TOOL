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
using System.Windows.Media.Imaging;
using TeamsTool.Core;
using TeamsTool.Services;

namespace TeamsTool.Views
{
    /// <summary>
    /// 🧪 Thử nhanh (13/11): dán / kéo thả ảnh, PDF hoặc DXF của MỘT hình → AI đọc kỹ + chọn mẫu (bước 2, không đọc cả trang) → vẽ hình + dim
    /// ngay theo thư viện mẫu (không F-name, Khung_IN). Không cần đơn Teams — thư mục thử %LOCALAPPDATA%\OHTA_TOOL\thu_nhanh\&lt;giờ&gt; dựng như thư mục đơn.
    /// Cột trái: danh sách các lần đã thử (bấm để xem / vẽ lại, 🤖 Đọc lại bằng AI, 🗑 Xoá). Sửa thông số (chi_tiet.json) hoặc mã mẫu
    /// rồi 🔁 Vẽ lại (không tốn AI); chưa có mẫu → ✨ AI viết mẫu; máy Admin ➕ Thêm vào thư viện.
    /// </summary>
    internal sealed class ThuNhanhWindow : Window
    {
        private static readonly string[] DuoiNhan = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".pdf", ".dxf" };
        private readonly Brush _mau;
        private readonly StackPanel _ds = new StackPanel();
        private readonly TextBlock _bao = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = KieuTeams.ChuPhu, Margin = new Thickness(0, 6, 0, 6) };
        private readonly List<Button> _nutCanThu = new List<Button>();
        private readonly ListBox _lanThu = new ListBox { Margin = new Thickness(10, 10, 0, 10), MinWidth = 220 };
        private bool _dangNapDs; // đang dựng lại danh sách — chọn mục lúc này không mở lần thử
        private string _tm;
        private CancellationTokenSource _huy;

        public ThuNhanhWindow(Brush mau)
        {
            _mau = mau;
            Title = "🧪 Thử nhanh — ảnh bản vẽ → DXF";
            Width = 1100; Height = 780; MinWidth = 700; MinHeight = 480;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = KieuTeams.Nen;
            FontFamily = KieuTeams.Font;
            AllowDrop = true;

            var nut = new WrapPanel();
            nut.Children.Add(KieuTeams.Nut("📋 Dán ảnh (Ctrl+V)", _mau, (s, e) => Dan()));
            nut.Children.Add(KieuTeams.Nut("📂 Chọn tệp", _mau, (s, e) => ChonTep(), chinh: false));
            Button Them(string chu, Action bam, string goiY)
            {
                var b = KieuTeams.Nut(chu, _mau, (s, e) => bam(), chinh: false);
                b.ToolTip = goiY; b.IsEnabled = false;
                _nutCanThu.Add(b); nut.Children.Add(b);
                return b;
            }
            Them("🔁 Vẽ lại", () => Chay(VeLai), "Vẽ lại từ thông số hiện tại (sau khi sửa chi_tiet.json hoặc mã mẫu) — không gọi AI");
            Them("🤖 Đọc lại (AI)", () => Chay(async (bao, huy) => { await AiDon.DocLaiThuNhanh(_tm, bao, huy); await VeLai(bao, huy); NapDs(); }),
                 "AI đọc lại hình của lần thử này (tốn một lượt AI) rồi vẽ lại — ô bạn đã sửa trong chi_tiet.json giữ nguyên");
            Them("✎ Sửa thông số", () => MoNotepad(AiDon.FileChiTiet(_tm)), "Mở chi_tiet.json bằng Notepad: sửa bien_dang, kích thước, lo, ke_gaki, goc, khoet, dim_khach… rồi lưu và bấm 🔁 Vẽ lại");
            Them("📁 Thư mục", () => Mo(_tm), "Mở thư mục thử (ai\\ban_ve: DXF, ai\\ho_so\\TN-1: ảnh cắt, sơ đồ đỉnh)");
            Them("🗑 Xoá lần thử", XoaLanThu, "Xoá hẳn lần thử đang mở (ảnh, dữ liệu, DXF, mẫu thử chưa thêm vào thư viện)");
            nut.Children.Add(KieuTeams.Nut("📚 Thư viện mẫu", _mau, (s, e) => new ThuVienMauWindow(_tm, _mau) { Owner = this }.ShowDialog(), chinh: false));
            nut.Children.Add(KieuTeams.Nut("Huỷ", _mau, (s, e) => _huy?.Cancel(), chinh: false));

            var dau = new StackPanel { Margin = new Thickness(10, 10, 10, 0) };
            dau.Children.Add(new TextBlock
            {
                Text = "Dán ảnh chụp MỘT hình (Ctrl+V), kéo thả hoặc chọn tệp ảnh / PDF / DXF → AI đọc kỹ hình + chọn mẫu rồi vẽ hình + dim ngay theo thư viện mẫu. "
                     + "Chưa ưng: sửa thông số (✎) hoặc mã mẫu (📄) rồi 🔁 Vẽ lại — không tốn AI. Chưa có mẫu: ✨ AI viết mẫu.",
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6),
            });
            dau.Children.Add(nut);
            dau.Children.Add(_bao);
            var khung = new DockPanel();
            DockPanel.SetDock(dau, Dock.Top);
            khung.Children.Add(dau);
            khung.Children.Add(new ScrollViewer { Content = _ds, Margin = new Thickness(10, 0, 10, 10), VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            var trai = new DockPanel();
            var tieuDe = new TextBlock { Text = "Các lần đã thử", FontWeight = FontWeights.SemiBold, Margin = new Thickness(10, 10, 0, 0) };
            DockPanel.SetDock(tieuDe, Dock.Top);
            trai.Children.Add(tieuDe);
            trai.Children.Add(_lanThu);
            var luoi = new Grid();
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            luoi.Children.Add(trai);
            Grid.SetColumn(khung, 1);
            luoi.Children.Add(khung);
            Content = luoi;
            _lanThu.SelectionChanged += (s, e) =>
            {
                if (_dangNapDs || !(_lanThu.SelectedItem is ListBoxItem it) || !(it.Tag is string tm) || tm == _tm || _huy != null) return;
                _tm = tm;
                _ds.Children.Clear();
                Chay(VeLai); // vẽ lại trên máy để xem — không gọi AI
            };
            Loaded += (s, e) => NapDs();

            KeyDown += (s, e) => { if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control) { Dan(); e.Handled = true; } };
            Drop += (s, e) => { if (e.Data.GetData(DataFormats.FileDrop) is string[] f && f.Length > 0) Nhan(f[0]); };
            Closed += (s, e) => _huy?.Cancel();
            _bao.Text = "Chưa có bản vẽ.";
        }

        private void Dan()
        {
            try
            {
                if (Clipboard.ContainsFileDropList() && Clipboard.GetFileDropList().Count > 0) { Nhan(Clipboard.GetFileDropList()[0]); return; }
                BitmapSource anh = Clipboard.GetImage();
                if (anh == null) { _bao.Text = "Bộ nhớ tạm không có ảnh — chụp vùng bản vẽ (Win+Shift+S) rồi dán lại."; return; }
                string f = Path.Combine(Path.GetTempPath(), "OHTA_TOOL", "thu_nhanh_dan.png");
                Directory.CreateDirectory(Path.GetDirectoryName(f));
                using (var fs = File.Create(f)) { var en = new PngBitmapEncoder(); en.Frames.Add(BitmapFrame.Create(anh)); en.Save(fs); }
                Nhan(f);
            }
            catch (Exception ex) { _bao.Text = "Không dán được: " + ex.Message; }
        }

        private void ChonTep()
        {
            var d = new Microsoft.Win32.OpenFileDialog { Filter = "Bản vẽ (ảnh, PDF, DXF)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.pdf;*.dxf|Mọi tệp|*.*" };
            if (d.ShowDialog(this) == true) Nhan(d.FileName);
        }

        /// <summary>Tệp mới → thư mục thử mới → AI đọc → vẽ.</summary>
        private void Nhan(string f)
        {
            if (!DuoiNhan.Contains(Path.GetExtension(f).ToLowerInvariant())) { _bao.Text = "Chỉ nhận ảnh, PDF hoặc DXF: " + Path.GetFileName(f); return; }
            Chay(async (bao, huy) =>
            {
                bao("Đang chuẩn bị " + Path.GetFileName(f) + "…");
                _tm = await AiDon.TaoThuNhanh(VePng(f), huy);
                _ds.Children.Clear();
                NapDs();
                await AiDon.DocThuNhanh(_tm, bao, huy);
                bao("AI đã đọc hình — đang vẽ…");
                await VeLai(bao, huy);
                NapDs();
            });
        }

        /// <summary>Dựng lại danh sách lần thử (ảnh nhỏ + giờ + mẫu + kích thước), chọn lần đang mở.</summary>
        private void NapDs()
        {
            _dangNapDs = true;
            try
            {
                _lanThu.Items.Clear();
                foreach (string tm in AiDon.CacLanThu())
                {
                    TamAi t = null;
                    try { t = AiDon.DocChiTiet(tm)?.Tam.FirstOrDefault(); } catch (Exception) { } // tệp hỏng: vẫn liệt kê để xoá được
                    string ten = Path.GetFileName(tm);
                    DateTime luc;
                    string gio = DateTime.TryParseExact(ten, "yyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out luc)
                        ? luc.ToString("dd/MM HH:mm", System.Globalization.CultureInfo.InvariantCulture) : ten;
                    var hang = new StackPanel { Orientation = Orientation.Horizontal };
                    string anh = AiDon.TepHoSo(tm, PhongTrienKhai.Bango(AiDon.TenThuNhanh, 1), ".png");
                    if (!File.Exists(anh)) anh = Path.Combine(AiDon.ThuMucHoSo(tm), PhongTrienKhai.Bango(AiDon.TenThuNhanh, 1) + ".png"); // lần thử trước 18/11
                    if (File.Exists(anh))
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.DecodePixelHeight = 60; bmp.UriSource = new Uri(anh); bmp.EndInit();
                            hang.Children.Add(new Image { Source = bmp, Height = 44, Width = 60, Stretch = Stretch.Uniform, Margin = new Thickness(0, 2, 6, 2) });
                        }
                        catch (Exception) { } // ảnh hỏng: chỉ hiện chữ
                    var chu = new StackPanel();
                    chu.Children.Add(new TextBlock { Text = gio, FontWeight = FontWeights.SemiBold });
                    chu.Children.Add(new TextBlock
                    {
                        Text = t == null ? "(chưa đọc)" : !t.DaPhongTo ? "AI chưa đọc được" : (t.BienDang.Length > 0 ? t.BienDang : "chưa có mẫu") + (t.KichThuoc.Length > 0 ? " · " + t.KichThuoc : ""),
                        Foreground = KieuTeams.ChuPhu, FontSize = 11,
                    });
                    hang.Children.Add(chu);
                    var it = new ListBoxItem { Content = hang, Tag = tm, ToolTip = tm };
                    _lanThu.Items.Add(it);
                    if (string.Equals(tm, _tm, StringComparison.OrdinalIgnoreCase)) _lanThu.SelectedItem = it;
                }
                if (_lanThu.Items.Count == 0) _lanThu.Items.Add(new ListBoxItem { Content = "(chưa có lần thử nào)", IsEnabled = false });
            }
            finally { _dangNapDs = false; }
        }

        private void XoaLanThu()
        {
            if (_tm == null || _huy != null) return;
            if (MessageBox.Show(this, "Xoá hẳn lần thử " + Path.GetFileName(_tm) + "? (ảnh, dữ liệu, DXF, mẫu thử chưa thêm vào thư viện)", "Thử nhanh",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try { Directory.Delete(_tm, true); }
            catch (Exception ex) { _bao.Text = "Chưa xoá được (DXF đang mở trong CAD?): " + ex.Message; return; }
            _tm = null;
            _ds.Children.Clear();
            foreach (Button b in _nutCanThu) b.IsEnabled = false;
            _bao.Text = "Đã xoá lần thử.";
            NapDs();
        }

        /// <summary>Ảnh khác PNG (jpg, bmp…) → PNG tạm (AI nhận ảnh PNG); PNG / PDF / DXF giữ nguyên.</summary>
        private static string VePng(string f)
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext == ".png" || ext == ".pdf" || ext == ".dxf") return f;
            string ra = Path.Combine(Path.GetTempPath(), "OHTA_TOOL", "thu_nhanh_anh.png");
            Directory.CreateDirectory(Path.GetDirectoryName(ra));
            BitmapFrame anh = BitmapDecoder.Create(new Uri(f), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            using (var fs = File.Create(ra)) { var en = new PngBitmapEncoder(); en.Frames.Add(anh); en.Save(fs); }
            return ra;
        }

        private async Task VeLai(Action<string> bao, CancellationToken huy)
        {
            var (ct, kq, ds, ve) = await AiDon.VeThuNhanh(_tm, bao, huy);
            HienKetQua(ct, kq, ds, ve);
            int duoc = ve.Count(v => v.Loi == null && v.Tep != null);
            bao(ds.Count == 0 ? "Không có tấm nào để vẽ — xem ✎ Sửa thông số."
                : duoc == 0 ? "Chưa vẽ được hình — xem dòng đỏ dưới ảnh."
                : "Đã vẽ " + duoc + "/" + ds.Count + " tấm. Bấm ảnh để mở to, 📐 Mở DXF để mở CAD.");
        }

        private async void Chay(Func<Action<string>, CancellationToken, Task> viec)
        {
            if (_huy != null) return;
            _huy = new CancellationTokenSource();
            foreach (Button b in _nutCanThu) b.IsEnabled = false;
            void Bao(string s) => Dispatcher.Invoke(() => _bao.Text = s);
            try { await viec(Bao, _huy.Token); }
            catch (OperationCanceledException) { _bao.Text = "Đã huỷ."; }
            catch (Exception ex) { _bao.Text = "Lỗi: " + ex.Message; }
            finally
            {
                _huy = null;
                foreach (Button b in _nutCanThu) b.IsEnabled = _tm != null;
            }
        }

        private void HienKetQua(ChiTiet ct, KetQuaTrienKhai kq, List<(TamAi Tam, TamTrienKhai Tk)> ds, List<AiDon.KetQuaVe> ve)
        {
            _ds.Children.Clear();
            for (int i = 0; i < ds.Count; i++)
            {
                var (tam, tk) = ds[i];
                string ten = PhongTrienKhai.Bango(AiDon.TenThuNhanh, i + 1), bd = PhongTrienKhai.BienDangVe(tk);
                AiDon.KetQuaVe kv = ve.FirstOrDefault(v => v.Bango == ten);
                var the = new StackPanel();
                the.Children.Add(new TextBlock
                {
                    Text = ten + " · mẫu " + bd + " · " + tam.M + " t" + tam.T + " · " + tam.KichThuoc + (tam.Ten.Length > 0 ? " · " + tam.Ten : ""),
                    FontWeight = FontWeights.SemiBold, FontSize = 13,
                });
                if (tam.MoTa.Length > 0) the.Children.Add(new TextBlock { Text = tam.MoTa, TextWrapping = TextWrapping.Wrap, Foreground = KieuTeams.ChuPhu, Margin = new Thickness(0, 2, 0, 4) });
                var anh = new WrapPanel();
                ThemAnh(anh, AiDon.TepHoSo(_tm, ten, ".png"), "Ảnh cắt từ bản vẽ gửi vào");
                ThemAnh(anh, kv?.Anh, "Bản vẽ DXF phần mềm vẽ");
                ThemAnh(anh, AiDon.TepHoSo(_tm, ten, "-dinh.png"), "Sơ đồ đỉnh (cho ô goc vát / bo)");
                the.Children.Add(anh);
                if (kv?.Loi != null) the.Children.Add(Chu("✖ " + kv.Loi, "#B42318"));
                if (kv == null) the.Children.Add(Chu(tk.LamTay ? "✖ Chưa vẽ: hình làm tay (lam_tay) — không có mẫu vẽ."
                    : "✖ Chưa vẽ: chưa có kích thước trải phẳng (kich_thuoc / td_x, td_y trống) — ✎ Sửa thông số rồi 🔁 Vẽ lại.", "#B42318"));
                foreach (string s in kv?.ThieuDim ?? new List<string>()) the.Children.Add(Chu("Thiếu dim khách: " + s, "#B54708"));
                foreach (string s in kv?.CanhBao ?? new List<string>()) the.Children.Add(Chu("⚠ " + s, "#B54708"));

                var nut = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                if (kv?.Tep != null) nut.Children.Add(KieuTeams.Nut("📐 Mở DXF", _mau, (s, e) => Mo(kv.Tep), chinh: false));
                string mauThu = Path.Combine(AiDon.ThuVienThu(_tm), bd + ".py"), maMau = File.Exists(mauThu) ? mauThu : AiDon.TepMauChung(bd);
                if (maMau != null)
                    nut.Children.Add(KieuTeams.Nut("📄 Mã mẫu " + bd, _mau, (s, e) => MoNotepad(maMau), chinh: false)
                        .Also(b => b.ToolTip = (maMau == mauThu ? "Mẫu thử (chỉ thư mục thử này): " : "Mẫu thư viện chung — sửa là đổi cho MỌI đơn: ") + maMau + " — lưu rồi 🔁 Vẽ lại"));
                if (maMau == null || bd == PhongTrienKhai.ChuaCoMau)
                    nut.Children.Add(KieuTeams.Nut("✨ AI viết mẫu", _mau, (s, e) => Chay(async (bao, huy) =>
                    {
                        PhongTrienKhai.MauMoi m = await AiDon.DeXuatMau(ct, tam, tk, kq, _tm, AiDon.TenThuNhanh, bao, huy);
                        if (m.Loi != null) { bao("AI chưa viết được mẫu: " + m.Loi); return; }
                        await VeLai(bao, huy);
                    }), chinh: false).Also(b => b.ToolTip = "AI viết mẫu Python cho hình này (lưu là mẫu thử) rồi vẽ lại"));
                if (maMau == mauThu && AiDon.LaMayAdmin())
                    nut.Children.Add(KieuTeams.Nut("➕ Thêm vào thư viện", _mau, (s, e) =>
                    {
                        if (MessageBox.Show(this, "Thêm mẫu '" + bd + "' vào thư viện chung? Mọi máy dùng mẫu này sau khi cập nhật.", "Thử nhanh", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                        try { string f = AiDon.ThemVaoThuVien(_tm, bd); File.Delete(mauThu); _bao.Text = "Đã thêm mẫu vào " + f + " — commit để máy khác nhận."; }
                        catch (Exception ex) { _bao.Text = "Chưa thêm được: " + ex.Message; }
                    }, chinh: false));
                the.Children.Add(nut);
                _ds.Children.Add(new Border
                {
                    Child = the, Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 8), CornerRadius = new CornerRadius(4),
                    Background = KieuTeams.Mau(kv?.Loi != null ? "#FFF6E0" : "#FFFFFF"), BorderBrush = KieuTeams.Mau("#D0D5DD"), BorderThickness = new Thickness(1),
                });
            }
        }

        private static TextBlock Chu(string s, string mau) => new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap, Foreground = KieuTeams.Mau(mau) };

        private static void ThemAnh(Panel p, string f, string goiY)
        {
            if (f == null || !File.Exists(f)) return;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad; // nhả tệp — vẽ lại ghi đè được
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.UriSource = new Uri(f);
                bmp.EndInit();
                var anh = new Image { Source = bmp, MaxHeight = 300, MaxWidth = 480, Cursor = Cursors.Hand, ToolTip = goiY + " — bấm để mở" };
                anh.MouseLeftButtonUp += (s, e) => Mo(f);
                p.Children.Add(new Border { Child = anh, BorderBrush = KieuTeams.Mau("#D0D5DD"), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 6, 4) });
            }
            catch (Exception) { } // ảnh hỏng: bỏ qua
        }

        private static void Mo(string f)
        {
            try { Process.Start(new ProcessStartInfo(f) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Không mở được " + f + ":\n" + ex.Message, "Thử nhanh", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        /// <summary>Notepad — mở .py bằng UseShellExecute có thể CHẠY mã thay vì mở để sửa.</summary>
        private static void MoNotepad(string f)
        {
            try { Process.Start("notepad.exe", "\"" + f + "\""); }
            catch (Exception ex) { MessageBox.Show("Không mở được " + f + ":\n" + ex.Message, "Thử nhanh", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }
}
