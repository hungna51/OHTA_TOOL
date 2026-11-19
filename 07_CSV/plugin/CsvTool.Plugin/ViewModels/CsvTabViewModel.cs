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
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
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
    /// Tab CSV. Nút Làm mới / EXPORT gửi lệnh CSVLAMMOI / CSVEXPORT (chọn khung cần ngữ cảnh lệnh),
    /// lệnh quét xong gọi NhanKetQua. Sửa ô ghi thẳng bản vẽ; CSV chỉ ghi khi EXPORT.
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
        private Document _docDanhSach;

        public ICommand B1Cmd { get; }
        public ICommand ExportCmd { get; }
        public ICommand LamMoiCmd { get; }
        public ICommand CaiDatCmd { get; }
        public ICommand MoFile10Cmd { get; }
        public ICommand MoFile35Cmd { get; }

        public CsvTabViewModel(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            TieuDe = tieuDe; MoTa = moTa; ThuMucModule = thuMucModule;
            try { AccentBrush = (Brush)new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(mauNhan) ? "#1E7F4F" : mauNhan); }
            catch { AccentBrush = Brushes.SeaGreen; }
            try { CaiDat = CaiDatRegistry.Doc(); } catch { CaiDat = new CaiDatCsv(); }

            B1Cmd = new Lenh(ChayB1);
            ExportCmd = new Lenh(() => GuiLenh("CSVEXPORT "));
            LamMoiCmd = new Lenh(() => GuiLenh("CSVLAMMOI "));
            CaiDatCmd = new Lenh(() => HienCaiDat = !HienCaiDat);
            MoFile10Cmd = new Lenh(() => Mo(File10));
            MoFile35Cmd = new Lenh(() => Mo(File35));

            HienTai = this;
            try { CadApplication.DocumentManager.DocumentActivated += (s, e) => DoiBanVe(e.Document); } catch { }
        }

        // ── Kết quả quét ──────────────────────────────────────────────

        public void NhanKetQua(Document doc, List<KhungTho> khung, int dimzin, bool export)
        {
            VongDoTam.Xoa();
            _docDanhSach = doc;
            var theoHandle = khung.ToDictionary(k => k.Handle);
            var tot = new List<TamXuat>();
            var loi = new List<DongTam>();
            foreach (KhungTho k in khung)
            {
                var (tam, l) = PhanTichKhung.Chay(k, CaiDat.LopDimDung, dimzin);
                if (tam != null) tot.Add(tam); else loi.Add(new DongTam(this, k, null, l));
            }
            var sx = CsvXuat.SapXep(tot, CaiDat.SapXep);
            string trung = CsvXuat.TenTrung(sx);
            _tatCa = loi.Concat(sx.Select(t => new DongTam(this, theoHandle[t.HandleKhung], t,
                                                           trung != null && t.N == trung ? "Tên tấm trùng: " + trung : null))).ToList();
            int soLoi = _tatCa.Count(d => d.CoLoi);
            Loc();

            if (!export)
            {
                TrangThai = _tatCa.Count + " tấm, " + soLoi + " lỗi.";
                return;
            }
            if (soLoi > 0)
            {
                ChiLoi = true;
                TrangThai = "Không xuất CSV: " + soLoi + " tấm lỗi — sửa rồi bấm EXPORT lại.";
                return;
            }
            string thuMuc = Convert.ToString(CadApplication.GetSystemVariable("DWGPREFIX"));
            bool daLuu = Convert.ToInt32(CadApplication.GetSystemVariable("DWGTITLED")) == 1;
            if (!daLuu)
            {
                TrangThai = "Bản vẽ chưa lưu — lưu bản vẽ rồi bấm EXPORT lại.";
                return;
            }
            string maDon = MaDon.TuTenFile(Convert.ToString(CadApplication.GetSystemVariable("DWGNAME")));
            try
            {
                CsvXuat.GhiHaiFile(thuMuc, maDon, sx);
            }
            catch (IOException ex)
            {
                TrangThai = "File CSV đang mở (Excel?) — đóng rồi bấm lại. (" + ex.Message + ")";
                return;
            }
            catch (Exception ex)
            {
                TrangThai = "Không ghi được CSV: " + ex.Message;
                return;
            }
            File10 = Path.Combine(thuMuc, maDon + ".csv");
            File35 = Path.Combine(thuMuc, "data", maDon + "-data.csv");
            TrangThai = "Đã xuất " + sx.Count + " tấm.";
            Mo(File10);
        }

        private void DoiBanVe(Document doc)
        {
            VongDoTam.Xoa();
            if (doc == _docDanhSach) return;
            _tatCa.Clear();
            Loc();
            TrangThai = "Đổi bản vẽ — bấm Làm mới.";
        }

        // ── Sửa ô ─────────────────────────────────────────────────────

        internal bool SuaThongTin(DongTam d, string nhan, string giaTri) =>
            ThuGhi(doc => GhiNguocCad.SuaThongTin(doc, d.Tam.HandleKhung, nhan, giaTri));

        internal bool SuaFname(DongTam d, char truong, string giaTri) =>
            ThuGhi(doc => GhiNguocCad.SuaFname(doc, d.Tam.HandleKhung, truong, giaTri));

        private bool ThuGhi(Action<Document> ghi)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null || doc != _docDanhSach)
            {
                BaoLoi("Danh sách không thuộc bản vẽ đang mở — bấm Làm mới.");
                return false;
            }
            try { ghi(doc); TrangThai = "Đã ghi vào bản vẽ (CSV chỉ đổi khi bấm EXPORT)."; return true; }
            catch (Exception ex) { BaoLoi("Không ghi được: " + ex.Message); return false; }
        }

        internal void BaoLoi(string s) => TrangThai = s;

        // ── Chọn dòng → zoom / vòng đỏ ────────────────────────────────

        public void ChonDong(DongTam d)
        {
            VongDoTam.Xoa();
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (d == null || doc == null || doc != _docDanhSach) return;
            KhungTho k = d.Khung;
            ZoomCad.ToiHop(doc, k.MinX, k.MinY, k.MaxX, k.MaxY);
            if (d.CoLoi)
            {
                double w = k.MaxX - k.MinX, h = k.MaxY - k.MinY;
                try { VongDoTam.Hien(doc, (k.MinX + k.MaxX) / 2, (k.MinY + k.MaxY) / 2, 0.55 * Math.Sqrt(w * w + h * h)); }
                catch (Exception ex) { TrangThai = "Không vẽ được vòng đỏ: " + ex.Message; }
            }
        }

        // ── Lọc, trạng thái ───────────────────────────────────────────

        private bool _chiLoi;
        public bool ChiLoi { get => _chiLoi; set { _chiLoi = value; Bao(); Loc(); } }

        private void Loc()
        {
            DongHien.Clear();
            foreach (DongTam d in _tatCa.Where(d => !_chiLoi || d.CoLoi)) DongHien.Add(d);
        }

        private string _trangThai = "Bấm Làm mới rồi chọn khung in để xem danh sách tấm.";
        public string TrangThai { get => _trangThai; set { _trangThai = value; Bao(); } }

        private string _file10, _file35;
        public string File10 { get => _file10; private set { _file10 = value; Bao(); Bao(nameof(CoFile)); } }
        public string File35 { get => _file35; private set { _file35 = value; Bao(); } }
        public bool CoFile => !string.IsNullOrEmpty(_file10);

        private void Mo(string f)
        {
            if (string.IsNullOrEmpty(f) || !File.Exists(f)) return;
            try { Process.Start(new ProcessStartInfo(f) { UseShellExecute = true }); }
            catch (Exception ex) { TrangThai = "Không mở được " + f + ": " + ex.Message; }
        }

        // ── B1 + cài đặt ──────────────────────────────────────────────

        private void ChayB1()
        {
            try { ChayB1Lisp.Chay(CaiDat, ThuMucModule); }
            catch (Exception ex) { TrangThai = "Không chạy được Xuất thông tin tấm: " + ex.Message; }
        }

        private void GuiLenh(string lenh)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            // ESC xoá luôn khung chọn sẵn (pickfirst) → chỉ huỷ khi đang có lệnh chạy dở
            bool dangLenh = !string.IsNullOrEmpty(Convert.ToString(CadApplication.GetSystemVariable("CMDNAMES")));
            doc.SendStringToExecute((dangLenh ? "\x03\x03" : "") + lenh, true, false, false);
        }

        private bool _hienCaiDat;
        public bool HienCaiDat { get => _hienCaiDat; set { _hienCaiDat = value; Bao(); } }

        public bool CheDo0 { get => CaiDat.CheDoTinh == 0; set { if (value) DatCheDo(0); } }
        public bool CheDo1 { get => CaiDat.CheDoTinh == 1; set { if (value) DatCheDo(1); } }
        public bool CheDo2 { get => CaiDat.CheDoTinh == 2; set { if (value) DatCheDo(2); } }
        private void DatCheDo(int v) { CaiDat.CheDoTinh = v; Luu(nameof(CheDo0), nameof(CheDo1), nameof(CheDo2)); }

        public bool SxTraiPhai { get => CaiDat.SapXep == KieuSapXep.TraiSangPhai; set { if (value) DatSx(KieuSapXep.TraiSangPhai); } }
        public bool SxTrenDuoi { get => CaiDat.SapXep == KieuSapXep.TrenXuongDuoi; set { if (value) DatSx(KieuSapXep.TrenXuongDuoi); } }
        public bool SxChuCai { get => CaiDat.SapXep == KieuSapXep.ChuCai; set { if (value) DatSx(KieuSapXep.ChuCai); } }
        private void DatSx(KieuSapXep k) { CaiDat.SapXep = k; Luu(nameof(SxTraiPhai), nameof(SxTrenDuoi), nameof(SxChuCai)); }

        public bool DungLopMacDinh { get => CaiDat.DungLopMacDinh; set { CaiDat.DungLopMacDinh = value; Luu(nameof(DungLopMacDinh), nameof(LopTuChon)); } }
        public bool LopTuChon => !CaiDat.DungLopMacDinh;
        public string LopKhung { get => CaiDat.LopKhung; set { CaiDat.LopKhung = Gon(value, CaiDatCsv.LopKhungMacDinh); Luu(nameof(LopKhung)); } }
        public string LopDim { get => CaiDat.LopDim; set { CaiDat.LopDim = Gon(value, CaiDatCsv.LopDimMacDinh); Luu(nameof(LopDim)); } }
        public string LopTapu { get => CaiDat.LopTapu; set { CaiDat.LopTapu = Gon(value, CaiDatCsv.LopTapuMacDinh); Luu(nameof(LopTapu)); } }
        public string BuocSpline { get => So(CaiDat.BuocSpline); set { if (DocSo(value, out double d)) CaiDat.BuocSpline = d; Luu(nameof(BuocSpline)); } }
        public string DungSaiKheHo { get => So(CaiDat.DungSaiKheHo); set { if (DocSo(value, out double d)) CaiDat.DungSaiKheHo = d; Luu(nameof(DungSaiKheHo)); } }

        private static string Gon(string s, string macDinh) => string.IsNullOrWhiteSpace(s) ? macDinh : s.Trim();
        private static string So(double d) => d.ToString(CultureInfo.InvariantCulture);
        private static bool DocSo(string s, out double d) =>
            double.TryParse((s ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d > 0;

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
