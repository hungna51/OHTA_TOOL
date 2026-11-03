# Kế hoạch: tab chung — nút ＋ thêm lisp, nhóm lệnh, cài đặt tab

Quyết định kiến trúc: [ADR 0002](../adr/0002-tab-chung-them-lisp-va-nhom.md). Áp dụng cho `_system/OhtaTool.Plugin/Views/ModuleTabControl` — tab More, Guro và mọi tab sau này; Fname giữ giao diện riêng.

## Bước 1 — Nhóm lệnh

- `ModuleManifest.Groups` (`groups`), `ToolItem.Icon` (`icon`), `ToolItem.Locked` (`locked`).
- `ModuleGrouping`: gom nút theo `category`, thứ tự theo `groups`, nhóm lạ xếp sau theo thứ tự xuất hiện, nút không nhóm vào "Khác" cuối cùng.
- Tiêu đề nhóm bấm để thu gọn/mở; trạng thái thu gọn lưu theo máy (`GroupCollapseStore`, `%APPDATA%\OHTA_TOOL\ui_state.txt`).
- Tìm kiếm: chỉ hiện nhóm có nút khớp, nhóm đó tự mở; xoá ô tìm thì về trạng thái cũ.
- Tab chỉ có một nhóm "Khác" thì không hiện tiêu đề nhóm (Guro trước khi chia nhóm trông như cũ).

## Bước 2 — Thanh công cụ và cửa sổ thêm lisp

- Thanh: ô tìm, ＋, ⟳. Bỏ MD, Rules; 📁 chuyển vào cài đặt. Nút ⚙ ở góc header.
- Tab trống: một nút ＋ lớn "Bấm ＋ để thêm lisp".
- Cửa sổ thêm: chọn `.lsp` → quét `defun c:XXX` (đọc theo byte, không đổi encoding) → tích chọn lệnh → mỗi lệnh điền tên, mô tả, badge, nhóm (chọn hoặc gõ mới), ảnh.
- Chép file vào `lisp/` (trùng tên: hỏi ghi đè hay đổi tên), ảnh vào `icons/`.
- Ô "Nạp khi mở CAD" (mặc định bật) → thêm vào `startupLisp`; `load` ngay trong phiên CAD.
- Chặn lệnh trùng trên mọi tab (báo tên tab đang dùng).
- `ModuleJsonEditor`: đọc lại → chèn → ghi file tạm → thay, giữ `.bak`, giữ trường lạ.

## Bước 3 — Menu chuột phải

- Sửa / Xoá / Chuyển nhóm / Lên / Xuống.
- `locked: true` → không có Xoá. Đánh dấu GRO, GI, DST của Guro.
- Xoá chỉ gỡ nút khỏi `module.json`, không xoá file `.lsp`.

## Bước 4 — Cửa sổ cài đặt tab (⚙)

- Màu chủ đạo: 8 ô mẫu + ô hex, xem trước header.
- Tiêu đề, mô tả.
- Nhóm: đổi tên, ▲▼, xoá (nút trong nhóm bị xoá chuyển về "Khác").
- Nút mở thư mục tab.

## Kiểm chứng

- `OhtaTool.Tests`: quét `defun c:`, đọc/ghi `module.json` (giữ trường lạ, `.bak`), trùng lệnh, sắp nhóm, lưu thu gọn.
- Mỗi bước chạy `build_ohta.ps1`.
- Giao diện: thử tay trên ZWCAD 2026 và AutoCAD.
