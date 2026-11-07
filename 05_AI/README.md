# 05_AI — Tab **AI** (AI kiểm tra & phân loại bản vẽ)

> **Chỉ hiển thị trên các máy Admin** (`"adminOnly": true` trong `module.json` — máy có tên trong tab `admin` của Google Sheet hoặc máy chủ cài Kev local) · Kiến trúc DLL riêng ([ADR 0006](../_docs/adr/0006-tab-ai-dll-rieng.md)) · Thiết kế: [_docs/specs/2026-11-04-tab-ai-kiem-tra-ban-ve.md](../_docs/specs/2026-11-04-tab-ai-kiem-tra-ban-ve.md).

Tab đọc dữ liệu trong từng **khung** (polyline kín 4 đỉnh, mặc định layer `Khung_IN`), kết hợp mô hình **Kev-0.8B** (`jaredpalmer/kev-0.8b` chạy nội bộ Local/LAN) và **Claude API** để thực hiện 2 nhiệm vụ:

1. **Phân loại tấm gia công sau cắt laser** và dán nhãn nhỏ ở **góc trên-trái** khung (layer `AI-PHAN-LOAI`, chữ cao tỷ lệ `3.5%` chiều cao khung, đồng bộ với hàm `CSV--scan-machining-texts` của Lisp Xuất CSV):
   - `[BE]` — Tấm bẻ / chấn / nhấn 1 lần (`内R-発押し`) / bẻ bước Pitchi (`曲げ角度` / `m(pitchi)`), đường bẻ `0-KAKOU` nét `DASHED*` hoặc `3-KEGAKI`, mã `V=/U=/P=/R=`, `シマ目` + `引代`.
   - `[VAT]` — Tấm vát mép (`開先施工範囲`, `開先`, `面取り`, `C面`, `K=...`, mã CSV: `k`).
   - `[CUON]` — Tấm cuộn / lốc tròn (`R曲げ`, `ロール`, nhiều đường chia bước cuộn song song trên `3-KEGAKI`, mã CSV: `r1`, `r2`).
   - `[SARA]` — Khoét loe âm mũ vít (`皿もみ`, `皿`, `サラ`, `CSK`, mã CSV: `s`).
   - `[TAPU]` — Taro ren (`タップ`, `M6..M24`, lỗ kép đồng tâm `0-KAKOU` + `3-KEGAKI`, hoặc `HATCH` trên `TAPU_TMT`, mã CSV: `t`).
   - `[PHANG]` — Tấm chỉ cắt laser phẳng.
   - Hỗ trợ đa nhãn khi 1 tấm qua nhiều công đoạn (ví dụ: `[BE - TAPU]`, `[VAT - SARA]`). Có sẵn mẫu `## [PITCHI]` và `## [PHAT_OSHI]` dạng chú thích cuối file `01-phan-loai-gia-cong.md` nếu muốn bật nhãn riêng.
2. **Kiểm tra lỗi bản vẽ & điều kiện chuẩn bị Xuất CSV (`xuat_csv_v5.0.1.lsp`)**:
   - **MTZ** — `M=` / `T=` / `Z=` không khớp bảng vật liệu chuẩn, tấm nhám `SIMA` thiếu `裏から見た図`, hoặc ghi chú có `CPL` / `裏面CPL` nhưng chưa đổi sang `M=ムキ 縞板`, `Z=SS400-SIMA`.
   - **Q** — `Q=` không khớp số lượng trong ghi chú (`4枚`, `2ヶ`, `2本`…).
   - **GHI_CHU & Chuẩn CSV** — ghi chú tiếng Nhật mâu thuẫn với F-name / hình học; trộn lẫn `TEXT` + `MTEXT` trên `2-F-NAME`; trùng tên `N=` giữa các khung; đối tượng lạ (`TEXT`/`DIM`/`HATCH`) trên `0-KAKOU`; tấm bẻ hỗn hợp (`V=` + `発押し`) thiếu chữ chú thích quanh đường bẻ (`±50mm`); kiểm tra thông minh dim tổng `1-XY` và hatch `TAPU_TMT`; kiểm tra khối MText Bước 1 trên `KHUNG_IN` (chưa nhập `Chiều dài vát`, `Pitchi`, mã cuộn `r1`/`r2`, sai số dòng, hoặc `> 8` loại đường bẻ).

