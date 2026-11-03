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
    /// Mục 177 (GĐ 3c): phần CAD của dò vòng kín ở bước nhận Dim. Lisp gửi chính ss (thứ tự ssname),
    /// lines-cont, arcs-cont; C# đọc điểm đầu / cuối (Curve.StartPoint / EndPoint = vlax-curve), dò vòng kín,
    /// trả (vk-geo lines-cont arcs-cont) — hai danh sách sau đã bỏ nét thuộc vòng kín, giữ thứ tự.
    /// </summary>
    public static class VongKinCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static object Chay(Document doc, string cheDo, string layGoc, IList<ObjectId> ss, List<object> lines, List<object> arcs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var ds = new List<NetVk>();
            var idCua = new Dictionary<NetVk, ObjectId>();
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss)
                {
                    var n = new NetVk { Handle = id.Handle.ToString(), Loai = id.ObjectClass?.DxfName ?? "" };
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity e)
                    {
                        n.Layer = e.Layer;
                        if (e is Polyline pl) n.Kin = pl.Closed;
                        else if (e is Polyline2d p2) n.Kin = p2.Closed;
                        else if (e is Polyline3d p3) n.Kin = p3.Closed;
                        if (e is Curve c)
                        {
                            try
                            {
                                var a = c.StartPoint; var b = c.EndPoint;
                                n.X1 = a.X; n.Y1 = a.Y; n.X2 = b.X; n.Y2 = b.Y; n.CoDau = true;
                            }
                            catch { n.CoDau = false; }
                        }
                    }
                    ds.Add(n);
                    idCua[n] = id;
                }
                tr.Commit();
            }
            List<NetVk> vk = VongKin.TrenLayer(layGoc, ds);
            var trong = new HashSet<ObjectId>(vk.Select(n => idCua[n]));
            sw.Stop();

            GhiDump(layGoc, ds);
            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                p.Dat("ab_vongkin", vk.Select((n, i) => new SoSanh.BanGhi(n.Handle).Dat("i", i.ToString(Inv))).ToList());
                p.Dat<object>("absai_vongkin", 0.0);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (NetVk n in vk) rb.Add(new TypedValue((int)LispDataType.ObjectId, idCua[n]));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            foreach (List<object> l in new[] { lines, arcs })
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                foreach (object o in l ?? new List<object>())
                    if (o is ObjectId id && !trong.Contains(id)) rb.Add(new TypedValue((int)LispDataType.ObjectId, id));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[Xu ly net] Do vong kin (.NET): {0} doi tuong -> {1} net vong kin, {2} ms.",
                ds.Count, vk.Count, sw.ElapsedMilliseconds));
            return rb;
        }

        private static string So(double v) => v.ToString("R", Inv);

        private static void GhiDump(string layGoc, List<NetVk> ds)
        {
            var d = new DumpBuoc("vongkin");
            d.Dong("G", layGoc ?? "");
            foreach (NetVk n in ds)
                d.Dong("E", n.Handle, n.Loai, n.Layer ?? "", n.Kin ? "1" : "0", n.CoDau ? "1" : "0", So(n.X1), So(n.Y1), So(n.X2), So(n.Y2));
            d.Ghi();
        }
    }
}
