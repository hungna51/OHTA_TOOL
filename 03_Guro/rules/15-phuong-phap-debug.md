# 15. Phương pháp debug đã dùng

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Phương pháp debug đã dùng

Toàn bộ các lỗi trên được xác định bằng cách chèn tạm các dòng `princ` in giá trị runtime
thật (nội dung text, mã byte từng ký tự qua `vl-string->list`, kết quả `wcmatch`/`entmake`/
`startapp`...) rồi nhờ chạy trực tiếp trong ZWCAD và đối chiếu log — không đoán mò. Xem chi
tiết các mẫu debug trong skill đã dẫn ở trên.

## Tách tấm gộp — bài học "sửa chạy bản vẽ này, hỏng bản vẽ khác" (mục 186, 210, 210b)

Đọc phần này **trước khi sửa bất cứ thứ gì** trong `gro--tach-tam-gop-cad`, `gro--ly-do-khong-cat`,
`gro--chon-vach-chia`, `gro--tim-vach-chia-noi`, `gro--text-ke-hinh-p`. Chi tiết từng lỗi:
[08-sap-xep-va-vung.md](08-sap-xep-va-vung.md) mục 186 và 210.

### Chuyện đã xảy ra (05/11/2026)
1. Bản vẽ A (`570 | 240`, bango 89/92): ghi chú 92 lấn qua vạch chia ~10 % bề rộng → phép kiểm
   "text gần đoạn của nó hơn" cho `d1 = d2` → không cắt. Sửa mục 210 bằng **ngưỡng đoán**: cho cắt
   khi tâm text cách vạch ≥ 1/4 bề rộng text. Mô phỏng 3/3, build đạt.
2. Người dùng báo "vẫn lỗi". Lần chạy đó **dùng code cũ**: bản sửa mới nằm trong worktree, CAD nạp
   lisp từ thư mục chính (chưa gộp).
3. Sau khi gộp, bản vẽ B (`390 | 840 | 725`, bango 67-69) vẫn hỏng: ghi chú `ｹｶﾞｷ線 t=9mm` rộng
   **555 mm** trên đoạn **390** — lấn 32 %, tâm cách vạch 102.8 < 138.8 → ngưỡng 1/4 chặn.
   Sửa 210b: bỏ ngưỡng, bằng nhau thì cắt (phía của text theo tâm text). Đã xác nhận trên CAD.

### Luật rút ra
1. **Lấy số thật trước khi sửa, không ước từ ảnh.** Ảnh chụp màn hình chỉ để biết chỗ nào; con số
   (hộp text, hộp hình, X vạch chia) lấy từ:
   - `%LOCALAPPDATA%\OHTA_TOOL\logs\ohta.log` — từ 210b mọi dòng `[TACH TAM CAD]` (phát hiện + cắt
     tại X, `KHONG cat ... : <lý do> (muc 186b)`, text thiếu hình cạnh hình gộp) ghi ở đây, 1 dòng
     mỗi sự kiện (`gro--tt-in`). Đọc phần sau dòng `GROCHAY bat dau` cuối cùng.
   - `%TEMP%\gro_xepcum_dump.txt` — dòng `E<TAB>handle<TAB>layer<TAB>..<TAB>..<TAB>x1<TAB>y1<TAB>x2<TAB>y2`
     của mọi đối tượng (text ghi chú ở `1-SUNPOU`, nét cắt `0-KAKOU`, `Khung_IN`). Lọc theo vùng:
     `awk -F'\t' '$1=="E" && $6>9400 && $6<10100 && $7>8650 && $7<9020' gro_xepcum_dump.txt`.
   - `%TEMP%\gro_dat_mk_dump.txt` — dòng `L<TAB>T|bango|x1,y1,x2,y2|...` = hộp **hình** của từng tấm
     sau khi cắt → kiểm bề rộng mỗi bango (đúng 390 / 840 chưa).
   - Các dump bị ghi đè mỗi lần chạy GRO — đọc ngay sau lần chạy cần xem.
2. **Kiểm CAD đang nạp file nào trước khi kết luận "sửa không ăn".** CAD nạp lisp từ thư mục ở
   registry `HKCU\Software\OFT_Lisp\OhtaTool\RootDir` (thư mục chính, không phải worktree); sửa trong
   worktree chưa có hiệu lực cho tới khi gộp `main`. `guro_gro.lsp` chỉ nạp **1 lần mỗi bản vẽ** →
   phải mở lại CAD (mục 202). So giờ chạy trong `ohta.log` với giờ commit gộp.
3. **Không đặt ngưỡng đoán theo một bản vẽ.** Ghi chú tiếng Nhật (`ｹｶﾞｷ線 t=9mm` ≈ 555 mm) thường
   **dài hơn đoạn ngắn** của tấm gộp (240, 390), nên hộp text hay lấn qua vạch chia với tỉ lệ bất
   kỳ. Mọi phép so khoảng cách hộp text ↔ đoạn hình khi text lấn vạch đều cho **bằng nhau** — chỉ
   **tâm text** (hoặc điểm chèn) phân biệt được. Muốn thêm ngưỡng mới: phải có ít nhất 2 bản vẽ thật
   khác nhau chứng minh, và ghi số đo vào bảng ca hồi quy dưới đây.
4. **Mỗi lần sửa phải chạy lại toàn bộ ca cũ**, không chỉ ca vừa báo:
   - Mô phỏng: `python -I 03_Guro/plugin/GroTool.Tests/lisp_sim/sim_tach_tam.py` — sửa `ly_do()`
     cho giống lisp, **thêm ca mới bằng số thật**, mọi ca cũ phải vẫn đúng.
   - Trên CAD: chạy GROCHAY ở bản vẽ vừa báo **và** các bản vẽ trong bảng dưới; so số dòng
     `Phat hien Tam` / `KHONG cat` trong `ohta.log` với số đã ghi ở bảng.
5. Phép kiểm chống nét thừa (186b) là lớp bảo vệ duy nhất chống cắt nhầm: vạch chia thật, cùng
   `t=`, mỗi đoạn ≥ 200 mm, khe text ↔ đoạn ≤ 800 mm, vạch nằm giữa tâm 2 ghi chú (±10). Nới luật
   nào thì ghi rõ ca nét thừa còn bị chặn bằng phép nào.

### Bảng ca hồi quy (kết quả đúng đã xác nhận trên CAD)
| Bản vẽ / ca | Kết quả đúng | Nguồn |
|---|---|---|
| `test` 29/10 — 6 tấm gộp (mục 186, 186b, 186c) | Cắt đủ 6: #215, #221, #233, #247 (X = 9770.9), #271, #292; 83/83 text có hình, 0 hình cô đơn | 08 mục 186 |
| Bản vẽ thử nét thừa Continuous trong hình + 1 text cô đơn (186b) | **Không** cắt, có dòng `KHONG cat ... (muc 186b)` | 08 mục 186b — chưa có log thật |
| `570 \| 240` (bango 89/92) | 89 hình 570, 92 hình 240 | 08 mục 210 |
| `390 \| 840 \| 725` (bango 67-69), chạy 05/11 17:57 | 67 = 390, 68 = 840, 69 = 775; `ohta.log` 24 dòng `Phat hien`, 0 `KHONG cat`; cắt tại X 10707.6 rồi 9867.6 | 08 mục 210b |
