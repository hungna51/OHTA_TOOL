# 03_Guro — Tab **Guro** (lệnh GRO)

Bộ lệnh **GURO** (đánh số bango, gán mã khắc, tạo F-name, xuất CSV, in PDF, chuyển layer nét) đã chuyển vào OHTA TOOL từ 11/2026 (mục 191). Chạy trên **ZWCAD 2026** và **AutoCAD 2027**.

## Cấu trúc

```
03_Guro/
├── module.json                  Tab "Guro": tiêu đề, màu; nút GRO, GI, DST chỉ dùng khi không nạp được giao diện tab
├── lisp/autoload.lsp            Lệnh mồi: OHTA_Loader.lsp nạp ở MỌI bản vẽ; lần đầu gõ GRO/XLN/DST... mới nạp guro_gro.lsp
├── lisp/guro_gro.lsp            Lisp GRO (bản DUY NHẤT còn sửa — trước là ZWLISP\GURO\Guro ZWCAD(gro).lsp)
├── plot/gro.ctb                 Bảng kiểu in; lisp tự chép vào thư mục plot style của CAD khi thiếu
├── plugin/GroTool.Plugin/       Nguồn C# của GROTOOL.dll (trước là laptrinhlisp\ZwCadPlugin → GROZWCAD.dll)
├── plugin/GroTool.Tests/        571 unit test phần Core/ (chạy không cần CAD)
├── bin/ZwCAD/GROTOOL.dll        Bản ZWCAD 2026 (net48)      — sinh bởi build_ohta.ps1, không đưa lên git
├── bin/AutoCAD/GROTOOL.dll      Bản AutoCAD 2027 (net10)    — sinh bởi build_ohta.ps1, không đưa lên git
└── rules/                       Luật riêng GURO (00 = mục lục cũ, 01..16)
```

## Cách hoạt động

1. `OHTA_Loader.lsp` (trong Startup Suite) nạp `03_Guro\lisp\autoload.lsp` ở mỗi bản vẽ → các lệnh `GRO, GROCHAY, GI, XLN, CLN, XLNDS, DS, DSN, DST, GRODS, GROKHUNG, GROTAM, FIXFD, NETDST, NETRELOAD` gõ được ngay.
2. Lần đầu gọi lệnh, `autoload.lsp` nạp `guro_gro.lsp` (tìm thư mục `03_Guro` theo thứ tự: `*gro-module-dir*` do nút tab đặt sẵn → `*ohta-root-dir*` → registry `OFT_Lisp\OhtaTool\RootDir` → Support Path — mục 198, máy không đặt OHTA_TOOL trong Support Path vẫn chạy); file này tự NETLOAD `bin\<ZwCAD|AutoCAD>\GROTOOL.dll` (`gro--tim-dll`). Trên ZWCAD còn tìm thêm `C:\GROZWCAD\GROZWCAD.dll` cũ.
3. Nút trên tab Guro nạp `autoload.lsp` rồi gọi lệnh — không nạp lại file 650 KB mỗi lần bấm.
4. **Tab Guro** (mục 192–194): giao diện nằm trong GROTOOL.dll (`GuroOhtaTab.TaoGiaoDien`); OHTATOOL.dll nạp DLL đó và gắn vào bảng — xem [ADR 0003](../_docs/adr/0003-tab-guro-nam-trong-grotool.md). Mặt tab chọn layer khung bao / vùng / gốc **cho bản vẽ đang mở**; nút ⚙ mở **Cài đặt GURO** — tab Chung (khung tấm + tên layer, in PDF, xếp cụm, mã khắc, xử lý nét) và tab Ghi chú (từ điển Nhật → Việt). Gõ `GRO` chỉ mở tab, nút ▶ Chạy GRO gửi `GROCHAY`. Không còn hộp thoại cài đặt nào.
5. **Không** cài thêm `Guro ZWCAD(gro).lsp` cũ vào Startup Suite song song — hai bản sẽ đè lệnh của nhau.

## Khác biệt ZWCAD / AutoCAD

| Việc | ZWCAD 2026 | AutoCAD 2027 |
|---|---|---|
| DLL | `bin\ZwCAD\GROTOOL.dll` (net48, `#if ZWCAD`) | `bin\AutoCAD\GROTOOL.dll` (net10.0-windows, `#if AUTOCAD`) |
| Máy in PDF (`gro--may-in-pdf`) | `DWG to PDF.pc5` | `DWG To PDF.pc3` |
| Thư mục ctb (`gro--thu-muc-ctb`) | `<ROAMABLEROOTPREFIX>\Printstyle\` | `PrinterStyleSheetDir` |
| Nạp lisp lần đầu | — | AutoCAD hỏi SECURELOAD: thêm thư mục `OHTA_TOOL` vào **Trusted Locations** (cả cây con) để khỏi hỏi |

Mã C#: mỗi file dùng `#if ZWCAD using ZwSoft.ZwCAD.X; #else using Autodesk.AutoCAD.X; #endif`; chỗ ghi đầy đủ kiểu dùng alias `CadColor`, `CadColorDialog`, `CadApplication`, `CadObjectId`. Thư mục `Core/` thuần C#, không phụ thuộc CAD.

## Build & kiểm thử

```
powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1
```
Build OHTATOOL + GROTOOL cho cả 2 nền tảng, chạy `OhtaTool.Tests` + `GroTool.Tests`, kiểm tra lisp ASCII / cân bằng ngoặc.

## Quy ước (giữ từ GURO)

- Lisp 100% ASCII: chữ Nhật / ký hiệu trong chuỗi dùng escape `\U+XXXX`, trong chú thích ghi `U+XXXX`.
- Sửa lisp / C# thì cập nhật rule tương ứng trong `rules/` cùng lần làm.
- Tra [rules/03-ban-do-ham.md](rules/03-ban-do-ham.md) trước khi viết hàm mới.
- Bản đầy đủ cũ `guro ZWCAD(BG MK FN).lsp` (bg, mk, FN, lệnh chẩn đoán grosort/grodbg) **không** chuyển sang; lệnh FN nay do tab Fname của OHTA đảm nhận.
