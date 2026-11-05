# 08. Sắp xếp, gom hàng và chế độ vùng

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Các lỗi đã phát hiện và sửa

22. **Hàm so sánh KHÔNG BẮC CẦU truyền cho `vl-sort` — lỗi thuật toán, không phải chuyện
   chỉnh dung sai.** Thuật toán gom hàng của `bg` (vẫn còn nguyên ở đó):

   ```lisp
   (if (equal Ya Yb (* height 3)) (< Xa Xb) (> Ya Yb))
   ```

   Quan hệ "cùng hàng theo dung sai" **không bắc cầu**: A≈B và B≈C nhưng A≉C. `vl-sort`
   nhận một hàm không phải quan hệ thứ tự toàn phần thì **kết quả phụ thuộc thứ tự đầu
   vào** — chạy hai lần trên cùng vùng chọn có thể ra hai thứ tự khác nhau. Chỉnh dung sai
   cho khéo **không chữa được**, vì lỗi nằm ở chỗ quan hệ không phải thứ tự.

   Thêm nữa `vl-sort` **loại bỏ phần tử** khi hàm so sánh coi hai phần tử bằng nhau — hai
   ghi chú cùng hàng cùng X thì mất một cái.

   **Cách chữa tổng quát** (đã làm trong `gro`, áp dụng được cho mọi chỗ khác trong codebase
   đang dùng kiểu banding này — xem danh sách ở mục "Cần kiểm tra lại"):

   1. **Gán chỉ số nhóm TRƯỚC**: sắp theo Y giảm dần, cắt hàng mới khi khoảng cách vượt
      ngưỡng → mỗi phần tử có một số hàng cụ thể.
   2. **Sắp theo khoá TOÀN PHẦN** `(hàng, X, stt)` với `stt` là số thứ tự đọc vào —
      **duy nhất tuyệt đối**. Nhờ khoá này không bao giờ có hai phần tử "bằng nhau", nên
      `vl-sort` **không thể loại bỏ phần tử nào nữa** — nguy cơ mất tấm biến mất hẳn, không
      cần cảnh báo suông.

   Ngưỡng cắt hàng của `gro--nguong-hang` **tự tìm từ dữ liệu** (khe tự nhiên giữa khoảng
   cách trong hàng và khoảng cách giữa hai hàng), không dùng hệ số cố định. Hệ số `height*3`
   cũ quá nhỏ: đo trên bản vẽ thật, ghi chú cùng một hàng lệch Y tới ~4 lần chiều cao chữ
   (vì tấm cao thấp khác nhau, ghi chú bám theo đỉnh tấm) → **một hàng bị xé làm nhiều**.

25. **Chế độ đánh số THEO VÙNG polyline.** Bản vẽ khách thường chia thành từng cụm đóng
   trong khung nét đứt. Đánh số chạy ngang qua các cụm thì người đọc rất khó dò; mà quét
   chọn từng vùng rồi chạy lệnh nhiều lần thì mất thời gian và dễ sai số thứ tự.

   Tích ô **"Đánh số theo vùng (polyline)"** trong hộp thoại rồi chọn layer vùng → lệnh
   **chỉ hỏi chọn polyline**, sau đó tự quét mọi TEXT/MTEXT nằm trong các vùng đó. Đánh
   **hết một vùng mới sang vùng khác**. Thứ tự các vùng dùng **chung `gro--sap-xep`**
   (mốc = góc trên-trái hộp bao) nên cũng là hàng trên→dưới, trái→phải — **không có thuật
   toán thứ hai phải bảo trì**.

   > **Bản đầu bắt quét text RỒI mới quét polyline — thừa và mất thời gian.** Người dùng
   > chỉ ra ngay: ở chế độ vùng thì chính polyline đã xác định phạm vi, quét text lần nữa
   > không thêm thông tin gì. Nay hai chế độ tách bạch, mỗi chế độ **quét chọn đúng một lần**
   > (xem bảng ở mục [Hộp thoại (DCL)](05-hop-thoai-dcl.md)).

   **Kiểm tra điểm trong vùng bằng RAY-CASTING thuần LISP, KHÔNG dùng `(ssget "_WP" …)`.**
   Hai lý do: mục 12 đã ghi các chuỗi mode của `ssget` chưa chắc ZWCAD hỗ trợ đầy đủ; và
   ray-casting chạy được với vùng hình **bất kỳ** chứ không riêng hình chữ nhật (đã thử
   hình L: điểm trong phần khuyết đúng là nằm ngoài).

   **Bẫy AutoLISP khi viết ray-casting:** công thức chuẩn là
   `(/= (> ay y) (> by y))`, nhưng **`/=` của AutoLISP là so sánh SỐ**, không dùng được với
   `T`/`nil`. Phải tách ra hai cờ rồi so bằng `and`/`or`. Việc `and` có short-circuit cũng
   chính là thứ chặn phép chia cho 0 khi cạnh nằm hoàn toàn trên/dưới tia.

   Các quyết định khác:
   - **Vùng lồng nhau → chọn vùng NHỎ NHẤT** (diện tích hộp bao bé nhất) vì đó mới là cụm
     thật sự của tấm. Có lọc nhanh bằng hộp bao trước khi chạy ray-casting.
   - Cơ chế "ghi chú ngoài mọi vùng vẫn được đánh số, xếp cuối, báo rõ kèm toạ độ" **vẫn
     còn trong code như lưới an toàn**, nhưng ở chế độ B nó **không bao giờ kích hoạt**: text
     đã được lọc theo vùng ngay từ đầu nên không thể có ghi chú ngoài vùng. Giữ lại để nếu
     `gro--vung-cua` cho kết quả khác nhau giữa bước lọc và bước sắp xếp thì lộ ra ngay.
   - Hỗ trợ cả `LWPOLYLINE` (nhiều group 10) lẫn `POLYLINE` nặng (duyệt `VERTEX` bằng
     `entnext`). Polyline dưới 3 đỉnh bị bỏ và **đếm vào báo cáo**.

   > **Hạn chế đã biết: điểm nằm ĐÚNG trên biên vùng là trường hợp không xác định** của
   > ray-casting (chuẩn chung của thuật toán, không phải lỗi cài đặt). Thực tế điểm chèn
   > ghi chú không bao giờ nằm đúng trên khung nét đứt nên chưa gây vấn đề.

28. **Chọn ngưỡng cắt hàng: phải đi từ ngưỡng NHỎ NHẤT lên, không lấy tỉ lệ lớn nhất.**
   Người dùng phát hiện trên bản vẽ thật: *"tại sao số 1 lại xếp dưới số 2?"* — một tấm nằm
   hẳn ở hàng dưới lại được đánh số **trước** tấm ở hàng trên.

   Đo lại đúng ô đó: các khoảng cách Y vượt sàn là **257, 72, 38**, tỉ lệ giữa chúng là
   **3.57** và **1.89**.

   | Cách chọn | Ngưỡng | Kết quả |
   |---|---|---|
   | tỉ lệ **lớn nhất** (3.57) — bản đầu | 136 | **gộp hết thành 1 hàng** ❌ |
   | khe **nhỏ nhất** đạt 1.8 (1.89) — bản sửa | 52 | chia đúng **3 hàng** ✓ |

   Khi đã bị gộp cùng hàng thì thứ tự do **X** quyết định — mà hai ghi chú đó lệch X chỉ
   khoảng **2 đơn vị**, nên tấm ở hàng dưới lọt lên trước. Triệu chứng nhìn thì như lỗi ngẫu
   nhiên, thực ra là tất yếu.

   **Sửa:** sắp các gap vượt sàn theo thứ tự giảm dần rồi **duyệt từ chỉ số lớn nhất xuống**
   (chỉ số càng lớn → ngưỡng càng nhỏ → càng nhiều hàng), lấy khe **đầu tiên** đạt tỉ lệ
   ≥ 1.8. Không thêm hằng số mới — vẫn đúng con số 1.8 đã dùng.

   > **Bất đối xứng ở đây là CÓ CHỦ Ý, cần nhớ khi sửa tiếp:** chia nhầm một hàng làm hai
   > thì thứ tự vẫn đọc được (đánh hết nhóm trái rồi sang nhóm phải — vẫn là thứ tự đọc hợp
   > lý), còn **gộp nhầm hai hàng làm một thì thứ tự loạn hoàn toàn**. Nên khi phân vân,
   > **luôn thiên về chia nhiều hàng**.

   Bài học chung: các thuật toán "tìm khe tự nhiên" kiểu này luôn có nhiều khe hợp lệ ở
   nhiều thang khác nhau (trong hàng / giữa hàng / giữa cụm). Chọn khe nào phải dựa trên
   **hậu quả của việc chọn sai**, không phải trên chỉ số thống kê thuần tuý.

