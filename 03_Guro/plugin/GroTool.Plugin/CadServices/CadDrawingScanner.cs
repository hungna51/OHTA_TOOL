using System;
using System.Collections.Generic;
using System.Linq;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
#endif
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Quét bản vẽ ra danh sách tấm cho palette DST.
    ///
    /// MỖI DÒNG LÀ MỘT TEXT BANGO, không phải một khung: một khung Khung_IN có thể chứa
    /// nhiều tấm (cặp 本図/逆図 của cùng một ghi chú) — bản cũ (cả lisp lẫn C#) chỉ lấy số
    /// đầu tiên trong khung nên mất tấm. Mã khắc và F-name được ghép vào bango theo ĐOẠN SỐ
    /// CUỐI ("I-1-3", "N=xxx-1-3" -> 3) và theo vị trí, đúng như lệnh GRO đặt chúng.
    ///
    /// Duyệt thẳng Model Space — không phụ thuộc tầm nhìn (mục 58).
    /// </summary>
    public static class CadDrawingScanner
    {
        private const string LayerKhung = "Khung_IN";
        private const string LayerCoDon = "CANH_BAO_CO_DON";
        private const string LayerBango = "0";
        private const string LayerMaKhac = "3-KEGAKI";
        private const string LayerFname = "2-F-NAME";
        private const string LayerKakou = "0-KAKOU";
        private const string LayerSunpou = "1-SUNPOU";
        private const string AppNameGroMetadata = "GRO_METADATA";
        private const string AppNameGroChupCopy = "GRO_CHUP_COPY";

        /// <summary>Số khung Khung_IN không có tấm nào ở lần quét gần nhất (để báo).</summary>
        public static int SoKhungTrong { get; private set; }

        public static List<PlateItem> ScanPlates()
        {
            var plates = new List<PlateItem>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return plates;
            Database db = doc.Database;

            DocumentLock docLock = null;
            try { docLock = doc.LockDocument(); }
            catch { /* dang trong ngu canh lenh: da khoa san */ }

            try
            {
                var khungs = new List<Khung>();
                var texts = new List<Chu>();
                var netCats = new List<double[]>();
                var ghiChus = new List<Chu>();
                var xdataMap = new Dictionary<ObjectId, (string role, string primary, string note, bool noShape, string flags)>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    foreach (ObjectId id in ms)
                    {
                        if (id.IsErased) continue;
                        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;
                        try
                        {
                            if (ent.GetXDataForApplication(AppNameGroChupCopy) != null) continue;
                        }
                        catch { }
                        string lay = ent.Layer ?? string.Empty;
                        if (Cung(lay, "0-BANGO_CHUP") || Cung(lay, "0-TEN_BV_CHUP") || lay.EndsWith("_CHUP", StringComparison.OrdinalIgnoreCase)) continue;

                        if (Cung(lay, LayerKakou) && TryHop(ent, out double[] hk, out _))
                        {
                            netCats.Add(hk);
                        }

                        if (ent is Polyline || ent is Polyline2d || ent is Polyline3d)
                        {
                            bool laKhung = Cung(lay, LayerKhung), laCoDon = Cung(lay, LayerCoDon);
                            if ((laKhung || laCoDon) && TryHop(ent, out double[] h, out Extents3d ext))
                                khungs.Add(new Khung { Id = id, Hop = h, Ext = ext, CoDon = laCoDon });
                        }
                        else if (ent is DBText dt)
                        {
                            if (Cung(lay, LayerBango) && DocXData(dt, out string r, out string pri, out string nt, out bool ns, out string fl))
                            {
                                xdataMap[id] = (r, pri, nt, ns, fl);
                            }

                            if (LaLayerChu(lay))
                            {
                                var c = new Chu
                                {
                                    Id = id, Layer = lay, LaMText = false,
                                    Goc = dt.TextString ?? string.Empty,
                                    X = dt.Position.X, Y = dt.Position.Y,
                                    Cao = dt.Height, Mau = dt.ColorIndex, Xoay = dt.Rotation,
                                };
                                c.Doc = c.Goc;
                                if (TryHop(ent, out double[] h, out _)) c.Hop = h;
                                texts.Add(c);
                            }
                            else if (LaLayerGhiChu(lay))
                            {
                                var c = new Chu
                                {
                                    Id = id, Layer = lay, LaMText = false,
                                    Goc = dt.TextString ?? string.Empty,
                                    X = dt.Position.X, Y = dt.Position.Y,
                                    Cao = dt.Height, Mau = dt.ColorIndex, Xoay = dt.Rotation,
                                };
                                c.Doc = c.Goc;
                                if (TryHop(ent, out double[] h, out _)) c.Hop = h;
                                ghiChus.Add(c);
                            }
                        }
                        else if (ent is MText mt)
                        {
                            if (LaLayerChu(lay))
                            {
                                var c = new Chu
                                {
                                    Id = id, Layer = lay, LaMText = true,
                                    Goc = mt.Contents ?? string.Empty,
                                    Doc = mt.Text ?? string.Empty,
                                    X = mt.Location.X, Y = mt.Location.Y,
                                    Cao = mt.TextHeight, Mau = mt.ColorIndex, Xoay = mt.Rotation,
                                };
                                if (TryHop(ent, out double[] h, out _)) c.Hop = h;
                                texts.Add(c);
                            }
                            else if (LaLayerGhiChu(lay))
                            {
                                var c = new Chu
                                {
                                    Id = id, Layer = lay, LaMText = true,
                                    Goc = mt.Contents ?? string.Empty,
                                    Doc = mt.Text ?? string.Empty,
                                    X = mt.Location.X, Y = mt.Location.Y,
                                    Cao = mt.TextHeight, Mau = mt.ColorIndex, Xoay = mt.Rotation,
                                };
                                if (TryHop(ent, out double[] h, out _)) c.Hop = h;
                                ghiChus.Add(c);
                            }
                        }
                    }
                    tr.Commit();
                }

                var scannedPlates = GhepTam(khungs, texts).ToList();
                // Tấm do GRO tạo (bango có XData GRO_METADATA): ghi chú TIN XData — không có là không có.
                var coXData = new HashSet<PlateItem>();
                foreach (PlateItem p in scannedPlates)
                {
                    if (xdataMap.TryGetValue(p.BangoTextId, out var xd))
                    {
                        if (!string.IsNullOrEmpty(xd.role)) p.Role = xd.role;
                        if (!string.IsNullOrEmpty(xd.primary)) p.PrimaryBango = xd.primary;
                        if (!string.IsNullOrEmpty(xd.note))
                            p.CustomerNote = JapanNoteTranslator.Translate(JapanNoteTranslator.ChuanHoaGhiChuGro(xd.note));
                        coXData.Add(p);
                        if (xd.noShape) p.NoCuttingShape = true;
                        // Mục 117: cờ mã khắc (chưa vào hình / đè ケガキ / thu nhỏ) -> cột Ghi chú
                        BangSoLieu.DocCoMaKhac(xd.flags, out var cbMk, out var gcMk);
                        foreach (string cb in cbMk) p.ThemCanhBao(cb);
                        if (gcMk.Count > 0) p.GhiChuThem = string.Join("; ", gcMk);
                    }

                    if (p.FrameId != ObjectId.Null && netCats.Count > 0)
                    {
                        Khung k = khungs.FirstOrDefault(x => x.Id == p.FrameId);
                        if (k != null)
                        {
                            if (!netCats.Any(nc => BangSoLieu.HopGiao(nc, k.Hop)))
                            {
                                p.NoCuttingShape = true;
                            }
                            else
                            {
                                // Neu khung bao cat giao voi net cat, chac chan tam da co hinh cat
                                p.NoCuttingShape = false;
                            }
                        }
                    }
                }

                GhepCapDoiXungDuPhong(scannedPlates, ghiChus, coXData);

                plates.AddRange(scannedPlates);
                plates.AddRange(khungs.Where(k => k.CoDon)
                                      .OrderByDescending(k => k.Hop[3]).ThenBy(k => k.Hop[0])
                                      .Select(TaoDongCoDon));
            }
            finally
            {
                docLock?.Dispose();
            }

            // Trung bango: canh bao tren tung dong (viec CHAN luu nam o KiemTraTruocKhiLuu)
            var trung = BangSoLieu.BangoTrung(plates.Where(p => !p.IsOrphan).Select(p => p.Bango));
            foreach (PlateItem p in plates.Where(p => !p.IsOrphan && trung.Contains(p.Bango)))
                p.TrungBango = true;

            // CSV cua GRO: cot GC lay tu day (F-name khong phan biet duoc m / k / *), va doi
            // chieu Q - bang, ban ve va CSV phai noi cung mot chuyen.
            string duongCsv = CsvService.DuongDan();
            CsvGro csv = CsvService.TryDoc(duongCsv, out _);
            int khongCoCsv = 0;
            if (csv != null)
            {
                foreach (PlateItem p in plates.Where(p => !p.IsOrphan))
                {
                    int i = csv.TimDong(p.Bango);
                    if (i < 0)
                    {
                        p.ThemCanhBao("[!] Không có trong CSV");
                        khongCoCsv++;
                        continue;
                    }
                    p.GC = csv.LayGC(i);
                    // Doi chieu Q / T voi CSV nam trong PlateItem.TinhLoi (to do neu lech).
                    p.CsvQ = csv.LayQ(i);
                    p.CsvT = csv.LayT(i);
                }
            }

            var kq = plates.OrderBy(p => p.IsOrphan ? 1 : 0)
                           .ThenBy(p => int.TryParse(p.Bango, out int n) ? n : int.MaxValue)
                           .ThenByDescending(p => p.BoundingBox.MaxPoint.Y)
                           .ThenBy(p => p.BoundingBox.MinPoint.X)
                           .ToList();
            foreach (PlateItem p in kq) p.DanhDauDaKhop();
            doc.Editor.WriteMessage($"\n[DST] {kq.Count(p => !p.IsOrphan)} tam, {kq.Count(p => p.IsOrphan)} hinh co don" +
                                    (SoKhungTrong > 0 ? $", {SoKhungTrong} khung {LayerKhung} khong co bango" : "") +
                                    (csv == null ? $"\n[DST] KHONG doc duoc CSV (cot GC doan tu dong V= cua F-name): {duongCsv}"
                                     : khongCoCsv > 0 ? $"\n[DST] {khongCoCsv} tam KHONG co dong trong CSV: {duongCsv}"
                                     : $"\n[DST] CSV: {duongCsv}"));
            return kq;
        }

        // ------------------------------------------------------------------

        private static IEnumerable<PlateItem> GhepTam(List<Khung> khungs, List<Chu> texts)
        {
            var khungIn = khungs.Where(k => !k.CoDon).ToList();
            var maKhacs = texts.Where(t => Cung(t.Layer, LayerMaKhac)).ToList();
            var fnames = texts.Where(t => Cung(t.Layer, LayerFname)).ToList();

            // Ung vien bango: TEXT (khong phai MTEXT) tren layer "0", chuoi so nguyen duong.
            var ungVien = texts.Where(t => !t.LaMText && t.Layer == LayerBango && GroText.LaBangoHopLe(t.Goc))
                               .ToList();

            var daCoTam = new HashSet<Khung>();
            foreach (Chu b in ungVien)
            {
                string so = b.Goc.Trim();
                Khung k = KhungChua(khungIn, b.X, b.Y);
                double cao = b.Cao > 0 ? b.Cao : 10.0;

                // Pham vi tim ma khac / F-name: trong khung neu co khung, khong thi quanh bango.
                Func<Chu, bool> trongPhamVi = k != null
                    ? (Func<Chu, bool>)(t => ThuocHop(t, k.Hop))
                    : (t => Math.Abs(t.X - b.X) <= 20 * cao && Math.Abs(t.Y - b.Y) <= 20 * cao);

                Chu mk = maKhacs.Where(trongPhamVi)
                                .Where(t => GroText.TryDocBangoCuoi(t.Doc.Trim(), out string s) && s == so)
                                .OrderBy(t => KhoangCach(t, b))
                                .FirstOrDefault();
                List<Chu> khoiFn = TimKhoiFname(fnames.Where(trongPhamVi).ToList(), so, b);

                bool coBangChung = mk != null || khoiFn.Count > 0;
                // Ngoai khung ma khong co ma khac / F-name di kem -> nhieu kha nang la chu
                // cua khach (so do tren layer 0). Bo qua de khong ra dong rac.
                if (k == null && !coBangChung) continue;
                // Trong khung: giu giong lisp (so tren layer 0 la bango), nhung chi lay chu
                // MAU DO hoac co bang chung - layer 0 cua khach hay co so ghi chu.
                if (k != null && !coBangChung && b.Mau != 1) continue;

                var p = new PlateItem
                {
                    Bango = so,
                    BangoTextId = b.Id,
                    FrameId = k?.Id ?? ObjectId.Null,
                    FrameBoundingBox = k != null ? (Extents3d?)k.Ext : null,
                    MaKhacTextId = mk?.Id ?? ObjectId.Null,
                };
                p.FnameTextIds.AddRange(khoiFn.Select(t => t.Id));
                DocFname(p, khoiFn);
                // Zoom toi CHINH tam (bango + ma khac + F-name), KHONG phai ca khung Khung_IN:
                // khung in co the la ca to ban ve nhieu tam -> ban cu zoom ra ca to (muc 86).
                p.BoundingBox = HopKhoi(b, mk, khoiFn);

                var thieu = new List<string>();
                if (mk == null) thieu.Add("mã khắc");
                if (khoiFn.Count == 0) thieu.Add("F-name");
                if (thieu.Count > 0) p.CanhBao = "[!] Thiếu " + string.Join(" + ", thieu);

                if (k != null) daCoTam.Add(k);
                yield return p;
            }
            SoKhungTrong = khungIn.Count(k => !daCoTam.Contains(k));
        }

        /// <summary>
        /// Khối F-name của một bango.
        ///   GRO: mỗi dòng một TEXT, cùng X, xếp dọc bắt đầu bằng "N=...-&lt;bango&gt;".
        ///   FN cũ: một MTEXT chứa cả khối, các dòng nối bằng \P.
        /// Không có dòng N= khớp số -> rỗng (người gọi coi là thiếu F-name).
        /// </summary>
        private static List<Chu> TimKhoiFname(List<Chu> fn, string so, Chu b)
        {
            var kq = new List<Chu>();

            Chu mt = fn.Where(t => t.LaMText && BangSoLieu.TachDongMText(t.Goc)
                                   .Any(d => BangSoLieu.PhanLoai(d) == LoaiDongFname.N &&
                                             GroText.TryDocBangoCuoi(d.Trim(), out string s) && s == so))
                       .OrderBy(t => KhoangCach(t, b))
                       .FirstOrDefault();

            Chu n = fn.Where(t => !t.LaMText && BangSoLieu.PhanLoai(t.Goc) == LoaiDongFname.N &&
                                  GroText.TryDocBangoCuoi(t.Goc.Trim(), out string s) && s == so)
                      .OrderBy(t => KhoangCach(t, b))
                      .FirstOrDefault();

            if (n == null || (mt != null && KhoangCach(mt, b) < KhoangCach(n, b)))
            {
                if (mt != null) kq.Add(mt);
                return kq;
            }

            // Cot TEXT cung x' voi dong N= TRONG HE TRUC CUA NO (cum co the bi xoay, muc 117),
            // di XUONG cho toi dong N= ke tiep hoac mot khoang ho lon.
            var dsText = fn.Where(t => !t.LaMText).ToList();
            var dong = dsText.Select(t => new BangSoLieu.DongChu
            {
                X = t.X, Y = t.Y, Cao = t.Cao,
                LaN = BangSoLieu.PhanLoai(t.Goc) == LoaiDongFname.N,
            }).ToList();
            foreach (int i in BangSoLieu.GomCotFname(dong, dsText.IndexOf(n), n.Xoay))
                kq.Add(dsText[i]);
            return kq;
        }

        private static void DocFname(PlateItem p, List<Chu> khoi)
        {
            bool be = false;
            foreach (Chu t in khoi)
            {
                IEnumerable<string> dong = t.LaMText ? BangSoLieu.TachDongMText(t.Goc) : new[] { t.Goc };
                foreach (string d in dong)
                {
                    switch (BangSoLieu.PhanLoai(d))
                    {
                        case LoaiDongFname.T: p.Thickness = BangSoLieu.GiaTri(d); break;
                        case LoaiDongFname.Q: p.Quantity = BangSoLieu.GiaTri(d); break;
                        case LoaiDongFname.M: p.Material = BangSoLieu.GiaTri(d); break;
                        case LoaiDongFname.V: be = true; break;
                    }
                }
            }
            // Lenh GRO chi ghi dong "V=" khi tam co BE ("m" hoac "*"), "k" (vat) thi khong ghi.
            // Day chi la gia tri tam - co CSV thi cot GC lay tu CSV (chinh xac hon).
            p.GC = be ? "m" : string.Empty;
        }

        private static PlateItem TaoDongCoDon(Khung k)
        {
            return new PlateItem
            {
                Bango = "?",
                IsOrphan = true,
                FrameId = k.Id,
                BoundingBox = k.Ext,
                GC = "-",
                Thickness = "-",
                Quantity = "-",
                Material = "-",
            };
        }

        // ------------------------------------------------------------------

        private static bool Cung(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool LaLayerChu(string lay)
        {
            return lay == LayerBango || Cung(lay, LayerMaKhac) || Cung(lay, LayerFname);
        }

        private static bool LaLayerGhiChu(string lay)
        {
            return Cung(lay, LayerSunpou) || lay == LayerBango;
        }

        private static bool DocXData(Entity ent, out string role, out string primary, out string note, out bool noShape, out string flags)
        {
            flags = string.Empty;
            role = string.Empty;
            primary = string.Empty;
            note = string.Empty;
            noShape = false;

            if (ent == null) return false;
            try
            {
                ResultBuffer rb = ent.GetXDataForApplication(AppNameGroMetadata);
                if (rb == null) return false;

                var strings = new List<string>();
                foreach (TypedValue tv in rb)
                {
                    if (tv.TypeCode == (short)DxfCode.ExtendedDataAsciiString || tv.TypeCode == 1000)
                    {
                        strings.Add(tv.Value as string ?? string.Empty);
                    }
                }

                if (strings.Count > 0 && !string.IsNullOrWhiteSpace(strings[0])) role = strings[0].Trim();
                if (strings.Count > 1 && !string.IsNullOrWhiteSpace(strings[1])) primary = strings[1].Trim();
                if (strings.Count > 2 && !string.IsNullOrWhiteSpace(strings[2])) note = strings[2].Trim();
                if (strings.Count > 3 && strings[3] != null) flags = strings[3];
                if (strings.Count > 3 && !string.IsNullOrWhiteSpace(strings[3]) &&
                    strings[3].IndexOf("NO_SHAPE", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    noShape = true;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void GhepCapDoiXungDuPhong(List<PlateItem> plates, List<Chu> ghiChus, HashSet<PlateItem> coXData)
        {
            var validPlates = plates.Where(p => !p.IsOrphan).ToList();
            if (validPlates.Count == 0) return;

            var honzus = new List<PlateItem>();
            var gyakus = new List<PlateItem>();

            // Mục 127: mỗi chữ ghi chú chỉ thuộc MỘT tấm (hộp chứa tâm chữ / gần nhất),
            // khung các tấm chồng nhau không còn chia một ghi chú cho cả hai tấm.
            var hopTams = validPlates.Select(p => p.BoundingBox == null ? null
                : new[] { p.BoundingBox.MinPoint.X, p.BoundingBox.MinPoint.Y, p.BoundingBox.MaxPoint.X, p.BoundingBox.MaxPoint.Y }).ToList();
            var chuCuaTam = new Dictionary<int, List<Chu>>();
            foreach (var g in ghiChus)
            {
                int i = BangSoLieu.ChonTamSoHuu(g.Hop ?? new[] { g.X, g.Y, g.X, g.Y }, hopTams);
                if (i < 0) continue;
                if (!chuCuaTam.TryGetValue(i, out var ds)) chuCuaTam[i] = ds = new List<Chu>();
                ds.Add(g);
            }

            for (int ip = 0; ip < validPlates.Count; ip++)
            {
                var p = validPlates[ip];
                // Cac ghi chu thuoc rieng tam p
                var gcTam = chuCuaTam.TryGetValue(ip, out var dsGc) ? dsGc : new List<Chu>();

                // Trich xuat CustomerNote neu chua co tu XData
                // Mục 126: chỉ dò ghi chú từ chữ quanh tấm khi tấm KHÔNG có XData GRO (bản vẽ cũ).
                // Có XData mà trống = tấm không có ghi chú. Từ khi mã khắc vào trong hình (mục 117),
                // hộp tấm to ra và chạm ghi chú tấm bên cạnh -> tấm 21 nhận nhầm "khắc kegaki" của tấm 23.
                if (string.IsNullOrEmpty(p.CustomerNote) && gcTam.Count > 0 && !coXData.Contains(p))
                {
                    foreach (var g in gcTam)
                    {
                        string dich = JapanNoteTranslator.ExtractAndTranslate(g.Doc);
                        if (string.IsNullOrEmpty(dich))
                            dich = JapanNoteTranslator.ExtractAndTranslate(g.Goc);

                        if (!string.IsNullOrEmpty(dich))
                        {
                            p.CustomerNote = dich;
                            break;
                        }
                    }
                }

                // Neu chua co Role tu XData thi nhan dien qua text
                if (string.IsNullOrEmpty(p.Role))
                {
                    bool coHon = gcTam.Any(g => g.Doc.Contains("本図") || g.Goc.Contains("本図"));
                    bool coGyaku = gcTam.Any(g => g.Doc.Contains("逆図") || g.Goc.Contains("逆図") || g.Doc.Contains("裏図") || g.Goc.Contains("裏図"));

                    if (coHon && !coGyaku) honzus.Add(p);
                    else if (coGyaku) gyakus.Add(p);
                }
            }

            // Ghep cap Honzu va Gyakuzu cho cac tam chua co Role
            foreach (var g in gyakus)
            {
                var hon = honzus.FirstOrDefault(h => h.FrameId == g.FrameId && h.FrameId != ObjectId.Null)
                          ?? honzus.OrderBy(h => KhoangCachTam(h, g)).FirstOrDefault();
                if (hon != null)
                {
                    hon.Role = "HONZU";
                    g.Role = "GYAKUZU";
                    g.PrimaryBango = hon.Bango;
                }
            }
        }

        private static double KhoangCachTam(PlateItem a, PlateItem b)
        {
            if (a.BoundingBox == null || b.BoundingBox == null) return double.MaxValue;
            double ax = (a.BoundingBox.MinPoint.X + a.BoundingBox.MaxPoint.X) / 2.0;
            double ay = (a.BoundingBox.MinPoint.Y + a.BoundingBox.MaxPoint.Y) / 2.0;
            double bx = (b.BoundingBox.MinPoint.X + b.BoundingBox.MaxPoint.X) / 2.0;
            double by = (b.BoundingBox.MinPoint.Y + b.BoundingBox.MaxPoint.Y) / 2.0;
            double dx = ax - bx, dy = ay - by;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool TryHop(Entity ent, out double[] h, out Extents3d ext)
        {
            try
            {
                ext = ent.GeometricExtents;
                h = new[] { ext.MinPoint.X, ext.MinPoint.Y, ext.MaxPoint.X, ext.MaxPoint.Y };
                return true;
            }
            catch
            {
                h = null;
                ext = new Extents3d();
                return false;
            }
        }

        /// <summary>Chữ thuộc khung: hộp chữ CHẠM hộp khung (như ssget "C"); không có hộp thì xét điểm chèn.</summary>
        private static bool ThuocHop(Chu t, double[] hop)
        {
            return t.Hop != null ? BangSoLieu.HopGiao(t.Hop, hop) : BangSoLieu.DiemTrongHop(t.X, t.Y, hop);
        }

        /// <summary>Khung chứa điểm; nhiều khung lồng nhau thì lấy khung NHỎ nhất.</summary>
        private static Khung KhungChua(List<Khung> ks, double x, double y)
        {
            return ks.Where(k => BangSoLieu.DiemTrongHop(x, y, k.Hop))
                     .OrderBy(k => (k.Hop[2] - k.Hop[0]) * (k.Hop[3] - k.Hop[1]))
                     .FirstOrDefault();
        }

        private static double KhoangCach(Chu a, Chu b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static Extents3d HopKhoi(Chu b, Chu mk, List<Chu> fn)
        {
            var hops = new List<double[]>();
            foreach (Chu t in new[] { b, mk }.Concat(fn))
                if (t != null) hops.Add(t.Hop ?? new[] { t.X, t.Y, t.X, t.Y });
            double le = (b.Cao > 0 ? b.Cao : 10.0) * 3;
            return new Extents3d(new Point3d(hops.Min(h => h[0]) - le, hops.Min(h => h[1]) - le, 0),
                                 new Point3d(hops.Max(h => h[2]) + le, hops.Max(h => h[3]) + le, 0));
        }

        private sealed class Khung
        {
            public ObjectId Id;
            public double[] Hop;
            public Extents3d Ext;
            public bool CoDon;
        }

        private sealed class Chu
        {
            public ObjectId Id;
            public string Layer;
            public bool LaMText;
            /// <summary>Chuỗi GỐC: TextString / MText.Contents (còn mã định dạng, \P).</summary>
            public string Goc;
            /// <summary>Chuỗi để ĐỌC: TextString / MText.Text (đã bỏ mã định dạng).</summary>
            public string Doc;
            public double X, Y, Cao;
            /// <summary>Góc xoay (radian) — cụm mã khắc + F-name có thể bị xoay khi đặt vào trong hình (mục 117).</summary>
            public double Xoay;
            public int Mau;
            public double[] Hop;
        }
    }
}
