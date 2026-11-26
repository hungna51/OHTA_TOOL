# QUY CHUẨN VẼ BẢN TRẢI PHẲNG (theo bản chuẩn xưởng, chốt 12/11/2026)

Áp dụng cho mọi bản vẽ phòng 3 (Triển khai & Vẽ) vẽ trên `moi-truong-ve.dxf`. Phần chung do `ve.py` (`them_chung`) tự làm; mẫu biên dạng (`thu_vien/<nhóm>/*.py`) chỉ vẽ hình và dim riêng của hình. Tệp này gửi kèm khi AI viết mẫu vẽ mới.

## Mẫu biên dạng phải đủ thông số
- Mỗi mẫu khai `THAM_SO = [("x", "ý nghĩa", "tự tính"), ("y", …), ("r1", "Bán kính cung lồi", "y/2"), …]`: ĐỦ mọi thông số hình cần, tên chữ cái ngắn (x, y, x1, y1, r1, r2, a1…). x, y do chương trình tính (trải phẳng); thông số riêng có mặc định (chữ mô tả) hoặc `None` = bắt buộc.
- `VI_DU = {"x": …, "y": …, "tham_so": {…}}` — số ví dụ, mọi số khác nhau. `ve.py --huong-dan` vẽ ảnh `<ten>.png` cạnh mẫu: dim nào bằng số của thông số thì ghi tên thông số (x, y, r1…) — ảnh hướng dẫn nhập.
- Mẫu đọc thông số riêng từ `t["tham_so"]`, thiếu thì dùng mặc định; số không hợp lệ (vd R < nửa bề rộng) → `raise ValueError` giải thích. Thiếu thông số bắt buộc → `ve.py` báo, không vẽ.
- Lúc đọc bản vẽ, AI điền `tham_so` cho tấm theo danh sách thông số của mẫu đã chọn; người kiểm ở ✎ Sửa thông số → Thông số mẫu (`r1=45; r2=38`) và xem ảnh ở 📚 Thư viện mẫu.

## Viền cắt — layer `0-KAKOU`
- Cạnh thẳng và cung vẽ **rời**: LINE (`nen.duong`) + ARC thật (`nen.cung`). **Không** chia cung thành nhiều đoạn thẳng, không dùng polyline xấp xỉ cung.
- Tấm toàn cạnh thẳng được dùng polyline kín (`nen.vien`, `nen.chu_nhat`).
- Đường bẻ: LINE `0-KAKOU`, nét DASHED (`nen.duong_be`).

## Phần AI tự suy — tô VÀNG (25/11, `chua_chac.py`)
- Khách không ghi số mà AI lấy theo hình (vd R bo góc khoét, vị trí lỗ không có dim) → AI ghi `chua_chac` [{o, so, noi_dung}] + nghi vấn 🟡 hỏi khách. `ve.py` tô nét vẽ từ mục đó màu **vàng (ACI 2)** — `o` = lo / khoet / goc / dinh / ke_gaki, `so` = thứ tự mục (0 = cả ô), hậu tố `.r` = chỉ cung bo R. Ô người dùng đã sửa (✎) = đã xác nhận → không tô. Ô khác (kich_thuoc…) chỉ ghi chú.

