"""Vẽ bản trải phẳng phòng 4 (OhtaTeams) bằng ezdxf trên môi trường moi-truong-ve.dxf.

    python ve.py --kiem                 → {"python", "ezdxf", "matplotlib"}  (ezdxf thiếu → "ezdxf": null)
    python ve.py --thu-vien DIR [DIR…]  → [{"ten", "nhom", "mo_ta", "tep", "tham_so": [[tên, ý nghĩa, mặc định]], "vi_du", "anh"}]
                                          mẫu biên dạng (thư mục sau đè thư mục trước); anh = <ten>.png hướng dẫn nếu có
    python ve.py --huong-dan MOI_TRUONG DIR → [{"ten", "anh", "loi"}]  vẽ ảnh hướng dẫn <ten>.png cho mọi mẫu trong DIR
    python ve.py --so-do VIEC.json      → [{"bango", "anh", "so_dinh", "loi"}]  ảnh sơ đồ đỉnh (viền + khoét, số đỉnh) mỗi tấm
    python ve.py --cat-anh ANH.json     → [{"ra", "loi"}]  cắt ảnh tấm từ bản vẽ khách (PDF: PyMuPDF; DXF: ezdxf)
    python ve.py VIEC.json              → {"ket_qua": [{"bango", "tep", "anh", "loi", "thieu_dim", "canh_bao"}]}

VIEC.json (OhtaTeams ghi): {"moi_truong": dxf, "thu_muc_ra": dir, "thu_vien": [dir…], "chi_hinh": bỏ F-name + Khung_IN (tuỳ chọn),
  "tam": [{"bango", "bien_dang", "x", "y", "tham_so": {tên: số}, "be_x", "be_y", "doan_x", "doan_y", "ke_gaki": [[x, y]…], "dim_khach": [{"loai", "gia_tri", "tu", "den"}], "fname": [dòng], "ghi_chu": [dòng],
  "goc": [{"dinh", "kieu" C/R, "gia_tri"}], "khoet": [{"kieu", "rong", "cao", "goc_xoay", "r", "dinh", "vi_tri"}], "t_day": số}]}  (xem goc_khoet.py)
Mẫu biên dạng (thu_vien/<nhóm>/<ten>.py — nhóm: tam_don, tam_be, tam_cuon…): MO_TA + ve(nen, t) vẽ viền / đường bẻ / dim riêng của hình; ケガキ, chuỗi dim dưới / phải,
dim tổng (1-XY), F-name, Khung_IN thêm ở đây (them_chung) theo bản chuẩn xưởng.
Tệp đã sửa tay trên CAD (mã băm khác lần ghi trước, nhớ trong .ve_dxf.json) không bị ghi đè: ghi "<bango> (2).dxf".
"""
import hashlib
import importlib.util
import json
import os
import sys
import traceback

DAY = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, DAY)
os.environ.setdefault("OPENBLAS_NUM_THREADS", "1")  # numpy (matplotlib) nhiều luồng có lúc hết bộ nhớ khi vẽ ảnh


def in_json(o):
    sys.stdout.buffer.write(json.dumps(o, ensure_ascii=False).encode("utf-8"))
    sys.stdout.buffer.write(b"\n")


def kiem():
    kq = {"python": sys.executable, "ezdxf": None, "matplotlib": False, "pymupdf": False}
    try:
        import fitz  # noqa: F401 — PyMuPDF: cắt ảnh tấm từ PDF khách
        kq["pymupdf"] = True
    except ImportError:
        pass
    try:
        import ezdxf
        kq["ezdxf"] = ezdxf.__version__
    except ImportError:
        pass
    try:
        import matplotlib  # noqa: F401 — chỉ để vẽ ảnh xem trước
        kq["matplotlib"] = True
    except ImportError:
        pass
    return kq


def cac_mau(thu_muc):
    """ten → tệp .py, tìm cả thư mục nhóm con (thu_vien/tam_be/be.py…); thư mục sau đè thư mục trước
    (mẫu vẽ thử của đơn đè mẫu chung cùng tên). Tên mẫu là duy nhất trong thư viện — trùng giữa 2 nhóm thì nhóm xếp trước thắng."""
    mau = {}
    for d in thu_muc:
        if d and os.path.isdir(d):
            rieng = {}
            for goc, con, tep in os.walk(d):
                con[:] = sorted(x for x in con if not x.startswith(("_", ".")))
                for f in sorted(tep):
                    if f.endswith(".py") and not f.startswith("_"):
                        rieng.setdefault(f[:-3], os.path.join(goc, f))
            mau.update(rieng)
    return mau


def nhom_mau(tep, thu_muc):
    """Nhóm = tên thư mục con chứa mẫu (tam_don / tam_be / tam_cuon…); mẫu nằm ngay thư mục gốc → ""."""
    cha = os.path.normcase(os.path.abspath(os.path.dirname(tep)))
    return "" if cha in {os.path.normcase(os.path.abspath(d)) for d in thu_muc if d} else os.path.basename(cha)


