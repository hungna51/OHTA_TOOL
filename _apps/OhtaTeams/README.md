# OhtaTeams.exe — ứng dụng xem đơn Teams chạy ngoài CAD

Thiết kế: [_docs/specs/2026-11-10-ung-dung-teams-rieng.md](../../_docs/specs/2026-11-10-ung-dung-teams-rieng.md).

- Mở `_apps\OhtaTeams\bin\OhtaTeams.exe` (không cần mở CAD). Màn hình giống hệt tab Teams: đăng nhập Microsoft 365 → nhóm → kênh → tab Lists → đơn.
- Dùng chung `06_Teams\data\teams.json`, phiên đăng nhập và bản lưu tạm với tab Teams → máy đã đăng nhập trên CAD không phải đăng nhập lại.
- Exe tìm `06_Teams` bằng cách đi ngược lên từ thư mục của nó, nên phải nằm trong thư mục OHTA_TOOL.

## Mã

Bước 1 chưa có mã riêng ngoài `Program.cs` (cửa sổ chứa `TeamsTabControl`). Core / Services / Views lấy bằng **link nguồn** từ `06_Teams\plugin\TeamsTool.Plugin` → sửa ở đó là cả tab CAD lẫn ứng dụng cùng nhận. WPF net48, không gói NuGet nào chạy kèm.

Build: `powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1` (hoặc `dotnet build _apps\OhtaTeams -c Release`). Đang mở OhtaTeams thì tắt trước khi build. `release_ohta.ps1` đưa `OhtaTeams.exe` + `.exe.config` vào zip.
