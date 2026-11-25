"""Phần vẽ ra từ giá trị AI TỰ SUY (khách không ghi số, vd R bo góc khoét chỉ thấy trên hình) → tô VÀNG (25/11) để người dùng xác nhận.

t["chua_chac"] = [{"o": ô, "so": thứ tự mục trong ô (1…; 0 = cả ô), "noi_dung": "..."}] — AI ghi lúc đọc; mục của ô người dùng
đã sửa thì OhtaTeams bỏ trước khi gửi (coi là đã xác nhận). ô: lo / khoet / goc / dinh / ke_gaki; thêm ".r" (vd "khoet.r") = chỉ
phần cung bo R của mục đó chưa chắc. Chọn nét theo hình học (không cần mẫu ghi lại nét nào vẽ từ đâu).
"""
import math

VANG = 2            # ACI vàng
SAI_SO = 0.05


def _diem(e):
    """Điểm mẫu của một nét (để xét nét nằm trong vùng nào)."""
    t = e.dxftype()
    if t == "LINE":
        a, b = e.dxf.start, e.dxf.end
        return [(a.x, a.y), (b.x, b.y), ((a.x + b.x) / 2, (a.y + b.y) / 2)]
    if t == "ARC":
        c, r = e.dxf.center, e.dxf.radius
        a0, a1 = e.dxf.start_angle, e.dxf.end_angle
        q = (a1 - a0) % 360 or 360
        return [(c.x + r * math.cos(math.radians(a0 + q * i / 4)), c.y + r * math.sin(math.radians(a0 + q * i / 4))) for i in range(5)]
    if t == "CIRCLE":
        c, r = e.dxf.center, e.dxf.radius
        return [(c.x + r * math.cos(math.radians(g)), c.y + r * math.sin(math.radians(g))) for g in (0, 90, 180, 270)]
    if t in ("LWPOLYLINE", "POLYLINE"):
        kq = []
        for v in e.virtual_entities():
            kq += _diem(v)
        return kq
    return []


def _chon(ds, so):
    """Mục thứ so (1…) của danh sách; 0 / ngoài khoảng → cả danh sách."""
    return [ds[so - 1]] if 1 <= so <= len(ds) else list(ds)


def vung_muc(t, o, so, diem_goc=()):
    """Mục chưa chắc → hàm kiểm điểm (x, y) thuộc phần vẽ của mục; None = ô không tô (chỉ ghi chú)."""
    if o == "lo":
        tam = []
        for g in _chon([g for g in (t.get("lo") or []) if isinstance(g, dict)], so):
            R = max(float(g.get("d") or 0), float(g.get("rong") or 0), float(g.get("dai") or 0), float(g.get("d_sara") or 0), 1.0) / 2.0 + 0.1
            tam += [(float(p["x"]), float(p["y"]), R) for p in (g.get("vi_tri") or []) if isinstance(p, dict) and p.get("x") is not None]
        return lambda p: any(math.hypot(p[0] - x, p[1] - y) <= R for x, y, R in tam)
    if o == "khoet":
        import goc_khoet
        hop = []
        for k in _chon(t.get("khoet") or [], so):
            for vong, r in goc_khoet._khoet_vong(k):
                xs = [s["a"][0] for s in vong]
                ys = [s["a"][1] for s in vong]
                hop.append((min(xs) - 0.1, min(ys) - 0.1, max(xs) + 0.1, max(ys) + 0.1))
        return lambda p: any(x0 <= p[0] <= x1 and y0 <= p[1] <= y1 for x0, y0, x1, y1 in hop)
    if o == "goc":
        goc = t.get("goc") or []
        chon = [i for i in range(len(goc)) if so == 0 or i == so - 1]
        vung = [(diem_goc[i], float(goc[i].get("gia_tri") or 0) * 1.5 + 0.1) for i in chon if i < len(diem_goc) and diem_goc[i] is not None]
        return lambda p: any(math.hypot(p[0] - c[0], p[1] - c[1]) <= R for c, R in vung)
    if o == "dinh":
        dinh = [(float(q[0]), float(q[1])) for q in (t.get("dinh") or []) if isinstance(q, (list, tuple)) and len(q) >= 2]
        if so == 0:
            return lambda p: True
        if not 1 <= so <= len(dinh):
            return None
        a, b, c = dinh[so - 2], dinh[so - 1], dinh[so % len(dinh)]   # hai cạnh kề đỉnh so
        return lambda p: _tren_doan(p, a, b) or _tren_doan(p, b, c) or _gan_cung(p, a, b) or _gan_cung(p, b, c)
    if o == "ke_gaki":
        import moi_truong as mt
        kg = _chon([mt.ke_gaki_so(k) for k in (t.get("ke_gaki") or [])], so)
        return lambda p: any(math.hypot(p[0] - x, p[1] - y) <= max(5.0, c) + SAI_SO for x, y, c, _, _ in kg)
    if o == "slit":
        sl = _chon([[float(v) for v in s[:4]] for s in (t.get("slit") or []) if isinstance(s, (list, tuple)) and len(s) >= 4], so)
        return lambda p: any(_tren_doan(p, (s[0], s[1]), (s[2], s[3])) for s in sl)
    return None


