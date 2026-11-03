using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 183 (GĐ 5d-b): phần CAD của đóng khung + vẽ Khung_IN. Lisp gửi mỗi tấm
    /// (tid t-bb (hop-hinh...) ((hop-dim chac) ...) (ename-khoi...) co-khoi) đúng thứ tự text-reps.
    /// C# tính hộp cụm bango bằng GeometricExtents (lisp: vla-GetBoundingBox), gọi <see cref="DongKhung.Chay"/>;
    /// "cs" vẽ LWPOLYLINE kín 4 đỉnh trên layer Khung_IN màu 4 (như gro--ve-khung-rect), một transaction.
    /// Trả ((bb-final ename-khung-hoac-nil (co-giu-dim...)) ...).
    /// </summary>
    public static class DongKhungCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static Hop DocHop(object o)
        {
            List<object> b = LispCay.Ds(o);
            if (b == null || b.Count < 4) return null;
            return new Hop(LispCay.So(b[0]) ?? 0, LispCay.So(b[1]) ?? 0, LispCay.So(b[2]) ?? 0, LispCay.So(b[3]) ?? 0);
        }

        /// <summary>gro--ensure-layer "Khung_IN" 4 "Continuous" + mở khoá / bật / bỏ đóng băng như gro--ve-khung-rect.</summary>
        private static void BaoDamLayer(Transaction tr, Database db, string ten)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            LayerTableRecord l;
            if (!lt.Has(ten))
            {
                lt.UpgradeOpen();
                l = new LayerTableRecord { Name = ten, Color = Color.FromColorIndex(ColorMethod.ByAci, 4) };
                var lts = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (lts.Has("Continuous")) l.LinetypeObjectId = lts["Continuous"];
                lt.Add(l);
                tr.AddNewlyCreatedDBObject(l, true);
            }
            else l = (LayerTableRecord)tr.GetObject(lt[ten], OpenMode.ForWrite);
            if (l.IsFrozen) l.IsFrozen = false;
            if (l.IsOff) l.IsOff = false;
            if (l.IsLocked) l.IsLocked = false;
        }

        public static object Chay(Document doc, string cheDo, List<object> tamLisp)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tams = new List<TamDk>();
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (object o in tamLisp ?? new List<object>())
                {
                    List<object> m = LispCay.Ds(o);
                    if (m == null || m.Count < 6 || LispCay.So(m[0]) == null || DocHop(m[1]) == null)
                    {
                        doc.Editor?.WriteMessage("\n[GRO .NET] dong khung: tam sai dang -> dung ban lisp.");
                        return null;
                    }
                    var t = new TamDk { Tid = (int)LispCay.So(m[0]).Value, TBb = DocHop(m[1]), CoKhoi = m[5] is bool b && b };
                    foreach (object h in LispCay.Ds(m[2]) ?? new List<object>()) { Hop hb = DocHop(h); if (hb != null) t.HinhBb.Add(hb); }
                    foreach (object d in LispCay.Ds(m[3]) ?? new List<object>())
                    {
                        List<object> dp = LispCay.Ds(d);
                        Hop db2 = dp != null && dp.Count >= 1 ? DocHop(dp[0]) : null;
                        if (db2 == null) { doc.Editor?.WriteMessage("\n[GRO .NET] dong khung: Dim sai dang -> dung ban lisp."); return null; }
                        t.Dims.Add(new DimDk { Bb = db2, ChacChan = dp.Count >= 2 && dp[1] is bool c && c });
                    }
                    foreach (object e in LispCay.Ds(m[4]) ?? new List<object>())
                    {
                        if (!(e is ObjectId id) || id.IsNull || id.IsErased) continue;
                        if (!(tr.GetObject(id, OpenMode.ForRead) is Entity en)) continue;
                        try
                        {
                            Extents3d x = en.GeometricExtents;
                            t.KhoiBb = DongKhung.Gop(t.KhoiBb, new Hop(x.MinPoint.X, x.MinPoint.Y, x.MaxPoint.X, x.MaxPoint.Y));
                        }
                        catch { /* gro--bbox nil -> bo qua */ }
                    }
                    tams.Add(t);
                }
                tr.Commit();
            }

            List<DongKhung.KetQua> kq = DongKhung.Chay(tams);
            var khung = new ObjectId[kq.Count];
            if (cheDo == "cs")
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BaoDamLayer(tr, db, "Khung_IN");
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    for (int i = 0; i < kq.Count; i++)
                    {
                        Hop b = kq[i].BbFinal;
                        var pl = new Polyline();
                        pl.AddVertexAt(0, new Point2d(b.X1, b.Y1), 0, 0, 0);
                        pl.AddVertexAt(1, new Point2d(b.X2, b.Y1), 0, 0, 0);
                        pl.AddVertexAt(2, new Point2d(b.X2, b.Y2), 0, 0, 0);
                        pl.AddVertexAt(3, new Point2d(b.X1, b.Y2), 0, 0, 0);
                        pl.Closed = true;
                        pl.SetDatabaseDefaults();
                        pl.Layer = "Khung_IN";
                        pl.ColorIndex = 4;
                        khung[i] = ms.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                    }
                    tr.Commit();
                }
            }
            sw.Stop();

            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                var r = new List<SoSanh.BanGhi>();
                for (int i = 0; i < kq.Count; i++)
                {
                    Hop b = kq[i].BbFinal;
                    r.Add(new SoSanh.BanGhi("K" + i.ToString(Inv)).Dat("tid", tams[i].Tid.ToString(Inv))
                             .Dat("x1", b.X1).Dat("y1", b.Y1).Dat("x2", b.X2).Dat("y2", b.Y2)
                             .Dat("nd", kq[i].GiuDim.Count(g => g).ToString(Inv)));
                }
                p.Dat("ab_dongkhung", r);
                p.Dat<object>("absai_dongkhung", 1e-6);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            for (int i = 0; i < kq.Count; i++)
            {
                Hop b = kq[i].BbFinal;
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                foreach (double v in new[] { b.X1, b.Y1, b.X2, b.Y2 }) rb.Add(new TypedValue((int)LispDataType.Double, v));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
                if (khung[i].IsNull) rb.Add(new TypedValue((int)LispDataType.Nil));
                else rb.Add(new TypedValue((int)LispDataType.ObjectId, khung[i]));
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                foreach (bool g in kq[i].GiuDim) rb.Add(new TypedValue((int)LispDataType.Int32, g ? 1 : 0));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[GRO] Dong khung (.NET): {0} tam, {1} ms{2}.", kq.Count, sw.ElapsedMilliseconds,
                cheDo == "cs" ? " (da ve Khung_IN)" : " (chi tinh de so A/B)"));
            return rb;
        }
    }
}
