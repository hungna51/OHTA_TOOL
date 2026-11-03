using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.ViewModels
{
    /// <summary>
    /// ViewModel của dialog gro. Luật BẬT/TẮT ô giữ đúng như DCL gro_dlg:
    ///   layer vùng          bật khi tích "Đánh số theo vùng"
    ///   layer gốc, auto Dim bật khi tích "Bật chuyển layer nét"
    ///   chọn màu, pick nét  bật khi tích xử lý nét VÀ KHÔNG tự nhận diện màu
    /// </summary>
    public sealed class GroSettingsViewModel : INotifyPropertyChanged
    {
        private LayerMuc _khung, _vung, _xln;
        private bool _useVung, _useInPdf, _useTaoKhung, _useXln, _autoDim;
        private int _mauDim = 4;
        private bool _xepCum = true, _datMk = true;

        public List<LayerMuc> DsKhung { get; }
        public List<LayerMuc> DsVung { get; }
        public List<LayerMuc> DsXln { get; }

        /// <summary>Không lọc được layer nào có khung -> popup đang liệt kê TẤT CẢ layer.</summary>
        public string CanhBaoKhung { get; }
        public string CanhBaoVung { get; }

        /// <param name="giuXln">
        /// true = chọn ĐÚNG layer gốc đã truyền vào, không áp luật mục 75. Dùng khi mở lại
        /// dialog sau "Xem DSN": người dùng vừa tự chọn layer đó, không được đổi ngầm.
        /// </param>
        public GroSettingsViewModel(List<LayerMuc> dsKhung, bool locKhung,
                                    List<LayerMuc> dsVung, bool locVung,
                                    List<LayerMuc> dsXln, GroSettings mac,
                                    bool giuXln = false)
        {
            DsKhung = dsKhung;
            DsVung = dsVung;
            DsXln = dsXln;
            CanhBaoKhung = locKhung ? null
                : "Không có polyline kín 4 đỉnh nào — đang liệt kê TẤT CẢ layer.";
            CanhBaoVung = locVung ? null
                : "Không có polyline kín từ 3 đỉnh nào — đang liệt kê TẤT CẢ layer.";

            _khung = Lay(dsKhung, LayerRanking.ViTri(dsKhung, mac.LayerKhung));
            _vung = Lay(dsVung, LayerRanking.ViTri(dsVung, mac.LayerVung));
            _xln = Lay(dsXln, giuXln ? LayerRanking.ViTri(dsXln, mac.LayerXln)
                                     : LayerRanking.ViTriXln(dsXln, mac.LayerXln));
            _useVung = mac.UseVung;
            _useInPdf = mac.UseInPdf;
            _useTaoKhung = mac.UseTaoKhung;
            _useXln = mac.UseXln;
            _autoDim = mac.AutoDim;
            _mauDim = mac.MauDim > 0 ? mac.MauDim : 4;
            _xepCum = mac.XepCum;
            _datMk = mac.DatMk;
        }

        private static LayerMuc Lay(List<LayerMuc> ds, int i)
        {
            return (ds != null && i >= 0 && i < ds.Count) ? ds[i] : null;
        }

        public LayerMuc ChonKhung { get => _khung; set { _khung = value; Bao(); } }
        public LayerMuc ChonVung { get => _vung; set { _vung = value; Bao(); } }
        public LayerMuc ChonXln { get => _xln; set { _xln = value; Bao(); } }

        public bool UseInPdf { get => _useInPdf; set { _useInPdf = value; Bao(); } }
        public bool UseTaoKhung
        {
            get => _useTaoKhung;
            set { _useTaoKhung = value; Bao(); Bao(nameof(BatSapXep)); Bao(nameof(BatDatMk)); }
        }

        // ---------- Mục 122: sắp xếp cụm + mã khắc vào hình ----------
        /// <summary>Hai bước sau đều cần khung Khung_IN -> chỉ bật khi "Tự động tạo khung bao".</summary>
        public bool BatSapXep => _useTaoKhung;

        /// <summary>
        /// Xếp cụm mirror / gia công sang cột phải + tách cặp mirror sinh hình nghịch. Là ô CHÍNH:
        /// tắt thì lisp bỏ qua cả tách va chạm khung in (6f) và đặt mã khắc (6g).
        /// </summary>
        public bool XepCum
        {
            get => _xepCum;
            set { _xepCum = value; Bao(); Bao(nameof(BatDatMk)); }
        }

        public bool BatDatMk => _useTaoKhung && _xepCum;

        /// <summary>
        /// Đặt mã khắc + F-name vào trong hình tấm đơn / mirror. Mục 170: chỉ một cách (trượt từ
        /// góc trên-trái, chạy bằng C#), không còn lựa chọn chế độ.
        /// </summary>
        public bool DatMk
        {
            get => _datMk;
            set { _datMk = value; Bao(); }
        }

        public bool UseVung
        {
            get => _useVung;
            set { _useVung = value; Bao(); Bao(nameof(BatLayerVung)); }
        }

        public bool UseXln
        {
            get => _useXln;
            set { _useXln = value; Bao(); Bao(nameof(BatXln)); Bao(nameof(BatChonMau)); }
        }

        public bool AutoDim
        {
            get => _autoDim;
            set { _autoDim = value; Bao(); Bao(nameof(BatChonMau)); Bao(nameof(NhanMau)); }
        }

        public int MauDim
        {
            get => _mauDim;
            set { _mauDim = value; Bao(); Bao(nameof(NhanMau)); }
        }

        public string NhanMau => _autoDim ? "Tự động: Hình học (3 loại)" : ("Thủ công: " + GroText.TenMau(_mauDim));

        public bool BatLayerVung => _useVung;
        public bool BatXln => _useXln;
        public bool BatChonMau => _useXln && !_autoDim;

        /// <summary>
        /// Chọn màu bằng tay (hộp màu hoặc pick nét mẫu) thì TẮT tự nhận diện — giống
        /// lisp: cur-auto-dim nil sau khi người dùng chọn.
        /// </summary>
        public void DatMauTay(int aci)
        {
            AutoDim = false;
            MauDim = aci;
        }

        public GroSettings KetQua()
        {
            return new GroSettings
            {
                UseVung = _useVung,
                LayerVung = _vung?.Ten,
                LayerKhung = _khung?.Ten,
                UseInPdf = _useInPdf,
                UseXln = _useXln,
                LayerXln = _xln?.Ten,
                AutoDim = _autoDim,
                MauDim = _mauDim,
                UseTaoKhung = _useTaoKhung,
                XepCum = _xepCum,
                DatMk = _datMk,
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao([CallerMemberName] string ten = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
        }
    }
}
