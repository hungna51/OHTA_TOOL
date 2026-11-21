using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvTool.Core;

namespace CsvTool.Tests
{
    /// <summary>Phân tích hình học (thay lisp B1): trường hợp dựng tay + đối chiếu CSV lisp của đơn thật.</summary>
    internal static partial class Program
    {
        private static NetCat Duong(double x1, double y1, double x2, double y2) => new NetCat
        {
            Loai = "LINE", Diem = { (x1, y1), (x2, y2) },
            DoDai = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)),
        };

        private static NetCat Tron(double x, double y, double r) => new NetCat
        {
            Loai = "CIRCLE", Kin = true, DoDai = 2 * Math.PI * r, Diem = LamPhang.Cung(x, y, r, 0, 2 * Math.PI),
        };

        private static List<NetCat> ChuNhat(double w, double h) => new List<NetCat>
            { Duong(0, 0, w, 0), Duong(w, 0, w, h), Duong(w, h, 0, h), Duong(0, h, 0, 0) };

        private static KetQuaHinh ChayHinh(IEnumerable<NetCat> net, Action<DuLieuHinh> them = null)
        {
            var d = new DuLieuHinh { HandleKhung = 7, Net = net.ToList() };
            them?.Invoke(d);
            return PhanTichHinh.Chay(d, 0.01, 0, null);
        }

        private static string LoiHinh(KetQuaHinh r) => string.Join(" | ", r.Loi.Select(l => l.NoiDung));

        private static void TestHinh()
        {
            Console.WriteLine("[Hinh - thay lisp B1]");

            var ok = ChayHinh(ChuNhat(200, 100).Concat(new[] { Tron(30, 30, 15), Tron(100, 50, 15.01), Tron(160, 50, 5) }));
            Kiem("Bien: phi 30 la lo nho, lon hon la lo lon", ok.Tam == null ? LoiHinh(ok) : ok.Tam.LoNho + "|" + ok.Tam.LoLon, "2|1");

            var ho = ChuNhat(200, 100);
            ho[2] = Duong(200, 100, 5, 100);
            var rHo = ChayHinh(ho);
            Kiem("Bien: net ho -> loi + 2 vi tri", LoiHinh(rHo) + "|" + rHo.Loi[0].ViTri.Count, "Nét cắt bị hở hoặc chồng nhau. Nối lại cho kín.|2");
            Kiem("Bien: khe <= dung sai van kin", LoiHinh(ChayHinh(new[] { Duong(0, 0, 100, 0), Duong(100.005, 0, 100, 50), Duong(100, 50, 0, 50), Duong(0, 50, 0, 0) })), "");

            Kiem("Bien: khong co net", LoiHinh(ChayHinh(new NetCat[0])), "Khung chưa có nét cắt (layer 0-KAKOU).");
            var dai0 = ChuNhat(50, 50);
            dai0.Add(new NetCat { Loai = "ARC", DoDai = 0, Diem = { (10, 10), (10, 10) } });
            Kiem("Bien: chieu dai 0", LoiHinh(ChayHinh(dai0)).StartsWith("Có 1 nét dài bằng 0"), true);
            Kiem("Bien: doi tuong la tren 0-KAKOU",
                 LoiHinh(ChayHinh(ChuNhat(50, 50), d => d.SaiKakou.Add(("MTEXT", (1, 1, 2, 2))))).Equals("0-KAKOU có đối tượng không hợp lệ: MTEXT"), true);

            Kiem("Bien: chi tiet thua ngoai bien", LoiHinh(ChayHinh(ChuNhat(100, 100).Concat(new[] { Tron(300, 50, 10) }))),
                 "Có nét thừa hoặc lỗ cắt qua biên tấm.");
            Kiem("Bien: lo cat bien la thua", LoiHinh(ChayHinh(ChuNhat(100, 100).Concat(new[] { Tron(100, 50, 10) }))),
                 "Có nét thừa hoặc lỗ cắt qua biên tấm.");
            Kiem("Bien: lo tiep xuc mep van hop le", LoiHinh(ChayHinh(ChuNhat(100, 100).Concat(new[] { Tron(10, 50, 10) }))), "");

            const string chong = "Có lỗ trùng hoặc chồng nhau. Xoá bớt hoặc sửa lại.";
            Kiem("Bien: 2 lo giao nhau", LoiHinh(ChayHinh(ChuNhat(100, 100).Concat(new[] { Tron(40, 50, 10), Tron(50, 50, 10) }))), chong);
            Kiem("Bien: 2 lo trung nhau", LoiHinh(ChayHinh(ChuNhat(100, 100).Concat(new[] { Tron(40, 50, 10), Tron(40, 50, 10) }))), chong);
            Kiem("Bien: lo nam trong lo", LoiHinh(ChayHinh(ChuNhat(100, 100).Concat(new[] { Tron(40, 50, 20), Tron(40, 50, 5) }))), chong);
            Kiem("Bien: 2 lo tiep xuc khong tinh giao", LoiHinh(ChayHinh(ChuNhat(100, 100).Concat(new[] { Tron(40, 50, 10), Tron(60, 50, 10) }))), "");

            Kiem("Bien: hinh so 8 tu cat", LoiHinh(ChayHinh(new[] { Duong(0, 0, 100, 100), Duong(100, 100, 100, 0), Duong(100, 0, 0, 100), Duong(0, 100, 0, 0) })),
                 "Biên dạng tự cắt nhau. Sửa lại nét cắt.");

            var nhieu = ChuNhat(100, 100).Concat(new[] { Tron(300, 50, 10), Tron(40, 50, 10), Tron(50, 50, 10), Duong(5, 5, 20, 5) }).ToList();
            var rNhieu = ChayHinh(nhieu, d => d.SaiKakou.Add(("MTEXT", (1, 1, 2, 2))));
            Kiem("Du loi: 1 tam bao cung luc 4 loai", rNhieu.Loi.Count + "|" + rNhieu.Loi[0].NoiDung.Contains("0-KAKOU") + "|" + rNhieu.Loi[1].NoiDung.Contains("hở") + "|" +
                 rNhieu.Loi[2].NoiDung.Contains("thừa") + "|" + rNhieu.Loi[3].NoiDung.Contains("chồng nhau") + "|" + (rNhieu.Tam == null), "4|True|True|True|True|True");
            var bienHo = ChuNhat(100, 100);
            bienHo[2] = Duong(100, 100, 5, 100);
            var rBienHo = ChayHinh(bienHo.Concat(new[] { Tron(30, 50, 10), Tron(70, 50, 10) }));
            Kiem("Du loi: bien ngoai ho -> khong bao cac lo la thua", LoiHinh(rBienHo), "Nét cắt bị hở hoặc chồng nhau. Nối lại cho kín.");
            var loHo = ChuNhat(100, 100).Concat(new[] { Duong(30, 30, 40, 30), Tron(70, 50, 10) });
            Kiem("Du loi: net ho nho trong bien van kiem phan con lai", LoiHinh(ChayHinh(loHo.Concat(new[] { Tron(75, 50, 10) }))),
                 "Nét cắt bị hở hoặc chồng nhau. Nối lại cho kín. | Có lỗ trùng hoặc chồng nhau. Xoá bớt hoặc sửa lại.");

            var cloud = CloudLoi.Tao(new[] { (10.0, 10.0, 20.0, 20.0), (21.0, 10.0, 30.0, 20.0), (500.0, 500.0, 500.0, 500.0) }, 100);
            Kiem("Cloud: hop chong nhau gop lam 1, diem ho co cloud rieng", cloud.Count, 2);
            var vien = LamPhang.Polyline(cloud[0], true);
            Kiem("Cloud: cung cong ra ngoai, bao tron doi tuong",
                 vien.Min(p => p.X) < 8.8 && vien.Max(p => p.X) > 31.2 && vien.Min(p => p.Y) < 8.8 && vien.Max(p => p.Y) > 21.2, true);

            var bulge = LamPhang.Polyline(new[] { (0.0, 0.0, 1.0), (10.0, 0.0, 1.0) }, true);
            double rMax = bulge.Max(p => Math.Sqrt((p.X - 5) * (p.X - 5) + p.Y * p.Y));
            Kiem("LamPhang: 2 bulge = hinh tron r5", Math.Abs(rMax - 5) < 1e-6 && Math.Abs(bulge[0].X - bulge[bulge.Count - 1].X) < 1e-9, true);

            var thuong = ChayHinh(ChuNhat(200, 100), d =>
            {
                d.Be.Add(new NetBe { X1 = 50, Y1 = 0, X2 = 50, Y2 = 100 });
                d.Be.Add(new NetBe { X1 = 150, Y1 = 0, X2 = 150, Y2 = 100 });
                d.Be.Add(new NetBe { X1 = 0, Y1 = 40, X2 = 30, Y2 = 40 });
            });
            Kiem("Be: gom theo dai, dai truoc, co m", string.Join(";", thuong.Tam.DuongBe.Select(b => b.Dai + "x" + b.SoLuong)) + "|" + thuong.Tam.LoaiGiaCong, "100x2;30x1|m");
            var noi = ChayHinh(ChuNhat(200, 100), d =>
            {
                d.Be.Add(new NetBe { X1 = 50, Y1 = 0, X2 = 50, Y2 = 40 });
                d.Be.Add(new NetBe { X1 = 50, Y1 = 60, X2 = 50, Y2 = 100 });
            });
            Kiem("Be: 2 doan cung duong la 1 duong dai", string.Join(";", noi.Tam.DuongBe.Select(b => b.Dai + "x" + b.SoLuong)), "100x1");
            var nhan = ChayHinh(ChuNhat(200, 100), d =>
            {
                d.ChuFname.Add(new ChuHinh { Lop = "2-F-NAME", NoiDung = "N=A-1,発押し" });
                d.Be.Add(new NetBe { X1 = 50, Y1 = 0, X2 = 50, Y2 = 100 });
            });
            Kiem("Be: F-name nhan 1 lan -> x2", string.Join(";", nhan.Tam.DuongBe.Select(b => b.Dai + "x" + b.SoLuong)), "100x2");
            var tron = ChayHinh(ChuNhat(200, 100), d =>
            {
                d.ChuFname.Add(new ChuHinh { Lop = "2-F-NAME", NoiDung = "N=A-1,V=1,発押し" });
                d.Be.Add(new NetBe { X1 = 50, Y1 = 0, X2 = 50, Y2 = 100 });
                d.Be.Add(new NetBe { X1 = 150, Y1 = 0, X2 = 150, Y2 = 100 });
                d.ChuQuanh.Add(new ChuHinh { Lop = "1-SUNPOU", NoiDung = "発押し", MinX = 45, MaxX = 55, MinY = 40, MaxY = 50 });
            });
            Kiem("Be: tron - co ky hieu x2, thieu ky hieu x1 + canh bao",
                 tron.Tam.DuongBe[0].SoLuong + "|" + tron.CanhBao.Count + "|" + tron.CanhBao[0].NoiDung, "3|1|Đường bẻ thiếu ký hiệu.");

            Kiem("GiaCong: thu tu m, pitchi, t, s, k",
                 PhanTichHinh.LoaiGiaCong(new[]
                 {
                     new ChuHinh { NoiDung = "開先施工範囲" }, new ChuHinh { NoiDung = "M6 タップ" },
                     new ChuHinh { NoiDung = "\\U+76BF\\U+3082\\U+307F" }, new ChuHinh { NoiDung = "曲げ角度 90" },
                 }, true), "m, m(pitchi), t, s, k");
            Kiem("GiaCong: v= tinh la m", PhanTichHinh.LoaiGiaCong(new[] { new ChuHinh { NoiDung = "v=12" } }, false), "m");
            Kiem("GiaCong: khong co gi", PhanTichHinh.LoaiGiaCong(new[] { new ChuHinh { NoiDung = "N=A" } }, false), "");

            string goc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive - OFT", "xxxxx");
            foreach (string don in new[] { "125953", "127143" })
                DoiChieuHinh(don, Path.Combine(goc, don, don + "-s.dxf"), Path.Combine(goc, don, don + ".csv"));
            // đơn ngoài bộ mẫu: CSV_VANG_THEM="tên|dxf|csv;tên|dxf|csv"
            foreach (string muc in (Environment.GetEnvironmentVariable("CSV_VANG_THEM") ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] o = muc.Split('|');
                if (o.Length == 3) DoiChieuHinh(o[0], o[1], o[2]);
            }
        }

        private static void DoiChieuHinh(string don, string dxf, string csv)
        {
            if (!File.Exists(dxf) || !File.Exists(csv)) { Console.WriteLine("  BO QUA Hinh_" + don + " (khong co file mau)"); return; }
            string noi = Encoding.UTF8.GetString(DxfThuGon.DocChiaSe(csv));
            if (!noi.TrimStart('﻿').StartsWith(CsvXuat.TieuDe35)) { Console.WriteLine("  BO QUA Hinh_" + don + " (khong phai CSV lisp 35 cot)"); return; }
            var mau = CsvXuat.DocData(noi);
            var (khung, _) = DxfThuGon.Doc(dxf, CaiDatCsv.LopKhungMacDinh, CaiDatCsv.LopDimMacDinh);
            var hinh = DxfThuGon.DocHinh(dxf, khung, CaiDatCsv.LopTapuMacDinh);
            int soTam = 0;
            var lech = new List<string>();
            for (int i = 0; i < khung.Count; i++)
            {
                string ten = PhanTichKhung.TenKhung(khung[i]);
                if (ten == null || !mau.TryGetValue(ten, out ThongTinTam m)) continue;
                soTam++;
                var r = PhanTichHinh.Chay(hinh[i], 0.01, 0, null);
                string ra = r.Tam == null ? "LOI " + LoiHinh(r) : TomTat(r.Tam.LoNho, r.Tam.LoLon, r.Tam.Tappu, r.Tam.DuongBe, r.Tam.LoaiGiaCong);
                string lisp = TomTat(m.LoNho, m.LoLon, m.Tappu + m.Sara, m.DuongBe, m.LoaiGiaCong);
                if (ra != lisp) lech.Add(ten + ": C# [" + ra + "] lisp [" + lisp + "]");
            }
            Kiem("Hinh_" + don + " (" + soTam + " tam khop CSV lisp)", soTam > 0 ? string.Join(" / ", lech) : "khong doi chieu duoc tam nao", "");
        }

        private static string TomTat(int nho, int lon, int tapu, List<(int Dai, int SoLuong)> be, string gc) =>
            nho + "|" + lon + "|" + tapu + "|" + string.Join(";", be.OrderByDescending(b => b.Dai).Select(b => b.Dai + "x" + b.SoLuong)) + "|" + PhanTichKhung.ChuanHoaGiaCong(gc);
    }
}
