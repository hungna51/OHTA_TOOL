"""Mẫu biên dạng: bẻ / ấn một hướng (L, C, Z, nhiều đường bẻ song song)."""

MO_TA = ("Bẻ hoặc ấn 1 lần theo MỘT hướng (L, C, Z, U, nhiều đường bẻ song song): hình chữ nhật X × Y, "
         "đường bẻ DASHED chạy hết bề rộng, dim chuỗi các đoạn phẳng (phía dưới / phải).")

# (tên, ý nghĩa, mặc định — None = bắt buộc; "tự tính" = chương trình tính)
THAM_SO = [("x", "Chiều dài trải phẳng (đường bẻ song song trục Y tính sẵn từ tiết diện)", "tự tính"),
           ("y", "Chiều rộng trải phẳng", "tự tính")]
VI_DU = {"x": 300, "y": 120, "be_x": [70, 230]}


def ve(nen, t):
    x, y = t["x"], t["y"]
    nen.chu_nhat(0, 0, x, y)
    for v in t.get("be_x", []):   # đường bẻ song song trục Y
        nen.duong_be((v, 0), (v, y))
    for v in t.get("be_y", []):   # đường bẻ song song trục X
        nen.duong_be((0, v), (x, v))
    # dim chuỗi theo đường bẻ: ve.py tự thêm (dưới / phải) từ be_x, be_y
