# Báo lỗi (Bug Report) — Kế hoạch triển khai

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Người dùng gửi báo lỗi (mô tả + ảnh cửa sổ CAD + nhật ký + stack trace + file tuỳ chọn) từ CAD lên Google Sheet/Drive; máy admin xem và đánh dấu đã xử lý ngay trong tab Info.

**Architecture:** Logic thuần (model, nhật ký xoay vòng, hàng chờ, nén/giới hạn đính kèm, chống hỏi trùng) ở `_system/OhtaTool.Plugin/Core` — được link nguồn vào `OhtaTool.Tests`. Gọi mạng tách ra `Services/SheetWebApp.cs` dùng chung với Fname. UI WPF dựng bằng code (giống `InfoTabControl.cs`). GROTOOL.dll gọi vào OHTATOOL.dll bằng phản chiếu; lisp gọi qua `vl-catch-all-apply`.

**Tech Stack:** C# (net48 cho ZWCAD 2026 + net10.0-windows cho AutoCAD 2027), WPF, `System.IO.Compression`, Google Apps Script, AutoLISP.

**Spec:** [_docs/specs/2026-11-05-bug-report.md](../specs/2026-11-05-bug-report.md)

## Global Constraints

- Build kép: mọi code phải biên dịch cho cả `net48` (define `ZWCAD`) và `net10.0-windows` (define `AUTOCAD`); kiểu CAD dùng alias trong `CadCompat.cs`.
- Không thêm gói NuGet mới (ADR 0006: tránh xung đột DLL trong CAD). JSON dùng `JsonLite`.
- Thông báo `princ` trong lisp: tiếng Việt **không dấu**. Chuỗi C#/WPF có dấu được.
- Mọi tương tác Database từ UI modeless bọc `using (doc.LockDocument())`.
- Không thư mục mới ở gốc `OHTA_TOOL/` không có tiền tố `_` (sẽ thành tab).
- Repo public: không commit khoá, không ghi `write_key` vào log hay báo lỗi.
- Giới hạn đính kèm: zip ≤ `25 * 1024 * 1024` byte. Nhật ký gửi kèm: 200 dòng cuối. File nhật ký xoay vòng ở 1 MB, giữ 3 file.
- Mức độ: `Chặn việc` / `Phiền` / `Nhỏ` (mặc định `Phiền`).
- Phiên bản: `VERSION` 1.3.0; `03_Guro/module.json` `"version": "1.6"`.
- Hoàn tất: `powershell -ExecutionPolicy Bypass -File build_ohta.ps1` xanh (2 bản build + OhtaTool.Tests + GroTool.Tests).

## Review Focus

1. **Máy chạy bản cũ hơn `phien_ban_toi_thieu`** — `Post` hiện chặn mọi action ghi; báo lỗi phải vẫn gửi được (Task 4 có test cho `SheetWebApp.RequiresVersionGate("submit_bug") == false`).
2. **Nhật ký/stack trace dài > 50.000 ký tự** (giới hạn 1 ô Sheet) — chi tiết đi vào file .txt trên Drive, ô `Loi` cắt ≤ 1000 ký tự (Task 1 test `ShortError`).
3. **Lỗi xảy ra trong chính luồng báo lỗi** (chụp ảnh lỗi, nhật ký bị khoá, form crash) — không được bật hộp báo lỗi lồng nhau hay làm sập CAD (Task 6 test cờ `BugDedup` chặn tái nhập; mọi đường chụp/ghi log nuốt exception).
4. **Gửi lại hàng chờ hai lần** (mở bảng OHTA 2 lần nhanh, hoặc Apps Script đã nhận nhưng trả lỗi timeout) — không ra 2 dòng trên Sheet: Apps Script bỏ qua ID đã có (Task 4), client giữ ID cố định trong hàng chờ (Task 3 test).
5. **Bản vẽ chưa lưu lần nào / đang mở bởi CAD** — `.dwg` chưa có trên đĩa thì ô tick bị tắt; file đang khoá thì đọc bằng `FileShare.ReadWrite` (Task 3 test zip file đang mở ghi).

---

### Task 1: Model báo lỗi + chống hỏi trùng

**Files:**
- Create: `_system/OhtaTool.Plugin/Core/BugReport.cs`
- Create: `_system/OhtaTool.Tests/Program.Bug.cs`
- Modify: `_system/OhtaTool.Tests/Program.cs` (thêm `TestBugReport();` trước `Console.WriteLine($"=== KET QUA`)

