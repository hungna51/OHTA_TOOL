"""Mẫu biên dạng: tấm phẳng."""

MO_TA = "Tấm phẳng, không bẻ / cuộn: hình chữ nhật X × Y."


def ve(nen, t):
    """nen: moi_truong.NenVe; t: một tấm của việc vẽ (x, y, be_x, be_y, doan_x, doan_y, ghi_chu…)."""
    nen.chu_nhat(0, 0, t["x"], t["y"])
