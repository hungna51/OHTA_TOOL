# 01. Kiến Trúc Tổng Thể & Luật Phân Tầng Dự Án OHTA TOOL

> Thuộc bộ luật chung cấp dự án: [../GEMINI.md](../GEMINI.md)

---

## 1. Triết Lý Thiết Kế: "1 Thư Mục Con = 1 Nhóm Nghiệp Vụ = 1 Tab Palette"

Dự án **OHTA TOOL** tách biệt hoàn toàn **Khung hiển thị (Host Infrastructure)** khỏi **Các nhóm công cụ nghiệp vụ (Functional Modules)**:

```text
OHTA_TOOL/
├── GEMINI.md / README.md       <-- Định hướng chung toàn dự án
├── rules/                      <-- Luật chung bắt buộc cho mọi nhóm
├── OHTA_Loader.lsp             <-- Điểm vào duy nhất từ CAD (lệnh OHTA)
├── _system/                    <-- Lõi C# PaletteSet + Unit Tests (Tiền tố "_" -> Ẩn khỏi Tab)
├── _Template_Module/           <-- Mẫu chuẩn để tạo nhóm mới (Tiền tố "_" -> Ẩn khỏi Tab)
│
├── 01_Ten_Nhom_A/              <-- Mục con 1 ==> Tự sinh Tab dọc 1 trên OHTA Tool Palettes
│   ├── README.md               <-- Đặc tả riêng của Nhóm A
│   ├── rules/                  <-- Luật nghiệp vụ riêng của Nhóm A
│   ├── module.json             <-- Cấu hình Tab & danh sách công cụ Nhóm A
│   └── lisp/                   <-- Mã nguồn .lsp / .dcl riêng của Nhóm A
│
└── 02_Ten_Nhom_B/              <-- Mục con 2 ==> Tự sinh Tab dọc 2 trên OHTA Tool Palettes
    ├── README.md
    ├── rules/
    ├── module.json
    └── lisp/
```

---

## 2. Quy Tắc Quét Thư Mục Sinh Tab (`ModuleDiscovery`)

Bộ quét `Core/ModuleDiscovery.cs` tự động duyệt các thư mục con cấp 1 nằm trực tiếp bên trong `OHTA_TOOL/` theo quy tắc cứng:

1. **Các thư mục BỊ BỎ QUA (Không sinh Tab)**:
   - Thư mục có tên `rules` (không phân biệt hoa thường).
   - Mọi thư mục bắt đầu bằng dấu gạch dưới `_` (ví dụ: `_system`, `_Template_Module`, `_backup`, `_archive`).
   - Mọi thư mục bắt đầu bằng dấu chấm `.` (ví dụ: `.git`, `.vs`, `.vscode`).
   - Thư mục có `"enabled": false` trong file `module.json`.

2. **Các thư mục ĐƯỢC NHẬN LÀM TAB**:
   - Mọi thư mục con cấp 1 còn lại trong `OHTA_TOOL/`.
   - **Có `module.json`**: Hiển thị đúng `tabTitle`, `accentColor`, `order` và danh sách nút lệnh `tools` khai báo trong `module.json`.
   - **Chưa có `module.json`**: Hệ thống **vẫn tự sinh Tab** để người dùng kiểm tra bố cục ngay khi vừa tạo thư mục! Tên Tab được lấy từ dòng tiêu đề `# ...` đầu tiên của `README.md` (nếu có) hoặc chuyển đổi tự động từ tên thư mục (ví dụ `03_KiemTra_BienDang` → `03. KiemTra BienDang`).

3. **Thứ tự sắp xếp các Tab từ trên xuống dưới**:
   - Ưu tiên 1: Giá trị số nguyên `"order"` trong `module.json` (tăng dần). Các thư mục chưa khai báo `order` mặc định nhận `order = 1000`.
   - Ưu tiên 2: So sánh tên thư mục theo thứ tự từ điển không phân biệt hoa thường (`01_...`, `02_...`, `03_...`).
   - **Khuyến nghị đặt tên thư mục con**: Luôn dùng tiền tố 2 chữ số `01_`, `02_`, `03_` và chỉ dùng ký tự ASCII không dấu, nối bằng gạch dưới `_` (tránh dấu cách và dấu tiếng Việt trong tên thư mục để tuyệt đối an toàn khi gọi hàm `(load ...)` của AutoLISP).

---

## 3. Luật Cô Lập & Kế Thừa Giữa Các Nhóm

1. **Kế thừa Luật**:
   - Luật trong `OHTA_TOOL/rules/` là luật nền (tương thích CAD, encoding, an toàn bộ nhớ). Luật riêng trong `<Muc_Con>/rules/` cụ thể hóa nghiệp vụ cho nhóm đó nhưng **không được vi phạm luật nền**.
2. **Không phụ thuộc chéo ngầm**:
   - File `.lsp` của `01_...` không được gọi trực tiếp hàm nội bộ riêng tư của `02_...`.
   - Đặt tiền tố hàm nội bộ theo mã nhóm để tránh đụng độ không gian tên toàn cục của AutoLISP:
     - Hàm khung chung: `ohta--<ten-ham>`
     - Hàm riêng nhóm 01: `ohta-m01--<ten-ham>` (hoặc tiền tố ngắn được quy định trong `rules/` riêng của nhóm đó).
3. **Tách biệt Logic Thuần (`Core/`) khi viết C#**:
   - Mọi thuật toán xử lý chuỗi, hình học, đọc cấu hình trong C# phải đặt ở `_system/OhtaTool.Plugin/Core/` (không `using` namespace của ZWCAD hay AutoCAD) và có Unit Test tương ứng trong `_system/OhtaTool.Tests/`.

