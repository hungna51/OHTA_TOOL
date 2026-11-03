# 16. Chuyển lisp GRO sang C# — lộ trình và trạng thái

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> Kế hoạch người dùng duyệt 28/10/2026. **Cập nhật bảng trạng thái mỗi khi xong / nghiệm thu một giai đoạn.**

## Mục tiêu và luật

- Chuyển **toàn bộ** `Guro ZWCAD(gro).lsp` (~17.400 dòng, gồm GRO, XLN/CLN/DSN, GROKHUNG/GROTAM, GI/in PDF, DST/FIXFD/NETDST) sang C# (`ZwCadPlugin`) để **tăng tốc**. Đích cuối là **lệnh C# `GRO`**.
- **Chia nhỏ từng bước**, mỗi bước kiểm riêng. **Giữ bản lisp của bước để so A/B** tới khi nghiệm thu.
- Mỗi bước có công tắc 3 chế độ: `lisp` / `ab` / `cs`. Đặt bằng lệnh **`GROCS`**, lưu ở registry `HKCU\Software\OFT_Lisp\GRO\cs_<buoc>`.
  - `ab`: C# chỉ tính, không ghi bản vẽ; lisp chạy; so kết quả, in `[AB buoc] khop N muc` hoặc `[AB buoc] LECH k: …`; **áp kết quả lisp**, nên an toàn trên bản vẽ thật.
  - `cs`: áp kết quả C#.
- Lisp ↔ C# **chỉ trao handle / mã**. Dữ liệu giữa các bước C# nằm trong phiên `GroPhien` (theo document, tạo mới đầu `c:gro`). **Không** truyền toạ độ qua chuỗi; riêng chế độ `ab` lisp gửi số `rtos 2 15` và so có dung sai. Bài học mục 170: `rtos 2 6` làm rơi 147 nét.
- Mỗi bước tách 2 lớp, như mục 170:
  - `Core/<Buoc>.cs`: thuần, có test;
  - `CadServices/<Buoc>Cad.cs`: đọc / ghi bản vẽ;
  - hàm lisp `GRO_NET_<BUOC>`.
- Mỗi bước ghi dump `%TEMP%\gro_<buoc>_dump.txt` (`Core/DumpBuoc`). Phát lại ngoài CAD: `dotnet run --project ZwCadPlugin.Tests -- phatlai <buoc> <file> [tham so]`.
- **Nghiệm thu một bước** khi đạt cả ba:
  1. test đạt;
  2. `ab` trên bộ bản vẽ mẫu không còn lệch (hoặc lệch đã giải thích và người dùng duyệt);
  3. người dùng chạy `cs` trên bản vẽ thật thấy OK.

  Sau đó xoá lisp của bước đó, ghi mục lỗi mới, cập nhật bảng dưới.
- **Chép đúng logic lisp.** Chỗ cố ý khác (ví dụ không chép lỗi `vl-sort` bỏ phần tử, mục 22) phải ghi vào rule.

## Bảng trạng thái

| GĐ | Mã bước (`GROCS`) | Nội dung | Trạng thái |
|---|---|---|---|
| 0 | — | Hạ tầng: đo thời gian, công tắc, `GroPhien`, so A/B, dump / phát lại | **Xong** (mục 171), chưa chạy thật |
| — | — | Bước 6g đặt mã khắc vào hình | **Đã C#** (mục 170, nghiệm thu 65/65 + 95/95) |
| 1 | `phanloai` | Đọc + phân loại text, ghép số lượng, cặp đối xứng, CPL, mã khắc | Chưa |
| 2 | `sapxep` | Sắp xếp, gom hàng, khung / vùng | Chưa |
| 3 | `xulynet` | Xử lý nét + nhận Dim (3b) + lệnh XLN/CLN/DSN | Đang làm: đo con xong (mục 173) |
| 3a | `nhannet` | Phân loại nét cho nhận Dim (phần chậm nhất của 3b, 6.4 s) | **Chạy C# (`cs`)**: AB khớp 1630/1630; 6.61 s → 0.09 s; GRO còn 14.8 s (mục 173). Bản lisp đã gỡ (mục 184) |
| 3b | `dimdai3b` | Khớp Dim dài ở bước xử lý nét (`gro--geom-match-linear-dims`, 2.07 s) | **Chạy C# (`cs`)**: AB khớp 394/394; 2.14 s → < 0.05 s; GRO còn 10.8 s (mục 175). Bản lisp đã gỡ (mục 184) |
| 3c | `vongkin` | Dò vòng kín biên dạng khi nhận Dim (bước 1b, 0.91 s) | **Chạy C# (`cs`)**: AB khớp 557/557; ~0.95 s → < 0.05 s (mục 177). Bản lisp đã gỡ (mục 184) |
| 4 | `datbango` | Đặt bango + tên bản vẽ + kiểu chữ; chỉ mục không gian thay `ssget "_C"` | Chưa |
| 5a | `quethinh` | Quét + gom hình cắt | **Chạy C# (`cs`)**: AB khớp 100%; 2 lần quét 4.27 s → < 0.05 s; GRO 29.9 s → 20.7 s (mục 172). Bản lisp đã gỡ (mục 184) |
| 5b | `ghephinh` | Ghép hình ↔ text (chính, chiếu tia, Hungarian) | Chưa |
| 5c | `dimhinh` | Gán Dim cho hình + cắt tấm gộp | Chưa |
| 5c-a | `dimdai` | Khớp Dim dài trong thu thập Dim (khung) | **Chạy C# (`cs`)**: AB khớp 134/134; 1.39 s → 0.05 s; GRO còn 12.8 s (mục 174). Bản lisp đã gỡ (mục 184) |
| 5c-b | `locdim` | Vòng lọc đầu thu thập Dim (khung, 1.31 s) | **Chạy C# (`cs`)**: AB khớp 395/395 (chưa gặp DIMENSION); 1.34 s → < 0.05 s; GRO còn 9.1 s (mục 176). Bản lisp đã gỡ (mục 184) |
| 5d | `khungin` | Danh sách chi tiết + Khung_IN + cô đơn + GROKHUNG/GROTAM | Chưa |
| 5d-a | `dsct` | Chia nét cắt / kegaki vào danh sách chi tiết (0.39 s) | **Chạy C# (`cs`)**: AB khớp 66/66; 0.42 s → 0.14 s; GRO 6.6 s (mục 181). Bản lisp đã gỡ (mục 184) |
| 5d-b | `dongkhung` | Đóng khung + vẽ Khung_IN (0.33–0.39 s) | **Chạy C# (`cs`)**: AB khớp 66/66; 0.34 s → 0.24 s (mục 183). Bản lisp đã gỡ (mục 184) |
| 6 | `bocuc` | Bản chụp, xếp cụm phải, tách mirror, tách va chạm khung in | Chưa |
| 6f | `tachgian` | Tách va chạm khung in tấm đơn (0.47 s) | **Chạy C# (`cs`)**: AB khớp 10/10; 0.53 s → 0.16 s (mục 180). Bản lisp đã gỡ (mục 184) |
| 6a | `xepcum` | Xếp cụm mirror / gia công sang cột phải (1.43 s) | **Chạy C# (`cs`)**: AB khớp 10/10; 1.43 s → ~0.19 s; GRO còn 7.8 s (mục 178). Người dùng xác nhận bằng mắt OK. Bản lisp đã gỡ (mục 184) |
| 7 | `csv` | CSV + XData + báo cáo + khắc chữ (156) + SHT | Chưa |
| 8 | `pdf` | In PDF (GI, in nhanh cuối GRO) | Chưa |
| 9 | — | Lệnh C# `GRO` thay `c:gro`; DST/FIXFD/NETDST sang C#; lisp còn trình nạp | Chưa |
| 10 | — | Dọn lisp, cập nhật CLAUDE.md / README | Chưa |

> **29/10/2026 — DỪNG chuyển đổi (mục 184).** Bản lisp dự phòng của mọi bước "Chạy C#" đã **gỡ**: lisp chỉ còn gọi C#, C# lỗi thì GRO dừng. Các bước "Chưa" ở trên vẫn chạy lisp. File lisp còn **16 080 dòng** (từ 18 168).

### Số đo GĐ 0 (28/10, bản vẽ thật, tổng 29.9 giây) → THỨ TỰ LÀM ĐÃ ĐỔI

| Bước lisp | Giây | % | GĐ |
|---|---|---|---|
| xử lý nét (3b) | 9.94 | 33% | 3 |
| tạo khung Khung_IN | 9.23 | 31% | 5a–5d |
| cặp đối xứng thiếu hình (4d) | 2.16 | 7% | 5 |
| in báo cáo (+ in PDF, mở DST) | 2.07 | 7% | 7–8 |
| chọn đối tượng + khởi động (hộp thoại) | 3.34 | 11% | phần lớn là thời gian người dùng thao tác |
| xếp cụm + tách mirror + tách va chạm | 2.09 | 7% | 6 |
| phân loại text, ghép SL, sắp xếp, tạo / đặt bango | < 0.5 | ~1% | 1, 2, 4 |

**Thứ tự làm (theo tốc độ):** GĐ 3 → GĐ 5 (gồm 4d) → GĐ 6 → GĐ 7–8 → GĐ 1, 2, 4 (gần như không tăng tốc; chép logic khi gộp) → GĐ 9 → GĐ 10.
GĐ 5 nhận ghi chú / dòng số lượng từ lisp **bằng handle**, nên không phải đợi GĐ 1.

## Các mục lỗi

