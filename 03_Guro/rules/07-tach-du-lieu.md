# 07. Tách dữ liệu: độ dày, số lượng, mã khắc

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Các lỗi đã phát hiện và sửa

10. **`(rtos (atof …))` không tham số → `T=` phụ thuộc `LUNITS`/`LUPREC` của bản vẽ** (mục
   "cần kiểm tra lại" cũ, nay đã sửa thành `(rtos v 2 2)`). Đây là lỗi **thật sự nguy hiểm**
   chứ không chỉ là chuyện định dạng:

   - `LUPREC = 0` → `(rtos 3.2)` = `"3"` → `(> (atof T=) 3.2)` sai → `M=` ra **`SPHC`** thay
     vì `SS400`, và cột T trong CSV mất luôn phần thập phân.
   - `LUNITS = 3/4` (engineering/architectural) → `(rtos 9.0)` = `0'-9"` → cột T trong CSV
     thành rác, `FN` đối chiếu độ dày luôn trượt.

11. **Tách `bg--co-t-p` và `bg--fields` ra khỏi `c:bg`.** Toàn bộ đoạn tính `T= Q= M= V=`
   (đổi số full-width → ASCII, hai lần `vl-string-left-trim` theo charset nhãn tiếng Nhật,
   suy ra vật liệu, dò cờ `曲`/`ｶｲｻｷ`) giờ nằm trong `bg--fields`, trả về
   `(contents T= Q= M= V=)`. Mục đích: lệnh chẩn đoán `bgdbg` gọi **đúng code mà `bg` chạy**,
   không phải một bản sao sẽ lệch dần mỗi lần sửa một bên.

17. **Hạn chế còn lại của thuật toán tách `Q=` (giữ nguyên từ bản gốc, chưa sửa).**
   Charset dùng để trim ra `Q=` gồm charset nhãn **cộng các chữ số của `T=`**. Nên nếu số
   lượng trùng chữ số với độ dày và không có ký tự nào khác chen giữa, nó bị ăn mất:

   ```
   "ｔ＝3.2　２枚"  ->  T= = 3.20, Q= = ""   (số 2 bị trim vì '2' nằm trong "3.20")
   "ｔ＝3.2 2本"    ->  T= = 3.20, Q= = ""
   "ｔ＝9mm 本図　２枚" -> T= = 9.00, Q= = "2"  (chữ 'm' chặn lại nên đúng)
   ```

   Đây là hành vi của bản gốc trên **cả hai máy**, không phải lỗi mới sinh ra khi port.
   `bg` và `bgdbg` đều cảnh báo rõ khi `Q=` rỗng.

   > **ĐÃ HẾT Ở `gro`** (xem mục 23). Nguyên nhân gốc là kiểu tách dữ liệu danh sách đen:
   > charset dùng để trim ra `Q=` gồm cả **chữ số của `T=`**. `gro--tim-sl` đọc số **ngay
   > trước ký tự đếm** nên không quan tâm `T=` có chữ số gì — `ｔ＝3.2　２枚` cho `Q=2`,
   > `ｔ＝3.2 2本` cho `Q=2`. **`bg` vẫn còn nguyên hạn chế này.**

