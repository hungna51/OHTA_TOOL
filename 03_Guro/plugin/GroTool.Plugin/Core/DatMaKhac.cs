using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ZwCadPlugin.Core
{
    // =====================================================================================
    // Mục 170: ĐẶT MÃ KHẮC + F-NAME VÀO TRONG HÌNH TẤM ĐƠN / MIRROR (bước 6g của c:gro) — bản C#.
    // Chép từ lisp chế độ 2 (mục 122, "trượt từ góc trên-trái"); chế độ 1 (polylabel) đã bỏ.
    // Toàn bộ file này là LOGIC THUẦN (không gọi API ZWCAD) để chạy test ngoài CAD. Phần lấy
    // mẫu nét / dời entity / ghi XData nằm ở CadServices/DatMaKhacCad.cs.
    //
    // Tên hàm lisp gốc ghi ở đầu từng hàm — sửa thuật toán thì đối chiếu với lịch sử mục
    // 117-119, 122, 136, 138 trong rules (lisp cũ đã xoá khỏi Guro ZWCAD(gro).lsp).
    // =====================================================================================

    /// <summary>Điểm 2D.</summary>
    public struct Diem
    {
        public double X, Y;
        public Diem(double x, double y) { X = x; Y = y; }
    }

    /// <summary>Hộp bao (x1 y1 x2 y2) — cùng thứ tự với list bb của lisp.</summary>
    public sealed class Hop
    {
        public double X1, Y1, X2, Y2;
        public Hop(double x1, double y1, double x2, double y2) { X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; }
        public double W => X2 - X1;
        public double H => Y2 - Y1;
        public double this[int i] => i == 0 ? X1 : i == 1 ? Y1 : i == 2 ? X2 : Y2;

        /// <summary>gro--bb-giao-p</summary>
        public bool Giao(Hop b) => X1 <= b.X2 && b.X1 <= X2 && Y1 <= b.Y2 && b.Y1 <= Y2;

        /// <summary>gro--bb-chua-p (this = ngoài)</summary>
        public bool Chua(Hop t) => X1 <= t.X1 && Y1 <= t.Y1 && X2 >= t.X2 && Y2 >= t.Y2;

        public Hop No(double d) => new Hop(X1 - d, Y1 - d, X2 + d, Y2 + d);

        /// <summary>(equal bb1 bb2 tol) của lisp: từng phần tử lệch không quá tol.</summary>
        public bool Bang(Hop b, double tol) =>
            Math.Abs(X1 - b.X1) <= tol && Math.Abs(Y1 - b.Y1) <= tol &&
            Math.Abs(X2 - b.X2) <= tol && Math.Abs(Y2 - b.Y2) <= tol;

        public static Hop TuDiem(IEnumerable<Diem> pts)
        {
            double x1 = double.MaxValue, y1 = double.MaxValue, x2 = double.MinValue, y2 = double.MinValue;
            foreach (Diem p in pts)
            {
                if (p.X < x1) x1 = p.X;
                if (p.Y < y1) y1 = p.Y;
                if (p.X > x2) x2 = p.X;
                if (p.Y > y2) y2 = p.Y;
            }
            return new Hop(x1, y1, x2, y2);
        }
    }

    /// <summary>Đoạn thẳng (x1 y1 x2 y2 xmin ymin xmax ymax keg) như lisp.</summary>
    public sealed class Doan
    {
        public readonly double X1, Y1, X2, Y2, MinX, MinY, MaxX, MaxY;
        public readonly bool Keg;
        public Doan(double x1, double y1, double x2, double y2, bool keg)
        {
            X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; Keg = keg;
            MinX = Math.Min(x1, x2); MinY = Math.Min(y1, y2);
            MaxX = Math.Max(x1, x2); MaxY = Math.Max(y1, y2);
        }

        /// <summary>gro--mk2-doi-xy: đổi x &lt;-&gt; y (tia DỌC thành tia NGANG).</summary>
        public Doan DoiXy() => new Doan(Y1, X1, Y2, X2, Keg);
    }

    /// <summary>
    /// Một nét đã lấy mẫu: (e lay bb kin pts) của gro--mk-quet-net. Layer viết HOA.
    /// </summary>
    public sealed class NetMau
    {
        public string Layer;
        public Hop Bb;
        public bool Kin;
        public List<Diem> Pts;

        public bool LaKegaki => Layer == "3-KEGAKI";
        public bool LaKakou => Layer == "0-KAKOU";
    }

    /// <summary>Chỉ mục theo dải ngang (y0 ch G hang) — gro--cm-tao.</summary>
    public sealed class ChiMuc
    {
        public const int G = 24;
        public readonly double Y0, Ch;
        public readonly List<Doan>[] Hang;

        public ChiMuc(IList<Doan> doans, double y0, double y1)
        {
            Y0 = y0;
            Ch = Math.Max(1e-6, (y1 - y0) / G);
            Hang = new List<Doan>[G];
            for (int j = 0; j < G; j++) Hang[j] = new List<Doan>();
            foreach (Doan d in doans)
            {
                int j0 = J(d.MinY), j1 = J(d.MaxY);
                for (int j = j0; j <= j1; j++) Hang[j].Add(d);
            }
        }

        /// <summary>gro--cm-j0: max(0, min(G-1, fix((y - y0) / ch))).</summary>
        public int J(double y)
        {
            double v = (y - Y0) / Ch;
            if (double.IsNaN(v)) return 0;
            if (v >= G - 1) return G - 1;
            if (v <= 0) return 0;
            return (int)Math.Truncate(v);
        }
    }

    /// <summary>Một tấm cần đặt: dữ liệu lisp gửi sang (đã đổi handle sang id ở lớp CAD).</summary>
    public sealed class TamCanDat
    {
        public string Bango;      // (rtos bango 2 0)
        public Hop BbHinh;        // hộp hình cắt của tấm (nth 13 ct)
        public Hop BbCum;         // hộp cụm mã khắc + F-name (gro--bb-ds-ename cum)
    }

    /// <summary>Kết quả đặt một tấm.</summary>
    public sealed class KetQuaTam
    {
        public TamCanDat Tam;
        public bool Dat;
        public double Cx, Cy, Th;   // tâm mới + góc xoay (rad)
        public double TiLe = 1.0;
        public bool BoKeg;
        public double Hw, Hh;       // nửa cạnh ĐÃ cộng khe c (vật cản cho tấm sau)
        public string TrungVoi;     // bango tấm chung hình (nếu có)
        public int NCham, NTia;     // mục 138: số vị trí bị loại vì chạm nét / ngoài thịt
        public string DongChanDoan; // dòng "[MK] ..." in ra dòng lệnh
    }

    /// <summary>Kết quả cả lượt.</summary>
    public sealed class KetQuaDatMk
    {
        public List<KetQuaTam> Tams = new List<KetQuaTam>();
        public int Tong, SoDat, ThuNho, DeKeg, Chua;
        /// <summary>Chuỗi báo cáo trả lisp (đúng định dạng BaoCaoGro.RDatMk), null nếu không có tấm nào.</summary>
        public string ThongBao;
    }

    public static class DatMaKhac
    {
        public const string CoThuNho = "MK_THU_NHO";
        public const string CoDeKegaki = "MK_DE_KEGAKI";
        public const string CoChuaDat = "MK_CHUA_DAT";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ---------------------------------------------------------------------------
        // Đoạn / hình học cơ bản
        // ---------------------------------------------------------------------------

        /// <summary>gro--diem->doan: nối điểm liên tiếp, bỏ cặp trùng (1e-9).</summary>
        public static List<Doan> DiemThanhDoan(IList<Diem> pts, bool keg)
        {
            var out_ = new List<Doan>();
            if (pts == null || pts.Count < 2) return out_;
            Diem a = pts[0];
            for (int i = 1; i < pts.Count; i++)
            {
                Diem b = pts[i];
                if (!(Math.Abs(a.X - b.X) <= 1e-9 && Math.Abs(a.Y - b.Y) <= 1e-9))
                    out_.Add(new Doan(a.X, a.Y, b.X, b.Y, keg));
                a = b;
            }
            return out_;
        }

        /// <summary>gro--doan-cat-hop-p: đoạn p1-p2 cắt (hoặc nằm trong) hộp — Liang-Barsky.</summary>
        public static bool DoanCatHop(double x1, double y1, double x2, double y2,
                                      double bx1, double by1, double bx2, double by2)
        {
            double dx = x2 - x1, dy = y2 - y1, t0 = 0.0, t1 = 1.0;
            double[] ps = { -dx, dx, -dy, dy };
            double[] qs = { x1 - bx1, bx2 - x1, y1 - by1, by2 - y1 };
            for (int i = 0; i < 4; i++)
            {
                double p = ps[i], q = qs[i];
                if (Math.Abs(p) <= 1e-12)
                {
                    if (q < 0.0) return false;
                }
                else
                {
                    double r = q / p;
                    if (p < 0.0)
                    {
                        if (r > t1) return false;
                        if (r > t0) t0 = r;
                    }
                    else
                    {
                        if (r < t0) return false;
                        if (r < t1) t1 = r;
                    }
                }
            }
            return true;
        }

        /// <summary>gro--dist-pt-seg</summary>
        public static double KcDiemDoan(Diem p, Diem a, Diem b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
            if (l2 <= 1e-8) return KhoangCach(p, a);
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2;
            if (t < 0) t = 0; else if (t > 1) t = 1;
            return KhoangCach(p, new Diem(a.X + t * dx, a.Y + t * dy));
        }

        public static double KhoangCach(Diem a, Diem b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        /// <summary>(angle a b) của lisp: [0, 2pi).</summary>
        public static double Goc(Diem a, Diem b)
        {
            double g = Math.Atan2(b.Y - a.Y, b.X - a.X);
            if (g < 0) g += 2.0 * Math.PI;
            return g;
        }

        // ---------------------------------------------------------------------------
        // Biên ngoài của nét hở (mục 119) — gro--mk-vong-ngoai / gro--mk-tach-T / gro--mk-chon-ngoai
        // ---------------------------------------------------------------------------

        private struct DauMut { public double X, Y; public int Ma; }
        private struct Canh { public int Dinh; public double Goc; public int Net; public int Dau; }

        /// <summary>
        /// gro--mk-vong-ngoai: mỗi thành phần liên thông đi vòng MẶT NGOÀI từ đỉnh trái nhất,
        /// tại mỗi đỉnh chọn cạnh đầu tiên theo chiều kim đồng hồ -> nét chia ngăn / nét cụt bị
        /// loại. Trả list vòng; vòng = list NÉT (list điểm).
        /// </summary>
        public static List<List<List<Diem>>> VongNgoai(List<List<Diem>> hos, double tol)
        {
            int n = hos.Count;
            var eps = new List<DauMut>();
            for (int i = 0; i < n; i++)
            {
                List<Diem> a = hos[i];
                eps.Add(new DauMut { X = a[0].X, Y = a[0].Y, Ma = 2 * i });
                eps.Add(new DauMut { X = a[a.Count - 1].X, Y = a[a.Count - 1].Y, Ma = 2 * i + 1 });
            }
            // sap theo x (on dinh; ban lisp vl-sort co the BO phan tu cung x - khong chep loi do)
            eps = eps.OrderBy(e => e.X).ToList();

            var gan = new Dictionary<int, int>();          // ma dau mut -> dinh
            var toado = new List<Diem>();                   // dinh -> toa do
            for (int q = 0; q < eps.Count; q++)
            {
                DauMut e = eps[q];
                if (gan.ContainsKey(e.Ma)) continue;
                int nid = toado.Count;
                gan[e.Ma] = nid;
                toado.Add(new Diem(e.X, e.Y));
                for (int r = q + 1; r < eps.Count && eps[r].X - e.X < tol; r++)
                {
                    if (Math.Abs(eps[r].Y - e.Y) < tol && !gan.ContainsKey(eps[r].Ma))
                        gan[eps[r].Ma] = nid;
                }
            }

            // canh: dung thu tu cua lisp (cons nguoc): (A_{n-1} B_{n-1} ... A_0 B_0)
            var canh = new List<Canh>();
            for (int i = n - 1; i >= 0; i--)
            {
                List<Diem> a = hos[i];
                canh.Add(new Canh { Dinh = gan[2 * i], Goc = Goc(a[0], a[1]), Net = i, Dau = 0 });
                canh.Add(new Canh { Dinh = gan[2 * i + 1], Goc = Goc(a[a.Count - 1], a[a.Count - 2]), Net = i, Dau = 1 });
            }
            var canhTaiDinh = new Dictionary<int, List<Canh>>();
            foreach (Canh c in canh)
            {
                if (!canhTaiDinh.TryGetValue(c.Dinh, out List<Canh> l)) canhTaiDinh[c.Dinh] = l = new List<Canh>();
                l.Add(c);
            }

            var kq = new List<List<List<Diem>>>();
            var seen = new HashSet<int>();
            // lisp duyet (mapcar 'car toado) - toado cons nguoc -> dinh moi nhat truoc
            for (int x = toado.Count - 1; x >= 0; x--)
            {
                if (seen.Contains(x)) continue;
                var comp = new List<int> { x };
                var hang = new Stack<int>();
                hang.Push(x);
                seen.Add(x);
                while (hang.Count > 0)
                {
                    int u = hang.Pop();
                    if (!canhTaiDinh.TryGetValue(u, out List<Canh> ls)) continue;
                    foreach (Canh cs in ls)
                    {
                        int y = gan[2 * cs.Net + (1 - cs.Dau)];
                        if (seen.Add(y)) { comp.Add(y); hang.Push(y); }
                    }
                }
                // dinh trai nhat (hoa: thap nhat)
                int v0 = comp[0];
                foreach (int y in comp)
                {
                    Diem a = toado[y], e = toado[v0];
                    if (a.X < e.X || (a.X == e.X && a.Y < e.Y)) v0 = y;
                }
                // di vong: canh lui ao huong TAY (pi)
                double lui = Math.PI;
                int luiNet = -1, luiDau = -1;
                int? v = v0;
                int buoc = 0;
                int startNet = -1, startDau = -1;
                var dem = new Dictionary<int, int>();
                var thuTuDem = new List<int>();
                while (v.HasValue && buoc < 4 + 4 * n)
                {
                    bool coBest = false;
                    double bestDl = 0;
                    int bNet = -1, bDau = -1;
                    if (canhTaiDinh.TryGetValue(v.Value, out List<Canh> ls))
                    {
                        foreach (Canh cs in ls)
                        {
                            if (cs.Net == luiNet && cs.Dau == luiDau) continue;
                            double dl = (lui - cs.Goc) % (2.0 * Math.PI);
                            if (dl < 0.0) dl += 2.0 * Math.PI;
                            if (dl <= 1e-12) dl = 2.0 * Math.PI;
                            if (!coBest || dl < bestDl) { coBest = true; bestDl = dl; bNet = cs.Net; bDau = cs.Dau; }
                        }
                    }
                    // chi con canh lui (net cut) -> quay lai theo no
                    if (!coBest && luiNet >= 0) { coBest = true; bNet = luiNet; bDau = luiDau; }
                    if (!coBest) { v = null; continue; }
                    if (startNet < 0) { startNet = bNet; startDau = bDau; }
                    else if (bNet == startNet && bDau == startDau) { v = null; continue; }

                    if (dem.ContainsKey(bNet)) dem[bNet]++;
                    else { dem[bNet] = 1; thuTuDem.Add(bNet); }
                    List<Diem> net = hos[bNet];
                    luiNet = bNet;
                    luiDau = 1 - bDau;
                    lui = bDau == 0 ? Goc(net[net.Count - 1], net[net.Count - 2]) : Goc(net[0], net[1]);
                    v = gan[2 * bNet + (1 - bDau)];
                    buoc++;
                }
                var mot = new List<List<Diem>>();
                foreach (int i in thuTuDem) if (dem[i] == 1) mot.Add(hos[i]);
                if (mot.Count > 0) kq.Add(mot);
            }
            return kq;
        }

        /// <summary>
        /// gro--mk-tach-T: cắt nét tại điểm NỐI CHỮ T (đầu mút nét khác nằm giữa nét này) để có
        /// ĐỈNH CHUNG; không thì tách thành thành phần riêng.
        /// </summary>
        public static List<List<Diem>> TachT(List<List<Diem>> hos, double tol)
        {
            var dau = new List<Diem>();
            foreach (List<Diem> b in hos) { dau.Add(b[0]); dau.Add(b[b.Count - 1]); }
            var out_ = new List<List<Diem>>();
            foreach (List<Diem> b in hos)
            {
                Hop bbb = Hop.TuDiem(b);
                var cat = new List<(int seg, double kc, Diem p)>();
                foreach (Diem p in dau)
                {
                    if (p.X > bbb.X1 - tol && p.X < bbb.X2 + tol && p.Y > bbb.Y1 - tol && p.Y < bbb.Y2 + tol &&
                        KhoangCach(p, b[0]) >= tol && KhoangCach(p, b[b.Count - 1]) >= tol)
                    {
                        int sk = -1;
                        for (int k = 0; k + 1 < b.Count && sk < 0; k++)
                            if (KcDiemDoan(p, b[k], b[k + 1]) < tol) sk = k;
                        if (sk >= 0) cat.Add((sk, KhoangCach(b[sk], p), p));
                    }
                }
                if (cat.Count == 0) { out_.Add(b); continue; }
                cat = cat.OrderBy(c => c.seg).ThenBy(c => c.kc).ToList();
                var moi = new List<Diem> { b[0] };
                Diem? truoc = null;
                for (int k = 0; k + 1 < b.Count; k++)
                {
                    foreach (var c in cat)
                    {
                        if (c.seg == k && (!truoc.HasValue || KhoangCach(c.p, truoc.Value) >= tol))
                        {
                            moi.Add(c.p);
                            out_.Add(moi);
                            moi = new List<Diem> { c.p };
                            truoc = c.p;
                        }
                    }
                    moi.Add(b[k + 1]);
                }
                out_.Add(moi);
            }
            return out_;
        }

        /// <summary>
        /// gro--mk-chon-ngoai: vòng có hộp LỚN NHẤT là biên ngoài, hộp đó phải KHỚP hộp hình
        /// (dung sai max(3, 1%)); các vòng còn lại là lỗ (vùng cấm).
        /// </summary>
        public static (List<List<Diem>> ngoai, List<List<List<Diem>>> lo) ChonNgoai(List<List<List<Diem>>> vongs, Hop bb)
        {
            List<List<Diem>> ngoai = null;
            var lo = new List<List<List<Diem>>>();
            double da = -1.0;
            Hop vlon = null;
            foreach (var vong in vongs)
            {
                Hop vbb = Hop.TuDiem(vong.SelectMany(p => p));
                double dt = vbb.W * vbb.H;
                if (dt > da)
                {
                    if (ngoai != null) lo.Add(ngoai);
                    ngoai = vong; da = dt; vlon = vbb;
                }
                else lo.Add(vong);
            }
            if (ngoai != null)
            {
                double tl = Math.Max(3.0, 0.01 * Math.Max(bb.W, bb.H));
                for (int i = 0; i < 4; i++)
                {
                    if (Math.Abs(vlon[i] - bb[i]) > tl) { ngoai = null; lo = new List<List<List<Diem>>>(); break; }
                }
            }
            return (ngoai, lo);
        }

        /// <summary>
        /// gro--hinh-tam-doan (phần chế độ 2 dùng): đoạn biên ngoài + đoạn các lỗ kín của tấm
        /// trong hộp bb. Biên ngoài = null nếu không tìm được (thử dung sai nối 0.5 rồi 2.0).
        /// </summary>
        public static (List<Doan> ngoai, List<Doan> lo) BienTam(IList<NetMau> net, Hop bb)
        {
            Hop bb1 = bb.No(1.0);
            var kin = new List<List<List<Diem>>>();
            var hos = new List<List<Diem>>();
            foreach (NetMau it in net)
            {
                if (!it.Bb.Giao(bb1)) continue;
                if (!it.LaKegaki && bb1.Chua(it.Bb))
                {
                    if (it.Kin) kin.Insert(0, new List<List<Diem>> { it.Pts });   // entity kin = 1 vong 1 net
                    else if (it.LaKakou) hos.Insert(0, it.Pts);
                }
            }
            foreach (double tol in new[] { 0.5, 2.0 })
            {
                var vongs = new List<List<List<Diem>>>(kin);
                if (hos.Count > 0) vongs.AddRange(VongNgoai(TachT(hos, tol), tol));
                var kq = ChonNgoai(vongs, bb);
                if (kq.ngoai != null)
                {
                    var doanLo = new List<Doan>();
                    foreach (var v in kq.lo) doanLo.AddRange(VongThanhDoan(v));
                    return (VongThanhDoan(kq.ngoai), doanLo);
                }
            }
            return (null, null);
        }

        /// <summary>gro--vong->doan: đoạn theo TỪNG NÉT (không nối điểm giữa các nét).</summary>
        public static List<Doan> VongThanhDoan(List<List<Diem>> vong)
        {
            var out_ = new List<Doan>();
            foreach (var p in vong) out_.AddRange(DiemThanhDoan(p, false));
            return out_;
        }

        // ---------------------------------------------------------------------------
        // Chế độ 2: trượt từ góc trên-trái (mục 122, 136, 138)
        // ---------------------------------------------------------------------------

        /// <summary>gro--hcn-xoay-cham-p: hình chữ nhật tâm (cx cy), nửa cạnh (hw hh), góc th cắt vật cản?</summary>
        public static bool HcnXoayCham(double cx, double cy, double th, double hw, double hh, ChiMuc cm)
        {
            double r = Math.Sqrt(hw * hw + hh * hh), cs = Math.Cos(th), sn = Math.Sin(th);
            int j = cm.J(cy - r), j2 = cm.J(cy + r);
            for (; j <= j2; j++)
            {
                foreach (Doan d in cm.Hang[j])
                {
                    if (d.MinX <= cx + r && d.MaxX >= cx - r && d.MinY <= cy + r && d.MaxY >= cy - r)
                    {
                        double dx1 = d.X1 - cx, dy1 = d.Y1 - cy, dx2 = d.X2 - cx, dy2 = d.Y2 - cy;
                        if (DoanCatHop(dx1 * cs + dy1 * sn, dy1 * cs - dx1 * sn,
                                       dx2 * cs + dy2 * sn, dy2 * cs - dx2 * sn,
                                       -hw, -hh, hw, hh))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>gro--mk2-dem-tia: số lần tia ngang từ (x y) theo hướng h (+1 phải / -1 trái) cắt đoạn.</summary>
        public static int DemTia(double x, double y, int h, ChiMuc cm)
        {
            int n = 0;
            foreach (Doan d in cm.Hang[cm.J(y)])
            {
                double y1 = d.Y1, y2 = d.Y2;
                if ((y1 > y && y2 <= y) || (y2 > y && y1 <= y))
                {
                    double xi = d.X1 + (d.X2 - d.X1) * (y - y1) / (y2 - y1);
                    if (h > 0 ? xi > x : xi < x) n++;
                }
            }
            return n;
        }

        /// <summary>gro--mk2-trong-thit-p: 8 tia từ 4 góc hình chữ nhật, mỗi tia cắt số LẺ lần.</summary>
        public static bool TrongThit8Tia(double cx, double cy, double th, double hw, double hh, ChiMuc cmH, ChiMuc cmV)
        {
            double cs = Math.Cos(th), sn = Math.Sin(th);
            double[,] goc = { { -hw, hh }, { hw, hh }, { -hw, -hh }, { hw, -hh } };
            for (int k = 0; k < 4; k++)
            {
                double lx = goc[k, 0], ly = goc[k, 1];
                double px = cx + (lx * cs - ly * sn);
                double py = cy + (lx * sn + ly * cs);
                if (DemTia(px, py, lx < 0 ? -1 : 1, cmH) % 2 == 0 ||
                    DemTia(py, px, ly < 0 ? -1 : 1, cmV) % 2 == 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// gro--mk2-truot: trượt từng hàng từ TRÊN xuống, mỗi hàng TRÁI -> PHẢI; trả vị trí đầu
        /// tiên thoả (không chạm vật cản kể cả khe c, 4 góc trong thịt). Tối đa 40000 vị trí.
        /// </summary>
        public static bool Truot(Hop bb, double th, double hw, double hh, double c, ChiMuc cmVatCan,
                                 ChiMuc cmH, ChiMuc cmV, double buoc, ref int nCham, ref int nTia,
                                 out double rx, out double ry)
        {
            double ex = hw * Math.Abs(Math.Cos(th)) + hh * Math.Abs(Math.Sin(th)) + c;
            double ey = hw * Math.Abs(Math.Sin(th)) + hh * Math.Abs(Math.Cos(th)) + c;
            double x0 = bb.X1 + ex, x1 = bb.X2 - ex, y = bb.Y2 - ey, y1 = bb.Y1 + ey;
            int dem = 0;
            rx = ry = 0;
            while (y >= y1 && dem < 40000)
            {
                double x = x0;
                while (x <= x1 && dem < 40000)
                {
                    dem++;
                    if (HcnXoayCham(x, y, th, hw + c, hh + c, cmVatCan)) nCham++;
                    else if (!TrongThit8Tia(x, y, th, hw, hh, cmH, cmV)) nTia++;
                    else { rx = x; ry = y; return true; }
                    x += buoc;
                }
                y -= buoc;
            }
            return false;
        }

        /// <summary>
        /// Khe tối thiểu từ cụm mã khắc tới nét (mép tấm / lỗ / kegaki): 1.0 mm; chỉ khi CẢ HAI cạnh
        /// tấm đều &lt; 50 mm mới giảm còn 0.1 mm (mục 187 — trước đây chỉ cần một cạnh &lt; 100,
        /// nên tấm dài mà hẹp như 60 × 1000 cũng bị đặt sát mép 0.1 mm; người dùng chốt 50 × 50).
        /// </summary>
        public static double LeMep(Hop bb) => (bb.W < 50.0 && bb.H < 50.0) ? 0.1 : 1.0;

        /// <summary>
        /// gro--mk2-dat: chế độ 2 cho 1 tấm. Thử góc 0 -> 90 -> 45, tỉ lệ 1 -> 0.5; không được thì
        /// làm lại từ đầu BỎ QUA KEGAKI. daDat = cụm các tấm đã đặt + text khắc chữ (vật cản).
        /// </summary>
        public static KetQuaTam DatMotTam(IList<NetMau> net, Hop bb, double w, double h, IList<Doan> daDat)
        {
            var kq = new KetQuaTam();
            double nho = Math.Min(bb.W, bb.H);
            double c = LeMep(bb);
            double buoc = Math.Max(0.25, Math.Min(2.0, nho / 80.0));   // muc 136

            // gro--mk2-doan: doan cua cac net cham hop hinh (+1mm)
            Hop bb1 = bb.No(1.0);
            var doans = new List<Doan>();
            foreach (NetMau it in net)
                if (it.Bb.Giao(bb1)) doans.AddRange(DiemThanhDoan(it.Pts, it.LaKegaki));
            List<Doan> kakou = doans.Where(d => !d.Keg).ToList();

            // Tia chan-le dem tren BIEN NGOAI THAT + lo kin (muc 119); khong co bien ngoai -> moi net KAKOU
            var bien = BienTam(net, bb);
            List<Doan> tia = bien.ngoai != null ? bien.ngoai.Concat(bien.lo).ToList() : kakou;
            var cmH = new ChiMuc(tia, bb.Y1, bb.Y2);
            var cmV = new ChiMuc(tia.Select(d => d.DoiXy()).ToList(), bb.X1, bb.X2);
            var cmAll = new ChiMuc(doans.Concat(daDat).ToList(), bb.Y1, bb.Y2);
            ChiMuc cmK = null;

            int nCham = 0, nTia = 0;
            foreach (bool boKeg in new[] { false, true })
            {
                if (boKeg && cmK == null) cmK = new ChiMuc(kakou.Concat(daDat).ToList(), bb.Y1, bb.Y2);
                foreach (double s in new[] { 1.0, 0.5 })
                {
                    foreach (double th in new[] { 0.0, Math.PI / 2.0, Math.PI / 4.0 })
                    {
                        double hw = w * s * 0.5, hh = h * s * 0.5;
                        if (Truot(bb, th, hw, hh, c, boKeg ? cmK : cmAll, cmH, cmV, buoc, ref nCham, ref nTia,
                                  out double x, out double y))
                        {
                            kq.Dat = true; kq.Cx = x; kq.Cy = y; kq.Th = th; kq.TiLe = s; kq.BoKeg = boKeg;
                            kq.Hw = hw + c; kq.Hh = hh + c;
                            kq.NCham = nCham; kq.NTia = nTia;
                            return kq;
                        }
                    }
                }
            }
            kq.NCham = nCham; kq.NTia = nTia;
            return kq;
        }

        /// <summary>gro--mk-hcn-doan: 4 cạnh hình chữ nhật xoay -> đoạn (vật cản cho tấm sau).</summary>
        public static List<Doan> HcnThanhDoan(double cx, double cy, double th, double hw, double hh)
        {
            double cs = Math.Cos(th), sn = Math.Sin(th);
            var g = new List<Diem>();
            foreach (var u in new[] { (-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh) })
                g.Add(new Diem(cx + (u.Item1 * cs - u.Item2 * sn), cy + (u.Item1 * sn + u.Item2 * cs)));
            g.Add(g[0]);
            return DiemThanhDoan(g, false);
        }

        /// <summary>Hộp text khắc chữ (mục 156) -> 4 cạnh vật cản, như lisp gọi gro--mk-hcn-doan góc 0.</summary>
        public static List<Doan> HopThanhDoan(Hop b) =>
            HcnThanhDoan((b.X1 + b.X2) / 2.0, (b.Y1 + b.Y2) / 2.0, 0.0, b.W / 2.0, b.H / 2.0);

        // ---------------------------------------------------------------------------
        // Cả lượt: gro--dat-ma-khac-vao-hinh (phần sau khi lisp đã chọn tấm)
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Đặt lần lượt từng tấm theo thứ tự lisp gửi. Cụm đã đặt là vật cản cho tấm sau; hai tấm
        /// CHUNG một hình (hộp hình trùng trong 1mm) -> tấm sau không đặt.
        /// </summary>
        public static KetQuaDatMk ChayHet(IList<TamCanDat> tams, IList<NetMau> net, IList<Hop> vatCanText)
        {
            var kq = new KetQuaDatMk();
            var daDat = new List<Doan>();
            if (vatCanText != null)
                foreach (Hop b in vatCanText) daDat.InsertRange(0, HopThanhDoan(b));
            var bbDa = new List<(Hop bb, string bango)>();
            var dsKeg = new List<string>();
            var dsChua = new List<string>();

            foreach (TamCanDat t in tams)
            {
                kq.Tong++;
                double w = t.BbCum.W, h = t.BbCum.H;
                string trung = null;
                for (int i = bbDa.Count - 1; i >= 0; i--)
                    if (bbDa[i].bb.Bang(t.BbHinh, 1.0)) { trung = bbDa[i].bango; break; }
                bbDa.Add((t.BbHinh, t.Bango));

                KetQuaTam r = trung == null ? DatMotTam(net, t.BbHinh, w, h, daDat) : new KetQuaTam();
                r.Tam = t;
                r.TrungVoi = trung;
                r.DongChanDoan = DongChanDoan(t, w, h, r);
                kq.Tams.Add(r);

                if (r.Dat)
                {
                    kq.SoDat++;
                    daDat.InsertRange(0, HcnThanhDoan(r.Cx, r.Cy, r.Th, r.Hw, r.Hh));
                    if (r.TiLe < 1.0) kq.ThuNho++;
                    if (r.BoKeg) { kq.DeKeg++; dsKeg.Add(t.Bango); }
                }
                else
                {
                    kq.Chua++;
                    dsChua.Add(t.Bango + (trung != null ? " (chung hinh voi bango " + trung + ")" : ""));
                }
            }

            if (kq.Tong > 0)
            {
                var sb = new StringBuilder();
                sb.Append("Dat ma khac vao hinh: ").Append(kq.SoDat).Append('/').Append(kq.Tong)
                  .Append(" tam (thu nho 1/2: ").Append(kq.ThuNho).Append(", de KEGAKI: ").Append(kq.DeKeg).Append(')');
                if (kq.DeKeg > 0)
                    sb.Append("\n!!! Ma khac de KEGAKI (").Append(kq.DeKeg).Append(" tam): bango ").Append(NoiDs(dsKeg, 15));
                if (kq.Chua > 0)
                    sb.Append("\n!!! Chua dat duoc ma khac vao hinh (").Append(kq.Chua)
                      .Append(" tam) - move thu cong: bango ").Append(NoiDs(dsChua, 15));
                kq.ThongBao = sb.ToString();
            }
            return kq;
        }

        /// <summary>Dòng chẩn đoán 1 tấm — giữ đúng chữ của lisp (mục 138).</summary>
        public static string DongChanDoan(TamCanDat t, double w, double h, KetQuaTam r)
        {
            var sb = new StringBuilder();
            sb.Append("  [MK] bango ").Append(t.Bango)
              .Append(" hinh X").Append(F0(t.BbHinh.X1)).Append("..").Append(F0(t.BbHinh.X2))
              .Append(" Y").Append(F0(t.BbHinh.Y1)).Append("..").Append(F0(t.BbHinh.Y2))
              .Append(" cum ").Append(F0(w)).Append('x').Append(F0(h)).Append(" -> ");
            if (r.Dat)
                sb.Append("[truot] dat goc ").Append(F0(r.Th * 180.0 / Math.PI))
                  .Append(" ti le ").Append(F1(r.TiLe)).Append(r.BoKeg ? " BO KEGAKI" : "")
                  .Append(" tam X").Append(F1(r.Cx)).Append(" Y").Append(F1(r.Cy));
            else if (r.TrungVoi != null)
                sb.Append("KHONG - chung hinh voi bango ").Append(r.TrungVoi);
            else
            {
                int tong = r.NCham + r.NTia;
                sb.Append("KHONG - hinh khong du cho (truot ").Append(tong).Append(" vi tri: cham net ")
                  .Append(r.NCham).Append(", ngoai thit ").Append(r.NTia)
                  .Append(tong == 0 ? " - CUM QUA LON" : "").Append(')');
            }
            return sb.ToString();
        }

        /// <summary>gro--noi-ds: nối bằng ", ", tối đa n phần tử (thêm ", ...").</summary>
        public static string NoiDs(IList<string> l, int n)
        {
            string s = string.Join(", ", l.Take(n));
            return l.Count > n ? s + ", ..." : s;
        }

        /// <summary>Thêm cờ vào chuỗi flags (trường thứ 4 XData GRO_METADATA) — gro--gan-co-tam.</summary>
        public static string ThemCo(string flags, string co)
        {
            flags = flags ?? "";
            if (flags.Contains(co)) return flags;
            return flags.Length == 0 ? co : flags + ";" + co;
        }

        // ---------------------------------------------------------------------------
        // Dữ liệu lisp gửi sang: chuỗi "T|bango|bb-hinh|bb-cum|handle-bango|h1;h2..." và "V|bb"
        // ---------------------------------------------------------------------------

        /// <summary>Một dòng tấm đã đọc (handle còn ở dạng chuỗi hex).</summary>
        public sealed class DongTam
        {
            public TamCanDat Tam;
            public string HandleBango;
            public List<string> HandleCum = new List<string>();
        }

        /// <summary>Đọc các chuỗi lisp gửi. Dòng hỏng bỏ qua (đếm vào loi).</summary>
        public static void DocDuLieu(IEnumerable<string> dong, List<DongTam> tams, List<Hop> vatCan, out int loi)
        {
            loi = 0;
            foreach (string s in dong)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                string[] p = s.Split('|');
                if (p[0] == "T" && p.Length >= 6 && DocHop(p[2], out Hop bb) && DocHop(p[3], out Hop bbc))
                {
                    var dt = new DongTam
                    {
                        Tam = new TamCanDat { Bango = p[1].Trim(), BbHinh = bb, BbCum = bbc },
                        HandleBango = p[4].Trim(),
                    };
                    foreach (string hd in p[5].Split(';'))
                        if (hd.Trim().Length > 0) dt.HandleCum.Add(hd.Trim());
                    if (dt.HandleCum.Count > 0) tams.Add(dt); else loi++;
                }
                else if (p[0] == "V" && p.Length >= 2 && DocHop(p[1], out Hop v)) vatCan.Add(v);
                else loi++;
            }
        }

        public static bool DocHop(string s, out Hop hop)
        {
            hop = null;
            if (s == null) return false;
            string[] p = s.Split(',');
            if (p.Length != 4) return false;
            var v = new double[4];
            for (int i = 0; i < 4; i++)
                if (!double.TryParse(p[i].Trim(), NumberStyles.Float, Inv, out v[i])) return false;
            if (v[2] < v[0] || v[3] < v[1]) return false;
            hop = new Hop(v[0], v[1], v[2], v[3]);
            return true;
        }

        private static string F0(double v) => Math.Round(v, MidpointRounding.AwayFromZero).ToString("0", Inv);
        private static string F1(double v) => Math.Round(v, 1, MidpointRounding.AwayFromZero).ToString("0.0", Inv);
    }
}
