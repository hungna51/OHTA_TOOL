# 04. Dữ liệu, layer và định dạng CSV

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Bảng dữ liệu và layer

Các con số đặt chữ rải rác trong code, gom về một chỗ. Gốc toạ độ = điểm chèn của ghi chú
(`(assoc 10)` của TEXT/MTEXT ghi chú), `height` = chiều cao chữ của ghi chú đó.

| Đầu ra | Layer | Màu | Chiều cao | Vị trí |
|---|---|---|---|---|
| TEXT bango | `0` | 1 (đỏ) | `height * 1.5`, kiểu chữ **`guro`** (mục 46) | **tự tìm chỗ trống** quanh cụm ghi chú (mục 33); ghi chú 1 tấm ưu tiên **trên giữa → dưới giữa → trái giữa** (mục 45), ghi chú ≥ 2 tấm ưu tiên **cột dọc bên trái** (mục 39) |
| MTEXT mã khắc | `3-KEGAKI` | 1 (đỏ) | `10.0` (group 71 = 1) | **đúng điểm của TEXT bango** |
| TEXT tên bản vẽ | `0` | 1 (đỏ) | `hmax * 2.25` (= 1.5 × bango) | góc **trên-trái** của khung bao, trong dải 1/5 trên (mục 60) |

> **Nội dung mã khắc: `<mã>-<số bản vẽ>-<bango>`**, ví dụ `I-1-1`, `I-1-2`… Đoạn `<số bản
> vẽ>` lấy từ chính tên bản vẽ (xem mục 32). Tên bản vẽ không có đoạn số ở cuối thì rút gọn
> còn `<mã>-<bango>`.
| F-name — `gro` | `2-F-NAME` | theo layer | `1.0`, giãn dòng `1.6667` | **cùng điểm với mã khắc** (nằm lồng trong nó) |
| F-name — `FN` (cũ) | `2-F-NAME` | theo layer | `1.0` (group 41 = `10.0`) | xuống `11.0` từ điểm mã khắc |

> **`gro` tạo F-name bằng TEXT — MỖI DÒNG MỘT ĐỐI TƯỢNG, không dùng MTEXT.** Hai khác biệt
> so với `FN`:
> 1. **Không bị ngắt dòng.** MTEXT của `FN` đặt group 41 (chiều rộng) = `10.0` nên dòng
>    `N=<tên bản vẽ>-<số>` dài bị xuống dòng giữa chừng, rất khó đọc. TEXT không có chiều
>    rộng nên dòng `N=` luôn nằm trọn một dòng.
> 2. **Nằm lồng trong vùng mã khắc** thay vì nằm dưới. Mã khắc cao `10.0`, F-name ~6 dòng ×
>    `1.6667` ≈ `10.0` — vừa khít cùng một dải.
>
> **Bẫy khi đổi MTEXT → TEXT:** MTEXT mã khắc dùng điểm căn **trên-trái** (group 71 = 1),
> còn TEXT lấy group 10 là **chân dòng** (baseline-left). Nên dòng F-name đầu tiên phải lùi
> xuống đúng một chiều cao chữ thì hai thứ mới khớp nhau.
>
> **`FN` vẫn tạo MTEXT như cũ**, chưa đổi — chạy `FN` sau `gro` sẽ cho ra hai kiểu khác nhau.

`gro` tạo bango màu **đỏ (1)**. Quy trình cũ `bg`→`mk` để lại màu **3** vì `mk` đổi màu bango
sang 3 để đánh dấu đã xử lý — đó là điểm khác biệt duy nhất giữa kết quả của `gro` và của
`bg`+`mk`+`FN`.

Định dạng mỗi dòng CSV (`bg` và `gro` ghi, `FN` đọc):

```
<don>-<bango> , Q , M , T , <don>- , [V]
     0          1   2   3      4      5
```

`don` = tên file DWG đã cắt đuôi bằng `(vl-string-right-trim "- S A s a . d f x g w" …)`.
File CSV nằm cạnh file DWG: `<DWGPREFIX><don>.csv`. **Mọi thay đổi định dạng CSV ở `bg`/`gro`
đều phải kiểm tra lại `FN`** (và ngược lại).

> **`gro` GHI ĐÈ file CSV, `bg` NỐI TIẾP.** Chạy `gro` lần hai sẽ **xoá kết quả lần một**
> khỏi CSV — file luôn là kết quả của đúng lần chạy cuối. Muốn đánh số làm nhiều đợt (ví dụ
> từng vùng một) thì phải **chọn hết trong một lần chạy**, hoặc tự sao lưu file CSV giữa các
> đợt. Báo cáo in rõ `… dong CSV (GHI DE)` kèm số dòng cũ đã xoá.

Nội dung MTEXT F-name (`\P` là mã xuống dòng của MTEXT), dòng `V=` chỉ có khi cờ V là
`m` hoặc `*` (**không** có với `k`):

```
N=<don>-<bango>  M=<vật liệu>  T=<độ dày>  Q=<số lượng>  [V=]  Z=<vật liệu Z>
```

Danh sách độ dày chuẩn (`dst`), lệch ra ngoài thì mã khắc bị đổi màu 2 + báo `Error T!!!`:

```
0.6  0.8  1.0  1.2  1.6  2.3  3.2  4.5  6.0  9.0  12.0  16.0  19.0
```

Cờ `V=`: `m` = bẻ (`曲`), `k` = vát (`ｶｲｻｷ`), `*` = cả hai, rỗng = không có.

