# Tab Teams đọc đơn qua Microsoft Graph, DLL riêng TEAMSTOOL.dll, đăng nhập bằng mã thiết bị

Từ 11/2026, công ty nhận và giao đơn qua nhiều kênh Microsoft Teams (tin nhắn kèm DWG / PDF / Excel, tag người phụ trách). Tab **Teams** (mục con `06_Teams`) đọc các kênh đó ngay trong CAD qua **Microsoft Graph** (`/me/joinedTeams`, `/teams/{id}/channels`, `.../messages`, `/shares/{u!…}/driveItem/content`, `.../replies`). Giao diện và logic nằm trong **TEAMSTOOL.dll**, nạp bằng `DllTabLoader` với cùng hợp đồng `TeamsTool.TeamsOhtaTab.TaoGiaoDien(tieuDe, moTa, mauNhan, thuMucModule)` như tab Guro / AI ([ADR 0003](0003-tab-guro-nam-trong-grotool.md), [ADR 0006](0006-tab-ai-dll-rieng.md)).

- **Đăng nhập kiểu mã thiết bị (OAuth device code, quyền Delegated)**: không cần client secret trong DLL (repo public), người dùng chỉ thấy đúng các kênh họ vốn xem được. Ứng dụng phải được admin đăng ký trên Entra ID và *Grant admin consent* vì `ChannelMessage.Read.All` bắt buộc admin duyệt.
- **HttpClient thẳng, không MSAL / Graph SDK** — cùng lý do ADR 0006 (không kéo NuGet vào AppDomain của CAD). JSON đọc bằng `JsonLite` (link nguồn).
- **Refresh token mã hoá DPAPI** (`ProtectedData`, CurrentUser) ở `%LOCALAPPDATA%\OHTA_TOOL\teams\`, ngoài thư mục OHTA (OneDrive + gói phát hành). Tenant/Client ID không phải bí mật nhưng vẫn để ở registry từng máy, không ghi vào repo public.
- **Đang phát triển (`stage: "dev"`) + `adminOnly: true`** cho tới khi chạy thử xong trên kênh thật.

## Considered Options

- **Đồng bộ thư mục Files của kênh bằng OneDrive**: không cần admin, nhưng không đọc được tin nhắn và không biết ai được tag. Giữ làm phương án dự phòng.
- **Bot Teams**: hai chiều đầy đủ nhưng cần máy chủ công khai và Azure Bot. Không chọn.
- **Gộp vào OHTATOOL.dll**: code mạng thử nghiệm nằm chung lõi tự cập nhật của cả đội. Không chọn.
