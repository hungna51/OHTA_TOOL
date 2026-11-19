# Tab CSV — Giai đoạn 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tab `CSV` mới trên bảng OHTA: EXPORT (B2) + danh sách tấm + 2 file CSV viết bằng C#; nút B1 tạm gọi lisp cũ.

**Architecture:** Mục con `07_CSV` có DLL riêng `CSVTOOL.dll` nạp qua `DllTabLoader` (giống `05_AI`). Logic thuần (đọc MText, F-name, dim → dòng CSV, sắp xếp, ghi file) nằm trong `Core/` không phụ thuộc CAD để test bằng `dotnet run`; `CadServices/` chỉ gom dữ liệu thô từ bản vẽ và ghi ngược. Lisp cũ chép vào `07_CSV/lisp/` chỉ để chạy B1 tới khi xong giai đoạn 2.

**Tech Stack:** C# (net48 cho ZWCAD 2026, `$(AutoCadTfm)` cho AutoCAD), WPF, test console net10 (`TestRunner` tự viết như `AiTool.Tests`), AutoLISP (chỉ wrapper B1).

**Spec:** [`_docs/specs/2026-11-19-tab-csv.md`](../specs/2026-11-19-tab-csv.md)

**Nguồn đối chiếu:** lisp `laptrinhlisp\1. xuất fname ra csv\xuat_CSV_V4\xuat_csv_v5.0.1 ZWCAD.lsp` (sau Task 7 có bản ở `07_CSV/lisp/`). Mỗi task ghi tên hàm lisp cần chép **đúng hành vi** — người làm đọc thân hàm đó.

## Global Constraints

- Tuân theo `OHTA_TOOL/rules/01..04`; chỉ sửa `07_CSV/` + các điểm nối hệ thống liệt kê trong Task 1.
- `.cs/.xaml/.json/.md` UTF-8; `.lsp` **100% ASCII**, không BOM; nhãn tiếng Việt trong lisp dùng `\U+XXXX`.
- Mọi đọc/ghi CAD từ palette bọc `using (doc.LockDocument())`; dùng `#if ZWCAD` như `05_AI` (không có `CadCompat` trong DLL tab).
- CSV: UTF-8 **có BOM**, xuống dòng **CRLF** sau **mọi** dòng (kể cả dòng cuối) — giống ADODB `WriteText ln 1`.
- Tiêu đề `<đơn>.csv` đúng chữ: `エクスポート用,数量,材質,板厚,noと安全,曲げ,X,Y,,単価`
- Tiêu đề `data\<đơn>-data.csv` đúng chữ (1 dòng): `Ten tam,Vat lieu,Do day,X (mm),Y (mm),So luong,Lo nho,Lo lon,Lo Tappu,Lo Sara,Pitchi - So lan,Pitchi - Chieu dai (mm),Loai gia cong,Loai cuon,Vat (mm),Be1 - So duong,Be1 - Dai (mm),Be2 - So duong,Be2 - Dai (mm),Be3 - So duong,Be3 - Dai (mm),Be4 - So duong,Be4 - Dai (mm),Be5 - So duong,Be5 - Dai (mm),Be6 - So duong,Be6 - Dai (mm),Be7 - So duong,Be7 - Dai (mm),Be8 - So duong,Be8 - Dai (mm),Che do tinh,Gia bao gia,Tien vat lieu(nhap san),Uu dai(%)`
- Layer cố định: F-name `2-F-NAME`, MText thông tin tấm `KHUNG_IN`. Layer cài đặt được: khung (mặc định `Khung_IN`), dim tổng (`1-XY`), tapu (`TAPU_TMT`). So tên layer **không phân biệt hoa thường**.
- Registry: `HKCU\Software\OFT_Lisp\CSV`.
- `module.json`: `"id": "csv"`, `"adminOnly": true`, `"order": 70`, `"tabTitle": "CSV"`, `"version": "0.1"`, `"stage": "beta"`.
- Trước khi coi là xong: `powershell -ExecutionPolicy Bypass -File build_ohta.ps1` build sạch cả 2 bản + toàn bộ test PASS.

## Review Focus

