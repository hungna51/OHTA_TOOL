using System.Collections.Generic;
using System.Globalization;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
#endif
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 218 (GĐ 3d): đọc bản vẽ cho <see cref="CungBoGoc"/>. Tham số lisp: ss (nét trên layer), arcs-cont (ename cung),
    /// texts ((x y nguong) ...). Đầu nét = điểm đầu / cuối của mọi LINE ARC LWPOLYLINE POLYLINE SPLINE ELLIPSE trong ss
    /// + các cung arcs-cont, đạo hàm lấy tại GetParameterAtPoint(điểm) như vlax-curve-getParamAtPoint của lisp.
    /// Chỉ đọc. Trả (so-cung-bo (ename ...)).
    /// </summary>
    public static class CungBoGocCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        // LINE ARC LWPOLYLINE POLYLINE (2D/3D) SPLINE ELLIPSE - luoi POLYLINE khong phai Curve, lisp cung bo qua (diem nil)
        private static bool LaDau(Entity e) => e is Line || e is Arc || e is Polyline || e is Polyline2d || e is Polyline3d || e is Spline || e is Ellipse;

        public static object Chay(Document doc, string cheDo, object ssLisp, List<object> arcsLisp, List<object> textsLisp)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var arcIds = new List<ObjectId>();
            foreach (object o in arcsLisp ?? new List<object>()) if (o is ObjectId id) arcIds.Add(id);
            var texts = new List<CungBoGoc.TextSo>();
            foreach (object o in textsLisp ?? new List<object>())
            {
                List<object> t = LispCay.Ds(o);
                if (t == null || t.Count < 3) { doc.Editor?.WriteMessage("\n[GRO .NET] cung bo goc: text sai dang."); return null; }
                texts.Add(new CungBoGoc.TextSo { X = LispCay.So(t[0]) ?? 0, Y = LispCay.So(t[1]) ?? 0, Nguong = LispCay.So(t[2]) ?? 0 });
            }

            var ssIds = ssLisp is SelectionSet ss ? ss.GetObjectIds() : new ObjectId[0];
            var cungs = new List<CungBoGoc.Cung>();
            var daus = new List<CungBoGoc.DauNet>();
            var idTheoHandle = new Dictionary<string, ObjectId>();
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                var daCo = new HashSet<ObjectId>();
                var dauIds = new List<ObjectId>();
                foreach (ObjectId id in ssIds)
                    if (!id.IsErased && tr.GetObject(id, OpenMode.ForRead) is Entity e && LaDau(e) && daCo.Add(id)) dauIds.Add(id);
                foreach (ObjectId id in arcIds) if (daCo.Add(id)) dauIds.Add(id);
                foreach (ObjectId id in dauIds)
                {
                    if (id.IsErased || !(tr.GetObject(id, OpenMode.ForRead) is Curve c)) continue;
                    string h = id.Handle.ToString();
                    Them(daus, h, c, SafePoint(() => c.StartPoint));
                    Them(daus, h, c, SafePoint(() => c.EndPoint));
                }
                foreach (ObjectId id in arcIds)
                {
                    if (id.IsErased || !(tr.GetObject(id, OpenMode.ForRead) is Arc a)) continue;
                    string h = id.Handle.ToString();
                    idTheoHandle[h] = id;
                    cungs.Add(new CungBoGoc.Cung { Id = h, Cx = a.Center.X, Cy = a.Center.Y, R = a.Radius, A0 = a.StartAngle, A1 = a.EndAngle });
                }
                tr.Commit();
            }

            List<string> bo = CungBoGoc.Chay(cungs, daus, texts);
            sw.Stop();

            if (cheDo == "ab")
            {
                var r = new List<SoSanh.BanGhi>();
                foreach (string h in bo) r.Add(new SoSanh.BanGhi(h).Dat("bo", "1"));
                GroBuocCommands.PhienHienTai().Dat("ab_cungbogoc", r);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            rb.Add(new TypedValue((int)LispDataType.Int32, bo.Count));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (string h in bo) rb.Add(new TypedValue((int)LispDataType.ObjectId, idTheoHandle[h]));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[GRO] Cung bo goc (.NET): {0} cung, {1} dau net, {2} text, bo {3}, {4} ms{5}.",
                cungs.Count, daus.Count, texts.Count, bo.Count, sw.ElapsedMilliseconds, cheDo == "cs" ? "" : " (chi tinh de so A/B)"));
            return rb;
        }

        private static Point3d? SafePoint(System.Func<Point3d> f)
        {
            try { return f(); } catch { return null; }
        }

        private static void Them(List<CungBoGoc.DauNet> daus, string h, Curve c, Point3d? q)
        {
            if (q == null) return;
            var d = new CungBoGoc.DauNet { Id = h, X = q.Value.X, Y = q.Value.Y };
            try
            {
                Vector3d v = c.GetFirstDerivative(c.GetParameterAtPoint(q.Value));
                d.Dx = v.X; d.Dy = v.Y;
            }
            catch { }
            daus.Add(d);
        }
    }
}
