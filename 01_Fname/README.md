# 01. Nhóm Chức Năng Fname (`01_Fname` — Tab `Fname`)

> Tài liệu đặc tả riêng của **Mục con 01 — Fname** (hiển thị thành Tab dọc **`Fname`** đầu tiên trên bảng Tool Palettes `OHTA`).
> Luật nền dự án: [../GEMINI.md](../GEMINI.md) | Luật nghiệp vụ riêng nhóm Fname: [rules/01-quy-tac-fname.md](rules/01-quy-tac-fname.md)

---

## 1. Mục Đích & Chức Năng Nhóm `Fname`

Tab **`Fname`** quản lý toàn bộ thao tác tạo mới, đọc, chỉnh sửa và kiểm tra thẻ thông tin chi tiết tấm (**F-NAME**) trên layer chuẩn **`2-F-NAME`**.

Khi bấm vào Tab **`Fname`** trên bảng `OHTA`, giao diện bố trí trực quan:
- **Hàng trên cùng (2 cột song song cạnh nhau)**:
  - **Cột trái - Khối Thông Tin F-NAME** (5 hàng độc lập, căn thẳng lề trái):
    - **Hàng 1**: **`N=`** *(Part Name)* (kèm nút `+1` tăng nhanh số thứ tự).
    - **Hàng 2**: **`M=`** *(Material)*: Mác vật liệu tiêu chuẩn hoặc nhập tự do.
    - **Hàng 3**: **`T=`** *(Thickness)*: Chiều dày danh nghĩa, kèm nhãn hiển thị ngay bên cạnh **`T thực:`** (lấy cột `T_hienthi` của tab `tam` theo Loại + T).
    - **Hàng 4**: **`Q=`** *(Quantity)*: Số lượng sản xuất của chi tiết (kèm 2 nút `−` và `+`).
    - **Hàng 5**: **`Z=`** *(Base Material)*: Mã phôi quản lý (tự động cố định theo `M=`, có thể mở khóa trong Cài đặt).
- **Quy tắc đặc thù tấm SIMA (縞板 - Tôn gân / tôn nhám)**: Với tất cả các mác vật liệu SIMA (`SS400 縞板`, `ムキ 縞板`, `SUS304 縞板`, hoặc `Z=*-SIMA`), ngay dưới dòng `Z=` luôn luôn tự động có dòng ghi chú **`裏から見た図`** (Nhìn từ mặt sau/mặt đáy).
- **Khung Xem trước (`2-F-NAME`)**: Nằm ngay bên cạnh hàng thông tin tấm, nền tối `#1E293B`, chữ xanh sáng `#38BDF8`, cập nhật thời gian thực theo từng phím gõ.

Kèm 2 Card gia công mở rộng chuyên biệt (có badge đếm số dòng, hiển thị 1 hàng gọn cho mỗi mục):
- **Card `● BẺ / UỐN (V=)`**: Bổ sung một hoặc nhiều đường bẻ `V=...-引代(góc°)`. Tự động tra cứu dao cối V và độ dãn denta từ cơ sở dữ liệu TraV chuẩn OFT theo vật liệu `M=` và độ dày `T=`. Hỗ trợ nút `📐` đo góc trực tiếp từ Dimension CAD. Toàn bộ các dòng `V=` được xếp trước `Z=`.
- **Card `● NHẤN 1 LẦN (内R-発押し)`**: Bổ sung gia công nhấn 1 lần định dạng `内R{r}-発押し{góc}°`. Hỗ trợ nút `📐` đo góc CAD. **Dòng nhấn 1 lần luôn được xếp tại vị trí cuối cùng dưới cùng của Fname** (sau cả `Z=` và các dòng ghi chú khác).

Các khối xem trước & mở rộng:
- **Khung Xem trước (`2-F-NAME`)**: Nền tối `#1E293B`, chữ xanh `#38BDF8`, có thể thu/mở gọn (`Expander`).
- **Lệnh Fname bổ sung**: Thiết kế `Expander` 2 cột thu gọn, không chiếm không gian khi chưa dùng tới.
- **Thanh nút hành động cố định**: 3 nút **Đặt Fname mới**, **Hút Fname** và **Cập nhật Fname** được ghim cố định ở chân bảng, luôn hiển thị trực quan không bị đẩy xuống khi thêm nhiều dòng gia công.

