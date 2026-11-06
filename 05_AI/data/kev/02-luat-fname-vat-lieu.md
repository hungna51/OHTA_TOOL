# 02 — Luật Kiểm Tra F-name, Layer 2-F-NAME & Bảng Vật Liệu (MTZ / CSV)

Quy định kiểm tra cấu trúc khối chữ F-name trên layer `2-F-NAME`, đối chiếu với bảng vật liệu chuẩn (`01_Fname\data\catalog.csv`) và các điều kiện bắt buộc của Lisp Xuất CSV (`xuat_csv_v5.0.1.lsp`).

## 1. Cấu hình kiểm tra F-name & Vật liệu
- kiem_tra_du_5_truong: true
- kiem_tra_bang_vat_lieu: true
- kiem_tra_sima: true
- dong_bat_buoc_sima: 裏から見た図
- hau_to_z_sima: -SIMA
- tu_khoa_m_sima: 縞板
- kiem_tra_fname_hon_hop_text_mtext: true
- kiem_tra_trung_ten_n: true
- kiem_tra_vat_lieu_cpl: true

## 2. Giải thích quy tắc áp dụng
1. **Đủ 5 trường bắt buộc (`N=`, `M=`, `T=`, `Q=`, `Z=`):**
   - Thứ tự dòng chuẩn: `N=`, `M=`, `T=` (luôn có ít nhất 1 số lẻ: `6.0`), `Q=`, các dòng bẻ `V=`, `Z=`, `裏から見た図` (nếu là tấm nhám SIMA), và **dòng nhấn 1 lần `内R...-発押し...°` luôn đứng cuối cùng dưới cùng**.
   - Thiếu bất kỳ trường nào hoặc giá trị rỗng -> báo lỗi đỏ `🔴 KHAC` (tương ứng `F-name error(1)` / `Lỗi Fname (2)` trong Lisp CSV).
2. **Không trộn lẫn `TEXT` và `MTEXT` trên layer `2-F-NAME` (`kiem_tra_fname_hon_hop_text_mtext`):**
   - Theo hàm `CSV--detect-fname-type` của Lisp CSV, mỗi khung in chỉ được chứa **1 kiểu** F-name (hoặc 1 khối `MTEXT` duy nhất, hoặc cụm `TEXT` rời) và chỉ có **1 cụm `N=`**. Nếu trộn cả `TEXT` và `MTEXT` hoặc có `> 1` cụm F-name -> báo lỗi đỏ `🔴 KHAC`.
3. **Không trùng lặp tên tấm `N=` giữa các khung (`kiem_tra_trung_ten_n`):**
   - Theo hàm `has-duplicates` của Lisp CSV, nếu hai khung trở lên có cùng tên `N=` thì lệnh `CSV` sẽ dừng hẳn và báo lỗi -> báo lỗi đỏ `🔴 KHAC`.
4. **Đối chiếu Bảng vật liệu (`M, Loai, Z, T_list`):**
   - `M=` có trong bảng chuẩn nhưng `Z=` khác cột `Z` chuẩn (ví dụ `M=SS400` nhưng `Z=SPHC` thay vì `Z=SS400`) -> báo lỗi đỏ `🔴 MTZ` và gợi ý `Z=` đúng.
   - `T=` không nằm trong danh sách `T_list` của `M=` -> báo lỗi đỏ `🔴 MTZ` và gợi ý các độ dày chuẩn.
   - `M=` chỉ nằm trong danh sách chờ duyệt hoặc chưa có trong bảng -> báo vàng `🟡 MTZ`.
5. **Vật liệu nhám (`SIMA`) và tấm chống trượt `CPL`:**
   - Khi `M=` chứa `縞板` hoặc `Z=` kết thúc bằng `-SIMA`, bên dưới `Z=` bắt buộc phải có dòng `裏から見た図`. Nếu thiếu -> báo vàng `🟡 KHAC`.
   - Khi ghi chú trên bản vẽ có `CPL` hoặc `裏面CPL` (thép tấm vân chống trượt), vật liệu chuẩn phải đổi thành `M=ムキ 縞板` và `Z=SS400-SIMA`.
