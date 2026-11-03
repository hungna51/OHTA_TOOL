using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Tests
{
    // Test cho tab chung: nhóm, nút ＋ thêm lisp, sửa module.json (ADR 0002)
    internal static partial class Program
    {
        private static void TestTabEditFeatures()
        {
            TestModuleGrouping();
            TestGroupCollapseStore();
            TestJsonLiteRoundTrip();
            TestModuleJsonEditorTools();
            TestModuleJsonEditorGroups();
            TestModuleJsonEditorDiskSave();
            TestLispScan();
            TestLispCopyAndNames();
            TestCommandConflict();
        }

        private static void TestModuleGrouping()
        {
            var tools = new List<ToolItem>
            {
                new ToolItem { Id = "a", Category = "Dim" },
                new ToolItem { Id = "b", Category = "" },
                new ToolItem { Id = "c", Category = "In" },
                new ToolItem { Id = "d", Category = "dim" },
                new ToolItem { Id = "e", Category = "Khác" }
            };
            List<ToolGroup> g = ModuleGrouping.BuildGroups(tools, new[] { "In", "Rong" });
            Check("Nhom: thu tu theo groups, nhom chua khai bao xep sau, Khac cuoi",
                string.Join(",", g.Select(x => x.Name)) == "In,Rong,Dim,Khác",
                string.Join(",", g.Select(x => x.Name)));
            Check("Nhom: so sanh ten nhom khong phan biet hoa thuong",
                g[2].Tools.Select(t => t.Id).SequenceEqual(new[] { "a", "d" }));
            Check("Nhom: category trong hoac 'Khác' vao nhom Khac",
                g[3].IsUngrouped && g[3].Tools.Select(t => t.Id).SequenceEqual(new[] { "b", "e" }));
            Check("Nhom: khong co nut chua nhom thi khong hien Khac",
                !ModuleGrouping.BuildGroups(new[] { new ToolItem { Category = "X" } }, null).Any(x => x.IsUngrouped));
        }

        private static void TestGroupCollapseStore()
        {
            string path = Path.Combine(Path.GetTempPath(), "ohta_collapse_" + Guid.NewGuid().ToString("N"), "ui_state.txt");
            try
            {
                var s1 = new GroupCollapseStore(path);
                s1.SetCollapsed("module_02", "Dim", true);
                s1.SetCollapsed("guro", "In", true);
                s1.SetCollapsed("guro", "In", false);
                var s2 = new GroupCollapseStore(path);
                Check("Thu gon nhom: luu va doc lai theo may",
                    s2.IsCollapsed("module_02", "dim") && !s2.IsCollapsed("guro", "In"));
            }
            finally
            {
                try { Directory.Delete(Path.GetDirectoryName(path), true); } catch { }
            }
        }

        private static void TestJsonLiteRoundTrip()
        {
            string src = "{\n  \"id\": \"m\",\n  \"order\": 90,\n  \"x_la\": {\"a\": [1, 2.5, true, null]},\n  \"title\": \"Cắt \\\"dim\\\" ムキ\"\n}\n";
            var root = (JsonObject)JsonLite.Parse(src);
            string outText = JsonLite.Serialize(root);
            var again = (JsonObject)JsonLite.Parse(outText);
            Check("JsonLite: giu thu tu khoa", string.Join(",", root.Items.Select(i => i.Key)) == "id,order,x_la,title");
            Check("JsonLite: so giu nguyen chuoi goc", outText.Contains("\"order\": 90,") && outText.Contains("2.5"), outText);
            Check("JsonLite: chu Viet/Nhat va dau ngoac kep doc lai dung", again.GetString("title") == "Cắt \"dim\" ムキ", again.GetString("title"));
            Check("JsonLite: truong la (object long) con nguyen", again.Get("x_la") is JsonObject xo && xo.Get("a") is List<object> l && l.Count == 4);
            bool threw = false;
            try { JsonLite.Parse("{\"a\": }"); } catch (FormatException) { threw = true; }
            Check("JsonLite: JSON hong thi bao loi", threw);
        }

        private static JsonObject SampleModuleJson() => (JsonObject)JsonLite.Parse(
            "{\"id\":\"module_02\",\"accentColor\":\"#2E7D32\",\"startupLisp\":[\"lisp/a.lsp\"],\"tools\":[" +
            "{\"id\":\"cd\",\"category\":\"Dim\",\"name\":\"Cat dim\",\"command\":\"CD\",\"lispFile\":\"lisp/a.lsp\",\"x_rieng\":1}," +
            "{\"id\":\"gro\",\"name\":\"GRO\",\"command\":\"GRO\",\"locked\":true}," +
            "{\"id\":\"bd\",\"category\":\"Dim\",\"name\":\"Can dim\",\"command\":\"BD\"}]}");

        private static void TestModuleJsonEditorTools()
        {
            JsonObject root = SampleModuleJson();

            string id = ModuleJsonEditor.AddTool(root, new ToolItem { Command = "CD", Name = "Moi", Category = "In", LispFile = "lisp\\b.lsp" });
            Check("Them nut: tu sinh id khong trung", id == "u_cd" && ModuleJsonEditor.FindTool(root, "u_cd") != null, id);
            Check("Them nut: lispFile doi \\ thanh /", ModuleJsonEditor.FindTool(root, id).GetString("lispFile") == "lisp/b.lsp");
            Check("Them nut: nhom moi vao groups (giu thu tu nhom dang hien)",
                string.Join(",", ModuleJsonEditor.GetGroups(root)) == "Dim,In", string.Join(",", ModuleJsonEditor.GetGroups(root)));

            ModuleJsonEditor.UpdateTool(root, "cd", new ToolItem { Command = "CD", Name = "Cat chan dim", Category = "Dim", Description = "x" });
            JsonObject cd = ModuleJsonEditor.FindTool(root, "cd");
            Check("Sua nut: doi ten, giu truong la x_rieng", cd.GetString("name") == "Cat chan dim" && cd.ContainsKey("x_rieng"));

            Check("Xoa nut locked bi tu choi", !ModuleJsonEditor.RemoveTool(root, "gro") && ModuleJsonEditor.FindTool(root, "gro") != null);
            Check("Xoa nut thuong", ModuleJsonEditor.RemoveTool(root, id) && ModuleJsonEditor.FindTool(root, id) == null);

            // Len/Xuong trong cung nhom: bd nhay qua gro (nhom khac) de len tren cd
            Check("Len trong nhom: nhay qua nut nhom khac", ModuleJsonEditor.MoveToolInGroup(root, "bd", -1));
            Check("Len trong nhom: thu tu moi bd,gro,cd",
                string.Join(",", ModuleJsonEditor.Tools(root).Select(t => t.GetString("id"))) == "bd,gro,cd",
                string.Join(",", ModuleJsonEditor.Tools(root).Select(t => t.GetString("id"))));
            Check("Len o dau nhom: khong doi", !ModuleJsonEditor.MoveToolInGroup(root, "bd", -1));

            ModuleJsonEditor.SetToolCategory(root, "gro", "Khác");
            Check("Chuyen nhom ve Khac: bo category", !ModuleJsonEditor.FindTool(root, "gro").ContainsKey("category"));

            ModuleJsonEditor.AddStartupLisp(root, "lisp/a.lsp");
            ModuleJsonEditor.AddStartupLisp(root, "lisp/b.lsp");
            Check("startupLisp: khong them trung, them file moi",
                ((List<object>)root.Get("startupLisp")).Count == 2);
        }

        private static void TestModuleJsonEditorGroups()
        {
            JsonObject root = SampleModuleJson();
            ModuleJsonEditor.SetGroups(root, new[] { "In", "Dim", "dim", " ", "Khác" });
            Check("SetGroups: bo trung, bo rong, bo Khac", string.Join(",", ModuleJsonEditor.GetGroups(root)) == "In,Dim");

            ModuleJsonEditor.RenameGroup(root, "Dim", "Kich thuoc");
            Check("Doi ten nhom: sua ca groups va category cua nut",
                ModuleJsonEditor.GetGroups(root).Contains("Kich thuoc") &&
                ModuleJsonEditor.FindTool(root, "bd").GetString("category") == "Kich thuoc");

            ModuleJsonEditor.DeleteGroup(root, "Kich thuoc");
            Check("Xoa nhom: nut ve Khac, groups bo ten",
                !ModuleJsonEditor.FindTool(root, "cd").ContainsKey("category") &&
                string.Join(",", ModuleJsonEditor.GetGroups(root)) == "In");

            ModuleJsonEditor.SetHeader(root, " Tab moi ", "Mo ta", "#123456");
            Check("Sua header tab", root.GetString("title") == "Tab moi" && root.GetString("accentColor") == "#123456");
        }

        private static void TestModuleJsonEditorDiskSave()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ohta_modjson_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "module.json");
                File.WriteAllText(path, JsonLite.Serialize(SampleModuleJson()), new UTF8Encoding(true));

                // Mô phỏng máy khác vừa thêm 1 nút trên đĩa: Update phải đọc lại bản mới nhất
                ModuleJsonEditor.Update(dir, r => ModuleJsonEditor.AddTool(r, new ToolItem { Command = "M1", Name = "May 1" }));
                ModuleJsonEditor.Update(dir, r => ModuleJsonEditor.AddTool(r, new ToolItem { Command = "M2", Name = "Máy 2" }));

                JsonObject back = ModuleJsonEditor.Load(dir);
                Check("Ghi dia: 2 lan ghi lien tiep khong mat nut",
                    ModuleJsonEditor.FindTool(back, "u_m1") != null && ModuleJsonEditor.FindTool(back, "u_m2") != null);
                Check("Ghi dia: co module.json.bak", File.Exists(path + ".bak"));
                Check("Ghi dia: khong con file .tmp", !File.Exists(path + ".tmp"));

                byte[] head = File.ReadAllBytes(path).Take(3).ToArray();
                Check("Ghi dia: UTF-8 khong BOM", !(head[0] == 0xEF && head[1] == 0xBB));

                ModuleManifest m = ModuleDiscovery.LoadModuleFromDirectory(dir, "02_Test", "#000000");
                Check("Ghi dia: ModuleDiscovery doc lai duoc (locked, nhom)",
                    m.LoadWarning == null && m.Tools.Count == 5 && m.Tools.Single(t => t.Id == "gro").IsLocked &&
                    m.Tools.Single(t => t.Id == "u_m2").Name == "Máy 2",
                    m.LoadWarning);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        private static void TestLispScan()
        {
            // Chuỗi CP932 (byte đuôi 0x5C '\') + chú thích + defun trong chuỗi phải bị bỏ qua
            var bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("; (defun c:COMMENT ()\n;| (defun c:BLOCK () |;\n(DEFUN C:cd (/ a)\n (princ \""));
            bytes.AddRange(new byte[] { 0x83, 0x5C, 0x83, 0x74, 0x83, 0x67 }); // ソフト (CP932)
            bytes.AddRange(Encoding.ASCII.GetBytes("\")\n (princ \"(defun c:INSTR\"))\n(defun  c:BD-2 ()) (defun c:cd ()) (defun helper ())"));

            List<string> cmds = LispImport.ScanCommands(bytes.ToArray());
            Check("Quet defun c: lay CD, BD-2 (in hoa, khong trung)", string.Join(",", cmds) == "CD,BD-2", string.Join(",", cmds));
            Check("Quet defun: file rong khong loi", LispImport.ScanCommands(new byte[0]).Count == 0);
            Check("Badge goi y toi da 4 ky tu", LispImport.SuggestBadge("bd-2") == "BD2" && LispImport.SuggestBadge("ABCDEF") == "ABCD");
        }

        private static void TestLispCopyAndNames()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ohta_import_" + Guid.NewGuid().ToString("N"));
            string src = Path.Combine(Path.GetTempPath(), "ohta_src_" + Guid.NewGuid().ToString("N") + ".lsp");
            try
            {
                Directory.CreateDirectory(Path.Combine(dir, "lisp"));
                byte[] raw = { 0x28, 0x83, 0x5C, 0x29, 0x0D, 0x0A };
                File.WriteAllBytes(src, raw);

                string rel = LispImport.CopyIntoModule(dir, src, "lisp", "a.lsp", false);
                Check("Chep lisp: tra ve duong dan tuong doi", rel == "lisp/a.lsp", rel);
                Check("Chep lisp: byte nguyen ven", File.ReadAllBytes(Path.Combine(dir, "lisp", "a.lsp")).SequenceEqual(raw));
                Check("Ten trung: goi y a_2.lsp", LispImport.NextFreeFileName(Path.Combine(dir, "lisp"), "a.lsp") == "a_2.lsp");

                string inside = Path.Combine(dir, "lisp", "a.lsp");
                Check("File da nam trong tab: khong chep lai", LispImport.CopyIntoModule(dir, inside, "lisp", "zzz.lsp", false) == "lisp/a.lsp");
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
                try { File.Delete(src); } catch { }
            }
        }

        private static void TestCommandConflict()
        {
            var modules = new List<ModuleManifest>
            {
                new ModuleManifest { TabTitle = "Guro", DirectoryPath = @"C:\o\03_Guro", Tools = new List<ToolItem> { new ToolItem { Id = "gro", Command = "GRO" } } },
                new ModuleManifest { TabTitle = "More", DirectoryPath = @"C:\o\02_More", Tools = new List<ToolItem> { new ToolItem { Id = "cd", Command = "CD" } } }
            };
            Check("Trung lenh: bao ten tab", ModuleJsonEditor.FindCommandOwner(modules, "gro") == "Guro");
            Check("Trung lenh: lenh moi khong trung", ModuleJsonEditor.FindCommandOwner(modules, "XYZ") == null);
            Check("Trung lenh: bo qua chinh nut dang sua", ModuleJsonEditor.FindCommandOwner(modules, "CD", @"C:\o\02_More\", "cd") == null);
        }
    }
}
