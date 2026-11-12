# 4 phòng làm việc AI trong OhtaTeams

> Trạng thái: **chờ duyệt** · 2026-11-11 · [ADR 0010](../adr/0010-phong-lam-viec-ai-ohtateams.md) · Thay spec chưa commit `2026-11-10-teams-gan-don-kiem-tra-ai.md` (viết cho tab Teams trong CAD, trước khi có [OhtaTeams.exe](../../_apps/OhtaTeams/README.md)).
>
> Nguồn nghiệp vụ: **QUY CHUẨN CHUNG 2025.04.21** (notebook "OHTA AI"). Bản Markdown ở `_apps/OhtaTeams/data/quy_chuan/` — **gitignore, không lên GitHub** (repo công khai; tài liệu nội bộ có tên khách và link OneDrive). Spec này chỉ trỏ tới mục (vd. "QC B-6-1"), không chép nội dung.

> **Chia lại 3 phòng (chốt với người dùng 12/11/2026, sau đơn chạy thử).** Bản 4 phòng gửi mỗi bản vẽ lên AI 3 lần (đọc, kiểm, tiết diện) — chậm, tốn, và mỗi lần đọc một kiểu. Nay:
> 1. **Nhận & Đọc** = phòng 1 + 2 cũ. Mỗi tệp **một lượt AI** trả thông số + **tiết diện** (x / y, `bien_dang`) + **nghi vấn** (`van_de`, nguồn `doc`); quy chuẩn gửi kèm = bộ 2 + 3 + 4 (bỏ trùng) + `bai_hoc.md`. Đọc xong luật tự chạy.
> 2. **Kiểm tra & Hỏi** = phòng 3 cũ, **không gửi PDF**. Luật C# chạy lại sau mỗi ô sửa (`AiDon.KiemLuat`); thêm luật tiết diện sai (🔴) / thiếu (🟡). Bảng tấm sửa được cả tiết diện (dạng chữ `50 b90 100 b90 50`, `PhongTrienKhai.ChuTd / DocChuTd`). **🤖 Kiểm lại câu trả lời** = luật + AI chỉ đọc chữ (`PhongKiemTra.LoiDanChu`). **✓ Xác nhận → Triển khai** là chốt duy nhất (`chi_tiet.json` `xac_nhan`; `kiem_tra.json` không còn `xac_nhan` / `tu_xac_nhan`).
> 3. **Triển khai & Vẽ** = phòng 4 cũ, **không gọi AI** (`AiDon.TaoTrienKhai` dựng từ bảng tấm, giữ V đã chọn). AI chỉ khi viết mẫu vẽ mới, hoặc đọc bù tiết diện cho đơn đọc bằng bản cũ (tiết diện cũ trong `trien_khai.json` tự chép vào bảng).
> 4. **Bài học** (`Core/BaiHoc.cs`): ô người sửa nhớ giá trị AI (`gia_tri_ai`) + lý do (`ly_do_sua`). Lúc xác nhận, AI xem chỗ sửa (chữ; tích "AI xem lại bản vẽ" thì gửi PDF từng tệp), so với bài học đã có → đề xuất (trùng ý thì `gop_voi` thay dòng cũ). **Chỉ máy Admin** ghi vào `bai_hoc.md` (`quy_chuan\2_doc_ban_ve\` hoặc thư mục dùng chung ở ⚙ → 🤖 AI). > 60 bài học → nhắc dọn.
> Key riêng giữ tên tệp cũ: phòng 1 · 2 · 3 = `ai.key.phong2` · `3` · `4`. Các mục "Phòng 1–4" bên dưới là thiết kế gốc, giữ để tra cứu.

## Mục tiêu

Trong **chi tiết đơn** của OhtaTeams thêm khu **🤖 AI** đưa một đơn qua 4 phòng, giống cách người triển khai đang làm tay theo QC:

| Phòng | Vào | Ra |
|---|---|---|
| 1 Tiếp nhận | Tệp đính kèm của dòng Lists + tệp trong chuỗi chat của đơn | Tệp đã xếp loại trong thư mục đơn, bản nháp `<mã>-番号.PDF`, **loại đơn**, **công ty đặc biệt** |
| 2 Đọc bản vẽ | PDF (Claude đọc ảnh) + DXF (đọc hình học bằng code) | Bảng chi tiết: mỗi dòng một tấm |
| 3 Kiểm tra | Bảng chi tiết + luật QC | Danh sách vấn đề chia 3 nhóm; bản nháp `お問い合わせ` / `連絡します`; người dùng chốt |
| 4 Triển khai | Bảng chi tiết đã chốt | Kích thước trải phẳng, V / δ từng chỗ bẻ, X × Y, dòng CSV; gửi lên chuỗi đơn để kiểm tra |

Không làm (để sau): DWG / STEP đọc hình học, vẽ hình trải phẳng ra DXF, pitchi / kakumaru / ensui / ống co (QC B-3, B-4, C-3 — dùng SolidWorks), tự đổi Trạng thái đơn, chạy trong CAD.

## Luồng

1. Mở chi tiết đơn → khu 🤖 AI (gập được, mặc định gập). Bấm **▶ Bắt đầu** → chạy 1 → 2 → 3 rồi **dừng ở 3** chờ người dùng. Mỗi phòng có **↻ Chạy lại**.
2. Kết quả mỗi phòng lưu ở `<Thư mục đơn>\<Title>\ai\` (`tiep_nhan.json`, `chi_tiet.json`, `kiem_tra.json`, `trien_khai.json`). Mở lại đơn → hiện ngay, không gọi AI lại. Tệp đầu vào không đổi (so **SHA-256**) thì phòng 2 không gọi lại Claude.
3. Thư mục đơn = `thu_muc_goc` ở ⚙ (đã có, ADR 0009). Chưa đặt → khu AI báo "Đặt Thư mục đơn ở ⚙ trước" và không chạy.
4. Mỗi lần gọi Claude hiện token vào/ra và $ (như tab AI). Khi Claude chạy: dòng trạng thái + nút **Huỷ**.

## Phòng 1 — Tiếp nhận

- Tải mọi tệp của đơn (`GraphTeams.TepCuaDon` / `TaiTep`, tệp chat `TaiTepChat`) vào `<Title>\goc\` — giữ tên gốc, không đổi gì trên Teams.
- **Xếp loại tệp** theo đuôi + nội dung trang đầu: `ban_ve` (PDF/DXF/DWG một hoặc nhiều tấm) · `3d` (STEP/STP) · `don_hang` (注文書, email, ảnh khách) · `khac`. DWG / STEP: chỉ xếp loại, ghi "mở bằng CAD / SolidWorks".
- **Bản nháp `<mã>-番号.PDF`** (QC I.1.A): gộp PDF đơn hàng + email + bản vẽ, xoay trang về đúng chiều, thứ tự đơn hàng → bản vẽ. Gộp bằng `PdfMerger` có sẵn (`_system`, PdfSharp — ADR 0005). Tô màu / ghi chú vẫn làm tay.
- **Loại đơn** (QC I.1): `moi` · `can_lien_lac` (có pitchi / kakumaru…) · `repeat` · `nua_repeat`. **Công ty đặc biệt** (QC III): theo cột Công ty của đơn + tên khách trong 注文書.
- `repeat` → **dừng**, không gọi AI phòng 2–4, ghi "Repeat — làm theo QUY TRÌNH REPEAT". `nua_repeat` → phòng 2 hiện danh sách tấm, người dùng tích tấm mới; phòng 3–4 chỉ chạy tấm đã tích.
- Người dùng sửa được loại tệp, loại đơn, công ty trước khi sang phòng 2.

## Phòng 2 — Đọc bản vẽ

- **PDF** → Claude API (gửi PDF dạng `document`), trả JSON theo schema; mỗi tấm một dòng:
  `so_ban_ve, ten, vat_lieu_goc, M, Z, T, Q, mat (HL/#400/sima/…), kich_thuoc_bao, canh[] (dài ngoài, góc, R trong, hướng V/N), lo[] (Ø, tâm, M-taro, 皿), kaisaki, dung_sai, ghi_chu_jp, ghi_chu_vi, trang`.
  `M`, `Z` quy về bảng `01_Fname/data/catalog.csv`; thiếu vật liệu → chọn theo QC II.1.C-1 và **đánh dấu "AI chọn"**.
- **DXF** → đọc ENTITY bằng code (LINE / ARC / CIRCLE / LWPOLYLINE / TEXT / MTEXT / DIMENSION) để lấy lỗ, kích thước bao, chữ; vật liệu / số lượng lấy từ chữ hoặc từ PDF cùng số bản vẽ. Không cần CAD.
- Ô nào AI không chắc → tô vàng. Người dùng sửa từng ô; ô đã sửa không bị lần chạy lại ghi đè.
- Lời dặn gửi Claude = `quy_chuan/*.md` (phần đọc bản vẽ + từ vựng tiếng Nhật) + schema. Key: dùng chung `05_AI\data\ai.key`, model đọc từ cài đặt tab AI.

## Phòng 3 — Kiểm tra

Luật có số cố định viết bằng **C# thuần** (`Core/`, có test); luật riêng từng công ty + giải thích lấy từ `quy_chuan/*.md` gửi Claude rà thêm. Mỗi vấn đề: mức 🔴 / 🟡, tấm, mục QC, và **một trong 3 nhóm**:

| Nhóm | Làm gì |
|---|---|
| **Tự xử lý theo QC** | Chỉ ghi chú (vd. "R10 góc 90° → ấn 1 lần", "chọn V32 b8/4") |
| **Hỏi nội bộ** | Câu hỏi tiếng Việt gửi quản lý |
| **Hỏi khách** | Câu tiếng Việt + bản dịch tiếng Nhật → bản nháp `お問い合わせ`; slit / phần thừa → bản nháp `連絡します` |

Luật C# bước đầu (số liệu tra bảng `01_Fname/data/*.csv` khi có, không chép cứng):

| Luật | QC | Bảng |
|---|---|---|
| Lỗ nằm trong **Lmin** tính từ đường bẻ → biến dạng, đề xuất slit / phần thừa | B-6-1 | `trav_90.L_saitei` theo M, T, V |
| Cạnh bẻ ngắn hơn Lmin | B-2 | `trav_90` |
| 内R bội 5, < 50, góc > 45° → ấn 1 lần; R khác không cuộn được → pitchi (làm tay) | B-2-4, B-3 | — |
| Cuộn: R / chiều dài trục theo độ dày, máy nhỏ / lớn | C-2 | bảng trong QC |
| Hộp 3–4 cạnh: c > 150 hoặc c > a, b → tách; bẻ C: a < c → bẻ W | B-2-1-2, B-2-1-3 | — |
| Vát: A ≤ 2000, B ≥ 100, 6 ≤ t ≤ 30, 35° ≤ góc ≤ 60° | D-2 | — |
| Xác nhận độ dày: SS t3.2, SUS t < 0.5; tấm < 3.2 đường bẻ > 2400; PL > 16 | II.1.C-1, B-1 | — |
| Taro M theo vật liệu / độ dày: máy di động / cố định / không làm được | E-1-2 | bảng màu QC (xem Câu hỏi mở 2) |
| Lỗ nhỏ hơn khả năng mở lỗ laser | A-1-4 | `Tiêu chuẩn mở lỗ (TruLaser3060-12kw).xlsx` — **chưa có**, tạm 🟡 "Ø < t, xác nhận" |
| Vật liệu không có trong `catalog.csv` | II.1.B | `catalog.csv` |

Chốt: mỗi vấn đề có nút **Đã xử lý / Bỏ qua (ghi lý do) / Sửa ô**. Câu "Hỏi khách" đã duyệt → **Gửi lên chuỗi đơn** (dùng `GraphTeams.TraLoi`, tag người triển khai + người check như `ChuyenTiep`) — gửi khi người dùng bấm, xem trước nội dung. **✓ Xác nhận → Triển khai** chỉ bật khi không còn 🔴 chưa xử lý.

## Phòng 4 — Triển khai

- Bẻ L / C / Z / hộp: kích thước ngoài, mỗi chỗ bẻ trừ **2δ** (QC B-2-1). V và δ lấy từ `TraVCalculator.GetBendOptionsForFname(M, T, góc)` (90°, góc tù, góc nhọn, bẻ thử / nội suy) — link nguồn `TraVCore.cs`, `TraVThuCore.cs`, `TamCore.cs` từ `_system` như `JsonLite`. V mặc định theo QC (V đầu với t ≥ 1.6, V thứ hai với t < 1.6); luật V riêng của công ty đặc biệt theo `quy_chuan/*.md`.
- Ấn 1 lần: đường trung bình **0.45 · T thực** (QC B-2-4); cuộn: **0.5 · T thực** (C-2); sima: kích thước trong + **T thực** (B-5-1). T thực từ `tam.csv`.
- Hộp: góc liên kết hở / kín, khe Ø theo độ dày (B-2-1-3).
- Pitchi / kakumaru / ống co → dòng "làm tay trên SolidWorks", không tính.
- Mỗi tấm ra: các đoạn trải, từng chỗ bẻ (góc, V, δ, 2δ, nguồn số liệu: bảng / bẻ thử / nội suy + cảnh báo của TraV), **X × Y**, và **dòng CSV** (QC I.1.E: bango, 数量, 材質, 板厚, 曲げ `r/m/t/s/*`, X, Y).
- **Gửi lên chuỗi đơn**: bảng tóm tắt (xem trước, người dùng bấm gửi), tag người triển khai + người check để kiểm tra. Lưu `trien_khai.json` + `trien_khai.csv` trong `ai\`.

## Mã

```
_apps/OhtaTeams/
├── Views/AiPhongControl.cs        khu 🤖 AI trong HienChiTiet (4 bước, bảng sửa được)
├── Services/ClaudeTeams.cs        gọi Claude (HttpClient, như 05_AI ClaudeHttp — không NuGet)
└── data/quy_chuan/*.md            (gitignore) QC tách theo phần, gửi kèm lời dặn
06_Teams/plugin/TeamsTool.Plugin/Core/   (thuần, OhtaTeams link nguồn, test trong TeamsTool.Tests)
├── PhongTiepNhan.cs               xếp loại tệp, loại đơn, công ty
├── ChiTietTam.cs                  model + đọc/ghi chi_tiet.json, khoá ô người dùng đã sửa
├── DocDxf.cs                      đọc DXF ASCII → lỗ, chữ, kích thước bao
├── LuatQuyChuan.cs                luật phòng 3
└── TrienKhai.cs                   trải phẳng + dòng CSV (gọi TraVCalculator)
```

- Không tham chiếu `AITOOL.dll` / `OHTATOOL.dll`; dùng lại bằng **link nguồn** (ADR 0009). Không thêm NuGet.
- Quyền Graph: không thêm (đã có đọc tệp, `ChannelMessage.Send`).
- Gửi dữ liệu ra ngoài: chỉ tệp của đơn gửi Claude API, khi bấm ▶. Lần đầu bật trên máy → hỏi xác nhận một lần (ghi registry `ai_dong_y`).

## Nguồn AI qua CLI (12/11/2026)

Ngoài key API: `claude -p` (Claude Code, gói Claude) và `agy -p` (Antigravity, gói Google, Gemini) — cả hai có `--output-format json`, `--json-schema`, `--effort`. `Core/AiCli.cs`: tách body /v1/messages → `yeu_cau.md` (lời dặn + QC + phần đơn; dòng lệnh Windows giới hạn ~32 nghìn ký tự) + `ban_ve.pdf` trong thư mục tạm; Claude Code chỉ `--allowed-tools Read`; kết quả (`structured_output` / `result` / `response`) đổi về dạng phản hồi API, usage = 0 (tính trong gói). `AiDon.GuiAi` chọn theo `ai_nguon`. Thử thật: Antigravity đọc PDF 130672 đúng (152 giây); Claude Code trên máy dev đang hết hạn mức tháng (429) — chưa thử lượt thành công, bộ chuyển đổi theo dạng kết quả tài liệu (`structured_output`). Rủi ro: điều khoản gói cá nhân khi dùng cho cả xưởng; Gemini khác Claude với lời dặn hiện tại.

## Bản vẽ phòng 4 bằng ezdxf (chốt 12/11/2026 — thay text-to-cad)

Lý do: bản vẽ xưởng là 2D sống nhờ F-name TEXT, dim, nét DASHED, layer OHTA → ezdxf ghi được hết; text-to-cad không xuất TEXT / dim / kiểu nét (giữ text-to-cad cho đơn có STEP 3D cần trải phẳng). Sinh bản vẽ không tốn token (AI chỉ đọc tiết diện + chọn loại biên dạng).

- **Môi trường vẽ** = `data/quy_chuan/4_trien_khai/moi_truong/moi-truong-ve.dxf` (gitignore, người dùng gửi — `mau.dxf`): R2007, cp932, mm, 10 layer OHTA, kiểu chữ `Text` (MS PGothic), kiểu dim `ISO-26` (DIMSCALE 8), DASHED / CENTER. **Giữ dạng DXF** (không chuyển Python): ezdxf mở làm nền cho mọi bản vẽ, vẽ vào Model, lưu `<bango>.dxf`. Tóm tắt layer / kiểu chữ / dim ở `moi-truong-ve.md` cạnh đó (thư mục con — không gửi AI cùng QC).
- **Thư viện biên dạng** = tệp Python trong repo (người dùng chọn: có lịch sử, tự đến máy khác qua cập nhật; mã dựng hình chung, không dữ liệu khách). Mỗi loại một tệp (`phang`, `be` L/C/Z một hướng, `hop`, `an_1_lan`, `cuon`…): mô tả (để AI chọn) + hàm `ve(...)` nhận số C# đã tính (X, Y, vị trí đường bẻ, đoạn phẳng, F-name, bango) và vẽ vào Model theo môi trường.
- **AI phòng 4 xác nhận loại biên dạng**: chọn một mẫu có trong thư viện (danh sách + mô tả gửi kèm) hoặc "chưa có" + mô tả. Có mẫu → vẽ nhanh, không tốn token. Chưa có → đề xuất mã Python mẫu mới; **chỉ máy Admin** được thêm vào thư viện chung (máy thường chỉ vẽ thử cho đơn đó); trước khi thêm: xem mã + vẽ thử lên tấm đang làm, mở CAD kiểm.
- **ezdxf trên máy**: chưa có Python / ezdxf → báo và đề nghị cài (`py -m pip install ezdxf`, người dùng bấm đồng ý).
- Bản vẽ: viền cắt `0-KAKOU`, đường bẻ LINE `0-KAKOU` nét DASHED, F-name TEXT `2-F-NAME` (kiểu `Text`, dòng `V=` từ TraV), dim tổng `1-SUNPOU` (ISO-26), `Khung_IN` bao tấm (không vẽ mã khắc — GRO đánh trên CAD, tránh GRO đọc lại). Đã sửa tay trên CAD thì không ghi đè (ghi `<bango> (2).dxf`).

## Đợt làm

1. ✅ (mã xong 11/11, **chờ chạy thử trên đơn thật**) Khu 🤖 AI + phòng 1 + phòng 2 (PDF + DXF). Chạy thử trên 3 đơn thật (có trong `Tài liệu\OHTA_DonTeams\`).
   Khác spec: chưa xoay trang 番号.PDF (không biết chiều trang khi chưa đọc nội dung — để AI báo ở đợt sau); ảnh chưa gộp vào 番号; công ty đặc biệt đoán theo `quy_chuan/cong_ty.txt` (gitignore) thay vì chữ trong 注文書; bảng chỉ sửa ô chữ, bẻ / lỗ sửa ở đợt 3.
   Sau đợt 1 (11/11): chạy thật đơn 130672 (1 tấm, $0.06); khu AI chuyển ra khung bên phải màn danh sách (chuột phải đơn → 🤖 Mở trong phòng AI); quy chuẩn chia bộ riêng từng phòng (`quy_chuan/<n_ten>/`); key riêng từng phòng (bật / tắt); nút **✓ Xác nhận bảng → phòng 3** (`chi_tiet.json` `xac_nhan`).
2. ✅ (mã xong 11/11, **chờ chạy thử trên đơn thật**) Phòng 3: luật C# + nhóm vấn đề + お問い合わせ / 連絡します + gửi lên chuỗi đơn. Đầu vào: bảng tấm đã xác nhận ở phòng 2. Mã: `Core/PhongKiemTra.cs` (thay tên `LuatQuyChuan.cs`), `AiDon.KiemTraDon`, kết quả `ai\kiem_tra.json`.
   Khác spec: phòng 2 thêm ô `lo.cach_duong_be` (mép lỗ → đường bẻ gần nhất) để luật lỗ gần đường bẻ chạy được — bảng đọc trước đó nhắc đọc lại; Lmin xét **V nhỏ nhất và lớn nhất** của độ dày (QC "V đầu tiên" không khớp thứ tự bảng TV: ví dụ QC SS400 PL4.5 chọn V32 là dòng thứ 2) — dưới Lmin nhỏ nhất 🔴, giữa 🟡 chọn V nhỏ; vật liệu ngoài `catalog.csv` không làm luật C# (catalog chỉ 13 vật liệu, báo nhầm nhiều) — để AI rà theo bảng C-1 / C-2; hộp c > 150, bẻ C a < c, vát để AI (bảng tấm không có a, b, c); nút "Sửa ô" trên từng vấn đề chưa làm — sửa bằng ↩ Bỏ xác nhận ở phòng 2. Câu hỏi nội bộ: chép ra để gửi quản lý (chưa gửi tự động).
3. ✅ (mã xong 12/11, **chờ chạy thử trên đơn thật**) Phòng 4: trải phẳng qua TraV + dòng CSV + gửi lên chuỗi đơn. Mã: `Core/TrienKhai.cs` (`PhongTrienKhai`), `AiDon.TinhLai` / `TrienKhaiDon`, kết quả `ai\trien_khai.json` + `trien_khai.csv`.
   Khác spec: AI đọc thêm **tiết diện** (đoạn ngoài / góc / R theo 2 hướng X, Y) vì bảng phòng 2 (canh: cặp kích thước kề) không đủ để trải; δ lấy từ chữ ứng viên của `TraVCalculator.GetBendOptionsForFname` (chuỗi V= tab Fname ghi vào F-name); V mặc định = ứng viên đầu có Lmin ≤ cạnh ngắn nhất (QC "V đầu tiên" khác thứ tự TraV — ví dụ QC t4.5 chọn V32), người dùng đổi V từng tấm; hộp: góc liên kết hở / kín, khe R chưa tính (chỉ X × Y); sima (B-5-1) chưa có luật riêng; chưa sửa tay tiết diện trên màn (sửa bằng câu trả lời phòng 3 rồi ↻ Triển khai lại); CSV chưa có cột 単価. Thêm 12/11: vẽ DXF — lúc đầu bằng text-to-cad, cùng ngày đổi sang **ezdxf** (mục "Bản vẽ phòng 4 bằng ezdxf"): thư viện `_apps/OhtaTeams/ve_dxf` (ve.py, moi_truong.py, thu_vien/phang, be, hop, cuon), AI chọn bien_dang, chua_co → AI viết mẫu (KiemMau) → Admin thêm; bỏ mã khắc (GRO đánh trên CAD, tránh trùng).

Mỗi đợt: `build_ohta.ps1` sạch, test PASS, README `_apps/OhtaTeams` + `06_Teams` + CHANGELOG cập nhật, người dùng chạy thử rồi mới sang đợt sau.

## Test

- Tự động: xếp loại tệp theo tên / đuôi; loại đơn; đọc DXF mẫu (lỗ, chữ JP); mỗi luật phòng 3 (trong ngưỡng / sát ngưỡng / ngoài ngưỡng); trải phẳng L, C, hộp so với ví dụ trong QC (SS400 PL4.5 V32 δ4; SPHC PL2.3 V12 δ1.9; ấn 1 lần T4.5 T thực 4.2 → O = 1.89); dòng CSV đúng định dạng; JSON hỏng / thiếu → coi như chưa chạy.
- Tay: 3 đơn thật → so bảng chi tiết, danh sách vấn đề, X × Y với bản người triển khai đã làm.

## Câu hỏi mở (cần người dùng trả lời trước đợt tương ứng)

1. ✅ *(11/11)* **Số luật trong mã công khai**: **chấp nhận** — ngưỡng máy (Lmin, vát, cuộn, taro) viết trong C# (thông số kỹ thuật, không phải dữ liệu khách); phần riêng từng công ty chỉ ở `quy_chuan/*.md` gitignore.
2. ✅ *(11/11)* **Bảng taro (QC E-1-2)**: ô `C1F0C8` xanh = máy di động, `FFC000` cam = máy cố định, `808080` xám = **🟡 xác nhận lại** (theo chú thích QC; bảng không có ô đen).
3. **Tài liệu kèm chưa có**: bảng mở lỗ laser, bảng delta góc nhọn (QC B-2-3 — kiểm tra trùng `trav_nho90.csv` không), `OHTA仕様書` 8.1 (R ngoài góc nhọn), `R.END 1.pdf`, `JIS.pdf`, quy trình triển khai / repeat. *(đợt 2–3)*
4. **"Ohta Press Calculator"** (QC B-2-2) có cùng số liệu với bảng TV không — kiểm bằng 5–10 ca trước khi dùng cho phòng 4. *(đợt 3)*
5. ✅ *(11/11)* **Chia sẻ `quy_chuan/` cho máy khác**: **chép tay** như `teams.json` (cả 4 thư mục phòng + `_goc` + `cong_ty.txt`).
