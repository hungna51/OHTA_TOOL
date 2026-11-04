using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Services;
using OhtaTool.Plugin.Views;

namespace OhtaTool.Plugin.ViewModels
{
    /// <summary>
    /// ViewModel chuyên biệt cho Tab 1 — Fname (chọn N=, M=, T=, Q=, Z= và thực thi các lệnh Fname).
    /// </summary>
    public sealed class FnameTabViewModel : INotifyPropertyChanged
    {
        private const string RegSubKey = @"Software\OFT_Lisp\OhtaTool\Fname";

        // N= nhớ riêng theo từng bản vẽ trong phiên CAD (static: sống qua OHTARELOAD tạo lại ViewModel)
        private static readonly Dictionary<string, string> NByDrawing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<CadDocument> _hookedDocs = new HashSet<CadDocument>();
        private static readonly object _hookedDocsLock = new object();
        private static string _currentDrawing;
        private static bool _docEventHooked;
        private static System.Windows.Threading.Dispatcher _uiDispatcher;

        private readonly ModuleManifest _manifest;
        private readonly Action _reloadTabsCallback;
        private FnameCatalogData _catalog;

        private string _valN = string.Empty;
        private string _valM = string.Empty;
        private string _valT = string.Empty;
        private string _valQ = string.Empty;
        private string _valZ = string.Empty;
        private bool _lockZInput = true;
        private bool _autoIncrementN = true;
        private bool _isMTextMode;
        private double _lastDraggedHeight = 20.0;
        private string _feedbackMessage = "Sẵn sàng đặt mới (kéo thả cỡ chữ) hoặc hút F-NAME từ bản vẽ.";
        private bool _isFeedbackWarning;
        private bool _suppressMaterialCascade;
        private PickedFnameTarget _boundTarget;

        public FnameTabViewModel(ModuleManifest manifest, Action reloadTabsCallback)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _reloadTabsCallback = reloadTabsCallback;

            MaterialOptions = new ObservableCollection<string>();
            ThicknessOptions = new ObservableCollection<string>();
            ZOptions = new ObservableCollection<string>();
            ExtraTools = new ObservableCollection<ToolItem>();
            LoadDataTables();

            if (_manifest.Tools != null)
            {
                foreach (ToolItem t in _manifest.Tools)
                    ExtraTools.Add(t);
            }

            AccentBrush = ParseBrush(_manifest.AccentColor, "#0277BD");

            BendRows = new ObservableCollection<BendRowItemViewModel>();
            PressRows = new ObservableCollection<PressRowItemViewModel>();

            PlaceFnameCommand = new RelayCommand(_ => OnRequestPlaceFname());
            PickFnameCommand = new RelayCommand(_ => OnRequestPickFname());
            UpdateFnameCommand = new RelayCommand(_ => OnRequestUpdateFname());
            IncrementNCommand = new RelayCommand(_ => ValN = FnameParser.IncrementPartName(ValN));
            IncrementQCommand = new RelayCommand(_ => AdjustQuantity(+1));
            DecrementQCommand = new RelayCommand(_ => AdjustQuantity(-1));
            ClearFieldsCommand = new RelayCommand(_ => ResetFormFields());
            ExecuteExtraToolCommand = new RelayCommand(OnExecuteExtraTool);
            OpenSettingsCommand = new RelayCommand(_ => OnOpenSettingsDialog());
            AddBendRowCommand = new RelayCommand(_ => AddBendRow());
            AddPressRowCommand = new RelayCommand(_ => AddPressRow());

            LoadPersistedPreferences();

            FnameCadCommands.ActiveViewModel = this;
            _uiDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            HookCadDocumentEvents();

            _currentDrawing = null; // ViewModel mới (OHTARELOAD) -> nạp lại N= của bản vẽ hiện hành
            SyncActiveDrawing();

            // Số đỏ chờ duyệt: lấy theo lần kiểm tra gần nhất (registry), không gọi mạng
            PendingRequests = new ObservableCollection<FnameUpdateRequest>();
            _isApprovalAdmin = FnameSheetWriter.LoadIsAdmin();
            _pendingCount = FnameSheetWriter.LoadPendingCount();

            if (FnameDataSync.ShouldAutoSync(_manifest.DirectoryPath))
            {
                _ = UpdateDataFromSheetAsync();
                if (FnameSheetWriter.IsConfigured(_manifest.DirectoryPath))
                    _ = RefreshPendingAsync();
            }
        }

        // ---------- Hàng chờ phê duyệt (tab "update" trên Google Sheet) ----------

        private bool _isApprovalAdmin;
        private int _pendingCount;

        /// <summary>Yêu cầu chờ duyệt (chỉ có dữ liệu trên máy admin, sau khi bấm Tải lại / lần mở đầu ngày).</summary>
        public ObservableCollection<FnameUpdateRequest> PendingRequests { get; }

        public bool IsApprovalAdmin
        {
            get => _isApprovalAdmin;
            private set
            {
                if (_isApprovalAdmin == value) return;
                _isApprovalAdmin = value;
                OnPropertyChanged(nameof(IsApprovalAdmin));
                OnPropertyChanged(nameof(ApprovalTabVisibility));
                OnPropertyChanged(nameof(PendingBadgeVisibility));
            }
        }

