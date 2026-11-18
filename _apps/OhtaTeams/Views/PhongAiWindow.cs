using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TeamsTool.Core;
using TeamsTool.Services;

namespace TeamsTool.Views
{
    /// <summary>
    /// 🤖 Phòng AI — màn hình tổng (18/11): 3 cột phòng (1 Nhận &amp; Đọc · 2 Kiểm tra &amp; Hỏi · 3 Triển khai &amp; Vẽ), mỗi cột các đơn đang ở phòng đó
    /// với trạng thái ⏳ chờ hàng / ⚙ AI đang làm / 👤 chờ người / ✓ xong (đơn không có việc thì suy từ dữ liệu đã lưu trong thư mục đơn).
    /// Bấm đơn → mở phòng AI của đơn. Dưới: nhật ký việc từng phòng + luồng suy nghĩ của AI (bật ở ô Hiện suy nghĩ).
    /// </summary>
    internal sealed class PhongAiWindow : Window
    {
        private readonly Func<IEnumerable<DonTeams>> _dsDon;
        private readonly Action<DonTeams> _moDon;
        private readonly ListBox[] _cot = { new ListBox(), new ListBox(), new ListBox() };
        private readonly TextBlock[] _tieuDe = { new TextBlock(), new TextBlock(), new TextBlock() };
        private readonly TextBox _nhatKy = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.NoWrap, FontFamily = new FontFamily("Consolas, Meiryo UI"),
                                                         FontSize = 11, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
        private readonly TextBox _suyNghi = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, FontSize = 12, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        private readonly TextBlock _tenSuyNghi = new TextBlock { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 2), TextTrimming = TextTrimming.CharacterEllipsis };
        private readonly DispatcherTimer _dongHo = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private HangAi.Viec _xemSuyNghi;              // việc đang xem suy nghĩ (null = việc đang chạy mới nhất)
        private DateTime _docTepLuc = DateTime.MinValue;
        private Dictionary<string, (int Phong, string TrangThai)> _theoTep = new Dictionary<string, (int, string)>();

        public PhongAiWindow(Brush mau, Func<IEnumerable<DonTeams>> dsDon, Action<DonTeams> moDon)
        {
            _dsDon = dsDon; _moDon = moDon;
            Title = "🤖 Phòng AI — các đơn đang làm";
            Width = 1150; Height = 760; MinWidth = 760; MinHeight = 480;
            Background = KieuTeams.Nen;
            FontFamily = KieuTeams.Font;

            var cot = new Grid { Margin = new Thickness(10, 10, 10, 6) };
            for (int i = 0; i < 3; i++)
            {
                cot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var p = new DockPanel { Margin = new Thickness(i == 0 ? 0 : 6, 0, 0, 0) };
                _tieuDe[i].FontWeight = FontWeights.SemiBold;
                _tieuDe[i].Margin = new Thickness(0, 0, 0, 4);
                DockPanel.SetDock(_tieuDe[i], Dock.Top);
                p.Children.Add(_tieuDe[i]);
                p.Children.Add(_cot[i]);
                int so = i;
                _cot[i].MouseDoubleClick += (s, e) => MoDonChon(_cot[so]);
                _cot[i].SelectionChanged += (s, e) =>
                {
                    if (_cot[so].SelectedItem is ListBoxItem it && it.Tag is Muc m && m.Viec != null) { _xemSuyNghi = m.Viec; VeSuyNghi(); }
                };
                Grid.SetColumn(p, i);
                cot.Children.Add(p);
            }

            var hienSuyNghi = new CheckBox { Content = "Hiện suy nghĩ của AI (tốn thêm token)", IsChecked = AiDon.HienSuyNghi, VerticalAlignment = VerticalAlignment.Center,
                                             ToolTip = "Bật: lượt AI sau gửi kiểu streaming, hiện dần bản tóm tắt suy nghĩ + chữ AI viết ra (key API Claude); CLI: hiện dần dòng CLI in ra. Áp từ việc bắt đầu sau khi bật." };
            hienSuyNghi.Checked += (s, e) => AiDon.HienSuyNghi = true;
            hienSuyNghi.Unchecked += (s, e) => AiDon.HienSuyNghi = false;
            var nut = new WrapPanel { Margin = new Thickness(10, 0, 10, 4) };
            nut.Children.Add(KieuTeams.Nut("📂 Mở phòng AI của đơn chọn", mau, (s, e) => MoDonChon(_cot.FirstOrDefault(c => c.SelectedItem != null)), chinh: false));
            nut.Children.Add(KieuTeams.Nut("✕ Huỷ việc của đơn chọn", mau, (s, e) =>
            {
                if (_cot.Select(c => c.SelectedItem).OfType<ListBoxItem>().FirstOrDefault()?.Tag is Muc m) HangAi.Huy(m.DonId);
            }, chinh: false));
            nut.Children.Add(hienSuyNghi);
            nut.Children.Add(new TextBlock { Text = "  Nhấp đúp một đơn để mở phòng AI của đơn. Đưa đơn vào phòng 1: chuột phải đơn / thanh đơn đã chọn → ➕ Đưa vào phòng AI.",
                                             Foreground = KieuTeams.ChuPhu, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });

            var duoi = new Grid { Margin = new Thickness(10, 0, 10, 10) };
            duoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            duoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var trai = new DockPanel();
            var tdNk = new TextBlock { Text = "Nhật ký việc của các phòng", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 2) };
            DockPanel.SetDock(tdNk, Dock.Top);
            trai.Children.Add(tdNk);
            trai.Children.Add(_nhatKy);
            var phai = new DockPanel { Margin = new Thickness(6, 0, 0, 0) };
            DockPanel.SetDock(_tenSuyNghi, Dock.Top);
            phai.Children.Add(_tenSuyNghi);
            phai.Children.Add(_suyNghi);
            Grid.SetColumn(phai, 1);
            duoi.Children.Add(trai);
            duoi.Children.Add(phai);

