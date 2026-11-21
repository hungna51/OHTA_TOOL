using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvTool.Core;

namespace CsvTool.Tests
{
    /// <summary>
    /// Đọc DXF ASCII vừa đủ cho test vàng: khung (LWPOLYLINE kín trên lớp khung) và TEXT/MTEXT/DIMENSION
    /// có điểm chèn nằm trong hộp khung — gần đúng "chọn trọn" của ssget W. Thứ tự = thứ tự trong file.
    /// </summary>
    internal static class DxfThuGon
    {
        private sealed class ThucThe
        {
            public string Loai, Lop = "";
            public List<(int Ma, string GiaTri)> Ma = new List<(int, string)>();
            public string Lay(int ma) => Ma.Where(x => x.Ma == ma).Select(x => x.GiaTri).FirstOrDefault();
            public double So(int ma) => double.Parse(Lay(ma) ?? "0", CultureInfo.InvariantCulture);
        }

        /// <summary>Đọc file mẫu trên OneDrive kể cả khi Excel / CAD đang mở nó (chỉ đọc, cho chia sẻ).</summary>
        internal static byte[] DocChiaSe(string file)
        {
            using (var s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var m = new MemoryStream()) { s.CopyTo(m); return m.ToArray(); }
        }

        public static (List<KhungTho> Khung, int Dimzin) Doc(string file, string lopKhung, string lopDim)
        {
            string[] dong = Encoding.UTF8.GetString(DocChiaSe(file)).Split('\n');
            var cap = new List<(int, string)>();
            for (int i = 0; i + 1 < dong.Length; i += 2)
                if (int.TryParse(dong[i].Trim(), out int ma)) cap.Add((ma, dong[i + 1].TrimEnd('\r')));

            int dimzin = 0, dimdecChung = 4;
            var dimdecKieu = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var tt = new List<ThucThe>();
            string mucLuc = null; ThucThe cur = null; string bienHeader = null;
            foreach (var (ma, v) in cap)
            {
                if (ma == 0 && v == "SECTION") { mucLuc = null; cur = null; continue; }
                if (ma == 2 && mucLuc == null) { mucLuc = v; continue; }
                if (mucLuc == "HEADER")
                {
                    if (ma == 9) bienHeader = v;
                    else if (bienHeader == "$DIMZIN") dimzin = int.Parse(v.Trim());
                    else if (bienHeader == "$DIMDEC") dimdecChung = int.Parse(v.Trim());
                    continue;
                }
                if (ma == 0) { cur = new ThucThe { Loai = v }; if (mucLuc == "ENTITIES" || mucLuc == "TABLES") tt.Add(cur); continue; }
                if (cur == null) continue;
                if (ma == 8) cur.Lop = v;
                cur.Ma.Add((ma, v));
            }
            foreach (var d in tt.Where(x => x.Loai == "DIMSTYLE"))
                if (d.Lay(2) != null && d.Lay(271) != null) dimdecKieu[d.Lay(2)] = int.Parse(d.Lay(271).Trim());

            var thucThe = tt.Where(x => x.Loai != "DIMSTYLE").ToList();
            var khung = new List<KhungTho>();
            foreach (var p in thucThe.Where(x => x.Loai == "LWPOLYLINE" && Cung(x.Lop, lopKhung) && (int.Parse(x.Lay(70)?.Trim() ?? "0") & 1) == 1))
            {
                var xs = p.Ma.Where(m => m.Ma == 10).Select(m => double.Parse(m.GiaTri, CultureInfo.InvariantCulture)).ToList();
                var ys = p.Ma.Where(m => m.Ma == 20).Select(m => double.Parse(m.GiaTri, CultureInfo.InvariantCulture)).ToList();
                var k = new KhungTho { MinX = xs.Min(), MaxX = xs.Max(), MinY = ys.Min(), MaxY = ys.Max() };
                bool Trong(double x, double y) => x >= k.MinX && x <= k.MaxX && y >= k.MinY && y <= k.MaxY;
                foreach (var e in thucThe)
                {
                    if ((e.Loai == "TEXT" || e.Loai == "MTEXT") && Trong(e.So(10), e.So(20)))
                        k.Text.Add(new TextTho
                        {
                            Lop = e.Lop, LaMtext = e.Loai == "MTEXT",
                            NoiDung = string.Concat(e.Ma.Where(m => m.Ma == 3 || m.Ma == 1).Select(m => m.GiaTri)),
                        });
                    else if (e.Loai == "DIMENSION" && Trong(e.So(10), e.So(20)) && Trong(e.So(11), e.So(21)))
                        k.Dim.Add(new DimTho
                        {
                            Lop = e.Lop, Co70 = int.Parse(e.Lay(70).Trim()), DoDai = e.So(42),
                            Dimdec = DimdecCuaDim(e, dimdecKieu, dimdecChung),
                        });
                }
                khung.Add(k);
            }
            return (khung, dimzin);
        }

