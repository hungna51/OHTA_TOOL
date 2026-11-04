# Mục lục luật riêng — Tab PRINT

| Việc | Đọc |
|---|---|
| Hiểu yêu cầu đã chốt (ưu tiên bản vẽ, cấu hình, tên file, thứ tự khung) | [../../_docs/plans/2026-11-04-tab-print-spec.md](../../_docs/plans/2026-11-04-tab-print-spec.md) |
| Vì sao in bằng PlotEngine, gộp bằng PdfSharp, DLL phụ thuộc | [../../_docs/adr/0005-tab-print-va-pdfsharp.md](../../_docs/adr/0005-tab-print-va-pdfsharp.md) |

Quy tắc cứng:
- Không gom hàng theo luật 08 của GURO: chỉ sắp theo Y điểm cao nhất của khung (dung sai 1 mm) rồi trái → phải.
- Logic không phụ thuộc CAD đặt ở `Core/` và phải có test; sửa tab thì tăng `version` trong `module.json`.
