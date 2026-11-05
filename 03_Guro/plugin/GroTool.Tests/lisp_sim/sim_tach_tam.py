# -*- coding: utf-8 -*-
"""Mo phong hoi quy - phep kiem truoc khi cat tam gop (gro--ly-do-khong-cat, muc 186b / 210).

Chay:  python -I sim_tach_tam.py
Moi lan sua luat cat tam gop trong lisp: sua ham ly_do() cho GIONG lisp, THEM ca moi
(so that lay tu %TEMP%\\gro_xepcum_dump.txt / ohta.log), roi chay lai - moi ca cu phai
van dung. Xem bai hoc: 03_Guro/rules/15-phuong-phap-debug.md, muc "Tach tam gop".

Hop (box) = (x1, y1, x2, y2). Khong mo phong buoc tim vach chia - xd la vach da tim duoc.
"""
import math


def kc_hop(b1, b2):
    """gro--kc-hop: khoang cach canh-canh 2 hop."""
    dx = max(0.0, b2[0] - b1[2], b1[0] - b2[2])
    dy = max(0.0, b2[1] - b1[3], b1[1] - b2[3])
    return math.hypot(dx, dy)


def ly_do(tb, sb, xd, t1=None, t2=None):
    """gro--ly-do-khong-cat (ban muc 210b). None = cho cat, chuoi = ly do KHONG cat."""
    if xd is None:
        return "khong co vach chia"
    if t1 is not None and t2 is not None and abs(t1 - t2) > 1e-3:
        return "do day khac"
    cx = (tb[0] + tb[2]) / 2.0
    if cx < xd:
        pt, ph = (sb[0], sb[1], xd, sb[3]), (xd, sb[1], sb[2], sb[3])
    else:
        pt, ph = (xd, sb[1], sb[2], sb[3]), (sb[0], sb[1], xd, sb[3])
    d1, d2 = kc_hop(tb, pt), kc_hop(tb, ph)
    if min(xd - sb[0], sb[2] - xd) < 200.0:
        return "doan < 200"
    if d1 > 800.0:
        return "xa > 800"
    if d1 > d2 + 0.5:  # muc 210b: bang nhau (text lan qua vach) van cat
        return "khong gan hon"
    return None


CAT = None
# (ten, hop text thieu hinh, hop hinh gop, X vach chia, t text, t text chu, ket qua mong doi)
CA = [
    # muc 210b - so THAT lan chay 05/11 17:47 (gro_xepcum_dump): ghi chu 67 rong 555 tren doan 390
    ("67 tren 390|840 (that)", (9487.34, 8745.92, 10042.34, 8883.88),
     (9477.65, 8603.63, 10707.65, 8693.63), 9867.65, 9, 9, CAT),
    # muc 210 - uoc tu anh: ghi chu 92 (~330) lan ~30 qua vach, thanh 570|240
    ("92 tren 570|240 (anh)", (540, 60, 870, 120), (0, 0, 810, 30), 570, 6, 6, CAT),
    ("text gon tren doan", (600, 60, 750, 120), (0, 0, 810, 30), 570, 6, 6, CAT),
    # muc 186b - cac phep kiem phai van chan
    ("khong co vach chia", (600, 60, 750, 120), (0, 0, 810, 30), None, 6, 6, "khong co vach chia"),
    ("khac do day t=", (600, 60, 750, 120), (0, 0, 810, 30), 570, 6, 9, "do day khac"),
    ("doan cat ra < 200", (650, 60, 780, 120), (0, 0, 810, 30), 700, 6, 6, "doan < 200"),
    ("text xa > 800", (600, 900, 750, 960), (0, 0, 810, 30), 570, 6, 6, "xa > 800"),
]


def main():
    ok = 0
    for ten, tb, sb, xd, t1, t2, mong in CA:
        kq = ly_do(tb, sb, xd, t1, t2)
        dung = kq == mong
        ok += dung
        print(("OK  " if dung else "SAI ") + ten + ": " + (kq or "CAT") + ("" if dung else "  (mong: " + (mong or "CAT") + ")"))
    print("%d/%d" % (ok, len(CA)))
    raise SystemExit(0 if ok == len(CA) else 1)


if __name__ == "__main__":
    main()
