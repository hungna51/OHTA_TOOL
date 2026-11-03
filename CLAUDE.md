# OHTA TOOL — Claude Code Development Guide

> Tài liệu chỉ dẫn dành cho Claude Code khi làm việc trong dự án `OHTA_TOOL/`.
> Toàn bộ cấu trúc phân tầng, danh mục luật chung (`rules/`) và quy tắc từng mục con được quản lý thống nhất tại **[GEMINI.md](GEMINI.md)**.

## Quy Tắc Cốt Lõi Cần Đọc Ngay

1. Đọc **[GEMINI.md](GEMINI.md)** để nắm bản đồ toàn dự án và danh sách các mục con hiện có.
2. Đọc bộ luật chung tại **[`rules/`](rules/)**:
   - [01-kien-truc-va-phan-tang.md](rules/01-kien-truc-va-phan-tang.md)
   - [02-da-nen-tang-zwcad-autocad.md](rules/02-da-nen-tang-zwcad-autocad.md)
   - [03-giao-thuc-module-va-palette.md](rules/03-giao-thuc-module-va-palette.md)
   - [04-encoding-va-checklist-an-toan.md](rules/04-encoding-va-checklist-an-toan.md)
3. Khi làm việc trong một thư mục con cụ thể (`01_...`, `02_...`), **bắt buộc** đọc thêm `README.md` và thư mục `rules/` nằm bên trong chính thư mục con đó.
4. Trước khi hoàn tất thay đổi, chạy `powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1` để kiểm chứng cả 2 bản build (`ZwCAD` + `AutoCAD`) và bộ Unit Test `OhtaTool.Tests`.

