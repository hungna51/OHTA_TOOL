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
    /// Phòng 🤖 AI của một đơn (spec 2026-11-11, chia lại 12/11) — khung bên phải màn hình, mở bằng chuột phải vào đơn → 🤖 Mở trong phòng AI.
    /// 3 phòng: 1 Nhận &amp; Đọc (tải tệp, AI đọc mỗi bản vẽ một lần: thông số + tiết diện + nghi vấn) → 2 Kiểm tra &amp; Hỏi (luật tự chạy,
    /// sửa bảng, trả lời, ✓ Xác nhận; chỗ sửa → AI rút bài học, Admin duyệt) → 3 Triển khai &amp; Vẽ (trải phẳng theo bảng TV, DXF, CSV — không gọi AI).
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
        private string _khoaMauMoi = ""; // tấm bấm ✨ AI đề xuất mẫu vẽ (Chay(6))
        private bool _baiHocPdf;           // rút bài học kèm bản vẽ (Chay(7))
        private StackPanel _khungVanDe = new StackPanel(); // danh sách vấn đề phòng 2 — vẽ lại riêng sau mỗi ô sửa
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
            KiemTra kt = AiDon.DocKiemTra(_tm);
            // Chưa kiểm, hoặc kiem_tra.json của bản 4 phòng cũ (có tu_xac_nhan) → chạy luật ngay (không gọi AI)
            if (ct != null && !chay && (kt == null || kt.TuXacNhan.Length > 0))
                try { kt = AiDon.KiemLuat(ct, _tm); } catch (Exception ex) { _trangThai.Text = "Không chạy được luật kiểm tra: " + ex.Message; }
            _than.Children.Add(Phong1(tn, ct, chay));
            _than.Children.Add(Phong2(tn, ct, kt, chay));
            _than.Children.Add(Phong3(ct, chay));
        }

        // ---------- Chạy ----------

        /// <summary>
        /// tu = 1: tải tệp → đọc → kiểm luật; 2: đọc → kiểm luật; 3: kiểm lại câu trả lời (AI, chữ); 4: đọc bù tiết diện (đơn cũ);
        /// 5: vẽ DXF (ezdxf); 6: AI đề xuất mẫu vẽ cho _khoaMauMoi; 7: AI rút bài học từ chỗ người sửa. docLaiHet: đọc lại cả tệp không đổi.
        /// </summary>
        private async void Chay(int tu, bool docLaiHet)
        {
            if (DangChay.Contains(_d.Id)) return;
            if (tu != 5 && !AiDon.DaDongY) // 5 = vẽ DXF trên máy, không gửi gì ra ngoài
            {
                if (MessageBox.Show("Khu AI gửi tệp bản vẽ của đơn lên dịch vụ AI (Claude của Anthropic, hoặc Gemini của Google nếu Nguồn AI = Antigravity) để đọc.\nCông ty đã cho phép gửi bản vẽ khách hàng ra dịch vụ AI chưa?\n\nĐồng ý → máy này không hỏi lại.",
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
                switch (tu)
                {
                    case 5:
                    {
                        // Vẽ DXF bằng ezdxf: máy chưa có ezdxf thì hỏi cài trước
                        AiDon.TinhTrangPython py = await AiDon.KiemPython(lai: true);
                        if (!py.SanSang)
                        {
                            if (py.Python == null) { Bao(py.Loi); return; }
                            if (MessageBox.Show("Máy chưa có thư viện ezdxf (Python) để vẽ DXF.\nCài ezdxf + matplotlib (ảnh xem trước) bằng pip ngay? Cần mạng, khoảng 1 phút.",
                                                "Cài ezdxf", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) { Bao(py.Loi); return; }
                            py = await AiDon.CaiEzdxf(Bao, _huy.Token);
                            if (!py.SanSang) { Bao(py.Loi); return; }
                        }
                        ChiTiet ct5 = AiDon.DocChiTiet(_tm);
                        List<AiDon.KetQuaVe> kq5 = await AiDon.VeBanVe(AiDon.TinhLai(ct5, AiDon.TaoTrienKhai(ct5, _tm), _d.Title, _tm), _d.Title, _tm, Bao, _huy.Token);
                        var loi5 = kq5.Where(k => k.Loi != null).Select(k => k.Bango + ": " + k.Loi).ToList();
                        Bao("Đã vẽ " + kq5.Count(k => k.Loi == null) + " bản trải phẳng (ezdxf, môi trường moi-truong-ve.dxf) trong ai\\ban_ve. Bấm ảnh để mở DXF."
                            + (loi5.Count > 0 ? "\nChưa vẽ được:\n" + string.Join("\n", loi5) : ""));
                        return;
                    }
                    case 6:
                    {
                        ChiTiet ct6 = AiDon.DocChiTiet(_tm);
                        KetQuaTrienKhai kq6 = AiDon.TaoTrienKhai(ct6, _tm);
                        var cap = AiDon.TinhLai(ct6, kq6, _d.Title, _tm).FirstOrDefault(x => x.Tk.Khoa == _khoaMauMoi);
                        if (cap.Tk == null) { Bao("Không thấy tấm cần mẫu mới."); return; }
                        PhongTrienKhai.MauMoi m = await AiDon.DeXuatMau(ct6, cap.Tam, cap.Tk, kq6, _tm, Bao, _huy.Token);
                        Bao(m.Loi != null ? "AI đề xuất mẫu chưa dùng được: " + m.Loi
                            : "AI đã viết mẫu '" + m.Ten + "' (" + m.MoTa + ") cho đơn này — bấm 📐 Vẽ DXF để vẽ thử, 📄 Xem mã để kiểm; máy Admin bấm ➕ Thêm vào thư viện khi đúng.");
                        return;
                    }
                    case 7:
                    {
                        int n = await AiDon.RutBaiHoc(tn, AiDon.DocChiTiet(_tm), _tm, _baiHocPdf, Bao, _huy.Token);
                        Bao(n > 0 ? "AI đề xuất " + n + " bài học từ chỗ bạn sửa — xem ở cuối phòng 2 (máy Admin duyệt ➕). Phòng 3 đã sẵn sàng."
                                  : "AI không thấy bài học chung nào cần ghi từ các chỗ sửa (lỗi riêng của bản vẽ / lựa chọn của khách). Phòng 3 đã sẵn sàng.");
                        return;
                    }
                    case 4:
                    {
                        KetQuaTrienKhai kq = await AiDon.TrienKhaiDon(AiDon.DocChiTiet(_tm), AiDon.DocKiemTra(_tm), _tm, Bao, _huy.Token);
                        if (!_trangThai.Text.StartsWith("Triển khai xong, có chỗ", StringComparison.Ordinal))
                            Bao("Đã đọc bù tiết diện (" + kq.Tam.Count + " tấm). Kiểm X × Y, đổi V nếu cần, rồi 📤 Gửi lên chuỗi đơn để người check kiểm.");
                        return;
                    }
                    case 3:
                        await AiDon.KiemLaiAi(tn, AiDon.DocChiTiet(_tm), _tm, Bao, _huy.Token);
                        return;
                }
                if (tu == 1 || tn == null) tn = await AiDon.TiepNhan(_d, _listUrl, _listId, _tm, Bao, _huy.Token);
                if (PhongTiepNhan.DungSauTiepNhan(tn.LoaiDon))
                    Bao("Đơn repeat hoàn toàn: dừng sau khi tải tệp, không gọi AI — làm theo QUY TRÌNH REPEAT. Đổi loại đơn nếu sai rồi bấm ↻ Đọc bản vẽ.");
                else
                {
                    ChiTiet ct = await AiDon.DocBanVe(tn, _tm, docLaiHet, Bao, _huy.Token);
                    KiemTra kt = AiDon.KiemLuat(ct, _tm);
                    if (!_trangThai.Text.StartsWith("Có tệp chưa đọc", StringComparison.Ordinal))
                        Bao("Xong phòng 1: " + ct.Tam.Count + " tấm, " + kt.VanDe.Count + " vấn đề (" + kt.DoConMo + " đỏ). Sang phòng 2: sửa ô sai (ô ⚠ là AI không chắc), trả lời câu hỏi, rồi ✓ Xác nhận → Triển khai.");
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

        // ---------- Phòng 1: Nhận & Đọc ----------

        private const string TenPhong1 = "1 · Nhận & Đọc", TenPhong2 = "2 · Kiểm tra & Hỏi", TenPhong3 = "3 · Triển khai & Vẽ";

        private Border Phong1(TiepNhan tn, ChiTiet ct, bool chay)
        {
            var p = new StackPanel();
            if (tn == null)
            {
                p.Children.Add(Chu("Chưa chạy. ▶ Bắt đầu: tải tệp đính kèm + tệp trong chat về " + Path.Combine(_tm, "ai", "goc") + ", gộp bản nháp 番号.PDF, "
                                   + "rồi AI đọc mỗi bản vẽ MỘT lần: thông số tấm, tiết diện để trải phẳng, nghi vấn cần hỏi."));
                return The(TenPhong1, p);
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

            // Đọc bản vẽ: một lượt AI mỗi tệp
            if (PhongTiepNhan.DungSauTiepNhan(tn.LoaiDon))
                p.Children.Add(Chu("Đơn repeat hoàn toàn: không gọi AI — làm theo QUY TRÌNH REPEAT."));
            else if (ct == null)
            {
                p.Children.Add(Chu("Chưa đọc. AI đọc các tệp loại \"Bản vẽ\" (PDF, DXF); DWG / STEP mở bằng CAD / SolidWorks."));
                p.Children.Add(KieuTeams.Nut("↻ Đọc bản vẽ", _mau, (s, e) => Chay(2, false), chinh: false).Also(b => { b.IsEnabled = !chay; b.HorizontalAlignment = HorizontalAlignment.Left; }));
            }
            else
            {
                p.Children.Add(Chu(string.Format(CultureInfo.InvariantCulture, "Đã đọc: {0} tấm · {1} tấm có ô ⚠ · {2} nghi vấn AI · AI đã dùng {3:N0} token vào / {4:N0} ra ≈ ${5:0.00}",
                    ct.Tam.Count, ct.Tam.Count(t => t.KhongChac.Count > 0), ct.NghiVan.Count, ct.TokenVao, ct.TokenRa, ct.Tien)));
                var doc = new WrapPanel();
                doc.Children.Add(KieuTeams.Nut("↻ Đọc tệp mới / đã đổi", _mau, (s, e) => Chay(2, false), chinh: false).Also(b => b.IsEnabled = !chay));
                doc.Children.Add(KieuTeams.Nut("↻ Đọc lại tất cả", _mau, (s, e) => Chay(2, true), chinh: false).Also(b => b.IsEnabled = !chay));
                p.Children.Add(doc);
            }
            return The(TenPhong1, p);
        }

        // ---------- Phòng 2: Kiểm tra & Hỏi ----------

        private static readonly (string Nhom, string Ten)[] NhomVanDe =
        {
            (VanDe.Khach, "Hỏi khách"), (VanDe.NoiBo, "Hỏi nội bộ"), (VanDe.TuXuLy, "Tự xử lý theo QC"),
        };

        private Border Phong2(TiepNhan tn, ChiTiet ct, KiemTra kt, bool chay)
        {
            var p = new StackPanel();
            if (ct == null)
            {
                p.Children.Add(Chu("Chờ phòng 1 đọc bản vẽ."));
                return The(TenPhong2, p);
            }
            bool daXn = ct.XacNhan.Length > 0;
            if (tn?.LoaiDon == PhongTiepNhan.NuaRepeat) p.Children.Add(Chu("Đơn nửa repeat: bỏ tích \"Mới\" ở tấm repeat — chỉ kiểm và triển khai tấm mới."));

            var g = new DataGrid
            {
                ItemsSource = ct.Tam, AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false,
                MaxHeight = 420, Margin = new Thickness(0, 6, 0, 6), HeadersVisibility = DataGridHeadersVisibility.Column,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = chay || daXn, // đã xác nhận: khoá, ↩ Bỏ xác nhận để sửa
            };
            if (tn?.LoaiDon == PhongTiepNhan.NuaRepeat) g.Columns.Add(new DataGridCheckBoxColumn { Header = "Mới", Binding = new Binding("Moi") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }, SortMemberPath = "moi" });
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
            g.Columns.Add(new DataGridComboBoxColumn
            {
                Header = "Kiểu TD", ItemsSource = new[] { "phang", "be", "lam_tay" }, SortMemberPath = "kieu_td",
                SelectedItemBinding = new Binding("KieuTd") { Mode = BindingMode.TwoWay },
            });
            Cot(g, "Tiết diện X", "TdX", "td_x", 170);
            Cot(g, "Tiết diện Y", "TdY", "td_y", 170);
            Cot(g, "Ghi chú TD", "GhiChuTd", "ghi_chu_td", 180);
            Cot(g, "Bẻ", "TomTatCanh", null);
            Cot(g, "Lỗ", "TomTatLo", null);
            Cot(g, "Dung sai", "DungSai", "dung_sai");
            Cot(g, "Ghi chú (JP)", "GhiChuJp", "ghi_chu_jp", 220);
            Cot(g, "Ghi chú (VI)", "GhiChuVi", "ghi_chu_vi", 220);
            Cot(g, "Lý do sửa", "LyDoSua", "ly_do_sua", 180);
            g.CellEditEnding += (s, e) =>
            {
                if (e.EditAction != DataGridEditAction.Commit || !(e.Row.Item is TamAi t)) return;
                string o = e.Column.SortMemberPath;
                bool theoDoi = o != "moi" && o != "ly_do_sua";
                // Sự kiện chạy trước khi binding ghi giá trị mới → còn đọc được giá trị AI (lần sửa đầu của ô)
                if (theoDoi && !t.DaSua.Contains(o) && !t.GiaTriAi.ContainsKey(o)) t.GiaTriAi[o] = PhongDocBanVe.Lay(t, o);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (theoDoi)
                    {
                        if (t.GiaTriAi.TryGetValue(o, out string ai) && ai == PhongDocBanVe.Lay(t, o)) { t.DaSua.Remove(o); t.GiaTriAi.Remove(o); } // sửa về như AI đọc
                        else { t.DaSua.Add(o); t.KhongChac.Remove(o); }
                        if (o == "kieu_td" || o == "td_x" || o == "td_y") t.KhongChac.Remove("tiet_dien");
                    }
                    try
                    {
                        AiDon.Ghi(_tm, ct);
                        VeVanDe(tn, ct, AiDon.KiemLuat(ct, _tm), chay); // luật chạy lại ngay sau mỗi ô sửa
                    }
                    catch (Exception ex) { _trangThai.Text = "Không lưu / kiểm được: " + ex.Message; }
                }));
            };
            p.Children.Add(g);
            p.Children.Add(Chu("Tiết diện: " + PhongTrienKhai.HuongDanTd + " Ô bạn sửa được ghi lại; khi ✓ Xác nhận, AI xem mình đọc sai ở đâu để rút bài học."));
            _khungVanDe = new StackPanel();
            p.Children.Add(_khungVanDe);
            VeVanDe(tn, ct, kt, chay);
            UIElement bh = KhungBaiHoc(ct, chay);
            if (bh != null) p.Children.Add(bh);
            return The(TenPhong2, p);
        }

        /// <summary>Danh sách vấn đề (luật + nghi vấn AI), bản nháp gửi khách, câu hỏi nội bộ, ✓ Xác nhận → Triển khai. Vẽ lại riêng sau mỗi ô sửa.</summary>
        private void VeVanDe(TiepNhan tn, ChiTiet ct, KiemTra kt, bool chay)
        {
            StackPanel p = _khungVanDe;
            p.Children.Clear();
            if (kt == null) { p.Children.Add(Chu("Chưa kiểm được (xem dòng trạng thái).", true)); return; }
            bool daXn = ct.XacNhan.Length > 0, sua = !chay && !daXn;
            p.Children.Add(Chu(string.Format(CultureInfo.InvariantCulture, "● đỏ (chặn) {0} · ● vàng (xác nhận) {1} · ℹ ghi chú {2} — còn {3} mục đỏ chưa xử lý · kiểm lúc {4}{5}",
                kt.VanDe.Count(v => v.Muc == VanDe.Do), kt.VanDe.Count(v => v.Muc == VanDe.Vang), kt.VanDe.Count(v => v.Muc == VanDe.ThongTin), kt.DoConMo, kt.Luc,
                kt.Tien > 0 ? string.Format(CultureInfo.InvariantCulture, " · AI kiểm lại ≈ ${0:0.00}", kt.Tien) : "")));

            foreach (var (nhom, ten) in NhomVanDe)
            {
                var ds = kt.VanDe.Where(v => v.Nhom == nhom).OrderBy(v => v.ConMo ? 0 : 1).ThenBy(v => v.Muc == VanDe.Do ? 0 : v.Muc == VanDe.Vang ? 1 : 2).ToList();
                if (ds.Count == 0) continue;
                p.Children.Add(new TextBlock { Text = ten + " (" + ds.Count(v => v.ConMo) + " / " + ds.Count + ")", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
                foreach (VanDe v in ds) p.Children.Add(DongVanDe(kt, v, sua));
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
                        _trangThai.Text = kq + " Khách trả lời thì gõ vào ô trả lời của từng mục rồi ✓ Đã xử lý.";
                    }
                    catch (Exception ex) { b.IsEnabled = true; MessageBox.Show("Không gửi được:\n" + ex.Message, "Gửi lên chuỗi đơn", MessageBoxButton.OK, MessageBoxImage.Warning); }
                }, chinh: false).Also(b => { b.HorizontalAlignment = HorizontalAlignment.Left; b.IsEnabled = !chay && _guiChuoi != null; b.Margin = new Thickness(0, 4, 0, 0); }));
            }
            var nutKiem = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            string noiBo = PhongKiemTra.CauHoiNoiBo(_d.Title, kt.VanDe);
            if (noiBo.Length > 0)
                nutKiem.Children.Add(KieuTeams.Nut("📋 Chép câu hỏi nội bộ", _mau, (s, e) =>
                {
                    try { Clipboard.SetText(noiBo); _trangThai.Text = "Đã chép " + kt.VanDe.Count(v => v.Nhom == VanDe.NoiBo && v.ConMo) + " câu hỏi nội bộ — dán gửi quản lý."; }
                    catch (Exception ex) { _trangThai.Text = "Không chép được: " + ex.Message; }
                }, chinh: false));
            nutKiem.Children.Add(KieuTeams.Nut("🤖 Kiểm lại câu trả lời", _mau, (s, e) => Chay(3, false), chinh: false).Also(b =>
            {
                b.IsEnabled = sua;
                b.ToolTip = "Chạy lại luật + AI đọc bảng tấm và các câu trả lời (CHỈ GỬI CHỮ, không gửi bản vẽ) để tìm vấn đề mới phát sinh";
            }));
            p.Children.Add(nutKiem);

            // ✓ Xác nhận → Triển khai (một chốt duy nhất trước khi trải phẳng)
            var hang = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
            if (daXn)
            {
                hang.Children.Add(new TextBlock { Text = "✓ Đã xác nhận lúc " + ct.XacNhan + " — đã chuyển sang phòng 3.", Foreground = KieuTeams.Mau("#1F7A4D"), FontWeight = FontWeights.SemiBold,
                                                  VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), TextWrapping = TextWrapping.Wrap });
                hang.Children.Add(KieuTeams.Nut("↩ Bỏ xác nhận", _mau, (s, e) => { ct.XacNhan = ""; AiDon.Ghi(_tm, ct); Ve(); }, chinh: false)
                    .Also(b => { b.IsEnabled = !chay; b.ToolTip = "Mở khoá bảng để sửa; sửa xong xác nhận lại"; }));
                p.Children.Add(hang);
                return;
            }
            int soSua = PhongBaiHoc.CacChoSua(ct).Count;
            hang.Children.Add(KieuTeams.Nut("✓ Xác nhận → Triển khai", _mau, (s, e) =>
            {
                int canhBao = ct.Tam.Count(t => t.Moi && t.KhongChac.Count > 0);
                if (canhBao > 0 && MessageBox.Show(canhBao + " tấm còn ô ⚠ (AI không chắc) chưa sửa.\nVẫn xác nhận và chuyển sang Triển khai?", "Xác nhận",
                                                   MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                ct.XacNhan = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                AiDon.Ghi(_tm, ct);
                Ve();
                if (soSua > 0) Chay(7, false); // AI rút bài học từ chỗ người sửa (chạy nền, Triển khai dùng được ngay)
            }).Also(b =>
            {
                b.IsEnabled = !chay && ct.Tam.Count > 0 && kt.DoConMo == 0;
                b.ToolTip = kt.DoConMo > 0 ? "Còn " + kt.DoConMo + " mục đỏ chưa xử lý (✓ Đã xử lý hoặc Bỏ qua kèm lý do)" : "Chốt bảng tấm + câu trả lời, chuyển sang phòng 3 trải phẳng";
            }));
            if (soSua > 0)
            {
                var kem = new CheckBox { Content = "AI xem lại bản vẽ khi rút bài học (đắt hơn)", IsChecked = _baiHocPdf, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0),
                                         ToolTip = "Bỏ tích: chỉ gửi chữ (ô, giá trị AI đọc, giá trị đúng, lý do). Tích: gửi kèm bản vẽ để AI xem lại đúng chỗ đọc sai." };
                kem.Checked += (s, e) => _baiHocPdf = true;
                kem.Unchecked += (s, e) => _baiHocPdf = false;
                hang.Children.Add(kem);
            }
            p.Children.Add(hang);
            if (soSua > 0) p.Children.Add(Chu(soSua + " ô bạn đã sửa so với AI đọc — khi xác nhận, AI xem mình đọc sai ở đâu và đề xuất bài học (máy Admin duyệt)."));
        }

        /// <summary>Bài học AI đề xuất chờ duyệt (➕ chỉ máy Admin) + tình trạng tệp bai_hoc.md. Không có gì → null.</summary>
        private UIElement KhungBaiHoc(ChiTiet ct, bool chay)
        {
            var cho = ct.BaiHoc.Where(b => b.TrangThai == "").ToList();
            int soSua = ct.XacNhan.Length > 0 ? PhongBaiHoc.CacChoSua(ct).Count : 0;
            if (cho.Count == 0 && soSua == 0) return null;
            var p = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
            p.Children.Add(new TextBlock { Text = "Bài học AI đề xuất (" + cho.Count + " chờ duyệt)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 2) });
            bool admin = AiDon.LaMayAdmin();
            foreach (DeXuatBaiHoc d in cho)
            {
                var the = new StackPanel();
                the.Children.Add(new TextBlock { Text = d.NoiDung, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold });
                if (d.LyDo.Length > 0) the.Children.Add(Chu("AI đã sai ở: " + d.LyDo));
                if (d.GopVoi.Length > 0) the.Children.Add(Chu("Thay bài học cũ: " + d.GopVoi));
                var nut = new WrapPanel { Margin = new Thickness(0, 3, 0, 0) };
                if (admin)
                    nut.Children.Add(NutNho("➕ Thêm vào bài học", !chay, () =>
                    {
                        try { AiDon.ThemBaiHoc(ct, _tm, d); _trangThai.Text = "Đã ghi bài học vào " + AiDon.FileBaiHoc + " — lần đọc sau AI áp dụng."; }
                        catch (Exception ex) { _trangThai.Text = "Không ghi được bài học: " + ex.Message; }
                        Ve();
                        return false;
                    }, null));
                nut.Children.Add(NutNho("Bỏ", !chay, () => { d.TrangThai = "bo"; AiDon.Ghi(_tm, ct); Ve(); return false; }, null));
                the.Children.Add(nut);
                p.Children.Add(new Border { Child = the, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 0, 0, 3), CornerRadius = new CornerRadius(4), Background = KieuTeams.Mau("#EEF4FF") });
            }
            if (!admin && cho.Count > 0) p.Children.Add(Chu("Chỉ máy Admin ghi được bài học — mở đơn này trên máy Admin để duyệt."));
            if (soSua > 0)
                p.Children.Add(KieuTeams.Nut("🤖 Rút bài học từ " + soSua + " chỗ sửa", _mau, (s, e) => Chay(7, false), chinh: false)
                    .Also(b => { b.IsEnabled = !chay; b.HorizontalAlignment = HorizontalAlignment.Left; b.Margin = new Thickness(0, 4, 0, 0); b.ToolTip = "Chỗ sửa chưa gửi AI (lần trước lỗi / sửa sau khi xác nhận)"; }));
            int so = PhongBaiHoc.CacBaiHoc(AiDon.DocBaiHoc()).Count;
            p.Children.Add(Chu("Tệp bài học: " + AiDon.FileBaiHoc + " — " + so + " bài học" + (so > PhongBaiHoc.NguongNhac ? " — quá " + PhongBaiHoc.NguongNhac + ", Admin nên mở tệp dọn bớt / gộp." : ""),
                               so > PhongBaiHoc.NguongNhac));
            if (File.Exists(AiDon.FileBaiHoc))
                p.Children.Add(KieuTeams.Nut("📄 Mở bai_hoc.md", _mau, (s, e) => Mo(AiDon.FileBaiHoc), chinh: false).Also(b => b.HorizontalAlignment = HorizontalAlignment.Left));
            return p;
        }

        // ---------- Phòng 3: Triển khai & Vẽ ----------

        private Border Phong3(ChiTiet ct, bool chay)
        {
            var p = new StackPanel();
            if (ct == null || ct.XacNhan.Length == 0)
            {
                p.Children.Add(Chu("Chờ xác nhận ở phòng 2 (✓ Xác nhận → Triển khai). Trải phẳng theo bảng TV như tab Fname, không gọi AI."));
                return The(TenPhong3, p);
            }
            KetQuaTrienKhai kq;
            List<(TamAi Tam, TamTrienKhai Tk)> ds;
            try { kq = AiDon.TaoTrienKhai(ct, _tm); ds = AiDon.TinhLai(ct, kq, _d.Title, _tm); }
            catch (Exception ex) { p.Children.Add(Chu("Không tính được: " + ex.Message, true)); return The(TenPhong3, p); }

            int thieu = ct.Tam.Count(t => t.Moi && !PhongTrienKhai.CoTietDien(t));
            if (thieu > 0)
            {
                p.Children.Add(Chu(thieu + " tấm đọc bằng bản cũ, chưa có tiết diện — AI đọc bù (hoặc ↩ Bỏ xác nhận rồi nhập cột Tiết diện X / Y ở phòng 2).", true));
                p.Children.Add(KieuTeams.Nut("🤖 AI đọc bù tiết diện", _mau, (s, e) => Chay(4, false)).Also(b => { b.IsEnabled = !chay; b.HorizontalAlignment = HorizontalAlignment.Left; }));
            }
            p.Children.Add(Chu(string.Format(CultureInfo.InvariantCulture, "{0} tấm · {1} làm tay · {2} tấm có cảnh báo{3}",
                ds.Count, ds.Count(x => x.Tk.LamTay), ds.Count(x => x.Tk.CanhBao.Count > 0),
                kq.Tien > 0 ? string.Format(CultureInfo.InvariantCulture, " · AI (mẫu vẽ / đọc bù) ≈ ${0:0.00}", kq.Tien) : "")));

            string dirBv = AiDon.ThuMucBanVe(_tm);
            int stt = 0;
            foreach (var (tam, tk) in ds)
            {
                var the = new StackPanel();
                var dau = new DockPanel();
                if (tk.CacV.Count > 1)
                {
                    var v = new ComboBox { ItemsSource = tk.CacV, SelectedItem = tk.VChon, MinWidth = 70, IsEnabled = !chay, ToolTip = "Dao V cho các chỗ bẻ của tấm (đổi là tính lại)" };
                    v.SelectionChanged += (s, e) =>
                    {
                        if (!(v.SelectedItem is string chon) || chon == tk.VChon) return;
                        tk.VChon = chon;
                        AiDon.Ghi(_tm, kq);
                        Ve();
                    };
                    DockPanel.SetDock(v, Dock.Right);
                    dau.Children.Add(v);
                }
                string kichThuoc = tk.LamTay ? "LÀM TAY" : (tk.XPhang.HasValue ? PhongTrienKhai.F(tk.XPhang.Value) : "?") + " × " + (tk.YPhang.HasValue ? PhongTrienKhai.F(tk.YPhang.Value) : "?");
                dau.Children.Add(new TextBlock
                {
                    Text = tk.Ten + " — " + tam.M + " t" + tam.T + " ×" + tam.Q + " — " + kichThuoc + (tk.KyHieu.Length > 0 ? "  [" + tk.KyHieu + "]" : "") + (tk.CacV.Count == 1 ? "  " + tk.VChon : ""),
                    FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center,
                });
                the.Children.Add(dau);
                foreach (string c in tk.ChiTiet) the.Children.Add(Chu(c));
                foreach (string c in tk.CanhBao) the.Children.Add(Chu("⚠ " + c, true));
                if (tk.GhiChu.Length > 0) the.Children.Add(Chu("AI: " + tk.GhiChu));
                // Biên dạng (mẫu vẽ ezdxf): AI chọn khi đọc / tự suy; chưa có mẫu → AI đề xuất; mẫu thử của đơn → Admin thêm vào thư viện
                string bd = PhongTrienKhai.BienDangVe(tk), mauThu = Path.Combine(AiDon.ThuVienThu(_tm), bd + ".py");
                if (!tk.LamTay)
                {
                    var hangBd = new WrapPanel { Margin = new Thickness(0, 3, 0, 0) };
                    bool chuaCo = bd == PhongTrienKhai.ChuaCoMau;
                    hangBd.Children.Add(new TextBlock
                    {
                        Text = chuaCo ? "Biên dạng: chưa có mẫu vẽ — " + tk.MoTaBienDang : "Biên dạng: " + bd + (tk.BienDang.Length == 0 ? " (tự suy)" : "") + (File.Exists(mauThu) ? " — mẫu thử của đơn" : ""),
                        Foreground = chuaCo ? KieuTeams.Mau("#B42318") : KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0),
                    });
                    string khoa = tk.Khoa;
                    if (chuaCo)
                        hangBd.Children.Add(NutNho("✨ AI đề xuất mẫu vẽ", !chay, () => { _khoaMauMoi = khoa; Chay(6, false); return false; }, null));
                    if (File.Exists(mauThu))
                    {
                        hangBd.Children.Add(NutNho("📄 Xem mã", true, () => { Mo(mauThu); return false; }, null));
                        if (AiDon.LaMayAdmin())
                            hangBd.Children.Add(NutNho("➕ Thêm vào thư viện", !chay, () =>
                            {
                                if (MessageBox.Show("Thêm mẫu '" + bd + "' vào thư viện chung (ve_dxf\\thu_vien)? Mọi máy sẽ dùng mẫu này sau khi cập nhật.\nĐã vẽ thử và mở CAD kiểm chưa?",
                                                    "Thêm mẫu biên dạng", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return false;
                                try { string f = AiDon.ThemVaoThuVien(_tm, bd); File.Delete(mauThu); _trangThai.Text = "Đã thêm mẫu vào " + f + " — commit để máy khác nhận."; }
                                catch (Exception ex) { _trangThai.Text = "Không thêm được: " + ex.Message; }
                                Ve();
                                return false;
                            }, null));
                    }
                    the.Children.Add(hangBd);
                }

                // Bản vẽ ezdxf: tệp mới nhất của bango (kể cả "<bango> (2).dxf" khi bản trước đã sửa tay), ảnh xem trước, bấm để mở
                string ma = PhongTrienKhai.Bango(_d.Title, ++stt);
                string dxf = Directory.Exists(dirBv)
                    ? Directory.GetFiles(dirBv, ma + "*.dxf").Where(f => Path.GetFileNameWithoutExtension(f) == ma || Path.GetFileNameWithoutExtension(f).StartsWith(ma + " (", StringComparison.Ordinal))
                               .OrderByDescending(File.GetLastWriteTime).FirstOrDefault()
                    : null;
                string png = dxf == null ? null : Path.ChangeExtension(dxf, ".png");
                if (dxf != null)
                {
                    var link = new TextBlock { Text = "📐 " + Path.GetFileName(dxf) + (File.GetLastWriteTime(dxf) < File.GetLastWriteTime(AiDon.FileTrienKhai(_tm)) ? "  (cũ hơn kết quả triển khai — vẽ lại)" : ""),
                                               Foreground = _mau, Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Mở DXF", Margin = new Thickness(0, 4, 0, 2) };
                    link.MouseLeftButtonUp += (s, e) => Mo(dxf);
                    the.Children.Add(link);
                    if (File.Exists(png))
                        try
                        {
                            var bmp = new System.Windows.Media.Imaging.BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // đọc xong nhả file — vẽ lại ghi đè được
                            bmp.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                            bmp.UriSource = new Uri(png);
                            bmp.EndInit();
                            var anh = new Image { Source = bmp, MaxHeight = 180, HorizontalAlignment = HorizontalAlignment.Left, Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Bấm để mở DXF" };
                            anh.MouseLeftButtonUp += (s, e) => Mo(dxf);
                            the.Children.Add(anh);
                        }
                        catch (Exception) { } // ảnh hỏng: vẫn còn link DXF
                }
                p.Children.Add(new Border
                {
                    Child = the, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 4, 0, 0), CornerRadius = new CornerRadius(4),
                    Background = KieuTeams.Mau(tk.LamTay || tk.CanhBao.Count > 0 ? "#FFF6E0" : "#F2F4F7"),
                });
            }

            string tomTat = PhongTrienKhai.TomTat(_d.Title, ds);
            var nut = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            nut.Children.Add(KieuTeams.Nut("📐 Vẽ DXF", _mau, (s, e) => Chay(5, false), chinh: false).Also(b =>
            {
                b.IsEnabled = !chay && ds.Any(x => !x.Tk.LamTay && x.Tk.XPhang.HasValue && x.Tk.YPhang.HasValue);
                b.ToolTip = "Vẽ trải phẳng bằng ezdxf trên môi trường moi-truong-ve.dxf theo mẫu biên dạng: viền 0-KAKOU, đường bẻ DASHED, dim, F-name, Khung_IN → ai\\ban_ve\\<bango>.dxf. "
                            + "Tệp đã sửa trên CAD không bị ghi đè. Chưa có lỗ — bổ sung trên CAD.";
            }));
            nut.Children.Add(KieuTeams.Nut("📄 Mở CSV", _mau, (s, e) => Mo(AiDon.FileCsv(_tm)), chinh: false).Also(b => b.ToolTip = "ai\\trien_khai.csv — bango tạm <mã>-<số>, GRO đánh bango thật trên CAD"));
            nut.Children.Add(KieuTeams.Nut("📋 Chép tóm tắt", _mau, (s, e) =>
            {
                try { Clipboard.SetText(tomTat); _trangThai.Text = "Đã chép bảng tóm tắt triển khai."; }
                catch (Exception ex) { _trangThai.Text = "Không chép được: " + ex.Message; }
            }, chinh: false));
            nut.Children.Add(KieuTeams.Nut("📤 Gửi lên chuỗi đơn", _mau, async (s, e) =>
            {
                if (_guiChuoi == null) return;
                if (MessageBox.Show("Gửi bảng triển khai lên chuỗi chat của đơn " + _d.Title + " (tag người triển khai + người check) để kiểm?\n\n" + tomTat,
                                    "Gửi lên chuỗi đơn", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                var b = (Button)s;
                b.IsEnabled = false;
                try { _trangThai.Text = await _guiChuoi(tomTat); }
                catch (Exception ex) { MessageBox.Show("Không gửi được:\n" + ex.Message, "Gửi lên chuỗi đơn", MessageBoxButton.OK, MessageBoxImage.Warning); }
                finally { b.IsEnabled = true; }
            }).Also(b => b.IsEnabled = !chay && _guiChuoi != null && ds.Count > 0));
            p.Children.Add(nut);
            return The(TenPhong3, p);
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
