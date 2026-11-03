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
    /// Mục 175 (GĐ 3b): phần CAD của khớp Dim dài ở bước xử lý nét. Cùng tham số mục 174
    /// (texts-lin dạng list số thực thật + lines-cont), trả <c>(res-dim-ents ((chi-so so-line) ...))</c>:
    /// res-dim-ents đúng thứ tự lisp; danh sách sau = chữ số có đường dim ghép chuỗi (lisp in thông báo mục 169).
    /// </summary>
    public static class DimDai3bCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static object Chay(Document doc, string cheDo, List<object> texts, List<object> lineIds)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string loi = DimDaiCad.DocVao(doc, texts, lineIds, out List<TextDim> ds, out List<DoanThang> lines, out Dictionary<DoanThang, ObjectId> idCua);
            if (loi != null)
            {
                doc.Editor?.WriteMessage("\n[GRO .NET] khop Dim dai 3b: " + loi + " -> dung ban lisp.");
                return null;
            }
            DimDai3b.KetQua kq = DimDai3b.Chay(ds, lines);
            sw.Stop();

            DimDaiCad.GhiDump("dimdai3b", ds, lines);
            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                p.Dat("ab_dimdai3b", BanGhi(kq));
                p.Dat<object>("absai_dimdai3b", 0.0);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (DoanThang d in kq.Net) rb.Add(new TypedValue((int)LispDataType.ObjectId, idCua[d]));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (int[] c in kq.Chuoi)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.Int32, c[0]));
                rb.Add(new TypedValue((int)LispDataType.Int32, c[1]));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            doc.Editor?.WriteMessage(string.Format(Inv, "\n[Xu ly net] Khop Dim dai (.NET): {0} chu so, {1} LINE -> {2} net Dim, {3} ms.",
                ds.Count, lines.Count, kq.Net.Count, sw.ElapsedMilliseconds));
            return rb;
        }

        /// <summary>Bản ghi so A/B: một bản ghi / net Dim, khoá = handle, trường i = vị trí trong res-dim-ents.</summary>
        public static List<SoSanh.BanGhi> BanGhi(DimDai3b.KetQua kq) =>
            kq.Net.Select((d, i) => new SoSanh.BanGhi(d.Handle).Dat("i", i.ToString(Inv))).ToList();
    }
}
