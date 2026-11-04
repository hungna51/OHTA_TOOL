using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OhtaTool.Plugin.Services
{
    /// <summary>Gộp nhiều PDF thành một (tab PRINT). Đăng ký resolver DLL trước khi đụng PdfSharp.</summary>
    public static class PdfMerger
    {
        public static void Merge(IList<string> inputs, string output)
        {
            PluginAssemblyResolver.EnsureRegistered();
            MergeCore(inputs, output);
        }

        // Tách hàm + NoInlining: JIT chỉ nạp PdfSharp khi vào hàm này, lúc resolver đã có.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MergeCore(IList<string> inputs, string output) => PdfSharpMerge.Merge(inputs, output);
    }
}
