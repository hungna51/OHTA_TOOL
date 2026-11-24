using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvTool.Core;

namespace CsvTool.Tests
{
    /// <summary>
    /// Tính giá. Bảng giá dưới đây là SỐ GIẢ (repo public, không để giá thật). Các ca mẫu lấy từ việc chạy
    /// đúng mã nguồn web price-calc (lib/calculate.ts + form) trên chính bảng giá giả này.
    /// </summary>
    internal static partial class Program
    {
        private static DongBangGia DongGia(string v, double d, double dg, double u, double c, double l, double t, double vat, double ty) =>
            new DongBangGia { VatLieu = v, DoDay = d, DonGia = dg, GiaUon = u, GiaCat = c, GiaMoLo = l, GiaTappu = t, GiaVat = vat, TyTrong = ty };

        private static BangGia BangGiaGia()
        {
            var b = new BangGia();
            b.Dong.AddRange(new[] { DongGia("SS400", 3.2, 150, 700, 6, 30, 120, 1500, 7.85), DongGia("SS400", 6, 140, 900, 7, 45, 150, 1700, 7.85), DongGia("SS400", 12, 0, 1400, 9, 80, 200, 0, 7.85),
                DongGia("SUS304", 3, 620, 1100, 12, 60, 180, 2100, 7.93), DongGia("SUS304", 9, 700, 1800, 20, 140, 260, 2600, 7.93), DongGia("A5052", 2, 900, 600, 5, 25, 100, 1400, 2.68) });
            b.HeSoVL["Ưu đãi SS"] = 1.15; b.HeSoVL["Ưu đãi SUS"] = 1.05; b.HeSoVL["Không ưu đãi SS"] = 1.2; b.HeSoVL["Không ưu đãi SUS"] = 1.25;
            b.PitchiTrongLuong.AddRange(new[] { 0.8, 1, 1.5, 2 });
            b.PitchiChieuDai.AddRange(new[] { 0.75, 1, 1.25, 1.5, 1.75, 2, 2.25, 2.5 });
            b.HeSoCuon.AddRange(new[] { 0.4, 0.5, 0.75, 1, 1.5, 2 });
            b.HeSoGiaCong["Pitchi"] = 1; b.HeSoGiaCong["Non"] = 1.5; b.HeSoGiaCong["NonLech"] = 2; b.HeSoGiaCong["Kakumaru"] = 1.2; b.HeSoGiaCong["KakumaruLech"] = 1.8;
            b.CongTyUuDai["UD"] = true; b.CongTyUuDai["TH"] = false; b.CongTyUuDai["DB1"] = false; b.CongTyUuDai["DB2"] = false;
            b.DacBiet["DB1"] = new CongTyDacBiet { DonGiaTamDon = 260, DonGiaTamGiaCong = 330, SoSanhGiaThuong = false };
            b.DacBiet["DB2"] = new CongTyDacBiet { DonGiaTamDon = 260, DonGiaTamGiaCong = 330, SoSanhGiaThuong = true };
            return b;
        }

        // vật liệu|dày|x|y|q|lỗ nhỏ|lỗ lớn|tapu|sara|bẻ|pitchi lần|pitchi dài|loại|cuộn|vát|đặt ngoài|chế độ|tròn 250|công ty|giá 1 tấm|all-in
        private static readonly string[] CaWeb =
        {
            "SUS304|3|1219|1072|106|0|0|0|1|2*4500;1*6000;4*3000|0|4200|Kakumaru|0|0|0|0|0|UD|113340|12014040",
            "SS400|12|301.1|1219|2|0|0|0|0|1*3999|0|829|Kakumaru|0|0|180|0|0|DB1|11690|23380",
            "SUS304|3|575|193|1|0|0|0|0||3|100|Pitchi|6000|0|0|1|1|UD|4660|4660",   // che do 1 co chu y khac web (4380 + 15% VL)
            "SS400|3.2|86.8|200|1|10|0|0|0|1*1950;2*999|0|5000|Kakumaru|0|0|0|0|0|DB2|160|160",
            "SS400|6|30|1166|5|0|0|0|0||0|5900|KakumaruLech|0|0|0|0|0|DB1|520|2600",
            "SUS304|9|86.8|1076|3|10|0|2|0|3*2999;1*718|0|830|KakumaruLech|0|0|0|0|0|TH|26390|79170",
            "SS400|6|30|65|36|0|0|0|0||0|4200|KakumaruLech|0|0|0|0|1|TH|250|9000",
            "SS400|6|575|1219|3|2|0|0|3||0|829|Non|6000|0|0|2|0|UD|4510|13530",
        };

        private static double SoCa(string s) => double.Parse(s, CultureInfo.InvariantCulture);

