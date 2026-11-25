"""Bộ kiểm mẫu da_giac + quy tắc chân dim (25/11) — vẽ thật trên môi trường của xưởng.

    python thu_da_giac.py [MOI_TRUONG.dxf]   (mặc định ../data/quy_chuan/4_trien_khai/moi_truong/moi-truong-ve.dxf — gitignore, chỉ máy có quy chuẩn)

Mỗi hình đạt khi: vẽ không lỗi, không "thiếu dim", viền ngoài là MỘT vòng kín, mọi chân dim (gióng) nằm trên hình trong 0.05 mm,
đủ số dim khách. Hình lấy từ các lần thử thật: chữ T (25/11 10:22), tam giác cụt (10:53), hình thang (10:57), dải đầu lồi / lõm
(thay dai_lom_loi), tấm chữ nhật có lỗ (4canh).
"""
import os
import sys
import tempfile

import ezdxf

DAY = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, DAY)
import chan_dim   # noqa: E402
import goc_khoet  # noqa: E402
import ve         # noqa: E402


def ng(a, b, tai=None):
    d = {"loai": "ngang", "gia_tri": round(b - a, 3), "tu": a, "den": b}
    if tai is not None:
        d["tai"] = tai
    return d


def doc(a, b, tai=None):
    d = {"loai": "doc", "gia_tri": round(b - a, 3), "tu": a, "den": b}
    if tai is not None:
        d["tai"] = tai
    return d


