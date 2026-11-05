# 14. Cần kiểm tra lại

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Cần kiểm tra lại

> Các mục dưới đây **chưa được xác minh bằng cách chạy thực tế** — đừng ghi nhận chúng như
> đã xong. Sửa được mục nào thì gạch ngang mục đó và ghi rõ đã kiểm chứng bằng cách nào.

- ~~`(rtos (atof ...))` không truyền mode/precision~~ — đã sửa, xem mục 10 ở trên.
- **Hộp thoại của `gro` đã hiện được trên ZWCAD** (người dùng xác nhận, sau khi sửa mục 18-20).
  **Nhưng kết quả tạo entity thì chưa đối chiếu.** Cách nghiệm thu: copy bản vẽ ra 2 file,
  file A chạy `bg`→`mk`→`FN`, file B chạy `gro`, rồi so entity trên 3 layer
  (`0`, `3-KEGAKI`, `2-F-NAME`) + nội dung 2 file CSV. Lưu ý **thứ tự đánh số nay CỐ Ý khác
  nhau** (mục 22) và `gro` có thể tạo **nhiều tấm hơn** do tách `本図`/`逆図` (mục 24).
- **Thuật toán mới ở mục 22-24 chưa chạy trên ZWCAD.** Đã kiểm offline bằng **mô phỏng
  Python 36 trường hợp, tất cả đạt**: 8 tổ hợp `t=` nửa/toàn chiều rộng; `t=6㎜*3` → TH3
  Q=3; từ chối `150` / `1.550` / `1,022.4` / `φ34` / `Plate 100x200`; và 7 ca gom hàng
  (bản vẽ thật 3 hàng, tất cả một hàng, một cột dọc, một ghi chú, trùng hệt toạ độ…).
  Ngoặc cân bằng, phần code mới ASCII thuần 100%. **Nhưng mô phỏng không phải chạy thật** —
  vẫn phải nghiệm thu bằng `grosort` trên bản vẽ 梁 No-3,4.
- **Chế độ đánh số theo VÙNG (mục 25) chưa chạy trên ZWCAD.** Mô phỏng Python 15 trường hợp
  đều đạt: ray-casting trong/ngoài hình chữ nhật và hình L (kể cả phần khuyết), vùng lồng
  nhau chọn đúng vùng nhỏ nhất và **không phụ thuộc thứ tự đầu vào**, và thứ tự 7 vùng mô
  phỏng theo bố cục bản vẽ thật ra đúng thứ tự đọc. Nghiệm thu bằng `grosort`: chọn ghi chú
  rồi chọn polyline, xem cột `vung` có khớp với các khung nét đứt nhìn thấy không.
- ~~Ghi chú có điểm chèn nằm ngoài khung bị bỏ sót~~ — **đã bịt ở mục 51**: cửa sổ `ssget "_C"`
  lấy cả text chỉ *giao* với khung, và báo cáo in `[N ghi chu vao duoc nho cua so CAT]`.
- ~~Chế độ vùng phụ thuộc tầm nhìn (mục 51)~~ — **đã sửa ở mục 58** (bỏ `ssget "_C"` khỏi khâu
  quét). **Chưa đo lại trên bản vẽ thật:** chạy `grosort` hai lần, một lần zoom hết một lần
  zoom sát, tổng số ghi chú phải bằng nhau (63).
- **Khâu ĐẶT bango vẫn dùng `ssget "_C"`** nên vẫn phụ thuộc tầm nhìn (mục 58) — nhưng mục 59
  đã cho `gro` **tự zoom** thấy hết vùng trước khi đặt, và **đo lại** để xác nhận. Chưa chạy
  thật: kiểm bằng cách zoom sát vào một góc rồi chạy, xem có in `Da zoom cho thay het...` không.
