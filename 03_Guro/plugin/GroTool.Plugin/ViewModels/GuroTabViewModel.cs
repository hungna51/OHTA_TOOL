using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;
using ZwCadPlugin.Views;

namespace ZwCadPlugin.ViewModels
{
    /// <summary>
    /// Tab Guro hợp nhất trên bảng OHTA TOOL (v1.7): 3 tab con ngang Home | Tấm | Error.
    /// - Home: chọn layer bản vẽ đang mở, chạy GRO/GI và báo cáo ngắn ngay bên dưới.
    /// - Tấm: bảng DataGrid 6 cột (DST) sửa hai chiều với bản vẽ + CSV.
    /// - Error: nét chưa đổi (DSN 4 nút ByLayer) + kiểm tra kín hở, ngoài hình, ByLayer, lỗi GRO, thử tỷ lệ mã khắc.
    /// </summary>
    public sealed class GuroTabViewModel : INotifyPropertyChanged
    {
        public const int TabHome = 0;
        public const int TabTam = 1;
        public const int TabError = 2;

        private static GuroTabViewModel _hienTai;
        private static bool _daMoc;
        public static GuroTabViewModel HienTai => _hienTai;

        private readonly System.Windows.Threading.Dispatcher _dispatcher;
        private readonly string _thuMucModule;
        private readonly Dictionary<string, HashSet<string>> _daXuLyTheoBanVe =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private bool _dangNap;
        private int _subTabIndex = TabHome;
        private GroSettings _caiDat = new GroSettings();
        private List<LayerMuc> _dsKhung = new List<LayerMuc>(), _dsVung = new List<LayerMuc>(), _dsXln = new List<LayerMuc>();
        private LayerMuc _chonKhung, _chonVung, _chonXln;
        private string _trangThai;
        private string _tenBanVe = "Chưa mở bản vẽ";
        private double _saiSoKinHo;
        private string _saiSoText;
        private bool _gomTamGiaCong;
        private List<LoiKinHo> _ketQuaKinHo = new List<LoiKinHo>();
        private HashSet<string> _bangoDaCoSht = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _banVeDaOverkill = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _dangDongBoLamMoi;
        private bool _dangChoOverkill;

        public PlatePaletteViewModel Dst { get; } = new PlatePaletteViewModel();
        public DsnViewModel Dsn => Dst.Dsn;
        public ObservableCollection<MucErrorItem> DsMucError { get; } = new ObservableCollection<MucErrorItem>();
        public ObservableCollection<MucErrorItem> DsMucDaXuLy { get; } = new ObservableCollection<MucErrorItem>();

        public GuroTabViewModel(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            TieuDe = string.IsNullOrWhiteSpace(tieuDe) ? "GURO" : tieuDe;
            MoTa = moTa ?? "";
            AccentBrush = TaoBrush(mauNhan, "#323639");
            _thuMucModule = thuMucModule;

            _saiSoKinHo = GroRegistry.DocSaiSoKinHo();
            _saiSoText = KiemTraKinHo.DinhDangSaiSo(_saiSoKinHo);
            _gomTamGiaCong = GroRegistry.DocGomTamGc();

            ChayGroCommand = new RelayCommand(() => GuiLenh("GROCHAY"));
            ChayGiCommand = new RelayCommand(() => GuiLenh("GI"));
            CaiDatCommand = new RelayCommand(MoCaiDat);
            MoTabTamCmd = new RelayCommand(() => SubTabIndex = TabTam);
            MoTabErrorCmd = new RelayCommand(() => SubTabIndex = TabError);
            KiemTraLaiCmd = new RelayCommand(() => YeuCauKiemTraCoOverkill(tuNguoiDung: true));
            ApDungSaiSoCmd = new RelayCommand(() => { ApDungSaiSo(); YeuCauKiemTraCoOverkill(tuNguoiDung: true); });

            MoFileCsvCmd = new RelayCommand(MoFileCsv);
            MoThuMucCmd = new RelayCommand(MoThuMuc);
            MoPdfCmd = new RelayCommand(MoFilePdf);

            BamMucErrorCmd = new RelayCommand(p => BamMucError(p as MucErrorItem));
            DuaVeByLayerCmd = new RelayCommand(p => DuaVeByLayer(p as MucErrorItem));
            Scale05Cmd = new RelayCommand(p => ThuScaleMaKhac(p as MucErrorItem, "0.5"));
            ThuScaleCmd = new RelayCommand(p => ThuScaleMaKhac(p as MucErrorItem, (p as MucErrorItem)?.ScaleInput));
            XemTamLoiTuErrorCmd = new RelayCommand(p => XemTamTuError(p as MucErrorItem));
            MoTuDienTuErrorCmd = new RelayCommand(MoTuDienTuError);
            ChonZoomErrorCmd = new RelayCommand(p => ChonZoomError(p as MucErrorItem));
            DanhDauDaXuLyCmd = new RelayCommand(p => DanhDauDaXuLy(p as MucErrorItem));
            HoanTacDaXuLyCmd = new RelayCommand(p => HoanTacDaXuLy(p as MucErrorItem));

            Dst.YeuCauMoTabTam = () => SubTabIndex = TabTam;
            Dst.YeuCauMoTabError = () => SubTabIndex = TabError;
            Dst.SauKhiLamMoi = () =>
            {
                if (_dangDongBoLamMoi) return;
                CapNhatTatCaNhan();
            };
            Dst.Dsn.SauKhiChuyenLayer = () => KiemTraLai(quetLaiTam: false);

            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            _hienTai = this;
            if (!_daMoc)
            {
                try
                {
                    CadApplication.DocumentManager.DocumentActivated += (s, e) => _hienTai?.KhiDoiBanVe();
                    _daMoc = true;
                }
                catch { }
            }
            LamMoi();

            if (JapanNoteTranslator.NenTuCapNhat(DateTime.Today))
                _ = TuCapNhatGhiChuAsync();
        }

