using System;
using System.Collections.Generic;
using System.Linq;
using CsvTool.Core;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
#endif

namespace CsvTool.CadServices
{
    /// <summary>
    /// Đọc hình học từng khung cho PhanTichHinh, chỉ đọc Database (không zoom, không tạo đối tượng tạm).
    /// Bỏ qua layer tắt / đóng băng như ssget của lisp cũ.
    /// shortcut: "nằm trong khung" dùng hộp bao của khung, khung không phải chữ nhật cần đổi sang đa giác.
    /// </summary>
    public static class QuetHinhCad
    {
        private const double Eps = 1e-6;
        private const string LopKakou = "0-KAKOU";
        private static readonly HashSet<string> LoaiNet = new HashSet<string> { "LINE", "ARC", "CIRCLE", "ELLIPSE", "SPLINE", "LWPOLYLINE", "POLYLINE" };

        public static List<DuLieuHinh> Doc(Document doc, IReadOnlyList<KhungTho> khung, CaiDatCsv c)
        {
            var ds = khung.Select(k => new DuLieuHinh { HandleKhung = k.Handle }).ToList();
            if (ds.Count == 0) return ds;
            var chuQuanh = new List<ChuHinh>();
            var dan = new List<GhiChuDan>();
            foreach (DuLieuHinh d in ds) { d.ChuQuanh = chuQuanh; d.Dan = dan; }

            Database db = doc.Database;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var lopAn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var netLop = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var lopTrang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ObjectId id in (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead))
                {
                    var l = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (l.IsOff || l.IsFrozen) lopAn.Add(l.Name);
                    if (LaTrang(l.Color)) lopTrang.Add(l.Name);
                    netLop[l.Name] = ((LinetypeTableRecord)tr.GetObject(l.LinetypeObjectId, OpenMode.ForRead)).Name;
                }

                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead))
                {
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Entity e) || !e.Visible || lopAn.Contains(e.Layer)) continue;
                    GhiChuDan gc = DocDan(e, tr);
                    if (gc != null) { dan.Add(gc); continue; }
                    Extents3d hop;
                    try { hop = e.GeometricExtents; } catch { continue; }
                    PhanVaoKhung(e, id.ObjectClass.DxfName, hop, khung, ds, chuQuanh, netLop, lopTrang, c);
                }
                for (int i = 0; i < khung.Count; i++)
                {
                    KhungTho k = khung[i];
                    ds[i].ChuDanTrong.AddRange(dan.Where(g => !string.IsNullOrEmpty(g.NoiDung) &&
                        g.Mui.Any(m => m.X >= k.MinX && m.X <= k.MaxX && m.Y >= k.MinY && m.Y <= k.MaxY)).Select(g => g.NoiDung));
                }
                tr.Commit();
            }
            return ds;
        }

        private static void PhanVaoKhung(Entity e, string loai, Extents3d hop, IReadOnlyList<KhungTho> khung, List<DuLieuHinh> ds,
                                         List<ChuHinh> chuQuanh, Dictionary<string, string> netLop, HashSet<string> lopTrang, CaiDatCsv c)
        {
            bool laKakou = Cung(e.Layer, LopKakou), laFname = Cung(e.Layer, PhanTichKhung.LopFname), laTapu = e is Hatch && Cung(e.Layer, c.LopTapuDung);
            var hopGon = (hop.MinPoint.X, hop.MinPoint.Y, hop.MaxPoint.X, hop.MaxPoint.Y);
            ChuHinh chu = null;
            if (e is MText mt) chu = new ChuHinh { Lop = mt.Layer, NoiDung = mt.Text };
            else if (e is DBText t) chu = new ChuHinh { Lop = t.Layer, NoiDung = t.TextString };
            if (chu != null)
            {
                chu.MinX = hop.MinPoint.X; chu.MinY = hop.MinPoint.Y; chu.MaxX = hop.MaxPoint.X; chu.MaxY = hop.MaxPoint.Y;
                chuQuanh.Add(chu);
            }

            bool hopLe = laKakou && LoaiNet.Contains(loai) && e is Curve;
            string lt = e.Linetype ?? "";
            bool netDut = lt.StartsWith("DASHED", StringComparison.OrdinalIgnoreCase);   // không phải biên dạng cắt
            bool laBe = hopLe && e is Line &&
                        (lt.StartsWith("DASHED", StringComparison.Ordinal) ||
                         (lt.Equals("BYLAYER", StringComparison.OrdinalIgnoreCase) && netLop.TryGetValue(e.Layer, out string ltLop) && ltLop.StartsWith("DASHED", StringComparison.Ordinal)));
            // 2-F-NAME đã có lỗi "đối tượng không hợp lệ" riêng; khung tấm không phải nét cắt
            bool trangSaiLop = !laKakou && !laFname && !Cung(e.Layer, c.LopKhungDung) && e is Curve && LoaiNet.Contains(loai) &&
                               (e.Color.IsByLayer ? lopTrang.Contains(e.Layer) : LaTrang(e.Color));
            NetCat net = null;
            NetBe be = laBe ? new NetBe { X1 = ((Line)e).StartPoint.X, Y1 = ((Line)e).StartPoint.Y, X2 = ((Line)e).EndPoint.X, Y2 = ((Line)e).EndPoint.Y } : null;

            for (int i = 0; i < khung.Count; i++)
            {
                KhungTho k = khung[i];
                if (e.Handle.Value == k.Handle) continue;
                DuLieuHinh d = ds[i];
                if (chu != null && laFname && Cham(hop, k)) d.ChuFname.Add(chu);
                // theo vị trí chữ: đường gióng của dim có thể thò ra ngoài khung
                if (e is Dimension dm && !string.IsNullOrEmpty(dm.DimensionText) && dm.TextPosition.X >= k.MinX && dm.TextPosition.X <= k.MaxX &&
                    dm.TextPosition.Y >= k.MinY && dm.TextPosition.Y <= k.MaxY) d.ChuDim.Add(dm.DimensionText);
                if (!Trong(hop, k)) continue;
                if (chu != null) d.ChuTrongKhung.Add(chu);
                if (laTapu) d.SoHatch++;
                if (laFname && chu == null) d.SaiFname.Add((loai, hopGon));
                if (trangSaiLop) d.NetTrangSaiLop.Add((e.Layer, hopGon));
                if (!laKakou) continue;
                if (!hopLe) { d.SaiKakou.Add((loai, hopGon)); continue; }
                if (be != null) d.Be.Add(be);
                if (netDut) continue;
                if (net == null) net = LamNet((Curve)e, loai, c.BuocSpline);
                d.Net.Add(net);
            }
        }

        // ByBlock trong model space hiện màu trắng như ACI 7
        private static bool LaTrang(Color m) =>
            m.IsByBlock || (m.IsByAci ? m.ColorIndex == 7 : !m.IsByLayer && m.Red == 255 && m.Green == 255 && m.Blue == 255);

        private static bool Cung(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool Trong(Extents3d h, KhungTho k) =>
            h.MinPoint.X >= k.MinX - Eps && h.MaxPoint.X <= k.MaxX + Eps && h.MinPoint.Y >= k.MinY - Eps && h.MaxPoint.Y <= k.MaxY + Eps;

        private static bool Cham(Extents3d h, KhungTho k) =>
            h.MaxPoint.X >= k.MinX && h.MinPoint.X <= k.MaxX && h.MaxPoint.Y >= k.MinY && h.MinPoint.Y <= k.MaxY;

        private static GhiChuDan DocDan(Entity e, Transaction tr)
        {
            try
            {
                if (e is Leader l)
                {
                    if (l.NumVertices == 0 || l.Annotation.IsNull) return new GhiChuDan { Lop = e.Layer };
                    var a = tr.GetObject(l.Annotation, OpenMode.ForRead) as Entity;
                    string chu = a is MText m ? m.Text : (a as DBText)?.TextString;
                    if (a == null || Cung(a.Layer, PhanTichKhung.LopFname)) return new GhiChuDan { Lop = e.Layer };
                    Point3d p = l.VertexAt(0);
                    return new GhiChuDan { Lop = e.Layer, NoiDung = chu, Mui = { (p.X, p.Y) } };
                }
                if (e is MLeader ml)
                {
                    var g = new GhiChuDan { Lop = e.Layer };
                    if (ml.ContentType != ContentType.MTextContent || ml.MText == null) return g;
                    g.NoiDung = ml.MText.Text;
                    foreach (int chiSo in ml.GetLeaderIndexes())
                        foreach (int duong in ml.GetLeaderLineIndexes(chiSo))
                        {
                            Point3d p = ml.GetFirstVertex(duong);
                            g.Mui.Add((p.X, p.Y));
                        }
                    return g;
                }
            }
            catch (Exception) { return new GhiChuDan { Lop = e.Layer }; }   // leader hỏng: coi như không có ký hiệu, tấm sẽ có cảnh báo
            return null;
        }

        private static NetCat LamNet(Curve c, string loai, double buocSpline)
        {
            var n = new NetCat { Handle = c.Handle.Value, Loai = loai, Kin = c is Circle || (!(c is Line) && !(c is Arc) && c.Closed) };
            n.DoDai = c is Circle tron ? tron.Circumference : c.GetDistanceAtParameter(c.EndParam) - c.GetDistanceAtParameter(c.StartParam);
            switch (c)
            {
                case Line l:
                    n.Diem.Add((l.StartPoint.X, l.StartPoint.Y));
                    n.Diem.Add((l.EndPoint.X, l.EndPoint.Y));
                    break;
                case Circle t:
                    n.Diem = LamPhang.Cung(t.Center.X, t.Center.Y, t.Radius, 0, 2 * Math.PI);
                    break;
                case Arc a:
                    double a0 = Math.Atan2(a.StartPoint.Y - a.Center.Y, a.StartPoint.X - a.Center.X);
                    n.Diem = LamPhang.Cung(a.Center.X, a.Center.Y, a.Radius, a0, a.TotalAngle * Math.Sign(a.Normal.Z));
                    n.Diem[n.Diem.Count - 1] = (a.EndPoint.X, a.EndPoint.Y);
                    break;
                case Polyline p:
                    double dau = Math.Sign(p.Normal.Z);
                    var dinh = new List<(double X, double Y, double Bulge)>();
                    for (int i = 0; i < p.NumberOfVertices; i++)
                    {
                        Point3d q = p.GetPoint3dAt(i);
                        dinh.Add((q.X, q.Y, p.GetBulgeAt(i) * dau));
                    }
                    n.Diem = LamPhang.Polyline(dinh, p.Closed);
                    break;
                case Polyline2d _:
                case Polyline3d _:
                    int soKhoang = (int)Math.Round(c.EndParam - c.StartParam) * 8;
                    for (int i = 0; i <= soKhoang; i++)
                    {
                        Point3d q = c.GetPointAtParameter(c.StartParam + (c.EndParam - c.StartParam) * i / soKhoang);
                        n.Diem.Add((q.X, q.Y));
                    }
                    break;
                default:   // SPLINE, ELLIPSE: chia theo bước spline trong cài đặt, ít nhất 20 đoạn như lisp
                    double buoc = buocSpline > 0 ? buocSpline : 1.0;
                    int soDoan = Math.Max(20, (int)(n.DoDai / buoc));
                    for (int i = 0; i < soDoan; i++)
                    {
                        Point3d q = c.GetPointAtDist(n.DoDai * i / soDoan);
                        n.Diem.Add((q.X, q.Y));
                    }
                    n.Diem.Add((c.EndPoint.X, c.EndPoint.Y));
                    break;
            }
            return n;
        }

        /// <summary>Ký hiệu bẻ không khớp số đoạn: hỏi trên dòng lệnh số đường nhấn 1 lần; Esc → null.</summary>
        public static int? HoiSoNhan(Document doc, string ten, int soDoan)
        {
            var o = new PromptIntegerOptions("\n[CSV] Tam " + ten + ": nhap so duong be nhan 1 lan (tong " + soDoan + " doan): ")
            {
                AllowNegative = false, LowerLimit = 0, UpperLimit = soDoan,
            };
            PromptIntegerResult r = doc.Editor.GetInteger(o);
            return r.Status == PromptStatus.OK ? r.Value : (int?)null;
        }
    }
}
