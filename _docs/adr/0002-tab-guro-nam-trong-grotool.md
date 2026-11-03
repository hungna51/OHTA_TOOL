# Giao diện tab Guro nằm trong GROTOOL.dll, OHTA nạp bằng phản chiếu

Từ GURO v1.5 (11/2026), thiết lập của lệnh GRO chuyển từ hộp thoại lên tab Guro của bảng OHTA (giống tab Fname). Giao diện tab (`GuroTabControl`, `GuroTabViewModel`) viết trong **GROTOOL.dll**; OHTATOOL.dll chỉ nạp `03_Guro\bin\<ZwCAD|AutoCAD>\GROTOOL.dll` rồi gọi `ZwCadPlugin.GuroOhtaTab.TaoGiaoDien(tieuDe, moTa, mauNhan, thuMucModule)` bằng phản chiếu (`Views/GuroTabLoader.cs`). Hai DLL không tham chiếu nhau khi build, nên vẫn giữ đúng [ADR 0001](0001-grotool-dll-tach-rieng.md): Guro sửa, build, phát hành riêng. Hợp đồng giữa hai bên chỉ là tên lớp, tên hàm và 4 tham số chuỗi — đổi chữ ký thì sửa cả hai phía. Không nạp được DLL thì tab hiện danh sách nút trong `module.json`.

## Considered Options

- **Viết lại giao diện trong OHTATOOL.dll**, cùng đọc/ghi khóa registry `OFT_Lisp\GRO`: dễ làm hơn nhưng có hai bản giao diện thiết lập GRO (tab + hộp thoại `GROCD`) phải sửa song song ở hai DLL. Không chọn.
- **Gộp hai DLL làm một**: đi ngược ADR 0001. Không chọn.
