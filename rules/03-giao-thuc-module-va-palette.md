# 03. Giao Thức Module (`module.json`) & Mở Rộng Tab Palette

> Thuộc bộ luật chung cấp dự án: [../GEMINI.md](../GEMINI.md)

Mỗi thư mục con trong `OHTA_TOOL/` tương ứng với một **Tab sườn dọc** trên bảng **Tool Palettes** khi gõ lệnh `OHTA`. Hệ thống hỗ trợ 2 cấp độ tích hợp từ đơn giản (không cần sửa C#) đến nâng cao (giao diện WPF riêng).

---

## 1. Cấu Trúc Chuẩn Của File `module.json`

Đặt file `module.json` (mã hóa **UTF-8 không BOM hoặc có BOM đều được**) ngay bên trong thư mục mục con, ví dụ `OHTA_TOOL/01_Module_Mau_1/module.json`:

```json
{
  "id": "module_01",
  "enabled": true,
  "order": 10,
  "tabTitle": "01. Nhóm Mẫu 1",
  "title": "Nhóm Chức Năng Mẫu 01",
  "description": "Mô tả ngắn gọn nhiệm vụ của nhóm chức năng này.",
  "accentColor": "#1E88E5",
  "startupLisp": [
    "lisp/ohta_m01_demo.lsp"
  ],
  "tools": [
    {
      "id": "m01_demo",
      "category": "Công cụ cơ bản",
      "name": "Chạy Lệnh Mẫu 01",
      "badge": "M1",
      "command": "OHTAM1",
      "lispFile": "lisp/ohta_m01_demo.lsp",
      "description": "Mô tả chi tiết hiện lên khi rê chuột hoặc dưới tên công cụ."
    }
  ]
}
```

### Giải thích các trường dữ liệu

| Trường | Bắt buộc? | Ý nghĩa |
|---|---|---|
| `id` | Khuyến nghị | Mã định danh duy nhất (ASCII) của nhóm; dùng để ánh xạ nếu nhóm có giao diện WPF tùy biến riêng trong `ModuleViewRegistry`. |
| `enabled` | Tùy chọn | Mặc định `true`. Đặt `false` nếu muốn tạm ẩn Tab của thư mục này mà không cần xóa thư mục. |
| `order` | Tùy chọn | Số thứ tự ưu tiên hiển thị Tab từ trên xuống dưới (nhỏ xếp trước, mặc định `1000`). |
| `tabTitle` | Khuyến nghị | Nhãn ngắn gọn hiển thị trực tiếp trên **dải Tab dọc** của CAD PaletteSet (nên dài từ 6–20 ký tự để dễ đọc). |
| `title` | Tùy chọn | Tiêu đề đầy đủ hiển thị trên phần Header bên trong Tab. |
| `description` | Tùy chọn | Dòng mô tả nhiệm vụ của nhóm nằm dưới tiêu đề Header. |
| `accentColor` | Tùy chọn | Mã màu Hex (`#RRGGBB`, ví dụ `#1E88E5`, `#2E7D32`, `#EF6C00`, `#6A1B9A`) làm màu nhận diện Header & Icon Badge của nhóm. |
| `startupLisp` | Tùy chọn | Danh sách đường dẫn tương đối (tính từ thư mục mục con) của các file `.lsp` muốn tự nạp sẵn khi mở bảng `OHTA`. |
| `tools` | Tùy chọn | Danh sách các nút công cụ hiển thị trong Tab. Để mảng rỗng `[]` khi nhóm đang trong giai đoạn thiết kế ban đầu. |

### Giải thích các trường của từng phần tử trong `tools`

| Trường | Ý nghĩa |
|---|---|
| `id` | Mã định danh nút công cụ trong nhóm. |
| `category` | Nhóm con phân mục bên trong Tab (tùy chọn). |
| `name` | Tên hiển thị của nút công cụ (hỗ trợ tiếng Việt có dấu / tiếng Nhật đầy đủ). |
| `badge` | Tối đa 2–4 ký tự ngắn (ví dụ `"M1"`, `"CSV"`, `"KT"`) vẽ trên ô vuông màu bên trái nút giống icon Tool Palettes. |
| `command` | Tên lệnh CAD sẽ gọi (ví dụ `"OHTAM1"`) hoặc macro CAD (nếu bắt đầu bằng `^C^C` hoặc `(` thì gửi nguyên văn). |
| `lispFile` | Đường dẫn tương đối tới file `.lsp` trong thư mục mục con (ví dụ `"lisp/ohta_m01_demo.lsp"`). Khi bấm nút, hệ thống tự động nạp file `.lsp` này trước rồi mới chạy `command` — đảm bảo không bao giờ báo `Unknown command`! |
| `description` | Chú thích ngắn dưới tên nút và hiện đầy đủ trong Tooltip khi rê chuột vào. |

---

## 2. Hai Cấp Độ Phát Triển Chức Năng Cho Một Mục Con

### Cấp độ 1: Thêm Công Cụ Nhanh Bằng LISP + `module.json` (Không Cần Build C#)
Phù hợp cho 90% tiện ích vẽ, kiểm tra, xử lý đối tượng CAD:
1. Đặt file `.lsp` vào `<Muc_Con>/lisp/`.
2. Thêm một mục vào mảng `"tools"` trong `<Muc_Con>/module.json`.
3. Gõ `OHTARELOAD` (hoặc bấm nút `⟳` trên bảng `OHTA TOOL`) → Nút bấm xuất hiện ngay lập tức và sẵn sàng sử dụng!

### Lệnh gõ được ngay ở mọi bản vẽ: `lisp/autoload.lsp`
`OHTA_Loader.lsp` (Startup Suite) nạp `<Muc_Con>/lisp/autoload.lsp` — nếu có — ở **mỗi bản vẽ** (bỏ qua thư mục bắt đầu bằng `_`). Dùng khi lệnh của mục con phải gõ được trên dòng lệnh mà không cần mở bảng `OHTA`.
- Chỉ khai báo **lệnh mồi** nhẹ: `(defun c:XXX () (if (nap-file-that) (c:XXX)) (princ))` — lần đầu gõ mới `load` file lisp lớn, file đó định nghĩa lại `c:XXX` thật.
- Chỉ khai báo lệnh mồi khi file thật **chưa** nạp (cờ do file thật đặt ở cuối), vì nút bấm nạp lại `lispFile` mỗi lần bấm.
- Ví dụ: [`03_Guro/lisp/autoload.lsp`](../03_Guro/lisp/autoload.lsp).

### Cấp độ 2: Giao Diện WPF Chuyên Sâu Cho Riêng Một Tab (`ModuleViewRegistry`)
Khi một mục con cần giao diện tương tác phức tạp hơn danh sách nút bấm (ví dụ: bảng `DataGrid` hai chiều, form nhập thông số trực quan, biểu đồ preview):
1. Tạo `UserControl` WPF mới trong `_system/OhtaTool.Plugin/Views/` (kèm logic thuần trong `Core/`).
2. Đăng ký factory tạo View theo `module.Id` trong `Views/ModuleViewRegistry.cs`:
   ```csharp
   ModuleViewRegistry.Register("module_01", manifest => new MyCustomModule01View(manifest));
   ```
3. Build lại bằng `build_ohta.ps1` → Tab của `01_Module_Mau_1` trên Tool Palettes sẽ hiển thị toàn bộ giao diện WPF chuyên sâu đó trong khi các Tab khác vẫn giữ giao diện danh sách công cụ tiêu chuẩn!

