using System;

namespace CsvTool.Tests
{
    /// <summary>
    /// Test logic thuần của CsvTool (Core/). In OK / FAIL từng ca rồi tổng kết, cùng kiểu
    /// AiTool.Tests. Mã thoát 1 nếu có ca hỏng.
    /// </summary>
    internal static partial class Program
    {
        private static int _ok, _fail;

        internal static void Kiem(string ten, object duoc, object mongDoi)
        {
            bool dung = Equals(duoc, mongDoi);
            if (dung) _ok++; else _fail++;
            Console.WriteLine("  {0} {1,-62} -> {2}{3}", dung ? "OK  " : "FAIL", ten,
                              duoc ?? "null", dung ? "" : "   (mong doi " + (mongDoi ?? "null") + ")");
        }

        /// <summary>Kiểm một khối code phải ném đúng loại ngoại lệ.</summary>
        internal static void KiemNem<T>(string ten, Action a) where T : Exception
        {
            string duoc;
            try { a(); duoc = "khong nem"; }
            catch (T) { duoc = typeof(T).Name; }
            catch (Exception ex) { duoc = ex.GetType().Name; }
            Kiem(ten, duoc, typeof(T).Name);
        }

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            TestKhung();
            TestThongTinTam();
            TestPhanTichKhung();
            TestCsvXuat();
            TestCaiDat();
            TestDuLieu();
            TestHinh();
            TestVang();

            Console.WriteLine();
            Console.WriteLine("Tong: {0} OK, {1} FAIL", _ok, _fail);
            return _fail == 0 ? 0 : 1;
        }

        private static void TestKhung()
        {
            Console.WriteLine("[Khung]");
            Kiem("Khung_ChayDuoc", true, true);
        }
    }
}
