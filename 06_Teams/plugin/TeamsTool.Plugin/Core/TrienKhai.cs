using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một chỗ uốn trên tiết diện. goc = góc trong như trên bản vẽ (90 = vuông); cung = góc cung khi cuộn (360 = tròn kín).</summary>
    public sealed class Uon
    {
        public string Kieu = "be"; // be | an_1_lan | cuon | pitchi
        public double Goc = 90, R, Cung;
    }

    /// <summary>
    /// Tiết diện theo một hướng: các đoạn thẳng nối nhau bằng chỗ uốn (Doan.Count = Uon.Count + 1).
    /// be / an_1_lan: đoạn = kích thước NGOÀI tới góc ảo; cuon: đoạn = phần thẳng tiếp tuyến (0 nếu không có).
    /// Không uốn: một đoạn = chiều dài phẳng.
    /// </summary>
    public sealed class TietDien
    {
        public List<double> Doan = new List<double>();
        public List<Uon> Uon = new List<Uon>();
    }

    /// <summary>Một ứng viên V của bảng TV cho một góc (từ TraVCalculator.GetBendOptionsForFname).</summary>
    public sealed class UngVienV
    {
        public string V = "", Chu = "", CanhBao = "", Nguon = "";
        public double Delta;
    }

    /// <summary>Phòng 4: dữ liệu một tấm (AI đọc tiết diện + V người chọn) và kết quả tính.</summary>
    public sealed class TamTrienKhai
    {
        public string Khoa = "", Ten = "", Kieu = "phang", GhiChu = "", VChon = "";
        /// <summary>Mẫu biên dạng AI chọn trong thư viện ve_dxf (phang / be / hop / cuon… hoặc chua_co); "" = tự suy.</summary>
        public string BienDang = "", MoTaBienDang = "";
        public TietDien X = new TietDien(), Y = new TietDien();
        /// <summary>Dòng F-name của chỗ uốn: chữ V= của TraV (như tab Fname), ấn 1 lần 内R…-発押し…°.</summary>
        public List<string> DongV = new List<string>();
        // Kết quả (tính lại mỗi lần vẽ, không lưu)
        public double? XPhang, YPhang;
        public bool LamTay;
        public List<string> ChiTiet = new List<string>(), CanhBao = new List<string>();
        public List<string> CacV = new List<string>();
        public string KyHieu = "";
        /// <summary>Vị trí đường bẻ trên tấm phẳng (mm từ mép trái / mép dưới) và các đoạn thẳng phẳng — để vẽ DXF.</summary>
        public List<double> BeX = new List<double>(), BeY = new List<double>(), DoanPhangX = new List<double>(), DoanPhangY = new List<double>();
    }

    /// <summary>Kết quả phòng 4, lưu &lt;thư mục đơn&gt;\ai\trien_khai.json (+ trien_khai.csv).</summary>
    public sealed class KetQuaTrienKhai
    {
        public List<TamTrienKhai> Tam = new List<TamTrienKhai>();
        /// <summary>Lúc xác nhận phòng 3 mà lần triển khai này dùng — khác = phòng 3 đã đổi, chạy lại.</summary>
        public string TuXacNhan = "", Luc = "";
        public long TokenVao, TokenRa;
        public decimal Tien;
    }

    /// <summary>
    /// Phòng 4 — Triển khai (spec 2026-11-11): trải phẳng theo QC B-2 / B-2-4 / C-2 và bảng TV (δ do TraV tính như tab Fname).
    /// AI chỉ đọc tiết diện; mọi con số tính ở đây (thuần, có test).
    /// </summary>
    public static class PhongTrienKhai
    {
        /// <summary>Đọc "V=32-4(90°)" / "シマ目外 V32引代 4 (90°)" / "V12x-1.9(60°)" → δ. Không đọc được → null.</summary>
        public static double? DeltaTuChu(string chu)
        {
            var m = System.Text.RegularExpressions.Regex.Match(chu ?? "", @"(?:-|引代\s*)(\d+(?:\.\d+)?)\s*\(");
            return m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : (double?)null;
        }

        /// <summary>
        /// Trải phẳng một tấm. traV(góc) = các ứng viên V của M, T ở góc đó (thứ tự TraV). tThuc = T thực (ấn 1 lần, cuộn).
        /// lmin(V) = Lmin của V (null = không biết) — để bỏ V có Lmin dài hơn cạnh ngắn nhất khi tự chọn.
        /// </summary>
        public static void Tinh(TamTrienKhai tk, TamAi tam, Func<double, List<UngVienV>> traV, double tThuc, Func<string, double?> lmin = null)
        {
            tk.ChiTiet.Clear(); tk.CanhBao.Clear(); tk.CacV.Clear();
            tk.BeX.Clear(); tk.BeY.Clear(); tk.DoanPhangX.Clear(); tk.DoanPhangY.Clear(); tk.DongV.Clear();
            tk.XPhang = tk.YPhang = null;
            tk.LamTay = tk.Kieu == "lam_tay";
            double T = PhongKiemTra.So(tam.T) ?? 0;

            var tatCaUon = tk.X.Uon.Concat(tk.Y.Uon).ToList();
            if (tatCaUon.Any(u => u.Kieu == "pitchi")) tk.LamTay = true;
            if (tk.LamTay)
            {
                tk.ChiTiet.Add("Làm tay (pitchi / kakumaru / ống co / hình phức tạp) — triển khai trên SolidWorks theo QC B-3, B-4, C-3.");
                tk.KyHieu = KyHieu(tam, tatCaUon);
                return;
            }

            // V của tấm: người đã chọn, không thì ứng viên đầu ở 90° có Lmin ≤ đoạn ngắn nhất cạnh chỗ bẻ
            var be = tatCaUon.Where(u => u.Kieu == "be").ToList();
            if (be.Count > 0)
            {
                List<UngVienV> uv90 = traV(be[0].Goc);
                tk.CacV.AddRange(uv90.Select(u => u.V).Distinct());
                if (tk.VChon.Length == 0 || !tk.CacV.Contains(tk.VChon))
                {
                    double ngan = DoanCanhBe(tk.X).Concat(DoanCanhBe(tk.Y)).DefaultIfEmpty(double.MaxValue).Min();
                    UngVienV hop = uv90.FirstOrDefault(u => !(lmin?.Invoke(u.V) is double l) || l <= ngan) ?? uv90.FirstOrDefault();
                    tk.VChon = hop?.V ?? "";
                }
            }

            tk.XPhang = Trai(tk.X, "X", tk, traV, T, tThuc, tk.BeX, tk.DoanPhangX);
            tk.YPhang = Trai(tk.Y, "Y", tk, traV, T, tThuc, tk.BeY, tk.DoanPhangY);
            if (tk.X.Doan.Count == 0) tk.CanhBao.Add("Chưa có tiết diện hướng X.");
            if (tk.Y.Doan.Count == 0) tk.CanhBao.Add("Chưa có tiết diện hướng Y.");
            tk.KyHieu = KyHieu(tam, tatCaUon);
        }

        /// <summary>Các đoạn kề một chỗ bẻ thường (để so Lmin).</summary>
        private static IEnumerable<double> DoanCanhBe(TietDien td)
        {
            for (int i = 0; i < td.Uon.Count && i + 1 < td.Doan.Count; i++)
                if (td.Uon[i].Kieu == "be") { yield return td.Doan[i]; yield return td.Doan[i + 1]; }
        }

        /// <summary>Chiều dài trải của một tiết diện; ghi từng chỗ uốn vào ChiTiet. Không tính được → null.</summary>
        private static double? Trai(TietDien td, string huong, TamTrienKhai tk, Func<double, List<UngVienV>> traV, double T, double tThuc,
                                    List<double> viTriBe, List<double> doanPhang)
        {
            if (td.Doan.Count == 0) return null;
            if (td.Doan.Count != td.Uon.Count + 1) { tk.CanhBao.Add("Tiết diện " + huong + ": số đoạn phải = số chỗ uốn + 1."); return null; }
            double tong = td.Doan.Sum();
            var dong = new StringBuilder(huong + ": " + string.Join(" + ", td.Doan.Select(F)));
            // mỗi chỗ uốn: bớt đoạn trước, bề rộng vùng uốn (đường bẻ ở giữa), bớt đoạn sau — để đặt đường bẻ trên tấm phẳng
            var cho = new List<(double Truoc, double Vung, double Sau)>();
            for (int i = 0; i < td.Uon.Count; i++)
            {
                Uon u = td.Uon[i];
                switch (u.Kieu)
                {
                    case "be":
                    {
                        List<UngVienV> uv = traV(u.Goc);
                        UngVienV chon = uv.FirstOrDefault(x => x.V == tk.VChon) ?? uv.FirstOrDefault();
                        if (chon == null) { tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": bảng TV không có góc " + F(u.Goc) + "° — tra tay / bẻ thử."); return null; }
                        tong -= 2 * chon.Delta;
                        cho.Add((chon.Delta, 0, chon.Delta));
                        string dv = chon.Chu.Length > 0 ? chon.Chu : "V=" + chon.V.TrimStart('V') + "-" + F(chon.Delta) + "(" + F(u.Goc) + "°)";
                        if (!tk.DongV.Contains(dv)) tk.DongV.Add(dv);
                        dong.Append(" | " + F(u.Goc) + "° " + chon.V + " δ" + F(chon.Delta) + " → −" + F(2 * chon.Delta) + (chon.Nguon.Length > 0 ? " (" + chon.Nguon + ")" : ""));
                        if (chon.CanhBao.Length > 0) tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": " + chon.CanhBao);
                        if (chon.V != tk.VChon && tk.VChon.Length > 0) tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": góc " + F(u.Goc) + "° không có " + tk.VChon + ", dùng " + chon.V + ".");
                        break;
                    }
                    case "an_1_lan":
                    {
                        // QC B-2-4: đường trung bình O = 0.45 × T thực; bớt 2 đoạn lùi (R + T)·tan(θ/2), cộng cung (R + O)·θ
                        double theta = 180 - u.Goc, rad = theta * Math.PI / 180, lui = (u.R + T) * Math.Tan(rad / 2), cung = (u.R + 0.45 * tThuc) * rad;
                        tong += cung - 2 * lui;
                        cho.Add((lui, cung, lui));
                        string an = "内R" + F(u.R) + "-発押し" + F(u.Goc) + "°"; // như TraVCalculator.FormatPressOneTimeLine
                        if (!tk.DongV.Contains(an)) tk.DongV.Add(an);
                        dong.Append(" | ấn 1 lần R" + F(u.R) + " " + F(u.Goc) + "°: −2×" + F(lui) + " + cung " + F(cung));
                        break;
                    }
                    case "cuon":
                    {
                        // QC C-2: đường trung bình 0.5 × T thực; đoạn kề là phần thẳng nên không bớt
                        double cung = (u.R + 0.5 * tThuc) * u.Cung * Math.PI / 180;
                        if (u.Cung <= 0) { tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": cuộn chưa có góc cung."); return null; }
                        tong += cung;
                        cho.Add((0, cung, 0));
                        dong.Append(" | cuộn R" + F(u.R) + " cung " + F(u.Cung) + "°: + " + F(cung));
                        break;
                    }
                    default:
                        tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": kiểu uốn '" + u.Kieu + "' chưa tính được.");
                        return null;
                }
            }
            double viTri = 0;
            for (int k = 0; k < td.Doan.Count; k++)
            {
                double doan = td.Doan[k] - (k > 0 ? cho[k - 1].Sau : 0) - (k < cho.Count ? cho[k].Truoc : 0);
                doanPhang.Add(Math.Round(doan, 2));
                viTri += doan;
                if (k < cho.Count) { viTriBe.Add(Math.Round(viTri + cho[k].Vung / 2, 2)); viTri += cho[k].Vung; }
            }
            double kq = Math.Round(tong, 1);
            tk.ChiTiet.Add(dong + " ⇒ " + F(kq));
            return kq;
        }

        /// <summary>Ký hiệu 曲げ (QC I.1.E): r cuộn, m nhấn / bẻ, t taro, s sara; nhiều loại → *.</summary>
        public static string KyHieu(TamAi tam, IEnumerable<Uon> uon)
        {
            var k = new List<char>();
            var ds = uon.ToList();
            if (ds.Any(u => u.Kieu == "cuon")) k.Add('r');
            if (ds.Any(u => u.Kieu != "cuon")) k.Add('m');
            List<object> lo;
            try { lo = JsonLite.Parse(tam.LoJson ?? "[]") as List<object> ?? new List<object>(); } catch (Exception) { lo = new List<object>(); }
            if (lo.OfType<JsonObject>().Any(l => (l.GetString("taro") ?? "").Trim().Length > 0)) k.Add('t');
            if (lo.OfType<JsonObject>().Any(l => l.Get("sara") is bool b && b)) k.Add('s');
            return k.Count == 0 ? "" : k.Count == 1 ? k[0].ToString() : "*";
        }

        // ---------- Bản vẽ trải phẳng (ezdxf — thư viện biên dạng _apps\OhtaTeams\ve_dxf) ----------

        /// <summary>Mã biên dạng AI báo khi thư viện chưa có mẫu khớp.</summary>
        public const string ChuaCoMau = "chua_co";

        /// <summary>Loại biên dạng tự suy khi AI chưa chọn: cuộn / hộp (bẻ 2 hướng) / bẻ (một hướng) / phẳng; làm tay → "".</summary>
        public static string BienDangMacDinh(TamTrienKhai tk)
        {
            if (tk.LamTay) return "";
            var uon = tk.X.Uon.Concat(tk.Y.Uon).ToList();
            if (uon.Any(u => u.Kieu == "cuon")) return "cuon";
            if (tk.X.Uon.Count > 0 && tk.Y.Uon.Count > 0) return "hop";
            return uon.Count > 0 ? "be" : "phang";
        }

        /// <summary>Biên dạng dùng để vẽ: AI chọn, không có thì tự suy.</summary>
        public static string BienDangVe(TamTrienKhai tk) => tk.BienDang.Length > 0 ? tk.BienDang : BienDangMacDinh(tk);

        /// <summary>Bango tạm &lt;title&gt;-&lt;stt&gt; (cột エクスポート用 của CSV, tên tệp bản vẽ). GRO đánh bango thật trên CAD.</summary>
        public static string Bango(string title, int stt) => title + "-" + stt;

        /// <summary>
        /// F-name như tab Fname: N=, M=, T=, Q=, các dòng V= của TraV / ấn 1 lần, Z=. Theo bản chuẩn xưởng (12/11):
        /// N= trống thì lấy bango; T luôn có phần thập phân (12 → 12.0).
        /// </summary>
        public static List<string> DongFname(TamAi tam, TamTrienKhai tk, string bango = "")
        {
            string n = tam.SoBanVe.Length > 0 ? tam.SoBanVe : tam.Ten.Length > 0 ? tam.Ten : bango;
            string t = double.TryParse((tam.T ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double so) ? so.ToString("0.0##", CultureInfo.InvariantCulture) : tam.T;
            var d = new List<string> { "N=" + n, "M=" + tam.M, "T=" + t, "Q=" + tam.Q };
            d.AddRange(tk.DongV);
            d.Add("Z=" + tam.Z);
            return d;
        }

        /// <summary>
        /// Tệp việc cho ve_dxf\ve.py: môi trường, thư mục ra, các thư mục thư viện (sau đè trước), mỗi tấm vẽ được
        /// (bango, biên dạng, X, Y, đường bẻ, đoạn phẳng, F-name, chi tiết). Tấm làm tay / thiếu X, Y bỏ.
        /// </summary>
        public static string ViecVe(string title, IList<(TamAi Tam, TamTrienKhai Tk)> ds, string moiTruong, string thuMucRa, IEnumerable<string> thuVien)
        {
            List<object> So(IEnumerable<double> v) => v.Select(d => (object)new JsonNumber(F(d))).ToList();
            var tam = new List<object>();
            for (int i = 0; i < ds.Count; i++)
            {
                var (t, tk) = ds[i];
                if (tk.LamTay || !(tk.XPhang is double x) || !(tk.YPhang is double y)) continue;
                var j = new JsonObject();
                j.Set("bango", Bango(title, i + 1));
                j.Set("bien_dang", BienDangVe(tk));
                j.Set("x", new JsonNumber(F(x)));
                j.Set("y", new JsonNumber(F(y)));
                j.Set("be_x", So(tk.BeX)); j.Set("be_y", So(tk.BeY));
                j.Set("doan_x", So(tk.DoanPhangX)); j.Set("doan_y", So(tk.DoanPhangY));
                j.Set("ke_gaki", KeGaki(t).Select(k => (object)So(new[] { k.X, k.Y })).ToList());
                j.Set("dim_khach", DsJson(t.DimKhachJson));
                j.Set("fname", DongFname(t, tk, Bango(title, i + 1)).Cast<object>().ToList());
                j.Set("ghi_chu", tk.ChiTiet.Cast<object>().ToList());
                tam.Add(j);
            }
            var o = new JsonObject();
            o.Set("moi_truong", moiTruong);
            o.Set("thu_muc_ra", thuMucRa);
            o.Set("thu_vien", thuVien.Cast<object>().ToList());
            o.Set("tam", tam);
            return JsonLite.Serialize(o);
        }

        private static List<object> DsJson(string json)
        {
            try { return JsonLite.Parse(json ?? "[]") as List<object> ?? new List<object>(); } catch (Exception) { return new List<object>(); }
        }

        /// <summary>Các dấu ケガキ đọc được (bỏ dấu thiếu x / y).</summary>
        public static List<(double X, double Y)> KeGaki(TamAi t)
        {
            var ds = new List<(double, double)>();
            List<object> l;
            try { l = JsonLite.Parse(t.KeGakiJson ?? "[]") as List<object> ?? new List<object>(); } catch (Exception) { l = new List<object>(); }
            foreach (JsonObject o in l.OfType<JsonObject>())
                if (SoJ(o, "x") is double x && SoJ(o, "y") is double y) ds.Add((x, y));
            return ds;
        }

        // ---------- Đề xuất mẫu biên dạng mới (AI viết, Admin duyệt) ----------

        public const string LoiDanMauMoi =
@"Bạn viết MẪU BIÊN DẠNG mới cho thư viện vẽ trải phẳng của OHTA (Python 3, ezdxf). Một mẫu = một tệp Python:
- MO_TA = ""..."": một câu tiếng Việt mô tả loại biên dạng (để AI phòng Triển khai chọn mẫu sau này) — mô tả LOẠI hình, không ghi số của tấm này.
- def ve(nen, t): vẽ viền cắt + đường bẻ (+ dim riêng nếu cần) của tấm bằng các hàm của nen (moi_truong.NenVe). t là dict: x, y (kích thước trải), be_x (vị trí đường bẻ song song trục Y, mm từ mép trái), be_y (song song trục X, mm từ mép dưới), doan_x, doan_y (các đoạn phẳng giữa đường bẻ), ghi_chu (list chuỗi), fname (list chuỗi — KHÔNG tự vẽ, chương trình vẽ).
- Đặt tấm từ gốc (0, 0). Viền cắt phải KÍN. Cạnh thẳng + cung: nen.duong + nen.cung (CUNG THẬT — không chia cung thành nhiều điểm); toàn cạnh thẳng: nen.vien / nen.chu_nhat. Chỉ dùng hàm của nen, không tự đặt layer / kiểu chữ / kiểu dim.
- Dim theo bản chuẩn xưởng: chuỗi dim chi tiết ở PHÍA DƯỚI (và bên phải) — chương trình tự dựng từ be_x / be_y / ケガキ / nen.moc_x, nen.moc_y; mẫu chỉ thêm mốc (nen.moc_x.append(...)). Cung cần ghi R: nen.cung(..., dim=True) (tự dim R + đưa tâm vào chuỗi). Dim riêng của hình (vd 2 dim nửa chiều cao tới tâm cung) đặt cách viền LE_DIM, chữ đoạn ngắn đưa ra ngoài bằng vi_tri_chu.
- KHÔNG vẽ: dim tổng X, Y (1-XY), ケガキ, F-name, Khung_IN — chương trình tự thêm. Theo QUY CHUẨN VẼ ở khối sau nếu có.
- CHỈ được import: math, moi_truong (vd from moi_truong import LE_DIM). Không đọc / ghi tệp, không os / sys / subprocess / eval / exec.
- Viết tổng quát cho mọi tấm cùng loại (đọc số từ t), không gắn cứng số của tấm này.
Trả: ten (tên tệp: chữ thường không dấu, số, gạch dưới, 3–30 ký tự, khác tên đã có), mo_ta (= MO_TA), ma (toàn bộ mã Python).";

        /// <summary>API của moi_truong.NenVe gửi kèm (AI chỉ được gọi các hàm này).</summary>
        public const string ApiMoiTruong =
@"nen.vien(diem)                 # viền cắt kín, diem = [(x, y), ...] không lặp đỉnh đầu (0-KAKOU)
nen.chu_nhat(x0, y0, x1, y1)   # viền cắt chữ nhật
nen.duong(a, b)                # một cạnh viền rời (LINE 0-KAKOU) — dùng cùng nen.cung khi viền có cung
nen.cung(tam, r, goc_dau, goc_cuoi, dim=False)  # cung viền thật (ARC 0-KAKOU), độ, ngược chiều kim đồng hồ; dim=True: dim R + tâm vào chuỗi dim
nen.moc_x.append(x); nen.moc_y.append(y)        # thêm mốc vào chuỗi dim dưới / phải
nen.duong_be(a, b)             # đường bẻ (LINE 0-KAKOU, nét DASHED), a, b = (x, y)
nen.slit(a, b)                 # nét rạch slit (4-SHT, không cần kín)
nen.chu(x, y, noi_dung, cao=14.0)   # chữ ghi chú (1-SUNPOU, kiểu Text)
nen.dim_ngang(x0, x1, y, y_dim)     # dim ngang đo x0 → x1 tại cao độ y, đường dim ở y_dim (ISO-26)
nen.dim_doc(y0, y1, x, x_dim, vi_tri_chu=None)  # dim dọc; vi_tri_chu=(x, y) đưa chữ ra ngoài
moi_truong.LE_DIM = 80.0       # khoảng viền → đường dim chi tiết";

        public static string BodyMauMoi(string model, string quyChuanVe, string pdfBase64, string dauVao) =>
            BodyChung(model, LoiDanMauMoi, SchemaMauMoi(), string.IsNullOrWhiteSpace(quyChuanVe) ? null : quyChuanVe, pdfBase64, dauVao, null);

        public static JsonObject SchemaMauMoi()
        {
            JsonObject s = Kieu("string");
            return Obj(("ten", s), ("mo_ta", s), ("ma", s));
        }

        public static string DauVaoMauMoi(TamAi tam, TamTrienKhai tk, IEnumerable<string> tenDaCo, string maViDu)
        {
            var sb = new StringBuilder();
            sb.Append("Tấm cần mẫu mới: ").Append(PhongKiemTra.TenTam(tam)).Append(" — ").Append(tam.M).Append(" t").Append(tam.T).Append('\n');
            sb.Append("Mô tả biên dạng (phòng Triển khai): ").Append(tk.MoTaBienDang.Length > 0 ? tk.MoTaBienDang : "(không có)").Append('\n');
            sb.Append("Số của tấm này (chỉ để hiểu, không gắn cứng): x=").Append(tk.XPhang.HasValue ? F(tk.XPhang.Value) : "?").Append(" y=").Append(tk.YPhang.HasValue ? F(tk.YPhang.Value) : "?")
              .Append(" be_x=[").Append(string.Join(", ", tk.BeX.Select(F))).Append("] be_y=[").Append(string.Join(", ", tk.BeY.Select(F)))
              .Append("] doan_x=[").Append(string.Join(", ", tk.DoanPhangX.Select(F))).Append("] doan_y=[").Append(string.Join(", ", tk.DoanPhangY.Select(F))).Append("]\n");
            foreach (string c in tk.ChiTiet) sb.Append("  ").Append(c).Append('\n');
            sb.Append("\nTên mẫu đã có (không trùng): ").Append(string.Join(", ", tenDaCo)).Append("\n\nHàm môi trường:\n").Append(ApiMoiTruong)
              .Append("\n\nVí dụ một mẫu trong thư viện:\n```python\n").Append(maViDu).Append("\n```\n");
            return sb.ToString();
        }

        /// <summary>Kết quả AI đề xuất: tên tệp, mô tả, mã; Loi khác null = không dùng được.</summary>
        public sealed class MauMoi
        {
            public string Ten = "", MoTa = "", Ma = "", Loi;
            public long Vao, GhiCache, DocCache, Ra;
        }

        public static MauMoi DocMauMoi(string json, IEnumerable<string> tenDaCo)
        {
            var kq = new MauMoi();
            PhongDocBanVe.PhanHoi p2 = PhongDocBanVe.DocPhanHoi(json);
            kq.Vao = p2.Vao; kq.GhiCache = p2.GhiCache; kq.DocCache = p2.DocCache; kq.Ra = p2.Ra;
            JsonObject goc;
            try { goc = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { goc = null; }
            string text = (goc?.Get("content") as List<object>)?.OfType<JsonObject>().FirstOrDefault(x => x.GetString("type") == "text")?.GetString("text");
            JsonObject nd;
            try { nd = text == null ? null : JsonLite.Parse(text) as JsonObject; } catch (Exception) { nd = null; }
            if (nd == null) { kq.Loi = p2.Loi ?? "Phản hồi AI không đúng mẫu"; return kq; }
            kq.Ten = (nd.GetString("ten") ?? "").Trim(); kq.MoTa = (nd.GetString("mo_ta") ?? "").Trim(); kq.Ma = nd.GetString("ma") ?? "";
            kq.Loi = KiemMau(kq.Ten, kq.Ma, tenDaCo);
            return kq;
        }

        private static readonly System.Text.RegularExpressions.Regex TenHopLe = new System.Text.RegularExpressions.Regex("^[a-z][a-z0-9_]{2,29}$");
        private static readonly string[] TuCam = { "open(", "exec(", "eval(", "compile(", "__import__", "globals(", "getattr(", "setattr(", "subprocess", "socket", "shutil", "ctypes", "os.", "sys.", "pathlib", "urllib", "requests", "input(" };

        /// <summary>
        /// Kiểm mã mẫu trước khi chạy (mã AI viết chạy trên máy): tên hợp lệ, không trùng, có MO_TA + def ve(nen, t),
        /// chỉ import math / moi_truong, không dùng hàm / module truy cập tệp, mạng, tiến trình. null = được.
        /// </summary>
        public static string KiemMau(string ten, string ma, IEnumerable<string> tenDaCo)
        {
            if (!TenHopLe.IsMatch(ten ?? "")) return "Tên mẫu không hợp lệ: '" + ten + "' (chữ thường không dấu, số, gạch dưới, 3–30 ký tự).";
            if ((tenDaCo ?? Enumerable.Empty<string>()).Contains(ten)) return "Tên mẫu '" + ten + "' đã có trong thư viện.";
            if (string.IsNullOrWhiteSpace(ma) || !ma.Contains("MO_TA") || !System.Text.RegularExpressions.Regex.IsMatch(ma, @"def\s+ve\s*\(\s*nen\s*,\s*t\s*\)"))
                return "Mã thiếu MO_TA hoặc def ve(nen, t).";
            foreach (string dong in ma.Replace("\r", "").Split('\n'))
            {
                string d = dong.Trim();
                if (d.StartsWith("#")) continue;
                var im = System.Text.RegularExpressions.Regex.Match(d, @"^(?:from\s+([\w.]+)\s+import\b|import\s+([\w.,\s]+))");
                if (im.Success)
                {
                    var mods = im.Groups[1].Success ? new[] { im.Groups[1].Value } : im.Groups[2].Value.Split(',').Select(m => m.Trim().Split(' ')[0]).ToArray();
                    string la = mods.FirstOrDefault(m => m != "math" && m != "moi_truong");
                    if (la != null) return "Mã import '" + la + "' — chỉ được import math, moi_truong.";
                }
                string cam = TuCam.FirstOrDefault(c => d.Contains(c));
                if (cam != null) return "Mã dùng '" + cam.TrimEnd('(') + "' — không được phép trong mẫu vẽ.";
            }
            return null;
        }

        // ---------- CSV + tóm tắt ----------

        public const string TieuDeCsv = "エクスポート用,数量,材質,板厚,曲げ,X,Y";

        /// <summary>
        /// Dòng CSV theo QC I.1.E. Bango tạm = &lt;title&gt;-&lt;số thứ tự&gt; (GRO đánh bango thật trên CAD).
        /// shortcut: chưa có cột 単価 (giá) — thêm khi có bảng giá.
        /// </summary>
        public static string DongCsv(string title, int stt, TamAi tam, TamTrienKhai tk) =>
            string.Join(",", new[]
            {
                Bango(title, stt), Csv(tam.Q), Csv(tam.M), Csv(tam.T), tk.KyHieu,
                tk.XPhang.HasValue ? F(tk.XPhang.Value) : "", tk.YPhang.HasValue ? F(tk.YPhang.Value) : "",
            });

        private static string Csv(string s) => (s ?? "").Replace(",", " ").Replace("\n", " ").Trim();

        /// <summary>Bảng tóm tắt gửi lên chuỗi đơn để người triển khai / check kiểm.</summary>
        public static string TomTat(string title, IList<(TamAi Tam, TamTrienKhai Tk)> ds)
        {
            var sb = new StringBuilder("【Triển khai AI — cần kiểm】" + title + "\n");
            for (int i = 0; i < ds.Count; i++)
            {
                var (tam, tk) = ds[i];
                sb.Append(i + 1).Append(". ").Append(PhongKiemTra.TenTam(tam)).Append(" — ").Append(tam.M).Append(" t").Append(tam.T).Append(" ×").Append(tam.Q);
                if (tk.LamTay) sb.Append(" — LÀM TAY");
                else sb.Append(" — ").Append(tk.XPhang.HasValue ? F(tk.XPhang.Value) : "?").Append(" × ").Append(tk.YPhang.HasValue ? F(tk.YPhang.Value) : "?");
                if (tk.VChon.Length > 0) sb.Append(" — ").Append(tk.VChon);
                if (tk.KyHieu.Length > 0) sb.Append(" [").Append(tk.KyHieu).Append(']');
                sb.Append('\n');
                foreach (string c in tk.ChiTiet) sb.Append("   ").Append(c).Append('\n');
                foreach (string c in tk.CanhBao) sb.Append("   ⚠ ").Append(c).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        // ---------- AI đọc tiết diện ----------

        public const int MaxTokens = 16000;

        public const string LoiDan =
@"Bạn là kỹ sư triển khai kim loại tấm của OHTA (phòng Triển khai). Đầu vào: bản vẽ khách (PDF hoặc tóm tắt DXF), bảng tấm đã kiểm (phòng Kiểm tra) và các câu trả lời đã có.
Với MỖI tấm trong bảng, đọc TIẾT DIỆN để trải phẳng (không tự tính trải phẳng — chương trình tính theo bảng TV):
- so_ban_ve, ten: đúng như bảng tấm (để ghép).
- kieu: phang (không uốn) / be (bẻ, ấn, cuộn tính được) / lam_tay (pitchi, kakumaru, ensui, ống co, hình 3D phức tạp — không đọc tiết diện).
- x, y: tiết diện theo 2 hướng vuông góc của tấm. doan = các đoạn theo thứ tự dọc tiết diện; uon = các chỗ uốn giữa 2 đoạn liền nhau (số uon = số doan − 1).
  · Bẻ / ấn: doan là kích thước NGOÀI (tới giao điểm cạnh ngoài), như kích thước ghi trên bản vẽ; uốn: kieu be (R nhỏ, bẻ dao V) hoặc an_1_lan (R trong ghi trên bản vẽ là bội 5, < 50); goc = góc trong giữa 2 đoạn (90 = vuông, 135 = mở, 60 = nhọn); r_trong = R trong (0 nếu không ghi); cung = 0.
  · Cuộn: uốn kieu cuon, r_trong = R trong, cung = góc cung (360 = tròn kín, 180 = nửa); doan kề là phần thẳng tiếp tuyến (0 nếu không có).
  · Hướng không uốn: một doan = chiều dài phẳng theo hướng đó, uon rỗng. Tấm phẳng: x = [dài], y = [rộng].
  · Hộp 3–4 cạnh: x và y đều có tiết diện (thành – đáy – thành).
- ghi_chu: tiếng Việt, ngắn (vd hướng bẻ V / N, chỗ chưa chắc). Dùng câu trả lời đã có làm dữ liệu đúng. Không bịa: không đọc được thì để doan rỗng và ghi lý do.
- bien_dang: tên đúng MỘT mẫu trong ""Thư viện biên dạng"" (đầu vào) khớp hình trải phẳng của tấm; tấm làm tay → rỗng; không mẫu nào khớp → ""chua_co"".
- mo_ta_bien_dang: chỉ khi chua_co — mô tả hình trải phẳng (viền, góc cắt, đường bẻ, lỗ / rãnh đặc biệt) đủ để lập trình viên viết mẫu vẽ mới; còn lại rỗng.";

        public static JsonObject Schema()
        {
            JsonObject s = Kieu("string"), td = SchemaTd();
            return Obj(("tam", Mang(Obj(("so_ban_ve", s), ("ten", s), ("kieu", LuaChon("phang", "be", "lam_tay")), ("x", td), ("y", td), ("ghi_chu", s), ("bien_dang", s), ("mo_ta_bien_dang", s)))));
        }

        /// <summary>Mẫu JSON một tiết diện (dùng chung với lượt đọc bản vẽ).</summary>
        public static JsonObject SchemaTd()
        {
            JsonObject n = Kieu("number");
            JsonObject uon = Obj(("kieu", LuaChon("be", "an_1_lan", "cuon", "pitchi")), ("goc", n), ("r_trong", n), ("cung", n));
            return Obj(("doan", Mang(n)), ("uon", Mang(uon)));
        }

        // ---------- Tiết diện dạng chữ (sửa trên bảng phòng Kiểm tra) ----------
        // Đoạn và chỗ uốn xen nhau: số = đoạn (mm); b90 = bẻ 90° (b90r2 có R trong); a90r10 = ấn 1 lần; c360r50 = cuộn cung 360° R50; p90 = pitchi.

        public const string HuongDanTd = "Số = đoạn ngoài (mm), xen chỗ uốn: b90 = bẻ 90° (b90r2 có R), a90r10 = ấn 1 lần R10, c360r50 = cuộn cung 360° R50, p90 = pitchi. Vd: 50 b90 100 b90 50. Không uốn: một số.";

        public static string ChuTd(TietDien td)
        {
            var p = new List<string>();
            for (int i = 0; i < Math.Max(td.Doan.Count, td.Uon.Count); i++)
            {
                if (i < td.Doan.Count) p.Add(F(td.Doan[i]));
                if (i < td.Uon.Count)
                {
                    Uon u = td.Uon[i];
                    string r = u.R > 0 ? "r" + F(u.R) : "";
                    p.Add(u.Kieu == "an_1_lan" ? "a" + F(u.Goc) + r : u.Kieu == "cuon" ? "c" + F(u.Cung) + r : u.Kieu == "pitchi" ? "p" + F(u.Goc) + r : "b" + F(u.Goc) + r);
                }
            }
            return string.Join(" ", p);
        }

        private static readonly System.Text.RegularExpressions.Regex ChuUon =
            new System.Text.RegularExpressions.Regex(@"^([bacp])(\d+(?:\.\d+)?)°?(?:r(\d+(?:\.\d+)?))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>Chữ → tiết diện; sai cú pháp → null + loi. Chữ rỗng → tiết diện rỗng.</summary>
        public static TietDien DocChuTd(string s, out string loi)
        {
            loi = null;
            var td = new TietDien();
            string[] tu = (s ?? "").Replace(',', ' ').Replace('+', ' ').Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tu.Length; i++)
            {
                if (i % 2 == 0)
                {
                    if (!double.TryParse(tu[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double d) || d < 0) { loi = "'" + tu[i] + "' phải là một đoạn (số mm)"; return null; }
                    td.Doan.Add(d);
                    continue;
                }
                var m = ChuUon.Match(tu[i]);
                if (!m.Success) { loi = "'" + tu[i] + "' không phải chỗ uốn (b90, a90r10, c360r50, p90)"; return null; }
                double so = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), r = m.Groups[3].Success ? double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
                char k = char.ToLowerInvariant(m.Groups[1].Value[0]);
                td.Uon.Add(k == 'c' ? new Uon { Kieu = "cuon", Cung = so, R = r }
                                    : new Uon { Kieu = k == 'a' ? "an_1_lan" : k == 'p' ? "pitchi" : "be", Goc = so, R = r });
            }
            if (tu.Length > 0 && tu.Length % 2 == 0) { loi = "phải kết thúc bằng một đoạn"; return null; }
            return td;
        }

        /// <summary>Tấm đã có tiết diện (đọc bằng bản mới, hoặc người nhập) chưa.</summary>
        public static bool CoTietDien(TamAi t) => t.KieuTd == "lam_tay" || t.TdX.Trim().Length > 0 || t.TdY.Trim().Length > 0;

        /// <summary>Dữ liệu triển khai từ bảng tấm đã xác nhận (V người chọn gán bên ngoài).</summary>
        public static TamTrienKhai TuTam(TamAi t) => new TamTrienKhai
        {
            Khoa = t.Khoa, Ten = PhongKiemTra.TenTam(t), Kieu = t.KieuTd.Length > 0 ? t.KieuTd : "phang", GhiChu = t.GhiChuTd,
            BienDang = t.BienDang, MoTaBienDang = t.MoTaBienDang,
            X = DocChuTd(t.TdX, out _) ?? new TietDien(), Y = DocChuTd(t.TdY, out _) ?? new TietDien(),
        };

        /// <summary>Đơn đọc bằng bản cũ: chép tiết diện phòng Triển khai cũ đã đọc vào bảng tấm.</summary>
        public static void ChepVaoTam(TamTrienKhai tk, TamAi t)
        {
            t.KieuTd = tk.Kieu; t.TdX = ChuTd(tk.X); t.TdY = ChuTd(tk.Y); t.GhiChuTd = tk.GhiChu;
            t.BienDang = tk.BienDang; t.MoTaBienDang = tk.MoTaBienDang;
        }

        /// <summary>Phần chữ gửi kèm bản vẽ: bảng tấm của tệp + câu trả lời phòng 3.</summary>
        /// <param name="thuVien">Mẫu biên dạng có trong thư viện ve_dxf (tên, mô tả) — AI chọn một hoặc chua_co.</param>
        public static string DauVao(string tenTep, IEnumerable<TamAi> tam, IEnumerable<VanDe> vanDe, IEnumerable<(string Ten, string MoTa)> thuVien = null)
        {
            var sb = new StringBuilder("Tệp: " + tenTep + "\n\nThư viện biên dạng (bien_dang = tên):\n");
            var tv = (thuVien ?? Enumerable.Empty<(string, string)>()).ToList();
            if (tv.Count == 0) sb.Append("(trống — để bien_dang rỗng)\n");
            foreach (var (ten, moTa) in tv) sb.Append("- ").Append(ten).Append(": ").Append(moTa).Append('\n');
            sb.Append("\nBảng tấm:\n");
            foreach (TamAi t in tam)
                sb.Append("- so_ban_ve=").Append(t.SoBanVe).Append(" ten=").Append(t.Ten).Append(" M=").Append(t.M).Append(" T=").Append(t.T).Append(" Q=").Append(t.Q)
                  .Append(" kích thước=").Append(t.KichThuoc).Append(" bẻ=").Append(t.CanhJson).Append(" ghi chú=").Append(t.GhiChuVi).Append('\n');
            var tl = (vanDe ?? Enumerable.Empty<VanDe>()).Where(v => v.TraLoi.Trim().Length > 0 || v.TrangThai == VanDe.BoQua).ToList();
            if (tl.Count > 0)
            {
                sb.Append("\nPhòng Kiểm tra — câu trả lời / quyết định (dữ liệu đúng):\n");
                foreach (VanDe v in tl)
                    sb.Append("- ").Append(v.Tam).Append(" | ").Append(v.NoiDung).Append(" | ").Append(v.TraLoi.Trim().Length > 0 ? "trả lời: " + v.TraLoi.Trim() : "bỏ qua: " + v.LyDo).Append('\n');
            }
            return sb.ToString();
        }

        public static string Body(string model, string quyChuan, string pdfBase64, string dauVao, string tomTatDxf) =>
            BodyChung(model, LoiDan, Schema(), quyChuan, pdfBase64, dauVao, tomTatDxf);

        /// <summary>Body POST /v1/messages: system = lời dặn (+ quy chuẩn có cache), user = PDF / chữ; trả JSON theo schema.</summary>
        public static string BodyChung(string model, string loiDan, JsonObject schema, string quyChuan, string pdfBase64, string dauVao, string tomTatDxf)
        {
            var body = new JsonObject();
            body.Set("model", model);
            body.Set("max_tokens", MaxTokens);
            if (PhongDocBanVe.CoFallback(model)) body.Set("fallbacks", "default");
            var format = new JsonObject();
            format.Set("type", "json_schema");
            format.Set("schema", schema);
            var oc = new JsonObject();
            oc.Set("effort", "high");
            oc.Set("format", format);
            body.Set("output_config", oc);
            var cache = new JsonObject();
            cache.Set("type", "ephemeral");
            body.Set("system", quyChuan == null ? new List<object> { Khoi(loiDan, null) } : new List<object> { Khoi(loiDan, null), Khoi(quyChuan, cache) });
            var nd = new List<object>();
            if (!string.IsNullOrEmpty(pdfBase64))
            {
                var src = new JsonObject();
                src.Set("type", "base64"); src.Set("media_type", "application/pdf"); src.Set("data", pdfBase64);
                var doc = new JsonObject();
                doc.Set("type", "document"); doc.Set("source", src);
                nd.Add(doc);
            }
            nd.Add(Khoi(dauVao + (string.IsNullOrEmpty(tomTatDxf) ? "" : "\n\n" + tomTatDxf), null));
            var msg = new JsonObject();
            msg.Set("role", "user");
            msg.Set("content", nd);
            body.Set("messages", new List<object> { msg });
            return JsonLite.Serialize(body);
        }

        public sealed class PhanHoi
        {
            /// <summary>so_ban_ve|ten (chữ thường) → tấm đọc được.</summary>
            public List<(string SoBanVe, string Ten, TamTrienKhai Tk)> Tam = new List<(string, string, TamTrienKhai)>();
            public long Vao, GhiCache, DocCache, Ra;
            public string Loi;
        }

        public static PhanHoi DocPhanHoi(string json)
        {
            var kq = new PhanHoi();
            PhongDocBanVe.PhanHoi p2 = PhongDocBanVe.DocPhanHoi(json); // token + lỗi chung
            kq.Vao = p2.Vao; kq.GhiCache = p2.GhiCache; kq.DocCache = p2.DocCache; kq.Ra = p2.Ra;
            JsonObject goc;
            try { goc = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { goc = null; }
            string text = (goc?.Get("content") as List<object>)?.OfType<JsonObject>().FirstOrDefault(x => x.GetString("type") == "text")?.GetString("text");
            JsonObject nd;
            try { nd = text == null ? null : JsonLite.Parse(text) as JsonObject; } catch (Exception) { nd = null; }
            if (!(nd?.Get("tam") is List<object> ds)) { kq.Loi = p2.Loi ?? "Phản hồi AI không đúng mẫu"; return kq; }
            foreach (JsonObject x in ds.OfType<JsonObject>())
            {
                var tk = new TamTrienKhai
                {
                    Kieu = x.GetString("kieu") ?? "phang", GhiChu = x.GetString("ghi_chu") ?? "",
                    BienDang = x.GetString("bien_dang") ?? "", MoTaBienDang = x.GetString("mo_ta_bien_dang") ?? "",
                    X = DocTd(x.Get("x") as JsonObject), Y = DocTd(x.Get("y") as JsonObject),
                };
                kq.Tam.Add((x.GetString("so_ban_ve") ?? "", x.GetString("ten") ?? "", tk));
            }
            return kq;
        }

        /// <summary>Ghép tấm AI đọc với bảng tấm: theo số bản vẽ, không có thì theo tên; còn lại theo thứ tự.</summary>
        public static TamTrienKhai Ghep(TamAi tam, List<(string SoBanVe, string Ten, TamTrienKhai Tk)> ds, HashSet<TamTrienKhai> daDung)
        {
            bool Bang(string a, string b) => a.Trim().Length > 0 && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
            var kq = ds.FirstOrDefault(x => !daDung.Contains(x.Tk) && Bang(x.SoBanVe, tam.SoBanVe)).Tk
                     ?? ds.FirstOrDefault(x => !daDung.Contains(x.Tk) && Bang(x.Ten, tam.Ten)).Tk
                     ?? ds.FirstOrDefault(x => !daDung.Contains(x.Tk)).Tk;
            if (kq != null) daDung.Add(kq);
            return kq;
        }

        // ---------- JSON trien_khai.json ----------

        public static string Ghi(KetQuaTrienKhai kq)
        {
            var o = new JsonObject();
            o.Set("tu_xac_nhan", kq.TuXacNhan ?? ""); o.Set("luc", kq.Luc ?? "");
            o.Set("token_vao", new JsonNumber(kq.TokenVao.ToString(CultureInfo.InvariantCulture)));
            o.Set("token_ra", new JsonNumber(kq.TokenRa.ToString(CultureInfo.InvariantCulture)));
            o.Set("tien", new JsonNumber(kq.Tien.ToString(CultureInfo.InvariantCulture)));
            o.Set("tam", kq.Tam.Select(t =>
            {
                var x = new JsonObject();
                x.Set("khoa", t.Khoa); x.Set("ten", t.Ten); x.Set("kieu", t.Kieu); x.Set("ghi_chu", t.GhiChu); x.Set("v_chon", t.VChon); x.Set("bien_dang", t.BienDang); x.Set("mo_ta_bien_dang", t.MoTaBienDang);
                x.Set("x", GhiTd(t.X)); x.Set("y", GhiTd(t.Y));
                return (object)x;
            }).ToList());
            return JsonLite.Serialize(o);
        }

        public static KetQuaTrienKhai Doc(string json)
        {
            JsonObject o;
            try { o = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { return null; }
            if (o == null) return null;
            var kq = new KetQuaTrienKhai
            {
                TuXacNhan = o.GetString("tu_xac_nhan") ?? "", Luc = o.GetString("luc") ?? "",
                TokenVao = (long)(SoJ(o, "token_vao") ?? 0), TokenRa = (long)(SoJ(o, "token_ra") ?? 0), Tien = (decimal)(SoJ(o, "tien") ?? 0),
            };
            foreach (JsonObject x in (o.Get("tam") as List<object> ?? new List<object>()).OfType<JsonObject>())
                kq.Tam.Add(new TamTrienKhai
                {
                    Khoa = x.GetString("khoa") ?? "", Ten = x.GetString("ten") ?? "", Kieu = x.GetString("kieu") ?? "phang",
                    GhiChu = x.GetString("ghi_chu") ?? "", VChon = x.GetString("v_chon") ?? "",
                    BienDang = x.GetString("bien_dang") ?? "", MoTaBienDang = x.GetString("mo_ta_bien_dang") ?? "",
                    X = DocTd(x.Get("x") as JsonObject), Y = DocTd(x.Get("y") as JsonObject),
                });
            return kq;
        }

        private static JsonObject GhiTd(TietDien td)
        {
            var o = new JsonObject();
            o.Set("doan", td.Doan.Select(d => (object)new JsonNumber(F(d))).ToList());
            o.Set("uon", td.Uon.Select(u =>
            {
                var x = new JsonObject();
                x.Set("kieu", u.Kieu); x.Set("goc", new JsonNumber(F(u.Goc))); x.Set("r_trong", new JsonNumber(F(u.R))); x.Set("cung", new JsonNumber(F(u.Cung)));
                return (object)x;
            }).ToList());
            return o;
        }

        public static TietDien DocTd(JsonObject o)
        {
            var td = new TietDien();
            if (o == null) return td;
            foreach (object d in o.Get("doan") as List<object> ?? new List<object>())
                if (SoObj(d) is double v) td.Doan.Add(v);
            foreach (JsonObject u in (o.Get("uon") as List<object> ?? new List<object>()).OfType<JsonObject>())
                td.Uon.Add(new Uon { Kieu = u.GetString("kieu") ?? "be", Goc = SoJ(u, "goc") ?? 90, R = SoJ(u, "r_trong") ?? 0, Cung = SoJ(u, "cung") ?? 0 });
            return td;
        }

        // ---------- tiện ích ----------

        public static string F(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);
        private static double? SoObj(object v) => v is JsonNumber n && double.TryParse(n.Raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : v is string s ? PhongKiemTra.So(s) : null;
        private static double? SoJ(JsonObject o, string k) => SoObj(o.Get(k));
        private static JsonObject Kieu(string type) { var o = new JsonObject(); o.Set("type", type); return o; }
        private static JsonObject LuaChon(params string[] gt) { JsonObject o = Kieu("string"); o.Set("enum", gt.Cast<object>().ToList()); return o; }
        private static JsonObject Mang(JsonObject item) { JsonObject a = Kieu("array"); a.Set("items", item); return a; }

        private static JsonObject Obj(params (string Ten, JsonObject Kieu)[] tt)
        {
            var p = new JsonObject();
            foreach (var t in tt) p.Set(t.Ten, t.Kieu);
            JsonObject o = Kieu("object");
            o.Set("properties", p);
            o.Set("required", tt.Select(t => (object)t.Ten).ToList());
            o.Set("additionalProperties", false);
            return o;
        }

        private static JsonObject Khoi(string text, JsonObject cache)
        {
            var o = new JsonObject();
            o.Set("type", "text");
            o.Set("text", string.IsNullOrEmpty(text) ? "(trống)" : text);
            if (cache != null) o.Set("cache_control", cache);
            return o;
        }
    }
}
