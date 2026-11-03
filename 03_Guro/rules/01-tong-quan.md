# 01. Tổng quan và quy ước bảo trì

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## HAI FILE .lsp — đọc trước khi sửa bất cứ thứ gì

| File | Dòng | Chứa gì | Dùng khi nào |
|---|---|---|---|
| `guro ZWCAD(BG MK FN).lsp` | ~3780 | **đầy đủ**: `gro`, `bg`, `mk`, `FN`, `ocsv` + 3 lệnh chẩn đoán `bgdbg` / `grodbg` / `grosort` | khi cần lần lỗi, hoặc chạy lẻ từng bước |
| `Guro ZWCAD(gro).lsp` | ~2047 | **chỉ `gro`** — 64 hàm, đúng bao đóng các hàm `c:gro` thực sự gọi tới | dùng hằng ngày |

Bản gọn **sinh ra bằng máy** từ bản đầy đủ: lấy bao đóng bắc cầu của đồ thị lời gọi bắt đầu
từ `c:gro`. Thân 64 hàm giữ lại **giống hệt từng byte** với bản đầy đủ — không sửa tay chữ nào.
Đã bỏ: 4 lệnh khác, 3 lệnh chẩn đoán, 2 hàm code chết, và các hàm riêng của `bg` / `FN`.

> ### ⚠ Hai bản KHÔNG tự đồng bộ với nhau
> Sửa bản này thì bản kia **không** đổi theo. Đây là chi phí thật của việc tách file, phải
> nhớ mỗi lần sửa. Cách làm an toàn: **coi bản đầy đủ là bản gốc duy nhất**, sửa ở đó rồi
> sinh lại bản gọn. Nếu sửa thẳng bản gọn thì phải chép ngược thủ công sang bản đầy đủ —
> và nhớ rằng bản đầy đủ mới là nơi có `grosort` / `grodbg` để lần lỗi.

Vài điểm kỹ thuật của bản gọn:
- **ASCII thuần 100%** (bản đầy đủ vẫn còn chú thích tiếng Việt có dấu ở các phần cũ). Đây là
  hệ quả may mắn: các hàm còn dấu đều nằm trong nhóm bị bỏ. Giữ nguyên trạng thái này.
- Cả hai bản đều ghi cùng một file DCL tạm `guro_gro.dcl` với nội dung y hệt — nạp cả hai
  file cùng lúc thì vô hại, `defun` sau ghi đè `defun` trước bằng code giống hệt.
- Bản gọn **không có** `grosort` — mất công cụ đã giúp tìm ra lỗi "đọc lại đầu ra của chính
  mình" ở mục 29. Gặp kết quả lạ thì nạp bản đầy đủ rồi chạy `grosort`.

## Chức năng

Đánh số bảng gô ("bango") lên các tấm kim loại sẽ cắt laser, đồng thời xuất thông tin ra
file CSV cạnh file DWG (phục vụ lấy dữ liệu từ file DXF), tạo mã khắc và tạo F-name.

Đọc ghi chú độ dày trên bản vẽ (TEXT/MTEXT có chứa ký tự `t`) rồi tách ra: `T=` độ dày,
`Q=` số lượng, `M=` vật liệu, `V=` cờ bẻ/vát. Sắp xếp theo toạ độ rồi đánh số tuần tự.

## QUY ƯỚC BẢO TRÌ — đọc trước khi sửa code

**Mỗi lần sửa lisp trong thư mục này phải cập nhật file CLAUDE.md ngay trong cùng lần làm
việc đó**, không để lại sau. Cụ thể phải xem lại các mục sau xem có còn đúng không:

| Sửa gì | Phải cập nhật mục nào của file này |
|---|---|
| **Bất cứ thứ gì liên quan `gro`** | **sinh lại `Guro ZWCAD(gro).lsp`** — hai bản không tự đồng bộ |
| Thêm/đổi tên/xoá lệnh | [Bảng lệnh](02-bang-lenh.md) |
| Thêm/đổi hàm helper | [Bản đồ hàm](03-ban-do-ham.md) |
| Đổi layer / màu / chiều cao / vị trí đặt chữ | [Bảng dữ liệu và layer](04-du-lieu-va-layer.md) |
| Đổi định dạng CSV | [Bảng dữ liệu và layer](04-du-lieu-va-layer.md) **và** kiểm tra lại lệnh `FN` |
| Thêm/đổi hộp thoại | [Hộp thoại (DCL)](05-hop-thoai-dcl.md) |
| Tìm ra / sửa được lỗi tương thích ZWCAD | [Lịch sử lỗi khi port](../../CLAUDE.md#mục-lục-33-mục-lỗi) |
| Phát hiện điểm còn nghi ngờ, chưa test | [Cần kiểm tra lại](14-can-kiem-tra-lai.md) |

Hai luật tuyệt đối khi viết code trong thư mục này:

1. **Không gõ ký tự tiếng Việt có dấu / tiếng Nhật trực tiếp vào file `.lsp`** — ZWCAD đọc
   source theo CP932 nên sẽ thành mojibake. `princ`/`alert` dùng **ASCII thuần**; nhãn hộp
   thoại dùng `\U+XXXX` (chỉ DCL mới dịch được escape này, `princ` thì in ra nguyên văn).
2. **Không viết lại bản sao của logic đã có** — dùng lại hàm trong [Bản đồ hàm](03-ban-do-ham.md).
   Đây là lý do `bgdbg` vẫn chẩn đoán đúng cho cả `bg` lẫn `gro`: cả hai gọi cùng `bg--fields`.
