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
    if len(vong) != 1:
        loi.append("viền ngoài phải là 1 vòng kín, đang có %d" % len(vong))
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
    can = len([x for x in t["dim_khach"] if x["loai"] != "r"])
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
