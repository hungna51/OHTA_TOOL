# 06. Encoding và xử lý chuỗi (CP932 / Unicode)

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Các lỗi đã phát hiện và sửa

2. **Ký tự tiếng Nhật/full-width gõ trực tiếp trong file `.lsp` (UTF-8) không khớp được với
   dữ liệu entity thật của ZWCAD (byte CP932/Shift-JIS)** — *xem mục 15: cách chữa bằng
   hàm `sj` (dựng chuỗi từ mã byte CP932) sau đó đã hỏng trên máy khác và **đã bị thay
   hoàn toàn** bằng lớp mã Unicode; phần dưới giữ lại để hiểu lịch sử.* — lỗi quan trọng nhất, khiến bộ
   lọc `"t"`, charset tách `T=`/`Q=`, và điều kiện `曲`/`ｶｲｻｷ` (V=) đều không hoạt động dù
   nhìn bằng mắt vẫn "đúng". Sửa bằng cách dựng chuỗi từ mã byte CP932 qua hàm helper
   `sj` (đã thêm vào đầu file) thay vì gõ ký tự trực tiếp, ví dụ `(sj '(139 200))` cho "曲".
   Số full-width (０-９) cũng đổi sang dùng `chr` theo byte thay vì `subst-all` với ký tự
   full-width gõ trực tiếp.

