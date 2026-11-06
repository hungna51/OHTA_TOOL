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
| **Sắp sửa tách / cắt tấm gộp** (text thiếu hình, tấm ôm 2 hình) | [15-phuong-phap-debug.md](15-phuong-phap-debug.md) phần **Tách tấm gộp** — luật + bảng ca hồi quy, chạy `sim_tach_tam.py` |
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
| 194 | **Tab Guro gọn lại**: nút ⚙ (thay ↻) mở cửa sổ **Cài đặt GURO** (OK/Hủy): tự tạo khung tấm + **tên layer khung tấm** (`layer_khung_tam`, mặc định `Khung_IN`, dùng ở mọi chỗ lisp/C#), in PDF, xếp cụm, mã khắc, xử lý nét + tự nhận diện Dim (tắt = không xử lý Dim; bỏ chọn màu Dim thủ công). Mặt tab chỉ chọn **layer cho từng bản vẽ đang mở** (`gro_net_layer_ban_ve`; mặc định khung bao = layer **ít** khung nhất, vùng = **nhiều** khung nhất). Xoá hộp thoại cũ (.NET + DCL), `GROCD`, `GRODLGNET`, `GROPICKMAU`; không mở được tab thì `GRO` báo lỗi | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 195 | Cửa sổ ⚙ Cài đặt GURO chia **2 tab: Chung + Ghi chú**; nội dung "Cài đặt ghi chú" tách thành `NoteDictionaryPanel` dùng chung với hộp thoại `NoteDictionaryWindow` (bảng DST). OK lưu cả hai; tab Ghi chú chưa mở thì không lưu phần ghi chú | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 196 | Cài đặt GURO + Cài đặt ghi chú **theo kiểu bảng "Danh sách góc bẻ thử" của Fname**: dải tiêu đề `#323639`, thanh đáy xám (nút phụ trắng, nút chính xám đen), tab Chung có dòng gợi ý dưới mỗi tuỳ chọn; tab Ghi chú: ô Tìm, thanh Google Sheet thu gọn, bảng **chỉ xem**, bấm đúp để sửa/xóa (`NoteTuWindow`), ➕ Thêm từ + ⟲ Khôi phục ở thanh đáy | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 197 | **Lỗi OK của Cài đặt GURO**: "DialogResult can be set only after Window is created and shown as dialog" — cửa sổ mở bằng `Application.ShowModalWindow` (AutoCAD không mở kiểu `ShowDialog`) → đóng bằng `Close()` + cờ `DaLuu`, nút Hủy/Đóng có `Click` riêng (`IsCancel` không tự đóng). **Tự cập nhật từ điển ghi chú mỗi ngày** như Fname: lần mở tab Guro đầu tiên trong ngày tải Google Sheet ở luồng nền (`JapanNoteTranslator.NenTuCapNhat`, khoá `__LastSync`); bỏ ô "Tự tải khi mở" | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 198 | **Máy khác: "Khong tim thay 03_Guro\lisp\guro_gro.lsp"** khi chạy GROCHAY — OHTA_TOOL không nằm trong Support Path nên `*ohta-root-dir*` rỗng (OHTA_Loader lấy thư mục gốc từ registry để nạp `autoload.lsp` nhưng không ghi biến). Sửa 3 lớp: OHTA_Loader ghi `*ohta-root-dir*` khi lấy từ registry; `ohta-guro--thu-muc` dò thêm registry `RootDir`; nút tab Guro đặt sẵn `*gro-module-dir*` trước khi nạp | [../README.md](../README.md) |
| 199 | **GRO chạy trên bản vẽ mới chưa lưu (Drawing1) mà không báo** — CSV / PDF ghi vào thư mục tạm. `c:grochay` kiểm tra `DWGTITLED = 0` ngay đầu lệnh: alert "Bản vẽ chưa lưu" rồi dừng, chưa đụng vào bản vẽ | [../lisp/guro_gro.lsp](../lisp/guro_gro.lsp) |
| 200 | **ZWCAD máy khác: GROCHAY báo "invalid argument type: consp nil (dang o buoc: khoi dong)"** — chưa tái hiện được tại máy này. Gia cố `gro--tham-so-chay` (kết quả `gro_net_layer_ban_ve` phải là danh sách 3 phần tử, tên layer phải là chuỗi, `gro--khung-it-truoc` không `vl-sort` danh sách rỗng) và chia bước "khoi dong" thành bước con (mở thử CSV, đọc registry, nạp DLL, đếm khung, đếm layer gốc, layer tab, in tóm tắt) để lần lỗi sau biết đúng chỗ. **Chưa xác nhận đã hết lỗi** | [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md) |
| 201 | Mục 200 chỉ ra chỗ lỗi `consp nil` trên ZWCAD: bước "layer chon tren tab Guro" (gọi `gro_net_layer_ban_ve`). Sửa: gọi **có 1 tham số giả** `(list 0)` thay vì danh sách rỗng; C# trả **chuỗi rỗng** thay `nil` trong danh sách; kiểm kết quả bằng `length` thay `vl-list-length`. **Chưa xác nhận trên ZWCAD máy đó** | [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md) |
| 202 | `consp nil` vẫn còn ở bước "layer chon tren tab Guro" (cả máy phát triển, ZWCAD). Bọc **cả bước** vào `gro--layer-tab` gọi qua `vl-catch-all-apply`: hỏng thì GRO vẫn chạy với layer mặc định và in câu lỗi gốc `[GRO] Khong doc duoc layer chon tren tab Guro (...)`. Lưu ý thử: `guro_gro.lsp` chỉ nạp 1 lần mỗi bản vẽ — nạp lại `autoload.lsp` **không** thay code cũ; phải mở lại CAD hoặc `(load ".../guro_gro.lsp")` | [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md) |
| 203 | **Chữ Nhật thành "?"** trên ZWCAD kiểu byte ("co ky tu CP932 khong co trong bang bg--uni-tbl"): bảng lisp chỉ ~60 ký tự. Nay `bg--cp932-uni` ngoài bảng thì hỏi GROTOOL.dll `(gro_net_cp932 b1 b2)` (bảng mã 932 đầy đủ của .NET, `GroText.Cp932SangUnicode`), nhớ vào `bg--uni-them` để không hỏi lại và để `bg--uni-cp932` đổi ngược được | [06-encoding-chuoi.md](06-encoding-chuoi.md) |
| 204 | **Tab "Nét Chưa Đổi" của DST liệt kê bango / tên bản vẽ / khung bao**: tab đọc layer gốc từ registry `layer_xln`, từ mục 194 GRO không ghi nữa → tự chọn layer đông nhất (thường là "0"). Nay: ưu tiên layer chọn trên tab Guro cho bản vẽ, rồi `layer_xln` (lisp ghi lại layer gốc vừa xử lý); `LineScanner.QuetNetChuaDoi` bỏ qua đối tượng có XData `GRO_CHUP_COPY` / `GRO_METADATA` / `GRO_DAU_RA` — lisp gắn `GRO_DAU_RA` lên polyline khung bao đã chọn và chữ tên bản vẽ (`gro--gan-dau-ra`) | [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) |
| 205 | **Tab Thông Tin chỉ ghi "Có cảnh báo — xem chi tiết ở dòng lệnh"**: 3 nhóm cảnh báo bắt buộc (TH4 không đọc được số lượng, dòng số lượng mồ côi kèm toạ độ, chữ có t/T bị bỏ qua) nay đưa thẳng vào báo cáo thành khối `!!!` + chi tiết; bỏ câu chung. Chữ Nhật gửi dạng ASCII `\U+XXXX` (`gro--bao-cao-chu`), C# `ThongTinGro.GiaiEscape` đổi lại — không phụ thuộc bảng mã khi chuỗi đi lisp → .NET | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 206 | Nút trên tab Guro gửi `(setq *gro-module-dir* …)` + `(load ".../autoload.lsp")` **trước mỗi lệnh**, hiện đầy dòng lệnh. Nay chỉ gửi **1 lần mỗi bản vẽ** (cờ `GRO_DA_NAP_AUTOLOAD` trong `Document.UserData`) và gửi với `echoCommand = false` + `(princ)` nên không hiện; lệnh (GROCHAY / GI / DST) gửi riêng, vẫn hiện | [../README.md](../README.md) |
| 207 | **ZWCAD: "buoc nhannet (C#) loi: undefined function - nil"** — tên hàm `gro_net_*` có nhưng gọi không được. Nghi do bảng OHTA nạp GROTOOL.dll trước bằng `ExtensionLoader.Load` (mục 192) → `NETLOAD` của lisp sau đó bị ZWCAD bỏ qua, hàm lisp không đăng ký cho bản vẽ. Sửa: `DllTabLoader` nạp GROTOOL chỉ bằng `Assembly.LoadFrom` (`dangKyVoiCad: false`), đăng ký để `NETLOAD` của lisp làm; `gro--nap-net` kiểm hàm **gọi được thật** (`gro--net-san-sang-p` gọi thử `gro_net_cp932`), không được thì `NETLOAD` lại, vẫn không được thì báo mở lại ZWCAD. **Giả thuyết SAI — xem mục 208** (phần `DllTabLoader` đã gỡ, quay về `ExtensionLoader`) | [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md) |
| 208 | **Nguyên nhân thật của mục 207**: chẩn đoán trên ZWCAD (1 bản vẽ, NETLOAD lại vô ích) cho thấy MỌI hàm `gro_net_*` đều "undefined function - nil" dù `(type …)` = `SUBR`. Thủ phạm nghi là `gro_net_cp932` (mục 203) **trả số nguyên trần** (`object` boxed `int`) — mọi hàm khác trả chuỗi / T / nil / `ResultBuffer`; từ lần gọi đầu (GRO đọc chữ Nhật trước bước xử lý nét) cầu nối lisp ↔ .NET của ZWCAD hỏng cả phiên. Sửa: `gro_net_cp932` trả **danh sách 1 phần tử** `(ma)` (ListBegin + Int32 + ListEnd), lisp đọc `(car r)`; phép thử "gọi được" dùng `gro_net_cong_tac` (hàm cũ, trả chuỗi). **Luật: hàm `[LispFunction]` chỉ trả chuỗi, T/nil hoặc `ResultBuffer` — số đặt trong danh sách.** **Đã xác nhận trên ZWCAD 05/11/2026** (mở lại ZWCAD, GRO chạy được) — phát hành v1.2.3 / Guro 1.5.3 | [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md) |
| 209 | **Báo lỗi cho admin** (ADR 0007, Guro 1.6): `*error*` của `c:grochay` gặp lỗi thật (không phải Esc/Cancel) gọi `(vl-catch-all-apply 'ohta_net_bug_auto (list "GRO" "<loi> (buoc: <gro--buoc>)"))` — OHTATOOL.dll hỏi "Gửi báo lỗi?" (mỗi lỗi 1 lần/phiên). Đầu/cuối lệnh ghi `ohta_net_log` vào nhật ký `%LOCALAPPDATA%\OHTA_TOOL\logs`. Lệnh C# của GROTOOL (`DST`, `GROCS`, `GROSETTING`, `GROTHONGTIN`, `DSTON`) bọc `OhtaBugBridge.Run` → `OhtaTool.Plugin.Core.BugGuard.ReportException` bằng phản chiếu. **Chưa kiểm trên CAD.** | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 210 | **Tấm gộp không cắt khi ghi chú dài hơn đoạn ngắn** (`570 \| 240`, ghi chú 92 lấn qua vạch chia): phép kiểm 186b số 4 đòi `d1 < d2`, text vắt qua vạch cho `d1 = d2` → không cắt. **210b**: ngưỡng 1/4 vẫn chặn ca `390 \| 840` (ghi chú 555 mm) → bỏ ngưỡng, bằng nhau thì cắt (phía text theo tâm text); dòng `[TACH TAM CAD]` ghi thêm vào `ohta.log` (`gro--tt-in`). **Đã xác nhận trên CAD 05/11** (67 = 390, 68 = 840, 0 dòng `KHONG cat`). | [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) |
| 211 | **Máy mới: mọi `gro_net_*` "undefined function - nil" từ lần gọi đầu, NETLOAD lại vô ích** (báo lỗi DESKTOP-HFJESCO 06/11/2026, nhật ký mục 209). Cùng phiên: hàm OHTATOOL (nạp bằng `NETLOAD` của `OHTA_Loader`) gọi được, hàm GROTOOL (nạp bằng `ExtensionLoader.Load` khi bảng OHTA dựng tab Guro) thì không → nghi `ExtensionLoader` của ZWCAD đăng ký tên hàm nhưng không gọi được (giả thuyết mục 207 có thể đúng; mục 208 không giải thích được ca này). Sửa: `OHTA_Loader.lsp` `NETLOAD` GROTOOL ngay sau OHTATOOL; `DllTabLoader` ghi nhật ký khi phải dùng `ExtensionLoader`; nhật ký ghi mọi DLL nạp vào CAD. **Đã xác nhận trên DESKTOP-HFJESCO 06/11/2026** (v1.3.3: `GRO_NET_PHAN_LOAI_NET` ok, `gro_net_layer_ban_ve` gọi được) → nguyên nhân là `ExtensionLoader`, KHÔNG để bảng OHTA nạp GROTOOL trước `NETLOAD`. | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| 212 | **ZWCAD ĐƠ (Not Responding) giữa chừng GRO, máy dev chạy cùng bản vẽ thì được — do lisp GURO CŨ trong Startup Suite xung đột** (DESKTOP-HFJESCO 06/11/2026, `dq.dxf`). Mỗi lần đơ một chỗ khác (v1.3.2 qua được dò vòng kín, v1.3.3 đơ ngay tại đó), không lỗi, không "GROCHAY dung" trong nhật ký. Sửa: gỡ lisp GURO cũ khỏi `APPLOAD → Startup Suite`, mở lại ZWCAD. Cách chẩn đoán + dấu hiệu: xem [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) mục 212. **Đã xác nhận** (gỡ ra là chạy). | [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) |
| [05-hop-thoai-dcl.md](05-hop-thoai-dcl.md) | Hộp thoại DCL + **bản .NET** (dialog `gro`, bảng DST), **tab Guro** | 18, 19, 20, 52, 55, 56, 64, 86, 192, 193, 194, 195, 196, 197 |
| [06-encoding-chuoi.md](06-encoding-chuoi.md) | Encoding và xử lý chuỗi (CP932 / Unicode) | 2, 9, 15, 16, 50, 57 |
| [07-tach-du-lieu.md](07-tach-du-lieu.md) | Tách độ dày, số lượng, mã khắc, **ghép dòng số lượng** | 10, 11, 17, 23, 24, 32, 43, 54, 168 |
| [08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) | Sắp xếp, gom hàng, chế độ vùng polyline | 22, 25, 28, 49, 51, 53, 58, 61, 62, 63, 65, 182, 186, 210 |
| [09-vi-tri-dau-ra.md](09-vi-tri-dau-ra.md) | Vị trí và hình thức đầu ra trên bản vẽ, **đặt mã khắc vào hình (C#)** | 26, 33, 34, 35, 37, 38, 39, 40, 41, 42, 44, 45, 46, 47, 59, 60, 170, 187, 188, 189, 190 |
| [10-csv.md](10-csv.md) | Ghi file CSV | 7, 8, 27, 30, 31 |
| [11-api-zwcad.md](11-api-zwcad.md) | Hàm/API ZWCAD khác AutoCAD | 1, 3, 4, 13 |
| [12-chan-doan-bao-loi.md](12-chan-doan-bao-loi.md) | Chẩn đoán và báo lỗi | 5, 6, 12, 14, 21, 29, 36, 48, 85 |
| [13-lenh-cu-fn-makhac.md](13-lenh-cu-fn-makhac.md) | Lệnh cũ: FN, ma_khac, **in nhanh PDF** | 66 |
| [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md) | Cần kiểm tra lại — chưa xác minh | — |
| [15-phuong-phap-debug.md](15-phuong-phap-debug.md) | Phương pháp debug đã dùng | — |
| [16-chuyen-sang-csharp.md](16-chuyen-sang-csharp.md) | **Lộ trình chuyển lisp → C#**: luật, công tắc `GROCS` lisp/ab/cs, bảng trạng thái từng giai đoạn | 171–181, 183, 184 |

## Mục lục mục lỗi

1–88 chi tiết tại đây, 89–167 ở bảng chung, 168–212 tại đây.

> ### ⚠ SỐ MỤC LÀ CỐ ĐỊNH, KHÔNG ĐƯỢC ĐÁNH SỐ LẠI
> Mã nguồn `.lsp` tham chiếu tới chúng ở **17 chỗ** (`CLAUDE.md muc 13`, `muc 29`, `muc 85`, `muc 86`, …) và các file rule tham chiếu chéo nhau ở **59 chỗ**. Đổi số là hỏng cả 59 chỗ đó cùng lúc.
>
> Thêm mục mới thì **nối tiếp số lớn nhất hiện có + 1**, bỏ vào file theo chủ đề, rồi cập nhật bảng dưới.
>
> **Mục 89–167 được ghi ở bảng chung** [`laptrinhlisp/rules/12-danh-muc-66-loi-va-bai-hoc.md`](../../../rules/12-danh-muc-66-loi-va-bai-hoc.md), không lặp lại ở đây. Bảng dưới chỉ liệt kê mục 1–88 và các mục mới có ghi chi tiết trong thư mục này. Số lớn nhất hiện có: **212**.

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
