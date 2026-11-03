# 12. Chẩn đoán và báo lỗi

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Các lỗi đã phát hiện và sửa

5. Thêm `*error*` handler cho cả `c:bg` và `c:ocsv` (khôi phục `CMDECHO`, đóng file CSV đang
   mở, kết thúc undo mark khi có lỗi/Cancel), và kiểm tra `nil` sau mỗi `(open ...)` để tránh
   lỗi `invalid argument type: portp: nil` nếu không mở/tạo được file CSV.

6. Thêm thông báo kết thúc `"Đã xuất thông tin N tấm."` (viết bằng `\U+XXXX`, không gõ dấu
   trực tiếp) để người dùng biết chắc lệnh đã chạy xong và xử lý bao nhiêu tấm — trước đây
   lệnh chạy xong trong im lặng, dễ hiểu nhầm là bị treo/lỗi.

12. **Lệnh chẩn đoán `bgdbg`** — chọn 1 TEXT/MTEXT (bằng `entsel`, không dùng
   `ssget "_+.:E:S"` vì chưa chắc ZWCAD hỗ trợ chuỗi mode đó) rồi in ra từng bước:

   > **Cảnh báo này chỉ nhắm chuỗi mode PHỨC TẠP như `"_+.:E:S"`.** Các mode cửa sổ
   > **`_C` / `_CP` / `_WP` thì CHẠY TỐT trên ZWCAD** — `xuat_csv_v5.0.1 ZWCAD.lsp`
   > dùng chúng ở dòng 697, 708, 3268, 3281. `gro--va-cham-p` (mục 33) dựa vào đó.
   > Đừng đọc mục này rồi tránh nhầm cả `_C`.
   group 1, từng group 3, chuỗi raw + mã byte, chuỗi sau khử mã định dạng + mã byte, kết quả
   bộ lọc `"t"`, rồi `T= / Q= / M= / V=` và **dòng CSV mà `bg` sẽ ghi**, kèm group 10,
   group 40 và `LUNITS`/`LUPREC`. Cảnh báo rõ khi `Q=` rỗng hoặc `T=` ra 0. Không sửa gì
   trong bản vẽ.

14. **`c:bg` giờ báo cáo tại sao bỏ qua đối tượng.** Trước đây chỉ in tổng số tấm, ra 0 thì
   không biết hỏng ở khâu nào (ssget không bắt được? bộ lọc `"t"` không khớp? nội dung rỗng?).
   Nay in: số đối tượng **chọn được** và số **thực sự xử lý**, cả hai tách theo TEXT/MTEXT,
   số bị bỏ qua vì không chứa `t`, kèm **3 ví dụ chuỗi thật** bị bỏ; cảnh báo riêng khi
   `ssget` bắt được MTEXT nhưng không cái nào qua được bộ lọc `t`. Đồng thời thêm guard
   `ssget` trả `nil` (bấm ESC) — trước đây rơi vào `(ssname nil 0)` →
   `bad argument type: lselsetp nil`.

   Ví dụ output thật:

   ```
   Chon 29 doi tuong: 26 TEXT, 3 MTEXT.
   Xu ly    9 doi tuong: 6 TEXT, 3 MTEXT.
   Bo qua 20 doi tuong khong chua "t" / fullwidth t. Vi du:
      TEXT [60]
      TEXT [800]
      TEXT [φ34]
   Đã xuất thông tin 9 tấm.
   ```

   Các đối tượng bị bỏ là kích thước/ghi chú thuần số (`60`, `800`, `φ34`) — đúng như thiết kế.

