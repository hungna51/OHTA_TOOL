using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
#endif

namespace ZwCadPlugin.CadServices
{
    public static class CadViewService
    {
        /// <summary>In một dòng ra dòng lệnh CAD (ASCII thuần — dòng lệnh ZWCAD không chắc hiện được dấu).</summary>
        public static void GhiDongLenh(string dong)
        {
            try
            {
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage("\n" + dong);
            }
            catch
            {
                // Chi la thong bao phu - khong de loi o day lam hong thao tac chinh
            }
        }

        /// <summary>Đường dẫn đầy đủ bản vẽ hiện hành (khoá phân biệt bản vẽ của palette, mục 114). Lỗi -> null.</summary>
        public static string TenBanVeHienHanh()
        {
            try
            {
                return Application.DocumentManager.MdiActiveDocument?.Name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Zoom tới một hộp (toạ độ WCS). lePhan = phần nới thêm so với kích thước hộp
        /// (2.0 = khung nhìn rộng gấp 3 lần hộp).
        ///
        /// Bản cũ đặt thẳng CenterPoint = tâm hộp theo WCS, nhưng CenterPoint của
        /// ViewTableRecord tính trong hệ MÀN HÌNH (DCS) — lệch đúng bằng Target của khung nhìn
        /// (và xoay theo ViewTwist). Bản vẽ nào có Target khác gốc là zoom trượt chỗ khác.
        /// Nay đổi hộp sang DCS bằng ma trận chuẩn trước khi đặt.
        ///
        /// Sau khi đặt thì ĐỌC LẠI khung nhìn: không đổi (ZWCAD bỏ qua SetCurrentView khi gọi
        /// từ palette) thì dùng lệnh ZOOM Window làm dự phòng.
        /// </summary>
        public static void ZoomToExtents(Extents3d ext, double lePhan = 0.15)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            double w = Math.Abs(ext.MaxPoint.X - ext.MinPoint.X);
            double h = Math.Abs(ext.MaxPoint.Y - ext.MinPoint.Y);
            if (w <= 0 && h <= 0) { w = 100.0; h = 100.0; }
            if (w <= 0) w = h;
            if (h <= 0) h = w;

            bool daDoi = false;
            try
            {
                using (doc.LockDocument())
                using (ViewTableRecord view = ed.GetCurrentView())
                {
                    // WCS -> DCS (cong thuc chuan: PlaneToWorld(huong nhin), dich theo Target, xoay ViewTwist)
                    Matrix3d dcsToWcs = Matrix3d.PlaneToWorld(view.ViewDirection);
                    dcsToWcs = Matrix3d.Displacement(view.Target - Point3d.Origin) * dcsToWcs;
                    dcsToWcs = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * dcsToWcs;
                    Extents3d dcs = ext;
                    dcs.TransformBy(dcsToWcs.Inverse());

                    double dw = Math.Max(Math.Abs(dcs.MaxPoint.X - dcs.MinPoint.X), w * 1e-3);
                    double dh = Math.Max(Math.Abs(dcs.MaxPoint.Y - dcs.MinPoint.Y), h * 1e-3);
                    var tam = new Point2d((dcs.MinPoint.X + dcs.MaxPoint.X) / 2.0,
                                          (dcs.MinPoint.Y + dcs.MaxPoint.Y) / 2.0);

                    double tiLe = view.Height > 0 ? view.Width / view.Height : 1.0;
                    if (tiLe <= 0) tiLe = 1.0;
                    double rong, cao;
                    if (dw / dh > tiLe) { rong = dw * (1.0 + lePhan); cao = rong / tiLe; }
                    else { cao = dh * (1.0 + lePhan); rong = cao * tiLe; }

                    view.CenterPoint = tam;
                    view.Width = rong;
                    view.Height = cao;
                    ed.SetCurrentView(view);

                    using (ViewTableRecord sau = ed.GetCurrentView())
                    {
                        daDoi = sau.CenterPoint.GetDistanceTo(tam) <= Math.Max(rong, cao) * 0.05 &&
                                Math.Abs(sau.Height - cao) <= cao * 0.05;
                    }
                }
                ed.UpdateScreen();
                Application.UpdateScreen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DST] Loi zoom bang khung nhin: {ex.Message} -> dung lenh ZOOM.");
            }

            if (!daDoi) ZoomBangLenh(doc, ext, lePhan);
        }