23. **Tách dữ liệu kiểu DANH SÁCH ĐEN là sai hướng.** `bg--fields` đọc số bằng cách **cắt bỏ
   ký tự nhãn ở đầu chuỗi** (`bg--trim` + `bg--charset-nhan`) rồi đọc phần còn lại.
   `bg--trim` **dừng ngay ở ký tự đầu tiên không có trong danh sách**. Hệ quả:

   - `ｹｶﾞｷ線 t=9mm` chỉ đọc được vì `ｹ ｶ ﾞ ｷ 線` **tình cờ** đều nằm trong danh sách;
   - đổi chữ đầu, hoặc gặp chú thích mới → `T=` ra **0** → `M=` luôn ra **`SPHC`** →
     **hỏng trong im lặng**, không báo gì;
   - đơn vị `㎜` (một ký tự U+339C) khác `mm` (hai ký tự ASCII) cũng phải bổ sung danh sách.

   Danh sách đen kiểu đó **không bao giờ đủ**. Cách đúng là **tìm thẳng dấu mốc**:

   | | Cách cũ | Cách mới (`gro`) |
   |---|---|---|
   | Độ dày | cắt ký tự nhãn rồi đọc số | tìm `t=` rồi đọc số **ngay sau** |
   | Số lượng | gom mọi chữ số còn lại | tìm `枚`/`本` rồi đọc số **ngay trước** |

   Bắt buộc phải có dấu `=` sau chữ `t` nên chữ `t` trong từ khác (`Plate`, `ﾌﾟﾚｰﾄ`) không
   bị nhận nhầm. Nhận **đủ 8 tổ hợp** `t`/`T`/`ｔ`/`Ｔ` × `=`/`＝` kể cả trộn lẫn, và cho
   phép dấu cách xen giữa. So sánh trên các chuỗi thật:

   | Chuỗi | `bg` (cũ) | `gro` (mới) |
   |---|---|---|
   | `ｹｶﾞｷ線 t=9mm 本図　１枚` | T=9.00 Q=1 | T=9.00 Q=(1) |
   | `ｔ＝3.2　２枚` | T=3.20 **Q=""** | T=3.20 **Q=(2)** |
   | `ｔ＝3.2 2本` | T=3.20 **Q=""** | T=3.20 **Q=(2)** |
   | `<chữ lạ> t=6mm 2枚` | **T=0 M=SPHC** | T=6.00 Q=(2) |
   | `t=6㎜*3` | **T=0 M=SPHC** | T=6.00 Q=(3) |

24. **`t=` nhận diện TẤM; số lượng chỉ quyết định `Q` và việc tách tấm.** Đây là nguyên tắc
   quan trọng nhất của `gro`, và phải tách bạch hai việc.

   Lý do: số lượng có **nhiều kiểu viết**, còn `t=` thì luôn có. `t=6㎜*3` viết số lượng
   bằng dấu nhân, **không có ký tự đếm nào**. Nếu lấy `枚` làm neo thì tấm này **biến mất
   khỏi CSV trong im lặng**. Neo vào `t=` thì không thể mất tấm — kém nhất là ghi `Q` rỗng
   rồi **báo** cho người dùng.

   | TH | Điều kiện | Số tấm | `Q` |
   |---|---|---|---|
   | 1 | có ≥1 **dòng số lượng** ghép được ở dưới | = số dòng phụ | số của từng dòng |
   | 2 | không có dòng phụ; `<số>` trước `枚`/`本` trên chính dòng độ dày | = số lần xuất hiện | số của từng lần |
   | 3 | không có dòng phụ; dấu nhân `*` `＊` `×` rồi `<số>` | 1 | số đó |
   | 4 | không khớp gì | **1** | **rỗng + cảnh báo nguyên văn** |

   TH1 với **đúng một** dòng phụ (có `本図` hay không cũng được, không có `逆図`) → **một
   tấm**. Đó là dạng phổ biến nhất trong bản vẽ thật.

   **`本図` / `逆図` tách thành HAI TẤM riêng**, mỗi tấm một số bango, không cộng dồn. Bango
   của tấm thứ `k` dịch xuống `k * gro--buoc-tam` để không đè lên tấm trước.

   **Nhận diện dòng số lượng phải chặt**: bắt buộc có ký tự đếm `枚`/`本` **kèm chữ số ngay
   trước**. Chỉ có số thì **tuyệt đối không nhận** — `150`, `1.550`, `1,022.4`, `φ34` là
   **kích thước**. Đây là khác biệt có chủ ý so với dòng độ dày (ở đó `*3` được chấp nhận
   vì đã có `t=` cùng dòng làm bối cảnh). Cũng vì thế **không dùng `x`/`X` ASCII làm dấu
   nhân** — `100x200` sẽ cho số lượng rác.

   **Phạm vi ghép dòng phụ**: nằm dưới, lệch X ≤ `hmax*2`, cách dưới ≤ `hmax*6`, và chọn
   dòng độ dày **gần nhất phía trên**. Cách chọn "gần nhất phía trên" tự động thoả hai ràng
   buộc: không có dòng độ dày nào xen giữa, và mỗi dòng phụ chỉ phục vụ **đúng một** tấm.

   **Ba nhóm cảnh báo bắt buộc, đều in nguyên văn chuỗi** — hỏng trong im lặng tệ hơn hỏng
   có báo, đó là bài học từ mục 23:
   1. ghi chú rơi vào **TH4** (có `t=` nhưng không đọc được số lượng);
   2. **dòng số lượng mồ côi** (đủ điều kiện nhưng không ghép được vào đâu) — nghĩa là một
      tấm đang thiếu số lượng ở đâu đó; in kèm toạ độ để tìm trên bản vẽ;
   3. đối tượng bị bỏ mà **có chữ `t` nhưng không khớp mẫu `t=`** — có thể là ghi chú tấm
      viết kiểu khác đang bị mất không dấu vết.

