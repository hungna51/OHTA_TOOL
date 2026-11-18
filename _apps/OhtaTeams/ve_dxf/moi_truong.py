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
LE_DIM = 80.0             # viền → chuỗi dim chi tiết (dưới / phải) — đo trên bản chuẩn xưởng (tấm lớn)
BUOC_DIM = 44.0           # khoảng giữa các hàng dim (chuỗi → hàng dim khách thêm → dim tổng)
LE_DIM_NHO = 32.0         # tấm nhỏ (bản chuẩn 130996-3, 80×80): chuỗi cách viền ~32, dim tổng ~64
BUOC_DIM_NHO = 32.0
DACH_FNAME = 28.0         # F-name đặt bên phải: cách dim ngoài cùng (bản chuẩn 130996-3)
MOC_TOI_DA = 12           # chuỗi dim quá 12 đoạn thì tách thêm hàng
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

    def dau_tam(self, x, y, nua_x, nua_y=None):
        """Dấu tâm lỗ: chữ thập trên 1-SUNPOU (nét theo layer), mỗi nhánh dài nua_x / nua_y từ tâm (bản sửa 18/11: r + 1.6)."""
        nua_y = nua_x if nua_y is None else nua_y
        self.msp.add_line((x - nua_x, y), (x + nua_x, y), dxfattribs={"layer": LOP_DIM})
        self.msp.add_line((x, y - nua_y), (x, y + nua_y), dxfattribs={"layer": LOP_DIM})

    def duong_tam(self, a, b):
        """Đường tâm (nét CENTER) trên 1-SUNPOU — chỉ để đọc, không cắt / không khắc."""
        return self.msp.add_line(a, b, dxfattribs={"layer": LOP_DIM, "linetype": "CENTER" if "CENTER" in self.doc.linetypes else "Continuous"})

    def duong_be(self, a, b):
        """Đường bẻ: LINE trên 0-KAKOU, nét DASHED."""
        return self.msp.add_line(a, b, dxfattribs={"layer": LOP_CAT, "linetype": self.net_be})

    def slit(self, a, b):
        return self.msp.add_line(a, b, dxfattribs={"layer": LOP_SLIT})

    # ---------- chữ / dim ----------

    def chu(self, x, y, noi_dung, cao=CAO_GHI_CHU, lop=LOP_DIM):
        return self.msp.add_text(noi_dung, height=cao, dxfattribs={"layer": lop, "style": self.kieu_chu, "insert": (x, y)})

    def lo_tron(self, x, y, d):
        """Lỗ tròn cắt: CIRCLE trên 0-KAKOU (lệnh TMT / GRO nhận Ø)."""
        return self.msp.add_circle((x, y), d / 2.0, dxfattribs={"layer": LOP_CAT})

    def ke_gaki(self, x, y):
        """Dấu ケガキ: chữ thập 10 mm trên 3-KEGAKI."""
        self.msp.add_line((x, y - CO_KEGAKI), (x, y + CO_KEGAKI), dxfattribs={"layer": LOP_KEGAKI})
        self.msp.add_line((x - CO_KEGAKI, y), (x + CO_KEGAKI, y), dxfattribs={"layer": LOP_KEGAKI})

    def leader(self, diem, noi_dung):
        """Leader 1-SUNPOU: mũi tên diem[0] (mép lỗ / điểm chỉ) → gãy tại diem[1] → đoạn ngang dài bằng chữ làm GẠCH CHÂN (bản sửa 18/11);
        chữ MTEXT nằm trên gạch chân, hướng theo diem[-1] (trái / phải của điểm gãy). Chữ gắn vào leader (annotation) — kéo chữ trong CAD leader theo."""
        (x, y), huong = diem[1], (-1 if len(diem) > 2 and diem[-1][0] < diem[1][0] else 1)
        st = self.doc.styles.get(self.kieu_chu) if self.kieu_chu in self.doc.styles else None
        rong = do_rong_chu(noi_dung, CAO_GHI_CHU, (st.dxf.font if st is not None and st.dxf.get("font") else "MS PGothic.ttf")) + 4
        mt = self.msp.add_mtext(noi_dung, dxfattribs={"layer": LOP_DIM, "style": self.kieu_chu, "char_height": CAO_GHI_CHU,
                                                     "insert": (x + huong * 2, y + 3), "attachment_point": 7 if huong > 0 else 9})  # 7 dưới-trái / 9 dưới-phải
        ld = self.msp.add_leader([diem[0], (x, y), (x + huong * rong, y)], dimstyle=self.kieu_dim, dxfattribs={"layer": LOP_DIM})
        ld.dxf.annotation_type = 0          # 0 = MTEXT
        ld.dxf.annotation_handle = mt.dxf.handle
        ld.dxf.has_hookline = 0             # gạch chân đã là đoạn cuối của leader
        ld.dxf.text_height = CAO_GHI_CHU
        ld.dxf.text_width = rong
        mt.append_reactor_handle(ld.dxf.handle)
        return mt

    def dim_ngang(self, x0, x1, y, y_dim, lop=LOP_DIM, chu="<>", y_a=None, y_b=None, vi_tri_chu=None):
        """Dim ngang đo x0 → x1 tại cao độ y, đường dim ở y_dim. chu: chữ thay số đo ("<>" = số đo, vd "100P×5=500").
        y_a / y_b: đường gióng mỗi đầu kéo từ điểm thật (vd tâm lỗ) thay vì mép tấm. vi_tri_chu=(x, y): chữ ra ngoài (dim ngắn ở đầu chuỗi)."""
        d = self.msp.add_linear_dim(base=(x0, y_dim), p1=(x0, y if y_a is None else y_a), p2=(x1, y if y_b is None else y_b), angle=0, text=chu, location=vi_tri_chu,
                                    dimstyle=self.kieu_dim, dxfattribs={"layer": lop})
        d.render()
        return d

    def dim_doc(self, y0, y1, x, x_dim, lop=LOP_DIM, vi_tri_chu=None, chu="<>", x_a=None, x_b=None):
        """Dim dọc đo y0 → y1 tại hoành độ x, đường dim ở x_dim. vi_tri_chu=(x, y): đặt chữ ra ngoài khi đoạn ngắn (vd 2 dim nửa chiều cao).
        x_a / x_b: đường gióng mỗi đầu kéo từ điểm thật (vd tâm lỗ)."""
        d = self.msp.add_linear_dim(base=(x_dim, y0), p1=(x if x_a is None else x_a, y0), p2=(x if x_b is None else x_b, y1), angle=90, location=vi_tri_chu, text=chu,
                                    dimstyle=self.kieu_dim, dxfattribs={"layer": lop})
        d.render()
        return d

    def dim_phi(self, x, y, d, goc=60.0, le=LE_DIM, chu="<>", chu_ngang=False):
        """Dim đường kính Ø một lỗ tròn, chữ ra ngoài theo góc goc (bản chuẩn 130996-3); chu "4-<>" = nhóm 4 lỗ (bản sửa 18/11)."""
        r, a = d / 2.0, math.radians(goc)
        d2 = self.msp.add_diameter_dim(center=(x, y), radius=r, angle=goc, text=chu, location=(x + (r + 2 * le) * math.cos(a), y + (r + 2 * le) * math.sin(a)),  # chữ ra hẳn ngoài viền
                                       dimstyle=self.kieu_dim, override=dict({"dimtmove": 0}, **({"dimtoh": 1} if chu_ngang else {})), dxfattribs={"layer": LOP_DIM})  # đường dim kéo tới chữ; chu_ngang: chữ nằm ngang
        d2.render()
        return d2

    def fname(self, x, y_dau, dong):
        """F-name: mỗi dòng một TEXT trên 2-F-NAME (N=, M=, T=, Q=, V=, Z=); y_dau = chân dòng đầu, các dòng cách KHOANG_FNAME."""
        for i, s in enumerate(dong):
            self.chu(x, y_dau - i * KHOANG_FNAME, s, CAO_FNAME, LOP_FNAME)

    def khung_in(self, x0, y0, x1, y1):
        """Khung_IN: LWPOLYLINE kín đúng 4 đỉnh."""
        return self.msp.add_lwpolyline([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], close=True, dxfattribs={"layer": LOP_KHUNG})

    def luu(self, tep):
        self.doc.saveas(tep)


