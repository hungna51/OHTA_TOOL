using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        // MText đúng kiểu nút "Xuất thông tin tấm" sinh ra (CSV--pricing-text-content)
        private static readonly string[] MauThongTin =
        {
            "Lỗ nhỏ: 4", "Lỗ lớn: 1", "Lỗ Tappu: 2", "Lỗ Sara: 0",
            "Pitchi - Số lần: 0", "Pitchi - Chiều dài (mm): 0", "Chiều dài vát: 0",
            "Đường bẻ 1: 200 x 1",
            "Các loại gia công: m",
            "Chế độ tính:0", "Giá báo giá:", "Tiền vật liệu(nhập sẵn):", "Ưu đãi(%): 5.5",
        };

        private static string MaHoaEscape(string s) =>
            string.Concat(s.Select(c => c < 128 ? c.ToString() : "\\U+" + ((int)c).ToString("X4")));

        private static void TestThongTinTam()
        {
            Console.WriteLine("[ThongTinTam]");

            var r = ThongTinTamParser.Doc(MauThongTin);
            Kiem("Doc_DayDu: khong loi", r.Loi, null);
            Kiem("Doc_DayDu: LoNho", r.Tam.LoNho, 4);
            Kiem("Doc_DayDu: LoLon", r.Tam.LoLon, 1);
            Kiem("Doc_DayDu: Tappu", r.Tam.Tappu, 2);
            Kiem("Doc_DayDu: DuongBe[0]", r.Tam.DuongBe.Count == 1 ? r.Tam.DuongBe[0] : (0, 0), (200, 1));
            Kiem("Doc_DayDu: LoaiGiaCong", r.Tam.LoaiGiaCong, "m");
            Kiem("Doc_DayDu: CheDoTinh", r.Tam.CheDoTinh, "0");

            var e = ThongTinTamParser.Doc(MauThongTin.Select(MaHoaEscape).ToList());
            Kiem("Doc_EscapeUnicode: khong loi", e.Loi, null);
            Kiem("Doc_EscapeUnicode: giong ban Unicode",
                 e.Tam == null ? "" : string.Join("|", e.Tam.LoNho, e.Tam.LoLon, e.Tam.Tappu, e.Tam.DuongBe.Count, e.Tam.LoaiGiaCong, e.Tam.UuDai),
                 "4|1|2|1|m|5.5");

            var thieu = MauThongTin.ToList();
            thieu.Insert(8, "Đường bẻ 2: 150 x 2");
            thieu.RemoveAt(0);           // 13 dòng, có m + 2 loại đường bẻ -> cần 14
            var s = ThongTinTamParser.Doc(thieu);
            Kiem("Doc_SaiSoDong: thong bao",
                 s.Loi != null && s.Loi.StartsWith("Text thông tin cũ sai số dòng (có 13, cần 14)."),
                 true);

            var b11 = MauThongTin.ToList();
            b11[7] = "Đường bẻ 11: 50 x 2";
            var r11 = ThongTinTamParser.Doc(b11);
            Kiem("Doc_DuongBe11: chi so 11 -> can 23 dong", r11.Loi != null && r11.Loi.Contains("cần 23"), true);

            Kiem("Doc_ConTrongGiuChuoi: GiaBaoGia", r.Tam.GiaBaoGia, "");
            Kiem("Doc_ConTrongGiuChuoi: UuDai", r.Tam.UuDai, "5.5");

            var hai = MauThongTin.ToList();
            hai.Insert(8, "Đường bẻ 2: 150 x 2");
            var rh = ThongTinTamParser.Doc(hai);
            Kiem("Doc_ThuTuDuongBe: giu thu tu dong nhan vao",
                 rh.Tam == null ? "" : string.Join(";", rh.Tam.DuongBe.Select(x => x.Dai + "x" + x.SoLuong)), "200x1;150x2");

            Kiem("Doc_Rong: bao khong co text", ThongTinTamParser.Doc(new List<string>()).Loi,
                 "Không có text thông tin trong khung.");

            string mt = "{\\fArial|b0;Lỗ nhỏ: 4\\PLỗ lớn: 1}";
            Kiem("TachDong_Mtext: bo {\\f..; } va tach \\P",
                 string.Join("|", ThongTinTamParser.TachDong(mt, true)), "Lỗ nhỏ: 4|Lỗ lớn: 1");
            Kiem("TachDong_XuongDong: bo \\r", string.Join("|", ThongTinTamParser.TachDong("A: 1\r\nB: 2", true)), "A: 1|B: 2");
            Kiem("LaTextThongTin: co Tappu", ThongTinTamParser.LaTextThongTin(MauThongTin), true);
            Kiem("LaTextThongTin: khong", ThongTinTamParser.LaTextThongTin(new[] { "N=A", "Q=1" }), false);

            string goc = string.Join("\\P", MauThongTin);
            string moi = ThongTinTamParser.ThayGiaTri(goc, "LO SARA", "3");
            Kiem("ThayGiaTri_Sara: doi dung 1 dong", moi, goc.Replace("Lỗ Sara: 0", "Lỗ Sara: 3"));
            string moiEsc = ThongTinTamParser.ThayGiaTri(MaHoaEscape(goc), "CHIEU DAI VAT", "12");
            Kiem("ThayGiaTri_Escape: nhan dang \\U+", moiEsc.Contains("v\\U+00E1t: 12"), true);
            KiemNem<InvalidOperationException>("ThayGiaTri_KhongCoNhan",
                () => ThongTinTamParser.ThayGiaTri("Lỗ nhỏ: 4", "LO SARA", "1"));

            Kiem("WcMatch: L*NH*:*", ThongTinTamParser.WcMatch("LO NHO: 4", "L*NH*:*"), true);
            Kiem("WcMatch: khong khop", ThongTinTamParser.WcMatch("LO LON: 4", "L*NH*:*"), false);
        }
    }
}
