"""Chân dim phải CHẠM đối tượng nó đo (quy tắc 25/11) — phần chung mọi mẫu.

Đối tượng = "điểm đặc trưng" của hình đã vẽ: đỉnh / mút đoạn viền và đường bẻ, cạnh thẳng đứng / ngang trùng đường gióng,
điểm cực (trái / phải / trên / dưới) của cung, tâm lỗ tròn, tâm dấu ケガキ. Một dim ngang đo tại x = v thì chân dim là điểm
đặc trưng có x = v (lệch ≤ SAI_SO) gần đường dim nhất; không có điểm nào → không vẽ dim đó (báo thiếu), không để chân lơ lửng.
"""
import math

SAI_SO = 0.05   # mm


# ---------- hình: danh sách phần tử thuần (không phụ thuộc ezdxf) ----------
#   ("doan", (x1, y1), (x2, y2))         viền / đường bẻ / slit
#   ("cung", (cx, cy), r, goc_dau, goc_cuoi)   độ, ngược chiều kim đồng hồ
#   ("diem", (x, y))                      tâm lỗ, ケガキ

def _trong_quet(g, a0, a1):
    """Góc g (độ) nằm trên cung a0 → a1 ngược chiều kim đồng hồ."""
    q = (a1 - a0) % 360 or 360
    return (g - a0) % 360 <= q + 1e-9


def khoang(hinh, truc, v):
    """Đường gióng qua toạ độ v (truc "x": đường đứng x = v → trả các khoảng y; "y": đường ngang y = v → khoảng x)
    chạm điểm đặc trưng nào. → [(lo, hi)] (điểm: lo = hi; cạnh trùng đường gióng: cả đoạn)."""
    i = 0 if truc == "x" else 1   # toạ độ dọc đường gióng là j
    j = 1 - i
    kq = []
    for p in hinh:
        if p[0] == "diem":
            if abs(p[1][i] - v) <= SAI_SO:
                kq.append((p[1][j], p[1][j]))
        elif p[0] == "doan":
            a, b = p[1], p[2]
            if abs(a[i] - v) <= SAI_SO and abs(b[i] - v) <= SAI_SO:          # cạnh trùng đường gióng
                kq.append((min(a[j], b[j]), max(a[j], b[j])))
            else:
                for q in (a, b):                                              # đỉnh / mút
                    if abs(q[i] - v) <= SAI_SO:
                        kq.append((q[j], q[j]))
        elif p[0] == "cung":
            (cx, cy), r, a0, a1 = p[1], p[2], p[3], p[4]   # tâm cung không phải đối tượng cắt — dim tới tâm cần dấu tâm (diem)
            for goc in (0.0, 90.0, 180.0, 270.0):                             # điểm cực của cung
                if _trong_quet(goc, a0, a1):
                    q = (cx + r * math.cos(math.radians(goc)), cy + r * math.sin(math.radians(goc)))
                    if abs(q[i] - v) <= SAI_SO:
                        kq.append((q[j], q[j]))
            for goc in (a0, a1):                                              # hai mút cung
                q = (cx + r * math.cos(math.radians(goc)), cy + r * math.sin(math.radians(goc)))
                if abs(q[i] - v) <= SAI_SO:
                    kq.append((q[j], q[j]))
    return kq


def chan(hinh, truc, v, duong):
    """Toạ độ chân dim trên đường gióng qua v, gần đường dim (toạ độ duong) nhất; None = không chạm đối tượng nào."""
    tot = None
    for lo, hi in khoang(hinh, truc, v):
        c = min(max(duong, lo), hi)
        if tot is None or abs(c - duong) < abs(tot - duong):
            tot = c
    return tot


def _kc_doan(p, a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]
    l2 = dx * dx + dy * dy
    t = 0.0 if l2 <= 1e-12 else max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / l2))
    return math.hypot(p[0] - a[0] - t * dx, p[1] - a[1] - t * dy)


def tren_hinh(hinh, p):
    """Điểm p (chân dim xiên / góc) nằm trên đối tượng nào đó (đỉnh, cạnh, cung, tâm lỗ) trong SAI_SO."""
    for q in hinh:
        if q[0] == "diem" and math.hypot(p[0] - q[1][0], p[1] - q[1][1]) <= SAI_SO:
            return True
        if q[0] == "doan" and _kc_doan(p, q[1], q[2]) <= SAI_SO:
            return True
        if q[0] == "cung":
            (cx, cy), r = q[1], q[2]
            if abs(math.hypot(p[0] - cx, p[1] - cy) - r) <= SAI_SO and _trong_quet(math.degrees(math.atan2(p[1] - cy, p[0] - cx)), q[3], q[4]):
                return True
    return False