49. **Chế độ VÙNG: hai hàng bị gộp làm một → tấm ở hàng dưới được đánh số trước.**
   Người dùng báo trên bản vẽ `JR西宮駅前 仕口梁No.10`: trong vùng 1, cụm **5-6 nằm TRÊN
   cụm 3-4**. Cùng họ với mục 28, nhưng **nguyên nhân khác hẳn** — lần này lỗi lộ ra vì chế
   độ vùng, chứ không phải vì cách chọn khe.

   ### Dựng lại được bằng số — đo bằng `grosort`, không ước lượng

   ```
   bango | vung |      X |       Y | cao | t= | TH | SL  | noi dung
   1     | 1    | 1780.9 | 10104.1 |  60 |  9 |  1 | 1 1 | t=9mm   <-- TACH 2 TAM
   3     | 1    | 1779.5 |  8815.5 |  60 |  6 |  1 | 1 1 | t=6mm   <-- TACH 2 TAM
   ```

   Vùng 1 có 5 ghi chú xếp thành 3 dải theo chiều dọc:

   | Dải | Gồm | Khe tới dải trên |
   |---|---|---|
   | 1 | `t=9㎜` tấm trên | — |
   | 2 | `t=6㎜` tấm lớn giữa + 2 ghi chú cột phải | **1288.6** = 21.5 h |
   | 3 | `t=9㎜` tấm dưới | **~458** = 7.6 h |

   **`cao` = 60**, tức `san` = 90 — cả hai khe đều vượt sàn nên đều là ứng viên.
   Khe nhỏ bằng **7.6 lần chiều cao chữ**, rõ ràng là khe giữa hai hàng. Nhưng:

   ```
   nguong = sqrt(1288.6 * 458) = 768   ->  2 hang: {dai 1} va {dai 2 + dai 3}
   ```

   Gộp xong thì thứ tự do **X** quyết định — mà hai ghi chú lệch nhau đúng **1.4 đơn vị**
   (1779.5 so với 1780.9), tức nhìn bằng mắt là thẳng hàng. Ghi chú tấm dưới nhỏ hơn một
   chút nên lọt lên trước, ra `1,2 → 3,4(dưới) → 5,6(giữa) → 7,8`.

   > Đây là chỗ đáng nhớ: **nguyên nhân nhìn thấy được (thứ tự sai) cách nguyên nhân thật
   > (ngưỡng gom hàng) hai lớp**, và lớp giữa — X lệch 1.4 trên tổng chiều rộng hàng nghìn —
   > thì không thể nhìn ra trên bản vẽ. Không có `grosort` thì không lần ra.

   ### Khuyết tật gốc: chỉ có ĐÚNG HAI khe thì kết quả đã định sẵn

   Với đúng hai khe `a > b`, nhánh `(= 1 (length ung))` không chạy, nên:

   | Tỉ lệ `a/b` | Ngưỡng | Số hàng |
   |---|---|---|
   | < 1.8 | `san` | **3** |
   | ≥ 1.8 | `sqrt(a*b)` — **luôn nằm giữa `b` và `a`** | **2** |

   Nghĩa là **mọi** bản vẽ có 3 dải mà tỉ lệ hai khe ≥ 1.8 đều bị gộp, **bất kể khe nhỏ lớn
   đến đâu** — 7.6 h hay 100 h cũng vậy. Đây mới là thứ cần chặn, không phải một con số
   cụ thể của bản vẽ này.

   ### Vì sao mục 28 không bắt được ca này

   Mục 28 sửa **cách chọn** trong số nhiều khe (đi từ ngưỡng nhỏ lên). Ở đây **chỉ có hai
   khe**, không còn gì để chọn. Và hai khe bất kỳ thì gần như luôn có tỉ lệ ≥ 1.8.

   > **Chế độ vùng làm lỗi này dễ xảy ra hơn hẳn**, vì ngưỡng được tính **riêng cho từng
   > vùng** (`gro--sap-theo-vung` gọi `gro--sap-xep` cho mỗi vùng). Mỗi vùng chỉ vài ghi chú
   > → rất ít khe → phép "so các khe với nhau" mất hết cơ sở thống kê. Cùng bản vẽ đó chạy
   > chế độ thường thì tất cả ghi chú chung một mẫu, nhiều khe hơn, nên không lộ.

   ### Sửa: chặn TRẦN cho ngưỡng

   `gro--nguong-toi-da` = `hmax * 5.0`. Ngưỡng tính ra vượt trần thì lấy trần.

   **5.0 lấy từ số đo của mục 28**: độ lệch Y *trong cùng một hàng* đo được tới ~4 lần chiều
   cao chữ (ghi chú bám theo đỉnh tấm, tấm cao thấp khác nhau). Lấy 5 để chắc chắn nằm trên
   mức đó, mà vẫn bắt được khe 7h của bản vẽ thật.

   Trần này **chỉ có thể làm TĂNG số hàng, không bao giờ giảm** — đúng hướng thiên vị đã
   chốt ở mục 28: gộp nhầm hai hàng thì thứ tự loạn hoàn toàn, còn chia nhầm một hàng thành
   hai thì vẫn đọc được. Nhãn `cach-chon` được nối thêm `/chan` để báo cáo và `grosort` cho
   thấy ngay trần có đụng vào hay không.

   Kiểm offline (`sim.py`, 11 ca gom hàng, tất cả đạt), gồm **cả hồi quy lẫn ca mới**:

   | Ca | Ngưỡng | Hàng |
   |---|---|---|
   | mục 28: khe 257/72/38, hmax 14 | 52.31 (`khe`) | 3 ✓ **không đổi** |
   | **mục 49: số đo thật 1288.6/458, cao 60** | **300.00 (`khe/chan`)** | **3 ✓ (trước là 768 → 2)** |
   | 2 khe, khe nhỏ = 10 h | 300.00 (`khe/chan`) | 3 ✓ |
   | trần không phá nhánh `mot-hang` | 27.00 (`mot-hang`) | 1 ✓ |
   | trần không đụng khi ngưỡng đã nhỏ | 13.50 (`san`) | 3 ✓ |

   ### Kèm theo: việc gom hàng TRONG vùng trước đây không nhìn thấy được

   Ở chế độ vùng, chỉ số "hàng" **bị ghi đè bằng chỉ số VÙNG** để vòng tạo đối tượng dùng
   chung một dạng dữ liệu. Hệ quả: cả báo cáo lẫn `grosort` đều **không hiện ngưỡng, không
   hiện số hàng** — đúng thứ đã giấu mất lỗi này. Người dùng nhìn thấy số sai mà không có
   con số nào để đối chiếu.

   Nay `gro--sap-theo-vung` trả thêm phần tử thứ ba: `(vùng, số ghi chú, số hàng, ngưỡng,
   cách chọn)` cho từng vùng, và `gro--in-hang-vung` in ra ở **cả `gro` lẫn `grosort`**:

   ```
   Theo VUNG: 13 vung polyline  (hmax=60.00)
   Gom hang trong vung (chi liet ke vung >1 hang hoac bi chan tran):
      vung 1: 5 ghi chu -> 3 hang  (nguong=300.00, khe/chan)
   ```

   Chỉ liệt kê vùng có từ 2 hàng trở lên hoặc bị chặn trần — hai thứ cần soi khi thứ tự
   trong một vùng trông có vẻ sai. Bài học lặp lại của mục 29 và 36: **đại lượng nào quyết
   định kết quả thì phải in ra được**, nếu không thì lần sau lại phải đoán.

   ### ĐÃ NGHIỆM THU TRÊN BẢN VẼ THẬT — cả 13 vùng

   Dựng lại toàn bộ bảng `grosort` (63 ghi chú → 81 tấm) bằng đúng thuật toán, bản mới
   **khớp 100%** với số bango thật. Bảng so sánh cũ/mới từng vùng:

   | Vùng | Hàng cũ | Hàng mới | Ngưỡng cũ | Ngưỡng mới | Thứ tự |
   |---|---|---|---|---|---|
   | **1** | 2 | **3** | 814.3 | **300.0** | **ĐỔI** |
   | **2** | 2 | **3** | 650.5 | **300.0** | **ĐỔI** |
   | **3** | 2 | **3** | 655.2 | **300.0** | **ĐỔI** |
   | 4 | 2 | 2 | 189.3 | 189.3 | — |
   | 5, 6, 7 | 5 | 5 | 90.0 | 90.0 | — |
   | 8 | 2 | 2 | 185.0 | 185.0 | — |
   | 9 | 2 | 2 | 199.5 | 199.5 | — |
   | **10** | 2 | **3** | 607.3 | **300.0** | **ĐỔI** |
   | 11, 13 | 1 | 1 | 180.0 | 180.0 | — |
   | 12 | 2 | 2 | 237.3 | 237.3 | — |

   Hai điều đáng ghi:

   1. **Lỗi rộng hơn người dùng thấy: 4 vùng sai, không phải 1.** Vùng 2, 3, 10 cũng bị gộp
      hàng y hệt, chỉ là không ai để ý. Đây là lý do phải dựng lại **toàn bộ** bảng chứ không
      chỉ kiểm cái ô được báo.
   2. **Trần chỉ đụng vào đúng 4 vùng đó, không vùng nào khác đổi.** 9 vùng còn lại có ngưỡng
      sẵn dưới 300 nên trần không với tới — không có chuyện chia quá tay. Riêng vùng 12
      (ngưỡng 237.3) là ca sát trần nhất mà vẫn đúng.


