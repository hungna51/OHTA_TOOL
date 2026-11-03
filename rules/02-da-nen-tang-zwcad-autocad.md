# 02. Chuẩn Đa Nền Tảng: ZWCAD 2026 & AutoCAD

> Thuộc bộ luật chung cấp dự án: [../GEMINI.md](../GEMINI.md)

Dự án **OHTA TOOL** biên dịch và vận hành song song trên cả **ZWCAD 2026** và **AutoCAD**. Mọi đoạn mã C# và AutoLISP viết trong dự án phải tuân thủ nghiêm ngặt các quy tắc tương thích dưới đây.

---

## 1. Sự Khác Biệt Runtime `.NET` & Giải Pháp Multi-Targeting

| Tiêu chí | ZWCAD 2026 | AutoCAD (2027 trên máy hiện tại) |
|---|---|---|
| **Target Framework** | `net48` (.NET Framework 4.8, `x64`) | `net10.0-windows` (.NET 10.0, `x64`) *(hoặc `net8.0-windows` cho AutoCAD 2025–2026 — `build_ohta.ps1` tự chọn theo bản AutoCAD đang cài)* |
| **Hằng biên dịch** | `ZWCAD` | `AUTOCAD` |
| **Thư viện SDK** | `ZwDatabaseMgd.dll`, `ZwManaged.dll` | `accoremgd.dll`, `acdbmgd.dll`, `acmgd.dll` |
| **Root Namespace SDK** | `ZwSoft.ZwCAD.*` | `Autodesk.AutoCAD.*` |
| **Đường dẫn DLL xuất ra** | `_system/bin/ZwCAD/OHTATOOL.dll` | `_system/bin/AutoCAD/OHTATOOL.dll` |

### Quy tắc viết C# dùng chung 100% mã nguồn
1. **Không `using` trực tiếp `ZwSoft.ZwCAD.*` hay `Autodesk.AutoCAD.*` rải rác ở các file UI/Command**:
   - Mọi ánh xạ kiểu dữ liệu CAD đều đi qua `_system/OhtaTool.Plugin/CadCompat.cs` (`CadApp`, `CadDocument`, `[CommandMethod]`, `[LispFunction]`, `PaletteSet`, `PaletteSetStyles`...).
2. **Không dùng NuGet ngoài sinh thêm DLL phụ thuộc (`CopyLocalLockFileAssemblies = false`)**:
   - Cả ZWCAD và AutoCAD khi `NETLOAD` một file DLL nằm ngoài thư mục cài đặt CAD rất dễ gặp lỗi `FileNotFoundException` nếu phụ thuộc `Newtonsoft.Json` hay `System.Text.Json` (trên `net48`).
   - Luôn dùng các thư viện có sẵn trong BCL của cả hai nền tảng (như `System.Runtime.Serialization.Json.DataContractJsonSerializer`, `System.Xml.Linq`, `System.Windows.*`).
3. **Bắt buộc khóa Document khi thao tác từ Modeless PaletteSet**:
   - Bảng `OHTA TOOL` là giao diện **Modeless** (không chặn màn hình CAD). Bất kỳ thao tác nào đọc/ghi `Database` hoặc đổi `Layer` trực tiếp từ sự kiện bấm nút WPF **PHẢI** bọc trong:
     ```csharp
     using (CadCompat.LockActiveDocument())
     {
         // Thao tác Transaction CAD ở đây
     }
     ```
   - Với thao tác kích hoạt lệnh CAD thông thường (để người dùng chọn điểm/đối tượng ngoài màn hình), ưu tiên dùng `CadCompat.SendCommand(macro)` (`SendStringToExecute`) — CAD sẽ tự chuyển ngữ cảnh về Command Line an toàn mà không bị `eLockViolation`.
4. **Nhúng WPF vào `PaletteSet` qua `ElementHost` (`Dock = DockStyle.Fill`)**:
   - **Cấm dùng `PaletteSet.AddVisual` trên ZWCAD**: ZWCAD đo chiều cao theo nội dung vô hạn khiến `ScrollViewer` / `DataGrid` bị cắt mất phần dưới và không hiện thanh cuộn.
   - Luôn bọc `UserControl` WPF bên trong `System.Windows.Forms.Integration.ElementHost` với `Dock = DockStyle.Fill`, sau đó gọi `_paletteSet.Add(tabTitle, host)`. Cách này chạy chuẩn xác 100% trên cả ZWCAD 2026 lẫn AutoCAD 2027.
5. **Bắt sự kiện con lăn chuột (`PreTranslateMessage`)**:
   - Trên CAD, khi focus đang nằm ngoài vùng vẽ, lăn chuột trên bảng Palette có thể làm zoom màn hình bản vẽ thay vì cuộn danh sách nút lệnh.
   - `OhtaPaletteCommands` móc `CadApp.PreTranslateMessage` kiểm tra tọa độ con trỏ qua `WindowFromPoint`: nếu con trỏ nằm trên bất kỳ `ElementHost` nào đang hiển thị của `OHTA TOOL`, chuyển xung cuộn vào `ScrollViewer` của Tab đó và đặt `e.Handled = true`.

---

## 2. Quy Tắc Tương Thích AutoLISP Giữa ZWCAD & AutoCAD

1. **Cấm tuyệt đối hàm Express Tools (`acet-*`)**:
   - ZWCAD không có bộ thư viện `acet-*` (`acet-str-find`, `acet-ui-progress`, `acet-geom-*`...). Chỉ sử dụng hàm AutoLISP chuẩn và `vl-*` / `vla-*` sau khi gọi `(vl-load-com)`.
2. **Bộ lọc `ssget` nhiều loại thực thể**:
   - Tránh viết tắt gây lỗi trên một số bản build ZWCAD; khi lọc nhiều loại đối tượng phức tạp nên dùng `(-4 . "<OR") ... (-4 . "OR>")`.
3. **Tạo `MTEXT` bằng `entmake`**:
   - Chỉ truyền mã căn lề DXF group `71` cho `MTEXT`, không truyền kèm group `72`/`73` của `TEXT` đơn để tránh nhảy chữ lệch tọa độ giữa AutoCAD và ZWCAD.
4. **Bọc bảo vệ các lời gọi COM/ActiveX**:
   - Luôn có `(vl-load-com)` ở đầu file `.lsp` và bọc các lời gọi `vla-*` nhạy cảm bằng `(vl-catch-all-apply '...)` (truyền đủ 2 đối số: hàm và danh sách tham số).
5. **Đường dẫn file trong LISP**:
   - Luôn chuyển dấu `\` thành `/` bằng `(vl-string-translate "\\" "/" path)` khi ghép chuỗi macro LISP gửi xuống dòng lệnh (trừ tham số truyền thẳng vào lệnh `NETLOAD` trên Windows dùng `\\`).