def hinh_tu_nen(nen):
    """Điểm đặc trưng của bản vẽ: nét 0-KAKOU (viền, đường bẻ — polyline tách đoạn / cung), slit 4-SHT, tâm lỗ CIRCLE, tâm ケガキ."""
    import moi_truong as mt
    kq = []

    def them(e):
        t = e.dxftype()
        if t == "LINE":
            kq.append(("doan", (e.dxf.start.x, e.dxf.start.y), (e.dxf.end.x, e.dxf.end.y)))
        elif t == "ARC":
            kq.append(("cung", (e.dxf.center.x, e.dxf.center.y), float(e.dxf.radius), float(e.dxf.start_angle), float(e.dxf.end_angle)))
        elif t == "CIRCLE":
            kq.append(("diem", (e.dxf.center.x, e.dxf.center.y)))
        elif t in ("LWPOLYLINE", "POLYLINE"):
            for v in e.virtual_entities():
                them(v)

    for e in nen.msp.query('*[layer=="%s" | layer=="%s"]' % (mt.LOP_CAT, mt.LOP_SLIT)):
        them(e)
    # ケガキ: NenVe nhớ tâm (+ đỉnh tam giác); bản vẽ đọc lại từ tệp thì chữ thập 2 nét → tâm = trung điểm nét, tam giác → đỉnh
    if hasattr(nen, "diem_kegaki"):
        kq += [("diem", p) for p in nen.diem_kegaki]
    else:
        canh = set()
        for e in nen.msp.query('LINE[layer=="%s"]' % mt.LOP_KEGAKI):
            a, b = (round(e.dxf.start.x, 3), round(e.dxf.start.y, 3)), (round(e.dxf.end.x, 3), round(e.dxf.end.y, 3))
            kq.append(("diem", ((a[0] + b[0]) / 2.0, (a[1] + b[1]) / 2.0)))
            kq.append(("diem", a))
            canh |= {(a, b), (b, a)}
        for a, b in canh:   # 3 nét khép kín = tam giác → trọng tâm
            for c in {q for p, q in canh if p == b and q != a}:
                if (c, a) in canh:
                    kq.append(("diem", ((a[0] + b[0] + c[0]) / 3.0, (a[1] + b[1] + c[1]) / 3.0)))
    return kq


# ---------- tự kiểm ----------
_T = [("doan", (0, 0), (276, 0)), ("doan", (276, 0), (276, 200)), ("doan", (276, 200), (156, 200)), ("doan", (156, 200), (156, 292)),
      ("doan", (156, 292), (76, 292)), ("doan", (76, 292), (76, 200)), ("doan", (76, 200), (0, 200)), ("doan", (0, 200), (0, 0)),
      ("diem", (99, 267)), ("diem", (99, 25))]
assert chan(_T, "x", 76, 320) == 292             # mép tai chữ T dim ở trên: chân ở đỉnh tai (76, 292), không phải y = 200
assert chan(_T, "x", 76, -40) == 200             # dim ở dưới: chân gần nhất là đỉnh (76, 200)
assert chan(_T, "x", 99, 320) == 267             # tâm lỗ ở tai
assert chan(_T, "x", 99, -40) == 25              # tâm lỗ dưới
assert chan(_T, "x", 120, 320) is None           # x = 120 không có đỉnh / tâm nào → không vẽ (không chân lơ lửng)
assert chan(_T, "x", 76.04, 320) == 292          # lệch 0.04 ≤ 0.05: vẫn chạm
assert chan(_T, "x", 76.06, 320) is None         # lệch 0.06 > 0.05
assert chan(_T, "y", 200, 300) == 276            # dim dọc bên phải tới cao 200: đỉnh (276, 200)
assert chan(_T, "x", 0, 320) == 200              # cạnh trái trùng đường gióng: chân ở đầu cạnh gần dim nhất
_C = [("cung", (100, 50), 50, -90, 90)]          # nửa cung phải tâm (100, 50)
assert chan(_C, "x", 150, -40) == 50             # điểm cực phải của cung
assert chan(_C, "x", 100, -40) == 0              # mút dưới cung
assert tren_hinh(_T, (156, 246)) and tren_hinh(_C, (135.36, 85.36)) and not tren_hinh(_T, (120, 120))