        private async System.Threading.Tasks.Task TuCapNhatGhiChuAsync()
        {
            var kq = await System.Threading.Tasks.Task.Run(() => JapanNoteTranslator.SyncFromGoogleSheet()).ConfigureAwait(false);
            try
            {
                _dispatcher.BeginInvoke(new Action(() =>
                    TrangThai = kq.Success
                        ? "Đã cập nhật từ điển ghi chú: " + kq.Count + " cụm từ (" + DateTime.Now.ToString("HH:mm dd/MM") + ")."
                        : "Chưa cập nhật được từ điển ghi chú: " + kq.Message));
            }
            catch { }
        }

        public string TieuDe { get; }
        public string MoTa { get; }
        public Brush AccentBrush { get; }

        public ImageSource SettingsLogo => _settingsLogo ?? (_settingsLogo = NapLogo());
        private static ImageSource _settingsLogo;

        private static ImageSource NapLogo()
        {
            try
            {
                using (Stream s = typeof(GuroTabViewModel).Assembly.GetManifestResourceStream("GroTool.Assets.settings_logo.png"))
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
            catch { return null; }
        }

        public ICommand ChayGroCommand { get; }
        public ICommand ChayGiCommand { get; }
        public ICommand CaiDatCommand { get; }
        public ICommand MoTabTamCmd { get; }
        public ICommand MoTabErrorCmd { get; }
        public ICommand KiemTraLaiCmd { get; }
        public ICommand ApDungSaiSoCmd { get; }
        public ICommand MoFileCsvCmd { get; }
        public ICommand MoThuMucCmd { get; }
        public ICommand MoPdfCmd { get; }

        public ICommand BamMucErrorCmd { get; }
        public ICommand DuaVeByLayerCmd { get; }
        public ICommand Scale05Cmd { get; }
        public ICommand ThuScaleCmd { get; }
        public ICommand XemTamLoiTuErrorCmd { get; }
        public ICommand MoTuDienTuErrorCmd { get; }
        public ICommand ChonZoomErrorCmd { get; }
        public ICommand DanhDauDaXuLyCmd { get; }
        public ICommand HoanTacDaXuLyCmd { get; }

        public int SubTabIndex
        {
            get => _subTabIndex;
            set
            {
                if (_subTabIndex != value)
                {
                    _subTabIndex = value;
                    Bao();
                    CloudLoiCad.Xoa();
                    if (_subTabIndex == TabTam && Dst.AllPlates.Count == 0)
                        KiemTraLai(quetLaiKinHo: false);
                    else if (_subTabIndex == TabError)
                    {
                        if (Dst.TotalCount == 0 && Dsn.Groups.Count == 0 && DsMucError.Count == 0)
                            KiemTraLai();
                        else if (Dst.TotalCount > 0 && !_banVeDaOverkill.Contains(CadViewService.TenBanVeHienHanh() ?? string.Empty))
                            YeuCauKiemTraCoOverkill(tuNguoiDung: false);
                    }
                }
            }
        }

        public string TenBanVe { get => _tenBanVe; private set { _tenBanVe = value; Bao(); } }
        public string TrangThai { get => _trangThai; private set { _trangThai = value; Bao(); } }

        // ---------------------------- Tiến độ GRO (mục 225) ----------------------------

        private bool _dangChay;
        private double _tienDo;
        private string _tienDoChu;
        private static DateTime _lanVe;

        public bool DangChay { get => _dangChay; private set { _dangChay = value; Bao(); } }
        public double TienDo { get => _tienDo; private set { _tienDo = value; Bao(); } }
        public string TienDoChu { get => _tienDoChu; private set { _tienDoChu = value; Bao(); } }

        public static void BaoTienDo(string ten)
        {
            GuroTabViewModel vm = _hienTai;
            if (vm == null) return;
            bool xong = string.IsNullOrEmpty(ten) || TienDoGro.Chuan(ten) == "ket thuc";
            bool doiTrangThai = xong == vm.DangChay;
            if (xong)
            {
                vm.DangChay = false;
                vm.TienDo = 0;
            }
            else
            {
                if (!vm.DangChay) vm.TienDo = 0;
                vm.DangChay = true;
                int? p = TienDoGro.PhanTram(ten);
                if (p != null && p.Value > vm.TienDo) vm.TienDo = p.Value;
                vm.TienDoChu = "Đang chạy GRO: " + ten + "  (" + (int)vm.TienDo + "%)";
            }
            if (!doiTrangThai && (DateTime.Now - _lanVe).TotalMilliseconds < 100) return;
            _lanVe = DateTime.Now;
            try
            {
                var frame = new System.Windows.Threading.DispatcherFrame();
                vm._dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new System.Windows.Threading.DispatcherOperationCallback(f =>
                    {
                        ((System.Windows.Threading.DispatcherFrame)f).Continue = false;
                        return null;
                    }), frame);
                System.Windows.Threading.Dispatcher.PushFrame(frame);
            }
            catch { }
        }

        // ---------------------------- Cài đặt & Layer ----------------------------

        public string TomTat
        {
            get
            {
                var bat = new List<string>();
                if (_caiDat.UseTaoKhung) bat.Add("Khung: " + _caiDat.LayerKhungTam);
                if (_caiDat.UseInPdf) bat.Add("PDF");
                if (_caiDat.UseTaoKhung && _caiDat.XepCum) bat.Add("Xếp cụm");
                if (_caiDat.UseTaoKhung && _caiDat.XepCum && _caiDat.DatMk) bat.Add("Mã khắc");
                if (_caiDat.UseXln) bat.Add(_caiDat.AutoDim ? "Xử lý nét + Dim" : "Xử lý nét");
                return bat.Count == 0 ? "Không bật cài đặt tự động nào" : string.Join(" · ", bat);
            }
        }

