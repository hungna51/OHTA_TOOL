# Tab AI — AI kiểm tra bản vẽ (thiết kế)

Ngày: 2026-11-04 · Trạng thái: chờ duyệt · Thuật ngữ theo [GLOSSARY.md](../../GLOSSARY.md)

## 1. Ý định và lý do

Lỗi hay lọt ra xưởng nhất là **F-name lệch với thực tế**: người dùng đổi vật liệu `M=` nhưng quên sửa `Z=` (mã phôi) hoặc `T=` (độ dày); `Q=` lệch số lượng trong ghi chú; ghi chú tiếng Nhật có yêu cầu đặc biệt (逆, タップ, 曲げ…) mà F-name không phản ánh. Mắt người dễ bỏ sót khi bản vẽ có hàng chục tấm.

Tab **AI** là chỗ **thử nghiệm** cho việc dùng Claude (Anthropic API) đọc dữ liệu trong từng khung và chỉ ra chỗ nghi sai. Tính năng **độc lập với GRO**: không cần đã chạy GRO, không dùng bango, cài đặt hay mã nguồn của GRO — chỉ đơn giản là kiểm dữ liệu nằm trong khung. Mục tiêu trước mắt là **xem AI làm được tới đâu** — nên bản đầu để AI làm hết, cái gì AI làm ổn định sẽ chuyển dần sang luật cứng (code thường, không tốn tiền, không cần mạng) ở các bản sau.

## 2. Mục tiêu / Không làm / Thất bại nếu

**Mục tiêu**
- Bấm một nút, nhận danh sách cảnh báo theo từng khung; bấm cảnh báo → zoom tới khung.
- Kiểm 3 nhóm lỗi: (a) `M=`/`T=`/`Z=` không khớp bảng vật liệu chuẩn; (b) `Q=` không khớp số lượng trong ghi chú; (c) ghi chú tiếng Nhật có yêu cầu mà F-name không phản ánh.
- Chỉnh được "lời dặn" cho AI ngay trên tab, không cần build lại; mỗi lần kiểm lưu nhật ký để so sánh.

**Không làm (bản này)**
- Không kiểm `V=` (bẻ) so với đường bẻ, không kiểm lỗ taro — để sau.
- Không gửi ảnh bản vẽ (chậm, tốn token).
- Không gắn với GRO: không chạy tự động ở cuối GRO, không đọc bango / cài đặt / registry GRO, không gọi GROTOOL.dll, không xử lý riêng cặp mirror hay tấm gộp. Khung nào kiểm khung đó với đúng những gì nằm trong nó.
- Không đưa tab AI vào bản phát hành cho đội.

**Thất bại nếu (dù chạy được)**
- AI tự sửa bản vẽ hoặc CSV. **AI chỉ cảnh báo, người dùng tự sửa.**
- Lỗi trong tab AI làm hỏng bảng OHTA hoặc các tab khác.
- API key lọt lên git / GitHub (repo là public).
- Cảnh báo nhiều nhiễu tới mức người dùng bỏ qua — vì vậy phải phân mức 🔴/🟡 và có lọc.

## 3. Ràng buộc

- Chạy trên **ZWCAD 2026 (net48)** và **AutoCAD 2027 (net10)**, theo chuẩn biên dịch kép của [rules/02](../../rules/02-da-nen-tang-zwcad-autocad.md).
- Mọi thao tác CAD Database từ giao diện palette modeless bọc `doc.LockDocument()`.
- Repo public: key không lên git, không vào zip phát hành.
- Thư mục gốc không có tiền tố `_` sẽ hiện thành tab ([ADR 0002](../adr/0002-tab-chung-them-lisp-va-nhom.md)); tài liệu này nằm ở `_docs/` vì thế.
- Máy không có mạng: báo rõ "không kết nối được AI", không treo CAD.

## 4. Những điều người dùng đã quyết

