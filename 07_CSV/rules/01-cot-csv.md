# 01 — Cột CSV, MText thông tin tấm, kiểm lỗi EXPORT

Nguồn: lisp `xuat_csv_v5.0.1` (`CSV--analyze-one-frame`, `CSV--pricing-data-from-text`, ráp dòng trong `c:csv`).
Mã C#: `plugin/CsvTool.Plugin/Core/` (`ThongTinTam.cs`, `PhanTichKhung.cs`, `CsvXuat.cs`).

## 2 file của EXPORT (ghi cả hai hoặc không ghi gì)

| File | Nội dung |
|---|---|
| `<thư mục dwg>\<đơn>.csv` | Gửi nhà máy. Tiêu đề `エクスポート用,数量,材質,板厚,noと安全,曲げ,X,Y,,単価` |
| `<thư mục dwg>\data\<đơn>-data.csv` | 35 cột cũ của lisp, giữ nguyên từng byte (đầu vào phần mềm tính giá) |

UTF-8 có BOM, CRLF sau mọi dòng. Mở độc quyền cả hai trước khi ghi: Excel đang mở 1 file → báo lỗi, không đổi file nào.

**Mã đơn**: tên file bỏ đuôi và đúng 1 hậu tố `-a`/`-s` (`129213-s.dxf` → `129213`). Không dùng cách cắt ký tự `- S A s a . d f x g w` của lisp (cắt nhầm mã đơn kết thúc bằng các chữ đó).

### `<đơn>.csv` (10 cột)
`N` · `Q` · `M` (giữ nguyên chuỗi, không đổi SPHC/SS400 như GRO) · `T` · `<đơn>-` · **曲げ** · `X` · `Y` (dim tổng, X ≥ Y) · *(trống)* · **単価** *(trống — chưa có cơ chế tính tiền)*.

**曲げ** chỉ dựa vào dòng "Các loại gia công" (đã chuẩn hoá): `m`/`m(pitchi)`→`m`, `k`, `s`, `t`, `r1`/`r2`→`r`; từ 2 loại trở lên → `*`; không có → trống. Mã lạ bị bỏ qua.

### `data\<đơn>-data.csv` (35 cột)
Đúng bảng cột trong luật lisp `05-csv-output.md`. Điểm dễ sai:
- X, Y ghi thẳng (không bọc nháy); số = `rtos` theo DIMDEC của dim + `$DIMZIN` bản vẽ (bit 8 bỏ 0 cuối, bit 4 bỏ 0 đầu).
- "Loại gia công" xuất nguyên danh sách sau khi đổi `m(pitchi)`→`m` và bỏ trùng (`"m, s"`, bọc nháy khi có phẩy).
- `Loai cuon`: chứa `R2` → `2`, `R1` → `1`, không → `0`.
- 8 cặp Bẻ: (Số đường, Dài), thiếu → `0`, quá 8 loại thì bỏ phần dư.
- 4 cột cuối giữ nguyên chuỗi người dùng gõ (trống → ô trống).

## Đọc text thông tin tấm (layer `KHUNG_IN`, TEXT hoặc MTEXT)
- Gom mọi dòng của mọi TEXT/MTEXT `KHUNG_IN` nằm trọn trong khung, rồi **đảo ngược toàn bộ** (lisp gom bằng `cons`) — thứ tự đường bẻ trong CSV phụ thuộc điều này.
- Chỉ nhận khi có dòng chứa `TAPPU` hoặc `TAPU&SARA`.
- So nhãn sau khi giải mã `\U+XXXX`, bỏ dấu, viết hoa → TEXT font SHX (lưu `\U+1ED7`) và TrueType (lưu chữ thật) cho cùng kết quả.
- Số dòng phải bằng `12 + (có "m" ? chỉ số đường bẻ lớn nhất : 0)`.

## Kiểm lỗi từng khung (thứ tự như lisp, gặp lỗi đầu là dừng)
1. F-name: trộn MTEXT + TEXT · nhiều hơn 1 MTEXT · không thấy · TEXT dưới 4 dòng (lisp bỏ qua) · trường N/M/T/Q rỗng.
2. Dim tổng (layer dim, cờ 70): dim thẳng `32/160` phải đúng 2; không có thì dim phi `3/35/163` phải đúng 1 (X = Y).
3. Text thông tin: không có / sai số dòng.
4. Có `k` mà vát ≤ 0 · có `pitchi` mà Pitchi số lần hoặc chiều dài ≤ 0.
5. Sau khi gom mọi khung: N trùng → không ghi.

## Test vàng (`CsvTool.Tests/TestVang.cs`)
Đọc `OneDrive - OFT\xxxxx\<đơn>\<đơn>-s.dxf` + `<đơn>.csv` (129213, 125953, 127143) — **không chép bản vẽ khách vào repo** (repo public). Máy không có thì bỏ qua.
- 125953: DXF ghi `T=6.0` nhưng CSV lisp ghi `6` — F-name bị chuẩn hoá lại sau khi xuất (DXF lưu sau CSV 2 giây). Test chỉ cho phép lệch dạng số ở cột Độ dày.
- Bản vẽ mẫu có khung `Khung_IN` chú thích / bảng mẫu (nhiều F-name mẫu, cả MTEXT lẫn TEXT) → `PhanTichKhung.LaKhungChuThich` (≥ 2 dòng `N=`) nhận ra; tab hiện thẻ "Bỏ qua", không chặn Xuất CSV, không xoá text trong nó.
- Từ v6.0.0 thông tin tấm lưu ở `data\<đơn>-data.csv` (ghép theo tên N), không để MText trên bản vẽ: `PhanTichKhung.Ghep` + `CsvXuat.DocData` (ngược của `Dong35`, test khứ hồi trong `TestDuLieu.cs`).
