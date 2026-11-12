"""Vẽ bản trải phẳng phòng 4 (OhtaTeams) bằng ezdxf trên môi trường moi-truong-ve.dxf.

    python ve.py --kiem                 → {"python", "ezdxf", "matplotlib"}  (ezdxf thiếu → "ezdxf": null)
    python ve.py --thu-vien DIR [DIR…]  → [{"ten", "mo_ta", "tep"}]  mẫu biên dạng (thư mục sau đè thư mục trước)
    python ve.py VIEC.json              → {"ket_qua": [{"bango", "tep", "anh", "loi", "thieu_dim"}]}

VIEC.json (OhtaTeams ghi): {"moi_truong": dxf, "thu_muc_ra": dir, "thu_vien": [dir…],
  "tam": [{"bango", "bien_dang", "x", "y", "be_x", "be_y", "doan_x", "doan_y", "ke_gaki": [[x, y]…], "dim_khach": [{"loai", "gia_tri", "tu", "den"}], "fname": [dòng], "ghi_chu": [dòng]}]}
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


def in_json(o):
    sys.stdout.buffer.write(json.dumps(o, ensure_ascii=False).encode("utf-8"))
    sys.stdout.buffer.write(b"\n")


def kiem():
    kq = {"python": sys.executable, "ezdxf": None, "matplotlib": False}
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


def du_dim_khach(nen, t, x0, y0, x1, y1, hang_x, hang_y):
    """Quy tắc 12/11: bản vẽ phải có ĐỦ kích thước khách (dim_khach). So với dim đã vẽ (kể cả dim tổng sắp vẽ);
    thiếu dim ngang / dọc → thêm một hàng dim (dưới / phải, sau chuỗi dim); thiếu R có tâm → dim R; còn lại báo thiếu.
    Trả (số hàng ngang, số hàng dọc đã dùng, [mô tả dim thiếu])."""
    import moi_truong as mt
    co = dim_da_ve(nen) + [("ngang", x1 - x0), ("doc", y1 - y0)]
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
        elif loai == "r" and (tu or den):
            d2 = nen.msp.add_radius_dim(center=(tu, den), radius=so, angle=45, dimstyle=nen.kieu_dim, dxfattribs={"layer": mt.LOP_DIM})
            d2.render()
        else:
            thieu.append(("R" if loai == "r" else "Ø" if loai == "phi" else "") + ("%g" % so) + ("°" if loai == "goc" else "") + " (" + loai + ")")
            continue
        co.append((loai, so))
    return hang_x, hang_y, thieu


def them_chung(nen, t, x0, y0, x1, y1):
    """Phần chung mọi tấm theo bản chuẩn xưởng (12/11): ケガキ + leader, chuỗi dim chi tiết dưới / phải (1-SUNPOU),
    dim tổng X dưới cùng + Y bên phải (1-XY), F-name dưới tấm, Khung_IN tỉ lệ khổ A ngang."""
    import moi_truong as mt
    kg = sorted(([float(k[0]), float(k[1])] for k in t.get("ke_gaki", [])), key=lambda k: k[0])
    for kx, ky in kg:
        nen.ke_gaki(kx, ky)
    if kg:
        gx, gy = kg[len(kg) // 2]
        nen.leader([(gx, gy), (gx + 23, y1 + 53), (gx + 24, y1 + 53)], "%d-ケガキ" % len(kg))
    mx = chuoi(list(t.get("be_x", [])) + [k[0] for k in kg] + nen.moc_x, x0, x1)
    my = chuoi(list(t.get("be_y", [])) + nen.moc_y, y0, y1)
    if len(mx) > 2:
        for a, b in zip(mx, mx[1:]):
            nen.dim_ngang(a, b, y0, y0 - mt.LE_DIM)
    if len(my) > 2:
        for a, b in zip(my, my[1:]):
            nen.dim_doc(a, b, x1, x1 + mt.LE_DIM)
    hx, hy, thieu = du_dim_khach(nen, t, x0, y0, x1, y1, 1 if len(mx) > 2 else 0, 1 if len(my) > 2 else 0)
    y_tong = y0 - mt.LE_DIM - hx * mt.BUOC_DIM  # dim tổng luôn ngoài cùng
    nen.dim_ngang(x0, x1, y0, y_tong, lop=mt.LOP_XY)
    nen.dim_doc(y0, y1, x1, x1 + mt.LE_DIM + hy * mt.BUOC_DIM, lop=mt.LOP_XY)
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
    nap(mau[ten]).ve(nen, t)
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
    elif len(argv) > 1 and argv[1] == "--thu-vien":
        ds = []
        for ten, tep in sorted(cac_mau(argv[2:]).items()):
            try:
                ds.append({"ten": ten, "mo_ta": getattr(nap(tep), "MO_TA", ""), "tep": tep})
            except Exception as ex:
                ds.append({"ten": ten, "mo_ta": "", "tep": tep, "loi": str(ex)})
        in_json(ds)
    elif len(argv) > 1:
        in_json(ve_viec(argv[1]))
    else:
        print(__doc__)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