- **Tốc độ sau mục 58 chưa đo.** Nay đọc hết TEXT/MTEXT của bản vẽ rồi mới lọc, thay vì chỉ
  đọc phần trong khung. Chậm thấy rõ thì tối ưu bằng cách lọc thô theo điểm chèn trước khi
  gọi `gro--bbox`.
- **Luật "chỉ nhận polyline kín 4 đỉnh" (mục 51) chưa thử trên khung thật.** Nếu bản vẽ
  `JR西宮駅前` nhận ít hơn 13 khung thì đọc dòng `(bo qua: …)` để biết khung nào sai kiểu.
- **`ssget "_X"` quét toàn bộ bản vẽ** kể cả không gian giấy và các layout khác. Đang dựa
  vào việc toạ độ của chúng không rơi vào polyline ở không gian mô hình. Chưa thử trên bản
  vẽ có layout dùng cùng vùng toạ độ với model space.
- **Ba hằng số layout chưa kiểm bằng mắt** — `gro--buoc-tam` = `15.0 + h*1.5`,
  `gro--fname-cao` = `1.0`, `gro--fname-dong` = `1.6667`. Chúng quyết định: hai tấm tách ra
  từ `本図`/`逆図` có đè lên nhau không, và F-name có nằm gọn trong dải mã khắc không. Đều
  suy ra từ chiều cao khối chứ **chưa nhìn bản vẽ thật**. Sai thì chỉnh đúng một chỗ — mỗi
  hằng số một hàm riêng.
- **Giãn dòng `1.6667` là hệ số mặc định của MTEXT**, chọn để F-name dạng TEXT trông giống
  hệt bản MTEXT cũ. Chưa đo lại trên bản vẽ xem ZWCAD có thật dùng hệ số đó không.
- **Chưa tách bạch được mục 18 hay mục 19 mới là thứ chữa được lỗi hộp thoại không hiện**
  (sửa cùng lúc). Nếu sau này gặp lại lỗi tương tự ở hộp thoại khác, thử `start_list` có
  tham số `3` trước — đó là nghi phạm chính.
- **`guro--write-dcl` của lệnh `bg` vẫn ghi DCL bằng một chuỗi lớn có `\n` lồng bên trong**,
  chưa đổi sang cách ghi từng dòng như `gro--write-dcl`. Hộp thoại của `bg` chưa được thử
  lại sau đợt sửa này.
- **Thứ tự ghép cặp của `FN`** — `FN` duyệt selection set **ngược** (từ cuối về đầu) rồi ghép
  với CSV theo thứ tự xuôi, trong khi `bg` sắp xếp CSV theo toạ độ (Y giảm dần, X tăng dần).
  Nếu thực tế hay báo "Error name!!!" hàng loạt thì nên cho `FN` sắp xếp mã khắc theo cùng
  quy tắc.
  > **Rủi ro này KHÔNG áp dụng cho `gro`.** `gro` không có khâu ghép cặp nào: bango, mã khắc,
  > F-name và dòng CSV của cùng một tấm đều tạo trong cùng một vòng lặp từ cùng một biến
  > `bango`, nên không thể lệch nhịp. Đó cũng là lý do `gro` bỏ hẳn phép kiểm tra
  > "Error name!!!" (chỉ còn `Error T!!!` và `Error Q!!!`). **Dùng `gro` là cách tránh lỗi
  > này, không cần chờ sửa `FN`.**
- **Hai ngưỡng vật liệu còn lệch nhau: `> 3.2` (cột M của CSV) vs `>= 4.5` (M/Z của F-name).**
  Xem bảng ở [Bảng dữ liệu và layer](04-du-lieu-va-layer.md). Tấm 4.5mm thì cả hai đều ra
  `SS400`, nhưng tấm **4.0mm** (không thuộc độ dày chuẩn) cho CSV `SS400` mà F-name `SPHC` —
  tức là hai nơi mâu thuẫn nhau về cùng một tấm. `gro` **cố ý giữ nguyên cả hai ngưỡng** để
  kết quả so sánh được với bản `bg`+`FN` khi nghiệm thu; nếu thống nhất lại thì phải quyết
  định ngưỡng nào đúng theo nghiệp vụ rồi sửa **cả `bg--fields` lẫn `FN` lẫn `gro`** cùng lúc.
