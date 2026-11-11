# OhtaTeams.exe — ứng dụng xem đơn Teams chạy ngoài CAD

Thiết kế: [_docs/specs/2026-11-10-ung-dung-teams-rieng.md](../../_docs/specs/2026-11-10-ung-dung-teams-rieng.md).

- Mở `_apps\OhtaTeams\bin\OhtaTeams.exe` (không cần mở CAD). Màn hình giống hệt tab Teams: đăng nhập Microsoft 365 → nhóm → kênh → tab Lists → đơn.
- Dùng chung `06_Teams\data\teams.json`, phiên đăng nhập và bản lưu tạm với tab Teams → máy đã đăng nhập trên CAD không phải đăng nhập lại.
- Ô **Nhân viên** trên danh sách: lọc đơn theo người. Bấm một dòng → chi tiết dạng form Lists (OHTA COMPANY / OFT BY OHTA, 3 cột), **bấm ô để sửa và ✓ Lưu lên Teams ngay**. **✏ Sửa nhiều ô**: sửa các cột chữ / số / lựa chọn / ngày / có-không / người (giao đơn) rồi **💾 Lưu lên Teams** (xác nhận cũ → mới; cần quyền `Sites.ReadWrite.All` — xem [06_Teams/README.md](../../06_Teams/README.md)).
- Chi tiết đơn → **📌 Đang làm** (bấm lại để bỏ); đơn đang chọn có 📌 trong danh sách. ⚙ → **Thư mục đơn**: thư mục chứa các thư mục đơn.
- Mỗi lần tải xong đơn / bấm 📌 / lưu ⚙, ứng dụng ghi `%LOCALAPPDATA%\OHTA_TOOL\teams\dang_lam.json` (đơn chưa xong + đơn 📌, thư mục đơn, giờ ghi) — tab Teams trên CAD đọc file này (bước 3).
- Exe tìm `06_Teams` bằng cách đi ngược lên từ thư mục của nó, nên phải nằm trong thư mục OHTA_TOOL.

## 🤖 Khu AI — phòng làm việc (đợt 1: phòng 1 + 2)

Thiết kế: [_docs/specs/2026-11-11-phong-lam-viec-ai-ohtateams.md](../../_docs/specs/2026-11-11-phong-lam-viec-ai-ohtateams.md) · [ADR 0010](../../_docs/adr/0010-phong-lam-viec-ai-ohtateams.md).

