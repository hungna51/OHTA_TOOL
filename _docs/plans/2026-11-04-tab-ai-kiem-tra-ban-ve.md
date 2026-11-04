# Tab AI kiểm tra bản vẽ — Kế hoạch triển khai

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm tab **AI** (mục con `05_AI`, DLL riêng `AITOOL.dll`) gửi dữ liệu trong từng khung lên Claude API và hiện danh sách cảnh báo F-name / số lượng / ghi chú, bấm vào thì zoom tới khung.

**Architecture:** `AITOOL.dll` build kép (net48 ZWCAD, net10 AutoCAD) giống `GROTOOL.dll`; OHTATOOL.dll nạp nó bằng phản chiếu qua một bộ nạp DLL-tab dùng chung (tổng quát hoá từ `GuroTabLoader`). Logic thuần (gom đối tượng theo khung, đổi toạ độ, đóng chữ, dựng request JSON, đọc response, chia lô, tính tiền) nằm trong `Core/` và có bộ test console chạy ngoài CAD. Phần CAD chỉ quét bản vẽ thành mô hình thuần và zoom; phần mạng là `HttpClient` gọi thẳng `POST /v1/messages`.

**Tech Stack:** C# (LangVersion latest), WPF, ZWCAD 2026 .NET (net48) / AutoCAD 2027 .NET (net10.0-windows), `System.Net.Http.HttpClient`, `JsonLite` của OHTA (link nguồn), Claude API (`anthropic-version: 2026-10-21`).

**Spec:** [_docs/specs/2026-11-04-tab-ai-kiem-tra-ban-ve.md](../specs/2026-11-04-tab-ai-kiem-tra-ban-ve.md)

## Global Constraints

- Không phụ thuộc GRO: không gọi GROTOOL.dll, không đọc registry `OFT_Lisp\GRO`, không dùng bango.
- AI **chỉ cảnh báo**: không đoạn code nào trong `AITOOL.dll` ghi vào bản vẽ hay CSV.
- Toạ độ gửi đi tính theo góc dưới trái khung = (0,0), làm tròn 2 chữ số thập phân, định dạng `InvariantCulture` `"0.##"`.
- Đối tượng thuộc khung khi **tâm hộp bao** (`GeometricExtents`) nằm trong khung; khung chồng nhau thì đối tượng đi theo mọi khung chứa nó.
- Khung = polyline kín đúng 4 đỉnh trên layer chọn trên tab (mặc định `Khung_IN`).
- Lô ~10 khung (`KichThuocLo = 10`), tối đa 3 lô chạy song song.
- Model mặc định `claude-sonnet-5-5`; ô chọn có thêm `claude-opus-5-5`, `claude-haiku-5-5`.
- Key: `05_AI/data/ai.key`; lời dặn: `05_AI/data/loi_dan.md`; nhật ký: `05_AI/log/`. `ai.key` và `log/` vào `.gitignore`.
- Registry riêng: `HKCU\Software\OFT_Lisp\OhtaTool\AI` (giá trị `layer_khung`, `model`).
- `05_AI` không vào zip phát hành (`release_ohta.ps1`).
- Mọi truy cập CAD Database từ palette chạy qua lệnh CAD (`SendStringToExecute`) hoặc `using (doc.LockDocument())`.
- `princ`/`WriteMessage` ra dòng lệnh dùng tiếng Việt **không dấu**; chữ trên giao diện WPF có dấu.
- Hoàn tất mỗi task: `powershell -ExecutionPolicy Bypass -File build_ohta.ps1` xanh (OhtaTool.Tests, GroTool.Tests, AiTool.Tests).

## Review Focus

1. **Khung không có F-name hoặc có 2 cụm F-name** (khung chồng nhau) — vẫn gửi; không parse thành một bản ghi lẫn lộn. Test: `DongChu_KhungHaiCumN_KhongTachTruong` (Task 4).
2. **MText có mã định dạng / chữ Nhật `\U+XXXX`** — nội dung gửi đi phải là chữ sạch, đúng ký tự Nhật. Test: `DongChu_MTextMaDinhDang_LamSach` (Task 4).
3. **Không có key / key sai / mất mạng / 429** — báo rõ bằng tiếng Việt, không treo CAD, lô khác vẫn chạy. Test: `Runner_MotLoLoi_LoKhacVanCoKetQua`, `Runner_ThieuKey_BaoLoi` (Task 7).
4. **AI trả về khung không tồn tại hoặc JSON hỏng / `stop_reason` = `refusal` / `max_tokens`** — bỏ cảnh báo lạ, báo lô lỗi, không văng. Test: `DocPhanHoi_KhungLa_BoQua`, `DocPhanHoi_Refusal_BaoLoi` (Task 6).
5. **Polyline có bulge (cung)** — chiều quay âm/dương ra đúng tâm. Test: `Bulge_AmDuong_TamDung` (Task 3).

---

## File Structure

