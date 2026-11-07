using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using AiTool.Core;
using AiTool.Services;

namespace AiTool.ViewModels
{
    /// <summary>Một bong bóng chat trên màn hình (bọc TinChat, báo thay đổi khi chữ chảy về).</summary>
    public sealed class DongChat : INotifyPropertyChanged
    {
        public DongChat(TinChat t, bool hienSuyNghi) { Tin = t; _hienSuyNghi = hienSuyNghi; }
        public TinChat Tin { get; }
        public bool LaNguoiDung => Tin.Vai == "user";
        public bool LaAi => !LaNguoiDung;
        public string NoiDung => Tin.NoiDung;
        public string SuyNghi => Tin.SuyNghi;
        public string NhanDinhKem => string.IsNullOrWhiteSpace(Tin.DinhKem) ? "" : "📎 " + DemKhung(Tin.DinhKem) + " khung đính kèm";
        public bool CoDinhKem => NhanDinhKem.Length > 0;

        private bool _hienSuyNghi;
        public bool HienSuyNghiO => _hienSuyNghi && !string.IsNullOrEmpty(Tin.SuyNghi);
        public void DatHienSuyNghi(bool v) { _hienSuyNghi = v; Bao(nameof(HienSuyNghiO)); }

        public void Them(string chu, string suyNghi)
        {
            if (chu != null) { Tin.NoiDung += chu; Bao(nameof(NoiDung)); }
            if (suyNghi != null) { Tin.SuyNghi += suyNghi; Bao(nameof(SuyNghi)); Bao(nameof(HienSuyNghiO)); }
        }

        internal static int DemKhung(string md) =>
            md.Split('\n').Count(l => l.StartsWith("## Tam ", StringComparison.Ordinal));

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    /// <summary>Một cuộc chat đã lưu (một file .json trong OHTA_AI\chat\&lt;bản vẽ&gt;).</summary>
    public sealed class MucCuoc
    {
        public string DuongDan;
        public string Nhan;
        public override string ToString() => Nhan;
    }

    /// <summary>
    /// Màn hình 💬 Chat của tab AI: gửi câu hỏi tới Ling (streaming), lưu mỗi cuộc ra một file .json,
    /// đính kèm mô tả khung quét từ bản vẽ (lệnh OHTAAICHATKHUNG). Chỉ đọc bản vẽ.
    /// </summary>
    public sealed class ChatViewModel : INotifyPropertyChanged
    {
        private static readonly UTF8Encoding Utf8KhongBom = new UTF8Encoding(false);
        private readonly Dispatcher _ui;
        private readonly string _thuMucModule;
        private readonly Func<string> _model;
        private readonly Func<string> _fileBanVe;
        private readonly List<TinChat> _lichSu = new List<TinChat>();
        private CancellationTokenSource _huy;
        private string _thuMucChat;
        private string _fileCuoc;
        private bool _dangNapCuoc;

        /// <summary>Gọi khi cần cuộn xuống cuối (code-behind nối vào ScrollViewer).</summary>
        public event Action CuonXuong;

        public ChatViewModel(string thuMucModule, Func<string> model, Func<string> fileBanVe, Action guiKhung)
        {
            _ui = Dispatcher.CurrentDispatcher;
            _thuMucModule = thuMucModule;
            _model = model;
            _fileBanVe = fileBanVe;
            GuiCmd = new Lenh(Gui);
            DungCmd = new Lenh(() => _huy?.Cancel());
            CuocMoiCmd = new Lenh(CuocMoi);
            GuiKhungCmd = new Lenh(guiKhung);
            BoDinhKemCmd = new Lenh(() => DinhKem = null);
            MoThuMucCmd = new Lenh(MoThuMuc);
        }

        public ObservableCollection<DongChat> Dong { get; } = new ObservableCollection<DongChat>();
        public ObservableCollection<MucCuoc> DanhSachCuoc { get; } = new ObservableCollection<MucCuoc>();
        public ICommand GuiCmd { get; }
        public ICommand DungCmd { get; }
        public ICommand CuocMoiCmd { get; }
        public ICommand GuiKhungCmd { get; }
        public ICommand BoDinhKemCmd { get; }
        public ICommand MoThuMucCmd { get; }

