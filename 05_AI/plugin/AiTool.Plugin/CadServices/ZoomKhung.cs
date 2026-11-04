using System;
using System.Globalization;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AiTool.CadServices
{
    /// <summary>
    /// Zoom tới một khung (toạ độ WCS), nới mỗi phía 5%. Chép cách làm của GROTOOL
    /// CadViewService.ZoomToExtents (không gọi chéo DLL): đổi hộp sang DCS rồi SetCurrentView;
    /// khung nhìn không đổi (ZWCAD bỏ qua khi gọi từ palette) thì dùng lệnh 'ZOOM W.
    /// </summary>
    public static class ZoomKhung
    {
        private const double LePhan = 0.10;

        public static void Toi(Document doc, double minX, double minY, double maxX, double maxY)
        {
            if (doc == null) return;
            Editor ed = doc.Editor;
            double w = Math.Max(maxX - minX, 1.0), h = Math.Max(maxY - minY, 1.0);
            var ext = new Extents3d(new Point3d(minX, minY, 0), new Point3d(maxX, maxY, 0));

            bool daDoi = false;
            try
            {
                using (doc.LockDocument())
                using (ViewTableRecord view = ed.GetCurrentView())
                {
                    Matrix3d dcsToWcs = Matrix3d.PlaneToWorld(view.ViewDirection);
                    dcsToWcs = Matrix3d.Displacement(view.Target - Point3d.Origin) * dcsToWcs;
                    dcsToWcs = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * dcsToWcs;
                    Extents3d dcs = ext;
                    dcs.TransformBy(dcsToWcs.Inverse());

                    double dw = Math.Max(Math.Abs(dcs.MaxPoint.X - dcs.MinPoint.X), w * 1e-3);
                    double dh = Math.Max(Math.Abs(dcs.MaxPoint.Y - dcs.MinPoint.Y), h * 1e-3);
                    var tam = new Point2d((dcs.MinPoint.X + dcs.MaxPoint.X) / 2.0, (dcs.MinPoint.Y + dcs.MaxPoint.Y) / 2.0);

                    double tiLe = view.Height > 0 ? view.Width / view.Height : 1.0;
                    if (tiLe <= 0) tiLe = 1.0;
                    double rong, cao;
                    if (dw / dh > tiLe) { rong = dw * (1.0 + LePhan); cao = rong / tiLe; }
                    else { cao = dh * (1.0 + LePhan); rong = cao * tiLe; }

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
                CadApplication.UpdateScreen();
            }
            catch (Exception ex)
            {
                ed.WriteMessage("\n[AI] Loi zoom bang khung nhin: " + ex.Message + " -> dung lenh ZOOM.");
            }

            if (!daDoi) ZoomBangLenh(doc, minX, minY, maxX, maxY);
        }

        private static void ZoomBangLenh(Document doc, double minX, double minY, double maxX, double maxY)
        {
            try
            {
                double nx = (maxX - minX) * LePhan / 2.0, ny = (maxY - minY) * LePhan / 2.0;
                Matrix3d wcsToUcs = doc.Editor.CurrentUserCoordinateSystem.Inverse();
                Point3d a = new Point3d(minX - nx, minY - ny, 0).TransformBy(wcsToUcs);
                Point3d b = new Point3d(maxX + nx, maxY + ny, 0).TransformBy(wcsToUcs);
                string Diem(Point3d p) => p.X.ToString("F4", CultureInfo.InvariantCulture) + "," +
                                          p.Y.ToString("F4", CultureInfo.InvariantCulture);
                doc.SendStringToExecute("'_.ZOOM _W " + Diem(a) + " " + Diem(b) + " ", true, false, false);
            }
            catch (Exception ex)
            {
                doc.Editor.WriteMessage("\n[AI] Khong zoom duoc: " + ex.Message);
            }
        }
    }
}