**Interfaces:**
- Produces:
  - `public sealed class BugReport { string Id; DateTime ThoiGian; string NguoiGui; string MucDo; string TabLenh; string MoTa; string PhienBan; string Loi; string ChiTiet; byte[] AnhJpeg; byte[] DinhKemZip; string DinhKemTen; }`
  - `static BugReport BugReport.Create()` — `Id` = 12 ký tự hex đầu của `Guid.NewGuid().ToString("N")`, `ThoiGian = DateTime.Now`, `NguoiGui = MachineName + " / " + UserName`, `MucDo = "Phiền"`.
  - `static readonly string[] BugReport.MucDoList = { "Chặn việc", "Phiền", "Nhỏ" }`
  - `string BugReport.Validate()` — `null` nếu hợp lệ, ngược lại câu lỗi tiếng Việt.
  - `static string BugReport.ShortError(string s)` — dòng đầu, cắt 1000 ký tự.
  - `Dictionary<string,string> BugReport.ToFields()` — khoá: `id, thoi_gian (yyyy-MM-dd HH:mm:ss), nguoi_gui, muc_do, tab_lenh, mo_ta, phien_ban, loi, chi_tiet, anh (base64, có khi AnhJpeg != null), dinh_kem (base64), dinh_kem_ten`.
  - `string BugReport.ToJson()` / `static BugReport BugReport.FromJson(string)` (cho hàng chờ; byte[] dạng base64).
  - `static class BugDedup { static bool ShouldAsk(string nguon, Exception ex); static bool ShouldAsk(string nguon, string loi); static bool InReport { get; set; } static void ResetForTest(); }` — khoá = `nguon + "|" + kiểu + "|" + ShortError(message)`; trả false nếu đã hỏi khoá đó trong phiên **hoặc** `InReport == true`.

- [ ] **Step 1: Viết test hỏng** `TestBugReport()` trong `Program.Bug.cs`:

```csharp
var r = BugReport.Create();
Check("Bug: Id 12 ky tu", r.Id.Length == 12);
Check("Bug: muc do mac dinh Phien", r.MucDo == "Phiền");
Check("Bug: mo ta rong khong hop le", r.Validate() != null);
r.MoTa = "  "; Check("Bug: mo ta trang khong hop le", r.Validate() != null);
r.MoTa = "GRO treo"; Check("Bug: hop le", r.Validate() == null);
r.MucDo = "Lung tung"; Check("Bug: muc do la khong hop le", r.Validate() != null); r.MucDo = "Nhỏ";
Check("Bug: ShortError cat 1000", BugReport.ShortError(new string('x', 5000)).Length == 1000);
Check("Bug: ShortError lay dong dau", BugReport.ShortError("a\r\nb") == "a");
var f = r.ToFields();
Check("Bug: fields khong co anh khi null", !f.ContainsKey("anh"));
r.AnhJpeg = new byte[] { 1, 2, 3 };
Check("Bug: anh base64", r.ToFields()["anh"] == "AQID");
var back = BugReport.FromJson(r.ToJson());
Check("Bug: json giu Id + anh", back.Id == r.Id && back.AnhJpeg.Length == 3 && back.MoTa == "GRO treo");
BugDedup.ResetForTest();
var ex = new InvalidOperationException("hong\nchi tiet");
Check("Bug: hoi lan dau", BugDedup.ShouldAsk("GRO", ex));
Check("Bug: khong hoi lai cung loi", !BugDedup.ShouldAsk("GRO", new InvalidOperationException("hong\nkhac")));
Check("Bug: hoi loi khac", BugDedup.ShouldAsk("GRO", new ArgumentException("hong")));
BugDedup.InReport = true;
Check("Bug: khong hoi long khi dang bao loi", !BugDedup.ShouldAsk("OHTA", "loi moi"));
BugDedup.InReport = false;
```

- [ ] **Step 2: Chạy, xác nhận hỏng**: `dotnet run --project _system/OhtaTool.Tests` → lỗi biên dịch `BugReport` không tồn tại.
- [ ] **Step 3: Viết `BugReport.cs`** theo Interfaces (JSON qua `JsonLite`).
- [ ] **Step 4: Chạy lại** → mọi dòng `Bug:` PASS, `0 FAIL`.
- [ ] **Step 5: Commit** `git commit -m "Bao loi: model BugReport + chong hoi trung (BugDedup)"`

---

### Task 2: Nhật ký xoay vòng + hàm lisp `ohta_net_log`

**Files:**
- Create: `_system/OhtaTool.Plugin/Core/BugLog.cs`
- Create: `_system/OhtaTool.Plugin/BugCommands.cs` (lớp `BugCommands`, chỉ phần LispFunction ở task này)
- Test: `_system/OhtaTool.Tests/Program.Bug.cs` (thêm `TestBugLog()`, gọi từ `TestBugReport()`)

