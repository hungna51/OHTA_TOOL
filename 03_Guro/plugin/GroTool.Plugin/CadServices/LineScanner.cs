using System;
using System.Collections.Generic;
using System.Linq;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
#endif
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.CadServices
{
    public sealed class LayerItemInfo
    {
        public string LayerName { get; set; } = string.Empty;
        public int EntityCount { get; set; }
        public string DisplayText => $"{LayerName} ({EntityCount} đối tượng)";
        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// Dịch vụ quét và chuyển đổi các nhóm nét trên layer gốc CAD.
    /// </summary>
    public static class LineScanner
    {
        // Danh sách layer chuẩn / đầu ra cần loại trừ khi đề xuất layer gốc
        private static readonly HashSet<string> StandardLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "0-KAKOU", "3-KEGAKI", "1-SUNPOU", "2-F-NAME", "Khung_IN", "TAPU_TMT",
            "CANH_BAO_CO_DON", "CANH_BAO_LECH", "CANH_BAO_SO_LUONG", "Defpoints"
        };

        /// <summary>
        /// Đọc danh sách các layer khả dụng trong bản vẽ, sắp xếp giảm dần theo số đối tượng.
        /// </summary>
        public static List<LayerItemInfo> DocDanhSachLayer(Database db)
        {
            var res = new List<LayerItemInfo>();
            if (db == null) return res;

            var dem = LayerQuery.Dem(db);
            foreach (var kv in dem.DoiTuong)
            {
                if (StandardLayers.Contains(kv.Key) || kv.Key.EndsWith("_CHUP", StringComparison.OrdinalIgnoreCase)) continue;
                if (kv.Value <= 0) continue;
                res.Add(new LayerItemInfo
                {
                    LayerName = kv.Key,
                    EntityCount = kv.Value
                });
            }

            // Sắp xếp giảm dần theo số lượng đối tượng
            res = res.OrderByDescending(x => x.EntityCount).ToList();

            // Nếu không có layer nào ngoài layer chuẩn, lấy mọi layer
            if (res.Count == 0)
            {
                foreach (string lay in dem.TatCaLayer)
                {
                    int cnt = dem.DoiTuong.TryGetValue(lay, out int c) ? c : 0;
                    res.Add(new LayerItemInfo { LayerName = lay, EntityCount = cnt });
                }
                res = res.OrderByDescending(x => x.EntityCount).ToList();
            }

            return res;
        }

        /// <summary>
        /// Quét các nét chưa đổi trên layer chỉ định.
        /// </summary>
        public static List<UnconvertedGroupItem> QuetNetChuaDoi(Database db, string layerGoc)
        {
            var res = new List<UnconvertedGroupItem>();
            if (db == null || string.IsNullOrWhiteSpace(layerGoc)) return res;

            var rawItems = new List<RawLineItem>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Đọc thông tin LayerTableRecord để tra Linetype và Color mặc định (ByLayer)
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                string layerLinetype = "Continuous";
                int layerColor = 7;
                if (lt.Has(layerGoc))
                {
                    var ltr = (LayerTableRecord)tr.GetObject(lt[layerGoc], OpenMode.ForRead);
                    layerColor = ltr.Color.ColorIndex;
                    if (!ltr.LinetypeObjectId.IsNull)
                    {
                        var ltrLtype = tr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;
                        if (ltrLtype != null) layerLinetype = ltrLtype.Name;
                    }
                }

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (id.IsErased) continue;
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;
                    if (ent.Layer != null && ent.Layer.EndsWith("_CHUP", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        if (ent.GetXDataForApplication("GRO_CHUP_COPY") != null) continue;
                    }
                    catch { }
                    if (!string.Equals(ent.Layer, layerGoc, StringComparison.OrdinalIgnoreCase)) continue;

                    // Xác định linetype thực tế
                    string ltype = ent.Linetype;
                    if (string.IsNullOrWhiteSpace(ltype) || string.Equals(ltype, "ByLayer", StringComparison.OrdinalIgnoreCase))
                        ltype = layerLinetype;

                    // Xác định color thực tế
                    int col = ent.Color.IsByLayer ? layerColor : ent.Color.ColorIndex;

                    // Lấy Extents hình học
                    double minX = 0, minY = 0, maxX = 0, maxY = 0;
                    try
                    {
                        var ext = ent.GeometricExtents;
                        minX = ext.MinPoint.X;
                        minY = ext.MinPoint.Y;
                        maxX = ext.MaxPoint.X;
                        maxY = ext.MaxPoint.Y;
                    }
                    catch
                    {
                        // Bỏ qua đối tượng không lấy được extents
                    }

                    rawItems.Add(new RawLineItem
                    {
                        EntityId = id,
                        Linetype = ltype,
                        ColorIndex = col,
                        MinX = minX,
                        MinY = minY,
                        MaxX = maxX,
                        MaxY = maxY
                    });
                }
                tr.Commit();
            }

            // Gom nhóm bằng LineClassifier (theo kiểu nét)
            var groups = LineClassifier.GomNhom(rawItems);
            foreach (var g in groups)
            {
                var item = new UnconvertedGroupItem
                {
                    Linetype = g.Linetype,
                    Count = g.Count,
                    EntityIds = g.EntityIds.OfType<ObjectId>().ToList(),
                    BoundingBox = new Extents3d(new Point3d(g.MinX, g.MinY, 0), new Point3d(g.MaxX, g.MaxY, 0))
                };
                res.Add(item);
            }

            return res;
        }

        /// <summary>
        /// Đảm bảo layer đích tồn tại trước khi chuyển.
        /// </summary>
        private static void EnsureLayer(Transaction tr, Database db, string layerName)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();
                short colorIndex = 7;
                if (string.Equals(layerName, "3-KEGAKI", StringComparison.OrdinalIgnoreCase)) colorIndex = 1;
                else if (string.Equals(layerName, "1-SUNPOU", StringComparison.OrdinalIgnoreCase)) colorIndex = 3;
                else if (string.Equals(layerName, "4-SHT", StringComparison.OrdinalIgnoreCase)) colorIndex = 4;
                else if (string.Equals(layerName, "0-KAKOU", StringComparison.OrdinalIgnoreCase)) colorIndex = 7;

                var ltr = new LayerTableRecord
                {
                    Name = layerName,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                };
                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }
        }

        /// <summary>
        /// Chuyển toàn bộ các đối tượng trong danh sách sang layer chỉ định.
        /// Đồng thời đưa toàn bộ thông số đối tượng (Màu sắc, Kiểu nét, Độ dày nét) về ByLayer.
        /// </summary>
        public static int ChuyenLayerNhom(Document doc, IEnumerable<ObjectId> ids, string layerDich)
        {
            if (doc == null || ids == null) return 0;
            var idList = ids.Where(id => !id.IsNull && id.IsValid && !id.IsErased).ToList();
            if (idList.Count == 0) return 0;

            Database db = doc.Database;
            int count = 0;

            DocumentLock docLock = null;
            try { docLock = doc.LockDocument(); } catch { }

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    EnsureLayer(tr, db, layerDich);

                    foreach (ObjectId id in idList)
                    {
                        try
                        {
                            var ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                            if (ent != null)
                            {
                                ent.Layer = layerDich;
                                // Đưa toàn bộ thông số về ByLayer
                                ent.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                                ent.Linetype = "ByLayer";
                                ent.LineWeight = LineWeight.ByLayer;
                                ent.LinetypeScale = 1.0;
                                count++;
                            }
                        }
                        catch
                        {
                            // Bỏ qua nếu thực thể bị khóa hoặc lỗi
                        }
                    }
                    tr.Commit();
                }
                doc.Editor.UpdateScreen();
            }
            finally
            {
                docLock?.Dispose();
            }

            return count;
        }
    }
}
