# OhtaTeams.exe — ứng dụng xem đơn Teams chạy ngoài CAD

Thiết kế: [_docs/specs/2026-11-10-ung-dung-teams-rieng.md](../../_docs/specs/2026-11-10-ung-dung-teams-rieng.md).

- Mở `_apps\OhtaTeams\bin\OhtaTeams.exe` (không cần mở CAD). Màn hình giống hệt tab Teams: đăng nhập Microsoft 365 → nhóm → kênh → tab Lists → đơn.
- Dùng chung `06_Teams\data\teams.json`, phiên đăng nhập và bản lưu tạm với tab Teams → máy đã đăng nhập trên CAD không phải đăng nhập lại.
- Chi tiết đơn → **📌 Đang làm** (bấm lại để bỏ); đơn đang chọn có 📌 trong danh sách. ⚙ → **Thư mục đơn**: thư mục chứa các thư mục đơn.
- Mỗi lần tải xong đơn / bấm 📌 / lưu ⚙, ứng dụng ghi `%LOCALAPPDATA%\OHTA_TOOL\teams\dang_lam.json` (đơn chưa xong + đơn 📌, thư mục đơn, giờ ghi) — tab Teams trên CAD đọc file này (bước 3).
- Exe tìm `06_Teams` bằng cách đi ngược lên từ thư mục của nó, nên phải nằm trong thư mục OHTA_TOOL.

## Mã

`Program.cs` (cửa sổ), `Services/GraphTeams.cs` (đăng nhập, Graph, cài đặt), `Views/TeamsTabControl.cs` + `TeamsCaiDatWindow.cs` (màn duyệt đơn, ⚙). `Core/*` và `KieuTeams.cs` lấy bằng **link nguồn** ([ADR 0009](../../_docs/adr/0009-ung-dung-ohtateams-rieng.md)) từ `06_Teams\plugin\TeamsTool.Plugin` → sửa ở đó là cả tab CAD lẫn ứng dụng cùng nhận. WPF net48, không gói NuGet nào chạy kèm.

Build: `powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1` (hoặc `dotnet build _apps\OhtaTeams -c Release`). Đang mở OhtaTeams thì tắt trước khi build. `release_ohta.ps1` đưa `OhtaTeams.exe` + `.exe.config` vào zip.
