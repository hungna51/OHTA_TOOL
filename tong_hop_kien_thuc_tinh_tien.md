# Cách công ty tính tiền một tấm kim loại gia công

> Tài liệu giải thích cho người đọc bình thường: một tấm kim loại cắt laser (và có thể bẻ, khoan ren, cuộn…) được tính giá như thế nào.
>
> Đơn vị tiền: **Yên Nhật (¥)**. Kích thước: **mm**. Khối lượng: **kg**.
>
> **Về số liệu**: đơn giá thật (giá thép, giá inox, giá bẻ…) thay đổi theo thời gian và theo bảng giá của công ty, nên không ghi trong tài liệu này. Các con số trong phần ví dụ là **số minh hoạ**, chỉ để hiểu cách tính. Những mức cố định như 300 ¥, 1 500 ¥, 6 000 ¥, 12 000 ¥, 250 ¥… là quy tắc thật đang áp dụng.

---

## 1. Tóm tắt trong một phút

Giá một tấm = **tiền vật liệu** + **tiền cắt** + **tiền gia công**, sau đó áp vài quy tắc điều chỉnh:

1. **Tiền vật liệu**: cân nặng tấm phôi (mỗi chiều cộng thêm 10 mm) × đơn giá vật liệu theo kg × hệ số (khách ưu đãi thì hệ số thấp hơn).
2. **Tiền cắt**: theo độ dày và chu vi tấm.
3. **Tiền gia công**: lỗ, taro ren (Tapu), khoét loe (Sara), bẻ, bẻ bước (pitchi), cuộn, vát mép.
4. **Điều chỉnh**:
   - Nếu tiền vật liệu chiếm quá nhiều trong giá (trên 55 % với thép, trên 68 % với inox), giá được nâng lên.
   - Làm tròn tới 10 ¥.
   - Một số khách đặc biệt được tính theo **đơn giá mỗi kg**.
   - Khách tự mang vật liệu hoặc chỉ nhờ gia công thì chỉ thu **15 % tiền vật liệu**.
   - Có thể đặt **giá tối thiểu 250 ¥ / tấm**.
5. **Thành tiền** = giá 1 tấm × số lượng. **Tổng đơn** = cộng thành tiền của mọi tấm.

---

## 2. Từ ngữ cần biết

| Từ | Nghĩa |
|---|---|
| **Tấm** | Một chi tiết kim loại phẳng được cắt ra từ tấm nguyên liệu, có thể gia công thêm. Mỗi tấm có tên, vật liệu, độ dày, kích thước, số lượng. |
| **Vật liệu** | Loại kim loại: thép thường (SS400, SPCC, SPHC…), inox (SUS304…), nhôm (A5052…). |
| **Độ dày (T)** | Bề dày tấm, tính bằng mm. |
| **X, Y** | Chiều dài và chiều rộng hình chữ nhật bao quanh tấm (X là cạnh lớn). |
| **Số lượng (Q)** | Số tấm giống nhau cần làm. |
| **Phôi** | Phần nguyên liệu dùng để cắt ra tấm. Khi tính tiền, phôi được tính lớn hơn tấm **10 mm mỗi chiều** (phần hao hụt khi cắt). |
| **Tỉ trọng** | Khối lượng riêng của vật liệu (thép ≈ 7.85). Dùng để đổi kích thước thành kg. |
| **Tấm đơn** | Tấm chỉ cắt (có thể có lỗ), không gia công gì thêm. |
| **Tấm gia công** (tấm bẻ) | Tấm có ít nhất một trong: bẻ, bẻ bước, Tapu, Sara, cuộn, vát. **Lỗ không tính** là gia công. |
| **Lỗ nhỏ / lỗ lớn** | Lỗ đường kính đến 30 mm là lỗ nhỏ, lớn hơn là lỗ lớn. |
| **Bẻ** (曲げ) | Chấn / uốn tấm thành góc theo một đường thẳng. |
| **Nhấn 1 lần** (発押し) | Kiểu bẻ ép một nhát; mỗi đường tính tiền **gấp đôi** đường bẻ thường. |
| **Pitchi** (bẻ bước, 曲げ角度) | Bẻ nhiều nhát nhỏ liên tiếp để tạo thành cung tròn. Có nhiều kiểu: Pitchi, Non, Non lệch, Kakumaru, Kakumaru lệch. |
| **Cuộn** | Lốc tấm thành cung tròn (**nửa hình**) hoặc thành ống tròn kín (**cả hình**). |
| **Tapu** (タップ) | Taro ren trong lỗ để bắt vít (M3, M5, M12…). |
| **Sara** (皿モミ) | Khoét loe miệng lỗ để đầu vít chìm vào mặt tấm. |
| **Vát** (開先) | Vát mép tấm để hàn; tính theo **chiều dài mép vát**. |
| **Khách ưu đãi** | Khách hàng được hưởng hệ số giá vật liệu thấp hơn. |
| **Khách đặc biệt** | Khách được tính giá theo **¥/kg** (một giá cho tấm đơn, một giá cho tấm gia công). |
| **Giá 1 tấm** (đơn giá, 単価) | Giá của một tấm sau khi làm tròn. |
| **Thành tiền** | Giá 1 tấm × số lượng. |

