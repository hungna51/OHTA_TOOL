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
                try { kt = AiDon.KiemLuat(ct, _tm, _d.Title); } catch (Exception ex) { _trangThai.Text = "Không chạy được luật kiểm tra: " + ex.Message; }
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
                        if (!py.SanSang || !py.Pymupdf)
                        {
                            if (py.Python == null) { Bao(py.Loi); return; }
                            string thieu = py.SanSang ? "PyMuPDF (cắt ảnh tấm từ PDF)" : "ezdxf (vẽ DXF)";
                            bool cai = MessageBox.Show("Máy chưa có " + thieu + ".\nCài ezdxf + matplotlib + PyMuPDF bằng pip ngay? Cần mạng, khoảng 1 phút.",
                                                       "Cài thư viện Python", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
                            if (cai) py = await AiDon.CaiEzdxf(Bao, _huy.Token);
                            if (!py.SanSang) { Bao(py.Loi); return; }
                        }
                        ChiTiet ct5 = AiDon.DocChiTiet(_tm);
                        await AiDon.CatAnh(ct5, _tm, _d.Title, _huy.Token); // ảnh tấm còn thiếu (máy vừa cài PyMuPDF)
                        List<AiDon.KetQuaVe> kq5 = await AiDon.VeBanVe(AiDon.TinhLai(ct5, AiDon.TaoTrienKhai(ct5, _tm), _d.Title, _tm), _d.Title, _tm, Bao, _huy.Token);
                        var loi5 = kq5.Where(k => k.Loi != null).Select(k => k.Bango + ": " + k.Loi).ToList();
                        var thieu5 = kq5.Where(k => k.ThieuDim.Count > 0).Select(k => k.Bango + ": " + string.Join(", ", k.ThieuDim)).ToList();
                        Bao("Đã vẽ " + kq5.Count(k => k.Loi == null) + " bản trải phẳng (ezdxf, môi trường moi-truong-ve.dxf) trong ai\\ban_ve. Bấm ảnh để mở DXF."
                            + (thieu5.Count > 0 ? "\n⚠ Còn thiếu dim của khách (bổ sung trên CAD):\n" + string.Join("\n", thieu5) : "")
                            + (loi5.Count > 0 ? "\nChưa vẽ được:\n" + string.Join("\n", loi5) : ""));
                        return;
                    }
                    case 6:
                    {
                        ChiTiet ct6 = AiDon.DocChiTiet(_tm);
                        KetQuaTrienKhai kq6 = AiDon.TaoTrienKhai(ct6, _tm);
                        var cap = AiDon.TinhLai(ct6, kq6, _d.Title, _tm).FirstOrDefault(x => x.Tk.Khoa == _khoaMauMoi);
                        if (cap.Tk == null) { Bao("Không thấy tấm cần mẫu mới."); return; }
                        _thuVien = null; // nạp lại để có mẫu thử mới
                        PhongTrienKhai.MauMoi m = await AiDon.DeXuatMau(ct6, cap.Tam, cap.Tk, kq6, _tm, _d.Title, Bao, _huy.Token);
                        Bao(m.Loi != null ? "AI đề xuất mẫu chưa dùng được: " + m.Loi
                            : "AI đã viết mẫu '" + m.Ten + "' (" + m.MoTa + ") cho đơn này — sang phòng 3 bấm 📐 Vẽ DXF để vẽ thử, 📄 Xem mã để kiểm; máy Admin bấm ➕ Thêm vào thư viện khi đúng.");
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
                        await AiDon.KiemLaiAi(tn, AiDon.DocChiTiet(_tm), _tm, _d.Title, Bao, _huy.Token);
                        return;
                }
                if (tu == 1 || tn == null) tn = await AiDon.TiepNhan(_d, _listUrl, _listId, _tm, Bao, _huy.Token);
                if (PhongTiepNhan.DungSauTiepNhan(tn.LoaiDon))
                    Bao("Đơn repeat hoàn toàn: dừng sau khi tải tệp, không gọi AI — làm theo QUY TRÌNH REPEAT. Đổi loại đơn nếu sai rồi bấm ↻ Đọc bản vẽ.");
                else
                {
                    ChiTiet ct = await AiDon.DocBanVe(tn, _tm, docLaiHet, Bao, _huy.Token);
                    string anh = await AiDon.CatAnh(ct, _tm, _d.Title, _huy.Token); // ảnh tấm cho phòng 2 + hồ sơ
                    KiemTra kt = AiDon.KiemLuat(ct, _tm, _d.Title);
                    if (!_trangThai.Text.StartsWith("Có tệp chưa đọc", StringComparison.Ordinal))
                        Bao("Xong phòng 1: " + ct.Tam.Count + " tấm, " + kt.VanDe.Count + " vấn đề (" + kt.DoConMo + " đỏ). Sang phòng 2: trả lời câu hỏi từng tấm, ✎ sửa thông số nếu sai, rồi ✓ Xác nhận → Triển khai."
                            + (anh != null ? "\n" + anh : ""));
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

        private readonly HashSet<string> _dongTam = new HashSet<string>(); // khoá tấm người dùng đã gập (mặc định mở)
        private string _suaTam = "";                                       // khoá tấm đang mở khung ✎ Sửa thông số

        /// <summary>
        /// Phòng 2 (12/11): mỗi bản vẽ một mục, trong đó mỗi tấm một mục tên &lt;đơn&gt;-n: M · T · Q, ảnh cắt từ bản vẽ, ghi chú đặc biệt,
        /// câu hỏi của tấm + ô trả lời; ✎ Sửa thông số mở khung sửa; 📄 Hồ sơ mở ai\ho_so\&lt;đơn&gt;-n.md. Mục "Chung cả đơn": ghi chú chung + câu hỏi không thuộc tấm nào.
        /// </summary>
        private Border Phong2(TiepNhan tn, ChiTiet ct, KiemTra kt, bool chay)
        {
            var p = new StackPanel();
            if (ct == null)
            {
                p.Children.Add(Chu("Chờ phòng 1 đọc bản vẽ."));
                return The(TenPhong2, p);
            }
            bool sua = !chay && ct.XacNhan.Length == 0, nuaRepeat = tn?.LoaiDon == PhongTiepNhan.NuaRepeat;
            NapThuVien(); // mô tả mẫu vẽ + ô chọn mẫu (nạp nền một lần, xong thì vẽ lại)
            if (nuaRepeat) p.Children.Add(Chu("Đơn nửa repeat: bỏ tích \"Mới\" ở tấm repeat — chỉ kiểm và triển khai tấm mới (tấm mới mới được đánh số)."));
            var danhSo = PhongHoSo.DanhSo(ct, _d.Title).ToDictionary(x => x.Tam, x => x.Ten);
            var vanDe = kt?.VanDe ?? new List<VanDe>();
            foreach (var nhom in ct.Tam.GroupBy(t => t.Tep))
            {
                var tep = new StackPanel { Margin = new Thickness(12, 2, 0, 4) };
                foreach (TamAi t in nhom)
                    tep.Children.Add(TheTam(ct, kt, t, danhSo.TryGetValue(t, out string ten) ? ten : "", vanDe.Where(v => PhongHoSo.CuaTam(v, t, ct)).ToList(), sua, nuaRepeat));
                p.Children.Add(new Expander { Header = new TextBlock { Text = "📄 " + nhom.Key + " — " + nhom.Count() + " tấm", FontWeight = FontWeights.SemiBold }, Content = tep, IsExpanded = true, Margin = new Thickness(0, 4, 0, 0) });
            }

            // Chung cả đơn: ghi chú chung của khách + câu hỏi không thuộc tấm nào
            var chung = vanDe.Where(v => !ct.Tam.Any(t => t.Moi && PhongHoSo.CuaTam(v, t, ct)))
                             .OrderBy(v => v.ConMo ? 0 : 1).ThenBy(v => v.Muc == VanDe.Do ? 0 : v.Muc == VanDe.Vang ? 1 : 2).ToList();
            var ghi = ct.GhiChuDon.Where(kv => kv.Value.Trim().Length > 0).ToList();
            if (chung.Count > 0 || ghi.Count > 0)
            {
                var c = new StackPanel { Margin = new Thickness(12, 2, 0, 4) };
                foreach (var kv in ghi) c.Children.Add(new TextBlock { Text = kv.Key + ":\n" + kv.Value.Trim(), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4), FontWeight = FontWeights.Normal });
                foreach (VanDe v in chung) c.Children.Add(DongVanDe(kt, v, sua));
                string md = Path.Combine(AiDon.ThuMucHoSo(_tm), PhongHoSo.TepDon);
                if (File.Exists(md)) c.Children.Add(NutNho("📄 Hồ sơ chung", true, () => { Mo(md); return false; }, null).Also(b => b.HorizontalAlignment = HorizontalAlignment.Left));
                p.Children.Add(new Expander
                {
                    Header = new TextBlock { Text = "📋 Chung cả đơn" + (chung.Count(v => v.ConMo) > 0 ? " — " + chung.Count(v => v.ConMo) + " câu chưa xong" : ""), FontWeight = FontWeights.SemiBold },
                    Content = c, IsExpanded = true, Margin = new Thickness(0, 6, 0, 0),
                });
            }

            _khungVanDe = new StackPanel();
            p.Children.Add(_khungVanDe);
            VeVanDe(tn, ct, kt, chay);
            UIElement bh = KhungBaiHoc(ct, chay);
            if (bh != null) p.Children.Add(bh);
            return The(TenPhong2, p);
        }

        /// <summary>Một tấm: tiêu đề &lt;đơn&gt;-n · M · T · Q (+ ⚠, số câu chưa xong), nội dung: ảnh, ghi chú, câu hỏi, nút sửa / hồ sơ.</summary>
        private UIElement TheTam(ChiTiet ct, KiemTra kt, TamAi t, string ten, List<VanDe> vd, bool sua, bool nuaRepeat)
        {
            var dau = new WrapPanel();
            if (nuaRepeat)
            {
                var moi = new CheckBox { Content = "Mới", IsChecked = t.Moi, IsEnabled = sua, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                void DoiMoi() { t.Moi = moi.IsChecked == true; LuuTam(ct); Ve(); }
                moi.Checked += (s, e) => DoiMoi();
                moi.Unchecked += (s, e) => DoiMoi();
                dau.Children.Add(moi);
            }
            dau.Children.Add(new TextBlock { Text = ten.Length > 0 ? ten : "(repeat)", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
            dau.Children.Add(new TextBlock
            {
                Text = (t.M.Length > 0 ? t.M : "M?") + " · t" + (t.T.Length > 0 ? t.T : "?") + " · ×" + (t.Q.Length > 0 ? t.Q : "?") + (t.KichThuoc.Length > 0 ? " · " + t.KichThuoc : "")
                       + (t.SoBanVe.Length > 0 ? " · BV " + t.SoBanVe : ""),
                FontWeight = FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center,
            });
            // ⚠ chỉ khi câu "AI chưa chắc ô" của tấm còn mở — đã trả lời / xử lý thì thôi báo
            if (t.KhongChac.Count > 0 && vd.Any(v => v.Ma.EndsWith("|khong_chac", StringComparison.Ordinal) && v.ConMo)) dau.Children.Add(new TextBlock { Text = "  " + t.CanhBao, Foreground = KieuTeams.Mau("#B54708"), FontWeight = FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center });
            int mo = vd.Count(v => v.ConMo && v.Muc != VanDe.ThongTin);
            if (mo > 0)
                dau.Children.Add(new TextBlock
                {
                    Text = "  ● " + mo + " câu chưa xong", FontWeight = FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center,
                    Foreground = KieuTeams.Mau(vd.Any(v => v.ConMo && v.Muc == VanDe.Do) ? "#D92D20" : "#F79009"),
                });

            var than = new StackPanel { Margin = new Thickness(4, 4, 0, 6) };
            string hs = AiDon.ThuMucHoSo(_tm), png = Path.Combine(hs, ten + ".png"), md = Path.Combine(hs, ten + ".md");
            if (ten.Length > 0 && File.Exists(png))
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // nhả tệp — cắt lại ghi đè được
                    bmp.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                    bmp.UriSource = new Uri(png);
                    bmp.EndInit();
                    var anh = new Image { Source = bmp, MaxHeight = 220, HorizontalAlignment = HorizontalAlignment.Left, Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Ảnh cắt từ bản vẽ khách — bấm để mở" };
                    anh.MouseLeftButtonUp += (s, e) => Mo(png);
                    than.Children.Add(new Border { Child = anh, BorderBrush = KieuTeams.Mau("#D0D5DD"), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 4) });
                }
                catch (Exception) { } // ảnh hỏng: bỏ qua, hồ sơ vẫn có
            if (t.GhiChuVi.Trim().Length > 0 || t.GhiChuJp.Trim().Length > 0)
                than.Children.Add(new TextBlock
                {
                    Text = "Ghi chú đặc biệt: " + (t.GhiChuVi.Trim().Length > 0 ? t.GhiChuVi.Trim() : t.GhiChuJp.Trim()), TextWrapping = TextWrapping.Wrap,
                    ToolTip = t.GhiChuJp, FontWeight = FontWeights.Normal, Margin = new Thickness(0, 0, 0, 4),
                });
            if (t.KieuTd != "lam_tay") than.Children.Add(DongMau(t, ten.Length > 0 && sua));
            foreach (VanDe v in vd.OrderBy(v => v.ConMo ? 0 : 1).ThenBy(v => v.Muc == VanDe.Do ? 0 : v.Muc == VanDe.Vang ? 1 : 2))
                than.Children.Add(DongVanDe(kt, v, sua));
            var nut = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
            string khoa = t.Khoa;
            nut.Children.Add(NutNho(_suaTam == khoa ? "✓ Xong sửa" : "✎ Sửa thông số", true, () => { _suaTam = _suaTam == khoa ? "" : khoa; Ve(); return false; }, null));
            if (File.Exists(md)) nut.Children.Add(NutNho("📄 Hồ sơ " + ten + ".md", true, () => { Mo(md); return false; }, null));
            than.Children.Add(nut);
            if (_suaTam == khoa) than.Children.Add(KhungSua(ct, t, sua));

            var ex = new Expander { Header = dau, Content = than, IsExpanded = !_dongTam.Contains(khoa), Margin = new Thickness(0, 2, 0, 0) };
            ex.Expanded += (s, e) => _dongTam.Remove(khoa);
            ex.Collapsed += (s, e) => _dongTam.Add(khoa);
            return ex;
        }

        /// <summary>
        /// Dòng "Mẫu vẽ" của tấm ở phòng 2: mẫu AI chọn trong thư viện lúc đọc (kèm mô tả), mẫu thử của đơn, hoặc chưa có mẫu →
        /// ✨ AI đề xuất mẫu ngay ở phòng 2 (không đợi phòng 3).
        /// </summary>
        private UIElement DongMau(TamAi t, bool deXuat)
        {
            string bd = t.BienDang;
            bool chuaCo = bd == PhongTrienKhai.ChuaCoMau || bd.Length == 0, thu = bd.Length > 0 && File.Exists(Path.Combine(AiDon.ThuVienThu(_tm), bd + ".py"));
            string moTa = _thuVien?.FirstOrDefault(m => m.Ten == bd).MoTa ?? "";
            var hang = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            hang.Children.Add(new TextBlock
            {
                Text = bd == PhongTrienKhai.ChuaCoMau ? "Mẫu vẽ: chưa có trong thư viện" + (t.MoTaBienDang.Length > 0 ? " — " + t.MoTaBienDang : "")
                     : bd.Length == 0 ? "Mẫu vẽ: chưa chọn" + (_thuVien != null && _thuVien.Count == 0 ? " (máy chưa có Python / ezdxf để đọc thư viện)" : " — đọc lại bản vẽ hoặc ✎ chọn mẫu")
                     : "Mẫu vẽ: " + bd + (thu ? " (mẫu thử của đơn — Admin duyệt ở phòng 3)" : moTa.Length > 0 ? " — " + moTa : ""),
                Foreground = chuaCo ? KieuTeams.Mau("#B42318") : KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0),
            });
            string khoa = t.Khoa;
            if (chuaCo && deXuat)
                hang.Children.Add(NutNho("✨ AI đề xuất mẫu vẽ", !DangChay.Contains(_d.Id), () => { _khoaMauMoi = khoa; Chay(6, false); return false; }, null)
                    .Also(b => b.ToolTip = "AI viết mẫu Python cho hình này từ hồ sơ tấm + bản vẽ; vẽ thử ở phòng 3, máy Admin duyệt vào thư viện"));
            return hang;
        }

        /// <summary>Thư viện mẫu (tên, mô tả) nạp một lần cho khung — null = đang / chưa nạp; rỗng = máy chưa có Python / ezdxf.</summary>
        private List<(string Ten, string MoTa, string Tep)> _thuVien;
        private bool _dangNapThuVien;

        private async void NapThuVien()
        {
            if (_thuVien != null || _dangNapThuVien || _tm == null) return;
            _dangNapThuVien = true;
            try { _thuVien = await AiDon.DanhSachMau(_tm); }
            catch (Exception) { _thuVien = new List<(string, string, string)>(); }
            _dangNapThuVien = false;
            if (!DangChay.Contains(_d.Id)) Ve();
        }

        private static readonly (string Nhom, (string O, string Nhan)[] O)[] NhomO =
        {
            ("Bản vẽ khách", new[] { ("so_ban_ve", "Số BV"), ("ten", "Tên") }),
            ("Thông số", new[] { ("M", "M (vật liệu)"), ("Z", "Z"), ("T", "T (độ dày)"), ("Q", "Q (số lượng)"), ("mat", "Mặt"), ("kich_thuoc", "Kích thước"), ("dung_sai", "Dung sai") }),
            ("Tiết diện", new[] { ("kieu_td", "Kiểu"), ("td_x", "X"), ("td_y", "Y"), ("ghi_chu_td", "Ghi chú"), ("bien_dang", "Mẫu vẽ") }),
            ("Mô tả & ghi chú", new[] { ("mo_ta", "Mô tả chi tiết"), ("ghi_chu_jp", "Ghi chú JP"), ("ghi_chu_vi", "Ghi chú VI") }),
            ("Lý do sửa", new[] { ("ly_do_sua", "Lý do (không bắt buộc)") }),
        };

        /// <summary>
        /// Khung ✎ Sửa thông số: các ô theo nhóm, ô AI không chắc tô vàng, ô đã sửa có ✎. Rời ô = lưu, nhớ giá trị AI (rút bài học),
        /// chạy lại luật + ghi hồ sơ md (không vẽ lại cả khung để khỏi mất chỗ con trỏ — bấm ✓ Xong sửa để cập nhật danh sách).
        /// </summary>
        private UIElement KhungSua(ChiTiet ct, TamAi t, bool sua)
        {
            var luoi = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            luoi.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            int hang = 0;
            void Them(UIElement nhan, UIElement o)
            {
                luoi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetRow(nhan, hang);
                luoi.Children.Add(nhan);
                if (o == null) Grid.SetColumnSpan(nhan, 2);
                else { Grid.SetRow(o, hang); Grid.SetColumn(o, 1); luoi.Children.Add(o); }
                hang++;
            }
            foreach (var (nhom, cacO) in NhomO)
            {
                luoi.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var tieuDe = new TextBlock { Text = nhom, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 2) };
                Grid.SetRow(tieuDe, hang++); Grid.SetColumnSpan(tieuDe, 2);
                luoi.Children.Add(tieuDe);
                foreach (var (o, nhan) in cacO)
                {
                    string giaTri = o == "ly_do_sua" ? t.LyDoSua : PhongDocBanVe.Lay(t, o);
                    var lbl = new TextBlock { Text = nhan + (t.DaSua.Contains(o) ? " ✎" : ""), FontWeight = FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center,
                                              ToolTip = t.GiaTriAi.TryGetValue(o, out string ai) ? "AI đọc: " + ai : null };
                    Brush nen = t.KhongChac.Contains(o) || (o.StartsWith("td_", StringComparison.Ordinal) && t.KhongChac.Contains("tiet_dien")) ? KieuTeams.Mau("#FFF6E0") : Brushes.White;
                    Control oNhap;
                    if (o == "kieu_td" || (o == "bien_dang" && _thuVien != null))
                    {
                        var muc = o == "kieu_td" ? new List<string> { "phang", "be", "lam_tay" }
                                                 : new List<string> { "" }.Concat(_thuVien.Select(m => m.Ten)).Concat(new[] { PhongTrienKhai.ChuaCoMau }).ToList();
                        if (!muc.Contains(giaTri)) muc.Add(giaTri); // mẫu thử của đơn
                        var cb = new ComboBox { ItemsSource = muc, SelectedItem = giaTri, IsEnabled = sua, Margin = new Thickness(0, 1, 0, 1), HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 120 };
                        cb.SelectionChanged += (s, e) => LuuO(ct, t, o, cb.SelectedItem as string ?? "");
                        oNhap = cb;
                    }
                    else
                    {
                        bool dai = o == "mo_ta" || o.StartsWith("ghi_chu", StringComparison.Ordinal);
                        var tb = new TextBox { Text = giaTri, IsReadOnly = !sua, Background = nen, Margin = new Thickness(0, 1, 0, 1), Padding = new Thickness(3, 1, 3, 1),
                                               AcceptsReturn = dai, TextWrapping = dai ? TextWrapping.Wrap : TextWrapping.NoWrap, MaxHeight = dai ? 160 : double.PositiveInfinity,
                                               VerticalScrollBarVisibility = dai ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled, FontWeight = FontWeights.Normal,
                                               ToolTip = o.StartsWith("td_", StringComparison.Ordinal) ? PhongTrienKhai.HuongDanTd : null };
                        tb.LostFocus += (s, e) => LuuO(ct, t, o, tb.Text.Trim());
                        oNhap = tb;
                    }
                    Them(lbl, oNhap);
                }
            }
            Them(new TextBlock
            {
                Text = "Chỉ xem: " + string.Join(" · ", new[] { t.TomTatCanh, t.TomTatLo, t.TomTatKeGaki, t.TomTatDimKhach }.Where(x => x.Length > 0).DefaultIfEmpty("không có bẻ / lỗ / ケガキ / dim khách")),
                Foreground = KieuTeams.ChuPhu, FontWeight = FontWeights.Normal, Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap,
                ToolTip = "bẻ " + t.CanhJson + "\nlỗ " + t.LoJson + "\nケガキ " + t.KeGakiJson + "\ndim khách " + t.DimKhachJson + "\n(chi tiết trong hồ sơ md)",
            }, null);
            return new Border { Child = luoi, Padding = new Thickness(8, 4, 8, 6), Background = KieuTeams.Mau("#F8F9FC"), CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 4, 0, 0) };
        }

        /// <summary>Lưu một ô: nhớ giá trị AI lần sửa đầu, sửa về như AI thì bỏ dấu sửa; lưu JSON, chạy luật, ghi hồ sơ md.</summary>
        private void LuuO(ChiTiet ct, TamAi t, string o, string moi)
        {
            if (o == "ly_do_sua")
            {
                if (moi == t.LyDoSua) return;
                t.LyDoSua = moi;
            }
            else
            {
                string cu = PhongDocBanVe.Lay(t, o);
                if (moi == cu) return;
                if (!t.DaSua.Contains(o) && !t.GiaTriAi.ContainsKey(o)) t.GiaTriAi[o] = cu;
                PhongDocBanVe.Dat(t, o, moi);
                if (t.GiaTriAi.TryGetValue(o, out string ai) && ai == moi) { t.DaSua.Remove(o); t.GiaTriAi.Remove(o); }
                else { t.DaSua.Add(o); t.KhongChac.Remove(o); }
                if (o == "kieu_td" || o == "td_x" || o == "td_y") t.KhongChac.Remove("tiet_dien");
            }
            LuuTam(ct);
        }

        /// <summary>Lưu bảng tấm + chạy lại luật + ghi hồ sơ md.</summary>
        private void LuuTam(ChiTiet ct)
        {
            try
            {
                AiDon.Ghi(_tm, ct);
                AiDon.KiemLuat(ct, _tm, _d.Title);
            }
            catch (Exception ex) { _trangThai.Text = "Không lưu / kiểm được: " + ex.Message; }
        }

        /// <summary>Tóm tắt vấn đề, bản nháp gửi khách, câu hỏi nội bộ, kiểm lại, ✓ Xác nhận → Triển khai.</summary>
        private void VeVanDe(TiepNhan tn, ChiTiet ct, KiemTra kt, bool chay)
        {
            StackPanel p = _khungVanDe;
            p.Children.Clear();
            if (kt == null) { p.Children.Add(Chu("Chưa kiểm được (xem dòng trạng thái).", true)); return; }
            bool daXn = ct.XacNhan.Length > 0, sua = !chay && !daXn;
            p.Children.Add(Chu(string.Format(CultureInfo.InvariantCulture, "● đỏ (chặn) {0} · ● vàng (xác nhận) {1} · ℹ ghi chú {2} — còn {3} mục đỏ chưa xử lý · kiểm lúc {4}{5}",
                kt.VanDe.Count(v => v.Muc == VanDe.Do), kt.VanDe.Count(v => v.Muc == VanDe.Vang), kt.VanDe.Count(v => v.Muc == VanDe.ThongTin), kt.DoConMo, kt.Luc,
                kt.Tien > 0 ? string.Format(CultureInfo.InvariantCulture, " · AI kiểm lại ≈ ${0:0.00}", kt.Tien) : "")));

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
                var ktMoi = AiDon.DocKiemTra(_tm) ?? kt; // đọc lại: trạng thái vừa đổi trên màn hình
                int canhBao = ct.Tam.Count(t => t.Moi && ktMoi.VanDe.Any(v => v.Ma == "luat|" + t.Khoa + "|khong_chac" && v.ConMo)); // câu đã trả lời / xử lý thì không hỏi lại
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
            try
            {
                kq = AiDon.TaoTrienKhai(ct, _tm);
                ds = AiDon.TinhLai(ct, kq, _d.Title, _tm);
                AiDon.GhiHoSo(ct, AiDon.DocKiemTra(_tm), _tm, _d.Title, ds); // hồ sơ thêm mục trải phẳng
            }
            catch (Exception ex) { p.Children.Add(Chu("Không tính được: " + ex.Message, true)); return The(TenPhong3, p); }

            int thieu = ct.Tam.Count(t => t.Moi && !PhongTrienKhai.CoTietDien(t));
            if (thieu > 0)
            {
                p.Children.Add(Chu(thieu + " tấm đọc bằng bản cũ, chưa có tiết diện — AI đọc bù (hoặc ↩ Bỏ xác nhận rồi ✎ Sửa thông số → Tiết diện X / Y ở phòng 2).", true));
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
                try { LuuKt(kt); } catch (IOException ex) { _trangThai.Text = "Không lưu được câu trả lời: " + ex.Message; }
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

        /// <summary>Lưu kiem_tra.json (trạng thái / câu trả lời) + ghi lại hồ sơ md — câu trả lời vào hồ sơ tấm cho phòng 3.</summary>
        private void LuuKt(KiemTra kt)
        {
            if (kt == null) return;
            AiDon.Ghi(_tm, kt);
            ChiTiet ct = AiDon.DocChiTiet(_tm);
            if (ct != null) AiDon.GhiHoSo(ct, kt, _tm, _d.Title);
        }

        private Button NutNho(string chu, bool bat, Func<bool> lam, KiemTra kt)
        {
            Button b = KieuTeams.Nut(chu, _mau, (s, e) => { if (lam()) { LuuKt(kt); Ve(); } }, chinh: false);
            b.IsEnabled = bat;
            b.Padding = new Thickness(8, 1, 8, 1);
            b.FontSize = 11;
            return b;
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