🔴 = chắc chắn sai, 🟡 = nghi ngờ. Bấm một cảnh báo để zoom tới khung.

## 3 Chế độ chạy

| Chế độ | Mô tả | Yêu cầu |
|---|---|---|
| **Hybrid (Kev + Claude/Ling)** *(mặc định)* | Kev-0.8B phân loại 100% khung + lọc các khung nghi vấn lỗi (`P >= 0.50`) → chỉ gửi khung nghi vấn lên Claude API giải thích chi tiết | Kev Server + Claude API key |
| **Chỉ Kev (Local/LAN)** | Chạy 100% nội bộ qua Kev-0.8B (~120–150 ms/khung trên GTX 1070 Ti, `$0.0000`) — phân loại tấm + cảnh báo lỗi | Chỉ cần Kev Server (không cần API key) |
| **Chỉ Claude / Ling** | Gửi toàn bộ khung lên Claude API theo lô 10 khung + phân loại tấm | Claude API key |

**Dùng Ling thay Claude:** ô **Model** chọn `Ling (model trong ⚙)` → phần "Claude" của 2 chế độ trên gửi tới Ling (key `data\ling.key`, tên model trong ⚙). Ling không có ép JSON schema nên schema được đưa vào lời dặn; Ling gửi **từng lô một** (gọi dồn bị 429) và thử lại 3 lần (5 / 10 / 20 s). Dòng tổng hiện token vào/ra thay cho tiền $.

## Khởi động Kev Server (Local / LAN) & Đồng bộ Luật `.md` cho Máy Con

- **Cách 1 (từ trong CAD trên máy chủ):** Mở tab **AI** → bấm nút **⚙** → bấm **▶ Bật Kev Server (Máy này)** → đợi 3–5 giây rồi bấm **Kiểm tra kết nối** (hoặc khi bấm **▶ Kiểm tra** trên máy chủ thì server tự bật ngầm).
- **Cách 2 (chạy trực tiếp):** Chạy file `05_AI\kev\start_kev_server.bat` (gọi `05_AI\kev\ohta_kev_server.py`).
- Môi trường Python + CUDA và model `jaredpalmer/kev-0.8b` được cài cố định tại `%LOCALAPPDATA%\OHTA_TOOL\kev` trên máy chủ (`NGUYEN-A-HUNG`).
- **Tự động cho các máy con trong mạng LAN:**
  - Máy con không cài môi trường Kev local sẽ tự động trỏ về `may_chu_lan` khai báo trong `00-index.md` (mặc định `http://NGUYEN-A-HUNG:8008`) và mặc định chọn chế độ **Chỉ Kev (Local/LAN)** nếu máy con không có file `ai.key`.
  - Mỗi lần máy con bấm **▶ Kiểm tra** (hoặc bấm **Kiểm tra kết nối** trong **⚙**), máy con sẽ gọi `GET http://<máy-chủ>:8008/v1/rules` để **tự động tải toàn bộ bộ file `.md` mới nhất (`05_AI\data\kev\*.md` và `huong_dan_ai.md`) từ Máy chủ về** trước khi chạy. Nhờ vậy chỉ cần sửa file `.md` ở duy nhất Máy chủ là cả xưởng tự nhận luật mới.

## Cách dùng

