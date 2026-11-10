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
- Danh sách đơn dạng **bảng như view Lists** (cửa sổ 1400 px, kéo ngang): nhóm theo **Hạn hoàn thành** ("Chưa được gán" trên cùng, bấm tiêu đề nhóm để thu gọn, cuối nhóm có số đơn, cuối bảng "Số lượng"); cột người hiện thẻ tên, cột lựa chọn hiện nhãn màu. Màu dòng: quá Hạn hoàn thành mà chưa xong = hồng (ưu tiên nhất); còn lại theo Trạng thái: Câu hỏi - xác nhận = đỏ, Đang check = vàng, Đang triển khai = xanh (Graph không trả quy tắc màu của view — quy tắc ở `Core/BangDon.cs`). Cột mặc định theo view tiến độ (Title, Người triển khai, Người check 1, 加工, Trạng thái, Hạn hoàn thành, 切断スタート納期, 納期, 担当者, Loại đơn, Phân loại, Công ty, 注意); **kéo thả tiêu đề cột** ngay trên bảng để đổi vị trí (vạch màu báo chỗ thả); **☰ Cột** chọn cột hiện và sắp thứ tự bằng ▲ ▼; thứ tự nhớ riêng từng danh sách (registry `cot_hien_<listId>`). Vẽ tối đa 500 dòng.
- Mặc định **ẩn đơn đã xong / dừng**: TRẠNG THÁI có "Đã hoàn thành" hoặc "STOP", hoặc ステータス = "完了" / "キャンセル". Ô tích **Xem cả đơn đã xong**, **Chỉ đơn của tôi** (tên người đăng nhập nằm trong Người triển khai / Người check).
- Bấm một dòng → **chi tiết đơn dạng form Lists**: 2 phần **OHTA COMPANY** (Title + cột tên tiếng Nhật) / **OFT BY OHTA** (còn lại), lưới 3 cột, biểu tượng theo loại cột, ô trống hiện "Nhập giá trị tại đây". **Bấm một ô để sửa ngay tại chỗ** → **✓ Lưu** gửi riêng cột đó lên Teams (kèm phiên bản đơn) rồi tải lại đúng đơn đó (~0,3 s); **✕ Huỷ** đóng ô. Cuối form: **📎 Tệp đính kèm** (bấm tên tệp → tải về `%TEMP%\OHTA_TOOL\teams\tep\<id đơn>\` rồi mở) và **💬 Trao đổi trên Teams** (mọi chuỗi chat gắn với đơn — cột ẩn `teamsConversationMetadata_*` — tin gốc + trả lời, ô **Gửi trả lời** vào đúng chuỗi). Cách chia phần theo tên cột (Graph không trả cấu hình form) — ở `BangDon.PhanForm`.
- **✏ Sửa nhiều ô** (trong chi tiết đơn): form theo loại cột — chữ (cả nhiều dòng), số, lựa chọn (ô tích nếu chọn nhiều), ngày (+ giờ nếu cột có giờ), có/không, **người** (giao đơn: ô gõ tên để lọc + danh sách nhân viên tích chọn; cột 1 người thì tích người mới là bỏ người cũ). Cột tra cứu / chữ có định dạng chỉ xem (sửa trên Teams). Danh sách nhân viên = mọi người từng có trong cột người của các đơn đã tải — người chưa từng được giao đơn nào thì giao lần đầu trên Teams. **💾 Lưu** hiện bảng *cũ → mới* để xác nhận, chỉ gửi các ô đã đổi, kèm phiên bản đơn: ai đó vừa sửa đơn trên Teams thì không ghi đè mà báo và tải lại. Lưu xong tự tải lại danh sách.
- Ô **🔍 Tìm đơn**: gõ số đơn, tên, ghi chú… — tìm trong mọi cột, không dấu, không phân biệt hoa thường, nhiều từ thì đơn phải có đủ; đang tìm thì tìm cả đơn đã xong; **Esc** xoá ô tìm.
- **Sửa nhanh nhiều đơn**: tích ô đầu dòng (ô ở hàng tiêu đề = chọn mọi đơn đang hiện) → thanh **Đã chọn N đơn — gán [cột] = [giá trị]** → **Áp dụng**: xác nhận 1 lần, gửi từng đơn (kèm phiên bản, đơn vừa bị người khác sửa thì bỏ qua và báo), xong tải lại danh sách. Mặc định cột Hạn hoàn thành.
- Ô **Nhân viên** trên danh sách đơn: chỉ hiện đơn có người đó ở bất kỳ cột người nào (triển khai, check 1/2, phụ trách…).
- Tab Lists chọn lần trước được nhớ: mở lại là vào thẳng và tự tải đơn.

Cột được nhận theo **tên hiển thị** (không phân biệt hoa thường / dấu): "Hạn hoàn thành", "Trạng thái", "ステータス", "…triển khai", "…check…".

Graph + màn hình duyệt đơn nằm trong `_apps/OhtaTeams` ([ADR 0008](../_docs/adr/0008-tab-teams-graph-api.md), [ADR 0009](../_docs/adr/0009-ung-dung-ohtateams-rieng.md)); tab CAD trong `TEAMSTOOL.dll` (`plugin/TeamsTool.Plugin`, ra `bin/<ZwCAD|AutoCAD>/`). Đang phát triển (`stage: "dev"`) và `adminOnly: true`: chỉ máy Admin thấy tab.

## Cài đặt một lần (admin Microsoft 365)

Entra admin center → **App registrations → New registration** (`OHTA TOOL`, single tenant):

1. **Authentication** → *Allow public client flows* = **Yes**.
2. **API permissions** → Microsoft Graph → *Delegated*: `User.Read`, `Team.ReadBasic.All`, `Channel.ReadBasic.All`, `TeamsTab.Read.All`, `Sites.Read.All` → **Grant admin consent**. Thêm cho OhtaTeams (Delegated → Grant admin consent): Microsoft Graph `Sites.ReadWrite.All` (sửa đơn), `ChannelMessage.Read.All` + `ChannelMessage.Send` (chat kèm đơn); **SharePoint** `AllSites.Read` (tệp đính kèm — Graph v1.0 không có) — mỗi nhóm quyền xin token riêng lúc dùng, thiếu quyền nào thì chỉ tính năng đó báo lỗi — chưa có quyền này thì chỉ nút Lưu báo lỗi, phần xem vẫn chạy; cấp xong không cần đăng nhập lại.
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
- Tệp đính kèm đọc qua SharePoint REST (`/_api/web/lists(guid)/items(id)/AttachmentFiles`) vì Graph v1.0 không hỗ trợ; chưa thêm / xoá tệp từ ứng dụng.
- Tên người lấy từ *User Information List* của site; người đã xoá / không đọc được thì hiện `#id`.
- Không tự làm mới theo giờ: mở danh sách hoặc bấm ⟳ để cập nhật.

## Test

`dotnet run --project 06_Teams/plugin/TeamsTool.Tests` (build_ohta.ps1 tự chạy) — tìm URL Lists trong cấu hình tab, tách site, khớp list, nhận cột theo tên, đọc giá trị cột (người, ngày, nhiều lựa chọn, HTML), đơn đã xong, `teams.json`, đọc/ghi `dang_lam.json`, khớp bản vẽ ↔ đơn.
