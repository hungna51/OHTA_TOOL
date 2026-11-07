using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiTool.Core;
using AiTool.Services;

namespace AiTool.Views
{
    /// <summary>
    /// Cửa sổ cài đặt tab AI (nút ⚙): Kev Server URL, API key (data\ai.key) và bộ file hướng dẫn AI
    /// (data\kev\00-index.md + các file .md con + data\huong_dan_ai.md).
    /// </summary>
    public partial class AiCaiDatWindow : Window
    {
        private static readonly UTF8Encoding Utf8KhongBom = new UTF8Encoding(false);
        private readonly string _thuMuc;
        private readonly string _macDinhClaude;
        private readonly List<MucFileHuongDan> _dsFile = new List<MucFileHuongDan>();
        private MucFileHuongDan _dangChon;
        private bool _dangNap;

        private sealed class MucFileHuongDan
        {
            public string NhanHienThi;
            public string DuongDan;
            public string NoiDungMacDinh;
            public string NoiDungDangSua;
            public override string ToString() => NhanHienThi;
        }

        public string KevUrlDaLuu { get; private set; }
        public string LingModelDaLuu { get; private set; }

        public AiCaiDatWindow(string thuMucModule, string huongDanMacDinh, Brush mauTab, string kevUrl = null, string lingModel = null)
        {
            InitializeComponent();
            _thuMuc = thuMucModule;
            _macDinhClaude = huongDanMacDinh ?? string.Empty;
            if (mauTab != null) NutLuu.Background = mauTab;

            KevUrlDaLuu = string.IsNullOrWhiteSpace(kevUrl) ? KevApi.UrlMacDinh : kevUrl.Trim();
            KevUrlBox.Text = KevUrlDaLuu;

            DuongDanKey.Text = Path.Combine(_thuMuc, "data", "ai.key");
            HienTrangThaiKey();

            LingModelDaLuu = string.IsNullOrWhiteSpace(lingModel) ? LingHttp.ModelMacDinh : lingModel.Trim();
            LingModelBox.Text = LingModelDaLuu;
            DuongDanLing.Text = Path.Combine(_thuMuc, "data", DuLieuTinh.FileKeyLing);
            HienTrangThaiLing();
            NapDanhSachFileHuongDan();
        }

        private void NapDanhSachFileHuongDan()
        {
            _dsFile.Clear();
            try { CauHinhKev.DamBaoFileMacDinh(_thuMuc); }
            catch (Exception) { }
            try { DuLieuTinh.DocHuongDan(_thuMuc, _macDinhClaude); }
            catch (Exception) { }

            string kevDir = CauHinhKev.DuongDanThuMucKev(_thuMuc);
            Dictionary<string, string> macDinhKev = CauHinhKev.CacFileMacDinh();
            var moTaChuan = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [CauHinhKev.FileIndex] = "kev\\00-index.md — File chính điều hướng Kev (Master Index)",
                [CauHinhKev.FilePhanLoai] = "kev\\01-phan-loai-gia-cong.md — Luật phân loại tấm (BE / VAT / CUON / SARA / TAPU)",
                [CauHinhKev.FileLuatFname] = "kev\\02-luat-fname-vat-lieu.md — Luật F-name & Vật liệu (MTZ)",
                [CauHinhKev.FileLuatGhiChu] = "kev\\03-luat-ghi-chu-ban-ve.md — Luật Ghi chú & Số lượng (Q / GHI_CHU)",
            };

            string[] thuTuUuTien = { CauHinhKev.FileIndex, CauHinhKev.FilePhanLoai, CauHinhKev.FileLuatFname, CauHinhKev.FileLuatGhiChu };
            foreach (string tenFile in thuTuUuTien)
            {
                string path = Path.Combine(kevDir, tenFile);
                string defText = macDinhKev.TryGetValue(tenFile, out string d) ? d : "";
                string curText = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : defText;
                _dsFile.Add(new MucFileHuongDan
                {
                    NhanHienThi = moTaChuan[tenFile],
                    DuongDan = path,
                    NoiDungMacDinh = defText,
                    NoiDungDangSua = curText,
                });
            }

            if (Directory.Exists(kevDir))
            {
                foreach (string extra in Directory.GetFiles(kevDir, "*.md").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    string fn = Path.GetFileName(extra);
                    if (thuTuUuTien.Contains(fn, StringComparer.OrdinalIgnoreCase)) continue;
                    _dsFile.Add(new MucFileHuongDan
                    {
                        NhanHienThi = "kev\\" + fn + " — File luật mở rộng",
                        DuongDan = extra,
                        NoiDungMacDinh = File.ReadAllText(extra, Encoding.UTF8),
                        NoiDungDangSua = File.ReadAllText(extra, Encoding.UTF8),
                    });
                }
            }