---

## 2. Các Thao Tác & Lệnh Thực Thi Trong Nhóm `Fname`

### A. Nhóm thao tác trực tiếp trên Form Fname (C# .NET Đa nền tảng)

| Nút / Lệnh CAD | Chức năng chi tiết |
|---|---|
| **`FN`** (lệnh tắt) | Mở bảng OHTA TOOL và chuyển thẳng sang tab **Fname**. Thay lệnh `FN` cũ của lisp GURO (lệnh .NET được CAD ưu tiên khi OHTATOOL.dll đã nạp). |
| **Logo Cài Đặt (Góc phải Header)** | Mở hộp thoại **Cài đặt Fname** (`FnameSettingsWindow`), chia 3 tab: **✎ Nhập liệu** (khóa Z=, tự tăng N=) · **🗄 Dữ liệu** (cập nhật từ Google Sheet, mở thư mục dữ liệu, **➕ Thêm / cập nhật vật liệu**) · **ℹ Thông tin** (lần đồng bộ cuối, đường dẫn cache & thư mục dữ liệu). Cài đặt mới thêm vào đúng tab theo nhóm. |
| **📍 Đặt Fname mới** (`OHTA_FN_PLACE`) | **Click 1**: Chọn điểm góc trên-trái trên CAD → **Di chuột**: Kéo thả trực tiếp (`DrawJig`) để xem trước và định cỡ chiều cao cả khối `N=, M=, T=, Q=, Z=` theo kích thước tấm → **Click 2** (hoặc `Enter`): Chốt chiều cao chữ và ghi lên layer `2-F-NAME` (`TEXT` rời hoặc `MTEXT`). |
| **💧 Hút Fname** (`OHTA_FN_PICK`) | Bấm chọn 1 dòng `TEXT` hoặc khối `MTEXT` bất kỳ của Fname có sẵn trên bản vẽ → Tự động gom toàn bộ các dòng thuộc cùng cụm Fname đó và đổ ngược dữ liệu lên 5 ô `N=`, `M=`, `T=`, `Q=`, `Z=` trên bảng. |
| **💾 Cập nhật Fname** (`OHTA_FN_UPDATE`) | Ghi đè 5 giá trị `N=`, `M=`, `T=`, `Q=`, `Z=` đang có trên bảng xuống cụm Fname vừa hút (hoặc chọn cụm Fname trên bản vẽ để cập nhật ngay tại chỗ, bảo toàn các dòng phụ như `V=` nếu có). |

### B. Nhóm lệnh Fname mở rộng bên dưới (`module.json` + `lisp/`)

| Lệnh CAD | Tên công cụ | File thực thi | Mô tả |
|---|---|---|---|
| `OHTAFNSEL` | Chọn tất cả Fname (`2-F-NAME`) | [`lisp/ohta_fname_tools.lsp`](lisp/ohta_fname_tools.lsp) | Quét và bật Grips chọn toàn bộ đối tượng `TEXT`/`MTEXT` thuộc layer `2-F-NAME` để kiểm tra nhanh |
| `OHTAFNCHK` | Kiểm tra nhanh Fname thiếu trường | [`lisp/ohta_fname_tools.lsp`](lisp/ohta_fname_tools.lsp) | Đếm và kiểm tra các Fname trên layer `2-F-NAME` bị thiếu một trong các trường `N=`, `M=`, `T=`, `Q=`, `Z=` |

*(Để bổ sung thêm lệnh mới liên quan đến Fname sau này, chỉ cần đặt file `.lsp` vào `01_Fname/lisp/` và thêm vào mảng `"tools"` trong [`module.json`](module.json)).*

---

## 3. Dữ Liệu Tra Cứu Trên Google Sheet (`data/`)

