"""Góc vát / bo theo đỉnh + khoét trong (12/11) — phần chung MỌI mẫu biên dạng, như lỗ.

Viền: các đối tượng 0-KAKOU mẫu đã vẽ (trừ CIRCLE lỗ và đường bẻ nét DASHED) nối thành vòng kín; vòng ngoài = vòng có hộp
bao lớn nhất. Mỗi vòng đi theo chiều kim đồng hồ, đỉnh (chỗ đổi hướng > 1°) đánh số 1, 2… bắt đầu từ đỉnh gần góc trái dưới
của hộp bao — viền ngoài "1".."n", khoét thứ N "kN.1".."kN.m" (khoét đánh số theo thứ tự nhập, mỗi vị trí @ là một khoét).

t["goc"]  = [{"dinh": "1" | "trai_duoi" | "120,40" | "k1.2", "kieu": "C" | "R", "gia_tri": số}]
t["khoet"] = [{"kieu": "chu_nhat", "rong", "cao", "goc_xoay", "r", "vi_tri": [[x, y]]}       (vi_tri = tâm)
            | {"kieu": "da_giac", "dinh": [[x, y]…], "vi_tri": [[x, y]]}]                   (đỉnh so với điểm đặt)
"""
import math
import re

import moi_truong as mt

DUNG_SAI = 0.5      # mm: toạ độ / tên góc lệch đỉnh quá mức này → báo không thấy đỉnh
NOI = 0.02          # mm: hai đầu mút coi là nối nhau
GAY = math.radians(1.0)
TEN_GOC = {"trai_duoi": (0, 0), "phai_duoi": (1, 0), "phai_tren": (1, 1), "trai_tren": (0, 1)}
DINH_K = re.compile(r"^k(\d+)\.(\d+)$")


# ---------- đoạn: thẳng L (a → b) hoặc cung A (a → b quanh c, ccw = ngược chiều kim đồng hồ) ----------

def _L(a, b, src=None):
    return {"k": "L", "a": tuple(a), "b": tuple(b), "src": src}


def _A(a, b, c, r, ccw, src=None):
    return {"k": "A", "a": tuple(a), "b": tuple(b), "c": tuple(c), "r": float(r), "ccw": ccw, "src": src}


def _kc(p, q):
    return math.hypot(p[0] - q[0], p[1] - q[1])


def _don_vi(x, y):
    d = math.hypot(x, y)
    return (x / d, y / d) if d > 1e-12 else (0.0, 0.0)


def _dao(s):
    s2 = dict(s)
    s2["a"], s2["b"] = s["b"], s["a"]
    if s["k"] == "A":
        s2["ccw"] = not s["ccw"]
    return s2


def _quet(s):
    """Cung: (góc đầu, góc quét có dấu) radian."""
    c = s["c"]
    a0 = math.atan2(s["a"][1] - c[1], s["a"][0] - c[0])
    a1 = math.atan2(s["b"][1] - c[1], s["b"][0] - c[0])
    return (a0, (a1 - a0) % (2 * math.pi)) if s["ccw"] else (a0, -((a0 - a1) % (2 * math.pi)))


def _huong(s, cuoi):
    """Hướng đi tại đầu (cuoi=False) / cuối đoạn."""
    if s["k"] == "L":
        return _don_vi(s["b"][0] - s["a"][0], s["b"][1] - s["a"][1])
    p = s["b"] if cuoi else s["a"]
    rx, ry = p[0] - s["c"][0], p[1] - s["c"][1]
    tx, ty = _don_vi(-ry, rx)
    return (tx, ty) if s["ccw"] else (-tx, -ty)


def _diem_mau(vong):
    """Điểm lấy mẫu dọc vòng (cung mỗi ~10°) — để tính diện tích, hộp bao, khoảng cách."""
    pts = []
    for s in vong:
        pts.append(s["a"])
        if s["k"] == "A":
            a0, q = _quet(s)
            n = max(2, int(abs(math.degrees(q)) / 10) + 1)
            pts += [(s["c"][0] + s["r"] * math.cos(a0 + q * i / n), s["c"][1] + s["r"] * math.sin(a0 + q * i / n)) for i in range(1, n)]
    return pts


