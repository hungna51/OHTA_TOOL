# Spec — Gắn đơn Teams với bản vẽ và chạy kiểm tra AI (Harness bước 1)

Soạn ngày 10/11/2026. Trạng thái: **chờ duyệt**. Chưa có kế hoạch triển khai (`_docs/plans/`) — Claude Code viết kế hoạch sau khi đọc spec này và trả lời mục *Cần kiểm tra trong mã trước khi làm*.

Mục con chính: `06_Teams`. Mục con `05_AI` **chỉ đọc, không sửa** (trừ khi mục *Cần kiểm tra* cho thấy bắt buộc — khi đó dừng lại hỏi người dùng).

## Bối cảnh

Đây là bước đầu của "harness đơn hàng": đơn đến qua Microsoft Teams → đọc → kiểm tra → phân loại → người duyệt → giao việc. OHTA đã có hai mảnh rời nhau:

- Tab **Teams** (`TEAMSTOOL.dll`, ADR 0008): xem đơn trong Microsoft Lists, chỉ đọc.
- Tab **AI** (`AITOOL.dll`, ADR 0006): kiểm tra và phân loại khung in của bản vẽ đang mở (lệnh `OHTAAI`).

Hiện kỹ sư phải tự nhớ bản vẽ nào thuộc đơn nào, rồi tự chuyển tab để kiểm tra. Bước 1 nối hai mảnh này **mà không xin thêm quyền Microsoft 365 và không gửi thêm dữ liệu ra ngoài**.

## Mục đích

Từ màn hình chi tiết một đơn trên tab Teams, kỹ sư:

1. **Gắn** bản vẽ đang mở trong CAD vào đơn đó.
2. **Chạy kiểm tra AI** bản vẽ đó bằng đúng một nút bấm.
3. Lần sau mở lại đơn, **thấy ngay** bản vẽ nào đã gắn và mở lại được.

## Ngoài phạm vi (để bước 2, 3)

- Đọc PDF thư hay file đính kèm của khách (Graph v1.0 không đọc được file đính kèm dòng Lists — ADR 0008).
- Ghi bất cứ gì lên Teams / Lists (đổi trạng thái, người triển khai). Không thêm quyền Graph mới; vẫn chỉ `Sites.Read.All`, `TeamsTab.Read.All`…
- Tự giao việc, nhắn tin, Power Automate.
- Đối chiếu tự động số lượng / hạn giao của đơn với F-name.
- Dùng chung liên kết giữa nhiều máy (liên kết bước 1 lưu riêng từng máy).

## Hành vi

### Màn hình chi tiết đơn (`TeamsTabControl.HienChiTiet`)

Thêm một khối **"Bản vẽ của đơn"** nằm giữa bảng cột và nút **Mở trên Teams**:

1. **Danh sách bản vẽ đã gắn** (một đơn có thể có nhiều bản vẽ). Mỗi dòng: tên file `.dwg`, thư mục rút gọn, ngày gắn. Bấm dòng → mở bản vẽ đó trong CAD (nếu đang mở thì chuyển sang). File không còn ở đường dẫn cũ → dòng mờ, ghi "không tìm thấy file", có nút ✕ gỡ liên kết.
2. Nút **＋ Gắn bản vẽ đang mở**:
   - Bản vẽ chưa lưu (chưa có `Database.Filename`) → báo "Lưu bản vẽ trước khi gắn", không gắn.
   - Đã gắn rồi → báo "Bản vẽ này đã gắn với đơn", không tạo bản trùng.
   - Bản vẽ đang gắn với **đơn khác** → hỏi xác nhận "Bản vẽ này đang gắn với đơn <Title khác>. Gắn thêm vào đơn này?" (cho phép một bản vẽ thuộc nhiều đơn, nhưng phải hỏi).
   - Gợi ý nhẹ: nếu tên file bản vẽ **có chứa** Title của đơn (so sánh bỏ dấu, không phân biệt hoa thường, bỏ khoảng trắng) thì hiện dấu ✓ "Tên file khớp mã đơn" cạnh nút. Không khớp vẫn gắn được — chỉ là gợi ý.
3. Nút **▶ Kiểm tra AI bản vẽ này** trên mỗi dòng bản vẽ đã gắn:
   - Nếu bản vẽ chưa phải bản vẽ đang hoạt động → chuyển sang (mở nếu cần) trước.
   - Hiện hộp xác nhận ghi rõ **chế độ AI đang chọn** (đọc `che_do` ở `HKCU\Software\OFT_Lisp\OhtaTool\AI`, chỉ đọc). Nếu chế độ **không phải** "Chỉ Kev (Local/LAN)" thì thêm dòng cảnh báo: *"Chế độ này gửi nội dung khung bản vẽ ra dịch vụ AI bên ngoài. Công ty chưa có quy định về dữ liệu khách hàng — cân nhắc chuyển sang Chỉ Kev."* Nút: **Kiểm tra** / **Huỷ**.
   - Bấm Kiểm tra → gửi lệnh `OHTAAI` cho bản vẽ đó, rồi chuyển bảng OHTA sang tab **AI** để xem kết quả (cách chuyển tab: xem mục *Cần kiểm tra*, câu 2).
   - Tab AI chưa nạp / máy không thấy tab AI → nút mờ, tooltip "Tab AI chưa có trên máy này".

