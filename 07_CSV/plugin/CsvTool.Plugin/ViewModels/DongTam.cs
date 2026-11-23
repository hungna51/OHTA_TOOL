using System.Collections.ObjectModel;
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
        public string Ten => _ten ?? Dong.N;
        private readonly string _ten;
        public string NoiDung { get; }
        public bool LaLoi { get; }
        public int SoViTri => Goc?.ViTri.Count ?? 0;
        private int _dangXem = -1;

        /// <summary>Cảnh báo thiếu giá: dòng có nút Nhập giá sang tab Giá VL.</summary>
        public bool LaThieuGia { get; }

        public DongLoi(DongTam dong, string noiDung, bool laLoi, LoiQuet goc, bool laThieuGia = false, string ten = null)
        {
            Dong = dong; NoiDung = noiDung; LaLoi = laLoi; Goc = goc; LaThieuGia = laThieuGia; _ten = ten;
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

    /// <summary>Một loại đường bẻ trên thẻ tấm. Đủ số đường và chiều dài mới được lưu.</summary>
    public sealed class DongBe : INotifyPropertyChanged
    {
        private readonly DongTam _tam;
        private string _so, _dai;

        public DongBe(DongTam tam, string so, string dai) { _tam = tam; _so = so; _dai = dai; }

        public bool SuaDuoc => true;
        public string So { get => _so; set => Sua(value, ref _so, nameof(So), "Số đường"); }
        public string Dai { get => _dai; set => Sua(value, ref _dai, nameof(Dai), "Chiều dài"); }
        internal bool Du => int.TryParse(_so, out int a) && a > 0 && int.TryParse(_dai, out int b) && b > 0;
        internal (int Dai, int SoLuong) GiaTri => (int.Parse(_dai), int.Parse(_so));

        private void Sua(string giaTri, ref string o, string ten, string nhan)
        {
            string v = (giaTri ?? "").Trim();
            if (v != o)
            {
                if (int.TryParse(v, out int n) && n > 0) { o = n.ToString(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten)); _tam.LuuBe(); return; }
                _tam.BaoLoiBe(nhan + " phải là số nguyên lớn hơn 0.");
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
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
            DungBe();
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

        public string X => Tam?.X;
        public string Y => Tam?.Y;
        public string LoNho => Tam?.Tt.LoNho.ToString();
        public string LoLon => Tam?.Tt.LoLon.ToString();

        // ── Đường bẻ: mỗi loại 1 dòng, tối đa 8 loại vì data CSV có 8 cặp cột ──
        private const int ToiDaBe = 8;
        public ObservableCollection<DongBe> Be { get; } = new ObservableCollection<DongBe>();
        public bool BeSuaTay => Tam != null && _vm.XemGia(Tam.N)?.SuaBe == true;

        private void DungBe()
        {
            Be.Clear();
            if (Tam == null) return;
            foreach (var b in Tam.Tt.DuongBe) Be.Add(new DongBe(this, b.SoLuong.ToString(), b.Dai.ToString()));
        }

        internal void BaoLoiBe(string s) => _vm.BaoLoi(s);

        public void ThemBe()
        {
            if (Be.Count >= ToiDaBe) { _vm.BaoLoi("Tối đa " + ToiDaBe + " loại đường bẻ."); return; }
            Be.Add(new DongBe(this, "1", ""));
        }

        public void XoaBe(DongBe b)
        {
            Be.Remove(b);
            LuuBe();
        }

        internal void LuuBe()
        {
            if (Tam == null) return;
            var moi = Be.Where(x => x.Du).Select(x => x.GiaTri).ToList();
            var cu = Tam.Tt.DuongBe;
            if (moi.SequenceEqual(cu)) return;
            if (BiKhoa()) { DungBe(); return; }
            GiaTam g = _vm.GiaCua(Tam.N);
            if (g == null) { _vm.BaoLoi("Chưa đọc được dữ liệu giá của đơn, quét lại."); DungBe(); return; }
            bool suaCu = g.SuaBe;
            Luu(() => { Tam.Tt.DuongBe = moi; g.SuaBe = true; }, () => { Tam.Tt.DuongBe = cu; g.SuaBe = suaCu; });
            if (Tam.Tt.DuongBe != moi) DungBe();
            Bao(nameof(BeSuaTay));
        }

        /// <summary>Bỏ dấu sửa tay: lần quét tấm này tiếp theo lấy lại đường bẻ từ bản vẽ.</summary>
        public void VeBeBanVe()
        {
            GiaTam g = Tam == null ? null : _vm.XemGia(Tam.N);
            if (g == null) return;
            g.SuaBe = false;
            _vm.LuuGia();
            _vm.BaoLoi("Quét lại tấm này để lấy đường bẻ từ bản vẽ.");
            Bao(nameof(BeSuaTay));
        }

        // ── Tiền vật liệu 1 tấm: hiện số đang tính, gõ số khác thì giá = số đó + cắt + gia công ──
        public bool LaVlTay => Tam != null && !string.IsNullOrWhiteSpace(Tam.Tt.TienVatLieu) && CheDoChon != 1;
        public bool ChoSuaTienVl => Tam != null && CheDoChon != 1;
        /// <summary>Dòng nhắc dưới thẻ: giá của tấm này có số do người dùng tự nhập.</summary>
        public string GhiChuGia => LaGiaTay ? (LaVlTay ? "Giá tấm sửa tay. Giá VL nhập tay không được tính." : "Giá tấm sửa tay.")
            : LaVlTay ? "Giá VL nhập tay." : null;
        public bool CoGhiChuGia => GhiChuGia != null;
        public string TienVl
        {
            get
            {
                if (!ChoSuaTienVl) return "";
                if (LaVlTay) return Tam.Tt.TienVatLieu;
                double h = _vm.XemGia(Tam.N)?.TienVlHienTai ?? 0;
                return h > 0 ? h.ToString("0", System.Globalization.CultureInfo.InvariantCulture) : "";
            }
            set
            {
                if (!ChoSuaTienVl) return;
                string v = (value ?? "").Trim();
                if (v == TienVl) return;   // Tab qua ô không được biến số đang tính thành số nhập tay
                double d = 0;
                if (v != "" && (!TinhGia.SoTien(v, out d) || d < 0)) { _vm.BaoLoi("Giá VL phải là số từ 0 trở lên."); Bao(null); return; }
                string moi = v == "" ? "" : d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), cu = Tam.Tt.TienVatLieu ?? "";
                if (moi == cu) { Bao(null); return; }
                if (BiKhoa()) return;
                GiaTam g = _vm.GiaCua(Tam.N);
                if (g == null) { _vm.BaoLoi("Chưa đọc được dữ liệu giá của đơn, quét lại."); Bao(null); return; }
                bool tuDongCu = g.TuDong;
                Luu(() => { Tam.Tt.TienVatLieu = moi; g.TuDong = true; }, () => { Tam.Tt.TienVatLieu = cu; g.TuDong = tuDongCu; });
                Bao(null);
            }
        }

        // ── Chế độ tính riêng từng tấm ──
        public string[] DsCheDo => CsvTabViewModel.DsCheDoTinh;
        public int CheDoChon
        {
            get => int.TryParse(Tam?.Tt.CheDoTinh, out int m) && m >= 0 && m <= 2 ? m : 0;
            set
            {
                // ComboBox đẩy -1 khi thẻ bị dựng lại: không được coi là người dùng chọn
                if (Tam == null || value < 0 || value == CheDoChon || BiKhoa()) return;
                GiaTam g = _vm.GiaCua(Tam.N);
                if (g == null) { _vm.BaoLoi("Chưa đọc được dữ liệu giá của đơn, quét lại."); Bao(null); return; }
                string cu = Tam.Tt.CheDoTinh;
                bool rieng = g.CheDoRieng;
                Luu(() => { Tam.Tt.CheDoTinh = value.ToString(); g.CheDoRieng = true; }, () => { Tam.Tt.CheDoTinh = cu; g.CheDoRieng = rieng; });
                Bao(null);
            }
        }

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
        public bool LamTron250
        {
            get => Tam != null && _vm.Tron250Cua(Tam.N);
            set => SuaGia(g => { g.LamTron250 = value; g.TatTron250 = !value && _vm.LamTron250Don; });
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

        public void BaoGia() { Bao(nameof(GiaTien)); Bao(nameof(LaGiaTay)); Bao(nameof(LamTron250)); Bao(nameof(CheDoChon)); Bao(nameof(TienVl)); Bao(nameof(LaVlTay)); Bao(nameof(ChoSuaTienVl)); Bao(nameof(GhiChuGia)); Bao(nameof(CoGhiChuGia)); }

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