**Interfaces:**
- Produces:
  - `static class BugLog { static string Directory { get; set; } /* mặc định %LOCALAPPDATA%\OHTA_TOOL\logs */ static void Write(string nguon, string message); static void Write(string nguon, Exception ex); static List<string> Tail(int lines); const long MaxBytes = 1024*1024; const int KeepFiles = 3; }`
  - File: `ohta.log`, xoay thành `ohta.1.log`, `ohta.2.log` (bỏ cũ hơn). Dòng: `yyyy-MM-dd HH:mm:ss [nguon] message`. `Write` không bao giờ ném exception. `Tail` đọc nối `ohta.1.log` + `ohta.log` khi file hiện tại ít hơn `lines` dòng, mở bằng `FileShare.ReadWrite`.
  - `[LispFunction("OHTA_NET_LOG")] (ohta_net_log "nguon" "noi dung")` → trả `T`.

- [ ] **Step 1: Test hỏng** (thư mục tạm riêng):

```csharp
BugLog.Directory = Path.Combine(Path.GetTempPath(), "ohta_buglog_" + Guid.NewGuid().ToString("N"));
for (int i = 0; i < 300; i++) BugLog.Write("GRO", "dong " + i);
var tail = BugLog.Tail(200);
Check("BugLog: tail 200 dong", tail.Count == 200);
Check("BugLog: dong cuoi dung", tail[199].EndsWith("[GRO] dong 299"));
BugLog.Write("X", new string('a', 600_000)); BugLog.Write("X", new string('b', 600_000));
BugLog.Write("X", "sau xoay");
Check("BugLog: da xoay", File.Exists(Path.Combine(BugLog.Directory, "ohta.1.log")));
Check("BugLog: toi da 3 file", Directory.GetFiles(BugLog.Directory, "ohta*.log").Length <= 3);
Check("BugLog: tail qua ranh gioi file", BugLog.Tail(3).Count == 3);
using (new FileStream(Path.Combine(BugLog.Directory, "ohta.log"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    BugLog.Write("X", "file dang khoa"); // khong nem
Check("BugLog: ghi khi file khoa khong nem", true);
```

- [ ] **Step 2: Chạy** → hỏng biên dịch.
- [ ] **Step 3: Viết `BugLog.cs`** và `BugCommands.OhtaNetLog(ResultBuffer)` (đọc 2 chuỗi đầu, thiếu thì `""`).
- [ ] **Step 4: Chạy** → PASS.
- [ ] **Step 5: Commit** `"Bao loi: nhat ky xoay vong BugLog + ohta_net_log"`

---

### Task 3: Đính kèm (zip, giới hạn 25 MB) + hàng chờ

**Files:**
- Create: `_system/OhtaTool.Plugin/Core/BugAttachments.cs`
- Create: `_system/OhtaTool.Plugin/Core/BugQueue.cs`
- Test: `Program.Bug.cs` (`TestBugAttachments()`, `TestBugQueue()`)

**Interfaces:**
- Consumes: `BugReport.ToJson/FromJson` (Task 1).
- Produces:
  - `static class BugAttachments { const long MaxZipBytes = 25L*1024*1024; static (byte[] zip, string error) Zip(IList<string> paths, long maxBytes = MaxZipBytes); }` — bỏ qua path không tồn tại; đọc từng file bằng `FileShare.ReadWrite | FileShare.Delete`; vượt `maxBytes` → `zip = null`, `error = "File đính kèm sau khi nén {x} MB, quá giới hạn 25 MB. Hãy gửi tay qua Zalo/OneDrive."`; danh sách rỗng → `(null, null)`.
  - `static class BugQueue { static string Directory { get; set; } /* %LOCALAPPDATA%\OHTA_TOOL\bug_queue */ static void Enqueue(BugReport r); static List<BugReport> Pending(); static void Remove(string id); }` — 1 file `<id>.json`; `Enqueue` cùng Id ghi đè (không nhân đôi); file hỏng thì bỏ qua (không ném).

- [ ] **Step 1: Test hỏng:**

