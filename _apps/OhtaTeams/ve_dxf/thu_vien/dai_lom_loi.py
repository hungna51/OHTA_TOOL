"""Mẫu biên dạng: dải dài, một đầu khoét cung bán nguyệt lõm, một đầu bo cung bán nguyệt lồi (Admin duyệt 12/11, vẽ lại theo bản chuẩn xưởng)."""

from moi_truong import LE_DIM

MO_TA = (
    "Tấm phẳng dải dài có hai cạnh dài song song, một đầu khoét cung bán nguyệt lõm vào trong "
    "và đầu kia bo tròn lồi nửa đường tròn tiếp tuyến với hai cạnh dài."
)


def ve(nen, t):
    x, y = float(t["x"]), float(t["y"])
    r = y / 2.0
    xt = max(x - r, 0.0)            # tâm cung lồi = mút phải của 2 cạnh thẳng
    nen.duong((0.0, y), (xt, y))
    nen.duong((xt, 0.0), (0.0, 0.0))
    nen.cung((xt, r), r, 270, 90, dim=True)   # đầu phải lồi: dim R + tâm vào chuỗi dim
    nen.cung((0.0, r), r, 270, 90)            # đầu trái lõm (cùng R, không dim lại)
    # đầu lõm: dim tới tâm cung (nửa chiều cao) bên trái, như bản chuẩn
    nen.dim_doc(0.0, r, 0.0, -LE_DIM, vi_tri_chu=(-LE_DIM, -33.0))      # chữ dưới
    nen.dim_doc(r, y, 0.0, -LE_DIM, vi_tri_chu=(-LE_DIM - 11.0, y + 27.0))  # chữ trên
    for v in t.get("be_x", []):
        nen.duong_be((v, 0.0), (v, y))
