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

    /// <summary>Ráp dòng và ghi 2 file CSV của nút EXPORT.</summary>
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
              .Append(',').Append(O(tt.LoaiGiaCong)).Append(',').Append(LoaiCuon(tt.LoaiGiaCong))
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
            string.Join(",", O(t.N), O(t.Q), O(t.M), O(t.T), O(maDon + "-"), MaMage(t.Tt.LoaiGiaCong), t.X, t.Y, "", "");

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

        /// <summary>
        /// Ghi &lt;đơn&gt;.csv và data\&lt;đơn&gt;-data.csv (UTF-8 BOM, CRLF). Mở độc quyền cả hai trước;
        /// một file bị khoá (Excel đang mở) thì ném IOException và không file nào bị đổi.
        /// </summary>
        public static void GhiHaiFile(string thuMucDwg, string maDon, IReadOnlyList<TamXuat> dsDaSapXep)
        {
            if (string.IsNullOrWhiteSpace(thuMucDwg)) throw new InvalidOperationException("Bản vẽ chưa lưu");
            string thuMucData = Path.Combine(thuMucDwg, "data");
            Directory.CreateDirectory(thuMucData);
            string f10 = Path.Combine(thuMucDwg, maDon + ".csv");
            string f35 = Path.Combine(thuMucData, maDon + "-data.csv");

            var noi10 = new[] { TieuDe10 }.Concat(dsDaSapXep.Select(t => Dong10(t, maDon)));
            var noi35 = new[] { TieuDe35 }.Concat(dsDaSapXep.Select(Dong35));

            var moi = new List<string>();
            FileStream s10 = null, s35 = null;
            try
            {
                s10 = MoDocQuyen(f10, moi);
                s35 = MoDocQuyen(f35, moi);
            }
            catch
            {
                s10?.Dispose(); s35?.Dispose();
                foreach (string f in moi) try { File.Delete(f); } catch { }   // không để lại file rỗng vừa tạo
                throw;
            }
            using (s10) Ghi(s10, noi10);
            using (s35) Ghi(s35, noi35);
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