        private string _cauHoi = "";
        public string CauHoi { get => _cauHoi; set => Dat(ref _cauHoi, value); }

        private string _dinhKem;
        public string DinhKem
        {
            get => _dinhKem;
            private set { if (Dat(ref _dinhKem, value)) { Bao(nameof(CoDinhKem)); Bao(nameof(NhanDinhKem)); } }
        }
        public bool CoDinhKem => !string.IsNullOrWhiteSpace(_dinhKem);
        public string NhanDinhKem => CoDinhKem ? "📎 " + DongChat.DemKhung(_dinhKem) + " khung sẽ gửi kèm câu hỏi kế tiếp" : "";

        private bool _hienSuyNghi;
        public bool HienSuyNghi
        {
            get => _hienSuyNghi;
            set { if (Dat(ref _hienSuyNghi, value)) foreach (DongChat d in Dong) d.DatHienSuyNghi(value); }
        }

        private bool _dangGui;
        public bool DangGui { get => _dangGui; private set { if (Dat(ref _dangGui, value)) Bao(nameof(KhongGui)); } }
        public bool KhongGui => !_dangGui;

        private string _trangThai = "";
        public string TrangThai { get => _trangThai; private set => Dat(ref _trangThai, value); }

        private MucCuoc _cuocChon;
        public MucCuoc CuocChon
        {
            get => _cuocChon;
            set { if (Dat(ref _cuocChon, value) && !_dangNapCuoc && value != null) MoCuoc(value.DuongDan); }
        }

        /// <summary>Mỗi lần vào màn hình chat: tính lại thư mục theo bản vẽ đang mở, nạp danh sách cuộc.</summary>
        public void VaoManHinh()
        {
            string moi = ChatLing.ThuMucChat(_fileBanVe(), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            if (moi == _thuMucChat) return;
            _thuMucChat = moi;
            NapDanhSachCuoc();
            if (DanhSachCuoc.Count > 0) MoCuoc(DanhSachCuoc[0].DuongDan, chon: true);
            else CuocMoi();
        }

        /// <summary>Lệnh OHTAAICHATKHUNG đưa mô tả khung (.md gọn) vào đây.</summary>
        public void DatDinhKem(string md)
        {
            DinhKem = md;
            TrangThai = "";
        }

        private void NapDanhSachCuoc()
        {
            _dangNapCuoc = true;
            DanhSachCuoc.Clear();
            try
            {
                if (Directory.Exists(_thuMucChat))
                    foreach (string f in Directory.GetFiles(_thuMucChat, "*.json").OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase))
                    {
                        List<TinChat> ls;
                        try { ls = ChatLing.DocLichSu(File.ReadAllText(f, Encoding.UTF8)); }
                        catch (Exception) { continue; } // file hỏng: bỏ qua, không làm sập danh sách
                        DanhSachCuoc.Add(new MucCuoc { DuongDan = f, Nhan = ChatLing.NhanCuoc(f, ls) });
                    }
            }
            catch (Exception ex) { TrangThai = "Không đọc được thư mục chat: " + ex.Message; }
            _cuocChon = DanhSachCuoc.FirstOrDefault(c => string.Equals(c.DuongDan, _fileCuoc, StringComparison.OrdinalIgnoreCase));
            Bao(nameof(CuocChon));
            _dangNapCuoc = false;
        }

        private void MoCuoc(string file, bool chon = false)
        {
            if (DangGui) return;
            try
            {
                List<TinChat> ls = ChatLing.DocLichSu(File.ReadAllText(file, Encoding.UTF8));
                _lichSu.Clear();
                _lichSu.AddRange(ls);
                _fileCuoc = file;
                Dong.Clear();
                foreach (TinChat t in ls) Dong.Add(new DongChat(t, HienSuyNghi));
                TrangThai = "";
                if (chon) { _dangNapCuoc = true; CuocChon = DanhSachCuoc.FirstOrDefault(c => c.DuongDan == file); _dangNapCuoc = false; }
                CuonXuong?.Invoke();
            }
            catch (Exception ex) { TrangThai = "Không mở được cuộc chat: " + ex.Message; }
        }

