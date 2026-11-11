using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly Func<string, Task<string>> _guiChuoi;

        /// <param name="dong">Bấm ✕ trên đầu khung (null = không có nút).</param>
        /// <param name="guiChuoi">Gửi chữ lên chuỗi chat của đơn (tag người triển khai + check); trả câu báo kết quả.</param>
        public AiPhongControl(DonTeams d, string listUrl, string listId, Brush mau, Action dong, Func<string, Task<string>> guiChuoi)
        {
            _d = d; _listUrl = listUrl; _listId = listId; _mau = mau; _guiChuoi = guiChuoi;
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
            KiemTra kt = AiDon.DocKiemTra(_tm);
            _than.Children.Add(Phong3(tn, ct, kt, chay));
            bool sang4 = kt != null && ct != null && kt.XacNhan.Length > 0 && kt.TuXacNhan == ct.XacNhan;
            _than.Children.Add(The("4 · Triển khai", Chu(sang4
                ? "Đã nhận " + ct.Tam.Count(t => t.Moi) + " tấm từ phòng 3 (xác nhận lúc " + kt.XacNhan + "). Trải phẳng theo bảng TV + dòng CSV có ở đợt 3."
                : "Chờ xác nhận ở phòng 3. Đợt 3: trải phẳng theo bảng TV, dòng CSV, gửi lên chuỗi đơn.")));
        }

        // ---------- Chạy ----------

        /// <summary>tu = 1: phòng 1 → 2; tu = 2: chỉ phòng 2; tu = 3: chỉ phòng 3 (kiểm tra). docLaiHet: phòng 2 đọc lại cả tệp không đổi.</summary>
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
                if (tu == 3)
                {
                    KiemTra kt = await AiDon.KiemTraDon(tn, AiDon.DocChiTiet(_tm), _tm, true, Bao, _huy.Token);
                    if (!_trangThai.Text.StartsWith("Kiểm xong, có chỗ", StringComparison.Ordinal))
                        Bao("Xong phòng 3: " + kt.VanDe.Count + " vấn đề, " + kt.DoConMo + " mục đỏ chưa xử lý. Xử lý / bỏ qua từng mục, gửi câu hỏi khách, rồi ✓ Xác nhận → phòng 4.");
                    return;
                }
                if (tu == 1 || tn == null) tn = await AiDon.TiepNhan(_d, _listUrl, _listId, _tm, Bao, _huy.Token);
                if (PhongTiepNhan.DungSauTiepNhan(tn.LoaiDon))
                    Bao("Đơn repeat hoàn toàn: dừng sau phòng 1, không gọi AI — làm theo QUY TRÌNH REPEAT. Đổi loại đơn nếu sai rồi bấm ↻ ở phòng 2.");
                else
                {
                    ChiTiet ct = await AiDon.DocBanVe(tn, _tm, docLaiHet, Bao, _huy.Token);
                    if (!_trangThai.Text.StartsWith("Có tệp chưa đọc", StringComparison.Ordinal))
                        Bao("Xong phòng 1–2: " + ct.Tam.Count + " tấm. Kiểm tra bảng, sửa ô sai (ô ⚠ là AI không chắc), rồi bấm ✓ Xác nhận bảng → phòng 3.");
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
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = chay || ct.XacNhan.Length > 0, // đã xác nhận: khoá, ↩ Bỏ xác nhận để sửa
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
            p.Children.Add(XacNhan(ct, chay));
            return The("2 · Đọc bản vẽ", p);
        }

        /// <summary>
        /// ✓ Xác nhận bảng tấm → chuyển phòng 3 (spec: người dùng xác nhận trước khi dữ liệu đi tiếp).
        /// Đã xác nhận: bảng khoá, muốn sửa phải ↩ Bỏ xác nhận; đọc lại bản vẽ cũng bỏ xác nhận.
        /// </summary>
        private UIElement XacNhan(ChiTiet ct, bool chay)
        {
            var hang = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            if (ct.XacNhan.Length > 0)
            {
                hang.Children.Add(new TextBlock { Text = "✓ Đã xác nhận lúc " + ct.XacNhan + " — dữ liệu đã chuyển sang phòng 3.", Foreground = KieuTeams.Mau("#1F7A4D"),
                                                  FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), TextWrapping = TextWrapping.Wrap });
                hang.Children.Add(KieuTeams.Nut("↩ Bỏ xác nhận", _mau, (s, e) => { ct.XacNhan = ""; AiDon.Ghi(_tm, ct); Ve(); }, chinh: false)
                    .Also(b => { b.IsEnabled = !chay; b.ToolTip = "Mở khoá bảng để sửa; sửa xong xác nhận lại"; }));
                return hang;
            }
            hang.Children.Add(KieuTeams.Nut("✓ Xác nhận bảng → phòng 3", _mau, (s, e) =>
            {
                int canhBao = ct.Tam.Count(t => t.KhongChac.Count > 0);
                if (canhBao > 0 && MessageBox.Show(canhBao + " tấm còn ô ⚠ (AI không chắc) chưa sửa.\nVẫn xác nhận và chuyển sang phòng 3?", "Xác nhận bảng tấm",
                                                   MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                ct.XacNhan = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                AiDon.Ghi(_tm, ct);
                Ve();
            }).Also(b => { b.IsEnabled = !chay && ct.Tam.Count > 0; b.ToolTip = "Kiểm tra bảng xong (sửa ô sai, ô ⚠) thì bấm để chuyển dữ liệu sang phòng 3"; }));
            return hang;
        }

        // ---------- Phòng 3 ----------

        private static readonly (string Nhom, string Ten)[] NhomVanDe =
        {
            (VanDe.Khach, "Hỏi khách"), (VanDe.NoiBo, "Hỏi nội bộ"), (VanDe.TuXuLy, "Tự xử lý theo QC"),
        };

        private Border Phong3(TiepNhan tn, ChiTiet ct, KiemTra kt, bool chay)
        {
            var p = new StackPanel();
            if (ct == null || ct.XacNhan.Length == 0)
            {
                p.Children.Add(Chu("Chờ xác nhận bảng ở phòng 2 (nút ✓ Xác nhận bảng → phòng 3)."));
                return The("3 · Kiểm tra", p);
            }
            bool daXn = kt != null && kt.XacNhan.Length > 0, cu = kt != null && kt.TuXacNhan != ct.XacNhan;
            p.Children.Add(KieuTeams.Nut(kt == null ? "▶ Kiểm tra" : "↻ Kiểm tra lại", _mau, (s, e) => Chay(3, false), chinh: kt == null || cu)
                .Also(b => { b.IsEnabled = !chay && !daXn; b.HorizontalAlignment = HorizontalAlignment.Left; b.ToolTip = "Luật QC (Lmin, R uốn, cuộn, taro, độ dày…) + AI rà bản vẽ theo QUY CHUẨN phòng 3 (~$0.06 / tệp)"; }));
            if (kt == null)
            {
                p.Children.Add(Chu("Đã nhận " + ct.Tam.Count(t => t.Moi) + " tấm từ phòng 2. Bấm ▶ Kiểm tra: tìm thiếu kích thước, mâu thuẫn, lỗ gần đường bẻ, R uốn, cuộn, taro… chia 3 nhóm."));
                return The("3 · Kiểm tra", p);
            }
            if (cu) p.Children.Add(Chu("Bảng phòng 2 đã đổi sau lần kiểm (" + kt.Luc + ") — bấm ↻ Kiểm tra lại.", true));
            p.Children.Add(Chu(string.Format(CultureInfo.InvariantCulture, "● đỏ (chặn) {0} · ● vàng (xác nhận) {1} · ℹ ghi chú {2} — còn {3} mục đỏ chưa xử lý · kiểm lúc {4} · AI ≈ ${5:0.00}",
                kt.VanDe.Count(v => v.Muc == VanDe.Do), kt.VanDe.Count(v => v.Muc == VanDe.Vang), kt.VanDe.Count(v => v.Muc == VanDe.ThongTin), kt.DoConMo, kt.Luc, kt.Tien)));

            foreach (var (nhom, ten) in NhomVanDe)
            {
                var ds = kt.VanDe.Where(v => v.Nhom == nhom).OrderBy(v => v.ConMo ? 0 : 1).ThenBy(v => v.Muc == VanDe.Do ? 0 : v.Muc == VanDe.Vang ? 1 : 2).ToList();
                if (ds.Count == 0) continue;
                p.Children.Add(new TextBlock { Text = ten + " (" + ds.Count(v => v.ConMo) + " / " + ds.Count + ")", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
                foreach (VanDe v in ds) p.Children.Add(DongVanDe(kt, v, !chay && !daXn));
            }
            if (kt.VanDe.Count == 0) p.Children.Add(Chu("Không thấy vấn đề nào."));

            // Bản nháp gửi khách + câu hỏi nội bộ
            foreach (bool lienLac in new[] { false, true })
            {
                string nhap = PhongKiemTra.BanNhap(_d.Title, kt.VanDe, lienLac);
                if (nhap.Length == 0) continue;
                p.Children.Add(new TextBlock { Text = lienLac ? "Bản nháp 連絡します (báo slit / phần thừa)" : "Bản nháp お問い合わせ (hỏi khách)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 2) });
                var o = new TextBox { Text = nhap, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 70, MaxHeight = 260,
                                      VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ToolTip = "Sửa trước khi gửi (chỉ sửa bản gửi, danh sách vấn đề giữ nguyên)" };
                p.Children.Add(o);
                bool ll = lienLac;
                p.Children.Add(KieuTeams.Nut("📤 Gửi lên chuỗi đơn", _mau, async (s, e) =>
                {
                    if (_guiChuoi == null || o.Text.Trim().Length == 0) return;
                    if (MessageBox.Show("Gửi nội dung này lên chuỗi chat của đơn " + _d.Title + " (tag người triển khai + người check)?\n\n" + o.Text,
                                        "Gửi lên chuỗi đơn", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                    var b = (Button)s;
                    b.IsEnabled = false;
                    try
                    {
                        string kq = await _guiChuoi(o.Text);
                        foreach (VanDe v in kt.VanDe.Where(x => x.Nhom == VanDe.Khach && x.ConMo && x.LienLac == ll)) v.DaGui = true;
                        AiDon.Ghi(_tm, kt);
                        Ve();
                        _trangThai.Text = kq + " Khách trả lời xong thì bấm ✓ Đã xử lý ở từng mục.";
                    }
                    catch (Exception ex) { b.IsEnabled = true; MessageBox.Show("Không gửi được:\n" + ex.Message, "Gửi lên chuỗi đơn", MessageBoxButton.OK, MessageBoxImage.Warning); }
                }, chinh: false).Also(b => { b.HorizontalAlignment = HorizontalAlignment.Left; b.IsEnabled = !chay && _guiChuoi != null; b.Margin = new Thickness(0, 4, 0, 0); }));
            }
            string noiBo = PhongKiemTra.CauHoiNoiBo(_d.Title, kt.VanDe);
            if (noiBo.Length > 0)
                p.Children.Add(KieuTeams.Nut("📋 Chép câu hỏi nội bộ", _mau, (s, e) =>
                {
                    try { Clipboard.SetText(noiBo); _trangThai.Text = "Đã chép " + kt.VanDe.Count(v => v.Nhom == VanDe.NoiBo && v.ConMo) + " câu hỏi nội bộ — dán gửi quản lý."; }
                    catch (Exception ex) { _trangThai.Text = "Không chép được: " + ex.Message; }
                }, chinh: false).Also(b => { b.HorizontalAlignment = HorizontalAlignment.Left; b.Margin = new Thickness(0, 8, 0, 0); }));

            // Xác nhận → phòng 4
            var hang = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
            if (daXn)
            {
                hang.Children.Add(new TextBlock { Text = "✓ Đã xác nhận lúc " + kt.XacNhan + " — chuyển sang phòng 4.", Foreground = KieuTeams.Mau("#1F7A4D"), FontWeight = FontWeights.SemiBold,
                                                  VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), TextWrapping = TextWrapping.Wrap });
                hang.Children.Add(KieuTeams.Nut("↩ Bỏ xác nhận", _mau, (s, e) => { kt.XacNhan = ""; AiDon.Ghi(_tm, kt); Ve(); }, chinh: false).Also(b => b.IsEnabled = !chay));
            }
            else
                hang.Children.Add(KieuTeams.Nut("✓ Xác nhận → phòng 4", _mau, (s, e) =>
                {
                    kt.XacNhan = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                    AiDon.Ghi(_tm, kt);
                    Ve();
                }).Also(b =>
                {
                    b.IsEnabled = !chay && !cu && kt.DoConMo == 0;
                    b.ToolTip = cu ? "Bảng phòng 2 đã đổi — kiểm tra lại trước" : kt.DoConMo > 0 ? "Còn " + kt.DoConMo + " mục đỏ chưa xử lý (✓ Đã xử lý hoặc Bỏ qua kèm lý do)" : "Chuyển dữ liệu sang phòng 4";
                }));
            p.Children.Add(hang);
            return The("3 · Kiểm tra", p);
        }

        /// <summary>Một vấn đề: biểu tượng, tấm, nội dung, câu JP; nút ✓ Đã xử lý / Bỏ qua (lý do) / ↩ Mở lại.</summary>
        private UIElement DongVanDe(KiemTra kt, VanDe v, bool suaDuoc)
        {
            var p = new StackPanel();
            // WPF (net48) không vẽ emoji màu → chấm tròn tô màu thật: đỏ / vàng / xanh
            var dau = new TextBlock { TextWrapping = TextWrapping.Wrap };
            dau.Inlines.Add(new System.Windows.Documents.Run(v.Muc == VanDe.ThongTin ? "ℹ " : "● ")
            {
                Foreground = KieuTeams.Mau(v.Muc == VanDe.Do ? "#D92D20" : v.Muc == VanDe.Vang ? "#F79009" : "#2E90FA"), FontWeight = FontWeights.Bold,
            });
            dau.Inlines.Add(new System.Windows.Documents.Run((v.Tam.Length > 0 ? "[" + v.Tam + "] " : "") + v.NoiDung + (v.MucQc.Length > 0 ? "  (QC " + v.MucQc + ")" : "") + (v.Nguon == "ai" ? "  · AI" : "")));
            p.Children.Add(dau);
            if (v.CauHoiJp.Length > 0) p.Children.Add(Chu("JP  " + v.CauHoiJp));

            // Ô trả lời: câu trả lời của khách / quản lý hoặc cách xử lý — lưu khi rời ô, kiểm lại gửi kèm AI, phòng 4 dùng
            p.Children.Add(new TextBlock { Text = v.Nhom == VanDe.TuXuLy ? "Cách xử lý / ghi chú:" : "Trả lời (khách / quản lý):", Foreground = KieuTeams.ChuPhu, FontSize = 11, Margin = new Thickness(0, 4, 0, 1) });
            var traLoi = new TextBox
            {
                Text = v.TraLoi, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 24, MaxHeight = 140, IsReadOnly = !suaDuoc,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(3, 2, 3, 2), Background = Brushes.White,
                ToolTip = "Gõ câu trả lời / quyết định. Lưu khi bấm ra ngoài ô. Bấm ✓ Đã xử lý khi xong.",
            };
            void LuuTraLoi()
            {
                if (traLoi.Text.Trim() == v.TraLoi) return;
                v.TraLoi = traLoi.Text.Trim();
                try { AiDon.Ghi(_tm, kt); } catch (IOException ex) { _trangThai.Text = "Không lưu được câu trả lời: " + ex.Message; }
            }
            traLoi.LostFocus += (s, e) => LuuTraLoi();
            traLoi.Unloaded += (s, e) => LuuTraLoi(); // đóng khung / vẽ lại khi con trỏ còn trong ô

            p.Children.Add(traLoi);
            var nut = new WrapPanel { Margin = new Thickness(0, 3, 0, 0) };
            if (v.ConMo)
            {
                nut.Children.Add(NutNho("✓ Đã xử lý", suaDuoc, () => { v.TrangThai = VanDe.Xong; v.LyDo = ""; }, kt));
                nut.Children.Add(NutNho("Bỏ qua…", suaDuoc, () =>
                {
                    string lyDo = Microsoft.VisualBasic.Interaction.InputBox("Lý do bỏ qua:\n" + v.NoiDung, "Bỏ qua vấn đề", "").Trim();
                    if (lyDo.Length == 0) return false; // bấm Cancel / để trống: không đổi
                    v.TrangThai = VanDe.BoQua; v.LyDo = lyDo;
                    return true;
                }, kt));
            }
            else
            {
                nut.Children.Add(new TextBlock { Text = v.TrangThai == VanDe.Xong ? "✓ Đã xử lý" : "Bỏ qua: " + v.LyDo, Foreground = KieuTeams.Mau("#1F7A4D"),
                                                 VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
                nut.Children.Add(NutNho("↩ Mở lại", suaDuoc, () => { v.TrangThai = VanDe.ChuaXong; v.LyDo = ""; }, kt));
            }
            if (v.DaGui) nut.Children.Add(new TextBlock { Text = "📤 đã gửi khách", Foreground = KieuTeams.ChuPhu, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) });
            p.Children.Add(nut);
            return new Border
            {
                Child = p, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 0, 0, 3), CornerRadius = new CornerRadius(4),
                Background = KieuTeams.Mau(!v.ConMo ? "#F2F4F7" : v.Muc == VanDe.Do ? "#FDECEC" : v.Muc == VanDe.Vang ? "#FFF6E0" : "#EEF4FF"),
                Opacity = v.ConMo ? 1 : 0.6,
            };
        }

        private Button NutNho(string chu, bool bat, Action lam, KiemTra kt) => NutNho(chu, bat, () => { lam(); return true; }, kt);

        private Button NutNho(string chu, bool bat, Func<bool> lam, KiemTra kt)
        {
            Button b = KieuTeams.Nut(chu, _mau, (s, e) => { if (lam()) { AiDon.Ghi(_tm, kt); Ve(); } }, chinh: false);
            b.IsEnabled = bat;
            b.Padding = new Thickness(8, 1, 8, 1);
            b.FontSize = 11;
            return b;
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
