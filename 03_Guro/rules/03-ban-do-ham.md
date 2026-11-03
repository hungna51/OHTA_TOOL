# 03. Bản đồ hàm

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Bản đồ hàm

Tra ở đây trước khi viết hàm mới — hầu hết việc cần làm đã có hàm sẵn.

### Lớp đọc & chuẩn hoá chuỗi (dùng chung cho `bg`, `bgdbg`, `gro`, `FN`)

Toàn bộ lớp này làm việc trên **danh sách mã Unicode**, không phải chuỗi — lý do xem
mục 15/16 phần lịch sử lỗi (hai bản ZWCAD trả chuỗi về khác kiểu).

| Hàm | Dòng | Việc |
|---|---|---|
| `bg--ent-codes` | 294 | **Cửa vào chính**: entity TEXT/MTEXT → danh sách mã Unicode đã khử mã định dạng |
| `bg--codes` | 173 | chuỗi → danh sách mã Unicode; nhận cả 3 dạng (mã Unicode sẵn / byte CP932 / escape `\U+XXXX`) |
| `bg--codes-str` | ~240 | danh sách mã → chuỗi **đúng kiểu máy đang chạy** (đường ngược lại; sửa ở mục 43) |
| `bg--may-unicode-p` | ~225 | máy kiểu B (chuỗi mã ký tự) hay kiểu A (byte CP932)? Đo một lần, nhớ vào `bg--may-uni` |
| `bg--strip-codes` | 253 | khử mã định dạng MTEXT (`\P`, `\A1;`, `{}`…) trên danh sách mã |
| `bg--fields` | 544 | **danh sách mã → `(T= Q= M= V=)`** — toàn bộ logic tách dữ liệu ở đây |
| `bg--co-t-p` | 478 | bộ lọc: có chứa `t` ASCII hoặc `t` full-width không? |
| `bg--charset-nhan` | 468 | charset các ký tự "nhãn" đứng trước con số độ dày |
| `bg--trim` | 486 | bỏ các mã thuộc charset ở đầu danh sách |
| `bg--so-dau` | 494 | đọc số thực ở đầu danh sách mã |
| `bg--so-ascii` | 507 | số full-width `０-９` → số ASCII |
| `bg--chua` | 519 | danh sách con có xuất hiện liên tiếp không (thay cho `wcmatch "*…*"`) |
| `bg--uni-tbl`, `bg--cp932-uni`, `bg--uni-cp932`, `bg--sj-lead-p`, `bg--hex4`, `bg--drop`, `bg--co-ma-lon` | 65-173 | bảng tra & tiện ích nội bộ của `bg--codes` |

### Lớp tách dữ liệu của `gro` — TÌM DẤU MỐC (chỉ `gro` dùng)

**`gro` KHÔNG dùng `bg--fields`.** Hai đường tách dữ liệu song song, cố ý:

| | `bg` / `bgdbg` | `gro` |
|---|---|---|
| Cách làm | **cắt bỏ ký tự nhãn** ở đầu rồi đọc số | **tìm dấu mốc** `t=` / `枚`·`本` |
| Phụ thuộc danh sách ký tự | **có** (`bg--charset-nhan`) | **không** |
| Gặp chữ lạ ở đầu | `T=0`, `M=SPHC` — **hỏng im lặng** | vẫn đọc đúng |

