# 01. Quy Tắc Nghiệp Vụ Riêng — Nhóm `Fname` (`01_Fname`)

> Áp dụng riêng cho mọi chức năng thuộc **Tab 1 — Fname**. Luôn tuân thủ song song với bộ luật nền chung tại [../../rules/](../../rules/).

---

## 1. Cấu Trúc Dữ Liệu Chuẩn F-NAME

1. **Thứ tự chuẩn các dòng khi xuất mới**:
   ```text
   N=<Tên tấm — tự do, GRO mặc định <đơn>-<bango>>
   M=<Mác vật liệu>
   T=<Chiều dày mm, luôn ≥ 1 số lẻ: 6 -> 6.0 (FnameCatalogData.FormatThickness)>
   Q=<Số lượng nguyên dương>
   Z=<Mã quản lý phôi>
   ```
2. **Bảo toàn các dòng gia công phụ khi Cập nhật (`Update`)**:
   - Một thẻ F-NAME có sẵn trên bản vẽ (do lệnh `FF` hoặc `GRO` sinh ra) có thể chứa thêm các dòng như `V=...`, `V1=...`, `シマ目内...`, `シマ目外...`, `裏から見た図`.
   - Khi thực hiện **Hút Fname → Sửa N/M/T/Q/Z → Cập nhật Fname**, hệ thống **BẮT BUỘC** giữ nguyên các dòng phụ (`ExtraLines`) nằm giữa `Q=` và `Z=` (hoặc cuối khối), tuyệt đối không làm mất dữ liệu chấn `V=` của tấm!
3. **Đọc đa định dạng khi Hút Fname (`Pick`)**:
   - Phải đọc được cả 3 dạng F-NAME thực tế trên bản vẽ công ty:
     1. Khối `MTEXT` nhiều dòng phân tách bởi `\P` hoặc `\n` (kể cả khi bị dính mã định dạng font `{\f...;...}`).
     2. Cụm nhiều đối tượng `TEXT` đơn lẻ xếp chồng theo chiều dọc trên cùng một cột X.
     3. Dòng đơn phân tách bằng dấu phẩy `N=...,M=...,T=...,Q=...,Z=...`.

---

## 2. Chuẩn Layer & Đối Tượng Đồ Họa Trên CAD

1. **Layer đích**: Luôn đặt trên layer **`2-F-NAME`**.
   - Nếu bản vẽ chưa có layer `2-F-NAME`, tự động khởi tạo với màu chuẩn ACI `3` (Green) và kiểu đường `Continuous`.
   - Không làm thay đổi layer hiện hành (`CLAYER`) của người dùng sau khi kết thúc lệnh.
2. **Kiểu chữ & Kéo thả chiều cao trực tiếp (`DrawJig`)**:
   - Ưu tiên TextStyle `"Text"` (`txt.shx` nếu tạo mới), fallback an toàn về TextStyle hiện hành của bản vẽ nếu không tìm thấy font.
   - **Không bắt nhập tay chiều cao chữ trên bảng UI**: Khi đặt Fname mới (`OHTA_FN_PLACE`), người dùng bấm **Click 1** chọn điểm neo góc trên-trái, sau đó di chuột kéo thả trực tiếp (`FnameScaleJig : DrawJig`) để xem trước khối 5 dòng `N=, M=, T=, Q=, Z=` phóng to/thu nhỏ theo con trỏ rồi bấm **Click 2** để chốt (hoặc nhấn `Enter` để dùng ngay cỡ chữ vừa đặt lần trước đã lưu trong Registry).
   - Khoảng cách bước dòng khi xuất dạng nhiều `TEXT` đơn: `lineStep = TextHeight * 5.0 / 3.0` (baseline dòng đầu tại `baseY - TextHeight`) để đỉnh khối `TEXT` bám sát điểm Click 1 và khớp đồng bộ chiều cao với khối `MTEXT`.