1. **Định dạng số X/Y** (`rtos` theo DIMDEC của dim + DIMZIN của bản vẽ): `320` / `301.1` / `1767.77` phải ra đúng như CSV lisp → test vàng Task 6 + test `Rtos` Task 3.
2. **MText lưu ký tự dạng `\U+1ED7`** (font SHX) thay vì Unicode thật → parser phải nhận cả hai → test Task 2.
3. **Excel đang mở `<đơn>.csv`** → không được ghi một nửa (file 10 cột ghi xong, file data lỗi) → test Task 4 `GhiHaiFile_KhoaFile_KhongGhiGi`.
4. **Thư mục `data` chưa có / dwg chưa lưu (`DWGPREFIX` rỗng)** → tự tạo `data`; dwg chưa lưu thì báo lỗi rõ, không ghi → test Task 4.
5. **Sửa ô trên danh sách tấm khi MText đã bị người dùng sửa tay lệch nhãn** → chỉ thay đúng dòng có nhãn, không có nhãn thì báo lỗi, không ghi bừa → test Task 2 `ThayGiaTri_KhongCoNhan`.

---

### Task 1: Khung mục con `07_CSV` + nạp tab + lệnh `CSV`

**Files:**
- Create: `07_CSV/module.json`, `07_CSV/README.md`, `07_CSV/rules/00-muc-luc.md`
- Create: `07_CSV/plugin/CsvTool.Plugin/CsvTool.Plugin.csproj` (chép `05_AI/plugin/AiTool.Plugin/AiTool.Plugin.csproj`, đổi `RootNamespace=CsvTool`, `AssemblyName=CSVTOOL`, bỏ PackageReference/Link không dùng; giữ Link `FnameCore.cs`)
- Create: `07_CSV/plugin/CsvTool.Plugin/CsvOhtaTab.cs`, `Views/CsvTabControl.xaml(.cs)` (tạm: tiêu đề + mô tả)
- Create: `07_CSV/plugin/CsvTool.Tests/CsvTool.Tests.csproj` (chép `AiTool.Tests.csproj`: net10, `Compile Include="..\CsvTool.Plugin\Core\*.cs"` + Link `FnameCore.cs`), `Program.cs`, `TestRunner.cs`
- Modify: `_system/OhtaTool.Plugin/Views/ModuleViewRegistry.cs:66-70` (thêm sau khối Teams)
- Modify: `_system/OhtaTool.Plugin/OhtaPaletteCommands.cs:117` (thêm lệnh sau `OI`)
- Modify: `build_ohta.ps1` (biến `$CsvProj/$CsvTestProj/$HasCsv` cạnh dòng 14-16; build cạnh dòng 75; `Publish-CadDll` cạnh dòng 159 và 165 với tên `CSVTOOL`, đích `07_CSV\bin\<ZwCAD|AutoCAD>`; chạy test cạnh dòng 178)
- Modify: `release_ohta.ps1:53` (thêm `"07_CSV/bin/ZwCAD/CSVTOOL.dll", "07_CSV/bin/AutoCAD/CSVTOOL.dll"`)
- Modify: `GEMINI.md` bảng mục 4 (thêm dòng 07), `CLAUDE.md` gốc workspace không đụng.

**Interfaces:**
- Produces: `public class CsvTool.CsvOhtaTab { public static FrameworkElement TaoGiaoDien(string tieuDe, string moTa, string mauNhan, string thuMucModule) }` — hợp đồng phản chiếu với OHTATOOL.dll.
- Produces: `TestRunner.Chay(string ten, Action a)` + `Program.Main` trả mã thoát ≠ 0 khi có test FAIL (đúng kiểu `AiTool.Tests`).

- [ ] **Step 1:** Tạo các file trên. Đăng ký:
```csharp
Func<ModuleManifest, Action, FrameworkElement> csvFactory = (manifest, reloadCallback) =>
    DllTabLoader.TaoGiaoDien(manifest, "CSVTOOL", "CsvTool.CsvOhtaTab", "CSV") ??
    new ModuleTabControl(new ModuleTabViewModel(manifest, reloadCallback));
Register("csv", csvFactory);
Register("07_CSV", csvFactory);
```
và `[CommandMethod("CSV")] public static void CmdOpenCsvTab() => BugGuard.Run("CSV", () => ActivateModuleTab("csv"));`
- [ ] **Step 2:** Thêm 1 test giả `Khung_ChayDuoc` (assert true) để runner có ít nhất 1 test.
- [ ] **Step 3:** Run: `powershell -ExecutionPolicy Bypass -File build_ohta.ps1` → Expected: `[OK] Da xuat DLL ... CSVTOOL.dll` cho mỗi bản có SDK; mọi bộ test PASS.
- [ ] **Step 4:** Commit `07_CSV: khung tab CSV (CSVTOOL.dll) + lenh CSV mo tab`.

