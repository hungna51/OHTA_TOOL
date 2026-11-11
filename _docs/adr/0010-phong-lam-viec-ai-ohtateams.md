# AI xử lý đơn chạy trong OhtaTeams (4 phòng), quy chuẩn nội bộ không lên GitHub

Trạng thái: **đề xuất** (chờ duyệt cùng spec). Thiết kế: [_docs/specs/2026-11-11-phong-lam-viec-ai-ohtateams.md](../specs/2026-11-11-phong-lam-viec-ai-ohtateams.md).

- **Chạy trong OhtaTeams.exe, không chạy trong CAD.** Đơn, tệp đính kèm, chuỗi chat và quyền gửi tin đã có sẵn ở ứng dụng (ADR 0009). Đọc bản vẽ PDF / DXF không cần CAD. DWG / STEP chưa đọc hình học.
- **Claude API gọi thẳng bằng HttpClient** (như `ClaudeHttp` của tab AI — ADR 0006), dùng chung key `05_AI\data\ai.key`. Không tham chiếu `AITOOL.dll`; mã thuần dùng chung bằng link nguồn (ADR 0009). TraV (`TraVCore`, `TraVThuCore`, `TamCore`) cũng link nguồn từ `_system` để phòng 4 ra đúng số như lệnh TV.
- **Luật có số cố định viết bằng C# có test**, luật riêng từng khách + giải thích để ở `_apps/OhtaTeams/data/quy_chuan/*.md` gửi kèm lời dặn cho Claude. Repo công khai, mà QUY CHUẨN là tài liệu nội bộ (tên khách, link OneDrive) → thư mục `quy_chuan/` **gitignore, gửi riêng** như `teams.json`.
- **Người dùng chốt trước mọi việc ra ngoài**: dừng ở phòng 3, tin gửi lên Teams phải xem trước và bấm gửi. AI không tự đổi đơn.
- **Kết quả lưu cạnh đơn** (`<Thư mục đơn>\<Title>\ai\*.json`), băm SHA-256 tệp đầu vào để không gọi lại Claude khi tệp không đổi.

## Considered Options

- **Làm trong tab AI của CAD** (`OHTAAI`): tab AI đọc khung bản vẽ đã triển khai, còn 4 phòng làm việc *trước khi* có bản vẽ triển khai, với tệp của khách. Không hợp.
- **Kev-0.8B / Ling thay Claude**: Kev không đọc được PDF ảnh; Ling chưa rõ có đọc ảnh không. Có thể thêm sau như tab AI.
- **Đưa QUY CHUẨN vào repo**: tiện đồng bộ, nhưng công khai tài liệu nội bộ. Không chọn.
