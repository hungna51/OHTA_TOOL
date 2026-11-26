using System;
using System.Collections.Generic;
using System.IO;
using AiTool.Core;

namespace AiTool.Tests
{
    /// <summary>
    /// Test logic thuần của AiTool (Core/): đọc dòng tiến độ / kết quả mà OhtaTeams.exe --thu-nhanh-* in ra.
    /// In OK / FAIL từng ca rồi tổng kết, cùng kiểu GroTool.Tests. Mã thoát 1 nếu có ca hỏng.
    /// </summary>
    internal static class Program
    {
        private static int _ok, _fail;

        internal static void Kiem(string ten, object duoc, object mongDoi)
        {
            bool dung = Equals(duoc, mongDoi);
            if (dung) _ok++; else _fail++;
            Console.WriteLine("  {0} {1,-62} -> {2}{3}", dung ? "OK  " : "FAIL", ten,
                              duoc ?? "null", dung ? "" : "   (mong doi " + (mongDoi ?? "null") + ")");
        }

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== Dong in ra cua OhtaTeams.exe --thu-nhanh-* ===");
            Kiem("@ -> tien do", DongExe.Doc("@ AI đang đọc…").Loai, LoaiDong.TienDo);
            Kiem("@ -> chu bo tien to", DongExe.Doc("@ AI đang đọc…").Chu, "AI đang đọc…");
            Kiem("#TM -> thu muc", DongExe.Doc("#TM C:\\a\\261125-1").Chu, "C:\\a\\261125-1");
            Kiem("#TM -> loai ThuMuc", DongExe.Doc("#TM C:\\a").Loai, LoaiDong.ThuMuc);
            Kiem("#KQ -> ket qua", DongExe.Doc("#KQ {\"a\":1}").Loai, LoaiDong.KetQua);
            Kiem("#LOI -> loi, giu chu", DongExe.Doc("#LOI Thiếu môi trường vẽ").Chu, "Thiếu môi trường vẽ");
            Kiem("Dong la (python in ra) -> Khac", DongExe.Doc("Traceback ...").Loai, LoaiDong.Khac);
            Kiem("null -> Khac, chu rong", DongExe.Doc(null).Chu, "");

            Console.WriteLine();
            Console.WriteLine("=== LanThu.TuJson ===");
            string mot = "{ \"thu_muc\": \"C:\\\\t\\\\261125-102246\", \"gio\": \"25/11 10:22\", \"mo_ta\": \"chu_t · 276 x 292\", " +
                         "\"chi_tiet\": \"SS400 t3.2\", \"anh\": \"C:\\\\a.png\", \"xem\": null, \"dxf\": \"C:\\\\t\\\\TN-1.dxf\", " +
                         "\"chi_tiet_json\": \"C:\\\\t\\\\chi_tiet.json\", \"loi\": \"Chưa có mẫu\", \"canh_bao\": [\"Thiếu dim khách: Ø18\", \"x\"] }";
            List<LanThu> ds = LanThu.TuJson(mot);
            Kiem("Mot doi tuong -> 1 lan thu", ds.Count, 1);
            LanThu l = ds[0];
            Kiem("thu_muc", l.ThuMuc, "C:\\t\\261125-102246");
            Kiem("gio + mo_ta", l.Gio + " | " + l.MoTa, "25/11 10:22 | chu_t · 276 x 292");
            Kiem("xem null -> null", l.Xem, null);
            Kiem("dxf", l.Dxf, "C:\\t\\TN-1.dxf");
            Kiem("loi", l.Loi, "Chưa có mẫu");
            Kiem("canh_bao 2 dong", l.CanhBao.Count, 2);
            Kiem("CoDxf: co duong dan nhung file khong ton tai -> false", l.CoDxf, false);
            Kiem("can_mau thieu -> false", l.CanMau, false);
            Kiem("can_mau true -> CanMau (hien nut AI viet mau)", LanThu.TuJson("{\"can_mau\": true}")[0].CanMau, true);
            Kiem("can_xac_nhan thieu -> rong", l.CanXacNhan.Count, 0);
            Kiem("can_xac_nhan doc du", string.Join(" / ", LanThu.TuJson("{\"can_xac_nhan\": [\"R10 bo góc khoét\", \"y lỗ Ø50\"]}")[0].CanXacNhan), "R10 bo góc khoét / y lỗ Ø50");
            var lg = LanThu.TuJson("{\"ghi_chu_nguoi\": \"Bo R5 mọi góc\", \"mau_thuan\": [\"Ghi chú R5, hình như R10\"]}")[0];
            Kiem("ghi_chu_nguoi + mau_thuan doc du", lg.GhiChuNguoi + "|" + string.Join(" / ", lg.MauThuan), "Bo R5 mọi góc|Ghi chú R5, hình như R10");
            Kiem("ghi_chu_nguoi thieu -> rong", l.GhiChuNguoi + "|" + l.MauThuan.Count, "|0");
            Kiem("Tham so kem ghi chu tep", DuongDan.ThamSo("--thu-nhanh-moi", @"C:\a.png", "--ghi-chu-tep", @"C:\t b\g.txt"), @"--thu-nhanh-moi C:\a.png --ghi-chu-tep ""C:\t b\g.txt""");
            Kiem("Mang 2 doi tuong", LanThu.TuJson("[" + mot + "," + mot + "]").Count, 2);
            Kiem("Mang rong", LanThu.TuJson("[]").Count, 0);
            Kiem("JSON hong -> rong, khong vang", LanThu.TuJson("{hong").Count, 0);
            Kiem("Thieu truong -> chuoi rong / null", LanThu.TuJson("{}")[0].MoTa + "|" + (LanThu.TuJson("{}")[0].Dxf ?? "null"), "|null");

            Console.WriteLine();
            Console.WriteLine("=== Duong dan OhtaTeams.exe ===");
            Kiem("exe nam o <OHTA_TOOL>\\_apps\\OhtaTeams\\bin",
                 DuongDan.Exe(Path.Combine("C:\\x", "OHTA_TOOL", "05_AI")), Path.Combine("C:\\x", "OHTA_TOOL", "_apps", "OhtaTeams", "bin", "OhtaTeams.exe"));
            Kiem("thu muc module co \\ cuoi", DuongDan.Exe("C:\\x\\OHTA_TOOL\\05_AI\\"), Path.Combine("C:\\x", "OHTA_TOOL", "_apps", "OhtaTeams", "bin", "OhtaTeams.exe"));
            Kiem("Tham so co khoang trang duoc boc ngoac kep", DuongDan.ThamSo("--thu-nhanh-moi", "C:\\a b\\x.png"), "--thu-nhanh-moi \"C:\\a b\\x.png\"");
            Kiem("Tham so ket thuc bang \\ khong nuot ngoac kep", DuongDan.ThamSo("--thu-nhanh-ve", "C:\\a b\\"), "--thu-nhanh-ve \"C:\\a b\\\\\"");

            Console.WriteLine();
            Console.WriteLine("Tong: {0} OK, {1} FAIL", _ok, _fail);
            return _fail == 0 ? 0 : 1;
        }
    }
}
