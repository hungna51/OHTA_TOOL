# 05. Hộp thoại (DCL)

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Hộp thoại (DCL)

Không có file `.dcl` đi kèm — nội dung DCL được **ghi ra lúc chạy** vào thư mục Support
(fallback `tempprefix`), nên chỉ cần phát hành đúng một file `.lsp`.

| Hộp thoại | File tạm | Của lệnh | Các ô |
|---|---|---|---|
| `gro_dlg` | `guro_gro.dcl` | `gro` | **`layer_khung`** (phạm vi — luôn bật, mục 55), **`use_vung` + `layer_vung`** (thứ tự, mục 53). **Không còn ô mã khắc** (mục 56) |
| `guro_bango_dlg` | `guro_bango.dcl` | `bg` | `start_num` (edit_box) |

> **`gro` KHÔNG có ô "Số bắt đầu"** — nó đánh số cả bản vẽ trong một lần chạy và ghi đè file
> CSV (mục 30), nên **luôn bắt đầu từ 1**. Cho nhập số bắt đầu chỉ tạo cơ hội gõ nhầm làm
> CSV lệch với bản vẽ. `bg` vẫn còn ô đó vì nó ghi nối tiếp.

**`gro` hiện hộp thoại TRƯỚC, bấm OK mới bắt đầu chọn.** Khác `bg` (gọi `ssget` trước nhưng
mãi sau hộp thoại mới kiểm `nil`, tức bắt điền xong mới báo không chọn được gì).

**Hai chế độ chọn TÁCH BẠCH — mỗi chế độ chỉ quét chọn MỘT lần:**

| Bước chọn khung bao | Quét chọn cái gì | Phạm vi làm việc |
|---|---|---|
| chọn polyline (mặc định) | **chỉ polyline kín 4 đỉnh** | lisp quét `ssget "_C"` **theo từng khung một**, lấy text nằm trong **hoặc giao** với khung (mục 51) |
| bấm ENTER, không chọn gì | **ghi chú** TEXT/MTEXT | đúng những gì đã quét (mục 55) |

Ô **"Đánh số theo vùng"** là chuyện khác hẳn: nó **không đụng tới phạm vi**, chỉ quyết định
thứ tự đánh số bên trong. Vùng do lisp **tự tìm** trên layer vùng, không phải chọn (mục 53).

Ở chế độ B **không phải quét text lần nào** — chính các polyline xác định phạm vi. Text ngoài
vùng không được tính là "bỏ qua" hay "ngoài vùng": nó đơn giản không thuộc phạm vi.

> ~~Phải ZOOM THẤY HẾT các khung trước khi chạy~~ — **mục 58 đã bỏ `ssget "_C"`** ở khâu quét
> nội dung, nên việc đọc ghi chú không còn phụ thuộc tầm nhìn. Zoom vẫn ảnh hưởng khâu **đặt**
> bango (dò vật cản), nhưng đó chỉ là vị trí xấu chứ không mất dữ liệu.

Thứ tự đầy đủ: kiểm tra quyền ghi CSV (im lặng, tức thì) → **hộp thoại** → quét chọn một lần
theo chế độ.

Popup `layer_vung` bị **vô hiệu hoá** (`mode_tile … 1`) khi không tích `use_vung`, bật lại
qua `action_tile` trên chính toggle đó — theo khuôn `CSV--set-layer-tiles` của
`xuat_csv_v5.0.1 ZWCAD.lsp`.

Popup mã khắc được nạp bằng `start_list`/`add_list`/`end_list` từ `gro--mk-list`, **không**
hard-code thuộc tính `list =` trong DCL — thêm/bớt mã chỉ phải sửa một chỗ. (Bản mẫu
`..\..\dialog\ht.dcl` dùng `list = "I \n0 \nG \nY"`; không theo cách đó vì mỗi mục dính một
dấu cách ở cuối, mà `get_tile` chỉ trả về **chỉ số** nên vẫn phải tra lại bằng list bên LISP.)

**Nhớ lựa chọn chỉ trong phiên làm việc**, không ghi registry. Ba biến toàn cục, cố ý
không khai báo trong danh sách local của lệnh:

| Biến | Nhớ gì |
|---|---|
| `gro--mk-code` | mã khắc vừa dùng — chỉ làm mặc định cho câu hỏi ở dòng lệnh (mục 56) |
| `gro--use-vung` | có tích "Đánh số theo vùng" hay không |
| `gro--layer-vung` | layer vùng vừa chọn |
| `gro--layer-khung` | layer khung bao vừa chọn (mục 53) |
| `gro--vung-bo` | số vùng bị bỏ vì không chạm khung bao nào — chỉ để báo cáo (mục 65) |
| `gro--buoc` | chặng đang chạy của `c:gro`, để `*error*` in ra khi lỗi (mục 48) |

