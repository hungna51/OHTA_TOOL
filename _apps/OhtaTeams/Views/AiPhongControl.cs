using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TeamsTool.Core;
using TeamsTool.Services;

namespace TeamsTool.Views
{
    /// <summary>
    /// Phòng 🤖 AI của một đơn (spec 2026-11-11) — khung bên phải màn hình, mở bằng chuột phải vào đơn → 🤖 Mở trong phòng AI.
    /// ▶ Bắt đầu chạy phòng 1 → 2 rồi dừng ở 3. Đợt 1: phòng 1 (tiếp nhận) + phòng 2 (đọc bản vẽ); phòng 3, 4 chỉ hiện chỗ chờ.
    /// </summary>
    public sealed class AiPhongControl : UserControl
    {
        private static readonly Dictionary<string, string> TenLoaiDon = new Dictionary<string, string>
        {
            [PhongTiepNhan.Moi] = "Mới", [PhongTiepNhan.CanLienLac] = "Cần liên lạc (pitchi, kakumaru…)",
            [PhongTiepNhan.Repeat] = "Repeat hoàn toàn", [PhongTiepNhan.NuaRepeat] = "Nửa repeat nửa mới",
        };
        private static readonly Dictionary<string, string> TenLoaiTep = new Dictionary<string, string>
        {
            [PhongTiepNhan.BanVe] = "Bản vẽ", [PhongTiepNhan.Ba_D] = "3D", [PhongTiepNhan.DonHang] = "Đơn hàng / mail / ảnh", [PhongTiepNhan.Khac] = "Khác",
        };
        // Đang chạy theo đơn (id) — vẽ lại chi tiết đơn giữa chừng không chạy trùng
        private static readonly HashSet<string> DangChay = new HashSet<string>();
        public static bool DangChayDon(string id) => DangChay.Contains(id);

