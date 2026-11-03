# OHTA TOOL — AI Development Guide & Project Index

Tài liệu chỉ dẫn trung tâm dành cho **AI Agent (Gemini / Antigravity / Claude)** và lập trình viên khi phát triển, bảo trì, mở rộng dự án **OHTA TOOL** trên cả hai nền tảng **ZWCAD 2026** và **AutoCAD**.

---

## 1. Nguyên Tắc Phân Tầng Bắt Buộc (Đọc Trước Khi Làm Việc)

Dự án **OHTA TOOL** được thiết kế theo kiến trúc **Module Phân Tầng 2 Cấp** (1 Thư mục con = 1 Tab sườn dọc trên Tool Palettes khi gõ lệnh `OHTA`):

1. **Cấp 1 — Luật Chung Nhóm (`OHTA_TOOL/GEMINI.md` + `OHTA_TOOL/rules/`)**:
   - Quy định kiến trúc khung, chuẩn biên dịch kép ZWCAD/AutoCAD, giao thức `module.json`, quy tắc encoding an toàn và checklist kiểm thử.
   - **Mọi thư mục con BẮT BUỘC phải tuân thủ Luật Cấp 1.**
2. **Cấp 2 — Luật Riêng Từng Mục Con (`OHTA_TOOL/<Muc_Con>/README.md` + `OHTA_TOOL/<Muc_Con>/rules/`)**:
   - Mỗi thư mục con phục vụ một nhóm chức năng nghiệp vụ độc lập và có bộ `rules/` riêng (quy định layer, cấu trúc dữ liệu, công thức tính toán, trường hợp biên riêng của nhóm đó).
   - **Quy tắc cô lập**: Khi làm việc trên mục con `A`, AI đọc `OHTA_TOOL/rules/*` + `A/README.md` + mục lục `A/rules/` (file `00-*` nếu có), rồi chỉ mở các file rule mà mục lục chỉ tới cho việc đang làm. AI chỉ sửa mã nguồn trong mục con `A`; mục con `B` và hạ tầng `_system/` chỉ sửa khi được yêu cầu.

---

## 2. Bản Đồ Tài Liệu Luật Chung (`rules/`)

| Tình huống công việc | Tài liệu cần đọc ngay |
|---|---|
| Hiểu cấu trúc thư mục, cách ánh xạ Thư mục con → Tab Palette, ranh giới giữa các nhóm | [01-kien-truc-va-phan-tang.md](rules/01-kien-truc-va-phan-tang.md) |
| Viết C# (`.NET`) hoặc AutoLISP chạy chung cho cả **ZWCAD 2026** và **AutoCAD** | [02-da-nen-tang-zwcad-autocad.md](rules/02-da-nen-tang-zwcad-autocad.md) |
| Thêm mục con mới, khai báo nút bấm trong `module.json`, hoặc nhúng giao diện WPF riêng vào Tab | [03-giao-thuc-module-va-palette.md](rules/03-giao-thuc-module-va-palette.md) |
| Kiểm tra lỗi Encoding (CP932 / UTF-8 / ASCII), khai báo biến cục bộ, `*error*`, Unit Test | [04-encoding-va-checklist-an-toan.md](rules/04-encoding-va-checklist-an-toan.md) |
| Gặp thuật ngữ nghiệp vụ (tấm, hình, khung tấm, bango, F-name…) | [GLOSSARY.md](GLOSSARY.md) |
| Định đổi cấu trúc DLL / mục con, hoặc thắc mắc "sao lại làm thế này" | [_docs/adr/](_docs/adr/) |

---

## 3. Các Lệnh Khung Của Hệ Thống `OHTA TOOL`

```text
OHTA        : Mở bảng Tool Palettes "OHTA TOOL" (tự nhận diện ZWCAD / AutoCAD, tự nạp DLL).
              Mỗi thư mục con hợp lệ trong OHTA_TOOL/ hiển thị thành 1 Tab sườn dọc trên PaletteSet.
FN          : Lệnh tắt — mở bảng OHTA TOOL và chuyển thẳng sang tab Fname.
              (Thay lệnh FN cũ của lisp GURO: khi OHTATOOL.dll đã nạp, CAD ưu tiên lệnh .NET.)
OHTARELOAD  : Quét lại thư mục OHTA_TOOL/ và cập nhật nóng toàn bộ danh sách Tab + nút công cụ
              ngay trong phiên làm việc CAD (không cần tắt mở lại CAD).
OHTACLOSE   : Ẩn / Bật nhanh bảng Tool Palettes OHTA TOOL.
OHTAINFO    : In thông tin chẩn đoán (nền tảng CAD, đường dẫn gốc OHTA_TOOL, danh sách Tab đã nạp).
```