        public bool UseVung
        {
            get => _caiDat.UseVung;
            set
            {
                if (_caiDat.UseVung == value) return;
                _caiDat.UseVung = value;
                Bao();
                try { GroRegistry.Ghi("use_vung", value); }
                catch (Exception ex) { OhtaBugBridge.Log("GRO/GuroTabViewModel", ex); TrangThai = "Không lưu được: " + ex.Message; }
            }
        }

        public bool UseXln => _caiDat.UseXln;

        public List<LayerMuc> DsKhung { get => _dsKhung; private set { _dsKhung = value; Bao(); } }
        public List<LayerMuc> DsVung { get => _dsVung; private set { _dsVung = value; Bao(); } }
        public List<LayerMuc> DsXln { get => _dsXln; private set { _dsXln = value; Bao(); } }

        public LayerMuc ChonKhung { get => _chonKhung; set => DatChon(ref _chonKhung, value, LayerTheoBanVe.Khung); }
        public LayerMuc ChonVung { get => _chonVung; set => DatChon(ref _chonVung, value, LayerTheoBanVe.Vung); }
        public LayerMuc ChonXln { get => _chonXln; set => DatChon(ref _chonXln, value, LayerTheoBanVe.Goc); }

        private void DatChon(ref LayerMuc truong, LayerMuc giaTri, int viTri, [CallerMemberName] string ten = null)
        {
            if (giaTri == null || ReferenceEquals(giaTri, truong)) return;
            truong = giaTri;
            Bao(ten);
            if (_dangNap) return;
            try { LayerTheoBanVe.Dat(CadApplication.DocumentManager.MdiActiveDocument, viTri, giaTri.Ten); }
            catch { }
        }

        // ---------------------------- Tab Error & Kín hở ----------------------------

        public string SaiSoText
        {
            get => _saiSoText;
            set
            {
                if (_saiSoText == value) return;
                _saiSoText = value;
                Bao();
                ApDungSaiSo();
            }
        }

        private void ApDungSaiSo()
        {
            if (KiemTraKinHo.ChuanHoaSaiSo(_saiSoText, out double tol))
            {
                _saiSoKinHo = tol;
                GroRegistry.GhiSaiSoKinHo(tol);
            }
            else
            {
                _saiSoText = KiemTraKinHo.DinhDangSaiSo(_saiSoKinHo);
                Bao(nameof(SaiSoText));
            }
        }

        public bool GomTamGiaCong
        {
            get => _gomTamGiaCong;
            set
            {
                if (_gomTamGiaCong == value) return;
                _gomTamGiaCong = value;
                Bao();
                GroRegistry.GhiGomTamGc(value);
                if (value && Dst.TotalCount > 0)
                    YeuCauKiemTraCoOverkill(tuNguoiDung: true);
                else
                    KiemTraLai(quetLaiTam: false);
            }
        }

        // ---------------------------- Nhan tab con & Bao cao ngan Home ----------------------------

        public string NhanTabHome => string.IsNullOrEmpty(Dst.GioBaoCao) ? "Sẵn sàng" : Dst.GioBaoCao;

        public int SoLoiTam => Dst.AllPlates.Count(p => p.MucDo == 2);
        public bool TabTamDo => SoLoiTam > 0;
        public string NhanTabTam => Dst.TotalCount + " tấm" + (SoLoiTam > 0 ? " · " + SoLoiTam + " lỗi" : "");

        public bool ChuaCoTam => Dst.TotalCount == 0;
        public int TongMucError => Dsn.Groups.Count + DsMucError.Count;
        public bool TabErrorDo => TongMucError > 0;
        public bool TabErrorXam => ChuaCoTam && TongMucError == 0;
        public string NhanTabError => TongMucError > 0 ? TongMucError + " mục" : (ChuaCoTam ? "Chưa có tấm" : "✓ Sạch");

        public string TieuDeBaoCao => string.IsNullOrEmpty(Dst.GioBaoCao) ? "BÁO CÁO NHANH" : "BÁO CÁO · " + Dst.GioBaoCao;
        public bool CoBaoCaoNgan => Dst.CoBangDieuKhien || Dst.TotalCount > 0 || Dsn.Groups.Count > 0 || DsMucError.Count > 0;
        public bool CoBaoCaoChiTiet => Dst.CoBangDieuKhien;

        public string OSoTam => Dst.CoBangDieuKhien ? Dst.TheTam : Dst.TotalCount.ToString();
        public string OSoNetDoi => Dst.TheNetDaDoi;
        public string OSoChuaDoi => Dsn.TotalUnconvertedCount.ToString();
        public bool CoNetChuaDoi => Dsn.TotalUnconvertedCount > 0;
        public string OSoLoiCd => (Dst.OrphanCount + Dst.SoKhongHinh + Dst.ErrorCount).ToString();
        public bool CoLoiCd => (Dst.OrphanCount + Dst.SoKhongHinh + Dst.ErrorCount) > 0;

