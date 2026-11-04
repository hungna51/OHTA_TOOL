# 04. Tab Print — lệnh `OI` v2.0 (`04_In`)

> Luật nền chung: [../GEMINI.md](../GEMINI.md) | Mục lục luật riêng: [rules/00-muc-luc.md](rules/00-muc-luc.md)

In bản vẽ ra PDF, thay lisp `innhanh(OI)-CaNhan.lsp`. Toàn bộ viết bằng C# trong **OHTATOOL.dll** (không có lisp) — xem [ADR 0005](../_docs/adr/0005-tab-print-va-pdfsharp.md).

## Lệnh

| Lệnh | Việc |
|---|---|
| `OI` | Mở bảng OHTA, chuyển sang tab Print (thay lệnh lisp `OI` cũ khi OHTATOOL.dll đã nạp). |
| `OIIN` | Lệnh nội bộ: tab ghi yêu cầu in rồi gửi lệnh này để pick điểm / in trong ngữ cảnh lệnh. Không gõ tay. |

`GI` của Guro không đổi.

## Mặt tab

- **Layer khung**: tự chọn layer đầu tiên trong danh sách ưu tiên (⚙) mà bản vẽ có.
- **Cấu hình**: mục đầu "Theo bản vẽ" — từng thông số lấy page setup của layout Model nếu máy có, không thì mặc định ⚙. Chọn cấu hình có tên thì cấu hình thắng bản vẽ; thông số máy không có thì thay (bản vẽ → mặc định → mục đầu máy có) và hiện dòng cảnh báo cam. Nhớ cấu hình chọn lần trước.
- **Máy in / Khổ giấy / Bảng ctb**: điền theo cấu hình, đổi tay chỉ áp cho lần in đó.
- **Chế độ**: Theo khung (2 nút "In tất cả khung", "Chọn khung để in") / Vùng quét (pick 2 góc) / Extents (ngang).
- **Gộp 1 file**, **Mở sau khi in**.

## Luồng in (`PrintCommands.cs`)

1. Bản vẽ chưa lưu → báo, không in.
2. Khung = LWPOLYLINE kín 4 đỉnh trên layer khung, trong Model. Sắp theo Y điểm cao nhất giảm dần, lệch ≤ 1 mm thì trái → phải (`PrintCore.SortFrames`). Khung cao ≥ rộng → trang dọc.
3. Tên: `<DWGPREFIX><tên bản vẽ bỏ -S/-A>.pdf`; không gộp → `<tên>-1.pdf`, `<tên>-2.pdf`… Có file trùng → hỏi **Ghi đè / Lưu thành (1) / Huỷ in**; ghi đè mà file đang mở → lưu `(n)` và báo.
4. Tắt plottable layer `CANH_BAO_*`, `GRO_XEP_NHOM`. In mỗi trang ra một PDF tạm trong `%TEMP%\OHTA_PRINT\<guid>\` (`CadPrintService.PlotPagesToPdfFiles`: thông số + driver máy in dựng một lần), kiểm tra file bắt đầu bằng `%PDF`, rồi gộp bằng PdfSharp. **Không** in nhiều trang trong một document PlotEngine: AutoCAD xuất ra DWF nhiều trang dù máy in là PDF (lỗi thật 04/11/2026). Dòng báo cuối có thời gian in.
5. Mở file (hoặc thư mục khi không gộp).

## Cài đặt

Registry `HKCU\Software\OFT_Lisp\IN`: `defaults`, `frame_layers`, `profiles` (JSON), `last_profile`.

## Mã nguồn

- `_system/OhtaTool.Plugin/Core/PrintCore.cs`, `PrintSettings.cs` — logic thuần, test trong `OhtaTool.Tests` (`Program.Print.cs`).
- `Services/CadPrintService.cs`, `PrintSettingsStore.cs`, `PdfMerger.cs` — CAD, registry, gộp PDF.
- `PrintCommands.cs` — lệnh `OIIN`; `OhtaPaletteCommands.cs` — lệnh `OI`.
- `ViewModels/PrintTabViewModel.cs`, `Views/PrintTabControl.xaml`, `Views/PrintSettingsWindow.cs`, `Views/PrintDialogs.cs`.

## Đã kiểm tra trên

- (chưa) ZWCAD 2026
- (chưa) AutoCAD 2027
