using System.ComponentModel;
using System.Globalization;
using CsvTool.Core;

namespace CsvTool.ViewModels
{
    /// <summary>Một dòng tab Giá VL: chỉ nhập giá vật liệu, thông số gia công luôn lấy từ database.</summary>
    public sealed class DongGiaVl : INotifyPropertyChanged
    {
        private readonly CsvTabViewModel _vm;

        public string VatLieu { get; }
        public double DoDay { get; }
        public int SoTam { get; set; }

        public DongGiaVl(CsvTabViewModel vm, string vatLieu, double doDay, int soTam)
        {
            _vm = vm; VatLieu = vatLieu; DoDay = doDay; SoTam = soTam;
        }

        private DongBangGia Tam => _vm.GiaTamCua(VatLieu, DoDay);
        private DongBangGia Bang => _vm.GiaBangCua(VatLieu, DoDay);

        public bool SuaDuoc => true;
        public string Ten => VatLieu + " " + DoDay.ToString("0.0#####", CultureInfo.InvariantCulture);
        /// <summary>Không có trong database: thiếu thông số gia công nên chưa tính được, phải bổ sung.</summary>
        public bool ChuaCoDb => _vm.DaCoBangGia && Bang == null;
        public bool Thieu => ChuaCoDb || (_vm.DaCoBangGia && Tam == null && _vm.VlThieuGia(VatLieu, DoDay));
        public bool HienNhap => _vm.DaCoBangGia && Bang != null && (Tam != null || Bang.DonGia == 0);
        public bool HienGiaBang => !HienNhap;
        public string NhanGiaBang
        {
            get
            {
                if (ChuaCoDb) return "Chưa có";
                double? g = Tam?.DonGia ?? Bang?.DonGia;
                return g > 0 ? "@" + g.Value.ToString("#,0.##", CultureInfo.InvariantCulture) : "Chưa có";
            }
        }
        public string Nguon => ChuaCoDb ? "Chưa có trong database. Cần bổ sung."
            : Tam != null ? "Giá tạm của đơn"
            : !_vm.DaCoBangGia ? "Chưa đọc bảng giá. Tích Tính giá rồi quét."
            : Thieu ? "Giá trong database = 0. Cần nhập giá."
            : Bang.DonGia == 0 ? "Bảng giá = 0, nhập nếu cần" : "Bảng giá";

        public string GiaVL
        {
            get => Tam == null ? "" : Tam.DonGia.ToString("0.######", CultureInfo.InvariantCulture);
            set
            {
                string s = (value ?? "").Trim();
                if (s == GiaVL) return;
                if (s == "") _vm.XoaGiaVl(this);
                else if (TinhGia.SoTien(s, out double d) && d > 0) _vm.DatGiaVl(this, d);
                else _vm.BaoLoi("Giá vật liệu phải là số lớn hơn 0.");
                Bao(null);
            }
        }

        internal void CapNhat() => Bao(null);

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao(string ten) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }
}