        public List<DongTomTat> DongTomTatNgan
        {
            get
            {
                var ds = new List<DongTomTat>();
                var b = Dst.BaoCao;
                if (!b.DocDuoc) return ds;

                if (b.SoTam.HasValue)
                {
                    bool ok = (b.SoBango ?? b.SoTam) == b.SoTam && (b.SoFname ?? b.SoTam) == b.SoTam && b.LoiKieuChu == 0;
                    string val = b.DanhSoDen.HasValue ? $"1→{b.DanhSoDen}" : $"{b.SoBango ?? b.SoTam}/{b.SoTam}";
                    ds.Add(new DongTomTat("Bango · F-name · Đánh số", val, ok));
                }
                if (b.SoMkTong.HasValue && b.SoMkTong.Value > 0)
                {
                    bool ok = b.SoMkDat == b.SoMkTong && b.SoMkDeKegaki == 0;
                    string info = $"{b.SoMkDat}/{b.SoMkTong}" + (b.SoMkChuaDat > 0 ? $" ({b.SoMkChuaDat} chưa đặt)" : "");
                    ds.Add(new DongTomTat("Mã khắc vào hình", info, ok));
                }
                if (b.SoCumXepPhai.HasValue || b.SoCapTachMirror.HasValue)
                {
                    var parts = new List<string>();
                    if (b.SoCumXepPhai.HasValue) parts.Add($"{b.SoCumXepPhai} cụm");
                    if (b.SoCapTachMirror.HasValue) parts.Add($"{b.SoCapTachMirror} cặp mirror");
                    bool ok = (!b.SoDoiTuongSot.HasValue || b.SoDoiTuongSot == 0) && !b.SoCapBoQuaMirror.HasValue;
                    ds.Add(new DongTomTat("Xếp cụm · Tách mirror", string.Join(" · ", parts), ok));
                }
                if (b.SoDongCsv.HasValue || !string.IsNullOrEmpty(b.PdfLayer))
                {
                    var parts = new List<string>();
                    if (b.SoDongCsv.HasValue) parts.Add(b.CsvLoi ? "CSV lỗi" : $"CSV {b.SoDongCsv}");
                    if (!string.IsNullOrEmpty(b.PdfLayer)) parts.Add("PDF " + b.PdfLayer);
                    ds.Add(new DongTomTat("Xuất file", string.Join(" · ", parts), !b.CsvLoi));
                }
                return ds;
            }
        }

        public bool KhongCoNetChuaDoi => !ChuaCoTam && Dsn.Groups.Count == 0;
        public bool KhongCoLoiKhac => !ChuaCoTam && DsMucError.Count == 0;
        public bool HienChuaCoTamLoi => ChuaCoTam && DsMucError.Count == 0;
        public bool CoMucDaXuLy => DsMucDaXuLy.Count > 0;
        public string TieuDeDaXuLy => "Đã xác nhận xử lý (" + DsMucDaXuLy.Count + ")";
        public string DemErrorNgan => ChuaCoTam ? "Chưa có dữ liệu tấm (0 tấm)" : (Dsn.Groups.Count + " nhóm nét · " + DsMucError.Count + " lỗi/cảnh báo");
        public string TieuDeNetChuaDoi => ChuaCoTam ? "NÉT CHƯA ĐỔI" : ("NÉT CHƯA ĐỔI · layer \"" + Dsn.CurrentLayerName + "\"");
        public string DemDoiTuongChuaDoi => ChuaCoTam ? "Đã chặn quét" : (Dsn.TotalUnconvertedCount + " đối tượng");
        public string NhacMoError => "⚠ Còn " + TongMucError + " mục cần kiểm tra / xử lý";

        // ---------------------------- Nạp & Kiểm tra lại ----------------------------

        public void LamMoi()
        {
            try { _caiDat = GroRegistry.Doc(); }
            catch (Exception ex) { OhtaBugBridge.Log("GRO/GuroTabViewModel", ex); TrangThai = "Không đọc được cài đặt: " + ex.Message; }
            Bao(nameof(UseVung));
            Bao(nameof(UseXln));
            Bao(nameof(TomTat));
            NapLayer();
            if (Dst.DoiBanVe())
                KiemTraLai();
            else
                CapNhatTatCaNhan();
        }

        private void KhiDoiBanVe()
        {
            try
            {
                CloudLoiCad.Xoa();
                _dispatcher.BeginInvoke(new Action(LamMoi));
            }
            catch { }
        }

        public void NapLayer()
        {
            Document doc = null;
            try { doc = CadApplication.DocumentManager.MdiActiveDocument; } catch { }

            List<LayerMuc> khung = new List<LayerMuc>(), vung = new List<LayerMuc>(), xln = new List<LayerMuc>();
            string[] daChon = new string[3];
            if (doc != null)
            {
                try
                {
                    LayerQuery.KetQuaDem dem;
                    using (doc.LockDocument())
                        dem = LayerQuery.Dem(doc.Database);
                    khung = GuroTabLayer.KhungItTruoc(LayerRanking.DanhSachKhung(dem.Khung4, dem.TatCaLayer, out bool locKhung), locKhung);
                    vung = LayerRanking.DanhSachKhung(dem.Khung3, dem.TatCaLayer, out _);
                    xln = LayerRanking.DanhSachXln(dem.DoiTuong, dem.TatCaLayer);
                    daChon = LayerTheoBanVe.Lay(doc);
                    TenBanVe = Path.GetFileName(doc.Name);
                    TrangThai = "Bản vẽ: " + TenBanVe;
                }
                catch (Exception ex)
                {
                    OhtaBugBridge.Log("GRO/GuroTabViewModel", ex);
                    TrangThai = "Không đọc được layer: " + ex.Message;
                }
            }
            else
            {
                TenBanVe = "Chưa mở bản vẽ nào";
                TrangThai = "Chưa mở bản vẽ nào.";
            }

            _dangNap = true;
            try
            {
                DsKhung = khung; _chonKhung = null; ChonKhung = GuroTabLayer.Chon(khung, daChon[LayerTheoBanVe.Khung]);
                DsVung = vung; _chonVung = null; ChonVung = GuroTabLayer.Chon(vung, daChon[LayerTheoBanVe.Vung]);
                DsXln = xln; _chonXln = null; ChonXln = GuroTabLayer.Chon(xln, daChon[LayerTheoBanVe.Goc]);
            }
            finally
            {
                _dangNap = false;
            }
        }