3. **Quy tắc tương thích `MTEXT` ZWCAD & AutoCAD**:
   - Chỉ gán `Attachment = AttachmentPoint.TopLeft` (DXF group `71 = 1`) và `Width = 0.0` (không tự ngắt dòng giữa chừng). Không can thiệp hỗn hợp DXF `72`/`73` của `TEXT` đơn.
   - Luôn đổi điểm chọn từ hệ tọa độ `UCS` hiện hành sang `WCS` (`TransformBy(ed.CurrentUserCoordinateSystem)`) trước khi ghi vào Database.

---

## 3. Quy Tắc Bảng Tra `M=` → `T=` & `Z=` Và Hậu Tố `N=`

1. **Ràng buộc thông minh `M=` → `T=`, `Z=`**:
   - Khi người dùng chọn một vật liệu `M=` trong danh mục chuẩn (`data/catalog.csv`, đồng bộ từ Google Sheet — xem README mục 3), tự động nạp danh sách độ dày `T=` của vật liệu đó (reset lựa chọn `T=` nếu giá trị cũ không nằm trong danh sách mới) và tự động điền `Z=` tương ứng.
   - **Cột `Loai` (loại vật liệu)** trong `catalog`: quy mác về nhóm vật liệu để tra bảng TraV / T thực (ví dụ `SUS304 No.1` → `SUS`, `SS400` → `SS`, `SPHC` → `SS`). Giá trị phải trùng mã cột `Mat` trong `trav_90` (`SS`, `SUS`, `AL`, `BON`, `HT590A`...). Mác 縞板 / SIMA tự ghép thành `<Loai> SIMA Trong` / `<Loai> SIMA Ngoài`. Mác không có trong catalog hoặc `Loai` để trống thì đoán theo tên mác như cũ.
   - **Không ghi thẳng dữ liệu lên Sheet**: mọi đề xuất từ tool (kể cả máy admin) chỉ vào tab `update` (`submit_request`); chỉ máy có tên trong tab `admin` mới `approve_request` / `reject_request`, cả hai đều xoá dòng khỏi `update`. Gửi thành công thì máy gửi lưu bổ sung vào `catalog_local.csv` (cache) để dùng ngay; `FnameCatalogData.LoadFromModuleDirectory` luôn cộng file này lên catalog, `FnameDataSync.Run` gọi `FnameLocalAdditions.Prune` với danh sách yêu cầu còn chờ của máy (`my_requests`): chỉ giữ dòng còn chờ duyệt, bỏ dòng đã duyệt / bị từ chối; không đọc được hàng chờ thì giữ nguyên. Định dạng `Thong_so`: `key=value` cách nhau ` | ` (ký tự `|` và xuống dòng trong giá trị bị thay bằng dấu cách). Thêm loại dữ liệu mới: 1 mục trong `HANDLERS` (validate + apply) của Apps Script + hằng loại / hàm `Build...Params` / nhánh `Describe` trong `FnameUpdateRequest`.
   - **Áp yêu cầu catalog khi duyệt** (`applyCatalog_` trong Apps Script, `FnameCatalogData.Upsert` cho cache máy): khoá so khớp là `M` (không phân biệt hoa thường). Mác có sẵn → gộp độ dày theo giá trị số (bỏ trùng, xếp tăng dần, giữ cách viết cũ), luôn ghi `Loai`, chỉ ghi `Z` khi có nhập. Mác mới → thêm dòng. Thiếu cột `Loai`/`Z` thì tự thêm cột. Ô được ghi dạng chữ (`6.0` không thành `6`). Quy tắc gộp phía C# (`FnameCatalogData.Upsert`) và phía Apps Script phải luôn giống nhau.
   - Các ô `M=`, `T=`, `Z=` đều bật chế độ cho phép sửa tay (`IsEditable = true`) để xử lý linh hoạt các đơn hàng vật liệu đặc chủng.
2. **`N=` mẫu theo tên bản vẽ (`DefaultPartNameFromDrawing`)**:
   - Lần đầu mở tool trong một bản vẽ, `N=` = `<tên bản vẽ>-1`; nếu tên kết thúc bằng `-s` / `-a` (không phân biệt hoa thường) thì bỏ đuôi đó (`47079-S.dwg` → `47079-1`).
   - `N=` được nhớ riêng theo từng bản vẽ trong phiên CAD (đổi qua lại bản vẽ không mất giá trị đang sửa); không còn lưu `LastN` vào Registry.
