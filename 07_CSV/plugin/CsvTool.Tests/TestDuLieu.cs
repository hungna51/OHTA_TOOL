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

            // khứ hồi: Dong34 → DocData → Dong34 giữ nguyên từng ký tự
            var t = PhanTichKhung.DocFnameDim(KhungMau(), "1-XY", 8).Tam;
            t.Tt = new ThongTinTam { LoNho = 4, LoLon = 1, Tappu = 2, LoaiGiaCong = "m, m(non), r1", CheDoTinh = "0", GiaTay = true };
            t.Tt.DuongBe.Add((200, 1));
            t.Tt.GiaBaoGia = "12,000"; t.Tt.DuongBe.Add((150, 2));
            string dong = CsvXuat.Dong34(t);
            var doc = CsvXuat.DocData("﻿" + CsvXuat.TieuDe34 + "\r\n" + dong + "\r\n");
            Kiem("DocData: 1 tam theo N", string.Join(",", doc.Keys), "A-1");
            t.Tt = doc["A-1"];
            Kiem("DocData: khu hoi Dong34 giong tung ky tu, giu gia tay + loai pitchi", CsvXuat.Dong34(t) + "|" + t.Tt.GiaTay, dong + "|True");
            Kiem("DocData: file moi khong phai data cu", CsvXuat.LaDataCu(CsvXuat.TieuDe34 + "\r\n" + dong), false);

            // file data cũ (lisp 35 cột) + -gia.json cũ → 34 cột: giá tay, loại pitchi (lisp ghi "m"), cuộn chọn tay
            string cu35 = "﻿" + CsvXuat.TieuDe35 + "\r\n" +
                "B-1,SS400,6,500,300,2,0,0,0,0,3,800,m,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,900,,5\r\n" +          // chỉ pitchi, giá tự động
                "B-2,SS400,6,500,300,1,0,0,0,0,0,0,\"m, t\",0,0,1,700,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1500,,\r\n" +   // giá tay, cuộn chọn nửa hình
                "B-3,SS400,6,500,300,1,0,0,0,0,2,500,\"m, r2\",2,0,1,300,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,800,,\r\n";  // bẻ + pitchi, chọn Không cuộn
            var json = new GiaDon();
            json.Lay("B-1").TuDong = true; json.Lay("B-1").LoaiPitchi = "KakumaruLech";
            json.Lay("B-2").DonGiaCuon = 6000;
            json.Lay("B-3").TuDong = true; json.Lay("B-3").DonGiaCuon = -1;
            Kiem("LaDataCu: file lisp 35 cot", CsvXuat.LaDataCu(cu35), true);
            var moi = CsvXuat.ChuyenDataCu(cu35, json);
            Kiem("ChuyenDataCu: tieu de 34 + 3 dong 34 cot", moi[0] + "|" + string.Join(";", moi.Skip(1).Select(x => CsvXuat.TachO(x).Count)), CsvXuat.TieuDe34 + "|34;34;34");
            var sau = CsvXuat.DocData(string.Join("\r\n", moi));
            Kiem("ChuyenDataCu: chi pitchi -> m(kakumarulech), bo m; gia tu dong",
                 sau["B-1"].LoaiGiaCong + "|" + sau["B-1"].GiaTay + "|" + sau["B-1"].PitchiSoLan, "m(kakumarulech)|False|3");
            Kiem("ChuyenDataCu: gia tay, cuon chon nua hinh -> r1, giu be",
                 sau["B-2"].LoaiGiaCong + "|" + sau["B-2"].GiaTay + "|" + sau["B-2"].DuongBe.Count, "m, t, r1|True|1");
            Kiem("ChuyenDataCu: be + pitchi chua chon loai -> m, m(pitchi); chon Khong cuon -> bo r2",
                 sau["B-3"].LoaiGiaCong + "|" + sau["B-3"].GiaTay, "m, m(pitchi)|False");
            Kiem("DocData file cu kem json = sau khi chuyen", CsvXuat.Dong34(new TamXuat { N = "B-2", M = "SS400", T = "6", X = "500", Y = "300", Q = "1", Tt = CsvXuat.DocData(cu35, json)["B-2"] }), moi[2]);
            Kiem("Ma gia cong: dat loai pitchi / cuon / doc lai",
                 CsvXuat.DatLoaiPitchi("m, t", "Non") + "|" + CsvXuat.DatLoaiPitchi("m(non), t", "Kakumaru") + "|" + CsvXuat.DatCuon("m, r1, t", "Cả hình") + "|" +
                 CsvXuat.DatCuon("m, r2", "Không") + "|" + CsvXuat.LoaiPitchiCua("m, m(nonlech)") + "|" + CsvXuat.LoaiPitchiCua("t"),
                 "m, m(non), t|m(kakumaru), t|m, t, r2|m|NonLech|Pitchi");
            Kiem("MaMage: m(...) -> m", CsvXuat.MaMage("m(kakumaru)") + "|" + CsvXuat.MaMage("m, m(non)") + "|" + CsvXuat.MaMage("m(non), t"), "m|m|*");
            Kiem("KiemTra: m(kakumaru) khong phai vat, m(non) thieu pitchi",
                 PhanTichKhung.KiemTra(new ThongTinTam { LoaiGiaCong = "m(kakumaru)", PitchiSoLan = 2, PitchiDai = 100 }) + "|" + PhanTichKhung.KiemTra(new ThongTinTam { LoaiGiaCong = "m(non)" }),
                 "|" + PhanTichKhung.LoiThieuPitchi);
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
            Kiem("Ghep_Quet: Tapu, Sara theo ban ve, giu vat, pitchi, gia nhap tay",
                 gop.Tam == null ? gop.Loi : gop.Tam.Tt.LoNho + "|" + gop.Tam.Tt.Tappu + "|" + gop.Tam.Tt.Sara + "|" + gop.Tam.Tt.Vat + "|" + gop.Tam.Tt.PitchiDai + "|" + gop.Tam.Tt.GiaBaoGia,
                 "3|4|0|70|50|9");
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