        private void YeuCauKiemTraCoOverkill(bool tuNguoiDung)
        {
            Document doc = null;
            try { doc = CadApplication.DocumentManager.MdiActiveDocument; } catch { }
            if (doc == null)
            {
                KiemTraLai(tuNguoiDung: tuNguoiDung);
                return;
            }
            if (_dangChoOverkill && !tuNguoiDung) return;
            _dangChoOverkill = true;
            try
            {
                string lenh = tuNguoiDung ? "\x03\x03GROKIEMTRA\n" : "_.GROKIEMTRA_AUTO\n";
                doc.SendStringToExecute(lenh, true, false, false);
            }
            catch
            {
                _dangChoOverkill = false;
                KiemTraLai(tuNguoiDung: tuNguoiDung);
            }
        }

        /// <summary>Chạy OVERKILL theo từng layer trong khung tấm (ngữ cảnh lệnh CAD), rồi quét kiểm tra kín hở + ByLayer.</summary>
        public void ChayKiemTraTuLenh(bool tuNguoiDung)
        {
            _dangChoOverkill = false;
            Document doc = null;
            try { doc = CadApplication.DocumentManager.MdiActiveDocument; } catch { }
            if (doc == null)
            {
                KiemTraLai(tuNguoiDung: tuNguoiDung);
                return;
            }

            if (!Dst.HasUnsavedChanges)
                Dst.RefreshFromDrawing();

            if (Dst.TotalCount == 0)
            {
                KiemTraLai(quetLaiTam: false, quetLaiKinHo: true, tuNguoiDung: tuNguoiDung);
                return;
            }

            int daXoa = 0;
            string loiOk = null;
            try
            {
                daXoa = KiemTraKinHoCad.OverkillTheoTam(doc, Dst.AllPlates, _gomTamGiaCong);
                string key = CadViewService.TenBanVeHienHanh() ?? string.Empty;
                if (!string.IsNullOrEmpty(key)) _banVeDaOverkill.Add(key);
            }
            catch (Exception ex)
            {
                OhtaBugBridge.Log("GRO/Overkill", ex);
                loiOk = ex.Message;
            }

            KiemTraLai(quetLaiTam: daXoa > 0, quetLaiKinHo: true, tuNguoiDung: tuNguoiDung);

            if (loiOk != null)
                TrangThai = "Không chạy được OVERKILL: " + loiOk;
            else if (daXoa > 0)
                TrangThai = $"OVERKILL theo từng layer đã xoá/gộp {daXoa} nét đè trùng · {(TongMucError == 0 ? "Sạch lỗi!" : $"Còn {TongMucError} mục cần kiểm tra.")}";
            else if (tuNguoiDung)
                TrangThai = $"Đã chạy OVERKILL theo từng layer (0 nét trùng) · {(TongMucError == 0 ? "Sạch lỗi!" : $"Còn {TongMucError} mục cần kiểm tra.")}";
        }

        /// <summary>Quét lại bản vẽ: đồng bộ cờ mã khắc, DST; chỉ quét DSN &amp; kín hở + ByLayer khi đã có tấm (&gt; 0 tấm).</summary>
        public void KiemTraLai(bool quetLaiTam = true, bool quetLaiKinHo = true, bool tuNguoiDung = false)
        {
            Document doc = null;
            try { doc = CadApplication.DocumentManager.MdiActiveDocument; } catch { }
            CloudLoiCad.Xoa();
            if (doc == null)
            {
                Dst.Dsn.DatChanQuet();
                Dst.DatChuaQuetNet();
                _ketQuaKinHo.Clear();
                _bangoDaCoSht.Clear();
                DsMucError.Clear();
                DsMucDaXuLy.Clear();
                CapNhatTatCaNhan();
                return;
            }

            _dangDongBoLamMoi = true;
            try
            {
                if (quetLaiTam && !Dst.HasUnsavedChanges)
                {
                    Dst.RefreshFromDrawing();
                    if (Dst.TotalCount > 0 && DatMaKhacCad.DongBoCoMaKhacDaVaoHinh(doc, Dst.AllPlates) > 0)
                        Dst.RefreshFromDrawing();
                }

                if (Dst.TotalCount == 0)
                {
                    Dst.Dsn.DatChanQuet();
                    Dst.DatChuaQuetNet();
                    _ketQuaKinHo.Clear();
                    _bangoDaCoSht.Clear();
                    if (tuNguoiDung)
                        TrangThai = "Danh sách tấm đang là 0 — chưa có dữ liệu hoặc không phải đơn Guro (đã chặn quét).";
                }
                else
                {
                    Dst.Dsn.RefreshFromDrawing();
                    if (quetLaiKinHo)
                    {
                        _ketQuaKinHo = KiemTraKinHoCad.QuetTheoTam(doc, Dst.AllPlates, _gomTamGiaCong, _saiSoKinHo).Loi;
                        _bangoDaCoSht = KiemTraKinHoCad.TapBangoDaCoNetSht(doc, Dst.AllPlates);
                    }
                }
            }
            catch (Exception ex)
            {
                OhtaBugBridge.Log("GRO/GuroTabViewModel", ex);
                TrangThai = "Lỗi kiểm tra bản vẽ: " + ex.Message;
            }
            finally
            {
                _dangDongBoLamMoi = false;
            }

            XayDungDanhSachError(doc);
            CapNhatTatCaNhan();
        }

