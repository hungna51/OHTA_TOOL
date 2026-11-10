# 06_Teams — Tab Teams: đơn đang làm (dữ liệu từ ứng dụng OhtaTeams)

Từ [ADR 0009](../_docs/adr/0009-ung-dung-ohtateams-rieng.md) việc duyệt đơn Teams nằm ở ứng dụng riêng **[OhtaTeams.exe](../_apps/OhtaTeams/README.md)** (chạy ngoài CAD). **Tab Teams trên CAD chỉ hiện một đơn — đơn đang làm:**

1. Bản vẽ đang mở nằm trong thư mục của một đơn chưa xong (thư mục dưới **Thư mục đơn** đặt ở ⚙ của OhtaTeams, sâu ≤ 3 cấp, tên chứa Title đơn — bỏ dấu, không phân biệt hoa thường) → hiện đơn đó.
2. Không khớp → đơn bấm **📌 Đang làm** trong OhtaTeams.
3. Không có → "Chưa có đơn đang làm" + gợi ý.

Thẻ đơn: Title, cách chọn (theo bản vẽ / 📌), trạng thái, hạn, người triển khai / check, mọi cột; nút **Mở trên Teams**, **📁 Mở thư mục đơn**, **Mở OhtaTeams**. Tab đọc `%LOCALAPPDATA%\OHTA_TOOL\teams\dang_lam.json` (OhtaTeams ghi mỗi lần tải đơn / bấm 📌 / lưu ⚙), tự vẽ lại khi file đổi hoặc chuyển bản vẽ; file cũ hơn 1 ngày thì dòng cập nhật tô vàng. CAD không đăng nhập Microsoft.

Mã: `Views/DangLamControl.cs` (tab CAD), `Core/DangLam.cs` (đọc/ghi file, khớp bản vẽ ↔ đơn — OhtaTeams dùng chung bằng link nguồn). Phần dưới mô tả màn hình duyệt đơn của **OhtaTeams**.

## OhtaTeams: duyệt đơn

Đơn của công ty nằm trong **Microsoft Lists** gắn làm tab trong kênh Teams (ví dụ kênh `注文書 - 新規`, tab `注文書2026.1-VN`). Mỗi **dòng** của danh sách là một **đơn**. OhtaTeams đi theo đúng thứ tự trên Teams:

**Nhóm → Kênh → Tab → Danh sách đơn → Chi tiết đơn** (nút ← quay lại ở mỗi bước).

- Chỉ tab **Lists** mở được; tab khác (Bài đăng, File, Wiki, web…) hiện mờ "chưa hỗ trợ".
- Danh sách đơn gom theo **Hạn hoàn thành** (gần nhất trên cùng). Thẻ đơn: Title, Trạng thái, Người triển khai, Người check.
- Mặc định **ẩn đơn đã xong / dừng**: TRẠNG THÁI có "Đã hoàn thành" hoặc "STOP", hoặc ステータス = "完了" / "キャンセル". Ô tích **Xem cả đơn đã xong**, **Chỉ đơn của tôi** (tên người đăng nhập nằm trong Người triển khai / Người check).
- Bấm thẻ → mọi cột của đơn + nút **Mở trên Teams** (xem file, sửa đơn ở đó).
- **Chỉ xem**, không sửa đơn. Tab Lists chọn lần trước được nhớ: mở lại CAD là vào thẳng và tự tải đơn.

Cột được nhận theo **tên hiển thị** (không phân biệt hoa thường / dấu): "Hạn hoàn thành", "Trạng thái", "ステータス", "…triển khai", "…check…".

Graph + màn hình duyệt đơn nằm trong `_apps/OhtaTeams` ([ADR 0008](../_docs/adr/0008-tab-teams-graph-api.md), [ADR 0009](../_docs/adr/0009-ung-dung-ohtateams-rieng.md)); tab CAD trong `TEAMSTOOL.dll` (`plugin/TeamsTool.Plugin`, ra `bin/<ZwCAD|AutoCAD>/`). Đang phát triển (`stage: "dev"`) và `adminOnly: true`: chỉ máy Admin thấy tab.

## Cài đặt một lần (admin Microsoft 365)

