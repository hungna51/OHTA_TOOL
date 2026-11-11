# Nhật ký phiên bản OHTA TOOL

Mỗi bản phát hành có một mục `## vX.Y.Z` ở đầu file. `release_ohta.ps1` lấy nội dung mục đó làm ghi chú Release trên GitHub, và hộp thoại "Có bản mới" trên máy đội cũng hiện đúng nội dung này — viết ngắn, cho người dùng xưởng đọc.

## v1.4.2

- Tab **Guro v1.6.3** — **GRO chạy nhanh hơn nhiều**: bản vẽ lớn từ ~67 giây còn ~27 giây (ghép hình chiếu, cung bo góc, đọc text số, khớp dim, chuyển layer nay chạy bằng C#).
- Tab Guro có **thanh tiến độ** khi đang chạy GRO: hiện bước đang làm và % hoàn thành.
- Chân dim góc dài hơn 45 mm cũng được chuyển sang `1-SUNPOU`.
- Tấm gộp có đầu nhô cao không còn bị báo nhầm "khung vẽ nhầm".
- Dấu độ kiểu `45ﾟ` không còn bị báo là "ghi chú tiếng Nhật mới". 裏 đứng một mình không còn bị hiểu là 逆図.
- Cảnh báo khi máy còn nạp DLL GURO cũ (GROZWCAD / ZwCadPlugin) gây trùng lệnh.
- Tab **Fname**: góc bẻ thử nhập kèm thông số dao (最長 / L / Z / Rend), tự gợi ý theo dữ liệu có sẵn.
- Sau khi cập nhật, **đóng ZWCAD rồi mở lại**.

## v1.4.1

- Tab **AI**: thêm **💬 Chat với AI (Ling)** — hỏi đáp trực tiếp trong tab, chữ trả lời hiện dần, bật/tắt "Hiện suy nghĩ", hiện tên model đang dùng.
- **📎 Gửi khung**: quét chọn khung in để AI đọc kèm (cả toạ độ từng đối tượng, AI tự đo được kích thước).
- Lịch sử chat lưu trong thư mục **`OHTA_AI`** cạnh bản vẽ (mỗi cuộc một file, mở lại được); nút **📝 Xuất mô tả** cũng ghi vào `OHTA_AI\mo-ta`.
- Nút ⚙ có mục **Ling API**: nhập key, đổi tên model, kiểm tra kết nối.
- Phần **▶ Kiểm tra** chạy được bằng Ling: ô **Model** chọn `Ling (model trong ⚙)`.

## v1.4.0

- Tab **AI v0.2 (beta)** *(mở thử nghiệm cho các máy Admin)*: kết hợp mô hình **Kev-0.8B** chạy mạng LAN (`http://NGUYEN-A-HUNG:8008`) và **Claude API** để kiểm tra bản vẽ.
- **Tự phân loại tấm gia công sau cắt laser**: dán nhãn `[BE]`, `[VAT]`, `[CUON]`, `[SARA]`, `[TAPU]`, `[PHANG]` (hoặc ghép nhiều công đoạn) ở góc trên-trái khung trên layer `AI-PHAN-LOAI`.
- **Kiểm tra F-name, ghi chú & chuẩn xuất CSV**: đối chiếu `M= / T= / Z=` theo bảng vật liệu chuẩn, tấm nhám `SIMA` / `CPL`, số lượng `Q=`, trùng tên `N=`, trộn `TEXT` + `MTEXT` trên `2-F-NAME`, đối tượng lạ trên `0-KAKOU`, chú thích đường bẻ và khối MText Bước 1 xuất CSV.
- **Tự đồng bộ luật AI qua mạng LAN**: máy Admin không cần cài Kev hay Python — chỉ cần cập nhật từ GitHub là chạy ngay chế độ Kev (LAN) và tự tải bộ luật `.md` mới nhất từ máy chủ mỗi lần kiểm tra.
- Bổ sung quy tắc hướng dẫn xử lý lỗi ZWCAD treo giữa chừng khi chạy GRO do còn lisp GURO cũ trong Startup Suite.

## v1.3.3

- Tab **Guro v1.6.2** — sửa lỗi ZWCAD trên một số máy: bấm **Chạy GRO** báo "buoc nhannet (C#) loi: undefined function" và dừng, NETRELOAD cũng không hết. Sau khi cập nhật, **đóng ZWCAD rồi mở lại**.
- Nhật ký báo lỗi ghi thêm các DLL đã nạp vào CAD để sửa lỗi nhanh hơn.

## v1.3.2

- **Báo lỗi gửi ở nền**: bấm Gửi là form đóng ngay, vẫn làm việc tiếp được. Tiến trình gửi (% tải lên, Huỷ gửi) hiện ở khung **BÁO LỖI** trên tab Info; gửi xong có thông báo trên dòng lệnh.
- Nhật ký gửi kèm báo lỗi ghi đầy đủ hơn (lệnh đã chạy, từng bước và thông báo của GRO, cài đặt GRO) để sửa lỗi nhanh hơn. Lỗi GRO thì tự tick sẵn gửi kèm bản vẽ (bỏ tick được).
- Tab **Guro v1.6.1**: khi một bước C# của GRO lỗi (hộp "buoc ... (C#) loi") cũng hỏi gửi báo lỗi cho admin.

## v1.3.1

- Vá bảo mật cho bản ZWCAD: cập nhật thư viện mã hoá đi kèm tab **PRINT** (gộp PDF) lên bản đã sửa 2 lỗi có thể làm treo chương trình. Không đổi cách dùng.

## v1.3.0

- **Báo lỗi cho admin**: nút **🐞 Báo lỗi** trên tab Info (hoặc gõ lệnh `OHTABUG`). Viết mô tả, chọn mức độ, tool tự gửi kèm ảnh cửa sổ CAD, phiên bản và nhật ký; tuỳ chọn gửi kèm bản vẽ / CSV.
- Khi một lệnh (hoặc **Chạy GRO**) gặp lỗi, tool hỏi "Gửi báo lỗi cho admin?" — bấm Có là form điền sẵn lỗi. Bấm Esc huỷ lệnh thì không hỏi.
- Mất mạng lúc gửi: báo lỗi được lưu lại và tự gửi ở lần mở bảng OHTA sau.
- Tab **Guro v1.6**: GRO ghi nhật ký và nối vào báo lỗi.
- Tab **Guro v1.6** — sửa lỗi tách tấm: thanh vẽ liền nhiều tấm (vd `570 | 240`, `390 | 840 | 725`) mà ghi chú dài hơn đoạn ngắn thì không cắt, một tấm ôm cả hai hình, tấm kia không có hình. Nay cắt đúng. Sau khi cập nhật, đóng CAD rồi mở lại.

## v1.2.3

- Tab **Guro v1.5.3** — sửa lỗi ZWCAD (từ v1.2.2): bấm **Chạy GRO** báo "buoc nhannet (C#) loi: undefined function" và dừng, nhất là với bản vẽ có chữ Nhật. Sau khi cập nhật, đóng ZWCAD rồi mở lại.

## v1.2.2

- Tab **Guro v1.5.2** — sửa lỗi:
  - ZWCAD bấm **Chạy GRO** báo lỗi "consp nil" và dừng.
  - Chữ Nhật trong ghi chú hiện thành dấu "?".
  - Tab **Nét Chưa Đổi** (bảng DST) liệt kê nhầm bango, tên bản vẽ, khung bao.
  - Tab **Thông Tin** chỉ ghi "xem chi tiết ở dòng lệnh" — nay hiện rõ từng cảnh báo (dòng số lượng không ghép được kèm vị trí, ghi chú không đọc được số lượng…).
  - Bấm nút trên tab Guro không còn in 2 dòng nạp lisp lên dòng lệnh.

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
