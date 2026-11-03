using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.Tests
{
    /// <summary>
    /// Test logic thuần của ZwCadPlugin. In OK / FAIL từng ca rồi tổng kết, cùng kiểu
    /// với các bộ mô phỏng Python của dự án lisp. Mã thoát 1 nếu có ca hỏng.
    /// </summary>
    internal static class Program
    {
        private static int _ok, _fail;

        private static void Kiem(string ten, object duoc, object mongDoi)
        {
            bool dung = Equals(duoc, mongDoi);
            if (dung) _ok++; else _fail++;
            Console.WriteLine("  {0} {1,-62} -> {2}{3}", dung ? "OK  " : "FAIL", ten,
                              duoc ?? "null", dung ? "" : "   (mong doi " + (mongDoi ?? "null") + ")");
        }

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            // Muc 170/171: phat lai dump ghi tu ZWCAD ngoai CAD.
            //   phatlai <buoc> <file> [tham so]   (vd: phatlai datmk %TEMP%\gro_dat_mk_dump.txt 13)
            //   phatlai <file> [bango]            (cu phap cu muc 170 = datmk)
            if (args != null && args.Length >= 2 && args[0] == "phatlai") return PhatLai(args);
            // .NET (khong phai Framework) can dang ky moi co CP932; ZWCAD chay net48 thi co san.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // Muc 158: test KHONG duoc dung vao tu dien that cua nguoi dung (ResetToDefaults / Sync /
            // SetAutoUpload / SetScriptUrl ghi registry) -> dung khoa rieng, xoa khi chay xong.
            JapanNoteTranslator.RegSubKey = KhoaTest;

            Console.WriteLine("=== Thay bango trong ma khac / F-name N= (doan so CUOI) ===");
            Kiem("ma khac co so ban ve  I-1-3 -> 5", GroText.ThayBangoCuoi("I-1-3", "5"), "I-1-5");
            Kiem("ma khac khong so ban ve  I-3 -> 5", GroText.ThayBangoCuoi("I-3", "5"), "I-5");
            Kiem("N= thuong  N=xxxxxx-1-3 -> 12", GroText.ThayBangoCuoi("N=xxxxxx-1-3", "12"), "N=xxxxxx-1-12");
            Kiem("N= ten ban ve BAT DAU bang so  N=26015-1-3 -> 5",
                 GroText.ThayBangoCuoi("N=26015-1-3", "5"), "N=26015-1-5");
            Kiem("khong co doan so cuoi -> giu nguyen", GroText.ThayBangoCuoi("KHONGCOSO", "5"), "KHONGCOSO");
            Kiem("chuoi null -> null", GroText.ThayBangoCuoi(null, "5"), null);
            Kiem("doan so cuoi co khoang trang  I-1- 3", GroText.ThayBangoCuoi("I-1- 3", "7"), "I-1- 7");

            // Tai hien LOI cua palette cu de chung minh no that su hong.
            string cuMaKhac = Regex.Replace("I-1-3", @"(-\s*)\d+", "${1}5");
            Kiem("[LOI CU] Regex.Replace moi '-so' bien I-1-3 thanh", cuMaKhac, "I-5-5");
            string cuN = Regex.Replace("N=26015-1-3", @"^(N=)(\d+)(.*)$", "${1}05${3}");
            Kiem("[LOI CU] thay cum so DAU sau N= bien N=26015-1-3 thanh", cuN, "N=05-1-3");

            Console.WriteLine();
            Console.WriteLine("=== Doc bango tu dong N= ===");
            Kiem("N=xxxxxx-1-3 -> 3", GroText.TryDocBangoCuoi("N=xxxxxx-1-3", out string b1) ? b1 : null, "3");
            Kiem("N=26015-1-17 -> 17 (khong lay 26015)",
                 GroText.TryDocBangoCuoi("N=26015-1-17", out string b2) ? b2 : null, "17");
            Kiem("N=ABC -> khong co", GroText.TryDocBangoCuoi("N=ABC", out string b3), false);

            Console.WriteLine();
            Console.WriteLine("=== Bango hop le ===");
            Kiem("'3'", GroText.LaBangoHopLe("3"), true);
            Kiem("'120'", GroText.LaBangoHopLe("120"), true);
            Kiem("'03' (lisp khong bao gio ghi so dem 0)", GroText.LaBangoHopLe("03"), false);
            Kiem("'0'", GroText.LaBangoHopLe("0"), false);
            Kiem("'3a'", GroText.LaBangoHopLe("3a"), false);
            Kiem("rong", GroText.LaBangoHopLe(""), false);
            Kiem("' 5 ' (co khoang trang)", GroText.LaBangoHopLe(" 5 "), true);

            Console.WriteLine();
            Console.WriteLine("=== Dong F-name bao tam be ===");
            Kiem("'V='", GroText.LaDongBe("V="), true);
            Kiem("'  v=  '", GroText.LaDongBe("  v=  "), true);
            Kiem("'Z=SS400' (vat lieu, KHONG phai be)", GroText.LaDongBe("Z=SS400"), false);
            Kiem("'Z=SS400-SIMA'", GroText.LaDongBe("Z=SS400-SIMA"), false);
            Kiem("'T=6.0'", GroText.LaDongBe("T=6.0"), false);

            Console.WriteLine();
            Console.WriteLine("=== Ten mau (giong gro--ten-mau) ===");
            Kiem("4", GroText.TenMau(4), "4 - Cyan (Da troi)");
            Kiem("1", GroText.TenMau(1), "1 - Red (Do)");
            Kiem("30", GroText.TenMau(30), "30 (Mau CAD)");
            Kiem("0 (khong xac dinh)", GroText.TenMau(0), "Khong xac dinh");
            Kiem("-1 (khong xac dinh)", GroText.TenMau(-1), "Khong xac dinh");

            Console.WriteLine();
            Console.WriteLine("=== Dem dinh polyline (gro--pl-gon) ===");
            var r4 = new List<double[]> { new[] { 0.0, 0 }, new[] { 10.0, 0 }, new[] { 10.0, 5 }, new[] { 0.0, 5 } };
            var r5 = new List<double[]>(r4) { new[] { 0.0, 0 } };
            var l6 = new List<double[]> { new[] { 0.0, 0 }, new[] { 10.0, 0 }, new[] { 10.0, 4 },
                                          new[] { 4.0, 4 }, new[] { 4.0, 10 }, new[] { 0.0, 10 } };
            Kiem("4 dinh", LayerRanking.SoDinhGon(r4), 4);
            Kiem("5 dinh, dinh cuoi trung dinh dau -> 4", LayerRanking.SoDinhGon(r5), 4);
            Kiem("hinh L 6 dinh", LayerRanking.SoDinhGon(l6), 6);

            Console.WriteLine();
            Console.WriteLine("=== Luat nhan khung: khung bao = dung 4, vung = tu 3 ===");
            Kiem("khung bao: 4 dinh kin phang", LayerRanking.LaKhung(4, true, true, true), true);
            Kiem("khung bao: 6 dinh -> khong", LayerRanking.LaKhung(6, true, true, true), false);
            Kiem("vung: 6 dinh -> co", LayerRanking.LaKhung(6, true, true, false), true);
            Kiem("vung: 3 dinh -> co", LayerRanking.LaKhung(3, true, true, false), true);
            Kiem("khong kin -> khong", LayerRanking.LaKhung(4, false, true, false), false);
            Kiem("khong phang -> khong", LayerRanking.LaKhung(4, true, false, false), false);

            Console.WriteLine();
            Console.WriteLine("=== Sap xep layer (so luong giam, ten tang) ===");
            var dem = new Dictionary<string, int> { { "B", 1 }, { "A", 1 }, { "KHUNG", 3 }, { "VIEN", 2 } };
            Kiem("thu tu", string.Join(",", LayerRanking.SapXep(dem).Select(kv => kv.Key)), "KHUNG,VIEN,A,B");
            Kiem("hai layer cung so luong KHONG bi mat (loi vl-sort muc 22)",
                 LayerRanking.SapXep(dem).Count, 4);

            Console.WriteLine();
            Console.WriteLine("=== Danh sach popup khung bao ===");
            var ds = LayerRanking.DanhSachKhung(new Dictionary<string, int> { { "KHUNG", 13 }, { "RAC", 0 } },
                                                new[] { "0", "KHUNG", "RAC" }, out bool coLoc);
            Kiem("chi layer co khung", string.Join("|", ds.Select(m => m.HienThi)), "KHUNG  (13 khung)");
            Kiem("   co loc duoc", coLoc, true);
            var ds2 = LayerRanking.DanhSachKhung(new Dictionary<string, int>(), new[] { "Z", "0", "A" }, out bool coLoc2);
            Kiem("khong co khung nao -> TAT CA layer theo ten", string.Join(",", ds2.Select(m => m.Ten)), "0,A,Z");
            Kiem("   bao la khong loc duoc", coLoc2, false);

            Console.WriteLine();
            Console.WriteLine("=== Danh sach popup layer goc (xu ly net) ===");
            var dx = LayerRanking.DanhSachXln(new Dictionary<string, int> { { "DIM", 40 }, { "GOC", 900 } },
                                              new[] { "0", "DIM", "GOC", "TRONG" });
            Kiem("moi layer, ke ca layer trong", string.Join("|", dx.Select(m => m.HienThi)),
                 "GOC  (900 doi tuong)|DIM  (40 doi tuong)|0  (0 doi tuong)|TRONG  (0 doi tuong)");

            Console.WriteLine();
            Console.WriteLine("=== Chon mac dinh layer goc (luat muc 75) ===");
            Kiem("layer da luu dung dau -> giu", LayerRanking.ViTriXln(dx, "GOC"), 0);
            Kiem("layer da luu IT hon layer dau -> chon layer dau", LayerRanking.ViTriXln(dx, "DIM"), 0);
            Kiem("layer da luu khong con -> chon layer dau", LayerRanking.ViTriXln(dx, "MAT"), 0);
            var bang = LayerRanking.DanhSachXln(new Dictionary<string, int> { { "A", 50 }, { "B", 50 } }, new[] { "A", "B" });
            Kiem("layer da luu BANG layer dau -> giu", LayerRanking.ViTriXln(bang, "B"), 1);
            Kiem("layer da luu = 0 doi tuong -> chon layer dau",
                 LayerRanking.ViTriXln(LayerRanking.DanhSachXln(new Dictionary<string, int>(), new[] { "A", "B" }), "B"), 0);

            Console.WriteLine();
            Console.WriteLine("=== Vi tri layer khung / vung da luu ===");
            Kiem("co trong danh sach", LayerRanking.ViTri(ds2, "Z"), 2);
            Kiem("khong co -> 0", LayerRanking.ViTri(ds2, "MAT"), 0);

            Console.WriteLine();
            Console.WriteLine("=== Tham so (gro_net_dialog ...) tu lisp ===");
            // Thu tu THAM SO: lay-k vung layer-vung in-pdf use-xln lay-xln auto-dim mau-dim tao-khung [giu-xln]
            var ts = GroThamSo.DocThamSo(new object[] { "KHUNG", true, "VUNG", null, true, "GOC", null, 3, true },
                                         out bool giu);
            Kiem("tham so 0 = layer KHUNG", ts.LayerKhung, "KHUNG");
            Kiem("tham so 1 = dung vung", ts.UseVung, true);
            Kiem("tham so 2 = layer VUNG", ts.LayerVung, "VUNG");
            Kiem("tham so 3 nil = khong in PDF", ts.UseInPdf, false);
            Kiem("tham so 5 = layer goc", ts.LayerXln, "GOC");
            Kiem("tham so 6 nil = tat auto Dim", ts.AutoDim, false);
            Kiem("tham so 7 = mau 3", ts.MauDim, 3);
            Kiem("tham so 8 = tao khung", ts.UseTaoKhung, true);
            Kiem("thieu tham so 9 -> khong giu layer goc", giu, false);
            var ts2 = GroThamSo.DocThamSo(new object[] { null, null, null, null, null, null, null, 4.0, null, true }, out bool giu2);
            Kiem("tham so 9 = T -> giu layer goc", giu2, true);
            Kiem("mau la so thuc 4.0 -> 4", ts2.MauDim, 4);
            Kiem("mau 0 (ByBlock) -> mac dinh 4",
                 GroThamSo.DocThamSo(new object[] { null, null, null, null, null, null, null, 0 }, out _).MauDim, 4);
            Kiem("mau 256 (ByLayer) -> mac dinh 4",
                 GroThamSo.DocThamSo(new object[] { null, null, null, null, null, null, null, 256 }, out _).MauDim, 4);
            Kiem("goi khong tham so -> khong no", GroThamSo.DocThamSo(null, out _).LayerKhung, null);
            Kiem("chuoi rong '' -> null (lisp ghi registry rong)",
                 GroThamSo.DocThamSo(new object[] { "" }, out _).LayerKhung, null);
            Kiem("co dang chuoi '0' -> sai", GroThamSo.LaDung("0"), false);
            Kiem("co dang chuoi '1' -> dung", GroThamSo.LaDung("1"), true);

            Console.WriteLine();
            Console.WriteLine("=== Ket qua tra ve lisp (nth 0..8 dlg) ===");
            var kq = GroThamSo.KetQua(new GroSettings
            {
                UseVung = true, LayerVung = "VUNG", LayerKhung = "KHUNG", UseInPdf = false,
                UseXln = true, LayerXln = "GOC", AutoDim = false, MauDim = 3, UseTaoKhung = true,
            });
            Kiem("dung 11 phan tu (muc 170 bo che do ma khac)", kq.Length, 11);
            Kiem("(nth 9) = xep cum (mac dinh T)", kq[9], true);
            Kiem("(nth 10) = dat ma khac (mac dinh T)", kq[10], true);
            var ts122 = GroThamSo.DocThamSo(new object[] { "K", null, null, null, null, null, null, 4, true, null, null, true }, out _);
            Kiem("tham so 10..11: xep cum nil -> tat", ts122.XepCum, false);
            Kiem("tham so 10..11: dat mk T", ts122.DatMk, true);
            var tsThua = GroThamSo.DocThamSo(new object[] { "K", null, null, null, null, null, null, 4, true, null, true, null, 1 }, out _);
            Kiem("lisp cu con gui che do (tham so 12) -> bo qua, khong loi", tsThua.DatMk, false);
            var tsCu = GroThamSo.DocThamSo(new object[] { "K", null, null, null, null, null, null, 4, true }, out _);
            Kiem("lisp cu (thieu 10..11) -> mac dinh bat", tsCu.XepCum + "/" + tsCu.DatMk, "True/True");
            Kiem("(nth 0) = use-vung", kq[0], true);
            Kiem("(nth 1) = layer VUNG (khong phai khung!)", kq[1], "VUNG");
            Kiem("(nth 2) = layer KHUNG", kq[2], "KHUNG");
            Kiem("(nth 3) = in PDF", kq[3], false);
            Kiem("(nth 5) = layer goc", kq[5], "GOC");
            Kiem("(nth 7) = mau Dim", kq[7], 3);
            Kiem("(nth 8) = tao khung", kq[8], true);
            Kiem("layer rong -> nil", GroThamSo.KetQua(new GroSettings { LayerKhung = "" })[2], null);
            Kiem("mau ngoai 1..255 -> 4", GroThamSo.KetQua(new GroSettings { MauDim = 300 })[7], 4);
            var vong = GroThamSo.DocThamSo(new object[] { kq[2], kq[0], kq[1], kq[3], kq[4], kq[5], kq[6], kq[7], kq[8] }, out _);
            Kiem("di mot vong ket qua -> tham so giu nguyen layer vung", vong.LayerVung, "VUNG");
            Kiem("   ... va layer khung", vong.LayerKhung, "KHUNG");

            Console.WriteLine();
            Console.WriteLine("=== Bang so lieu: dong F-name ===");
            Kiem("'N=26015-1-3' la dong N", BangSoLieu.PhanLoai("N=26015-1-3"), LoaiDongFname.N);
            Kiem("'t=6.0' (chu thuong) la dong T", BangSoLieu.PhanLoai("t=6.0"), LoaiDongFname.T);
            Kiem("'V=' la dong be", BangSoLieu.PhanLoai("V="), LoaiDongFname.V);
            Kiem("'SCALE 1:10' khong phai F-name", BangSoLieu.PhanLoai("SCALE 1:10"), LoaiDongFname.Khac);
            Kiem("gia tri 'Q= 2 ' -> '2'", BangSoLieu.GiaTri("Q= 2 "), "2");
            Kiem("viet lai N= chi doan so cuoi", BangSoLieu.VietLaiDong("N=26015-1-3", "12", null, null, null), "N=26015-1-12");
            Kiem("viet lai T= giu tien to goc", BangSoLieu.VietLaiDong("t=6.0", null, "9.0", null, null), "t=9.0");
            Kiem("viet lai: tham so null -> giu nguyen", BangSoLieu.VietLaiDong("Q=2", "5", "6.0", null, "SS400"), "Q=2");
            Kiem("viet lai: V= khong bao gio doi", BangSoLieu.VietLaiDong("V=", "5", "6.0", "3", "SS400"), "V=");
            Kiem("viet lai: Z= khong bao gio doi", BangSoLieu.VietLaiDong("Z=SPC", "5", "6.0", "3", "SS400"), "Z=SPC");
            var dongMt = BangSoLieu.TachDongMText("N=AB-3\\PM=SPHC\\PT=3.2\\PQ=1\\PV=\\PZ=SPC\\P");
            Kiem("MTEXT FN cu tach theo \\P", dongMt.Length, 7);
            Kiem("   noi lai nguyen ven", BangSoLieu.NoiDongMText(dongMt), "N=AB-3\\PM=SPHC\\PT=3.2\\PQ=1\\PV=\\PZ=SPC\\P");
            Kiem("   co dong be", dongMt.Any(GroText.LaDongBe), true);

            Console.WriteLine();
            Console.WriteLine("=== Chuan hoa T (giong fn--fmt-t) ===");
            Kiem("'6' -> 6.0", BangSoLieu.ChuanHoaT("6"), "6.0");
            Kiem("'6.00' -> 6.0", BangSoLieu.ChuanHoaT("6.00"), "6.0");
            Kiem("'4.5' -> 4.5", BangSoLieu.ChuanHoaT("4.5"), "4.5");
            Kiem("'3,2' (dau phay) -> 3.2", BangSoLieu.ChuanHoaT("3,2"), "3.2");
            Kiem("'abc' -> null", BangSoLieu.ChuanHoaT("abc"), null);
            Kiem("'0' -> null", BangSoLieu.ChuanHoaT("0"), null);

            Console.WriteLine();
            Console.WriteLine("=== Kiem tra truoc khi luu ===");
            var bangLuu = new List<DongBang>
            {
                new DongBang { GocBango = "1", Bango = "1", T = "6.0", Q = "2", M = "SS400" },
                new DongBang { GocBango = "2", Bango = "1", T = "6.0", Q = "2", M = "SS400", DaSua = true },
                new DongBang { GocBango = "?", Bango = "?", LaCoDon = true },
                new DongBang { GocBango = "?", Bango = "?", LaCoDon = true },
            };
            var loi = BangSoLieu.KiemTraTruocKhiLuu(bangLuu);
            Kiem("sua dong 2 thanh bango 1 -> bao TRUNG voi dong chua sua", loi.Count, 1);
            Kiem("   noi dung", loi.FirstOrDefault(), "Bango 1 bị TRÙNG ở 2 tấm (bango lúc quét: 1, 2).");
            bangLuu[1].Bango = "2";
            Kiem("doi thanh 2 -> het loi; hai dong co don '?' khong tinh trung",
                 BangSoLieu.KiemTraTruocKhiLuu(bangLuu).Count, 0);
            bangLuu[1].Bango = "02"; bangLuu[1].T = "x"; bangLuu[1].Q = "0"; bangLuu[1].M = " ";
            Kiem("bango/T/Q/M sai deu bao", BangSoLieu.KiemTraTruocKhiLuu(bangLuu).Count, 4);
            bangLuu[0].Q = "0";
            Kiem("dong CHUA SUA sai dinh dang -> khong chan luu", BangSoLieu.KiemTraTruocKhiLuu(bangLuu).Count, 4);

            Console.WriteLine();
            Console.WriteLine("=== Text thuoc khung (giao nhu ssget C) ===");
            var khung = new double[] { 0, 0, 100, 50 };
            Kiem("text nam han trong", BangSoLieu.HopGiao(new double[] { 10, 10, 20, 15 }, khung), true);
            Kiem("text cat mep phai", BangSoLieu.HopGiao(new double[] { 95, 10, 130, 15 }, khung), true);
            Kiem("text cham dung mep", BangSoLieu.HopGiao(new double[] { 100, 10, 110, 15 }, khung), true);
            Kiem("text ngoai 5mm (ban cu tinh la trong vi le 10mm)",
                 BangSoLieu.HopGiao(new double[] { 105, 10, 120, 15 }, khung), false);

            Console.WriteLine();
            Console.WriteLine("=== CSV cua GRO ===");
            Kiem("ten don  '26015-1-S.dwg' -> 26015-1", CsvGro.TenDon("26015-1-S.dwg"), "26015-1");
            Kiem("ten don  'test.dwg' -> test", CsvGro.TenDon("test.dwg"), "test");
            Kiem("ten don  'plan-a.dwg' -> plan (giong lisp, cat ca chu a)", CsvGro.TenDon("plan-a.dwg"), "plan");
            // Dung dung dinh dang file mau tren may: BOM + CRLF, T ghi gon "9"
            string goc = "test-1,1,SS400,9,test-\r\ntest-2,2,SS400,9,test-,m\r\ntest-3,1,SPHC,3.20,test-,k\r\n";
            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
            byte[] du = bom.Concat(Encoding.UTF8.GetBytes(goc)).ToArray();
            var csv = CsvGro.Doc(du);
            Kiem("doc 3 dong (bo dong rong cuoi)", csv.Dong.Count, 3);
            Kiem("tim dong bango 2", csv.TimDong("2"), 1);
            Kiem("GC tam don = rong", csv.LayGC(0), "");
            Kiem("GC tam be = m", csv.LayGC(1), "m");
            Kiem("GC tam vat = k", csv.LayGC(2), "k");
            Kiem("ghi lai NGUYEN VEN tung byte khi khong sua", Convert.ToBase64String(csv.Ghi()), Convert.ToBase64String(du));
            Kiem("T o cu '9' -> ghi gon", CsvGro.DinhDangT("12", "9"), "12");
            Kiem("T o cu '3.20' -> giu 2 so le", CsvGro.DinhDangT("4.5", "3.20"), "4.50");
            Kiem("T o cu '9', moi 4.5 -> 4.5", CsvGro.DinhDangT("4.5", "9"), "4.5");

            var cb = new List<string>();
            int nSua = csv.ApDung(new List<SuaCsv>
            {
                // doi CHEO so 1 <-> 2, va sua Q cua tam 1
                new SuaCsv { GocBango = "1", BangoMoi = "2", Q = "5" },
                new SuaCsv { GocBango = "2", BangoMoi = "1" },
                new SuaCsv { GocBango = "3", T = "4.5" },
                new SuaCsv { GocBango = "9", Q = "1" },
            }, cb);
            Kiem("sua 3 dong (tam 9 khong co trong CSV)", nSua, 3);
            Kiem("doi cheo: dong 1 thanh bango 2, Q=5, giu M/T/X", csv.Dong[0], "test-2,5,SS400,9,test-");
            Kiem("doi cheo: dong 2 thanh bango 1, GIU cot V=m", csv.Dong[1], "test-1,2,SS400,9,test-,m");
            Kiem("chi sua T, giu kieu 2 so le", csv.Dong[2], "test-3,1,SPHC,4.50,test-,k");
            Kiem("bao tam 9 khong co trong CSV", cb.Count(x => x.Contains("tấm 9")), 1);
            Kiem("khong co canh bao trung", cb.Count(x => x.Contains("cùng bango")), 0);

            var csv2 = CsvGro.Doc(du);
            var cb2 = new List<string>();
            csv2.ApDung(new List<SuaCsv> { new SuaCsv { GocBango = "1", BangoMoi = "3" } }, cb2);
            Kiem("doi so dung mot dong CSV khac -> canh bao trung", cb2.Count(x => x.Contains("cùng bango 3")), 1);
            Kiem("file CP932 cu (khong BOM, khong phai UTF-8) van doc duoc",
                 CsvGro.Doc(Encoding.GetEncoding(932).GetBytes("ア-1,1,SS400,9,ア-\r\n")).TimDong("1"), 0);

            Console.WriteLine();
            Console.WriteLine("=== Loi du lieu tam (to noi bat trong bang) ===");
            Kiem("tam dung: Q=2 T=6.0 M=SS400", KiemTraTam.Loi("2", "6.0", "SS400", null, null).Count, 0);
            Kiem("tam dung: T=3.2 SPHC", KiemTraTam.Loi("1", "3.2", "SPHC", null, null).Count, 0);
            Kiem("thieu Q (Error Q cua gro)", string.Join("|", KiemTraTam.Loi("", "6.0", "SS400", null, null)), "Thiếu Q");
            Kiem("Q khong phai so nguyen", string.Join("|", KiemTraTam.Loi("1.5", "6.0", "SS400", null, null)), "Q sai \"1.5\"");
            Kiem("thieu T", string.Join("|", KiemTraTam.Loi("1", "", "SS400", null, null)), "Thiếu T");
            Kiem("T=5 khong thuoc bang chuan (Error T cua gro)",
                 string.Join("|", KiemTraTam.Loi("1", "5.0", "SS400", null, null)), "T=5.0 không chuẩn");
            Kiem("T=4.5 SPHC -> M khong khop (phai SS400)",
                 string.Join("|", KiemTraTam.Loi("1", "4.5", "SPHC", null, null)), "M không khớp T (T=4.5 phải SS400)");
            Kiem("T=3.2 SS400 -> M khong khop (phai SPHC)",
                 KiemTraTam.Loi("1", "3.2", "SS400", null, null).Count, 1);
            Kiem("vat lieu CPL (chu Nhat) -> khong xet luat M", KiemTraTam.Loi("1", "3.2", "ムキ 縞板", null, null).Count, 0);
            Kiem("thieu M", string.Join("|", KiemTraTam.Loi("1", "6.0", "", null, null)), "Thiếu M");
            Kiem("Q lech CSV", string.Join("|", KiemTraTam.Loi("2", "6.0", "SS400", "3", null)), "Q lệch CSV (3)");
            Kiem("T '6.0' vs CSV '6' -> KHONG lech (so so, khong so chuoi)", KiemTraTam.Loi("2", "6.0", "SS400", "2", "6").Count, 0);
            Kiem("T lech CSV", string.Join("|", KiemTraTam.Loi("2", "6.0", "SS400", "2", "9.00")), "T lệch CSV (9.00)");
            Kiem("T '6' (dang gro ghi '6.0') van la chuan", KiemTraTam.LaDoDayChuan(6), true);

            Console.WriteLine();
            Console.WriteLine("=== Sua GC (chi ghi CSV) ===");
            var csvGc = CsvGro.Doc(du);   // test-1 don, test-2 m, test-3 k
            var cbGc = new List<string>();
            csvGc.ApDung(new List<SuaCsv>
            {
                new SuaCsv { GocBango = "1", GC = "m" },   // don -> be: THEM cot 6
                new SuaCsv { GocBango = "2", GC = "" },    // be -> don: BO cot 6, khong de dau phay cuoi
                new SuaCsv { GocBango = "3", GC = "*" },   // vat -> tu 2 loai gia cong tro len
            }, cbGc);
            Kiem("don -> m: them cot V", csvGc.Dong[0], "test-1,1,SS400,9,test-,m");
            Kiem("m -> don: bo cot V (giong GRO ghi tam don)", csvGc.Dong[1], "test-2,2,SS400,9,test-");
            Kiem("k -> *", csvGc.Dong[2], "test-3,1,SPHC,3.20,test-,*");
            Kiem("GC hop le: '', m, k, *, 'M' (hoa)", new[] { "", "m", "k", "*", "M" }.All(CsvGro.LaGCHopLe), true);
            Kiem("GC 'x' khong hop le", CsvGro.LaGCHopLe("x"), false);
            Kiem("GC moi s / t / r (va 'S' hoa) hop le", new[] { "s", "t", "r", "S" }.All(CsvGro.LaGCHopLe), true);
            Kiem("GC ghep 'ms' KHONG hop le (moi tam mot ma)", CsvGro.LaGCHopLe("ms"), false);
            Kiem("ten t", CsvGro.TenGC("t"), "gia công M-タップ");
            Kiem("ten *", CsvGro.TenGC("*"), "từ 2 loại gia công khác nhau trở lên");
            Kiem("thong ke mot dong, chi liet ke ma co tam",
                 CsvGro.ThongKe(new[] { "", "", "m", "s", "s", "r" }, 1, 0, out string ctTk),
                 "Tổng 6 │ Đơn 2 │ m 1 · s 2 · r 1 │ Lỗi 1 │ Cô đơn 0");
            Kiem("thong ke khong co gia cong", CsvGro.ThongKe(new[] { "", "" }, 0, 2, out _), "Tổng 2 │ Đơn 2 │ Lỗi 0 │ Cô đơn 2");
            Kiem("chi tiet (tooltip) co du 6 ma", ctTk.Split('\n').Length, 10);
            Kiem("kiem tra truoc khi luu bat GC sai",
                 BangSoLieu.KiemTraTruocKhiLuu(new List<DongBang> { new DongBang { GocBango = "1", Bango = "1", Q = "1", T = "6.0", M = "SS400", GC = "x", DaSua = true } }).Count, 1);

            Console.WriteLine();
            Console.WriteLine("=== Tab DSN: Phan loai va gom nhom net (LineClassifier) ===");
            Kiem("chuan hoa linetype rong", LineClassifier.ChuanHoaLinetype(""), "Continuous");
            Kiem("chuan hoa linetype null", LineClassifier.ChuanHoaLinetype(null), "Continuous");
            Kiem("chuan hoa linetype trim", LineClassifier.ChuanHoaLinetype("  Hidden  "), "Hidden");

            var rawNet = new List<RawLineItem>
            {
                new RawLineItem { Linetype = "Dashed", ColorIndex = 7, EntityId = 101, MinX = 10, MinY = 20, MaxX = 100, MaxY = 200 },
                new RawLineItem { Linetype = "Dashed", ColorIndex = 1, EntityId = 102, MinX = 5, MinY = 15, MaxX = 120, MaxY = 220 }, // Khác màu nhưng cùng linetype
                new RawLineItem { Linetype = "Hidden", ColorIndex = 4, EntityId = 103, MinX = 30, MinY = 40, MaxX = 50, MaxY = 60 }
            };
            var nhomNet = LineClassifier.GomNhom(rawNet);
            Kiem("gom duoc 2 nhom (bo qua mau sac)", nhomNet.Count, 2);
            Kiem("nhom nhieu nhat dung dau", nhomNet[0].Linetype, "Dashed");
            Kiem("so luong nhom Dashed gom ca 2 mau", nhomNet[0].Count, 2);
            Kiem("minX bao trum dung", nhomNet[0].MinX, 5.0);
            Kiem("maxX bao trum dung", nhomNet[0].MaxX, 120.0);
            Kiem("nhom Hidden dung sau", nhomNet[1].Linetype, "Hidden");
            Kiem("so luong nhom Hidden", nhomNet[1].Count, 1);
            Kiem("thong ke DSN co net", LineClassifier.ThongKeDsn(3, 2), "Tổng 3 nét chưa đổi │ 2 nhóm kiểu nét");
            Kiem("thong ke DSN het net", LineClassifier.ThongKeDsn(0, 0), "Không còn nét nào chưa đổi");

            Console.WriteLine();
            Console.WriteLine("=== Tu dien dich ghi chu Nhat - Viet (JapanNoteTranslator) ===");
            JapanNoteTranslator.ResetToDefaults();
            Kiem("dich 'ｹｶﾞｷ線' (half-width)", JapanNoteTranslator.Translate("ｹｶﾞｷ線"), "khắc kegaki");
            Kiem("dich 'ケガキ線' (full-width)", JapanNoteTranslator.Translate("ケガキ線"), "khắc kegaki");
            JapanNoteTranslator.Register("裏面CPL", "Mặt sau CPL");
            Kiem("dang ky tu moi -> dich '裏面CPL'", JapanNoteTranslator.Translate("裏面CPL"), "Mặt sau CPL");
            Kiem("extract 'chu co ｹｶﾞｷ線 trong cau'", JapanNoteTranslator.ExtractAndTranslate("chu co ｹｶﾞｷ線 trong cau"), "khắc kegaki");
            Kiem("extract 'chu co 裏面CPL trong cau'", JapanNoteTranslator.ExtractAndTranslate("tam 5 (裏面CPL)"), "Mặt sau CPL");
            Kiem("extract text khong co tu dien -> rong", JapanNoteTranslator.ExtractAndTranslate("100x200 SPHC 3.2"), string.Empty);
            var items = JapanNoteTranslator.GetItems();
            Kiem("GetItems co chua tu kegaki", items.Any(x => x.Japanese == "ｹｶﾞｷ線"), true);

            // Test Google Sheet URL conversion
            string convertedUrl = JapanNoteTranslator.ChuyenDoiUrlGoogleSheet("https://docs.google.com/spreadsheets/d/1gmtbKIS56uaLkMNMWybrds9uJhyz3wIBuVrEbZWMtOw/edit?gid=0#gid=0");
            Kiem("ChuyenDoiUrlGoogleSheet sinh dung link export CSV", convertedUrl.Contains("export?format=csv&gid=0"), true);

            // Test ParseCsv
            string sampleCsv = "Tiếng Nhật,Ý nghĩa\nケガキ線,Khắc kegaki\n裏面CPL,Mặt sau CPL\n\"Có dấu phẩy, từ khóa\",Ý nghĩa đặc biệt";
            var parsedItems = JapanNoteTranslator.ParseCsv(sampleCsv);
            Kiem("ParseCsv bo qua header va dem dung 3 dong", parsedItems.Count, 3);
            Kiem("ParseCsv dong 1: ケガキ線", parsedItems[0].Japanese, "ケガキ線");
            Kiem("ParseCsv dong 2: Mặt sau CPL", parsedItems[1].Vietnamese, "Mặt sau CPL");
            Kiem("ParseCsv dong 3 xu ly quote phay: Có dấu phẩy, từ khóa", parsedItems[2].Japanese, "Có dấu phẩy, từ khóa");

            // Test SyncFromGoogleSheet voi sheet mau that cua nguoi dung
            var syncKq = JapanNoteTranslator.SyncFromGoogleSheet("https://docs.google.com/spreadsheets/d/1gmtbKIS56uaLkMNMWybrds9uJhyz3wIBuVrEbZWMtOw/edit?gid=0#gid=0");
            Kiem("SyncFromGoogleSheet that thanh cong", syncKq.Success, true);
            Kiem("SyncFromGoogleSheet co it nhat 2 tu khoa", syncKq.Count >= 2, true);

            // Tests đồng bộ ngược (2-way sync & serialization)
            Console.WriteLine();
            Console.WriteLine("=== Dong bo nguoc len Google Sheet (2-way sync) ===");
            var testItems = new List<NoteDictionaryItem>
            {
                new NoteDictionaryItem { Japanese = "ｹｶﾞｷ線", Vietnamese = "khắc kegaki" },
                new NoteDictionaryItem { Japanese = "皿モミ", Vietnamese = "vát mép ốc chìm" }
            };
            string json = JapanNoteTranslator.ToJson(testItems);
            Kiem("ToJson chua 'ｹｶﾞｷ線'", json.Contains("ｹｶﾞｷ線"), true);
            Kiem("ToJson chua '皿モミ'", json.Contains("皿モミ"), true);
            Kiem("ToJson bat dau bang '['", json.StartsWith("["), true);

            string tsv = JapanNoteTranslator.ToTsv(testItems);
            Kiem("ToTsv chua ky tu tab", tsv.Contains("\t"), true);
            Kiem("ToTsv dong 1 chua Tieng Nhat", tsv.StartsWith("Tiếng Nhật\tTiếng Việt"), true);

            var parsedJson = JapanNoteTranslator.ParseResponse(json);
            Kiem("ParseResponse doc duoc JSON 2 phan tu", parsedJson.Count, 2);
            Kiem("ParseResponse phan tu 1 Japanese", parsedJson[0].Japanese, "ｹｶﾞｷ線");
            Kiem("ParseResponse phan tu 2 Vietnamese", parsedJson[1].Vietnamese, "vát mép ốc chìm");

            Kiem("AppsScriptTemplate co doPost", JapanNoteTranslator.AppsScriptTemplate.Contains("doPost"), true);
            Kiem("AppsScriptTemplate co doGet", JapanNoteTranslator.AppsScriptTemplate.Contains("doGet"), true);

            JapanNoteTranslator.SetAutoUpload(true);
            Kiem("GetAutoUpload tra ve true khi set true", JapanNoteTranslator.GetAutoUpload(), true);
            JapanNoteTranslator.SetAutoUpload(false);
            Kiem("GetAutoUpload tra ve false khi set false", JapanNoteTranslator.GetAutoUpload(), false);

            JapanNoteTranslator.SetScriptUrl("https://script.google.com/macros/s/test/exec");
            Kiem("GetScriptUrl tra ve dung link", JapanNoteTranslator.GetScriptUrl(), "https://script.google.com/macros/s/test/exec");
            JapanNoteTranslator.SetScriptUrl(string.Empty);

            var uploadTest = JapanNoteTranslator.UploadToGoogleSheetAsync(testItems, "https://docs.google.com/spreadsheets/d/test").Result;
            Kiem("Upload voi link Sheet thuong tra ve NEED_SETUP", uploadTest.Message, "NEED_SETUP");

            JapanNoteTranslator.ResetToDefaults();

            Console.WriteLine();
            Console.WriteLine("=== PlateItem: Ghi chu, Cặp đối xứng, Cham trang thai & Khong co hinh cat ===");
            var pNormal = new PlateItem { Bango = "1", Quantity = "2", Thickness = "6.0", Material = "SS400" };
            pNormal.DanhDauDaKhop();
            Kiem("tam binh thuong: Note rong (xoa Da luu / Khop)", pNormal.Note, string.Empty);
            Kiem("tam binh thuong: StatusColor la Xanh", pNormal.StatusColor, "#43A047");
            Kiem("tam binh thuong: MucDo = 0", pNormal.MucDo, 0);

            var pHonzu = new PlateItem { Bango = "28", Role = "HONZU", Quantity = "2", Thickness = "6.0", Material = "SS400" };
            pHonzu.DanhDauDaKhop();
            Kiem("tam Honzu trong cap: Note la 'Bản chính'", pHonzu.Note, "Bản chính");
            Kiem("tam Honzu: StatusColor la Xanh", pHonzu.StatusColor, "#43A047");

            var pGyaku = new PlateItem { Bango = "27", Role = "GYAKUZU", PrimaryBango = "28", Quantity = "2", Thickness = "6.0", Material = "SS400" };
            pGyaku.DanhDauDaKhop();
            Kiem("tam Gyakuzu: Note la 'Bản nghịch - 28'", pGyaku.Note, "Bản nghịch - 28");

            var pNote = new PlateItem { Bango = "3", CustomerNote = "khắc kegaki", Quantity = "1", Thickness = "3.2", Material = "SPHC" };
            pNote.DanhDauDaKhop();
            Kiem("tam co CustomerNote: Note la 'khắc kegaki'", pNote.Note, "khắc kegaki");

            var pNoShape = new PlateItem { Bango = "4", NoCuttingShape = true, Quantity = "1", Thickness = "3.2", Material = "SPHC" };
            pNoShape.DanhDauDaKhop();
            Kiem("tam khong tim thay hinh cat: Note bao loi", pNoShape.Note, "[!] Không tìm thấy hình cắt");
            Kiem("tam khong tim thay hinh cat: StatusColor la Do", pNoShape.StatusColor, "#DC2626");
            Kiem("tam khong tim thay hinh cat: MucDo = 2 (Do)", pNoShape.MucDo, 2);

            var pMulti = new PlateItem
            {
                Bango = "27",
                Role = "GYAKUZU",
                PrimaryBango = "28",
                CustomerNote = "khắc kegaki",
                NoCuttingShape = true,
                Quantity = "1",
                Thickness = "3.2",
                Material = "SPHC"
            };
            pMulti.DanhDauDaKhop();
            Kiem("tam vua co ghi chu vua co loi: chua '\\n'", pMulti.Note.Contains("\n"), true);
            Kiem("tam vua co ghi chu vua co loi: noi dung 3 dong", pMulti.Note, "Bản nghịch - 28\nkhắc kegaki\n[!] Không tìm thấy hình cắt");

            Console.WriteLine();
            Console.WriteLine("=== Tab Thong Tin: phan loai dong bao cao cuoi lenh GRO (muc 105) ===");
            // Bao cao THAT tu anh nguoi dung gui (lisp ghep bang "\n", ASCII thuan).
            string baoCao = "20 ghi chu -> 28 tam:\n  28 bango (layer 0), kieu chu guro\n  28 ma khac I-1- (layer 3-KEGAKI)"
                          + "\nDa danh so tu 1 den 28\n\n*** CANH BAO: CO 1 HINH CO DON!\n"
                          + "  + Khong thuoc bat ky text thong tin nao hoac nam ngoai vung.\n"
                          + "[x] Tu dong in nhanh PDF theo layer: 0"
                          + "\n\n--- THONG BAO XU LY NET (Layer \"_0-0_\") ---\n  + 373 net Continuous -> 0-KAKOU"
                          + "\n  -> CON LAI 14 DOI TUONG GIU NGUYEN (KHONG DOI):\n     * Net DASHED1: 2 doi tuong\n";
            var dongBc = ThongTinGro.PhanDong(baoCao);
            Kiem("dong tong 'N ghi chu -> M tam' la TieuDe", dongBc[0].Loai, LoaiDongThongTin.TieuDe);
            Kiem("dong thuong giu nguyen thut dau (da dich)", dongBc[1].Text, "  28 bango (layer 0), kiểu chữ guro");
            Kiem("dong thuong giu chuoi GOC ASCII", dongBc[1].Goc, "  28 bango (layer 0), kieu chu guro");
            Kiem("dong thuong la BinhThuong", dongBc[1].Loai, LoaiDongThongTin.BinhThuong);
            Kiem("'*** CANH BAO ...' la CanhBao", dongBc.First(d => d.Text.StartsWith("***")).Loai, LoaiDongThongTin.CanhBao);
            Kiem("'--- THONG BAO XU LY NET ---' la TieuDe", dongBc.First(d => d.Text.StartsWith("---")).Loai, LoaiDongThongTin.TieuDe);
            Kiem("dong cuoi trong bi cat bo", dongBc[dongBc.Count - 1].Text, "     * Nét DASHED1: 2 đối tượng");
            Kiem("nhieu dong trong lien nhau gop con 1",
                 ThongTinGro.PhanDong("a\n\n\n\nb").Count, 3);
            Kiem("dem canh bao + loi = 1", ThongTinGro.DemCanhBao(dongBc), 1);
            Kiem("'Error T!!!' la Loi", ThongTinGro.PhanLoai("Error T!!!"), LoaiDongThongTin.Loi);
            Kiem("'KHONG GHI DUOC FILE CSV' la Loi", ThongTinGro.PhanLoai("KHONG GHI DUOC FILE CSV"), LoaiDongThongTin.Loi);
            Kiem("'... -> *** GHI THAT BAI ***' la Loi (khong phai CanhBao)",
                 ThongTinGro.PhanLoai("  28 dong CSV (GHI DE) -> *** GHI THAT BAI ***"), LoaiDongThongTin.Loi);
            Kiem("'!!! SO TAM NHO HON SO GHI CHU' la Loi",
                 ThongTinGro.PhanLoai("!!! SO TAM NHO HON SO GHI CHU - co tam bi mat, bao loi ngay!"), LoaiDongThongTin.Loi);
            Kiem("'!!! 2 ghi chu KHONG TIM THAY ...' la CanhBao",
                 ThongTinGro.PhanLoai("!!! 2 ghi chu KHONG TIM THAY HINH VE CAT GIA CONG:"), LoaiDongThongTin.CanhBao);
            Kiem("'-> DA DOI HET!' la ThanhCong",
                 ThongTinGro.PhanLoai("  -> DA DOI HET! Khong con doi tuong nao tren layer goc."), LoaiDongThongTin.ThanhCong);
            Kiem("chuoi rong -> 0 dong", ThongTinGro.PhanDong("").Count, 0);
            Kiem("chuoi null -> 0 dong", ThongTinGro.PhanDong(null).Count, 0);
            Kiem("xuong dong CRLF cung tach dung", ThongTinGro.PhanDong("a\r\nb").Count, 2);

            Console.WriteLine();
            Console.WriteLine("=== Lam moi: xoa dong CSV cua tam da xoa khoi ban ve (muc 106) ===");
            var csvXoa = CsvGro.Doc(Encoding.UTF8.GetBytes(
                "129696-1-1,2,SS400,6.00,129696-1-\r\n" +
                "129696-1-2,4,SS400,6.00,129696-1-,m\r\n" +
                "129696-1-3,1,SPHC,3.20,129696-1-\r\n" +
                "dong la khong co bango\r\n" +
                "129696-1-12,2,SS400,6.00,129696-1-\r\n"));
            List<int> thuaX = csvXoa.DongThua(new[] { "1", "3", "12" });
            Kiem("ban ve con 1,3,12 -> dong thua = [1] (bango 2)", string.Join(",", thuaX), "1");
            Kiem("dong la (khong doc duoc bango) KHONG bi coi la thua",
                 csvXoa.DongThua(new[] { "1", "2", "3", "12" }).Count, 0);
            Kiem("bango '1' KHONG khop nham dong bango 12", csvXoa.DongThua(new[] { "1", "2", "3" }).Count, 1);
            Kiem("bango co khoang trang ' 3 ' van khop", csvXoa.DongThua(new[] { "1", "2", " 3 ", "12" }).Count, 0);
            Kiem("danh sach rong -> moi dong co bango deu thua (nguoi goi PHAI chan truong hop nay)",
                 csvXoa.DongThua(new string[0]).Count, 4);
            csvXoa.XoaDong(new[] { 4, 1, 1 });
            Kiem("xoa dong 1 va 4 (trung lap, sai thu tu) -> con 3 dong", csvXoa.Dong.Count, 3);
            Kiem("thu tu dong con lai giu nguyen",
                 string.Join("|", csvXoa.Dong.Select(d => CsvGro.BangoCua(d) ?? "-")), "1|3|-");
            csvXoa.XoaDong(new[] { -1, 99 });
            Kiem("chi so ngoai pham vi bi bo qua", csvXoa.Dong.Count, 3);
            Kiem("ghi lai van UTF-8 BOM + CRLF",
                 BitConverter.ToString(csvXoa.Ghi().Take(3).ToArray()) + "|" + Encoding.UTF8.GetString(csvXoa.Ghi(), 3, csvXoa.Ghi().Length - 3).EndsWith("\r\n"),
                 "EF-BB-BF|True");

            Console.WriteLine();
            Console.WriteLine("=== Tab Thong Tin: dich sang tieng Viet co dau (muc 107) ===");
            // Tung dong THAT trong anh bao cao nguoi dung gui + cac dong canh bao khac.
            var mauDich = new[]
            {
                new[] { "20 ghi chu -> 28 tam:", "20 ghi chú → 28 tấm:" },
                new[] { "  28 bango (layer 0), kieu chu guro", "  28 bango (layer 0), kiểu chữ guro" },
                new[] { "  28 bango (layer 0), kieu chu guro *** 3 CAI DAT KHONG DUOC ***",
                        "  28 bango (layer 0), kiểu chữ guro  *** 3 bango KHÔNG đặt được kiểu chữ ***" },
                new[] { "  28 ma khac I-1- (layer 3-KEGAKI)", "  28 mã khắc I-1- (layer 3-KEGAKI)" },
                new[] { "  28 F-name (layer 2-F-NAME)", "  28 F-name (layer 2-F-NAME)" },
                new[] { @"  28 dong CSV (GHI DE) -> C:\DTK\129696-1.csv", @"  28 dòng CSV (ghi đè) → C:\DTK\129696-1.csv" },
                new[] { "  28 dong CSV (GHI DE) -> *** GHI THAT BAI ***", "  28 dòng CSV (ghi đè) → *** GHI THẤT BẠI ***" },
                new[] { "  (da xoa 28 dong cu trong file CSV)", "  (đã xoá 28 dòng cũ trong file CSV)" },
                new[] { "Da danh so tu 1 den 28", "Đã đánh số từ 1 đến 28" },
                new[] { "*** CANH BAO: CO 1 HINH CO DON!", "*** CẢNH BÁO: CÓ 1 HÌNH CÔ ĐƠN!" },
                new[] { "  + Khong thuoc bat ky text thong tin nao hoac nam ngoai vung.",
                        "  + Không thuộc text thông tin nào, hoặc nằm ngoài vùng." },
                new[] { "  + Da khoanh khung chu nhat mau DO tren layer \"CANH_BAO_CO_DON\".",
                        "  + Đã khoanh khung chữ nhật màu ĐỎ trên layer \"CANH_BAO_CO_DON\"." },
                new[] { "[x] Tu dong in nhanh PDF theo layer: 0", "[x] Tự động in nhanh PDF theo layer: 0" },
                new[] { "--- THONG BAO XU LY NET (Layer \"_0-0_\") ---", "--- XỬ LÝ NÉT (layer gốc \"_0-0_\") ---" },
                new[] { "  + 373 net Continuous -> 0-KAKOU", "  + 373 nét Continuous → 0-KAKOU" },
                new[] { "  + 88 net Center1 -> 3-KEGAKI", "  + 88 nét Center1 → 3-KEGAKI" },
                new[] { "  + 51 text -> 1-SUNPOU", "  + 51 text → 1-SUNPOU" },
                new[] { "  -> CON LAI 14 DOI TUONG GIU NGUYEN (KHONG DOI):", "  → CÒN LẠI 14 ĐỐI TƯỢNG GIỮ NGUYÊN (CHƯA ĐỔI):" },
                new[] { "  -> DA DOI HET! Khong con doi tuong nao tren layer goc.", "  → ĐÃ ĐỔI HẾT! Không còn đối tượng nào trên layer gốc." },
                new[] { "     * Net DASHED2: 12 doi tuong", "     * Nét DASHED2: 12 đối tượng" },
                new[] { "  (Xem tab Net Chua Doi de zoom kiem tra)", "  (Xem tab Nét Chưa Đổi để zoom kiểm tra)" },
                new[] { "!!! 2 ghi chu KHONG TIM THAY HINH VE CAT GIA CONG:", "!!! 2 ghi chú KHÔNG TÌM THẤY HÌNH VẼ CẮT GIA CÔNG:" },
                new[] { "      Text #5 [t=6mm 2枚]  tai X=120.5 Y=-30.0", "      Text #5 [t=6mm 2枚]  tại X=120.5 Y=-30.0" },
                new[] { "Error T!!!", "Lỗi T!!! (độ dày không có trong danh sách độ dày chuẩn)" },
                new[] { "KHONG GHI DUOC FILE CSV", "KHÔNG GHI ĐƯỢC FILE CSV" },
                new[] { "  I-1-12 T=7.5", "  I-1-12 T=7.5" },
                new[] { "cau la chua co mau dich", "cau la chua co mau dich" },
            };
            foreach (var md in mauDich) Kiem("dich: " + md[0].Trim(), ThongTinGro.Dich(md[0]), md[1]);
            Kiem("dich xong VAN phan loai mau theo chuoi goc: 'Error T!!!' -> Loi",
                 ThongTinGro.PhanDong("Error T!!!")[0].Loai, LoaiDongThongTin.Loi);

            Console.WriteLine();
            Console.WriteLine("=== Lam moi: THEM dong CSV cho tam chua co (muc 107) ===");
            Kiem("tam don: 5 cot, khong dau phay cuoi",
                 CsvGro.TaoDong("129696-1", "29", "4", "SS400", "6.00", ""), "129696-1-29,4,SS400,6.00,129696-1-");
            Kiem("tam be: them cot V",
                 CsvGro.TaoDong("129696-1", "30", "2", "SPHC", "3.20", "m"), "129696-1-30,2,SPHC,3.20,129696-1-,m");
            Kiem("dong tao ra doc lai dung bango", CsvGro.BangoCua(CsvGro.TaoDong("129696-1", "30", "2", "SPHC", "3.20", "")), "30");
            Kiem("M theo GRO: T=6 -> SS400 (du F-name ghi SPHC)", CsvGro.VatLieuCsv("6.00", "SPHC"), "SS400");
            Kiem("M theo GRO: T=4.0 -> SS400 (F-name nguong 4.5 ghi SPHC)", CsvGro.VatLieuCsv("4.0", "SPHC"), "SS400");
            Kiem("M theo GRO: T=3.2 -> SPHC (nguong > 3.2)", CsvGro.VatLieuCsv("3.20", "SS400"), "SPHC");
            Kiem("M vat lieu khac (CPL) giu nguyen", CsvGro.VatLieuCsv("4.5", "ムキ 縞板"), "ムキ 縞板");
            Kiem("M vat lieu SUS giu nguyen", CsvGro.VatLieuCsv("2.0", "SUS304"), "SUS304");
            var csvThem = CsvGro.Doc(Encoding.UTF8.GetBytes(
                "129696-1-1,2,SS400,6.00,129696-1-\r\n129696-1-5,1,SPHC,3.20,129696-1-\r\n129696-1-12,2,SS400,6.00,129696-1-\r\n"));
            Kiem("mau T lay tu dong dau co so", csvThem.MauT(), "6.00");
            Kiem("mau T: file rong -> 0.00", CsvGro.Doc(new byte[0]).MauT(), "0.00");
            Kiem("dinh dang T theo mau 6.00: 4.5 -> 4.50", CsvGro.DinhDangT("4.5", csvThem.MauT()), "4.50");
            Kiem("chen bango 7 giua 5 va 12 -> chi so 2", csvThem.ChenTheoBango(CsvGro.TaoDong("129696-1", "7", "1", "SS400", "6.00", "")), 2);
            Kiem("chen bango 3 giua 1 va 5 -> chi so 1", csvThem.ChenTheoBango(CsvGro.TaoDong("129696-1", "3", "1", "SS400", "6.00", "")), 1);
            Kiem("chen bango 40 lon nhat -> cuoi file", csvThem.ChenTheoBango(CsvGro.TaoDong("129696-1", "40", "1", "SS400", "6.00", "")), 5);
            Kiem("thu tu bango sau khi chen", string.Join(",", csvThem.Dong.Select(CsvGro.BangoCua)), "1,3,5,7,12,40");

            Console.WriteLine();
            Console.WriteLine("=== Tab Thong Tin dang BANG DIEU KHIEN: doc bao cao thanh so lieu (muc 108) ===");
            // Dung DUNG thu tu lisp ghep: bao cao tong + "\n\n" + canh bao gom (msg-tt) + File PDF.
            // So lieu lay tu anh mau nguoi dung gui.
            string bcDay =
                "24 ghi chu -> 33 tam:\n  33 bango (layer 0), kieu chu guro\n  33 ma khac I-1- (layer 3-KEGAKI)\n" +
                "  33 F-name (layer 2-F-NAME)\n  33 dong CSV (GHI DE) -> C:\\Users\\oftby\\OneDrive - OFT\\Desktop\\test.csv\n" +
                "  (da xoa 32 dong cu trong file CSV)\nDa danh so tu 1 den 33\nCo canh bao - xem dong lenh." +
                "\n\n*** CANH BAO: CO 1 HINH CO DON!\n  + Khong thuoc bat ky text thong tin nao hoac nam ngoai vung.\n" +
                "  + Da khoanh khung chu nhat mau DO tren layer \"CANH_BAO_CO_DON\".\n" +
                "  + Go lenh DST (hoac DS) de xem danh sach va zoom den tung tam." +
                "\n[x] Tu dong in nhanh PDF theo layer: 0" +
                "\n\n--- THONG BAO XU LY NET (Layer \"_0-8_\") ---\n  + 407 net Continuous -> 0-KAKOU\n" +
                "  + 162 net Center1 -> 3-KEGAKI\n  + 69 text -> 1-SUNPOU\n  -> CON LAI 25 DOI TUONG GIU NGUYEN (KHONG DOI):\n" +
                "     * Net DASHED1: 13 doi tuong\n     * Net DASHED3: 12 doi tuong\n  (Xem tab Net Chua Doi de zoom kiem tra)" +
                "\n\n!!! 2 ghi chu KHONG TIM THAY HINH VE CAT GIA CONG:\n      Text #5 [t=6mm 2枚]  tai X=120.5 Y=-30.0\n" +
                "      Text #9 [t=6mm 4枚]  tai X=880.0 Y=-30.0\n    Kiem tra lai net ve tren layer cat hoac vi tri dat text.\n\n" +
                "Error T!!!\n  I-1-12 T=7.5\n\n" +
                "\nFile PDF: C:\\Users\\oftby\\Desktop\\test-20261014.pdf";
            var bc = BaoCaoGro.Doc(bcDay);
            Kiem("doc duoc (co dong tong)", bc.DocDuoc, true);
            Kiem("the Ghi chu = 24", bc.SoGhiChu, (int?)24);
            Kiem("the Tam = 33", bc.SoTam, (int?)33);
            Kiem("Net da doi = 407+162+69 = 638", bc.Net.DaDoi, 638);
            Kiem("Chua doi = 25", bc.Net.ConLai, 25);
            Kiem("layer goc", bc.Net.Layer, "_0-8_");
            Kiem("2 nhom chua doi", string.Join(",", bc.Net.Nhom.Select(n => n.Linetype + "=" + n.SoLuong)), "DASHED1=13,DASHED3=12");
            Kiem("nhan nhom 'DASHED1 · chưa đổi'", bc.Net.Nhom[0].Nhan, "DASHED1 · chưa đổi");
            Kiem("dong ghi chu tren tam",
                 string.Join(" | ", bc.DongGhiChu().Select(d => d.Nhan + "=" + d.GiaTri + (d.Dat ? "✓" : "⚠"))),
                 "Bango · layer 0 · guro=33✓ | Mã khắc I-1- · 3-KEGAKI=33✓ | F-name · 2-F-NAME=33✓ | Đánh số=1 → 33✓");
            Kiem("dong net da doi",
                 string.Join(" | ", bc.DongNetDaDoi().Select(d => d.Nhan + "=" + d.GiaTri)),
                 "Continuous → 0-KAKOU=407 | Center1 → 3-KEGAKI=162 | Text → 1-SUNPOU=69");
            Kiem("nhan CSV", bc.NhanCsv, "CSV ghi đè (xoá 32 dòng cũ)");
            Kiem("duong CSV day du", bc.DuongCsv, @"C:\Users\oftby\OneDrive - OFT\Desktop\test.csv");
            Kiem("duong CSV rut gon 3 doan", BaoCaoGro.RutGonDuong(bc.DuongCsv), @"…\OneDrive - OFT\Desktop\test.csv");
            Kiem("PDF theo layer 0", bc.PdfLayer, "0");
            Kiem("duong PDF", bc.DuongPdf, @"C:\Users\oftby\Desktop\test-20261014.pdf");
            Kiem("so khoi canh bao", bc.CanhBao.Count, 4);
            Kiem("khoi 'co canh bao' (khong chi tiet)", bc.CanhBao[0].Loai + "/" + bc.CanhBao[0].ChiTiet.Count, "Khac/0");
            Kiem("khoi hinh co don: loai + 3 dong chi tiet + co nut danh sach",
                 bc.CanhBao[1].Loai + "/" + bc.CanhBao[1].ChiTiet.Count + "/" + bc.CanhBao[1].CoNutDanhSach, "CoDon/3/True");
            Kiem("khoi hinh co don: tieu de da dich", bc.CanhBao[1].TieuDe, "*** CẢNH BÁO: CÓ 1 HÌNH CÔ ĐƠN!");
            Kiem("khoi KHONG dong ve [x] PDF (dong tom tat dong khoi)", bc.CanhBao[1].ChiTiet.Any(c => c.Contains("PDF")), false);
            Kiem("khoi khong tim thay hinh: 2 text + 1 dong goi y",
                 bc.CanhBao[2].Loai + "/" + bc.CanhBao[2].ChiTiet.Count, "KhongHinh/3");
            Kiem("chi tiet da dich 'tại X='", bc.CanhBao[2].ChiTiet[0], "Text #5 [t=6mm 2枚]  tại X=120.5 Y=-30.0");
            Kiem("khoi Error T: la LOI, 1 dong", bc.CanhBao[3].Loai + "/" + bc.CanhBao[3].LaLoi + "/" + bc.CanhBao[3].ChiTiet.Count,
                 "LoiT/True/1");
            // Tam thieu ma khac -> dong do khong dat
            var bcThieu = BaoCaoGro.Doc("3 ghi chu -> 4 tam:\n  4 bango (layer 0), kieu chu guro *** 1 CAI DAT KHONG DUOC ***\n  3 ma khac I- (layer 3-KEGAKI)");
            Kiem("bango loi kieu chu -> khong dat", bcThieu.DongGhiChu()[0].Dat, false);
            Kiem("ma khac 3/4 tam -> khong dat", bcThieu.DongGhiChu()[1].Dat, false);
            Kiem("khong co xu ly net -> Net null", bcThieu.Net == null, true);
            var bcCsvLoi = BaoCaoGro.Doc("1 ghi chu -> 1 tam:\n  1 dong CSV (GHI DE) -> *** GHI THAT BAI ***\n\n" +
                                         "KHONG GHI DUOC FILE CSV\n\nBango / ma khac / F-name DA duoc tao tren ban ve, nhung file CSV thi KHONG ghi duoc.\n" +
                                         "Thuong la vi file dang bi MO boi Excel.\n\nHay DONG file CSV lai, UNDO lenh nay, roi chay lai.\n\nC:\\a\\b.csv\n\n");
            Kiem("CSV ghi that bai -> CsvLoi, khong co duong", bcCsvLoi.CsvLoi + "/" + (bcCsvLoi.DuongCsv ?? "null"), "True/null");
            Kiem("khoi loi CSV gom ca cac dong cach dong trong", bcCsvLoi.CanhBao[0].Loai + "/" + bcCsvLoi.CanhBao[0].ChiTiet.Count, "LoiCsv/4");
            var bcHet = BaoCaoGro.Doc("1 ghi chu -> 1 tam:\n\n--- THONG BAO XU LY NET (Layer \"0\") ---\n  + 5 net Continuous -> 0-KAKOU\n" +
                                      "  + 0 net Center1 -> 3-KEGAKI\n  + 2 text -> 1-SUNPOU\n  -> DA DOI HET! Khong con doi tuong nao tren layer goc.");
            Kiem("da doi het -> ConLai 0, DaDoiHet", bcHet.Net.ConLai + "/" + bcHet.Net.DaDoiHet, "0/True");
            var bcKhong = BaoCaoGro.Doc("1 ghi chu -> 1 tam:\n\n[XU LY NET]: Khong tim thay doi tuong nao tren layer goc \"ABC\" ben trong cac khung bao da chon.");
            Kiem("xu ly net khong tim thay", bcKhong.Net.KhongTimThay + "/" + bcKhong.Net.Layer + "/" + bcKhong.DongNetDaDoi().Count, "True/ABC/0");
            Kiem("bao cao la -> khong doc duoc (tab dung dang dong chu)", BaoCaoGro.Doc("cai gi do khac").DocDuoc, false);

            Console.WriteLine();
            Console.WriteLine("=== Xep cum sang phai: doc bao cao & canh bao sot (muc 109) ===");
            var bcXep = BaoCaoGro.Doc("24 ghi chu -> 33 tam:\n  33 bango (layer 0), kieu chu guro\n"
                                    + "Xep cum sang phai: 5 cum (mirror 1, vat 1, be+vat 1, be 2) - cot X=2500.0 (da tranh vat can)\n"
                                    + "!!! 2 doi tuong con sot tai vi tri cum cu (kiem tra lai).\n");
            Kiem("doc duoc cum xep phai = 5", bcXep.SoCumXepPhai, 5);
            Kiem("doc duoc mirror = 1", bcXep.SoCumMirror, 1);
            Kiem("doc duoc vat = 1", bcXep.SoCumVat, 1);
            Kiem("doc duoc be+vat = 1", bcXep.SoCumBeVat, 1);
            Kiem("doc duoc be = 2", bcXep.SoCumBe, 2);
            Kiem("doc duoc cot X = 2500.0", bcXep.CotXepX, 2500.0);
            Kiem("doc duoc da tranh vat can = true", bcXep.DaTranhVatCan, true);
            Kiem("doc duoc doi tuong sot = 2", bcXep.SoDoiTuongSot, 2);
            Kiem("dong ghi chu tren tam co dong Xep cum", bcXep.DongGhiChu().Any(d => d.Nhan == "Sắp xếp cụm"), true);
            Kiem("dong Xep cum khong dat khi co sot", bcXep.DongGhiChu().First(d => d.Nhan == "Sắp xếp cụm").Dat, false);
            Kiem("dich dong xep cum sang phai",
                 ThongTinGro.Dich("Xep cum sang phai: 5 cum (mirror 1, vat 1, be+vat 1, be 2) - cot X=2500.0 (da tranh vat can)"),
                 "Xếp cụm sang phải: 5 cụm (mirror 1, vát 1, bẻ+vát 1, bẻ 2) — cột X=2500.0 (đã né vật cản)");
            Kiem("dich dong doi tuong sot",
                 ThongTinGro.Dich("!!! 2 doi tuong con sot tai vi tri cum cu (kiem tra lai)."),
                 "!!! 2 đối tượng còn sót tại vị trí cụm cũ (cần kiểm tra lại).");

            Console.WriteLine();
            Console.WriteLine("=== Tach va cham khung in: doc bao cao (muc 111) ===");
            var bcTach = BaoCaoGro.Doc("24 ghi chu -> 33 tam:\n  33 bango (layer 0), kieu chu guro\n"
                                    + "Tach va cham khung in: da doi 3 luot cho 2 tam don.\n");
            Kiem("doc duoc so luot tach khung = 3", bcTach.SoLuotTachKhung, 3);
            Kiem("doc duoc so tam tach khung = 2", bcTach.SoTamTachKhung, 2);
            Kiem("dong ghi chu co dong Tach va cham khung in", bcTach.DongGhiChu().Any(d => d.Nhan == "Tách va chạm khung in"), true);
            Kiem("dich dong tach va cham khung in",
                 ThongTinGro.Dich("Tach va cham khung in: da doi 3 luot cho 2 tam don."),
                 "Tách va chạm khung in: đã dời 3 lượt cho 2 tấm đơn.");

            Console.WriteLine();
            Console.WriteLine("=== Tach cap mirror Honzu/Gyakuzu thanh 2 tam (muc 112) ===");
            // Dung thu tu lisp ghep: 6e (xep cum) -> 6e2 (tach mirror) -> 6f (tach va cham)
            var bcMir = BaoCaoGro.Doc("24 ghi chu -> 33 tam:\n  33 bango (layer 0), kieu chu guro\n"
                                    + "Xep cum sang phai: 5 cum (mirror 2, vat 1, be+vat 1, be 1) - cot X=2500.0\n"
                                    + "Tach cap mirror: 2 cap -> 4 tam (ban nghich dat ben phai, khe 100mm)\n"
                                    + "Tach va cham khung in: da doi 1 luot cho 1 tam don.\n");
            Kiem("doc duoc so cap tach mirror = 2", bcMir.SoCapTachMirror, 2);
            Kiem("khong bo qua cap nao", bcMir.SoCapBoQuaMirror.HasValue, false);
            Kiem("dong tach mirror dat", bcMir.DongGhiChu().First(d => d.Nhan == "Tách cặp mirror").Dat, true);
            Kiem("dong tach mirror: so tam", bcMir.DongGhiChu().First(d => d.Nhan == "Tách cặp mirror").GiaTri, "2 cặp → 4 tấm");
            Kiem("dong 6e van doc duoc khi co 6e2", bcMir.SoCumXepPhai, 5);
            Kiem("dong 6f van doc duoc khi co 6e2", bcMir.SoLuotTachKhung, 1);
            Kiem("tach mirror khong tao khoi canh bao", bcMir.CanhBao.Count, 0);
            var bcBoQua = BaoCaoGro.Doc("3 ghi chu -> 4 tam:\n"
                                      + "Tach cap mirror: 1 cap -> 2 tam (ban nghich dat ben phai, khe 100mm)\n"
                                      + "!!! Bo qua tach mirror 1 cap: bango 7 (khong co dong Honzu/Gyakuzu rieng)\n");
            Kiem("doc duoc so cap bo qua = 1", bcBoQua.SoCapBoQuaMirror, 1);
            Kiem("bo qua -> dong tach mirror khong dat", bcBoQua.DongGhiChu().First(d => d.Nhan == "Tách cặp mirror").Dat, false);
            Kiem("bo qua -> 1 khoi canh bao cam", bcBoQua.CanhBao.Count + "/" + bcBoQua.CanhBao[0].LaLoi, "1/False");
            Kiem("dich dong tach mirror",
                 ThongTinGro.Dich("Tach cap mirror: 2 cap -> 4 tam (ban nghich dat ben phai, khe 100mm)"),
                 "Tách cặp mirror: 2 cặp → 4 tấm (bản nghịch đặt bên phải, khe 100 mm)");
            Kiem("dich dong bo qua tach mirror",
                 ThongTinGro.Dich("!!! Bo qua tach mirror 2 cap: bango 7 (khong co dong Honzu/Gyakuzu rieng), bango 12 (3 tam)"),
                 "!!! Bỏ qua tách mirror 2 cặp: bango 7 (không có dòng 本図/逆図 riêng), bango 12 (3 tấm)");

            Console.WriteLine();
            Console.WriteLine("=== Tab Thong Tin theo so lieu song (muc 113) ===");
            var gop = BaoCaoGro.GopNhomNet(new[]
            {
                new KeyValuePair<string, int>("Continuous", 3),
                new KeyValuePair<string, int>("CENTER1", 2),
                new KeyValuePair<string, int>("continuous", 4),   // cung linetype, khac mau -> gop
                new KeyValuePair<string, int>("HIDDEN", 0),       // nhom rong -> bo
            });
            Kiem("gop nhom net: 2 linetype", gop.Count, 2);
            Kiem("gop nhom net: Continuous 7 dung dau", gop[0].Linetype + "/" + gop[0].SoLuong, "Continuous/7");
            Kiem("gop nhom net: CENTER1 2", gop[1].SoLuong, 2);
            Kiem("gop nhom net: rong -> rong", BaoCaoGro.GopNhomNet(new KeyValuePair<string, int>[0]).Count, 0);
            Kiem("gop nhom net: null -> rong", BaoCaoGro.GopNhomNet(null).Count, 0);
            var bcSong = BaoCaoGro.Doc("3 ghi chu -> 3 tam:\n"
                                     + "!!! CO 2 HINH CO DON (khong co text thong tin)\n"
                                     + "!!! 1 doi tuong con sot tai vi tri cum cu (kiem tra lai).\n");
            int coNut = bcSong.CanhBao.Count(k => k.CoNutDanhSach);
            Kiem("khoi co don co nut danh sach", coNut >= 1, true);
            Kiem("co tam song -> bo khoi co don cua bao cao, giu khoi khac",
                 bcSong.CanhBaoKhiCoTamSong().Count, bcSong.CanhBao.Count - coNut);
            Kiem("co tam song -> khong con khoi co nut danh sach",
                 bcSong.CanhBaoKhiCoTamSong().Any(k => k.CoNutDanhSach), false);

            Console.WriteLine();
            Console.WriteLine("=== Ghi chu XData GRO (muc 126) ===");
            Kiem("giai ma \\U+ + sua chuoi go nham ｹﾞｿ線",
                 JapanNoteTranslator.ChuanHoaGhiChuGro("\\U+FF79\\U+FF9E\\U+FF7F\\U+7DDA"), "ｹｶﾞｷ線");
            Kiem("chuoi dung giu nguyen",
                 JapanNoteTranslator.ChuanHoaGhiChuGro("\\U+FF79\\U+FF76\\U+FF9E\\U+FF77\\U+7DDA"), "ｹｶﾞｷ線");
            Kiem("rong -> rong", JapanNoteTranslator.ChuanHoaGhiChuGro(null), "");
            // muc 153: lisp ghi kegaki + ghi chu tach roi (\U+XXXX) noi bang "; "
            Kiem("kegaki + ghi chu tach roi (muc 153)",
                 JapanNoteTranslator.ChuanHoaGhiChuGro("\\U+FF79\\U+FF76\\U+FF9E\\U+FF77\\U+7DDA; \\U+7A74\\U+30CE\\U+30B3\\U+30B7"),
                 "ｹｶﾞｷ線; 穴ノコシ");
            Kiem("chi ghi chu tach roi (muc 153)",
                 JapanNoteTranslator.ChuanHoaGhiChuGro("\\U+7A74\\U+30CE\\U+30B3\\U+30B7"), "穴ノコシ");
            // muc 154: khoa DAI truoc - "文字ｹｶﾞｷ線" khong bi "ｹｶﾞｷ線" an mat
            JapanNoteTranslator.Register("文字ｹｶﾞｷ線", "khắc chữ");
            Kiem("文字ｹｶﾞｷ線 -> khac chu (muc 154)", JapanNoteTranslator.Translate("文字ｹｶﾞｷ線"), "khắc chữ");
            Kiem("2 ghi chu: kegaki + khac chu (muc 154)",
                 JapanNoteTranslator.Translate(JapanNoteTranslator.ChuanHoaGhiChuGro(
                     "\\U+FF79\\U+FF76\\U+FF9E\\U+FF77\\U+7DDA; \\U+6587\\U+5B57\\U+FF79\\U+FF76\\U+FF9E\\U+FF77\\U+7DDA")),
                 "khắc kegaki; khắc chữ");
            Kiem("ExtractAndTranslate cung uu tien khoa dai", JapanNoteTranslator.ExtractAndTranslate("文字ｹｶﾞｷ線 t=9mm"), "khắc chữ");
            // muc 157: tam khac chu theo BAN DICH (khong theo chu 文字)
            Kiem("LaKhacChu: 文字ｹｶﾞｷ線 (escape) -> co", JapanNoteTranslator.LaKhacChu("\\U+6587\\U+5B57\\U+FF79\\U+FF76\\U+FF9E\\U+FF77\\U+7DDA"), true);
            Kiem("LaKhacChu: chi ｹｶﾞｷ線 -> khong", JapanNoteTranslator.LaKhacChu("ｹｶﾞｷ線"), false);
            Kiem("LaKhacChu: kegaki + khac chu -> co", JapanNoteTranslator.LaKhacChu("ｹｶﾞｷ線; 文字ｹｶﾞｷ線"), true);
            JapanNoteTranslator.Register("文字サイズ", "cỡ chữ");
            Kiem("LaKhacChu: 文字 nghia khac (co chu) -> khong", JapanNoteTranslator.LaKhacChu("文字サイズ"), false);
            Kiem("LaKhacChu: rong -> khong", JapanNoteTranslator.LaKhacChu(""), false);
            Console.WriteLine();
            Console.WriteLine("=== 1 ghi chu chi thuoc 1 tam (muc 127) ===");
            var hop2123 = new List<double[]> { new[] { 70.0, -200, 430, 40 }, new[] { 375.0, -200, 770, 25 } };
            Kiem("ｹｶﾞｷ線 cham ca khung 21 lan 23 -> chi tam 23",
                 BangSoLieu.ChonTamSoHuu(new[] { 415.0, 0, 540, 20 }, hop2123), 1);
            Kiem("tam chu nam trong ca 2 hop -> hop co tam gan hon",
                 BangSoLieu.ChonTamSoHuu(new[] { 55.0, 40, 65, 50 },
                     new List<double[]> { new[] { 0.0, 0, 100, 100 }, new[] { 50.0, 0, 300, 100 } }), 0);
            Kiem("khong cham hop nao -> -1",
                 BangSoLieu.ChonTamSoHuu(new[] { 900.0, 900, 910, 910 }, hop2123), -1);
            Console.WriteLine();
            Console.WriteLine("=== Doi tuong con sot: handle + nut Chon & Zoom (muc 134) ===");
            var bcSot = BaoCaoGro.Doc("26 ghi chu -> 31 tam:\n"
                                    + "Xep cum sang phai: 5 cum (mirror 3, vat 2, be+vat 0, be 0) - cot X=12128.4\n"
                                    + "!!! 2 doi tuong con sot tai vi tri cum cu (kiem tra lai).\n"
                                    + "Handle con sot: 2A3F 1b0 2A3F zz\n");
            var kSot = bcSot.CanhBao.FirstOrDefault(k => k.Loai == LoaiCanhBao.ConSot);
            Kiem("co khoi con sot", kSot != null, true);
            Kiem("doc 2 handle (bo trung, bo chuoi khong phai hex, viet hoa)",
                 kSot == null ? "" : string.Join(",", kSot.Handles), "2A3F,1B0");
            Kiem("co nut Chon & Zoom", kSot != null && kSot.CoNutChonZoom, true);
            Kiem("khoi con sot khong co nut Danh sach", kSot != null && kSot.CoNutDanhSach, false);
            Kiem("van giu khi co so lieu tam song", bcSot.CanhBaoKhiCoTamSong().Contains(kSot), true);
            Kiem("dich dong handle", ThongTinGro.Dich("Handle con sot: 2A3F 1B0"), "Handle còn sót: 2A3F 1B0");
            var bcSot0 = BaoCaoGro.Doc("!!! 1 doi tuong con sot tai vi tri cum cu (kiem tra lai).\n");
            Kiem("khong co dong handle -> khong co nut",
                 bcSot0.CanhBao.Count == 1 && !bcSot0.CanhBao[0].CoNutChonZoom, true);
            Console.WriteLine();
            Console.WriteLine("=== Tach va cham khung in: bao cap con chong (muc 121) ===");
            Kiem("dich dong con cap khung chong",
                 ThongTinGro.Dich("!!! Con 3 cap khung in chong nhau - kiem tra va tach tay."),
                 "!!! Còn 3 cặp khung in chồng nhau — kiểm tra và tách tay.");
            var bcChong = BaoCaoGro.Doc("1 ghi chu -> 1 tam:\nTach va cham khung in: da doi 2 luot cho 2 tam don.\n"
                                      + "!!! Con 3 cap khung in chong nhau - kiem tra va tach tay.\n");
            Kiem("cap con chong -> 1 khoi canh bao, van doc dong tach", bcChong.CanhBao.Count + "/" + bcChong.SoLuotTachKhung, "1/2");

            Console.WriteLine("=== Ma khac + F-name vao trong hinh (muc 117) ===");
            var bcMk = BaoCaoGro.Doc("24 ghi chu -> 33 tam:\n  33 bango (layer 0), kieu chu guro\n"
                                   + "Dat ma khac vao hinh: 18/20 tam (thu nho 1/2: 3, de KEGAKI: 1)\n"
                                   + "!!! Ma khac de KEGAKI (1 tam): bango 7\n"
                                   + "!!! Chua dat duoc ma khac vao hinh (2 tam) - move thu cong: bango 12, 15 (khong tim thay bien kin)\n");
            Kiem("doc duoc ma khac dat/tong", bcMk.SoMkDat + "/" + bcMk.SoMkTong, "18/20");
            Kiem("doc duoc thu nho / de KEGAKI / chua dat", bcMk.SoMkThuNho + "/" + bcMk.SoMkDeKegaki + "/" + bcMk.SoMkChuaDat, "3/1/2");
            var dongMk = bcMk.DongGhiChu().First(d => d.Nhan == "Mã khắc vào hình");
            Kiem("dong ma khac vao hinh: gia tri", dongMk.GiaTri, "18/20 tấm · thu nhỏ 3 · đè ケガキ 1");
            Kiem("dong ma khac vao hinh: khong dat khi con sot", dongMk.Dat, false);
            Kiem("2 khoi canh bao: de KEGAKI cam, chua dat do",
                 bcMk.CanhBao.Count + "/" + bcMk.CanhBao[0].LaLoi + "/" + bcMk.CanhBao[1].LaLoi, "2/False/True");
            Kiem("dich dong dat ma khac",
                 ThongTinGro.Dich("Dat ma khac vao hinh: 18/20 tam (thu nho 1/2: 3, de KEGAKI: 1)"),
                 "Đặt mã khắc vào hình: 18/20 tấm (thu nhỏ 1/2: 3, đè ケガキ: 1)");
            Kiem("dich dong chua dat",
                 ThongTinGro.Dich("!!! Chua dat duoc ma khac vao hinh (2 tam) - move thu cong: bango 12, 15 (khong tim thay bien kin)"),
                 "!!! Chưa đặt được mã khắc vào hình (2 tấm) — cần move thủ công: bango 12, 15 (không tìm thấy biên kín)");
            Kiem("dich dong chua dat: chung hinh",
                 ThongTinGro.Dich("!!! Chua dat duoc ma khac vao hinh (1 tam) - move thu cong: bango 2 (chung hinh voi bango 1)"),
                 "!!! Chưa đặt được mã khắc vào hình (1 tấm) — cần move thủ công: bango 2 (chung hình với bango 1)");
            Kiem("dich dong de KEGAKI",
                 ThongTinGro.Dich("!!! Ma khac de KEGAKI (1 tam): bango 7"), "!!! Mã khắc đè ケガキ (1 tấm): bango 7");
            var bcMkHet = BaoCaoGro.Doc("1 ghi chu -> 1 tam:\nDat ma khac vao hinh: 1/1 tam (thu nho 1/2: 0, de KEGAKI: 0)\n");
            Kiem("dat het -> dong dat, khong ghi thu nho",
                 bcMkHet.DongGhiChu().First(d => d.Nhan == "Mã khắc vào hình").GiaTri + "/" +
                 bcMkHet.DongGhiChu().First(d => d.Nhan == "Mã khắc vào hình").Dat, "1/1 tấm/True");

            // Gom cot F-name theo he truc dong N= (cum xoay)
            Func<double, List<BangSoLieu.DongChu>> cotXoay = gx =>
            {
                var ds = new List<BangSoLieu.DongChu>();
                double cs = Math.Cos(gx), sn = Math.Sin(gx);
                for (int i = 0; i < 5; i++)
                {
                    double lx = 0, ly = -1.6667 * i;   // cot doc o he truc rieng: N= tren cung
                    ds.Add(new BangSoLieu.DongChu { X = 100 + lx * cs - ly * sn, Y = 50 + lx * sn + ly * cs, Cao = 1.0, LaN = i == 0 });
                }
                // dong N= cua tam KHAC dat ngay ben duoi cot -> phai dung lai
                double l2 = -1.6667 * 5;
                ds.Add(new BangSoLieu.DongChu { X = 100 - l2 * sn, Y = 50 + l2 * cs, Cao = 1.0, LaN = true });
                // chu lech cot (x' = 3) -> khong gom
                ds.Add(new BangSoLieu.DongChu { X = 100 + 3 * cs - (-2.0) * sn, Y = 50 + 3 * sn + (-2.0) * cs, Cao = 1.0 });
                return ds;
            };
            foreach (double gq in new[] { 0.0, Math.PI / 2, Math.PI / 6, -Math.PI / 4 })
            {
                var g = BangSoLieu.GomCotFname(cotXoay(gq), 0, gq);
                Kiem("gom cot F-name xoay " + Math.Round(gq * 180 / Math.PI) + " do", string.Join(",", g), "0,1,2,3,4");
            }
            Kiem("gom cot F-name: cum xoay 90 ma doc o 0 do -> chi con dong N=",
                 string.Join(",", BangSoLieu.GomCotFname(cotXoay(Math.PI / 2), 0, 0.0)), "0");

            BangSoLieu.DocCoMaKhac("NO_SHAPE;MK_THU_NHO;MK_DE_KEGAKI", out var cbMk, out var gcMk);
            Kiem("co ma khac: canh bao + ghi chu", string.Join("|", cbMk) + "#" + string.Join("|", gcMk),
                 "Mã khắc đè ケガキ#Mã khắc thu nhỏ 1/2");
            BangSoLieu.DocCoMaKhac("MK_CHUA_DAT", out cbMk, out gcMk);
            Kiem("co ma khac: chua dat", string.Join("|", cbMk), "[!] Mã khắc chưa vào hình — move thủ công");
            // muc 190: co chu khac -> cot Ghi chu
            BangSoLieu.DocCoMaKhac("KC_NGOAI_HINH;KC_CHAM_NET", out cbMk, out gcMk);
            Kiem("co khac chu: ngoai hinh + cham net", string.Join("|", cbMk),
                 "Chữ khắc nằm một phần ngoài hình — kiểm tra|Chữ khắc chạm nét cắt 0-KAKOU — kiểm tra");
            Kiem("dich dong khac chu",
                 ThongTinGro.Dich("Khac chu: doi 14 text trong hinh sang 3-KEGAKI - bango 6, 5, 4"),
                 "Khắc chữ: đã đổi 14 chữ trong hình sang ケガキ (3-KEGAKI) — bango 6, 5, 4");
            Kiem("dich dong canh bao khac chu",
                 ThongTinGro.Dich("!!! Khac chu: 6 text nam mot phan ngoai hinh / cham net cat 0-KAKOU - kiem tra: bango 6, 5, 4, 3"),
                 "!!! Khắc chữ: 6 chữ nằm một phần ngoài hình hoặc chạm nét cắt 0-KAKOU — cần kiểm tra: bango 6, 5, 4, 3");
            Kiem("dich dong tam khac chu khong co chu",
                 ThongTinGro.Dich("!!! Tam khac chu (2 tam) khong co text nao trong hinh de doi sang 3-KEGAKI."),
                 "!!! Tấm khắc chữ (2 tấm) không có chữ nào trong hình để đổi sang ケガキ (3-KEGAKI).");
            BangSoLieu.DocCoMaKhac("", out cbMk, out gcMk);
            Kiem("co ma khac: rong", cbMk.Count + gcMk.Count, 0);

            Console.WriteLine();
            Console.WriteLine("=== An / hien Ban chinh / Ban nghich trong cot Ghi chu (muc 159) ===");
            var pHon = new PlateItem { Bango = "1", Quantity = "1", Thickness = "9.0", Material = "SS400", Role = "HONZU", CustomerNote = "khắc chữ" };
            var pGya = new PlateItem { Bango = "2", Quantity = "1", Thickness = "9.0", Material = "SS400", Role = "GYAKUZU", PrimaryBango = "1" };
            pHon.DanhDauDaKhop();
            pGya.DanhDauDaKhop();
            Kiem("mac dinh hien Ban chinh", pHon.Note, "Bản chính\nkhắc chữ");
            PlateItem.HienVaiTro = false;
            pHon.LamMoiGhiChu();
            Kiem("tat -> chi con ghi chu khach", pHon.Note, "khắc chữ");
            Kiem("tat -> ban nghich khong con ghi chu", pGya.Note, string.Empty);
            Kiem("tat -> van giu vai tro (ghep cap / luu ban ve)", pGya.Role + "/" + pGya.PrimaryBango, "GYAKUZU/1");
            PlateItem.HienVaiTro = true;
            Kiem("bat lai -> Ban nghich - 1", pGya.Note, "Bản nghịch - 1");

            Console.WriteLine();
            Console.WriteLine("=== Ghi chu tieng Nhat moi (chua co trong tu dien, muc 160) ===");
            Kiem("doan chua dich: スリット", string.Join("|", JapanNoteTranslator.DoanChuaDich("スリット")), "スリット");
            Kiem("doan chua dich: da dich het -> rong", JapanNoteTranslator.DoanChuaDich("khắc chữ; khắc kegaki").Count, 0);
            Kiem("doan chua dich: tron dich + chua dich", string.Join("|", JapanNoteTranslator.DoanChuaDich("khắc kegaki; ｽﾘｯﾄ; 穴ノコシ")), "ｽﾘｯﾄ|穴ノコシ");
            var khoiMoi = BaoCaoGro.KhoiGhiChuMoi(new[]
            {
                new KeyValuePair<string, string>("1", "スリット"),
                new KeyValuePair<string, string>("2", "スリット"),
                new KeyValuePair<string, string>("3", "khắc chữ"),
                new KeyValuePair<string, string>("4", "穴ノコシ; khắc kegaki"),
            });
            Kiem("khoi ghi chu moi: co", khoiMoi != null, true);
            Kiem("khoi ghi chu moi: tieu de", khoiMoi?.TieuDe, "Có 2 ghi chú tiếng Nhật mới (chưa có trong từ điển) — cần lưu ý");
            Kiem("khoi ghi chu moi: chi tiet", khoiMoi == null ? "" : string.Join("|", khoiMoi.ChiTiet), "スリット — bango 1, 2|穴ノコシ — bango 4");
            Kiem("khoi ghi chu moi: co nut Tu dien, la canh bao", khoiMoi != null && khoiMoi.CoNutTuDien && !khoiMoi.LaLoi, true);
            Kiem("khoi ghi chu moi: tat ca da dich -> null",
                 BaoCaoGro.KhoiGhiChuMoi(new[] { new KeyValuePair<string, string>("1", "khắc chữ") }) == null, true);

            Console.WriteLine();
            Console.WriteLine("=== Canh bao tam can xu ly doi net sang 4-SHT ===");
            Kiem("LaXuLySht: 'xử lý SHT' -> true", JapanNoteTranslator.LaXuLySht("xử lý SHT"), true);
            Kiem("LaXuLySht: 'xu ly sht' (khong dau) -> true", JapanNoteTranslator.LaXuLySht("xu ly sht"), true);
            Kiem("LaXuLySht: 'XỬ LÝ SHT' (in hoa) -> true", JapanNoteTranslator.LaXuLySht("XỬ LÝ SHT"), true);
            Kiem("LaXuLySht: 'tam can xu ly SHT' -> true", JapanNoteTranslator.LaXuLySht("tam can xu ly SHT"), true);
            Kiem("LaXuLySht: 'khac kegaki' -> false", JapanNoteTranslator.LaXuLySht("khắc kegaki"), false);
            Kiem("LaXuLySht: null/empty -> false", JapanNoteTranslator.LaXuLySht(""), false);

            // Dang ky tu dien thu nghiem
            JapanNoteTranslator.Register("SHT処理", "xử lý SHT");
            Kiem("LaXuLySht: qua tu dien 'SHT処理' -> true", JapanNoteTranslator.LaXuLySht("SHT処理"), true);

            // Test PlateItem LaXuLySht va TinhGhiChu
            var plateSht = new PlateItem
            {
                Bango = "5",
                Quantity = "1",
                Thickness = "3.2",
                Material = "SPHC"
            };
            plateSht.CustomerNote = "xử lý SHT";
            Kiem("PlateItem.LaXuLySht -> true", plateSht.LaXuLySht, true);
            Kiem("PlateItem.MucDo khi la SHT -> 1 (canh bao)", plateSht.MucDo, 1);
            Kiem("PlateItem.Note co chua canh bao 4-SHT", plateSht.Note.Contains("4-SHT"), true);

            // Test KhoiCanhBaoSht song
            var dsTamSht = new[]
            {
                new KeyValuePair<string, string>("1", "khắc chữ"),
                new KeyValuePair<string, string>("3", "xử lý SHT"),
                new KeyValuePair<string, string>("7", "SHT処理"),
            };
            var khoiSht = BaoCaoGro.KhoiCanhBaoSht(dsTamSht);
            Kiem("KhoiCanhBaoSht: co", khoiSht != null, true);
            Kiem("KhoiCanhBaoSht: tieu de co 2 tam", khoiSht?.TieuDe.Contains("2 tấm"), true);
            Kiem("KhoiCanhBaoSht: chi tiet chua bango 3, 7", khoiSht != null && khoiSht.ChiTiet.Count > 0 && khoiSht.ChiTiet[0].Contains("3, 7"), true);
            Kiem("KhoiCanhBaoSht: CoNutDanhSach", khoiSht?.CoNutDanhSach, true);

            // Test doc dong canh bao tu chuoi bao cao GRO
            string groReport = "10 ghi chu -> 10 tam:\n  10 bango (layer 0), kieu chu Standard\n*** CANH BAO: Co 2 tam can xu ly doi net sang 4-SHT (ghi chu co \"xu ly SHT\") - bango: 3, 7\nDa danh so tu 1 den 10";
            var bcDoc = BaoCaoGro.Doc(groReport);
            var kShtDoc = bcDoc.CanhBao.FirstOrDefault(k => k.Loai == LoaiCanhBao.XuLySht);
            Kiem("BaoCaoGro.Doc nhan duoc khoi SHT", kShtDoc != null, true);
            Kiem("BaoCaoGro.Doc: CoNutDanhSach", kShtDoc?.CoNutDanhSach, true);
            Kiem("BaoCaoGro.Doc: ChiTiet chua bango", kShtDoc != null && kShtDoc.ChiTiet.Count > 0 && kShtDoc.ChiTiet[0].Contains("3, 7"), true);

            // Test dich dong bao cao SHT
            string dongGocSht = "*** CANH BAO: Co 2 tam can xu ly doi net sang 4-SHT (ghi chu co \"xu ly SHT\") - bango: 3, 7";
            string dongDichSht = ThongTinGro.Dich(dongGocSht);
            Kiem("ThongTinGro.Dich dong canh bao SHT", dongDichSht.Contains("4-SHT"), true);

            TestDatMaKhac();
            TestGd0();
            TestQuetHinh();
            TestPhanLoaiNet();
            TestDimDai();
            TestDimDai3b();
            TestLocDimNet();
            TestVongKin();
            TestXepCum();
            TestTachVaCham();
            TestChiaNetCat();
            TestDongKhung();

            Console.WriteLine();
            Console.WriteLine("=== Test khong dung tu dien that (muc 158) ===");
            Kiem("dang dung khoa registry rieng cho test", JapanNoteTranslator.RegSubKey, KhoaTest);
            try { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(KhoaTest, false); } catch { }

            Console.WriteLine();
            Console.WriteLine("KET QUA TONG: {0} OK, {1} FAIL{2}", _ok, _fail, _fail == 0 ? "  -> TAT CA DAT" : "  *** CO TEST HONG ***");
            return _fail == 0 ? 0 : 1;
        }

        private const string KhoaTest = @"Software\OFT_Lisp\JapanNoteDictionary_Test";

        // ------------------------------------------------------------------
        // Muc 170: dat ma khac + F-name vao hinh (che do 2, ban C#)
        // ------------------------------------------------------------------

        private static NetMau Net(string lay, bool kin, params double[] xy)
        {
            var pts = new List<Diem>();
            for (int i = 0; i + 1 < xy.Length; i += 2) pts.Add(new Diem(xy[i], xy[i + 1]));
            if (kin) pts.Add(pts[0]);
            return new NetMau { Layer = lay, Kin = kin, Pts = pts, Bb = Hop.TuDiem(pts) };
        }

        private static NetMau ChuNhat(double x1, double y1, double x2, double y2, string lay = "0-KAKOU") =>
            Net(lay, true, x1, y1, x2, y1, x2, y2, x1, y2);

        private static TamCanDat Tam(string bango, Hop hinh, double w, double h) =>
            new TamCanDat { Bango = bango, BbHinh = hinh, BbCum = new Hop(5000, 5000, 5000 + w, 5000 + h) };

        private static string S(double v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// Muc 171: bo chon phat lai theo ma buoc. Them buoc moi = them mot nhanh o day.
        /// </summary>
        private static int PhatLai(string[] args)
        {
            string buoc = args[1], file, thamSo;
            if (System.IO.File.Exists(buoc)) { file = buoc; thamSo = args.Length > 2 ? args[2] : null; buoc = "datmk"; }
            else { file = args.Length > 2 ? args[2] : null; thamSo = args.Length > 3 ? args[3] : null; }
            if (file == null || !System.IO.File.Exists(file))
            {
                Console.WriteLine("Khong thay file dump: " + (file ?? "(thieu)"));
                return 2;
            }
            switch (GroCongTac.ChuanMa(buoc))
            {
                case "datmk": return PhatLaiDatMk(file, thamSo);
                case "quethinh": return PhatLaiQuetHinh(file);
                case "nhannet": return PhatLaiNhanNet(file);
                case "dimdai": return PhatLaiDimDai(file);
                case "dimdai3b": return PhatLaiDimDai(file, true);
                default:
                    Console.WriteLine("Chua co bo phat lai cho buoc \"" + buoc + "\".");
                    return 2;
            }
        }

        private static int PhatLaiQuetHinh(string file)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var nets = new List<NetKakou>();
            var hd = new List<string>();
            var kb = new List<Hop>();
            var pts = new List<Diem>();
            double D(string s) => double.Parse(s, inv);
            foreach (string[] p in DumpBuoc.Doc(file))
            {
                if (p[0] == "K") kb.Add(new Hop(D(p[1]), D(p[2]), D(p[3]), D(p[4])));
                else if (p[0] == "P") pts.Add(new Diem(D(p[1]), D(p[2])));
                else if (p[0] == "N") { nets.Add(new NetKakou(nets.Count, new Hop(D(p[2]), D(p[3]), D(p[4]), D(p[5])))); hd.Add(p[1]); }
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var kq = QuetHinh.Gom(nets, kb, pts);
            Console.WriteLine("Net 0-KAKOU: {0} (sau loc khung {1}), khung {2}, ghi chu {3} -> {4} hinh, {5} khung nham ({6} ms)",
                kq.SoNetVao, kq.SoNetSauLoc, kb.Count, pts.Count, kq.Hinh.Count, kq.LoiKhung.Count, sw.ElapsedMilliseconds);
            foreach (var h in kq.Hinh.Take(15))
                Console.WriteLine("  #{0,-3} {1,3} net  {2:0.0},{3:0.0}..{4:0.0},{5:0.0}  [{6}]", h.Id, h.ThanhVien.Count,
                    h.Bb.X1, h.Bb.Y1, h.Bb.X2, h.Bb.Y2, string.Join(" ", h.ThanhVien.Take(4).Select(i => hd[i])));
            return 0;
        }

        // ------------------------------------------------------------------
        // Muc 173: GD3a phan loai net cho nhan Dim
        // ------------------------------------------------------------------
        private static NetVao NV(string h, string loai, string lay, string lt, double x = 0, double y = 0, double r = 0) =>
            new NetVao { Handle = h, Loai = loai, Layer = lay, Linetype = lt, X = x, Y = y, R = r, CoTam = loai == "CIRCLE" };

        private static string Hs(IEnumerable<NetVao> ds) => string.Join(",", ds.Select(n => n.Handle));
        private static string Hs(IEnumerable<VongTron> ds) => string.Join(",", ds.Select(n => n.Net.Handle));

        private static void TestPhanLoaiNet()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD3a phan loai net cho nhan Dim (muc 173) ===");
            var ss = new List<NetVao>
            {
                NV("1", "LINE", "goc", "ByLayer"),
                NV("2", "LINE", "GOC", "Continuous"),          // layer khac hoa/thuong van nhan
                NV("3", "LINE", "goc", "CENTER1"),             // khong Continuous -> bo
                NV("4", "ARC", "goc", ""),                      // rong = ByLayer
                NV("5", "LINE", "khac", "Continuous"),          // khac layer -> bo
                NV("6", "CIRCLE", "goc", "DASHED", 10, 10, 1.5),// CIRCLE moi linetype; r<=1.5 -> rat nho
                NV("7", "CIRCLE", "goc", "ByLayer", 50, 50, 5),
                NV("8", "LWPOLYLINE", "goc", "ByLayer"),
                NV("9", "ARC", "goc", "BYBLOCK"),               // BYBLOCK != Continuous -> bo (nhu lisp)
            };
            var cir = new List<NetVao>
            {
                ss[5],                                          // chinh no -> trung
                NV("C1", "CIRCLE", "0", "ByLayer", 50.3, 50.3, 2),   // cach 7 0.42 -> trung
                NV("C2", "CIRCLE", "0", "ByLayer", 100, 100, 3),
                NV("C3", "CIRCLE", "0", "ByLayer", 100.4, 100, 3),   // trung C2 vua them
            };
            var arc = new List<NetVao> { ss[3], NV("A1", "ARC", "0", "HIDDEN") };
            var kq = PhanLoaiNet.Chay("goc", "Continuous", ss, cir, arc);
            Kiem("lines = dao nguoc thu tu (2,1)", Hs(kq.Lines), "2,1");
            Kiem("arcs = A1 roi 4 (ARC ca ban ve them sau, khong trung)", Hs(kq.Arcs), "A1,4");
            Kiem("circles = C2,7,6 (C1/C3 trung tam <=0.5)", Hs(kq.Circles), "C2,7,6");
            Kiem("tiny = 6", Hs(kq.Tiny), "6");
            var kq2 = PhanLoaiNet.Chay("goc", "DASHED", ss, null, null);
            Kiem("layer goc DASHED: LINE ByLayer bo, LINE Continuous giu", Hs(kq2.Lines), "2");
            Kiem("layer goc khong co linetype -> Continuous", Hs(PhanLoaiNet.Chay("goc", null, ss, null, null).Lines), "2,1");
            // gioi han 500 phan tu
            var nhieu = new List<NetVao>();
            for (int i = 0; i < 600; i++) nhieu.Add(NV("N" + i, "CIRCLE", "0", "", i * 10, 0, 1));
            Kiem("chi xet 500 CIRCLE dau", PhanLoaiNet.Chay("goc", null, null, nhieu, null).Circles.Count, 500);
            Kiem("strcase chi doi a-z", PhanLoaiNet.HoaAscii("abａc"), "ABａC");
            PhanLoaiNet.WcsSangOcs(3, 4, 0, 0, 0, 1, out double ox, out double oy);
            Kiem("OCS phap tuyen +Z giu nguyen", S(ox) + "," + S(oy), "3.00,4.00");
            PhanLoaiNet.WcsSangOcs(3, 4, 0, 0, 0, -1, out ox, out oy);
            Kiem("OCS phap tuyen -Z (mirror): x doi dau", S(ox) + "," + S(oy), "-3.00,4.00");
        }

        private static int PhatLaiNhanNet(string file)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string lay = null, lt = null;
            var s = new List<NetVao>(); var c = new List<NetVao>(); var a = new List<NetVao>();
            foreach (string[] p in DumpBuoc.Doc(file))
            {
                if (p[0] == "G") { lay = p[1]; lt = p.Length > 2 && p[2] != "" ? p[2] : null; continue; }
                var n = new NetVao { Handle = p[1], Loai = p[2], Layer = p[3], Linetype = p[4], CoTam = p[5] == "1",
                                     X = double.Parse(p[6], inv), Y = double.Parse(p[7], inv), R = double.Parse(p[8], inv) };
                (p[0] == "S" ? s : p[0] == "C" ? c : a).Add(n);
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var kq = PhanLoaiNet.Chay(lay, lt, s, c, a);
            Console.WriteLine("Layer goc \"{0}\" (linetype {1}): vao {2}+{3}+{4} -> {5} LINE, {6} ARC, {7} CIRCLE ({8} rat nho), {9} ms",
                lay, lt ?? "-", s.Count, c.Count, a.Count, kq.Lines.Count, kq.Arcs.Count, kq.Circles.Count, kq.Tiny.Count, sw.ElapsedMilliseconds);
            return 0;
        }

        // ------------------------------------------------------------------
        // Muc 174: GD5c-a khop Dim dai khi thu thap Dim
        // ------------------------------------------------------------------
        private static string Hs(IEnumerable<DoanThang> ds) => string.Join(",", ds.Select(n => n.Handle));

        private static void TestDimDai()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD5c-a khop Dim dai (muc 174) ===");
            var A = new DoanThang("A", 0, 0, 100, 0);          // duong dim chu "100"
            var D = new DoanThang("D", 0, 50, 100, 50);        // song song nhung xa chu
            var T1 = new DoanThang("T1", 0, 1, 0, -20);        // chan dim trai
            var T2 = new DoanThang("T2", 100, 1, 100, -20);    // chan dim phai
            var L1 = new DoanThang("L1", 200, 0, 300, 0);      // duong dim "200" bi tach 2 doan (muc 169)
            var L2 = new DoanThang("L2", 300, 0, 400, 0);
            var LG = new DoanThang("LG", -100, 4, 200, 4);     // cham hop chu "100" nhung dai 300
            var lines = new List<DoanThang> { A, D, T1, T2, L1, L2, LG };
            var t1 = new TextDim { Handle = "t1", V = 100, Px = 50, Py = 2, Rot = 0, H = 3.5, Bb = new Hop(45, 1, 55, 4) };
            var t2 = new TextDim { Handle = "t2", V = 200, Px = 300, Py = 2, Rot = 0, H = 3.5, Bb = new Hop(295, 1, 305, 4) };
            var t3 = new TextDim { Handle = "t3", V = null, Px = 0, Py = 0, Rot = 0, H = 3.5 };
            var t4 = new TextDim { Handle = "t4", V = 50, Px = 50, Py = 25, Rot = 0, H = 3.5, Bb = new Hop(45, 23.5, 55, 26.5) };
            var kq = DimDai.KhopHet(new List<TextDim> { t1, t2, t3, t4 }, lines);
            Kiem("t1: duong dim A", kq[0] == null ? "null" : Hs(kq[0].Mem), "A");
            Kiem("t1: chan dim = dao nguoc thu tu (T2,T1)", kq[0] == null ? "null" : Hs(kq[0].Ticks), "T2,T1");
            Kiem("t1: p-mid + huong tia (0,-1)", kq[0] == null ? "null" : S(kq[0].PmX) + "," + S(kq[0].PmY) + " " + S(kq[0].RdX) + "," + S(kq[0].RdY), "50.00,0.00 0.00,-1.00");
            Kiem("t2: chuoi thang hang L2,L1 (thu tu cons)", kq[1] == null ? "null" : Hs(kq[1].Mem), "L2,L1");
            Kiem("t2: khong chan dim -> tia vuong goc (0,1)", kq[1] == null ? "null" : Hs(kq[1].Ticks) + "|" + S(kq[1].RdX) + "," + S(kq[1].RdY), "|-0.00,1.00");
            Kiem("t3: V nil -> nil", kq[2] == null, true);
            Kiem("t4: khong co duong ~V -> nil", kq[3] == null, true);
            Kiem("angle (-1,0) = pi", S(DimDai.Goc(0, 0, -1, 0)), S(Math.PI));
            Kiem("angle (0,-1) = 3pi/2", S(DimDai.Goc(0, 0, 0, -1)), S(1.5 * Math.PI));
            Kiem("lech phuong 0 va pi = 0", S(DimDai.LechGoc(0, Math.PI)), "0.00");
            Kiem("lech phuong 0.1 va pi-0.1 = 0.2", S(DimDai.LechGoc(0.1, Math.PI - 0.1)), "0.20");
            var nho = DimDai.NoHop15(null, 3);
            Kiem("bb nil -> hop 20x15 quanh goc", S(nho.X1) + "," + S(nho.Y1) + "," + S(nho.X2) + "," + S(nho.Y2), "-10.00,-7.50,10.00,7.50");
            Kiem("doan cat qua hop (2 dau ngoai)", DimDai.DoanChamHop(-10, 5, 20, 5, new Hop(0, 0, 10, 10)), true);
            Kiem("doan ngoai hop", DimDai.DoanChamHop(-10, 15, 20, 15, new Hop(0, 0, 10, 10)), false);
        }

        private static int PhatLaiDimDai(string file, bool b3 = false)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            double D(string s) => double.Parse(s, inv);
            var ts = new List<TextDim>(); var ls = new List<DoanThang>();
            foreach (string[] p in DumpBuoc.Doc(file))
            {
                if (p[0] == "L") ls.Add(new DoanThang(p[1], D(p[2]), D(p[3]), D(p[4]), D(p[5])));
                else if (p[0] == "T")
                    ts.Add(new TextDim { Handle = p[1], V = p[2] == "" ? (double?)null : D(p[2]), Px = D(p[3]), Py = D(p[4]), Rot = D(p[5]), H = D(p[6]),
                                         Bb = p[7] == "" ? null : new Hop(D(p[7]), D(p[8]), D(p[9]), D(p[10])) });
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (b3)
            {
                var k3 = DimDai3b.Chay(ts, ls);
                Console.WriteLine("{0} chu so, {1} LINE -> {2} net Dim, {3} chuoi muc 169 ({4} ms)", ts.Count, ls.Count, k3.Net.Count, k3.Chuoi.Count, sw.ElapsedMilliseconds);
                return 0;
            }
            var kq = DimDai.KhopHet(ts, ls);
            Console.WriteLine("{0} chu so, {1} LINE -> {2} co duong dim ({3} ms)", ts.Count, ls.Count, kq.Count(x => x != null), sw.ElapsedMilliseconds);
            for (int i = 0; i < ts.Count && i < 15; i++)
                Console.WriteLine("  {0} V={1}: {2}", ts[i].Handle, ts[i].V, kq[i] == null ? "-" : "dim " + Hs(kq[i].Mem) + " chan " + Hs(kq[i].Ticks));
            return 0;
        }

        // ------------------------------------------------------------------
        // Muc 175: GD3b khop Dim dai khi xu ly net (gro--geom-match-linear-dims)
        // ------------------------------------------------------------------
        private static void TestDimDai3b()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD3b khop Dim dai khi xu ly net (muc 175) ===");
            var A = new DoanThang("A", 0, 0, 100, 0);
            var D = new DoanThang("D", 0, 50, 100, 50);
            var T1 = new DoanThang("T1", 0, 1, 0, -20);
            var T2 = new DoanThang("T2", 100, 1, 100, -20);
            var L1 = new DoanThang("L1", 200, 0, 300, 0);
            var L2 = new DoanThang("L2", 300, 0, 400, 0);
            var LG = new DoanThang("LG", -100, 4, 200, 4);
            var T1b = new DoanThang("T1b", 0, -20.5, 0, -40);   // noi thang hang voi chan dim T1
            var K1 = new DoanThang("K1", 585, 0, 585, 10);      // cap tick cua "30" (khong co duong dim ~V)
            var K2 = new DoanThang("K2", 615, 0, 615, 10);
            var M = new DoanThang("M", 587, 5, 613, 5);         // manh duong dim giua 2 tick
            var lines = new List<DoanThang> { A, D, T1, T2, L1, L2, LG, T1b, K1, K2, M };
            var texts = new List<TextDim>
            {
                new TextDim { Handle = "t1", V = 100, Px = 50, Py = 2, Rot = 0, H = 3.5, Bb = new Hop(45, 1, 55, 4) },
                new TextDim { Handle = "t2", V = 200, Px = 300, Py = 2, Rot = 0, H = 3.5, Bb = new Hop(295, 1, 305, 4) },
                new TextDim { Handle = "t3", V = 30, Px = 600, Py = 5, Rot = 0, H = 3.5, Bb = new Hop(595, 3.5, 605, 6.5) },
                new TextDim { Handle = "t4", V = null, Px = 0, Py = 0, Rot = 0, H = 3.5 },
            };
            var kq = DimDai3b.Chay(texts, lines);
            Kiem("res-dim-ents dung thu tu cons (gom ca chuoi + cap tick)", Hs(kq.Net), "M,K1,K2,L1,L2,T2,T1b,T1,A");
            Kiem("chu so dung chuoi muc 169: chu #1, 2 LINE", string.Join(";", kq.Chuoi.Select(c => c[0] + "," + c[1])), "1,2");
            // trang thai tich luy: chu sau khong them lai net da co
            var kq2 = DimDai3b.Chay(new List<TextDim> { texts[0], texts[0] }, lines);
            Kiem("2 lan cung chu -> khong trung", Hs(kq2.Net), "T2,T1b,T1,A");
        }

        // ------------------------------------------------------------------
        // Muc 176: GD5c-b vong loc dau thu thap Dim
        // ------------------------------------------------------------------
        private static DimNetVao DN(string h, string loai, string lay, double x1, double y1, double x2, double y2, bool giay = false, bool khoi = false) =>
            new DimNetVao { Handle = h, Loai = loai, Layer = lay, Giay = giay, LaKhoi = khoi, Bb = new Hop(x1, y1, x2, y2) };

        private static string Hs(IEnumerable<DimNetVao> ds) => string.Join(",", ds.Select(n => n.Handle));

        private static void TestLocDimNet()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD5c-b vong loc dau thu thap Dim (muc 176) ===");
            var note = new Hop(500, 500, 600, 520);
            var ss = new List<DimNetVao>
            {
                DN("L1", "LINE", "1-SUNPOU", 0, 0, 100, 0),
                DN("L2", "LINE", "0-KAKOU-X", 0, 0, 100, 0),          // chua 0-KAKOU -> bo
                DN("L3", "LINE", "1-SUNPOU", 0, 0, 100, 0, giay: true), // khong gian giay -> bo
                DN("L4", "LINE", "1-SUNPOU", 0, 0, 100, 0, khoi: true), // cum bango -> bo
                DN("L5", "LINE", "1-SUNPOU", 540, 505, 560, 515),     // tam trong hop ghi chu -> bo
                DN("L6", "LINE", "1-SUNPOU", 0, 0, 2500, 0),          // qua dai -> bo
                new DimNetVao { Handle = "L7", Loai = "LINE", Layer = "1-SUNPOU" },  // khong co hop -> bo
                DN("A1", "ARC", "dim", 0, 0, 10, 10),
                DN("D1", "DIMENSION", "DIM", 0, 0, 50, 10),
                DN("T1", "TEXT", "1-SUNPOU", 0, 0, 10, 3),            // TEXT khong gom
                DN("L8", "LINE", "khung_in2", 0, 0, 100, 0),          // strcase -> KHUNG_IN -> bo
                DN("L9", "LINE", "MY_DIM", 0, 0, 100, 0),
                DN("L10", "LINE", "1-SUNPOU", 0, 0, 100, 0),
            };
            var kq = LocDimNet.Chay(ss, new List<Hop> { null, note });
            Kiem("LINE qua loc, dao thu tu (cons)", Hs(kq.Lines), "L10,L9,L1");
            Kiem("ARC", Hs(kq.Arcs), "A1");
            Kiem("DIMENSION", Hs(kq.Natives), "D1");
            Kiem("tam sat mep hop ghi chu (nới 1.0) -> trong", LocDimNet.TrongGhiChu(new Hop(600.5, 510, 601.5, 510), new List<Hop> { note }), true);
            Kiem("tam ngoai hop ghi chu 1.5 -> ngoai", LocDimNet.TrongGhiChu(new Hop(601, 510, 602, 510), new List<Hop> { note }), false);
        }

        // ------------------------------------------------------------------
        // Muc 177: GD3c do vong kin bien dang khi nhan Dim
        // ------------------------------------------------------------------
        private static NetVk VK(string h, string loai, string lay, double x1, double y1, double x2, double y2, bool kin = false) =>
            new NetVk { Handle = h, Loai = loai, Layer = lay, Kin = kin, CoDau = true, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };

        private static void TestVongKin()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD3c do vong kin (muc 177) ===");
            var ss = new List<NetVk>
            {
                VK("R1", "LINE", "goc", 0, 0, 10, 0),
                VK("R2", "ARC", "goc", 10, 0, 10, 10),
                VK("R3", "LINE", "GOC", 10, 10, 0.00000001, 10),   // lech 1e-8 -> cung khoa
                VK("R4", "LINE", "goc", 0, 10, 0, 0),
                VK("B", "LINE", "goc", 10, 10, 20, 20),            // nhanh treo 2 doan
                VK("B2", "LINE", "goc", 20, 20, 30, 20),
                VK("X", "LINE", "goc", 100, 0, 110, 0),            // net le
                VK("P", "LWPOLYLINE", "goc", 0, 0, 0, 0, kin: true),// polyline kin -> khong xet
                VK("K", "LINE", "khac", 0, 0, 10, 10),             // khac layer
                VK("C", "CIRCLE", "goc", 0, 0, 0, 0),              // khong phai loai xet
            };
            var vk = VongKin.TrenLayer("goc", ss);
            Kiem("vong kin = 4 net, thu tu sau 3 luot tia (dao moi luot)", string.Join(",", vk.Select(n => n.Handle)), "R4,R3,R2,R1");
            Kiem("khoa diem: 1234.25 -> 1234.3 (nua ra xa 0)", VongKin.Tron1(1234.25), "1234.3");
            Kiem("khoa diem: 9.99999999 va 10.0000001 cung khoa", VongKin.Tron1(9.99999999) == VongKin.Tron1(10.0000001), true);
            Kiem("khoa diem: -0.04 -> 0.0", VongKin.Tron1(-0.04), "0.0");
            Kiem("khoa diem am: -12.36 -> -12.4", VongKin.Tron1(-12.36), "-12.4");
        }

        // ------------------------------------------------------------------
        // Muc 178: GD6a xep cum sang cot phai
        // ------------------------------------------------------------------
        private static EntXc EX(string h, string lay, double x1, double y1, double x2, double y2, bool chup = false, bool poly = false) =>
            new EntXc { H = h, Layer = lay, ChupCopy = chup, LaPoly = poly, Bb = new Hop(x1, y1, x2, y2) };

        private static void TestXepCum()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD6a xep cum sang cot phai (muc 178) ===");
            var c1 = new CumXc { Tid = 5, Nhom = 0, Mirror1 = true, Bb = new Hop(100, 800, 300, 900), Ents = new List<string> { "a1", "a2" } };
            var c2 = new CumXc { Tid = 7, Nhom = 0, Bb = new Hop(400, 800, 500, 850), Ents = new List<string> { "b1" } };
            var c3 = new CumXc { Tid = 9, Nhom = 1, Bb = new Hop(100, 100, 200, 200), Ents = new List<string> { "c1" } };
            var ss = new List<EntXc>
            {
                EX("O", "0", 2100, 900, 2500, 950),              // vat can trong vung cot -> X_L = 3500
                EX("O2", "0", 3600, 0, 3700, 10),                // chi vuong SAU khi X_L da doi -> 4700
                EX("CP", "0", 4800, 0, 9000, 10, chup: true),    // ban chup -> khong tinh
                EX("CL", "X_CHUP", 4800, 0, 9000, 10),           // layer *_CHUP -> khong tinh
                EX("a1", "0-KAKOU", 110, 810, 150, 850),
                EX("a2", "0", 120, 820, 130, 830),
                EX("b1", "0", 410, 810, 420, 820),
                EX("c1", "0", 110, 110, 120, 120),
                EX("s1", "0", 200, 850, 210, 860),               // con sot o cho cum 1
                EX("d1", "1-SUNPOU", 150, 820, 160, 830),        // dim chua gan nam tron trong hop cu -> doi theo
                EX("k1", "0", 250, 850, 260, 860),               // cua tam khong xep (es-khac) -> bo
                EX("f1", "KHUNG", 0, 0, 1000, 1000, poly: true), // khung pham vi -> bo
                EX("s2", "0", 440, 815, 460, 825),               // con sot o cho cum 2
            };
            var kq = XepCum.Chay(ss, new List<CumXc> { c1, c2, c3 }, new Hop(0, 0, 1000, 1000),
                                 new HashSet<string> { "k1" }, new HashSet<string> { "f1" }, new List<Hop>(), 100, 15);
            Kiem("X_L tang theo thu tu ss (2000 -> 3500 -> 4700), da ne", S(kq.XL) + " " + kq.DaNe, "4700.00 True");
            Kiem("cum 1: dx dy", S(kq.Dat[0].Dx) + "," + S(kq.Dat[0].Dy), "4600.00,40.00");
            Kiem("cum 2 cung nhom: khe 50", S(kq.Dat[1].Dy) + " y2=" + S(kq.Dat[1].BbMoi.Y2), "-60.00 y2=790.00");
            Kiem("cum 3 nhom moi: khe 300 + nhan", S(kq.Dat[2].Dy) + " y2=" + S(kq.Dat[2].BbMoi.Y2), "190.00 y2=390.00");
            Kiem("2 nhan nhom (0 tai 990, 1 tai 440)", string.Join(";", kq.Nhans.Select(n => n.Nhom + "@" + S(n.Y))), "0@990.00;1@440.00");
            Kiem("con sot (thu tu cons)", kq.NSot + ":" + string.Join(",", kq.Sot), "2:s2,s1");
            Kiem("dim 1-SUNPOU doi theo cum 1", kq.NDimTheo + " " + S(kq.DoDoi["d1"][0]) + "," + S(kq.DoDoi["d1"][1]), "1 4600.00,40.00");
            Kiem("entity cum doi dung do doi", S(kq.DoDoi["b1"][0]) + "," + S(kq.DoDoi["b1"][1]), "4300.00,-60.00");
            Kiem("khung pham vi theo hop (dung sai 0.5)", XepCum.LaKhungPhamVi(EX("p", "0", 0, 0, 10, 10, poly: true), new Hop(0.3, 0, 10, 10), null, new List<Hop> { new Hop(0, 0, 10, 10) }), true);
        }

        // ------------------------------------------------------------------
        // Muc 180: GD6f tach va cham khung in
        // ------------------------------------------------------------------
        private static KhungTg KT(int tid, double x1, double y1, double x2, double y2) => new KhungTg { Tid = tid, Bb = new Hop(x1, y1, x2, y2) };

        private static void TestTachVaCham()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD6f tach va cham khung in (muc 180) ===");
            var none = new HashSet<int>();
            var kq = TachVaCham.Chay(new List<KhungTg> { KT(1, 0, 0, 100, 100), KT(2, 80, 10, 150, 60) }, none, 50, 20);
            Kiem("tam nho (2) doi sang phai 70 (huong ngan nhat, khe 50)",
                 string.Join(";", kq.Doi.Select(d => d.ViTri + ":" + d.Tid + ":" + S(d.Dx) + "," + S(d.Dy))), "1:2:70.00,0.00");
            Kiem("het chong", kq.ConChong, 0);
            var kq2 = TachVaCham.Chay(new List<KhungTg> { KT(1, 0, 0, 100, 100), KT(2, 80, 10, 150, 60) }, new HashSet<int> { 2 }, 50, 20);
            Kiem("tam trong cot phai (bo qua) dung yen -> tam 1 doi", kq2.Doi.Count > 0 ? kq2.Doi[0].Tid : -1, 1);
            var kq3 = TachVaCham.Chay(new List<KhungTg> { KT(5, 0, 0, 100, 100), KT(3, 50, 0, 150, 100) }, none, 50, 20);
            Kiem("bang dien tich -> tid nho (3) dung yen, tid 5 doi", kq3.Doi.Count > 0 ? kq3.Doi[0].Tid : -1, 5);
            Kiem("dem chong bo cap ca hai trong cot phai",
                 TachVaCham.DemChong(new List<KhungTg> { KT(1, 0, 0, 10, 10), KT(2, 5, 5, 15, 15) }, new HashSet<int> { 1, 2 }), 0);
            Kiem("giao phai rong hon 0.1", TachVaCham.HcnGiao(new Hop(0, 0, 10, 10), new Hop(9.95, 0, 20, 10)) == null, true);
        }

        // ------------------------------------------------------------------
        // Muc 181: GD5d-a chia net cat / kegaki vao danh sach chi tiet
        // ------------------------------------------------------------------
        private static NetCat NC(string h, double cx, double cy, bool bo = false) =>
            new NetCat { H = h, Bo = bo, Bb = new Hop(cx - 1, cy - 1, cx + 1, cy + 1) };

        private static void TestChiaNetCat()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD5d-a chia net cat vao danh sach chi tiet (muc 181) ===");
            var m1 = new MucCt { Tid = 1, BbF = new Hop(0, 0, 100, 100), BbC = new Hop(10, 10, 90, 90), Pt9 = new List<string> { "p1" } };
            var m2 = new MucCt { Tid = 2, BbF = new Hop(90, 0, 200, 100), BbC = new Hop(110, 10, 190, 90) };
            var ss = new List<NetCat>
            {
                NC("n1", 50, 50),            // chi trong khung 1
                NC("n2", 95, 50),            // trong ca 2 khung -> gan vung net cat 1 hon (5 < 15)
                NC("n3", 250, 50),           // ngoai moi khung, cach vung net cat 2 la 60 > 30 -> bo
                NC("n4", 215, 50),           // ngoai khung, cach vung net cat 2 la 25 -> tam 2
                NC("n5", 50, 50, bo: true),  // ban chup / giay -> bo
                NC("p1", 40, 40),            // da co trong pt9 -> khong trung
            };
            var kq = ChiaNetCat.Chay(ss, new List<MucCt> { m1, m2 });
            Kiem("so net xet (bo net bi loai)", kq.SoNet, 5);
            Kiem("pt9 tam 1 = net gan them theo thu tu ss + pt9 cu, bo trung", string.Join(",", kq.Pt9[0]), "n1,n2,p1");
            Kiem("pt9 tam 2 = net ngoai khung <= 30", string.Join(",", kq.Pt9[1]), "n4");
            var kq0 = ChiaNetCat.Chay(new List<NetCat>(), new List<MucCt> { new MucCt { Tid = 1, Pt9 = new List<string> { "a", "a" } } });
            Kiem("khong co net nao -> pt9 giu nguyen (ke ca trung)", string.Join(",", kq0.Pt9[0]), "a,a");
        }

        // ------------------------------------------------------------------
        // Muc 183: GD5d-b dong khung + ve Khung_IN
        // ------------------------------------------------------------------
        private static string H4(Hop b) => S(b.X1) + "," + S(b.Y1) + "," + S(b.X2) + "," + S(b.Y2);

        private static void TestDongKhung()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD5d-b dong khung + ve Khung_IN (muc 183) ===");
            var t1 = new TamDk { Tid = 1, TBb = new Hop(0, 0, 100, 20), CoKhoi = true, KhoiBb = new Hop(0, -30, 40, -10) };
            t1.HinhBb.Add(new Hop(0, 30, 100, 130));
            t1.Dims.Add(new DimDk { Bb = new Hop(110, 10, 120, 130) });                  // cham ghi chu tam 2 -> bo
            t1.Dims.Add(new DimDk { Bb = new Hop(-20, 30, -10, 130), ChacChan = true }); // gan chac -> giu
            var t2 = new TamDk { Tid = 2, TBb = new Hop(115, 0, 200, 20) };
            t2.HinhBb.Add(new Hop(130, 30, 200, 130));
            var kq = DongKhung.Chay(new List<TamDk> { t1, t2 });
            Kiem("tam 1: bo Dim cham tam khac, giu Dim chac chan", string.Join(",", kq[0].GiuDim), "False,True");
            Kiem("tam 1: hop + le co theo khe 15 -> 5.5 (X), le 15 (Y)", H4(kq[0].BbFinal), "-25.50,-45.00,105.50,145.00");
            Kiem("tam 2: khe toi ghi chu + bango tam 1 = 15 -> le X 5.5", H4(kq[1].BbFinal), "109.50,-15.00,205.50,145.00");
            var don = DongKhung.Chay(new List<TamDk> { new TamDk { Tid = 7, TBb = new Hop(0, 0, 10, 10) } });
            Kiem("tam le khong hinh / Dim: le 15", H4(don[0].BbFinal), "-15.00,-15.00,25.00,25.00");
        }

        private static void TestQuetHinh()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD5a quet + gom hinh 0-KAKOU (muc 172) ===");
            // tam A: 4 LINE chu nhat 0..100 x 0..50 + 1 lo tron trong; tam B: 4 LINE 200..260 x 0..40; net le cach B 2mm
            var nets = new List<NetKakou>();
            int Them(double x1, double y1, double x2, double y2) { nets.Add(new NetKakou(nets.Count, new Hop(x1, y1, x2, y2))); return nets.Count - 1; }
            Them(0, 0, 100, 0); Them(100, 0, 100, 50); Them(0, 50, 100, 50); Them(0, 0, 0, 50);
            int lo = Them(40, 20, 50, 30);
            Them(200, 0, 260, 0); Them(260, 0, 260, 40); Them(200, 40, 260, 40); Them(200, 0, 200, 40);
            int le = Them(262, 10, 270, 10);           // cach B 2mm -> gop vao B
            int xa = Them(400, 0, 401, 1);             // hinh 1x1 -> qua nho, bo
            var kq = QuetHinh.Gom(nets, null, null);
            Kiem("2 hinh (lo gop vao phoi, net 1x1 bo)", kq.Hinh.Count, 2);
            Kiem("hinh #1 = dien tich lon nhat (tam A 100x50)", kq.Hinh[0].Bb.W + "x" + kq.Hinh[0].Bb.H, "100x50");
            Kiem("  lo tron KHONG thanh hinh rieng", kq.Hinh.Any(h => h.ThanhVien.Contains(lo) && h.ThanhVien.Count == 1), false);
            Kiem("net cach 2mm gop vao tam B (khe <= 2.5)", kq.Hinh[1].ThanhVien.Contains(le), true);
            Kiem("  hop tam B mo rong toi 270", kq.Hinh[1].Bb.X2, 270.0);
            Kiem("net 1x1 bi bo", kq.Hinh.Any(h => h.ThanhVien.Contains(xa)), false);
            // khe 3mm -> khong gop
            var n2 = new List<NetKakou> { new NetKakou(0, new Hop(0, 0, 10, 10)), new NetKakou(1, new Hop(13, 0, 23, 10)) };
            Kiem("khe 3mm -> 2 hinh rieng", QuetHinh.Gom(n2, null, null).Hinh.Count, 2);
            // khung pham vi: bo net ngoai khung, bo chinh khung, bo 4 net mep khung
            var kb = new List<Hop> { new Hop(-20, -20, 300, 100) };
            var n3 = new List<NetKakou>(nets)
            {
                new NetKakou(100, new Hop(-20, -20, 300, 100)),   // chinh khung (polyline)
                new NetKakou(101, new Hop(-20, 100, 300, 100)),   // mep tren khung (LINE)
                new NetKakou(102, new Hop(500, 0, 600, 50)),      // ngoai khung
            };
            var kq3 = QuetHinh.Gom(n3, kb, null);
            Kiem("khung pham vi: van 2 hinh (bo khung, mep khung, net ngoai)", kq3.Hinh.Count, 2);
            Kiem("  so net sau loc = 10 (11 + 3, bo khung + mep + 2 net ngoai khung)", kq3.SoNetSauLoc, 10);
            // hinh chua >= 2 diem ghi chu -> khung ve nham, bao loi
            var kq4 = QuetHinh.Gom(nets, null, new List<Diem> { new Diem(10, 10), new Diem(90, 40) });
            // bo khung nham TRUOC buoc gop lo (nhu lisp) -> lo cua tam A thanh hinh rieng
            Kiem("hinh chua 2 ghi chu -> bao khung nham; lo ben trong thanh hinh rieng", kq4.LoiKhung.Count + "/" + kq4.Hinh.Count, "1/2");
            // thu tu dau vao khong doi nhom
            var dao = new List<NetKakou>(nets); dao.Reverse();
            var kq5 = QuetHinh.Gom(dao, null, null);
            Kiem("dao thu tu dau vao -> cung nhom", string.Join("|", kq5.Hinh.Select(h => string.Join(",", h.ThanhVien.OrderBy(i => i)))),
                 string.Join("|", kq.Hinh.Select(h => string.Join(",", h.ThanhVien.OrderBy(i => i)))));
            // hai hinh CUNG dien tich: sap ON DINH (nhu vl-sort lisp) - muc 172 so A/B that
            var hoa = new List<NetKakou>
            {
                new NetKakou(0, new Hop(0, 0, 50, 20)),      // gap truoc
                new NetKakou(1, new Hop(100, 0, 150, 20)),   // gap sau, cung dien tich
            };
            var kqHoa = QuetHinh.Gom(hoa, null, null);
            Kiem("hoa dien tich: hinh gap TRUOC duoc id 1 (sap on dinh)", kqHoa.Hinh[0].ThanhVien[0], 0);
            // nhom xep theo net DAU TIEN cua nhom (hat giong lisp) - net xen ke giua 2 nhom
            var xen = new List<NetKakou>
            {
                new NetKakou(0, new Hop(100, 0, 110, 20)),   // nhom B (gap truoc)
                new NetKakou(1, new Hop(0, 0, 10, 20)),      // nhom A
                new NetKakou(2, new Hop(110, 0, 150, 20)),   // nhom B (noi tiep 0)
                new NetKakou(3, new Hop(10, 0, 50, 20)),     // nhom A
            };
            Kiem("2 nhom cung dien tich, nhom co net gap truoc -> id 1", QuetHinh.Gom(xen, null, null).Hinh[0].ThanhVien.Min(), 0);
            // chuoi 1000 net lien nhau -> 1 hinh (kiem toc do + lien thong bac cau)
            var chuoi = new List<NetKakou>();
            for (int i = 0; i < 1000; i++) chuoi.Add(new NetKakou(i, new Hop(i * 10, 0, i * 10 + 9, 5)));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var kq6 = QuetHinh.Gom(chuoi, null, null);
            Kiem("1000 net noi tiep khe 1mm -> 1 hinh", kq6.Hinh.Count, 1);
            Kiem("  duoi 200 ms", sw.ElapsedMilliseconds < 200, true);
        }

        private static void TestGd0()
        {
            Console.WriteLine();
            Console.WriteLine("=== GD0 chuyen lisp -> C#: cong tac, phien, so A/B, dump (muc 171) ===");
            Kiem("che do rong -> lisp", GroCongTac.DocCheDo(null), CheDoBuoc.Lisp);
            Kiem("che do 'AB' -> ab", GroCongTac.DocCheDo(" AB "), CheDoBuoc.AB);
            Kiem("che do 'c#' -> cs", GroCongTac.DocCheDo("c#"), CheDoBuoc.CS);
            Kiem("che do la -> lisp (an toan)", GroCongTac.DocCheDo("xyz"), CheDoBuoc.Lisp);
            Kiem("ten khoa registry", GroCongTac.TenKhoa(" PhanLoai "), "cs_phanloai");
            Kiem("buoc co trong danh sach", GroCongTac.LaBuoc("XULYNET"), true);
            Kiem("buoc khong co", GroCongTac.LaBuoc("abc"), false);
            Kiem("20 buoc theo ke hoach (+ 7 buoc con 173-180, dsct 181, dongkhung 183)", GroCongTac.Buoc.Count, 20);

            var p1 = GroPhien.Moi("A.dwg");
            p1.Dat("x", new List<int> { 1, 2 });
            Kiem("phien: doc lai du lieu", GroPhien.Lay("a.DWG").Doc<List<int>>("x")?.Count, 2);
            GroPhien.Moi("A.dwg");
            Kiem("phien moi xoa du lieu cu", GroPhien.Lay("A.dwg").Co("x"), false);
            Kiem("phien rieng tung ban ve", GroPhien.Lay("B.dwg").Co("x"), false);

            var lisp = SoSanh.DocChuoi(new[] { "2A1|x=10.00000001|lop=0-KAKOU", "2A2|x=5|lop=1-SUNPOU", "2A3|x=1" });
            var cs = new List<SoSanh.BanGhi>
            {
                new SoSanh.BanGhi("2A1").Dat("x", 10.0).Dat("lop", "0-KAKOU"),
                new SoSanh.BanGhi("2A2").Dat("x", 5.0).Dat("lop", "0-KAKOU"),
                new SoSanh.BanGhi("2A4").Dat("x", 1.0),
            };
            var kq = SoSanh.So("thu", lisp, cs, 1e-6);
            Kiem("so A/B: 1 khop", kq.Khop, 1);
            Kiem("so A/B: 3 lech (sai layer, chi lisp, chi C#)", kq.Lech.Count, 3);
            Kiem("  lech ghi ro truong", kq.Lech.Any(l => l.StartsWith("2A2: lop lisp=1-SUNPOU C#=0-KAKOU")), true);
            Kiem("  dong in ra bat dau [AB thu] LECH 3", kq.InRa().StartsWith("[AB thu] LECH 3 (khop 1):"), true);
            Kiem("so A/B khop het", SoSanh.So("thu", lisp.Take(1), cs.Take(1), 1e-6).InRa(), "[AB thu] khop 1 muc");
            Kiem("so so voi dung sai", SoSanh.Bang("1.0004", "1", 1e-3), true);
            Kiem("so chu khac nhau", SoSanh.Bang("a", "b", 1), false);

            string f = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gro_test_dump.txt");
            new DumpBuoc("test").Dong("T", "a\tb", "c").Dong("N", "1").Ghi(f);
            var doc = DumpBuoc.Doc(f);
            Kiem("dump: 2 dong (bo dong #)", doc.Count, 2);
            Kiem("dump: TAB trong truong doi thanh cach", doc[0][1], "a b");
            Kiem("dump: duong dan theo buoc", DumpBuoc.DuongDan("XuLyNet").EndsWith("gro_xulynet_dump.txt"), true);
            try { System.IO.File.Delete(f); } catch { }
        }

        /// <summary>
        /// Phat lai file dump: chay DatMaKhac.ChayHet, in dong [MK] tung tam + chan doan bien ngoai.
        /// bango != null -> chi in chi tiet tam do.
        /// </summary>
        private static int PhatLaiDatMk(string file, string bango)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var dong = new List<string>();
            var net = new List<NetMau>();
            foreach (string l in System.IO.File.ReadAllLines(file))
            {
                string[] p = l.Split('\t');
                if (p[0] == "L") dong.Add(p[1]);
                else if (p[0] == "N")
                {
                    var pts = p[3].Split(';').Select(s => s.Split(','))
                                  .Select(a => new Diem(double.Parse(a[0], inv), double.Parse(a[1], inv))).ToList();
                    DatMaKhac.DocHop(p[4], out Hop bb);
                    net.Add(new NetMau { Layer = p[1], Kin = p[2] == "1", Pts = pts, Bb = bb });
                }
            }
            var ds = new List<DatMaKhac.DongTam>();
            var vc = new List<Hop>();
            DatMaKhac.DocDuLieu(dong, ds, vc, out int loi);
            Console.WriteLine("Doc: {0} tam, {1} vat can, {2} dong hong, {3} net", ds.Count, vc.Count, loi, net.Count);
            var kq = DatMaKhac.ChayHet(ds.Select(d => d.Tam).ToList(), net, vc);
            foreach (var r in kq.Tams)
            {
                if (bango != null && r.Tam.Bango != bango) continue;
                Console.WriteLine(r.DongChanDoan);
                Hop bb = r.Tam.BbHinh, bb1 = bb.No(1.0);
                var cua = net.Where(n => n.Bb.Giao(bb1)).ToList();
                var b = DatMaKhac.BienTam(net, bb);
                Console.WriteLine("     net cham hop: {0} (kin trong hop {1}, KAKOU ho trong hop {2}, KEGAKI {3})  bien ngoai: {4}  doan lo: {5}",
                    cua.Count, cua.Count(n => !n.LaKegaki && n.Kin && bb1.Chua(n.Bb)),
                    cua.Count(n => n.LaKakou && !n.Kin && bb1.Chua(n.Bb)), cua.Count(n => n.LaKegaki),
                    b.ngoai == null ? "KHONG" : b.ngoai.Count + " doan", b.lo == null ? "-" : b.lo.Count.ToString());
                if (bango != null)
                    foreach (var n in cua)
                        Console.WriteLine("       {0,-9} kin={1} {2} diem  bb {3:0.0},{4:0.0}..{5:0.0},{6:0.0}  dau {7:0.0},{8:0.0} cuoi {9:0.0},{10:0.0}",
                            n.Layer, n.Kin ? 1 : 0, n.Pts.Count, n.Bb.X1, n.Bb.Y1, n.Bb.X2, n.Bb.Y2,
                            n.Pts[0].X, n.Pts[0].Y, n.Pts[n.Pts.Count - 1].X, n.Pts[n.Pts.Count - 1].Y);
            }
            Console.WriteLine(kq.ThongBao);
            return 0;
        }

        private static void TestDatMaKhac()
        {
            Console.WriteLine();
            Console.WriteLine("=== Dat ma khac vao hinh - che do 2 (muc 170, C#) ===");

            // 1. Tam chu nhat 200x100, cum 40x20: buoc = 100/80 = 1.25, khe c = 1.
            //    x0 = 0+20+1 = 21 cham canh trai (khe c tinh ca CHAM) -> 22.25; hang dau y=89 cham canh tren -> 87.75
            var hinh = new Hop(0, 0, 200, 100);
            var net1 = new List<NetMau> { ChuNhat(0, 0, 200, 100) };
            KetQuaTam r1 = DatMaKhac.DatMotTam(net1, hinh, 40, 20, new List<Doan>());
            Kiem("tam 200x100 cum 40x20 -> dat duoc", r1.Dat, true);
            Kiem("  vi tri dau tien tu goc tren-trai (22.25, 87.75)", S(r1.Cx) + "," + S(r1.Cy), "22.25,87.75");
            Kiem("  goc 0, ti le 1, khong bo KEGAKI", r1.Th + "/" + r1.TiLe + "/" + r1.BoKeg, "0/1/False");
            Kiem("  nua canh vat can da cong khe c=1", S(r1.Hw) + "x" + S(r1.Hh), "21.00x11.00");

            // 2. Lo (vung cam) o goc tren-trai -> cum truot sang phai lo
            var net2 = new List<NetMau> { ChuNhat(0, 0, 200, 100), ChuNhat(10, 60, 60, 95) };
            KetQuaTam r2 = DatMaKhac.DatMotTam(net2, hinh, 40, 20, new List<Doan>());
            Kiem("lo o goc tren-trai -> cum khong chong len lo",
                 r2.Dat && (r2.Cx - r2.Hw >= 60 || r2.Cy + r2.Hh <= 60), true);

            // 3. Tam hep 30x200, cum 60x20 -> phai XOAY 90 do
            var hinh3 = new Hop(0, 0, 30, 200);
            KetQuaTam r3 = DatMaKhac.DatMotTam(new List<NetMau> { ChuNhat(0, 0, 30, 200) }, hinh3, 60, 20, new List<Doan>());
            Kiem("tam hep 30x200 cum 60x20 -> xoay 90 do", r3.Dat ? S(r3.Th * 180 / Math.PI) : "khong dat", "90.00");
            // muc 187: le mep 0.1 chi khi CA HAI canh < 50 (truoc: chi can 1 canh < 100)
            Kiem("le mep: tam 40x1000 -> 1.0 (truoc 0.1)", S(DatMaKhac.LeMep(new Hop(0, 0, 40, 1000))), "1.00");
            Kiem("le mep: tam 1000x40 -> 1.0", S(DatMaKhac.LeMep(new Hop(0, 0, 1000, 40))), "1.00");
            Kiem("le mep: tam 40x45 (ca 2 canh < 50) -> 0.1", S(DatMaKhac.LeMep(new Hop(0, 0, 40, 45))), "0.10");
            Kiem("le mep: tam 60x80 (truoc 0.1) -> 1.0", S(DatMaKhac.LeMep(new Hop(0, 0, 60, 80))), "1.00");
            Kiem("le mep: tam 50x40 (canh = 50 khong < 50) -> 1.0", S(DatMaKhac.LeMep(new Hop(0, 0, 50, 40))), "1.00");
            Kiem("  tam hep 30x200: nua canh vat can cong khe 1.0", S(r3.Hw) + "x" + S(r3.Hh), "31.00x11.00");

            // 4. Tam qua nho -> khong dat, bao CUM QUA LON (0 vi tri)
            var hinh4 = new Hop(0, 0, 30, 30);
            var r4t = DatMaKhac.ChayHet(new List<TamCanDat> { Tam("7", hinh4, 100, 40) },
                                         new List<NetMau> { ChuNhat(0, 0, 30, 30) }, null);
            Kiem("tam 30x30 cum 100x40 -> khong dat", r4t.Tams[0].Dat, false);
            Kiem("  dong chan doan bao CUM QUA LON",
                 r4t.Tams[0].DongChanDoan.EndsWith("(truot 0 vi tri: cham net 0, ngoai thit 0 - CUM QUA LON)"), true);
            Kiem("  bao cao chua dat", r4t.ThongBao,
                 "Dat ma khac vao hinh: 0/1 tam (thu nho 1/2: 0, de KEGAKI: 0)\n" +
                 "!!! Chua dat duoc ma khac vao hinh (1 tam) - move thu cong: bango 7");

            // 5. KEGAKI ke ngang day dac -> chi dat duoc khi BO QUA KEGAKI
            var net5 = new List<NetMau> { ChuNhat(0, 0, 200, 100) };
            for (int y = 5; y < 100; y += 8) net5.Add(Net("3-KEGAKI", false, 0, y, 200, y));
            KetQuaTam r5 = DatMaKhac.DatMotTam(net5, hinh, 40, 20, new List<Doan>());
            Kiem("KEGAKI day dac -> dat, BO KEGAKI", r5.Dat + "/" + r5.BoKeg, "True/True");

            // 6. Cum 210x60 khong vua tam 200x100 o ca 3 goc -> thu nho 1/2
            KetQuaTam r6 = DatMaKhac.DatMotTam(net1, hinh, 210, 60, new List<Doan>());
            Kiem("cum 210x60 trong tam 200x100 -> thu nho 1/2", r6.Dat + "/" + r6.TiLe, "True/0.5");

            // 7. Ca luot: 2 tam chung mot hinh -> tam sau bao chung hinh
            var kq7 = DatMaKhac.ChayHet(new List<TamCanDat> { Tam("3", hinh, 40, 20), Tam("4", new Hop(0.5, 0, 200, 100.4), 40, 20) },
                                         net1, null);
            Kiem("2 tam chung hinh -> tam 4 khong dat", kq7.Tams[1].Dat, false);
            Kiem("  dong chan doan 'chung hinh voi bango 3'", kq7.Tams[1].DongChanDoan.EndsWith("KHONG - chung hinh voi bango 3"), true);
            Kiem("  bao cao ghi ly do", kq7.ThongBao.EndsWith("bango 4 (chung hinh voi bango 3)"), true);

            // 8. Text khac chu (muc 156) la vat can
            var net8 = new List<NetMau> { ChuNhat(0, 0, 200, 100), ChuNhat(300, 0, 500, 100) };
            var kq8 = DatMaKhac.ChayHet(new List<TamCanDat> { Tam("1", hinh, 40, 20), Tam("2", new Hop(300, 0, 500, 100), 40, 20) },
                                         net8, new List<Hop> { new Hop(310, 70, 380, 98) });
            Kiem("text khac chu la vat can -> tam 2 dat ben phai text",
                 kq8.Tams[1].Dat && kq8.Tams[1].Cx - kq8.Tams[1].Hw >= 380, true);
            Kiem("bao cao dung mau BaoCaoGro (2/2)", kq8.ThongBao, "Dat ma khac vao hinh: 2/2 tam (thu nho 1/2: 0, de KEGAKI: 0)");

            // 9. Bien ngoai ve bang 4 LINE + 1 net CHIA NGAN o giua (muc 119)
            var net9 = new List<NetMau>
            {
                Net("0-KAKOU", false, 0, 0, 200, 0), Net("0-KAKOU", false, 200, 0, 200, 100),
                Net("0-KAKOU", false, 200, 100, 0, 100), Net("0-KAKOU", false, 0, 100, 0, 0),
                Net("0-KAKOU", false, 100, 0, 100, 100),
            };
            var b9 = DatMaKhac.BienTam(net9, hinh);
            Kiem("4 LINE + net chia ngan -> tim duoc bien ngoai", b9.ngoai != null, true);
            Kiem("  bien ngoai khong gom net chia x=100",
                 b9.ngoai != null && !b9.ngoai.Any(d => d.X1 == 100 && d.X2 == 100), true);
            KetQuaTam r9 = DatMaKhac.DatMotTam(net9, hinh, 40, 20, new List<Doan>());
            Kiem("  dat duoc trong ngan trai (khong cat net chia)", r9.Dat && r9.Cx + r9.Hw <= 100, true);

            // 10. Noi chu T: net doc cham GIUA canh tren -> canh tren bi cat lam 2
            var t10 = DatMaKhac.TachT(new List<List<Diem>>
            {
                new List<Diem> { new Diem(0, 100), new Diem(200, 100) },
                new List<Diem> { new Diem(80, 100), new Diem(80, 0) },
            }, 0.5);
            Kiem("tach T: 2 net -> 3 net (canh tren cat tai x=80)", t10.Count, 3);

            // 11. Vong lon nhat lech hop hinh > 3mm -> khong co bien ngoai
            var b11 = DatMaKhac.BienTam(new List<NetMau> { ChuNhat(0, 0, 150, 100) }, hinh);
            Kiem("vong lech hop hinh -> khong co bien ngoai", b11.ngoai == null, true);

            // 12. Doc du lieu lisp
            var ds = new List<DatMaKhac.DongTam>();
            var vc = new List<Hop>();
            DatMaKhac.DocDuLieu(new[] { "T|12|0,0,200,100|10.5,20,50.5,40|2A1|2A2;2A3", "V|1,2,3,4", "rac", "T|1|x|y|z|" },
                                ds, vc, out int loi);
            Kiem("doc du lieu: 1 tam, 1 vat can, 2 dong hong", ds.Count + "/" + vc.Count + "/" + loi, "1/1/2");
            Kiem("  bango / handle bango / so handle cum", ds[0].Tam.Bango + "/" + ds[0].HandleBango + "/" + ds[0].HandleCum.Count, "12/2A1/2");
            Kiem("  hop cum doc dung", S(ds[0].Tam.BbCum.X1) + "," + S(ds[0].Tam.BbCum.Y2), "10.50,40.00");

            // 13. Co XData + noi danh sach
            Kiem("them co vao flags rong", DatMaKhac.ThemCo("", "MK_THU_NHO"), "MK_THU_NHO");
            Kiem("them co noi bang ;", DatMaKhac.ThemCo("A", "MK_THU_NHO"), "A;MK_THU_NHO");
            Kiem("co da co -> giu nguyen", DatMaKhac.ThemCo("MK_THU_NHO", "MK_THU_NHO"), "MK_THU_NHO");
            Kiem("noi ds toi da 2 + ...", DatMaKhac.NoiDs(new List<string> { "1", "2", "3" }, 2), "1, 2, ...");
        }
    }
}