        /// <summary>
        /// Dữ liệu hình học của từng khung (cùng thứ tự với <paramref name="khung"/>). Chỉ hiểu LINE/ARC/CIRCLE/LWPOLYLINE
        /// (đủ cho đơn mẫu). MTEXT trên 0-KAKOU bị bỏ qua: đó là chữ lỗi lisp ghi SAU lần xuất CSV. Hộp chữ = điểm chèn.
        /// </summary>
        public static List<DuLieuHinh> DocHinh(string file, List<KhungTho> khung, string lopTapu)
        {
            string[] dong = Encoding.UTF8.GetString(DocChiaSe(file)).Split('\n');
            var tt = new List<ThucThe>();
            string muc = null; ThucThe cur = null;
            for (int i = 0; i + 1 < dong.Length; i += 2)
            {
                if (!int.TryParse(dong[i].Trim(), out int ma)) continue;
                string v = dong[i + 1].TrimEnd('\r');
                if (ma == 0 && v == "SECTION") { muc = null; cur = null; continue; }
                if (ma == 2 && muc == null) { muc = v; continue; }
                if (ma == 0) { cur = new ThucThe { Loai = v }; if (muc == "ENTITIES" || muc == "TABLES") tt.Add(cur); continue; }
                if (cur == null) continue;
                if (ma == 8) cur.Lop = v;
                cur.Ma.Add((ma, v));
            }
            var netLop = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in tt.Where(x => x.Loai == "LAYER" && x.Lay(2) != null)) netLop[l.Lay(2)] = l.Lay(6) ?? "";

            var kq = new List<DuLieuHinh>();
            foreach (KhungTho k in khung)
            {
                var d = new DuLieuHinh();
                bool Trong(double x, double y) => x >= k.MinX && x <= k.MaxX && y >= k.MinY && y <= k.MaxY;
                foreach (var e in tt)
                {
                    if (e.Loai == "TEXT" || e.Loai == "MTEXT")
                    {
                        var c = new ChuHinh { Lop = e.Lop, MinX = e.So(10), MaxX = e.So(10), MinY = e.So(20), MaxY = e.So(20) };
                        string tho = string.Concat(e.Ma.Where(m => m.Ma == 3 || m.Ma == 1).Select(m => m.GiaTri));
                        c.NoiDung = e.Loai == "MTEXT" ? ThongTinTamParser.BoBocMtext(tho).Replace("\\P", "\n") : tho;
                        d.ChuQuanh.Add(c);
                        if (!Trong(c.MinX, c.MinY)) continue;
                        d.ChuTrongKhung.Add(c);
                        if (Cung(e.Lop, PhanTichKhung.LopFname)) d.ChuFname.Add(c);
                    }
                    else if (e.Loai == "HATCH" && Cung(e.Lop, lopTapu) && Trong(e.So(10), e.So(20))) d.SoHatch++;
                    else if (Cung(e.Lop, "0-KAKOU")) DocNet(e, d, Trong, netLop);
                }
                kq.Add(d);
            }
            return kq;
        }

