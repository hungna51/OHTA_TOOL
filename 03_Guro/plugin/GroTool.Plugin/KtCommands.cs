using System;
using System.Collections.Generic;
using System.Windows;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif
using ZwCadPlugin.CadServices;
using ZwCadPlugin.ViewModels;
using ZwCadPlugin.Views;

[assembly: CommandClass(typeof(ZwCadPlugin.KtCommands))]

namespace ZwCadPlugin
{
    /// <summary>
    /// Lệnh KT (Kiểm tra kín hở &amp; thuộc tính ByLayer cho vùng quét chọn, đặt nút trong tab More).
    /// Yêu cầu người dùng quét chọn đối tượng trên bản vẽ trước, rồi mở cửa sổ nổi độc lập (modeless), KHÔNG mở sang tab Guro.
    /// </summary>
    public class KtCommands
    {
        private static KtWindow _win;
        private static KtViewModel _vm;
        private static Rect? _viTriCuaSo;
        private static bool _daMocDoiBanVe;

        [CommandMethod("KT", CommandFlags.UsePickSet)]
        public static void CmdKt()
        {
            OhtaBugBridge.Run("KT", () => ChonVaMo(false));
        }

        [CommandMethod("KTCHONLAI")]
        public static void CmdKtChonLai()
        {
            OhtaBugBridge.Run("KT", () => ChonVaMo(true));
        }

        [LispFunction("GRO_NET_KT")]
        public static object LispKt(ResultBuffer rb)
        {
            try
            {
                ChonVaMo(false);
                return true;
            }
            catch (System.Exception ex)
            {
                OhtaBugBridge.Log("GRO/KtCommands", ex);
                CadApplication.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage("\n[KT] Loi: " + ex.Message);
                return null;
            }
        }

        private static void ChonVaMo(bool tuNutChonLai)
        {
            Document doc = null;
            try { doc = CadApplication.DocumentManager.MdiActiveDocument; } catch { }
            if (doc == null) return;

            if (!_daMocDoiBanVe)
            {
                try
                {
                    CadApplication.DocumentManager.DocumentActivated += (s, e) =>
                    {
                        CloudLoiCad.Xoa();
                        if (_win != null && _win.IsVisible)
                            _vm?.KiemTraLai();
                    };
                    _daMocDoiBanVe = true;
                }
                catch { }
            }

            if (_vm == null) _vm = new KtViewModel();
            _vm.YeuCauChonLai = BatDauChonLai;

            DongCuaSo();
            CloudLoiCad.Xoa();

            Editor ed = doc.Editor;
            PromptSelectionResult res = null;
            if (!tuNutChonLai)
            {
                PromptSelectionResult imp = ed.SelectImplied();
                if (imp != null && imp.Status == PromptStatus.OK && imp.Value != null && imp.Value.Count > 0)
                {
                    res = imp;
                    try { ed.SetImpliedSelection(new ObjectId[0]); } catch { }
                }
            }
            if (res == null)
            {
                var opts = new PromptSelectionOptions { MessageForAdding = "\n[KT] Quet chon doi tuong can kiem tra: " };
                res = ed.GetSelection(opts);
            }

            if (res == null || res.Status != PromptStatus.OK || res.Value == null || res.Value.Count == 0)
            {
                if (res == null || res.Status != PromptStatus.Cancel)
                    ed.WriteMessage("\n[KT] Chua chon doi tuong nao.");
                if (tuNutChonLai && _vm.CoTapChon(doc))
                    HienCuaSo();
                return;
            }

            var handles = new List<long>();
            foreach (ObjectId id in res.Value.GetObjectIds())
            {
                if (!id.IsNull)
                    handles.Add((long)id.Handle.Value);
            }

            _vm.CapNhatTapChon(doc, handles);
            HienCuaSo();
        }

        private static void BatDauChonLai()
        {
            DongCuaSo();
            CloudLoiCad.Xoa();
            try
            {
                CadApplication.DocumentManager.MdiActiveDocument?.SendStringToExecute("\x03\x03KTCHONLAI\n", true, false, false);
            }
            catch { }
        }

        public static void DongKhiThoatCad() => DongCuaSo();

        private static void LuuViTri(Window w)
        {
            if (w == null) return;
            try
            {
                Rect r = w.WindowState == WindowState.Normal
                    ? new Rect(w.Left, w.Top, w.Width, w.Height)
                    : w.RestoreBounds;
                if (!double.IsNaN(r.Left) && !double.IsNaN(r.Top) &&
                    !double.IsInfinity(r.Left) && !double.IsInfinity(r.Top) &&
                    r.Width > 100 && r.Height > 100)
                {
                    _viTriCuaSo = r;
                }
            }
            catch { }
        }

        private static void DongCuaSo()
        {
            if (_win == null) return;
            try
            {
                LuuViTri(_win);
                var cu = _win;
                _win = null;
                cu.Close();
            }
            catch { _win = null; }
        }

        private static void HienCuaSo()
        {
            var w = new KtWindow(_vm);
            if (_viTriCuaSo.HasValue)
            {
                w.WindowStartupLocation = WindowStartupLocation.Manual;
                w.Left = _viTriCuaSo.Value.Left;
                w.Top = _viTriCuaSo.Value.Top;
                w.Width = _viTriCuaSo.Value.Width;
                w.Height = _viTriCuaSo.Value.Height;
            }
            w.Closed += (s, e) =>
            {
                LuuViTri(w);
                if (_win == w) _win = null;
            };
            try { System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(w); } catch { }
            _win = w;
            w.Show();
            w.Activate();
        }
    }
}