| Hàm | Dòng | Việc |
|---|---|---|
| `gro--fields` | 1904 | danh sách mã → `(T= M= V=)`, `nil` nếu không có `t=`. **Không** trả `Q=` |
| `gro--co-cpl-p` / `gro--ghi-chu-cpl` / `gro--cpl-ds` | ~2045-2110 | tấm CPL: nhận chữ CPL; gán text CPL cho ghi chú gần nhất (khe ≤ `gro--cpl-gan` = h) (mục 43) |
| `gro--vl-cpl` / `gro--vl-cpl-z` | ~2040 | vật liệu CPL: `ムキ 縞板` (dựng từ mã Unicode) / `"SS400-SIMA"` |
| `gro--cum` | ~2075 | hộp cụm ghi chú (ghi chú + dòng số lượng) — **nguồn duy nhất** cho đặt bango và gán CPL |
| `gro--khe-bb` | ~2070 | khe giữa hai hộp bao = max(khe ngang, khe dọc) |
| `gro--tim-day` | 1826 | tìm `t=` rồi đọc số ngay sau — nhận **8 tổ hợp** nửa/toàn chiều rộng, cho phép dấu cách xen giữa |
| `gro--tim-sl` | 1866 | tìm **mọi** `<số>` ngay trước ký tự đếm `枚`/`本` → danh sách số |
| `gro--tim-nhan` | 1882 | tìm `<số>` sau dấu nhân `*` `＊` `×` (dạng `t=6㎜*3`) |
| `gro--so-luong` | 2172 | **quyết định số tấm + `Q`** theo 4 trường hợp → `(TH danh-sách)` |
| `gro--so-lui` | 1851 | đọc số từ danh sách mã **đảo ngược** (đi lùi từ ký tự đếm) |
| `gro--ma-chu-t` / `gro--ma-dau-bang` / `gro--ma-cach` / `gro--ma-dem` / `gro--ma-nhan` | 1793-1799 | **bảng mã — thêm biến thể chỉ phải thêm một mã**, không thêm nhánh `cond` |
| `gro--bo-cach`, `gro--chu-so-p`, `gro--codes-int` | 1802-1812 | tiện ích nội bộ |

### Lớp gom hàng & sắp xếp của `gro`

