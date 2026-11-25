---
name: mo-ta-dxf-anh
description: >
  Viết đoạn "Thông tin bổ sung" cho tab DXF ảnh / Thử nhanh từ ảnh bản vẽ khách (fax, PDF, ảnh chụp)
  để AI vẽ đúng. Dùng khi người dùng gửi ảnh một tấm và xin mô tả / ghi chú / thông tin bổ sung
  cho AI vẽ lại, hoặc hỏi đơn này cần ghi gì để AI đọc đúng.
---

# Mô tả bản vẽ cho tab DXF ảnh

Đầu ra là một khối chữ ngắn người dùng dán vào ô **Thông tin bổ sung** (tab DXF ảnh trong CAD, hoặc ô ghi chú cửa sổ Thử nhanh của OhtaTeams). Khối **chỉ** gồm các thông tin chưa rõ trên ảnh — đã được người dùng xác nhận, hoặc còn phải hỏi khách — để AI vẽ không **đoán mò**. Phần ảnh đã rõ AI tự đọc, không chép lại. Ghi chú thắng hình: khác hình thì AI báo "⚠ Ghi chú khác hình".

## Các bước

### 1. Kê mọi chi tiết trên ảnh
Liệt kê (cho riêng mình) từng chi tiết của tấm: viền ngoài, lỗ, khoét trong, vát / bo góc, dấu khắc ケガキ / マーキング, スリット, đường bẻ, chữ ghi chú, số lượng, vật liệu. Với mỗi chi tiết ghi số khách viết tay / in trên hình.
Xong khi: mọi nét và mọi chữ trên ảnh đều thuộc một chi tiết trong danh sách, hoặc được xếp là ghi chú đơn (giá, hạn giao, người gửi…).

### 2. Xếp mỗi chi tiết vào một trong ba loại
- **chắc**: khách ghi số đủ để vẽ.
- **tính được**: suy ra bằng hình học từ số khách (vd toạ độ 3 dấu cách đều 120° từ bán kính và góc). Tự tính, ghi kết quả.
- **không rõ**: thiếu số, chữ nhoè đọc hai cách, nét không rõ là cắt hay khắc, hai số mâu thuẫn, vị trí chỉ có mũi tên chỉ chung chung, hình không theo tỉ lệ nên không đo được.

Đọc hình theo số, không theo tỉ lệ — bản vẽ tay hầu như không đúng tỉ lệ.

### 3. Hỏi người dùng mọi chỗ **không rõ** — trước khi viết khối
Dùng công cụ hỏi (AskUserQuestion, ≤ 4 câu mỗi lần, hỏi tiếp nếu còn). Mỗi câu nêu đúng chỗ mơ hồ + cách đọc mình nghiêng về (lựa chọn đầu, ghi "(Đề xuất)") + các cách đọc khác; luôn kèm lựa chọn **"Chưa rõ — hỏi khách"**.
Xong khi: mỗi chi tiết **không rõ** đã được xác nhận, hoặc người dùng chọn hỏi khách.

### 4. Viết khối mô tả — chỉ các chỗ chưa rõ
Mỗi dòng một chỗ chưa rõ, gọn như ghi chú tay. Chi tiết **chắc** (in rõ, số rõ) → bỏ, không ghi. Hai loại dòng:
- **Đã xác nhận**: `<chi tiết>: <giá trị đúng>` — số nhoè / đọc hai cách, nét cắt hay khắc, vị trí chỉ có mũi tên, nghĩa thuật ngữ dễ hiểu sai (vd "中抜きなし = chỉ khắc, không cắt"), chữ trên hình KHÔNG phải thông số ("bỏ qua số 90 — viết nhầm"). Chi tiết **tính được** mà AI dễ tính sai (dấu cách đều quanh tâm, đỉnh qua dim xiên…) → ghi luôn giá trị ô (mục *Ô dữ liệu*) để AI chép.
- **Dim AI hay bỏ sót**: dim góc giữa các dấu / lỗ quanh tâm (vd 120°), dim Ø của tấm tròn → `Dim khách cần có: <loại số, chỗ đặt>` (dim góc ghi cả 3 điểm: điểm 1, đỉnh góc, điểm 2).
- **Chưa rõ — hỏi khách**: `CHƯA RÕ <chi tiết>: <điều chưa biết> — không đoán, ghi chua_chac + hỏi khách`. AI vẽ chỗ hợp lý nhất, phần đó tô VÀNG và vào danh sách "Cần xác nhận" — không lặng lẽ chọn một số.
Dạng khối code để người dùng chép nguyên.

