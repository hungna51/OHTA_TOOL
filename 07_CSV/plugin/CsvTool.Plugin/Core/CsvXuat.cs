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

    /// <summary>Ráp dòng, đọc / ghi data CSV (35 cột) và file cuối &lt;đơn&gt;.csv (10 cột).</summary>
    public static class CsvXuat
    {
        public const string TieuDe10 = "エクスポート用,数量,材質,板厚,noと安全,曲げ,X,Y,,単価";

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
                string m = x == "m" || x == "m(pitchi)" ? "m" : x == "r1" || x == "r2" ? "r" :
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

        public static string Dong35(TamXuat t)
        {
            var tt = t.Tt;
            var sb = new StringBuilder();
            sb.Append(O(t.N)).Append(',').Append(O(t.M)).Append(',').Append(O(t.T))
              .Append(',').Append(t.X).Append(',').Append(t.Y)          // dimcsv ghi thẳng, không qua csv-field
              .Append(',').Append(O(t.Q))
              .Append(',').Append(O(tt.LoNho)).Append(',').Append(O(tt.LoLon))
              .Append(',').Append(O(tt.Tappu)).Append(',').Append(O(tt.Sara))
              .Append(',').Append(O(tt.PitchiSoLan)).Append(',').Append(O(tt.PitchiDai))
              .Append(',').Append(O(PhanTichKhung.ChuanHoaGiaCong(tt.LoaiGiaCong))).Append(',').Append(LoaiCuon(tt.LoaiGiaCong))
              .Append(',').Append(O(tt.Vat));
            for (int i = 0; i < 8; i++)
            {
                bool co = i < tt.DuongBe.Count;
                sb.Append(',').Append(co ? O(tt.DuongBe[i].SoLuong) : "0")
                  .Append(',').Append(co ? O(tt.DuongBe[i].Dai) : "0");
            }
            sb.Append(',').Append(O(tt.CheDoTinh)).Append(',').Append(O(tt.GiaBaoGia))
              .Append(',').Append(O(tt.TienVatLieu)).Append(',').Append(O(tt.UuDai));
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

        /// <summary>Nút Quét / sửa ô: chỉ ghi data\&lt;đơn&gt;-data.csv (35 cột).</summary>
        public static void GhiData(string thuMucDwg, string maDon, IReadOnlyList<TamXuat> dsDaSapXep) =>
            GhiCacFile(thuMucDwg, (DuongData(thuMucDwg, maDon), NoiDungData(dsDaSapXep)));

        /// <summary>Nút Xuất CSV: chỉ ghi file cuối &lt;đơn&gt;.csv (10 cột gửi nhà máy).</summary>
        public static void GhiCuoi(string thuMucDwg, string maDon, IReadOnlyList<TamXuat> dsDaSapXep) =>
            GhiCacFile(thuMucDwg, (DuongCuoi(thuMucDwg, maDon), NoiDungCuoi(maDon, dsDaSapXep)));

        private static IEnumerable<string> NoiDungCuoi(string maDon, IEnumerable<TamXuat> ds) =>
            new[] { TieuDe10 }.Concat(ds.Select(t => Dong10(t, maDon)));

        private static IEnumerable<string> NoiDungData(IEnumerable<TamXuat> ds) => new[] { TieuDe35 }.Concat(ds.Select(Dong35));

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
        /// Đọc data\&lt;đơn&gt;-data.csv → thông tin tấm theo tên N (ngược của Dong35). Dòng thiếu cột bị bỏ;
        /// đường bẻ "0,0" là ô trống của lisp nên bỏ qua.
        /// </summary>
        public static Dictionary<string, ThongTinTam> DocData(string noiDung)
        {
            var kq = new Dictionary<string, ThongTinTam>();
            foreach (string dong in (noiDung ?? "").TrimStart('\uFEFF').Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (dong == TieuDe35 || dong.Trim() == "") continue;
                List<string> o = TachO(dong);
                if (o.Count < 35 || o[0] == "") continue;
                int I(int i) => ThongTinTamParser.Atoi(o[i]);
                var tt = new ThongTinTam
                {
                    LoNho = I(6), LoLon = I(7), Tappu = I(8), Sara = I(9), PitchiSoLan = I(10), PitchiDai = I(11),
                    LoaiGiaCong = o[12], Vat = I(14), CheDoTinh = o[31], GiaBaoGia = o[32], TienVatLieu = o[33], UuDai = o[34],
                };
                for (int b = 0; b < 8; b++)
                {
                    int soLuong = I(15 + 2 * b), dai = I(16 + 2 * b);
                    if (soLuong != 0 || dai != 0) tt.DuongBe.Add((dai, soLuong));
                }
                kq[o[0]] = tt;
            }
            return kq;
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
