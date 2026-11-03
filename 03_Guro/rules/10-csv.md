# 10. Ghi file CSV

> Một phần tài liệu GURO. Mục lục: [00-muc-luc-guro.md](00-muc-luc-guro.md)
>
> **Số mục (1-33) giữ nguyên như bản gộp cũ** — mã nguồn `.lsp` và các file
> rule khác tham chiếu tới chúng, đổi số là hỏng 59 chỗ.

## Các lỗi đã phát hiện và sửa

7. **CSV xuất ra giờ là UTF-8 kèm BOM** thay vì ghi thẳng theo codepage hệ thống. `(open ...)`/
   `(write-line ...)` chuẩn của AutoLISP không có cách chọn encoding khi ghi, nên `c:bg` không
   còn ghi từng dòng trực tiếp vào `filecsv` nữa (biến này chỉ dùng để kiểm tra quyền ghi rồi
   đóng ngay) — các dòng CSV được gom vào list `csv-line` trong lúc lặp, rồi cuối hàm gọi
   `guro--csv-append-utf8bom` (dùng COM `ADODB.Stream`) đọc lại nội dung cũ (nếu có, tự nhận
   diện BOM UTF-8 hay codepage hệ thống qua Charset `"_autodetect"`) và ghi đè lại toàn bộ file
   với Charset `"utf-8"` (tự kèm BOM khi `SaveToFile`). Nhờ vậy dữ liệu tiếng Nhật/Việt trong
   cột `don` (lấy từ tên file DWG) cũng được chuyển đổi đúng, không chỉ riêng phần ASCII.

8. **Charset `"_autodetect"` phá hỏng DÒNG ĐẦU TIÊN của CSV mỗi lần chạy `bg`** — lỗi phát
   sinh từ chính mục 7 ở trên. Máy này có `ACP = 932`, mà `_autodetect` **không** nhận ra BOM
   UTF-8: nó giải mã y hệt `shift_jis`, biến 3 byte BOM (`EF BB BF`) thành 2 ký tự rác
   `U+30FB U+FF7F`. Vì `guro--csv-append-utf8bom` đọc lại rồi **ghi đè toàn bộ** file, đống
   rác đó trở thành nội dung thật và dồn thêm sau mỗi lần chạy → lệnh `FN` đọc sai cột `N`
   của tấm đầu tiên. Đã thêm `guro--has-utf8-bom` (so 3 mã byte đầu qua `vl-string->list`)
   để chọn Charset `"utf-8"` cho file đã có BOM, chỉ giữ `"_autodetect"` cho file cũ ghi bằng
   `write-line` theo codepage hệ thống.

   Đã đo thực tế bằng COM `ADODB.Stream` trên chính máy này:

   | Charset | 5 ký tự đầu đọc từ file có BOM |
   |---|---|
   | `_autodetect` | `U+30FB U+FF7F` `A B -` ← rác |
   | `shift_jis` | `U+30FB U+FF7F` `A B -` (giống hệt) |
   | `utf-8` | `A B - 1 ,` ✅ |

   > **File CSV đã sinh ra trước bản sửa này vẫn còn rác ở dòng 1** — bản sửa không tự dọn
   > được vì đống rác giờ đã là nội dung thật. Phải xoá file CSV rồi chạy lại `bg`, hoặc mở
   > bằng editor và xoá phần rác ở đầu dòng đầu tiên.

27. **Báo lỗi ghi CSV: dùng `alert`, và phân biệt hai nguyên nhân.** Người dùng gặp
   `Khong the mo/tao file CSV tai: … - kiem tra quyen ghi thu muc.` in ra dòng lệnh, không
   hiểu vì sao. Nguyên nhân thật là **Excel đang mở file CSV đó** — Excel giữ khoá độc quyền.

   Hai sửa đổi (chỉ ở `gro`):

   1. **Phân biệt hai nguyên nhân bằng `findfile`** — chúng khác hẳn nhau và cách xử lý
      cũng khác:

      | Điều kiện | Nguyên nhân | Bảo người dùng làm gì |
      |---|---|---|
      | file **có** tồn tại mà `(open … "a")` trả `nil` | đang bị **khoá** (Excel) | đóng file CSV lại |
      | file **không** tồn tại và tạo không được | không có quyền ghi thư mục | kiểm tra thư mục |

   2. **Dùng `alert` chứ không chỉ `princ`.** Thông báo ở dòng lệnh rất dễ bị bỏ qua, mà
      lệnh thì dừng hẳn tại đó — người dùng tưởng lệnh không chạy.

   **Bịt luôn lỗ hổng cùng loại ở khâu ghi thật.** Phép thử quyền ghi ở đầu lệnh dùng
   `(open … "a")`, còn lúc ghi thật lại là COM `SaveToFile` — **hai cơ chế khác nhau**, và
   giữa hai thời điểm đó người dùng hoàn toàn có thể vừa mở file bằng Excel. Nếu không bắt
   lỗi thì **entity đã tạo rồi nhưng CSV không có dòng nào** — bản vẽ và CSV lệch nhau trong
   im lặng, đúng loại lỗi tệ nhất. Nay bọc `vl-catch-all-apply`, hỏng thì `alert` rõ là
   "đã tạo entity nhưng CSV ghi thất bại — đóng file, UNDO, chạy lại", và dòng tổng kết ghi
   `*** GHI THAT BAI ***` thay cho đường dẫn.

   > `vl-catch-all-apply` bắt được lỗi COM ở đây vì `guro--csv-append-utf8bom` chắc chắn
   > tồn tại. Nó **không** bắt được lỗi `undefined function` (mục 3) — đó là chuyện khác.

   **`bg` vẫn giữ thông báo cũ** (chỉ `princ`, không phân biệt nguyên nhân, không bắt lỗi ở
   khâu ghi thật).