171. **GĐ 0 — hạ tầng chuyển lisp → C#** (28/10/2026).

   ### Lisp
   - `gro--moc` thay mọi `(setq gro--buoc "...")`: đặt mốc **và** cộng dồn thời gian bước vừa xong. Mốc theo bango (`tao entity - bango 7`, `dat vi tri - bango 7`) gộp theo phần trước `" - "`. Thêm mốc cho các đầu mục chưa có: 3 chọn đối tượng, 3b xử lý nét, 4 đọc phân loại, 4b mã khắc + ghép số lượng, 4d cặp thiếu hình, 5b zoom, 6 chuẩn bị, 7 CSV.
   - `gro--moc-bat-dau` (đầu `c:gro`): reset bảng giờ, gọi `gro_net_phien_moi` nếu DLL có.
   - `gro--moc-bao-cao` (cuối `c:gro`): in ra dòng lệnh
     ```
     [GRO] Thoi gian tung buoc (tong 23.4 giay):
        xu ly net (3b)                    6.10 giay  (26%)
        dat vi tri                        4.82 giay  (21%)
        ...
     ```
     Chỉ in bước ≥ 0.05 giây. **Gửi bảng này sau lần chạy đầu** để chốt thứ tự giai đoạn.
   - `gro--cs-che-do "buoc"` → `"lisp"` / `"ab"` / `"cs"`. DLL cũ / chưa nạp thì luôn `"lisp"`. Các giai đoạn sau dùng hàm này để rẽ nhánh.

   ### C# (`ZwCadPlugin`)
   - `Core/GroCongTac.cs`: danh sách 11 mã bước, đọc chế độ (lạ → `lisp`, an toàn).
   - `Core/GroPhien.cs`: phiên theo document.
   - `Core/SoSanh.cs`: so bản ghi `khoa|ten=gia tri`, trường số có dung sai, in `[AB …]`.
   - `Core/DumpBuoc.cs`: dump chung.
   - `GroBuocCommands.cs`:
     - `GRO_NET_CONG_TAC`, `GRO_NET_PHIEN_MOI`;
     - `GRO_NET_AB "buoc" "khoa|…" …`: so bản ghi lisp với bản ghi C# mà bước đó đặt vào phiên ở khoá `ab_<buoc>`;
     - lệnh **`GROCS`**: bảng chế độ + đặt từng bước, `TAT` đưa hết về lisp.
   - Lệnh phát lại tổng quát: `phatlai <buoc> <file> [tham so]`. Vẫn nhận cú pháp cũ `phatlai <file> [bango]` của mục 170.

   ### Kiểm chứng
   - Test **475/475** (21 ca mới).
   - Build 0 lỗi, DLL đã chép (28/10 13:36).
   - Lisp cân ngoặc, 386 → 392 form (+6 hàm), CRLF.
   - Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc171._ls`.

   > **Chưa chạy trong ZWCAD.** Nghiệm thu:
   > 1. Chạy `gro` như thường: cuối lệnh phải có bảng `Thoi gian tung buoc`; kết quả bản vẽ không đổi gì.
   > 2. Gõ `GROCS`: phải hiện 11 bước, tất cả `lisp`.

172. **GĐ 5 bắt đầu: đo con + sửa nhanh "đóng khung" + GĐ 5a quét hình sang C#** (28/10/2026).

   ### Số đo con (bản vẽ 66 tấm, sau khi thêm 15 mốc `gro--moc` trong 4d / 6c)

   | Bước con | Giây |
   |---|---|
   | 4d: quét hình (trong `gro--tach-tam-gop-cad`) | 2.08 |
   | khung: quét hình (`gro--tao-khung-dong-thuan`) | 2.11 |
   | khung: thu thập Dim (`gro--thu-thap-dim-units`) | 3.19 |
   | khung: đóng khung + vẽ Khung_IN | 3.18 |
   | còn lại (ghép hình chính / chiếu, gán Dim, cắt tấm gộp, danh sách chi tiết…) | < 0.8 |

   Cùng một phép quét `gro--quet-hinh-primitives` chạy **2 lần** mỗi GRO (4d và tạo khung). Phép quét ở 4d nằm trong vòng lặp cắt tấm gộp, nên mỗi lượt cắt lại quét lại.

   ### Sửa nhanh trong lisp: đóng khung (kết quả không đổi)
   Vòng đóng khung tính lại hộp cụm bango / mã khắc / F-name của **mọi tấm khác** qua COM ở 3 vòng lồng (n tấm × n tấm × số entity). Nay tính **một lần** vào `bb-khoi-tid` (chỉ tấm có khối trong `cum-khois-map`, giữ đúng nghĩa `(if k-list …)` cũ), 3 chỗ dùng chỉ tra lại. `gro--bb-gop-2` đã xử lý `nil` nên kết quả y hệt. Dự kiến bớt khoảng 3 giây.

   ### GĐ 5a: quét + gom hình sang C#
   - `Core/QuetHinh.cs`: chép `gro--quet-hinh-primitives`.
     1. Lọc nét theo khung phạm vi (nới 5 mm), bỏ chính khung (lệch ≤ 10 mm) và nét nằm trên mép khung (> 40 % cạnh, lệch ≤ 5 mm).
     2. **Gom nét chạm nhau (khe hộp ≤ 2.5 mm)**: lisp quét lặp n² × nhóm; điều kiện "chạm hộp nhóm" bị "chạm 1 thành viên" bao hàm, nên đây là **thành phần liên thông**. C# dùng union-find + quét theo X: cùng kết quả, 1000 nét < 200 ms.
     3. Bỏ hình ≤ 3 × 3, bỏ hình chứa ≥ 2 điểm chèn ghi chú (khung vẽ nhầm → `gro--hinh-loi-khung`), sắp diện tích giảm dần (sắp **ổn định**), gộp lỗ vào phôi chứa nó, đánh id từ 1.
   - `CadServices/QuetHinhCad.cs`: đọc LINE / ARC / CIRCLE / ELLIPSE / SPLINE / polyline trên `0-KAKOU` ở Model Space, theo **thứ tự CSDL** (đã chốt bằng so A/B, xem bên dưới). Trả về lisp đúng cấu trúc `((id (ename…) (x1 y1 x2 y2) (cx cy)) …)` với **ename thật và số thực đầy đủ**, không qua chuỗi; kèm danh sách khung nhầm. Ghi dump `%TEMP%\gro_quethinh_dump.txt`.
   - Lisp `gro--quet-hinh` (công tắc `quethinh`) thay **cả 2 chỗ gọi**:
     - `lisp`: như cũ;
     - `ab`: C# tính + lisp tính, so bằng `gro--quet-hinh-ab` → `GRO_NET_AB`; khoá = handle nhỏ nhất của hình; so `id`, số nét, danh sách handle, hộp với sai số 0.01; **áp kết quả lisp**;
     - `cs`: dùng kết quả C#.

     C# lỗi hoặc DLL cũ thì tự chạy lisp.
   - Phát lại: `dotnet run --project ZwCadPlugin.Tests -- phatlai quethinh <file>`.

   **Chỗ có thể lệch khi so A/B** (chưa biết, chờ số đo):
   - hộp bao `GeometricExtents` so với `vla-GetBoundingBox` (spline / ellipse);
   - `id` của các hình **cùng diện tích**, nếu `vl-sort` của lisp không ổn định hoặc bỏ phần tử trùng (mục 22).

   Kiểm chứng: test 488/488 (13 ca mới), build 0 lỗi, lisp cân ngoặc (+2 hàm). Bản lisp trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc172._ls`.

   ### Kết quả so A/B lần 1 (28/10, bản vẽ 66 tấm, 799 nét 0-KAKOU)
   - `[AB quethinh] LECH 4 (khop 68)` ở **cả hai** lần quét (4d và tạo khung). **Cả 72 hình khớp thành viên và hộp bao**; chỉ đảo **id** hai cặp: 31↔32 và 45↔46.
   - Mỗi cặp là hai tấm **cùng diện tích đúng từng bit** (29872.67, 25449.99…), nên id do **thứ tự trước khi sắp** quyết định.
   - Lần sửa 1 (sai gốc): cho hình gặp sau lên trước. Khớp 2 cặp đó, nhưng lần so thứ 2 lộ cặp mới 56↔57 (21FA6 / 21FAE, diện tích 13500).
   - **Nguyên nhân thật:** `ssget "X"` của ZWCAD trả nét theo **thứ tự ngược CSDL**, rồi lisp `cons` đảo lại, nên lisp duyệt **xuôi CSDL**. C# đã đảo một lần dư. Nhóm xếp theo nét gặp đầu tiên, nên đọc ngược thì nhóm có nét xen kẽ đổi chỗ. `vl-sort` thực ra **ổn định**.
   - Sửa: `QuetHinhCad.DocNet` đọc xuôi CSDL, sắp ổn định, bỏ khoá phụ. Phát lại dump lần 2 với quy tắc mới: **cả 3 cặp** ra đúng id lisp (31 = 2211C, 32 = 2243F, 45 = 22145, 46 = 22466, 56 = 21FA6, 57 = 21FAE). Test 490/490.
   - **Bài học:** cấu trúc trả về của `ssget` không định thứ tự; muốn khớp lisp tuyệt đối thì phải dò bằng dữ liệu thật, đừng đoán "lisp đảo" hay "lisp không ổn định".
   - Thời gian C# gom 799 nét: **15–33 ms**, so với ~2.06 giây của lisp, mỗi lần quét.

   - **AB lần 3 (sau khi sửa thứ tự đọc): `[AB quethinh] khop 72 muc` ở cả 4d lẫn tạo khung.** Đã đặt `quethinh = cs`.
   - **Chạy `cs` (28/10):** hai dòng "quét hình" biến khỏi bảng thời gian (< 0.05 s); tổng GRO **20.7 s**, lúc đầu GĐ 0 là 29.9 s. Còn chậm: xử lý nét 9.8 s, thu thập Dim 3.0 s, xếp cụm 1.4 s.

   > **Nghiệm thu:**
   > 1. `GROCS` → `quethinh` → `Ab`, chạy `gro`: dòng lệnh phải có `[AB quethinh] khop N muc` (2 lần, cho 4d và tạo khung) hoặc danh sách lệch.
   > 2. Không lệch thì đặt `Cs`, chạy lại, xem bảng thời gian: hai dòng "quét hình" phải tụt từ ~2 giây xuống dưới 0.3 giây.

