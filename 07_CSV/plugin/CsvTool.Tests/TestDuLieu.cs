using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvTool.Core;

namespace CsvTool.Tests
{
    /// <summary>Luồng quét mới: thông tin tấm lưu ở data\&lt;đơn&gt;-data.csv thay cho text trên bản vẽ.</summary>
    internal static partial class Program
    {
        private static void TestDuLieu()
        {
            Console.WriteLine("[DuLieu - CSV data]");

            var tim = new TamXuat { N = "131217-5", M = "SS400", T = "6.0", Q = "36", X = "575", Y = "65", Tt = new ThongTinTam { LoaiGiaCong = "m, k" } };
            Kiem("KhopTim: hoa thuong, nhieu tu, rong", CsvXuat.KhopTim(tim, null, "ss400 575") && CsvXuat.KhopTim(tim, null, "  "), true);
            Kiem("KhopTim: thieu 1 tu -> khong khop", CsvXuat.KhopTim(tim, null, "ss400 SUS"), false);
            Kiem("KhopTim: the loi tim theo noi dung, bo dau", CsvXuat.KhopTim(null, "Khung thiếu F-name", "thieu"), true);

            Kiem("TachO: o co nhay giu dau phay", string.Join("|", CsvXuat.TachO("A,\"m, k\",\"x\"\"y\",")), "A|m, k|x\"y|");

            // khứ hồi: Dong35 → DocData → Dong35 giữ nguyên từng ký tự
            var t = PhanTichKhung.DocFnameDim(KhungMau(), "1-XY", 8).Tam;
            t.Tt = new ThongTinTam { LoNho = 4, LoLon = 1, Tappu = 2, LoaiGiaCong = "m", CheDoTinh = "0", UuDai = "5.5" };
            t.Tt.DuongBe.Add((200, 1));
            t.Tt.GiaBaoGia = "12,000"; t.Tt.DuongBe.Add((150, 2));
            string dong = CsvXuat.Dong35(t);
            var doc = CsvXuat.DocData("﻿" + CsvXuat.TieuDe35 + "\r\n" + dong + "\r\n");
            Kiem("DocData: 1 tam theo N", string.Join(",", doc.Keys), "A-1");
            t.Tt = doc["A-1"];
            Kiem("DocData: khu hoi Dong35 giong tung ky tu", CsvXuat.Dong35(t), dong);
            Kiem("DocData: bo dong thieu cot / rong", CsvXuat.DocData("A,B,C\r\n\r\n").Count, 0);

            var cu = new Dictionary<string, ThongTinTam> { ["A-1"] = doc["A-1"] };
            var khongCo = new Dictionary<string, ThongTinTam>();
            var chuaQuet = new Dictionary<long, ThongTinTam>();
            var tQuet = new ThongTinTam { LoNho = 3, LoLon = 1, Tappu = 4, LoaiGiaCong = "m, k", CheDoTinh = "1" };
            tQuet.DuongBe.Add((150, 2));
            var quet = new Dictionary<long, ThongTinTam> { [0x2F1] = tQuet };
            Kiem("TenKhung: lay N khi F-name thieu truong", PhanTichKhung.TenKhung(KhungMau(text: new[] { Fn("N=A-7\\PM=SS400") })), "A-7");
            var kQuet = KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2") });
            kQuet.Handle = 0x2F1;
            var daNhap = new Dictionary<string, ThongTinTam> { ["A-1"] = new ThongTinTam { Tappu = 3, Sara = 1, Vat = 70, PitchiSoLan = 2, PitchiDai = 50, GiaBaoGia = "9" } };
            var gop = PhanTichKhung.Ghep(kQuet, "1-XY", 8, daNhap, quet);
            Kiem("Ghep_Quet: lay ket qua quet, giu so nhap tay trong CSV",
                 gop.Tam == null ? gop.Loi : gop.Tam.Tt.LoNho + "|" + gop.Tam.Tt.Tappu + "|" + gop.Tam.Tt.Sara + "|" + gop.Tam.Tt.Vat + "|" + gop.Tam.Tt.PitchiDai + "|" + gop.Tam.Tt.GiaBaoGia,
                 "3|3|1|70|50|9");
            var textCu = PhanTichKhung.Ghep(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2"), new TextTho { Lop = "KHUNG_IN", NoiDung = "Lỗ Tappu: 2", LaMtext = true } }), "1-XY", 8, khongCo, chuaQuet);
            Kiem("Ghep_TextCu: khong doc text cu tren ban ve", textCu.Loi, PhanTichKhung.LoiChuaCoThongTin);
            var tuCsv = PhanTichKhung.Ghep(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2") }), "1-XY", 8, cu, chuaQuet);
            Kiem("Ghep_TuCsv: khong quet -> lay CSV, F-name/dim tu ban ve",
                 tuCsv.Tam == null ? tuCsv.Loi : tuCsv.Tam.Tt.DuongBe.Count + "|" + tuCsv.Tam.X, "2|300");
            var chua = PhanTichKhung.Ghep(KhungMau(text: new[] { Fn("N=B-9\\PM=SS400\\PT=3.2\\PQ=2") }), "1-XY", 8, cu, chuaQuet);
            Kiem("Ghep_ChuaCo: khong text, khong CSV", chua.Loi, PhanTichKhung.LoiChuaCoThongTin);

            var thieuVat = new Dictionary<string, ThongTinTam> { ["A-1"] = new ThongTinTam { LoaiGiaCong = "m, k" } };
            var canhBao = PhanTichKhung.Ghep(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2") }), "1-XY", 8, thieuVat, chuaQuet);
            Kiem("Ghep_ThieuVat: van co tam (sua duoc) + canh bao", canhBao.Tam != null && (canhBao.Loi ?? "").Contains("vát"), true);
            Kiem("CoFname: khung chu thich khong co F-name", PhanTichKhung.CoFname(KhungMau(text: new[] { new TextTho { Lop = "KHUNG_IN", NoiDung = "x" } })), false);
            Kiem("LaKhungChuThich: 2 dong N= (TEXT + MTEXT)",
                 PhanTichKhung.LaKhungChuThich(KhungMau(text: new[] { Fn("N=A"), Fn("N=B", false), Fn("Q=1", false) })), true);
            Kiem("LaKhungChuThich: khung tam binh thuong", PhanTichKhung.LaKhungChuThich(KhungMau()), false);
            Kiem("ChuanHoaGiaCong lap lai van nhu nhau", PhanTichKhung.ChuanHoaGiaCong(PhanTichKhung.ChuanHoaGiaCong("m(pitchi), m, k")), "m, k");

            string thu = Path.Combine(Path.GetTempPath(), "csvtool_dl_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(thu);
            try
            {
                var ds = new List<TamXuat> { Tam("9-1") };
                CsvXuat.GhiData(thu, "9", ds);
                Kiem("GhiData: chi ghi file data", File.Exists(CsvXuat.DuongData(thu, "9")) && !File.Exists(CsvXuat.DuongCuoi(thu, "9")), true);
                CsvXuat.GhiCuoi(thu, "9", ds);
                Kiem("GhiCuoi: tieu de 10 cot", File.ReadAllLines(CsvXuat.DuongCuoi(thu, "9"))[0], CsvXuat.TieuDe10);
                using (new FileStream(CsvXuat.DuongData(thu, "9"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    KiemNem<IOException>("GhiData: file dang mo -> IOException", () => CsvXuat.GhiData(thu, "9", ds));
            }
            finally { Directory.Delete(thu, true); }
        }
    }
}
