using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Vát C / bo R một đỉnh. Dinh: số đỉnh viền ngoài ("3"), tên góc khung bao ("trai_duoi"…), toạ độ ("120,40") hoặc đỉnh khoét ("k1.2").</summary>
    public sealed class GocVat
    {
        public string Dinh = "";
        public char Kieu = 'C';
        public double GiaTri;
    }

    /// <summary>Khoét trong (không phải lỗ tròn / lỗ dài): chu_nhat (rộng × cao, góc xoay, R góc) hoặc da_giac (đỉnh so với điểm đặt); mỗi vị trí một khoét.</summary>
    public sealed class Khoet
    {
        public string Kieu = "chu_nhat";
        public double Rong, Cao, GocXoay, R;
        public List<(double X, double Y)> Dinh = new List<(double, double)>();
        public List<(double X, double Y)> ViTri = new List<(double, double)>();
    }

    /// <summary>
    /// Góc vát / bo + khoét trong (12/11) — dữ liệu chung mọi mẫu, như lỗ. Người / AI ghi dạng chữ gọn, chương trình đọc ra cấu trúc
    /// gửi ve.py (phần chung vẽ). Đỉnh đánh số theo chiều kim đồng hồ quanh viền ngoài, bắt đầu từ đỉnh trái dưới (sơ đồ đỉnh ở phòng 2).
    /// </summary>
    public static class PhongGocKhoet
    {
        public static readonly string[] TenGoc = { "trai_duoi", "phai_duoi", "phai_tren", "trai_tren" };

        public const string HuongDanGoc = "Mỗi góc: <đỉnh> C<số> (vát) hoặc R<số> (bo), cách nhau bằng ; — đỉnh = số trên sơ đồ đỉnh (1 = trái dưới, theo chiều kim đồng hồ), "
                                        + "tên góc trai_duoi / phai_duoi / phai_tren / trai_tren, toạ độ x,y trên hình trải phẳng, hoặc đỉnh khoét k1.2. Vd: 1 C10; trai_tren R5; 120,40 C5";
        public const string HuongDanKhoet = "Mỗi khoét cách nhau bằng | — chữ nhật: □40x60 R5 G30 @ 100,50; 300,50 (R = bo 4 góc, G = góc xoay, sau @ là tâm các vị trí); "
                                          + "đa giác: [0,0 40,0 40,20 0,20] @ 100,50 (đỉnh so với điểm đặt). Toạ độ trên hình trải phẳng, gốc trái dưới.";

        private static readonly Regex MucGoc = new Regex(@"^\s*(?<d>.+?)\s+(?<k>[CcRr])\s*(?<v>\d+(?:[.,]\d+)?)\s*$");
        private static readonly Regex DinhSo = new Regex(@"^\d+$"), DinhKhoet = new Regex(@"^[kK]\d+\.\d+$");

        /// <summary>"1 C10; trai_tren R5; 120,40 C5; k1.2 R3" → danh sách; sai cú pháp → null + loi. Rỗng → rỗng.</summary>
        public static List<GocVat> DocGoc(string s, out string loi)
        {
            loi = null;
            var ds = new List<GocVat>();
            foreach (string muc in (s ?? "").Split(';', '\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                Match m = MucGoc.Match(muc);
                if (!m.Success) { loi = "'" + muc + "' — đúng dạng <đỉnh> C10 / R5"; return null; }
                string d = m.Groups["d"].Value.Replace(" ", "").ToLowerInvariant();
                bool toaDo = d.Contains(",") && d.Split(',').Length == 2 && d.Split(',').All(p => So(p) != null);
                if (!DinhSo.IsMatch(d) && !TenGoc.Contains(d) && !toaDo && !DinhKhoet.IsMatch(d))
                { loi = "đỉnh '" + d + "' không hiểu (số, tên góc, x,y hoặc k1.2)"; return null; }
                if (toaDo) d = string.Join(",", d.Split(',').Select(p => F(So(p).Value)));
                double v = So(m.Groups["v"].Value) ?? 0;
                if (v <= 0) { loi = "'" + muc + "': giá trị phải > 0"; return null; }
                char k = char.ToUpperInvariant(m.Groups["k"].Value[0]);
                if (ds.Any(g => g.Dinh == d && g.Kieu != k)) { loi = "đỉnh " + d + " vừa vát C vừa bo R — chỉ chọn một"; return null; }
                if (ds.Any(g => g.Dinh == d)) { loi = "đỉnh " + d + " ghi hai lần"; return null; }
                ds.Add(new GocVat { Dinh = d, Kieu = k, GiaTri = v });
            }
            return ds;
        }

        /// <summary>"□40x60 R5 G30 @ 100,50; 300,50 | [0,0 40,0 40,20] @ 10,10" → danh sách; sai → null + loi.</summary>
        public static List<Khoet> DocKhoet(string s, out string loi)
        {
            loi = null;
            var ds = new List<Khoet>();
            foreach (string muc in (s ?? "").Split('|', '\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                int at = muc.IndexOf('@');
                if (at < 0) { loi = "'" + muc + "' thiếu @ vị trí"; return null; }
                string hinh = muc.Substring(0, at).Trim(), viTri = muc.Substring(at + 1);
                var k = new Khoet();
                foreach (string p in viTri.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0))
                {
                    var c = Cap(p);
                    if (c == null) { loi = "vị trí '" + p + "' phải là x,y"; return null; }
                    k.ViTri.Add(c.Value);
                }
                if (k.ViTri.Count == 0) { loi = "'" + muc + "' chưa có vị trí sau @"; return null; }
                if (hinh.StartsWith("[", StringComparison.Ordinal))
                {
                    if (!hinh.EndsWith("]", StringComparison.Ordinal)) { loi = "đa giác '" + hinh + "' thiếu ]"; return null; }
                    k.Kieu = "da_giac";
                    foreach (string p in hinh.Trim('[', ']').Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var c = Cap(p);
                        if (c == null) { loi = "đỉnh '" + p + "' của đa giác phải là x,y"; return null; }
                        k.Dinh.Add(c.Value);
                    }
                    if (k.Dinh.Count < 3) { loi = "đa giác '" + hinh + "' cần ít nhất 3 đỉnh"; return null; }
                }
                else
                {
                    string h = hinh.TrimStart('□', '口').Trim();
                    if (h.StartsWith("cn", StringComparison.OrdinalIgnoreCase)) h = h.Substring(2).Trim();
                    string[] tu = h.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] wh = tu.Length > 0 ? tu[0].ToLowerInvariant().Split('x', '×', '*') : new string[0];
                    if (wh.Length != 2 || So(wh[0]) is null || So(wh[1]) is null || So(wh[0]) <= 0 || So(wh[1]) <= 0)
                    { loi = "khoét '" + hinh + "' — đúng dạng □rộngxcao (vd □40x60)"; return null; }
                    k.Rong = So(wh[0]).Value; k.Cao = So(wh[1]).Value;
                    foreach (string t in tu.Skip(1))
                    {
                        char c0 = char.ToUpperInvariant(t[0]);
                        double? v = So(t.Substring(1));
                        if ((c0 != 'R' && c0 != 'G') || v == null) { loi = "'" + t + "' trong khoét — chỉ R<số> (bo góc) hoặc G<số> (góc xoay)"; return null; }
                        if (c0 == 'R') k.R = v.Value; else k.GocXoay = v.Value;
                    }
                    if (k.R * 2 > Math.Min(k.Rong, k.Cao) + 0.001) { loi = "khoét " + F(k.Rong) + "x" + F(k.Cao) + ": R" + F(k.R) + " quá lớn"; return null; }
                }
                ds.Add(k);
            }
            return ds;
        }

        /// <summary>Cấu trúc gửi ve.py: [{dinh, kieu C / R, gia_tri}].</summary>
        public static List<object> GocJson(IEnumerable<GocVat> ds) => ds.Select(g =>
        {
            var o = new JsonObject();
            o.Set("dinh", g.Dinh); o.Set("kieu", g.Kieu.ToString()); o.Set("gia_tri", new JsonNumber(F(g.GiaTri)));
            return (object)o;
        }).ToList();

        /// <summary>Cấu trúc gửi ve.py: [{kieu, rong, cao, goc_xoay, r, dinh: [[x, y]], vi_tri: [[x, y]]}].</summary>
        public static List<object> KhoetJson(IEnumerable<Khoet> ds) => ds.Select(k =>
        {
            var o = new JsonObject();
            o.Set("kieu", k.Kieu);
            o.Set("rong", new JsonNumber(F(k.Rong))); o.Set("cao", new JsonNumber(F(k.Cao)));
            o.Set("goc_xoay", new JsonNumber(F(k.GocXoay))); o.Set("r", new JsonNumber(F(k.R)));
            o.Set("dinh", k.Dinh.Select(p => (object)new List<object> { new JsonNumber(F(p.X)), new JsonNumber(F(p.Y)) }).ToList());
            o.Set("vi_tri", k.ViTri.Select(p => (object)new List<object> { new JsonNumber(F(p.X)), new JsonNumber(F(p.Y)) }).ToList());
            return (object)o;
        }).ToList();

        /// <summary>ケガキ JSON [{x, y}] → chữ gọn "120,27.5; 180,27.5" (ô sửa ở phòng 2).</summary>
        public static string KeGakiChu(string json)
        {
            List<object> l;
            try { l = JsonLite.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json) as List<object> ?? new List<object>(); } catch (Exception) { return json ?? ""; }
            return string.Join("; ", l.OfType<JsonObject>().Select(o => Chu(o.Get("x")) + "," + Chu(o.Get("y"))));
        }

        /// <summary>Chữ gọn ケガキ → JSON [{x, y}]; sai → null + loi.</summary>
        public static string KeGakiJson(string chu, out string loi)
        {
            loi = null;
            var ds = new List<object>();
            foreach (string p in (chu ?? "").Split(';', '\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                var c = Cap(p);
                if (c == null) { loi = "'" + p + "' phải là x,y"; return null; }
                var o = new JsonObject();
                o.Set("x", new JsonNumber(F(c.Value.X))); o.Set("y", new JsonNumber(F(c.Value.Y)));
                ds.Add(o);
            }
            return JsonLite.Serialize(ds);
        }

        /// <summary>
        /// Dữ liệu cũ (mẫu phang có c1…c4 / r1…r4 trong thông số mẫu, 12/11) → ô Góc dạng chung: c1 → trai_duoi, c2 → phai_duoi,
        /// c3 → phai_tren, c4 → trai_tren. Trả true nếu có đổi.
        /// </summary>
        public static bool DoiGocCu(TamAi t)
        {
            if (t.BienDang != "phang") return false; // chỉ mẫu phang từng có c / r góc — r1, r2 của mẫu khác là thông số riêng (vd bán kính cung)
            var ts = PhongTrienKhai.ThamSo(t.ThamSo);
            var cu = ts.Where(kv => Regex.IsMatch(kv.Key, "^[cr][1-4]$") && kv.Value > 0).ToList();
            if (cu.Count == 0) return false;
            var them = cu.Select(kv => TenGoc[kv.Key[1] - '1'] + " " + char.ToUpperInvariant(kv.Key[0]) + F(kv.Value));
            t.Goc = string.Join("; ", new[] { t.Goc }.Where(x => !string.IsNullOrWhiteSpace(x)).Concat(them));
            t.ThamSo = string.Join("; ", ts.Where(kv => !Regex.IsMatch(kv.Key, "^[cr][1-4]$")).Select(kv => kv.Key + "=" + F(kv.Value)));
            return true;
        }

        private static (double X, double Y)? Cap(string p)
        {
            string[] a = (p ?? "").Split(',');
            return a.Length == 2 && So(a[0]) is double x && So(a[1]) is double y ? (x, y) : ((double, double)?)null;
        }

        private static double? So(string s) =>
            double.TryParse((s ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : (double?)null;

        private static string Chu(object v) => v is JsonNumber n ? n.Raw : v as string ?? "";
        private static string F(double d) => d.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