- **`height` dùng chung trong `bg` — vẫn còn, đã sửa trong `gro`.** `bg` đặt `height` trong
  vòng đọc rồi dùng lại ở vòng `repeat` phía sau, nên **mọi** bango đều lấy chiều cao của đối
  tượng được xử lý **cuối cùng** — sai vị trí nếu bản vẽ có ghi chú nhiều cỡ chữ. `gro` lưu
  `height` theo từng tấm và dùng đúng cái đó khi đặt bango (dải dung sai của `vl-sort` vẫn
  dùng một `height` chung như `bg` để thứ tự sắp xếp không đổi).
- **Vị trí đặt bango với MTEXT.** `bg` lấy `(assoc 10 ...)` làm gốc rồi dời xuống
  `height + 30`. Với TEXT group 10 là điểm chân dòng (baseline-left), còn với MTEXT group 10
  là điểm chèn phụ thuộc `attachment point` (group 71) — mặc định là **góc trên-trái**. Nên
  số bango của MTEXT có thể lệch xuống khoảng một chiều cao dòng so với của TEXT. Chưa sửa
  vì chưa rõ layout thực tế mong muốn. `gro` thừa hưởng y nguyên đặc điểm này.
- `vl-sort` trong `mk` (và trong `bg`) **loại bỏ phần tử trùng** khi hàm so sánh cho rằng
  hai phần tử bằng nhau. Với `mk` sắp theo `(atof noi-dung)`, hai bango cùng số sẽ mất một
  cái. Đã thêm cảnh báo in ra khi số phần tử sau khi sort ít hơn trước, nhưng chưa test
  thực tế. **`gro` đã hết hẳn lỗi này** nhờ khoá `stt` duy nhất (mục 22) — không cần cảnh
  báo nữa.
- **`bg` vẫn còn NGUYÊN cả ba nhóm lỗi mà `gro` đã sửa** (cố ý, để còn đối chiếu):
  hàm so sánh không bắc cầu + dung sai `height*3` quá nhỏ + `height` lấy của đối tượng cuối
  (mục 22); tách dữ liệu kiểu danh sách đen (mục 23); và `Q=` nối chuỗi mọi chữ số thay vì
  tách tấm — `… 本図 1枚 逆図 1枚` cho `Q="11"` chứ không phải 2 tấm (mục 24).
- **Kiểu banding không bắc cầu ở mục 22 còn ở nhiều file khác trong codebase** — cùng một
  đoạn `(if (equal Ya Yb tol) (< Xa Xb) (> Ya Yb))`: `guro ZWCAD(BG MK FN).lsp` dòng 753
  (`bg`), `1. xuất fname ra csv\Danh_bango_guro.lsp` dòng 175. Ngoài ra các file
  `xuat_csv_v*.lsp` và `2. project A\xuat_csv_v4.0.0.lsp` sort **một khoá duy nhất không
  tie-break**, nên `vl-sort` **âm thầm mất dòng CSV** khi hai khung trùng `xmin`/`ymax` —
  các file đó còn không có cảnh báo như GURO. Chưa sửa.
- **Thông tin thuận/ngược (`本図`/`逆図`) KHÔNG đi vào CSV.** Hai tấm tách ra chỉ phân biệt
  bằng số bango; CSV và F-name giữ nguyên định dạng 6 cột nên `FN` không phải sửa. Nếu
  xưởng cần biết tấm nào là bản ngược (để lật khi cắt) thì phải bổ sung sau — cờ `V=` đang
  dùng cho bẻ/vát, **không** dùng lại được.
