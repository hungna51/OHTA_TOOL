using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Tests
{
    internal static partial class Program
    {
        private static void TestBugReport()
        {
            var r = BugReport.Create();
            Check("Bug: Id 12 ky tu", r.Id.Length == 12, r.Id);
            Check("Bug: muc do mac dinh Phien", r.MucDo == "Phiền");
            Check("Bug: mo ta rong khong hop le", r.Validate() != null);
            r.MoTa = "  "; Check("Bug: mo ta trang khong hop le", r.Validate() != null);
            r.MoTa = "GRO treo"; Check("Bug: hop le", r.Validate() == null);
            r.MucDo = "Lung tung"; Check("Bug: muc do la khong hop le", r.Validate() != null); r.MucDo = "Nhỏ";
            Check("Bug: ShortError cat 1000", BugReport.ShortError(new string('x', 5000)).Length == 1000);
            Check("Bug: ShortError lay dong dau", BugReport.ShortError("a\r\nb") == "a");
            var f = r.ToFields();
            Check("Bug: fields khong co anh khi null", !f.ContainsKey("anh"));
            Check("Bug: fields co id + mo ta", f["id"] == r.Id && f["mo_ta"] == "GRO treo");
            r.AnhJpeg = new byte[] { 1, 2, 3 };
            Check("Bug: anh base64", r.ToFields()["anh"] == "AQID");
            var back = BugReport.FromJson(r.ToJson());
            Check("Bug: json giu Id + anh", back != null && back.Id == r.Id && back.AnhJpeg.Length == 3 && back.MoTa == "GRO treo");
            Check("Bug: json giu thoi gian", back != null && Math.Abs((back.ThoiGian - r.ThoiGian).TotalSeconds) < 1);

            BugDedup.ResetForTest();
            var ex = new InvalidOperationException("hong\nchi tiet");
            Check("Bug: hoi lan dau", BugDedup.ShouldAsk("GRO", ex));
            Check("Bug: khong hoi lai cung loi", !BugDedup.ShouldAsk("GRO", new InvalidOperationException("hong\nkhac")));
            Check("Bug: hoi loi khac", BugDedup.ShouldAsk("GRO", new ArgumentException("hong")));
            BugDedup.InReport = true;
            Check("Bug: khong hoi long khi dang bao loi", !BugDedup.ShouldAsk("OHTA", "loi moi"));
            BugDedup.InReport = false;
            BugDedup.ResetForTest();

            TestBugLog();
        }

        private static void TestBugLog()
        {
            string old = BugLog.Directory;
            BugLog.Directory = Path.Combine(Path.GetTempPath(), "ohta_buglog_" + Guid.NewGuid().ToString("N"));
            for (int i = 0; i < 300; i++) BugLog.Write("GRO", "dong " + i);
            var tail = BugLog.Tail(200);
            Check("BugLog: tail 200 dong", tail.Count == 200, tail.Count.ToString());
            Check("BugLog: dong cuoi dung", tail.Count == 200 && tail[199].EndsWith("[GRO] dong 299"), tail.LastOrDefault());
            BugLog.Write("X", new string('a', 600_000)); BugLog.Write("X", new string('b', 600_000));
            BugLog.Write("X", "sau xoay");
            Check("BugLog: da xoay", File.Exists(Path.Combine(BugLog.Directory, "ohta.1.log")));
            for (int k = 0; k < 6; k++) BugLog.Write("X", new string('c', 600_000));
            Check("BugLog: toi da 3 file", Directory.GetFiles(BugLog.Directory, "ohta*.log").Length <= 3,
                Directory.GetFiles(BugLog.Directory, "ohta*.log").Length.ToString());
            BugLog.Write("X", "moi");
            Check("BugLog: tail qua ranh gioi file", BugLog.Tail(3).Count == 3);
            BugLog.Write("X", new InvalidOperationException("no tung"));
            Check("BugLog: ghi exception", BugLog.Tail(20).Any(l => l.Contains("InvalidOperationException") && l.Contains("no tung")));
            using (new FileStream(Path.Combine(BugLog.Directory, "ohta.log"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                BugLog.Write("X", "file dang khoa"); // khong duoc nem
            Check("BugLog: ghi khi file khoa khong nem", true);
            BugLog.Directory = old;
        }
    }
}