## Dim
- **Chân dim chạm đối tượng (25/11, `chan_dim.py`)**: mỗi chân gióng phải đặt vào đúng đối tượng dim đo — đỉnh / mút đoạn viền hoặc đường bẻ, cạnh thẳng đứng / ngang trùng đường gióng, điểm cực của cung, tâm lỗ, tâm ケガキ — sai số **0.05 mm**, chọn điểm gần đường dim nhất. Không có đối tượng nào → **không vẽ** dim đó, báo "chân dim không chạm hình" (dòng cam) để người vẽ xem. Áp cho mọi dim `ve.py` vẽ: dim khách, chuỗi tự dựng, dim tổng X / Y.
- **Chỉ dim như khách + dim tổng X / Y**: ngang, dọc, R, Ø, **xiên** (`dim_khach.loai = "xien"`, `diem` = 2 đầu cạnh — dim theo cạnh, đường dim ra phía ngoài hình) và **góc** (`"goc"`, `diem` = [điểm cạnh 1, đỉnh, điểm cạnh 2]). Điểm không nằm trên hình, hoặc số đo lệch số khách (> 0.5 mm / 0.5°) → không vẽ, báo thiếu.
- **Vát / bo góc, khoét trong** (`goc_khoet.py`, chung mọi mẫu): vát = đoạn thẳng LINE `0-KAKOU`, bo = ARC thật, cùng layer viền; nhãn gom theo nhóm bằng leader `n-C10` / `n-R5` (không thêm dim R riêng — dim khách R trùng nhãn coi là đủ). Khoét = viền kín `0-KAKOU` (bo R mỗi góc nếu có); **không** thêm chuỗi dim riêng — chỉ dim như khách dim (`dim_khach`).
- **Luôn đủ kích thước của khách**: mọi kích thước khách ghi trên bản vẽ còn đúng trên hình trải phẳng (`dim_khach`, AI đọc lúc đọc bản vẽ) phải có trên bản vẽ OHTA, cùng số. `ve.py` so với các dim đã vẽ: thiếu dim ngang / dọc → tự thêm một hàng dim (sau chuỗi dim, trước dim tổng); thiếu R → dim R chỉ khi bản vẽ có đúng cung R đó, không có thì báo **lệch** (không dim sai); còn lại (Ø lỗ, góc…) báo "còn thiếu dim của khách" để bổ sung trên CAD.
- **Dim tổng** X (dưới cùng) và Y (bên phải): layer **`1-XY`**, kiểu `ISO-26` — `ve.py` tự thêm.
- **Dim theo khách (18/11)**: khách có dim vị trí (`dim_khach` ngang / dọc có tu, den) → vẽ **đúng từng chuỗi của khách**, mỗi chuỗi một hàng — chuỗi ngang ở **TRÊN**, chuỗi dọc ở **PHẢI** (như bản khách), chuỗi ngắn sát tấm, chuỗi dài ra ngoài, dim tổng `1-XY` ngoài cùng; không tự dựng chuỗi, chỉ thêm dim tổng khi khách thiếu. Vẽ **đường tâm** (nét CENTER, `1-SUNPOU`) ngang + dọc qua tâm tấm — chi tiết không có dim ra mép là đối xứng qua đây. Nhãn lỗ / ケガキ đặt xuống dưới tấm. Không có dim khách → tự dựng chuỗi dưới / phải như dưới đây.
- **Lỗ khoá / 変形孔** (`kieu` "khoa D d k G nho|lon"): Ø lớn D + Ø nhỏ d, k = khoảng cách hai tâm; vị trí lỗ = **tâm khách dim tới** (nho / lon = tâm Ø nhỏ hay Ø lớn), G = hướng từ tâm đó sang tâm kia — **mọi lỗ cùng hướng như hình chi tiết của khách** (thiếu G: Ø nhỏ bên trái); viền = 2 cung thật hợp lại. Vẽ thêm **hình chi tiết A (5:1)** bên trái tấm (layer `1-SUNPOU`, không cắt) như hình khách: nét mép tấm, đường tâm, nhãn Ø lớn / Ø nhỏ, dim "mép tấm → tâm khách dim tới" | "tâm → tâm kia" (khách 130996-thử 18/11: 10 | 6); khoanh lỗ tham chiếu trên tấm + chữ A.
- **Chỗ đặt dim**: `dim_khach.tai` = toạ độ đường dim khách đặt (ngang: y, dọc: x) — dim đặt đúng chỗ đó, **được nằm trong hình** như bản khách; không có `tai` thì xếp hàng ngoài tấm.
- **Tỉ lệ dim theo cỡ tấm (18/11)**: cạnh lớn ≥ 400 mm → cỡ gốc; nhỏ hơn thu theo (cạnh / 400, không dưới 0.35 — vd tấm 100 × 50 dùng 0.35): DIMSCALE của kiểu dim (chữ + mũi tên dim, leader), cỡ chữ ghi chú, khoảng viền → dim / giữa các hàng dim, vị trí nhãn lỗ, ngưỡng dim ngắn, hình chi tiết. F-name giữ cỡ 24 (dữ liệu cho máy / GRO).
- **Đường dẫn nhãn (leader)**: mũi tên chạm **mép lỗ** (lỗ khoá: mép Ø lớn) theo hướng ra điểm gãy, không chỉ vào giữa lỗ; đoạn cuối nằm ngang dài bằng chữ (đo bằng font thật của kiểu chữ) làm **gạch chân**, chữ ngồi trên; chữ **gắn** vào leader (annotation) để kéo trong CAD leader đi theo.
- **Theo bản người sửa 18/11**: đường tâm tấm chỉ theo hướng có dim khách đo từ tâm; mọi lỗ có **dấu tâm** (chữ thập, mỗi nhánh r + 1.6; lỗ khoá: đoạn qua 2 tâm + đoạn vuông góc tại mỗi tâm); **nối tâm** các lỗ hàng / cột ngoài cùng của nhóm bằng nét CENTER **chỉ khi bản khách có vẽ** (`lo.noi_tam`); nhóm lỗ tròn thường ghi **dim đường kính `n-Øx`** (chỉ vào lỗ gần góc trên-trái, kéo xuống phải, chữ ngang) thay đường dẫn; nhãn lỗ khác (khoá, taro, 皿, lỗ dài) kéo từ lỗ gần chỗ đặt nhãn nhất; dim **ngắn hơn 25 mm ở đầu / cuối chuỗi** đưa chữ ra ngoài 2 bên (vd `10` sang trái, `15` sang phải), dim giữa chuỗi giữ thẳng hàng.
- **Chuỗi dim chi tiết**: layer `1-SUNPOU`, giữa tấm và dim tổng — chuỗi ngang **phía dưới**, chuỗi dọc **bên phải**. Mốc: mép tấm, đường bẻ, dấu ケガキ, tâm cung có ghi R, mốc mẫu thêm (`nen.moc_x`, `nen.moc_y`). `ve.py` tự dựng.
- Khoảng cách: viền → chuỗi dim 80 (`LE_DIM`), mỗi hàng dim sau cách thêm 44 (`BUOC_DIM`); dim tổng luôn ở hàng ngoài cùng (có chuỗi dim: 124).
- **Cung cần ghi R**: `nen.cung(..., dim=True)` — dim R (kiểu `ISO-26`, CAD tự dùng `ISO-26$4`), chữ ra ngoài phía trên-phải, tâm cung vào chuỗi dim. Nhiều cung cùng R chỉ ghi một lần.
- Đầu tấm là cung bán nguyệt: 2 dim nửa chiều cao (mép → tâm cung → mép) phía đầu đó, cách viền `LE_DIM`, chữ đưa ra ngoài (dưới / trên) bằng `vi_tri_chu`.
- Không đặt dim chi tiết phía trên tấm.

