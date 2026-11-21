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
