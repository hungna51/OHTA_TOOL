# 13. Lệnh cũ: FN và ma_khac

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## `lisp_xuat_Fname(FN).lsp` — các lỗi đã phát hiện và sửa khi port

Bản gốc AutoCAD giữ nguyên tại `laptrinhlisp\lisp_xuat_Fname(FN).lsp`.

1. **Treo vĩnh viễn khi gặp dòng CSV lỗi.** 4 vòng `while` quét ký tự tìm dấu `,`/`;`
   không có điều kiện thoát khi hết chuỗi — `(substr fc i 1)` quá độ dài trả `""`, khác cả
   `","` lẫn `";"` nên lặp mãi. Thay bằng hàm `fn--split` duyệt có giới hạn theo `strlen`.
2. **`\U+000a` KHÔNG phải là LF** — bộ đọc chuỗi AutoLISP giữ nguyên 7 ký tự literal
   (đã xác minh: 3 hằng số `20`/`17`/`17` trong `cond` báo lỗi chỉ đúng khi tính 7 ký tự).
   Hệ quả: mọi `alert`/`princ` dùng `\U+XXXX` hiện ra nguyên văn chứ không thành tiếng Việt,
   và toàn bộ khối báo lỗi phụ thuộc vào con số byte trùng khớp — lệch 1 ký tự là **nuốt im
   lặng** mọi báo lỗi. Đã thay bằng cờ list rỗng/không rỗng + thông báo ASCII thuần.
3. **BOM UTF-8 lọt vào trường đầu tiên.** `guro.lsp` giờ ghi CSV dạng UTF-8 **có BOM**, mà
   `(read-line)` trả 3 byte BOM như 3 ký tự thường → `N=` của F-name đầu tiên dính rác.
   Thêm `fn--strip-bom`.
4. **So sánh độ dày bằng chuỗi.** `(member Tt '("0.6" ... "4.5" ...))` phụ thuộc vào việc
   `guro.lsp` ghi cột T bằng `(rtos (atof ...))` **không chỉ định precision** → giá trị thật
   phụ thuộc biến hệ thống `LUPREC` (có thể ra `"4.5000"`). Đã chuyển sang so sánh số thực
   có sai số (`fn--member-num`).
5. **Cờ `V=` không bao giờ khớp.** Bản gốc kiểm tra `(substr fc (- (strlen fc) 2) 1)` = ký
   tự thứ 3 từ cuối, trong khi cờ V của `guro.lsp` là trường **cuối cùng**. Đã đọc theo
   trường số 5. Ghi `V=` cho cả `"m"` (bẻ) lẫn `"*"` (bẻ + vát), không ghi cho `"k"`.
6. **`entmake` thất bại im lặng** nếu bản vẽ không có text style tên `"Text"` — không tạo ra
   MTEXT nào mà cũng không báo gì. Đã guard bằng `tblsearch` + fallback `TEXTSTYLE`, và
   kiểm tra giá trị trả về của `entmake`.
7. **`(command ".change" a "" "p" "color" ...)`** → thay bằng `vla-put-color`, tránh phụ
   thuộc lệnh dòng lệnh của ZWCAD (lệnh lạ chỉ *in* "Unknown command" chứ không ném lỗi bắt
   được, và tham số phía sau bị đẩy vào dòng lệnh như lệnh rác).
8. Thêm `(vl-load-com)`, `*error*` handler, kiểm tra `nil` sau `ssget`/`open`; đọc hết CSV
   rồi đóng file ngay thay vì giữ mở suốt vòng lặp; khôi phục `CMDECHO` về giá trị cũ
   (bản gốc `setvar` cứng về `1`); số dòng CSV trong báo lỗi trước đây bị lệch `+1`.
9. Không dùng `getpropertyvalue` để đọc MTEXT — đọc thẳng group 3 + group 1 rồi khử mã định
   dạng, vì `vl-catch-all-apply` **không** bắt được lỗi "undefined function".

File hiện là **ASCII thuần 100%** nên không còn rủi ro lệch encoding UTF-8/CP932.

## `ma_khac.lsp` / `ma_khac2.lsp` — các lỗi đã phát hiện và sửa khi port

Bản gốc AutoCAD giữ nguyên tại `laptrinhlisp\1. xuất fname ra csv\ma_khac.lsp` và
`...\.vscode\makhac2.lsp`. Trước lần port này cả hai chưa hề được sửa (byte-identical
với bản AutoCAD).

1. **Lỗi ngay lúc `load` trên máy mới** — khối khởi tạo cuối `ma_khac.lsp` viết
   `((vl-registry-write ...))`, thừa một lớp ngoặc: AutoLISP tính ra chuỗi `"I"` rồi gọi
   `("I")` như một hàm → *bad function*. Chỉ xảy ra khi key `mks` chưa tồn tại, tức đúng
   lần chạy đầu tiên trên máy mới; lần thứ hai lại chạy được vì giá trị đã kịp ghi.