        private void CuocMoi()
        {
            if (DangGui) return;
            _lichSu.Clear();
            Dong.Clear();
            _fileCuoc = null;
            _dangNapCuoc = true; CuocChon = null; _dangNapCuoc = false;
            TrangThai = "Cuộc mới · lưu ở " + (_thuMucChat ?? "");
        }

        private void Gui()
        {
            string hoi = (CauHoi ?? "").Trim();
            if (hoi.Length == 0 || DangGui) return;
            string key = DuLieuTinh.DocKey(_thuMucModule, DuLieuTinh.FileKeyLing);
            if (key == null) { TrangThai = "Chưa có key Ling — bấm ⚙ để nhập."; return; }

            string luc = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var hoiTin = new TinChat { Vai = "user", NoiDung = hoi, DinhKem = DinhKem, Luc = luc };
            var traLoi = new TinChat { Vai = "assistant", NoiDung = "", SuyNghi = "", Luc = luc };
            _lichSu.Add(hoiTin);
            Dong.Add(new DongChat(hoiTin, HienSuyNghi));
            var dongAi = new DongChat(traLoi, HienSuyNghi);
            Dong.Add(dongAi);
            CauHoi = "";
            DinhKem = null;
            CuonXuong?.Invoke();

            string model = _model();
            string loiDan;
            try { loiDan = ChatLing.LoiDan(ChatLing.DocBoLuat(CauHinhKev.DuongDanThuMucKev(_thuMucModule))); }
            catch (Exception) { loiDan = ChatLing.LoiDan(""); }
            string body = ChatLing.TaoBody(model, loiDan, _lichSu);

            DangGui = true;
            TrangThai = "⏳ " + model + " đang trả lời…";
            _huy = new CancellationTokenSource();
            CancellationToken ct = _huy.Token;
            Task.Run(() => LingHttp.ChatStream(key, body,
                    (chu, sn) => _ui.BeginInvoke(new Action(() => { dongAi.Them(chu, sn); CuonXuong?.Invoke(); })), ct))
                .ContinueWith(t => _ui.BeginInvoke(new Action(() => XongGui(t, ct, traLoi, model))));
        }

        private void XongGui(Task t, CancellationToken ct, TinChat traLoi, string model)
        {
            DangGui = false;
            if (ct.IsCancellationRequested) TrangThai = "Đã dừng.";
            else if (t.IsFaulted) TrangThai = "❌ " + (t.Exception?.GetBaseException().Message ?? "lỗi không rõ");
            else TrangThai = "";
            if (!string.IsNullOrEmpty(traLoi.NoiDung)) _lichSu.Add(traLoi);
            else if (string.IsNullOrEmpty(traLoi.SuyNghi)) Dong.Remove(Dong.LastOrDefault(d => d.Tin == traLoi));
            LuuCuoc(model);
        }

        private void LuuCuoc(string model)
        {
            if (_lichSu.Count == 0 || string.IsNullOrEmpty(_thuMucChat)) return;
            try
            {
                Directory.CreateDirectory(_thuMucChat);
                bool moi = _fileCuoc == null;
                if (moi) _fileCuoc = Path.Combine(_thuMucChat, ChatLing.TenFileMoi(DateTime.Now));
                File.WriteAllText(_fileCuoc, ChatLing.GhiLichSu(model, _lichSu), Utf8KhongBom);
                if (moi) NapDanhSachCuoc();
            }
            catch (Exception ex) { TrangThai = "Không lưu được cuộc chat: " + ex.Message; }
        }

        private void MoThuMuc()
        {
            try
            {
                if (string.IsNullOrEmpty(_thuMucChat)) return;
                Directory.CreateDirectory(_thuMucChat);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + _thuMucChat + "\"");
            }
            catch (Exception) { }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        private bool Dat<T>(ref T f, T v, [CallerMemberName] string p = null)
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return false;
            f = v;
            Bao(p);
            return true;
        }
    }
}
