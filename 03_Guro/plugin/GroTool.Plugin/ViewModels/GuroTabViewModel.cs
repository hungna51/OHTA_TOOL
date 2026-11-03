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
using ZwCadPlugin.Views;

namespace ZwCadPlugin.ViewModels
{
    /// <summary>
    /// Tab Guro trên bảng OHTA (mục 192, 194). Mặt tab chỉ chọn layer cho BẢN VẼ ĐANG MỞ
    /// (LayerTheoBanVe) + ô đánh số theo vùng; cài đặt chung nằm ở cửa sổ ⚙ (GroRegistry).
    /// Danh sách layer làm mới khi chuyển bản vẽ, khi tab hiện lại và khi bấm vào ô chọn.
    /// </summary>
    public sealed class GuroTabViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Chỉ tab đang hiện mới nghe sự kiện: OHTARELOAD dựng lại palette tạo ViewModel
        /// mới, ViewModel cũ thôi đọc bản vẽ.
        /// </summary>
        private static GuroTabViewModel _hienTai;
        private static bool _daMoc;

        private readonly System.Windows.Threading.Dispatcher _dispatcher;
        private readonly string _thuMucModule;
        private bool _dangNap;

        private GroSettings _caiDat = new GroSettings();
        private List<LayerMuc> _dsKhung = new List<LayerMuc>(), _dsVung = new List<LayerMuc>(), _dsXln = new List<LayerMuc>();
        private LayerMuc _chonKhung, _chonVung, _chonXln;
        private string _trangThai;

        public GuroTabViewModel(string tieuDe, string moTa, string mauNhan, string thuMucModule)
        {
            TieuDe = string.IsNullOrWhiteSpace(tieuDe) ? "GURO" : tieuDe;
            MoTa = moTa ?? "";
            AccentBrush = TaoBrush(mauNhan, "#323639");
            _thuMucModule = thuMucModule;

            ChayGroCommand = new RelayCommand(() => GuiLenh("GROCHAY"));   // GRO chỉ mở tab (mục 193)
            ChayGiCommand = new RelayCommand(() => GuiLenh("GI"));
            MoDstCommand = new RelayCommand(() => GuiLenh("DST"));
            CaiDatCommand = new RelayCommand(MoCaiDat);

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
        public ICommand CaiDatCommand { get; }

        public string TrangThai { get => _trangThai; private set { _trangThai = value; Bao(); } }

        /// <summary>Một dòng các cài đặt chung đang bật (chi tiết ở cửa sổ ⚙).</summary>
        public string TomTat
        {
            get
            {
                var bat = new List<string>();
                if (_caiDat.UseTaoKhung) bat.Add("Khung tấm: " + _caiDat.LayerKhungTam);
                if (_caiDat.UseInPdf) bat.Add("In PDF");
                if (_caiDat.UseTaoKhung && _caiDat.XepCum) bat.Add("Xếp cụm");
                if (_caiDat.UseTaoKhung && _caiDat.XepCum && _caiDat.DatMk) bat.Add("Mã khắc");
                if (_caiDat.UseXln) bat.Add(_caiDat.AutoDim ? "Xử lý nét + Dim" : "Xử lý nét");
                return bat.Count == 0 ? "Không bật cài đặt nào" : string.Join(" · ", bat);
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
                catch (Exception ex) { TrangThai = "Không lưu được: " + ex.Message; }
            }
        }

        /// <summary>Ô layer gốc chỉ hiện khi bật xử lý nét trong ⚙.</summary>
        public bool UseXln => _caiDat.UseXln;

        // ---------------------------- Layer theo bản vẽ ----------------------------

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
            try { LayerTheoBanVe.Dat(Application.DocumentManager.MdiActiveDocument, viTri, giaTri.Ten); }
            catch { }
        }

        // ---------------------------- Nạp ----------------------------

        /// <summary>Đọc lại cài đặt chung và layer của bản vẽ đang mở.</summary>
        public void LamMoi()
        {
            try { _caiDat = GroRegistry.Doc(); }
            catch (Exception ex) { TrangThai = "Không đọc được cài đặt: " + ex.Message; }
            Bao(nameof(UseVung));
            Bao(nameof(UseXln));
            Bao(nameof(TomTat));
            NapLayer();
        }

        /// <summary>Làm mới sau khi CAD xong việc đang làm (đổi bản vẽ).</summary>
        private void LamMoiSau()
        {
            try { _dispatcher.BeginInvoke(new Action(LamMoi)); }
            catch { }
        }

        public void NapLayer()
        {
            Document doc = null;
            try { doc = Application.DocumentManager.MdiActiveDocument; } catch { }

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

        // ---------------------------- Cài đặt ⚙ ----------------------------

        private void MoCaiDat()
        {
            try
            {
                var w = new GuroCaiDatWindow(new GuroCaiDatViewModel(GroRegistry.Doc()));
                Application.ShowModalWindow(w);
                if (w.DaLuu) LamMoi();
            }
            catch (Exception ex)
            {
                TrangThai = "Không mở được cài đặt: " + ex.Message;
            }
        }

        // ---------------------------- Tiện ích ----------------------------

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