```csharp
string d = Path.Combine(Path.GetTempPath(), "ohta_bugatt_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(d);
string a = Path.Combine(d, "a.csv"); File.WriteAllText(a, "x,y\n1,2");
var (zip, err) = BugAttachments.Zip(new[] { a, Path.Combine(d, "khong_co.dwg") });
Check("BugAtt: zip co du lieu", zip != null && zip.Length > 0 && err == null);
using (var fs = new FileStream(a, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
    Check("BugAtt: zip file dang mo ghi", BugAttachments.Zip(new[] { a }).zip != null);
var (z2, e2) = BugAttachments.Zip(new[] { a }, maxBytes: 10);
Check("BugAtt: vuot gioi han bi chan", z2 == null && e2.Contains("25 MB"));
Check("BugAtt: rong tra null", BugAttachments.Zip(new string[0]).zip == null);

BugQueue.Directory = Path.Combine(d, "queue");
var r = BugReport.Create(); r.MoTa = "test";
BugQueue.Enqueue(r); BugQueue.Enqueue(r);
Check("BugQueue: cung Id khong nhan doi", BugQueue.Pending().Count == 1);
Check("BugQueue: giu Id", BugQueue.Pending()[0].Id == r.Id);
File.WriteAllText(Path.Combine(BugQueue.Directory, "hong.json"), "{khong phai json");
Check("BugQueue: bo qua file hong", BugQueue.Pending().Count == 1);
BugQueue.Remove(r.Id);
Check("BugQueue: xoa", BugQueue.Pending().Count == 0);
```

- [ ] **Step 2: Chạy** → hỏng.
- [ ] **Step 3: Viết 2 file** (`ZipArchive` trong `MemoryStream`, `CompressionLevel.Optimal`; tên entry = tên file, trùng thì thêm hậu tố `_2`).
- [ ] **Step 4: Chạy** → PASS.
- [ ] **Step 5: Commit** `"Bao loi: nen dinh kem 25 MB + hang cho gui lai"`

---

### Task 4: Gửi lên Sheet (C# + Apps Script)

**Files:**
- Create: `_system/OhtaTool.Plugin/Services/SheetWebApp.cs` — tách phần thân `FnameSheetWriter.Post` (đọc cấu hình, form-encode với `EscapeLong`, HttpClient, parse `FnameSheetReply`).
- Modify: `_system/OhtaTool.Plugin/Services/FnameSheetWriter.cs:263-322` — `Post` gọi `SheetWebApp.Post`, hành vi Fname giữ nguyên.
- Create: `_system/OhtaTool.Plugin/Services/BugReportService.cs`
- Create: `_system/OhtaTool.Plugin/Core/BugAdminItem.cs` (lớp `BugAdminItem` + lớp tĩnh `SheetActions`; nằm trong Core nên test project tự link, không sửa csproj)
- Modify: `01_Fname/data/google_sheet_apps_script.gs` — thêm `case 'submit_bug' / 'list_bugs' / 'resolve_bug'` trong `doPost` và các hàm dưới.
- Test: `Program.Bug.cs` (`TestBugSheet()`)

**Interfaces:**
- Consumes: `BugReport.ToFields()`, `BugQueue` (Task 1, 3); `FnameDataSync.ReadConfig/ConfigPath/KeyPath` (có sẵn).
- Produces:
  - `static class SheetActions { static bool RequiresVersionGate(string action); }` — `false` cho `list_requests, my_requests, submit_bug, list_bugs, resolve_bug`; `true` còn lại.
  - `static FnameSheetReply SheetWebApp.Post(string fnameModuleDir, string action, Dictionary<string,string> fields, int timeoutSeconds, out string error)` — kiểm `SheetActions.RequiresVersionGate` trước khi chặn bản cũ.
  - `sealed class BugAdminItem { string Id, ThoiGian, NguoiGui, MucDo, TabLenh, MoTa, PhienBan, Loi, Anh, ChiTiet, DinhKem; static List<BugAdminItem> ParseList(string json); }` — đọc mảng `bugs` trong JSON trả về.
  - `static class BugReportService { static string FnameDir { get; } /* <root>\01_Fname */ static (bool ok, string message) Submit(BugReport r); static int ResendQueued(); static (bool ok, bool isAdmin, List<BugAdminItem> bugs, string message) ListBugs(); static (bool ok, string message) Resolve(string id, string banSua); }`
    - `Submit`: timeout 300 s nếu có `DinhKemZip`, 60 s nếu có ảnh, 30 s còn lại. Lỗi → `BugQueue.Enqueue(r)`, message `"Chưa gửi được (…). Báo lỗi đã lưu, sẽ tự gửi lại lần mở bảng OHTA sau."`. Thành công → `BugQueue.Remove(r.Id)`.
    - `ResendQueued`: gửi lần lượt, trả số đã gửi; chạy trên thread nền, không ném.
  - Apps Script:
    - Tab `bug_report` (tự tạo) cột: `ID | Thoi_gian | May_gui | Nguoi_gui | Muc_do | Tab_lenh | Mo_ta | Phien_ban | Loi | Anh | Chi_tiet | Dinh_kem`. Tab `bug_done` = cùng cột + `Ngay_xu_ly | Ban_sua`.
    - `submitBug_(p, ctx)`: nếu `ID` đã có ở `bug_report` hoặc `bug_done` → `{ok:true, message:'Đã nhận trước đó.'}`. Lưu vào thư mục Drive `OHTA Bug Report` (tạo nếu chưa có): `<id>.jpg` và `<id>_chi_tiet.txt` → chia sẻ *Bất kỳ ai có link – Xem*; `<id>_<dinh_kem_ten>.zip` → **không chia sẻ** (chỉ chủ Drive mở được). Ghi link vào cột. Gửi `MailApp.sendEmail(Session.getEffectiveUser().getEmail(), '[OHTA Bug][' + muc_do + '] ' + tab_lenh, mo_ta + '\n\nMáy: ' + nguoi_gui + '\nSheet: ' + ss.getUrl())` trong `try` (lỗi mail không làm hỏng việc ghi).
    - `listBugs_(ctx)`: không phải admin → `{ok:true,isAdmin:false,bugs:[]}`; admin → mọi dòng `bug_report`, mới nhất trước.
    - `resolveBug_(p, ctx)`: chỉ admin; chuyển dòng sang `bug_done` + `Ngay_xu_ly` + `Ban_sua`, `setTrashed(true)` file zip; giữ ảnh + chi tiết.
    - Chuỗi người dùng nhập ghi bằng `setText_` (chặn công thức `=`).

