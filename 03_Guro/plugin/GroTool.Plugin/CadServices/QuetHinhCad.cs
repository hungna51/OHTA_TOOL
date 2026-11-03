using System;
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
    /// Mục 172 (GĐ 5a): phần CAD của quét hình — đọc nét 0-KAKOU ở Model Space (thay
    /// <c>ssget "X"</c> + <c>gro--bbox</c>), gọi <see cref="QuetHinh.Gom"/>, trả kết quả cho lisp
    /// đúng cấu trúc <c>gro--quet-hinh-primitives</c>: <c>((id (ename...) (x1 y1 x2 y2) (cx cy)) ...)</c>.
    /// </summary>
    public static class QuetHinhCad
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static bool LaNet(Entity e) =>
            e is Line || e is Arc || e is Circle || e is Ellipse || e is Spline ||
            e is Polyline || e is Polyline2d || e is Polyline3d;

        /// <summary>
        /// Đọc nét 0-KAKOU (Model Space) theo THỨ TỰ CSDL. Lisp: ssget "X" của ZWCAD trả NGƯỢC CSDL,
        /// rồi cons đảo lại -> lisp duyệt XUÔI CSDL. Đã chốt bằng so A/B thật (mục 172): đảo thứ tự
        /// thì id các hình cùng diện tích lệch lisp.
        /// </summary>
        public static List<(ObjectId id, Hop bb)> DocNet(Database db)
        {
            var ds = new List<(ObjectId, Hop)>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    if (id.IsErased) continue;
                    var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e == null || !LaNet(e)) continue;
                    if (!string.Equals(e.Layer, "0-KAKOU", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        Extents3d x = e.GeometricExtents;
                        ds.Add((id, new Hop(x.MinPoint.X, x.MinPoint.Y, x.MaxPoint.X, x.MaxPoint.Y)));
                    }
                    catch { /* lisp: gro--bbox nil -> bo qua */ }
                }
                tr.Commit();
            }
            return ds;
        }

        /// <summary>
        /// Chạy cả bước. duLieu: "K|x1,y1,x2,y2" (khung phạm vi), "P|x,y" (điểm chèn ghi chú).
        /// cheDo "ab": ghi bản ghi so sánh vào phiên ("ab_quethinh"). Trả ResultBuffer cho lisp:
        /// (hinh loi-khung) hoặc chuỗi "KHONG_KAKOU" khi 0-KAKOU không có nét nào.
        /// </summary>
        public static object Chay(Document doc, string cheDo, IList<string> duLieu)
        {
            var kBoxes = new List<Hop>();
            var diem = new List<Diem>();
            foreach (string s in duLieu)
            {
                if (s.StartsWith("K|") && DatMaKhac.DocHop(s.Substring(2), out Hop k)) kBoxes.Add(k);
                else if (s.StartsWith("P|"))
                {
                    string[] p = s.Substring(2).Split(',');
                    if (p.Length == 2 && double.TryParse(p[0], NumberStyles.Float, Inv, out double x) &&
                        double.TryParse(p[1], NumberStyles.Float, Inv, out double y))
                        diem.Add(new Diem(x, y));
                }
            }

            List<(ObjectId id, Hop bb)> nets = DocNet(doc.Database);
            if (nets.Count == 0) return "KHONG_KAKOU";
            var dauVao = nets.Select((t, i) => new NetKakou(i, t.bb)).ToList();
            QuetHinh.KetQua kq = QuetHinh.Gom(dauVao, kBoxes, diem);

            // dump de phat lai ngoai CAD
            var d = new DumpBuoc("quethinh");
            foreach (Hop k in kBoxes) d.Dong("K", So(k.X1), So(k.Y1), So(k.X2), So(k.Y2));
            foreach (Diem p in diem) d.Dong("P", So(p.X), So(p.Y));
            for (int i = 0; i < nets.Count; i++)
                d.Dong("N", nets[i].id.Handle.ToString(), So(nets[i].bb.X1), So(nets[i].bb.Y1), So(nets[i].bb.X2), So(nets[i].bb.Y2));
            d.Ghi();

            if (cheDo == "ab")
            {
                var p = GroBuocCommands.PhienHienTai();
                p.Dat("ab_quethinh", BanGhi(kq.Hinh, nets));
                p.Dat<object>("absai_quethinh", 0.01);
            }

            // ResultBuffer: (hinh loi-khung)
            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (HinhPrim h in kq.Hinh)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                rb.Add(new TypedValue((int)LispDataType.Int32, h.Id));
                ThemEname(rb, h.ThanhVien, nets);
                ThemSo(rb, h.Bb.X1, h.Bb.Y1, h.Bb.X2, h.Bb.Y2);
                ThemSo(rb, h.Cx, h.Cy);
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            // loi-khung: ((ename...) (x1 y1 x2 y2) dien-tich) - dung dang shape so bo cua lisp
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (HinhPrim h in kq.LoiKhung)
            {
                rb.Add(new TypedValue((int)LispDataType.ListBegin));
                ThemEname(rb, h.ThanhVien, nets);
                ThemSo(rb, h.Bb.X1, h.Bb.Y1, h.Bb.X2, h.Bb.Y2);
                rb.Add(new TypedValue((int)LispDataType.Double, h.DienTich));
                rb.Add(new TypedValue((int)LispDataType.ListEnd));
            }
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
            return rb;
        }

        /// <summary>Bản ghi so A/B: khoá = handle NHỎ NHẤT (so chuỗi thứ tự mã) của hình.</summary>
        public static List<SoSanh.BanGhi> BanGhi(IEnumerable<HinhPrim> hinh, List<(ObjectId id, Hop bb)> nets)
        {
            var ds = new List<SoSanh.BanGhi>();
            foreach (HinhPrim h in hinh)
            {
                List<string> hs = h.ThanhVien.Select(i => nets[i].id.Handle.ToString()).OrderBy(x => x, StringComparer.Ordinal).ToList();
                ds.Add(new SoSanh.BanGhi(hs[0]).Dat("id", h.Id.ToString(Inv)).Dat("n", hs.Count.ToString(Inv))
                           .Dat("hs", string.Join(";", hs))
                           .Dat("x1", h.Bb.X1).Dat("y1", h.Bb.Y1).Dat("x2", h.Bb.X2).Dat("y2", h.Bb.Y2));
            }
            return ds;
        }

        private static void ThemEname(ResultBuffer rb, List<int> tv, List<(ObjectId id, Hop bb)> nets)
        {
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (int i in tv) rb.Add(new TypedValue((int)LispDataType.ObjectId, nets[i].id));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
        }

        private static void ThemSo(ResultBuffer rb, params double[] v)
        {
            rb.Add(new TypedValue((int)LispDataType.ListBegin));
            foreach (double x in v) rb.Add(new TypedValue((int)LispDataType.Double, x));
            rb.Add(new TypedValue((int)LispDataType.ListEnd));
        }

        private static string So(double v) => v.ToString("R", Inv);
    }
}
