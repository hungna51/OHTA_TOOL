# 11. Hàm/API ZWCAD khác AutoCAD

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Các lỗi đã phát hiện và sửa

1. **`acet-str-find`** (AutoCAD Express Tools, không có trên ZWCAD) trong hàm dead-code
   `export-string-a2b` → thay bằng `vl-string-search` (portable).

3. **`getpropertyvalue` không tồn tại trên ZWCAD 2026** — đã xác minh thực tế bằng lỗi runtime
   `Loi: undefined function - GETPROPERTYVALUE` khi chạy `bg`. Bọc `vl-catch-all-apply` KHÔNG
   cứu được (lỗi xảy ra ở bước tra cứu symbol, trước khi kịp bọc). `bg--mtext-text` giờ đọc
   thẳng DXF (gộp mọi group 3 + group 1 cuối, để không cắt cụt MTEXT > 250 ký tự) rồi khử mã
   định dạng MTEXT (`\P` → dấu cách, `{`/`}`), giống `fn--ent-text`.

4. **`startapp`** bị chặn hoàn toàn trong môi trường ZWCAD test (kể cả `notepad.exe` không
   tham số) → `c:ocsv` chuyển sang gọi qua COM `Shell.Application`/`ShellExecute`.

13. **Bộ lọc `ssget` dạng danh sách phẩy `(0 . "TEXT,MTEXT")` — nghi phạm chính của lỗi
   "`bg` không ăn MTEXT".** Triệu chứng thực tế người dùng báo: chạy `bg`, lệnh **kết thúc
   bình thường nhưng ra 0 tấm**, trong khi cùng bản vẽ đó chọn TEXT thì đúng. Khâu đọc đã
   được `bgdbg` chứng minh là tốt (MTEXT `ｹｶｷ線 t=9mm 本図　２枚` → `T=9.00 Q=2 M=SS400`),
   nên chỗ duy nhất còn có thể nuốt MTEXT là chính `ssget`. AutoCAD hỗ trợ danh sách nhiều
   giá trị ngăn cách bằng dấu phẩy trong group 0; nếu ZWCAD chỉ lấy **giá trị đầu tiên**
   (hoặc so sánh nguyên chuỗi) thì nó bắt `TEXT` và bỏ `MTEXT` — khớp chính xác triệu chứng.
   Đã đổi cả `c:bg` lẫn `c:FN` sang cấu trúc `<OR ... OR>`:

   ```lisp
   (ssget '((-4 . "<OR") (0 . "TEXT") (0 . "MTEXT") (-4 . "OR>")))
   ```

   > **ĐÃ XÁC MINH trên ZWCAD 2026.** Sau khi sửa, cùng một vùng chọn cho ra
   > `Chon 29 doi tuong: 26 TEXT, 3 MTEXT` — `ssget` bắt được MTEXT. Trước đó `bg` chạy hết
   > mà ra 0 tấm với đúng vùng chọn đó. Đây là nguyên nhân thật của lỗi "`bg` không dùng được
   > khi chọn MTEXT"; bộ khử mã định dạng ở mục 9 **không** phải nguyên nhân.
