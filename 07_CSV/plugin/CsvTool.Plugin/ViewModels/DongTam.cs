using System.ComponentModel;
using System.Linq;
using CsvTool.Core;

namespace CsvTool.ViewModels
{
    /// <summary>Một dòng danh sách lỗi: bấm lần lượt để đi qua từng vị trí lỗi trên bản vẽ.</summary>
    public sealed class DongLoi : INotifyPropertyChanged
    {
        public DongTam Dong { get; }
        public LoiQuet Goc { get; }
        public string Ten => Dong.N;
        public string NoiDung { get; }
        public bool LaLoi { get; }
        public int SoViTri => Goc?.ViTri.Count ?? 0;
        private int _dangXem = -1;

        public DongLoi(DongTam dong, string noiDung, bool laLoi, LoiQuet goc)
        {
            Dong = dong; NoiDung = noiDung; LaLoi = laLoi; Goc = goc;
        }

        public string NhanViTri => SoViTri == 0 ? "Xem khung" : "Vị trí " + (_dangXem < 0 ? 1 : _dangXem + 1) + "/" + SoViTri;

        /// <summary>Vị trí kế tiếp (quay vòng); gọi khi SoViTri &gt; 0.</summary>
        public (double MinX, double MinY, double MaxX, double MaxY) Tiep()
        {
            _dangXem = (_dangXem + 1) % SoViTri;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NhanViTri)));
            return Goc.ViTri[_dangXem];
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

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
        /// <summary>Khung chú thích: hiện để người dùng thấy nhưng không chặn Xuất CSV.</summary>
        public bool BoQua { get; set; }
        /// <summary>Lỗi hình học của lượt quét gần nhất: giữ qua các lần sửa ô, tới khi quét lại khung này.</summary>
        public string LoiQuet { get; set; }
        /// <summary>Lỗi ngoài hình học (F-name, dim, tên trùng, thiếu vát / pitchi) để danh sách lỗi hiện cùng lúc.</summary>
        public string LoiKhac { get; set; }
        public bool SuaDuoc => Tam != null;
        public string Loi { get => _loi; set { _loi = value; Bao(nameof(Loi)); Bao(nameof(CoLoi)); } }

        // F-name → SuaFname
        public string N { get => Tam?.N ?? PhanTichKhung.TenKhung(Khung) ?? "(chưa có tên)"; set => SuaF('N', value, v => Tam.N = v); }
        public string Q { get => Tam?.Q; set => SuaF('Q', value, v => Tam.Q = v); }
        public string M { get => Tam?.M; set => SuaF('M', value, v => Tam.M = v); }
        public string T
        {
            get => Tam?.T;
            set
            {
                if (Tam == null) return;
                string v = PhanTichKhung.ChuanDoDay(value);
                if (v == null) _vm.BaoLoi("\"" + value + "\" không phải độ dày hợp lệ.");
                else SuaF('T', v, x => Tam.T = x);
                Bao(nameof(T));   // gõ 6 khi đang là 6.0 cũng phải hiện lại 6.0
            }
        }

        public string XxY => Tam == null ? "" : Tam.X + " × " + Tam.Y;
        public string LoNho => Tam?.Tt.LoNho.ToString();
        public string LoLon => Tam?.Tt.LoLon.ToString();
        // số đường × chiều dài, như bảng giấy của xưởng
        public string DuongBe => Tam == null ? "" : Tam.Tt.DuongBe.Count == 0 ? "Không có"
            : string.Join("     ", Tam.Tt.DuongBe.Select(b => b.SoLuong + " × " + b.Dai));

        private bool _moRong;
        public bool MoRong { get => _moRong; set { _moRong = value; Bao(nameof(MoRong)); } }

        public string GiaTien
        {
            get => Tam?.Tt.GiaBaoGia;
            set
            {
                if (Tam == null) return;
                string v = (value ?? "").Trim(), cu = Tam.Tt.GiaBaoGia;
                if (v == cu || BiKhoa()) return;
                GiaTam g = _vm.GiaCua(Tam.N);
                bool tuDongCu = g?.TuDong ?? false;
                Luu(() => { Tam.Tt.GiaBaoGia = v; if (g != null) g.TuDong = false; },
                    () => { Tam.Tt.GiaBaoGia = cu; if (g != null) g.TuDong = tuDongCu; });
                Bao(null);
            }
        }

        // ── Tính giá: thông tin riêng lưu ở data\<đơn>-gia.json ──
        /// <summary>Lý do chưa tính được giá; hiện ở danh sách cảnh báo, không chặn Xuất CSV.</summary>
        public string CanhBaoGia { get; set; }
        public bool LaGiaTay => Tam != null && !string.IsNullOrEmpty(Tam.Tt.GiaBaoGia) && _vm.XemGia(Tam.N)?.TuDong != true;
        public string[] DsLoaiPitchi => TinhGia.DsLoaiPitchi;
        public string LoaiPitchi
        {
            get => Tam == null ? null : _vm.XemGia(Tam.N)?.LoaiPitchi ?? "Pitchi";
            // ComboBox đẩy null khi thẻ bị dựng lại: không được coi là người dùng chọn
            set { if (value != null && value != LoaiPitchi) SuaGia(g => g.LoaiPitchi = value); }
        }
        public string[] DsLoaiCuon => TinhGia.DsLoaiCuon;
        public string LoaiCuonGia
        {
            get
            {
                if (Tam == null) return null;
                double d = TinhGia.DonGiaCuonCua(Tam, _vm.XemGia(Tam.N));
                return d == TinhGia.GiaCuonCaHinh ? "Cả hình" : d == TinhGia.GiaCuonNuaHinh ? "Nửa hình" : d > 0 ? null : "Không";
            }
            set
            {
                if (value == null || value == LoaiCuonGia) return;
                double d = value == "Cả hình" ? TinhGia.GiaCuonCaHinh : value == "Nửa hình" ? TinhGia.GiaCuonNuaHinh : -1;
                SuaGia(g => g.DonGiaCuon = d);
            }
        }
        public string DonGiaDatNgoai { get => SoGia(_vm.XemGia(Tam?.N)?.DonGiaDatNgoai); set => SuaSoGia(value, (g, d) => g.DonGiaDatNgoai = d); }
        public bool LamTron250 { get => _vm.XemGia(Tam?.N)?.LamTron250 == true; set => SuaGia(g => g.LamTron250 = value); }

        private static string SoGia(double? d) => d > 0 ? d.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

        private void SuaSoGia(string giaTri, System.Action<GiaTam, double> gan)
        {
            string s = (giaTri ?? "").Trim();
            if (s == "") SuaGia(g => gan(g, 0));
            else if (TinhGia.SoTien(s, out double d) && d >= 0) SuaGia(g => gan(g, d));
            else { _vm.BaoLoi("\"" + giaTri + "\" không phải số ≥ 0."); Bao(null); }
        }

        private void SuaGia(System.Action<GiaTam> doi)
        {
            if (Tam != null && BiKhoa()) return;
            GiaTam g = Tam == null ? null : _vm.GiaCua(Tam.N);
            if (g == null) { _vm.BaoLoi("Chưa đọc được dữ liệu giá của đơn, quét lại."); Bao(null); return; }
            doi(g);
            _vm.TinhLaiNeuCo();
            _vm.DanhGiaLai();
            if (_vm.LuuData()) _vm.SauThayDoi();
            Bao(null);
        }

        public void BaoGia() { Bao(nameof(GiaTien)); Bao(nameof(LaGiaTay)); }

        public string MaMage => Tam == null ? "" : CsvXuat.MaMage(Tam.Tt.LoaiGiaCong);

        // thông tin tấm → data CSV (không còn text trên bản vẽ)
        public string Tappu { get => Tam?.Tt.Tappu.ToString(); set => SuaSo(value, Tam?.Tt.Tappu, v => Tam.Tt.Tappu = v); }
        public string Sara { get => Tam?.Tt.Sara.ToString(); set => SuaSo(value, Tam?.Tt.Sara, v => Tam.Tt.Sara = v); }
        public string PitchiSoLan { get => Tam?.Tt.PitchiSoLan.ToString(); set => SuaSo(value, Tam?.Tt.PitchiSoLan, v => Tam.Tt.PitchiSoLan = v); }
        public string PitchiDai { get => Tam?.Tt.PitchiDai.ToString(); set => SuaSo(value, Tam?.Tt.PitchiDai, v => Tam.Tt.PitchiDai = v); }
        public string Vat { get => Tam?.Tt.Vat.ToString(); set => SuaSo(value, Tam?.Tt.Vat, v => Tam.Tt.Vat = v); }
        public string LoaiGiaCong
        {
            get => Tam?.Tt.LoaiGiaCong;
            set { string cu = Tam?.Tt.LoaiGiaCong; SuaChu(value, cu, v => Tam.Tt.LoaiGiaCong = v); }
        }

        private void SuaF(char truong, string giaTri, System.Action<string> gan)
        {
            giaTri = (giaTri ?? "").Trim();
            if (Tam == null || giaTri == Truong(truong) || (giaTri != "" && BiKhoa())) return;
            if (giaTri == "") _vm.BaoLoi("Trường " + truong + "= không được để trống.");
            else if (_vm.SuaFname(this, truong, giaTri))
            {
                string tenCu = Tam.N;
                gan(giaTri);
                if (truong == 'N') _vm.DoiTenGia(tenCu, giaTri);
                _vm.TinhLaiNeuCo();
                _vm.DanhGiaLai();
                if (_vm.LuuData()) _vm.SauThayDoi();   // hỏng thì đã báo; bản vẽ đúng, CSV đồng bộ lại ở lần quét sau
            }
            Bao(truong.ToString());
        }

        private void SuaSo(string giaTri, int? cu, System.Action<int> gan)
        {
            if (Tam == null) return;
            if (!int.TryParse((giaTri ?? "").Trim(), out int so) || so < 0) _vm.BaoLoi("\"" + giaTri + "\" không phải số nguyên ≥ 0.");
            else if (so != cu && !BiKhoa()) Luu(() => gan(so), () => gan(cu.Value));
            Bao(null);   // trả mọi ô về giá trị đang có
        }

        private void SuaChu(string giaTri, string cu, System.Action<string> gan)
        {
            string v = (giaTri ?? "").Trim();
            if (Tam == null || v == cu || BiKhoa()) return;
            Luu(() => gan(v), () => gan(cu));
            Bao(null);
        }

        /// <summary>Đổi trong bộ nhớ → ghi data CSV; ghi hỏng (Excel đang mở) thì trả giá trị cũ.</summary>
        private void Luu(System.Action doi, System.Action hoan)
        {
            doi();
            _vm.TinhLaiNeuCo();
            if (_vm.LuuData()) { _vm.DanhGiaLai(); _vm.SauThayDoi(); }
            else { hoan(); _vm.TinhLaiNeuCo(); }
        }

        /// <summary>File đang mở: bỏ thay đổi, trả ô về giá trị cũ sau khi binding xong (ComboBox không nhận nếu trả ngay).</summary>
        private bool BiKhoa()
        {
            if (_vm.ChoPhepSua()) return false;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new System.Action(() => Bao(null)));
            return true;
        }

        private string Truong(char t) => t == 'N' ? Tam.N : t == 'Q' ? Tam.Q : t == 'M' ? Tam.M : Tam.T;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Bao(string ten) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }
}