51. **Chế độ vùng đổi sang QUÉT CHỌN THEO TỪNG KHUNG** (người dùng yêu cầu và chốt chi tiết).

   Trước đây: quét `ssget "_X"` cả bản vẽ, rồi kiểm **điểm chèn** của text có nằm trong đa
   giác không (ray-casting). Nay: mỗi khung là một **cửa sổ chọn** riêng.

   ### Luật nhận khung — chốt với người dùng

   | | Trước | Nay |
   |---|---|---|
   | Polyline nhận được | bất kỳ, ≥ 3 đỉnh | **kín, ĐÚNG 4 đỉnh**, nằm phẳng theo Z |
   | Vùng là gì | danh sách đỉnh + ray-casting | **hộp bao** của 4 đỉnh |
   | Lấy text | `ssget "_X"` cả bản vẽ rồi lọc | `ssget "_C"` **từng khung một**, hợp lại |
   | Text được tính | điểm chèn nằm trong đa giác | **nằm trong HOẶC giao** với khung |

   Hình chữ nhật vẽ kiểu **5 đỉnh** (đỉnh cuối đè đỉnh đầu, thay vì đặt cờ closed) vẫn được
   nhận — `gro--pl-gon` bỏ đỉnh trùng trước khi đếm. Cùng một hình, chỉ khác cách vẽ; loại
   nó đi thì người dùng mất khung mà không hiểu vì sao.

   Khung **xoay** hoặc không phải hình chữ nhật vẫn nhận được nếu đủ 4 đỉnh, nhưng hộp bao
   của nó **rộng hơn chính nó** — cần biết khi đọc kết quả.

   ### Cái được: hết bỏ sót ghi chú nhô ra ngoài khung

   Đây đang là **rủi ro số 1** của chế độ vùng ghi trong [Cần kiểm tra lại](14-can-kiem-tra-lai.md):
   ghi chú có điểm chèn nằm ngoài khung thì biến mất khỏi CSV, không dấu vết. Cửa sổ CẮT bắt
   cả thứ chỉ chạm viền nên lỗ đó bịt lại. Báo cáo in rõ con số:

   ```
   Theo VUNG: 13 khung polyline  (hmax=60.00)  [2 ghi chu vao duoc nho cua so CAT - ban cu bo sot]
   ```

   ### Cái mất: PHỤ THUỘC TẦM NHÌN — phải zoom thấy hết trước khi chạy

   `ssget` cửa sổ của ZWCAD làm việc theo những gì đang hiện trên màn hình (mục 41 đã đo được
   chiều ngược lại: nó bắt cả đối tượng ngoài cửa sổ). Zoom sát vào một góc rồi chạy thì các
   khung ngoài màn hình **có thể không bắt được gì** — và hỏng trong im lặng.

   Bản cũ dùng `ssget "_X"` nên **không** có ràng buộc này. Đây là cái giá đã nói rõ với người
   dùng trước khi làm, và họ chọn đổi. Lệnh in sẵn lời nhắc khi không tìm thấy text nào.

   ### Gán ghi chú vào khung: ưu tiên CHỨA, rồi mới GIAO

   Cửa sổ cắt bắt cả thứ chỉ chạm viền, nên một ghi chú nằm hẳn trong khung 5 có thể đồng
   thời chạm khung 2. Nếu lấy "khung đầu tiên bắt được" thì nó bị khung bên cạnh giành mất.

   `gro--vung-cua` xét **hai vòng**: khung nhỏ nhất **chứa điểm chèn** trước; không có thì
   khung nhỏ nhất **giao hộp bao**. Luôn lấy khung **nhỏ nhất**, không lấy "khung đầu tiên
   tìm thấy" — thứ tự khung do `gro--sap-vung` quyết định và có thể đổi, mà kết quả thì
   **không được phụ thuộc thứ tự đầu vào** (cùng bài học mục 22).

   ### Ray-casting đã bị BỎ

   `gro--trong-vung` không còn tồn tại — với khung chữ nhật thì kiểm điểm trong hộp bao là
   đủ, giữ lại sẽ thành code chết. Mục 25 từng chọn ray-casting vì hai lý do, **cả hai nay
   đều không còn**: (a) lo `ssget "_WP"`/`"_CP"` không chạy trên ZWCAD — mục 12 đã đính chính
   là các mode cửa sổ chạy tốt; (b) để xử lý hình bất kỳ — nay người dùng chốt chỉ dùng hình
   4 đỉnh.

   > **Muốn khung hình L hay đa giác bất kỳ trở lại thì phải viết lại ray-casting.** Thư mục
   > này không có version control nên code đó mất hẳn. Phần mô phỏng của nó trong
   > `sim_vung.py` cũng đã gỡ (test xanh cho code không còn tồn tại là test đánh lừa).

   ### Báo lý do khi khung bị loại

   Quét chọn 13 khung mà lệnh chỉ nhận 11 thì phải biết 2 cái kia sai ở đâu:

   ```
   (bo qua: 1 polyline KHONG KIN, 1 khong phai 4 dinh, 0 khong nam phang theo Z)
   ```

   Kiểm offline `sim_khung.py` — 15 ca, tất cả đạt: luật nhận khung (4 đỉnh / 5 đỉnh trùng /
   3 đỉnh / 5 đỉnh khác nhau / không kín / không phẳng), ưu tiên chứa trước giao, vùng lồng
   nhau lấy vùng nhỏ nhất, **không phụ thuộc thứ tự đầu vào**, và cửa sổ cắt bắt cả text
   giao mà không trùng lặp.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu theo thứ tự: (1) chạy `grosort` chọn polyline,
   > xem có nhận đủ 13 khung không và dòng `(bo qua: …)`; (2) so **tổng số ghi chú** với lần
   > chạy trước — phải **bằng hoặc nhiều hơn** 63, ít hơn là cửa sổ đang bỏ sót vì zoom;
   > (3) xem con số `[N ghi chu vao duoc nho cua so CAT]`.

