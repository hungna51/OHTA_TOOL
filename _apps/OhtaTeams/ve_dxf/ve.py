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


def ve_lo(nen, t, y1, nhan_dau, le=None):
    """Lỗ (12/11, phần chung mọi mẫu): mỗi nhóm lỗ của bảng tấm {d, so_luong, taro, sara, d_sara, loai tron / dai, rong, dai, goc,
    vi_tri: [{x, y}]} — tâm trên hình trải phẳng, gốc trái dưới. Lỗ tròn: CIRCLE 0-KAKOU (taro: Ø mồi theo bảng TMT); lỗ dài:
    2 cạnh + 2 cung thật. Mỗi nhóm một leader nhãn "n-Ø10" / "n-M6" / "n-皿Ø6.6 (Ø12)" / "n-10×20";
    nhóm chỉ MỘT lỗ tròn thường → dim đường kính Ø thay leader (bản chuẩn 130996-3).
    Trả (tâm x, tâm y cho chuỗi dim, [Ø đã ghi nhãn], [báo thiếu])."""
    import math
    xs, ys, phi, thieu = [], [], [], []
    k = nhan_dau
    for g in t.get("lo", []) or []:
        if not isinstance(g, dict):
            continue
        d = float(g.get("d") or 0)
        taro = str(g.get("taro") or "").strip().upper()
        co = "M" + taro.lstrip("M").split("X")[0].split("×")[0].split("-")[0].strip() if taro else ""
        dai = str(g.get("loai") or "") == "dai" and float(g.get("dai") or 0) > float(g.get("rong") or 0) > 0
        pts = [(float(p["x"]), float(p["y"])) for p in (g.get("vi_tri") or []) if isinstance(p, dict) and p.get("x") is not None and p.get("y") is not None]
        if dai:
            nhan = "%g×%g" % (float(g["rong"]), float(g["dai"]))
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
            else:
                dv = TARO_MOI.get(co, d) if taro else d
                if dv <= 0:
                    thieu.append("lỗ %s: chưa có Ø" % nhan)
                    break
                nen.lo_tron(px, py, dv)
            xs.append(px)
            ys.append(py)
        if taro and co not in TARO_MOI:
            thieu.append("taro %s: ngoài bảng Ø mồi TMT — vẽ theo Ø %g, kiểm lại" % (co, d))
        if not taro and not dai and d > 0:
            phi.append(d)
        gx, gy = pts[0]
        if len(pts) == 1 and not taro and not dai and not g.get("sara") and d > 0:
            nen.dim_phi(gx, gy, d, le=le or 80.0)
            continue
        nen.leader([(gx, gy), (gx + 23, y1 + 53 + k * 26), (gx + 24, y1 + 53 + k * 26)], "%d-%s" % (len(pts), nhan))
        k += 1
    return xs, ys, phi, thieu


def du_dim_khach(nen, t, x0, y0, x1, y1, hang_x, hang_y, lo_phi=(), co_them=(), le=None, buoc=None):
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
            nen.dim_ngang(a, b, y0, y0 - le - hang_x * buoc)
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
            d2 = nen.msp.add_radius_dim(center=(c.x, c.y), radius=so, angle=45, dimstyle=nen.kieu_dim, dxfattribs={"layer": mt.LOP_DIM})
            d2.render()
        else:
            thieu.append(("R" if loai == "r" else "Ø" if loai == "phi" else "") + ("%g" % so) + ("°" if loai == "goc" else "") + " (" + loai + ")")
            continue
        co.append((loai, so))
    return hang_x, hang_y, thieu


def them_chung(nen, t, x0, y0, x1, y1, chi_dim=False, nhan_goc=()):
    """Phần chung mọi tấm theo bản chuẩn xưởng (12/11): ケガキ + leader, chuỗi dim chi tiết dưới / phải (1-SUNPOU),
    dim tổng X dưới cùng + Y bên phải (1-XY), F-name dưới hoặc phải tấm (bên nào Khung_IN nhỏ hơn), Khung_IN tỉ lệ khổ A ngang.
    Bản chuẩn 130996-3: khoảng dim theo cỡ tấm, gióng dim kéo từ tâm lỗ, chuỗi chỉ 1 mốc giữa thì dim mép → mốc (đoạn còn lại = tổng − đoạn đầu).
    chi_dim: bỏ F-name + khung (ảnh hướng dẫn)."""
    import moi_truong as mt
    le, buoc = mt.khoang_dim(x1 - x0, y1 - y0)
    kg = sorted(([float(k[0]), float(k[1])] for k in t.get("ke_gaki", [])), key=lambda k: k[0])
    for kx, ky in kg:
        nen.ke_gaki(kx, ky)
    if kg:
        gx, gy = kg[len(kg) // 2]
        nen.leader([(gx, gy), (gx + 23, y1 + 53), (gx + 24, y1 + 53)], "%d-ケガキ" % len(kg))
    lo_x, lo_y, lo_phi, thieu_lo = ve_lo(nen, t, y1, 1 if kg else 0, le)
    gion_x, gion_y = {}, {}  # mốc → điểm gióng thật (tâm lỗ gần mép dim nhất)
    for px, py in zip(lo_x, lo_y):
        gion_x[round(px, 3)] = min(py, gion_x.get(round(px, 3), py))
        gion_y[round(py, 3)] = max(px, gion_y.get(round(py, 3), px))
    mx = chuoi(list(t.get("be_x", [])) + [k[0] for k in kg] + nen.moc_x + lo_x, x0, x1)
    my = chuoi(list(t.get("be_y", [])) + nen.moc_y + lo_y, y0, y1)
    hang_x = hang_y = 0
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
    hx, hy, thieu = du_dim_khach(nen, t, x0, y0, x1, y1, hang_x, hang_y, lo_phi, co_them, le, buoc)
    thieu = thieu_lo + thieu
    y_tong = y0 - le - hx * buoc  # dim tổng luôn ngoài cùng
    nen.dim_ngang(x0, x1, y0, y_tong, lop=mt.LOP_XY)
    nen.dim_doc(y0, y1, x1, x1 + le + hy * buoc, lop=mt.LOP_XY)
    if chi_dim:
        return thieu
    # F-name: thử dưới tấm (bản chuẩn 12/11) và bên phải (bản chuẩn 130996-3) — giữ chỗ cho Khung_IN nhỏ hơn
    _, _, phai, _ = mt.bao_tat_ca(nen)
    cho = [(x0 + mt.LECH_FNAME, y_tong - mt.DUOI_FNAME), (phai + mt.DACH_FNAME, y1 - 6.0)]
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
    """VIEC.json như khi vẽ; mỗi tấm → ảnh sơ đồ đỉnh t["anh_dinh"] (mặc định <thu_muc_ra>/<bango>-dinh.png)."""
    import goc_khoet
    import moi_truong as mt
    with open(tep_viec, encoding="utf-8") as f:
        viec = json.load(f)
    mau = cac_mau(viec.get("thu_vien", []))
    kq = []
    for t in viec["tam"]:
        ra = t.get("anh_dinh") or os.path.join(viec["thu_muc_ra"], t.get("bango", "tam") + "-dinh.png")
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
