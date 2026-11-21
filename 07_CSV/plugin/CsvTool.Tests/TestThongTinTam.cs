using System;
using CsvTool.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        private static void TestThongTinTam()
        {
            Console.WriteLine("[ThongTinTam]");
            string mt = "{\\fArial|b0;Lỗ nhỏ: 4\\PLỗ lớn: 1}";
            Kiem("TachDong_Mtext: bo {\\f..; } va tach \\P",
                 string.Join("|", ThongTinTamParser.TachDong(mt, true)), "Lỗ nhỏ: 4|Lỗ lớn: 1");
            Kiem("TachDong_XuongDong: bo \\r", string.Join("|", ThongTinTamParser.TachDong("A: 1\r\nB: 2", true)), "A: 1|B: 2");
            Kiem("ChuanHoa: giai ma \\U+, bo dau, viet hoa", ThongTinTamParser.ChuanHoa("v\\U+00E1t Đường"), "VAT DUONG");
            Kiem("Atoi: so dau chuoi", ThongTinTamParser.Atoi("12 x"), 12);
            Kiem("Atoi: khong co so", ThongTinTamParser.Atoi("x"), 0);
        }
    }
}