53. **TÁCH BẠCH "khung bao" và "vùng" — chúng là hai việc khác nhau** (người dùng chỉ ra
   rằng mục 51-52 đã gộp nhầm hai thứ làm một).

   | | KHUNG BAO | VÙNG |
   |---|---|---|
   | Dùng để làm gì | **quét cái gì** — thay cho quét chọn text thủ công | **thứ tự bên trong** |
   | Có ảnh hưởng phạm vi không | **có** | **không** |
   | Lấy ở đâu | người dùng **quét chọn** trên layer khung bao | lisp **tự tìm** trên layer vùng |
   | Nằm ở đâu | ngoài cùng | **bên trong khung bao** (người dùng chốt) |

   Hộp thoại nay có **hai ô tích + hai popup layer** độc lập:

   ```
   Mã khắc:                              [I ▼]
   [ ] Quét chọn bằng khung bao (polyline)
       Layer khung bao:                  [... ▼]
   [ ] Đánh số theo vùng (polyline)
       Layer vùng:                       [... ▼]
   ```

   ### Thứ tự đánh số — hai cấp

   Người dùng chốt: chọn nhiều khung bao thì **đánh hết khung này rồi sang khung kia**. Nên
   thứ tự đầy đủ là **khung bao → vùng trong khung → hàng → X**.

   Bốn tổ hợp:

   | Khung bao | Vùng | Phạm vi | Thứ tự |
   |---|---|---|---|
   | tắt | tắt | quét chọn text thủ công | hàng |
   | **bật** | tắt | text trong/giao các khung | khung này hết rồi sang khung kia |
   | tắt | **bật** | quét chọn text thủ công | vùng này hết rồi sang vùng kia |
   | **bật** | **bật** | text trong/giao các khung | khung → vùng trong khung → hàng |

   ### Cài đặt: GỘP hai cấp thành MỘT danh sách phẳng

   `gro--nhom-vung` trả về danh sách vùng phẳng **đã đúng thứ tự**: với từng khung bao, các
   vùng nằm trong nó, rồi mới đến chính khung bao. Nhờ vậy `gro--sap-theo-vung` **không phải
   sửa một dòng nào** — nó vẫn chỉ biết "một danh sách vùng, chia ghi chú vào đó rồi sắp".

   Hai điểm làm cho cách gộp này chạy đúng:

   1. **Khung bao đặt SAU các vùng của nó.** Ghi chú nằm trong khung mà không thuộc vùng nào
      sẽ được đánh số **sau hết** các vùng, không lọt vào giữa.
   2. **Việc gán ghi chú vào nhóm vẫn do `gro--vung-cua` lo** — nó lấy vùng **nhỏ nhất**, mà
      vùng bao giờ cũng nhỏ hơn khung bao chứa nó. Không cần luật mới nào.

   ### `gro--trong-khung-p` dùng "nằm GỌN", không dùng "giao"

   Lọc vùng theo khung bao phải là **nằm gọn bên trong**, và phải loại **chính khung bao**
   (hộp bao trùng khít). Lấy "giao" thì viền khung bao chạm mọi thứ, và khung bao sẽ tự nhận
   chính nó làm một vùng.

   > **Vì sao phải lọc vùng theo khung bao:** tấm thép thường cũng là hình chữ nhật kín 4
   > đỉnh. Không giới hạn phạm vi thì rất dễ nhặt nhầm viền tấm làm vùng. Lọc theo layer là
   > lớp phòng thứ nhất, lọc theo khung bao là lớp thứ hai.

   Kiểm offline (`sim_khung.py`, 6 ca thêm, tổng 25 ca, tất cả đạt): khung bao đứng sau các
   vùng của nó; vùng ngoài mọi khung bị loại; không có khung bao thì giữ nguyên danh sách
   vùng; có khung bao mà không có vùng thì chỉ còn các khung; khung bao **không tự nhận
   chính nó** làm vùng; vùng trùng khít một khung **khác** thì vẫn được nhận.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: tích cả hai ô, chọn khung bao ngoài cùng,
   > rồi xem dòng `Nhom: 1 khung bao + 13 vung` và `so ghi chu moi nhom`.

   ### Hai lỗi của tôi khi làm mục này — ghi lại để không lặp

   1. **Cắt khối theo chỉ số và nhân đôi 3843 dòng.** Mốc kết thúc tìm bằng `s.index(...)`
      từ đầu file nên trúng một chuỗi giống hệt **nằm trước** mốc bắt đầu → `s[:start] + new
      + s[end:]` dán lại cả đoạn ở giữa. `parens.py` vẫn báo cân bằng (đoạn nhân đôi tự nó
      cân), chỉ có **số form toplevel nhảy từ 157 lên 283** và `grep -c '^(defun c:'` từ 11
      lên 21 mới lộ ra.
   2. Trước đó cùng một kiểu cắt đã **xoá mất `gro--pl-dinh`** (mục 51), và trước nữa là
      `c:grodbg`.

   > **Luật: không cắt khối bằng chỉ số chuỗi nữa.** Dùng mốc DÒNG, và sau mỗi lần sửa cấu
   > trúc phải kiểm **ba** con số chứ không chỉ ngoặc: số form toplevel, số `(defun c:`, và
   > danh sách `defun` trùng lặp (`sort | uniq -d`).

58. **Bỏ `ssget "_C"` ở khâu quét nội dung — nó chỉ thấy phần đang hiện trên màn hình.**
   Người dùng gặp thật: zoom vào một góc rồi chạy thì phần ngoài khung nhìn không được đọc.
   Đây đúng là cái giá đã cảnh báo ở mục 51, nay trả lại.

   | | Mục 51 (hỏng) | Mục 58 (nay) |
   |---|---|---|
   | Lấy text bằng | `ssget "_C"` từng khung, hợp lại | **`ssget "_X"`** một lần, đọc thẳng CSDL bản vẽ |
   | Phụ thuộc tầm nhìn | **có** — hỏng trong im lặng | **không** |
   | Lọc theo khung | do chính cửa sổ `ssget` | `gro--vung-cua pt ebb khungs` (đã có sẵn) |
   | Nghĩa "trong hoặc giao" | theo độ phân giải màn hình | **hình học chính xác** (hộp bao) |

   Phép lọc không phải viết mới: vòng phân loại **vốn đã** gọi
   `(gro--vung-cua pt ebb khungs)` — "nằm trong HOẶC giao với khung". Bỏ cửa sổ `ssget` đi là
   phép lọc đó trở thành nơi duy nhất quyết định, và nó chính xác hơn hẳn cửa sổ chọn.

   `gro--quet-vung` đổi tên thành **`gro--quet-text`** và không còn nhận tham số — nó chỉ còn
   một việc: đọc mọi TEXT/MTEXT của bản vẽ.

   ### Kéo theo: phải lọc KHÔNG GIAN GIẤY

   `ssget "_X"` quét **cả các layout**. Entity ở không gian giấy có `group 67 = 1`, và toạ độ
   của chúng hoàn toàn có thể trùng vùng toạ độ của khung bao ở không gian mô hình. Bản trước
   mục 51 cũng dùng `"_X"` và chỉ *giả định* là không trùng — nay có `gro--giay-p` lọc thật,
   dùng ở **cả `gro` lẫn `grosort`**.

   ### Cái giá: đọc nhiều text hơn

   Phải đọc hết text của bản vẽ rồi mới lọc, thay vì chỉ đọc phần trong khung. Chấp nhận:
   **quét thiếu mà không biết** là loại lỗi tệ nhất, còn chậm hơn một chút thì nhìn thấy
   được. Báo cáo đổi chữ cho đúng: `(doc N text ca ban ve)`.

   ### ⚠ Chỗ VẪN còn phụ thuộc tầm nhìn — và vì sao giữ nguyên

   `gro--dem-va-cham` / `gro--vat-can` (dò vật cản khi **đặt** bango) **vẫn dùng `ssget "_C"`**.
   Không đổi được: nó phải tìm vật cản **mọi loại** quanh ~20 ô ứng viên × ~100 tấm. Thay
   bằng `"_X"` + so hộp bao trong LISP thì phải tính hộp bao cho toàn bộ entity của bản vẽ
   rồi so hàng chục triệu lần — không chạy nổi. `ssget "_C"` ở đó là một **truy vấn không
   gian**, đúng việc của nó.

   > Nên lời khuyên "zoom thấy hết vùng cần đánh số" **vẫn còn**, nhưng đổi hẳn mức nghiêm
   > trọng: trước là **sai dữ liệu** (thiếu tấm, thiếu mã khắc), nay chỉ còn là **vị trí đặt
   > bango có thể đè hình**. Nhìn thấy được và sửa tay được.

   Kiểm offline (`sim_khung.py`): lấy đúng text nằm trong, text giao viền, và text có **điểm
   chèn ngoài khung nhưng thân chạm khung**; text ngoài hẳn thì không vào; và kết quả **không
   đổi khi đảo thứ tự khung**.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: zoom sát vào một góc rồi chạy `grosort` —
   > tổng số ghi chú phải vẫn là 63, bằng với khi zoom thấy hết. Đó là phép thử trực tiếp cho
   > đúng lỗi này.

61. **VÙNG được phép có nhiều hơn 4 đỉnh — và ray-casting quay lại** (người dùng yêu cầu).

   | | KHUNG BAO | VÙNG |
   |---|---|---|
   | Số đỉnh | **đúng 4** (không đổi) | **từ 3 trở lên** |
   | Phép "chứa điểm" | hộp bao = ray-casting (hình 4 đỉnh thì trùng nhau) | **ray-casting** |

   Khung bao giữ luật 4 đỉnh vì nó quyết định **phạm vi** và thứ tự ngoài cùng — để hình chữ
   nhật cho rõ ràng. Người dùng chỉ yêu cầu nới ở phần đánh số theo vùng. *Muốn nới cả khung
   bao thì đổi tham số `chi-4` ở chỗ gọi — chỉ một chữ.*

   ### Hộp bao không còn đủ, nên phải khôi phục ray-casting

   Vùng hình **L** có hộp bao trùm cả phần khuyết:

   ```
   hop bao cua hinh L (0,0)-(100,0)-(100,40)-(40,40)-(40,100)  ->  (0 0 100 100)
   diem (80,80): hop bao noi "trong", nhung thuc te nam trong phan KHUYET
   ```

   Nên `gro--vung-chua` nay: lọc nhanh bằng hộp bao trước (rẻ), rồi mới chạy **ray-casting**.
   Với hình 4 đỉnh hai phép cho cùng kết quả nên khung bao không đổi hành vi gì.

   `gro--trong-vung` **được khôi phục nguyên văn** bản đã bỏ ở mục 51 — gồm cả ghi chú về bẫy
   `/=` của AutoLISP (mục 25): công thức chuẩn `(/= (> ay y) (> by y))` **không dùng được** vì
   `/=` là phép so **số**, phải tách thành hai cờ rồi so bằng `and`/`or`; và chính
   short-circuit của `and` là thứ chặn phép chia cho 0 khi cạnh nằm hoàn toàn trên/dưới tia.

   > Mục 51 đã ghi "bỏ ray-casting thì mất hẳn, muốn dùng lại phải viết lại". May là nguyên
   > văn hàm còn trong lịch sử phiên làm việc nên chép lại được y nguyên. **Bài học: trong
   > thư mục không có version control, xoá code đang chạy là quyết định một chiều** — lần sau
   > nên để lại trong file rule trước khi xoá.

   ### Chỗ VẪN dùng hộp bao, và vì sao chấp nhận được

   `gro--vung-giao` (đường dự phòng: ghi chú có điểm chèn **ngoài mọi vùng**, lấy vùng nhỏ
   nhất *giao* hộp bao) vẫn chỉ so hộp bao. Với vùng hình L đây là xấp xỉ rộng hơn thực tế.
   Chấp nhận: nó chỉ chạy cho ghi chú đã nằm ngoài mọi vùng, và luật "vùng nhỏ nhất" hạn chế
   bớt. Làm giao đa giác–chữ nhật chính xác thì tốn nhiều code cho một đường hiếm.

   Kiểm offline (`sim_khung.py`, 9 ca thêm, tổng 34 ca): khung bao vẫn loại polyline 5 đỉnh;
   vùng nhận 5 đỉnh và 3 đỉnh; vùng vẫn loại 2 đỉnh và polyline không kín; và ba ca ray-casting
   trên hình L — điểm trong phần đặc thì thuộc vùng, **điểm trong phần khuyết thì không**
   (dù hộp bao vẫn chứa), điểm ngoài hẳn thì không.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: chạy `grosort` ở chế độ vùng, so số vùng
   > nhận được với số khung cụm nhìn thấy; nếu vẫn thiếu thì đọc dòng `(bo qua: …)` xem chúng
   > rớt vì **không kín** hay vì **dưới 3 đỉnh**.

