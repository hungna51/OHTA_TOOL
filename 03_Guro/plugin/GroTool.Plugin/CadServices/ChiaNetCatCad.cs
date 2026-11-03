using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 181 (GĐ 5d-a): phần CAD của chia nét cắt / kegaki về tấm (danh sách chi tiết). Lisp gửi chính
    /// ss-kakou-keg (thứ tự ssname) + các mục ((tid bb-final bb-cut (pt9...)) ...) đúng thứ tự ds-ct. Chỉ đọc
    /// bản vẽ. Trả (so-net ((pt9...) ...)) — pt9 cuối cùng từng mục.
    /// </summary>
    public static class ChiaNetCatCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static Hop DocHop(object o)
        {
            List<object> b = LispCay.Ds(o);
            if (b == null || b.Count < 4) return null;
            return new Hop(LispCay.So(b[0]) ?? 0, LispCay.So(b[1]) ?? 0, LispCay.So(b[2]) ?? 0, LispCay.So(b[3]) ?? 0);
        }

        /// <summary>(cdr (assoc 10 ed)) cho các loại có trong bộ lọc ssget của lisp (dự phòng khi không có hộp bao).</summary>
        private static Point3d? Diem10(Entity e)
        {
            switch (e)
            {
                case Line l: return l.StartPoint;
                case Circle c: return c.Center;
                case Arc a: return a.Center;
                case Ellipse el: return el.Center;
                case BlockReference br: return br.Position;
                default: return null;
            }
        }

        public static object Chay(Document doc, string cheDo, IList<ObjectId> ssIds, List<object> mucLisp)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var idCua = new Dictionary<string, ObjectId>(System.StringComparer.Ordinal);
            var ds = new List<MucCt>();
            foreach (object o in mucLisp ?? new List<object>())
            {
                List<object> m = LispCay.Ds(o);
                if (m == null || m.Count < 4 || LispCay.So(m[0]) == null)
                {
                    doc.Editor?.WriteMessage("\n[GRO .NET] danh sach chi tiet: muc sai dang -> dung ban lisp.");
                    return null;
                }
                var mc = new MucCt { Tid = (int)LispCay.So(m[0]).Value, BbF = DocHop(m[1]), BbC = DocHop(m[2]) };
                foreach (object e in LispCay.Ds(m[3]) ?? new List<object>())
                    if (e is ObjectId id) { string h = id.Handle.ToString(); mc.Pt9.Add(h); idCua[h] = id; }
                ds.Add(mc);
            }

            var ss = new List<NetCat>(ssIds.Count);
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId ms = bt[BlockTableRecord.ModelSpace];
                foreach (ObjectId id in ssIds)
                {
                    var n = new NetCat { H = id.Handle.ToString() };
                    idCua[n.H] = id;
                    if (id.IsErased || !(tr.GetObject(id, OpenMode.ForRead) is Entity e)) { n.Bo = true; ss.Add(n); continue; }
                    string lay = PhanLoaiNet.HoaAscii(e.Layer ?? "");
                    n.Bo = e.BlockId != ms || e.GetXDataForApplication("GRO_CHUP_COPY") != null || lay.EndsWith("_CHUP", System.StringComparison.Ordinal);
                    if (!n.Bo)
                    {
                        try
                        {
                            Extents3d x = e.GeometricExtents;
                            n.Bb = new Hop(x.MinPoint.X, x.MinPoint.Y, x.MaxPoint.X, x.MaxPoint.Y);
                        }
                        catch
                        {
                            Point3d? p = Diem10(e);
                            if (p.HasValue) n.Bb = new Hop(p.Value.X - 5.0, p.Value.Y - 5.0, p.Value.X + 5.0, p.Value.Y + 5.0);
                        }
                    }
                    ss.Add(n);
                }
                tr.Commit();
            }

            ChiaNetCat.KetQua kq = ChiaNetCat.Chay(ss, ds);
            sw.Stop();

            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                var r = new List<SoSanh.BanGhi>();
                for (int i = 0; i < ds.Count; i++)
                    r.Add(new SoSanh.BanGhi("C" + i.ToString(Inv)).Dat("tid", ds[i].Tid.ToString(Inv))
                             .Dat("n", kq.Pt9[i].Count.ToString(Inv)).Dat("hs", string.Join(";", kq.Pt9[i])));
                p.Dat("ab_dsct", r);
                p.Dat<object>("absai_dsct", 0.0);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            rb.Add(new TypedValue((int)LispDataType.Int32, kq.SoNet));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (List<string> l in kq.Pt9)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                foreach (string h in l) rb.Add(new TypedValue((int)LispDataType.ObjectId, idCua[h]));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[GRO] Danh sach chi tiet (.NET): chia {0} net cat / kegaki cho {1} tam, {2} ms.",
                kq.SoNet, ds.Count, sw.ElapsedMilliseconds));
            return rb;
        }
    }
}