def _dien_tich(pts):
    return sum(pts[i - 1][0] * pts[i][1] - pts[i][0] * pts[i - 1][1] for i in range(len(pts))) / 2.0


def _hop(pts):
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def _giua(s):
    if s["k"] == "L":
        return ((s["a"][0] + s["b"][0]) / 2, (s["a"][1] + s["b"][1]) / 2)
    a0, q = _quet(s)
    return (s["c"][0] + s["r"] * math.cos(a0 + q / 2), s["c"][1] + s["r"] * math.sin(a0 + q / 2))


# ---------- dựng vòng ----------

def _tu_thuc_the(e, src):
    t = e.dxftype()
    if t == "LINE":
        a, b = (e.dxf.start.x, e.dxf.start.y), (e.dxf.end.x, e.dxf.end.y)
        return [_L(a, b, src)] if _kc(a, b) > NOI else []
    if t == "ARC":
        return [_A(tuple(e.start_point)[:2], tuple(e.end_point)[:2], (e.dxf.center.x, e.dxf.center.y), e.dxf.radius, True, src)]
    if t in ("LWPOLYLINE", "POLYLINE"):
        kq = []
        for v in e.virtual_entities():
            kq += _tu_thuc_the(v, src)
        return kq
    return []


def _noi_vong(manh):
    """Nối các đoạn theo đầu mút thành vòng kín; chuỗi hở bỏ qua."""
    con, kq = list(manh), []
    while con:
        cur = [con.pop(0)]
        while _kc(cur[-1]["b"], cur[0]["a"]) > NOI:
            j = next((i for i, s in enumerate(con) if _kc(s["a"], cur[-1]["b"]) <= NOI or _kc(s["b"], cur[-1]["b"]) <= NOI), None)
            if j is None:
                break
            s = con.pop(j)
            cur.append(s if _kc(s["a"], cur[-1]["b"]) <= NOI else _dao(s))
        if len(cur) > 1 and _kc(cur[-1]["b"], cur[0]["a"]) <= NOI:
            kq.append(cur)
    return kq


def _chuan_hoa(vong):
    """Chiều kim đồng hồ, bắt đầu ở đỉnh gần góc trái dưới hộp bao → (vòng, [chỉ số đoạn bắt đầu tại mỗi đỉnh])."""
    if _dien_tich(_diem_mau(vong)) > 0:
        vong = [_dao(s) for s in reversed(vong)]
    dinh = [i for i in range(len(vong)) if _goc_doi(_huong(vong[i - 1], True), _huong(vong[i], False)) > GAY]
    if dinh:
        x0, y0, _, _ = _hop(_diem_mau(vong))
        dau = min(dinh, key=lambda i: (_kc(vong[i]["a"], (x0, y0)), vong[i]["a"][1], vong[i]["a"][0]))
        vong = vong[dau:] + vong[:dau]
        dinh = sorted((i - dau) % len(vong) for i in dinh)
    return vong, dinh


def _goc_doi(u, v):
    return math.acos(max(-1.0, min(1.0, u[0] * v[0] + u[1] * v[1])))


def doc_vien(nen):
    """Vòng kín trên 0-KAKOU (trừ lỗ tròn, đường bẻ). → [vòng] (đoạn có src = thực thể gốc)."""
    manh = []
    for e in nen.msp.query('*[layer=="%s"]' % mt.LOP_CAT):
        if e.dxftype() == "CIRCLE" or str(e.dxf.get("linetype", "")).upper() == mt.NET_BE:
            continue
        manh += _tu_thuc_the(e, e)
    return _noi_vong(manh)


def _xoay(p, g):
    c, s = math.cos(math.radians(g)), math.sin(math.radians(g))
    return (p[0] * c - p[1] * s, p[0] * s + p[1] * c)


