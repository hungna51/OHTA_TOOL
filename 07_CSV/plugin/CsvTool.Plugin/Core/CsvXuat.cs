using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvTool.Core
{
    /// <summary>Thứ tự dòng CSV (= csvmode của lisp).</summary>
    public enum KieuSapXep { TraiSangPhai = 1, TrenXuongDuoi = 2, ChuCai = 3 }

    /// <summary>Ráp dòng, đọc / ghi data CSV (34 cột, chung với web price-calc) và file cuối &lt;đơn&gt;.csv (10 cột).</summary>
    public static class CsvXuat
    {
        public const string TieuDe10 = "エクスポート用,数量,材質,板厚,noと安全,曲げ,X,Y,,単価";

        /// <summary>
        /// File data chung với web price-calc. Loại pitchi và cuộn ghi trong cột Loai gia cong (m(non), r1, r2…),
        /// cột cuối Gia tay = 1 khi Gia bao gia là giá sửa tay.
        /// </summary>
        public const string TieuDe34 =
            "Ten tam,Vat lieu,Do day,X (mm),Y (mm),So luong," +
            "Lo nho,Lo lon,Lo Tappu,Lo Sara," +
            "Pitchi - So lan,Pitchi - Chieu dai (mm)," +
            "Loai gia cong,Vat (mm)," +
            "Be1 - So duong,Be1 - Dai (mm),Be2 - So duong,Be2 - Dai (mm)," +
            "Be3 - So duong,Be3 - Dai (mm),Be4 - So duong,Be4 - Dai (mm)," +
            "Be5 - So duong,Be5 - Dai (mm),Be6 - So duong,Be6 - Dai (mm)," +
            "Be7 - So duong,Be7 - Dai (mm),Be8 - So duong,Be8 - Dai (mm)," +
            "Che do tinh,Gia bao gia,Tien vat lieu(nhap san),Gia tay";

        /// <summary>Data cũ (lisp, tới 11/2026): còn cột Loai cuon và Uu dai(%). Chỉ để nhận và chuyển file cũ.</summary>
        public const string TieuDe35 =
            "Ten tam,Vat lieu,Do day,X (mm),Y (mm),So luong," +
            "Lo nho,Lo lon,Lo Tappu,Lo Sara," +
            "Pitchi - So lan,Pitchi - Chieu dai (mm)," +
            "Loai gia cong,Loai cuon,Vat (mm)," +
            "Be1 - So duong,Be1 - Dai (mm),Be2 - So duong,Be2 - Dai (mm)," +
            "Be3 - So duong,Be3 - Dai (mm),Be4 - So duong,Be4 - Dai (mm)," +
            "Be5 - So duong,Be5 - Dai (mm),Be6 - So duong,Be6 - Dai (mm)," +
            "Be7 - So duong,Be7 - Dai (mm),Be8 - So duong,Be8 - Dai (mm)," +
            "Che do tinh,Gia bao gia,Tien vat lieu(nhap san),Uu dai(%)";

        /// <summary>CSV--csv-field: bọc nháy kép khi có dấu phẩy hoặc nháy.</summary>
        public static string O(string s)
        {
            s = s ?? "";
            return s.Contains(",") || s.Contains("\"") ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }

        private static string O(int n) => n.ToString(CultureInfo.InvariantCulture);

        /// <summary>Mã cột 曲げ gửi nhà máy: 1 loại → mã của nó, từ 2 loại → "*", không có → "".</summary>
        public static string MaMage(string loaiGiaCong)
        {
            var ma = new List<string>();
            foreach (string p in (loaiGiaCong ?? "").Split(','))
            {
                string x = p.Trim().ToLowerInvariant();
                string m = x == "m" || x.StartsWith("m(") ? "m" : x == "r1" || x == "r2" ? "r" :
                           x == "k" || x == "s" || x == "t" ? x : null;
                if (m != null && !ma.Contains(m)) ma.Add(m);
            }
            return ma.Count == 0 ? "" : ma.Count == 1 ? ma[0] : "*";
        }

        /// <summary>CSV--coil-type: "R2" → "2", "R1" → "1", không có → "0".</summary>
        public static string LoaiCuon(string loaiGiaCong)
        {
            string u = (loaiGiaCong ?? "").ToUpperInvariant();
            return u.Contains("R2") ? "2" : u.Contains("R1") ? "1" : "0";
        }

        // ── Mã gia công (cột Loai gia cong): "m, m(non), r1, t, s, k" ──

        /// <summary>Tên trong mã m(...), cùng thứ tự TinhGia.DsLoaiPitchi.</summary>
        public static readonly string[] MaPitchi = { "pitchi", "non", "nonlech", "kakumaru", "kakumarulech" };

        public static List<string> TachMa(string gc) =>
            (gc ?? "").Split(',').Select(x => x.Trim()).Where(x => x != "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        private static bool LaMaPitchi(string x) => x.StartsWith("m(", StringComparison.OrdinalIgnoreCase);

        public static bool CoPitchi(string gc) => TachMa(gc).Any(LaMaPitchi);

        /// <summary>m(non) → "Non" (tên trong TinhGia.DsLoaiPitchi); không có hoặc lạ → "Pitchi".</summary>
        public static string LoaiPitchiCua(string gc)
        {
            string ma = TachMa(gc).FirstOrDefault(LaMaPitchi);
            int i = ma == null ? -1 : Array.IndexOf(MaPitchi, ma.Substring(2).TrimEnd(')').Trim().ToLowerInvariant());
            return TinhGia.DsLoaiPitchi[Math.Max(i, 0)];
        }

        /// <summary>Đặt loại pitchi: thay mã m(...) đang có, chưa có thì thêm sau "m".</summary>
        public static string DatLoaiPitchi(string gc, string loai)
        {
            string ma = "m(" + MaPitchi[Math.Max(Array.IndexOf(TinhGia.DsLoaiPitchi, loai), 0)] + ")";
            var ds = TachMa(gc);
            int i = ds.FindIndex(LaMaPitchi);
            if (i >= 0) ds[i] = ma;
            else ds.Insert(ds.FindIndex(x => x.Equals("m", StringComparison.OrdinalIgnoreCase)) + 1, ma);
            return string.Join(", ", ds);
        }

        /// <summary>Cuộn chọn tay: "Không" bỏ r1 / r2, "Nửa hình" → r1, "Cả hình" → r2.</summary>
        public static string DatCuon(string gc, string cuon)
        {
            var ds = TachMa(gc).Where(x => !x.Equals("r1", StringComparison.OrdinalIgnoreCase) && !x.Equals("r2", StringComparison.OrdinalIgnoreCase)).ToList();
            if (cuon == TinhGia.DsLoaiCuon[1]) ds.Add("r1");
            else if (cuon == TinhGia.DsLoaiCuon[2]) ds.Add("r2");
            return string.Join(", ", ds);
        }

        public static string Dong34(TamXuat t) =>
            string.Join(",", O(t.N), O(t.M), O(t.T), t.X, t.Y, O(t.Q)) + "," + DuoiDong(t.Tt);   // X, Y ghi thẳng như dimcsv

        private static string DuoiDong(ThongTinTam tt)
        {
            var sb = new StringBuilder();
            sb.Append(O(tt.LoNho)).Append(',').Append(O(tt.LoLon))
              .Append(',').Append(O(tt.Tappu)).Append(',').Append(O(tt.Sara))
              .Append(',').Append(O(tt.PitchiSoLan)).Append(',').Append(O(tt.PitchiDai))
              .Append(',').Append(O(string.Join(", ", TachMa(tt.LoaiGiaCong))))
              .Append(',').Append(O(tt.Vat));
            for (int i = 0; i < 8; i++)
            {
                bool co = i < tt.DuongBe.Count;
                sb.Append(',').Append(co ? O(tt.DuongBe[i].SoLuong) : "0")
                  .Append(',').Append(co ? O(tt.DuongBe[i].Dai) : "0");
            }
            sb.Append(',').Append(O(tt.CheDoTinh)).Append(',').Append(O(tt.GiaBaoGia))
              .Append(',').Append(O(tt.TienVatLieu)).Append(',').Append(tt.GiaTay ? "1" : "0");
            return sb.ToString();
        }

        public static string Dong10(TamXuat t, string maDon) =>
            string.Join(",", O(t.N), O(t.Q), O(t.M), O(t.T), O(maDon + "-"), MaMage(t.Tt.LoaiGiaCong), t.X, t.Y, "", O(t.Tt.GiaBaoGia));

        public static List<TamXuat> SapXep(IEnumerable<TamXuat> ds, KieuSapXep k)
        {
            switch (k)
            {
                case KieuSapXep.TrenXuongDuoi: return ds.OrderByDescending(x => x.DiemY).ToList();
                case KieuSapXep.ChuCai: return ds.OrderBy(x => x, Comparer<TamXuat>.Create(SoTen)).ToList();
                default: return ds.OrderBy(x => x.DiemX).ToList();
            }
        }

        /// <summary>has-duplicates: tên N trùng đầu tiên.</summary>
        public static string TenTrung(IEnumerable<TamXuat> ds)
        {
            var thay = new HashSet<string>();
            foreach (var t in ds)
                if (t.N != null && !thay.Add(t.N)) return t.N;
            return null;
        }

        /// <summary>Ô tìm kiếm: mọi từ khoá phải có trong thẻ, không phân biệt hoa thường / dấu. Thẻ lỗi (t = null) tìm theo nội dung lỗi.</summary>
        public static bool KhopTim(TamXuat t, string loi, string tim)
        {
            string the = ThongTinTamParser.ChuanHoa(string.Join(" ", t?.N, t?.M, t?.T, t?.Q, t?.X, t?.Y, t?.Tt?.LoaiGiaCong, loi));
            return ThongTinTamParser.ChuanHoa(tim).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).All(k => the.Contains(k));
        }

        public static string DuongCuoi(string thuMucDwg, string maDon) => Path.Combine(thuMucDwg, maDon + ".csv");
        public static string DuongData(string thuMucDwg, string maDon) => Path.Combine(thuMucDwg, "data", maDon + "-data.csv");
        public static string DuongGia(string thuMucDwg, string maDon) => Path.Combine(thuMucDwg, "data", maDon + "-gia.json");
        public static string DuongGiaVl(string thuMucDwg, string maDon) => Path.Combine(thuMucDwg, "data", maDon + "-giavl.csv");
        public static string DuongDataCu(string thuMucDwg, string maDon) => Path.Combine(thuMucDwg, "data", maDon + "-data.cu.csv");
        public static string DuongDataTruocQuet(string thuMucDwg, string maDon) => Path.Combine(thuMucDwg, "data", maDon + "-data.truoc-quet.csv");

        /// <summary>File đầu tiên đang bị chương trình khác giữ (Excel...); không có → null. File chưa tồn tại không tính.</summary>
        public static string TepDangMo(params string[] tep)
        {
            foreach (string f in tep)
            {
                if (!File.Exists(f)) continue;
                try { using (new FileStream(f, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } }
                catch (IOException) { return f; }
                catch (UnauthorizedAccessException) { return f; }
            }
            return null;
        }

        /// <summary>
        /// Ghi &lt;đơn&gt;.csv và data\&lt;đơn&gt;-data.csv (UTF-8 BOM, CRLF). Mở độc quyền cả hai trước;
        /// một file bị khoá (Excel đang mở) thì ném IOException và không file nào bị đổi.
        /// </summary>
        public static void GhiHaiFile(string thuMucDwg, string maDon, IReadOnlyList<TamXuat> dsDaSapXep) =>
            GhiCacFile(thuMucDwg, (DuongCuoi(thuMucDwg, maDon), NoiDungCuoi(maDon, dsDaSapXep)),
                                  (DuongData(thuMucDwg, maDon), NoiDungData(dsDaSapXep)));

        /// <summary>Nút Quét / sửa ô: chỉ ghi data\&lt;đơn&gt;-data.csv (34 cột).</summary>
        public static void GhiData(string thuMucDwg, string maDon, IReadOnlyList<TamXuat> dsDaSapXep) =>
            GhiCacFile(thuMucDwg, (DuongData(thuMucDwg, maDon), NoiDungData(dsDaSapXep)));

        /// <summary>Nút Xuất CSV: chỉ ghi file cuối &lt;đơn&gt;.csv (10 cột gửi nhà máy).</summary>
        public static void GhiCuoi(string thuMucDwg, string maDon, IReadOnlyList<TamXuat> dsDaSapXep) =>
            GhiCacFile(thuMucDwg, (DuongCuoi(thuMucDwg, maDon), NoiDungCuoi(maDon, dsDaSapXep)));

        private static IEnumerable<string> NoiDungCuoi(string maDon, IEnumerable<TamXuat> ds) =>
            new[] { TieuDe10 }.Concat(ds.Select(t => Dong10(t, maDon)));

        private static IEnumerable<string> NoiDungData(IEnumerable<TamXuat> ds) => new[] { TieuDe34 }.Concat(ds.Select(Dong34));

        /// <summary>Ghi lại data\&lt;đơn&gt;-data.csv đã chuyển sang dạng 34 cột (xem ChuyenDataCu).</summary>
        public static void GhiDataDaChuyen(string thuMucDwg, string maDon, IEnumerable<string> dong) =>
            GhiCacFile(thuMucDwg, (DuongData(thuMucDwg, maDon), dong));

        private static void GhiCacFile(string thuMucDwg, params (string File, IEnumerable<string> Dong)[] tep)
        {
            if (string.IsNullOrWhiteSpace(thuMucDwg)) throw new InvalidOperationException("Bản vẽ chưa lưu");
            Directory.CreateDirectory(Path.Combine(thuMucDwg, "data"));
            var moi = new List<string>();
            var luong = new List<FileStream>();
            try
            {
                foreach (var t in tep) luong.Add(MoDocQuyen(t.File, moi));
            }
            catch
            {
                foreach (var l in luong) l.Dispose();
                foreach (string f in moi) try { File.Delete(f); } catch { }   // không để lại file rỗng vừa tạo
                throw;
            }
            for (int i = 0; i < tep.Length; i++)
                using (luong[i]) Ghi(luong[i], tep[i].Dong);
        }

        /// <summary>
        /// Đọc data\&lt;đơn&gt;-data.csv → thông tin tấm theo tên N (ngược của Dong34). Đọc được cả dạng cũ 35 cột;
        /// dòng cũ lấy cờ giá tay, loại pitchi, cuộn chọn tay từ -gia.json cũ (don). Dòng thiếu cột bị bỏ.
        /// </summary>
        public static Dictionary<string, ThongTinTam> DocData(string noiDung, GiaDon don = null)
        {
            var kq = new Dictionary<string, ThongTinTam>();
            foreach (var (dau, tt) in DocDong(noiDung, don)) kq[dau[0]] = tt;
            return kq;
        }

        /// <summary>Có dòng nào còn dạng 35 cột cũ (còn Loai cuon, Uu dai) không.</summary>
        public static bool LaDataCu(string noiDung) => Dong(noiDung).Any(o => o.Count >= 35);

        /// <summary>Chuyển nội dung data cũ sang 34 cột (dòng đã ở dạng mới giữ nguyên nghĩa).</summary>
        public static List<string> ChuyenDataCu(string noiDung, GiaDon don) =>
            new[] { TieuDe34 }.Concat(DocDong(noiDung, don).Select(r => string.Join(",", r.Dau.Select(O)) + "," + DuoiDong(r.Tt))).ToList();

        private static IEnumerable<List<string>> Dong(string noiDung)
        {
            foreach (string dong in (noiDung ?? "").TrimStart('\uFEFF').Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (dong == TieuDe34 || dong == TieuDe35 || dong.Trim() == "") continue;
                List<string> o = TachO(dong);
                if (o.Count >= 34 && o[0] != "") yield return o;
            }
        }

        // đường bẻ "0,0" là ô trống của lisp nên bỏ qua
        private static IEnumerable<(List<string> Dau, ThongTinTam Tt)> DocDong(string noiDung, GiaDon don)
        {
            foreach (List<string> o in Dong(noiDung))
            {
                bool cu = o.Count >= 35;
                int d = cu ? 1 : 0;   // dạng cũ còn cột Loai cuon sau Loai gia cong
                int I(int i) => ThongTinTamParser.Atoi(o[i]);
                var tt = new ThongTinTam
                {
                    LoNho = I(6), LoLon = I(7), Tappu = I(8), Sara = I(9), PitchiSoLan = I(10), PitchiDai = I(11),
                    LoaiGiaCong = o[12], Vat = I(13 + d), CheDoTinh = o[30 + d], GiaBaoGia = o[31 + d], TienVatLieu = o[32 + d],
                    GiaTay = !cu && o[33] == "1",
                };
                for (int b = 0; b < 8; b++)
                {
                    int soLuong = I(14 + d + 2 * b), dai = I(15 + d + 2 * b);
                    if (soLuong != 0 || dai != 0) tt.DuongBe.Add((dai, soLuong));
                }
                if (cu) ApDungGiaCu(tt, don != null && don.Tam.TryGetValue(o[0], out GiaTam g) ? g : null);
                yield return (o.Take(6).ToList(), tt);
            }
        }

        /// <summary>
        /// Dòng data cũ + -gia.json cũ: giá tay = có giá mà json không đánh dấu tự động; lisp ghi m(pitchi) thành "m"
        /// nên có số lần pitchi thì dựng lại mã m(...) theo loại đã chọn ("m" chỉ còn khi có đường bẻ); cuộn chọn tay → r1 / r2.
        /// </summary>
        private static void ApDungGiaCu(ThongTinTam tt, GiaTam g)
        {
            tt.GiaTay = !string.IsNullOrEmpty(tt.GiaBaoGia) && g?.TuDong != true;
            if (tt.PitchiSoLan > 0 || CoPitchi(tt.LoaiGiaCong))
            {
                tt.LoaiGiaCong = DatLoaiPitchi(tt.LoaiGiaCong, string.IsNullOrEmpty(g?.LoaiPitchi) ? LoaiPitchiCua(tt.LoaiGiaCong) : g.LoaiPitchi);
                if (tt.DuongBe.Count == 0)
                    tt.LoaiGiaCong = string.Join(", ", TachMa(tt.LoaiGiaCong).Where(x => !x.Equals("m", StringComparison.OrdinalIgnoreCase)));
            }
            if (g != null && g.DonGiaCuon != 0)
                tt.LoaiGiaCong = DatCuon(tt.LoaiGiaCong, g.DonGiaCuon >= TinhGia.GiaCuonCaHinh ? TinhGia.DsLoaiCuon[2] : g.DonGiaCuon > 0 ? TinhGia.DsLoaiCuon[1] : TinhGia.DsLoaiCuon[0]);
        }

        /// <summary>Tách 1 dòng CSV, ô có nháy kép (do O() bọc) giữ nguyên dấu phẩy bên trong.</summary>
        public static List<string> TachO(string dong)
        {
            var kq = new List<string>();
            var sb = new StringBuilder();
            bool trongNhay = false;
            for (int i = 0; i < dong.Length; i++)
            {
                char c = dong[i];
                if (trongNhay)
                {
                    if (c != '"') sb.Append(c);
                    else if (i + 1 < dong.Length && dong[i + 1] == '"') { sb.Append('"'); i++; }
                    else trongNhay = false;
                }
                else if (c == '"') trongNhay = true;
                else if (c == ',') { kq.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            kq.Add(sb.ToString());
            return kq;
        }

        private static FileStream MoDocQuyen(string f, List<string> moi)
        {
            bool co = File.Exists(f);
            var s = new FileStream(f, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (!co) moi.Add(f);
            return s;
        }

        private static void Ghi(FileStream s, IEnumerable<string> dong)
        {
            s.SetLength(0);
            var enc = new UTF8Encoding(true);
            byte[] bom = enc.GetPreamble();
            s.Write(bom, 0, bom.Length);
            byte[] b = enc.GetBytes(string.Concat(dong.Select(d => d + "\r\n")));
            s.Write(b, 0, b.Length);
        }

        /// <summary>compare-fname của lisp, dạng Comparison.</summary>
        private static int SoTen(TamXuat a, TamXuat b) => TruocTen(a.N, b.N) ? -1 : TruocTen(b.N, a.N) ? 1 : 0;

        private static bool TruocTen(string a, string b)
        {
            a = (a ?? "").ToUpperInvariant(); b = (b ?? "").ToUpperInvariant();
            int k = 0;
            while (k < a.Length && k < b.Length && a[k] == b[k]) k++;
            string sa = a.Substring(k), sb = b.Substring(k);
            if (sa == "" && sb == "") return false;
            if (sa == "") return true;
            if (sb == "") return false;
            sa = PhanTichKhung.ExportA2b(sa, "", "-");
            sb = PhanTichKhung.ExportA2b(sb, "", "-");
            int ca = sa.Length > 0 ? sa[0] : 0, cb = sb.Length > 0 ? sb[0] : 0;
            if (ca == 45) return true;
            if (cb == 45) return false;
            if (ca > 64 && cb > 64) return ca < cb;
            if (ca > 64) return true;
            if (cb > 64) return false;
            return SoDau(sa) < SoDau(sb);
        }

        /// <summary>atof: phần số ở đầu chuỗi.</summary>
        private static double SoDau(string s)
        {
            int i = 0;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || (i == 0 && (s[i] == '-' || s[i] == '+')))) i++;
            return double.TryParse(s.Substring(0, i), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0;
        }
    }
}
