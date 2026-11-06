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
            TestBugGuard();
            TestBugReviewFixes();
            TestProgressContent();
        }

        // Thanh tien trinh gui bao loi: dem byte da tai len
        private static void TestProgressContent()
        {
            byte[] data = new byte[200_000];
            new Random(1).NextBytes(data);
            var calls = new List<long>();
            long total = -1;
            var content = new ProgressContent(data, "application/x-www-form-urlencoded", (sent, tot) => { calls.Add(sent); total = tot; });
            var ms = new MemoryStream();
            content.CopyToAsync(ms).GetAwaiter().GetResult();
            Check("Progress: du lieu gui nguyen ven", ms.ToArray().SequenceEqual(data));
            Check("Progress: bao nhieu lan, tang dan", calls.Count >= 3 && calls.SequenceEqual(calls.OrderBy(x => x)), calls.Count.ToString());
            Check("Progress: lan cuoi = tong", calls.Count > 0 && calls[calls.Count - 1] == data.Length && total == data.Length);
            Check("Progress: Content-Length dung", content.Headers.ContentLength == data.Length);
            var cts = new System.Threading.CancellationTokenSource(); cts.Cancel();
            bool huy = false;
            try { new ProgressContent(data, "x/y", null, cts.Token).CopyToAsync(new MemoryStream()).GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { huy = true; }
            Check("Progress: huy giua chung", huy);
        }

        // Sua sau review toan nhanh (ledger "Final: fixed ...")
        private static void TestBugReviewFixes()
        {
            // #2: mo ta dan ca stack trace -> o Sheet <= 20000 ky tu, phan con lai vao chi_tiet
            var r = BugReport.Create(); r.MoTa = new string('m', 60_000); r.ChiTiet = "ct";
            var f = r.ToFields();
            Check("Fix: mo_ta cat <= 20000", f["mo_ta"].Length <= BugReport.MaxMoTa, f["mo_ta"].Length.ToString());
            Check("Fix: mo ta day du vao chi_tiet", f["chi_tiet"].Contains(new string('m', 60_000)));
            Check("Fix: tab_lenh/nguoi_gui cat", BugReport.Create().ToFields()["nguoi_gui"].Length <= 200);

            // #2: gui lai hang cho - loi mang thi giu, may chu tu choi thi bo, khong dung lai o muc hong
            string d = Path.Combine(Path.GetTempPath(), "ohta_resend_" + Guid.NewGuid().ToString("N"));
            string oldQ = BugQueue.Directory; BugQueue.Directory = d;
            Check("Fix: HasPending rong", !BugQueue.HasPending());
            var a = BugReport.Create(); a.MoTa = "a"; a.ThoiGian = DateTime.Now.AddMinutes(-3);
            var b = BugReport.Create(); b.MoTa = "b"; b.ThoiGian = DateTime.Now.AddMinutes(-2);
            var c = BugReport.Create(); c.MoTa = "c"; c.ThoiGian = DateTime.Now.AddMinutes(-1);
            BugQueue.Enqueue(a); BugQueue.Enqueue(b); BugQueue.Enqueue(c);
            Check("Fix: HasPending co", BugQueue.HasPending());
            var sentIds = new List<string>();
            int n = BugResender.Run(x =>
            {
                if (x.Id == a.Id) return BugSendResult.Rejected("Thiếu mô tả");
                if (x.Id == b.Id) return BugSendResult.NetworkError("mat mang");
                sentIds.Add(x.Id); return BugSendResult.Sent();
            });
            Check("Fix: khong dung lai o muc hong, gui duoc c", n == 1 && sentIds.Contains(c.Id));
            var left = BugQueue.Pending().Select(x => x.Id).ToList();
            Check("Fix: may chu tu choi -> bo khoi hang cho", !left.Contains(a.Id));
            Check("Fix: loi mang -> giu lai", left.Contains(b.Id) && left.Count == 1);
            BugQueue.Directory = oldQ;

            // Loi tam cua Apps Script (retry) khong duoc xoa khoi hang cho
            var rRetry = BugSendResult.FromReply(FnameSheetReply.Parse("{\"ok\":false,\"retry\":true,\"message\":\"Lỗi Apps Script: lock\"}"), null);
            Check("Fix: retry -> loi tam, giu hang cho", !rRetry.Ok && !rRetry.RejectedByServer);
            var rRej = BugSendResult.FromReply(FnameSheetReply.Parse("{\"ok\":false,\"message\":\"Thiếu mô tả lỗi.\"}"), null);
            Check("Fix: ok false khong retry -> tu choi", rRej.RejectedByServer && rRej.Message == "Thiếu mô tả lỗi.");
            var rNet = BugSendResult.FromReply(null, "mat mang");
            Check("Fix: khong co tra loi -> loi mang", !rNet.Ok && !rNet.RejectedByServer && rNet.Message == "mat mang");
            Check("Fix: ok true -> da gui", BugSendResult.FromReply(FnameSheetReply.Parse("{\"ok\":true}"), null).Ok);

            // #8 + #10: hop hoi dang mo thi khong hoi long; loi luon in ra dong lenh
            BugDedup.ResetForTest();
            var notes = new List<string>(); int hoi = 0;
            BugGuard.Notify = s => notes.Add(s);
            BugGuard.Capture = () => null;
            BugGuard.ShowForm = (x1, x2, x3, x4) => { };
            BugGuard.AskUser = (nguon, loi) =>
            {
                hoi++;
                Check("Fix: InReport bat khi hop hoi dang mo", BugDedup.InReport);
                BugGuard.ReportLispError("GRO", "loi long trong luc hoi");
                return false;
            };
            BugGuard.Run("X", () => throw new InvalidOperationException("mot"));
            Check("Fix: khong hoi long", hoi == 1);
            Check("Fix: InReport tat sau hop hoi", !BugDedup.InReport);
            BugGuard.Run("X", () => throw new InvalidOperationException("mot"));
            Check("Fix: lan 2 cung loi van in dong lenh", notes.Count(s => s.Contains("mot")) == 2, string.Join(" | ", notes));
            BugGuard.Notify = null; BugGuard.Capture = null; BugGuard.ShowForm = null; BugGuard.AskUser = null;
            BugDedup.ResetForTest();
        }

        private static void TestBugGuard()
        {
            string oldLog = BugLog.Directory;
            BugLog.Directory = Path.Combine(Path.GetTempPath(), "ohta_bugguard_" + Guid.NewGuid().ToString("N"));
            BugDedup.ResetForTest();
            int hoi = 0, form = 0, chup = 0;
            BugGuard.Capture = () => { chup++; return null; };
            BugGuard.ShowForm = (n, l, st, anh) => form++;
            BugGuard.AskUser = (n, l) => { hoi++; return false; };
            BugGuard.Run("TEST", () => throw new InvalidOperationException("no"));
            Check("Guard: khong nem ra ngoai + hoi 1 lan", hoi == 1);
            Check("Guard: chup anh luc loi", chup == 1);
            Check("Guard: tra loi Khong -> khong mo form", form == 0);
            BugGuard.Run("TEST", () => throw new InvalidOperationException("no"));
            Check("Guard: cung loi khong hoi lai", hoi == 1);
            Check("Guard: loi da vao nhat ky", BugLog.Tail(80).Any(l => l.Contains("[TEST]") && l.Contains("InvalidOperationException")));
            BugGuard.AskUser = (n, l) => { hoi++; return true; };
            BugGuard.ReportLispError("GRO", "bad argument type: consp nil (buoc: chia net)");
            Check("Guard: lisp loi -> hoi + mo form", hoi == 2 && form == 1);
            BugDedup.InReport = true;
            BugGuard.ReportLispError("GRO", "loi khac han");
            Check("Guard: khong hoi long", hoi == 2);
            BugDedup.InReport = false;
            BugGuard.AskUser = (n, l) => throw new Exception("UI hong");
            BugGuard.ReportLispError("GRO", "loi thu ba");
            Check("Guard: loi trong luong bao loi khong nem", true);
            bool ran = false;
            BugGuard.Run("TEST", () => ran = true);
            Check("Guard: than lenh chay binh thuong", ran);
            Check("Guard: UserBreak nhan theo ten", BugGuard.IsUserCancel(new FakeCadException("UserBreak")));
            Check("Guard: loi CAD khac khong phai huy", !BugGuard.IsUserCancel(new FakeCadException("eInvalidInput")));
            BugGuard.Capture = null; BugGuard.ShowForm = null; BugGuard.AskUser = null;
            BugDedup.ResetForTest();
            BugLog.Directory = oldLog;
        }

        // Giả lập Runtime.Exception của CAD: có thuộc tính ErrorStatus
        private sealed class FakeCadException : Exception
        {
            public FakeCadException(string status) : base(status) { ErrorStatus = status; }
            public string ErrorStatus { get; }
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
