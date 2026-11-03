# Tự cập nhật từ GitHub Releases, giữ phần máy tự thêm

Từ v1.1.0 (11/2026), mỗi máy trong xưởng có một bản OHTA_TOOL riêng trên ổ của máy và tự cập nhật từ Release của repo public `hungna51/OHTA_TOOL`. Mỗi bản phát hành là một tag `vX.Y.Z` kèm `OHTA_TOOL_vX.Y.Z.zip` đã build sẵn cả 2 DLL; zip có `VERSION` và `_system/manifest.txt` (sha256 từng file). Bạn phát hành bằng `release_ohta.ps1`: chỉ chạy trên `main` sạch, build đủ ZWCAD + AutoCAD và test PASS, ghi chú lấy từ mục `## vX.Y.Z` của `CHANGELOG.md`.

Trên máy đội (`OhtaUpdateCommands`, `Core/UpdateCore.cs`, `Core/UpdateInstaller.cs`):

- `OHTA_Loader.lsp` gắn vào `S::STARTUP` của mỗi bản vẽ: nạp DLL rồi gọi `(ohta_net_update_check)`. Gọi mạng tối đa 1 lần/ngày, chạy nền; kết quả lưu ở `%LOCALAPPDATA%\OHTA_TOOL\update\state.json`. Còn bản mới chưa cài thì mỗi bản vẽ mở ra hỏi một lần "Cập nhật ngay / Để sau". `OHTAUPDATE` kiểm tra ngay.
- Cài: tải zip, so sha256 do GitHub cấp, giải nén ra thư mục tạm, so sha256 từng file theo manifest; xong hết mới đụng thư mục tool. Sao lưu các file sắp bị đụng vào `update\backup\OHTA_TOOL_v<cũ>.zip` (giữ 1 bản) — `OHTAROLLBACK` quay lại. `VERSION` + manifest ghi sau cùng.
- DLL đang bị CAD khoá thì đổi tên `*.old_<giờ>` rồi đặt bản mới (dọn ở lần nạp sau); bản mới chạy từ lần mở CAD sau. Mutex `Local\OHTA_TOOL_Update` chặn 2 cửa sổ CAD cùng cài; cửa sổ kia thấy `VERSION` đổi thì chỉ nhắc khởi động lại.
- Giữ phần máy tự thêm: chỉ xoá file *có trong manifest bản cũ, không có trong bản mới, và máy chưa sửa*. File chưa bản phát hành nào có (lisp / icon / tab thêm bằng nút ＋, `google_sheet.key`, `module.json.bak`) không bao giờ bị đụng. `module.json` được gộp với `update\base\<tab>.module.json` (bản phát hành lần cài trước) làm gốc: bản mới thắng ở nút nó có, nút máy tự thêm giữ ở cuối, nút máy đã gỡ không hiện lại (trừ `locked`), `groups` / `startupLisp` lấy hợp, tiêu đề / màu tab máy đã đổi thì giữ.
- Repo public nên zip **không** chứa `google_sheet.key`: khoá gửi tay một lần khi cài máy mới, cập nhật không đụng tới.
- Máy đang chạy v1.0 (chưa có trình cập nhật) phải cài tay zip v1.1.0 một lần.

## Considered Options

- **Kéo thẳng nhánh `main`**: không có DLL (`bin/` bị git bỏ qua) và đội nhận cả code đang làm dở. Không chọn.
- **Repo private + token trên mỗi máy**: giữ được khoá trong zip nhưng phải phát và giữ token trên từng máy, lộ token còn tệ hơn. Không chọn.
- **Ghi đè nguyên gói**: mất lisp / nút người dùng tự thêm bằng nút ＋ ([ADR 0002](0002-tab-chung-them-lisp-va-nhom.md)). Không chọn.
- **Cài im lặng không hỏi**: người dùng có thể đang làm dở bản vẽ, và DLL chỉ có hiệu lực sau khi mở lại CAD. Không chọn.
