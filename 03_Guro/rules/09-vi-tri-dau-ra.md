# 09. Vị trí và hình thức đầu ra trên bản vẽ

> Một phần tài liệu GURO. Mục lục: [../../CLAUDE.md](../../CLAUDE.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Các lỗi đã phát hiện và sửa

26. **F-name của `gro`: đổi từ MTEXT sang TEXT từng dòng, và nằm LỒNG trong mã khắc.**
   Hai vấn đề người dùng chỉ ra khi xem kết quả thật:

   1. **MTEXT bị ngắt dòng giữa chừng.** `FN` đặt group 41 (chiều rộng MTEXT) = `10.0`, mà
      dòng `N=<tên bản vẽ>-<số>` thì dài hơn nhiều — nên nó tự xuống dòng ngay giữa tên bản
      vẽ, ví dụ `N=DPL武蔵小杉 / 大梁No26レーザ / ー（3)-191`. Rất khó đọc. TEXT không có
      khái niệm chiều rộng nên không thể ngắt dòng.
   2. **F-name nằm dưới mã khắc** làm cụm chữ chiếm gấp đôi chiều cao. Nay đặt **cùng điểm**
      với mã khắc: mã khắc cao `10.0`, F-name ~6 dòng × `1.6667` ≈ `10.0`, vừa khít một dải.

   Cách làm: thay chuỗi ghép bằng `\P` (mã xuống dòng của MTEXT) bằng **danh sách từng dòng**,
   mỗi dòng `entmake` một TEXT riêng, xếp xuống theo `gro--fname-dong`.

   > **Bẫy: MTEXT và TEXT dùng điểm gốc KHÁC NHAU.** MTEXT mã khắc dùng điểm căn **trên-trái**
   > (group 71 = 1), còn TEXT lấy group 10 là **chân dòng** (baseline-left). Nên dòng F-name
   > đầu tiên phải lùi xuống đúng một chiều cao chữ, nếu không cả cụm sẽ nhô lên trên mã khắc
   > một dòng. Đây cùng họ với cạm bẫy đã ghi ở mục "Vị trí đặt bango với MTEXT".

   Hệ quả dây chuyền: **`gro--buoc-tam` phải giảm** từ `30.0 + h*1.5` xuống `15.0 + h*1.5`.
   Giá trị cũ tính cho khối *mã khắc 10 + khoảng cách 11 + F-name 6 dòng*; nay F-name nằm
   lồng nên khối chỉ còn cao ~`10.0` + chiều cao chữ bango. Giữ giá trị cũ thì hai tấm tách
   ra từ `本図`/`逆図` sẽ cách nhau một khoảng trống vô lý.

   **`FN` vẫn tạo MTEXT như cũ**, chưa đổi — chạy `FN` sau `gro` trên cùng bản vẽ sẽ cho ra
   hai kiểu F-name khác nhau.

33. **Đặt vị trí bango thông minh — tự tránh hình và chữ của khách.** Trước đây khối bango
   đặt ở **một vị trí cố định** (lùi xuống `h+30`, sang phải `2h`) mà không biết chỗ đó có gì,
   nên thường đè lên hình chi tiết hoặc chữ của khách; người dùng phải kéo lại từng cái.

   Nay thử **5 vị trí quanh cụm text thông tin** theo đúng thứ tự, lấy cái đầu tiên không va:
   **trên → dưới → trái → phải → GIỮA**. Vị trí thứ 5 là phương án cuối, đè lên chính ghi chú
   và **không thử va chạm**.

   Ba quyết định của người dùng: phương án cuối = giữa cụm ghi chú; **mỗi hướng thử đúng một
   lần**; **mọi đối tượng đều là vật cản**, không trừ layer nào.

   **Đo hình THẬT, không ước lượng chiều rộng chữ.** Khối bango chứa dòng `N=` có cả chữ Nhật
   full-width — hệ số rộng/ký tự khác nhau giữa ASCII và full-width, giữa các font. Ước lượng
   thừa thì nhiều tấm bị đẩy xuống phương án cuối oan uổng; ước lượng thiếu thì vẫn đè nhau.
   Cách làm: **tạo khối ở vị trí ưu tiên → đo `vla-GetBoundingBox` thật → suy ra hộp ở 4 vị
   trí còn lại bằng phép dịch → dời khối nếu cần**. Khối nào sạch ngay ở vị trí 1 thì không
   phải dời (đường đi phổ biến nhất).

   > **`textbox` KHÔNG dùng:** cả cây thư mục chỉ có 2 file đời AutoCAD gọi nó
   > (`PolyInfoV1-3.lsp`, `Tạo Fnam(FF)fix 3.lsp`), chưa file ZWCAD nào. Còn
   > `vla-GetBoundingBox` thì đã chạy thật trong `xuat_csv_v5.0.1 ZWCAD.lsp` (6 lần).

   ### Bẫy nguy hiểm nhất: `trans` WCS → UCS

   `vla-GetBoundingBox` trả toạ độ **WCS**, còn `(ssget "_C" p1 p2)` nhận điểm theo **UCS
   hiện hành**. Quên `(trans p 0 1)` thì trên bản vẽ có UCS xoay/dời, cửa sổ tìm kiếm rơi sai
   chỗ và hàm **luôn báo "sạch"** → mọi bango vẫn đè như cũ mà **không có dấu hiệu gì**.
   `xuat_csv_v5.0.1 ZWCAD.lsp` dòng 739 có đúng ghi chú này.

   Phép thử trực tiếp: **xoay UCS rồi chạy lại** — kết quả phải tương đương. Quên `trans` thì
   tất cả sẽ rơi vào "trên" bất kể có vướng gì.

   ### Các điểm khác

   - **Đổi hành vi mặc định**: trước ưu tiên **dưới**, nay ưu tiên **trên**. Mọi bản vẽ cũ
     chạy lại sẽ ra bố cục khác — đó **không phải lỗi**.
   - **Tấm tạo trước KHÔNG bị loại khỏi phép thử va chạm.** Chúng đã nằm trong bản vẽ nên
     `ssget` thấy — và đó là điều mong muốn: tấm sau tự tránh tấm trước. Nhờ vậy hai tấm tách
     ra từ `本図`/`逆図` tự xếp một trên một dưới, không cần luật riêng.
   - Chỉ loại khỏi kết quả `ssget` các `ename` của **chính khối đang xét**, nếu không nó luôn
     tự va vào mình.
   - Cấu trúc GHI CHÚ thêm ô thứ 5: `(stt pt h codes bbox)` — nay thêm ô thứ 6 `ename` (mục 38). Hộp bao đo **ngay lúc đọc**, chỉ
     cho đối tượng được giữ lại — nên ở chế độ vùng quét cả bản vẽ cũng không tốn thêm.

   ### `grosort` CỐ Ý không dự đoán hướng

   Muốn biết trước hướng thì phải đo hộp bao thật, mà khối chỉ có sau `entmake`. Nếu `grosort`
   tự dựng lại code tạo khối để đo thì có **hai bản code làm cùng một việc** — đúng loại lệch
   nhau đã gây ra lỗi ở mục 29 (công cụ chẩn đoán nói khác kết quả thật). Nên `grosort` chỉ
   nhắc, còn con số thật do chính `gro` in ra.

   ### Báo cáo bắt buộc

   Dòng `Vi tri bango: tren=.. duoi=.. trai=.. phai=.. GIUA=..`, kèm danh sách tấm phải dùng
   phương án cuối (có toạ độ). **Con số cần nhìn đầu tiên là `GIUA=`** — đó là số tấm bị đè
   lên ghi chú, tức vẫn phải chỉnh tay. Quá 20% thì lệnh tự nhắc rằng bộ lựa chọn đang quá
   chặt và chỉ đích danh hai hàm cần nới: `gro--ho-bango` (khoảng hở) và `gro--va-cham-p`
   (thêm bộ lọc bỏ qua layer đường tâm / hatch).

34. **Bản đầu chỉ có 4 vị trí cố định — trên bản vẽ thật bị dồn hết vào giữa ghi chú.**
   Người dùng gửi ảnh: khối bango nằm **đúng tâm cụm ghi chú**, đè lên chữ. Đó là phương án
   cuối của mục 33, nghĩa là **cả 4 hướng đều báo va chạm**.

   Nguyên nhân: bản vẽ vẽ **hình tấm ngay phía trên ghi chú**, mà khối bango lại rất cao —
   chữ bango cao `h*1.5` (= 90 khi `hmax=60`), cộng mã khắc nữa là hơn 100 đơn vị. Đặt sát
   trên cụm là thọc thẳng vào hình tấm. Mỗi hướng chỉ thử **một** điểm nên không còn đường lui.

   ### Cách chữa: cho hộp bango TRƯỢT quanh chu vi hộp cụm ghi chú

   Thay 4 điểm cố định bằng **5 vị trí trượt dọc mỗi cạnh** → **20 vị trí**, thứ tự ưu tiên
   giữ nguyên: trên (trái→phải) → dưới (trái→phải) → trái (trên→xuống) → phải (trên→xuống).

   Điểm ăn tiền: khi cạnh trên **bị che một phần**, bản cũ phải bỏ cả cạnh trên; bản mới
   trượt ngang tìm được khe trống và **vẫn giữ được vị trí ưu tiên nhất**.

   `gro--vi-tri-bango` nở hộp cụm ra một khoảng hở thành "đường ray", rồi `gro--truot` rải
   đều `gro--so-buoc-truot` (= 5) vị trí dọc mỗi cạnh.

   ### Bỏ hẳn phương án "đặt vào giữa"

   Nếu cả 20 vị trí đều vướng thì lấy **chỗ ít vướng nhất**, chứ không đè lên ghi chú nữa.
   `gro--va-cham-p` (trả về có/không) đổi thành **`gro--dem-va-cham`** (trả về *số* đối tượng
   vướng) — nhờ có con số mới so được chỗ nào ít vướng hơn. Chỉ tốn đúng một lượt duyệt: gặp
   chỗ sạch đầu tiên là dừng, không sạch thì ghi lại số đếm để cuối cùng chọn min.

   > Biến đếm `vt-giua` đổi tên thành **`vt-chua-sach`**: nghĩa của nó đã đổi từ "phải đặt
   > vào giữa" thành "không tìm được chỗ hoàn toàn sạch". Giữ tên cũ là gài bẫy cho lần sau.

   Báo cáo đổi theo: `Vi tri bango: tren=.. duoi=.. trai=.. phai=..  (chua sach: N)`. Số
   `chua sach` **không còn nghĩa là đè lên chữ** — tấm vẫn nằm sát bên ghi chú, chỉ là có
   vướng thứ gì đó, nên chỉ cần liếc mắt kiểm chứ không bắt buộc sửa.

35. **[ĐÃ THAY bằng mục 40] Hai tấm của `本図`/`逆図` bị xếp CẠNH NHAU thay vì chồng lên nhau.** Người dùng gửi ảnh:
   bango `4` và `5` nằm sát nhau theo chiều ngang phía trên ghi chú — nhìn không ra là một cặp.

   Nguyên nhân là hệ quả trực tiếp của mục 34: tấm 1 chiếm vị trí đầu tiên của cạnh trên, tấm 2
   chạy lại đúng danh sách ứng viên đó, thấy vị trí 1 vướng (chính tấm 1) nên **trượt ngang**
   sang vị trí 2 — thuật toán làm đúng như đã viết, chỉ là kết quả nhìn sai ý đồ.

   ### Cách chữa: tấm thứ 2 trở đi ưu tiên XẾP CHỒNG lên tấm trước

   Thêm `gro--vi-tri-chong` sinh **một ứng viên đặt lên đầu danh sách ưu tiên**: khối mới căn
   thẳng **mép trái** tấm trước, cách một khoảng hở theo chiều dọc. Chỉ khi chỗ đó vướng mới
   quay lại duyệt 20 vị trí quanh chu vi như cũ.

   **Hướng chồng đi RA XA cụm ghi chú:**

   | Tấm trước ở cạnh | Tấm sau chồng |
   |---|---|
   | trên | lên trên nó |
   | dưới / trái / phải | xuống dưới nó |

   Nhờ vậy cả cụm bango luôn nằm về một phía, không bao giờ quặt ngược lại đè lên ghi chú.

   `gro--chon-vi-tri` nhận thêm hai tham số `bb-truoc` / `canh-truoc` và trả về thêm **hộp bao
   cuối cùng** để tấm kế tiếp xếp chồng lên. Hai biến đó **reset ở đầu mỗi ghi chú**, không
   phải mỗi tấm — quên chỗ này thì tấm đầu của ghi chú sau sẽ chồng lên tấm cuối của ghi chú
   trước, xa tít.

   > Luật này áp dụng cho **mọi tấm thứ 2 trở đi**, không riêng cặp `本図`/`逆図`. Ghi chú có
   > 3 dòng số lượng thì ra 3 tấm xếp chồng thành một cột.

37. **Mã khắc và F-name KHÔNG còn tính là vật cản.** Bản vẽ thật báo `chua sach: 86/100`;
   `grovc` (mục 36) trên một bango "chưa sạch" chỉ thấy 2 vật cản — đúng là mã khắc
   (`3-KEGAKI`) và F-name (`2-F-NAME`). Người dùng quyết định loại hai layer này khỏi phép
   đếm.

   `gro--dem-va-cham` nay bỏ qua mọi đối tượng thoả `gro--layer-dau-ra-p` — **dùng lại** hàm
   lọc của mục 29, không thêm danh sách layer thứ hai. Áp dụng cho khối **nào cũng vậy**, kể cả
   mã khắc / F-name cũ sót lại từ lần chạy trước.

   > **TEXT bango (layer `0`) VẪN là vật cản** — nên hai khối không thể đè lên nhau hoàn toàn;
   > chỉ phần chữ rất nhỏ của mã khắc/F-name treo dưới bango là có thể chạm khối khác.
   > Không lọc layer `0` vì lý do đã ghi ở mục 29.

   `grovc` áp cùng luật và in `(bo qua - layer dau ra)` cho các đối tượng đó — sửa một bên
   thì sửa bên kia. **Chưa chạy lại trên bản vẽ thật** để xem `chua sach` giảm còn bao nhiêu.

38. **Ghi chú của CHÍNH tấm bị tính là vật cản — mọi vị trí phía trên luôn "vướng".**
   Người dùng hỏi: sao cụm `t=6mm` / `t=9mm` có bango nằm **dưới**, còn cụm `t=4.5mm` thì
   nằm trên như mong muốn. Chạy `grovtdbg` (mục 36) cho bango 1:

   ```
   cum Y 26513.9..26736.7                       <- vla-GetBoundingBox cua ghi chu
   [tren] Y 26766.7..26866.7 -> 1 vat can: TEXT [ t=6mm]    <- CHINH ghi chu do
   [phai] X 21708.1..     (cum X ..21678.1) -> 1 vat can: TEXT [ t=6mm]
   ```

   > **Mục 41 đã chỉ ra phân tích dưới đây SAI nguyên nhân** — thủ phạm là `ssget` chọn theo
   > độ phân giải màn hình, không phải vùng chọn riêng của TEXT. Giữ lại để hiểu lịch sử.

   Ô thử cách mép hộp bao của ghi chú 30 đơn vị (`ho = h*0.5`), vậy mà `ssget "_C"` vẫn bắt
   trúng ghi chú. Nghĩa là **ZWCAD chọn TEXT theo một vùng LỚN HƠN hộp bao mà
   `vla-GetBoundingBox` trả về** (ít nhất 30 đơn vị ở phía trên và bên phải, với chữ cao 60).
   Cả 5 vị trí trên và phần lớn bên phải đều vướng vào chính ghi chú, nên lệnh phải rơi
   xuống vị trí dưới 5/5. Đây mới là lý do thật của con số `chua sach: 86-93/100`.

   **Cách chữa:** ghi chú và các dòng số lượng của **chính tấm đó** không bao giờ là vật cản.
   Khối bango vốn được đặt để bám vào chúng. Cấu trúc GHI CHÚ thêm ô thứ 6 `ename`
   (`gro--it-ename`). `gro--chon-vi-tri` nhận thêm tham số `rieng` (ename ghi chú + dòng
   phụ) và nối vào danh sách bỏ qua cùng với `khoi`.

   > Ghi chú của **tấm khác** vẫn là vật cản như cũ. Vì vùng chọn của ZWCAD rộng hơn hộp bao,
   > phép thử với ghi chú lân cận **thiên về an toàn** (báo vướng hơi sớm) chứ không để đè.

   > **Chưa rõ vì sao hai vùng lệch nhau.** Có thể do font bigfont / chữ full-width, hoặc
   > ZWCAD nới vùng chọn TEXT. Nếu sau bản sửa bango có **dính sát** vào chữ ghi chú thì hộp bao
   > đang nhỏ hơn chữ thật. Lúc đó tăng `gro--ho-bango` chứ đừng quay lại đếm ghi chú.
   > **Chưa chạy lại trên bản vẽ thật.**

39. **Ghi chú tách nhiều tấm: bango xếp thành CỘT DỌC BÊN TRÁI, ngang hàng các dòng
   `本図`/`逆図`** (người dùng yêu cầu, kèm ảnh mẫu). Trước đó cặp bango bị đặt theo luật chung
   (trên → dưới → trái → phải), rồi tấm 2 chồng lên tấm 1. Kết quả là cặp số nằm trên hoặc
   dưới cụm chữ, không đối diện với dòng số lượng của chính nó.

   Hàm mới `gro--vi-tri-cot` sinh **ứng viên số 0**, đứng trước cả ứng viên xếp chồng, chỉ
   khi ghi chú có **≥ 2 tấm**:

   - **mép PHẢI** mọi khối = mép trái cụm ghi chú − `ho`. Các khối căn phải thẳng hàng, sát chữ;
   - cả cột (n khối, cách nhau `ho`) **căn giữa theo chiều dọc** quanh tâm vùng các **dòng
     số lượng** (`bb-dp`). Ghi chú không có dòng phụ (TH2, số lượng viết ngay trên dòng `t=`)
     thì căn giữa theo cả cụm;
   - tấm thứ `k` vào ô thứ `k` từ trên xuống, cùng thứ tự với các dòng số lượng
     (`gro--sl-cua` đã sắp trên → dưới). `本図` ở trên, `逆図` ở dưới;
   - chiều cao mỗi ô = chiều cao khối **đang xét**. Mọi khối cùng cấu trúc (bango `h*1.5` + mã
     khắc + F-name treo dưới) nên cao như nhau, chỉ khác bề rộng.

   Cột cao `n*bh + (n-1)*ho`, **lớn hơn** vùng hai dòng chữ (khối ~100 so với dòng ~60), nên
   cột nhô ra một chút ở trên và dưới vùng `本図`/`逆図`. Ảnh mẫu của người dùng cũng như vậy.

   **Luật chống tách cặp:** tấm thứ 2 trở đi **chỉ thử cột khi tấm trước cũng đã vào cột**
   (`canh-truoc` = 4). Nếu không có luật này, tấm 1 bị chặn ở cột sẽ ra chỗ khác, còn tấm 2
   lọt vào cột, và cặp bị tách rời. Cột bị chặn thì quay về luật cũ: xếp chồng, rồi trượt
   quanh chu vi.

   Cạnh mới **`4` = "cot trai"** (`gro--ten-canh`). Báo cáo thêm `cot trai=N`. `gro--vi-tri-chong`
   coi cạnh 4 như các cạnh không phải "trên", tức chồng **xuống dưới**. `gro--chon-vi-tri` nhận
   thêm tham số `cot` = `(k n yc)`, hoặc `nil` khi ghi chú chỉ có 1 tấm.

   > Dùng `(equal canh-truoc 4)` chứ **không** dùng `=`: ở tấm đầu tiên `canh-truoc` là `nil`.
   > **Chưa chạy trên bản vẽ thật.**

40. **Cặp `本図`/`逆図` vẫn bị TÁCH — đặt CẢ CỤM bango như một khối.** Người dùng gửi ảnh sau
   mục 39: bango `7` nằm bên trái ghi chú nhưng `8` lại ở trên đầu (tương tự `11`/`12`,
   `13`/`14`). Nguyên nhân gốc: **mỗi tấm vẫn tự tìm chỗ riêng**. Tấm 1 vào ô trên của cột
   trái. Ô dưới của cột đâm vào hình tấm vẽ bên dưới ghi chú, ô xếp chồng cũng vậy, nên tấm 2
   trượt quanh chu vi và lên phía trên. Các luật vá ở mục 35/39 (xếp chồng, "chỉ vào cột khi
   tấm trước cũng vào cột") không chữa được, vì quyết định vẫn được đưa ra **từng tấm một**.

   **Cách chữa: đặt vị trí theo GHI CHÚ, không theo tấm** (`gro--dat-cot`):

   1. Vòng TẤM chỉ **tạo** khối (bango + mã khắc + F-name) rồi gom vào `khois`, chưa đặt.
   2. Hết vòng TẤM: `gro--xep-cot` xếp n khối thành **một cột dọc** trong hệ toạ độ cục bộ
      (mép phải thẳng hàng, cách nhau `ho`, `本図` trên), cho ra hộp bao cả cột.
   3. Trượt **hộp cả cột** quanh cụm ghi chú. `gro--vi-tri-bango` dùng lại nguyên, chỉ thay
      hộp khối bằng hộp cột và điểm gốc `(0 0 0)`, nên ứng viên trả về chính là phép dịch.
   4. Thử va chạm **từng khối bằng hộp bao riêng** (`gro--vat-can-cot`), không dùng hộp gộp
      cả cột. Số `1` hẹp nằm trên số `10` rộng để lại khoảng trống bên cạnh, hộp gộp sẽ coi
      chỗ đó là đã chiếm. Đây là phần "hộp bao sát chữ nhất có thể" người dùng yêu cầu.
   5. Dời **mọi khối cùng một phép dịch**, nên cặp **không thể tách** về mặt cấu trúc.

   Thứ tự ưu tiên:

   | Ghi chú | Thứ tự |
   |---|---|
   | 1 tấm | trên → dưới → trái → phải (như mục 34) |
   | ≥ 2 tấm | **cột trái căn giữa** vùng dòng số lượng → trượt dọc **cạnh trái** → trên → dưới → phải |

   Ghi chú 1 tấm cũng đi đường này (cột 1 khối), nên không có hai bản code.

   **`gro--ho-bango` giảm từ `h*0.5` xuống `h*0.2`** (= 12 khi `h=60`), dùng cho cả khoảng hở
   với chữ lẫn khoảng cách giữa các khối trong cột. Người dùng muốn cụm bango **sát chữ**.
   Làm được vì ghi chú của chính tấm không còn là vật cản (mục 38). Trước mục 38, hở nhỏ thì
   mọi vị trí đều vướng oan.

   **Đã xoá** `gro--chon-vi-tri`, `gro--vi-tri-chong` và hai biến `bb-truoc`/`canh-truoc`.
   Xếp chồng giờ là một phần của cột, không còn là ứng viên riêng. Cạnh `4` = "cot trai" giữ
   nguyên. Bộ đếm `tren/duoi/trai/phai/cot trai` và `chua sach` vẫn tính **theo tấm** (cộng
   n cho cả cụm), nên tổng vẫn bằng số tấm. Ví dụ "chưa sạch" in dạng `bango 7-8 (canh …)`.

   `grovtdbg` nay đếm theo **ghi chú**: mỗi ghi chú in một khối `== DBG bango a..b`.

   > Vị trí **tạo ra ban đầu** (lùi `h+30+k*gro--buoc-tam`, sang phải `2h`) không đổi. Nó chỉ
   > là chỗ đo hộp bao, sau đó cả cụm luôn được dời đi. **Chưa chạy trên bản vẽ thật.**

41. **`ssget "_C"` chọn theo ĐỘ PHÂN GIẢI MÀN HÌNH — bắt cả đối tượng nằm NGOÀI cửa sổ.**
   Người dùng đo trên bản vẽ (ảnh cụm `13`/`14`): cột bango còn cách cạnh tấm bên dưới tới
   **~75 đơn vị**, vậy mà đã bị đẩy lên khỏi vị trí căn giữa. Theo số đo (khối cao 100, hở
   `h*0.2` = 12), ô cột căn giữa nằm cách cạnh tấm khoảng **12 đơn vị**, không có gì chạm nhau,
   nhưng vẫn bị báo vướng.

   Hộp va chạm của khối **vẫn sát**: đúng `vla-GetBoundingBox` của bango + mã khắc + F-name,
   không cộng lề. Khoảng hở `ho` chỉ cộng vào cụm ghi chú. Thứ không sát là **`ssget "_C"`**.
   Đây là phép chọn **theo màn hình**: zoom xa để thấy cả bản vẽ thì một pixel bằng hàng chục
   đơn vị, nên nó bắt cả đối tượng cách cửa sổ vài chục đơn vị.

   > **Sửa lại giả thuyết ở mục 38.** "ZWCAD chọn TEXT theo vùng rộng hơn hộp bao" là **sai
   > nguyên nhân**. Ghi chú cách 30 đơn vị vẫn bị bắt cũng vì cùng lỗi "mờ" này của `ssget`,
   > không riêng gì TEXT. Luật của mục 38 (ghi chú của chính tấm không là vật cản) vẫn đúng
   > và vẫn giữ, vì khối bango vốn được đặt để bám vào ghi chú đó.

   **Cách chữa: `ssget` chỉ còn là BỘ LỌC THÔ.** Mọi đối tượng nó trả về phải qua
   `gro--cham-that-p` (hình học chính xác) mới tính là vật cản:

   | Loại | Cách kiểm |
   |---|---|
   | `LINE` | đoạn thẳng cắt hộp — Liang-Barsky (`gro--doan-cat-hop-p`) |
   | `LWPOLYLINE` (phẳng XY) | **từng cạnh**, cạnh cung chia 8 dây cung (`gro--cung-cat-hop-p`) — không dùng hộp bao cả polyline (viền tấm dài sẽ trùm cả vùng trống) |
   | `CIRCLE` (phẳng XY) | chỉ nét viền: điểm gần nhất của hộp ≤ r ≤ góc xa nhất |
   | còn lại (TEXT, MTEXT, DIMENSION, INSERT, HATCH, ARC…) | giao hộp bao (thiên về an toàn) |
   | không lấy được hộp bao | **coi là chạm** — không được bỏ sót vật cản thật |

   Đã kiểm offline 13 ca bằng bản chép thuật toán sang PowerShell, tất cả đạt, gồm: cạnh
   tấm cách 12 → không chạm; chạm mép → chạm; nửa vòng cung đỉnh cách 7 → không chạm, lấn
   vào 8 → chạm; bulge âm cong ngược chiều; đường tròn bao trùm cả khối → không chạm nét.

   `grovc` in thêm lý do loại `(ssget bat NHAM - khong cham that)`, để thấy ngay các đối
   tượng bị lỗi "mờ" này bắt nhầm.

   > **Chưa chạy trên bản vẽ thật.**

42. **Ghi chú 1 tấm: ưu tiên TRÊN, CĂN GIỮA cụm chữ** (người dùng yêu cầu, kèm ảnh so sánh).
   Trước đó ứng viên đầu tiên của ghi chú 1 tấm là ô **trên, sát trái** (ô đầu của dãy trượt
   trái → phải). Bango vì thế lệch hẳn về góc trên-trái, trong khi người dùng muốn nó nằm
   giữa phía trên ghi chú.

   `gro--vi-tri-tren-giua` sinh **ứng viên số 0** cho ghi chú 1 tấm: tâm ngang khối = tâm
   ngang cụm ghi chú, đáy khối = đỉnh cụm + `ho`. Cạnh `0` ("trên"). Vướng thì quay lại xét
   **từ đầu** danh sách trượt cũ: trên (trái → phải) → dưới → trái → phải.

   Ô giữa cạnh trên trùng với ô trượt thứ 3/5, nên nó bị thử lại một lần. Việc này vô hại và
   được giữ để danh sách trượt nguyên vẹn, dễ đọc.

   Thứ tự đầy đủ sau mục 40 + 42:

   | Ghi chú | Thứ tự |
   |---|---|
   | 1 tấm | **trên căn giữa** → trên (trái → phải) → dưới → trái → phải |
   | ≥ 2 tấm | cột trái căn giữa vùng dòng số lượng → trượt dọc cạnh trái → trên → dưới → phải |

   Đã kiểm công thức bằng số: khối rộng 80 trên cụm X 0..600 → khối X 260..340 (tâm 300),
   đáy = đỉnh cụm + 12. **Chưa chạy trên bản vẽ thật.**

44. **Ghi chú 1 tấm: trên-giữa bị chặn thì ưu tiên BÊN TRÁI** (người dùng yêu cầu, kèm ảnh).
   Ảnh: ghi chú `t=9㎜ 1枚` nằm kẹp giữa hai dòng kích thước `1,130`. Ô trên-giữa vướng
   dòng kích thước phía trên, nên theo mục 42 bango trượt trên cạnh trên và dừng ở **trên-phải**.
   Người dùng muốn vị trí dự phòng đầu tiên là **bên trái** cụm chữ.

   Dùng lại nguyên cơ chế của ghi chú ≥ 2 tấm (mục 39/40): ô **cột trái** (`gro--vi-tri-cot`)
   rồi các ô trượt dọc cạnh trái, đặt lên trước trên/dưới/phải. Nay áp dụng cho **mọi** ghi
   chú. Ghi chú 1 tấm vẫn giữ ô trên-giữa đứng đầu:

   | Ghi chú | Thứ tự |
   |---|---|
   | 1 tấm | trên căn giữa → **trái, căn giữa theo chiều dọc cả cụm** → trượt dọc cạnh trái → trên (trái → phải) → dưới → phải |
   | ≥ 2 tấm | cột trái căn giữa vùng dòng số lượng → trượt dọc cạnh trái → trên → dưới → phải |

   Tâm dọc (`yc`) của ô trái: ghi chú ≥ 2 tấm lấy **vùng các dòng số lượng**; ghi chú 1 tấm lấy
   **cả cụm ghi chú**. Một tấm thì không có cặp 本図/逆図 nào để ngang hàng theo, nên
   căn theo cả cụm chữ tự nhiên hơn.

   Bango 1 tấm đặt ở ô trái được đếm vào `cot trai=` trong báo cáo (cạnh 4), không vào `trai=`.
   `trai=` chỉ đếm các ô trượt dọc cạnh trái.

   > **Chưa chạy trên bản vẽ thật.**

45. **Ghi chú 1 tấm: chèn DƯỚI-GIỮA vào vị trí ưu tiên số 2** (người dùng yêu cầu). Sửa trực
   tiếp mục 44: thứ tự mong muốn là **trên giữa → dưới giữa → trái giữa → rồi mới lần lượt
   các vị trí còn lại**.

   | Ghi chú | Thứ tự (bản 45 — đang dùng) |
   |---|---|
   | 1 tấm | **trên giữa → dưới giữa → trái giữa** (ô cột) → trượt dọc cạnh trái → các cạnh còn lại |
   | ≥ 2 tấm | cột trái căn giữa vùng dòng số lượng → trượt dọc cạnh trái → các cạnh còn lại (mục 39/40/44, **không đổi**) |

   Ba ô đầu đều **căn giữa** theo cạnh tương ứng, nên khi vùng quanh ghi chú trống thì bango
   luôn nằm cân đối — đó là điều người dùng nhìn thấy đầu tiên.

   ### Hai thay đổi cài đặt kèm theo, cần biết khi sửa tiếp

   1. **`gro--vi-tri-bango` nay trả về 4 ô GIỮA của 4 cạnh TRƯỚC, rồi mới đến phần trượt.**
      Trước đây nó trả về lần lượt trọn từng cạnh (cạnh 0 đủ 5 ô, rồi cạnh 1 đủ 5 ô…). Nay:

      ```lisp
      (append (list (car l0) (car l1) (car l2) (car l3))   ; 4 o GIUA
              (cdr l0) (cdr l1) (cdr l2) (cdr l3))         ; phan con lai
      ```

      Mỗi `lN` đã được `gro--sap-tu-giua` sắp **từ giữa ra hai biên**, nên `(car lN)` **chính là
      ô giữa** của cạnh đó. Hàm mới `gro--dau-canh` khai thác đúng tính chất này: lấy ứng viên
      đầu tiên của một cạnh là lấy được ô giữa cạnh ấy, không phải tính lại toạ độ.

      > Ràng buộc ngầm: **nếu sau này đổi `gro--sap-tu-giua` hoặc thứ tự trong
      > `gro--vi-tri-bango` thì `gro--dau-canh` lặng lẽ trả về ô khác.** Đây là chỗ dễ hỏng
      > trong im lặng nhất của cả lớp đặt vị trí.

   2. **Thứ tự ưu tiên viết TƯỜNG MINH trong `gro--dat-cot`**, không dựa vào thứ tự nội bộ của
      `gro--vi-tri-bango`. Lý do rất thực tế: đây là thứ người dùng đổi nhiều lần nhất
      (mục 33 → 39 → 42 → 44 → 45). Để ở một chỗ thì sửa một chỗ, và đọc code là thấy ngay
      thứ tự chứ không phải lần theo hai hàm.

   Kiểm offline (`sim_uutien.py`, 13 trường hợp, tất cả đạt) — mô phỏng **đúng kiến trúc hiện
   tại** của `gro--dat-cot` chứ không phải một bản dựng lại:
   - ba ứng viên đầu của ghi chú 1 tấm đúng là cạnh `0 → 1 → 4`;
   - ô trên-giữa và dưới-giữa **cùng tâm ngang** với cụm; đúng khoảng hở `ho` trên/dưới;
   - mép phải ô cột = trái cụm − `ho`;
   - trống → trên; chặn trên → **dưới**; chặn trên+dưới → trái (cột); chặn cả ba → phải;
   - chặn **đúng mỗi ô trên-giữa** thì vẫn xuống dưới-giữa (không trượt ngang cạnh trên trước) —
     đây chính là hành vi người dùng yêu cầu, và là chỗ khác mục 44;
   - ghi chú ≥ 2 tấm: ứng viên đầu vẫn là cột trái, **không** bị chèn trên/dưới-giữa.

   > **Chưa chạy trên bản vẽ thật.** Mục 44 cũng chưa, nên hai mục này sẽ được nghiệm thu
   > cùng lúc: nhìn dòng đếm `tren= duoi= trai= phai= cot trai=` và vài ghi chú 1 tấm ở
   > vùng trống (phải nằm **trên, căn giữa**).

46. **TEXT bango dùng kiểu chữ riêng `guro`, chưa có thì lisp TỰ TẠO** (người dùng yêu cầu,
   kèm ảnh hộp thoại Text Style Manager).

   Trước đây `entmake` của TEXT bango **không ghi group 7** nên nó ăn theo `TEXTSTYLE` hiện
   hành — mỗi bản vẽ một kiểu, người dùng phải chỉnh lại. Nay luôn là `guro`.

   Thông số đúng theo ảnh, và đây cũng là **định nghĩa chuẩn** khi phải tạo lại bằng tay:

   | Mục trong hộp thoại | Giá trị | Group DXF |
   |---|---|---|
   | Height | `0.0000` | 40 |
   | Width factor | `1.0000` | 41 |
   | Oblique angle | `0.0000` | 50 |
   | Font Name | Arial (`arial.ttf`) | 3 |
   | Bigfont | (trống) | 4 |
   | Annotative / Vertical | không | 70 = 0 |
   | Backwards / Upside down | không | 71 = 0 |

   `Style = Regular` và `Language = Western` trong ảnh là **giá trị mặc định** khi bản ghi
   STYLE không có dữ liệu font mở rộng, nên không cần ghi thêm gì.

   > ### HEIGHT = 0 KHÔNG PHẢI chỉ để giống ảnh
   > Kiểu chữ có **chiều cao cố định** (group 40 ≠ 0) sẽ **đè bẹp group 40 của từng TEXT**.
   > Hậu quả dây chuyền: mọi bango ra cùng một cỡ chữ bất kể chiều cao ghi chú (đang là
   > `h*1.5`), và vì bước đặt vị trí **đo hộp bao thật** của khối (mục 33) nên cả layout
   > cũng lệch theo. Vì thế khi kiểu chữ `guro` **đã có sẵn** mà Height ≠ 0, lệnh
   > **cảnh báo** và nói rõ phải sửa Height về 0 — chứ **không tự ý sửa** kiểu chữ của
   > người dùng.

   **Hai đường tạo, thử lần lượt** (`gro--bao-dam-style`):
   1. `entmake` bản ghi bảng STYLE — cách chuẩn, không phụ thuộc dòng lệnh.
   2. `entmake` bị từ chối thì thử ActiveX `vla-add` trên collection `TextStyles`
      (`gro--style-activex`), bọc `vl-catch-all-apply`.
   3. Cả hai hỏng → in cảnh báo rõ rồi **dùng kiểu chữ hiện hành**, lệnh vẫn chạy tiếp.

   Lý do có đường 2: `entmake` trên **bảng symbol** khác hẳn `entmake` trên entity thường và
   **chưa được chứng minh trên ZWCAD** — cả file này lẫn `xuat_csv_v5.0.1 ZWCAD.lsp` chưa
   dùng nó lần nào. Đường 2 đi qua lớp COM mà file này đã dùng nhiều (`vla-GetBoundingBox`,
   `vla-put-color`, `vla-startundomark`).

   Gọi **một lần** trước vòng lặp, cạnh chỗ tính `style` của F-name — `tblsearch`/`entmake`
   chỉ cần chạy đúng một lần cho cả bản vẽ.

   **Chỉ áp cho TEXT bango.** MTEXT mã khắc và TEXT F-name **giữ nguyên** như cũ (F-name dùng
   kiểu `Text` hoặc `TEXTSTYLE` hiện hành). Muốn mở rộng thì thêm `(cons 7 style-bg)` vào hai
   chỗ `entmake` còn lại. `bg` / `mk` / `FN` không đụng tới.

   > **Chưa chạy trên bản vẽ thật** — đặc biệt là câu hỏi `entmake` bảng STYLE có được ZWCAD
   > 2026 chấp nhận không. Nghiệm thu: chạy `gro` trên bản vẽ **chưa có** kiểu `guro`, xem
   > dòng in ra là `Da tao kieu chu "guro" (font arial.ttf, ...)` hay
   > `... bang ActiveX ...` hay `*** KHONG tao duoc ...`, rồi mở Text Style Manager đối chiếu
   > với ảnh.

47. **Bản đầu của mục 46 KHÔNG ăn — bango vẫn ra kiểu chữ `Standard`** (người dùng báo sau khi
   chạy thật). Hai lỗi thiết kế, **mỗi cái riêng lẻ đã đủ** gây ra đúng triệu chứng đó.

   ### Lỗi 1 — quyết định bằng GIÁ TRỊ TRẢ VỀ của lệnh tạo, không bằng trạng thái thật

   Bản đầu viết `(cond ((entmake …) …) ((gro--style-activex ten) …) (t <thất bại>))`, và
   `gro--style-activex` gói **cả 5 bước vào một `lambda`**:

   ```lisp
   (vla-add …)                  ; tao duoc kieu chu
   (vla-put-fontfile sty …)     ; <- nem loi o day
   ```

   `vl-catch-all-apply` bắt lỗi của bước 2 → **cả lambda coi như hỏng** → hàm trả `nil` →
   rơi xuống nhánh cuối → dùng `TEXTSTYLE` hiện hành = `Standard`. **Trong khi kiểu chữ
   `guro` đã nằm trong bản vẽ rồi.** Cùng kiểu lỗi nếu `entmake` trả `nil` nhưng vẫn tạo
   được.

   **Sửa:** sau mỗi lần thử đều hỏi lại `tblsearch` — đó là **trạng thái thật của bản vẽ**,
   còn giá trị trả về chỉ là lời khai của lệnh tạo. Và mỗi thuộc tính ActiveX **bắt lỗi
   riêng**, hỏng một thuộc tính không được kéo theo mất cả kiểu chữ.

   > **Luật rút ra, áp cho mọi chỗ khác:** khi một thao tác có **tác dụng phụ lên bản vẽ**,
   > nghiệm thu bằng cách **đọc lại bản vẽ**, không bằng giá trị trả về. Đây là họ hàng của
   > luật ở mục 29 (lệnh không đọc lại đầu ra của chính nó) — cùng gốc: **tin số đo, đừng
   > tin lời khai.**

   ### Lỗi 2 — `(cons 7 …)` trong `entmake` chưa chắc được ZWCAD tôn trọng

   Kể cả khi kiểu chữ đã tồn tại, `entmake` một TEXT với `(cons 7 "guro")` có thể **lặng lẽ
   bỏ qua** và cho ra kiểu chữ hiện hành — không lỗi, không cảnh báo. Đây là cách viết
   **chưa từng được chứng minh** trên ZWCAD: dòng `(cons 7 style)` của F-name tồn tại từ lâu
   nhưng chưa ai kiểm nó có ăn hay không, vì `style` thường trùng luôn với `TEXTSTYLE`.

   **Sửa:** `gro--dat-style` **đọc lại group 7** của TEXT vừa tạo; lệch thì sửa bằng
   `entmod`, rồi **đọc lại lần nữa** để xác nhận. Đếm số cái không đặt được và in vào báo
   cáo: `N bango (layer 0), kieu chu guro` — hoặc `… *** k CAI DAT KHONG DUOC ***`.

   Nhờ vậy kết quả **đúng bất kể ZWCAD tôn trọng cơ chế nào**: `entmake` ăn thì thôi, không
   ăn thì `entmod` vá.

   ### Lệnh chẩn đoán `grostyle` (chỉ có ở bản đầy đủ)

   Trả lời dứt điểm ba câu hỏi theo đúng thứ tự dây chuyền, thay vì đoán:

   | Bước | Đo cái gì |
   |---|---|
   | 1 | bản vẽ đã có `guro` chưa, thông số ra sao (font / Height / Width / Oblique / flags) |
   | 2 | `entmake` bảng STYLE có chạy không; không thì ActiveX có chạy không — **kiểm bằng `tblsearch` sau mỗi lần** |
   | 3 | tạo một TEXT thử, **đọc lại group 7**, thử `entmod` nếu lệch, rồi **`entdel` xoá ngay** |

   Bước 3 là bước quan trọng nhất và là thứ bản đầu thiếu hẳn.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: nạp bản đầy đủ, chạy `grostyle`, đọc ba
   > bước. Sau đó chạy `gro` và xem dòng `… bango (layer 0), kieu chu guro`.

59. **`gro` TỰ ZOOM cho thấy hết vùng làm việc trước khi đặt bango** (người dùng yêu cầu).

   Mục 58 đã bỏ `ssget "_C"` khỏi khâu **đọc** nội dung, nhưng khâu **đặt** bango
   (`gro--dem-va-cham` dò vật cản) vẫn phải dùng nó — và vẫn chỉ thấy phần đang hiện trên
   màn hình. Hệ quả: zoom sát vào một góc rồi chạy thì bango có thể đè lên hình ở chỗ khác.

   Cách chữa đơn giản nhất là **tự zoom**, chứ không bắt người dùng nhớ:

   ```
   Da zoom cho thay het vung lam viec (de do vat can dung).
   ```

   Vùng zoom = hộp bao của **các khung bao** (hoặc của toàn bộ ghi chú + dòng số lượng nếu
   quét chọn thủ công), nới thêm **5%** mỗi phía để vật cản sát mép cũng nằm trong tầm nhìn.
   Chạy ở bước **5b**, ngay trước bước tạo đối tượng.

   ### Ba quyết định cài đặt

   1. **Dùng `(command "_.ZOOM" "_W" …)`, KHÔNG dùng `vla-zoomwindow`.** Nếu bản ZWCAD nào
      đó không có hàm `vla` đó thì lỗi `undefined function` **không bọc được** bằng
      `vl-catch-all-apply` (mục 3) — tức là làm chết cả lệnh. ZOOM thì bản CAD nào cũng có.

   2. **Gọi xong phải ĐO LẠI khung nhìn**, không tin vào việc gọi thành công —
      `gro--khung-nhin` dựng hộp bao tầm nhìn từ `VIEWCTR` / `VIEWSIZE` / `SCREENSIZE`, rồi
      kiểm nó có chứa trọn vùng làm việc không. Không chứa thì **báo rõ** là bango có thể đè
      hình. Đúng bài học mục 47: **tin số đo, đừng tin lời khai.**

   3. **Tắt `CMDECHO` trước khi gọi ZOOM**, nếu không lệnh ZOOM đổ chữ ra dòng lệnh giữa báo
      cáo. Giá trị cũ đã lưu ở `old-cmdecho` từ đầu lệnh.

   > **Tầm nhìn bị đổi và KHÔNG khôi phục lại.** Cố ý: sau khi chạy xong người dùng cần nhìn
   > kết quả trên cả vùng vừa đánh số, chứ không phải quay về chỗ zoom cũ.

   ### Lỗi bắt được nhân tiện: `vl-catch-all-apply` thiếu tham số

   `vl-catch-all-apply` **bắt buộc hai tham số**: hàm **và danh sách đối số**. Hai chỗ trong
   file chỉ truyền một — chỗ zoom vừa viết, và **đường ActiveX tạo kiểu chữ ở mục 47**.

   Nguy hiểm ở chỗ: lỗi "too few arguments" xảy ra ngay tại **chính chỗ đang đi bắt lỗi**,
   nên nó không được bắt mà làm chết cả lệnh. Đường ActiveX của mục 47 chưa bao giờ chạy
   (vì `entmake` luôn thành công trước) nên lỗi này nằm im từ đó tới giờ.

   Đã rà **toàn bộ 16 lời gọi** `vl-catch-all-apply` trong file bằng bộ đếm đối số thật
   (không phải đếm bằng mắt): nay tất cả đều đúng hai tham số.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: zoom sát vào một góc rồi chạy `gro` — màn
   > hình phải tự nhảy ra thấy hết vùng, và in dòng `Da zoom cho thay het...`.

60. **Ghi TÊN BẢN VẼ vào từng khung bao** (người dùng yêu cầu).

   | | |
   |---|---|
   | Nội dung | `don` — tên bản vẽ đã cắt đuôi `-s` / `-a` (chính chuỗi dùng cho cột `N` của CSV) |
   | Chiều cao | **gấp 1.5 lần bango** → `hmax * 1.5 * 1.5` = `hmax * 2.25` |
   | Layer / màu / kiểu chữ | **y như bango**: layer `0`, màu 1 (đỏ), kiểu chữ `guro` |
   | Vị trí | ưu tiên **góc trên bên trái**, chỉ thử trong **dải 1/5 trên** của khung |
   | Không có chỗ sạch | **ép thẳng vào góc trên bên trái** (người dùng chốt) |

   Mỗi khung bao được một text. Chạy ở bước **6b**, **sau** vòng ghi chú — lúc đó mọi khối
   bango đã nằm đúng chỗ nên chúng được tính là vật cản, tên bản vẽ tự tránh.

   ### Ứng viên: 5 cột × 3 hàng, duyệt trên→dưới, trái→phải

   Thứ tự duyệt làm cho **ứng viên đầu tiên chính là góc trên-trái** — không cần luật riêng
   cho "ưu tiên góc trên trái". Lề vào từ viền khung = `0.4 × chiều cao chữ`.

   Dùng lại nguyên lớp va chạm của bango (`gro--dem-va-cham`), và dùng lại cách làm của mục
   33: **tạo text trước rồi ĐO hộp bao thật**, vì chiều rộng phụ thuộc số ký tự và font —
   ước lượng là sai.

   ### Lỗi tôi mắc khi viết: nhầm dải 1/5 TRÊN thành 1/5 DƯỚI

   Viết `(+ ymin (/ h 5.0))` thay vì `(- ymax (/ h 5.0))`. Hậu quả nếu lọt: với khung cao,
   các hàng ứng viên sẽ tụt gần xuống **đáy** khung thay vì bám dải trên — vẫn chạy, vẫn có
   chữ, chỉ sai chỗ. Loại lỗi mà nhìn code không thấy, phải dựng số ra mới lộ.

   Bắt được nhờ mô phỏng có hẳn một phép kiểm **"mọi ứng viên đều nằm trong dải 1/5 trên"**
   chứ không chỉ kiểm "ứng viên đầu đúng góc trên-trái" — cái sau vẫn đúng kể cả khi công
   thức sai.

   Kiểm offline (`sim_tenbv.py`, 14 ca, tất cả đạt): chiều cao đúng gấp 1.5 lần bango; đủ 15
   ứng viên; ứng viên đầu đúng góc trên-trái; ứng viên 2 lệch phải cùng hàng; ứng viên 6
   xuống hàng dưới; **mọi ứng viên trong dải 1/5 trên**; không ứng viên nào thò ra ngoài
   khung; chặn góc trên-trái thì nhảy sang chỗ khác; chặn hết thì ép về góc trên-trái và
   **báo**; và khung thấp (dải 1/5 mỏng hơn chữ) thì các hàng đè lên nhau chứ **không tụt
   xuống đáy**.

   Báo cáo: `Ten ban ve: dat 13/13 khung  [xxxxxx-1]`, kèm `(N cai khong co cho sach -> ep
   vao goc tren-trai)` khi có.

   > **Quét chọn thủ công (không có khung bao) thì BỎ QUA** và nói rõ — không có khung thì
   > không có chỗ nào để ghi tên vào.

   > **Chưa chạy trên bản vẽ thật.**

170. **Đặt mã khắc + F-name vào trong hình chuyển HOÀN TOÀN sang C#; chỉ còn chế độ 2, bỏ chế độ 1 và ô chọn chế độ** (người dùng yêu cầu, 28/10/2026).

   ### Phân công

   | Bên | Làm gì | Ở đâu |
   |---|---|---|
   | lisp | **chọn tấm** (1 bango, GC rỗng, khung `Tam don (1 hinh)*` có hình thật) — dữ liệu tấm / khung chỉ có bên lisp; gửi sang C# | `gro--dat-ma-khac-vao-hinh` (+ `gro--mk-bb-str`, `gro--mk-so`) |
   | C# thuần (có test) | biên ngoài (vòng mặt ngoài, tách chữ T, chọn vòng khớp hộp hình — mục 119), chỉ mục 24 dải ngang, **trượt từ góc trên-trái** (mục 122/136/138), tấm chung hình, cụm đã đặt + text khắc chữ là vật cản, chuỗi báo cáo | `ZwCadPlugin/Core/DatMaKhac.cs` |
   | C# CAD | lấy mẫu nét 0-KAKOU / 3-KEGAKI / TAPU_TMT ở Model Space (bỏ bản chụp) như `gro--ent->diem`; dời / xoay / thu nhỏ cụm bằng **một** ma trận (thu nhỏ quanh tâm cũ → xoay → dời); cờ `MK_THU_NHO` / `MK_DE_KEGAKI` / `MK_CHUA_DAT` vào XData `GRO_METADATA` | `ZwCadPlugin/CadServices/DatMaKhacCad.cs`, hàm lisp `GRO_NET_DAT_MK` ở `ZwCadPlugin/DatMaKhacCommands.cs` |

   **Giao thức:** `(gro_net_dat_mk "T|bango|x1,y1,x2,y2|cx1,cy1,cx2,cy2|handle-bango|h1;h2;..." ... "V|x1,y1,x2,y2" ...)` — mỗi tham số một chuỗi ASCII, số ghi bằng `(rtos v 2 6)`. Dòng `T` theo đúng thứ tự đặt (tấm trước là vật cản tấm sau); hộp cụm do lisp đo (`gro--bb-ds-ename`) để giữ y hệt cách đo cũ. Trả chuỗi báo cáo **đúng định dạng cũ** (`Dat ma khac vao hinh: a/b tam (thu nho 1/2: x, de KEGAKI: y)` + dòng `!!!`) — `BaoCaoGro` / `ThongTinGro` đọc được, không phải sửa; dòng `[MK]` từng tấm in như cũ (dòng thời gian ghi thêm `(.NET)`).

   **DLL cũ (chưa có hàm):** lisp gọi `gro--nap-net` thử nạp lại; vẫn không có thì báo `!!! Chua dat ma khac vao hinh: ZwCadPlugin.dll chua co ham gro_net_dat_mk (cap nhat DLL) - move thu cong.` và **không dời gì**. Lỗi .NET cũng thành dòng `!!!` trong báo cáo.

   ### Đã xoá

   - Lisp: 22 hàm của chế độ 1 / 2 (`gro--polylabel`, `gro--tim-cho-cum`, `gro--mk2-*`, `gro--hcn-xoay-cham-p`, `gro--mk-ap-cum`, `gro--gan-co-tam`…). **Giữ** các hàm hình học mà mục 156 (đổi text khắc chữ) còn dùng: `gro--mk-quet-net`, `gro--ent->diem`, `gro--hinh-tam-doan`, `gro--mk-vong-ngoai`, `gro--mk-tach-T`, `gro--mk-chon-ngoai`, `gro--cm-tao`, `gro--trong-thit-p`…
   - Biến `gro--mk-che-do`, khoá registry `mk_che_do`, tham số `def-che-do` của `gro-get-input`.
   - Hộp thoại .NET: 2 nút "Chế độ 1 / Chế độ 2"; `GroSettings.CheDoMk`, `GroThamSo.ChuanCheDo`. Giao thức dialog còn **11** giá trị (lisp cũ gửi thừa tham số 12 thì bị bỏ qua).

   ### Khác bản lisp (có chủ ý)

   - Sắp đầu mút theo X khi dựng đỉnh (vòng mặt ngoài) dùng sắp **ổn định**: `vl-sort` của lisp có thể bỏ phần tử cùng X (mục 22) — không chép lỗi đó.
   - Hộp bao nét dùng `GeometricExtents` (lisp: `vla-GetBoundingBox`) — chỉ để lọc nét quanh tấm.

   ### Kiểm chứng

   - `dotnet run --project ZwCadPlugin.Tests`: **454/454** (28 ca mới mục 170 — vị trí đầu tiên tính tay khớp `(22.25, 87.75)`, lỗ góc trên-trái, tấm hẹp phải xoay 90°, cụm quá lớn → `CUM QUA LON`, KEGAKI dày đặc → bỏ KEGAKI, thu nhỏ 1/2, tấm chung hình, text khắc chữ là vật cản, biên 4 LINE + nét chia ngăn, tách chữ T, vòng lệch hộp hình, đọc dữ liệu lisp, cờ XData; bỏ 3 ca cũ của chế độ).
   - Build plugin (Debug) **0 lỗi, 0 cảnh báo** — build vào thư mục tạm; sau đó chạy `build.ps1` (28/10) — DLL mới **đã chép** vào `C:\ZwCadPlugin\ZwCadPlugin.dll`.
   - Lisp: ngoặc cân bằng, 406 → 386 form (−22 hàm +2), không `defun` trùng, không còn chỗ gọi tới hàm đã xoá, CRLF. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc170._ls`.

   > **ĐÃ NGHIỆM THU TRÊN ZWCAD (28/10, sau khi sửa lỗi làm tròn bên dưới):** bản vẽ 65 tấm đơn / mirror -> `Dat ma khac vao hinh: 65/65 tam (thu nho 1/2: 2, de KEGAKI: 1)`, C# quét 1034 nét (trước 914). Ghi chú cũ: phải build + cập nhật DLL (`ZwCadPlugin/build.ps1` — bước PostBuild tự chép DLL vào `C:\ZwCadPlugin`; đóng ZWCAD trước) rồi chạy `gro`. Nghiệm thu: dòng `[MK]` từng tấm và dòng `Dat ma khac (.NET): ...`, cụm mã khắc + F-name nằm trong hình như bản lisp chế độ 2; cột Ghi chú DST hiện đúng cờ.

   ### Lỗi lần chạy thật đầu tiên: 14/65 tấm — cạnh tấm rơi vì LÀM TRÒN (đã sửa)

   Chạy thật: chỉ 14/65 tấm vào hình, dòng `[MK]` gần như toàn `ngoai thit` (vd `cham net 0, ngoai thit 258326`). Lần ra bằng file dữ liệu `%TEMP%\gro_dat_mk_dump.txt` (C# ghi mỗi lần chạy) + phát lại ngoài CAD `dotnet run --project ZwCadPlugin.Tests -- phatlai <file> [bango]`:

   - Tấm 13 (549×200) C# chỉ quét được cạnh dưới + phải; cạnh trên (22267) + trái (22268) **vẫn nằm trên 0-KAKOU đúng chỗ** (LIST xác nhận) nhưng bị lọc bỏ.
   - Nguyên nhân: lisp gửi hộp hình bằng `(rtos v 2 6)` -> `X1 = 6475.01388` trong khi cạnh trái thật `x = 6475.0138797…`; cạnh tấm nằm **đúng trên mép** hộp hình nên lệch 3·10⁻⁷ mm là đủ thành "không chạm hộp". 147 nét 0-KAKOU cả bản vẽ rơi như vậy -> không khép được biên ngoài -> 8 tia báo ngoài thịt mọi vị trí. Lisp cũ dùng số thực đầy đủ nên không dính.
   - Sửa: C# lọc nét theo hộp hình **nới 1 mm** (như `bb1` của lisp ở các bước sau); lisp gửi `(rtos v 2 8)`. Phát lại dữ liệu thật có bổ sung các nét bị rơi: **65/65**.
   - Kèm theo: hộp bao nét tính từ điểm mẫu (toạ độ hiện tại), `GeometricExtents` chỉ dự phòng; lấy mẫu polyline lỗi thì dự phòng mẫu theo chiều dài; đếm + báo nét không lấy mẫu được; file dữ liệu ghi thêm dòng `E` cho mọi entity chạm hộp tấm (loại, layer, handle, lý do bị loại).

   > **Bài học:** dữ liệu hình học đi qua chuỗi (lisp -> .NET) **mất độ chính xác** — mọi phép so "chạm / nằm trong" ở đầu nhận phải có dung sai, nhất là với hộp bao mà nét nằm đúng trên mép.

187. **Lề mã khắc 0.1 mm chỉ khi CẢ HAI cạnh tấm < 50 mm** (người dùng yêu cầu, 29/10/2026).

   - Trước: đặt mã khắc vào hình (C#, `Core/DatMaKhac.cs` `DatMotTam`, mục 170) dùng khe từ cụm mã khắc tới nét = **0.1 mm** khi cạnh **nhỏ hơn** của tấm < 100 mm, còn lại 1.0 mm → tấm dài mà hẹp (vd 60 × 1000) cũng bị đặt sát mép 0.1 mm.
   - Nay: hàm `DatMaKhac.LeMep(bb)` = 0.1 chỉ khi **cả chiều rộng lẫn chiều cao** < **50 mm** (bản đầu 100 × 100, người dùng giảm về 50 × 50 cùng ngày); còn lại 1.0. Bước trượt (`nho / 80`) không đổi.
   - Test **556/556** (6 ca: 40×1000 → 1.0, 1000×40 → 1.0, 40×45 → 0.1, 60×80 → 1.0, 50×40 → 1.0, tấm 30×200 cộng khe 1.0). Build 0 lỗi 0 cảnh báo, DLL **đã chép** `C:\GROZWCAD\GROZWCAD.dll` (29/10 14:43).

   > **Chưa chạy trong ZWCAD.** Nghiệm thu: tấm hẹp dài hoặc tấm nhỏ có một cạnh ≥ 50 — cụm mã khắc cách mép ≥ 1 mm; tấm nhỏ cả hai cạnh < 50 vẫn lề 0.1. Tấm hẹp có thể chuyển sang thu nhỏ 1/2 hoặc không đặt được nhiều hơn trước (khe lớn hơn) — xem dòng `Dat ma khac vao hinh: a/b tam`.

188. **Khắc chữ (mục 156): chữ trong hình viết ở layer khác không được đổi sang 3-KEGAKI** (người dùng báo kèm ảnh, 29/10/2026).

   - Triệu chứng: tấm có ghi chú `文字ｹｶﾞｷ線 t=9mm / 本図 1枚`, chữ `V14` nằm trong hình nhưng **không** được chuyển sang `3-KEGAKI`.
   - Nguyên nhân: `gro--doi-text-khac-chu` chỉ lấy TEXT/MTEXT trên **`1-SUNPOU` + layer gốc** (`gro--layer-xln`). Chữ khắc khách viết ở layer khác → không bao giờ được xét.
   - Sửa: lấy TEXT/MTEXT **mọi layer** (`ssget "X"` dạng `<OR` — mục 13), rồi loại: bản chụp (`GRO_CHUP_COPY`) / không gian giấy, layer đầu ra của GRO (`gro--layer-dau-ra-p`: 2-F-NAME, 3-KEGAKI, CANH_BAO*, Khung_IN), layer `*_CHUP`, bango / tên bản vẽ (`gro--bango-ent-p`), ghi chú **và dòng số lượng** (trước chỉ loại ghi chú). Các phép còn lại giữ nguyên: hộp chữ nằm trong hộp hình ±0.5, tâm trong thịt, không phải chữ kiểu kích thước.
   - Lisp: không thêm hàm, ngoặc cân bằng, CRLF, ASCII không đổi. Bản trước: `backup/Guro ZWCAD(gro).lsp.truoc-muc188._ls`.

   > **Chưa chạy trong ZWCAD.** Nghiệm thu: chạy `gro` trên bản vẽ đó — `V14` phải sang `3-KEGAKI`, dòng báo cáo `Khac chu: doi N text trong hinh sang 3-KEGAKI - bango ...`, và mã khắc của tấm đó tránh chữ `V14` (vật cản). Nếu có chữ khác trong hình bị đổi nhầm (vd ghi chú phụ không có `t=`), báo lại để thêm luật loại.

189. **Khắc chữ: chữ THÒ RA NGOÀI mép hình không được đổi sang 3-KEGAKI — nay đổi + cảnh báo** (người dùng báo kèm ảnh, 29/10/2026).

   - Triệu chứng: tấm `文字ｹｶﾞｷ線 t=9mm` (bango 3), chữ `2C-W4,W8,W9` nằm trong hình nhưng đuôi chữ thò ra ngoài mép phải → không được đổi.
   - Nguyên nhân: `gro--doi-text-khac-chu` đòi hộp chữ nằm **trọn** trong hộp hình (±0.5 mm).
   - Sửa (người dùng chốt):
     - Nhận chữ khi hộp chữ **giao** hộp hình và **tâm chữ** nằm trong hộp hình + trong thịt (`gro--trong-thit-p`; không dò được biên ngoài thì chỉ theo hộp hình). Phép loại chữ kiểu kích thước giữ nguyên.
     - **Cảnh báo** khi chữ đã đổi mà (a) hộp chữ không nằm trọn trong hộp hình, hoặc (b) hộp chữ **chạm nét cắt 0-KAKOU của hình** (`gro--cham-that-p` với từng nét của hình — LINE / LWPOLYLINE / CIRCLE chính xác, loại khác theo hộp bao): dòng lệnh in `!!! Khac chu bango N: text [..] nam mot phan NGOAI hinh / CHAM net cat 0-KAKOU ... KIEM TRA LAI vi tri chu (muc 189)`, báo cáo (tab Thông Tin) thêm dòng `!!! Khac chu: k text nam mot phan ngoai hinh / cham net cat 0-KAKOU - kiem tra: bango ...`.
   - Lisp: không thêm hàm, ngoặc cân bằng, CRLF, ASCII không đổi. Bản trước: `backup/Guro ZWCAD(gro).lsp.truoc-muc189._ls`.

   > **Chưa chạy trong ZWCAD.** Nghiệm thu: bản vẽ trong ảnh — `2C-W4,W8,W9` sang 3-KEGAKI và có dòng `!!! Khac chu bango 3 ... NGOAI hinh`. Chữ nằm gọn trong hình, không chạm nét → đổi mà không cảnh báo.

190. **Khắc chữ: cảnh báo tiếng Việt có dấu + đưa vào cột Ghi chú; tăng tốc bước khắc chữ** (người dùng yêu cầu, 29/10/2026).

   - **Chạy thật mục 189 (bản vẽ Drawing1, 14 tấm khắc chữ):** đổi 14 chữ sang 3-KEGAKI; 6 chữ `2C-W4,W8,W9` / `2C-W5,W18,W19` thò ra ngoài hình + chạm nét cắt → cảnh báo đúng (bango 1–6, 13–18). Nhưng tab Thông Tin hiện dòng ASCII không dấu, và bước `khac chu (156)` mất **9.02 s** (60 % GRO).
   - **Tiếng Việt có dấu:** lisp vẫn ghi ASCII (luật CP932); C# `ThongTinGro.Dich` thêm 3 mẫu: `Khac chu: doi N text ...` → "Khắc chữ: đã đổi N chữ trong hình sang ケガキ (3-KEGAKI) — bango ...", `!!! Khac chu: N text nam mot phan ngoai hinh / cham net cat 0-KAKOU ...` → "!!! Khắc chữ: N chữ nằm một phần ngoài hình hoặc chạm nét cắt 0-KAKOU — cần kiểm tra: bango ...", `!!! Tam khac chu (N tam) khong co text nao ...`.
   - **Cột Ghi chú (bảng DST):** lisp hàm mới `gro--gan-co-xdata (ens co)` thêm cờ vào chuỗi thứ 4 XData `GRO_METADATA` của bango (cùng chỗ cờ `MK_*`, mục 117/170): `KC_NGOAI_HINH` (chữ thò ra ngoài hình), `KC_CHAM_NET` (chữ chạm nét cắt). C# `BangSoLieu.DocCoMaKhac` đọc ra cảnh báo dòng: "Chữ khắc nằm một phần ngoài hình — kiểm tra" / "Chữ khắc chạm nét cắt 0-KAKOU — kiểm tra". Gắn cho mọi bango của ghi chú (cặp 本図/逆図 cả hai).
   - **Tốc độ:** trước đây mỗi hình lại gọi `gro--bbox` (COM) + đọc nội dung cho **mọi** chữ (mục 188 quét mọi layer → ~160 chữ × 14 hình). Nay hộp bao + phép "chữ kích thước" tính **một lần mỗi chữ**, chỉ giữ chữ chạm ít nhất một hình của tấm khắc chữ.
   - Test C# **560/560** (4 ca mới). Lisp: +1 hàm, ngoặc cân bằng, CRLF, ASCII không đổi. Bản trước: `backup/Guro ZWCAD(gro).lsp.truoc-muc190._ls`. Build 0 lỗi 0 cảnh báo, DLL **đã chép** `C:/GROZWCAD/GROZWCAD.dll` (29/10 15:50).

   > **Chưa chạy trong ZWCAD.** Nghiệm thu: tab Thông Tin hiện "!!! Khắc chữ: 6 chữ ... cần kiểm tra"; tab Danh Sách Tấm, cột Ghi chú các bango 1–6, 13–18 có "Chữ khắc nằm một phần ngoài hình — kiểm tra"; bảng thời gian `khac chu (156)` tụt từ 9 s.
