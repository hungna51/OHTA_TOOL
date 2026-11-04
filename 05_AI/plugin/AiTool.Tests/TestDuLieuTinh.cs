using System;
using System.Collections.Generic;
using System.IO;
using AiTool.Core;
using OhtaTool.Plugin.Core;

namespace AiTool.Tests
{
    internal static partial class Program
    {
        private static void TestDuLieuTinh()
        {
            Console.WriteLine("=== DuLieuTinh ===");
            string[] sheet = { "M,Loai,Z,T_list", "SS400,SS,SS400,3.2 4.5", "SPCC,SS,SPC,0.8 1.0" };
            string khong = DuLieuTinh.KhoiVatLieu(sheet, new List<FnameLocalAdditions.Entry>());
            Kiem("Khoi vat lieu: co tieu de", khong.StartsWith("## BANG VAT LIEU CHUAN (M,Loai,Z,T_list)\n"), true);
            Kiem("Khoi vat lieu: giu dong sheet", khong.Contains("\nSS400,SS,SS400,3.2 4.5\nSPCC,SS,SPC,0.8 1.0"), true);
            Kiem("Khoi vat lieu: khong cho duyet -> khong co muc CHUA DUYET", khong.Contains("CHUA DUYET"), false);

            var cho = new List<FnameLocalAdditions.Entry>
            {
                new FnameLocalAdditions.Entry { M = "SUS430", Loai = "SUS", Z = "SUS-430", Thicknesses = new List<string> { "1.0", "1.5" } }
            };
            string co = DuLieuTinh.KhoiVatLieu(sheet, cho);
            Kiem("Khoi vat lieu: co muc CHUA DUYET",
                 co.Contains("## VAT LIEU MAY TU THEM - CHUA DUYET (chi bao vang neu M chi co o day)\nSUS430,SUS,SUS-430,1.0 1.5"), true);
            Kiem("Khoi vat lieu: hai lan goi giong het", DuLieuTinh.KhoiVatLieu(sheet, cho) == co, true);

            Kiem("ThuMucFname = <cha>\\01_Fname",
                 DuLieuTinh.ThuMucFname(Path.Combine("C:\\x", "OHTA_TOOL", "05_AI")), Path.Combine("C:\\x", "OHTA_TOOL", "01_Fname"));

            string tam = Path.Combine(Path.GetTempPath(), "aitool_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tam);
            try
            {
                Kiem("Loi dan chua co file -> tra mac dinh", DuLieuTinh.DocLoiDan(tam, "MAC DINH"), "MAC DINH");
                Kiem("Loi dan chua co file -> da ghi file", File.Exists(Path.Combine(tam, "data", "loi_dan.md")), true);
                DuLieuTinh.GhiLoiDan(tam, "moi");
                Kiem("Loi dan ghi roi doc", DuLieuTinh.DocLoiDan(tam, "MAC DINH"), "moi");

                Kiem("Key khong co file -> null", DuLieuTinh.DocKey(tam), null);
                File.WriteAllText(Path.Combine(tam, "data", "ai.key"), "# key cua OHTA\n\n  sk-ant-x  \nrac\n");
                Kiem("Key bo dong chu thich + trim", DuLieuTinh.DocKey(tam), "sk-ant-x");
                File.Delete(Path.Combine(tam, "data", "ai.key"));
                File.WriteAllText(Path.Combine(tam, "data", "ai.key.txt"), "sk-ant-txt\n");
                Kiem("Key luu nham ai.key.txt (Notepad) van doc duoc", DuLieuTinh.DocKey(tam), "sk-ant-txt");
            }
            finally
            {
                Directory.Delete(tam, true);
            }
            Console.WriteLine();
        }
    }
}
