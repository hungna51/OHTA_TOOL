using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
#endif
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 178 (GĐ 6a): phần CAD của xếp cụm sang cột phải. Lisp gửi ss-model (thứ tự ssname), các cụm
    /// ((tid nhom mirror1 bb (ename...)) ...), bb-goc, es-khac, gro--ds-khung-bo, gro--hop-khung-bo,
    /// khe / lề mirror. C# đọc hộp bao (GeometricExtents), gọi <see cref="XepCum.Chay"/>; chế độ "cs" áp
    /// độ dời vào bản vẽ (TransformBy, mỗi entity một lần). Trả
    /// (X_L da-ne ((dx dy (x1 y1 x2 y2)) ...) ((nhom x y) ...) n-sot (ename-sot ...) n-dim-theo).
    /// </summary>
    public static class XepCumCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static Hop DocHop(object o)
        {
            List<object> b = LispCay.Ds(o);
            if (b == null || b.Count < 4) return null;
            return new Hop(LispCay.So(b[0]) ?? 0, LispCay.So(b[1]) ?? 0, LispCay.So(b[2]) ?? 0, LispCay.So(b[3]) ?? 0);
        }

        private static HashSet<string> TapHandle(List<object> ds)
        {
            var s = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (object o in ds ?? new List<object>()) if (o is ObjectId id) s.Add(id.Handle.ToString());
            return s;
        }

        public static object Chay(Document doc, string cheDo, IList<ObjectId> ssIds, List<object> cumsLisp, object bbGocLisp,
                                  List<object> esKhacLisp, List<object> khungBoLisp, List<object> hopKhungBoLisp, double mKhe, double mLe)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Hop bbGoc = DocHop(bbGocLisp);
            if (bbGoc == null) return Loi(doc, "bb-goc sai dang");
            var idCua = new Dictionary<string, ObjectId>(System.StringComparer.Ordinal);
            var cums = new List<CumXc>();
            foreach (object o in cumsLisp ?? new List<object>())
            {
                List<object> c = LispCay.Ds(o);
                if (c == null || c.Count < 5) return Loi(doc, "cum sai dang");
                var cum = new CumXc
                {
                    Tid = (int)(LispCay.So(c[0]) ?? -1), Nhom = (int)(LispCay.So(c[1]) ?? -1),
                    Mirror1 = c[2] is bool bo && bo, Bb = DocHop(c[3]),
                };
                if (cum.Bb == null) return Loi(doc, "cum thieu hop bao");
                foreach (object e in LispCay.Ds(c[4]) ?? new List<object>())
                    if (e is ObjectId id) { string h = id.Handle.ToString(); cum.Ents.Add(h); idCua[h] = id; }
                cums.Add(cum);
            }
            var hopKhungBo = new List<Hop>();
            foreach (object o in hopKhungBoLisp ?? new List<object>()) hopKhungBo.Add(DocHop(o));

            var ss = new List<EntXc>(ssIds.Count);
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ssIds)
                {
                    var e = new EntXc { H = id.Handle.ToString() };
                    idCua[e.H] = id;
                    if (!id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Entity en)
                    {
                        e.Layer = en.Layer;
                        string dxf = id.ObjectClass?.DxfName ?? "";
                        e.LaPoly = dxf == "LWPOLYLINE" || dxf == "POLYLINE";
                        e.ChupCopy = en.GetXDataForApplication("GRO_CHUP_COPY") != null;
                        try
                        {
                            Extents3d x = en.GeometricExtents;
                            e.Bb = new Hop(x.MinPoint.X, x.MinPoint.Y, x.MaxPoint.X, x.MaxPoint.Y);
                        }
                        catch { e.Bb = null; }
                    }
                    ss.Add(e);
                }
                tr.Commit();
            }

            XepCum.KetQua kq = XepCum.Chay(ss, cums, bbGoc, TapHandle(esKhacLisp), TapHandle(khungBoLisp), hopKhungBo, mKhe, mLe);
            long msTinh = sw.ElapsedMilliseconds;

            int nDoi = 0, nLoi = 0;
            if (cheDo == "cs")
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    foreach (var kv in kq.DoDoi)
                    {
                        if (!idCua.TryGetValue(kv.Key, out ObjectId id) || id.IsErased) continue;
                        if (kv.Value[0] == 0.0 && kv.Value[1] == 0.0) continue;
                        try
                        {
                            var en = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                            en.TransformBy(Matrix3d.Displacement(new Vector3d(kv.Value[0], kv.Value[1], 0.0)));
                            nDoi++;
                        }
                        catch { nLoi++; }   // lisp: vla-move loi (vd layer khoa) -> bo qua
                    }
                    tr.Commit();
                }
            }
            sw.Stop();

            GhiDump(ss, cums, bbGoc);
            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                p.Dat("ab_xepcum", BanGhi(kq, cums));
                p.Dat<object>("absai_xepcum", 1e-6);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            rb.Add(new TypedValue((int)LispDataType.Double, kq.XL));
            rb.Add(new TypedValue((int)(kq.DaNe ? LispDataType.T_atom : LispDataType.Nil)));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (XepCum.ODat d in kq.Dat)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.Double, d.Dx));
                rb.Add(new TypedValue((int)LispDataType.Double, d.Dy));
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                foreach (double v in new[] { d.BbMoi.X1, d.BbMoi.Y1, d.BbMoi.X2, d.BbMoi.Y2 }) rb.Add(new TypedValue((int)LispDataType.Double, v));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (XepCum.Nhan n in kq.Nhans)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.Int32, n.Nhom));
                rb.Add(new TypedValue((int)LispDataType.Double, n.X));
                rb.Add(new TypedValue((int)LispDataType.Double, n.Y));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.Int32, kq.NSot));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (string h in kq.Sot) rb.Add(new TypedValue((int)LispDataType.ObjectId, idCua[h]));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.Int32, kq.NDimTheo));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));

            doc.Editor?.WriteMessage(string.Format(Inv, "\n[GRO] Xep cum (.NET): {0} cum, {1} doi tuong quet, tinh {2} ms{3}.",
                cums.Count, ss.Count, msTinh,
                cheDo == "cs" ? string.Format(Inv, ", da doi {0} doi tuong ({1} loi) - tong {2} ms", nDoi, nLoi, sw.ElapsedMilliseconds) : " (chi tinh de so A/B)"));
            return rb;
        }

        private static object Loi(Document doc, string ly)
        {
            doc.Editor?.WriteMessage("\n[GRO .NET] xep cum: " + ly + " -> dung ban lisp.");
            return null;
        }

        /// <summary>Bản ghi so A/B — cùng khoá với gro--xep-cum-ab bên lisp.</summary>
        public static List<SoSanh.BanGhi> BanGhi(XepCum.KetQua kq, List<CumXc> cums)
        {
            var r = new List<SoSanh.BanGhi>
            {
                new SoSanh.BanGhi("XL").Dat("x", kq.XL).Dat("dane", kq.DaNe ? "1" : "0"),
                new SoSanh.BanGhi("SOT").Dat("n", kq.NSot.ToString(Inv)).Dat("hs", string.Join(";", kq.Sot)),
                new SoSanh.BanGhi("DIM").Dat("n", kq.NDimTheo.ToString(Inv)),
            };
            for (int i = 0; i < kq.Dat.Count; i++)
                r.Add(new SoSanh.BanGhi("C" + i.ToString(Inv)).Dat("tid", cums[i].Tid.ToString(Inv))
                         .Dat("dx", kq.Dat[i].Dx).Dat("dy", kq.Dat[i].Dy));
            for (int i = 0; i < kq.Nhans.Count; i++)
                r.Add(new SoSanh.BanGhi("N" + i.ToString(Inv)).Dat("x", kq.Nhans[i].X).Dat("y", kq.Nhans[i].Y));
            return r;
        }

        private static string So(double v) => v.ToString("R", Inv);

        private static void GhiDump(List<EntXc> ss, List<CumXc> cums, Hop bbGoc)
        {
            var d = new DumpBuoc("xepcum");
            d.Dong("G", So(bbGoc.X1), So(bbGoc.Y1), So(bbGoc.X2), So(bbGoc.Y2));
            foreach (CumXc c in cums)
                d.Dong("C", c.Tid.ToString(Inv), c.Nhom.ToString(Inv), c.Mirror1 ? "1" : "0", So(c.Bb.X1), So(c.Bb.Y1), So(c.Bb.X2), So(c.Bb.Y2), c.Ents.Count.ToString(Inv));
            foreach (EntXc e in ss)
                d.Dong("E", e.H, e.Layer ?? "", e.ChupCopy ? "1" : "0", e.LaPoly ? "1" : "0",
                       e.Bb == null ? "" : So(e.Bb.X1), e.Bb == null ? "" : So(e.Bb.Y1), e.Bb == null ? "" : So(e.Bb.X2), e.Bb == null ? "" : So(e.Bb.Y2));
            d.Ghi();
        }
    }
}