def _khoet_vong(k):
    """Một khoét → [(vòng đa giác, R bo mọi đỉnh)] theo từng vị trí."""
    kq = []
    for vt in k.get("vi_tri") or []:
        px, py = float(vt[0]), float(vt[1])
        if k.get("kieu") == "da_giac":
            dinh = [(px + float(p[0]), py + float(p[1])) for p in k.get("dinh") or []]
            r = 0.0
        else:
            w, h, g = float(k.get("rong") or 0), float(k.get("cao") or 0), float(k.get("goc_xoay") or 0)
            if w <= 0 or h <= 0:
                raise ValueError("khoét chữ nhật cần rộng × cao > 0")
            dinh = [(px + q[0], py + q[1]) for q in (_xoay(p, g) for p in ((-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)))]
            r = float(k.get("r") or 0)
        if len(dinh) < 3:
            raise ValueError("khoét đa giác cần ít nhất 3 đỉnh")
        kq.append(([_L(dinh[i], dinh[(i + 1) % len(dinh)]) for i in range(len(dinh))], r))
    return kq


# ---------- vát / bo một vòng ----------

def _ap_goc(vong, yc):
    """yc {chỉ số đoạn bắt đầu tại đỉnh: (kiểu, giá trị, tên đỉnh)} → (vòng mới, [(kiểu, giá trị, điểm giữa mảnh góc)])."""
    n = len(vong)
    cat_dau, cat_cuoi = [0.0] * n, [0.0] * n
    ten_dau, ten_cuoi = [""] * n, [""] * n
    manh, nhan = {}, []
    for i, (k, v, ten) in yc.items():
        s_in, s_out = vong[i - 1], vong[i]
        if s_in["k"] != "L" or s_out["k"] != "L":
            raise ValueError("đỉnh %s nằm giữa cạnh thẳng và cung — chỉ vát / bo được góc giữa 2 cạnh thẳng" % ten)
        p = s_out["a"]
        u_in = _don_vi(s_in["a"][0] - p[0], s_in["a"][1] - p[1])
        u_out = _don_vi(s_out["b"][0] - p[0], s_out["b"][1] - p[1])
        phi = _goc_doi(u_in, u_out)
        if phi < math.radians(2) or phi > math.radians(178):
            raise ValueError("đỉnh %s gần như thẳng / gập ngược — không vát / bo được" % ten)
        d = v if k == "C" else v / math.tan(phi / 2)
        cat_cuoi[i - 1] += d
        cat_dau[i] += d
        ten_cuoi[i - 1], ten_dau[i] = ten, ten
        p_in, p_out = (p[0] + u_in[0] * d, p[1] + u_in[1] * d), (p[0] + u_out[0] * d, p[1] + u_out[1] * d)
        if k == "C":
            manh[i] = _L(p_in, p_out)
        else:
            b = _don_vi(u_in[0] + u_out[0], u_in[1] + u_out[1])
            c = (p[0] + b[0] * v / math.sin(phi / 2), p[1] + b[1] * v / math.sin(phi / 2))
            re_trai = (-u_in[0]) * u_out[1] - (-u_in[1]) * u_out[0] > 0
            manh[i] = _A(p_in, p_out, c, v, re_trai)
        if not ten.endswith("(R khoét)"):      # R bo sẵn của khoét chữ nhật: không nhãn riêng (dim như khách)
            nhan.append((k, v, _giua(manh[i])))
    moi = []
    for i, s in enumerate(vong):
        if i in manh:
            moi.append(manh[i])
        if s["k"] != "L":
            moi.append(s)
            continue
        dai = _kc(s["a"], s["b"])
        if cat_dau[i] + cat_cuoi[i] > dai + NOI:
            if ten_dau[i] and ten_cuoi[i]:
                raise ValueError("đỉnh %s và %s: phần lùi %g + %g mm lấn nhau trên cạnh dài %g mm"
                                 % (ten_dau[i], ten_cuoi[i], round(cat_dau[i], 2), round(cat_cuoi[i], 2), round(dai, 2)))
            raise ValueError("đỉnh %s: phần lùi %g mm dài hơn cạnh kề (%g mm)" % (ten_dau[i] or ten_cuoi[i], round(cat_dau[i] + cat_cuoi[i], 2), round(dai, 2)))
        u = _don_vi(s["b"][0] - s["a"][0], s["b"][1] - s["a"][1])
        a = (s["a"][0] + u[0] * cat_dau[i], s["a"][1] + u[1] * cat_dau[i])
        b = (s["b"][0] - u[0] * cat_cuoi[i], s["b"][1] - u[1] * cat_cuoi[i])
        if _kc(a, b) > NOI:
            moi.append(_L(a, b, s["src"]))
    return moi, nhan


