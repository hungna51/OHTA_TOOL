"""Môi trường vẽ phòng 4 (OhtaTeams): mở moi-truong-ve.dxf làm nền và các hàm vẽ theo quy ước OHTA.

Mẫu biên dạng trong thu_vien/ chỉ gọi các hàm của lớp NenVe — không tự đặt layer / kiểu chữ / kiểu dim,
nên đổi môi trường (thay tệp DXF) là mọi mẫu theo.
"""
import math
import sys

import ezdxf

# Layer / kiểu chữ / kiểu dim lấy theo moi-truong-ve.dxf (xem moi-truong-ve.md cạnh tệp đó)
LOP_CAT = "0-KAKOU"       # viền cắt (kín) + đường bẻ (nét DASHED)
LOP_DIM = "1-SUNPOU"      # dim, ghi chú
LOP_FNAME = "2-F-NAME"    # F-name
LOP_SLIT = "4-SHT"        # slit
LOP_KHUNG = "Khung_IN"    # khung in bao tấm
LOP_XY = "1-XY"           # dim tổng X, Y (bản chuẩn xưởng 12/11)
LOP_KEGAKI = "3-KEGAKI"   # dấu khắc vạch ケガキ (chữ thập)
NET_BE = "DASHED"
KIEU_CHU = "Text"         # MS PGothic: chữ Nhật + Việt
KIEU_DIM = "ISO-26"
CAO_FNAME = 24.0          # như mẫu F-name trong bản vẽ xưởng
CAO_GHI_CHU = 14.0
LE_DIM = 80.0             # viền → chuỗi dim chi tiết (dưới / phải) — đo trên bản chuẩn xưởng
LE_TONG = 124.0           # viền → dim tổng khi có chuỗi dim (không có chuỗi: LE_DIM)
KHOANG_FNAME = 40.0       # khoảng giữa các dòng F-name
LECH_FNAME = 100.0        # F-name: cách mép trái tấm
DUOI_FNAME = 98.0         # F-name: dòng đầu dưới đường dim tổng X
LE_KHUNG = 30.0           # lề tối thiểu của Khung_IN (rồi nới đủ tỉ lệ khổ A ngang)
TI_LE_KHUNG = 2 ** 0.5
CO_KEGAKI = 5.0           # nửa cạnh chữ thập ケガキ (10 mm)


