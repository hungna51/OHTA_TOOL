# 02. Bảng lệnh

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Bảng lệnh

Tất cả nằm trong **một file** `guro ZWCAD(BG MK FN).lsp` (gộp từ 4 file AutoCAD gốc):

| Lệnh | Dòng | Chọn gì | Nhập gì | Tạo ra |
|---|---|---|---|---|
| **`gro`** | `c:gro` | — | — | **chỉ mở tab Guro** trên bảng OHTA (mục 193); không mở được tab thì báo lỗi (mục 194). Đánh số thật: `grochay` |
| **`grochay`** (thân `gro` cũ, nút ▶ Chạy GRO) | ~4670 | **polyline khung bao** (ENTER = quét chọn text thủ công) | layer chọn trên **tab Guro** cho từng bản vẽ, cài đặt ở nút ⚙ của tab (mục 192, 194): layer khung bao, **đánh số theo vùng**, layer vùng, **tự động in PDF**, **xử lý nét & chuyển layer**. **Mã khắc tự nhận diện** (mục 54, 56, 68) | **tất cả**: bango + mã khắc + F-name + dòng CSV + in PDF + chuyển đổi layer (mục 67, 69, 70) |
| `xln` | ~3464 | 1 đối tượng layer gốc (hoặc Enter nhập tên layer), quét vùng (Enter = cả layer) | — | Chuyển Continuous→`0-KAKOU`, Center1→`3-KEGAKI`, Text→`1-SUNPOU`, các nét khác giữ nguyên layer gốc (mục 69) |
| `cln` | ~3502 | (Tương tự `xln`) | — | Lệnh viết tắt của `xln` |
| `xlnds` | ~3504 | — | — | Mở hộp thoại `xln_dlg` xem nhóm nét chưa đổi và bấm "Chọn nhóm nét (Select)" để chọn trực tiếp (mục 69, 70) |
| `bg` | 577 | ghi chú TEXT/MTEXT | hộp thoại: số bắt đầu | TEXT bango + dòng CSV |
| `mk` | 1119 | TEXT bango đỏ layer `0` | mã khắc (`getstring`) | MTEXT mã khắc |
| `FN` | 1352 | MTEXT layer `3-KEGAKI` | — (đọc lại file CSV) | MTEXT F-name + đối chiếu mã khắc |
| `bgdbg` | 902 | 1 TEXT/MTEXT (`entsel`) | — | **không sửa gì** — in ra từng bước đọc chuỗi (**chỉ đúng cho `bg`**) |
| `grodbg` | 2204 | — | — | **không sửa gì** — chẩn đoán hộp thoại của `gro` |
| `grosort` | 2617 | ghi chú TEXT/MTEXT, rồi polyline (Enter để bỏ qua) | — | **không sửa gì** — chẩn đoán tách dữ liệu + gom hàng + chế độ vùng |
| `grovc` | ~3323 | 1 đối tượng (`entsel`), thường là TEXT bango bị báo "chua sach" | — | **không sửa gì** — liệt kê từng vật cản mà `ssget "_C"` của `gro--dem-va-cham` bắt được (mục 36) |
| `grovtdbg` | ~3323 | — | số tấm N (`getint`, 0 = tắt) | **không sửa gì** — bật cờ `gro--dbg-vc`: lần chạy `gro` kế tiếp in từng ứng viên vị trí + từng vật cản của N **ghi chú** đầu (mục 36, 40) |
| `grostyle` | ~4056 | — | — | **không sửa gì** — chẩn đoán kiểu chữ `guro`: đã có chưa, `entmake`/ActiveX đường nào chạy, và `(cons 7 …)` của `entmake` TEXT có thật sự ăn không (mục 46, 47). Tạo một TEXT thử rồi **xoá ngay** |
| `growp` | ~5050 | polyline (khung bao hoặc vùng) | — | **không sửa gì** — so `ssget "_X"`+lọc với `ssget "_CP"` từng vùng, để chốt có nên đổi cách quét không (mục 62). Chạy 2 lần: zoom hết và zoom sát |
| `oi` | ~6420 | polyline layer `Khung_IN` (kín, 4 đỉnh) | — | in **từng khung** ra PDF (tên kèm `-YYYYMMDD`), gộp bằng Ghostscript, mở file (mục 66, 67) |
| `GI` | ~6544 | — | — | in **toàn bộ extents** ra một PDF rồi mở (mục 66) |
| `ocsv` | 1048 | — | — | mở file CSV bằng ứng dụng mặc định |
| `dst` / `grods` / `ds` | ~8503 | — | sửa bango / T / Q / M ngay trong bảng | bảng danh sách tấm: palette .NET nếu đã nạp DLL, không thì DCL. Bản .NET ghi ngược vào bản vẽ (mục 81, 86) — **không** sửa CSV |
| `netdst` | ~8532 | — | — | NETLOAD **`GROZWCAD.dll`** (ưu tiên `C:/GROZWCAD/`, mục 179 — tên cũ `ZwCadPlugin.dll`) rồi mở bảng .NET. Từ mục 179 lisp tự nạp DLL ngay khi file `.lsp` được nạp. **Không cần gõ nữa**: `gro` tự nạp DLL và tự mở + làm mới bảng cuối lệnh (mục 86) |

