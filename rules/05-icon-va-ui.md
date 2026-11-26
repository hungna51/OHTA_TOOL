# 05. Quy Chuẩn Icon & Giao Diện (`koboyo.com`)

> Thuộc bộ luật chung cấp dự án: [../GEMINI.md](../GEMINI.md)

Tài liệu quy định phong cách biểu tượng (icon) và cách nhúng vào giao diện WPF (`.xaml`) cũng như bản xem trước HTML của hệ thống **OHTA TOOL**.

---

## 1. Nguồn Tham Chiếu & Phong Cách Icon

- **Nguồn chuẩn**: [koboyo.com](https://koboyo.com) — ưu tiên phong cách **Original / Geometric hand-drawn sketch** (nét phác tay gọn gàng, hơi cong nhẹ tự nhiên, rõ nghĩa kỹ thuật).
- **Lý do**: Phù hợp thẩm mỹ bản vẽ kỹ thuật CAD, đồng bộ sắc nét trên cả hai nền tảng **ZWCAD 2026** (`.NET 4.8`) và **AutoCAD 2027** (`.NET 10.0`), không bị vỡ hình ở DPI cao (125%–200%).

---

## 2. Quy Tắc Bắt Buộc Khi Thiết Kế Giao Diện Mới

1. **Không dùng Emoji màu mè làm icon nút / trạng thái / tiêu đề**
   - Không dùng `📋`, `📂`, `📥`, `🔁`, `🤖`, `✎`, `⚙`, `🗑`, `📌`, `✨`, `⚠`, `🔍`, `📝` trong XAML hoặc HTML mới.
   - Ký tự chữ thuần giữ lại nếu là nhãn nút hệ thống cửa sổ rất nhỏ hoặc dấu phân tách văn bản (nhưng ưu tiên SVG/Path cho mọi nút thao tác).
2. **Vector đơn sắc theo ngữ cảnh (`currentColor` / `Foreground`)**
   - Icon kế thừa trực tiếp màu chữ của thành phần chứa:
     - Trắng (`#FFFFFF`) trên nút chính nền màu nhấn (`AccentBrush`).
     - Xám xanh đậm (`#334E68`) trên nút phụ nền trắng.
     - Đỏ (`#B42318`) cho thao tác xoá / huỷ / báo lỗi.
     - Vàng hổ phách (`#92400E` / `#B54708`) cho cảnh báo / xác nhận.
3. **Thông số hình học đồng nhất**
   - Khung chuẩn: `viewBox="0 0 24 24"`.
   - Kích thước hiển thị: `14×14` px (trong badge/chip nhỏ), `15×15` hoặc `16×16` px (trên nút bấm), `18×18` px (trên header).
   - Đầu nét & góc nối bo tròn: `StrokeStartLineCap="Round"`, `StrokeEndLineCap="Round"`, `StrokeLineJoin="Round"` (WPF) và `stroke-linecap="round" stroke-linejoin="round"` (SVG).
   - Độ dày nét: `1.75` – `2.0`.
   - Đặc trưng phác tay kiểu Koboyo: dùng đường cong `C` / `Q` nhẹ thay cho góc vuông cứng nhắc tuyệt đối, giữ nét thoáng để đọc rõ ở cỡ `14px`.

---

## 3. Cách Nhúng Trong WPF (`.xaml`) Đa Nền Tảng

Khai báo `StreamGeometry` trong `<UserControl.Resources>` hoặc `<Window.Resources>` (hoặc `ResourceDictionary` dùng chung) thay vì tạo hàng chục file `.png` rời — không tốn công đóng gói vào `.csproj` và tự co giãn sắc nét theo DPI.

### Mẫu chuẩn XAML

```xml
<UserControl.Resources>
    <Geometry x:Key="IconPaste">
        M8.5,4.2 C8.5,3.3 9.3,2.6 10.2,2.6 L13.8,2.6 C14.7,2.6 15.5,3.3 15.5,4.2
        M9,4.5 L6.8,4.7 C5.8,4.8 5.2,5.6 5.3,6.6 L5.6,19.4 C5.7,20.4 6.5,21.1 7.5,21.1 L16.6,20.9 C17.6,20.9 18.3,20.1 18.4,19.1 L18.7,6.5 C18.7,5.5 18,4.7 17,4.6 L15,4.5
        M9.2,11.2 L14.8,11.1 M9.2,15.1 L13.2,15.0
    </Geometry>
</UserControl.Resources>

<Button Style="{StaticResource NutChinh}">
    <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
        <Path Data="{StaticResource IconPaste}"
              Stroke="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}"
              StrokeThickness="1.8"
              StrokeStartLineCap="Round"
              StrokeEndLineCap="Round"
              StrokeLineJoin="Round"
              Stretch="Uniform"
              Width="15" Height="15"
              Margin="0,0,6,0"
              VerticalAlignment="Center"/>
        <TextBlock Text="Dán ảnh (Ctrl+V)" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

> Lưu ý: Nếu icon có đường nét nằm lệch khỏi khung `0..24`, có thể đặt `Path` bên trong `<Viewbox Width="15" Height="15">` bọc `<Canvas Width="24" Height="24"><Path .../></Canvas>` để giữ đúng tỉ lệ khoảng trắng của `viewBox="0 0 24 24"`.

---

## 4. Từ Điển Icon Cốt Lõi Dùng Chung (Koboyo Sketch Style)

Khi cần thêm icon mới, tìm trên [koboyo.com](https://koboyo.com) (hoặc Koboyo MCP) và chuẩn hoá về `viewBox="0 0 24 24"`, `fill="none"`, `stroke="currentColor"`. Dưới đây là bộ vector cốt lõi dùng cho Tab AI, cửa sổ nổi `Photo to DXF` và các tab mới:

| Tên khóa (`Icon...`) | Ý nghĩa | Đường dẫn SVG `d` (`viewBox 0 0 24 24`) |
|---|---|---|
| `IconPaste` | Dán ảnh / Clipboard | `M8.5,4.2 C8.5,3.3 9.3,2.6 10.2,2.6 L13.8,2.6 C14.7,2.6 15.5,3.3 15.5,4.2 M9,4.5 L6.8,4.7 C5.8,4.8 5.2,5.6 5.3,6.6 L5.6,19.4 C5.7,20.4 6.5,21.1 7.5,21.1 L16.6,20.9 C17.6,20.9 18.3,20.1 18.4,19.1 L18.7,6.5 C18.7,5.5 18,4.7 17,4.6 L15,4.5 M9.2,11.2 L14.8,11.1 M9.2,15.1 L13.2,15.0` |
| `IconFolder` | Chọn tệp / Mở thư mục | `M3.4,7.2 C3.4,5.9 4.3,5.1 5.6,5.1 L9.3,5.1 C10,5.1 10.6,5.5 11.1,6.1 L12.3,7.4 L18.6,7.5 C19.8,7.5 20.6,8.4 20.6,9.6 L20.4,17.5 C20.4,18.7 19.5,19.5 18.3,19.5 L5.6,19.6 C4.3,19.6 3.5,18.7 3.5,17.5 Z` |
| `IconInsert` | Chèn vào bản vẽ CAD | `M12.1,3.5 C12.0,7.8 12.1,11.5 12.0,15.2 M8.4,11.7 C9.8,13.1 11.0,14.5 12.0,15.6 C13.1,14.4 14.4,13.1 15.7,11.8 M4.5,16.2 L4.7,18.6 C4.8,19.6 5.6,20.3 6.6,20.3 L17.4,20.2 C18.4,20.2 19.2,19.5 19.3,18.5 L19.5,16.1` |
| `IconAiCheck` | AI kiểm & tinh chỉnh | `M10.5,4.2 C14.1,4.1 16.9,6.9 16.8,10.5 C16.8,14.1 14.0,16.9 10.4,16.9 C6.8,16.9 4.1,14.1 4.1,10.4 C4.1,6.9 6.9,4.2 10.5,4.2 Z M15.3,15.3 L20.1,20.2 M18.5,3.2 L19.1,5.1 L21.0,5.6 L19.1,6.2 L18.5,8.1 L18.0,6.2 L16.1,5.6 L18.0,5.1 Z` |
| `IconCode` | Code Python (`.py`) | `M8.6,7.4 C6.8,9.1 5.1,10.7 3.7,12.0 C5.2,13.4 6.9,15.0 8.6,16.6 M15.4,7.4 C17.2,9.1 18.9,10.7 20.3,12.0 C18.8,13.4 17.1,15.0 15.4,16.6 M13.4,5.2 L10.6,18.8` |
| `IconRedraw` | Vẽ lại | `M19.5,11.5 C19.3,7.3 15.9,4.2 11.7,4.3 C8.7,4.4 6.1,6.1 4.9,8.7 M4.5,4.9 L4.7,9.0 L8.8,8.7 M4.6,12.8 C5.0,16.9 8.4,19.8 12.5,19.7 C15.5,19.6 18.1,17.8 19.2,15.1 M19.5,19.1 L19.3,15.0 L15.2,15.3` |
| `IconReread` | AI đọc lại | `M6.2,9.2 C6.2,7.8 7.3,6.8 8.7,6.8 L15.3,6.8 C16.7,6.8 17.8,7.8 17.8,9.2 L17.7,16.1 C17.7,17.5 16.6,18.5 15.2,18.5 L8.8,18.5 C7.4,18.5 6.3,17.5 6.3,16.1 Z M12,3.4 L12,6.6 M9.6,11.6 L9.6,12.2 M14.4,11.6 L14.4,12.2 M9.4,15.2 C10.9,16.1 13.1,16.1 14.6,15.2` |
| `IconUndo` | Hoàn tác bản cũ | `M8.8,7.1 L4.4,10.6 L8.8,14.2 M5.0,10.6 L14.1,10.6 C17.2,10.6 19.5,12.8 19.5,15.6 C19.5,18.0 17.6,19.6 14.9,19.6 L11.2,19.6` |
| `IconPin` | Ghim cửa sổ nổi | `M9.2,4.4 L15.2,4.5 L14.4,9.8 L17.3,12.7 L17.2,14.1 L6.9,14.1 L6.8,12.7 L9.8,9.8 Z M12.0,14.2 L12.0,20.5` |
| `IconSidebar` | Thu gọn / mở cột trái | `M4.2,5.3 C4.2,4.4 4.9,3.8 5.8,3.8 L18.2,3.8 C19.1,3.8 19.8,4.4 19.8,5.3 L19.8,18.7 C19.8,19.6 19.1,20.2 18.2,20.2 L5.8,20.2 C4.9,20.2 4.2,19.6 4.2,18.7 Z M9.6,4.0 L9.5,20.0` |
| `IconSettings` | Cài đặt (`OhtaTeams`) | `M12.0,8.7 C13.8,8.7 15.3,10.2 15.3,12.0 C15.3,13.8 13.8,15.3 12.0,15.3 C10.2,15.3 8.7,13.8 8.7,12.0 C8.7,10.2 10.2,8.7 12.0,8.7 Z M12,3.3 L12,5.4 M12,18.6 L12,20.7 M3.3,12 L5.4,12 M18.6,12 L20.7,12 M5.9,5.9 L7.4,7.4 M16.6,16.6 L18.1,18.1 M18.1,5.9 L16.6,7.4 M7.4,16.6 L5.9,18.1` |
| `IconTrash` | Xoá | `M4.8,6.8 L19.2,6.7 M9.2,6.6 L9.6,4.3 C9.7,3.9 10.1,3.6 10.5,3.6 L13.5,3.6 C13.9,3.6 14.3,3.9 14.4,4.3 L14.8,6.6 M7.1,7.0 L7.8,18.7 C7.9,19.7 8.7,20.4 9.7,20.4 L14.3,20.4 C15.3,20.4 16.1,19.7 16.2,18.7 L16.9,7.0 M10.4,10.5 L10.6,16.6 M13.6,10.5 L13.4,16.6` |
| `IconWarn` | Cảnh báo / Cần xác nhận | `M12.0,4.1 C12.5,4.1 13.0,4.4 13.3,4.9 L20.3,17.8 C20.8,18.7 20.1,19.7 19.1,19.7 L4.9,19.7 C3.9,19.7 3.2,18.7 3.7,17.8 L10.7,4.9 C11.0,4.4 11.5,4.1 12.0,4.1 Z M12.0,9.6 L12.0,13.9 M12.0,16.8 L12.0,17.1` |
| `IconCheck` | Hoàn tất / Đã chèn | `M5.2,12.6 C7.1,14.5 8.8,16.3 10.1,17.7 C12.9,13.8 15.9,9.9 19.1,6.4` |
| `IconZoom` | Phóng to ảnh | `M10.6,4.5 C14.0,4.5 16.7,7.2 16.7,10.6 C16.7,14.0 14.0,16.7 10.6,16.7 C7.2,16.7 4.5,14.0 4.5,10.6 C4.5,7.2 7.2,4.5 10.6,4.5 Z M15.1,15.1 L19.6,19.7 M8.3,10.6 L12.9,10.6 M10.6,8.3 L10.6,12.9` |
| `IconClose` | Đóng / Huỷ | `M6.4,6.5 C9.8,9.9 14.1,14.2 17.6,17.5 M17.5,6.4 C14.1,9.9 9.8,14.2 6.5,17.6` |
| `IconMore` | Menu thêm (`⋯`) | `M6.2,12.0 C6.2,12.4 5.9,12.7 5.5,12.7 C5.1,12.7 4.8,12.4 4.8,12.0 C4.8,11.6 5.1,11.3 5.5,11.3 C5.9,11.3 6.2,11.6 6.2,12.0 Z M12.7,12.0 C12.7,12.4 12.4,12.7 12.0,12.7 C11.6,12.7 11.3,12.4 11.3,12.0 C11.3,11.6 11.6,11.3 12.0,11.3 C12.4,11.3 12.7,11.6 12.7,12.0 Z M19.2,12.0 C19.2,12.4 18.9,12.7 18.5,12.7 C18.1,12.7 17.8,12.4 17.8,12.0 C17.8,11.6 18.1,11.3 18.5,11.3 C18.9,11.3 19.2,11.6 19.2,12.0 Z` |
