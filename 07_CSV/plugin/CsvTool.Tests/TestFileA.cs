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

            var gc = new ThongTinTam(); gc.DuongBe.Add((100, 1));
            Kiem("In PDF: tam be la tam gia cong", PhanTichKhung.LaTamGiaCong(gc), true);
        }
    }
}