---

## 3. Bảng giá: công ty cần những con số nào?

Với **mỗi loại vật liệu và mỗi độ dày**, bảng giá của công ty có:

| Mục | Ý nghĩa |
|---|---|
| Đơn giá vật liệu | ¥ cho mỗi kg vật liệu |
| Giá bẻ | ¥ cho mỗi mét đường bẻ |
| Giá cắt | Hệ số tiền cắt laser |
| Giá mở lỗ | ¥ cho mỗi lỗ |
| Giá Tapu | ¥ cho mỗi lỗ taro (Sara cũng dùng giá này) |
| Giá vát | ¥ cho mỗi mét vát |
| Tỉ trọng | Để đổi kích thước ra kg |

Ngoài ra còn có:
- **Danh sách khách hàng**: khách nào được ưu đãi.
- **Danh sách khách đặc biệt**: đơn giá ¥/kg cho tấm đơn và cho tấm gia công, và có so với giá thường hay không.
- **Hệ số giá vật liệu**: 4 mức — Ưu đãi thép, Ưu đãi inox, Không ưu đãi thép, Không ưu đãi inox.
- **Hệ số cho pitchi** (theo cân nặng, theo chiều dài, theo kiểu pitchi) và **hệ số cuộn** (theo cân nặng).

Một vật liệu + độ dày **chưa có trong bảng giá**, hoặc có nhưng đơn giá vật liệu bằng 0, thì **chưa báo giá được** cho tới khi bổ sung giá. Có thể đặt **giá vật liệu tạm** riêng cho một đơn hàng (chỉ thay đơn giá vật liệu; các giá bẻ, cắt, lỗ… vẫn theo bảng giá chung). Ngoại lệ: khách tính theo kg mà không so với giá thường thì không cần giá vật liệu.

---

## 4. Cách tính chi tiết từng bước

### Bước 1 — Cân nặng phôi
```
Cân phôi (kg) = (X + 10) × (Y + 10) × T × tỉ trọng ÷ 1 000 000
```

### Bước 2 — Tiền vật liệu
```
Tiền vật liệu = Cân phôi × Đơn giá vật liệu × Hệ số
```
- Hệ số chọn theo 2 câu hỏi: khách có **ưu đãi** không? Vật liệu là **inox** (SUS) hay không?
- Thép thường và nhôm đều dùng hệ số nhóm "thép".
- Khách không có trong danh sách → tính như khách không ưu đãi.

### Bước 3 — Tiền cắt laser
```
Tiền cắt = Giá cắt × T × (X + Y) ÷ 1000 × 2
```
Nghĩa là: độ dày × chu vi hình chữ nhật bao quanh tấm (tính bằng mét) × giá cắt.