### 5. Tự kiểm khối trước khi trả
- Khung bao: đỉnh viền bắt đầu từ (0, 0), rộng × cao khớp kích thước tổng.
- Mọi lỗ, dấu, slit nằm trong tấm; đầu slit nằm đúng trên mép (vd vành: một đầu trên vòng ngoài, một đầu trên vòng trong).
- Số trong khối khớp số trên hình và câu trả lời của người dùng.
Rồi trả khối + một dòng nói phần nào hệ thống chưa vẽ được (mục *Giới hạn*) nếu có.

## Ô dữ liệu (toạ độ mm trên hình trải phẳng, gốc = góc trái dưới khung bao)

- **Biên dạng**: chữ nhật → mẫu `4canh`. Mọi tấm phẳng khác → mẫu `da_giac`, `dinh = [[x, y] | [x, y, R]…]` đi một vòng; R ở đỉnh i = cạnh i → i+1 là cung (R > 0 lồi, R < 0 lõm). Tấm tròn / vành Øn: `dinh = [[0, n/2, n/2], [n, n/2, n/2]]`.
- **Lỗ** `lo`: Ø + tâm từng lỗ `[x, y]`; lỗ giữa vành tròn cũng là lỗ.
- **Khoét trong** `khoet`: `□rộngxcao R<bo> G<góc xoay> @ x,y`.
- **Góc** `goc`: `trai_duoi C10; phai_tren R5` (đỉnh trước khi vát / bo).
- **ケガキ** `ke_gaki`: `[x, y]` = chữ thập 10 mm tại tâm; `[x, y, đáy, hướng, cao]` = tam giác / chữ V: (x, y) = **mũi** (chỗ khách dim tới), `hướng` = hướng mũi chỉ (0 phải, 90 lên, 180 trái, 270 xuống), `cao` = mũi → đáy (0 = tam giác đều). Đáy nằm trên mép tấm → `cao` tính để 2 góc đáy chạm mép (vành bán kính R, mũi cách tâm r: cao = √(R² − (đáy/2)²) − r); khi đó tool vẽ chữ **V** 2 nét như bản xưởng. Dấu cách đều quanh tâm tròn: toạ độ mũi VÀ hướng đều quay theo từng dấu (mũi chỉ vào tâm → hướng = góc từ mũi về tâm).
- **スリット** `slit`: `[x1, y1, x2, y2]` — một nét cắt đơn (layer 4-SHT), vd cắt đứt vành theo bán kính.
- **Dim**: kê các dim khách có (tổng X / Y, Ø, khoảng tâm → dấu…) — bản vẽ OHTA chép đúng các dim đó, chỉ thêm tổng X / Y nếu thiếu.

## Thuật ngữ đơn Nhật hay gặp

マーキング / ケガキ / 罫書き = dấu khắc (không cắt) · 中抜きなし = không cắt thủng phần giữa · スリット有 = có slit (nét cắt) · n ヶ所 = n chỗ · PL t = tấm dày t · 数量 / 枚 = số lượng · 〜ほど = khoảng (chưa chốt → hỏi) · 単価 = đơn giá · 納 = hạn giao · 面取り = vát mép chiều dày (ghi chú gia công, không phải ô goc).

## Giới hạn hiện tại (nói với người dùng khi gặp)

- Tấm bẻ / cuộn: tab DXF ảnh mới chắc với tấm đơn (phẳng).