| Hàm | Dòng | Việc |
|---|---|---|
| `gro--sap-xep` | 2037 | **cửa vào chính** → `(danh-sách-(hàng . ghi-chú) ngưỡng cách-chọn số-hàng)` |
| `gro--nguong-hang` | 1969 | tìm ngưỡng cắt hàng **từ chính dữ liệu** (khe tự nhiên), không dùng hệ số cố định |
| `gro--nguong-toi-da` | ~2290 | trần cứng của ngưỡng cắt hàng = `hmax*5` (mục 49) |
| `gro--in-hang-vung` | ~3736 | in việc gom hàng bên trong từng vùng — chỉ vùng >1 hàng hoặc bị chặn trần (mục 49) |
| `gro--gan-hang` | 2018 | gán chỉ số hàng cho danh sách đã sắp Y giảm dần |
| `gro--ghep-sl` | ~2002 | `(notes cands hmax vungs)` — ghép dòng số lượng vào ghi chú theo **bảng ứng viên**: dòng bị tranh về ghi chú gần hơn (tâm–tâm), tối đa 2 dòng / ghi chú → `(bảng danh-sách-mồ-côi)` (mục 168) |
| `gro--don-vi-sl` | ~1983 | cụm cột → đơn vị ứng viên: đúng 2 dòng HONZU+GYAKUZU = 1 đơn vị CẶP, còn lại tách lẻ (mục 168) |
| `gro--sl-toi-da` | ~1980 | số dòng số lượng tối đa một ghi chú nhận = `2` — **hằng số nghiệp vụ**, dùng ở cả `gro--ghep-sl` lẫn mục 130 (mục 168) |
| `gro--dem-ung-vien` | ~1996 | số ghi chú hợp lệ của một đơn vị — chỉ để báo cáo "bị tranh" (mục 168) |
| `gro--dat-ma-khac-vao-hinh` | ~14280 | `(kq-tk)` bước 6g: **chỉ chọn tấm** rồi gọi C# `gro_net_dat_mk` (mục 170); trả chuỗi báo cáo hoặc nil |
| `gro--mk-bb-str` / `gro--mk-so` | ~14270 | hộp bao / số → chuỗi `x1,y1,x2,y2` (`rtos 2 6`) gửi C# (mục 170) |
| `gro--cs-goi` / `gro--cs-dung` | ~13588 | `(buoc ham args n)` gọi hàm C# với `"cs"`, kiểm lỗi / `nil` / số phần tử; sai → in + `alert` + **dừng lệnh** (mục 184 — bản lisp dự phòng đã gỡ) |
| `gro--phan-loai-net` | ~6123 | `(lay-goc ss)` → `(lines-cont arcs-cont circles tiny-circles)` cho nhận Dim; công tắc `nhannet` lisp/ab/cs, bản C# `gro_net_phan_loai_net` (mục 173) — **chỉ còn gọi C#** (mục 184) |
| `gro--xep-cum-goi-cs` / `gro--xep-cum-can-geom` / `gro--xep-cum-ten-nhom` | ~13390 | cầu nối C# của `gro--xep-cum-phai` (công tắc `xepcum`, mục 178): gọi `gro_net_xep_cum`, so A/B, có cần quét hình học bước 4 không, tên nhãn nhóm — **chỉ còn gọi C#** (mục 184) |
| `gro--vong-kin-geo` | ~6280 | `(lay-goc ss lines-cont arcs-cont)` → `(vk-geo lines-cont arcs-cont)` — bước 1b nhận Dim: nét vòng kín biên dạng trên layer gốc, bỏ khỏi 2 danh sách; công tắc `vongkin`, bản C# `gro_net_vong_kin` (mục 177) — **chỉ còn gọi C#** (mục 184) |
| `gro--dim-dai` | ~10626 | `(texts-lin lines-cont)` → danh sách song song: `nil` hoặc `(best-mem cand-ticks p-mid ray-dir)` — tìm đường dim + chân dim khi thu thập Dim; công tắc `dimdai`, bản C# `gro_net_dim_dai` (mục 174) — **chỉ còn gọi C#** (mục 184) |
| `gro--loc-dim-net` | ~10862 | `(ss-all-dims notes note-bbs all-k-ents)` → `(lines-cont arcs-cont native-dims)` — vòng lọc đầu thu thập Dim; công tắc `locdim`, bản C# `gro_net_loc_dim_net` (mục 176) — **chỉ còn gọi C#** (mục 184) |
| `gro--dim-dai-3b` | ~5484 | cầu nối công tắc `dimdai3b` cho `gro--geom-match-linear-dims` (khớp Dim dài ở bước xử lý nét); bản C# `gro_net_dim_dai_3b` trả `(res-dim-ents ((chỉ-số số-LINE) ...))` (mục 175) — **chỉ còn gọi C#** (mục 184) |
| `gro--chuoi-thang-hang` | ~5433 | `(e-line lines-list)` → `(các-ename điểm-đầu điểm-cuối)`: ghép LINE thẳng hàng nối tiếp (lệch góc < 0.02 rad, lệch ngang ≤ 0.5 mm, khe ≤ 1 mm) — đường dim chính bị tách nhiều đoạn (mục 169). **Đã gỡ khỏi lisp** (mục 184) — nay ở C# `DimDai` / `DimDai3b` |
| `gro--tim-vach-chia-noi` / `gro--diem-noi-so-phia` / `gro--hinh-doan` | ~9290 | `(s-host x-left x-right chat)` → `(x-div e-div điểm)`: vạch chia của tấm gộp = đoạn đứng trong hình; điểm 2 = 2 đầu nối nét ngang sang cả 2 phía, điểm 1 = vạch ở bậc (1 đầu 2 phía, đầu kia 1 phía, chỉ khi `chat` = nil); không đòi chiều cao (mục 186); ưu tiên trước `gro--tim-duong-chia-cat-kem-ename` |
| `gro--ly-do-khong-cat` / `gro--chon-vach-chia` / `gro--t-cua-tr` | ~9340 | phép kiểm TRƯỚC KHI CẮT tấm gộp (mục 186b): phải có vạch chia thật (không cắt ở trung điểm), cùng `t=`, đoạn ≥ 200 mm, text gần đoạn của nó hơn và ≤ 800 mm → `nil` (cho cắt) hoặc chuỗi lý do |
| `gro--dist-hop` | ~1941 | phép thử **trong tầm ghép** (lệch dọc tâm ≤ `max(180,4.5h)`, khe ngang hộp ≤ `max(120,2.5h)`). Từ mục 168 chỉ còn dùng làm phép thử hợp lệ, không dùng để chọn |
| `gro--sl-cua` | 2130 | lấy các dòng phụ của một ghi chú, đã sắp trên→dưới |
| `gro--it-stt` / `-pt` / `-h` / `-codes` / `-x` / `-y` | 1949-1954 | truy cập một GHI CHÚ `(stt pt h codes)` |
| `gro--buoc-tam` | 2819 | khoảng dịch xuống giữa hai tấm tách ra — **hằng số layout** |