9. **Bộ khử mã định dạng MTEXT (`bg--mtext-strip`)** — group 1/3 của MTEXT **không đảm bảo**
   là text thuần: ZWCAD/AutoCAD chèn mã định dạng vào đó khi text có font riêng, có căn
   chỉnh, đổi màu/chiều cao giữa dòng… Bản trước chỉ bỏ `\P`, `{`, `}` nên các mã còn lại
   (`\A1;`, `{\fMS Gothic|b0|i0|c128|p49;…}`, `\H0.8x;`, `\W0.75;`, `\pxqc;`…) vẫn lọt vào
   `contents`. Nếu mã nằm ở **đầu** chuỗi thì `vl-string-left-trim` dừng ngay ở ký tự `\`
   → `(atof …)` = `0.0` → `T=` = 0, `M=` luôn ra `SPHC`, `Q=` nhặt nhầm chữ số.

   > **Lưu ý:** MTEXT thực tế đầu tiên được kiểm tra bằng `bgdbg`
   > (`ｹｶﾞｷ線 t=9mm 本図　２枚`) **không hề có mã định dạng nào** — group 1 đã là text thuần.
   > Nên bộ khử này là **phòng thủ**, chưa được chứng minh là nguyên nhân của lỗi người dùng
   > gặp. Đừng ghi nhận nó như "đã tìm ra nguyên nhân".

   Xử lý theo đặc tả MTEXT:

   | Mã | Xử lý |
   |---|---|
   | `\P` | → dấu cách |
   | `\~` | → dấu cách (non-breaking space) |
   | `\\` `\{` `\}` | → ký tự thật |
   | `\L \l \O \o \K \k \X` | công tắc, bỏ (không tham số) |
   | `\A \C \F \f \H \Q \T \W \p \S` | có tham số, bỏ đến hết dấu `;` |
   | `{` `}` | dấu nhóm định dạng, bỏ |

   Riêng `\S` (xếp chồng) giữ lại phần chữ, chỉ bỏ ký tự phân cách `^` `#` `/`.

   **Bẫy CP932 khi quét dấu `\`:** rất nhiều kanji/katakana có **trail byte = 0x5C**, đúng
   bằng mã của `\` — `表` = `0x95 0x5C`, `ソ` = `0x83 0x5C`, `十` = `0x8F 0x5C`, `予`, `構`…
   Quét từng byte mà không bỏ qua trail byte sẽ hiểu nhầm chúng là mã định dạng và **làm
   hỏng nội dung tiếng Nhật**. `bg--mtext-strip` nhận diện lead byte CP932 (`0x81-0x9F`,
   `0xE0-0xFC`) qua `bg--sj-lead-p` và chép nguyên cả 2 byte. Hàm làm việc trên danh sách
   byte (`vl-string->list` / `vl-list->string`) chứ **không** dùng `substr`/`strlen`, vì hai
   hàm này có thể DBCS-aware tuỳ phiên bản còn `vl-string->list` chắc chắn trả về từng byte.

   `fn--ent-text` (lệnh `FN`) có cùng hạn chế và giờ gọi chung `bg--mtext-strip`.

15. **"Máy tôi chạy OK, máy khác không xuất được" — hai bản ZWCAD trả chuỗi về khác kiểu.**
   Đây là phụ thuộc máy nghiêm trọng nhất của cả bộ lisp, và **nó không nằm trong bản vẽ**.

   > **Giả thuyết đầu tiên (codepage hệ thống / ACP) là SAI — đã bác bỏ bằng đo đạc.**
   > Máy hỏng báo `SYSCODEPAGE = ANSI_932`, `DWGCODEPAGE = ansi_932`, y hệt máy chạy được.
   > Đừng quay lại hướng đó.

   Đo thực tế cùng một MTEXT `ｹｶﾞｷ線 t=9mm 本図　２枚` bằng `bgdbg` trên hai máy:

   | Máy | `(vl-string->list ...)` trả về | Nghĩa |
   |---|---|---|
   | A — chạy đúng | `185 182 222 183 144 252 32 116 61 57 …` | **byte CP932** |
   | B — không ra gì | `65401 65398 65438 65399 32218 32 116 61 57 …` | **mã Unicode** |

   `65401 = U+FF79 ｹ`, `32218 = U+7DDA 線`… Bản ZWCAD trên máy B có AutoLISP **kiểu
   Unicode**: chuỗi là dãy *mã ký tự*, không phải dãy byte. Mọi charset trong file này
   trước đây dựng bằng **mã byte CP932** (hàm `sj`) nên trên máy B không khớp được gì:

   - bộ lọc `"t"` **vẫn khớp** (chữ `t` là ASCII, giống nhau ở cả hai kiểu) → đối tượng
     **vẫn được nhận**, lệnh chạy hết, không báo lỗi;
   - `vl-string-left-trim` không ăn ký tự nào → `T=` = `0`, `M=` luôn ra `SPHC`,
     `Q=` nhặt nhầm chữ số.

   Đúng những gì `bgdbg` in ra trên máy B: `T= [0]`, `Q= [9]`, `M= [SPHC]`,
   `dong CSV: <don>-<n>,9,SPHC,0,<don>-,` — trong khi đáng lẽ phải là `2,SS400,9.00`.

   **Cách sửa: quy mọi chuỗi về DANH SÁCH MÃ UNICODE ngay từ đầu**, rồi so khớp trên
   danh sách đó. Sau bước này không còn chỗ nào trong logic phụ thuộc vào việc chuỗi là
   byte hay là mã ký tự. `bg--codes` xử lý cả ba dạng biểu diễn có thể gặp:

   | Dạng | Xử lý |
   |---|---|
   | Mã Unicode sẵn (build kiểu B) | giữ nguyên |
   | Byte CP932 (build kiểu A) | ghép cặp lead/trail rồi tra `bg--uni-tbl` |
   | Escape `\U+XXXX` | đọc 4 chữ số hex |

   Nhận dạng kiểu build: **nếu có phần tử > 255 thì chắc chắn là kiểu Unicode**. Quyết
   định **một lần cho cả chuỗi**, không theo từng ký tự — vì mã `224–252` vừa là lead byte
   CP932 hợp lệ vừa là mã Unicode hợp lệ (`à`, `ü`…), đoán theo từng ký tự sẽ sai với text
   có dấu.

   Đã kiểm chứng bằng mô phỏng: cùng chuỗi đó ở **cả ba dạng** đều cho ra
   `[65401 65398 65438 65399 32218 32 116 61 57 109 109 32 26412 22259 12288 65298 26522]`
   → `T=9.00 Q=2 M=SS400`. Cờ `V=` (`曲` / `ｶｲｻｷ`) cũng cho `*` giống nhau ở cả hai kiểu.

   Ký tự CP932 không có trong `bg--uni-tbl` được thay bằng **dấu hỏi**, có chủ ý: dấu hỏi
   cũng không nằm trong charset nhãn nên nó chặn trim y hệt ký tự thật, và không đóng góp
   chữ số nào cho `Q=`. Hai kiểu build vì thế cho **kết quả giống hệt nhau, kể cả khi cùng
   sai** — không có chuyện "máy này dễ tính hơn máy kia", thứ khiến lỗi loại này rất khó lần.
   `bg` in ra mã byte của các ký tự đó để còn biết đường bổ sung vào bảng.

   **Có đổi được kiểu chuỗi bằng cài đặt không? — Không.** Kiểu biểu diễn chuỗi của
   AutoLISP do **bản dựng của CAD** quyết định (engine LISP dựng theo MBCS hay theo
   wide-char), không có biến hệ thống nào đổi được — `SETVAR` không có mục nào cho việc
   này, và `SYSCODEPAGE` thì đã đo là giống nhau ở cả hai máy nên không phải nó.
   Thứ khác nhau gần như chắc chắn là **phiên bản / bản dựng ZWCAD** (hoặc bản Japanese
   edition vs bản quốc tế). `bgdbg` giờ in `ACADVER`, `PRODUCT`, `LOCALE`, `(ver)` để so
   sánh hai máy — chạy trên cả hai rồi đối chiếu là ra ngay chỗ lệch.

   > **Đừng đi theo hướng "làm cho hai máy giống nhau".** Kể cả đồng bộ được phiên bản thì
   > lần cập nhật ZWCAD sau lại lệch tiếp. Cách đúng là code chạy được trên cả hai kiểu —
   > đó chính là việc `bg--codes` làm. Sau bản sửa này không cần cài đặt gì trên máy nào cả.

16. **Hệ quả kiến trúc: bỏ hẳn lớp làm việc theo byte.** Các hàm sau đã bị thay:

   | Cũ (theo byte) | Mới (theo mã Unicode) |
   |---|---|
   | `sj` + charset dựng bằng mã byte | `bg--charset-nhan` (danh sách mã Unicode) |
   | `bg--mtext-strip` (chuỗi) | `bg--strip-codes` (danh sách mã) |
   | `bg--mtext-text` / `bg--u2sj` / `bg--ent-text` | `bg--ent-codes` |
   | `wcmatch contents "*…*"` cho cờ `V=` | `bg--chua` (tìm dãy con trên list) |
   | `vl-string-left-trim` + `atof` | `bg--trim` + `bg--so-dau` |

   Bẫy **trail byte `0x5C`** của CP932 (rất nhiều kanji kết thúc bằng byte `0x5C`, đúng
   bằng mã của `\`) **biến mất hoàn toàn** — sau `bg--codes` thì `表` là một mã `34920`
   chứ không còn là cặp byte `0x95 0x5C`, nên bộ khử mã định dạng MTEXT không thể hiểu
   nhầm nữa. `bg--sj-lead-p` giờ chỉ còn được dùng bên trong `bg--codes`.

   `c:bg` trước đây đọc TEXT bằng `(cdr (assoc 1 ...))` thẳng, chỉ MTEXT mới đi qua hàm xử
   lý — nên TEXT **không** được chuẩn hoá gì cả. Nay cả hai loại đều qua `bg--ent-codes`.
   `fn--ent-text` (lệnh `FN`) cũng gọi chính đường đó rồi dựng lại thành chuỗi bằng
   `bg--codes-str`; trước đây nó mắc đúng lỗi này nên trên máy kiểu B, mã khắc đọc ra
   không bao giờ khớp với CSV.

   **Tránh `vl-some` / `vl-every` / `vl-remove-if-not`** — không chắc có trên mọi bản ZWCAD,
   mà thiếu thì lỗi `undefined function` **không bọc được** bằng `vl-catch-all-apply`
   (mục 3 ở trên). Đã viết tay `bg--co-ma-lon`, dùng `foreach` và `vl-remove-if` — những
   thứ file này đã chạy được trong thực tế.

50. **Thiếu `㎜` trong `bg--uni-tbl` — chuỗi in ra thành `t=4.5?`.** Thấy trong bảng `grosort`
   của bản vẽ `JR西宮駅前` (vùng 11). Ký tự `㎜` là **U+339C**, một ký tự đơn, khác hẳn hai
   chữ ASCII `mm` — mục 23 đã nhắc nhưng chỉ bổ sung cho khâu **đọc**, chưa bổ sung cho khâu
   **dựng chuỗi ngược lại**.

   Trên máy kiểu A (chuỗi là byte CP932), `bg--codes-str` không tra được thì thay bằng `?`
   đúng như thiết kế (mục 15). **Dữ liệu không sai** — `gro--tim-day` làm việc trên danh sách
   mã Unicode nên vẫn đọc ra `T=4.5`. Chỉ có chuỗi hiển thị bị mất chữ.

   Nhưng đó là chuỗi dùng trong **mọi cảnh báo in nguyên văn ghi chú** (TH4, dòng số lượng
   mồ côi, đối tượng có `t` mà không khớp `t=`), tức đúng lúc người dùng cần đọc được nó nhất.

   Đã thêm: `㎜` (13212 → 135 111) và `㎝` (13213 → 135 112). Mã byte lấy bằng cách mã hoá
   CP932 thật, **không gõ tay**.

   > **Cách phát hiện loại lỗi này:** dấu `?` trong bất kỳ chuỗi nào `gro`/`grosort` in ra
   > đều có nghĩa là "ký tự này chưa có trong `bg--uni-tbl`". Thấy `?` thì bổ sung, đừng bỏ qua.

57. **Nhận diện mã khắc không chạy được trên máy thật — vì THIẾU KÝ TỰ trong `bg--uni-tbl`.**
   Người dùng báo: trong khung in có đúng dòng `岡田鉄工さん入れでお願いします` nhưng lisp
   không nhận ra mã `0`.

   ### Cơ chế

   Máy người dùng là **kiểu A**: chuỗi AutoLISP là **dãy byte CP932**, không phải mã Unicode
   (mục 15). Nên `bg--codes` phải ghép từng cặp lead/trail byte rồi **tra `bg--uni-tbl`** để
   ra mã Unicode. Ký tự **không có trong bảng thì bị đổi thành `?`** — và đó là thiết kế có
   chủ ý từ mục 15.

   Hậu quả: khi mục 54 so khớp `さん` / `岡田` theo mã Unicode, nó so với một chuỗi đã biến
   thành `岡田鉄工さん?????????`… nhưng **ngay cả 岡 và さ cũng chưa có trong bảng**, nên
   thực tế là `?????????????`. **9 trong 10 ký tự cần thiết đều thiếu.**

   > Bảng `bg--uni-tbl` chỉ có ~27 mục, gom dần theo nhu cầu từ mục 15 tới giờ. Mỗi lần thêm
   > một mẫu chữ Nhật mới để so khớp thì **phải thêm ký tự của mẫu đó vào bảng** — nếu không
   > sẽ hỏng y hệt lần này, và trên **máy kiểu B thì vẫn chạy đúng**, nên rất dễ bỏ sót khi
   > kiểm bằng mô phỏng.

   ### Sửa

   Thêm 11 ký tự, mã byte lấy bằng **mã hoá CP932 thật**, không gõ tay:

   | | | | | |
   |---|---|---|---|---|
   | さ `130 179` | ん `130 241` | 岡 `137 170` | 田 `147 99` | 鉄 `147 83` |
   | シ `131 86` | グ `131 79` | ロ `131 141` | 吉 `139 103` | 野 `150 236` |

   `イ` đã có sẵn. Hai tên katakana dạng **nửa chiều rộng** không cần vào bảng — `bg--cp932-uni`
   đổi chúng bằng công thức (`byte 161-223` → `+65216`).

   ### Kiểm bằng ĐÚNG bảng đang chạy

   `sim_mayA.py` **đọc thẳng `bg--uni-tbl` từ file `.lsp`**, mã hoá chuỗi thật sang CP932, mô
   phỏng `bg--codes` của máy kiểu A rồi chạy nhận diện. Đây là điểm quan trọng: mô phỏng cũ
   (`sim_makhac.py`) làm việc trên mã Unicode nên **đi đúng đường của máy kiểu B** và không
   bao giờ phát hiện được lỗi này.

   Kết quả: cả 6 ca đạt, gồm đúng dòng thật của bản vẽ. Có thêm **phép thử ngược**: bỏ chữ
   `岡` khỏi bảng thì nhận diện phải hỏng — để chứng minh test thật sự đang kiểm cái cơ chế
   đó chứ không ăn may.

   Phần `入れでお願いします` **vẫn còn `?`**, nhưng chỉ ảnh hưởng chuỗi hiển thị trong báo
   cáo, không ảnh hưởng nhận diện.

   ### Lệnh tự chỉ ra nguyên nhân này lần sau

   Khi không nhận diện được **và** `bg--uni-thieu` khác rỗng, lệnh in thêm:

   ```
   *** Khong nhan dien duoc ma khac: ...
       CO THE do THIEU KY TU trong bg--uni-tbl: tren may ma chuoi
       la byte CP932, ky tu khong co trong bang bi doi thanh "?"
       ngay tu khau doc nen khong con khop duoc voi mau nao.
   ```

   Chính tôi vừa phải lần thủ công đúng chuỗi suy luận đó; để sẵn trong lệnh thì lần sau
   không ai phải lần lại.

   > **Bài học chung:** `bg--uni-tbl` là **danh sách trắng**, mà mục 23 đã chốt là danh sách
   > kiểu đó "không bao giờ đủ". Ở đây không bỏ được vì nó là cầu nối hai kiểu máy — nhưng
   > phải nhớ: **thêm mẫu chữ Nhật mới ⇒ thêm ký tự vào bảng ⇒ kiểm bằng đường máy kiểu A.**
