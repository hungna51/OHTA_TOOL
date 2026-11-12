"""Mẫu biên dạng: bẻ / ấn một hướng (L, C, Z, nhiều đường bẻ song song)."""

from moi_truong import LE_DIM

MO_TA = ("Bẻ hoặc ấn 1 lần theo MỘT hướng (L, C, Z, U, nhiều đường bẻ song song): hình chữ nhật X × Y, "
         "đường bẻ DASHED chạy hết bề rộng, dim chuỗi các đoạn phẳng giữa các đường bẻ.")


def ve(nen, t):
    x, y = t["x"], t["y"]
    nen.chu_nhat(0, 0, x, y)
    for v in t.get("be_x", []):   # đường bẻ song song trục Y
        nen.duong_be((v, 0), (v, y))
    for v in t.get("be_y", []):   # đường bẻ song song trục X
        nen.duong_be((0, v), (x, v))
    # dim chuỗi đoạn phẳng (phía trên / phía phải), để người check so với tiết diện
    moc = [0.0] + list(t.get("be_x", [])) + [x]
    if len(moc) > 2:
        for a, b in zip(moc, moc[1:]):
            nen.dim_ngang(a, b, y, y + LE_DIM)
    moc = [0.0] + list(t.get("be_y", [])) + [y]
    if len(moc) > 2:
        for a, b in zip(moc, moc[1:]):
            nen.dim_doc(a, b, x, x + LE_DIM)