21. **Lệnh chẩn đoán `grodbg`** — cùng triết lý với `bgdbg`: thay vì đoán, in ra từng bước
   của đường mở hộp thoại (đường dẫn, kết quả ghi file, **dump nguyên nội dung file DCL**,
   `load_dialog`, `new_dialog`, `start_dialog`, giá trị đọc về từ hai ô). Bước dump nội dung
   file là quan trọng nhất: nó trả lời dứt điểm câu hỏi các escape `\U+XXXX` có còn nguyên
   dấu `\` sau khi qua bộ đọc chuỗi của AutoLISP hay không.

29. **`gro` ĐỌC LẠI CHÍNH ĐẦU RA CỦA NÓ — lỗi nặng nhất từ trước tới nay.**
   Chạy `gro` lần hai trên bản vẽ đã có kết quả lần một: dòng **`T=6.0` của F-name** bị nhận
   là **ghi chú tấm**, vì mẫu dấu mốc `t=` nhận cả chữ `T` hoa (mục 23), mà F-name có đúng
   dòng `T=<độ dày>`.

   Quy mô thật, đo bằng `grosort`: bản vẽ có **10** ghi chú thật nhưng lệnh đếm ra
   **160 "ghi chú tấm"** — 150 cái là F-name cũ. Phân biệt được ngay ở cột `cao`:

   ```
   121 | cao 60.00 | t=9?*1   <- ghi chu THAT
   123 | cao  1.00 | T=6.0    <- F-NAME do chinh gro tao ra
   ```

   Hậu quả: số bango nhảy vọt (mỗi ghi chú thật cách nhau ~8-16 số), thứ tự loạn, CSV đầy
   dòng rác `Q` rỗng.

   **Hai thay đổi gần đây cộng hưởng tạo ra lỗ này**, từng cái riêng lẻ thì chưa đủ:
   - chế độ **VÙNG** dùng `ssget "_X"` → quét **toàn bộ** bản vẽ, không còn giới hạn ở vùng
     người dùng quét chọn (mục 25);
   - **F-name đổi từ MTEXT sang TEXT từng dòng** → dòng `T=6.0` đứng riêng thành một đối
     tượng, càng dễ bị nhận nhầm (mục 26).

   **Cách chữa: bỏ qua các layer mà chính `gro` ghi ra** — `gro--layer-dau-ra-p`, lọc
   **bên LISP** chứ không nhét vào bộ lọc `ssget` (mục 13 đã ghi ZWCAD xử lý danh sách ngăn
   cách bằng dấu phẩy khác AutoCAD, không đặt cược vào đó). Báo cáo in rõ số đối tượng đã bỏ
   qua vì lý do này.

   > **Layer `0` KHÔNG lọc.** TEXT bango chỉ chứa con số (`"121"`) nên không có `t=` lẫn ký
   > tự đếm, tự rơi vào nhóm bỏ qua. Mà lọc layer `0` thì **rất nguy hiểm** — ghi chú thật
   > của người dùng hoàn toàn có thể nằm trên đó.

   **Luật rút ra, áp dụng cho mọi lệnh sau này: lệnh không bao giờ được đọc lại đầu ra của
   chính nó.** Mỗi khi thêm một loại đầu ra mới, phải tự hỏi ngay "chạy lần hai thì khâu đọc
   có nhặt phải nó không?". Đặc biệt nguy hiểm khi đầu ra **có cùng định dạng với đầu vào** —
   ở đây cả hai đều là `<chữ>=<số>`.

   **Dọn dẹp bản vẽ đã bị dính:** kết quả các lần chạy trước chồng lên nhau (dữ liệu thật cho
   thấy F-name trùng toạ độ 4-6 lớp). Phải xoá sạch layer `2-F-NAME`, `3-KEGAKI` và các TEXT
   bango cũ rồi chạy lại, chứ bản sửa này không tự dọn được.

36. **Lệnh chẩn đoán `grovc` — bộ đặt vị trí bango báo vướng ở gần như mọi nơi.**
   Bản vẽ thật (89 ghi chú → 100 tấm) cho `chua sach: 86` — 86% số tấm không tìm được chỗ
   sạch trong 20 ứng viên, trong khi nhìn bằng mắt quanh ghi chú là chỗ trống. Hệ quả nhìn
   thấy: khi mọi ứng viên đều vướng, nhánh "ít vướng nhất" hoà số đếm thì lấy ứng viên
   **đứng đầu danh sách** (góc trên-trái), nên cặp `本図`/`逆図` bị văng ra hai góc đối diện
   (bango 4 dưới-phải, bango 5 trên-trái của cùng một ghi chú).

   **Kết quả đo:** vật cản là mã khắc + F-name → đã loại khỏi phép đếm (mục 37).
   Đoạn dưới là giả thuyết ban đầu, giữ lại để hiểu lịch sử.

   Giả thuyết ban đầu: `gro--dem-va-cham` đang đếm một vật cản **không nhìn thấy**. Nghi phạm: đối
   tượng trên layer tắt/đóng băng, khung/hatch/block lớn mà `ssget "_C"` của ZWCAD bắt theo
   hộp bao. **Chưa xác minh** — `grovc` được thêm để đo chứ không đoán: chọn một bango bị
   báo "chua sach", lệnh chạy đúng phép `ssget "_C"` (kèm `trans`) trên hộp bao của nó rồi
   in từng đối tượng bắt được: loại, layer, trạng thái layer, và cờ `BAO TRUM ca cua so`.

   > `grovc` **chép** phép `ssget` của `gro--dem-va-cham` chứ không gọi hàm đó (nó cần liệt kê
   > cả đối tượng bị bỏ qua). Sửa một bên thì phải sửa bên kia.

   **`grovc` chưa đủ** — nó chỉ đo hộp của riêng TEXT bango, không đo đúng ô ứng viên mà `gro`
   thử. Sau mục 37, hai cụm `t=6mm` / `t=9mm` vẫn bị đặt ở ứng viên **dưới 5/5**, nghĩa là cả
   5 vị trí **trên** đều bị báo vướng dù nhìn thì trống. Nên thêm chế độ in ngay trong `gro`:

   - `gro--vat-can` tách ra từ `gro--dem-va-cham`, trả **danh sách** vật cản;
   - cờ toàn cục **`gro--dbg-vc`** = số tấm đầu tiên cần in (bật bằng lệnh `grovtdbg`, hoặc gõ
     `(setq gro--dbg-vc 5)` khi chỉ nạp bản gọn). Tự giảm mỗi tấm nên chạy xong là tự tắt;
   - mỗi tấm in hộp cụm ghi chú, kích thước khối, khoảng hở; mỗi ứng viên đã thử in canh,
     toạ độ hộp và **từng vật cản: loại, layer, nội dung chữ**; cuối cùng in canh đã chọn.

   `gro--dbg-vc` **cố ý là biến toàn cục**, không được thêm vào danh sách local của `c:gro`.
   Tắt thì chi phí gần bằng 0 (một phép `and`). Có trong **cả hai bản** — đã đồng bộ.

48. **`Loi: invalid argument type: stringp: T` ngay sau khi tạo được kiểu chữ — CHƯA TÌM RA
   NGUYÊN NHÂN, đã dựng phép đo.** Ghi lại trung thực vì đây là lỗi đang mở.

   Nguyên văn người dùng báo:

   ```
   2477 found, Filtered out 2361
   Da tao kieu chu "guro" bang entmake (font arial.ttf, Height 0, Width factor 1).
   Loi: invalid argument type: stringp: T
   ```

   ### Khoanh được vùng, nhưng không chỉ được dòng

   Dòng `Vi tri bango:` của báo cáo **chưa in** → lỗi nằm giữa bước tạo kiểu chữ và bước in
   báo cáo, tức **trong vòng tạo entity hoặc khâu ghi CSV**.

   Đã quét tĩnh hết các khả năng và **không có cái nào khớp**:

   | Đã kiểm | Kết quả |
   |---|---|
   | `gro--bao-dam-style` trả về gì | luôn là `ten` (chuỗi) hoặc `nil` — không có đường nào trả `T` |
   | định nghĩa trùng lặp (bản cũ đè bản mới) | không có, ở **cả hai** file |
   | biến từng được gán `T` rồi đưa vào hàm đòi chuỗi | không có |
   | hàm có thể trả `T` được gọi lồng trong `strcat`/`strcase`/… | không có |
   | mọi biến truyền **thẳng** vào `strcat` trong đường chạy | đều là chuỗi, trừ các biến chỉ dùng ở chế độ vùng (người dùng không dùng) |
   | `bg--codes-str` / `bg--uni-cp932` / `gro--vl-cpl` (đường chữ Nhật, mục 43) | trả chuỗi / list số |
   | `fn--fmt-t`, `gro--fields`, `gro--dich-khoi`, `fn--set-color` | không dính chuỗi kiểu này |

   **Đừng lặp lại các hướng trên** — đã loại rồi.

   ### Hai việc đã làm

   **1. Chốt kiểu dữ liệu ở đúng chỗ khớp với thông báo lỗi.** `style-bg` được ép phải là
   chuỗi trước khi vào `(cons 7 …)`; `gro--dat-style` không gọi `strcase` khi tham số không
   phải chuỗi. Nếu `T` thật sự đi ra từ đường kiểu chữ thì chốt này chặn đứng.

   > Dùng `eq` chứ **không** dùng `=` / `/=` khi so `(type x)` với `'STR`: `=` và `/=` của
   > AutoLISP là **phép so SỐ** (xem mục 25). Viết `(/= (type x) 'STR)` là tự tạo ra đúng
   > loại lỗi đang đi tìm.

   **2. Bộ theo dõi bước `gro--buoc`.** Lỗi runtime của AutoLISP **không cho biết dòng nào**,
   nên không có mốc thì mỗi lần hỏng lại phải đoán. Nay `c:gro` đặt `gro--buoc` ở từng chặng
   và `*error*` in ra:

   ```
   Loi: invalid argument type: stringp: T
      (dang o buoc: tao entity - bango 7)
   ```

   Các mốc: `khoi dong` → `tao kieu chu bango` → `gan tam CPL` →
   `tao entity - bango N` → `dat vi tri - bango N` → `ghi file CSV` → `in bao cao`.

   `gro--buoc` **cố ý là biến toàn cục**, cùng nhóm với `gro--dbg-vc` — không được thêm vào
   danh sách local của `c:gro`. Chi phí: một `setq` mỗi tấm.

   > **Bài học:** với AutoLISP, mốc bước là thứ nên có **sẵn** trong lệnh dài, không phải thứ
   > thêm vào sau khi đã hỏng. Đọc code để tìm lỗi kiểu dữ liệu trong ngôn ngữ không có kiểu
   > tĩnh là việc **tốn thời gian mà không chắc ra** — đúng bài học của mục 12 và 36.