## Lỗ — layer `0-KAKOU` (phần chung `ve_lo`, mẫu không tự vẽ)
- Mỗi nhóm lỗ của bảng tấm: `d`, `so_luong`, `taro`, `sara`, `d_sara`, `loai` (`tron` / `dai`: `rong`, `dai`, `goc`), `vi_tri` = tâm từng lỗ trên hình trải phẳng (gốc trái dưới).
- Lỗ tròn: CIRCLE Ø `d`. Taro: CIRCLE **Ø mồi theo bảng TMT** (M3 2.6 · M4 3.4 · M5 4.3 · M6 5.1 · M8 6.9 · M10 8.6 · M12 10.5 · M14 12.2 · M16 14.3 · M20 17.7). 皿: CIRCLE Ø lỗ thông, nhãn kèm Ø miệng côn. Lỗ dài: 2 cạnh + 2 cung thật.
- Mỗi nhóm một leader `1-SUNPOU` nhãn `n-Ø10` / `n-M6` / `n-皿Ø6.6 (Ø12)` / `n-10×20` (như cách khách ghi); nhãn Ø đủ cho dim khách Ø.
- Tâm lỗ vào chuỗi dim dưới (x) và phải (y); lỗ thẳng hàng chỉ một mốc. Chuỗi dim quá 12 đoạn → tách thêm hàng (`MOC_TOI_DA`).
- Nhóm lỗ chưa có vị trí → không vẽ, báo để nhập ở ✎ Sửa thông số → Lỗ.