| # | Quyết định |
|---|---|
| 1 | Tab riêng tên **AI**, mục con mới `04_AI/`. |
| 2 | Code nằm trong **DLL riêng `AITOOL.dll`** (`04_AI/plugin/AiTool.Plugin`, ra `04_AI/bin/<ZwCAD|AutoCAD>/AITOOL.dll`), OHTATOOL.dll nạp bằng phản chiếu theo cùng cách nạp như tab Guro ([ADR 0003](../adr/0003-tab-guro-nam-trong-grotool.md)). Lỗi tab AI không kéo sập OHTA. |
| 3 | **Chỉ máy người phát triển** thấy tab AI trong lúc thử nghiệm; không có trong zip phát hành. |
| 4 | Đơn vị kiểm là **khung**: polyline kín 4 đỉnh trên một layer (mặc định `Khung_IN`). Mỗi khung ứng với một tấm (biên dạng + F-name + ghi chú…). Chỉ xét đối tượng **bên trong khung**; phần ngoài khung bỏ qua. Không phụ thuộc GRO. |
| 5 | Phạm vi: mặc định **mọi khung trong bản vẽ**; có nút **Chọn vùng** để kiểm vài khung. |
| 6 | Được gửi lên AI **mọi dữ liệu trong khung**, kể cả tên khách và hình học. |
| 7 | Hình học gửi dưới dạng **danh sách đối tượng dạng chữ**, không gửi ảnh. Toạ độ tính lại theo **góc dưới trái khung = (0,0)**, làm tròn **2 chữ số thập phân**. Đối tượng thuộc khung khi **tâm** của nó nằm trong khung. |
| 8 | Thuộc tính gửi theo loại: Line (đầu, cuối, layer) · Arc/Circle (tâm, bán kính, góc; lỗ tròn ghi đường kính) · Polyline (đỉnh; đoạn cong thành cung) · Text/MText (nội dung, vị trí, chiều cao, layer) · Dim (giá trị đo, giá trị ghi đè nếu có, loại, điểm đo) · F-name (tách sẵn `N, M, T, Q, Z, V…` + các dòng phụ). Mọi đối tượng kèm layer. |
| 9 | Gửi kèm **cả bảng vật liệu** trong phần đầu cố định để được **prompt caching** (5 phút; dữ liệu cố định chỉ trả ~10% giá ở các lô sau). Không cần Files API (vẫn tính token mỗi lần). |
| 10 | Gửi theo **lô khoảng 10 khung** một lần, các lô chạy song song, có thanh tiến độ. |
| 11 | Mỗi cảnh báo: **khung nào** (tên tấm `N=` nếu có, kèm số thứ tự khung), **mức độ** (🔴 chắc chắn sai / 🟡 nghi ngờ), **lý do tiếng Việt**, **giá trị gợi ý** (vd. "Z nên là SUS-N2"). Khung ổn thì không hiện. Có lọc "chỉ hiện 🔴". Bấm cảnh báo → zoom tới khung. |
| 12 | AI **không bao giờ** sửa bản vẽ hay CSV. |
| 13 | Claude API qua HTTPS; **key** trong file riêng `04_AI/data/ai.key`, thêm vào `.gitignore`. Mặc định model **Sonnet** (`claude-sonnet-5-5`), có ô chọn model trên tab. Mỗi lần kiểm hiện số token và tiền ước tính. |
| 14 | **Lời dặn** (system prompt) lưu ở file text `04_AI/data/loi_dan.md`, sửa được trên tab, có nút **Khôi phục mặc định**. Bản mặc định do người phát triển soạn từ luật đã có: bảng M→T/Z, quy tắc SIMA (`裏から見た図` dưới `Z=`), thứ tự dòng F-name, nghĩa các từ tiếng Nhật hay gặp trong ghi chú. |
| 15 | **Nhật ký** mỗi lần kiểm ghi ra `04_AI/log/` (không lên git): dữ liệu gửi, câu trả lời, token, tiền, model, thời điểm. |
| 16 | Hướng dẫn tạo API key Anthropic (console.anthropic.com, nạp tiền, chép key vào `ai.key`) viết vào `04_AI/README.md` **sau khi code xong**. |

### Quyết định của người viết tài liệu (người dùng xem lại)

- **Layer khung chọn trên tab**, mặc định `Khung_IN`, nhớ lựa chọn lần trước (registry riêng của tab AI, không dùng registry GRO).
- **Khung chồng nhau**: mỗi khung độc lập — đối tượng có tâm nằm trong nhiều khung thì được gửi cùng mọi khung đó.
- **Bảng vật liệu** = bảng thật mà tab Fname đang dùng trên máy: bản cache đồng bộ từ Google Sheet **cộng** phần máy tự thêm đang chờ duyệt (`%LOCALAPPDATA%\OHTA_TOOL\fname_data\catalog_local.csv`), phần chờ duyệt được đánh dấu rõ. Vật liệu chỉ có trong phần chờ duyệt → AI chỉ báo 🟡, không báo 🔴. Không có cache thì dùng `01_Fname/data/catalog.csv` trong thư mục cài đặt.
- **Không thấy khung nào** trên layer đã chọn → báo "Không thấy khung trên layer `<tên>`" và dừng.

## 5. Luồng chạy (tham khảo cho người xây)

1. Người dùng chọn layer khung, bấm **Kiểm tra** (hoặc **Chọn vùng** rồi quét).
2. C# (trong khoá document) tìm khung → với mỗi khung gom đối tượng có tâm trong khung, đổi toạ độ, tách F-name → đóng thành dữ liệu chữ.
3. Chia lô ~10 khung; mỗi lô một request: `system` = lời dặn + bảng vật liệu (đánh dấu cache), `user` = dữ liệu các khung. Yêu cầu AI trả **JSON có cấu trúc** (structured output) — danh sách cảnh báo `{khung, muc_do, loai_loi, ly_do, goi_y}`.
4. Thu kết quả các lô (song song, không khoá CAD trong lúc chờ mạng), hiện danh sách, cộng token/tiền, ghi nhật ký.
5. Bấm một cảnh báo → zoom tới khung đó.

## 6. Để người xây tự quyết

- Định dạng chữ cụ thể của dữ liệu khung (JSON hay dạng dòng gọn) — miễn đủ thuộc tính ở mục 4.8 và ít token.
- Cách nhận diện F-name trong khung (text/mtext layer `2-F-NAME`; viết trong `Core/` thuần C# có unit test, có thể chép logic tách dòng từ tab Fname — không gọi chéo DLL).
- Gọi API bằng SDK chính thức `Anthropic` cho C# nếu chạy được trên cả net48 và net10; không thì HttpClient thẳng (OHTA đã có tiền lệ `GitHubReleaseClient`). Ghi lý do chọn.
- Số lô song song tối đa, timeout, thử lại khi lỗi 429/5xx.
- Bố cục giao diện tab (theo phong cách tab Guro/Fname: nền trắng, nút ⚙ màu tab).
- Cách loại `04_AI` khỏi zip phát hành (vd. thêm điều kiện trong `release_ohta.ps1`) và khỏi build mặc định nếu cần; ghi vào ADR mới `0005`.
- Unit test cho phần thuần C#: gom đối tượng theo khung, đổi toạ độ, tách F-name, chia lô, đọc JSON trả về.

## 7. Câu hỏi còn mở

Không còn. Reviewer đã đọc thử; câu hỏi về cặp mirror / bản vẽ không có khung GRO bị loại vì tính năng không gắn với GRO.