85. **Rà lại toàn bộ sau đợt sửa ngày 05/10 — 9 lỗi, có 1 lỗi làm chết `gro`.**

   Đợt 05/10 (không qua phiên làm việc này) thêm 11 lệnh (`xln`, `cln`, `dsn`, `xlnds`,
   `grokhung`, `grotam`, `dst`, `grods`, `ds`, `fixfd`, `netdst`), dialog `gro` thêm 6 lựa chọn
   kèm lưu registry, và một plugin .NET (`laptrinhlisp\ZwCadPlugin`). Bản gọn cũng được sửa
   tay chứ không sinh lại từ bản đầy đủ.

   ### Hai bản có thật sự lệch nhau không?

   So khớp **chính xác từng byte** báo gần như mọi hàm đều khác — sai lệch đó là do **định dạng
   lại** (dấu cách trước ngoặc đóng, chú thích dồn dòng). Sau khi bỏ chú thích, gom khoảng
   trắng và bỏ khoảng trắng cạnh ngoặc: **218/221 hàm giống hệt**, chỉ **3 hàm lệch thật**.

   > Bài học: so khớp từng byte là phép kiểm **quá chặt** khi có người dùng công cụ format.
   > Phải so trên dạng chuẩn hoá, nếu không sẽ tưởng hai bản đã tách nhau hoàn toàn.

   ### Các lỗi đã sửa (cả hai bản)

   | # | Chỗ | Lỗi | Hậu quả |
   |---|---|---|---|
   | 1-3 | `c:gro` (cuối vòng ghi chú), `gro--tao-khung-dong-thuan` ×2 | thay thế hàng loạt `cum-khois-map` → `cum-khois-map vungs-tk raw-tam-list` trúng nhầm `(cons …)` và `(assoc …)` | **`gro` ở bản đầy đủ chết ngay sau tấm đầu tiên** — "too many arguments". Bản gọn đúng nên người dùng chưa gặp |
   | 4 | `gro--xu-ly-net` | bản gọn đặt `gro--mau-dim-xln` = `nil` khi không dò được | lấy cách an toàn của bản đầy đủ: mặc định `4` |
   | 5-7 | `gro--ve-khung-rect` ×2, `gro--tao-khung-dong-thuan` | `vl-catch-all-apply` chỉ 1 tham số (thiếu danh sách đối số) | đúng loại lỗi mục 59: nổ ngay tại chỗ đang đi bắt lỗi |
   | 8 | `gro--it-la-tam-be-p` | dò tấm bẻ bằng `"曲"` gõ thẳng và `"\U+66F2"` | **không bao giờ khớp**: ZWCAD đọc source theo CP932 (mục 2), còn `\U+` không được dịch trong chuỗi (FN mục 2). Nay so mã `26354` qua `bg--codes` |
   | 9 | `gro--detect-dim-color` ×2 | `wcmatch` với `*枚*,*本*` gõ thẳng | phần loại trừ đó không bao giờ khớp → dòng số lượng có thể bị tính là chữ kích thước. Nay so mã `26522`/`26412` |
   | 10 | `gro--giay-p` | định nghĩa **hai lần** (giống hệt) | bỏ bản thứ hai |

   Đồng thời chuẩn hoá xuống dòng: cả hai file dùng **CRLF**, ba dòng chèn mới đã được đổi
   theo — không để lẫn hai kiểu.

   ### Còn lại, chưa sửa (độ ưu tiên thấp)

   - **Bản gọn không còn ASCII thuần**: có chữ Nhật trong **chú thích** (dòng ~45, 1631,
     1809-1818, 3529-3553) và một chuỗi `princ` hiển thị `"入"`. Chú thích không ảnh hưởng
     chạy trong đa số trường hợp, nhưng UTF-8 đọc theo CP932 **có thể** nuốt mất ký tự xuống
     dòng ở vài cách ghép byte — tức dòng code kế tiếp thành chú thích. Nên đổi sang romaji.
   - Đợt 05/10 đã ghi tài liệu cho các lệnh mới ở **mục 67–84** (xem mục lục CLAUDE.md).

   > **Lỗi của chính tôi khi ghi mục này:** ban đầu đánh số **68**, trùng với mục 68 có sẵn
   > ("`さん` → `入`") — vì không đọc lại mục lục trước khi đánh số, và còn ghi đè tiêu đề
   > "84 mục" thành "68 mục". Đã đổi về **85**. Luật: **luôn lấy số lớn nhất hiện có trong
   > mục lục + 1**, không suy từ trí nhớ — người khác có thể đã thêm mục giữa hai phiên.
