using System.Collections.Generic;
using System.Globalization;
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
    /// Mục 215 (GĐ 5b-a): đọc dữ liệu lisp gửi, gọi <see cref="GhepHinhChieu.Chay"/>. Không đụng bản vẽ —
    /// lisp tự áp các lần ghép. Tham số: tat-ca ((sid x1 y1 x2 y2 vid|-1) ...), cho (sid ...),
    /// cum ((tid vid|-1 (sid ...)) ...) hình mới ghép đứng đầu, max-ray, co-vung 1/0, so-vung, ds-cat ((6 số) ...).
    /// Trả (so-lan ((sid tid d loai) ...)) — luôn khác nil.
    /// </summary>
    public static class GhepHinhChieuCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static int? Vung(object o) => LispCay.So(o) is double d && d >= 0 ? (int)d : (int?)null;

        public static object Chay(Document doc, string cheDo, List<object> a)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tatCa = new List<GhepHinhChieu.Hinh>();
            foreach (object o in LispCay.Ds(a[0]) ?? new List<object>())
            {
                List<object> s = LispCay.Ds(o);
                if (s == null || s.Count < 6) return Sai(doc, "hinh");
                tatCa.Add(new GhepHinhChieu.Hinh
                {
                    Id = (int)(LispCay.So(s[0]) ?? 0), X1 = LispCay.So(s[1]) ?? 0, Y1 = LispCay.So(s[2]) ?? 0,
                    X2 = LispCay.So(s[3]) ?? 0, Y2 = LispCay.So(s[4]) ?? 0, Vung = Vung(s[5]),
                });
            }
            var cho = new List<int>();
            foreach (object o in LispCay.Ds(a[1]) ?? new List<object>()) cho.Add((int)(LispCay.So(o) ?? 0));
            var cum = new List<GhepHinhChieu.Cum>();
            foreach (object o in LispCay.Ds(a[2]) ?? new List<object>())
            {
                List<object> c = LispCay.Ds(o);
                if (c == null || c.Count < 3) return Sai(doc, "cum");
                var cu = new GhepHinhChieu.Cum { Tid = (int)(LispCay.So(c[0]) ?? 0), Vung = Vung(c[1]) };
                foreach (object h in LispCay.Ds(c[2]) ?? new List<object>()) cu.HinhIds.Add((int)(LispCay.So(h) ?? 0));
                cum.Add(cu);
            }
            var dsCat = new List<GhepHinhChieu.VetCat>();
            foreach (object o in LispCay.Ds(a[6]) ?? new List<object>())
            {
                List<object> v = LispCay.Ds(o);
                if (v == null || v.Count < 6) return Sai(doc, "net cat");
                dsCat.Add(new GhepHinhChieu.VetCat
                {
                    XDiv = LispCay.So(v[0]) ?? 0, Gap = LispCay.So(v[1]) ?? 0, X1 = LispCay.So(v[2]) ?? 0,
                    Y1 = LispCay.So(v[3]) ?? 0, X2 = LispCay.So(v[4]) ?? 0, Y2 = LispCay.So(v[5]) ?? 0,
                });
            }

            List<GhepHinhChieu.LanGhep> kq;
            try
            {
                kq = GhepHinhChieu.Chay(tatCa, cho, cum, LispCay.So(a[3]) ?? 0, (LispCay.So(a[4]) ?? 0) != 0,
                                        (int)(LispCay.So(a[5]) ?? 0), dsCat);
            }
            catch (KeyNotFoundException) { return Sai(doc, "id hinh"); }
            sw.Stop();

            var dump = new DumpBuoc("ghephc");
            foreach (GhepHinhChieu.LanGhep g in kq)
                dump.Dong("G", g.Hinh.ToString(Inv), g.Tam.ToString(Inv), g.D.ToString("0.###", Inv), g.Loai.ToString(Inv));
            dump.Ghi();

            if (cheDo == "ab")
            {
                var r = new List<SoSanh.BanGhi>();
                foreach (GhepHinhChieu.LanGhep g in kq)
                    r.Add(new SoSanh.BanGhi("H" + g.Hinh.ToString(Inv)).Dat("tam", g.Tam.ToString(Inv)));
                GroBuocCommands.PhienHienTai().Dat("ab_ghephc", r);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            rb.Add(new TypedValue((int)LispDataType.Int32, kq.Count));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (GhepHinhChieu.LanGhep g in kq)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.Int32, g.Hinh));
                rb.Add(new TypedValue((int)LispDataType.Int32, g.Tam));
                rb.Add(new TypedValue((int)LispDataType.Double, g.D));
                rb.Add(new TypedValue((int)LispDataType.Int32, g.Loai));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[GRO] Ghep hinh chieu (.NET): {0} hinh cho, {1} cum, {2} lan ghep, {3} ms{4}.",
                cho.Count, cum.Count, kq.Count, sw.ElapsedMilliseconds, cheDo == "cs" ? "" : " (chi tinh de so A/B)"));
            return rb;
        }

        private static object Sai(Document doc, string gi)
        {
            doc.Editor?.WriteMessage("\n[GRO .NET] ghep hinh chieu: du lieu " + gi + " sai dang.");
            return null;
        }
    }
}
