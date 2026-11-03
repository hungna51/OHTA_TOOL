# GURO — Tài liệu & cấu trúc thư mục

Thư mục này chứa bộ lệnh LISP **GURO** cho **ZWCAD 2026** (đánh số bango, gán mã khắc, tạo F-name, xuất CSV, in PDF, chuyển layer nét) cùng tài liệu bảo trì cho các AI assistant.

## Cấu trúc thư mục

```
GURO/
├── guro ZWCAD(BG MK FN).lsp   ← BẢN GỐC (đầy đủ): gro, bg, mk, FN, ocsv + lệnh chẩn đoán
├── Guro ZWCAD(gro).lsp        ← Bản GỌN (sinh từ bản gốc): chỉ gro + oi + xln/dsn/dst…
├── lisp_xuat_Fname(FN).lsp    ← Lệnh FN (xuất F-name)
├── ma_khac.lsp / ma_khac2.lsp ← Lệnh mã khắc (mk / mk2)
├── TXTEXP_nhieu_doi_tuong (xt) ZW.lsp ← Lệnh TXTEXP nhiều đối tượng
├── *.ctb                      ← Plot style dùng khi in PDF (gro.ctb, guro.ctb, monochrome-guro.ctb)
├── CLAUDE.md                  ← Mục lục tài liệu cho Claude  → .claude/rules/
├── deepseek.md                ← Mục lục tài liệu cho DeepSeek → rules/
├── plan_ghi_chu_plate_palette.md ← Bản kế hoạch ghi chú / plate palette
├── .claude/rules/             ← 15 file rule cho Claude
├── rules/                     ← 15 file rule cho DeepSeek
├── backup/                    ← Bản sao lưu tự động (._ls) của các file .lsp
└── archive/                   ← Phiên bản cũ (không còn dùng)
```

## Quy ước quan trọng

1. **`guro ZWCAD(BG MK FN).lsp` là bản gốc duy nhất.** Sửa `gro` thì sửa ở đây rồi **sinh lại** `Guro ZWCAD(gro).lsp` — hai file KHÔNG tự đồng bộ.
2. **Không gõ tiếng Việt có dấu / tiếng Nhật trực tiếp vào `.lsp`** (ZWCAD đọc theo CP932 → mojibake). Dùng ASCII thuần cho `princ`/`alert`; nhãn DCL dùng `\U+XXXX`.
3. **Mỗi lần sửa lisp phải cập nhật file rule tương ứng ngay trong cùng lần làm việc** (xem `CLAUDE.md` / `deepseek.md`).
4. **Không viết lại logic đã có** — tra [Bản đồ hàm](rules/03-ban-do-ham.md) trước khi viết hàm mới.

## Ghi chú

- Ba file `.ctb` (`gro.ctb`, `guro.ctb`, `monochrome-guro.ctb`) hiện **giống hệt nhau từng byte**. `gro.ctb` được code dùng trực tiếp (`gro--ctb-in`), `monochrome-guro.ctb` được các file OI cũ ở thư mục cha tham chiếu — nên giữ nguyên cả ba để tránh hỏng đường in.
- `backup/` chứa bản sao lưu tự động; ZWCAD có thể tạo lại file `._ls` mới ở gốc mỗi lần lưu.