- [ ] **Step 1: Test hỏng:**

```csharp
Check("Sheet: submit_bug khong bi chan ban cu", !SheetActions.RequiresVersionGate("submit_bug"));
Check("Sheet: resolve_bug khong bi chan", !SheetActions.RequiresVersionGate("resolve_bug"));
Check("Sheet: submit_request van bi chan", SheetActions.RequiresVersionGate("submit_request"));
var list = BugAdminItem.ParseList("{\"ok\":true,\"isAdmin\":true,\"bugs\":[{\"ID\":\"abc\",\"Muc_do\":\"Nhỏ\",\"Mo_ta\":\"x\",\"Anh\":\"\"}]}");
Check("Sheet: parse bugs", list.Count == 1 && list[0].Id == "abc" && list[0].MucDo == "Nhỏ" && list[0].Anh == "");
Check("Sheet: parse rong", BugAdminItem.ParseList("{\"ok\":true,\"bugs\":[]}").Count == 0);
Check("Sheet: parse json hong", BugAdminItem.ParseList("<html>").Count == 0);
```

- [ ] **Step 2: Chạy** → hỏng.
- [ ] **Step 3: Viết `SheetActions` + `BugAdminItem`, tách `SheetWebApp`, viết `BugReportService`, thêm Apps Script.**
- [ ] **Step 4: Chạy** `dotnet run --project _system/OhtaTool.Tests` → PASS (test Fname cũ vẫn PASS).
- [ ] **Step 5: Kiểm tay Apps Script**: dán script mới, *Triển khai → Quản lý → Sửa → Phiên bản mới*; chạy `capQuyenDrive` + gửi thử 1 mail để cấp quyền `MailApp`. Gọi `submit_bug` 2 lần cùng `id` → Sheet chỉ 1 dòng, mail tới.
- [ ] **Step 6: Commit** `"Bao loi: gui len Google Sheet/Drive (submit_bug, list_bugs, resolve_bug)"`

---

### Task 5: Form báo lỗi, chụp cửa sổ CAD, lệnh `OHTABUG`, nút tab Info

**Files:**
- Create: `_system/OhtaTool.Plugin/Services/CadScreenshot.cs`
- Create: `_system/OhtaTool.Plugin/Views/BugReportWindow.cs`
- Modify: `_system/OhtaTool.Plugin/BugCommands.cs` (thêm lệnh)
- Modify: `_system/OhtaTool.Plugin/Views/InfoTabControl.cs:105-110` (thêm nút `🐞  Báo lỗi` cạnh 2 nút cập nhật)
- Modify: `_system/OhtaTool.Plugin/OhtaPaletteCommands.cs` (sau khi mở bảng: `Task.Run(BugReportService.ResendQueued)`; >0 thì `ed.WriteMessage("\nDa gui N bao loi con cho.")` qua `Application.Idle` một lần)

