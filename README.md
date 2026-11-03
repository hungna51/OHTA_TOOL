# Dự Án OHTA TOOL (ZWCAD 2026 & AutoCAD)

Hệ thống công cụ CAD dạng mô-đun (**Modular Tool Palettes**), trong đó mỗi thư mục con đại diện cho một nhóm chức năng độc lập (có tài liệu `.md` và bộ `rules/` riêng) và tự động hiển thị thành một **Tab dọc** trên bảng **Tool Palettes** khi gọi lệnh **`OHTA`**.

---

## 1. Cấu Trúc Thư Mục

```text
OHTA_TOOL/
├── GEMINI.md / CLAUDE.md       # Chỉ dẫn phát triển & bản đồ điều hướng cho AI
├── README.md                   # Hướng dẫn sử dụng, build và thêm nhóm công cụ mới
├── OHTA_Loader.lsp             # File LISP khởi động (tự nhận diện ZWCAD / AutoCAD & nạp DLL)
├── build_ohta.ps1              # Script build 2 bản DLL (.NET) + kiểm tra LISP + chạy Unit Tests
├── rules/                      # Thư mục LUẬT CHUNG áp dụng cho toàn bộ dự án
│
├── _Template_Module/           # Thư mục mẫu để nhân bản khi muốn thêm Tab mới (không hiện Tab)
├── 01_Fname/                   # Mục con 01 -> Tab "Fname" (chọn N=, M=, T=, Q=, Z= & lệnh Fname)
│   ├── README.md               # Tài liệu chức năng riêng của mục Fname
│   ├── rules/                  # Luật lệ nghiệp vụ riêng của mục Fname
│   ├── data/                   # CSV vật liệu + bảng TraV (đồng bộ từ Google Sheet) & google_sheet.ini
│   ├── module.json             # Khai báo Tab "Fname" và các lệnh Fname bổ sung
│   └── lisp/                   # Các file .lsp riêng của nhóm Fname
├── 02_Module_Mau_2/            # Mục con 02 -> Ứng với Tab 2 trên OHTA Tool Palettes
│   └── ...
└── _system/                    # Hạ tầng mã nguồn C# WPF PaletteSet, bin DLL & Unit Tests
    ├── bin/ZwCAD/OHTATOOL.dll  # DLL cho ZWCAD 2026 (.NET Framework 4.8)
    └── bin/AutoCAD/OHTATOOL.dll# DLL cho AutoCAD (.NET 10.0 Windows)
```

---

## 2. Cách Khởi Chạy Trên ZWCAD 2026 Hoặc AutoCAD

1. Mở **ZWCAD 2026** hoặc **AutoCAD 2027** (có thể thêm vào Startup Suite của `APPLOAD`).
2. Kéo-thả file **`OHTA_Loader.lsp`** vào vùng vẽ CAD (hoặc nạp qua lệnh `APPLOAD`).
3. Gõ lệnh **`OHTA`** rồi nhấn `Enter`:
   - Hệ thống tự nhận diện CAD đang chạy, tự động `NETLOAD` đúng bản `OHTATOOL.dll` và mở bảng Tool Palettes **`OHTA TOOL`** neo cạnh màn hình.
   - Mỗi mục con (`01_Module_Mau_1`, `02_Module_Mau_2`, ...) hiển thị thành một **Tab dọc** bên sườn bảng giống hệt Tool Palettes chuẩn của CAD.

### Các lệnh điều khiển nhanh
- `OHTA` : Mở bảng Tool Palettes **OHTA TOOL**.
- `OHTARELOAD` : Quét lại thư mục `OHTA_TOOL/` và làm mới các Tab ngay lập tức (hoặc bấm nút **`⟳`** trên đầu bảng).
- `OHTACLOSE` : Ẩn / hiện nhanh bảng **OHTA TOOL**.
- `OHTAINFO` : Kiểm tra phiên bản, đường dẫn gốc, nền tảng CAD và danh sách các Tab đã nhận diện.
- `OHTAUPDATE` : Kiểm tra ngay bản mới trên GitHub. Bình thường không cần gõ: khi mở bản vẽ mà có bản mới, CAD tự hỏi **Cập nhật ngay / Để sau**. Cập nhật xong đóng và mở lại CAD.
- `OHTAROLLBACK` : Quay về bản trước lần cập nhật gần nhất.

> Muốn tự cập nhật, `OHTA_Loader.lsp` phải nằm trong **Startup Suite** của `APPLOAD`. Lisp / nút bạn tự thêm bằng nút ＋ được giữ nguyên khi cập nhật.

---

## 3. Cách Bổ Sung Một Mục Con (Tab Mới) Trong 30 Giây

1. **Copy thư mục mẫu**: Nhân bản thư mục `_Template_Module` ngay trong `OHTA_TOOL/` và đặt tên không dấu theo thứ tự, ví dụ: `03_TrienKhai_Phoi`.
2. **Cập nhật thông tin nhóm**:
   - Ghi mục tiêu chức năng vào `03_TrienKhai_Phoi/README.md` và quy tắc riêng vào `03_TrienKhai_Phoi/rules/`.
   - Sửa `tabTitle`, `title`, `accentColor` và khai báo các nút lệnh trong `03_TrienKhai_Phoi/module.json` *(ngay cả khi chưa tạo `module.json`, hệ thống vẫn tự nhận thư mục `03_TrienKhai_Phoi` và tạo Tab chờ sẵn!)*.
3. **Làm mới trên CAD**: Bấm nút **`⟳`** trên bảng `OHTA TOOL` hoặc gõ lệnh **`OHTARELOAD`** → Tab mới xuất hiện ngay mà không cần khởi động lại CAD hay build lại DLL!

---

## 4. Cách Biên Dịch Lại Plugin `.NET` & Chạy Kiểm Thử

Khi cần nâng cấp giao diện khung C# trong `_system/OhtaTool.Plugin/`, chạy lệnh sau tại thư mục gốc workspace:

```powershell
powershell -ExecutionPolicy Bypass -File "OHTA_TOOL\build_ohta.ps1"
```

Script sẽ tự động:
1. Build bản **ZWCAD 2026** (`net48`) → xuất ra `OHTA_TOOL\_system\bin\ZwCAD\OHTATOOL.dll`.
2. Build bản **AutoCAD** (`net10.0-windows`) → xuất ra `OHTA_TOOL\_system\bin\AutoCAD\OHTATOOL.dll`.
3. Chạy toàn bộ bộ kiểm thử tự động `OhtaTool.Tests` và kiểm tra cú pháp các file `.lsp`.