3. **Quy tắc tăng đuôi `N=` (`IncrementPartName`)**:
   - Nếu `N=` kết thúc bằng `-<số>` (ví dụ `PART-1`, `47079-05`), tăng phần số thêm 1 (`PART-2`, `47079-06`, giữ độ dài số 0 dẫn đầu nếu có).
   - Nếu `N=` chưa có hậu tố `-<số>` (ví dụ `COVER`), khi bấm nút `+1` sẽ gắn `-1` (`COVER-1`).
4. **Quy ước tiền tố hàm LISP phụ trợ của nhóm**:
   - Mọi hàm nội bộ trong `01_Fname/lisp/*.lsp` bắt đầu bằng `ohta-fn--` để cô lập hoàn toàn với các Tab khác.

---

## 4. Quy Tắc Gia Công Bẻ / Uốn (V=) & Nhấn 1 Lần (内R-発押し)

1. **Thứ tự dòng chuẩn tuyệt đối khi sinh và lưu F-NAME**:
   - Dòng 1: `N=...`
   - Dòng 2: `M=...`
   - Dòng 3: `T=...`
   - Dòng 4: `Q=...`
   - Dòng 5+: Toàn bộ các dòng **Bẻ / Uốn (`V=`)** (nằm giữa `Q=` và `Z=`).
   - Dòng trung gian: Các dòng ghi chú phụ trước Z (`ExtraBeforeZ`) nếu có.
   - Dòng kế tiếp: `Z=...`
   - Dòng sau Z: Các dòng ghi chú phụ sau Z (`ExtraAfterZ`) nếu có.
   - **Dòng cuối cùng**: Toàn bộ các dòng **Nhấn 1 lần (`内R...-発押し...°`) BẮT BUỘC NẰM Ở DƯỚI CÙNG** của khối F-name (sau cả `Z=` và các ghi chú khác).
2. **Cơ chế tính toán và tra cứu TraV (`TraVCalculator`)**:
   - Tra cứu đầy đủ bảng 155 mục tiêu chuẩn TraV OFT theo vật liệu `M=` và chiều dày `T=`.
   - Với góc bẻ vuông `90°`: lấy trực tiếp từ bảng 90.
   - Với góc tù `> 90°`: tính theo công thức độ dãn denta chuẩn:
     $$\Delta = 2T \cdot \tan\left(\frac{180^\circ - \text{angle}}{4}\right) - \frac{(180^\circ - \text{angle}) \cdot \pi}{180^\circ} \cdot \left(R + \frac{T}{3}\right)$$
     (cắt cụt 2 chữ số thập phân).
   - Với góc nhọn `< 90°`: tra cứu nội suy từ bảng góc nhọn riêng `traVnho90Table`. Góc nằm trên góc lớn nhất của bảng (vd. 89° khi bảng bắt đầu 88°) nội suy với Denta 90° của `trav_90` cùng dao (`HikiShiro_90`, trống thì `Denta90` — `Denta90Of`), không ngoại suy 88/86.
   - Vật liệu tôn gân / nhám (縞板 - Sima): Tự động phân tách hai hướng uốn `シマ目外` (uốn chiều vân ngoài) và `シマ目内` (uốn chiều vân trong).
3. **Dòng thông tin phụ dưới mỗi dòng bẻ (`TraVCandidate.DetailParts`)**:
   - Không lặp lại tên dao V (đã hiện ở ô chọn V).
   - Góc `>= 90°`: `最長 - Ｌ最低 - Ｚ最低 - 実内R - 内R - 外R - Rend` (lấy từ bảng 90 theo vật liệu / độ dày / dao).
   - Góc nhọn `< 90°`: cùng các thông số của bảng 90 với đúng dao đó nhưng **bỏ 実内R / 内R / 外R (giữ Rend)**, rồi `R ngoài(<góc>°)` từ `trav_nho90r`. **Không hiện góc nhỏ nhất**; chỉ khi góc nhập vào nhỏ hơn góc nhỏ nhất còn số liệu trong cột dao/độ dày của `trav_nho90` thì hiện thêm dòng **cảnh báo chữ đỏ** `⚠ Góc <x>° nhỏ hơn góc nhỏ nhất trong bảng là <min>°` (`TraVCandidate.Warning`) ngay dưới thông tin phụ — các thông tin khác vẫn hiện bình thường, dòng V vẫn được tạo (độ dãn ngoại suy từ 2 góc nhỏ nhất của cột). Dao chỉ có ở bảng góc nhọn (vd. `U18`) thì không có phần thông số bảng 90.
   - Giao diện hiển thị bằng `WrapPanel`: mỗi cụm "tên + giá trị" là 1 khối không ngắt, bảng hẹp thì xuống dòng nguyên cụm.
