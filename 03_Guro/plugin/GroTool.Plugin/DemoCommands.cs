using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.Runtime;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Colors;

namespace ZwCadPlugin
{
    public class DemoCommands
    {
        /// <summary>
        /// Lệnh 1: CS_DOIMAU
        /// Quét chọn các đối tượng bất kỳ trên bản vẽ, đọc thông tin Layer, loại đối tượng,
        /// và đổi màu các đối tượng đã chọn sang màu Đỏ (ColorIndex = 1).
        /// </summary>
        [CommandMethod("CS_DOIMAU")]
        public void DoiMauDoiTuong()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1. Nhắc người dùng quét chọn đối tượng
            PromptSelectionOptions opt = new PromptSelectionOptions();
            opt.MessageForAdding = "\n[C# Plugin] Quet chon cac doi tuong can kiem tra va doi mau: ";
            
            PromptSelectionResult selRes = ed.GetSelection(opt);
            if (selRes.Status != PromptStatus.OK || selRes.Value == null)
            {
                ed.WriteMessage("\n[C# Plugin] Ban da huy chon hoac khong co doi tuong nao duoc chon.\n");
                return;
            }

            SelectionSet ss = selRes.Value;
            ed.WriteMessage($"\n--- [C# Plugin] Dang xu ly {ss.Count} doi tuong... ---");

            // Thống kê theo layer và loại đối tượng
            Dictionary<string, int> layerCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> typeCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // 2. Mở Transaction để thao tác an toàn với Database của ZWCAD
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                int modifiedCount = 0;

                foreach (SelectedObject selObj in ss)
                {
                    if (selObj == null) continue;

                    // Mở đối tượng ở chế độ ghi (ForWrite) để sửa màu
                    DBObject obj = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite);
                    if (obj is Entity ent)
                    {
                        // Thống kê loại đối tượng
                        string typeName = ent.GetType().Name;
                        if (!typeCount.ContainsKey(typeName)) typeCount[typeName] = 0;
                        typeCount[typeName]++;

                        // Thống kê Layer
                        string layerName = ent.Layer;
                        if (!layerCount.ContainsKey(layerName)) layerCount[layerName] = 0;
                        layerCount[layerName]++;

                        // Đổi màu sang màu Đỏ (ColorIndex: 1 = Red, 2 = Yellow, 3 = Green, 4 = Cyan...)
                        ent.ColorIndex = 1;
                        modifiedCount++;
                    }
                }

                // 3. Xác nhận lưu thay đổi vào cơ sở dữ liệu bản vẽ
                tr.Commit();

                // 4. In báo cáo thống kê ra cửa sổ Command Line của ZWCAD
                ed.WriteMessage($"\n-> Thanh cong! Da doi mau {modifiedCount} doi tuong sang mau DO (Red).");
                ed.WriteMessage("\n--- THONG KE THEO LOAI DOI TUONG ---");
                foreach (var kvp in typeCount)
                {
                    ed.WriteMessage($"\n   + {kvp.Key}: {kvp.Value} doi tuong");
                }

                ed.WriteMessage("\n--- THONG KE THEO LAYER ---");
                foreach (var kvp in layerCount)
                {
                    ed.WriteMessage($"\n   * Layer [{kvp.Key}]: {kvp.Value} doi tuong");
                }
                ed.WriteMessage("\n----------------------------------------\n");
            }
        }

        /// <summary>
        /// Lệnh 2: CS_DOCCHU
        /// Quét chọn chuyên biệt cho Text và MText, đọc nội dung và layer,
        /// minh họa cách lọc đối tượng (Selection Filter) bằng C#.
        /// </summary>
        [CommandMethod("CS_DOCCHU")]
        public void DocChuVaLayer()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1. Tạo bộ lọc: Chỉ cho phép chọn TEXT hoặc MTEXT (tương tự '((0 . "TEXT,MTEXT")) trong LISP)
            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "TEXT,MTEXT")
            };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionOptions opt = new PromptSelectionOptions();
            opt.MessageForAdding = "\n[C# Plugin] Quet chon cac chu Text / MText can doc: ";

            PromptSelectionResult selRes = ed.GetSelection(opt, filter);
            if (selRes.Status != PromptStatus.OK || selRes.Value == null)
            {
                ed.WriteMessage("\n[C# Plugin] Khong co chu Text/MText nao duoc chon.\n");
                return;
            }

            SelectionSet ss = selRes.Value;
            ed.WriteMessage($"\n--- [C# Plugin] Tim thay {ss.Count} doi tuong Text/MText ---");

            // 2. Mở Transaction ở chế độ đọc (ForRead) để tối ưu tốc độ
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                int index = 1;
                foreach (SelectedObject selObj in ss)
                {
                    if (selObj == null) continue;

                    DBObject obj = tr.GetObject(selObj.ObjectId, OpenMode.ForRead);
                    string textContent = "";
                    string layerName = "";

                    if (obj is DBText dbText)
                    {
                        textContent = dbText.TextString;
                        layerName = dbText.Layer;
                    }
                    else if (obj is MText mText)
                    {
                        textContent = mText.Contents;
                        layerName = mText.Layer;
                    }

                    ed.WriteMessage($"\n [{index++}] Layer: \"{layerName}\" | Noi dung: \"{textContent}\"");
                }

                // Không cần tr.Commit() vì chỉ đọc dữ liệu
            }
            ed.WriteMessage("\n----------------------------------------------------\n");
        }
    }
}