Các bảng dữ liệu được quản lý trên **1 Google Sheet** (mỗi bảng 1 tab; tab `README` đầu Sheet mô tả từng tab), mọi máy tự tải về. Tool đọc theo **tên cột** (trừ 2 bảng lưới góc nhọn), nên thứ tự cột trên Sheet tùy ý:

| Tab / file CSV | Nội dung |
|---|---|
| `catalog` | `M,Loai,Z,T_list` — mác vật liệu, **loại vật liệu** (`SS`, `SUS`, `AL`, `BON`… = mã cột `Mat` của bảng `trav_90`), mã phôi Z=, danh sách độ dày (cách nhau dấu cách). Cột đọc theo tên tiêu đề nên thứ tự cột tùy ý |
| `trav_90` | Bảng TraV 90° theo dao (khoá Mat + T + V): `T_thuc` (**chỉ dùng tính denta góc tù**), dao V, 引代, denta, Ri, 最長, L/Z最低, 内R/外R... |
| `tam` *(tùy chọn)* | `Loai,T,T_hienthi,Nguon,Nguoi_xac_nhan,Ghi_chu,Anh,Ngay` — thông số theo **tấm** (khoá Loai + T). `T_hienthi` = T thực để người dùng xem / tính toán (số trên nhãn "T thực:"). SIMA ghi **1 dòng chung** (`SS SIMA`) cho cả Trong lẫn Ngoài. `Nguon` = `OFT` hoặc `Bổ sung` (kèm ảnh + người xác nhận) |
| `cau_hinh` *(tùy chọn)* | `Khoa,Gia_tri,Ghi_chu` — `phien_ban_toi_thieu`: máy chạy bản thấp hơn hiện dải đỏ "Cần cập nhật" và bị khoá mọi lệnh ghi lên Sheet (vẫn đặt / sửa Fname được) |
| `trav_nho90` | Bảng độ dãn góc nhọn < 90° (3 dòng đầu: vật liệu / dao / độ dày theo cột) |
| `trav_nho90r` | Bảng R ngoài góc nhọn (cùng bố cục với `trav_nho90`) |
| `trav_thu` *(tùy chọn)* | `Loai,T,Goc,Dao,Denta,Ma_tam,Dinh_danh` — **góc bẻ thử** đo thực tế (`Dinh_danh = Bẻ thử`) và góc tool nội suy đã được duyệt (`Nội suy`). Chỉ dùng khi bảng TraV không tính được góc. Chưa điền `trav_thu=<gid>` trong `google_sheet.ini` thì bỏ qua bảng này khi tải |