        /// <summary>
        /// Dự phòng: lệnh ZOOM Window qua dòng lệnh. Dấu ' = lệnh trong suốt (chạy được cả khi
        /// đang có lệnh khác). Điểm gõ vào hiểu theo UCS nên phải đổi WCS -> UCS trước.
        /// </summary>
        private static void ZoomBangLenh(Document doc, Extents3d ext, double lePhan)
        {
            try
            {
                double w = ext.MaxPoint.X - ext.MinPoint.X, h = ext.MaxPoint.Y - ext.MinPoint.Y;
                double nx = w * lePhan / 2.0, ny = h * lePhan / 2.0;
                Matrix3d wcsToUcs = doc.Editor.CurrentUserCoordinateSystem.Inverse();
                Point3d a = new Point3d(ext.MinPoint.X - nx, ext.MinPoint.Y - ny, 0).TransformBy(wcsToUcs);
                Point3d b = new Point3d(ext.MaxPoint.X + nx, ext.MaxPoint.Y + ny, 0).TransformBy(wcsToUcs);
                string Diem(Point3d p) => p.X.ToString("F4", CultureInfo.InvariantCulture) + "," +
                                          p.Y.ToString("F4", CultureInfo.InvariantCulture);
                doc.SendStringToExecute("'_.ZOOM _W " + Diem(a) + " " + Diem(b) + " ", true, false, false);
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\n[DST] Khong zoom duoc: {ex.Message}");
            }
        }

        /// <summary>
        /// Mục 134: chọn (bật grip) + zoom tới các thực thể theo handle (chuỗi hex lisp ghi trong
        /// báo cáo GRO). Bỏ qua handle không còn / đã xoá. Trả về số thực thể tìm thấy.
        /// </summary>
        public static int ChonVaZoomTheoHandle(IEnumerable<string> handles, double lePhan = 0.5)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || handles == null) return 0;
            Database db = doc.Database;
            var ids = new List<ObjectId>();
            Extents3d? hop = null;
            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (string h in handles)
                    {
                        if (!long.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long so)) continue;
                        if (!db.TryGetObjectId(new Handle(so), out ObjectId id) || id.IsNull || id.IsErased) continue;
                        var ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (ent == null) continue;
                        ids.Add(id);
                        try
                        {
                            Extents3d e = ent.GeometricExtents;
                            if (hop.HasValue) { var g = hop.Value; g.AddExtents(e); hop = g; }
                            else hop = e;
                        }
                        catch
                        {
                            // Thuc the khong co extents (vd text rong) -> van chon, chi khong tinh vao hop zoom
                        }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                GhiDongLenh("[DST] Loi tim doi tuong theo handle: " + ex.Message);
                return 0;
            }
            if (hop.HasValue) ZoomToExtents(hop.Value, lePhan);
            if (ids.Count > 0) HighlightEntities(ids);
            return ids.Count;
        }

        /// <summary>
        /// Bật sáng Grips (SetImpliedSelection) cho các thực thể để người dùng nhận diện ngay trên CAD
        /// </summary>
        public static void HighlightEntities(IEnumerable<ObjectId> ids)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var validIds = ids?.Where(id => !id.IsNull && id.IsValid && !id.IsErased).ToArray();
                using (doc.LockDocument())
                {
                    ed.SetImpliedSelection(validIds != null && validIds.Length > 0 ? validIds : new ObjectId[0]);
                }
            }
            catch
            {
                // Bỏ qua lỗi selection nếu CAD đang ở trạng thái command khác
            }
        }
    }
}