### Bước 4 — Tiền lỗ, Tapu, Sara
```
Tiền lỗ  = (số lỗ nhỏ + số lỗ lớn × 1.5) × Giá mở lỗ
Tiền Tapu = số lỗ Tapu × Giá Tapu
Tiền Sara = số lỗ Sara × Giá Tapu
```
Một lỗ lớn tính bằng 1.5 lỗ nhỏ.

### Bước 5 — Tiền bẻ
Mỗi đường bẻ:
```
Tiền 1 đường = Dài (m) × Giá bẻ × Hệ số,  nhưng ít nhất 300 ¥
```
Hệ số = số **lớn hơn** trong hai hệ số sau:

| Cân phôi | Hệ số theo cân |
|---|---|
| dưới 40 kg | 1 |
| 40 đến dưới 100 kg | 1.5 |
| từ 100 kg | 2 |

| Chiều dài đường bẻ | Hệ số theo chiều dài |
|---|---|
| dưới 3 m | 1 |
| 3 đến dưới 4 m | 2.5 |
| 4 đến dưới 5 m | 3 |
| 5 đến dưới 6 m | 3.5 |
| từ 6 m | 4.5 |

Tấm to, nặng hoặc đường bẻ dài thì khó bẻ hơn nên đắt hơn. Đường **nhấn 1 lần** tính như 2 đường.

### Bước 6 — Tiền pitchi (bẻ bước)
Pitchi tính theo **thời gian gia công (phút)**:
```
Phút = Hệ số lớn hơn (theo cân, theo chiều dài) × Hệ số kiểu pitchi × 2 × Số lần pitchi
Tiền pitchi = Phí chuẩn bị chia cho số lượng + 100 ¥ × Phút
```
- **Phí chuẩn bị 1 500 ¥** cho cả lô, chia đều cho số tấm, nhưng chỉ chia tối đa cho 5 → mỗi tấm chịu ít nhất 300 ¥.
- Khi tính cân cho pitchi luôn dùng tỉ trọng thép 7.85.

Hệ số theo cân (giá trị thường dùng): dưới 20 kg: 0.8 · 20–40 kg: 1 · 40–100 kg: 1.5 · từ 100 kg: 2.

Hệ số theo chiều dài pitchi (giá trị thường dùng): dưới 0.83 m: 0.75 · 0.83–1.66: 1 · 1.66–2.49: 1.25 · 2.49–3.32: 1.5 · 3.32–4.15: 1.75 · 4.15–4.98: 2 · 4.98–5.81: 2.25 · từ 5.81 m: 2.5.

Kiểu pitchi (Pitchi, Non, Non lệch, Kakumaru, Kakumaru lệch) có hệ số riêng; kiểu khó hơn có hệ số cao hơn.

### Bước 7 — Tiền cuộn
```
Tiền cuộn = Hệ số theo cân × Giá cuộn
```
- **Cuộn nửa hình** (cung tròn): **6 000 ¥**.
- **Cuộn cả hình** (ống tròn kín): **12 000 ¥**.

Hệ số theo cân (tỉ trọng 7.85; giá trị thường dùng): dưới 30 kg: 0.4 · 30–50: 0.5 · 50–100: 0.75 · 100–200: 1 · 200–400: 1.5 · từ 400 kg: 2.

### Bước 8 — Tiền vát
```
Tiền vát = Chiều dài vát (m) × Giá vát
```
Chưa có giá vát trong bảng giá thì dùng **1 800 ¥ / m**.

### Bước 9 — Cộng lại
```
Tiền gia công = lỗ + Tapu + Sara + bẻ + pitchi + cuộn + vát
Tổng          = Tiền vật liệu + Tiền cắt + Tiền gia công
```