Entra admin center → **App registrations → New registration** (`OHTA TOOL`, single tenant):

1. **Authentication** → *Allow public client flows* = **Yes**.
2. **API permissions** → Microsoft Graph → *Delegated*: `User.Read`, `Team.ReadBasic.All`, `Channel.ReadBasic.All`, `TeamsTab.Read.All`, `Sites.Read.All` → **Grant admin consent**.
3. Lấy **Directory (tenant) ID** và **Application (client) ID** ở trang Overview, ghi vào file `teams.json`:

```json
{ "tenantId": "<Directory (tenant) ID>", "clientId": "<Application (client) ID>" }
```

File **không lên GitHub** (`.gitignore`) — gửi riêng cho từng máy.

Thêm quyền mới sau khi đã đăng nhập → trong OhtaTeams bấm ⚙ → **Đăng xuất** rồi đăng nhập lại.

## Mỗi máy

1. Chép `teams.json` vào `06_Teams\data\` (hoặc trong OhtaTeams bấm **⚙** → nhập Tenant ID, Client ID → **Lưu**, ứng dụng tự tạo file).
2. Mở `_apps\OhtaTeams\bin\OhtaTeams.exe` (hoặc nút **Mở OhtaTeams** trên tab CAD). Chưa đăng nhập thì chỉ có nút **Đăng nhập** (mờ nếu chưa có `teams.json`). Bấm → trình duyệt mở `microsoft.com/devicelogin`, mã hiện to dưới nút và đã chép sẵn → dán, đăng nhập tài khoản công ty.
3. Chọn nhóm → kênh → tab Lists. **⟳ Làm mới** khi cần. ⚙ → **Thư mục đơn** để tab CAD khớp bản vẽ với đơn. Đăng xuất ở ⚙.

## Dữ liệu nằm ở đâu

| Thứ | Chỗ |
|---|---|
| Tenant ID, Client ID | `06_Teams\data\teams.json` (gitignore, gửi riêng) |
| Tab Lists đã chọn, thư mục đơn, đơn 📌 | `HKCU\Software\OFT_Lisp\OhtaTool\Teams` (`tab_chon`, `thu_muc_goc`, `don_chon`) |
| Đơn đang làm cho tab CAD | `%LOCALAPPDATA%\OHTA_TOOL\teams\dang_lam.json` (OhtaTeams ghi, CAD đọc) |
| Phiên đăng nhập (refresh token, mã hoá DPAPI theo tài khoản Windows) | `%LOCALAPPDATA%\OHTA_TOOL\teams\token.bin` — **Đăng xuất** xoá file này |
| Bản lưu tạm danh sách đơn (JSON thô) | `%LOCALAPPDATA%\OHTA_TOOL\teams\tam\<listId>\` — **Đăng xuất** xoá |

## Tốc độ

- Lần đầu mở một danh sách: tải cột + dòng song song, `$top=5000` (thường 1 lần gọi), nén gzip; tên người chỉ tra những người có trong các dòng (song song 8). Đo 11/2026: 3748 đơn ~4,7 s (trước ~25 s).
- Lần sau: hiện ngay bản lưu tạm (~0,3 s) rồi tự cập nhật ở nền; trong lúc đó vẫn bấm xem / quay lại được. ⟳ Làm mới cũng tải ở nền.

## Giới hạn hiện tại

- Tải hết các dòng (tối đa 10 × 5000) rồi lọc trên máy.
- Không đọc file đính kèm của dòng (Graph v1.0 không hỗ trợ) — dùng **Mở trên Teams**.
- Tên người lấy từ *User Information List* của site; người đã xoá / không đọc được thì hiện `#id`.
- Không tự làm mới theo giờ: mở danh sách hoặc bấm ⟳ để cập nhật.

## Test

`dotnet run --project 06_Teams/plugin/TeamsTool.Tests` (build_ohta.ps1 tự chạy) — tìm URL Lists trong cấu hình tab, tách site, khớp list, nhận cột theo tên, đọc giá trị cột (người, ngày, nhiều lựa chọn, HTML), đơn đã xong, `teams.json`, đọc/ghi `dang_lam.json`, khớp bản vẽ ↔ đơn.
