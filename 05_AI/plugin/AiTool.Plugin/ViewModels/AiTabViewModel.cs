using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AiTool.Core;
using AiTool.Services;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AiTool.ViewModels
{
    public sealed class Lenh : ICommand
    {
        private readonly Action _a;
        public Lenh(Action a) { _a = a; }
        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _a();
    }

    /// <summary>Một dòng trong danh sách lần thử (ảnh nhỏ + giờ + mẫu / kích thước).</summary>
    public sealed class DongLanThu
    {
        public LanThu L { get; set; }
        public ImageSource AnhNho { get; set; }
        public string Gio => L.Gio;
        public string MoTa => L.MoTa;
    }

    /// <summary>
    /// Tab "Tạo dữ liệu DXF từ ảnh" (thay tab AI cũ): dán ảnh / chọn tệp MỘT hình → OhtaTeams.exe --thu-nhanh-* (AI đọc + chọn mẫu
    /// ve_dxf, vẽ chỉ hình + dim) → xem ảnh, chèn DXF vào bản vẽ đang mở. Lần thử dùng chung với cửa sổ Thử nhanh của OhtaTeams.
    /// </summary>
    public sealed class AiTabViewModel : INotifyPropertyChanged
    {
        public static AiTabViewModel HienTai { get; private set; }

        private static readonly string[] DuoiNhan = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".pdf", ".dxf" };
        private readonly Dispatcher _ui;
        private CancellationTokenSource _huy;

        public AiTabViewModel(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            _ui = Dispatcher.CurrentDispatcher;
            TieuDe = tieuDe;
            MoTa = moTa;
            MauNhan = string.IsNullOrWhiteSpace(mauNhan) ? "#5B3FD1" : mauNhan;
            Exe = DuongDan.Exe(thuMucModule);

            DanCmd = new Lenh(Dan);
            ChonTepCmd = new Lenh(ChonTep);
            HuyCmd = new Lenh(() => _huy?.Cancel());
            ChenCmd = new Lenh(() => GuiLenh("OHTADXFCHEN "));
            VeLaiCmd = new Lenh(() => ChayLan("--thu-nhanh-ve", "Đang vẽ lại (không gọi AI)…"));
            DocLaiCmd = new Lenh(() => ChayLan("--thu-nhanh-doc-lai", "AI đang đọc lại hình…"));
            VietMauCmd = new Lenh(() => ChayLan("--thu-nhanh-viet-mau", "AI đang viết mẫu vẽ cho hình này (1–2 phút)…"));
            SuaCmd = new Lenh(() => { if (Chon?.ChiTietJson != null) Mo("notepad.exe", "\"" + Chon.ChiTietJson + "\""); });
            ThuMucCmd = new Lenh(() => { if (Chon != null) Mo("explorer.exe", "\"" + Chon.ThuMuc + "\""); });
            XoaCmd = new Lenh(Xoa);
            ThuNhanhCmd = new Lenh(() => MoExe("--thu-nhanh"));
            HienTai = this;
            NapDs();
        }

        public string TieuDe { get; }
        public string MoTa { get; }
        public string MauNhan { get; }
        public string Exe { get; }

        public Brush AccentBrush
        {
            get
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(MauNhan)); }
                catch (Exception) { return new SolidColorBrush(Color.FromRgb(0x5B, 0x3F, 0xD1)); }
            }
        }

        /// <summary>Bánh răng của nút ⚙ (cùng ảnh các tab khác), tô theo màu tab bằng OpacityMask.</summary>
        public ImageSource SettingsLogo => _logo ?? (_logo = NapResource("AiTool.Assets.settings_logo.png"));
        private static ImageSource _logo;

        public ICommand DanCmd { get; }
        public ICommand ChonTepCmd { get; }
        public ICommand HuyCmd { get; }
        public ICommand ChenCmd { get; }
        public ICommand VeLaiCmd { get; }
        public ICommand DocLaiCmd { get; }
        public ICommand VietMauCmd { get; }
        public ICommand SuaCmd { get; }
        public ICommand ThuMucCmd { get; }
        public ICommand XoaCmd { get; }
        public ICommand ThuNhanhCmd { get; }

        public ObservableCollection<DongLanThu> DanhSach { get; } = new ObservableCollection<DongLanThu>();

        private DongLanThu _dongChon;
        public DongLanThu DongChon
        {
            get => _dongChon;
            set { if (Dat(ref _dongChon, value)) HienChon(); }
        }

        private LanThu Chon => _dongChon?.L;

        /// <summary>DXF của lần thử đang chọn — lệnh OHTADXFCHEN đọc.</summary>
        public string DxfDangChon => Chon != null && Chon.CoDxf ? Chon.Dxf : null;

        private bool _dangChay;
        public bool DangChay { get => _dangChay; private set { if (Dat(ref _dangChay, value)) { OnChanged(nameof(KhongChay)); OnChanged(nameof(CoTheSua)); OnChanged(nameof(CoDxf)); } } }
        public bool KhongChay => !_dangChay;
        public bool CoChon => Chon != null;
        public bool CoTheSua => CoChon && !_dangChay;
        /// <summary>Hình chưa có mẫu biên dạng → nút ✨ AI viết mẫu (mẫu thử của lần thử này; thêm vào thư viện ở ⚙ Thử nhanh).</summary>
        public bool CanMau => Chon != null && Chon.CanMau;
        public bool CoDxf => DxfDangChon != null && !_dangChay;

        private string _bao = "Dán ảnh chụp MỘT hình (Ctrl+V) hoặc chọn tệp ảnh / PDF / DXF.";
        public string Bao { get => _bao; private set => Dat(ref _bao, value); }

        // ----- Lần thử đang chọn -----
        public string TieuDeChon => Chon == null ? "" : Chon.Gio + " · " + Chon.MoTa;
        public string ChiTietChon => Chon?.ChiTiet ?? "";
        public ImageSource AnhGoc => Chon?.Anh == null ? null : NapAnh(Chon.Anh, 0);
        public ImageSource AnhVe => Chon?.Xem == null ? null : NapAnh(Chon.Xem, 0);
        public string LoiChon => Chon?.Loi;
        public bool CoLoi => !string.IsNullOrEmpty(Chon?.Loi);
        public string CanhBaoChon => Chon == null ? "" : string.Join("\n", Chon.CanhBao.Select(s => "⚠ " + s));
        public bool CoCanhBao => Chon != null && Chon.CanhBao.Count > 0;
        public string XacNhanChon => Chon == null ? "" : "Cần xác nhận (AI tự suy — nét VÀNG trong DXF):\n" + string.Join("\n", Chon.CanXacNhan.Select(s => "• " + s));
        public bool CoXacNhan => Chon != null && Chon.CanXacNhan.Count > 0;

        private void HienChon()
        {
            foreach (string p in new[] { nameof(CoChon), nameof(CoTheSua), nameof(CoDxf), nameof(TieuDeChon), nameof(ChiTietChon), nameof(AnhGoc),
                                         nameof(AnhVe), nameof(LoiChon), nameof(CoLoi), nameof(CanhBaoChon), nameof(CoCanhBao), nameof(CanMau), nameof(XacNhanChon), nameof(CoXacNhan) })
                OnChanged(p);
        }

        // ----- Nhận ảnh -----

        /// <summary>Ctrl+V / nút Dán: ảnh trong bộ nhớ tạm, hoặc tệp đã copy trong Explorer.</summary>
        public void Dan()
        {
            try
            {
                if (Clipboard.ContainsFileDropList() && Clipboard.GetFileDropList().Count > 0) { Nhan(Clipboard.GetFileDropList()[0]); return; }
                BitmapSource anh = Clipboard.GetImage();
                if (anh == null) { Bao = "Bộ nhớ tạm không có ảnh — chụp vùng bản vẽ (Win+Shift+S) rồi dán lại."; return; }
                string f = Path.Combine(Path.GetTempPath(), "OHTA_TOOL", "dxf_tu_anh_dan.png");
                Directory.CreateDirectory(Path.GetDirectoryName(f));
                using (var fs = File.Create(f)) { var en = new PngBitmapEncoder(); en.Frames.Add(BitmapFrame.Create(anh)); en.Save(fs); }
                Nhan(f);
            }
            catch (Exception ex) { Bao = "Không dán được: " + ex.Message; }
        }

        private void ChonTep()
        {
            var d = new Microsoft.Win32.OpenFileDialog { Filter = "Bản vẽ (ảnh, PDF, DXF)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.pdf;*.dxf|Mọi tệp|*.*" };
            if (d.ShowDialog() == true) Nhan(d.FileName);
        }

        /// <summary>Tệp mới → OhtaTeams tạo lần thử, AI đọc + chọn mẫu, vẽ.</summary>
        public void Nhan(string f)
        {
            if (!DuoiNhan.Contains(Path.GetExtension(f).ToLowerInvariant())) { Bao = "Chỉ nhận ảnh, PDF hoặc DXF: " + Path.GetFileName(f); return; }
            Chay("Đang gửi " + Path.GetFileName(f) + "…", "--thu-nhanh-moi", f);
        }

        private void ChayLan(string lenh, string bao)
        {
            if (Chon != null) Chay(bao, lenh, Chon.ThuMuc);
        }

        /// <summary>Chạy exe nền; dòng tiến độ lên ô báo, #KQ → nạp lại danh sách và chọn lần thử đó.</summary>
        private void Chay(string bao, string lenh, string thamSo)
        {
            if (DangChay) return;
            DangChay = true;
            Bao = bao;
            _huy = new CancellationTokenSource();
            string thuMuc = null, loi = null;
            LanThu kq = null;
            ChayExe.Chay(Exe, DuongDan.ThamSo(lenh, thamSo), dong =>
            {
                var (loai, chu) = DongExe.Doc(dong);
                switch (loai)
                {
                    case LoaiDong.TienDo: _ui.BeginInvoke(new Action(() => Bao = chu)); break;
                    case LoaiDong.ThuMuc: thuMuc = chu; break;
                    case LoaiDong.KetQua: kq = LanThu.TuJson(chu).FirstOrDefault(); break;
                    case LoaiDong.Loi: loi = chu; break;
                }
            }, _huy.Token).ContinueWith(t => _ui.BeginInvoke(new Action(() =>
            {
                DangChay = false;
                string chonLai = kq?.ThuMuc ?? thuMuc ?? thamSo;
                NapDs(chonLai, kq); // kq vừa chạy có lỗi vẽ cụ thể (vd thiếu thông số mẫu) mà danh sách không biết
                if (t.IsCanceled) Bao = "Đã huỷ.";
                else if (t.IsFaulted) Bao = "Lỗi: " + t.Exception?.GetBaseException().Message;
                else if (loi != null) Bao = "✖ " + loi;
                else if (kq == null) Bao = "OhtaTeams không trả kết quả (exe cũ? chạy build_ohta.ps1).";
                else Bao = kq.CoDxf ? "Đã vẽ — bấm 📥 Chèn vào bản vẽ rồi chọn điểm đặt góc dưới trái. F-name tự thêm."
                         : "Chưa vẽ được hình — xem dòng đỏ, ✎ Sửa thông số rồi 🔁 Vẽ lại.";
            })));
        }

        /// <summary>Nạp danh sách lần thử (--thu-nhanh-ds), chọn lại thư mục chonLai (null = giữ lần đang chọn, không có thì lần mới nhất).</summary>
        public void NapDs(string chonLai = null, LanThu moi = null)
        {
            string giu = chonLai ?? Chon?.ThuMuc;
            List<LanThu> ds = null;
            string loi = null;
            ChayExe.Chay(Exe, "--thu-nhanh-ds", dong =>
            {
                var (loai, chu) = DongExe.Doc(dong);
                if (loai == LoaiDong.KetQua) ds = LanThu.TuJson(chu);
                else if (loai == LoaiDong.Loi) loi = chu;
            }, CancellationToken.None).ContinueWith(t => _ui.BeginInvoke(new Action(() =>
            {
                if (t.IsFaulted) { Bao = t.Exception?.GetBaseException().Message; return; }
                if (loi != null) { Bao = "✖ " + loi; return; }
                DanhSach.Clear();
                foreach (LanThu l in ds ?? new List<LanThu>())
                    DanhSach.Add(new DongLanThu
                    {
                        L = moi != null && string.Equals(moi.ThuMuc, l.ThuMuc, StringComparison.OrdinalIgnoreCase) ? moi : l,
                        AnhNho = l.Anh == null ? null : NapAnh(l.Anh, 60),
                    });
                DongChon = DanhSach.FirstOrDefault(d => string.Equals(d.L.ThuMuc, giu, StringComparison.OrdinalIgnoreCase)) ?? DanhSach.FirstOrDefault();
                HienChon(); // cùng thư mục nhưng dữ liệu mới (vừa vẽ lại)
            })));
        }

        private void Xoa()
        {
            if (Chon == null || DangChay) return;
            if (MessageBox.Show("Xoá hẳn lần thử " + Chon.Gio + "? (ảnh, dữ liệu, DXF, mẫu thử chưa thêm vào thư viện)", "Tạo DXF từ ảnh",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try { Directory.Delete(Chon.ThuMuc, true); }
            catch (Exception ex) { Bao = "Chưa xoá được (DXF đang mở trong CAD?): " + ex.Message; return; }
            Bao = "Đã xoá lần thử.";
            NapDs("");
        }

        private void GuiLenh(string lenh)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) { Bao = "Chưa mở bản vẽ nào."; return; }
            if (DxfDangChon == null) { Bao = "Lần thử này chưa có DXF."; return; }
            doc.SendStringToExecute("\x03\x03" + lenh, true, false, false);
        }

        private void MoExe(string thamSo)
        {
            try { ChayExe.Mo(Exe, thamSo); }
            catch (Exception ex) { Bao = ex.Message; }
        }

        private void Mo(string exe, string thamSo)
        {
            try { Process.Start(exe, thamSo); }
            catch (Exception ex) { Bao = "Không mở được: " + ex.Message; }
        }

        /// <summary>Ảnh nạp hẳn vào bộ nhớ (OnLoad) để không khoá tệp — xoá / vẽ lại được.</summary>
        private static ImageSource NapAnh(string f, int cao)
        {
            if (!File.Exists(f)) return null;
            try
            {
                var b = new BitmapImage();
                b.BeginInit();
                b.CacheOption = BitmapCacheOption.OnLoad;
                b.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                if (cao > 0) b.DecodePixelHeight = cao;
                b.UriSource = new Uri(f);
                b.EndInit();
                b.Freeze();
                return b;
            }
            catch (Exception) { return null; }
        }

        private static ImageSource NapResource(string ten)
        {
            try
            {
                using (Stream s = typeof(AiTabViewModel).Assembly.GetManifestResourceStream(ten))
                {
                    if (s == null) return null;
                    var b = new BitmapImage();
                    b.BeginInit(); b.CacheOption = BitmapCacheOption.OnLoad; b.StreamSource = s; b.EndInit(); b.Freeze();
                    return b;
                }
            }
            catch (Exception) { return null; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        private bool Dat<T>(ref T f, T v, [CallerMemberName] string p = null)
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return false;
            f = v;
            OnChanged(p);
            return true;
        }
    }
}
