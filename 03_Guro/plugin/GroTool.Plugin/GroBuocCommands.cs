using System;
using System.Collections.Generic;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
#endif
using ZwCadPlugin.Core;
using WinReg = Microsoft.Win32;

[assembly: CommandClass(typeof(ZwCadPlugin.GroBuocCommands))]

namespace ZwCadPlugin
{
    /// <summary>
    /// Mục 171 (GĐ 0) — hạ tầng chuyển lisp GRO sang C# từng bước:
    ///   (gro_net_cong_tac "buoc")      -> "lisp" / "ab" / "cs"   (registry HKCU\Software\OFT_Lisp\GRO\cs_buoc)
    ///   (gro_net_phien_moi)            -> T  (xoá GroPhien của bản vẽ — đầu c:gro)
    ///   (gro_net_ab "buoc" "khoa|ten=gt|..." ...) -> T nếu khớp, nil nếu lệch
    ///        So bản ghi lisp gửi với bản ghi C# đã để trong phiên ("ab_buoc"), in kết quả.
    ///   Lệnh GROCS: xem / đặt chế độ từng bước.
    /// </summary>
    public class GroBuocCommands
    {
        private const string KhoaReg = @"Software\OFT_Lisp\GRO";

        public static CheDoBuoc DocCheDo(string buoc)
        {
            try
            {
                using (WinReg.RegistryKey k = WinReg.Registry.CurrentUser.OpenSubKey(KhoaReg))
                    return GroCongTac.DocCheDo(k?.GetValue(GroCongTac.TenKhoa(buoc)) as string);
            }
            catch { return CheDoBuoc.Lisp; }
        }

        public static void GhiCheDo(string buoc, CheDoBuoc c)
        {
            using (WinReg.RegistryKey k = WinReg.Registry.CurrentUser.CreateSubKey(KhoaReg))
                k.SetValue(GroCongTac.TenKhoa(buoc), GroCongTac.TenCheDo(c));
        }

        /// <summary>Phiên của bản vẽ đang mở (khoá = tên document).</summary>
        public static GroPhien PhienHienTai()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            return GroPhien.Lay(doc?.Name ?? "");
        }

        private static List<string> ChuoiTuRb(ResultBuffer rb)
        {
            var ds = new List<string>();
            if (rb != null) foreach (TypedValue tv in rb) if (tv.Value is string s) ds.Add(s);
            return ds;
        }

        [LispFunction("GRO_NET_CONG_TAC")]
        public static object LispCongTac(ResultBuffer rb)
        {
            List<string> a = ChuoiTuRb(rb);
            if (a.Count == 0 || !GroCongTac.LaBuoc(a[0])) return "lisp";
            return GroCongTac.TenCheDo(DocCheDo(a[0]));
        }

        [LispFunction("GRO_NET_PHIEN_MOI")]
        public static object LispPhienMoi(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            GroPhien.Moi(doc?.Name ?? "");
            return true;
        }