HINH = [
    {"bango": "chu_t", "bien_dang": "da_giac", "x": 276, "y": 292,
     "dinh": [[0, 0], [276, 0], [276, 200], [156, 200], [156, 292], [76, 292], [76, 200], [0, 200]],
     "lo": [{"d": 18, "so_luong": 3, "vi_tri": [{"x": 99, "y": 25}, {"x": 253, "y": 25}, {"x": 99, "y": 267}]}],
     "dim_khach": [ng(0, 76, 320), ng(76, 156, 320), ng(156, 276, 320), ng(0, 99, -30), ng(99, 253, -30),
                   doc(0, 200, 300), doc(200, 292, 300), doc(0, 25, -30), doc(25, 267, -30)]},
    {"bango": "tam_giac_cut", "bien_dang": "da_giac", "x": 230, "y": 170,
     "dinh": [[0, 170], [230, 170], [230, 0], [218, 0], [0, 152]],
     "dim_khach": [ng(218, 230, -30), doc(152, 170, -30),
                   {"loai": "xien", "gia_tri": 265.76, "tu": 0, "den": 0, "diem": [[218, 0], [0, 152]]}]},
    {"bango": "hinh_thang", "bien_dang": "da_giac", "x": 110, "y": 160,
     "dinh": [[0, 0], [10, 0], [110, 150], [110, 160], [0, 160]],
     "dim_khach": [ng(0, 10, -30), doc(150, 160, 140),
                   {"loai": "xien", "gia_tri": 180.28, "tu": 0, "den": 0, "diem": [[10, 0], [110, 150]]},
                   {"loai": "goc", "gia_tri": 33.69, "tu": 0, "den": 0, "diem": [[110, 150], [10, 0], [10, 160]]}]},
    {"bango": "dai_loi_lom", "bien_dang": "da_giac", "x": 330, "y": 60,
     "dinh": [[0, 0], [300, 0, 30], [300, 60], [0, 60, -30]],
     "dim_khach": [{"loai": "r", "gia_tri": 30, "tu": 300, "den": 30}, ng(0, 300, 90)]},
    {"bango": "lo_4canh", "bien_dang": "4canh", "x": 300, "y": 150,
     "lo": [{"d": 10, "so_luong": 4, "vi_tri": [{"x": 20, "y": 20}, {"x": 280, "y": 20}, {"x": 20, "y": 130}, {"x": 280, "y": 130}]}],
     "dim_khach": [ng(0, 20, 180), ng(20, 280, 180), ng(280, 300, 180), doc(0, 20, 330), doc(20, 130, 330), doc(130, 150, 330)]},
    # bản vẽ khách 25/11: khoét 240 × 240 có bo góc nhưng KHÔNG ghi R → AI lấy R10 theo hình, ghi chua_chac → 4 cung R10 tô vàng
    {"bango": "khoet_r10", "bien_dang": "da_giac", "x": 455, "y": 370,
     "dinh": [[100, 0], [355, 0], [455, 35], [455, 335], [355, 370], [100, 370], [0, 335], [0, 35]],
     "lo": [{"d": 50, "so_luong": 1, "vi_tri": [{"x": 50, "y": 185}]}],
     "khoet": [{"kieu": "chu_nhat", "rong": 240, "cao": 240, "goc_xoay": 0, "r": 10, "vi_tri": [[240, 185]]}],
     "chua_chac": [{"o": "khoet.r", "so": 1, "noi_dung": "R10 bo góc khoét: khách không ghi số, lấy theo hình"}],
     "vang": 4,
     "dim_khach": [ng(0, 100, 420), ng(100, 355, 420), ng(355, 455, 420), ng(0, 120, 185), ng(120, 360, 185), ng(360, 455, 185),
                   ng(0, 50, -30), doc(35, 335, 420), doc(0, 65, 230), doc(65, 305, 230), doc(305, 370, 230)]},
    # fax 25/11: vành Ø242.6 / Ø151.7, 3 マーキング chữ V đáy 30 nằm trên vòng ngoài, MŨI cách tâm 95.5 chỉ vào tâm (120°) — bản đúng Desktop/test.dxf, スリット cắt đứt vành bên trái ngang tâm (chưa chắc)
    {"bango": "vanh_slit", "bien_dang": "da_giac", "x": 242.6, "y": 242.6,
     "dinh": [[121.3, 0, 121.3], [242.6, 121.3, 121.3], [121.3, 242.6, 121.3], [0, 121.3, 121.3]],
     "lo": [{"d": 151.7, "so_luong": 1, "vi_tri": [{"x": 121.3, "y": 121.3}]}],
     "ke_gaki": [[121.3, 216.8, 30, 270, 24.869], [38.594, 73.55, 30, 30, 24.869], [204.006, 73.55, 30, 150, 24.869]],
     "slit": [[0, 121.3, 45.45, 121.3]],
     "chua_chac": [{"o": "slit", "so": 1, "noi_dung": "スリット: khách không ghi vị trí / bề rộng"}],
     "vang_lop": ("4-SHT", 1), "ke_gaki_net": 6,
     "dim_khach": [{"loai": "phi", "gia_tri": 242.6, "tu": 0, "den": 0}, doc(121.3, 216.8, 140), ng(106.3, 136.3, 260),
                   {"loai": "goc", "gia_tri": 120, "tu": 0, "den": 0, "diem": [[121.3, 216.8], [121.3, 121.3], [204.006, 73.55]]}]},
    # Drawing7 25/11: cung R40 tâm (25, 40) + mép trái / trên cách tâm 25, góc trái-trên CẮT XIÊN song song đường tâm 45° cách 25,
    # lỗ vuông 20.2 xoay 45° R0.5 — AI từng bỏ cạnh xiên vì "chưa rõ"; dim 25 xiên + góc 45° đặt chân trên đường tâm xiên (duong_tam)
    {"bango": "cung_cat_xien", "bien_dang": "da_giac", "x": 65, "y": 65,
     "dinh": [[0, 8.775, 40], [56.225, 65], [14.645, 65], [0, 50.355]],
     "khoet": [{"kieu": "chu_nhat", "rong": 20.2, "cao": 20.2, "goc_xoay": 45, "r": 0.5, "vi_tri": [[25, 40]]}],
     "duong_tam": [[10, 25, 60, 75], [0, 40, 70, 40]],
     "dim_khach": [ng(0, 25, 75), doc(40, 65, -10),
                   {"loai": "xien", "gia_tri": 25, "tu": 0, "den": 0, "diem": [[25, 40], [7.3225, 57.6775]]},
                   {"loai": "goc", "gia_tri": 45, "tu": 0, "den": 0, "diem": [[60, 40], [25, 40], [60, 75]]}]},
    # 25/11 12:41: tấm tròn Ø500 lỗ Ø245 — AI ghi 2 đỉnh (2 nửa cung R250), trước bị chặn "cần ≥ 3 đỉnh"
    {"bango": "tron", "bien_dang": "da_giac", "x": 500, "y": 500, "dinh": [[0, 250, 250], [500, 250, 250]],
     "lo": [{"d": 245, "so_luong": 1, "vi_tri": [{"x": 250, "y": 250}]}],
     "dim_khach": [{"loai": "phi", "gia_tri": 500, "tu": 0, "den": 0}]},
]


class _Nen:
    def __init__(self, msp):
        self.msp = msp