        public int PendingCount
        {
            get => _pendingCount;
            private set
            {
                value = Math.Max(0, value);
                if (_pendingCount == value) return;
                _pendingCount = value;
                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(PendingBadgeText));
                OnPropertyChanged(nameof(PendingBadgeVisibility));
            }
        }

        public string PendingBadgeText => _pendingCount > 9 ? "9+" : _pendingCount.ToString(CultureInfo.InvariantCulture);

        public Visibility PendingBadgeVisibility => _isApprovalAdmin && _pendingCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ApprovalTabVisibility => _isApprovalAdmin ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>Tải danh sách yêu cầu chờ duyệt ở luồng nền, cập nhật số đỏ. Trả về dòng trạng thái.</summary>
        public async System.Threading.Tasks.Task<string> RefreshPendingAsync()
        {
            var ui = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var r = await System.Threading.Tasks.Task.Run(() => FnameSheetWriter.ListRequests(_manifest.DirectoryPath)).ConfigureAwait(false);
            ui.Invoke(() =>
            {
                if (!r.ok)
                    return;
                IsApprovalAdmin = r.isAdmin;
                PendingRequests.Clear();
                foreach (FnameUpdateRequest req in r.requests)
                    PendingRequests.Add(req);
                PendingCount = PendingRequests.Count;
            });
            return r.message;
        }

        /// <summary>Duyệt hoặc từ chối 1 yêu cầu; thành công thì bỏ khỏi danh sách, giảm số đỏ, duyệt thì nạp lại dữ liệu.</summary>
        public async System.Threading.Tasks.Task<(bool ok, string message)> ResolveRequestAsync(FnameUpdateRequest request, bool approve)
        {
            var ui = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var r = await System.Threading.Tasks.Task.Run(() => FnameSheetWriter.Resolve(_manifest.DirectoryPath, request, approve)).ConfigureAwait(false);
            ui.Invoke(() =>
            {
                // "Không tìm thấy" = máy admin khác đã xử lý -> cũng bỏ khỏi danh sách
                if (r.ok || (r.message ?? string.Empty).StartsWith("Không tìm thấy yêu cầu", StringComparison.Ordinal))
                {
                    PendingRequests.Remove(request);
                    PendingCount = PendingRequests.Count;
                    FnameSheetWriter.SaveState(_isApprovalAdmin, PendingCount);
                }
                if (r.ok && approve)
                    ReloadDataKeepingInputs();
                SetFeedback(r.message, isWarning: !r.ok);
            });
            return r;
        }

        private void ReloadDataKeepingInputs()
        {
            string keepM = _valM;
            string keepT = _valT;
            string keepZ = _valZ;
            SuppressInputSync = true;
            _suppressMaterialCascade = true;
            try
            {
                LoadDataTables();
                _valM = keepM;
                PopulateThicknessOptionsFor(_valM);
                _valT = keepT;
                FnameMaterialSpec spec = _catalog?.FindMaterial(_valM);
                if (spec != null && (_lockZInput || string.IsNullOrWhiteSpace(keepZ)))
                    _valZ = spec.Z ?? keepZ;
                else
                    _valZ = keepZ;
            }
            finally
            {
                _suppressMaterialCascade = false;
                SuppressInputSync = false;
            }
            UpdateRealThickness();
            RefreshAllBendRows();
            OnPropertyChanged(nameof(ValM));
            OnPropertyChanged(nameof(ValT));
            OnPropertyChanged(nameof(ValZ));
            OnPropertyChanged(nameof(PreviewText));
        }

        /// <summary>
        /// Đọc catalog + 3 bảng TraV từ CSV (cache Google Sheet hoặc 01_Fname/data/) và đổ lại danh sách chọn.
        /// </summary>
        private void LoadDataTables()
        {
            TraVCalculator.Load(_manifest.DirectoryPath);
            _catalog = FnameCatalogData.LoadFromModuleDirectory(_manifest.DirectoryPath);
            TraVCalculator.SetCatalog(_catalog);

            MaterialOptions.Clear();
            foreach (FnameMaterialSpec spec in _catalog.Materials)
            {
                if (!string.IsNullOrWhiteSpace(spec.M))
                    MaterialOptions.Add(spec.M);
            }
            ZOptions.Clear();
            foreach (string z in _catalog.GetDistinctZList())
                ZOptions.Add(z);
        }

        /// <summary>
        /// Gửi yêu cầu thêm / cập nhật vật liệu vào hàng chờ (tab "update") ở luồng nền.
        /// Gửi được thì máy này dùng ngay (lưu vào catalog_local.csv); catalog chung trên Sheet đổi khi admin duyệt.
        /// </summary>
        public async System.Threading.Tasks.Task<(bool ok, string message)> SubmitMaterialAsync(string m, string loai, string z, List<string> thicknesses)
        {
            var ui = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var result = await System.Threading.Tasks.Task.Run(() =>
            {
                var r = FnameSheetWriter.SubmitCatalogRequest(_manifest.DirectoryPath, m, loai, z, thicknesses);
                if (!r.ok)
                    return r;
                try
                {
                    FnameLocalAdditions.Add(null, m, loai, z, thicknesses);
                    return (true, "Đã bổ sung \"" + m + "\" — dùng được ngay trên máy này, chờ admin duyệt để cập nhật cho mọi máy.");
                }
                catch (Exception ex)
                {
                    return (true, r.message + " (Chưa lưu được trên máy: " + ex.GetBaseException().Message + ")");
                }
            }).ConfigureAwait(false);
            ui.Invoke(() =>
            {
                if (result.Item1)
                    ReloadDataKeepingInputs();
                SetFeedback(result.Item2, isWarning: !result.Item1);
            });
            return result;
        }

        /// <summary>
        /// Tải dữ liệu mới từ Google Sheet ở luồng nền rồi nạp lại bảng trên luồng giao diện (giữ nguyên ô đang nhập).
        /// </summary>
        public async System.Threading.Tasks.Task<string> UpdateDataFromSheetAsync()
        {
            // CAD không chắc có SynchronizationContext -> tự quay về Dispatcher của giao diện
            var ui = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var (ok, message) = await System.Threading.Tasks.Task.Run(() => FnameDataSync.Run(_manifest.DirectoryPath)).ConfigureAwait(false);
            ui.Invoke(() =>
            {
                if (ok)
                {
                    ReloadDataKeepingInputs();
                }
                SetFeedback(message, isWarning: !ok);
            });
            return message;
        }

        /// <summary>
        /// Đồng bộ tên bản vẽ hiện hành từ CAD vào ô N= nếu bản vẽ thay đổi hoặc vừa được lưu / đổi tên.
        /// </summary>
        public void SyncActiveDrawing()
        {
            try
            {
                string activeName = CadCompat.ActiveDocument?.Name;
                if (!string.IsNullOrEmpty(activeName))
                {
                    SwitchDrawing(activeName);
                }
            }
            catch
            {
                // Bỏ qua lỗi truy cập CAD khi CAD đang bận
            }
        }

        /// <summary>
        /// Nạp N= của bản vẽ vừa kích hoạt hoặc vừa được lưu / đổi tên (lần đầu = "tên bản vẽ-1"; ValN tự ghi lại theo bản vẽ khi đổi).
        /// Tự động cập nhật N= khi bản vẽ tạm (Drawing1, Drawing2, ...) được lưu/đổi tên thành tên thật (VD: 47079.dwg -> N=47079-1).
        /// </summary>
        public void SwitchDrawing(string drawingPath)
        {
            if (string.IsNullOrEmpty(drawingPath))
                return;

            string oldDrawing = _currentDrawing;
            bool isSameDrawing = string.Equals(drawingPath, oldDrawing, StringComparison.OrdinalIgnoreCase);

            // TH 1: Cùng tên file bản vẽ nhưng ValN đang là generic (VD Drawing2-1) mà tên bản vẽ đã là tên thật (VD 47079.dwg)
            if (isSameDrawing)
            {
                if (FnameParser.IsGenericPartName(_valN) && !FnameParser.IsGenericDrawingName(drawingPath))
                {
                    ValN = FnameParser.DefaultPartNameFromDrawing(drawingPath);
                }
                return;
            }

            _currentDrawing = drawingPath;

            // Nếu bản vẽ cũ là bản vẽ tạm chưa lưu (Drawing1, Drawing2, ...) -> dọn dẹp khỏi từ điển lưu trữ
            if (!string.IsNullOrEmpty(oldDrawing) && FnameParser.IsGenericDrawingName(oldDrawing))
            {
                NByDrawing.Remove(oldDrawing);
            }

            // Xác định N= cho bản vẽ mới:
            // Nếu bản vẽ mới đã có N= lưu trước đó và không phải tên generic rỗng -> phục hồi
            if (NByDrawing.TryGetValue(drawingPath, out string rememberedN) &&
                !string.IsNullOrWhiteSpace(rememberedN) &&
                (!FnameParser.IsGenericPartName(rememberedN) || FnameParser.IsGenericDrawingName(drawingPath)))
            {
                ValN = rememberedN;
            }
            else
            {
                ValN = FnameParser.DefaultPartNameFromDrawing(drawingPath);
            }
        }

        private static void HookCadDocumentEvents()
        {
            if (_docEventHooked || CadApp.DocumentManager == null)
                return;

            _docEventHooked = true;

            try
            {
                CadApp.DocumentManager.DocumentActivated += (s, e) =>
                {
                    HookSingleDocument(e?.Document);
                    _uiDispatcher?.BeginInvoke(new Action(() =>
                    {
                        FnameCadCommands.ActiveViewModel?.SwitchDrawing(e?.Document?.Name);
                    }));
                };

                CadApp.DocumentManager.DocumentCreated += (s, e) =>
                {
                    HookSingleDocument(e?.Document);
                    _uiDispatcher?.BeginInvoke(new Action(() =>
                    {
                        FnameCadCommands.ActiveViewModel?.SwitchDrawing(e?.Document?.Name);
                    }));
                };

                CadApp.DocumentManager.DocumentToBeDestroyed += (s, e) =>
                {
                    if (e?.Document != null)
                    {
                        lock (_hookedDocsLock)
                        {
                            _hookedDocs.Remove(e.Document);
                        }
                    }
                };

                foreach (CadDocument doc in CadApp.DocumentManager)
                {
                    HookSingleDocument(doc);
                }
            }
            catch
            {
                // Tránh lỗi khi CAD chưa khởi tạo xong DocumentManager
            }
        }

        private static void HookSingleDocument(CadDocument doc)
        {
            if (doc == null)
                return;

            lock (_hookedDocsLock)
            {
                if (_hookedDocs.Contains(doc))
                    return;
                _hookedDocs.Add(doc);
            }

            try
            {
                doc.CommandEnded += (sender, e) =>
                {
                    string cmd = e?.GlobalCommandName?.ToUpperInvariant() ?? string.Empty;
                    if (cmd == "SAVE" || cmd == "SAVEAS" || cmd == "QSAVE" || cmd == "SAVEALL" ||
                        cmd == "WBLOCK" || cmd == "NEW" || cmd == "QNEW" || cmd == "OPEN")
                    {
                        CadDocument d = sender as CadDocument ?? CadCompat.ActiveDocument;
                        string docName = d?.Name ?? CadCompat.ActiveDocument?.Name;
                        _uiDispatcher?.BeginInvoke(new Action(() =>
                        {
                            FnameCadCommands.ActiveViewModel?.SwitchDrawing(docName);
                        }));
                    }
                };
            }
            catch { }

            try
            {
                if (doc.Database != null)
                {
                    doc.Database.SaveComplete += (sender, e) =>
                    {
                        _uiDispatcher?.BeginInvoke(new Action(() =>
                        {
                            FnameCadCommands.ActiveViewModel?.SwitchDrawing(CadCompat.ActiveDocument?.Name);
                        }));
                    };
                }
            }
            catch { }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title => _manifest.DisplayTitle;
        public string FolderName => _manifest.FolderName;
        public SolidColorBrush AccentBrush { get; }

        public ObservableCollection<string> MaterialOptions { get; }
        public ObservableCollection<string> ThicknessOptions { get; }
        public ObservableCollection<string> ZOptions { get; }
        public ObservableCollection<ToolItem> ExtraTools { get; }

        public ObservableCollection<BendRowItemViewModel> BendRows { get; }
        public ObservableCollection<PressRowItemViewModel> PressRows { get; }

        public Visibility ExtraToolsVisibility =>
            ExtraTools.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility BoundTargetBadgeVisibility =>
            _boundTarget != null && _boundTarget.IsValidFor(CadCompat.ActiveDocument)
                ? Visibility.Visible
                : Visibility.Collapsed;

        public SolidColorBrush FeedbackBrush =>
            _isFeedbackWarning ? ParseBrush("#C62828", "#C62828") : ParseBrush("#1B5E20", "#1B5E20");

        public SolidColorBrush FeedbackBgBrush =>
            _isFeedbackWarning ? ParseBrush("#FFEBEE", "#FFEBEE") : ParseBrush("#E8F5E9", "#E8F5E9");

        public string ValN
        {
            get => _valN;
            set
            {
                if (_valN == value) return;
                _valN = value ?? string.Empty;
                if (_currentDrawing != null)
                    NByDrawing[_currentDrawing] = _valN;
                OnPropertyChanged(nameof(ValN));
                OnPropertyChanged(nameof(PreviewText));
            }
        }

        /// <summary>
        /// Tạm ngắt cập nhật ngược từ ComboBox.Text xuống ValM/ValT trong lúc đang làm mới danh sách ứng viên.
        /// </summary>
        public bool SuppressInputSync { get; set; }

        public FnameCatalogData Catalog => _catalog;

        public string ValM
        {
            get => _valM;
            set
            {
                if (SuppressInputSync) return;
                string next = value ?? string.Empty;
                if (_valM == next) return;
                _valM = next;
                OnPropertyChanged(nameof(ValM));
                if (!_suppressMaterialCascade)
                    ApplyMaterialSelection(_valM, preserveMatchingT: true);
                UpdateRealThickness();
                RefreshAllBendRows();
                OnPropertyChanged(nameof(PreviewText));
            }
        }

        public string ValT
        {
            get => _valT;
            set
            {
                if (SuppressInputSync) return;
                if (_valT == value) return;
                _valT = value ?? string.Empty;
                OnPropertyChanged(nameof(ValT));
                UpdateRealThickness();
                RefreshAllBendRows();
                OnPropertyChanged(nameof(PreviewText));
            }
        }

        /// <summary>
        /// Lọc danh sách ứng viên M= theo nội dung đang gõ trong ô M=.
        /// </summary>
        public void FilterMaterialOptions(string query)
        {
            if (_catalog == null)
                return;

            List<string> matches = _catalog.GetMatchingMaterials(query);
            MaterialOptions.Clear();
            foreach (string m in matches)
                MaterialOptions.Add(m);
        }

        /// <summary>
        /// Khôi phục đầy đủ danh sách tất cả vật liệu M= trong catalog (khi bấm nút mũi tên xổ xuống).
        /// </summary>
        public void RestoreAllMaterialOptions()
        {
            FilterMaterialOptions(string.Empty);
        }

        /// <summary>
        /// Lọc danh sách ứng viên T= của vật liệu hiện tại theo nội dung đang gõ trong ô T=.
        /// </summary>
        public void FilterThicknessOptions(string query)
        {
            if (_catalog == null)
                return;

            List<string> matches = _catalog.GetMatchingThicknesses(_valM, query);
            ThicknessOptions.Clear();
            foreach (string t in matches)
                ThicknessOptions.Add(t);
        }

        /// <summary>
        /// Khôi phục đầy đủ danh sách độ dày T= của vật liệu M= hiện tại.
        /// </summary>
        public void RestoreAllThicknessOptions()
        {
            FilterThicknessOptions(string.Empty);
        }

        private string _realThicknessText = string.Empty;
        public string RealThicknessText
        {
            get => _realThicknessText;
            set
            {
                if (_realThicknessText == value) return;
                _realThicknessText = value ?? string.Empty;
                OnPropertyChanged(nameof(RealThicknessText));
            }
        }

        private void UpdateRealThickness()
        {
            RealThicknessText = TraVCalculator.GetRealThickness(_valM, _valT);
        }

        public string ValQ
        {
            get => _valQ;
            set
            {
                if (_valQ == value) return;
                _valQ = value ?? string.Empty;
                OnPropertyChanged(nameof(ValQ));
                OnPropertyChanged(nameof(PreviewText));
            }
        }

        public string ValZ
        {
            get => _valZ;
            set
            {
                if (_valZ == value) return;
                _valZ = value ?? string.Empty;
                OnPropertyChanged(nameof(ValZ));
                OnPropertyChanged(nameof(PreviewText));
            }
        }

        public bool LockZInput
        {
            get => _lockZInput;
            set
            {
                if (_lockZInput == value) return;
                _lockZInput = value;
                OnPropertyChanged(nameof(LockZInput));
                OnPropertyChanged(nameof(IsZInputEnabled));
                if (_lockZInput)
                {
                    SyncZFromMaterial(_valM);
                }
                SavePersistedPreferences();
            }
        }

        public bool IsZInputEnabled => !_lockZInput;

        public bool AutoIncrementN
        {
            get => _autoIncrementN;
            set
            {
                if (_autoIncrementN == value) return;
                _autoIncrementN = value;
                OnPropertyChanged(nameof(AutoIncrementN));
                SavePersistedPreferences();
            }
        }

        public bool IsTextMode
        {
            get => !_isMTextMode;
            set
            {
                IsMTextMode = !value;
            }
        }

        public bool IsMTextMode
        {
            get => _isMTextMode;
            set
            {
                if (_isMTextMode == value) return;
                _isMTextMode = value;
                OnPropertyChanged(nameof(IsMTextMode));
                OnPropertyChanged(nameof(IsTextMode));
                SavePersistedPreferences();
            }
        }

        public ImageSource SettingsLogo => IconLoader.GetSettingsLogo();

        public double LastDraggedHeight => _lastDraggedHeight;

        public string FeedbackMessage => _feedbackMessage;

        public string PreviewText => BuildCurrentRecord(includeBoundExtras: true).BuildPreviewText(includePreservedExtras: true);

        public ICommand PlaceFnameCommand { get; }
        public ICommand PickFnameCommand { get; }
        public ICommand UpdateFnameCommand { get; }
        public ICommand IncrementNCommand { get; }
        public ICommand IncrementQCommand { get; }
        public ICommand DecrementQCommand { get; }
        public ICommand ClearFieldsCommand { get; }
        public ICommand ExecuteExtraToolCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand AddBendRowCommand { get; }
        public ICommand AddPressRowCommand { get; }

        public FnameRecord BuildCurrentRecord(bool includeBoundExtras)
        {
            var rec = new FnameRecord
            {
                N = ValN,
                M = ValM,
                T = FnameCatalogData.FormatThickness(ValT),
                Q = ValQ,
                Z = ValZ
            };

            if (BendRows != null)
            {
                foreach (var b in BendRows)
                {
                    if (!string.IsNullOrWhiteSpace(b.SelectedVText))
                        rec.BendLines.Add(b.SelectedVText.Trim());
                }
            }

            if (PressRows != null)
            {
                foreach (var p in PressRows)
                {
                    if (!string.IsNullOrWhiteSpace(p.FormattedLine))
                        rec.PressLines.Add(p.FormattedLine.Trim());
                }
            }

            if (includeBoundExtras && _boundTarget != null && _boundTarget.IsValidFor(CadCompat.ActiveDocument))
            {
                if (_boundTarget.ExtraBeforeZ != null)
                    rec.ExtraBeforeZ.AddRange(_boundTarget.ExtraBeforeZ);
                if (_boundTarget.ExtraAfterZ != null)
                    rec.ExtraAfterZ.AddRange(_boundTarget.ExtraAfterZ);
            }

            return rec;
        }

        public void LoadFromPickedRecord(FnameRecord parsed, PickedFnameTarget target, double detectedHeight)
        {
            if (parsed == null)
                return;

            _boundTarget = target;
            _suppressMaterialCascade = true;
            try
            {
                ValM = parsed.M ?? string.Empty;
                PopulateThicknessOptionsFor(_valM);
                ValN = parsed.N ?? string.Empty;
                ValT = parsed.T ?? string.Empty;
                ValQ = parsed.Q ?? string.Empty;
                ValZ = parsed.Z ?? string.Empty;

                BendRows.Clear();
                if (parsed.BendLines != null)
                {
                    foreach (string b in parsed.BendLines)
                    {
                        var row = new BendRowItemViewModel(() => (ValM, ValT), () => OnPropertyChanged(nameof(PreviewText)), RemoveBendRow);
                        row.SelectedVText = b;
                        BendRows.Add(row);
                    }
                }

                PressRows.Clear();
                if (parsed.PressLines != null)
                {
                    foreach (string p in parsed.PressLines)
                    {
                        var row = new PressRowItemViewModel(() => OnPropertyChanged(nameof(PreviewText)), RemovePressRow);
                        if (TraVCalculator.TryParsePressOneTimeLine(p, out string r, out string a))
                        {
                            row.InnerRText = r;
                            row.AngleText = a;
                        }
                        PressRows.Add(row);
                    }
                }

                if (target != null)
                    IsMTextMode = target.IsMText;

                if (detectedHeight > 0.1)
                    _lastDraggedHeight = detectedHeight;
            }
            finally
            {
                _suppressMaterialCascade = false;
                UpdateRealThickness();
            }

            OnPropertyChanged(nameof(BoundTargetBadgeVisibility));
            OnPropertyChanged(nameof(PreviewText));
            SetFeedback("Đã hút Fname lên bảng. Sửa giá trị rồi bấm [Cập nhật Fname] để lưu.", isWarning: false);
        }

        public void NotifyPlacedSuccessfully(PickedFnameTarget createdTarget, double placedHeight)
        {
            _boundTarget = createdTarget;
            if (placedHeight > 0.1)
                _lastDraggedHeight = placedHeight;

            SavePersistedPreferences();

            if (_autoIncrementN && !string.IsNullOrWhiteSpace(ValN))
            {
                ValN = FnameParser.IncrementPartName(ValN);
                SavePersistedPreferences();
            }

            OnPropertyChanged(nameof(BoundTargetBadgeVisibility));
            OnPropertyChanged(nameof(PreviewText));
            SetFeedback($"Đã đặt F-NAME (cao chữ {placedHeight:0.#}) lên layer 2-F-NAME.", isWarning: false);
        }

        public void NotifyUpdatedSuccessfully(PickedFnameTarget updatedTarget)
        {
            _boundTarget = updatedTarget;
            SavePersistedPreferences();
            OnPropertyChanged(nameof(BoundTargetBadgeVisibility));
            OnPropertyChanged(nameof(PreviewText));
            SetFeedback("Đã cập nhật F-NAME trên bản vẽ thành công!", isWarning: false);
        }

        public void SetFeedback(string message, bool isWarning)
        {
            _feedbackMessage = message ?? string.Empty;
            _isFeedbackWarning = isWarning;
            OnPropertyChanged(nameof(FeedbackMessage));
            OnPropertyChanged(nameof(FeedbackBrush));
            OnPropertyChanged(nameof(FeedbackBgBrush));
        }

        /// <summary>
        /// Kiểm tra thông số Fname trước khi Đặt mới hoặc Cập nhật:
        /// 1. Đủ 5 trường N, M, T, Q, Z; T &gt; 0, Q nguyên dương.
        /// 2. M=, T=, Z= bắt buộc phải có trong dữ liệu chuẩn (catalog). Nếu chưa có sẽ chặn xuất và gợi ý vào Cài đặt để thêm.
        /// </summary>
        public bool ValidateReadyForExport(out FnameRecord record, bool showPromptIfMissingInCatalog = true)
        {
            FnameMaterialSpec spec = _catalog?.FindMaterial(_valM);
            if (spec != null)
            {
                if (!string.Equals(_valM, spec.M, StringComparison.Ordinal))
                {
                    _suppressMaterialCascade = true;
                    try { ValM = spec.M; }
                    finally { _suppressMaterialCascade = false; }
                }

                string canonicalT = FnameCatalogData.FindCanonicalThickness(spec.Thicknesses, _valT);
                if (canonicalT != null && !string.Equals(_valT, canonicalT, StringComparison.Ordinal))
                {
                    ValT = canonicalT;
                }
            }

            // T= luôn có 1 số sau dấu chấm (6 -> 6.0)
            string formattedT = FnameCatalogData.FormatThickness(_valT);
            if (!string.Equals(_valT, formattedT, StringComparison.Ordinal))
                ValT = formattedT;

            record = BuildCurrentRecord(includeBoundExtras: false);
            if (!record.ValidateAgainstCatalog(_catalog, out string warn, out bool isMissingInCatalog))
            {
                SetFeedback(warn, isWarning: true);
                if (isMissingInCatalog && showPromptIfMissingInCatalog)
                {
                    var res = MessageBox.Show(
                        warn + "\n\nBạn có muốn mở cửa sổ Thêm / sửa vật liệu & độ dày trong Cài đặt ngay bây giờ không?",
                        "Thông số chưa có trong dữ liệu Fname",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (res == MessageBoxResult.Yes)
                    {
                        OpenMaterialEditorDirectly();
                    }
                }
                return false;
            }

            return true;
        }

        public void OpenMaterialEditorDirectly(Window owner = null)
        {
            var editor = new FnameMaterialWindow(() => _catalog, TraVCalculator.GetMaterialTypeOptions(), _valM, _valT, SubmitMaterialAsync);
            if (owner != null)
            {
                editor.Owner = owner;
                editor.ShowDialog();
            }
            else
            {
                CadCompat.ShowModalDialog(editor);
            }
        }

        private void OnRequestPlaceFname()
        {
            FnameCadCommands.ActiveViewModel = this;
            SyncActiveDrawing();
            if (!ValidateReadyForExport(out _, showPromptIfMissingInCatalog: true) || !ConfirmAndSubmitInterpolatedBends())
                return;

            SetFeedback("Click 1: chọn điểm đặt -> Di chuột kéo cỡ chữ -> Click 2: chốt F-NAME...", isWarning: false);
            CadCompat.SendCommand("\x03\x03OHTA_FN_PLACE\n");
        }

        /// <summary>
        /// Dòng bẻ dùng góc nội suy vừa tính: khoảng 2 góc &gt; 10° thì hỏi đã xác nhận với quản lý chưa (Không = huỷ xuất Fname);
        /// sau đó gửi từng góc nội suy lên Sheet chờ admin duyệt (chạy nền, không chặn việc đặt Fname).
        /// </summary>
        private bool ConfirmAndSubmitInterpolatedBends()
        {
            var interp = new List<TraVCandidate>();
            foreach (BendRowItemViewModel row in BendRows)
            {
                TraVCandidate c = row.SelectedCandidate;
                if (c?.NewTrialRecord != null && !interp.Any(x => x.NewTrialRecord.SameKey(c.NewTrialRecord)))
                    interp.Add(c);
            }
            if (interp.Count == 0)
                return true;

            var risky = interp.Where(c => c.NeedsManagerConfirm).ToList();
            if (risky.Count > 0)
            {
                string list = string.Join("\n", risky.Select(c => "• " + c.FormattedText + "\n   " + c.Source + " " + c.SourceInfo + "\n   " + c.Warning.TrimStart('⚠', ' ')));
                var res = MessageBox.Show(
                    "Các góc sau tính từ góc bẻ thử nhưng độ tin cậy thấp:\n\n"
                    + list + "\n\nĐã xác nhận với quản lý chưa?\n(Yes = tiếp tục xuất Fname, No = huỷ)",
                    "Cần xác nhận với quản lý", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                {
                    SetFeedback("Đã huỷ: góc nội suy chưa được quản lý xác nhận.", isWarning: true);
                    return false;
                }
            }

            foreach (TraVCandidate c in interp)
            {
                string note = c.Source + " " + c.SourceInfo
                    + (c.NeedsManagerConfirm ? "; " + c.Warning.TrimStart('⚠', ' ').Replace("cần xác nhận", "người dùng đã xác nhận") : string.Empty);
                _ = SubmitTrialAsync(c.NewTrialRecord, note);
            }
            return true;
        }

        /// <summary>
        /// Gửi 1 góc bẻ thử / nội suy vào hàng chờ duyệt ở luồng nền. Gửi được thì máy này dùng ngay (trav_thu_local.csv).
        /// </summary>
        public async System.Threading.Tasks.Task<(bool ok, string message)> SubmitTrialAsync(TraVThuRecord record, string ghiChu)
        {
            var ui = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var result = await System.Threading.Tasks.Task.Run(() =>
            {
                var r = FnameSheetWriter.SubmitTrialRequest(_manifest.DirectoryPath, record, ghiChu);
                if (!r.ok)
                    return (false, "Chưa gửi được " + record.DinhDanh.ToLowerInvariant() + " lên Google Sheet: " + r.message);
                try
                {
                    TraVThuLocalAdditions.Add(null, record);
                    return (true, $"Đã gửi {record.DinhDanh.ToLowerInvariant()} {record.Loai} T={record.T} {TraVCalculator.FormatAngle(record.Goc)}° {record.Dao} "
                        + "— dùng được ngay trên máy này, chờ admin duyệt.");
                }
                catch (Exception ex)
                {
                    return (true, r.message + " (Chưa lưu được trên máy: " + ex.GetBaseException().Message + ")");
                }
            }).ConfigureAwait(false);
            ui.Invoke(() =>
            {
                if (result.Item1)
                    ReloadDataKeepingInputs();
                SetFeedback(result.Item2, isWarning: !result.Item1);
            });
            return result;
        }

        private void OnRequestPickFname()
        {
            FnameCadCommands.ActiveViewModel = this;
            SyncActiveDrawing();
            SetFeedback("Hãy chọn đối tượng F-NAME (TEXT hoặc MTEXT) ngoài bản vẽ...", isWarning: false);
            CadCompat.SendCommand("\x03\x03OHTA_FN_PICK\n");
        }

        private void OnRequestUpdateFname()
        {
            FnameCadCommands.ActiveViewModel = this;
            SyncActiveDrawing();
            if (!ValidateReadyForExport(out _, showPromptIfMissingInCatalog: true) || !ConfirmAndSubmitInterpolatedBends())
                return;

            if (_boundTarget != null && _boundTarget.IsValidFor(CadCompat.ActiveDocument))
            {
                CadFnameService.UpdateTargetInPlace(this, _boundTarget, lockDoc: true);
                return;
            }

            SetFeedback("Chọn F-NAME trên bản vẽ cần ghi đè cập nhật...", isWarning: false);
            CadCompat.SendCommand("\x03\x03OHTA_FN_UPDATE\n");
        }

        private void ApplyMaterialSelection(string selectedM, bool preserveMatchingT)
        {
            string oldT = _valT;
            PopulateThicknessOptionsFor(selectedM);

            FnameMaterialSpec spec = _catalog.FindMaterial(selectedM);
            if (spec != null)
            {
                if (_lockZInput || !string.IsNullOrWhiteSpace(spec.Z))
                    ValZ = spec.Z ?? string.Empty;

                string matchedT = FnameCatalogData.FindCanonicalThickness(spec.Thicknesses, oldT);
                if (!preserveMatchingT || matchedT == null)
                    ValT = string.Empty;
                else if (!string.Equals(oldT, matchedT, StringComparison.Ordinal))
                    ValT = matchedT;
            }
            else if (_lockZInput)
            {
                ValZ = string.Empty;
            }
        }

        private void SyncZFromMaterial(string mat)
        {
            if (string.IsNullOrWhiteSpace(mat)) return;
            FnameMaterialSpec spec = _catalog.FindMaterial(mat);
            if (spec != null && !string.IsNullOrEmpty(spec.Z))
            {
                ValZ = spec.Z;
            }
        }

        private void PopulateThicknessOptionsFor(string selectedM)
        {
            ThicknessOptions.Clear();
            FnameMaterialSpec spec = _catalog.FindMaterial(selectedM);
            if (spec?.Thicknesses != null)
            {
                foreach (string t in spec.Thicknesses)
                    ThicknessOptions.Add(t);
            }
        }

        private void AdjustQuantity(int delta)
        {
            int cur = 0;
            int.TryParse((ValQ ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out cur);
            int next = Math.Max(1, cur + delta);
            ValQ = next.ToString(CultureInfo.InvariantCulture);
        }

        private void ResetFormFields()
        {
            _boundTarget = null;
            ValQ = string.Empty;
            ValT = string.Empty;
            BendRows.Clear();
            PressRows.Clear();
            OnPropertyChanged(nameof(BoundTargetBadgeVisibility));
            OnPropertyChanged(nameof(PreviewText));
            SetFeedback("Đã xóa trắng T=, Q=, các dòng V/Nhấn và hủy liên kết đối tượng cũ.", isWarning: false);
        }

        private void RefreshAllBendRows()
        {
            if (BendRows != null)
            {
                foreach (var r in BendRows)
                    r.RefreshVOptions();
            }
        }

        private void AddBendRow()
        {
            var row = new BendRowItemViewModel(() => (ValM, ValT), () => OnPropertyChanged(nameof(PreviewText)), RemoveBendRow);
            BendRows.Add(row);
            OnPropertyChanged(nameof(PreviewText));
        }

        private void RemoveBendRow(BendRowItemViewModel row)
        {
            if (row != null && BendRows.Contains(row))
            {
                BendRows.Remove(row);
                OnPropertyChanged(nameof(PreviewText));
            }
        }

        private void AddPressRow()
        {
            var row = new PressRowItemViewModel(() => OnPropertyChanged(nameof(PreviewText)), RemovePressRow);
            PressRows.Add(row);
            OnPropertyChanged(nameof(PreviewText));
        }

        private void RemovePressRow(PressRowItemViewModel row)
        {
            if (row != null && PressRows.Contains(row))
            {
                PressRows.Remove(row);
                OnPropertyChanged(nameof(PreviewText));
            }
        }

        private void OnOpenSettingsDialog()
        {
            var dlg = new FnameSettingsWindow(_lockZInput, _autoIncrementN, Path.Combine(_manifest.DirectoryPath, "data"), UpdateDataFromSheetAsync);
            dlg.SetDataInfo(FnameSheetWriter.IsConfigured(_manifest.DirectoryPath), FnameDataStore.DefaultCacheDirectory, FnameDataSync.LastSyncDate());
            dlg.AttachApprovals(this);
            dlg.OpenMaterialEditor = owner =>
            {
                var editor = new FnameMaterialWindow(() => _catalog, TraVCalculator.GetMaterialTypeOptions(), _valM, _valT, SubmitMaterialAsync)
                {
                    Owner = owner
                };
                editor.ShowDialog();
            };
            Action<Window> openTrialEditor = owner =>
            {
                var editor = new FnameTrialBendWindow(_valM, _valT, BendRows.LastOrDefault()?.AngleText, SubmitTrialAsync)
                {
                    Owner = owner
                };
                editor.ShowDialog();
            };
            dlg.OpenTrialEditor = openTrialEditor;
            dlg.OpenTrialList = owner =>
            {
                // Lọc sẵn theo loại vật liệu + T đang nhập trên bảng
                string loai = string.IsNullOrWhiteSpace(_valM) ? string.Empty : TraVCalculator.GetTraVMaterialKeys(_valM).FirstOrDefault()?.Split(' ')[0] ?? string.Empty;
                var list = new FnameTrialListWindow((loai + " " + (_valT ?? string.Empty)).Trim(), openTrialEditor)
                {
                    Owner = owner
                };
                list.ShowDialog();
            };
            if (CadCompat.ShowModalDialog(dlg) == true)
            {
                LockZInput = dlg.LockZInput;
                AutoIncrementN = dlg.AutoIncrementN;
                SavePersistedPreferences();
                SetFeedback($"Đã lưu cài đặt Fname (Khóa Z=: {(LockZInput ? "Bật" : "Tắt")}).", isWarning: false);
            }
        }

        private void OnExecuteExtraTool(object parameter)
        {
            if (!(parameter is ToolItem tool))
                return;

            string macro = MacroBuilder.BuildToolMacro(_manifest.DirectoryPath, tool);
            if (!string.IsNullOrWhiteSpace(macro))
                CadCompat.SendCommand(macro);
        }

        private void LoadPersistedPreferences()
        {
            string savedM = MaterialOptions.Count > 0 ? MaterialOptions[0] : "SS400";
            string savedMode = "TEXT";
            string savedHeight = "20";
            bool savedAutoInc = true;
            bool savedLockZ = true;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegSubKey, writable: false))
                {
                    if (key != null)
                    {
                        savedM = (key.GetValue("LastM") as string) ?? savedM;
                        savedMode = (key.GetValue("OutMode") as string) ?? "TEXT";
                        savedHeight = (key.GetValue("TextHeight") as string) ?? "20";
                        string autoIncStr = key.GetValue("AutoIncN") as string;
                        if (!string.IsNullOrEmpty(autoIncStr))
                            savedAutoInc = autoIncStr == "1";
                        string lockZStr = key.GetValue("LockZ") as string;
                        if (!string.IsNullOrEmpty(lockZStr))
                            savedLockZ = lockZStr == "1";
                    }
                }
            }
            catch
            {
            }

            _suppressMaterialCascade = true;
            try
            {
                _valM = savedM;
                PopulateThicknessOptionsFor(_valM);
                FnameMaterialSpec spec = _catalog.FindMaterial(_valM);
                _valZ = spec?.Z ?? "SS400";
                _valT = string.Empty;
                _valQ = string.Empty;
                _isMTextMode = string.Equals(savedMode, "MTEXT", StringComparison.OrdinalIgnoreCase);
                if (double.TryParse(savedHeight, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedH) && parsedH > 0.1)
                    _lastDraggedHeight = parsedH;
                _autoIncrementN = savedAutoInc;
                _lockZInput = savedLockZ;
            }
            finally
            {
                _suppressMaterialCascade = false;
                UpdateRealThickness();
            }
        }

        private void SavePersistedPreferences()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegSubKey))
                {
                    if (key != null)
                    {
                        key.SetValue("LastM", _valM ?? string.Empty);
                        key.SetValue("OutMode", _isMTextMode ? "MTEXT" : "TEXT");
                        key.SetValue("TextHeight", _lastDraggedHeight.ToString("0.##", CultureInfo.InvariantCulture));
                        key.SetValue("AutoIncN", _autoIncrementN ? "1" : "0");
                        key.SetValue("LockZ", _lockZInput ? "1" : "0");
                    }
                }
            }
            catch
            {
            }
        }

        private static void OpenPathSafe(string targetPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(targetPath) || (!File.Exists(targetPath) && !Directory.Exists(targetPath)))
                    return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = targetPath,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        private static SolidColorBrush ParseBrush(string hex, string fallback)
        {
            try
            {
                var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
                b?.Freeze();
                return b;
            }
            catch
            {
                var fb = (SolidColorBrush)new BrushConverter().ConvertFromString(fallback);
                fb?.Freeze();
                return fb;
            }
        }

        private void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public sealed class BendRowItemViewModel : INotifyPropertyChanged
    {
        private string _angleText = "90";
        private string _selectedVText = string.Empty;
        private string _detailInfo = string.Empty;
        private readonly Action _onChanged;
        private readonly Func<(string M, string T)> _getMaterialAndThickness;

        public BendRowItemViewModel(Func<(string M, string T)> getMaterialAndThickness, Action onChanged, Action<BendRowItemViewModel> onRemove)
        {
            _getMaterialAndThickness = getMaterialAndThickness ?? (() => (string.Empty, string.Empty));
            _onChanged = onChanged;
            AvailableVOptions = new ObservableCollection<TraVCandidate>();
            RemoveCommand = new RelayCommand(_ => onRemove?.Invoke(this));
            PickAngleCommand = new RelayCommand(_ => OnPickAngle());

            RefreshVOptions();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TraVCandidate> AvailableVOptions { get; }

        public string AngleText
        {
            get => _angleText;
            set
            {
                if (_angleText == value) return;
                _angleText = value ?? string.Empty;
                OnPropertyChanged(nameof(AngleText));
                RefreshVOptions();
                _onChanged?.Invoke();
            }
        }

        public string SelectedVText
        {
            get => _selectedVText;
            set
            {
                if (_selectedVText == value) return;
                _selectedVText = value ?? string.Empty;
                OnPropertyChanged(nameof(SelectedVText));
                UpdateDetailInfo();
                _onChanged?.Invoke();
            }
        }

        public string DetailInfo
        {
            get => _detailInfo;
            private set
            {
                if (_detailInfo == value) return;
                _detailInfo = value;
                OnPropertyChanged(nameof(DetailInfo));
            }
        }

        public ICommand RemoveCommand { get; }
        public ICommand PickAngleCommand { get; }

        public void SetAngleFromCad(double angleDeg)
        {
            AngleText = TraVCalculator.FormatAngle(angleDeg);
        }

        public void RefreshVOptions()
        {
            var (m, t) = _getMaterialAndThickness();
            double angle = 90.0;
            if (!double.TryParse(AngleText?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out angle) || angle <= 0)
                angle = 90.0;

            var cands = TraVCalculator.GetBendOptionsForFname(m, t, angle);
            AvailableVOptions.Clear();
            foreach (var c in cands)
                AvailableVOptions.Add(c);

            if (AvailableVOptions.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(_selectedVText) || !AvailableVOptions.Any(x => x.FormattedText == _selectedVText))
                {
                    _selectedVText = AvailableVOptions[0].FormattedText;
                    OnPropertyChanged(nameof(SelectedVText));
                }
            }
            UpdateDetailInfo();
        }

        /// <summary>
        /// Các cụm thông số hiển thị trong WrapPanel: mỗi cụm (kèm dấu " - " phía sau, trừ cụm cuối) là 1 khối
        /// không ngắt, nên khi bảng hẹp sẽ xuống dòng nguyên cụm (vd. "Rend 3" không bị tách).
        /// </summary>
        public List<string> DetailChunks
        {
            get => _detailChunks;
            private set
            {
                _detailChunks = value ?? new List<string>();
                OnPropertyChanged(nameof(DetailChunks));
            }
        }
        private List<string> _detailChunks = new List<string>();

        private void UpdateDetailInfo()
        {
            var match = AvailableVOptions.FirstOrDefault(x => x.FormattedText == _selectedVText);
            SelectedCandidate = match;
            DetailInfo = match != null ? match.DetailInfo : string.Empty;

            var parts = match?.DetailParts ?? new List<string>();
            DetailChunks = parts.Select((p, i) => i < parts.Count - 1 ? p + " - " : p).ToList();
            SourceText = match != null && match.Source.Length > 0 ? $"[{match.Source}] {match.SourceInfo}" : string.Empty;

            string hint = string.Empty;
            if (match == null && AvailableVOptions.Count == 0 && !string.IsNullOrWhiteSpace(_getMaterialAndThickness().T))
                hint = "⚠ Không có số liệu TraV / góc bẻ thử cho góc này — bổ sung góc bẻ thử trong Cài đặt → Dữ liệu";
            WarningText = match?.Warning is string w && w.Length > 0 ? w : hint;
        }

        /// <summary>Lựa chọn V đang chọn (null khi dòng V hút từ bản vẽ không có trong danh sách tra).</summary>
        public TraVCandidate SelectedCandidate { get; private set; }

        /// <summary>Nhãn nguồn số liệu khi không tính được từ bảng TraV: "[Bẻ thử] Góc bẻ thử tham khảo tấm ...".</summary>
        public string SourceText
        {
            get => _sourceText;
            private set
            {
                if (_sourceText == value) return;
                _sourceText = value ?? string.Empty;
                OnPropertyChanged(nameof(SourceText));
                OnPropertyChanged(nameof(SourceVisibility));
            }
        }
        private string _sourceText = string.Empty;

        public Visibility SourceVisibility => string.IsNullOrEmpty(_sourceText) ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>Cảnh báo chữ đỏ dưới dòng bẻ (vd. góc nhọn nhỏ hơn góc tối thiểu trong bảng).</summary>
        public string WarningText
        {
            get => _warningText;
            private set
            {
                if (_warningText == value) return;
                _warningText = value ?? string.Empty;
                OnPropertyChanged(nameof(WarningText));
                OnPropertyChanged(nameof(WarningVisibility));
            }
        }
        private string _warningText = string.Empty;

        public Visibility WarningVisibility => string.IsNullOrEmpty(_warningText) ? Visibility.Collapsed : Visibility.Visible;

        private void OnPickAngle()
        {
            CadFnameService.PendingAngleReceiver = deg =>
            {
                SetAngleFromCad(deg);
            };
            CadCompat.SendCommand("\x03\x03OHTA_FN_PICK_ANGLE\n");
        }

        private void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public sealed class PressRowItemViewModel : INotifyPropertyChanged
    {
        private string _innerRText = "5";
        private string _angleText = "110";
        private readonly Action _onChanged;

        public PressRowItemViewModel(Action onChanged, Action<PressRowItemViewModel> onRemove)
        {
            _onChanged = onChanged;
            RemoveCommand = new RelayCommand(_ => onRemove?.Invoke(this));
            PickAngleCommand = new RelayCommand(_ => OnPickAngle());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string InnerRText
        {
            get => _innerRText;
            set
            {
                if (_innerRText == value) return;
                _innerRText = value ?? string.Empty;
                OnPropertyChanged(nameof(InnerRText));
                OnPropertyChanged(nameof(FormattedLine));
                _onChanged?.Invoke();
            }
        }

        public string AngleText
        {
            get => _angleText;
            set
            {
                if (_angleText == value) return;
                _angleText = value ?? string.Empty;
                OnPropertyChanged(nameof(AngleText));
                OnPropertyChanged(nameof(FormattedLine));
                _onChanged?.Invoke();
            }
        }

        public string FormattedLine => TraVCalculator.FormatPressOneTimeLine(_innerRText, _angleText);

        public ICommand RemoveCommand { get; }
        public ICommand PickAngleCommand { get; }

        public void SetAngleFromCad(double angleDeg)
        {
            AngleText = TraVCalculator.FormatAngle(angleDeg);
        }

        private void OnPickAngle()
        {
            CadFnameService.PendingAngleReceiver = deg =>
            {
                SetAngleFromCad(deg);
            };
            CadCompat.SendCommand("\x03\x03OHTA_FN_PICK_ANGLE\n");
        }

        private void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