### Bước 10 — Quy tắc "vật liệu không quá nửa giá"
So **tiền vật liệu gốc** (chưa nhân hệ số) với tổng ở bước 9:
- **Thép, nhôm…**: nếu vật liệu gốc chiếm **trên 55 %** tổng → giá = vật liệu gốc ÷ 0.55.
- **Inox**: nếu chiếm **trên 68 %** → giá = vật liệu gốc ÷ 0.68.
- Ngược lại giữ nguyên tổng.

Mục đích: tấm to mà ít gia công vẫn có lãi hợp lý.

### Bước 11 — Làm tròn
Làm tròn tới **10 ¥** gần nhất (5 thì làm tròn lên). Đây là **giá 1 tấm**.

### Bước 12 — Khách đặc biệt tính theo kg
Áp dụng khi: chế độ tính **Thường**, vật liệu **không phải inox**, khách có tên trong danh sách khách đặc biệt.
```
Cân tính theo kg = (X + 6) × (Y + 6) × T × tỉ trọng ÷ 1 000 000     (chỉ cộng 6 mm mỗi chiều)
Giá 1 tấm        = Cân × đơn giá ¥/kg   (làm tròn 10 ¥)
```
- Tấm đơn dùng giá ¥/kg cho tấm đơn; tấm gia công dùng giá ¥/kg cho tấm gia công.
- Nếu khách được **so với giá thường** → lấy giá **thấp hơn** giữa giá theo kg và giá thường.
- Tấm inox của khách đặc biệt vẫn tính giá thường.

### Bước 13 — Ba chế độ tính

| Chế độ | Khi nào | Cách tính |
|---|---|---|
| **Thường** | Công ty cấp vật liệu, cắt và gia công | Như bước 1–12 |
| **Vật liệu khách cấp** | Khách đưa vật liệu, công ty vẫn cắt và gia công | 15 % tiền vật liệu + tiền cắt + tiền gia công |
| **Chỉ gia công** | Khách đưa tấm đã cắt sẵn, công ty chỉ bẻ / taro / cuộn… | 15 % tiền vật liệu + tiền gia công, **không tính tiền cắt và tiền lỗ** (lỗ là một kiểu cắt) |

15 % tiền vật liệu là **phí quản lý, xử lý vật liệu**. Hai chế độ này không áp quy tắc ở bước 10 và không áp giá theo kg.

### Bước 14 — Tự nhập tiền vật liệu
Nếu tự tính được tiền vật liệu của 1 tấm (ví dụ vật liệu mua riêng), có thể nhập thẳng số đó:
- Chế độ Thường và Vật liệu khách cấp: tổng = số nhập + tiền cắt + tiền gia công, rồi **vẫn áp quy tắc 55 % / 68 %** với số nhập là "vật liệu gốc": số nhập chiếm trên 55 % tổng (thép) / 68 % (inox) thì giá = số nhập ÷ 0.55 / ÷ 0.68.
- Chế độ Chỉ gia công: giá = số nhập + tiền gia công (không cắt, không lỗ, không áp quy tắc 55 % / 68 %).
- Không nhân hệ số vật liệu, không áp giá theo kg.

### Bước 15 — Giá tối thiểu 250 ¥
Khi bật, tấm nào có giá dưới 250 ¥ được tính **250 ¥**. Có thể bật cho cả đơn, hoặc bật / tắt riêng từng tấm.

### Bước 16 — Thành tiền và tổng đơn
```
Thành tiền = Giá 1 tấm × Số lượng
Tổng đơn   = Cộng thành tiền của mọi tấm
```

### Thứ tự áp dụng
```
Cân phôi → Tiền vật liệu → Tiền cắt → Lỗ / Tapu / Sara → Bẻ → Pitchi → Cuộn → Vát
→ Cộng → Quy tắc 55 % / 68 % → Làm tròn 10 ¥
→ (Khách đặc biệt: giá theo kg)
→ (Chế độ Chỉ gia công / Tự nhập tiền vật liệu / Vật liệu khách cấp)
→ Giá tối thiểu 250 ¥ (nếu bật) → × Số lượng
```