**Interfaces:**
- Consumes: Task 1–4; `ToolInfoCore` (nội dung như `OHTAINFO`) để dựng `PhienBan` + đầu `ChiTiet`.
- Produces:
  - `static byte[] CadScreenshot.CaptureMainWindowJpeg()` — `GetWindowRect(Application.MainWindow.Handle)` (P/Invoke `user32`), `Graphics.CopyFromScreen`, thu nhỏ để cạnh dài ≤ 1600 px, JPEG chất lượng 70; mọi lỗi → `null`.
  - `static void BugReportWindow.Show(string tabLenh, string loi, string stackTrace, byte[] anh)` — modal với cửa sổ CAD. Gồm: ô Mô tả (nhiều dòng), Tab/lệnh, ComboBox Mức độ (`BugReport.MucDoList`), Người gửi, ảnh xem trước + tick "Gửi kèm ảnh" (tắt nếu `anh == null`), 3 tick đính kèm (`Bản vẽ đang mở (.dwg)` — tắt nếu `Document.Name` chưa có trên đĩa; `CSV vừa xuất`; `gro_thong_tin.txt` — tắt nếu file không có), nút Gửi / Huỷ. Gửi: `Validate()`, `BugAttachments.Zip` (lỗi hiện ngay, không đóng form), `ChiTiet = thông tin phiên bản + "\n\n--- LOI ---\n" + stackTrace + "\n\n--- NHAT KY ---\n" + string.Join("\n", BugLog.Tail(200))`, gọi `Submit` trên thread nền, hiện kết quả. Đặt `BugDedup.InReport = true` khi mở, `false` khi đóng (cả khi lỗi).
  - Đường dẫn CSV vừa xuất: đọc registry `HKCU\Software\OFT_Lisp\GRO` giá trị `csv_cuoi` nếu có; không có thì tắt tick. (Nếu GRO chưa ghi giá trị này, thêm `(vl-registry-write ... "csv_cuoi" duong-dan)` ở chỗ GRO ghi CSV trong Task 6.)
  - `[CommandMethod("OHTABUG")]` — chụp ảnh **trước**, rồi `BugReportWindow.Show(tabDangMo, null, null, anh)`. Tab đang mở lấy từ palette (`OhtaPaletteCommands` có tab hiện hành), không có thì `"OHTA"`.

- [ ] **Step 1: Build** `powershell -ExecutionPolicy Bypass -File build_ohta.ps1` → xanh.
- [ ] **Step 2: Kiểm tay trên ZWCAD 2026 và AutoCAD 2027**: gõ `OHTABUG` → ảnh xem trước là cả cửa sổ CAD (thấy bảng OHTA + dòng lệnh), không thấy form; gửi khi mô tả rỗng → báo lỗi, không đóng; bản vẽ chưa lưu → tick .dwg tắt; gửi thật → dòng mới trên `bug_report`, ảnh mở được, `chi_tiet.txt` có 200 dòng nhật ký; rút mạng gửi → báo đã lưu hàng chờ; cắm mạng, `OHTA` → "Da gui 1 bao loi con cho", Sheet chỉ 1 dòng.
- [ ] **Step 3: Commit** `"Bao loi: form OHTABUG, chup cua so CAD, nut tab Info, gui lai hang cho"`

---

### Task 6: Tự bật khi gặp lỗi (C# OHTATOOL + GROTOOL + lisp GRO)

**Files:**
- Create: `_system/OhtaTool.Plugin/Services/BugGuard.cs`
- Modify: các `[CommandMethod]` trong `_system/OhtaTool.Plugin/OhtaPaletteCommands.cs`, `OhtaUpdateCommands.cs`, `PrintCommands.cs` — bọc thân bằng `BugGuard.Run("<TÊN LỆNH>", () => { ... })`.
- Create: `03_Guro/plugin/GroTool.Plugin/OhtaBugBridge.cs`
- Modify: các `[CommandMethod]` công khai cho người dùng trong `03_Guro/plugin/GroTool.Plugin/` (`DST`, `GROPALETTE`, `GROSETTING`, `GROCS` và các lệnh trong `GroBuocCommands`, `DatMaKhacCommands`) — bọc `OhtaBugBridge.Run("<TÊN>", ...)`. Bỏ qua lệnh demo/test (`CS_*`, `TESTNET`).
- Modify: `03_Guro/lisp/guro_gro.lsp:13819-13840` (`*error*` của `c:grochay`) và đầu/cuối `c:grochay`.
- Test: `Program.Bug.cs` (`TestBugGuard()`)

