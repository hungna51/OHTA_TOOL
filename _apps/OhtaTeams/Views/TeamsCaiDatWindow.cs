using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TeamsTool.Core;
using TeamsTool.Services;

namespace TeamsTool.Views
{
    /// <summary>
    /// Cửa sổ ⚙ Cài đặt OhtaTeams, chia tab: Tài khoản (Tenant/Client ID — 06_Teams\data\teams.json, Đăng xuất), Thư mục đơn,
    /// Nhân viên, Chuyển tiếp chat, 🤖 AI (key Claude, model, đồng ý gửi bản vẽ, quy chuẩn).
    /// DaDangXuat = true khi người dùng bấm Đăng xuất (tab quay về màn hình đăng nhập).
    /// </summary>
    internal sealed class TeamsCaiDatWindow : Window
    {
        private readonly TextBox _tenant = O(), _client = O(), _thuMuc = O();
        private readonly TextBlock _baoLoi = new TextBlock { Foreground = KieuTeams.Mau("#B42318"), TextWrapping = TextWrapping.Wrap };
        public bool DaDangXuat { get; private set; }

        private static TextBox O() => new TextBox { Padding = new Thickness(4, 3, 4, 3), Margin = new Thickness(0, 2, 0, 8) };

        private readonly List<CheckBox> _nhanVien = new List<CheckBox>(); // ô "hiện" từng người — bỏ tích = ẩn khỏi ô chọn người
        private readonly CheckBox _tuChuyen = new CheckBox { Content = "Máy này tự chuyển tiếp chat OHTA-受注 → OHTA-日本 (5 phút một lần, khi OhtaTeams đang mở)",
                                                             Margin = new Thickness(0, 0, 0, 4) };