62. **"Sao không đổi cách quét sang `_WP`?" — chưa đổi, và đây là lý do.**

   `ssget "_WP"` / `"_CP"` chọn theo **đa giác**, nghe rất hợp với vùng hình L của mục 61.
   Hai lợi thế thật:

   - **Chính xác hơn**: cắt đúng theo đa giác, không phải xấp xỉ bằng hộp bao + ray-casting
     trên điểm chèn. `gro--vung-giao` (đường dự phòng) sẽ hết chỗ xấp xỉ rộng.
   - **Nhanh hơn**: không phải gọi `gro--bbox` (COM) cho **mọi** text của bản vẽ — đây đang
     là chi phí lớn nhất của cách hiện tại.

   ### Vì sao vẫn chưa đổi

   `_WP` và `_CP` **cùng họ với `_C`** — đều là phép chọn **cửa sổ**. Mà `_C` chính là thứ
   vừa phải bỏ ở mục 58 vì nó **chỉ thấy phần đang hiện trên màn hình**, và người dùng đã gặp
   thật: zoom sát vào một góc thì nội dung ngoài khung nhìn không được đọc.

   Đổi sang `_WP` **rất có thể mang lại đúng con bug vừa sửa** — và đó là loại hỏng trong im
   lặng, tệ nhất.

   > **Nhưng đây mới là SUY LUẬN, chưa phải số đo.** Mục 41 đo `_C`, mục 58 đo triệu chứng
   > của `_C`. **Chưa ai đo `_WP`/`_CP` trên ZWCAD.** Mà `xuat_csv_v5.0.1 ZWCAD.lsp` thì đang
   > dùng `"_CP"`/`"_WP"` thật (dòng 697, 708), nên không loại trừ chúng khác `_C`.
   >
   > Đổi theo suy luận là sai phương pháp — đúng bài học của cả file này: **đo, đừng đoán.**

   ### Lệnh `growp` — phép đo để chốt

   `growp` chạy **song song hai cách** trên cùng một bộ polyline và so từng vùng:

   | | Cách |
   |---|---|
   | A | `ssget "_X"` cả bản vẽ rồi lọc bằng `gro--vung-cua` — **đang dùng** |
   | B | `ssget "_CP"` với chính các đỉnh polyline — **đề xuất** |

   In ra số lượng mỗi bên, kết luận `KHOP` / `B THIEU` / `B thua`, và liệt kê tối đa 5 đối
   tượng chênh lệch mỗi bên **kèm nội dung** để biết cái gì bị bỏ. Cũng in khung nhìn hiện
   tại để đối chiếu với mức zoom.

   **Cách dùng — chạy HAI LẦN trên cùng bản vẽ:**

   1. **Zoom thấy hết** → chạy `growp`.
   2. **Zoom sát vào một góc** → chạy `growp` lần nữa.

   | Kết quả | Kết luận |
   |---|---|
   | B khớp A ở **cả hai** lần | đổi sang `_CP` được — chính xác hơn **và** nhanh hơn |
   | B thiếu khi zoom sát | `_CP` phụ thuộc tầm nhìn → **giữ nguyên cách hiện tại** |
   | B thừa | `_CP` bắt cả thứ chỉ chạm viền — cân nhắc `_WP` thay vì `_CP` |

   `growp` **không sửa gì** trong bản vẽ. Chỉ có ở bản đầy đủ.

   > Gửi lại kết quả hai lần chạy thì chốt được ngay, không phải đoán thêm.

63. **Đổi khâu quét sang `ssget "_CP"` — được, vì ĐÃ CÓ bước tự zoom** (người dùng chốt).

   Mục 62 để ngỏ câu hỏi "sao không dùng `_WP`". Lý do người dùng đưa ra là đúng: từ mục 59,
   `gro` **đã tự zoom** cho thấy hết vùng làm việc, nên điểm yếu "chỉ thấy phần trên màn
   hình" của phép chọn cửa sổ đã được bịt.

   | | Mục 58 | Mục 63 (nay) |
   |---|---|---|
   | Quét bằng | `ssget "_X"` cả bản vẽ + lọc hộp bao | **`ssget "_CP"`** từng vùng |
   | Cắt theo | hộp bao + ray-casting điểm chèn | **đúng đa giác** |
   | Chi phí | `gro--bbox` (COM) cho **mọi** text của bản vẽ | chỉ cho text trong vùng |
   | An toàn tầm nhìn | tự thân | **nhờ bước zoom chạy trước** |

   ### THỨ TỰ LÀ BẮT BUỘC — và đây là chỗ dễ hỏng nhất

   Bước zoom của mục 59 nằm ở **5b**, tức **sau** khâu quét. Giữ nguyên thứ tự đó mà đổi sang
   `_CP` là quay lại đúng con bug của mục 58 — và nó **hỏng trong im lặng**.

   Nên bước zoom đã được **dời lên bước 3**, ngay sau khi đọc xong khung bao và trước khi
   quét. Một lần zoom phục vụ cả hai: quét nội dung **và** dò vật cản lúc đặt bango.

   > Ghi rõ ràng trong chú thích của `gro--quet-cp`: *"zoom phải chạy TRƯỚC quét. Đổi chỗ hai
   > bước này là quay lại đúng con bug của mục 58."*

   Đường **quét chọn thủ công** (không có khung bao) vẫn zoom ở bước 5b như cũ — nó không
   dùng `_CP`, chỉ cần zoom cho khâu đặt bango. Cờ `da-zoom` để không zoom hai lần.

   ### Kéo theo

   - `grosort` đổi y hệt (zoom trước, rồi `_CP`) — lệnh chẩn đoán mà quét khác lệnh thật thì
     nó nói dối (bài học mục 29).
   - `ssget "_CP"` được bọc `vl-catch-all-apply`: đa giác tự cắt hoặc suy biến có thể làm nó
     ném lỗi, mà một vùng hỏng thì không được kéo chết cả lệnh.
   - `gro--quet-text` (cách cũ) **giữ lại** — chỉ còn `growp` dùng, làm lưới an toàn để đối
     chiếu khi nghi `_CP` bỏ sót. Vì thế nó không còn trong bản gọn.
   - `growp` đổi nhãn: A = cách cũ, B = cách đang dùng. Và **cố ý không tự zoom**, để đo được
     đúng ảnh hưởng của tầm nhìn.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: zoom sát một góc rồi chạy `gro` — nó phải tự
   > zoom ra trước khi quét, và tổng số ghi chú vẫn là 63. Nếu nghi ngờ, chạy `growp` (lệnh
   > này không tự zoom) để so hai cách.