**Interfaces:**
- Consumes: `BugDedup`, `BugLog`, `CadScreenshot`, `BugReportWindow` (Task 1, 2, 5).
- Produces:
  - `static class BugGuard { static void Run(string lenh, Action body); static void ReportException(string nguon, Exception ex); static void ReportLispError(string nguon, string loi); internal static Func<string, string, bool> AskUser; /* thay được trong test */ }`
    - `Run`: `BugLog.Write(lenh, "bat dau")`; `catch (Exception ex)` → `ReportException(lenh, ex)` rồi **không** ném lại (CAD đã có dòng lỗi trong nhật ký). Không bắt khi ex là lỗi huỷ của CAD (`Exception` CAD có `ErrorStatus.UserBreak`) — chỉ ghi log.
    - `ReportException`: luôn `BugLog.Write(nguon, ex)`; nếu `BugDedup.ShouldAsk(nguon, ex)` → chụp ảnh ngay → `AskUser(nguon, ShortError)` (MessageBox Có/Không "Lệnh {nguon} gặp lỗi: … Gửi báo lỗi cho admin?") → Có thì `BugReportWindow.Show(nguon, ShortError(ex.Message), ex.ToString(), anh)`. Toàn bộ bọc `try/catch` rỗng.
    - `ReportLispError`: như trên nhưng `stackTrace = loi`.
  - `[LispFunction("OHTA_NET_BUG_AUTO")] (ohta_net_bug_auto "GRO" "Loi: ... (dang o buoc: ...)")` → gọi `ReportLispError`, trả `T`.
  - `OhtaBugBridge` (GROTOOL): `static void Run(string lenh, Action body)` — `catch` → tìm assembly đã nạp tên `OHTATOOL`, kiểu `OhtaTool.Plugin.Services.BugGuard`, gọi `ReportException(string, Exception)` bằng phản chiếu; không tìm thấy → `ed.WriteMessage` lỗi như cũ. Hợp đồng chữ ký ghi trong ADR 0007.
  - Lisp `*error*` của `c:grochay`, trong nhánh lỗi thật (sau `princ` "dang o buoc"):

```lisp
(vl-catch-all-apply 'ohta_net_bug_auto
  (list "GRO" (strcat m " (buoc: " (cond ((eq (type gro--buoc) 'STR) gro--buoc) ("?")) ")")))
```

    Đầu `c:grochay`: `(vl-catch-all-apply 'ohta_net_log (list "GRO" "GROCHAY bat dau"))`; cuối khi thành công: `... "GROCHAY xong"`. Mỗi lần gán `gro--buoc` không thêm log (tránh sửa ~nhiều chỗ) — `*error*` đã kèm bước.

- [ ] **Step 1: Test hỏng:**

```csharp
BugDedup.ResetForTest(); int hoi = 0;
BugGuard.AskUser = (n, l) => { hoi++; return false; };
BugGuard.Run("TEST", () => throw new InvalidOperationException("no"));
Check("Guard: khong nem ra ngoai + hoi 1 lan", hoi == 1);
BugGuard.Run("TEST", () => throw new InvalidOperationException("no"));
Check("Guard: cung loi khong hoi lai", hoi == 1);
Check("Guard: loi da vao nhat ky", BugLog.Tail(50).Any(l => l.Contains("[TEST]") && l.Contains("InvalidOperationException")));
BugDedup.InReport = true; BugGuard.ReportLispError("GRO", "bad argument type");
Check("Guard: khong hoi long", hoi == 1); BugDedup.InReport = false;
BugGuard.AskUser = (n, l) => throw new Exception("UI hong");
BugGuard.ReportLispError("GRO", "loi khac"); Check("Guard: loi trong luong bao loi khong nem", true);
```

  (`CadScreenshot` gọi trong test phải trả `null` an toàn khi không có CAD — `BugGuard` gọi qua `internal static Func<byte[]> Capture` thay được trong test.)
- [ ] **Step 2: Chạy** → hỏng.
- [ ] **Step 3: Viết `BugGuard`, `OHTA_NET_BUG_AUTO`, `OhtaBugBridge`, bọc lệnh, sửa lisp.**
- [ ] **Step 4: Kiểm ngoặc lisp**: chạy script kiểm cân bằng ngoặc theo `03_Guro/rules/15-phuong-phap-debug.md` → cân bằng.
- [ ] **Step 5: Build + test** `build_ohta.ps1` → xanh.
- [ ] **Step 6: Kiểm tay ZWCAD + AutoCAD**: `GROCHAY` rồi Esc → không hỏi; tạo lỗi thật (bản vẽ thiếu layer bắt buộc hoặc tạm `(car 1)` trong bản thử) → hỏi 1 lần, form điền sẵn lỗi + bước; chạy lại cùng lỗi → không hỏi; bảng OHTA chưa nạp (chỉ nạp GRO) → `vl-catch-all-apply` không làm hỏng `*error*`.
- [ ] **Step 7: Commit** `"Bao loi: tu bat khi lenh C#/GRO gap loi (BugGuard, ohta_net_bug_auto)"`