---

### Task 2: Đọc / sửa MText thông tin tấm (`Core/ThongTinTam.cs`)

**Files:**
- Create: `07_CSV/plugin/CsvTool.Plugin/Core/ThongTinTam.cs`
- Test: `07_CSV/plugin/CsvTool.Tests/TestThongTinTam.cs`

**Interfaces:**
- Produces:
```csharp
public sealed class ThongTinTam {
  public int LoNho, LoLon, Tappu, Sara, PitchiSoLan, PitchiDai, Vat;
  public List<(int Dai, int SoLuong)> DuongBe;   // thứ tự xuất hiện
  public string LoaiGiaCong, CheDoTinh, GiaBaoGia, TienVatLieu, UuDai; // giữ nguyên chuỗi
}
public static class ThongTinTamParser {
  public static List<string> TachDong(string noiDungThoMtextHoacText, bool laMtext); // = CSV--mtext-plain + CSV--pricing-text-lines
  public static bool LaTextThongTin(IEnumerable<string> dong);                       // có dòng chứa "TAPPU" hoặc "TAPU&SARA" (so chữ hoa)
  public static (ThongTinTam Tam, string Loi) Doc(IReadOnlyList<string> tatCaDong);  // = CSV--pricing-data-from-text; Loi != null khi sai
  public static string ThayGiaTri(string noiDungMtext, string nhanKhongDau, string giaTriMoi); // ném InvalidOperationException nếu không thấy nhãn
  public static bool WcMatch(string s, string mau); // chỉ hỗ trợ '*'; so trên chuỗi đã viết hoa ASCII a-z
}
```

- [ ] **Step 1: Test hỏng** — `TestThongTinTam.cs`, các ca (dòng MText đúng kiểu B1 sinh ra):
  - `Doc_DayDu`: 12 dòng + `Đường bẻ 1: 200 x 1` + `Các loại gia công: m` → `LoNho==4, DuongBe[0]==(200,1), LoaiGiaCong=="m"`, `Loi==null`.
  - `Doc_EscapeUnicode`: cùng nội dung nhưng nhãn dạng `L\U+1ED7 nh\U+1ECF: 4` → kết quả giống hệt `Doc_DayDu`.
  - `Doc_SaiSoDong`: có `m`, 2 đường bẻ nhưng chỉ 13 dòng → `Loi` bắt đầu bằng `"Số lượng text thông tin không chính xác (Thực tế: 13 text, Yêu cầu: 14 text)."`
  - `Doc_DuongBe11`: nhãn `Đường bẻ 11: 50 x 2` → tính chỉ số 11 (không phải 1) — theo `CSV--trailing-int`.
  - `Doc_ConTrongGiuChuoi`: `Giá báo giá:` rỗng → `GiaBaoGia==""`; `Ưu đãi(%): 5.5` → `"5.5"`.
  - `ThayGiaTri_Sara`: thay `Lỗ Sara` thành `3` → chỉ dòng đó đổi, mọi `\P` và dòng khác giữ nguyên.
  - `ThayGiaTri_KhongCoNhan` → ném `InvalidOperationException`.
- [ ] **Step 2:** Run `dotnet run --project 07_CSV/plugin/CsvTool.Tests -c Release` → FAIL (chưa có lớp).
- [ ] **Step 3:** Cài đặt. Chép **đúng thứ tự** các nhánh `cond` của `CSV--pricing-data-from-text` (Tappu → Sara → `PITCHI*(MM)*` → `PITCHI*` → `CH*NH:*` → `GI*` → `TI*` → `*(%):*` → `L*NH*:*` → `L*L*N:*` → vát → `*C*C LO*I GIA C*NG*` → dòng đường bẻ có `x`). Trước khi so mẫu: giải mã `\U+XXXX` thành ký tự rồi bỏ dấu tiếng Việt (`đ/Đ`→`d/D`) và viết hoa — nhờ vậy cả hai dạng lưu cho cùng kết quả. Số đọc như `atoi` (lấy phần số nguyên đầu, không có thì 0). `expected = 12 + (LoaiGiaCong chứa 'M' ? maxChiSoBe : 0)`. Thông báo lỗi viết tiếng Việt có dấu đúng như lisp.
- [ ] **Step 4:** Run → PASS.
- [ ] **Step 5:** Commit `07_CSV: doc/sua MText thong tin tam`.