- **`ssget "_C"` có bỏ sót đối tượng NGOÀI khung nhìn không?** (mục 41) Nếu phép chọn cửa sổ
  chỉ thấy đối tượng **đang hiện trên màn hình** (như AutoCAD), thì khi zoom sát vào một
  góc rồi chạy `gro`, các ứng viên nằm ngoài màn hình sẽ luôn báo "sạch" dù đè lên hình.
  Lớp lọc hình học của mục 41 chỉ **bớt** được bắt nhầm, **không thêm** được đối tượng mà
  `ssget` bỏ sót. Cách tránh an toàn: **zoom để thấy toàn bộ vùng cần đánh số** trước khi
  chạy `gro`. Cách kiểm: chạy hai lần trên cùng bản vẽ, một lần zoom hết, một lần zoom sát,
  rồi so vị trí bango.
- **Thứ tự ưu tiên vị trí bango của ghi chú 1 tấm (mục 44 + 45) chưa chạy trên bản vẽ thật.**
  Đã kiểm offline `sim_uutien.py` 13 trường hợp, mô phỏng đúng kiến trúc `gro--dat-cot` hiện
  tại. Nghiệm thu: nhìn dòng đếm `tren= duoi= trai= phai= cot trai=` và vài ghi chú 1 tấm ở
  vùng trống — phải nằm **trên, căn giữa** cụm chữ.
- **`gro--dau-canh` phụ thuộc NGẦM vào thứ tự nội bộ của `gro--vi-tri-bango`** (mục 45): nó lấy
  ứng viên đầu tiên của một cạnh và **coi đó là ô giữa** cạnh ấy. Đúng vì mỗi cạnh đã qua
  `gro--sap-tu-giua`. Đổi `gro--sap-tu-giua` hoặc thứ tự `append` cuối `gro--vi-tri-bango` thì
  hàm này **lặng lẽ trả về ô khác** mà không có gì báo. Đây là chỗ dễ hỏng trong im lặng nhất
  của cả lớp đặt vị trí.
- **`entmake` bản ghi bảng STYLE chưa được chứng minh trên ZWCAD** (mục 46). Cả file này lẫn
  `xuat_csv_v5.0.1 ZWCAD.lsp` chưa dùng `entmake` trên **bảng symbol** lần nào — khác hẳn
  `entmake` trên entity thường. Đã có đường dự phòng ActiveX và đường cuối là dùng kiểu chữ
  hiện hành, nên hỏng cũng không chặn lệnh. Nghiệm thu: chạy `gro` trên bản vẽ **chưa có**
  kiểu `guro`, đọc dòng lệnh xem tạo bằng đường nào, rồi mở Text Style Manager đối chiếu
  Height / Width factor / Oblique / Font.
- **Kiểu chữ `guro` đã có sẵn mà Height ≠ 0**: lệnh chỉ **cảnh báo**, cố ý **không tự sửa**
  kiểu chữ của người dùng. Nếu thực tế hay gặp thì cân nhắc cho phép sửa — nhưng phải hỏi
  người dùng trước, vì kiểu chữ đó có thể đang dùng cho đối tượng khác trong bản vẽ.
- **`(cons 7 …)` của `entmake` TEXT có được ZWCAD tôn trọng không?** (mục 47) Chưa biết —
  `gro` nay không phụ thuộc vào câu trả lời nữa (đọc lại rồi vá bằng `entmod`), nhưng
  **`FN` thì vẫn phụ thuộc**: nó `entmake` MTEXT F-name với `(cons 7 style)` và không kiểm
  lại bao giờ. Chạy `grostyle` là biết ngay; nếu `entmake` bỏ qua group 7 thì `FN` đang âm
  thầm tạo F-name sai kiểu chữ từ lâu.
