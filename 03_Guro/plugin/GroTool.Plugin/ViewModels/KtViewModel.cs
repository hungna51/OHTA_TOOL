using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

namespace ZwCadPlugin.ViewModels
{
    /// <summary>ViewModel cho cửa sổ nổi lệnh KT (Kiểm tra kín hở &amp; thuộc tính nét cắt theo vùng chọn).</summary>
    public sealed class KtViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<Document, List<long>> _tapChonTheoBanVe = new Dictionary<Document, List<long>>();
        private double _saiSo;
        private string _saiSoText;
        private string _tenBanVe = "Chưa mở bản vẽ";
        private string _trangThai = "Sẵn sàng";
        private bool _daChon;
        private bool _ghimTrenCung;

        public ObservableCollection<MucErrorItem> DsLoi { get; } = new ObservableCollection<MucErrorItem>();

        public KtViewModel()
        {
            _saiSo = GroRegistry.DocSaiSoKinHo();
            _saiSoText = KiemTraKinHo.DinhDangSaiSo(_saiSo);
            _ghimTrenCung = GroRegistry.DocGhimKt();

            ChonLaiCmd = new RelayCommand(() => YeuCauChonLai?.Invoke());
            KiemTraLaiCmd = new RelayCommand(KiemTraLai);
            ApDungSaiSoCmd = new RelayCommand(() => { ApDungSaiSo(); KiemTraLai(); });
            BamLoiCmd = new RelayCommand(p => BamLoi(p as MucErrorItem));
            DuaVeByLayerCmd = new RelayCommand(p => DuaVeByLayer(p as MucErrorItem));
        }

        public Action YeuCauChonLai { get; set; }

        public ICommand ChonLaiCmd { get; }
        public ICommand KiemTraLaiCmd { get; }
        public ICommand ApDungSaiSoCmd { get; }
        public ICommand BamLoiCmd { get; }
        public ICommand DuaVeByLayerCmd { get; }

        public string TenBanVe { get => _tenBanVe; private set { _tenBanVe = value; Bao(); } }
        public string TrangThai { get => _trangThai; private set { _trangThai = value; Bao(); } }
        public bool DaChon { get => _daChon; private set { _daChon = value; Bao(); } }

        public bool GhimTrenCung
        {
            get => _ghimTrenCung;
            set
            {
                if (_ghimTrenCung == value) return;
                _ghimTrenCung = value;
                Bao();
                GroRegistry.GhiGhimKt(value);
            }
        }

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

        public bool SachLoi => DsLoi.Count == 0;
        public bool HienSachLoi => DaChon && DsLoi.Count == 0;
        public bool MauXanhDem => DaChon && DsLoi.Count == 0;
        public string NhanDem => !DaChon ? "Chưa chọn" : (DsLoi.Count > 0 ? DsLoi.Count + " nhóm lỗi" : "✓ Sạch lỗi");

        public bool CoTapChon(Document doc)
            => doc != null && _tapChonTheoBanVe.TryGetValue(doc, out var ds) && ds != null && ds.Count > 0;

        public void CapNhatTapChon(Document doc, List<long> handles)
        {
            if (doc == null) return;
            _tapChonTheoBanVe[doc] = handles ?? new List<long>();
            KiemTraLai();
        }

        private void ApDungSaiSo()
        {
            if (KiemTraKinHo.ChuanHoaSaiSo(_saiSoText, out double tol))
            {
                _saiSo = tol;
                GroRegistry.GhiSaiSoKinHo(tol);
            }
            else
            {
                _saiSoText = KiemTraKinHo.DinhDangSaiSo(_saiSo);
                Bao(nameof(SaiSoText));
            }
        }

        public void KiemTraLai()
        {
            CloudLoiCad.Xoa();
            DsLoi.Clear();

            Document doc = null;
            try { doc = CadApplication.DocumentManager.MdiActiveDocument; } catch { }
            if (doc == null)
            {
                DaChon = false;
                TenBanVe = "Chưa mở bản vẽ";
                TrangThai = "Không có bản vẽ đang mở.";
                ThongBaoTrangThai();
                return;
            }

            TenBanVe = Path.GetFileName(doc.Name);
            if (!_tapChonTheoBanVe.TryGetValue(doc, out var handles) || handles == null || handles.Count == 0)
            {
                DaChon = false;
                TrangThai = "Chưa quét chọn đối tượng — bấm '⬚ Chọn lại' hoặc gõ KT.";
                ThongBaoTrangThai();
                return;
            }

            try
            {
                var kq = KiemTraKinHoCad.QuetTheoTapChon(doc, handles, _saiSo);
                if (kq.SoDoiTuong == 0)
                {
                    DaChon = false;
                    TrangThai = "Các đối tượng đã chọn không còn tồn tại — bấm '⬚ Chọn lại'.";
                    ThongBaoTrangThai();
                    return;
                }

                DaChon = true;
                foreach (LoiKinHo l in kq.Loi)
                {
                    DsLoi.Add(new MucErrorItem
                    {
                        Key = "KT:" + l.Ma,
                        TieuDe = l.TieuDe,
                        ChiTiet = l.ChiTiet,
                        LaLoi = true,
                        TuDongKiemTra = true,
                        ViTri = l.ViTri,
                        LaByLayer = l.LaByLayer,
                        ByLayerHandles = l.Handles,
                    });
                }
                TrangThai = DsLoi.Count == 0
                    ? $"Đã quét {kq.SoDoiTuong} đối tượng ({kq.SoNetKakou} nét 0-KAKOU) — sạch lỗi ({KiemTraKinHo.DinhDangSaiSo(_saiSo)} mm) & ByLayer."
                    : $"Đã quét {kq.SoDoiTuong} đối tượng — có {DsLoi.Count} nhóm lỗi (bấm từng dòng để nhảy Cloud đỏ).";
            }
            catch (Exception ex)
            {
                DaChon = false;
                OhtaBugBridge.Log("GRO/KtViewModel", ex);
                TrangThai = "Lỗi kiểm tra: " + ex.Message;
            }
            ThongBaoTrangThai();
        }

        private void ThongBaoTrangThai()
        {
            Bao(nameof(DaChon));
            Bao(nameof(HienSachLoi));
            Bao(nameof(MauXanhDem));
            Bao(nameof(SachLoi));
            Bao(nameof(NhanDem));
        }

        private void BamLoi(MucErrorItem item)
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
            KiemTraLai();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao([CallerMemberName] string ten = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }
}
