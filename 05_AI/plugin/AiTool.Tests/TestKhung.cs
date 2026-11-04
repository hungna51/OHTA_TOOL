using System;
using System.Collections.Generic;
using System.Linq;
using AiTool.Core;

namespace AiTool.Tests
{
    internal static partial class Program
    {
        private static DoiTuong Line(string layer, double x1, double y1, double x2, double y2) => new DoiTuong
        {
            Loai = LoaiDoiTuong.Line, Layer = layer, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            MinX = Math.Min(x1, x2), MinY = Math.Min(y1, y2), MaxX = Math.Max(x1, x2), MaxY = Math.Max(y1, y2)
        };

        private static void TestHinhHoc()
        {
            Console.WriteLine("=== HinhHoc: lam tron, bulge ===");
            Kiem("Tron(1.005) = 1.01", HinhHoc.Tron(1.005), "1.01");
            Kiem("Tron(2.0) = 2", HinhHoc.Tron(2.0), "2");
            Kiem("Tron(-0.001) = 0 (khong -0)", HinhHoc.Tron(-0.001), "0");
            Kiem("Tron(-3.456) = -3.46", HinhHoc.Tron(-3.456), "-3.46");

            DoanPl d1 = HinhHoc.TuBulge(0, 0, 10, 0, 1);
            Kiem("Bulge 1: la cung", d1.LaCung, true);
            Kiem("Bulge 1: tam (5,0) R=5", HinhHoc.Tron(d1.Cx) + "," + HinhHoc.Tron(d1.Cy) + " R" + HinhHoc.Tron(d1.R), "5,0 R5");
            DoanPl d2 = HinhHoc.TuBulge(0, 0, 10, 0, -1);
            Kiem("Bulge -1: tam (5,0) R=5", HinhHoc.Tron(d2.Cx) + "," + HinhHoc.Tron(d2.Cy) + " R" + HinhHoc.Tron(d2.R), "5,0 R5");
            DoanPl d3 = HinhHoc.TuBulge(0, 0, 10, 0, 0.414213562);
            Kiem("Bulge +tan(22.5) (1/4 vong, CCW, cung vong xuong): tam (5,5) R=7.07",
                 HinhHoc.Tron(d3.Cx) + "," + HinhHoc.Tron(d3.Cy) + " R" + HinhHoc.Tron(d3.R), "5,5 R7.07");
            DoanPl d4 = HinhHoc.TuBulge(0, 0, 10, 0, -0.414213562);
            Kiem("Bulge -tan(22.5): tam (5,-5)", HinhHoc.Tron(d4.Cx) + "," + HinhHoc.Tron(d4.Cy), "5,-5");
            Kiem("Bulge 0: doan thang", HinhHoc.TuBulge(0, 0, 10, 0, 0).LaCung, false);
            Console.WriteLine();
        }

        private static void TestGomKhung()
        {
            Console.WriteLine("=== GomKhung ===");
            var khung = new List<(double, double, double, double)> { (100, 200, 500, 400) };
            var ds = new List<DoiTuong> { Line("0-KAKOU", 110, 210, 150, 210), Line("0-KAKOU", 600, 210, 650, 210) };
            List<KhungDuLieu> kq = GomKhung.Gom(khung, ds);
            Kiem("Gom: 1 khung", kq.Count, 1);
            Kiem("Gom: ngoai khung bi bo -> 1 doi tuong", kq[0].DoiTuong.Count, 1);
            DoiTuong l = kq[0].DoiTuong[0];
            Kiem("Gom: doi goc X1,Y1 = 10,10", l.X1 + "," + l.Y1, "10,10");
            Kiem("Gom: doi goc X2 = 50", l.X2, 50.0);
            Kiem("Gom: doi goc hop bao MinX = 10", l.MinX, 10.0);
            Kiem("Gom: KHONG sua doi tuong goc", ds[0].X1, 110.0);
            Kiem("Gom: khung giu toa do goc MinX=100", kq[0].MinX, 100.0);

            var pl = new DoiTuong
            {
                Loai = LoaiDoiTuong.Polyline, Layer = "0-KAKOU", MinX = 120, MinY = 220, MaxX = 140, MaxY = 240,
                Doan = new List<DoanPl> { new DoanPl { X1 = 120, Y1 = 220, X2 = 140, Y2 = 240, LaCung = true, Cx = 130, Cy = 230, R = 14 } }
            };
            DoanPl dp = GomKhung.Gom(khung, new List<DoiTuong> { pl })[0].DoiTuong[0].Doan[0];
            Kiem("Gom: doi goc ca Doan va tam cung", dp.X1 + "," + dp.Y1 + " c" + dp.Cx + "," + dp.Cy, "20,20 c30,30");
            Kiem("Gom: KHONG sua Doan goc", pl.Doan[0].X1, 120.0);

            var chong = new List<(double, double, double, double)> { (0, 0, 100, 100), (50, 0, 150, 100) };
            var giua = Line("0-KAKOU", 70, 50, 80, 50);
            List<KhungDuLieu> kq2 = GomKhung.Gom(chong, new List<DoiTuong> { giua });
            Kiem("Khung chong nhau: doi tuong vao ca hai", kq2.Count(k => k.DoiTuong.Count == 1), 2);

            var canh = Line("0-KAKOU", 90, 100, 110, 100); // tam (100,100) nam dung goc khung 1
            Kiem("Tam nam tren canh khung: van tinh la trong",
                 GomKhung.Gom(new List<(double, double, double, double)> { (0, 0, 100, 100) }, new List<DoiTuong> { canh })[0].DoiTuong.Count, 1);

            // Thu tu: tren xuong (MaxY giam), roi trai sang (MinX tang)
            var nhieu = new List<(double, double, double, double)>
            {
                (200, 0, 300, 100),     // hang duoi, phai
                (0, 0, 100, 100),       // hang duoi, trai
                (200, 200, 300, 300),   // hang tren, phai
                (0, 200, 100, 300),     // hang tren, trai
            };
            List<KhungDuLieu> kq3 = GomKhung.Gom(nhieu, new List<DoiTuong>());
            string thuTu = string.Join(" ", kq3.OrderBy(k => k.SoThuTu).Select(k => k.MinX + "/" + k.MinY));
            Kiem("Thu tu khung: tren xuong, trai sang", thuTu, "0/200 200/200 0/0 200/0");
            Kiem("So thu tu bat dau tu 1", kq3.Min(k => k.SoThuTu), 1);

            // Khung vien ban ve (cung layer) bao trum cac khung tam -> bo, khong gui ca ban ve thanh 1 khung
            var vien = new List<(double, double, double, double)> { (0, 0, 1000, 1000), (10, 10, 100, 100), (200, 10, 300, 100) };
            List<KhungDuLieu> kq4 = GomKhung.Gom(vien, new List<DoiTuong> { Line("0-KAKOU", 20, 20, 30, 20) });
            Kiem("Khung bao trum khung khac bi bo -> con 2", kq4.Count, 2);
            Kiem("Khung bi bo khong lay doi tuong", kq4.Sum(k => k.DoiTuong.Count), 1);
            Console.WriteLine();
        }
    }
}