def nap(tep):
    spec = importlib.util.spec_from_file_location("mau_" + os.path.splitext(os.path.basename(tep))[0], tep)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def ma_bam(tep):
    with open(tep, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


def ten_ra(thu_muc, bango, so_tay):
    """<bango>.dxf; đã bị sửa tay (mã băm khác lần ghi trước) → <bango> (2).dxf, (3)…"""
    tep = os.path.join(thu_muc, bango + ".dxf")
    if not os.path.exists(tep) or so_tay.get(bango + ".dxf") == ma_bam(tep):
        return tep
    i = 2
    while True:
        tep = os.path.join(thu_muc, "%s (%d).dxf" % (bango, i))
        if not os.path.exists(tep) or so_tay.get(os.path.basename(tep)) == ma_bam(tep):
            return tep
        i += 1


def tham_so_mau(mod):
    """THAM_SO của mẫu: [(tên, ý nghĩa, mặc định)] — mặc định None = bắt buộc, "tự tính" = chương trình tính (x, y…)."""
    kq = []
    for ts in getattr(mod, "THAM_SO", []) or []:
        ts = list(ts) + [None] * 3
        kq.append((str(ts[0]), str(ts[1] or ""), None if ts[2] is None else str(ts[2])))
    return kq


def anh_huong_dan(moi_truong, thu_muc):
    """Ảnh hướng dẫn mỗi mẫu trong thu_muc → <ten>.png cạnh mẫu: vẽ VI_DU, dim nào bằng số của x / y / tham số thì ghi tên
    (x, y, r1…) thay cho số. Số trong VI_DU phải khác nhau để nhận ra. Trả [{"ten", "anh", "loi"}]."""
    import moi_truong as mt
    from ezdxf.addons.drawing import matplotlib as dm
    kq = []
    for ten, tep in sorted(cac_mau([thu_muc]).items()):
        try:
            mod = nap(tep)
            vd = dict(getattr(mod, "VI_DU", None) or {})
            if not vd:
                raise ValueError("mẫu chưa có VI_DU (số ví dụ)")
            t = {"x": float(vd.get("x", 300)), "y": float(vd.get("y", 100)), "tham_so": dict(vd.get("tham_so") or {})}
            for k in ("be_x", "be_y", "doan_x", "doan_y"):
                t[k] = list(vd.get(k, []))
            nen = mt.NenVe(moi_truong)
            nen.dat_ti_le(t["x"], t["y"])
            mod.ve(nen, t)
            x0, y0, x1, y1 = mt.khung_bao(nen)
            them_chung(nen, t, x0, y0, x1, y1, chi_dim=True)
            nhan = [(t["x"], "x"), (t["y"], "y")] + [(float(v), k) for k, v in t["tham_so"].items()]
            for e in list(nen.msp.query("DIMENSION")):
                so = float(e.get_measurement())
                chu = next((k for v, k in nhan if abs(v - so) <= 0.01), None)
                if chu:
                    e.dxf.text = chu
                    e.override().render()
            # nhãn "C15" (vát góc) bằng số thông số → ghi tên (c1…)
            for e in nen.msp.query("MTEXT TEXT"):
                chu_nhan = e.text if e.dxftype() == "MTEXT" else e.dxf.text
                if chu_nhan[:1] == "C":
                    try:
                        so = float(chu_nhan[1:])
                    except ValueError:
                        continue
                    ten_ts = next((k for v, k in nhan if abs(v - so) <= 0.01), None)
                    if ten_ts:
                        if e.dxftype() == "MTEXT":
                            e.text = ten_ts
                        else:
                            e.dxf.text = ten_ts
            anh = os.path.splitext(tep)[0] + ".png"  # cạnh mẫu, trong thư mục nhóm
            dm.qsave(nen.msp, anh, bg="#FFFFFF", size_inches=(6, 4), dpi=110)
            kq.append({"ten": ten, "anh": anh, "loi": None})
        except Exception as ex:
            kq.append({"ten": ten, "anh": None, "loi": "%s: %s" % (type(ex).__name__, ex)})
    return kq


def chuoi(moc, a, b):
    """Mốc trong [a, b] (gồm 2 đầu), sắp xếp, bỏ trùng (< 0.01 mm)."""
    kq = []
    for v in sorted([a, b] + [float(m) for m in moc if a - 0.01 <= float(m) <= b + 0.01]):
        if not kq or v - kq[-1] > 0.01:
            kq.append(v)
    return kq


def doan_gop(moc):
    """Mốc chuỗi dim → [(a, b, chữ)]: từ 3 đoạn bằng nhau liền nhau gộp một dim "100P×5=500" như bản vẽ khách."""
    doan = list(zip(moc, moc[1:]))
    kq, i = [], 0
    while i < len(doan):
        b = doan[i][1] - doan[i][0]
        j = i
        while j + 1 < len(doan) and abs(doan[j + 1][1] - doan[j + 1][0] - b) <= 0.01:
            j += 1
        if j - i >= 2:
            kq.append((doan[i][0], doan[j][1], "%gP×%d=%g" % (b, j - i + 1, round(b * (j - i + 1), 2))))
            i = j + 1
        else:
            kq.append((doan[i][0], doan[i][1], "<>"))
            i += 1
    return kq


assert [c for _, _, c in doan_gop([0, 50, 150, 250, 350, 450, 550, 600])] == ["<>", "100P×5=500", "<>"]
assert [c for _, _, c in doan_gop([0, 100, 140])] == ["<>", "<>"]


def dim_da_ve(nen):
    """(loai, số đo) các dim đã vẽ: ngang / doc / r / phi / goc."""
    kq = []
    for e in nen.msp.query("DIMENSION"):
        kieu = e.dimtype & 7
        try:
            so = float(e.get_measurement())
        except Exception:
            continue
        if kieu == 4:
            kq.append(("r", so))
        elif kieu == 3:
            kq.append(("phi", so))
        elif kieu in (2, 5):
            kq.append(("goc", so))
        else:
            kq.append(("doc" if abs(float(e.dxf.get("angle", 0)) % 180 - 90) < 1 else "ngang", so))
    return kq


TARO_MOI = {"M3": 2.6, "M4": 3.4, "M5": 4.3, "M6": 5.1, "M8": 6.9, "M10": 8.6, "M12": 10.5, "M14": 12.2, "M16": 14.3, "M20": 17.7}  # Ø mồi như lệnh TMT


def hai_tam_khoa(px, py, k, goc, tam_nho):
    """Lỗ khoá: điểm (px, py) là tâm khách dim tới (tam_nho: tâm Ø nhỏ, không thì tâm Ø lớn); tâm kia cách k theo hướng goc (độ).
    Trả (tâm lớn, tâm nhỏ)."""
    import math
    q = (px + k * math.cos(math.radians(goc)), py + k * math.sin(math.radians(goc)))
    return (q, (px, py)) if tam_nho else ((px, py), q)


def cung_khoa(them_cung, lon, nho, R, r):
    """Viền hợp 2 vòng tròn (Ø lớn tâm lon bán kính R, Ø nhỏ tâm nho bán kính r) bằng 2 cung; them_cung(tâm, r, đầu, cuối).
    Hai vòng không giao nhau → False (người gọi vẽ 2 vòng riêng)."""
    import math
    k = math.hypot(nho[0] - lon[0], nho[1] - lon[1])
    if not (abs(R - r) < k < R + r):
        return False
    h = math.degrees(math.atan2(nho[1] - lon[1], nho[0] - lon[0]))       # hướng tâm lớn → tâm nhỏ
    a = math.degrees(math.acos((R * R + k * k - r * r) / (2 * R * k)))   # nửa góc giao nhìn từ tâm lớn
    b = math.degrees(math.acos((r * r + k * k - R * R) / (2 * r * k)))   # nhìn từ tâm nhỏ
    them_cung(lon, R, (h + a) % 360, (h - a) % 360)              # phần vòng lớn ngoài vòng nhỏ (góc 0–360 cho CAD)
    them_cung(nho, r, (h + 180 + b) % 360, (h + 180 - b) % 360)  # phần vòng nhỏ lồi ra ngoài
    return True


def ve_lo_khoa(nen, px, py, D, dn, k, goc, tam_nho):
    """Lỗ khoá / 変形孔 (18/11): Ø lớn D + Ø nhỏ dn, hai tâm cách k; (px, py) = tâm khách dim tới — viền cắt = hợp 2 vòng (cung thật 0-KAKOU)."""
    import math
    lon, nho = hai_tam_khoa(px, py, k, goc, tam_nho)
    if not cung_khoa(nen.cung, lon, nho, D / 2.0, dn / 2.0):
        nen.lo_tron(lon[0], lon[1], D)
        nen.lo_tron(nho[0], nho[1], dn)
    # dấu tâm như bản sửa 18/11: đoạn qua 2 tâm (ra ngoài mỗi vòng 1.6) + đoạn vuông góc tại mỗi tâm
    R, r = D / 2.0, dn / 2.0
    h = math.atan2(nho[1] - lon[1], nho[0] - lon[0])
    ux, uy, vx, vy = math.cos(h), math.sin(h), -math.sin(h), math.cos(h)
    nen.msp.add_line((lon[0] - ux * (R + 1.6), lon[1] - uy * (R + 1.6)), (nho[0] + ux * (r + 1.6), nho[1] + uy * (r + 1.6)), dxfattribs={"layer": "1-SUNPOU"})
    for c, rr in ((lon, R), (nho, r)):
        nen.msp.add_line((c[0] - vx * (rr + 1.6), c[1] - vy * (rr + 1.6)), (c[0] + vx * (rr + 1.6), c[1] + vy * (rr + 1.6)), dxfattribs={"layer": "1-SUNPOU"})


def ve_chi_tiet_khoa(nen, ten, D, dn, k, goc, tam_nho, tam, ref, khung):
    """Hình chi tiết lỗ khoá như hình A của khách (18/11): phóng TL lần tại `tam` (ngoài tấm, 1-SUNPOU nét liền — không cắt): viền lỗ,
    đường tâm, nét mép tấm gần nhất + dim "mép tấm → tâm khách dim tới" và "tâm → tâm kia" (khách: 10 | 6), nhãn Ø lớn / Ø nhỏ, chữ "A (5:1)".
    Trên tấm: khoanh lỗ tham chiếu `ref` + chữ ten. khung = (x0, y0, x1, y1) của tấm."""
    import math
    import moi_truong as mt
    s = nen.ti_le
    TL = max(1, int(round(60.0 * s / D)))
    R, r = D / 2.0, dn / 2.0
    lien = {"layer": mt.LOP_DIM, "linetype": "Continuous"}
    lon0, nho0 = hai_tam_khoa(ref[0], ref[1], k, goc, tam_nho)
    goc_x, goc_y = ref  # gốc phóng: tâm khách dim tới của lỗ tham chiếu ↔ điểm `tam`

    def P(p):  # toạ độ thật trên tấm → toạ độ trên hình chi tiết
        return (tam[0] + (p[0] - goc_x) * TL, tam[1] + (p[1] - goc_y) * TL)
    lon, nho = P(lon0), P(nho0)
    if not cung_khoa(lambda c, rr, a0, a1: nen.msp.add_arc(c, rr, a0, a1, dxfattribs=lien), lon, nho, R * TL, r * TL):
        nen.msp.add_circle(lon, R * TL, dxfattribs=lien)
        nen.msp.add_circle(nho, r * TL, dxfattribs=lien)
    # đường tâm (nét CENTER theo layer 1-SUNPOU): qua 2 tâm + vuông góc tại mỗi tâm
    h = math.atan2(nho[1] - lon[1], nho[0] - lon[0])
    ux, uy = math.cos(h), math.sin(h)
    vx, vy = -uy, ux
    nen.duong_tam((lon[0] - ux * (R * TL + 8), lon[1] - uy * (R * TL + 8)), (nho[0] + ux * (r * TL + 8), nho[1] + uy * (r * TL + 8)))
    for c, rr in ((lon, R * TL), (nho, r * TL)):
        nen.duong_tam((c[0] - vx * (rr + 8), c[1] - vy * (rr + 8)), (c[0] + vx * (rr + 8), c[1] + vy * (rr + 8)))
    top = max(lon[1] + R * TL, nho[1] + r * TL)
    nen.chu(tam[0] - 20 * s, top + 45 * s, "%s (%d:1)" % (ten, TL))
    nen.leader([(lon[0] + R * TL * 0.7, lon[1] + R * TL * 0.7), (lon[0] + R * TL + 20 * s, top + 15 * s), (lon[0] + R * TL + 21 * s, top + 15 * s)], "Ø%g" % D)
    nen.leader([(nho[0] - r * TL * 0.7, nho[1] + r * TL * 0.7), (nho[0] - r * TL - 25 * s, top + 15 * s), (nho[0] - r * TL - 45 * s, top + 15 * s)], "Ø%g" % dn)
    # dim như khách: mép tấm → tâm khách dim tới → tâm kia (chữ = số thật)
    x0, y0, x1, y1 = khung
    g = round(math.degrees(h)) % 360
    if g in (0, 180):
        mep = x0 if abs(ref[0] - x0) <= abs(x1 - ref[0]) else x1
        mepP = P((mep, ref[1]))[0]
        yd = min(lon[1] - R * TL, nho[1] - r * TL) - 25 * s
        nen.msp.add_line((mepP, tam[1] - R * TL - 15 * s), (mepP, tam[1] + R * TL + 15 * s), dxfattribs=lien)  # nét mép tấm
        moc = sorted([(mepP, None), (tam[0], None), (P(lon0 if tam_nho else nho0)[0], None)])
        for (a_, _), (b_, _) in zip(moc, moc[1:]):
            if b_ - a_ > 0.01:
                nen.dim_ngang(a_, b_, tam[1], yd, chu="%g" % round((b_ - a_) / TL, 2))
    elif g in (90, 270):
        mep = y0 if abs(ref[1] - y0) <= abs(y1 - ref[1]) else y1
        mepP = P((ref[0], mep))[1]
        xd = max(lon[0] + R * TL, nho[0] + r * TL) + 25 * s
        nen.msp.add_line((tam[0] - R * TL - 15 * s, mepP), (tam[0] + R * TL + 15 * s, mepP), dxfattribs=lien)
        moc = sorted([mepP, tam[1], P(lon0 if tam_nho else nho0)[1]])
        for a_, b_ in zip(moc, moc[1:]):
            if b_ - a_ > 0.01:
                nen.dim_doc(a_, b_, tam[0], xd, chu="%g" % round((b_ - a_) / TL, 2))
    nen.msp.add_circle(ref, D * 1.6, dxfattribs=lien)  # khoanh lỗ tham chiếu trên tấm
    nen.chu(ref[0] - D * 1.6 - 18 * s, ref[1] + 4 * s, ten)


assert hai_tam_khoa(10, 230, 6, 0, True) == ((16.0, 230.0), (10, 230))  # khách 18/11: dim tới tâm Ø7, Ø12 lệch 6 sang phải


def huong_khoa(g):
    """Hướng từ tâm khách dim tới sang tâm kia (độ); thiếu → Ø nhỏ bên trái Ø lớn (0 khi điểm là tâm nhỏ, 180 khi là tâm lớn)."""
    if g.get("goc") not in (None, ""):
        return float(g["goc"])
    return 180.0 if g.get("tam") == "lon" else 0.0


def ve_lo(nen, t, y1, nhan_dau, le=None, y_nhan_duoi=None):
    """Lỗ (12/11, phần chung mọi mẫu): mỗi nhóm lỗ của bảng tấm {d, so_luong, taro, sara, d_sara, loai tron / dai, rong, dai, goc,
    vi_tri: [{x, y}]} — tâm trên hình trải phẳng, gốc trái dưới. Lỗ tròn: CIRCLE 0-KAKOU (taro: Ø mồi theo bảng TMT); lỗ dài:
    2 cạnh + 2 cung thật. Mỗi nhóm một leader nhãn "n-Ø10" / "n-M6" / "n-皿Ø6.6 (Ø12)" / "n-10×20";
    nhóm chỉ MỘT lỗ tròn thường → dim đường kính Ø thay leader (bản chuẩn 130996-3). Lỗ khoá (loai khoa: d + d_nho lệch, MỌI lỗ cùng hướng goc
    như hình chi tiết của khách, mặc định 180 = Ø nhỏ bên trái) — ve_lo_khoa + hình chi tiết A, B… bên trái tấm (ve_chi_tiet_khoa). y_nhan_duoi: dim đặt phía trên (theo khách) → nhãn leader xuống dưới tấm, từ cao độ này.
    Trả (tâm x, tâm y cho chuỗi dim, [Ø đã ghi nhãn], [báo thiếu])."""
    import math
    import moi_truong as mt
    bx0, by0, bx1, by1 = mt.khung_bao(nen)
    xs, ys, phi, thieu = [], [], [], []
    k = nhan_dau
    so_chi_tiet = 0
    for g in t.get("lo", []) or []:
        if not isinstance(g, dict):
            continue
        d = float(g.get("d") or 0)
        taro = str(g.get("taro") or "").strip().upper()
        co = "M" + taro.lstrip("M").split("X")[0].split("×")[0].split("-")[0].strip() if taro else ""
        dai = str(g.get("loai") or "") == "dai" and float(g.get("dai") or 0) > float(g.get("rong") or 0) > 0
        pts = [(float(p["x"]), float(p["y"])) for p in (g.get("vi_tri") or []) if isinstance(p, dict) and p.get("x") is not None and p.get("y") is not None]
        khoa = str(g.get("loai") or "") == "khoa" and d > 0 and float(g.get("d_nho") or 0) > 0 and float(g.get("lech") or 0) > 0
        if dai:
            nhan = "%g×%g" % (float(g["rong"]), float(g["dai"]))
        elif khoa:
            nhan = "変形孔 Ø%g+Ø%g" % (d, float(g["d_nho"]))
        elif taro:
            nhan = co
        elif g.get("sara"):
            nhan = "皿Ø%g" % d + (" (Ø%g)" % float(g["d_sara"]) if g.get("d_sara") else "")
        else:
            nhan = "Ø%g" % d
        if not pts:
            thieu.append("lỗ %s ×%g: chưa có vị trí — nhập ở ✎ Sửa thông số → Lỗ" % (nhan, float(g.get("so_luong") or 1)))
            continue
        for px, py in pts:
            if dai:
                w, l, goc = float(g["rong"]), float(g["dai"]), math.radians(float(g.get("goc") or 0))
                r, nua = w / 2.0, (l - w) / 2.0
                ux, uy = math.cos(goc), math.sin(goc)   # trục dài
                vx, vy = -uy, ux
                a1, a2 = (px - ux * nua, py - uy * nua), (px + ux * nua, py + uy * nua)
                nen.duong((a1[0] + vx * r, a1[1] + vy * r), (a2[0] + vx * r, a2[1] + vy * r))
                nen.duong((a1[0] - vx * r, a1[1] - vy * r), (a2[0] - vx * r, a2[1] - vy * r))
                gd = math.degrees(goc)
                nen.cung(a2, r, gd - 90, gd + 90)
                nen.cung(a1, r, gd + 90, gd + 270)
                nen.dau_tam(px, py, (l / 2.0 + 1.6) if abs(math.sin(goc)) < 0.5 else (w / 2.0 + 1.6), (w / 2.0 + 1.6) if abs(math.sin(goc)) < 0.5 else (l / 2.0 + 1.6))
            elif khoa:
                ve_lo_khoa(nen, px, py, d, float(g["d_nho"]), float(g["lech"]), huong_khoa(g), g.get("tam") != "lon")
            else:
                dv = TARO_MOI.get(co, d) if taro else d
                if dv <= 0:
                    thieu.append("lỗ %s: chưa có Ø" % nhan)
                    break
                nen.lo_tron(px, py, dv)
                nen.dau_tam(px, py, dv / 2.0 + 1.6)
            xs.append(px)
            ys.append(py)
        if taro and co not in TARO_MOI:
            thieu.append("taro %s: ngoài bảng Ø mồi TMT — vẽ theo Ø %g, kiểm lại" % (co, d))
        if not taro and not dai and d > 0:
            phi.append(d)
        if khoa:
            phi.append(float(g["d_nho"]))
            ref = min(pts, key=lambda p: (p[0], abs(p[1] - (by0 + by1) / 2.0)))  # như khách: lỗ sát mép trái, gần giữa
            ve_chi_tiet_khoa(nen, "ABCDEFGH"[so_chi_tiet % 8], d, float(g["d_nho"]), float(g["lech"]), huong_khoa(g), g.get("tam") != "lon",
                             (bx0 - 140.0 * nen.ti_le, (by0 + by1) / 2.0 - so_chi_tiet * 160.0 * nen.ti_le), ref, (bx0, by0, bx1, by1))
            so_chi_tiet += 1
        if g.get("noi_tam"):  # khách vẽ đường tâm nối các lỗ → nối hàng / cột NGOÀI CÙNG của nhóm (lỗ khoá: qua tâm Ø lớn)
            tam_noi = [hai_tam_khoa(px, py, float(g["lech"]), huong_khoa(g), g.get("tam") != "lon")[0] for px, py in pts] if khoa else pts
            for cot in (0, 1):
                for v in (min(p[cot] for p in tam_noi), max(p[cot] for p in tam_noi)):
                    hang = sorted(p[1 - cot] for p in tam_noi if abs(p[cot] - v) <= 0.5)
                    if len(hang) >= 2:
                        a_, b_ = ((hang[0], v), (hang[-1], v)) if cot else ((v, hang[0]), (v, hang[-1]))
                        nen.duong_tam(a_, b_)
        thuong = not taro and not dai and not khoa and not g.get("sara") and d > 0
        if len(pts) == 1 and thuong:
            nen.dim_phi(pts[0][0], pts[0][1], d, le=le or 80.0)
            continue
        if thuong:  # nhóm lỗ tròn thường: dim Ø "n-Øx" chỉ vào lỗ gần góc trên-trái (bản sửa 18/11)
            gx, gy = min(pts, key=lambda p: (p[0] - p[1], p[0]))
            nen.dim_phi(gx, gy, d, goc=-45.0, le=(le or 80.0) / 2.0, chu="%d-<>" % len(pts), chu_ngang=True)  # xuống phải, chữ ngang (bản sửa 18/11)
            continue
        s_ = nen.ti_le  # khoảng nhãn theo tỉ lệ dim
        yn = y1 + (53 + k * 26) * s_ if y_nhan_duoi is None else y_nhan_duoi - (53 + k * 26) * s_
        gx, gy = min(pts, key=lambda p: (abs(p[1] - yn), p[0]))  # đường dẫn từ lỗ gần chỗ đặt nhãn nhất — ngắn, không cắt ngang tấm
        # mũi tên chạm MÉP lỗ (lỗ khoá: mép Ø lớn) theo hướng ra điểm gãy — không chỉ vào giữa lỗ (bản sửa 18/11)
        if khoa:
            (cx_, cy_), ban_kinh = hai_tam_khoa(gx, gy, float(g["lech"]), huong_khoa(g), g.get("tam") != "lon")[0], d / 2.0
        else:
            cx_, cy_, ban_kinh = gx, gy, (float(g["rong"]) / 2.0 if dai else (TARO_MOI.get(co, d) if taro else d) / 2.0)
        ex, ey = gx + 23 * s_ - cx_, yn - cy_
        dd = math.hypot(ex, ey) or 1.0
        nen.leader([(cx_ + ex / dd * ban_kinh, cy_ + ey / dd * ban_kinh), (gx + 23 * s_, yn), (gx + 24 * s_, yn)], "%d-%s" % (len(pts), nhan))
        k += 1
    return xs, ys, phi, thieu


def du_dim_khach(nen, t, x0, y0, x1, y1, hang_x, hang_y, lo_phi=(), co_them=(), le=None, buoc=None, tren=False):
    """Quy tắc 12/11: bản vẽ phải có ĐỦ kích thước khách (dim_khach). So với dim đã vẽ (kể cả dim tổng sắp vẽ);
    thiếu dim ngang / dọc → thêm một hàng dim (dưới / phải, sau chuỗi dim); thiếu R có tâm → dim R; còn lại báo thiếu.
    co_them = [(loai, số)] đã thể hiện bằng nhãn (C / R góc, rộng × dài lỗ dài, bước P của chuỗi gộp) — coi là đủ.
    Trả (số hàng ngang, số hàng dọc đã dùng, [mô tả dim thiếu])."""
    import moi_truong as mt
    le, buoc = le or mt.LE_DIM, buoc or mt.BUOC_DIM
    co = dim_da_ve(nen) + [("ngang", x1 - x0), ("doc", y1 - y0)] + [("phi", d) for d in lo_phi] + list(co_them)  # Ø lỗ / nhãn: là đủ
    thieu = []
    for d in t.get("dim_khach", []):
        loai, so = d.get("loai", ""), float(d.get("gia_tri", 0) or 0)
        if so <= 0 or any(l == loai and abs(m - so) <= 0.05 for l, m in co):
            continue
        tu, den = float(d.get("tu", 0) or 0), float(d.get("den", 0) or 0)
        a, b = min(tu, den), max(tu, den)
        if loai == "ngang" and abs((b - a) - so) <= 0.5 and x0 - 0.01 <= a and b <= x1 + 0.01:
            nen.dim_ngang(a, b, y1 if tren else y0, (y1 + le + hang_x * buoc) if tren else (y0 - le - hang_x * buoc))
            hang_x += 1
        elif loai == "doc" and abs((b - a) - so) <= 0.5 and y0 - 0.01 <= a and b <= y1 + 0.01:
            nen.dim_doc(a, b, x1, x1 + le + hang_y * buoc)
            hang_y += 1
        elif loai == "r":
            # chỉ dim R khi bản vẽ thật sự có cung bán kính đó (gần tâm khách ghi nếu có) — không thì báo lệch, không dim sai
            cung = [e for e in nen.msp.query('ARC[layer=="%s"]' % mt.LOP_CAT) if abs(float(e.dxf.radius) - so) <= 0.05]
            if tu or den:
                cung.sort(key=lambda e: (e.dxf.center.x - tu) ** 2 + (e.dxf.center.y - den) ** 2)
            if not cung:
                thieu.append("R%g lệch — bản vẽ không có cung R%g (kiểm thông số mẫu)" % (so, so))
                continue
            c = cung[0].dxf.center
            d2 = nen.msp.add_radius_dim(center=(c.x, c.y), radius=so, angle=45, dimstyle=nen.kieu_dim, override=nen.ov(), dxfattribs={"layer": mt.LOP_DIM})
            d2.render()
        else:
            thieu.append(("R" if loai == "r" else "Ø" if loai == "phi" else "") + ("%g" % so) + ("°" if loai == "goc" else "") + " (" + loai + ")")
            continue
        co.append((loai, so))
    return hang_x, hang_y, thieu


def chuoi_khach(t, loai, a0, a1):
    """Dim ngang / doc của khách (bỏ dim tổng a0 → a1) → các chuỗi mốc nối liền (đầu đoạn sau = cuối đoạn trước), như khách dim từng nhóm.
    Chuỗi ngắn trước (sát tấm), dài sau (ngoài, sát dim tổng) — như bản khách 18/11.
    tai = toạ độ đường dim khách đặt (ngang: y, doc: x; có thể nằm TRONG hình) — đoạn cùng chuỗi phải cùng tai (±5). Trả [(mốc, tai | None)]."""
    doan = set()
    for d in t.get("dim_khach", []) or []:
        if not isinstance(d, dict) or str(d.get("loai") or "") != loai:
            continue
        a, b = sorted((float(d.get("tu") or 0), float(d.get("den") or 0)))
        if b - a <= 0.01 or (abs(a - a0) <= 0.5 and abs(b - a1) <= 0.5) or a < a0 - 0.5 or b > a1 + 0.5:
            continue
        tai = d.get("tai")
        doan.add((round(a, 3), round(b, 3), None if tai in (None, "") else round(float(tai), 1)))
    ds = []
    for a, b, tai in sorted(doan, key=lambda x: (x[0], x[1], -1e9 if x[2] is None else x[2])):
        c = next((c for c in ds if abs(c[0][-1] - a) <= 0.5 and ((c[1] is None and tai is None) or (c[1] is not None and tai is not None and abs(c[1] - tai) <= 5))), None)
        if c:
            c[0].append(b)
        else:
            ds.append([[a, b], tai])
    return [(c, tai) for c, tai in sorted(ds, key=lambda c: (c[0][-1] - c[0][0], c[0][0]))]


assert chuoi_khach({"dim_khach": [{"loai": "ngang", "tu": 0, "den": 635}, {"loai": "ngang", "tu": 0, "den": 10}, {"loai": "ngang", "tu": 10, "den": 160},
                                  {"loai": "ngang", "tu": 0, "den": 190, "tai": 20}, {"loai": "ngang", "tu": 190, "den": 310, "tai": 22}, {"loai": "doc", "tu": 0, "den": 9}]},
                   "ngang", 0, 635) == [([0, 10, 160], None), ([0, 190, 310], 20)]


NGAN_DIM = 25.0  # mm: dim ngắn hơn ở ĐẦU / CUỐI chuỗi thì chữ ra ngoài 2 bên (bản sửa 18/11: 10 sang trái, 15 sang phải)


def chu_ngoai(i, n, a, b, duong, doc, s=1.0):
    """Chỗ đặt chữ dim thứ i / n của chuỗi: đoạn đầu / cuối ngắn hơn NGAN_DIM → ra ngoài (trước a / sau b); còn lại None (giữa đường dim)."""
    if b - a >= NGAN_DIM * s or n < 2 or 0 < i < n - 1:
        return None
    v = a - NGAN_DIM * s if i == 0 else b + NGAN_DIM * s
    return (duong, v) if doc else (v, duong)


assert chu_ngoai(0, 6, 0, 10, 530, 0) == (-25.0, 530) and chu_ngoai(5, 6, 620, 635, 530, 0) == (660.0, 530) and chu_ngoai(2, 6, 160, 170, 530, 0) is None


def them_chung(nen, t, x0, y0, x1, y1, chi_dim=False, nhan_goc=()):
    """Phần chung mọi tấm theo bản chuẩn xưởng (12/11): ケガキ + leader, chuỗi dim chi tiết dưới / phải (1-SUNPOU),
    dim tổng X dưới cùng + Y bên phải (1-XY), F-name dưới hoặc phải tấm (bên nào Khung_IN nhỏ hơn), Khung_IN tỉ lệ khổ A ngang.
    Bản chuẩn 130996-3: khoảng dim theo cỡ tấm, gióng dim kéo từ tâm lỗ, chuỗi chỉ 1 mốc giữa thì dim mép → mốc (đoạn còn lại = tổng − đoạn đầu).
    18/11: khách có dim vị trí (dim_khach ngang / doc) → vẽ ĐÚNG các chuỗi đó, mỗi chuỗi một hàng (ngang ở TRÊN, dọc ở PHẢI như bản khách),
    không tự dựng chuỗi; chỉ thêm dim tổng (1-XY, ngoài cùng); nhãn leader xuống dưới tấm; đường tâm (CENTER) qua tâm tấm.
    Không có dim khách → tự dựng chuỗi từ mốc (dưới / phải) như trước.
    chi_dim: bỏ F-name + khung (ảnh hướng dẫn)."""
    import moi_truong as mt
    le, buoc = mt.khoang_dim(x1 - x0, y1 - y0)
    le, buoc = max(10.0, le * nen.ti_le), max(8.0, buoc * nen.ti_le)  # tỉ lệ dim theo cỡ tấm (18/11)
    ck_x, ck_y = chuoi_khach(t, "ngang", x0, x1), chuoi_khach(t, "doc", y0, y1)
    tren = bool(ck_x or ck_y)  # dim theo khách
    kg = sorted(([float(k[0]), float(k[1])] for k in t.get("ke_gaki", [])), key=lambda k: k[0])
    for kx, ky in kg:
        nen.ke_gaki(kx, ky)
    y_duoi = y0 - 10.0 * nen.ti_le if tren else None  # dim ở trên → nhãn leader xuống dưới
    if kg:
        gx, gy = kg[len(kg) // 2]
        yk = y1 + 53 * nen.ti_le if not tren else y_duoi - 53 * nen.ti_le
        nen.leader([(gx, gy), (gx + 23 * nen.ti_le, yk), (gx + 24 * nen.ti_le, yk)], "%d-ケガキ" % len(kg))
    lo_x, lo_y, lo_phi, thieu_lo = ve_lo(nen, t, y1, 1 if kg else 0, le, y_duoi)
    gion_x, gion_y = {}, {}  # mốc → điểm gióng thật (tâm lỗ gần mép dim nhất)
    for px, py in zip(lo_x, lo_y):
        gion_x[round(px, 3)] = (max if tren else min)(py, gion_x.get(round(px, 3), py))
        gion_y[round(py, 3)] = max(px, gion_y.get(round(py, 3), px))
    hang_x = hang_y = 0
    if tren:
        dx = [x for c, _ in ck_x for x in doan_gop(c)]
        dy = [x for c, _ in ck_y for x in doan_gop(c)]
        diem = list(zip(lo_x, lo_y))

        def gion(v, cot, dim):  # tâm lỗ ở mốc v gần đường dim nhất (cot 0: x → y, 1: y → x); không có → None (gióng từ mép)
            ds_ = [p[1 - cot] for p in diem if abs(p[cot] - v) <= 0.01]
            return min(ds_, key=lambda q: abs(q - dim)) if ds_ else None
        tren_max, phai_max = y1, x1  # đường dim khách đặt ngoài tấm → dim tổng phải ra ngoài nữa
        for c, tai in ck_x:  # có tai: đặt đúng chỗ khách (kể cả trong hình); không: hàng trên tấm
            yd = tai if tai is not None else y1 + le + hang_x * buoc
            yb = min(max(yd, y0), y1)
            dg = doan_gop(c)
            for i, (a, b, chu) in enumerate(dg):
                nen.dim_ngang(a, b, yb, yd, chu=chu, y_a=gion(a, 0, yd), y_b=gion(b, 0, yd), vi_tri_chu=chu_ngoai(i, len(dg), a, b, yd, 0, nen.ti_le))
            hang_x += tai is None
            tren_max = max(tren_max, yd)
        for c, tai in ck_y:
            xd = tai if tai is not None else x1 + le + hang_y * buoc
            xb = min(max(xd, x0), x1)
            dg = doan_gop(c)
            for i, (a, b, chu) in enumerate(dg):
                nen.dim_doc(a, b, xb, xd, chu=chu, x_a=gion(a, 1, xd), x_b=gion(b, 1, xd), vi_tri_chu=chu_ngoai(i, len(dg), a, b, xd, 1, nen.ti_le))
            hang_y += tai is None
            phai_max = max(phai_max, xd)
        # đường tâm qua tâm tấm — chỉ hướng có dim khách đo từ tâm (bản sửa 18/11: 190 | 190 từ y = 230 → chỉ đường tâm ngang)
        cx, cy, ra = (x0 + x1) / 2.0, (y0 + y1) / 2.0, 10.0
        if any(abs(m - cx) <= 0.5 for c, _ in ck_x for m in c[1:-1]):
            nen.duong_tam((cx, y0 - ra), (cx, y1 + ra))
        if any(abs(m - cy) <= 0.5 for c, _ in ck_y for m in c[1:-1]):
            nen.duong_tam((x0 - ra, cy), (x1 + ra, cy))
    else:
        mx = chuoi(list(t.get("be_x", [])) + [k[0] for k in kg] + nen.moc_x + lo_x, x0, x1)
        my = chuoi(list(t.get("be_y", [])) + nen.moc_y + lo_y, y0, y1)
        # chuỗi dim chi tiết (bước đều gộp "100P×5=500"); quá MOC_TOI_DA đoạn thì tách thêm hàng
        dx, dy = doan_gop(mx) if len(mx) > 2 else [], doan_gop(my) if len(my) > 2 else []
        dx, dy = dx[:1] if len(dx) == 2 else dx, dy[:1] if len(dy) == 2 else dy  # 1 mốc giữa: chỉ mép → mốc
        for i in range(0, len(dx), mt.MOC_TOI_DA):
            for a, b, chu in dx[i:i + mt.MOC_TOI_DA]:
                nen.dim_ngang(a, b, y0, y0 - le - hang_x * buoc, chu=chu, y_a=gion_x.get(round(a, 3)), y_b=gion_x.get(round(b, 3)))
            hang_x += 1
        for i in range(0, len(dy), mt.MOC_TOI_DA):
            for a, b, chu in dy[i:i + mt.MOC_TOI_DA]:
                nen.dim_doc(a, b, x1, x1 + le + hang_y * buoc, chu=chu, x_a=gion_y.get(round(a, 3)), x_b=gion_y.get(round(b, 3)))
            hang_y += 1
    # đã có trên nhãn: C góc (2 cạnh), R góc, rộng × dài lỗ dài, bước P
    co_them = [(l, v) for k, v in nhan_goc for l in (("r",) if k == "R" else ("ngang", "doc"))]
    for g in t.get("lo", []) or []:
        if isinstance(g, dict) and str(g.get("loai") or "") == "dai":
            co_them += [(l, float(g.get(k) or 0)) for k in ("rong", "dai") for l in ("ngang", "doc")]
    co_them += [("ngang", (b - a) / int(chu.split("×")[1].split("=")[0])) for a, b, chu in dx if chu != "<>"]
    co_them += [("doc", (b - a) / int(chu.split("×")[1].split("=")[0])) for a, b, chu in dy if chu != "<>"]
    hx, hy, thieu = du_dim_khach(nen, t, x0, y0, x1, y1, hang_x, hang_y, lo_phi, co_them, le, buoc, tren)
    thieu = thieu_lo + thieu
    y_tong = max(y1 + le + hx * buoc, tren_max + buoc) if tren else (y0 - le - hx * buoc)  # dim tổng luôn ngoài cùng
    nen.dim_ngang(x0, x1, y1 if tren else y0, y_tong, lop=mt.LOP_XY)
    nen.dim_doc(y0, y1, x1, max(x1 + le + hy * buoc, phai_max + buoc) if tren else x1 + le + hy * buoc, lop=mt.LOP_XY)
    if chi_dim:
        return thieu
    # F-name: thử dưới tấm (bản chuẩn 12/11) và bên phải (bản chuẩn 130996-3) — giữ chỗ cho Khung_IN nhỏ hơn
    _, day, phai, _ = mt.bao_tat_ca(nen)
    cho = [(x0 + mt.LECH_FNAME, (day - 40.0 - mt.CAO_FNAME) if tren else (y_tong - mt.DUOI_FNAME)), (phai + mt.DACH_FNAME, y1 - 6.0)]
    tot = None
    for fx, fy in cho:
        nen.fname(fx, fy, t.get("fname", []))
        bx0, by0, bx1, by1 = mt.bao_tat_ca(nen)
        k = mt.khung_a(bx0 - mt.LE_KHUNG, by0 - mt.LE_KHUNG, bx1 + mt.LE_KHUNG, by1 + mt.LE_KHUNG)
        if tot is None or (k[2] - k[0]) < (tot[1][2] - tot[1][0]) - 0.01:
            tot = ((fx, fy), k)
        for e in list(nen.msp.query('TEXT[layer=="%s"]' % mt.LOP_FNAME)):
            nen.msp.delete_entity(e)
    nen.fname(tot[0][0], tot[0][1], t.get("fname", []))
    nen.khung_in(*tot[1])
    return thieu


def ve_tam(viec, t, mau, so_tay, co_anh):
    import moi_truong as mt
    ten = t.get("bien_dang", "")
    if ten not in mau:
        raise ValueError("chưa có mẫu biên dạng '%s' trong thư viện" % ten)
    nen = mt.NenVe(viec["moi_truong"])
    mod = nap(mau[ten])
    thieu_ts = [ts[0] for ts in tham_so_mau(mod) if ts[2] is None and ts[0] not in (t.get("tham_so") or {})]
    if thieu_ts:
        raise ValueError("thiếu thông số mẫu '%s': %s — nhập ở ✎ Sửa thông số → Thông số mẫu" % (ten, ", ".join(thieu_ts)))
    nen.dat_ti_le(float(t.get("x") or 0), float(t.get("y") or 0))  # tỉ lệ dim theo cỡ tấm — trước khi mẫu vẽ dim riêng
    mod.ve(nen, t)
    import goc_khoet
    _, nhan_goc, canh_bao = goc_khoet.ap_dung(nen, t, lo_tron(t))   # vát / bo theo đỉnh + khoét trong (chung mọi mẫu)
    x0, y0, x1, y1 = mt.khung_bao(nen)
    if x1 <= x0 or y1 <= y0:
        raise ValueError("mẫu '%s' không vẽ viền cắt nào" % ten)
    thieu = them_chung(nen, t, x0, y0, x1, y1, chi_dim=bool(viec.get("chi_hinh")), nhan_goc=nhan_goc)  # chi_hinh: 🧪 Thử nhanh, không F-name / Khung_IN
    tep = ten_ra(viec["thu_muc_ra"], t["bango"], so_tay)
    nen.luu(tep)
    so_tay[os.path.basename(tep)] = ma_bam(tep)
    anh = None
    if co_anh:
        try:
            from ezdxf.addons.drawing import matplotlib as dm
            anh = os.path.splitext(tep)[0] + ".png"
            dm.qsave(nen.msp, anh, bg="#FFFFFF", size_inches=(6, 4), dpi=110)
        except Exception:  # ảnh xem trước hỏng không làm hỏng bản vẽ
            anh = None
    return tep, anh, thieu, canh_bao


LE_ANH = 0.03       # nới tối thiểu 3% trang mỗi bên
LE_ANH_VUNG = 0.3   # và 30% kích thước vùng mỗi bên — 100% (13/11) làm tấm nhỏ xíu, AI phóng to sót góc vát không nhãn (test2)
DPI_ANH = 250       # đủ nét cho lượt AI đọc phóng to


def cat_anh(tep_viec):
    """ANH.json: [{"nguon": pdf / dxf, "trang": 1, "vung": {"x0", "y0", "x1", "y1"} tỉ lệ 0–1 gốc trái trên (rỗng = cả trang), "ra": png}]."""
    with open(tep_viec, encoding="utf-8") as f:
        ds = json.load(f)
    kq = []
    for v in ds:
        try:
            vg = v.get("vung") or {}
            hop = None
            if vg:
                x0, y0, x1, y1 = float(vg["x0"]), float(vg["y0"]), float(vg["x1"]), float(vg["y1"])
                lx, ly = max(LE_ANH, (x1 - x0) * LE_ANH_VUNG), max(LE_ANH, (y1 - y0) * LE_ANH_VUNG)
                hop = (max(0.0, x0 - lx), max(0.0, y0 - ly), min(1.0, x1 + lx), min(1.0, y1 + ly))
            if v["nguon"].lower().endswith(".pdf"):
                import fitz
                with fitz.open(v["nguon"]) as d:
                    trang = d[min(max(int(v.get("trang") or 1), 1), d.page_count) - 1]
                    r = trang.rect
                    vung = r if hop is None else fitz.Rect(r.x0 + r.width * hop[0], r.y0 + r.height * hop[1], r.x0 + r.width * hop[2], r.y0 + r.height * hop[3])
                    trang.get_pixmap(dpi=DPI_ANH, clip=vung).save(v["ra"])
            else:
                import ezdxf
                from ezdxf.addons.drawing import matplotlib as dm
                dm.qsave(ezdxf.readfile(v["nguon"]).modelspace(), v["ra"], bg="#FFFFFF", dpi=150)
                if hop is not None:
                    from PIL import Image
                    with Image.open(v["ra"]) as a:
                        w, h = a.size
                        a.crop((int(w * hop[0]), int(h * hop[1]), int(w * hop[2]), int(h * hop[3]))).save(v["ra"])
            kq.append({"ra": v["ra"], "loi": None})
        except Exception as ex:
            kq.append({"ra": v.get("ra"), "loi": "%s: %s" % (type(ex).__name__, ex)})
    return kq


def lo_tron(t):
    """Lỗ của tấm → [(x, y, bán kính, nhãn)] để kiểm khoét đè lỗ."""
    kq = []
    for g in t.get("lo", []) or []:
        if not isinstance(g, dict):
            continue
        taro = str(g.get("taro") or "").strip().upper()
        co = "M" + taro.lstrip("M").split("X")[0].split("×")[0].split("-")[0].strip() if taro else ""
        dai = str(g.get("loai") or "") == "dai" and float(g.get("dai") or 0) > float(g.get("rong") or 0) > 0
        d = float(g["dai"]) if dai else TARO_MOI.get(co, float(g.get("d") or 0)) if taro else float(g.get("d") or 0)
        nhan = co or ("%g×%g" % (float(g["rong"]), float(g["dai"])) if dai else "Ø%g" % d)
        for p in g.get("vi_tri") or []:
            if isinstance(p, dict) and p.get("x") is not None and p.get("y") is not None:
                kq.append((float(p["x"]), float(p["y"]), d / 2.0, nhan))
    return kq


def so_do_viec(tep_viec):
    """VIEC.json như khi vẽ; mỗi tấm → ảnh sơ đồ đỉnh t["anh_dinh"] (mặc định <thu_muc_ra>/<bango>/<bango>-dinh.png — thư mục hồ sơ của tấm, 18/11)."""
    import goc_khoet
    import moi_truong as mt
    with open(tep_viec, encoding="utf-8") as f:
        viec = json.load(f)
    mau = cac_mau(viec.get("thu_vien", []))
    kq = []
    for t in viec["tam"]:
        ra = t.get("anh_dinh") or os.path.join(viec["thu_muc_ra"], t.get("bango", "tam"), t.get("bango", "tam") + "-dinh.png")
        try:
            if t.get("bien_dang", "") not in mau:
                raise ValueError("chưa có mẫu biên dạng '%s'" % t.get("bien_dang", ""))
            nen = mt.NenVe(viec["moi_truong"])
            nap(mau[t["bien_dang"]]).ve(nen, t)
            os.makedirs(os.path.dirname(ra) or ".", exist_ok=True)
            kq.append({"bango": t.get("bango"), "anh": ra, "so_dinh": goc_khoet.so_do(nen, t, ra), "loi": None})
        except Exception as ex:
            kq.append({"bango": t.get("bango"), "anh": None, "so_dinh": 0, "loi": "%s: %s" % (type(ex).__name__, ex)})
    return kq


def ve_viec(tep_viec):
    with open(tep_viec, encoding="utf-8") as f:
        viec = json.load(f)
    os.makedirs(viec["thu_muc_ra"], exist_ok=True)
    tep_so = os.path.join(viec["thu_muc_ra"], ".ve_dxf.json")
    so_tay = {}
    if os.path.exists(tep_so):
        try:
            with open(tep_so, encoding="utf-8") as f:
                so_tay = json.load(f)
        except ValueError:
            so_tay = {}
    mau = cac_mau(viec.get("thu_vien", []))
    co_anh = kiem()["matplotlib"]
    kq = []
    for t in viec["tam"]:
        try:
            tep, anh, thieu, canh_bao = ve_tam(viec, t, mau, so_tay, co_anh)
            kq.append({"bango": t["bango"], "tep": tep, "anh": anh, "loi": None, "thieu_dim": thieu, "canh_bao": canh_bao})
        except Exception as ex:
            kq.append({"bango": t.get("bango", "?"), "tep": None, "anh": None,
                       "loi": "%s: %s" % (type(ex).__name__, ex), "chi_tiet": traceback.format_exc(limit=3)})
    with open(tep_so, "w", encoding="utf-8") as f:
        json.dump(so_tay, f, ensure_ascii=False, indent=1)
    return {"ket_qua": kq}


def main(argv):
    if len(argv) > 1 and argv[1] == "--kiem":
        in_json(kiem())
    elif len(argv) > 2 and argv[1] == "--so-do":
        in_json(so_do_viec(argv[2]))
    elif len(argv) > 2 and argv[1] == "--cat-anh":
        in_json(cat_anh(argv[2]))
    elif len(argv) > 1 and argv[1] == "--thu-vien":
        ds = []
        for ten, tep in sorted(cac_mau(argv[2:]).items(), key=lambda x: (nhom_mau(x[1], argv[2:]), x[0])):
            anh = os.path.splitext(tep)[0] + ".png"
            nhom = nhom_mau(tep, argv[2:])
            try:
                mod = nap(tep)
                ds.append({"ten": ten, "nhom": nhom, "mo_ta": getattr(mod, "MO_TA", ""), "tep": tep, "tham_so": [list(x) for x in tham_so_mau(mod)],
                           "vi_du": getattr(mod, "VI_DU", None) or {}, "anh": anh if os.path.exists(anh) else None})
            except Exception as ex:
                ds.append({"ten": ten, "nhom": nhom, "mo_ta": "", "tep": tep, "loi": str(ex), "tham_so": [], "vi_du": {}, "anh": None})
        in_json(ds)
    elif len(argv) > 3 and argv[1] == "--huong-dan":
        in_json(anh_huong_dan(argv[2], argv[3]))
    elif len(argv) > 1:
        in_json(ve_viec(argv[1]))
    else:
        print(__doc__)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
