using System;
using System.Collections.Generic;
using System.Linq;
using AiTool.Core;

namespace AiTool.Tests
{
    internal static partial class Program
    {
        private static DoiTuong Chu(string layer, double x, double y, string noiDung, double h = 3.5) => new DoiTuong
        {
            Loai = LoaiDoiTuong.Text, Layer = layer, X1 = x, Y1 = y, NoiDung = noiDung, ChieuCao = h,
            MinX = x, MinY = y, MaxX = x + 10, MaxY = y + h
        };

        private static KhungDuLieu KhungMau(params DoiTuong[] ds) => new KhungDuLieu
        {
            SoThuTu = 1, MinX = 0, MinY = 0, MaxX = 420, MaxY = 297, DoiTuong = ds.ToList()
        };

        private static string[] Dong(string s) => s.Replace("\r", "").Split('\n');

        private static void TestDongChu()
        {
            Console.WriteLine("=== DongChu ===");
            string k = DongChu.Khung(KhungMau(Line("0-KAKOU", 10, 10, 50, 10)));
            Kiem("Tieu de khung", Dong(k)[0], "#KHUNG 1 rong=420 cao=297");
            Kiem("Line dinh dang", Dong(k).Contains("L 0-KAKOU 10,10 50,10"), true);
            Kiem("Khong co F-name", Dong(k).Contains("FNAME: (khong co)"), true);

            var tron = new DoiTuong { Loai = LoaiDoiTuong.Circle, Layer = "0-KAKOU", Cx = 20, Cy = 30, R = 2.25, MinX = 17.75, MinY = 27.75, MaxX = 22.25, MaxY = 32.25 };
            Kiem("Circle ghi duong kinh", Dong(DongChu.Khung(KhungMau(tron))).Contains("C 0-KAKOU 20,30 d=4.5"), true);

            var cung = new DoiTuong { Loai = LoaiDoiTuong.Arc, Layer = "0-KAKOU", Cx = 5, Cy = 5, R = 3, GocDau = 0, GocCuoi = 90, MinX = 5, MinY = 5, MaxX = 8, MaxY = 8 };
            Kiem("Arc dinh dang", Dong(DongChu.Khung(KhungMau(cung))).Contains("A 0-KAKOU 5,5 r=3 0->90"), true);

            var pl = new DoiTuong
            {
                Loai = LoaiDoiTuong.Polyline, Layer = "0-KAKOU", Kin = false, MinX = 0, MinY = 0, MaxX = 20, MaxY = 10,
                Doan = new List<DoanPl>
                {
                    new DoanPl { X1 = 0, Y1 = 0, X2 = 10, Y2 = 0 },
                    new DoanPl { X1 = 10, Y1 = 0, X2 = 20, Y2 = 0, LaCung = true, Cx = 15, Cy = 0, R = 5 },
                }
            };
            Kiem("Polyline co cung", Dong(DongChu.Khung(KhungMau(pl))).Contains("P 0-KAKOU ho 0,0 10,0 ~15,0 r=5 20,0"), true);

            var dim = new DoiTuong { Loai = LoaiDoiTuong.Dim, Layer = "1-SUNPOU", LoaiDim = "RotatedDimension", GiaTriDo = 120.004, GhiDe = "120±0.1", X1 = 0, Y1 = 0, X2 = 120, Y2 = 0, MinX = 0, MinY = 0, MaxX = 120, MaxY = 5 };
            Kiem("Dim co ghi de", Dong(DongChu.Khung(KhungMau(dim))).Contains("D 1-SUNPOU RotatedDimension do=120 ghide=\"120±0.1\" 0,0 120,0"), true);
            dim.GhiDe = null;
            Kiem("Dim khong ghi de", Dong(DongChu.Khung(KhungMau(dim))).Contains("D 1-SUNPOU RotatedDimension do=120 0,0 120,0"), true);

            string mtext = DongChu.Khung(KhungMau(Chu("3-KEGAKI", 1, 2, "{\\fMS Gothic;\\U+677F\\U+539A}\\Pt=2.3")));
            Kiem("DongChu_MTextMaDinhDang_LamSach", Dong(mtext).Contains("T 3-KEGAKI 1,2 h=3.5 \"板厚 / t=2.3\""), true);

            string nhay = DongChu.Khung(KhungMau(Chu("0", 0, 0, "a \"b\"")));
            Kiem("Nhay kep trong chu duoc thoat", Dong(nhay).Contains("T 0 0,0 h=3.5 \"a \\\"b\\\"\""), true);

            var fn = KhungMau(
                Chu("2-F-NAME", 0, 50, "N=A-1"), Chu("2-F-NAME", 0, 45, "M=SUS304 2B"), Chu("2-F-NAME", 0, 40, "T=1.5"),
                Chu("2-F-NAME", 0, 35, "Q=2"), Chu("2-F-NAME", 0, 30, "V=10-引代(90°)"), Chu("2-F-NAME", 0, 25, "Z=SUS-N2"));
            string[] dfn = Dong(DongChu.Khung(fn));
            Kiem("FNAME: cac dong theo Y giam", dfn.Contains("FNAME: N=A-1 | M=SUS304 2B | T=1.5 | Q=2 | V=10-引代(90°) | Z=SUS-N2"), true);
            Kiem("FNAME_TACH khi dung 1 dong N=", dfn.Contains("FNAME_TACH: N=A-1; M=SUS304 2B; T=1.5; Q=2; Z=SUS-N2; BE=V=10-引代(90°)"), true);
            Kiem("F-name khong lap thanh dong T", dfn.Count(d => d.StartsWith("T 2-F-NAME")), 0);
            Kiem("TenKhung = N= kem so khung (spec 4.11)", DongChu.TenKhung(fn), "N=A-1 (khung 1)");

            var hai = KhungMau(Chu("2-F-NAME", 0, 50, "N=A-1"), Chu("2-F-NAME", 0, 45, "M=SS400"),
                               Chu("2-F-NAME", 200, 50, "N=A-2"), Chu("2-F-NAME", 200, 45, "M=SPCC"));
            string[] dhai = Dong(DongChu.Khung(hai));
            Kiem("DongChu_KhungHaiCumN_KhongTachTruong", dhai.Any(d => d.StartsWith("FNAME_TACH")), false);
            Kiem("Hai cum: KHONG tron thanh 1 dong FNAME", dhai.Any(d => d.Contains("N=A-1 | N=A-2")), false);
            Kiem("Hai cum: FNAME bao nhieu cum", dhai.Contains("FNAME: (2 cum N=, xem cac dong T 2-F-NAME theo vi tri)"), true);
            Kiem("Hai cum: tung dong F-name giu vi tri", dhai.Contains("T 2-F-NAME 200,50 h=3.5 \"N=A-2\""), true);
            Kiem("Khung 2 cum N=: TenKhung = Khung 1", DongChu.TenKhung(hai), "Khung 1");
            Kiem("F-name 1 MText co \\P: van tach",
                 DongChu.TenKhung(KhungMau(Chu("2-f-name", 0, 0, "N=B-7\\PM=SS400"))), "N=B-7 (khung 1)");

            var lo = new List<KhungDuLieu> { KhungMau(), KhungMau() };
            lo[1].SoThuTu = 2;
            string chuLo = DongChu.Lo(lo);
            Kiem("Lo: 2 khung, cach nhau dong trong", chuLo.Contains("\n\n#KHUNG 2 "), true);
            Console.WriteLine();
        }
    }
}
