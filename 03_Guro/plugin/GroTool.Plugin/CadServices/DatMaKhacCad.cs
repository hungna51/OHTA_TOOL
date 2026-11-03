using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Mục 170: phần CAD của bước 6g (đặt mã khắc + F-name vào trong hình). Lisp đã chọn tấm và
    /// gửi sang; ở đây: lấy mẫu nét 0-KAKOU / 3-KEGAKI / TAPU_TMT quanh các tấm (thay
    /// gro--mk-quet-net + gro--ent->diem), gọi DatMaKhac.ChayHet, rồi dời / xoay / thu nhỏ cụm
    /// (thay gro--mk-ap-cum) và gắn cờ XData lên bango (thay gro--gan-co-tam).
    /// </summary>
    public static class DatMaKhacCad
    {
        private const string AppMeta = "GRO_METADATA";
        private const string AppChup = "GRO_CHUP_COPY";
        private static readonly string[] LayerNet = { "0-KAKOU", "3-KEGAKI", "TAPU_TMT" };

        /// <summary>Chạy cả bước. Trả chuỗi báo cáo cho lisp (null = không có tấm nào).</summary>
        public static string Chay(Document doc, IEnumerable<string> duLieu)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;
            var dong = new List<DatMaKhac.DongTam>();
            var vatCan = new List<Hop>();
            DatMaKhac.DocDuLieu(duLieu, dong, vatCan, out int loi);
            if (loi > 0) ed.WriteMessage("\n[GRO .NET] Dat ma khac: bo qua " + loi + " dong du lieu hong tu lisp.");
            if (dong.Count == 0) return null;

            var sw = Stopwatch.StartNew();
            var tams = new List<TamCanDat>();
            foreach (var d in dong) tams.Add(d.Tam);

            DocumentLock khoa = null;
            try { khoa = doc.LockDocument(); }
            catch { /* dang trong ngu canh lenh: da khoa san */ }
            try
            {
                var chanDoan = new List<string>();
                List<NetMau> net = QuetNet(db, tams, chanDoan, out int soLoiMau);
                double tLayMau = sw.Elapsed.TotalSeconds;
                if (soLoiMau > 0)
                    ed.WriteMessage("\n[GRO .NET] !!! " + soLoiMau + " net (0-KAKOU/3-KEGAKI/TAPU_TMT) khong lay mau duoc - xem file du lieu.");
                string fileDump = GhiDump(duLieu, net, chanDoan);
                if (fileDump != null) ed.WriteMessage("\n[GRO .NET] Du lieu dat ma khac (de phat lai): " + fileDump);

                KetQuaDatMk kq = DatMaKhac.ChayHet(tams, net, vatCan);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    for (int i = 0; i < kq.Tams.Count; i++)
                    {
                        KetQuaTam r = kq.Tams[i];
                        DatMaKhac.DongTam d = dong[i];
                        if ((i + 1) % 10 == 0)
                            ed.WriteMessage("\n[GRO] Dat ma khac vao hinh... " + (i + 1) + "/" + kq.Tams.Count);
                        ed.WriteMessage("\n" + r.DongChanDoan);

                        ObjectId idBango = LayId(db, d.HandleBango);
                        if (r.Dat)
                        {
                            ApCum(tr, db, d.HandleCum, d.Tam.BbCum, r);
                            if (r.TiLe < 1.0) GanCo(tr, idBango, DatMaKhac.CoThuNho);
                            if (r.BoKeg) GanCo(tr, idBango, DatMaKhac.CoDeKegaki);
                        }
                        else GanCo(tr, idBango, DatMaKhac.CoChuaDat);
                    }
                    tr.Commit();
                }

                ed.WriteMessage("\n[GRO] Dat ma khac (.NET): " + kq.Tong + " tam, " +
                                sw.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) +
                                " giay (lay mau net " + tLayMau.ToString("0.0", CultureInfo.InvariantCulture) +
                                " giay, " + net.Count + " net)");
                if (kq.ThongBao != null) ed.WriteMessage("\n[GRO] " + kq.ThongBao);
                return kq.ThongBao;
            }
            finally
            {
                khoa?.Dispose();
            }
        }

        // ------------------------------------------------------------------
        // Lấy mẫu nét — gro--mk-quet-net + gro--ent->diem
        // ------------------------------------------------------------------

        /// <summary>
        /// Nét LINE/ARC/CIRCLE/ELLIPSE/SPLINE/LWPOLYLINE/POLYLINE trên 0-KAKOU / 3-KEGAKI /
        /// TAPU_TMT ở Model Space, bỏ bản chụp (XData GRO_CHUP_COPY), chỉ lấy nét có hộp bao chạm
        /// MỘT trong các hộp hình cần đặt.
        /// </summary>
        public static List<NetMau> QuetNet(Database db, IList<TamCanDat> tams) => QuetNet(db, tams, null, out _);

        /// <summary>
        /// Như trên. chanDoan != null: ghi MỌI entity có hộp bao chạm hộp hình một tấm (kể cả
        /// loại / layer không lấy) kèm lý do — để biết vì sao một cạnh tấm không được quét.
        /// soLoi = số nét đúng loại + layer mà lấy mẫu hỏng hẳn.
        /// </summary>
        public static List<NetMau> QuetNet(Database db, IList<TamCanDat> tams, List<string> chanDoan, out int soLoi)
        {
            var out_ = new List<NetMau>();
            soLoi = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    if (id.IsErased) continue;
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;
                    string lydo = null;
                    Hop bb = null;
                    string loiExt = null;
                    try
                    {
                        Extents3d e = ent.GeometricExtents;
                        bb = new Hop(e.MinPoint.X, e.MinPoint.Y, e.MaxPoint.X, e.MaxPoint.Y);
                    }
                    catch (System.Exception ex) { loiExt = ex.Message; }

                    var cv = ent as Curve;
                    string lay = (ent.Layer ?? "").ToUpperInvariant();
                    List<Diem> pts = null;
                    bool kin = false;
                    if (cv == null || !LaLoaiNet(cv)) lydo = "khong phai loai net";
                    else if (Array.IndexOf(LayerNet, lay) < 0) lydo = "layer khac";
                    else
                    {
                        bool chup = false;
                        try { chup = cv.GetXDataForApplication(AppChup) != null; } catch { }
                        if (chup) lydo = "XData GRO_CHUP_COPY";
                    }

                    // Lay mau moi net dung loai (ke ca layer khac - de chan doan). Hop bao TINH TU DIEM MAU
                    // (toa do hien tai), KHONG tin GeometricExtents: tren ZWCAD no co the tra hop CU cua
                    // net vua bi lisp sua trong cung lenh -> net bi coi la khong cham tam (canh tam mat).
                    string lechExt = null;
                    if (cv != null && LaLoaiNet(cv))
                    {
                        try { pts = LayMau(cv, out kin); }
                        catch (System.Exception ex)
                        {
                            try { pts = LayMauTheoDai(cv, out kin); }
                            catch (System.Exception ex2) { if (lydo == null) lydo = "lay mau loi: " + ex.Message + " / " + ex2.Message; }
                        }
                        if (pts != null && pts.Count > 1)
                        {
                            Hop bbDiem = Hop.TuDiem(pts);
                            if (bb != null && !bb.Bang(bbDiem, 1.0) && !(cv is Spline) && !(cv is Ellipse))
                                lechExt = string.Format(CultureInfo.InvariantCulture, "LECH_EXTENTS ext={0:0.0},{1:0.0},{2:0.0},{3:0.0}",
                                                        bb.X1, bb.Y1, bb.X2, bb.Y2);
                            bb = bbDiem;
                        }
                    }
                    if (bb == null) { if (lydo == null) { lydo = "khong co hop bao: " + loiExt; soLoi++; } continue; }

                    // Hop tam NOI 1mm (nhu bb1 cua lisp): canh tam nam DUNG tren mep hop hinh; hop lisp
                    // gui sang da qua rtos -> lech vai phan trieu mm -> khong noi thi canh rot (muc 170:
                    // tam 13 mat canh tren + trai, 147 net 0-KAKOU ca ban ve, 51/65 tam khong dat duoc).
                    bool cham = false;
                    foreach (TamCanDat t in tams) if (bb.Giao(t.BbHinh.No(1.0))) { cham = true; break; }
                    if (!cham) continue;

                    if (lydo == null && (pts == null || pts.Count < 2)) lydo = "khong co diem mau";
                    if (lydo != null && lydo.StartsWith("lay mau loi")) soLoi++;
                    if (lydo == null) out_.Add(new NetMau { Layer = lay, Bb = bb, Kin = kin, Pts = pts });

                    if (chanDoan != null)
                    {
                        string apps = "";
                        try
                        {
                            ResultBuffer xd = ent.XData;
                            if (xd != null)
                                foreach (TypedValue tv in xd)
                                    if (tv.TypeCode == (short)DxfCode.ExtendedDataRegAppName) apps += (apps.Length > 0 ? "," : "") + tv.Value;
                        }
                        catch { }
                        chanDoan.Add(string.Join("\t", "E", ent.GetType().Name, ent.Layer ?? "", ent.Handle.ToString(),
                            (cv != null && LaLoaiNet(cv) && SafeClosed(cv)) ? "kin" : "-",
                            string.Format(CultureInfo.InvariantCulture, "{0:0.0},{1:0.0},{2:0.0},{3:0.0}", bb.X1, bb.Y1, bb.X2, bb.Y2),
                            lydo ?? "LAY", apps, (loiExt ?? "") + (lechExt ?? "")));
                    }
                }
                tr.Commit();
            }
            return out_;
        }

        private static bool SafeClosed(Curve c) { try { return c.Closed; } catch { return false; } }

        /// <summary>Dự phòng khi cách lấy mẫu riêng của loại nét bị lỗi: mẫu đều theo chiều dài.</summary>
        private static List<Diem> LayMauTheoDai(Curve cv, out bool kin)
        {
            var pts = new List<Diem>();
            kin = SafeClosed(cv);
            double len = cv.GetDistanceAtParameter(cv.EndParam);
            int n = Math.Max(8, Math.Min(64, (int)Math.Truncate(len / 2.0)));
            for (int i = 0; i < n; i++)
            {
                Point3d p = cv.GetPointAtDist(len * (i / (double)n));
                pts.Add(new Diem(p.X, p.Y));
            }
            pts.Add(new Diem(cv.EndPoint.X, cv.EndPoint.Y));
            return pts;
        }

        private static bool LaLoaiNet(Curve c) =>
            c is Line || c is Arc || c is Circle || c is Ellipse || c is Spline ||
            c is Polyline || c is Polyline2d || c is Polyline3d;

        /// <summary>
        /// gro--ent->diem. LINE: 2 đầu. LWPOLYLINE: cạnh thẳng lấy đúng 2 đỉnh, cạnh cung chia 8.
        /// POLYLINE cũ: mỗi cạnh chia 4. Còn lại: mẫu đều theo chiều dài (8..64 điểm).
        /// </summary>
        public static List<Diem> LayMau(Curve cv, out bool kin)
        {
            var pts = new List<Diem>();
            kin = false;
            void Them(Point3d p) => pts.Add(new Diem(p.X, p.Y));

            if (cv is Line ln)
            {
                Them(ln.StartPoint);
                Them(ln.EndPoint);
                return pts;
            }
            if (cv is Polyline pl)
            {
                int ep = (int)Math.Floor(pl.EndParam + 0.5);
                for (int i = 0; i < ep; i++)
                {
                    double b = 0.0;
                    try { b = pl.GetBulgeAt(i); } catch { }
                    int n = Math.Abs(b) > 1e-9 ? 8 : 1;
                    for (int k = 0; k < n; k++) Them(pl.GetPointAtParameter(i + k / (double)n));
                }
                Them(pl.EndPoint);
                kin = pl.Closed;
                return pts;
            }
            if (cv is Polyline2d || cv is Polyline3d)
            {
                int ep = (int)Math.Floor(cv.EndParam + 0.5);
                for (int i = 0; i < ep; i++)
                    for (int k = 0; k < 4; k++) Them(cv.GetPointAtParameter(i + k / 4.0));
                Them(cv.EndPoint);
                kin = cv.Closed;
                return pts;
            }
            // ARC CIRCLE ELLIPSE SPLINE
            double len = cv.GetDistanceAtParameter(cv.EndParam);
            if (len > 1e-6)
            {
                int n = Math.Max(8, Math.Min(64, (int)Math.Truncate(len / 2.0)));
                for (int i = 0; i < n; i++)
                {
                    try { Them(cv.GetPointAtDist(len * (i / (double)n))); } catch { }
                }
                Them(cv.EndPoint);
            }
            kin = cv.Closed;
            return pts;
        }

        // ------------------------------------------------------------------
        // Dời cụm + cờ XData
        // ------------------------------------------------------------------

        /// <summary>
        /// gro--mk-ap-cum: thu nhỏ quanh TÂM CŨ -> xoay quanh tâm cũ -> dời tới tâm mới.
        /// Mỗi entity bọc lỗi riêng (giống vl-catch-all-apply từng lệnh vla của lisp).
        /// </summary>
        private static void ApCum(Transaction tr, Database db, List<string> handles, Hop bbCum, KetQuaTam r)
        {
            var c0 = new Point3d((bbCum.X1 + bbCum.X2) / 2.0, (bbCum.Y1 + bbCum.Y2) / 2.0, 0.0);
            Matrix3d m = Matrix3d.Displacement(new Vector3d(r.Cx - c0.X, r.Cy - c0.Y, 0.0));
            if (Math.Abs(r.Th) > 1e-9) m = m * Matrix3d.Rotation(r.Th, Vector3d.ZAxis, c0);
            if (r.TiLe != 1.0) m = m * Matrix3d.Scaling(r.TiLe, c0);
            foreach (string h in handles)
            {
                try
                {
                    ObjectId id = LayId(db, h);
                    if (id.IsNull) continue;
                    var ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    ent?.TransformBy(m);
                }
                catch { }
            }
        }

        /// <summary>
        /// gro--gan-co-tam: thêm cờ vào trường thứ 4 (flags) của XData GRO_METADATA trên TEXT bango.
        /// Bango không có GRO_METADATA thì bỏ qua (như lisp).
        /// </summary>
        private static void GanCo(Transaction tr, ObjectId id, string co)
        {
            if (id.IsNull) return;
            try
            {
                var ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                ResultBuffer rb = ent?.GetXDataForApplication(AppMeta);
                if (rb == null) return;
                var vals = new List<string>();
                foreach (TypedValue tv in rb)
                    if (tv.TypeCode == (short)DxfCode.ExtendedDataAsciiString) vals.Add(tv.Value as string ?? "");
                while (vals.Count < 4) vals.Add("");
                string moi = DatMaKhac.ThemCo(vals[3], co);
                if (moi == vals[3]) return;
                var nb = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AppMeta));
                // lisp chi giu 4 chuoi dau
                for (int i = 0; i < 4; i++)
                    nb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, i == 3 ? moi : vals[i]));
                ent.XData = nb;
            }
            catch { }
        }

        /// <summary>
        /// Ghi dữ liệu đầu vào (dòng lisp + nét đã lấy mẫu) ra %TEMP%\gro_dat_mk_dump.txt để phát
        /// lại thuật toán ngoài CAD: dotnet run --project ZwCadPlugin.Tests -- phatlai &lt;file&gt;.
        /// Ghi đè mỗi lần chạy; lỗi ghi file thì bỏ qua.
        /// </summary>
        private static string GhiDump(IEnumerable<string> duLieu, List<NetMau> net, List<string> chanDoan)
        {
            try
            {
                string p = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gro_dat_mk_dump.txt");
                var sb = new System.Text.StringBuilder();
                foreach (string s in duLieu) sb.Append("L\t").Append(s).Append('\n');
                if (chanDoan != null) foreach (string s in chanDoan) sb.Append(s).Append('\n');
                foreach (NetMau n in net)
                {
                    sb.Append("N\t").Append(n.Layer).Append('\t').Append(n.Kin ? '1' : '0').Append('\t');
                    for (int i = 0; i < n.Pts.Count; i++)
                    {
                        if (i > 0) sb.Append(';');
                        sb.Append(n.Pts[i].X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                          .Append(n.Pts[i].Y.ToString("R", CultureInfo.InvariantCulture));
                    }
                    sb.Append('\t').Append(n.Bb.X1.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                      .Append(n.Bb.Y1.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                      .Append(n.Bb.X2.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                      .Append(n.Bb.Y2.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                }
                System.IO.File.WriteAllText(p, sb.ToString());
                return p;
            }
            catch { return null; }
        }

        private static ObjectId LayId(Database db, string handle)
        {
            if (string.IsNullOrWhiteSpace(handle)) return ObjectId.Null;
            if (!long.TryParse(handle.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long so))
                return ObjectId.Null;
            if (!db.TryGetObjectId(new Handle(so), out ObjectId id) || id.IsNull || id.IsErased) return ObjectId.Null;
            return id;
        }
    }
}