**Góc bẻ thử / nội suy (card BẺ / UỐN):** khi bảng TraV không cho kết quả nào cho vật liệu + T + góc (vd. góc nhọn SIMA, độ dày ngoài bảng), tool tra `trav_thu` theo Loại (`SS`, `SUS`, `SS SIMA Ngoài`... — không phân biệt hoa thường / dấu) + T, từng dao:
- Có dòng đúng góc → dùng luôn, dòng tím dưới ô V: `[Bẻ thử] Góc bẻ thử tham khảo tấm xxxx` (hoặc `[Nội suy] Góc nội suy tham khảo tấm ...`). Dòng `V=` trên Fname giữ định dạng cũ.
- Góc **không làm tròn**: góc lẻ (vd. 76.3°) không trùng góc bẻ thử nào thì nội suy / ngoại suy như mọi góc khác.
- Không có → **nội suy** Denta tuyến tính từ 2 góc **bẻ thử** gần nhất kẹp 2 bên (vd. muốn 75°, có 70° và 80°). 2 góc cách nhau **> 10°** → cảnh báo đỏ và khi Đặt / Cập nhật Fname phải bấm **Yes** xác nhận đã hỏi quản lý (No = huỷ).
- Chỉ có góc bẻ thử **1 phía** → **ngoại suy** từ 2 góc bẻ thử gần nhất phía đó, chỉ khi góc cần tra cách góc đo gần nhất **≤ 2°** (vd. có 72° và 74°, tra 75° hoặc 76°; 76.4° thì không). Nhãn `[Ngoại suy]`, bắt xác nhận quản lý.
- **Miễn hỏi quản lý** khi 1 trong 2 góc gốc cách góc cần tra **chưa tới 1°** (vd. nội suy 60.5° từ 60°/85°, ngoại suy 74.5° từ 72°/74°).
- Dòng tím ghi rõ 2 góc gốc + mã tấm: `[Nội suy] từ góc 70° (tấm A1) và 80° (tấm B2)`. `Ma_tam` gửi lên Sheet cho góc tính ra: `A1 (70°) / B2 (80°)`.
- Gốc ưu tiên chỉ lấy góc **Bẻ thử**. Không đủ góc bẻ thử mới dùng cả góc **Nội suy / Ngoại suy** làm gốc — khi đó **luôn** hỏi quản lý (kể cả khi cách < 1°), dòng tím ghi `80° (nội suy: ...)`.
- **📋 Danh sách góc bẻ thử** (Cài đặt → 🗄 Dữ liệu): bảng toàn bộ góc bẻ thử / nội suy / ngoại suy trên máy (Loại, T, Góc, Dao, Denta, Mã tấm, Định danh, Trạng thái Đã duyệt / Chờ duyệt), lọc theo từ khoá và định danh, có nút ➕ bổ sung.
- Mỗi góc nội suy / ngoại suy vừa dùng để Đặt / Cập nhật Fname được tự gửi vào tab `update` chờ admin duyệt (`trav_thu`, `Dinh_danh=Nội suy`, `Ma_tam` = 2 tấm tham khảo); máy gửi lưu `trav_thu_local.csv` để dùng ngay và không gửi lại.
- Nhập góc bẻ thử mới: **Cài đặt ⚙ → 🗄 Dữ liệu → ➕ Bổ sung góc bẻ thử** (Loại, T, Góc, Dao, Denta, Mã tấm). Admin duyệt → Apps Script ghi vào tab `trav_thu` (tự tạo nếu chưa có; trùng Loai + T + Góc + Dao thì ghi đè).

**Thiết lập 1 lần:**
1. Tạo Google Sheet, mỗi file trong [`data/`](data/) → `File > Import > Upload` vào **1 tab riêng** (Insert new sheet).
2. `File > Settings > Locale = United States` (để số dùng dấu chấm `3.2`).
3. `Share > General access = Anyone with the link — Viewer`.
4. Điền vào [`data/google_sheet.ini`](data/google_sheet.ini): `spreadsheet_id` (đoạn giữa `/d/` và `/edit` của link) và `gid` từng tab (số sau `#gid=` khi bấm vào tab).

