using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsvTool.Core
{
    /// <summary>Một nét cắt trên 0-KAKOU đã làm phẳng thành dãy điểm từ đầu tới cuối nét.</summary>
    public sealed class NetCat
    {
        public long Handle;
        public string Loai;      // LINE, ARC, CIRCLE, ELLIPSE, SPLINE, LWPOLYLINE, POLYLINE
        public bool Kin;         // cờ closed của polyline / spline; circle luôn kín
        public double DoDai;     // chiều dài thật, không phải của dãy điểm
        public List<(double X, double Y)> Diem = new List<(double, double)>();
        public (double X, double Y) Dau => Diem[0];
        public (double X, double Y) Cuoi => Diem[Diem.Count - 1];
    }

    public sealed class NetBe { public double X1, Y1, X2, Y2; }

    /// <summary>Cung hoặc đường tròn (Goc = 360) ngoài layer cắt, nằm trọn trong khung.</summary>
    public sealed class CungHinh { public double CX, CY, R, Goc, X1, Y1, X2, Y2; }

    public sealed class ChuHinh { public string Lop, NoiDung; public double MinX, MinY, MaxX, MaxY; }

    /// <summary>LEADER / MULTILEADER: chữ và các đầu mũi tên.</summary>
    public sealed class GhiChuDan
    {
        public string Lop, NoiDung;
        public List<(double X, double Y)> Mui = new List<(double, double)>();
    }

    /// <summary>Mọi thứ phép phân tích hình học cần của một khung tấm.</summary>
    public sealed class DuLieuHinh
    {
        public long HandleKhung;
        public List<NetCat> Net = new List<NetCat>();
        public List<NetBe> Be = new List<NetBe>();
        public List<(string Loai, (double MinX, double MinY, double MaxX, double MaxY) Hop)> SaiKakou = new List<(string, (double, double, double, double))>();
        public List<(string Loai, (double MinX, double MinY, double MaxX, double MaxY) Hop)> SaiFname = new List<(string, (double, double, double, double))>();
        /// <summary>Nét màu trắng trong khung nhưng không ở 0-KAKOU: dễ là nét cắt vẽ nhầm layer, file cắt sẽ mất nét.</summary>
        public List<(string Lop, (double MinX, double MinY, double MaxX, double MaxY) Hop)> NetTrangSaiLop = new List<(string, (double, double, double, double))>();
        public int SoHatch;
        public List<ChuHinh> ChuTrongKhung = new List<ChuHinh>();   // chọn trọn trong khung
        public List<ChuHinh> ChuFname = new List<ChuHinh>();        // F-name chạm khung
        public List<ChuHinh> ChuQuanh = new List<ChuHinh>();        // ký hiệu bẻ có thể nằm ngoài khung
        public List<GhiChuDan> Dan = new List<GhiChuDan>();
        public List<string> ChuDanTrong = new List<string>();       // chữ của leader có mũi tên chỉ vào khung
        public List<string> ChuDim = new List<string>();            // chữ ghi đè của DIMENSION trong khung
        public List<CungHinh> CungMatCat = new List<CungHinh>();    // cung ngoài 0-KAKOU, để nhận mặt cắt tấm cuộn
        public List<NetBe> DoanMatCat = new List<NetBe>();          // đoạn thẳng ngoài 0-KAKOU, phần thẳng nối tiếp cung cuộn
    }

    public sealed class KetQuaHinh
    {
        public ThongTinTam Tam;
        public List<LoiQuet> Loi = new List<LoiQuet>();   // mọi lỗi biên dạng của tấm
        public List<LoiQuet> CanhBao = new List<LoiQuet>();
    }

    /// <summary>Làm phẳng cung tròn / polyline có bulge, sai số dây cung ≤ 0.005 mm.</summary>
    public static class LamPhang
    {
        private const double SaiSoDay = 0.005;

        /// <summary>Điểm trên cung từ góc a0, quét góc có dấu (rad), gồm cả 2 đầu.</summary>
        public static List<(double X, double Y)> Cung(double cx, double cy, double r, double a0, double quet)
        {
            double buoc = r > SaiSoDay ? 2 * Math.Acos(1 - SaiSoDay / r) : Math.PI / 4;
            int n = Math.Min(2000, Math.Max(8, (int)Math.Ceiling(Math.Abs(quet) / buoc)));
            var kq = new List<(double, double)>(n + 1);
            for (int i = 0; i <= n; i++)
            {
                double a = a0 + quet * i / n;
                kq.Add((cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
            }
            return kq;
        }

        /// <summary>Polyline đỉnh + bulge; kín thì điểm cuối trùng điểm đầu.</summary>
        public static List<(double X, double Y)> Polyline(IReadOnlyList<(double X, double Y, double Bulge)> dinh, bool kin)
        {
            var kq = new List<(double, double)>();
            if (dinh.Count == 0) return kq;
            kq.Add((dinh[0].X, dinh[0].Y));
            int soCanh = kin ? dinh.Count : dinh.Count - 1;
            for (int i = 0; i < soCanh; i++)
            {
                var p = dinh[i];
                var q = dinh[(i + 1) % dinh.Count];
                double dx = q.X - p.X, dy = q.Y - p.Y, d = Math.Sqrt(dx * dx + dy * dy);
                if (Math.Abs(p.Bulge) < 1e-9 || d < 1e-12) { kq.Add((q.X, q.Y)); continue; }
                double h = d * (1 - p.Bulge * p.Bulge) / (4 * p.Bulge);
                double cx = (p.X + q.X) / 2 - dy / d * h, cy = (p.Y + q.Y) / 2 + dx / d * h;
                double r = Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy));
                var cung = Cung(cx, cy, r, Math.Atan2(p.Y - cy, p.X - cx), 4 * Math.Atan(p.Bulge));
                cung[cung.Count - 1] = (q.X, q.Y);
                kq.AddRange(cung.Skip(1));
            }
            return kq;
        }
    }

    /// <summary>
    /// Phân tích hình học 1 khung, thay CSV--analyze-boundary + CSV--bend-list-smart + CSV--scan-machining-texts
    /// của lisp v5.0.1. Vòng biên tính trên đa giác đã làm phẳng, không tạo REGION tạm trên bản vẽ.
    /// </summary>
    public static class PhanTichHinh
    {
        private const double Eps = 1e-4;
        private const double NguongSau = 0.01;   // lớn hơn sai số dây cung của 2 cung tiếp xúc
        private const double BeTol = 0.001;

        private const string Poshi = "\u767A\u62BC\u3057", Shimame = "\u30B7\u30DE\u76EE", Hikishiro = "\u5F15\u4EE3";
        private const string MageKakudo = "\u66F2\u3052\u89D2\u5EA6";
        private const string Kaisaki = "\u958B\u5148\u65BD\u5DE5\u7BC4\u56F2";

        private static readonly Regex MaU = new Regex(@"\\U\+([0-9A-Fa-f]{4})", RegexOptions.Compiled);

        /// <summary>hoiSoNhan(tên tấm, số đoạn): số đường bẻ nhấn 1 lần khi ký hiệu không khớp; null = không nhập.</summary>
        public static KetQuaHinh Chay(DuLieuHinh d, double tol, int cheDoTinh, Func<string, int, int?> hoiSoNhan)
        {
            var kq = new KetQuaHinh();
            kq.Loi = Bien(d, tol, out int nho, out int lon);
            if (kq.Loi.Count > 0) return kq;
            string fname = string.Concat(d.ChuFname.Select(c => "," + c.NoiDung));
            var be = DuongBe(d, fname, hoiSoNhan, s => kq.CanhBao.Add(new LoiQuet { Handle = d.HandleKhung, NoiDung = s }));
            var chuTrong = d.ChuTrongKhung.Select(c => c.NoiDung).ToList();
            // chữ của LEADER cũng là 1 đối tượng trong khung: không đếm 2 lần
            var ghiChu = chuTrong.Concat(d.ChuDanTrong.Where(x => !chuTrong.Contains(x))).Concat(d.ChuDim).ToList();
            int sara = DemSara(ghiChu), tapuGhi = DemTapu(ghiChu);
            if (d.SoHatch > 0 && tapuGhi > 0 && d.SoHatch != tapuGhi)
                kq.CanhBao.Add(new LoiQuet { Handle = d.HandleKhung, NoiDung = "Số Tapu lệch: tô màu " + d.SoHatch + ", ghi chú " + tapuGhi + ". Đang lấy số lớn hơn." });
            int tapu = Math.Max(d.SoHatch, tapuGhi);
            var tt = new ThongTinTam
            {
                LoNho = nho, LoLon = lon, Tappu = tapu, Sara = sara,
                LoaiGiaCong = LoaiGiaCong(d.ChuTrongKhung, be.Count > 0, tapuGhi > 0, sara > 0),
                CheDoTinh = cheDoTinh.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            tt.DuongBe.AddRange(be);
            kq.Tam = tt;
            return kq;
        }

        // ── Biên dạng ────────────────────────────────────────────────

        private sealed class Vong
        {
            public List<(double X, double Y)> Diem;
            public List<NetCat> Nguon;
            public double DienTich, ChuVi, MinX, MinY, MaxX, MaxY;
        }

        /// <summary>
        /// Báo đủ mọi lỗi: nét đã báo lỗi được loại ra rồi kiểm tra tiếp phần còn lại.
        /// Biên ngoài còn hở thì không biết vòng nào là biên, nên chỉ kiểm tra các vòng kín có chồng nhau không.
        /// </summary>
        private static List<LoiQuet> Bien(DuLieuHinh d, double tol, out int nho, out int lon)
        {
            nho = lon = 0;
            var kq = new List<LoiQuet>();
            void Them(string msg, IEnumerable<(double MinX, double MinY, double MaxX, double MaxY)> viTri) =>
                kq.Add(new LoiQuet { Handle = d.HandleKhung, LaLoi = true, NoiDung = msg, ViTri = viTri.ToList() });

            if (d.SaiKakou.Count > 0)
                Them("0-KAKOU có đối tượng không hợp lệ: " + string.Join(", ", d.SaiKakou.Select(x => x.Loai).Distinct()), d.SaiKakou.Select(x => x.Hop));
            if (d.SaiFname.Count > 0)
                Them("2-F-NAME có đối tượng không hợp lệ: " + string.Join(", ", d.SaiFname.Select(x => x.Loai).Distinct()), d.SaiFname.Select(x => x.Hop));
            if (d.NetTrangSaiLop.Count > 0)
                Them("Nét màu trắng sai layer: " + string.Join(", ", d.NetTrangSaiLop.Select(x => x.Lop).Distinct()) + ". Chuyển sang 0-KAKOU.",
                     d.NetTrangSaiLop.Select(x => x.Hop));
            if (d.Net.Count == 0)
            {
                Them("Khung chưa có nét cắt (layer 0-KAKOU).", new List<(double, double, double, double)>());
                return kq;
            }

            var bang0 = d.Net.Where(n => n.Loai != "CIRCLE" && !n.Kin && KC(n.Dau, n.Cuoi) <= Eps && n.DoDai <= Eps).ToList();
            if (bang0.Count > 0)
                Them("Có " + bang0.Count + " nét dài bằng 0. Xoá các nét này.", bang0.Select(HopNet));
            var net = d.Net.Except(bang0).ToList();

            var tu = DiemTu(net, tol);
            if (tu.Count > 0) Them("Nét cắt bị hở hoặc chồng nhau. Nối lại cho kín.", tu.Select(p => (p.X, p.Y, p.X, p.Y)));

            var nhom = NhomVong(net, tol, out var ho, out var nhomHo);
            if (ho.Count > 0 && tu.Count == 0) Them("Biên dạng chưa khép kín. Nối lại cho kín.", ho.Select(p => (p.X, p.Y, p.X, p.Y)));

            var vong = new List<Vong>();
            var hong = new List<NetCat>();
            foreach (var g in nhom)
            {
                Vong v = TaoVong(g, tol);
                if (v == null) hong.AddRange(g); else vong.Add(v);
            }
            if (hong.Count > 0) Them("Biên dạng tự cắt nhau. Sửa lại nét cắt.", hong.Select(HopNet));

            Vong ngoai = vong.OrderByDescending(v => v.DienTich).FirstOrDefault();
            if (ngoai == null)
            {
                if (kq.Count == 0) Them("Không xác định được biên ngoài của tấm.", new List<(double, double, double, double)>());
                return kq;
            }
            bool tinBienNgoai = hong.Concat(nhomHo.SelectMany(g => g)).All(n =>
            {
                var h = HopNet(n);
                return h.MinX >= ngoai.MinX && h.MaxX <= ngoai.MaxX && h.MinY >= ngoai.MinY && h.MaxY <= ngoai.MaxY;
            });

            var lo = new List<Vong>();
            if (tinBienNgoai)
            {
                var thua = new List<NetCat>();
                foreach (Vong v in vong.Where(x => x != ngoai))
                    if (NamTrongVong(v, ngoai)) lo.Add(v); else thua.AddRange(v.Nguon);
                if (thua.Count > 0) Them("Có nét thừa hoặc lỗ cắt qua biên tấm.", thua.Select(HopNet));
            }
            else lo = vong;

            var chong = new HashSet<NetCat>();
            for (int i = 0; i < lo.Count; i++)
                for (int j = i + 1; j < lo.Count; j++)
                    if (LanChong(lo[i], lo[j])) { chong.UnionWith(lo[i].Nguon); chong.UnionWith(lo[j].Nguon); }
            if (chong.Count > 0) Them("Có lỗ trùng hoặc chồng nhau. Xoá bớt hoặc sửa lại.", chong.Select(HopNet));

            if (kq.Count > 0) return kq;
            double chuVi30 = Math.PI * 30.0;
            nho = lo.Count(v => v.ChuVi <= chuVi30 + Eps);
            lon = lo.Count - nho;
            return kq;
        }

        private static (double MinX, double MinY, double MaxX, double MaxY) HopNet(NetCat n) =>
            (n.Diem.Min(p => p.X), n.Diem.Min(p => p.Y), n.Diem.Max(p => p.X), n.Diem.Max(p => p.Y));

        private static double KC((double X, double Y) a, (double X, double Y) b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        /// <summary>CSV--find-freepoints: đầu mút không có đúng 2 nét gặp nhau.</summary>
        private static List<(double X, double Y)> DiemTu(List<NetCat> net, double tol)
        {
            var dau = new List<(double, double)>();
            foreach (NetCat n in net)
            {
                bool co;
                switch (n.Loai)
                {
                    case "LINE": case "ARC": co = true; break;
                    case "LWPOLYLINE": case "POLYLINE": co = !n.Kin; break;
                    case "SPLINE": case "ELLIPSE": co = KC(n.Dau, n.Cuoi) > tol; break;
                    default: co = false; break;
                }
                if (co) { dau.Add(n.Dau); dau.Add(n.Cuoi); }
            }
            return DemDiem(dau, tol).Where(x => x.Dem != 2).Select(x => x.Diem).ToList();
        }

        private static List<((double X, double Y) Diem, int Dem)> DemDiem(IEnumerable<(double X, double Y)> ds, double tol)
        {
            var kq = new List<((double X, double Y) Diem, int Dem)>();
            foreach (var p in ds)
            {
                int i = kq.FindIndex(x => KC(p, x.Diem) <= tol);
                if (i >= 0) kq[i] = (kq[i].Diem, kq[i].Dem + 1); else kq.Add((p, 1));
            }
            return kq;
        }

        /// <summary>CSV--group-curves-by-loop: nét tự kín thành 1 nhóm, nét hở gom theo đầu mút chạm nhau.</summary>
        private static List<List<NetCat>> NhomVong(List<NetCat> net, double tol, out List<(double X, double Y)> ho, out List<List<NetCat>> nhomHo)
        {
            ho = new List<(double, double)>();
            nhomHo = new List<List<NetCat>>();
            var kq = new List<List<NetCat>>();
            var moHo = new List<NetCat>();
            foreach (NetCat n in net)
            {
                bool tuKin = n.Loai == "CIRCLE" ||
                             ((n.Loai == "LWPOLYLINE" || n.Loai == "POLYLINE" || n.Loai == "SPLINE") && (n.Kin || KC(n.Dau, n.Cuoi) <= tol));
                if (tuKin) kq.Add(new List<NetCat> { n }); else moHo.Add(n);
            }
            var daXet = new HashSet<NetCat>();
            foreach (NetCat goc in moHo)
            {
                if (!daXet.Add(goc)) continue;
                var cum = new List<NetCat>();
                var hang = new Queue<NetCat>();
                hang.Enqueue(goc);
                while (hang.Count > 0)
                {
                    NetCat c = hang.Dequeue();
                    cum.Add(c);
                    foreach (NetCat k in moHo)
                        if (!daXet.Contains(k) && (KC(c.Dau, k.Dau) <= tol || KC(c.Dau, k.Cuoi) <= tol || KC(c.Cuoi, k.Dau) <= tol || KC(c.Cuoi, k.Cuoi) <= tol))
                        {
                            daXet.Add(k);
                            hang.Enqueue(k);
                        }
                }
                var hoCum = DemDiem(cum.SelectMany(n => new[] { n.Dau, n.Cuoi }), tol).Where(x => x.Dem != 2).Select(x => x.Diem).ToList();
                ho.AddRange(hoCum);
                if (hoCum.Count > 0) nhomHo.Add(cum); else kq.Add(cum);
            }
            return kq;
        }

        /// <summary>Nối các nét của nhóm thành 1 đa giác; suy biến hoặc tự cắt → null (REGION của lisp cũng hỏng).</summary>
        private static Vong TaoVong(List<NetCat> nhom, double tol)
        {
            var diem = new List<(double X, double Y)>(nhom[0].Diem);
            var con = new List<NetCat>(nhom.Skip(1));
            while (con.Count > 0)
            {
                var cuoi = diem[diem.Count - 1];
                int i = con.FindIndex(n => KC(cuoi, n.Dau) <= tol || KC(cuoi, n.Cuoi) <= tol);
                if (i < 0) return null;
                bool xuoi = KC(cuoi, con[i].Dau) <= tol;
                diem.AddRange(xuoi ? con[i].Diem.Skip(1) : Enumerable.Reverse(con[i].Diem).Skip(1));
                con.RemoveAt(i);
            }
            if (KC(diem[0], diem[diem.Count - 1]) > tol) return null;
            diem.RemoveAt(diem.Count - 1);
            var gon = new List<(double X, double Y)>(diem.Count);
            foreach (var p in diem)
                if (gon.Count == 0 || KC(gon[gon.Count - 1], p) > 1e-9) gon.Add(p);
            if (gon.Count < 3) return null;

            double s = 0;
            for (int i = 0; i < gon.Count; i++)
            {
                var a = gon[i];
                var b = gon[(i + 1) % gon.Count];
                s += a.X * b.Y - b.X * a.Y;
            }
            var v = new Vong
            {
                Diem = gon, Nguon = nhom, DienTich = Math.Abs(s) / 2, ChuVi = nhom.Sum(n => n.DoDai),
                MinX = gon.Min(p => p.X), MinY = gon.Min(p => p.Y), MaxX = gon.Max(p => p.X), MaxY = gon.Max(p => p.Y),
            };
            return v.DienTich <= Eps || TuCat(v) ? null : v;
        }

        private static bool TuCat(Vong v)
        {
            int n = v.Diem.Count;
            for (int i = 0; i < n; i++)
                for (int j = i + 2; j < n; j++)
                {
                    if (i == 0 && j == n - 1) continue;
                    if (CatSau(v.Diem[i], v.Diem[(i + 1) % n], v.Diem[j], v.Diem[(j + 1) % n])) return true;
                }
            return false;
        }

        private static bool CatNhau(Vong a, Vong b)
        {
            int m = a.Diem.Count, n = b.Diem.Count;
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    if (CatSau(a.Diem[i], a.Diem[(i + 1) % m], b.Diem[j], b.Diem[(j + 1) % n])) return true;
            return false;
        }

        /// <summary>2 đoạn cắt nhau thật: mọi đầu mút cách đường kia &gt; NguongSau, nên 2 cung tiếp xúc không bị tính.</summary>
        private static bool CatSau((double X, double Y) a, (double X, double Y) b, (double X, double Y) c, (double X, double Y) d)
        {
            if (Math.Max(a.X, b.X) < Math.Min(c.X, d.X) || Math.Max(c.X, d.X) < Math.Min(a.X, b.X) ||
                Math.Max(a.Y, b.Y) < Math.Min(c.Y, d.Y) || Math.Max(c.Y, d.Y) < Math.Min(a.Y, b.Y)) return false;
            double d1 = KcCoDau(c, d, a), d2 = KcCoDau(c, d, b), d3 = KcCoDau(a, b, c), d4 = KcCoDau(a, b, d);
            return d1 * d2 < 0 && d3 * d4 < 0 &&
                   Math.Min(Math.Min(Math.Abs(d1), Math.Abs(d2)), Math.Min(Math.Abs(d3), Math.Abs(d4))) > NguongSau;
        }

        private static double KcCoDau((double X, double Y) a, (double X, double Y) b, (double X, double Y) p)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, l = Math.Sqrt(dx * dx + dy * dy);
            return l < 1e-12 ? 0 : (dx * (p.Y - a.Y) - dy * (p.X - a.X)) / l;
        }

        private static bool TrongDaGiac((double X, double Y) p, List<(double X, double Y)> da)
        {
            bool trong = false;
            for (int i = 0, j = da.Count - 1; i < da.Count; j = i++)
                if ((da[i].Y > p.Y) != (da[j].Y > p.Y) &&
                    p.X < (da[j].X - da[i].X) * (p.Y - da[i].Y) / (da[j].Y - da[i].Y) + da[i].X)
                    trong = !trong;
            return trong;
        }

        private static double KcBien((double X, double Y) p, List<(double X, double Y)> da)
        {
            double m = double.MaxValue;
            for (int i = 0; i < da.Count; i++)
            {
                var a = da[i];
                var b = da[(i + 1) % da.Count];
                m = Math.Min(m, KcDoan(p.X, p.Y, a.X, a.Y, b.X, b.Y));
            }
            return m;
        }

        private static bool NamTrongVong(Vong trong, Vong ngoai)
        {
            if (trong.MinX < ngoai.MinX - 2 * NguongSau || trong.MaxX > ngoai.MaxX + 2 * NguongSau ||
                trong.MinY < ngoai.MinY - 2 * NguongSau || trong.MaxY > ngoai.MaxY + 2 * NguongSau) return false;
            foreach (var p in trong.Diem)
                if (!TrongDaGiac(p, ngoai.Diem) && KcBien(p, ngoai.Diem) > 2 * NguongSau) return false;
            return !CatNhau(trong, ngoai);
        }

        /// <summary>Một điểm chắc chắn nằm trong đa giác: giữa cặp giao đầu tiên của đường quét ngang.</summary>
        private static (double X, double Y) DiemTrong(Vong v)
        {
            double y = v.MinY + (v.MaxY - v.MinY) * 0.5137;   // lệch giữa để không đi đúng qua đỉnh
            var xs = new List<double>();
            for (int i = 0; i < v.Diem.Count; i++)
            {
                var a = v.Diem[i];
                var b = v.Diem[(i + 1) % v.Diem.Count];
                if ((a.Y > y) != (b.Y > y)) xs.Add(a.X + (y - a.Y) / (b.Y - a.Y) * (b.X - a.X));
            }
            xs.Sort();
            return xs.Count >= 2 ? ((xs[0] + xs[1]) / 2, y) : (v.Diem[0].X, v.Diem[0].Y);
        }

        private static bool LanChong(Vong a, Vong b)
        {
            if (a.MaxX < b.MinX || b.MaxX < a.MinX || a.MaxY < b.MinY || b.MaxY < a.MinY) return false;
            return TrongDaGiac(DiemTrong(a), b.Diem) || TrongDaGiac(DiemTrong(b), a.Diem) || CatNhau(a, b);
        }

        // ── Đường bẻ ─────────────────────────────────────────────────

        private sealed class NhomBe
        {
            public double DirX, DirY, BaseX, BaseY, Min, Max;
            public List<(double A, double B)> Doan = new List<(double, double)>();
            public int Dai => (int)Math.Truncate(Max - Min + 0.5);
            public double Chieu(double x, double y) => (x - BaseX) * DirX + (y - BaseY) * DirY;
        }

        private static (double X, double Y)? HuongBe(NetBe b)
        {
            double dx = b.X2 - b.X1, dy = b.Y2 - b.Y1, l = Math.Sqrt(dx * dx + dy * dy);
            if (l <= 0) return null;
            dx /= l; dy /= l;
            return dx < 0 || (Math.Abs(dx) <= 1e-12 && dy < 0) ? (-dx, -dy) : (dx, dy);
        }

        private static List<NhomBe> NhomDuongBe(IEnumerable<NetBe> be)
        {
            var kq = new List<NhomBe>();
            foreach (NetBe b in be)
            {
                var h = HuongBe(b);
                if (h == null) continue;
                NhomBe g = kq.FirstOrDefault(x =>
                    Math.Abs(x.DirX * h.Value.Y - x.DirY * h.Value.X) <= BeTol &&
                    Math.Abs((b.X1 - x.BaseX) * x.DirY - (b.Y1 - x.BaseY) * x.DirX) <= BeTol &&
                    Math.Abs((b.X2 - x.BaseX) * x.DirY - (b.Y2 - x.BaseY) * x.DirX) <= BeTol);
                if (g == null)
                {
                    g = new NhomBe { DirX = h.Value.X, DirY = h.Value.Y, BaseX = b.X1, BaseY = b.Y1 };
                    double c = g.Chieu(b.X2, b.Y2);
                    g.Min = Math.Min(0, c); g.Max = Math.Max(0, c);
                    g.Doan.Add((g.Min, g.Max));
                    kq.Insert(0, g);
                    continue;
                }
                double p = g.Chieu(b.X1, b.Y1), q = g.Chieu(b.X2, b.Y2);
                g.Min = Math.Min(g.Min, Math.Min(p, q));
                g.Max = Math.Max(g.Max, Math.Max(p, q));
                g.Doan.Insert(0, (Math.Min(p, q), Math.Max(p, q)));
            }
            return kq;
        }

        private static void Cong(List<(int Dai, int SoLuong)> ds, int dai, int so)
        {
            int i = ds.FindIndex(x => x.Dai == dai);
            if (i >= 0) ds[i] = (dai, ds[i].SoLuong + so); else ds.Add((dai, so));
        }

        private static List<(int Dai, int SoLuong)> DuongBe(DuLieuHinh d, string fname, Func<string, int, int?> hoiSoNhan, Action<string> canhBao)
        {
            var nhom = NhomDuongBe(d.Be);
            var kq = new List<(int Dai, int SoLuong)>();
            if (!CoPoshi(fname))
                foreach (NhomBe g in nhom) Cong(kq, g.Dai, 1);
            else if (!fname.Contains("V=") && !CoShimame(fname))
                foreach (NhomBe g in nhom) Cong(kq, g.Dai, 2);
            else
            {
                string ten = fname.Contains("N=") ? PhanTichKhung.ExportA2b(fname, "N=", ",") : "?";
                bool thieuKyHieu = false, itKyHieu = false;
                foreach (NhomBe g in nhom)
                {
                    var ghiChu = KyHieuCua(g, d);
                    bool coNhan = ghiChu.Any(a => CoPoshi(a.Chu)), coKhac = ghiChu.Any(a => !CoPoshi(a.Chu));
                    int soDoan = g.Doan.Count;
                    if (coNhan && !coKhac)
                    {
                        if (soDoan > 1 && ghiChu.Count < soDoan) itKyHieu = true;
                        Cong(kq, g.Dai, 2);
                    }
                    else if (!coNhan)
                    {
                        if (ghiChu.Count == 0) thieuKyHieu = true;
                        Cong(kq, g.Dai, 1);
                    }
                    else if (soDoan == 1)
                    {
                        canhBao("Đường bẻ vừa có ký hiệu thường vừa có nhấn 1 lần.");
                        Cong(kq, g.Dai, 1);
                    }
                    else if (ghiChu.Count == soDoan)
                    {
                        foreach (var s in g.Doan)
                        {
                            double giua = (s.A + s.B) / 2;
                            var gan = ghiChu.OrderBy(a => Math.Abs(g.Chieu(a.X, a.Y) - giua)).First();
                            Cong(kq, (int)Math.Truncate(s.B - s.A + 0.5), CoPoshi(gan.Chu) ? 2 : 1);
                        }
                    }
                    else
                    {
                        int? nhap = hoiSoNhan?.Invoke(ten, soDoan);
                        if (nhap == null || nhap < 0 || nhap > soDoan)
                        {
                            canhBao("Chưa nhập số đường nhấn 1 lần, đang tính là bẻ thường.");
                            nhap = 0;
                        }
                        Cong(kq, g.Dai, 2 * nhap.Value + (soDoan - nhap.Value));
                    }
                }
                if (thieuKyHieu) canhBao("Đường bẻ thiếu ký hiệu.");
                if (itKyHieu) canhBao("Đường bẻ có ít ký hiệu hơn số đoạn.");
            }
            return kq.OrderByDescending(x => x.Dai).ToList();
        }

        /// <summary>Ký hiệu của 1 nhóm bẻ: chữ chạm đường bẻ, hoặc leader có mũi tên cách đường bẻ ≤ 5 mm.</summary>
        private static List<(string Chu, double X, double Y)> KyHieuCua(NhomBe g, DuLieuHinh d)
        {
            double x1 = g.BaseX + g.DirX * g.Min, y1 = g.BaseY + g.DirY * g.Min;
            double x2 = g.BaseX + g.DirX * g.Max, y2 = g.BaseY + g.DirY * g.Max;
            var kq = new List<(string, double, double)>();
            foreach (ChuHinh c in d.ChuQuanh)
                if (!LaFname(c.Lop) && !string.IsNullOrEmpty(c.NoiDung) && DoanChamHop(x1, y1, x2, y2, c.MinX, c.MinY, c.MaxX, c.MaxY))
                    kq.Add((c.NoiDung, (c.MinX + c.MaxX) / 2, (c.MinY + c.MaxY) / 2));
            foreach (GhiChuDan a in d.Dan)
            {
                if (LaFname(a.Lop) || string.IsNullOrEmpty(a.NoiDung)) continue;
                foreach (var m in a.Mui)
                    if (KcDoan(m.X, m.Y, x1, y1, x2, y2) <= 5.0) { kq.Add((a.NoiDung, m.X, m.Y)); break; }
            }
            return kq;
        }

        private static bool LaFname(string lop) => string.Equals(lop, PhanTichKhung.LopFname, StringComparison.OrdinalIgnoreCase);

        private static double KcDoan(double px, double py, double sx, double sy, double ex, double ey)
        {
            double dx = ex - sx, dy = ey - sy, l2 = dx * dx + dy * dy;
            double t = l2 < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((px - sx) * dx + (py - sy) * dy) / l2));
            double cx = sx + t * dx - px, cy = sy + t * dy - py;
            return Math.Sqrt(cx * cx + cy * cy);
        }

        /// <summary>Đoạn thẳng có chạm hình chữ nhật không (cắt Liang-Barsky).</summary>
        private static bool DoanChamHop(double x1, double y1, double x2, double y2, double minX, double minY, double maxX, double maxY)
        {
            double t0 = 0, t1 = 1, dx = x2 - x1, dy = y2 - y1;
            bool Cat(double p, double q)
            {
                if (p == 0) return q >= 0;
                double r = q / p;
                if (p < 0) { if (r > t1) return false; if (r > t0) t0 = r; }
                else { if (r < t0) return false; if (r < t1) t1 = r; }
                return true;
            }
            return Cat(-dx, x1 - minX) && Cat(dx, maxX - x1) && Cat(-dy, y1 - minY) && Cat(dy, maxY - y1);
        }

        // ── Loại gia công ────────────────────────────────────────────

        private static string GiaiMa(string s) => MaU.Replace(s ?? "", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());

        // lisp còn nhận chữ đầu ở dạng mã "\U+767A" khi chuỗi chưa giải mã
        private static bool CoPoshi(string s) => GiaiMa(s).Contains(Poshi) || (s ?? "").Contains("\\U+767A");
        private static bool CoShimame(string s) => GiaiMa(s).Contains(Shimame) || (s ?? "").Contains("\\U+30B7");

        // Bản vẽ thật ghi "2x2-∅8孔皿モミ"; "皿小ネジ" là tên vít, không phải gia công
        private static readonly Regex RxSara = new Regex("\u76BF\\s*(\u30E2\u30DF|\u3082\u307F|\u63C9|\u52A0\u5DE5|\u53D6|\u30B6\u30B0\u30EA|\u3056\u3050\u308A)|\u30B5\u30E9\\s*(\u30E2\u30DF|\u3082\u307F|\u63C9|\u52A0\u5DE5|\u53D6)|\u3055\u3089\u3082\u307F", RegexOptions.Compiled);
        private static readonly Regex RxTapu = new Regex("\u30BF\u30C3\u30D7|\\bTAP\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RxMaMtext = new Regex(@"\\[A-OQ-Za-z][^;\\]*;|[{}]", RegexOptions.Compiled);
        private static readonly Regex RxTich = new Regex(@"^\s*(\d+)\s*[xX×*]\s*(\d+)\s*[-‐−–ー]", RegexOptions.Compiled);
        private static readonly Regex RxDauSo = new Regex(@"^\s*(\d+)\s*(?:[-‐−–ー]|[xX×*]\s*[∅φΦØøMm%])", RegexOptions.Compiled);
        // dòng trên chỉ ghi lỗ: "5-∅6.8" rồi xuống dòng "皿加工(∅12.8)"
        private static readonly Regex RxDongLo = new Regex(@"^\s*\d+\s*(?:[xX×*]\s*\d+\s*)?[-‐−–ーxX×*]\s*(?:[∅φΦØø]|%%[Cc]|[Mm]\d)", RegexOptions.Compiled);
        private static readonly Regex RxChoSo = new Regex("(\\d+)\\s*(?:\u7B87\u6240|\u30F6\u6240|\u304B\u6240|\u30AB\u6240|\u30B1\u6240|\u500B\u6240)", RegexOptions.Compiled);

        // bỏ mã định dạng MTEXT, đưa chữ nửa độ rộng (ｻﾗﾓﾐ) về chữ thường
        private static string ChuanChu(string s) => RxMaMtext.Replace(GiaiMa(s), "").Normalize(NormalizationForm.FormKC);

        /// <summary>Tổng số lỗ Sara theo ghi chú: "2x2-∅8孔皿モミ" = 4, "4-φ8皿もみ" = 4, "皿モミ 3箇所" = 3, "5-∅6.8" + dòng "皿加工" = 5, không ghi số = 1.</summary>
        public static int DemSara(IEnumerable<string> chu) => DemGhiChu(chu, RxSara);

        /// <summary>Tổng số lỗ Tapu theo ghi chú: "4-M5タップ" = 4, "5×2-M12タップ" = 10, "4-M5" + dòng "タップ" = 4, không ghi số = 1.</summary>
        public static int DemTapu(IEnumerable<string> chu) => DemGhiChu(chu, RxTapu);

        private static int DemGhiChu(IEnumerable<string> chu, Regex loai)
        {
            int tong = 0;
            foreach (string c in chu)
            {
                string[] dong = ChuanChu((c ?? "").Replace("\\P", "\n")).Split('\n');
                for (int i = 0; i < dong.Length; i++)
                {
                    if (!loai.IsMatch(dong[i])) continue;
                    int? so = SoLoGhi(dong[i]);
                    // dòng trên là ghi chú gia công khác thì số lỗ không thuộc dòng này
                    if (so == null && i > 0 && RxDongLo.IsMatch(dong[i - 1]) && !RxSara.IsMatch(dong[i - 1]) && !RxTapu.IsMatch(dong[i - 1]))
                        so = SoLoGhi(dong[i - 1]);
                    tong += so ?? 1;
                }
            }
            return tong;
        }

        private const double DungSaiTam = 0.5, DungSaiDay = 0.2, DungSaiGoc = 1, DungSaiCuon = 2;

        /// <summary>
        /// Nhận cuộn chỉ bằng hình: mặt cắt là 2 cung đồng tâm, cùng góc, cách nhau đúng độ dày,
        /// và chiều dài cung giữa (cộng phần thẳng nối tiếp nếu có) bằng X hoặc Y của tấm.
        /// Cả vòng → r2, một phần vòng → r1, không có → null.
        /// </summary>
        public static string LoaiCuonTuHinh(IReadOnlyList<CungHinh> cung, double x, double y, double doDay, IReadOnlyList<NetBe> doan = null)
        {
            string kq = null;
            foreach (CungHinh a in cung)
                foreach (CungHinh b in cung)
                {
                    if (a.R <= b.R || Math.Abs(a.R - b.R - doDay) > DungSaiDay || Math.Abs(a.Goc - b.Goc) > DungSaiGoc) continue;
                    if (Math.Abs(a.CX - b.CX) > DungSaiTam || Math.Abs(a.CY - b.CY) > DungSaiTam) continue;
                    double dai = a.Goc * Math.PI / 180 * (a.R + b.R) / 2;
                    // shortcut: R < 10t coi là góc bẻ (130581-4-1: R9, t6), gặp tấm cuộn bán kính nhỏ thì hạ ngưỡng
                    double coThang = a.Goc >= 359 || doan == null || a.R < 10 * doDay ? dai : dai + PhanThang(a, b, doan);
                    if (!new[] { dai, coThang }.Any(l => Math.Abs(x - l) <= DungSaiCuon || Math.Abs(y - l) <= DungSaiCuon)) continue;
                    if (a.Goc >= 359) return "r2";
                    kq = "r1";
                }
            return kq;
        }

        /// <summary>Tổng phần thẳng ở 2 đầu cung: mỗi đầu cần 1 đoạn tiếp tuyến ở cả cung ngoài và cung trong, dài bằng nhau.</summary>
        private static double PhanThang(CungHinh a, CungHinh b, IReadOnlyList<NetBe> doan)
        {
            double tong = 0;
            foreach (var (px, py) in new[] { (a.X1, a.Y1), (a.X2, a.Y2) })
            {
                bool dau1 = Math.Abs(b.X1 - px) + Math.Abs(b.Y1 - py) <= Math.Abs(b.X2 - px) + Math.Abs(b.Y2 - py);
                double la = DoanTiepTuyen(a, px, py, doan), lb = DoanTiepTuyen(b, dau1 ? b.X1 : b.X2, dau1 ? b.Y1 : b.Y2, doan);
                if (la > 0 && lb > 0 && Math.Abs(la - lb) <= DungSaiCuon) tong += (la + lb) / 2;
            }
            return tong;
        }

        private static double DoanTiepTuyen(CungHinh c, double px, double py, IReadOnlyList<NetBe> doan)
        {
            double dai = 0;
            foreach (NetBe d in doan)
            {
                bool cham1 = Math.Abs(d.X1 - px) <= DungSaiTam && Math.Abs(d.Y1 - py) <= DungSaiTam;
                if (!cham1 && (Math.Abs(d.X2 - px) > DungSaiTam || Math.Abs(d.Y2 - py) > DungSaiTam)) continue;
                double dx = d.X2 - d.X1, dy = d.Y2 - d.Y1, l = Math.Sqrt(dx * dx + dy * dy);
                // tiếp tuyến thì vuông góc bán kính; nắp cuối dày t nằm theo bán kính nên bị loại
                if (l > dai && Math.Abs(dx * (px - c.CX) + dy * (py - c.CY)) <= 0.05 * l * c.R) dai = l;
            }
            return dai;
        }

        private static int? SoLoGhi(string dong)
        {
            Match m = RxTich.Match(dong);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int a) && int.TryParse(m.Groups[2].Value, out int b) && a > 0 && b > 0 && a <= 1000 && b <= 1000) return a * b;
            m = RxDauSo.Match(dong);
            if (!m.Success) m = RxChoSo.Match(dong);
            return m.Success && int.TryParse(m.Groups[1].Value, out int n) && n > 0 ? n : (int?)null;
        }

        /// <summary>CSV--scan-machining-texts: đọc mọi chữ trong khung ra "m, m(pitchi), t, s, k".</summary>
        public static string LoaiGiaCong(IEnumerable<ChuHinh> chu, bool coDuongBe, bool coTapu = false, bool coSara = false)
        {
            bool m = coDuongBe, pitchi = false, t = coTapu, s = coSara, k = false;
            foreach (ChuHinh c in chu)
            {
                if (c.NoiDung == null) continue;
                string v = GiaiMa(c.NoiDung), hoa = v.ToUpperInvariant();
                if (hoa.Contains("V=") || hoa.Contains("U=") || hoa.Contains("P=") || hoa.Contains("R=") || CoPoshi(c.NoiDung) ||
                    (CoShimame(c.NoiDung) && v.Contains(Hikishiro))) m = true;
                if (v.Contains(MageKakudo)) pitchi = true;
                if (RxTapu.IsMatch(ChuanChu(c.NoiDung))) t = true;
                if (RxSara.IsMatch(ChuanChu(c.NoiDung))) s = true;
                if (v.Contains(Kaisaki)) k = true;
            }
            var kq = new List<string>();
            if (m) kq.Add("m");
            if (pitchi) kq.Add("m(pitchi)");
            if (t) kq.Add("t");
            if (s) kq.Add("s");
            if (k) kq.Add("k");
            return string.Join(", ", kq);
        }
    }
}
