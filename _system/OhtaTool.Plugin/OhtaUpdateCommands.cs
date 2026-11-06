using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OhtaTool.Plugin.Core;
using OhtaTool.Plugin.Services;

[assembly: CommandClass(typeof(OhtaTool.Plugin.OhtaUpdateCommands))]

namespace OhtaTool.Plugin
{
    /// <summary>
    /// Tự cập nhật OHTA TOOL từ GitHub Releases (ADR 0004).
    /// - OHTA_Loader.lsp gọi (ohta_net_update_check) mỗi lần mở bản vẽ: còn bản mới chưa cài thì hỏi
    ///   "Cập nhật ngay / Để sau"; gọi mạng tối đa 1 lần/ngày, các lần nhắc dùng kết quả đã lưu.
    /// - OHTAUPDATE: kiểm tra ngay. OHTAROLLBACK: quay về bản trước lần cập nhật gần nhất.
    /// Mạng + ghi file chạy ở luồng nền; hộp thoại hiện trên luồng giao diện lúc CAD rảnh (Idle).
    /// </summary>
    public sealed class OhtaUpdateCommands
    {
        private const string Caption = "OHTA TOOL — Cập nhật";
        private const string MutexName = @"Local\OHTA_TOOL_Update";

        private static Dispatcher _ui;
        private static string _loadedVersion = "0";
        private static int _checking;
        private static int _installing;
        private static bool _promptShowing;
        private static readonly ConditionalWeakTable<CadDocument, object> _promptedDocs = new ConditionalWeakTable<CadDocument, object>();

        private static string StatePath => Path.Combine(UpdateState.DefaultDirectory, "state.json");
        private static string BackupDir => Path.Combine(UpdateState.DefaultDirectory, "backup");

        /// <summary>Phiên bản lúc nạp DLL (bản đang chạy trong phiên CAD này).</summary>
        public static string LoadedVersion => _loadedVersion;

        /// <summary>Dòng trạng thái cập nhật cho tab Info (đọc state.json, không gọi mạng).</summary>
        public static string DescribeStatus()
        {
            string root = CadCompat.GetRootDirectory();
            string current = string.IsNullOrEmpty(root) ? _loadedVersion : UpdateVersion.ReadInstalled(root);
            string text = ToolInfoCore.DescribeUpdateStatus(UpdateState.Load(StatePath), current);
            return _checking == 1 ? "Đang kiểm tra bản mới trên GitHub..." : text;
        }

        /// <summary>Gọi từ OhtaPaletteCommands.Initialize() (luồng giao diện).</summary>
        public static void OnInitialize()
        {
            try
            {
                _ui = Dispatcher.CurrentDispatcher;
                string root = CadCompat.GetRootDirectory();
                if (string.IsNullOrEmpty(root))
                    return;
                _loadedVersion = UpdateVersion.ReadInstalled(root);
                UpdateInstaller.CleanupOldFiles(root);
            }
            catch
            {
            }
        }

        // ====================================================================
        // Lệnh
        // ====================================================================

        [LispFunction("OHTA_NET_UPDATE_CHECK")]
        public static object LispCheckOnDocumentOpen(ResultBuffer rb)
        {
            try
            {
                CheckOnDocumentOpen();
            }
            catch
            {
                // Không bao giờ làm hỏng việc mở bản vẽ
            }
            return true;
        }

        [CommandMethod("OHTAUPDATE")]
        [CommandMethod("OHTA_UPDATE")]
        public static void CmdUpdateNow()
        {
            BugGuard.Run("OHTAUPDATE", () =>
            {
                CadCompat.WriteMessage("\n[OHTA TOOL] Đang kiểm tra bản mới trên GitHub...\n");
                StartCheck(manual: true);
            });
        }

        [LispFunction("OHTA_NET_UPDATE")]
        public static object LispUpdateNow(ResultBuffer rb)
        {
            CmdUpdateNow();
            return true;
        }

