# Spec — Báo lỗi (Bug Report) cho OHTA TOOL

Chốt qua phiên hỏi đáp ngày 05/11/2026 (Q1–Q16). Kế hoạch triển khai: [_docs/plans/2026-11-05-bug-report.md](../plans/2026-11-05-bug-report.md).

## Mục đích

Người dùng xưởng gửi báo lỗi ngay trong CAD; máy admin (dev) xem, tải dữ liệu, sửa rồi bấm "Đã xử lý". Người gửi **không** theo dõi tình trạng (Q5). Chỉ có loại "Lỗi", chưa có "Góp ý" (Q8).

## Cửa mở form (Q3)

1. Nút **🐞 Báo lỗi** trên tab Info.
2. Lệnh `OHTABUG`.
3. **Tự bật** hộp "Gửi báo lỗi?" (form điền sẵn lỗi) khi (Q9):
   - exception thoát ra tới đầu lệnh C# (OHTATOOL.dll và GROTOOL.dll);
   - `*error*` của `c:grochay` bắt lỗi thật (không phải `*BREAK,*CANCEL*,*EXIT*`).
   - Esc/Cancel không bật. Cùng một lỗi (nguồn + kiểu + dòng đầu thông báo) chỉ hỏi **1 lần mỗi phiên CAD**.

## Form (Q8)

- Mô tả (bắt buộc, không rỗng sau khi trim).
- Tab/lệnh: tự điền (tab đang mở hoặc lệnh gây lỗi), sửa được.
- Mức độ: `Chặn việc` / `Phiền` / `Nhỏ` (mặc định `Phiền`).
- Người gửi: tự điền `Environment.MachineName + " / " + Environment.UserName`, sửa được.

## Đính kèm (Q4, Q6, Q7, Q13)

Mặc định gửi:
1. Thông tin: phiên bản Tool, phiên bản từng tab, ZWCAD/AutoCAD + bản CAD, tên máy (nội dung như `OHTAINFO`).
2. Ảnh **toàn bộ cửa sổ CAD** (bản vẽ + bảng OHTA + dòng lệnh), chụp **trước khi form hiện** (hộp tự bật: chụp lúc lỗi). Form có xem trước + ô tick "Gửi kèm ảnh".
3. 200 dòng cuối nhật ký xoay vòng `%LOCALAPPDATA%\OHTA_TOOL\logs\` (tool tự ghi: lệnh OHTA/GRO, bước chính, cảnh báo, exception; lisp ghi qua `ohta_net_log`).
4. Stack trace exception (nếu có).

Ô tick, mặc định **không** tick: bản vẽ `.dwg` đang mở, CSV vừa xuất, `%TEMP%\gro_thong_tin.txt`. Các file này nén zip; zip > **25 MB** thì chặn, báo người dùng gửi tay qua Zalo/OneDrive.

## Gửi (Q2, Q12)

- Qua Apps Script Web App đang có (cùng `write_url` / `write_key` của Fname), action `submit_bug`. Dòng vào tab `bug_report`; ảnh, nhật ký + stack trace (file .txt) và zip vào thư mục Drive `OHTA Bug Report`.
- Báo lỗi **không** bị chặn bởi `phien_ban_toi_thieu` (máy bản cũ vẫn phải báo lỗi được).
- Gửi lỗi (mất mạng, Apps Script lỗi) → lưu hàng chờ `%LOCALAPPDATA%\OHTA_TOOL\bug_queue\`, lần mở bảng OHTA sau tự gửi lại, báo "Đã gửi N báo lỗi còn chờ". Gửi lại không tạo bản trùng (ID do máy sinh).

## Admin (Q1, Q10, Q11, Q15)

- Admin = tên máy có trong tab `admin` của Sheet (như Fname).
- Tab Info, chỉ ở máy admin: mục **🐞 Báo lỗi (N)** → cửa sổ danh sách: xem mô tả, mở ảnh, mở nhật ký, tải file đính kèm, bấm **Đã xử lý** (nhập bản đã sửa, tuỳ chọn).
- Đã xử lý → dòng chuyển sang tab `bug_done` (thêm `Ngay_xu_ly`, `Ban_sua`), zip đính kèm vào thùng rác Drive; giữ ảnh + nhật ký.
- Mỗi báo lỗi mới Apps Script gửi email tới chủ script (`Session.getEffectiveUser()`), tiêu đề có mức độ.

## Kiến trúc & phiên bản (Q14, Q16)

- Lõi trong `OHTATOOL.dll` (`_system`). GROTOOL.dll gọi vào bằng phản chiếu (không tham chiếu), lisp gọi `ohta_net_log` / `ohta_net_bug_auto` qua `vl-catch-all-apply`. ADR 0007.
- Tool v1.2.3 → **v1.3.0**; Guro v1.5.3 → **v1.6**.