        private readonly DonTeams _d;
        private readonly string _listUrl, _listId;
        private readonly Brush _mau;
        private readonly StackPanel _than = new StackPanel();
        private readonly TextBlock _trangThai = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 6) };
        private CancellationTokenSource _huy;
        private string _tm;

        /// <param name="dong">Bấm ✕ trên đầu khung (null = không có nút).</param>
        public AiPhongControl(DonTeams d, string listUrl, string listId, Brush mau, Action dong)
        {
            _d = d; _listUrl = listUrl; _listId = listId; _mau = mau;
            var dau = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            if (dong != null)
            {
                var x = KieuTeams.Nut("✕", mau, (s, e) => dong(), chinh: false);
                x.ToolTip = "Đóng phòng AI của đơn này (kết quả vẫn lưu trong thư mục đơn)";
                DockPanel.SetDock(x, Dock.Right);
                dau.Children.Add(x);
            }
            dau.Children.Add(new TextBlock { Text = "🤖 Phòng AI — " + d.Title, FontWeight = FontWeights.SemiBold, FontSize = 14,
                                             VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
            var khung = new DockPanel();
            DockPanel.SetDock(dau, Dock.Top);
            khung.Children.Add(dau);
            khung.Children.Add(new ScrollViewer { Content = _than, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                  HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
            Content = khung;
            Ve();
        }

        private void Bao(string s) => Dispatcher.Invoke(() => _trangThai.Text = s);

        private void Ve()
        {
            _than.Children.Clear();
            _tm = PhongTiepNhan.ThuMucDon(CaiDatTeams.ThuMucGoc, _d.Title);
            if (_tm == null)
            {
                _than.Children.Add(Chu("Đặt Thư mục đơn ở ⚙ trước (kết quả AI lưu trong thư mục của đơn).", true));
                return;
            }
            bool chay = DangChay.Contains(_d.Id);
            var hang = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            hang.Children.Add(KieuTeams.Nut("▶ Bắt đầu", _mau, (s, e) => Chay(1, false)).Also(b => b.IsEnabled = !chay));
            if (chay) hang.Children.Add(KieuTeams.Nut("Huỷ", _mau, (s, e) => _huy?.Cancel(), chinh: false));
            hang.Children.Add(KieuTeams.Nut("📁 Thư mục đơn", _mau, (s, e) => { Directory.CreateDirectory(_tm); Mo(_tm); }, chinh: false));
            _than.Children.Add(hang);
            _than.Children.Add(_trangThai);

            TiepNhan tn = AiDon.DocTiepNhan(_tm);
            ChiTiet ct = AiDon.DocChiTiet(_tm);
            _than.Children.Add(Phong1(tn, chay));
            _than.Children.Add(Phong2(tn, ct, chay));
            _than.Children.Add(The("3 · Kiểm tra", Chu("Đợt 2: kiểm theo QUY CHUẨN, chia vấn đề 3 nhóm, soạn お問い合わせ / 連絡します.")));
            _than.Children.Add(The("4 · Triển khai", Chu("Đợt 3: trải phẳng theo bảng TV, dòng CSV, gửi lên chuỗi đơn.")));
        }

        // ---------- Chạy ----------

        /// <summary>tu = 1: phòng 1 → 2; tu = 2: chỉ phòng 2. docLaiHet: phòng 2 đọc lại cả tệp không đổi.</summary>
        private async void Chay(int tu, bool docLaiHet)
        {
            if (DangChay.Contains(_d.Id)) return;
            if (!AiDon.DaDongY)
            {
                if (MessageBox.Show("Khu AI gửi tệp bản vẽ của đơn lên Claude API (Anthropic) để đọc.\nCông ty đã cho phép gửi bản vẽ khách hàng ra dịch vụ AI chưa?\n\nĐồng ý → máy này không hỏi lại.",
                                    "AI đọc bản vẽ", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                AiDon.DaDongY = true;
            }
            if (docLaiHet && MessageBox.Show("Đọc lại mọi bản vẽ (kể cả tệp không đổi)? Mất thêm tiền AI. Ô bạn đã sửa vẫn giữ.",
                                             "Đọc lại", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            DangChay.Add(_d.Id);
            _huy = new CancellationTokenSource();
            Ve();
            try
            {
                Directory.CreateDirectory(_tm);
                TiepNhan tn = AiDon.DocTiepNhan(_tm);
                if (tu == 1 || tn == null) tn = await AiDon.TiepNhan(_d, _listUrl, _listId, _tm, Bao, _huy.Token);
                if (PhongTiepNhan.DungSauTiepNhan(tn.LoaiDon))
                    Bao("Đơn repeat hoàn toàn: dừng sau phòng 1, không gọi AI — làm theo QUY TRÌNH REPEAT. Đổi loại đơn nếu sai rồi bấm ↻ ở phòng 2.");
                else
                {
                    ChiTiet ct = await AiDon.DocBanVe(tn, _tm, docLaiHet, Bao, _huy.Token);
                    if (!_trangThai.Text.StartsWith("Có tệp chưa đọc", StringComparison.Ordinal))
                        Bao("Xong phòng 1–2: " + ct.Tam.Count + " tấm. Kiểm tra bảng, sửa ô sai (ô ⚠ là AI không chắc). Phòng 3 có ở đợt 2.");
                }
            }
            catch (OperationCanceledException) { Bao("Đã huỷ. Phần đã xong vẫn được lưu."); }
            catch (Exception ex) { Bao("Lỗi: " + ex.Message); }
            finally
            {
                DangChay.Remove(_d.Id);
                _huy = null;
                string tt = _trangThai.Text;
                Ve();
                _trangThai.Text = tt;
            }
        }

        // ---------- Phòng 1 ----------

        private Border Phong1(TiepNhan tn, bool chay)
        {
            var p = new StackPanel();
            if (tn == null)
            {
                p.Children.Add(Chu("Chưa chạy. ▶ Bắt đầu: tải tệp đính kèm + tệp trong chat về " + Path.Combine(_tm, "ai", "goc") + ", gộp bản nháp 番号.PDF."));
                return The("1 · Tiếp nhận", p);
            }
            var dong = new WrapPanel();
            dong.Children.Add(Chu("Loại đơn: "));
            var loaiDon = new ComboBox { MinWidth = 220, Margin = new Thickness(4, 0, 12, 4), IsEnabled = !chay, ItemsSource = TenLoaiDon, DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = tn.LoaiDon };
            loaiDon.SelectionChanged += (s, e) => { tn.LoaiDon = (string)loaiDon.SelectedValue; AiDon.Ghi(_tm, tn); Ve(); };
            dong.Children.Add(loaiDon);
            dong.Children.Add(Chu("Công ty đặc biệt: "));
            var congTy = new TextBox { Text = tn.CongTy, MinWidth = 160, Margin = new Thickness(4, 0, 0, 4), IsEnabled = !chay, ToolTip = "Trống = công ty thường. Đoán từ quy_chuan\\cong_ty.txt" };
            congTy.LostFocus += (s, e) => { if (congTy.Text != tn.CongTy) { tn.CongTy = congTy.Text.Trim(); AiDon.Ghi(_tm, tn); } };
            dong.Children.Add(congTy);
            p.Children.Add(dong);

            if (tn.Tep.Count == 0) p.Children.Add(Chu("Đơn không có tệp nào (đính kèm / chat)."));
            var luoi = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < tn.Tep.Count; i++)
            {
                TepDon t = tn.Tep[i];
                luoi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var ten = new TextBlock { Text = "📄 " + t.Ten, TextTrimming = TextTrimming.CharacterEllipsis, Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Bấm để mở", VerticalAlignment = VerticalAlignment.Center };
                ten.MouseLeftButtonUp += (s, e) => Mo(Path.Combine(AiDon.ThuMucTep(_tm), t.Ten));
                var nguon = Chu(t.Nguon == "chat" ? "chat" : "đính kèm");
                nguon.Margin = new Thickness(8, 0, 8, 0);
                nguon.VerticalAlignment = VerticalAlignment.Center;
                var loai = new ComboBox { MinWidth = 160, Margin = new Thickness(0, 1, 0, 1), IsEnabled = !chay, ItemsSource = TenLoaiTep, DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = t.Loai };
                loai.SelectionChanged += (s, e) =>
                {
                    t.Loai = (string)loai.SelectedValue;
                    AiDon.GopBango(tn, _tm, _d.Title, Bao);
                    AiDon.Ghi(_tm, tn);
                };
                Grid.SetRow(ten, i); Grid.SetRow(nguon, i); Grid.SetColumn(nguon, 1); Grid.SetRow(loai, i); Grid.SetColumn(loai, 2);
                luoi.Children.Add(ten); luoi.Children.Add(nguon); luoi.Children.Add(loai);
            }
            p.Children.Add(luoi);

            var nut = new WrapPanel();
            string bango = Path.Combine(_tm, PhongTiepNhan.ThuMucAi, PhongTiepNhan.TenBango(_d.Title));
            if (tn.BanNhapBango.Length > 0)
                nut.Children.Add(KieuTeams.Nut("📄 Bản nháp " + tn.BanNhapBango, _mau, (s, e) => Mo(bango), chinh: false)
                    .Also(b => b.ToolTip = "Đơn hàng trước, bản vẽ sau. Xoay trang, tô màu, ghi chú vẫn làm tay (QC I.1.A)."));
            nut.Children.Add(KieuTeams.Nut("↻ Tải lại tệp", _mau, (s, e) => Chay(1, false), chinh: false).Also(b => b.IsEnabled = !chay));
            p.Children.Add(nut);
            return The("1 · Tiếp nhận", p);
        }

        // ---------- Phòng 2 ----------

        private Border Phong2(TiepNhan tn, ChiTiet ct, bool chay)
        {
            var p = new StackPanel();
            if (ct == null)
            {
                p.Children.Add(Chu(tn == null ? "Chạy sau phòng 1." : "Chưa đọc. AI đọc các tệp loại \"Bản vẽ\" (PDF, DXF); DWG / STEP mở bằng CAD / SolidWorks."));
                if (tn != null) p.Children.Add(KieuTeams.Nut("↻ Đọc bản vẽ", _mau, (s, e) => Chay(2, false), chinh: false).Also(b => b.IsEnabled = !chay));
                return The("2 · Đọc bản vẽ", p);
            }
            bool nuaRepeat = tn?.LoaiDon == PhongTiepNhan.NuaRepeat;
            p.Children.Add(Chu(string.Format(CultureInfo.InvariantCulture, "{0} tấm · {1} tấm có ô ⚠ · AI đã dùng {2:N0} token vào / {3:N0} ra ≈ ${4:0.00}",
                ct.Tam.Count, ct.Tam.Count(t => t.KhongChac.Count > 0), ct.TokenVao, ct.TokenRa, ct.Tien)));
            if (nuaRepeat) p.Children.Add(Chu("Đơn nửa repeat: bỏ tích \"Mới\" ở tấm repeat — phòng 3, 4 chỉ làm tấm mới."));

            var g = new DataGrid
            {
                ItemsSource = ct.Tam, AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false,
                MaxHeight = 420, Margin = new Thickness(0, 6, 0, 6), HeadersVisibility = DataGridHeadersVisibility.Column,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = chay,
            };
            if (nuaRepeat) g.Columns.Add(new DataGridCheckBoxColumn { Header = "Mới", Binding = new Binding("Moi") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }, SortMemberPath = "moi" });
            Cot(g, "Tệp", "Tep", null);
            Cot(g, "Tr.", "Trang", null);
            Cot(g, "⚠", "CanhBao", null);
            Cot(g, "Số BV", "SoBanVe", "so_ban_ve");
            Cot(g, "Tên", "Ten", "ten");
            Cot(g, "M", "M", "M");
            Cot(g, "Z", "Z", "Z");
            Cot(g, "T", "T", "T");
            Cot(g, "Q", "Q", "Q");
            Cot(g, "Mặt", "Mat", "mat");
            Cot(g, "Kích thước", "KichThuoc", "kich_thuoc");
            Cot(g, "Bẻ", "TomTatCanh", null);
            Cot(g, "Lỗ", "TomTatLo", null);
            Cot(g, "Dung sai", "DungSai", "dung_sai");
            Cot(g, "Ghi chú (JP)", "GhiChuJp", "ghi_chu_jp", 220);
            Cot(g, "Ghi chú (VI)", "GhiChuVi", "ghi_chu_vi", 220);
            g.CellEditEnding += (s, e) =>
            {
                if (e.EditAction != DataGridEditAction.Commit || !(e.Row.Item is TamAi t)) return;
                string o = e.Column.SortMemberPath;
                // Lưu sau khi binding đã ghi giá trị mới vào TamAi
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (o != "moi") { t.DaSua.Add(o); t.KhongChac.Remove(o); }
                    AiDon.Ghi(_tm, ct);
                }));
            };
            p.Children.Add(g);
            p.Children.Add(Chu("Bẻ / lỗ chi tiết nằm trong ai\\chi_tiet.json (phòng 3–4 dùng). Ô đã sửa không bị lần đọc lại ghi đè."));
            var nut = new WrapPanel();
            nut.Children.Add(KieuTeams.Nut("↻ Đọc tệp mới / đã đổi", _mau, (s, e) => Chay(2, false), chinh: false).Also(b => b.IsEnabled = !chay));
            nut.Children.Add(KieuTeams.Nut("↻ Đọc lại tất cả", _mau, (s, e) => Chay(2, true), chinh: false).Also(b => b.IsEnabled = !chay));
            p.Children.Add(nut);
            return The("2 · Đọc bản vẽ", p);
        }

        /// <summary>o = tên ô JSON (null = chỉ xem) — giữ ở SortMemberPath để biết ô nào người dùng sửa.</summary>
        private static void Cot(DataGrid g, string tieuDe, string thuocTinh, string o, double rong = double.NaN)
        {
            var c = new DataGridTextColumn
            {
                Header = tieuDe, Binding = new Binding(thuocTinh) { Mode = o == null ? BindingMode.OneWay : BindingMode.TwoWay },
                IsReadOnly = o == null, SortMemberPath = o ?? thuocTinh,
            };
            if (!double.IsNaN(rong)) c.Width = rong;
            g.Columns.Add(c);
        }

        // ---------- tiện ích ----------

        private static TextBlock Chu(string s, bool loi = false) =>
            new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap, Foreground = loi ? KieuTeams.Mau("#B42318") : KieuTeams.ChuPhu, Margin = new Thickness(0, 2, 0, 2) };

        private static Border The(string tieuDe, UIElement noiDung)
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = tieuDe, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            p.Children.Add(noiDung);
            Border b = KieuTeams.The(p);
            b.Margin = new Thickness(0, 0, 0, 8);
            return b;
        }

        private static void Mo(string f)
        {
            try { Process.Start(new ProcessStartInfo(f) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Không mở được " + f + ":\n" + ex.Message, "OHTA Teams", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }

    internal static class MoRong
    {
        public static T Also<T>(this T x, Action<T> lam) { lam(x); return x; }
    }
}
