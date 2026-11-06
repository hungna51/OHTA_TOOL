# 00 — File Chỉ Dẫn Chính Của AI Kev (Master Index)

Đây là **file gốc (`00-index.md`)** mà **Kev-0.8B** đọc đầu tiên trước mỗi lần kiểm tra & phân loại bản vẽ.
Từ file này, hệ thống tự động lần theo các đường dẫn `.md` bên dưới để nạp luật phân loại tấm gia công và luật kiểm lỗi.
Khi chạy chế độ **Hybrid** hoặc **Chỉ Claude**, nội dung các file này cũng được đồng bộ sang cho Claude.
Khi các máy con trong mạng LAN kết nối tới Máy chủ Kev (`may_chu_lan`), máy con sẽ **tự động đồng bộ toàn bộ bộ file `.md` mới nhất từ Máy chủ về** trước khi kiểm tra.

## 1. Thông số chung của Kev
- may_chu_lan: http://NGUYEN-A-HUNG:8008
- nguong_phan_loai: 0.50
- nguong_nghi_loi: 0.50
- nhan_mac_dinh: PHANG
- so_duong_kegaki_cuon: 6

## 2. Danh sách các file hướng dẫn con (Kev đọc theo thứ tự)
1. [01-phan-loai-gia-cong.md](01-phan-loai-gia-cong.md) — Định nghĩa các loại tấm gia công (`BE`, `VAT`, `CUON`, `SARA`, `TAPU`...) để dán nhãn góc khung.
2. [02-luat-fname-vat-lieu.md](02-luat-fname-vat-lieu.md) — Luật kiểm tra cấu trúc F-name (`N, M, T, Q, Z`), đối chiếu bảng vật liệu chuẩn và tấm nhám `SIMA`.
3. [03-luat-ghi-chu-ban-ve.md](03-luat-ghi-chu-ban-ve.md) — Luật đối chiếu ghi chú tiếng Nhật trên bản vẽ với F-name (số lượng `Q`, độ dày `t=`, yêu cầu gia công).

*(Mẹo: Nếu muốn tách thêm file luật mới, ví dụ `04-luat-rieng-xuong.md`, chỉ cần tạo file trong thư mục `05_AI\data\kev\` trên máy chủ và thêm 1 dòng liên kết `[04-luat-rieng-xuong.md](04-luat-rieng-xuong.md)` vào danh sách trên — các máy con trong mạng LAN sẽ tự tải về).*
