using System.Collections.Generic;
using System.Globalization;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
#endif
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 225 (GĐ 3e): phần CAD của 3 bước 3b cuối cùng còn lisp —
    /// <list type="bullet">
    /// <item><c>doctext3b</c>: đọc + phân loại text số (gro--geom-parse-dim-texts) -> (txt-lin txt-hole txt-ang);</item>
    /// <item><c>dimkhop3b</c>: nâng dim góc + leader + Dim dài + dim góc -> (so-net (ename ...));</item>
    /// <item><c>chuyenlayer3b</c>: đổi layer từng đối tượng trên layer gốc -> (k dim g t 0 ((e lt) ...)).</item>
    /// </list>
    /// </summary>
    public static class Dim3bCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ============================ doctext3b ============================

        /// <summary>ssIds = text trong ss (đúng thứ tự), suppIds = ssget "X" text layer dim chuẩn (lisp xét tối đa 300 phần tử đầu).</summary>
        public static object DocText(Document doc, string cheDo, IList<ObjectId> ssIds, IList<ObjectId> suppIds)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var ung = new List<ObjectId>();
            var co = new HashSet<ObjectId>();
            var lin = new List<object[]>();
            var hole = new List<object[]>();
            var ang = new List<object[]>();
            var ab = new List<SoSanh.BanGhi>();
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ssIds)
                    if (!id.IsErased && LaText(tr.GetObject(id, OpenMode.ForRead)) && co.Add(id)) ung.Add(id);
                for (int i = 0; i < suppIds.Count && i < 300; i++)
                    if (!suppIds[i].IsErased && co.Add(suppIds[i])) ung.Add(suppIds[i]);

                foreach (ObjectId id in ung)
                {
                    var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    string raw; double h, rot; double[] pos;
                    if (e is MText mt) { raw = mt.Contents; h = mt.TextHeight; rot = mt.Rotation; pos = new[] { mt.Location.X, mt.Location.Y, mt.Location.Z }; }
                    else if (e is DBText dt) { raw = dt.TextString; h = dt.Height; rot = dt.Rotation; pos = new[] { dt.Position.X, dt.Position.Y, dt.Position.Z }; }
                    else continue;
                    DocText3b.KetQua k = DocText3b.PhanLoai(raw);
                    if (k == null) continue;
                    if (!(h > 0)) h = 5.0;
                    double[] bb = null;
                    try { Extents3d x = e.GeometricExtents; bb = new[] { x.MinPoint.X, x.MinPoint.Y, x.MaxPoint.X, x.MaxPoint.Y }; } catch { }
                    object pt = bb != null ? (object)new[] { (bb[0] + bb[2]) / 2.0, (bb[1] + bb[3]) / 2.0 } : pos;
                    var item = new List<object> { id, k.Txt, k.V, pt, rot, h, bb };
                    if (k.Loai == DocText3b.Lo) item.Add(k.LaPhi);
                    (k.Loai == DocText3b.Goc ? ang : k.Loai == DocText3b.Lo ? hole : lin).Add(item.ToArray());
                    ab.Add(new SoSanh.BanGhi(id.Handle.ToString()).Dat("loai", k.Loai.ToString(Inv)).Dat("v", k.V));
                }
                tr.Commit();
            }
            sw.Stop();
            if (cheDo == "ab") GroBuocCommands.PhienHienTai().Dat("ab_doctext3b", ab);

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (List<object[]> ds in new[] { lin, hole, ang })
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                foreach (object[] it in ds) ThemCay(rb, it);
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            Ghi(doc, cheDo, string.Format(Inv, "doc text so: {0} text -> dai {1}, lo {2}, goc {3}, {4} ms",
                ung.Count, lin.Count, hole.Count, ang.Count, sw.ElapsedMilliseconds));
            return rb;
        }

        private static bool LaText(DBObject o) => o is MText || (o is DBText && !(o is AttributeDefinition));

        // ============================ dimkhop3b ============================

        public static object DimKhop(Document doc, string cheDo, List<object> linL, List<object> holeL, List<object> angL,
                                     List<object> linesL, List<object> arcsL, List<object> tinyL)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var id = new Dictionary<string, ObjectId>();
            var lines = new List<DoanThang>();
            var arcs = new List<DimKhop3b.Cung>();
            var tiny = new List<DimKhop3b.TronNho>();
            List<DimKhop3b.TextSo> lin, hole, ang;
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (object o in linesL ?? new List<object>())
                    if (o is ObjectId lid && !lid.IsErased && tr.GetObject(lid, OpenMode.ForRead) is Line ln)
                    {
                        lines.Add(new DoanThang(lid.Handle.ToString(), ln.StartPoint.X, ln.StartPoint.Y, ln.EndPoint.X, ln.EndPoint.Y));
                        id[lid.Handle.ToString()] = lid;
                    }
                foreach (object o in arcsL ?? new List<object>())
                    if (o is ObjectId aid && !aid.IsErased && tr.GetObject(aid, OpenMode.ForRead) is Arc a)
                    {
                        arcs.Add(new DimKhop3b.Cung { Handle = aid.Handle.ToString(), Cx = a.Center.X, Cy = a.Center.Y, R = a.Radius, A0 = a.StartAngle, A1 = a.EndAngle });
                        id[aid.Handle.ToString()] = aid;
                    }
                tr.Commit();
            }
            foreach (object o in tinyL ?? new List<object>())
            {
                List<object> t = LispCay.Ds(o);
                List<object> c = t != null && t.Count >= 3 ? LispCay.Ds(t[0]) : null;
                if (c == null || c.Count < 2 || !(t[2] is ObjectId tid)) return Loi(doc, "tiny-circles sai dang");
                tiny.Add(new DimKhop3b.TronNho { Handle = tid.Handle.ToString(), X = LispCay.So(c[0]) ?? 0, Y = LispCay.So(c[1]) ?? 0 });
                id[tid.Handle.ToString()] = tid;
            }
            string loi1 = DocTexts(linL, out lin), loi2 = DocTexts(holeL, out hole), loi3 = DocTexts(angL, out ang);
            if ((loi1 ?? loi2 ?? loi3) != null) return Loi(doc, loi1 ?? loi2 ?? loi3);

            DimKhop3b.KetQua kq = DimKhop3b.Chay(lin, hole, ang, lines, arcs, tiny);
            sw.Stop();
            if (cheDo == "ab")
            {
                var r = new List<SoSanh.BanGhi>();
                foreach (string h in kq.Net) r.Add(new SoSanh.BanGhi(h));
                GroBuocCommands.PhienHienTai().Dat("ab_dimkhop3b", r);
            }
            if (cheDo == "cs")
                foreach (KeyValuePair<string, int> c in kq.Chuoi)
                    doc.Editor?.WriteMessage("\n[Xu ly net] Dim \"" + c.Key + "\": duong dim chinh ghep tu " + c.Value + " LINE thang hang (muc 169).");

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            rb.Add(new TypedValue((int)LispDataType.Int32, kq.Net.Count));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (string h in kq.Net) rb.Add(new TypedValue((int)LispDataType.ObjectId, id[h]));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            Ghi(doc, cheDo, string.Format(Inv, "khop dim: {0} chu so (nang goc {1}), {2} lo, {3} LINE, {4} cung -> leader {5}, dai {6}, goc {7}, tong {8} net, {9} ms",
                lin.Count, kq.SoNang, hole.Count, lines.Count, arcs.Count, kq.SoLeader, kq.SoDai, kq.SoGoc, kq.Net.Count, sw.ElapsedMilliseconds));
            return rb;
        }

        /// <summary>(e txt v pt rot h bb [is-phi]) -> TextSo.</summary>
        private static string DocTexts(List<object> ds, out List<DimKhop3b.TextSo> kq)
        {
            kq = new List<DimKhop3b.TextSo>();
            foreach (object o in ds ?? new List<object>())
            {
                List<object> it = LispCay.Ds(o);
                if (it == null || it.Count < 7 || !(it[0] is ObjectId tid)) return "text so sai dang";
                List<object> pt = LispCay.Ds(it[3]), bb = LispCay.Ds(it[6]);
                double? v = LispCay.So(it[2]), rot = LispCay.So(it[4]), h = LispCay.So(it[5]);
                if (pt == null || pt.Count < 2 || v == null || rot == null || h == null) return "text so thieu v / pt / rot / h";
                var t = new DimKhop3b.TextSo
                {
                    Handle = tid.Handle.ToString(), Txt = it[1] as string ?? "", V = v.Value,
                    Px = LispCay.So(pt[0]) ?? 0, Py = LispCay.So(pt[1]) ?? 0, Rot = rot.Value, H = h.Value,
                    LaPhi = it.Count > 7 && it[7] is bool b && b,
                };
                if (bb != null && bb.Count >= 4)
                    t.Bb = new Hop(LispCay.So(bb[0]) ?? 0, LispCay.So(bb[1]) ?? 0, LispCay.So(bb[2]) ?? 0, LispCay.So(bb[3]) ?? 0);
                kq.Add(t);
            }
            return null;
        }

        // ============================ chuyenlayer3b ============================

        /// <summary>
        /// Vòng "3b: chuyen layer tung doi tuong" của gro--xu-ly-net, chỉ nhánh TH1a / TH2-TH5 (nhánh đổi theo MÀU
        /// thủ công TH1b vẫn ở lisp). "ab" chỉ tính, không ghi bản vẽ.
        /// </summary>
        public static object ChuyenLayer(Document doc, string cheDo, string layGoc, IList<ObjectId> ssIds, List<object> dimL)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var dim = new HashSet<ObjectId>();
            foreach (object o in dimL ?? new List<object>()) if (o is ObjectId d) dim.Add(d);
            int k = 0, nd = 0, g = 0, t = 0, n0 = 0, loiGhi = 0;
            var chuaDoi = new List<KeyValuePair<ObjectId, string>>();
            var ab = new List<SoSanh.BanGhi>();
            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                string ltLayer = "Continuous";
                if (lt.Has(layGoc))
                {
                    var ltr = (LayerTableRecord)tr.GetObject(lt[layGoc], OpenMode.ForRead);
                    if (tr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) is LinetypeTableRecord lr) ltLayer = lr.Name;
                }
                foreach (ObjectId id in ssIds)
                {
                    if (id.IsErased || !(tr.GetObject(id, OpenMode.ForRead) is Entity e)) continue;
                    if (!string.Equals(e.Layer, layGoc, System.StringComparison.OrdinalIgnoreCase)) continue;
                    using (ResultBuffer xd = e.GetXDataForApplication("GRO_CHUP_COPY")) if (xd != null) continue;
                    string loaiNet = e.Linetype;
                    if (string.IsNullOrEmpty(loaiNet) || string.Equals(loaiNet, "BYLAYER", System.StringComparison.OrdinalIgnoreCase)) loaiNet = ltLayer;
                    string ltU = loaiNet.ToUpperInvariant();
                    bool hinh = e is Line || e is Polyline || e is Polyline2d || e is Polyline3d || e is Arc || e is Circle || e is Spline || e is Ellipse;
                    string den = null;
                    if (dim.Contains(id)) { den = "1-SUNPOU"; nd++; }
                    else if (hinh && ltU == "CONTINUOUS") { den = "0-KAKOU"; k++; }
                    else if (hinh && ltU == "CENTER1") { den = "3-KEGAKI"; g++; }
                    else if (LaText(e) || e is Dimension || e is Leader || e is MLeader) { den = "1-SUNPOU"; t++; }
                    else { n0++; chuaDoi.Insert(0, new KeyValuePair<ObjectId, string>(id, loaiNet)); }
                    if (den == null) continue;
                    ab.Add(new SoSanh.BanGhi(id.Handle.ToString()).Dat("lop", den));
                    if (cheDo != "cs") continue;
                    try
                    {
                        e.UpgradeOpen();
                        e.Layer = den;
                        if (!string.Equals(e.Linetype, "BYLAYER", System.StringComparison.OrdinalIgnoreCase)) e.Linetype = "ByLayer";
                        if (!e.Color.IsByLayer) e.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                    }
                    catch { loiGhi++; }   // layer goc khoa: entmod cua lisp cung im lang khong doi
                }
                tr.Commit();
            }
            sw.Stop();
            if (cheDo == "ab") GroBuocCommands.PhienHienTai().Dat("ab_chuyenlayer3b", ab);

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (int so in new[] { k, nd, g, t, n0 }) rb.Add(new TypedValue((int)LispDataType.Int32, so));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (KeyValuePair<ObjectId, string> c in chuaDoi)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.ObjectId, c.Key));
                rb.Add(new TypedValue((int)LispDataType.Text, c.Value));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            Ghi(doc, cheDo, string.Format(Inv, "chuyen layer: 0-KAKOU {0}, dim {1}, 3-KEGAKI {2}, text {3}, giu {4}{5}, {6} ms",
                k, nd, g, t, n0, loiGhi > 0 ? ", " + loiGhi + " loi ghi (layer khoa?)" : "", sw.ElapsedMilliseconds));
            return rb;
        }

        // ============================ chung ============================

        private static object Loi(Document doc, string ly)
        {
            doc.Editor?.WriteMessage("\n[GRO .NET] buoc 3b: " + ly + ".");
            return null;
        }

        private static void Ghi(Document doc, string cheDo, string dong)
        {
            ZwCadPlugin.OhtaBugBridge.Info("GRO", dong + " (" + cheDo + ")");
            doc.Editor?.WriteMessage("\n[GRO] " + dong + (cheDo == "cs" ? "." : " (chi tinh de so A/B)."));
        }

        /// <summary>object[] / double[] -> list lisp; ObjectId, string, double, bool (T / nil), null (nil).</summary>
        private static void ThemCay(ResultBuffer rb, object o)
        {
            switch (o)
            {
                case null: rb.Add(new TypedValue((int)LispDataType.Nil)); break;
                case bool b: rb.Add(new TypedValue((int)(b ? LispDataType.T_atom : LispDataType.Nil))); break;
                case ObjectId id: rb.Add(new TypedValue((int)LispDataType.ObjectId, id)); break;
                case string s: rb.Add(new TypedValue((int)LispDataType.Text, s)); break;
                case double d: rb.Add(new TypedValue((int)LispDataType.Double, d)); break;
                case double[] ds:
                    rb.Add(new TypedValue((int)LispDataType.ListBegin));
                    foreach (double d in ds) rb.Add(new TypedValue((int)LispDataType.Double, d));
                    rb.Add(new TypedValue((int)LispDataType.ListEnd));
                    break;
                case object[] arr:
                    rb.Add(new TypedValue((int)LispDataType.ListBegin));
                    foreach (object x in arr) ThemCay(rb, x);
                    rb.Add(new TypedValue((int)LispDataType.ListEnd));
                    break;
            }
        }
    }
}
