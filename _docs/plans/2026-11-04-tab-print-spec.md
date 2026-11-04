# Spec — Tab PRINT (lệnh OI v2.0)

Chốt qua buổi hỏi đáp 04/11/2026. Thay lisp `innhanh(OI)-CaNhan.lsp` bằng tab C# trong OHTA TOOL.

## Vị trí & nhận diện
- Thư mục `04_In/`, `module.json`: `id` = `print`, `tabTitle` = `PRINT`, `title` = `OI`, `version` = `2.0`, `accentColor` = `#2E7D32`, `order` = `40` (sau Guro = 30).
- Header trong tab hiện `OI · v2.0`. Nút ⚙ nền trắng, bánh răng tô màu tab (như Fname/Guro).
- Lệnh .NET `OI` (trong `OHTATOOL.dll`) mở bảng OHTA và chuyển sang tab PRINT; thay lisp `OI` cũ. `GI` của Guro giữ nguyên.
- Giao diện WPF trong `OHTATOOL.dll`, đăng ký qua `ModuleViewRegistry` (không DLL riêng).

## Mặt tab
- Layer khung, Cấu hình in, Máy in, Khổ giấy, Bảng ctb.
- Chế độ: Theo khung / Vùng quét / Extents.
- Ô chọn: Gộp 1 file, Mở sau khi in.
- Nút: "In tất cả khung", "Chọn khung để in" (chế độ Theo khung); "In" (Vùng quét / Extents).
- Dòng cảnh báo màu cam dưới ô Cấu hình (xem dưới). Không chặn nút In.

## Cấu hình in (⚙)
- ⚙ chứa: giá trị mặc định (máy in, khổ giấy, ctb, gộp 1 file, mở sau khi in), danh sách tên layer khung ưu tiên (mặc định `Khung_IN`), quản lý cấu hình (thêm / sửa / xoá, tên do người dùng đặt).
- Một cấu hình lưu: máy in, khổ giấy, ctb, gộp 1 file, mở sau khi in. Không lưu layer khung, chế độ.
- Nét in (lineweight) không có ô cài đặt (bỏ 04/11/2026): luôn theo page setup của bản vẽ, không đặt thì bật.
- Ô Cấu hình luôn có mục đầu **"Theo bản vẽ"**: từng thông số — bản vẽ (page setup của Model) có và máy có → dùng; không thì mặc định ⚙.
- Chọn cấu hình có tên → cấu hình thắng bản vẽ. Thông số của cấu hình mà máy không có → thay bằng của bản vẽ, không có nữa thì mặc định ⚙; hiện cảnh báo trên palette dạng `⚠ Không có bw.ctb → đang dùng monochrome.ctb (theo bản vẽ)`.
- Cảnh báo hiện ngay khi chọn cấu hình và cập nhật khi đổi bản vẽ.
- Nhớ cấu hình chọn lần trước (theo máy).
- Lưu ở registry `HKCU\Software\OFT_Lisp\IN`.
- Layer khung: tự chọn layer đầu tiên trong danh sách ưu tiên mà bản vẽ có (không phân biệt hoa thường).

## In
- Khung in = LWPOLYLINE kín, 4 đỉnh, trên layer khung đang chọn, trong Model. "In tất cả khung" lấy mọi khung; "Chọn khung để in" cho quét chọn và lọc khung hợp lệ. Không có khung → báo, không in.
- Thứ tự: theo Y của điểm cao nhất mỗi khung, giảm dần; Y lệch ≤ 1 mm coi như bằng → khung bên trái (Xmin nhỏ) trước. Không gom hàng theo luật 08.
- Hướng giấy: khung cao ≥ rộng → dọc, ngược lại ngang. Fit, căn giữa.
- Trước khi in tắt plottable các layer `CANH_BAO_*` và `GRO_XEP_NHOM` (giống GRO, không bật lại).
- Vùng quét: pick 2 góc, 1 trang, hướng theo tỷ lệ vùng. Extents: toàn bản vẽ 1 trang, ngang.
- In bằng API `PlotEngine` (cả ZWCAD + AutoCAD). Gộp PDF bằng PdfSharp.

## File PDF
- Cạnh file DWG, tên `<tên bản vẽ>.pdf`; bỏ đúng hậu tố `-S` / `-A` cuối tên (không phân biệt hoa thường).
- Đã có file: hộp thoại **Ghi đè / Lưu thành (1) / Huỷ in**. `(1)` đã có → `(2)`, `(3)`… không hỏi lại. Chọn Ghi đè mà file đang bị khoá (đang mở) → lưu thành `(n)` và báo.
- ⚙ có ô "Không hỏi khi trùng tên" (thêm 04/11/2026, mặc định tắt): bật thì trùng tên tự lưu `<tên> (n).pdf`, không hỏi.

## Giả định do người viết plan chọn (spec chưa nói — xác nhận khi duyệt plan)
- A1. Bỏ "Gộp 1 file": mỗi khung một file `<tên>-1.pdf`, `<tên>-2.pdf`…; kiểm tra trùng / hỏi ghi đè một lần cho cả bộ.
- A2. "Mở sau khi in" khi không gộp: mở thư mục chứa file.
- A3. Bản vẽ chưa lưu lần nào (không có thư mục): báo "Hãy lưu bản vẽ trước khi in", không in.
- A4. Mặc định ⚙ ban đầu: ZWCAD `DWG to PDF.pc5`, AutoCAD `DWG To PDF.pc3`; khổ `ISO_full_bleed_A3_(420.00_x_297.00_MM)`; ctb `monochrome.ctb`; gộp bật; mở bật.