---

## 4. Danh Sách Các Mục Con (Tabs) Hiện Có

| Thứ tự | Thư mục mục con | Tài liệu nhóm | Thư mục luật riêng | Trạng thái |
|---|---|---|---|---|
| mẫu | [`_Template_Module/`](_Template_Module/README.md) | Khuôn mẫu để nhân bản khi tạo nhóm mới (tiền tố `_` nên không hiện Tab) | [`_Template_Module/rules/`](_Template_Module/rules/) | Khuôn mẫu |
| 01 | [`01_Fname/`](01_Fname/README.md) | Tab **Fname**: tạo, hút, sửa F-NAME (`N=, M=, T=, Q=, Z=`) & các lệnh Fname tấm | [`01_Fname/rules/`](01_Fname/rules/) | Đã tích hợp UI chọn Fname |
| 02 | [`02_Module_Mau_2/`](02_Module_Mau_2/README.md) | Nhóm chức năng mẫu 02 | [`02_Module_Mau_2/rules/`](02_Module_Mau_2/rules/) | Khung sẵn sàng mở rộng |
| 03 | [`03_Guro/`](03_Guro/README.md) | Tab **Guro**: lệnh `GRO` (bango + mã khắc + F-name + CSV + PDF + chuyển layer), `DST`, `XLN`, `GI`… — DLL riêng `GROTOOL.dll`, chạy ZWCAD 2026 + AutoCAD 2027 | [`03_Guro/rules/`](03_Guro/rules/) | Đã tích hợp (chuyển từ ZWLISP\GURO) |

*(Khi tạo thêm thư mục mục con mới, hãy cập nhật bổ sung 1 dòng vào bảng trên để đồng bộ bản đồ dự án).*

---

## 5. Quản Lý Phiên Bản (git) & Đóng Gói

- Thư mục `OHTA_TOOL/` là 1 repo git (nhánh `main`). Mỗi bản gửi đội được gắn tag (`v1.0`, ...); xem lại bằng `git checkout v1.0`.
- `.gitignore` bỏ qua `bin/`, `obj/`, `*.pdb`, `OHTATOOL.old_*`, `_release/` (file zip), `graphify-out/`.
- **Khoá ghi Google Sheet** để riêng trong `01_Fname/data/google_sheet.key` (`write_key=...`) — **không đưa lên git**, nhưng **phải có trong gói zip** gửi đội. Apps Script đọc khoá từ *Thuộc tính tập lệnh* `WRITE_KEY`, không ghi trong mã nguồn.
- Đóng gói: `OHTA_Loader.lsp`, `README.md`, `_system/bin/*/OHTATOOL.dll`, `03_Guro/bin/*/GROTOOL.dll`, các thư mục mục con (bỏ `google_sheet_apps_script.gs`, `03_Guro/plugin/`) → `_release/OHTA_TOOL_vX.zip`.
- AutoCAD hỏi SECURELOAD khi nạp lisp ngoài thư mục tin cậy: thêm thư mục `OHTA_TOOL` (kèm cây con `...`) vào **Trusted Locations** trên mỗi máy.

## 6. Checklist Bắt Buộc Trước Khi Lưu Mã Nguồn

- [ ] **Đúng phạm vi nhóm**: Chỉ sửa trong đúng thư mục mục con được yêu cầu (trừ khi nâng cấp tính năng khung `_system/`).
- [ ] **Cập nhật tài liệu đi kèm**: Nếu thêm/sửa lệnh hoặc luật nghiệp vụ của nhóm nào, cập nhật ngay `README.md`, `module.json` và `rules/` của chính nhóm đó.
- [ ] **An toàn Encoding LISP**: File `.lsp` đạt 100% ASCII thuần (không gõ tiếng Việt có dấu / tiếng Nhật trần, không có UTF-8 BOM). Nhãn DCL dùng `\U+XXXX`. File `.json`, `.cs`, `.xaml`, `.md` dùng UTF-8 chuẩn.
- [ ] **Chuẩn AutoLISP đa nền tảng**: Không dùng hàm `acet-*` (Express Tools); khai báo đầy đủ biến cục bộ sau dấu `/`; mọi lệnh `c:XXX` có `*error*` khôi phục `CMDECHO`, `FILEDIA`, `OSMODE`, `CLAYER`.
- [ ] **Chuẩn .NET Palette đa nền tảng**: Mọi thao tác đọc/ghi CAD Database từ giao diện modeless đều bọc trong `using (CadCompat.LockDocument())`.
- [ ] **Kiểm chứng tự động**: Chạy `powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1` đạt build sạch cả 2 bản DLL (`ZwCAD` & `AutoCAD`) và 100% Unit Tests PASS.

