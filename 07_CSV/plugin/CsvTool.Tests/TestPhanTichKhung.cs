using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        private static TextTho Fn(string s, bool mtext = true) => new TextTho { Lop = "2-F-NAME", NoiDung = s, LaMtext = mtext };
        private static TextTho Tt(IEnumerable<string> dong) => new TextTho { Lop = "KHUNG_IN", NoiDung = string.Join("\\P", dong), LaMtext = true };
        private static DimTho Dim(int co70, double d, int dec = 0, string lop = "1-XY") => new DimTho { Lop = lop, Co70 = co70, DoDai = d, Dimdec = dec };

        private static KhungTho KhungMau(IEnumerable<TextTho> text = null, IEnumerable<DimTho> dim = null) => new KhungTho
        {
            MinX = 10, MinY = 0, MaxX = 500, MaxY = 400, Handle = 0x2A,
            Text = (text ?? new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2\\PZ=1"), Tt(MauThongTin) }).ToList(),
            Dim = (dim ?? new[] { Dim(32, 300), Dim(32, 200) }).ToList(),
        };

        private static string Loi(KhungTho k) => PhanTichKhung.Chay(k, "1-XY", 8).Loi;

        private static void TestPhanTichKhung()
        {
            Console.WriteLine("[PhanTichKhung]");

            Kiem("Rtos 320,2,8", PhanTichKhung.Rtos(320, 2, 8), "320");
            Kiem("Rtos 301.1,2,8", PhanTichKhung.Rtos(301.1, 2, 8), "301.1");
            Kiem("Rtos 1767.774,2,8", PhanTichKhung.Rtos(1767.774, 2, 8), "1767.77");
            Kiem("Rtos 320,2,0", PhanTichKhung.Rtos(320, 2, 0), "320.00");
            Kiem("Rtos 0.5,1,12 bo 0 dau", PhanTichKhung.Rtos(0.5, 1, 12), ".5");

            var ok = PhanTichKhung.Chay(KhungMau(), "1-XY", 8);
            Kiem("Chay_Dung: khong loi", ok.Loi, null);
            Kiem("Chay_Dung: N M T Q", ok.Tam == null ? "" : string.Join("|", ok.Tam.N, ok.Tam.M, ok.Tam.T, ok.Tam.Q), "A-1|SS400|3.2|2");
            Kiem("Chay_Dung: diem pointss", ok.Tam == null ? "" : ok.Tam.DiemX + "," + ok.Tam.DiemY, "10,400");
            Kiem("HaiDimThang: X Y", ok.Tam == null ? "" : ok.Tam.X + "x" + ok.Tam.Y, "300x200");
            var dao = PhanTichKhung.Chay(KhungMau(dim: new[] { Dim(160, 200), Dim(32, 300) }), "1-XY", 8);
            Kiem("HaiDimThang dao: X >= Y", dao.Tam == null ? "" : dao.Tam.X + "x" + dao.Tam.Y, "300x200");

            Kiem("KhongFname", Loi(KhungMau(text: new[] { Tt(MauThongTin) })), "Không tìm thấy F-name!!!");
            Kiem("HaiMtextFname", Loi(KhungMau(text: new[] { Fn("N=A\\PM=B\\PT=1\\PQ=1"), Fn("N=C\\PM=B\\PT=1\\PQ=1"), Tt(MauThongTin) })),
                 "Có nhiều hơn 1 Fname trong khung!!!");
            Kiem("TronMtextVaText", (Loi(KhungMau(text: new[] { Fn("N=A\\PM=B\\PT=1\\PQ=1"), Fn("Z=1", false), Tt(MauThongTin) })) ?? "")
                 .StartsWith("Khung in có các loại Fname không hợp lệ!"), true);
            Kiem("FnameTruongRong", Loi(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ="), Tt(MauThongTin) })),
                 "Lỗi Fname (2): Giá trị trường thông tin bị rỗng!");
            var txt = PhanTichKhung.Chay(KhungMau(text: new[] { Fn("N=A-2", false), Fn("M=SPHC", false), Fn("T=1.6", false), Fn("Q=3", false), Tt(MauThongTin) }), "1-XY", 8);
            Kiem("FnameText: 4 TEXT rieng", txt.Tam == null ? txt.Loi : string.Join("|", txt.Tam.N, txt.Tam.M, txt.Tam.T, txt.Tam.Q), "A-2|SPHC|1.6|3");
            Kiem("FnameText: duoi 4 TEXT -> khong thay", Loi(KhungMau(text: new[] { Fn("N=A-2", false), Fn("M=SPHC", false), Fn("Q=3", false), Tt(MauThongTin) })),
                 "Không tìm thấy F-name!!!");

            Kiem("BaDimThang", Loi(KhungMau(dim: new[] { Dim(32, 1), Dim(32, 2), Dim(32, 3) })), "Số lượng dim tổng không đúng!!!");
            var phi = PhanTichKhung.Chay(KhungMau(dim: new[] { Dim(163, 150) }), "1-XY", 8);
            Kiem("MotDimPhi: X=Y", phi.Tam == null ? phi.Loi : phi.Tam.X + "x" + phi.Tam.Y, "150x150");
            Kiem("KhongDim", Loi(KhungMau(dim: new DimTho[0])), "Không tìm thấy dim tổng, kiểm tra lại dữ liệu!!!");
            Kiem("DimSaiLop: bo qua", Loi(KhungMau(dim: new[] { Dim(32, 300, 0, "0"), Dim(32, 200, 0, "0") })), "Không tìm thấy dim tổng, kiểm tra lại dữ liệu!!!");

            var k = MauThongTin.ToList(); k[8] = "Các loại gia công: m, k";
            Kiem("VatChuaNhap", Loi(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2"), Tt(k) })),
                 "Chi tiết có gia công vát (k) nhưng chưa nhập chiều dài vát!");
            var p = MauThongTin.ToList(); p[8] = "Các loại gia công: m(pitchi)";
            Kiem("PitchiChuaNhap", Loi(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2"), Tt(p) })),
                 "Chi tiết có gia công m(pitchi) nhưng chưa nhập Pitchi - Số lần hoặc Pitchi - Chiều dài (mm)!");

            var g = MauThongTin.ToList(); g[8] = "Các loại gia công: m(pitchi), s, m"; g[4] = "Pitchi - Số lần: 2"; g[5] = "Pitchi - Chiều dài (mm): 30";
            var rg = PhanTichKhung.Chay(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2"), Tt(g) }), "1-XY", 8);
            Kiem("LoaiGiaCong: m(pitchi)->m, bo trung", rg.Tam == null ? rg.Loi : rg.Tam.Tt.LoaiGiaCong, "m, s");

            Kiem("ExportA2b: giua 2 dau phay", PhanTichKhung.ExportA2b(",N=A-1,M=SS,", "N=", ","), "A-1");
            Kiem("ExportA2b: cuoi chuoi", PhanTichKhung.ExportA2b(",Q=12", "Q=", ","), "12");
        }
    }
}
