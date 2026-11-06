# 01 — Luật Phân Loại Tấm Gia Công Sau Cắt Laser (Đồng Bộ Lisp CSV v5.0)

Mỗi mục `## [MÃ_NHÃN]` bên dưới định nghĩa một nhóm gia công (bám sát hàm `CSV--scan-machining-texts` và `CSV--bend-list-smart` của Lisp CSV).
- Nếu muốn **thêm nhóm gia công mới** (ví dụ `## [BARING] Dập lỗ bẻ mép`), chỉ cần thêm một khối `## [BARING]` theo cùng mẫu bên dưới.
- Nhãn dán ở góc trên-trái khung sẽ ghép theo đúng thứ tự các mục trong file này (ví dụ `[BE - TAPU]`). Nếu không khớp nhóm nào sẽ dán nhãn mặc định `[PHANG]`.

## [BE] Tấm bẻ / chấn / nhấn 1 lần / bẻ bước Pitchi (mã CSV: m, m(pitchi), 発押し)
- tu_khoa_ghi_chu: 曲げ, 折り, 曲げ角度, 発押し
- tu_khoa_fname: V=, V1=, V2=, V3=, U=, P=, R=, 発押し, シマ目内, シマ目外, 引代
- regex_ghi_chu: 
- hinh_hoc: duong_be_dashed, mtext_khung_in_m
- loai_tru_hinh_hoc_khi_co: 
- mo_ta_tieng_anh: Requires bending, press-bending (発押し), or pitch-bending (曲げ角度/V=/DASHED bend lines)
- cau_hoi_kev: Does this part require bending, press-bending, or folding (interpreted_notes mentions bending or bending_BE has V=/発押し)?

## [VAT] Tấm vát mép hàn (Chamfer / Bevel 開先, mã CSV: k)
- tu_khoa_ghi_chu: 開先施工範囲, 面取り, 開先, カイサキ, ｶｲｻｷ, レ形, K形, テーパー, 糸面
- tu_khoa_fname: K=
- regex_ghi_chu: \bC\d+(\.\d+)?\b
- hinh_hoc: mtext_khung_in_k
- loai_tru_hinh_hoc_khi_co: 
- mo_ta_tieng_anh: Requires edge chamfering or weld beveling (開先施工範囲/開先/面取り)
- cau_hoi_kev: Does this part require edge chamfering or beveling (interpreted_notes mentions chamfering/beveling)?

## [CUON] Tấm cuộn cung tròn / lốc ống tròn (Roll, mã CSV: r1, r2)
- tu_khoa_ghi_chu: R曲げ, ロール, 巻き, 円筒
- tu_khoa_fname: 
- regex_ghi_chu: 
- hinh_hoc: pichi_cuon, mtext_khung_in_r
- loai_tru_hinh_hoc_khi_co: 
- mo_ta_tieng_anh: Requires rolling/curving or multiple parallel pitch lines (ロール/R曲げ/r1/r2)
- cau_hoi_kev: Does this part require rolling or has multiple parallel pitch lines (has_multiple_parallel_pitch_lines is true or interpreted_notes mentions rolling)?

## [SARA] Khoét loe âm mũ vít (Countersink 皿もみ, mã CSV: s)
- tu_khoa_ghi_chu: 皿もみ, 皿モミ, 皿穴, 皿, サラ, ｻﾗ, CSK
- tu_khoa_fname: 
- regex_ghi_chu: 
- hinh_hoc: mtext_khung_in_s
- loai_tru_hinh_hoc_khi_co: 
- mo_ta_tieng_anh: Requires countersinking (皿もみ/皿/sara)
- cau_hoi_kev: Does this part require countersinking (interpreted_notes mentions countersinking/sara)?

## [TAPU] Taro ren lỗ (Tap タップ, mã CSV: t)
- tu_khoa_ghi_chu: タップ, ﾀｯﾌﾟ, TAP, ネジ
- tu_khoa_fname: 
- regex_ghi_chu: \bM([3-9]|1\d|2[0-4])\b
- hinh_hoc: lo_kep_dong_tam, hatch_tapu_tmt, mtext_khung_in_t
- loai_tru_hinh_hoc_khi_co: SARA
- mo_ta_tieng_anh: Requires tapping threads (タップ/concentric double circles/TAPU_TMT hatch)
- cau_hoi_kev: Does this part have tapped holes or concentric double circles (concentric_double_circles_count > 0 or interpreted_notes mentions tapping)?

<!--
Nếu muốn tách riêng nhãn [PITCHI] (bẻ bước 曲げ角度) hoặc [PHAT_OSHI] (nhấn 1 lần 発押し) trên bản vẽ,
hãy bỏ cặp thẻ chú thích ở đầu và cuối khối bên dưới:

## [PITCHI] Bẻ nhiều bước theo cung (曲げ角度 - mã CSV: m(pitchi))
- tu_khoa_ghi_chu: 曲げ角度
- tu_khoa_fname: 
- hinh_hoc: 
- mo_ta_tieng_anh: Requires multi-step pitch bending (曲げ角度 / m(pitchi))
- cau_hoi_kev: Does this part require multi-step pitch bending (曲げ角度)?

## [PHAT_OSHI] Gia công Nhấn 1 lần (内R-発押し)
- tu_khoa_ghi_chu: 発押し
- tu_khoa_fname: 発押し
- hinh_hoc: 
- mo_ta_tieng_anh: Requires single-stroke radius press bending (内R-発押し)
- cau_hoi_kev: Does this part require single-stroke press bending (発押し)?
-->