```
05_AI/
├── module.json                         Tab "AI" (id "ai", order 40)
├── README.md                           Mô tả tab + hướng dẫn tạo API key (Task 10)
├── data/loi_dan.md                     Lời dặn đang dùng (tự sinh từ bản mặc định nếu thiếu; có trong git)
├── data/ai.key                         Key (gitignore)
├── log/                                Nhật ký (gitignore)
├── bin/<ZwCAD|AutoCAD>/AITOOL.dll      Sinh bởi build_ohta.ps1
└── plugin/
    ├── AiTool.Plugin/
    │   ├── AiTool.Plugin.csproj
    │   ├── AiOhtaTab.cs                Cửa vào TaoGiaoDien (hợp đồng với OHTATOOL)
    │   ├── AiCommands.cs               Lệnh OHTAAI, OHTAAIVUNG, OHTAAIZOOM
    │   ├── Assets/loi_dan_mac_dinh.md  Lời dặn mặc định (EmbeddedResource)
    │   ├── Core/KhungModel.cs          LoaiDoiTuong, DoiTuong, DoanPl, KhungDuLieu
    │   ├── Core/HinhHoc.cs             Bulge→cung, làm tròn, tâm hộp bao
    │   ├── Core/GomKhung.cs            Gán đối tượng vào khung + đổi toạ độ
    │   ├── Core/DongChu.cs             Đóng dữ liệu khung thành chữ gửi AI
    │   ├── Core/DuLieuTinh.cs          Khối bảng vật liệu, đọc/ghi lời dặn, đọc key
    │   ├── Core/YeuCauApi.cs           Dựng body JSON request
    │   ├── Core/PhanHoiApi.cs          Đọc response → CanhBao, SuDung, KetQuaLo
    │   ├── Core/BangGia.cs             Giá theo model → tiền ước tính
    │   ├── Core/KiemTraRunner.cs       Chia lô, chạy song song qua IGuiYeuCau, gom kết quả
    │   ├── Services/ClaudeHttp.cs      IGuiYeuCau thật (HttpClient, retry)
    │   ├── Services/NhatKy.cs          Ghi log JSON
    │   ├── CadServices/QuetKhung.cs    CAD → List<KhungDuLieu>
    │   ├── CadServices/ZoomKhung.cs    Zoom (chép cách làm của GROTOOL CadViewService)
    │   ├── ViewModels/AiTabViewModel.cs
    │   └── Views/AiTabControl.xaml(.cs)
    └── AiTool.Tests/
        ├── AiTool.Tests.csproj          Console net10.0, link Core/*.cs
        └── Program.cs
```

Sửa: `_system/OhtaTool.Plugin/Views/GuroTabLoader.cs` → `DllTabLoader.cs`, `_system/OhtaTool.Plugin/Views/ModuleViewRegistry.cs`, `build_ohta.ps1`, `release_ohta.ps1`, `.gitignore`, `GEMINI.md`; tạo `_docs/adr/0006-tab-ai-dll-rieng.md`.

**Dùng lại mã Fname bằng link nguồn** (không tham chiếu OHTATOOL.dll): `AiTool.Plugin.csproj` và `AiTool.Tests.csproj` thêm `<Compile Include="..\..\..\_system\OhtaTool.Plugin\Core\{JsonLite,FnameCore,FnameDataStore,FnameLocalAdditions}.cs" Link="Shared\..." />`. Nếu các file này cần thêm file `Core/` thuần khác để biên dịch thì link thêm file đó. Kiểu nằm trong namespace `OhtaTool.Plugin.Core` nhưng thuộc assembly AITOOL, nên không xung đột.

---

### Task 1: Khung mục con 05_AI, dự án AITOOL, build và loại khỏi bản phát hành

**Files:**
- Create: `05_AI/module.json`, `05_AI/plugin/AiTool.Plugin/AiTool.Plugin.csproj`, `05_AI/plugin/AiTool.Plugin/AiOhtaTab.cs`, `05_AI/plugin/AiTool.Tests/AiTool.Tests.csproj`, `05_AI/plugin/AiTool.Tests/Program.cs`, `_docs/adr/0006-tab-ai-dll-rieng.md`
- Modify: `build_ohta.ps1`, `release_ohta.ps1:64-71`, `.gitignore`, `GEMINI.md` (bảng mục 4)

**Interfaces:**
- Produces: `public static System.Windows.FrameworkElement AiTool.AiOhtaTab.TaoGiaoDien(string tieuDe, string moTa, string mauNhan, string thuMucModule)` (tạm trả `TextBlock` "Tab AI đang dựng"); test runner có `Kiem(string ten, object duoc, object mongDoi)` và exit code 1 khi có ca FAIL, giống `03_Guro/plugin/GroTool.Tests/Program.cs`.