**Cần trên máy:** ⚙ → tab **Thư mục đơn** đã đặt · key Claude (⚙ → tab **🤖 AI**, ghi `05_AI\data\ai.key` dùng chung với tab AI trên CAD) · thư mục `_apps\OhtaTeams\data\quy_chuan\` (**gitignore — chép tay từ máy Admin**):
- `1_tiep_nhan\`, `2_doc_ban_ve\`, `3_kiem_tra\`, `4_trien_khai\` — **mỗi phòng một bộ quy chuẩn `*.md` riêng**, chỉ phần liên quan việc của phòng (phòng 2: vật liệu M/Z, tên kiểu gia công, bảng taro / sara; phòng 3: điều kiện, giới hạn máy, lỗ gần đường bẻ, công ty đặc biệt — không gồm các bước triển khai; phòng 4: các bước triển khai, bảng delta, file hoàn thành, layer). Phòng gọi AI chỉ gửi bộ của mình. Máy chưa có thư mục phòng → gửi các `*.md` ở gốc `quy_chuan` như cũ.
- `_goc\` — bản QUY CHUẨN CHUNG đầy đủ (không gửi AI). Sửa bản gốc thì chia lại các bộ phòng cho khớp.
- `cong_ty.txt` — mỗi dòng `khoá1,khoá2|Tên hiện` (đoán công ty đặc biệt từ các cột của đơn).

**Key:** ⚙ → 🤖 AI → **Mỗi phòng một key riêng** (bật / tắt). Tắt: mọi phòng dùng key chung `ai.key`. Bật: phòng có `05_AI\data\ai.key.phong<n>` thì dùng key đó, chưa có thì dùng key chung. Giới hạn tốc độ Claude tính theo tổ chức — key cùng tổ chức vẫn chia chung giới hạn; key riêng chủ yếu để xem tiền từng phòng. Nhiều đơn chạy cùng lúc được: mở đơn khác trong phòng AI trong khi đơn trước đang chạy, quay lại đơn cũ vẫn thấy tiến độ.

Phòng AI là khung bên phải màn danh sách đơn (kéo vạch giữa để đổi rộng). **Chuột phải vào đơn → 🤖 Mở trong phòng AI** (hoặc nút **🤖 Phòng AI** trong chi tiết đơn) → **▶ Bắt đầu** (lần đầu hỏi đồng ý gửi bản vẽ lên Claude API; bật / tắt ở ⚙ → 🤖 AI):

1. **Tiếp nhận** — tải tệp đính kèm + tệp trong các chuỗi chat của đơn vào `<thư mục đơn>\ai\goc\` (trùng nội dung bỏ qua, trùng tên khác nội dung thêm ` (2)`), xếp loại tệp (bản vẽ / 3D / đơn hàng-mail-ảnh / khác — đoán theo tên, sửa ở ô chọn), đoán **loại đơn** (mới / cần liên lạc / repeat / nửa repeat — từ các cột đơn) và **công ty đặc biệt**. Gộp bản nháp `ai\<Title>-番号.PDF` (đơn hàng trước, bản vẽ sau; xoay trang, tô màu vẫn làm tay). **Repeat hoàn toàn → dừng**, không gọi AI.
2. **Đọc bản vẽ** — mỗi tệp loại *Bản vẽ* (PDF gửi nguyên, DXF gửi bản tóm tắt: kích thước bao, lỗ gom theo Ø, chữ) → Claude trả bảng **mỗi dòng một tấm**: số BV, tên, M, Z, T, Q, mặt, kích thước, bẻ, lỗ, dung sai, ghi chú JP + VI. Ô AI không chắc / tự chọn hiện ở cột ⚠. **Sửa thẳng trong bảng** (lưu ngay; ô đã sửa không bị lần đọc lại ghi đè). Đơn nửa repeat: bỏ tích **Mới** ở tấm repeat. Tệp không đổi (SHA-256) thì không gọi AI lại; **↻ Đọc lại tất cả** để ép đọc lại.
   Kiểm bảng xong → **✓ Xác nhận bảng → phòng 3** (bảng khoá; **↩ Bỏ xác nhận** để sửa; đọc lại bản vẽ thì phải xác nhận lại).
3. **Kiểm tra** — **▶ Kiểm tra**: luật C# (`PhongKiemTra.cs`) rồi AI rà từng bản vẽ theo QUY CHUẨN phòng 3 (không có key → chỉ chạy luật). Luật: ô AI chưa chắc; thiếu M / T / Q; độ dày cần xác nhận (PL > 16, SS t3.2, SUS < 0.5, tấm mỏng dài); không có trong bảng TV; **cạnh bẻ ngắn hơn Lmin** và **lỗ cách đường bẻ < Lmin** (Lmin = `L_saitei` của `01_Fname\data\trav_90.csv`, xét V nhỏ nhất và lớn nhất của độ dày đó); R trong → ấn 1 lần (bội 5, < 50, góc > 45°) / pitchi; cuộn (R ngoài 100 / 120 … 300, máy lớn > 300); **taro** theo bảng QC E-1-2 (xám = 🟡 xác nhận lại); lỗ Ø < t. AI tìm thêm: thiếu kích thước, mâu thuẫn, hộp / bẻ C / vát / slit, luật công ty, và dịch câu hỏi khách sang tiếng Nhật.
   Mỗi vấn đề 🔴 / 🟡 / ℹ chia 3 nhóm **Hỏi khách / Hỏi nội bộ / Tự xử lý theo QC**, ô **Trả lời** (câu trả lời của khách / quản lý, hoặc cách xử lý — lưu khi rời ô; lần ↻ Kiểm tra lại gửi kèm AI để không hỏi lại; phòng 4 dùng), nút **✓ Đã xử lý / Bỏ qua… (lý do) / ↩ Mở lại** (giữ qua các lần kiểm lại). Bản nháp **お問い合わせ** và **連絡します** (JP + dòng VI, sửa được) → **📤 Gửi lên chuỗi đơn** (xem trước, tag người triển khai + người check; đơn chưa có chuỗi ở kênh thì tạo). **📋 Chép câu hỏi nội bộ** để gửi quản lý. **✓ Xác nhận → phòng 4** chỉ bật khi không còn 🔴 chưa xử lý.
   Lỗ gần đường bẻ cần ô `cach_duong_be` của phòng 2 (thêm 11/11) — đơn đọc trước đó: phòng 2 **↻ Đọc lại tất cả**.
4. **Triển khai** — đợt 3.

Model: chọn ở ⚙ → 🤖 AI (Opus / Sonnet / Haiku 5.5, hoặc theo tab AI trên CAD: Ling / không chọn → `claude-opus-5-5`), effort `high`, quy chuẩn được cache. Tiền mỗi lần hiện dưới phòng 2.

Kết quả: `<thư mục đơn>\ai\tiep_nhan.json`, `chi_tiet.json` (bẻ / lỗ chi tiết nằm ở đây), `kiem_tra.json` (vấn đề + trạng thái). Thư mục đơn = thư mục có sẵn (≤ 3 cấp dưới Thư mục đơn, tên chứa Title — như tab CAD khớp bản vẽ) hoặc tạo `<Thư mục đơn>\<Title>`.

Giới hạn: DWG / STEP chỉ xếp loại, không đọc; DXF không đọc BLOCK / INSERT; DXF nhị phân báo lỗi; PDF > 22 MB phải tách; ảnh chưa gộp vào 番号.PDF; bẻ / lỗ chưa sửa được trên bảng (sửa ở đợt 3).

## Mã

`Program.cs` (cửa sổ), `Services/GraphTeams.cs` (đăng nhập, Graph, cài đặt), `Views/TeamsTabControl.cs` + `TeamsCaiDatWindow.cs` (màn duyệt đơn, ⚙). Khu AI: `Views/AiPhongControl.cs`, `Services/AiDon.cs` (tải tệp, gộp PDF — PdfSharp chỉ biên dịch, lúc chạy nạp từ `_system\bin\ZwCAD`; gọi Claude bằng HttpClient như tab AI), logic thuần `06_Teams/.../Core/PhongTiepNhan.cs`, `ChiTietTam.cs`, `DocDxf.cs`, `PhongKiemTra.cs` (luật phòng 3 + bản nháp + JSON). `Core/*` và `KieuTeams.cs` lấy bằng **link nguồn** ([ADR 0009](../../_docs/adr/0009-ung-dung-ohtateams-rieng.md)) từ `06_Teams\plugin\TeamsTool.Plugin` → sửa ở đó là cả tab CAD lẫn ứng dụng cùng nhận. WPF net48, không gói NuGet nào chạy kèm.

Build: `powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1` (hoặc `dotnet build _apps\OhtaTeams -c Release`). Đang mở OhtaTeams thì tắt trước khi build. `release_ohta.ps1` đưa `OhtaTeams.exe` + `.exe.config` vào zip.
