using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Tests
{
    // Test tab Info: phien ban tung tab, dem lisp/lenh, CHANGELOG, trang thai cap nhat
    internal static partial class Program
    {
        private static void TestInfoTabFeatures()
        {
            var m = new ModuleManifest { Title = "GURO", Version = "1.5" };
            Check("Info: VersionLabel them tien to v", m.VersionLabel == "v1.5", m.VersionLabel);
            Check("Info: DisplayTitle ghep title + phien ban", m.DisplayTitle == "GURO · v1.5", m.DisplayTitle);

            m = new ModuleManifest { Title = "Fname", Version = "v1.1", Stage = "beta" };
            Check("Info: stage hien trong ngoac, khong lap chu v", m.VersionLabel == "v1.1 (beta)", m.VersionLabel);

            m = new ModuleManifest { Title = "More" };
            Check("Info: thieu version -> DisplayTitle = title", m.VersionLabel == "" && m.DisplayTitle == "More", m.DisplayTitle);

            m = new ModuleManifest
            {
                Tools = new List<ToolItem>
                {
                    new ToolItem { Command = "CD", LispFile = "lisp/Catdim_CD_BD.lsp" },
                    new ToolItem { Command = "BD", LispFile = "lisp\\CATDIM_CD_BD.lsp" },
                    new ToolItem { Command = "FMK", LispFile = "./lisp/FnameMoveTextKe.LSP" },
                    new ToolItem { Command = "", LispFile = "" }
                }
            };
            Check("Info: dem lisp khac nhau (khong phan biet hoa thuong, \\ va ./)", ToolInfoCore.CountLispFiles(m) == 2,
                ToolInfoCore.CountLispFiles(m).ToString());
            Check("Info: dem lenh bo nut khong co command", ToolInfoCore.CountCommands(m) == 3);
            Check("Info: DescribeContents", ToolInfoCore.DescribeContents(m) == "2 lisp · 3 lệnh", ToolInfoCore.DescribeContents(m));
            Check("Info: tab khong co tools -> 0", ToolInfoCore.DescribeContents(new ModuleManifest()) == "0 lisp · 0 lệnh");

            string log = "# Nhat ky\r\n\r\nGioi thieu ## khong phai muc\r\n\r\n## v1.2.0\r\n\r\n- Moi A\r\n- Moi B\r\n\r\n## v1.1.0\r\n\r\n- Cu\r\n";
            string notes = ToolInfoCore.LatestChangelogEntry(log, out string ver);
            Check("Info: CHANGELOG lay dung muc dau", ver == "1.2.0" && notes == "- Moi A\n- Moi B", ver + " | " + notes);
            notes = ToolInfoCore.LatestChangelogEntry("# Rong\n## Ghi chu\n- x\n", out ver);
            Check("Info: CHANGELOG khong co muc vX.Y.Z -> rong", notes == "" && ver == "");

            string root = ModuleDiscovery.ResolveRootDirectory(AppContext.BaseDirectory);
            string installed = UpdateVersion.ReadInstalled(root);
            ToolInfoCore.ReadLatestChangelogEntry(root, out ver);
            Check("Info: muc dau CHANGELOG.md thuc te trung VERSION", UpdateVersion.Compare(ver, installed) == 0,
                $"CHANGELOG={ver}, VERSION={installed}");

            List<ModuleManifest> modules = ModuleDiscovery.DiscoverModules(root);
            Check("Info: moi tab thuc te deu khai bao version",
                modules.All(x => !string.IsNullOrWhiteSpace(x.Version)),
                string.Join(", ", modules.Where(x => string.IsNullOrWhiteSpace(x.Version)).Select(x => x.FolderName)));
            Check("Info: title thuc te khong con ghi phien ban (dung truong version)",
                modules.All(x => !System.Text.RegularExpressions.Regex.IsMatch(x.Title ?? "", @"\bv\d")),
                string.Join(", ", modules.Select(x => x.Title)));
            ModuleManifest guro = modules.FirstOrDefault(x => x.Id == "guro");
            Check("Info: tab Guro hien GURO · v<version>",
                guro != null && !string.IsNullOrWhiteSpace(guro.Version) && guro.DisplayTitle == "GURO · v" + guro.Version,
                guro?.DisplayTitle);

            Check("Info: chua kiem tra lan nao",
                ToolInfoCore.DescribeUpdateStatus(new UpdateState(), "1.1.1") == "Chưa kiểm tra bản mới lần nào.");
            var st = new UpdateState { LastCheckDate = "2026-11-04", Latest = new ReleaseInfo { Version = "1.2.0" } };
            Check("Info: co ban moi", ToolInfoCore.DescribeUpdateStatus(st, "1.1.1").EndsWith("có bản mới v1.2.0."));
            Check("Info: dang dung ban moi nhat", ToolInfoCore.DescribeUpdateStatus(st, "1.2.0").EndsWith("đang dùng bản mới nhất."));
            st.Latest = null;
            Check("Info: chua co ban phat hanh", ToolInfoCore.DescribeUpdateStatus(st, "1.1.1").Contains("chưa có bản phát hành"));
        }
    }
}