### Lớp đặt vị trí bango (tránh va chạm)

| Hàm | Dòng | Việc |
|---|---|---|
| `gro--dat-cot` | ~2877 | **cửa vào chính** — đặt CẢ CỤM bango của một ghi chú: xếp cột, trượt cả cột quanh cụm chữ, dời mọi khối cùng lúc (mục 40) |
| `gro--xep-cot` | ~2815 | xếp n khối thành cột dọc (căn phải, cách `ho`) trong hệ toạ độ cục bộ → `(dịch-từng-khối hộp-cột)` |
| `gro--vat-can-cot` | ~2847 | vật cản của cả cột — thử **từng khối bằng hộp bao riêng**, không dùng hộp gộp |
| `gro--bbox-khoi` | ~2806 | hộp bao một khối (danh sách ename) |
| `gro--vi-tri-bango` | 2637 | 20 ô ứng viên: **4 ô GIỮA của 4 cạnh trước**, rồi phần trượt của từng cạnh (mục 45) |
| `gro--dau-canh` | ~2630 | ứng viên ĐẦU TIÊN của một cạnh — tức **ô giữa** cạnh đó, nhờ mỗi cạnh đã sắp từ giữa ra (mục 45) |
| `gro--style-bango` / `gro--font-bango` | ~3406 | tên kiểu chữ (`guro`) và font (`arial.ttf`) của TEXT bango (mục 46) |
| `gro--bao-dam-style` | ~3425 | tìm kiểu chữ `guro`, chưa có thì tạo; **nghiệm thu bằng `tblsearch`** sau mỗi lần thử (mục 46, 47) |
| `gro--style-entmake` | ~3474 | đường 1: `entmake` bản ghi bảng STYLE, bọc catch (mục 47) |
| `gro--dat-style` | ~3540 | đặt group 7 cho TEXT vừa tạo rồi **đọc lại**; lệch thì vá bằng `entmod` (mục 47) |
| `gro--style-activex` | ~3499 | đường 2: tạo kiểu chữ qua COM; **mỗi thuộc tính bắt lỗi riêng** (mục 47) |
| `gro--vi-tri-tren-giua` | ~2836 | ứng viên số 0 cho ghi chú 1 tấm: trên, căn giữa cụm chữ (mục 42) |
| `gro--vi-tri-cot` | ~2836 | ô TRÁI căn giữa dọc — ứng viên số 0 cho ghi chú ≥ 2 tấm (mục 39, 40), ứng viên số **2** cho ghi chú 1 tấm, sau trên-giữa và dưới-giữa (mục 44, 45) |
| `gro--vat-can` | ~2850 | **danh sách** vật cản trong hộp — `ssget "_C"` lọc thô (**nhớ `trans` WCS→UCS**) rồi `gro--cham-that-p` lọc lại; bỏ qua khối đang xét + layer đầu ra (mục 37, 41) |
| `gro--cham-that-p` | ~2830 | đối tượng có THẬT SỰ chạm hộp không — hình học chính xác cho LINE / LWPOLYLINE / CIRCLE, hộp bao cho loại khác (mục 41) |
| `gro--doan-cat-hop-p` / `gro--cung-cat-hop-p` / `gro--lwpl-cat-hop-p` / `gro--tron-cat-hop-p` / `gro--bb-giao-p` / `gro--oz-p` | ~2735-2825 | các phép thử hình học của `gro--cham-that-p` |
| `gro--dem-va-cham` | ~2742 | `(length (gro--vat-can …))` |
| `gro--dbg-in-ung-vien` | ~2750 | in một ứng viên + vật cản, chỉ khi cờ toàn cục `gro--dbg-vc` > 0 (bật bằng `grovtdbg`) |
| `gro--dich-khoi` | 2691 | dời cả khối bằng `entmod` group 10 |
| `gro--bbox` | 2582 | hộp bao một entity (`vla-GetBoundingBox` có bọc lỗi) |
| `gro--bbox-gop` / `gro--bbox-dich` | 2601 / 2620 | gộp / dịch hộp bao |
| `gro--ho-bango` | 2633 | khoảng hở quanh khối và giữa các khối trong cột = `h*0.2` (trước là `0.5`, mục 40) — **hằng số layout** |
| `gro--ma-san` / `gro--ma-ten` | ~3722 | mã Unicode của `さん` và của 4 tên hãng → mã khắc (mục 54) |
| `gro--ma-tu-codes` | ~3760 | một dòng text → mã khắc, bắt buộc có `さん` trước (mục 54) |
| `gro--chot-ma-khac` | ~3780 | chốt một mã từ kết quả quét từng khung; báo thiếu / mâu thuẫn (mục 54) |
| `gro--hoi-ma-khac` | ~3820 | hỏi mã ở dòng lệnh khi không nhận diện được (mục 54) |
| `gro--khung-co-ghi-chu-p` / `gro--ds-chi-so` / `gro--noi-chuoi` | ~3800 | phụ trợ cho mục 54 |
| `gro--ma-khac-dau` | 2614 | phần đầu mã khắc: `<mã>-<số bản vẽ>` hoặc `<mã>` |
| `gro--so-ban-ve` | 2592 | lấy đoạn số ở cuối `don` (sau dấu `-` cuối) |
| `gro--toan-chu-so-p` | 2574 | chuỗi có toàn chữ số không |
| `gro--fname-cao` / `gro--fname-dong` | 2527 / 2528 | chiều cao chữ và giãn dòng của F-name — **hằng số layout** |
| `gro--layer-dau-ra` / `gro--layer-dau-ra-p` | 2030 / 2034 | **lệnh không được đọc lại đầu ra của chính nó** — bỏ qua `2-F-NAME` / `3-KEGAKI` (xem mục 29) |