4. **Góc bẻ thử / nội suy (`trav_thu`, `Core/TraVThuCore.cs`, `TraVCalculator.LookupTrialCandidates`)**:
   - Chỉ dùng khi `LookupCandidates` (bảng TraV) trả rỗng cho loại vật liệu đó — bảng TraV luôn được ưu tiên.
   - Khớp Loai bằng `TraVThuData.NormalizeLoai` (bỏ dấu, hoa thường), T sai số 0.05, dao chuẩn hoá `12` → `V12`. Nhóm theo dao.
   - Góc không làm tròn (khớp đúng góc với sai số 1e-4).
   - Đúng góc → dùng dòng đó (nhãn theo `Dinh_danh`). Không có → nội suy tuyến tính từ 2 dòng **Bẻ thử** gần nhất kẹp 2 bên. Chỉ có 1 phía → ngoại suy từ 2 dòng Bẻ thử gần nhất phía đó nếu góc gần nhất cách ≤ `MaxExtrapolationDistance` (2°), `Dinh_danh = Ngoại suy`. Dòng `Nội suy` / `Ngoại suy` không làm gốc. Denta cắt cụt 2 chữ số (`FormatDenta`).
   - Cần xác nhận quản lý (`TraVCandidate.Warning` đỏ + `NeedsManagerConfirm`, Đặt / Cập nhật Fname hỏi Yes/No): nội suy có khoảng 2 góc > `MaxInterpolationGap` (10°), hoặc mọi ngoại suy — **trừ khi** 1 trong 2 góc gốc cách góc cần tra < `NoConfirmDistance` (1°).
   - Góc 90° của `trav_90` (cùng vật liệu / T / dao) được thêm làm 1 gốc ngang Bẻ thử (`Table90Base`, `Ma_tam = bảng TraV`) nếu dao đó chưa có dòng 90° trong `trav_thu`.
   - Gốc tính: thử chỉ dòng Bẻ thử trước (`TryDerive`); không ra mới thử cả dòng Nội suy / Ngoại suy — gốc không phải bẻ thử thì **luôn** `NeedsManagerConfirm` (không áp miễn 1°).
   - `SourceInfo` ghi 2 góc gốc + mã tấm (`DescribeBase`); `Ma_tam` của dòng tính ra = `BaseToken(a) / BaseToken(b)` (`A1 (70°)` hoặc `nội suy 80°`).
   - `FnameTrialListWindow`: danh sách chỉ đọc của `TraVCalculator.GetTrialRecords()`, dòng trùng `trav_thu_local.csv` là "Chờ duyệt".
   - `TraVCandidate.NewTrialRecord` (góc nội suy chưa có trong bảng) được gửi `submit_request` loại `trav_thu` khi Đặt / Cập nhật Fname, rồi lưu `trav_thu_local.csv` (dọn khi đồng bộ giống `catalog_local.csv`). Khoá trùng = Loai + T + Góc + Dao, quy tắc ghi đè phía C# (`TraVThuData.Upsert`) và Apps Script (`applyTravThu_`) phải giống nhau.
   - Dòng `V=` giữ nguyên định dạng tính toán; thông tin "[Bẻ thử] / [Nội suy] ... tham khảo tấm xxxx" chỉ hiện trên bảng (`SourceText`).
