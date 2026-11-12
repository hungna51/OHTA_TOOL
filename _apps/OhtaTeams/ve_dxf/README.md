# ve_dxf — thư viện vẽ trải phẳng phòng 4 (ezdxf)

OhtaTeams (phòng 4 → **📐 Vẽ DXF**) ghi `ai\ban_ve\viec.json` rồi chạy `python ve.py viec.json`. Mỗi tấm vẽ trên **môi trường** `_apps\OhtaTeams\data\quy_chuan\4_trien_khai\moi_truong\moi-truong-ve.dxf` (layer, kiểu chữ, kiểu dim của xưởng — gitignore, chép tay) và lưu `<bango>.dxf` (+ `.png` xem trước khi có matplotlib). Sinh bản vẽ không tốn token.

- [`QUY_CHUAN_VE.md`](QUY_CHUAN_VE.md) — **quy chuẩn vẽ theo bản chuẩn xưởng** (12/11): cung thật, dim tổng `1-XY` dưới / phải, chuỗi dim dưới, dim R, ケガキ, F-name dưới tấm, Khung_IN tỉ lệ √2. Gửi kèm khi AI viết mẫu mới.
- `ve.py` — chạy: `--kiem` (python, ezdxf, matplotlib, pymupdf), `--huong-dan MOI_TRUONG DIR` (ảnh hướng dẫn `<ten>.png` cho mọi mẫu: vẽ `VI_DU`, dim ghi tên thông số x / y / r1…), `--cat-anh ANH.json` (cắt ảnh tấm từ PDF khách bằng PyMuPDF / DXF bằng ezdxf → `ai\ho_so\<đơn>-n.png`), `--thu-vien DIR…` (liệt kê mẫu cho AI chọn), `viec.json` (vẽ). Phần chung `them_chung`: **lỗ** (`ve_lo` — tròn / taro Ø mồi TMT / 皿 / lỗ dài theo `vi_tri`, nhãn `n-Ø…`, tâm vào chuỗi dim), **đủ dim khách** (`dim_khach` — thiếu ngang / dọc tự thêm hàng dim, còn lại báo `thieu_dim`), ケガキ (chữ thập `3-KEGAKI` + leader `n-ケガキ`), chuỗi dim chi tiết dưới / phải (`1-SUNPOU`), dim tổng X dưới + Y phải (`1-XY`), F-name TEXT dưới tấm (`2-F-NAME`, kiểu `Text`: N= (trống → bango), M=, T= (12.0), Q=, V= của TraV, Z=), `Khung_IN` tỉ lệ khổ A ngang. Tệp đã sửa tay trên CAD (mã băm khác lần ghi, nhớ trong `.ve_dxf.json`) không bị ghi đè → `<bango> (2).dxf`.
- `moi_truong.py` — lớp `NenVe` (mở môi trường, hàm vẽ: `vien`, `chu_nhat`, `duong` + `cung` (cung thật, `dim=True` ghi R), `duong_be` (0-KAKOU, DASHED), `slit` (4-SHT), `ke_gaki`, `leader`, `chu`, `dim_ngang`, `dim_doc`; mốc chuỗi dim `moc_x`, `moc_y`). Mẫu chỉ gọi các hàm này, không tự đặt layer / kiểu.
- `goc_khoet.py` — **vát C / bo R theo đỉnh + khoét trong**, chung mọi mẫu (như lỗ): dựng vòng viền từ nét `0-KAKOU` mẫu đã vẽ (trừ lỗ tròn, đường bẻ), đánh số đỉnh theo chiều kim đồng hồ từ đỉnh gần góc trái dưới (khoét: `k1.1`…), cắt lùi 2 cạnh rồi chèn đoạn vát / cung bo, vẽ khoét, kiểm khoét (vượt viền, đè, mép < T). `ve.py --so-do VIEC.json` vẽ ảnh sơ đồ đỉnh. Mẫu biên dạng **không** khai thông số góc nữa.
- `thu_vien/<ten>.py` — **mẫu biên dạng**, mỗi loại một tệp: `phang`, `be` (L / C / Z một hướng), `hop` (bẻ 2 hướng, cắt góc), `cuon`, `dai_lom_loi` (dải dài, đầu cung lõm r2 + đầu cung lồi r1). Mỗi mẫu có `THAM_SO` (thông số chữ cái), `VI_DU` (số ví dụ) và ảnh `<ten>.png` hướng dẫn — xem / vẽ thử ở OhtaTeams **📚 Thư viện mẫu**.

## Viết mẫu mới

```python
"""Mẫu biên dạng: ..."""
from moi_truong import LE_DIM          # chỉ được import math, moi_truong

MO_TA = "Một câu mô tả LOẠI hình — AI đọc để chọn mẫu."
THAM_SO = [("x", "Chiều dài", "tự tính"), ("y", "Chiều rộng", "tự tính"), ("r1", "Bán kính góc", "0")]  # đủ thông số, tên chữ cái
VI_DU = {"x": 300, "y": 120, "tham_so": {"r1": 15}}                                       # số ví dụ khác nhau → ảnh hướng dẫn


def ve(nen, t):
    # t: x, y, be_x, be_y (vị trí đường bẻ), doan_x, doan_y (đoạn phẳng), ke_gaki, ghi_chu, fname (ke_gaki / fname đừng vẽ — ve.py vẽ)
    nen.chu_nhat(0, 0, t["x"], t["y"])  # viền cắt phải kín, tấm đặt từ gốc (0, 0); có cung → nen.duong + nen.cung
```

Mẫu không vẽ dim tổng, chuỗi dim theo đường bẻ / ケガキ, F-name, Khung_IN — `ve.py` thêm theo [QUY_CHUAN_VE.md](QUY_CHUAN_VE.md).

AI phòng 4 chọn mẫu theo `MO_TA`; không mẫu nào khớp → báo "chưa có mẫu" + mô tả. Bấm **✨ AI đề xuất mẫu vẽ**: AI viết mẫu (kiểm an toàn: chỉ import `math` / `moi_truong`, không `open` / `os` / `subprocess` / `eval`…), lưu `ai\ban_ve\mau_thu\<ten>.py` của đơn để vẽ thử. **Chỉ máy Admin** (registry Fname `ApprovalIsAdmin` = 1 hoặc có Kev) bấm **➕ Thêm vào thư viện** → chép vào `thu_vien\` — commit để máy khác nhận.

Máy chưa có ezdxf: bấm Vẽ DXF → OhtaTeams hỏi cài (`python -m pip install --user ezdxf matplotlib`). Chưa có Python: cài Python 3 (python.org / Microsoft Store).