- [ ] **Step 1:** Tạo `module.json`: `id` `"ai"`, `enabled` true, `order` 40, `tabTitle` `"AI"`, `title` `"AI KIỂM TRA"`, `version` `"0.1"`, `description` `"AI đọc dữ liệu trong khung, cảnh báo F-name / số lượng / ghi chú."`, `accentColor` `"#5B3FD1"`, `tools` `[]`.
- [ ] **Step 2:** Tạo `AiTool.Plugin.csproj` bằng cách chép `03_Guro/plugin/GroTool.Plugin/GroTool.Plugin.csproj`, đổi `RootNamespace` → `AiTool`, `AssemblyName` → `AITOOL`, target copy → `CopyOutputToAiBin` (ra `..\..\bin\<sub>`). Bỏ `EmbeddedResource` settings_logo, thêm `<EmbeddedResource Include="Assets\loi_dan_mac_dinh.md" LogicalName="AiTool.Assets.loi_dan_mac_dinh.md" />` (để file rỗng ở task này). Thêm các dòng link nguồn Fname như ở mục File Structure. Với nhánh `net48`, thêm `<Reference Include="System.Net.Http" />`.
- [ ] **Step 3:** Tạo `AiTool.Tests.csproj` theo mẫu `GroTool.Tests.csproj`: `net10.0`, `DefineConstants` không cần ZWCAD, `<Compile Include="..\AiTool.Plugin\Core\*.cs" Link="Core\%(Filename)%(Extension)" />` cộng 4 file Fname đã link. `Program.cs` có một ca `Kiem("khung test chay", 1, 1)`.
- [ ] **Step 4:** Sửa `build_ohta.ps1`: thêm `$AiProj` và `$AiTestProj`, **chỉ build khi `Test-Path`**. Build ngay sau GroTool. Thêm `Publish-CadDll ... "05_AI\bin\<ZwCAD|AutoCAD>" ... "AITOOL"`, rồi chạy `dotnet run --project $AiTestProj` trong bước [2/3].
- [ ] **Step 5:** Sửa `release_ohta.ps1`: trong bộ lọc `$Files`, thêm `if ($top -eq '05_AI') { return $false }` trước dòng `return -not (...)`. Thêm vào `.gitignore` hai dòng `05_AI/data/ai.key` và `05_AI/log/`.
- [ ] **Step 6:** Viết `_docs/adr/0006-tab-ai-dll-rieng.md` theo giọng ADR 0003. Các điểm cần ghi:
  - AITOOL là DLL riêng, nạp bằng phản chiếu, cùng hợp đồng 4 chuỗi.
  - Gọi API bằng `HttpClient` thẳng thay vì SDK `Anthropic`. Lý do: plugin CAD là một DLL đơn, chạy net48, `CopyLocalLockFileAssemblies=false`, kéo thêm gói NuGet vào AppDomain của CAD dễ xung đột phiên bản; OHTA đã có tiền lệ `GitHubReleaseClient`.
  - Mã Fname dùng lại bằng link nguồn.
  - Tab không có trong bản phát hành cho tới khi gỡ dòng loại trong `release_ohta.ps1`.
- [ ] **Step 7:** Thêm 1 dòng `04` vào bảng mục 4 của `GEMINI.md` (Tab **AI**, trạng thái "Thử nghiệm — chỉ máy phát triển").
- [ ] **Step 8:** Run `powershell -ExecutionPolicy Bypass -File build_ohta.ps1` — Expected: `[OK] Da xuat DLL ... AITOOL`, dòng `OK   khung test chay`, kết thúc `=== HOAN TAT`.
- [ ] **Step 9:** Run `powershell -ExecutionPolicy Bypass -File release_ohta.ps1 -DryRun` rồi liệt kê zip: không có đường dẫn nào chứa `05_AI/`.
- [ ] **Step 10:** Commit `"Tab AI: khung muc con 05_AI + AITOOL.dll (ADR 0006)"`.

### Task 2: OHTATOOL nạp tab AI qua bộ nạp DLL-tab dùng chung

**Files:**
- Modify (đổi tên): `_system/OhtaTool.Plugin/Views/GuroTabLoader.cs` → `DllTabLoader.cs`
- Modify: `_system/OhtaTool.Plugin/Views/ModuleViewRegistry.cs:41-47`

**Interfaces:**
- Consumes: `AiTool.AiOhtaTab.TaoGiaoDien` (Task 1).
- Produces: `internal static FrameworkElement DllTabLoader.TaoGiaoDien(ModuleManifest manifest, string tenDll, string tenLop, string tenTab)`. Hàm trả `null` khi không nạp được, giữ nguyên hành vi của GuroTabLoader: tìm assembly đã nạp theo tên, nếu chưa có thì `CadExtensionLoader.Load`, bọc kết quả trong `CuonHo`.

- [ ] **Step 1:** Đổi `GuroTabLoader` thành `DllTabLoader` có tham số. Các thông báo `WriteMessage` thay "tab Guro" bằng `"tab " + tenTab`, còn "GROTOOL.dll" thay bằng `tenDll + ".dll"`.
- [ ] **Step 2:** Trong `ModuleViewRegistry`, factory Guro gọi `DllTabLoader.TaoGiaoDien(manifest, "GROTOOL", "ZwCadPlugin.GuroOhtaTab", "Guro")`. Thêm factory AI gọi `DllTabLoader.TaoGiaoDien(manifest, "AITOOL", "AiTool.AiOhtaTab", "AI")`, nếu nạp không được thì dùng `ModuleTabControl`. Đăng ký với cả hai khoá `"ai"` và `"05_AI"`.
- [ ] **Step 3:** Chạy `build_ohta.ps1`. Kết quả mong đợi: xanh.
- [ ] **Step 4:** Kiểm thử tay trong ZWCAD: `OHTARELOAD`, tab Guro vẫn hiện giao diện cũ, tab AI hiện "Tab AI đang dựng".
- [ ] **Step 5:** Commit `"OHTA: DllTabLoader dung chung cho tab Guro va tab AI"`.

### Task 3: Mô hình dữ liệu khung, hình học và gom khung

**Files:**
- Create: `05_AI/plugin/AiTool.Plugin/Core/KhungModel.cs`, `Core/HinhHoc.cs`, `Core/GomKhung.cs`
- Test: `05_AI/plugin/AiTool.Tests/Program.cs`