2. **Tiếng Việt có dấu gõ trực tiếp** trong `princ`/`getstring` → mojibake trên ZWCAD
   (ZWCAD đọc source `.lsp` theo CP932). Cả hai file giờ là **ASCII thuần 100%**.
3. **`CMDECHO` không bao giờ được khôi phục** — `ma_khac.lsp` đặt `0` ở đầu rồi đặt `0`
   thêm lần nữa ở cuối (có lẽ gõ nhầm `1`), nên chạy `mk` một lần là tắt echo dòng lệnh
   cho cả phiên. Đã lưu và trả lại giá trị cũ.
4. **DXF group `72`/`73` sai cho MTEXT** — với MTEXT thì `72` là hướng viết (1..5) và `73`
   là kiểu giãn dòng (chỉ nhận 1 hoặc 2); giá trị `0` của bản gốc đều không hợp lệ nên
   ZWCAD có thể **từ chối `entmake` im lặng**. Đã bỏ, thay bằng `71` (điểm căn). Group
   `40` cũng đổi từ `10` (số nguyên) sang `10.0` vì DXF group 40 đòi số thực.
5. **Không kiểm tra loại đối tượng được chọn** — chọn trúng line/circle thì
   `(cdr (assoc 1 ...))` trả `nil` và `(strcat b "-" nil)` crash. Đã lọc `TEXT,MTEXT`.
   Tương tự, `b` (mã khắc đọc từ registry) có thể `nil` nếu chưa chạy `mks` bao giờ.
6. **`(nth nn nlst)` trong `makhac2`** — trên ZWCAD `(nth n nil)` **ném lỗi**
   `invalid argument type: consp nil` thay vì trả `nil` như AutoCAD (skill mục 8), nên
   danh sách rỗng là crash. Đã đổi sang `foreach`. `ssget` cũng chưa được kiểm tra `nil`.
7. `(command ".change" e "" "p" "color" "3" "")` → `vla-put-color`; thêm `*error*` handler
   cho cả hai file; MTEXT dài > 250 ký tự giờ đọc cả group 3 chứ không chỉ group 1.
8. Registry key **giữ nguyên** `HKCU\Software\Autodesk\mks` để AutoCAD và ZWCAD dùng chung
   một giá trị mã khắc.

## `innhanh(OI)-CaNhan-ZWCAD.lsp` — đã gộp vào file guro (mục 66)

Bản gốc **vẫn giữ nguyên** tại `laptrinhlisp\innhanh(OI)-CaNhan-ZWCAD.lsp`. Hai file nay
**không tự đồng bộ** — cùng cảnh báo như hai bản `.lsp` của guro.

| Lệnh / hàm | Làm gì |
|---|---|
| `c:oi` | chọn polyline layer `Khung_IN` (kín, **đúng 4 đỉnh**) → in **từng khung** ra PDF tạm → gộp bằng Ghostscript → mở file |
| `c:GI` | in **toàn bộ extents** ra một PDF rồi mở |
| `OI--dwg-base` | tên bản vẽ đã cắt đuôi — **trùng công thức với `don` của `gro`** |
| `OI--available-pdf-name` | tìm tên `… (1).pdf`, `… (2).pdf` chưa dùng, không bao giờ ghi đè |
| `OI--open-file` / `OI--merge-pdfs` | mở file / gộp PDF, đều qua COM `WScript.Shell` |
| `OIN_IN` | in một khung (plot window), chọn ngang/dọc theo góc khung |
| `LM:lwvertices1` | đọc đỉnh LWPOLYLINE (`trans` sang UCS) |

### Ba sửa đổi khi gộp — ghi rõ để còn đối chiếu

1. **Toàn bộ chú thích/chuỗi đổi sang ASCII thuần.** Bản gốc có tiếng Việt có dấu; ZWCAD đọc
   source theo CP932 nên sẽ thành mojibake. Đây là **luật tuyệt đối** của thư mục này.
   Chuỗi `alert` cũng đổi theo.
2. **Khai báo `xmax` `ymin` `ang` cho `c:oi`** — trước đây ba biến này rò ra toàn cục.
3. **Thêm `*error*` và lưu/trả lại `CMDECHO`, `FILEDIA`, `BACKGROUNDPLOT`** thay vì đặt cứng
   về `1`. Trước đây huỷ ngang giữa lệnh làm **cả phiên ZWCAD kẹt `FILEDIA` = 0** — mọi hộp
   thoại chọn file đều không hiện nữa. Cùng loại lỗi với `ma_khac` mục 3.

Logic sáu hàm phụ đã đối chiếu với bản gốc (bỏ chú thích và khoảng trắng): **giống hệt**.

### Điểm đáng nhớ nhất cho bước sau

