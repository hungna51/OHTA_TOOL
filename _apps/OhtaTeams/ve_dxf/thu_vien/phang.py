"""Mẫu biên dạng: tấm phẳng chữ nhật, mỗi góc có thể vát C hoặc bo R."""

import math

MO_TA = ("Tấm phẳng, không bẻ / cuộn: hình chữ nhật X × Y; góc nào vát C (c1…c4) hoặc bo R (r1…r4) thì ghi thông số — "
         "góc 1 trái dưới, 2 phải dưới, 3 phải trên, 4 trái trên (theo hình trải phẳng, gốc trái dưới).")

# (tên, ý nghĩa, mặc định — None = bắt buộc; "tự tính" = chương trình tính)
THAM_SO = [
    ("x", "Chiều dài tấm", "tự tính"), ("y", "Chiều rộng tấm", "tự tính"),
    ("c1", "Vát C góc 1 (trái dưới)", "0"), ("c2", "Vát C góc 2 (phải dưới)", "0"),
    ("c3", "Vát C góc 3 (phải trên)", "0"), ("c4", "Vát C góc 4 (trái trên)", "0"),
    ("r1", "Bo R góc 1 (trái dưới)", "0"), ("r2", "Bo R góc 2 (phải dưới)", "0"),
    ("r3", "Bo R góc 3 (phải trên)", "0"), ("r4", "Bo R góc 4 (trái trên)", "0"),
]
VI_DU = {"x": 300, "y": 150, "tham_so": {"c1": 15, "c3": 25, "r2": 20}}


def ve(nen, t):
    """nen: moi_truong.NenVe; t: một tấm của việc vẽ (x, y, tham_so…)."""
    x, y = float(t["x"]), float(t["y"])
    ts = t.get("tham_so") or {}
    goc = [(0.0, 0.0), (x, 0.0), (x, y), (0.0, y)]          # góc 1..4, ngược chiều kim đồng hồ
    huong = [(1, 1), (-1, 1), (-1, -1), (1, -1)]             # hướng vào trong tấm từ mỗi góc
    c = [float(ts.get("c%d" % (i + 1)) or 0) for i in range(4)]
    r = [float(ts.get("r%d" % (i + 1)) or 0) for i in range(4)]
    if not any(c) and not any(r):
        nen.chu_nhat(0, 0, x, y)
        return
    for i in range(4):
        if c[i] and r[i]:
            raise ValueError("góc %d vừa vát C vừa bo R — chỉ chọn một" % (i + 1))
        if max(c[i], r[i]) * 2 > min(x, y) + 0.01:
            raise ValueError("góc %d: C / R quá lớn so với cạnh tấm" % (i + 1))
    # mỗi góc lùi a = C hoặc R dọc 2 cạnh; nối các cạnh thẳng, góc vát = đoạn chéo, góc bo = cung thật
    dau, cuoi = [], []   # điểm bắt đầu / kết thúc phần góc i (trên cạnh tới / cạnh đi)
    for i in range(4):
        a = c[i] or r[i]
        (gx, gy), (hx, hy) = goc[i], huong[i]
        truoc = goc[i - 1]                                    # cạnh tới góc i đi từ góc i-1
        vx, vy = (gx - truoc[0], gy - truoc[1])
        dai = math.hypot(vx, vy)
        p_toi = (gx - vx / dai * a, gy - vy / dai * a)       # trên cạnh tới
        sau = goc[(i + 1) % 4]
        wx, wy = (sau[0] - gx, sau[1] - gy)
        dai2 = math.hypot(wx, wy)
        p_di = (gx + wx / dai2 * a, gy + wy / dai2 * a)      # trên cạnh đi
        dau.append(p_toi)
        cuoi.append(p_di)
        if c[i]:
            nen.duong(p_toi, p_di)
            giua = ((p_toi[0] + p_di[0]) / 2, (p_toi[1] + p_di[1]) / 2)
            ra = (giua[0] - hx * 30, giua[1] - hy * 30)       # chữ ra ngoài góc
            nen.leader([giua, ra, (ra[0] - hx * 1, ra[1])], "C%g" % c[i])
        elif r[i]:
            tam = (gx + hx * a, gy + hy * a)
            g0 = math.degrees(math.atan2(p_toi[1] - tam[1], p_toi[0] - tam[0]))
            g1 = math.degrees(math.atan2(p_di[1] - tam[1], p_di[0] - tam[0]))
            nen.cung(tam, a, g0, g1, dim=True)
    for i in range(4):
        nen.duong(cuoi[i], dau[(i + 1) % 4])                  # cạnh thẳng giữa góc i và góc i+1
