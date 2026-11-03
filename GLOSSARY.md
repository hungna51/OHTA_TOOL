# OHTA TOOL

Bộ công cụ CAD (ZWCAD / AutoCAD) chuẩn bị bản vẽ gia công kim loại tấm: từ bản vẽ thiết kế đến dữ liệu cắt laser, khắc mã và in.

## Bản vẽ đầu vào

**Tấm**:
Một phôi kim loại được cắt ra thật, với một bango, một F-name và số lượng Q bản.
_Avoid_: chi tiết, phôi, plate

**Hình**:
Đường biên dạng vẽ trên CAD; thường ứng với một tấm, nhưng một cặp mirror chung một hình và một tấm gộp có thể bị chia thành nhiều tấm.
_Avoid_: tấm (khi nói về nét vẽ), biên dạng

**Cặp mirror**:
Hai tấm đối xứng 本図/逆図 vẽ chung một hình; người dùng phải xử lý xong rồi mới lấy đối xứng, chưa tự động được.
_Avoid_: tấm thuận nghịch, tấm mirror

**Ghi chú**:
Chữ người thiết kế ghi cạnh hình, chứa độ dày `t=`, số lượng, vật liệu; là nguồn dữ liệu để đánh số.
_Avoid_: note, text ghi chú

**Khung bao**:
Polyline người dùng quét chọn để giới hạn phạm vi lệnh GRO xét tới; nằm ngoài cùng.
_Avoid_: vùng, khung tấm

**Vùng**:
Polyline nằm bên trong khung bao, chỉ quyết định thứ tự đánh số, không đổi phạm vi.
_Avoid_: khung bao, zone

## Đầu ra

**Bango**:
Số thứ tự GRO đánh cho từng tấm trên bản vẽ (番号).
_Avoid_: số tấm, số thứ tự

**F-name**:
Khối các dòng `N=`, `M=`, `T=`, `Q=`, `Z=` (và dòng phụ như `V=`) trên layer `2-F-NAME`, mô tả một tấm cho khâu cắt.
_Avoid_: Fname, F-NAME, FN

**Tên tấm**:
Tên tự do của một tấm, ghi ở dòng `N=` của F-name; mặc định là `<đơn>-<bango>` nhưng người dùng đổi được.
_Avoid_: tên chi tiết, mã bản vẽ, mã tấm

**Mã khắc**:
Mã ngắn đặt bên trong hình để khắc lên tấm khi cắt.
_Avoid_: khắc chữ, mã khắc chữ

**Khung tấm**:
Hình chữ nhật bao quanh một hình cùng mọi dữ liệu của nó (ghi chú, dim, F-name, mã khắc); không dùng để in.
_Avoid_: Khung_IN, khung in

## Công cụ

**Mục con**:
Một thư mục con của OHTA TOOL, hiện thành một tab trên bảng OHTA.
_Avoid_: module, nhóm, tab (khi nói về thư mục)

**Guro**:
Mục con chứa bộ lệnh đánh số tấm; lệnh chính của nó là `GRO`.
_Avoid_: GURO, GRO (khi nói về cả bộ)
