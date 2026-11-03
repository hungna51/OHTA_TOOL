using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
#endif
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 174 (GĐ 5c-a): phần CAD của khớp Dim dài khi thu thập Dim. Lisp gửi texts-lin dạng
    /// list số thực thật ((ename v pt rot h bb) ...) + lines-cont (ename) đúng thứ tự; C# đọc đầu
    /// mút LINE, gọi <see cref="DimDai.KhopHet"/>, trả danh sách SONG SONG texts-lin: mỗi phần tử
    /// nil hoặc ((mem...) (ticks...) (pmx pmy) (rdx rdy)).
    /// </summary>
    public static class DimDaiCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>
        /// Đọc tham số chung của mục 174 / 175: texts ((ename v pt rot h bb) ...) + lines (ename LINE ...).
        /// Trả chuỗi lý do lỗi, hoặc null nếu đọc được hết.
        /// </summary>
        public static string DocVao(Document doc, List<object> texts, List<object> lineIds,
                                    out List<TextDim> ds, out List<DoanThang> lines, out Dictionary<DoanThang, ObjectId> idCua)
        {
            ds = new List<TextDim>();
            lines = new List<DoanThang>();
            idCua = new Dictionary<DoanThang, ObjectId>();
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (object o in lineIds ?? new List<object>())
                {
                    if (!(o is ObjectId id) || id.IsNull || id.IsErased) return "phan tu lines-cont khong phai ename";
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Line ln)) return "lines-cont co doi tuong khong phai LINE";
                    var d = new DoanThang(id.Handle.ToString(), ln.StartPoint.X, ln.StartPoint.Y, ln.EndPoint.X, ln.EndPoint.Y);
                    lines.Add(d);
                    idCua[d] = id;
                }
                foreach (object o in texts ?? new List<object>())
                {
                    List<object> it = LispCay.Ds(o);
                    if (it == null || it.Count < 6 || !(it[0] is ObjectId tid)) return "phan tu texts-lin sai dang";
                    List<object> pt = LispCay.Ds(it[2]);
                    List<object> bb = LispCay.Ds(it[5]);
                    double? rot = LispCay.So(it[3]), h = LispCay.So(it[4]);
                    if (pt == null || pt.Count < 2 || rot == null || h == null) return "texts-lin thieu pt / rot / h";
                    var t = new TextDim
                    {
                        Handle = tid.Handle.ToString(), V = LispCay.So(it[1]),
                        Px = LispCay.So(pt[0]) ?? 0, Py = LispCay.So(pt[1]) ?? 0, Rot = rot.Value, H = h.Value,
                    };
                    if (bb != null && bb.Count >= 4)
                        t.Bb = new Hop(LispCay.So(bb[0]) ?? 0, LispCay.So(bb[1]) ?? 0, LispCay.So(bb[2]) ?? 0, LispCay.So(bb[3]) ?? 0);
                    ds.Add(t);
                }
                tr.Commit();
            }
            return null;
        }

        public static object Chay(Document doc, string cheDo, List<object> texts, List<object> lineIds)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string loi = DocVao(doc, texts, lineIds, out List<TextDim> ds, out List<DoanThang> lines, out Dictionary<DoanThang, ObjectId> idCua);
            if (loi != null) return Loi(doc, loi);
            long msDoc = sw.ElapsedMilliseconds;
            List<KqDimDai> kq = DimDai.KhopHet(ds, lines);
            sw.Stop();

            GhiDump("dimdai", ds, lines);
            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                p.Dat("ab_dimdai", BanGhi(ds, kq));
                p.Dat<object>("absai_dimdai", 1e-6);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (KqDimDai k in kq)
            {
                if (k == null) { rb.Add(new TypedValue((int)LispDataType.Nil)); continue; }
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                ThemIds(rb, k.Mem, idCua);
                ThemIds(rb, k.Ticks, idCua);
                ThemSo(rb, k.PmX, k.PmY);
                ThemSo(rb, k.RdX, k.RdY);
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[DIM] Khop Dim dai (.NET): {0}/{1} chu so co duong dim, {2} LINE, {3} ms (doc {4} ms).",
                kq.Count(x => x != null), ds.Count, lines.Count, sw.ElapsedMilliseconds, msDoc));
            return rb;
        }

        private static object Loi(Document doc, string ly)
        {
            doc.Editor?.WriteMessage("\n[GRO .NET] khop Dim dai: " + ly + " -> dung ban lisp.");
            return null;
        }

        /// <summary>Bản ghi so A/B: khoá = vị trí + handle chữ số; mem / ticks theo đúng thứ tự.</summary>
        public static List<SoSanh.BanGhi> BanGhi(List<TextDim> ds, List<KqDimDai> kq)
        {
            var r = new List<SoSanh.BanGhi>();
            for (int i = 0; i < ds.Count; i++)
            {
                var b = new SoSanh.BanGhi(i.ToString(Inv) + ":" + ds[i].Handle);
                KqDimDai k = kq[i];
                if (k == null) b.Dat("co", "0");
                else b.Dat("co", "1").Dat("mem", string.Join(";", k.Mem.Select(x => x.Handle)))
                      .Dat("tick", string.Join(";", k.Ticks.Select(x => x.Handle)))
                      .Dat("pmx", k.PmX).Dat("pmy", k.PmY).Dat("rdx", k.RdX).Dat("rdy", k.RdY);
                r.Add(b);
            }
            return r;
        }

        private static void ThemIds(ResultBuffer rb, List<DoanThang> ds, Dictionary<DoanThang, ObjectId> idCua)
        {
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (DoanThang d in ds) rb.Add(new TypedValue((int)LispDataType.ObjectId, idCua[d]));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
        }

        private static void ThemSo(ResultBuffer rb, params double[] v)
        {
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (double x in v) rb.Add(new TypedValue((int)LispDataType.Double, x));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
        }

        private static string So(double v) => v.ToString("R", Inv);

        public static void GhiDump(string buoc, List<TextDim> ds, List<DoanThang> lines)
        {
            var d = new DumpBuoc(buoc);
            foreach (TextDim t in ds)
                d.Dong("T", t.Handle, t.V.HasValue ? So(t.V.Value) : "", So(t.Px), So(t.Py), So(t.Rot), So(t.H),
                       t.Bb == null ? "" : So(t.Bb.X1), t.Bb == null ? "" : So(t.Bb.Y1),
                       t.Bb == null ? "" : So(t.Bb.X2), t.Bb == null ? "" : So(t.Bb.Y2));
            foreach (DoanThang l in lines) d.Dong("L", l.Handle, So(l.X1), So(l.Y1), So(l.X2), So(l.Y2));
            d.Ghi();
        }
    }
}