**Hai ngưỡng vật liệu KHÁC NHAU** — xem [Cần kiểm tra lại](14-can-kiem-tra-lai.md):

| Dùng ở | Ngưỡng | Kết quả |
|---|---|---|
| cột `M` của CSV (`bg--fields`, dòng 562) | `> 3.2` | `SS400` / `SPHC` |
| **tấm CPL** (`gro`, mục 43) — ghi đè cả hai dòng trên | có chữ `CPL` | M = `ムキ 縞板`, Z = `SS400-SIMA` |
| `M=` và `Z=` của F-name (`FN` dòng 1486, `gro`) | `>= 4.5` | `SS400`+`SS400` / `SPHC`+`SPC` |

---

## Quy tắc xử lý nét & chuyển đổi Layer (Mục 69)

Áp dụng cho lệnh độc lập `c:xln`, `c:cln`, và tích hợp tự động trong `c:gro` (chỉ xử lý các đối tượng nằm trong phạm vi các Khung bao được chọn):

| Đối tượng nguồn (thuộc layer gốc) | Linetype | Layer đích | Thuộc tính gán |
|---|---|---|---|
| Hình học (`LINE`, `LWPOLYLINE`, `POLYLINE`, `ARC`, `CIRCLE`, `SPLINE`, `ELLIPSE`) | `Continuous` | `0-KAKOU` | Color = ByLayer, Linetype = ByLayer |
| Hình học (`LINE`, `LWPOLYLINE`...) | `CENTER1` | `3-KEGAKI` | Color = ByLayer, Linetype = ByLayer |
| Văn bản (`TEXT`, `MTEXT`) | Bất kỳ | `1-SUNPOU` | Color = ByLayer, Linetype = ByLayer |
| **Các nét còn lại (`DASHED`, `HIDDEN`...)** | Khác 2 loại trên | **GIỮ NGUYÊN trên layer gốc** | Giữ nguyên thuộc tính, nhóm theo Linetype đưa vào danh sách kiểm tra |


---

## 169. Đường dim chính bị TÁCH thành nhiều LINE thẳng hàng → Dim không được nhận (người dùng báo, 28/10/2026)

**Triệu chứng:** dim `700.8` đứng bên trái tấm, vẽ thành chữ U (tick trên + đường đứng + tick dưới) bằng LINE rời, nhưng `gro` / `xln` chế độ tự động **không** nhận là Dim → cả chữ U bị coi là nét cắt Continuous, chuyển sang `0-KAKOU`.

**Nguyên nhân (người dùng chỉ ra bằng grip):** đường đứng không phải 1 LINE mà là **2 LINE nối tiếp** (~350 + ~350, nối ở giữa). Bộ khớp Dim chiều dài `gro--geom-match-linear-dims` bước 1 chỉ nhận **một** LINE có chiều dài ≈ V (sai số `max(2, 2%·V)`) → không đoạn nào đạt. Nhánh dự phòng "cặp tick" cũng không cứu được ca này.

**Sửa:** hàm mới `gro--chuoi-thang-hang (e-line lines-list)` ghép mọi LINE **thẳng hàng nối tiếp** với `e-line` thành một đường, trả `(các-ename điểm-đầu điểm-cuối)`:

| Điều kiện nối một LINE vào chuỗi | Giá trị |
|---|---|
| cùng phương | lệch góc < 0.02 rad |
| nằm trên cùng đường thẳng | cả 2 đầu mút cách đường thẳng của `e-line` ≤ 0.5 mm |
| nối tiếp / chồng lên chuỗi hiện có | khe dọc trục ≤ 1.0 mm |

Áp dụng ở **cả 2 chỗ** dò "đường dim chính":
1. `gro--geom-match-linear-dims` (nhận nét Dim để chuyển layer `1-SUNPOU`): LINE song song chữ số, chạm hộp chữ nới 1.5 lần mà **không** dài ≈ V → thử ghép chuỗi; tổng chiều dài chuỗi ≈ V thì **cả chuỗi** là đường dim chính, chân dim / tick dò tại 2 đầu xa nhất của chuỗi. In `[Xu ly net] Dim "700.8": duong dim chinh ghep tu 2 LINE thang hang (muc 169).`
2. `gro--thu-thap-dim-units` (gom cụm Dim để gán Dim cho hình): cùng luật, cụm Dim chứa mọi đoạn của chuỗi.

LINE đơn dài ≈ V vẫn được ưu tiên như cũ (không ghép chuỗi). Nét biên dạng thuộc vòng kín (mục 143/145) đã bị loại khỏi danh sách nét trước bước này nên không thể bị ghép vào chuỗi Dim.

**Kiểm chứng:** mô phỏng Python `sim_chuoi_dim.py` 8/8 đạt — ca thật (2 đoạn → 700.8); không nuốt cạnh tấm song song cách 33 mm; 3 đoạn có khe 0.5 mm; khe 5 mm không ghép; lệch ngang 2 mm không ghép; tổng 600 ≠ 700.8 không khớp; LINE đơn vẫn khớp; 2 đoạn ngang chồng nhau. Cấu trúc file: ngoặc cân bằng, +1 `defun`, không trùng tên, CRLF.

> **Chưa chạy trên bản vẽ thật.** Nghiệm thu: chạy `gro` (xử lý nét tự động) trên tấm có dim `700.8` — chữ U phải sang `1-SUNPOU`, dòng lệnh in dòng `muc 169` ở trên.
