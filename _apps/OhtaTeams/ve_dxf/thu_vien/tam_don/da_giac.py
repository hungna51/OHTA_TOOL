"""Mẫu biên dạng: đa giác chung cho MỌI tấm phẳng không phải chữ nhật (25/11) — AI ghi đỉnh viền, không cần mẫu riêng từng hình."""

import math

MO_TA = ("Tấm phẳng (không bẻ / cuộn) có viền ngoài bất kỳ gồm cạnh thẳng và cạnh cung: chữ T, L, U, hình thang, tam giác cụt, "
         "dải có đầu bo / khoét cung, tấm có tai… — dùng cho MỌI tấm phẳng không phải hình chữ nhật (chữ nhật dùng 4canh). "
         "Không có thông số mẫu: ghi đỉnh viền vào ô dinh [[x, y] hoặc [x, y, R]…] theo thứ tự đi vòng, gốc (0, 0) = góc trái dưới khung bao; "
         "R ở đỉnh i = cạnh từ đỉnh i tới đỉnh kế là cung bán kính |R| (R > 0 lồi ra ngoài, R < 0 lõm vào trong). "
         "Vát C / bo R tại góc vẫn ghi ở ô goc, lỗ ở lo, khoét trong ở khoet — không ghi vào đỉnh.")

THAM_SO = [("x", "Chiều dài khung bao", "tự tính"), ("y", "Chiều cao khung bao", "tự tính")]
VI_DU = {"x": 276, "y": 292, "tham_so": {},
         "dinh": [[0, 0], [276, 0], [276, 200], [156, 200], [156, 252, 40], [76, 252], [76, 200], [0, 200]]}
SAI_SO = 0.05


def doc_dinh(ds):
    """[[x, y] | [x, y, R]…] → [(x, y, R)]; bỏ đỉnh trùng liên tiếp và đỉnh cuối lặp đỉnh đầu. Sai dạng → ValueError."""
    if not isinstance(ds, list) or len(ds) < 3:
        raise ValueError("chưa có đỉnh viền (ô dinh cần ≥ 3 đỉnh [x, y]) — nhập ở ✎ Sửa thông số")
    kq = []
    for i, p in enumerate(ds):
        if not isinstance(p, (list, tuple)) or len(p) < 2:
            raise ValueError("đỉnh %d sai dạng: %r (cần [x, y] hoặc [x, y, R])" % (i + 1, p))
        q = (float(p[0]), float(p[1]), float(p[2]) if len(p) > 2 and p[2] not in (None, "") else 0.0)
        if not kq or math.hypot(q[0] - kq[-1][0], q[1] - kq[-1][1]) > SAI_SO:
            kq.append(q)
    if len(kq) > 1 and math.hypot(kq[0][0] - kq[-1][0], kq[0][1] - kq[-1][1]) <= SAI_SO:
        kq.pop()
    if len(kq) < 3:
        raise ValueError("ô dinh chỉ còn %d đỉnh khác nhau — cần ≥ 3" % len(kq))
    return kq


def _cat(a, b, c, d):
    """Hai đoạn ab, cd cắt nhau tại điểm trong (không tính chạm mút)."""
    def ccw(p, q, r):
        return (q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0])
    d1, d2, d3, d4 = ccw(c, d, a), ccw(c, d, b), ccw(a, b, c), ccw(a, b, d)
    return d1 * d2 < -1e-9 and d3 * d4 < -1e-9


def tu_cat(pts):
    """Cặp cạnh (số đỉnh đầu, 1-based) cắt nhau — thứ tự đỉnh sai; None = viền đơn."""
    n = len(pts)
    for i in range(n):
        for j in range(i + 2, n):
            if i == 0 and j == n - 1:
                continue
            if _cat(pts[i], pts[(i + 1) % n], pts[j], pts[(j + 1) % n]):
                return i + 1, j + 1
    return None


