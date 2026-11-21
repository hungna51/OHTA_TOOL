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
            var t = PhanTichKhung.Chay(KhungMau(), "1-XY", 8).Tam;
            t.Tt.GiaBaoGia = "12,000"; t.Tt.DuongBe.Add((150, 2));
            string dong = CsvXuat.Dong35(t);
            var doc = CsvXuat.DocData("﻿" + CsvXuat.TieuDe35 + "\r\n" + dong + "\r\n");
            Kiem("DocData: 1 tam theo N", string.Join(",", doc.Keys), "A-1");
            t.Tt = doc["A-1"];
            Kiem("DocData: khu hoi Dong35 giong tung ky tu", CsvXuat.Dong35(t), dong);
            Kiem("DocData: bo dong thieu cot / rong", CsvXuat.DocData("A,B,C\r\n\r\n").Count, 0);

            var cu = new Dictionary<string, ThongTinTam> { ["A-1"] = doc["A-1"] };
            var tuText = PhanTichKhung.Ghep(KhungMau(), "1-XY", 8, cu);
            Kiem("Ghep_TuText: lay text, giu gia da nhap", tuText.Tam == null ? "" : tuText.TuText + "|" + tuText.Tam.Tt.GiaBaoGia + "|" + tuText.Tam.Tt.DuongBe.Count,
                 "True|12,000|1");
            var tuCsv = PhanTichKhung.Ghep(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2") }), "1-XY", 8, cu);
            Kiem("Ghep_TuCsv: khong text -> lay CSV, F-name/dim tu ban ve",
                 tuCsv.Tam == null ? tuCsv.Loi : tuCsv.TuText + "|" + tuCsv.Tam.Tt.DuongBe.Count + "|" + tuCsv.Tam.X, "False|2|300");
            var chua = PhanTichKhung.Ghep(KhungMau(text: new[] { Fn("N=B-9\\PM=SS400\\PT=3.2\\PQ=2") }), "1-XY", 8, cu);
            Kiem("Ghep_ChuaCo: khong text, khong CSV", chua.Loi, PhanTichKhung.LoiChuaCoThongTin);

            var vat = MauThongTin.Select(s => s.StartsWith("Các loại") ? "Các loại gia công: m, k" : s).ToList();
            var canhBao = PhanTichKhung.Ghep(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2"), Tt(vat) }), "1-XY", 8, cu);
            Kiem("Ghep_ThieuVat: van co tam (sua duoc) + canh bao", canhBao.Tam != null && (canhBao.Loi ?? "").Contains("vát"), true);
            Kiem("Chay_ThieuVat: EXPORT kieu lisp van la loi", PhanTichKhung.Chay(KhungMau(text: new[] { Fn("N=A-1\\PM=SS400\\PT=3.2\\PQ=2"), Tt(vat) }), "1-XY", 8).Tam, null);
            Kiem("CoFname: khung chu thich khong co F-name", PhanTichKhung.CoFname(KhungMau(text: new[] { Tt(MauThongTin) })), false);
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