        [CommandMethod("OHTAROLLBACK")]
        [CommandMethod("OHTA_ROLLBACK")]
        public static void CmdRollback()
        {
            BugGuard.Run("OHTAROLLBACK", () =>
            {
                string root = CadCompat.GetRootDirectory();
                string backup = Directory.Exists(BackupDir)
                    ? Directory.GetFiles(BackupDir, "OHTA_TOOL_v*.zip").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                    : null;
                if (string.IsNullOrEmpty(root) || backup == null)
                {
                    MessageBox.Show("Chưa có bản sao lưu nào (chỉ có sau lần tự cập nhật đầu tiên).", Caption,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string backupVersion = UpdateInstaller.ReadBackupVersion(backup) ?? "?";
                if (MessageBox.Show(
                        $"Quay về OHTA TOOL v{backupVersion} (đang dùng v{UpdateVersion.ReadInstalled(root)})?\n\n" +
                        "Lisp / nút bạn tự thêm vẫn giữ nguyên. Xong cần khởi động lại CAD.",
                        Caption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                if (Interlocked.Exchange(ref _installing, 1) == 1)
                    return;
                Task.Run(() =>
                {
                    try
                    {
                        UpdateReport rep;
                        using (var mutex = new Mutex(false, MutexName))
                        {
                            if (!mutex.WaitOne(0))
                                throw new InvalidOperationException("Một cửa sổ CAD khác đang cập nhật OHTA TOOL. Hãy thử lại sau.");
                            try { rep = UpdateInstaller.Rollback(root, backup); }
                            finally { mutex.ReleaseMutex(); }
                        }
                        // Quay lại rồi thì bản trên GitHub lại "mới" -> nhắc lại theo quy tắc bình thường
                        PostToUi(() => MessageBox.Show(
                            $"Đã quay về OHTA TOOL v{backupVersion} ({rep.Written} file).\n\nHãy khởi động lại CAD để dùng bản này.",
                            Caption, MessageBoxButton.OK, MessageBoxImage.Information));
                    }
                    catch (Exception ex)
                    {
                        OhtaTool.Plugin.Core.BugLog.Write("OHTA/OhtaUpdateCommands", ex);
                        PostToUi(() => MessageBox.Show("Không quay lại được:\n" + ex.GetBaseException().Message, Caption,
                            MessageBoxButton.OK, MessageBoxImage.Warning));
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _installing, 0);
                    }
                });
            });
        }

        [LispFunction("OHTA_NET_ROLLBACK")]
        public static object LispRollback(ResultBuffer rb)
        {
            CmdRollback();
            return true;
        }

        // ====================================================================
        // Kiểm tra
        // ====================================================================

        private static void CheckOnDocumentOpen()
        {
            string root = CadCompat.GetRootDirectory();
            if (string.IsNullOrEmpty(root))
                return;

            string onDisk = UpdateVersion.ReadInstalled(root);
            if (UpdateVersion.Compare(onDisk, _loadedVersion) != 0)
            {
                // Cửa sổ CAD khác (hoặc chính phiên này) đã cài bản mới -> chỉ nhắc khởi động lại
                CadCompat.WriteMessage($"\n[OHTA TOOL] Đã cài bản v{onDisk} — khởi động lại CAD để dùng bản mới.\n");
                return;
            }

            UpdateState state = UpdateState.Load(StatePath);
            if (state.Latest != null && UpdateVersion.IsNewer(state.Latest.Version, onDisk))
                PostToUi(() => PromptForActiveDocument(state.Latest));

            if (!state.CheckedToday(DateTime.Now))
                StartCheck(manual: false);
        }

        private static void StartCheck(bool manual)
        {
            if (Interlocked.Exchange(ref _checking, 1) == 1)
                return;

            Task.Run(() =>
            {
                try
                {
                    ReleaseInfo latest = GitHubReleaseClient.FetchLatest();
                    UpdateState state = UpdateState.Load(StatePath);
                    state.LastCheckDate = DateTime.Now.ToString("yyyy-MM-dd");
                    state.Latest = latest;
                    state.Save(StatePath);

                    string root = CadCompat.GetRootDirectory();
                    string current = UpdateVersion.ReadInstalled(root);
                    if (latest != null && UpdateVersion.IsNewer(latest.Version, current))
                    {
                        if (manual)
                            PostToUi(() => ShowPrompt(latest));
                        else
                            PostToUi(() => PromptForActiveDocument(latest));
                    }
                    else if (manual)
                    {
                        PostToUi(() => MessageBox.Show(
                            latest == null
                                ? "Chưa có bản phát hành nào trên GitHub."
                                : $"Đang dùng bản mới nhất: OHTA TOOL v{current}.",
                            Caption, MessageBoxButton.OK, MessageBoxImage.Information));
                    }
                }
                catch (Exception ex)
                {
                    OhtaTool.Plugin.Core.BugLog.Write("OHTA/OhtaUpdateCommands", ex);
                    string msg = ex.GetBaseException().Message;
                    if (manual)
                        PostToUi(() => MessageBox.Show("Không kiểm tra được bản mới (mạng / GitHub):\n" + msg, Caption,
                            MessageBoxButton.OK, MessageBoxImage.Warning));
                    else
                        PostToUi(() => CadCompat.WriteMessage("\n[OHTA TOOL] Không kiểm tra được bản cập nhật: " + msg + "\n"));
                }
                finally
                {
                    Interlocked.Exchange(ref _checking, 0);
                }
            });
        }

        /// <summary>Nhắc tối đa 1 lần cho mỗi bản vẽ (kết quả lưu sẵn + kết quả mạng cùng ngày không hỏi 2 lần).</summary>
        private static void PromptForActiveDocument(ReleaseInfo latest)
        {
            CadDocument doc = CadCompat.ActiveDocument;
            if (doc != null)
            {
                if (_promptedDocs.TryGetValue(doc, out _))
                    return;
                _promptedDocs.Add(doc, null);
            }
            ShowPrompt(latest);
        }

        private static void ShowPrompt(ReleaseInfo latest)
        {
            if (_promptShowing || _installing == 1)
                return;

            string root = CadCompat.GetRootDirectory();
            string current = UpdateVersion.ReadInstalled(root);
            if (!UpdateVersion.IsNewer(latest.Version, current))
                return;

            string notes = string.IsNullOrWhiteSpace(latest.Notes) ? "(không có ghi chú)" : latest.Notes;
            if (notes.Length > 1500)
                notes = notes.Substring(0, 1500) + "\n...";

            MessageBoxResult ans;
            _promptShowing = true;
            try
            {
                ans = MessageBox.Show(
                    $"Có bản OHTA TOOL v{latest.Version} (đang dùng v{current}).\n\n{notes}\n\n" +
                    "Cập nhật ngay?\n   Yes = Cập nhật ngay     No = Để sau (nhắc lại khi mở bản vẽ khác)",
                    Caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
            }
            finally
            {
                _promptShowing = false;
            }

            if (ans == MessageBoxResult.Yes)
                StartInstall(latest);
        }

        // ====================================================================
        // Cài đặt
        // ====================================================================

        private static void StartInstall(ReleaseInfo latest)
        {
            if (Interlocked.Exchange(ref _installing, 1) == 1)
                return;

            CadCompat.WriteMessage($"\n[OHTA TOOL] Đang tải và cài bản v{latest.Version}...\n");
            Task.Run(() =>
            {
                string work = Path.Combine(Path.GetTempPath(), "ohta_update_" + Guid.NewGuid().ToString("N"));
                try
                {
                    string root = CadCompat.GetRootDirectory();
                    if (string.IsNullOrEmpty(root))
                        throw new InvalidOperationException("Không xác định được thư mục OHTA_TOOL.");

                    UpdateReport rep;
                    using (var mutex = new Mutex(false, MutexName))
                    {
                        if (!mutex.WaitOne(0))
                            throw new InvalidOperationException("Một cửa sổ CAD khác đang cập nhật OHTA TOOL. Hãy thử lại sau.");
                        try
                        {
                            string current = UpdateVersion.ReadInstalled(root);
                            if (!UpdateVersion.IsNewer(latest.Version, current))
                            {
                                PostToUi(() => MessageBox.Show(
                                    $"OHTA TOOL v{current} đã được cài (từ cửa sổ CAD khác).\n\nHãy khởi động lại CAD để dùng bản mới.",
                                    Caption, MessageBoxButton.OK, MessageBoxImage.Information));
                                return;
                            }

                            string extracted = GitHubReleaseClient.DownloadAndExtract(latest, work);
                            string backup = Path.Combine(BackupDir, "OHTA_TOOL_v" + current + ".zip");
                            rep = UpdateInstaller.Install(root, extracted, backup, UpdateInstaller.BaseStoreDirectory(UpdateState.DefaultDirectory));
                            BugLog.Write("UPDATE", "da cai v" + current + " -> ban moi tai " + root);

                            // Chỉ giữ 1 bản sao lưu gần nhất
                            foreach (string f in Directory.GetFiles(BackupDir, "OHTA_TOOL_v*.zip"))
                            {
                                if (!string.Equals(f, backup, StringComparison.OrdinalIgnoreCase))
                                    try { File.Delete(f); } catch { }
                            }
                        }
                        finally
                        {
                            mutex.ReleaseMutex();
                        }
                    }

                    PostToUi(() =>
                    {
                        CadCompat.WriteMessage($"\n[OHTA TOOL] Đã cập nhật lên v{latest.Version} ({rep.Written} file, gộp {rep.Merged} module.json, xoá {rep.Deleted}).\n");
                        MessageBox.Show(
                            $"Đã cập nhật OHTA TOOL lên v{latest.Version}.\n\n" +
                            "Hãy đóng và mở lại CAD để dùng bản mới.\n(Gõ OHTAROLLBACK nếu cần quay về bản cũ.)",
                            Caption, MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    OhtaTool.Plugin.Core.BugLog.Write("OHTA/OhtaUpdateCommands", ex);
                    string msg = ex.GetBaseException().Message;
                    PostToUi(() => MessageBox.Show(
                        "Cập nhật không thành công, OHTA TOOL vẫn giữ bản đang dùng:\n" + msg, Caption,
                        MessageBoxButton.OK, MessageBoxImage.Warning));
                }
                finally
                {
                    try { Directory.Delete(work, true); } catch { }
                    Interlocked.Exchange(ref _installing, 0);
                }
            });
        }

        // ====================================================================
        // Luồng giao diện
        // ====================================================================

        /// <summary>Chạy action trên luồng giao diện, đợi lúc CAD rảnh (không chen giữa lệnh / lúc đang nạp bản vẽ).</summary>
        private static void PostToUi(Action action)
        {
            Action onIdle = () =>
            {
                EventHandler handler = null;
                handler = (s, e) =>
                {
                    CadApp.Idle -= handler;
                    try { action(); } catch { }
                };
                CadApp.Idle += handler;
            };

            if (_ui != null)
                _ui.BeginInvoke(onIdle);
            else
                onIdle();
        }
    }
}