def _tren_doan(p, a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]
    l2 = dx * dx + dy * dy
    u = 0.0 if l2 <= 1e-12 else max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / l2))
    return math.hypot(p[0] - a[0] - u * dx, p[1] - a[1] - u * dy) <= SAI_SO


def _gan_cung(p, a, b):
    """Cạnh cung (dinh có R): điểm nằm trong hình tròn đường kính = dây cung nới rộng (cung phồng ra tối đa nửa dây)."""
    m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
    return math.hypot(p[0] - m[0], p[1] - m[1]) <= math.hypot(b[0] - a[0], b[1] - a[1]) / 2 + SAI_SO


def to_vang(nen, t, diem_goc=()):
    """Tô vàng nét 0-KAKOU (và ケガキ) thuộc các mục chưa chắc. → số nét đã tô."""
    import moi_truong as mt
    muc = [m for m in (t.get("chua_chac") or []) if isinstance(m, dict)]
    if not muc:
        return 0
    net = [e for e in nen.msp.query('*[layer=="%s" | layer=="%s" | layer=="%s"]' % (mt.LOP_CAT, mt.LOP_KEGAKI, mt.LOP_SLIT))]
    lop = {"ke_gaki": mt.LOP_KEGAKI, "slit": mt.LOP_SLIT}
    da = 0
    for m in muc:
        o = str(m.get("o") or "").strip().lower()
        chi_r = o.endswith(".r")
        o = o.split(".")[0]
        try:
            so = int(float(m.get("so") or 0))
        except (TypeError, ValueError):
            so = 0
        thuoc = vung_muc(t, o, so, diem_goc)
        if thuoc is None:
            continue
        for e in net:
            if e.dxf.layer != lop.get(o, mt.LOP_CAT):
                continue
            if chi_r and e.dxftype() != "ARC":
                continue
            ds = _diem(e)
            if ds and all(thuoc(p) for p in ds) and e.dxf.get("color") != VANG:
                e.dxf.color = VANG
                da += 1
    return da


# ---------- tự kiểm (hàm thuần) ----------
_t = {"khoet": [{"kieu": "chu_nhat", "rong": 240, "cao": 240, "goc_xoay": 0, "r": 10, "vi_tri": [[240, 185]]}],
      "lo": [{"d": 50, "vi_tri": [{"x": 50, "y": 185}]}], "dinh": [[0, 0], [10, 0], [110, 150], [110, 160], [0, 160]]}
assert vung_muc(_t, "khoet", 1)((125, 70)) and not vung_muc(_t, "khoet", 1)((10, 185))       # mép khoét / mép tấm
assert vung_muc(_t, "lo", 1)((75, 185)) and not vung_muc(_t, "lo", 1)((120, 185))
assert vung_muc(_t, "dinh", 3)((60, 75)) and vung_muc(_t, "dinh", 3)((110, 155)) and not vung_muc(_t, "dinh", 3)((5, 0))
assert vung_muc({"slit": [[0, 0, 0, 40]]}, "slit", 1)((0, 20)) and not vung_muc({"slit": [[0, 0, 0, 40]]}, "slit", 1)((1, 20))
assert vung_muc({"ke_gaki": [[0, 0, 30, 90]]}, "ke_gaki", 0)((0, 17.3)) and not vung_muc({"ke_gaki": [[0, 0]]}, "ke_gaki", 0)((0, 17.3))
assert vung_muc(_t, "kich_thuoc", 0) is None                                                    # ô khác: chỉ ghi chú