**Cập nhật:** tự động ở lần mở tool đầu tiên trong ngày, hoặc bấm **Cài đặt ⚙ → 🔄 Cập nhật từ Google Sheet**. Dữ liệu tải về được kiểm tra hợp lệ cả 4 bảng rồi mới lưu vào cache máy `%LOCALAPPDATA%\OHTA_TOOL\fname_data\`; lỗi mạng / Sheet sai định dạng thì giữ nguyên dữ liệu cũ. Máy chưa tải lần nào dùng CSV mẫu trong `data/`.

**Bổ sung vật liệu → admin phê duyệt (Cài đặt ⚙ → tab 🗄 Dữ liệu → ➕ Bổ sung vật liệu):**
- Cửa sổ `FnameMaterialWindow` gồm: **M=** (chọn mác có sẵn hoặc gõ mác mới), **Loại vật liệu** (gợi ý từ cột `Mat` của `trav_90`), **Z=** (có thể để trống), **Độ dày T=** (nhiều giá trị cách nhau dấu cách). Mặc định điền sẵn M= / T= đang nhập trên bảng.
- Bấm **⬆ Gửi yêu cầu duyệt** → yêu cầu được ghi **tạm** vào tab **`update`** trên Sheet (mọi máy, kể cả máy admin). Tab `catalog` trên Sheet **không đổi** cho tới khi admin duyệt, nhưng **máy gửi dùng được ngay**: bổ sung được lưu vào `%LOCALAPPDATA%\OHTA_TOOL\fname_data\catalog_local.csv` và luôn cộng lên catalog tải từ Sheet (`FnameLocalAdditions`). Sheet là dữ liệu gốc: mỗi lần đồng bộ Sheet (lần mở đầu ngày hoặc bấm Cập nhật), tool hỏi Apps Script các yêu cầu còn chờ của chính máy mình (`my_requests`) rồi chỉ giữ bổ sung **còn đang chờ duyệt**. Đã duyệt (Sheet có đủ) hoặc bị từ chối (không còn trong `update`) → xoá khỏi file, máy quay về đúng dữ liệu Sheet. Không hỏi được (mất mạng...) thì giữ nguyên.
- **Máy admin** = tên máy (`Environment.MachineName`) có trong tab **`admin`** (cột A, dòng 1 là tiêu đề `May_admin`, hiện có `NGUYEN-A-HUNG`, không phân biệt hoa thường). Thêm admin: gõ thêm tên máy vào cột A. Nên khoá tab `admin` bằng *Dữ liệu → Bảo vệ trang tính* để chỉ chủ Sheet sửa.
- Trên máy admin: **số đỏ** trên icon ⚙ của bảng Fname và trên tab **✅ Chờ phê duyệt** của Cài đặt = số yêu cầu đang chờ (`9+` khi lớn hơn 9). Mỗi yêu cầu có **✔ Duyệt** (áp vào `catalog`: mác có sẵn → gộp độ dày, ghi Loai, Z= trống thì giữ Z cũ; mác mới → thêm dòng) hoặc **✕ Từ chối** (có hỏi lại). Cả hai đều **xoá hẳn** dòng khỏi `update`. Duyệt xong, cache trên máy admin được cập nhật ngay; máy khác nhận ở lần cập nhật dữ liệu tiếp theo.
- Kiểm tra yêu cầu mới: ở **lần mở tool đầu tiên trong ngày** và khi bấm **🔄 Tải lại** (tab Chờ phê duyệt) hoặc **🔄 Kiểm tra** (tab ℹ Thông tin — dùng cho máy admin mới, khi tab Chờ phê duyệt chưa hiện). Số đỏ nhớ trong registry nên vẫn hiện khi mở lại CAD trong ngày.
- **Tab `update`** (script tự tạo): `Loai_du_lieu | Thong_so | May_gui | Thoi_gian | ID`. `Thong_so` là các cặp `key=value` cách nhau ` | ` (ví dụ `M=SPHC | Loai=SS | Z= | T=2.3 4.5`) nên thêm loại dữ liệu khác sau này không phải đổi cột.
- Ghi qua **Google Apps Script Web App** ([`data/google_sheet_apps_script.gs`](data/google_sheet_apps_script.gs), project "OHTA Fname - Ghi catalog" ở tài khoản openai.ohta@gmail.com) — các action `submit_request`, `list_requests`, `my_requests`, `approve_request`, `reject_request`. Cấu hình `write_url`, `write_key` trong [`data/google_sheet.ini`](data/google_sheet.ini). Sửa script → *Triển khai → Quản lý các tùy chọn triển khai → Sửa → Phiên bản mới* (URL giữ nguyên).
- Code: `Core/FnameUpdateRequest.cs` (định dạng `Thong_so`, đọc JSON, có unit test), `FnameCatalogData.TryParseThicknessInput / MergeThicknesses / Upsert`, `Services/FnameSheetWriter.cs`, tab Chờ phê duyệt trong `FnameSettingsWindow`.

> ⚠ Tên máy do tool tự gửi lên — người cố ý sửa code có thể giả tên máy admin. Đủ cho dùng nội bộ; cần chặt hơn thì thêm `admin_key` riêng chỉ lưu trên máy admin.

> ⚠ `write_key` là chìa khoá ghi Sheet — chỉ để trong `google_sheet.ini` nội bộ, không gửi ra ngoài.

> ⚠ Sheet để chế độ "ai có link đều xem được" — không đưa dữ liệu nhạy cảm vào Sheet này.