- ~~Trần ngưỡng `hmax*5` (mục 49) chưa chạy trên bản vẽ thật~~ — **đã nghiệm thu**: dựng lại
  cả bảng `grosort` của `JR西宮駅前` (63 ghi chú, 13 vùng) khớp 100% với bản mới; 4 vùng
  (1, 2, 3, 10) được sửa, 9 vùng còn lại không đổi. Vẫn để ngỏ chiều ngược lại: con số 5 suy
  từ số đo của mục 28, chưa gặp bản vẽ nào bị **chia quá tay**. Vùng 12 (ngưỡng 237.3) là ca
  sát trần nhất đã gặp — bản vẽ nào có ngưỡng tự nhiên trong khoảng 300-400 thì đáng soi lại.
  Nếu sau này gặp ca **chia quá tay** (một hàng bị xé làm hai ở chỗ không nên), nới 5 lên —
  sửa đúng một chỗ là `gro--nguong-toi-da`. Muốn biết trần có đụng vào hay không thì nhìn
  nhãn `/chan` trong báo cáo hoặc `grosort`.
- **Chế độ thường (không vùng) chưa được thử lại sau mục 49.** Trần chỉ làm tăng số hàng nên
  rủi ro là chia quá tay trên bản vẽ nhiều ghi chú cao thấp lệch nhau. Cách kiểm nhanh:
  chạy `grosort` không chọn polyline, so số hàng trước/sau.
- **Popup "Layer khung bao" (mục 52) chưa thử trên bản vẽ thật.** Nó quét thêm một lượt
  `ssget "_X"` các polyline **mỗi lần mở hộp thoại** — chưa đo thời gian trên bản vẽ nhiều
  polyline. Nếu chậm thấy rõ thì nhớ kết quả vào biến toàn cục trong phiên.
- **Nếu khung của người dùng KHÔNG phải polyline kín 4 đỉnh** thì popup sẽ không liệt kê
  layer đó, và danh sách rơi về "tất cả layer" kèm cảnh báo. Khi đó phải sửa khung trên bản
  vẽ (đặt cờ closed / vẽ lại bằng RECTANG), chứ không phải sửa lisp — trừ khi quyết định nới
  luật nhận khung ở mục 51.
- **Hai cấp khung bao + vùng (mục 53) chưa chạy trên bản vẽ thật.** Nghiệm thu trên
  `JR西宮駅前`: tích cả hai ô, chọn khung ngoài cùng làm khung bao, layer vùng = layer của 13
  khung cụm. Phải ra `Nhom: 1 khung bao + 13 vung` và tổng ghi chú ≥ 63.
- **Nếu khung bao và vùng nằm CÙNG một layer** thì popup hai ô sẽ cùng trỏ vào layer đó, và
  `gro--tim-vung` sẽ nhặt cả khung bao lẫn các vùng. `gro--trong-khung-p` loại được chính
  khung bao (hộp bao trùng khít), nhưng chưa thử thực tế. Cách chắc ăn: để khung bao ở layer
  riêng.
- **Tự nhận diện mã khắc (mục 54) chưa chạy trên bản vẽ thật.** Nghiệm thu trên `JR西宮駅前`:
  phải in `Ma khac TU NHAN DIEN: 0`. Nếu không nhận được, nguyên nhân hay gặp nhất là dòng
  `岡田鉄工さん…` **nằm ngoài khung bao** — nó ở góc dưới phải, ngoài 13 khung cụm.
- **Danh sách tên hãng (mục 54) mới có 4 cái.** Gặp khách mới thì thêm vào `gro--ma-ten`,
  nhớ lấy mã Unicode bằng cách mã hoá thật chứ không gõ tay, và cân nhắc cả dạng nửa chiều
  rộng nếu là katakana.
- **Mọi mẫu chữ Nhật mới đem đi so khớp PHẢI kiểm bằng đường máy kiểu A** (mục 57). Mô phỏng
  trên mã Unicode đi đúng đường của máy kiểu B và **không bao giờ** bắt được lỗi thiếu ký tự
  trong `bg--uni-tbl`. Dùng `sim_mayA.py` — nó đọc thẳng bảng từ file `.lsp`.
