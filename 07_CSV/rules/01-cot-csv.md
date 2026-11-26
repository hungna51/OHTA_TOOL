# 01 — Cột CSV, thông tin tấm, kiểm lỗi

Nguồn: lisp `xuat_csv_v5.0.1` (`CSV--analyze-one-frame`, ráp dòng trong `c:csv`).
Mã C#: `plugin/CsvTool.Plugin/Core/` (`ThongTinTam.cs`, `PhanTichKhung.cs`, `CsvXuat.cs`).

## 2 file của EXPORT (ghi cả hai hoặc không ghi gì)

| File | Nội dung |
|---|---|
| `<thư mục dwg>\<đơn>.csv` | Gửi nhà máy. Tiêu đề `エクスポート用,数量,材質,板厚,noと安全,曲げ,X,Y,,単価` |
| `<thư mục dwg>\data\<đơn>-data.csv` | 34 cột, **định dạng chung với web price-calc** (nhập / xuất file data của web) |

UTF-8 có BOM, CRLF sau mọi dòng. Mở độc quyền cả hai trước khi ghi: Excel đang mở 1 file → báo lỗi, không đổi file nào.

**Mã đơn**: tên file bỏ đuôi và đúng 1 hậu tố `-a`/`-s` (`129213-s.dxf` → `129213`). Không dùng cách cắt ký tự `- S A s a . d f x g w` của lisp (cắt nhầm mã đơn kết thúc bằng các chữ đó).

### `<đơn>.csv` (10 cột)
`N` · `Q` · `M` (giữ nguyên chuỗi, không đổi SPHC/SS400 như GRO) · `T` · `<đơn>-` · **曲げ** · `X` · `Y` (dim tổng, X ≥ Y) · *(trống)* · **単価** *(trống — chưa có cơ chế tính tiền)*.

**曲げ** chỉ dựa vào cột "Loại gia công": `m`/`m(...)`→`m`, `k`, `s`, `t`, `r1`/`r2`→`r`; từ 2 loại trở lên → `*`; không có → trống. Mã lạ bị bỏ qua.

### `data\<đơn>-data.csv` (34 cột, chung với web)
`Ten tam, Vat lieu, Do day, X, Y, So luong, Lo nho, Lo lon, Lo Tappu, Lo Sara, Pitchi so lan, Pitchi dai, Loai gia cong, Vat, Be1..Be8 (so duong, dai), Che do tinh, Gia bao gia, Tien vat lieu(nhap san), Gia tay` (`CsvXuat.TieuDe34`). Điểm dễ sai:
- X, Y ghi thẳng (không bọc nháy); số = `rtos` theo DIMDEC của dim + `$DIMZIN` bản vẽ (bit 8 bỏ 0 cuối, bit 4 bỏ 0 đầu).
- "Loại gia công" là **nơi duy nhất** lưu loại pitchi và cuộn: `m` = có đường bẻ, `m(pitchi | non | nonlech | kakumaru | kakumarulech)` = có pitchi + loại, `r1` / `r2` = cuộn nửa / cả hình, `t`, `s`, `k`. Ô Loại pitchi và ô Cuộn trên thẻ tấm sửa thẳng mã này (`CsvXuat.DatLoaiPitchi`, `DatCuon`). Bỏ trùng, bọc nháy khi có phẩy. So mã từng cái (`TachMa`), không tìm chữ trong cả chuỗi (`m(kakumaru)` có chữ k nhưng không phải vát).
- 8 cặp Bẻ: (Số đường, Dài), thiếu → `0`, quá 8 loại thì bỏ phần dư.
- Che do tinh `0/1/2`, Gia bao gia, Tien vat lieu giữ nguyên chuỗi người dùng gõ. **Gia tay** `1` = giá sửa tay, tính lại không đè.
- **File cũ 35 cột** (lisp, còn `Loai cuon` sau Loại gia công và `Uu dai(%)` cuối): `DocData` đọc được, nhận ra theo số cột (≥ 35). Mở bản vẽ thì tab tự chuyển sang 34 cột (`CsvTabViewModel.ChuyenDataCu`): bản cũ giữ ở `data\<đơn>-data.cu.csv`; cờ giá tay, loại pitchi, cuộn chọn tay lấy từ `-gia.json` cũ (`TuDong`, `LoaiPitchi`, `DonGiaCuon`) rồi xoá khỏi json. Lisp ghi `m(pitchi)` thành `m`, nên dòng cũ có Pitchi số lần > 0 được dựng lại mã `m(loại)`, `m` chỉ còn khi có đường bẻ. File đang mở thì để lần mở sau.
- `-gia.json` chỉ còn thông tin riêng của OHTA TOOL: công ty, có tính giá sau quét, chế độ tính của đơn, tấm chọn chế độ riêng.

