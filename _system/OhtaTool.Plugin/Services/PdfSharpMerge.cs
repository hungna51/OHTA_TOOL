using System.Collections.Generic;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace OhtaTool.Plugin.Services
{
    /// <summary>
    /// Nơi duy nhất tham chiếu kiểu PdfSharp. Gọi qua PdfMerger để resolver DLL được đăng ký
    /// trước khi JIT chạm tới các kiểu này (ADR 0005).
    /// </summary>
    internal static class PdfSharpMerge
    {
        public static void Merge(IList<string> inputs, string output)
        {
            using (var target = new PdfDocument())
            {
                foreach (string path in inputs)
                {
                    using (PdfDocument src = PdfReader.Open(path, PdfDocumentOpenMode.Import))
                    {
                        for (int i = 0; i < src.PageCount; i++)
                            target.AddPage(src.Pages[i]);
                    }
                }
                target.Save(output);
            }
        }
    }
}