32. **Mã khắc thiếu đoạn số của bản vẽ.** Tên bản vẽ thường có dạng `xxxxxx-1-s` / `xxxxx-1-a`.
   Sau khi cắt đuôi bằng `(vl-string-right-trim "- S A s a . d f x g w" …)` thì
   `don` = `xxxxxx-1`, tức **đoạn số nằm ở cuối `don`**.

   Cột `N` của CSV vốn đã đúng vì nó ghép `<don>-<bango>` = `xxxxxx-1-1`. Nhưng **mã khắc**
   lại chỉ ghép `<mã>-<bango>` = `I-1`, `I-2`… — **mất đoạn số giữa**. Đúng phải là
   `I-1-1`, `I-1-2`…

   | Tên bản vẽ | `don` | Mã khắc (cũ) | Mã khắc (mới) |
   |---|---|---|---|
   | `xxxxxx-1-s.dwg` | `xxxxxx-1` | `I-1` | **`I-1-1`** |
   | `DPL-26-3-s.dwg` | `DPL-26-3` | `I-1` | **`I-3-1`** |
   | `KHONGCOSO.dwg` | `KHONGCOSO` | `I-1` | `I-1` (không đổi) |
   | `ABC-DEF-s.dwg` | `ABC-DEF` | `I-1` | `I-1` (không đổi) |

   **Bắt buộc đoạn cuối phải TOÀN CHỮ SỐ** (`gro--toan-chu-so-p`). Không kiểm thì tên kiểu
   `ABC-DEF` sẽ cho mã khắc rác `I-DEF-1`. Không tìm thấy đoạn số thì giữ nguyên dạng cũ
   `<mã>-<bango>` và **in cảnh báo** để người dùng biết đoạn giữa bị bỏ.

   **`FN` không bị ảnh hưởng.** Nó đối chiếu bằng `fn--tail` (phần sau dấu `-` cuối cùng):
   `I-1-2` → `2`, cột N `xxxxxx-1-2` → `2` — vẫn khớp. Đã kiểm với bango 1, 2, 12, 107.

   > **An toàn với tên bản vẽ tiếng Nhật.** Hàm chỉ quét tìm dấu `-` (0x2D) và chữ số
   > (0x30-0x39), mà hai vùng mã đó **không nằm trong dải trail byte của CP932**
   > (0x40-0x7E, 0x80-0xFC) — nên `substr` theo byte cũng không thể nhận nhầm nửa sau của
   > một ký tự 2 byte. Cùng lý do mà `fn--split` quét được dấu `,` và `;` an toàn (mục FN 1).

   Chuỗi mã khắc trước đây được ghép lại ở **3 chỗ** (`entmake`, `err-t`, `err-q`) cộng 1 chỗ
   trong báo cáo. Nay tính **một lần** vào biến `mk-day` — sửa một chỗ là xong, không còn
   nguy cơ ba chỗ lệch nhau.