65. **Vùng THÒ RA NGOÀI khung bao bị loại sạch** (người dùng báo).

   Mục 53 đòi vùng phải nằm **trọn** trong khung bao (`gro--trong-khung-p`). Trên bản vẽ thật
   nhiều khung cụm thò ra ngoài khung bao một chút — và chúng **bị loại hết**, mất hẳn việc
   nhóm thứ tự ở những cụm đó. Loại trong im lặng: báo cáo chỉ in số vùng *tìm được*, không
   in số bị bỏ.

   ### Luật mới: `gro--khung-cua-vung`

   | Thứ tự xét | |
   |---|---|
   | 1 | có khung **chứa trọn** vùng → lấy khung **nhỏ nhất** trong số đó |
   | 2 | không có → lấy khung **chồng lấn nhiều nhất** |
   | 3 | không chồng lấn với khung nào → **nil**, vùng không thuộc phạm vi |

   Vẫn bỏ qua hộp bao **trùng khít** khung bao — đó chính là khung bao, không phải vùng.

   Trả về **chỉ số** một khung duy nhất, nên mỗi vùng chỉ thuộc **đúng một** nhóm. Trước đây
   `gro--nhom-vung` gọi lại phép kiểm cho từng khung, nên nếu nới sang "chồng lấn" mà giữ
   cách cũ thì một vùng chạm hai khung sẽ **lọt vào cả hai nhóm**.

   Ưu tiên "chứa trọn" trước "chồng lấn nhiều nhất" là có lý do: vùng nằm gọn trong khung A
   nhưng khung B rất to cũng trùm qua thì vẫn phải thuộc A.

   Báo cáo thêm dòng đếm số vùng bị bỏ:

   ```
   Tim thay 13 vung tren layer VUNG.
      (2 vung tren layer do bi bo vi khong cham khung bao nao)
   ```

   Kiểm offline (`sim_khung.py`, 5 ca thêm): vùng thò ra ngoài **vẫn thuộc khung**; vùng
   không chạm khung nào → nil; chạm hai khung → lấy cái chồng lấn nhiều hơn; nằm trọn trong
   khung nhỏ thì **không** bị khung to giành; và mỗi vùng chỉ vào **đúng một** nhóm.

   ### Dọn kèm: 10 hàm rò biến vòng `foreach` ra toàn cục

   Rà bằng bộ dò đối chiếu danh sách local với biến thật sự được gán (bỏ chú thích và chuỗi).
   Hai hàm vừa viết (`gro--khung-cua-vung`, `gro--nhom-vung`) thiếu `k`, và tám hàm cũ trong
   namespace `gro--` cũng thiếu: `gro--codes-int` `c`, `gro--tim-sl` `c`, `gro--gan-hang`
   `it`, `gro--ghep-sl` `c n`, `gro--sl-cua` `p`, `gro--so-luong` `d`, `gro--dau-canh` `q`,
   `gro--write-dcl` `x`. Đã khai báo hết.

   > Chưa hỏng gì vì chưa trùng tên với biến nào đang dùng — nhưng đó là **may**, không phải
   > thiết kế. Bộ dò vẫn còn nhiễu với các hàm `bg--`/`fn--` (bắt nhầm chữ trong chuỗi của
   > `setq` nhiều biến), nên phần đó chưa đụng tới.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: so số vùng tìm được với số khung cụm nhìn
   > thấy; nếu còn thiếu thì đọc dòng `(N vung ... bi bo vi khong cham khung bao nao)`.