class NenVe:
    """Một bản vẽ: môi trường + Model để vẽ. Toạ độ mm, tấm đặt từ gốc (0, 0)."""

    def __init__(self, tep_moi_truong):
        self.doc = ezdxf.readfile(tep_moi_truong)
        self.msp = self.doc.modelspace()
        for e in list(self.msp):  # môi trường phải trống; còn sót gì thì bỏ
            self.msp.delete_entity(e)
        self.moc_x, self.moc_y = [], []  # mốc thêm vào chuỗi dim dưới / phải (vd tâm cung có dim R)
        for ten in (LOP_CAT, LOP_DIM, LOP_FNAME, LOP_SLIT, LOP_KHUNG, LOP_XY, LOP_KEGAKI):
            if ten not in self.doc.layers:
                self.doc.layers.add(ten)
        self.kieu_chu = KIEU_CHU if KIEU_CHU in self.doc.styles else "Standard"
        self.kieu_dim = KIEU_DIM if KIEU_DIM in self.doc.dimstyles else "Standard"
        self.net_be = NET_BE if NET_BE in self.doc.linetypes else "Continuous"

    # ---------- hình ----------

    def vien(self, diem):
        """Viền cắt kín (danh sách đỉnh, không lặp đỉnh đầu)."""
        return self.msp.add_lwpolyline(diem, close=True, dxfattribs={"layer": LOP_CAT})

    def duong(self, a, b):
        """Một cạnh viền cắt rời (LINE 0-KAKOU) — viền có cung thì vẽ bằng duong + cung, không chia cung thành điểm."""
        return self.msp.add_line(a, b, dxfattribs={"layer": LOP_CAT})

    def cung(self, tam, r, goc_dau, goc_cuoi, dim=False):
        """Cung viền cắt thật (ARC 0-KAKOU), góc độ ngược chiều kim đồng hồ từ goc_dau tới goc_cuoi.
        dim=True: thêm dim bán kính R ở giữa cung và đưa tâm vào chuỗi dim (như bản chuẩn xưởng)."""
        a = self.msp.add_arc(tam, r, goc_dau, goc_cuoi, dxfattribs={"layer": LOP_CAT})
        if dim:
            # chữ R ra ngoài phía trên-phải (45°) như bản chuẩn — không đè dim tổng Y bên phải; cung không qua 45° thì giữa cung
            quet = (goc_cuoi - goc_dau) % 360
            goc = 45.0 if (45.0 - goc_dau) % 360 <= quet else goc_dau + quet / 2.0
            ra = r + LE_DIM
            vi_tri = (tam[0] + ra * math.cos(math.radians(goc)), tam[1] + ra * math.sin(math.radians(goc)))
            d = self.msp.add_radius_dim(center=tam, radius=r, angle=goc, location=vi_tri, dimstyle=self.kieu_dim, dxfattribs={"layer": LOP_DIM})
            d.render()
            self.moc_x.append(tam[0])
        return a

    def chu_nhat(self, x0, y0, x1, y1):
        return self.vien([(x0, y0), (x1, y0), (x1, y1), (x0, y1)])

    def duong_be(self, a, b):
        """Đường bẻ: LINE trên 0-KAKOU, nét DASHED."""
        return self.msp.add_line(a, b, dxfattribs={"layer": LOP_CAT, "linetype": self.net_be})

    def slit(self, a, b):
        return self.msp.add_line(a, b, dxfattribs={"layer": LOP_SLIT})

    # ---------- chữ / dim ----------

    def chu(self, x, y, noi_dung, cao=CAO_GHI_CHU, lop=LOP_DIM):
        return self.msp.add_text(noi_dung, height=cao, dxfattribs={"layer": lop, "style": self.kieu_chu, "insert": (x, y)})

    def ke_gaki(self, x, y):
        """Dấu ケガキ: chữ thập 10 mm trên 3-KEGAKI."""
        self.msp.add_line((x, y - CO_KEGAKI), (x, y + CO_KEGAKI), dxfattribs={"layer": LOP_KEGAKI})
        self.msp.add_line((x - CO_KEGAKI, y), (x + CO_KEGAKI, y), dxfattribs={"layer": LOP_KEGAKI})

    def leader(self, diem, noi_dung):
        """Leader 1-SUNPOU từ diem[0] (mũi tên) tới diem[-1], chữ MTEXT ở cuối (như bản chuẩn xưởng)."""
        self.msp.add_leader(diem, dimstyle=self.kieu_dim, dxfattribs={"layer": LOP_DIM})
        x, y = diem[-1][0], diem[-1][1]
        return self.msp.add_mtext(noi_dung, dxfattribs={"layer": LOP_DIM, "style": self.kieu_chu, "char_height": CAO_GHI_CHU,
                                                        "insert": (x + 4, y + 4), "attachment_point": 7})  # 7 = dưới-trái

    def dim_ngang(self, x0, x1, y, y_dim, lop=LOP_DIM):
        """Dim ngang đo x0 → x1 tại cao độ y, đường dim ở y_dim."""
        d = self.msp.add_linear_dim(base=(x0, y_dim), p1=(x0, y), p2=(x1, y), angle=0,
                                    dimstyle=self.kieu_dim, dxfattribs={"layer": lop})
        d.render()
        return d

    def dim_doc(self, y0, y1, x, x_dim, lop=LOP_DIM, vi_tri_chu=None):
        """Dim dọc đo y0 → y1 tại hoành độ x, đường dim ở x_dim. vi_tri_chu=(x, y): đặt chữ ra ngoài khi đoạn ngắn (vd 2 dim nửa chiều cao)."""
        d = self.msp.add_linear_dim(base=(x_dim, y0), p1=(x, y0), p2=(x, y1), angle=90, location=vi_tri_chu,
                                    dimstyle=self.kieu_dim, dxfattribs={"layer": lop})
        d.render()
        return d

    def fname(self, x, y_dau, dong):
        """F-name: mỗi dòng một TEXT trên 2-F-NAME (N=, M=, T=, Q=, V=, Z=); y_dau = chân dòng đầu, các dòng cách KHOANG_FNAME."""
        for i, s in enumerate(dong):
            self.chu(x, y_dau - i * KHOANG_FNAME, s, CAO_FNAME, LOP_FNAME)

    def khung_in(self, x0, y0, x1, y1):
        """Khung_IN: LWPOLYLINE kín đúng 4 đỉnh."""
        return self.msp.add_lwpolyline([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], close=True, dxfattribs={"layer": LOP_KHUNG})

    def luu(self, tep):
        self.doc.saveas(tep)


def khung_a(x0, y0, x1, y1):
    """Nới hộp (đã cộng lề) cho đủ tỉ lệ khổ A nằm ngang (√2), giữ tâm."""
    w, h = x1 - x0, y1 - y0
    if w < h * TI_LE_KHUNG:
        them = (h * TI_LE_KHUNG - w) / 2.0
        return x0 - them, y0, x1 + them, y1
    them = (w / TI_LE_KHUNG - h) / 2.0
    return x0, y0 - them, x1, y1 + them


def bao_tat_ca(nen):
    """Hộp bao mọi thứ đã vẽ (viền, dim, chữ, ケガキ) — để đặt Khung_IN."""
    from ezdxf import bbox
    b = bbox.BoundingBox()
    for e in nen.msp:
        try:
            b.extend(bbox.extents([e], fast=True))
        except Exception as ex:  # một đối tượng không tính được hộp bao không làm hỏng cả bản vẽ
            print("bao_tat_ca: bỏ %s (%s)" % (e.dxftype(), ex), file=sys.stderr)
    return (b.extmin.x, b.extmin.y, b.extmax.x, b.extmax.y)


def khung_bao(nen):
    """Khung bao các đối tượng trên 0-KAKOU (để đặt dim, F-name, Khung_IN)."""
    from ezdxf import bbox
    b = bbox.extents(nen.msp.query('*[layer=="%s"]' % LOP_CAT), fast=True)
    return (b.extmin.x, b.extmin.y, b.extmax.x, b.extmax.y) if b.has_data else (0.0, 0.0, 0.0, 0.0)
