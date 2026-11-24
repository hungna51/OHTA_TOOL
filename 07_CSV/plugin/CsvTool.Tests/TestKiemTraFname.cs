using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;
using OhtaTool.Plugin.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        private static void TestKiemTraFname()
        {
            Console.WriteLine("[KiemTraFname]");
            var cat = FnameCatalogData.Parse(new[] { "M,Loai,Z,T_list", "SS400,SS,SS-PL,3.2 6.0", "SPHC,SS,,2.3" });
            // bảng TraV giả: SS400 6.0 góc 90 có V6 (denta 1.2) và V8 (1.5)
            List<TraVCandidate> TraV(string m, string t, double goc) => m == "SS400" && t == "6.0" && goc == 90
                ? new List<TraVCandidate> { new TraVCandidate { FormattedText = "V=6-1.2(90°)", DieLabel = "V6" }, new TraVCandidate { FormattedText = "V=8-1.5(90°)", DieLabel = "V8" } }
                : new List<TraVCandidate>();
            List<GoiYFname> Chay(params string[] dong) => KiemTraFname.Chay(dong, cat, TraV);

            Kiem("Fname: dung het -> khong goi y", Chay("N=A", "M=SS400", "T=6.0", "Q=1", "V=6-1.2(90°)", "Z=SS-PL").Count, 0);
            var z = Chay("M=SS400", "T=6.0", "Z=SPCC");
            Kiem("Fname: Z lech -> sua theo bang", z.Count + "|" + z[0].DongCu + "|" + z[0].DongMoi, "1|Z=SPCC|Z=SS-PL");
            Kiem("Fname: thieu Z -> chi bao", Chay("M=SS400", "T=6.0")[0].DongMoi, null);
            Kiem("Fname: mac khong co Z -> khong bat Z", Chay("M=SPHC", "T=2.3").Count, 0);
            var m = Chay("M=SS41", "T=6.0", "Z=X");
            Kiem("Fname: vat lieu chua co -> bo sung, dung kiem tiep", m.Count + "|" + m[0].BoSung + "|" + m[0].M + "|" + m[0].T, "1|True|SS41|6.0");
            var t = Chay("M=SS400", "T=9.0", "Z=SS-PL", "V=6-1.2(90°)");
            Kiem("Fname: do day chua co -> bo sung, bo qua V", t.Count + "|" + t[0].BoSung, "1|True");
            var d = Chay("M=SS400", "T=6.0", "Z=SS-PL", "V=6-1.0(90°)");
            Kiem("Fname: denta lech -> sua theo dao cu", d.Count + "|" + d[0].DongMoi, "1|V=6-1.2(90°)");
            var dao = Chay("M=SS400", "T=6.0", "Z=SS-PL", "V=10-1.2(90°)");
            Kiem("Fname: dao khong co -> chi bao, liet ke dao", dao[0].DongMoi == null && dao[0].NoiDung.Contains("V6, V8"), true);
            Kiem("Fname: khong tra duoc goc", Chay("M=SS400", "T=6.0", "Z=SS-PL", "V=6-1.2(45°)")[0].NoiDung.Contains("không tra được"), true);
            List<TraVCandidate> NoiSuy(string a, string b, double c) => new List<TraVCandidate> { new TraVCandidate { FormattedText = "V=6-1.3(90°)", DieLabel = "V6", Source = TraVThuRecord.NoiSuy } };
            Kiem("Fname: so noi suy khong sua 1 cham", KiemTraFname.Chay(new[] { "M=SS400", "T=6.0", "Z=SS-PL", "V=6-1.2(90°)" }, cat, NoiSuy)[0].DongMoi, null);
            Kiem("Fname: dong SIMA doc duoc", KiemTraFname.DocV("シマ目外 V6引代 1.2(90°)", out string daoS, out double dS, out string gS) + "|" + daoS + "|" + dS + "|" + gS, "True|6|1.2|90");
            Kiem("Fname: chua co du lieu -> bo qua", KiemTraFname.Chay(new[] { "M=SS41" }, new FnameCatalogData(), TraV).Count, 0);

            string mt = "{\\fArial|b0;N=A\\PM=SS400\\PV=6-1.0(90°)\\PZ=SPCC}";
            Kiem("ThayDongFname: MTEXT dong giua", PhanTichKhung.ThayDongFname(mt, true, x => x == "V=6-1.0(90°)", "V=6-1.2(90°)"), "{\\fArial|b0;N=A\\PM=SS400\\PV=6-1.2(90°)\\PZ=SPCC}");
            Kiem("ThayDongFname: MTEXT dong cuoi giu }", PhanTichKhung.ThayDongFname(mt, true, x => x == "Z=SPCC", "Z=SS-PL"), "{\\fArial|b0;N=A\\PM=SS400\\PV=6-1.0(90°)\\PZ=SS-PL}");
            Kiem("ThayDongFname: khong co dong -> null", PhanTichKhung.ThayDongFname(mt, true, x => x == "Z=X", "Z=Y"), null);
            Kiem("ThayDongFname: TEXT", PhanTichKhung.ThayDongFname("Z=SPCC", false, x => x == "Z=SPCC", "Z=SS-PL"), "Z=SS-PL");
        }
    }
}
