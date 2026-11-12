"""Môi trường vẽ phòng 4 (OhtaTeams): mở moi-truong-ve.dxf làm nền và các hàm vẽ theo quy ước OHTA.

Mẫu biên dạng trong thu_vien/ chỉ gọi các hàm của lớp NenVe — không tự đặt layer / kiểu chữ / kiểu dim,
nên đổi môi trường (thay tệp DXF) là mọi mẫu theo.
"""
import ezdxf

# Layer / kiểu chữ / kiểu dim lấy theo moi-truong-ve.dxf (xem moi-truong-ve.md cạnh tệp đó)
LOP_CAT = "0-KAKOU"       # viền cắt (kín) + đường bẻ (nét DASHED)
LOP_DIM = "1-SUNPOU"      # dim, ghi chú
LOP_FNAME = "2-F-NAME"    # F-name
LOP_SLIT = "4-SHT"        # slit
LOP_KHUNG = "Khung_IN"    # khung in bao tấm
NET_BE = "DASHED"
KIEU_CHU = "Text"         # MS PGothic: chữ Nhật + Việt
KIEU_DIM = "ISO-26"
CAO_FNAME = 24.0          # như mẫu F-name trong bản vẽ xưởng
CAO_GHI_CHU = 14.0
LE_DIM = 40.0             # khoảng từ viền tới đường dim
LE_KHUNG = 30.0


class NenVe:
    """Một bản vẽ: môi trường + Model để vẽ. Toạ độ mm, tấm đặt từ gốc (0, 0)."""

    def __init__(self, tep_moi_truong):
        self.doc = ezdxf.readfile(tep_moi_truong)
        self.msp = self.doc.modelspace()
        for e in list(self.msp):  # môi trường phải trống; còn sót gì thì bỏ
            self.msp.delete_entity(e)
        for ten in (LOP_CAT, LOP_DIM, LOP_FNAME, LOP_SLIT, LOP_KHUNG):
            if ten not in self.doc.layers:
                self.doc.layers.add(ten)
        self.kieu_chu = KIEU_CHU if KIEU_CHU in self.doc.styles else "Standard"
        self.kieu_dim = KIEU_DIM if KIEU_DIM in self.doc.dimstyles else "Standard"
        self.net_be = NET_BE if NET_BE in self.doc.linetypes else "Continuous"

    # ---------- hình ----------

    def vien(self, diem):
        """Viền cắt kín (danh sách đỉnh, không lặp đỉnh đầu)."""
        return self.msp.add_lwpolyline(diem, close=True, dxfattribs={"layer": LOP_CAT})

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

    def dim_ngang(self, x0, x1, y, y_dim):
        """Dim ngang đo x0 → x1 tại cao độ y, đường dim ở y_dim."""
        d = self.msp.add_linear_dim(base=(x0, y_dim), p1=(x0, y), p2=(x1, y), angle=0,
                                    dimstyle=self.kieu_dim, dxfattribs={"layer": LOP_DIM})
        d.render()
        return d

    def dim_doc(self, y0, y1, x, x_dim):
        d = self.msp.add_linear_dim(base=(x_dim, y0), p1=(x, y0), p2=(x, y1), angle=90,
                                    dimstyle=self.kieu_dim, dxfattribs={"layer": LOP_DIM})
        d.render()
        return d

    def fname(self, x, y_tren, dong):
        """F-name: mỗi dòng một TEXT trên 2-F-NAME (N=, M=, T=, Q=, V=, Z=), từ y_tren xuống. Trả bề rộng ước."""
        for i, s in enumerate(dong):
            self.chu(x, y_tren - CAO_FNAME - i * CAO_FNAME * 2.0, s, CAO_FNAME, LOP_FNAME)
        return max((len(s) for s in dong), default=0) * CAO_FNAME * 0.75

    def khung_in(self, x0, y0, x1, y1):
        """Khung_IN: LWPOLYLINE kín đúng 4 đỉnh."""
        return self.msp.add_lwpolyline([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], close=True, dxfattribs={"layer": LOP_KHUNG})

    def luu(self, tep):
        self.doc.saveas(tep)


def khung_bao(nen):
    """Khung bao các đối tượng trên 0-KAKOU (để đặt dim, F-name, Khung_IN)."""
    from ezdxf import bbox
    b = bbox.extents(nen.msp.query('*[layer=="%s"]' % LOP_CAT), fast=True)
    return (b.extmin.x, b.extmin.y, b.extmax.x, b.extmax.y) if b.has_data else (0.0, 0.0, 0.0, 0.0)
