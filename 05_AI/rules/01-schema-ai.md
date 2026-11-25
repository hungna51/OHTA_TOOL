# Schema AI trả về (structured output)

Mỗi lượt gọi Claude API gửi `output_config.format = json_schema` (hàm `Schema()` trong `06_Teams/plugin/TeamsTool.Plugin/Core`). API biên dịch schema thành **grammar**; grammar quá lớn → API trả `400 The compiled grammar is too large…` và **cả lượt đọc hỏng** (tab báo "AI chưa đọc lại được — Bước 2").

## Sự cố 25/11

Thêm 2 ô `slit`, `duong_tam` kiểu `[[số…]…]` vào schema bước 2 (`PhongPhongTo.Schema`) → vượt ngưỡng. Đo bằng lời gọi thật:
- 6 ô mảng-số-lồng (`ke_gaki`, `dinh`, `slit`, `duong_tam`, `lo.vi_tri`, `dim_khach.diem`) → **lỗi**; bỏ 2 ô → vừa qua (sát ngưỡng).
- Đổi `ke_gaki`, `dinh`, `slit`, `duong_tam` sang **chuỗi** chứa mảng JSON → qua, còn chỗ thêm ~2 ô mảng-số-lồng; thêm 4 → lại lỗi.

## Luật

1. **Ô mới dạng mảng số / toạ độ → khai báo `string`** (chuỗi chứa mảng JSON, vd `"[[0,0],[10,0]]"`), không khai báo `array` lồng `array` số. Đọc lại bằng `PhongPhongTo.MangHoacChuoi` (nhận cả mảng lẫn chuỗi, chuỗi hỏng → bỏ ô đó, không hỏng cả lượt). Lời dặn (`LoiDan`) phải nói ô đó trả dạng chuỗi.
2. **Ngân sách**: schema bước 2 có tối đa **4** ô `array` → `array` → `number` (hiện 2: `lo.vi_tri`, `dim_khach.diem`). Test `TeamsTool.Tests` "schema buoc 2: ngan sach grammar" chặn — test đỏ thì đổi ô mới sang chuỗi, đừng nâng ngân sách khi chưa đo lại bằng API.
3. **Ô object mới** (mảng object nhiều trường) cũng làm grammar lớn nhanh: ưu tiên gộp vào ô có sẵn hoặc dạng chuỗi.
4. **Sửa schema xong phải gọi thử API** trước khi gộp main: xuất schema từ `OhtaTeams.exe` vừa build (PowerShell 5.1: `LoadFrom` exe → `TeamsTool.Core.PhongPhongTo.Schema()` → `JsonLite.Serialize`) rồi gửi một lời gọi nhỏ (`max_tokens` 16, không ảnh) với `output_config` đó; phải ra 200. Khoá ở `05_AI/data/ai.key` — không in ra, không commit.
