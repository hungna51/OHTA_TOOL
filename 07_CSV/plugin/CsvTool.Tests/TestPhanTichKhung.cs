using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        private static TextTho Fn(string s, bool mtext = true) => new TextTho { Lop = "2-F-NAME", NoiDung = s, LaMtext = mtext };
        private static DimTho Dim(int co70, double d, int dec = 0, string lop = "1-XY") => new DimTho { Lop = lop, Co70 = co70, DoDai = d, Dimdec = dec };

        private static KhungTho KhungMau(IEnumerable<TextTho> text = null, IEnumerable<DimTho> dim = null) => new KhungTho
        {
            MinX = 10, MinY = 0, MaxX = 500, MaxY = 400, Handle = 0x2A,
            Text = (text ?? new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2\\PZ=1") }).ToList(),
            Dim = (dim ?? new[] { Dim(32, 300), Dim(32, 200) }).ToList(),
        };

        private static string Loi(KhungTho k) => PhanTichKhung.DocFnameDim(k, "1-XY", 8).Loi;

        private static void TestPhanTichKhung()
        {
            Console.WriteLine("[PhanTichKhung]");

            Kiem("Rtos 320,2,8", PhanTichKhung.Rtos(320, 2, 8), "320");
            Kiem("Rtos 301.1,2,8", PhanTichKhung.Rtos(301.1, 2, 8), "301.1");
            Kiem("Rtos 1767.774,2,8", PhanTichKhung.Rtos(1767.774, 2, 8), "1767.77");
            Kiem("Rtos 320,2,0", PhanTichKhung.Rtos(320, 2, 0), "320.00");
            Kiem("Rtos 0.5,1,12 bo 0 dau", PhanTichKhung.Rtos(0.5, 1, 12), ".5");

            var ok = PhanTichKhung.DocFnameDim(KhungMau(), "1-XY", 8);
            Kiem("Chay_Dung: khong loi", ok.Loi, null);
            Kiem("Chay_Dung: N M T Q", ok.Tam == null ? "" : string.Join("|", ok.Tam.N, ok.Tam.M, ok.Tam.T, ok.Tam.Q), "A-1|SS400|3.2|2");
            Kiem("Chay_Dung: diem pointss", ok.Tam == null ? "" : ok.Tam.DiemX + "," + ok.Tam.DiemY, "10,400");
            Kiem("HaiDimThang: X Y", ok.Tam == null ? "" : ok.Tam.X + "x" + ok.Tam.Y, "300x200");
            var dao = PhanTichKhung.DocFnameDim(KhungMau(dim: new[] { Dim(160, 200), Dim(32, 300) }), "1-XY", 8);
            Kiem("HaiDimThang dao: X >= Y", dao.Tam == null ? "" : dao.Tam.X + "x" + dao.Tam.Y, "300x200");

            DimTho Canh(double d, double dx, double dy, int dec = 0) { var x = Dim(33, d, dec); x.DX = dx; x.DY = dy; return x; }
            var canh = PhanTichKhung.DocFnameDim(KhungMau(dim: new[] { Canh(20, 0, 20), Canh(1015.26, 1015.26, 0, 2) }), "1-XY", 8);
            Kiem("HaiDimAligned: X Y", canh.Tam == null ? canh.Loi : canh.Tam.X + "x" + canh.Tam.Y, "1015.26x20");
            var uuTien = PhanTichKhung.DocFnameDim(KhungMau(dim: new[] { Dim(32, 300), Dim(32, 200), Canh(50, 30, 40) }), "1-XY", 8);
            Kiem("Dim thang uu tien hon Aligned", uuTien.Tam == null ? uuTien.Loi : uuTien.Tam.X + "x" + uuTien.Tam.Y, "300x200");
            var phiCanh = PhanTichKhung.DocFnameDim(KhungMau(dim: new[] { Dim(3, 90), Canh(50, 50, 0) }), "1-XY", 8);
            Kiem("HaiDimAligned xien: bao loi", Loi(KhungMau(dim: new[] { Canh(20, 0, 20), Canh(1015.26, 800, 625) })),
                 "2 dim Aligned không theo phương X và Y (cần 1 dim ngang, 1 dim đứng).");
            Kiem("HaiDimAligned cung ngang: bao loi", Loi(KhungMau(dim: new[] { Canh(20, 20, 0), Canh(1015.26, -1015.26, 0) })),
                 "2 dim Aligned không theo phương X và Y (cần 1 dim ngang, 1 dim đứng).");
            Kiem("Dim phi khong bi 1 Aligned lan", phiCanh.Tam == null ? phiCanh.Loi : phiCanh.Tam.X + "x" + phiCanh.Tam.Y, "90x90");

            Kiem("KhongFname", Loi(KhungMau(text: new TextTho[0])), "Không tìm thấy F-name.");
            Kiem("HaiMtextFname", Loi(KhungMau(text: new[] { Fn("N=A\\PM=B\\PT=1\\PQ=1"), Fn("N=C\\PM=B\\PT=1\\PQ=1") })),
                 "Khung có nhiều hơn 1 F-name.");
            Kiem("TronMtextVaText", (Loi(KhungMau(text: new[] { Fn("N=A\\PM=B\\PT=1\\PQ=1"), Fn("Z=1", false) })) ?? "")
                 .StartsWith("F-name trộn TEXT và MTEXT."), true);
            Kiem("FnameTruongRong", Loi(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=") })),
                 "F-name có trường bị trống.");
            var txt = PhanTichKhung.DocFnameDim(KhungMau(text: new[] { Fn("N=A-2", false), Fn("M=SPHC", false), Fn("T=1.6", false), Fn("Q=3", false) }), "1-XY", 8);
            Kiem("FnameText: 4 TEXT rieng", txt.Tam == null ? txt.Loi : string.Join("|", txt.Tam.N, txt.Tam.M, txt.Tam.T, txt.Tam.Q), "A-2|SPHC|1.6|3");
            Kiem("FnameText: duoi 4 TEXT -> khong thay", Loi(KhungMau(text: new[] { Fn("N=A-2", false), Fn("M=SPHC", false), Fn("Q=3", false) })),
                 "Không tìm thấy F-name.");

            Kiem("BaDimThang", Loi(KhungMau(dim: new[] { Dim(32, 1), Dim(32, 2), Dim(32, 3) })), "Sai số dim tổng (cần 2 dim, hoặc 1 dim phi).");
            var phi = PhanTichKhung.DocFnameDim(KhungMau(dim: new[] { Dim(163, 150) }), "1-XY", 8);
            Kiem("MotDimPhi: X=Y", phi.Tam == null ? phi.Loi : phi.Tam.X + "x" + phi.Tam.Y, "150x150");
            Kiem("KhongDim", Loi(KhungMau(dim: new DimTho[0])), "Không tìm thấy dim tổng.");
            Kiem("DimSaiLop: bo qua", Loi(KhungMau(dim: new[] { Dim(32, 300, 0, "0"), Dim(32, 200, 0, "0") })), "Không tìm thấy dim tổng.");

            Kiem("VatChuaNhap", PhanTichKhung.KiemTra(new ThongTinTam { LoaiGiaCong = "m, k" }), "Có gia công vát (k), chưa nhập chiều dài vát.");
            Kiem("PitchiChuaNhap", PhanTichKhung.KiemTra(new ThongTinTam { LoaiGiaCong = "m(pitchi)" }), "Có gia công pitchi, chưa nhập số lần hoặc chiều dài.");
            Kiem("LoaiGiaCong: m(pitchi)->m, bo trung", PhanTichKhung.ChuanHoaGiaCong("m(pitchi), s, m"), "m, s");

            Kiem("ExportA2b: giua 2 dau phay", PhanTichKhung.ExportA2b(",N=A-1,M=SS,", "N=", ","), "A-1");
            Kiem("ExportA2b: cuoi chuoi", PhanTichKhung.ExportA2b(",Q=12", "Q=", ","), "12");

            Kiem("ThayTruongFname Mtext Q", PhanTichKhung.ThayTruongFname("N=A-1\\PM=SS400\\PT=3.2\\PQ=2\\PZ=1", true, 'Q', "5"),
                 "N=A-1\\PM=SS400\\PT=3.2\\PQ=5\\PZ=1");
            Kiem("ThayTruongFname Mtext boc {..}", PhanTichKhung.ThayTruongFname("{\\fArial|b0;N=A-1\\PQ=2}", true, 'N', "B-7"),
                 "{\\fArial|b0;N=B-7\\PQ=2}");
            Kiem("ThayTruongFname Mtext cuoi boc {..}", PhanTichKhung.ThayTruongFname("{\\fArial|b0;N=A-1\\PQ=2}", true, 'Q', "9"),
                 "{\\fArial|b0;N=A-1\\PQ=9}");
            Kiem("ThayTruongFname Text", PhanTichKhung.ThayTruongFname("T=1.6", false, 'T', "2.3"), "T=2.3");
            Kiem("ThayTruongFname Text khac truong", PhanTichKhung.ThayTruongFname("M=SPHC", false, 'T', "2.3"), null);
            Kiem("ThayTruongFname khong co", PhanTichKhung.ThayTruongFname("N=A\\PM=B", true, 'Q', "1"), null);
        }
    }
}
