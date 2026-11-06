# 03 — Luật Đối Chiếu Ghi Chú Bản Vẽ, Hình Học & Thông Tin Tấm CSV (Q / GHI_CHU / KHUNG_IN)

Quy định đối chiếu giữa chữ ghi chú của người thiết kế, hình học gia công (`0-KAKOU`, `3-KEGAKI`, `1-XY`, `TAPU_TMT`) và khối MText thông tin tấm trên layer `KHUNG_IN` của Lisp Xuất CSV (`xuat_csv_v5.0.1.lsp`).

## 1. Cấu hình kiểm tra Số lượng (Q), Độ dày (t=) & Chuẩn Lisp CSV
- kiem_tra_so_luong_q: true
- don_vi_so_luong: 枚, 個, ケ, 本
- tu_khoa_so_luong_mo_ho: 台, 本図+逆図
- kiem_tra_do_day_t: true
- kiem_tra_doi_tuong_la_0_kakou: true
- kiem_tra_be_hon_hop_phat_oshi: true
- kiem_tra_mtext_khung_in: true
- kiem_tra_dim_tong_1_xy: auto
- kiem_tra_hatch_tapu_tmt: auto

## 2. Giải thích các kiểm tra tự động theo chuẩn Lisp CSV
1. **Đối tượng lạ trên layer `0-KAKOU` (`kiem_tra_doi_tuong_la_0_kakou`):**
   - Layer `0-KAKOU` chỉ được chứa nét cắt (`Continuous`) và đường bẻ (`DASHED*`). Nếu có `TEXT`, `MTEXT`, `DIMENSION` hoặc `HATCH` nằm nhầm trên `0-KAKOU` -> báo lỗi đỏ `🔴 KHAC` (tương ứng lỗi `invalid-entity` Stage 0a của Lisp CSV).
2. **Tấm bẻ hỗn hợp `V=` + `発押し` (`kiem_tra_be_hon_hop_phat_oshi` — `09-press-detection.md`):**
   - Khi F-name vừa có `発押し` vừa có `V=` hoặc `シマ目` (chế độ `'mixed`), Lisp CSV quét hộp nghiêng `±50mm` quanh từng nhóm đường bẻ để phân loại đường nào nhấn 1 lần (`count × 2`), đường nào bẻ thường (`count × 1`).
   - Nếu có nhóm đường bẻ không tìm thấy chữ chú thích ký hiệu bẻ gần đó -> báo vàng `🟡 GHI_CHU`. Nếu 1 đoạn đường bẻ đơn có cả 2 loại chú thích mâu thuẫn -> báo đỏ `🔴 GHI_CHU`.
3. **Kiểm tra khối MText thông tin tấm trên layer `KHUNG_IN` (`kiem_tra_mtext_khung_in` — Bước 1 sang Bước 2 của lệnh `CSV`):**
   - Khi khung đã chạy Bước 1 (*Xuất thông tin tấm*) và có MText trên layer `KHUNG_IN`:
     - Tổng số dòng phải bằng `12 + số loại đường bẻ` (nếu có gia công `m`). Sai số dòng (do xuất đè 2 lần hoặc MText bản cũ) -> báo đỏ `🔴 KHAC`.
     - Có gia công vát (`k` hoặc ghi chú `開先施工範囲`) nhưng `Chiều dài vát: 0` -> báo đỏ `🔴 GHI_CHU` (*chưa nhập chiều dài vát*).
     - Có gia công bẻ bước `m(pitchi)` (hoặc ghi chú `曲げ角度`) nhưng `Pitchi - Số lần: 0` hoặc `Pitchi - Chiều dài (mm): 0` -> báo đỏ `🔴 GHI_CHU`.
     - Tấm có gia công cuộn (`CUON`) nhưng dòng `Các loại gia công:` chưa điền `r1` (cuộn cung tròn 1) hoặc `r2` (cuộn ống tròn) -> báo vàng `🟡 GHI_CHU`.
     - Tấm có nhiều hơn **8 loại đường bẻ** -> báo vàng `🟡 KHAC` (file CSV chỉ có 8 cặp cột `Bẻ1..Bẻ8`).
4. **Kiểm tra Dim tổng `1-XY` và Hatch `TAPU_TMT` thông minh (`auto`):**
   - Khi bản vẽ đã sử dụng layer `1-XY` (hoặc đã có MText `KHUNG_IN`): khung nào thiếu dim tổng trên `1-XY` hoặc có số dim `1-XY` khác 1 và 2 -> báo đỏ `🔴 KHAC`.
   - Khi bản vẽ đã sử dụng layer `TAPU_TMT`: khung nào có gia công Tapu (`タップ`) hoặc Sara (`皿もみ`) mà chưa có `HATCH` trên `TAPU_TMT` -> báo vàng `🟡 GHI_CHU`.

## 3. Các luật ràng buộc Ghi chú <-> F-name (Có thể tự thêm mục `## [LUAT: ...]` mới)

## [LUAT: BE_THIEU_V] Ghi chú có yêu cầu bẻ nhưng F-name thiếu dòng V= / 発押し
- muc_do: do
- loai_loi: GHI_CHU
- khi_ghi_chu_co: 曲げ, 折り
- bat_buoc_fname_co: V=, V1=, V2=, V3=, U=, P=, R=, 発押し, シマ目内, シマ目外
- ly_do: Ghi chú có yêu cầu bẻ ('曲げ'/'折り') nhưng F-name thiếu dòng thông số bẻ (V= / 発押し / シマ目).
- goi_y: Điền thông số bẻ vào dòng V= hoặc 発押し

<!--
Mẫu thêm luật mới (bỏ dấu chú thích để kích hoạt):
## [LUAT: BAO_VE_BE_MAT] Ghi chú yêu cầu dán nilon bảo vệ
- muc_do: vang
- loai_loi: GHI_CHU
- khi_ghi_chu_co: 保護シート, ビニール
- bat_buoc_fname_co: 保護シート, ビニール
- ly_do: Ghi chú có yêu cầu bảo vệ bề mặt (保護シート) nhưng F-name chưa ghi chú.
- goi_y: Kiểm tra yêu cầu bề mặt
-->
