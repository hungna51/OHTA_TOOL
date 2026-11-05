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
            TestBugAttachmentsAndQueue();
            TestBugSheet();
        }

        private static void TestBugSheet()
        {
            Check("Sheet: submit_bug khong bi chan ban cu", !SheetActions.RequiresVersionGate("submit_bug"));
            Check("Sheet: list_bugs khong bi chan", !SheetActions.RequiresVersionGate("list_bugs"));
            Check("Sheet: resolve_bug khong bi chan", !SheetActions.RequiresVersionGate("resolve_bug"));
            Check("Sheet: list_requests khong bi chan", !SheetActions.RequiresVersionGate("list_requests"));
            Check("Sheet: submit_request van bi chan", SheetActions.RequiresVersionGate("submit_request"));
            var list = BugAdminItem.ParseList("{\"ok\":true,\"isAdmin\":true,\"bugs\":[{\"ID\":\"abc\",\"Muc_do\":\"Nhỏ\",\"Mo_ta\":\"x\",\"Anh\":\"\"}]}");
            Check("Sheet: parse bugs", list.Count == 1 && list[0].Id == "abc" && list[0].MucDo == "Nhỏ" && list[0].Anh == "");
            Check("Sheet: parse rong", BugAdminItem.ParseList("{\"ok\":true,\"bugs\":[]}").Count == 0);
            Check("Sheet: parse json hong", BugAdminItem.ParseList("<html>").Count == 0);
            Check("Sheet: parse null", BugAdminItem.ParseList(null).Count == 0);
        }

        private static void TestBugAttachmentsAndQueue()
        {
            string d = Path.Combine(Path.GetTempPath(), "ohta_bugatt_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(d);
            string a = Path.Combine(d, "a.csv");
            File.WriteAllText(a, "x,y\n1,2");
            var (zip, err) = BugAttachments.Zip(new[] { a, Path.Combine(d, "khong_co.dwg") });
            Check("BugAtt: zip co du lieu", zip != null && zip.Length > 0 && err == null, err);
            using (new FileStream(a, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                Check("BugAtt: zip file dang mo ghi", BugAttachments.Zip(new[] { a }).zip != null);
            var (z2, e2) = BugAttachments.Zip(new[] { a }, maxBytes: 10);
            Check("BugAtt: vuot gioi han bi chan", z2 == null && e2 != null && e2.Contains("25 MB"), e2);
            Check("BugAtt: rong tra null", BugAttachments.Zip(new string[0]).zip == null && BugAttachments.Zip(new string[0]).error == null);
            string sub = Path.Combine(d, "sub"); Directory.CreateDirectory(sub);
            string a2 = Path.Combine(sub, "a.csv"); File.WriteAllText(a2, "khac");
            var (z3, _) = BugAttachments.Zip(new[] { a, a2 });
            using (var zs = new System.IO.Compression.ZipArchive(new MemoryStream(z3)))
                Check("BugAtt: trung ten them hau to", zs.Entries.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(new[] { "a.csv", "a_2.csv" }),
                    string.Join(",", zs.Entries.Select(x => x.Name)));

            string oldQ = BugQueue.Directory;
            BugQueue.Directory = Path.Combine(d, "queue");
            Check("BugQueue: thu muc chua co -> rong", BugQueue.Pending().Count == 0);
            var r = BugReport.Create(); r.MoTa = "test";
            BugQueue.Enqueue(r); BugQueue.Enqueue(r);
            Check("BugQueue: cung Id khong nhan doi", BugQueue.Pending().Count == 1);
            Check("BugQueue: giu Id", BugQueue.Pending()[0].Id == r.Id);
            File.WriteAllText(Path.Combine(BugQueue.Directory, "hong.json"), "{khong phai json");
            Check("BugQueue: bo qua file hong", BugQueue.Pending().Count == 1);
            BugQueue.Remove(r.Id);
            Check("BugQueue: xoa", BugQueue.Pending().Count == 0);
            BugQueue.Directory = oldQ;
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