43. **Tấm CPL (thép tấm vân chống trượt) — đổi vật liệu thành `ムキ 縞板`.** Người dùng yêu cầu
   hai trường hợp:

   - chữ `CPL` **nằm ngay trên dòng độ dày**: `CPL  t=2.3㎜ 1枚`;
   - chữ `CPL` nằm ở **một text riêng sát cụm ghi chú**: dòng `裏面CPL` ngay trên `t=6㎜ 1枚`.

   Khi đó:

   | Nơi ghi | Giá trị thường | Tấm CPL |
   |---|---|---|
   | cột `M` của CSV | `SS400` / `SPHC` | `ムキ 縞板` |
   | dòng `M=` của F-name | `SS400` / `SPHC` | `ムキ 縞板` |
   | dòng `Z=` của F-name | `SS400` / `SPC` | **`SS400-SIMA`** (người dùng chọn) |

   **Nhận diện:** `gro--co-cpl-p` tìm dãy `C P L` liền nhau, chữ ASCII không phân biệt hoa
   thường, cộng thêm dạng full-width `ＣＰＬ`. `C P L` có dấu cách xen giữa thì **không** nhận.

   **"Sát cụm ghi chú":** khe giữa hộp bao text CPL và hộp **cụm** ghi chú (ghi chú + dòng số
   lượng, `gro--cum`) phải **≤ 1 chiều cao chữ** của ghi chú (`gro--cpl-gan`). Đủ cho dòng
   nằm ngay trên/dưới (khe giữa hai dòng ~0.5h), không với sang ghi chú kế bên. Mỗi text
   CPL chỉ gán cho **một** ghi chú gần nhất (`gro--ghi-chu-cpl`), nên text nằm giữa hai ghi
   chú không biến cả hai thành CPL. Việc gán phải chạy **trước** vòng chính, vì muốn biết
   ghi chú nào gần nhất thì phải thấy hết các ghi chú.

   Text CPL chỉ được tính nếu **nằm trong vùng chọn** (chế độ thường) hoặc trong vùng
   polyline (chế độ vùng). Quét chọn thiếu dòng `裏面CPL` thì tấm đó giữ vật liệu thường.

   `gro--cum` (hộp cụm ghi chú) nay là **nguồn duy nhất** cho cả vòng đặt bango lẫn bước gán
   CPL. Hai nơi tự tính riêng thì dễ có chuyện một text CPL "sát" ở nơi này mà "xa" ở nơi kia.

   ### Sửa lỗi kèm theo: `bg--codes-str` hỏng chữ Nhật trên máy kiểu A

   Muốn ghi `ムキ 縞板` ra CSV / F-name thì phải dựng chuỗi đúng kiểu máy. Bản cũ của
   `bg--codes-str` chọn cách đổi theo **nội dung danh sách** (có mã > 255 không) chứ **không
   theo kiểu máy**. Danh sách có chữ Nhật luôn có mã > 255, nên luôn đi nhánh `(chr mã)`.
   Nhánh này đúng trên máy kiểu B, nhưng trên máy kiểu A (chuỗi là byte CP932, **máy của
   người dùng**) thì ra rác. Bằng chứng: `grovc` in `[ t=6mm]`, mất hẳn chữ `曲げ` đầu dòng.

   Nay `bg--may-unicode-p` đo **kiểu máy** một lần: `(chr 12354)` có trả đúng một ký tự mã
   12354 không. Kết quả nhớ vào biến toàn cục `bg--may-uni`.
   - Máy B: `(chr mã)`.
   - Máy A: ASCII giữ nguyên, còn lại tra `bg--uni-tbl` ra byte CP932, không có thì `?`.

   `bg--uni-tbl` bổ sung: `ム` (131 128), `縞` (142 200), `板` (148 194), `面` (150 202),
   `Ｃ` `Ｐ` `Ｌ` full-width. `キ` đã có sẵn. Mã byte lấy từ bảng mã 932 của .NET, không gõ tay.

   Kiểm offline: chuỗi byte máy A `(131 128 131 76 32 142 200 148 194)` giải mã CP932 ra đúng
   `ムキ 縞板`; `CPL` / `裏面CPL` / `cpl` / `ＣＰＬ` → có; `ケガキ線 t=9mm` / `C P L` → không;
   `裏面CPL` cách ghi chú 30 (h = 60) → gán; ghi chú xa 1170 → không; text CPL giữa hai ghi chú
   → gán ghi chú gần hơn.

   Báo cáo in thêm: `Tam CPL (thep van chong truot): N tam -> M = MUKI SHIMAITA, Z= SS400-SIMA`
   (chỉ ASCII).

   > **Chưa chạy trên bản vẽ thật.** `grosort` **chưa** hiển thị cờ CPL. Muốn nghiệm thu thì
   > xem cột M của CSV và F-name sau khi chạy `gro`.

