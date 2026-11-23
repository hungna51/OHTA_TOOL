using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CsvTool.Core
{
    public sealed class DauVaoGia
    {
        public string VatLieu = "", CongTy = "", LoaiPitchi = "Pitchi";
        public double DoDay, X, Y, SoLuong, LoNho, LoLon, Tappu, Sara, PitchiSoLan, PitchiDai, DonGiaCuon, VatMm, DonGiaDatNgoai;
        public List<(double SoDuong, double Dai)> Be = new List<(double, double)>();
        public int CheDoTinh;   // 0 thường, 1 chỉ gia công, 2 vật liệu khách cấp
        public double? TienVLTay;   // tiền vật liệu 1 tấm do người dùng tự tính: giá = số này + cắt + gia công
        public bool LamTron250;
    }

    public sealed class KetQuaGia
    {
        public double KlThucTe, TienVL, TienCat, TongGiaCong, Gia1Tam, AllIn;
    }

    /// <summary>
    /// Công thức của price-calc: lib/calculate.ts + phần form trong app/cong-cu-tinh-tien/page.tsx
    /// (form là chuẩn, luồng nhập file của web bỏ sót nhiều nhánh). Giữ nguyên cả tỉ trọng 7.85 của pitchi / cuộn.
    /// </summary>
    public static class TinhGia
    {
        public static readonly string[] DsLoaiPitchi = { "Pitchi", "Non", "NonLech", "Kakumaru", "KakumaruLech" };
        public static readonly string[] DsLoaiCuon = { "Không", "Nửa hình", "Cả hình" };
        public const double GiaCuonNuaHinh = 6000, GiaCuonCaHinh = 12000;   // 2 mức cố định trong form của web

        /// <summary>
        /// Đơn giá cuộn của tấm: đã chọn trên thẻ thì theo đó (âm = chọn Không), chưa chọn thì suy từ
        /// loại gia công: r1 (cung tròn) = nửa hình, r2 (ống tròn) = cả hình.
        /// </summary>
        public static double DonGiaCuonCua(TamXuat t, GiaTam g)
        {
            double chon = g?.DonGiaCuon ?? 0;
            if (chon != 0) return Math.Max(chon, 0);
            string loai = CsvXuat.LoaiCuon(t.Tt.LoaiGiaCong);
            return loai == "2" ? GiaCuonCaHinh : loai == "1" ? GiaCuonNuaHinh : 0;
        }

        // Math.round của JS: .5 luôn làm tròn lên
        private static double TronJs(double x) => Math.Floor(x + 0.5);
        private static double Tron10(double x) => TronJs(x / 10) * 10;

        // "a || b" của JS: thiếu hoặc bằng 0 thì lấy mặc định
        private static double Hs(List<double> ds, int i, double macDinh) => i < ds.Count && ds[i] != 0 ? ds[i] : macDinh;
        private static double Hs(Dictionary<string, double> ds, string k, double macDinh) => ds.TryGetValue(k, out double v) && v != 0 ? v : macDinh;

        private static double HeSoKL(double kg) => kg < 40 ? 1 : kg < 100 ? 1.5 : 2;

        private static double HeSoCD(double daiMm)
        {
            double m = daiMm / 1000;
            return m < 3 ? 1 : m < 4 ? 2.5 : m < 5 ? 3 : m < 6 ? 3.5 : 4.5;
        }

        private static double TienPitchi(DauVaoGia v, BangGia b)
        {
            if (v.PitchiSoLan <= 0) return 0;
            double kl = (v.X + 10) * (v.Y + 10) * v.DoDay * 7.85 / 1_000_000;
            var t = b.PitchiTrongLuong;
            var c = b.PitchiChieuDai;
            double hsTL = kl < 20 ? Hs(t, 0, 0.8) : kl < 40 ? Hs(t, 1, 1) : kl < 100 ? Hs(t, 2, 1.5) : Hs(t, 3, 2);
            double m = v.PitchiDai / 1000;
            double hsCD = m < 0.83 ? Hs(c, 0, 0.75) : m < 1.66 ? Hs(c, 1, 1) : m < 2.49 ? Hs(c, 2, 1.25) : m < 3.32 ? Hs(c, 3, 1.5)
                        : m < 4.15 ? Hs(c, 4, 1.75) : m < 4.98 ? Hs(c, 5, 2) : m < 5.81 ? Hs(c, 6, 2.25) : Hs(c, 7, 2.5);
            double heGC = Hs(b.HeSoGiaCong, string.IsNullOrEmpty(v.LoaiPitchi) ? "Pitchi" : v.LoaiPitchi, 1);
            double phut = Math.Max(hsTL, hsCD) * heGC * 2 * v.PitchiSoLan;
            return TronJs(v.SoLuong <= 5 ? 1500 / v.SoLuong + 100 * phut : 1500.0 / 5 + 100 * phut);
        }

        private static double TienCuon(DauVaoGia v, BangGia b)
        {
            if (v.DonGiaCuon <= 0) return 0;
            double kl = (v.X + 10) * (v.Y + 10) * v.DoDay * 7.85 / 1_000_000;
            var r = b.HeSoCuon.OrderBy(x => x).ToList();
            double hs = kl < 30 ? Hs(r, 0, 0.4) : kl < 50 ? Hs(r, 1, 0.5) : kl < 100 ? Hs(r, 2, 0.75) : kl < 200 ? Hs(r, 3, 1) : kl < 400 ? Hs(r, 4, 1.5) : 2;
            return TronJs(hs * v.DonGiaCuon);
        }

        /// <summary>Tính 1 tấm. Không có giá cho vật liệu + độ dày hoặc thiếu kích thước → Loi.</summary>
        public static (KetQuaGia Kq, string Loi) Tinh(DauVaoGia v, BangGia b)
        {
            if (string.IsNullOrEmpty(v.VatLieu) || v.DoDay <= 0 || v.X <= 0 || v.Y <= 0 || v.SoLuong <= 0)
                return (null, "Thiếu vật liệu, độ dày, kích thước hoặc số lượng.");
            DongBangGia g = b.Dong.FirstOrDefault(x => x.VatLieu == v.VatLieu && x.DoDay == v.DoDay);
            if (g == null) return (null, LoiChuaCoGia(v));

            string hoa = v.VatLieu.ToUpperInvariant();
            bool coSus = hoa.Contains("SUS");
            bool datNgoai = g.DonGia == 0 && v.DonGiaDatNgoai > 0;
            b.CongTyUuDai.TryGetValue(v.CongTy ?? "", out bool uuDai);
            double heSoVL = datNgoai ? (coSus || hoa.StartsWith("A") ? 1.2 : 1.3)
                          : Hs(b.HeSoVL, (uuDai ? "Ưu đãi " : "Không ưu đãi ") + (coSus ? "SUS" : "SS"), 1);
            double donGia = datNgoai ? v.DonGiaDatNgoai : g.DonGia;

            double kl = (v.X + 10) * (v.Y + 10) * v.DoDay * g.TyTrong / 1_000_000;
            double donGiaVL = donGia > 0 ? donGia * heSoVL : 0;
            double tienVL = donGiaVL > 0 ? kl * donGiaVL : 0;
            double tienCat = g.GiaCat > 0 ? g.GiaCat * v.DoDay * ((v.X + v.Y) / 1000) * 2 : 0;
            double tienLo = (v.LoNho + v.LoLon * 1.5) * g.GiaMoLo;
            double tienTappu = v.Tappu * g.GiaTappu, tienSara = v.Sara * g.GiaTappu;

            double hsKL = HeSoKL(kl), tienBe = 0;
            foreach (var be in v.Be)
            {
                if (be.Dai <= 0 || g.GiaUon <= 0) continue;
                tienBe += be.SoDuong * Math.Max(be.Dai / 1000 * g.GiaUon * Math.Max(hsKL, HeSoCD(be.Dai)), 300);
            }
            double tienVat = v.VatMm / 1000 * (g.GiaVat != 0 ? g.GiaVat : 1800);
            double giaCong = tienLo + tienTappu + tienSara + tienBe + TienPitchi(v, b) + TienCuon(v, b) + tienVat;

            double tong = tienVL + tienCat + giaCong;
            double vlNhap = kl * donGia, tiLe = tong > 0 ? vlNhap / tong : 0;
            bool laInox = hoa.StartsWith("SUS");
            double tho = !laInox && tiLe > 0.55 ? vlNhap * (20.0 / 11) : laInox && tiLe > 0.68 ? vlNhap * (25.0 / 17) : tong;
            var kq = new KetQuaGia { KlThucTe = kl, TienVL = tienVL, TienCat = tienCat, TongGiaCong = giaCong, Gia1Tam = Tron10(tho) };

            if (v.CheDoTinh == 0 && !coSus && b.DacBiet.TryGetValue(v.CongTy ?? "", out CongTyDacBiet db))
            {
                bool coGiaCong = v.Be.Any(x => x.Dai > 0) || v.PitchiSoLan > 0 || v.Tappu > 0 || v.Sara > 0 || v.DonGiaCuon > 0 || v.VatMm > 0;
                double giaKg = coGiaCong ? db.DonGiaTamGiaCong : db.DonGiaTamDon;
                double klDb = (v.X + 6) * (v.Y + 6) * v.DoDay * (g.TyTrong != 0 ? g.TyTrong : 7.85) / 1_000_000;
                double giaKL = Tron10(giaKg * klDb);
                if (!db.SoSanhGiaThuong || giaKL < kq.Gia1Tam)
                {
                    kq.TienVL = giaKg * klDb; kq.KlThucTe = klDb; kq.TienCat = 0; kq.TongGiaCong = 0; kq.Gia1Tam = giaKL;
                }
            }
            if (v.CheDoTinh == 1)
            {
                kq.TienVL = 0; kq.TienCat = 0;
                kq.Gia1Tam = Tron10(kq.TongGiaCong);
            }
            else if (v.TienVLTay != null)
            {
                // đã tự tính vật liệu: không áp hệ số, luật tỉ trọng hay giá theo kg của công ty
                kq.TienVL = v.TienVLTay.Value; kq.KlThucTe = kl; kq.TienCat = tienCat; kq.TongGiaCong = giaCong;
                kq.Gia1Tam = Tron10(kq.TienVL + tienCat + giaCong);
            }
            else if (v.CheDoTinh == 2)
            {
                kq.TienVL *= 0.15;
                kq.Gia1Tam = Tron10(kq.TienVL + kq.TienCat + kq.TongGiaCong);
            }
            if (v.LamTron250 && kq.Gia1Tam < 250) kq.Gia1Tam = 250;
            kq.AllIn = kq.Gia1Tam * v.SoLuong;
            return (kq, null);
        }

        private static string LoiChuaCoGia(DauVaoGia v) => "Chưa có giá: " + v.VatLieu + " dày " + v.DoDay.ToString(CultureInfo.InvariantCulture) + ".";

        /// <summary>
        /// Bảng giá = 0 mà tấm cần tiền vật liệu → báo thiếu, để không ra báo giá thấp mà không ai biết.
        /// Không cần giá VL: chế độ chỉ gia công, công ty tính theo kg không so với giá thường.
        /// </summary>
        private static string LoiGiaVlBang0(DauVaoGia v, BangGia b)
        {
            DongBangGia g = b.Dong.FirstOrDefault(x => x.VatLieu == v.VatLieu && x.DoDay == v.DoDay);
            if (g == null || g.DonGia > 0 || v.DonGiaDatNgoai > 0 || v.CheDoTinh == 1 || v.TienVLTay != null) return null;
            bool coSus = (v.VatLieu ?? "").ToUpperInvariant().Contains("SUS");
            if (v.CheDoTinh == 0 && !coSus && b.DacBiet.TryGetValue(v.CongTy ?? "", out CongTyDacBiet db) && !db.SoSanhGiaThuong) return null;
            return LoiChuaCoGia(v);
        }

        private static bool So(string s, out double d) =>
            double.TryParse((s ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out d);

        /// <summary>Giá người dùng gõ có thể có dấu phẩy ngăn nghìn ("12,000").</summary>
        public static bool SoTien(string s, out double d) => So((s ?? "").Replace(",", ""), out d);

        public static (DauVaoGia V, string Loi) TaoDauVao(TamXuat t, GiaTam g, string congTy)
        {
            if (!So(t.T, out double doDay) || !So(t.X, out double x) || !So(t.Y, out double y) || !So(t.Q, out double q))
                return (null, "Độ dày, kích thước hoặc số lượng không phải số.");
            int.TryParse(t.Tt.CheDoTinh, out int cheDo);
            var v = new DauVaoGia
            {
                VatLieu = t.M, CongTy = congTy, LoaiPitchi = g?.LoaiPitchi ?? "Pitchi", DoDay = doDay, X = x, Y = y, SoLuong = q,
                LoNho = t.Tt.LoNho, LoLon = t.Tt.LoLon, Tappu = t.Tt.Tappu, Sara = t.Tt.Sara,
                PitchiSoLan = t.Tt.PitchiSoLan, PitchiDai = t.Tt.PitchiDai, VatMm = t.Tt.Vat,
                DonGiaCuon = DonGiaCuonCua(t, g), LamTron250 = g?.LamTron250 ?? false, CheDoTinh = cheDo,
                TienVLTay = SoTien(t.Tt.TienVatLieu, out double tay) && tay >= 0 ? tay : (double?)null,
            };
            v.Be.AddRange(t.Tt.DuongBe.Select(b => ((double)b.SoLuong, (double)b.Dai)));
            return (v, null);
        }

        /// <summary>
        /// Tính lại mọi tấm không phải giá tay. Giá đã có mà không được đánh dấu tự động là giá tay, không bị đè.
        /// Tấm không tính được thì xoá giá tự động cũ để không xuất nhầm. Trả về tên tấm → lý do.
        /// </summary>
        public static Dictionary<string, string> ApDung(IEnumerable<TamXuat> ds, GiaDon don, BangGia b)
        {
            var canhBao = new Dictionary<string, string>();
            foreach (TamXuat t in ds)
            {
                GiaTam g = don.Lay(t.N);
                var (v, loi) = TaoDauVao(t, g, don.CongTy);
                KetQuaGia kq = null;
                if (v != null) v.LamTron250 = don.Tron250(g);
                if (loi == null) loi = LoiGiaVlBang0(v, b);
                if (loi == null) (kq, loi) = Tinh(v, b);
                g.TienVlHienTai = kq?.TienVL ?? 0;
                if (!string.IsNullOrEmpty(t.Tt.GiaBaoGia) && !g.TuDong) continue;   // giá tay: không đè, không cảnh báo
                if (kq == null)
                {
                    t.Tt.GiaBaoGia = "";
                    g.TuDong = false;
                    canhBao[t.N] = loi;
                    continue;
                }
                t.Tt.GiaBaoGia = kq.Gia1Tam.ToString("0", CultureInfo.InvariantCulture);
                g.TuDong = true;
            }
            return canhBao;
        }

        /// <summary>Tổng đơn = đơn giá × số lượng của các tấm đã có giá.</summary>
        public static (double Tong, int CoGia) TongTien(IEnumerable<TamXuat> ds)
        {
            double tong = 0;
            int coGia = 0;
            foreach (TamXuat t in ds)
                if (SoTien(t.Tt.GiaBaoGia, out double gia) && So(t.Q, out double q)) { tong += gia * q; coGia++; }
            return (tong, coGia);
        }
    }
}
