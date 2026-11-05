# Báo lỗi gửi qua Google Sheet + Drive của Fname, lõi trong OHTATOOL.dll

Từ v1.3.0, người dùng gửi báo lỗi ngay trong CAD (nút 🐞 trên tab Info, lệnh `OHTABUG`, hoặc hộp tự bật khi lệnh gặp lỗi), máy admin xem và đánh dấu đã xử lý trên tab Info. Thiết kế: [_docs/specs/2026-11-05-bug-report.md](../specs/2026-11-05-bug-report.md).

- **Đích là Apps Script Web App sẵn có của Fname** (`01_Fname/data/google_sheet_apps_script.gs`, cùng `write_url` / `write_key`), thêm action `submit_bug`, `list_bugs`, `resolve_bug`. Dòng vào tab `bug_report` (xử lý xong chuyển sang `bug_done`) của một **Sheet riêng "OHTA Bug Report"** (script tự tạo, ID ở Thuộc tính tập lệnh `BUG_SPREADSHEET_ID`) — **không** phải Sheet Fname, vì Sheet Fname mở "ai có link xem" để tool tải dữ liệu và ID của nó có trong repo public. File vào thư mục Drive `OHTA Bug Report`, mỗi báo lỗi gửi 1 email tới chủ script. Admin = tên máy trong tab `admin` (như duyệt Fname).
- **Báo lỗi không bị khoá bởi `phien_ban_toi_thieu`** (`SheetActions.RequiresVersionGate`): máy bản cũ càng cần báo lỗi được.
- **Nhật ký + stack trace là file .txt trên Drive**, không ghi vào ô Sheet (giới hạn 50.000 ký tự/ô); mô tả > 20.000 ký tự cắt ở ô, bản đầy đủ vào file. **Không file nào được chia sẻ** (ảnh chụp có bản vẽ của khách): admin mở link bằng tài khoản Google chủ script, hoặc chủ script chia sẻ thư mục `OHTA Bug Report` cho email từng admin.
- **Khoá Apps Script** chỉ giữ ở đoạn kiểm trùng ID + ghi dòng; tải file lên làm ngoài khoá (zip 25 MB không chặn máy khác). Exception trong `doPost` trả `retry: true` → máy gửi giữ hàng chờ; `ok:false` không retry (dữ liệu sai) → bỏ khỏi hàng chờ, không chặn báo lỗi sau.
- **Lõi trong OHTATOOL.dll** (`Core/Bug*.cs`, test được; `Services/BugReportService`, `CadScreenshot`, `Views/BugReportWindow`, `BugAdminWindow`). Nhật ký xoay vòng `%LOCALAPPDATA%\OHTA_TOOL\logs`, hàng chờ gửi lại `%LOCALAPPDATA%\OHTA_TOOL\bug_queue` (ID do máy sinh, Apps Script bỏ qua ID đã có).
- **GROTOOL.dll gọi bằng phản chiếu** (không tham chiếu — [ADR 0001](0001-grotool-dll-tach-rieng.md)). HỢP ĐỒNG: kiểu `OhtaTool.Plugin.Core.BugGuard`, hàm tĩnh `ReportException(string nguon, Exception ex)` — ở `03_Guro/plugin/GroTool.Plugin/OhtaBugBridge.cs`. Đổi tên/chữ ký thì sửa cả hai.
- **Lisp gọi qua `vl-catch-all-apply`**: `(ohta_net_log "nguon" "noi dung")`, `(ohta_net_bug_auto "GRO" "loi (buoc: ...)")` — DLL chưa nạp thì bỏ qua, không làm hỏng `*error*`. `*error*` của `c:grochay` gọi `ohta_net_bug_auto` khi lỗi thật (không phải Esc/Cancel) — mục 209 GURO. Bản vẽ không gọi được hàm .NET (mở sau NETLOAD, như mục 114) thì lisp ghi `%TEMP%\ohta_bug_auto.txt` rồi gửi lệnh `OHTABUGAUTO`.
- **Chỉ hỏi khi exception thoát ra tới đầu lệnh** (`BugGuard.Run` bọc thân `[CommandMethod]`), cùng lỗi chỉ hỏi 1 lần mỗi phiên, không hỏi lồng khi đang trong form báo lỗi.

## Considered Options

- **GitHub Issues**: repo public nên tên bản vẽ / dữ liệu khách lộ ra ngoài; mỗi máy phải có token. Không chọn.
- **Email SMTP từ máy người dùng**: phải để mật khẩu SMTP trên máy. Không chọn (email chỉ gửi từ Apps Script).
- **Ghi file vào thư mục OneDrive chung**: không có quyền admin, không có thông báo, máy ngoài OneDrive không gửi được. Không chọn.
