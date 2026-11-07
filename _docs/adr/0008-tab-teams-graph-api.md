# Tab Teams đọc đơn qua Microsoft Graph, DLL riêng TEAMSTOOL.dll, đăng nhập bằng mã thiết bị

Từ 11/2026, công ty nhận và giao đơn qua Microsoft Teams. Đơn **không nằm trong tin nhắn** mà trong **Microsoft Lists** gắn làm tab của kênh (mỗi dòng một đơn: Title, Người triển khai, Người check, Trạng thái, Hạn hoàn thành…). Tab **Teams** (mục con `06_Teams`) đi Nhóm → Kênh → Tab → Đơn ngay trong CAD qua **Microsoft Graph** (`/me/joinedTeams`, `/teams/{id}/channels`, `.../tabs`, `/sites/{host}:{path}`, `/sites/{id}/lists`, `.../columns`, `.../items?$expand=fields`). Giao diện và logic nằm trong **TEAMSTOOL.dll**, nạp bằng `DllTabLoader` với cùng hợp đồng `TeamsTool.TeamsOhtaTab.TaoGiaoDien(tieuDe, moTa, mauNhan, thuMucModule)` như tab Guro / AI ([ADR 0003](0003-tab-guro-nam-trong-grotool.md), [ADR 0006](0006-tab-ai-dll-rieng.md)).

- **Tab Lists nhận ra bằng URL `…/Lists/…`** trong cấu hình tab (contentUrl / websiteUrl, có thể mã hoá nhiều lớp), rồi khớp list theo `webUrl` — không phụ thuộc id ứng dụng Teams của Lists. Cột chính nhận theo **tên hiển thị** bỏ dấu, vì tên nội bộ của SharePoint đặt tuỳ ý.
- **Chỉ xem.** Sửa đơn cần `Sites.ReadWrite.All`; để sau. File đính kèm của dòng Graph v1.0 không đọc được → nút "Mở trên Teams".
- **Đăng nhập kiểu mã thiết bị (OAuth device code, quyền Delegated)**: không cần client secret trong DLL (repo public), người dùng chỉ thấy đúng nhóm / kênh / list họ vốn xem được. Ứng dụng phải được admin đăng ký trên Entra ID và *Grant admin consent* (`TeamsTab.Read.All`, `Sites.Read.All`).
- **HttpClient thẳng, không MSAL / Graph SDK** — cùng lý do ADR 0006 (không kéo NuGet vào AppDomain của CAD). JSON đọc bằng `JsonLite` (link nguồn).
- **Refresh token mã hoá DPAPI** (`ProtectedData`, CurrentUser) ở `%LOCALAPPDATA%\OHTA_TOOL\teams\`, ngoài thư mục OHTA (OneDrive + gói phát hành). Tenant/Client ID không phải bí mật nhưng repo public nên để trong `06_Teams/data/teams.json` (gitignore, admin gửi file riêng từng máy); tab Lists đã chọn ở registry từng máy.
- **Đang phát triển (`stage: "dev"`) + `adminOnly: true`** cho tới khi chạy thử xong trên danh sách thật.

## Considered Options

- **Đọc tin nhắn kênh (`ChannelMessage.Read.All`) làm đơn** — bản đầu (v0.1) làm vậy; bỏ vì đơn thật nằm trong Lists, tin nhắn không có cột Trạng thái / Hạn / Người check.
- **Đồng bộ thư mục Files của kênh bằng OneDrive**: không cần admin, nhưng không có dữ liệu đơn. Không chọn.
- **Bot Teams**: hai chiều đầy đủ nhưng cần máy chủ công khai và Azure Bot. Không chọn.
- **Gộp vào OHTATOOL.dll**: code mạng thử nghiệm nằm chung lõi tự cập nhật của cả đội. Không chọn.
