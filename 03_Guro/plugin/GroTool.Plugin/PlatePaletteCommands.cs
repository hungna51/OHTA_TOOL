using System;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
#endif
using ZwCadPlugin.Core;
using ZwCadPlugin.ViewModels;

[assembly: ExtensionApplication(typeof(ZwCadPlugin.PlatePaletteCommands))]
[assembly: CommandClass(typeof(ZwCadPlugin.PlatePaletteCommands))]
[assembly: CommandClass(typeof(ZwCadPlugin.DemoCommands))]

namespace ZwCadPlugin
{
    /// <summary>
    /// Quản lý các lệnh điều hướng vào Tab Guro hợp nhất trên bảng OHTA TOOL (Home | Tấm | Error).
    /// Bỏ hoàn toàn PaletteSet rời của DST trước đây.
    /// </summary>
    public class PlatePaletteCommands : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage("\n[GROTOOL] Da nap GROTOOL.dll (Tab Guro hop nhat Home / Tam / Error + lenh KT).\n");
            }
            catch
            {
            }
        }

        public void Terminate()
        {
            KtCommands.DongKhiThoatCad();
            CadServices.CloudLoiCad.Xoa();
        }

        [CommandMethod("TESTNET")]
        public static void TestNetCommand()
        {
            Application.ShowAlertDialog("GROTOOL.dll da nap va hoat dong tren OHTA TOOL!");
        }

        [LispFunction("GRO_NET_HOME")]
        public static object LispShowHomeTab(ResultBuffer rb)
        {
            try
            {
                return ShowGuroSubTab(GuroTabViewModel.TabHome, lamMoi: false) ? (object)true : null;
            }
            catch (System.Exception ex)
            {
                OhtaBugBridge.Log("GRO/PlatePaletteCommands", ex);
                return null;
            }
        }

        [LispFunction("GRO_NET_DST")]
        [LispFunction("GROPALETTE")]
        public static object LispShowPlatePalette(ResultBuffer rb)
        {
            try
            {
                return ShowGuroSubTab(GuroTabViewModel.TabTam, lamMoi: true) ? (object)true : null;
            }
            catch (System.Exception ex)
            {
                OhtaBugBridge.Log("GRO/PlatePaletteCommands", ex);
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n[LispFunction] Loi mo tab Tam: {ex.Message}");
                return null;
            }
        }

        [LispFunction("GRO_NET_DSN")]
        public static object LispShowDsnPalette(ResultBuffer rb)
        {
            try
            {
                return ShowGuroSubTab(GuroTabViewModel.TabError, lamMoi: true) ? (object)true : null;
            }
            catch (System.Exception ex)
            {
                OhtaBugBridge.Log("GRO/PlatePaletteCommands", ex);
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n[LispFunction] Loi mo tab Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>Báo cáo cuối lệnh GRO -> cập nhật Home + Error + Tấm trên tab Guro (ở lại Home xem báo cáo).</summary>
        [LispFunction("GRO_NET_THONG_TIN")]
        public static object LispThongTin(ResultBuffer rb)
        {
            try
            {
                string baoCao = string.Empty;
                if (rb != null)
                {
                    foreach (TypedValue tv in rb)
                    {
                        if (tv.Value is string s) { baoCao = s; break; }
                    }
                }
                return ShowGuroWithReport(baoCao) ? (object)true : null;
            }
            catch (System.Exception ex)
            {
                OhtaBugBridge.Log("GRO/PlatePaletteCommands", ex);
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n[LispFunction] Loi dua bao cao vao tab Guro: {ex.Message}");
                return null;
            }
        }

        [LispFunction("GRO_NET_KHAC_CHU")]
        public static object LispKhacChu(ResultBuffer rb)
        {
            try
            {
                string ghiChu = string.Empty;
                if (rb != null)
                {
                    foreach (TypedValue tv in rb)
                    {
                        if (tv.Value is string s) { ghiChu = s; break; }
                    }
                }
                return JapanNoteTranslator.LaKhacChu(ghiChu) ? (object)true : null;
            }
            catch (System.Exception ex)
            {
                OhtaBugBridge.Log("GRO/PlatePaletteCommands", ex);
                return null;
            }
        }

        [LispFunction("GRO_NET_XU_LY_SHT")]
        public static object LispXuLySht(ResultBuffer rb)
        {
            try
            {
                string ghiChu = string.Empty;
                if (rb != null)
                {
                    foreach (TypedValue tv in rb)
                    {
                        if (tv.Value is string s) { ghiChu = s; break; }
                    }
                }
                return JapanNoteTranslator.LaXuLySht(ghiChu) ? (object)true : null;
            }
            catch (System.Exception ex)
            {
                OhtaBugBridge.Log("GRO/PlatePaletteCommands", ex);
                return null;
            }
        }

        [CommandMethod("GROSETTING")]
        [CommandMethod("NOTE_SETTING")]
        public static void ShowNoteSettingCommand()
        {
            OhtaBugBridge.Run("GROSETTING", () =>
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                try
                {
                    var dlg = new Views.NoteDictionaryWindow();
                    Application.ShowModalWindow(dlg);
                }
                catch (System.Exception ex)
                {
                    OhtaBugBridge.Log("GRO/PlatePaletteCommands", ex);
                    doc?.Editor?.WriteMessage($"\n[GROSETTING] Loi mo cai dat ghi chu: {ex.Message}");
                }
            });
        }

        [CommandMethod("GROPALETTE")]
        [CommandMethod("DST")]
        [CommandMethod("GRODS")]
        [CommandMethod("DS")]
        [CommandMethod("DSTON")]
        public static void CmdShowDst()
        {
            OhtaBugBridge.Run("DST", () =>
            {
                ShowGuroSubTab(GuroTabViewModel.TabTam, lamMoi: true);
            });
        }

        [CommandMethod("DSN")]
        [CommandMethod("XLNDS")]
        public static void CmdShowDsn()
        {
            OhtaBugBridge.Run("DSN", () =>
            {
                ShowGuroSubTab(GuroTabViewModel.TabError, lamMoi: true);
            });
        }

        [CommandMethod("GROKIEMTRA")]
        public static void CmdGroKiemTra()
        {
            OhtaBugBridge.Run("GROKIEMTRA", () => GuroTabViewModel.HienTai?.ChayKiemTraTuLenh(tuNguoiDung: true));
        }

        [CommandMethod("GROKIEMTRA_AUTO")]
        public static void CmdGroKiemTraAuto()
        {
            OhtaBugBridge.Run("GROKIEMTRA", () => GuroTabViewModel.HienTai?.ChayKiemTraTuLenh(tuNguoiDung: false));
        }

        private static string FileThongTin() =>
            System.IO.Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? System.IO.Path.GetTempPath(), "gro_thong_tin.txt");

        [CommandMethod("GROTHONGTIN")]
        public static void CmdThongTin()
        {
            OhtaBugBridge.Run("GROTHONGTIN", () =>
            {
                string baoCao = null;
                string f = FileThongTin();
                try
                {
                    if (System.IO.File.Exists(f))
                    {
                        baoCao = System.IO.File.ReadAllText(f, System.Text.Encoding.UTF8).TrimEnd('\r', '\n');
                        System.IO.File.Delete(f);
                    }
                }
                catch { }
                if (baoCao != null)
                    ShowGuroWithReport(baoCao);
                else
                    ShowGuroSubTab(GuroTabViewModel.TabHome, lamMoi: true);
            });
        }

        public static bool ShowGuroSubTab(int subTabIndex, bool lamMoi)
        {
            OhtaBugBridge.MoTabGuro();
            GuroTabViewModel vm = GuroTabViewModel.HienTai;
            if (vm == null) return false;
            vm.MoTabCon(subTabIndex, lamMoi);
            return true;
        }

        public static bool ShowGuroWithReport(string baoCao)
        {
            OhtaBugBridge.MoTabGuro();
            GuroTabViewModel vm = GuroTabViewModel.HienTai;
            if (vm == null) return false;
            var doc = Application.DocumentManager.MdiActiveDocument;
            string ten = null;
            try { ten = System.IO.Path.GetFileName(doc?.Name); } catch { }
            vm.NhanBaoCao(baoCao ?? string.Empty, ten);
            return true;
        }
    }
}
