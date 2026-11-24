using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using CsvTool.CadServices;
using CsvTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using Autodesk_ObjectId = ZwSoft.ZwCAD.DatabaseServices.ObjectId;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk_ObjectId = Autodesk.AutoCAD.DatabaseServices.ObjectId;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace CsvTool.ViewModels
{
    public sealed class Lenh : ICommand
    {
        private readonly Action _a;
        public Lenh(Action a) { _a = a; }
        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _a();
    }

    /// <summary>
    /// Tab CSV gồm 4 tab con. Lệnh CSV mở tab Home (CSVTHIETLAP), 2 nút quét phân tích hình học bằng C#: thông tin tấm lưu ở
    /// data\&lt;đơn&gt;-data.csv (không để text trên bản vẽ). Sửa ô: N/Q/M/T ghi F-name, còn lại ghi data CSV.
    /// File cuối &lt;đơn&gt;.csv tự cập nhật sau khi quét và sau mỗi thay đổi trên tab.
    /// </summary>
    public sealed class CsvTabViewModel : INotifyPropertyChanged
    {
        public static CsvTabViewModel HienTai { get; private set; }

        public string TieuDe { get; }
        public string MoTa { get; }
        public string ThuMucModule { get; }
        public Brush AccentBrush { get; }
        public CaiDatCsv CaiDat { get; }

        public ObservableCollection<DongTam> DongHien { get; } = new ObservableCollection<DongTam>();
        private List<DongTam> _tatCa = new List<DongTam>();
        /// <summary>Danh sách tổng hợp lỗi + cảnh báo của bản vẽ đang quét.</summary>
        public ObservableCollection<DongLoi> DsLoi { get; } = new ObservableCollection<DongLoi>();
        public bool KhongCoLoi => DsLoi.Count == 0;
        /// <summary>Giá vật liệu trong đơn, mỗi cặp vật liệu + độ dày 1 dòng.</summary>
        public ObservableCollection<DongGiaVl> DsGiaVl { get; } = new ObservableCollection<DongGiaVl>();
        public static readonly string[] DsCheDoTinh = { "Thường", "Chỉ gia công", "VL khách cấp" };

        private int _tabDangChon;
        public int TabDangChon { get => _tabDangChon; set { _tabDangChon = value; Bao(); } }

        public string NhanTabChung => CoDanhSach ? "Đã quét" : "Chưa quét";
        public string NhanTabTam => _tatCa.Count(d => !d.BoQua) + " tấm";
        public string NhanTabLoi
        {
            get
            {
                int loi = DsLoi.Count(l => l.LaLoi), cb = DsLoi.Count - loi;
                return loi > 0 ? loi + " lỗi" : cb > 0 ? cb + " cảnh báo" : "Không có";
            }
        }
        public bool TabLoiDo => DsLoi.Count > 0;
        public string NhanTabGia
        {
            get
            {
                int thieu = DsGiaVl.Count(d => d.Thieu);
                int canBoSung = DsGiaVl.Count(d => d.ChuaCoDb);
                return thieu > 0 ? thieu + " thiếu" : canBoSung > 0 ? canBoSung + " cần bổ sung"
                    : DsGiaVl.Count == 0 || _bangGia == null ? "Chưa tính" : "Đủ giá";
            }
        }
        public bool TabGiaDo => DsGiaVl.Any(d => d.Thieu || d.ChuaCoDb);
        private const string KhoaLoiQuet = "CSVTOOL_LOI_QUET";
        private Document _docDanhSach;

        public ICommand QuetVungCmd { get; }
        public ICommand QuetToanCmd { get; }
        public ICommand CaiDatCmd { get; }
        public ICommand ChiTietCmd { get; }

        /// <summary>Nút bật/tắt chi tiết: mở hoặc thu gọn mọi thẻ tấm; thẻ mới tạo theo trạng thái này.</summary>
        private bool _chiTiet;
        public bool ChiTiet
        {
            get => _chiTiet;
            set
            {
                _chiTiet = value;
                foreach (DongTam d in _tatCa) d.MoRong = value;
                Bao(); Bao(nameof(NhanChiTiet));
            }
        }
        public string NhanChiTiet => _chiTiet ? "Xem rút gọn" : "Xem chi tiết";
        public ICommand MoFile10Cmd { get; }
        public ICommand MoFile35Cmd { get; }
        public ICommand XuatFileACmd { get; }
        public ICommand XuatPdfCmd { get; }
        public ICommand MoFileACmd { get; }
        public ICommand MoPdfCmd { get; }

        public CsvTabViewModel(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            TieuDe = tieuDe; MoTa = moTa; ThuMucModule = thuMucModule;
            try { AccentBrush = (Brush)new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(mauNhan) ? "#1E7F4F" : mauNhan); }
            catch { AccentBrush = Brushes.SeaGreen; }
            try { CaiDat = CaiDatRegistry.Doc(); } catch { CaiDat = new CaiDatCsv(); }
            DocNhanh();

            QuetVungCmd = new Lenh(() => GoiQuet(false));
            QuetToanCmd = new Lenh(() => GoiQuet(true));
            CaiDatCmd = new Lenh(MoCaiDat);
            ChiTietCmd = new Lenh(() => ChiTiet = !ChiTiet);
            MoFile10Cmd = new Lenh(() => Mo(File10));
            MoFile35Cmd = new Lenh(() => Mo(File35));
            XuatFileACmd = new Lenh(() => XuatNgay(true, false));
            XuatPdfCmd = new Lenh(() => XuatNgay(false, true));
            MoFileACmd = new Lenh(() => MoHoacBao(DuongFileA));
            MoPdfCmd = new Lenh(() => MoHoacBao(DuongPdf));

            HienTai = this;
            try { CadApplication.DocumentManager.DocumentActivated += (s, e) => DoiBanVe(e.Document); } catch { }
            try { if (CadApplication.DocumentManager.MdiActiveDocument != null) { NapThietLap(CadApplication.DocumentManager.MdiActiveDocument); NapLai(CadApplication.DocumentManager.MdiActiveDocument); } } catch { }
        }

        // ── Quét: chọn khung → PhanTichHinh → SauQuet ─────────────────

        private string _thuMuc, _maDon;

        private Document _docThietLap;
        private int _cheDo;
        private int? _soKhung;

        /// <summary>2 nút quét: chạy trong ngữ cảnh lệnh để còn kéo chọn vùng được.</summary>
        private void GoiQuet(bool toan)
        {
            LoiThietLap = KiemTruocQuet();
            if (LoiThietLap != null) return;
            try
            {
                Document doc = CadApplication.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                // không huỷ khi rảnh: ^C sẽ xoá khung người dùng đã chọn sẵn
                string huy = Convert.ToInt32(CadApplication.GetSystemVariable("CMDACTIVE")) == 0 ? "" : "\x03\x03";
                doc.SendStringToExecute(huy + (toan ? "CSVQUETTOAN\n" : "CSVQUETVUNG\n"), true, false, true);
            }
            catch (Exception ex) { LoiThietLap = "Không chạy được lệnh quét: " + ex.Message; }
        }

        private string KiemTruocQuet()
        {
            if (_thuMuc != null)
            {
                string f = CsvXuat.TepDangMo(CsvXuat.DuongCuoi(_thuMuc, _maDon), CsvXuat.DuongData(_thuMuc, _maDon), CsvXuat.DuongGiaVl(_thuMuc, _maDon));
                if (f != null) return Path.GetFileName(f) + " đang mở. Đóng file rồi bấm quét.";
            }
            if (HienDsCongTy) XongCongTy();
            if (HienDsCongTy) return LoiNhieuCongTy;
            return _giaDon != null && TinhGiaBat && string.IsNullOrEmpty(CongTy) ? "Chọn công ty, hoặc bỏ tích Tính giá." : null;
        }

        /// <summary>Lệnh CSV: mở tab Home, không đụng vào danh sách đã quét.</summary>
        public void MoThietLap(Document doc)
        {
            CloudLoiTam.Xoa();
            NapThietLap(doc);
            NapCongTyNen();
            CapNhatSoKhung(doc);
            TabDangChon = 0;
        }

        /// <summary>Lệnh của 2 nút quét.</summary>
        public void QuetTuLenh(Document doc, bool toan)
        {
            CloudLoiTam.Xoa();
            NapThietLap(doc);
            LoiThietLap = KiemTruocQuet();
            if (LoiThietLap != null) { TabDangChon = 0; return; }
            CaiDat.CheDoTinh = _cheDo;
            Luu();
            QuetCad(doc, toan, TinhGiaBat, CongTy);
        }

        /// <summary>Đọc thiết lập đã lưu theo đơn của bản vẽ. Chưa quét thì chỉ ghi gia.json, không ghi data CSV.</summary>
        private void NapThietLap(Document doc)
        {
            if (doc == _docThietLap && _giaDon != null) { CapNhatDsCongTy(); return; }
            _docThietLap = doc;
            _thuMuc = _maDon = null;
            _giaDon = null;
            _giaVl = null;
            _soKhung = null;
            File10 = File35 = null;
            LoiThietLap = null;
            if (Convert.ToInt32(CadApplication.GetSystemVariable("DWGTITLED")) == 1)
            {
                _thuMuc = Convert.ToString(CadApplication.GetSystemVariable("DWGPREFIX"));
                _maDon = MaDon.TuTenFile(Convert.ToString(CadApplication.GetSystemVariable("DWGNAME")));
                string fCuoi = CsvXuat.DuongCuoi(_thuMuc, _maDon), fData = CsvXuat.DuongData(_thuMuc, _maDon);
                File10 = File.Exists(fCuoi) ? fCuoi : null;
                File35 = File.Exists(fData) ? fData : null;
                try { _giaDon = GiaDon.DocFile(CsvXuat.DuongGia(_thuMuc, _maDon)); }
                catch (Exception) { LoiThietLap = "File " + _maDon + "-gia.json bị hỏng, chưa tính giá được."; }
                try { _giaVl = GiaVlDon.DocFile(CsvXuat.DuongGiaVl(_thuMuc, _maDon)); }
                catch (Exception) { LoiThietLap = "File " + _maDon + "-giavl.csv bị hỏng, chưa nhập được giá tạm."; }
            }
            _cheDo = _giaDon?.CheDoTinh ?? CaiDat.CheDoTinh;
            NhanBanVe = "Bản vẽ " + Path.GetFileNameWithoutExtension(doc.Name);
            CapNhatDsCongTy();
            DungGiaVl();
            BaoThietLap();
        }

        private void CapNhatSoKhung(Document doc)
        {
            try { _soKhung = QuetKhungCad.TatCaKhung(doc, CaiDat.LopKhungDung).Count; }
            catch (Exception) { _soKhung = null; }
            Bao(nameof(NhanKhung));
        }

        // ── Tab Home ───────────────────────────────────────

        private bool CoDanhSach => _docDanhSach != null && _tatCa.Any(d => d.Tam != null);

        public string NhanBanVe { get; private set; } = "Chưa có bản vẽ";
        public string NhanKhung => (_soKhung == null ? "" : _soKhung + " khung tấm · ") + (_maDon == null ? "Bản vẽ chưa lưu" : "đơn " + _maDon);

        private string _loiThietLap;
        public string LoiThietLap { get => _loiThietLap; private set { _loiThietLap = value; Bao(); Bao(nameof(CoLoiThietLap)); } }
        public bool CoLoiThietLap => _loiThietLap != null;

        public bool TinhGiaBat
        {
            get => _giaDon?.KhongTinhSauQuet != true;
            set
            {
                if (value == TinhGiaBat) return;
                SuaThietLap(() =>
                {
                    _giaDon.KhongTinhSauQuet = !value;
                    if (!value) { _bangGia = null; DungGiaVl(); }   // bỏ tích: giữ nguyên giá đang có
                });
            }
        }

        public bool LamTron250Don
        {
            get => _giaDon?.LamTron250 == true;
            set { if (value != LamTron250Don) SuaThietLap(() => _giaDon.LamTron250 = value); }
        }

        internal bool Tron250Cua(string ten) => _giaDon != null && _giaDon.Tron250(XemGia(ten));

        public bool CheDo0 { get => _cheDo == 0; set { if (value) DoiCheDo(0); } }
        public bool CheDo1 { get => _cheDo == 1; set { if (value) DoiCheDo(1); } }
        public bool CheDo2 { get => _cheDo == 2; set { if (value) DoiCheDo(2); } }
        public string MoTaCheDo => _cheDo == 2 ? "Vật liệu do khách cấp, tính 15% tiền vật liệu."
                                 : _cheDo == 1 ? "Chỉ gia công, không cắt, không cấp vật liệu, tính 15% tiền vật liệu." : "Tính đủ vật liệu, cắt và gia công.";

        /// <summary>Chế độ của đơn áp cho mọi tấm, trừ tấm đã chọn riêng trên thẻ.</summary>
        private void DoiCheDo(int m)
        {
            if (m == _cheDo) return;
            SuaThietLap(() =>
            {
                _cheDo = m;
                _giaDon.CheDoTinh = m;
                CaiDat.CheDoTinh = m;
                Luu();
                foreach (DongTam d in _tatCa.Where(x => x.Tam != null && XemGia(x.Tam.N)?.CheDoRieng != true))
                    d.Tam.Tt.CheDoTinh = m.ToString(CultureInfo.InvariantCulture);
            });
        }

        private void SuaThietLap(Action doi)
        {
            if (_giaDon == null) { LoiThietLap = "Chưa đọc được thiết lập của đơn."; BaoTre(); return; }
            if (CoDanhSach && !ChoPhepSua()) { BaoTre(); return; }
            doi();
            LuuGia();
            LoiThietLap = null;
            foreach (DongTam d in _tatCa) d.BaoGia();
            TinhLaiSauThietLap();
            BaoThietLap();
        }

        private void TinhLaiSauThietLap()
        {
            if (!CoDanhSach) return;
            if (TinhGiaBat && !string.IsNullOrEmpty(CongTy))
            {
                if (_bangGia != null) ApDungVaLuu(_bangGia, "");
                else TinhGiaSauQuet("");
            }
            else
            {
                DanhGiaLai();
                if (LuuData()) SauThayDoi();
            }
        }

        // trả ô về giá trị cũ sau khi binding xong (RadioButton, CheckBox không nhận nếu trả ngay)
        private void BaoTre() => System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(BaoThietLap));

        private void BaoThietLap()
        {
            DongBoTenCongTy();
            foreach (string t in new[] { nameof(TinhGiaBat), nameof(CongTy), nameof(CoGiaDon), nameof(CheDo0), nameof(CheDo1), nameof(CheDo2),
                                         nameof(MoTaCheDo), nameof(LamTron250Don), nameof(NhanBanVe), nameof(NhanKhung) }) Bao(t);
            BaoNhanTab();
        }

        private void BaoNhanTab()
        {
            foreach (string t in new[] { nameof(NhanTabChung), nameof(NhanTabTam), nameof(NhanTabLoi), nameof(TabLoiDo), nameof(NhanTabGia), nameof(TabGiaDo), nameof(KhongCoLoi) }) Bao(t);
        }

        // ── Tab Giá VL: giá tạm lưu ở data\<đơn>-giavl.csv ─────────────

        private GiaVlDon _giaVl;

        private HashSet<(string, double)> _vlThieu = new HashSet<(string, double)>();
        internal bool VlThieuGia(string vl, double day) => _vlThieu.Contains((vl, day));
        internal bool DaCoBangGia => _bangGia != null;
        internal BangGia BangGoc => _bangGia;
        private BangGia BangDung => _giaVl == null ? _bangGia : _giaVl.Gop(_bangGia);
        internal DongBangGia GiaTamCua(string vl, double day) => _giaVl?.Tim(vl, day);
        internal DongBangGia GiaBangCua(string vl, double day) => _bangGia?.Dong.FirstOrDefault(x => x.VatLieu == vl && x.DoDay == day);

        /// <summary>Giữ nguyên các dòng khi vật liệu không đổi, để nhập giá xong con trỏ không bị mất.</summary>
        private void DungGiaVl()
        {
            var ds = CoDanhSach ? GiaVlDon.DsVatLieu(_tatCa.Where(d => d.Tam != null && !d.BoQua).Select(d => d.Tam))
                                : new List<(string VatLieu, double DoDay, int SoTam)>();
            bool giong = DsGiaVl.Count == ds.Count && ds.All(x => DsGiaVl.Any(r => r.VatLieu == x.VatLieu && r.DoDay == x.DoDay));
            if (!giong)
            {
                DsGiaVl.Clear();
                foreach (var x in ds.OrderBy(x => _bangGia != null && GiaTamCua(x.VatLieu, x.DoDay) == null && GiaBangCua(x.VatLieu, x.DoDay) == null ? 0 : 1))
                    DsGiaVl.Add(new DongGiaVl(this, x.VatLieu, x.DoDay, x.SoTam));
            }
            foreach (DongGiaVl r in DsGiaVl)
            {
                r.SoTam = ds.First(x => x.VatLieu == r.VatLieu && x.DoDay == r.DoDay).SoTam;
                r.CapNhat();
            }
            BaoNhanTab();
        }

        /// <summary>Đổi giá tạm rồi ghi file; không ghi được thì trả lại như cũ.</summary>
        private bool GhiGiaVl(Action doi, Action hoan)
        {
            if (_giaVl == null || _thuMuc == null) { BaoLoi("Chưa đọc được file giá vật liệu của đơn."); return false; }
            if (!ChoPhepSua()) return false;
            doi();
            try { _giaVl.GhiFile(CsvXuat.DuongGiaVl(_thuMuc, _maDon)); return true; }
            catch (IOException) { hoan(); BaoLoi(_maDon + "-giavl.csv đang mở. Đóng file rồi sửa lại."); }
            catch (Exception ex) { hoan(); BaoLoi("Không ghi được giá vật liệu: " + ex.Message); }
            return false;
        }

        private void SauGiaVl()
        {
            DungGiaVl();
            if (TinhGiaBat && !string.IsNullOrEmpty(CongTy)) TinhLaiSauThietLap();
            else
            {
                GomLoi();
                TrangThai = "Đã lưu giá tạm. Tích Tính giá và chọn công ty để tính tiền.";
            }
        }

        internal void DatGiaVl(DongGiaVl d, double gia)
        {
            DongBangGia t = GiaTamCua(d.VatLieu, d.DoDay), n = t ?? new DongBangGia { VatLieu = d.VatLieu, DoDay = d.DoDay };
            double cu = n.DonGia;
            if (!GhiGiaVl(() => { n.DonGia = gia; _giaVl.Dat(n); },
                          () => { if (t == null) _giaVl.Xoa(d.VatLieu, d.DoDay); else n.DonGia = cu; })) return;
            SauGiaVl();
        }

        internal void XoaGiaVl(DongGiaVl d)
        {
            DongBangGia n = GiaTamCua(d.VatLieu, d.DoDay);
            if (n == null || !GhiGiaVl(() => _giaVl.Xoa(d.VatLieu, d.DoDay), () => _giaVl.Dat(n))) return;
            SauGiaVl();
        }

        /// <summary>Phân tích hình học các khung, một khung lỗi không làm dừng lượt quét; xong thì tính giá nếu đã chọn.</summary>
        private void QuetCad(Document doc, bool toanBanVe, bool tinhGia, string congTy)
        {
            List<Autodesk_ObjectId> ids = toanBanVe ? QuetKhungCad.TatCaKhung(doc, CaiDat.LopKhungDung) : QuetKhungCad.ChonVung(doc, CaiDat.LopKhungDung);
            if (ids == null) { TrangThai = "Đã huỷ quét."; return; }
            // OVERKILL trước khi đọc khung: handle chữ / dim đọc sau đó không trỏ vào đối tượng đã bị xoá
            string ghiChuOk;
            try { int xoa = QuetKhungCad.Overkill(doc, ids); ghiChuOk = xoa > 0 ? " OVERKILL bỏ " + xoa + " đối tượng trùng." : ""; }
            catch (Exception ex) { ghiChuOk = " Không chạy được OVERKILL: " + ex.Message; }
            var (daChon, dimzin) = QuetKhungCad.Quet(doc, ids);
            // chọn nhầm vùng trống không được xoá danh sách và file data đang có
            if (!daChon.Any(k => PhanTichKhung.CoFname(k) && !PhanTichKhung.LaKhungChuThich(k)))
            {
                TrangThai = "Vùng chọn không có khung tấm. Giữ nguyên danh sách.";
                return;
            }
            _bangGia = null;   // không tính lại bằng bảng giá cũ: có tích thì đọc mới, bỏ tích thì giữ nguyên giá

            var hinh = QuetHinhCad.Doc(doc, daChon.Where(k => PhanTichKhung.SoDongN(k) == 1).ToList(), CaiDat);
            var quet = new Dictionary<long, ThongTinTam>();
            _hinhQuet.Clear();
            var loiMoi = new List<LoiQuet>();
            foreach (DuLieuHinh d in hinh)
            {
                try
                {
                    KetQuaHinh r = PhanTichHinh.Chay(d, CaiDat.DungSaiKheHo, CaiDat.CheDoTinh, (ten, soDoan) => QuetHinhCad.HoiSoNhan(doc, ten, soDoan), TaoFileA);
                    if (r.Tam != null) quet[d.HandleKhung] = r.Tam;
                    _hinhQuet[d.HandleKhung] = (d, r);
                    loiMoi.AddRange(r.Loi);
                    loiMoi.AddRange(r.CanhBao);
                }
                catch (Exception ex)
                {
                    loiMoi.Add(new LoiQuet { Handle = d.HandleKhung, LaLoi = true, NoiDung = "Không phân tích được: " + ex.Message });
                }
            }
            var hinhKhung = hinh.ToDictionary(x => x.HandleKhung);
            bool daQuet = SauQuet(doc, daChon, dimzin, quet, loiMoi, hinhKhung);
            TrangThai += ghiChuOk;
            if (daQuet) TaoNhanh(doc);
            CapNhatSoKhung(doc);
            BaoThietLap();
            if (_tatCa.Count > 0) TabDangChon = 1;
            if (daQuet && tinhGia && _giaDon != null)
            {
                _giaDon.CongTy = congTy;
                TinhGiaSauQuet(TrangThai + " ");
            }
            else if (daQuet) XuatTuDong(TrangThai + " ");
        }

        /// <summary>
        /// Danh sách mới chỉ gồm các khung vừa quét (khung quét nhầm, tấm thừa của lượt trước biến mất).
        /// Thông tin tấm lấy từ kết quả vừa phân tích, số đã nhập lấy từ data CSV. Không tạo / xoá gì trên bản vẽ.
        /// </summary>
        /// <param name="chiNap">Mở / quay lại bản vẽ: dựng danh sách từ file đã xuất, không ghi đè file nào.</param>
        private bool SauQuet(Document doc, List<KhungTho> khung, int dimzin, Dictionary<long, ThongTinTam> quet, List<LoiQuet> loiMoi, Dictionary<long, DuLieuHinh> hinhKhung,
                             bool chiNap = false)
        {
            _docDanhSach = _docThietLap = doc;
            if (Convert.ToInt32(CadApplication.GetSystemVariable("DWGTITLED")) != 1)
            {
                _thuMuc = null;
                TrangThai = "Bản vẽ chưa lưu — lưu bản vẽ rồi quét lại (data CSV nằm cạnh file bản vẽ).";
                return false;
            }
            _thuMuc = Convert.ToString(CadApplication.GetSystemVariable("DWGPREFIX"));
            _maDon = MaDon.TuTenFile(Convert.ToString(CadApplication.GetSystemVariable("DWGNAME")));
            string fData = CsvXuat.DuongData(_thuMuc, _maDon), fCuoi = CsvXuat.DuongCuoi(_thuMuc, _maDon);
            File10 = File.Exists(fCuoi) ? fCuoi : null;
            File35 = File.Exists(fData) ? fData : null;

            Dictionary<string, ThongTinTam> cu;
            try { cu = File.Exists(fData) ? CsvXuat.DocData(DocChiaSe(fData)) : new Dictionary<string, ThongTinTam>(); }
            catch (Exception ex) { TrangThai = "Không đọc được " + fData + ": " + ex.Message; return false; }

            string loiGia = null;
            try { _giaDon = GiaDon.DocFile(CsvXuat.DuongGia(_thuMuc, _maDon)); }
            catch (Exception) { _giaDon = null; loiGia = " File " + _maDon + "-gia.json bị hỏng, chưa tính giá được."; }
            try { _giaVl = GiaVlDon.DocFile(CsvXuat.DuongGiaVl(_thuMuc, _maDon)); }
            catch (Exception) { _giaVl = null; loiGia += " File " + _maDon + "-giavl.csv bị hỏng, chưa dùng được giá tạm."; }
            CapNhatDsCongTy();

            var loiKhung = new Dictionary<long, List<LoiQuet>>();
            foreach (var g in loiMoi.GroupBy(l => l.Handle)) loiKhung[g.Key] = g.ToList();
            doc.UserData[KhoaLoiQuet] = loiKhung;
            _loiKhung = loiKhung;
            _goiYKhung.Clear();
            NapFname();

            var tot = new List<(TamXuat Tam, KhungTho Khung, string CanhBao)>();
            var loi = new List<DongTam>();
            foreach (KhungTho k in khung.Where(PhanTichKhung.CoFname))
            {
                if (PhanTichKhung.LaKhungChuThich(k))
                {
                    loi.Add(new DongTam(this, k, null, PhanTichKhung.LoiKhungChuThich) { BoQua = true });
                    continue;
                }
                var r = PhanTichKhung.Ghep(k, CaiDat.LopDimDung, dimzin, cu, quet);
                if (r.Tam != null)
                {
                    cu.TryGetValue(r.Tam.N, out ThongTinTam daCo);
                    _giaDon?.GiuSuaTay(r.Tam.N, r.Tam.Tt, daCo);
                    if (quet.ContainsKey(k.Handle) && hinhKhung.TryGetValue(k.Handle, out DuLieuHinh dh)) PhanTichKhung.ThemCuonTuHinh(r.Tam, dh.CungMatCat, dh.DoanMatCat, PhanTichKhung.BeTuFname(PhanTichKhung.DongFname(k)));
                    tot.Add((r.Tam, k, r.Loi));
                    continue;
                }
                string loiHinh = LoiHinhCua(k.Handle);
                string khac = loiHinh != null && r.Loi == PhanTichKhung.LoiChuaCoThongTin ? null : r.Loi;
                loi.Add(new DongTam(this, k, null, NoiLoi(loiHinh, khac)) { LoiQuet = loiHinh, LoiKhac = khac });
            }
            var theoHandle = tot.ToDictionary(x => x.Tam.HandleKhung);
            _tatCa = loi.Concat(CsvXuat.SapXep(tot.Select(x => x.Tam), CaiDat.SapXep)
                                      .Select(t => new DongTam(this, theoHandle[t.HandleKhung].Khung, t, theoHandle[t.HandleKhung].CanhBao)
                                      { LoiQuet = LoiHinhCua(t.HandleKhung) }))
                        .ToList();
            foreach (DongTam d in _tatCa) { d.MoRong = _chiTiet; KiemFname(d); }
            DsGiaVl.Clear();   // giá tạm vừa đọc lại từ file: không giữ dòng trỏ vào dữ liệu cũ
            DungGiaVl();
            TinhLaiNeuCo();   // số đo có thể đã đổi: giá tự động không được giữ giá trị cũ
            DanhGiaLai();
            Loc();

            string trung = CsvXuat.TenTrung(_tatCa.Where(d => d.Tam != null).Select(d => d.Tam));
            if (trung != null)
            {
                TrangThai = "Tên tấm trùng \"" + trung + "\". Sửa N rồi quét lại.";
                return false;
            }
            if (tot.Count == 0)
            {
                TrangThai = "Không có tấm đọc được. Giữ nguyên file data.";
                return false;
            }
            if (chiNap)
            {
                TrangThai = "Đã nạp lại " + tot.Count + " tấm từ file data lần xuất gần nhất. Sửa bản vẽ thì bấm Quét lại.";
                return true;
            }
            var conLai = new HashSet<string>(tot.Select(x => x.Tam.N));
            int boRa = cu.Keys.Count(n => !conLai.Contains(n));
            if (boRa > 0)   // file data sắp mất các dòng này: giữ 1 bản để lấy lại số đã nhập nếu quét nhầm
            {
                try { File.Copy(fData, CsvXuat.DuongDataTruocQuet(_thuMuc, _maDon), true); }
                catch (Exception ex) { TrangThai = "Không sao lưu được file data, chưa ghi: " + ex.Message; return false; }
            }
            if (!LuuData()) return false;   // LuuData đã báo lỗi; đóng file rồi quét lại
            LuuGia();
            File35 = fData;
            TrangThai = "Quét " + _tatCa.Count(d => !d.BoQua) + " tấm, " + _tatCa.Count(d => d.CoLoi && !d.BoQua) + " lỗi · đã lưu data CSV." +
                        (boRa > 0 ? " Bỏ " + boRa + " tấm không còn trong lượt quét." : "") + loiGia;
            return true;
        }

        // ── Tính giá ──────────────────────────────────────────────────

        private readonly NguonGiaCoDem _nguonGia = new NguonGiaCoDem();
        private bool _dangDocGia;
        private BangGia _bangGia;
        private GiaDon _giaDon;

        public string NhanTongTien
        {
            get
            {
                var ds = _tatCa.Where(d => d.Tam != null && !d.BoQua).Select(d => d.Tam).ToList();
                var (tong, coGia) = TinhGia.TongTien(ds);
                return coGia == 0 ? "Tổng đơn: chưa có giá"
                    : "Tổng đơn: " + tong.ToString("#,0", CultureInfo.InvariantCulture) + " ¥ · " + coGia + "/" + ds.Count + " tấm";
            }
        }

        /// <summary>Tổng cân (kg) của đơn; chỉ hiện khi đã có bảng giá vì tỉ trọng lấy từ bảng giá.</summary>
        public string NhanKg
        {
            get
            {
                if (_bangGia == null || _giaDon == null) return "";
                var ds = _tatCa.Where(d => d.Tam != null && !d.BoQua).Select(d => d.Tam).ToList();
                if (ds.Count == 0) return "";
                TongCan c = TinhGia.TongKg(ds, _giaDon, BangDung);
                string Kg(double x) => x.ToString("#,0.0", CultureInfo.InvariantCulture);
                string Yen(double x) => x.ToString("#,0", CultureInfo.InvariantCulture) + " ¥";
                string s = "Tổng KG: " + Kg(c.KgDon + c.KgGiaCong) + " kg · tấm đơn " + Kg(c.KgDon) + " · gia công " + Kg(c.KgGiaCong) +
                           (c.ChuaCoKg > 0 ? " · " + c.ChuaCoKg + " tấm chưa có KG" : "");
                if (!c.DacBiet) return s;
                var phan = new List<string>();
                if (c.KgDbDon > 0) phan.Add("tấm đơn " + Kg(c.KgDbDon) + " kg × " + Yen(c.DonGiaKgDon) + " = " + Yen(c.TienDbDon));
                if (c.KgDbGiaCong > 0) phan.Add("gia công " + Kg(c.KgDbGiaCong) + " kg × " + Yen(c.DonGiaKgGiaCong) + " = " + Yen(c.TienDbGiaCong));
                if (c.TamThuong > 0) phan.Add(c.TamThuong + " tấm tính giá thường");
                return s + "\nTheo kg (X+6): " + string.Join(" · ", phan);
            }
        }

        public bool CoNhanKg => NhanKg != "";

        // ── Chọn công ty: gõ hoặc dán tên phiên âm / tiếng Nhật ──
        private List<MucCongTy> _dsMucCongTy = new List<MucCongTy>();
        private BangGia _bangCongTy;
        private bool _daNapCongTy;
        public ObservableCollection<MucCongTy> DsCongTyLoc { get; } = new ObservableCollection<MucCongTy>();

        private string _timCongTy = "";
        public string TimCongTy
        {
            get => _timCongTy;
            set { _timCongTy = value ?? ""; HienDsCongTy = true; LocCongTy(); Bao(); }
        }

        private bool _hienDsCongTy;
        public bool HienDsCongTy { get => _hienDsCongTy; private set { _hienDsCongTy = value; Bao(); } }

        public MucCongTy CongTyChon => _dsMucCongTy.FirstOrDefault(m => m.Ten == CongTy);
        public bool CoCongTyChon => CongTyChon != null;

        private void LocCongTy()
        {
            DsCongTyLoc.Clear();
            foreach (MucCongTy m in CongTyTim.Loc(_dsMucCongTy, _timCongTy).Take(50)) DsCongTyLoc.Add(m);
        }

        internal void MoDsCongTy()
        {
            _timCongTy = CongTy ?? "";
            DsCongTyLoc.Clear();
            foreach (MucCongTy m in _dsMucCongTy.Take(50)) DsCongTyLoc.Add(m);   // vừa bấm vào: hiện cả danh sách
            HienDsCongTy = true;
        }

        internal void HuyDsCongTy()
        {
            HienDsCongTy = false;
            if (LoiThietLap == LoiNhieuCongTy) LoiThietLap = null;
            DongBoTenCongTy();
        }

        private const string LoiNhieuCongTy = "Có nhiều công ty khớp. Chọn 1 công ty trong danh sách.";

        /// <summary>Chốt chữ đã gõ: tên phiên âm hoặc tiếng Nhật đều quy về tên chuẩn trong database.</summary>
        internal void XongCongTy(MucCongTy chon = null)
        {
            if (!HienDsCongTy && chon == null) return;
            string q = _timCongTy.Trim();
            // không đổi chữ thì chỉ đóng danh sách, kể cả tên cũ không còn trong database
            if (chon == null && q == (CongTy ?? "")) { HuyDsCongTy(); return; }
            MucCongTy m = chon ?? CongTyTim.TimDung(_dsMucCongTy, q);
            if (m == null && q != "" && CongTyTim.Loc(_dsMucCongTy, q).Count > 1) { LoiThietLap = LoiNhieuCongTy; return; }
            HienDsCongTy = false;
            if (LoiThietLap == LoiNhieuCongTy) LoiThietLap = null;
            CongTy = m?.Ten ?? q;
            DongBoTenCongTy();
        }

        private void DongBoTenCongTy()
        {
            if (!HienDsCongTy) { _timCongTy = CongTy ?? ""; Bao(nameof(TimCongTy)); }
            Bao(nameof(CongTyChon)); Bao(nameof(CoCongTyChon));
        }

        /// <summary>Đọc danh sách mới 1 lần mỗi phiên, không chặn giao diện; lỗi mạng thì giữ danh sách từ bản sao.</summary>
        private async void NapCongTyNen()
        {
            if (_daNapCongTy) return;
            _daNapCongTy = true;
            try
            {
                BangGia b = await Task.Run(() => new NguonGiaCoDem().Doc());
                if (b != null) { _bangCongTy = b; CapNhatDsCongTy(); }
            }
            catch (Exception) { _daNapCongTy = false; }
        }

        public bool CoGiaDon => _giaDon != null;

        /// <summary>Đổi công ty: đã quét thì tính lại giá tự động, giá sửa tay giữ nguyên.</summary>
        public string CongTy
        {
            get => _giaDon?.CongTy;
            set
            {
                string v = (value ?? "").Trim();
                if (_giaDon == null || v == _giaDon.CongTy) return;
                if (CoDanhSach && TinhGiaBat)
                {
                    if (v != "") DoiCongTy(v); else BaoTre();
                    return;
                }
                if (CoDanhSach && !ChoPhepSua()) { BaoTre(); return; }
                _giaDon.CongTy = v;
                LuuGia();
                LoiThietLap = null;
                CapNhatDsCongTy();
            }
        }

        private void CapNhatDsCongTy()
        {
            BangGia b = _bangGia ?? _bangCongTy ?? (_bangCongTy = NguonGiaCoDem.BangDem());
            _dsMucCongTy = CongTyTim.Ds(b);
            if (HienDsCongTy) LocCongTy();
            Bao(nameof(CoGiaDon)); Bao(nameof(CongTy));
            DongBoTenCongTy();
        }

        private async void DoiCongTy(string moi)
        {
            GiaDon don = _giaDon;
            // chỉ đổi công ty khi có bảng giá, để tên công ty luôn khớp với giá đang hiện
            BangGia b = ChoPhepSua() ? await DocBangGiaNen("") : null;
            if (_giaDon != don) return;
            if (b != null && ChoPhepSua())
            {
                don.CongTy = moi;
                don.KhongTinhSauQuet = false;
                ApDungVaLuu(b, "");
                CapNhatDsCongTy();
                return;
            }
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => { Bao(nameof(CongTy)); DongBoTenCongTy(); }));
        }

        internal GiaTam XemGia(string ten) =>
            ten != null && _giaDon != null && _giaDon.Tam.TryGetValue(ten, out GiaTam g) ? g : null;

        internal GiaTam GiaCua(string ten) => ten == null ? null : _giaDon?.Lay(ten);

        internal void DoiTenGia(string cu, string moi) => _giaDon?.DoiTen(cu, moi);

        internal void LuuGia()
        {
            if (_giaDon == null || _thuMuc == null) return;
            try { _giaDon.GhiFile(CsvXuat.DuongGia(_thuMuc, _maDon)); }
            catch (Exception ex) { BaoLoi("Không ghi được dữ liệu giá: " + ex.Message); }
        }

        /// <summary>Đã bấm Tính giá trong phiên này thì sửa số đo / quét lại sẽ tự tính lại các giá tự động.</summary>
        internal void TinhLaiNeuCo()
        {
            if (_bangGia != null && _giaDon != null && _giaDon.CongTy != "") ApDungGia();
        }

        private void ApDungGia()
        {
            var canhBao = TinhGia.ApDung(_tatCa.Where(d => d.Tam != null && !d.BoQua).Select(d => d.Tam), _giaDon, BangDung);
            foreach (DongTam d in _tatCa)
            {
                d.CanhBaoGia = d.Tam != null && canhBao.TryGetValue(d.Tam.N, out string s) ? s : null;
                d.BaoGia();
            }
            _vlThieu = new HashSet<(string, double)>(_tatCa.Where(d => d.Tam != null && d.CanhBaoGia?.StartsWith("Chưa có giá") == true)
                .Select(d => (d.Tam.M, double.TryParse((d.Tam.T ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double day) ? day : -1)));
            foreach (DongGiaVl r in DsGiaVl) r.CapNhat();
            BaoNhanTab();
        }

        private async Task<BangGia> DocBangGiaNen(string tienTo)
        {
            if (_dangDocGia) return null;
            _dangDocGia = true;
            TrangThai = tienTo + "Đang đọc bảng giá...";
            try
            {
                BangGia b = await Task.Run(() => _nguonGia.Doc());   // không treo CAD khi mạng chậm
                if (b == null) TrangThai = tienTo + "Chưa kết nối bảng giá. Nhập kết nối ở nút cài đặt.";
                return b;
            }
            catch (Exception ex) { TrangThai = tienTo + "Không đọc được bảng giá: " + ex.Message; return null; }
            finally { _dangDocGia = false; }
        }

        private void ApDungVaLuu(BangGia b, string tienTo)
        {
            _bangGia = _bangCongTy = b;
            _dsMucCongTy = CongTyTim.Ds(b);
            DongBoTenCongTy();
            DungGiaVl();
            ApDungGia();
            DanhGiaLai();
            if (!LuuData()) return;
            LuuGia();
            var ds = _tatCa.Where(d => d.Tam != null && !d.BoQua).Select(d => d.Tam).ToList();
            string kq = tienTo + "Đã tính giá " + TinhGia.TongTien(ds).CoGia + "/" + ds.Count + " tấm · " + _giaDon.CongTy +
                        (b.CongTyUuDai.ContainsKey(_giaDon.CongTy) ? "." : " (không có trong danh sách, tính giá thường).") +
                        (_nguonGia.GhiChu == null ? "" : " " + _nguonGia.GhiChu);
            XuatTuDong(kq + " ");
        }

        /// <summary>Sau mỗi thay đổi trên tab: lưu thông tin giá rồi cập nhật file cuối.</summary>
        internal void SauThayDoi()
        {
            string truoc = TrangThai;
            LuuGia();
            XuatTuDong(TrangThai != truoc && TrangThai.StartsWith("Không ghi") ? TrangThai + " " : "");
        }

        /// <summary>Ghi &lt;đơn&gt;.csv. Còn tấm lỗi thì giữ nguyên file cũ và báo, để không gửi nhà máy danh sách thiếu.</summary>
        private void XuatTuDong(string tienTo)
        {
            if (_thuMuc == null) return;
            int soLoi = _tatCa.Count(d => d.CoLoi && !d.BoQua);
            if (soLoi > 0) { TrangThai = tienTo + "Chưa cập nhật " + _maDon + ".csv: còn " + soLoi + " tấm lỗi."; return; }
            var ds = CsvXuat.SapXep(_tatCa.Where(d => d.Tam != null && !d.BoQua).Select(d => d.Tam), CaiDat.SapXep);
            if (ds.Count == 0) { TrangThai = tienTo + "Không có tấm nào để xuất."; return; }
            try { CsvXuat.GhiCuoi(_thuMuc, _maDon, ds); }
            catch (IOException) { TrangThai = tienTo + _maDon + ".csv đang mở (Excel?). Đóng file, lần thay đổi sau sẽ tự cập nhật."; return; }
            catch (Exception ex) { TrangThai = tienTo + "Không ghi được CSV: " + ex.Message; return; }
            File10 = CsvXuat.DuongCuoi(_thuMuc, _maDon);
            TrangThai = tienTo + "Đã cập nhật " + _maDon + ".csv (" + ds.Count + " tấm).";
        }

        private async void TinhGiaSauQuet(string tienTo)
        {
            GiaDon don = _giaDon;
            BangGia b = await DocBangGiaNen(tienTo);
            if (_giaDon != don) return;
            if (b != null) ApDungVaLuu(b, tienTo);
            else if (LuuData()) XuatTuDong(TrangThai + " ");   // không đọc được giá vẫn cập nhật thông số, giữ giá đang có
        }

        /// <summary>Nút ↺ trên ô giá: bỏ giá sửa tay, quay về giá tự tính.</summary>
        internal void VeGiaTuDong(DongTam d)
        {
            if (!ChoPhepSua()) return;
            if (_bangGia == null || _giaDon == null || _giaDon.CongTy == "") { TrangThai = "Chưa có bảng giá. Tích Tính giá ở tab Home rồi quét."; return; }
            GiaCua(d.Tam.N).TuDong = true;
            ApDungGia();
            DanhGiaLai();
            if (LuuData()) SauThayDoi();
        }

        private static string DocChiaSe(string f)
        {
            using (var s = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var r = new StreamReader(s, System.Text.Encoding.UTF8)) return r.ReadToEnd();
        }

        /// <summary>Ghi lại data CSV từ mọi tấm đọc được (đã sắp xếp). Lỗi → báo, trả false.</summary>
        internal bool LuuData()
        {
            if (_thuMuc == null) { BaoLoi("Chưa quét bản vẽ đã lưu — bấm Quét trước."); return false; }
            try
            {
                CsvXuat.GhiData(_thuMuc, _maDon, CsvXuat.SapXep(_tatCa.Where(d => d.Tam != null).Select(d => d.Tam), CaiDat.SapXep));
                return true;
            }
            catch (IOException) { BaoLoi("data\\" + _maDon + "-data.csv đang mở (Excel?) — đóng rồi làm lại."); }
            catch (Exception ex) { BaoLoi("Không ghi được data CSV: " + ex.Message); }
            return false;
        }

        /// <summary>Cảnh báo vát / pitchi + tên trùng cho mọi tấm đọc được (sau quét và sau mỗi lần sửa ô).</summary>
        internal void DanhGiaLai()
        {
            var tam = _tatCa.Where(d => d.Tam != null).ToList();
            var trung = new HashSet<string>(tam.GroupBy(d => d.Tam.N).Where(g => g.Count() > 1).Select(g => g.Key));
            foreach (DongTam d in tam)
            {
                d.LoiKhac = trung.Contains(d.Tam.N) ? "Tên tấm trùng: " + d.Tam.N : PhanTichKhung.KiemTra(d.Tam.Tt);
                d.Loi = NoiLoi(d.LoiQuet, d.LoiKhac);
            }
            GomLoi();
        }

        private Dictionary<long, List<LoiQuet>> _loiKhung = new Dictionary<long, List<LoiQuet>>();

        /// <summary>Mọi lỗi hình học của khung, mỗi lỗi 1 dòng để hiện trên thẻ; không có → null.</summary>
        private string LoiHinhCua(long handle)
        {
            if (!_loiKhung.TryGetValue(handle, out var ds)) return null;
            var loi = ds.Where(l => l.LaLoi).Select(l => l.NoiDung).ToList();
            return loi.Count == 0 ? null : string.Join("\n", loi);
        }

        private static string NoiLoi(string a, string b) => a == null ? b : b == null ? a : a + "\n" + b;

        private void GomLoi()
        {
            var cu = DsLoi.ToList();
            var moi = new List<DongLoi>();
            // giữ dòng không đổi: đang gõ ở ô pitchi trên thẻ lỗi thì thẻ không bị dựng lại
            void Them(DongLoi l) => moi.Add(cu.FirstOrDefault(x => !moi.Contains(x) && x.Dong == l.Dong && x.Goc == l.Goc && x.Ten == l.Ten &&
                x.NoiDung == l.NoiDung && x.LaLoi == l.LaLoi && x.LaThieuGia == l.LaThieuGia) ?? l);
            foreach (DongGiaVl v in DsGiaVl.Where(x => x.ChuaCoDb))
            {
                DongTam dau = _tatCa.FirstOrDefault(x => x.Tam != null && !x.BoQua && x.Tam.M == v.VatLieu
                    && double.TryParse((x.Tam.T ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double day) && day == v.DoDay);
                if (dau != null) Them(new DongLoi(dau, "Chưa có trong database (" + v.SoTam + " tấm). Cần bổ sung.", false, null, true, v.Ten) { LaGia = true });
            }
            foreach (DongTam d in _tatCa.Where(x => !x.BoQua))
            {
                _loiKhung.TryGetValue(d.Khung.Handle, out var ds);
                foreach (LoiQuet l in ds?.Where(x => x.LaLoi) ?? Enumerable.Empty<LoiQuet>())
                    Them(new DongLoi(d, l.NoiDung, true, l));
                if (d.LoiKhac != null) Them(new DongLoi(d, d.LoiKhac, true, null));
                if (d.Tam != null && _goiYKhung.TryGetValue(d.Khung.Handle, out var goiY))
                    foreach (GoiYFname g in goiY) Them(new DongLoi(d, g.NoiDung, false, null) { GoiY = g });
                foreach (LoiQuet w in ds?.Where(l => !l.LaLoi) ?? Enumerable.Empty<LoiQuet>())
                    Them(new DongLoi(d, w.NoiDung, false, w));
                if (d.CanhBaoGia != null) Them(new DongLoi(d, d.CanhBaoGia, false, null, d.CanhBaoGia.StartsWith("Chưa có giá")) { LaGia = true });
            }
            PhanTichKhung.DongBoDs(DsLoi, moi);
            Bao(nameof(NhanTongTien)); Bao(nameof(NhanKg)); Bao(nameof(CoNhanKg));
            BaoNhanTab();
        }

        /// <summary>Bấm dòng lỗi: zoom sát vị trí kế tiếp, cloud vẫn hiện ở mọi lỗi của tấm; lỗi không có vị trí thì zoom cả khung.</summary>
        public void ChonLoi(DongLoi l)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (l == null || doc == null || doc != _docDanhSach) return;
            KhungTho f = l.Dong.Khung;
            if (l.SoViTri == 0) ZoomCad.ToiHop(doc, f.MinX, f.MinY, f.MaxX, f.MaxY);
            else
            {
                var v = l.Tiep();
                // nới theo cỡ khung để điểm hở (hộp = 0) vẫn thấy được nét xung quanh
                double le = Math.Max(0.12 * Math.Max(f.MaxX - f.MinX, f.MaxY - f.MinY), 1.0);
                ZoomCad.ToiHop(doc, v.MinX - le, v.MinY - le, v.MaxX + le, v.MaxY + le);
            }
            HienCloud(doc, l.Dong);
        }

        private void HienCloud(Document doc, DongTam d)
        {
            CloudLoiTam.Xoa();
            if (!_loiKhung.TryGetValue(d.Khung.Handle, out var ds)) return;
            KhungTho k = d.Khung;
            try { CloudLoiTam.Hien(doc, CloudLoi.Tao(ds.Where(l => l.LaLoi).SelectMany(l => l.ViTri), Math.Max(k.MaxX - k.MinX, k.MaxY - k.MinY))); }
            catch (Exception ex) { TrangThai = "Không vẽ được cloud: " + ex.Message; }
        }

        /// <summary>Esc trên tab.</summary>
        public void XoaCloud()
        {
            CloudLoiTam.Xoa();
            CadApplication.DocumentManager.MdiActiveDocument?.Editor.UpdateScreen();
        }

        private void DoiBanVe(Document doc)
        {
            if (doc == null || doc == _docThietLap) return;   // kích hoạt lại đúng bản vẽ: giữ danh sách và cloud
            CloudLoiTam.Xoa();
            _tatCa.Clear();
            _docDanhSach = null;
            _bangGia = null;
            DsGiaVl.Clear();
            GomLoi();
            Loc();
            try { NapThietLap(doc); }
            catch (Exception ex) { _docThietLap = null; LoiThietLap = "Không đọc được thiết lập của bản vẽ: " + ex.Message; }
            TabDangChon = 0;
            TrangThai = "Đổi bản vẽ. Thiết lập rồi bấm quét.";
            NapLai(doc);
        }

        /// <summary>
        /// Mở / quay lại bản vẽ đã xuất: ghép khung trên bản vẽ với data CSV (theo tên N), giá json và giá VL.
        /// Không phân tích lại hình; lỗi hình học lấy từ lượt quét gần nhất của bản vẽ trong phiên CAD này.
        /// </summary>
        private void NapLai(Document doc)
        {
            try
            {
                if (_thuMuc == null || !File.Exists(CsvXuat.DuongData(_thuMuc, _maDon))) return;
                var tenData = new HashSet<string>(CsvXuat.DocData(DocChiaSe(CsvXuat.DuongData(_thuMuc, _maDon))).Keys);
                var (khung, dimzin) = QuetKhungCad.Quet(doc, QuetKhungCad.TatCaKhung(doc, CaiDat.LopKhungDung));
                // chỉ các khung đã có trong data: lượt xuất trước có thể chỉ quét một vùng
                var daXuat = khung.Where(k => PhanTichKhung.CoFname(k) && tenData.Contains(PhanTichKhung.TenKhung(k) ?? "")).ToList();
                if (daXuat.Count == 0) return;
                var loiCu = (doc.UserData[KhoaLoiQuet] as Dictionary<long, List<LoiQuet>>)?.Values.SelectMany(x => x).ToList() ?? new List<LoiQuet>();
                _hinhQuet.Clear();
                if (SauQuet(doc, daXuat, dimzin, new Dictionary<long, ThongTinTam>(), loiCu, new Dictionary<long, DuLieuHinh>(), chiNap: true))
                {
                    CapNhatSoKhung(doc);
                    BaoThietLap();
                    TabDangChon = 1;
                }
            }
            catch (Exception ex) { TrangThai = "Không nạp lại được danh sách đã xuất: " + ex.Message; }
        }

        // ── Sửa ô ─────────────────────────────────────────────────────

        /// <summary>N/Q/M/T ghi vào F-name trên bản vẽ, rồi lưu lại data CSV (N đổi = đổi tên dòng).</summary>
        internal bool SuaFname(DongTam d, char truong, string giaTri)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null || doc != _docDanhSach)
            {
                BaoLoi("Danh sách không thuộc bản vẽ đang mở — quét lại.");
                return false;
            }
            try { GhiNguocCad.SuaFname(doc, d.Tam.HandleKhung, truong, giaTri); }
            catch (Exception ex) { BaoLoi("Không ghi được F-name: " + ex.Message); return false; }
            TrangThai = "Đã ghi F-name vào bản vẽ.";
            return true;
        }

        internal void BaoLoi(string s) => TrangThai = s;

        // ── Tạo file A và PDF nhanh (tab Home) ──
        private readonly Dictionary<long, (DuLieuHinh D, KetQuaHinh R)> _hinhQuet = new Dictionary<long, (DuLieuHinh, KetQuaHinh)>();
        private bool _taoFileA, _inPdf;
        private string _profileIn;
        public ObservableCollection<string> DsProfileIn { get; } = new ObservableCollection<string>();
        public const string ProfileTheoBanVe = "Theo bản vẽ";   // trùng PrintSettingsResolver.FromDrawingName của tab PRINT

        public bool TaoFileA { get => _taoFileA; set { _taoFileA = value; Bao(); GhiNhanh(); } }
        public bool InPdf { get => _inPdf; set { _inPdf = value; Bao(); GhiNhanh(); } }
        public string ProfileIn { get => _profileIn; set { if (value == null) return; _profileIn = value; Bao(); GhiNhanh(); } }
        public string NhanFileA => (_maDon ?? "<đơn>") + "-A.dxf, F-name thu vào trong tấm";
        public string NhanPdf => (_maDon ?? "<đơn>") + ".pdf, tấm gia công in trước";

        private void GhiNhanh() => CaiDatRegistry.GhiNhanh(_taoFileA, _inPdf, _profileIn);

        private void DocNhanh()
        {
            var (a, b, p) = CaiDatRegistry.DocNhanh();
            _taoFileA = a; _inPdf = b; _profileIn = p;
            NapProfileIn();
        }

        /// <summary>Danh sách profile của tab PRINT; profile đã nhớ không còn thì về Theo bản vẽ.</summary>
        public void NapProfileIn()
        {
            string giu = _profileIn;
            DsProfileIn.Clear();
            DsProfileIn.Add(ProfileTheoBanVe);
            foreach (string t in CaiDatRegistry.DocTenProfileIn()) DsProfileIn.Add(t);
            _profileIn = DsProfileIn.FirstOrDefault(t => string.Equals(t, giu, StringComparison.OrdinalIgnoreCase)) ?? ProfileTheoBanVe;
            Bao(nameof(ProfileIn));
        }

        /// <summary>
        /// Sau khi quét: nếu không còn lỗi và cảnh báo nào ngoài cảnh báo giá thì tạo file A / in PDF theo ô đã tick.
        /// Còn lỗi thì không tạo gì, người dùng sửa rồi quét lại.
        /// </summary>
        private void TaoNhanh(Document doc) => TaoNhanh(doc, _taoFileA, _inPdf);

        private void TaoNhanh(Document doc, bool taoA, bool inPdf)
        {
            Bao(nameof(NhanFileA)); Bao(nameof(NhanPdf));
            if (!taoA && !inPdf) return;
            int chan = DsLoi.Count(l => !l.LaGia);
            if (chan > 0) { TrangThai += " Còn " + chan + " lỗi / cảnh báo, chưa tạo file A và PDF. Sửa ở tab Error rồi quét lại."; return; }
            var tam = _tatCa.Where(d => d.Tam != null && !d.BoQua).ToList();
            if (tam.Count == 0) return;
            taoA = taoA && ChoGhiDe(DuongFileA);
            inPdf = inPdf && ChoGhiDe(DuongPdf);

            if (taoA)
            {
                var ds = new List<TamFileA>();
                var loi = new List<LoiQuet>();
                foreach (DongTam d in tam)
                {
                    long h = d.Khung.Handle;
                    _hinhQuet.TryGetValue(h, out var hq);
                    var fn = hq.D?.ChuFname ?? new List<ChuHinh>();
                    ChoFname cho = null;
                    if (hq.R?.BienNgoai != null && fn.Count > 0 &&
                        double.TryParse(d.Tam.Y, NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                    {
                        double x0 = fn.Min(c => c.MinX), y0 = fn.Min(c => c.MinY);
                        cho = FnameTrongTam.TimCho(hq.R.BienNgoai, hq.R.Lo, fn.Max(c => c.MaxX) - x0, fn.Max(c => c.MaxY) - y0, y);
                        if (cho != null) ds.Add(new TamFileA { HandleKhung = h, GocX = x0, GocY = y0, TiLe = cho.TiLe, DenX = cho.X, DenY = cho.Y });
                    }
                    if (cho == null)
                        loi.Add(new LoiQuet { Handle = h, LaLoi = true, NoiDung = "Không đặt được F-name vào phần thịt sau " + FnameTrongTam.SoLanThu + " lần thu nhỏ. Kiểm tra lại hình." });
                }
                if (loi.Count > 0)
                {
                    foreach (LoiQuet l in loi)
                    {
                        if (!_loiKhung.TryGetValue(l.Handle, out var dsl)) _loiKhung[l.Handle] = dsl = new List<LoiQuet>();
                        dsl.Add(l);
                    }
                    GomLoi();
                    TrangThai += " " + loi.Count + " tấm không đặt được F-name, chưa tạo file A và PDF.";
                    return;
                }
                string duongA = DuongFileA;
                try
                {
                    TaoFileACad.Ghi(doc, duongA, ds);
                    TrangThai += " Đã tạo " + Path.GetFileName(duongA) + ".";
                }
                catch (Exception ex) { TrangThai += " Không tạo được file A: " + ex.Message; return; }
            }

            if (inPdf)
            {
                var thuTu = tam.Where(d => PhanTichKhung.LaTamGiaCong(d.Tam.Tt)).Concat(tam.Where(d => !PhanTichKhung.LaTamGiaCong(d.Tam.Tt)));
                var dong = new List<string> { _profileIn ?? ProfileTheoBanVe, DuongPdf };
                dong.AddRange(thuTu.Select(d => string.Join(",", new[] { d.Khung.MinX, d.Khung.MinY, d.Khung.MaxX, d.Khung.MaxY }
                    .Select(v => v.ToString("R", CultureInfo.InvariantCulture)))));
                // OHTATOOL.dll in giúp (dùng chung máy in, profile, gộp PDF của tab PRINT); 2 DLL không tham chiếu nhau
                AppDomain.CurrentDomain.SetData("OHTA_CSV_PDF", string.Join("\n", dong));
                doc.SendStringToExecute("OHTA_CSV_PDF\n", true, false, true);
                TrangThai += " Đang in " + tam.Count + " tấm ra " + _maDon + ".pdf.";
            }
        }

        private string DuongFileA => _thuMuc == null ? null : Path.Combine(_thuMuc, _maDon + "-A.dxf");
        private string DuongPdf => _thuMuc == null ? null : Path.Combine(_thuMuc, _maDon + ".pdf");

        /// <summary>File đã có thì hỏi trước khi ghi đè; chọn Không thì giữ file cũ và báo.</summary>
        private bool ChoGhiDe(string f)
        {
            if (f == null || !File.Exists(f)) return true;
            bool co = System.Windows.MessageBox.Show(Path.GetFileName(f) + " đã có. Ghi đè file cũ?", "OHTA CSV",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
            if (!co) TrangThai += " Giữ nguyên " + Path.GetFileName(f) + ".";
            return co;
        }

        /// <summary>2 nút Xuất file A / Xuất PDF: dùng kết quả lượt quét gần nhất, không cần tích ô.</summary>
        private void XuatNgay(bool taoA, bool inPdf)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null || doc != _docThietLap || _thuMuc == null || _hinhQuet.Count == 0)
            {
                TrangThai = "Quét bản vẽ trước rồi mới xuất file A / PDF.";
                return;
            }
            TrangThai = "";
            TaoNhanh(doc, taoA, inPdf);
            if (TrangThai == "") TrangThai = "Không có tấm nào để xuất.";
            TrangThai = TrangThai.Trim();
        }

        // ── Đối chiếu F-name với dữ liệu tab Fname (catalog + TraV) ──
        private readonly Dictionary<long, List<GoiYFname>> _goiYKhung = new Dictionary<long, List<GoiYFname>>();
        private OhtaTool.Plugin.Core.FnameCatalogData _catFname;
        private const string KhoaBoSung = "OHTA_FN_BOSUNG";   // OHTATOOL đọc M, T qua AppDomain (2 DLL không tham chiếu nhau)

        /// <summary>Đọc catalog + TraV như tab Fname: cache Google Sheet trên máy, không có thì bản mẫu 01_Fname\data.</summary>
        private void NapFname()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(CsvTabViewModel).Assembly.Location), "..", "..", "..", "01_Fname"));
                _catFname = OhtaTool.Plugin.Core.FnameCatalogData.LoadFromModuleDirectory(dir);
                OhtaTool.Plugin.Core.TraVCalculator.Load(dir);
                OhtaTool.Plugin.Core.TraVCalculator.SetCatalog(_catFname);
            }
            catch (Exception) { _catFname = null; }   // thiếu dữ liệu Fname thì bỏ qua kiểm tra, không chặn quét
        }

        private void KiemFname(DongTam d)
        {
            if (d.Tam == null) return;
            try { _goiYKhung[d.Khung.Handle] = KiemTraFname.Chay(PhanTichKhung.DongFname(d.Khung), _catFname, OhtaTool.Plugin.Core.TraVCalculator.GetBendOptionsForFname); }
            catch (Exception) { _goiYKhung.Remove(d.Khung.Handle); }
        }

        /// <summary>Nút Sửa theo bảng: thay dòng F-name trên bản vẽ bằng dòng đúng theo dữ liệu Fname.</summary>
        public void SuaTheoBang(DongLoi l)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            GoiYFname g = l?.GoiY;
            if (g?.DongMoi == null || doc == null || doc != _docDanhSach) { BaoLoi("Mở đúng bản vẽ đã quét rồi bấm lại."); return; }
            KhungTho k = l.Dong.Khung;
            try { GhiNguocCad.SuaDong(doc, k.Handle, g.DongCu, g.DongMoi); }
            catch (Exception ex) { BaoLoi("Không sửa được F-name: " + ex.Message); return; }
            foreach (TextTho t in k.Text.Where(x => string.Equals(x.Lop, PhanTichKhung.LopFname, StringComparison.OrdinalIgnoreCase)))
            {
                string moi = PhanTichKhung.ThayDongFname(t.NoiDung, t.LaMtext, x => x.Trim() == g.DongCu, g.DongMoi);
                if (moi != null) { t.NoiDung = moi; break; }
            }
            KiemFname(l.Dong);
            GomLoi();
            TrangThai = "Đã sửa " + g.DongCu + " thành " + g.DongMoi + ".";
        }

        /// <summary>Nút Bổ sung: mở cửa sổ Bổ sung vật liệu của tab Fname, điền sẵn M và T.</summary>
        public void BoSungFname(DongLoi l)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (l?.GoiY?.BoSung != true || doc == null) return;
            AppDomain.CurrentDomain.SetData(KhoaBoSung, l.GoiY.M + "\n" + l.GoiY.T);
            string huy = Convert.ToInt32(CadApplication.GetSystemVariable("CMDACTIVE")) == 0 ? "" : "\x03\x03";
            doc.SendStringToExecute(huy + "OHTA_FN_BOSUNG\n", true, false, true);
            TrangThai = "Gửi bổ sung xong thì quét lại để kiểm tra.";
        }

        /// <summary>File CSV / data đang mở thì không cho đổi gì, để bản vẽ, tab và 2 file không lệch nhau.</summary>
        internal bool ChoPhepSua()
        {
            if (_thuMuc == null) return true;
            string f = CsvXuat.TepDangMo(CsvXuat.DuongCuoi(_thuMuc, _maDon), CsvXuat.DuongData(_thuMuc, _maDon), CsvXuat.DuongGiaVl(_thuMuc, _maDon));
            if (f == null) return true;
            TrangThai = Path.GetFileName(f) + " đang mở. Đóng file rồi sửa lại.";
            return false;
        }

        // ── Chọn thẻ → zoom cả tấm + cloud mọi đối tượng lỗi ──────────

        public void ChonDong(DongTam d)
        {
            CloudLoiTam.Xoa();
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (d == null || doc == null || doc != _docDanhSach) return;
            KhungTho k = d.Khung;
            ZoomCad.ToiHop(doc, k.MinX, k.MinY, k.MaxX, k.MaxY);
            HienCloud(doc, d);
        }

        // ── Lọc, trạng thái ───────────────────────────────────────────

        private bool _chiLoi;
        public bool ChiLoi { get => _chiLoi; set { _chiLoi = value; Bao(); Loc(); } }

        private string _timKiem = "";
        public string TimKiem { get => _timKiem; set { _timKiem = value ?? ""; Bao(); Loc(); } }

        private void Loc()
        {
            DongHien.Clear();
            foreach (DongTam d in _tatCa.Where(d => (!_chiLoi || d.CoLoi) && CsvXuat.KhopTim(d.Tam, d.N + " " + d.Loi, _timKiem))) DongHien.Add(d);
        }

        private string _trangThai = "Bấm Quét theo vùng hoặc Quét toàn bản vẽ để xem danh sách tấm.";
        public string TrangThai { get => _trangThai; set { _trangThai = value; Bao(); } }

        private string _file10, _file35;
        public string File10 { get => _file10; private set { _file10 = value; Bao(); Bao(nameof(CoFile10)); } }
        public string File35 { get => _file35; private set { _file35 = value; Bao(); Bao(nameof(CoFile35)); } }
        public bool CoFile10 => !string.IsNullOrEmpty(_file10);
        public bool CoFile35 => !string.IsNullOrEmpty(_file35);

        // PDF được in sau (lệnh OHTA_CSV_PDF) nên không khoá nút theo file có hay chưa, bấm mà chưa có thì báo
        private void MoHoacBao(string f)
        {
            if (f == null || !File.Exists(f)) { TrangThai = "Chưa có " + (f == null ? "file" : Path.GetFileName(f)) + ". Bấm Xuất file A / Xuất PDF trước."; return; }
            Mo(f);
        }

        private void Mo(string f)
        {
            if (string.IsNullOrEmpty(f) || !File.Exists(f)) return;
            try { Process.Start(new ProcessStartInfo(f) { UseShellExecute = true }); }
            catch (Exception ex) { TrangThai = "Không mở được " + f + ": " + ex.Message; }
        }

        // ── Cài đặt ───────────────────────────────────────────────────

        /// <summary>Nút ⚙: cửa sổ cài đặt như tab AI / Guro; Lưu mới ghi registry.</summary>
        private void MoCaiDat()
        {
            try
            {
                var w = new Views.CsvCaiDatWindow(CaiDat, AccentBrush);
                if (CadApplication.ShowModalWindow(w) == true) Luu();
            }
            catch (Exception ex) { TrangThai = "Không mở được cài đặt: " + ex.Message; }
        }

        /// <summary>Bánh răng của nút ⚙ (cùng ảnh với tab AI / Guro), tô màu tab qua OpacityMask.</summary>
        public ImageSource SettingsLogo => _settingsLogo ?? (_settingsLogo = NapLogo());
        private static ImageSource _settingsLogo;

        private static ImageSource NapLogo()
        {
            try
            {
                using (Stream s = typeof(CsvTabViewModel).Assembly.GetManifestResourceStream("CsvTool.Assets.settings_logo.png"))
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

        private void Luu(params string[] ten)
        {
            try { CaiDatRegistry.Ghi(CaiDat); }
            catch (Exception ex) { TrangThai = "Không lưu được cài đặt: " + ex.Message; }
            foreach (string t in ten) Bao(t);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao([CallerMemberName] string ten = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }
}