1. Gõ `OHTA` → tab **AI**. Chọn **Chế độ**, **Layer khung**, **Model**, và bật/tắt **Dán nhãn gia công ở góc trên-trái khung (layer AI-PHAN-LOAI)**.
2. **▶ Kiểm tra** (= lệnh `OHTAAI`) kiểm & phân loại mọi khung trong bản vẽ; **⬚ Chọn vùng** (= `OHTAAIVUNG`) chỉ kiểm khung nằm trọn trong vùng quét.
3. Khi bật dán nhãn, nhãn cũ trên layer `AI-PHAN-LOAI` trong các khung được kiểm sẽ tự động xoá trước khi ghi nhãn mới (không bị chồng chữ khi chạy lại). Layer `AI-PHAN-LOAI` tự động được bộ quét bỏ qua ở các lần kiểm tiếp theo.
4. Nút **⚙** (góc phải tiêu đề) mở **Cài Đặt AI**:
   - **Kev Server**: nhập URL (`http://127.0.0.1:8008` hoặc IP LAN), kiểm tra kết nối, hoặc bật nhanh server trên máy hiện tại.
   - **API key**: dán key rồi **Lưu** (hiện dạng che `sk-ant-…1234`), **Xoá key** để gỡ khỏi máy.
   - **Ling API** (cho màn hình 💬 Chat): dán key Ling (`sk-studio-…`, lưu ở `data\ling.key`), ô **Model** (mặc định `Ling-3.2-flash` — model cũ bị ngừng thì gõ tên mới, không cần build lại), **Kiểm tra kết nối** gửi thử một câu ngắn.
   - **Bộ file hướng dẫn & luật AI (`data\kev\*.md` + `data\huong_dan_ai.md`)**: chọn từng file trong ô **Chọn file** để sửa trực tiếp trên giao diện, mở bằng Notepad, mở thư mục `data\kev`, hoặc khôi phục mặc định:
     - `00-index.md`: File chính điều hướng Kev (ngưỡng xác suất + liên kết tới các file `.md` con).
     - `01-phan-loai-gia-cong.md`: Định nghĩa các loại tấm gia công (`BE`, `VAT`, `CUON`, `SARA`, `TAPU`... có thể tự thêm nhóm `## [MÃ]` mới mà không cần build lại DLL).
     - `02-luat-fname-vat-lieu.md`: Luật kiểm tra cấu trúc F-name, đối chiếu bảng vật liệu chuẩn `catalog.csv` và tấm nhám `SIMA`.
     - `03-luat-ghi-chu-ban-ve.md`: Luật đối chiếu ghi chú tiếng Nhật với F-name (số lượng `Q`, độ dày `t=`, và các mục `## [LUAT: ...]` tự định nghĩa).

