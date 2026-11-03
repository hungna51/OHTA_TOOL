using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
using ZwSoft.ZwCAD.Windows;
using ZwCadPlugin.Core;
using ZwCadPlugin.ViewModels;
using ZwCadPlugin.Views;

[assembly: ExtensionApplication(typeof(ZwCadPlugin.PlatePaletteCommands))]
[assembly: CommandClass(typeof(ZwCadPlugin.PlatePaletteCommands))]
[assembly: CommandClass(typeof(ZwCadPlugin.DemoCommands))]

namespace ZwCadPlugin
{
    public class PlatePaletteCommands : IExtensionApplication
    {
        private static PaletteSet _paletteSet;
        private static PlatePaletteControl _control;
        private static PlatePaletteViewModel _viewModel;
        private static ElementHost _host;
        private static bool _daMocConLan;
        private static bool _daMocDoiBanVe;

        public void Initialize()
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage("\n[GROZWCAD] Da nap GROZWCAD.dll (bang DST + cac buoc GRO chay bang C#). Go DST, GROPALETTE hoac TESTNET." +
                                          "\n[GROZWCAD] Lenh GRO se dung hop thoai .NET (thu rieng bang GRODLGNET).\n");
            }
            catch
            {
                // Tranh throw exception trong Initialize()
            }
        }

        public void Terminate()
        {
            if (_daMocConLan)
            {
                Application.PreTranslateMessage -= BatConLan;
                _daMocConLan = false;
            }
            if (_daMocDoiBanVe)
            {
                try { Application.DocumentManager.DocumentActivated -= DoiBanVe; } catch { }
                _daMocDoiBanVe = false;
            }
            if (_paletteSet != null)
            {
                _paletteSet.Visible = false;
                _paletteSet.Dispose();
                _paletteSet = null;
            }
        }

        /// <summary>
        /// Lenh kiem tra ket noi don gian nhat: TESTNET
        /// Dung de xac nhan DLL da nap va chay duoc 100% tren ZWCAD 2026
        /// </summary>
        [CommandMethod("TESTNET")]
        public static void TestNetCommand()
        {
            Application.ShowAlertDialog("ZwCadPlugin .NET da nap va hoat dong 100% tren ZWCAD 2026!\n\nBan co the dung lenh GROPALETTE hoac DST de mo bang.");
        }

        /// <summary>
        /// Ham LISP mo bang Palette DST truc tiep tu AutoLISP: (gro_net_dst) hoac (gropalette)
        /// </summary>
        [LispFunction("GRO_NET_DST")]
        public static object LispShowPlatePalette(ResultBuffer rb)
        {
            try
            {
                ShowPlatePalette(PlatePaletteViewModel.TabDanhSach);
                return true;
            }
            catch (System.Exception ex)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n[LispFunction] Loi mo Palette DST: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Báo cáo cuối lệnh GRO -> tab THÔNG TIN của palette (thay hộp thoại alert, mục 105):
        /// (gro_net_thong_tin "chuoi bao cao"). Mở palette ở tab Thông Tin, đồng thời làm mới
        /// tab Danh Sách Tấm và Nét Chưa Đổi. Trả T nếu đã hiện; nil -> lisp tự alert như cũ.
        /// </summary>
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
                return ShowPlatePalette(PlatePaletteViewModel.TabThongTin, baoCao) ? (object)true : null;
            }
            catch (System.Exception ex)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n[LispFunction] Loi dua bao cao vao tab Thong Tin: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Mục 157: (gro_net_khac_chu "ghi chu XData bango") -> T nếu ghi chú dịch ra có "khắc chữ"
        /// (đúng bản dịch cột Ghi chú của Danh Sách Tấm), nil nếu không. Lisp dùng để chuyển text
        /// trong hình của tấm khắc chữ sang 3-KEGAKI.
        /// </summary>
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
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n[LispFunction] Loi kiem tra ghi chu khac chu: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// (gro_net_xu_ly_sht "ghi chu XData bango") -> T nếu ghi chú dịch ra có "xử lý SHT", nil nếu không.
        /// Lisp dùng để cảnh báo tấm cần đổi nét sang 4-SHT trong lệnh GRO.
        /// </summary>
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
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n[LispFunction] Loi kiem tra ghi chu xu ly SHT: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// Ham LISP mo bang Palette DSN (Net chua doi) truc tiep tu AutoLISP: (gro_net_dsn)
        /// </summary>
        [LispFunction("GRO_NET_DSN")]
        public static object LispShowDsnPalette(ResultBuffer rb)
        {
            try
            {
                ShowPlatePalette(PlatePaletteViewModel.TabNet);
                return true;
            }
            catch (System.Exception ex)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n[LispFunction] Loi mo Palette DSN: {ex.Message}");
                return null;
            }
        }

        [LispFunction("GROPALETTE")]
        public static object LispGroPalette(ResultBuffer rb)
        {
            return LispShowPlatePalette(rb);
        }

        /// <summary>
        /// Lenh mo cua so cai dat tu dien ghi chu Nhat - Viet: GROSETTING (hoac NOTE_SETTING)
        /// </summary>
        [CommandMethod("GROSETTING")]
        [CommandMethod("NOTE_SETTING")]
        public static void ShowNoteSettingCommand()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            try
            {
                var dlg = new Views.NoteDictionaryWindow();
                Application.ShowModalWindow(dlg);
            }
            catch (System.Exception ex)
            {
                doc?.Editor?.WriteMessage($"\n[GROSETTING] Loi mo cai dat ghi chu: {ex.Message}");
            }
        }

        /// <summary>
        /// Lenh mo bang Palette danh sach tam & net: DST (hoac GRODS, DS, GROPALETTE)
        /// </summary>
        [CommandMethod("GROPALETTE")]
        [CommandMethod("DST")]
        [CommandMethod("GRODS")]
        [CommandMethod("DS")]
        public static void CmdShowDst()
        {
            ShowPlatePalette(PlatePaletteViewModel.TabDanhSach);
        }

        /// <summary>
        /// Ham mo Palette chinh. tabIndex = PlatePaletteViewModel.TabThongTin / TabDanhSach / TabNet.
        /// thongTin != null -> thay bao cao tren tab Thong Tin (bao cao cuoi lenh GRO).
        /// Tra ve true neu palette da hien.
        /// </summary>
        public static bool ShowPlatePalette(int tabIndex = PlatePaletteViewModel.TabDanhSach, string thongTin = null)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;

            try
            {
                if (_paletteSet == null)
                {
                    _paletteSet = new PaletteSet(
                        "Danh Sách Tấm & Nét (DST)",
                        new Guid("7A5D8B12-4C3A-4F89-9A0B-1C2D3E4F5A6C")
                    );

                    _paletteSet.Style = PaletteSetStyles.ShowAutoHideButton |
                                        PaletteSetStyles.ShowCloseButton |
                                        PaletteSetStyles.ShowPropertiesMenu;

                    _paletteSet.MinimumSize = new Size(480, 500);
                    _paletteSet.Size = new Size(490, 680);
                    _paletteSet.Dock = DockSides.Right;

                    _viewModel = new PlatePaletteViewModel();
                    _control = new PlatePaletteControl(_viewModel);

                    // KHONG dung AddVisual: ZWCAD do control theo chieu cao NOI DUNG, nen DataGrid
                    // cao bang ca danh sach, phan duoi bi cat va khong bao gio hien thanh cuon.
                    // ElementHost DOCK FILL ep WPF dung dung kich thuoc palette -> hang "*" cua
                    // Grid co chieu cao huu han -> DataGrid tu cuon.
                    _host = new ElementHost
                    {
                        Dock = System.Windows.Forms.DockStyle.Fill,
                        Child = _control,
                    };
                    _paletteSet.Add("GRO Toolbox", _host);

                    if (!_daMocConLan)
                    {
                        Application.PreTranslateMessage += BatConLan;
                        _daMocConLan = true;
                    }
                }
                else
                {
                    // Cập nhật lại MinimumSize nếu palette đã được khởi tạo từ trước
                    _paletteSet.MinimumSize = new Size(480, 500);
                }

                if (!_daMocDoiBanVe)
                {
                    Application.DocumentManager.DocumentActivated += DoiBanVe;
                    _daMocDoiBanVe = true;
                }

                // Cài mặc định kích thước khi mở: luôn đạt chuẩn tối thiểu 490px để cả 3 tab
                // thẳng hàng trên 1 dòng, không bị chồng lên nhau
                try
                {
                    if (_paletteSet.Size.Width < 490 || _paletteSet.Size.Height < 550)
                    {
                        _paletteSet.Size = new Size(Math.Max(490, _paletteSet.Size.Width), Math.Max(680, _paletteSet.Size.Height));
                    }
                }
                catch { }

                _paletteSet.Visible = true;
                HienRo();

                try
                {
                    if (_paletteSet.Size.Width < 490)
                    {
                        _paletteSet.Size = new Size(490, Math.Max(680, _paletteSet.Size.Height));
                    }
                }
                catch { }

                if (_viewModel != null)
                {
                    // Dat bao cao TRUOC khi quet: quet ban ve lau, bao cao phai co ngay.
                    if (thongTin != null)
                    {
                        string ten = null;
                        try { ten = System.IO.Path.GetFileName(doc?.Name); } catch { }
                        _viewModel.DatThongTin(thongTin, ten);
                    }
                    _viewModel.SelectedTabIndex = tabIndex;
                    _viewModel.RefreshFromDrawing();
                    _viewModel.Dsn.RefreshFromDrawing();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n[Palette] LOI KHI MO PALETTE: {ex.Message}\n{ex.StackTrace}\n");
                Application.ShowAlertDialog("Lỗi khi mở Palette: " + ex.Message + "\n\n" + ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Palette đang dock ở chế độ tự ẩn (auto-hide) thì Visible = true vẫn chỉ là một mép
        /// thu gọn -> cuối lệnh GRO người dùng không thấy bảng. Bung ra (mục 114).
        /// </summary>
        private static void HienRo()
        {
            try
            {
                if (_paletteSet != null && _paletteSet.RolledUp) _paletteSet.RolledUp = false;
            }
            catch
            {
                // Ban ZWCAD khong ho tro RolledUp -> bo qua
            }
        }

        /// <summary>
        /// Đổi bản vẽ hiện hành: palette dùng chung cho mọi bản vẽ, nên phải nạp lại dữ liệu
        /// của bản vẽ mới — không thì bảng vẫn là bản vẽ cũ trong khi Lưu / Làm Mới lại ghi
        /// vào bản vẽ và CSV của bản vẽ mới (mục 114). Còn sửa chưa lưu thì giữ bảng + cảnh báo.
        /// </summary>
        private static void DoiBanVe(object sender, DocumentCollectionEventArgs e)
        {
            try
            {
                if (_paletteSet == null || _viewModel == null || e?.Document == null) return;
                bool quetLai = _viewModel.DoiBanVe();
                // Palette đang tắt: lần mở sau (DST / cuối GRO) luôn quét lại -> không cần quét ở đây
                if (!_paletteSet.Visible || !quetLai) return;
                _viewModel.RefreshFromDrawing();
                _viewModel.Dsn.RefreshFromDrawing();
            }
            catch (System.Exception ex)
            {
                e?.Document?.Editor?.WriteMessage($"\n[Palette] Loi cap nhat khi doi ban ve: {ex.Message}\n");
            }
        }

        /// <summary>File tạm lisp ghi báo cáo cho lệnh GROTHONGTIN — PHẢI khớp gro--file-thong-tin bên lisp.</summary>
        private static string FileThongTin() =>
            System.IO.Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? System.IO.Path.GetTempPath(), "gro_thong_tin.txt");

        /// <summary>
        /// Đường dự phòng của báo cáo cuối GRO (mục 114). Hàm LISP (LispFunction) có thể không có
        /// trong bản vẽ mở SAU khi NETLOAD, còn LỆNH .NET thì dùng chung mọi bản vẽ: lisp ghi
        /// báo cáo ra file tạm (UTF-8) rồi gọi lệnh này.
        /// </summary>
        [CommandMethod("GROTHONGTIN")]
        public static void CmdThongTin()
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
            catch
            {
                // Khong doc duoc file -> van mo bang (khong co bao cao moi)
            }
            ShowPlatePalette(baoCao != null ? PlatePaletteViewModel.TabThongTin : PlatePaletteViewModel.TabDanhSach, baoCao);
        }

        private const int WM_MOUSEWHEEL = 0x020A;

        [StructLayout(LayoutKind.Sequential)]
        private struct DIEM { public int X; public int Y; }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(DIEM p);

        [DllImport("user32.dll")]
        private static extern bool IsChild(IntPtr cha, IntPtr con);

        /// <summary>
        /// Con lan chuot tren bang -> cuon bang. ZWCAD gui WM_MOUSEWHEEL cho cua so dang giu
        /// FOCUS, ma bam mot dong la bang zoom ban ve -> focus thuong nam o ban ve, nen lan
        /// chuot tren bang lai zoom ban ve thay vi cuon danh sach. Bat thong diep o vong lap
        /// cua ZWCAD: con tro nam tren palette (va KHONG bi cua so khac che) thi cuon bang va
        /// nuot thong diep. Ngoai palette thi khong dong vao.
        /// </summary>
        private static void BatConLan(object sender, PreTranslateMessageEventArgs e)
        {
            try
            {
                System.Windows.Interop.MSG m = e.Message;
                if (m.message != WM_MOUSEWHEEL) return;
                if (_paletteSet == null || !_paletteSet.Visible || _host == null || !_host.IsHandleCreated) return;

                IntPtr duoi = WindowFromPoint(new DIEM { X = m.pt_x, Y = m.pt_y });
                if (duoi != _host.Handle && !IsChild(_host.Handle, duoi)) return;

                int delta = (short)(((long)m.wParam >> 16) & 0xFFFF);
                if (delta == 0) return;
                _control?.CuonTheoConLan(delta);
                e.Handled = true;
            }
            catch
            {
                // Khong bao gio de loi o day lam hong vong lap thong diep cua ZWCAD
            }
        }

        /// <summary>
        /// Lenh dong/an bang Palette: DSTON
        /// </summary>
        [CommandMethod("DSTON")]
        public static void TogglePalette()
        {
            if (_paletteSet == null)
            {
                ShowPlatePalette();
            }
            else
            {
                _paletteSet.Visible = !_paletteSet.Visible;
                if (_paletteSet.Visible)
                {
                    try
                    {
                        if (_paletteSet.Size.Width < 490)
                        {
                            _paletteSet.Size = new Size(490, Math.Max(680, _paletteSet.Size.Height));
                        }
                    }
                    catch { }
                    _viewModel?.RefreshFromDrawing();
                }
            }
        }
    }
}