30. **`gro` chuyển từ NỐI TIẾP sang GHI ĐÈ file CSV** (người dùng yêu cầu). File CSV giờ
   luôn là kết quả của **đúng lần chạy cuối cùng**, không còn tích luỹ qua các lần chạy.

   Cài bằng hàm riêng `gro--csv-ghi-de`, **không sửa `guro--csv-append-utf8bom`** vì hàm đó
   dùng chung với `bg` — `bg` vẫn nối tiếp như cũ.

   Điểm đáng nhớ về cài đặt: `SaveToFile … 2` (`adSaveCreateOverWrite`) **vốn đã là ghi đè**.
   Thứ tạo ra hành vi "nối tiếp" ở bản cũ là **bước đọc lại nội dung cũ rồi ghép vào trước**.
   Bỏ đúng bước đó đi là xong — và kéo theo một lợi ích lớn: **cả lớp lỗi ở mục 7 và 8
   (dò BOM UTF-8, Charset `"_autodetect"` làm hỏng dòng đầu tiên) không còn áp dụng cho
   `gro`**, vì nó không bao giờ đọc lại file cũ nữa.

   **Đổi lại là rủi ro mất dữ liệu, phải nói rõ với người dùng:** chạy `gro` lần hai xoá sạch
   kết quả lần một khỏi CSV. Muốn đánh số làm nhiều đợt thì phải chọn hết trong một lần chạy,
   hoặc tự sao lưu CSV giữa các đợt. Nên báo cáo in `… dong CSV (GHI DE)` kèm **số dòng cũ đã
   xoá** (đếm trước khi ghi bằng `gro--csv-dem-dong`) — để người dùng thấy ngay chuyện gì vừa
   xảy ra thay vì phát hiện muộn.

   > Phép thử quyền ghi ở đầu lệnh **vẫn phải dùng `(open … "a")`**, tuyệt đối không đổi sang
   > `"w"`: chế độ `"w"` **cắt trắng file ngay tại bước thử**, tức là phá dữ liệu trước cả khi
   > biết lệnh có chạy trót lọt hay không.

31. **Bỏ ô "Số bắt đầu" khỏi hộp thoại `gro`** — hệ quả trực tiếp của mục 30. Khi đã ghi đè
   CSV và đánh số cả bản vẽ trong một lần chạy thì **không còn khái niệm "đánh số tiếp"**:
   `bango` luôn bắt đầu từ 1. Giữ ô đó lại chỉ tạo cơ hội gõ nhầm làm **CSV lệch với bản vẽ**
   mà không có gì phát hiện được.

   Kéo theo: **`bango` chuyển từ biến toàn cục sang biến LOCAL của `c:gro`.** Trước kia nó
   toàn cục và **dùng chung với `bg`** để nhớ số tiếp theo giữa các lần chạy — nay `gro`
   không được đụng vào trạng thái của `bg` nữa. Đây là loại dọn dẹp dễ quên: bỏ một ô trong
   hộp thoại mà không bỏ cái trạng thái toàn cục đứng sau nó thì `gro` vẫn âm thầm ghi đè
   biến của `bg`.

   Ba chỗ phải sửa theo, dễ sót:
   - `grodbg` đang đặt và đọc ô `start_num` **không còn tồn tại trong DCL** → đổi sang kiểm
     `mk_code` / `use_vung` / `layer_vung`;
   - `grosort` đoán số bango từ biến toàn cục `bango` → đổi thành bắt đầu từ 1;
   - dòng tổng kết `"So tiep theo: N"` không còn nghĩa → đổi thành `"Da danh so tu 1 den N"`.
