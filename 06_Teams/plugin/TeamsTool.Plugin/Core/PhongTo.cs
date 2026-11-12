using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>
    /// Lượt "đọc phóng to" (12/11): API đọc PDF bằng ảnh thu nhỏ trang — bản vẽ dày đặc (fax A3) thì chi tiết một tấm chỉ còn
    /// vài trăm điểm ảnh, AI đọc sót lỗ / nhầm số (vd 140 → 1400). Sau lượt đọc cả trang, gửi ẢNH CẮT vùng tấm (250 dpi, nới rộng)
    /// + dữ liệu đã đọc để AI đọc kỹ và sửa lại số, lỗ, ケガキ, dim khách; đối chiếu ghi chú với dim; bỏ nghi vấn đã rõ.
    /// </summary>
    public static class PhongPhongTo
    {
        public const int MaxTokens = 16000;

        public const string LoiDan =
@"Bạn là kỹ sư triển khai kim loại tấm của OHTA. Đầu vào: ẢNH PHÓNG TO vùng MỘT tấm cắt từ bản vẽ khách (có thể lẫn phần hình khác ở mép — chỉ xét tấm được mô tả) và dữ liệu lượt đọc cả trang (đọc trên ảnh nhỏ nên CÓ THỂ SAI: nhầm số, sót lỗ, sót kích thước).
Đọc KỸ ảnh và trả lại bản ĐÚNG của các ô dưới đây (giữ đúng giá trị đầu vào nếu ảnh không cho thấy khác):
- M, T, Q, kich_thuoc (""rộng x dài""), td_x, td_y (tiết diện dạng chữ: số = đoạn mm xen chỗ uốn b<góc> / a<góc>r<R> / c<cung>r<R> / p<góc>), tham_so (""r1=45; r2=38"" nếu mẫu có thông số riêng), mo_ta (mô tả chi tiết hình đủ để vẽ lại).
- lo: mỗi nhóm lỗ {d, so_luong, taro, kieu ("""" tròn / ""sara D"" / ""dai RxL G""), ghi_chu, cach_duong_be, vi_tri [[x, y], …] = TÂM từng lỗ trên hình TRẢI PHẲNG, gốc góc trái dưới của tấm, tính từ kích thước ghi trên bản vẽ (vd ""100P×5=500"" = 6 lỗ cách đều 100)}.
- goc: vát / bo góc viền ngoài ""<đỉnh> C10; <đỉnh> R5"" (đỉnh = trai_duoi / phai_duoi / phai_tren / trai_tren hoặc x,y). khoet: khoét trong (không phải lỗ) ""□rộngxcao R<r> G<góc> @ x,y; x,y"" hoặc ""[x,y x,y …] @ x,y"", cách nhau |.
- ke_gaki: [[x, y], …] tâm từng dấu khắc vạch. dim_khach: MỌI kích thước khách ghi cho tấm {loai ngang / doc / r / phi / goc, gia_tri, tu, den}.
- ĐỐI CHIẾU ghi chú (vd ""140*600"", ""PL9"", ""数量2個"") với dim trên hình: khớp → dùng; KHÔNG khớp → để kich_thuoc theo ghi chú, thêm kich_thuoc vào khong_chac và tạo van_de muc do nhom khach nêu cả hai số.
- khong_chac: các ô vẫn chưa chắc sau khi xem ảnh. van_de: vấn đề MỚI thấy trên ảnh (cùng cách viết: tam, muc do / vang / info, nhom tu_xu_ly / noi_bo / khach, muc_qc, noi_dung một ý ~20 từ, lien_lac).
- bo_nghi_van: SỐ THỨ TỰ các ""Nghi vấn đã có"" mà ảnh phóng to đã trả lời được (vd đã đọc được vị trí lỗ) — để bỏ.
Không bịa: không đọc được thì giữ đầu vào và đưa vào khong_chac.";

        public static JsonObject Schema()
        {
            JsonObject s = Kieu("string"), n = Kieu("number"), cap = Mang(Mang(n));
            return Obj(("M", s), ("T", s), ("Q", s), ("kich_thuoc", s), ("td_x", s), ("td_y", s), ("tham_so", s), ("goc", s), ("khoet", s), ("mo_ta", s),
                       ("lo", Mang(Obj(("d", n), ("so_luong", n), ("taro", s), ("kieu", s), ("ghi_chu", s), ("cach_duong_be", n), ("vi_tri", cap)))),
                       ("ke_gaki", cap), ("dim_khach", Mang(Obj(("loai", s), ("gia_tri", n), ("tu", n), ("den", n)))),
                       ("khong_chac", Mang(s)), ("van_de", Mang(PhongKiemTra.SchemaVanDe())), ("bo_nghi_van", Mang(n)));
        }

        /// <summary>Phần chữ: tấm (tên để AI ghi vào van_de.tam), dữ liệu lượt đọc cả trang, nghi vấn đã có (đánh số).</summary>
        public static string DauVao(TamAi t, string bienDangMoTa, IList<VanDe> nghiVan)
        {
            var sb = new StringBuilder();
            sb.Append("Tấm: ").Append(PhongKiemTra.TenTam(t)).Append(" (tệp ").Append(t.Tep).Append(", trang ").Append(t.Trang).Append(")\n");
            sb.Append("Dữ liệu lượt đọc cả trang:\n");
            foreach (var (o, v) in new[] { ("M", t.M), ("T", t.T), ("Q", t.Q), ("kich_thuoc", t.KichThuoc), ("mat", t.Mat), ("kieu_td", t.KieuTd), ("td_x", t.TdX), ("td_y", t.TdY),
                                           ("bien_dang", t.BienDang + (bienDangMoTa.Length > 0 ? " — " + bienDangMoTa : "")), ("tham_so", t.ThamSo), ("goc", t.Goc), ("khoet", t.Khoet), ("mo_ta", t.MoTa),
                                           ("ghi_chu_jp", t.GhiChuJp), ("lo", t.LoJson), ("ke_gaki", t.KeGakiJson), ("dim_khach", t.DimKhachJson) })
                sb.Append("- ").Append(o).Append(": ").Append(string.IsNullOrWhiteSpace(v) ? "(trống)" : v.Replace("\r", "").Replace("\n", " ")).Append('\n');
            if (t.DaSua.Count > 0) sb.Append("(Người dùng đã sửa — coi là đúng: ").Append(string.Join(", ", t.DaSua.OrderBy(x => x))).Append(")\n");
            sb.Append("\nNghi vấn đã có:\n");
            if (nghiVan.Count == 0) sb.Append("(không có)\n");
            for (int i = 0; i < nghiVan.Count; i++) sb.Append(i + 1).Append(". ").Append(nghiVan[i].NoiDung).Append('\n');
            return sb.ToString();
        }

        /// <summary>Body POST /v1/messages: system = lời dặn + quy chuẩn (cache), user = ảnh PNG + chữ.</summary>
        public static string Body(string model, string quyChuan, string pngBase64, string dauVao)
        {
            var body = new JsonObject();
            body.Set("model", model);
            body.Set("max_tokens", MaxTokens);
            if (PhongDocBanVe.CoFallback(model)) body.Set("fallbacks", "default");
            var format = new JsonObject();
            format.Set("type", "json_schema");
            format.Set("schema", Schema());
            var oc = new JsonObject();
            oc.Set("effort", "high");
            oc.Set("format", format);
            body.Set("output_config", oc);
            var cache = new JsonObject();
            cache.Set("type", "ephemeral");
            body.Set("system", new List<object> { Khoi(LoiDan, null), Khoi(quyChuan, cache) });
            var src = new JsonObject();
            src.Set("type", "base64"); src.Set("media_type", "image/png"); src.Set("data", pngBase64);
            var anh = new JsonObject();
            anh.Set("type", "image"); anh.Set("source", src);
            var msg = new JsonObject();
            msg.Set("role", "user");
            msg.Set("content", new List<object> { anh, Khoi(dauVao, null) });
            body.Set("messages", new List<object> { msg });
            return JsonLite.Serialize(body);
        }

        public sealed class PhanHoi
        {
            public JsonObject Nd;
            public long Vao, GhiCache, DocCache, Ra;
            public string Loi;
        }

        public static PhanHoi DocPhanHoi(string json)
        {
            var kq = new PhanHoi();
            PhongDocBanVe.PhanHoi p2 = PhongDocBanVe.DocPhanHoi(json); // token + lỗi chung
            kq.Vao = p2.Vao; kq.GhiCache = p2.GhiCache; kq.DocCache = p2.DocCache; kq.Ra = p2.Ra;
            JsonObject goc;
            try { goc = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { goc = null; }
            string text = (goc?.Get("content") as List<object>)?.OfType<JsonObject>().FirstOrDefault(x => x.GetString("type") == "text")?.GetString("text");
            try { kq.Nd = text == null ? null : JsonLite.Parse(text) as JsonObject; } catch (Exception) { kq.Nd = null; }
            if (kq.Nd == null) kq.Loi = p2.Loi ?? "Phản hồi AI không đúng mẫu";
            return kq;
        }

        /// <summary>
        /// Áp kết quả phóng to vào tấm: ô người đã sửa giữ nguyên; ô chữ rỗng / mảng rỗng của AI không xoá dữ liệu cũ.
        /// Trả tên các ô đã đổi.
        /// </summary>
        public static List<string> ApDung(TamAi t, JsonObject nd)
        {
            var doi = new List<string>();
            foreach (string o in new[] { "M", "T", "Q", "kich_thuoc", "td_x", "td_y", "tham_so", "goc", "khoet", "mo_ta" })
            {
                string v = Chu(nd.Get(o)).Trim();
                if (v.Length == 0 || t.DaSua.Contains(o) || v == PhongDocBanVe.Lay(t, o)) continue;
                PhongDocBanVe.Dat(t, o, v);
                doi.Add(o);
            }
            if (!t.DaSua.Contains("lo") && nd.Get("lo") is List<object> lo && lo.Count > 0)
            {
                foreach (JsonObject l in lo.OfType<JsonObject>())
                {
                    if (l.Get("vi_tri") is List<object> vt) l.Set("vi_tri", PhongDocBanVe.Diem(vt));
                    PhongDocBanVe.TachKieuLo(l);
                }
                string j = JsonLite.Serialize(lo);
                if (j != t.LoJson) { t.LoJson = j; doi.Add("lo"); }
            }
            if (!t.DaSua.Contains("ke_gaki") && nd.Get("ke_gaki") is List<object> kg && kg.Count > 0)
            {
                string j = JsonLite.Serialize(PhongDocBanVe.Diem(kg));
                if (j != t.KeGakiJson) { t.KeGakiJson = j; doi.Add("ke_gaki"); }
            }
            if (nd.Get("dim_khach") is List<object> dk && dk.Count > 0)
            {
                string j = JsonLite.Serialize(dk);
                if (j != t.DimKhachJson) { t.DimKhachJson = j; doi.Add("dim_khach"); }
            }
            if (nd.Get("khong_chac") is List<object> kc)
            {
                // chỉ giữ tên ô (AI hay ghi cả câu / "lo.d"): "lo.vi_tri y…" → lo; câu không ra tên ô thì bỏ — giải thích đã ở van_de
                var hopLe = new HashSet<string>(PhongDocBanVe.O.Concat(PhongDocBanVe.OTd).Concat(new[] { "lo", "ke_gaki", "dim_khach", "tiet_dien", "canh" }));
                t.KhongChac.Clear();
                foreach (string x in kc.Select(Chu))
                {
                    string o = new string(x.Trim().TakeWhile(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
                    if (hopLe.Contains(o) && !t.DaSua.Contains(o)) t.KhongChac.Add(o);
                }
            }
            return doi;
        }

        /// <summary>Số thứ tự (1…) các nghi vấn AI báo đã rõ.</summary>
        public static HashSet<int> NghiVanDaRo(JsonObject nd) =>
            new HashSet<int>((nd.Get("bo_nghi_van") as List<object> ?? new List<object>()).Select(x => (int)(PhongKiemTra.So(Chu(x)) ?? 0)).Where(i => i > 0));

        private static string Chu(object v) => v is string s ? s : v is JsonNumber n ? n.Raw : v is bool b ? (b ? "true" : "false") : "";
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

        private static JsonObject Khoi(string text, JsonObject cache)
        {
            var o = new JsonObject();
            o.Set("type", "text");
            o.Set("text", string.IsNullOrEmpty(text) ? "(trống)" : text);
            if (cache != null) o.Set("cache_control", cache);
            return o;
        }
    }
}
