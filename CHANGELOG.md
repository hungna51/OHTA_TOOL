# Nhật ký phiên bản OHTA TOOL

Mỗi bản phát hành có một mục `## vX.Y.Z` ở đầu file. `release_ohta.ps1` lấy nội dung mục đó làm ghi chú Release trên GitHub, và hộp thoại "Có bản mới" trên máy đội cũng hiện đúng nội dung này — viết ngắn, cho người dùng xưởng đọc.

## v1.2.1

- Tab **Guro v1.5.1**: chạy GRO trên bản vẽ mới chưa lưu giờ báo "Bản vẽ chưa lưu" và dừng, không ghi CSV / PDF vào thư mục tạm nữa. Lưu bản vẽ (Ctrl+S) rồi chạy lại.

## v1.2.0

- Tab **Print** mới (lệnh `OI` v2.0, thay lisp OI cũ): in khung `Khung_IN` / vùng quét / extents ra PDF, gộp 1 file, không cần Ghostscript.
- Cấu hình in tự đặt tên (vd "Đen trắng" dùng ctb khác) ở nút ⚙; mục "Theo bản vẽ" lấy máy in / khổ / ctb của bản vẽ nếu có.
- File PDF đã có: hỏi Ghi đè / Lưu thành (1) / Huỷ in.
- Tab **Fname v1.2**: bổ sung **T thực** của tấm ngay trong cửa sổ "Bổ sung vật liệu" (kèm 1–3 ảnh bằng chứng, người xác nhận, ghi chú); máy gửi dùng được ngay, T thực bổ sung có dấu `*` và chú thích ở cuối bảng.
- Fname: dữ liệu T thực hiển thị tách sang bảng `tam` riêng (vd SUS SIMA T=6 → T thực 6.2); Sheet có thể đặt phiên bản tối thiểu — bản cũ hơn sẽ không ghi được lên Sheet.
- Fname: mục Cài đặt > Bổ sung dữ liệu chia rõ 3 phần (vật liệu / góc bẻ thử / T thực), có cảnh báo khi liên kết dữ liệu lỗi.
- Tab Guro: sửa lỗi máy khác bấm **Chạy GRO** báo không tìm thấy `guro_gro.lsp`.

## v1.1.1

- Tab **Info** mới (cuối bảng OHTA): nhà phát triển, phiên bản Tool, phiên bản từng tab (Fname, Guro, More), số lisp / lệnh của mỗi tab.
- Nút **Kiểm tra cập nhật** và **Quay về bản trước** ngay trong tab Info.
- Đầu mỗi tab hiện kèm phiên bản, vd `GURO · v1.5`.

## v1.1.0

- Tự cập nhật từ GitHub: mỗi lần mở bản vẽ, nếu có bản mới sẽ hỏi "Cập nhật ngay / Để sau".
- Lệnh `OHTAUPDATE` kiểm tra bản mới ngay; `OHTAROLLBACK` quay về bản trước lần cập nhật gần nhất.
- Lisp / nút bạn tự thêm bằng nút ＋ được giữ nguyên khi cập nhật.
- Tab Guro: lệnh GRO mở tab, `GROCHAY` đánh số; cài đặt GURO 2 tab Chung + Ghi chú.
