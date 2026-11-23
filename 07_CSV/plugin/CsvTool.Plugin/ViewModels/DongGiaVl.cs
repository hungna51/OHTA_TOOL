using System;
using System.ComponentModel;
using System.Globalization;
using CsvTool.Core;

namespace CsvTool.ViewModels
{
    /// <summary>Một dòng tab Giá VL: vật liệu + độ dày trong đơn, giá từ bảng giá hoặc giá tạm của đơn.</summary>
    public sealed class DongGiaVl : INotifyPropertyChanged
    {
        private readonly CsvTabViewModel _vm;
        private bool _daSuaPhu;   // đang nhập dở: chưa đủ thông số, không đè bằng dữ liệu bảng giá

        public string VatLieu { get; }
        public double DoDay { get; }
        public int SoTam { get; set; }
        internal DongBangGia Nhap { get; private set; }

        public DongGiaVl(CsvTabViewModel vm, string vatLieu, double doDay, int soTam)
        {
            _vm = vm; VatLieu = vatLieu; DoDay = doDay; SoTam = soTam;
            CapNhat();
        }

        private DongBangGia Tam => _vm.GiaTamCua(VatLieu, DoDay);
        private DongBangGia Bang => _vm.GiaBangCua(VatLieu, DoDay);

        public bool SuaDuoc => true;
        public string Ten => VatLieu + " " + DoDay.ToString("0.0#####", CultureInfo.InvariantCulture);
        public bool Thieu => _vm.DaCoBangGia && Tam == null && Bang == null;
        /// <summary>Không có trong database (kể cả đã nhập giá tạm): phải báo để bổ sung.</summary>
        public bool ChuaCoDb => _vm.DaCoBangGia && Bang == null;
        /// <summary>Chỉ cho nhập khi đã đọc bảng giá, để biết chắc vật liệu có trong database hay không.</summary>
        public bool HienNhap => _vm.DaCoBangGia && (Tam != null || Bang == null || Bang.DonGia == 0);
        public bool HienGiaBang => !HienNhap;
        public string NhanGiaBang
        {
            get
            {
                double? g = Tam?.DonGia ?? Bang?.DonGia;
                return g > 0 ? "@" + g.Value.ToString("#,0.##", CultureInfo.InvariantCulture) : "Chưa có";
            }
        }
        public string Nguon => ChuaCoDb ? (Tam != null ? "Giá tạm. Cần bổ sung vào database." : "Chưa có trong database. Cần bổ sung.")
            : Tam != null ? "Giá tạm của đơn"
            : !_vm.DaCoBangGia ? "Chưa đọc bảng giá. Tích Tính giá rồi quét."
            : Bang.DonGia == 0 ? "Bảng giá = 0, nhập nếu cần" : "Bảng giá";

        public string GiaVL
        {
            get => SoHien(Nhap.DonGia);
            set
            {
                string s = (value ?? "").Trim();
                if (s == GiaVL) return;
                if (s == "")
                {
                    if (Tam != null) _vm.XoaGiaVl(this);
                    else Nhap.DonGia = double.NaN;
                }
                else if (!TinhGia.SoTien(s, out double d) || d <= 0) _vm.BaoLoi("Giá vật liệu phải là số lớn hơn 0.");
                else if (Tam != null) _vm.DatGiaVl(this, d);
                else
                {
                    Nhap.DonGia = d;
                    _daSuaPhu = true;
                    LuuNeuDu();
                }
                Bao(null);
            }
        }

        public string Cat { get => SoHien(Nhap.GiaCat); set => SuaPhu(value, Nhap.GiaCat, x => Nhap.GiaCat = x); }
        public string Uon { get => SoHien(Nhap.GiaUon); set => SuaPhu(value, Nhap.GiaUon, x => Nhap.GiaUon = x); }
        public string MoLo { get => SoHien(Nhap.GiaMoLo); set => SuaPhu(value, Nhap.GiaMoLo, x => Nhap.GiaMoLo = x); }
        public string Tapu { get => SoHien(Nhap.GiaTappu); set => SuaPhu(value, Nhap.GiaTappu, x => Nhap.GiaTappu = x); }
        public string GiaVat { get => SoHien(Nhap.GiaVat); set => SuaPhu(value, Nhap.GiaVat, x => Nhap.GiaVat = x); }
        public string TyTrong { get => SoHien(Nhap.TyTrong); set => SuaPhu(value, Nhap.TyTrong, x => Nhap.TyTrong = x, true); }

        private static string SoHien(double d) => double.IsNaN(d) ? "" : d.ToString("0.######", CultureInfo.InvariantCulture);

        private void LuuNeuDu()
        {
            if (GiaVlDon.Du(Nhap)) _vm.DatGiaVl(this, Nhap.DonGia);
            else _vm.BaoLoi("Nhập đủ giá VL, cắt, uốn, mở lỗ, Tapu, vát và tỷ trọng để tính tiền.");
        }

        private void SuaPhu(string giaTri, double cu, Action<double> gan, bool phaiDuong = false)
        {
            if ((giaTri ?? "").Trim() == "" && Tam == null) { gan(double.NaN); _daSuaPhu = true; }
            else if (TinhGia.SoTien(giaTri, out double d) && (phaiDuong ? d > 0 : d >= 0))
            {
                if (d != cu)
                {
                    if (Tam != null) _vm.SuaPhuGiaVl(this, () => gan(d), () => gan(cu));
                    else { gan(d); _daSuaPhu = true; LuuNeuDu(); }
                }
            }
            else _vm.BaoLoi(phaiDuong ? "Tỷ trọng phải là số lớn hơn 0." : "Giá gia công phải là số từ 0 trở lên.");
            Bao(null);
        }

        /// <summary>Lấy lại giá tạm hoặc gợi ý theo bảng giá mới.</summary>
        internal void CapNhat()
        {
            DongBangGia t = Tam;
            if (t != null) Nhap = t;
            else if (Nhap == null || !_daSuaPhu) Nhap = GiaVlDon.GoiY(_vm.BangGoc, VatLieu, DoDay);
            Bao(null);
        }

        internal void BoGiaTam()
        {
            _daSuaPhu = false;
            Nhap = GiaVlDon.GoiY(_vm.BangGoc, VatLieu, DoDay);
            Bao(null);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao(string ten) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }
}
