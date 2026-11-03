# Tab chung tự sửa module.json từ giao diện (nút ＋, nhóm, cài đặt tab)

Từ 11/2026, giao diện tab chung (`ModuleTabControl` — mọi tab trừ Fname) cho người dùng thêm lisp bằng nút ＋, chia nút lệnh thành nhóm và đổi màu/tiêu đề tab bằng nút ⚙. Mọi thay đổi ghi thẳng vào `module.json` của tab — vẫn là nguồn dữ liệu duy nhất. Mỗi máy có bản OHTA_TOOL riêng, nên lệnh thêm bằng nút ＋ chỉ có trên máy đó; muốn cả xưởng dùng thì đưa vào gói rồi phát hành. Tự cập nhật giữ nguyên phần máy tự thêm ([ADR 0004](0004-tu-dong-cap-nhat-tu-github.md)). File lisp được chép vào `lisp/` của tab, ảnh nút vào `icons/`. Bỏ hai nút MD và Rules (người dùng xưởng không cần), nút 📁 chuyển vào cửa sổ cài đặt.

`module.json` có thêm:

- `groups` (cấp tab): thứ tự các nhóm. Tên nhóm vẫn là trường `category` có sẵn của từng nút; nút không có `category` vào nhóm "Khác" ở cuối.
- `icon` (cấp nút): đường dẫn tương đối tới ảnh `.png`/`.ico`; không có thì hiện ô chữ `badge` như cũ.
- `locked` (cấp nút): nút gốc do lập trình viên khai báo (GRO, GI, DST) — giao diện không cho xoá.

Ghi file an toàn: đọc lại file mới nhất ngay trước khi ghi, chỉ chèn phần thay đổi, giữ nguyên các trường lạ, ghi qua file tạm rồi thay, giữ `module.json.bak`. Trạng thái thu gọn nhóm là của từng máy, lưu ở `%APPDATA%\OHTA_TOOL\`, không ghi vào `module.json`.

## Considered Options

- **Lưu lệnh thêm vào file riêng của từng máy (kiểu `FnameLocalAdditions`)**: không đụng `module.json`. Không chọn — lúc đó tưởng mọi máy dùng chung thư mục OneDrive; nay mỗi máy một bản nên lý do "máy khác không thấy" không còn, nhưng gộp `module.json` khi cập nhật (ADR 0004) đã giữ được phần máy tự thêm.
- **Chỉ ghi đường dẫn tới file lisp gốc, không chép**: máy khác không có file đó. Không chọn.
- **Khoá quyền sửa bằng mật khẩu**: xưởng nhỏ, cờ `locked` và file `.bak` đủ đỡ lỗi. Không chọn.
