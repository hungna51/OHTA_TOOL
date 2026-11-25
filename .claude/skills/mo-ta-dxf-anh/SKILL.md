---
name: mo-ta-dxf-anh
description: >
  Viết đoạn "Thông tin bổ sung" cho tab DXF ảnh / Thử nhanh từ ảnh bản vẽ khách (fax, PDF, ảnh chụp)
  để AI vẽ đúng. Dùng khi người dùng gửi ảnh một tấm và xin mô tả / ghi chú / thông tin bổ sung
  cho AI vẽ lại, hoặc hỏi đơn này cần ghi gì để AI đọc đúng.
---

# Mô tả bản vẽ cho tab DXF ảnh

Đầu ra là một khối chữ người dùng dán vào ô **Thông tin bổ sung** (tab DXF ảnh trong CAD, hoặc ô ghi chú cửa sổ Thử nhanh của OhtaTeams). AI đọc ảnh làm theo khối này — **ghi chú thắng hình**, khác hình thì AI báo "⚠ Ghi chú khác hình". Nên mọi số trong khối phải là số đúng: chỗ còn mơ hồ phải hỏi người dùng **trước** khi viết khối.

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
Dùng công cụ hỏi (AskUserQuestion, ≤ 4 câu mỗi lần, hỏi tiếp nếu còn). Mỗi câu nêu đúng chỗ mơ hồ + cách đọc mình nghiêng về (lựa chọn đầu, ghi "(Đề xuất)") + các cách đọc khác; luôn kèm lựa chọn **"Chưa rõ — để AI tự suy (tô vàng)"**.
Xong khi: mỗi chi tiết **không rõ** đã có câu trả lời, hoặc người dùng chọn để AI tự suy.

### 4. Viết khối mô tả — NGẮN, chỉ phần AI dễ sai
AI đọc được ảnh, nên khối chỉ ghi chỗ **khó đọc, đặc biệt, hoặc vừa được người dùng trả lời**. Chi tiết in rõ, số rõ ràng (Ø ngoài, kích thước tổng, lỗ thường có dim đủ) → bỏ, không ghi. Mỗi dòng một ý, gọn như ghi chú tay.
Dòng đáng ghi:
- Số nhoè / đọc được hai cách → số đúng.
- Chi tiết phải **tính** (dấu cách đều quanh tâm, đỉnh qua dim xiên…) → ghi luôn giá trị ô (mục *Ô dữ liệu*) để AI chép, không tự tính.
- Thuật ngữ dễ hiểu sai → nghĩa ngắn (vd "中抜きなし = chỉ khắc, không cắt").
- Câu trả lời của người dùng ở bước 3.
- Chỗ để AI tự suy → "chưa rõ <cái gì> → tự suy, ghi chua_chac" (tô vàng).
- Chữ trên hình dễ bị hiểu là thông số mà không phải (số lượng khoảng, giá, hạn giao, nét người dùng viết nhầm) → "bỏ qua …".
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
- **ケガキ** `ke_gaki`: `[x, y]` = chữ thập 10 mm tại tâm; `[x, y, cạnh, hướng]` = tam giác ĐỀU, (x, y) = **mũi** (chỗ khách dim tới), `hướng` = hướng mũi chỉ (0 phải, 90 lên, 180 trái, 270 xuống). Dấu cách đều quanh tâm tròn: toạ độ mũi VÀ hướng đều quay theo từng dấu (mũi chỉ vào tâm → hướng = góc từ mũi về tâm).
- **スリット** `slit`: `[x1, y1, x2, y2]` — một nét cắt đơn (layer 4-SHT), vd cắt đứt vành theo bán kính.
- **Dim**: kê các dim khách có (tổng X / Y, Ø, khoảng tâm → dấu…) — bản vẽ OHTA chép đúng các dim đó, chỉ thêm tổng X / Y nếu thiếu.

## Thuật ngữ đơn Nhật hay gặp

マーキング / ケガキ / 罫書き = dấu khắc (không cắt) · 中抜きなし = không cắt thủng phần giữa · スリット有 = có slit (nét cắt) · n ヶ所 = n chỗ · PL t = tấm dày t · 数量 / 枚 = số lượng · 〜ほど = khoảng (chưa chốt → hỏi) · 単価 = đơn giá · 納 = hạn giao · 面取り = vát mép chiều dày (ghi chú gia công, không phải ô goc).

## Giới hạn hiện tại (nói với người dùng khi gặp)

- ケガキ tam giác chỉ vẽ được tam giác **đều**. Dấu tam giác cân (vd đáy 30 nằm trên vòng ngoài, mũi ở 95.5 → cao ≠ 0.866 × đáy): ghi đúng ý trong khối mô tả, chọn cạnh = bề rộng đáy, và báo người dùng 2 góc đáy có thể lệch khỏi mép ~1 mm, cần chỉnh tay trên CAD.
- Tấm bẻ / cuộn: tab DXF ảnh mới chắc với tấm đơn (phẳng).
