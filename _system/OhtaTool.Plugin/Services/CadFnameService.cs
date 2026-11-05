#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.GraphicsInterface;
using ZwSoft.ZwCAD.Runtime;
#elif AUTOCAD
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.ViewModels;

[assembly: CommandClass(typeof(OhtaTool.Plugin.Services.FnameCadCommands))]

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Lưu tham chiếu tới cụm đối tượng F-NAME vừa được Hút trên bản vẽ để Cập nhật tại chỗ 1 chạm.
    /// </summary>
    public sealed class PickedFnameTarget
    {
        public CadDocument Document { get; set; }
        public bool IsMText { get; set; }
        public List<ObjectId> EntityIds { get; set; } = new List<ObjectId>();
        public List<string> ExtraBeforeZ { get; set; } = new List<string>();
        public List<string> ExtraAfterZ { get; set; } = new List<string>();

        public bool IsValidFor(CadDocument activeDoc)
        {
            if (activeDoc == null || Document != activeDoc || EntityIds == null || EntityIds.Count == 0)
                return false;

            return EntityIds.All(id => !id.IsNull && !id.IsErased && id.IsValid);
        }
    }

    /// <summary>
    /// DrawJig cho phép người dùng kéo thả chuột trực tiếp trên bản vẽ để phóng to / thu nhỏ chiều cao chữ F-NAME.
    /// Click 1 = Điểm góc trên-trái; Di chuột = Xem trước cả khối N=, M=, T=, Q=, Z= co giãn; Click 2 = Chốt chiều cao.
    /// </summary>
    internal sealed class FnameScaleJig : DrawJig, IDisposable
    {
        private readonly Point3d _baseWcs;
        private readonly Point3d _baseUcs;
        private readonly Matrix3d _wcsToUcs;
        private readonly bool _useMText;
        private readonly List<string> _lines;
        private readonly int _maxLineChars;

        private Point3d _lastSampleWcs;
        private double _currentHeight;
        private readonly MText _previewMText;
        private readonly List<DBText> _previewTexts;

        public FnameScaleJig(
            Database db,
            ObjectId textStyleId,
            Point3d baseWcs,
            Point3d baseUcs,
            Matrix3d wcsToUcs,
            List<string> lines,
            bool useMText,
            double fallbackHeight)
        {
            _baseWcs = baseWcs;
            _baseUcs = baseUcs;
            _wcsToUcs = wcsToUcs;
            _useMText = useMText;
            _lines = lines ?? new List<string>();
            _maxLineChars = _lines.Count > 0 ? _lines.Max(s => s != null ? s.Length : 0) : 8;
            _currentHeight = fallbackHeight > 0.5 ? fallbackHeight : 20.0;
            _lastSampleWcs = baseWcs;

            if (_useMText)
            {
                _previewMText = new MText();
                _previewMText.SetDatabaseDefaults(db);
                _previewMText.TextStyleId = textStyleId;
                _previewMText.ColorIndex = 3;
                _previewMText.Attachment = AttachmentPoint.TopLeft;
                _previewMText.Width = 0.0;
                _previewMText.Location = _baseWcs;
                _previewMText.TextHeight = _currentHeight;
                _previewMText.Contents = string.Join("\\P", _lines);
            }
            else
            {
                _previewTexts = new List<DBText>(_lines.Count);
                for (int i = 0; i < _lines.Count; i++)
                {
                    var dt = new DBText();
                    dt.SetDatabaseDefaults(db);
                    dt.TextStyleId = textStyleId;
                    dt.ColorIndex = 3;
                    dt.TextString = _lines[i];
                    _previewTexts.Add(dt);
                }
                SyncPreviewTextPositions();
            }
        }

        public double SelectedHeight => _currentHeight;

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var jppo = new JigPromptPointOptions(
                "\n[OHTA Fname] Kéo chuột chỉnh cỡ chữ F-NAME và bấm điểm thứ 2 để chốt (Enter = dùng cỡ hiện tại): ")
            {
                BasePoint = _baseWcs,
                UseBasePoint = true,
                UserInputControls = UserInputControls.Accept3dCoordinates |
                                    UserInputControls.NullResponseAccepted |
                                    UserInputControls.NoZeroResponseAccepted |
                                    UserInputControls.NoNegativeResponseAccepted
            };

            PromptPointResult res = prompts.AcquirePoint(jppo);
            if (res.Status == PromptStatus.Cancel || res.Status == PromptStatus.Error)
                return SamplerStatus.Cancel;

            if (res.Status != PromptStatus.OK)
                return SamplerStatus.NoChange;

            Point3d cursorWcs = res.Value;
            if (cursorWcs.DistanceTo(_lastSampleWcs) < 1e-4)
                return SamplerStatus.NoChange;

            _lastSampleWcs = cursorWcs;
            Point3d cursorUcs = cursorWcs.TransformBy(_wcsToUcs);
            double dx = cursorUcs.X - _baseUcs.X;
            double dy = cursorUcs.Y - _baseUcs.Y;

            // Khi con trỏ rời điểm gốc đủ rõ (> 0.5 đơn vị bản vẽ), tính chiều cao theo khung kéo
            if (Math.Abs(dx) > 0.5 || Math.Abs(dy) > 0.5)
            {
                double nextH = FnameParser.ComputeDraggedTextHeight(dx, dy, _lines.Count, _maxLineChars, minHeight: 0.5);
                if (Math.Abs(nextH - _currentHeight) >= 0.05)
                {
                    _currentHeight = nextH;
                    if (_useMText && _previewMText != null)
                        _previewMText.TextHeight = _currentHeight;
                    else
                        SyncPreviewTextPositions();
                }
            }

            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (draw?.Geometry == null)
                return true;

            if (_useMText)
            {
                if (_previewMText != null)
                    draw.Geometry.Draw(_previewMText);
            }
            else if (_previewTexts != null)
            {
                for (int i = 0; i < _previewTexts.Count; i++)
                {
                    draw.Geometry.Draw(_previewTexts[i]);
                }
            }

            return true;
        }

        private void SyncPreviewTextPositions()
        {
            if (_previewTexts == null)
                return;

            double h = _currentHeight;
            double lineStep = h * 5.0 / 3.0;
            // Đặt baseline dòng 0 tại (baseY - h) để mép trên của chữ N= cố định đúng tại điểm Click 1
            double firstBaselineY = _baseWcs.Y - h;

            for (int i = 0; i < _previewTexts.Count; i++)
            {
                DBText dt = _previewTexts[i];
                dt.Height = h;
                dt.Position = new Point3d(_baseWcs.X, firstBaselineY - i * lineStep, _baseWcs.Z);
            }
        }

        public void Dispose()
        {
            try { _previewMText?.Dispose(); } catch { }
            if (_previewTexts != null)
            {
                foreach (DBText dt in _previewTexts)
                {
                    try { dt?.Dispose(); } catch { }
                }
                _previewTexts.Clear();
            }
        }
    }

    /// <summary>
    /// Các lệnh CAD thực thi tương tác chuột từ Tab Fname.
    /// </summary>
    public static class FnameCadCommands
    {
        public static FnameTabViewModel ActiveViewModel { get; set; }

        [CommandMethod("OHTA_FN_PLACE")]
        public static void CmdPlaceFname()
        {
            BugGuard.Run("OHTA_FN_PLACE", () =>
            {
                FnameTabViewModel vm = ActiveViewModel;
                if (vm == null)
                {
                    CadCompat.WriteMessage("\n[OHTA Fname] Hãy mở bảng OHTA (Tab Fname) trước.\n");
                    return;
                }

                vm.SyncActiveDrawing();
                CadFnameService.PlaceNewFnameInteractive(vm);
            });
        }

        [CommandMethod("OHTA_FN_PICK")]
        public static void CmdPickFname()
        {
            BugGuard.Run("OHTA_FN_PICK", () =>
            {
                FnameTabViewModel vm = ActiveViewModel;
                if (vm == null)
                {
                    CadCompat.WriteMessage("\n[OHTA Fname] Hãy mở bảng OHTA (Tab Fname) trước.\n");
                    return;
                }

                vm.SyncActiveDrawing();
                CadFnameService.PickExistingFnameInteractive(vm);
            });
        }

        [CommandMethod("OHTA_FN_UPDATE")]
        public static void CmdPickAndUpdateFname()
        {
            BugGuard.Run("OHTA_FN_UPDATE", () =>
            {
                FnameTabViewModel vm = ActiveViewModel;
                if (vm == null)
                {
                    CadCompat.WriteMessage("\n[OHTA Fname] Hãy mở bảng OHTA (Tab Fname) trước.\n");
                    return;
                }

                vm.SyncActiveDrawing();
                CadFnameService.PickAndOverwriteFnameInteractive(vm);
            });
        }

        [CommandMethod("OHTA_FN_PICK_ANGLE")]
        public static void CmdPickAngle()
        {
            BugGuard.Run("OHTA_FN_PICK_ANGLE", () =>
            {
                CadFnameService.PickAngleInteractive();
            });
        }
    }

    public static class CadFnameService
    {
        public const string FnameLayerName = "2-F-NAME";
        public const string PreferredStyleName = "Text";

        public static Action<double> PendingAngleReceiver { get; set; }

        public static void PickAngleInteractive()
        {
            CadDocument doc = CadCompat.ActiveDocument;
            if (doc == null)
            {
                PendingAngleReceiver = null;
                return;
            }

            Editor ed = doc.Editor;
            var peo = new PromptEntityOptions("\n[OHTA Fname] Chọn dimension đo góc trên bản vẽ (hoặc Enter để hủy): ");
            peo.SetRejectMessage("\nVui lòng chọn đối tượng Dimension đo góc.");
            peo.AddAllowedClass(typeof(Dimension), exactMatch: false);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                PendingAngleReceiver = null;
                return;
            }

            try
            {
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent is Dimension dim)
                    {
                        double rad = dim.Measurement;
                        if (rad > 0)
                        {
                            double deg = rad * 180.0 / Math.PI;
                            if (PendingAngleReceiver != null)
                            {
                                PendingAngleReceiver(deg);
                                PendingAngleReceiver = null;
                            }
                            CadCompat.WriteMessage($"\n[OHTA Fname] Đã nhận góc đo: {deg:0.##}°\n");
                        }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                CadCompat.WriteMessage($"\n[OHTA Fname] Lỗi đo góc: {ex.Message}\n");
            }
            finally
            {
                PendingAngleReceiver = null;
            }
        }

        /// <summary>
        /// Bước 1: Chọn điểm góc trên-trái.
        /// Bước 2: Kéo thả trực tiếp (DrawJig) để chỉnh chiều cao F-NAME theo kích thước tấm rồi bấm chốt.
        /// </summary>
        public static void PlaceNewFnameInteractive(FnameTabViewModel vm)
        {
            CadDocument doc = CadCompat.ActiveDocument;
            if (doc == null)
                return;

            if (!vm.ValidateReadyForExport(out FnameRecord record, showPromptIfMissingInCatalog: true))
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;
            var ppo = new PromptPointOptions("\n[OHTA Fname] Chọn điểm đặt góc trên-trái của F-NAME: ")
            {
                AllowNone = false
            };

            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                vm.SetFeedback("Đã hủy đặt Fname.", isWarning: false);
                return;
            }

            Matrix3d ucsToWcs = ed.CurrentUserCoordinateSystem;
            Matrix3d wcsToUcs = ucsToWcs.Inverse();
            Point3d basePointUcs = ppr.Value;
            Point3d wcsPoint = basePointUcs.TransformBy(ucsToWcs);

            bool useMText = vm.IsMTextMode;
            List<string> lines = record.BuildLines(includePreservedExtras: false);
            double textHeight = vm.LastDraggedHeight;

            try
            {
                ObjectId styleId;
                using (Transaction trPrep = db.TransactionManager.StartTransaction())
                {
                    EnsureFnameLayer(db, trPrep);
                    styleId = EnsurePreferredTextStyle(db, trPrep);
                    trPrep.Commit();
                }

                // Thực thi kéo thả trực tiếp trên bản vẽ để người dùng định cỡ chiều cao F-NAME
                using (var jig = new FnameScaleJig(db, styleId, wcsPoint, basePointUcs, wcsToUcs, lines, useMText, textHeight))
                {
                    PromptResult dragRes = ed.Drag(jig);
                    if (dragRes.Status == PromptStatus.Cancel || dragRes.Status == PromptStatus.Error)
                    {
                        vm.SetFeedback("Đã hủy kéo thả đặt Fname.", isWarning: false);
                        return;
                    }

                    // OK (bấm điểm thứ 2) hoặc None (nhấn Enter dùng ngay cỡ đang hiện)
                    textHeight = jig.SelectedHeight;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    EnsureFnameLayer(db, tr);
                    BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    var createdIds = new List<ObjectId>();
                    if (useMText)
                    {
                        var mt = new MText();
                        mt.SetDatabaseDefaults(db);
                        mt.Layer = FnameLayerName;
                        mt.TextStyleId = styleId;
                        mt.TextHeight = textHeight;
                        mt.Attachment = AttachmentPoint.TopLeft;
                        mt.Width = 0.0;
                        mt.Location = wcsPoint;
                        mt.Contents = string.Join("\\P", lines);

                        ObjectId id = space.AppendEntity(mt);
                        tr.AddNewlyCreatedDBObject(mt, true);
                        createdIds.Add(id);
                    }
                    else
                    {
                        double lineStep = textHeight * 5.0 / 3.0;
                        double firstBaselineY = wcsPoint.Y - textHeight;
                        for (int i = 0; i < lines.Count; i++)
                        {
                            var dt = new DBText();
                            dt.SetDatabaseDefaults(db);
                            dt.Layer = FnameLayerName;
                            dt.TextStyleId = styleId;
                            dt.Height = textHeight;
                            dt.Position = new Point3d(wcsPoint.X, firstBaselineY - i * lineStep, wcsPoint.Z);
                            dt.TextString = lines[i];

                            ObjectId id = space.AppendEntity(dt);
                            tr.AddNewlyCreatedDBObject(dt, true);
                            createdIds.Add(id);
                        }
                    }

                    tr.Commit();

                    vm.NotifyPlacedSuccessfully(
                        new PickedFnameTarget
                        {
                            Document = doc,
                            IsMText = useMText,
                            EntityIds = createdIds
                        },
                        textHeight);
                }

                ed.UpdateScreen();
                CadCompat.WriteMessage($"\n[OHTA Fname] Đã đặt F-NAME (Cao chữ={textHeight:0.#}, N={record.N}, M={record.M}, T={record.T}, Q={record.Q}, Z={record.Z}) lên layer {FnameLayerName}.\n");
            }
            catch (System.Exception ex)
            {
                vm.SetFeedback("Lỗi khi ghi Fname: " + ex.Message, isWarning: true);
                CadCompat.WriteMessage($"\n[OHTA Fname] Lỗi ghi Fname: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Cho người dùng bấm chọn một dòng TEXT hoặc khối MTEXT trên CAD -> Hút toàn bộ cụm F-NAME lên bảng.
        /// </summary>
        public static void PickExistingFnameInteractive(FnameTabViewModel vm)
        {
            CadDocument doc = CadCompat.ActiveDocument;
            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            var peo = new PromptEntityOptions("\n[OHTA Fname] Chọn đối tượng F-NAME trên bản vẽ (TEXT hoặc MTEXT): ");
            peo.SetRejectMessage("\nVui lòng chọn đúng đối tượng chữ TEXT hoặc MTEXT.");
            peo.AddAllowedClass(typeof(DBText), exactMatch: false);
            peo.AddAllowedClass(typeof(MText), exactMatch: false);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                vm.SetFeedback("Đã hủy chọn Fname.", isWarning: false);
                return;
            }

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    PickedFnameTarget target = ExtractClusterFromPickedEntity(doc, tr, per.ObjectId, out FnameRecord parsed, out double detectedHeight);
                    tr.Commit();

                    if (target == null || parsed == null)
                    {
                        vm.SetFeedback("Đối tượng vừa chọn không chứa dữ liệu F-NAME.", isWarning: true);
                        return;
                    }

                    try { ed.SetImpliedSelection(target.EntityIds.ToArray()); } catch { }
                    vm.LoadFromPickedRecord(parsed, target, detectedHeight);
                    CadCompat.WriteMessage($"\n[OHTA Fname] Đã hút F-NAME: N={parsed.N} | M={parsed.M} | T={parsed.T} | Q={parsed.Q} | Z={parsed.Z}\n");
                }
            }
            catch (System.Exception ex)
            {
                vm.SetFeedback("Lỗi hút Fname: " + ex.Message, isWarning: true);
                CadCompat.WriteMessage($"\n[OHTA Fname] Lỗi hút Fname: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Chọn một F-NAME trên bản vẽ và ghi đè ngay lập tức bằng 5 giá trị hiện tại trên Tab Fname.
        /// </summary>
        public static void PickAndOverwriteFnameInteractive(FnameTabViewModel vm)
        {
            CadDocument doc = CadCompat.ActiveDocument;
            if (doc == null)
                return;

            if (!vm.ValidateReadyForExport(out _, showPromptIfMissingInCatalog: true))
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            var peo = new PromptEntityOptions("\n[OHTA Fname] Chọn F-NAME trên bản vẽ cần ghi đè cập nhật: ");
            peo.SetRejectMessage("\nVui lòng chọn đối tượng chữ TEXT hoặc MTEXT.");
            peo.AddAllowedClass(typeof(DBText), exactMatch: false);
            peo.AddAllowedClass(typeof(MText), exactMatch: false);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
                return;

            try
            {
                PickedFnameTarget target;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    target = ExtractClusterFromPickedEntity(doc, tr, per.ObjectId, out _, out _);
                    tr.Commit();
                }

                if (target == null)
                {
                    vm.SetFeedback("Không nhận diện được cụm F-NAME để cập nhật.", isWarning: true);
                    return;
                }

                UpdateTargetInPlace(vm, target, lockDoc: false);
            }
            catch (System.Exception ex)
            {
                vm.SetFeedback("Lỗi cập nhật Fname: " + ex.Message, isWarning: true);
            }
        }

        /// <summary>
        /// Ghi đè trực tiếp xuống cụm F-NAME đã liên kết (có thể gọi thẳng từ nút WPF nhờ LockDocument).
        /// </summary>
        public static bool UpdateTargetInPlace(FnameTabViewModel vm, PickedFnameTarget target, bool lockDoc)
        {
            CadDocument doc = CadCompat.ActiveDocument;
            if (doc == null || target == null || !target.IsValidFor(doc))
                return false;

            IDisposable docLock = null;
            try
            {
                if (lockDoc)
                    docLock = doc.LockDocument();

                Database db = doc.Database;
                Editor ed = doc.Editor;

                FnameRecord record = vm.BuildCurrentRecord(includeBoundExtras: false);
                record.ExtraBeforeZ = target.ExtraBeforeZ != null ? new List<string>(target.ExtraBeforeZ) : new List<string>();
                record.ExtraAfterZ = target.ExtraAfterZ != null ? new List<string>(target.ExtraAfterZ) : new List<string>();
                List<string> updatedLines = record.BuildLines(includePreservedExtras: true);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    EnsureFnameLayer(db, tr);

                    if (target.IsMText)
                    {
                        var mt = tr.GetObject(target.EntityIds[0], OpenMode.ForWrite) as MText;
                        if (mt == null)
                            return false;

                        mt.Layer = FnameLayerName;
                        mt.Contents = string.Join("\\P", updatedLines);
                    }
                    else
                    {
                        var existingTexts = new List<DBText>();
                        foreach (ObjectId id in target.EntityIds)
                        {
                            if (tr.GetObject(id, OpenMode.ForWrite) is DBText dt)
                                existingTexts.Add(dt);
                        }

                        if (existingTexts.Count == 0)
                            return false;

                        existingTexts = existingTexts.OrderByDescending(t => t.Position.Y).ToList();
                        DBText anchor = existingTexts[0];
                        Point3d topPos = anchor.Position;
                        double height = anchor.Height > 0 ? anchor.Height : vm.LastDraggedHeight;
                        double lineStep = existingTexts.Count >= 2
                            ? Math.Max(height * 1.1, Math.Abs(existingTexts[0].Position.Y - existingTexts[1].Position.Y))
                            : height * 5.0 / 3.0;

                        BlockTableRecord ownerSpace = (BlockTableRecord)tr.GetObject(anchor.OwnerId, OpenMode.ForWrite);
                        var newIds = new List<ObjectId>();

                        for (int i = 0; i < updatedLines.Count; i++)
                        {
                            if (i < existingTexts.Count)
                            {
                                DBText dt = existingTexts[i];
                                dt.Layer = FnameLayerName;
                                dt.TextString = updatedLines[i];
                                newIds.Add(dt.ObjectId);
                            }
                            else
                            {
                                var extraDt = new DBText();
                                extraDt.SetDatabaseDefaults(db);
                                extraDt.Layer = FnameLayerName;
                                extraDt.TextStyleId = anchor.TextStyleId;
                                extraDt.Height = height;
                                extraDt.Position = new Point3d(topPos.X, topPos.Y - i * lineStep, topPos.Z);
                                extraDt.TextString = updatedLines[i];
                                ObjectId addedId = ownerSpace.AppendEntity(extraDt);
                                tr.AddNewlyCreatedDBObject(extraDt, true);
                                newIds.Add(addedId);
                            }
                        }

                        for (int j = updatedLines.Count; j < existingTexts.Count; j++)
                        {
                            existingTexts[j].Erase();
                        }

                        target.EntityIds = newIds;
                    }

                    tr.Commit();
                }

                ed.UpdateScreen();
                vm.NotifyUpdatedSuccessfully(target);
                CadCompat.WriteMessage($"\n[OHTA Fname] Đã cập nhật F-NAME thành công (N={record.N}, M={record.M}, T={record.T}, Q={record.Q}, Z={record.Z}).\n");
                return true;
            }
            catch (System.Exception ex)
            {
                vm.SetFeedback("Lỗi cập nhật Fname: " + ex.Message, isWarning: true);
                CadCompat.WriteMessage($"\n[OHTA Fname] Lỗi cập nhật Fname: {ex.Message}\n");
                return false;
            }
            finally
            {
                docLock?.Dispose();
            }
        }

        private static PickedFnameTarget ExtractClusterFromPickedEntity(
            CadDocument doc,
            Transaction tr,
            ObjectId pickedId,
            out FnameRecord parsedRecord,
            out double detectedHeight)
        {
            parsedRecord = null;
            detectedHeight = 20.0;

            Entity ent = tr.GetObject(pickedId, OpenMode.ForRead) as Entity;
            if (ent == null)
                return null;

            if (ent is MText mt)
            {
                detectedHeight = mt.TextHeight > 0 ? mt.TextHeight : 20.0;
                parsedRecord = FnameParser.Parse(new[] { mt.Contents });
                return new PickedFnameTarget
                {
                    Document = doc,
                    IsMText = true,
                    EntityIds = new List<ObjectId> { mt.ObjectId },
                    ExtraBeforeZ = new List<string>(parsedRecord.ExtraBeforeZ),
                    ExtraAfterZ = new List<string>(parsedRecord.ExtraAfterZ)
                };
            }

            if (ent is DBText clickedText)
            {
                double h = clickedText.Height > 0 ? clickedText.Height : 20.0;
                detectedHeight = h;
                Point3d basePos = clickedText.Position;
                string targetLayer = clickedText.Layer;

                var candidates = new List<DBText>();
                BlockTableRecord space = tr.GetObject(clickedText.OwnerId, OpenMode.ForRead) as BlockTableRecord;
                if (space != null)
                {
                    foreach (ObjectId objId in space)
                    {
                        if (!(tr.GetObject(objId, OpenMode.ForRead) is DBText dt))
                            continue;

                        if (!string.Equals(dt.Layer, targetLayer, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (Math.Abs(dt.Position.X - basePos.X) > h * 3.0)
                            continue;

                        if (Math.Abs(dt.Position.Y - basePos.Y) > h * 15.0)
                            continue;

                        if (FnameParser.IsFnameComponentLine(dt.TextString))
                            candidates.Add(dt);
                    }
                }

                if (!candidates.Any(c => c.ObjectId == clickedText.ObjectId))
                    candidates.Add(clickedText);

                List<DBText> ordered = candidates.OrderByDescending(c => c.Position.Y).ToList();
                int clickedIdx = ordered.FindIndex(c => c.ObjectId == clickedText.ObjectId);
                double maxGapY = h * 3.6;

                int start = clickedIdx;
                while (start > 0 && Math.Abs(ordered[start - 1].Position.Y - ordered[start].Position.Y) <= maxGapY)
                    start--;

                int end = clickedIdx;
                while (end < ordered.Count - 1 && Math.Abs(ordered[end].Position.Y - ordered[end + 1].Position.Y) <= maxGapY)
                    end++;

                List<DBText> cluster = ordered.GetRange(start, end - start + 1);
                parsedRecord = FnameParser.Parse(cluster.Select(c => c.TextString));

                return new PickedFnameTarget
                {
                    Document = doc,
                    IsMText = false,
                    EntityIds = cluster.Select(c => c.ObjectId).ToList(),
                    ExtraBeforeZ = new List<string>(parsedRecord.ExtraBeforeZ),
                    ExtraAfterZ = new List<string>(parsedRecord.ExtraAfterZ)
                };
            }

            return null;
        }

        private static void EnsureFnameLayer(Database db, Transaction tr)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(FnameLayerName))
            {
                lt.UpgradeOpen();
                var ltr = new LayerTableRecord
                {
                    Name = FnameLayerName,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, 3)
                };
                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }
        }

        private static ObjectId EnsurePreferredTextStyle(Database db, Transaction tr)
        {
            TextStyleTable tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (tst.Has(PreferredStyleName))
                return tst[PreferredStyleName];

            try
            {
                tst.UpgradeOpen();
                var tstr = new TextStyleTableRecord
                {
                    Name = PreferredStyleName,
                    FileName = "txt.shx"
                };
                ObjectId id = tst.Add(tstr);
                tr.AddNewlyCreatedDBObject(tstr, true);
                return id;
            }
            catch
            {
                return db.Textstyle;
            }
        }
    }
}
