using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        private static void TestFileA()
        {
            Console.WriteLine("[FileA]");
            List<(double X, double Y)> Hcn(double x1, double y1, double x2, double y2) => new List<(double, double)> { (x1, y1), (x2, y1), (x2, y2), (x1, y2) };
            var tam = Hcn(0, 0, 1000, 200);   // Y = 200 → cụm F-name cao 20
            var cho = FnameTrongTam.TimCho(tam, null, 40, 50, 200);   // F-name gốc rộng 40 cao 50
            Kiem("FnameA: lan 1 cao Y/10, sat goc tren-trai", cho == null ? "null" : cho.Cao + "|" + Math.Round(cho.X, 2) + "|" + Math.Round(cho.Y + cho.Cao, 2) + "|" + cho.TiLe, "20|3|197|0.4");

            var loGiua = new List<List<(double X, double Y)>> { Hcn(0.5, 0.5, 999.5, 199.5) };   // lỗ chiếm gần hết tấm
            Kiem("FnameA: khong co phan thit -> null", FnameTrongTam.TimCho(tam, loGiua, 40, 50, 200) == null, true);

            // tấm chữ U: dải trên cao 15 không đủ chỗ cho 20 → thu nhỏ còn 10
            var chuU = new List<(double X, double Y)> { (0, 0), (300, 0), (300, 200), (0, 200) };
            var loU = new List<List<(double X, double Y)>> { Hcn(2, 2, 298, 185) };
            var choU = FnameTrongTam.TimCho(chuU, loU, 40, 50, 200);
            Kiem("FnameA: khong vua thi giam 1/2", choU == null ? "null" : choU.Cao.ToString(), "10");
            Kiem("FnameA: khong cham lo", choU != null && choU.Y >= 185, true);

            Kiem("FnameA: tam qua nho sau 5 lan -> null", FnameTrongTam.TimCho(Hcn(0, 0, 0.5, 200), null, 40, 50, 200) == null, true);

            var d = new DuLieuHinh { HandleKhung = 9 };
            d.NetPhu.Add(("4-SHT", new List<(double, double)> { (0, 100), (50, 100) }, (0, 100, 50, 100)));   // スリット từ biên vào trong
            Kiem("NetPhu: スリット bat dau tren bien khong loi", PhanTichHinh.NetPhuNgoaiHinh(d, tam, 0.01).Count, 0);
            d.NetPhu.Add(("3-KEGAKI", new List<(double, double)> { (1100, 50), (1200, 50) }, (1100, 50, 1200, 50)));
            var loiPhu = PhanTichHinh.NetPhuNgoaiHinh(d, tam, 0.01);
            Kiem("NetPhu: ケガキ ngoai hinh chinh -> loi", loiPhu.Count + "|" + loiPhu[0].LaLoi + "|" + loiPhu[0].NoiDung.StartsWith("Nét 3-KEGAKI (ケガキ)"), "1|True|True");

            string mt = "{\\fGulim|b0|i0|c129|p0;N=131311-1\\PM=SUS30}{\\fArial|b0|i0|c0|p34;\\C3;4 No.1}\\P\\H2.5x;T=4.0\\PV=25-3.7(90%%d)\\PZ=SUS-N2";
            Kiem("MtextPhang: bo ma giua dong, giu font dau", ThongTinTamParser.MtextPhang(mt),
                 "{\\fGulim|b0|i0|c129|p0;N=131311-1\\PM=SUS304 No.1\\PT=4.0\\PV=25-3.7(90%%d)\\PZ=SUS-N2}");
            Kiem("MtextPhang: tach dong van dung", string.Join("|", ThongTinTamParser.TachDong(ThongTinTamParser.MtextPhang(mt), true)),
                 "N=131311-1|M=SUS304 No.1|T=4.0|V=25-3.7(90%%d)|Z=SUS-N2");
            var kMt = new KhungTho { Text = new List<TextTho> { new TextTho { Lop = "2-F-NAME", LaMtext = true, NoiDung = mt.Replace("\\PZ", "\\PQ=5\\PZ") } },
                                     Dim = new List<DimTho> { new DimTho { Lop = "1-XY", Co70 = 32, DoDai = 300 }, new DimTho { Lop = "1-XY", Co70 = 32, DoDai = 200 } } };
            var tMt = PhanTichKhung.DocFnameDim(kMt, "1-XY", 8).Tam;
            Kiem("Quet F-name MTEXT doi font giua dong: ten vat lieu dung", tMt == null ? "null" : tMt.M + "|" + tMt.T + "|" + tMt.Q, "SUS304 No.1|4.0|5");
            Kiem("MtextPhang: giu \\U+ va ngoac thoat", ThongTinTamParser.MtextPhang("M=SS400 \\U+7E1E\\{x\\}"), "M=SS400 \\U+7E1E\\{x\\}");
            var fnS = new[] { "N=131311-1", "M=SUS304 No.1", "T=4.0", "Q=5", "V=25-3.7(90%%d)", "Z=SUS-N2" };
            Kiem("SoDongFname: khop (khac thu tu, %%d = do)", PhanTichKhung.SoDongFname(fnS, new[] { "Z=SUS-N2", "N=131311-1", "M=SUS304 No.1", "T=4.0", "Q=5", "V=25-3.7(90°)" }), null);
            Kiem("SoDongFname: vat lieu tach 2 dong", PhanTichKhung.SoDongFname(fnS, new[] { "N=131311-1", "M=SUS304", "No.1", "T=4.0", "Q=5", "V=25-3.7(90°)", "Z=SUS-N2" }),
                 "thiếu \"M=SUS304 NO.1\", thừa \"M=SUS304\", \"NO.1\".");
            var gc = new ThongTinTam(); gc.DuongBe.Add((100, 1));
            Kiem("In PDF: tam be la tam gia cong", PhanTichKhung.LaTamGiaCong(gc), true);
        }
    }
}