            string claudePath = DuLieuTinh.FileHuongDan(_thuMuc);
            string claudeText = _macDinhClaude;
            try { claudeText = DuLieuTinh.DocHuongDan(_thuMuc, _macDinhClaude); } catch (Exception) { }
            _dsFile.Add(new MucFileHuongDan
            {
                NhanHienThi = "huong_dan_ai.md — Hướng dẫn gốc của Claude (System Prompt)",
                DuongDan = claudePath,
                NoiDungMacDinh = _macDinhClaude,
                NoiDungDangSua = claudeText,
            });

            _dangNap = true;
            ChonFileHuongDan.ItemsSource = _dsFile;
            ChonFileHuongDan.SelectedIndex = 0;
            _dangNap = false;
            ChuyenSangFile(_dsFile[0]);
        }

        private void DoiFileHuongDan(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNap) return;
            if (_dangChon != null)
                _dangChon.NoiDungDangSua = HuongDan.Text;
            if (ChonFileHuongDan.SelectedItem is MucFileHuongDan moi)
                ChuyenSangFile(moi);
        }

        private void ChuyenSangFile(MucFileHuongDan m)
        {
            _dangChon = m;
            if (m == null) return;
            DuongDanHuongDan.Text = m.DuongDan;
            HuongDan.Text = m.NoiDungDangSua ?? string.Empty;
        }

        private void HienTrangThaiKey()
        {
            string k = DuLieuTinh.DocKey(_thuMuc);
            TrangThaiKey.Text = k == null ? "🔴 Chưa có API key (Chỉ Kev không cần key)" : "✅ Đã có key: " + DuLieuTinh.AnKey(k);
            NutXoaKey.IsEnabled = k != null;
        }

        private void HienTrangThaiLing()
        {
            string k = DuLieuTinh.DocKey(_thuMuc, DuLieuTinh.FileKeyLing);
            TrangThaiLing.Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x4E, 0x68));
            TrangThaiLing.Text = k == null ? "🔴 Chưa có key Ling (màn hình 💬 Chat cần key này)" : "✅ Đã có key: " + DuLieuTinh.AnKey(k);
            NutXoaLing.IsEnabled = k != null;
        }

        private async void KiemTraLing(object sender, RoutedEventArgs e)
        {
            string key = LingKeyMoi.Password?.Trim();
            if (string.IsNullOrEmpty(key)) key = DuLieuTinh.DocKey(_thuMuc, DuLieuTinh.FileKeyLing);
            string model = string.IsNullOrWhiteSpace(LingModelBox.Text) ? LingHttp.ModelMacDinh : LingModelBox.Text.Trim();
            TrangThaiLing.Foreground = new SolidColorBrush(Color.FromRgb(0x5F, 0x6B, 0x7A));
            TrangThaiLing.Text = "⏳ Đang gửi thử tới " + model + "...";
            var (ok, thongTin) = await LingHttp.KiemTraKey(key, model);
            TrangThaiLing.Foreground = ok ? new SolidColorBrush(Color.FromRgb(0x1F, 0x7A, 0x4D)) : new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
            TrangThaiLing.Text = ok ? "✅ Key + model dùng được (" + thongTin + "). Bấm Lưu để giữ." : "❌ " + thongTin;
        }

        private void XoaKeyLing(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Xoá key Ling khỏi máy này?", "AI", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try { DuLieuTinh.XoaKey(_thuMuc, DuLieuTinh.FileKeyLing); }
            catch (Exception ex) { MessageBox.Show(this, "Không xoá được: " + ex.Message, "AI"); }
            LingKeyMoi.Clear();
            HienTrangThaiLing();
        }

        private async void KiemTraKev(object sender, RoutedEventArgs e)
        {
            string url = KevUrlBox.Text?.Trim();
            if (string.IsNullOrEmpty(url)) url = KevApi.UrlMacDinh;
            TrangThaiKev.Foreground = new SolidColorBrush(Color.FromRgb(0x5F, 0x6B, 0x7A));
            TrangThaiKev.Text = "⏳ Đang kiểm tra kết nối tới " + url + "...";
            var (ok, thongTin) = await KevHttp.KiemTraMayChu(url, _thuMuc);
            if (ok)
            {
                int soFileDongBo = await KevHttp.DongBoLuatTuMayChu(url, _thuMuc);
                if (soFileDongBo > 0)
                    NapDanhSachFileHuongDan();
                TrangThaiKev.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x7A, 0x4D));
                TrangThaiKev.Text = "✅ Đã kết nối Kev-0.8B thành công (" + url + ")" +
                    (soFileDongBo > 0 ? (" · Đã đồng bộ " + soFileDongBo + " file luật .md từ Máy chủ") : "");
            }
            else
            {
                TrangThaiKev.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
                TrangThaiKev.Text = "❌ Không kết nối được (" + thongTin + "). Bấm \"▶ Bật Kev Server (Máy này)\" hoặc kiểm tra IP LAN.";
            }
        }

        private void BatKevLocal(object sender, RoutedEventArgs e)
        {
            try
            {
                string bat = Path.Combine(_thuMuc, "kev", "start_kev_server.bat");
                if (!File.Exists(bat))
                {
                    MessageBox.Show(this, "Không tìm thấy file khởi động Kev:\n" + bat, "AI", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Process.Start(new ProcessStartInfo(bat)
                {
                    WorkingDirectory = Path.GetDirectoryName(bat),
                    UseShellExecute = true,
                });
                TrangThaiKev.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x7A, 0x4D));
                TrangThaiKev.Text = "🚀 Đã mở cửa sổ Kev Server. Đợi 3–5 giây rồi bấm \"Kiểm tra kết nối\".";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không bật được Kev Server: " + ex.Message, "AI");
            }
        }

        private void Luu(object sender, RoutedEventArgs e)
        {
            try
            {
                string kevUrl = KevUrlBox.Text?.Trim();
                KevUrlDaLuu = string.IsNullOrEmpty(kevUrl) ? KevApi.UrlMacDinh : kevUrl;

                string moi = KeyMoi.Password?.Trim();
                if (!string.IsNullOrEmpty(moi))
                {
                    if (!moi.StartsWith("sk-ant-", StringComparison.Ordinal) &&
                        MessageBox.Show(this, "Key không bắt đầu bằng \"sk-ant-\". Vẫn lưu?", "AI",
                                        MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        return;
                    DuLieuTinh.GhiKey(_thuMuc, moi);
                }

                string lingMoi = LingKeyMoi.Password?.Trim();
                if (!string.IsNullOrEmpty(lingMoi))
                    DuLieuTinh.GhiKey(_thuMuc, lingMoi, DuLieuTinh.FileKeyLing);
                string lingModel = LingModelBox.Text?.Trim();
                LingModelDaLuu = string.IsNullOrEmpty(lingModel) ? LingHttp.ModelMacDinh : lingModel;

                if (_dangChon != null)
                    _dangChon.NoiDungDangSua = HuongDan.Text;

                foreach (MucFileHuongDan m in _dsFile)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(m.DuongDan));
                    File.WriteAllText(m.DuongDan, m.NoiDungDangSua ?? string.Empty, Utf8KhongBom);
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không lưu được: " + ex.Message, "AI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void XoaKey(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Xoá API key khỏi máy này?", "AI", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try { DuLieuTinh.XoaKey(_thuMuc); }
            catch (Exception ex) { MessageBox.Show(this, "Không xoá được: " + ex.Message, "AI"); }
            KeyMoi.Clear();
            HienTrangThaiKey();
        }

        private void KhoiPhuc(object sender, RoutedEventArgs e)
        {
            if (_dangChon == null) return;
            if (MessageBox.Show(this, "Thay nội dung file đang chọn (" + Path.GetFileName(_dangChon.DuongDan) + ") bằng bản mặc định?\n(Bấm Lưu mới ghi vào file.)", "AI",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                HuongDan.Text = _dangChon.NoiDungMacDinh;
                _dangChon.NoiDungDangSua = _dangChon.NoiDungMacDinh;
            }
        }

        private void MoNotepad(object sender, RoutedEventArgs e)
        {
            if (_dangChon == null) return;
            try
            {
                string f = _dangChon.DuongDan;
                Directory.CreateDirectory(Path.GetDirectoryName(f));
                if (!File.Exists(f))
                    File.WriteAllText(f, HuongDan.Text ?? string.Empty, Utf8KhongBom);
                Process.Start("notepad.exe", "\"" + f + "\"");
                MessageBox.Show(this, "Sửa xong trong Notepad thì lưu file, rồi đóng cửa sổ này bằng Huỷ\n(bấm Lưu ở đây sẽ ghi đè bằng nội dung trong ô).", "AI");
            }
            catch (Exception ex) { MessageBox.Show(this, "Không mở được Notepad: " + ex.Message, "AI"); }
        }

        private void MoThuMucKev(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = CauHinhKev.DuongDanThuMucKev(_thuMuc);
                Directory.CreateDirectory(dir);
                Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception ex) { MessageBox.Show(this, "Không mở được thư mục: " + ex.Message, "AI"); }
        }

        private void MoConsole(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://console.anthropic.com/settings/keys") { UseShellExecute = true }); }
            catch (Exception) { }
        }
    }
}