54. **Tự nhận diện mã khắc — người dùng không phải chọn tay.**

   Lisp quét text trong phạm vi làm việc, tìm dòng **tên khách hàng**, rồi suy ra mã khắc.
   Hai bước, đúng thứ tự người dùng mô tả:

   1. **Lọc bằng `さん`** (U+3055 U+3093). Chỉ dòng có kính ngữ này mới là dòng tên khách —
      không có bước lọc này thì một dòng tình cờ chứa tên hãng sẽ bị nhận nhầm.
   2. **Đọc tên hãng** trong chính dòng đó:

   | Tên | Mã Unicode | Mã khắc |
   |---|---|---|
   | 岡田 | U+5CA1 U+7530 | `0` |
   | イシ | U+30A4 U+30B7 | `I` |
   | グロ | U+30B0 U+30ED | `G` |
   | 吉野 | U+5409 U+91CE | `Y` |

   **Nhận cả dạng nửa chiều rộng** của hai tên katakana — `ｲｼ` (U+FF72 U+FF7C) và `ｸﾞﾛ`
   (U+FF78 U+FF9E U+FF9B). Bản vẽ Nhật dùng katakana nửa chiều rộng rất nhiều (mục 15), bỏ
   qua thì sẽ có bản vẽ không nhận được mà không rõ vì sao. `さん` là hiragana, không có dạng
   nửa chiều rộng nên chỉ một kiểu.

   > Toàn bộ viết bằng **mã Unicode**, không gõ ký tự Nhật vào `.lsp` (luật ở
   > [01-tong-quan](01-tong-quan.md)). Mã lấy bằng cách **mã hoá thật**, không gõ tay.

   ### Luật khi có nhiều khung bao — người dùng chốt

   Mỗi **khung bao** được xét riêng. Khung không có ghi chú nào thì **không tính là thiếu**
   (nó không có tấm nào để khắc).

   | Tình huống | Xử lý |
   |---|---|
   | Mọi khung ra **cùng một mã** | dùng mã đó |
   | Có khung ra mã, có khung **không** | **vẫn chạy**, dùng mã chung, nhưng **báo rõ** khung nào thiếu và đang dùng mã gì |
   | Hai khung ra **hai mã khác nhau** | **DỪNG HẲN** + `alert` |
   | **Không** khung nào đọc được | hỏi người dùng nhập ở dòng lệnh |

   Dừng hẳn khi mâu thuẫn là đúng: chạy tiếp sẽ khắc sai mã cho một nửa số tấm mà không ai
   biết — đúng loại lỗi tệ nhất (hỏng trong im lặng).

   ### Ba điểm cài đặt

   1. **Gom `さん` ngay trong vòng phân loại**, không quét thêm lượt nữa — chỗ đó đã có sẵn
      `codes`, `pt` và `ebb`. Dòng tên khách rơi vào nhóm "bỏ qua" của khâu phân loại, nên
      không ảnh hưởng gì đến việc đếm tấm.

   2. **`mk-day` phải tính SAU bước nhận diện.** Trước đây nó tính ngay sau hộp thoại; nay
      `mk-code` có thể vừa bị đổi, nên phải dời xuống. Quên chỗ này thì mã khắc in ra một
      đằng, khắc vào bản vẽ một nẻo.

   3. **Hỏi bằng `getstring`, không dùng `initget`/`getkword`.** Một trong các mã là `"0"` —
      từ khoá là chữ số thì chưa chắc mọi bản CAD hiểu giống nhau. `gro--hoi-ma-khac` lặp
      đến khi gõ đúng một mã trong danh sách, bỏ trống thì giữ mã đang có.

   Popup mã khắc trong hộp thoại **vẫn còn**, nay đóng vai **dự phòng**: nhận diện được thì
   nó bị ghi đè (và lệnh nói rõ là đã ghi đè), không nhận diện được thì nó là giá trị mặc
   định cho câu hỏi ở dòng lệnh.

   Kiểm offline (`sim_makhac.py`, 16 ca, tất cả đạt): đọc đúng mã từ dòng thật
   `岡田鉄工さん入れでお願いします` của bản vẽ `JR西宮駅前`; cả 4 tên; cả hai dạng chiều
   rộng; **có tên mà không có `さん` thì bỏ qua**; có `さん` mà không có tên nào thì `nil`;
   dòng ghi chú tấm bình thường không bị nhận nhầm; và đủ 6 tổ hợp của luật nhiều khung.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu trên `JR西宮駅前`: phải in
   > `Ma khac TU NHAN DIEN: 0` (dòng `岡田鉄工さん…` nằm trong khung bao ngoài cùng).
   > Lưu ý dòng đó nằm **ngoài** 13 khung cụm nhưng **trong** khung bao — nên chế độ khung
   > bao phải bật thì mới quét tới.

