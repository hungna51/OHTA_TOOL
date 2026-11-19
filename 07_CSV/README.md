# 07_CSV — Tab CSV (xuất thông tin tấm + EXPORT CSV)

Thay lisp `xuat_csv_v5.0.1` (ZWCAD + AUTOCAD). Đặc tả: [`_docs/specs/2026-11-19-tab-csv.md`](../_docs/specs/2026-11-19-tab-csv.md).

- Lệnh `CSV`: mở bảng OHTA, chuyển sang tab CSV.
- **Xuất thông tin tấm (B1)**: ghi MText thông tin tấm (layer `KHUNG_IN`) vào từng khung. *Giai đoạn 1: chạy lisp cũ trong `lisp/`.*
- **EXPORT (B2)**: đọc MText + F-name + dim tổng của mọi khung → `<đơn>.csv` (10 cột, gửi nhà máy) và `data\<đơn>-data.csv` (35 cột cũ). Có lỗi ở bất kỳ tấm nào thì không ghi file nào.
- **Danh sách tấm**: sửa ô thông tin → ghi MText; sửa N/Q/M/T → ghi F-name. Bấm lỗi → zoom + vòng đỏ tạm.

## Cấu trúc
- `plugin/CsvTool.Plugin/Core/` — logic thuần (không CAD), test bằng `plugin/CsvTool.Tests`.
- `plugin/CsvTool.Plugin/CadServices/` — đọc / ghi bản vẽ.
- `lisp/` — lisp cũ chỉ để chạy B1 tới giai đoạn 2.
- `rules/` — luật riêng tab (mục lục: `rules/00-muc-luc.md`).
