# 07_CSV — Tab CSV (xuất thông tin tấm + EXPORT CSV)

Thay lisp `xuat_csv_v5.0.1` (ZWCAD + AUTOCAD). Đặc tả: [`_docs/specs/2026-11-19-tab-csv.md`](../_docs/specs/2026-11-19-tab-csv.md).

- Lệnh `CSV`: mở bảng OHTA, chuyển sang tab CSV.
- **Xuất thông tin tấm (B1)**: ghi MText thông tin tấm (layer `KHUNG_IN`) vào từng khung. *Giai đoạn 1: chạy lisp cũ trong `lisp/`.*
- **EXPORT (B2)** — lệnh `CSVEXPORT`: chọn khung in (nhận cả khung chọn sẵn), đọc MText + F-name + dim tổng → `<đơn>.csv` (10 cột, gửi nhà máy) và `data\<đơn>-data.csv` (35 cột cũ). Có lỗi ở bất kỳ tấm nào, tên tấm trùng, bản vẽ chưa lưu hay file đang mở trong Excel thì không ghi file nào. Ghi xong tự mở `<đơn>.csv`.
- **Làm mới** — lệnh `CSVLAMMOI`: như EXPORT nhưng chỉ đọc lại danh sách, không ghi CSV.
- **Danh sách tấm**: sửa ô Tappu / Sara / Pitchi / Vát / Gia công → ghi MText; sửa N/Q/M/T → ghi F-name (Ctrl+Z hoàn tác được). CSV chỉ đổi khi bấm EXPORT. Bấm dòng → zoom tới khung; dòng lỗi → zoom + vòng đỏ tạm (transient, không nằm trong bản vẽ).
- ⚙ cài đặt (sắp xếp, layer khung / dim / tapu, bước spline, dung sai khe hở) lưu ở `HKCU\Software\OFT_Lisp\CSV`.

## Giai đoạn 2 (còn lại)
- Chuyển hình học B1 sang C#: `CSV--analyze-boundary`, `CSV--bend-list-smart`, `CSV--pricing-info`, `CSV--count-vat`, `CSV--scan-machining-texts`.
- Sau đó xoá `lisp/`, `CadServices/ChayB1Lisp.cs` (kèm đoạn ghi khoá registry cũ `HKCU\Software\Autodesk`).

## Cấu trúc
- `plugin/CsvTool.Plugin/Core/` — logic thuần (không CAD), test bằng `plugin/CsvTool.Tests`.
- `plugin/CsvTool.Plugin/CadServices/` — đọc / ghi bản vẽ.
- `lisp/` — lisp cũ chỉ để chạy B1 tới giai đoạn 2.
- `rules/` — luật riêng tab (mục lục: `rules/00-muc-luc.md`).
