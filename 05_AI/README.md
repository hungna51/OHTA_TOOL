# 05_AI — Tab **Tạo dữ liệu DXF từ ảnh**

> **Chỉ hiển thị trên máy Admin** (`"adminOnly": true`) · DLL riêng `AITOOL.dll` ([ADR 0006](../_docs/adr/0006-tab-ai-dll-rieng.md)) · Từ 25/11/2026 thay toàn bộ tab AI cũ (phân loại, kiểm tra khung, Kev, chat Ling).

Tab là **🧪 Thử nhanh của OhtaTeams đặt trong CAD**: dán ảnh chụp MỘT hình → AI đọc kỹ + chọn mẫu trong thư viện `_apps\OhtaTeams\ve_dxf\thu_vien` → vẽ **chỉ hình + dim** (không F-name, không Khung_IN) → **📥 Chèn vào bản vẽ** đang mở. F-name người dùng tự thêm (tab Fname).

> **Luật của mục:** [rules/00-muc-luc-ai.md](rules/00-muc-luc-ai.md) — đọc [rules/01-schema-ai.md](rules/01-schema-ai.md) **trước khi thêm / đổi ô AI trả về** (schema quá lớn → API lỗi "compiled grammar too large", cả lượt đọc hỏng).

## Dùng

1. `OHTA` → tab **DXF ảnh**.
2. **📋 Dán ảnh (Ctrl+V)** — ảnh chụp vùng bản vẽ (Win+Shift+S) hoặc tệp đã copy; hoặc **📂 Chọn tệp** / kéo thả tệp ảnh, PDF, DXF vào tab. Đợi AI đọc + vẽ (dòng chữ tím báo tiến độ; **■ Huỷ** để dừng).
2b. ☐ **Thông tin bổ sung** (tab nhớ): bật thì dán ảnh / chọn tệp / 🤖 Đọc lại **chưa gửi ngay** — hiện khung ảnh + ô nhập (đọc lại: điền sẵn ghi chú cũ) → **▶ Gửi AI** (Ctrl+Enter) / Huỷ. Ghi chú đi kèm ảnh, **thắng hình**: AI làm theo, không tô vàng / hỏi lại điều đã ghi; chỗ ghi chú khác hình → dòng cam **⚠ Ghi chú khác hình** (AI vẫn vẽ theo ghi chú). Ghi chú lưu trong lần thử (`ghi_chu_nguoi`), hiện 📝 dưới mô tả. Exe: `--ghi-chu-tep TEP` (UTF-8) cho `--thu-nhanh-moi` / `--thu-nhanh-doc-lai`.
3. Ô vàng **Cần xác nhận**: chỗ khách không ghi số mà AI lấy theo hình (vd R10 bo góc khoét) — nét đó trong DXF **tô vàng**. Đúng → đổi nét về ByLayer sau khi chèn; sai → ✎ Sửa thông số rồi 🔁 Vẽ lại (ô đã sửa coi là đã xác nhận, hết vàng).
3b. Xem **ảnh gửi vào** và **hình phần mềm vẽ** cạnh nhau (bấm ảnh mở to); dòng đỏ = chưa vẽ được, dòng cam = thiếu dim khách / cảnh báo khoét.
4. **📥 Chèn vào bản vẽ** (lệnh `OHTADXFCHEN`) → bấm điểm đặt **góc dưới trái biên dạng**. Layer / kiểu trùng tên giữ bản của bản vẽ.
5. **Chưa có mẫu** (AI ghi `chua_co` — thư viện chưa có loại hình này, vd hình thang vát): bấm **✨ AI viết mẫu** → AI viết mẫu Python làm *mẫu thử* của lần thử này rồi vẽ lại (tốn AI). Dùng tốt thì vào ⚙ → **➕ Thêm vào thư viện** (máy Admin) để lần sau AI chọn được luôn.
6. Sai số / sai mẫu: **✎ Sửa thông số** (Notepad mở `chi_tiet.json`) → lưu → **🔁 Vẽ lại** (không gọi AI), hoặc **🤖 Đọc lại (AI)**. **📁 Thư mục**, **🗑 Xoá** lần thử.
7. Nút **⚙** mở cửa sổ **Thử nhanh của OhtaTeams**: 📚 Thư viện mẫu, ✨ AI viết mẫu khi "chưa có mẫu", ➕ Thêm vào thư viện (máy Admin).

**Các lần thử** dùng chung thư mục `%LOCALAPPDATA%\OHTA_TOOL\thu_nhanh` với cửa sổ Thử nhanh — làm bên nào cũng thấy bên kia.

## Cách chạy

CAD không tự gọi AI: tab chạy ẩn `_apps\OhtaTeams\bin\OhtaTeams.exe` ở chế độ dòng lệnh (`Services/ThuNhanhCli.cs` của OhtaTeams), cùng luồng `AiDon.TaoThuNhanh → DocThuNhanh → VeThuNhanh` với cửa sổ Thử nhanh — sửa đọc / vẽ / thư viện mẫu ở OhtaTeams là tab này nhận luôn.

| Lệnh exe | Việc |
|---|---|
| `--thu-nhanh-moi TEP` | tạo lần thử, AI đọc + chọn mẫu, vẽ |
| `--thu-nhanh-doc-lai TM` | AI đọc lại rồi vẽ |
| `--thu-nhanh-ve TM` | vẽ lại từ `chi_tiet.json` (không AI) |
| `--thu-nhanh-viet-mau TM` | AI viết mẫu thử cho hình chưa có mẫu rồi vẽ |
| `--thu-nhanh-ds` | liệt kê các lần thử |
| `--thu-nhanh` | mở cửa sổ Thử nhanh |

Stdout UTF-8, mỗi dòng: `@ tiến độ` · `#TM thư mục` · `#KQ json` · `#LOI lỗi` (đọc bằng `Core/ThuNhanh.cs`, có test).

**Máy cần** (như Thử nhanh của OhtaTeams): `OhtaTeams.exe` đã build · key / nguồn AI đặt ở OhtaTeams → ⚙ → 🤖 AI (key chung `05_AI\data\ai.key`) · Python 3 + ezdxf (+ matplotlib, pymupdf) · môi trường vẽ `_apps\OhtaTeams\data\quy_chuan\4_trien_khai\moi_truong\moi-truong-ve.dxf` (gitignore, chép tay từ máy Admin).

## Tệp

```
05_AI/
├── module.json
├── data/ai.key                 key Claude dùng chung với OhtaTeams (gitignore ai.key*)
└── plugin/AiTool.Plugin/
    ├── AiOhtaTab.cs            cửa vào tab (DllTabLoader)
    ├── AiCommands.cs           OHTADXFCHEN
    ├── Core/ThuNhanh.cs        đọc dòng exe, LanThu, đường dẫn exe (test: AiTool.Tests)
    ├── Services/ChayExe.cs     chạy OhtaTeams.exe ẩn, huỷ = giết tiến trình
    ├── CadServices/ChenDxf.cs  DxfIn + Database.Insert dời gốc (0,0) → điểm bấm
    └── ViewModels/, Views/     giao diện tab
```
