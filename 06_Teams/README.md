# 06_Teams — Tab Teams: nhận đơn từ kênh Microsoft Teams

Đọc tin nhắn ở các kênh Teams đã chọn. Mỗi **tin nhắn gốc có file DWG / DXF / PDF / Excel / CSV** thành một **đơn**: người gửi, giờ, người được tag (phụ trách), nội dung, các file. Từ tab có thể:

- **Chỉ đơn tag tôi**: lọc đơn có tag người đang đăng nhập.
- Bấm tên file: tải về `Thư mục lưu đơn\<nhóm / kênh>\yyyyMMdd_HHmm_<người gửi>\` rồi mở (DWG/DXF mở trong CAD, file khác mở bằng chương trình mặc định).
- **Tải hết**: tải tất cả file của đơn rồi mở thư mục.
- **✔ Nhận đơn**: trả lời "Đã nhận đơn – <tên> (OHTA TOOL)" vào luồng tin nhắn đó.

Giao diện + logic nằm trong `TEAMSTOOL.dll` (`plugin/TeamsTool.Plugin`, ra `bin/<ZwCAD|AutoCAD>/`) — [ADR 0008](../_docs/adr/0008-tab-teams-graph-api.md). Đang phát triển (`stage: "dev"`, hiện `v0.1 (dev)`) và `adminOnly: true`: chỉ máy Admin thấy tab.

## Cài đặt một lần (admin Microsoft 365)

Entra admin center → **App registrations → New registration** (`OHTA TOOL`, single tenant):

1. **Authentication** → *Allow public client flows* = **Yes**.
2. **API permissions** → Microsoft Graph → *Delegated*: `User.Read`, `Team.ReadBasic.All`, `Channel.ReadBasic.All`, `ChannelMessage.Read.All`, `ChannelMessage.Send`, `Files.Read.All` → **Grant admin consent**.
3. Lấy **Directory (tenant) ID** và **Application (client) ID** ở trang Overview.

## Mỗi máy

1. Tab Teams → **⚙ Cài đặt**: dán Tenant ID, Client ID (chỉnh thư mục lưu nếu muốn) → **Lưu**.
2. **Đăng nhập**: trình duyệt mở `microsoft.com/devicelogin`, mã đã chép sẵn → dán, đăng nhập tài khoản công ty.
3. **Chọn kênh** → tích các kênh nhận đơn → **Lưu kênh đã chọn**.
4. **⟳ Làm mới đơn**.

## Dữ liệu nằm ở đâu

| Thứ | Chỗ |
|---|---|
| Tenant ID, Client ID, thư mục lưu, kênh đã chọn | `HKCU\Software\OFT_Lisp\OhtaTool\Teams` |
| Phiên đăng nhập (refresh token, mã hoá DPAPI theo tài khoản Windows) | `%LOCALAPPDATA%\OHTA_TOOL\teams\token.bin` — **Đăng xuất** xoá file này |
| File đơn tải về | Mặc định `Documents\OHTA_DonTeams` |

## Giới hạn hiện tại

- Mỗi kênh đọc **50 tin gốc mới nhất**; file gửi trong **tin trả lời** chưa được tính là đơn.
- Không tự làm mới: bấm ⟳ khi cần.
- Trạng thái "đã nhận" không lưu trên máy — xem trong luồng Teams.
- Chỉ thấy kênh mà tài khoản đang đăng nhập có quyền xem.

## Test

`dotnet run --project 06_Teams/plugin/TeamsTool.Tests` (build_ohta.ps1 tự chạy) — phân tích JSON Graph, lọc đơn, tag, mã `/shares`, tên thư mục an toàn.