- **Tên bản vẽ trong khung bao (mục 60) chưa chạy thật.** Hai thứ cần nhìn bằng mắt: cỡ chữ
  `hmax*2.25` có hợp lý trên bản vẽ không, và con số `(N cai khong co cho sach)` có cao không
  — cao nghĩa là dải 1/5 trên quá chật, nới `gro--ten-bv-hang` hoặc đổi tỉ lệ dải.
- **Vùng > 4 đỉnh (mục 61) chưa chạy thật.** Nghiệm thu bằng `grosort` chế độ vùng: số vùng
  nhận được phải khớp số khung cụm nhìn thấy. Còn thiếu thì đọc `(bo qua: …)` xem rớt vì
  **không kín** hay **dưới 3 đỉnh**.
- **`gro--vung-giao` vẫn chỉ so hộp bao** (mục 61) nên với vùng hình L nó rộng hơn thực tế.
  Chỉ ảnh hưởng ghi chú có điểm chèn nằm ngoài mọi vùng. Nếu gặp ghi chú bị gán nhầm vùng
  thì đây là chỗ đầu tiên cần soi.
- **Đã đổi sang `ssget "_CP"` (mục 63) — chưa chạy thật.** An toàn của nó phụ thuộc HOÀN TOÀN
  vào việc bước zoom chạy **trước** khâu quét. Nghiệm thu: zoom sát một góc rồi chạy `gro`,
  tổng ghi chú phải vẫn là 63. Nghi ngờ thì chạy `growp` (cố ý không tự zoom) để so hai cách.
- **Vẫn chưa biết `_CP` có phụ thuộc tầm nhìn hay không** — chỉ biết là **không cần biết nữa**
  vì đã zoom trước. Nếu sau này bỏ bước zoom thì câu hỏi này sống lại ngay.
- **Hai popup layer dùng hai luật đếm (mục 64) chưa thử thật.** Nghiệm thu: mở hộp thoại, so
  hai popup — popup vùng phải có **ít nhất** tất cả layer mà popup khung bao có, và thường
  nhiều hơn. Hoặc chạy `grodbg` đọc hai dòng `layer khung bao:` / `layer vung:`.
- **Luật "vùng thuộc khung bao nào" (mục 65) chưa chạy thật.** Nghiệm thu: so số vùng tìm
  được với số khung cụm nhìn thấy trên bản vẽ. Còn thiếu thì đọc dòng
  `(N vung ... bi bo vi khong cham khung bao nao)`.
- **Bộ dò biến rò ra toàn cục vẫn nhiễu với `bg--`/`fn--`** (mục 65): nó bắt nhầm chữ nằm
  trong chuỗi của `setq` nhiều biến. Muốn rà nốt nhóm đó thì phải sửa bộ dò trước, đừng tin
  danh sách nó in ra.
- **Hộp thoại .NET của `gro` (mục 86) chưa chạy thật trong ZWCAD.** Biên dịch được, logic thuần
  đã test, nhưng 5 điều chỉ biết khi chạy: (1) `Application.ShowModalWindow` gọi từ trong một
  `LispFunction` có hiện được không; (2) `vl-catch-all-apply` với hàm do .NET đăng ký; (3) tên
  layer có **chữ Nhật** đi lisp → .NET → lisp trên **máy kiểu A (CP932)** có giữ nguyên không — nếu
  hỏng, popup chọn sai layer và `gro` báo 0 khung; (4) `Editor.GetEntity` sau khi đóng cửa sổ;
  (5) `ColorDialog` mở chồng lên cửa sổ WPF. Nghiệm thu nhanh: `GRODLGNET` trước, rồi `gro`.
  Hỏng bất kỳ điểm nào thì lisp **tự quay về DCL** (chuỗi `"LOI"` / lỗi lisp), không chặn công việc.
