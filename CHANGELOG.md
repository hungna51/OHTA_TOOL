# Nhật ký phiên bản OHTA TOOL

Mỗi bản phát hành có một mục `## vX.Y.Z` ở đầu file. `release_ohta.ps1` lấy nội dung mục đó làm ghi chú Release trên GitHub, và hộp thoại "Có bản mới" trên máy đội cũng hiện đúng nội dung này — viết ngắn, cho người dùng xưởng đọc.

## v1.2.0

- Tab **PRINT** mới (lệnh `OI` v2.0, thay lisp OI cũ): in khung `Khung_IN` / vùng quét / extents ra PDF, gộp 1 file, không cần Ghostscript.
- Cấu hình in tự đặt tên (vd "Đen trắng" dùng ctb khác) ở nút ⚙; mục "Theo bản vẽ" lấy máy in / khổ / ctb của bản vẽ nếu có.
- File PDF đã có: hỏi Ghi đè / Lưu thành (1) / Huỷ in.

## v1.1.1

- Tab **Info** mới (cuối bảng OHTA): nhà phát triển, phiên bản Tool, phiên bản từng tab (Fname, Guro, More), số lisp / lệnh của mỗi tab.
- Nút **Kiểm tra cập nhật** và **Quay về bản trước** ngay trong tab Info.
- Đầu mỗi tab hiện kèm phiên bản, vd `GURO · v1.5`.

## v1.1.0

- Tự cập nhật từ GitHub: mỗi lần mở bản vẽ, nếu có bản mới sẽ hỏi "Cập nhật ngay / Để sau".
- Lệnh `OHTAUPDATE` kiểm tra bản mới ngay; `OHTAROLLBACK` quay về bản trước lần cập nhật gần nhất.
- Lisp / nút bạn tự thêm bằng nút ＋ được giữ nguyên khi cập nhật.
- Tab Guro: lệnh GRO mở tab, `GROCHAY` đánh số; cài đặt GURO 2 tab Chung + Ghi chú.
