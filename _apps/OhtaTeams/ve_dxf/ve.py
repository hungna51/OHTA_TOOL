"""Vẽ bản trải phẳng phòng 4 (OhtaTeams) bằng ezdxf trên môi trường moi-truong-ve.dxf.

    python ve.py --kiem                 → {"python", "ezdxf", "matplotlib"}  (ezdxf thiếu → "ezdxf": null)
    python ve.py --thu-vien DIR [DIR…]  → [{"ten", "mo_ta", "tep", "tham_so": [[tên, ý nghĩa, mặc định]], "vi_du", "anh"}]
                                          mẫu biên dạng (thư mục sau đè thư mục trước); anh = <ten>.png hướng dẫn nếu có
    python ve.py --huong-dan MOI_TRUONG DIR → [{"ten", "anh", "loi"}]  vẽ ảnh hướng dẫn <ten>.png cho mọi mẫu trong DIR
    python ve.py --cat-anh ANH.json     → [{"ra", "loi"}]  cắt ảnh tấm từ bản vẽ khách (PDF: PyMuPDF; DXF: ezdxf)
    python ve.py VIEC.json              → {"ket_qua": [{"bango", "tep", "anh", "loi", "thieu_dim"}]}

VIEC.json (OhtaTeams ghi): {"moi_truong": dxf, "thu_muc_ra": dir, "thu_vien": [dir…],
  "tam": [{"bango", "bien_dang", "x", "y", "tham_so": {tên: số}, "be_x", "be_y", "doan_x", "doan_y", "ke_gaki": [[x, y]…], "dim_khach": [{"loai", "gia_tri", "tu", "den"}], "fname": [dòng], "ghi_chu": [dòng]}]}
Mẫu biên dạng (thu_vien/<ten>.py): MO_TA + ve(nen, t) vẽ viền / đường bẻ / dim riêng của hình; ケガキ, chuỗi dim dưới / phải,
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
    """ten → tệp .py; thư mục sau đè thư mục trước (mẫu vẽ thử của đơn đè mẫu chung cùng tên)."""
    mau = {}
    for d in thu_muc:
        if d and os.path.isdir(d):
            for f in sorted(os.listdir(d)):
                if f.endswith(".py") and not f.startswith("_"):
                    mau[f[:-3]] = os.path.join(d, f)
    return mau


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
            anh = os.path.join(thu_muc, ten + ".png")
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


def ve_lo(nen, t, y1, nhan_dau):
    """Lỗ (12/11, phần chung mọi mẫu): mỗi nhóm lỗ của bảng tấm {d, so_luong, taro, sara, d_sara, loai tron / dai, rong, dai, goc,
    vi_tri: [{x, y}]} — tâm trên hình trải phẳng, gốc trái dưới. Lỗ tròn: CIRCLE 0-KAKOU (taro: Ø mồi theo bảng TMT); lỗ dài:
    2 cạnh + 2 cung thật. Mỗi nhóm một leader nhãn "n-Ø10" / "n-M6" / "n-皿Ø6.6 (Ø12)" / "n-10×20".
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
        nen.leader([(gx, gy), (gx + 23, y1 + 53 + k * 26), (gx + 24, y1 + 53 + k * 26)], "%d-%s" % (len(pts), nhan))
        k += 1
    return xs, ys, phi, thieu


def du_dim_khach(nen, t, x0, y0, x1, y1, hang_x, hang_y, lo_phi=()):
    """Quy tắc 12/11: bản vẽ phải có ĐỦ kích thước khách (dim_khach). So với dim đã vẽ (kể cả dim tổng sắp vẽ);
    thiếu dim ngang / dọc → thêm một hàng dim (dưới / phải, sau chuỗi dim); thiếu R có tâm → dim R; còn lại báo thiếu.
    Trả (số hàng ngang, số hàng dọc đã dùng, [mô tả dim thiếu])."""
    import moi_truong as mt
    co = dim_da_ve(nen) + [("ngang", x1 - x0), ("doc", y1 - y0)] + [("phi", d) for d in lo_phi]  # Ø lỗ: nhãn nhóm lỗ là đủ
    thieu = []
    for d in t.get("dim_khach", []):
        loai, so = d.get("loai", ""), float(d.get("gia_tri", 0) or 0)
        if so <= 0 or any(l == loai and abs(m - so) <= 0.05 for l, m in co):
            continue
        tu, den = float(d.get("tu", 0) or 0), float(d.get("den", 0) or 0)
        a, b = min(tu, den), max(tu, den)
        if loai == "ngang" and abs((b - a) - so) <= 0.5 and x0 - 0.01 <= a and b <= x1 + 0.01:
            nen.dim_ngang(a, b, y0, y0 - mt.LE_DIM - hang_x * mt.BUOC_DIM)
            hang_x += 1
        elif loai == "doc" and abs((b - a) - so) <= 0.5 and y0 - 0.01 <= a and b <= y1 + 0.01:
            nen.dim_doc(a, b, x1, x1 + mt.LE_DIM + hang_y * mt.BUOC_DIM)
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