def kiem_tam(moi_truong, t, thu_muc):
    mau = ve.cac_mau([os.path.join(DAY, "thu_vien")])
    tep, _, thieu, canh_bao = ve.ve_tam({"moi_truong": moi_truong, "thu_muc_ra": thu_muc, "chi_hinh": True}, t, mau, {}, False)
    loi = ["thiếu dim: " + s for s in thieu]
    msp = ezdxf.readfile(tep).modelspace()
    vong = goc_khoet.doc_vien(_Nen(msp))
    can_vong = 1 + sum(len(k.get("vi_tri") or []) for k in t.get("khoet") or [])
    if len(vong) != can_vong:
        loi.append("cần %d vòng kín (viền + khoét), đang có %d" % (can_vong, len(vong)))
    hinh = chan_dim.hinh_tu_nen(_Nen(msp))
    so_dim = 0
    for d in msp.query("DIMENSION"):
        kieu = d.dimtype & 7
        if kieu in (0, 1):     # thẳng / theo cạnh: 2 chân gióng
            chan = [d.dxf.defpoint2, d.dxf.defpoint3]
        elif kieu == 5:        # góc 3 điểm: 2 điểm cạnh + đỉnh
            chan = [d.dxf.defpoint2, d.dxf.defpoint3, d.dxf.defpoint4]
        else:
            continue
        so_dim += 1
        for p in chan:
            if not chan_dim.tren_hinh(hinh, (p.x, p.y)):
                loi.append("chân dim %s (%.2f) tại (%.3f, %.3f) không chạm hình" % (d.dxf.get("layer"), d.get_measurement(), p.x, p.y))
    if "ke_gaki_net" in t and len(msp.query('LINE[layer=="3-KEGAKI"]')) != t["ke_gaki_net"]:
        loi.append("ケガキ: %d nét, cần %d" % (len(msp.query('LINE[layer=="3-KEGAKI"]')), t["ke_gaki_net"]))
    if "slit" in t and len(msp.query('LINE[layer=="4-SHT"]')) != len(t["slit"]):
        loi.append("slit: %d nét 4-SHT, cần %d" % (len(msp.query('LINE[layer=="4-SHT"]')), len(t["slit"])))
    if "vang_lop" in t:
        lp, n = t["vang_lop"]
        if len([e for e in msp.query('*[layer=="%s"]' % lp) if e.dxf.get("color") == 2]) != n:
            loi.append("cần %d nét vàng trên %s" % (n, lp))
    for d in msp.query("DIMENSION"):
        if d.dimtype & 7 == 5:
            gk = [x["gia_tri"] for x in t["dim_khach"] if x["loai"] == "goc"]
            if gk and all(abs(d.get_measurement() * (180 / 3.141592653589793 if d.get_measurement() < 6.3 else 1) - g) > 0.5 for g in gk):
                loi.append("dim góc ra %.2f, khách ghi %s" % (d.get_measurement(), gk))
    vang = [e for e in msp.query('*[layer=="0-KAKOU"]') if e.dxf.get("color") == 2]
    if len(vang) != t.get("vang", 0) or any(e.dxftype() != "ARC" for e in vang):
        loi.append("tô vàng %d nét (%s), cần %d cung" % (len(vang), ",".join(sorted({e.dxftype() for e in vang})), t.get("vang", 0)))
    can = len([x for x in t["dim_khach"] if x["loai"] not in ("r", "phi")])   # R / Ø: dim bán kính / đường kính, không có chân gióng
    if so_dim < can:
        loi.append("chỉ có %d dim thẳng / góc, khách có %d" % (so_dim, can))
    return loi + ["cảnh báo: " + s for s in canh_bao]


def main(argv):
    mt = argv[1] if len(argv) > 1 else os.path.join(DAY, "..", "data", "quy_chuan", "4_trien_khai", "moi_truong", "moi-truong-ve.dxf")
    if not os.path.exists(mt):
        print("Không có môi trường vẽ:", mt)
        return 2
    hong = 0
    with tempfile.TemporaryDirectory() as tm:
        for t in HINH:
            try:
                loi = kiem_tam(mt, dict(t), tm)
            except Exception as ex:
                loi = ["%s: %s" % (type(ex).__name__, ex)]
            print(("OK   " if not loi else "FAIL ") + t["bango"])
            for s in loi:
                print("       " + s)
            hong += bool(loi)
    print("Tong: %d OK, %d FAIL" % (len(HINH) - hong, hong))
    return 1 if hong else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
