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
    /// Tab CSV. Lệnh CSV và nút Quét... mở dialog thiết lập (CSVTHIETLAP), phân tích hình học bằng C#: thông tin tấm lưu ở
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
        public bool CoDsLoi => DsLoi.Count > 0;
        public string NhanDsLoi => "Lỗi và cảnh báo (" + DsLoi.Count(l => l.LaLoi) + " lỗi, " + DsLoi.Count(l => !l.LaLoi) + " cảnh báo)";
        private const string KhoaLoiQuet = "CSVTOOL_LOI_QUET";
        private Document _docDanhSach;

        public ICommand QuetCmd { get; }
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

        public CsvTabViewModel(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            TieuDe = tieuDe; MoTa = moTa; ThuMucModule = thuMucModule;
            try { AccentBrush = (Brush)new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(mauNhan) ? "#1E7F4F" : mauNhan); }
            catch { AccentBrush = Brushes.SeaGreen; }
            try { CaiDat = CaiDatRegistry.Doc(); } catch { CaiDat = new CaiDatCsv(); }

            QuetCmd = new Lenh(GoiThietLap);
            CaiDatCmd = new Lenh(MoCaiDat);
            ChiTietCmd = new Lenh(() => ChiTiet = !ChiTiet);
            MoFile10Cmd = new Lenh(() => Mo(File10));
            MoFile35Cmd = new Lenh(() => Mo(File35));

            HienTai = this;
            try { CadApplication.DocumentManager.DocumentActivated += (s, e) => DoiBanVe(e.Document); } catch { }
        }

        // ── Quét: chọn khung → PhanTichHinh → SauQuet ─────────────────

        private string _thuMuc, _maDon;

        /// <summary>Nút Quét...: chạy trong ngữ cảnh lệnh để sau dialog còn kéo chọn vùng được.</summary>
        private void GoiThietLap()
        {
            try
            {
                Document doc = CadApplication.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                // không huỷ khi rảnh: ^C sẽ xoá khung người dùng đã chọn sẵn
                string huy = Convert.ToInt32(CadApplication.GetSystemVariable("CMDACTIVE")) == 0 ? "" : "\x03\x03";
                doc.SendStringToExecute(huy + "CSVTHIETLAP\n", true, false, true);
            }
            catch (Exception ex) { TrangThai = "Không mở được thiết lập quét: " + ex.Message; }
        }

        /// <summary>Lệnh CSVTHIETLAP: hiện dialog, nhớ lựa chọn theo đơn, bấm nút quét là chạy.</summary>
        public void MoThietLap(Document doc)
        {
            CloudLoiTam.Xoa();
            GiaDon don = null;
            string fGia = null;
            if (Convert.ToInt32(CadApplication.GetSystemVariable("DWGTITLED")) == 1)
            {
                fGia = CsvXuat.DuongGia(Convert.ToString(CadApplication.GetSystemVariable("DWGPREFIX")),
                                        MaDon.TuTenFile(Convert.ToString(CadApplication.GetSystemVariable("DWGNAME"))));
                try { don = GiaDon.DocFile(fGia); } catch (Exception) { }   // file hỏng: SauQuet sẽ báo, không ghi đè
            }
            var w = new Views.CsvThietLapQuetWindow(Path.GetFileNameWithoutExtension(doc.Name), QuetKhungCad.TatCaKhung(doc, CaiDat.LopKhungDung).Count,
                NguonGiaCoDem.DsCongTyDem(), don?.CongTy, !(don?.KhongTinhSauQuet ?? false), don?.CheDoTinh ?? CaiDat.CheDoTinh, AccentBrush);
            if (CadApplication.ShowModalWindow(w) != true) { TrangThai = "Đã huỷ quét."; return; }

            CaiDat.CheDoTinh = w.CheDo;
            Luu();
            if (don != null)
            {
                don.KhongTinhSauQuet = !w.TinhGia;
                don.CheDoTinh = w.CheDo;
                if (w.TinhGia) don.CongTy = w.CongTy;
                try { don.GhiFile(fGia); } catch (Exception ex) { TrangThai = "Không lưu được thiết lập: " + ex.Message; }
            }
            QuetCad(doc, w.ToanBanVe, w.TinhGia, w.CongTy);
        }

        /// <summary>Phân tích hình học các khung, một khung lỗi không làm dừng lượt quét; xong thì tính giá nếu đã chọn.</summary>
        private void QuetCad(Document doc, bool toanBanVe, bool tinhGia, string congTy)
        {
            List<Autodesk_ObjectId> ids = toanBanVe ? QuetKhungCad.TatCaKhung(doc, CaiDat.LopKhungDung) : QuetKhungCad.ChonVung(doc, CaiDat.LopKhungDung);
            if (ids == null) { TrangThai = "Đã huỷ quét."; return; }
            _bangGia = null;   // không tính lại bằng bảng giá cũ: có tích thì đọc mới, bỏ tích thì giữ nguyên giá

            var (daChon, _) = QuetKhungCad.Quet(doc, ids);
            var hinh = QuetHinhCad.Doc(doc, daChon.Where(k => PhanTichKhung.SoDongN(k) == 1).ToList(), CaiDat);
            var quet = new Dictionary<long, ThongTinTam>();
            var loiMoi = new List<LoiQuet>();
            foreach (DuLieuHinh d in hinh)
            {
                try
                {
                    KetQuaHinh r = PhanTichHinh.Chay(d, CaiDat.DungSaiKheHo, CaiDat.CheDoTinh, (ten, soDoan) => QuetHinhCad.HoiSoNhan(doc, ten, soDoan));
                    if (r.Tam != null) quet[d.HandleKhung] = r.Tam;
                    loiMoi.AddRange(r.Loi);
                    loiMoi.AddRange(r.CanhBao);
                }
                catch (Exception ex)
                {
                    loiMoi.Add(new LoiQuet { Handle = d.HandleKhung, LaLoi = true, NoiDung = "Không phân tích được: " + ex.Message });
                }
            }
            bool daQuet = SauQuet(doc, quet, loiMoi);
            if (daQuet && tinhGia && _giaDon != null)
            {
                _giaDon.CongTy = congTy;
                TinhGiaSauQuet(TrangThai + " ");
            }
            else if (daQuet) XuatTuDong(TrangThai + " ");
        }

        /// <summary>
        /// Đọc mọi khung có F-name trên bản vẽ; thông tin tấm lấy từ kết quả vừa phân tích
        /// hoặc từ data CSV, rồi lưu data CSV. Không tạo / xoá gì trên bản vẽ.
        /// </summary>
        private bool SauQuet(Document doc, Dictionary<long, ThongTinTam> quet, List<LoiQuet> loiMoi)
        {
            _docDanhSach = doc;
            if (Convert.ToInt32(CadApplication.GetSystemVariable("DWGTITLED")) != 1)
            {
                _thuMuc = null;
                TrangThai = "Bản vẽ chưa lưu — lưu bản vẽ rồi quét lại (data CSV nằm cạnh file bản vẽ).";
                return false;
            }
            _thuMuc = Convert.ToString(CadApplication.GetSystemVariable("DWGPREFIX"));
            _maDon = MaDon.TuTenFile(Convert.ToString(CadApplication.GetSystemVariable("DWGNAME")));
            string fData = CsvXuat.DuongData(_thuMuc, _maDon);

            Dictionary<string, ThongTinTam> cu;
            try { cu = File.Exists(fData) ? CsvXuat.DocData(DocChiaSe(fData)) : new Dictionary<string, ThongTinTam>(); }
            catch (Exception ex) { TrangThai = "Không đọc được " + fData + ": " + ex.Message; return false; }

            string loiGia = null;
            try { _giaDon = GiaDon.DocFile(CsvXuat.DuongGia(_thuMuc, _maDon)); }
            catch (Exception) { _giaDon = null; loiGia = " File " + _maDon + "-gia.json bị hỏng, chưa tính giá được."; }

            // quét theo vùng: khung ngoài vùng giữ lỗi của lượt trước, khung vừa quét thay bằng kết quả mới
            var loiKhung = doc.UserData[KhoaLoiQuet] as Dictionary<long, List<LoiQuet>> ?? new Dictionary<long, List<LoiQuet>>();
            doc.UserData[KhoaLoiQuet] = loiKhung;
            foreach (long h in quet.Keys.Concat(loiMoi.Select(l => l.Handle)).ToList()) loiKhung.Remove(h);
            foreach (var g in loiMoi.GroupBy(l => l.Handle)) loiKhung[g.Key] = g.ToList();
            _loiKhung = loiKhung;

            var (khung, dimzin) = QuetKhungCad.Quet(doc, QuetKhungCad.TatCaKhung(doc, CaiDat.LopKhungDung));
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
                if (r.Tam != null) { tot.Add((r.Tam, k, r.Loi)); continue; }
                string loiHinh = LoiHinhCua(k.Handle);
                string khac = loiHinh != null && r.Loi == PhanTichKhung.LoiChuaCoThongTin ? null : r.Loi;
                loi.Add(new DongTam(this, k, null, NoiLoi(loiHinh, khac)) { LoiQuet = loiHinh, LoiKhac = khac });
            }
            var theoHandle = tot.ToDictionary(x => x.Tam.HandleKhung);
            _tatCa = loi.Concat(CsvXuat.SapXep(tot.Select(x => x.Tam), CaiDat.SapXep)
                                      .Select(t => new DongTam(this, theoHandle[t.HandleKhung].Khung, t, theoHandle[t.HandleKhung].CanhBao)
                                      { LoiQuet = LoiHinhCua(t.HandleKhung) }))
                        .ToList();
            foreach (DongTam d in _tatCa) d.MoRong = _chiTiet;
            TinhLaiNeuCo();   // số đo có thể đã đổi: giá tự động không được giữ giá trị cũ
            DanhGiaLai();
            Loc();

            string trung = CsvXuat.TenTrung(_tatCa.Where(d => d.Tam != null).Select(d => d.Tam));
            if (trung != null)
            {
                TrangThai = "Tên tấm trùng \"" + trung + "\". Sửa N rồi quét lại.";
                return false;
            }
            if (!LuuData()) return false;   // LuuData đã báo lỗi; đóng file rồi quét lại
            LuuGia();
            File35 = fData;
            TrangThai = "Quét " + _tatCa.Count(d => !d.BoQua) + " tấm, " + _tatCa.Count(d => d.CoLoi && !d.BoQua) + " lỗi · đã lưu data CSV." + loiGia;
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
                    : "Tổng đơn: " + tong.ToString("#,0", CultureInfo.InvariantCulture) + " · " + coGia + "/" + ds.Count + " tấm";
            }
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
            var canhBao = TinhGia.ApDung(_tatCa.Where(d => d.Tam != null && !d.BoQua).Select(d => d.Tam), _giaDon, _bangGia);
            foreach (DongTam d in _tatCa)
            {
                d.CanhBaoGia = d.Tam != null && canhBao.TryGetValue(d.Tam.N, out string s) ? s : null;
                d.BaoGia();
            }
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
            _bangGia = b;
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
            else XuatTuDong(TrangThai + " ");   // không đọc được giá vẫn cập nhật thông số, giữ giá đang có
        }

        /// <summary>Nút ↺ trên ô giá: bỏ giá sửa tay, quay về giá tự tính.</summary>
        internal void VeGiaTuDong(DongTam d)
        {
            if (_bangGia == null || _giaDon == null || _giaDon.CongTy == "") { TrangThai = "Chưa có bảng giá. Bấm Quét... và tích Tính giá."; return; }
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
            DsLoi.Clear();
            foreach (DongTam d in _tatCa.Where(x => !x.BoQua))
            {
                _loiKhung.TryGetValue(d.Khung.Handle, out var ds);
                foreach (LoiQuet l in ds?.Where(x => x.LaLoi) ?? Enumerable.Empty<LoiQuet>())
                    DsLoi.Add(new DongLoi(d, l.NoiDung, true, l));
                if (d.LoiKhac != null) DsLoi.Add(new DongLoi(d, d.LoiKhac, true, null));
                foreach (LoiQuet w in ds?.Where(l => !l.LaLoi) ?? Enumerable.Empty<LoiQuet>())
                    DsLoi.Add(new DongLoi(d, w.NoiDung, false, w));
                if (d.CanhBaoGia != null) DsLoi.Add(new DongLoi(d, d.CanhBaoGia, false, null));
            }
            Bao(nameof(CoDsLoi)); Bao(nameof(NhanDsLoi)); Bao(nameof(NhanTongTien));
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
            if (doc == _docDanhSach) return;   // kích hoạt lại đúng bản vẽ: giữ cloud
            CloudLoiTam.Xoa();
            _tatCa.Clear();
            _thuMuc = null;
            _giaDon = null;
            GomLoi();
            Loc();
            TrangThai = "Đổi bản vẽ — bấm Quét để xem danh sách tấm.";
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