182. **Thứ tự KHUNG BAO / VÙNG không theo trái → phải — vùng bên trái bị đánh số sau** (người dùng báo kèm ảnh, 29/10/2026).

   ### Triệu chứng
   Bản vẽ có 12 khung nét đứt. Bango chạy 2 → 3 → 6 → 9 → 8 → 7 → 5 → 4 → 10 → 11 → 12, khung **1** (góc trên-trái, cao nhất) bị đánh **sau cùng** (73–82), mất thẩm mỹ.

   ### Nguyên nhân (tìm được)
   `gro--sap-vung` (sắp khung bao khi chọn, và sắp vùng trong `gro--tim-vung`) gọi `gro--sap-xep` với **`hmax = 1.0`** — chiều cao chữ GIẢ cho khung. Sàn / trần ngưỡng hàng của `gro--nguong-hang` (1.5·hmax / 5·hmax, mục 49) chỉ vài mm → hai khung lệch đỉnh vài mm đã thành hai hàng → thứ tự gần như **chỉ theo đỉnh khung từ trên xuống**, không có "trái → phải". Thứ tự trong ảnh (2, 3, 6, 9, 8, 7, 5, 4, 10, 11, 12) khớp đúng kiểu sắp này.

   ### Sửa
   `gro--sap-vung` viết lại (không dùng `gro--sap-xep` nữa): dung sai hàng = **½ chiều cao khung NHỎ NHẤT**; duyệt khung theo đỉnh giảm dần, khung vào **hàng mới** khi đỉnh thấp hơn đỉnh khung **đầu hàng** quá dung sai; trong hàng sắp **x trái tăng dần**. Khoá sắp toàn phần `(hàng, x trái, stt)` — không mất phần tử (mục 22). Tham số `hmax` giữ cho tương thích, bỏ không dùng. Hai chỗ gọi (`c:gro` khung bao, `gro--tim-vung`) không phải sửa.

   Mô phỏng trên vị trí 12 khung đo từ ảnh: **1 → 2 → 3 → 6 → 9 → 8 → 4 → 5 → 7 → 10 → 11 → 12** (hàng trên: 1, 2, 3 cùng đỉnh; 4 / 5 / 7 cùng hàng → trái sang phải).

   > **Còn một khả năng chưa loại trừ:** nếu 12 khung là **VÙNG** (tích "Đánh số theo vùng") bên trong một khung bao lớn, mà khung 1 **không được nhận là vùng** (không kín, < 3 đỉnh, không chạm khung bao…), thì ghi chú trong khung 1 vẫn rơi vào nhóm "trong khung bao, ngoài mọi vùng" — đánh số **sau** các vùng (mục 53), dù sắp đúng. Dòng lệnh khi đó có `(bo qua: ...)` / `N vung ... bi bo` — xem log để biết.

   - Lisp: ngoặc cân bằng, số hàm không đổi, CRLF, ASCII không đổi. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc182._ls`.
   - **Chạy thật (29/10, bản vẽ `DTK/130350`):** thứ tự hàng đúng (2, 3, 6, 9, 8 → 4, 5, 7 → 10, 11, 12) nhưng khung 1 vẫn cuối vì log `Tim thay 11 vung` — khung 1 trong DXF gốc là 4 LINE nét **DASHED2** (11 khung còn lại DASHED3) và chưa được ghép thành polyline kín như các khung kia (`Net DASHED2: 4 doi tuong` giữ nguyên) → không phải vùng → ghi chú rơi vào nhóm "ngoài vùng". **Người dùng xác nhận là do thao tác vẽ / ghép khung**, không sửa lisp thêm.

186. **Tấm gộp không được cắt — text tấm sau bị báo "cô đơn"** (người dùng báo kèm ảnh, 29/10/2026).

   ### Triệu chứng
   Một tấm profile dài vẽ liền (dải cao 50 + 2 gân, cao tổng 199.8; Dim `3,000` + `2,040`). Text `t=6mm 4枚` của tấm **26** nằm giữa hai gân (trong hình), text tấm **21** nằm **phía trên** gân phải. GRO không cắt: tấm 26 ôm cả hình, tấm 21 thành khung cô đơn.

   ### Nguyên nhân (đọc code `gro--tach-tam-gop-cad`, chưa có log)
   1. **Điều kiện 4 "thẳng hàng Y với text chủ"** (lệch tâm ≤ 1 chiều cao chữ): text 21 cao hơn text 26 ~250 mm → hình gộp không bao giờ được xét cắt, và không có dòng nào được in ra.
   2. **Dò vạch chia** `gro--tim-duong-chia-cat-kem-ename` đòi nét đứng cao **≥ 50 % chiều cao hình**: vạch chia ở đây chỉ cao bằng dải (50 / 199.8 = 25 %) → không thấy. Tệ hơn, nó lấy nét đứng **cao nhất** giữa 2 text — thành đứng của gân (~150 mm) có thể bị nhận nhầm là vạch chia → cắt sai chỗ.

   ### Sửa (lisp, ASCII)
   - Hàm mới `gro--tim-vach-chia-noi (s-host x-left x-right)` → `(x-div e-div)`: đoạn **đứng** của hình (LINE hoặc cạnh LWPOLYLINE thẳng, `gro--hinh-doan`), nằm trong hình (cách mép > 5 mm), giữa 2 text (±10 mm), mà **cả 2 đầu** đều nối vào nét ngang của hình đi sang **cả trái lẫn phải** (`gro--diem-noi-2-phia-p`). Không đòi chiều cao. Thành đứng của gân / bẻ không qua được: đầu trên chỉ có nét ngang về một phía. Nhận cả 2 polyline kín đặt sát nhau (cạnh chung). `e-div` chỉ có khi vạch là LINE riêng.
   - Điều kiện 4 thành: thẳng hàng Y **hoặc** có vạch chia nối giữa 2 text.
   - Bước 5 (chọn chỗ cắt): **ưu tiên** vạch chia nối, không có mới dùng cách cũ (≥ 50 %), rồi mới tới trung điểm 2 text.
   - Chẩn đoán: khi còn text thiếu hình nằm cạnh một hình rộng ≥ 1200 mà không cắt, in `[TACH TAM CAD] Text #n THIEU HINH, canh hinh gop cua Tam #m ... nhung KHONG cat: lech hang Y voi text chu va khong co vach chia noi giua 2 text (muc 186).` — trước đây im lặng.

   ### Kiểm chứng
   - Mô phỏng `sim186.py` 5/5: LINE chia nằm trong dải 50 → 3000; chỉ có thành gân → không nhận; 2 polyline kín sát nhau → 3000; vạch ngoài khoảng 2 text → không; tấm chữ nhật mục 93 (vạch cao hết tấm) → vẫn nhận.
   - Lisp: ngoặc cân bằng, +3 hàm (`gro--hinh-doan`, `gro--diem-noi-2-phia-p`, `gro--tim-vach-chia-noi`), 16 103 → 16 251 dòng, CRLF, ASCII không đổi, không gọi hàm chưa định nghĩa. Bản trước khi sửa: `backup/Guro ZWCAD(gro).lsp.truoc-muc186._ls`.

   > **Chưa chạy trên bản vẽ thật.** Nghiệm thu: chạy `gro` trên chính bản vẽ đó — phải có `[TACH TAM CAD] Phat hien Tam #21 bi gop trong Tam #26 ... Duong chia cat tai X = ...` đúng chỗ nối `3,000 | 2,040`; tấm 26 và 21 mỗi tấm một khung, hết khung cô đơn. Nếu thay vào đó in dòng `KHONG cat ... (muc 186)` thì vạch chia trên bản vẽ không nối vào nét ngang (vd hở, hoặc là cung) — gửi lại dòng đó + LIST của vạch chia.

   ### Chạy thật lần 1 (29/10, bản vẽ `test`, 83 ghi chú → 102 tấm)
   - **Cắt đúng 5 tấm gộp** (#215/#212, #221/#218, #233/#234, #271/#270, #292/#278). Cặp người dùng báo: `Tam #233 bi gop trong Tam #234 (Rong 5040mm) ... Duong chia cat tai X = 10730.9` = mép trái 7731 + **3000** đúng chỗ nối; bango 26 → hình X7731..10731, bango 21 → X10801..12841 (2040 + khe 20 + 50). DST: 0 hình cô đơn.
   - Text #247 (`ｹｶﾞｷ線 t=9mm`, nằm dưới tấm lớn 5040 × 1335 của #249) in dòng chẩn đoán `KHONG cat ... (muc 186)` — chưa rõ có phải tấm gộp không; bước tạo khung sau đó vẫn ghép được 1 hình cho #247.
   - **Chậm: `4d: cat tam gop` 24.7 s (63 % GRO).** Nghi phạm: `gro--tim-vach-chia-noi` nằm ở điều kiện 4, **trước** các điều kiện rẻ (cùng vùng, text đã có hình) → bị gọi cho gần mọi cặp text × hình rộng (cả text đang xét với chính hình của nó), mỗi lần `entget` lại mọi nét của hình, × 6 lượt vòng cắt.
   - **Sửa tốc độ:** đưa phép đo vạch chia xuống **cuối** `and` (chỉ chạy khi mọi điều kiện rẻ đã qua và text lệch hàng); đoạn của mỗi hình chỉ đọc **một lần mỗi lượt** (`gro--vach-cache`, biến local của `gro--tach-tam-gop-cad`, đặt lại đầu mỗi lượt vì id hình đổi sau khi cắt); tách sẵn đoạn ngang / đứng. Kết quả chọn vạch không đổi. Thêm mốc **`4d: cat + doi CAD`** để tách thời gian cắt thật (BREAK, dời) khỏi thời gian dò — nếu lần sau vẫn chậm thì biết ở phần nào.

   ### Lần 2 — cụm 64-67 vẫn không cắt (người dùng báo kèm ảnh, 29/10)
   Tấm 5040 = dải **2040** (cao 110) nối phần **3000** có gân (cao tổng 259.8); ghi chú 64/65 (`本図 1枚 / 逆図 1枚`, text #247) trên dải trái, 66/67 trên gân phải, **cùng hàng**. GRO: 66/67 ôm cả hình, 64/65 thành khung không hình (chính là dòng `Text #247 ... KHONG cat` lần 1).
   - **Nguyên nhân 1 — `gro--text-ke-hinh-p`:** chỉ nhận text TRÊN / DƯỚI hộp hình, chồng ≤ 100 mm. Gân phải đẩy đỉnh hộp hình lên cao hơn đáy cụm chữ 64/65 ~110 mm → text bị coi là "không cạnh hình" → hình đúng **không bao giờ được xét**; dòng chẩn đoán chỉ thấy hình #249 phía trên.
   - **Nguyên nhân 2 (phòng trước) — vạch chia ở BẬC:** nếu hai phần cao khác nhau, đầu trên của vạch chia chỉ nối nét ngang về **một** phía → luật "2 đầu đều nối 2 phía" không nhận → cắt ở trung điểm 2 text (sai chỗ).
   - **Sửa:**
     - `gro--text-ke-hinh-p` thêm nhánh: text **chồng lên dải Y** của hộp hình (text nằm cạnh gân) cũng là "cạnh hình".
     - `gro--diem-noi-2-phia-p` → **`gro--diem-noi-so-phia`** trả số phía (0/1/2). `gro--tim-vach-chia-noi` thêm tham số `chat` và chấm điểm: **2** = cả 2 đầu nối 2 phía; **1 (bậc)** = một đầu 2 phía + đầu kia 1 phía; thành gân / bẻ / khấc (mỗi đầu 1 phía) = 0, không bao giờ nhận. Chọn điểm cao nhất, bằng điểm thì dài nhất. Trả `(x-div e-div điểm)`.
     - Tìm hình chủ chạy **2 lượt**: lượt 1 chỉ hình có text chủ **thẳng hàng**; lượt 2 mới tới hình có vạch chia **chặt** (điểm 2) khi text lệch hàng. Hình đúng hàng (66/67) luôn thắng hình khác trên / dưới (#249).
     - Chọn chỗ cắt: vạch chia nối (cho phép bậc) → cách cũ (≥ 50 %) → trung điểm. Dòng log ghi rõ nguồn: `Duong chia cat tai X = ... (vach chia noi 2 dau | vach chia o BAC ... | net dung cao >= 50% hinh | *** KHONG tim thay vach chia - cat o TRUNG DIEM 2 text, KIEM TRA LAI)`.
   - Mô phỏng `sim186.py` **13/13** (thêm: bậc 2 polyline → 2040; bậc + lượt chặt → không; 1 polyline + LINE chia ở bậc → 2040; tấm bậc nguyên khối → không cắt; gân đơn → không; text cạnh gân → cạnh hình; text xa > 800 / ngoài X → không).
   - Lisp: ngoặc cân bằng, +3 hàm so với trước mục 186, CRLF (đã chuẩn lại sau khi `sed -i` của Git Bash đổi cả file sang LF — **đừng dùng `sed -i` trên file lisp này**), ASCII không đổi.

   > **ĐÃ CHẠY (29/10, bản vẽ `test`):** `Tam #247 bi gop trong Tam #244 ... Duong chia cat tai X = 9770.9mm (vach chia noi 2 dau)` = 7731 + **2040**; bango 64/65 hình 2040, 66/67 hình 3000; 6 tấm gộp được cắt, 83/83 text có hình, mã khắc 101/101, 0 hình cô đơn. Cả 6 vạch chia đều là loại "nối 2 đầu" (chưa gặp loại bậc).

   ### Lần 3 — tốc độ: `4d: cat tam gop` 45 s (76 % GRO), phần cắt thật chỉ 0.44 s
   - **Nguyên nhân:** vòng dò tìm text chủ của mỗi hình bằng `(vl-some '(lambda ...) text-reps)` **trước** mọi điều kiện → text × hình × text ≈ 83³ = 570 nghìn lần gọi lambda mỗi lượt; lượt 2 của mục 186 nhân đôi; vòng cắt chạy 7 lượt (6 lần cắt + 1 lần kiểm). Cách viết cũ đã có từ mục 131 (khi đó vòng thường chỉ 1 lượt nên không lộ).
   - **Sửa:** `h-tr` = `(assoc h-tid text-reps)` (phần tử đầu của `text-reps` là `stt`, kết quả y hệt); phép thử thẳng hàng / lệch hàng (4a) đặt **trước** nhánh "text đã có hình" (nhánh này gọi COM `gro--bbox` trong `gro--co-vach-chia-p`); vạch chia chặt (4b) đặt cuối. Thêm mốc đo con `4d: ghep hinh (Hungarian)` và `4d: tim tam gop` (còn `4d: cat tam gop` chỉ còn bước chuẩn bị).

   > **Chưa chạy lại sau khi tăng tốc.** Nghiệm thu: bảng thời gian — tổng `4d: ...` phải tụt từ 45 s xuống vài giây; kết quả cắt như lần 2 (6 tấm, X = 9770.9 cho #247).

   ### 186b — siết điều kiện cắt (người dùng thử: 1 text cô đơn + 1 nét Continuous thừa trong hình → hình bị cắt nhầm, đoạn cắt ra thành hình cô đơn, text vẫn cô đơn)
   Một nét thừa chạy từ mép trên xuống mép dưới hình trông **y hệt** vạch chia; và khi không thấy vạch chia, lisp vẫn cắt ở trung điểm 2 text. Nay mọi cặp (text thiếu hình, hình chủ) qua đủ điều kiện cũ còn phải qua **phép kiểm trước khi cắt** `gro--ly-do-khong-cat` (cả đường thẳng hàng lẫn lệch hàng):
   1. **Phải có vạch chia thật** (`gro--chon-vach-chia`: vạch nối → cách cũ ≥ 50 %). **Bỏ cắt ở trung điểm 2 text.**
   2. **Cùng độ dày `t=`** (`gro--t-cua-tr`, đọc bằng `gro--fields`; không đọc được thì bỏ qua phép này) — tấm khách chia vì quá khổ là một tấm thép.
   3. Mỗi đoạn cắt ra **≥ 200 mm**.
   4. Text thiếu hình **gần đoạn của nó hơn đoạn còn lại**, và cách đoạn đó **≤ 800 mm** (`gro--kc-hop`, đúng tầm ghép hình) — không thì cắt xong text vẫn cô đơn, đẻ thêm 1 hình cô đơn (đúng ca trong ảnh).

   Không đạt → bỏ cặp đó, tìm cặp khác; nếu không còn cặp nào, in `[TACH TAM CAD] KHONG cat Tam #m cho Text #n: <lý do> (muc 186b).` Vạch chia chọn ở bước 4 được dùng luôn ở bước 5 (không tính lại).

   > **Giới hạn (nói rõ với người dùng):** nét thừa nằm **đúng giữa 2 text**, text cô đơn nằm ngay trên đoạn hình của nó, cùng `t=` → hình học không phân biệt được với tấm gộp thật → **vẫn cắt**. Dòng log `Duong chia cat tai X = ... (<nguồn>)` cho biết vạch nào được dùng để kiểm lại.

   - Mô phỏng `sim186b.py` 10/10 (26/21, 64-67, mục 93 vẫn cắt; ca ảnh → `xa > 800`; khác `t=`; đoạn < 200; không vạch; text vắt qua vạch; ca giới hạn → vẫn cắt). Lisp: +3 hàm (`gro--chon-vach-chia`, `gro--t-cua-tr`, `gro--ly-do-khong-cat`), ngoặc cân bằng, CRLF, ASCII không đổi. Bản trước: `backup/Guro ZWCAD(gro).lsp.truoc-muc186b._ls`.

   > **Chưa chạy trong ZWCAD.** Nghiệm thu: (1) bản vẽ thử của người dùng → không cắt, có dòng `KHONG cat ... (muc 186b)`; (2) bản vẽ `test` → vẫn cắt đủ 6 tấm như lần 2.

   ### 186c — người dùng chốt 2 thay đổi luật (29/10)
   | Luật | Trước | Nay |
   |---|---|---|
   | Bề rộng hình gộp | ≥ 1200 mm | **Bỏ** (có tấm gộp nhỏ hơn). Phép kiểm 186b vẫn đòi mỗi đoạn ≥ 200 mm; dòng chẩn đoán chỉ liệt kê hình ≥ 400 mm (nhỏ hơn không thể cắt ra 2 đoạn ≥ 200) |
   | Thẳng hàng với text chủ | lệch tâm Y ≤ 1 chiều cao chữ | lệch tâm Y ≤ 1 chiều cao chữ **+ phần text chủ chồng vào chiều cao hình** (`h-chong` = độ cao đoạn hộp text chủ nằm trong dải Y của hộp hình; text chủ ở ngoài hình → 0, như cũ) |

   Lượt 2 (lệch hàng + vạch chia nối chặt) giữ nguyên, dùng cùng ngưỡng mới. Lisp: không thêm hàm, ngoặc cân bằng, CRLF.

   > **ĐÃ CHẠY (29/10, bản vẽ `test`, bản 186 + 186b + 186c + tăng tốc):** vẫn cắt đúng 6 tấm gộp (#215, #221, #233, #247, #271, #292), cả 6 vạch chia loại "nối 2 đầu"; 83/83 text có hình, 0 hình cô đơn, mã khắc 101/101. **Tốc độ:** tổng các bước `4d` 45 s → **1.42 s** (Hungarian 0.35, tìm tấm gộp 0.34, cắt + dời 0.44); cả GRO **14.8 s** (2.76 s là thao tác chọn). Chưa có log bản vẽ thử nét thừa (186b).

210. **Tấm gộp không cắt khi ghi chú dài hơn đoạn ngắn** (người dùng báo kèm ảnh, 05/11/2026).

   ### Triệu chứng
   Thanh dài vẽ liền `570 | 240` (cao 22.4), ghi chú 89 trên đoạn 570, ghi chú 92 trên đoạn 240, cùng hàng. GRO: khung 89 ôm cả 810, khung 92 không có hình (92 thành text thiếu hình).

   ### Nguyên nhân (đọc code, chưa có log)
   Phép kiểm 186b số 4 trong `gro--ly-do-khong-cat` đòi hộp text thiếu hình **gần hẳn** đoạn của nó: `d1 < d2`. Ghi chú 92 (`ｹｶﾞｷ線 t=6mm` rộng ~330 mm) dài hơn đoạn 240 nên **lấn qua vạch chia** sang đoạn 570 vài chục mm; text nằm trên cả hai đoạn → `d1 = d2` (cùng khe đứng) → báo `text khong gan doan hinh cua no hon doan con lai (x / x mm) (muc 186b)` và không cắt. Tâm text vẫn nằm rõ trên đoạn 240.

   ### Sửa (lisp, ASCII)
   - `d1 > d2` (+0.5) → vẫn bỏ như cũ.
   - `d1 = d2` (±0.5, text vắt qua vạch) → **cho cắt** khi tâm text cách vạch **≥ 1/4 bề rộng text** (≥ 3/4 text nằm phía đoạn của nó); gần vạch hơn → lý do mới `text vat qua vach chia tai X = ... (muc 186b)`.
   - Mô phỏng `sim210.py` 3/3: ca ảnh (text 330 lấn 30 mm) → cắt (cũ: không cắt); text gọn trên đoạn 240 → cắt; text đặt đúng giữa vạch → không cắt.
   - Lisp: không thêm hàm, ngoặc cân bằng, CRLF, ASCII.

   ### 210b — ngưỡng 1/4 vẫn chặn (chạy thật 17:47, cụm 67-68-69, thanh `390 | 840 | 725`)
   - Số liệu từ dump `%TEMP%\gro_xepcum_dump.txt` / `gro_dat_mk_dump.txt` (log dòng lệnh chưa ghi vào nhật ký): hình 68 = X 9477.6 → 10707.6 (**1230 = 390 + 840**, chưa cắt); 69 đã cắt đúng (cách 20). Vạch chia thật: LINE `0-KAKOU` X **9867.6** cao hết thanh 90. Ghi chú 67 rộng **555** (X 9487.3 → 10042.3) trên đoạn **390**, tâm 9764.8; khe xuống cả 2 đoạn = 52.3 → `d1 = d2`; tâm cách vạch 102.8 < 1/4 × 555 = 138.8 → `text vat qua vach chia` → không cắt.
   - **Người dùng chốt:** bỏ ngưỡng. `d1 = d2` → **cắt**; phía của text đã quyết bằng tâm text (`p-t` chọn theo `t-cx`). Chỉ bỏ khi `d1 > d2` (+0.5). Bảo vệ chống nét thừa 186b còn: vạch chia thật, cùng `t=`, đoạn ≥ 200, khe ≤ 800, vạch nằm giữa tâm 2 ghi chú (±10).
   - **Ghi nhật ký:** hàm mới `gro--tt-in` — 3 dòng `[TACH TAM CAD]` (phát hiện + cắt tại X, `KHONG cat ... (muc 186b)`, text thiếu hình cạnh hình gộp) vừa in dòng lệnh vừa ghi `ohta.log` qua `ohta_net_log` (1 dòng, `\n` → ` | `). Lần sau đọc thẳng lý do ở `%LOCALAPPDATA%\OHTA_TOOL\logs\ohta.log`.
   - Mô phỏng `sim210b.py` 5/5 (số thật 67: gốc + 210a không cắt, 210b cắt; 92 ảnh trước vẫn cắt). Lisp: +1 hàm, ngoặc cân bằng, CRLF, ASCII.

   > **Chưa chạy trong CAD.** Nghiệm thu: mở lại CAD, GROCHAY trên bản vẽ cụm 67-69 → `ohta.log` có `[TACH TAM CAD] Phat hien Tam #.. bi gop ... Duong chia cat tai X = 9867.6mm ...`; 67 hình 390, 68 hình 840, 69 hình 725. Bản vẽ `570 | 240` (89/92) cũng phải cắt.
