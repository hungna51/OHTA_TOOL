# ve_dxf — thư viện vẽ trải phẳng phòng 4 (ezdxf)

OhtaTeams (phòng 4 → **📐 Vẽ DXF**) ghi `ai\ban_ve\viec.json` rồi chạy `python ve.py viec.json`. Mỗi tấm vẽ trên **môi trường** `_apps\OhtaTeams\data\quy_chuan\4_trien_khai\moi_truong\moi-truong-ve.dxf` (layer, kiểu chữ, kiểu dim của xưởng — gitignore, chép tay) và lưu `<bango>.dxf` (+ `.png` xem trước khi có matplotlib). Sinh bản vẽ không tốn token.

- `ve.py` — chạy: `--kiem` (python, ezdxf, matplotlib), `--thu-vien DIR…` (liệt kê mẫu cho AI chọn), `viec.json` (vẽ). Thêm dim tổng X / Y (`1-SUNPOU`, `ISO-26`), F-name TEXT (`2-F-NAME`, kiểu `Text`: N=, M=, T=, Q=, V= của TraV, Z=), `Khung_IN`. Tệp đã sửa tay trên CAD (mã băm khác lần ghi, nhớ trong `.ve_dxf.json`) không bị ghi đè → `<bango> (2).dxf`.
- `moi_truong.py` — lớp `NenVe` (mở môi trường, hàm vẽ: `vien`, `chu_nhat`, `duong_be` (0-KAKOU, DASHED), `slit` (4-SHT), `chu`, `dim_ngang`, `dim_doc`). Mẫu chỉ gọi các hàm này, không tự đặt layer / kiểu.
- `thu_vien/<ten>.py` — **mẫu biên dạng**, mỗi loại một tệp: `phang`, `be` (L / C / Z một hướng), `hop` (bẻ 2 hướng, cắt góc), `cuon`.

## Viết mẫu mới

```python
"""Mẫu biên dạng: ..."""
from moi_truong import LE_DIM          # chỉ được import math, moi_truong

MO_TA = "Một câu mô tả LOẠI hình — AI phòng 4 đọc để chọn mẫu."


def ve(nen, t):
    # t: x, y, be_x, be_y (vị trí đường bẻ), doan_x, doan_y (đoạn phẳng), ghi_chu, fname (đừng vẽ — ve.py vẽ)
    nen.chu_nhat(0, 0, t["x"], t["y"])  # viền cắt phải kín, tấm đặt từ gốc (0, 0)
```

AI phòng 4 chọn mẫu theo `MO_TA`; không mẫu nào khớp → báo "chưa có mẫu" + mô tả. Bấm **✨ AI đề xuất mẫu vẽ**: AI viết mẫu (kiểm an toàn: chỉ import `math` / `moi_truong`, không `open` / `os` / `subprocess` / `eval`…), lưu `ai\ban_ve\mau_thu\<ten>.py` của đơn để vẽ thử. **Chỉ máy Admin** (registry Fname `ApprovalIsAdmin` = 1 hoặc có Kev) bấm **➕ Thêm vào thư viện** → chép vào `thu_vien\` — commit để máy khác nhận.

Máy chưa có ezdxf: bấm Vẽ DXF → OhtaTeams hỏi cài (`python -m pip install --user ezdxf matplotlib`). Chưa có Python: cài Python 3 (python.org / Microsoft Store).