## Nguồn thông tin tấm
Kết quả quét của khung → `data\<đơn>-data.csv` theo tên N → không có thì báo "Chưa có thông tin tấm". **Không đọc text trên layer `KHUNG_IN`**, text còn sót trên bản vẽ cũ bị bỏ qua.

## Kiểm lỗi từng khung
1. F-name: trộn MTEXT + TEXT · nhiều hơn 1 MTEXT · không thấy · TEXT dưới 4 dòng (lisp bỏ qua) · trường N/M/T/Q rỗng.
2. Dim tổng (layer dim, cờ 70): dim thẳng `32/160` phải đúng 2; không có thì lấy nếu có đúng 2 dim Aligned `33/161`, 1 cái nằm ngang và 1 cái thẳng đứng (xiên thì báo lỗi); không có nữa thì dim phi `3/35/163` phải đúng 1 (X = Y).
3. Biên dạng: xem `PhanTichHinh`, báo đủ mọi lỗi của tấm. Gồm **nét trắng sai layer**: curve trong khung, màu trắng (ACI 7, RGB 255, ByBlock, hoặc ByLayer với layer trắng), không ở `0-KAKOU` / `2-F-NAME` / layer khung.
4. Có `k` mà vát ≤ 0 · có `m(...)` mà Pitchi số lần hoặc chiều dài ≤ 0.
5. Sau khi gom mọi khung: N trùng → không ghi.

## Test vàng (`CsvTool.Tests/TestVang.cs`)
Đọc `OneDrive - OFT\xxxxx\<đơn>\<đơn>-s.dxf` + `<đơn>.csv` (129213, 125953, 127143) — **không chép bản vẽ khách vào repo** (repo public). Máy không có thì bỏ qua.
- 125953: DXF ghi `T=6.0` nhưng CSV lisp ghi `6` — F-name bị chuẩn hoá lại sau khi xuất (DXF lưu sau CSV 2 giây). Test chỉ cho phép lệch dạng số ở cột Độ dày.
- Bản vẽ mẫu có khung `Khung_IN` chú thích / bảng mẫu (nhiều F-name mẫu, cả MTEXT lẫn TEXT) → `PhanTichKhung.LaKhungChuThich` (≥ 2 dòng `N=`) nhận ra; tab hiện thẻ "Bỏ qua", không chặn Xuất CSV, không xoá text trong nó.
- Từ v6.0.0 thông tin tấm lưu ở `data\<đơn>-data.csv` (ghép theo tên N): `PhanTichKhung.Ghep` + `CsvXuat.DocData` (ngược của `Dong34`, test khứ hồi + chuyển file cũ trong `TestDuLieu.cs`). File mẫu lisp vẫn 35 cột: test vàng chuyển mẫu sang 34 cột rồi so, mã gia công so theo cách lisp (`m(...)` → `m`).
- Quét **chỉ đọc bản vẽ**: `QuetHinhCad` đọc Database (bỏ layer tắt / đóng băng), `PhanTichHinh` tính trên đa giác đã làm phẳng thay REGION của lisp (sai số dây cung 0.005, ngưỡng cắt thật 0.01 để 2 cung tiếp xúc không bị tính là giao). Test đối chiếu CSV lisp: `DoiChieuHinh` trong `TestHinh.cs`. Quét lại giữ Pitchi (cả loại pitchi), giá, cờ giá tay, tiền vật liệu, vát đã nhập; bẻ, r1/r2, Tapu, Sara luôn lấy lại theo bản vẽ.
- Lỗi quét không ghi lên bản vẽ và không dừng lượt quét (không đổi màu khung, không MText lỗi, không alert, không `-ERROR.dwg`): `PhanTichHinh.Chay` trả **mọi** `LoiQuet` của tấm kèm hộp vị trí (nét đã báo lỗi được loại ra rồi kiểm tiếp; biên ngoài còn hở thì không kết luận "nét thừa", chỉ kiểm các vòng kín có chồng nhau không), tab gom vào danh sách lỗi. Lỗi chặn Xuất CSV tới khi quét lại khung đó, cảnh báo thì không. Riêng trường hợp đường bẻ phải nhập tay vẫn hỏi trên dòng lệnh.