| Lệnh | Việc |
|---|---|
| `OHTAAI` | Quét mọi khung trên layer đang chọn ở tab, phân loại & kiểm tra AI |
| `OHTAAIVUNG` | Như trên, chỉ khung nằm trọn trong vùng chọn 2 điểm |
| `OHTAAIZOOM` | Zoom tới khung theo số thứ tự (tab gọi khi bấm cảnh báo) |
| `OHTAAIMOTA` | Nút **📝 Xuất mô tả**: quét chọn khung in (mỗi khung = 1 tấm), ghi `<bản vẽ>.ai.md` (gọn, không toạ độ) + `.ai.json` (đủ toạ độ) + chép `kien-thuc-co-ban.md` vào `OHTA_AI\mo-ta\` cạnh file bản vẽ để Kev / Claude đọc hiểu. Thông tin tấm chỉ lấy từ F-name. Không gọi AI, không sửa bản vẽ |
| `OHTAAICHATKHUNG` | Nút **📎 Gửi khung** của màn hình chat: quét vùng, mô tả gọn các khung in trong vùng được gửi kèm câu hỏi kế tiếp |

## 💬 Chat với AI (Ling)

Nút **💬 Chat với AI (Ling)** đổi tab sang màn hình chat; **← Quay lại** trở về màn hình kiểm tra. Cần key Ling (nút ⚙).

- Chữ trả lời hiện dần (streaming); ô **Hiện suy nghĩ** bật/tắt phần suy nghĩ của model (mặc định ẩn). **Enter** gửi, **Shift+Enter** xuống dòng, **■ Dừng** ngắt giữa chừng.
- Mỗi lần gửi luôn kèm bộ luật `data\kev\*.md` làm kiến thức nền, cùng tối đa **20 tin gần nhất** của cuộc (file lưu vẫn đủ).
- **📎 Gửi khung** đính kèm mô tả khung in vào câu hỏi kế tiếp — cả bản `.md` gọn lẫn bản `.json` đủ toạ độ từng đối tượng (để AI tự đo kích thước tổng khi bản vẽ không có dim); bấm ✕ để bỏ. Nhiều khung thì JSON dài, tốn token.
- Tên model đang chat hiện ở góc dưới trái (🤖 …).
- AI **chỉ đọc và trả lời**, không sửa bản vẽ.
- Mỗi cuộc chat là một file `.json`; ô chọn trên cùng mở lại cuộc cũ của bản vẽ đang mở, **＋ Cuộc mới** bắt đầu cuộc khác, **📁** mở thư mục lưu.

Thư mục AI cạnh bản vẽ:

```
<thư mục bản vẽ>\OHTA_AI\
├── mo-ta\                     <bản vẽ>.ai.md, .ai.json, kien-thuc-co-ban.md (nút 📝 Xuất mô tả)
└── chat\<tên bản vẽ>\         2026-11-07_153005.json … (mỗi cuộc một file)
```

Bản vẽ chưa lưu: chat ghi vào `%LOCALAPPDATA%\OHTA_TOOL\AI\chat\_chua-luu\`.

## Tệp

```
05_AI/
├── module.json                         Tab "AI" (id "ai", order 50)
├── kev/start_kev_server.bat            Script bật Kev-0.8B Server (port 8008, hỗ trợ LAN)
├── data/kev/00-index.md                File chính điều hướng Kev (Master Index)
├── data/kev/01-phan-loai-gia-cong.md   Luật phân loại tấm gia công (BE/VAT/CUON/SARA/TAPU...)
├── data/kev/02-luat-fname-vat-lieu.md  Luật F-name & bảng vật liệu (MTZ)
├── data/kev/03-luat-ghi-chu-ban-ve.md  Luật đối chiếu ghi chú & số lượng (Q / GHI_CHU)
├── data/huong_dan_ai.md                File hướng dẫn gốc của Claude
├── data/ai.key                         API key Claude — KHÔNG lên git (.gitignore chặn ai.key*)
├── data/ling.key                       API key Ling (chat) — KHÔNG lên git (.gitignore chặn ling.key*)
├── log/                                Nhật ký mỗi lần kiểm (JSON) — không lên git
├── bin/<ZwCAD|AutoCAD>/AITOOL.dll
└── plugin/AiTool.Plugin, AiTool.Tests
```

Cài đặt riêng: `HKCU\Software\OFT_Lisp\OhtaTool\AI` (`che_do`, `kev_url`, `dan_nhan`, `layer_khung`, `model`, `ling_model`). Build + test: `powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1` (chạy cả `AiTool.Tests`).

Dữ liệu gửi đi: mọi đối tượng có tâm hộp bao nằm trong khung (line, cung, tròn, polyline, chữ, dim — kèm layer), toạ độ tính từ góc dưới trái khung, làm tròn 2 chữ số; cùng bảng vật liệu và file hướng dẫn AI (được Claude cache 5 phút nên các lô sau rẻ hơn). Không gửi ảnh. Ghi chú MLeader được đọc; đối tượng nằm trong block (BlockReference) **không** được đọc. Khung bao trùm trọn một khung khác (vd. viền bản vẽ cùng layer) bị bỏ qua — không phải một tấm.

## Tạo API key Anthropic

1. Vào **console.anthropic.com**, đăng ký / đăng nhập bằng tài khoản công ty.
2. **Billing** (Plans & Billing) → nạp tiền (credit trả trước). Vài USD đủ thử hàng trăm khung với Sonnet.
3. **Limits** → đặt **giới hạn chi tiêu tháng** để không phát sinh ngoài ý muốn.
4. **API Keys** → **Create Key**, đặt tên `OHTA-AI` → chép key (chỉ hiện một lần; bắt đầu bằng `sk-ant-`).
5. Trên tab AI bấm **⚙** → dán key vào ô **API KEY** → **Lưu**. (Key được ghi vào `05_AI\data\ai.key`. Tự tạo file bằng Notepad cũng được: một dòng duy nhất; Notepad hay lưu thành `ai.key.txt` — tab vẫn đọc được, và `.gitignore` chặn cả `ai.key*`.)
6. Không gửi key qua chat / email, không đưa lên git (repo public). Lộ key thì vào **API Keys** xoá key cũ, tạo key mới.

Chưa có key thì tab báo đỏ "Chưa có API key…" và không gửi gì.