68. **Đổi từ khóa nhận diện mã khắc từ `さん` sang ký tự `入` (Kanji U+5165).**

   Trước đây, thuật toán tự nhận diện mã khắc (mục 54) lọc các dòng text chứa chuỗi Hiragana `さん` (ví dụ `岡田鉄工さん入れでお願いします`). Tuy nhiên trên thực tế, nhiều khách hàng ghi yêu cầu khắc dấu bằng các cụm khác như `グローバル入れでお願いします` (không chứa chữ `さん` mà chứa `入れ` hoặc `入`).

   Do đó, logic nhận diện được cập nhật: chuyển từ việc tìm `さん` sang tìm ký tự Kanji `入` (mã Unicode `\U+5165` hoặc mã số 20837 trong hàm `gro--ma-san`). Nhờ vậy, mọi chỉ thị gia công dạng `...入れでお願いします` hay `...入` đều được nhận diện chính xác mã khắc của khách hàng.

168. **Ghép dòng số lượng theo BẢNG ỨNG VIÊN — dòng bị tranh về ghi chú GẦN HƠN, mỗi ghi chú tối đa 2 dòng** (người dùng báo + chốt luật, 28/10/2026).

   ### Lỗi người dùng gặp

   Hai ghi chú `ｹｶﾞｷ線 t=6mm` nằm cạnh nhau, mỗi ghi chú có một dòng `本図 ４枚` ngay dưới. Ghi chú bên trái **dài, hộp chữ lấn sang tận dưới tấm bên phải** (tấm 56). Kết quả: ghi chú trái nhận **cả 2** dòng `本図` → tách thành 2 tấm (54, 55), bị coi như cụm mirror, khung đỏ `CANH_BAO_SO_LUONG` bật; tấm 56 mất số lượng (TH4, Q rỗng).

   ### Nguyên nhân

   `gro--ghep-sl` cũ chạy theo **từng cụm dòng số lượng**, mỗi cụm chỉ giữ **một** ghi chú có chi phí `gro--dist-hop` thấp nhất (so `<`). Mục 167 đo lệch ngang bằng **khe giữa hai hộp chữ** (0 khi chồng nhau theo X) → với dòng `本図` của tấm 56, khe tới **cả hai** ghi chú đều = 0, lệch dọc như nhau → chi phí **bằng nhau** → ghi chú xét trước thắng. Không có bước nào biết một dòng đang bị hai ghi chú tranh, cũng không biết ghi chú nào bị bỏ trống. `gro--kiem-tra-trung-loai-sl` chỉ **cảnh báo** sau khi đã ghép sai.

   ### Sửa: hai bước

   1. **Bảng ứng viên.** Mỗi *đơn vị* dòng số lượng × mỗi ghi chú `t=`; cặp **hợp lệ** khi đủ 3 điều kiện:
      - **trong tầm ghép** — `gro--dist-hop` khác `nil` với ít nhất một dòng của đơn vị: lệch dọc tâm–tâm ≤ `max(180, 4.5h)`, khe ngang hộp chữ ≤ `max(120, 2.5h)` (`h` = chiều cao chữ ghi chú). **Ngưỡng giữ nguyên** mục 87/167;
      - ghi chú **không** phải tấm đơn khép kín (`gro--tam-don-khep-kin-p`, mục 82);
      - **cùng khung** (`gro--vung-cua`) — bỏ qua khi chọn text thủ công (tham số `vungs` = `nil`).

      Kèm khoảng cách `d` = **tâm dòng số lượng → tâm ghi chú** (đơn vị cặp: lấy dòng gần nhất).
   2. **Gán tham lam theo `d` tăng dần**, khoá sắp `(d, chỉ số đơn vị, stt ghi chú)` duy nhất (mục 22). Gán khi đơn vị chưa có chủ **và** ghi chú còn chỗ (`gro--sl-toi-da` = **2** dòng). Kết quả:

      | Tình huống | Kết quả |
      |---|---|
      | dòng chỉ hợp lệ với 1 ghi chú | về ghi chú đó (như cũ) |
      | dòng bị ≥ 2 ghi chú tranh | về ghi chú **gần hơn** — in `[GHEP SL] ... hop le voi N ghi chu -> chon ghi chu GAN HON #k` |
      | ghi chú gần nhất đã đủ 2 dòng | sang ghi chú hợp lệ kế tiếp còn chỗ — in `[GHEP SL] ... ghi chu gan hon da du 2 dong` |
      | không còn ghi chú nào | **mồ côi**, báo như cũ |

   ### Đơn vị ứng viên — cặp `本図` + `逆図` không bao giờ bị xé

   Các dòng vẫn được gom cột bằng `gro--tao-cum-cands` (mục 82), rồi `gro--don-vi-sl`:

   | Cụm gom được | Đơn vị |
   |---|---|
   | 1 dòng | 1 đơn vị lẻ |
   | **đúng 2 dòng, một HONZU + một GYAKUZU** | **1 đơn vị CẶP**, chiếm cả 2 chỗ → chỉ vào ghi chú **chưa có dòng nào** |
   | 2 dòng cùng loại, hoặc ≥ 3 dòng | tách lẻ từng dòng (cảnh báo trùng loại mục 87 vẫn bắt nếu sai) |

   Nếu để từng dòng tự chọn, dòng `逆図` (xa hơn) có thể có tâm gần ghi chú bên cạnh hơn → cặp bị xé — đúng lỗi mục 82.

   Ca của người dùng: 2 dòng `本図` nằm **ngang hàng** (không gom cột) → 2 đơn vị lẻ; dòng dưới tấm 56 cách tâm ghi chú 56 ~80, cách tâm ghi chú trái ~470 → về 56.

   ### Bước sau

   - **Cặp 2 dòng `t=` (mục 130)** `gro--gop-cap-dong-doi-xung`: thêm điều kiện sau khi gộp, dòng HONZU không vượt `gro--sl-toi-da` (dòng phụ của a + dòng phụ của b + chính dòng b ≤ 2).
   - **Chữ CPL (mục 43)** đã chọn ghi chú gần nhất; hộp cụm dựng từ dòng số lượng nên tự đúng khi ghép đúng.
   - **Ghi chú tách rời (mục 128/153)** `gro--gc-gan-vao`: **bỏ ưu tiên "text chính nằm dưới"**, chỉ xét **gần hơn** (tâm–tâm) như dòng số lượng — người dùng chốt. Ứng viên vẫn là text chính / dòng số lượng chạm hộp nới `2h` quanh text ghi chú; bằng khoảng cách thì giữ text xét trước. Đầu vào là `bang` nên cũng hưởng lợi từ ghép đúng.

     > **Đổi hành vi:** ghi chú kiểu `ｹｶﾞｷ線` đứng riêng **giữa** hai text chính (một trên, một dưới) trước đây luôn về text **dưới**; nay về text có **tâm gần hơn**, có thể là text trên.

   `gro--ghep-sl` đổi chữ ký thành `(notes cands hmax vungs)`: `c:gro` truyền `khungs`, `c:grokhung` truyền `nil`.

   ### Kiểm chứng

   Mô phỏng Python `sim_ghep_sl.py` (chép đúng logic lisp, kèm bản cũ để so) — **19/19 đạt**: ca thật (bản cũ **tái hiện đúng lỗi**, bản mới chia đúng); ghi chú trái thấp hơn một chút; mục 167 (cặp thụt phải); mục 82 (tấm đơn khép kín không nhận); cặp thật; 2 dòng HONZU cùng cột; khác khung → mồ côi / chọn thủ công → ghép; 3 dòng → tối đa 2; cặp dưới A với ghi chú B thiếu SL nằm sát dòng GYAKUZU → cặp không bị xé; cặp nằm trên ghi chú; 2 ghi chú mỗi bên 1 cặp; cặp và dòng lẻ tranh một ghi chú (cái gần hơn thắng, cặp không bị chia); đảo thứ tự đầu vào 50 lần; dòng ngoài tầm → mồ côi.

   Kiểm cấu trúc file: ngoặc cân bằng, thêm đúng 3 `defun` (`gro--sl-toi-da`, `gro--don-vi-sl`, `gro--dem-ung-vien`), không `defun` trùng, 14 `defun c:` như cũ, CRLF toàn bộ.

   > **Đổi hành vi có chủ ý:** trước đây dòng chỉ hợp lệ với nhiều ghi chú được chọn theo **chi phí** `gro--dist-hop` (ưu tiên dưới, phạt lệch ngang ×2.5); nay khi bị tranh thì chọn theo **khoảng cách tâm–tâm**. `gro--dist-hop` chỉ còn làm phép thử "trong tầm". Ghi chú nhận > 2 dòng không còn xảy ra — dòng thừa sang ghi chú khác hoặc mồ côi (có báo).

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu trên chính bản vẽ người dùng báo: ghi chú trái ra **1** tấm, tấm 56 có **Q=4**, không còn khung đỏ trùng loại; dòng lệnh in `[GHEP SL] ... chon ghi chu GAN HON`.