**Interfaces:**
- Produces (namespace `AiTool.Core`, mọi toạ độ là `double`):
  - `enum LoaiDoiTuong { Line, Arc, Circle, Polyline, Text, Dim }`
  - `sealed class DoanPl { double X1, Y1, X2, Y2; bool LaCung; double Cx, Cy, R; }`
  - `sealed class DoiTuong { LoaiDoiTuong Loai; string Layer; double MinX, MinY, MaxX, MaxY; double X1, Y1, X2, Y2; double Cx, Cy, R, GocDau, GocCuoi; bool Kin; List<DoanPl> Doan; string NoiDung; double ChieuCao; string LoaiDim; double GiaTriDo; string GhiDe; }` (góc theo **độ**, field nào không áp dụng thì để mặc định)
  - `sealed class KhungDuLieu { int SoThuTu; double MinX, MinY, MaxX, MaxY; List<DoiTuong> DoiTuong; }`
  - `static class HinhHoc { DoanPl TuBulge(double x1, double y1, double x2, double y2, double bulge); string Tron(double v); }`. `Tron` dùng `Math.Round(v, 2, MidpointRounding.AwayFromZero).ToString("0.##", InvariantCulture)`, và `-0` phải ra `"0"`.
  - `static class GomKhung { List<KhungDuLieu> Gom(IList<(double MinX,double MinY,double MaxX,double MaxY)> khung, IList<DoiTuong> doiTuong); }`. Khung được đánh `SoThuTu` theo thứ tự: trên xuống (MaxY giảm), rồi trái sang phải (MinX tăng), bắt đầu từ 1. Đối tượng được **sao chép** và trừ (MinX, MinY) của khung ở mọi toạ độ (kể cả `Doan` và hộp bao); riêng `KhungDuLieu.MinX..MaxY` giữ **toạ độ gốc** bản vẽ (để zoom). Tâm hộp bao nằm **trên cạnh** khung vẫn tính là ở trong khung.

- [ ] **Step 1:** Viết các test FAIL:
  - `Tron(1.005)=="1.01"`, `Tron(2.0)=="2"`, `Tron(-0.001)=="0"`
  - `Bulge_AmDuong_TamDung`: `TuBulge(0,0,10,0,1)` cho `LaCung`, tâm (5,0), R=5; `TuBulge(0,0,10,0,-1)` cũng tâm (5,0), R=5; `TuBulge(0,0,10,0,0.414213562)` (¼ vòng) cho tâm (5,-5), R≈7.07; bulge 0 thì `LaCung==false`
  - `Gom_DoiToaDoTheoGocKhung`: khung (100,200)-(500,400), line (110,210)-(150,210), sau gom X1=10, Y1=10
  - `Gom_NgoaiKhung_BoQua`
  - `Gom_KhungChongNhau_DoiTuongVaoCaHai`
  - `Gom_ThuTuKhung_TrenXuongTraiSang`
- [ ] **Step 2:** Run `dotnet run --project 05_AI/plugin/AiTool.Tests`. Kết quả mong đợi: lỗi biên dịch hoặc FAIL.
- [ ] **Step 3:** Viết `KhungModel.cs`, `HinhHoc.cs`, `GomKhung.cs` theo Interfaces.
- [ ] **Step 4:** Chạy lại lệnh ở Step 2. Kết quả mong đợi: toàn bộ OK.
- [ ] **Step 5:** Commit `"Tab AI: mo hinh khung, bulge, gom khung"`.

### Task 4: Đóng dữ liệu khung thành chữ

**Files:**
- Create: `05_AI/plugin/AiTool.Plugin/Core/DongChu.cs`
- Test: `05_AI/plugin/AiTool.Tests/Program.cs`

**Interfaces:**
- Consumes: `KhungDuLieu`, `DoiTuong`, `HinhHoc.Tron` (Task 3); `FnameParser.SplitAndCleanMText`, `FnameParser.Parse` (link nguồn).
- Produces:
  - `static string DongChu.Khung(KhungDuLieu k)`: trả chữ của một khung.
  - `static string DongChu.Lo(IList<KhungDuLieu> lo)`: các khung nối nhau, ngăn cách bằng một dòng trống.
  - `static string DongChu.TenKhung(KhungDuLieu k)`: trả `N=` nếu khung có **đúng một** dòng `N=`, ngược lại trả `"Khung " + SoThuTu`. Giao diện dùng hàm này để đặt tên cảnh báo.

Định dạng (mỗi đối tượng 1 dòng, số qua `Tron`, chữ trong `"..."` với `"` thoát thành `\"`):
```
#KHUNG <SoThuTu> rong=<W> cao=<H>
FNAME: <các dòng F-name sạch, nối bằng " | ", sắp theo Y giảm rồi X tăng>
FNAME_TACH: N=<..>; M=<..>; T=<..>; Q=<..>; Z=<..>; V=<dòng bẻ nối ;>   (chỉ khi đúng một dòng N=)
L <layer> <x1>,<y1> <x2>,<y2>
C <layer> <cx>,<cy> d=<2R>
A <layer> <cx>,<cy> r=<R> <gocDau>-><gocCuoi>
P <layer> kin|ho <x,y> [~<cx>,<cy> r=<R>] <x,y> ...      (đoạn cung có ~ trước đỉnh cuối)
T <layer> <x>,<y> h=<ChieuCao> "<nội dung sạch, các dòng nối bằng " / ">"
D <layer> <LoaiDim> do=<GiaTriDo> [ghide="<GhiDe>"] <x1>,<y1> <x2>,<y2>
```
Text/MText trên layer `2-F-NAME` chỉ vào dòng `FNAME`, không lặp lại thành dòng `T`. Khung không có F-name thì ghi `FNAME: (khong co)`.

