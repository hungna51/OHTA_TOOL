using System;
using System.Collections.Generic;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>Một entity đầu vào của bước phân loại nét (đọc từ selection set lisp gửi sang).</summary>
    public sealed class NetVao
    {
        public string Handle;
        /// <summary>"LINE" / "ARC" / "CIRCLE" / loại khác (giữ tên DXF cho dễ đọc dump).</summary>
        public string Loai;
        public string Layer;
        /// <summary>Linetype của CHÍNH entity như .NET trả ("ByLayer", "Continuous"...). Rỗng = ByLayer.</summary>
        public string Linetype;
        /// <summary>Tâm CIRCLE theo OCS (= group 10 lisp đọc) và bán kính. Chỉ có nghĩa với CIRCLE.</summary>
        public double X, Y, R;
        public bool CoTam;
    }

    /// <summary>Một phần tử danh sách circles của lisp: ((x y) r e).</summary>
    public sealed class VongTron
    {
        public NetVao Net;
        public double X, Y, R;
    }

    /// <summary>
    /// Mục 173 (GĐ 3a): chép phần 1 của <c>gro--detect-geom-dims</c> — phân loại nét trên layer gốc
    /// (LINE / ARC Continuous, CIRCLE, CIRCLE rất nhỏ) + gom thêm CIRCLE / ARC cả bản vẽ.
    /// Thuần (không gọi ZWCAD). Lớp CAD: <c>CadServices/NhanNetCad.cs</c>.
    ///
    /// Thứ tự: lisp nhận 3 selection set và <c>cons</c> từng phần tử, nên mỗi danh sách kết quả =
    /// ĐẢO NGƯỢC thứ tự thêm vào. C# trả đúng thứ tự đó (các bước khớp Dim sau còn ở lisp, chúng
    /// duyệt danh sách theo thứ tự nên phải giữ y hệt).
    /// </summary>
    public static class PhanLoaiNet
    {
        public sealed class KetQua
        {
            public List<NetVao> Lines = new List<NetVao>();
            public List<NetVao> Arcs = new List<NetVao>();
            public List<VongTron> Circles = new List<VongTron>();
            public List<VongTron> Tiny = new List<VongTron>();
        }

        /// <summary>Số phần tử tối đa lisp xét ở mỗi lượt quét cả bản vẽ (<c>(&lt; i 500)</c>).</summary>
        public const int ToiDaQuetCaBanVe = 500;

        /// <summary>strcase của lisp: chỉ đổi a–z (không đụng chữ full-width như ToUpperInvariant).</summary>
        public static string HoaAscii(string s)
        {
            if (s == null) return null;
            char[] c = s.ToCharArray();
            for (int i = 0; i < c.Length; i++) if (c[i] >= 'a' && c[i] <= 'z') c[i] = (char)(c[i] - 32);
            return new string(c);
        }

        /// <param name="layGoc">layer gốc người dùng chọn</param>
        /// <param name="ltLayerGoc">linetype của layer gốc (tblsearch group 6), null nếu không có</param>
        /// <param name="ss">selection set lisp đang xử lý, đúng thứ tự ssname</param>
        /// <param name="cirAll">ssget "X" CIRCLE (cả bản vẽ), đúng thứ tự</param>
        /// <param name="arcAll">ssget "X" ARC (cả bản vẽ), đúng thứ tự</param>
        public static KetQua Chay(string layGoc, string ltLayerGoc, IList<NetVao> ss, IList<NetVao> cirAll, IList<NetVao> arcAll)
        {
            var kq = new KetQua();
            string layHoa = HoaAscii(layGoc ?? "");
            var lines = new List<NetVao>();
            var arcs = new List<NetVao>();
            var circles = new List<VongTron>();
            var tiny = new List<VongTron>();

            // 1. Phan loai tren layer goc
            if (ss != null)
                foreach (NetVao n in ss)
                {
                    if (n.Layer == null || HoaAscii(n.Layer) != layHoa) continue;
                    string lt = string.IsNullOrEmpty(n.Linetype) || HoaAscii(n.Linetype) == "BYLAYER" ? ltLayerGoc : n.Linetype;
                    if (lt == null) lt = "Continuous";
                    bool cont = HoaAscii(lt) == "CONTINUOUS";
                    if (n.Loai == "LINE" && cont) lines.Add(n);
                    else if (n.Loai == "ARC" && cont) arcs.Add(n);
                    else if (n.Loai == "CIRCLE" && n.CoTam)
                    {
                        var v = new VongTron { Net = n, X = n.X, Y = n.Y, R = n.R };
                        circles.Add(v);
                        if (n.R <= 1.5) tiny.Add(v);
                    }
                }

            // Them CIRCLE ca ban ve: bo cai co tam cach mot circle da co <= 0.5 (ke ca cai vua them)
            if (cirAll != null)
                for (int i = 0; i < cirAll.Count && i < ToiDaQuetCaBanVe; i++)
                {
                    NetVao n = cirAll[i];
                    if (!n.CoTam) continue;
                    bool trung = false;
                    foreach (VongTron c in circles)
                    {
                        double dx = c.X - n.X, dy = c.Y - n.Y;
                        if (Math.Sqrt(dx * dx + dy * dy) <= 0.5) { trung = true; break; }
                    }
                    if (!trung) circles.Add(new VongTron { Net = n, X = n.X, Y = n.Y, R = n.R });
                }

            // Them ARC ca ban ve (khong xet linetype / layer), bo cai da co (cung entity)
            if (arcAll != null)
            {
                var co = new HashSet<string>(arcs.Select(a => a.Handle), StringComparer.Ordinal);
                for (int i = 0; i < arcAll.Count && i < ToiDaQuetCaBanVe; i++)
                {
                    NetVao n = arcAll[i];
                    if (co.Add(n.Handle)) arcs.Add(n);
                }
            }

            // lisp cons -> dao nguoc thu tu them
            lines.Reverse(); arcs.Reverse(); circles.Reverse(); tiny.Reverse();
            kq.Lines = lines; kq.Arcs = arcs; kq.Circles = circles; kq.Tiny = tiny;
            return kq;
        }

        /// <summary>
        /// WCS -> OCS (thuật toán trục tuỳ ý của DXF) — group 10 của CIRCLE lisp đọc là toạ độ OCS.
        /// Pháp tuyến (0,0,1) thì giữ nguyên số (không tính để khỏi sai bit cuối).
        /// </summary>
        public static void WcsSangOcs(double px, double py, double pz, double nx, double ny, double nz, out double ox, out double oy)
        {
            double l = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (l < 1e-12 || (nx == 0 && ny == 0 && nz > 0)) { ox = px; oy = py; return; }
            nx /= l; ny /= l; nz /= l;
            double ax, ay, az;
            if (Math.Abs(nx) < 1.0 / 64 && Math.Abs(ny) < 1.0 / 64) { ax = nz; ay = 0; az = -nx; }   // Wy x N
            else { ax = -ny; ay = nx; az = 0; }                                                    // Wz x N
            double la = Math.Sqrt(ax * ax + ay * ay + az * az);
            ax /= la; ay /= la; az /= la;
            double bx = ny * az - nz * ay, by = nz * ax - nx * az, bz = nx * ay - ny * ax;      // N x Ax
            ox = px * ax + py * ay + pz * az;
            oy = px * bx + py * by + pz * bz;
        }
    }
}
