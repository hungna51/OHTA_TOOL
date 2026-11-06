using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AiTool.CadServices;
using AiTool.Core;
using AiTool.Services;
using WinReg = Microsoft.Win32;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AiTool.ViewModels
{
    /// <summary>Một dòng cảnh báo hiện trên tab.</summary>
    public sealed class DongCanhBao
    {
        public int SoThuTu { get; set; }
        public bool Do { get; set; }
        public string Icon => Do ? "🔴" : "🟡";
        public string TieuDe { get; set; }
        public string LyDo { get; set; }
        public string GoiY { get; set; }
        public bool CoGoiY => !string.IsNullOrWhiteSpace(GoiY);
    }

    public sealed class Lenh : ICommand
    {
        private readonly Action _a;
        public Lenh(Action a) { _a = a; }
        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _a();
    }

    /// <summary>
    /// Tab AI. Nút bấm gửi lệnh CAD (OHTAAI / OHTAAIVUNG / OHTAAIZOOM) để việc đọc bản vẽ chạy
    /// trong ngữ cảnh lệnh; lệnh quét xong gọi BatDauKiemTra, phần gọi mạng chạy nền, cập nhật
    /// giao diện qua Dispatcher. Không đoạn nào ở đây ghi vào bản vẽ.
    /// </summary>
    public sealed class AiTabViewModel : INotifyPropertyChanged
    {
        public static AiTabViewModel HienTai { get; private set; }

        private const string KhoaReg = @"Software\OFT_Lisp\OhtaTool\AI";
        private const string LayerMacDinh = "Khung_IN";
        private readonly Dispatcher _ui;
        private CancellationTokenSource _huy;

        public AiTabViewModel(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            _ui = Dispatcher.CurrentDispatcher;
            TieuDe = tieuDe;
            MoTa = moTa;
            MauNhan = string.IsNullOrWhiteSpace(mauNhan) ? "#5B3FD1" : mauNhan;
            ThuMucModule = thuMucModule;

            try { DuLieuTinh.DocHuongDan(ThuMucModule, HuongDanMacDinh()); } // chưa có file thì tạo từ bản mặc định
            catch (Exception) { }
            try { CauHinhKev.DamBaoFileMacDinh(ThuMucModule); }
            catch (Exception) { }

            CauHinhKev chInit = CauHinhKev.DocTuThuMuc(ThuMucModule);
            _cheDo = DocReg("che_do");
            if (!KiemTraRunner.DanhSachCheDo.Contains(_cheDo))
            {
                _cheDo = DuLieuTinh.DocKey(ThuMucModule) != null
                    ? KiemTraRunner.CheDoHybrid
                    : KiemTraRunner.CheDoChiKev;
            }
            _kevUrl = KevHttp.ChonUrlHieuLuc(DocReg("kev_url"), chInit.MayChuLan);
            string danNhanReg = DocReg("dan_nhan");
            _danNhanGocKhung = danNhanReg == null || danNhanReg == "1";

            _model = DocReg("model");
            if (!BangGia.Model.Contains(_model)) _model = BangGia.Model[0];
            _layerKhung = DocReg("layer_khung") ?? LayerMacDinh;
            LamMoiLayer();

            KiemTraCmd = new Lenh(() => GuiLenh("OHTAAI "));
            ChonVungCmd = new Lenh(() => GuiLenh("OHTAAIVUNG "));
            XuatMoTaCmd = new Lenh(() => GuiLenh("OHTAAIMOTA "));
            DungCmd = new Lenh(() => _huy?.Cancel());
            CaiDatCmd = new Lenh(MoCaiDat);
            MoNhatKyCmd = new Lenh(MoNhatKy);
            HienTai = this;
        }

        public string TieuDe { get; }
        public string MoTa { get; }
        public string MauNhan { get; }

        public Brush AccentBrush
        {
            get
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(MauNhan)); }
                catch (Exception) { return new SolidColorBrush(Color.FromRgb(0x5B, 0x3F, 0xD1)); }
            }
        }

        /// <summary>Bánh răng của nút cài đặt (cùng ảnh các tab khác), tô theo màu tab bằng OpacityMask.</summary>
        public ImageSource SettingsLogo => _settingsLogo ?? (_settingsLogo = NapLogo());
        private static ImageSource _settingsLogo;

        private static ImageSource NapLogo()
        {
            try
            {
                using (Stream s = typeof(AiTabViewModel).Assembly.GetManifestResourceStream("AiTool.Assets.settings_logo.png"))
                {
                    if (s == null) return null;
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.StreamSource = s;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch (Exception) { return null; }
        }

        /// <summary>Trạng thái key hiện dưới tiêu đề nút cài đặt / trên tab.</summary>
        public string TrangThaiKey
        {
            get
            {
                string k = DuLieuTinh.DocKey(ThuMucModule);
                return k == null ? "Chưa có API key Claude — bấm ⚙ để nhập (hoặc chọn Chỉ Kev)" : "API key: " + DuLieuTinh.AnKey(k);
            }
        }
        public bool ChuaCoKey => _cheDo == KiemTraRunner.CheDoChiClaude && DuLieuTinh.DocKey(ThuMucModule) == null;
        public string ThuMucModule { get; }
        public string[] DanhSachCheDo => KiemTraRunner.DanhSachCheDo;
        public string[] DanhSachModel => BangGia.Model;
        public ObservableCollection<string> DanhSachLayer { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Loi { get; } = new ObservableCollection<string>();
        public ObservableCollection<DongCanhBao> CanhBaoHien { get; } = new ObservableCollection<DongCanhBao>();
        private List<DongCanhBao> _tatCa = new List<DongCanhBao>();

        /// <summary>Khung của lần quét gần nhất (để OHTAAIZOOM tìm theo số thứ tự).</summary>
        public List<KhungDuLieu> KhungLanCuoi { get; private set; } = new List<KhungDuLieu>();

        public ICommand KiemTraCmd { get; }
        public ICommand ChonVungCmd { get; }
        public ICommand XuatMoTaCmd { get; }
        public ICommand DungCmd { get; }
        public ICommand CaiDatCmd { get; }
        public ICommand MoNhatKyCmd { get; }

        private string _cheDo;
        public string CheDo
        {
            get => _cheDo;
            set
            {
                if (Dat(ref _cheDo, value))
                {
                    GhiReg("che_do", value);
                    OnChanged(nameof(ChuaCoKey));
                    OnChanged(nameof(CanClaudeModel));
                }
            }
        }
        public bool CanClaudeModel => _cheDo != KiemTraRunner.CheDoChiKev;

        private string _kevUrl;
        public string KevUrl
        {
            get => _kevUrl;
            set { if (Dat(ref _kevUrl, value)) GhiReg("kev_url", value); }
        }

        private bool _danNhanGocKhung;
        public bool DanNhanGocKhung
        {
            get => _danNhanGocKhung;
            set { if (Dat(ref _danNhanGocKhung, value)) GhiReg("dan_nhan", value ? "1" : "0"); }
        }

        private string _thongKePhanLoai = "";
        public string ThongKePhanLoai
        {
            get => _thongKePhanLoai;
            private set { if (Dat(ref _thongKePhanLoai, value)) OnChanged(nameof(CoThongKePhanLoai)); }
        }
        public bool CoThongKePhanLoai => !string.IsNullOrEmpty(_thongKePhanLoai);

        private string _layerKhung;
        public string LayerKhung
        {
            get => _layerKhung;
            set { if (Dat(ref _layerKhung, value)) GhiReg("layer_khung", value); }
        }

        private string _model;
        public string Model
        {
            get => _model;
            set { if (Dat(ref _model, value)) GhiReg("model", value); }
        }

        private bool _chiHienDo;
        public bool ChiHienDo
        {
            get => _chiHienDo;
            set { if (Dat(ref _chiHienDo, value)) LocCanhBao(); }
        }

        private bool _dangChay;
        public bool DangChay { get => _dangChay; private set { if (Dat(ref _dangChay, value)) OnChanged(nameof(KhongChay)); } }
        public bool KhongChay => !_dangChay;

        private string _tienDo = "";
        public string TienDo { get => _tienDo; private set => Dat(ref _tienDo, value); }

        private string _tong = "Chưa kiểm tra.";
        public string Tong { get => _tong; private set => Dat(ref _tong, value); }

        private string _fileNhatKy;
        public bool CoNhatKy => !string.IsNullOrEmpty(_fileNhatKy);


        /// <summary>Đọc lại danh sách layer của bản vẽ đang mở (gọi khi mở ô chọn layer).</summary>
        public void LamMoiLayer()
        {
            var ds = new List<string>();
            try
            {
                Document doc = CadApplication.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    using (doc.LockDocument())
                    using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                        foreach (ObjectId id in lt)
                            ds.Add(((LayerTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name);
                        tr.Commit();
                    }
                }
            }
            catch (Exception) { }
            if (!ds.Contains(_layerKhung, StringComparer.OrdinalIgnoreCase)) ds.Add(_layerKhung);
            string giu = _layerKhung;
            DanhSachLayer.Clear();
            foreach (string l in ds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) DanhSachLayer.Add(l);
            _layerKhung = giu;
            OnChanged(nameof(LayerKhung));
        }

        /// <summary>Lệnh OHTAAI / OHTAAIVUNG gọi sau khi quét (luồng CAD).</summary>
        public void BatDauKiemTra(List<KhungDuLieu> khung)
        {
            if (DangChay) { CanhBaoDongLenh("[AI] Dang kiem tra, bam Dung truoc."); return; }
            KhungLanCuoi = khung ?? new List<KhungDuLieu>();
            Loi.Clear();
            ThongKePhanLoai = "";
            _tatCa = new List<DongCanhBao>();
            LocCanhBao();

            string cheDo = CheDo;
            string key = DuLieuTinh.DocKey(ThuMucModule);
            string kevUrl = KevUrl;
            string loiTruoc = KiemTraRunner.LoiTruocKhiChay(cheDo, key, kevUrl, KhungLanCuoi.Count, LayerKhung);
            if (loiTruoc != null)
            {
                Loi.Add(loiTruoc);
                Tong = "Chưa kiểm tra.";
                return;
            }

            string model = Model, thuMuc = ThuMucModule;
            string vatLieu = "";
            try { vatLieu = DuLieuTinh.KhoiVatLieuTrenMay(thuMuc); }
            catch (Exception ex) { Loi.Add("Không đọc được bảng vật liệu: " + ex.Message); return; }

            DangChay = true;
            _huy = new CancellationTokenSource();
            CancellationToken ct = _huy.Token;
            List<KhungDuLieu> ds = KhungLanCuoi;
            TienDo = "Đang phân tích " + ds.Count + " khung (" + cheDo + ")…";
            var tienDo = new BaoTienDo(_ui, p => TienDo = "Bước " + p.xong + "/" + p.tong);

            IGuiYeuCau guiKev = cheDo != KiemTraRunner.CheDoChiClaude
                ? new KevHttp(kevUrl, thuMuc)
                : null;
            IGuiYeuCau guiClaude = (cheDo != KiemTraRunner.CheDoChiKev && !string.IsNullOrWhiteSpace(key))
                ? new ClaudeHttp(key, YeuCauApi.CoFallback(model))
                : null;

            Task.Run(async () =>
            {
                if (cheDo != KiemTraRunner.CheDoChiClaude)
                    await KevHttp.DongBoLuatTuMayChu(kevUrl, thuMuc, ct).ConfigureAwait(false);
                CauHinhKev cauHinhKev = CauHinhKev.DocTuThuMuc(thuMuc);
                string loiDan = "";
                if (guiClaude != null)
                {
                    try
                    {
                        loiDan = DuLieuTinh.DocHuongDan(thuMuc, HuongDanMacDinh())
                               + CauHinhKev.DocKhoiDongBoChoClaude(thuMuc);
                    }
                    catch (Exception) { }
                }
                return await KiemTraRunner.ChayTheoCheDo(cheDo, ds, model, loiDan, vatLieu, guiKev, guiClaude, tienDo, ct, cauHinhKev).ConfigureAwait(false);
            }).ContinueWith(t => _ui.BeginInvoke(new Action(() => XongKiemTra(t, cheDo, model, thuMuc))));
        }

        private void XongKiemTra(Task<KetQuaKiemTra> t, string cheDo, string model, string thuMuc)
        {
            DangChay = false;
            TienDo = "";
            // Đọc t.Result khi task bị huỷ / lỗi sẽ ném trong callback Dispatcher -> có thể kéo sập CAD
            if (t.Status != TaskStatus.RanToCompletion || t.Result == null)
            {
                Loi.Add(t.IsCanceled ? "Đã dừng." : "Lỗi khi kiểm tra: " + (t.Exception?.GetBaseException().Message ?? "không rõ"));
                return;
            }
            KetQuaKiemTra kq = t.Result;
            foreach (string l in kq.LoiLo) Loi.Add(l);
            _tatCa = kq.CanhBao.Select(x => new DongCanhBao
            {
                SoThuTu = x.Khung.SoThuTu,
                Do = x.CB.Do,
                TieuDe = DongChu.TenKhung(x.Khung) + " · " + x.CB.LoaiLoi,
                LyDo = x.CB.LyDo,
                GoiY = x.CB.GoiY,
            }).ToList();
            LocCanhBao();

            if (kq.NhanGiaCong != null && kq.NhanGiaCong.Count > 0)
            {
                string tomTat = string.Join(" · ", kq.NhanGiaCong.Values
                    .GroupBy(v => v)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key + " (" + g.Count() + ")"));

                if (DanNhanGocKhung)
                {
                    try
                    {
                        int soGhi = DanNhanKhung.GhiNhan(CadApplication.DocumentManager.MdiActiveDocument, KhungLanCuoi, kq.NhanGiaCong);
                        ThongKePhanLoai = "🏷 Phân loại " + kq.NhanGiaCong.Count + " tấm: " + tomTat
                            + (soGhi > 0 ? " — đã dán " + soGhi + " nhãn góc trên-trái (layer " + KevApi.LayerNhan + ")" : "");
                    }
                    catch (Exception ex)
                    {
                        Loi.Add("Không dán được nhãn lên bản vẽ: " + ex.Message);
                        ThongKePhanLoai = "🏷 Phân loại " + kq.NhanGiaCong.Count + " tấm: " + tomTat;
                    }
                }
                else
                {
                    ThongKePhanLoai = "🏷 Phân loại " + kq.NhanGiaCong.Count + " tấm: " + tomTat;
                }
            }

            int soDo = _tatCa.Count(c => c.Do);
            if (cheDo == KiemTraRunner.CheDoChiKev)
            {
                Tong = string.Format(CultureInfo.InvariantCulture,
                    "{0} cảnh báo ({1} 🔴) · {2} khung · Kev-0.8B ({3:0} ms · $0.0000)",
                    _tatCa.Count, soDo, KhungLanCuoi.Count, kq.ThoiGianKevMs);
            }
            else if (cheDo == KiemTraRunner.CheDoHybrid)
            {
                Tong = string.Format(CultureInfo.InvariantCulture,
                    "{0} cảnh báo ({1} 🔴) · {2} khung (Kev lọc {3} nghi vấn, {4:0} ms) · Claude ≈ ${5:0.0000}",
                    _tatCa.Count, soDo, KhungLanCuoi.Count, kq.SoKhungGuiClaude, kq.ThoiGianKevMs, kq.TongTien);
            }
            else
            {
                Tong = string.Format(CultureInfo.InvariantCulture,
                    "{0} cảnh báo ({1} 🔴) · {2} khung · token vào {3} / ra {4} / cache đọc {5} · ≈ ${6:0.0000}",
                    _tatCa.Count, soDo, KhungLanCuoi.Count, kq.TongSuDung.Vao + kq.TongSuDung.GhiCache, kq.TongSuDung.Ra,
                    kq.TongSuDung.DocCache, kq.TongTien);
            }
            try
            {
                _fileNhatKy = NhatKy.Ghi(thuMuc, model, kq);
                OnChanged(nameof(CoNhatKy));
            }
            catch (Exception ex) { Loi.Add("Không ghi được nhật ký: " + ex.Message); }
        }

        private void LocCanhBao()
        {
            CanhBaoHien.Clear();
            foreach (DongCanhBao c in _tatCa.Where(c => !ChiHienDo || c.Do)) CanhBaoHien.Add(c);
        }

        public void ZoomToi(DongCanhBao c)
        {
            if (c != null) GuiLenh("OHTAAIZOOM " + c.SoThuTu + " ");
        }

        private void GuiLenh(string lenh)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            if (DangChay && lenh != null && !lenh.StartsWith("OHTAAIZOOM", StringComparison.Ordinal))
            {
                Loi.Add("Đang kiểm tra — bấm Dừng trước khi chạy lại.");
                return;
            }
            doc.SendStringToExecute("\x03\x03" + lenh, true, false, false);
        }

        /// <summary>Nút ⚙: cửa sổ cài đặt (Kev URL + API key + file hướng dẫn AI). Đóng thì cập nhật trạng thái key.</summary>
        private void MoCaiDat()
        {
            try
            {
                var win = new Views.AiCaiDatWindow(ThuMucModule, HuongDanMacDinh(), AccentBrush, KevUrl);
                if (CadApplication.ShowModalWindow(win) == true && !string.IsNullOrWhiteSpace(win.KevUrlDaLuu))
                    KevUrl = win.KevUrlDaLuu;
            }
            catch (Exception ex)
            {
                Loi.Add("Không mở được cài đặt: " + ex.Message);
            }
            OnChanged(nameof(TrangThaiKey));
            OnChanged(nameof(ChuaCoKey));
            if (!ChuaCoKey)
                for (int i = Loi.Count - 1; i >= 0; i--)
                    if (Loi[i].StartsWith("Chưa có API key", StringComparison.Ordinal)) Loi.RemoveAt(i);
        }

        private void MoNhatKy()
        {
            if (string.IsNullOrEmpty(_fileNhatKy) || !File.Exists(_fileNhatKy)) return;
            try { Process.Start("explorer.exe", "/select,\"" + _fileNhatKy + "\""); }
            catch (Exception) { }
        }

        /// <summary>Bản mặc định của file hướng dẫn AI (nhúng trong DLL).</summary>
        public static string HuongDanMacDinh()
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("AiTool.Assets.huong_dan_ai_mac_dinh.md"))
            {
                if (s == null) return string.Empty;
                using (var r = new StreamReader(s, System.Text.Encoding.UTF8)) return r.ReadToEnd();
            }
        }


        private static void CanhBaoDongLenh(string s)
        {
            try { CadApplication.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n" + s); } catch (Exception) { }
        }

        private static string DocReg(string ten)
        {
            try
            {
                using (WinReg.RegistryKey k = WinReg.Registry.CurrentUser.OpenSubKey(KhoaReg))
                    return k?.GetValue(ten) as string;
            }
            catch (Exception) { return null; }
        }

        private static void GhiReg(string ten, string gt)
        {
            try
            {
                using (WinReg.RegistryKey k = WinReg.Registry.CurrentUser.CreateSubKey(KhoaReg))
                    k?.SetValue(ten, gt ?? string.Empty);
            }
            catch (Exception) { }
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

        /// <summary>IProgress đẩy về luồng giao diện qua Dispatcher (không phụ thuộc SynchronizationContext của CAD).</summary>
        private sealed class BaoTienDo : IProgress<(int xong, int tong)>
        {
            private readonly Dispatcher _d;
            private readonly Action<(int xong, int tong)> _a;
            private int _max;
            public BaoTienDo(Dispatcher d, Action<(int xong, int tong)> a) { _d = d; _a = a; }
            public void Report((int xong, int tong) v)
            {
                // Các lô song song có thể báo lệch thứ tự -> chỉ hiện số lớn nhất
                int moi = v.xong, cu;
                do { cu = _max; if (moi <= cu) return; } while (Interlocked.CompareExchange(ref _max, moi, cu) != cu);
                _d.BeginInvoke(new Action(() => _a(v)));
            }
        }
    }
}
