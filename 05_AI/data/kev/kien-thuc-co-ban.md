# Kiến thức cơ bản để đọc file mô tả bản vẽ (`<bản vẽ>.ai.md` / `.ai.json`)

File này được chép ra cạnh mỗi file mô tả do nút **📝 Xuất mô tả** (lệnh `OHTAAIMOTA`) của tab AI tạo ra.
Bản gốc nằm ở `05_AI\data\kev\kien-thuc-co-ban.md` (không có trong `00-index.md` nên Kev không nạp nó làm luật kiểm tra).

## Nghề
Bản vẽ là bản vẽ **gia công kim loại tấm**: cắt laser rồi tuỳ tấm mà bẻ, vát, cuộn, khoét, taro.
- **Tấm**: một phôi kim loại được cắt ra thật, có một F-name và số lượng Q.
- **Khung in** (polyline kín 4 đỉnh trên layer `Khung_IN`): mỗi khung in chứa **đúng 1 tấm** cùng mọi chữ, kích thước, ghi chú của nó.
- **Ghi chú**: chữ người thiết kế ghi cạnh hình, thường là tiếng Nhật (độ dày `t=`, số lượng, yêu cầu gia công).

## F-name (layer `2-F-NAME`): thông tin chính của tấm
| Dòng | Nghĩa |
|---|---|
| `N=` | Tên tấm |
| `M=` | Vật liệu (SS400, SPHC, SUS304…) |
| `T=` | Độ dày (mm) |
| `Q=` | Số lượng tấm cần cắt |
| `Z=` | Mã đơn / mã phụ |
| `V=`, `U=`, `P=`, `R=` | Đường bẻ (BE): góc, bán kính, bước |

## Layer
| Layer | Nghĩa |
|---|---|
| `0-KAKOU` | Nét cắt laser: chu vi tấm và lỗ. Nét `DASHED` = đường bẻ |
| `3-KEGAKI` | Nét vạch dấu (không cắt): đường bẻ, đường chia cuộn |
| `1-SUNPOU` | Kích thước (DIM) và chữ chú thích |
| `2-F-NAME` | F-name |
| `TAPU_TMT` | Lỗ taro ren (đã tô màu bằng lệnh TMT) |
| `Khung_IN` | Khung in, mỗi khung = 1 tấm |
| `AI-PHAN-LOAI` | Nhãn AI dán trước đó. Không phải dữ liệu bản vẽ, không được xuất |

## Loại gia công
Xem chi tiết ở [01-phan-loai-gia-cong.md](01-phan-loai-gia-cong.md): `BE` (bẻ), `VAT` (vát mép), `CUON` (cuộn), `SARA` (khoét loe mũ vít), `TAPU` (taro ren), `PHANG` (tấm phẳng, chỉ cắt).
Luật F-name/vật liệu: [02-luat-fname-vat-lieu.md](02-luat-fname-vat-lieu.md). Luật ghi chú tiếng Nhật: [03-luat-ghi-chu-ban-ve.md](03-luat-ghi-chu-ban-ve.md).

## Cách đọc file mô tả
- `.ai.md`: mỗi tấm một mục `## Tam n`, gồm kích thước khung, F-name đã tách, chữ/ghi chú/DIM, số đối tượng theo layer. Không có toạ độ.
- `.ai.json`: cùng dữ liệu, thêm `doi_tuong[]`. Mỗi phần tử có `loai`, `layer`, `hop` = [minX, minY, maxX, maxY], và `chi_tiet` (dòng toạ độ đầy đủ):
  - `L layer x1,y1 x2,y2 [net=DASHED]`: đoạn thẳng
  - `C layer cx,cy d=đường_kính`: lỗ tròn
  - `A layer cx,cy r=bán_kính gócĐầu->gócCuối`: cung
  - `P layer kin|ho x,y ... [~cx,cy r=R]`: polyline (`~` = đoạn cung)
  - `T layer x,y h=cao "chữ"`: chữ
  - `D layer loại do=giá_trị [ghide="..."]`: kích thước
  - `H layer x,y`: hatch
- Đơn vị: mm. Trục Y hướng lên.