---

## 5. Ví dụ tính (số minh hoạ, không phải giá thật)

Bảng giá minh hoạ:

| Vật liệu | T | Giá VL ¥/kg | Giá bẻ ¥/m | Giá cắt | Giá lỗ | Giá Tapu | Giá vát ¥/m | Tỉ trọng |
|---|---|---|---|---|---|---|---|---|
| Thép SS400 | 6 | 140 | 900 | 7 | 45 | 150 | 1700 | 7.85 |
| Thép SS400 | 12 | (chưa có) | 1400 | 9 | 80 | 200 | — | 7.85 |
| Inox SUS304 | 3 | 620 | 1100 | 12 | 60 | 180 | 2100 | 7.93 |
| Inox SUS304 | 9 | 700 | 1800 | 20 | 140 | 260 | 2600 | 7.93 |

Hệ số minh hoạ: Ưu đãi thép 1.15 · Ưu đãi inox 1.05 · Không ưu đãi thép 1.2 · Không ưu đãi inox 1.25.

### Ví dụ 1 — Tấm inox có bẻ, khách không ưu đãi
Inox SUS304 dày 9, X = 86.8, Y = 1076, 3 tấm, 10 lỗ nhỏ, 2 lỗ Tapu, bẻ 3 đường dài 2999 mm và 1 đường dài 718 mm.
- Cân phôi = 96.8 × 1086 × 9 × 7.93 ÷ 1 000 000 = **7.50 kg**
- Tiền vật liệu = 7.50 × 700 × 1.25 = **6 565 ¥**
- Tiền cắt = 20 × 9 × 1.1628 × 2 = **419 ¥**
- Lỗ = 10 × 140 = **1 400 ¥**; Tapu = 2 × 260 = **520 ¥**
- Bẻ: cân dưới 40 kg và đường dưới 3 m → hệ số 1.
  Đường 2999 mm: 2.999 × 1800 = 5 398 ¥ × 3 đường = 16 195 ¥. Đường 718 mm: 1 292 ¥. Tổng **17 487 ¥**
- Tổng = 6 565 + 419 + 1 400 + 520 + 17 487 = 26 390.5 ¥. Vật liệu gốc chỉ chiếm 20 % → giữ nguyên.
- **Giá 1 tấm = 26 390 ¥ · Thành tiền = 79 170 ¥**

### Ví dụ 2 — Vật liệu khách cấp, có lỗ, Sara và cuộn nửa hình, khách ưu đãi
Thép SS400 dày 6, X = 575, Y = 1219, 3 tấm, 2 lỗ nhỏ, 3 lỗ Sara, cuộn nửa hình.
- Cân phôi = 585 × 1229 × 6 × 7.85 ÷ 1 000 000 = **33.86 kg**
- Tiền vật liệu = 33.86 × 140 × 1.15 = 5 452 ¥ → khách cấp vật liệu nên chỉ thu 15 % = **818 ¥**
- Tiền cắt = 7 × 6 × 1.794 × 2 = **151 ¥**
- Lỗ = 2 × 45 = **90 ¥**; Sara = 3 × 150 = **450 ¥**
- Cuộn: cân 30–50 kg → hệ số 0.5 → 0.5 × 6 000 = **3 000 ¥**
- Tổng = 818 + 151 + 90 + 450 + 3 000 = 4 508.5 ¥
- **Giá 1 tấm = 4 510 ¥ · Thành tiền = 13 530 ¥**

### Ví dụ 3 — Chỉ gia công: pitchi và cuộn
Inox SUS304 dày 3, X = 575, Y = 193, 1 tấm, pitchi 3 lần dài 100 mm (kiểu Pitchi, hệ số 1), cuộn nửa hình, khách ưu đãi.
- Tiền vật liệu = 2.83 kg × 620 × 1.05 = 1 839 ¥ → chỉ thu 15 % = **276 ¥**
- Pitchi: cân dưới 20 kg → 0.8; dài 0.1 m → 0.75; lấy 0.8. Phút = 0.8 × 1 × 2 × 3 = 4.8.
  Tiền = 1 500 ÷ 1 + 100 × 4.8 = **1 980 ¥**
