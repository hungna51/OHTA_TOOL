# Tab AI nằm trong AITOOL.dll riêng, gọi Claude API bằng HttpClient, chưa phát hành

Từ 11/2026, tab **AI** (mục con `05_AI`, thiết kế ở [_docs/specs/2026-11-04-tab-ai-kiem-tra-ban-ve.md](../specs/2026-11-04-tab-ai-kiem-tra-ban-ve.md)) gửi dữ liệu trong từng khung lên Claude API để cảnh báo F-name / số lượng / ghi chú. Giao diện và logic viết trong **AITOOL.dll** (`05_AI\plugin\AiTool.Plugin`, ra `05_AI\bin\<ZwCAD|AutoCAD>\AITOOL.dll`). OHTATOOL.dll nạp DLL đó và gọi `AiTool.AiOhtaTab.TaoGiaoDien(tieuDe, moTa, mauNhan, thuMucModule)` bằng phản chiếu — cùng cách và cùng hợp đồng 4 tham số chuỗi như tab Guro ([ADR 0003](0003-tab-guro-nam-trong-grotool.md)), qua bộ nạp dùng chung `DllTabLoader`. Lỗi trong tab AI (đang thử nghiệm) không kéo sập bảng OHTA; không nạp được thì tab hiện danh sách nút của `module.json`.

- **Gọi API bằng `HttpClient` thẳng (`POST /v1/messages`)**, không dùng gói NuGet `Anthropic`: plugin CAD là một DLL đơn, chạy cả net48 (ZWCAD) lẫn net10 (AutoCAD), `CopyLocalLockFileAssemblies=false`; kéo gói NuGet và các phụ thuộc của nó vào AppDomain của CAD dễ xung đột phiên bản với DLL của CAD hoặc của plugin khác. OHTA đã có tiền lệ `GitHubReleaseClient`. JSON đọc/ghi bằng `JsonLite` của OHTA.
- **Mã Fname dùng lại bằng link nguồn** (`JsonLite`, `FnameCore`, `FnameDataStore`, `FnameLocalAdditions` từ `_system\OhtaTool.Plugin\Core`), không tham chiếu OHTATOOL.dll — hai DLL vẫn build/sửa riêng ([ADR 0001](0001-grotool-dll-tach-rieng.md)). Kiểu nằm trong namespace `OhtaTool.Plugin.Core` nhưng thuộc assembly AITOOL nên không xung đột.
- **Không gắn với GRO**: không gọi GROTOOL.dll, không đọc registry GRO. Cài đặt riêng ở `HKCU\Software\OFT_Lisp\OhtaTool\AI`.
- **Chưa phát hành**: `release_ohta.ps1` loại cả thư mục `05_AI` khỏi zip; chỉ máy phát triển có tab. Trình cập nhật không đụng tới file chưa từng phát hành nên tab vẫn còn sau khi máy tự cập nhật. Khi muốn phát hành: gỡ dòng loại `05_AI` trong `release_ohta.ps1`, thêm `AITOOL.dll` vào danh sách DLL đóng gói, cập nhật ADR này.
- **Key** ở `05_AI\data\ai.key`, **nhật ký** ở `05_AI\log\` — cả hai trong `.gitignore` (repo public).

## Considered Options

- **Viết trong OHTATOOL.dll** (như tab Fname): ít việc hơn nhưng code thử nghiệm nằm chung với lõi mà cả đội tự cập nhật. Không chọn.
- **SDK `Anthropic` chính thức**: gọn hơn khi viết, nhưng rủi ro xung đột phụ thuộc trong CAD như trên. Không chọn; có thể xem lại nếu SDK chạy được không cần thêm DLL phụ.