        private static void TestGia()
        {
            Console.WriteLine("[Gia - cong thuc price-calc]");
            BangGia b = BangGiaGia();
            foreach (string dong in CaWeb)
            {
                string[] o = dong.Split('|');
                var v = new DauVaoGia
                {
                    VatLieu = o[0], DoDay = SoCa(o[1]), X = SoCa(o[2]), Y = SoCa(o[3]), SoLuong = SoCa(o[4]), LoNho = SoCa(o[5]), LoLon = SoCa(o[6]), Tappu = SoCa(o[7]), Sara = SoCa(o[8]),
                    PitchiSoLan = SoCa(o[10]), PitchiDai = SoCa(o[11]), LoaiPitchi = o[12], DonGiaCuon = SoCa(o[13]), VatMm = SoCa(o[14]), DonGiaDatNgoai = SoCa(o[15]),
                    CheDoTinh = int.Parse(o[16]), LamTron250 = o[17] == "1", CongTy = o[18],
                };
                foreach (string x in o[9].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { var p = x.Split('*'); v.Be.Add((SoCa(p[0]), SoCa(p[1]))); }
                var r = TinhGia.Tinh(v, b);
                Kiem("Web: " + o[0] + " " + o[1] + " " + o[18] + " che do " + o[16], r.Kq == null ? r.Loi : r.Kq.Gia1Tam + "/" + r.Kq.AllIn, o[19] + "/" + o[20]);
            }
            Kiem("Khong co gia vat lieu", TinhGia.Tinh(new DauVaoGia { VatLieu = "SPCC", DoDay = 1, X = 10, Y = 10, SoLuong = 1 }, b).Loi, "Chưa có giá: SPCC dày 1.");

            TamXuat TamGia(string n, string gia = "") => new TamXuat { N = n, M = "SS400", T = "6.0", Q = "3", X = "575", Y = "1219", Tt = new ThongTinTam { GiaBaoGia = gia, CheDoTinh = "0" } };
            var tu = TamGia("A-1");
            var tay = TamGia("A-2", "12,000");
            var khongGia = TamGia("A-3"); khongGia.M = "SPCC";
            var cuon = TamGia("A-4"); cuon.Tt.LoaiGiaCong = "R1";
            var don = new GiaDon { CongTy = "TH" };
            var cb = TinhGia.ApDung(new[] { tu, tay, khongGia, cuon }, don, b);
            Kiem("ApDung: tam thuong co gia, danh dau tu dong", tu.Tt.GiaBaoGia != "" && don.Tam["A-1"].TuDong, true);
            Kiem("ApDung: gia da co la gia tay, khong de", tay.Tt.GiaBaoGia + "|" + don.Tam.ContainsKey("A-2"), "12,000|True");
            Kiem("ApDung: khong co vat lieu -> canh bao", cb["A-3"], "Chưa có giá: SPCC dày 6.");
            Kiem("Cuon: r1 tu tinh nua hinh, gia cao hon tam khong cuon",
                 cb.ContainsKey("A-4") + "|" + TinhGia.DonGiaCuonCua(cuon, don.Tam["A-4"]) + "|" + (double.Parse(cuon.Tt.GiaBaoGia) > double.Parse(tu.Tt.GiaBaoGia)), "False|6000|True");
            var cuonOng = TamGia("A-5"); cuonOng.Tt.LoaiGiaCong = "m, r2";
            Kiem("Cuon: r2 = ca hinh", TinhGia.DonGiaCuonCua(cuonOng, null), 12000.0);
            Kiem("Cuon: chon tren the uu tien hon r1/r2", TinhGia.DonGiaCuonCua(cuonOng, new GiaTam { DonGiaCuon = 6000 }), 6000.0);
            Kiem("Cuon: chon Khong thi khong tinh", TinhGia.DonGiaCuonCua(cuonOng, new GiaTam { DonGiaCuon = -1 }), 0.0);
            Kiem("Cuon: khong co r -> khong tinh", TinhGia.DonGiaCuonCua(TamGia("A-6"), null), 0.0);
            string giaCu = tu.Tt.GiaBaoGia;
            tu.X = "100";
            TinhGia.ApDung(new[] { tu }, don, b);
            Kiem("ApDung: doi so do -> gia tu dong tinh lai", tu.Tt.GiaBaoGia != giaCu, true);
            tu.M = "SPCC";
            TinhGia.ApDung(new[] { tu }, don, b);
            Kiem("ApDung: het gia -> xoa gia tu dong cu", tu.Tt.GiaBaoGia + "|" + don.Tam["A-1"].TuDong, "|False");

            var tong = TinhGia.TongTien(new[] { tay, TamGia("B", "100"), TamGia("C") });
            Kiem("TongTien: don gia x Q, bo tam chua co gia", tong.Tong + "|" + tong.CoGia, "36300|2");

            // SS400 6 x 575 x 1219, Q=3: thuong (585*1229*6*7.85/1e6)*3 = 101.59; dac biet (581*1225)*... *3 = 100.57; SUS 3 = 51.31
            TamXuat TamKg(string n) => new TamXuat { N = n, M = "SS400", T = "6.0", Q = "3", X = "575", Y = "1219", Tt = new ThongTinTam { CheDoTinh = "0" } };
            var kDon = TamKg("K1"); var kGc = TamKg("K2"); kGc.Tt.Tappu = 2;
            var kSus = TamKg("K3"); kSus.M = "SUS304"; kSus.T = "3"; var kThieu = TamKg("K4"); kThieu.M = "SPCC";
            var kTay = TamKg("K5"); kTay.Tt.GiaBaoGia = "5000";
            string R(double x) => x.ToString("0.00", CultureInfo.InvariantCulture);
            var cTh = TinhGia.TongKg(new[] { kDon, kGc, kThieu }, new GiaDon { CongTy = "TH" }, b);
            Kiem("TongKg: cong ty thuong, (X+10), tach don/gia cong, dem tam chua co KG",
                 R(cTh.KgDon) + "|" + R(cTh.KgGiaCong) + "|" + cTh.ChuaCoKg + "|" + cTh.DacBiet, "101.59|101.59|1|False");
            var cDb = TinhGia.TongKg(new[] { kDon, kGc, kSus, kTay }, new GiaDon { CongTy = "DB1" }, b);
            Kiem("TongKg: dac biet, (X+6) theo kg, SUS + gia tay tinh thuong",
                 R(cDb.KgDbDon) + "|" + cDb.DonGiaKgDon + "|" + R(cDb.KgDbGiaCong) + "|" + cDb.DonGiaKgGiaCong + "|" + cDb.TamThuong + "|" + R(cDb.KgDon),
                 "100.57|260|100.57|330|2|253.47");
            Kiem("TongKg: tien theo kg = gia 1 tam x Q", cDb.TienDbDon, TinhGia.Tinh(TinhGia.TaoDauVao(kDon, null, "DB1").V, b).Kq.Gia1Tam * 3);
            Kiem("Dong10: cot don gia, boc nhay khi co phay", CsvXuat.Dong10(tay, "9").EndsWith(",,\"12,000\""), true);

            Kiem("ChuanDoDay: so nguyen -> 1 so le", PhanTichKhung.ChuanDoDay("6"), "6.0");
            Kiem("ChuanDoDay: dau phay", PhanTichKhung.ChuanDoDay(" 3,2 "), "3.2");
            Kiem("ChuanDoDay: giu so le co san", PhanTichKhung.ChuanDoDay("1.25"), "1.25");
            Kiem("ChuanDoDay: sai -> null", PhanTichKhung.ChuanDoDay("abc") ?? PhanTichKhung.ChuanDoDay("0") ?? PhanTichKhung.ChuanDoDay("-1") ?? PhanTichKhung.ChuanDoDay(""), null);

            var dTron = new GiaDon { LamTron250 = true };
            Kiem("Tron250: ca don bat -> tam chua chinh duoc tron", dTron.Tron250(new GiaTam()), true);
            Kiem("Tron250: tat rieng 1 tam", dTron.Tron250(new GiaTam { TatTron250 = true }), false);
            Kiem("Tron250: don tat, tam bat rieng", new GiaDon().Tron250(new GiaTam { LamTron250 = true }), true);
            var tamNho = new TamXuat { N = "R", M = "SS400", T = "6.0", Q = "1", X = "10", Y = "10", Tt = new ThongTinTam { CheDoTinh = "1" } };
            var dNho = new GiaDon { CongTy = "TH" };
            TinhGia.ApDung(new[] { tamNho }, dNho, b);
            string giaNho = tamNho.Tt.GiaBaoGia;
            dNho.LamTron250 = true;
            TinhGia.ApDung(new[] { tamNho }, dNho, b);
            Kiem("ApDung: tron 250 cho ca don", giaNho != "250" && tamNho.Tt.GiaBaoGia == "250", true);

            var dSua = new GiaDon();
            dSua.Lay("S").CheDoRieng = true; dSua.Lay("S").DonGiaCuon = 12000;
            var ttCu = new ThongTinTam { CheDoTinh = "1", DuongBe = new List<(int, int)> { (700, 2) } };
            var ttMoi = new ThongTinTam { CheDoTinh = "0", DuongBe = new List<(int, int)> { (900, 1) } };
            dSua.GiuSuaTay("S", ttMoi, ttCu);
            Kiem("GiuSuaTay: giu che do rieng, be va o cuon theo ban ve", ttMoi.CheDoTinh + "|" + ttMoi.DuongBe[0].Item1 + "|" + dSua.Tam["S"].DonGiaCuon, "1|900|0");
            var ttKhac = new ThongTinTam { CheDoTinh = "0", DuongBe = new List<(int, int)> { (900, 1) } };
            dSua.GiuSuaTay("KHAC", ttKhac, ttCu);
            Kiem("GiuSuaTay: tam khong sua lay theo ban ve", ttKhac.CheDoTinh + "|" + ttKhac.DuongBe[0].Item1, "0|900");

            var ttQuetLai = new ThongTinTam { CheDoTinh = "0", DuongBe = new List<(int, int)> { (900, 1) } };
            dSua.GiuSuaTay("S", ttQuetLai, null);
            Kiem("GiuSuaTay: tam da roi danh sach -> bo dau sua tay", dSua.Tam["S"].CheDoRieng + "|" + ttQuetLai.DuongBe[0].Item1, "False|900");

            var vl = new GiaVlDon();
            vl.Dat(new DongBangGia { VatLieu = "ZAM", DoDay = 3, DonGia = 200 });
            var tamZam = new TamXuat { N = "Z", M = "ZAM", T = "3.0", Q = "1", X = "100", Y = "100", Tt = new ThongTinTam { CheDoTinh = "0" } };
            var dZam = new GiaDon { CongTy = "TH" };
            Kiem("GiaVl: khong co trong database -> bao thieu", TinhGia.ApDung(new[] { tamZam }, dZam, b)["Z"], "Chưa có giá: ZAM dày 3.");
            Kiem("GiaVl: co gia tam nhung khong co database -> van chua tinh", TinhGia.ApDung(new[] { tamZam }, dZam, vl.Gop(b))["Z"] + "|" + tamZam.Tt.GiaBaoGia, "Chưa có giá: ZAM dày 3.|");
            var vl2 = new GiaVlDon();
            vl2.Dat(new DongBangGia { VatLieu = "SS400", DoDay = 6, DonGia = 999, GiaCat = 1234, TyTrong = 1 });
            var dongGop = vl2.Gop(b).Dong.First(x => x.VatLieu == "SS400" && x.DoDay == 6);
            Kiem("GiaVl: chi lay gia VL, thong so theo database", dongGop.DonGia + "|" + dongGop.GiaCat + "|" + dongGop.TyTrong, "999|7|7.85");
            Kiem("GiaVl: bang goc khong bi doi", b.Dong.First(x => x.VatLieu == "SS400" && x.DoDay == 6).DonGia, 140.0);
            TamXuat Tam12(string n, string cheDo) => new TamXuat { N = n, M = "SS400", T = "12.0", Q = "1", X = "100", Y = "100", Tt = new ThongTinTam { CheDoTinh = cheDo } };
            Kiem("Gia 0: che do thuong -> bao thieu", TinhGia.ApDung(new[] { Tam12("A", "0") }, new GiaDon { CongTy = "TH" }, b)["A"], "Chưa có giá: SS400 dày 12.");
            Kiem("Gia 0: VL khach cap -> bao thieu", TinhGia.ApDung(new[] { Tam12("A", "2") }, new GiaDon { CongTy = "TH" }, b).ContainsKey("A"), true);
            var tam0Gc = Tam12("A", "1");
            Kiem("Gia 0: chi gia cong can 15% VL -> bao thieu", TinhGia.ApDung(new[] { tam0Gc }, new GiaDon { CongTy = "TH" }, b).ContainsKey("A") + "|" + tam0Gc.Tt.GiaBaoGia, "True|");
            DauVaoGia VaoGc(int cheDo, double lo) => new DauVaoGia { VatLieu = "SS400", DoDay = 6, X = 500, Y = 400, SoLuong = 1, LoNho = lo, Tappu = 2, CongTy = "TH", CheDoTinh = cheDo };
            var gcCoLo = TinhGia.Tinh(VaoGc(1, 8), b).Kq; var gcKhongLo = TinhGia.Tinh(VaoGc(1, 0), b).Kq; var thuong = TinhGia.Tinh(VaoGc(0, 0), b).Kq;
            Kiem("Chi gia cong: khong tinh mo lo", gcCoLo.Gia1Tam == gcKhongLo.Gia1Tam && gcCoLo.TongGiaCong == gcKhongLo.TongGiaCong, true);
            Kiem("Chi gia cong: 15% VL, khong cat", Math.Abs(gcKhongLo.TienVL - thuong.TienVL * 0.15) < 1e-6 && gcKhongLo.TienCat == 0, true);
            var tam0Kg = Tam12("A", "0");
            Kiem("Gia 0: cong ty theo kg -> van tinh", TinhGia.ApDung(new[] { tam0Kg }, new GiaDon { CongTy = "DB1" }, b).Count + "|" + (tam0Kg.Tt.GiaBaoGia != ""), "0|True");
            Kiem("Gia 0: cong ty so voi gia thuong -> bao thieu", TinhGia.ApDung(new[] { Tam12("A", "0") }, new GiaDon { CongTy = "DB2" }, b).ContainsKey("A"), true);
            var vl12 = new GiaVlDon();
            vl12.Dat(new DongBangGia { VatLieu = "SS400", DoDay = 12, DonGia = 160 });
            var tam0Tam = Tam12("A", "0");
            Kiem("Gia 0: nhap gia tam -> tinh duoc", TinhGia.ApDung(new[] { tam0Tam }, new GiaDon { CongTy = "TH" }, vl12.Gop(b)).Count + "|" + (tam0Tam.Tt.GiaBaoGia != ""), "0|True");

            string GiaVlTay(string m, string t, string cheDo, string congTy, string tien)
            {
                var x = new TamXuat { N = "V", M = m, T = t, Q = "1", X = "100", Y = "100", Tt = new ThongTinTam { CheDoTinh = cheDo, TienVatLieu = tien } };
                var cb = TinhGia.ApDung(new[] { x }, new GiaDon { CongTy = congTy }, b);
                return cb.ContainsKey("V") ? "thieu" : x.Tt.GiaBaoGia;
            }
            Kiem("VL tay: VL qua 55% -> VL x 20/11 (1000 / 1016.8)", GiaVlTay("SS400", "6.0", "0", "TH", "1000"), "1820");
            Kiem("VL tay: VL duoi 55% -> VL + cat (10 + 16.8)", GiaVlTay("SS400", "6.0", "0", "TH", "10"), "30");
            Kiem("VL tay: VL khach cap khong nhan 0.15", GiaVlTay("SS400", "6.0", "2", "TH", "1000"), "1820");
            Kiem("VL tay: cong ty theo kg khong de", GiaVlTay("SS400", "6.0", "0", "DB1", "1000"), "1820");
            Kiem("VL tay: chi gia cong lay dung so nhap, khong cat", GiaVlTay("SS400", "6.0", "1", "TH", "1000"), "1000");
            Kiem("VL tay: bang gia = 0 van tinh duoc", GiaVlTay("SS400", "12.0", "0", "TH", "500"), "910");
            Kiem("VL tay: khong co trong database van thieu", GiaVlTay("ZAM", "3.0", "0", "TH", "500"), "thieu");
            Kiem("VL tay: de trong tinh nhu thuong", GiaVlTay("SS400", "6.0", "0", "TH", "") != "1820", true);
            var dHien = new GiaDon { CongTy = "TH" };
            var tHien = new TamXuat { N = "H", M = "SS400", T = "6.0", Q = "1", X = "100", Y = "100", Tt = new ThongTinTam { CheDoTinh = "0", GiaBaoGia = "9999" } };
            TinhGia.ApDung(new[] { tHien }, dHien, b);
            Kiem("VL tay: tam gia tay van co tien VL de hien, khong doi gia", dHien.Tam["H"].TienVlHienTai > 0 && tHien.Tt.GiaBaoGia == "9999", true);

            var dsVl = GiaVlDon.DsVatLieu(new[] { tamZam, tamNho, new TamXuat { N = "H", M = "SS400", T = "6", Q = "1", Tt = new ThongTinTam() }, new TamXuat { N = "L", M = "SS400", T = "x", Q = "1", Tt = new ThongTinTam() } });
            Kiem("GiaVl: gom theo vat lieu + do day", string.Join(";", dsVl.Select(x => x.VatLieu + "/" + x.DoDay + "/" + x.SoTam)), "SS400/6/2;ZAM/3/1");

            var bCt = new BangGia();
            bCt.CongTyUuDai["OTK BANDO"] = true; bCt.TenNhat["OTK BANDO"] = "株式会社オーティーケー坂東";
            bCt.CongTyUuDai["HANWA"] = false; bCt.TenNhat["HANWA"] = "阪和興業";
            bCt.CongTyUuDai["HANWA 2"] = false;
            bCt.CongTyUuDai["ABC"] = false;
            bCt.DacBiet["OTK BANDO"] = new CongTyDacBiet { DonGiaTamDon = 230, DonGiaTamGiaCong = 280 };
            var dsCt = CongTyTim.Ds(bCt);
            string CtDung(string q) => CongTyTim.TimDung(dsCt, q)?.Ten;
            Kiem("CongTy: go phien am khong phan biet hoa thuong", CtDung("otk bando"), "OTK BANDO");
            Kiem("CongTy: dan ten tieng Nhat", CtDung("株式会社オーティーケー坂東"), "OTK BANDO");
            Kiem("CongTy: dan ten co (株) va 御中", CtDung("(株)オーティーケー坂東 御中"), "OTK BANDO");
            Kiem("CongTy: dan ky hieu ㈱", CtDung("㈱オーティーケー坂東"), "OTK BANDO");
            Kiem("CongTy: nua do rong", CtDung("ｵｰﾃｨｰｹｰ坂東"), "OTK BANDO");
            Kiem("CongTy: dan them chi nhanh", CtDung("阪和興業株式会社 大阪支店"), "HANWA");
            Kiem("CongTy: mot phan tieng Nhat", CtDung("坂東"), "OTK BANDO");
            Kiem("CongTy: khop dung uu tien hon khop mot phan", CtDung("hanwa"), "HANWA");
            Kiem("CongTy: nhieu ket qua, khong tu chon", CtDung("han"), null);
            Kiem("CongTy: loc nhieu ket qua", CongTyTim.Loc(dsCt, "han").Count, 2);
            Kiem("CongTy: khong co", CtDung("XYZ"), null);
            Kiem("CongTy: tag dac biet", dsCt.First(x => x.Ten == "OTK BANDO").NhanTamDon + "|" + dsCt.First(x => x.Ten == "OTK BANDO").NhanTamBe + "|" + dsCt.First(x => x.Ten == "OTK BANDO").UuDai, "Tấm đơn: ¥230/kg|Tấm bẻ: ¥280/kg|True");
            Kiem("CongTy: ban sao cu chua co ten Nhat", CongTyTim.Ds(new BangGia { TenNhat = null, CongTyUuDai = { ["A"] = false } })[0].TiengNhat, "");

            List<CungHinh> MatCat(double r, double t, double goc) => new List<CungHinh> { new CungHinh { CX = 5, CY = 7, R = r, Goc = goc }, new CungHinh { CX = 5, CY = 7, R = r - t, Goc = goc } };
            Kiem("Cuon: mau that 131190 R365", PhanTichHinh.LoaiCuonTuHinh(MatCat(365, 3.2, 180), 1141.97, 265, 3.2), "r1");
            Kiem("Cuon: mau that 131190 R300", PhanTichHinh.LoaiCuonTuHinh(MatCat(300, 3.2, 180), 937.77, 275, 3.2), "r1");
            Kiem("Cuon: kiem ca canh Y", PhanTichHinh.LoaiCuonTuHinh(MatCat(365, 3.2, 180), 265, 1141.97, 3.2), "r1");
            Kiem("Cuon: ca vong = r2", PhanTichHinh.LoaiCuonTuHinh(MatCat(95, 2.3, 360), 589.36, 100, 2.3), "r2");
            Kiem("Cuon: chieu dai khong khop", PhanTichHinh.LoaiCuonTuHinh(MatCat(365, 3.2, 180), 1000, 265, 3.2), null);
            Kiem("Cuon: khoang cach khac do day", PhanTichHinh.LoaiCuonTuHinh(MatCat(365, 5, 180), 1141.97, 265, 3.2), null);
            Kiem("Cuon: mot cung don khong tinh", PhanTichHinh.LoaiCuonTuHinh(new List<CungHinh> { new CungHinh { R = 363.5, Goc = 180 } }, 1141.97, 265, 3.2), null);
            var lechTam = MatCat(365, 3.2, 180); lechTam[1].CX += 10;
            Kiem("Cuon: khong dong tam", PhanTichHinh.LoaiCuonTuHinh(lechTam, 1141.97, 265, 3.2), null);
            var lechGoc = MatCat(365, 3.2, 180); lechGoc[1].Goc = 90;
            Kiem("Cuon: khac goc", PhanTichHinh.LoaiCuonTuHinh(lechGoc, 1141.97, 265, 3.2), null);
            // cung 90° tâm (0,0), đuôi thẳng 50 tiếp tuyến tại đầu (R,0): dài giữa = 90°·R + 50
            var cungDuoi = new List<CungHinh> {
                new CungHinh { R = 100, Goc = 90, X1 = 100, Y1 = 0, X2 = 0, Y2 = 100 },
                new CungHinh { R = 96.8, Goc = 90, X1 = 96.8, Y1 = 0, X2 = 0, Y2 = 96.8 } };
            var duoi = new List<NetBe> { new NetBe { X1 = 100, Y1 = 0, X2 = 100, Y2 = -50 }, new NetBe { X1 = 96.8, Y1 = 0, X2 = 96.8, Y2 = -50 } };
            double daiDuoi = Math.PI / 2 * 98.4 + 50;
            Kiem("Cuon: cung + phan thang", PhanTichHinh.LoaiCuonTuHinh(cungDuoi, daiDuoi, 20, 3.2, duoi), "r1");
            Kiem("Cuon: thieu phan thang thi khong khop", PhanTichHinh.LoaiCuonTuHinh(cungDuoi, daiDuoi, 20, 3.2), null);
            Kiem("Cuon: chi 1 net thang khong tinh", PhanTichHinh.LoaiCuonTuHinh(cungDuoi, daiDuoi, 20, 3.2, duoi.Take(1).ToList()), null);
            var napCuoi = new List<NetBe> { new NetBe { X1 = 100, Y1 = 0, X2 = 50, Y2 = 0 }, new NetBe { X1 = 96.8, Y1 = 0, X2 = 46.8, Y2 = 0 } };
            Kiem("Cuon: doan theo ban kinh khong phai phan thang", PhanTichHinh.LoaiCuonTuHinh(cungDuoi, daiDuoi, 20, 3.2, napCuoi), null);
            // cuộn + chân bẻ 90° (giống 131190-5): cung trong R96.8 0–90°, cung ngoài R100 dừng ở x = 3.2,
            // chân theo hướng bán kính: mặt ngoài 50 từ đầu cung trong, mặt trong từ đầu cung ngoài; đầu kia có nắp dày t
            double gocNgoai = Math.Acos(3.2 / 100) * 180 / Math.PI, yNgoai = Math.Sqrt(100 * 100 - 3.2 * 3.2);
            var cungChan = new List<CungHinh> {
                new CungHinh { R = 100, Goc = gocNgoai, X1 = 100, Y1 = 0, X2 = 3.2, Y2 = yNgoai },
                new CungHinh { R = 96.8, Goc = 90, X1 = 96.8, Y1 = 0, X2 = 0, Y2 = 96.8 } };
            var chan = new List<NetBe> { new NetBe { X1 = 0, Y1 = 96.8, X2 = 0, Y2 = 146.8 }, new NetBe { X1 = 3.2, Y1 = yNgoai, X2 = 3.2, Y2 = 146.8 },
                                         new NetBe { X1 = 96.8, Y1 = 0, X2 = 100, Y2 = 0 } };
            double daiChan = Math.PI / 2 * 98.4 + 50 - 2 * 2.6;
            var v90 = new List<(double, double)> { (90.0, 2.6) };
            Kiem("Cuon + chan be: tru 2 x denta", PhanTichHinh.LoaiCuonTuHinh(cungChan, daiChan, 20, 3.2, chan, v90), "r1");
            Kiem("Cuon + chan be: khong co V= cung goc thi khong nhan", PhanTichHinh.LoaiCuonTuHinh(cungChan, daiChan, 20, 3.2, chan, new List<(double, double)> { (120.0, 2.6) }), null);
            Kiem("Cuon + chan be: chi cung van nhan khi X bang cung", PhanTichHinh.LoaiCuonTuHinh(cungChan, Math.PI / 2 * 98.4, 20, 3.2, chan, v90), "r1");
            // 2 chân: chân 1 đi lên, góc 90° rồi chân 2 sang trái; mặt ngoài góc 2 là phía x = 3.2 nên chân 1 dài 53.2
            var chan2 = new List<NetBe> {
                new NetBe { X1 = 0, Y1 = 96.8, X2 = 0, Y2 = 146.8 }, new NetBe { X1 = 3.2, Y1 = yNgoai, X2 = 3.2, Y2 = 150 },
                new NetBe { X1 = 0, Y1 = 146.8, X2 = -40, Y2 = 146.8 }, new NetBe { X1 = 3.2, Y1 = 150, X2 = -40, Y2 = 150 },
                new NetBe { X1 = -40, Y1 = 146.8, X2 = -40, Y2 = 150 }, new NetBe { X1 = 96.8, Y1 = 0, X2 = 100, Y2 = 0 } };
            double dai2Chan = Math.PI / 2 * 98.4 + 53.2 + 43.2 - 4 * 2.6;
            Kiem("Cuon + 2 chan be: do giua dinh nhon mat ngoai", PhanTichHinh.LoaiCuonTuHinh(cungChan, dai2Chan, 20, 3.2, chan2, v90), "r1");
            Kiem("Cuon + 2 chan be: chi tinh chan 1 thi khong khop", PhanTichHinh.LoaiCuonTuHinh(cungChan, Math.PI / 2 * 98.4 + 50 - 2 * 2.6, 20, 3.2, chan2, v90), null);
            Kiem("BeTuFname: doc goc, denta", string.Join(";", PhanTichKhung.BeTuFname(new[] { "N=A", "V=16-2.6(90°)", "シマ目外 V6引代 1.2(135°)" })), "(90, 2.6);(135, 1.2)");
            var gocBe = new List<CungHinh> {
                new CungHinh { R = 9, Goc = 90, X1 = 9, Y1 = 0, X2 = 0, Y2 = 9 },
                new CungHinh { R = 3, Goc = 90, X1 = 3, Y1 = 0, X2 = 0, Y2 = 3 } };
            var canhBe = new List<NetBe> { new NetBe { X1 = 9, Y1 = 0, X2 = 9, Y2 = -61 }, new NetBe { X1 = 3, Y1 = 0, X2 = 3, Y2 = -61 } };
            Kiem("Cuon: goc be ban kinh nho khong phai cuon", PhanTichHinh.LoaiCuonTuHinh(gocBe, Math.PI / 2 * 6 + 61, 75, 6, canhBe), null);
            var tamCuon = new TamXuat { N = "C", M = "SS400", T = "3.2", Q = "1", X = "1141.97", Y = "265", Tt = new ThongTinTam { LoaiGiaCong = "m" } };
            PhanTichKhung.ThemCuonTuHinh(tamCuon, MatCat(365, 3.2, 180));
            Kiem("Cuon: them r1 vao gia cong, gia cuon nua hinh", tamCuon.Tt.LoaiGiaCong + "|" + TinhGia.DonGiaCuonCua(tamCuon, null), "m, r1|6000");
            PhanTichKhung.ThemCuonTuHinh(tamCuon, MatCat(365, 3.2, 180));
            Kiem("Cuon: khong them trung", tamCuon.Tt.LoaiGiaCong, "m, r1");
            var kCuon = new KhungTho { Handle = 0x77, Text = KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2") }).Text, Dim = KhungMau().Dim };
            var gopCuon = PhanTichKhung.Ghep(kCuon, "1-XY", 8, new Dictionary<string, ThongTinTam> { ["A-1"] = new ThongTinTam { LoaiGiaCong = "m, r2" } },
                new Dictionary<long, ThongTinTam> { [0x77] = new ThongTinTam { LoaiGiaCong = "m" } });
            Kiem("Cuon: quet lai bo r2 cu, lay theo ban ve", gopCuon.Tam?.Tt.LoaiGiaCong, "m");

            int Sara1(string x) => PhanTichHinh.DemSara(new[] { x });
            Kiem("Sara: mau that 2x2-phi8 = 4", Sara1("{\\W1;2x2-∅8孔皿モミ}"), 4);
            Kiem("Sara: 4-phi8 hiragana", Sara1("4-φ8皿もみ"), 4);
            Kiem("Sara: nua do rong", Sara1("6-ｻﾗﾓﾐ"), 6);
            Kiem("Sara: 4xphi8", Sara1("4xφ8 皿モミ"), 4);
            Kiem("Sara: 3 cho", Sara1("∅8孔皿モミ 3箇所"), 3);
            Kiem("Sara: khong ghi so = 1", Sara1("∅8孔皿モミ"), 1);
            Kiem("Sara: dang ma U+", Sara1("2-\\U+76BF\\U+30E2\\U+30DF"), 2);
            Kiem("Sara: nhieu dong", Sara1("2-M5タップ\\P3-∅6皿加工"), 3);
            Kiem("Sara: mau that 131107, so lo o dong tren", Sara1("{\\fMS PGothic|b0|i0|c128|p50;5-∅6.8\\P皿加工(∅12.8)}"), 5);
            Kiem("Sara: dong tren dung %%C", Sara1("3-%%C8\\P皿加工"), 3);
            Kiem("Sara: dong tren la tap thi khong muon so", Sara1("4-M5タップ\\P皿加工"), 1);
            Kiem("Sara: ten vit khong tinh", Sara1("皿小ネジ"), 0);
            Kiem("Sara: tap khong tinh", Sara1("4-M5タップ"), 0);
            Kiem("Sara: loai gia cong co s", PhanTichHinh.LoaiGiaCong(new[] { new ChuHinh { NoiDung = "2x2-∅8孔皿モミ" } }, false), "s");
            var hinhSara = new DuLieuHinh { HandleKhung = 1 };
            hinhSara.Net.Add(new NetCat { Handle = 1, Loai = "LWPOLYLINE", Kin = true, DoDai = 400, Diem = new List<(double X, double Y)> { (0, 0), (100, 0), (100, 100), (0, 100), (0, 0) } });
            hinhSara.ChuTrongKhung.Add(new ChuHinh { Lop = "1-SUNPOU", NoiDung = "2x2-∅8孔皿モミ" });
            hinhSara.ChuDanTrong.Add("2x2-∅8孔皿モミ");   // leader của chính ghi chú trên
            hinhSara.ChuDanTrong.Add("2-∅6皿もみ");
            var kqSara = PhanTichHinh.Chay(hinhSara, 0.05, 0, (a, c) => null);
            Kiem("Sara: quet ra so lo + loai s, leader khong dem trung", kqSara.Tam == null ? "loi" : kqSara.Tam.Sara + "|" + kqSara.Tam.LoaiGiaCong, "6|s");
            var hinhDim = new DuLieuHinh { HandleKhung = 2 };
            hinhDim.Net.AddRange(hinhSara.Net);
            hinhDim.ChuDim.Add("{\\fMS PGothic|b0|i0|c128|p50;5-∅6.8\\P皿加工(∅12.8)}");
            hinhDim.ChuDim.Add("%%C6.8");
            var kqDim = PhanTichHinh.Chay(hinhDim, 0.05, 0, (a, c) => null);
            Kiem("Sara: doc tu chu ghi de cua dim", kqDim.Tam == null ? "loi" : kqDim.Tam.Sara + "|" + kqDim.Tam.LoaiGiaCong, "5|s");
            int Tapu1(string x) => PhanTichHinh.DemTapu(new[] { x });
            Kiem("Tapu: 4-M5", Tapu1("4-M5タップ"), 4);
            Kiem("Tapu: mau that 5x2-M12", Tapu1("{\\W1;5×2-M12タップ}"), 10);
            Kiem("Tapu: mau that 6x2-M12", Tapu1("{\\W1;6x2-M12タップ}"), 12);
            Kiem("Tapu: nua do rong", Tapu1("3-M6ﾀｯﾌﾟ"), 3);
            Kiem("Tapu: khong ghi so = 1", Tapu1("M5タップ"), 1);
            Kiem("Tapu: inch khong ghi so = 1", Tapu1("w1/2ﾀｯﾌﾟ"), 1);
            Kiem("Tapu: so lo o dong tren", Tapu1("{\\fMS PGothic|b0;4-M5\\Pタップ}"), 4);
            Kiem("Tapu: 3 cho", Tapu1("M8タップ 3箇所"), 3);
            Kiem("Tapu: tieng Anh", Tapu1("2-M6 TAP"), 2);
            Kiem("Tapu: text Lo Tappu cu khong tinh", Tapu1("Lỗ Tappu: 0"), 0);
            Kiem("Tapu: Sara khong tinh", Tapu1("4-∅8皿モミ"), 0);
            Kiem("Sara: dong tren la Sara, dong duoi la Tapu", Tapu1("2-∅8皿モミ\\PM5タップ"), 1);
            var hinhTap = new DuLieuHinh { HandleKhung = 3 };
            hinhTap.Net.AddRange(hinhSara.Net);
            hinhTap.ChuDim.Add("{\\W1;4-M5タップ}");
            var kqTap = PhanTichHinh.Chay(hinhTap, 0.05, 0, (a, c) => null);
            Kiem("Tapu: doc tu chu ghi de cua dim", kqTap.Tam.Tappu + "|" + kqTap.Tam.LoaiGiaCong + "|" + kqTap.CanhBao.Count, "4|t|0");
            hinhTap.SoHatch = 4;
            var kqTapKhop = PhanTichHinh.Chay(hinhTap, 0.05, 0, (a, c) => null);
            Kiem("Tapu: hatch khop ghi chu khong cong don", kqTapKhop.Tam.Tappu + "|" + kqTapKhop.CanhBao.Count, "4|0");
            hinhTap.SoHatch = 6;
            var kqTapLech = PhanTichHinh.Chay(hinhTap, 0.05, 0, (a, c) => null);
            Kiem("Tapu: lech -> lay so lon, co canh bao", kqTapLech.Tam.Tappu + "|" + kqTapLech.CanhBao.Count, "6|1");
            var hinhCaHai = new DuLieuHinh { HandleKhung = 4 };
            hinhCaHai.Net.AddRange(hinhSara.Net);
            hinhCaHai.ChuDim.Add("2-M5タップ");
            hinhCaHai.ChuDim.Add("3-∅8皿モミ");
            Kiem("Gia cong: thu tu t, s", PhanTichHinh.Chay(hinhCaHai, 0.05, 0, (a, c) => null).Tam.LoaiGiaCong, "t, s");

            string thu = Path.Combine(Path.GetTempPath(), "csvtool_gia_" + Guid.NewGuid().ToString("N"));
            try
            {
                string f = Path.Combine(thu, "data", "9-gia.json");
                Kiem("GiaDon: chua co file -> don trong", GiaDon.DocFile(f).Tam.Count, 0);
                var d = new GiaDon { CongTy = "株式会社 Á" };
                d.Lay("A-1").TuDong = true; d.Lay("A-1").DonGiaCuon = 6000; d.Lay("A-1").LoaiPitchi = "Non"; d.Lay("A-2").LamTron250 = true;
                d.GhiFile(f);
                var r = GiaDon.DocFile(f);
                Kiem("GiaDon: khu hoi", r.CongTy + "|" + r.Tam["A-1"].TuDong + "|" + r.Tam["A-1"].DonGiaCuon + "|" + r.Tam["A-1"].LoaiPitchi + "|" + r.Tam["A-2"].LamTron250, "株式会社 Á|True|6000|Non|True");
                r.DoiTen("A-1", "A-9");
                Kiem("GiaDon: doi ten giu thong tin", r.Tam.ContainsKey("A-1") + "|" + r.Tam["A-9"].DonGiaCuon, "False|6000");
                r.DoiTen("A-9", "A-2");
                Kiem("GiaDon: ten moi trung -> khong de tam kia", r.Tam["A-2"].LamTron250, true);
                Kiem("TepDangMo: file ranh", CsvXuat.TepDangMo(f, f + ".chua-co"), null);
                using (new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Kiem("TepDangMo: file dang bi giu", CsvXuat.TepDangMo(f + ".chua-co", f), f);
                string fVl = Path.Combine(thu, "data", "9-giavl.csv");
                Kiem("GiaVl: chua co file -> trong", GiaVlDon.DocFile(fVl).Dong.Count, 0);
                var vlFile = new GiaVlDon();
                vlFile.Dat(new DongBangGia { VatLieu = "ZAM,A", DoDay = 2.3, DonGia = 212.5 });
                vlFile.GhiFile(fVl);
                Kiem("GiaVl: khu hoi file", GiaVlDon.DocFile(fVl).Tim("ZAM,A", 2.3).DonGia, 212.5);
                Kiem("GiaVl: file co BOM", File.ReadAllBytes(fVl)[0], (byte)0xEF);
                File.WriteAllText(fVl, "Vat lieu,Do day,Gia VL,Gia cat,Gia uon,Gia mo lo,Gia tappu,Gia vat,Ty trong\r\nZAM,2.3,1220,35,400,25,150,1800,7.85\r\n");
                var vlCu = GiaVlDon.DocFile(fVl).Tim("ZAM", 2.3);
                Kiem("GiaVl: doc file ban cu, bo cot gia cong", vlCu.DonGia + "|" + vlCu.GiaCat, "1220|0");
                File.WriteAllText(fVl, GiaVlDon.TieuDe + "\r\nZAM,2.3,abc\r\n");
                KiemNemBatKy("GiaVl: file hong -> bao loi, khong coi la trong", () => GiaVlDon.DocFile(fVl));
                File.WriteAllText(f, "{hong");
                KiemNemBatKy("GiaDon: file hong -> bao loi, khong coi la trong", () => GiaDon.DocFile(f));
            }
            finally { if (Directory.Exists(thu)) Directory.Delete(thu, true); }
        }

        private static void KiemNemBatKy(string ten, Action a)
        {
            bool nem = false;
            try { a(); } catch (Exception) { nem = true; }
            Kiem(ten, nem, true);
        }
    }
}
