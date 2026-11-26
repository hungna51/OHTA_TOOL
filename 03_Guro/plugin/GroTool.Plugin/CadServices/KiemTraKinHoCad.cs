using System;
using System.Collections.Generic;
using System.Linq;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
using CadColor = ZwSoft.ZwCAD.Colors.Color;
using DbPolyline = ZwSoft.ZwCAD.DatabaseServices.Polyline;
using DbLine = ZwSoft.ZwCAD.DatabaseServices.Line;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using CadColor = Autodesk.AutoCAD.Colors.Color;
using DbPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using DbLine = Autodesk.AutoCAD.DatabaseServices.Line;
#endif
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.CadServices
{
    public sealed class KetQuaQuetKinHo
    {
        public List<LoiKinHo> Loi = new List<LoiKinHo>();
        public int SoDoiTuong;
        public int SoNetKakou;
    }

    /// <summary>
    /// Đọc bản vẽ kiểm tra kín hở biên dạng 0-KAKOU, nét ngoài hình chính (3-KEGAKI / 4-SHT),
    /// thuộc tính ByLayer (0-KAKOU, 3-KEGAKI, 4-SHT, 2-F-NAME) và đồng bộ trạng thái xử lý lỗi trên bản vẽ.
    /// </summary>
    public static class KiemTraKinHoCad
    {
        private const double Eps = 1e-6;
        private const string AppChup = "GRO_CHUP_COPY";
        private static readonly HashSet<string> LoaiNet =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "LINE", "ARC", "CIRCLE", "ELLIPSE", "SPLINE", "LWPOLYLINE", "POLYLINE" };

        /// <summary>
        /// Chạy -OVERKILL riêng theo từng layer (và tách riêng đường bẻ DASHED* trên 0-KAKOU) cho các nét vẽ
        /// nằm trong các khung tấm cần kiểm tra, giúp loại bỏ nét đè/trùng trước khi check kín hở &amp; ByLayer.
        /// Phải gọi trong ngữ cảnh lệnh CAD (không khoá Document). Trả về số nét bị xoá/gộp.
        /// </summary>
        public static int OverkillTheoTam(Document doc, IList<PlateItem> plates, bool gomTamGiaCong)
        {
            if (doc == null || plates == null || plates.Count == 0) return 0;

            var tamKiem = plates
                .Where(p => !p.IsOrphan && !p.FrameId.IsNull && p.FrameBoundingBox.HasValue && !p.NoCuttingShape &&
                            (gomTamGiaCong || string.IsNullOrWhiteSpace(p.GC)))
                .Select(p => p.FrameBoundingBox.Value)
                .ToList();
            if (tamKiem.Count == 0) return 0;

            var khungIds = new HashSet<ObjectId>(plates.Where(p => !p.FrameId.IsNull).Select(p => p.FrameId));
            string lopKhungTam = GroRegistry.LayerKhungTam();

            var nhom = new Dictionary<string, List<ObjectId>>(StringComparer.OrdinalIgnoreCase);
            var daCo = new HashSet<ObjectId>();

            Database db = doc.Database;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DocThongTinLayer(tr, db, out var lopAn, out var netLop);
                var ms = (BlockTableRecord)tr.GetObject(
                    ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))[BlockTableRecord.ModelSpace],
                    OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (id.IsErased || khungIds.Contains(id) || !daCo.Add(id)) continue;
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Entity e) || !e.Visible) continue;
                    string loai = id.ObjectClass?.DxfName ?? "";
                    if (!(e is Curve) || !LoaiNet.Contains(loai)) continue;

                    string lay = e.Layer ?? "";
                    if (lopAn.Contains(lay) || lay.EndsWith("_CHUP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (Cung(lay, "KHUNG_IN") || Cung(lay, lopKhungTam)) continue;
                    if (CoXDataChup(e)) continue;

                    Extents3d ext;
                    try { ext = e.GeometricExtents; }
                    catch { continue; }
                    if (!tamKiem.Any(box => Trong(ext, box))) continue;

                    string lt = e.Linetype ?? "ByLayer";
                    if (lt.Equals("ByLayer", StringComparison.OrdinalIgnoreCase) && netLop.TryGetValue(lay, out string ltLop))
                        lt = ltLop;
                    string nhomLt = LaDuongBeCad(e, netLop) ? "DASHED:" + lt : lt;
                    string khoa = lay + "|" + nhomLt;
                    if (!nhom.TryGetValue(khoa, out var ds)) nhom[khoa] = ds = new List<ObjectId>();
                    ds.Add(id);
                }
                tr.Commit();
            }

            int xoa = 0;
            object oldCmdEcho = null;
            try
            {
                try
                {
                    oldCmdEcho = CadApplication.GetSystemVariable("CMDECHO");
                    CadApplication.SetSystemVariable("CMDECHO", 0);
                }
                catch { oldCmdEcho = null; }

                foreach (List<ObjectId> ids in nhom.Values.Where(x => x.Count > 1))
                {
                    doc.Editor.Command("_.-OVERKILL", SelectionSet.FromObjectIds(ids.ToArray()), "", "");
                    xoa += ids.Count(id => id.IsErased);
                }
            }
            finally
            {
                if (oldCmdEcho != null)
                {
                    try { CadApplication.SetSystemVariable("CMDECHO", oldCmdEcho); } catch { }
                }
                try { doc.Editor?.UpdateScreen(); } catch { }
            }
            return xoa;
        }

        public static KetQuaQuetKinHo QuetTheoTam(Document doc, IList<PlateItem> plates, bool gomTamGiaCong, double saiSo)
        {
            var kq = new KetQuaQuetKinHo();
            if (doc == null || plates == null || plates.Count == 0) return kq;

            var tamKiem = plates
                .Where(p => !p.IsOrphan && !p.FrameId.IsNull && p.FrameBoundingBox.HasValue && !p.NoCuttingShape &&
                            (gomTamGiaCong || string.IsNullOrWhiteSpace(p.GC)))
                .Select(p => (
                    Plate: p,
                    Box: p.FrameBoundingBox.Value,
                    DuLieu: new DuLieuTamKiem { Bango = p.Bango }
                ))
                .ToList();

            var saiByLayerHandles = new List<long>();
            var saiByLayerPos = new List<(double MinX, double MinY, double MaxX, double MaxY)>();
            var saiByLayerLop = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Database db = doc.Database;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DocThongTinLayer(tr, db, out var lopAn, out var netLop);
                var ms = (BlockTableRecord)tr.GetObject(((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (id.IsErased) continue;
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Entity e) || !e.Visible) continue;
                    string lay = e.Layer ?? "";
                    if (lopAn.Contains(lay) || lay.EndsWith("_CHUP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (CoXDataChup(e)) continue;

                    string loai = id.ObjectClass?.DxfName ?? "";
                    Extents3d ext;
                    try { ext = e.GeometricExtents; }
                    catch { continue; }
                    var hop = (ext.MinPoint.X, ext.MinPoint.Y, ext.MaxPoint.X, ext.MaxPoint.Y);

                    if (!KiemTraKinHo.LaDungByLayer(lay, loai, e.Color.IsByLayer, e.Linetype, e.LineWeight == LineWeight.ByLayer))
                    {
                        saiByLayerHandles.Add((long)e.Handle.Value);
                        saiByLayerPos.Add(hop);
                        saiByLayerLop.Add(lay.ToUpperInvariant());
                    }

                    bool laKakou = Cung(lay, "0-KAKOU");
                    bool laPhu = (Cung(lay, "3-KEGAKI") || Cung(lay, "4-SHT")) && e is Curve && LoaiNet.Contains(loai);
                    if (!laKakou && !laPhu) continue;

                    bool hopLeKakou = laKakou && e is Curve && LoaiNet.Contains(loai);
                    bool laBe = hopLeKakou && LaDuongBeCad(e, netLop);
                    NetKiem net = null;
                    NetPhuKiem netPhu = null;

                    for (int i = 0; i < tamKiem.Count; i++)
                    {
                        var tk = tamKiem[i];
                        if (id == tk.Plate.FrameId) continue;
                        if (!Trong(ext, tk.Box)) continue;

                        if (laPhu)
                        {
                            if (netPhu == null)
                            {
                                var n = LamNet((Curve)e, loai);
                                netPhu = new NetPhuKiem { Lop = lay.ToUpperInvariant(), Diem = n.Diem, Hop = hop };
                            }
                            tk.DuLieu.NetPhu.Add(netPhu);
                        }
                        if (!laKakou) continue;
                        if (!hopLeKakou)
                        {
                            tk.DuLieu.SaiLoaiKakou.Add((loai, hop));
                            continue;
                        }
                        if (laBe) continue;
                        if (net == null) net = LamNet((Curve)e, loai);
                        tk.DuLieu.NetKakou.Add(net);
                    }
                }
                tr.Commit();
            }

            if (saiByLayerHandles.Count > 0)
            {
                kq.Loi.Add(new LoiKinHo
                {
                    Ma = "BYLAYER",
                    TieuDe = "Thuộc tính chưa để ByLayer (" + string.Join(", ", saiByLayerLop.OrderBy(x => x)) + ")",
                    ChiTiet = saiByLayerHandles.Count + " đối tượng sai Color / Linetype / LineWeight (trừ đường bẻ DASHED*)",
                    LaByLayer = true,
                    ViTri = saiByLayerPos,
                    Handles = saiByLayerHandles,
                });
            }

            foreach (var tk in tamKiem)
            {
                kq.Loi.AddRange(KiemTraKinHo.KiemTraTamKhung(tk.DuLieu, saiSo));
            }
            return kq;
        }

        /// <summary>Quét các đối tượng do người dùng quét chọn cho lệnh KT (không bắt lỗi ngoài hình chính).</summary>
        public static KetQuaQuetKinHo QuetTheoTapChon(Document doc, IReadOnlyCollection<long> handles, double saiSo)
        {
            var kq = new KetQuaQuetKinHo();
            if (doc == null || handles == null || handles.Count == 0) return kq;

            var netKakou = new List<NetKiem>();
            var saiLoaiKakou = new List<(string Loai, (double MinX, double MinY, double MaxX, double MaxY) Hop)>();
            var saiByLayerHandles = new List<long>();
            var saiByLayerPos = new List<(double MinX, double MinY, double MaxX, double MaxY)>();
            var saiByLayerLop = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Database db = doc.Database;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DocThongTinLayer(tr, db, out var lopAn, out var netLop);

                foreach (long h in handles.Distinct())
                {
                    if (!db.TryGetObjectId(new Handle(h), out ObjectId id) || id.IsNull || id.IsErased) continue;
                    if (!(tr.GetObject(id, OpenMode.ForRead, false) is Entity e) || !e.Visible) continue;
                    string lay = e.Layer ?? "";
                    if (lopAn.Contains(lay) || lay.EndsWith("_CHUP", StringComparison.OrdinalIgnoreCase)) continue;
                    if (CoXDataChup(e)) continue;

                    kq.SoDoiTuong++;
                    string loai = id.ObjectClass?.DxfName ?? "";
                    Extents3d ext;
                    try { ext = e.GeometricExtents; }
                    catch { continue; }
                    var hop = (ext.MinPoint.X, ext.MinPoint.Y, ext.MaxPoint.X, ext.MaxPoint.Y);

                    if (!KiemTraKinHo.LaDungByLayer(lay, loai, e.Color.IsByLayer, e.Linetype, e.LineWeight == LineWeight.ByLayer))
                    {
                        saiByLayerHandles.Add((long)e.Handle.Value);
                        saiByLayerPos.Add(hop);
                        saiByLayerLop.Add(lay.ToUpperInvariant());
                    }

                    if (!Cung(lay, "0-KAKOU")) continue;
                    if (!(e is Curve cv) || !LoaiNet.Contains(loai))
                    {
                        saiLoaiKakou.Add((loai, hop));
                        continue;
                    }
                    if (LaDuongBeCad(e, netLop)) continue;
                    netKakou.Add(LamNet(cv, loai));
                }
                kq.SoNetKakou = netKakou.Count;
                tr.Commit();
            }

            kq.Loi.AddRange(KiemTraKinHo.KiemTraToanBanVe(netKakou, saiLoaiKakou, saiSo));

            if (saiByLayerHandles.Count > 0)
            {
                kq.Loi.Add(new LoiKinHo
                {
                    Ma = "BYLAYER",
                    TieuDe = "Chưa để ByLayer (" + string.Join(", ", saiByLayerLop.OrderBy(x => x)) + ")",
                    ChiTiet = saiByLayerHandles.Count + " đối tượng sai Color / Linetype / LineWeight (trừ DASHED*)",
                    LaByLayer = true,
                    ViTri = saiByLayerPos,
                    Handles = saiByLayerHandles,
                });
            }
            return kq;
        }

        /// <summary>Đưa các đối tượng về ByLayer (giữ nguyên kiểu nét DASHED* cho đường bẻ LINE trên 0-KAKOU).</summary>
        public static int DuaVeByLayer(Document doc, IEnumerable<long> handles)
        {
            if (doc == null || handles == null) return 0;
            Database db = doc.Database;
            int doi = 0;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (long h in handles)
                {
                    if (!db.TryGetObjectId(new Handle(h), out ObjectId id) || id.IsNull || id.IsErased) continue;
                    if (!(tr.GetObject(id, OpenMode.ForWrite, false) is Entity ent)) continue;
                    string loai = id.ObjectClass?.DxfName ?? "";
                    bool giuDash = KiemTraKinHo.LaDuongBe(ent.Layer, loai, ent.Linetype);
                    ent.Color = CadColor.FromColorIndex(ColorMethod.ByLayer, 256);
                    ent.LineWeight = LineWeight.ByLayer;
                    if (!giuDash)
                    {
                        ent.Linetype = "ByLayer";
                        ent.LinetypeScale = 1.0;
                    }
                    doi++;
                }
                tr.Commit();
            }
            doc.Editor?.UpdateScreen();
            return doi;
        }

        /// <summary>Zoom tới vị trí lỗi và vẽ Cloud đỏ tạm quanh nó.</summary>
        public static void ZoomVaVeCloud(Document doc, (double MinX, double MinY, double MaxX, double MaxY) viTri, double coThamChieu = 120.0)
        {
            if (doc == null) return;
            double co = Math.Max(coThamChieu, Math.Max(viTri.MaxX - viTri.MinX, viTri.MaxY - viTri.MinY));
            var clouds = KiemTraKinHo.TaoCloud(new[] { viTri }, Math.Max(30.0, co));
            CloudLoiCad.Hien(doc, clouds);

            double w = viTri.MaxX - viTri.MinX, h = viTri.MaxY - viTri.MinY;
            double pad = Math.Max(15.0, Math.Max(w, h) * 0.4);
            var ext = new Extents3d(
                new Point3d(viTri.MinX - pad, viTri.MinY - pad, 0),
                new Point3d(viTri.MaxX + pad, viTri.MaxY + pad, 0));
            CadViewService.ZoomToExtents(ext, 0.2);
        }

        /// <summary>Trả về tập Bango đã có ít nhất 1 Curve trên layer 4-SHT nằm trong khung tấm.</summary>
        public static HashSet<string> TapBangoDaCoNetSht(Document doc, IList<PlateItem> plates)
        {
            var kq = new HashSet<string>(StringComparer.Ordinal);
            if (doc == null || plates == null) return kq;
            var shtPlates = plates.Where(p => !p.IsOrphan && p.LaXuLySht && p.FrameBoundingBox.HasValue).ToList();
            if (shtPlates.Count == 0) return kq;

            Database db = doc.Database;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    if (id.IsErased) continue;
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Entity e) || !e.Visible) continue;
                    if (!Cung(e.Layer, "4-SHT") || !(e is Curve)) continue;
                    if (CoXDataChup(e)) continue;
                    Extents3d ext;
                    try { ext = e.GeometricExtents; } catch { continue; }
                    foreach (PlateItem p in shtPlates)
                    {
                        if (!string.IsNullOrEmpty(p.Bango) && Trong(ext, p.FrameBoundingBox.Value))
                            kq.Add(p.Bango);
                    }
                }
                tr.Commit();
            }
            return kq;
        }

        /// <summary>Đếm số handle còn tồn tại (chưa bị xoá) trên bản vẽ.</summary>
        public static int DemHandleConTonTai(Document doc, IEnumerable<string> handles)
        {
            if (doc == null || handles == null) return 0;
            Database db = doc.Database;
            int n = 0;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (string h in handles)
                {
                    if (!long.TryParse(h, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out long so))
                        continue;
                    if (db.TryGetObjectId(new Handle(so), out ObjectId id) && !id.IsNull && !id.IsErased)
                    {
                        if (tr.GetObject(id, OpenMode.ForRead, false) is Entity) n++;
                    }
                }
                tr.Commit();
            }
            return n;
        }

        private static void DocThongTinLayer(Transaction tr, Database db, out HashSet<string> lopAn, out Dictionary<string, string> netLop)
        {
            lopAn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            netLop = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (ObjectId id in (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead))
            {
                var l = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (l.IsOff || l.IsFrozen) lopAn.Add(l.Name);
                var lt = tr.GetObject(l.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;
                netLop[l.Name] = lt?.Name ?? "Continuous";
            }
        }

        private static bool LaDuongBeCad(Entity e, Dictionary<string, string> netLop)
        {
            if (!(e is DbLine)) return false;
            string lt = e.Linetype ?? "";
            if (lt.StartsWith("DASHED", StringComparison.OrdinalIgnoreCase)) return true;
            return lt.Equals("BYLAYER", StringComparison.OrdinalIgnoreCase) &&
                   netLop.TryGetValue(e.Layer ?? "", out string ltLop) &&
                   ltLop.StartsWith("DASHED", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CoXDataChup(Entity e)
        {
            try { return e.GetXDataForApplication(AppChup) != null; }
            catch { return false; }
        }

        private static bool Cung(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool Trong(Extents3d h, Extents3d k) =>
            h.MinPoint.X >= k.MinPoint.X - Eps && h.MaxPoint.X <= k.MaxPoint.X + Eps &&
            h.MinPoint.Y >= k.MinPoint.Y - Eps && h.MaxPoint.Y <= k.MaxPoint.Y + Eps;

        private static NetKiem LamNet(Curve c, string loai)
        {
            var n = new NetKiem
            {
                Handle = (long)c.Handle.Value,
                Loai = loai.ToUpperInvariant(),
                Kin = c is Circle || (!(c is DbLine) && !(c is Arc) && SafeClosed(c)),
            };
            try
            {
                n.DoDai = c is Circle tron ? tron.Circumference
                        : c.GetDistanceAtParameter(c.EndParam) - c.GetDistanceAtParameter(c.StartParam);
            }
            catch { n.DoDai = 0; }

            switch (c)
            {
                case DbLine l:
                    n.Diem.Add((l.StartPoint.X, l.StartPoint.Y));
                    n.Diem.Add((l.EndPoint.X, l.EndPoint.Y));
                    break;
                case Circle t:
                    n.Diem = LamPhangGro.Cung(t.Center.X, t.Center.Y, t.Radius, 0, 2 * Math.PI);
                    break;
                case Arc a:
                    double a0 = Math.Atan2(a.StartPoint.Y - a.Center.Y, a.StartPoint.X - a.Center.X);
                    n.Diem = LamPhangGro.Cung(a.Center.X, a.Center.Y, a.Radius, a0, a.TotalAngle * Math.Sign(a.Normal.Z));
                    if (n.Diem.Count > 0) n.Diem[n.Diem.Count - 1] = (a.EndPoint.X, a.EndPoint.Y);
                    break;
                case DbPolyline p:
                    double dau = Math.Sign(p.Normal.Z);
                    if (dau == 0) dau = 1.0;
                    var dinh = new List<(double X, double Y, double Bulge)>();
                    for (int i = 0; i < p.NumberOfVertices; i++)
                    {
                        Point3d q = p.GetPoint3dAt(i);
                        dinh.Add((q.X, q.Y, p.GetBulgeAt(i) * dau));
                    }
                    n.Diem = LamPhangGro.Polyline(dinh, p.Closed);
                    break;
                case Polyline2d _:
                case Polyline3d _:
                    int soKhoang = Math.Max(8, (int)Math.Round(c.EndParam - c.StartParam) * 8);
                    for (int i = 0; i <= soKhoang; i++)
                    {
                        Point3d q = c.GetPointAtParameter(c.StartParam + (c.EndParam - c.StartParam) * i / soKhoang);
                        n.Diem.Add((q.X, q.Y));
                    }
                    break;
                default:
                    int soDoan = Math.Max(20, (int)(n.DoDai / 1.0));
                    for (int i = 0; i < soDoan; i++)
                    {
                        Point3d q = c.GetPointAtDist(n.DoDai * i / soDoan);
                        n.Diem.Add((q.X, q.Y));
                    }
                    n.Diem.Add((c.EndPoint.X, c.EndPoint.Y));
                    break;
            }
            if (n.Diem.Count == 0)
            {
                n.Diem.Add((0, 0));
                n.Diem.Add((0, 0));
            }
            return n;
        }

        private static bool SafeClosed(Curve c) { try { return c.Closed; } catch { return false; } }
    }
}
