using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    /// <summary>
    /// Mô tả các khung in (mỗi khung = 1 tấm) thành <c>.md</c> (gọn, không toạ độ — cho Kev/Claude đọc)
    /// và <c>.json</c> (cùng dữ liệu + toạ độ từng đối tượng). Thông tin tấm chỉ lấy từ F-name.
    /// </summary>
    public static class MoTaKhung
    {
        public const string FileKienThuc = "kien-thuc-co-ban.md";

        public static string Md(string tenBanVe, IList<KhungDuLieu> lo)
        {
            var sb = new StringBuilder();
            sb.Append("# ").Append(tenBanVe).Append('\n')
              .Append("> Kien thuc co ban (tam, F-name, layer...): xem ").Append(FileKienThuc).Append('\n')
              .Append("> So tam (khung in): ").Append(lo.Count).Append('\n');
            foreach (KhungDuLieu k in lo)
            {
                sb.Append("\n## Tam ").Append(k.SoThuTu).Append(" — ").Append(DongChu.TenKhung(k)).Append('\n')
                  .Append("- Kich thuoc khung: ").Append(T(k.MaxX - k.MinX)).Append(" x ").Append(T(k.MaxY - k.MinY)).Append('\n');

                List<string> fname = DongChu.DongFname(k);
                FnameRecord r = DongChu.DemN(fname) == 1 ? FnameParser.Parse(fname) : null;
                if (r != null)
                    sb.Append("- F-name: N=").Append(r.N).Append("; M=").Append(r.M).Append("; T=").Append(r.T)
                      .Append("; Q=").Append(r.Q).Append("; Z=").Append(r.Z)
                      .Append("; BE=").Append(string.Join(" ; ", r.BendLines)).Append('\n');
                else
                    sb.Append("- F-name: ").Append(fname.Count == 0 ? "(khong co)" : string.Join(" | ", fname)).Append('\n');

                List<string> chu = k.DoiTuong.Where(d => !DongChu.LaFname(d)).Select(ChuMd).Where(s => s != null).ToList();
                sb.Append("\n### Chu / ghi chu / kich thuoc\n");
                if (chu.Count == 0) sb.Append("(khong co)\n");
                foreach (string c in chu) sb.Append("- ").Append(c).Append('\n');

                sb.Append("\n### Dem doi tuong theo layer\n");
                foreach (var nhom in DemTheoLayer(k))
                    sb.Append("- ").Append(nhom.Key).Append(": ")
                      .Append(string.Join(", ", nhom.Value.Select(x => x.Value + " " + x.Key))).Append('\n');
            }
            return sb.ToString();
        }

        public static string Json(string tenBanVe, IList<KhungDuLieu> lo)
        {
            var goc = new JsonObject();
            goc.Set("ban_ve", tenBanVe);
            goc.Set("kien_thuc", FileKienThuc);
            var dsTam = new List<object>();
            foreach (KhungDuLieu k in lo)
            {
                var tam = new JsonObject();
                tam.Set("so_thu_tu", k.SoThuTu);
                tam.Set("ten", DongChu.TenKhung(k));
                tam.Set("khung", Hop(k.MinX, k.MinY, k.MaxX, k.MaxY));

                List<string> fname = DongChu.DongFname(k);
                var fn = new JsonObject();
                fn.Set("dong", fname.Cast<object>().ToList());
                if (DongChu.DemN(fname) == 1)
                {
                    FnameRecord r = FnameParser.Parse(fname);
                    fn.Set("N", r.N); fn.Set("M", r.M); fn.Set("T", r.T); fn.Set("Q", r.Q); fn.Set("Z", r.Z);
                    fn.Set("BE", r.BendLines.Cast<object>().ToList());
                }
                tam.Set("fname", fn);

                var dem = new JsonObject();
                foreach (var nhom in DemTheoLayer(k))
                {
                    var o = new JsonObject();
                    foreach (var x in nhom.Value) o.Set(x.Key, x.Value);
                    dem.Set(nhom.Key, o);
                }
                tam.Set("dem_theo_layer", dem);

                var ds = new List<object>();
                foreach (DoiTuong d in k.DoiTuong)
                {
                    var o = new JsonObject();
                    o.Set("loai", d.Loai.ToString());
                    o.Set("layer", d.Layer);
                    if (d.NoiDung != null) o.Set("noi_dung", d.NoiDung);
                    o.Set("hop", Hop(d.MinX, d.MinY, d.MaxX, d.MaxY));
                    o.Set("chi_tiet", DongChu.Dong(d)); // toạ độ đủ, cùng định dạng dòng gửi Kev
                    ds.Add(o);
                }
                tam.Set("doi_tuong", ds);
                dsTam.Add(tam);
            }
            goc.Set("tam", dsTam);
            return JsonLite.Serialize(goc);
        }

        private static string ChuMd(DoiTuong d)
        {
            switch (d.Loai)
            {
                case LoaiDoiTuong.Text:
                    return "[" + d.Layer + "] " + string.Join(" / ", FnameParser.SplitAndCleanMText(d.NoiDung));
                case LoaiDoiTuong.Dim:
                    return "[" + d.Layer + "] DIM " + T(d.GiaTriDo) + (string.IsNullOrEmpty(d.GhiDe) ? "" : " (ghi de: " + d.GhiDe + ")");
                default:
                    return null;
            }
        }

        /// <summary>Layer (theo tên) → [loại → số lượng], loại xếp theo số lượng giảm.</summary>
        private static List<KeyValuePair<string, List<KeyValuePair<string, int>>>> DemTheoLayer(KhungDuLieu k) =>
            k.DoiTuong.GroupBy(d => d.Layer ?? "", StringComparer.OrdinalIgnoreCase)
             .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
             .Select(g => new KeyValuePair<string, List<KeyValuePair<string, int>>>(g.Key,
                 g.GroupBy(d => d.Loai.ToString()).OrderByDescending(x => x.Count()).ThenBy(x => x.Key)
                  .Select(x => new KeyValuePair<string, int>(x.Key, x.Count())).ToList()))
             .ToList();

        private static List<object> Hop(double x1, double y1, double x2, double y2) =>
            new List<object> { Math.Round(x1, 3), Math.Round(y1, 3), Math.Round(x2, 3), Math.Round(y2, 3) };

        private static string T(double v) => HinhHoc.Tron(v);
    }
}
