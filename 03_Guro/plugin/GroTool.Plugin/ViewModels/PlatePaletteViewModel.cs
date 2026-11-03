using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;
#if ZWCAD
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
using CadObjectId = ZwSoft.ZwCAD.DatabaseServices.ObjectId;
#else
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using CadObjectId = Autodesk.AutoCAD.DatabaseServices.ObjectId;
#endif

namespace ZwCadPlugin.ViewModels
{
    public class PlatePaletteViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<PlateItem> _allPlates = new ObservableCollection<PlateItem>();
        private ICollectionView _filteredPlates;
        private PlateItem _selectedPlate;
        private string _searchText = string.Empty;
        private bool _filterOrphansOnly;
        private bool _locChiTamSht;
        private int _totalCount;
        private int _orphanCount;
        private bool _hasUnsavedChanges;

        public PlatePaletteViewModel()
        {
            DocCaiDatVaiTro(); // muc 159
            _filteredPlates = CollectionViewSource.GetDefaultView(_allPlates);
            _filteredPlates.Filter = FilterPlateItem;

            RefreshCommand = new RelayCommand(LamMoiVaDongBoCsv);
            SaveCommand = new RelayCommand(SaveChangesToDrawing, () => HasUnsavedChanges);
            NextOrphanCommand = new RelayCommand(SelectNextLoi, () => OrphanCount + ErrorCount > 0);
            CopyThongTinCommand = new RelayCommand(SaoChepThongTin, () => CoThongTin);
            XemDanhSachCommand = new RelayCommand(() => SelectedTabIndex = TabDanhSach);
            TaoLenhThongTin();
            // Tab Nét Chưa Đổi quét lại / chuyển layer -> tab Thông Tin theo ngay (mục 113)
            Dsn.Groups.CollectionChanged += (s, e) =>
            {
                _daQuetNet = true;
                CapNhatThongTinSong();
            };
        }

        // ---------------- TAB THONG TIN (bao cao cuoi lenh GRO, muc 105) ----------------
        private List<DongThongTin> _thongTinDong = new List<DongThongTin>();
        private string _thongTinVanBan = string.Empty;
        private string _thongTinNguon = string.Empty;
        private int _thongTinCanhBao;

