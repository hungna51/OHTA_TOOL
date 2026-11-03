using System;
using System.IO;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Sinh chuỗi macro thực thi lệnh CAD an toàn cho cả ZWCAD và AutoCAD (hỗ trợ đường dẫn có khoảng trắng).
    /// </summary>
    public static class MacroBuilder
    {
        public static string ToLispSafePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return path.Trim().Replace('\\', '/').Replace("\"", "\\\"");
        }

        /// <summary>
        /// Tạo biểu thức LISP nạp một file .lsp tương đối trong thư mục mục con:
        /// (if (findfile "C:/.../lisp/file.lsp") (load "C:/.../lisp/file.lsp"))
        /// </summary>
        public static string BuildLoadLispExpression(string moduleDirectory, string relativeLispFile)
        {
            if (string.IsNullOrWhiteSpace(moduleDirectory) || string.IsNullOrWhiteSpace(relativeLispFile))
                return string.Empty;

            string fullPath = Path.IsPathRooted(relativeLispFile)
                ? relativeLispFile
                : Path.Combine(moduleDirectory, relativeLispFile);

            string safePath = ToLispSafePath(fullPath);
            return "(if (findfile \"" + safePath + "\") (load \"" + safePath + "\"))";
        }

        /// <summary>
        /// Tạo chuỗi gửi xuống doc.SendStringToExecute khi người dùng bấm nút công cụ trên Tab Palette.
        /// Ký tự \x03\x03 hủy lệnh đang treo trên dòng lệnh (giống ^C^C của Tool Palettes chuẩn).
        /// </summary>
        public static string BuildToolMacro(string moduleDirectory, ToolItem tool)
        {
            if (tool == null || string.IsNullOrWhiteSpace(tool.Command))
                return string.Empty;

            string rawCmd = tool.Command.Trim();
            string loadExpr = BuildLoadLispExpression(moduleDirectory, tool.LispFile);

            // Nếu người dùng viết macro thô có sẵn ^C^C ở đầu thì chuyển ^C thành \x03
            if (rawCmd.StartsWith("^C^C", StringComparison.OrdinalIgnoreCase))
            {
                string body = rawCmd.Substring(4).TrimStart();
                if (!string.IsNullOrEmpty(loadExpr))
                    return "\x03\x03" + loadExpr + " " + body + "\n";
                return "\x03\x03" + body + "\n";
            }

            if (!string.IsNullOrEmpty(loadExpr))
            {
                return "\x03\x03" + loadExpr + " " + rawCmd + "\n";
            }

            return "\x03\x03" + rawCmd + "\n";
        }
    }
}

