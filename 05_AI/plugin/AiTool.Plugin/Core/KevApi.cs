using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    public sealed class KetQuaKhungKev
    {
        public int Khung;
        public string NhanGiaCong = "PHANG";
        public double XacSuatLoi;
        public bool NghiLoi;
        public double LatencyMs;
        public long TokenVao, TokenRa;
        public List<CanhBao> CanhBao = new List<CanhBao>();
        public string Loi;
    }

    public sealed class DacTrungKhung
    {
        public List<string> DongFname = new List<string>();
        public int SoCumN;
        public FnameRecord Fname;
        public List<string> GhiChu = new List<string>();
        public int SoDuongKegaki;
        public int SoDuongBeDashed;
        public int SoHatchTapu;
        public int SoDim1Xy;
        public bool CoMtextKhungIn;
        public List<string> CacLoaiGiaCongKhungIn = new List<string>();
        public bool CoPichiCuon;
        public int SoLoKakou;
        public int SoVongKegaki;
        public int SoCapDongTam;
        public Dictionary<string, bool> KhopLoaiCucBo = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public bool CoBe => KhopLoai("BE");
        public bool CoVat => KhopLoai("VAT");
        public bool CoCuon => KhopLoai("CUON");
        public bool CoSara => KhopLoai("SARA");
        public bool CoTapu => KhopLoai("TAPU");
        public string ZChuanTheoBang = "";
        public string DsDayChuanTheoBang = "";
        public List<CanhBao> CanhBaoCucBo = new List<CanhBao>();

        public bool KhopLoai(string ma) =>
            KhopLoaiCucBo.TryGetValue(ma ?? "", out bool v) && v;
    }

    /// <summary>
    /// Giao thức tạo request / đọc response cho Kev Server (POST /v1/systemone)
    /// kết hợp bộ file cấu hình .md phân tầng (<see cref="CauHinhKev"/>) để phân loại tấm gia công
    /// (BE, VAT, CUON, SARA, TAPU, PHANG... hoặc nhóm tự thêm) và lọc khung nghi ngờ lỗi.
    /// </summary>
    public static class KevApi
    {
        public const string LayerNhan = "AI-PHAN-LOAI";
        public const string UrlMacDinh = "http://127.0.0.1:8008";
        public const double NguongPhanLoai = 0.50;
        public const double NguongNghiLoi = 0.50;

        private static readonly Regex RxDoDayGhiChu = new Regex(@"(?:板厚|(?<![A-Za-z])[tT]\s*=)\s*(\d+(?:\.\d+)?)\s*(?:mm)?", RegexOptions.Compiled);
        private static readonly Regex RxDongBeKhungIn = new Regex(@"^(\d+)\.\s*Chi", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static DacTrungKhung PhanTich(KhungDuLieu k, string khoiVatLieu, CauHinhKev cauHinh = null)
        {
            CauHinhKev ch = cauHinh ?? CauHinhKev.DocTuBoNhoMacDinh();
            var dt = new DacTrungKhung();
            if (k == null) return dt;

            var fnameDoiTuong = k.DoiTuong
                .Where(d => d.Loai == LoaiDoiTuong.Text && string.Equals(d.Layer, DongChu.LayerFname, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => Math.Round(d.Y1, 2)).ThenBy(d => d.X1)
                .ToList();

            dt.DongFname = fnameDoiTuong
                .SelectMany(d => FnameParser.SplitAndCleanMText(d.NoiDung))
                .ToList();

            dt.SoCumN = dt.DongFname.Count(s => s.TrimStart().StartsWith("N=", StringComparison.OrdinalIgnoreCase));
            if (dt.SoCumN >= 1)
                dt.Fname = FnameParser.Parse(dt.DongFname);

            var tamKakou = new List<(double X, double Y, double D)>();
            var tamKegaki = new List<(double X, double Y, double D)>();
            var dsMtextKhungIn = new List<string>();

            foreach (DoiTuong d in k.DoiTuong)
            {
                if (string.Equals(d.Layer, LayerNhan, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (d.Loai == LoaiDoiTuong.Text && !string.Equals(d.Layer, DongChu.LayerFname, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(d.Layer, "KHUNG_IN", StringComparison.OrdinalIgnoreCase) && LaMtextThongTinTamCsv(d.NoiDung))
                    {
                        dt.CoMtextKhungIn = true;
                        dsMtextKhungIn.Add(d.NoiDung);
                        TrichLoaiGiaCongKhungIn(d.NoiDung, dt.CacLoaiGiaCongKhungIn);
                        continue;
                    }

                    foreach (string s in FnameParser.SplitAndCleanMText(d.NoiDung))
                    {
                        string t = s.Trim();
                        if (t.Length > 0) dt.GhiChu.Add(t);
                    }
                }
                else if (d.Loai == LoaiDoiTuong.Line)
                {
                    if (string.Equals(d.Layer, "3-KEGAKI", StringComparison.OrdinalIgnoreCase))
                    {
                        dt.SoDuongKegaki++;
                    }
                    else if (string.Equals(d.Layer, "0-KAKOU", StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrEmpty(d.KieuNet) &&
                             d.KieuNet.StartsWith("DASHED", StringComparison.OrdinalIgnoreCase))
                    {
                        dt.SoDuongBeDashed++;
                    }
                }
                else if (d.Loai == LoaiDoiTuong.Circle)
                {
                    if (string.Equals(d.Layer, "0-KAKOU", StringComparison.OrdinalIgnoreCase))
                    {
                        dt.SoLoKakou++;
                        tamKakou.Add((d.Cx, d.Cy, 2 * d.R));
                    }
                    else if (string.Equals(d.Layer, "3-KEGAKI", StringComparison.OrdinalIgnoreCase))
                    {
                        dt.SoVongKegaki++;
                        tamKegaki.Add((d.Cx, d.Cy, 2 * d.R));
                    }
                }
                else if (d.Loai == LoaiDoiTuong.Hatch)
                {
                    if (string.Equals(d.Layer, "TAPU_TMT", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.Layer, "TO", StringComparison.OrdinalIgnoreCase))
                    {
                        dt.SoHatchTapu++;
                    }
                }
                else if (d.Loai == LoaiDoiTuong.Dim)
                {
                    if (string.Equals(d.Layer, "1-XY", StringComparison.OrdinalIgnoreCase))
                    {
                        dt.SoDim1Xy++;
                    }
                }
            }

            foreach (var ck in tamKegaki)
            {
                if (tamKakou.Any(ca => Math.Abs(ca.X - ck.X) <= 1.0 && Math.Abs(ca.Y - ck.Y) <= 1.0 && Math.Abs(ca.D - ck.D) > 0.2))
                    dt.SoCapDongTam++;
            }

            dt.CoPichiCuon = dt.SoDuongKegaki >= ch.SoDuongKegakiCuon;

            string tatCaGhiChu = string.Join(" | ", dt.GhiChu);
            string tatCaFname = string.Join(" | ", dt.DongFname);

            // Vòng 1: kiểm tra từ khóa ghi chú, từ khóa F-name và regex cho từng loại gia công trong 01-phan-loai-gia-cong.md
            foreach (LoaiGiaCongKev loai in ch.DanhSachLoai)
            {
                bool khop = false;
                if (loai.TuKhoaGhiChu.Any(tk => ChuaTuKhoa(tatCaGhiChu, tk)))
                    khop = true;
                if (!khop && loai.TuKhoaFname.Any(tk => KhopDongFname(dt.DongFname, tatCaFname, tk, yeuCauCoGiaTri: false)))
                    khop = true;
                if (!khop && !string.IsNullOrWhiteSpace(loai.RegexGhiChu))
                {
                    try { if (Regex.IsMatch(tatCaGhiChu, loai.RegexGhiChu)) khop = true; }
                    catch (Exception) { }
                }
                dt.KhopLoaiCucBo[loai.Ma] = khop;
            }

            // Vòng 2: kiểm tra điều kiện hình học (có xét loại trừ khi đã khớp nhóm khác, vd lo_kep_dong_tam của TAPU loại trừ khi có SARA)
            foreach (LoaiGiaCongKev loai in ch.DanhSachLoai)
            {
                if (dt.KhopLoai(loai.Ma) || loai.HinhHoc.Count == 0)
                    continue;
                bool biLoaiTru = loai.LoaiTruHinhHocKhiCo.Any(maKhac => dt.KhopLoai(maKhac));
                if (biLoaiTru)
                    continue;
                foreach (string hh in loai.HinhHoc)
                {
                    if (string.Equals(hh, "pichi_cuon", StringComparison.OrdinalIgnoreCase) && dt.CoPichiCuon)
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "lo_kep_dong_tam", StringComparison.OrdinalIgnoreCase) && dt.SoCapDongTam > 0)
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "kegaki", StringComparison.OrdinalIgnoreCase) && dt.SoDuongKegaki > 0)
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "duong_be_dashed", StringComparison.OrdinalIgnoreCase) && dt.SoDuongBeDashed > 0)
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "hatch_tapu_tmt", StringComparison.OrdinalIgnoreCase) && dt.SoHatchTapu > 0)
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "mtext_khung_in_m", StringComparison.OrdinalIgnoreCase) &&
                             dt.CacLoaiGiaCongKhungIn.Any(x => string.Equals(x, "m", StringComparison.OrdinalIgnoreCase) || string.Equals(x, "m(pitchi)", StringComparison.OrdinalIgnoreCase)))
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "mtext_khung_in_k", StringComparison.OrdinalIgnoreCase) &&
                             dt.CacLoaiGiaCongKhungIn.Any(x => string.Equals(x, "k", StringComparison.OrdinalIgnoreCase)))
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "mtext_khung_in_r", StringComparison.OrdinalIgnoreCase) &&
                             dt.CacLoaiGiaCongKhungIn.Any(x => string.Equals(x, "r1", StringComparison.OrdinalIgnoreCase) || string.Equals(x, "r2", StringComparison.OrdinalIgnoreCase)))
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "mtext_khung_in_s", StringComparison.OrdinalIgnoreCase) &&
                             dt.CacLoaiGiaCongKhungIn.Any(x => string.Equals(x, "s", StringComparison.OrdinalIgnoreCase)))
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                    else if (string.Equals(hh, "mtext_khung_in_t", StringComparison.OrdinalIgnoreCase) &&
                             dt.CacLoaiGiaCongKhungIn.Any(x => string.Equals(x, "t", StringComparison.OrdinalIgnoreCase)))
                        dt.KhopLoaiCucBo[loai.Ma] = true;
                }
            }

            KiemTraLuatCucBo(k, fnameDoiTuong, dsMtextKhungIn, dt, tatCaGhiChu, tatCaFname, khoiVatLieu, ch);
            return dt;
        }

        private static bool LaMtextThongTinTamCsv(string noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung)) return false;
            return noiDung.IndexOf("TAPPU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   noiDung.IndexOf("TAPU&SARA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   noiDung.IndexOf("Các loại gia công", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void TrichLoaiGiaCongKhungIn(string noiDung, List<string> dsLoai)
        {
            foreach (string line in FnameParser.SplitAndCleanMText(noiDung))
            {
                string t = line.Trim();
                int idx = t.IndexOf("Các loại gia công:", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string val = t.Substring(idx + "Các loại gia công:".Length).Trim();
                    foreach (string tk in val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string m = tk.Trim();
                        if (m.Length > 0 && !dsLoai.Contains(m, StringComparer.OrdinalIgnoreCase))
                            dsLoai.Add(m);
                    }
                }
            }
        }

        private static bool ChuaTuKhoa(string vanBan, string tuKhoa)
        {
            if (string.IsNullOrEmpty(vanBan) || string.IsNullOrEmpty(tuKhoa)) return false;
            return vanBan.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool KhopDongFname(List<string> dongFname, string tatCaFname, string mau, bool yeuCauCoGiaTri)
        {
            if (string.IsNullOrWhiteSpace(mau)) return false;
            string m = mau.Trim();
            if (m.EndsWith("=", StringComparison.Ordinal))
            {
                foreach (string d in dongFname)
                {
                    string t = d.Trim();
                    if (t.StartsWith(m, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!yeuCauCoGiaTri) return true;
                        string gt = t.Substring(m.Length).Trim();
                        if (gt.Length > 0) return true;
                    }
                }
                return false;
            }
            return tatCaFname.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void KiemTraLuatCucBo(
            KhungDuLieu k,
            List<DoiTuong> fnameDoiTuong,
            List<string> dsMtextKhungIn,
            DacTrungKhung dt,
            string tatCaGhiChu,
            string tatCaFname,
            string khoiVatLieu,
            CauHinhKev ch)
        {
            int soKhung = k.SoThuTu;
            if (dt.DongFname.Count == 0)
            {
                dt.CanhBaoCucBo.Add(new CanhBao
                {
                    Khung = soKhung, Do = true, LoaiLoi = "KHAC",
                    LyDo = "Khung không có F-name trên layer 2-F-NAME.", GoiY = ""
                });
                return;
            }

            if (ch.KiemTraFnameHonHopTextMtext)
            {
                if (dt.SoCumN > 1)
                {
                    dt.CanhBaoCucBo.Add(new CanhBao
                    {
                        Khung = soKhung, Do = true, LoaiLoi = "KHAC",
                        LyDo = "Khung chứa " + dt.SoCumN + " cụm F-name (N=) trên layer 2-F-NAME; mỗi khung chỉ được phép có 1 khối F-name.",
                        GoiY = "Xóa khối F-name thừa hoặc tách khung"
                    });
                }
                else if (fnameDoiTuong.Any(x => x.LaMText) && fnameDoiTuong.Any(x => !x.LaMText))
                {
                    dt.CanhBaoCucBo.Add(new CanhBao
                    {
                        Khung = soKhung, Do = true, LoaiLoi = "KHAC",
                        LyDo = "Layer 2-F-NAME trộn lẫn cả TEXT và MTEXT trong cùng một khung (Lisp CSV sẽ báo lỗi).",
                        GoiY = "Chuyển toàn bộ về 1 MTEXT hoặc toàn TEXT rời"
                    });
                }
            }

            if (ch.KiemTraDoiTuongLa0Kakou)
            {
                var loaiLaKakou = k.DoiTuong
                    .Where(d => string.Equals(d.Layer, "0-KAKOU", StringComparison.OrdinalIgnoreCase) &&
                                (d.Loai == LoaiDoiTuong.Text || d.Loai == LoaiDoiTuong.Dim || d.Loai == LoaiDoiTuong.Hatch))
                    .Select(d => d.Loai.ToString())
                    .Distinct()
                    .ToList();
                if (loaiLaKakou.Count > 0)
                {
                    dt.CanhBaoCucBo.Add(new CanhBao
                    {
                        Khung = soKhung, Do = true, LoaiLoi = "KHAC",
                        LyDo = "Layer 0-KAKOU chứa đối tượng không hợp lệ (" + string.Join(", ", loaiLaKakou) + "); Lisp CSV sẽ dừng ở bước kiểm tra đầu vào.",
                        GoiY = "Chuyển chữ/dim/hatch sang layer đúng"
                    });
                }

                var loaiLaFname = k.DoiTuong
                    .Where(d => string.Equals(d.Layer, DongChu.LayerFname, StringComparison.OrdinalIgnoreCase) &&
                                d.Loai != LoaiDoiTuong.Text)
                    .Select(d => d.Loai.ToString())
                    .Distinct()
                    .ToList();
                if (loaiLaFname.Count > 0)
                {
                    dt.CanhBaoCucBo.Add(new CanhBao
                    {
                        Khung = soKhung, Do = true, LoaiLoi = "KHAC",
                        LyDo = "Layer 2-F-NAME chứa đối tượng hình học (" + string.Join(", ", loaiLaFname) + ") không phải chữ.",
                        GoiY = "Chuyển hình học khỏi layer 2-F-NAME"
                    });
                }
            }

            if (dt.SoCumN == 1 && dt.Fname != null)
            {
                FnameRecord r = dt.Fname;
                if (ch.KiemTraDu5Truong)
                {
                    var thieu = new List<string>();
                    if (string.IsNullOrWhiteSpace(r.N)) thieu.Add("N=");
                    if (string.IsNullOrWhiteSpace(r.M)) thieu.Add("M=");
                    if (string.IsNullOrWhiteSpace(r.T)) thieu.Add("T=");
                    if (string.IsNullOrWhiteSpace(r.Q)) thieu.Add("Q=");
                    if (string.IsNullOrWhiteSpace(r.Z)) thieu.Add("Z=");

                    if (thieu.Count > 0)
                    {
                        dt.CanhBaoCucBo.Add(new CanhBao
                        {
                            Khung = soKhung, Do = true, LoaiLoi = "KHAC",
                            LyDo = "F-name thiếu dòng " + string.Join(", ", thieu) + ".",
                            GoiY = ""
                        });
                    }
                }

                if (ch.KiemTraVatLieuCpl && CoGhiChuCpl(tatCaGhiChu))
                {
                    string mTrim = (r.M ?? "").Trim();
                    string zTrim = (r.Z ?? "").Trim();
                    if (!string.Equals(mTrim, "ムキ 縞板", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(zTrim, "SS400-SIMA", StringComparison.OrdinalIgnoreCase))
                    {
                        dt.CanhBaoCucBo.Add(new CanhBao
                        {
                            Khung = soKhung, Do = true, LoaiLoi = "MTZ",
                            LyDo = "Ghi chú bản vẽ có 'CPL' (thép tấm chống trượt) nhưng F-name đang ghi M=" + r.M + ", Z=" + r.Z + "; chuẩn CPL phải dùng M=ムキ 縞板 và Z=SS400-SIMA.",
                            GoiY = "M=ムキ 縞板, Z=SS400-SIMA"
                        });
                    }
                }

                if (ch.KiemTraBangVatLieu && !string.IsNullOrWhiteSpace(r.M))
                    KiemTraBangVatLieu(soKhung, r, dt, khoiVatLieu, ch);

                string gcChuan = ChuanHoaSoNhat(tatCaGhiChu);
                if (ch.KiemTraSoLuongQ && !string.IsNullOrWhiteSpace(r.Q) && int.TryParse(r.Q.Trim(), out int qFname) && ch.DonViSoLuong.Count > 0)
                {
                    string patDonVi = string.Join("|", ch.DonViSoLuong.Select(Regex.Escape));
                    MatchCollection ms = Regex.Matches(gcChuan, @"(\d+)\s*(" + patDonVi + @")");
                    if (ms.Count == 1 && int.TryParse(ms[0].Groups[1].Value, out int qNote) && qNote != qFname)
                    {
                        bool moHo = KiemTraMoHoSoLuong(tatCaGhiChu, ch.TuKhoaSoLuongMoHo);
                        dt.CanhBaoCucBo.Add(new CanhBao
                        {
                            Khung = soKhung, Do = !moHo, LoaiLoi = "Q",
                            LyDo = "F-name ghi Q=" + qFname + " nhưng ghi chú ghi '" + ms[0].Value + "'.",
                            GoiY = "Q=" + qNote
                        });
                    }
                }

                if (ch.KiemTraDoDayT && !string.IsNullOrWhiteSpace(r.T) &&
                    double.TryParse(r.T.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double tFname))
                {
                    Match mDay = RxDoDayGhiChu.Match(gcChuan);
                    if (mDay.Success &&
                        double.TryParse(mDay.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double tNote) &&
                        Math.Abs(tFname - tNote) > 1e-4)
                    {
                        dt.CanhBaoCucBo.Add(new CanhBao
                        {
                            Khung = soKhung, Do = true, LoaiLoi = "GHI_CHU",
                            LyDo = "F-name ghi T=" + r.T + " nhưng ghi chú ghi '" + mDay.Value.Trim() + "'.",
                            GoiY = "T=" + FnameCatalogData.FormatThickness(mDay.Groups[1].Value)
                        });
                    }
                }
            }

            if (ch.KiemTraBeHonHopPhatOshi)
                KiemTraBeHonHopPhatOshi(soKhung, k, dt, tatCaFname);

            if (ch.KiemTraMtextKhungIn && dsMtextKhungIn.Count > 0)
            {
                foreach (string mtext in dsMtextKhungIn)
                    KiemTraMotMtextKhungIn(soKhung, mtext, dt);
            }

            // Áp dụng các luật ràng buộc tùy chỉnh trong 02-luat-fname-vat-lieu.md & 03-luat-ghi-chu-ban-ve.md
            foreach (LuatTuyChinhKev luat in ch.DanhSachLuat)
            {
                if (luat.KhiGhiChuCo.Count == 0 && luat.KhiFnameCo.Count == 0)
                    continue;
                bool kichHoat = true;
                if (luat.KhiGhiChuCo.Count > 0 && !luat.KhiGhiChuCo.Any(tk => ChuaTuKhoa(tatCaGhiChu, tk)))
                    kichHoat = false;
                if (kichHoat && luat.KhiFnameCo.Count > 0 && !luat.KhiFnameCo.Any(tk => KhopDongFname(dt.DongFname, tatCaFname, tk, yeuCauCoGiaTri: true)))
                    kichHoat = false;
                if (!kichHoat) continue;

                bool thoaMan = true;
                if (luat.BatBuocFnameCo.Count > 0 && !luat.BatBuocFnameCo.Any(tk => KhopDongFname(dt.DongFname, tatCaFname, tk, yeuCauCoGiaTri: true)))
                    thoaMan = false;
                if (thoaMan && luat.BatBuocGhiChuCo.Count > 0 && !luat.BatBuocGhiChuCo.Any(tk => ChuaTuKhoa(tatCaGhiChu, tk)))
                    thoaMan = false;
                if (thoaMan && !string.IsNullOrWhiteSpace(luat.BatBuocHinhHoc))
                {
                    if (string.Equals(luat.BatBuocHinhHoc, "kegaki", StringComparison.OrdinalIgnoreCase) && dt.SoDuongKegaki == 0 && dt.SoDuongBeDashed == 0)
                        thoaMan = false;
                    else if (string.Equals(luat.BatBuocHinhHoc, "lo_kep_dong_tam", StringComparison.OrdinalIgnoreCase) && dt.SoCapDongTam == 0)
                        thoaMan = false;
                    else if (string.Equals(luat.BatBuocHinhHoc, "lo_kakou", StringComparison.OrdinalIgnoreCase) && dt.SoLoKakou == 0)
                        thoaMan = false;
                }

                if (!thoaMan)
                {
                    dt.CanhBaoCucBo.Add(new CanhBao
                    {
                        Khung = soKhung,
                        Do = luat.Do,
                        LoaiLoi = string.IsNullOrWhiteSpace(luat.LoaiLoi) ? "GHI_CHU" : luat.LoaiLoi,
                        LyDo = string.IsNullOrWhiteSpace(luat.LyDo) ? ("Vi phạm luật " + luat.Ma + ": " + luat.Ten) : luat.LyDo,
                        GoiY = luat.GoiY ?? "",
                    });
                }
            }
        }

        private static bool CoGhiChuCpl(string tatCaGhiChu)
        {
            if (string.IsNullOrEmpty(tatCaGhiChu)) return false;
            if (tatCaGhiChu.IndexOf("ＣＰＬ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                tatCaGhiChu.IndexOf("裏面CPL", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return Regex.IsMatch(tatCaGhiChu, @"(?<![A-Za-z])CPL(?![A-Za-z])", RegexOptions.IgnoreCase);
        }

        private static void KiemTraBeHonHopPhatOshi(int soKhung, KhungDuLieu k, DacTrungKhung dt, string tatCaFname)
        {
            bool coPhatOshi = tatCaFname.IndexOf("発押し", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!coPhatOshi) return;

            bool coBeThuong = KhopDongFname(dt.DongFname, tatCaFname, "V=", false) ||
                              KhopDongFname(dt.DongFname, tatCaFname, "V1=", false) ||
                              KhopDongFname(dt.DongFname, tatCaFname, "V2=", false) ||
                              KhopDongFname(dt.DongFname, tatCaFname, "V3=", false) ||
                              tatCaFname.IndexOf("シマ目", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!coBeThuong) return;

            // Lấy danh sách đường bẻ trong khung (ưu tiên 0-KAKOU DASHED*, nếu chưa chuyển layer thì lấy 3-KEGAKI)
            var duongBe = k.DoiTuong
                .Where(d => d.Loai == LoaiDoiTuong.Line &&
                            string.Equals(d.Layer, "0-KAKOU", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(d.KieuNet) &&
                            d.KieuNet.StartsWith("DASHED", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (duongBe.Count == 0)
            {
                duongBe = k.DoiTuong
                    .Where(d => d.Loai == LoaiDoiTuong.Line && string.Equals(d.Layer, "3-KEGAKI", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            if (duongBe.Count == 0) return;

            var chuGhiChu = k.DoiTuong
                .Where(d => d.Loai == LoaiDoiTuong.Text &&
                            !string.Equals(d.Layer, DongChu.LayerFname, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(d.Layer, "KHUNG_IN", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(d.Layer, "1-XY", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(d.Layer, LayerNhan, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Gom nhóm các đoạn thẳng đồng tuyến (cùng góc ±0.02 rad và khoảng cách vuông góc <= 1.0mm)
            var nhom = new List<List<DoiTuong>>();
            foreach (DoiTuong seg in duongBe)
            {
                double dx = seg.X2 - seg.X1, dy = seg.Y2 - seg.Y1;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-3) continue;
                bool daGom = false;
                foreach (var g in nhom)
                {
                    DoiTuong r0 = g[0];
                    if (DongTuyen(r0, seg))
                    {
                        g.Add(seg);
                        daGom = true;
                        break;
                    }
                }
                if (!daGom) nhom.Add(new List<DoiTuong> { seg });
            }

            bool coMauThuan = false;
            int soNhomThieu = 0;

            foreach (var g in nhom)
            {
                bool nhomCoPhatOshi = false;
                bool nhomCoThuong = false;
                foreach (DoiTuong seg in g)
                {
                    bool segPhatOshi = false;
                    bool segThuong = false;
                    foreach (DoiTuong txt in chuGhiChu)
                    {
                        if (!NamTrongHopQuetDuongBe(seg, txt.X1, txt.Y1, 50.0, 20.0))
                            continue;
                        string nd = txt.NoiDung ?? "";
                        if (nd.IndexOf("発押し", StringComparison.OrdinalIgnoreCase) >= 0)
                            segPhatOshi = true;
                        else if (nd.IndexOf("V=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 nd.IndexOf("シマ目", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 nd.IndexOf("曲げ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 nd.IndexOf("折り", StringComparison.OrdinalIgnoreCase) >= 0)
                            segThuong = true;
                    }
                    if (segPhatOshi && segThuong) coMauThuan = true;
                    if (segPhatOshi) nhomCoPhatOshi = true;
                    if (segThuong) nhomCoThuong = true;
                }
                if (nhomCoPhatOshi && nhomCoThuong) coMauThuan = true;
                if (!nhomCoPhatOshi && !nhomCoThuong) soNhomThieu++;
            }

            if (coMauThuan)
            {
                dt.CanhBaoCucBo.Add(new CanhBao
                {
                    Khung = soKhung, Do = true, LoaiLoi = "GHI_CHU",
                    LyDo = "Tấm bẻ hỗn hợp (V= + 発押し): có nhóm đường bẻ vừa gần chữ '発押し' vừa gần chữ bẻ thường trong phạm vi ±50mm (Lisp CSV sẽ báo lỗi mâu thuẫn).",
                    GoiY = "Tách rõ vị trí chú thích bẻ trên từng đường bẻ"
                });
            }
            else if (soNhomThieu > 0)
            {
                dt.CanhBaoCucBo.Add(new CanhBao
                {
                    Khung = soKhung, Do = false, LoaiLoi = "GHI_CHU",
                    LyDo = "Tấm bẻ hỗn hợp (V= + 発押し): có " + soNhomThieu + " nhóm đường bẻ thiếu chữ chú thích ký hiệu bẻ trong phạm vi ±50mm để Lisp CSV phân biệt đường nhấn 1 lần và bẻ thường.",
                    GoiY = "Đặt chữ V=... hoặc 発押し gần từng đường bẻ (±50mm)"
                });
            }
        }

        private static bool DongTuyen(DoiTuong a, DoiTuong b)
        {
            double dxA = a.X2 - a.X1, dyA = a.Y2 - a.Y1;
            double lenA = Math.Sqrt(dxA * dxA + dyA * dyA);
            double dxB = b.X2 - b.X1, dyB = b.Y2 - b.Y1;
            double lenB = Math.Sqrt(dxB * dxB + dyB * dyB);
            if (lenA < 1e-3 || lenB < 1e-3) return false;
            double ux = dxA / lenA, uy = dyA / lenA;
            double vx = dxB / lenB, vy = dyB / lenB;
            double cross = Math.Abs(ux * vy - uy * vx);
            if (cross > 0.02) return false;
            double distPerp = Math.Abs((b.X1 - a.X1) * (-uy) + (b.Y1 - a.Y1) * ux);
            return distPerp <= 1.0;
        }

        private static bool NamTrongHopQuetDuongBe(DoiTuong seg, double px, double py, double halfWidthPerp, double marginAlong)
        {
            double dx = seg.X2 - seg.X1, dy = seg.Y2 - seg.Y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-3) return false;
            double ux = dx / len, uy = dy / len;
            double relX = px - seg.X1, relY = py - seg.Y1;
            double along = relX * ux + relY * uy;
            double perp = Math.Abs(relX * (-uy) + relY * ux);
            return perp <= halfWidthPerp && along >= -marginAlong && along <= len + marginAlong;
        }

        private static void KiemTraMotMtextKhungIn(int soKhung, string mtext, DacTrungKhung dt)
        {
            List<string> lines = FnameParser.SplitAndCleanMText(mtext)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
            if (lines.Count == 0) return;

            bool hasM = false, hasPitchi = false, hasK = false, hasR1 = false, hasR2 = false;
            double vatCount = 0, pitchiCount = 0, pitchiLen = 0;
            int maxBendIdx = 0;

            foreach (string line in lines)
            {
                if (line.StartsWith("Các loại gia công:", StringComparison.OrdinalIgnoreCase))
                {
                    string val = line.Substring("Các loại gia công:".Length).Trim();
                    foreach (string tk in val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string m = tk.Trim();
                        if (string.Equals(m, "m", StringComparison.OrdinalIgnoreCase)) hasM = true;
                        else if (string.Equals(m, "m(pitchi)", StringComparison.OrdinalIgnoreCase)) { hasM = true; hasPitchi = true; }
                        else if (string.Equals(m, "k", StringComparison.OrdinalIgnoreCase)) hasK = true;
                        else if (string.Equals(m, "r1", StringComparison.OrdinalIgnoreCase)) hasR1 = true;
                        else if (string.Equals(m, "r2", StringComparison.OrdinalIgnoreCase)) hasR2 = true;
                    }
                }
                else if (line.StartsWith("Chiều dài vát:", StringComparison.OrdinalIgnoreCase))
                {
                    vatCount = DocSoCuoiDong(line);
                }
                else if (line.StartsWith("Pitchi - Số lần:", StringComparison.OrdinalIgnoreCase))
                {
                    pitchiCount = DocSoCuoiDong(line);
                }
                else if (line.StartsWith("Pitchi - Chiều dài", StringComparison.OrdinalIgnoreCase))
                {
                    pitchiLen = DocSoCuoiDong(line);
                }
                else
                {
                    Match mBend = RxDongBeKhungIn.Match(line);
                    if (mBend.Success && int.TryParse(mBend.Groups[1].Value, out int idx) && idx > maxBendIdx)
                        maxBendIdx = idx;
                }
            }

            int expectedCount = 12 + (hasM ? maxBendIdx : 0);
            if (lines.Count != expectedCount)
            {
                dt.CanhBaoCucBo.Add(new CanhBao
                {
                    Khung = soKhung, Do = true, LoaiLoi = "KHAC",
                    LyDo = "MText thông tin tấm trên layer KHUNG_IN có " + lines.Count + " dòng (chuẩn Bước 1 của Lisp CSV cần " + expectedCount + " dòng; có thể do xuất đè 2 lần hoặc MText cũ).",
                    GoiY = "Xóa MText KHUNG_IN cũ và chạy lại Bước 1"
                });
            }

            if ((hasK || dt.CoVat) && vatCount <= 0)
            {
                dt.CanhBaoCucBo.Add(new CanhBao
                {
                    Khung = soKhung, Do = true, LoaiLoi = "GHI_CHU",
                    LyDo = "MText trên layer KHUNG_IN có gia công vát (k / 開先施工範囲) nhưng 'Chiều dài vát: 0' — chưa nhập chiều dài vát trước khi xuất CSV Bước 2.",
                    GoiY = "Nhập Chiều dài vát > 0"
                });
            }

            if (hasPitchi && (pitchiCount <= 0 || pitchiLen <= 0))
            {
                dt.CanhBaoCucBo.Add(new CanhBao
                {
                    Khung = soKhung, Do = true, LoaiLoi = "GHI_CHU",
                    LyDo = "MText trên layer KHUNG_IN có gia công bẻ bước m(pitchi) nhưng 'Pitchi - Số lần' hoặc 'Pitchi - Chiều dài (mm)' đang bằng 0.",
                    GoiY = "Nhập Số lần & Chiều dài Pitchi > 0"
                });
            }

            if (dt.CoCuon && !hasR1 && !hasR2)
            {
                dt.CanhBaoCucBo.Add(new CanhBao
                {
                    Khung = soKhung, Do = false, LoaiLoi = "GHI_CHU",
                    LyDo = "Tấm có dấu hiệu gia công cuộn nhưng dòng 'Các loại gia công:' trên MText KHUNG_IN chưa điền mã cuộn r1 (cung tròn) hoặc r2 (ống tròn).",
                    GoiY = "Thêm r1 hoặc r2 vào Các loại gia công"
                });
            }

            if (maxBendIdx > 8)
            {
                dt.CanhBaoCucBo.Add(new CanhBao
                {
                    Khung = soKhung, Do = false, LoaiLoi = "KHAC",
                    LyDo = "Tấm có " + maxBendIdx + " loại đường bẻ (> 8 cột Bẻ1..Bẻ8 tối đa của file CSV); từ loại thứ 9 sẽ bị bỏ qua khi xuất CSV.",
                    GoiY = "Kiểm tra gộp các đường bẻ cùng chiều dài"
                });
            }
        }

        private static double DocSoCuoiDong(string line)
        {
            int idx = line.LastIndexOf(':');
            string tail = idx >= 0 ? line.Substring(idx + 1).Trim() : line.Trim();
            return double.TryParse(tail, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0.0;
        }

        /// <summary>
        /// Kiểm tra liên khung trên toàn bộ danh sách khung (trùng tên N= giữa các khung,
        /// kiểm tra thông minh dim tổng 1-XY và HATCH TAPU_TMT khi bản vẽ đang chuẩn bị xuất CSV).
        /// </summary>
        public static List<(KhungDuLieu Khung, CanhBao CB)> KiemTraLienKhung(
            IList<KhungDuLieu> dsKhung,
            string khoiVatLieu,
            CauHinhKev cauHinh = null)
        {
            var kq = new List<(KhungDuLieu Khung, CanhBao CB)>();
            if (dsKhung == null || dsKhung.Count == 0) return kq;

            CauHinhKev ch = cauHinh ?? CauHinhKev.DocTuBoNhoMacDinh();
            var dsPhanTich = dsKhung.Select(k => (Khung: k, Dt: PhanTich(k, khoiVatLieu, ch))).ToList();

            if (ch.KiemTraTrungTenN)
            {
                var nhomTheoN = dsPhanTich
                    .Where(x => x.Dt.Fname != null && !string.IsNullOrWhiteSpace(x.Dt.Fname.N))
                    .GroupBy(x => x.Dt.Fname.N.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1);

                foreach (var g in nhomTheoN)
                {
                    string dsSoKhung = string.Join(", ", g.Select(x => "#" + x.Khung.SoThuTu));
                    foreach (var item in g)
                    {
                        kq.Add((item.Khung, new CanhBao
                        {
                            Khung = item.Khung.SoThuTu,
                            Do = true,
                            LoaiLoi = "KHAC",
                            LyDo = "Trùng tên tấm N=" + g.Key + " tại các khung " + dsSoKhung + " (Lisp CSV sẽ dừng khi có F-name trùng lặp).",
                            GoiY = "Đổi tên N= cho duy nhất"
                        }));
                    }
                }
            }

            bool batDim1Xy = string.Equals(ch.KiemTraDimTong1Xy, "true", StringComparison.OrdinalIgnoreCase) ||
                             (string.Equals(ch.KiemTraDimTong1Xy, "auto", StringComparison.OrdinalIgnoreCase) &&
                              dsPhanTich.Any(x => x.Dt.SoDim1Xy > 0 || x.Dt.CoMtextKhungIn));
            if (batDim1Xy)
            {
                foreach (var item in dsPhanTich)
                {
                    if (item.Dt.DongFname.Count == 0) continue;
                    if (item.Dt.SoDim1Xy == 0 || item.Dt.SoDim1Xy > 2)
                    {
                        kq.Add((item.Khung, new CanhBao
                        {
                            Khung = item.Khung.SoThuTu,
                            Do = true,
                            LoaiLoi = "KHAC",
                            LyDo = "Khung có " + item.Dt.SoDim1Xy + " kích thước trên layer 1-XY (chuẩn Lisp CSV yêu cầu 1 hoặc 2 dim tổng X, Y trên layer 1-XY).",
                            GoiY = "Đặt 1-2 dim tổng trên layer 1-XY"
                        }));
                    }
                }
            }

            bool batHatchTapu = string.Equals(ch.KiemTraHatchTapuTmt, "true", StringComparison.OrdinalIgnoreCase) ||
                                (string.Equals(ch.KiemTraHatchTapuTmt, "auto", StringComparison.OrdinalIgnoreCase) &&
                                 dsPhanTich.Any(x => x.Dt.SoHatchTapu > 0));
            if (batHatchTapu)
            {
                foreach (var item in dsPhanTich)
                {
                    if (item.Dt.DongFname.Count == 0) continue;
                    if ((item.Dt.CoTapu || item.Dt.CoSara) && item.Dt.SoHatchTapu == 0)
                    {
                        kq.Add((item.Khung, new CanhBao
                        {
                            Khung = item.Khung.SoThuTu,
                            Do = false,
                            LoaiLoi = "GHI_CHU",
                            LyDo = "Khung có gia công Tapu/Sara nhưng chưa có HATCH đánh dấu trên layer TAPU_TMT (Lisp CSV đếm số lỗ qua HATCH TAPU_TMT).",
                            GoiY = "Tạo HATCH trên layer TAPU_TMT"
                        }));
                    }
                }
            }

            return kq;
        }

        private static bool KiemTraMoHoSoLuong(string tatCaGhiChu, List<string> dsMoHo)
        {
            foreach (string mh in dsMoHo)
            {
                if (mh.Contains("+"))
                {
                    string[] parts = mh.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && parts.All(p => ChuaTuKhoa(tatCaGhiChu, p.Trim())))
                        return true;
                }
                else if (ChuaTuKhoa(tatCaGhiChu, mh))
                {
                    return true;
                }
            }
            return false;
        }

        private static void KiemTraBangVatLieu(int soKhung, FnameRecord r, DacTrungKhung dt, string khoiVatLieu, CauHinhKev ch)
        {
            if (string.IsNullOrWhiteSpace(khoiVatLieu)) return;
            List<string> dongFname = dt.DongFname;
            List<CanhBao> dsCanhBao = dt.CanhBaoCucBo;

            var chuan = new Dictionary<string, (string Z, List<string> Ts)>(StringComparer.OrdinalIgnoreCase);
            var chuaDuyet = new Dictionary<string, (string Z, List<string> Ts)>(StringComparer.OrdinalIgnoreCase);
            bool trongChuaDuyet = false;

            foreach (string rawLine in khoiVatLieu.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("##", StringComparison.Ordinal))
                {
                    trongChuaDuyet = line.IndexOf("CHUA DUYET", StringComparison.OrdinalIgnoreCase) >= 0;
                    continue;
                }
                string[] parts = line.Split(',');
                if (parts.Length < 4) continue;
                string mKey = parts[0].Trim();
                string zVal = parts[2].Trim();
                var ts = parts[3].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (trongChuaDuyet) chuaDuyet[mKey] = (zVal, ts);
                else chuan[mKey] = (zVal, ts);
            }

            if (chuan.Count == 0 && chuaDuyet.Count == 0) return;

            string m = r.M.Trim();
            if (chuan.TryGetValue(m, out var info))
            {
                dt.ZChuanTheoBang = info.Z;
                dt.DsDayChuanTheoBang = string.Join(" ", info.Ts);
                if (!string.IsNullOrWhiteSpace(r.Z) && !string.Equals(r.Z.Trim(), info.Z, StringComparison.OrdinalIgnoreCase))
                {
                    dsCanhBao.Add(new CanhBao
                    {
                        Khung = soKhung, Do = true, LoaiLoi = "MTZ",
                        LyDo = "M=" + r.M + " nhưng Z=" + r.Z + "; bảng vật liệu chuẩn ghi Z=" + info.Z + ".",
                        GoiY = "Z=" + info.Z
                    });
                }
                if (!string.IsNullOrWhiteSpace(r.T) && info.Ts.Count > 0 && !FnameCatalogData.ContainsThickness(info.Ts, r.T.Trim()))
                {
                    dsCanhBao.Add(new CanhBao
                    {
                        Khung = soKhung, Do = true, LoaiLoi = "MTZ",
                        LyDo = "T=" + r.T + " không nằm trong danh sách độ dày chuẩn của " + r.M + ".",
                        GoiY = "T=" + string.Join(" / ", info.Ts)
                    });
                }
            }
            else if (chuaDuyet.TryGetValue(m, out var infoCho))
            {
                dt.ZChuanTheoBang = infoCho.Z;
                dt.DsDayChuanTheoBang = string.Join(" ", infoCho.Ts);
                dsCanhBao.Add(new CanhBao
                {
                    Khung = soKhung, Do = false, LoaiLoi = "MTZ",
                    LyDo = "Vật liệu M=" + r.M + " đang nằm trong danh sách chờ duyệt.",
                    GoiY = ""
                });
            }
            else
            {
                dsCanhBao.Add(new CanhBao
                {
                    Khung = soKhung, Do = false, LoaiLoi = "MTZ",
                    LyDo = "Vật liệu M=" + r.M + " chưa có trong bảng vật liệu.",
                    GoiY = ""
                });
            }

            if (ch.KiemTraSima)
            {
                bool laSima = (!string.IsNullOrWhiteSpace(r.Z) && r.Z.Trim().EndsWith(ch.HauToZSima, StringComparison.OrdinalIgnoreCase)) ||
                              (!string.IsNullOrWhiteSpace(ch.TuKhoaMSima) && m.Contains(ch.TuKhoaMSima));
                if (laSima && !dongFname.Any(s => s.Contains(ch.DongBatBuocSima)))
                {
                    dsCanhBao.Add(new CanhBao
                    {
                        Khung = soKhung, Do = false, LoaiLoi = "KHAC",
                        LyDo = "Vật liệu nhám (" + ch.TuKhoaMSima + " / " + ch.HauToZSima + ") nhưng F-name thiếu dòng '" + ch.DongBatBuocSima + "' dưới Z=.",
                        GoiY = "Thêm " + ch.DongBatBuocSima
                    });
                }
            }
        }

        private static string ChuanHoaSoNhat(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '０' && chars[i] <= '９')
                    chars[i] = (char)('0' + (chars[i] - '０'));
            }
            return new string(chars);
        }

        public static string GopNhan(bool be, bool vat, bool cuon, bool sara, bool tapu)
        {
            var ds = new List<string>(5);
            if (be) ds.Add("BE");
            if (vat) ds.Add("VAT");
            if (cuon) ds.Add("CUON");
            if (sara) ds.Add("SARA");
            if (tapu) ds.Add("TAPU");
            return ds.Count > 0 ? string.Join(" - ", ds) : "PHANG";
        }

        public static string GopNhanTheoCauHinh(DacTrungKhung dt, CauHinhKev ch, JsonObject answers = null)
        {
            CauHinhKev cfg = ch ?? CauHinhKev.DocTuBoNhoMacDinh();
            var ds = new List<string>();
            foreach (LoaiGiaCongKev loai in cfg.DanhSachLoai)
            {
                bool khop = dt.KhopLoai(loai.Ma);
                if (!khop && answers != null)
                {
                    double p = XacSuatNoul(answers, loai.Ma.ToLowerInvariant());
                    if (p >= cfg.NguongPhanLoai) khop = true;
                }
                if (khop) ds.Add(loai.Ma);
            }
            return ds.Count > 0 ? string.Join(" - ", ds) : cfg.NhanMacDinh;
        }

        public static string TaoBody(KhungDuLieu k, string khoiVatLieu, CauHinhKev cauHinh = null)
        {
            CauHinhKev ch = cauHinh ?? CauHinhKev.DocTuBoNhoMacDinh();
            DacTrungKhung dt = PhanTich(k, khoiVatLieu, ch);

            var state = new JsonObject();
            state.Set("frame_id", new JsonNumber(k.SoThuTu.ToString(CultureInfo.InvariantCulture)));
            state.Set("fname_raw", string.Join(" | ", dt.DongFname));
            if (dt.Fname != null)
            {
                state.Set("material_M", dt.Fname.M ?? "");
                state.Set("thickness_T", dt.Fname.T ?? "");
                state.Set("quantity_Q", dt.Fname.Q ?? "");
                state.Set("stock_code_Z", dt.Fname.Z ?? "");
                if (!string.IsNullOrEmpty(dt.ZChuanTheoBang))
                    state.Set("catalog_expected_Z", dt.ZChuanTheoBang);
                if (!string.IsNullOrEmpty(dt.DsDayChuanTheoBang))
                    state.Set("catalog_allowed_T", dt.DsDayChuanTheoBang);
                state.Set("bending_BE", string.Join(" ; ", dt.Fname.BendLines));
            }
            state.Set("drawing_notes", string.Join(" | ", dt.GhiChu));

            var hints = new List<string>();
            foreach (LoaiGiaCongKev loai in ch.DanhSachLoai)
            {
                if (dt.KhopLoai(loai.Ma))
                {
                    hints.Add(!string.IsNullOrWhiteSpace(loai.MoTaTiengAnh)
                        ? loai.MoTaTiengAnh
                        : ("Requires " + loai.Ma + " (" + loai.Ten + ")"));
                }
            }
            state.Set("interpreted_notes", hints.Count > 0 ? string.Join("; ", hints) : "Standard flat laser cutting only");

            var geom = new JsonObject();
            geom.Set("kegaki_lines_count", new JsonNumber(dt.SoDuongKegaki.ToString(CultureInfo.InvariantCulture)));
            geom.Set("dashed_bend_lines_count", new JsonNumber(dt.SoDuongBeDashed.ToString(CultureInfo.InvariantCulture)));
            geom.Set("has_multiple_parallel_pitch_lines", dt.CoPichiCuon);
            geom.Set("concentric_double_circles_count", new JsonNumber(dt.SoCapDongTam.ToString(CultureInfo.InvariantCulture)));
            geom.Set("tapu_hatch_count", new JsonNumber(dt.SoHatchTapu.ToString(CultureInfo.InvariantCulture)));
            geom.Set("cut_holes_count", new JsonNumber(dt.SoLoKakou.ToString(CultureInfo.InvariantCulture)));
            state.Set("geometry_features", geom);

            var checks = new JsonObject();
            checks.Set("rule_errors_count", new JsonNumber(dt.CanhBaoCucBo.Count.ToString(CultureInfo.InvariantCulture)));
            checks.Set("rule_findings", dt.CanhBaoCucBo.Count > 0
                ? "ERROR: " + string.Join(" | ", dt.CanhBaoCucBo.Select(c => c.LyDo))
                : "Valid");
            state.Set("validation_checks", checks);

            var questions = new JsonObject();
            foreach (LoaiGiaCongKev loai in ch.DanhSachLoai)
            {
                string key = loai.Ma.ToLowerInvariant();
                string instr = !string.IsNullOrWhiteSpace(loai.CauHoiKev)
                    ? loai.CauHoiKev
                    : ("Does this part require " + loai.Ma + " (" + loai.Ten + ")?");
                questions.Set(key, CauHoiNoul(instr, "No " + loai.Ma, "Requires " + loai.Ma));
            }
            questions.Set("nghi_loi", CauHoiNoul(
                "Does validation_checks report any ERROR, or is there a mismatch between drawing_notes and F-name?",
                "No error, part F-name is valid", "Has ERROR or mismatch requiring review"));

            var root = new JsonObject();
            root.Set("state", state);
            root.Set("model", "kev-latest");
            root.Set("questions", questions);
            return JsonLite.Serialize(root);
        }

        private static JsonObject CauHoiNoul(string instr, string noDesc, string yesDesc)
        {
            var crit = new JsonObject();
            crit.Set("false", noDesc);
            crit.Set("true", yesDesc);
            var q = new JsonObject();
            q.Set("type", "noul");
            q.Set("instructions", instr);
            q.Set("criteria", crit);
            return q;
        }

        public static KetQuaKhungKev DocPhanHoi(string json, KhungDuLieu k, string khoiVatLieu, CauHinhKev cauHinh = null)
        {
            CauHinhKev ch = cauHinh ?? CauHinhKev.DocTuBoNhoMacDinh();
            DacTrungKhung dt = PhanTich(k, khoiVatLieu, ch);
            var kq = new KetQuaKhungKev { Khung = k?.SoThuTu ?? 0 };

            JsonObject goc;
            try { goc = JsonLite.Parse(json) as JsonObject; }
            catch (Exception) { goc = null; }

            if (goc == null)
            {
                kq.Loi = "Phản hồi Kev Server không đọc được";
                kq.NhanGiaCong = GopNhanTheoCauHinh(dt, ch);
                kq.CanhBao.AddRange(dt.CanhBaoCucBo);
                kq.NghiLoi = kq.CanhBao.Count > 0;
                return kq;
            }

            if (goc.GetString("detail") != null)
            {
                kq.Loi = "Kev Server báo lỗi: " + goc.GetString("detail");
                kq.NhanGiaCong = GopNhanTheoCauHinh(dt, ch);
                kq.CanhBao.AddRange(dt.CanhBaoCucBo);
                kq.NghiLoi = kq.CanhBao.Count > 0;
                return kq;
            }

            kq.LatencyMs = SoThuc(goc, "latency_ms");
            if (goc.Get("usage") is JsonObject u)
            {
                kq.TokenVao = (long)SoThuc(u, "input_tokens");
                kq.TokenRa = (long)SoThuc(u, "output_tokens");
            }

            var ans = goc.Get("answers") as JsonObject;
            if (ans == null)
            {
                kq.Loi = "Phản hồi Kev Server thiếu trường answers";
                kq.NhanGiaCong = GopNhanTheoCauHinh(dt, ch);
                kq.CanhBao.AddRange(dt.CanhBaoCucBo);
                kq.NghiLoi = kq.CanhBao.Count > 0;
                return kq;
            }

            double pLoi = XacSuatNoul(ans, "nghi_loi");
            kq.NhanGiaCong = GopNhanTheoCauHinh(dt, ch, ans);
            kq.XacSuatLoi = pLoi;
            kq.CanhBao.AddRange(dt.CanhBaoCucBo);

            if (pLoi >= ch.NguongNghiLoi && kq.CanhBao.Count == 0)
            {
                kq.CanhBao.Add(new CanhBao
                {
                    Khung = k?.SoThuTu ?? 0,
                    Do = pLoi >= 0.85,
                    LoaiLoi = "GHI_CHU",
                    LyDo = string.Format(CultureInfo.InvariantCulture,
                        "Kev phát hiện dấu hiệu nghi ngờ trong khung (xác suất {0:0}%). Nhãn phân loại: {1}.",
                        pLoi * 100.0, kq.NhanGiaCong),
                    GoiY = ""
                });
            }

            kq.NghiLoi = pLoi >= ch.NguongNghiLoi || kq.CanhBao.Count > 0;
            return kq;
        }

        private static double XacSuatNoul(JsonObject answers, string key)
        {
            if (answers?.Get(key) is JsonObject q)
                return SoThuc(q, "noul");
            return 0.0;
        }

        private static double SoThuc(JsonObject o, string key)
        {
            object v = o?.Get(key);
            string raw = v is JsonNumber n ? n.Raw : v?.ToString();
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0.0;
        }
    }
}