            var khung = new Grid();
            khung.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.1, GridUnitType.Star) });
            khung.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            khung.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(nut, 1); Grid.SetRow(duoi, 2);
            khung.Children.Add(cot); khung.Children.Add(nut); khung.Children.Add(duoi);
            Content = khung;

            _nhatKy.Text = string.Join("\n", HangAi.NhatKy.Skip(Math.Max(0, HangAi.NhatKy.Count - 400)));
            _nhatKy.ScrollToEnd();
            HangAi.ThayDoi += VeCot;
            HangAi.CoNhatKy += ThemNhatKy;
            HangAi.CoSuyNghi += CoSuyNghiMoi;
            _dongHo.Tick += (s, e) => VeCot();
            _dongHo.Start();
            Closed += (s, e) =>
            {
                HangAi.ThayDoi -= VeCot; HangAi.CoNhatKy -= ThemNhatKy; HangAi.CoSuyNghi -= CoSuyNghiMoi;
                _dongHo.Stop();
            };
            VeCot();
            VeSuyNghi();
        }

        private sealed class Muc
        {
            public string DonId = "";
            public HangAi.Viec Viec;
        }

        private void MoDonChon(ListBox ds)
        {
            if (!(ds?.SelectedItem is ListBoxItem it) || !(it.Tag is Muc m)) return;
            DonTeams d = _dsDon().FirstOrDefault(x => x.Id == m.DonId);
            if (d == null) { MessageBox.Show(this, "Đơn không còn trong danh sách đang mở — chọn lại danh sách đơn ở tab Teams.", "Phòng AI"); return; }
            _moDon(d);
            Owner?.Activate();
        }

        /// <summary>Phòng + trạng thái của đơn không có việc trong hàng: suy từ tiep_nhan / chi_tiet / bản vẽ trong thư mục đơn (đọc lại mỗi 5 giây).</summary>
        private static (int Phong, string TrangThai)? TuTep(DonTeams d)
        {
            string tm = PhongTiepNhan.ThuMucDon(CaiDatTeams.ThuMucGoc, d.Title);
            if (tm == null || !Directory.Exists(Path.Combine(tm, PhongTiepNhan.ThuMucAi))) return null;
            ChiTiet ct;
            try { ct = AiDon.DocChiTiet(tm); } catch (Exception) { return (2, "⚠ chi_tiet.json hỏng"); }
            if (ct == null) return AiDon.DocTiepNhan(tm) == null ? ((int, string)?)null : (1, "👤 đã tải tệp, chưa đọc");
            if (ct.XacNhan.Length == 0)
            {
                int cho = ct.Tam.Count(PhongPhongTo.CanBuoc2);
                return (2, cho > 0 ? "👤 xem ảnh cắt → 🔍 đọc kỹ (" + cho + " tấm)" : "👤 trả lời / ✓ Xác nhận");
            }
            string bv = AiDon.ThuMucBanVe(tm);
            bool coDxf = Directory.Exists(bv) && Directory.EnumerateFiles(bv, "*.dxf").Any();
            return (3, coDxf ? "✓ đã vẽ DXF" : "👤 chờ vẽ DXF");
        }

        private void VeCot()
        {
            List<DonTeams> ds = (_dsDon() ?? Enumerable.Empty<DonTeams>()).ToList();
            if (DateTime.Now - _docTepLuc > TimeSpan.FromSeconds(5))
            {
                _docTepLuc = DateTime.Now;
                _theoTep = new Dictionary<string, (int, string)>();
                foreach (DonTeams d in ds)
                    if (TuTep(d) is (int, string) x) _theoTep[d.Id] = x;
            }
            var chon = _cot.Select(c => (c.SelectedItem as ListBoxItem)?.Tag is Muc m ? m.DonId : null).ToArray();
            for (int p = 1; p <= 3; p++)
            {
                ListBox lb = _cot[p - 1];
                lb.Items.Clear();
                IReadOnlyList<HangAi.Viec> hang = HangAi.Hang(p);
                foreach (HangAi.Viec v in hang)
                {
                    string chu = v.BatDau != null
                        ? "⚙ " + v.DonTen + " · " + v.MoTa + " · " + HangAi.ThoiGian(DateTime.Now - v.BatDau.Value)
                        : "⏳ " + v.DonTen + " · " + v.MoTa + " · chờ thứ " + (hang.ToList().IndexOf(v) + 1);
                    lb.Items.Add(Dong(chu, new Muc { DonId = v.DonId, Viec = v }, v.BatDau != null ? "#1F4FB0" : "#5F6B7A"));
                }
                // đơn không có việc trong hàng: theo dữ liệu đã lưu (không lặp đơn đang có việc ở phòng khác)
                foreach (DonTeams d in ds.Where(d => HangAi.CuaDon(d.Id) == null && _theoTep.TryGetValue(d.Id, out var x) && x.Phong == p))
                    lb.Items.Add(Dong(_theoTep[d.Id].TrangThai.Substring(0, 2) + " " + d.Title + " · " + _theoTep[d.Id].TrangThai.Substring(2).Trim(), new Muc { DonId = d.Id },
                                      _theoTep[d.Id].TrangThai.StartsWith("✓", StringComparison.Ordinal) ? "#1F7A4D" : "#202939"));
                int dang = hang.Count(v => v.BatDau != null), cho = hang.Count - dang;
                _tieuDe[p - 1].Text = "Phòng " + HangAi.TenPhong[p - 1] + (dang > 0 ? " — ⚙ đang làm " + dang : " — rảnh") + (cho > 0 ? " · ⏳ " + cho + " chờ" : "");
                foreach (ListBoxItem it in lb.Items) if (it.Tag is Muc m && m.DonId == chon[p - 1]) { lb.SelectedItem = it; break; }
            }
        }

        private static ListBoxItem Dong(string chu, Muc m, string mau) =>
            new ListBoxItem { Content = new TextBlock { Text = chu, TextWrapping = TextWrapping.Wrap, Foreground = KieuTeams.Mau(mau) }, Tag = m, Padding = new Thickness(4, 3, 4, 3) };

        private void ThemNhatKy(HangAi.DongNhatKy d)
        {
            bool cuoi = _nhatKy.VerticalOffset + _nhatKy.ViewportHeight >= _nhatKy.ExtentHeight - 4;
            _nhatKy.AppendText((_nhatKy.Text.Length > 0 ? "\n" : "") + d);
            if (_nhatKy.Text.Length > 300000) _nhatKy.Text = _nhatKy.Text.Substring(_nhatKy.Text.Length - 200000);
            if (cuoi) _nhatKy.ScrollToEnd();
        }

        private void CoSuyNghiMoi(HangAi.Viec v)
        {
            if (_xemSuyNghi == null || (_xemSuyNghi.BatDau != null && !HangAi.Hang(_xemSuyNghi.Phong).Contains(_xemSuyNghi))) _xemSuyNghi = v; // chưa chọn / việc đã xong → theo việc mới
            if (v == _xemSuyNghi) VeSuyNghi();
        }

        private void VeSuyNghi()
        {
            HangAi.Viec v = _xemSuyNghi ?? Enumerable.Range(1, 3).SelectMany(p => HangAi.Hang(p)).FirstOrDefault(x => x.BatDau != null);
            if (v == null)
            {
                _tenSuyNghi.Text = "💭 Luồng suy nghĩ của AI";
                _suyNghi.Text = AiDon.HienSuyNghi ? "(chưa có việc AI nào đang chạy — chọn một việc ⚙ ở cột trên để xem)" : "(đang tắt — tích ô Hiện suy nghĩ của AI để xem từ việc kế tiếp)";
                return;
            }
            _tenSuyNghi.Text = "💭 " + v.DonTen + " · P" + v.Phong + " · " + v.MoTa;
            bool cuoi = _suyNghi.VerticalOffset + _suyNghi.ViewportHeight >= _suyNghi.ExtentHeight - 4;
            _suyNghi.Text = v.SuyNghi.Length > 0 ? v.SuyNghi.ToString() : (AiDon.HienSuyNghi ? "(chưa có chữ — AI đang đọc / việc này không gọi AI)" : "(đang tắt hiện suy nghĩ)");
            if (cuoi) _suyNghi.ScrollToEnd();
        }
    }
}
