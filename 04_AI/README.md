# 04_AI — Tab **AI** (AI kiểm tra bản vẽ)

> **Thử nghiệm — chưa có trong bản phát hành** ([ADR 0005](../_docs/adr/0005-tab-ai-dll-rieng.md)). Chỉ máy phát triển có tab này.
> Thiết kế: [_docs/specs/2026-11-04-tab-ai-kiem-tra-ban-ve.md](../_docs/specs/2026-11-04-tab-ai-kiem-tra-ban-ve.md).

Tab gửi dữ liệu trong từng **khung** (polyline kín 4 đỉnh, mặc định layer `Khung_IN`) lên **Claude API** và hiện danh sách cảnh báo:

- **MTZ** — `M=` / `T=` / `Z=` không khớp bảng vật liệu (bảng của tab Fname: cache Google Sheet + phần máy tự thêm đang chờ duyệt).
- **Q** — `Q=` không khớp số lượng trong ghi chú.
- **GHI_CHU** — ghi chú tiếng Nhật có yêu cầu (逆, タップ, 曲げ…) mà F-name không phản ánh.

🔴 = chắc chắn sai, 🟡 = nghi ngờ. Bấm một cảnh báo để zoom tới khung. **AI không sửa bản vẽ hay CSV** — người dùng tự sửa. Không phụ thuộc GRO: bản vẽ chỉ cần có khung.

## Cách dùng

1. Gõ `OHTA` → tab **AI**. Chọn **Layer khung** và **Model** (mặc định `claude-sonnet-5-5`; Opus kỹ hơn và đắt gấp đôi; Haiku rẻ nhất).
2. **▶ Kiểm tra** (= lệnh `OHTAAI`) kiểm mọi khung trong bản vẽ; **⬚ Chọn vùng** (= `OHTAAIVUNG`) chỉ kiểm khung nằm trọn trong vùng quét. Gửi theo lô 10 khung, 3 lô song song; **■ Dừng** để huỷ.
3. Dòng tổng hiện số cảnh báo, token, tiền ước tính (USD). **Nhật ký** mở file log của lần kiểm.
4. **Lời dặn cho AI** (cuối tab): sửa rồi **Lưu** để đổi cách AI kiểm — không cần build lại. **Khôi phục mặc định** ghi lại bản gốc.

| Lệnh | Việc |
|---|---|
| `OHTAAI` | Quét mọi khung trên layer đang chọn ở tab, gửi AI |
| `OHTAAIVUNG` | Như trên, chỉ khung nằm trọn trong vùng chọn 2 điểm |
| `OHTAAIZOOM` | Zoom tới khung theo số thứ tự (tab gọi khi bấm cảnh báo) |

## Tệp

```
04_AI/
├── module.json              Tab "AI" (id "ai", order 40)
├── data/loi_dan.md          Lời dặn đang dùng (sửa trên tab)
├── data/ai.key              API key — KHÔNG lên git (.gitignore)
├── log/                     Nhật ký mỗi lần kiểm (JSON) — không lên git
├── bin/<ZwCAD|AutoCAD>/AITOOL.dll
└── plugin/AiTool.Plugin, AiTool.Tests
```

Cài đặt riêng: `HKCU\Software\OFT_Lisp\OhtaTool\AI` (`layer_khung`, `model`). Build + test: `powershell -ExecutionPolicy Bypass -File OHTA_TOOL\build_ohta.ps1` (chạy cả `AiTool.Tests`).

Dữ liệu gửi đi: mọi đối tượng có tâm hộp bao nằm trong khung (line, cung, tròn, polyline, chữ, dim — kèm layer), toạ độ tính từ góc dưới trái khung, làm tròn 2 chữ số; cùng bảng vật liệu và lời dặn (được Claude cache 5 phút nên các lô sau rẻ hơn). Không gửi ảnh. Ghi chú MLeader được đọc; đối tượng nằm trong block (BlockReference) **không** được đọc. Khung bao trùm trọn một khung khác (vd. viền bản vẽ cùng layer) bị bỏ qua — không phải một tấm.

## Tạo API key Anthropic

1. Vào **console.anthropic.com**, đăng ký / đăng nhập bằng tài khoản công ty.
2. **Billing** (Plans & Billing) → nạp tiền (credit trả trước). Vài USD đủ thử hàng trăm khung với Sonnet.
3. **Limits** → đặt **giới hạn chi tiêu tháng** để không phát sinh ngoài ý muốn.
4. **API Keys** → **Create Key**, đặt tên `OHTA-AI` → chép key (chỉ hiện một lần; bắt đầu bằng `sk-ant-`).
5. Tạo file `04_AI\data\ai.key`, dán key vào **một dòng duy nhất** (dòng bắt đầu bằng `#` được bỏ qua), lưu.
6. Không gửi key qua chat / email, không đưa lên git (repo public). Lộ key thì vào **API Keys** xoá key cũ, tạo key mới.

Chưa có key thì tab báo đỏ "Chưa có API key…" và không gửi gì.
