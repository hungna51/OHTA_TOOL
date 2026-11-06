using System;
using System.Linq;
using AiTool.Core;
using OhtaTool.Plugin.Core;

namespace AiTool.Tests
{
    internal static partial class Program
    {
        private static void TestMoTaKhung()
        {
            Console.WriteLine("=== MoTaKhung ===");
            var k = KhungMau(
                Chu("2-F-NAME", 0, 50, "N=A-1"), Chu("2-F-NAME", 0, 45, "M=SS400"), Chu("2-F-NAME", 0, 40, "T=3.2"),
                Chu("2-F-NAME", 0, 35, "Q=2"), Chu("2-F-NAME", 0, 30, "Z=X"),
                Chu("1-SUNPOU", 100, 100, "曲げ"),
                Line("0-KAKOU", 10, 10, 50, 10), Line("0-KAKOU", 50, 10, 50, 40),
                new DoiTuong { Loai = LoaiDoiTuong.Circle, Layer = "0-KAKOU", Cx = 20, Cy = 20, R = 5, MinX = 15, MinY = 15, MaxX = 25, MaxY = 25 });

            string[] md = Dong(MoTaKhung.Md("BV-01", new[] { k }));
            Kiem("MD tieu de ten ban ve", md[0], "# BV-01");
            Kiem("MD tieu de tam", md.Contains("## Tam 1 — N=A-1 (khung 1)"), true);
            Kiem("MD kich thuoc khung", md.Contains("- Kich thuoc khung: 420 x 297"), true);
            Kiem("MD F-name tach", md.Contains("- F-name: N=A-1; M=SS400; T=3.2; Q=2; Z=X; BE="), true);
            Kiem("MD ghi chu (khong lap F-name)", md.Contains("- [1-SUNPOU] 曲げ") && !md.Any(l => l.Contains("[2-F-NAME]")), true);
            Kiem("MD dem theo layer", md.Contains("- 0-KAKOU: 2 Line, 1 Circle"), true);
            Kiem("MD khong co toa do", md.Any(l => l.Contains("10,10")), false);

            var j = JsonLite.Parse(MoTaKhung.Json("BV-01", new[] { k })) as JsonObject;
            var tam = (JsonObject)((System.Collections.Generic.List<object>)j.Get("tam"))[0];
            Kiem("JSON ban_ve", j.GetString("ban_ve"), "BV-01");
            Kiem("JSON fname.T", ((JsonObject)tam.Get("fname")).GetString("T"), "3.2");
            Kiem("JSON doi_tuong du 9", ((System.Collections.Generic.List<object>)tam.Get("doi_tuong")).Count, 9);
            Kiem("JSON chi_tiet co toa do", MoTaKhung.Json("BV-01", new[] { k }).Contains("L 0-KAKOU 10,10 50,10"), true);

            string[] trong = Dong(MoTaKhung.Md("X", new[] { KhungMau() }));
            Kiem("MD khung rong: khong co F-name", trong.Contains("- F-name: (khong co)"), true);
        }
    }
}
