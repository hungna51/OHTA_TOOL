using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một vấn đề phòng 3 tìm ra (luật C# hoặc AI).</summary>
    public sealed class VanDe
    {
        public const string Do = "do", Vang = "vang", ThongTin = "info";
        public const string TuXuLy = "tu_xu_ly", NoiBo = "noi_bo", Khach = "khach";
        public const string ChuaXong = "", Xong = "xong", BoQua = "bo_qua";

        /// <summary>Khoá ổn định giữa các lần kiểm — giữ trạng thái người dùng đã chọn.</summary>
        public string Ma = "";
        public string Muc = Vang, Nhom = NoiBo;
        public string Tam = "", MucQc = "", NoiDung = "", CauHoiJp = "";
        /// <summary>Thuộc bản nháp 連絡します (slit / phần thừa); false = お問い合わせ.</summary>
        public bool LienLac;
        public string Nguon = "luat"; // luat | ai
        public string TrangThai = ChuaXong, LyDo = "";
        /// <summary>Câu trả lời của khách / quản lý, hoặc cách xử lý người dùng ghi — phòng 4 và lần kiểm lại dùng.</summary>
        public string TraLoi = "";
        public bool DaGui;

        public bool ConMo => TrangThai == ChuaXong;
        public string BieuTuong => Muc == Do ? "🔴" : Muc == Vang ? "🟡" : "ℹ";
    }

    /// <summary>Kết quả phòng 3, lưu &lt;thư mục đơn&gt;\ai\kiem_tra.json.</summary>
    public sealed class KiemTra
    {
        public List<VanDe> VanDe = new List<VanDe>();
        /// <summary>Lúc xác nhận bảng phòng 2 mà lần kiểm này dùng — khác chi_tiet.json hiện tại = bảng đã đổi, kiểm lại.</summary>
        public string TuXacNhan = "";
        /// <summary>Lúc bấm ✓ Xác nhận → phòng 4; "" = chưa.</summary>
        public string XacNhan = "";
        public string Luc = "";
        public long TokenVao, TokenRa;
        public decimal Tien;

        public int DoConMo => VanDe.Count(v => v.Muc == global::TeamsTool.Core.VanDe.Do && v.ConMo);
    }

    /// <summary>
    /// Phòng 3 — Kiểm tra (spec 2026-11-11): luật có số cố định viết C# (thuần, có test), AI rà phần còn lại theo QUY CHUẨN phòng 3.
    /// Ngưỡng máy nằm trong mã công khai (người dùng đồng ý 11/11); luật riêng từng công ty chỉ ở quy_chuan (gitignore).
    /// </summary>
    public static class PhongKiemTra
    {
        // ---------- Bảng TV (01_Fname\data\trav_90.csv) ----------

        /// <summary>Dòng bảng bẻ 90°: vật liệu, T, V, Lmin (L_saitei).</summary>
        public sealed class DongTv
        {
            public string Mat = "";
            public double T, V, Lmin;
        }

        /// <summary>Đọc trav_90.csv (dòng đầu là tiêu đề, có thể có BOM); dòng hỏng bỏ qua.</summary>
        public static List<DongTv> DocTv(string csv)
        {
            var ds = new List<DongTv>();
            string[] dong = (csv ?? "").TrimStart('\uFEFF').Replace("\r", "").Split('\n');
            if (dong.Length < 2) return ds;
            string[] tieuDe = dong[0].Split(',');
            int cMat = Array.IndexOf(tieuDe, "Mat"), cT = Array.IndexOf(tieuDe, "T"), cV = Array.IndexOf(tieuDe, "V"), cL = Array.IndexOf(tieuDe, "L_saitei");
            if (cMat < 0 || cT < 0 || cV < 0 || cL < 0) return ds;
            foreach (string s in dong.Skip(1))
            {
                string[] o = s.Split(',');
                if (o.Length <= Math.Max(Math.Max(cMat, cT), Math.Max(cV, cL))) continue;
                if (So(o[cT]) is double t && So(o[cV]) is double v && So(o[cL]) is double l)
                    ds.Add(new DongTv { Mat = o[cMat].Trim(), T = t, V = v, Lmin = l });
            }
            return ds;
        }

        /// <summary>Các (V, Lmin) của vật liệu ở đúng độ dày T, V tăng dần; không có → rỗng.</summary>
        public static List<DongTv> Lmin(List<DongTv> tv, string mat, double t) =>
            tv.Where(r => string.Equals(r.Mat, mat, StringComparison.OrdinalIgnoreCase) && Math.Abs(r.T - t) < 0.01).OrderBy(r => r.V).ToList();

        /// <summary>
        /// Khoá vật liệu của bảng TV từ M (+ mặt): catalog.csv (M → Loai) trước, không có thì đoán theo tên.
        /// Tấm sima (縞板 / SIMA / シマ) dùng dòng "&lt;Loai&gt; SIMA Ngoài" nếu bảng có. Không đoán được → null.
        /// </summary>
        public static string LoaiTv(string m, string mat, string catalogCsv, List<DongTv> tv)
        {
            string loai = null;
            foreach (string s in (catalogCsv ?? "").TrimStart('\uFEFF').Replace("\r", "").Split('\n').Skip(1))
            {
                string[] o = s.Split(',');
                if (o.Length > 1 && string.Equals(o[0].Trim(), (m ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { loai = o[1].Trim(); break; }
            }
            string u = (m ?? "").Trim().ToUpperInvariant();
            if (loai == null)
            {
                if (u.StartsWith("SUS", StringComparison.Ordinal)) loai = "SUS";
                else if (u.StartsWith("HT590", StringComparison.Ordinal)) loai = "HT590A";
                else if (u.StartsWith("AL", StringComparison.Ordinal) || u.Contains("アルミ") || (u.Length > 1 && u[0] == 'A' && char.IsDigit(u[1]))) loai = "AL";
                else if (u.StartsWith("BON", StringComparison.Ordinal) || u.Contains("ボンデ")) loai = "BON";
                else if (u.StartsWith("S", StringComparison.Ordinal) || u.Contains("ムキ")) loai = "SS";
                else return null;
            }
            string chu = (m + " " + mat).ToUpperInvariant();
            if (chu.Contains("縞") || chu.Contains("SIMA") || chu.Contains("シマ"))
            {
                string sima = loai + " SIMA Ngoài";
                if (tv.Any(r => string.Equals(r.Mat, sima, StringComparison.OrdinalIgnoreCase))) return sima;
            }
            return loai;
        }

        // ---------- Bảng taro (QC E-1-2) ----------
        // x = máy di động, c = máy cố định, g = xám "xác nhận lại" (người dùng chốt 11/11 là 🟡), - = ngoài bảng (xác nhận).

        private static readonly double[] CotTaroSs = { 2.3, 3.2, 4.5, 6.0, 9.0, 12, 16, 19, 22, 25 };
        private static readonly double[] CotTaroSus = { 4.0, 5.0, 6.0, 8.0, 9.0, 10, 12, 18, 25 };
        private static readonly Dictionary<string, string> TaroSs = BangTaro(
            ("1 1.2 1.4 1.7 2 2.3 2.6", "xxxxggggg"), ("3 3.5 4 4.5 5 5.5 6", "xxxxxgggg"), ("7 8 9", "xxxxxxggg"),
            ("10 12 14 16 18 20 22 24 27", "xxxxxxccc"));
        private static readonly Dictionary<string, string> TaroSus = BangTaro(
            ("1 1.2 1.4 1.7 2 2.3 2.6", "xxggggggg"), ("3 3.5 4 4.5 5 5.5", "xxxgggggg"), ("6 7", "xxxccccgg"), ("8 9", "xxxcccccg"),
            ("10 12 14 16 18 20 22 24 27", "xxxcccccc"));

        private static Dictionary<string, string> BangTaro(params (string Co, string Ma)[] nhom)
        {
            var d = new Dictionary<string, string>();
            foreach (var n in nhom) foreach (string co in n.Co.Split(' ')) d["M" + co] = n.Ma;
            return d;
        }

        /// <summary>Kết quả bảng taro: 'x' di động, 'c' cố định, 'g' xác nhận lại; null = vật liệu / cỡ không có trong bảng.</summary>
        public static char? Taro(string loai, string mCo, double t)
        {
            bool sus = loai != null && loai.StartsWith("SUS", StringComparison.OrdinalIgnoreCase);
            bool ss = loai != null && loai.StartsWith("SS", StringComparison.OrdinalIgnoreCase);
            if (!sus && !ss) return null;
            string co = "M" + (mCo ?? "").Trim().TrimStart('M', 'm').Split('x', 'X', '×', '-')[0].Trim();
            if (!(sus ? TaroSus : TaroSs).TryGetValue(co, out string ma)) return null;
            double[] cot = sus ? CotTaroSus : CotTaroSs;
            for (int i = 0; i < ma.Length && i < cot.Length; i++)
                if (t <= cot[i] + 0.001) return ma[i];
            return 'g'; // dày hơn cột cuối của bảng: xác nhận lại
        }

        // ---------- Luật C# ----------

        /// <summary>
        /// Chạy mọi luật trên các tấm mới (Moi). tv / catalogCsv: bảng 01_Fname\data (rỗng → bỏ luật Lmin, báo 🟡 một lần).
        /// </summary>
        public static List<VanDe> Luat(IEnumerable<TamAi> tam, List<DongTv> tv, string catalogCsv)
        {
            var ra = new List<VanDe>();
            foreach (TamAi t in tam.Where(x => x.Moi))
            {
                string ten = TenTam(t);
                void Them(string ma, string muc, string nhom, string qc, string nd, bool lienLac = false) =>
                    ra.Add(new VanDe { Ma = "luat|" + t.Khoa + "|" + ma, Muc = muc, Nhom = nhom, Tam = ten, MucQc = qc, NoiDung = nd, LienLac = lienLac });

                // Ô AI chưa chắc (người dùng chưa sửa)
                var chuaChac = t.KhongChac.Where(k => !t.DaSua.Contains(k)).OrderBy(k => k).ToList();
                if (chuaChac.Count > 0)
                    Them("khong_chac", VanDe.Vang, VanDe.NoiBo, "", "AI chưa chắc ô: " + string.Join(", ", chuaChac) + " — đối chiếu lại bản vẽ (✎ Sửa thông số của tấm).");

                // Tiết diện (đọc cùng lượt đọc bản vẽ; KieuTd rỗng = đọc bằng bản cũ → phòng Triển khai đọc bù)
                foreach (var (chu, huong) in new[] { (t.TdX, "X"), (t.TdY, "Y") })
                    if (chu.Trim().Length > 0 && PhongTrienKhai.DocChuTd(chu, out string loiTd) == null)
                        Them("td_sai_" + huong, VanDe.Do, VanDe.TuXuLy, "", "Tiết diện " + huong + " viết sai: " + loiTd + " — ✎ Sửa thông số → Tiết diện " + huong + ".");
                // Lỗ chưa có vị trí (đọc bằng bản mới mà AI không tính được tâm) → không vẽ được lỗ
                int loThieu = DsJson(t.LoJson).OfType<JsonObject>().Count(l => !(l.Get("vi_tri") is List<object> vt && vt.Count > 0));
                if (t.KieuTd.Length > 0 && t.KieuTd != "lam_tay" && loThieu > 0)
                    Them("lo_thieu_vi_tri", VanDe.Vang, VanDe.TuXuLy, "", loThieu + " nhóm lỗ chưa có vị trí — ✎ Sửa thông số → Lỗ (vi_tri x, y từ góc trái dưới hình trải phẳng).");
                if (t.BienDang == PhongTrienKhai.ChuaCoMau && t.KieuTd != "lam_tay")
                    Them("chua_co_mau", VanDe.Vang, VanDe.TuXuLy, "", "Thư viện chưa có mẫu vẽ cho hình này — bấm ✨ AI đề xuất mẫu vẽ (hoặc ✎ chọn mẫu).");
                if (t.KieuTd.Length > 0 && t.KieuTd != "lam_tay" && t.TdX.Trim().Length == 0 && t.TdY.Trim().Length == 0)
                    Them("thieu_td", VanDe.Vang, VanDe.TuXuLy, "", "Chưa có tiết diện — ✎ Sửa thông số → Tiết diện X / Y để trải phẳng.");

                double? T = So(t.T), Q = So(t.Q);
                if (T == null || T <= 0) Them("thieu_t", VanDe.Do, VanDe.Khach, "II.1", "Bản vẽ không ghi / không đọc được độ dày T.");
                if (Q == null || Q <= 0) Them("thieu_q", VanDe.Do, VanDe.Khach, "", "Bản vẽ không ghi / không đọc được số lượng Q.");
                if (string.IsNullOrWhiteSpace(t.M)) Them("thieu_m", VanDe.Do, VanDe.Khach, "II.1", "Bản vẽ không ghi vật liệu.");

                string loai = LoaiTv(t.M, t.Mat, catalogCsv, tv);
                List<object> canh = DsJson(t.CanhJson), lo = DsJson(t.LoJson);
                bool coBe = canh.Count > 0;

                // Độ dày cần xác nhận (QC B-1 chú ý, C-1)
                if (T is double td)
                {
                    if (td > 16 && coBe) Them("pl16", VanDe.Vang, VanDe.NoiBo, "B-1", "Tấm bẻ PL" + F(td) + " > 16 — xác nhận lại với xưởng.");
                    if (loai == "SS" && Math.Abs(td - 3.2) < 0.01) Them("ss32", VanDe.Vang, VanDe.NoiBo, "C-1", "SS t3.2: SPHC hay SS400 — xác nhận vật liệu (M=" + t.M + ").");
                    if (loai == "SUS" && td < 0.5) Them("sus05", VanDe.Vang, VanDe.NoiBo, "C-1", "SUS t" + F(td) + " < 0.5 — xác nhận lại.");
                    if (td < 3.2 && coBe && KichThuocLon(t.KichThuoc) > 2400)
                        Them("mong_dai", VanDe.Vang, VanDe.NoiBo, "B-1", "Tấm mỏng < PL3.2 có bẻ, kích thước " + t.KichThuoc + " > 2400 — xác nhận chiều dài đường bẻ với xưởng.");
                }

                // Lmin theo bảng TV
                List<DongTv> lm = T is double t2 && loai != null ? Lmin(tv, loai, t2) : new List<DongTv>();
                if (coBe && T != null && lm.Count == 0)
                    Them("khong_tv", VanDe.Vang, VanDe.NoiBo, "B-2-1", "Không có " + (loai ?? t.M) + " t" + t.T + " trong bảng TV — tra V / Lmin tay (bẻ thử?).");
                DongTv nho = lm.FirstOrDefault(), lon = lm.LastOrDefault();

                for (int i = 0; i < canh.Count; i++)
                {
                    if (!(canh[i] is JsonObject c)) continue;
                    string kieu = c.GetString("kieu") ?? "";
                    double goc = SoJ(c, "goc") ?? 90, r = SoJ(c, "r_trong") ?? 0;
                    string cho = "chỗ bẻ " + (i + 1);

                    if (kieu == "cuon")
                    {
                        if (T is double tc) KiemCuon(tc, r, cho, Them);
                        continue;
                    }
                    // R trong: bội 5, < 50, góc > 45° → ấn 1 lần; R khác → pitchi (làm tay)
                    if (r > 0 && goc > 45 && kieu != "pitchi")
                    {
                        if (Math.Abs(r % 5) < 0.001 && r < 50)
                            Them("r_" + i, VanDe.ThongTin, VanDe.TuXuLy, "B-2-4", cho + ": R" + F(r) + " góc " + F(goc) + "° → ấn 1 lần (O = 0.45 × T thực).");
                        else
                            Them("r_" + i, VanDe.Vang, VanDe.TuXuLy, "B-3", cho + ": R" + F(r) + " không thuộc ấn 1 lần (bội 5, < 50) → cuộn nếu đủ điều kiện, không thì ấn pitchi (làm tay SolidWorks).");
                    }
                    // Cạnh bẻ ngắn hơn Lmin
                    if (nho != null && c.Get("dai_ngoai") is List<object> dai)
                    {
                        double? ngan = dai.Select(SoObj).Where(x => x > 0).Select(x => (double?)x).Min();
                        if (ngan is double n)
                        {
                            if (n < nho.Lmin)
                                Them("canh_" + i, VanDe.Do, VanDe.Khach, "B-2-1",
                                     cho + ": cạnh " + F(n) + " mm ngắn hơn Lmin nhỏ nhất (V" + F(nho.V) + " Lmin " + F(nho.Lmin) + ") — không bẻ được, hỏi khách thêm phần thừa / đổi kích thước.", true);
                            else if (n < lon.Lmin)
                                Them("canh_" + i, VanDe.Vang, VanDe.TuXuLy, "B-2-1",
                                     cho + ": cạnh " + F(n) + " mm — chỉ dùng V có Lmin ≤ " + F(n) + " (" + string.Join(", ", lm.Where(x => x.Lmin <= n).Select(x => "V" + F(x.V) + " Lmin " + F(x.Lmin))) + ").");
                        }
                    }
                }

                // Lỗ — bảng đọc trước khi có cach_duong_be (11/11) thì nhắc đọc lại
                if (coBe && lo.OfType<JsonObject>().Any() && lo.OfType<JsonObject>().All(x => x.Get("cach_duong_be") == null))
                    Them("thieu_cach", VanDe.Vang, VanDe.NoiBo, "B-6-1", "Bảng tấm chưa có khoảng cách lỗ – đường bẻ (đọc bằng bản cũ) — phòng 1 ↻ Đọc lại tất cả để kiểm lỗ gần đường bẻ.");
                for (int i = 0; i < lo.Count; i++)
                {
                    if (!(lo[i] is JsonObject l)) continue;
                    double d = SoJ(l, "d") ?? 0;
                    string taro = (l.GetString("taro") ?? "").Trim();
                    string nhan = (taro.Length > 0 ? taro : "Ø" + F(d)) + " ×" + F(SoJ(l, "so_luong") ?? 1);

                    // Lỗ gần đường bẻ (QC B-6-1): cach_duong_be = mép lỗ → đường bẻ gần nhất; -1 / thiếu = không xác định
                    if (coBe && nho != null && SoJ(l, "cach_duong_be") is double cach && cach >= 0)
                    {
                        if (cach < nho.Lmin)
                            Them("lo_be_" + i, VanDe.Do, VanDe.Khach, "B-6-1",
                                 "Lỗ " + nhan + " cách đường bẻ " + F(cach) + " mm < Lmin " + F(nho.Lmin) + " (V" + F(nho.V) + ") — lỗ biến dạng sau bẻ: rạch slit, báo khách.", true);
                        else if (cach < lon.Lmin)
                            Them("lo_be_" + i, VanDe.Vang, VanDe.TuXuLy, "B-6-1",
                                 "Lỗ " + nhan + " cách đường bẻ " + F(cach) + " mm — biến dạng nếu dùng V có Lmin > " + F(cach) + "; chọn V nhỏ hoặc rạch slit.");
                    }
                    // Taro (QC E-1-2)
                    if (taro.Length > 0 && T is double tt)
                    {
                        char? kq = Taro(loai, taro, tt);
                        if (kq == null)
                            Them("taro_" + i, VanDe.Vang, VanDe.NoiBo, "E-1-2", "Taro " + taro + " trên " + t.M + " t" + t.T + ": không có trong bảng taro — xác nhận khả năng gia công.");
                        else if (kq == 'c')
                            Them("taro_" + i, VanDe.ThongTin, VanDe.TuXuLy, "E-1-2", "Taro " + taro + " t" + t.T + " → máy cố định.");
                        else if (kq == 'g')
                            Them("taro_" + i, VanDe.Vang, VanDe.NoiBo, "E-1-2", "Taro " + taro + " t" + t.T + " ô xám bảng taro — xác nhận lại với xưởng.");
                    }
                    // Lỗ nhỏ hơn độ dày (bảng mở lỗ laser chưa có — QC A-1-4)
                    else if (d > 0 && T is double t3 && d < t3)
                        Them("lo_nho_" + i, VanDe.Vang, VanDe.NoiBo, "A-1-4", "Lỗ Ø" + F(d) + " < t" + t.T + " — xác nhận khả năng mở lỗ laser (chưa có bảng mở lỗ).");
                }
            }
            if (tv.Count == 0)
                ra.Insert(0, new VanDe { Ma = "luat||khong_bang_tv", Muc = VanDe.Vang, Nhom = VanDe.NoiBo, NoiDung = "Không đọc được 01_Fname\\data\\trav_90.csv — bỏ qua luật Lmin." });
            return ra;
        }

        /// <summary>Cuộn (QC C-2): R ngoài = R trong + T. Máy nhỏ t ≤ 10, R ngoài 100 (t ≤ 3.2) / 120 … 300; máy lớn R &gt; 300.</summary>
        private static void KiemCuon(double t, double rTrong, string cho, Action<string, string, string, string, string, bool> them)
        {
            if (rTrong <= 0) { them("cuon_r", VanDe.Vang, VanDe.NoiBo, "C-2", cho + ": cuộn nhưng không đọc được R — xác nhận bán kính.", false); return; }
            double r = rTrong + t, rMin = t <= 3.2 ? 100 : 120;
            if (r > 300) return; // máy lớn (chiều dài trục < 2300)
            if (t > 10)
                them("cuon_" + cho, VanDe.Vang, VanDe.NoiBo, "C-2", cho + ": cuộn t" + F(t) + " R ngoài " + F(r) + " ≤ 300 — máy nhỏ chỉ tới t10, xác nhận máy.", false);
            else if (r < rMin)
                them("cuon_" + cho, VanDe.Vang, VanDe.TuXuLy, "C-2, B-3", cho + ": R ngoài " + F(r) + " < " + F(rMin) + " — không cuộn được, ấn pitchi (làm tay).", false);
        }

        // ---------- Gộp với lần kiểm trước ----------

        /// <summary>Giữ trạng thái / lý do / đã gửi của vấn đề cùng mã; vấn đề cũ không còn thì bỏ.</summary>
        public static List<VanDe> Gop(List<VanDe> cu, List<VanDe> moi)
        {
            var theoMa = (cu ?? new List<VanDe>()).GroupBy(v => v.Ma).ToDictionary(g => g.Key, g => g.First());
            foreach (VanDe v in moi)
                if (theoMa.TryGetValue(v.Ma, out VanDe c))
                {
                    v.TrangThai = c.TrangThai; v.LyDo = c.LyDo; v.DaGui = c.DaGui; v.TraLoi = c.TraLoi;
                    if (v.CauHoiJp.Length == 0) v.CauHoiJp = c.CauHoiJp;
                }
            return moi;
        }

        /// <summary>Mã vấn đề AI: tệp + tấm + đầu nội dung (chạy lại cùng ý thì giữ trạng thái).</summary>
        public static string MaAi(string tep, string tam, string noiDung)
        {
            string nd = new string((noiDung ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).Take(60).ToArray());
            return "ai|" + (tep ?? "").ToLowerInvariant() + "|" + (tam ?? "").ToLowerInvariant() + "|" + nd;
        }

        // ---------- Bản nháp gửi chuỗi đơn ----------

        /// <summary>
        /// Bản nháp お問い合わせ (lienLac = false) hoặc 連絡します (true): các vấn đề "Hỏi khách" còn mở, JP + dòng VI.
        /// Không có vấn đề → "".
        /// </summary>
        public static string BanNhap(string title, IEnumerable<VanDe> vanDe, bool lienLac)
        {
            var ds = vanDe.Where(v => v.Nhom == VanDe.Khach && v.ConMo && v.LienLac == lienLac).ToList();
            if (ds.Count == 0) return "";
            var sb = new StringBuilder();
            sb.Append(lienLac ? "【連絡します】" : "【お問い合わせ】").Append(title).Append('\n');
            for (int i = 0; i < ds.Count; i++)
            {
                VanDe v = ds[i];
                sb.Append(i + 1).Append(". ");
                if (v.Tam.Length > 0) sb.Append('[').Append(v.Tam).Append("] ");
                sb.Append(v.NoiDung).Append('\n'); // shortcut: chỉ tiếng Việt (11/12: chưa cần dịch Nhật) — cần thì dịch tay trước khi gửi khách
            }
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>Danh sách câu hỏi nội bộ còn mở (tiếng Việt) để chép gửi quản lý.</summary>
        public static string CauHoiNoiBo(string title, IEnumerable<VanDe> vanDe)
        {
            var ds = vanDe.Where(v => v.Nhom == VanDe.NoiBo && v.ConMo).ToList();
            if (ds.Count == 0) return "";
            return "Đơn " + title + " — cần xác nhận:\n" + string.Join("\n", ds.Select((v, i) => (i + 1) + ". " + v.BieuTuong + " " + (v.Tam.Length > 0 ? "[" + v.Tam + "] " : "") + v.NoiDung));
        }

        // ---------- AI rà theo QUY CHUẨN phòng 3 ----------

        public const int MaxTokens = 16000;

        public const string LoiDan =
@"Bạn là kỹ sư kiểm tra dữ liệu triển khai kim loại tấm của OHTA (phòng Kiểm tra). Đầu vào: bản vẽ khách (PDF hoặc tóm tắt DXF), bảng tấm phòng Đọc bản vẽ đã được người dùng xác nhận, và các vấn đề luật tự động đã tìm.
Tìm THÊM các vấn đề luật chưa bắt, theo QUY CHUẨN ở khối sau — chỉ vấn đề có thật trên bản vẽ này, không bịa, không lặp lại vấn đề luật đã có:
- Thiếu kích thước cần để triển khai (bao, cạnh bẻ, vị trí lỗ, góc), kích thước cộng không khớp tổng.
- Mâu thuẫn giữa các hình chiếu, giữa hình và ghi chú / bảng, giữa bản vẽ và bảng tấm (vật liệu, độ dày, số lượng).
- Bẻ hộp 3–4 cạnh (c > 150, c > a hoặc b), bẻ C khi a < c (bẻ W), cạnh chéo / bẻ một phần cần slit (B-6), vát (D-2), sima / mặt đẹp / HL (B-5), cuộn (C-2).
- Luật công ty đặc biệt nếu đơn thuộc công ty đó; vật liệu / Z không đúng bảng C-1, C-2.
Mỗi vấn đề: tam (số bản vẽ hoặc tên tấm, rỗng nếu chung), muc (do = chặn triển khai; vang = cần xác nhận; info = chỉ ghi chú), nhom (tu_xu_ly = làm theo QC không cần hỏi; noi_bo = hỏi quản lý; khach = hỏi khách), muc_qc (vd B-6-1), lien_lac (true nếu là báo slit / phần thừa — bản 連絡します; false nếu là câu hỏi お問い合わせ).
noi_dung: tiếng Việt, MỘT ý, tối đa ~20 từ, đúng trọng tâm: chỗ nào + thiếu / sai gì (+ số). Nhóm khach / noi_bo viết thành câu hỏi trực tiếp (vd ""Vị trí lỗ M5 cách mép trái bao nhiêu?""). Không giải thích lý do, không suy đoán, không lặp ý; nhiều ý cùng một chỗ thì gộp. Chỉ nêu vấn đề làm thay đổi cách triển khai.";

        /// <summary>Kiểm lại bằng chữ (không gửi bản vẽ): sau khi có câu trả lời của khách / quản lý.</summary>
        public const string LoiDanChu =
@"Bạn là kỹ sư kiểm tra dữ liệu triển khai kim loại tấm của OHTA (phòng Kiểm tra). LẦN NÀY KHÔNG CÓ BẢN VẼ — chỉ có bảng tấm (đã qua người sửa), các vấn đề đã tìm và câu trả lời của khách / quản lý.
Tìm vấn đề MỚI phát sinh từ câu trả lời hoặc từ số liệu bảng tấm, theo QUY CHUẨN ở khối sau:
- Câu trả lời mâu thuẫn với bảng tấm / với câu trả lời khác, hoặc còn thiếu để triển khai (vd trả lời đổi vật liệu nhưng T không có trong bảng).
- Câu trả lời làm phát sinh việc cần làm (slit, phần thừa, đổi V, làm tay).
KHÔNG lặp vấn đề đã có, không hỏi lại ý đã được trả lời, không đoán nội dung bản vẽ. Không có gì mới → van_de rỗng.
Mỗi vấn đề: tam, muc (do / vang / info), nhom (tu_xu_ly / noi_bo / khach), muc_qc, lien_lac, noi_dung (tiếng Việt, MỘT ý, ~20 từ, nhóm khach / noi_bo viết thành câu hỏi trực tiếp).";

        /// <summary>Mẫu JSON một vấn đề (dùng chung: đọc bản vẽ, kiểm lại bằng chữ).</summary>
        public static JsonObject SchemaVanDe()
        {
            JsonObject s = Kieu("string");
            return Obj(("tam", s), ("muc", LuaChon("do", "vang", "info")), ("nhom", LuaChon("tu_xu_ly", "noi_bo", "khach")),
                       ("muc_qc", s), ("noi_dung", s), ("lien_lac", Kieu("boolean")));
        }

        /// <summary>Mảng van_de của AI → vấn đề (mã theo tệp + tấm + nội dung, nguồn "doc" / "ai").</summary>
        public static List<VanDe> DocDsVanDe(List<object> ds, string tenTep, string nguon)
        {
            var kq = new List<VanDe>();
            foreach (JsonObject x in ds.OfType<JsonObject>())
            {
                string tam = x.GetString("tam") ?? "", noiDung = x.GetString("noi_dung") ?? "";
                if (noiDung.Trim().Length == 0) continue;
                kq.Add(new VanDe
                {
                    Ma = MaAi(tenTep, tam, noiDung), Tam = tam, NoiDung = noiDung, Nguon = nguon,
                    Muc = Mot(x.GetString("muc"), VanDe.Vang, VanDe.Do, VanDe.Vang, VanDe.ThongTin),
                    Nhom = Mot(x.GetString("nhom"), VanDe.NoiBo, VanDe.TuXuLy, VanDe.NoiBo, VanDe.Khach),
                    MucQc = x.GetString("muc_qc") ?? "", LienLac = x.Get("lien_lac") is bool b && b,
                });
            }
            return kq;
        }

        /// <summary>Phần chữ gửi kèm bản vẽ: bảng tấm của tệp + vấn đề luật (mã, nhóm, nội dung) + công ty.</summary>
        public static string DauVao(string tenTep, string congTy, IEnumerable<TamAi> tam, IEnumerable<VanDe> luat, IEnumerable<VanDe> daTraLoi = null)
        {
            var sb = new StringBuilder();
            sb.Append("Tệp: ").Append(tenTep).Append('\n');
            sb.Append("Công ty đặc biệt: ").Append(string.IsNullOrWhiteSpace(congTy) ? "(không)" : congTy).Append("\n\nBảng tấm (đã xác nhận):\n");
            foreach (TamAi t in tam)
                sb.Append("- ").Append(TenTam(t)).Append(": M=").Append(t.M).Append(" Z=").Append(t.Z).Append(" T=").Append(t.T).Append(" Q=").Append(t.Q)
                  .Append(" mặt=").Append(t.Mat).Append(" kích thước=").Append(t.KichThuoc).Append(" dung sai=").Append(t.DungSai)
                  .Append(" bẻ=").Append(t.CanhJson).Append(" lỗ=").Append(t.LoJson).Append(" ghi chú=").Append(t.GhiChuJp)
                  .Append(t.KieuTd.Length > 0 ? " tiết diện " + t.KieuTd + " X=[" + t.TdX + "] Y=[" + t.TdY + "]" : "").Append('\n');
            sb.Append("\nVấn đề luật đã tìm:\n");
            var ds = luat.ToList();
            if (ds.Count == 0) sb.Append("(không có)\n");
            foreach (VanDe v in ds) sb.Append("- ma=").Append(v.Ma).Append(" | ").Append(v.Nhom).Append(" | ").Append(v.Tam).Append(" | ").Append(v.NoiDung).Append('\n');
            var tl = (daTraLoi ?? Enumerable.Empty<VanDe>()).Where(v => v.TraLoi.Trim().Length > 0).ToList();
            if (tl.Count > 0)
            {
                sb.Append("\nĐã có câu trả lời (khách / quản lý / người triển khai) — coi là dữ liệu đúng, KHÔNG hỏi lại các ý này:\n");
                foreach (VanDe v in tl) sb.Append("- ").Append(v.Tam).Append(" | hỏi: ").Append(v.NoiDung).Append(" | trả lời: ").Append(v.TraLoi.Trim()).Append('\n');
            }
            return sb.ToString();
        }

        public static JsonObject Schema() => Obj(("van_de", Mang(SchemaVanDe())));

        /// <summary>Body POST /v1/messages — system = lời dặn (mặc định LoiDan; LoiDanChu khi kiểm lại bằng chữ) + QC có cache.</summary>
        public static string Body(string model, string quyChuan, string pdfBase64, string dauVao, string tomTatDxf, string loiDan = null)
        {
            var body = new JsonObject();
            body.Set("model", model);
            body.Set("max_tokens", MaxTokens);
            if (PhongDocBanVe.CoFallback(model)) body.Set("fallbacks", "default");
            var format = new JsonObject();
            format.Set("type", "json_schema");
            format.Set("schema", Schema());
            var oc = new JsonObject();
            oc.Set("effort", "high");
            oc.Set("format", format);
            body.Set("output_config", oc);
            var cache = new JsonObject();
            cache.Set("type", "ephemeral");
            body.Set("system", new List<object> { Khoi(loiDan ?? LoiDan, null), Khoi(quyChuan, cache) });
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
            public List<VanDe> VanDe = new List<VanDe>();
            public long Vao, GhiCache, DocCache, Ra;
            public string Loi;
        }

        /// <summary>Đọc phản hồi Claude; lỗi API / từ chối / cắt dùng chung cách báo với phòng 2.</summary>
        public static PhanHoi DocPhanHoi(string json, string tenTep)
        {
            var kq = new PhanHoi();
            PhongDocBanVe.PhanHoi p2 = PhongDocBanVe.DocPhanHoi(json); // lấy token + lỗi chung; "không đúng mẫu" của phòng 2 bỏ qua
            kq.Vao = p2.Vao; kq.GhiCache = p2.GhiCache; kq.DocCache = p2.DocCache; kq.Ra = p2.Ra;
            JsonObject goc;
            try { goc = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { goc = null; }
            string text = (goc?.Get("content") as List<object>)?.OfType<JsonObject>().FirstOrDefault(x => x.GetString("type") == "text")?.GetString("text");
            JsonObject nd;
            try { nd = text == null ? null : JsonLite.Parse(text) as JsonObject; } catch (Exception) { nd = null; }
            if (nd?.Get("van_de") is List<object> ds)
            {
                kq.VanDe = DocDsVanDe(ds, tenTep, "ai");
                return kq;
            }
            kq.Loi = p2.Loi ?? "Phản hồi AI không đúng mẫu";
            return kq;
        }

        // ---------- JSON kiem_tra.json ----------

        public static string Ghi(KiemTra kt)
        {
            var o = new JsonObject();
            o.Set("tu_xac_nhan", kt.TuXacNhan ?? "");
            o.Set("xac_nhan", kt.XacNhan ?? "");
            o.Set("luc", kt.Luc ?? "");
            o.Set("token_vao", new JsonNumber(kt.TokenVao.ToString(CultureInfo.InvariantCulture)));
            o.Set("token_ra", new JsonNumber(kt.TokenRa.ToString(CultureInfo.InvariantCulture)));
            o.Set("tien", new JsonNumber(kt.Tien.ToString(CultureInfo.InvariantCulture)));
            o.Set("van_de", kt.VanDe.Select(v => (object)GhiVanDe(v)).ToList());
            return JsonLite.Serialize(o);
        }

        public static JsonObject GhiVanDe(VanDe v)
        {
            var x = new JsonObject();
            x.Set("ma", v.Ma); x.Set("muc", v.Muc); x.Set("nhom", v.Nhom); x.Set("tam", v.Tam); x.Set("muc_qc", v.MucQc);
            x.Set("noi_dung", v.NoiDung); x.Set("cau_hoi_jp", v.CauHoiJp); x.Set("lien_lac", v.LienLac); x.Set("nguon", v.Nguon);
            x.Set("trang_thai", v.TrangThai); x.Set("ly_do", v.LyDo); x.Set("tra_loi", v.TraLoi ?? ""); x.Set("da_gui", v.DaGui);
            return x;
        }

        public static VanDe DocVanDe(JsonObject x) => new VanDe
        {
            Ma = x.GetString("ma") ?? "", Muc = x.GetString("muc") ?? VanDe.Vang, Nhom = x.GetString("nhom") ?? VanDe.NoiBo, Tam = x.GetString("tam") ?? "",
            MucQc = x.GetString("muc_qc") ?? "", NoiDung = x.GetString("noi_dung") ?? "", CauHoiJp = x.GetString("cau_hoi_jp") ?? "",
            LienLac = x.Get("lien_lac") is bool b && b, Nguon = x.GetString("nguon") ?? "luat", TrangThai = x.GetString("trang_thai") ?? "",
            LyDo = x.GetString("ly_do") ?? "", TraLoi = x.GetString("tra_loi") ?? "", DaGui = x.Get("da_gui") is bool g && g,
        };

        public static KiemTra Doc(string json)
        {
            JsonObject o;
            try { o = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { return null; }
            if (o == null) return null;
            var kt = new KiemTra
            {
                TuXacNhan = o.GetString("tu_xac_nhan") ?? "", XacNhan = o.GetString("xac_nhan") ?? "", Luc = o.GetString("luc") ?? "",
                TokenVao = (long)(SoObj(o.Get("token_vao")) ?? 0), TokenRa = (long)(SoObj(o.Get("token_ra")) ?? 0), Tien = (decimal)(SoObj(o.Get("tien")) ?? 0),
            };
            foreach (JsonObject x in (o.Get("van_de") as List<object> ?? new List<object>()).OfType<JsonObject>()) kt.VanDe.Add(DocVanDe(x));
            return kt;
        }

        // ---------- tiện ích ----------

        public static string TenTam(TamAi t) => t.SoBanVe.Length > 0 ? t.SoBanVe + (t.Ten.Length > 0 ? " " + t.Ten : "") : t.Ten.Length > 0 ? t.Ten : t.Tep;

        private static string Mot(string v, string macDinh, params string[] hopLe) => hopLe.Contains(v) ? v : macDinh;

        private static List<object> DsJson(string json)
        {
            try { return JsonLite.Parse(json ?? "[]") as List<object> ?? new List<object>(); } catch (Exception) { return new List<object>(); }
        }

        /// <summary>Số trong chữ ("2.3", "t2.3", "10枚"…) — lấy cụm số đầu tiên.</summary>
        public static double? So(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            string chu = s.Trim().Replace(',', '.');
            int a = 0;
            while (a < chu.Length && !char.IsDigit(chu[a])) a++;
            int b = a;
            while (b < chu.Length && (char.IsDigit(chu[b]) || chu[b] == '.')) b++;
            return b > a && double.TryParse(chu.Substring(a, b - a), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : (double?)null;
        }

        private static double? SoObj(object v) => v is JsonNumber n ? So(n.Raw) : v is string s ? So(s) : null;

        private static double? SoJ(JsonObject o, string k)
        {
            object v = o.Get(k);
            if (v is JsonNumber n && double.TryParse(n.Raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d; // giữ dấu âm (-1 = không xác định)
            return v is string s ? So(s) : null;
        }

        /// <summary>Cạnh lớn nhất của "dài x rộng".</summary>
        private static double KichThuocLon(string kt) =>
            (kt ?? "").Split('x', 'X', '×', '*').Select(So).Where(x => x != null).Select(x => x.Value).DefaultIfEmpty(0).Max();

        private static string F(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);

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
