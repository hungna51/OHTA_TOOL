using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>Làm phẳng cung tròn / polyline có bulge, sai số dây cung <= 0.005 mm.</summary>
    public static class LamPhangGro
    {
        private const double SaiSoDay = 0.005;

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

        public static List<(double X, double Y)> Polyline(IReadOnlyList<(double X, double Y, double Bulge)> dinh, bool kin)
        {
            var kq = new List<(double, double)>();
            if (dinh == null || dinh.Count == 0) return kq;
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

    /// <summary>Một nét trên layer 0-KAKOU đã làm phẳng thành dãy điểm.</summary>
    public sealed class NetKiem
    {
        public long Handle;
        public string Loai;
        public bool Kin;
        public double DoDai;
        public List<(double X, double Y)> Diem = new List<(double, double)>();
        public (double X, double Y) Dau => Diem[0];
        public (double X, double Y) Cuoi => Diem[Diem.Count - 1];
    }

    public sealed class NetPhuKiem
    {
        public string Lop;
        public List<(double X, double Y)> Diem = new List<(double, double)>();
        public (double MinX, double MinY, double MaxX, double MaxY) Hop;
    }

    public sealed class DuLieuTamKiem
    {
        public string Bango;
        public List<NetKiem> NetKakou = new List<NetKiem>();
        public List<(string Loai, (double MinX, double MinY, double MaxX, double MaxY) Hop)> SaiLoaiKakou =
            new List<(string, (double, double, double, double))>();
        public List<NetPhuKiem> NetPhu = new List<NetPhuKiem>();
    }

    public sealed class LoiKinHo
    {
        public string Ma;
        public string Bango;
        public string TieuDe;
        public string ChiTiet;
        public bool LaByLayer;
        public List<(double MinX, double MinY, double MaxX, double MaxY)> ViTri =
            new List<(double, double, double, double)>();
        public List<long> Handles = new List<long>();
    }

    /// <summary>
    /// Kiểm tra kín hở biên dạng 0-KAKOU, nét 3-KEGAKI / 4-SHT ngoài hình chính và thuộc tính ByLayer
    /// (0-KAKOU, 3-KEGAKI, 4-SHT, 2-F-NAME) dùng chung cho từng tấm GRO và lệnh KT toàn bản vẽ.
    /// Thuần C#, không phụ thuộc API CAD.
    /// </summary>
    public static class KiemTraKinHo
    {
        public const double SaiSoMacDinh = 0.0001;
        private const double Eps = 1e-6;
        private const double NguongSau = 0.01;

        private static readonly HashSet<string> LayerKiemByLayer =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "0-KAKOU", "3-KEGAKI", "4-SHT", "2-F-NAME" };

        public static bool ChuanHoaSaiSo(string s, out double tol)
        {
            tol = SaiSoMacDinh;
            if (string.IsNullOrWhiteSpace(s)) return false;
            string chuan = s.Trim().Replace(',', '.');
            if (!double.TryParse(chuan, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return false;
            if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0.0 || v > 10.0)
                return false;
            tol = v;
            return true;
        }

        public static string DinhDangSaiSo(double tol)
        {
            return (tol > 0 ? tol : SaiSoMacDinh).ToString("0.######", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Kiểm tra thuộc tính ByLayer cho 4 layer: 0-KAKOU, 3-KEGAKI, 4-SHT, 2-F-NAME.
        /// Đường bẻ (LINE trên 0-KAKOU có Linetype DASHED*) được giữ nguyên Linetype nhưng Color và LineWeight phải ByLayer.
        /// </summary>
        public static bool LaDungByLayer(string layer, string loaiEntity, bool colorByLayer, string linetype, bool lineWeightByLayer)
        {
            if (string.IsNullOrWhiteSpace(layer) || !LayerKiemByLayer.Contains(layer.Trim()))
                return true;
            if (!colorByLayer || !lineWeightByLayer)
                return false;

            string lt = (linetype ?? string.Empty).Trim();
            if (string.Equals(layer.Trim(), "0-KAKOU", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(loaiEntity, "LINE", StringComparison.OrdinalIgnoreCase) &&
                lt.StartsWith("DASHED", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return lt.Length == 0 || string.Equals(lt, "ByLayer", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Đường bẻ trên 0-KAKOU có cần bỏ qua khi đặt lại Linetype về ByLayer không.</summary>
        public static bool LaDuongBe(string layer, string loaiEntity, string linetype)
        {
            return string.Equals((layer ?? "").Trim(), "0-KAKOU", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(loaiEntity, "LINE", StringComparison.OrdinalIgnoreCase) &&
                   (linetype ?? "").Trim().StartsWith("DASHED", StringComparison.OrdinalIgnoreCase);
        }

        public static List<LoiKinHo> KiemTraTamKhung(DuLieuTamKiem d, double tol)
        {
            var kq = new List<LoiKinHo>();
            if (d == null) return kq;
            if (tol <= 0) tol = SaiSoMacDinh;
            string prefix = string.IsNullOrEmpty(d.Bango) ? "" : "Bango " + d.Bango + " — ";

            void Them(string ma, string tieuDe, string chiTiet, IEnumerable<(double MinX, double MinY, double MaxX, double MaxY)> viTri) =>
                kq.Add(new LoiKinHo
                {
                    Ma = ma,
                    Bango = d.Bango,
                    TieuDe = prefix + tieuDe,
                    ChiTiet = chiTiet,
                    ViTri = viTri.ToList(),
                });

            if (d.SaiLoaiKakou.Count > 0)
            {
                Them("SAI_LOAI",
                     "0-KAKOU có đối tượng không hợp lệ",
                     string.Join(", ", d.SaiLoaiKakou.Select(x => x.Loai).Distinct()),
                     d.SaiLoaiKakou.Select(x => x.Hop));
            }
            if (d.NetKakou.Count == 0)
            {
                Them("THIEU_KAKOU", "Chưa có nét cắt (0-KAKOU)", "Không tìm thấy nét cắt hợp lệ trong khung.", Array.Empty<(double, double, double, double)>());
                return kq;
            }

            var bang0 = d.NetKakou.Where(n => n.Loai != "CIRCLE" && !n.Kin && KC(n.Dau, n.Cuoi) <= Eps && n.DoDai <= Eps).ToList();
            if (bang0.Count > 0)
                Them("NET_0", "Có " + bang0.Count + " nét dài bằng 0", "Xoá các nét có chiều dài bằng 0 trên 0-KAKOU.", bang0.Select(HopNet));

            var net = d.NetKakou.Except(bang0).ToList();
            if (net.Count == 0) return kq;

            var tu = DiemTu(net, tol);
            if (tu.Count > 0)
                Them("HO_NET", "Biên dạng 0-KAKOU bị hở hoặc chồng",
                     tu.Count + " vị trí chưa khép kín (sai số " + DinhDangSaiSo(tol) + " mm)",
                     tu.Select(p => (p.X, p.Y, p.X, p.Y)));

            var nhom = NhomVong(net, tol, out var ho, out var nhomHo);
            if (ho.Count > 0 && tu.Count == 0)
                Them("HO_VONG", "Biên dạng chưa khép kín",
                     ho.Count + " đầu mút hở (sai số " + DinhDangSaiSo(tol) + " mm)",
                     ho.Select(p => (p.X, p.Y, p.X, p.Y)));

            var vong = new List<Vong>();
            var hong = new List<NetKiem>();
            foreach (var g in nhom)
            {
                Vong v = TaoVong(g, tol);
                if (v == null) hong.AddRange(g); else vong.Add(v);
            }
            if (hong.Count > 0)
                Them("TU_CAT", "Biên dạng tự cắt nhau", "Sửa lại nét cắt 0-KAKOU.", hong.Select(HopNet));

            Vong ngoai = vong.OrderByDescending(v => v.DienTich).FirstOrDefault();
            if (ngoai == null)
            {
                if (kq.Count == 0)
                    Them("KHONG_BIEN", "Không xác định được biên ngoài của tấm", "Kiểm tra lại các nét cắt 0-KAKOU.", Array.Empty<(double, double, double, double)>());
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
                var thua = new List<NetKiem>();
                foreach (Vong v in vong.Where(x => x != ngoai))
                    if (NamTrongVong(v, ngoai)) lo.Add(v); else thua.AddRange(v.Nguon);
                if (thua.Count > 0)
                    Them("NET_THUA", "Có nét thừa hoặc lỗ cắt qua biên tấm", "Kiểm tra nét cắt thừa ngoài biên chính.", thua.Select(HopNet));
            }
            else
            {
                lo = vong;
            }

            var chong = new HashSet<NetKiem>();
            for (int i = 0; i < lo.Count; i++)
                for (int j = i + 1; j < lo.Count; j++)
                    if (LanChong(lo[i], lo[j])) { chong.UnionWith(lo[i].Nguon); chong.UnionWith(lo[j].Nguon); }
            if (chong.Count > 0)
                Them("LO_CHONG", "Có lỗ trùng hoặc chồng nhau", "Xoá bớt hoặc sửa lại lỗ trùng.", chong.Select(HopNet));

            if (tinBienNgoai && d.NetPhu.Count > 0)
            {
                double sai = Math.Max(tol, 0.1);
                bool Ngoai((double X, double Y) p)
                {
                    if (TrongDaGiac(p, ngoai.Diem)) return false;
                    for (int i = 0; i < ngoai.Diem.Count; i++)
                        if (KcDoan(p, ngoai.Diem[i], ngoai.Diem[(i + 1) % ngoai.Diem.Count]) <= sai) return false;
                    return true;
                }
                foreach (var g in d.NetPhu.Where(n => n.Diem.Any(Ngoai)).GroupBy(n => n.Lop.ToUpperInvariant()))
                {
                    Them("NGOAI_HINH_" + g.Key,
                         "Nét " + g.Key + " nằm ngoài hình chính",
                         g.Count() + " nét nằm ngoài biên tấm — xoá hoặc dời vào trong",
                         g.Select(n => n.Hop));
                }
            }
            return kq;
        }

        /// <summary>
        /// Kiểm tra kín hở tập đối tượng 0-KAKOU được chọn (lệnh KT): KHÔNG kiểm tra đối tượng nằm ngoài hình chính,
        /// chỉ kiểm tra sai loại, nét = 0, nét hở/chồng đầu mút, tự cắt và các vòng giao/trùng nhau.
        /// </summary>
        public static List<LoiKinHo> KiemTraToanBanVe(
            IList<NetKiem> netKakou,
            IList<(string Loai, (double MinX, double MinY, double MaxX, double MaxY) Hop)> saiLoaiKakou,
            double tol)
        {
            var kq = new List<LoiKinHo>();
            if (tol <= 0) tol = SaiSoMacDinh;

            void Them(string ma, string tieuDe, string chiTiet, IEnumerable<(double MinX, double MinY, double MaxX, double MaxY)> viTri) =>
                kq.Add(new LoiKinHo { Ma = ma, TieuDe = tieuDe, ChiTiet = chiTiet, ViTri = viTri.ToList() });

            if (saiLoaiKakou != null && saiLoaiKakou.Count > 0)
                Them("SAI_LOAI", "0-KAKOU — Đối tượng không hợp lệ",
                     string.Join(", ", saiLoaiKakou.Select(x => x.Loai).Distinct()),
                     saiLoaiKakou.Select(x => x.Hop));

            if (netKakou == null || netKakou.Count == 0) return kq;

            var bang0 = netKakou.Where(n => n.Loai != "CIRCLE" && !n.Kin && KC(n.Dau, n.Cuoi) <= Eps && n.DoDai <= Eps).ToList();
            if (bang0.Count > 0)
                Them("NET_0", "0-KAKOU — Có " + bang0.Count + " nét dài bằng 0", "Xoá các nét có chiều dài bằng 0.", bang0.Select(HopNet));

            var net = netKakou.Except(bang0).ToList();
            if (net.Count == 0) return kq;

            var tu = DiemTu(net, tol);
            if (tu.Count > 0)
                Them("HO_NET", "0-KAKOU — Nét cắt bị hở hoặc chồng đầu mút",
                     tu.Count + " vị trí hở > " + DinhDangSaiSo(tol) + " mm trong vùng chọn",
                     tu.Select(p => (p.X, p.Y, p.X, p.Y)));

            var nhom = NhomVong(net, tol, out var ho, out _);
            if (ho.Count > 0 && tu.Count == 0)
                Them("HO_VONG", "0-KAKOU — Biên dạng chưa khép kín",
                     ho.Count + " đầu mút hở > " + DinhDangSaiSo(tol) + " mm",
                     ho.Select(p => (p.X, p.Y, p.X, p.Y)));

            var vong = new List<Vong>();
            var hong = new List<NetKiem>();
            foreach (var g in nhom)
            {
                Vong v = TaoVong(g, tol);
                if (v == null) hong.AddRange(g); else vong.Add(v);
            }
            if (hong.Count > 0)
                Them("TU_CAT", "0-KAKOU — Biên dạng tự cắt nhau", hong.Count + " nét tạo vòng tự cắt.", hong.Select(HopNet));

            // Giao nhau hoặc trùng đè biên giữa các vòng kín (lỗ nằm gọn trong biên ngoài KHÔNG bị báo)
            var giao = new HashSet<NetKiem>();
            for (int i = 0; i < vong.Count; i++)
            {
                for (int j = i + 1; j < vong.Count; j++)
                {
                    Vong a = vong[i], b = vong[j];
                    if (a.MaxX < b.MinX - NguongSau || b.MaxX < a.MinX - NguongSau ||
                        a.MaxY < b.MinY - NguongSau || b.MaxY < a.MinY - NguongSau)
                        continue;

                    if (CatNhau(a, b) || TrungBien(a, b))
                    {
                        giao.UnionWith(a.Nguon);
                        giao.UnionWith(b.Nguon);
                    }
                }
            }
            if (giao.Count > 0)
                Them("GIAO_NHAU", "0-KAKOU — Biên dạng giao hoặc chồng đè nhau",
                     "Các vòng kín cắt chéo hoặc trùng lặp nhau.", giao.Select(HopNet));

            return kq;
        }

        public static List<List<(double X, double Y, double Bulge)>> TaoCloud(
            IEnumerable<(double MinX, double MinY, double MaxX, double MaxY)> viTri, double coKhung)
        {
            double le = Math.Max(1.5, coKhung * 0.012);
            var hop = viTri.Select(h => (MinX: h.MinX - le, MinY: h.MinY - le, MaxX: h.MaxX + le, MaxY: h.MaxY + le)).ToList();
            for (bool coGop = true; coGop;)
            {
                coGop = false;
                for (int i = 0; i < hop.Count && !coGop; i++)
                    for (int j = i + 1; j < hop.Count; j++)
                    {
                        var a = hop[i];
                        var b = hop[j];
                        if (a.MaxX < b.MinX || b.MaxX < a.MinX || a.MaxY < b.MinY || b.MaxY < a.MinY) continue;
                        hop[i] = (Math.Min(a.MinX, b.MinX), Math.Min(a.MinY, b.MinY), Math.Max(a.MaxX, b.MaxX), Math.Max(a.MaxY, b.MaxY));
                        hop.RemoveAt(j);
                        coGop = true;
                        break;
                    }
            }
            return hop.Select(h => Vien(h.MinX, h.MinY, h.MaxX, h.MaxY, Math.Max(2.0, le * 1.5))).ToList();
        }

        private static List<(double X, double Y, double Bulge)> Vien(double x0, double y0, double x1, double y1, double cung)
        {
            var goc = new[] { (X: x0, Y: y0), (X: x1, Y: y0), (X: x1, Y: y1), (X: x0, Y: y1) };
            var kq = new List<(double, double, double)>();
            for (int c = 0; c < 4; c++)
            {
                var p = goc[c];
                var q = goc[(c + 1) % 4];
                double dai = Math.Abs(q.X - p.X) + Math.Abs(q.Y - p.Y);
                int n = Math.Max(2, (int)Math.Round(dai / cung));
                for (int i = 0; i < n; i++)
                    kq.Add((p.X + (q.X - p.X) * i / n, p.Y + (q.Y - p.Y) * i / n, 0.5));
            }
            return kq;
        }

        // ── Hình học nội bộ ─────────────────────────────────────────────────────────

        private sealed class Vong
        {
            public List<(double X, double Y)> Diem;
            public List<NetKiem> Nguon;
            public double DienTich, MinX, MinY, MaxX, MaxY;
        }

        private static (double MinX, double MinY, double MaxX, double MaxY) HopNet(NetKiem n) =>
            (n.Diem.Min(p => p.X), n.Diem.Min(p => p.Y), n.Diem.Max(p => p.X), n.Diem.Max(p => p.Y));

        private static double KC((double X, double Y) a, (double X, double Y) b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private static List<(double X, double Y)> DiemTu(List<NetKiem> net, double tol)
        {
            var dau = new List<(double, double)>();
            foreach (NetKiem n in net)
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

        private static List<List<NetKiem>> NhomVong(List<NetKiem> net, double tol, out List<(double X, double Y)> ho, out List<List<NetKiem>> nhomHo)
        {
            ho = new List<(double, double)>();
            nhomHo = new List<List<NetKiem>>();
            var kq = new List<List<NetKiem>>();
            var moHo = new List<NetKiem>();
            foreach (NetKiem n in net)
            {
                bool tuKin = n.Loai == "CIRCLE" ||
                             ((n.Loai == "LWPOLYLINE" || n.Loai == "POLYLINE" || n.Loai == "SPLINE") && (n.Kin || KC(n.Dau, n.Cuoi) <= tol));
                if (tuKin) kq.Add(new List<NetKiem> { n }); else moHo.Add(n);
            }
            var daXet = new HashSet<NetKiem>();
            foreach (NetKiem goc in moHo)
            {
                if (!daXet.Add(goc)) continue;
                var cum = new List<NetKiem>();
                var hang = new Queue<NetKiem>();
                hang.Enqueue(goc);
                while (hang.Count > 0)
                {
                    NetKiem c = hang.Dequeue();
                    cum.Add(c);
                    foreach (NetKiem k in moHo)
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

        private static Vong TaoVong(List<NetKiem> nhom, double tol)
        {
            var diem = new List<(double X, double Y)>(nhom[0].Diem);
            var con = new List<NetKiem>(nhom.Skip(1));
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
                Diem = gon,
                Nguon = nhom,
                DienTich = Math.Abs(s) / 2,
                MinX = gon.Min(p => p.X),
                MinY = gon.Min(p => p.Y),
                MaxX = gon.Max(p => p.X),
                MaxY = gon.Max(p => p.Y),
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

        private static bool TrungBien(Vong a, Vong b)
        {
            double tol = 0.05;
            if (Math.Abs(a.MinX - b.MinX) > tol || Math.Abs(a.MaxX - b.MaxX) > tol ||
                Math.Abs(a.MinY - b.MinY) > tol || Math.Abs(a.MaxY - b.MaxY) > tol)
                return false;
            return Math.Abs(a.DienTich - b.DienTich) <= Math.Max(1.0, 0.01 * Math.Max(a.DienTich, b.DienTich));
        }

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

        public static bool TrongDaGiac((double X, double Y) p, IReadOnlyList<(double X, double Y)> da)
        {
            bool trong = false;
            for (int i = 0, j = da.Count - 1; i < da.Count; j = i++)
                if ((da[i].Y > p.Y) != (da[j].Y > p.Y) &&
                    p.X < (da[j].X - da[i].X) * (p.Y - da[i].Y) / (da[j].Y - da[i].Y) + da[i].X)
                    trong = !trong;
            return trong;
        }

        private static double KcDoan((double X, double Y) p, (double X, double Y) a, (double X, double Y) b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
            double t = l2 <= 0 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2));
            double x = a.X + t * dx - p.X, y = a.Y + t * dy - p.Y;
            return Math.Sqrt(x * x + y * y);
        }

        private static double KcBien((double X, double Y) p, IReadOnlyList<(double X, double Y)> da)
        {
            double m = double.MaxValue;
            for (int i = 0; i < da.Count; i++)
                m = Math.Min(m, KcDoan(p, da[i], da[(i + 1) % da.Count]));
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

        private static (double X, double Y) DiemTrong(Vong v)
        {
            double y = v.MinY + (v.MaxY - v.MinY) * 0.5137;
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
    }
}
