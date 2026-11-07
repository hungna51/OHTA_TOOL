# 06_Teams — Tab Teams: xem đơn trong tab Lists của kênh Teams

Đơn của công ty nằm trong **Microsoft Lists** gắn làm tab trong kênh Teams (ví dụ kênh `注文書 - 新規`, tab `注文書2026.1-VN`). Mỗi **dòng** của danh sách là một **đơn**. Tab Teams trên bảng OHTA đi theo đúng thứ tự trên Teams:

**Nhóm → Kênh → Tab → Danh sách đơn → Chi tiết đơn** (nút ← quay lại ở mỗi bước).

- Chỉ tab **Lists** mở được; tab khác (Bài đăng, File, Wiki, web…) hiện mờ "chưa hỗ trợ".
- Danh sách đơn gom theo **Hạn hoàn thành** (gần nhất trên cùng). Thẻ đơn: Title, Trạng thái, Người triển khai, Người check.
- Mặc định **ẩn đơn đã xong**: TRẠNG THÁI = "Đã hoàn thành" hoặc ステータス = "完了". Ô tích **Xem cả đơn đã xong**, **Chỉ đơn của tôi** (tên người đăng nhập nằm trong Người triển khai / Người check).
- Bấm thẻ → mọi cột của đơn + nút **Mở trên Teams** (xem file, sửa đơn ở đó).
- **Chỉ xem**, không sửa đơn. Tab Lists chọn lần trước được nhớ: mở lại CAD là vào thẳng và tự tải đơn.

Cột được nhận theo **tên hiển thị** (không phân biệt hoa thường / dấu): "Hạn hoàn thành", "Trạng thái", "ステータス", "…triển khai", "…check…".

Giao diện + logic nằm trong `TEAMSTOOL.dll` (`plugin/TeamsTool.Plugin`, ra `bin/<ZwCAD|AutoCAD>/`) — [ADR 0008](../_docs/adr/0008-tab-teams-graph-api.md). Đang phát triển (`stage: "dev"`) và `adminOnly: true`: chỉ máy Admin thấy tab.

## Cài đặt một lần (admin Microsoft 365)

Entra admin center → **App registrations → New registration** (`OHTA TOOL`, single tenant):

1. **Authentication** → *Allow public client flows* = **Yes**.
2. **API permissions** → Microsoft Graph → *Delegated*: `User.Read`, `Team.ReadBasic.All`, `Channel.ReadBasic.All`, `TeamsTab.Read.All`, `Sites.Read.All` → **Grant admin consent**.
3. Lấy **Directory (tenant) ID** và **Application (client) ID** ở trang Overview, ghi vào file `teams.json`:

```json
{ "tenantId": "<Directory (tenant) ID>", "clientId": "<Application (client) ID>" }
```

File **không lên GitHub** (`.gitignore`) — gửi riêng cho từng máy.

Thêm quyền mới sau khi đã đăng nhập → trên CAD bấm ⚙ → **Đăng xuất** rồi đăng nhập lại.

## Mỗi máy

1. Chép `teams.json` vào `06_Teams\data\` (hoặc nút **⚙** trên header → nhập Tenant ID, Client ID → **Lưu**, tab tự tạo file).
2. Chưa đăng nhập, tab chỉ có nút **Đăng nhập** giữa palette (mờ nếu chưa có `teams.json`). Bấm → trình duyệt mở `microsoft.com/devicelogin`, mã hiện to dưới nút và đã chép sẵn → dán, đăng nhập tài khoản công ty.
3. Chọn nhóm → kênh → tab Lists. **⟳ Làm mới** khi cần. Đăng xuất ở ⚙.

## Dữ liệu nằm ở đâu

| Thứ | Chỗ |
|---|---|
| Tenant ID, Client ID | `06_Teams\data\teams.json` (gitignore, gửi riêng) |
| Tab Lists đã chọn | `HKCU\Software\OFT_Lisp\OhtaTool\Teams` (`tab_chon`) |
| Phiên đăng nhập (refresh token, mã hoá DPAPI theo tài khoản Windows) | `%LOCALAPPDATA%\OHTA_TOOL\teams\token.bin` — **Đăng xuất** xoá file này |

## Giới hạn hiện tại

- Tải hết các dòng (tối đa 5000) rồi lọc trên máy.
- Không đọc file đính kèm của dòng (Graph v1.0 không hỗ trợ) — dùng **Mở trên Teams**.
- Tên người lấy từ *User Information List* của site; không đọc được thì hiện `#id`.
- Không tự làm mới: bấm ⟳ khi cần.

## Test

`dotnet run --project 06_Teams/plugin/TeamsTool.Tests` (build_ohta.ps1 tự chạy) — tìm URL Lists trong cấu hình tab, tách site, khớp list, nhận cột theo tên, đọc giá trị cột (người, ngày, nhiều lựa chọn, HTML), đơn đã xong, `teams.json`.
