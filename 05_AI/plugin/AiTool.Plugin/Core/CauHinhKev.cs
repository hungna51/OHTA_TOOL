using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    public sealed class LoaiGiaCongKev
    {
        public string Ma = "";
        public string Ten = "";
        public List<string> TuKhoaGhiChu = new List<string>();
        public List<string> TuKhoaFname = new List<string>();
        public string RegexGhiChu = "";
        public List<string> HinhHoc = new List<string>();
        public List<string> LoaiTruHinhHocKhiCo = new List<string>();
        public string MoTaTiengAnh = "";
        public string CauHoiKev = "";
    }

    public sealed class LuatTuyChinhKev
    {
        public string Ma = "";
        public string Ten = "";
        public bool Do = true;
        public string LoaiLoi = "GHI_CHU";
        public List<string> KhiGhiChuCo = new List<string>();
        public List<string> KhiFnameCo = new List<string>();
        public List<string> BatBuocFnameCo = new List<string>();
        public List<string> BatBuocGhiChuCo = new List<string>();
        public string BatBuocHinhHoc = "";
        public string LyDo = "";
        public string GoiY = "";
    }

    /// <summary>
    /// Đọc cấu hình và luật của Kev từ file gốc <c>05_AI\data\kev\00-index.md</c>
    /// rồi lần theo các liên kết <c>[...](ten-file.md)</c> tới các file .md con.
    /// Đồng thời tổng hợp nội dung để đồng bộ sang Claude khi chạy Hybrid / Chỉ Claude.
    /// </summary>
    public sealed class CauHinhKev
    {
        public const string ThuMucKev = @"data\kev";
        public const string FileIndex = "00-index.md";
        public const string FilePhanLoai = "01-phan-loai-gia-cong.md";
        public const string FileLuatFname = "02-luat-fname-vat-lieu.md";
        public const string FileLuatGhiChu = "03-luat-ghi-chu-ban-ve.md";

        private static readonly UTF8Encoding Utf8KhongBom = new UTF8Encoding(false);
        private static readonly Regex RxLinkMd = new Regex(@"\[[^\]]+\]\(([^)]+\.md)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RxHeadingMuc = new Regex(@"^##\s+\[([^\]]+)\]\s*(.*)$", RegexOptions.Compiled);

        public string MayChuLan = "http://NGUYEN-A-HUNG:8008";
        public double NguongPhanLoai = 0.50;
        public double NguongNghiLoi = 0.50;
        public string NhanMacDinh = "PHANG";
        public int SoDuongKegakiCuon = 6;

        public bool KiemTraDu5Truong = true;
        public bool KiemTraBangVatLieu = true;
        public bool KiemTraSima = true;
        public string DongBatBuocSima = "裏から見た図";
        public string HauToZSima = "-SIMA";
        public string TuKhoaMSima = "縞板";
        public bool KiemTraFnameHonHopTextMtext = true;
        public bool KiemTraTrungTenN = true;
        public bool KiemTraVatLieuCpl = true;

        public bool KiemTraSoLuongQ = true;
        public List<string> DonViSoLuong = new List<string> { "枚", "個", "ケ", "本" };
        public List<string> TuKhoaSoLuongMoHo = new List<string> { "台", "本図+逆図" };
        public bool KiemTraDoDayT = true;
        public bool KiemTraDoiTuongLa0Kakou = true;
        public bool KiemTraBeHonHopPhatOshi = true;
        public bool KiemTraMtextKhungIn = true;
        public string KiemTraDimTong1Xy = "auto";
        public string KiemTraHatchTapuTmt = "auto";

        public List<LoaiGiaCongKev> DanhSachLoai = new List<LoaiGiaCongKev>();
        public List<LuatTuyChinhKev> DanhSachLuat = new List<LuatTuyChinhKev>();
        public List<string> DanhSachFileDaNap = new List<string>();

        public static string DuongDanThuMucKev(string thuMucModule) =>
            Path.Combine(thuMucModule, "data", "kev");

        public static Dictionary<string, string> CacFileMacDinh()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [FileIndex] = NoiDungMacDinhIndex(),
                [FilePhanLoai] = NoiDungMacDinhPhanLoai(),
                [FileLuatFname] = NoiDungMacDinhLuatFname(),
                [FileLuatGhiChu] = NoiDungMacDinhLuatGhiChu(),
            };
        }

        /// <summary>Tạo sẵn các file .md mặc định trong 05_AI\data\kev\ nếu chưa tồn tại.</summary>
        public static void DamBaoFileMacDinh(string thuMucModule)
        {
            if (string.IsNullOrWhiteSpace(thuMucModule)) return;
            string dir = DuongDanThuMucKev(thuMucModule);
            Directory.CreateDirectory(dir);
            foreach (var kv in CacFileMacDinh())
            {
                string path = Path.Combine(dir, kv.Key);
                if (!File.Exists(path))
                    File.WriteAllText(path, kv.Value, Utf8KhongBom);
            }
        }

        public static CauHinhKev DocTuThuMuc(string thuMucModule)
        {
            if (string.IsNullOrWhiteSpace(thuMucModule))
                return DocTuBoNhoMacDinh();

            try
            {
                DamBaoFileMacDinh(thuMucModule);
                string dir = DuongDanThuMucKev(thuMucModule);
                string indexPath = Path.Combine(dir, FileIndex);
                if (!File.Exists(indexPath))
                    return DocTuBoNhoMacDinh();

                var ch = new CauHinhKev();
                string indexText = File.ReadAllText(indexPath, Encoding.UTF8);
                ch.DanhSachFileDaNap.Add(indexPath);
                ch.DocThongSoChung(indexText);

                var fileCon = new List<string>();
                foreach (Match m in RxLinkMd.Matches(indexText))
                {
                    string rel = m.Groups[1].Value.Trim().Replace('/', Path.DirectorySeparatorChar);
                    string full = Path.GetFullPath(Path.Combine(dir, rel));
                    if (File.Exists(full) && !fileCon.Contains(full, StringComparer.OrdinalIgnoreCase))
                        fileCon.Add(full);
                }

                foreach (string f in fileCon)
                {
                    ch.DanhSachFileDaNap.Add(f);
                    string nd = File.ReadAllText(f, Encoding.UTF8);
                    ch.DocThongSoChung(nd);
                    ch.DocCacMuc(nd);
                }

                if (ch.DanhSachLoai.Count == 0)
                    ch.DocCacMuc(NoiDungMacDinhPhanLoai());
                return ch;
            }
            catch (Exception)
            {
                return DocTuBoNhoMacDinh();
            }
        }

        public static CauHinhKev DocTuBoNhoMacDinh()
        {
            var ch = new CauHinhKev();
            ch.DocThongSoChung(NoiDungMacDinhIndex());
            ch.DocCacMuc(NoiDungMacDinhPhanLoai());
            ch.DocThongSoChung(NoiDungMacDinhLuatFname());
            ch.DocCacMuc(NoiDungMacDinhLuatFname());
            ch.DocThongSoChung(NoiDungMacDinhLuatGhiChu());
            ch.DocCacMuc(NoiDungMacDinhLuatGhiChu());
            return ch;
        }

        /// <summary>Gộp nội dung hướng dẫn Kev để đính kèm vào lời dặn của Claude (đồng bộ 2 chiều).</summary>
        public static string DocKhoiDongBoChoClaude(string thuMucModule)
        {
            if (string.IsNullOrWhiteSpace(thuMucModule)) return string.Empty;
            try
            {
                CauHinhKev ch = DocTuThuMuc(thuMucModule);
                var sb = new StringBuilder();
                sb.Append("\n\n# Quy tắc bổ sung từ bộ hướng dẫn Kev (05_AI\\data\\kev\\)\n");
                foreach (string f in ch.DanhSachFileDaNap)
                {
                    if (File.Exists(f))
                    {
                        sb.Append("\n<!-- ").Append(Path.GetFileName(f)).Append(" -->\n");
                        sb.Append(File.ReadAllText(f, Encoding.UTF8).Trim()).Append('\n');
                    }
                }
                return sb.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void DocThongSoChung(string md)
        {
            if (string.IsNullOrEmpty(md)) return;
            md = Regex.Replace(md, @"<!--[\s\S]*?-->", string.Empty);
            foreach (string raw in md.Split('\n'))
            {
                string line = raw.Trim();
                if (!line.StartsWith("-", StringComparison.Ordinal)) continue;
                string body = line.Substring(1).Trim();
                int idx = body.IndexOf(':');
                if (idx <= 0) continue;
                string k = body.Substring(0, idx).Trim().ToLowerInvariant();
                string v = body.Substring(idx + 1).Trim();

                switch (k)
                {
                    case "may_chu_lan":
                        if (!string.IsNullOrWhiteSpace(v)) MayChuLan = v.Trim();
                        break;
                    case "nguong_phan_loai":
                        if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double npl) && npl > 0 && npl < 1)
                            NguongPhanLoai = npl;
                        break;
                    case "nguong_nghi_loi":
                        if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double nnl) && nnl > 0 && nnl < 1)
                            NguongNghiLoi = nnl;
                        break;
                    case "nhan_mac_dinh":
                        if (!string.IsNullOrWhiteSpace(v)) NhanMacDinh = v;
                        break;
                    case "so_duong_kegaki_cuon":
                        if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int skc) && skc >= 2)
                            SoDuongKegakiCuon = skc;
                        break;
                    case "kiem_tra_du_5_truong":
                        KiemTraDu5Truong = LaTrue(v);
                        break;
                    case "kiem_tra_bang_vat_lieu":
                        KiemTraBangVatLieu = LaTrue(v);
                        break;
                    case "kiem_tra_sima":
                        KiemTraSima = LaTrue(v);
                        break;
                    case "dong_bat_buoc_sima":
                        if (!string.IsNullOrWhiteSpace(v)) DongBatBuocSima = v;
                        break;
                    case "hau_to_z_sima":
                        if (!string.IsNullOrWhiteSpace(v)) HauToZSima = v;
                        break;
                    case "tu_khoa_m_sima":
                        if (!string.IsNullOrWhiteSpace(v)) TuKhoaMSima = v;
                        break;
                    case "kiem_tra_so_luong_q":
                        KiemTraSoLuongQ = LaTrue(v);
                        break;
                    case "don_vi_so_luong":
                        var dv = TachDanhSach(v);
                        if (dv.Count > 0) DonViSoLuong = dv;
                        break;
                    case "tu_khoa_so_luong_mo_ho":
                        var mh = TachDanhSach(v);
                        if (mh.Count > 0) TuKhoaSoLuongMoHo = mh;
                        break;
                    case "kiem_tra_do_day_t":
                        KiemTraDoDayT = LaTrue(v);
                        break;
                    case "kiem_tra_fname_hon_hop_text_mtext":
                        KiemTraFnameHonHopTextMtext = LaTrue(v);
                        break;
                    case "kiem_tra_trung_ten_n":
                        KiemTraTrungTenN = LaTrue(v);
                        break;
                    case "kiem_tra_vat_lieu_cpl":
                        KiemTraVatLieuCpl = LaTrue(v);
                        break;
                    case "kiem_tra_doi_tuong_la_0_kakou":
                        KiemTraDoiTuongLa0Kakou = LaTrue(v);
                        break;
                    case "kiem_tra_be_hon_hop_phat_oshi":
                        KiemTraBeHonHopPhatOshi = LaTrue(v);
                        break;
                    case "kiem_tra_mtext_khung_in":
                        KiemTraMtextKhungIn = LaTrue(v);
                        break;
                    case "kiem_tra_dim_tong_1_xy":
                        if (!string.IsNullOrWhiteSpace(v)) KiemTraDimTong1Xy = v.Trim().ToLowerInvariant();
                        break;
                    case "kiem_tra_hatch_tapu_tmt":
                        if (!string.IsNullOrWhiteSpace(v)) KiemTraHatchTapuTmt = v.Trim().ToLowerInvariant();
                        break;
                }
            }
        }

        private void DocCacMuc(string md)
        {
            if (string.IsNullOrEmpty(md)) return;
            md = Regex.Replace(md, @"<!--[\s\S]*?-->", string.Empty);
            LoaiGiaCongKev loaiHienTai = null;
            LuatTuyChinhKev luatHienTai = null;

            foreach (string raw in md.Split('\n'))
            {
                string line = raw.Trim();
                Match mHead = RxHeadingMuc.Match(line);
                if (mHead.Success)
                {
                    string tag = mHead.Groups[1].Value.Trim();
                    string ten = mHead.Groups[2].Value.Trim();
                    loaiHienTai = null;
                    luatHienTai = null;

                    if (tag.StartsWith("LUAT:", StringComparison.OrdinalIgnoreCase))
                    {
                        luatHienTai = new LuatTuyChinhKev
                        {
                            Ma = tag.Substring(5).Trim(),
                            Ten = ten,
                        };
                        DanhSachLuat.Add(luatHienTai);
                    }
                    else
                    {
                        loaiHienTai = DanhSachLoai.FirstOrDefault(x => string.Equals(x.Ma, tag, StringComparison.OrdinalIgnoreCase));
                        if (loaiHienTai == null)
                        {
                            loaiHienTai = new LoaiGiaCongKev { Ma = tag.ToUpperInvariant(), Ten = ten };
                            DanhSachLoai.Add(loaiHienTai);
                        }
                    }
                    continue;
                }

                if (line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("# ", StringComparison.Ordinal))
                {
                    loaiHienTai = null;
                    luatHienTai = null;
                    continue;
                }

                if (!line.StartsWith("-", StringComparison.Ordinal)) continue;
                string body = line.Substring(1).Trim();
                int idx = body.IndexOf(':');
                if (idx <= 0) continue;
                string k = body.Substring(0, idx).Trim().ToLowerInvariant();
                string v = body.Substring(idx + 1).Trim();

                if (loaiHienTai != null)
                {
                    switch (k)
                    {
                        case "tu_khoa_ghi_chu":
                            loaiHienTai.TuKhoaGhiChu = TachDanhSach(v);
                            break;
                        case "tu_khoa_fname":
                            loaiHienTai.TuKhoaFname = TachDanhSach(v);
                            break;
                        case "regex_ghi_chu":
                            loaiHienTai.RegexGhiChu = v;
                            break;
                        case "hinh_hoc":
                            loaiHienTai.HinhHoc = TachDanhSach(v);
                            break;
                        case "loai_tru_hinh_hoc_khi_co":
                            loaiHienTai.LoaiTruHinhHocKhiCo = TachDanhSach(v);
                            break;
                        case "mo_ta_tieng_anh":
                            loaiHienTai.MoTaTiengAnh = v;
                            break;
                        case "cau_hoi_kev":
                            loaiHienTai.CauHoiKev = v;
                            break;
                    }
                }
                else if (luatHienTai != null)
                {
                    switch (k)
                    {
                        case "muc_do":
                            luatHienTai.Do = !string.Equals(v, "vang", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "loai_loi":
                            if (!string.IsNullOrWhiteSpace(v)) luatHienTai.LoaiLoi = v.ToUpperInvariant();
                            break;
                        case "khi_ghi_chu_co":
                            luatHienTai.KhiGhiChuCo = TachDanhSach(v);
                            break;
                        case "khi_fname_co":
                            luatHienTai.KhiFnameCo = TachDanhSach(v);
                            break;
                        case "bat_buoc_fname_co":
                            luatHienTai.BatBuocFnameCo = TachDanhSach(v);
                            break;
                        case "bat_buoc_ghi_chu_co":
                            luatHienTai.BatBuocGhiChuCo = TachDanhSach(v);
                            break;
                        case "bat_buoc_hinh_hoc":
                            luatHienTai.BatBuocHinhHoc = v;
                            break;
                        case "ly_do":
                            luatHienTai.LyDo = v;
                            break;
                        case "goi_y":
                            luatHienTai.GoiY = v;
                            break;
                    }
                }
            }
        }

        private static bool LaTrue(string v) =>
            string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "co", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);

        private static List<string> TachDanhSach(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return new List<string>();
            return v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();
        }

        /// <summary>
        /// Ghi bộ file luật .md nhận được từ API GET /v1/rules của Máy chủ Kev xuống ổ cứng máy con.
        /// Trả về số file .md đã cập nhật.
        /// </summary>
        public static int ApDungGoiLuatJson(string json, string thuMucModule)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(thuMucModule))
                return 0;
            try
            {
                var root = JsonLite.Parse(json) as JsonObject;
                if (root == null) return 0;

                int daGhi = 0;
                if (root.Get("kev_files") is JsonObject kevFiles)
                {
                    string kevDir = DuongDanThuMucKev(thuMucModule);
                    Directory.CreateDirectory(kevDir);
                    foreach (var kv in kevFiles.Items)
                    {
                        string safeName = Path.GetFileName(kv.Key);
                        if (string.IsNullOrWhiteSpace(safeName) || !safeName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                            continue;
                        string content = kv.Value as string;
                        if (string.IsNullOrWhiteSpace(content)) continue;
                        string fullPath = Path.Combine(kevDir, safeName);
                        string hienCo = File.Exists(fullPath) ? File.ReadAllText(fullPath, Encoding.UTF8) : null;
                        if (!string.Equals(hienCo, content, StringComparison.Ordinal))
                            File.WriteAllText(fullPath, content, Utf8KhongBom);
                        daGhi++;
                    }
                }

                string hdClaude = root.GetString("huong_dan_ai");
                if (!string.IsNullOrWhiteSpace(hdClaude))
                {
                    string claudePath = DuLieuTinh.FileHuongDan(thuMucModule);
                    Directory.CreateDirectory(Path.GetDirectoryName(claudePath));
                    string hienCo = File.Exists(claudePath) ? File.ReadAllText(claudePath, Encoding.UTF8) : null;
                    if (!string.Equals(hienCo, hdClaude, StringComparison.Ordinal))
                        File.WriteAllText(claudePath, hdClaude, Utf8KhongBom);
                    daGhi++;
                }
                return daGhi;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string NoiDungMacDinhIndex() =>
@"# 00 — File Chỉ Dẫn Chính Của AI Kev (Master Index)

Đây là **file gốc (`00-index.md`)** mà **Kev-0.8B** đọc đầu tiên trước mỗi lần kiểm tra & phân loại bản vẽ.
Từ file này, hệ thống tự động lần theo các đường dẫn `.md` bên dưới để nạp luật phân loại tấm gia công và luật kiểm lỗi.
Khi chạy chế độ **Hybrid** hoặc **Chỉ Claude**, nội dung các file này cũng được đồng bộ sang cho Claude.
Khi các máy con trong mạng LAN kết nối tới Máy chủ Kev (`may_chu_lan`), máy con sẽ **tự động đồng bộ toàn bộ bộ file `.md` mới nhất từ Máy chủ về** trước khi kiểm tra.

## 1. Thông số chung của Kev
- may_chu_lan: http://NGUYEN-A-HUNG:8008
- nguong_phan_loai: 0.50
- nguong_nghi_loi: 0.50
- nhan_mac_dinh: PHANG
- so_duong_kegaki_cuon: 6

## 2. Danh sách các file hướng dẫn con (Kev đọc theo thứ tự)
1. [01-phan-loai-gia-cong.md](01-phan-loai-gia-cong.md) — Định nghĩa các loại tấm gia công (`BE`, `VAT`, `CUON`, `SARA`, `TAPU`...) để dán nhãn góc khung.
2. [02-luat-fname-vat-lieu.md](02-luat-fname-vat-lieu.md) — Luật kiểm tra cấu trúc F-name (`N, M, T, Q, Z`), đối chiếu bảng vật liệu chuẩn và tấm nhám `SIMA`.
3. [03-luat-ghi-chu-ban-ve.md](03-luat-ghi-chu-ban-ve.md) — Luật đối chiếu ghi chú tiếng Nhật trên bản vẽ với F-name (số lượng `Q`, độ dày `t=`, yêu cầu gia công).

*(Mẹo: Nếu muốn tách thêm file luật mới, ví dụ `04-luat-rieng-xuong.md`, chỉ cần tạo file trong thư mục `05_AI\data\kev\` trên máy chủ và thêm 1 dòng liên kết `[04-luat-rieng-xuong.md](04-luat-rieng-xuong.md)` vào danh sách trên — các máy con trong mạng LAN sẽ tự tải về).*
";

        public static string NoiDungMacDinhPhanLoai() =>
@"# 01 — Luật Phân Loại Tấm Gia Công Sau Cắt Laser (Đồng Bộ Lisp CSV v5.0)

Mỗi mục `## [MÃ_NHÃN]` bên dưới định nghĩa một nhóm gia công (bám sát hàm `CSV--scan-machining-texts` và `CSV--bend-list-smart` của Lisp CSV).
- Nếu muốn **thêm nhóm gia công mới** (ví dụ `## [BARING] Dập lỗ bẻ mép`), chỉ cần thêm một khối `## [BARING]` theo cùng mẫu bên dưới.
- Nhãn dán ở góc trên-trái khung sẽ ghép theo đúng thứ tự các mục trong file này (ví dụ `[BE - TAPU]`). Nếu không khớp nhóm nào sẽ dán nhãn mặc định `[PHANG]`.

## [BE] Tấm bẻ / chấn / nhấn 1 lần / bẻ bước Pitchi (mã CSV: m, m(pitchi), 発押し)
- tu_khoa_ghi_chu: 曲げ, 折り, 曲げ角度, 発押し
- tu_khoa_fname: V=, V1=, V2=, V3=, U=, P=, R=, 発押し, シマ目内, シマ目外, 引代
- regex_ghi_chu: 
- hinh_hoc: duong_be_dashed, mtext_khung_in_m
- loai_tru_hinh_hoc_khi_co: 
- mo_ta_tieng_anh: Requires bending, press-bending (発押し), or pitch-bending (曲げ角度/V=/DASHED bend lines)
- cau_hoi_kev: Does this part require bending, press-bending, or folding (interpreted_notes mentions bending or bending_BE has V=/発押し)?

## [VAT] Tấm vát mép hàn (Chamfer / Bevel 開先, mã CSV: k)
- tu_khoa_ghi_chu: 開先施工範囲, 面取り, 開先, カイサキ, ｶｲｻｷ, レ形, K形, テーパー, 糸面
- tu_khoa_fname: K=
- regex_ghi_chu: \bC\d+(\.\d+)?\b
- hinh_hoc: mtext_khung_in_k
- loai_tru_hinh_hoc_khi_co: 
- mo_ta_tieng_anh: Requires edge chamfering or weld beveling (開先施工範囲/開先/面取り)
- cau_hoi_kev: Does this part require edge chamfering or beveling (interpreted_notes mentions chamfering/beveling)?

## [CUON] Tấm cuộn cung tròn / lốc ống tròn (Roll, mã CSV: r1, r2)
- tu_khoa_ghi_chu: R曲げ, ロール, 巻き, 円筒
- tu_khoa_fname: 
- regex_ghi_chu: 
- hinh_hoc: pichi_cuon, mtext_khung_in_r
- loai_tru_hinh_hoc_khi_co: 
- mo_ta_tieng_anh: Requires rolling/curving or multiple parallel pitch lines (ロール/R曲げ/r1/r2)
- cau_hoi_kev: Does this part require rolling or has multiple parallel pitch lines (has_multiple_parallel_pitch_lines is true or interpreted_notes mentions rolling)?

## [SARA] Khoét loe âm mũ vít (Countersink 皿もみ, mã CSV: s)
- tu_khoa_ghi_chu: 皿もみ, 皿モミ, 皿穴, 皿, サラ, ｻﾗ, CSK
- tu_khoa_fname: 
- regex_ghi_chu: 
- hinh_hoc: mtext_khung_in_s
- loai_tru_hinh_hoc_khi_co: 
- mo_ta_tieng_anh: Requires countersinking (皿もみ/皿/sara)
- cau_hoi_kev: Does this part require countersinking (interpreted_notes mentions countersinking/sara)?

## [TAPU] Taro ren lỗ (Tap タップ, mã CSV: t)
- tu_khoa_ghi_chu: タップ, ﾀｯﾌﾟ, TAP, ネジ
- tu_khoa_fname: 
- regex_ghi_chu: \bM([3-9]|1\d|2[0-4])\b
- hinh_hoc: lo_kep_dong_tam, hatch_tapu_tmt, mtext_khung_in_t
- loai_tru_hinh_hoc_khi_co: SARA
- mo_ta_tieng_anh: Requires tapping threads (タップ/concentric double circles/TAPU_TMT hatch)
- cau_hoi_kev: Does this part have tapped holes or concentric double circles (concentric_double_circles_count > 0 or interpreted_notes mentions tapping)?

<!--
Nếu muốn tách riêng nhãn [PITCHI] (bẻ bước 曲げ角度) hoặc [PHAT_OSHI] (nhấn 1 lần 発押し) trên bản vẽ,
hãy bỏ cặp thẻ chú thích ở đầu và cuối khối bên dưới:

## [PITCHI] Bẻ nhiều bước theo cung (曲げ角度 - mã CSV: m(pitchi))
- tu_khoa_ghi_chu: 曲げ角度
- tu_khoa_fname: 
- hinh_hoc: 
- mo_ta_tieng_anh: Requires multi-step pitch bending (曲げ角度 / m(pitchi))
- cau_hoi_kev: Does this part require multi-step pitch bending (曲げ角度)?

## [PHAT_OSHI] Gia công Nhấn 1 lần (内R-発押し)
- tu_khoa_ghi_chu: 発押し
- tu_khoa_fname: 発押し
- hinh_hoc: 
- mo_ta_tieng_anh: Requires single-stroke radius press bending (内R-発押し)
- cau_hoi_kev: Does this part require single-stroke press bending (発押し)?
-->
";

        public static string NoiDungMacDinhLuatFname() =>
@"# 02 — Luật Kiểm Tra F-name, Layer 2-F-NAME & Bảng Vật Liệu (MTZ / CSV)

Quy định kiểm tra cấu trúc khối chữ F-name trên layer `2-F-NAME`, đối chiếu với bảng vật liệu chuẩn (`01_Fname\data\catalog.csv`) và các điều kiện bắt buộc của Lisp Xuất CSV (`xuat_csv_v5.0.1.lsp`).

## 1. Cấu hình kiểm tra F-name & Vật liệu
- kiem_tra_du_5_truong: true
- kiem_tra_bang_vat_lieu: true
- kiem_tra_sima: true
- dong_bat_buoc_sima: 裏から見た図
- hau_to_z_sima: -SIMA
- tu_khoa_m_sima: 縞板
- kiem_tra_fname_hon_hop_text_mtext: true
- kiem_tra_trung_ten_n: true
- kiem_tra_vat_lieu_cpl: true

## 2. Giải thích quy tắc áp dụng
1. **Đủ 5 trường bắt buộc (`N=`, `M=`, `T=`, `Q=`, `Z=`):**
   - Thứ tự dòng chuẩn: `N=`, `M=`, `T=` (luôn có ít nhất 1 số lẻ: `6.0`), `Q=`, các dòng bẻ `V=`, `Z=`, `裏から見た図` (nếu là tấm nhám SIMA), và **dòng nhấn 1 lần `内R...-発押し...°` luôn đứng cuối cùng dưới cùng**.
   - Thiếu bất kỳ trường nào hoặc giá trị rỗng -> báo lỗi đỏ `🔴 KHAC` (tương ứng `F-name error(1)` / `Lỗi Fname (2)` trong Lisp CSV).
2. **Không trộn lẫn `TEXT` và `MTEXT` trên layer `2-F-NAME` (`kiem_tra_fname_hon_hop_text_mtext`):**
   - Theo hàm `CSV--detect-fname-type` của Lisp CSV, mỗi khung in chỉ được chứa **1 kiểu** F-name (hoặc 1 khối `MTEXT` duy nhất, hoặc cụm `TEXT` rời) và chỉ có **1 cụm `N=`**. Nếu trộn cả `TEXT` và `MTEXT` hoặc có `> 1` cụm F-name -> báo lỗi đỏ `🔴 KHAC`.
3. **Không trùng lặp tên tấm `N=` giữa các khung (`kiem_tra_trung_ten_n`):**
   - Theo hàm `has-duplicates` của Lisp CSV, nếu hai khung trở lên có cùng tên `N=` thì lệnh `CSV` sẽ dừng hẳn và báo lỗi -> báo lỗi đỏ `🔴 KHAC`.
4. **Đối chiếu Bảng vật liệu (`M, Loai, Z, T_list`):**
   - `M=` có trong bảng chuẩn nhưng `Z=` khác cột `Z` chuẩn (ví dụ `M=SS400` nhưng `Z=SPHC` thay vì `Z=SS400`) -> báo lỗi đỏ `🔴 MTZ` và gợi ý `Z=` đúng.
   - `T=` không nằm trong danh sách `T_list` của `M=` -> báo lỗi đỏ `🔴 MTZ` và gợi ý các độ dày chuẩn.
   - `M=` chỉ nằm trong danh sách chờ duyệt hoặc chưa có trong bảng -> báo vàng `🟡 MTZ`.
5. **Vật liệu nhám (`SIMA`) và tấm chống trượt `CPL`:**
   - Khi `M=` chứa `縞板` hoặc `Z=` kết thúc bằng `-SIMA`, bên dưới `Z=` bắt buộc phải có dòng `裏から見た図`. Nếu thiếu -> báo vàng `🟡 KHAC`.
   - Khi ghi chú trên bản vẽ có `CPL` hoặc `裏面CPL` (thép tấm vân chống trượt), vật liệu chuẩn phải đổi thành `M=ムキ 縞板` và `Z=SS400-SIMA`.
";

        public static string NoiDungMacDinhLuatGhiChu() =>
@"# 03 — Luật Đối Chiếu Ghi Chú Bản Vẽ, Hình Học & Thông Tin Tấm CSV (Q / GHI_CHU / KHUNG_IN)

Quy định đối chiếu giữa chữ ghi chú của người thiết kế, hình học gia công (`0-KAKOU`, `3-KEGAKI`, `1-XY`, `TAPU_TMT`) và khối MText thông tin tấm trên layer `KHUNG_IN` của Lisp Xuất CSV (`xuat_csv_v5.0.1.lsp`).

## 1. Cấu hình kiểm tra Số lượng (Q), Độ dày (t=) & Chuẩn Lisp CSV
- kiem_tra_so_luong_q: true
- don_vi_so_luong: 枚, 個, ケ, 本
- tu_khoa_so_luong_mo_ho: 台, 本図+逆図
- kiem_tra_do_day_t: true
- kiem_tra_doi_tuong_la_0_kakou: true
- kiem_tra_be_hon_hop_phat_oshi: true
- kiem_tra_mtext_khung_in: true
- kiem_tra_dim_tong_1_xy: auto
- kiem_tra_hatch_tapu_tmt: auto

## 2. Giải thích các kiểm tra tự động theo chuẩn Lisp CSV
1. **Đối tượng lạ trên layer `0-KAKOU` (`kiem_tra_doi_tuong_la_0_kakou`):**
   - Layer `0-KAKOU` chỉ được chứa nét cắt (`Continuous`) và đường bẻ (`DASHED*`). Nếu có `TEXT`, `MTEXT`, `DIMENSION` hoặc `HATCH` nằm nhầm trên `0-KAKOU` -> báo lỗi đỏ `🔴 KHAC` (tương ứng lỗi `invalid-entity` Stage 0a của Lisp CSV).
2. **Tấm bẻ hỗn hợp `V=` + `発押し` (`kiem_tra_be_hon_hop_phat_oshi` — `09-press-detection.md`):**
   - Khi F-name vừa có `発押し` vừa có `V=` hoặc `シマ目` (chế độ `'mixed`), Lisp CSV quét hộp nghiêng `±50mm` quanh từng nhóm đường bẻ để phân loại đường nào nhấn 1 lần (`count × 2`), đường nào bẻ thường (`count × 1`).
   - Nếu có nhóm đường bẻ không tìm thấy chữ chú thích ký hiệu bẻ gần đó -> báo vàng `🟡 GHI_CHU`. Nếu 1 đoạn đường bẻ đơn có cả 2 loại chú thích mâu thuẫn -> báo đỏ `🔴 GHI_CHU`.
3. **Kiểm tra khối MText thông tin tấm trên layer `KHUNG_IN` (`kiem_tra_mtext_khung_in` — Bước 1 sang Bước 2 của lệnh `CSV`):**
   - Khi khung đã chạy Bước 1 (*Xuất thông tin tấm*) và có MText trên layer `KHUNG_IN`:
     - Tổng số dòng phải bằng `12 + số loại đường bẻ` (nếu có gia công `m`). Sai số dòng (do xuất đè 2 lần hoặc MText bản cũ) -> báo đỏ `🔴 KHAC`.
     - Có gia công vát (`k` hoặc ghi chú `開先施工範囲`) nhưng `Chiều dài vát: 0` -> báo đỏ `🔴 GHI_CHU` (*chưa nhập chiều dài vát*).
     - Có gia công bẻ bước `m(pitchi)` (hoặc ghi chú `曲げ角度`) nhưng `Pitchi - Số lần: 0` hoặc `Pitchi - Chiều dài (mm): 0` -> báo đỏ `🔴 GHI_CHU`.
     - Tấm có gia công cuộn (`CUON`) nhưng dòng `Các loại gia công:` chưa điền `r1` (cuộn cung tròn 1) hoặc `r2` (cuộn ống tròn) -> báo vàng `🟡 GHI_CHU`.
     - Tấm có nhiều hơn **8 loại đường bẻ** -> báo vàng `🟡 KHAC` (file CSV chỉ có 8 cặp cột `Bẻ1..Bẻ8`).
4. **Kiểm tra Dim tổng `1-XY` và Hatch `TAPU_TMT` thông minh (`auto`):**
   - Khi bản vẽ đã sử dụng layer `1-XY` (hoặc đã có MText `KHUNG_IN`): khung nào thiếu dim tổng trên `1-XY` hoặc có số dim `1-XY` khác 1 và 2 -> báo đỏ `🔴 KHAC`.
   - Khi bản vẽ đã sử dụng layer `TAPU_TMT`: khung nào có gia công Tapu (`タップ`) hoặc Sara (`皿もみ`) mà chưa có `HATCH` trên `TAPU_TMT` -> báo vàng `🟡 GHI_CHU`.

## 3. Các luật ràng buộc Ghi chú <-> F-name (Có thể tự thêm mục `## [LUAT: ...]` mới)

## [LUAT: BE_THIEU_V] Ghi chú có yêu cầu bẻ nhưng F-name thiếu dòng V= / 発押し
- muc_do: do
- loai_loi: GHI_CHU
- khi_ghi_chu_co: 曲げ, 折り
- bat_buoc_fname_co: V=, V1=, V2=, V3=, U=, P=, R=, 発押し, シマ目内, シマ目外
- ly_do: Ghi chú có yêu cầu bẻ ('曲げ'/'折り') nhưng F-name thiếu dòng thông số bẻ (V= / 発押し / シマ目).
- goi_y: Điền thông số bẻ vào dòng V= hoặc 発押し

<!--
Mẫu thêm luật mới (bỏ dấu chú thích để kích hoạt):
## [LUAT: BAO_VE_BE_MAT] Ghi chú yêu cầu dán nilon bảo vệ
- muc_do: vang
- loai_loi: GHI_CHU
- khi_ghi_chu_co: 保護シート, ビニール
- bat_buoc_fname_co: 保護シート, ビニール
- ly_do: Ghi chú có yêu cầu bảo vệ bề mặt (保護シート) nhưng F-name chưa ghi chú.
- goi_y: Kiểm tra yêu cầu bề mặt
-->
";
    }
}
