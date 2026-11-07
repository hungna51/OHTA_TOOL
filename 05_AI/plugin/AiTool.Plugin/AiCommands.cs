using System;
using System.Collections.Generic;
using System.Linq;
using AiTool.CadServices;
using AiTool.Core;
using AiTool.ViewModels;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;
using CadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadApplication = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

[assembly: CommandClass(typeof(AiTool.AiCommands))]

namespace AiTool
{
    /// <summary>
    /// Lệnh của tab AI. Chỉ ĐỌC bản vẽ (quét khung) và zoom — không ghi gì vào bản vẽ.
    /// Thông báo dòng lệnh viết không dấu (dòng lệnh ZWCAD không chắc hiện được dấu).
    /// </summary>
    public class AiCommands
    {
        [CommandMethod("OHTAAI")]
        public void KiemTraCaBanVe() => Chay(null);

        [CommandMethod("OHTAAIVUNG")]
        public void KiemTraVung()
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null || CanTab(doc) == null) return;
            Editor ed = doc.Editor;
            PromptPointResult p1 = ed.GetPoint("\n[AI] Goc thu nhat cua vung: ");
            if (p1.Status != PromptStatus.OK) return;
            PromptPointResult p2 = ed.GetCorner("\n[AI] Goc doi dien: ", p1.Value);
            if (p2.Status != PromptStatus.OK) return;

