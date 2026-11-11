using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một tấm (phòng 2). Thuộc tính chữ để DataGrid sửa thẳng; Canh / Lo giữ JSON gốc của AI cho phòng 3–4.</summary>
    public sealed class TamAi
    {
        public string Tep { get; set; } = "";
        public string Trang { get; set; } = "";
        public string SoBanVe { get; set; } = "";
        public string Ten { get; set; } = "";
        public string M { get; set; } = "";
        public string Z { get; set; } = "";
        public string T { get; set; } = "";
        public string Q { get; set; } = "";
        public string Mat { get; set; } = "";
        public string KichThuoc { get; set; } = "";
        public string DungSai { get; set; } = "";
        public string GhiChuJp { get; set; } = "";
        public string GhiChuVi { get; set; } = "";
        /// <summary>Đơn nửa repeat: tấm mới (phòng 3–4 chỉ chạy tấm được tích).</summary>
        public bool Moi { get; set; } = true;
        public string CanhJson = "[]", LoJson = "[]";
        public HashSet<string> KhongChac = new HashSet<string>(); // ô AI không chắc / AI tự chọn
        public HashSet<string> DaSua = new HashSet<string>();     // ô người dùng đã sửa — chạy lại không ghi đè

        public string TomTatCanh => TomTat(CanhJson, "chỗ bẻ");
        public string TomTatLo => TomTat(LoJson, "lỗ");
        public string CanhBao => KhongChac.Count == 0 ? "" : "⚠ " + string.Join(", ", KhongChac.OrderBy(x => x));

        private static string TomTat(string json, string ten)
        {
            int n = 0;
            try { n = (JsonLite.Parse(json) as List<object>)?.Count ?? 0; } catch (Exception) { }
            return n == 0 ? "" : n + " " + ten;
        }

        /// <summary>Khoá ghép tấm giữa 2 lần chạy: tệp + số bản vẽ + tên.</summary>
        public string Khoa => (Tep + "|" + SoBanVe + "|" + Ten).ToLowerInvariant();
    }

    /// <summary>Kết quả phòng 2, lưu &lt;thư mục đơn&gt;\ai\chi_tiet.json.</summary>
    public sealed class ChiTiet
    {
        public List<TamAi> Tam = new List<TamAi>();
        public Dictionary<string, string> MaBamDaDoc = new Dictionary<string, string>(); // tên tệp → mã băm lúc AI đọc
        public long TokenVao, TokenRa;
        public decimal Tien;
    }

    /// <summary>Phòng 2 — Đọc bản vẽ: model + JSON + dựng yêu cầu Claude + đọc phản hồi (logic thuần).</summary>
    public static class PhongDocBanVe
    {
        /// <summary>Các ô chữ (tên JSON ↔ thuộc tính).</summary>
        public static readonly string[] O = { "so_ban_ve", "ten", "M", "Z", "T", "Q", "mat", "kich_thuoc", "dung_sai", "ghi_chu_jp", "ghi_chu_vi" };

        public static string Lay(TamAi t, string o)
        {
            switch (o)
            {
                case "so_ban_ve": return t.SoBanVe; case "ten": return t.Ten; case "M": return t.M; case "Z": return t.Z;
                case "T": return t.T; case "Q": return t.Q; case "mat": return t.Mat; case "kich_thuoc": return t.KichThuoc;
                case "dung_sai": return t.DungSai; case "ghi_chu_jp": return t.GhiChuJp; case "ghi_chu_vi": return t.GhiChuVi;
                default: return "";
            }
        }

        public static void Dat(TamAi t, string o, string v)
        {
            v = v ?? "";
            switch (o)
            {
                case "so_ban_ve": t.SoBanVe = v; break; case "ten": t.Ten = v; break; case "M": t.M = v; break; case "Z": t.Z = v; break;
                case "T": t.T = v; break; case "Q": t.Q = v; break; case "mat": t.Mat = v; break; case "kich_thuoc": t.KichThuoc = v; break;
                case "dung_sai": t.DungSai = v; break; case "ghi_chu_jp": t.GhiChuJp = v; break; case "ghi_chu_vi": t.GhiChuVi = v; break;
            }
        }

        /// <summary>
        /// Ghép lần chạy mới vào kết quả cũ: tấm cùng khoá giữ các ô người dùng đã sửa (+ ô tích Mới);
        /// tấm của tệp không đọc lại (keepTep) giữ nguyên; tấm cũ của tệp vừa đọc lại mà không còn → bỏ.
        /// </summary>
        public static List<TamAi> Gop(List<TamAi> cu, List<TamAi> moi, ISet<string> tepDocLai)
        {
            var kq = cu.Where(t => !tepDocLai.Contains(t.Tep)).ToList();
            var theoKhoa = cu.Where(t => tepDocLai.Contains(t.Tep)).GroupBy(t => t.Khoa).ToDictionary(g => g.Key, g => g.First());
            foreach (TamAi t in moi)
            {
                if (theoKhoa.TryGetValue(t.Khoa, out TamAi c))
                {
                    foreach (string o in c.DaSua) { Dat(t, o, Lay(c, o)); t.KhongChac.Remove(o); }
                    t.DaSua.UnionWith(c.DaSua);
                    t.Moi = c.Moi;
                }
                kq.Add(t);
            }
            return kq;
        }

        // ---------- chi_tiet.json ----------

        public static string Ghi(ChiTiet ct)
        {
            var o = new JsonObject();
            o.Set("token_vao", new JsonNumber(ct.TokenVao.ToString(CultureInfo.InvariantCulture)));
            o.Set("token_ra", new JsonNumber(ct.TokenRa.ToString(CultureInfo.InvariantCulture)));
            o.Set("tien", new JsonNumber(ct.Tien.ToString(CultureInfo.InvariantCulture)));
            var mb = new JsonObject();
            foreach (var kv in ct.MaBamDaDoc) mb.Set(kv.Key, kv.Value);
            o.Set("ma_bam_da_doc", mb);
            o.Set("tam", ct.Tam.Select(t =>
            {
                var x = new JsonObject();
                x.Set("tep", t.Tep); x.Set("trang", t.Trang);
                foreach (string k in O) x.Set(k, Lay(t, k));
                x.Set("canh", SafeParse(t.CanhJson));
                x.Set("lo", SafeParse(t.LoJson));
                x.Set("moi", t.Moi);
                x.Set("khong_chac", t.KhongChac.OrderBy(s => s).Cast<object>().ToList());
                x.Set("da_sua", t.DaSua.OrderBy(s => s).Cast<object>().ToList());
                return (object)x;
            }).ToList());
            return JsonLite.Serialize(o);
        }

        public static ChiTiet Doc(string json)
        {
            JsonObject o;
            try { o = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { return null; }
            if (o == null) return null;
            var ct = new ChiTiet { TokenVao = (long)So(o, "token_vao"), TokenRa = (long)So(o, "token_ra"), Tien = (decimal)So(o, "tien") };
            if (o.Get("ma_bam_da_doc") is JsonObject mb)
                foreach (var kv in mb.Items) ct.MaBamDaDoc[kv.Key] = kv.Value as string ?? "";
            foreach (JsonObject x in (o.Get("tam") as List<object> ?? new List<object>()).OfType<JsonObject>())
            {
                TamAi t = DocTam(x);
                t.Tep = Chu(x.Get("tep"));
                t.Moi = !(x.Get("moi") is bool b) || b;
                t.DaSua.UnionWith(DsChu(x.Get("da_sua")));
                ct.Tam.Add(t);
            }
            return ct;
        }

        // ---------- Yêu cầu Claude ----------

        public const string Url = "https://api.anthropic.com/v1/messages";
        public const string PhienBanApi = "2026-10-21";
        public const string BetaFallback = "server-side-fallback-2026-07-01";
        public const string ModelMacDinh = "claude-opus-5-5";
        public const int MaxTokens = 32000;
        /// <summary>Giới hạn request 32 MB; base64 tăng ~4/3 → PDF trên ~22 MB thì báo.</summary>
        public const long PdfToiDa = 22L * 1024 * 1024;

        public static bool CoFallback(string model) => model == "claude-sonnet-5-5" || model == "claude-opus-5-5";

        /// <summary>
        /// Body POST /v1/messages: system = lời dặn + quy chuẩn (đánh dấu cache — đơn nhiều tệp gọi nhiều lần),
        /// user = PDF (document base64) hoặc tóm tắt DXF (chữ). Trả JSON theo Schema().
        /// </summary>
        public static string Body(string model, string loiDan, string quyChuan, string tenTep, string pdfBase64, string tomTatDxf)
        {
            var body = new JsonObject();
            body.Set("model", model);
            body.Set("max_tokens", MaxTokens);
            if (CoFallback(model)) body.Set("fallbacks", "default");
            var format = new JsonObject();
            format.Set("type", "json_schema");
            format.Set("schema", Schema());
            var oc = new JsonObject();
            oc.Set("effort", "high"); // đọc bản vẽ cần kỹ; Opus 5.5 mặc định medium
            oc.Set("format", format);
            body.Set("output_config", oc);

            var cache = new JsonObject();
            cache.Set("type", "ephemeral");
            body.Set("system", new List<object> { Khoi(loiDan, null), Khoi(quyChuan, cache) });

            var noiDung = new List<object>();
            if (!string.IsNullOrEmpty(pdfBase64))
            {
                var src = new JsonObject();
                src.Set("type", "base64");
                src.Set("media_type", "application/pdf");
                src.Set("data", pdfBase64);
                var doc = new JsonObject();
                doc.Set("type", "document");
                doc.Set("source", src);
                noiDung.Add(doc);
            }
            noiDung.Add(Khoi("Tệp: " + tenTep + (string.IsNullOrEmpty(tomTatDxf) ? "" : "\n\n" + tomTatDxf), null));
            var msg = new JsonObject();
            msg.Set("role", "user");
            msg.Set("content", noiDung);
            body.Set("messages", new List<object> { msg });
            return JsonLite.Serialize(body);
        }

        public const string LoiDan =
@"Bạn là kỹ sư triển khai kim loại tấm của OHTA. Đọc bản vẽ khách hàng gửi (PDF, hoặc tóm tắt DXF) và liệt kê MỖI TẤM một phần tử trong ""tam"".
- Theo QUY CHUẨN ở khối sau. Vật liệu M và chế độ cắt Z dùng đúng tên trong bảng của quy chuẩn (C-1, C-2). Bản vẽ không ghi vật liệu → chọn theo C-1 và thêm ""M"" vào khong_chac.
- T: độ dày danh nghĩa (số, mm). Q: số lượng (số). kich_thuoc: kích thước bao ""dài x rộng"" (mm).
- canh: mỗi chỗ bẻ / nhấn / cuộn: dai_ngoai = các kích thước ngoài hai cạnh kề (mm), goc (độ), r_trong (mm, 0 nếu không ghi), kieu (be / an_1_lan / pitchi / cuon / khac), huong (V hoặc N, rỗng nếu không rõ).
- lo: mỗi nhóm lỗ: d (Ø mm), so_luong, taro (vd ""M6"" nếu là lỗ ren, rỗng nếu không), sara (true nếu là lỗ 皿), ghi_chu.
- mat: bề mặt đặc biệt (HL, #400, 縞板/sima, ミガキ…), rỗng nếu thường. dung_sai: các dung sai ghi trên bản vẽ.
- ghi_chu_jp: ghi chú tiếng Nhật giữ nguyên chữ; ghi_chu_vi: dịch sang tiếng Việt, ngắn gọn.
- trang: số trang PDF chứa tấm (1 = trang đầu); DXF để rỗng.
- khong_chac: tên các ô bạn đọc không chắc hoặc tự chọn (so_ban_ve, ten, M, Z, T, Q, mat, kich_thuoc, dung_sai, canh, lo). Không bịa: không đọc được thì để rỗng và đưa vào khong_chac.
- Trang không phải bản vẽ chi tiết (đơn đặt hàng, email, bảng kê) → không tạo tấm cho trang đó.";

        private static JsonObject Khoi(string text, JsonObject cache)
        {
            var o = new JsonObject();
            o.Set("type", "text");
            o.Set("text", string.IsNullOrEmpty(text) ? "(trống)" : text);
            if (cache != null) o.Set("cache_control", cache);
            return o;
        }

        private static JsonObject Kieu(string type)
        {
            var o = new JsonObject();
            o.Set("type", type);
            return o;
        }

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

        private static JsonObject Mang(JsonObject item)
        {
            JsonObject a = Kieu("array");
            a.Set("items", item);
            return a;
        }

        public static JsonObject Schema()
        {
            JsonObject s = Kieu("string"), n = Kieu("number");
            var tt = new List<(string, JsonObject)>();
            foreach (string o in O) tt.Add((o, s));
            tt.Add(("trang", s));
            tt.Add(("canh", Mang(Obj(("dai_ngoai", Mang(n)), ("goc", n), ("r_trong", n), ("kieu", s), ("huong", s)))));
            tt.Add(("lo", Mang(Obj(("d", n), ("so_luong", n), ("taro", s), ("sara", Kieu("boolean")), ("ghi_chu", s)))));
            tt.Add(("khong_chac", Mang(s)));
            return Obj(("tam", Mang(Obj(tt.ToArray()))));
        }

        /// <summary>Kết quả đọc phản hồi: tấm (Tep gán bên ngoài), token, lỗi (null = được).</summary>
        public sealed class PhanHoi
        {
            public List<TamAi> Tam = new List<TamAi>();
            public long Vao, GhiCache, DocCache, Ra;
            public string Loi;
        }

        public static PhanHoi DocPhanHoi(string json)
        {
            var kq = new PhanHoi();
            JsonObject goc;
            try { goc = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { goc = null; }
            if (goc == null) { kq.Loi = "Phản hồi AI không đọc được"; return kq; }
            if (goc.Get("error") is JsonObject err)
            {
                string msg = err.GetString("message") ?? "";
                switch (err.GetString("type"))
                {
                    case "authentication_error": kq.Loi = "API key sai hoặc đã bị xoá (05_AI\\data\\ai.key)"; break;
                    case "rate_limit_error": case "overloaded_error": kq.Loi = "AI đang quá tải, thử lại sau ít phút"; break;
                    case "billing_error": kq.Loi = "Tài khoản API hết tiền / chưa nạp tiền"; break;
                    default: kq.Loi = msg.Length > 0 ? msg : "AI báo lỗi"; break;
                }
                return kq;
            }
            if (goc.Get("usage") is JsonObject u)
            {
                kq.Vao = (long)So(u, "input_tokens"); kq.GhiCache = (long)So(u, "cache_creation_input_tokens");
                kq.DocCache = (long)So(u, "cache_read_input_tokens"); kq.Ra = (long)So(u, "output_tokens");
            }
            string stop = goc.GetString("stop_reason");
            if (stop == "refusal") { kq.Loi = "AI từ chối đọc tệp này"; return kq; }
            if (stop == "max_tokens") { kq.Loi = "Phản hồi bị cắt (tệp quá nhiều tấm) — tách PDF rồi chạy lại"; return kq; }
            string text = (goc.Get("content") as List<object>)?.OfType<JsonObject>().FirstOrDefault(b => b.GetString("type") == "text")?.GetString("text");
            JsonObject nd;
            try { nd = text == null ? null : JsonLite.Parse(text) as JsonObject; } catch (Exception) { nd = null; }
            if (!(nd?.Get("tam") is List<object> ds)) { kq.Loi = "Phản hồi AI không đúng mẫu"; return kq; }
            foreach (JsonObject x in ds.OfType<JsonObject>()) kq.Tam.Add(DocTam(x));
            return kq;
        }

        /// <summary>Giá USD / 1 triệu token (10/2026, như 05_AI BangGia). Ghi cache = 1.25 × giá vào.</summary>
        public static decimal Tien(string model, long vao, long ghiCache, long docCache, long ra)
        {
            (decimal Vao, decimal Ra, decimal Doc) g;
            switch (model)
            {
                case "claude-opus-5-5": g = (4m, 20m, 0.2m); break;
                case "claude-sonnet-5-5": g = (2m, 10m, 0.2m); break;
                case "claude-haiku-5-5": g = (0.1m, 0.5m, 0.01m); break;
                default: return 0m;
            }
            return (vao * g.Vao + ghiCache * g.Vao * 1.25m + docCache * g.Doc + ra * g.Ra) / 1000000m;
        }

        // ---------- tiện ích ----------

        private static TamAi DocTam(JsonObject x)
        {
            var t = new TamAi { Trang = Chu(x.Get("trang")) };
            foreach (string o in O) Dat(t, o, Chu(x.Get(o)));
            t.CanhJson = JsonLite.Serialize(x.Get("canh") as List<object> ?? new List<object>());
            t.LoJson = JsonLite.Serialize(x.Get("lo") as List<object> ?? new List<object>());
            t.KhongChac.UnionWith(DsChu(x.Get("khong_chac")));
            return t;
        }

        private static IEnumerable<string> DsChu(object v) => (v as List<object> ?? new List<object>()).Select(Chu).Where(s => s.Length > 0);

        /// <summary>Chuỗi / số JSON → chữ (AI đôi khi trả số cho ô chữ).</summary>
        private static string Chu(object v) => v is string s ? s : v is JsonNumber n ? n.Raw : v is bool b ? (b ? "true" : "false") : "";

        private static double So(JsonObject o, string k)
        {
            object v = o.Get(k);
            string raw = v is JsonNumber n ? n.Raw : v as string;
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0;
        }

        private static object SafeParse(string json)
        {
            try { return JsonLite.Parse(json ?? "[]"); } catch (Exception) { return new List<object>(); }
        }
    }
}
