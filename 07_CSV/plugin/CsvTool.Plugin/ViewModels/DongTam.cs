using System.ComponentModel;
using System.Linq;
using CsvTool.Core;

namespace CsvTool.ViewModels
{
    /// <summary>
    /// Một dòng danh sách tấm: bọc TamXuat (đọc được) hoặc chỉ có lỗi. Ô sửa được gọi ngược
    /// CsvTabViewModel.Sua — ghi bản vẽ hỏng thì giá trị cũ hiện lại (PropertyChanged).
    /// </summary>
    public sealed class DongTam : INotifyPropertyChanged
    {
        private readonly CsvTabViewModel _vm;
        public TamXuat Tam { get; }
        public KhungTho Khung { get; }
        private string _loi;

        public DongTam(CsvTabViewModel vm, KhungTho khung, TamXuat tam, string loi)
        {
            _vm = vm; Khung = khung; Tam = tam; _loi = loi;
        }

        public bool CoLoi => _loi != null;
        public bool SuaDuoc => Tam != null;
        public string Loi { get => _loi; set { _loi = value; Bao(nameof(Loi)); Bao(nameof(CoLoi)); } }

        // F-name → SuaFname
        public string N { get => Tam?.N; set => SuaF('N', value, v => Tam.N = v); }
        public string Q { get => Tam?.Q; set => SuaF('Q', value, v => Tam.Q = v); }
        public string M { get => Tam?.M; set => SuaF('M', value, v => Tam.M = v); }
        public string T { get => Tam?.T; set => SuaF('T', value, v => Tam.T = v); }

        public string XxY => Tam == null ? "" : Tam.X + " × " + Tam.Y;
        public string LoNho => Tam?.Tt.LoNho.ToString();
        public string LoLon => Tam?.Tt.LoLon.ToString();
        // số đường × chiều dài, như bảng giấy của xưởng
        public string DuongBe => Tam == null ? "" : string.Join("     ", Tam.Tt.DuongBe.Select(b => b.SoLuong + " × " + b.Dai));
        public bool CoDuongBe => Tam != null && Tam.Tt.DuongBe.Count > 0;

        private bool _moRong;
        public bool MoRong { get => _moRong; set { _moRong = value; Bao(nameof(MoRong)); } }

        public string GiaTien
        {
            get => Tam?.Tt.GiaBaoGia;
            set
            {
                string v = (value ?? "").Trim();
                if (Tam == null || v == Tam.Tt.GiaBaoGia) return;
                if (_vm.SuaThongTin(this, "GIA BAO GIA", v)) Tam.Tt.GiaBaoGia = v;
                Bao(nameof(GiaTien));
            }
        }
        public string MaMage => Tam == null ? "" : CsvXuat.MaMage(Tam.Tt.LoaiGiaCong);

        // MText thông tin tấm → SuaThongTin (nhãn đã bỏ dấu, viết hoa)
        public string Tappu { get => Tam?.Tt.Tappu.ToString(); set => SuaSo("LO TAPPU", value, v => Tam.Tt.Tappu = v); }
        public string Sara { get => Tam?.Tt.Sara.ToString(); set => SuaSo("LO SARA", value, v => Tam.Tt.Sara = v); }
        public string PitchiSoLan { get => Tam?.Tt.PitchiSoLan.ToString(); set => SuaSo("PITCHI - SO LAN", value, v => Tam.Tt.PitchiSoLan = v); }
        public string PitchiDai { get => Tam?.Tt.PitchiDai.ToString(); set => SuaSo("PITCHI - CHIEU DAI (MM)", value, v => Tam.Tt.PitchiDai = v); }
        public string Vat { get => Tam?.Tt.Vat.ToString(); set => SuaSo("CHIEU DAI VAT", value, v => Tam.Tt.Vat = v); }
        public string LoaiGiaCong
        {
            get => Tam?.Tt.LoaiGiaCong;
            set
            {
                if (Tam == null || value == Tam.Tt.LoaiGiaCong) return;
                if (_vm.SuaThongTin(this, "CAC LOAI GIA CONG", (value ?? "").Trim())) Tam.Tt.LoaiGiaCong = (value ?? "").Trim();
                Bao(nameof(LoaiGiaCong)); Bao(nameof(MaMage));
            }
        }

        private void SuaF(char truong, string giaTri, System.Action<string> gan)
        {
            giaTri = (giaTri ?? "").Trim();
            if (Tam == null || giaTri == Truong(truong)) return;
            if (giaTri == "") _vm.BaoLoi("Trường " + truong + "= không được để trống.");
            else if (_vm.SuaFname(this, truong, giaTri)) gan(giaTri);
            Bao(truong.ToString());
        }

        private void SuaSo(string nhan, string giaTri, System.Action<int> gan)
        {
            if (Tam == null) return;
            if (!int.TryParse((giaTri ?? "").Trim(), out int so) || so < 0) _vm.BaoLoi("\"" + giaTri + "\" không phải số nguyên ≥ 0.");
            else if (_vm.SuaThongTin(this, nhan, so.ToString())) gan(so);
            Bao(null);   // trả mọi ô về giá trị đang có
        }

        private string Truong(char t) => t == 'N' ? Tam.N : t == 'Q' ? Tam.Q : t == 'M' ? Tam.M : Tam.T;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao(string ten) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }
}
