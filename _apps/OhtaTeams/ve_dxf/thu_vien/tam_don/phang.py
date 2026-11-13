"""Mẫu biên dạng: tấm phẳng chữ nhật."""

MO_TA = ("Tấm phẳng, không bẻ / cuộn: hình chữ nhật X × Y. Vát / bo góc ghi ở ô Góc (theo đỉnh), khoét trong ở ô Khoét — "
         "phần chung mọi mẫu, không phải thông số mẫu.")

# (tên, ý nghĩa, mặc định — None = bắt buộc; "tự tính" = chương trình tính)
THAM_SO = [("x", "Chiều dài tấm", "tự tính"), ("y", "Chiều rộng tấm", "tự tính")]
VI_DU = {"x": 300, "y": 150, "tham_so": {}}


def ve(nen, t):
    """nen: moi_truong.NenVe; t: một tấm của việc vẽ (x, y, tham_so…)."""
    nen.chu_nhat(0, 0, float(t["x"]), float(t["y"]))