### Lớp đánh số theo VÙNG polyline (chế độ tuỳ chọn của `gro`)

| Hàm | Dòng | Việc |
|---|---|---|
| `gro--sap-theo-vung` | 2427 | **cửa vào chính** → `(danh-sách-(chỉ-số-vùng . ghi-chú) danh-sách-ngoài-vùng)` |
| `gro--doc-vung` | ~2652 | đọc polyline thành vùng. `chi-4` = T → **đúng 4 đỉnh** (khung bao); nil → **≥ 3 đỉnh** (vùng, mục 61) |
| `gro--trong-vung` | ~2600 | ray-casting điểm-trong-đa-giác; cần cho vùng hình L (mục 61) |
| `gro--pl-kin-p` / `gro--pl-gon` | ~2495 | cờ closed; bỏ đỉnh cuối trùng đỉnh đầu (mục 51) |
| `gro--quet-cp` | ~2774 | quét text theo **từng vùng** bằng `ssget "_CP"` — cắt đúng đa giác; **phải zoom trước** (mục 63) |
| `gro--quet-text` | ~2751 | cách cũ: `ssget "_X"` cả bản vẽ (mục 58). Nay chỉ còn `growp` dùng để đối chiếu |
| `gro--zoom-vung` | ~3760 | tự ZOOM thấy hết vùng làm việc rồi **đo lại** để xác nhận (mục 59) |
| `gro--dat-ten-bv` | ~3830 | ghi tên bản vẽ vào từng khung bao, tự tránh vật cản (mục 60) |
| `gro--ten-bv-ds` | ~3790 | 15 ô ứng viên trong dải 1/5 trên, duyệt trên→dưới trái→phải (mục 60) |
| `gro--ten-bv-cao` / `-le` / `-cot` / `-hang` | ~3770 | hằng số hình thức của tên bản vẽ (mục 60) |
| `gro--khung-nhin` | ~3740 | hộp bao tầm nhìn hiện tại từ `VIEWCTR`/`VIEWSIZE`/`SCREENSIZE` (mục 59) |
| `gro--bb-chua-p` | ~3730 | hộp ngoài có chứa trọn hộp trong không (mục 59) |
| `gro--giay-p` | ~2725 | đối tượng có ở không gian giấy không (group 67 = 1), mục 58 |
| `gro--tim-vung` | ~2850 | tự tìm vùng trên layer vùng, chỉ giữ cái nằm trong khung bao (mục 53) |
| `gro--khung-cua-vung` | ~2900 | khung bao nào "sở hữu" một vùng: chứa trọn → nhỏ nhất; không → chồng lấn nhiều nhất (mục 65) |
| `gro--bb-giao-dt` / `gro--bb-trung-p` | ~2880 | diện tích chồng lấn hai hộp bao; hai hộp có trùng khít không (mục 65) |
| `gro--nhom-vung` | ~2910 | gộp khung bao + vùng thành một danh sách phẳng đúng thứ tự (mục 53) |
| `gro--in-pl-bo` | ~2556 | in lý do các polyline bị loại (mục 51) |
| `gro--vung-chua` / `gro--vung-giao` | ~2650 | vùng nhỏ nhất chứa điểm chèn / giao hộp bao (mục 51) |
| `gro--sap-vung` | 2371 | sắp xếp vùng theo hàng trên→dưới, trái→phải |
| `gro--vung-cua` | ~2686 | vùng của một ghi chú: ưu tiên **chứa** điểm chèn, rồi mới **giao** hộp bao (mục 51) |
| `gro--pl-dinh` | 2269 | đỉnh của LWPOLYLINE (group 10 lặp) hoặc POLYLINE nặng (duyệt VERTEX) |
| `gro--vung-bbox` / `gro--bbox-dt` | 2335 / 2342 | hộp bao và diện tích hộp bao |
| `gro--layer-list` | 1710 | danh sách tên layer trong bản vẽ (nạp popup "Layer vùng") |
| `gro--dem-khung-layer` | ~1765 | **một lượt** đếm cả hai luật cho từng layer: `(tên số-4-đỉnh số-từ-3-đỉnh)` (mục 64) |
| `gro--layer-khung-ds` | ~1800 | hai danh sách song song cho MỘT popup; `chi-4` chọn luật đếm (mục 52, 64) |

