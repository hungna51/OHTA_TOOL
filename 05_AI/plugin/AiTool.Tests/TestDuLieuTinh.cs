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
                Kiem("File huong dan AI = data\\huong_dan_ai.md", DuLieuTinh.FileHuongDan(tam), Path.Combine(tam, "data", "huong_dan_ai.md"));
                Kiem("Huong dan chua co file -> tra mac dinh", DuLieuTinh.DocHuongDan(tam, "MAC DINH"), "MAC DINH");
                Kiem("Huong dan chua co file -> da ghi file", File.Exists(DuLieuTinh.FileHuongDan(tam)), true);
                DuLieuTinh.GhiHuongDan(tam, "moi");
                Kiem("Huong dan ghi roi doc", DuLieuTinh.DocHuongDan(tam, "MAC DINH"), "moi");

                DuLieuTinh.GhiKey(tam, "  sk-ant-api03-abcdefgh1234  ");
                Kiem("GhiKey: trim, doc lai duoc", DuLieuTinh.DocKey(tam), "sk-ant-api03-abcdefgh1234");
                Kiem("AnKey: chi lo dau va 4 ky tu cuoi", DuLieuTinh.AnKey("sk-ant-api03-abcdefgh1234"), "sk-ant-…1234");
                Kiem("AnKey: null -> null", DuLieuTinh.AnKey(null), null);
                Kiem("AnKey: key ngan -> chi dau sao", DuLieuTinh.AnKey("abc"), "…");
                File.WriteAllText(Path.Combine(tam, "data", "ai.key.txt"), "cu\n");
                DuLieuTinh.XoaKey(tam);
                Kiem("XoaKey: xoa ca ai.key va ai.key.txt", DuLieuTinh.DocKey(tam), null);

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