`c:oi` lọc polyline layer `Khung_IN`, **kín, đúng 4 đỉnh** — **y hệt luật khung bao của `gro`**
(mục 51). "Khung in" và "khung bao" thực chất là **một thứ**, chỉ khác tên layer. Đây là chỗ
để `gro` tự in mà không phải chọn lại gì.

> Hai hằng số phụ thuộc máy, chưa kiểm lại sau khi gộp: `*OI-GS-EXE*` (đường dẫn Ghostscript)
> và tên máy in `"DWG to PDF.pc5"` + khổ giấy `"ISO full bleed A3 (420.00 x 297.00 MM)"`.
> `c:oi` dùng `monochrome.ctb`, còn `c:GI` dùng `monochrome-guro.ctb` — **khác nhau**, chưa rõ
> có chủ ý hay không.

### 4. Tự động gắn hậu tố ngày tháng dạng `-YYYYMMDD` (Mục 67)

Hàm `OI--pdf-base-name` tự động lấy ngày tháng hiện tại của hệ thống qua biến hệ thống `CDATE` (ví dụ `(getvar "CDATE")` $\rightarrow$ `20261002.1950...`) và trích xuất 8 ký tự đầu tiên để gắn vào đuôi tên file PDF:
```text
<tên bản vẽ đã cắt đuôi>-YYYYMMDD.pdf
```
Ví dụ: Bản vẽ `ABC-01.dwg` in trong ngày 02/10/2026 sẽ sinh ra file `ABC-01-20261002.pdf`. Nếu tên file đã tồn tại, hàm `OI--available-pdf-name` sẽ tự động nối thêm `(1)`, `(2)`... tránh ghi đè.

### 185. In PDF tự động không chạy trên MÁY KHÁC — thiếu `gro.ctb`, lệnh `-PLOT` lệch câu trả lời mà vẫn báo "Da in" (người dùng báo, 29/10/2026)

**Log máy kia:**
```
*** Khong tim thay bang kieu in "gro.ctb".
    Chep file do vao: C:\Users\opena\AppData\Roaming\ZWSoft\ZWCAD\2026\en-US\Printstyle\
<gro.ctb> not found.<_Y.ctb> not found.<_A.ctb> not found.<...130352-20261029.pdf.ctb> not found.<_N.ctb> not found.<_Y.ctb> not found.
Da in ban chup (1 khung tren layer "0") ra 1 file PDF (kieu GI):
```

**Ba lỗi:**
1. `gro.ctb` **chỉ có trong Printstyle của máy này** — không có bản nào "đi kèm cùng thư mục với file .lsp" như thông báo nói. `gro--bao-dam-ctb` chỉ tìm bằng `(findfile ten)` (Support path) → máy mới không bao giờ tự chép được.
2. Thiếu ctb mà vẫn gửi `"gro.ctb"` cho `-PLOT` → ZWCAD hỏi lại tên ctb, **mọi câu trả lời phía sau** (`_Y`, `_A`, đường dẫn PDF, `_N`, `_Y`) bị nuốt làm tên ctb → không in.
3. `gro--in-nhanh-pdf` in `Da in ... ra 1 file PDF` **vô điều kiện** (lệnh `GI` thì đã kiểm `findfile`).

**Sửa (lisp, ASCII, không thêm hàm):**
- `gro--bao-dam-ctb` nay **trả tên ctb dùng được** (nil = không có). Tìm nguồn để chép theo thứ tự: Support path → thư mục chứa `Guro ZWCAD(gro).lsp` → thư mục chứa `GROZWCAD.dll` (`gro--tim-dll`) → `C:/GROZWCAD/`. Không có → dự phòng **`Monochrome.ctb`** (đi kèm ZWCAD; màu đỏ in ra đen) + báo rõ; không có cả hai → **bỏ qua in** và báo, không gửi `-PLOT` với tên sai.
- `gro--in-nhanh-pdf` / `gro--in-pdf-vung` (`GI`) dùng tên ctb trả về; chỉ báo `Da in` khi file PDF **có thật**, không thì `*** In PDF THAT BAI ...`.
- **Đặt sẵn `gro.ctb`** (4 419 byte, bản trong Printstyle máy này) ở `ZWLISP/GURO/gro.ctb`, `C:/GROZWCAD/gro.ctb` và `laptrinhlisp/gro.ctb` — **chép thư mục `C:\GROZWCAD\` sang máy khác là có cả DLL lẫn ctb**.

- Lisp: ngoặc cân bằng, 388 hàm không đổi, 16 080 → 16 103 dòng, CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc185._ls`.

> **Chưa chạy trên máy kia.** Nghiệm thu: chép `Guro ZWCAD(gro).lsp` mới + `C:\GROZWCAD\gro.ctb` sang máy đó, chạy `gro`: phải thấy `Da chep gro.ctb vao ...Printstyle\ (tu ...)` (lần đầu) rồi `Da in ban chup ...` và file PDF mở ra.