5. **Đo góc trực tiếp từ bản vẽ (`OHTA_FN_PICK_ANGLE`)**:
   - Khi người dùng ấn biểu tượng thước đo `📐`, CAD bật lệnh chọn Dimension góc trên bản vẽ (`Dimension.Measurement`), tự động quy đổi radian sang độ và điền vào ô góc trên bảng.


---

## 5. Bố Cục & Liên Kết Các Bảng Dữ Liệu Sheet Fname

1. **Khoá liên kết là giá trị tự nhiên**, không dùng mã ID: `catalog.Loai` → `trav_90.Mat`; `trav_90` = Mat + T + V; `tam` = Loai + T; `trav_thu` = Loai + T + Goc + Dao.
2. **Thông số theo tấm nằm ở tab `tam`, thông số theo dao nằm ở `trav_90`.** `tam.T_hienthi` = T thực người dùng xem (nhãn "T thực:"); `trav_90.T_thuc` = số dùng trong công thức denta góc tù — **2 đại lượng khác nhau**, không gộp, không sửa `T_thuc` khi đụng tới `tam`.
3. **SIMA trong `tam` ghi 1 dòng chung** (`SS SIMA`, không kèm chiều) — `TamData.BaseLoai` bỏ đuôi `Trong` / `Ngoài` khi so khớp, cùng quy tắc `tamBaseLoai_` trong Apps Script. Các bảng khác vẫn ghi tên loại kèm chiều (`SS SIMA Ngoài`).
4. **Đọc theo tên cột** cho `catalog`, `trav_90` (`TraVCalculator.ParseTable90`), `tam`, `trav_thu`, `cau_hinh`. Riêng `trav_nho90` / `trav_nho90r` giữ dạng lưới (đọc theo vị trí). Không có dòng tiêu đề: `trav_90` hiểu theo thứ tự cột cũ, `tam` bỏ qua.
5. **`T thực` trên bảng**: `TraVCalculator.GetRealThickness` lấy `tam` trước; cột `T_hienthi` cũ của `trav_90` (nếu Sheet chưa chuyển) rồi `T_thuc` chỉ là dự phòng.
6. **`cau_hinh.phien_ban_toi_thieu`**: bản đang chạy (`OhtaUpdateCommands.LoadedVersion`) thấp hơn → `FnameSheetWriter.Post` chặn mọi action ghi (`submit_request`, `approve_request`, `reject_request`), đọc vẫn cho; bảng Fname hiện dải đỏ.
7. **Kiểm tra liên kết** (`FnameDataCheck`) sau mỗi lần tải: Loai của catalog không có trong `trav_90`, dòng `tam` trùng khoá hoặc Loai lạ → cảnh báo trong Cài đặt → Dữ liệu, không chặn.
8. Đổi cấu trúc tab trên Sheet: cập nhật tab `README` đầu Sheet + bảng mục 3 của README nhóm.
9. **Bổ sung T thực có bằng chứng** (phần T thực của `FnameMaterialWindow` → `SubmitTamAsync` → `FnameSheetWriter.SubmitTamRequest`, loại yêu cầu `tam`, mỗi T 1 yêu cầu): chỉ lấp chỗ trống (Loai + T chưa có `T_hienthi`, kiểm tra cả ở form, `prepareTam_` khi gửi và `applyTam_` khi duyệt). Ảnh gửi dạng `anh_1..anh_3` (base64 JPEG, đã thu nhỏ), Apps Script lưu Drive rồi thêm khoá `Anh` (link cách nhau dấu cách) vào Thong_so. Máy gửi lưu `tam_local.csv` (`TamLocalAdditions`, đánh dấu `ChoDuyet`), dọn khi đồng bộ giống `trav_thu_local.csv`. Từ chối → `trashAnh_`. Văn bản người gõ đi qua `cleanText_` / `TamData.Clean` (bỏ dấu phẩy, ngoặc kép, xuống dòng); tab `tam` vẫn được đọc bằng `TamData.SplitCsvLine` phòng khi sửa tay có dấu phẩy.
10. Gửi form có giá trị dài (ảnh base64) không dùng `FormUrlEncodedContent` (bản net48 lỗi "Uri string is too long") — `FnameSheetWriter.Post` tự mã hoá từng đoạn (`EscapeLong`).
