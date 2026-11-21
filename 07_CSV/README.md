# 07_CSV — Tab CSV (quét thông tin tấm → data CSV → file CSV cuối)

Thay lisp `xuat_csv_v5.0.1` (ZWCAD + AUTOCAD). Đặc tả: [`_docs/specs/2026-11-19-tab-csv.md`](../_docs/specs/2026-11-19-tab-csv.md).

- Lệnh `CSV`: mở bảng OHTA, chuyển sang tab CSV.
- **Quét theo vùng / Quét toàn bản vẽ** (lệnh `CSVQUETVUNG` / `CSVQUETTOAN`): C# phân tích hình học các khung được chọn, chỉ đọc bản vẽ, **không tạo, xoá hay zoom gì trên bản vẽ**; rồi đọc **mọi** khung có F-name — thông tin tấm lấy từ kết quả vừa quét, không có thì lấy từ `data\<đơn>-data.csv` theo tên N — và ghi lại file data. Mỗi lần quét là một lần làm mới danh sách. Pitchi, giá báo giá, tiền vật liệu, ưu đãi, vát đã nhập được giữ khi quét lại; cách chia Tapu / Sara được giữ khi tổng không đổi.
- **Lỗi và cảnh báo**: lỗi không ghi lên bản vẽ và không dừng lượt quét, mọi lỗi gom vào danh sách đầu tab, **mỗi tấm báo đủ các lỗi trong một lần quét**. Bấm một dòng để zoom sát vị trí lỗi, bấm tiếp để sang vị trí kế. Đối tượng lỗi được bao bằng cloud đỏ vẽ tạm (không lưu, không in, không thuộc layer), mất khi chọn tấm khác, quét lại, đổi bản vẽ hoặc bấm Esc trên tab. Lỗi chặn Xuất CSV tới khi sửa và quét lại khung đó.
- **Nét trắng sai layer**: nét (LINE, ARC, CIRCLE, ELLIPSE, SPLINE, polyline) màu trắng trong khung tấm nhưng không ở `0-KAKOU` bị báo lỗi và chặn Xuất CSV, vì file cắt sẽ mất nét / mất lỗ. Tính cả màu theo layer và ByBlock; không tính layer khung và nét ngoài khung.
- **Xuất CSV**: tạo file cuối `<đơn>.csv` (10 cột gửi nhà máy) từ danh sách tấm. Còn thẻ lỗi (kể cả tên trùng, thiếu vát / pitchi) hoặc file đang mở trong Excel thì không ghi.
- **Tính giá** (giai đoạn 1): công thức của web `price-calc` đã chuyển sang C# (`Core/TinhGia.cs`), nhưng **chưa nối nguồn bảng giá** nên nút báo "Chưa kết nối bảng giá". Nối nguồn = viết 1 lớp `INguonBangGia`; repo public nên không được để giá hay mật khẩu database trong code. Công ty, cờ giá tự tính, loại pitchi, giá cuộn, giá đặt ngoài lưu ở `data\<đơn>-gia.json`. Giá không có cờ tự tính là giá tay và không bị tính đè.
- **Khung chú thích** (≥ 2 dòng `N=` trên layer F-name, như bảng mẫu trên bản vẽ) hiện thẻ xám "Bỏ qua", không chặn Xuất CSV.
- **Danh sách tấm** (thẻ, nút Xem chi tiết / rút gọn): sửa N/Q/M/T → ghi F-name trên bản vẽ (Ctrl+Z được); sửa lỗ / tapu / sara / pitchi / vát / gia công / giá → ghi `data\<đơn>-data.csv` ngay. Bấm thẻ → zoom vừa cả tấm, hiện cloud ở mọi đối tượng lỗi của tấm.
- **Chế độ tính** (Thường / Gia công / VL khách) dùng cho lần quét kế tiếp.
- ⚙ cài đặt (sắp xếp, layer khung / dim / tapu, bước spline, dung sai khe hở) lưu ở `HKCU\Software\OFT_Lisp\CSV`.

## Cấu trúc
- `plugin/CsvTool.Plugin/Core/` — logic thuần (không CAD), test bằng `plugin/CsvTool.Tests`. `PhanTichHinh.cs` là phần hình học thay lisp.
- `plugin/CsvTool.Plugin/CadServices/` — đọc / ghi bản vẽ.
- `rules/` — luật riêng tab (mục lục: `rules/00-muc-luc.md`).
