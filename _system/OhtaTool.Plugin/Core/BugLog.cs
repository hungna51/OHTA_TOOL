using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// Nhật ký xoay vòng của OHTA TOOL (%LOCALAPPDATA%\OHTA_TOOL\logs): ohta.log → ohta.1.log → ohta.2.log.
    /// Báo lỗi gửi kèm các dòng cuối. Ghi nhật ký không bao giờ được làm hỏng lệnh đang chạy, nên mọi lỗi bị nuốt.
    /// </summary>
    public static class BugLog
    {
        public const long MaxBytes = 1024 * 1024;
        public const int KeepFiles = 3;
        private static readonly object Gate = new object();

        public static string Directory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "logs");

        private static string FilePath(int index) =>
            Path.Combine(Directory, index == 0 ? "ohta.log" : "ohta." + index + ".log");

        /// <summary>Số dòng cuối gửi kèm báo lỗi.</summary>
        public const int TailLines = 500;

        // Gộp dòng giống hệt liên tiếp (catch trong vòng lặp duyệt đối tượng): ghi 1 lần + "lap lai N lan"
        private static string _lastKey;
        private static string _lastNguon;
        private static int _repeat;

        public static void Write(string nguon, string message)
        {
            try
            {
                string key = (nguon ?? "") + "|" + (message ?? "");
                lock (Gate)
                {
                    if (key == _lastKey)
                    {
                        _repeat++;
                        return;
                    }
                    if (_repeat > 0)
                        Append(_lastNguon, "(dong tren lap lai " + _repeat + " lan)");
                    _lastKey = key;
                    _lastNguon = nguon;
                    _repeat = 0;
                    Append(nguon, message);
                }
            }
            catch
            {
                // nhật ký là phụ: không ghi được (đĩa đầy, file bị khoá) thì bỏ qua
            }
        }

        private static void Append(string nguon, string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                + " [" + (nguon ?? "") + "] " + (message ?? "") + Environment.NewLine;
            System.IO.Directory.CreateDirectory(Directory);
            string cur = FilePath(0);
            var info = new FileInfo(cur);
            if (info.Exists && info.Length > 0 && info.Length + line.Length > MaxBytes)
                Rotate();
            using (var fs = new FileStream(cur, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        public static void Write(string nguon, Exception ex) =>
            Write(nguon, ex == null ? "(exception null)" : ex.ToString());

        private static void Rotate()
        {
            string oldest = FilePath(KeepFiles - 1);
            if (File.Exists(oldest))
                File.Delete(oldest);
            for (int i = KeepFiles - 2; i >= 0; i--)
            {
                string src = FilePath(i);
                if (File.Exists(src))
                    File.Move(src, FilePath(i + 1));
            }
        }

        /// <summary>Tối đa `lines` dòng cuối, nối sang file cũ hơn khi file hiện tại chưa đủ.</summary>
        public static List<string> Tail(int lines)
        {
            var result = new List<string>();
            try
            {
                lock (Gate)
                {
                    for (int i = 0; i < KeepFiles && result.Count < lines; i++)
                    {
                        string path = FilePath(i);
                        if (!File.Exists(path))
                            continue;
                        List<string> fileLines = ReadLines(path);
                        int need = lines - result.Count;
                        int start = Math.Max(0, fileLines.Count - need);
                        result.InsertRange(0, fileLines.GetRange(start, fileLines.Count - start));
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        private static List<string> ReadLines(string path)
        {
            var list = new List<string>();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                string l;
                while ((l = sr.ReadLine()) != null)
                    list.Add(l);
            }
            return list;
        }
    }
}