---

### Task 7: Cửa sổ admin trên tab Info

**Files:**
- Create: `_system/OhtaTool.Plugin/Views/BugAdminWindow.cs`
- Modify: `_system/OhtaTool.Plugin/Views/InfoTabControl.cs` (card "🐞 Báo lỗi (N)" chỉ hiện khi `isAdmin`)

**Interfaces:**
- Consumes: `BugReportService.ListBugs/Resolve`, `BugAdminItem` (Task 4).
- Produces:
  - Info tab: khi dựng, gọi `ListBugs()` trên thread nền; `isAdmin == false` hoặc lỗi mạng → không hiện card (không báo lỗi gì). Admin → card có nút `🐞 Báo lỗi (N)`.
  - `BugAdminWindow` (không modal): DataGrid cột Thời gian / Mức độ / Tab-lệnh / Người gửi / Mô tả (xếp `Chặn việc` trước rồi mới nhất); khung chi tiết hiện Mô tả đầy đủ + Lỗi + Phiên bản; nút `Mở ảnh`, `Mở nhật ký`, `Mở file đính kèm` (`Process.Start(url)`, tắt khi link rỗng), `✓ Đã xử lý` (hỏi "Bản đã sửa (vd 1.3.1), để trống nếu không có") → `Resolve` → bỏ dòng khỏi lưới, cập nhật N trên tab Info; `⟳ Làm mới`.

- [ ] **Step 1: Build** `build_ohta.ps1` → xanh.
- [ ] **Step 2: Kiểm tay**: máy không có trong tab `admin` → không thấy card; thêm tên máy vào `admin` → card hiện đúng N; Mở ảnh/nhật ký mở được trên trình duyệt; Mở file đính kèm cần đăng nhập Google chủ Drive; `Đã xử lý` → dòng sang `bug_done` có `Ngay_xu_ly`, `Ban_sua`, zip vào thùng rác Drive, ảnh + nhật ký còn.
- [ ] **Step 3: Commit** `"Bao loi: cua so admin tren tab Info (xem, mo file, da xu ly)"`

---

### Task 8: Tài liệu, ADR 0007, phiên bản

**Files:**
- Create: `_docs/adr/0007-bao-loi-qua-google-sheet.md` — quyết định: Sheet+Drive qua Apps Script Fname (không GitHub Issues vì repo public), lõi trong OHTATOOL, hợp đồng phản chiếu `OhtaTool.Plugin.Services.BugGuard.ReportException(string, Exception)` cho GROTOOL, lisp `ohta_net_log` / `ohta_net_bug_auto` gọi qua `vl-catch-all-apply`, zip đính kèm không chia sẻ công khai; Considered Options: GitHub Issues, email SMTP, thư mục OneDrive.
- Modify: `GEMINI.md` §3 (thêm `OHTABUG`) và bảng tab Info (§4 dòng "cuối").
- Modify: `README.md` gốc nếu có bảng lệnh.
- Modify: `01_Fname/README.md` mục Apps Script: thêm 3 action mới + bước cấp quyền `MailApp`.
- Modify: `03_Guro/README.md` + rule Guro phù hợp (`03_Guro/rules/12-chan-doan-bao-loi.md`) thêm **mục 209**: `*error*` của `c:grochay` gọi `ohta_net_bug_auto`; mục lục `00-muc-luc-guro.md`.
- Modify: `VERSION` → `1.3.0`; `03_Guro/module.json` `"version": "1.6"`; `CHANGELOG.md` mục đầu `## v1.3.0` (ghi cho người dùng xưởng: "Thêm nút 🐞 Báo lỗi trên tab Info và lệnh OHTABUG…").

- [ ] **Step 1: Viết các file trên.**
- [ ] **Step 2: Build cuối** `powershell -ExecutionPolicy Bypass -File build_ohta.ps1` → 2 bản build + OhtaTool.Tests + GroTool.Tests đều PASS.
- [ ] **Step 3: Commit** `"Bao loi: ADR 0007, tai lieu, OHTA TOOL v1.3.0 / Guro v1.6"`

Phát hành (`release_ohta.ps1`) do người dùng chạy sau khi kiểm trên CAD — không nằm trong kế hoạch này.
