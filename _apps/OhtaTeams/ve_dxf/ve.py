"""Vẽ bản trải phẳng phòng 4 (OhtaTeams) bằng ezdxf trên môi trường moi-truong-ve.dxf.

    python ve.py --kiem                 → {"python", "ezdxf", "matplotlib"}  (ezdxf thiếu → "ezdxf": null)
    python ve.py --thu-vien DIR [DIR…]  → [{"ten", "mo_ta", "tep"}]  mẫu biên dạng (thư mục sau đè thư mục trước)
    python ve.py VIEC.json              → {"ket_qua": [{"bango", "tep", "anh", "loi"}]}

VIEC.json (OhtaTeams ghi): {"moi_truong": dxf, "thu_muc_ra": dir, "thu_vien": [dir…],
  "tam": [{"bango", "bien_dang", "x", "y", "be_x", "be_y", "doan_x", "doan_y", "fname": [dòng], "ghi_chu": [dòng]}]}
Mẫu biên dạng (thu_vien/<ten>.py): MO_TA + ve(nen, t) vẽ viền / đường bẻ / dim riêng; dim tổng, F-name, Khung_IN thêm ở đây.
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
    # dim tổng (dưới, trái), F-name bên phải, Khung_IN bao hết
    nen.dim_ngang(x0, x1, y0, y0 - mt.LE_DIM)
    nen.dim_doc(y0, y1, x0, x0 - mt.LE_DIM)
    fx = x1 + mt.LE_DIM * 2.5
    dong = t.get("fname", [])
    rong = nen.fname(fx, y1, dong)
    cao_fname = len(dong) * mt.CAO_FNAME * 2.0
    nen.khung_in(x0 - mt.LE_DIM - mt.LE_KHUNG, min(y0, y1 - cao_fname) - mt.LE_DIM - mt.LE_KHUNG,
                 fx + rong + mt.LE_KHUNG, y1 + mt.LE_DIM * 1.5 + mt.LE_KHUNG)
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
    return tep, anh


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
            tep, anh = ve_tam(viec, t, mau, so_tay, co_anh)
            kq.append({"bango": t["bango"], "tep": tep, "anh": anh, "loi": None})
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
