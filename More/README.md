# 02. Nhóm Chức Năng Mẫu 02 (`More`)

> Tài liệu đặc tả riêng của **Mục con 02** (tương ứng **Tab dọc số 2** trên bảng Tool Palettes `OHTA`).
> Luật nền chung của dự án: [../GEMINI.md](../GEMINI.md) | Luật riêng của nhóm: [rules/01-quy-tac-nhom-02.md](rules/01-quy-tac-nhom-02.md)

---

## 1. Mục Đích Nhóm 02

Thư mục này đóng vai trò là **Mục con số 02** trên giao diện Tool Palettes `OHTA`. Mỗi mục con hoàn toàn độc lập về tài liệu (`.md`), bộ quy tắc (`rules/`) và danh sách công cụ (`module.json` + `lisp/`).

---

## 2. Danh Sách Công Cụ Hiện Có Trên Tab 02

| Lệnh CAD | Tên nút trên Tab | File thực thi | Chức năng |
|---|---|---|---|
| `OHTAM2` | Lệnh Thử Nghiệm Nhóm 02 | [`lisp/ohta_m02_demo.lsp`](lisp/ohta_m02_demo.lsp) | Kiểm tra luồng bấm nút trên Tab 02 → tự nạp LISP → thực thi lệnh trên ZWCAD & AutoCAD |
| `KT` | Kiểm tra kín hở | [`lisp/kiem_tra_kin_ho.lsp`](lisp/kiem_tra_kin_ho.lsp) | Quét chọn đối tượng để kiểm tra kín hở `0-KAKOU` và kiểm tra `ByLayer` cho `0-KAKOU`, `3-KEGAKI`, `4-SHT`, `2-F-NAME` (không báo đối tượng ngoài hình chính) |

---

## 3. Tài Liệu Luật Riêng Của Nhóm 02

- [rules/01-quy-tac-nhom-02.md](rules/01-quy-tac-nhom-02.md): Quy định tiền tố hàm `ohta-m02--*`, chuẩn dữ liệu và các quy tắc nghiệp vụ riêng của Nhóm 02.

