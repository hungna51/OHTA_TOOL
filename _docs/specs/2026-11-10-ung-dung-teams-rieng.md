# Tách tab Teams thành ứng dụng riêng `OhtaTeams.exe`

> Trạng thái: **đã duyệt — xong bước 1** · 2026-11-10 · Thay một phần [ADR 0008](../adr/0008-tab-teams-graph-api.md) (sẽ viết ADR 0009 khi duyệt).

## Mục tiêu

- **Ứng dụng riêng `OhtaTeams.exe`** (chạy ngoài CAD): đăng nhập Microsoft, duyệt Nhóm → Kênh → Tab Lists → Đơn → Chi tiết, như tab Teams hiện nay. Mọi tính năng làm việc với đơn sau này thêm vào đây.
- **Tab Teams trên CAD chỉ còn một việc:** hiện thông tin **đơn đang làm**. CAD không đăng nhập Microsoft, không gọi Graph.

## Đơn đang làm: chọn thế nào

1. **Bản vẽ đang mở nằm trong thư mục của đơn** → hiện đơn đó (ưu tiên).
   Thư mục gốc chọn ở ⚙ (của ứng dụng). Đường dẫn bản vẽ nằm dưới thư mục gốc, một thư mục trên đường đi (sâu ≤ 3 cấp) có tên chứa **Title** của đơn (bỏ dấu, không phân biệt hoa thường) → khớp. Nhiều đơn khớp thì chọn Title dài nhất.
2. **Không khớp** → hiện đơn người dùng bấm **📌 Đang làm** trong ứng dụng.
3. Không có cả hai → tab hiện "Chưa có đơn đang làm" + nút **Mở OhtaTeams**.

Tab tự cập nhật khi đổi bản vẽ đang mở và khi ứng dụng ghi file mới.

## Ứng dụng → CAD: một file

Ứng dụng ghi `%LOCALAPPDATA%\OHTA_TOOL\teams\dang_lam.json` mỗi lần tải xong danh sách hoặc bấm 📌:

```json
{ "luc": "2026-11-10T09:30:00", "thu_muc_goc": "D:\\Don", "don_chon": "123",
  "don": [ { "id": "123", "title": "2026-1234", "han": "2026-11-12", "trang_thai": "…",
             "url": "https://…", "cot": [["Hạn hoàn thành","12/11"], ["…","…"]] } ] }
```

- Chỉ ghi **đơn chưa xong** (+ đơn đã chọn) đã tính sẵn tên người, ngày → CAD không cần biết cột SharePoint.
- Ghi file tạm rồi đổi tên (không để CAD đọc file ghi dở).
- CAD hiện thêm "cập nhật lúc …"; cũ hơn 1 ngày thì tô vàng nhắc mở ứng dụng.

## Tab CAD còn lại

Thẻ đơn: Title, Hạn, Trạng thái, Người triển khai / check, mọi cột còn lại; dòng "khớp theo bản vẽ" hoặc "đơn đã chọn". Nút: **Mở trên Teams**, **📁 Mở thư mục đơn**, **Mở OhtaTeams**.

## Mã

```
_apps/OhtaTeams/                      (có _ nên không thành tab CAD)
├── OhtaTeams.csproj                  WPF net48 (Windows 11 có sẵn 4.8, không cần cài thêm)
├── App.xaml / MainWindow.cs          cửa sổ chứa TeamsTabControl (chuyển sang từ plugin)
└── bin/OhtaTeams.exe
06_Teams/plugin/TeamsTool.Plugin/
├── Core/DonTeams.cs                  giữ; ứng dụng link nguồn (như JsonLite)
├── Core/DangLam.cs                   MỚI: đọc/ghi dang_lam.json, khớp bản vẽ ↔ đơn (thuần, test được)
├── Services/GraphTeams.cs            chuyển sang ứng dụng (link nguồn), plugin không dùng nữa
└── Views/TeamsTabControl.cs          thay bằng thẻ đơn đang làm (nhỏ hơn nhiều)
```

- Màn hình duyệt đơn (`TeamsTabControl`, `KieuTeams`, `TeamsCaiDatWindow`) chuyển nguyên sang ứng dụng, không viết lại.
- Đăng nhập (`token.bin`), `teams.json`, bản lưu tạm giữ nguyên chỗ cũ → máy đã đăng nhập không phải đăng nhập lại.
- `build_ohta.ps1` build thêm `OhtaTeams`; `release_ohta.ps1` đưa `_apps/OhtaTeams/bin/OhtaTeams.exe` (+ DLL cần) vào bản phát hành.

## Các bước

1. ✅ Tạo ứng dụng, dùng màn hình duyệt đơn (link nguồn, chưa xoá khỏi plugin), chạy được độc lập.
2. Ghi `dang_lam.json` + nút 📌 + thư mục gốc ở ⚙.
3. Thu tab CAD về thẻ đơn đang làm, bỏ Graph khỏi plugin.
4. (Sau) Tính năng mới trong ứng dụng: đổi trạng thái (cần quyền `Sites.ReadWrite.All`), mở thư mục / bản vẽ…

## Test

- Khớp bản vẽ ↔ đơn: bỏ dấu, nhiều đơn khớp → Title dài nhất, bản vẽ ngoài thư mục gốc, quá 3 cấp.
- Ưu tiên: khớp bản vẽ > đơn chọn > trống.
- Đọc/ghi `dang_lam.json`: file thiếu, file hỏng, file cũ.