def them_chung(nen, t, x0, y0, x1, y1, chi_dim=False):
    """Phần chung mọi tấm theo bản chuẩn xưởng (12/11): ケガキ + leader, chuỗi dim chi tiết dưới / phải (1-SUNPOU),
    dim tổng X dưới cùng + Y bên phải (1-XY), F-name dưới tấm, Khung_IN tỉ lệ khổ A ngang. chi_dim: bỏ F-name + khung (ảnh hướng dẫn)."""
    import moi_truong as mt
    kg = sorted(([float(k[0]), float(k[1])] for k in t.get("ke_gaki", [])), key=lambda k: k[0])
    for kx, ky in kg:
        nen.ke_gaki(kx, ky)
    if kg:
        gx, gy = kg[len(kg) // 2]
        nen.leader([(gx, gy), (gx + 23, y1 + 53), (gx + 24, y1 + 53)], "%d-ケガキ" % len(kg))
    lo_x, lo_y, lo_phi, thieu_lo = ve_lo(nen, t, y1, 1 if kg else 0)
    mx = chuoi(list(t.get("be_x", [])) + [k[0] for k in kg] + nen.moc_x + lo_x, x0, x1)
    my = chuoi(list(t.get("be_y", [])) + nen.moc_y + lo_y, y0, y1)
    hang_x = hang_y = 0
    # chuỗi dim chi tiết; quá MOC_TOI_DA đoạn thì tách thêm hàng
    for i in range(0, len(mx) - 1 if len(mx) > 2 else 0, mt.MOC_TOI_DA):
        for a, b in zip(mx[i:i + mt.MOC_TOI_DA + 1], mx[i + 1:i + mt.MOC_TOI_DA + 1]):
            nen.dim_ngang(a, b, y0, y0 - mt.LE_DIM - hang_x * mt.BUOC_DIM)
        hang_x += 1
    for i in range(0, len(my) - 1 if len(my) > 2 else 0, mt.MOC_TOI_DA):
        for a, b in zip(my[i:i + mt.MOC_TOI_DA + 1], my[i + 1:i + mt.MOC_TOI_DA + 1]):
            nen.dim_doc(a, b, x1, x1 + mt.LE_DIM + hang_y * mt.BUOC_DIM)
        hang_y += 1
    hx, hy, thieu = du_dim_khach(nen, t, x0, y0, x1, y1, hang_x, hang_y, lo_phi)
    thieu = thieu_lo + thieu
    y_tong = y0 - mt.LE_DIM - hx * mt.BUOC_DIM  # dim tổng luôn ngoài cùng
    nen.dim_ngang(x0, x1, y0, y_tong, lop=mt.LOP_XY)
    nen.dim_doc(y0, y1, x1, x1 + mt.LE_DIM + hy * mt.BUOC_DIM, lop=mt.LOP_XY)
    if chi_dim:
        return thieu
    nen.fname(x0 + mt.LECH_FNAME, y_tong - mt.DUOI_FNAME, t.get("fname", []))
    bx0, by0, bx1, by1 = mt.bao_tat_ca(nen)
    nen.khung_in(*mt.khung_a(bx0 - mt.LE_KHUNG, by0 - mt.LE_KHUNG, bx1 + mt.LE_KHUNG, by1 + mt.LE_KHUNG))
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
    x0, y0, x1, y1 = mt.khung_bao(nen)
    if x1 <= x0 or y1 <= y0:
        raise ValueError("mẫu '%s' không vẽ viền cắt nào" % ten)
    thieu = them_chung(nen, t, x0, y0, x1, y1)
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
    return tep, anh, thieu


LE_ANH = 0.03       # nới tối thiểu 3% trang mỗi bên
LE_ANH_VUNG = 1.0   # và 100% kích thước vùng mỗi bên (ảnh gấp 3 vùng) — vùng AI chọn hay lệch (vd ôm ô ghi chú, sót khung hình mũi tên chỉ tới)
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
            tep, anh, thieu = ve_tam(viec, t, mau, so_tay, co_anh)
            kq.append({"bango": t["bango"], "tep": tep, "anh": anh, "loi": None, "thieu_dim": thieu})
        except Exception as ex:
            kq.append({"bango": t.get("bango", "?"), "tep": None, "anh": None,
                       "loi": "%s: %s" % (type(ex).__name__, ex), "chi_tiet": traceback.format_exc(limit=3)})
    with open(tep_so, "w", encoding="utf-8") as f:
        json.dump(so_tay, f, ensure_ascii=False, indent=1)
    return {"ket_qua": kq}


def main(argv):
    if len(argv) > 1 and argv[1] == "--kiem":
        in_json(kiem())
    elif len(argv) > 2 and argv[1] == "--cat-anh":
        in_json(cat_anh(argv[2]))
    elif len(argv) > 1 and argv[1] == "--thu-vien":
        ds = []
        for ten, tep in sorted(cac_mau(argv[2:]).items()):
            anh = os.path.splitext(tep)[0] + ".png"
            try:
                mod = nap(tep)
                ds.append({"ten": ten, "mo_ta": getattr(mod, "MO_TA", ""), "tep": tep, "tham_so": [list(x) for x in tham_so_mau(mod)],
                           "vi_du": getattr(mod, "VI_DU", None) or {}, "anh": anh if os.path.exists(anh) else None})
            except Exception as ex:
                ds.append({"ten": ten, "mo_ta": "", "tep": tep, "loi": str(ex), "tham_so": [], "vi_du": {}, "anh": None})
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
