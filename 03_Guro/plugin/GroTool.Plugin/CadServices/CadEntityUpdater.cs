using System;
using System.Collections.Generic;
using System.Linq;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
#endif
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.CadServices
{
    public static class CadEntityUpdater
    {
        /// <summary>
        /// Ghi các dòng ĐÃ SỬA của bảng ngược vào bản vẽ: TEXT bango, MTEXT mã khắc và các
        /// dòng F-name (N= T= Q= M=). Gọi SAU khi BangSoLieu.KiemTraTruocKhiLuu không còn lỗi.
        ///
        /// Mã khắc và N= chỉ thay ĐOẠN SỐ CUỐI (mục 86): bản cũ thay mọi "-số" nên
        /// "I-1-3" thành "I-5-5", và thay cụm số ĐẦU sau N= nên tên bản vẽ bắt đầu bằng số bị
        /// hỏng. Chỉ ghi các ô THẬT SỰ bị sửa; ô khác giữ nguyên. CSV do ViewModel ghi.
        /// </summary>
        /// <param name="canhBao">Những chỗ không sửa được (định dạng lạ, đối tượng đã xoá...).</param>
        /// <returns>Số tấm đã ghi.</returns>
        public static int SavePlateChanges(IEnumerable<PlateItem> plates, out List<string> canhBao)
        {
            canhBao = new List<string>();
            var sua = plates.Where(p => p.IsModified && !p.IsOrphan).ToList();
            if (sua.Count == 0) return 0;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return 0;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            int count = 0;

            DocumentLock docLock = null;
            try { docLock = doc.LockDocument(); }
            catch { /* dang trong ngu canh lenh: da khoa san */ }

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (PlateItem p in sua)
                    {
                        string ten = "Tấm " + p.GocBango;
                        // null = o KHONG bi sua -> giu nguyen tung ky tu tren ban ve.
                        string bango = p.DaSuaBango ? (p.Bango ?? string.Empty).Trim() : null;
                        string t = p.DaSuaThickness
                            ? (BangSoLieu.ChuanHoaT(p.Thickness) ?? (p.Thickness ?? string.Empty).Trim())
                            : null;
                        string q = p.DaSuaQuantity ? (p.Quantity ?? string.Empty).Trim() : null;
                        string m = p.DaSuaMaterial ? (p.Material ?? string.Empty).Trim() : null;

                        if (bango != null)
                        {
                            // 1. TEXT bango (layer "0")
                            if (MoGhi(tr, p.BangoTextId) is DBText txt)
                                txt.TextString = bango;
                            else if (!p.BangoTextId.IsNull)
                                canhBao.Add(ten + ": TEXT bango không còn trên bản vẽ.");

                            // 2. Mã khắc (layer "3-KEGAKI") - chi doan so cuoi
                            DBObject mk = MoGhi(tr, p.MaKhacTextId);
                            if (mk is MText mmk)
                                mmk.Contents = ThayHoacBao(mmk.Contents, bango, ten + ": mã khắc", canhBao);
                            else if (mk is DBText tmk)
                                tmk.TextString = ThayHoacBao(tmk.TextString, bango, ten + ": mã khắc", canhBao);
                        }

                        // 3. F-name (layer "2-F-NAME"): TEXT tung dong (GRO) hoac MTEXT \P (FN cu)
                        foreach (ObjectId id in p.FnameTextIds)
                        {
                            DBObject o = MoGhi(tr, id);
                            if (o is DBText fn)
                                fn.TextString = BangSoLieu.VietLaiDong(fn.TextString, bango, t, q, m);
                            else if (o is MText mfn)
                                mfn.Contents = BangSoLieu.NoiDongMText(
                                    BangSoLieu.TachDongMText(mfn.Contents)
                                              .Select(d => BangSoLieu.VietLaiDong(d, bango, t, q, m)));
                        }
                        count++;
                    }
                    tr.Commit();
                }

                // Chi SAU khi commit thanh cong moi dua bang ve trang thai "khop".
                // Hien dung gia tri da ghi (T chuan hoa "6" -> "6.0").
                foreach (PlateItem p in sua)
                {
                    if (p.DaSuaThickness)
                        p.Thickness = BangSoLieu.ChuanHoaT(p.Thickness) ?? p.Thickness;
                    p.DanhDauDaKhop();
                }
                ed.Regen();
                ed.WriteMessage($"\n[DST] Da ghi {count} tam vao ban ve.");
            }
            catch (System.Exception ex)
            {
                canhBao.Add("Lỗi khi ghi bản vẽ: " + ex.Message + " (không có thay đổi nào được ghi).");
                ed.WriteMessage($"\n[DST] Loi ghi ban ve: {ex.Message}\n");
                count = 0;
            }
            finally
            {
                docLock?.Dispose();
            }
            return count;
        }

        private static DBObject MoGhi(Transaction tr, ObjectId id)
        {
            if (id.IsNull || !id.IsValid || id.IsErased) return null;
            return tr.GetObject(id, OpenMode.ForWrite);
        }

        private static string ThayHoacBao(string goc, string bango, string ten, List<string> canhBao)
        {
            if (!GroText.TryDocBangoCuoi(goc ?? string.Empty, out _))
            {
                canhBao.Add(ten + " \"" + goc + "\" không kết thúc bằng -<số>, giữ nguyên.");
                return goc;
            }
            return GroText.ThayBangoCuoi(goc, bango);
        }
    }
}
