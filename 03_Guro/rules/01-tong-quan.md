# 01. Tổng quan và quy ước bảo trì

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## File lisp — đọc trước khi sửa bất cứ thứ gì

Chỉ có **một** file lisp: [`lisp/guro_gro.lsp`](../lisp/guro_gro.lsp) — sửa trực tiếp, lịch sử nằm trong git của OHTA_TOOL. Nạp qua lệnh mồi [`lisp/autoload.lsp`](../lisp/autoload.lsp); phần C# ở `plugin/GroTool.Plugin` (DLL `GROTOOL.dll`). Xem [../README.md](../README.md).

- File giữ **100% ASCII** (`build_ohta.ps1` chặn nếu sai): chữ Nhật / ký hiệu trong chuỗi viết `\U+XXXX`, trong chú thích viết `U+XXXX`.
- Lệnh chẩn đoán `grosort` / `grodbg` / `bgdbg` chỉ có ở bản đầy đủ cũ `guro ZWCAD(BG MK FN).lsp` (không chuyển sang). Gặp kết quả lạ thì theo [15-phuong-phap-debug.md](15-phuong-phap-debug.md).

## Chức năng

Đánh số bảng gô ("bango") lên các tấm kim loại sẽ cắt laser, đồng thời xuất thông tin ra
file CSV cạnh file DWG (phục vụ lấy dữ liệu từ file DXF), tạo mã khắc và tạo F-name.

Đọc ghi chú độ dày trên bản vẽ (TEXT/MTEXT có chứa ký tự `t`) rồi tách ra: `T=` độ dày,
`Q=` số lượng, `M=` vật liệu, `V=` cờ bẻ/vát. Sắp xếp theo toạ độ rồi đánh số tuần tự.

## QUY ƯỚC BẢO TRÌ — đọc trước khi sửa code

**Mỗi lần sửa lisp / C# của Guro thì cập nhật file rule tương ứng ngay trong cùng lần làm
việc đó.** Cụ thể phải xem lại các mục sau xem có còn đúng không:

| Sửa gì | Phải cập nhật |
|---|---|
| Thêm/đổi tên/xoá lệnh | [Bảng lệnh](02-bang-lenh.md) **và** nút trong `../module.json`, lệnh mồi trong `../lisp/autoload.lsp` |
| Thêm/đổi hàm helper | [Bản đồ hàm](03-ban-do-ham.md) |
| Đổi layer / màu / chiều cao / vị trí đặt chữ | [Bảng dữ liệu và layer](04-du-lieu-va-layer.md) |
| Đổi định dạng CSV | [Bảng dữ liệu và layer](04-du-lieu-va-layer.md) **và** kiểm tra lại lệnh `FN` |
| Thêm/đổi hộp thoại | [Hộp thoại (DCL)](05-hop-thoai-dcl.md) |
| Tìm ra / sửa được lỗi tương thích ZWCAD / AutoCAD | [Mục lục mục lỗi](00-muc-luc-guro.md) (số mục mới = lớn nhất + 1) |
| Phát hiện điểm còn nghi ngờ, chưa test | [Cần kiểm tra lại](14-can-kiem-tra-lai.md) |

Hai luật tuyệt đối khi viết code trong thư mục này:

1. **File `.lsp` viết bằng ASCII thuần** — ZWCAD đọc source theo CP932, ký tự khác thành
   mojibake. `princ`/`alert` dùng tiếng Việt không dấu; nhãn hộp thoại DCL dùng `\U+XXXX`
   (trên ZWCAD chỉ DCL dịch escape này, `princ` in nguyên văn); so khớp ký tự Nhật theo **mã
   số** (`bg--codes` + `member`), xem [06-encoding-chuoi.md](06-encoding-chuoi.md).
2. **Dùng lại hàm đã có** trong [Bản đồ hàm](03-ban-do-ham.md) trước khi viết hàm mới.