- Cuộn: cân dưới 30 kg → 0.4 × 6 000 = **2 400 ¥**
- Không tính cắt, không tính lỗ.
- **Giá 1 tấm = 276 + 1 980 + 2 400 = 4 656 → 4 660 ¥**

### Ví dụ 4 — Khách đặc biệt tính theo kg
Thép SS400 dày 12, X = 301.1, Y = 1219, 2 tấm, có 1 đường bẻ. Khách đặc biệt: tấm gia công 330 ¥/kg, không so với giá thường.
- Có bẻ → tấm gia công → 330 ¥/kg
- Cân = 307.1 × 1225 × 12 × 7.85 ÷ 1 000 000 = **35.44 kg**
- 330 × 35.44 = 11 694.5 → **Giá 1 tấm = 11 690 ¥ · Thành tiền = 23 380 ¥**
- Dù vật liệu này chưa có đơn giá trong bảng, khách theo kg vẫn báo giá được.

### Ví dụ 5 — Tấm phẳng to: quy tắc 55 %
Thép SS400 dày 6, 1000 × 1000, 1 tấm, không gia công, khách ưu đãi.
- Cân phôi = 1010 × 1010 × 6 × 7.85 ÷ 1 000 000 = 48.05 kg
- Tiền vật liệu = 48.05 × 140 × 1.15 = 7 736 ¥; tiền cắt = 168 ¥; tổng = 7 904 ¥
- Vật liệu gốc = 48.05 × 140 = 6 727 ¥, chiếm 85 % tổng → vượt 55 %
- Giá = 6 727 ÷ 0.55 = 12 230 ¥ → **Giá 1 tấm = 12 230 ¥**

### Ví dụ 6 — Tự nhập tiền vật liệu
Thép SS400 dày 6, 100 × 100, 1 tấm, nhập tiền vật liệu 1 000 ¥.
- Tiền cắt = 7 × 6 × 0.2 × 2 = 16.8 ¥ → tổng 1 016.8 ¥
- Vật liệu nhập chiếm 1 000 ÷ 1 016.8 ≈ 98 % → vượt 55 %
- Giá = 1 000 ÷ 0.55 = 1 818 ¥ → **Giá 1 tấm = 1 820 ¥**
- Nếu chỉ nhập 10 ¥: tổng 26.8 ¥, vật liệu chiếm 37 % → không vượt → **30 ¥**
- Nếu là chế độ Chỉ gia công: không cắt, không áp quy tắc → **1 000 ¥**

---

## 6. Tấm nào là tấm gia công? Đếm gia công thế nào?

- **Lỗ**: đếm các lỗ cắt trong tấm; đường kính đến 30 mm là lỗ nhỏ.
- **Bẻ**: gom các đường bẻ theo chiều dài; cùng chiều dài thì cộng số đường. Đường nhấn 1 lần (発押し) tính gấp đôi.
- **Tapu**: đọc ghi chú trên bản vẽ. Ví dụ `4-M5タップ` = 4 lỗ; `5×2-M12タップ` = 10 lỗ; ghi `M8タップ 3箇所` = 3 lỗ; không ghi số = 1 lỗ.
- **Sara**: ghi chú `皿モミ`, `皿もみ`, `皿加工`… Ví dụ `2x2-∅8孔皿モミ` = 4 lỗ; `4-φ8皿もみ` = 4 lỗ. `皿小ネジ` là tên vít, **không** tính.
- **Cuộn**: một phần vòng tròn = nửa hình; cả vòng = cả hình.
- **Pitchi** và **vát**: phải ghi rõ số lần pitchi, chiều dài pitchi, chiều dài vát; thiếu thì chưa báo giá đúng được.