173. **GĐ 3 bắt đầu: đo con bước xử lý nét + GĐ 3a phân loại nét sang C#** (28/10/2026).

   ### Số đo con bước 3b (bản vẽ thật, tổng GRO 26.4 s, sau khi thêm 11 mốc `gro--moc`)

   | Bước con (trong `gro--detect-geom-dims` / `gro--xu-ly-net`) | Giây |
   |---|---|
   | **3b dim: phân loại nét** | **6.42** |
   | 3b dim: khớp Dim dài (`gro--geom-match-linear-dims`) | 2.11 |
   | 3b dim: vòng kín (mục 143/145) | 0.91 |
   | 3b: chuyển layer từng đối tượng | 0.15 |
   | đọc text số, cung bo góc, nâng Dim góc, khớp lỗ / leader, khớp Dim góc, hợp nhất | < 0.2 mỗi bước |

   Phần chậm nhất là vòng phân loại đầu `gro--detect-geom-dims`. Nghi phạm: (1) bỏ CIRCLE trùng tâm bằng `vl-some` duyệt cả danh sách `circles` cho từng CIRCLE của bản vẽ (tối đa 500), tức O(n²) lời gọi `lambda` + `distance`; (2) mỗi entity của layer gốc một lần `entget` + `strcase` + `tblsearch "LAYER"`.

   ### GĐ 3a: phân loại nét sang C# (công tắc `nhannet`)
   - Bước mới trong `GROCS`: **`nhannet`** (12 bước). `xulynet` giữ cho phần còn lại của GĐ 3 — mỗi phần một công tắc để nghiệm thu riêng.
   - Lisp: phần 1 cũ của `gro--detect-geom-dims` chuyển **nguyên văn** sang `gro--phan-loai-net-lisp` (trả `(lines-cont arcs-cont circles tiny-circles)`). `gro--detect-geom-dims` gọi cầu nối `gro--phan-loai-net`:
     - `lisp`: bản lisp;
     - `ab`: C# tính + lisp tính, `gro--phan-loai-net-ab` gửi bản ghi (khoá `L:`/`A:`/`C:`/`T:` + handle, trường `i` = vị trí trong danh sách, CIRCLE thêm `x y r`) cho `GRO_NET_AB`; **áp kết quả lisp**;
     - `cs`: kết quả C#. C# lỗi / DLL cũ → bản lisp.
   - **Lisp gửi chính các selection set** (`ss` của bước xử lý nét, `ssget "X"` CIRCLE, `ssget "X"` ARC) sang `gro_net_phan_loai_net`, nên C# duyệt **đúng thứ tự `ssname`** của lisp — không phải đoán thứ tự CSDL như mục 172. Kết quả trả về là ename + số thực thật, không qua chuỗi.
   - `Core/PhanLoaiNet.cs` chép đúng luật:
     - layer so bằng `strcase` (chỉ đổi a–z, `HoaAscii`); linetype rỗng / `BYLAYER` → linetype của **layer gốc**, không có → `Continuous`;
     - LINE / ARC Continuous; CIRCLE **mọi** linetype, `r ≤ 1.5` → thêm vào tiny-circles;
     - CIRCLE cả bản vẽ (500 cái đầu): bỏ cái có tâm cách một circle đã có ≤ 0.5 (kể cả cái vừa thêm); ARC cả bản vẽ (500 cái đầu): bỏ cái đã có;
     - mỗi danh sách = **đảo ngược** thứ tự thêm (lisp `cons`);
     - tâm CIRCLE đổi WCS → **OCS** (group 10 lisp đọc là OCS; CIRCLE lật gương có pháp tuyến −Z thì x đổi dấu).
   - `CadServices/NhanNetCad.cs`: đọc entity, gọi Core, trả ResultBuffer, in `[Xu ly net] Phan loai net (.NET): ... ms`, ghi dump `%TEMP%\gro_nhannet_dump.txt`. Phát lại: `dotnet run --project ZwCadPlugin.Tests -- phatlai nhannet <file>`.

   ### Kiểm chứng
   - Test **500/500** (10 ca mới: thứ tự đảo, layer khác hoa/thường, linetype layer gốc, BYBLOCK bị bỏ, CIRCLE trùng tâm kể cả cái vừa thêm, giới hạn 500, OCS ±Z).
   - Build 0 lỗi 0 cảnh báo, DLL đã chép `C:\ZwCadPlugin` (28/10 15:55).
   - Lisp: ngoặc cân bằng, 394 → 398 form (+4 hàm: `gro--phan-loai-net-lisp`, `gro--phan-loai-net`, `gro--phan-loai-net-ab`, `gro--hdl`), khối chuyển giống hệt bản cũ, CRLF. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc173b._ls` (bản trước khi thêm mốc đo: `...truoc-muc173._ls`).
   - Registry đã đặt `cs_nhannet = ab`.

   ### Kết quả so A/B (28/10, bản vẽ 68 tấm)
   - ZWCAD **có** chuyển selection set lisp sang .NET (`SelectionSet`) — cách truyền này dùng được cho các bước sau.
   - `[AB nhannet] khop 1630 muc` ngay lần đầu (823 LINE, 36 ARC, 771 CIRCLE, 0 rất nhỏ; đầu vào 1682 + 832 + 36 đối tượng). Không lệch thứ tự, không lệch toạ độ.
   - C# chạy **4 ms**; dòng `3b dim: phan loai net` ở chế độ `ab` vẫn 6.61 s vì lisp cũng chạy. 771 CIRCLE × 500 lượt `vl-some` ≈ 385 nghìn lời gọi `lambda` — khớp nghi phạm O(n²).
   - Đã đặt `cs_nhannet = cs`. **Chạy `cs` (28/10):** `3b dim: phan loai net` 6.61 s → **0.09 s**; tổng GRO 21.4 s → **14.8 s** (lúc đầu GĐ 0: 29.9 s). Còn chậm: thu thập Dim (khung) 3.21 s, khớp Dim dài 2.09 s, xếp cụm 1.48 s, vòng kín 0.91 s.

   > **Nghiệm thu (bản đầu):**
   > 1. Chạy `gro` (xử lý nét tự động): dòng lệnh phải có `[Xu ly net] Phan loai net (.NET): ...` và `[AB nhannet] khop N muc`. Nếu thấy `tham so ... khong phai selection set` thì ZWCAD không chuyển pickset sang .NET như AutoCAD — báo lại để đổi cách truyền.
   > 2. Khớp thì `GROCS` → `nhannet` → `Cs`, chạy lại: dòng `3b dim: phan loai net` trong bảng thời gian phải tụt từ ~6.4 s xuống dưới 0.5 s.

174. **GĐ 5c-a: khớp Dim dài trong "thu thập Dim" (khung) sang C# + đo con thu thập Dim** (28/10/2026, người dùng chọn làm tiếp "Thu thập Dim").

   ### Vì sao chọn phần này
   `khung: thu thap dim` (`gro--thu-thap-dim-units`) chiếm 3.21 s / 14.8 s. Bước 3 của hàm (đóng gói Dim dài) duyệt **mọi** LINE trên layer Dim cho **mỗi** chữ số đo, mỗi lượt một `entget` + `angle` + phép thử chạm hộp; tìm được đường dim thì duyệt hết LINE thêm lần nữa để gom chân dim (~170 chữ số × ~400 LINE × 2). Các bước khác của hàm chưa đo — đã thêm mốc con: `khung: dim doc text so`, `khung: dim 2 net`, `khung: dim dai`, `khung: dim goc`, `khung: dim native + net le` (phần còn lại vẫn tính vào `khung: thu thap dim` = vòng lọc đầu).

   ### Thay đổi
   - Bước mới trong `GROCS`: **`dimdai`** (13 bước).
   - Lisp: phần tính của bước 3 chuyển **nguyên logic** sang `gro--dim-dai-lisp (texts-lin lines-cont)`, trả danh sách **song song** `texts-lin`: mỗi phần tử `nil` hoặc `(best-mem cand-ticks p-mid ray-dir)`. Phần đóng gói (`gro--bbox` các entity, `dim-units`, `used-ents`) giữ ở `gro--thu-thap-dim-units`. Cầu nối `gro--dim-dai` (lisp / ab / cs), so A/B `gro--dim-dai-ab` (khoá = vị trí + handle chữ số; so `co`, `mem`, `tick` theo đúng thứ tự, `pm` / `rd` dung sai 1e-6).
   - **Giao thức mới: lisp gửi list số thực thật**, không qua chuỗi: `(gro_net_dim_dai che ((ename v pt rot h bb) ...) (ename-line ...))`. C# đọc bằng `CadServices/LispCay.cs` (cây list / số / ename / nil; điểm 2–3 số ZWCAD gửi dạng Point2d / Point3d cũng đổi về list).
   - `Core/DimDai.cs` chép: `angle` (đưa về [0, 2π)), `gro--angle-diff-rad`, `gro--bbox-scale-15` (bb nil → hộp 20×15 quanh gốc, như lisp), `gro--line-bbox-collide-p` (+ `inters ... T`), `gro--dist-pt-seg`, `gro--dist-pt-infinite-line`, `gro--chuoi-thang-hang` (mục 169, cùng thứ tự `mem`), chọn đường dim gần tâm chữ nhất (so `<`, cái gặp trước thắng khi bằng), gom chân dim vuông góc (≤ 4.5 mm từ 2 đầu), hướng tia = tổng véc-tơ chân dim chuẩn hoá, không có thì `(-sin rot, cos rot)`. Danh sách chân dim đảo ngược như `cons`.
   - `CadServices/DimDaiCad.cs`: đọc đầu mút LINE, in `[DIM] Khop Dim dai (.NET): a/b chu so co duong dim, n LINE, t ms`, dump `%TEMP%\gro_dimdai_dump.txt`; phát lại `dotnet run --project ZwCadPlugin.Tests -- phatlai dimdai <file>`.

   ### Kiểm chứng
   - Test **514/514** (14 ca mới: dim 1 LINE + 2 chân dim, chuỗi 2 LINE thẳng hàng mục 169, LINE dài chạm hộp nhưng sai chiều dài, V nil, không có đường ~V, `angle` / lệch phương, hộp nil, đoạn cắt qua hộp).
   - Build 0 lỗi 0 cảnh báo, DLL đã chép `C:\ZwCadPlugin` (28/10 16:16).
   - Lisp: ngoặc cân bằng, 398 → 401 form (+3 hàm), khối chuyển giữ nguyên từng dòng (chỉ đổi thụt lề), CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc174._ls`.
   - Registry đã đặt `cs_dimdai = ab` (DLL cũ thì lisp tự chạy bản lisp).

   ### Kết quả so A/B (28/10, bản vẽ 68 tấm)
   - `[AB dimdai] khop 134 muc` ngay lần đầu; C#: 129/134 chữ số có đường dim, 392 LINE, **15 ms** (đọc 1 ms). Giao thức list số thực thật chạy tốt trên ZWCAD.
   - Số đo con thu thập Dim (chế độ `ab`, lisp vẫn chạy bước 3): vòng lọc đầu `khung: thu thap dim` **1.31 s**, `dim doc text so` 0.31 s, `dim dai` 1.39 s (lisp + C# + đóng gói `gro--bbox`), `dim native + net le` 0.13 s; `dim 2 net` và `dim goc` < 0.05 s. Tổng GRO 14.7 s.
   - Đã đặt `cs_dimdai = cs`. **Chạy `cs` (28/10):** `khung: dim dai` 1.39 s → **0.05 s**; tổng GRO 14.7 s → **12.8 s** (lúc đầu GĐ 0: 29.9 s). Còn chậm: 3b khớp Dim dài 2.07 s, xếp cụm 1.42 s, vòng lọc đầu thu thập Dim 1.31 s, vòng kín 0.93 s.

   > **Nghiệm thu (bản đầu — đã chạy, xem kết quả trên):**
   > 1. Đóng ZWCAD, chạy `ZwCadPlugin/build.ps1` (chép DLL), mở lại, chạy `gro`: phải có `[DIM] Khop Dim dai (.NET): ...` và `[AB dimdai] khop N muc`; bảng thời gian có thêm các dòng `khung: dim ...`.
   > 2. Khớp thì đặt `dimdai = cs`, chạy lại: dòng `khung: dim dai` phải tụt mạnh.

175. **GĐ 3b: khớp Dim dài ở bước xử lý nét sang C#** (28/10/2026, người dùng chọn "Khớp Dim dài 3b").

   ### Vì sao
   Sau mục 173–174, `3b dim: khop dim dai` (`gro--geom-match-linear-dims`, 2.07 s) là bước chậm nhất còn lại. Cùng kiểu vòng lồng chữ số × LINE với `entget` từng lượt, cộng thêm vòng lồng thứ ba khi nối chân dim thẳng hàng.

   ### Khác bản thu thập Dim (mục 174) — đã chép đúng từng điểm
   - Chọn đường dim theo khoảng cách tâm chữ → **đoạn** (`gro--dist-pt-seg`), không phải → trung điểm. Song song nới **0.28** rad, hoặc cả hai cùng gần đứng / gần ngang (`|cos| < 0.28` / `|sin| < 0.28`).
   - Chân dim so phương với **đường dim** (không phải chữ): vuông góc (±0.28) hoặc tick chéo 45° (±0.20) dài < 15; cách đầu mút ≤ **4.0**. Mỗi chân dim nối thêm đoạn thẳng hàng (lệch ≤ 0.15 rad, đầu mút ≤ 3.5 mm).
   - Không có đường dim ≈ V: dò **cặp tick** (ngắn ≤ max(30, 5h), trung điểm cách chữ ≤ max(50, V/2 + 25)) có khoảng chiếu ≈ V, lệch ngang ≤ 8; gom mảnh đường dim song song giữa 2 tick (lệch ≤ 4, ra ngoài tick ≤ 3).
   - **Kết quả là một danh sách tích luỹ** `res-dim-ents` dùng chung cho mọi chữ số: chữ sau thấy phần tử chữ trước đã thêm (phép thử `member res-dim-ents` khi nối chân dim). C# giữ đúng trạng thái đó và thứ tự `cons`.
   - Thông báo `Dim "700.8": duong dim chinh ghep tu 2 LINE thang hang (muc 169)`: C# trả `(chỉ-số-chữ số-LINE)`, **lisp in** bằng chuỗi của chính nó (không gửi chuỗi chữ Nhật qua .NET).

   ### Thay đổi
   - Bước mới trong `GROCS`: **`dimdai3b`** (14 bước).
   - Lisp: `gro--geom-match-linear-dims` **giữ nguyên** làm bản lisp; chỗ gọi trong `gro--detect-geom-dims` đổi sang cầu nối `gro--dim-dai-3b` (lisp / ab / cs). So A/B `gro--dim-dai-3b-ab`: khoá = handle nét Dim, trường `i` = vị trí trong `res-dim-ents` (so đúng tuyệt đối).
   - C#: `Core/DimDai3b.cs` (dùng lại hàm hình học của `Core/DimDai.cs`), `CadServices/DimDai3bCad.cs`; phần đọc tham số tách thành `DimDaiCad.DocVao` dùng chung với mục 174. Hàm lisp `gro_net_dim_dai_3b`. Dump `%TEMP%\gro_dimdai3b_dump.txt`, phát lại `phatlai dimdai3b <file>`.

   ### Kiểm chứng
   - Test **517/517** (3 ca mới: một bản vẽ thu nhỏ có dim 1 LINE + 2 chân dim + đoạn nối chân dim, dim tách 2 LINE (mục 169), dim chỉ có cặp tick + mảnh giữa; kiểm đúng thứ tự `res-dim-ents`; cùng chữ 2 lần không thêm trùng).
   - Build 0 lỗi 0 cảnh báo, DLL đã chép `C:\ZwCadPlugin` (28/10 16:26).
   - Lisp: ngoặc cân bằng, 401 → 403 form (+2 hàm), CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc175._ls`.
   - Registry `cs_dimdai3b = ab`.

   ### Kết quả so A/B (28/10, bản vẽ 68 tấm)
   - `[AB dimdai3b] khop 394 muc` ngay lần đầu (131 chữ số, 407 LINE → 394 nét Dim, C# **25 ms**). Tổng nét Dim của bước xử lý nét vẫn 397 như trước.
   - Đã đặt `cs_dimdai3b = cs`. **Chạy `cs` (28/10):** `3b dim: khop dim dai` 2.14 s → **< 0.05 s** (biến khỏi bảng); tổng GRO 12.8 s → **10.8 s** (lúc đầu GĐ 0: 29.9 s). Còn chậm: xếp cụm 1.43 s, vòng lọc đầu thu thập Dim 1.31 s, vòng kín 0.92 s, danh sách chi tiết 0.39 s.

   > **Nghiệm thu (bản đầu — đã chạy, xem kết quả trên):** chạy `gro` → phải có `[Xu ly net] Khop Dim dai (.NET): ...` và `[AB dimdai3b] khop N muc` (N = số nét Dim dài); khớp thì đặt `cs`, dòng `3b dim: khop dim dai` phải tụt từ ~2.1 s.

176. **GĐ 5c-b: vòng lọc đầu của "thu thập Dim" sang C#** (28/10/2026, người dùng chọn "Vòng lọc thu thập Dim").

   ### Vì sao
   Sau mục 175 (GRO 10.8 s), `khung: thu thap dim` = vòng lọc đầu của `gro--thu-thap-dim-units` còn **1.31 s**. Với **mọi** entity của `ss-all-dims` (cả TEXT) nó gọi `entget`, `gro--layer-dau-ra-p` (thêm một `entget`), `vl-some` qua mọi ghi chú, `gro--bango-ent-p` (thêm `entget`), `member all-k-ents`, `gro--bbox` qua COM và `vl-some` qua mọi hộp ghi chú — trong khi chỉ LINE / ARC / DIMENSION được giữ.

   ### Thay đổi
   - Bước mới trong `GROCS`: **`locdim`** (15 bước).
   - Lisp: vòng lọc chuyển **nguyên văn** sang `gro--loc-dim-net-lisp (ss-all-dims notes note-bbs all-k-ents)` → `(lines-cont arcs-cont native-dims)`. Cầu nối `gro--loc-dim-net` (lisp / ab / cs), so A/B `gro--loc-dim-net-ab` (khoá `L:` / `A:` / `D:` + handle, `i` = vị trí, so tuyệt đối).
   - Lisp gửi C# **chính `ss-all-dims`** (giữ thứ tự `ssname`), `note-bbs` (list số thực thật, phần tử nil giữ nguyên) và `all-k-ents` (ename).
   - `Core/LocDimNet.cs`: chỉ xét LINE / ARC / DIMENSION (tên DXF qua `ObjectId.ObjectClass.DxfName` — ARC_DIMENSION / LARGE_RADIAL_DIMENSION không phải "DIMENSION", như lisp). Điều kiện: không ở không gian giấy (`BlockId` ≠ Model Space), layer (đổi hoa a–z) không thuộc danh sách layer đầu ra và không chứa `KHUNG_IN` / `CANH_BAO` / `0-KAKOU` / `2-F-NAME` / `3-KEGAKI`, không thuộc cụm bango, có hộp bao, tâm hộp không nằm trong hộp ghi chú nới 1.0, rộng và cao < 2000. Mỗi danh sách đảo thứ tự như `cons`.
   - **Bỏ hai phép thử mà kết quả không đổi:** `gro--bango-ent-p` (với LINE / ARC / DIMENSION luôn sai — không phải TEXT, không có group 7; nhánh `(assoc -3 ed)` không bao giờ đúng vì `entget` không xin XData) và so ename ghi chú (ghi chú là TEXT / MTEXT).
   - **Chỗ có thể lệch:** hộp bao C# = `GeometricExtents`, lisp = `vla-GetBoundingBox`. Với LINE / ARC hai cái trùng; với DIMENSION chưa chắc → so A/B sẽ cho biết.
   - `CadServices/LocDimNetCad.cs`, hàm lisp `gro_net_loc_dim_net`, in `[DIM] Loc net Dim (.NET): ...`, dump `%TEMP%\gro_locdim_dump.txt`.

   ### Kiểm chứng
   - Test **522/522** (5 ca mới: 13 entity đủ mọi lý do bị loại — layer chứa 0-KAKOU, giấy, cụm bango, tâm trong ghi chú, quá dài, không có hộp, layer chữ thường `khung_in2`, TEXT không gom; thứ tự đảo; mép hộp ghi chú nới 1.0).
   - Build 0 lỗi 0 cảnh báo, DLL đã chép `C:\ZwCadPlugin` (28/10 16:38).
   - Lisp: ngoặc cân bằng, 403 → 406 form (+3 hàm), khối chuyển giữ nguyên (chỉ đổi thụt lề), CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc176._ls`.
   - Registry `cs_locdim = ab`.

   ### Kết quả so A/B (28/10, bản vẽ 68 tấm)
   - `[AB locdim] khop 395 muc` ngay lần đầu (622 đối tượng → 392 LINE, 3 ARC, 0 DIMENSION; C# **7 ms**). Bản vẽ này không có DIMENSION nên chỗ hộp bao có thể lệch **chưa được kiểm** — gặp bản vẽ có DIMENSION thì chạy lại `ab`.
   - Đã đặt `cs_locdim = cs`. **Chạy `cs` (28/10):** `khung: thu thap dim` 1.34 s → **< 0.05 s** (biến khỏi bảng); tổng GRO 10.8 s → **9.1 s** (lúc đầu GĐ 0: 29.9 s). Còn chậm: xếp cụm 1.42 s, vòng kín 0.91 s, đặt mã khắc 0.52 s, tách va chạm khung in 0.46 s, danh sách chi tiết 0.38 s.

   > **Nghiệm thu (bản đầu — đã chạy, xem kết quả trên):** chạy `gro` → phải có `[DIM] Loc net Dim (.NET): ...` và `[AB locdim] khop N muc`; khớp thì đặt `cs`, dòng `khung: thu thap dim` phải tụt từ ~1.3 s.

177. **GĐ 3c: dò vòng kín biên dạng (bước 1b nhận Dim, mục 143/145) sang C#** (28/10/2026, người dùng chọn "Vòng kín").

   ### Vì sao
   Sau mục 176 (GRO 9.1 s), `3b dim: vong kin` còn **0.91 s**. `gro--es-vong-kin` đếm bậc đầu mút bằng danh sách kết hợp (`assoc` + `subst` mỗi lần thêm → O(n²) mỗi lượt tỉa), lặp tới khi hết nét treo; trước đó còn một vòng `entget` toàn bộ ss để lọc layer, và `vl-remove-if member` bỏ nét vòng kín khỏi lines-cont / arcs-cont.

   ### Thay đổi
   - Bước mới trong `GROCS`: **`vongkin`** (16 bước).
   - Lisp: bước 1b chuyển **nguyên logic** sang `gro--vong-kin-geo-lisp (lay-goc ss lines-cont arcs-cont)` → `(vk-geo lines-cont arcs-cont)`; dòng `Bao ve N net bien dang (vong kin)` vẫn do `gro--detect-geom-dims` in. Cầu nối `gro--vong-kin-geo` (lisp / ab / cs), so A/B `gro--vong-kin-geo-ab` (khoá = handle, `i` = vị trí trong vk-geo, so tuyệt đối). `gro--es-vong-kin` **giữ nguyên** — còn 2 chỗ khác dùng (dò nét cong, xử lý nét).
   - `Core/VongKin.cs`: lọc layer (đổi hoa a–z, vk-geo = `cons` → đảo ss), chỉ LINE / ARC / LWPOLYLINE / POLYLINE / SPLINE, bỏ polyline **kín**, bỏ nét không lấy được đầu / cuối; khoá đầu mút = `(rtos x 2 1)`; đếm bậc bằng từ điển, tỉa nét có đầu bậc 1 tới khi ổn định; **mỗi lượt đảo thứ tự** như `cons` của lisp (kể cả lượt cuối không bỏ gì).
   - **Khoá đầu mút:** C# làm tròn 1 số lẻ trên dạng 15 chữ số có nghĩa, nửa ra xa 0 (cách CAD hiển thị số). Chưa biết chắc `rtos` của ZWCAD làm tròn đúng điểm "nửa" (vd `1234.25`) theo cách nào, và `-0.04` lisp có thể ra `-0.0` còn C# ra `0.0`. Chỉ ảnh hưởng khi hai đầu mút lệch nhau rất ít mà nằm hai bên ranh làm tròn — so A/B sẽ bắt được.
   - `CadServices/VongKinCad.cs`: đầu / cuối = `Curve.StartPoint / EndPoint` (cùng nguồn với `vlax-curve-getStartPoint`), cờ kín `Polyline` / `Polyline2d` / `Polyline3d.Closed`; trả luôn lines-cont / arcs-cont đã bỏ nét vòng kín (giữ thứ tự). Hàm lisp `gro_net_vong_kin`, in `[Xu ly net] Do vong kin (.NET): ...`, dump `%TEMP%\gro_vongkin_dump.txt`.

   ### Kiểm chứng
   - Test **527/527** (5 ca mới: chữ nhật 4 nét + nhánh treo 2 đoạn + nét lẻ + polyline kín + khác layer + CIRCLE → đúng 4 nét, đúng thứ tự sau 3 lượt tỉa; khoá điểm lệch 1e-8 vẫn trùng; làm tròn nửa / số âm / -0).
   - Build 0 lỗi 0 cảnh báo, DLL đã chép `C:\ZwCadPlugin` (28/10 16:51).
   - Lisp: ngoặc cân bằng, 406 → 409 form (+3 hàm), CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc177._ls`.
   - Registry `cs_vongkin = ab` (DLL cũ thì lisp tự chạy bản lisp).

   ### Kết quả so A/B (28/10, bản vẽ 68 tấm)
   - `[AB vongkin] khop 557 muc` ngay lần đầu (1682 đối tượng → 557 nét vòng kín, C# **19 ms**) — khoá đầu mút làm tròn không gây lệch nào trên bản vẽ này.
   - Đã đặt `cs_vongkin = cs`. **Chạy `cs` (28/10):** `3b dim: vong kin` ~0.95 s → **< 0.05 s** (biến khỏi bảng); tổng GRO 9.6 s, trong đó 2.31 s là chọn đối tượng (người dùng thao tác) — phần máy chạy ~7.3 s (lúc đầu GĐ 0: 29.9 s tổng). Còn chậm: xếp cụm 1.43 s, khởi động 1.07 s, đặt mã khắc 0.66 s, tách va chạm khung in 0.48 s, đóng khung 0.43 s, danh sách chi tiết 0.38 s.

   > **Nghiệm thu (bản đầu — đã chạy, xem kết quả trên):** đóng ZWCAD → `build.ps1` → chạy `gro`: phải có `[Xu ly net] Do vong kin (.NET): ... 557 net vong kin` và `[AB vongkin] khop 557 muc`; khớp thì đặt `cs`, dòng `3b dim: vong kin` phải tụt từ ~0.9 s.

178. **GĐ 6a: xếp cụm mirror / gia công sang cột phải — phần nặng sang C#** (28/10/2026, người dùng: "chuyển toàn bộ phần này sang C#").

   ### Vì sao
   `xep cum sang cot ben phai` (`gro--xep-cum-phai`) còn **1.43 s** — lớn nhất sau mục 177. Hàm duyệt **toàn bộ Model Space** (~5000 đối tượng, có cả bản chụp) và lấy hộp bao qua COM từng cái ở 3 chỗ: bước 4 gom hình học theo khung, `gro--bo-cuc-cot` tìm vật cản vùng cột, bước 9 kiểm còn sót — bước 9 quét lại cả bản vẽ **cho mỗi cụm**. Đã thêm 7 mốc đo con (`xep cum: quet hinh hoc / gom entity / bo cuc cot / doi entity / nhan nhom / kiem con sot / cap nhat ds`, chế độ `cs` thêm `xep cum: C#`).

   ### Phân công
   | Bên | Làm gì |
   |---|---|
   | lisp | bước 1–3 (lọc + sắp cụm, khung), bước 5 (gom ename từng cụm từ `gro--danh-sach-chi-tiet` / ghi chú / bảng / cụm bango), bước 8 tạo nhãn nhóm (chữ Nhật dựng bằng `bg--codes-str`, helper `gro--xep-cum-ten-nhom`), bước 10–11 cập nhật `kq-tk` / `gro--danh-sach-chi-tiet` / `gro--danh-sach-tam` + báo cáo |
   | C# (`Core/XepCum.cs`) | `gro--bo-cuc-cot` (bề rộng cột kể cả chỗ bản nghịch mục 112, tránh vật cản — X_L tăng **theo thứ tự ss** nên kết quả phụ thuộc thứ tự, giữ đúng; xếp dọc theo nhóm, khe 10 / 50 / 300, nhãn cao 20), bước 7 dời cụm, bước 9 kiểm còn sót + dời nét dim `1-SUNPOU` chưa gán nằm trọn hộp cũ (mục 134 / 141, phép thử khung phạm vi mục 135, bỏ đối tượng của tấm không xếp) |
   | C# CAD (`CadServices/XepCumCad.cs`) | đọc layer / XData `GRO_CHUP_COPY` / hộp bao (`GeometricExtents`); **mọi phép dời ghi vào bảng độ dời**, hộp "hiện tại" = hộp gốc + độ dời (y như lisp đọc lại sau mỗi `vla-move`); chế độ `cs` áp độ dời một lần mỗi entity (`TransformBy`), entity không dời được (vd layer khoá) thì bỏ qua như `vl-catch-all-apply` |

   - **Bước 4 (quét hình học cả bản vẽ) ở chế độ `cs` chỉ chạy khi có cụm KHÔNG có trong `gro--danh-sach-chi-tiet`** (`gro--xep-cum-can-geom`) — kết quả quét chỉ dùng cho cụm như vậy, nên bỏ qua không đổi kết quả. Chế độ `lisp` / `ab` giữ nguyên.
   - Lisp gửi C# **chính `ss-model`** (thứ tự `ssname`). Hàm lisp `gro_net_xep_cum`; công tắc mới **`xepcum`** (17 bước). Chế độ `ab`: C# chỉ tính (gọi **trước** khi lisp dời), lisp chạy đầy đủ, `gro--xep-cum-ab` gửi bản ghi `XL` (x, đã né), `C<i>` (tid, dx, dy), `N<i>` (vị trí nhãn), `SOT` (số + handle theo thứ tự), `DIM` (số nét dim dời theo) — dung sai 1e-6.
   - **Chỗ có thể lệch:** hộp bao C# `GeometricExtents`, lisp `vla-GetBoundingBox` — với TEXT / MTEXT có thể khác → ảnh hưởng vật cản vùng cột và "còn sót". So A/B sẽ cho biết.

   ### Kiểm chứng
   - Test **536/536** (9 ca mới: X_L tăng theo thứ tự ss 2000 → 3500 → 4700, bản chụp / `*_CHUP` không tính; dx dy 3 cụm; khe cùng nhóm 50, nhóm mới 300 + nhãn; vị trí nhãn; còn sót 2 cái đúng thứ tự `cons`; dim `1-SUNPOU` dời theo; đối tượng tấm không xếp và khung phạm vi bị bỏ; khung phạm vi theo hộp dung sai 0.5).
   - Build 0 lỗi 0 cảnh báo, DLL đã chép `C:\ZwCadPlugin` (28/10 17:07).
   - Lisp: ngoặc cân bằng, 409 → 413 form (+4 hàm), bước 6–9 bản lisp giữ nguyên (chỉ lồng vào nhánh), CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc178._ls` (trước mốc đo), `...truoc-muc178b._ls` (trước khi nối C#).
   - Registry `cs_xepcum = ab`.

   ### Kết quả so A/B (28/10, bản vẽ 68 tấm)
   - `[AB xepcum] khop 10 muc` ngay lần đầu (X_L, còn sót, dim dời theo, 5 cụm dx / dy, 2 nhãn nhóm); C# quét 3978 đối tượng, tính **57 ms**. Hộp bao `GeometricExtents` không gây lệch trên bản vẽ này. Dòng `Xep cum sang phai: 5 cum ... cot X=14939.3` không đổi.
   - Số đo con bản lisp (chế độ `ab`): quét hình học 0.32 s, bố cục cột 0.2 s, **kiểm còn sót 0.78 s**, gom entity 0.1 s, cập nhật 0.11 s; dời entity + nhãn < 0.05 s.
   - Đã đặt `cs_xepcum = cs`. **Chạy `cs` (28/10):** xếp cụm 1.43 s → **~0.19 s** (còn `gom entity` 0.08 + `cap nhat ds` 0.11 ở lisp; phần C# < 0.05 s). Tổng GRO **7.8 s**, trong đó 2.2 s chọn đối tượng — phần máy chạy ~5.6 s (lúc đầu GĐ 0: 29.9 s tổng). **Người dùng xác nhận bằng mắt: cột phải đúng như trước** (5 cụm, 2 cặp mirror, nhãn nhóm). Còn lại: khởi động 0.98 s, đặt mã khắc 0.67 s, tách va chạm khung in 0.47 s, danh sách chi tiết 0.38 s, đóng khung 0.33 s, đọc text số (khung) 0.3 s.

   > **Nghiệm thu (bản đầu — đã chạy, xem kết quả trên):** chạy `gro` → phải có `[GRO] Xep cum (.NET): ... (chi tinh de so A/B)` và `[AB xepcum] khop N muc`; dòng `Xep cum sang phai: 5 cum ... cot X=...` như cũ. Khớp thì đặt `cs`: bảng thời gian có `xep cum: C#`, các dòng `xep cum: bo cuc cot / doi entity / kiem con sot` biến mất; kiểm bằng mắt 5 cụm cột phải + 2 cặp mirror tách như trước.

179. **DLL đổi tên `GROZWCAD.dll`; lisp là nơi DUY NHẤT nạp DLL; lộ trình chuyển nốt toàn bộ GRO sang C#** (28/10/2026, người dùng: "chuyển nốt các phần còn lại sang C#, lisp chỉ còn tác dụng load DLL mỗi khi mở chương trình; DLL đổi tên GROZWCAD.dll để phân biệt với các chương trình khác").

   ### Đổi tên DLL (đã làm)
   | | Trước | Nay |
   |---|---|---|
   | Tên assembly | `ZwCadPlugin.dll` (+ bản phụ `ZwCadPlugin2.dll`) | **`GROZWCAD.dll`** (`<AssemblyName>GROZWCAD</AssemblyName>`; namespace C# giữ `ZwCadPlugin`) |
   | Thư mục chép | `C:\ZwCadPlugin\` (chung với `HelloZw.dll`…) | **`C:\GROZWCAD\`** (riêng) + bản ở `laptrinhlisp\GROZWCAD.dll` |
   | Ai nạp DLL | khoá ZWCAD `HKCU\Software\ZWSOFT\ZWCAD\2026\en-US\Applications\ZwCadPlugin` (LOADCTRLS 2 = tự nạp khi mở) **và** lisp `gro--nap-net` | **chỉ lisp**: cuối `Guro ZWCAD(gro).lsp` gọi `(gro--nap-net)` ngay khi file được nạp; khoá tự nạp cũ **đã gỡ** (sao lưu `backup/zwcad-autoload-ZwCadPlugin.reg` — nhấp đúp file này là khôi phục) |
   | Tìm DLL | 9 đường dẫn tên cũ, lặp ở 2 chỗ | hàm mới `gro--tim-dll`: `C:/GROZWCAD/GROZWCAD.dll` → Support path → `laptrinhlisp/GROZWCAD.dll` → `bin/Debug` → `bin/Release`; `gro--nap-net` và `c:netreload` dùng chung |
   | Thông báo khi nạp | `[ZwCadPlugin] ...` | `[GROZWCAD] Da nap GROZWCAD.dll ...` + `[GRO] Da nap ban .NET: C:/GROZWCAD/GROZWCAD.dll` |

   - Vì sao phải gỡ khoá tự nạp: để nguyên thì ZWCAD nạp **cả** `ZwCadPlugin.dll` cũ lẫn `GROZWCAD.dll` mới — hai DLL đăng ký cùng tên lệnh / hàm lisp (`GRO_NET_*`, `DST`, `GROCS`…) → lỗi trùng lệnh, hoặc lisp gọi nhầm hàm của DLL cũ (thiếu các bước mới).
   - File cũ `C:\ZwCadPlugin\ZwCadPlugin.dll`, `ZwCadPlugin2.dll` **để nguyên** (không xoá) — không còn ai nạp. Xoá tay khi đã chạy ổn.
   - Không có XAML / tài nguyên nào ghi tên assembly (`pack://...;component`) — đã kiểm, đổi tên không làm hỏng giao diện.
   - `KTech_Ribbon/lisp/Guro_ZWCAD_gro.lsp` (bản sao lisp cho ribbon) **chưa đổi** — vẫn tìm tên DLL cũ. Nếu ribbon được dùng thì phải đồng bộ.
   - Build 0 lỗi 0 cảnh báo, `C:\GROZWCAD\GROZWCAD.dll` 28/10 17:25. Lisp: ngoặc cân bằng, +1 hàm, CRLF. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc179._ls`.

   > **ĐÃ NGHIỆM THU (28/10):** mở lại ZWCAD in `[GROZWCAD] Da nap GROZWCAD.dll ...` + `[GRO] Da nap ban .NET: C:\GROZWCAD\GROZWCAD.dll`, không còn `[ZwCadPlugin]`.
   > Nghiệm thu gốc: đóng hẳn ZWCAD rồi mở lại. Dòng lệnh phải có `[GRO] Da nap ban .NET: C:/GROZWCAD/GROZWCAD.dll` và `[GROZWCAD] Da nap GROZWCAD.dll ...`, **không** còn `[ZwCadPlugin] ...`. Gõ `GROCS` phải thấy đủ 17 bước như cũ; chạy `gro` như thường.

   ### Lộ trình chuyển nốt (đích: lisp chỉ còn trình nạp)
   Đã chạy C#: 5a quét hình, 3a/3b/3c nhận Dim (phân loại nét, khớp Dim dài, vòng kín), 5c-a/5c-b thu thập Dim, 6a xếp cụm, 6g đặt mã khắc (mục 170–178). Còn lại, theo thứ tự (mỗi bước vẫn lisp/ab/cs + so A/B như cũ, **trừ** bước cuối):

   | # | Phần | Thời gian lisp hiện tại | Ghi chú |
   |---|---|---|---|
   | 1 | 6 còn lại: tách va chạm khung in, tách cặp mirror, bản chụp | 0.47 + 0.16 + … s | ghi bản vẽ, làm như mục 178 (bảng độ dời) |
   | 2 | 5d danh sách chi tiết + đóng khung / vẽ Khung_IN + tổng hợp ds tấm | 0.38 + 0.33 + 0.1 s | |
   | 3 | 5b/5c còn lại: ghép hình chính / chiếu tia / Hungarian, gán Dim cho hình, đọc text số (khung + 3b), cung bo góc, dim 2 nét / góc / native / nét lẻ, cắt tấm gộp (4d) | ~0.9 s | dùng lại `DimDai`, `QuetHinh` |
   | 4 | 6g phần lisp: chọn tấm + đo hộp cụm trước khi gọi C# | ~0.6 s | |
   | 5 | GĐ 1 + 2: đọc / phân loại text (chuỗi Nhật CP932!), ghép số lượng (mục 168), cặp đối xứng, CPL, mã khắc, sắp xếp + vùng | < 0.3 s | rủi ro encoding — kiểm kỹ cả máy kiểu A |
   | 6 | GĐ 4: tạo bango / mã khắc / F-name, đặt vị trí (chỉ mục không gian thay `ssget "_C"`), tên bản vẽ, kiểu chữ `guro` | ~0.3 s | |
   | 7 | 3 còn lại: chuyển layer từng đối tượng, XLN / CLN / danh sách nét chưa đổi | 0.15 s | |
   | 8 | GĐ 7–8: CSV ghi đè + XData + báo cáo + khắc chữ (156) + SHT; in PDF (GI, in nhanh cuối GRO) | 0.3–0.7 s | |
   | 9 | GĐ 9: **lệnh C# `GRO`** điều phối toàn bộ (hộp thoại .NET có sẵn), `GROKHUNG`/`GROTAM`, `GI`, `FIXFD`, `NETDST`/`NETRELOAD`/`DST`; khởi động 1 s (hộp thoại, đọc registry) | — | so `GRO` C# với `c:gro` trên bộ bản vẽ mẫu |
   | 10 | GĐ 10: thu `Guro ZWCAD(gro).lsp` về **trình nạp** (~30 dòng: `gro--tim-dll` + NETLOAD + thông báo); bản lisp đầy đủ chuyển vào `backup/` | — | chỉ làm sau khi người dùng chạy C# `GRO` ổn trên bản vẽ thật |

180. **GĐ 6f: tách va chạm khung in sang C#** (28/10/2026 — phần 1 lộ trình mục 179).

   - `tach va cham khung in tam don` (`gro--tach-va-cham-khung-in`, mục 111 / 121) 0.47 s: mỗi lượt duyệt mọi cặp khung bằng `nth` trên danh sách (O(n³) mỗi lượt), mỗi hướng thử dò mọi khung tới 40 bước, rồi `vla-move` từng entity của tấm bị dời.
   - `Core/TachVaCham.cs` chép đúng: cặp giao lớn nhất (bỏ cặp cả hai ở cột xếp phải; giao phải rộng > 0.1 hai chiều), tấm cố định = tấm trong cột phải / diện tích lớn hơn / bằng thì tid nhỏ hơn; `gro--tg-chon-huong` 4 hướng (+X, −X, +Y, −Y), dời tới khi tách mọi khung khe `gap`, chọn quãng ngắn nhất; không hướng nào sạch → theo cạnh ngắn HCN giao; mọi khung cùng tid thay bằng **cùng một** phần tử mới (như `mapcar` lisp, phép thử `eq` lượt sau giữ đúng); số cặp còn chồng.
   - `CadServices/TachVaChamCad.cs`: lisp gửi khung `(tid bb (ename...))` — ename tính **như bản lisp** (`gro--danh-sach-chi-tiet` của tid, không có thì khung + nét của khung, bỏ trùng); C# trả `(((vi-tri tid dx dy) ...) con-chong)`; chế độ `cs` dời entity (tổng độ dời, một `TransformBy` mỗi entity). Lisp nhánh `cs` chỉ chạy bước 4–5 (cập nhật `gro--danh-sach-chi-tiet` + `cur-khungs`) theo chuỗi lần dời; báo cáo `Tach va cham khung in: da doi N luot cho M tam don` giữ nguyên.
   - Công tắc **`tachgian`** (18 bước), hàm lisp `gro_net_tach_gian`, cầu nối `gro--tach-gian-goi-cs`, so A/B `gro--tach-gian-ab` (bản lisp ghi thêm chuỗi lần dời `ds-doi`; bản ghi `CHONG` + `D<i>` tid / dx / dy, dung sai 1e-6).
   - Test **542/542** (6 ca mới). Build 0 lỗi, DLL đã chép `C:\GROZWCAD` (28/10 17:43). Lisp: ngoặc cân bằng, +2 hàm, CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc180._ls`. Registry `cs_tachgian = ab`.

   - **So A/B (28/10):** `[AB tachgian] khop 10 muc` ngay lần đầu — 68 khung, 9 lượt dời, còn chồng 0, C# **3 ms**; báo cáo `da doi 9 luot cho 9 tam don` không đổi. Đã đặt `cs_tachgian = cs`. **Chạy `cs` (28/10):** `(.NET): 68 khung, 9 luot doi, con chong 0, 4 ms (da doi 203 doi tuong, 0 loi)`; dòng `tach va cham khung in tam don` 0.53 s → **0.16 s** (còn lại là lisp gom ename từng khung trong `gro--tach-gian-goi-cs`). Báo cáo không đổi.

   > **Nghiệm thu (bản đầu — đã chạy A/B):** đóng ZWCAD → `build.ps1` → chạy `gro`: `[GRO] Tach va cham khung in (.NET): 66 khung, 9 luot doi ... (chi tinh de so A/B)` + `[AB tachgian] khop 10 muc`; khớp thì đặt `cs`, kiểm bằng mắt 9 tấm đơn được tách như trước.

181. **GĐ 5d-a: chia nét cắt / kegaki vào danh sách chi tiết sang C#** (28/10/2026 — phần 2 lộ trình mục 179, nửa đầu).

   - `danh sach chi tiet` (`gro--tao-danh-sach-chi-tiet`) 0.39 s. Bước 2 quét ~1060 nét `0-KAKOU` / `3-KEGAKI` / `TAPU_TMT` (`entget` + XData + `gro--bbox` COM từng nét); bước 4 chia từng nét về tấm, mỗi lần chia **dựng lại cả danh sách 66 tấm** bằng `mapcar`.
   - Lisp: bước 2 (vòng quét) + bước 4 chuyển **nguyên văn** sang `gro--ds-ct-chia-net-lisp (ss-kakou-keg ds-ct)` và được gọi **sau** bước 3 (bước 3 không dùng kết quả bước 2 — dời chỗ không đổi kết quả). Cầu nối `gro--ds-ct-chia-net` (công tắc **`dsct`**, 19 bước), so A/B `gro--ds-ct-chia-net-ab` (mỗi tấm: tid, số nét, handle pt9 đúng thứ tự — so tuyệt đối). Bước 1 / 3 / 5 (nét chưa đổi, 3 cái) giữ ở lisp.
   - `Core/ChiaNetCat.cs` chép: tâm nét trong khung in nới 3 của đúng 1 tấm → tấm đó; nhiều tấm (duyệt **ngược** — cons) → vùng nét cắt gần nhất (trong nới 2 = 0, `<` giữ cái gặp trước); không tấm nào → vùng nét cắt gần nhất nếu ≤ 30; gán cho **mọi** mục cùng tid; nét duyệt **ngược ss** (kakou-keg-items dựng bằng cons). pt9 cuối = nét gán thêm (theo thứ tự ss) + pt9 cũ, bỏ trùng giữ lần đầu; không có nét nào thì pt9 giữ nguyên (lisp bỏ cả bước khử trùng).
   - `CadServices/ChiaNetCatCad.cs`: chỉ đọc; loại không gian giấy / XData `GRO_CHUP_COPY` / layer `*_CHUP`; hộp bao `GeometricExtents`, không có thì ±5 quanh group 10 (LINE đầu, CIRCLE / ARC / ELLIPSE tâm, INSERT điểm chèn). Hàm lisp `gro_net_ds_chi_tiet`, in `[GRO] Danh sach chi tiet (.NET): chia N net ... ms`.
   - **Chỗ có thể lệch:** hộp bao `GeometricExtents` so với `vla-GetBoundingBox` cho INSERT / HATCH / SPLINE — tâm nét chỉ cần rơi đúng khung (nới 3), so A/B sẽ cho biết.
   - Test **546/546** (4 ca mới). Build 0 lỗi, DLL đã chép `C:\GROZWCAD` (28/10 17:59). Lisp: ngoặc cân bằng, +3 hàm, CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc181._ls`. Registry `cs_dsct = ab`.

   - **So A/B (28/10):** `[AB dsct] khop 66 muc` ngay lần đầu — chia 1057 nét cho 66 tấm, C# **7 ms**; hộp bao `GeometricExtents` không gây lệch. Đã đặt `cs_dsct = cs`. **Chạy `cs` (28/10):** `danh sach chi tiet` 0.42 s → **0.14 s**; tổng GRO **6.6 s** (1.73 s chọn đối tượng — máy chạy ~4.9 s; lúc đầu GĐ 0: 29.9 s tổng).

   > **Nghiệm thu (bản đầu — đã chạy A/B):** đóng ZWCAD → `build.ps1` → chạy `gro`: `[GRO] Danh sach chi tiet (.NET): chia ~1060 net ... cho 66 tam` + `[AB dsct] khop 66 muc`; khớp thì đặt `cs`, dòng `danh sach chi tiet` phải tụt từ ~0.39 s. Đóng khung + vẽ Khung_IN (0.31 s) là nửa sau phần 2 — nằm trong `gro--tao-khung-dong-thuan`, làm lượt sau.

183. **GĐ 5d-b: đóng khung + vẽ Khung_IN sang C#** (29/10/2026 — phần 2 lộ trình mục 179, nửa sau).

   - `khung: dong khung + ve Khung_IN` 0.33–0.39 s: hộp cụm bango / mã khắc / F-name qua COM (`gro--bbox` từng entity), lọc Dim chạm cụm của tấm khác (vòng n²), co lề theo khe tới mọi tấm khác (vòng n²), rồi `gro--ve-khung-rect` **mỗi khung** một lần ActiveX (layer + `addlightweightpolyline` + `vla-update`).
   - **Lisp** vẫn dựng `s-list` / `dims-cua-tam` từng tấm như cũ (`gro--dong-khung-goi-cs` chép đúng đoạn đó) rồi gửi `(tid t-bb (hộp-hình...) ((hộp-dim chắc) ...) (ename-khối...) có-khối)`.
   - **`Core/DongKhung.cs`**: bỏ Dim **chưa gán chắc** chạm (mép tính là chạm) `t-bb ∪ hộp cụm bango` của tấm khác; hộp = hình ∪ Dim còn lại ∪ `t-bb` ∪ hộp cụm bango của chính tấm (chỉ tấm có mục trong `cum-khois-map`, mục 172); lề 15 co theo khe 0 < khe < 35 → `max(2, khe/2 − 2)`, X / Y riêng.
   - **`CadServices/DongKhungCad.cs`**: hộp cụm bango = gộp `GeometricExtents` các entity (lisp `vla-GetBoundingBox`); chế độ `cs` vẽ cả 82 khung trong **một** transaction (LWPOLYLINE kín 4 đỉnh, layer `Khung_IN` màu 4 — tạo layer nếu thiếu, bỏ đóng băng / bật / mở khoá như `gro--ve-khung-rect`). Trả `((bb-final ename-khung (cờ-giữ-Dim 1/0...)) ...)`; lisp dựng `new-khungs` / `dim-es` / dòng `[Khung n]` như cũ (`gro--loc-theo-co`).
   - Công tắc **`dongkhung`** (20 bước), hàm lisp `gro_net_dong_khung`, so A/B `gro--dong-khung-ab` (mỗi tấm: tid, bb-final dung sai 1e-6, số Dim giữ). Ở `cs` bỏ luôn vòng tính `bb-khoi-tid` (COM) của lisp.
   - **Chỗ có thể lệch:** hộp cụm bango (TEXT bango, MTEXT mã khắc, TEXT F-name) `GeometricExtents` so với `vla-GetBoundingBox` — vào thẳng bb-final và khe co lề. Lệch thì đổi sang gửi hộp lisp tính sẵn.
   - Test **550/550** (4 ca mới). Build thử 0 lỗi — **chưa chép DLL** (ZWCAD đang mở). Lisp: ngoặc cân bằng, +3 hàm, đoạn gốc giữ nguyên trong nhánh `lisp`, CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc183._ls`. Registry `cs_dongkhung = ab`.

   > **Chưa chạy trong ZWCAD.** Nghiệm thu: đóng ZWCAD → `build.ps1` → chạy `gro`: `[GRO] Dong khung (.NET): N tam ... (chi tinh de so A/B)` + `[AB dongkhung] khop N muc`; khớp thì đặt `cs`, dòng `khung: dong khung + ve Khung_IN` phải tụt; nhìn Khung_IN như cũ.

   - **Chạy A/B (29/10, bản vẽ Drawing1, 66 ghi chú → 68 tấm):** DLL chép 08:32; `[GRO] Dong khung (.NET): 66 tam, 5 ms (chi tinh de so A/B)` + **`[AB dongkhung] khop 66 muc`** — hộp `GeometricExtents` của cụm bango cho cùng bb-final như `vla-GetBoundingBox` (dung sai 1e-6). Dòng thời gian ở `ab` 0.34 s (lisp vẫn chạy). Đã đặt `cs_dongkhung = cs`. **Chạy `cs` (29/10):** dòng `khung: dong khung + ve Khung_IN` 0.34 s → **0.24 s** (còn lại là lisp dựng s-list / Dim từng tấm trong `gro--dong-khung-goi-cs` + in 66 dòng `[Khung n]`); GRO 11.9 s, trong đó 5.9 s chọn đối tượng (người dùng thao tác).

184. **DỪNG chuyển đổi; GỠ bản lisp dự phòng của 10 bước đã chạy C#** (29/10/2026, người dùng: "dừng ở đây, không chuyển đổi nữa; loại bỏ các phần đã chuyển sang C# trong lisp và báo cáo số dòng lisp còn lại").

   ### Kết quả
   | | Trước (mục 183) | Nay |
   |---|---|---|
   | Dòng `Guro ZWCAD(gro).lsp` | 18 168 | **16 080** (−2 088) |
   | Form toplevel / `defun` | 424 / 415 | **397 / 388** |
   | Lệnh `c:` | 14 | 14 |

   ### Đã gỡ (29 hàm — gồm công tắc `gro--cs-che-do`, gỡ bằng bộ đọc lisp theo cấu trúc — không cắt chuỗi theo chỉ số, mục 53)
   - Bản lisp: `gro--phan-loai-net-lisp`, `gro--vong-kin-geo-lisp`, `gro--geom-match-linear-dims` (+ `gro--chuoi-thang-hang`, `gro--line-bbox-collide-p`, `gro--bbox-scale-15`, `gro--dist-pt-infinite-line`), `gro--quet-hinh-primitives`, `gro--dim-dai-lisp`, `gro--loc-dim-net-lisp`, `gro--ds-ct-chia-net-lisp`, `gro--bo-cuc-cot` (+ `gro--bb-dien-tich`), `gro--tg-chon-huong` (+ `gro--tg-can-dich`, `gro--tg-bb-no`, `gro--tg-bb-dich`).
   - Nhánh lisp nội tuyến: đóng khung (`gro--tao-khung-dong-thuan`: lọc Dim / hộp / lề / `gro--ve-khung-rect` từng khung, vòng `bb-khoi-tid`), xếp cụm (`gro--xep-cum-phai`: 134 dòng bố cục + dời), tách va chạm (`gro--tach-va-cham-khung-in`: 189 dòng vòng lặp).
   - So A/B: 10 hàm `gro--*-ab` + `gro--hdl`; công tắc `gro--cs-che-do`.
   - Hàm bị gỡ = hàm **trở thành không còn ai gọi** sau khi bỏ nhánh lisp (lặp tới khi hết); hàm vốn đã chết từ trước giữ nguyên. Không còn lời gọi tới hàm không định nghĩa (so với bản trước).

   ### Cầu nối nay chỉ gọi C#
   - Hàm chung mới `gro--cs-goi (buoc ham args n)`: kiểm DLL có hàm, gọi `(ham "cs" . args)` bằng `vl-catch-all-apply`, kiểm lỗi / `nil` / số phần tử `n`; sai → **`gro--cs-dung`**: in + `alert` rồi `(exit)` — **dừng lệnh**, không còn chạy bản lisp. Không có đường "hỏng im lặng".
   - Hành vi khi C# chạy tốt **giữ đúng chế độ `cs`** đã nghiệm thu: chỉ xoá các nhánh mà chế độ `cs` không đi qua; nhánh `cs` giữ nguyên chữ.
   - Đầu vào rỗng giữ như bản lisp mà không gọi C#: `gro--dim-dai` / `gro--dim-dai-3b` (`texts-lin` rỗng → `nil`), `gro--vong-kin-geo` (`ss` rỗng → `(nil lines-cont arcs-cont)`), `gro--ds-ct-chia-net` / `gro--dong-khung-goi-cs` (danh sách tấm rỗng).
   - Lệnh `GROCS` (C#) vẫn còn nhưng **không còn tác dụng** với 10 bước này — lisp luôn gửi `"cs"`.

   ### Rủi ro mới cần biết
   - **GRO nay bắt buộc có `GROZWCAD.dll` đúng bản.** Thiếu DLL / DLL cũ thiếu hàm → GRO dừng ở bước đầu tiên cần C# (`*** [GRO .NET] Buoc nhannet (C#) loi: GROZWCAD.dll chua co ham ...`). Bản lisp đầy đủ để khôi phục: `backup/Guro ZWCAD(gro).lsp.truoc-muc184._ls`.
   - Các bước **chưa** chuyển (đọc text, ghép số lượng, sắp xếp, đặt bango, ghép hình, gán Dim, CSV, PDF…) vẫn là lisp — lộ trình mục 179 dừng ở phần 2.

   - Lisp: ngoặc cân bằng, không `defun` trùng, CRLF, số dòng có ký tự không phải ASCII 60 → 51 (chỉ giảm). Test C# không đổi (550/550, C# không sửa). **Chưa chạy trong ZWCAD.**

   > **ĐÃ NGHIỆM THU (29/10, Drawing1):** chạy `gro` sau khi gỡ — 66 ghi chú → 68 tấm, 66 Khung_IN (`Dong khung (.NET): 66 tam, 11 ms (da ve Khung_IN)`), xếp cụm 5 cụm, tách va chạm 9 lượt, mã khắc 65/65, CSV 68 dòng, PDF in được; **không** có dòng `*** [GRO .NET] ... loi`. Kết quả trùng lần `cs` mục 183; tổng 8.8 s.
   > **Nghiệm thu:** mở ZWCAD (DLL đã nạp sẵn) → chạy `gro` trên bản vẽ cũ: kết quả, báo cáo và bảng thời gian phải như lần chạy `cs` gần nhất (mục 183); không có dòng `*** [GRO .NET] ... loi`.