        private void XayDungDanhSachError(Document doc)
        {
            string banVeKey = CadViewService.TenBanVeHienHanh() ?? string.Empty;
            if (!_daXuLyTheoBanVe.TryGetValue(banVeKey, out HashSet<string> daXuLyKeys))
                daXuLyKeys = new HashSet<string>(StringComparer.Ordinal);

            var scaleCu = DsMucError.Concat(DsMucDaXuLy).FirstOrDefault(x => x.LaMaKhacChuaDat);

            DsMucError.Clear();
            DsMucDaXuLy.Clear();

            void ThemMuc(MucErrorItem m)
            {
                if (!m.TuDongKiemTra && daXuLyKeys.Contains(m.Key))
                {
                    m.DaXuLy = true;
                    DsMucDaXuLy.Add(m);
                }
                else
                {
                    m.DaXuLy = false;
                    DsMucError.Add(m);
                }
            }

            // 1. Lỗi kín hở, ngoài hình chính và ByLayer
            foreach (LoiKinHo l in _ketQuaKinHo)
            {
                ThemMuc(new MucErrorItem
                {
                    Key = "KH:" + l.Ma + ":" + (l.Bango ?? ""),
                    TieuDe = l.TieuDe,
                    ChiTiet = l.ChiTiet,
                    LaLoi = true,
                    TuDongKiemTra = true,
                    ViTri = l.ViTri,
                    LaByLayer = l.LaByLayer,
                    ByLayerHandles = l.Handles,
                });
            }

            // 2. Hình cô đơn, không tìm thấy hình cắt, lỗi dữ liệu bảng Tấm
            var coDon = Dst.AllPlates.Where(p => p.IsOrphan).ToList();
            if (coDon.Count > 0)
            {
                ThemMuc(new MucErrorItem
                {
                    Key = "DST:CO_DON",
                    TieuDe = "Hình cô đơn · thiếu text thông tin",
                    ChiTiet = coDon.Count + " hình chưa có Bango / F-name",
                    LaLoi = true,
                    TuDongKiemTra = true,
                    CoNutDanhSach = true,
                    LocSht = false,
                    ViTri = coDon.Select(HopCuaTam).ToList(),
                });
            }
            var khongHinh = Dst.AllPlates.Where(p => !p.IsOrphan && p.NoCuttingShape).ToList();
            if (khongHinh.Count > 0)
            {
                ThemMuc(new MucErrorItem
                {
                    Key = "DST:KHONG_HINH",
                    TieuDe = "Text thông tin không tìm thấy hình cắt",
                    ChiTiet = "Bango: " + string.Join(", ", khongHinh.Select(p => p.Bango).Take(15)),
                    LaLoi = true,
                    TuDongKiemTra = true,
                    CoNutDanhSach = true,
                    LocSht = false,
                    ViTri = khongHinh.Select(HopCuaTam).ToList(),
                });
            }
            var loiDuLieu = Dst.AllPlates.Where(p => !p.IsOrphan && !string.IsNullOrEmpty(p.Loi)).ToList();
            if (loiDuLieu.Count > 0)
            {
                ThemMuc(new MucErrorItem
                {
                    Key = "DST:LOI_DU_LIEU",
                    TieuDe = "Lỗi dữ liệu tấm (trùng Bango / sai Q, T, M)",
                    ChiTiet = "Bango: " + string.Join(", ", loiDuLieu.Select(p => p.Bango).Take(15)),
                    LaLoi = true,
                    TuDongKiemTra = true,
                    CoNutDanhSach = true,
                    LocSht = false,
                    ViTri = loiDuLieu.Select(HopCuaTam).ToList(),
                });
            }

            // 3. Mã khắc chưa đặt được vào hình (cho thử Scale 0.5 hoặc nhập tỷ lệ mới)
            var mkChuaDat = Dst.AllPlates.Where(p => !p.IsOrphan && p.CoMkChuaDat).ToList();
            if (mkChuaDat.Count > 0)
            {
                var mucMk = new MucErrorItem
                {
                    Key = "MK:CHUA_DAT",
                    TieuDe = "Chưa đặt được mã khắc vào hình (" + mkChuaDat.Count + " tấm)",
                    ChiTiet = "Bango " + string.Join(", ", mkChuaDat.Select(p => p.Bango).Take(15)) + " — thử scale hoặc move thủ công",
                    LaLoi = true,
                    TuDongKiemTra = true,
                    LaMaKhacChuaDat = true,
                    ViTri = mkChuaDat.Select(HopCuaTam).ToList(),
                };
                if (scaleCu != null)
                {
                    mucMk.ScaleInput = scaleCu.ScaleInput;
                    mucMk.ScaleThongBao = scaleCu.ScaleThongBao;
                }
                ThemMuc(mucMk);
            }

            // 4. Tấm cần đổi nét sang 4-SHT (tự mất khi đã thấy nét 4-SHT trong khung)
            var shtCon = Dst.AllPlates.Where(p => !p.IsOrphan && p.LaXuLySht && !_bangoDaCoSht.Contains(p.Bango)).ToList();
            if (shtCon.Count > 0)
            {
                ThemMuc(new MucErrorItem
                {
                    Key = "SHT:CON",
                    TieuDe = "Có " + shtCon.Count + " tấm cần xử lý đổi nét sang 4-SHT",
                    ChiTiet = "Bango: " + string.Join(", ", shtCon.Select(p => p.Bango).Take(15)),
                    LaLoi = false,
                    TuDongKiemTra = true,
                    CoNutDanhSach = true,
                    LocSht = true,
                    ViTri = shtCon.Select(HopCuaTam).ToList(),
                });
            }

            // 5. Ghi chú tiếng Nhật mới chưa có trong từ điển
            KhoiCanhBao ghiChuMoi = BaoCaoGro.KhoiGhiChuMoi(
                Dst.AllPlates.Where(p => !p.IsOrphan).Select(p => new KeyValuePair<string, string>(p.Bango, p.CustomerNote)));
            if (ghiChuMoi != null)
            {
                ThemMuc(new MucErrorItem
                {
                    Key = "GC:NHAT_MOI",
                    TieuDe = ghiChuMoi.TieuDe,
                    ChiTiet = ghiChuMoi.ChiTietGop,
                    LaLoi = false,
                    TuDongKiemTra = true,
                    CoNutTuDien = true,
                });
            }

            // 6. Các cảnh báo khác từ báo cáo GRO
            foreach (KhoiCanhBao k in Dst.BaoCao.CanhBaoKhiCoTamSong())
            {
                if (k.Loai == LoaiCanhBao.GhiChuMoi) continue;
                if (k.TieuDe.IndexOf("Chưa đặt được mã khắc", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                bool tuDong = false;
                if (k.Loai == LoaiCanhBao.ConSot && k.Handles.Count > 0)
                {
                    if (KiemTraKinHoCad.DemHandleConTonTai(doc, k.Handles) == 0) continue;
                    tuDong = true;
                }

                ThemMuc(new MucErrorItem
                {
                    Key = "BC:" + k.Loai + ":" + k.TieuDe,
                    TieuDe = k.TieuDe,
                    ChiTiet = k.ChiTietGop,
                    LaLoi = k.LaLoi,
                    TuDongKiemTra = tuDong,
                    HandlesZoom = k.Handles,
                    KhoiGoc = k,
                });
            }
        }

        private static (double MinX, double MinY, double MaxX, double MaxY) HopCuaTam(PlateItem p)
        {
            var b = p.FrameBoundingBox ?? p.BoundingBox;
            return (b.MinPoint.X, b.MinPoint.Y, b.MaxPoint.X, b.MaxPoint.Y);
        }

        private void CapNhatTatCaNhan()
        {
            foreach (string ten in new[]
            {
                nameof(NhanTabHome), nameof(SoLoiTam), nameof(TabTamDo), nameof(NhanTabTam),
                nameof(TongMucError), nameof(TabErrorDo), nameof(TabErrorXam), nameof(NhanTabError), nameof(ChuaCoTam),
                nameof(TieuDeBaoCao), nameof(CoBaoCaoNgan), nameof(CoBaoCaoChiTiet),
                nameof(OSoTam), nameof(OSoNetDoi), nameof(OSoChuaDoi), nameof(CoNetChuaDoi),
                nameof(OSoLoiCd), nameof(CoLoiCd), nameof(DongTomTatNgan),
                nameof(KhongCoNetChuaDoi), nameof(KhongCoLoiKhac), nameof(HienChuaCoTamLoi), nameof(CoMucDaXuLy),
                nameof(TieuDeDaXuLy), nameof(DemErrorNgan), nameof(TieuDeNetChuaDoi),
                nameof(DemDoiTuongChuaDoi), nameof(NhacMoError),
            })
                Bao(ten);
        }

        // ---------------------------- Tương tác trên Tab Error ----------------------------

        private void BamMucError(MucErrorItem item)
        {
            if (item == null || !item.CoViTri) return;
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            if (item.ViTri.Count > 1)
                item.ViTriHienTai = (item.ViTriHienTai + 1) % item.ViTri.Count;
            var vt = item.ViTri[item.ViTriHienTai];
            KiemTraKinHoCad.ZoomVaVeCloud(doc, vt);
            TrangThai = item.TieuDe + " (" + item.NhanViTri + ")";
        }

        private void DuaVeByLayer(MucErrorItem item)
        {
            if (item == null || item.ByLayerHandles.Count == 0) return;
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            int n = KiemTraKinHoCad.DuaVeByLayer(doc, item.ByLayerHandles);
            TrangThai = $"Đã đưa {n} đối tượng về ByLayer (giữ nguyên đường bẻ DASHED*).";
            KiemTraLai(quetLaiTam: false);
        }

        private void ThuScaleMaKhac(MucErrorItem item, string tiLeChu)
        {
            if (item == null) return;
            if (!KiemTraKinHo.ChuanHoaSaiSo(tiLeChu, out double val) || val > 1.0)
            {
                item.ScaleThongBao = "⚠ Nhập tỷ lệ hợp lệ (0 < tỷ lệ ≤ 1, vd 0.3).";
                return;
            }
            item.ScaleInput = KiemTraKinHo.DinhDangSaiSo(val);

            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            DatMaKhacCad.ThuDatLaiTheoTiLe(doc, Dst.AllPlates, val, out var datDuoc, out var chuaDuoc);
            string nhan = KiemTraKinHo.DinhDangSaiSo(val);
            if (datDuoc.Count > 0 && chuaDuoc.Count == 0)
            {
                TrangThai = $"Đã đặt mã khắc (tỷ lệ {nhan}) cho bango: {string.Join(", ", datDuoc)}.";
                KiemTraLai(quetLaiKinHo: true);
            }
            else if (datDuoc.Count > 0)
            {
                KiemTraLai(quetLaiKinHo: true);
                var con = DsMucError.FirstOrDefault(x => x.LaMaKhacChuaDat);
                if (con != null)
                    con.ScaleThongBao = $"✓ Đã đặt {datDuoc.Count} tấm. Bango {string.Join(", ", chuaDuoc)} vẫn chưa đủ chỗ ở tỷ lệ {nhan} — thử số nhỏ hơn.";
                TrangThai = $"Đã đặt {datDuoc.Count} tấm; còn {chuaDuoc.Count} tấm chưa vừa tỷ lệ {nhan}.";
            }
            else
            {
                item.ScaleThongBao = $"⚠ Tỷ lệ {nhan} vẫn chưa đủ chỗ — nhập tỷ lệ nhỏ hơn rồi bấm Thử lại.";
                TrangThai = $"Tỷ lệ {nhan} chưa đặt được mã khắc vào hình.";
            }
        }

        private void XemTamTuError(MucErrorItem item)
        {
            if (item == null) return;
            if (item.LocSht)
            {
                Dst.FilterOrphansOnly = false;
                Dst.LocChiTamSht = true;
            }
            else
            {
                Dst.LocChiTamSht = false;
                Dst.FilterOrphansOnly = true;
            }
            SubTabIndex = TabTam;
        }

        private void MoTuDienTuError()
        {
            Dst.MoTuDienCommand.Execute(null);
            KiemTraLai();
        }

        private void ChonZoomError(MucErrorItem item)
        {
            if (item?.KhoiGoc == null) return;
            Dst.ChonZoomDoiTuongCommand.Execute(item.KhoiGoc);
        }

        private void DanhDauDaXuLy(MucErrorItem item)
        {
            if (item == null || item.TuDongKiemTra) return;
            string k = CadViewService.TenBanVeHienHanh() ?? string.Empty;
            if (!_daXuLyTheoBanVe.TryGetValue(k, out var set))
                _daXuLyTheoBanVe[k] = set = new HashSet<string>(StringComparer.Ordinal);
            set.Add(item.Key);
            XayDungDanhSachError(CadApplication.DocumentManager.MdiActiveDocument);
            CapNhatTatCaNhan();
            TrangThai = "Đã đánh dấu xử lý: " + item.TieuDe;
        }

        private void HoanTacDaXuLy(MucErrorItem item)
        {
            if (item == null) return;
            string k = CadViewService.TenBanVeHienHanh() ?? string.Empty;
            if (_daXuLyTheoBanVe.TryGetValue(k, out var set))
                set.Remove(item.Key);
            XayDungDanhSachError(CadApplication.DocumentManager.MdiActiveDocument);
            CapNhatTatCaNhan();
        }

        // ---------------------------- Lệnh từ ngoài & Footer ----------------------------

        public void MoTabCon(int subTab, bool lamMoi = false)
        {
            SubTabIndex = subTab;
            if (subTab == TabError && lamMoi)
            {
                YeuCauKiemTraCoOverkill(tuNguoiDung: true);
                return;
            }
            if (lamMoi || Dst.AllPlates.Count == 0)
                KiemTraLai();
        }

        public void NhanBaoCao(string baoCao, string tenBanVe)
        {
            Dst.DatThongTin(baoCao, tenBanVe);
            SubTabIndex = TabHome;
            string key = CadViewService.TenBanVeHienHanh() ?? string.Empty;
            if (!string.IsNullOrEmpty(key)) _banVeDaOverkill.Remove(key);
            KiemTraLai();
            if (Dst.TotalCount > 0)
                YeuCauKiemTraCoOverkill(tuNguoiDung: false);
        }

        private string DuongCsvHienTai() =>
            !string.IsNullOrEmpty(Dst.BaoCao.DuongCsv) ? Dst.BaoCao.DuongCsv : CsvService.DuongDan();

        private string DuongPdfHienTai()
        {
            if (!string.IsNullOrEmpty(Dst.BaoCao.DuongPdf)) return Dst.BaoCao.DuongPdf;
            try
            {
                string thuMuc = CadApplication.GetSystemVariable("DWGPREFIX") as string ?? string.Empty;
                return Path.Combine(thuMuc, CsvService.TenDonBanVe() + ".pdf");
            }
            catch { return string.Empty; }
        }

        private void MoFileCsv()
        {
            string p = DuongCsvHienTai();
            if (string.IsNullOrEmpty(p) || !File.Exists(p))
            {
                MessageBox.Show("Chưa thấy file CSV của bản vẽ:\n" + p, "GURO", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(p) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Không mở được CSV:\n" + ex.Message, "GURO", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void MoThuMuc()
        {
            string p = DuongCsvHienTai();
            try
            {
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                    System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + p + "\"");
                else
                {
                    string dir = !string.IsNullOrEmpty(p) ? Path.GetDirectoryName(p) : (CadApplication.GetSystemVariable("DWGPREFIX") as string ?? "");
                    if (Directory.Exists(dir)) System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
                }
            }
            catch { }
        }

        private void MoFilePdf()
        {
            string p = DuongPdfHienTai();
            if (string.IsNullOrEmpty(p) || !File.Exists(p))
            {
                MessageBox.Show("Chưa thấy file PDF của bản vẽ:\n" + p, "GURO", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(p) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Không mở được PDF:\n" + ex.Message, "GURO", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void MoCaiDat()
        {
            try
            {
                var w = new GuroCaiDatWindow(new GuroCaiDatViewModel(GroRegistry.Doc()));
                CadApplication.ShowModalWindow(w);
                if (w.DaLuu) LamMoi();
            }
            catch (Exception ex)
            {
                OhtaBugBridge.Log("GRO/GuroTabViewModel", ex);
                TrangThai = "Không mở được cài đặt: " + ex.Message;
            }
        }

        private const string KhoaDaNap = "GRO_DA_NAP_AUTOLOAD";

        private void GuiLenh(string lenh)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            bool daNap = false;
            try { daNap = doc.UserData?[KhoaDaNap] is bool b && b; } catch { }
            if (!daNap && !string.IsNullOrEmpty(_thuMucModule))
            {
                string f = Path.Combine(_thuMucModule, "lisp", "autoload.lsp").Replace('\\', '/');
                string d = _thuMucModule.TrimEnd('\\', '/').Replace("\\", "\\\\");
                string nap = "(setq *gro-module-dir* \"" + d + "\") (if (findfile \"" + f + "\") (load \"" + f + "\")) (princ)";
                doc.SendStringToExecute("\x03\x03" + nap + "\n", true, false, false);
                try { if (doc.UserData != null) doc.UserData[KhoaDaNap] = true; } catch { }
                doc.SendStringToExecute(lenh + "\n", true, false, true);
                return;
            }
            doc.SendStringToExecute("\x03\x03" + lenh + "\n", true, false, true);
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
