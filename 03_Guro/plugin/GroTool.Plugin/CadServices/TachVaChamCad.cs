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
    /// Mục 180 (GĐ 6f): phần CAD của tách va chạm khung in. Lisp gửi các khung ((tid (x1 y1 x2 y2) (ename...)) ...)
    /// đúng thứ tự kq-tk, các tid bỏ qua (cột xếp phải), gap, max-iter. C# tính chuỗi lần dời; "cs" dời entity
    /// (mỗi entity một TransformBy với tổng độ dời). Trả (((vi-tri tid dx dy) ...) con-chong).
    /// </summary>
    public static class TachVaChamCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static object Chay(Document doc, string cheDo, List<object> khungsLisp, List<object> boQuaLisp, double gap, int maxIter)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var idCua = new Dictionary<string, ObjectId>(System.StringComparer.Ordinal);
            var khungs = new List<KhungTg>();
            foreach (object o in khungsLisp ?? new List<object>())
            {
                List<object> k = LispCay.Ds(o);
                List<object> bb = k != null && k.Count >= 3 ? LispCay.Ds(k[1]) : null;
                if (bb == null || bb.Count < 4 || LispCay.So(k[0]) == null)
                {
                    doc.Editor?.WriteMessage("\n[GRO .NET] tach va cham: khung sai dang -> dung ban lisp.");
                    return null;
                }
                var kh = new KhungTg
                {
                    Tid = (int)LispCay.So(k[0]).Value,
                    Bb = new Hop(LispCay.So(bb[0]) ?? 0, LispCay.So(bb[1]) ?? 0, LispCay.So(bb[2]) ?? 0, LispCay.So(bb[3]) ?? 0),
                };
                foreach (object e in LispCay.Ds(k[2]) ?? new List<object>())
                    if (e is ObjectId id) { string h = id.Handle.ToString(); kh.Ents.Add(h); idCua[h] = id; }
                khungs.Add(kh);
            }
            var boQua = new HashSet<int>();
            foreach (object o in boQuaLisp ?? new List<object>()) if (LispCay.So(o) is double d) boQua.Add((int)d);

            TachVaCham.KetQua kq = TachVaCham.Chay(khungs, boQua, gap, maxIter);

            int nDoi = 0, nLoi = 0;
            if (cheDo == "cs" && kq.Doi.Count > 0)
            {
                var doDoi = new Dictionary<string, double[]>(System.StringComparer.Ordinal);
                foreach (TachVaCham.LanDoi l in kq.Doi)
                    foreach (string h in l.Ents.Distinct())   // lisp: gro--remove-duplicates truoc khi doi
                    {
                        if (!doDoi.TryGetValue(h, out double[] v)) doDoi[h] = v = new double[2];
                        v[0] += l.Dx; v[1] += l.Dy;
                    }
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    foreach (var kv in doDoi)
                    {
                        if (!idCua.TryGetValue(kv.Key, out ObjectId id) || id.IsErased) continue;
                        try
                        {
                            var en = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                            en.TransformBy(Matrix3d.Displacement(new Vector3d(kv.Value[0], kv.Value[1], 0.0)));
                            nDoi++;
                        }
                        catch { nLoi++; }
                    }
                    tr.Commit();
                }
            }
            sw.Stop();

            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                var r = new List<SoSanh.BanGhi> { new SoSanh.BanGhi("CHONG").Dat("n", kq.ConChong.ToString(Inv)).Dat("luot", kq.Doi.Count.ToString(Inv)) };
                for (int i = 0; i < kq.Doi.Count; i++)
                    r.Add(new SoSanh.BanGhi("D" + i.ToString(Inv)).Dat("tid", kq.Doi[i].Tid.ToString(Inv)).Dat("dx", kq.Doi[i].Dx).Dat("dy", kq.Doi[i].Dy));
                p.Dat("ab_tachgian", r);
                p.Dat<object>("absai_tachgian", 1e-6);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (TachVaCham.LanDoi l in kq.Doi)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.Int32, l.ViTri));
                rb.Add(new TypedValue((int)LispDataType.Int32, l.Tid));
                rb.Add(new TypedValue((int)LispDataType.Double, l.Dx));
                rb.Add(new TypedValue((int)LispDataType.Double, l.Dy));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.Int32, kq.ConChong));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[GRO] Tach va cham khung in (.NET): {0} khung, {1} luot doi, con chong {2}, {3} ms{4}.",
                khungs.Count, kq.Doi.Count, kq.ConChong, sw.ElapsedMilliseconds,
                cheDo == "cs" ? string.Format(Inv, " (da doi {0} doi tuong, {1} loi)", nDoi, nLoi) : " (chi tinh de so A/B)"));
            return rb;
        }
    }
}