        /// <summary>Các dòng báo cáo đã phân loại màu (thay hộp thoại alert cuối lệnh GRO).</summary>
        public List<DongThongTin> ThongTinDong
        {
            get => _thongTinDong;
            private set { _thongTinDong = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoThongTin)); }
        }

        public bool CoThongTin => _thongTinDong.Count > 0;

        /// <summary>"14:32:05 — 129696-1.dwg": lần chạy GRO nào đã sinh báo cáo này.</summary>
        public string ThongTinNguon
        {
            get => _thongTinNguon;
            private set { if (_thongTinNguon != value) { _thongTinNguon = value; OnPropertyChanged(); } }
        }

        /// <summary>Số dòng cảnh báo + lỗi — huy hiệu trên tiêu đề tab.</summary>
        public int ThongTinCanhBao
        {
            get => _thongTinCanhBao;
            private set { if (_thongTinCanhBao != value) { _thongTinCanhBao = value; OnPropertyChanged(); } }
        }

        public ICommand CopyThongTinCommand { get; }
        public ICommand XemDanhSachCommand { get; }

        /// <summary>Nhận báo cáo từ lisp (hàm GRO_NET_THONG_TIN), thay báo cáo cũ.</summary>
        public void DatThongTin(string baoCao, string tenBanVe)
        {
            string nguon = DateTime.Now.ToString("HH:mm:ss")
                           + (string.IsNullOrEmpty(tenBanVe) ? "" : "  —  " + tenBanVe);
            // Nhớ theo TỪNG bản vẽ: đổi qua lại giữa các bản vẽ thì tab hiện đúng báo cáo của bản vẽ đó (mục 114)
            _baoCaoTheoBanVe[CadViewService.TenBanVeHienHanh() ?? string.Empty] = Tuple.Create(baoCao ?? string.Empty, nguon);
            HienThongTin(baoCao, nguon);
        }

        // ---------- Palette dùng chung cho mọi bản vẽ đang mở (mục 114) ----------
        private readonly Dictionary<string, Tuple<string, string>> _baoCaoTheoBanVe =
            new Dictionary<string, Tuple<string, string>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Bản vẽ của dữ liệu đang nằm trong bảng (lúc quét).</summary>
        private string _banVeDuLieu;

        /// <summary>Bảng đang giữ dữ liệu của bản vẽ KHÁC bản vẽ hiện hành (chỉ xảy ra khi còn sửa chưa lưu).</summary>
        public bool LechBanVe => !string.IsNullOrEmpty(_banVeDuLieu) &&
                                 !string.Equals(_banVeDuLieu, CadViewService.TenBanVeHienHanh(), StringComparison.OrdinalIgnoreCase);

        public string ThongBaoLechBanVe => "⚠ Bảng đang là dữ liệu của bản vẽ \"" + TenFile(_banVeDuLieu) +
                                           "\" (còn sửa chưa lưu). Quay lại bản vẽ đó để Lưu, hoặc bấm Làm Mới để bỏ phần sửa và tải bản vẽ hiện tại.";

        private static string TenFile(string duong)
        {
            try { return Path.GetFileName(duong ?? string.Empty); }
            catch { return duong; }
        }

        /// <summary>
        /// Bản vẽ hiện hành vừa đổi. Tab Thông Tin chuyển sang báo cáo của bản vẽ mới (chưa chạy GRO
        /// thì để trống). Trả true nếu nên quét lại bảng; còn sửa chưa lưu thì GIỮ bảng cũ
        /// (không mất công sửa) và hiện cảnh báo + chặn Lưu nhầm sang bản vẽ mới.
        /// </summary>
        public bool DoiBanVe()
        {
            string moi = CadViewService.TenBanVeHienHanh() ?? string.Empty;
            if (_baoCaoTheoBanVe.TryGetValue(moi, out var bc))
                HienThongTin(bc.Item1, bc.Item2);
            else
                HienThongTin(string.Empty, string.Empty);
            OnPropertyChanged(nameof(LechBanVe));
            OnPropertyChanged(nameof(ThongBaoLechBanVe));
            return !HasUnsavedChanges;
        }

        private void HienThongTin(string baoCao, string nguon)
        {
            var dong = ThongTinGro.PhanDong(baoCao ?? string.Empty);
            // Nút Sao chép chép đúng bản ĐÃ DỊCH đang hiển thị (mục 107).
            _thongTinVanBan = string.Join("\n", dong.Select(d => d.Text));
            _demDongBaoCao = ThongTinGro.DemCanhBao(dong);
            BaoCao = BaoCaoGro.Doc(baoCao ?? string.Empty);
            ThongTinNguon = nguon ?? string.Empty;
            ThongTinDong = dong;
        }

        // ---------- Tab Thông Tin dạng BẢNG ĐIỀU KHIỂN (mục 108) ----------
        private BaoCaoGro _baoCao = new BaoCaoGro();

        /// <summary>Báo cáo đã đọc thành số liệu. DocDuoc = false -> tab hiện dạng dòng chữ cũ.</summary>
        public BaoCaoGro BaoCao
        {
            get => _baoCao;
            private set
            {
                _baoCao = value ?? new BaoCaoGro();
                OnPropertyChanged();
                foreach (string ten in new[]
                {
                    nameof(CoBangDieuKhien), nameof(TheGhiChu), nameof(TheTam), nameof(TheNetDaDoi), nameof(TheChuaDoi),
                    nameof(ChuaDoiCanXem), nameof(DongGhiChu), nameof(DongNetDaDoi), nameof(NhomNetChuaDoi),
                    nameof(KhoiCanhBao), nameof(CoCanhBao), nameof(CoXuLyNet), nameof(TieuDeXuLyNet),
                    nameof(NetDaDoiHet), nameof(NetKhongTimThay), nameof(CoCsv), nameof(NhanCsv), nameof(DuongCsvNgan),
                    nameof(CsvLoi), nameof(CoPdf), nameof(NhanPdf), nameof(CoFilePdf),
                })
                    OnPropertyChanged(ten);
                CapNhatThongTinSong();
            }
        }

        public bool CoBangDieuKhien => _baoCao.DocDuoc;
        public string TheGhiChu => So(_baoCao.SoGhiChu);
        public string TheTam => So(_baoCao.SoTam);
        public string TheNetDaDoi => _baoCao.Net == null || _baoCao.Net.KhongTimThay ? "—" : _baoCao.Net.DaDoi.ToString();

        // ---------- Số liệu SỐNG (mục 113) ----------
        // Báo cáo lisp chỉ là ảnh chụp lúc GRO chạy xong. Nét chưa đổi và hình cô đơn / text
        // thiếu hình thì người dùng còn xử lý tiếp -> lấy theo tab Nét Chưa Đổi / Danh Sách Tấm
        // (đã quét ít nhất một lần), không lấy theo báo cáo nữa.
        private bool _daQuetTam;
        private bool _daQuetNet;
        private int _soKhongHinh;
        private int _demDongBaoCao;

        private int SoNetSong => Dsn.Groups.Sum(g => g.Count);

        public string TheChuaDoi => _daQuetNet ? SoNetSong.ToString()
                                  : _baoCao.Net == null || _baoCao.Net.KhongTimThay ? "—" : _baoCao.Net.ConLai.ToString();
        /// <summary>Thẻ "Chưa đổi" tô cam khi còn nét chưa đổi.</summary>
        public bool ChuaDoiCanXem => _daQuetNet ? SoNetSong > 0 : _baoCao.Net != null && _baoCao.Net.ConLai > 0;

        /// <summary>Số text thông tin (tấm) không tìm thấy hình cắt — tính theo Danh Sách Tấm.</summary>
        public int SoKhongHinh
        {
            get => _soKhongHinh;
            private set { if (_soKhongHinh != value) { _soKhongHinh = value; OnPropertyChanged(); } }
        }

        /// <summary>Thẻ "Cô đơn": hình không có text + text không có hình. Chưa quét bảng -> "—".</summary>
        public string TheCoDon => _daQuetTam ? (OrphanCount + SoKhongHinh).ToString() : "—";
        public bool CoDonCanXem => _daQuetTam && OrphanCount + SoKhongHinh > 0;
        public bool CoTamCanXuLy => _daQuetTam;
        public bool CoHinhCoDon => _daQuetTam && OrphanCount > 0;
        public bool CoTextThieuHinh => _daQuetTam && SoKhongHinh > 0;
        public bool TamDatHet => _daQuetTam && OrphanCount == 0 && SoKhongHinh == 0;

        public List<DongTomTat> DongGhiChu => _baoCao.DongGhiChu();
        public List<DongTomTat> DongNetDaDoi => _baoCao.DongNetDaDoi();
        public List<NhomNetChuaDoi> NhomNetChuaDoi => _daQuetNet
            ? BaoCaoGro.GopNhomNet(Dsn.Groups.Select(g => new KeyValuePair<string, int>(g.Linetype, g.Count)))
            : _baoCao.Net?.Nhom ?? new List<NhomNetChuaDoi>();
        // Mục 160: khối SỐNG "ghi chú tiếng Nhật mới" (tính lại mỗi lần quét Danh Sách Tấm)
        private KhoiCanhBao _khoiGhiChuMoi;
        private KhoiCanhBao _khoiXuLySht;
        public List<KhoiCanhBao> KhoiCanhBao
        {
            get
            {
                var ds = _daQuetTam ? _baoCao.CanhBaoKhiCoTamSong() : new List<KhoiCanhBao>(_baoCao.CanhBao);
                if (_daQuetTam && _khoiGhiChuMoi != null) ds.Add(_khoiGhiChuMoi);
                if (_daQuetTam && _khoiXuLySht != null) ds.Add(_khoiXuLySht);
                return ds;
            }
        }
        public bool CoCanhBao => KhoiCanhBao.Count > 0;

        public bool CoXuLyNet => _baoCao.Net != null || (_daQuetNet && SoNetSong > 0);
        public string TieuDeXuLyNet
        {
            get
            {
                string layer = _daQuetNet && !string.IsNullOrEmpty(Dsn.CurrentLayerName) ? Dsn.CurrentLayerName : _baoCao.Net?.Layer;
                return layer == null ? "Xử lý nét" : "Xử lý nét · layer gốc \"" + layer + "\"";
            }
        }
        public bool NetKhongTimThay => _baoCao.Net != null && _baoCao.Net.KhongTimThay;
        public bool NetDaDoiHet => _daQuetNet ? CoXuLyNet && SoNetSong == 0 && !NetKhongTimThay
                                              : _baoCao.Net != null && _baoCao.Net.DaDoiHet;

        /// <summary>Báo các ô phụ thuộc số liệu sống + tính lại huy hiệu tab.</summary>
        private void CapNhatThongTinSong()
        {
            foreach (string ten in new[]
            {
                nameof(TheChuaDoi), nameof(ChuaDoiCanXem), nameof(NhomNetChuaDoi), nameof(CoXuLyNet),
                nameof(TieuDeXuLyNet), nameof(NetDaDoiHet), nameof(KhoiCanhBao), nameof(CoCanhBao),
                nameof(TheCoDon), nameof(CoDonCanXem), nameof(CoTamCanXuLy), nameof(CoHinhCoDon),
                nameof(CoTextThieuHinh), nameof(TamDatHet),
            })
                OnPropertyChanged(ten);
            // Huy hiệu = số mục cần xem trên bảng điều khiển; báo cáo không đọc được -> đếm dòng như cũ
            ThongTinCanhBao = CoBangDieuKhien
                ? KhoiCanhBao.Count + (CoHinhCoDon ? 1 : 0) + (CoTextThieuHinh ? 1 : 0) + NhomNetChuaDoi.Count
                : _demDongBaoCao;
        }

        public bool CoCsv => _baoCao.SoDongCsv.HasValue;
        public string NhanCsv => _baoCao.NhanCsv;
        public string DuongCsvNgan => BaoCaoGro.RutGonDuong(_baoCao.DuongCsv);
        public bool CsvLoi => _baoCao.CsvLoi;
        public bool CoPdf => !string.IsNullOrEmpty(_baoCao.PdfLayer);
        public string NhanPdf => "In nhanh PDF theo layer " + _baoCao.PdfLayer;
        public bool CoFilePdf => !string.IsNullOrEmpty(_baoCao.DuongPdf);

        private static string So(int? n) => n.HasValue ? n.Value.ToString() : "—";

        public ICommand MoCsvCommand { get; private set; }
        public ICommand MoThuMucCsvCommand { get; private set; }
        public ICommand MoPdfCommand { get; private set; }
        public ICommand ZoomNhomNetCommand { get; private set; }
        public ICommand XemTamLoiCommand { get; private set; }
        public ICommand ChonZoomDoiTuongCommand { get; private set; }
        public ICommand MoTuDienCommand { get; private set; }

        private void TaoLenhThongTin()
        {
            MoCsvCommand = new RelayCommand(() => MoFile(_baoCao.DuongCsv), () => !string.IsNullOrEmpty(_baoCao.DuongCsv));
            MoThuMucCsvCommand = new RelayCommand(() => MoThuMuc(_baoCao.DuongCsv), () => !string.IsNullOrEmpty(_baoCao.DuongCsv));
            MoPdfCommand = new RelayCommand(() => MoFile(_baoCao.DuongPdf), () => !string.IsNullOrEmpty(_baoCao.DuongPdf));
            ZoomNhomNetCommand = new RelayCommand(p => ZoomNhomNet(p as string));
            ChonZoomDoiTuongCommand = new RelayCommand(p => ChonZoomDoiTuong(p as KhoiCanhBao));
            MoTuDienCommand = new RelayCommand(MoTuDien);
            XemTamLoiCommand = new RelayCommand(p =>
            {
                var khoi = p as KhoiCanhBao;
                if (khoi != null && khoi.Loai == LoaiCanhBao.XuLySht)
                {
                    FilterOrphansOnly = false;
                    LocChiTamSht = true;
                }
                else
                {
                    LocChiTamSht = false;
                    FilterOrphansOnly = true;
                }
                SelectedTabIndex = TabDanhSach;
            });
        }

        private static void MoFile(string duong)
        {
            if (string.IsNullOrEmpty(duong)) return;
            if (!File.Exists(duong))
            {
                MessageBox.Show("Không thấy file:\n" + duong, "Thông tin GRO", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(duong) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không mở được file:\n" + duong + "\n\n" + ex.Message, "Thông tin GRO",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>Mở Explorer tại thư mục chứa file, chọn sẵn file đó.</summary>
        private static void MoThuMuc(string duong)
        {
            if (string.IsNullOrEmpty(duong)) return;
            try
            {
                if (File.Exists(duong))
                    System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + duong + "\"");
                else if (Directory.Exists(Path.GetDirectoryName(duong)))
                    System.Diagnostics.Process.Start("explorer.exe", "\"" + Path.GetDirectoryName(duong) + "\"");
            }
            catch
            {
                // Explorer khong mo duoc -> bo qua, khong lam hong palette
            }
        }

        /// <summary>
        /// Zoom + chọn các nét của một linetype còn nằm trên layer gốc. Dùng lại nhóm của tab
        /// Nét Chưa Đổi (cùng linetype, có thể nhiều màu -> gộp). Không thấy -> mở tab đó.
        /// </summary>
        private void ZoomNhomNet(string linetype)
        {
            if (string.IsNullOrEmpty(linetype)) return;
            Func<List<UnconvertedGroupItem>> tim = () => Dsn.Groups
                .Where(g => string.Equals(g.Linetype, linetype, StringComparison.OrdinalIgnoreCase) && g.EntityIds.Count > 0)
                .ToList();
            var nhom = tim();
            if (nhom.Count == 0)
            {
                Dsn.RefreshFromDrawing();
                nhom = tim();
            }
            if (nhom.Count == 0)
            {
                SelectedTabIndex = TabNet;
                return;
            }
            var hop = nhom[0].BoundingBox;
            foreach (var g in nhom.Skip(1)) hop.AddExtents(g.BoundingBox);
            CadViewService.ZoomToExtents(hop, 0.25);
            CadViewService.HighlightEntities(nhom.SelectMany(g => g.EntityIds).ToList());
        }

        /// <summary>
        /// Mục 134: chọn + zoom tới các đối tượng của một khối cảnh báo (vd đối tượng còn sót tại
        /// vị trí cụm cũ sau khi xếp cụm). Không còn đối tượng nào (đã xoá / sửa tay) -> báo.
        /// </summary>
        private static void ChonZoomDoiTuong(KhoiCanhBao khoi)
        {
            if (khoi == null || khoi.Handles.Count == 0) return;
            int n = CadViewService.ChonVaZoomTheoHandle(khoi.Handles);
            if (n == 0)
                MessageBox.Show("Không còn đối tượng nào trong bản vẽ (đã xoá hoặc đã chạy lại GRO).",
                                "Thông tin GRO", MessageBoxButton.OK, MessageBoxImage.Information);
            else if (n < khoi.Handles.Count)
                CadViewService.GhiDongLenh($"[DST] Da chon {n}/{khoi.Handles.Count} doi tuong (cac doi tuong con lai da bi xoa).");
        }

        /// <summary>
        /// Mục 160: mở Từ điển ghi chú (thêm nghĩa cho ghi chú mới); đóng xong quét lại Danh Sách
        /// Tấm để dịch theo từ điển mới — trừ khi còn sửa chưa lưu (không ghi đè bảng).
        /// </summary>
        private void MoTuDien()
        {
            try
            {
                var dlg = new Views.NoteDictionaryWindow();
                CadApplication.ShowModalWindow(dlg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không mở được Từ điển ghi chú:\n" + ex.Message, "Thông tin GRO", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!HasUnsavedChanges) RefreshFromDrawing();
        }

        private void SaoChepThongTin()
        {
            try
            {
                Clipboard.SetText(_thongTinVanBan.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));
            }
            catch
            {
                // Clipboard dang bi ung dung khac giu -> bo qua, khong lam hong palette
            }
        }

        public ICollectionView FilteredPlates => _filteredPlates;

        public ObservableCollection<PlateItem> AllPlates => _allPlates;

        public PlateItem SelectedPlate
        {
            get => _selectedPlate;
            set
            {
                if (_selectedPlate != value)
                {
                    _selectedPlate = value;
                    OnPropertyChanged();

                    if (_selectedPlate != null)
                    {
                        ZoomToPlate(_selectedPlate);
                    }
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    _filteredPlates.Refresh();
                }
            }
        }

        /// <summary>
        /// Mục 159: ô tích "Bản chính/nghịch" — hiện / ẩn dòng vai trò cặp đối xứng trong cột Ghi chú.
        /// Nhớ lựa chọn trong HKCU\Software\OFT_Lisp\GRO (dst_hien_vai_tro).
        /// </summary>
        public bool HienBanChinhNghich
        {
            get => PlateItem.HienVaiTro;
            set
            {
                if (PlateItem.HienVaiTro != value)
                {
                    PlateItem.HienVaiTro = value;
                    GhiCaiDatVaiTro(value);
                    foreach (var p in _allPlates) p.LamMoiGhiChu();
                    OnPropertyChanged();
                }
            }
        }

        private const string KhoaCaiDatGro = @"Software\OFT_Lisp\GRO";

        private static void DocCaiDatVaiTro()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KhoaCaiDatGro))
                {
                    object v = k?.GetValue("dst_hien_vai_tro");
                    if (v != null) PlateItem.HienVaiTro = v.ToString().Trim() != "0";
                }
            }
            catch
            {
                // Khong doc duoc registry -> giu mac dinh (hien)
            }
        }

        private static void GhiCaiDatVaiTro(bool hien)
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KhoaCaiDatGro))
                {
                    k?.SetValue("dst_hien_vai_tro", hien ? "1" : "0");
                }
            }
            catch
            {
                // Khong ghi duoc registry -> chi co hieu luc trong phien nay
            }
        }

        public bool FilterOrphansOnly
        {
            get => _filterOrphansOnly;
            set
            {
                if (_filterOrphansOnly != value)
                {
                    _filterOrphansOnly = value;
                    OnPropertyChanged();
                    _filteredPlates.Refresh();
                }
            }
        }

        public bool LocChiTamSht
        {
            get => _locChiTamSht;
            set
            {
                if (_locChiTamSht != value)
                {
                    _locChiTamSht = value;
                    OnPropertyChanged();
                    _filteredPlates?.Refresh();
                }
            }
        }

        public DsnViewModel Dsn { get; } = new DsnViewModel();

        // Thu tu tab tren palette (muc 105). Doi thu tu tab trong XAML thi PHAI sua ca day.
        public const int TabThongTin = 0;
        public const int TabDanhSach = 1;
        public const int TabNet = 2;

        private int _selectedTabIndex = TabDanhSach;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();

                    if (_selectedTabIndex == TabNet && Dsn.Groups.Count == 0)
                    {
                        Dsn.RefreshFromDrawing();
                    }
                }
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            private set { if (_totalCount != value) { _totalCount = value; OnPropertyChanged(); } }
        }

        private string _thongKe = string.Empty, _thongKeChiTiet = string.Empty;

        /// <summary>Thanh trạng thái MỘT DÒNG: tổng, tấm đơn, từng mã gia công, lỗi, cô đơn.</summary>
        public string ThongKe
        {
            get => _thongKe;
            private set { if (_thongKe != value) { _thongKe = value; OnPropertyChanged(); } }
        }

        /// <summary>Bản đầy đủ (tên từng loại gia công) cho tooltip.</summary>
        public string ThongKeChiTiet
        {
            get => _thongKeChiTiet;
            private set { if (_thongKeChiTiet != value) { _thongKeChiTiet = value; OnPropertyChanged(); } }
        }

        public int OrphanCount
        {
            get => _orphanCount;
            private set { if (_orphanCount != value) { _orphanCount = value; OnPropertyChanged(); } }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set { if (_hasUnsavedChanges != value) { _hasUnsavedChanges = value; OnPropertyChanged(); } }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand NextOrphanCommand { get; }

        public void RefreshFromDrawing()
        {
            var scanned = CadDrawingScanner.ScanPlates();

            _allPlates.Clear();
            foreach (var item in scanned)
            {
                item.PropertyChanged += Item_PropertyChanged;
                _allPlates.Add(item);
            }

            _daQuetTam = true;
            _banVeDuLieu = CadViewService.TenBanVeHienHanh();
            OnPropertyChanged(nameof(LechBanVe));
            OnPropertyChanged(nameof(ThongBaoLechBanVe));
            UpdateStatistics();
            HasUnsavedChanges = false;
            _filteredPlates.Refresh();
        }

        /// <summary>
        /// Nút "⟳ Làm Mới": quét lại bản vẽ rồi ĐỒNG BỘ CSV theo bản vẽ (mục 106, 107):
        ///   - tấm đã XOÁ khỏi bản vẽ (mất TEXT bango) mà CSV còn dòng  -> xoá dòng;
        ///   - tấm CÓ trên bản vẽ mà CSV chưa có dòng                  -> thêm dòng (đúng
        ///     kiểu c:gro ghi, chèn theo thứ tự bango).
        /// Hỏi MỘT lần cho cả hai. CHỈ nút này mới sửa CSV: mở palette / cuối lệnh GRO chỉ quét.
        /// </summary>
        public void LamMoiVaDongBoCsv()
        {
            RefreshFromDrawing();
            if (DongBoCsvVoiBanVe()) RefreshFromDrawing();
        }

        /// <summary>Một dòng sắp thêm vào CSV, dựng từ F-name của tấm trên bản vẽ.</summary>
        private sealed class DongThem
        {
            public string Bango, Q, M, T, GC;
        }

        /// <summary>
        /// Dựng dòng CSV cho tấm chưa có trong CSV. Thiếu / sai Q hoặc T (thường là thiếu
        /// F-name, hoặc đổi số bango mà F-name N= còn số cũ) -> null + lý do: KHÔNG thêm dòng
        /// rác vào CSV.
        /// </summary>
        private static DongThem TaoDongThem(PlateItem p, string mauT, out string lyDo)
        {
            lyDo = null;
            if (!KiemTraTam.TryDocSo(p.Quantity, out double q) || q <= 0 || Math.Abs(q - Math.Round(q)) > 1e-9)
            {
                lyDo = "Q \"" + p.Quantity + "\"";
                return null;
            }
            if (!KiemTraTam.TryDocSo(p.Thickness, out double t) || t <= 0)
            {
                lyDo = "T \"" + p.Thickness + "\"";
                return null;
            }
            string tCsv = CsvGro.DinhDangT(t.ToString(System.Globalization.CultureInfo.InvariantCulture), mauT);
            return new DongThem
            {
                Bango = p.Bango.Trim(),
                Q = ((long)Math.Round(q)).ToString(System.Globalization.CultureInfo.InvariantCulture),
                M = CsvGro.VatLieuCsv(tCsv, p.Material),
                T = tCsv,
                GC = p.GC,
            };
        }

        private static string NoiSo(IList<string> ds, int toiDa = 30) =>
            string.Join(", ", ds.Take(toiDa)) + (ds.Count > toiDa ? " … (+" + (ds.Count - toiDa) + ")" : "");

        /// <returns>true nếu CSV đã được sửa.</returns>
        private bool DongBoCsvVoiBanVe()
        {
            const string tieuDe = "Làm mới — đồng bộ CSV";
            var tams = _allPlates.Where(p => !p.IsOrphan && GroText.LaBangoHopLe(p.Bango)).ToList();
            var bango = tams.Select(p => p.Bango).ToList();
            // Không quét được tấm nào: nhiều khả năng nhầm bản vẽ / quét hỏng -> KHÔNG được
            // coi cả file CSV là "thừa".
            if (bango.Count == 0) return false;

            string duongCsv = CsvService.DuongDan();
            CsvGro csv = CsvService.TryDoc(duongCsv, out _);
            // Chưa có file CSV: để lệnh GRO tạo (GRO ghi cả tên / định dạng chuẩn).
            if (csv == null) return false;

            // --- tấm đã xoá: dòng CSV thừa ---
            var soXoa = csv.DongThua(bango).Select(i => CsvGro.BangoCua(csv.Dong[i])).Distinct().ToList();

            // --- tấm mới: chưa có dòng CSV (trùng bango trên bản vẽ thì chỉ xét một lần) ---
            string mauT = csv.MauT();
            var them = new List<DongThem>();
            var boQua = new List<string>();
            foreach (PlateItem p in tams.Where(p => csv.TimDong(p.Bango) < 0)
                                        .GroupBy(p => p.Bango.Trim()).Select(g => g.First()))
            {
                DongThem d = TaoDongThem(p, mauT, out string lyDo);
                if (d != null) them.Add(d);
                else boQua.Add(p.Bango + " (" + lyDo + ")");
            }

            if (soXoa.Count == 0 && them.Count == 0)
            {
                if (boQua.Count > 0)
                    CadViewService.GhiDongLenh("[DST] " + boQua.Count + " tam chua co dong CSV nhung thieu Q/T (thieu F-name?) - khong them: "
                                               + string.Join(", ", boQua.Select(b => b.Split(' ')[0])));
                return false;
            }

            string hoi = "";
            if (them.Count > 0)
                hoi += "THÊM " + them.Count + " dòng — tấm có trên bản vẽ nhưng CSV chưa có:\n"
                     + "    bango " + NoiSo(them.Select(d => d.Bango).ToList()) + "\n"
                     + "    (Q / T / vật liệu lấy từ F-name; cột M theo quy tắc của GRO: T > 3.2 → SS400)\n\n";
            if (soXoa.Count > 0)
                hoi += "XOÁ " + soXoa.Count + " dòng — tấm KHÔNG còn trên bản vẽ:\n"
                     + "    bango " + NoiSo(soXoa) + "\n\n";
            if (boQua.Count > 0)
                hoi += "Bỏ qua (chưa có dòng CSV nhưng thiếu dữ liệu F-name): " + NoiSo(boQua, 10) + "\n\n";
            if (soXoa.Count > 0 && (them.Count > 0 || boQua.Count > 0))
                hoi += "Lưu ý: vừa có tấm mới vừa có tấm mất. Nếu bạn chỉ ĐỔI SỐ bango trực tiếp\n"
                     + "trên bản vẽ thì nên chọn No — xoá dòng cũ sẽ mất Q / M / T / GC đã sửa trong CSV.\n\n";
            hoi += "Cập nhật file CSV?\n" + duongCsv;

            if (MessageBox.Show(hoi, tieuDe, MessageBoxButton.YesNo, MessageBoxImage.Question,
                                MessageBoxResult.No) != MessageBoxResult.Yes)
                return false;

            var daHoiXoa = new HashSet<string>(soXoa);
            int daXoa, daThem = 0;
            try
            {
                using (FileStream fs = CsvService.MoDeGhi(duongCsv))
                {
                    // Đọc LẠI trong lúc giữ khoá (file có thể vừa đổi sau lần đọc để hỏi) và chỉ
                    // làm đúng những bango người dùng đã đồng ý.
                    CsvGro moi = CsvGro.Doc(CsvService.DocHet(fs));
                    List<int> xoa = moi.DongThua(bango).Where(i => daHoiXoa.Contains(CsvGro.BangoCua(moi.Dong[i]))).ToList();
                    daXoa = xoa.Count;
                    moi.XoaDong(xoa);

                    string don = CsvService.TenDonBanVe();
                    foreach (DongThem d in them.OrderBy(d => int.TryParse(d.Bango, out int n) ? n : int.MaxValue))
                    {
                        if (moi.TimDong(d.Bango) >= 0) continue; // vua co nguoi khac them
                        moi.ChenTheoBango(CsvGro.TaoDong(don, d.Bango, d.Q, d.M, d.T, d.GC));
                        daThem++;
                    }
                    if (daXoa == 0 && daThem == 0) return false;
                    CsvService.GhiDe(fs, moi.Ghi());
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show("CHƯA CẬP NHẬT — không mở được file CSV để ghi:\n" + duongCsv + "\n\n" +
                                "Thường là vì file đang MỞ trong Excel. Đóng file rồi bấm Làm Mới lại.\n\n(" + ex.Message + ")",
                                tieuDe, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            CadViewService.GhiDongLenh("[DST] Dong bo CSV: them " + daThem + " dong, xoa " + daXoa + " dong.");
            return true;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlateItem.IsModified):
                    HasUnsavedChanges = _allPlates.Any(p => p.IsModified);
                    break;
                case nameof(PlateItem.Bango):
                    CapNhatTrung();
                    break;
                case nameof(PlateItem.GC):
                    UpdateStatistics();
                    break;
                case nameof(PlateItem.Loi):
                    // Chi cap nhat so dem. KHONG Refresh bo loc o day: dang sua o thi Refresh
                    // nem loi, va dong vua sua het loi se bien mat khoi tay nguoi dung.
                    UpdateStatistics();
                    break;
            }
        }

        /// <summary>
        /// Lưu vào bản vẽ VÀ file CSV của GRO.
        ///   1. Kiểm tra (bango hợp lệ, không trùng, T/Q/M đúng định dạng) — lỗi thì dừng.
        ///   2. Mở CSV ĐỘC QUYỀN trước khi đụng bản vẽ: Excel đang giữ file thì dừng ngay,
        ///      không có gì bị đổi (tránh bản vẽ một đằng, CSV một nẻo — mục 27).
        ///   3. Ghi bản vẽ (một transaction). Hỏng thì không ghi CSV.
        ///   4. Ghi CSV: chỉ những ô thật sự bị sửa; tìm dòng theo bango LÚC QUÉT.
        ///   5. Quét lại để bảng khớp đúng với bản vẽ + CSV.
        /// </summary>
        public void SaveChangesToDrawing()
        {
            const string tieuDe = "Danh sách tấm (DST)";
            // Bảng của bản vẽ A mà bản vẽ hiện hành là B: Lưu sẽ ghi nhầm vào bản vẽ B và CSV của B (mục 114)
            if (LechBanVe)
            {
                MessageBox.Show("CHƯA LƯU — bảng đang là dữ liệu của bản vẽ:\n" + _banVeDuLieu + "\n\n" +
                                "Bản vẽ hiện hành là bản vẽ khác. Quay lại bản vẽ đó rồi bấm Lưu\n" +
                                "(hoặc bấm Làm Mới để bỏ phần sửa và tải bản vẽ hiện tại).",
                                tieuDe, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dong = _allPlates.Select(p => new DongBang
            {
                GocBango = p.GocBango, Bango = p.Bango, T = p.Thickness, Q = p.Quantity, M = p.Material, GC = p.GC,
                LaCoDon = p.IsOrphan, DaSua = p.IsModified,
            }).ToList();
            List<string> loi = BangSoLieu.KiemTraTruocKhiLuu(dong);
            if (loi.Count > 0)
            {
                MessageBox.Show("CHƯA LƯU — sửa các lỗi sau rồi bấm Lưu lại:\n\n" + GopDong(loi, 15),
                                tieuDe, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sua = _allPlates.Where(p => p.IsModified && !p.IsOrphan).ToList();
            if (sua.Count == 0) return;

            // Danh sach sua CSV phai lap TRUOC khi ghi ban ve: ghi xong thi gia tri goc bi chup lai.
            var dsCsv = sua.Select(p => new SuaCsv
            {
                GocBango = p.GocBango,
                BangoMoi = p.DaSuaBango ? p.Bango.Trim() : null,
                Q = p.DaSuaQuantity ? p.Quantity : null,
                M = p.DaSuaMaterial ? p.Material : null,
                T = p.DaSuaThickness ? p.Thickness : null,
                GC = p.DaSuaGC ? CsvGro.ChuanHoaGC(p.GC) : null,
            }).ToList();

            string duongCsv = CsvService.DuongDan();
            FileStream fs = null;
            CsvGro csv = null;
            if (File.Exists(duongCsv))
            {
                try
                {
                    fs = CsvService.MoDeGhi(duongCsv);
                    csv = CsvGro.Doc(CsvService.DocHet(fs));
                }
                catch (Exception ex)
                {
                    fs?.Dispose();
                    MessageBox.Show("CHƯA LƯU — không mở được file CSV để ghi:\n" + duongCsv + "\n\n" +
                                    "Thường là do file đang mở trong Excel. Đóng file đó rồi bấm Lưu lại.\n" +
                                    "Bản vẽ CHƯA bị thay đổi.\n\n(" + ex.Message + ")",
                                    tieuDe, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else if (MessageBox.Show("Không thấy file CSV của bản vẽ:\n" + duongCsv + "\n\n" +
                                     "Vẫn lưu vào BẢN VẼ (không có CSV để cập nhật)?",
                                     tieuDe, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            int updated, nCsv = 0;
            List<string> canhBao;
            bool csvLoi = false;
            using (fs)
            {
                updated = CadEntityUpdater.SavePlateChanges(_allPlates, out canhBao);
                if (updated > 0 && csv != null)
                {
                    try
                    {
                        nCsv = csv.ApDung(dsCsv, canhBao);
                        CsvService.GhiDe(fs, csv.Ghi());
                    }
                    catch (Exception ex)
                    {
                        csvLoi = true;
                        canhBao.Add("Bản vẽ ĐÃ lưu nhưng ghi CSV THẤT BẠI: " + ex.Message +
                                    " — CSV đang lệch bản vẽ. Sửa tay CSV, hoặc UNDO bản vẽ rồi lưu lại.");
                    }
                }
            }

            if (csv == null && dsCsv.Any(d => d.GC != null))
                canhBao.Add("GC chỉ lưu trong CSV — không có file CSV nên GC CHƯA được lưu.");

            if (updated > 0) RefreshFromDrawing();

            string tb = "Đã ghi " + updated + " tấm vào bản vẽ.\n" +
                        (csv == null ? "Không có file CSV — chỉ lưu bản vẽ."
                         : csvLoi ? "CSV: GHI THẤT BẠI (xem bên dưới)."
                         : "Đã cập nhật " + nCsv + " dòng trong CSV:\n" + duongCsv);
            if (canhBao.Count > 0) tb += "\n\nCần xem lại:\n" + GopDong(canhBao, 15);
            MessageBox.Show(tb, tieuDe, MessageBoxButton.OK,
                            canhBao.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        private static string GopDong(List<string> ds, int toiDa)
        {
            string s = string.Join("\n", ds.Take(toiDa).Select(x => "• " + x));
            if (ds.Count > toiDa) s += "\n… và " + (ds.Count - toiDa) + " mục khác.";
            return s;
        }

        /// <summary>
        /// Zoom tới CHÍNH tấm (hộp bango + mã khắc + F-name, nới rộng gấp 3 để thấy chi tiết
        /// xung quanh) và bật grips cho đúng các chữ của tấm đó. Hình cô đơn thì zoom tới
        /// polyline cảnh báo của nó.
        /// </summary>
        public void ZoomToPlate(PlateItem plate)
        {
            if (plate == null) return;

            // Uu tien zoom toi khung in Khung_IN cua tam (neu co) theo yeu cau nguoi dung
            if (plate.FrameBoundingBox.HasValue)
            {
                CadViewService.ZoomToExtents(plate.FrameBoundingBox.Value, 0.15);
            }
            else
            {
                CadViewService.ZoomToExtents(plate.BoundingBox, plate.IsOrphan ? 0.3 : 2.0);
            }

            var ids = new List<CadObjectId>();
            // Khung Khung_IN KHONG bat grips: no co the la ca to ban ve, che mat tam dang chon.
            if (plate.IsOrphan && !plate.FrameId.IsNull) ids.Add(plate.FrameId);
            if (!plate.BangoTextId.IsNull) ids.Add(plate.BangoTextId);
            if (!plate.MaKhacTextId.IsNull) ids.Add(plate.MaKhacTextId);
            ids.AddRange(plate.FnameTextIds);
            CadViewService.HighlightEntities(ids);
        }

        /// <summary>Nhảy vòng qua các dòng ĐỎ (tấm lỗi + hình cô đơn), theo thứ tự đang hiển thị.</summary>
        private void SelectNextLoi()
        {
            var loi = _filteredPlates.Cast<PlateItem>().Where(p => p.MucDo == 2).ToList();
            if (loi.Count == 0) loi = _allPlates.Where(p => p.MucDo == 2).ToList();
            if (loi.Count == 0) return;

            int currentIndex = SelectedPlate != null ? loi.IndexOf(SelectedPlate) : -1;
            SelectedPlate = loi[(currentIndex + 1) % loi.Count];
        }

        private bool FilterPlateItem(object obj)
        {
            if (!(obj is PlateItem plate)) return false;

            if (FilterOrphansOnly && plate.MucDo != 2) return false;
            if (LocChiTamSht && !plate.LaXuLySht) return false;

            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            string query = SearchText.Trim();
            return (plate.Bango != null && plate.Bango.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (plate.Thickness != null && plate.Thickness.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (plate.Quantity != null && plate.Quantity.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (plate.Material != null && plate.Material.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (!string.IsNullOrEmpty(plate.GC) && plate.GC.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>Đếm theo cột GC (cột V của CSV) — mỗi mã một số riêng.</summary>
        private void UpdateStatistics()
        {
            var tam = _allPlates.Where(p => !p.IsOrphan).ToList();
            TotalCount = tam.Count;
            OrphanCount = _allPlates.Count(p => p.IsOrphan);
            ErrorCount = tam.Count(p => !string.IsNullOrEmpty(p.Loi));
            ThongKe = CsvGro.ThongKe(tam.Select(p => p.GC), ErrorCount, OrphanCount, out string ct);
            ThongKeChiTiet = ct;
            SoKhongHinh = tam.Count(p => p.NoCuttingShape);
            _khoiGhiChuMoi = BaoCaoGro.KhoiGhiChuMoi(tam.Select(p => new KeyValuePair<string, string>(p.Bango, p.CustomerNote)));
            _khoiXuLySht = BaoCaoGro.KhoiCanhBaoSht(tam.Select(p => new KeyValuePair<string, string>(p.Bango, p.CustomerNote)));
            CapNhatThongTinSong();
        }

        private int _errorCount;

        /// <summary>Số tấm có lỗi dữ liệu (không tính hình cô đơn).</summary>
        public int ErrorCount
        {
            get => _errorCount;
            private set { if (_errorCount != value) { _errorCount = value; OnPropertyChanged(); } }
        }

        /// <summary>Đánh dấu lại các tấm trùng bango — gọi mỗi khi có bango đổi.</summary>
        private void CapNhatTrung()
        {
            var trung = BangSoLieu.BangoTrung(_allPlates.Where(p => !p.IsOrphan).Select(p => p.Bango));
            foreach (PlateItem p in _allPlates.Where(p => !p.IsOrphan))
                p.TrungBango = trung.Contains((p.Bango ?? string.Empty).Trim());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
