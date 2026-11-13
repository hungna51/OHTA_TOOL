"""Mẫu biên dạng: dải dài, đầu trái khoét cung lõm, đầu phải bo cung lồi (Admin duyệt 12/11, vẽ theo bản chuẩn xưởng)."""

import math
from moi_truong import LE_DIM

MO_TA = (
    "Tấm phẳng dải dài có hai cạnh dài song song, một đầu khoét cung lõm vào trong "
    "và đầu kia bo cung lồi nối hai cạnh dài (cung có thể là bán nguyệt hoặc cung lớn hơn)."
)

# (tên, ý nghĩa, mặc định — None = bắt buộc; "tự tính" = chương trình tính)
THAM_SO = [
    ("x", "Chiều dài trải phẳng (mút cung lồi → mép trái)", "tự tính"),
    ("y", "Chiều rộng dải", "tự tính"),
    ("r1", "Bán kính cung lồi đầu phải (≥ y/2)", "y/2"),
    ("r2", "Bán kính cung lõm đầu trái (≥ y/2)", "y/2"),
]
VI_DU = {"x": 330, "y": 60, "tham_so": {"r1": 45, "r2": 38}}


def ve(nen, t):
    x, y = float(t["x"]), float(t["y"])
    h = y / 2.0
    ts = t.get("tham_so") or {}
    r1, r2 = float(ts.get("r1") or h), float(ts.get("r2") or h)
    if r1 < h - 0.01 or r2 < h - 0.01:
        raise ValueError("r1, r2 phải ≥ y/2 (%g)" % h)
    d1 = math.sqrt(max(r1 * r1 - h * h, 0.0))   # tâm cung lồi lùi vào so với mút cạnh thẳng
    d2 = math.sqrt(max(r2 * r2 - h * h, 0.0))   # tâm cung lõm nằm ngoài mép trái
    cx = x - r1                                # tâm cung lồi
    xt = cx + d1                               # mút phải 2 cạnh thẳng
    g1, g2 = math.degrees(math.atan2(h, d1)), math.degrees(math.atan2(h, d2))
    nen.duong((0.0, y), (xt, y))
    nen.duong((xt, 0.0), (0.0, 0.0))
    nen.cung((cx, h), r1, -g1, g1, dim=True)        # đầu phải lồi: dim R + tâm vào chuỗi dim
    nen.cung((-d2, h), r2, -g2, g2, dim=abs(r2 - r1) > 0.01)  # đầu trái lõm: cùng R thì không dim lại
    # đầu lõm: dim tới tâm cung (nửa chiều cao) bên trái, như bản chuẩn
    nen.dim_doc(0.0, h, 0.0, -LE_DIM, vi_tri_chu=(-LE_DIM, -33.0))      # chữ dưới
    nen.dim_doc(h, y, 0.0, -LE_DIM, vi_tri_chu=(-LE_DIM - 11.0, y + 27.0))  # chữ trên
    for v in t.get("be_x", []):
        nen.duong_be((v, 0.0), (v, y))
