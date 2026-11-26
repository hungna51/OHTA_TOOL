using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZwCadPlugin.Core;

namespace ZwCadPlugin.Models
{
    /// <summary>Một thẻ lỗi hoặc cảnh báo trên tab Error của Guro (hoặc cửa sổ KT).</summary>
    public sealed class MucErrorItem : INotifyPropertyChanged
    {
        private int _viTriHienTai;
        private string _scaleInput = "0.3";
        private string _scaleThongBao = string.Empty;
        private bool _daXuLy;

        public string Key { get; set; } = string.Empty;
        public string TieuDe { get; set; } = string.Empty;
        public string ChiTiet { get; set; } = string.Empty;
        public bool LaLoi { get; set; }
        public bool TuDongKiemTra { get; set; } = true;

        public List<(double MinX, double MinY, double MaxX, double MaxY)> ViTri { get; set; } =
            new List<(double, double, double, double)>();
        public int ViTriHienTai
        {
            get => _viTriHienTai;
            set
            {
                if (_viTriHienTai != value)
                {
                    _viTriHienTai = value;
                    Bao();
                    Bao(nameof(NhanViTri));
                }
            }
        }
        public bool CoViTri => ViTri != null && ViTri.Count > 0;
        public string NhanViTri => ViTri != null && ViTri.Count > 0 ? (_viTriHienTai + 1) + "/" + ViTri.Count : string.Empty;

        public bool LaByLayer { get; set; }
        public List<long> ByLayerHandles { get; set; } = new List<long>();

        public bool LaMaKhacChuaDat { get; set; }
        public string ScaleInput
        {
            get => _scaleInput;
            set { if (_scaleInput != value) { _scaleInput = value; Bao(); } }
        }
        public string ScaleThongBao
        {
            get => _scaleThongBao;
            set
            {
                if (_scaleThongBao != value)
                {
                    _scaleThongBao = value;
                    Bao();
                    Bao(nameof(CoScaleThongBao));
                }
            }
        }
        public bool CoScaleThongBao => !string.IsNullOrEmpty(_scaleThongBao);

        public bool CoNutDanhSach { get; set; }
        public bool LocSht { get; set; }
        public bool CoNutTuDien { get; set; }
        public List<string> HandlesZoom { get; set; } = new List<string>();
        public bool CoNutChonZoom => HandlesZoom != null && HandlesZoom.Count > 0;

        public bool DaXuLy
        {
            get => _daXuLy;
            set { if (_daXuLy != value) { _daXuLy = value; Bao(); Bao(nameof(ChoDanhDau)); } }
        }
        public bool ChoDanhDau => !TuDongKiemTra && !_daXuLy;

        public KhoiCanhBao KhoiGoc { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao([CallerMemberName] string ten = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }
}
