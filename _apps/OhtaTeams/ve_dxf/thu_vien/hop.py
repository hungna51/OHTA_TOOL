"""Mẫu biên dạng: hộp 3–4 cạnh (bẻ cả 2 hướng)."""

MO_TA = ("Hộp / khay bẻ cả 2 hướng (3–4 cạnh): hình chữ thập — chữ nhật X × Y cắt 4 góc theo đoạn phẳng đầu / cuối "
         "mỗi hướng, đường bẻ DASHED chỉ trong phần đáy, dim chuỗi 2 hướng (phía dưới / phải). Liên kết góc hở / kín, khe R chưa vẽ.")

# (tên, ý nghĩa, mặc định — None = bắt buộc; "tự tính" = chương trình tính)
THAM_SO = [("x", "Chiều dài trải phẳng (thành – đáy – thành theo X)", "tự tính"),
           ("y", "Chiều rộng trải phẳng (thành – đáy – thành theo Y)", "tự tính")]
VI_DU = {"x": 260, "y": 200, "be_x": [40, 220], "be_y": [40, 160], "doan_x": [40, 180, 40], "doan_y": [40, 120, 40]}


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
    # dim chuỗi theo đường bẻ: ve.py tự thêm (dưới / phải) từ be_x, be_y