### Không đổi

- Danh sách đơn, lọc, đăng nhập, bản lưu tạm: giữ nguyên.
- Tab AI: giữ nguyên cách kiểm tra, cách hiện kết quả, nhật ký `05_AI\log\`.
- Bản vẽ: bước 1 **không ghi gì vào bản vẽ** (việc dán nhãn `AI-PHAN-LOAI` vẫn do cài đặt của tab AI quyết định như hiện nay).

## Dữ liệu liên kết

- File: `%LOCALAPPDATA%\OHTA_TOOL\teams\lien-ket.json` (cùng thư mục với `token.bin` và `tam\` — ngoài OneDrive và ngoài gói phát hành; ADR 0008).
- **Đăng xuất không xoá file này** (khác `token.bin` và `tam\`): liên kết là công việc của kỹ sư, không phải phiên đăng nhập. Ghi rõ điều này trong README.
- Nội dung (UTF-8 không BOM, ghi/đọc bằng `JsonLite`):

```json
{
  "phien_ban": 1,
  "lien_ket": [
    {
      "list_id": "<listId>",
      "don_id": "<item id của dòng Lists>",
      "don_title": "<Title lúc gắn — chỉ để hiện khi đơn không còn tải được>",
      "ban_ve": "C:\\...\\ABC-123.dwg",
      "luc_gan": "2026-11-10T11:20:00+07:00"
    }
  ]
}
```

- Khoá của một liên kết = (`list_id`, `don_id`, `ban_ve` so sánh không phân biệt hoa thường). Không lưu dữ liệu đơn nào khác (không chép cột, không chép tên khách).
- File hỏng / không đọc được → coi như rỗng, đổi tên file cũ thành `lien-ket.loi_<thời gian>.json`, ghi nhật ký; không ném lỗi vào CAD.
- Ghi file: ghi ra file tạm rồi thay thế, để mất điện giữa chừng không làm hỏng file.

## Ràng buộc kiến trúc (bắt buộc)

1. **Không tham chiếu chéo DLL.** `TEAMSTOOL.dll` không tham chiếu `AITOOL.dll` và ngược lại (ADR 0006, 0008). Teams gọi AI **chỉ qua lệnh CAD** (`OHTAAI`) gửi bằng `Document.SendStringToExecute` (hoặc cơ chế tương đương đang dùng trong OHTA). Đọc cài đặt AI chỉ qua registry, chỉ đọc.
2. **Logic thuần nằm trong `06_Teams/plugin/TeamsTool.Plugin/Core/`** (đọc/ghi/gộp/tìm liên kết, so tên file với Title), không `using` namespace ZWCAD/AutoCAD, có test trong `TeamsTool.Tests` (rules/01 §3).
3. **Mọi thao tác chạm CAD** (lấy bản vẽ đang mở, mở/chuyển bản vẽ, gửi lệnh) đặt trong một lớp riêng phía CAD (kiểu `CadServices/` của tab AI), bọc `CadCompat.LockDocument()` khi đọc Database từ palette modeless (GEMINI.md §6). Chạy được cả ZWCAD (`net48`) lẫn AutoCAD (`net10.0-windows`).
4. **Không thêm gói NuGet.**
5. **Lỗi không kéo sập palette**: dùng lại mẫu `Chay(...)` / dòng trạng thái có sẵn của `TeamsTabControl`.
6. Chuỗi trên dòng lệnh CAD viết **không dấu** (như `AiCommands`); chuỗi trên giao diện WPF có dấu bình thường.

## Cần kiểm tra trong mã trước khi làm

Claude Code trả lời các câu này (đọc mã, không đoán) rồi mới viết kế hoạch. Câu nào làm thay đổi thiết kế → dừng lại hỏi người dùng.

1. `TeamsTool.Plugin` đã tham chiếu API CAD chưa (có `bin/ZwCAD` và `bin/AutoCAD` nên nhiều khả năng có), và đã có lớp lệnh / `CadCompat` dùng chung chưa? Nếu chưa, lấy theo cách tab AI đang làm (link nguồn từ `_system`, không tham chiếu `OHTATOOL.dll`).
2. Bảng OHTA chuyển sang một tab theo tên/id bằng cách nào? (Lệnh `FN` chuyển sang tab Fname, `OI` sang tab Print — tìm cơ chế này trong `_system`.) Từ `TEAMSTOOL.dll` có gọi được mà không sửa `_system` không? Nếu **phải sửa `_system`** (ví dụ thêm lệnh `OHTATAB <id>`) → dừng, hỏi người dùng, vì `_system` là lõi tự cập nhật của cả đội.
3. `AiCommands.Chay` cần `AiTabViewModel.HienTai` khác null ("Mở tab AI trên bảng OHTA trước"). Tab AI được tạo lúc dựng palette hay lúc người dùng bấm vào tab lần đầu? Nếu lúc bấm lần đầu thì phải chuyển tab **trước** khi gửi `OHTAAI`.
4. Tên giá trị registry `che_do` và các giá trị có thể có (để biết chế độ nào là "Chỉ Kev"). Đọc ở `AiTabViewModel` / `CauHinhKev`.
5. `build_ohta.ps1` có chạy `TeamsTool.Tests` không (README `06_Teams` nói có; CLAUDE.md chỉ nhắc `OhtaTool.Tests`, `GroTool.Tests`). Xác nhận.
6. `DonTeams.Id` có phải item id ổn định của dòng Lists không (dùng làm `don_id`).

## Test

Tự động (`TeamsTool.Tests`, logic thuần):

- Đọc `lien-ket.json` rỗng / không tồn tại / hỏng / thiếu khoá.
- Thêm liên kết mới; thêm trùng (khác hoa thường đường dẫn) → không tạo bản thứ hai.
- Một bản vẽ thuộc hai đơn; một đơn có ba bản vẽ; gỡ một liên kết.
- Tìm liên kết theo (`list_id`, `don_id`) và theo `ban_ve`.
- So tên file với Title: có dấu / không dấu, hoa thường, khoảng trắng, ký tự tiếng Nhật, Title rỗng.
- Ghi rồi đọc lại cho ra đúng dữ liệu (UTF-8 có tiếng Nhật và tiếng Việt).

Tay, trên **cả ZWCAD 2026 và AutoCAD** (ghi kết quả vào mô tả commit):

1. Mở đơn → gắn bản vẽ đã lưu → đóng CAD, mở lại → đơn vẫn hiện bản vẽ đã gắn.
2. Gắn bản vẽ chưa lưu → báo đúng, không gắn.
3. Bấm ▶ Kiểm tra AI khi bản vẽ khác đang hoạt động → chuyển đúng bản vẽ, tab AI hiện kết quả.
4. Chế độ AI = Hybrid → hộp xác nhận có cảnh báo dữ liệu; = Chỉ Kev → không có.
5. Đổi tên / chuyển file `.dwg` → dòng mờ "không tìm thấy file", gỡ được.
6. Đăng xuất Teams rồi đăng nhập lại → liên kết còn nguyên.
7. Mở một đơn có 3.000+ dòng trong list → danh sách đơn vẫn nhanh như trước (khối mới chỉ dựng ở màn hình chi tiết).

## Tài liệu & phiên bản

- `06_Teams/module.json`: `version` `0.1` → `0.2` (thêm tính năng), giữ `stage: "dev"`, `adminOnly: true`.
- `05_AI/module.json`: không đổi (không sửa tab AI).
- Cập nhật `06_Teams/README.md` (mục mới "Bản vẽ của đơn", bảng *Dữ liệu nằm ở đâu* thêm `lien-ket.json`, ghi rõ đăng xuất không xoá).
- `CHANGELOG.md`: thêm dòng cho người dùng xưởng.
- Cách hai tab nói chuyện với nhau qua lệnh CAD là mẫu mới → viết **ADR 0009** ngắn (vì sao không tham chiếu DLL, vì sao lưu liên kết riêng từng máy, phương án đã cân nhắc: ghi liên kết lên cột Lists — cần `Sites.ReadWrite.All`, để bước sau).
- Chạy `powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1`: cả hai bản build sạch, mọi test PASS.

## Tiêu chí xong

- [ ] Gắn, xem, mở lại, gỡ bản vẽ của đơn chạy đúng trên ZWCAD và AutoCAD.
- [ ] ▶ Kiểm tra AI chạy `OHTAAI` đúng bản vẽ và kết quả hiện ở tab AI.
- [ ] Cảnh báo dữ liệu hiện khi chế độ AI gửi ra ngoài.
- [ ] Không thêm quyền Graph, không thêm NuGet, không tham chiếu chéo DLL, `_system` không đổi (hoặc đã được người dùng duyệt nếu bắt buộc).
- [ ] Test tự động PASS; checklist tay 1–7 đã chạy.
- [ ] README, module.json, CHANGELOG, ADR 0009 đã cập nhật.