---

### Task 3: Phân tích 1 khung (F-name + dim + thông tin → `TamXuat`) (`Core/PhanTichKhung.cs`)

**Files:**
- Create: `07_CSV/plugin/CsvTool.Plugin/Core/PhanTichKhung.cs`
- Test: `07_CSV/plugin/CsvTool.Tests/TestPhanTichKhung.cs`

**Interfaces:**
- Consumes: `ThongTinTamParser.Doc`, `FnameParser.Parse(IEnumerable<string>)` (`_system/OhtaTool.Plugin/Core/FnameCore.cs:593`).
- Produces:
```csharp
public sealed class TextTho { public string Lop, NoiDung; public bool LaMtext; }
public sealed class DimTho { public string Lop; public int Co70; public double DoDai; public int Dimdec; } // Co70 = DXF 70
public sealed class KhungTho { public double MinX, MinY, MaxX, MaxY; public long Handle;
  public List<TextTho> Text; public List<DimTho> Dim; }                 // chỉ các đối tượng NẰM TRỌN trong khung
public sealed class TamXuat { public string N, M, T, Q; public string X, Y; public ThongTinTam Tt;
  public double DiemX, DiemY; public long HandleKhung; }                  // DiemX = MinX, DiemY = MaxY (pointss)
public static class PhanTichKhung {
  public static (TamXuat Tam, string Loi) Chay(KhungTho k, string lopDim, int dimzin);
  public static string Rtos(double v, int soLe, int dimzin); // rtos mode 2: bit 8 của dimzin → bỏ số 0 cuối (và dấu chấm thừa)
}
```

- [ ] **Step 1: Test hỏng** — các ca (thông báo đúng chữ lisp, tiếng Việt có dấu):
  - `Rtos`: `Rtos(320,2,8)=="320"`, `Rtos(301.1,2,8)=="301.1"`, `Rtos(1767.774,2,8)=="1767.77"`, `Rtos(320,2,0)=="320.00"`.
  - `KhongFname` → `"Không tìm thấy F-name!!!"`; `HaiMtextFname` → `"Có nhiều hơn 1 Fname trong khung!!!"`; `TronMtextVaText` → thông báo "Khung in có các loại Fname không hợp lệ!…".
  - `FnameTruongRong` (`Q=` rỗng) → `"Lỗi Fname (2): Giá trị trường thông tin bị rỗng!"`.
  - `HaiDimThang`: 2 dim `Co70=32` (300 và 200) → `X=="300", Y=="200"`; đảo thứ tự → vẫn X ≥ Y.
  - `BaDimThang` → `"Số lượng dim tổng không đúng!!!"`.
  - `MotDimPhi`: không có dim thẳng, 1 dim `Co70=163` 150 → `X==Y=="150"`.
  - `KhongDim` → `"Không tìm thấy dim tổng, kiểm tra lại dữ liệu!!!"`.
  - `VatChuaNhap`: gia công có `k`, `Vat==0` → lỗi "Chi tiết có gia công vát (k) nhưng chưa nhập chiều dài vát!"; `PitchiChuaNhap` tương tự với `m(pitchi)`.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Cài đặt theo `CSV--analyze-one-frame` (+ `CSV--detect-fname-type`). Dim thẳng = `Co70 ∈ {32,160}`, dim phi = `Co70 ∈ {3,35,163}`; chỉ xét `Lop == lopDim`. Text thông tin = text trên `KHUNG_IN` (qua `TachDong`), F-name = text trên `2-F-NAME`.
- [ ] **Step 4:** Run → PASS.
- [ ] **Step 5:** Commit `07_CSV: phan tich khung (fname, dim tong, kiem loi)`.

---

### Task 4: Dòng CSV, sắp xếp, ghi 2 file (`Core/CsvXuat.cs`, `Core/MaDon.cs`)

**Files:**
- Create: `07_CSV/plugin/CsvTool.Plugin/Core/CsvXuat.cs`, `Core/MaDon.cs`
- Test: `07_CSV/plugin/CsvTool.Tests/TestCsvXuat.cs`