def tam_cung(a, b, r, ngoai_ben_phai):
    """Cạnh cung a → b bán kính |r|: → (tâm, góc đầu, góc cuối) cho cung ngược chiều kim đồng hồ.
    ngoai_ben_phai: phía ngoài hình nằm bên phải hướng a → b (đa giác ngược chiều kim đồng hồ). r > 0 lồi ra ngoài, r < 0 lõm vào."""
    dx, dy = b[0] - a[0], b[1] - a[1]
    c = math.hypot(dx, dy)
    R = abs(r)
    if R < c / 2.0 - SAI_SO:
        raise ValueError("cung R%g ngắn hơn nửa dây cung %g (đỉnh (%g, %g) → (%g, %g))" % (R, c / 2.0, a[0], a[1], b[0], b[1]))
    h = math.sqrt(max(R * R - c * c / 4.0, 0.0))
    nx, ny = -dy / c, dx / c                       # pháp tuyến bên trái a → b
    phong_phai = (r > 0) == ngoai_ben_phai         # cung phồng sang phải a → b
    s = 1.0 if phong_phai else -1.0                # phồng phải → tâm bên trái (cung nhỏ)
    tam = ((a[0] + b[0]) / 2.0 + s * nx * h, (a[1] + b[1]) / 2.0 + s * ny * h)
    ga = math.degrees(math.atan2(a[1] - tam[1], a[0] - tam[0]))
    gb = math.degrees(math.atan2(b[1] - tam[1], b[0] - tam[0]))
    return (tam, ga, gb) if phong_phai else (tam, gb, ga)   # tâm bên trái: a → b ngược chiều kim đồng hồ


def ve(nen, t):
    import moi_truong as mt
    pts = doc_dinh(t.get("dinh"))
    x0, y0 = min(p[0] for p in pts), min(p[1] for p in pts)
    if abs(x0) > SAI_SO or abs(y0) > SAI_SO:
        raise ValueError("đỉnh phải tính từ góc trái dưới khung bao: x nhỏ nhất %g, y nhỏ nhất %g (cần 0)" % (x0, y0))
    cat = tu_cat(pts)
    if cat:
        raise ValueError("viền tự cắt: cạnh từ đỉnh %d cắt cạnh từ đỉnh %d — thứ tự đỉnh sai" % cat)
    dien_tich = sum(pts[i - 1][0] * pts[i][1] - pts[i][0] * pts[i - 1][1] for i in range(len(pts))) / 2.0
    ngoai_ben_phai = dien_tich > 0                 # đi ngược chiều kim đồng hồ: trong hình bên trái
    n = len(pts)
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        if abs(a[2]) > SAI_SO:
            tam, g0, g1 = tam_cung(a, b, a[2], ngoai_ben_phai)
            nen.cung(tam, abs(a[2]), g0, g1)
        else:
            nen.duong((a[0], a[1]), (b[0], b[1]))
    bx0, by0, bx1, by1 = mt.khung_bao(nen)
    x, y = float(t.get("x") or 0), float(t.get("y") or 0)
    if bx0 < -SAI_SO or by0 < -SAI_SO:
        raise ValueError("cung lồi vượt ra ngoài gốc (0, 0): khung bao bắt đầu (%g, %g) — đỉnh tính lại từ góc trái dưới của cả cung" % (bx0, by0))
    if x > 0 and y > 0 and (abs(bx1 - x) > SAI_SO or abs(by1 - y) > SAI_SO):
        raise ValueError("đỉnh không khớp kích thước %g × %g: viền vẽ ra %g × %g — kiểm ô dinh / kich_thuoc" % (x, y, bx1, by1))
    for v in t.get("be_x", []):
        nen.duong_be((v, 0.0), (v, by1))


# ---------- tự kiểm ----------
assert doc_dinh([[0, 0], [10, 0], [10, 0], [10, 5], [0, 0]]) == [(0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 5.0, 0.0)]
assert tu_cat([(0, 0, 0), (10, 10, 0), (10, 0, 0), (0, 10, 0)]) is not None          # hình nơ: thứ tự đỉnh sai
assert tu_cat([(0, 0, 0), (10, 0, 0), (10, 10, 0), (0, 10, 0)]) is None
_tam, _g0, _g1 = tam_cung((156, 252), (76, 252), 40, True)   # tai chữ T bo bán nguyệt R40 (đa giác ngược chiều kim đồng hồ, cạnh đi sang trái)
assert abs(_tam[0] - 116) < 1e-9 and abs(_tam[1] - 252) < 1e-9 and round(_g0) == 0 and round(_g1) == 180   # lồi lên trên
_tam, _g0, _g1 = tam_cung((156, 252), (76, 252), -40, True)  # cùng cạnh, lõm xuống
assert round(_g0) == 180 and round(_g1) % 360 == 0                                   # quét 180° → 360°: nửa dưới
_tam, _g0, _g1 = tam_cung((156, 252), (76, 252), 50, True)   # R50 > nửa dây 40: cung nhỏ, tâm dưới dây 30
assert abs(_tam[1] - 222) < 1e-9 and round(_g0, 2) == 36.87 and round(_g1, 2) == 143.13
