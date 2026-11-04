using System;
using System.IO;
using System.Reflection;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Nạp DLL phụ thuộc (PdfSharp, Microsoft.Extensions.*, System.Memory…) từ thư mục chứa OHTATOOL.dll,
    /// bất kể phiên bản được yêu cầu. Plugin không có app.config nên ZWCAD (net48) không có binding
    /// redirect; thiếu bước này dễ gặp FileLoadException khi phiên bản lệch (ADR 0005).
    /// </summary>
    public static class PluginAssemblyResolver
    {
        private static readonly object Gate = new object();
        private static bool _registered;

        public static void EnsureRegistered()
        {
            lock (Gate)
            {
                if (_registered)
                    return;
                AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
                _registered = true;
            }
        }

        private static Assembly OnResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string simpleName = new AssemblyName(args.Name).Name;
                foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(loaded.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                        return loaded;
                }

                string dir = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location);
                if (string.IsNullOrEmpty(dir))
                    return null;
                string candidate = Path.Combine(dir, simpleName + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
