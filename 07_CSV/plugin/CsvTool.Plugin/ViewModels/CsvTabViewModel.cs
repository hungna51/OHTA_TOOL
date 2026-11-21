using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Tab CSV. Nút Quét gửi lệnh CSVQUETVUNG / CSVQUETTOAN, phân tích hình học bằng C#: thông tin tấm lưu ở
    /// data\&lt;đơn&gt;-data.csv (không để text trên bản vẽ). Sửa ô: N/Q/M/T ghi F-name, còn lại ghi data CSV.
    /// Xuất CSV ghi file cuối &lt;đơn&gt;.csv.
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

        public ICommand QuetVungCmd { get; }
        public ICommand QuetToanCmd { get; }
        public ICommand XuatCsvCmd { get; }
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

            QuetVungCmd = new Lenh(() => Quet(false));
            QuetToanCmd = new Lenh(() => Quet(true));
            XuatCsvCmd = new Lenh(XuatCsv);
            CaiDatCmd = new Lenh(MoCaiDat);
            ChiTietCmd = new Lenh(() => ChiTiet = !ChiTiet);
            MoFile10Cmd = new Lenh(() => Mo(File10));
            MoFile35Cmd = new Lenh(() => Mo(File35));

            HienTai = this;
            try { CadApplication.DocumentManager.DocumentActivated += (s, e) => DoiBanVe(e.Document); } catch { }
        }

        // ── Quét: chọn khung → PhanTichHinh → SauQuet ─────────────────

        private string _thuMuc, _maDon;

        private void Quet(bool toanBanVe)
        {
            try
            {
                Document doc = CadApplication.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                // không huỷ khi rảnh: ^C sẽ xoá khung người dùng đã chọn sẵn
                string huy = Convert.ToInt32(CadApplication.GetSystemVariable("CMDACTIVE")) == 0 ? "" : "\x03\x03";
                doc.SendStringToExecute(huy + (toanBanVe ? "CSVQUETTOAN" : "CSVQUETVUNG") + "\n", true, false, true);
            }
            catch (Exception ex) { TrangThai = "Không chạy được lệnh quét: " + ex.Message; }
        }

        /// <summary>Lệnh CSVQUETVUNG / CSVQUETTOAN: phân tích hình học các khung, một khung lỗi không làm dừng lượt quét.</summary>
        public void QuetCad(Document doc, bool toanBanVe)
        {
            CloudLoiTam.Xoa();
            List<Autodesk_ObjectId> ids = toanBanVe ? QuetKhungCad.TatCaKhung(doc, CaiDat.LopKhungDung) : QuetKhungCad.ChonVung(doc, CaiDat.LopKhungDung);
            if (ids == null) { TrangThai = "Đã huỷ quét."; return; }

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
            SauQuet(doc, quet, loiMoi);
        }

        /// <summary>
        /// Đọc mọi khung có F-name trên bản vẽ; thông tin tấm lấy từ kết quả vừa phân tích
        /// hoặc từ data CSV, rồi lưu data CSV. Không tạo / xoá gì trên bản vẽ.
        /// </summary>
        private void SauQuet(Document doc, Dictionary<long, ThongTinTam> quet, List<LoiQuet> loiMoi)
        {
            _docDanhSach = doc;
            if (Convert.ToInt32(CadApplication.GetSystemVariable("DWGTITLED")) != 1)
            {
                _thuMuc = null;
                TrangThai = "Bản vẽ chưa lưu — lưu bản vẽ rồi quét lại (data CSV nằm cạnh file bản vẽ).";
                return;
            }
            _thuMuc = Convert.ToString(CadApplication.GetSystemVariable("DWGPREFIX"));
            _maDon = MaDon.TuTenFile(Convert.ToString(CadApplication.GetSystemVariable("DWGNAME")));
            string fData = CsvXuat.DuongData(_thuMuc, _maDon);

            Dictionary<string, ThongTinTam> cu;
            try { cu = File.Exists(fData) ? CsvXuat.DocData(DocChiaSe(fData)) : new Dictionary<string, ThongTinTam>(); }
            catch (Exception ex) { TrangThai = "Không đọc được " + fData + ": " + ex.Message; return; }

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
            DanhGiaLai();
            Loc();

            string trung = CsvXuat.TenTrung(_tatCa.Where(d => d.Tam != null).Select(d => d.Tam));
            if (trung != null)
            {
                TrangThai = "Tên tấm trùng \"" + trung + "\". Sửa N rồi quét lại.";
                return;
            }
            if (!LuuData()) return;   // LuuData đã báo lỗi; đóng file rồi quét lại
            File35 = fData;
            TrangThai = "Quét " + _tatCa.Count(d => !d.BoQua) + " tấm, " + _tatCa.Count(d => d.CoLoi && !d.BoQua) + " lỗi · đã lưu data CSV.";
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
            }
            Bao(nameof(CoDsLoi)); Bao(nameof(NhanDsLoi));
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

        /// <summary>Nút Xuất CSV: file cuối &lt;đơn&gt;.csv từ danh sách đang hiện; còn thẻ lỗi thì không ghi.</summary>
        private void XuatCsv()
        {
            CloudLoiTam.Xoa();
            if (_thuMuc == null || _tatCa.Count == 0) { TrangThai = "Chưa có danh sách tấm — bấm Quét trước."; return; }
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc != _docDanhSach) { TrangThai = "Danh sách không thuộc bản vẽ đang mở — quét lại."; return; }
            int soLoi = _tatCa.Count(d => d.CoLoi && !d.BoQua);
            if (soLoi > 0)
            {
                TimKiem = "";   // từ khoá đang lọc không được che mất tấm lỗi
                ChiLoi = true;
                TrangThai = "Không xuất CSV: còn " + soLoi + " tấm lỗi — sửa rồi bấm Xuất CSV lại.";
                return;
            }
            var ds = CsvXuat.SapXep(_tatCa.Where(d => d.Tam != null).Select(d => d.Tam), CaiDat.SapXep);
            if (ds.Count == 0) { TrangThai = "Không có tấm nào để xuất."; return; }
            try { CsvXuat.GhiCuoi(_thuMuc, _maDon, ds); }
            catch (IOException) { TrangThai = _maDon + ".csv đang mở (Excel?) — đóng rồi bấm lại."; return; }
            catch (Exception ex) { TrangThai = "Không ghi được CSV: " + ex.Message; return; }
            File10 = CsvXuat.DuongCuoi(_thuMuc, _maDon);
            TrangThai = "Đã xuất " + ds.Count + " tấm ra " + _maDon + ".csv.";
            Mo(File10);
        }

        private void DoiBanVe(Document doc)
        {
            if (doc == _docDanhSach) return;   // kích hoạt lại đúng bản vẽ: giữ cloud
            CloudLoiTam.Xoa();
            _tatCa.Clear();
            _thuMuc = null;
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

        public bool CheDo0 { get => CaiDat.CheDoTinh == 0; set { if (value) DatCheDo(0); } }
        public bool CheDo1 { get => CaiDat.CheDoTinh == 1; set { if (value) DatCheDo(1); } }
        public bool CheDo2 { get => CaiDat.CheDoTinh == 2; set { if (value) DatCheDo(2); } }
        private void DatCheDo(int v) { CaiDat.CheDoTinh = v; Luu(nameof(CheDo0), nameof(CheDo1), nameof(CheDo2)); }

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