## Dấu khắc vạch ケガキ — layer `3-KEGAKI`
- Mỗi dấu: chữ thập 10 mm (2 LINE) tại tâm dấu (`ke_gaki` của bảng tấm, toạ độ trên hình trải phẳng, gốc góc trái dưới).
- Một leader `1-SUNPOU` từ dấu giữa chéo lên trên-phải (≈ +23, cao hơn mép trên 53), chữ **`n-ケガキ`**. Bản khách ghi `マーキング` / `罫書き` vẫn ghi `ケガキ`.

- **Dấu tam giác / chữ V (25/11)**: `ke_gaki` mục `[x, y, đáy, hướng, cao]` — **mũi tại (x, y)** (chỗ khách dim tới), mũi chỉ `hướng` độ (0 phải, 90 lên; dấu quanh tâm tròn chỉ vào tâm), đáy rộng `đáy` lùi sau mũi một đoạn `cao` (0 = tam giác đều). Hai góc đáy nằm trên mép tấm → vẽ **chữ V 2 nét** (đáy là mép — như bản xưởng Desktop/test.dxf 25/11); không thì tam giác kín 3 nét. `[x, y]` vẫn là chữ thập 10 mm. Chân dim chạm được mũi, 2 góc đáy và giữa đáy.
- **Tấm tròn** (mọi đỉnh `dinh` trên vòng tâm khung bao, mọi cạnh là cung bán kính đó): dim tổng là **Ø** (1-XY) thay cho X / Y.
- **Tấm tròn / vành khăn** (mẫu `da_giac`): 2 đỉnh đối nhau, cả hai có R — Øn: `[[0, n/2, n/2], [n, n/2, n/2]]`; lỗ giữa ở `lo`.

## Đường tâm khách dim từ đó (25/11, Drawing7)
- `duong_tam`: mỗi mục `[x1, y1, x2, y2]` = đường tâm (CENTER, `1-SUNPOU`) khách lấy làm gốc dim — vd đường tâm xiên 45° qua tâm lỗ, dim góc 45° và dim khoảng cách tới cạnh cắt góc song song. Chân dim chạm được đường này.

## スリット — layer `4-SHT` (25/11)
- Nhãn leader `スリット` (nhiều nét: `n-スリット`) từ giữa nét đầu ra ngoài bên trái tấm.
- `slit`: mỗi mục `[x1, y1, x2, y2]` = một nét cắt đơn (LINE `4-SHT`), vd vành tròn cắt đứt: từ mép trong tới mép ngoài theo bán kính. Chân dim chạm được hai mút. Khách không ghi vị trí → `chua_chac {o: "slit"}` → nét slit tô vàng.

## F-name — layer `2-F-NAME`
- TEXT cao 24, kiểu chữ `Text` (MS PGothic), dòng cách 40; đặt **dưới tấm**, cách mép trái 100, dòng đầu dưới đường dim tổng X ≈ 98.
- `N=` (số bản vẽ / tên; không có thì bango tạm `<mã>-<số>`), `M=`, `T=` (luôn có phần thập phân: 12.0, 1.6), `Q=`, các dòng `V=` / ấn 1 lần, `Z=`.

## Khung_IN
- LWPOLYLINE kín 4 đỉnh, bao hết tấm + dim + ケガキ + F-name, lề tối thiểu 30, nới đều cho đủ tỉ lệ **khổ A nằm ngang (√2 : 1)**.