**Interfaces:**
- Consumes: `TamXuat`.
- Produces:
```csharp
public static class MaDon { public static string TuTenFile(string dwgName); }
public enum KieuSapXep { TraiSangPhai = 1, TrenXuongDuoi = 2, ChuCai = 3 }   // = csvmode
public static class CsvXuat {
  public const string TieuDe10 = "エクスポート用,数量,材質,板厚,noと安全,曲げ,X,Y,,単価";
  public const string TieuDe35 = "...";                       // đúng chữ ở Global Constraints
  public static string O(string s);                           // = CSV--csv-field
  public static string MaMage(string loaiGiaCong);            // m/k/s/t/r/*/""
  public static string LoaiCuon(string loaiGiaCong);          // = CSV--coil-type: "1"/"2"/"0"
  public static string Dong35(TamXuat t);
  public static string Dong10(TamXuat t, string maDon);
  public static List<TamXuat> SapXep(IEnumerable<TamXuat> ds, KieuSapXep k); // ChuCai = compare-fname
  public static string TenTrung(IEnumerable<TamXuat> ds);     // N trùng đầu tiên, null nếu không
  public static void GhiHaiFile(string thuMucDwg, string maDon, IReadOnlyList<TamXuat> dsDaSapXep); // ném IOException, không ghi gì nếu 1 trong 2 file bị khoá
}
```

