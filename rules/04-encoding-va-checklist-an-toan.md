# 04. Quy Tắc Encoding An Toàn & Checklist Kiểm Thử

> Thuộc bộ luật chung cấp dự án: [../GEMINI.md](../GEMINI.md)

Trên môi trường máy trạm thiết kế kỹ thuật (có cả Windows hệ tiếng Nhật CP932 / Shift-JIS và hệ Unicode tiếng Việt/Anh), lỗi bảng mã (Mojibake) là nguyên nhân hàng đầu làm gãy cú pháp LISP hoặc hỏng hiển thị giao diện.

---

## 1. Quy Định Bảng Mã Theo Từng Loại File

| Loại file | Bảng mã bắt buộc | Quy tắc chi tiết |
|---|---|---|
| **`*.lsp`** | **ASCII thuần (7-bit, KHÔNG BOM)** | **TUYỆT ĐỐI KHÔNG** gõ trực tiếp tiếng Việt có dấu hoặc chữ Nhật trần vào chuỗi hay chú thích của file `.lsp`. Dòng thông báo `princ`/`prompt`/`alert` trong LISP viết tiếng Việt không dấu. Nếu bắt buộc đối chiếu ký tự Nhật trong LISP, dùng mã Unicode / hàm tạo chuỗi từ mã số an toàn. |
| **`*.dcl`** | **ASCII thuần + `\U+XXXX` (KHÔNG BOM)** | Mọi nhãn tiếng Việt có dấu hoặc tiếng Nhật trong file hộp thoại `.dcl` phải viết dưới dạng escape `\U+XXXX`. |
| **`module.json`** | **UTF-8** | Hỗ trợ đầy đủ tiếng Việt có dấu và tiếng Nhật cho `tabTitle`, `title`, `name`, `description` vì được đọc trực tiếp bằng C# `Encoding.UTF8`. |
| **`*.cs`, `*.xaml`** | **UTF-8** | Hỗ trợ đầy đủ chuỗi Unicode tiếng Việt / tiếng Nhật trên giao diện WPF. |
| **`*.md`** | **UTF-8** | Tài liệu hướng dẫn và `rules/` viết tiếng Việt có dấu rõ ràng, mạch lạc. |

---

## 2. Quy Chuẩn An Toàn Cho Mọi File AutoLISP Trong `OHTA_TOOL/`

1. **Khai báo biến cục bộ 100%**:
   ```lisp
   (defun c:OHTAM1 ( / *error* old-ce old-os )
     ...
   )
   ```
   Không để rò rỉ biến tạm ra không gian toàn cục gây xung đột với lệnh khác.
2. **Bẫy lỗi `*error*` khôi phục biến hệ thống**:
   Mọi lệnh có thay đổi `CMDECHO`, `FILEDIA`, `OSMODE`, `CLAYER` đều phải lưu giá trị gốc ở đầu hàm và khôi phục lại ở cả nhánh kết thúc bình thường lẫn trong hàm cục bộ `*error*`.
3. **Đóng mọi File Handle**:
   Mọi lời gọi `(open path mode)` phải kiểm tra khác `nil` trước khi đọc/ghi và luôn gọi `(close f)` ngay khi xong việc.
4. **Kiểm tra cân bằng dấu ngoặc `(` và `)`**:
   Luôn chạy `build_ohta.ps1` (có tích hợp trình kiểm tra cân bằng ngoặc và kiểm tra ký tự ngoài ASCII cho mọi file `.lsp` trong `OHTA_TOOL/`) trước khi nạp vào CAD.

---

## 3. Quy Trình Kiểm Thử Bắt Buộc (`build_ohta.ps1`)

Trước khi bàn giao bất kỳ thay đổi nào trong `OHTA_TOOL/`:
1. Chạy lệnh:
   ```powershell
   powershell -ExecutionPolicy Bypass -File "OHTA_TOOL\build_ohta.ps1"
   ```
2. Đảm bảo cả 3 bước trong script đều báo **[OK]**:
   - **Bước 1 (Build .NET Plugin)**: Xuất thành công `bin/ZwCAD/OHTATOOL.dll` (`net48`) và `bin/AutoCAD/OHTATOOL.dll` không có lỗi biên dịch. Script **tự dò SDK đang cài trên máy**: ZWCAD 2026 lấy từ `InstallLocation` trong registry (vd `D:\ZWCAD`), fallback `C:\Program Files\ZWSOFT\ZWCAD 2026`; AutoCAD chọn bản mới nhất ≥ 2025 trong `C:\Program Files\Autodesk\AutoCAD 20xx` (2027+ → `net10.0-windows`, 2025–2026 → `net8.0-windows`). Nền tảng nào không có SDK sẽ được **bỏ qua kèm cảnh báo `[!]`** và giữ nguyên DLL cũ trong `bin/`. Lưu ý: `bin/AutoCAD/` dùng chung qua OneDrive — DLL xuất ra là bản của AutoCAD trên máy vừa build. *(Lưu ý: Nếu CAD đang mở và đã `NETLOAD` `OHTATOOL.dll`, Windows sẽ khóa file trong `bin/` — khi đó cần đóng CAD rồi chạy lại script để chép bản DLL mới nhất).*
   - **Bước 2 (Unit Tests `OhtaTool.Tests`)**: Toàn bộ ca kiểm thử logic `Core/` đạt 100% PASS.
   - **Bước 3 (Kiểm định LISP)**: Tất cả các file `.lsp` trong `OHTA_TOOL/` đều không có BOM, đạt 100% ASCII thuần và cân bằng ngoặc tuyệt đối.

