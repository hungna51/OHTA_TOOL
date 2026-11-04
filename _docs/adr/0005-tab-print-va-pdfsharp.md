# Tab PRINT nằm trong OHTATOOL.dll, gộp PDF bằng PdfSharp đặt cạnh DLL

Từ 11/2026, lệnh in khung `OI` (lisp `innhanh(OI)-CaNhan.lsp`) chuyển thành tab **PRINT** (`04_In`, lệnh `OI` v2.0) viết bằng C#. Giao diện và logic nằm ngay trong **OHTATOOL.dll** (đăng ký qua `ModuleViewRegistry`), không tách DLL riêng như Guro ([ADR 0001](0001-grotool-dll-tach-rieng.md)): tính năng nhỏ, không có lisp lớn đi kèm, tách DLL chỉ thêm việc build và phát hành.

In dùng API `PlotEngine` (có cả trên ZWCAD 2026 và AutoCAD) thay chuỗi `-PLOT`: in đồng bộ, không lệch prompt. Gộp các PDF từng khung bằng **PDFsharp 6.2.4** (MIT) thay Ghostscript, để máy không cần cài thêm gì.

Hệ quả:
- `OhtaTool.Plugin.csproj` bật `CopyLocalLockFileAssemblies`; `build_ohta.ps1` chép mọi `*.dll` của bản build ra `_system/bin/<ZwCAD|AutoCAD>/` cạnh `OHTATOOL.dll` (bản ZWCAD kéo theo cả `System.Memory`, `System.Buffers`… vì PdfSharp dùng bản netstandard2.0). `release_ohta.ps1` đưa các DLL đó vào zip; trình cập nhật xử lý chúng như DLL chính (đang khoá thì đổi tên `.old_*`).
- Plugin không có `app.config` nên ZWCAD (net48) không có binding redirect: `Services/PluginAssemblyResolver.cs` nạp DLL phụ thuộc từ thư mục của `OHTATOOL.dll` bất kể phiên bản. Chỉ `Services/PdfSharpMerge.cs` tham chiếu kiểu PdfSharp, gọi qua `PdfMerger` để resolver đăng ký trước khi JIT chạm tới chúng.

## Considered Options

- **Ghostscript như lisp cũ**: phải cài trên từng máy, đường dẫn ghi cứng phiên bản. Không chọn.
- **Gửi lệnh `-PLOT` từ C#**: bất đồng bộ khi gọi từ palette, khó biết lúc in xong để gộp; dễ lệch prompt giữa ZWCAD/AutoCAD. Không chọn.
- **DLL riêng cho tab PRINT**: xem trên. Không chọn.
