using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.ViewModels
{
    /// <summary>
    /// Cửa sổ ⚙ Cài đặt GURO (mục 194): sửa trên bản sao, chỉ lưu khi OK. Luật BẬT/TẮT ô:
    ///   tên layer khung tấm, xếp cụm  bật khi "Tự tạo khung tấm"
    ///   mã khắc                       bật khi tự tạo khung tấm VÀ xếp cụm
    ///   tự nhận diện Dim              bật khi "Xử lý nét"
    /// </summary>
    public sealed class GuroCaiDatViewModel : INotifyPropertyChanged
    {
        private readonly GroSettings _s;

        public GuroCaiDatViewModel(GroSettings goc)
        {
            _s = (goc ?? new GroSettings()).Sao();
        }

        public bool UseTaoKhung
        {
            get => _s.UseTaoKhung;
            set { _s.UseTaoKhung = value; Bao(); Bao(nameof(BatXepCum)); Bao(nameof(BatDatMk)); }
        }

        public string LayerKhungTam
        {
            get => _s.LayerKhungTam;
            set { _s.LayerKhungTam = value; Bao(); Bao(nameof(Loi)); Bao(nameof(DuocLuu)); }
        }

        public bool UseInPdf { get => _s.UseInPdf; set { _s.UseInPdf = value; Bao(); } }

        public bool XepCum
        {
            get => _s.XepCum;
            set { _s.XepCum = value; Bao(); Bao(nameof(BatDatMk)); }
        }

        public bool DatMk { get => _s.DatMk; set { _s.DatMk = value; Bao(); } }

        public bool UseXln { get => _s.UseXln; set { _s.UseXln = value; Bao(); } }

        public bool AutoDim { get => _s.AutoDim; set { _s.AutoDim = value; Bao(); } }

        public bool BatXepCum => _s.UseTaoKhung;
        public bool BatDatMk => _s.UseTaoKhung && _s.XepCum;

        /// <summary>Lỗi tên layer khung tấm (null = được).</summary>
        public string Loi => TenKhungTam.Loi(_s.LayerKhungTam);
        public bool DuocLuu => Loi == null;

        public GroSettings KetQua()
        {
            GroSettings kq = _s.Sao();
            kq.LayerKhungTam = TenKhungTam.ChuanHoa(kq.LayerKhungTam);
            return kq;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao([CallerMemberName] string ten = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
        }
    }
}
