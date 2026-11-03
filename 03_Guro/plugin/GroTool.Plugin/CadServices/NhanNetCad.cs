using System;
using System.Collections.Generic;
using System.Globalization;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Runtime;
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 173 (GĐ 3a): phần CAD của phân loại nét cho nhận Dim — đọc 3 selection set lisp gửi
    /// (giữ đúng thứ tự ssname), gọi <see cref="PhanLoaiNet.Chay"/>, trả cho lisp
    /// <c>(lines-cont arcs-cont circles tiny-circles)</c>, circles = <c>(((x y) r ename) ...)</c>.
    /// </summary>
    public static class NhanNetCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static List<NetVao> Doc(Transaction tr, IList<ObjectId> ids, Dictionary<ObjectId, NetVao> nho)
        {
            var ds = new List<NetVao>();
            if (ids == null) return ds;
            foreach (ObjectId id in ids)
            {
                if (nho.TryGetValue(id, out NetVao co)) { ds.Add(co); continue; }
                var n = new NetVao { Handle = id.Handle.ToString(), Loai = "?" };
                if (!id.IsNull && !id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Entity e)
                {
                    n.Layer = e.Layer;
                    n.Linetype = e.Linetype;
                    if (e is Line) n.Loai = "LINE";
                    else if (e is Arc) n.Loai = "ARC";
                    else if (e is Circle c)
                    {
                        n.Loai = "CIRCLE";
                        PhanLoaiNet.WcsSangOcs(c.Center.X, c.Center.Y, c.Center.Z, c.Normal.X, c.Normal.Y, c.Normal.Z, out n.X, out n.Y);
                        n.R = c.Radius;
                        n.CoTam = true;
                    }
                    else n.Loai = e.GetType().Name.ToUpperInvariant();
                }
                nho[id] = n;
                ds.Add(n);
            }
            return ds;
        }

        /// <summary>Linetype của layer gốc (như (cdr (assoc 6 (tblsearch "LAYER" lay)))) hoặc null.</summary>
        private static string LtLayer(Transaction tr, Database db, string lay)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (string.IsNullOrEmpty(lay) || !lt.Has(lay)) return null;
            var ltr = (LayerTableRecord)tr.GetObject(lt[lay], OpenMode.ForRead);
            if (ltr.LinetypeObjectId.IsNull) return null;
            return (tr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord)?.Name;
        }

        public static object Chay(Document doc, string cheDo, string layGoc, IList<ObjectId> ss, IList<ObjectId> cir, IList<ObjectId> arc)
        {
            Database db = doc.Database;
            var nho = new Dictionary<ObjectId, NetVao>();
            var map = new Dictionary<NetVao, ObjectId>();
            List<NetVao> a, b, c;
            string ltGoc;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ltGoc = LtLayer(tr, db, layGoc);
                a = Doc(tr, ss, nho);
                b = Doc(tr, cir, nho);
                c = Doc(tr, arc, nho);
                tr.Commit();
            }
            foreach (var kv in nho) map[kv.Value] = kv.Key;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            PhanLoaiNet.KetQua kq = PhanLoaiNet.Chay(layGoc, ltGoc, a, b, c);
            sw.Stop();

            GhiDump(layGoc, ltGoc, a, b, c);
            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                p.Dat("ab_nhannet", BanGhi(kq));
                p.Dat<object>("absai_nhannet", 1e-6);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            ThemDs(rb, kq.Lines, map);
            ThemDs(rb, kq.Arcs, map);
            ThemTron(rb, kq.Circles, map);
            ThemTron(rb, kq.Tiny, map);
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[Xu ly net] Phan loai net (.NET): {0} LINE, {1} ARC, {2} CIRCLE ({3} rat nho) tu {4}+{5}+{6} doi tuong, {7} ms.",
                kq.Lines.Count, kq.Arcs.Count, kq.Circles.Count, kq.Tiny.Count, a.Count, b.Count, c.Count, sw.ElapsedMilliseconds));
            return rb;
        }

        /// <summary>Bản ghi so A/B: khoá = nhóm + handle; trường i = vị trí trong danh sách lisp.</summary>
        public static List<SoSanh.BanGhi> BanGhi(PhanLoaiNet.KetQua kq)
        {
            var ds = new List<SoSanh.BanGhi>();
            for (int i = 0; i < kq.Lines.Count; i++) ds.Add(new SoSanh.BanGhi("L:" + kq.Lines[i].Handle).Dat("i", i.ToString(Inv)));
            for (int i = 0; i < kq.Arcs.Count; i++) ds.Add(new SoSanh.BanGhi("A:" + kq.Arcs[i].Handle).Dat("i", i.ToString(Inv)));
            for (int i = 0; i < kq.Circles.Count; i++)
                ds.Add(new SoSanh.BanGhi("C:" + kq.Circles[i].Net.Handle).Dat("i", i.ToString(Inv))
                           .Dat("x", kq.Circles[i].X).Dat("y", kq.Circles[i].Y).Dat("r", kq.Circles[i].R));
            for (int i = 0; i < kq.Tiny.Count; i++) ds.Add(new SoSanh.BanGhi("T:" + kq.Tiny[i].Net.Handle).Dat("i", i.ToString(Inv)));
            return ds;
        }

        private static void ThemDs(ResultBuffer rb, List<NetVao> ds, Dictionary<NetVao, ObjectId> map)
        {
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (NetVao n in ds) rb.Add(new TypedValue((int)LispDataType.ObjectId, map[n]));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
        }

        private static void ThemTron(ResultBuffer rb, List<VongTron> ds, Dictionary<NetVao, ObjectId> map)
        {
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (VongTron v in ds)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.Double, v.X));
                rb.Add(new TypedValue((int)LispDataType.Double, v.Y));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
                rb.Add(new TypedValue((int)LispDataType.Double, v.R));
                rb.Add(new TypedValue((int)LispDataType.ObjectId, map[v.Net]));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
        }

        private static string So(double v) => v.ToString("R", Inv);

        private static void GhiDump(string layGoc, string ltGoc, List<NetVao> a, List<NetVao> b, List<NetVao> c)
        {
            var d = new DumpBuoc("nhannet");
            d.Dong("G", layGoc ?? "", ltGoc ?? "");
            void Ghi(string nhom, List<NetVao> ds)
            {
                foreach (NetVao n in ds)
                    d.Dong(nhom, n.Handle, n.Loai, n.Layer ?? "", n.Linetype ?? "", n.CoTam ? "1" : "0", So(n.X), So(n.Y), So(n.R));
            }
            Ghi("S", a); Ghi("C", b); Ghi("A", c);
            d.Ghi();
        }
    }
}