def _ve_vong(nen, vong):
    for s in vong:
        if s["k"] == "L":
            nen.duong(s["a"], s["b"])
        else:
            a0, q = _quet(s)
            g0, g1 = math.degrees(a0), math.degrees(a0 + q)
            nen.cung(s["c"], s["r"], g0, g1) if q > 0 else nen.cung(s["c"], s["r"], g1, g0)


# ---------- tìm đỉnh ----------

def _vong_goc(nen, t):
    """→ (vòng ngoài, [vòng khoét (R)], [vòng trong khác của mẫu]) đã chuẩn hoá, chưa vát / bo."""
    vong = doc_vien(nen)
    if not vong:
        raise ValueError("không dựng được viền kín từ các nét 0-KAKOU của mẫu")
    def dt(v):
        x0, y0, x1, y1 = _hop(_diem_mau(v))
        return (x1 - x0) * (y1 - y0)
    ngoai = max(vong, key=dt)
    khoet = []
    for k in t.get("khoet") or []:
        khoet += _khoet_vong(k)
    return ({"seg": ngoai, "goc_cu": True}, [{"seg": v, "r": r} for v, r in khoet],
            [{"seg": v, "goc_cu": True} for v in vong if v is not ngoai])


def _danh_so(ngoai, khoet, trong):
    """Gắn chuẩn hoá + tên đỉnh: {tên: (vòng, chỉ số đoạn)}."""
    ten = {}
    ngoai["seg"], ngoai["dinh"] = _chuan_hoa(ngoai["seg"])
    for j, i in enumerate(ngoai["dinh"]):
        ten[str(j + 1)] = (ngoai, i)
    for n, k in enumerate(khoet):
        k["seg"], k["dinh"] = _chuan_hoa(k["seg"])
        k["ten"] = "k%d" % (n + 1)
        for j, i in enumerate(k["dinh"]):
            ten["k%d.%d" % (n + 1, j + 1)] = (k, i)
    for v in trong:
        v["seg"], v["dinh"] = _chuan_hoa(v["seg"])
    return ten


def _tim(d, ngoai, khoet, trong, ten):
    if d in ten:
        return ten[d]
    if d.isdigit():
        raise ValueError("không có đỉnh số %s — viền ngoài có %d đỉnh (xem sơ đồ đỉnh)" % (d, len(ngoai["dinh"])))
    if DINH_K.match(d):
        raise ValueError("không có đỉnh khoét %s (xem sơ đồ đỉnh)" % d)
    if d in TEN_GOC:
        x0, y0, x1, y1 = _hop(_diem_mau(ngoai["seg"]))
        fx, fy = TEN_GOC[d]
        dich = (x1 if fx else x0, y1 if fy else y0)
        ung = [(ngoai, i) for i in ngoai["dinh"]]
    else:
        try:
            dich = tuple(float(p) for p in d.split(","))
            assert len(dich) == 2
        except (ValueError, AssertionError):
            raise ValueError("đỉnh '%s' không hiểu" % d)
        ung = [(v, i) for v in [ngoai] + khoet + trong for i in v["dinh"]]
    if not ung:
        raise ValueError("viền không có đỉnh nào để tìm '%s'" % d)
    v, i = min(ung, key=lambda x: _kc(x[0]["seg"][x[1]]["a"], dich))
    lech = _kc(v["seg"][i]["a"], dich)
    if lech > DUNG_SAI:
        raise ValueError("không thấy đỉnh '%s' — đỉnh gần nhất lệch %g mm (xem sơ đồ đỉnh)" % (d, round(lech, 2)))
    return v, i


# ---------- kiểm khoét (K3) ----------

def _trong_da_giac(p, pts):
    x, y, kq = p[0], p[1], False
    for i in range(len(pts)):
        (x1, y1), (x2, y2) = pts[i - 1], pts[i]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
            kq = not kq
    return kq


