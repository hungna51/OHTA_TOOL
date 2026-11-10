using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Tests
{
    internal static partial class Program
    {
        private static int _passed;
        private static int _failed;

        // Test doc CSV mau trong 01_Fname/data, khong dung cache Google Sheet cua may
        private static readonly string FnameDir = Path.Combine(ModuleDiscovery.ResolveRootDirectory(AppContext.BaseDirectory) ?? "", "01_Fname");
        private static readonly string NoCache = Path.Combine(Path.GetTempPath(), "ohta_test_no_cache_" + Guid.NewGuid().ToString("N"));

        private static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== OHTA TOOL CORE UNIT TESTS ===");
            TraVCalculator.Load(FnameDir, NoCache);
            TestFnameDataCsv();

            TestIgnoreRules();
            TestActualOhtaToolWorkspace();
            TestDynamicSubfolderWithoutManifestAndOrdering();
            TestResolveRootFromCompiledDllLocation();
            TestMacroBuilderPathsWithSpaces();
            TestFnameCatalogAndMaterialMapping();
            TestFnameUpdateRequests();
            TestFnameParserMultiFormatAndExtraPreservation();
            TestFnameIncrementPartName();
            TestFnameComputeDraggedTextHeight();
            TestTraVCalculations();
            TestTrialBendLookup();
            TestTrialDieParams();
            TestTamTableAndConfig();
            TestTamBoSung();
            TestPressOneTimeFormatAndParsing();
            TestFnameRecordBendAndPressOrdering();
            TestFnameParserWithBendAndPress();
            TestTabEditFeatures();
            TestUpdateFeatures();
            TestInfoTabFeatures();
            TestPrintCore();
            TestPrintSettings();
            TestPdfMerge();
            TestBugReport();

            Console.WriteLine($"=== KET QUA: {_passed} PASS, {_failed} FAIL ===");
            return _failed == 0 ? 0 : 1;
        }

        private static void Check(string name, bool condition, string detail = "")
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine($"  [PASS] {name}");
            }
            else
            {
                _failed++;
                Console.WriteLine($"  [FAIL] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" -> {detail}"));
            }
        }

        private static void TestIgnoreRules()
        {
            Check("Bo qua thu muc 'rules'", ModuleDiscovery.ShouldIgnoreFolder("rules"));
            Check("Bo qua thu muc 'RULES' (hoa thuong)", ModuleDiscovery.ShouldIgnoreFolder("RULES"));
            Check("Bo qua thu muc '_system'", ModuleDiscovery.ShouldIgnoreFolder("_system"));
            Check("Bo qua thu muc '_Template_Module'", ModuleDiscovery.ShouldIgnoreFolder("_Template_Module"));
            Check("Bo qua thu muc '.git'", ModuleDiscovery.ShouldIgnoreFolder(".git"));
            Check("Nhan thu muc '01_Fname'", !ModuleDiscovery.ShouldIgnoreFolder("01_Fname"));
            Check("Nhan thu muc '03_TrienKhai_Phoi'", !ModuleDiscovery.ShouldIgnoreFolder("03_TrienKhai_Phoi"));
        }

        private static void TestActualOhtaToolWorkspace()
        {
            string baseDir = AppContext.BaseDirectory;
            string rootDir = ModuleDiscovery.ResolveRootDirectory(baseDir);
            Check("Tim thay thu muc goc OHTA_TOOL thuc te", !string.IsNullOrEmpty(rootDir) && Directory.Exists(rootDir), rootDir);
            if (string.IsNullOrEmpty(rootDir))
                return;

            List<ModuleManifest> modules = ModuleDiscovery.DiscoverModules(rootDir);
            Check("Quet duoc it nhat 2 muc con ban dau", modules.Count >= 2, $"Count={modules.Count}");
            if (modules.Count >= 2)
            {
                Check("Tab 1 la 01_Fname", modules[0].FolderName == "01_Fname", modules[0].FolderName);
                Check("Tab 1 co id = fname va tabTitle = Fname",
                    modules[0].Id == "fname" && modules[0].TabTitle == "Fname",
                    $"{modules[0].Id} / {modules[0].TabTitle}");
                Check("Tab 1 co README.md va rules/ rieng",
                    File.Exists(modules[0].ReadmePath) && Directory.Exists(modules[0].RulesDirectoryPath));

                FnameCatalogData catalog = FnameCatalogData.LoadFromModuleDirectory(modules[0].DirectoryPath, NoCache);
                Check("Tab 1 doc duoc data/catalog.csv du 13 vat lieu chuan",
                    catalog.Materials.Count == 13, $"Count={catalog.Materials.Count}");

                Check("Tab 2 la 03_Guro", modules[1].FolderName == "03_Guro", modules[1].FolderName);
                Check("Tab 2 co README.md va rules/ rieng",
                    File.Exists(modules[1].ReadmePath) && Directory.Exists(modules[1].RulesDirectoryPath));
                Check("Tab cuoi la More (More, order 90)",
                    modules[modules.Count - 1].FolderName == "More", modules[modules.Count - 1].FolderName);
                Check("Khong co tab tu thu muc docs / graphify-out",
                    !modules.Exists(m => m.FolderName == "docs" || m.FolderName == "graphify-out"));

                List<ModuleManifest> nonAdminModules = ModuleDiscovery.DiscoverModules(rootDir, includeAdminOnly: false);
                Check("Tab 05_AI co adminOnly=true -> chi hien tren may Admin, an tren may thuong",
                    modules.Exists(m => m.FolderName == "05_AI" && m.IsAdminOnly) &&
                    !nonAdminModules.Exists(m => m.FolderName == "05_AI"));
            }
        }

        private static void TestDynamicSubfolderWithoutManifestAndOrdering()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "OhtaToolTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempRoot);
                File.WriteAllText(Path.Combine(tempRoot, "GEMINI.md"), "# Root", Encoding.UTF8);
                Directory.CreateDirectory(Path.Combine(tempRoot, "rules"));
                Directory.CreateDirectory(Path.Combine(tempRoot, "_system"));
                Directory.CreateDirectory(Path.Combine(tempRoot, "_Template_Module"));

                string modNoJson = Path.Combine(tempRoot, "03_KiemTra_BienDang");
                Directory.CreateDirectory(Path.Combine(modNoJson, "rules"));
                File.WriteAllText(Path.Combine(modNoJson, "README.md"), "# 03. Kiểm Tra Biên Dạng (`03_KiemTra_BienDang`)\nNội dung.", Encoding.UTF8);

                string modBom = Path.Combine(tempRoot, "02_Uu_Tien");
                Directory.CreateDirectory(modBom);
                string jsonBom = "{\"id\":\"m_bom\",\"order\":5,\"tabTitle\":\"02. Ưu Tiên\",\"tools\":[{\"name\":\"Tạo Khung\",\"command\":\"TK\"}]}";
                File.WriteAllText(Path.Combine(modBom, "module.json"), jsonBom, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                string modDisabled = Path.Combine(tempRoot, "04_Tam_An");
                Directory.CreateDirectory(modDisabled);
                File.WriteAllText(Path.Combine(modDisabled, "module.json"), "{\"enabled\":false,\"tabTitle\":\"Ẩn\"}", Encoding.UTF8);

                List<ModuleManifest> list = ModuleDiscovery.DiscoverModules(tempRoot);
                Check("Sandbox chi sinh dung 2 Tab (bo qua rules, _*, enabled=false)", list.Count == 2, $"Count={list.Count}");
                if (list.Count == 2)
                {
                    Check("Sap xep order=5 len dau va doc dung UTF-8 BOM", list[0].TabTitle == "02. Ưu Tiên", list[0].TabTitle);
                    Check("Tu sinh Badge khi khong khai bao", list[0].Tools[0].Badge == "TK", list[0].Tools[0].Badge);
                    Check("Muc con chua co module.json van tu sinh Tab", list[1].FolderName == "03_KiemTra_BienDang" && !list[1].HasManifestFile);
                    Check("Format TabTitle tu ten thu muc", list[1].TabTitle == "03. KiemTra BienDang", list[1].TabTitle);
                    Check("Lay Title tu tieu de # cua README.md", list[1].Title == "03. Kiểm Tra Biên Dạng", list[1].Title);
                }
            }
            finally
            {
                try { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true); } catch { }
            }
        }

        private static void TestResolveRootFromCompiledDllLocation()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "OhtaRootResolve_" + Guid.NewGuid().ToString("N"));
            try
            {
                string binZw = Path.Combine(tempRoot, "_system", "bin", "ZwCAD");
                string binAc = Path.Combine(tempRoot, "_system", "bin", "AutoCAD");
                Directory.CreateDirectory(binZw);
                Directory.CreateDirectory(binAc);
                File.WriteAllText(Path.Combine(tempRoot, "OHTA_Loader.lsp"), "(princ)", Encoding.ASCII);

                string fakeZwDll = Path.Combine(binZw, "OHTATOOL.dll");
                string fakeAcDll = Path.Combine(binAc, "OHTATOOL.dll");
                File.WriteAllBytes(fakeZwDll, new byte[] { 0 });
                File.WriteAllBytes(fakeAcDll, new byte[] { 0 });

                Check("Resolve tu _system/bin/ZwCAD/OHTATOOL.dll",
                    string.Equals(ModuleDiscovery.ResolveRootDirectory(fakeZwDll), Path.GetFullPath(tempRoot), StringComparison.OrdinalIgnoreCase));
                Check("Resolve tu _system/bin/AutoCAD/OHTATOOL.dll",
                    string.Equals(ModuleDiscovery.ResolveRootDirectory(fakeAcDll), Path.GetFullPath(tempRoot), StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                try { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true); } catch { }
            }
        }

        private static void TestMacroBuilderPathsWithSpaces()
        {
            string modDir = @"C:\Users\oftby\OneDrive - OFT\laptrinhlisp\OHTA_TOOL\01_Fname";
            var tool = new ToolItem
            {
                Name = "Chon Fname",
                Command = "OHTAFNSEL",
                LispFile = @"lisp\ohta_fname_tools.lsp"
            };

            string macro = MacroBuilder.BuildToolMacro(modDir, tool);
            Check("Macro bat dau bang \\x03\\x03 huy lenh cu", macro.StartsWith("\x03\x03", StringComparison.Ordinal));
            Check("Macro chuyen dau \\ sang / cho duong dan co khoang trang",
                macro.Contains("\"C:/Users/oftby/OneDrive - OFT/laptrinhlisp/OHTA_TOOL/01_Fname/lisp/ohta_fname_tools.lsp\"", StringComparison.Ordinal),
                macro);
            Check("Macro ket thuc bang lenh OHTAFNSEL va xuong dong", macro.EndsWith(" OHTAFNSEL\n", StringComparison.Ordinal), macro);
        }

        private static void TestFnameDataCsv()
        {
            string[] t90 = FnameDataStore.ReadLines(FnameDir, "trav_90", NoCache);
            Check("CSV mau trav_90 doc du 155 dong du lieu", TraVCalculator.ParseTable90(t90).Count == 155, $"Count={TraVCalculator.ParseTable90(t90).Count}");
            foreach (string table in FnameDataStore.TableNames)
                Check($"CSV mau '{table}' qua kiem tra hop le", FnameDataStore.Validate(table, FnameDataStore.ReadLines(FnameDir, table, NoCache), out string err), err);

            Check("Tu choi trang HTML (Sheet chua chia se)", !FnameDataStore.Validate("trav_90", new[] { "<!DOCTYPE html><html>" }, out _));
            Check("Tu choi CSV co dau ngoac kep (dau phay thap phan)", !FnameDataStore.Validate("trav_90", new[] { "SS,\"3,2\",3,16,2.6,2.6,2" }, out _));
            Check("Tu choi bang rong / chi co tieu de", !FnameDataStore.Validate("catalog", new[] { "M,Z,T_list" }, out _));

            // Cache dang bi mo (kieu Excel): van doc duoc; khoa cung thi quay ve CSV mau.
            string cache = Path.Combine(Path.GetTempPath(), "ohta_test_cache_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cache);
            string file = Path.Combine(cache, "catalog.csv");
            File.WriteAllText(file, "M,Z,T_list\nXX,XX,1.0\n");
            try
            {
                using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    Check("Doc cache khi file dang mo (chia se ghi)", FnameDataStore.ReadLines(FnameDir, "catalog", cache).Length == 2);
                using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    Check("Cache bi khoa cung -> dung CSV mau", FnameDataStore.ReadLines(FnameDir, "catalog", cache).Length == 14);
            }
            finally { Directory.Delete(cache, true); }
        }

        private static void TestFnameUpdateRequests()
        {
            string ps = FnameUpdateRequest.BuildCatalogParams("SPHC", "SS", "", new[] { "2.3", "4.5" });
            Check("Thong_so catalog dung dinh dang key=value | ...", ps == "M=SPHC | Loai=SS | Z= | T=2.3 4.5", ps);
            var back = FnameUpdateRequest.ParseParams(ps);
            Check("ParseParams khu hoi du 4 khoa, Z rong",
                back.Count == 4 && back["M"] == "SPHC" && back["loai"] == "SS" && back["Z"] == "" && back["T"] == "2.3 4.5");
            string bad = FnameUpdateRequest.BuildCatalogParams("A|B\nC", "SS", "", new[] { "1.0" });
            Check("Gia tri co dau | va xuong dong bi thay bang dau cach",
                FnameUpdateRequest.ParseParams(bad)["M"] == "A B C" && FnameUpdateRequest.ParseParams(bad).Count == 4, bad);

            var req = new FnameUpdateRequest { Loai = "catalog", ThongSo = ps, MayGui = "PC-01", Id = "x1" };
            Check("Describe catalog", req.Describe() == "Vật liệu SPHC (SS) — T: 2.3 4.5 — Z: giữ nguyên / trống", req.Describe());

            var admin = FnameSheetReply.Parse("{\"ok\":true,\"isAdmin\":true,\"requests\":[{\"loai\":\"catalog\",\"thongSo\":\"M=SPHC | Loai=SS | Z=SPC | T=2.3\",\"mayGui\":\"PC-01\",\"thoiGian\":\"2026-11-02 10:00\",\"id\":\"abc\"}]}");
            Check("Parse list_requests (admin, 1 yeu cau)",
                admin != null && admin.Ok && admin.IsAdmin && admin.Requests.Count == 1 && admin.Requests[0].Id == "abc" && admin.Requests[0].Get("Z") == "SPC");
            var member = FnameSheetReply.Parse("{\"ok\":true,\"isAdmin\":false,\"requests\":[]}");
            Check("Parse list_requests (khong phai admin)", member != null && !member.IsAdmin && member.Requests.Count == 0);
            var plain = FnameSheetReply.Parse("{\"ok\":false,\"message\":\"Sai write_key\"}");
            Check("Parse tra loi khong co requests -> danh sach rong", plain != null && !plain.Ok && plain.Requests.Count == 0 && plain.Message == "Sai write_key");
            // Bo sung cuc bo (catalog_local.csv): dung ngay tren may gui, tu xoa khi Sheet da co
            string cache = Path.Combine(Path.GetTempPath(), "ohta_local_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cache);
            try
            {
                File.WriteAllText(Path.Combine(cache, "catalog.csv"), "M,Loai,Z,T_list\nSPHC,SS,SPC,1.6 2.3\n");
                FnameLocalAdditions.Add(cache, "sphc", "SS", "", new List<string> { "4.5" });
                FnameLocalAdditions.Add(cache, "SUS430", "SUS", "SUS-N2", new List<string> { "1.0" });
                FnameLocalAdditions.Add(cache, "SPHC", "SS", "", new List<string> { "6.0", "4.5" });
                var entries = FnameLocalAdditions.Load(cache);
                Check("Them bo sung: trung M thi gop do day", entries.Count == 2 && string.Join(" ", entries[0].Thicknesses) == "4.5 6.0",
                    string.Join(" | ", entries.Select(x => x.M + ":" + string.Join(" ", x.Thicknesses))));

                var merged = FnameCatalogData.LoadFromModuleDirectory(FnameDir, cache);
                Check("Catalog doc ra da cong bo sung (SPHC co 4.5 6.0, giu Z=SPC; co SUS430)",
                    string.Join(" ", merged.FindMaterial("SPHC").Thicknesses) == "1.6 2.3 4.5 6.0" && merged.FindMaterial("SPHC").Z == "SPC"
                    && merged.FindMaterial("SUS430")?.Z == "SUS-N2");

                // Sheet moi chi co SPHC du do day -> chi bo dong SPHC
                var sheet = FnameCatalogData.Parse(new[] { "M,Loai,Z,T_list", "SPHC,SS,SPC,1.6 2.3 4.5 6.0" });
                Check("Prune bo dong Sheet da co du, giu dong chua duyet",
                    FnameLocalAdditions.Prune(cache, sheet) == 1 && FnameLocalAdditions.Load(cache).Single().M == "SUS430");

                // SUS430 van con trong hang cho -> giu; mat mang (null) -> giu; khong con trong hang cho (bi tu choi) -> bo
                Check("Prune: yeu cau van cho duyet -> giu",
                    FnameLocalAdditions.Prune(cache, sheet, new List<string> { "sus430" }) == 0 && FnameLocalAdditions.Load(cache).Count == 1);
                Check("Prune: khong doc duoc hang cho (null) -> giu",
                    FnameLocalAdditions.Prune(cache, sheet, null) == 0 && FnameLocalAdditions.Load(cache).Count == 1);
                Check("Prune: bi tu choi (khong con trong hang cho) -> bo, quay ve du lieu Sheet",
                    FnameLocalAdditions.Prune(cache, sheet, new List<string>()) == 1 && FnameLocalAdditions.Load(cache).Count == 0
                    && FnameCatalogData.LoadFromModuleDirectory(FnameDir, cache).FindMaterial("SUS430") == null);
            }
            finally { Directory.Delete(cache, true); }

            Check("Parse HTML / rong / JSON hong -> null",
                FnameSheetReply.Parse("<html>") == null && FnameSheetReply.Parse("") == null && FnameSheetReply.Parse("{\"ok\":") == null);
        }

        private static void TestFnameCatalogAndMaterialMapping()
        {
            FnameCatalogData cat = FnameCatalogData.LoadFromModuleDirectory(FnameDir, NoCache);
            FnameMaterialSpec ss400 = cat.FindMaterial("SS400");
            FnameMaterialSpec sphc = cat.FindMaterial("sphc");
            FnameMaterialSpec shima = cat.FindMaterial("ムキ 縞板");
            FnameMaterialSpec sus = cat.FindMaterial("SUS304 2B");

            Check("SS400 -> Z=SS400 & chua T=3.2, 9.0", ss400 != null && ss400.Z == "SS400" && ss400.Thicknesses.Contains("3.2") && ss400.Thicknesses.Contains("9.0"));
            Check("SPHC -> Z=SPC & chua T=1.6, 2.3, 3.2", sphc != null && sphc.Z == "SPC" && sphc.Thicknesses.Count == 3);
            Check("MUKI SHIMAITA -> Z=SS400-SIMA", shima != null && shima.Z == "SS400-SIMA");
            Check("SUS304 2B -> Z=SUS-N2", sus != null && sus.Z == "SUS-N2");

            // Cot Loai (loai vat lieu)
            Check("Loai: SUS304 No.1 -> SUS", cat.FindMaterial("SUS304 No.1")?.Loai == "SUS");
            Check("Loai: SS400 -> SS", ss400?.Loai == "SS");
            Check("Loai: SPHC -> SS", sphc?.Loai == "SS");
            Check("Moi vat lieu trong catalog deu co Loai", cat.Materials.TrueForAll(x => !string.IsNullOrEmpty(x.Loai)));

            var legacy = FnameCatalogData.Parse(new[] { "M,Z,T_list", "SPHC,SPC,1.6 2.3" });
            Check("CSV cu (khong co cot Loai) van doc dung M/Z/T",
                legacy.Materials.Count == 1 && legacy.Materials[0].Z == "SPC" && legacy.Materials[0].Thicknesses.Count == 2 && legacy.Materials[0].Loai == "");
            var reordered = FnameCatalogData.Parse(new[] { "Loai,M,T_list,Z", "SUS,ABC,1.0,SUS-N2" });
            Check("Cot doc theo ten tieu de (thu tu tuy y)",
                reordered.Materials.Count == 1 && reordered.Materials[0].M == "ABC" && reordered.Materials[0].Loai == "SUS" && reordered.Materials[0].Z == "SUS-N2");

            TraVCalculator.SetCatalog(FnameCatalogData.Parse(new[] { "M,Loai,Z,T_list", "XYZ-1,SUS,SUS-N2,2.0", "XYZ 縞板,SUS,SUS-N2,3.0" }));
            Check("TraV dung cot Loai: mac la XYZ-1 (Loai=SUS) tra bang SUS",
                string.Join("|", TraVCalculator.GetTraVMaterialKeys("XYZ-1")) == "SUS");
            Check("TraV dung cot Loai + 縞板 -> SUS SIMA Trong/Ngoai",
                string.Join("|", TraVCalculator.GetTraVMaterialKeys("XYZ 縞板")) == "SUS SIMA Trong|SUS SIMA Ngoài");
            Check("Mac khong co trong catalog -> doan theo ten (SUS304 -> SUS)",
                string.Join("|", TraVCalculator.GetTraVMaterialKeys("SUS304 abc")) == "SUS");
            TraVCalculator.SetCatalog(cat);
            Check("T thuc SUS304 No.1 T=3.0 qua Loai", TraVCalculator.GetRealThickness("SUS304 No.1", "3.0") != "");
            TraVCalculator.SetCatalog(null);

            // Ghi nguoc vat lieu (them / cap nhat catalog)
            Check("Nhap do day '2.3 6; 3.2,1' -> 2.3 6.0 3.2 1.0",
                FnameCatalogData.TryParseThicknessInput("2.3 6; 3.2,1", out var tin, out _) && string.Join(" ", tin) == "2.3 6.0 3.2 1.0",
                string.Join(" ", tin));
            Check("Nhap do day so toan goc '２．３' -> 2.3",
                FnameCatalogData.TryParseThicknessInput("２．３", out var tfw, out _) && string.Join(" ", tfw) == "2.3");
            Check("Nhap do day sai 'abc' bi chan", !FnameCatalogData.TryParseThicknessInput("3.2 abc", out _, out string terr) && terr.Contains("abc"));
            Check("Nhap do day rong bi chan", !FnameCatalogData.TryParseThicknessInput("  ", out _, out _));

            string[] baseCsv = { "M,Loai,Z,T_list", "SPHC,SS,SPC,1.6 2.3 3.2" };
            var up1 = FnameCatalogData.Parse(FnameCatalogData.Upsert(baseCsv, "sphc", "SS", "", new[] { "4.5", "2.3" }));
            var sphcUp = up1.FindMaterial("SPHC");
            Check("Upsert vat lieu co san: gop do day, bo trung, xep tang, giu Z cu khi Z trong",
                up1.Materials.Count == 1 && string.Join(" ", sphcUp.Thicknesses) == "1.6 2.3 3.2 4.5" && sphcUp.Z == "SPC" && sphcUp.M == "SPHC",
                string.Join(" ", sphcUp?.Thicknesses ?? new List<string>()));
            var up2 = FnameCatalogData.Parse(FnameCatalogData.Upsert(baseCsv, "SUS430 2B", "SUS", "", new[] { "1.0", "0.8" }));
            var sus430 = up2.FindMaterial("SUS430 2B");
            Check("Upsert vat lieu moi: them dong, Z de trong, do day xep tang",
                up2.Materials.Count == 2 && sus430 != null && sus430.Loai == "SUS" && sus430.Z == "" && string.Join(" ", sus430.Thicknesses) == "0.8 1.0");
            var up3 = FnameCatalogData.Parse(FnameCatalogData.Upsert(new[] { "M,Z,T_list", "SS400,SS400,3.2" }, "SS400", "SS", "SS400X", new[] { "3.2" }));
            Check("Upsert vao CSV cu chua co cot Loai: tu them cot Loai, cap nhat Z",
                up3.FindMaterial("SS400")?.Loai == "SS" && up3.FindMaterial("SS400")?.Z == "SS400X");
            Check("Upsert bo dau phay trong ten M (giu CSV dung cot)",
                FnameCatalogData.Parse(FnameCatalogData.Upsert(baseCsv, "A,B", "SS", "", new[] { "1.0" })).FindMaterial("A B") != null);

            var rec = new FnameRecord { N = "47079-1", M = "SS400", T = "6.0", Q = "4", Z = "SS400" };
            Check("Validate Fname hop le tra true", rec.Validate(out string w1) && string.IsNullOrEmpty(w1));

            var invalidRec = new FnameRecord { N = "47079-1", M = "SS400", T = "6.0", Q = "2.5", Z = "SS400" };
            Check("Validate Q le (2.5) bi chan", !invalidRec.Validate(out string w2) && w2.Contains("Q="));

            // Kiem tra loc ung vien M= va T= khi go phim
            var susMatches = cat.GetMatchingMaterials("sus");
            Check("Loc ung vien M='sus' tra ra cac mac SUS304",
                susMatches.Count >= 6 && susMatches.All(m => m.StartsWith("SUS", StringComparison.OrdinalIgnoreCase)),
                string.Join(", ", susMatches));

            var simaMatches = cat.GetMatchingMaterials("sima");
            Check("Loc ung vien M='sima' nhan dien duoc 3 mac 縞板",
                simaMatches.Count == 3 && simaMatches.Contains("SS400 縞板") && simaMatches.Contains("ムキ 縞板") && simaMatches.Contains("SUS304 縞板"),
                string.Join(", ", simaMatches));

            var tMatchesSS = cat.GetMatchingThicknesses("SS400", "1");
            Check("Loc ung vien T='1' cua SS400 tra ra 12.0, 14.0, 16.0, 19.0",
                string.Join(" ", tMatchesSS) == "12.0 14.0 16.0 19.0",
                string.Join(" ", tMatchesSS));

            var tMatchesSus = cat.GetMatchingThicknesses("SUS304 2B", "2");
            Check("Loc ung vien T='2' cua SUS304 2B uu tien 2.0, 2.5 len dau",
                tMatchesSus.Count >= 4 && tMatchesSus[0] == "2.0" && tMatchesSus[1] == "2.5",
                string.Join(" ", tMatchesSus));

            // Kiem tra ValidateAgainstCatalog chan xuat Fname khi M/T/Z khong co trong du lieu va goi y vao Cai dat
            Check("ValidateAgainstCatalog hop le voi SS400 T=3.2",
                rec.ValidateAgainstCatalog(cat, out string wCatOk, out bool missOk) && !missOk && string.IsNullOrEmpty(wCatOk));

            var recUnknownM = new FnameRecord { N = "47079-1", M = "MAC_LA_999", T = "3.2", Q = "1", Z = "SS400" };
            Check("Chan xuat Fname khi M= khong co trong du lieu + goi y vao Cai dat",
                !recUnknownM.ValidateAgainstCatalog(cat, out string wUnknownM, out bool missM) && missM && wUnknownM.Contains("Cài đặt"),
                wUnknownM);

            var recUnknownT = new FnameRecord { N = "47079-1", M = "SS400", T = "5.0", Q = "1", Z = "SS400" };
            Check("Chan xuat Fname khi T=5.0 khong co trong SS400 + goi y vao Cai dat",
                !recUnknownT.ValidateAgainstCatalog(cat, out string wUnknownT, out bool missT) && missT && wUnknownT.Contains("Cài đặt"),
                wUnknownT);

            var recUnknownZ = new FnameRecord { N = "47079-1", M = "SS400", T = "3.2", Q = "1", Z = "Z_KHONG_CO" };
            Check("Chan xuat Fname khi Z= khong co trong du lieu + goi y vao Cai dat",
                !recUnknownZ.ValidateAgainstCatalog(cat, out string wUnknownZ, out bool missZ) && missZ && wUnknownZ.Contains("Cài đặt"),
                wUnknownZ);

            // Kiem tra 100% vat lieu trong danh muc deu co ma phoi Z de phuc vu khoa Z co dinh
            foreach (var m in cat.Materials)
            {
                Check($"Vat lieu {m.M} co ma phoi Z hop le de khoa co dinh ({m.Z})", !string.IsNullOrWhiteSpace(m.Z));
            }
        }

        private static void TestFnameParserMultiFormatAndExtraPreservation()
        {
            // 1. MTEXT co ma font va dong V= (BendLines) + dong sau Z= (ExtraAfterZ)
            string mtext = "{\\fArial|b0|i0;N=47079-1}\\PM=ムキ 縞板\\PT=4.5\\PQ=2\\Pシマ目内 V35\\PZ=SS400-SIMA\\P裏から見た図";
            FnameRecord r1 = FnameParser.Parse(new[] { mtext });
            Check("Parse MTEXT doc dung N, M, T, Q, Z",
                r1.N == "47079-1" && r1.M == "ムキ 縞板" && r1.T == "4.5" && r1.Q == "2" && r1.Z == "SS400-SIMA");
            Check("Parse MTEXT nhan dien dung dong be BendLines va dong phu sau Z=",
                r1.BendLines.Count == 1 && r1.BendLines[0] == "シマ目内 V35" &&
                r1.ExtraAfterZ.Count == 1 && r1.ExtraAfterZ[0] == "裏から見た図");

            // Khi sua Q=5 va BuildLines -> van giu nguyen dong be va dong phu!
            r1.Q = "5";
            List<string> rebuilt = r1.BuildLines(includePreservedExtras: true);
            Check("BuildLines sau khi sua Q giu nguyen 7 dong va thu tu chuan",
                rebuilt.Count == 7 && rebuilt[3] == "Q=5" && rebuilt[4] == "シマ目内 V35" && rebuilt[5] == "Z=SS400-SIMA" && rebuilt[6] == "裏から見た図");

            // 2. Dang 1 dong ngan cach dau phay
            FnameRecord r2 = FnameParser.Parse(new[] { "N=BRK-02,M=SPHC,T=2.3,Q=8,Z=SPC,V=V16" });
            Check("Parse Fname ngan cach dau phay doc dung 5 truong + V= vao BendLines",
                r2.N == "BRK-02" && r2.M == "SPHC" && r2.T == "2.3" && r2.Q == "8" && r2.Z == "SPC" && r2.BendLines.Contains("V=V16"));
        }

        private static void TestFnameIncrementPartName()
        {
            Check("Increment '47079-1' -> '47079-2'", FnameParser.IncrementPartName("47079-1") == "47079-2");
            Check("Increment '47079-01' -> '47079-02' (giu so 0)", FnameParser.IncrementPartName("47079-01") == "47079-02");
            Check("Increment '47079-09' -> '47079-10'", FnameParser.IncrementPartName("47079-09") == "47079-10");
            Check("Increment 'COVER' -> 'COVER-1'", FnameParser.IncrementPartName("COVER") == "COVER-1");
            Check("N mau tu ban ve 'D:\\x\\47079.dwg' -> '47079-1'", FnameParser.DefaultPartNameFromDrawing(@"D:\x\47079.dwg") == "47079-1");
            Check("N mau bo duoi -s / -A", FnameParser.DefaultPartNameFromDrawing("47079-s.dwg") == "47079-1" && FnameParser.DefaultPartNameFromDrawing("47079-A.dwg") == "47079-1");
            Check("N mau giu -sa (khong phai -s/-a)", FnameParser.DefaultPartNameFromDrawing("47079-sa.dwg") == "47079-sa-1");
            // Kiem tra nhan dien ten ban ve tam (generic drawing name)
            Check("Drawing2.dwg la ten ban ve generic", FnameParser.IsGenericDrawingName("Drawing2.dwg"));
            Check("Drawing1 la ten ban ve generic", FnameParser.IsGenericDrawingName("Drawing1"));
            Check("Drawing 2 la ten ban ve generic", FnameParser.IsGenericDrawingName("Drawing 2"));
            Check("図面1.dwg la ten ban ve generic", FnameParser.IsGenericDrawingName("図面1.dwg"));
            Check("banve1.dwg la ten ban ve generic", FnameParser.IsGenericDrawingName("banve1.dwg"));
            Check("47079.dwg khong phai generic", !FnameParser.IsGenericDrawingName("47079.dwg"));
            Check("D:\\Works\\47079.dwg khong phai generic", !FnameParser.IsGenericDrawingName(@"D:\Works\47079.dwg"));

            // Kiem tra nhan dien ten chi tiet tam (generic part name)
            Check("Drawing2-1 la ten chi tiet generic", FnameParser.IsGenericPartName("Drawing2-1"));
            Check("Drawing1-1 la ten chi tiet generic", FnameParser.IsGenericPartName("Drawing1-1"));
            Check("Drawing-1 la ten chi tiet generic", FnameParser.IsGenericPartName("Drawing-1"));
            Check("47079-1 khong phai generic", !FnameParser.IsGenericPartName("47079-1"));
            Check("COVER-1 khong phai generic", !FnameParser.IsGenericPartName("COVER-1"));
        }

        private static void TestFnameComputeDraggedTextHeight()
        {
            // 5 dong: blockHeightFactor = 4 * (5/3) + 1 = 23/3 = 7.6667 -> deltaY = -153.333 => H = 20.0
            double h20 = FnameParser.ComputeDraggedTextHeight(deltaX: 10.0, deltaY: -153.333, lineCount: 5, maxLineCharLength: 10);
            Check("Keo doc 5 dong 153.33 don vi -> cao chu = 20.0", Math.Abs(h20 - 20.0) < 0.05, $"h20={h20}");

            // Keo ngang manh hon keo doc -> co chu phong theo be rong keo ngang
            double hHoriz = FnameParser.ComputeDraggedTextHeight(deltaX: 144.0, deltaY: -5.0, lineCount: 5, maxLineCharLength: 10);
            Check("Keo ngang 144 don vi (10 ky tu) -> cao chu = 20.0", Math.Abs(hHoriz - 20.0) < 0.05, $"hHoriz={hHoriz}");

            // Keo rat nho -> chan duoi minHeight = 0.5
            double hMin = FnameParser.ComputeDraggedTextHeight(deltaX: 0.1, deltaY: -0.1, lineCount: 5, maxLineCharLength: 8, minHeight: 0.5);
            Check("Keo rat nho -> giu muc toi thieu minHeight = 0.5", Math.Abs(hMin - 0.5) < 0.001, $"hMin={hMin}");
        }

        private static void TestTraVCalculations()
        {
            // 1. SS400 3.2mm tai 90 do
            var cands90 = TraVCalculator.GetBendOptionsForFname("SS400", "3.2", 90.0);
            Check("TraV SS400 T=3.2 tai 90 do tra ra cac lua chon V", cands90.Count >= 2, $"Count={cands90.Count}");
            Check("TraV SS400 T=3.2 goc 90 co V=16-2.6(90°)",
                cands90.Exists(c => c.FormattedText == "V=16-2.6(90°)"),
                string.Join(", ", cands90.ConvertAll(c => c.FormattedText)));

            // 2. SS400 3.2mm tai 135 do (goc tu > 90)
            var cands135 = TraVCalculator.GetBendOptionsForFname("SS400", "3.2", 135.0);
            Check("TraV SS400 T=3.2 tai 135 do tra ra V=16",
                cands135.Exists(c => c.FormattedText.StartsWith("V=16-") && c.FormattedText.EndsWith("(135°)")),
                string.Join(", ", cands135.ConvertAll(c => c.FormattedText)));

            // 3. SS400 3.2mm tai 75 do (goc nhon < 90 dung bang tra noi suy)
            var cands75 = TraVCalculator.GetBendOptionsForFname("SS400", "3.2", 75.0);
            Check("TraV SS400 T=3.2 tai 75 do tra ra U=18",
                cands75.Exists(c => c.FormattedText.StartsWith("U=18-") && c.FormattedText.EndsWith("(75°)")),
                string.Join(", ", cands75.ConvertAll(c => c.FormattedText)));

            // 3b. Thong tin phu: khong lap ten dao V; 90 do co R; goc nhon bo R (giu Rend), co goc nho nhat o cuoi
            var v16 = cands90.Find(c => c.FormattedText == "V=16-2.6(90°)");
            Check("Thong tin phu 90 do khong bat dau bang ten dao V",
                v16 != null && v16.DetailParts.Count > 0 && !v16.DetailParts[0].StartsWith("V") && v16.DetailParts.Exists(p => p.StartsWith("内R ")),
                v16?.DetailInfo);
            var v12 = TraVCalculator.GetBendOptionsForFname("SS400", "1.6", 60.0).Find(c => c.FormattedText.StartsWith("V=12-"));
            Check("Goc nhon V12 T=1.6: co 最長 / Rend, khong co 実内R/内R/外R, khong hien goc nho nhat, khong canh bao",
                v12 != null && v12.DetailParts.Exists(p => p.StartsWith("最長 ")) && v12.DetailParts.Exists(p => p.StartsWith("Rend ")) &&
                !v12.DetailParts.Exists(p => p.StartsWith("実内R ") || p.StartsWith("内R ") || p.StartsWith("外R ") || p.StartsWith("Góc")) &&
                v12.Warning == "",
                v12?.DetailInfo);

            // V40 SS T=4.5 chi co so lieu den 42 do -> goc 35 van ra dong V40, kem canh bao do, thong tin khac van hien
            var v40 = TraVCalculator.GetBendOptionsForFname("SS400", "4.5", 35.0).Find(c => c.DieLabel == "V40");
            Check("Goc 35 < goc nho nhat 42 (V40 T=4.5): van co dong V40 + canh bao 42°",
                v40 != null && v40.Warning.Contains("42°") && v40.DetailParts.Exists(p => p.StartsWith("最長 ")),
                v40 == null ? "khong co V40" : v40.Warning + " | " + v40.DetailInfo);
            var v40ok = TraVCalculator.GetBendOptionsForFname("SS400", "4.5", 42.0).Find(c => c.DieLabel == "V40");
            Check("Goc 42 = goc nho nhat: khong canh bao", v40ok != null && v40ok.Warning == "", v40ok?.Warning);

            // 3c. Goc 88-90 nam tren goc lon nhat bang goc nhon -> noi suy voi goc 90 bang TraV (khong ngoai suy 88/86)
            var v8 = TraVCalculator.GetBendOptionsForFname("SS400", "1.0", 89.0).Find(c => c.DieLabel == "V8");
            Check("Goc 89 SS T=1.0 V8: noi suy 88 (0.92) va 90 (0.9) -> V=8-0.91(89°)",
                v8 != null && v8.FormattedText == "V=8-0.91(89°)", v8?.FormattedText);

            // 4. Vat lieu Sima (縞板) tra ra ca Shima me ngoai va me trong
            var candsSima = TraVCalculator.GetBendOptionsForFname("ムキ 縞板", "3.2", 90.0);
            Check("TraV Sima 3.2 tra ra cac tuy chon ca シマ目外 va シマ目内",
                candsSima.Exists(c => c.FormattedText.Contains("シマ目外")) &&
                candsSima.Exists(c => c.FormattedText.Contains("シマ目内")),
                string.Join(", ", candsSima.ConvertAll(c => c.FormattedText)));

            // 5. T thuc (Do day thuc) tra tu bang TraV OFT
            Check("T thuc SS400 T=3.2 tra ra '3'", TraVCalculator.GetRealThickness("SS400", "3.2") == "3");
            Check("T thuc SS400 T=1.6 tra ra '1.5'", TraVCalculator.GetRealThickness("SS400", "1.6") == "1.5");
            Check("T thuc SUS304 T=2 tra ra '1.9'", TraVCalculator.GetRealThickness("SUS304 2B", "2.0") == "1.9");
            Check("T thuc SIMA 3.2 tra ra '4.6'", TraVCalculator.GetRealThickness("SS400 縞板", "3.2") == "4.6");
            Check("T thuc SS400 T=14.0 (khong co trong bang TraV) tra ra rong ''", TraVCalculator.GetRealThickness("SS400", "14.0") == "");
            Check("T thuc SUS304 2B T=0.1 (khong co trong bang TraV) tra ra rong ''", TraVCalculator.GetRealThickness("SUS304 2B", "0.1") == "");
            Check("T thuc SUS SIMA T=4.0 (dong trong bang khong co T_thuc) tra ra rong ''", TraVCalculator.GetRealThickness("SUS304 縞板", "4.0") == "");
        }

        private static void TestTamBoSung()
        {
            // Ghi_chu nguoi go co dau phay / ngoac kep -> Sheet boc ngoac kep khi xuat CSV
            Check("SplitCsvLine doc o co ngoac kep, dau phay va \"\" ben trong",
                string.Join("|", TamData.SplitCsvLine("SS,\"a, b\",\"x \"\"y\"\"\",")) == "SS|a, b|x \"y\"|");
            string[] sheet =
            {
                "Loai,T,T_hienthi,Nguon,Nguoi_xac_nhan,Ghi_chu,Anh,Ngay",
                "SUS SIMA,6.0,7.3,Bổ sung,Anh Tu,\"do thuoc kep, 3 diem\",https://drive.google.com/file/d/abc/view https://drive.google.com/file/d/def/view,2026-11-05"
            };
            Check("Validate tab tam chap nhan o ngoac kep", FnameDataStore.Validate("tam", sheet, out string verr), verr);
            var rec = TamData.Parse(sheet).Single();
            Check("Parse tam: ghi chu giu dau phay, 2 link anh, IsBoSung",
                rec.GhiChu == "do thuoc kep, 3 diem" && rec.AnhLinks.Count == 2 && rec.IsBoSung && !rec.ChoDuyet, rec.GhiChu);
            Check("Clean bo dau phay / ngoac kep / xuong dong", TamData.Clean("a, \"b\"\nc") == "a  'b' c");

            // Thong_so yeu cau + link anh Apps Script them vao
            var send = new TamRecord { Loai = "SUS SIMA", T = "6.0", THienThi = "7.3", NguoiXacNhan = "Anh Tu", GhiChu = "do | 3 diem" };
            string ps = FnameUpdateRequest.BuildTamParams(send) + " | Anh=https://drive.google.com/file/d/abc/view?usp=drivesdk";
            var req = new FnameUpdateRequest { Loai = FnameUpdateRequest.LoaiTam, ThongSo = ps };
            var back = TamRecord.FromParams(req.Params);
            Check("Thong_so tam khu hoi + link anh co dau =",
                back.Loai == "SUS SIMA" && back.THienThi == "7.3" && back.GhiChu == "do   3 diem" && req.ImageLinks.Count == 1 && req.HasImages
                && req.ImageLinks[0].EndsWith("usp=drivesdk"), ps);
            Check("Describe tam", req.Describe().StartsWith("SUS SIMA T=6.0: T thực = 7.3 — xác nhận: Anh Tu") && req.Describe().EndsWith("1 ảnh"), req.Describe());
            var reply = FnameSheetReply.Parse("{\"ok\":true,\"message\":\"x\",\"anh\":\"https://a https://b\"}");
            Check("Tra loi submit tam co link anh", reply != null && reply.Anh == "https://a https://b");

            // Bo sung cuc bo: dung ngay (cho duyet), Sheet co roi / bi tu choi thi bo
            string cache = Path.Combine(Path.GetTempPath(), "ohta_tam_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cache);
            try
            {
                Check("Truoc khi bo sung: SUS SIMA 6.0 chua co T thuc", TraVCalculator.GetRealThickness("SUS304 縞板", "6.0") == "");
                TamLocalAdditions.Add(cache, new TamRecord { Loai = "SUS SIMA", T = "6.0", THienThi = "7.3", Nguon = TamRecord.NguonBoSung, NguoiXacNhan = "Anh Tu", GhiChu = "x" });
                TraVCalculator.Load(FnameDir, cache);
                var src = TraVCalculator.GetRealThicknessSource("SUS304 縞板", "6.0");
                Check("Sau khi gui: T thuc 7.3 dung ngay, danh dau bo sung + cho duyet (ca chieu Trong / Ngoai)",
                    TraVCalculator.GetRealThickness("SUS304 縞板", "6.0") == "7.3" && src != null && src.IsBoSung && src.ChoDuyet
                    && TraVCalculator.FindTam("SUS SIMA Ngoài", 6) != null);
                Check("T thuc tu OFT khong danh dau bo sung", TraVCalculator.GetRealThicknessSource("SS400", "3.2")?.IsBoSung == false);

                var pending = new List<TamRecord> { new TamRecord { Loai = "SUS SIMA", T = "6" } };
                Check("Prune: con cho duyet -> giu", TamLocalAdditions.Prune(cache, new List<TamRecord>(), pending) == 0);
                Check("Prune: khong doc duoc hang cho -> giu", TamLocalAdditions.Prune(cache, new List<TamRecord>(), null) == 0);
                Check("Prune: Sheet da co (duyet) -> bo", TamLocalAdditions.Prune(cache, TamData.Parse(sheet), null) == 1 && TamLocalAdditions.Load(cache).Count == 0);
                TamLocalAdditions.Add(cache, new TamRecord { Loai = "SUS SIMA", T = "6.0", THienThi = "7.3" });
                Check("Prune: bi tu choi (khong con trong hang cho) -> bo", TamLocalAdditions.Prune(cache, new List<TamRecord>(), new List<TamRecord>()) == 1);
            }
            finally
            {
                Directory.Delete(cache, true);
                TraVCalculator.Load(FnameDir, NoCache);
            }
        }

        private static void TestTamTableAndConfig()
        {
            // trav_90 doc theo ten cot: dao thu tu cot + bo T_hienthi van doc dung
            var t90 = TraVCalculator.ParseTable90(new[]
            {
                "V,Mat,Ri,T,Denta90,T_thuc,HikiShiro_90",
                "16,SS,2,3.2,2.6,3,2.6"
            });
            Check("trav_90 doc theo ten cot (dao thu tu, khong co T_hienthi)",
                t90.Count == 1 && t90[0].Mat == "SS" && t90[0].V == "16" && Math.Abs(t90[0].Tthuc - 3) < 1e-9 && t90[0].V2 == "2.6" && t90[0].C15 == "");
            Check("trav_90 khong co dong tieu de -> doc theo thu tu mac dinh",
                TraVCalculator.ParseTable90(new[] { "SS,3.2,3,16,2.6,2.6,2" }).Single().V == "16");

            // Bang tam: SIMA 1 dong chung cho ca Trong / Ngoai
            Check("BaseLoai bo chieu SIMA, giu loai thuong",
                TamData.BaseLoai("SUS SIMA Ngoài") == "SUS SIMA" && TamData.BaseLoai("sus sima trong") == "SUS SIMA" && TamData.BaseLoai("SS") == "SS");
            var tam = TamData.Parse(new[] { "T,Loai,T_hienthi,Nguon", "6.0,SUS SIMA,7.3,Bổ sung", "3.2,SS,3,OFT" });
            Check("Tam: SUS SIMA 6.0 dung cho ca Trong va Ngoai",
                TamData.Find(tam, "SUS SIMA Trong", 6)?.THienThi == "7.3" && TamData.Find(tam, "SUS SIMA Ngoài", 6.0)?.Nguon == "Bổ sung"
                && TamData.Find(tam, "SUS", 6) == null);
            Check("Tam: khong co dong tieu de -> khong doan cot", TamData.Parse(new[] { "SS,3.2,3" }).Count == 0);

            // File mau: T thuc lay tu tam.csv
            Check("CSV mau tam.csv co du SIMA gop 1 dong",
                TamData.Parse(FnameDataStore.ReadLines(FnameDir, "tam", NoCache)).Count(r => r.Loai == "SS SIMA") == 5);
            Check("T thuc SUS SIMA 3.0 lay tu tam = 4.5", TraVCalculator.GetRealThickness("SUS304 縞板", "3.0") == "4.5");

            // Phien ban toi thieu (tab cau_hinh)
            var cfg = FnameSheetConfig.Parse(new[] { "Khoa,Gia_tri,Ghi_chu", "phien_ban_toi_thieu,1.3.0,x" });
            Check("cau_hinh: 1.2.0 < 1.3.0 -> phai cap nhat; 1.3.0 / 1.4 -> khong",
                FnameVersionGate.IsOutdated("1.2.0", cfg) && !FnameVersionGate.IsOutdated("1.3.0", cfg) && !FnameVersionGate.IsOutdated("1.4", cfg));
            Check("cau_hinh: trong / sai -> khong chan",
                !FnameVersionGate.IsOutdated("1.0", new Dictionary<string, string>()) &&
                !FnameVersionGate.IsOutdated("1.0", FnameSheetConfig.Parse(new[] { "Khoa,Gia_tri", "phien_ban_toi_thieu,abc" })));
            Check("cau_hinh: kiem tra tieu de", FnameSheetConfig.HasHeader(new[] { "Khoa,Gia_tri" }) && !FnameSheetConfig.HasHeader(new[] { "a,b" }));

            // Kiem tra lien ket
            var cat = FnameCatalogData.Parse(new[] { "M,Loai,Z,T_list", "SS400,SS,SS400,3.2", "XX1,ZZZ,,1.0", "XX 縞板,SUS,,3.0" });
            var warn = FnameDataCheck.Check(cat, new[] { "SS", "SUS", "SUS SIMA Trong" },
                TamData.Parse(new[] { "Loai,T,T_hienthi", "SUS SIMA,3,4.5", "SUS SIMA,3.0,4.6", "QQ,1,1" }));
            Check("Kiem tra lien ket: bao Loai ZZZ, trung dong SUS SIMA 3, tam QQ; khong bao SS / SUS",
                warn.Count == 3 && warn.Any(w => w.Contains("ZZZ")) && warn.Any(w => w.Contains("trùng")) && warn.Any(w => w.Contains("\"QQ\"")),
                string.Join(" | ", warn));
            Check("File mau hien tai khong co canh bao lien ket", TraVCalculator.GetDataWarnings(FnameCatalogData.LoadFromModuleDirectory(FnameDir, NoCache)).Count == 0,
                string.Join(" | ", TraVCalculator.GetDataWarnings(FnameCatalogData.LoadFromModuleDirectory(FnameDir, NoCache))));
        }

        private static void TestTrialDieParams()
        {
            string cache = Path.Combine(Path.GetTempPath(), "ohta_thu_die_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cache);
            try
            {
                File.WriteAllText(Path.Combine(cache, "trav_thu.csv"), string.Join("\n", new[]
                {
                    TraVThuData.Header,
                    "SUS,9,60,V40,15,85494-2-1,Bẻ thử,2600,40,50,12.3",
                    "SUS,9,70,V40,13,X1,Bẻ thử,,,,"
                }) + "\n");
                TraVCalculator.Load(FnameDir, cache);

                var c60 = TraVCalculator.GetBendOptionsForFname("SUS304 2B", "9", 60).Find(c => c.DieLabel == "V40");
                Check("Be thu co thong so dao -> hien 最長 / L / Z / Rend, khong co R",
                    c60 != null && string.Join("|", c60.DetailParts) == "最長 2600|Ｌ最低 40|Ｚ最低 50|Rend 12.3", c60?.DetailInfo);

                var c65 = TraVCalculator.GetBendOptionsForFname("SUS304 2B", "9", 65).Find(c => c.DieLabel == "V40");
                Check("Noi suy 60/70: lay thong so dao cua goc goc co nhap, ghi kem vao dong gui Sheet",
                    c65 != null && c65.DetailParts.Count == 4 && c65.NewTrialRecord?.Saichou == "2600" && c65.NewTrialRecord.Rend == "12.3", c65?.DetailInfo);

                var back = TraVThuRecord.FromParams(FnameUpdateRequest.ParseParams(FnameUpdateRequest.BuildTrialParams(c65.NewTrialRecord)));
                Check("Thong_so trav_thu khu hoi 4 thong so dao", back.LSaitei == "40" && back.ZSaitei == "50" && back.HasDieParams);
                Check("CSV trav_thu ghi / doc lai 4 cot dao",
                    TraVThuData.Parse(TraVThuData.ToCsvLines(new[] { back })).Single().Saichou == "2600");

                // Goi y thong so dao cho form be thu
                var s1 = TraVCalculator.SuggestDieParams("SUS", 9, "40", out string src1);
                Check("Goi y: uu tien goc be thu cung loai + T + dao da co so", s1?.Saichou == "2600" && src1.Contains("85494-2-1"), src1);
                var s2 = TraVCalculator.SuggestDieParams("SUS", 6, "V40", out string src2);
                Check("Goi y: khong co be thu -> dong trav_90 DUNG loai + T + dao (SUS T=6 V40)",
                    s2 != null && s2.HasDieParams && src2.Contains("SUS T=6 V40"), src2);
                var s3 = TraVCalculator.SuggestDieParams("SUS", 7, "V40", out string src3);
                Check("Goi y: SUS T=7 V40 khong co dong -> chi 最長 theo dao V40, L/Z/Rend de trong",
                    s3 != null && s3.Saichou.Length > 0 && s3.LSaitei.Length == 0 && s3.Rend.Length == 0 && src3.Contains("V40"), src3);
                Check("最長 theo dao: khac loai + T van lay duoc",
                    TraVCalculator.SuggestDieParams("SPCC", 1.2, "V40", out _)?.Saichou == s3.Saichou);
                Check("Goi y: dao khong co trong du lieu -> null",
                    TraVCalculator.SuggestDieParams("SUS", 9, "V999", out _) == null);

                // Goc be thu khong co thong so dao + trav_90 khong co dong -> bao thieu
                var c70 = TraVCalculator.GetBendOptionsForFname("SUS304 2B", "9", 70).Find(c => c.DieLabel == "V40");
                Check("Be thu thieu thong so dao -> 最長 theo dao V40 + bao 'Chua co thong so dao' cho L/Z/Rend",
                    c70 != null && c70.DetailParts.Count == 2 && c70.DetailParts[0].StartsWith("最長 ")
                    && c70.DetailParts[1].StartsWith("Chưa có thông số dao"), c70?.DetailInfo);

                // Bang chung cua thong so dao di cung dong (chep sang dong noi suy, khu hoi qua Thong_so)
                var ev = new TraVThuRecord { Loai = "SUS", T = "9", Goc = 60, Dao = "V40", Denta = "15", Saichou = "2600",
                    NguoiXacNhan = "Anh Tu", GhiChu = "do thuoc", Anh = "https://drive.google.com/file/d/abc/view" };
                var evBack = TraVThuRecord.FromParams(FnameUpdateRequest.ParseParams(FnameUpdateRequest.BuildTrialParams(ev, "thay so cu")));
                Check("Thong_so trav_thu khu hoi bang chung + gop ghi chu",
                    evBack.NguoiXacNhan == "Anh Tu" && evBack.Anh.EndsWith("/view") && evBack.GhiChu == "do thuoc — thay so cu", evBack.GhiChu);
                var copy = new TraVThuRecord();
                copy.CopyDieParams(ev);
                Check("CopyDieParams chep ca bang chung", copy.NguoiXacNhan == "Anh Tu" && copy.Anh == ev.Anh && copy.SameDieParams(ev));
                Check("CSV trav_thu ghi / doc lai 3 cot bang chung",
                    TraVThuData.Parse(TraVThuData.ToCsvLines(new[] { ev })).Single().NguoiXacNhan == "Anh Tu");
            }
            finally
            {
                Directory.Delete(cache, true);
                TraVCalculator.Load(FnameDir, NoCache);
            }
        }

        private static void TestTrialBendLookup()
        {
            Check("T= luon co 1 so le: 6 -> 6.0, 3.2 giu, 0.05 giu, ６ toan goc -> 6.0, chu giu nguyen",
                FnameCatalogData.FormatThickness("6") == "6.0" && FnameCatalogData.FormatThickness(" 3.2 ") == "3.2"
                && FnameCatalogData.FormatThickness("0.05") == "0.05" && FnameCatalogData.FormatThickness("６") == "6.0"
                && FnameCatalogData.FormatThickness("abc") == "abc" && FnameCatalogData.FormatThickness("") == "");

            string cache = Path.Combine(Path.GetTempPath(), "ohta_trial_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cache);
            try
            {
                File.WriteAllText(Path.Combine(cache, "trav_thu.csv"), string.Join("\n", new[]
                {
                    "Dinh_danh,Loai,T,Goc,Dao,Denta,Ma_tam", // thu tu cot tuy y
                    "Bẻ thử,SS SIMA Ngoài,3.2,70,V12,3.0,A1",
                    "Bẻ thử,ss sima ngoai,3.2,80,12,3.4,B2",   // khong dau / thieu chu V van khop
                    "Bẻ thử,SS SIMA Ngoài,3.2,60,V16,4.0,C3",
                    "Bẻ thử,SS SIMA Ngoài,3.2,85,V16,5.0,D4",
                    "Nội suy,SS SIMA Ngoài,3.2,72,V12,3.08,A1 / B2",
                    "Bẻ thử,SS,3.2,90,V16,9.9,X9",
                    "Bẻ thử,SS,14.0,90,V100,20.5,Z1",
                    "Bẻ thử,SS SIMA Ngoài,3.2,72,V20,3.0,E5",
                    "Bẻ thử,SS SIMA Ngoài,3.2,74,V20,3.1,F6",
                    "Bẻ thử,SS SIMA Ngoài,3.2,70,V18,3.0,G7",
                    "Nội suy,SS SIMA Ngoài,3.2,80,V18,3.4,H8 (78°) / I9 (82°)",
                    "Bẻ thử,SS SIMA Ngoài,3.2,86,V32,4.1,K1"   // V32 co so lieu 90 do trong bang TraV (4.5)
                }) + "\n");
                TraVCalculator.Load(FnameDir, cache);

                // Khong lam tron: goc le 70.3 -> noi suy 70/80 dung goc 70.3
                var c703 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 70.3).Find(c => c.DieLabel == "V12");
                Check("Goc le 70.3 -> noi suy 70/80 = 3.01, gui Sheet goc 70.3",
                    c703 != null && c703.FormattedText == "シマ目外 V12引代 3.01(70.3°)" && c703.Source == "Nội suy"
                    && Math.Abs(c703.NewTrialRecord.Goc - 70.3) < 1e-9,
                    c703?.FormattedText + " | " + c703?.SourceInfo);

                // Goc goc cach goc can tra < 1 do -> khong hoi quan ly (du khoang 60/85 > 10 do)
                var n605 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 60.5).Find(c => c.DieLabel == "V16");
                Check("Noi suy 60.5 tu 60/85: cach 60 chi 0.5 do -> khong hoi quan ly",
                    n605 != null && !n605.NeedsManagerConfirm && n605.Warning == "", n605?.Warning);
                var n61 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 61.0).Find(c => c.DieLabel == "V16");
                Check("Noi suy 61 tu 60/85: cach dung 1 do -> van hoi quan ly", n61 != null && n61.NeedsManagerConfirm, n61?.Warning);

                // Chi co goc 1 phia (72, 74) -> ngoai suy toi da 2 do, hoi quan ly tru khi cach goc do < 1 do
                var e75 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 75.0).Find(c => c.DieLabel == "V20");
                Check("Ngoai suy 75 tu 72/74 (V20) -> 3.15, nhan Ngoai suy, bat xac nhan",
                    e75 != null && e75.FormattedText == "シマ目外 V20引代 3.15(75°)" && e75.Source == "Ngoại suy" && e75.NeedsManagerConfirm
                    && e75.Warning.Contains("Ngoại suy") && e75.NewTrialRecord?.DinhDanh == "Ngoại suy" && e75.NewTrialRecord.Goc == 75,
                    e75?.FormattedText + " | " + e75?.Warning);
                var e745 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 74.5).Find(c => c.DieLabel == "V20");
                Check("Ngoai suy 74.5 (cach 74 chi 0.5 do) -> 3.12, khong hoi quan ly",
                    e745 != null && e745.FormattedText == "シマ目外 V20引代 3.12(74.5°)" && !e745.NeedsManagerConfirm && e745.Warning == "",
                    e745?.FormattedText + " | " + e745?.Warning);
                Check("Ngoai suy qua 2 do (76.4 cach 74 la 2.4 do) -> khong ra dong",
                    !TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 76.4).Exists(c => c.DieLabel == "V20"));

                var c75 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 75.0);
                var v12 = c75.Find(c => c.DieLabel == "V12");
                Check("Noi suy SIMA Ngoai 75 tu 70/80 (V12) -> シマ目外 V12引代 3.2(75°)",
                    v12 != null && v12.FormattedText == "シマ目外 V12引代 3.2(75°)" && v12.Source == "Nội suy"
                    && v12.SourceInfo == "từ góc 70° (tấm A1) và 80° (tấm B2)" && !v12.NeedsManagerConfirm && v12.Warning == "",
                    string.Join(", ", c75.ConvertAll(c => c.FormattedText + " [" + c.SourceInfo + "]")));
                Check("Noi suy moi -> co NewTrialRecord de gui Sheet",
                    v12?.NewTrialRecord != null && v12.NewTrialRecord.Denta == "3.2" && v12.NewTrialRecord.DinhDanh == "Nội suy" && v12.NewTrialRecord.MaTam == "A1 (70°) / B2 (80°)");
                var v16 = c75.Find(c => c.DieLabel == "V16");
                Check("Noi suy 60/85 cach 25 do > 10 -> canh bao + bat xac nhan quan ly",
                    v16 != null && v16.NeedsManagerConfirm && v16.Warning.Contains("quản lý"), v16?.Warning);
                Check("Khong co SIMA Trong trong bang thu -> khong ra dong シマ目内", !c75.Exists(c => c.FormattedText.Contains("シマ目内")));

                var c70 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 70.0).Find(c => c.DieLabel == "V12");
                Check("Dung goc be thu 70 -> nhan Be thu, tham khao tam A1, khong gui lai",
                    c70 != null && c70.Source == "Bẻ thử" && c70.SourceInfo == "tham khảo tấm A1" && c70.NewTrialRecord == null
                    && c70.FormattedText == "シマ目外 V12引代 3(70°)", c70?.FormattedText + " " + c70?.SourceInfo);
                var c72 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 72.0).Find(c => c.DieLabel == "V12");
                Check("Goc noi suy da luu (72) -> nhan Noi suy, khong gui lai",
                    c72 != null && c72.Source == "Nội suy" && c72.NewTrialRecord == null && c72.FormattedText.Contains("3.08"), c72?.FormattedText);
                Check("Chi 1 phia (goc 50) -> khong ngoai suy",
                    !TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 50.0).Exists(c => c.DieLabel == "V12"));

                // Khong du goc be thu -> dung ca goc noi suy lam goc, LUON hoi quan ly (ke ca khi cach < 1 do)
                var g75 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 75.0).Find(c => c.DieLabel == "V18");
                Check("Goc goc co dinh danh noi suy -> van tinh, bat xac nhan, ghi ro goc noi suy",
                    g75 != null && g75.FormattedText == "シマ目外 V18引代 3.2(75°)" && g75.NeedsManagerConfirm
                    && g75.Warning.Contains("80° (nội suy)") && g75.SourceInfo == "từ góc 70° (tấm G7) và 80° (nội suy: H8 (78°) / I9 (82°))"
                    && g75.NewTrialRecord.MaTam == "G7 (70°) / nội suy 80°",
                    g75?.SourceInfo + " | " + g75?.Warning + " | " + g75?.NewTrialRecord?.MaTam);
                var g705 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 70.5).Find(c => c.DieLabel == "V18");
                Check("Goc noi suy lam goc: cach 70 chi 0.5 do van hoi quan ly", g705 != null && g705.NeedsManagerConfirm, g705?.Warning);

                // Goc 90 cua bang TraV lam goc noi suy nhu 1 goc be thu: 88 tu 86 (K1, 4.1) va 90 (4.5) -> 4.3
                var k88 = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 88.0).Find(c => c.DieLabel == "V32");
                Check("Noi suy 88 tu be thu 86 va goc 90 bang TraV (V32) -> 4.3, khong hoi quan ly",
                    k88 != null && k88.FormattedText == "シマ目外 V32引代 4.3(88°)" && k88.Source == "Nội suy"
                    && k88.SourceInfo == "từ góc 86° (tấm K1) và 90° (bảng TraV)" && !k88.NeedsManagerConfirm
                    && k88.NewTrialRecord?.MaTam == "K1 (86°) / bảng TraV (90°)",
                    k88?.FormattedText + " | " + k88?.SourceInfo + " | " + k88?.NewTrialRecord?.MaTam);

                var ss90 = TraVCalculator.GetBendOptionsForFname("SS400", "3.2", 90.0);
                Check("Bang TraV tinh duoc -> khong dung goc be thu", ss90.Count > 0 && ss90.TrueForAll(c => c.Source == "" && !c.FormattedText.Contains("9.9")));
                var ss14 = TraVCalculator.GetBendOptionsForFname("SS400", "14.0", 90.0);
                Check("SS T=14 khong co trong TraV -> dung be thu V=100-20.5(90°)",
                    ss14.Count == 1 && ss14[0].FormattedText == "V=100-20.5(90°)" && ss14[0].Source == "Bẻ thử",
                    string.Join(", ", ss14.ConvertAll(c => c.FormattedText)));

                // Yeu cau duyet + bo sung cuc bo
                var rec = v12.NewTrialRecord;
                string ps = FnameUpdateRequest.BuildTrialParams(rec, "x");
                var back = TraVThuRecord.FromParams(FnameUpdateRequest.ParseParams(ps));
                Check("Thong_so trav_thu khu hoi", back.SameKey(rec) && back.Denta == "3.2" && back.MaTam == "A1 (70°) / B2 (80°)" && back.DinhDanh == "Nội suy", ps);
                var req = new FnameUpdateRequest { Loai = FnameUpdateRequest.LoaiTrialBend, ThongSo = ps };
                Check("Describe trav_thu", req.Describe().StartsWith("Nội suy SS SIMA Ngoài T=3.2 — 75° — V12 — Denta 3.2 — tấm A1 (70°) / B2 (80°)"), req.Describe());

                TraVThuLocalAdditions.Add(cache, rec);
                TraVCalculator.Load(FnameDir, cache);
                var again = TraVCalculator.GetBendOptionsForFname("SS400 縞板", "3.2", 75.0).Find(c => c.DieLabel == "V12");
                Check("Sau khi gui: may nay dung ngay dong noi suy da luu (khong gui lai)",
                    again != null && again.Source == "Nội suy" && again.NewTrialRecord == null);

                var sheet = TraVThuData.Parse(new[] { TraVThuData.Header });
                Check("Prune: van cho duyet -> giu", TraVThuLocalAdditions.Prune(cache, sheet, new List<TraVThuRecord> { back }) == 0);
                Check("Prune: khong doc duoc hang cho -> giu", TraVThuLocalAdditions.Prune(cache, sheet, null) == 0);
                var approved = TraVThuData.Parse(TraVThuData.ToCsvLines(new[] { rec }));
                Check("Prune: Sheet da co (admin duyet) -> bo", TraVThuLocalAdditions.Prune(cache, approved, null) == 1 && TraVThuLocalAdditions.Load(cache).Count == 0);
            }
            finally
            {
                Directory.Delete(cache, true);
                TraVCalculator.Load(FnameDir, NoCache);
            }
        }

        private static void TestPressOneTimeFormatAndParsing()
        {
            string line1 = TraVCalculator.FormatPressOneTimeLine("5", "110");
            Check("Format nhan 1 lan 5mm 110 do -> '内R5-発押し110°'", line1 == "内R5-発押し110°", line1);

            string line2 = TraVCalculator.FormatPressOneTimeLine(" 8.5 ", " 90 ");
            Check("Format nhan 1 lan co space -> '内R8.5-発押し90°'", line2 == "内R8.5-発押し90°", line2);

            bool ok1 = TraVCalculator.TryParsePressOneTimeLine("内R5-発押し110°", out string r1, out string a1);
            Check("Parse dung dong nhan 1 lan chuẩn -> r=5, a=110", ok1 && r1 == "5" && a1 == "110", $"ok={ok1}, r={r1}, a={a1}");

            bool ok2 = TraVCalculator.TryParsePressOneTimeLine("内R10-発押し90", out string r2, out string a2);
            Check("Parse dung dong nhan thieu ky hieu do -> r=10, a=90", ok2 && r2 == "10" && a2 == "90", $"ok={ok2}, r={r2}, a={a2}");

            bool okBad = TraVCalculator.TryParsePressOneTimeLine("N=47079-1", out _, out _);
            Check("Dong khong phai nhan 1 lan tra false", !okBad);
        }

        private static void TestFnameRecordBendAndPressOrdering()
        {
            var rec = new FnameRecord
            {
                N = "47079-1",
                M = "SS400",
                T = "3.2",
                Q = "2",
                Z = "SS400"
            };
            rec.BendLines.Add("V=16-2.6(90°)");
            rec.BendLines.Add("V=25-2.9(90°)");
            rec.ExtraBeforeZ.Add("Ghi chu truoc Z");
            rec.ExtraAfterZ.Add("Ghi chu sau Z");
            rec.PressLines.Add("内R5-発押し110°");

            List<string> lines = rec.BuildLines(includePreservedExtras: true);

            Check("BuildLines gom du 10 dong", lines.Count == 10, $"Count={lines.Count}");
            Check("Dong 0 la N=47079-1", lines[0] == "N=47079-1");
            Check("Dong 1 la M=SS400", lines[1] == "M=SS400");
            Check("Dong 2 la T=3.2", lines[2] == "T=3.2");
            Check("Dong 3 la Q=2", lines[3] == "Q=2");
            Check("Dong 4 la V=16-2.6(90°) (BendLines nam truoc Z)", lines[4] == "V=16-2.6(90°)");
            Check("Dong 5 la V=25-2.9(90°)", lines[5] == "V=25-2.9(90°)");
            Check("Dong 6 la ExtraBeforeZ", lines[6] == "Ghi chu truoc Z");
            Check("Dong 7 la Z=SS400", lines[7] == "Z=SS400");
            Check("Dong 8 la ExtraAfterZ", lines[8] == "Ghi chu sau Z");
            Check("Dong cuoi cung (Dong 9 trong danh sach) BAT BUOC la nhan 1 lan (PressLines)",
                lines[lines.Count - 1] == "内R5-発押し110°",
                lines[lines.Count - 1]);
        }

        private static void TestFnameParserWithBendAndPress()
        {
            string mtext = "N=47079-1\\PM=SS400\\PT=3.2\\PQ=2\\PV=16-2.6(90°)\\PZ=SS400\\P内R5-発押し110°";
            FnameRecord parsed = FnameParser.Parse(new[] { mtext });

            Check("Parse tach dung BendLines", parsed.BendLines.Contains("V=16-2.6(90°)"));
            Check("Parse tach dung PressLines", parsed.PressLines.Contains("内R5-発押し110°"));
            Check("Parse khong bi trung dong o ExtraBeforeZ hoac ExtraAfterZ",
                !parsed.ExtraBeforeZ.Contains("V=16-2.6(90°)") &&
                !parsed.ExtraAfterZ.Contains("内R5-発押し110°"));

            List<string> rebuilt = parsed.BuildLines(includePreservedExtras: true);
            Check("Rebuilt dong cuoi cung van luon la PressLines",
                rebuilt[rebuilt.Count - 1] == "内R5-発押し110°",
                rebuilt[rebuilt.Count - 1]);

            // Test tam SIMA luon co dong 裏から見た図 duoi Z=
            var simaRec = new FnameRecord
            {
                N = "47079-SIMA",
                M = "ムキ 縞板",
                T = "3.2",
                Q = "1",
                Z = "SS400-SIMA"
            };
            simaRec.BendLines.Add("シマ目外 V25引代 4.25(90°)");
            simaRec.PressLines.Add("内R5-発押し110°");
            var simaLines = simaRec.BuildLines(includePreservedExtras: false);

            Check("Tam Sima tu sinh '裏から見た図' duoi Z=",
                simaLines.Count == 8 &&
                simaLines[5] == "Z=SS400-SIMA" &&
                simaLines[6] == "裏から見た図" &&
                simaLines[7] == "内R5-発押し110°");
        }
    }
}
