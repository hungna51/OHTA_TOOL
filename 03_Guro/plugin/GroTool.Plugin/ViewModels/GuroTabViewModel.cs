using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
#else
using Autodesk.AutoCAD.ApplicationServices;
#endif
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.ViewModels
{
    /// <summary>
    /// Tab Guro trên bảng OHTA (mục 192): cùng các thiết lập của hộp thoại GRO, nhưng mỗi
    /// lần đổi là ghi ngay registry (GroRegistry), không có nút OK. Luật BẬT/TẮT ô giữ
    /// đúng như GroSettingsViewModel. Danh sách layer lấy từ bản vẽ đang mở và tự làm
    /// mới khi chuyển bản vẽ.
    /// </summary>
    public sealed class GuroTabViewModel : INotifyPropertyChanged
    {
        /// <summary>Lệnh GROPICKMAU (pick nét mẫu) ghi registry xong thì báo tab đọc lại.</summary>
        public static void BaoDaDoiThietLap() => _hienTai?.LamMoiSau();

        /// <summary>
        /// Chỉ tab đang hiện mới nghe sự kiện: OHTARELOAD dựng lại palette tạo ViewModel
        /// mới, ViewModel cũ thôi đọc bản vẽ.
        /// </summary>
        private static GuroTabViewModel _hienTai;
        private static bool _daMoc;
        private readonly System.Windows.Threading.Dispatcher _dispatcher;

        private readonly string _thuMucModule;
        private bool _dangNap;

        private bool _useVung, _useInPdf, _useTaoKhung, _useXln, _autoDim, _xepCum, _datMk;
        private int _mauDim = GroThamSo.MauMacDinh;
        private string _layerKhung, _layerVung, _layerXln;
        private bool _xlnTuDong;

        private List<LayerMuc> _dsKhung = new List<LayerMuc>(), _dsVung = new List<LayerMuc>(), _dsXln = new List<LayerMuc>();
        private LayerMuc _chonKhung, _chonVung, _chonXln;
        private string _canhBaoKhung, _canhBaoVung, _canhBaoXln, _trangThai;

        public GuroTabViewModel(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            TieuDe = string.IsNullOrWhiteSpace(tieuDe) ? "GURO" : tieuDe;
            MoTa = moTa ?? "";
            AccentBrush = TaoBrush(mauNhan, "#323639");
            _thuMucModule = thuMucModule;

            ChayGroCommand = new RelayCommand(() => GuiLenh("GROCHAY"));   // GRO chỉ mở tab (mục 193)
            ChayGiCommand = new RelayCommand(() => GuiLenh("GI"));
            MoDstCommand = new RelayCommand(() => GuiLenh("DST"));
            LamMoiCommand = new RelayCommand(LamMoi);
            PickNetCommand = new RelayCommand(() => GuiLenh("GROPICKMAU"), () => BatChonMau);

            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            _hienTai = this;
            if (!_daMoc)
            {
                try
                {
                    Application.DocumentManager.DocumentActivated += (s, e) => _hienTai?.LamMoiSau();
                    _daMoc = true;
                }
                catch { }
            }
            LamMoi();
        }

        public string TieuDe { get; }
        public string MoTa { get; }
        public Brush AccentBrush { get; }

        public ICommand ChayGroCommand { get; }
        public ICommand ChayGiCommand { get; }
        public ICommand MoDstCommand { get; }
        public ICommand LamMoiCommand { get; }
        public ICommand PickNetCommand { get; }

        public string TrangThai { get => _trangThai; private set { _trangThai = value; Bao(); } }

        // ---------------------------- Layer ----------------------------

        public List<LayerMuc> DsKhung { get => _dsKhung; private set { _dsKhung = value; Bao(); } }
        public List<LayerMuc> DsVung { get => _dsVung; private set { _dsVung = value; Bao(); } }
        public List<LayerMuc> DsXln { get => _dsXln; private set { _dsXln = value; Bao(); } }

        public string CanhBaoKhung { get => _canhBaoKhung; private set { _canhBaoKhung = value; Bao(); } }
        public string CanhBaoVung { get => _canhBaoVung; private set { _canhBaoVung = value; Bao(); } }
        public string CanhBaoXln { get => _canhBaoXln; private set { _canhBaoXln = value; Bao(); } }

        public LayerMuc ChonKhung
        {
            get => _chonKhung;
            set
            {
                if (value == null || ReferenceEquals(value, _chonKhung)) return;
                _chonKhung = value; Bao();
                if (_dangNap) return;
                _layerKhung = value.Ten;
                Ghi("layer_khung", value.Ten);
                if (value.SoLuong >= 0) CanhBaoKhung = null;
            }
        }

        public LayerMuc ChonVung
        {
            get => _chonVung;
            set
            {
                if (value == null || ReferenceEquals(value, _chonVung)) return;
                _chonVung = value; Bao();
                if (_dangNap) return;
                _layerVung = value.Ten;
                Ghi("layer_vung", value.Ten);
                if (value.SoLuong >= 0) CanhBaoVung = null;
            }
        }

        public LayerMuc ChonXln
        {
            get => _chonXln;
            set
            {
                if (value == null || ReferenceEquals(value, _chonXln)) return;
                _chonXln = value; Bao();
                if (_dangNap) return;
                _xlnTuDong = value.Ten == GuroTabLayer.TenTuDong;
                Ghi("xln_tu_dong", _xlnTuDong);
                if (!_xlnTuDong)
                {
                    _layerXln = value.Ten;
                    Ghi("layer_xln", value.Ten);
                }
                if (value.SoLuong >= 0) CanhBaoXln = null;
            }
        }

        // ---------------------------- Cờ ----------------------------

        public bool UseInPdf { get => _useInPdf; set => DatCo(ref _useInPdf, value, "use_in_pdf"); }

        public bool UseTaoKhung
        {
            get => _useTaoKhung;
            set { DatCo(ref _useTaoKhung, value, "use_tao_khung"); Bao(nameof(BatSapXep)); Bao(nameof(BatDatMk)); }
        }

        public bool UseVung
        {
            get => _useVung;
            set { DatCo(ref _useVung, value, "use_vung"); Bao(nameof(BatLayerVung)); }
        }

        public bool XepCum
        {
            get => _xepCum;
            set { DatCo(ref _xepCum, value, "use_xep_cum"); Bao(nameof(BatDatMk)); }
        }

        public bool DatMk { get => _datMk; set => DatCo(ref _datMk, value, "use_dat_mk"); }

        public bool UseXln
        {
            get => _useXln;
            set { DatCo(ref _useXln, value, "use_xln"); Bao(nameof(BatXln)); Bao(nameof(BatChonMau)); }
        }

        public bool AutoDim
        {
            get => _autoDim;
            set { DatCo(ref _autoDim, value, "auto_dim"); Bao(nameof(BatChonMau)); Bao(nameof(NhanMau)); }
        }

        public int MauDim => _mauDim;

        public string NhanMau => _autoDim ? "Tự động: Hình học (3 loại)" : ("Thủ công: " + GroText.TenMau(_mauDim));

        /// <summary>Hai bước sau đều cần khung Khung_IN -> chỉ bật khi "Tự động tạo khung bao".</summary>
        public bool BatSapXep => _useTaoKhung;
        public bool BatDatMk => _useTaoKhung && _xepCum;
        public bool BatLayerVung => _useVung;
        public bool BatXln => _useXln;
        public bool BatChonMau => _useXln && !_autoDim;

        /// <summary>Chọn màu bằng tay thì TẮT tự nhận diện — giống hộp thoại.</summary>
        public void DatMauTay(int aci)
        {
            if (!GroThamSo.LaMauHopLe(aci)) return;
            _mauDim = aci;
            Ghi("mau_dim", aci);
            AutoDim = false;
            Bao(nameof(MauDim));
            Bao(nameof(NhanMau));
        }

        // ---------------------------- Nạp ----------------------------

        /// <summary>Đọc lại registry và layer của bản vẽ đang mở.</summary>
        public void LamMoi()
        {
            _dangNap = true;
            try
            {
                GroSettings s = GroRegistry.Doc(out _xlnTuDong);
                _useVung = s.UseVung; _useInPdf = s.UseInPdf; _useTaoKhung = s.UseTaoKhung;
                _useXln = s.UseXln; _autoDim = s.AutoDim; _xepCum = s.XepCum; _datMk = s.DatMk;
                _mauDim = s.MauDim;
                _layerKhung = s.LayerKhung; _layerVung = s.LayerVung; _layerXln = s.LayerXln;
            }
            catch (Exception ex)
            {
                TrangThai = "Không đọc được thiết lập: " + ex.Message;
            }
            finally
            {
                _dangNap = false;
            }
            Bao(string.Empty);
            NapLayer();
        }

        /// <summary>Làm mới sau khi CAD xong việc đang làm (đổi bản vẽ, lệnh vừa ghi registry).</summary>
        private void LamMoiSau()
        {
            try { _dispatcher.BeginInvoke(new Action(LamMoi)); }
            catch { }
        }

        private void NapLayer()
        {
            Document doc = null;
            try { doc = Application.DocumentManager.MdiActiveDocument; } catch { }

            List<LayerMuc> khung = new List<LayerMuc>(), vung = new List<LayerMuc>(), xln = new List<LayerMuc>();
            if (doc != null)
            {
                try
                {
                    LayerQuery.KetQuaDem dem;
                    using (doc.LockDocument())
                        dem = LayerQuery.Dem(doc.Database);
                    khung = LayerRanking.DanhSachKhung(dem.Khung4, dem.TatCaLayer, out _);
                    vung = LayerRanking.DanhSachKhung(dem.Khung3, dem.TatCaLayer, out _);
                    xln = LayerRanking.DanhSachXln(dem.DoiTuong, dem.TatCaLayer);
                    TrangThai = "Bản vẽ: " + Path.GetFileName(doc.Name);
                }
                catch (Exception ex)
                {
                    TrangThai = "Không đọc được layer: " + ex.Message;
                }
            }
            else
            {
                TrangThai = "Chưa mở bản vẽ nào.";
            }

            GuroTabLayer.KetQua k = GuroTabLayer.ChoKhung(khung, _layerKhung);
            GuroTabLayer.KetQua v = GuroTabLayer.ChoKhung(vung, _layerVung);
            GuroTabLayer.KetQua x = GuroTabLayer.ChoXln(xln, _xlnTuDong, _layerXln);

            _dangNap = true;
            try
            {
                DsKhung = k.Ds; _chonKhung = null; ChonKhung = k.Chon; CanhBaoKhung = k.CanhBao;
                DsVung = v.Ds; _chonVung = null; ChonVung = v.Chon; CanhBaoVung = v.CanhBao;
                DsXln = x.Ds; _chonXln = null; ChonXln = x.Chon; CanhBaoXln = x.CanhBao;
            }
            finally
            {
                _dangNap = false;
            }
        }

        // ---------------------------- Tiện ích ----------------------------

        private void DatCo(ref bool truong, bool giaTri, string khoa, [CallerMemberName] string ten = null)
        {
            if (truong == giaTri) return;
            truong = giaTri;
            Bao(ten);
            if (!_dangNap) Ghi(khoa, giaTri);
        }

        private void Ghi(string khoa, string v) => GhiAnToan(() => GroRegistry.Ghi(khoa, v));
        private void Ghi(string khoa, bool v) => GhiAnToan(() => GroRegistry.Ghi(khoa, v));
        private void Ghi(string khoa, int v) => GhiAnToan(() => GroRegistry.Ghi(khoa, v));

        private void GhiAnToan(Action ghi)
        {
            try { ghi(); }
            catch (Exception ex) { TrangThai = "Không lưu được thiết lập: " + ex.Message; }
        }

        /// <summary>
        /// Gửi lệnh xuống dòng lệnh như nút của OHTA: hủy lệnh đang treo, nạp
        /// lisp/autoload.lsp của mục con (bản vẽ mới mở chưa kịp nạp) rồi gọi lệnh.
        /// </summary>
        private void GuiLenh(string lenh)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            string nap = "";
            if (!string.IsNullOrEmpty(_thuMucModule))
            {
                string f = Path.Combine(_thuMucModule, "lisp", "autoload.lsp").Replace('\\', '/');
                nap = "(if (findfile \"" + f + "\") (load \"" + f + "\")) ";
            }
            doc.SendStringToExecute("\x03\x03" + nap + lenh + "\n", true, false, true);
        }

        private static Brush TaoBrush(string mau, string macDinh)
        {
            try { return (Brush)new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(mau) ? macDinh : mau); }
            catch { return (Brush)new BrushConverter().ConvertFromString(macDinh); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao([CallerMemberName] string ten = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
        }
    }
}
