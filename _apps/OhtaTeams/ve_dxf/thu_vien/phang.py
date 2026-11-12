"""Mẫu biên dạng: tấm phẳng."""

MO_TA = "Tấm phẳng, không bẻ / cuộn: hình chữ nhật X × Y."

# (tên, ý nghĩa, mặc định — None = bắt buộc; "tự tính" = chương trình tính)
THAM_SO = [("x", "Chiều dài tấm", "tự tính"), ("y", "Chiều rộng tấm", "tự tính")]
VI_DU = {"x": 300, "y": 150}


def ve(nen, t):
    """nen: moi_truong.NenVe; t: một tấm của việc vẽ (x, y, be_x, be_y, doan_x, doan_y, ghi_chu…)."""
    nen.chu_nhat(0, 0, t["x"], t["y"])
