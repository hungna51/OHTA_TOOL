# GROTOOL.dll để riêng trong 03_Guro, không gộp vào OHTATOOL.dll

Khi đưa Guro vào OHTA TOOL (11/2026), phần .NET của lệnh GRO (khoảng 14 000 dòng C#, 560 test, bảng DST riêng) được giữ thành DLL riêng `03_Guro/bin/<ZwCAD|AutoCAD>/GROTOOL.dll` thay vì gộp vào `OHTATOOL.dll` của khung `_system/`. Lý do: gộp vào là việc lớn và dễ hỏng một bộ lệnh đang chạy ổn; để riêng thì Guro nâng cấp, build và nạp lại độc lập với khung OHTA, đúng nguyên tắc cô lập mục con. Cái giá phải chịu: CAD nạp hai DLL, có hai bảng (OHTA và DST), và `build_ohta.ps1` phải build cả hai.

## Considered Options

- **Gộp vào OHTATOOL.dll, bảng DST thành một tab của OHTA**: chỉ còn một DLL và một bảng, nhưng phải chuyển 60 file C# vào `_system/` và trộn hai bộ PaletteSet. Không chọn.
- **Giữ GROZWCAD.dll ở `C:\GROZWCAD` như cũ**: không phải làm gì, nhưng không nằm trong git, không đóng gói cùng OHTA và chỉ chạy trên ZWCAD. Không chọn.