- [ ] **Step 1: Test hỏng:**
  - `MaDon`: `"129213-s.dxf"→"129213"`, `"100554-2-a.dwg"→"100554-2"`, `"1234-A2a.dwg"→"1234-A2a"`, `"131102.dwg"→"131102"`, `"X-S.DWG"→"X"`.
  - `MaMage`: `"m"→"m"`, `"m(pitchi)"→"m"`, `"s"→"s"`, `"r1"→"r"`, `"m, t"→"*"`, `"m, m(pitchi)"→"m"`, `"r1, r2"→"r"`, `""→""`.
  - `O`: `"m, t"→"\"m, t\""`, `"a\"b"→"\"a\"\"b\""`.
  - `Dong10`: tấm 131102-2 → `"131102-2,2,SUS304 No.1,3,131102-,s,1500,1000,,"`.
  - `Dong35`: ví dụ trong `xuat_CSV_V4/.claude/rules/05-csv-output.md` (`ABC-01,SPCC,1.6,300,200,2,4,1,2,0,0,0,"m, t",0,150,2,150,1,80,0,...,,,`) — đường bẻ ghi (Số đường, Dài), thiếu thì `0`; quá 8 loại thì bỏ phần thừa.
  - `SapXep`: L→R tăng theo `DiemX`; Top→Bottom giảm theo `DiemY`; ChuCai: `A-2` trước `A-10`.
  - `GhiHaiFile_Dung`: thư mục tạm → có `<đơn>.csv` + `data\<đơn>-data.csv`, 3 byte đầu `EF BB BF`, mọi dòng kết thúc `\r\n`.
  - `GhiHaiFile_KhoaFile_KhongGhiGi`: giữ `FileShare.None` trên `data\<đơn>-data.csv` cũ → ném `IOException`, `<đơn>.csv` cũ **không đổi** nội dung.
  - `GhiHaiFile_ThuMucRong`: `thuMucDwg==""` → ném `InvalidOperationException("Bản vẽ chưa lưu")`.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Cài đặt. `MaDon`: bỏ đuôi, rồi bỏ đúng 1 hậu tố `-a`/`-s` (không phân biệt hoa thường). `GhiHaiFile`: tạo `data\`; mở **cả hai** đích bằng `FileMode.OpenOrCreate, FileShare.None` trước, chỉ khi mở được cả hai mới `SetLength(0)` và ghi (`new UTF8Encoding(true)`). Sắp xếp ChuCai chép `compare-fname`.
- [ ] **Step 4:** Run → PASS.
- [ ] **Step 5:** Commit `07_CSV: ghi CSV 10 cot + data 35 cot`.

---

### Task 5: Cài đặt (`Core/CaiDatCsv.cs`)

**Files:**
- Create: `07_CSV/plugin/CsvTool.Plugin/Core/CaiDatCsv.cs`
- Test: `07_CSV/plugin/CsvTool.Tests/TestCaiDat.cs`

**Interfaces:**
- Produces:
```csharp
public sealed class CaiDatCsv {
  public KieuSapXep SapXep = KieuSapXep.TraiSangPhai; public bool DungLopMacDinh = true;
  public string LopKhung = "Khung_IN", LopDim = "1-XY", LopTapu = "TAPU_TMT";
  public double BuocSpline = 1.0, DungSaiKheHo; public int CheDoTinh;
  public static CaiDatCsv Doc(Func<string, object> docMoi, Func<string, object> docCu); // docMoi null hết → lấy từ docCu (khoá cũ: csvmode, use_default_layers, pop_1, pop_2, pop_3, spline_precision, gap_tolerance, calc_mode)
  public void Ghi(Action<string, object> ghiMoi);
}
```
- [ ] **Step 1: Test hỏng:** `Doc_ChuaCoKhoaMoi_LayKhoaCu` (`csvmode=2, pop_1="KHUNG"` → `SapXep==TrenXuongDuoi, LopKhung=="KHUNG"`); `Doc_CoKhoaMoi_BoQuaKhoaCu`; `Doc_GiaTriHong` (`csvmode="x"` → mặc định 1); `DungLopMacDinh` → 3 lớp trả về giá trị mặc định dù đã lưu khác.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Cài đặt (ép kiểu như `CSV--to-int / CSV--to-float`; giá trị mặc định của `gap_tolerance` lấy theo `CSV--dcl-settings`). Bọc registry thật ở `CadServices/CaiDatRegistry.cs` (`Software\OFT_Lisp\CSV`, cũ `Software\Autodesk`). **Step 4:** Run → PASS. **Step 5:** Commit.

---

### Task 6: Test vàng với 3 đơn mẫu (chỉ trong Tests)

**Files:**
- Create: `07_CSV/plugin/CsvTool.Tests/DxfThuGon.cs` — đọc DXF ASCII (cặp mã nhóm) thành `KhungTho` (LWPOLYLINE kín trên lớp khung → khung; TEXT/MTEXT/DIMENSION với lớp, nội dung (gộp mã 3 + 1), mã 70, 42, điểm chèn/hộp bao gần đúng; `$DIMZIN` từ HEADER; DIMDEC: override trong XDATA `ACAD` mã 1070 = 271, nếu không thì `$DIMDEC`).
- Create: `07_CSV/plugin/CsvTool.Tests/TestVang.cs`

**Interfaces:**
- Consumes: `PhanTichKhung.Chay`, `CsvXuat.SapXep/Dong35/TieuDe35`.

- [ ] **Step 1:** Test `Vang_<don>` cho `129213`, `125953`, `127143`: thư mục `%USERPROFILE%\OneDrive - OFT\xxxxx\<don>`; không có → in `BO QUA` và coi là PASS. Có → đọc `<don>-s.dxf`, phân tích mọi khung, sắp xếp theo đúng kiểu đã dùng khi xuất (thử L→R; nếu không khớp thì đọc lại `csvmode` mặc định 1), ghép `TieuDe35 + dòng` và so **từng byte** với `<don>.csv` (bỏ qua BOM/CRLF chỉ khi file mẫu không có — in rõ dòng khác đầu tiên khi FAIL).
- [ ] **Step 2:** Run → sửa `DxfThuGon` / Task 2-4 cho tới khi 3 đơn PASS. Chỗ lệch do **dữ liệu DXF** (không phải logic) thì ghi vào `07_CSV/rules/` kèm lý do, không nắn logic theo.
- [ ] **Step 3:** Commit `07_CSV: test vang 3 don mau (doc tu OneDrive, khong chep ban ve)`.

---

### Task 7: B1 tạm qua lisp cũ

**Files:**
- Create: `07_CSV/lisp/xuat_csv_zwcad.lsp`, `07_CSV/lisp/xuat_csv_autocad.lsp` (chép 2 bản v5.0.1; đổi mọi chú thích sang ASCII không dấu, bỏ BOM; **không** đổi mã)
- Create: `07_CSV/lisp/csvtool_b1.lsp`
- Create: `07_CSV/plugin/CsvTool.Plugin/CadServices/ChayB1Lisp.cs`

**Interfaces:**
- Produces lisp: `(csvtool-b1 frame-layer tapu-layer)` — đặt `*bp3-spline-precision*`, `*bp3-calc-mode*`, `*bp3-zoom-on-error* 0`, `*bp3-auto-compare* 0` từ registry cũ; chọn khung đúng như `c:csv` (`CSV--frame-filter` + `CSV--filter-valid-frames`, fallback mọi lớp); gọi `CSV--run-pricing-text-mode`. Có `*error*` khôi phục `CMDECHO`.
- Produces C#: `ChayB1Lisp.Chay(CaiDatCsv c)` — chép giá trị cài đặt sang các khoá cũ mà lisp đọc (`spline_precision`, `gap_tolerance`, `calc_mode`, `zoom_on_error=0`, `auto_compare=0`); `// shortcut: lisp cũ đọc registry Autodesk, bỏ ở giai đoạn 2`. Nạp lisp đúng bản theo `#if ZWCAD` (lần đầu trong phiên), rồi `SendStringToExecute("(csvtool-b1 \"<khung>\" \"<tapu>\")\n", ...)` như `GuroTabViewModel.cs:329-334`.