def khoang_dim(w, h):
    """(viền → chuỗi dim, giữa các hàng dim) theo cỡ tấm: tấm lớn 80 / 44 (bản chuẩn 12/11), tấm nhỏ 32 / 32 (bản chuẩn 130996-3),
    ở giữa nội suy theo cạnh lớn. shortcut: nội suy tuyến tính 80 → 200 mm là đoán, có bản chuẩn tấm cỡ vừa thì chỉnh."""
    k = min(1.0, max(0.0, (max(w, h) - 80.0) / 120.0))
    return LE_DIM_NHO + k * (LE_DIM - LE_DIM_NHO), BUOC_DIM_NHO + k * (BUOC_DIM - BUOC_DIM_NHO)


assert khoang_dim(80, 80) == (32.0, 32.0) and khoang_dim(600, 140) == (80.0, 44.0)


def do_rong_chu(s, cao, font="MS PGothic.ttf"):
    """Bề rộng chữ (mm) cho gạch chân leader: đo bằng font thật của kiểu chữ (ezdxf.fonts); máy thiếu font → ước lượng
    (chữ Nhật / Hán ~ 1 cao, Latinh / số ~ 0.9 cao — gần MS PGothic, thà dài hơn chữ còn hơn hụt)."""
    try:
        from ezdxf.fonts import fonts
        return float(fonts.make_font(font, cao).text_width(s))
    except Exception:  # font / ezdxf.fonts không có: ước lượng
        return sum(cao * (1.0 if ord(c) >= 0x2E80 else 0.9) for c in s)


assert do_rong_chu("Ø12", 14) > 20


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
    b = bbox.extents(nen.msp.query('*[layer=="%s"]' % LOP_CAT), fast=False)  # cung: chế độ nhanh vượt mép (điểm điều khiển Bezier)
    return (b.extmin.x, b.extmin.y, b.extmax.x, b.extmax.y) if b.has_data else (0.0, 0.0, 0.0, 0.0)
