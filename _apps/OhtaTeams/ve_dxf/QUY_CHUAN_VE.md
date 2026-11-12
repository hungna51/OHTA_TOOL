# QUY CHUẨN VẼ BẢN TRẢI PHẲNG (theo bản chuẩn xưởng, chốt 12/11/2026)

Áp dụng cho mọi bản vẽ phòng 3 (Triển khai & Vẽ) vẽ trên `moi-truong-ve.dxf`. Phần chung do `ve.py` (`them_chung`) tự làm; mẫu biên dạng (`thu_vien/*.py`) chỉ vẽ hình và dim riêng của hình. Tệp này gửi kèm khi AI viết mẫu vẽ mới.

## Mẫu biên dạng phải đủ thông số
- Mỗi mẫu khai `THAM_SO = [("x", "ý nghĩa", "tự tính"), ("y", …), ("r1", "Bán kính cung lồi", "y/2"), …]`: ĐỦ mọi thông số hình cần, tên chữ cái ngắn (x, y, x1, y1, r1, r2, a1…). x, y do chương trình tính (trải phẳng); thông số riêng có mặc định (chữ mô tả) hoặc `None` = bắt buộc.
- `VI_DU = {"x": …, "y": …, "tham_so": {…}}` — số ví dụ, mọi số khác nhau. `ve.py --huong-dan` vẽ ảnh `<ten>.png` cạnh mẫu: dim nào bằng số của thông số thì ghi tên thông số (x, y, r1…) — ảnh hướng dẫn nhập.
- Mẫu đọc thông số riêng từ `t["tham_so"]`, thiếu thì dùng mặc định; số không hợp lệ (vd R < nửa bề rộng) → `raise ValueError` giải thích. Thiếu thông số bắt buộc → `ve.py` báo, không vẽ.
- Lúc đọc bản vẽ, AI điền `tham_so` cho tấm theo danh sách thông số của mẫu đã chọn; người kiểm ở ✎ Sửa thông số → Thông số mẫu (`r1=45; r2=38`) và xem ảnh ở 📚 Thư viện mẫu.

## Viền cắt — layer `0-KAKOU`
- Cạnh thẳng và cung vẽ **rời**: LINE (`nen.duong`) + ARC thật (`nen.cung`). **Không** chia cung thành nhiều đoạn thẳng, không dùng polyline xấp xỉ cung.
- Tấm toàn cạnh thẳng được dùng polyline kín (`nen.vien`, `nen.chu_nhat`).
- Đường bẻ: LINE `0-KAKOU`, nét DASHED (`nen.duong_be`).

## Dim
- **Luôn đủ kích thước của khách**: mọi kích thước khách ghi trên bản vẽ còn đúng trên hình trải phẳng (`dim_khach`, AI đọc lúc đọc bản vẽ) phải có trên bản vẽ OHTA, cùng số. `ve.py` so với các dim đã vẽ: thiếu dim ngang / dọc → tự thêm một hàng dim (sau chuỗi dim, trước dim tổng); thiếu R → dim R chỉ khi bản vẽ có đúng cung R đó, không có thì báo **lệch** (không dim sai); còn lại (Ø lỗ, góc…) báo "còn thiếu dim của khách" để bổ sung trên CAD.
- **Dim tổng** X (dưới cùng) và Y (bên phải): layer **`1-XY`**, kiểu `ISO-26` — `ve.py` tự thêm.
- **Chuỗi dim chi tiết**: layer `1-SUNPOU`, giữa tấm và dim tổng — chuỗi ngang **phía dưới**, chuỗi dọc **bên phải**. Mốc: mép tấm, đường bẻ, dấu ケガキ, tâm cung có ghi R, mốc mẫu thêm (`nen.moc_x`, `nen.moc_y`). `ve.py` tự dựng.
- Khoảng cách: viền → chuỗi dim 80 (`LE_DIM`), mỗi hàng dim sau cách thêm 44 (`BUOC_DIM`); dim tổng luôn ở hàng ngoài cùng (có chuỗi dim: 124).
- **Cung cần ghi R**: `nen.cung(..., dim=True)` — dim R (kiểu `ISO-26`, CAD tự dùng `ISO-26$4`), chữ ra ngoài phía trên-phải, tâm cung vào chuỗi dim. Nhiều cung cùng R chỉ ghi một lần.
- Đầu tấm là cung bán nguyệt: 2 dim nửa chiều cao (mép → tâm cung → mép) phía đầu đó, cách viền `LE_DIM`, chữ đưa ra ngoài (dưới / trên) bằng `vi_tri_chu`.
- Không đặt dim chi tiết phía trên tấm.

## Dấu khắc vạch ケガキ — layer `3-KEGAKI`
- Mỗi dấu: chữ thập 10 mm (2 LINE) tại tâm dấu (`ke_gaki` của bảng tấm, toạ độ trên hình trải phẳng, gốc góc trái dưới).
- Một leader `1-SUNPOU` từ dấu giữa chéo lên trên-phải (≈ +23, cao hơn mép trên 53), chữ **`n-ケガキ`**. Bản khách ghi `マーキング` / `罫書き` vẫn ghi `ケガキ`.

## F-name — layer `2-F-NAME`
- TEXT cao 24, kiểu chữ `Text` (MS PGothic), dòng cách 40; đặt **dưới tấm**, cách mép trái 100, dòng đầu dưới đường dim tổng X ≈ 98.
- `N=` (số bản vẽ / tên; không có thì bango tạm `<mã>-<số>`), `M=`, `T=` (luôn có phần thập phân: 12.0, 1.6), `Q=`, các dòng `V=` / ấn 1 lần, `Z=`.

## Khung_IN
- LWPOLYLINE kín 4 đỉnh, bao hết tấm + dim + ケガキ + F-name, lề tối thiểu 30, nới đều cho đủ tỉ lệ **khổ A nằm ngang (√2 : 1)**.
