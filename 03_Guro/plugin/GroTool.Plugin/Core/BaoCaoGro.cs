using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZwCadPlugin.Core
{
    /// <summary>Một dòng tóm tắt có dấu ✓ (đạt) hoặc ⚠ (cần xem) trên tab Thông Tin.</summary>
    public sealed class DongTomTat
    {
        public DongTomTat(string nhan, string giaTri, bool dat)
        {
            Nhan = nhan;
            GiaTri = giaTri;
            Dat = dat;
        }

        public string Nhan { get; }
        public string GiaTri { get; }
        public bool Dat { get; }
    }

    /// <summary>Một nhóm nét còn nằm trên layer gốc (chưa đổi) — dòng cam có nút Zoom.</summary>
    public sealed class NhomNetChuaDoi
    {
        public NhomNetChuaDoi(string linetype, int soLuong)
        {
            Linetype = linetype;
            SoLuong = soLuong;
        }

        public string Linetype { get; }
        public int SoLuong { get; }
        public string Nhan => Linetype + " · chưa đổi";
    }

    public enum LoaiCanhBao { Khac, CoDon, KhongHinh, TrungSoLuong, LoiT, LoiQ, LoiCsv, MatTam, ConSot, GhiChuMoi, XuLySht }

    /// <summary>Một khối cảnh báo / lỗi của báo cáo (tiêu đề + các dòng chi tiết, đã dịch).</summary>
    public sealed class KhoiCanhBao
    {
        public string TieuDe { get; set; }
        public List<string> ChiTiet { get; } = new List<string>();
        public LoaiCanhBao Loai { get; set; }
        /// <summary>true = lỗi phải xử lý (đỏ); false = cảnh báo (cam).</summary>
        public bool LaLoi { get; set; }
        /// <summary>Khối nói về tấm cụ thể -> có nút mở danh sách tấm lỗi.</summary>
        public bool CoNutDanhSach => Loai == LoaiCanhBao.CoDon || Loai == LoaiCanhBao.KhongHinh || Loai == LoaiCanhBao.XuLySht;
        /// <summary>Mục 134: handle các đối tượng cụ thể của khối (vd đối tượng còn sót khi xếp cụm).</summary>
        public List<string> Handles { get; } = new List<string>();
        /// <summary>Có handle -> nút "Chọn &amp; Zoom" tới các đối tượng đó.</summary>
        public bool CoNutChonZoom => Handles.Count > 0;
        /// <summary>Mục 160: khối "ghi chú tiếng Nhật mới" -> nút mở Từ điển ghi chú.</summary>
        public bool CoNutTuDien => Loai == LoaiCanhBao.GhiChuMoi;
        public string ChiTietGop => string.Join(Environment.NewLine, ChiTiet);
        public bool CoChiTiet => ChiTiet.Count > 0;
    }

    /// <summary>Kết quả xử lý nét của lệnh GRO (khối "--- THONG BAO XU LY NET").</summary>
    public sealed class KetQuaXuLyNet
    {
        public string Layer { get; set; }
        public int Kakou { get; set; }
        public int Kegaki { get; set; }
        public int Sunpou { get; set; }
        public int ConLai { get; set; }
        public bool DaDoiHet { get; set; }
        /// <summary>Layer gốc không có đối tượng nào trong các khung bao.</summary>
        public bool KhongTimThay { get; set; }
        public List<NhomNetChuaDoi> Nhom { get; } = new List<NhomNetChuaDoi>();
        public int DaDoi => Kakou + Kegaki + Sunpou;
    }

    /// <summary>
    /// Đọc báo cáo cuối lệnh GRO (chuỗi ASCII lisp ghép — mục 105) thành dữ liệu có cấu trúc
    /// cho tab Thông Tin dạng bảng điều khiển (mục 108). Lisp vẫn gửi CHỮ (dùng chung cho
    /// dòng lệnh và alert dự phòng); ở đây chỉ nhận lại đúng các mẫu câu lisp đã ghi.
    /// Không nhận ra dòng tổng "N ghi chu -> M tam" -> DocDuoc = false, tab hiện dạng dòng chữ.
    /// </summary>
    public sealed class BaoCaoGro
    {
        public int? SoGhiChu { get; private set; }
        public int? SoTam { get; private set; }
        public int? SoBango { get; private set; }
        public string KieuChu { get; private set; }
        public int LoiKieuChu { get; private set; }
        public int? SoMaKhac { get; private set; }
        public string MaKhacDau { get; private set; }
        public int? SoFname { get; private set; }
        public int? SoDongCsv { get; private set; }
        public string DuongCsv { get; private set; }
        public bool CsvLoi { get; private set; }
        public int CsvCu { get; private set; }
        public int? DanhSoDen { get; private set; }
        public string PdfLayer { get; private set; }
        public string DuongPdf { get; private set; }
        public int? SoCumXepPhai { get; private set; }
        public int? SoCumMirror { get; private set; }
        public int? SoCumVat { get; private set; }
        public int? SoCumBeVat { get; private set; }
        public int? SoCumBe { get; private set; }
        public double? CotXepX { get; private set; }
        public bool DaTranhVatCan { get; private set; }
        public int? SoDoiTuongSot { get; private set; }
        public int? SoLuotTachKhung { get; private set; }
        public int? SoTamTachKhung { get; private set; }
        public int? SoCapTachMirror { get; private set; }
        public int? SoCapBoQuaMirror { get; private set; }
        public int? SoMkDat { get; private set; }
        public int? SoMkTong { get; private set; }
        public int SoMkThuNho { get; private set; }
        public int SoMkDeKegaki { get; private set; }
        public int SoMkChuaDat { get; private set; }
        public KetQuaXuLyNet Net { get; private set; }
        public List<KhoiCanhBao> CanhBao { get; } = new List<KhoiCanhBao>();

        public bool DocDuoc => SoGhiChu.HasValue && SoTam.HasValue;

        private static Regex R(string mau) => new Regex(mau, RegexOptions.CultureInvariant);

        private static readonly Regex RTong = R(@"^(\d+) ghi chu -> (\d+) tam:$");
        private static readonly Regex RBango = R(@"^(\d+) bango \(layer 0\), kieu chu (.*?)(?: \*\*\* (\d+) CAI DAT KHONG DUOC \*\*\*)?$");
        private static readonly Regex RMaKhac = R(@"^(\d+) ma khac (.*) \(layer 3-KEGAKI\)$");
        private static readonly Regex RFname = R(@"^(\d+) F-name \(layer 2-F-NAME\)$");
        private static readonly Regex RCsv = R(@"^(\d+) dong CSV \(GHI DE\) -> (.*)$");
        private static readonly Regex RCsvCu = R(@"^\(da xoa (\d+) dong cu trong file CSV\)$");
        private static readonly Regex RDanhSo = R(@"^Da danh so tu 1 den (\d+)$");
        private static readonly Regex RPdf = R(@"^\[x\] Tu dong in nhanh PDF theo layer: (.*)$");
        private static readonly Regex RFilePdf = R(@"^File PDF: (.*)$");
        private static readonly Regex RXepCum = R(@"^Xep cum sang phai:\s*(\d+)\s*cum\s*\(mirror\s*(\d+),\s*vat\s*(\d+),\s*be\+vat\s*(\d+),\s*be\s*(\d+)\)\s*-\s*cot\s*X=([0-9\.\-]+)(?:\s*\((da tranh vat can)\))?$");
        private static readonly Regex RSotCum = R(@"^!!!\s*(\d+)\s*doi tuong con sot tai vi tri cum cu");
        private static readonly Regex RSotHandle = R(@"^Handle con sot:\s*(.*)$");
        private static readonly Regex RHandle = R(@"^[0-9A-Fa-f]{1,16}$");
        private static readonly Regex RTachMirror = R(@"^Tach cap mirror:\s*(\d+)\s*cap\s*->\s*(\d+)\s*tam\s*\(ban nghich dat ben phai, khe ([0-9\.]+)mm\)$");
        private static readonly Regex RBoQuaMirror = R(@"^!!!\s*Bo qua tach mirror\s*(\d+)\s*cap:\s*(.*)$");
        private static readonly Regex RDatMk = R(@"^Dat ma khac vao hinh: (\d+)/(\d+) tam \(thu nho 1/2: (\d+), de KEGAKI: (\d+)\)$");
        private static readonly Regex RMkDeKegaki = R(@"^!!! Ma khac de KEGAKI \((\d+) tam\): bango (.*)$");
        private static readonly Regex RMkChuaDat = R(@"^!!! Chua dat duoc ma khac vao hinh \((\d+) tam\) - move thu cong: bango (.*)$");
        private static readonly Regex RTachKhung = R(@"^Tach va cham khung in:\s*da doi\s*(\d+)\s*luot\s*cho\s*(\d+)\s*tam don\.$");
        private static readonly Regex RCanhBaoSht = R(@"^(\*\*\*|!!!)\s*CANH BAO:\s*Co\s*(\d+)\s*tam\s*can xu ly doi net sang 4-SHT.*?-\s*bango:\s*(.*)$");
        private static readonly Regex RNet = R(@"^--- THONG BAO XU LY NET \(Layer ""(.*)""\) ---$");
        private static readonly Regex RNetKhong = R(@"^\[XU LY NET\]: Khong tim thay doi tuong nao tren layer goc ""(.*)""");
        private static readonly Regex RKakou = R(@"^\+ (\d+) net Continuous -> 0-KAKOU$");
        private static readonly Regex RKegaki = R(@"^\+ (\d+) net Center1 -> 3-KEGAKI$");
        private static readonly Regex RSunpou = R(@"^\+ (\d+) text -> 1-SUNPOU$");
        private static readonly Regex RDoiHet = R(@"^-> DA DOI HET!");
        private static readonly Regex RConLai = R(@"^-> CON LAI (\d+) DOI TUONG");
        private static readonly Regex RNhom = R(@"^\* Net (.*): (\d+) doi tuong$");
        private static readonly Regex RGoiYNet = R(@"^\((Xem tab Net Chua Doi|Bam OK se tu dong mo)");

        private static int So(Group g) => int.Parse(g.Value, CultureInfo.InvariantCulture);

        public static BaoCaoGro Doc(string baoCao)
        {
            var kq = new BaoCaoGro();
            if (string.IsNullOrEmpty(baoCao)) return kq;

            KhoiCanhBao khoi = null;
            foreach (string dongGoc in baoCao.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string s = dongGoc.Trim();
                // Dòng trống KHÔNG đóng khối: khối lỗi CSV có dòng trống ở giữa. Khối chỉ đóng
                // khi gặp khối mới hoặc một dòng tóm tắt nhận ra được.
                if (s.Length == 0) continue;

                if (kq.DocDongTomTat(s))
                {
                    khoi = null;
                    continue;
                }

                if (LaDauKhoi(s))
                {
                    khoi = new KhoiCanhBao
                    {
                        TieuDe = ThongTinGro.Dich(s),
                        Loai = LoaiCua(s),
                        LaLoi = ThongTinGro.PhanLoai(s) == LoaiDongThongTin.Loi,
                    };
                    kq.CanhBao.Add(khoi);
                    continue;
                }

                if (khoi != null) khoi.ChiTiet.Add(ThongTinGro.Dich(s));
                // Dòng lạ ngoài mọi khối: bỏ qua (vẫn xem được nguyên văn qua nút Sao chép).
            }
            return kq;
        }

        /// <summary>Nhận một dòng tóm tắt (số liệu). true nếu đã nhận.</summary>
        private bool DocDongTomTat(string s)
        {
            Match m;
            if ((m = RTong.Match(s)).Success) { SoGhiChu = So(m.Groups[1]); SoTam = So(m.Groups[2]); return true; }
            if ((m = RBango.Match(s)).Success)
            {
                SoBango = So(m.Groups[1]);
                KieuChu = m.Groups[2].Value.Trim();
                LoiKieuChu = m.Groups[3].Success ? So(m.Groups[3]) : 0;
                return true;
            }
            if ((m = RMaKhac.Match(s)).Success) { SoMaKhac = So(m.Groups[1]); MaKhacDau = m.Groups[2].Value.Trim(); return true; }
            if ((m = RFname.Match(s)).Success) { SoFname = So(m.Groups[1]); return true; }
            if ((m = RCsv.Match(s)).Success)
            {
                SoDongCsv = So(m.Groups[1]);
                string duong = m.Groups[2].Value.Trim();
                if (duong.Contains("GHI THAT BAI")) CsvLoi = true;
                else DuongCsv = duong;
                return true;
            }
            if ((m = RCsvCu.Match(s)).Success) { CsvCu = So(m.Groups[1]); return true; }
            if ((m = RDanhSo.Match(s)).Success) { DanhSoDen = So(m.Groups[1]); return true; }
            if ((m = RXepCum.Match(s)).Success)
            {
                SoCumXepPhai = So(m.Groups[1]);
                SoCumMirror = So(m.Groups[2]);
                SoCumVat = So(m.Groups[3]);
                SoCumBeVat = So(m.Groups[4]);
                SoCumBe = So(m.Groups[5]);
                if (double.TryParse(m.Groups[6].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                    CotXepX = x;
                DaTranhVatCan = m.Groups[7].Success;
                return true;
            }
            if ((m = RSotCum.Match(s)).Success)
            {
                SoDoiTuongSot = So(m.Groups[1]);
                CanhBao.Add(new KhoiCanhBao
                {
                    TieuDe = ThongTinGro.Dich(s),
                    Loai = LoaiCanhBao.ConSot,
                    LaLoi = false,
                });
                return true;
            }
            // Mục 134: danh sách handle đối tượng còn sót -> gắn vào khối "còn sót" vừa tạo
            if ((m = RSotHandle.Match(s)).Success)
            {
                var khoiSot = CanhBao.LastOrDefault(k => k.Loai == LoaiCanhBao.ConSot);
                if (khoiSot != null)
                    foreach (string h in m.Groups[1].Value.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        if (RHandle.IsMatch(h) && !khoiSot.Handles.Contains(h, StringComparer.OrdinalIgnoreCase))
                            khoiSot.Handles.Add(h.ToUpperInvariant());
                return true;
            }
            if ((m = RTachMirror.Match(s)).Success) { SoCapTachMirror = So(m.Groups[1]); return true; }
            if ((m = RBoQuaMirror.Match(s)).Success)
            {
                SoCapBoQuaMirror = So(m.Groups[1]);
                CanhBao.Add(new KhoiCanhBao
                {
                    TieuDe = ThongTinGro.Dich(s),
                    Loai = LoaiCanhBao.Khac,
                    LaLoi = false,
                });
                return true;
            }
            if ((m = RDatMk.Match(s)).Success)
            {
                SoMkDat = So(m.Groups[1]);
                SoMkTong = So(m.Groups[2]);
                SoMkThuNho = So(m.Groups[3]);
                SoMkDeKegaki = So(m.Groups[4]);
                return true;
            }
            // Mục 117: đè ケガキ = cảnh báo cam; chưa đặt được = LỖI đỏ (người dùng phải move tay)
            if ((m = RMkDeKegaki.Match(s)).Success || (m = RMkChuaDat.Match(s)).Success)
            {
                bool chua = m.Groups[0].Value.Contains("move thu cong");
                if (chua) SoMkChuaDat = So(m.Groups[1]);
                var k = new KhoiCanhBao { TieuDe = ThongTinGro.Dich(s), Loai = LoaiCanhBao.Khac, LaLoi = chua };
                CanhBao.Add(k);
                return true;
            }
            if ((m = RTachKhung.Match(s)).Success)
            {
                SoLuotTachKhung = So(m.Groups[1]);
                SoTamTachKhung = So(m.Groups[2]);
                return true;
            }
            if ((m = RCanhBaoSht.Match(s)).Success)
            {
                var k = new KhoiCanhBao
                {
                    TieuDe = ThongTinGro.Dich(s),
                    Loai = LoaiCanhBao.XuLySht,
                    LaLoi = false,
                };
                if (m.Groups[3].Success && !string.IsNullOrWhiteSpace(m.Groups[3].Value))
                {
                    k.ChiTiet.Add("Bango: " + m.Groups[3].Value.Trim());
                }
                CanhBao.Add(k);
                return true;
            }
            if ((m = RPdf.Match(s)).Success) { PdfLayer = m.Groups[1].Value.Trim(); return true; }
            if ((m = RFilePdf.Match(s)).Success) { DuongPdf = m.Groups[1].Value.Trim(); return true; }
            if ((m = RNet.Match(s)).Success) { Net = new KetQuaXuLyNet { Layer = m.Groups[1].Value }; return true; }
            if ((m = RNetKhong.Match(s)).Success)
            {
                Net = new KetQuaXuLyNet { Layer = m.Groups[1].Value, KhongTimThay = true };
                return true;
            }
            if (Net != null)
            {
                if ((m = RKakou.Match(s)).Success) { Net.Kakou = So(m.Groups[1]); return true; }
                if ((m = RKegaki.Match(s)).Success) { Net.Kegaki = So(m.Groups[1]); return true; }
                if ((m = RSunpou.Match(s)).Success) { Net.Sunpou = So(m.Groups[1]); return true; }
                if (RDoiHet.IsMatch(s)) { Net.DaDoiHet = true; return true; }
                if ((m = RConLai.Match(s)).Success) { Net.ConLai = So(m.Groups[1]); return true; }
                if ((m = RNhom.Match(s)).Success)
                {
                    Net.Nhom.Add(new NhomNetChuaDoi(m.Groups[1].Value.Trim(), So(m.Groups[2])));
                    return true;
                }
                if (RGoiYNet.IsMatch(s)) return true;
            }
            return false;
        }

        private static bool LaDauKhoi(string s)
        {
            return s.StartsWith("!!!") || s.StartsWith("***")
                || s == "Error T!!!" || s == "Error Q!!!"
                || s == "KHONG GHI DUOC FILE CSV"
                || s.StartsWith("Co canh bao - xem dong lenh");
        }

        private static LoaiCanhBao LoaiCua(string s)
        {
            string h = s.ToUpperInvariant();
            if (h.Contains("HINH CO DON")) return LoaiCanhBao.CoDon;
            if (h.Contains("KHONG TIM THAY HINH VE CAT")) return LoaiCanhBao.KhongHinh;
            if (h.Contains("TRUNG LOAI SO LUONG")) return LoaiCanhBao.TrungSoLuong;
            if (h.Contains("4-SHT") || h.Contains("XU LY SHT")) return LoaiCanhBao.XuLySht;
            if (h.StartsWith("ERROR T")) return LoaiCanhBao.LoiT;
            if (h.StartsWith("ERROR Q")) return LoaiCanhBao.LoiQ;
            if (h.Contains("KHONG GHI DUOC FILE CSV")) return LoaiCanhBao.LoiCsv;
            if (h.Contains("SO TAM NHO HON SO GHI CHU")) return LoaiCanhBao.MatTam;
            return LoaiCanhBao.Khac;
        }


        // ---------------- Dữ liệu dựng sẵn cho giao diện ----------------

        /// <summary>Nhóm "Ghi chú trên tấm": bango, mã khắc, F-name, đánh số — đạt khi khớp số tấm.</summary>
        public List<DongTomTat> DongGhiChu()
        {
            var ds = new List<DongTomTat>();
            int tam = SoTam ?? -1;
            if (SoBango.HasValue)
                ds.Add(new DongTomTat(
                    "Bango · layer 0 · " + (string.IsNullOrEmpty(KieuChu) ? "?" : KieuChu)
                    + (LoiKieuChu > 0 ? " · " + LoiKieuChu + " lỗi kiểu chữ" : ""),
                    SoBango.Value.ToString(CultureInfo.InvariantCulture),
                    SoBango.Value == tam && LoiKieuChu == 0));
            if (SoMaKhac.HasValue)
                ds.Add(new DongTomTat("Mã khắc " + MaKhacDau + " · 3-KEGAKI",
                                      SoMaKhac.Value.ToString(CultureInfo.InvariantCulture), SoMaKhac.Value == tam));
            if (SoFname.HasValue)
                ds.Add(new DongTomTat("F-name · 2-F-NAME",
                                      SoFname.Value.ToString(CultureInfo.InvariantCulture), SoFname.Value == tam));
            if (DanhSoDen.HasValue)
                ds.Add(new DongTomTat("Đánh số", "1 → " + DanhSoDen.Value.ToString(CultureInfo.InvariantCulture),
                                      DanhSoDen.Value == tam));
            if (SoCumXepPhai.HasValue)
            {
                string info = $"{SoCumXepPhai.Value} cụm (mirror {SoCumMirror}, vát {SoCumVat}, bẻ+vát {SoCumBeVat}, bẻ {SoCumBe}) · Cột X={CotXepX}"
                            + (DaTranhVatCan ? " (đã né)" : "");
                ds.Add(new DongTomTat("Sắp xếp cụm", info, !SoDoiTuongSot.HasValue || SoDoiTuongSot.Value == 0));
            }
            if (SoCapTachMirror.HasValue || SoCapBoQuaMirror.HasValue)
            {
                int n = SoCapTachMirror ?? 0;
                string info = $"{n} cặp → {2 * n} tấm"
                            + (SoCapBoQuaMirror.HasValue ? $" · bỏ qua {SoCapBoQuaMirror.Value}" : "");
                ds.Add(new DongTomTat("Tách cặp mirror", info, !SoCapBoQuaMirror.HasValue));
            }
            if (SoMkTong.HasValue && SoMkTong.Value > 0)
            {
                string info = $"{SoMkDat}/{SoMkTong} tấm"
                            + (SoMkThuNho > 0 ? $" · thu nhỏ {SoMkThuNho}" : "")
                            + (SoMkDeKegaki > 0 ? $" · đè ケガキ {SoMkDeKegaki}" : "");
                ds.Add(new DongTomTat("Mã khắc vào hình", info, SoMkDat == SoMkTong && SoMkDeKegaki == 0));
            }
            if (SoLuotTachKhung.HasValue && SoLuotTachKhung.Value > 0)
            {
                string info = $"{SoLuotTachKhung.Value} lượt ({SoTamTachKhung} tấm)";
                ds.Add(new DongTomTat("Tách va chạm khung in", info, true));
            }
            return ds;
        }

        /// <summary>Ba dòng đổi layer (đều là "đạt"); nhóm chưa đổi đi riêng ở Net.Nhom.</summary>
        public List<DongTomTat> DongNetDaDoi()
        {
            var ds = new List<DongTomTat>();
            if (Net == null || Net.KhongTimThay) return ds;
            ds.Add(new DongTomTat("Continuous → 0-KAKOU", Net.Kakou.ToString(CultureInfo.InvariantCulture), true));
            ds.Add(new DongTomTat("Center1 → 3-KEGAKI", Net.Kegaki.ToString(CultureInfo.InvariantCulture), true));
            ds.Add(new DongTomTat("Text → 1-SUNPOU", Net.Sunpou.ToString(CultureInfo.InvariantCulture), true));
            return ds;
        }

        public string NhanCsv => CsvLoi ? "CSV — GHI THẤT BẠI"
                               : "CSV ghi đè" + (CsvCu > 0 ? " (xoá " + CsvCu + " dòng cũ)" : "");

        /// <summary>"…\OneDrive - OFT\Desktop\test.csv": chỉ giữ 3 đoạn cuối của đường dẫn.</summary>
        /// <summary>
        /// Gộp các nhóm của tab Nét Chưa Đổi (tách theo linetype + màu) thành nhóm theo linetype
        /// cho tab Thông Tin — mục 113. Nhóm 0 đối tượng bị bỏ; nhiều nét nhất đứng trước.
        /// </summary>
        public static List<NhomNetChuaDoi> GopNhomNet(IEnumerable<KeyValuePair<string, int>> nhom)
        {
            if (nhom == null) return new List<NhomNetChuaDoi>();
            return nhom.Where(n => n.Value > 0)
                       .GroupBy(n => n.Key ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                       .Select(g => new NhomNetChuaDoi(g.First().Key ?? string.Empty, g.Sum(n => n.Value)))
                       .OrderByDescending(n => n.SoLuong)
                       .ThenBy(n => n.Linetype, StringComparer.OrdinalIgnoreCase)
                       .ToList();
        }

        /// <summary>
        /// Khối cảnh báo còn hiện khi đã có số liệu SỐNG của tab Danh Sách Tấm: bỏ khối hình cô đơn /
        /// không thấy hình cắt của báo cáo (đã chụp lúc chạy GRO) vì dòng sống thay thế — mục 113.
        /// </summary>
        public List<KhoiCanhBao> CanhBaoKhiCoTamSong() => CanhBao.Where(k => !k.CoNutDanhSach).ToList();

        /// <summary>
        /// Mục 160: khối cảnh báo SỐNG "ghi chú tiếng Nhật mới" — gom các đoạn tiếng Nhật còn sót
        /// trong ghi chú ĐÃ DỊCH của mọi tấm (chưa có trong từ điển), mỗi đoạn kèm bango. null nếu không có.
        /// </summary>
        public static KhoiCanhBao KhoiGhiChuMoi(IEnumerable<KeyValuePair<string, string>> bangoVaGhiChu)
        {
            var bango = new Dictionary<string, List<string>>();
            var thuTu = new List<string>();
            if (bangoVaGhiChu != null)
                foreach (var kv in bangoVaGhiChu)
                    foreach (string d in JapanNoteTranslator.DoanChuaDich(kv.Value))
                    {
                        if (!bango.ContainsKey(d)) { bango[d] = new List<string>(); thuTu.Add(d); }
                        if (!string.IsNullOrEmpty(kv.Key) && !bango[d].Contains(kv.Key)) bango[d].Add(kv.Key);
                    }
            if (thuTu.Count == 0) return null;
            var khoi = new KhoiCanhBao
            {
                TieuDe = "Có " + thuTu.Count + " ghi chú tiếng Nhật mới (chưa có trong từ điển) — cần lưu ý",
                Loai = LoaiCanhBao.GhiChuMoi,
                LaLoi = false,
            };
            foreach (string d in thuTu)
            {
                var ds = bango[d];
                khoi.ChiTiet.Add(d + (ds.Count > 0 ? " — bango " + string.Join(", ", ds.Take(15)) + (ds.Count > 15 ? ", …" : "") : ""));
            }
            return khoi;
        }

        /// <summary>
        /// Khối cảnh báo SỐNG cho các tấm cần xử lý đổi nét sang 4-SHT (ghi chú có "xử lý SHT").
        /// </summary>
        public static KhoiCanhBao KhoiCanhBaoSht(IEnumerable<KeyValuePair<string, string>> bangoVaGhiChu)
        {
            if (bangoVaGhiChu == null) return null;
            var shtBango = new List<string>();
            foreach (var kv in bangoVaGhiChu)
            {
                if (JapanNoteTranslator.LaXuLySht(kv.Value))
                {
                    if (!string.IsNullOrEmpty(kv.Key) && !shtBango.Contains(kv.Key))
                        shtBango.Add(kv.Key);
                }
            }
            if (shtBango.Count == 0) return null;

            var khoi = new KhoiCanhBao
            {
                TieuDe = "Có " + shtBango.Count + " tấm cần xử lý đổi nét sang 4-SHT (ghi chú \"xử lý SHT\")",
                Loai = LoaiCanhBao.XuLySht,
                LaLoi = false,
            };
            khoi.ChiTiet.Add("Bango: " + string.Join(", ", shtBango.Take(15)) + (shtBango.Count > 15 ? ", …" : ""));
            return khoi;
        }

        public static string RutGonDuong(string duong, int soDoan = 3)
        {
            if (string.IsNullOrEmpty(duong)) return string.Empty;
            string[] p = duong.Split('\\', '/');
            if (p.Length <= soDoan) return duong;
            return "…\\" + string.Join("\\", p.Skip(p.Length - soDoan));
        }
    }
}