Thứ tự các vùng dùng **chung `gro--sap-xep`** với mốc là **góc trên-trái hộp bao** — nên vùng
cũng xếp theo hàng trên→dưới, trái→phải, y như ghi chú. Không có thuật toán thứ hai.

> **Vì sao `gro--doc-vung` và `gro--sap-vung` phải tách làm hai.** Sắp xếp vùng cần `hmax`
> (chiều cao chữ lớn nhất), mà `hmax` chỉ biết được **sau** khi đã quét text — còn quét text
> ở chế độ B lại cần biết các vùng trước để lọc. Vòng phụ thuộc đó được cắt bằng cách: đọc
> vùng (chưa sắp) → lọc text bằng `gro--vung-cua` (chỉ cần *có nằm trong vùng nào không*,
> không cần thứ tự) → tính `hmax` → giờ mới sắp xếp vùng → gán chỉ số vùng.

### Lớp ghi CSV

**Hai lệnh ghi CSV theo hai kiểu KHÁC HẲN nhau:**

| Hàm | Dòng | Của lệnh | Việc |
|---|---|---|---|
| `gro--csv-ghi-de` | 392 | **`gro`** | **GHI ĐÈ** — xoá sạch rồi ghi lại từ đầu |
| `guro--csv-append-utf8bom` | 334 | `bg` | **NỐI TIẾP** — đọc lại file cũ, ghép thêm, ghi đè toàn bộ |
| `guro--has-utf8-bom` | 318 | `bg` | file đã có BOM UTF-8 chưa (quyết định Charset khi đọc lại) |
| `gro--csv-dem-dong` | 411 | `gro` | đếm số dòng cũ **chỉ để báo cáo** đã xoá bao nhiêu |