- [ ] **Step 1:** Viết các test FAIL:
  - `DongChu_Line_DinhDang`: ra đúng `L 0-KAKOU 10,10 50,10`
  - `DongChu_Circle_GhiDuongKinh`: R=2.25 ra `d=4.5`
  - `DongChu_MTextMaDinhDang_LamSach`: `{\fMS Gothic;\U+677F\U+539A}\Pt=2.3` ra `"板厚 / t=2.3"`
  - `DongChu_FnameTachTruong`: các dòng `N=A-1`, `M=SUS304 2B`, `T=1.5`, `Q=2`, `Z=SUS-N2` ra dòng `FNAME_TACH: N=A-1; M=SUS304 2B; T=1.5; Q=2; Z=SUS-N2; V=`
  - `DongChu_KhungHaiCumN_KhongTachTruong`: không có dòng `FNAME_TACH`; `TenKhung` trả `"Khung 1"`
  - `DongChu_KhongFname`
  - `DongChu_NhayKepTrongChu_DuocThoat`
- [ ] **Step 2:** Chạy test. Kết quả mong đợi: FAIL.
- [ ] **Step 3:** Viết `DongChu.cs`.
- [ ] **Step 4:** Chạy test. Kết quả mong đợi: OK.
- [ ] **Step 5:** Commit `"Tab AI: dong du lieu khung thanh chu"`.

### Task 5: Dữ liệu cố định — bảng vật liệu, lời dặn, key

**Files:**
- Create: `05_AI/plugin/AiTool.Plugin/Core/DuLieuTinh.cs`
- Test: `05_AI/plugin/AiTool.Tests/Program.cs` (dùng thư mục tạm `Path.GetTempPath()/aitool_test_<guid>`, xoá khi xong)

**Interfaces:**
- Consumes: `FnameDataStore.ReadLines`, `FnameDataStore.DefaultCacheDirectory`, `FnameLocalAdditions.Load` (link nguồn).
- Produces:
  - `static string DuLieuTinh.KhoiVatLieu(string[] dongSheet, IList<FnameLocalAdditions.Entry> choDuyet)`: khối gồm tiêu đề `## BANG VAT LIEU CHUAN (M,Loai,Z,T_list)` cộng các dòng sheet giữ nguyên thứ tự. Khi `choDuyet` có phần tử thì thêm `## VAT LIEU MAY TU THEM - CHUA DUYET (chi bao vang neu M chi co o day)` và mỗi entry một dòng `M,Loai,Z,T1 T2`. Hàm thuần, kết quả ổn định từng byte để cache trúng.
  - `static string DuLieuTinh.ThuMucFname(string thuMucModuleAi)`: trả `Path.Combine(Directory.GetParent(thuMucModuleAi).FullName, "01_Fname")`.
  - `static string DuLieuTinh.DocLoiDan(string thuMucModule, string macDinh)`: đọc `data/loi_dan.md`; nếu chưa có file thì ghi `macDinh` vào rồi trả về.
  - `static void DuLieuTinh.GhiLoiDan(string thuMucModule, string noiDung)`
  - `static string DuLieuTinh.DocKey(string thuMucModule)`: đọc `data/ai.key`, trả dòng đầu tiên không rỗng và không bắt đầu bằng `#`, đã `Trim`. Không có file thì trả `null`.

- [ ] **Step 1:** Viết các test FAIL:
  - `KhoiVatLieu_KhongChoDuyet_KhongCoMucChuaDuyet`
  - `KhoiVatLieu_CoChoDuyet_DanhDau`
  - `KhoiVatLieu_HaiLanGoi_GiongHetByte`
  - `LoiDan_ChuaCoFile_GhiMacDinh`
  - `LoiDan_GhiRoiDoc`
  - `Key_BoDongChuThich`: file `# key\n  sk-ant-x  \n` ra `"sk-ant-x"`
  - `Key_KhongFile_Null`
- [ ] **Step 2:** Chạy test. Kết quả mong đợi: FAIL. **Step 3:** Viết code. **Step 4:** Chạy test. Kết quả mong đợi: OK.
- [ ] **Step 5:** Commit `"Tab AI: bang vat lieu, loi dan, key"`.

### Task 6: Dựng request, đọc response, tính tiền

**Files:**
- Create: `05_AI/plugin/AiTool.Plugin/Core/YeuCauApi.cs`, `Core/PhanHoiApi.cs`, `Core/BangGia.cs`
- Test: `05_AI/plugin/AiTool.Tests/Program.cs`

**Interfaces:**
- Consumes: `JsonLite.Parse`/`Serialize`, `JsonObject` (link nguồn).
- Produces:
  - `static string YeuCauApi.Body(string model, string loiDan, string khoiVatLieu, string duLieuLo)`: trả JSON gồm:
    ```json
    {"model": <model>, "max_tokens": 16000, "fallbacks": "default",
     "output_config": {"effort": "medium", "format": {"type": "json_schema", "schema": <SCHEMA>}},
     "system": [{"type":"text","text": <loiDan>},
                {"type":"text","text": <khoiVatLieu>, "cache_control": {"type":"ephemeral"}}],
     "messages": [{"role":"user","content": <duLieuLo>}]}
    ```
    `<SCHEMA>` = object `{canh_bao: array of {khung: integer, muc_do: enum ["do","vang"], loai_loi: enum ["MTZ","Q","GHI_CHU","KHAC"], ly_do: string, goi_y: string}}`. Mọi object có `additionalProperties: false` và `required` liệt kê hết các khoá.
  - Hằng `YeuCauApi.Url = "https://api.anthropic.com/v1/messages"`. Header (do Task 7 gửi): `x-api-key`, `anthropic-version: 2026-10-21`, `anthropic-beta: server-side-fallback-2026-07-01`, `content-type: application/json`.
  - `sealed class CanhBao { int Khung; bool Do; string LoaiLoi, LyDo, GoiY; }`
  - `sealed class SuDung { long Vao, GhiCache, DocCache, Ra; }`, đọc từ `usage.input_tokens`, `cache_creation_input_tokens`, `cache_read_input_tokens`, `output_tokens`.
  - `sealed class KetQuaLo { List<CanhBao> CanhBao; SuDung SuDung; string Loi; }`
  - `static KetQuaLo PhanHoiApi.Doc(string json, ISet<int> khungHopLe)`. Lấy khối `content` đầu tiên có `type=="text"`, parse JSON trong đó. Cảnh báo có `khung` không thuộc `khungHopLe` thì bỏ. Các trường hợp `Loi` khác null:
    - `stop_reason=="refusal"`: `Loi = "AI tu choi lo nay"`
    - `stop_reason=="max_tokens"`: `Loi = "Phan hoi bi cat (lo qua dai)"`
    - JSON hỏng: `Loi = "Phan hoi khong doc duoc"`
    - Body có `error.message`: `Loi` là message đó
  - `static decimal BangGia.Tien(string model, SuDung s)`. Bảng giá theo USD/1M token (vào, ra, đọc cache); ghi cache tính bằng 1.25 × giá vào.

    | model | vào | ra | đọc cache |
    |---|---|---|---|
    | `claude-sonnet-5-5` | 2 | 10 | 0.20 |
    | `claude-opus-5-5` | 4 | 20 | 0.20 |
    | `claude-haiku-5-5` | 0.10 | 0.50 | 0.01 |

    Model lạ thì trả 0.

