"""Mẫu biên dạng: cuộn (lốc tròn / cung)."""

MO_TA = ("Cuộn / lốc (ống tròn, nửa ống, cung): hình chữ nhật X × Y (chiều trải theo đường trung bình 0.5 × T thực), "
         "không vẽ đường bẻ; ghi chú cuộn trên 1-SUNPOU.")


def ve(nen, t):
    x, y = t["x"], t["y"]
    nen.chu_nhat(0, 0, x, y)
    dong = [s for s in t.get("ghi_chu", []) if "cuộn" in s] or ["ロール"]
    for i, s in enumerate(dong):
        nen.chu(10, y / 2 - i * 25, s)