Cả hai đều ghi UTF-8 có BOM qua COM `ADODB.Stream`. Điểm tạo ra hành vi "nối tiếp" ở bản cũ
chính là **bước đọc lại rồi ghép vào trước** — bỏ bước đó đi là thành ghi đè
(`SaveToFile … 2` = `adSaveCreateOverWrite`, vốn đã là ghi đè sẵn).

> Nhờ không phải đọc lại file cũ, `gro--csv-ghi-de` **không dính gì** đến đoạn rắc rối nhất
> của bản nối tiếp: dò BOM UTF-8 và chọn Charset `"_autodetect"` (mục 7 và 8 — từng làm hỏng
> **dòng đầu tiên** của CSV mỗi lần chạy `bg`). Cả một lớp lỗi biến mất theo.

> **Không dùng `(open)` + `(write-line)` để ghi CSV** — AutoLISP không chọn được encoding khi
> ghi, sẽ ghi theo codepage hệ thống (mục 7). `(open … "a")` chỉ dùng để **thử quyền ghi**
> rồi đóng ngay.

### Lớp hộp thoại

| Hàm | Dòng | Việc |
|---|---|---|
| `gro--dcl-path` / `guro--dcl-path` | 1634 / 375 | đường dẫn file DCL tạm (Support, fallback tempprefix) |
| `gro--mk-list` | 1651 | **danh sách mã khắc — nguồn duy nhất**: `("I" "0" "G" "Y")` |
| `gro--write-dcl` / `guro--write-dcl` | 1664 / 387 | ghi nội dung DCL ra file lúc chạy (không cần đi kèm file `.dcl` riêng) |
| `gro-get-input` | ~2194 | **lớp chọn đường** (mục 86): DLL đã nạp → hộp thoại .NET `gro_net_dialog`, không thì `gro--hoi-dcl`. Trả list 9 giá trị `(nth 0..8 dlg)` hoặc `nil` |
| `gro--hoi-dcl` | ~2271 | hộp thoại DCL `gro_dlg` (ruột cũ của `gro-get-input`, chỉ đổi tên) — **dự phòng** khi chưa NETLOAD |
| `guro-get-bango` | 418 | hiện hộp thoại của `bg` → số bắt đầu hoặc `nil` |

### Lớp tiện ích F-name / đối chiếu

| Hàm | Dòng | Việc |
|---|---|---|
| `fn--fmt-t` | 1334 | định dạng độ dày cho F-name (`12` → `"12.0"`, `4.5` → `"4.5"`) |
| `fn--member-num` | 1323 | số có nằm trong danh sách không — **so sánh có sai số**, không so chuỗi |
| `fn--set-color` | 1313 | đổi màu entity qua `vla-put-color` (thay cho `(command ".change" …)`) |
| `fn--block` | 1342 | dựng một khối báo lỗi có tiêu đề |
| `fn--split` | 1261 | tách một dòng CSV thành list các trường (có giới hạn vòng lặp) |
| `fn--tail` | 1281 | lấy phần sau dấu `-` cuối cùng (`"I-AB-12"` → `"12"`) |
| `fn--strip-bom` | 1242 | bỏ BOM UTF-8 ở đầu dòng CSV đọc bằng `read-line` |
| `fn--ent-text` | 1301 | text thuần của entity (gọi `bg--ent-codes` + `bg--codes-str`) |
| `bg--getvar-safe` | 890 | `getvar` trả `nil` thay vì nổ khi biến hệ thống không tồn tại |
| `bg--bytes` / `bg--bytes-list` | 899 / 894 | in mã byte từng ký tự — dùng cho `bgdbg` |

Hàm **chết / không dùng**: `export-string-a2b` (2), `subst-all` (34), `fn--subst-all` (1229),
`mk2--set-color` (1107, trùng chức năng `fn--set-color`).