        /// <param name="danhBa">Tên mọi người (danh bạ công ty + người trong đơn) để chọn ẩn / hiện; rỗng = chưa mở danh sách đơn.</param>
        public TeamsCaiDatWindow(Brush mau, List<string> danhBa = null)
        {
            Title = "Cài Đặt Teams";
            Width = 600;
            Height = 560;
            MinWidth = 480;
            MinHeight = 400;
            ResizeMode = ResizeMode.CanResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = KieuTeams.Nen;
            FontFamily = KieuTeams.Font;
            FontSize = 12;

            _tenant.Text = CaiDatTeams.TenantId;
            _client.Text = CaiDatTeams.ClientId;

            var ma = new StackPanel();
            ma.Children.Add(new TextBlock { Text = "Microsoft 365", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            ma.Children.Add(new TextBlock { Text = "Tenant ID (mã công ty)" });
            ma.Children.Add(_tenant);
            ma.Children.Add(new TextBlock { Text = "Client ID (mã ứng dụng OHTA TOOL)" });
            ma.Children.Add(_client);
            ma.Children.Add(new TextBlock { Text = "Lưu trong " + CaiDatTeams.FileMa + " (không lên GitHub).",
                                            Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap });

            _thuMuc.Text = CaiDatTeams.ThuMucGoc;
            var tm = new StackPanel();
            tm.Children.Add(new TextBlock { Text = "Thư mục đơn", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            tm.Children.Add(new TextBlock { Text = "Thư mục chứa các thư mục đơn (tên thư mục có Title đơn). Tab Teams trên CAD dùng để biết bản vẽ đang mở thuộc đơn nào.",
                                            TextWrapping = TextWrapping.Wrap });
            var hangTm = new DockPanel();
            var chon = KieuTeams.Nut("Chọn…", mau, (s, e) => ChonThuMuc(), chinh: false);
            chon.Margin = new Thickness(6, 2, 0, 8);
            DockPanel.SetDock(chon, Dock.Right);
            hangTm.Children.Add(chon);
            hangTm.Children.Add(_thuMuc);
            tm.Children.Add(hangTm);

            var tk = new DockPanel();
            var dx = KieuTeams.Nut("Đăng xuất", mau, (s, e) =>
            {
                GraphTeams.DangXuat();
                DaDangXuat = true;
                Close();
            }, chinh: false);
            dx.IsEnabled = GraphTeams.DaCoDangNhap;
            DockPanel.SetDock(dx, Dock.Right);
            tk.Children.Add(dx);
            tk.Children.Add(new TextBlock { Text = GraphTeams.DaCoDangNhap ? "Tài khoản Teams đang đăng nhập trên máy này." : "Chưa đăng nhập.",
                                            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });

            var nut = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            nut.Children.Add(KieuTeams.Nut("Huỷ", mau, (s, e) => Close(), chinh: false));
            nut.Children.Add(KieuTeams.Nut("Lưu", mau, (s, e) => Luu()));

            var tab = new TabControl { Margin = new Thickness(0, 0, 0, 8) };
            // Chuyển tiếp chat: bật trên 1–2 máy; chỉ chuyển tin từ 0 giờ ngày bật lần đầu, kiểm tra trùng nên nhiều máy bật cũng không gửi đôi
            _tuChuyen.IsChecked = CaiDatTeams.TuChuyenTiep;
            var ct = new StackPanel();
            ct.Children.Add(new TextBlock { Text = "Chuyển tiếp chat", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            ct.Children.Add(_tuChuyen);
            string kq = CaiDatTeams.KetQuaChuyen;
            ct.Children.Add(new TextBlock { Text = "Lần chuyển gần nhất: " + (kq.Length > 0 ? kq : "chưa có")
                                                   + (CaiDatTeams.MocChuyen is DateTime moc ? " · chuyển tin từ " + moc.ToString("dd/MM HH:mm") : ""),
                                            Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap });
            ct.Children.Add(new TextBlock { Text = "Chuột phải vào tin / tiêu đề nhóm OHTA-受注 trong chi tiết đơn để chuyển tiếp ngay (không cần bật ô này).",
                                            Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap });
            var tkMa = new StackPanel();
            tkMa.Children.Add(KieuTeams.The(ma));
            tkMa.Children.Add(KieuTeams.The(tk));
            Tab(tab, "Tài khoản", tkMa);
            Tab(tab, "Thư mục đơn", KieuTeams.The(tm));
            Tab(tab, "Nhân viên", KieuTeams.The(KhoiNhanVien(mau, danhBa ?? new List<string>())));
            Tab(tab, "Chuyển tiếp chat", KieuTeams.The(ct));
            Tab(tab, "🤖 AI", KhoiAi(mau));
            if (int.TryParse(CaiDatTeams.Doc("cai_dat_tab", "0"), out int tabCu)) tab.SelectedIndex = Math.Max(0, Math.Min(tab.Items.Count - 1, tabCu));
            tab.SelectionChanged += (s, e) => { if (e.Source == tab) CaiDatTeams.Ghi("cai_dat_tab", tab.SelectedIndex.ToString()); };

            var day = new StackPanel();
            day.Children.Add(_baoLoi);
            day.Children.Add(nut);
            var goc = new DockPanel { Margin = new Thickness(12) };
            DockPanel.SetDock(day, Dock.Bottom);
            goc.Children.Add(day);
            goc.Children.Add(tab);
            Content = goc;
        }

        private static void Tab(TabControl t, string ten, UIElement noiDung) =>
            t.Items.Add(new TabItem
            {
                Header = new TextBlock { Text = ten, Margin = new Thickness(4, 2, 4, 2) },
                Content = new ScrollViewer { Content = noiDung, Padding = new Thickness(6), VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            });

        // ---------- 🤖 AI ----------

        private static readonly KeyValuePair<string, string>[] ModelAi =
        {
            new KeyValuePair<string, string>("", "Theo tab AI trên CAD (Ling / chưa chọn → Opus 5.5)"),
            new KeyValuePair<string, string>("claude-opus-5-5", "Claude Opus 5.5 — đọc kỹ nhất (mặc định)"),
            new KeyValuePair<string, string>("claude-sonnet-5-5", "Claude Sonnet 5.5 — rẻ hơn ~2 lần"),
            new KeyValuePair<string, string>("claude-haiku-5-5", "Claude Haiku 5.5 — rẻ nhất, dễ đọc sót"),
        };
        private readonly PasswordBox _key = new PasswordBox { Padding = new Thickness(4, 3, 4, 3), Margin = new Thickness(0, 2, 0, 4) };
        private readonly ComboBox _model = new ComboBox { Margin = new Thickness(0, 2, 0, 4), DisplayMemberPath = "Value", SelectedValuePath = "Key" };
        private readonly CheckBox _dongY = new CheckBox { Content = "Cho phép gửi tệp bản vẽ của đơn lên dịch vụ AI (Anthropic / Google, theo Nguồn AI) để đọc", Margin = new Thickness(0, 2, 0, 4) };
        private readonly ComboBox _nguon = new ComboBox { Margin = new Thickness(0, 2, 0, 4), DisplayMemberPath = "Value", SelectedValuePath = "Key" };

        private readonly CheckBox _keyRieng = new CheckBox { Content = "Mỗi phòng một key riêng (tắt = mọi phòng dùng key chung)", Margin = new Thickness(0, 10, 0, 2) };
        private readonly PasswordBox[] _keyPhong = new PasswordBox[AiDon.KeyTheoPhong.Length];
        private readonly TextBox _baiHoc = new TextBox { Padding = new Thickness(4, 2, 4, 2), Margin = new Thickness(0, 2, 6, 4), MinWidth = 300 };

        /// <summary>Dòng "Tên: đã có key …abcd" / chữ đỏ khi chưa có.</summary>
        private static TextBlock TrangThaiKey(int phong, string ten, string chuaCo)
        {
            string cu = null, loi = null;
            try { cu = AiDon.DocKeyFile(phong); } catch (Exception ex) { loi = ex.Message; }
            return new TextBlock
            {
                Text = ten + ": " + (loi != null ? "không đọc được (" + loi + ")" : cu == null ? chuaCo : "đã có key …" + cu.Substring(Math.Max(0, cu.Length - 4))),
                Foreground = cu == null ? KieuTeams.Mau("#B42318") : KieuTeams.Mau("#1F7A4D"), TextWrapping = TextWrapping.Wrap,
            };
        }

        private StackPanel KhoiAi(Brush mau)
        {
            TextBlock Phu(string s) => new TextBlock { Text = s, Foreground = KieuTeams.ChuPhu, FontSize = 10.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
            TextBlock Dau(string s) => new TextBlock { Text = s, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) };
            var p = new StackPanel();

            // Nguồn AI: key API (mặc định) hoặc CLI dùng gói của người đăng nhập trên máy
            var n = new StackPanel();
            n.Children.Add(Dau("Nguồn AI"));
            string CoCli(string ng) => AiDon.TimCli(ng) != null ? "đã cài" : "chưa cài";
            _nguon.ItemsSource = new[]
            {
                new KeyValuePair<string, string>(AiCli.Api, "Key API Claude (mặc định — tính tiền theo token)"),
                new KeyValuePair<string, string>(AiCli.ClaudeCode, "Claude Code CLI (claude — gói Claude của người đăng nhập, " + CoCli(AiCli.ClaudeCode) + ")"),
                new KeyValuePair<string, string>(AiCli.Antigravity, "Antigravity CLI (agy — gói Google, model Gemini, " + CoCli(AiCli.Antigravity) + ")"),
            };
            _nguon.SelectedValue = AiDon.NguonAi;
            n.Children.Add(_nguon);
            n.Children.Add(Phu("CLI: không cần key, không hiện $ (tính trong gói), chậm hơn ~10–30 giây mỗi lần, không cache quy chuẩn, dễ chạm giới hạn gói khi đọc nhiều PDF. "
                               + "Mỗi máy đăng nhập bằng tài khoản của chính người dùng — dùng chung một tài khoản gói cá nhân cho nhiều người có thể trái điều khoản. "
                               + "Antigravity dùng model Gemini: lời dặn viết theo Claude, kiểm kết quả kỹ trước khi dùng rộng."));
            p.Children.Add(KieuTeams.The(n));

            var k = new StackPanel();
            k.Children.Add(Dau("Key Claude API"));
            k.Children.Add(TrangThaiKey(0, "Key chung", "Chưa có key — phòng AI không chạy được."));
            k.Children.Add(new TextBlock { Text = "Dán key chung mới để thay (để trống = giữ key cũ):", Margin = new Thickness(0, 6, 0, 0) });
            k.Children.Add(_key);
            k.Children.Add(Phu("Lưu trong " + AiDon.FileKey(0) + " — dùng chung với tab AI trên CAD, không lên GitHub."));

            // Công tắc: mỗi phòng một key riêng ↔ mọi phòng dùng key chung
            _keyRieng.IsChecked = AiDon.KeyRieng;
            k.Children.Add(_keyRieng);
            var rieng = new StackPanel { Margin = new Thickness(18, 2, 0, 0) };
            for (int i = 0; i < _keyPhong.Length; i++)
            {
                var (tep, ten) = AiDon.KeyTheoPhong[i];
                rieng.Children.Add(TrangThaiKey(tep, ten, "chưa có → dùng key chung"));
                _keyPhong[i] = new PasswordBox { Padding = new Thickness(4, 2, 4, 2), Margin = new Thickness(0, 1, 0, 6), ToolTip = "Dán key riêng của phòng (để trống = giữ)" };
                rieng.Children.Add(_keyPhong[i]);
            }
            rieng.Children.Add(Phu("Lưu trong " + Path.GetDirectoryName(AiDon.FileKey(0)) + "\\ai.key.phong2…4 (phòng 1 · 2 · 3). Phòng 1 đọc bản vẽ; phòng 2 kiểm lại câu trả lời + rút bài học; phòng 3 viết mẫu vẽ / đọc bù đơn cũ. "
                                   + "Lưu ý: giới hạn tốc độ của Claude tính theo tổ chức (organization) — các key cùng một tổ chức vẫn chia chung giới hạn; "
                                   + "key riêng giúp xem tiền theo từng phòng trên Console, muốn chạy song song nhiều hơn thì cần key ở tổ chức khác hoặc nâng hạn mức."));
            void BatTat() => rieng.IsEnabled = _keyRieng.IsChecked == true;
            _keyRieng.Checked += (s, e) => BatTat();
            _keyRieng.Unchecked += (s, e) => BatTat();
            BatTat();
            k.Children.Add(rieng);
            p.Children.Add(KieuTeams.The(k));

            var m = new StackPanel();
            m.Children.Add(Dau("Model đọc bản vẽ"));
            _model.ItemsSource = ModelAi;
            _model.SelectedValue = ModelAi.Any(x => x.Key == AiDon.ModelRieng) ? AiDon.ModelRieng : "";
            m.Children.Add(_model);
            m.Children.Add(Phu("Đang dùng: " + AiDon.Model() + ". Tiền mỗi lần đọc hiện ở phòng 1."));
            p.Children.Add(KieuTeams.The(m));

            var d = new StackPanel();
            d.Children.Add(Dau("Gửi bản vẽ ra ngoài"));
            _dongY.IsChecked = AiDon.DaDongY;
            d.Children.Add(_dongY);
            d.Children.Add(Phu("Bỏ tích → lần ▶ Bắt đầu sau hỏi lại. Chỉ bật khi công ty đã cho phép gửi bản vẽ khách hàng lên dịch vụ AI."));
            p.Children.Add(KieuTeams.The(d));

            var q = new StackPanel();
            q.Children.Add(Dau("Quy chuẩn gửi kèm AI"));
            string tmQc = AiDon.ThuMucQuyChuan;
            for (int i = 0; i < AiDon.ThuMucPhong.Length; i++)
            {
                string tmP = AiDon.ThuMucQuyChuanPhong(i + 1);
                string[] md = Directory.Exists(tmP) ? Directory.GetFiles(tmP, "*.md") : new string[0];
                q.Children.Add(new TextBlock
                {
                    Text = AiDon.TenPhong[i] + ": " + (md.Length == 0 ? "chưa có bộ riêng (dùng các .md ở gốc quy_chuan)"
                                                                       : md.Length + " tệp, " + (md.Sum(f => new FileInfo(f).Length) / 1024) + " KB"),
                    Foreground = md.Length == 0 ? KieuTeams.Mau("#B42318") : Brushes.Black, TextWrapping = TextWrapping.Wrap,
                });
            }
            q.Children.Add(new TextBlock { Text = "cong_ty.txt: " + (File.Exists(Path.Combine(tmQc, "cong_ty.txt")) ? "có" : "chưa có"), Margin = new Thickness(0, 2, 0, 0) });
            q.Children.Add(Phu(tmQc + "\\<phòng>\\*.md — mỗi phòng chỉ gửi bộ của mình cho AI (bản gốc đầy đủ ở _goc, không gửi). "
                               + "Không lên GitHub, chép tay từ máy Admin. cong_ty.txt: mỗi dòng khoá1,khoá2|Tên hiện (đoán công ty đặc biệt)."));
            var moQc = KieuTeams.Nut("📁 Mở thư mục quy chuẩn", mau, (s, e) =>
            {
                try { Directory.CreateDirectory(tmQc); System.Diagnostics.Process.Start("explorer.exe", "\"" + tmQc + "\""); }
                catch (Exception ex) { _baoLoi.Text = "Không mở được: " + ex.Message; }
            }, chinh: false);
            moQc.HorizontalAlignment = HorizontalAlignment.Left;
            q.Children.Add(moQc);
            p.Children.Add(KieuTeams.The(q));

            // Bài học AI rút từ chỗ người sửa (Admin duyệt) — gửi kèm quy chuẩn ở lượt đọc bản vẽ
            var bh = new StackPanel();
            bh.Children.Add(Dau("Bài học đọc bản vẽ"));
            int so = PhongBaiHoc.CacBaiHoc(AiDon.DocBaiHoc()).Count;
            bh.Children.Add(new TextBlock
            {
                Text = "Đang dùng: " + AiDon.FileBaiHoc + " — " + so + " bài học" + (so > PhongBaiHoc.NguongNhac ? " (quá " + PhongBaiHoc.NguongNhac + ", nên dọn bớt)" : ""),
                Foreground = so > PhongBaiHoc.NguongNhac ? KieuTeams.Mau("#B42318") : Brushes.Black, TextWrapping = TextWrapping.Wrap,
            });
            bh.Children.Add(new TextBlock { Text = "Thư mục bài học dùng chung (OneDrive / SharePoint — để trống = tệp trên máy này):", Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap });
            _baiHoc.Text = AiDon.ThuMucBaiHocChung;
            var hangBh = new WrapPanel();
            hangBh.Children.Add(_baiHoc);
            hangBh.Children.Add(KieuTeams.Nut("Chọn…", mau, (s, e) =>
            {
                using (var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Chọn thư mục dùng chung chứa bai_hoc.md", SelectedPath = _baiHoc.Text })
                    if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) _baiHoc.Text = d.SelectedPath;
            }, chinh: false));
            bh.Children.Add(hangBh);
            bh.Children.Add(Phu("Mọi máy chọn cùng một thư mục thì đọc chung một bai_hoc.md; chỉ máy Admin ghi thêm bài học. Tệp không lên GitHub."));
            p.Children.Add(KieuTeams.The(bh));
            return p;
        }

        /// <summary>
        /// Danh sách nhân viên: tích = hiện trong ô chọn người (giao đơn) và ô lọc Nhân viên; bỏ tích = ẩn (công ty khách, Administrator…).
        /// Người đã được giao trên đơn vẫn hiện tên ở bảng / chi tiết.
        /// </summary>
        private StackPanel KhoiNhanVien(Brush mau, List<string> danhBa)
        {
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = "Danh sách nhân viên", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            if (danhBa.Count == 0)
            {
                p.Children.Add(new TextBlock { Text = "Mở một danh sách đơn trước để tải danh bạ công ty.", Foreground = KieuTeams.ChuPhu, TextWrapping = TextWrapping.Wrap });
                return p;
            }
            p.Children.Add(new TextBlock { Text = "Bỏ tích người không phải nhân viên (công ty khách, Administrator…) để ẩn khỏi ô chọn người khi giao đơn và ô lọc Nhân viên.",
                                           TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            HashSet<string> an = CaiDatTeams.NhanVienAn;
            var ds = new StackPanel();
            foreach (string ten in danhBa.OrderBy(x => x, StringComparer.CurrentCulture))
            {
                var o = new CheckBox { Content = ten, IsChecked = !an.Contains(ten), Margin = new Thickness(0, 2, 0, 2) };
                _nhanVien.Add(o);
                ds.Children.Add(o);
            }
            var dem = new TextBlock { Foreground = KieuTeams.ChuPhu, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            void Dem() => dem.Text = "Hiện " + _nhanVien.Count(x => x.IsChecked == true) + " / " + _nhanVien.Count + " người";
            _nhanVien.ForEach(o => { o.Checked += (s, e) => Dem(); o.Unchecked += (s, e) => Dem(); });
            Dem();

            var loc = new TextBox { Width = 180, Padding = new Thickness(4, 2, 4, 2), ToolTip = "Gõ tên để lọc" };
            loc.TextChanged += (s, e) =>
            {
                string k = PhanTichTeams.BoDau(loc.Text);
                foreach (CheckBox o in _nhanVien)
                    o.Visibility = k.Length == 0 || PhanTichTeams.BoDau((string)o.Content).Contains(k) ? Visibility.Visible : Visibility.Collapsed;
            };
            var hang = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            hang.Children.Add(new TextBlock { Text = "🔍 ", VerticalAlignment = VerticalAlignment.Center });
            hang.Children.Add(loc);
            hang.Children.Add(KieuTeams.Nut("Hiện hết", mau, (s, e) => _nhanVien.Where(o => o.Visibility == Visibility.Visible).ToList().ForEach(o => o.IsChecked = true), chinh: false));
            hang.Children.Add(KieuTeams.Nut("Ẩn hết", mau, (s, e) => _nhanVien.Where(o => o.Visibility == Visibility.Visible).ToList().ForEach(o => o.IsChecked = false), chinh: false));
            hang.Children.Add(dem);
            p.Children.Add(hang);
            p.Children.Add(new Border
            {
                BorderBrush = KieuTeams.Mau("#D0D5DD"), BorderThickness = new Thickness(1), Padding = new Thickness(6, 2, 6, 2),
                Child = new ScrollViewer { Content = ds, MaxHeight = 240, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            });
            return p;
        }

        private void Luu()
        {
            try
            {
                // Kiểm hết các ô key trước rồi mới ghi — một ô sai thì không ghi ô nào
                var keys = new[] { _key }.Concat(_keyPhong).Select(o => o.Password.Trim()).ToArray();
                for (int i = 0; i < keys.Length; i++)
                    if (keys[i].Any(char.IsWhiteSpace))
                    {
                        _baoLoi.Text = "Key " + (i == 0 ? "chung" : "phòng " + AiDon.KeyTheoPhong[i - 1].Ten) + " có khoảng trắng — dán lại đúng key.";
                        return;
                    }
                string bh = _baiHoc.Text.Trim().Trim('"');
                if (bh.Length > 0 && !Directory.Exists(bh)) { _baoLoi.Text = "Thư mục bài học không tồn tại: " + bh; return; }
                for (int i = 0; i < keys.Length; i++)
                    if (keys[i].Length > 0) AiDon.GhiKey(keys[i], i == 0 ? 0 : AiDon.KeyTheoPhong[i - 1].Tep);
                AiDon.ThuMucBaiHocChung = bh;
                AiDon.KeyRieng = _keyRieng.IsChecked == true;
                AiDon.NguonAi = _nguon.SelectedValue as string;
                AiDon.ModelRieng = _model.SelectedValue as string;
                AiDon.DaDongY = _dongY.IsChecked == true;
                CaiDatTeams.TuChuyenTiep = _tuChuyen.IsChecked == true;
                if (_nhanVien.Count > 0)
                    CaiDatTeams.NhanVienAn = new HashSet<string>(_nhanVien.Where(o => o.IsChecked != true).Select(o => (string)o.Content));
                string tm = _thuMuc.Text.Trim().Trim('"');
                if (tm.Length > 0 && !Directory.Exists(tm)) { _baoLoi.Text = "Thư mục đơn không tồn tại: " + tm; return; }
                CaiDatTeams.GhiMa(_tenant.Text, _client.Text);
                CaiDatTeams.ThuMucGoc = tm;
                DialogResult = true;
            }
            catch (Exception ex) { _baoLoi.Text = "Không lưu được: " + ex.Message; }
        }

        private void ChonThuMuc()
        {
            using (var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Chọn thư mục chứa các thư mục đơn", SelectedPath = _thuMuc.Text })
                if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) _thuMuc.Text = d.SelectedPath;
        }
    }
}
