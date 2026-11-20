# 07_CSV — Tab CSV (quét thông tin tấm → data CSV → file CSV cuối)

Thay lisp `xuat_csv_v5.0.1` (ZWCAD + AUTOCAD). Đặc tả: [`_docs/specs/2026-11-19-tab-csv.md`](../_docs/specs/2026-11-19-tab-csv.md).

- Lệnh `CSV`: mở bảng OHTA, chuyển sang tab CSV.
- **Quét theo vùng / Quét toàn bản vẽ**: lisp cũ (`lisp/`, giai đoạn 1) phân tích hình học các khung được chọn (vùng kéo, hoặc mọi khung trên layer khung qua `ssget "_X"`) và ghi MText thông tin tấm; ngay sau đó lệnh `CSVQUET` đọc **mọi** khung có F-name trên bản vẽ — thông tin tấm lấy từ MText vừa quét, không có thì lấy từ `data\<đơn>-data.csv` theo tên N — ghi lại file data rồi **mới xoá MText** (ghi hỏng thì MText còn nguyên). Mỗi lần quét là một lần làm mới danh sách. Giá báo giá / tiền vật liệu / ưu đãi đã nhập được giữ khi quét lại.
- **Xuất CSV**: tạo file cuối `<đơn>.csv` (10 cột gửi nhà máy) từ danh sách tấm. Còn thẻ lỗi (kể cả tên trùng, thiếu vát / pitchi) hoặc file đang mở trong Excel thì không ghi.
- **Khung chú thích** (≥ 2 dòng `N=` trên layer F-name, như bảng mẫu trên bản vẽ) hiện thẻ xám "Bỏ qua", không chặn Xuất CSV, text trong nó không bị xoá.
- **Danh sách tấm** (thẻ, nút Xem chi tiết / rút gọn): sửa N/Q/M/T → ghi F-name trên bản vẽ (Ctrl+Z được); sửa lỗ / tapu / sara / pitchi / vát / gia công / giá → ghi `data\<đơn>-data.csv` ngay. Bấm thẻ → zoom; thẻ lỗi → zoom + vòng đỏ tạm (transient).
- **Chế độ tính** (Thường / Gia công / VL khách) dùng cho lần quét kế tiếp.
- ⚙ cài đặt (sắp xếp, layer khung / dim / tapu, bước spline, dung sai khe hở) lưu ở `HKCU\Software\OFT_Lisp\CSV`.

## Giai đoạn 2 (còn lại)
- Chuyển hình học B1 sang C#: `CSV--analyze-boundary`, `CSV--bend-list-smart`, `CSV--pricing-info`, `CSV--count-vat`, `CSV--scan-machining-texts`.
- Sau đó xoá `lisp/`, `CadServices/ChayB1Lisp.cs` (kèm đoạn ghi khoá registry cũ `HKCU\Software\Autodesk`).

## Cấu trúc
- `plugin/CsvTool.Plugin/Core/` — logic thuần (không CAD), test bằng `plugin/CsvTool.Tests`.
- `plugin/CsvTool.Plugin/CadServices/` — đọc / ghi bản vẽ.
- `lisp/` — lisp cũ chỉ để chạy B1 tới giai đoạn 2.
- `rules/` — luật riêng tab (mục lục: `rules/00-muc-luc.md`).
