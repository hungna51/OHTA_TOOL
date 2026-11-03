using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ZwCadPlugin.Core
{
    /// <summary>Một lần sửa một dòng CSV. Trường null = GIỮ NGUYÊN ô cũ.</summary>
    public sealed class SuaCsv
    {
        /// <summary>Bango lúc quét — dùng để TÌM dòng (trước khi đổi số).</summary>
        public string GocBango { get; set; }
        public string BangoMoi { get; set; }
        public string Q { get; set; }
        public string M { get; set; }
        public string T { get; set; }
        /// <summary>Cột V (GC). "" = tấm đơn (xoá cột V); null = giữ nguyên.</summary>
        public string GC { get; set; }
    }

    /// <summary>
    /// File CSV mà GRO ghi ra: mỗi dòng
    ///     &lt;tên&gt;-&lt;bango&gt;,Q,M,T,&lt;tên&gt;-[,V]
    /// V = "m" (bẻ) / "k" (vát) / "*" (từ 2 loại gia công khác nhau trở lên), tấm đơn thì không có cột V.
    /// Ghi lại đúng kiểu GRO ghi (gro--csv-ghi-de): UTF-8 có BOM, mỗi dòng kết thúc CRLF.
    /// Không phụ thuộc ZWCAD.
    /// </summary>
    public sealed class CsvGro
    {
        public const int CotN = 0, CotQ = 1, CotM = 2, CotT = 3, CotX = 4, CotV = 5;

        public List<string> Dong { get; } = new List<string>();

        public static CsvGro Doc(byte[] data)
        {
            var csv = new CsvGro();
            string s = GiaiMa(data ?? new byte[0]);
            foreach (string d in s.Replace("\r\n", "\n").Split('\n'))
                csv.Dong.Add(d);
            // dong rong cuoi do CRLF cuoi file sinh ra
            while (csv.Dong.Count > 0 && csv.Dong[csv.Dong.Count - 1].Length == 0)
                csv.Dong.RemoveAt(csv.Dong.Count - 1);
            return csv;
        }

        /// <summary>
        /// GRO/BG ghi UTF-8 có BOM (mục 7). File rất cũ ghi bằng write-line theo codepage
        /// máy (CP932) — không phải UTF-8 hợp lệ thì đọc theo CP932.
        /// </summary>
        private static string GiaiMa(byte[] b)
        {
            if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
                return new UTF8Encoding(false).GetString(b, 3, b.Length - 3);
            try
            {
                return new UTF8Encoding(false, true).GetString(b);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(932).GetString(b);
            }
        }

        public byte[] Ghi()
        {
            var sb = new StringBuilder();
            foreach (string d in Dong) sb.Append(d).Append("\r\n");
            byte[] than = new UTF8Encoding(false).GetBytes(sb.ToString());
            var kq = new byte[than.Length + 3];
            kq[0] = 0xEF; kq[1] = 0xBB; kq[2] = 0xBF;
            Buffer.BlockCopy(than, 0, kq, 3, than.Length);
            return kq;
        }

        public static string[] Tach(string dong) => (dong ?? string.Empty).Split(',');

        /// <summary>Bango của một dòng = đoạn số cuối của cột N; không đọc được thì null.</summary>
        public static string BangoCua(string dong)
        {
            string n = Tach(dong)[CotN].Trim();
            return GroText.TryDocBangoCuoi(n, out string b) ? b : null;
        }

        /// <summary>Chỉ số dòng có bango này, -1 nếu không có.</summary>
        public int TimDong(string bango)
        {
            if (string.IsNullOrWhiteSpace(bango)) return -1;
            string b = bango.Trim();
            for (int i = 0; i < Dong.Count; i++)
                if (BangoCua(Dong[i]) == b) return i;
            return -1;
        }

        /// <summary>
        /// Chỉ số các dòng có bango KHÔNG còn trong <paramref name="bangoTrenBanVe"/> — tấm đã
        /// xoá khỏi bản vẽ mà CSV vẫn giữ dòng (mục 106). Dòng không đọc được bango (trống,
        /// dòng lạ) KHÔNG bao giờ bị coi là thừa.
        /// </summary>
        public List<int> DongThua(IEnumerable<string> bangoTrenBanVe)
        {
            var co = new HashSet<string>((bangoTrenBanVe ?? Enumerable.Empty<string>())
                                         .Where(b => !string.IsNullOrWhiteSpace(b))
                                         .Select(b => b.Trim()));
            var kq = new List<int>();
            for (int i = 0; i < Dong.Count; i++)
            {
                string b = BangoCua(Dong[i]);
                if (b != null && !co.Contains(b)) kq.Add(i);
            }
            return kq;
        }

        /// <summary>
        /// Dòng CSV mới ĐÚNG kiểu c:gro ghi (mục 107):
        ///     &lt;don&gt;-&lt;bango&gt;,Q,M,T,&lt;don&gt;-[,V]
        /// don = TenDon(DWGNAME). V chỉ thêm khi có gia công (tấm đơn: 5 cột, không dấu phẩy cuối).
        /// </summary>
        public static string TaoDong(string don, string bango, string q, string m, string t, string gc)
        {
            var f = new List<string>
            {
                (don ?? string.Empty) + "-" + (bango ?? string.Empty).Trim(),
                (q ?? string.Empty).Trim(), (m ?? string.Empty).Trim(), (t ?? string.Empty).Trim(),
                (don ?? string.Empty) + "-",
            };
            string v = ChuanHoaGC(gc);
            if (v.Length > 0) f.Add(v);
            return string.Join(",", f);
        }

        /// <summary>
        /// Cột M của CSV theo quy tắc gro--fields: T &gt; 3.2 -&gt; SS400, còn lại SPHC.
        /// KHÁC F-name (ngưỡng &gt;= 4.5 của lệnh FN) nên KHÔNG chép M của F-name khi nó là
        /// SS400 / SPHC. M của F-name là vật liệu khác (tấm CPL "ムキ 縞板", SUS...) thì giữ.
        /// </summary>
        public static string VatLieuCsv(string t, string mFname)
        {
            string m = (mFname ?? string.Empty).Trim();
            if (m.Length > 0 && !m.Equals("SS400", StringComparison.OrdinalIgnoreCase)
                             && !m.Equals("SPHC", StringComparison.OrdinalIgnoreCase))
                return m;
            if (!double.TryParse((t ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Float,
                                 CultureInfo.InvariantCulture, out double v))
                return m;
            return v > 3.2 + 1e-9 ? "SS400" : "SPHC";
        }

        /// <summary>
        /// Định dạng T mẫu của file: T của dòng đầu tiên có số (lisp ghi (rtos v 2 2): "6.00"
        /// hoặc "6" tuỳ DIMZIN của máy). File chưa có dòng nào thì "0.00".
        /// </summary>
        public string MauT()
        {
            foreach (string d in Dong)
            {
                string[] f = Tach(d);
                if (f.Length > CotT && BangoCua(d) != null &&
                    double.TryParse(f[CotT].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    return f[CotT].Trim();
            }
            return "0.00";
        }

        /// <summary>
        /// Chèn dòng theo thứ tự bango TĂNG DẦN (GRO ghi CSV theo thứ tự bango): đặt trước dòng
        /// đầu tiên có bango lớn hơn; không có thì nối cuối. Trả về chỉ số đã chèn.
        /// </summary>
        public int ChenTheoBango(string dong)
        {
            int moi = int.TryParse(BangoCua(dong), out int n) ? n : int.MaxValue;
            for (int i = 0; i < Dong.Count; i++)
            {
                if (int.TryParse(BangoCua(Dong[i]), out int b) && b > moi)
                {
                    Dong.Insert(i, dong);
                    return i;
                }
            }
            Dong.Add(dong);
            return Dong.Count - 1;
        }

        /// <summary>Xoá các dòng theo chỉ số (thứ tự / trùng lặp không quan trọng). Thứ tự các dòng còn lại giữ nguyên.</summary>
        public void XoaDong(IEnumerable<int> chiSo)
        {
            foreach (int i in (chiSo ?? Enumerable.Empty<int>()).Distinct().OrderByDescending(x => x))
            {
                if (i >= 0 && i < Dong.Count) Dong.RemoveAt(i);
            }
        }

        /// <summary>Cột V (gia công) của dòng; tấm đơn / không có cột thì "".</summary>
        public string LayGC(int i)
        {
            string[] f = Tach(Dong[i]);
            return f.Length > CotV ? f[CotV].Trim() : string.Empty;
        }

        public string LayQ(int i)
        {
            string[] f = Tach(Dong[i]);
            return f.Length > CotQ ? f[CotQ].Trim() : string.Empty;
        }

        public string LayT(int i)
        {
            string[] f = Tach(Dong[i]);
            return f.Length > CotT ? f[CotT].Trim() : string.Empty;
        }

        /// <summary>
        /// Áp một loạt sửa. TÌM TẤT CẢ các dòng theo bango GỐC trước rồi mới sửa — nên đổi
        /// chéo số (3 &lt;-&gt; 5) vẫn đúng. Thứ tự dòng trong file giữ nguyên.
        /// </summary>
        /// <returns>Số dòng đã sửa.</returns>
        public int ApDung(IList<SuaCsv> ds, List<string> canhBao)
        {
            var viTri = new List<KeyValuePair<int, SuaCsv>>();
            foreach (SuaCsv s in ds)
            {
                int i = TimDong(s.GocBango);
                if (i < 0) canhBao.Add("CSV không có dòng của tấm " + s.GocBango + " — chỉ sửa bản vẽ.");
                else if (Tach(Dong[i]).Length <= CotT) canhBao.Add("CSV dòng " + (i + 1) + " thiếu cột, bỏ qua: " + Dong[i]);
                else viTri.Add(new KeyValuePair<int, SuaCsv>(i, s));
            }

            foreach (var kv in viTri)
            {
                string[] f = Tach(Dong[kv.Key]);
                SuaCsv s = kv.Value;
                if (s.BangoMoi != null) f[CotN] = GroText.ThayBangoCuoi(f[CotN], s.BangoMoi.Trim());
                if (s.Q != null) f[CotQ] = s.Q.Trim();
                if (s.M != null) f[CotM] = s.M.Trim();
                if (s.T != null) f[CotT] = DinhDangT(s.T, f[CotT]);
                if (s.GC != null) f = DatGC(f, ChuanHoaGC(s.GC), kv.Key, canhBao);
                Dong[kv.Key] = string.Join(",", f);
            }

            // Doi so co the dung mot dong CSV KHONG co tren ban ve (tam da xoa) cung so.
            foreach (var g in Dong.Select((d, i) => new { b = BangoCua(d), i })
                                  .Where(x => x.b != null)
                                  .GroupBy(x => x.b)
                                  .Where(g => g.Count() > 1))
                canhBao.Add("CSV có " + g.Count() + " dòng cùng bango " + g.Key + " (dòng " +
                            string.Join(", ", g.Select(x => x.i + 1)) + ").");
            return viTri.Count;
        }

        /// <summary>
        /// Các mã gia công (cột V / GC), theo thứ tự hiển thị. Trống = tấm đơn.
        /// m, k, * do GRO tự ghi; s, t, r người dùng ghi tay trong bảng DST.
        /// </summary>
        public static readonly string[] MaGC = { "m", "k", "*", "s", "t", "r" };

        public static string TenGC(string ma)
        {
            switch (ChuanHoaGC(ma))
            {
                case "": return "tấm đơn";
                case "m": return "bẻ";
                case "k": return "vát";
                case "*": return "từ 2 loại gia công khác nhau trở lên";
                case "s": return "gia công sara";
                case "t": return "gia công M-タップ";
                case "r": return "gia công cuộn";
                default: return "?";
            }
        }

        /// <summary>Gợi ý ngắn cho ô nhập GC.</summary>
        public static string GoiYGC => "để trống = tấm đơn; " +
            string.Join(", ", MaGC.Select(m => m + " = " + TenGC(m)));

        /// <summary>GC hợp lệ: trống hoặc MỘT mã trong MaGC. Không phân biệt hoa thường.</summary>
        public static bool LaGCHopLe(string gc)
        {
            string v = ChuanHoaGC(gc);
            return v.Length == 0 || MaGC.Contains(v);
        }

        /// <summary>
        /// Dòng thống kê MỘT DÒNG cho thanh dưới bảng: "Tổng 81 │ Đơn 60 │ m 10 · k 5 │ Lỗi 3 │ Cô đơn 0".
        /// Chỉ liệt kê mã gia công có ít nhất 1 tấm để dòng luôn ngắn. chiTiet = bản đầy đủ
        /// nhiều dòng (tên từng loại) để làm tooltip.
        /// </summary>
        public static string ThongKe(IEnumerable<string> gcs, int loi, int coDon, out string chiTiet)
        {
            var ds = gcs.Select(ChuanHoaGC).ToList();
            int don = ds.Count(g => g.Length == 0);
            var gc = MaGC.Select(m => new { m, n = ds.Count(g => g == m) }).Where(x => x.n > 0).ToList();
            int khac = ds.Count(g => g.Length > 0 && !MaGC.Contains(g));

            var phan = new List<string> { "Tổng " + ds.Count, "Đơn " + don };
            if (gc.Count > 0 || khac > 0)
                phan.Add(string.Join(" · ", gc.Select(x => x.m + " " + x.n)
                                              .Concat(khac > 0 ? new[] { "? " + khac } : new string[0])));
            phan.Add("Lỗi " + loi);
            phan.Add("Cô đơn " + coDon);

            var ct = new List<string> { "Tổng: " + ds.Count + " tấm", "Tấm đơn: " + don };
            ct.AddRange(MaGC.Select(m => m + " (" + TenGC(m) + "): " + ds.Count(g => g == m)));
            if (khac > 0) ct.Add("GC không hợp lệ: " + khac);
            ct.Add("Tấm lỗi: " + loi);
            ct.Add("Hình cô đơn: " + coDon);
            chiTiet = string.Join(Environment.NewLine, ct);
            return string.Join(" │ ", phan);
        }

        public static string ChuanHoaGC(string gc) => (gc ?? string.Empty).Trim().ToLowerInvariant();

        /// <summary>
        /// Đặt cột V đúng kiểu GRO ghi: tấm đơn thì dòng KHÔNG có cột V (5 cột, không có dấu
        /// phẩy cuối), có gia công thì thêm cột thứ 6.
        /// </summary>
        private static string[] DatGC(string[] f, string v, int dong, List<string> canhBao)
        {
            var ds = f.ToList();
            if (ds.Count <= CotX)
            {
                canhBao.Add("CSV dòng " + (dong + 1) + " thiếu cột X, không ghi được GC.");
                return f;
            }
            if (v.Length == 0)
            {
                if (ds.Count == CotV + 1) ds.RemoveAt(CotV);
                else if (ds.Count > CotV + 1) ds[CotV] = string.Empty;
            }
            else if (ds.Count > CotV) ds[CotV] = v;
            else ds.Add(v);
            return ds.ToArray();
        }

        /// <summary>
        /// Ghi T theo ĐÚNG kiểu của ô cũ: lisp ghi bằng (rtos v 2 2) nên ra "6.00" hoặc "6"
        /// tuỳ biến DIMZIN của máy. Ô cũ có N chữ số lẻ thì giữ N; không có dấu chấm thì
        /// ghi gọn nhất (bỏ số 0 thừa, tối đa 2 số lẻ).
        /// </summary>
        public static string DinhDangT(string tMoi, string tCu)
        {
            string s = (tMoi ?? string.Empty).Trim().Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return (tMoi ?? string.Empty).Trim();
            string cu = (tCu ?? string.Empty).Trim();
            int dot = cu.IndexOf('.');
            if (dot >= 0)
                return v.ToString("F" + (cu.Length - dot - 1), CultureInfo.InvariantCulture);
            return v.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Tên "đơn" của bản vẽ, y như lisp:
        ///     (vl-string-right-trim "- S A s a . d f x g w" (getvar "DWGNAME"))
        /// tức là cắt khỏi ĐUÔI mọi ký tự thuộc tập {- space S A s a . d f x g w}.
        /// File CSV = DWGPREFIX + tên đơn + ".csv".
        /// </summary>
        public static string TenDon(string dwgName)
        {
            return (dwgName ?? string.Empty).TrimEnd("- SAsa.dfxgw".ToCharArray());
        }
    }
}