- [ ] **Step 1:** Chép + đổi chú thích. Run kiểm ASCII: `python -c "import sys;d=open(sys.argv[1],'rb').read();print(all(b<128 for b in d))" 07_CSV/lisp/xuat_csv_zwcad.lsp` → `True` (cả 3 file). Kiểm cân bằng ngoặc theo `rules/11` của workspace → khớp.
- [ ] **Step 2:** Viết wrapper + `ChayB1Lisp`. Build.
- [ ] **Step 3 (trên CAD — ZWCAD và AutoCAD):** mở bản sao `127143-s.dxf`, bấm B1 → MText thông tin tấm xuất hiện trên `KHUNG_IN` giống hệt khi chạy `CSV` lisp cũ; không có hộp thoại DCL, không mở `-ERROR.dwg`.
- [ ] **Step 4:** Commit `07_CSV: B1 tam goi lisp cu`.

---

### Task 8: Quét bản vẽ, ghi ngược, vòng đỏ tạm (`CadServices/`)

**Files:**
- Create: `07_CSV/plugin/CsvTool.Plugin/CadServices/QuetKhungCad.cs`, `GhiNguocCad.cs`, `VongDoTam.cs`, `ZoomCad.cs`

**Interfaces:**
- Consumes: `KhungTho/TextTho/DimTho`, `ThongTinTamParser.ThayGiaTri`, `CaiDatCsv`.
- Produces:
```csharp
public static class QuetKhungCad { public static (List<KhungTho> Khung, int Dimzin) Quet(Document doc, IEnumerable<ObjectId> khungDaChon, CaiDatCsv c); }
  // khungDaChon rỗng → mọi polyline kín hợp lệ trên lớp khung (theo CSV--valid-frame-p). Chọn trọn = GeometricExtents nằm trong hộp khung (dung sai 1e-6). Dim: DXF70 tính từ loại (RotatedDimension → 32, +128 nếu text đã dời; DiametricDimension → 3/35/163 cùng quy tắc), Dimdec = dim.Dimdec.
public static class GhiNguocCad {
  public static void SuaThongTin(Document doc, long handleKhung, string nhanKhongDau, string giaTri); // tìm MText thông tin trong khung, ThayGiaTri, ghi Contents
  public static void SuaFname(Document doc, long handleKhung, char truong, string giaTri);          // truong ∈ N,M,T,Q; thay dòng "X=..." trong F-name
}
public static class VongDoTam { public static void Hien(Document doc, double cx, double cy, double banKinh); public static void Xoa(); }
  // TransientManager.CurrentTransientManager.AddTransient(circle màu 1, DirectShortTerm, 128, new IntegerCollection()); Xoa gọi EraseTransient + Dispose. ZWCAD 2026 có TransientManager (đã kiểm ZwDatabaseMgd.dll).
public static class ZoomCad { public static void ToiKhung(Document doc, KhungTho k); } // zoom W hộp khung nới 10%
```
- [ ] **Step 1:** Cài đặt; mọi truy cập DB trong `using (doc.LockDocument())` + transaction.
- [ ] **Step 2 (trên CAD, cả 2 nền tảng):** `VongDoTam.Hien` hiện vòng đỏ không nằm trong bản vẽ (`SELECT` không chọn được, lưu file không có), `Xoa` làm mất; sửa `Lỗ Sara` qua `SuaThongTin` → MText đổi đúng 1 dòng; `Ctrl+Z` hoàn tác được.
- [ ] **Step 3:** Commit `07_CSV: quet khung, ghi nguoc MText/F-name, vong do tam`.