def _kc_doan(p, a, b):
    vx, vy = b[0] - a[0], b[1] - a[1]
    l2 = vx * vx + vy * vy
    s = 0.0 if l2 == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * vx + (p[1] - a[1]) * vy) / l2))
    return _kc(p, (a[0] + vx * s, a[1] + vy * s))


def _kc_vong(p, pts):
    return min(_kc_doan(p, pts[i - 1], pts[i]) for i in range(len(pts)))


def _kc_hai(a, b):
    return min(min(_kc_vong(p, b) for p in a), min(_kc_vong(p, a) for p in b))


def kiem_khoet(ngoai, khoet, lo, t_day):
    """Cảnh báo (vàng): khoét chạm / vượt viền, đè lỗ / khoét khác, mép còn lại mỏng hơn T. lo = [(x, y, bán kính, nhãn)]."""
    cb = []
    pn = _diem_mau(ngoai)
    mau = [(k["ten"], _diem_mau(k["seg"])) for k in khoet]
    for ten, p in mau:
        if not all(_trong_da_giac(q, pn) for q in p):
            cb.append("khoét %s chạm / vượt viền ngoài" % ten)
            continue
        d = _kc_hai(p, pn)
        if d < NOI:
            cb.append("khoét %s chạm viền ngoài" % ten)
        elif d < t_day - 0.01:
            cb.append("khoét %s: mép tới viền ngoài còn %g mm < T %g" % (ten, round(d, 2), t_day))
    for i in range(len(mau)):
        for j in range(i + 1, len(mau)):
            (ta, pa), (tb, pb) = mau[i], mau[j]
            d = _kc_hai(pa, pb)
            if d < NOI or any(_trong_da_giac(q, pb) for q in pa) or any(_trong_da_giac(q, pa) for q in pb):
                cb.append("khoét %s đè khoét %s" % (ta, tb))
            elif d < t_day - 0.01:
                cb.append("khoét %s – %s: vách còn %g mm < T %g" % (ta, tb, round(d, 2), t_day))
    for x, y, r, nhan in lo:
        for ten, p in mau:
            d = _kc_vong((x, y), p) - r
            if _trong_da_giac((x, y), p) or d < NOI:
                cb.append("lỗ %s tại %g,%g đè khoét %s" % (nhan, x, y, ten))
            elif d < t_day - 0.01:
                cb.append("lỗ %s tại %g,%g: mép tới khoét %s còn %g mm < T %g" % (nhan, x, y, ten, round(d, 2), t_day))
    return cb


# ---------- áp vào bản vẽ ----------

def ap_dung(nen, t, lo=()):
    """Vẽ khoét + vát / bo theo t["goc"], t["khoet"] sau khi mẫu vẽ viền. Sai đỉnh / quá khả năng → ValueError.
    → ([(nhãn "2-C10", điểm)], [R đã ghi nhãn], [cảnh báo K3])."""
    goc, ds_khoet = t.get("goc") or [], t.get("khoet") or []
    if not goc and not ds_khoet:
        return [], [], []
    ngoai, khoet, trong = _vong_goc(nen, t)
    ten = _danh_so(ngoai, khoet, trong)
    yc = {}   # id(vòng) → {chỉ số: (kiểu, giá trị, tên)}
    for k in khoet:
        if k["r"] > 0:
            yc[id(k)] = {i: ("R", k["r"], "%s (R khoét)" % k["ten"]) for i in k["dinh"]}
    da = {}
    nhan_goc = []
    for g in goc:
        d = str(g.get("dinh", "")).strip().lower().replace(" ", "")
        kieu, v = str(g.get("kieu", "")).upper()[:1], float(g.get("gia_tri") or 0)
        if kieu not in ("C", "R") or v <= 0:
            raise ValueError("góc '%s': cần C hoặc R > 0" % d)
        vong, i = _tim(d, ngoai, khoet, trong, ten)
        khoa = (id(vong), i)
        if khoa in da:
            raise ValueError("đỉnh '%s' và '%s' là cùng một đỉnh" % (da[khoa], d))
        da[khoa] = d
        cu = yc.setdefault(id(vong), {})
        if i in cu:
            raise ValueError("đỉnh %s đã bo R theo khoét — bỏ R khoét hoặc bỏ góc này" % d)
        cu[i] = (kieu, v, d)
    for vong in [ngoai] + khoet + trong:
        y = yc.get(id(vong))
        if y:
            if vong.get("goc_cu"):     # vòng của mẫu: xoá nét cũ rồi vẽ lại cả vòng
                for e in {id(s["src"]): s["src"] for s in vong["seg"] if s["src"] is not None}.values():
                    nen.msp.delete_entity(e)
            vong["seg"], nh = _ap_goc(vong["seg"], y)
            nhan_goc += nh
        if y or "r" in vong:
            _ve_vong(nen, vong["seg"])
    canh_bao = kiem_khoet(ngoai["seg"], khoet, list(lo), float(t.get("t_day") or 0)) if khoet else []
    return _nhom_nhan(nen, ngoai["seg"], nhan_goc), sorted({v for k, v, _ in nhan_goc if k == "R"}), canh_bao


