using System;
using System.Collections.Generic;
using WinReg = Microsoft.Win32;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
using ZwCadPlugin.CadServices;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;
using ZwCadPlugin.ViewModels;
using ZwCadPlugin.Views;

[assembly: CommandClass(typeof(ZwCadPlugin.GroDialogCommands))]

namespace ZwCadPlugin
{
    /// <summary>
    /// Dialog cài đặt của lệnh GRO viết bằng WPF, thay cho DCL gro_dlg (mục 86).
    ///
    /// Lisp gọi:  (gro_net_dialog lay-k vung layer-vung in-pdf use-xln lay-xln auto-dim mau-dim tao-khung [giu-xln]
    ///                            [xep-cum dat-mk])                     -- mục 122 (mục 170 bỏ che-do-mk)
    /// Trả về:
    ///   danh sách 11 giá trị       -> OK, cùng thứ tự với (nth 0..10 dlg) của c:gro
    ///   nil                        -> Cancel
    ///   ("XLN_DS" + 9 giá trị)     -> người dùng bấm "Xem DSN"; lisp mở danh sách nét
    ///                                 (dữ liệu nằm bên lisp) rồi gọi lại với 9 giá trị đó
    ///   "LOI"                      -> dialog .NET hỏng; lisp chuyển sang DCL
    ///
    /// Đọc/ghi registry vẫn do c:gro làm như cũ — hàm này không đụng registry.
    /// </summary>
    public class GroDialogCommands
    {
        private const string LoiNhacPick = "\nChon 1 net Dim mau (LINE / PLINE / ARC) tren ban ve: ";

        [LispFunction("GRO_NET_DIALOG")]
        public static object LispGroDialog(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;
            try
            {
                GroSettings mac = GroThamSo.DocThamSo(DocResultBuffer(rb), out bool giuXln);
                KetThucDialog kt = HienDialog(doc, mac, giuXln, out GroSettings kq);
                switch (kt)
                {
                    case KetThucDialog.Ok:
                        return MaHoa(kq, null);
                    case KetThucDialog.XemDsn:
                        return MaHoa(kq, GroThamSo.NhanXemDsn);
                    default:
                        return null;
                }
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage("\n[GRO .NET] Loi hop thoai: " + ex.Message +
                                 "\n  -> Chuyen sang hop thoai DCL cu.");
                return GroThamSo.NhanLoi;
            }
        }

        /// <summary>
        /// Lệnh thử dialog ngoài GRO: đọc mặc định từ registry như c:gro, hiện dialog,
        /// in kết quả ra dòng lệnh. KHÔNG ghi registry, KHÔNG đánh số.
        /// </summary>
        [CommandMethod("GRODLGNET")]
        public static void ThuDialog()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            try
            {
                GroSettings mac = DocRegistry();
                KetThucDialog kt = HienDialog(doc, mac, false, out GroSettings kq);
                if (kt == KetThucDialog.Huy)
                {
                    ed.WriteMessage("\n[GRODLGNET] Cancel.");
                    return;
                }
                ed.WriteMessage("\n[GRODLGNET] " + (kt == KetThucDialog.XemDsn ? "Xem DSN" : "OK") +
                                " (KHONG ghi registry):" +
                                "\n  layer khung  = " + (kq.LayerKhung ?? "nil") +
                                "\n  in PDF       = " + YesNo(kq.UseInPdf) +
                                "\n  tao khung    = " + YesNo(kq.UseTaoKhung) +
                                "\n  theo vung    = " + YesNo(kq.UseVung) + "   layer vung = " + (kq.LayerVung ?? "nil") +
                                "\n  xu ly net    = " + YesNo(kq.UseXln) + "   layer goc  = " + (kq.LayerXln ?? "nil") +
                                "\n  auto Dim     = " + YesNo(kq.AutoDim) + "   mau Dim    = " + GroText.TenMau(kq.MauDim) +
                                "\n  xep cum      = " + YesNo(kq.XepCum) +
                                "\n  dat ma khac  = " + YesNo(kq.DatMk) + " (truot goc tren-trai, C#)");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[GRODLGNET] Loi: " + ex.Message);
            }
        }

        private static string YesNo(bool b) => b ? "co" : "khong";

        /// <summary>
        /// Đếm layer một lần, rồi mở dialog. "Pick nét mẫu" phải trả quyền chuột cho bản
        /// vẽ, nên dialog ĐÓNG lại, pick, rồi MỞ LẠI cửa sổ mới trên cùng ViewModel — y như
        /// vòng lặp done_dialog 4 của DCL, chỉ khác là không mất các lựa chọn đang dở.
        /// </summary>
        private static KetThucDialog HienDialog(Document doc, GroSettings mac, bool giuXln, out GroSettings kq)
        {
            LayerQuery.KetQuaDem dem = LayerQuery.Dem(doc.Database);
            List<LayerMuc> dsKhung = LayerRanking.DanhSachKhung(dem.Khung4, dem.TatCaLayer, out bool locKhung);
            List<LayerMuc> dsVung = LayerRanking.DanhSachKhung(dem.Khung3, dem.TatCaLayer, out bool locVung);
            List<LayerMuc> dsXln = LayerRanking.DanhSachXln(dem.DoiTuong, dem.TatCaLayer);
            if (!locKhung)
                doc.Editor.WriteMessage("\nLuu y: khong co polyline KIN 4 dinh nao tren ban ve, nen popup" +
                                        "\n  LAYER KHUNG BAO dang liet ke TAT CA layer. Xem muc 51/52.");
            if (!locVung)
                doc.Editor.WriteMessage("\nLuu y: khong co polyline KIN tu 3 dinh nao tren ban ve, nen popup" +
                                        "\n  LAYER VUNG dang liet ke TAT CA layer. Xem muc 61/64.");

            var vm = new GroSettingsViewModel(dsKhung, locKhung, dsVung, locVung, dsXln, mac, giuXln);
            while (true)
            {
                var w = new GroSettingsWindow(vm);
                Application.ShowModalWindow(w);
                if (w.KetThuc == KetThucDialog.PickNet)
                {
                    PickMauNet(doc, vm);
                    continue;
                }
                kq = vm.KetQua();
                return w.KetThuc;
            }
        }