---

### Task 9: Giao diện tab CSV (`ViewModels/`, `Views/`)

**Files:**
- Create: `07_CSV/plugin/CsvTool.Plugin/ViewModels/CsvTabViewModel.cs`, `ViewModels/DongTam.cs`, `ViewModels/RelayCommand.cs` (chép từ Guro)
- Modify: `07_CSV/plugin/CsvTool.Plugin/Views/CsvTabControl.xaml(.cs)`, `CsvOhtaTab.cs`

**Interfaces:**
- Consumes: Task 2-5, 7, 8.
- Produces: `DongTam` (bọc `TamXuat` hoặc lỗi): cột `N, Q, M, T, XxY, LoNho, LoLon, Tappu, Sara, PitchiSoLan, PitchiDai, Vat, DuongBe` (chuỗi `200×1; 150×2`, chỉ đọc), `LoaiGiaCong, MaMage` (chỉ đọc), `Loi`. Ô sửa được: `N,Q,M,T` → `SuaFname`; `Tappu, Sara, PitchiSoLan, PitchiDai, Vat, LoaiGiaCong` → `SuaThongTin`; ghi lỗi thì trả ô về giá trị cũ + hiện thông báo.

- [ ] **Step 1:** Bố cục theo spec: thanh nút (`Xuất thông tin tấm`, `EXPORT`, `Làm mới`, radio Chế độ tính, ⚙ mở vùng cài đặt gập/mở), `DataGrid` + ô lọc "chỉ tấm lỗi", dòng trạng thái (2 đường dẫn + nút Mở).
- [ ] **Step 2:** Luồng: `Làm mới` = `Quet` (người dùng chọn khung trên CAD; không chọn → tất cả) → `PhanTichKhung.Chay` → `SapXep` → lưới. `EXPORT` = `Làm mới`; có dòng lỗi hoặc `TenTrung != null` → không ghi, lọc lỗi, báo số tấm lỗi; không lỗi → `GhiHaiFile` → mở `<đơn>.csv` bằng `Process.Start` (như `CSV--open-file`). Bắt `IOException` → "File CSV đang mở (Excel?) — đóng rồi bấm lại".
- [ ] **Step 3:** Bấm dòng → `ZoomCad.ToiKhung`; bấm ô lỗi → zoom + `VongDoTam.Hien` (tâm khung, bán kính = 0.55 × đường chéo). Vòng cũ xoá khi bấm chỗ khác, khi EXPORT, khi đổi bản vẽ.
- [ ] **Step 4 (trên CAD):** trên bản sao 3 đơn mẫu, `EXPORT` → `<đơn>.csv` và `data\<đơn>-data.csv`; file data **giống từng byte** `<đơn>.csv` lisp gốc; file 10 cột đúng tiêu đề và cột như spec. Mở `<đơn>.csv` trong Excel rồi EXPORT lại → báo lỗi, không file nào bị đổi.
- [ ] **Step 5:** Commit `07_CSV: giao dien tab CSV + danh sach tam`.

---

### Task 10: Tài liệu, phiên bản, phát hành admin

**Files:**
- Modify: `07_CSV/README.md`, `07_CSV/rules/00-muc-luc.md` (+ `01-cot-csv.md` chép quy tắc cột 10/35 và mã 曲げ), `GLOSSARY.md` (thêm "MText thông tin tấm", "dim tổng"), `VERSION` (tăng số giữa), `CHANGELOG.md` (mục mới đầu file: tab CSV — chỉ máy admin)

- [ ] **Step 1:** Viết tài liệu. Ghi rõ GĐ2 còn lại: hình học B1 (`CSV--analyze-boundary`, `CSV--bend-list-smart`, `CSV--pricing-info`, `CSV--count-vat`, `CSV--scan-machining-texts`) → C#, rồi xoá `07_CSV/lisp/` và đoạn chép registry cũ trong `ChayB1Lisp`.
- [ ] **Step 2:** Run `powershell -ExecutionPolicy Bypass -File build_ohta.ps1 -RequireBoth` → build cả 2 bản + mọi test PASS.
- [ ] **Step 3:** Commit `v<x.y>: tab CSV giai doan 1 (admin)`. Phát hành chỉ khi người dùng đồng ý.