---

## 7. Giá sửa tay

- Người báo giá có thể **sửa tay** giá của một tấm. Giá sửa tay được giữ nguyên, không bị tính lại khi thay đổi khách, chế độ hay bảng giá.
- Có thể bỏ giá sửa tay để quay về giá tính theo quy tắc.

---

## 8. Tổng cân của đơn

Tổng cân giúp nhìn nhanh quy mô đơn hàng:
- Tách **tấm đơn** và **tấm gia công**, nhân số lượng.
- Tấm tính theo kg của khách đặc biệt dùng cân (X+6)(Y+6); các tấm khác dùng cân phôi (X+10)(Y+10).
- Với khách đặc biệt, có thêm dòng: cân tấm đơn × giá ¥/kg, cân tấm gia công × giá ¥/kg, và số tấm vẫn tính giá thường (tấm inox, tấm sửa tay, hoặc tấm có giá thường rẻ hơn).

---

## 9. Những điều dễ hiểu nhầm

1. **"Ưu đãi" là ưu đãi của khách**, quyết định hệ số giá vật liệu — không phải phần trăm giảm giá trên hoá đơn.
2. **Lỗ không làm tấm thành tấm gia công**, nhưng vẫn mất tiền mở lỗ.
3. **Nhôm** tính hệ số giống thép thường.
4. **Pitchi và cuộn** luôn tính cân với tỉ trọng 7.85, kể cả inox hay nhôm.
5. **Làm tròn tới 10 ¥ gần nhất**, không phải luôn làm tròn lên.
6. **Inox của khách đặc biệt** không tính theo kg.
7. **Chỉ gia công** không thu tiền cắt và tiền lỗ; **vật liệu khách cấp** vẫn thu.

---

## 10. Câu hỏi thường gặp

**Giá một tấm gồm những gì?** Tiền vật liệu + tiền cắt + tiền gia công (lỗ, Tapu, Sara, bẻ, pitchi, cuộn, vát), sau đó áp quy tắc 55 % / 68 %, làm tròn 10 ¥, có thể đổi sang giá theo kg (khách đặc biệt) hoặc theo chế độ tính, cuối cùng giá tối thiểu 250 ¥ nếu bật.

**Vì sao phôi cộng thêm 10 mm?** Đó là phần vật liệu hao hụt quanh tấm khi cắt laser.

**Vì sao tấm phẳng to lại đắt hơn tổng các khoản?** Vì quy tắc vật liệu không quá 55 % (thép) hoặc 68 % (inox) giá bán.

**Khách ưu đãi được lợi gì?** Hệ số giá vật liệu thấp hơn.

**Khách đặc biệt tính thế nào?** Cân (X+6)(Y+6) × đơn giá ¥/kg, có giá riêng cho tấm đơn và tấm gia công; nếu được so với giá thường thì lấy giá thấp hơn.

**Khách mang vật liệu tới thì sao?** Chỉ thu 15 % tiền vật liệu làm phí quản lý. Nếu còn cắt thì vẫn tính tiền cắt và lỗ; nếu chỉ gia công thì không.

**Vì sao một đường bẻ ngắn vẫn mất 300 ¥?** Đó là mức tối thiểu cho mỗi đường bẻ (công gá đặt máy).

**Vì sao pitchi số lượng ít lại đắt?** Phí chuẩn bị 1 500 ¥ chia cho số tấm; làm 1 tấm thì tấm đó chịu cả 1 500 ¥, làm từ 5 tấm trở lên thì mỗi tấm chịu 300 ¥.

**Vật liệu chưa có giá thì sao?** Chưa báo giá được; cần bổ sung vào bảng giá, hoặc đặt giá vật liệu tạm cho đơn đó.

**Giá nào gửi cho nhà máy?** Giá 1 tấm (đơn giá) của từng tấm; thành tiền = đơn giá × số lượng.
