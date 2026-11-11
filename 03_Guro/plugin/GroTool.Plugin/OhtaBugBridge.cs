using System;
using System.Linq;
using System.Reflection;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
#else
using Autodesk.AutoCAD.ApplicationServices;
#endif

namespace ZwCadPlugin
{
    /// <summary>
    /// Mục 209 (ADR 0007): lệnh C# của GROTOOL gặp lỗi -> chuyển sang báo lỗi của OHTATOOL.dll.
    /// GROTOOL KHÔNG tham chiếu OHTATOOL (ADR 0001) nên gọi bằng phản chiếu. HỢP ĐỒNG: kiểu
    /// "OhtaTool.Plugin.Core.BugGuard", hàm tĩnh ReportException(string, Exception). OHTATOOL chưa nạp
    /// thì in lỗi ra dòng lệnh như trước.
    /// </summary>
    internal static class OhtaBugBridge
    {
        private static MethodInfo _report;
        private static MethodInfo _log;

        /// <summary>
        /// Ghi exception vào nhật ký báo lỗi của OHTATOOL (OhtaTool.Plugin.Core.BugLog.Write(string, Exception)).
        /// Các catch của GROTOOL gọi hàm này; OHTATOOL chưa nạp thì bỏ qua. Không bao giờ ném.
        /// </summary>
        public static void Log(string nguon, System.Exception ex)
        {
            try
            {
                if (_log == null)
                {
                    Assembly ohta = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => string.Equals(a.GetName().Name, "OHTATOOL", StringComparison.OrdinalIgnoreCase));
                    _log = ohta?.GetType("OhtaTool.Plugin.Core.BugLog")
                        ?.GetMethod("Write", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(System.Exception) }, null);
                }
                _log?.Invoke(null, new object[] { nguon, ex });
            }
            catch
            {
            }
        }

        private static MethodInfo _info;

        /// <summary>Mục 220: ghi một dòng thông tin vào ohta.log (BugLog.Write(string, string)). Không bao giờ ném.</summary>
        public static void Info(string nguon, string dong)
        {
            try
            {
                if (_info == null)
                {
                    Assembly ohta = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => string.Equals(a.GetName().Name, "OHTATOOL", StringComparison.OrdinalIgnoreCase));
                    _info = ohta?.GetType("OhtaTool.Plugin.Core.BugLog")
                        ?.GetMethod("Write", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null);
                }
                _info?.Invoke(null, new object[] { nguon, dong });
            }
            catch
            {
            }
        }

        public static void Run(string lenh, Action body)
        {
            try
            {
                body();
            }
            catch (System.Exception ex)
            {
                if (!TryReport(lenh, ex))
                {
                    try
                    {
                        Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n[GRO] Loi lenh " + lenh + ": " + ex.Message + "\n");
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool TryReport(string lenh, System.Exception ex)
        {
            try
            {
                if (_report == null)
                {
                    Assembly ohta = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => string.Equals(a.GetName().Name, "OHTATOOL", StringComparison.OrdinalIgnoreCase));
                    _report = ohta?.GetType("OhtaTool.Plugin.Core.BugGuard")
                        ?.GetMethod("ReportException", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(System.Exception) }, null);
                }
                if (_report == null)
                    return false;
                _report.Invoke(null, new object[] { lenh, ex });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
