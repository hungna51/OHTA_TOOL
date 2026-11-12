using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OhtaTool.Plugin.Core;
using TeamsTool.Services;

namespace TeamsTool.Views
{
    /// <summary>
    /// 📚 Thư viện mẫu vẽ (12/11): danh sách mẫu (thư viện chung + mẫu thử của đơn) — bấm một mẫu: mô tả, ảnh hướng dẫn (biên dạng + dim
    /// ghi tên thông số x, y, r1…), bảng thông số cần nhập (điền sẵn số ví dụ); ✏ Vẽ thử ra DXF + ảnh để kiểm mẫu với số tuỳ ý.
    /// </summary>
    internal sealed class ThuVienMauWindow : Window
    {
        private readonly string _tm;
        private readonly Brush _mau;
        private readonly ListBox _ds = new ListBox { Margin = new Thickness(0, 0, 8, 0) };
        private readonly StackPanel _ct = new StackPanel();
        private readonly TextBlock _bao = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = KieuTeams.ChuPhu, Margin = new Thickness(0, 6, 0, 0) };
        private List<AiDon.MauVe> _mauVe = new List<AiDon.MauVe>();
        private bool _dangChay;

        /// <param name="tm">Thư mục đơn (để thấy mẫu thử của đơn) — null = chỉ thư viện chung.</param>
        /// <param name="chon">Tên mẫu chọn sẵn.</param>
        public ThuVienMauWindow(string tm, Brush mau, string chon = null)
        {
            _tm = tm; _mau = mau;
            Title = "📚 Thư viện mẫu vẽ";
            Width = 1000; Height = 720; MinWidth = 700; MinHeight = 480;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var luoi = new Grid { Margin = new Thickness(10) };
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            luoi.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            luoi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            luoi.Children.Add(_ds);
            var cuon = new ScrollViewer { Content = _ct, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetColumn(cuon, 1);
            luoi.Children.Add(cuon);
            Grid.SetRow(_bao, 1); Grid.SetColumnSpan(_bao, 2);
            luoi.Children.Add(_bao);
            Content = luoi;
            _ds.SelectionChanged += (s, e) => { if (_ds.SelectedIndex >= 0 && _ds.SelectedIndex < _mauVe.Count) Hien(_mauVe[_ds.SelectedIndex]); };
            Loaded += (s, e) => Nap(chon);
        }

        private async void Nap(string chon)
        {
            _bao.Text = "Đang đọc thư viện mẫu…";
            try { _mauVe = await AiDon.DanhSachMau(_tm); }
            catch (Exception ex) { _bao.Text = "Không đọc được thư viện: " + ex.Message; return; }
            _ds.Items.Clear();
            foreach (AiDon.MauVe m in _mauVe) _ds.Items.Add(m.Ten + (m.LaMauThu ? "  (mẫu thử của đơn)" : "") + (m.Loi != null ? "  ⚠" : ""));
            if (_mauVe.Count == 0) { _bao.Text = "Không có mẫu nào — máy chưa có Python / ezdxf (bấm 📐 Vẽ DXF ở phòng 3 để cài)."; return; }
            _bao.Text = _mauVe.Count + " mẫu. Bấm một mẫu để xem ảnh hướng dẫn và thông số cần nhập.";
            int i = _mauVe.FindIndex(m => m.Ten == chon);
            _ds.SelectedIndex = i >= 0 ? i : 0;
        }

        private void Hien(AiDon.MauVe m)
        {
            _ct.Children.Clear();
            _ct.Children.Add(new TextBlock { Text = m.Ten + (m.LaMauThu ? " — mẫu thử của đơn (chưa vào thư viện chung)" : ""), FontSize = 16, FontWeight = FontWeights.SemiBold });
            _ct.Children.Add(new TextBlock { Text = m.MoTa, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 6) });
            if (m.Loi != null) _ct.Children.Add(new TextBlock { Text = "Mẫu lỗi: " + m.Loi, Foreground = KieuTeams.Mau("#B42318"), TextWrapping = TextWrapping.Wrap });
            if (m.Anh != null && File.Exists(m.Anh)) _ct.Children.Add(Anh(m.Anh, 340, "Ảnh hướng dẫn: dim ghi tên thông số (x, y, r1…) — bấm để mở"));
            else _ct.Children.Add(new TextBlock { Text = "Chưa có ảnh hướng dẫn — bấm 🖼 Tạo ảnh hướng dẫn.", Foreground = KieuTeams.Mau("#B54708") });

            // Bảng thông số: tên | ý nghĩa | mặc định | ô nhập (điền sẵn số ví dụ)
            _ct.Children.Add(new TextBlock { Text = "Thông số", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 2) });
            var luoi = new Grid();
            foreach (double w in new[] { 60.0, 0, 110, 100 })
                luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = w == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(w) });
            var o = new Dictionary<string, TextBox>();
            var ts = m.ViDu.Get("tham_so") as JsonObject ?? new JsonObject();
            int hang = 0;
            void Them(params UIElement[] cot)
            {
                luoi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int c = 0; c < cot.Length; c++) { Grid.SetRow(cot[c], hang); Grid.SetColumn(cot[c], c); luoi.Children.Add(cot[c]); }
                hang++;
            }
            Them(Chu("Tên", true), Chu("Ý nghĩa", true), Chu("Mặc định", true), Chu("Số thử", true));
            var dsTs = m.ThamSo.Count > 0 ? m.ThamSo : new List<(string, string, string)> { ("x", "Chiều dài", "tự tính"), ("y", "Chiều rộng", "tự tính") };
            foreach (var (ten, yNghia, macDinh) in dsTs)
            {
                object vd = ten == "x" || ten == "y" ? m.ViDu.Get(ten) : ts.Get(ten);
                var tb = new TextBox { Text = vd is JsonNumber n ? n.Raw : vd as string ?? "", Margin = new Thickness(0, 1, 0, 1), Padding = new Thickness(3, 1, 3, 1) };
                o[ten] = tb;
                Them(Chu(ten, true), Chu(yNghia), Chu(macDinh ?? "bắt buộc"), tb);
            }
            _ct.Children.Add(luoi);
            if (m.ThamSo.Count == 0) _ct.Children.Add(new TextBlock { Text = "⚠ Mẫu chưa khai THAM_SO — cần bổ sung để AI điền đủ thông số khi đọc.", Foreground = KieuTeams.Mau("#B54708"), Margin = new Thickness(0, 4, 0, 0) });

            var ketQua = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            var nut = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            nut.Children.Add(KieuTeams.Nut("✏ Vẽ thử", _mau, async (s, e) =>
            {
                if (_dangChay) return;
                double? So(string k) => o.TryGetValue(k, out TextBox t) ? TeamsTool.Core.PhongKiemTra.So(t.Text) : null;
                if (!(So("x") is double x) || !(So("y") is double y) || x <= 0 || y <= 0) { _bao.Text = "Nhập x, y (số > 0)."; return; }
                var thamSo = new Dictionary<string, double>();
                foreach (var kv in o.Where(kv => kv.Key != "x" && kv.Key != "y"))
                    if (So(kv.Key) is double v) thamSo[kv.Key] = v;
                _dangChay = true;
                _bao.Text = "Đang vẽ thử " + m.Ten + "…";
                try
                {
                    AiDon.KetQuaVe kq = await AiDon.VeThuMau(m, x, y, thamSo, _tm, CancellationToken.None);
                    ketQua.Children.Clear();
                    if (kq.Loi != null) { ketQua.Children.Add(Chu("Chưa vẽ được: " + kq.Loi, mau: "#B42318")); _bao.Text = "Vẽ thử lỗi."; return; }
                    if (kq.Anh != null && File.Exists(kq.Anh)) ketQua.Children.Add(Anh(kq.Anh, 340, "Bản vẽ thử — bấm để mở ảnh"));
                    string dxf = kq.Tep;
                    ketQua.Children.Add(KieuTeams.Nut("📐 Mở DXF vẽ thử", _mau, (s2, e2) => Mo(dxf), chinh: false).Also(b => b.HorizontalAlignment = HorizontalAlignment.Left));
                    if (kq.ThieuDim.Count > 0) ketQua.Children.Add(Chu("⚠ " + string.Join(", ", kq.ThieuDim), mau: "#B54708"));
                    _bao.Text = "Đã vẽ thử " + m.Ten + " (" + Path.GetFileName(dxf) + ").";
                }
                catch (Exception ex) { _bao.Text = "Vẽ thử lỗi: " + ex.Message; }
                finally { _dangChay = false; }
            }));
            nut.Children.Add(KieuTeams.Nut("🖼 Tạo ảnh hướng dẫn", _mau, async (s, e) =>
            {
                if (_dangChay) return;
                _dangChay = true;
                _bao.Text = "Đang vẽ ảnh hướng dẫn…";
                try
                {
                    string loi = await AiDon.TaoAnhHuongDan(Path.GetDirectoryName(m.Tep), CancellationToken.None);
                    _dangChay = false;
                    string ten = m.Ten;
                    Nap(ten);
                    if (loi.Length > 0) _bao.Text = "Có mẫu chưa vẽ được ảnh: " + loi;
                }
                catch (Exception ex) { _bao.Text = "Không tạo được ảnh: " + ex.Message; }
                finally { _dangChay = false; }
            }, chinh: false).Also(b => b.ToolTip = "Vẽ lại ảnh <ten>.png của các mẫu trong cùng thư mục bằng số VI_DU — dim có số trùng thông số ghi tên thông số"));
            nut.Children.Add(KieuTeams.Nut("📄 Mở mã", _mau, (s, e) => Mo(m.Tep), chinh: false));
            _ct.Children.Add(nut);
            _ct.Children.Add(ketQua);
        }

        private static TextBlock Chu(string s, bool dam = false, string mau = null) => new TextBlock
        {
            Text = s, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 6, 2), VerticalAlignment = VerticalAlignment.Center,
            FontWeight = dam ? FontWeights.SemiBold : FontWeights.Normal, Foreground = mau != null ? KieuTeams.Mau(mau) : Brushes.Black,
        };

        private static UIElement Anh(string f, double cao, string meo)
        {
            try
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // nhả tệp — vẽ lại ghi đè được
                bmp.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                bmp.UriSource = new Uri(f);
                bmp.EndInit();
                var a = new Image { Source = bmp, MaxHeight = cao, HorizontalAlignment = HorizontalAlignment.Left, Cursor = System.Windows.Input.Cursors.Hand, ToolTip = meo };
                a.MouseLeftButtonUp += (s, e) => Mo(f);
                return new Border { Child = a, BorderBrush = KieuTeams.Mau("#D0D5DD"), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
            }
            catch (Exception) { return Chu("(ảnh hỏng: " + f + ")"); }
        }

        private static void Mo(string f)
        {
            try { Process.Start(new ProcessStartInfo(f) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Không mở được " + f + ":\n" + ex.Message, "OHTA Teams", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }
}
