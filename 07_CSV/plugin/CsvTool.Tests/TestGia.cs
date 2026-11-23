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
            "SUS304|3|575|193|1|0|0|0|0||3|100|Pitchi|6000|0|0|1|1|UD|4380|4380",
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
            dSua.Lay("S").SuaBe = true; dSua.Lay("S").CheDoRieng = true;
            var ttCu = new ThongTinTam { CheDoTinh = "1", DuongBe = new List<(int, int)> { (700, 2) } };
            var ttMoi = new ThongTinTam { CheDoTinh = "0", DuongBe = new List<(int, int)> { (900, 1) } };
            dSua.GiuSuaTay("S", ttMoi, ttCu);
            Kiem("GiuSuaTay: giu be va che do rieng", ttMoi.CheDoTinh + "|" + ttMoi.DuongBe[0].Item1, "1|700");
            var ttKhac = new ThongTinTam { CheDoTinh = "0", DuongBe = new List<(int, int)> { (900, 1) } };
            dSua.GiuSuaTay("KHAC", ttKhac, ttCu);
            Kiem("GiuSuaTay: tam khong sua lay theo ban ve", ttKhac.CheDoTinh + "|" + ttKhac.DuongBe[0].Item1, "0|900");

            var goiY = GiaVlDon.GoiY(b, "ZAM", 3);
            Kiem("GiaVl: khong co trong bang -> de trong, khong muon vat lieu khac",
                 double.IsNaN(goiY.DonGia) && double.IsNaN(goiY.GiaCat) && double.IsNaN(goiY.GiaUon) && double.IsNaN(goiY.GiaMoLo) && double.IsNaN(goiY.GiaTappu) && double.IsNaN(goiY.GiaVat) && double.IsNaN(goiY.TyTrong), true);
            Kiem("GiaVl: thieu thong so -> chua du", GiaVlDon.Du(goiY), false);
            Kiem("GiaVl: dong da co trong bang lay dung dong", GiaVlDon.GoiY(b, "SUS304", 9).GiaCat, 20.0);
            var vl = new GiaVlDon();
            goiY = new DongBangGia { VatLieu = "ZAM", DoDay = 3, DonGia = 200, GiaCat = 6, GiaUon = 700, GiaMoLo = 30, GiaTappu = 120, GiaVat = 1500, TyTrong = 7.85 };
            Kiem("GiaVl: du 7 thong so", GiaVlDon.Du(goiY), true);
            vl.Dat(goiY);
            var tamZam = new TamXuat { N = "Z", M = "ZAM", T = "3.0", Q = "1", X = "100", Y = "100", Tt = new ThongTinTam { CheDoTinh = "0" } };
            var dZam = new GiaDon { CongTy = "TH" };
            Kiem("GiaVl: chua co gia tam -> canh bao", TinhGia.ApDung(new[] { tamZam }, dZam, b)["Z"], "Chưa có giá: ZAM dày 3.");
            Kiem("GiaVl: co gia tam -> tinh duoc", TinhGia.ApDung(new[] { tamZam }, dZam, vl.Gop(b)).Count + "|" + (tamZam.Tt.GiaBaoGia != ""), "0|True");
            var ghiDe = GiaVlDon.GoiY(b, "SS400", 6); ghiDe.DonGia = 999;
            var vl2 = new GiaVlDon(); vl2.Dat(ghiDe);
            Kiem("GiaVl: gia tam uu tien hon bang gia", vl2.Gop(b).Dong.First(x => x.VatLieu == "SS400" && x.DoDay == 6).DonGia, 999.0);
            Kiem("GiaVl: bang goc khong bi doi", b.Dong.First(x => x.VatLieu == "SS400" && x.DoDay == 6).DonGia, 140.0);
            TamXuat Tam12(string n, string cheDo) => new TamXuat { N = n, M = "SS400", T = "12.0", Q = "1", X = "100", Y = "100", Tt = new ThongTinTam { CheDoTinh = cheDo } };
            Kiem("Gia 0: che do thuong -> bao thieu", TinhGia.ApDung(new[] { Tam12("A", "0") }, new GiaDon { CongTy = "TH" }, b)["A"], "Chưa có giá: SS400 dày 12.");
            Kiem("Gia 0: VL khach cap -> bao thieu", TinhGia.ApDung(new[] { Tam12("A", "2") }, new GiaDon { CongTy = "TH" }, b).ContainsKey("A"), true);
            var tam0Gc = Tam12("A", "1");
            Kiem("Gia 0: chi gia cong -> van tinh", TinhGia.ApDung(new[] { tam0Gc }, new GiaDon { CongTy = "TH" }, b).Count + "|" + (tam0Gc.Tt.GiaBaoGia != ""), "0|True");
            var tam0Kg = Tam12("A", "0");
            Kiem("Gia 0: cong ty theo kg -> van tinh", TinhGia.ApDung(new[] { tam0Kg }, new GiaDon { CongTy = "DB1" }, b).Count + "|" + (tam0Kg.Tt.GiaBaoGia != ""), "0|True");
            Kiem("Gia 0: cong ty so voi gia thuong -> bao thieu", TinhGia.ApDung(new[] { Tam12("A", "0") }, new GiaDon { CongTy = "DB2" }, b).ContainsKey("A"), true);
            var vl12 = new GiaVlDon();
            var dong12 = GiaVlDon.GoiY(b, "SS400", 12); dong12.DonGia = 160; vl12.Dat(dong12);
            var tam0Tam = Tam12("A", "0");
            Kiem("Gia 0: nhap gia tam -> tinh duoc", TinhGia.ApDung(new[] { tam0Tam }, new GiaDon { CongTy = "TH" }, vl12.Gop(b)).Count + "|" + (tam0Tam.Tt.GiaBaoGia != ""), "0|True");

            var dsVl = GiaVlDon.DsVatLieu(new[] { tamZam, tamNho, new TamXuat { N = "H", M = "SS400", T = "6", Q = "1", Tt = new ThongTinTam() }, new TamXuat { N = "L", M = "SS400", T = "x", Q = "1", Tt = new ThongTinTam() } });
            Kiem("GiaVl: gom theo vat lieu + do day", string.Join(";", dsVl.Select(x => x.VatLieu + "/" + x.DoDay + "/" + x.SoTam)), "SS400/6/2;ZAM/3/1");

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
                vlFile.Dat(new DongBangGia { VatLieu = "ZAM,A", DoDay = 2.3, DonGia = 212.5, GiaCat = 6, GiaUon = 700, GiaMoLo = 30, GiaTappu = 120, GiaVat = 1500, TyTrong = 7.85 });
                vlFile.GhiFile(fVl);
                var vlDoc = GiaVlDon.DocFile(fVl).Tim("ZAM,A", 2.3);
                Kiem("GiaVl: khu hoi file", vlDoc.DonGia + "|" + vlDoc.GiaVat + "|" + vlDoc.TyTrong, "212.5|1500|7.85");
                Kiem("GiaVl: file co BOM", File.ReadAllBytes(fVl)[0], (byte)0xEF);
                File.WriteAllText(fVl, GiaVlDon.TieuDe + "\r\nZAM,abc\r\n");
                KiemNemBatKy("GiaVl: file hong -> bao loi, khong coi la trong", () => GiaVlDon.DocFile(fVl));
                File.WriteAllText(fVl, GiaVlDon.TieuDe + "\r\nZAM,2.3,1220,NaN,400,25,150,1800,7.85\r\n");
                KiemNemBatKy("GiaVl: file thieu thong so -> bao loi", () => GiaVlDon.DocFile(fVl));
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