- **Bảng DST mới (mục 86) chưa chạy thật.** Nghiệm thu: số dòng bảng = số tấm `gro` báo; một khung
  có cặp 本図/逆図 phải ra **hai** dòng; sửa bango một tấm rồi Lưu → TEXT bango, đuôi mã khắc và
  đuôi `N=` cùng đổi, phần trước giữ nguyên. Dòng nào hiện "[!] Thiếu mã khắc / F-name" thì soi
  luật ghép (đoạn số cuối + cùng X với dòng `N=`).
- **Bảng DST lưu cả CSV (mục 86) chưa chạy thật.** Nghiệm thu: (1) sửa Q một tấm → Lưu → mở CSV xem
  đúng dòng đó đổi, các dòng khác giữ nguyên; (2) mở CSV bằng Excel rồi bấm Lưu → phải báo "đang mở"
  và bản vẽ KHÔNG đổi; (3) cột GC khớp cột cuối của CSV. Nếu GC trống hết thì kiểm đường dẫn CSV
  in ở dòng lệnh `[DST] CSV: …` — tên đơn cắt đuôi phải ra đúng tên file CSV mà `gro` đã ghi.
- **Zoom tới tấm trong bảng DST (mục 86) chưa chạy thật.** Cần biết `SetCurrentView` gọi từ palette
  có ăn trên ZWCAD không. Nếu dòng lệnh hiện `'_.ZOOM _W …` mỗi lần bấm thì là đang chạy đường dự
  phòng (khung nhìn không đổi sau `SetCurrentView`) — vẫn đúng chỗ, chỉ là chậm hơn.
- **Ghép dòng số lượng theo bảng ứng viên (mục 168) chưa chạy trên ZWCAD.** Mô phỏng 19/19 đạt. Nghiệm thu: bản vẽ có cụm 54/55/56 — ghi chú trái 1 tấm, tấm 56 Q=4, hết khung `CANH_BAO_SO_LUONG`. Soi thêm các dòng `[GHEP SL]` in ra: mỗi dòng là một chỗ hành vi khác bản cũ — đọc lướt xem có chỗ nào chọn sai không. Nếu gặp ca dòng số lượng thụt xa bị ghi chú bên cạnh (tâm gần hơn) lấy mất, đó là giới hạn của luật "gần hơn tâm–tâm" người dùng chốt.
- **Ghi chú tách rời (mục 128) nay chỉ xét gần hơn, bỏ ưu tiên "text nằm dưới" (mục 168) — chưa chạy thật.** Soi các tấm có dòng `ｹｶﾞｷ線` đứng riêng nằm giữa hai text: ghi chú kegaki / mã khắc phải về đúng tấm. Nếu hay về nhầm tấm phía trên thì đây là chỗ cần xem lại.
- **Ghép đường dim chính bị tách nhiều LINE (mục 169) chưa chạy trên ZWCAD.** Mô phỏng 8/8. Soi các dòng `muc 169` in ra: nếu có chuỗi ghép nhầm (vd đường dim nối thẳng hàng với một nét khác cùng trục) thì siết khe nối 1.0 mm / lệch ngang 0.5 mm trong `gro--chuoi-thang-hang`.
- ~~Đặt mã khắc vào hình bằng C# (mục 170) chưa chạy trong ZWCAD~~ — **đã nghiệm thu 28/10: 65/65 tấm** (sau khi sửa lỗi làm tròn hộp hình). Còn để ngỏ: chưa so vị trí từng cụm với bản lisp cũ, chưa thử bản vẽ có polyline / spline làm biên dạng. Test logic 454/454, build 0 lỗi, DLL mới **đã chép** (28/10, `build.ps1`) vào `C:\ZwCadPlugin` (chạy `ZwCadPlugin/build.ps1` — PostBuild tự chép DLL; đóng ZWCAD trước). Cần biết khi chạy thật: (1) `Polyline.GetBulgeAt` / `GetPointAtParameter` của ZWCAD lấy mẫu giống `vlax-curve` (so vài tấm với bản lisp cũ trong `backup/Guro ZWCAD(gro).lsp.truoc-muc170._ls`); (2) `TransformBy` trên MTEXT mã khắc + TEXT F-name cho cùng kết quả `vla-scaleentity` / `vla-rotate` / `vla-move`; (3) ghi `ent.XData` trong `LispFunction` lúc lệnh đang chạy không bị từ chối.
- ~~Escape `\U+XXXX` trong chuỗi lisp (mục 191) chưa kiểm trên AutoCAD 2027~~ — **đã kiểm 03/11: AutoCAD KHÔNG dịch** (`(strlen "\U+03C6")` = 7). Đã thêm ký tự thật `(chr …)` cho ký hiệu phi (`gro--extract-num-after-sym`) và ký hiệu độ (bước bóc ký hiệu) qua `gro--ky-tu-unicode` — chỉ trên AutoCAD; ZWCAD giữ nguyên vì máy kiểu byte CP932 cho `(chr 966)` = byte 198 = U+FF86, dễ khớp nhầm. Thử trên AutoCAD: `φ12` → 12, `4-%%C20` → 20, `R15` → 15. **Chưa chạy trọn GRO trên bản vẽ thật bằng AutoCAD.** Ghi chép cũ: Khi chuyển sang 100% ASCII, 25 dòng code có ký tự Nhật / ký hiệu viết thẳng (`"°"`, `"φ"`, `"入"`…) đã đổi thành `"\U+00B0"`, `"\U+03C6"`, `"\U+5165"`. Trên ZWCAD chữ UTF-8 viết thẳng vốn bị đọc theo CP932 thành mojibake nên không đổi hành vi. Trên AutoCAD (đọc `.lsp` UTF-8 được) thì chưa biết trình đọc AutoLISP có dịch `\U+XXXX` trong chuỗi hay không. Nhận diện Dim góc / phi vẫn có lớp so theo mã số (`member 176`, `member 966`…) nên không phụ thuộc chỗ này; riêng **`gro--extract-num-after-sym`** (tách số sau ký hiệu phi, vd `φ12` → 12) chỉ dựa vào chuỗi. Nghiệm thu trên AutoCAD: `(strlen "\U+03C6")` — ra `1` là dịch được, ra `7` thì phải đổi danh sách ký hiệu trong hàm đó sang `(chr 966)`…