- [ ] **Step 1:** Viết các test FAIL:
  - `Body_CoCacheControlOKhoiVatLieu`: parse lại body, `system[1].cache_control.type=="ephemeral"` và `system[0]` không có `cache_control`
  - `Body_SchemaKhongChoThuocTinhLa`
  - `DocPhanHoi_HaiCanhBao`
  - `DocPhanHoi_KhungLa_BoQua`
  - `DocPhanHoi_Refusal_BaoLoi`
  - `DocPhanHoi_MaxTokens_BaoLoi`
  - `DocPhanHoi_LoiApi_LayMessage`
  - `Tien_Sonnet`: vào 1.000.000, ra 0, đọc cache 0 ra 2.00
- [ ] **Step 2:** Chạy test. Kết quả mong đợi: FAIL. **Step 3:** Viết code. **Step 4:** Chạy test. Kết quả mong đợi: OK.
- [ ] **Step 5:** Commit `"Tab AI: request/response Claude API + bang gia"`.

### Task 7: Chạy kiểm tra theo lô, client HTTP, nhật ký

**Files:**
- Create: `05_AI/plugin/AiTool.Plugin/Core/KiemTraRunner.cs`, `Services/ClaudeHttp.cs`, `Services/NhatKy.cs`
- Test: `05_AI/plugin/AiTool.Tests/Program.cs` (dùng `IGuiYeuCau` giả)

**Interfaces:**
- Consumes: Task 4–6.
- Produces:
  - `interface IGuiYeuCau { Task<string> Gui(string body, CancellationToken ct); }`: trả body response, kể cả khi là JSON lỗi. Hàm chỉ ném lỗi khi mất mạng hoặc hết giờ.
  - `sealed class KetQuaKiemTra { List<(KhungDuLieu Khung, CanhBao CB)> CanhBao; List<string> LoiLo; SuDung TongSuDung; decimal TongTien; List<(string Body, string PhanHoi)> NhatKy; }`
  - `static async Task<KetQuaKiemTra> KiemTraRunner.Chay(IList<KhungDuLieu> khung, string model, string loiDan, string khoiVatLieu, IGuiYeuCau gui, IProgress<(int xong,int tong)> tienDo, CancellationToken ct)`. Hàm chia lô theo `KichThuocLo = 10`, chạy tối đa `SongSong = 3` lô cùng lúc bằng `SemaphoreSlim`. Lô nào ném lỗi thì ghi `"Lo i: <msg>"` vào `LoiLo` và các lô khác vẫn chạy. Kết quả trả về sắp theo `Khung.SoThuTu`, cảnh báo đỏ đứng trước.
  - `sealed class ClaudeHttp : IGuiYeuCau` với hàm dựng `ClaudeHttp(string apiKey)`:
    - Dùng **một** `HttpClient` static, timeout 180 s, gửi các header ở Task 6.
    - Gặp 429 / 5xx / 529 thì thử lại tối đa 2 lần, chờ theo `retry-after` nếu có, không có thì 2 s rồi 4 s.
    - Trên net48 đặt `ServicePointManager.SecurityProtocol |= Tls12`.
  - `static string NhatKy.Ghi(string thuMucModule, string model, KetQuaKiemTra kq)`: ghi `log/yyyyMMdd_HHmmss.json` với các trường thời điểm, model, tổng token, tiền, các cặp body/phản hồi. **Không** ghi key. Hàm trả đường dẫn file.
  - Runner kiểm tra key **trước khi** tạo `ClaudeHttp`, qua hàm `static string KiemTraRunner.LoiTruocKhiChay(string key, int soKhung)`:
    - key `null` hoặc rỗng: `"Chua co API key: tao file 05_AI\\data\\ai.key (xem README)"`
    - `soKhung == 0`: `"Khong thay khung tren layer da chon"`
    - hợp lệ: `null`

- [ ] **Step 1:** Viết các test FAIL (với `IGuiYeuCau` giả trả JSON dựng sẵn):
  - `Runner_23Khung_3Lo`: đếm 3 lần gọi
  - `Runner_MotLoLoi_LoKhacVanCoKetQua`: giả ném `HttpRequestException` ở lần gọi thứ 2; `LoiLo.Count==1` và vẫn có cảnh báo của lô 1, 3
  - `Runner_CongTokenVaTien`
  - `Runner_SapDoTruocVang`
  - `Runner_ThieuKey_BaoLoi`
  - `NhatKy_KhongChuaKey`: ghi xong, đọc file, không thấy chuỗi key giả
