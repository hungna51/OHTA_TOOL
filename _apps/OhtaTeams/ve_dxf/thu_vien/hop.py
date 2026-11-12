"""Mẫu biên dạng: hộp 3–4 cạnh (bẻ cả 2 hướng)."""

from moi_truong import LE_DIM

MO_TA = ("Hộp / khay bẻ cả 2 hướng (3–4 cạnh): hình chữ thập — chữ nhật X × Y cắt 4 góc theo đoạn phẳng đầu / cuối "
         "mỗi hướng, đường bẻ DASHED chỉ trong phần đáy, dim chuỗi 2 hướng. Liên kết góc hở / kín, khe R chưa vẽ.")


def ve(nen, t):
    x, y = t["x"], t["y"]
    dx, dy = t.get("doan_x", []), t.get("doan_y", [])
    gx0, gx1 = (dx[0], dx[-1]) if len(dx) > 1 else (0.0, 0.0)
    gy0, gy1 = (dy[0], dy[-1]) if len(dy) > 1 else (0.0, 0.0)
    # viền chữ thập, đi ngược chiều kim đồng hồ từ góc dưới-trái của đáy
    nen.vien([
        (gx0, 0), (x - gx1, 0), (x - gx1, gy0), (x, gy0), (x, y - gy1), (x - gx1, y - gy1),
        (x - gx1, y), (gx0, y), (gx0, y - gy1), (0, y - gy1), (0, gy0), (gx0, gy0),
    ])
    for v in t.get("be_x", []):
        nen.duong_be((v, gy0), (v, y - gy1))
    for v in t.get("be_y", []):
        nen.duong_be((gx0, v), (x - gx1, v))
    moc = [0.0] + list(t.get("be_x", [])) + [x]
    for a, b in zip(moc, moc[1:]):
        nen.dim_ngang(a, b, y, y + LE_DIM)
    moc = [0.0] + list(t.get("be_y", [])) + [y]
    for a, b in zip(moc, moc[1:]):
        nen.dim_doc(a, b, x, x + LE_DIM)