- **Mục 200 — GROCHAY lỗi `consp nil` ở bước "khoi dong" trên ZWCAD máy khác** (05/11/2026, thư mục `C:\lisp\OHTA_TOOL`): chưa tái hiện tại máy phát triển. Đã gia cố `gro--tham-so-chay` và chia bước con. Nếu còn lỗi: đọc tên bước con trong câu báo lỗi, rồi trên máy đó chạy lần lượt `(gro--dem-khung-layer)`, `(gro--layer-xln-ds)`, `(gro_net_layer_ban_ve)` để xem lệnh nào hỏng.
  - **Mục 201**: lỗi nằm ở bước "khoi dong: layer chon tren tab Guro". Đã đổi: `(vl-catch-all-apply 'gro_net_layer_ban_ve (list 0))` (không gọi với `'()`), C# trả `""` thay `nil`, kiểm bằng `length`. Bài học ZWCAD (cần xác nhận cái nào là thủ phạm): tránh gọi hàm .NET với danh sách tham số rỗng, tránh `nil` trong ResultBuffer trả về, tránh `vl-list-length`.
  - **Mục 202**: vẫn lỗi ở bước đó (kể cả máy phát triển). Bọc cả bước trong `vl-catch-all-apply` (`gro--layer-tab`) — GRO chạy tiếp với layer mặc định và in câu lỗi gốc. **Việc còn lại**: lấy câu lỗi gốc đó để sửa tận gốc (layer chọn trên tab Guro chưa có tác dụng trên ZWCAD cho tới khi sửa).