- [ ] **Step 2:** Chạy test. Kết quả mong đợi: FAIL. **Step 3:** Viết `KiemTraRunner`, `NhatKy`, `ClaudeHttp`. **Step 4:** Chạy test. Kết quả mong đợi: OK.
- [ ] **Step 5:** Commit `"Tab AI: chay theo lo, client HTTP, nhat ky"`.

### Task 8: Quét bản vẽ và zoom

**Files:**
- Create: `05_AI/plugin/AiTool.Plugin/CadServices/QuetKhung.cs`, `CadServices/ZoomKhung.cs`

**Interfaces:**
- Consumes: `DoiTuong`, `GomKhung.Gom`, `HinhHoc.TuBulge` (Task 3).
- Produces:
  - `static List<KhungDuLieu> QuetKhung.Quet(Document doc, string layerKhung, Extents3d? vung)`:
    - Đọc ModelSpace trong một transaction chỉ đọc.
    - Khung là `Polyline` có `Closed`, `NumberOfVertices==4`, layer trùng `layerKhung` (không phân biệt hoa thường). Khi có `vung` thì chỉ lấy khung có hộp bao nằm trọn trong vùng.
    - Mọi polyline khung bị loại khỏi danh sách đối tượng.
    - Đổi entity sang `DoiTuong`:
      - `Line`
      - `Arc`: góc chuyển sang độ
      - `Circle`
      - `Polyline`: mỗi đoạn qua `TuBulge`
      - `DBText`: lấy `TextString`, `Height`, `Position`
      - `MText`: lấy `Contents` thô (`DongChu` sẽ làm sạch), `TextHeight`, `Location`
      - `Dimension`: `LoaiDim` = tên lớp (`RotatedDimension`…), `GiaTriDo` = `Measurement`, `GhiDe` = `DimensionText` khi khác rỗng và khác `"<>"`. Hai điểm đo lấy `XLine1Point` / `XLine2Point` nếu kiểu dim có, còn lại lấy hai góc hộp bao.
    - Entity khác và entity không có `GeometricExtents` thì bỏ qua.
    - Cuối cùng trả `GomKhung.Gom(...)`.
  - `static void ZoomKhung.Toi(Document doc, double minX, double minY, double maxX, double maxY)`: chép cách làm của `03_Guro/plugin/GroTool.Plugin/CadServices/CadViewService.cs` (`SetCurrentView`, đọc lại khung nhìn, nếu không đổi thì dùng lệnh `'_.ZOOM _W` làm dự phòng). Nới mỗi phía 5%.
  - Khung trả về giữ hộp bao **toạ độ gốc** của bản vẽ (`MinX..MaxY` của `KhungDuLieu` là toạ độ thật; chỉ đối tượng bên trong bị đổi gốc), để zoom dùng được.

- [ ] **Step 1:** Viết `QuetKhung`, `ZoomKhung` (không có unit test, vì phần thuần đã được test ở Task 3).
- [ ] **Step 2:** Chạy `build_ohta.ps1`. Kết quả mong đợi: xanh cho cả hai nền tảng.
- [ ] **Step 3:** Commit `"Tab AI: quet khung tu ban ve, zoom"`.

### Task 9: Giao diện tab AI và lệnh CAD

**Files:**
- Create: `05_AI/plugin/AiTool.Plugin/ViewModels/AiTabViewModel.cs`, `Views/AiTabControl.xaml`, `Views/AiTabControl.xaml.cs`, `05_AI/plugin/AiTool.Plugin/AiCommands.cs`
- Modify: `05_AI/plugin/AiTool.Plugin/AiOhtaTab.cs` (trả `new AiTabControl(new AiTabViewModel(...))`)

**Interfaces:**
- Consumes: Task 5–8.
- Produces: `public static AiTabViewModel AiTabViewModel.HienTai` và `public void BatDauKiemTra(List<KhungDuLieu> khung)`. Hàm này gọi `KiemTraRunner.LoiTruocKhiChay`, chạy `KiemTraRunner.Chay` bằng `async` (không chặn luồng CAD), cập nhật UI qua `Dispatcher`, xong thì ghi `NhatKy.Ghi`. `AiTabControl` có `public void ScrollByMouseWheel(int delta)` để `CuonHo` gọi.

Bố cục (từ trên xuống, theo phong cách nền trắng của tab Guro/Fname):
- Hàng chọn: **Layer khung** (ComboBox, danh sách layer của bản vẽ đang mở, mặc định `Khung_IN`, nhớ trong registry `layer_khung`) và **Model** (3 mục, nhớ `model`).
- Nút **▶ Kiểm tra** (gửi `OHTAAI `), **⬚ Chọn vùng** (gửi `OHTAAIVUNG `), **■ Dừng** (huỷ `CancellationToken`).
- Thanh tiến độ "Lô x/y", dòng tổng "N cảnh báo · token vào/ra/cache · ≈ $0.0000" và nút mở file nhật ký.
- Ô **Chỉ hiện 🔴**.
- Danh sách cảnh báo: mỗi dòng `🔴/🟡 <DongChu.TenKhung> · <LoaiLoi>` cộng lý do, cộng "Gợi ý: …". Bấm một dòng thì gửi `OHTAAIZOOM <SoThuTu> `.
- Dòng lỗi lô và lỗi trước khi chạy hiện màu đỏ phía trên danh sách.
- Expander **Lời dặn cho AI**: TextBox nhiều dòng, nút **Lưu** (`GhiLoiDan`) và **Khôi phục mặc định** (hỏi xác nhận, ghi lại nội dung từ embedded resource).