        private static void PickMauNet(Document doc, GroSettingsViewModel vm)
        {
            Editor ed = doc.Editor;
            PromptEntityResult r = ed.GetEntity(new PromptEntityOptions(LoiNhacPick));
            if (r.Status != PromptStatus.OK) return;
            int c = MauCuaDoiTuong(doc.Database, r.ObjectId);
            vm.DatMauTay(c);
            ed.WriteMessage("\nDa nhan dien mau net Dim: " + GroText.TenMau(c));
        }

        /// <summary>
        /// Màu hiển thị của một đối tượng, như gro--entity-color: ByLayer/ByBlock lấy màu
        /// layer (trị tuyệt đối — layer tắt có màu âm), không đọc được thì 7.
        /// </summary>
        private static int MauCuaDoiTuong(Database db, ObjectId id)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                int c = 7;
                if (ent != null)
                {
                    c = ent.ColorIndex;
                    if (c == 256 || c == 0 || c < 0 || c > 256)
                    {
                        c = 7;
                        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                        if (ent.Layer != null && lt.Has(ent.Layer))
                        {
                            var ltr = (LayerTableRecord)tr.GetObject(lt[ent.Layer], OpenMode.ForRead);
                            int lc = Math.Abs((int)ltr.Color.ColorIndex);
                            if (GroThamSo.LaMauHopLe(lc)) c = lc;
                        }
                    }
                }
                tr.Commit();
                return c;
            }
        }

        // ------------------------------------------------------------------
        // Đổi ResultBuffer <-> object (string / true / null / int / double)
        // ------------------------------------------------------------------

        private static List<object> DocResultBuffer(ResultBuffer rb)
        {
            var ds = new List<object>();
            if (rb == null) return ds;
            foreach (TypedValue tv in rb)
            {
                switch ((LispDataType)tv.TypeCode)
                {
                    case LispDataType.Text: ds.Add(tv.Value as string); break;
                    case LispDataType.T_atom: ds.Add(true); break;
                    case LispDataType.Nil: ds.Add(null); break;
                    case LispDataType.Int16: ds.Add(Convert.ToInt32(tv.Value)); break;
                    case LispDataType.Int32: ds.Add(Convert.ToInt32(tv.Value)); break;
                    case LispDataType.Double: ds.Add(Convert.ToDouble(tv.Value)); break;
                    default: ds.Add(tv.Value); break;   // danh sach / diem: khong dung toi
                }
            }
            return ds;
        }

        private static ResultBuffer MaHoa(GroSettings s, string nhan)
        {
            var rb = new ResultBuffer();
            if (nhan != null) rb.Add(new TypedValue((int)LispDataType.Text, nhan));
            foreach (object o in GroThamSo.KetQua(s))
            {
                switch (o)
                {
                    case null: rb.Add(new TypedValue((int)LispDataType.Nil)); break;
                    case bool b: rb.Add(new TypedValue((int)(b ? LispDataType.T_atom : LispDataType.Nil))); break;
                    case string t: rb.Add(new TypedValue((int)LispDataType.Text, t)); break;
                    case int i: rb.Add(new TypedValue((int)LispDataType.Int16, (short)i)); break;
                    default: throw new InvalidOperationException("Kieu khong ho tro: " + o.GetType().Name);
                }
            }
            return rb;
        }

        // ------------------------------------------------------------------
        // Mặc định cho lệnh thử — đọc đúng khóa và mặc định như c:gro
        // ------------------------------------------------------------------

        private static GroSettings DocRegistry()
        {
            using (WinReg.RegistryKey k = WinReg.Registry.CurrentUser.OpenSubKey(@"Software\OFT_Lisp\GRO"))
            {
                string Doc(string ten) => k?.GetValue(ten) as string;
                bool Co(string ten, bool macDinh)
                {
                    string v = Doc(ten);
                    return v == "1" ? true : v == "0" ? false : macDinh;
                }
                int.TryParse(Doc("mau_dim"), out int mau);
                return new GroSettings
                {
                    UseInPdf = Co("use_in_pdf", false),
                    UseTaoKhung = Co("use_tao_khung", true),
                    UseVung = Co("use_vung", false),
                    LayerVung = Doc("layer_vung"),
                    LayerKhung = Doc("layer_khung"),
                    UseXln = Co("use_xln", true),
                    LayerXln = Doc("layer_xln"),
                    AutoDim = Co("auto_dim", true),
                    MauDim = GroThamSo.LaMauHopLe(mau) ? mau : GroThamSo.MauMacDinh,
                    XepCum = Co("use_xep_cum", true),
                    DatMk = Co("use_dat_mk", true),
                };
            }
        }
    }
}
