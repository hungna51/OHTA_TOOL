using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Runtime;
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 176 (GĐ 5c-b): phần CAD của vòng lọc đầu thu thập Dim. Lisp gửi chính ss-all-dims (giữ thứ tự
    /// ssname), note-bbs (list số thực thật) và all-k-ents (ename). C# đọc LINE / ARC / DIMENSION, hộp bao
    /// = GeometricExtents (lisp: vla-GetBoundingBox — so A/B để chốt), trả (lines-cont arcs-cont native-dims).
    /// </summary>
    public static class LocDimNetCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static object Chay(Document doc, string cheDo, IList<ObjectId> ss, List<object> noteBbs, List<object> kEnts)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var khoi = new HashSet<ObjectId>();
            foreach (object o in kEnts ?? new List<object>()) if (o is ObjectId k) khoi.Add(k);
            var nbs = new List<Hop>();
            foreach (object o in noteBbs ?? new List<object>())
            {
                List<object> b = LispCay.Ds(o);
                nbs.Add(b != null && b.Count >= 4
                    ? new Hop(LispCay.So(b[0]) ?? 0, LispCay.So(b[1]) ?? 0, LispCay.So(b[2]) ?? 0, LispCay.So(b[3]) ?? 0)
                    : null);
            }

            var ds = new List<DimNetVao>();
            var idCua = new Dictionary<DimNetVao, ObjectId>();
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId ms = bt[BlockTableRecord.ModelSpace];
                foreach (ObjectId id in ss)
                {
                    string dxf = id.ObjectClass?.DxfName ?? "";
                    var n = new DimNetVao { Handle = id.Handle.ToString(), Loai = dxf };
                    if (dxf == "LINE" || dxf == "ARC" || dxf == "DIMENSION")
                    {
                        var e = (Entity)tr.GetObject(id, OpenMode.ForRead);
                        n.Layer = e.Layer;
                        n.Giay = e.BlockId != ms;
                        n.LaKhoi = khoi.Contains(id);
                        try
                        {
                            Extents3d x = e.GeometricExtents;
                            n.Bb = new Hop(x.MinPoint.X, x.MinPoint.Y, x.MaxPoint.X, x.MaxPoint.Y);
                        }
                        catch { n.Bb = null; }
                    }
                    ds.Add(n);
                    idCua[n] = id;
                }
                tr.Commit();
            }
            LocDimNet.KetQua kq = LocDimNet.Chay(ds, nbs);
            sw.Stop();

            GhiDump(ds, nbs);
            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                p.Dat("ab_locdim", BanGhi(kq));
                p.Dat<object>("absai_locdim", 0.0);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (List<DimNetVao> l in new[] { kq.Lines, kq.Arcs, kq.Natives })
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                foreach (DimNetVao n in l) rb.Add(new TypedValue((int)LispDataType.ObjectId, idCua[n]));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[DIM] Loc net Dim (.NET): {0} doi tuong -> {1} LINE, {2} ARC, {3} DIMENSION, {4} ms.",
                ds.Count, kq.Lines.Count, kq.Arcs.Count, kq.Natives.Count, sw.ElapsedMilliseconds));
            return rb;
        }

        /// <summary>Bản ghi so A/B: khoá = nhóm (L/A/D) + handle, trường i = vị trí trong danh sách lisp.</summary>
        public static List<SoSanh.BanGhi> BanGhi(LocDimNet.KetQua kq)
        {
            var r = new List<SoSanh.BanGhi>();
            r.AddRange(kq.Lines.Select((n, i) => new SoSanh.BanGhi("L:" + n.Handle).Dat("i", i.ToString(Inv))));
            r.AddRange(kq.Arcs.Select((n, i) => new SoSanh.BanGhi("A:" + n.Handle).Dat("i", i.ToString(Inv))));
            r.AddRange(kq.Natives.Select((n, i) => new SoSanh.BanGhi("D:" + n.Handle).Dat("i", i.ToString(Inv))));
            return r;
        }

        private static string So(double v) => v.ToString("R", Inv);

        private static void GhiDump(List<DimNetVao> ds, List<Hop> nbs)
        {
            var d = new DumpBuoc("locdim");
            foreach (Hop b in nbs) d.Dong("N", b == null ? "" : So(b.X1), b == null ? "" : So(b.Y1), b == null ? "" : So(b.X2), b == null ? "" : So(b.Y2));
            foreach (DimNetVao n in ds)
                d.Dong("E", n.Handle, n.Loai, n.Layer ?? "", n.Giay ? "1" : "0", n.LaKhoi ? "1" : "0",
                       n.Bb == null ? "" : So(n.Bb.X1), n.Bb == null ? "" : So(n.Bb.Y1), n.Bb == null ? "" : So(n.Bb.X2), n.Bb == null ? "" : So(n.Bb.Y2));
            d.Ghi();
        }
    }
}
