using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>
    /// 🔄 Cập nhật theo câu trả lời (12/11): người dùng trả lời đủ câu hỏi ở phòng 2 rồi bấm — AI đọc câu trả lời + thông số tấm
    /// (CHỈ CHỮ) và trả các ô cần đổi (vd "vẫn vát C10" → tham_so c1=10; c2=10). Ô đổi được đánh dấu TheoTraLoi (giữ khi đọc lại,
    /// không tính bài học); câu đã áp → ✓ Đã xử lý.
    /// </summary>
    public static class PhongCapNhat
    {
        /// <summary>Ô AI được đổi (chữ); lo / ke_gaki là JSON mảng.</summary>
        public static readonly string[] ODoi =
        {
            "M", "Z", "T", "Q", "mat", "kich_thuoc", "dung_sai", "ghi_chu_jp", "ghi_chu_vi", "mo_ta",
            "kieu_td", "td_x", "td_y", "ghi_chu_td", "bien_dang", "tham_so", "goc", "khoet", "lo", "ke_gaki",
        };

        public const string LoiDan =
@"Bạn là kỹ sư triển khai kim loại tấm của OHTA. Đầu vào: thông số các tấm của đơn (AI đọc từ bản vẽ, người có thể đã sửa) và các câu hỏi đã được khách / quản lý TRẢ LỜI.
Nhiệm vụ: cập nhật thông số tấm cho ĐÚNG với câu trả lời. Với mỗi tấm có ô cần đổi, trả doi = các {o, gia_tri, ly_do}:
- o: tên ô, đúng một trong: M, Z, T, Q, mat, kich_thuoc, dung_sai, ghi_chu_jp, ghi_chu_vi, mo_ta, kieu_td (phang / be / lam_tay), td_x, td_y (tiết diện dạng chữ: số = đoạn mm xen chỗ uốn b<góc> / a<góc>r<R> / c<cung>r<R> / p<góc>), ghi_chu_td, bien_dang (tên mẫu trong Thư viện), tham_so (""tên=số; …"" theo thông số của mẫu), goc (vát / bo góc viền ngoài: ""<đỉnh> C10; <đỉnh> R5"" — đỉnh = tên góc trai_duoi / phai_duoi / phai_tren / trai_tren, số trên sơ đồ đỉnh, toạ độ x,y hoặc k1.2), khoet (khoét trong: ""□40x60 R5 @ x,y; x,y"" hoặc ""[x,y …] @ x,y"", các khoét cách nhau |), lo, ke_gaki.
- gia_tri: giá trị MỚI đầy đủ của ô (không ghi phần thay đổi). lo: JSON mảng các nhóm {d, so_luong, taro, kieu ("""" tròn / ""sara D"" / ""dai RxL G""), ghi_chu, cach_duong_be, vi_tri [[x, y], …]}; ke_gaki: JSON mảng [[x, y], …].
- Giữ thông số cũ khi gộp: vd goc cũ ""trai_duoi R5"" + câu trả lời thêm vát C10 góc phải dưới → ""trai_duoi R5; phai_duoi C10"". Ô kéo theo cũng phải đổi (đổi kích thước thì đổi cả td_x / td_y; đổi hình thì đổi bien_dang + tham_so; đổi T thì kiểm tiết diện).
- ly_do: ngắn, dẫn câu trả lời (vd ""khách: vẫn vát C10"").
- Câu trả lời không đổi thông số (vd ""đúng"", ""giữ nguyên"") → không tạo doi, nhưng vẫn đưa số câu đó vào da_xu_ly.
- da_xu_ly: SỐ THỨ TỰ các câu trả lời đã được áp / không cần đổi gì. Câu trả lời mơ hồ, mâu thuẫn hoặc thiếu số → KHÔNG đưa vào da_xu_ly, tạo van_de hỏi lại (muc vang, nhom khach / noi_bo, noi_dung một ý ~20 từ).
- ten: đúng tên tấm trong đầu vào. Không bịa số không có trong câu trả lời / thông số.";

        public static JsonObject Schema()
        {
            JsonObject s = Kieu("string"), n = Kieu("number");
            return Obj(("tam", Mang(Obj(("ten", s), ("doi", Mang(Obj(("o", s), ("gia_tri", s), ("ly_do", s))))))),
                       ("da_xu_ly", Mang(n)), ("van_de", Mang(PhongKiemTra.SchemaVanDe())));
        }

        /// <summary>Câu hỏi đã có câu trả lời (thứ tự đánh số gửi AI).</summary>
        public static List<VanDe> CauDaTraLoi(IEnumerable<VanDe> vanDe) => vanDe.Where(v => v.TraLoi.Trim().Length > 0).ToList();

        /// <summary>Phần chữ: tấm (tên &lt;đơn&gt;-n + ô), thư viện mẫu (tên + thông số), câu hỏi + trả lời đánh số.</summary>
        public static string DauVao(IList<(TamAi Tam, string Ten)> tam, IEnumerable<string> thuVien, IList<VanDe> traLoi)
        {
            var sb = new StringBuilder("Các tấm:\n");
            foreach (var (t, ten) in tam)
            {
                sb.Append("## ").Append(ten).Append(" (số BV ").Append(t.SoBanVe.Length > 0 ? t.SoBanVe : "-").Append(", tên ").Append(t.Ten.Length > 0 ? t.Ten : "-").Append(")\n");
                foreach (string o in ODoi)
                {
                    string v = PhongDocBanVe.Lay(t, o);
                    if (!string.IsNullOrWhiteSpace(v)) sb.Append("- ").Append(o).Append(": ").Append(v.Replace("\r", "").Replace("\n", " ")).Append('\n');
                }
            }
            sb.Append("\nThư viện biên dạng:\n");
            foreach (string m in thuVien ?? Enumerable.Empty<string>()) sb.Append("- ").Append(m).Append('\n');
            sb.Append("\nCâu hỏi đã trả lời:\n");
            for (int i = 0; i < traLoi.Count; i++)
                sb.Append(i + 1).Append(". [").Append(traLoi[i].Tam.Length > 0 ? traLoi[i].Tam : "chung").Append("] hỏi: ").Append(traLoi[i].NoiDung)
                  .Append(" | trả lời: ").Append(traLoi[i].TraLoi.Trim()).Append('\n');
            return sb.ToString();
        }

        public static string Body(string model, string quyChuan, string dauVao) =>
            PhongTrienKhai.BodyChung(model, LoiDan, Schema(), quyChuan, null, dauVao, null);

        public sealed class KetQua
        {
            /// <summary>Mô tả từng ô đã đổi: "test2-1 · tham_so: '' → 'c1=10; c2=10' (khách: vẫn vát C10)".</summary>
            public List<string> Doi = new List<string>();
            public HashSet<int> DaXuLy = new HashSet<int>();
            public List<VanDe> VanDeMoi = new List<VanDe>();
            public long Vao, GhiCache, DocCache, Ra;
            public string Loi;
        }

        /// <summary>Đọc phản hồi và ÁP vào các tấm (theo tên &lt;đơn&gt;-n). Ô / tên lạ, JSON lỗ hỏng → bỏ qua, ghi vào Doi dạng "⚠ …".</summary>
        public static KetQua ApDung(string json, IList<(TamAi Tam, string Ten)> tam)
        {
            var kq = new KetQua();
            PhongDocBanVe.PhanHoi p2 = PhongDocBanVe.DocPhanHoi(json);
            kq.Vao = p2.Vao; kq.GhiCache = p2.GhiCache; kq.DocCache = p2.DocCache; kq.Ra = p2.Ra;
            JsonObject goc, nd;
            try { goc = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { goc = null; }
            string text = (goc?.Get("content") as List<object>)?.OfType<JsonObject>().FirstOrDefault(x => x.GetString("type") == "text")?.GetString("text");
            try { nd = text == null ? null : JsonLite.Parse(text) as JsonObject; } catch (Exception) { nd = null; }
            if (nd == null) { kq.Loi = p2.Loi ?? "Phản hồi AI không đúng mẫu"; return kq; }
            foreach (JsonObject x in (nd.Get("tam") as List<object> ?? new List<object>()).OfType<JsonObject>())
            {
                string ten = (x.GetString("ten") ?? "").Trim();
                TamAi t = tam.FirstOrDefault(p => string.Equals(p.Ten, ten, StringComparison.OrdinalIgnoreCase)).Tam;
                if (t == null) { kq.Doi.Add("⚠ không thấy tấm '" + ten + "'"); continue; }
                foreach (JsonObject d in (x.Get("doi") as List<object> ?? new List<object>()).OfType<JsonObject>())
                {
                    string o = (d.GetString("o") ?? "").Trim(), moi = (d.GetString("gia_tri") ?? "").Trim(), lyDo = (d.GetString("ly_do") ?? "").Trim();
                    if (!ODoi.Contains(o)) { kq.Doi.Add("⚠ " + ten + ": ô lạ '" + o + "'"); continue; }
                    if (o == "lo" || o == "ke_gaki")
                    {
                        List<object> ds;
                        try { ds = JsonLite.Parse(moi.Length > 0 ? moi : "[]") as List<object>; } catch (Exception) { ds = null; }
                        if (ds == null) { kq.Doi.Add("⚠ " + ten + ": " + o + " không phải JSON mảng"); continue; }
                        if (o == "lo")
                            foreach (JsonObject l in ds.OfType<JsonObject>())
                            {
                                if (l.Get("vi_tri") is List<object> vt) l.Set("vi_tri", PhongDocBanVe.Diem(vt));
                                PhongDocBanVe.TachKieuLo(l);
                            }
                        moi = JsonLite.Serialize(o == "ke_gaki" ? PhongDocBanVe.Diem(ds) : ds);
                    }
                    string cu = PhongDocBanVe.Lay(t, o);
                    if (cu == moi) continue;
                    PhongDocBanVe.Dat(t, o, moi);
                    t.DaSua.Add(o);          // giữ khi đọc lại bản vẽ
                    t.TheoTraLoi.Add(o);     // nhưng không phải AI đọc sai → không rút bài học
                    t.KhongChac.Remove(o);
                    if (o == "kieu_td" || o == "td_x" || o == "td_y") t.KhongChac.Remove("tiet_dien");
                    kq.Doi.Add(ten + " · " + o + ": " + Ngan(cu) + " → " + Ngan(moi) + (lyDo.Length > 0 ? " (" + lyDo + ")" : ""));
                }
            }
            kq.DaXuLy.UnionWith((nd.Get("da_xu_ly") as List<object> ?? new List<object>()).Select(v => (int)(PhongKiemTra.So(v is JsonNumber jn ? jn.Raw : v as string) ?? 0)).Where(i => i > 0));
            if (nd.Get("van_de") is List<object> vd) kq.VanDeMoi = PhongKiemTra.DocDsVanDe(vd, "don", "ai");
            return kq;
        }

        private static string Ngan(string s)
        {
            string t = (s ?? "").Replace("\r", "").Replace("\n", " ").Trim();
            return "'" + (t.Length > 60 ? t.Substring(0, 60) + "…" : t) + "'";
        }

        private static JsonObject Kieu(string type) { var o = new JsonObject(); o.Set("type", type); return o; }
        private static JsonObject Mang(JsonObject item) { JsonObject a = Kieu("array"); a.Set("items", item); return a; }

        private static JsonObject Obj(params (string Ten, JsonObject Kieu)[] tt)
        {
            var p = new JsonObject();
            foreach (var t in tt) p.Set(t.Ten, t.Kieu);
            JsonObject o = Kieu("object");
            o.Set("properties", p);
            o.Set("required", tt.Select(t => (object)t.Ten).ToList());
            o.Set("additionalProperties", false);
            return o;
        }
    }
}
