# Mục lục rules — Guro (lệnh GRO)

Mục lục các file rule của mục con [03_Guro](../README.md): lisp `lisp/guro_gro.lsp` + C# `plugin/GroTool.Plugin`, chạy ZWCAD 2026 và AutoCAD 2027. Đọc bảng **Đọc gì trước** rồi chỉ mở file rule ứng với việc đang làm.

## Đọc gì trước

| Tình huống | Đọc file nào |
|---|---|
| Lần đầu động vào thư mục này | [01-tong-quan.md](01-tong-quan.md) |
| Không nhớ lệnh nào làm gì | [02-bang-lenh.md](02-bang-lenh.md) |
| Sắp viết một hàm mới | [03-ban-do-ham.md](03-ban-do-ham.md) — **hầu hết việc cần làm đã có hàm sẵn** |
| Sắp sửa layer / chiều cao / vị trí / CSV | [04-du-lieu-va-layer.md](04-du-lieu-va-layer.md) |
| **Sắp chuyển một phần lisp sang C#** | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) — đọc luật + bảng trạng thái TRƯỚC |
| Gặp kết quả lạ, chưa rõ nguyên nhân | [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md) rồi [15-phuong-phap-debug.md](15-phuong-phap-debug.md) |

## Các file rule

| File | Nội dung | Mục lỗi |
|---|---|---|
| [01-tong-quan.md](01-tong-quan.md) | Tổng quan, file lisp, quy ước bảo trì | — |
| [02-bang-lenh.md](02-bang-lenh.md) | Bảng lệnh — lệnh nào làm gì | — |
| [03-ban-do-ham.md](03-ban-do-ham.md) | Bản đồ hàm — tra trước khi viết hàm mới | — |
| [04-du-lieu-va-layer.md](04-du-lieu-va-layer.md) | Layer, chiều cao, vị trí, định dạng CSV, **nhận diện nét Dim** | 169 |
| 170 | Bước 6g **đặt mã khắc + F-name vào trong hình chạy bằng C#** (`gro_net_dat_mk`, `Core/DatMaKhac.cs`); chỉ còn cách trượt từ góc trên-trái (chế độ 2), **bỏ chế độ 1** và ô chọn chế độ trên hộp thoại; lisp chỉ chọn tấm | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 171 | **GĐ 0 chuyển lisp → C#**: đo thời gian từng bước `c:gro` (`gro--moc`, bảng cuối lệnh), công tắc `lisp/ab/cs` + lệnh `GROCS`, phiên `GroPhien`, so A/B `GRO_NET_AB`, dump / phát lại tổng quát | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 172 | GĐ 5: đo con (quét hình 2 lần ~4.2 s, thu thập Dim 3.2 s, đóng khung 3.2 s); sửa nhanh lisp đóng khung (hộp cụm bango tính 1 lần); **GĐ 5a quét + gom hình sang C#** (`Core/QuetHinh.cs`, union-find, công tắc `quethinh`) | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 173 | **GĐ 3**: đo con bước xử lý nét (phân loại nét 6.4 s, khớp Dim dài 2.1 s, vòng kín 0.9 s); **GĐ 3a phân loại nét sang C#** (`Core/PhanLoaiNet.cs`, lisp gửi chính selection set để giữ thứ tự, công tắc `nhannet`) | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 174 | **GĐ 5c-a**: khớp Dim dài trong thu thập Dim sang C# (`Core/DimDai.cs`, công tắc `dimdai`); lisp gửi **list số thực thật** (`CadServices/LispCay.cs`), không qua chuỗi; thêm mốc đo con thu thập Dim | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 175 | **GĐ 3b**: khớp Dim dài ở bước xử lý nét (`gro--geom-match-linear-dims`) sang C# (`Core/DimDai3b.cs`, công tắc `dimdai3b`); giữ đúng danh sách tích luỹ dùng chung mọi chữ số; lisp vẫn tự in thông báo mục 169 | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 176 | **GĐ 5c-b**: vòng lọc đầu của thu thập Dim sang C# (`Core/LocDimNet.cs`, công tắc `locdim`); chỉ xét LINE / ARC / DIMENSION, bỏ 2 phép thử chắc chắn sai với 3 loại đó | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 177 | **GĐ 3c**: dò vòng kín biên dạng khi nhận Dim sang C# (`Core/VongKin.cs`, công tắc `vongkin`); giữ đúng thứ tự đảo mỗi lượt tỉa; khoá đầu mút làm tròn như `rtos 2 1` | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 178 | **GĐ 6a**: xếp cụm sang cột phải — bố cục, dời cụm, kiểm còn sót sang C# (`Core/XepCum.cs`, công tắc `xepcum`); dời được mô phỏng bằng bảng độ dời rồi áp một lần; lisp giữ dữ liệu tấm + nhãn nhóm | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 179 | DLL đổi tên **`GROZWCAD.dll`** (thư mục `C:\GROZWCAD\`), **lisp là nơi duy nhất nạp DLL** (gỡ khoá tự nạp `Applications\ZwCadPlugin` của ZWCAD, sao lưu `.reg`); lộ trình chuyển nốt toàn bộ GRO sang C#, đích lisp chỉ còn trình nạp | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 180 | **GĐ 6f**: tách va chạm khung in sang C# (`Core/TachVaCham.cs`, công tắc `tachgian`); C# trả chuỗi lần dời + dời entity, lisp chỉ cập nhật dữ liệu | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 181 | **GĐ 5d-a**: chia nét cắt / kegaki vào danh sách chi tiết sang C# (`Core/ChiaNetCat.cs`, công tắc `dsct`); bước quét + chia tách thành hàm, gọi sau bước 3 | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 182 | Thứ tự **khung bao / vùng** không theo trái → phải (vùng trái bị đánh sau): `gro--sap-vung` dùng hmax giả 1.0 → mỗi khung lệch đỉnh vài mm thành một hàng. Nay gom hàng theo ½ chiều cao khung nhỏ nhất, trong hàng trái → phải | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 183 | **GĐ 5d-b**: đóng khung + vẽ Khung_IN sang C# (`Core/DongKhung.cs`, công tắc `dongkhung`); C# vẽ cả loạt khung trong một transaction | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 184 | **Dừng chuyển đổi; gỡ bản lisp dự phòng** của 10 bước đã chạy C# (29 hàm, 18 168 → 16 080 dòng); cầu nối chỉ gọi C# qua `gro--cs-goi`, C# lỗi → dừng lệnh (`gro--cs-dung`) | [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) |
| 185 | In PDF tự động không chạy trên **máy khác**: thiếu `gro.ctb` (chỉ có ở Printstyle máy này) → `-PLOT` lệch câu trả lời mà vẫn báo "Da in". Nay tìm ctb cạnh `.lsp` / `GROZWCAD.dll`, dự phòng `Monochrome.ctb`, chỉ báo khi có file PDF; `gro.ctb` đặt sẵn ở `C:\GROZWCAD\` | [13-lenh-cu-fn-makhac.md](13-lenh-cu-fn-makhac.md) |
| 186 | **Tấm gộp không được cắt** khi text tấm sau nằm khác hàng (trên gân) và vạch chia chỉ cao bằng dải (< 50 % hình) → tấm sau thành cô đơn. Nay nhận **vạch chia nối** (2 đầu nối nét ngang sang cả 2 phía, `gro--tim-vach-chia-noi`), ưu tiên khi chọn chỗ cắt; in chẩn đoán khi không cắt. **186b**: kiểm trước khi cắt (vạch chia thật — bỏ cắt ở trung điểm, cùng `t=`, đoạn ≥ 200, text gần đoạn của nó ≤ 800); vòng dò dùng `assoc` (45 s → nhanh). **186c**: bỏ điều kiện rộng ≥ 1200; lệch hàng cho phép cộng thêm phần text chủ chồng vào chiều cao hình | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 187 | Đặt mã khắc vào hình (C#): lề 0.1 mm tới mép chỉ khi **cả hai** cạnh tấm < 50 mm (trước: một cạnh < 100) — `DatMaKhac.LeMep` | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 188 | Khắc chữ (mục 156): chữ trong hình ở **layer khác** (không phải 1-SUNPOU / layer gốc) không được đổi sang 3-KEGAKI. Nay quét TEXT/MTEXT mọi layer, loại đầu ra GRO / bản chụp / bango / ghi chú + dòng số lượng | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 189 | Khắc chữ: chữ **thò ra ngoài mép hình** không được đổi. Nay nhận khi tâm chữ trong hình; chữ thò ra ngoài hoặc chạm nét cắt 0-KAKOU → vẫn đổi + **cảnh báo** `!!! Khac chu ...` | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 190 | Khắc chữ: cảnh báo hiện **tiếng Việt có dấu** ở tab Thông Tin (`ThongTinGro.Dich`) + cờ `KC_NGOAI_HINH` / `KC_CHAM_NET` trong XData bango → **cột Ghi chú** bảng DST (`gro--gan-co-xdata`); bước khắc chữ 9 s → hộp bao mỗi chữ tính 1 lần | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 191 | **Chuyển vào OHTA_TOOL/03_Guro + chạy AutoCAD 2027**: lisp → `lisp/guro_gro.lsp` (100% ASCII, chuỗi Nhật dùng `\U+XXXX`), nạp qua lệnh mồi `lisp/autoload.lsp`; DLL đổi tên **`GROTOOL.dll`** ở `03_Guro\bin\<ZwCAD\|AutoCAD>` (`gro--tim-dll`, ZWCAD còn tìm `C:\GROZWCAD` cũ); C# build kép net48/net10 bằng `#if ZWCAD`; in PDF `gro--may-in-pdf` / `gro--thu-muc-ctb` theo nền tảng | [../README.md](../README.md) |
| 192 | **GRO không hiện hộp thoại nữa** — thiết lập nằm trên **tab Guro** của bảng OHTA (giao diện trong GROTOOL.dll, ADR 0003), đổi là ghi registry ngay; `c:gro` đọc lại registry mỗi lần chạy, chọn layer bằng `gro--tham-so-chay` (layer đã lưu mà bản vẽ không có → layer đứng đầu; layer gốc có mục **Tự động**, khóa `xln_tu_dong`). Hộp thoại cũ: lệnh **`GROCD`** | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 193 | **Gõ `GRO` chỉ mở tab Guro** trên bảng OHTA (`ohta_net_tab "guro"`), chưa đánh số; nút **▶ Chạy GRO** gửi **`GROCHAY`** (thân lệnh GRO cũ đổi tên `c:grochay`). Không mở được tab → chạy `GROCD` | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) | Hộp thoại DCL + **bản .NET** (dialog `gro`, bảng DST), **tab Guro** | 18, 19, 20, 52, 55, 56, 64, 86, 192, 193 |
| [06-encoding-chuoi.md](06-encoding-chuoi.md) | Encoding và xử lý chuỗi (CP932 / Unicode) | 2, 9, 15, 16, 50, 57 |
| [07-tach-du-lieu.md](07-tach-du-lieu.md) | Tách độ dày, số lượng, mã khắc, **ghép dòng số lượng** | 10, 11, 17, 23, 24, 32, 43, 54, 168 |
| [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) | Sắp xếp, gom hàng, chế độ vùng polyline | 22, 25, 28, 49, 51, 53, 58, 61, 62, 63, 65, 182, 186 |
| [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) | Vị trí và hình thức đầu ra trên bản vẽ, **đặt mã khắc vào hình (C#)** | 26, 33, 34, 35, 37, 38, 39, 40, 41, 42, 44, 45, 46, 47, 59, 60, 170, 187, 188, 189, 190 |
| [10-csv.md](10-csv.md) | Ghi file CSV | 7, 8, 27, 30, 31 |
| [11-api-zwcad.md](11-api-zwcad.md) | Hàm/API ZWCAD khác AutoCAD | 1, 3, 4, 13 |
| [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) | Chẩn đoán và báo lỗi | 5, 6, 12, 14, 21, 29, 36, 48, 85 |
| [13-lenh-cu-fn-makhac.md](13-lenh-cu-fn-makhac.md) | Lệnh cũ: FN, ma_khac, **in nhanh PDF** | 66 |
| [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md) | Cần kiểm tra lại — chưa xác minh | — |
| [15-phuong-phap-debug.md](15-phuong-phap-debug.md) | Phương pháp debug đã dùng | — |
| [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) | **Lộ trình chuyển lisp → C#**: luật, công tắc `GROCS` lisp/ab/cs, bảng trạng thái từng giai đoạn | 171–181, 183, 184 |

## Mục lục mục lỗi

1–88 chi tiết tại đây, 89–167 ở bảng chung, 168–193 tại đây.

> ### ⚠ SỐ MỤC LÀ CỐ ĐỊNH, KHÔNG ĐƯỢC ĐÁNH SỐ LẠI
> Mã nguồn `.lsp` tham chiếu tới chúng ở **17 chỗ** (`CLAUDE.md muc 13`, `muc 29`, `muc 85`, `muc 86`, …) và các file rule tham chiếu chéo nhau ở **59 chỗ**. Đổi số là hỏng cả 59 chỗ đó cùng lúc.
>
> Thêm mục mới thì **nối tiếp số lớn nhất hiện có + 1**, bỏ vào file theo chủ đề, rồi cập nhật bảng dưới.
>
> **Mục 89–167 được ghi ở bảng chung** [`laptrinhlisp/rules/12-danh-muc-66-loi-va-bai-hoc.md`](../../../rules/12-danh-muc-66-loi-va-bai-hoc.md), không lặp lại ở đây. Bảng dưới chỉ liệt kê mục 1–88 và các mục mới có ghi chi tiết trong thư mục này. Số lớn nhất hiện có: **193**.

> ### Lệnh chẩn đoán cũ
> `grosort` / `grodbg` / `growp` / `grostyle` / `bgdbg` thuộc bản đầy đủ cũ `guro ZWCAD(BG MK FN).lsp`, không có trong `guro_gro.lsp`. Các mục bên dưới nhắc tới chúng là ghi chép lịch sử. Lịch sử thay đổi lisp nay nằm trong git của OHTA_TOOL.

Chi tiết đầy đủ quy trình port chung xem tại skill [`.claude/skills/zwcad-lisp-port/SKILL.md`](../../../.claude/skills/zwcad-lisp-port/SKILL.md).

| Mục | Nội dung | Ở file |
|---|---|---|
| 1 | Lỗi ngay lúc `load` trên máy mới | [11-api-zwcad.md](11-api-zwcad.md) |
| 2 | Tiếng Việt có dấu gõ trực tiếp | [06-encoding-chuoi.md](06-encoding-chuoi.md) |
| 3 | `CMDECHO` không bao giờ được khôi phục | [11-api-zwcad.md](11-api-zwcad.md) |
| 4 | DXF group `72`/`73` sai cho MTEXT | [11-api-zwcad.md](11-api-zwcad.md) |
| 5 | Không kiểm tra loại đối tượng được chọn | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 6 | `(nth nn nlst)` trong `makhac2` | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 7 | `(command " | [10-csv.md](10-csv.md) |
| 8 | Charset `"_autodetect"` phá hỏng DÒNG ĐẦU TIÊN của CSV mỗi lần chạy `b | [10-csv.md](10-csv.md) |
| 9 | Bộ khử mã định dạng MTEXT (`bg--mtext-strip`) | [06-encoding-chuoi.md](06-encoding-chuoi.md) |
| 10 | `(rtos (atof …))` không tham số → `T=` phụ thuộc `LUNITS`/`LUPREC` của | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 11 | Tách `bg--co-t-p` và `bg--fields` ra khỏi `c:bg` | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 12 | Lệnh chẩn đoán `bgdbg` | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 13 | Bộ lọc `ssget` dạng danh sách phẩy `(0 | [11-api-zwcad.md](11-api-zwcad.md) |
| 14 | `c:bg` giờ báo cáo tại sao bỏ qua đối tượng | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 15 | "Máy tôi chạy OK | [06-encoding-chuoi.md](06-encoding-chuoi.md) |
| 16 | Hệ quả kiến trúc: bỏ hẳn lớp làm việc theo byte | [06-encoding-chuoi.md](06-encoding-chuoi.md) |
| 17 | Hạn chế còn lại của thuật toán tách `Q=` (giữ nguyên từ bản gốc | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 18 | `start_list` PHẢI truyền tham số thao tác `3` — hộp thoại `gro` không | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 19 | Ghi file DCL theo từng dòng | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 20 | Nhánh `new_dialog` trả `nil` không được im lặng | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 21 | Lệnh chẩn đoán `grodbg` | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 22 | Hàm so sánh KHÔNG BẮC CẦU truyền cho `vl-sort` — lỗi thuật toán | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 23 | Tách dữ liệu kiểu DANH SÁCH ĐEN là sai hướng | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 24 | `t=` nhận diện TẤM; số lượng chỉ quyết định `Q` và việc tách tấm | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 25 | Chế độ đánh số THEO VÙNG polyline | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 26 | F-name của `gro`: đổi từ MTEXT sang TEXT từng dòng | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 27 | Báo lỗi ghi CSV: dùng `alert` | [10-csv.md](10-csv.md) |
| 28 | Chọn ngưỡng cắt hàng: phải đi từ ngưỡng NHỎ NHẤT lên | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 29 | `gro` ĐỌC LẠI CHÍNH ĐẦU RA CỦA NÓ — lỗi nặng nhất từ trước tới nay | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 30 | `gro` chuyển từ NỐI TIẾP sang GHI ĐÈ file CSV | [10-csv.md](10-csv.md) |
| 31 | Bỏ ô "Số bắt đầu" khỏi hộp thoại `gro` | [10-csv.md](10-csv.md) |
| 32 | Mã khắc thiếu đoạn số của bản vẽ | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 33 | Đặt vị trí bango thông minh — tự tránh hình và chữ của khách | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 34 | Cho hộp bango trượt quanh chu vi cụm ghi chú (thay 4 vị trí cố định) | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 35 | Tấm thứ 2 trở đi xếp CHỒNG lên tấm trước (cặp 本図/逆図) | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 36 | Lệnh chẩn đoán `grovc` — 86% bango "chưa sạch", đo vật cản vô hình | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 37 | Mã khắc / F-name không còn tính là vật cản | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 38 | Ghi chú của CHÍNH tấm bị tính là vật cản (ZWCAD chọn TEXT rộng hơn hộp bao) | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 39 | Ghi chú tách nhiều tấm: bango xếp CỘT DỌC BÊN TRÁI, ngang hàng 本図/逆図 | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 40 | Cặp 本図/逆図 vẫn bị tách — đặt CẢ CỤM bango như một khối, hở `h*0.2` | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 41 | `ssget "_C"` chọn theo độ phân giải màn hình — lọc lại bằng hình học chính xác | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 42 | Ghi chú 1 tấm: ưu tiên TRÊN, CĂN GIỮA cụm chữ | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 43 | Tấm CPL → vật liệu ムキ 縞板 / Z=SS400-SIMA; sửa `bg--codes-str` hỏng chữ Nhật trên máy kiểu A | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 44 | Ghi chú 1 tấm: trên-giữa bị chặn thì ưu tiên BÊN TRÁI | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 45 | Ghi chú 1 tấm: thứ tự trên giữa → DƯỚI GIỮA → trái giữa → còn lại | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 46 | TEXT bango dùng kiểu chữ `guro`, chưa có thì tự tạo (Arial, Height 0) | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 47 | Mục 46 không ăn: tin lời khai của lệnh tạo thay vì đọc lại bản vẽ | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 48 | **ĐANG MỞ** — `stringp: T` chưa rõ nguyên nhân; đã thêm mốc bước `gro--buoc` | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 49 | Chế độ vùng: gộp nhầm hai hàng → tấm hàng dưới đánh số trước; chặn trần ngưỡng | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 50 | Thiếu `㎜` trong `bg--uni-tbl` → chuỗi in ra thành `t=4.5?` | [06-encoding-chuoi.md](06-encoding-chuoi.md) |
| 51 | Chế độ vùng: quét chọn theo TỪNG KHUNG (polyline kín 4 đỉnh), bỏ ray-casting | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 52 | Popup "Layer khung bao": chỉ liệt kê layer thật sự có khung, kèm số đếm | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 53 | Tách bạch KHUNG BAO (phạm vi) và VÙNG (thứ tự) — hai ô tích riêng | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 54 | Tự nhận diện mã khắc từ dòng tên khách có `さん` | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 55 | Khung bao thành mặc định: bỏ ô tích, chỉ còn popup layer | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 56 | Bỏ popup "Mã khắc" — mã lấy hoàn toàn từ tự nhận diện | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 57 | Nhận diện mã khắc hỏng trên máy thật: thiếu ký tự trong `bg--uni-tbl` | [06-encoding-chuoi.md](06-encoding-chuoi.md) |
| 58 | Bỏ `ssget "_C"` ở khâu quét nội dung — nó chỉ thấy phần đang hiện trên màn hình | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 59 | `gro` tự ZOOM thấy hết vùng trước khi đặt bango; sửa `vl-catch-all-apply` thiếu tham số | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 60 | Ghi TÊN BẢN VẼ vào từng khung bao, cỡ chữ gấp 1.5 lần bango | [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) |
| 61 | VÙNG nhận polyline > 4 đỉnh; khôi phục ray-casting cho hình L | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 62 | Có nên đổi sang `ssget "_CP"` không; lệnh `growp` để đo — **đã chốt ở mục 63** | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 63 | Đổi khâu quét sang `ssget "_CP"`; **dời bước zoom lên TRƯỚC khâu quét** | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 64 | Popup "Layer vùng" dùng nhầm luật 4 đỉnh của khung bao | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 65 | Vùng thò ra ngoài khung bao bị loại sạch; dọn 10 hàm rò biến `foreach` | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 66 | Gộp `innhanh(OI)` vào file guro: `c:oi` / `c:GI` in nhanh ra PDF | [13-lenh-cu-fn-makhac.md](13-lenh-cu-fn-makhac.md) |
| 67 | Tự động thêm hậu tố ngày tháng dạng `-YYYYMMDD` vào cuối tên PDF | [13-lenh-cu-fn-makhac.md](13-lenh-cu-fn-makhac.md) |
| 68 | Đổi từ khóa nhận diện mã khắc từ `さん` sang ký tự `入` (U+5165) | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 69 | Xử lý nét & chuyển layer nhanh (`xln`/`cln`/`xlnds`/tích hợp `gro`): Continuous→`0-KAKOU`, Center1→`3-KEGAKI`, Text→`1-SUNPOU`, các linetype khác giữ nguyên layer gốc | [04-du-lieu-va-layer.md](04-du-lieu-va-layer.md) |
| 70 | Hộp thoại `xln_dlg`: `sssetfirst` bật Grips/Pickset thay vì zoom; thoát vòng lặp `gro_dlg` khi chọn nhóm nét | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 71 | Lọc nét gióng kích thước (nét Dim, cung ARC đo góc) rời có linetype Continuous đưa về layer 1-SUNPOU (ByLayer) thay vì nhận nhầm thành nét cắt 0-KAKOU. Hỗ trợ cả 2 chế độ Tự động (phân tích số đo) và Thủ công (gọi bảng màu mặc định CAD acad_colordlg hoặc Pick nét mẫu) | [04-du-lieu-va-layer.md](04-du-lieu-va-layer.md) |
| 72 | Lỗi cú pháp DCL do thiếu escape ngoặc kép đóng `\";` trong LISP sinh file DCL động (`write-dcl`) | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 73 | Lỗi `undefined function - BG--MTEXT-STRIP` do thiếu định nghĩa hàm bóc tách chuỗi MTEXT | [03-ban-do-ham.md](03-ban-do-ham.md) |
| 74 | Lưu trạng thái các lựa chọn trong hộp thoại DCL (layer khung, vùng, in PDF, xử lý nét...) khi tạm đóng để Pick nét mẫu hoặc Chọn màu CAD | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 75 | Popup "Layer gốc bản vẽ" (`layer_xln`): Sắp xếp theo số đối tượng giảm dần và ưu tiên tự động chọn Layer có nhiều đối tượng nhất | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 76 | Nhận diện nhầm màu nét Dim tự động thành nét cắt: Bán kính 250mm quá rộng + đếm dồn làm nét trắng áp đảo | [04-du-lieu-va-layer.md](04-du-lieu-va-layer.md) |
| 77 | Bỏ sót số lượng cặp tấm thuận/nghịch (本図/逆図) trong bản vẽ nhiều tấm do chặn cứng dx, dy | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 78 | Nhận diện cụm chi tiết nhiều hình (1 text - nhiều hình) & phát hiện hình cô đơn: Kiến trúc Đối soát kép Dual-Validation | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 79 | Bẫy màu chủ đạo khi lọc nét Dim: Trừ điểm nặng (-1000) màu chứa hình kín / lỗ tròn, bảo vệ Polyline khép kín | [04-du-lieu-va-layer.md](04-du-lieu-va-layer.md) |
| 80 | Luật cứng Tấm đơn 1 hình duy nhất + Chiếu tia Line-of-Sight & Ràng buộc theo Vùng: Tấm đơn đóng băng 100%, hình ngoài vùng là hình cô đơn | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 81 | Bảng danh sách tấm chia cột (`GRODS`/`DS`) + Cảnh báo hình cô đơn trong thông báo chung của `GRO` | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 82 | Ghép nhầm dòng số lượng tấm nghịch (逆図) sang tấm đơn lân cận: Đo khoảng cách Tâm-đến-Tâm & Cụm Đa Điểm Xét (Multi-Center Cluster Chaining) | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 83 | Lỗi hố đen gom cụm hình học (Black-Hole Grouping) & Cơ chế Tự động tạo Khung bao (`use_tao_khung`): So khoảng cách với từng thực thể thành viên thay vì bbox nở to của nhóm; lọc khung viền bản vẽ khổ lớn; mở khóa layer Khung_IN | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 84 | Đo khoảng cách từ Trung Tâm Cụm Text đến Mép Chi Tiết (Center-to-Edge Matching): Triệt tiêu lỗi mép chữ dài vươn sang cướp hình của cột bên cạnh | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 85 | Rà lại sau đợt sửa 05/10: `gro` bản đầy đủ chết sau tấm đầu; 9 lỗi khác | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 86 | Hộp thoại `gro` + bảng DST sang .NET C# (`ZwCadPlugin`), tích hợp 2 Tab (DST & DSN): lisp gọi `gro_net_dialog`, DCL làm dự phòng; bảng 2 tab DST/DSN, lệnh DSN mở thẳng Tab nét, sửa 6 lỗi ghi hỏng mã khắc / `N=` | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 87 | Ghép số lượng hai chiều (Cả Trên Cả Dưới) & Cảnh báo cụm text trùng loại 本図 / 逆図 | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 88 | Reset và làm mới sạch sẽ danh sách nét chưa đổi (`gro--xln-danh-sach-chua-doi`) mỗi lượt chạy `GRO`/`XLN` | [04-du-lieu-va-layer.md](04-du-lieu-va-layer.md) |
| 89–167 | Xem bảng chung `../../rules/12-danh-muc-66-loi-va-bai-hoc.md` | — |
| 168 | Ghép dòng số lượng theo **bảng ứng viên**: dòng bị nhiều ghi chú tranh về ghi chú **gần hơn** (tâm–tâm), mỗi ghi chú tối đa **2** dòng, cặp `本図`+`逆図` xếp dọc đi chung không bị xé. Sửa lỗi ghi chú dài lấy mất dòng `本図` của tấm bên cạnh → bị tách 2 tấm / coi là cụm mirror, tấm kia mất số lượng | [07-tach-du-lieu.md](07-tach-du-lieu.md) |
| 169 | Dim kích thước có **đường dim chính bị tách thành nhiều LINE thẳng hàng nối tiếp** (vd `700.8` = 2 đoạn ~350 nối ở giữa) không được nhận là Dim → cả chữ U bị giữ làm nét cắt `0-KAKOU`. Nay ghép chuỗi LINE thẳng hàng (`gro--chuoi-thang-hang`) rồi so tổng chiều dài | [04-du-lieu-va-layer.md](04-du-lieu-va-layer.md) |

## Quy ước bảo trì

**Mỗi lần sửa lisp trong thư mục này phải cập nhật file rule tương ứng ngay trong cùng lần làm việc đó**, không để lại sau. Chi tiết xem [01-tong-quan.md](01-tong-quan.md).

Sửa `lisp/guro_gro.lsp` trực tiếp — đây là bản duy nhất. Xong chạy `build_ohta.ps1` (kiểm ASCII + cân bằng ngoặc + test).
