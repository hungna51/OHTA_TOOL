using System;
using System.Collections.Generic;
using AiTool.Core;
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

namespace AiTool.CadServices
{
    /// <summary>
    /// Dán nhãn phân loại tấm gia công (BE, VAT, CUON, SARA, TAPU, PHANG) ở góc trên-trái bên trong
    /// mỗi khung, tỷ lệ chiều cao chữ theo chiều cao khung, đặt trên layer riêng AI-PHAN-LOAI.
    /// </summary>
    public static class DanNhanKhung
    {
        public const double TyLeChieuCao = 0.035;

        public static int GhiNhan(Document doc, IList<KhungDuLieu> danhSachKhung, IDictionary<int, string> nhanTheoKhung)
        {
            if (doc == null || danhSachKhung == null || danhSachKhung.Count == 0 || nhanTheoKhung == null || nhanTheoKhung.Count == 0)
                return 0;

            int dem = 0;
            Database db = doc.Database;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DamBaoLayer(db, tr, KevApi.LayerNhan, 2); // ACI 2 = Vàng
                ObjectId textStyleId = TimTextStyle(db, tr);
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                // Xoá nhãn cũ trên layer AI-PHAN-LOAI nằm trong các khung đang cập nhật
                var cuCanXoa = new List<Entity>();
                foreach (ObjectId id in ms)
                {
                    if (!(tr.GetObject(id, OpenMode.ForRead, false) is Entity ent))
                        continue;
                    if (!string.Equals(ent.Layer, KevApi.LayerNhan, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Point3d? pos = null;
                    if (ent is DBText dbt) pos = dbt.Position;
                    else if (ent is MText mt) pos = mt.Location;
                    if (!pos.HasValue) continue;

                    double px = pos.Value.X, py = pos.Value.Y;
                    foreach (KhungDuLieu k in danhSachKhung)
                    {
                        if ( nhanTheoKhung.ContainsKey(k.SoThuTu) &&
                             px >= k.MinX && px <= k.MaxX && py >= k.MinY && py <= k.MaxY)
                        {
                            cuCanXoa.Add(ent);
                            break;
                        }
                    }
                }

                foreach (Entity ent in cuCanXoa)
                {
                    ent.UpgradeOpen();
                    ent.Erase(true);
                }

                // Tạo nhãn mới ở góc trên-trái mỗi khung, chiều cao tỷ lệ theo chiều cao khung
                foreach (KhungDuLieu k in danhSachKhung)
                {
                    if (!nhanTheoKhung.TryGetValue(k.SoThuTu, out string nhan) || string.IsNullOrWhiteSpace(nhan))
                        continue;

                    double caoKhung = Math.Max(10.0, k.MaxY - k.MinY);
                    double chieuCaoChu = Math.Max(6.0, Math.Round(caoKhung * TyLeChieuCao, 1));
                    double leTrai = chieuCaoChu * 0.6;
                    double leTren = chieuCaoChu * 1.4;

                    var txt = new DBText
                    {
                        Layer = KevApi.LayerNhan,
                        TextStyleId = textStyleId,
                        Height = chieuCaoChu,
                        Position = new Point3d(k.MinX + leTrai, k.MaxY - leTren, 0.0),
                        TextString = "[" + nhan.Trim() + "]"
                    };
                    ms.AppendEntity(txt);
                    tr.AddNewlyCreatedDBObject(txt, true);
                    dem++;
                }

                tr.Commit();
            }
            return dem;
        }

        private static void DamBaoLayer(Database db, Transaction tr, string tenLayer, short mauAci)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(tenLayer))
            {
                lt.UpgradeOpen();
                var ltr = new LayerTableRecord
                {
                    Name = tenLayer,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, mauAci)
                };
                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }
            else
            {
                var ltr = (LayerTableRecord)tr.GetObject(lt[tenLayer], OpenMode.ForRead);
                if (ltr.IsLocked || ltr.IsOff || ltr.IsFrozen)
                {
                    ltr.UpgradeOpen();
                    ltr.IsLocked = false;
                    ltr.IsOff = false;
                    ltr.IsFrozen = false;
                }
            }
        }

        private static ObjectId TimTextStyle(Database db, Transaction tr)
        {
            try
            {
                var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (tst.Has("Text")) return tst["Text"];
            }
            catch (Exception) { }
            return db.Textstyle;
        }
    }
}