        /// <summary>
        /// So A/B dùng chung cho mọi bước: tham số 1 = mã bước, còn lại = bản ghi lisp. Bản ghi C#
        /// do bước đó đặt sẵn vào phiên bằng khoá "ab_&lt;buoc&gt;" (List&lt;SoSanh.BanGhi&gt;),
        /// dung sai "absai_&lt;buoc&gt;" (mặc định 0.01). In kết quả, ghi nhật ký phiên.
        /// </summary>
        [LispFunction("GRO_NET_AB")]
        public static object LispAB(ResultBuffer rb)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            try
            {
                List<string> a = ChuoiTuRb(rb);
                if (a.Count == 0) return null;
                string buoc = GroCongTac.ChuanMa(a[0]);
                GroPhien p = PhienHienTai();
                var cs = p.Doc<List<SoSanh.BanGhi>>("ab_" + buoc);
                if (cs == null)
                {
                    ed?.WriteMessage("\n[AB " + buoc + "] C# chua tinh buoc nay trong phien - bo qua so sanh.");
                    return null;
                }
                var sai = p.Doc<object>("absai_" + buoc);
                double dungSai = sai is double d ? d : 0.01;
                SoSanh.KetQua kq = SoSanh.So(buoc, SoSanh.DocChuoi(a.GetRange(1, a.Count - 1)), cs, dungSai);
                string dong = kq.InRa();
                p.NhatKyAB.Add(dong);
                ed?.WriteMessage("\n" + dong);
                return kq.KhopHet ? (object)true : null;
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                ed?.WriteMessage("\n[AB] Loi so sanh: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Mục 172 (GĐ 5a): (gro_net_quet_hinh "ab"|"cs" "K|x1,y1,x2,y2" ... "P|x,y" ...) — quét + gom
        /// hình 0-KAKOU. Trả (hinh loi-khung) đúng cấu trúc gro--quet-hinh-primitives, hoặc
        /// "KHONG_KAKOU". Lỗi -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_QUET_HINH")]
        public static object LispQuetHinh(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return null;
            try
            {
                List<string> a = ChuoiTuRb(rb);
                string cheDo = a.Count > 0 ? GroCongTac.TenCheDo(GroCongTac.DocCheDo(a[0])) : "cs";
                return CadServices.QuetHinhCad.Chay(doc, cheDo, a.Count > 1 ? a.GetRange(1, a.Count - 1) : new List<string>());
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi quet hinh: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 173 (GĐ 3a): (gro_net_phan_loai_net "ab"|"cs" lay-goc ss ss-cir-all ss-arc-all) — phân
        /// loại nét cho nhận Dim. 3 selection set (hoặc nil) đúng thứ tự lisp duyệt. Trả
        /// (lines-cont arcs-cont circles tiny-circles). Lỗi / tham số lạ -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_PHAN_LOAI_NET")]
        public static object LispPhanLoaiNet(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                TypedValue[] tv = rb.AsArray();
                if (tv.Length < 5 || !(tv[0].Value is string che) || !(tv[1].Value is string lay)) return null;
                var bo = new List<IList<ObjectId>>();
                for (int i = 2; i < 5; i++)
                {
                    if (tv[i].Value is SelectionSet s) bo.Add(s.GetObjectIds());
                    else if (tv[i].TypeCode == (short)LispDataType.Nil || tv[i].Value == null) bo.Add(new ObjectId[0]);
                    else
                    {
                        doc.Editor?.WriteMessage("\n[GRO .NET] phan loai net: tham so " + (i + 1) + " khong phai selection set (ma "
                                                 + tv[i].TypeCode + ") -> dung ban lisp.");
                        return null;
                    }
                }
                return CadServices.NhanNetCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)), lay, bo[0], bo[1], bo[2]);
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi phan loai net: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 174 (GĐ 5c-a): (gro_net_dim_dai "ab"|"cs" texts lines) — khớp Dim dài trong thu thập
        /// Dim. texts = ((ename v pt rot h bb) ...), lines = (ename ...). Trả danh sách song song texts.
        /// Lỗi -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_DIM_DAI")]
        public static object LispDimDai(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                List<object> a = CadServices.LispCay.Doc(rb);
                if (a.Count < 3 || !(a[0] is string che)) return null;
                return CadServices.DimDaiCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)),
                                                  CadServices.LispCay.Ds(a[1]), CadServices.LispCay.Ds(a[2]));
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi khop Dim dai: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 175 (GĐ 3b): (gro_net_dim_dai_3b "ab"|"cs" texts lines) — khớp Dim dài ở bước xử lý nét
        /// (gro--geom-match-linear-dims). Trả (res-dim-ents ((chi-so so-line) ...)). Lỗi -> nil.
        /// </summary>
        [LispFunction("GRO_NET_DIM_DAI_3B")]
        public static object LispDimDai3b(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                List<object> a = CadServices.LispCay.Doc(rb);
                if (a.Count < 3 || !(a[0] is string che)) return null;
                return CadServices.DimDai3bCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)),
                                                    CadServices.LispCay.Ds(a[1]), CadServices.LispCay.Ds(a[2]));
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi khop Dim dai 3b: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 176 (GĐ 5c-b): (gro_net_loc_dim_net "ab"|"cs" ss-all-dims note-bbs all-k-ents) — vòng lọc đầu
        /// thu thập Dim. Trả (lines-cont arcs-cont native-dims). Lỗi -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_LOC_DIM_NET")]
        public static object LispLocDimNet(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                List<object> a = CadServices.LispCay.Doc(rb);
                if (a.Count < 4 || !(a[0] is string che) || !(a[1] is SelectionSet ss)) return null;
                return CadServices.LocDimNetCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)), ss.GetObjectIds(),
                                                     CadServices.LispCay.Ds(a[2]), CadServices.LispCay.Ds(a[3]));
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi loc net Dim: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 177 (GĐ 3c): (gro_net_vong_kin "ab"|"cs" lay-goc ss lines-cont arcs-cont) — dò vòng kín biên
        /// dạng ở bước nhận Dim. Trả (vk-geo lines-cont arcs-cont). Lỗi -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_VONG_KIN")]
        public static object LispVongKin(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                List<object> a = CadServices.LispCay.Doc(rb);
                if (a.Count < 5 || !(a[0] is string che) || !(a[1] is string lay) || !(a[2] is SelectionSet ss)) return null;
                return CadServices.VongKinCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)), lay, ss.GetObjectIds(),
                                                   CadServices.LispCay.Ds(a[3]), CadServices.LispCay.Ds(a[4]));
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi do vong kin: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 178 (GĐ 6a): (gro_net_xep_cum "ab"|"cs" ss-model cums bb-goc es-khac ds-khung-bo hop-khung-bo
        /// mirror-khe mirror-le) — bố cục cột phải + dời cụm + kiểm còn sót. "cs" dời thật, "ab" chỉ tính.
        /// Trả (X_L da-ne layout nhan n-sot ds-sot n-dim-theo). Lỗi -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_XEP_CUM")]
        public static object LispXepCum(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                List<object> a = CadServices.LispCay.Doc(rb);
                if (a.Count < 9 || !(a[0] is string che) || !(a[1] is SelectionSet ss)) return null;
                double? mk = CadServices.LispCay.So(a[7]), ml = CadServices.LispCay.So(a[8]);
                if (mk == null || ml == null) return null;
                return CadServices.XepCumCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)), ss.GetObjectIds(),
                    CadServices.LispCay.Ds(a[2]), a[3], CadServices.LispCay.Ds(a[4]), CadServices.LispCay.Ds(a[5]),
                    CadServices.LispCay.Ds(a[6]), mk.Value, ml.Value);
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi xep cum: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 180 (GĐ 6f): (gro_net_tach_gian "ab"|"cs" khungs bo-qua gap max-iter) — tách va chạm khung in.
        /// Trả (((vi-tri tid dx dy) ...) con-chong). Lỗi -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_TACH_GIAN")]
        public static object LispTachGian(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                List<object> a = CadServices.LispCay.Doc(rb);
                if (a.Count < 5 || !(a[0] is string che)) return null;
                double? gap = CadServices.LispCay.So(a[3]), mi = CadServices.LispCay.So(a[4]);
                return CadServices.TachVaChamCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)),
                    CadServices.LispCay.Ds(a[1]), CadServices.LispCay.Ds(a[2]), gap ?? 0.0, (int)(mi ?? 0));
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi tach va cham khung in: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 181 (GĐ 5d-a): (gro_net_ds_chi_tiet "ab"|"cs" ss-kakou-keg muc) — chia nét cắt / kegaki về tấm
        /// trong danh sách chi tiết. Trả (so-net ((pt9...) ...)). Lỗi -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_DS_CHI_TIET")]
        public static object LispDsChiTiet(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                List<object> a = CadServices.LispCay.Doc(rb);
                if (a.Count < 3 || !(a[0] is string che)) return null;
                IList<ObjectId> ids = a[1] is SelectionSet ss ? (IList<ObjectId>)ss.GetObjectIds() : new ObjectId[0];
                return CadServices.ChiaNetCatCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)), ids, CadServices.LispCay.Ds(a[2]));
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi danh sach chi tiet: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// Mục 183 (GĐ 5d-b): (gro_net_dong_khung "ab"|"cs" tams) — đóng khung + vẽ Khung_IN.
        /// Trả ((bb-final ename-khung (co-giu-dim...)) ...). Lỗi -> nil (lisp tự chạy bản lisp).
        /// </summary>
        [LispFunction("GRO_NET_DONG_KHUNG")]
        public static object LispDongKhung(ResultBuffer rb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || rb == null) return null;
            try
            {
                List<object> a = CadServices.LispCay.Doc(rb);
                if (a.Count < 2 || !(a[0] is string che)) return null;
                return CadServices.DongKhungCad.Chay(doc, GroCongTac.TenCheDo(GroCongTac.DocCheDo(che)), CadServices.LispCay.Ds(a[1]));
            }
            catch (System.Exception ex)
            {
                ZwCadPlugin.OhtaBugBridge.Log("GRO/GroBuocCommands", ex);
                doc.Editor?.WriteMessage("\n[GRO .NET] Loi dong khung: " + ex.Message + " -> dung ban lisp.");
                return null;
            }
        }

        /// <summary>
        /// GROCS: in bảng chế độ từng bước; chọn bước rồi chọn Lisp / Ab / Cs. "TAT" đưa mọi bước
        /// về lisp. Bước chưa có bản C# thì dù đặt ab / cs, lisp vẫn chạy bản lisp (hàm cầu nối báo).
        /// </summary>
        [CommandMethod("GROCS")]
        public static void LenhGroCs()
        {
            OhtaBugBridge.Run("GROCS", () =>
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                Editor ed = doc.Editor;
                while (true)
                {
                    ed.WriteMessage("\n=== Che do tung buoc GRO (lisp / ab = so A/B / cs = chay C#) ===");
                    foreach (var b in GroCongTac.Buoc)
                        ed.WriteMessage("\n  " + b.Key.PadRight(10) + GroCongTac.TenCheDo(DocCheDo(b.Key)).PadRight(6) + b.Value);
                    var po = new PromptStringOptions("\nTen buoc can doi (Enter = thoat, TAT = tat het ve lisp): ") { AllowSpaces = false };
                    PromptResult r = ed.GetString(po);
                    if (r.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(r.StringResult)) return;
                    string ma = GroCongTac.ChuanMa(r.StringResult);
                    if (ma == "tat")
                    {
                        foreach (var b in GroCongTac.Buoc) GhiCheDo(b.Key, CheDoBuoc.Lisp);
                        ed.WriteMessage("\nDa dua MOI buoc ve lisp.");
                        continue;
                    }
                    if (!GroCongTac.LaBuoc(ma)) { ed.WriteMessage("\nKhong co buoc \"" + ma + "\"."); continue; }
                    var pk = new PromptKeywordOptions("\nChe do cho " + ma + " [Lisp/Ab/Cs]: ");
                    pk.Keywords.Add("Lisp");
                    pk.Keywords.Add("Ab");
                    pk.Keywords.Add("Cs");
                    pk.Keywords.Default = GroCongTac.TenCheDo(DocCheDo(ma)) == "ab" ? "Ab" :
                                          GroCongTac.TenCheDo(DocCheDo(ma)) == "cs" ? "Cs" : "Lisp";
                    PromptResult k = ed.GetKeywords(pk);
                    if (k.Status != PromptStatus.OK) return;
                    GhiCheDo(ma, GroCongTac.DocCheDo(k.StringResult));
                    ed.WriteMessage("\nDa dat " + ma + " = " + GroCongTac.TenCheDo(DocCheDo(ma)) + ".");
                }
            });
        }
    }
}
