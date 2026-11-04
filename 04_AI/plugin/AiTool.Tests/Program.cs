using System;

namespace AiTool.Tests
{
    /// <summary>
    /// Test logic thuần của AiTool (Core/). In OK / FAIL từng ca rồi tổng kết, cùng kiểu
    /// GroTool.Tests. Mã thoát 1 nếu có ca hỏng.
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

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            TestHinhHoc();
            TestGomKhung();
            TestDongChu();
            TestDuLieuTinh();

            Console.WriteLine();
            Console.WriteLine("Tong: {0} OK, {1} FAIL", _ok, _fail);
            return _fail == 0 ? 0 : 1;
        }
    }
}