Lệnh gửi qua `doc.SendStringToExecute(..., true, false, false)`.

Lệnh trong `AiCommands.cs` (`[CommandMethod]`, không dùng `CommandFlags.Session`). Layer khung lấy từ ViewModel:
- `OHTAAI`: gọi `QuetKhung.Quet(doc, layer, null)` rồi `AiTabViewModel.HienTai?.BatDauKiemTra(...)`.
- `OHTAAIVUNG`: lấy vùng bằng `GetPoint` rồi `GetCorner`, sau đó quét như `OHTAAI`.
- `OHTAAIZOOM <so thu tu>`: zoom tới khung trong lần quét gần nhất, ViewModel giữ danh sách khung.
- Không có tab thì in `[AI] Mo tab AI tren bang OHTA truoc`.

- [ ] **Step 1:** Viết ViewModel, View và `AiCommands`.
- [ ] **Step 2:** Chạy `build_ohta.ps1`. Kết quả mong đợi: xanh.
- [ ] **Step 3:** Kiểm thử tay trong ZWCAD, với bản vẽ mẫu có 2 khung `Khung_IN` và **chưa** có `ai.key`. Bấm Kiểm tra, thấy dòng đỏ "Chưa có API key…" và CAD không treo. Tạo một khung trên layer khác rồi đổi ComboBox, thấy số khung thay đổi theo.
- [ ] **Step 4:** Commit `"Tab AI: giao dien tab"`.

### Task 10: Lời dặn mặc định, README hướng dẫn key, chạy thử thật

**Files:**
- Modify: `05_AI/plugin/AiTool.Plugin/Assets/loi_dan_mac_dinh.md`
- Create: `05_AI/README.md`, `05_AI/data/loi_dan.md` (chép từ bản mặc định)

- [ ] **Step 1:** Viết lời dặn mặc định bằng tiếng Việt, gồm các phần:
  - **Vai trò**: kiểm tra viên bản vẽ kim loại tấm, chỉ cảnh báo, không sửa.
  - **Cách đọc dữ liệu**: các dòng `#KHUNG`, `FNAME`, `FNAME_TACH`, `L/C/A/P/T/D`; toạ độ mm tính từ góc dưới trái khung.
  - **Luật F-name** (lấy từ `01_Fname/rules/01-quy-tac-fname.md` và `01_Fname/README.md`):
    - thứ tự dòng N, M, T, Q, (V…), Z, rồi dòng nhấn 1 lần `内R…発押し…°` ở cuối;
    - `Z=` và danh sách `T=` phải khớp bảng vật liệu theo `M=`;
    - vật liệu SIMA (縞板 / `Z=*-SIMA`) phải có dòng `裏から見た図` ngay dưới `Z=`.
  - **Ba nhóm cần kiểm**: MTZ, Q với số lượng trong ghi chú, GHI_CHU (yêu cầu tiếng Nhật mà F-name không phản ánh). Kèm bảng nghĩa các từ hay gặp: 逆 (đối xứng / mirror), タップ (taro ren), 曲げ (bẻ), 板厚 (độ dày), 数量 / 個 / 枚 (số lượng), 材質 (vật liệu), 本図 / 逆図.
  - **Mức độ**: `do` khi chắc chắn mâu thuẫn với dữ liệu hoặc bảng vật liệu; `vang` khi nghi ngờ hoặc khi vật liệu chỉ có trong mục CHƯA DUYỆT.
  - **Khung ổn thì không báo**. `ly_do` và `goi_y` viết bằng tiếng Việt, ngắn gọn.
- [ ] **Step 2:** Viết `05_AI/README.md`: tab làm gì, cấu trúc thư mục, lệnh `OHTAAI` / `OHTAAIVUNG` / `OHTAAIZOOM`, nơi đặt key, lời dặn và nhật ký, cùng ghi chú "chưa có trong bản phát hành (ADR 0006)". Mục **Tạo API key Anthropic** gồm các bước:
  1. Đăng ký hoặc đăng nhập console.anthropic.com.
  2. Vào Billing, nạp tiền (credit trả trước).
  3. Vào API Keys, Create Key, đặt tên `OHTA-AI`.
  4. Chép key vào `05_AI\data\ai.key`, một dòng duy nhất.
  5. Không gửi key qua chat, không đưa lên git.
  6. Đặt giới hạn chi tiêu tháng ở Limits.
- [ ] **Step 3:** Chạy thử thật trong ZWCAD **và** AutoCAD trên một bản vẽ có ít nhất 12 khung (để có 2 lô), trong đó cố ý sai một `Z=`. Kết quả mong đợi:
  - khung sai có cảnh báo 🔴 MTZ kèm gợi ý Z đúng;
  - lô thứ hai có `cache_read_input_tokens > 0` trong file log;
  - bấm vào cảnh báo thì zoom đúng khung;
  - file log không chứa key.
  Nếu API trả 400 vì kết hợp `fallbacks` với `output_config.format`, bỏ `fallbacks` cùng header beta, rồi ghi lý do vào ADR 0006.
- [ ] **Step 4:** Chạy `build_ohta.ps1`. Kết quả mong đợi: xanh.
- [ ] **Step 5:** Commit `"Tab AI: loi dan mac dinh, README huong dan API key"`.