        private static void DocNet(ThucThe e, DuLieuHinh d, Func<double, double, bool> trong, Dictionary<string, string> netLop)
        {
            var n = new NetCat { Loai = e.Loai };
            switch (e.Loai)
            {
                case "LINE":
                    n.Diem.Add((e.So(10), e.So(20)));
                    n.Diem.Add((e.So(11), e.So(21)));
                    break;
                case "CIRCLE":
                    n.Kin = true;
                    n.DoDai = 2 * Math.PI * e.So(40);
                    n.Diem = LamPhang.Cung(e.So(10), e.So(20), e.So(40), 0, 2 * Math.PI);
                    break;
                case "ARC":
                    double a0 = e.So(50) * Math.PI / 180, quet = (e.So(51) - e.So(50)) * Math.PI / 180;
                    if (quet <= 0) quet += 2 * Math.PI;
                    n.DoDai = e.So(40) * quet;
                    n.Diem = LamPhang.Cung(e.So(10), e.So(20), e.So(40), a0, quet);
                    break;
                case "LWPOLYLINE":
                    n.Kin = (int.Parse(e.Lay(70)?.Trim() ?? "0") & 1) == 1;
                    var dinh = new List<(double X, double Y, double Bulge)>();
                    foreach (var (ma, v) in e.Ma)
                    {
                        double so = ma == 10 || ma == 20 || ma == 42 ? double.Parse(v, CultureInfo.InvariantCulture) : 0;
                        if (ma == 10) dinh.Add((so, 0, 0));
                        else if (ma == 20) dinh[dinh.Count - 1] = (dinh[dinh.Count - 1].X, so, 0);
                        else if (ma == 42) dinh[dinh.Count - 1] = (dinh[dinh.Count - 1].X, dinh[dinh.Count - 1].Y, so);
                    }
                    n.Diem = LamPhang.Polyline(dinh, n.Kin);
                    break;
                case "MTEXT": return;
                default:
                    if (trong(e.So(10), e.So(20))) d.SaiKakou.Add((e.Loai, (e.So(10), e.So(20), e.So(10), e.So(20))));
                    return;
            }
            if (!n.Diem.All(p => trong(p.X, p.Y))) return;
            if (e.Loai == "LINE" || e.Loai == "LWPOLYLINE")
                n.DoDai = n.Diem.Zip(n.Diem.Skip(1), (a, b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y))).Sum();
            string lt = e.Lay(6);
            if (lt != null && lt.StartsWith("DASHED", StringComparison.OrdinalIgnoreCase))
            {
                if (e.Loai == "LINE" && lt.StartsWith("DASHED", StringComparison.Ordinal))
                    d.Be.Add(new NetBe { X1 = n.Dau.X, Y1 = n.Dau.Y, X2 = n.Cuoi.X, Y2 = n.Cuoi.Y });
                return;
            }
            if (e.Loai == "LINE" && (lt == null || lt.Equals("BYLAYER", StringComparison.OrdinalIgnoreCase)) &&
                netLop.TryGetValue(e.Lop, out string ltLop) && ltLop.StartsWith("DASHED", StringComparison.Ordinal))
                d.Be.Add(new NetBe { X1 = n.Dau.X, Y1 = n.Dau.Y, X2 = n.Cuoi.X, Y2 = n.Cuoi.Y });
            d.Net.Add(n);
        }

        /// <summary>DIMDEC: ghi đè trong XDATA ACAD (1070 271 → 1070 giá trị), không có thì theo kiểu dim, rồi $DIMDEC.</summary>
        private static int DimdecCuaDim(ThucThe e, Dictionary<string, int> kieu, int chung)
        {
            for (int i = 0; i + 1 < e.Ma.Count; i++)
                if (e.Ma[i].Ma == 1070 && e.Ma[i].GiaTri.Trim() == "271" && e.Ma[i + 1].Ma == 1070)
                    return int.Parse(e.Ma[i + 1].GiaTri.Trim());
            string ten = e.Lay(3);
            return ten != null && kieu.TryGetValue(ten, out int d) ? d : chung;
        }

        private static bool Cung(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