> **`bango` nay là biến LOCAL của `c:gro`**, không còn là biến toàn cục dùng chung với `bg`.
> Trước kia nó toàn cục để nhớ số tiếp theo giữa các lần chạy; nay `gro` luôn bắt đầu từ 1
> nên **không được đụng vào trạng thái của `bg`** nữa. `bg` vẫn dùng `bango` toàn cục như cũ.

Ba luật DCL bắt buộc:

1. `set_tile` / `get_tile` / `mode_tile` / `start_list` chỉ gọi được **sau** `new_dialog` và
   **trước** `start_dialog`.
2. Nhãn tiếng Việt viết bằng `\U+XXXX`. Trong source `.lsp` viết một dấu `\` (AutoLISP giữ
   nguyên vì `\U` không phải escape nó biết) — **không** dùng `\\U+`.
3. Kiểm tra `nil` sau `(open dcl "w")` và chờ `(while (not (findfile dcl)))` trước khi
   `load_dialog`, nếu không sẽ lỗi im lặng khi không ghi được file.


## Các lỗi đã phát hiện và sửa

18. **`start_list` PHẢI truyền tham số thao tác `3` — hộp thoại `gro` không hiện ra.**
   Triệu chứng người dùng báo: gọi lệnh `gro`, chọn đối tượng xong, **không thấy hộp thoại
   nào hiện ra**. Bản đầu viết `(start_list "mk_code")` không tham số, dựa vào giá trị mặc
   định như tài liệu AutoLISP của AutoCAD. Sửa thành `(start_list "mk_code" 3)` — đúng khuôn
   của `xuat_csv_v5.0.1 ZWCAD.lsp`, file DCL nhiều hộp thoại **đã chạy thật** trên ZWCAD,
   nơi cả 3 popup đều gọi `(start_list "pop_N" 3)` — thì hộp thoại hiện bình thường.

   > **Đã sửa xong, nhưng hai thay đổi được áp dụng CÙNG LÚC** (xem mục 19), nên chưa tách
   > bạch được cái nào thực sự chữa. `start_list` là nghi phạm chính vì nó nằm giữa
   > `new_dialog` và `start_dialog`: lỗi ở đó làm hộp thoại chết trước khi kịp hiện.
   > **Quy tắc rút ra: mọi lời gọi hàm DCL đều truyền đủ tham số, không dựa vào giá trị
   > mặc định của AutoCAD.**

19. **Ghi file DCL theo từng dòng, không dùng một chuỗi lớn có `\n` lồng bên trong.**
   Sửa cùng lúc với mục 18. `guro--write-dcl` (của `bg`) ghi cả hộp thoại bằng **một**
   `write-line` với một chuỗi khổng lồ chứa `\n`; `gro--write-dcl` nay dùng `foreach` +
   `write-line` từng dòng, đúng khuôn `write_dcl` của `xuat_csv_v5.0.1 ZWCAD.lsp`. Bớt được
   một biến số (ZWCAD xử lý `\n` lồng trong chuỗi khi ghi file văn bản ra sao).

   > `guro--write-dcl` của `bg` **vẫn giữ cách cũ**, chưa đổi — nếu hộp thoại của `bg` cũng
   > có lúc không hiện thì đây là chỗ đầu tiên cần sửa.

20. **Nhánh `new_dialog` trả `nil` không được im lặng.** Bản đầu của `gro-get-input` trả
   `nil` không in gì khi `new_dialog` thất bại, mà `c:gro` lại in "Da huy." cho mọi giá trị
   `nil` — nên **lỗi cú pháp DCL trông y hệt như người dùng bấm Cancel**. Mất thời gian lần
   mò đúng vì chỗ này. Nay nhánh đó in rõ lý do + chỉ sang lệnh `grodbg`, và thông báo của
   `c:gro` nói rõ là "Cancel **hoặc** không mở được hộp thoại".

   Bài học chung: **mọi nhánh thất bại của `load_dialog`/`new_dialog`/`write-dcl` đều phải
   in thông báo riêng biệt**, vì chúng đều trả về cùng một giá trị `nil`.

52. **Popup layer đổi thành "Layer khung bao" — chỉ liệt kê layer THẬT SỰ có khung.**

   Ô này vốn **đã có** từ mục 25 (`layer_vung`), nhưng nó đổ ra **toàn bộ layer của bản vẽ**
   theo thứ tự chữ cái. Bản vẽ thật có vài chục layer, nên tìm đúng layer khung giữa đống đó
   gần như là tự mò — ô có mà như không.

   Nay `gro--dem-khung-layer` quét một lần `ssget "_X"` các polyline, đếm trên từng layer số
   **khung hợp lệ** (kín, đúng 4 đỉnh, nằm phẳng — đúng luật nhận khung của mục 51), rồi
   popup chỉ hiện các layer đó, **kèm số lượng**, sắp theo số khung giảm dần:

   ```
   Layer khung bao:  [ KHUNG  (13 khung)  v ]
                       VIEN   (2 khung)
                       0      (1 khung)
   ```

   Sắp theo số khung giảm dần nên **mục đầu tiên là phỏng đoán tốt nhất** — mở hộp thoại ra
   là thường đã đúng, không phải chọn gì. Nhãn cũng đổi từ "Layer vùng:" sang "Layer khung
   bao:" cho khớp cách người dùng gọi, và `edit_width` nới 22 → 30 để chứa phần đếm.

   ### Ba điểm cài đặt đáng nhớ

   1. **Hai danh sách SONG SONG.** `get_tile` của popup chỉ trả về **chỉ số**, nên chuỗi hiển
      thị (`"KHUNG  (13 khung)"`) không dùng để tra layer được. `gro--layer-khung-ds` trả về
      cả danh sách **tên thật** lẫn danh sách **hiển thị**; `set_tile`/`add_list` dùng cái
      sau, tra ngược dùng cái trước.

   2. **Không lọc được thì phải NÓI.** Bản vẽ không có polyline kín 4 đỉnh nào → quay về liệt
      kê tất cả layer, kèm dòng cảnh báo. Im lặng thì người dùng tưởng layer khung của mình
      bị thiếu.

   3. **`grodbg` dùng CHUNG `gro--layer-khung-ds`.** Nó tự dựng danh sách riêng thì lệnh chẩn
      đoán sẽ nói khác lệnh thật — đúng lỗi đã mắc ở mục 29. `grodbg` còn in thêm số mục và
      việc có lọc được hay không.

   > **Bẫy `vl-sort` (mục 22) có mặt ở đây.** Khoá sắp xếp là `(số khung giảm, tên tăng)`.
   > Tên layer là duy nhất nên không bao giờ có hai phần tử "bằng nhau" — nếu chỉ sắp theo
   > số khung thì hai layer cùng số khung sẽ **bị mất một cái**.

   Kiểm offline (`sim_khung.py`, 4 ca thêm): chỉ đếm polyline hợp lệ; layer chỉ có polyline
   hỏng thì không xuất hiện; bản vẽ không có khung → danh sách rỗng (kích hoạt nhánh dự
   phòng); hai layer cùng số khung thì sắp theo tên và **không mất layer nào**.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: mở hộp thoại `gro` xem popup có đúng layer
   > khung với số đếm khớp số khung nhìn thấy không; hoặc chạy `grodbg` đọc dòng
   > `layer khung bao: N muc (da loc theo polyline kin 4 dinh)`.

55. **Khung bao thành chế độ MẶC ĐỊNH — bỏ ô tích, chỉ còn popup layer** (người dùng yêu cầu).

   Hộp thoại nay còn ba ô:

   ```
   Mã khắc:                              [I ▼]
   Layer khung bao:                      [KHUNG (1 khung) ▼]
   [ ] Đánh số theo vùng (polyline)
       Layer vùng:                       [VUNG (13 khung) ▼]
   ```

   Ô tích `use_khung` của mục 53 **đã bỏ**, popup `layer_khung` **luôn bật**. Biến toàn cục
   `gro--use-khung` cũng bỏ theo — bỏ một ô trong hộp thoại mà để lại trạng thái đứng sau nó
   là loại rác dễ quên nhất (cùng bài học mục 31).

   Ô **"Đánh số theo vùng"** giữ nguyên: nó là tính năng khác (thứ tự, không phải phạm vi).

   ### Bấm ENTER ở bước chọn khung = quay về quét chọn text thủ công

   Bỏ ô tích nghĩa là không còn cách nào tắt chế độ khung bao từ hộp thoại. Nếu bản vẽ chưa
   kẻ khung thì lệnh sẽ thành **ngõ cụt**. Nên đường quét chọn thủ công **vẫn giữ**, chỉ đổi
   cách vào: bấm ENTER ở bước chọn khung (không chọn gì). Lời nhắc nói rõ:

   ```
   Chon cac KHUNG BAO (polyline kin 4 dinh, layer KHUNG)
     hoac bam ENTER de quet chon text thu cong:
   ```

   > Đây là phần **tôi tự quyết**, người dùng không yêu cầu. Lý do: xoá hẳn đường thủ công
   > thì mất khả năng chạy trên bản vẽ chưa có khung, mà giữ lại thì không tốn gì — code đã
   > có sẵn, chỉ đổi điều kiện vào. Nếu không muốn có đường này thì bỏ nhánh `(if ssv …)`
   > phần `else` trong `c:gro` và báo lỗi dừng thay vào đó.

   `grodbg` bỏ theo phần đọc ô `use_khung` — tile đó không còn trong DCL nữa, để lại thì
   `get_tile` gọi vào một key không tồn tại.

56. **Bỏ luôn popup "Mã khắc" khỏi hộp thoại** — hệ quả trực tiếp của mục 54 (tự nhận diện).

   Hộp thoại nay chỉ còn:

   ```
   Layer khung bao:                      [KHUNG (1 khung) ▼]
   [ ] Đánh số theo vùng (polyline)
       Layer vùng:                       [VUNG (13 khung) ▼]
   ```

   Mã khắc lấy **hoàn toàn** từ bước tự nhận diện. Không nhận diện được thì hỏi ở **dòng
   lệnh** (`gro--hoi-ma-khac`), chứ không quay lại hộp thoại.

   Biến toàn cục `gro--mk-idx` (chỉ số mã trong popup) **bỏ hẳn**, thay bằng `gro--mk-code`
   (chuỗi mã vừa dùng). Nó chỉ còn một việc: làm **giá trị mặc định** cho câu hỏi ở dòng lệnh
   khi nhận diện thất bại. Lần đầu chạy trong phiên thì mặc định là mã đầu trong
   `gro--mk-list`.

   > Lại đúng bài học của mục 31 và 55: **bỏ một ô trong hộp thoại thì phải bỏ cả trạng thái
   > đứng sau nó.** Để `gro--mk-idx` lại thì nó thành biến toàn cục không ai ghi, không ai
   > đọc — rác khó tìm.

   Kéo theo ở `grodbg`: bỏ phần nạp và đọc ô `mk_code`, và bỏ local `lst` vì nó chỉ phục vụ ô
   đó. `grodbg` giờ chỉ còn kiểm ba ô `layer_khung` / `use_vung` / `layer_vung`.

   ### Hệ quả cần biết

   **Không còn cách nào chọn trước mã khắc.** Trước đây gặp bản vẽ không có dòng tên khách
   thì vẫn chọn tay trong hộp thoại rồi chạy thẳng; nay bắt buộc phải qua câu hỏi ở dòng lệnh
   sau khi đã quét xong. Thêm một bước, nhưng đổi lại đường bình thường **không phải chọn gì**
   — đúng mục đích của mục 54.

64. **Popup "Layer vùng" đang dùng nhầm luật của khung bao** (người dùng phát hiện).

   Mục 61 cho **vùng** nhận polyline từ 3 đỉnh, nhưng `gro--dem-khung-layer` vẫn chỉ đếm
   polyline **đúng 4 đỉnh**. Hệ quả: bản vẽ có vùng hình L hoặc tam giác thì layer chứa chúng
   **không xuất hiện trong popup "Layer vùng"** — tính năng của mục 61 coi như không dùng được
   qua hộp thoại.

   Lỗi điển hình của kiểu "nới luật ở một chỗ mà quên chỗ đọc luật đó": mục 61 sửa
   `gro--doc-vung`, nhưng popup lại đếm bằng một đoạn code **riêng** cũng cài cùng luật.

   ### Sửa: mỗi popup một luật, nhưng chỉ quét MỘT lượt

   | Popup | Luật đếm |
   |---|---|
   | Layer khung bao | polyline kín **đúng 4 đỉnh** |
   | Layer vùng | polyline kín **từ 3 đỉnh trở lên** |

   `gro--dem-khung-layer` nay quét một lượt và trả **cả hai** con số cho từng layer:
   `(tên số-4-đỉnh số-từ-3-đỉnh)`. `gro--layer-khung-ds` nhận thêm tham số `chi-4` để chọn
   cột. Nhờ vậy không phải quét bản vẽ hai lần.

   Lưu ý: **cùng một layer có thể hiện số khác nhau ở hai popup** — đúng, vì hai luật đếm hai
   thứ khác nhau. Layer có 1 hình chữ nhật và 1 hình L sẽ hiện `(1 khung)` ở popup khung bao
   và `(2 khung)` ở popup vùng.

   Hai nhánh dự phòng (không lọc được layer nào) nay **tách riêng cho từng popup**, mỗi cái
   một thông báo — trước đây chung một thông báo nên không biết popup nào đang liệt kê bừa.

   `grodbg` in cả hai dòng:

   ```
   layer khung bao: 2 muc (loc theo polyline kin DUNG 4 dinh)
   layer vung     : 5 muc (loc theo polyline kin TU 3 dinh - muc 64)
   ```

   Kiểm offline (`sim_khung.py`, 3 ca thêm): popup khung bao **không** thấy layer chỉ có hình
   L / tam giác; popup vùng thấy hết; và cùng một layer vừa có hình 4 đỉnh vừa có hình L thì
   **hai popup hiện hai số khác nhau**.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: mở hộp thoại, so hai popup — popup vùng phải
   > có ít nhất tất cả layer mà popup khung bao có, và thường nhiều hơn.

70. **Hộp thoại kiểm tra nét `xln_dlg`: Dùng `sssetfirst` bật Grips & Pickset thay vì Zoom; ngắt vòng lặp hộp thoại chính `gro_dlg`.**

   Khi bổ sung hộp thoại `xln_dlg` ("Danh sách nhóm nét giữ nguyên (không đổi)"), có hai bài học thiết kế giao diện quan trọng:

   ### Vấn đề 1: Zoom làm mất Selection Set
   Ban đầu code dùng `(command "_.zoom" "_W" p1 p2)` để phóng to nhóm nét. Tuy nhiên, trong AutoCAD/ZWCAD, bất kỳ lệnh `command` nào chạy sau `(sssetfirst ...)` đều **ngay lập tức hủy bỏ tập chọn đối tượng (Deselect)**. Hơn nữa, người dùng muốn tự tay thao tác trên đối tượng bằng Properties / Layer dropdown.
   **Giải pháp**: Bỏ hoàn toàn lệnh Zoom. Sử dụng `(sssetfirst ss-grp ss-grp)` để bật đồng thời cả Grips (điểm xanh) lẫn Pickset (tập chọn hiện hành). Đổi tên nút thành `Chọn nhóm nét (Select)`.

   ### Vấn đề 2: Hộp thoại chính tự động bật lại đè màn hình
   Trong hàm `gro_dlg`, vòng lặp `(while loop-dlg ...)` mở hộp thoại chính `gro_dlg`. Khi người dùng bấm nút xem danh sách nét chưa đổi (`n = 3`), `gro--hien-dialog-xln` mở ra. Khi người dùng bấm `Chọn nhóm nét (Select)`, nếu không ngắt vòng lặp `while`, hộp thoại chính `gro_dlg` sẽ tự động mở lại lần 2, đè lên màn hình và hủy bỏ selection set vừa chọn.
   **Giải pháp**: `gro--hien-dialog-xln` trả về `T` khi người dùng bấm chọn nhóm nét. `gro_dlg` kiểm tra giá trị trả về này và gán `(setq loop-dlg nil)` để thoát hẳn vòng lặp. Đồng thời trong `c:gro`, khi `dlg` là `nil`, kiểm tra `(ssgetfirst)` để in thông báo hướng dẫn và thoát êm ái mà không gọi `(exit)` (vì `exit` sẽ kích hoạt `*error*` và hủy Grips).

74. **Lưu trạng thái các lựa chọn trong hộp thoại DCL khi tạm đóng để Pick nét mẫu hoặc Chọn màu CAD.**

   Khi hộp thoại DCL hoạt động theo mô hình vòng lặp `(while loop-dlg ...)` để cho phép người dùng tạm đóng hộp thoại, thực hiện thao tác tương tác ngoài không gian vẽ CAD (như `entsel` để Pick nét mẫu hoặc `acad_colordlg` để chọn màu CAD) rồi mở lại hộp thoại:
   - **Vấn đề**: Các nút gọi tác vụ tạm đóng hộp thoại (`(done_dialog 4)`, `(done_dialog 5)`) nếu không đọc `(get_tile ...)` của toàn bộ các control thì khi hộp thoại mở lại ở vòng lặp kế tiếp, mọi lựa chọn trước đó của người dùng (layer khung bao, bật/tắt in nhanh PDF, đánh số theo vùng, layer vùng, bật chuyển layer nét, layer gốc...) sẽ bị khôi phục về giá trị mặc định ban đầu, làm mất toàn bộ thao tác chọn của người dùng.
   - **Giải pháp**: Gán một chuỗi hành động chung `act-save` cho tất cả các nút đóng/tạm đóng (`accept`, `btn_chon_mau`, `btn_pick_dim`, `btn_xln_ds`). Chuỗi này gọi `get_tile` để lưu lại giá trị tất cả các ô vào biến tạm trước khi gọi `done_dialog`. Sau khi `(unload_dialog)` và `(> n 0)`, cập nhật các biến điều khiển (`lk`, `li`, `lx`, `def-in-pdf`, `def-vung`, `def-use-xln`, `cur-auto-dim`) kèm kiểm tra biên danh sách, đảm bảo khi hộp thoại mở lại thì tất cả trạng thái người dùng đã chọn đều được giữ nguyên 100%.

75. **Popup "Layer gốc bản vẽ" (`layer_xln`): Ưu tiên sắp xếp và tự động chọn Layer có nhiều đối tượng nhất.**

   Trong mục xử lý nét & chuyển layer (`xln` / `gro`):
   - **Vấn đề**: Trước đây, popup `layer_xln` dùng `(gro--layer-list)` lấy danh sách layer theo bảng chữ cái. Do layer `"0"` luôn có trong mọi bản vẽ CAD và đứng đầu theo thứ tự chữ cái, popup luôn mặc định chọn layer `"0"` (vốn chỉ chứa 0-2 đối tượng rác). Tệ hơn, nếu bản vẽ mới được mở, biến toàn cục `gro--layer-xln` lưu tên layer từ bản vẽ cũ, nếu không tồn tại hoặc ít đối tượng thì lại rơi về layer `"0"`, khiến người dùng phải mất công mở popup tìm layer chứa chi tiết thực sự.
   - **Giải pháp**:
     1. Hàm `gro--dem-doi-tuong-layer`: Quét `(ssget "_X")`, lọc bỏ Paper Space (`assoc 67 = 1`), đếm số lượng thực thể của từng layer trong Model Space.
     2. Hàm `gro--layer-xln-ds`: Sắp xếp toàn bộ layer theo số lượng đối tượng **giảm dần** (nếu bằng nhau thì xếp theo tên tăng dần). Hiển thị rõ số lượng trên DCL dạng `TÊN_LAYER  (N doi tuong)`.
     3. Khởi tạo lựa chọn trong `gro-get-input`: Ưu tiên chọn ngay layer đứng đầu (index `0` - layer có nhiều đối tượng nhất trên bản vẽ hiện tại). Nếu `def-lay-xln` có số lượng đối tượng bằng với top thì giữ nguyên. Đồng thời trong lệnh `c:xln`, nếu biến lưu chưa có thì tự động đề xuất layer nhiều đối tượng nhất làm giá trị mặc định.




86. **Chuyển hộp thoại `gro` và bảng danh sách tấm (`DST`) sang .NET C# (`ZwCadPlugin`).**

   Phạm vi người dùng chốt: **chỉ hộp thoại + bảng số liệu**. Logic đánh số của `gro` vẫn ở lisp.

   ### Hộp thoại: lisp gọi .NET, DCL làm dự phòng
   - `gro-get-input` nay chỉ là **lớp chọn đường**: nếu `(vl-symbol-value 'gro_net_dialog)` (DLL đã
     NETLOAD) thì gọi `(gro_net_dialog …)`, không thì gọi `gro--hoi-dcl` — chính là hàm DCL cũ,
     **chỉ đổi tên**, ruột giữ nguyên. Hai đường trả về **giống hệt nhau**: list 9 giá trị theo đúng
     `(nth 0..8 dlg)` của `c:gro`, hoặc `nil` = Cancel. Registry vẫn do `c:gro` đọc/ghi.
   - `GRO_NET_DIALOG` trả về 4 dạng: list 9 giá trị (OK), `nil` (Cancel), `("XLN_DS" + 9 giá trị)`
     (bấm "Xem DSN" — danh sách nét nằm bên lisp nên lisp mở `xln_dlg` rồi gọi lại), chuỗi `"LOI"`
     (dialog .NET hỏng → lisp tự chuyển sang DCL). Lỗi lisp khi gọi cũng bắt bằng
     `vl-catch-all-apply` và chuyển sang DCL.
   - **Bẫy thứ tự**: THAM SỐ của `gro-get-input` bắt đầu bằng `lay-k vung layer-vung…`, còn KẾT QUẢ
     bắt đầu bằng `vung layer-vung lay-k…`. Khi gọi lại sau "Xem DSN", lisp phải đổi ngược thứ tự.
     Phần chuyển đổi nằm ở `Core/GroThamSo.cs` và có test "đi một vòng" để bắt lệch.
   - Gọi lại sau "Xem DSN" truyền thêm tham số thứ 10 = `T`: **giữ đúng layer gốc** người dùng vừa
     chọn, không áp lại luật mục 75 (nếu không, chọn một layer ít đối tượng hơn sẽ bị đổi ngầm).
   - "Pick nét mẫu": cửa sổ WPF **đóng**, `Editor.GetEntity`, rồi **mở cửa sổ mới trên cùng
     ViewModel** — giống vòng `done_dialog 4` của DCL nhưng không mất lựa chọn đang dở.
     "Chọn màu CAD…" mở `ZwSoft.ZwCAD.Windows.ColorDialog` **chồng lên** dialog, không cần đóng.
   - Ba popup đếm bằng **một lượt duyệt Model Space** (`CadServices/LayerQuery.cs`), luật sắp và
     chọn mặc định ở `Core/LayerRanking.cs` viết lại đúng từ lisp (mục 52, 61, 64, 75).
   - Lệnh thử riêng: **`GRODLGNET`** — đọc mặc định từ registry như `c:gro`, hiện dialog, in kết
     quả. **Không** ghi registry, không đánh số.

   ### Bảng DST: sửa 6 lỗi của palette cũ (có test tái hiện lỗi cũ)
   | Lỗi cũ | Hậu quả | Sửa |
   |---|---|---|
   | Mỗi dòng = một **khung**, lấy số đầu tiên trong khung | khung có cặp 本図/逆図 mất tấm thứ hai | mỗi dòng = một **TEXT bango**; mã khắc / F-name ghép theo **đoạn số cuối** + vị trí |
   | Lưu mã khắc: `Regex.Replace` **mọi** `-số` | `I-1-3` → `I-5-5` | `GroText.ThayBangoCuoi`: chỉ đoạn số **cuối** |
   | Lưu `N=`: thay cụm số **đầu** sau `N=`, đệm `D2` | tên bản vẽ bắt đầu bằng số bị hỏng (`N=26015-1-3` → `N=05-1-3`) | như trên |
   | Nhận "tấm bẻ" theo dòng `Z=` có chữ 曲げ/BEND | không bao giờ đúng — `gro` ghi `Z=SS400` | dòng **`V=`** có mặt = bẻ (đúng quy tắc `gro`) |
   | Khung không có bango → bango giả = STT | số giả hiện trong bảng | bỏ, báo số khung trống |
   | Không kiểm gì trước khi lưu | ghi được bango trùng / `03` / T chữ | `BangSoLieu.KiemTraTruocKhiLuu`: có lỗi thì **không ghi gì** |

   Thêm: text thuộc khung theo phép **giao** như `ssget "C"` của lisp (bỏ lề 10 mm tự đặt của bản
   cũ); F-name dạng MTEXT `\P` của lệnh `FN` cũ cũng đọc/ghi được; T chuẩn hoá như `fn--fmt-t`
   (`6` → `6.0`); trùng bango hiện cảnh báo màu vàng; ghi xong **quét lại** cho bảng khớp bản vẽ.
   *(Bản đầu của mục này chưa ghi CSV — đã bổ sung, xem phần "Cột bảng theo CSV" bên dưới.)*

   ### Cột bảng theo CSV + lưu cả CSV (sửa 06/10, người dùng yêu cầu)
   - Bỏ cột STT. Cột theo **đúng thứ tự CSV**: `Bango | Q | M | T | GC | Trạng thái`.
     **GC** = cột V của CSV: trống = tấm đơn, `m` bẻ, `k` vát, `*` bẻ + vát. GC lấy **từ CSV** —
     F-name không phân biệt được (dòng `V=` chỉ có khi `m`/`*`, `k` không để lại dấu gì). Không có
     CSV thì GC đoán từ `V=` và dòng lệnh báo rõ. GC sửa được (xem dưới).
   - **GC sửa được (06/10, người dùng yêu cầu)** — Lưu **chỉ ghi GC vào CSV**, bản vẽ không đổi (dòng
     `V=` của F-name giữ nguyên). Giá trị hợp lệ: trống / `m` bẻ / `k` vát / `*` bẻ + vát, và 3 mã người
     dùng ghi tay: `s` gia công sara, `t` gia công M-タップ, `r` gia công cuộn. **Mỗi tấm một mã**, gõ hoa
     tự đổi thường. Danh sách mã ở **một chỗ**: `CsvGro.MaGC` / `TenGC`.
   - Thanh dưới bảng là **một dòng** (`CsvGro.ThongKe`): `Tổng 81 │ Đơn 60 │ m 10 · s 2 │ Lỗi 3 │ Cô đơn 0` — chỉ
     liệt kê mã có tấm, hẹp quá thì cắt "…", rê chuột xem bản đầy đủ có tên từng loại.
     Ghi đúng kiểu GRO: tấm đơn thì dòng CSV **bỏ hẳn cột 6** (5 cột, không dấu phẩy cuối), có gia
     công thì thêm cột 6. Không có file CSV thì GC không lưu được — hộp thông báo nói rõ.
   - Lúc quét còn đối chiếu **Q của bảng với Q của CSV**: lệch, hoặc tấm không có dòng CSV, thì hiện
     cảnh báo vàng trên dòng đó.
   - **Lưu = bản vẽ + CSV.** File CSV = `DWGPREFIX` + tên đơn + `.csv`; tên đơn cắt đuôi y như lisp
     (`vl-string-right-trim "- S A s a . d f x g w"`, tức `TrimEnd` trên tập ký tự đó).
     Thứ tự bắt buộc: **mở CSV độc quyền TRƯỚC** → ghi bản vẽ → ghi CSV. Excel đang giữ file thì
     dừng ngay ở bước 1, bản vẽ chưa bị đổi (bài học mục 27). Ghi bản vẽ hỏng thì không ghi CSV.
   - Chỉ ghi **ô thật sự bị sửa** (so với giá trị lúc quét); ô khác giữ nguyên từng ký tự — vì F-name
     và CSV **cố ý khác nhau** ở M (ngưỡng 4.5 và 3.2) và cách ghi T. Dòng CSV tìm theo **bango lúc
     quét**, tìm hết rồi mới sửa nên đổi chéo số (3 ↔ 5) vẫn đúng. Thứ tự dòng trong CSV giữ nguyên.
   - **T trong CSV giữ kiểu số lẻ của ô cũ**: lisp ghi bằng `(rtos v 2 2)` nên ra `9.00` hoặc `9`
     tuỳ `DIMZIN` của máy (file mẫu trên máy này là `9`). Ô cũ có N số lẻ thì ghi N số lẻ, không có
     dấu chấm thì ghi gọn.
   - Ghi lại đúng kiểu `gro--csv-ghi-de`: UTF-8 có BOM, CRLF. Không sửa gì thì ghi ra giống từng byte
     (có test). Đổi số trùng với một dòng CSV không còn trên bản vẽ thì báo "CSV có 2 dòng cùng bango".

   ### Không cần gõ NETDST nữa (06/10, người dùng yêu cầu)
   - Hàm chung `gro--nap-net`: chưa có `gro_net_dst` thì NETLOAD `ZwCadPlugin.dll` (thứ tự tìm:
     `C:/ZwCadPlugin/`, Support path, thư mục `laptrinhlisp`, `bin/Release`, `bin/Debug`), trả `T` nếu
     bản .NET sẵn sàng. `c:netdst` nay chỉ gọi hàm này.
   - `c:gro` gọi `gro--nap-net` **trước hộp thoại** (để hộp thoại .NET cũng tự có), và **cuối lệnh
     luôn mở + làm mới bảng DST** (`gro_net_dst`). Không nạp được DLL thì giữ cách cũ: chỉ mở bảng
     DCL khi có hình cô đơn.

   ### Tô nổi bật tấm lỗi + sửa zoom (sửa 06/10, người dùng yêu cầu)
   - **Dòng đỏ** (nền đỏ nhạt, viền trái đỏ, chữ đậm) = lỗi dữ liệu hoặc hình cô đơn; **dòng vàng** = cảnh
     báo (thiếu mã khắc / F-name, không có dòng CSV). Luật lỗi ở `Core/KiemTraTam.cs`, **cùng luật
     với `gro`**: thiếu Q / Q không nguyên ("Error Q"), thiếu T / T ngoài bảng chuẩn
     `0.6 … 19.0` ("Error T"); thêm: thiếu M, M không khớp T theo luật F-name (T ≥ 4.5 → SS400,
     còn lại SPHC — **chỉ xét SS400/SPHC**, vật liệu CPL chữ Nhật bỏ qua), Q / T lệch CSV (T so
     **bằng số**: `6.0` = `6`), trùng bango. Lỗi tính lại **ngay khi sửa ô** — sửa đúng là hết đỏ.
     Ô đang sửa thì không so với CSV nữa (lưu xong CSV ghi theo).
   - Lọc "Chỉ tấm lỗi / cô đơn", đếm "Lỗi: n", nút "→ Tấm lỗi tiếp" nhảy vòng qua các dòng đỏ.
   - **Zoom sai, 2 nguyên nhân:** (1) tấm trong `Khung_IN` zoom tới **cả khung in** — mà khung in
     có thể là cả tờ nhiều tấm, nên trông như "không zoom tới tấm". Nay zoom tới hộp của **chính
     tấm** (bango + mã khắc + F-name) nới gấp 3, và không bật grips cho khung in nữa. (2) Đặt
     `CenterPoint` bằng toạ độ WCS — sai, nó tính trong **DCS**, lệch theo `Target`/`ViewTwist`. Nay
     đổi WCS → DCS bằng ma trận chuẩn; sau khi đặt thì **đọc lại** khung nhìn, không đổi thì dùng lệnh
     `'_.ZOOM _W` (điểm đổi WCS → UCS) làm dự phòng.
   - (Từng thêm cột nút 🔍 để zoom lại tấm đang chọn — người dùng bỏ đi: bấm vào dòng là đủ.)

   ### Bảng không cuộn được (sửa 06/10)
   - **Nhúng WPF bằng `ElementHost` Dock=Fill, KHÔNG dùng `PaletteSet.AddVisual`.** Với `AddVisual`,
     ZWCAD đo control theo chiều cao **nội dung**: DataGrid cao bằng cả danh sách, phần dưới bị cắt,
     không bao giờ có thanh cuộn — dù XAML đúng (hàng `*`). Dock=Fill ép đúng kích thước palette.
   - **Con lăn chuột:** ZWCAD gửi `WM_MOUSEWHEEL` cho cửa sổ giữ focus; bấm một dòng thì bảng zoom bản
     vẽ nên focus thường nằm ở bản vẽ → lăn trên bảng lại zoom bản vẽ. Sửa bằng
     `Application.PreTranslateMessage`: con trỏ nằm trên palette (kiểm `WindowFromPoint` + `IsChild`
     để không ăn nhầm con lăn của cửa sổ khác đè lên) thì cuộn bảng và nuốt thông điệp.
   - Thanh cuộn dọc **luôn hiện** — kéo tay được ngay cả khi con lăn có vấn đề.

   ### Kiểm chứng
   `dotnet run --project ZwCadPlugin.Tests` (từ thư mục `laptrinhlisp`): **107/107** — chạy ngoài
   ZWCAD vì chỉ biên dịch thư mục `Core/` + `Models/GroSettings.cs`. Nay **153/153** (thêm phần CSV, luật lỗi, sửa GC, mã s/t/r). Phần gọi API ZWCAD (cửa sổ
   modal từ `LispFunction`, `GetEntity`, `ColorDialog`, ghi entity) **chưa chạy thật** — xem
   [14-can-kiem-tra-lai.md](14-can-kiem-tra-lai.md).