def _nhom_nhan(nen, ngoai, nhan):
    """Gom nhãn theo (kiểu, giá trị) → leader "n-C10" ra ngoài từ góc đầu tiên của nhóm."""
    x0, y0, x1, y1 = _hop(_diem_mau(ngoai))
    tam = ((x0 + x1) / 2, (y0 + y1) / 2)
    nhom = {}
    for k, v, p in nhan:
        nhom.setdefault((k, round(v, 3)), []).append(p)
    kq = []
    for (k, v), ds in sorted(nhom.items()):
        chu = "%d-%s%g" % (len(ds), k, v)
        p = ds[0]
        u = _don_vi(p[0] - tam[0], p[1] - tam[1])
        q = (p[0] + u[0] * 40, p[1] + u[1] * 40)
        nen.leader([p, q, (q[0] + (1 if u[0] >= 0 else -1), q[1])], chu)
        kq.append((chu, p))
    return kq


# ---------- sơ đồ đỉnh ----------

def so_do(nen, t, ra):
    """Ảnh PNG viền ngoài + khoét (CHƯA vát / bo) có ghi tên đỉnh — người / AI chỉ đỉnh theo đây. → số đỉnh viền ngoài."""
    import ezdxf
    from ezdxf.addons.drawing import matplotlib as dm
    from ezdxf.enums import TextEntityAlignment
    ngoai, khoet, trong = _vong_goc(nen, t)
    ten = _danh_so(ngoai, khoet, trong)
    x0, y0, x1, y1 = _hop(_diem_mau(ngoai["seg"]))
    cao = max(x1 - x0, y1 - y0) / 40.0
    doc = ezdxf.new()
    msp = doc.modelspace()
    for v in [ngoai] + khoet + trong:
        for s in v["seg"]:
            if s["k"] == "L":
                msp.add_line(s["a"], s["b"], dxfattribs={"color": 7})
            else:
                a0, q = _quet(s)
                g0, g1 = math.degrees(a0), math.degrees(a0 + q)
                msp.add_arc(s["c"], s["r"], *((g0, g1) if q > 0 else (g1, g0)), dxfattribs={"color": 7})
    for chu, (v, i) in ten.items():
        p = v["seg"][i]["a"]
        d1, d2 = _huong(v["seg"][i - 1], True), _huong(v["seg"][i], False)
        n = _don_vi(-d1[1] - d2[1], d1[0] + d2[0])      # pháp tuyến trái (vòng chiều kim đồng hồ → ra ngoài vòng)
        msp.add_circle(p, cao / 5, dxfattribs={"color": 1})
        msp.add_text(chu, height=cao, dxfattribs={"color": 1 if v is ngoai else 5}).set_placement(
            (p[0] + n[0] * cao * 1.3, p[1] + n[1] * cao * 1.3), align=TextEntityAlignment.MIDDLE_CENTER)
    dm.qsave(msp, ra, bg="#FFFFFF", size_inches=(6, 4), dpi=120)
    return len(ngoai["dinh"])
