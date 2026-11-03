# 15. Phương pháp debug đã dùng

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Phương pháp debug đã dùng

Toàn bộ các lỗi trên được xác định bằng cách chèn tạm các dòng `princ` in giá trị runtime
thật (nội dung text, mã byte từng ký tự qua `vl-string->list`, kết quả `wcmatch`/`entmake`/
`startapp`...) rồi nhờ chạy trực tiếp trong ZWCAD và đối chiếu log — không đoán mò. Xem chi
tiết các mẫu debug trong skill đã dẫn ở trên.
