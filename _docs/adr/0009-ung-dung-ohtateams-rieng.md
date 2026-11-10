# Đơn Teams chuyển sang ứng dụng riêng OhtaTeams.exe, tab CAD chỉ đọc file đơn đang làm

Thay phần "giao diện duyệt đơn nằm trong CAD" của [ADR 0008](0008-tab-teams-graph-api.md); cách gọi Graph, đăng nhập mã thiết bị, DPAPI, `teams.json` giữ nguyên. Thiết kế: [_docs/specs/2026-11-10-ung-dung-teams-rieng.md](../specs/2026-11-10-ung-dung-teams-rieng.md).

- **OhtaTeams.exe** (`_apps/OhtaTeams`, WPF net48, không NuGet) giữ toàn bộ Graph + màn hình Nhóm → Kênh → Tab → Đơn. Tính năng làm việc với đơn từ nay thêm ở đây, không thêm vào CAD: mở CAD lâu, palette chật, và lỗi mạng / đăng nhập không được làm phiền phiên vẽ.
- **CAD không gọi Graph.** Ứng dụng ghi `%LOCALAPPDATA%\OHTA_TOOL\teams\dang_lam.json` (đơn chưa xong + đơn 📌, đã tính sẵn tên người / ngày, thư mục đơn, giờ ghi) bằng ghi-file-tạm-rồi-thay. Tab Teams trên CAD (`DangLamControl` trong TEAMSTOOL.dll) chỉ đọc file này, theo dõi bằng `FileSystemWatcher` + `DocumentActivated`. Ứng dụng tắt lâu thì thông tin cũ: tab tô vàng khi file cũ hơn 1 ngày.
- **Đơn đang làm = khớp bản vẽ trước, không thì đơn 📌.** Khớp: một trong ≤ 3 thư mục đầu dưới *Thư mục đơn* (⚙ của ứng dụng, registry `thu_muc_goc`) có tên chứa Title đơn (bỏ dấu, không phân biệt hoa thường, Title ≥ 3 ký tự); nhiều đơn khớp lấy Title dài nhất. Logic thuần ở `Core/DangLam.cs`, có test.
- **Mã dùng chung bằng link nguồn**, không DLL chung: `06_Teams/plugin/TeamsTool.Plugin/Core/*` và `Views/KieuTeams.cs` biên dịch vào cả TEAMSTOOL.dll lẫn OhtaTeams.exe (như JsonLite — ADR 0006).
- **Sửa đơn chỉ trong OhtaTeams.** Quyền `Sites.ReadWrite.All` xin bằng refresh token **riêng lúc lưu** (đăng nhập vẫn chỉ xin quyền đọc), để tenant chưa cấp quyền ghi vẫn xem được đơn. PATCH `…/items/{id}/fields` kèm `If-Match: <eTag>`: người khác vừa sửa → 412, báo và tải lại thay vì ghi đè. Cột người / tra cứu / chữ richText chưa sửa (cần tra id người, dễ mất định dạng).
- **Đường dẫn exe cố định** `_apps/OhtaTeams/bin/OhtaTeams.exe` (gói phát hành thêm riêng vì thư mục `_` không được đóng gói); exe đi ngược lên tìm `06_Teams` để đọc `teams.json`.

## Considered Options

- **CAD tự gọi Graph lấy 1 đơn**: luôn mới, nhưng CAD vẫn giữ đăng nhập + mã mạng, và hai tiến trình cùng làm mới refresh token. Không chọn.
- **Gửi qua named pipe / HTTP local**: tức thì, nhưng CAD mất dữ liệu khi ứng dụng tắt; file đơn giản hơn và đủ nhanh.
- **Repo / DLL chung riêng**: chưa cần khi chỉ có 2 nơi dùng.
