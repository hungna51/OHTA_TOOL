using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Mục 171 (GĐ 0): so kết quả một bước giữa LISP và C# (chế độ "ab"). Mỗi bên là danh sách
    /// BẢN GHI có khoá (thường là handle) và các trường tên = giá trị. Trường số so với dung sai
    /// (toạ độ qua lisp đã làm tròn — mục 170), trường chữ so nguyên văn.
    /// </summary>
    public static class SoSanh
    {
        public sealed class BanGhi
        {
            public string Khoa;
            public Dictionary<string, string> Truong = new Dictionary<string, string>(StringComparer.Ordinal);

            public BanGhi(string khoa) { Khoa = khoa; }

            public BanGhi Dat(string ten, string giaTri) { Truong[ten] = giaTri ?? ""; return this; }

            public BanGhi Dat(string ten, double giaTri) => Dat(ten, giaTri.ToString("R", CultureInfo.InvariantCulture));
        }

        public sealed class KetQua
        {
            public string Buoc;
            public int Khop;
            public List<string> Lech = new List<string>();
            public bool KhopHet => Lech.Count == 0;

            /// <summary>Dòng ASCII in ra dòng lệnh: "[AB buoc] khop N muc" hoặc "[AB buoc] LECH k: ..." (tối đa n dòng).</summary>
            public string InRa(int toiDa = 20)
            {
                if (KhopHet) return "[AB " + Buoc + "] khop " + Khop + " muc";
                var sb = new StringBuilder();
                sb.Append("[AB ").Append(Buoc).Append("] LECH ").Append(Lech.Count).Append(" (khop ").Append(Khop).Append("):");
                foreach (string l in Lech.Take(toiDa)) sb.Append("\n   ").Append(l);
                if (Lech.Count > toiDa) sb.Append("\n   ... ").Append(Lech.Count - toiDa).Append(" dong nua (xem file dump)");
                return sb.ToString();
            }
        }

        /// <summary>
        /// So hai danh sách. dungSai = sai số tuyệt đối cho trường số (cả hai bên đọc được là số).
        /// Khoá trùng trong một bên -> báo lệch (dữ liệu hỏng).
        /// </summary>
        public static KetQua So(string buoc, IEnumerable<BanGhi> lisp, IEnumerable<BanGhi> cs, double dungSai)
        {
            var kq = new KetQua { Buoc = buoc };
            Dictionary<string, BanGhi> L = TaoBang(lisp, "lisp", kq), C = TaoBang(cs, "C#", kq);
            foreach (string k in L.Keys.Union(C.Keys).OrderBy(x => x, StringComparer.Ordinal))
            {
                bool coL = L.TryGetValue(k, out BanGhi a), coC = C.TryGetValue(k, out BanGhi b);
                if (!coL) { kq.Lech.Add(k + ": chi C# co"); continue; }
                if (!coC) { kq.Lech.Add(k + ": chi lisp co"); continue; }
                var lech = new List<string>();
                foreach (string t in a.Truong.Keys.Union(b.Truong.Keys).OrderBy(x => x, StringComparer.Ordinal))
                {
                    a.Truong.TryGetValue(t, out string va);
                    b.Truong.TryGetValue(t, out string vb);
                    if (!Bang(va, vb, dungSai)) lech.Add(t + " lisp=" + (va ?? "-") + " C#=" + (vb ?? "-"));
                }
                if (lech.Count == 0) kq.Khop++;
                else kq.Lech.Add(k + ": " + string.Join("; ", lech));
            }
            return kq;
        }

        public static bool Bang(string a, string b, double dungSai)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                return Math.Abs(x - y) <= dungSai;
            return false;
        }

        private static Dictionary<string, BanGhi> TaoBang(IEnumerable<BanGhi> ds, string ben, KetQua kq)
        {
            var d = new Dictionary<string, BanGhi>(StringComparer.Ordinal);
            if (ds == null) return d;
            foreach (BanGhi g in ds)
            {
                if (d.ContainsKey(g.Khoa)) kq.Lech.Add(g.Khoa + ": khoa trung ben " + ben);
                else d[g.Khoa] = g;
            }
            return d;
        }

        /// <summary>
        /// Đọc bản ghi lisp gửi sang dạng chuỗi "khoa|ten=gia tri|ten=gia tri". Dòng hỏng bỏ qua.
        /// </summary>
        public static List<BanGhi> DocChuoi(IEnumerable<string> dong)
        {
            var ds = new List<BanGhi>();
            foreach (string s in dong ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                string[] p = s.Split('|');
                var g = new BanGhi(p[0].Trim());
                for (int i = 1; i < p.Length; i++)
                {
                    int k = p[i].IndexOf('=');
                    if (k > 0) g.Dat(p[i].Substring(0, k).Trim(), p[i].Substring(k + 1).Trim());
                }
                ds.Add(g);
            }
            return ds;
        }
    }
}
