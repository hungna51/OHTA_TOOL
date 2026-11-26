using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvTool.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        private static TamXuat Tam(string n, double x = 0, double y = 0, string gc = "s", string q = "2", string m = "SUS304 No.1", string t = "3",
                                   string dx = "1500", string dy = "1000") => new TamXuat
        {
            N = n, Q = q, M = m, T = t, X = dx, Y = dy, DiemX = x, DiemY = y,
            Tt = new ThongTinTam { LoaiGiaCong = gc },
        };

        private static void TestCsvXuat()
        {
            Console.WriteLine("[CsvXuat]");

            Kiem("MaDon 129213-s.dxf", MaDon.TuTenFile("129213-s.dxf"), "129213");
            Kiem("MaDon 100554-2-a.dwg", MaDon.TuTenFile("100554-2-a.dwg"), "100554-2");
            Kiem("MaDon 1234-A2a.dwg", MaDon.TuTenFile("1234-A2a.dwg"), "1234-A2a");
            Kiem("MaDon 131102.dwg", MaDon.TuTenFile("131102.dwg"), "131102");
            Kiem("MaDon X-S.DWG", MaDon.TuTenFile("X-S.DWG"), "X");

            Kiem("MaMage m", CsvXuat.MaMage("m"), "m");
            Kiem("MaMage m(pitchi)", CsvXuat.MaMage("m(pitchi)"), "m");
            Kiem("MaMage s", CsvXuat.MaMage("s"), "s");
            Kiem("MaMage r1", CsvXuat.MaMage("r1"), "r");
            Kiem("MaMage m, t", CsvXuat.MaMage("m, t"), "*");
            Kiem("MaMage m, m(pitchi)", CsvXuat.MaMage("m, m(pitchi)"), "m");
            Kiem("MaMage r1, r2", CsvXuat.MaMage("r1, r2"), "r");
            Kiem("MaMage rong", CsvXuat.MaMage(""), "");
            Kiem("LoaiCuon r2", CsvXuat.LoaiCuon("m, r2"), "2");
            Kiem("LoaiCuon khong", CsvXuat.LoaiCuon("m"), "0");

            Kiem("O co phay", CsvXuat.O("m, t"), "\"m, t\"");
            Kiem("O co nhay", CsvXuat.O("a\"b"), "\"a\"\"b\"");
            Kiem("O thuong", CsvXuat.O("SS400"), "SS400");

            Kiem("Dong10 131102-2", CsvXuat.Dong10(Tam("131102-2"), "131102"), "131102-2,2,SUS304 No.1,3,131102-,s,1500,1000,,");

            var abc = new TamXuat
            {
                N = "ABC-01", M = "SPCC", T = "1.6", Q = "2", X = "300", Y = "200",
                Tt = new ThongTinTam
                {
                    LoNho = 4, LoLon = 1, Tappu = 2, Sara = 0, PitchiSoLan = 0, PitchiDai = 0, Vat = 150,
                    LoaiGiaCong = "m, t", DuongBe = new List<(int, int)> { (150, 2), (80, 1) },
                    CheDoTinh = "0", GiaBaoGia = "", TienVatLieu = "",
                },
            };
            Kiem("Dong34: bo cot cuon + uu dai, them gia tay",
                 CsvXuat.Dong34(abc),
                 "ABC-01,SPCC,1.6,300,200,2,4,1,2,0,0,0,\"m, t\",150,2,150,1,80,0,0,0,0,0,0,0,0,0,0,0,0,0,,,0");
            Kiem("TieuDe34 du 34 cot", CsvXuat.TieuDe34.Split(',').Length, 34);
            var nhieuBe = abc.Tt.DuongBe; abc.Tt.DuongBe = Enumerable.Range(1, 9).Select(i => (i * 10, 1)).ToList();
            Kiem("Dong34 qua 8 duong be -> 34 cot", CsvXuat.Dong34(abc).Split(',').Length, 35); // +1 vì "m, t" có dấu phẩy trong nháy
            abc.Tt.DuongBe = nhieuBe;

            var ds = new[] { Tam("A-10", 30, 5), Tam("A-2", 10, 50), Tam("A-1", 20, 20) };
            Kiem("SapXep L->R", string.Join(" ", CsvXuat.SapXep(ds, KieuSapXep.TraiSangPhai).Select(x => x.N)), "A-2 A-1 A-10");
            Kiem("SapXep Tren->Duoi", string.Join(" ", CsvXuat.SapXep(ds, KieuSapXep.TrenXuongDuoi).Select(x => x.N)), "A-2 A-1 A-10");
            Kiem("SapXep ChuCai", string.Join(" ", CsvXuat.SapXep(ds, KieuSapXep.ChuCai).Select(x => x.N)), "A-1 A-2 A-10");
            Kiem("TenTrung", CsvXuat.TenTrung(new[] { Tam("A"), Tam("B"), Tam("A") }), "A");
            Kiem("TenTrung khong", CsvXuat.TenTrung(ds), null);

            string thu = Path.Combine(Path.GetTempPath(), "csvtool_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(thu);
            try
            {
                var hai = new List<TamXuat> { Tam("131102-1"), Tam("131102-2") };
                foreach (var x in hai) x.Tt.DuongBe = new List<(int, int)>();
                CsvXuat.GhiHaiFile(thu, "131102", hai);
                string f10 = Path.Combine(thu, "131102.csv"), f35 = Path.Combine(thu, "data", "131102-data.csv");
                byte[] b = File.ReadAllBytes(f10);
                Kiem("GhiHaiFile_Dung: co 2 file", File.Exists(f10) && File.Exists(f35), true);
                Kiem("GhiHaiFile_Dung: BOM", b.Length > 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF, true);
                string noi = Encoding.UTF8.GetString(b, 3, b.Length - 3);
                Kiem("GhiHaiFile_Dung: noi dung + CRLF", noi,
                     CsvXuat.TieuDe10 + "\r\n131102-1,2,SUS304 No.1,3,131102-,s,1500,1000,,\r\n131102-2,2,SUS304 No.1,3,131102-,s,1500,1000,,\r\n");
                Kiem("GhiHaiFile_Dung: data dong 1 la tieu de 34", File.ReadAllLines(f35)[0], CsvXuat.TieuDe34);

                string cu = File.ReadAllText(f10);
                using (new FileStream(f35, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    KiemNem<IOException>("GhiHaiFile_KhoaFile_KhongGhiGi: nem IOException",
                        () => CsvXuat.GhiHaiFile(thu, "131102", new List<TamXuat> { Tam("131102-9") }));
                }
                Kiem("GhiHaiFile_KhoaFile_KhongGhiGi: file 10 cot khong doi", File.ReadAllText(f10), cu);
                KiemNem<InvalidOperationException>("GhiHaiFile_ThuMucRong", () => CsvXuat.GhiHaiFile("", "131102", hai));
            }
            finally { Directory.Delete(thu, true); }
        }
    }
}