            Matrix3d ucs = ed.CurrentUserCoordinateSystem;
            Point3d a = p1.Value.TransformBy(ucs), b = p2.Value.TransformBy(ucs);
            var vung = new Extents3d(new Point3d(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), 0),
                                     new Point3d(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), 0));
            Chay(vung);
        }

        /// <summary>Nút 📎 Gửi khung của màn hình chat: quét vùng → mô tả gọn các khung in đính kèm câu hỏi kế tiếp.</summary>
        [CommandMethod("OHTAAICHATKHUNG")]
        public void ChatGuiKhung()
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            AiTabViewModel vm = doc == null ? null : CanTab(doc);
            if (vm == null) return;
            Extents3d? vung = ChonVung(doc.Editor, "\n[AI] Chon khung gui chat - goc thu nhat: ");
            if (vung == null) return;
            try
            {
                List<KhungDuLieu> khung = QuetKhung.Quet(doc, vm.LayerKhung, vung);
                if (khung.Count == 0)
                {
                    doc.Editor.WriteMessage("\n[AI] Khong co khung in (layer " + vm.LayerKhung + ") trong vung chon.");
                    return;
                }
                string ten = System.IO.Path.GetFileNameWithoutExtension(doc.Name);
                vm.Chat.DatDinhKem(ChatLing.GhepDinhKem(MoTaKhung.Md(ten, khung), MoTaKhung.Json(ten, khung)));
                doc.Editor.WriteMessage("\n[AI] Da dinh kem " + khung.Count + " khung - go cau hoi o tab AI roi bam Gui.");
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("\n[AI] Loi doc ban ve: " + ex.Message);
            }
        }

        private static Extents3d? ChonVung(Editor ed, string loiNhac)
        {
            PromptPointResult p1 = ed.GetPoint(loiNhac);
            if (p1.Status != PromptStatus.OK) return null;
            PromptPointResult p2 = ed.GetCorner("\n[AI] Goc doi dien: ", p1.Value);
            if (p2.Status != PromptStatus.OK) return null;
            Matrix3d ucs = ed.CurrentUserCoordinateSystem;
            Point3d a = p1.Value.TransformBy(ucs), b = p2.Value.TransformBy(ucs);
            return new Extents3d(new Point3d(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), 0),
                                 new Point3d(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), 0));
        }

        /// <summary>Quét chọn vùng → ghi &lt;ban ve&gt;.ai.md + .ai.json (mô tả từng khung in) vào OHTA_AI\mo-ta cạnh file bản vẽ.</summary>
        [CommandMethod("OHTAAIMOTA")]
        public void XuatMoTa()
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            AiTabViewModel vm = doc == null ? null : CanTab(doc);
            if (vm == null) return;
            Editor ed = doc.Editor;
            string fileBv = doc.Database.Filename;
            string thuMuc = ChatLing.ThuMucMoTa(fileBv);
            if (thuMuc == null)
            {
                ed.WriteMessage("\n[AI] Luu ban ve truoc khi xuat mo ta.");
                return;
            }
            Extents3d? vung = ChonVung(ed, "\n[AI] Chon khung in - goc thu nhat: ");
            if (vung == null) return;
            try
            {
                List<KhungDuLieu> khung = QuetKhung.Quet(doc, vm.LayerKhung, vung);
                if (khung.Count == 0)
                {
                    ed.WriteMessage("\n[AI] Khong co khung in (layer " + vm.LayerKhung + ") trong vung chon - chay KI de tao khung truoc.");
                    return;
                }
                System.IO.Directory.CreateDirectory(thuMuc);
                string ten = System.IO.Path.GetFileNameWithoutExtension(fileBv);
                var utf8 = new System.Text.UTF8Encoding(false);
                string md = System.IO.Path.Combine(thuMuc, ten + ".ai.md");
                System.IO.File.WriteAllText(md, MoTaKhung.Md(ten, khung), utf8);
                System.IO.File.WriteAllText(System.IO.Path.Combine(thuMuc, ten + ".ai.json"), MoTaKhung.Json(ten, khung), utf8);
                string kienThuc = System.IO.Path.Combine(CauHinhKev.DuongDanThuMucKev(vm.ThuMucModule), MoTaKhung.FileKienThuc);
                if (System.IO.File.Exists(kienThuc))
                    System.IO.File.Copy(kienThuc, System.IO.Path.Combine(thuMuc, MoTaKhung.FileKienThuc), true);
                ed.WriteMessage("\n[AI] Da xuat mo ta " + khung.Count + " tam: " + md);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[AI] Loi xuat mo ta (file dang mo o chuong trinh khac?): " + ex.Message);
            }
        }

        [CommandMethod("OHTAAIZOOM")]
        public void ZoomKhungCanhBao()
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            AiTabViewModel vm = doc == null ? null : CanTab(doc);
            if (vm == null) return;
            PromptIntegerResult r = doc.Editor.GetInteger("\n[AI] So thu tu khung: ");
            if (r.Status != PromptStatus.OK) return;
            KhungDuLieu k = vm.KhungLanCuoi.FirstOrDefault(x => x.SoThuTu == r.Value);
            if (k == null)
            {
                doc.Editor.WriteMessage("\n[AI] Khong con khung " + r.Value + " (kiem tra lai?).");
                return;
            }
            ZoomKhung.Toi(doc, k.MinX, k.MinY, k.MaxX, k.MaxY);
        }

        private static void Chay(Extents3d? vung)
        {
            Document doc = CadApplication.DocumentManager.MdiActiveDocument;
            AiTabViewModel vm = doc == null ? null : CanTab(doc);
            if (vm == null) return;
            List<KhungDuLieu> khung;
            try
            {
                khung = QuetKhung.Quet(doc, vm.LayerKhung, vung);
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("\n[AI] Loi doc ban ve: " + ex.Message);
                return;
            }
            doc.Editor.WriteMessage("\n[AI] Tim thay " + khung.Count + " khung tren layer " + vm.LayerKhung +
                                    (khung.Count > 0 ? " -> dang gui AI, xem ket qua o tab AI." : "."));
            vm.BatDauKiemTra(khung);
        }

        private static AiTabViewModel CanTab(Document doc)
        {
            AiTabViewModel vm = AiTabViewModel.HienTai;
            if (vm == null)
                doc.Editor.WriteMessage("\n[AI] Mo tab AI tren bang OHTA truoc (lenh OHTA).");
            return vm;
        }
    }
}