> **`bgdbg` KHÔNG còn chẩn đoán đúng cho `gro`.** `bgdbg` chạy `bg--fields`, còn `gro` nay
> dùng đường tách dữ liệu riêng (`gro--fields` / `gro--tim-day` / `gro--tim-sl`). Muốn soi
> `gro` thì dùng **`grosort`** — nó in bảng `bango | hàng | X | Y | cao | t= | TH | SL |
> nội dung`, cho thấy đúng thứ tự đánh số và việc tách tấm **trước khi** chạy thật.

**Thứ tự thao tác của `gro`: chọn đối tượng TRƯỚC, hộp thoại hiện SAU.** Khác `bg` (gọi
`ssget` trước nhưng mãi sau hộp thoại mới kiểm tra `nil`, tức bắt điền xong mới báo không
chọn được gì). Nếu bỏ qua bước chọn (Enter/Esc) thì `gro` thoát luôn và **không có hộp
thoại nào hiện ra** — đó là hành vi đúng, không phải lỗi.

Khi hộp thoại của `gro` không hiện, chạy **`grodbg`**: nó in đường dẫn file DCL, kết quả
ghi file, **dump nguyên nội dung file DCL đã ghi ra** (để thấy chính xác ZWCAD đọc được gì,
kể cả các escape `\U+XXXX` còn nguyên dấu `\` hay không), rồi kết quả `load_dialog` /
`new_dialog` / `start_dialog` và giá trị đọc về từ hai ô.

**`gro` là lệnh nên dùng** cho quy trình bình thường: gộp cả `bg` + `mk` + `FN` vào một
lượt, chỉ chọn đối tượng một lần, một hộp thoại. Ba lệnh `bg` / `mk` / `FN` **giữ lại**
để chạy lẻ từng bước khi cần sửa chữa, và `FN` còn cần để đọc file CSV cũ từ phiên trước.

Lợi ích của `gro` không chỉ là giao diện: bango / mã khắc / F-name / dòng CSV của cùng một
tấm được tạo trong **cùng một vòng lặp từ cùng một biến `bango`**, nên:

- **Rủi ro ghép cặp sai biến mất.** `FN` duyệt selection set **ngược** rồi ghép với các dòng
  CSV theo thứ tự **xuôi** — lệch một nhịp là mọi F-name gắn sai tấm (xem
  [Cần kiểm tra lại](14-can-kiem-tra-lai.md)). `gro` không có khâu ghép cặp nào cả.
- **Phép kiểm tra "Error name!!!" không còn cần thiết** — nó so số tấm của mã khắc với cột N
  của CSV, mà nay hai thứ đó sinh ra từ cùng một biến nên không thể lệch. `gro` chỉ còn giữ
  `Error T!!!` và `Error Q!!!`.
- **Không còn CSV round-trip** — không phải ghi ra file rồi đọc lại để tạo F-name, nên không
  dính các lỗi BOM/encoding của khâu đọc lại (mục 3 phần FN, mục 8 phần ZWCAD).

> **Cảnh báo về tên lệnh.** Bảng cũ của file này ghi `ma_khac2.lsp` định nghĩa `mk2` và
> `ma_khac.lsp` định nghĩa `mks` — **sai với file đang dùng**: file gộp định nghĩa `c:mk`
> (chính là code của `ma_khac2` = bản hàng loạt) và **không có `c:mk2`, cũng không có
> `c:mks`**. `mk` đọc mã khắc từ registry qua `mk2--reg-get` nhưng **không hàm nào trong file
> này ghi vào registry**, nên giá trị mặc định chỉ có nếu đã từng chạy `mks` của
> `ma_khac.lsp` (file riêng, ở cùng thư mục). Lệnh `gro` không dùng registry.
