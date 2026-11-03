using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Tests
{
    // Test tu dong cap nhat tu GitHub Releases (ADR 0004)
    internal static partial class Program
    {
        private static void TestUpdateFeatures()
        {
            TestUpdateVersionCompare();
            TestReleaseManifestParse();
            TestReleaseInfoParseGitHub();
            TestUpdateStateRoundTrip();
            TestModuleJsonMergeRules();
            TestUpdateInstallAndRollback();
            TestUpdateInstallRejectsBadHash();
        }

        private static void TestUpdateVersionCompare()
        {
            Check("Phien ban: 1.10.0 moi hon 1.9.3", UpdateVersion.Compare("1.10.0", "1.9.3") > 0);
            Check("Phien ban: tien to v va so thieu coi la 0", UpdateVersion.Compare("v1.2", "1.2.0") == 0);
            Check("Phien ban: sai dinh dang khong bao gio la ban moi", !UpdateVersion.IsNewer("abc", "0"));
            Check("Phien ban: ban hien tai '0' (chua co VERSION) cu hon moi ban", UpdateVersion.IsNewer("1.1.0", "0"));
            Check("Phien ban: so am / chu bi tu choi", UpdateVersion.Parse("1.-2") == null && UpdateVersion.Parse("1.x") == null);
        }

        private static void TestReleaseManifestParse()
        {
            ReleaseManifest m = ReleaseManifest.Parse("# ghi chu\nABC  _system/bin/ZwCAD/OHTATOOL.dll\r\ndef  03_Guro\\module.json\n");
            Check("Manifest: doc hash (chu thuong) va duong dan dung dau /",
                m.Files["_system/bin/ZwCAD/OHTATOOL.dll"] == "abc" && m.Files.ContainsKey("03_Guro/module.json"));
            Check("Manifest: ghi lai roi doc lai giu nguyen",
                ReleaseManifest.Parse(m.Serialize()).Files.Count == 2);

            bool threw = false;
            try { ReleaseManifest.Parse("abc  ../ngoai.txt"); } catch (FormatException) { threw = true; }
            Check("Manifest: chan duong dan '..' ra ngoai thu muc goc", threw);
            Check("Manifest: chan duong dan tuyet doi",
                ReleaseManifest.NormalizePath("C:/x.txt") == null && ReleaseManifest.NormalizePath("/x") == null);
        }

        private static void TestReleaseInfoParseGitHub()
        {
            string json = "{\"tag_name\":\"v1.2.0\",\"body\":\"- Sua loi A\\r\\n- Them B\",\"assets\":[" +
                          "{\"name\":\"khac.txt\",\"browser_download_url\":\"https://x/khac.txt\"}," +
                          "{\"name\":\"OHTA_TOOL_v1.2.0.zip\",\"browser_download_url\":\"https://x/o.zip\",\"digest\":\"sha256:ABCD\"}]}";
            ReleaseInfo r = ReleaseInfo.ParseGitHub(json);
            Check("GitHub: doc tag, ghi chu, asset zip va sha256",
                r != null && r.Version == "1.2.0" && r.AssetUrl == "https://x/o.zip" && r.AssetSha256 == "abcd" &&
                r.Notes == "- Sua loi A\n- Them B",
                r == null ? "null" : $"{r.Version}|{r.AssetUrl}|{r.AssetSha256}|{r.Notes}");
            Check("GitHub: release khong co zip OHTA_TOOL_ thi bo qua",
                ReleaseInfo.ParseGitHub("{\"tag_name\":\"v1.2.0\",\"assets\":[]}") == null);
        }

        private static void TestUpdateStateRoundTrip()
        {
            string path = Path.Combine(Path.GetTempPath(), "ohta_upd_state_" + Guid.NewGuid().ToString("N"), "state.json");
            try
            {
                var s = new UpdateState
                {
                    LastCheckDate = "2026-11-03",
                    Latest = new ReleaseInfo { Version = "1.2.0", Notes = "Ghi chú tiếng Việt", AssetUrl = "u", AssetSha256 = "h" }
                };
                s.Save(path);
                UpdateState t = UpdateState.Load(path);
                Check("Trang thai: luu va doc lai ngay kiem tra + ban moi nhat",
                    t.CheckedToday(new DateTime(2026, 11, 3)) && t.Latest?.Version == "1.2.0" && t.Latest.Notes == "Ghi chú tiếng Việt");
                File.WriteAllText(path, "{hong");
                Check("Trang thai: file hong coi nhu chua kiem tra", UpdateState.Load(path).LastCheckDate == null);
            }
            finally
            {
                try { Directory.Delete(Path.GetDirectoryName(path), true); } catch { }
            }
        }

        private static JsonObject J(string json) => (JsonObject)JsonLite.Parse(json);

        private static string ToolIds(JsonObject o) => string.Join(",", ModuleJsonEditor.Tools(o).Select(t => t.GetString("id")));

        private static void TestModuleJsonMergeRules()
        {
            JsonObject baseJ = J("{\"title\":\"A\",\"accentColor\":\"#111\",\"groups\":[\"In\"],\"tools\":[" +
                                 "{\"id\":\"gro\",\"locked\":true,\"name\":\"GRO\"},{\"id\":\"x\",\"name\":\"X cu\"},{\"id\":\"y\",\"name\":\"Y\"}]}");
            JsonObject release = J("{\"title\":\"A2\",\"accentColor\":\"#111\",\"groups\":[\"In\",\"Dim\"],\"tools\":[" +
                                   "{\"id\":\"gro\",\"locked\":true,\"name\":\"GRO moi\"},{\"id\":\"x\",\"name\":\"X moi\"},{\"id\":\"y\",\"name\":\"Y\"},{\"id\":\"z\",\"name\":\"Z\"}]}");

            JsonObject same = ModuleJsonMerge.Merge(baseJ, J(JsonLite.Serialize(baseJ)), release);
            Check("Gop module.json: may chua sua -> lay nguyen ban phat hanh",
                JsonLite.Serialize(same) == JsonLite.Serialize(release));

            // May: go nut y, them nut u_abc (nut +), doi mau tab, them nhom Rieng
            JsonObject local = J("{\"title\":\"A\",\"accentColor\":\"#F00\",\"groups\":[\"In\",\"Rieng\"],\"tools\":[" +
                                 "{\"id\":\"gro\",\"locked\":true,\"name\":\"GRO\"},{\"id\":\"x\",\"name\":\"X sua tay\"}," +
                                 "{\"id\":\"u_abc\",\"name\":\"Lisp may\",\"category\":\"Rieng\"}]}");
            JsonObject m = ModuleJsonMerge.Merge(baseJ, local, release);
            Check("Gop module.json: nut ban phat hanh thang, nut may tu them giu o cuoi, nut may da go khong hien lai",
                ToolIds(m) == "gro,x,z,u_abc", ToolIds(m));
            Check("Gop module.json: noi dung nut chung lay theo ban phat hanh",
                ModuleJsonEditor.FindTool(m, "x").GetString("name") == "X moi");
            Check("Gop module.json: mau tab may da doi thi giu, tieu de may chua doi thi lay ban moi",
                m.GetString("accentColor") == "#F00" && m.GetString("title") == "A2");
            Check("Gop module.json: groups lay hop",
                string.Join(",", ModuleJsonEditor.GetGroups(m)) == "In,Dim,Rieng");

            // Chua co ban goc (lan cap nhat dau tien): nut la tren may coi la nut may tu them
            JsonObject noBase = ModuleJsonMerge.Merge(null, local, release);
            Check("Gop module.json: chua co ban goc -> giu nut la, khong xoa nut nao cua ban moi",
                ToolIds(noBase) == "gro,x,y,z,u_abc", ToolIds(noBase));
            Check("Gop module.json: chua co ban goc -> tieu de / mau lay theo ban moi",
                noBase.GetString("accentColor") == "#111");

            JsonObject lockedGone = ModuleJsonMerge.Merge(baseJ, J("{\"tools\":[]}"), release);
            Check("Gop module.json: nut locked luon con du may da mat",
                ModuleJsonEditor.FindTool(lockedGone, "gro") != null && ModuleJsonEditor.FindTool(lockedGone, "x") == null);
        }

        // Tao 1 ban phat hanh gia trong thu muc (file + manifest)
        private static void MakeRelease(string dir, string version, Dictionary<string, string> files)
        {
            var m = new ReleaseManifest();
            files = new Dictionary<string, string>(files) { [UpdateVersion.FileName] = version + "\n" };
            foreach (var kv in files)
            {
                string f = Path.Combine(dir, kv.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(f));
                File.WriteAllText(f, kv.Value, new UTF8Encoding(false));
                m.Files[kv.Key] = ReleaseManifest.Sha256OfFile(f);
            }
            string mp = Path.Combine(dir, ReleaseManifest.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(mp));
            File.WriteAllText(mp, m.Serialize(), new UTF8Encoding(false));
        }

        private static void CopyTree(string from, string to)
        {
            foreach (string f in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
            {
                string dst = Path.Combine(to, f.Substring(from.Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(f, dst, true);
            }
        }

        private static string Read(string root, string rel) =>
            File.Exists(Path.Combine(root, rel)) ? File.ReadAllText(Path.Combine(root, rel)) : null;

        private static void TestUpdateInstallAndRollback()
        {
            string tmp = Path.Combine(Path.GetTempPath(), "ohta_upd_" + Guid.NewGuid().ToString("N"));
            string root = Path.Combine(tmp, "OHTA_TOOL");
            string v1 = Path.Combine(tmp, "v1");
            string v2 = Path.Combine(tmp, "v2");
            string state = Path.Combine(tmp, "state");
            string backup = Path.Combine(state, "backup", "OHTA_TOOL_backup.zip");
            try
            {
                string moduleV1 = "{\"title\":\"G\",\"tools\":[{\"id\":\"gro\",\"name\":\"GRO\"}]}";
                MakeRelease(v1, "1.1.0", new Dictionary<string, string>
                {
                    ["OHTA_Loader.lsp"] = "loader1",
                    ["03_Guro/module.json"] = moduleV1,
                    ["03_Guro/lisp/bo_di.lsp"] = "se bi xoa",
                    ["03_Guro/lisp/sua_tay.lsp"] = "ban goc"
                });
                // Cai tay v1 (giai nen zip) + ghi ban goc module.json nhu khi cai bang trinh cap nhat
                CopyTree(v1, root);
                Directory.CreateDirectory(UpdateInstaller.BaseStoreDirectory(state));
                File.WriteAllText(Path.Combine(UpdateInstaller.BaseStoreDirectory(state), "03_Guro.module.json"), moduleV1);

                // May: them lisp bang nut +, sua tay 1 file cua ban phat hanh, khoa google sheet
                File.WriteAllText(Path.Combine(root, "03_Guro/lisp/cua_may.lsp"), "lisp may");
                File.WriteAllText(Path.Combine(root, "03_Guro/lisp/sua_tay.lsp"), "may da sua");
                ModuleJsonEditor.Update(Path.Combine(root, "03_Guro"),
                    j => ModuleJsonEditor.AddTool(j, new ToolItem { Id = "u_may", Name = "May", Command = "MAY", LispFile = "lisp/cua_may.lsp" }));

                MakeRelease(v2, "1.2.0", new Dictionary<string, string>
                {
                    ["OHTA_Loader.lsp"] = "loader2",
                    ["03_Guro/module.json"] = "{\"title\":\"G\",\"tools\":[{\"id\":\"gro\",\"name\":\"GRO 2\"},{\"id\":\"gi\",\"name\":\"GI\"}]}",
                    ["03_Guro/lisp/moi.lsp"] = "moi",
                    ["03_Guro/lisp/sua_tay.lsp"] = "ban goc"
                });

                UpdateReport rep = UpdateInstaller.Install(root, v2, backup, UpdateInstaller.BaseStoreDirectory(state));
                JsonObject mj = ModuleJsonEditor.Load(Path.Combine(root, "03_Guro"));
                Check("Cai dat: VERSION + file moi + ghi de file cua ban phat hanh",
                    UpdateVersion.ReadInstalled(root) == "1.2.0" && Read(root, "OHTA_Loader.lsp") == "loader2" &&
                    Read(root, "03_Guro/lisp/moi.lsp") == "moi");
                Check("Cai dat: xoa file ban moi da bo (may chua sua)", Read(root, "03_Guro/lisp/bo_di.lsp") == null);
                Check("Cai dat: khong dung file may tu them", Read(root, "03_Guro/lisp/cua_may.lsp") == "lisp may");
                Check("Cai dat: gop module.json giu nut may tu them",
                    ToolIds(mj) == "gro,gi,u_may" && rep.Merged == 1, ToolIds(mj));
                Check("Cai dat: manifest tren may la cua ban moi",
                    ReleaseManifest.LoadFrom(root).Files.ContainsKey("03_Guro/lisp/moi.lsp"));

                Check("Sao luu: ghi phien ban cu", UpdateInstaller.ReadBackupVersion(backup) == "1.1.0");
                UpdateInstaller.Rollback(root, backup);
                Check("Quay lai: tra ve VERSION, file va module.json cu cua may",
                    UpdateVersion.ReadInstalled(root) == "1.1.0" && Read(root, "OHTA_Loader.lsp") == "loader1" &&
                    Read(root, "03_Guro/lisp/bo_di.lsp") == "se bi xoa" && Read(root, "03_Guro/lisp/sua_tay.lsp") == "may da sua" &&
                    ToolIds(ModuleJsonEditor.Load(Path.Combine(root, "03_Guro"))) == "gro,u_may");
                Check("Quay lai: xoa file lan cap nhat da them, giu file may tu them",
                    Read(root, "03_Guro/lisp/moi.lsp") == null && Read(root, "03_Guro/lisp/cua_may.lsp") == "lisp may");
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }

        private static void TestUpdateInstallRejectsBadHash()
        {
            string tmp = Path.Combine(Path.GetTempPath(), "ohta_upd_bad_" + Guid.NewGuid().ToString("N"));
            string root = Path.Combine(tmp, "OHTA_TOOL");
            string v2 = Path.Combine(tmp, "v2");
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "OHTA_Loader.lsp"), "loader1");
                MakeRelease(v2, "1.2.0", new Dictionary<string, string> { ["OHTA_Loader.lsp"] = "loader2" });
                File.WriteAllText(Path.Combine(v2, "OHTA_Loader.lsp"), "bi sua sau khi dong goi");

                bool threw = false;
                try { UpdateInstaller.Install(root, v2, Path.Combine(tmp, "b.zip"), Path.Combine(tmp, "base")); }
                catch (InvalidDataException) { threw = true; }
                Check("Cai dat: sai sha256 -> dung lai, khong dung file nao tren may",
                    threw && Read(root, "OHTA_Loader.lsp") == "loader1" && !File.Exists(Path.Combine(root, "VERSION")));
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }
    }
}
