using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>
    /// BƯỚC 2 "Đọc kỹ &amp; chọn mẫu" (13/11 — tách khỏi bước 1: bước 1 chỉ đọc cả trang + cắt ảnh, chưa chọn mẫu).
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
- M, T, Q, kich_thuoc (""rộng x dài""), kieu_td (phang / be / lam_tay), td_x, td_y (LUÔN điền — tấm phẳng: td_x = chiều theo x / ngang, td_y = chiều theo y / dọc, vd ""635"" và ""460""; tiết diện dạng chữ: số = đoạn mm xen chỗ uốn b<góc> / a<góc>r<R> / c<cung>r<R> / p<góc>), mo_ta (mô tả chi tiết hình đủ để vẽ lại).
- CHỌN MẪU VẼ: bien_dang = tên đúng MỘT mẫu trong ""Thư viện biên dạng"" (cuối phần người dùng, [nhóm] tam_don / tam_be / tam_cuon…) khớp hình TRẢI PHẲNG; làm tay (lam_tay) → rỗng; không mẫu nào khớp → ""chua_co"" và mo_ta_bien_dang = mô tả hình trải phẳng (viền, góc cắt, đường bẻ, lỗ / rãnh đặc biệt) đủ để viết mẫu vẽ mới (còn lại rỗng). tham_so: thông số riêng của mẫu đã chọn dạng ""r1=45; r2=38"" — đúng tên thông số của mẫu, số đọc từ ảnh; mẫu không có thông số riêng → rỗng; bắt buộc mà không đọc được → hỏi trong van_de. bien_dang người dùng đã sửa → giữ nguyên.
- lo: mỗi nhóm lỗ {d, so_luong, taro, kieu ("""" tròn / ""sara D"" / ""dai RxL G"" / ""khoa D d k G nho"" = lỗ khoá / 変形孔 (hình ở chi tiết phóng A, B…): Ø lớn D + Ø nhỏ d, k = khoảng cách HAI TÂM; vi_tri = TÂM MÀ KHÁCH DIM TỚI (chuỗi dim trên hình chính), chữ cuối nho / lon = tâm đó là của Ø nhỏ hay Ø lớn; G = hướng từ tâm đó sang tâm kia (0 phải, 90 lên, 180 trái, 270 xuống) — MỌI lỗ cùng hướng như hình chi tiết. Vd chi tiết A ghi mép tấm → tâm Ø7 = 10, tâm Ø7 → tâm Ø12 = 6, Ø12 bên phải → ""khoa 12 7 6 0 nho"", vi_tri x = 10.), ghi_chu, cach_duong_be, vi_tri [[x, y], …] = TÂM từng lỗ trên hình TRẢI PHẲNG, gốc góc trái dưới của tấm, tính từ kích thước ghi trên bản vẽ (vd ""100P×5=500"" = 6 lỗ cách đều 100), noi_tam: true nếu bản vẽ khách VẼ đường tâm (nét chấm gạch) nối các lỗ của nhóm (vd khung chữ nhật qua tâm các lỗ chu vi), không vẽ → false.}.
- goc: vát / bo góc viền ngoài ""<đỉnh> C10; <đỉnh> R5"" (đỉnh = trai_duoi / phai_duoi / phai_tren / trai_tren hoặc x,y). XEM TỪNG GÓC trên hình vẽ: khách thường chỉ ghi nhãn (C10) ở MỘT góc — góc khác vẽ vát / bo giống hệt cũng ghi vào goc cùng cỡ. khoet: khoét trong (không phải lỗ) ""□rộngxcao R<r> G<góc> @ x,y; x,y"" hoặc ""[x,y x,y …] @ x,y"", cách nhau |.
- dinh: CHỈ khi bien_dang = da_giac (tấm phẳng không phải chữ nhật), còn lại []. Đỉnh viền NGOÀI theo thứ tự đi một vòng, mỗi đỉnh [x, y] mm trên hình trải phẳng, gốc (0, 0) = góc trái dưới KHUNG BAO (x nhỏ nhất = 0, y nhỏ nhất = 0, như vi_tri lỗ), tính từ dim khách; không lặp đỉnh đầu. Cạnh từ đỉnh này tới đỉnh kế là CUNG → [x, y, R] (R > 0 cung lồi ra ngoài, R < 0 lõm vào trong; R tính từ dim khách). Vát C / bo R nhỏ ở góc vẫn ghi ở goc (đỉnh là góc nhọn trước khi vát), lỗ ở lo, khoét trong ở khoet — không đưa vào dinh. Đỉnh phải tính qua dim xiên / góc (không có dim ngang / dọc trực tiếp) → thêm dinh vào khong_chac.
- ke_gaki: [[x, y], …] tâm từng dấu khắc vạch. dim_khach: MỌI kích thước khách ghi cho tấm {loai ngang / doc / r / phi / goc / xien, gia_tri, tu, den, diem} — xien (cạnh xiên — đo chiều dài thật) / goc (độ): tu = den = tai = 0 và diem = các điểm THẬT trên hình trải phẳng mà chân dim khách chạm: xien [[x1, y1], [x2, y2]] hai đầu cạnh; goc [[điểm trên cạnh 1], [đỉnh góc], [điểm trên cạnh 2]]; loại khác diem = []; — tu, den = toạ độ THẬT hai đầu (dim từ đường tâm thì tu = toạ độ tâm), giữ đúng cách khách chia chuỗi (bản vẽ OHTA chép lại từng chuỗi); không gồm dim của hình chi tiết phóng (đã ở kieu lỗ). tai = toạ độ ĐƯỜNG DIM khách đặt trên hình trải phẳng (ngang: y của đường dim, doc: x của đường dim; dim nằm trong hình thì toạ độ trong tấm, ngoài thì ngoài — vd y > chiều cao tấm khi dim ở trên) để bản vẽ OHTA đặt dim đúng chỗ khách đặt; r / phi / goc: 0.
- ĐỐI CHIẾU ghi chú (vd ""140*600"", ""PL9"", ""数量2個"") với dim trên hình: khớp → dùng; KHÔNG khớp → để kich_thuoc theo ghi chú, thêm kich_thuoc vào khong_chac và tạo van_de muc do nhom khach nêu cả hai số.
- chua_chac: MỌI giá trị bạn TỰ SUY vì khách KHÔNG ghi số (vd góc khoét / góc tấm thấy bo tròn trên hình mà không có R, lỗ không có dim vị trí, đỉnh lấy theo tỉ lệ hình): vẫn ghi giá trị hợp lý nhất vào ô tương ứng để vẽ được, nhưng thêm một mục {o: tên ô (lo / khoet / goc / dinh / ke_gaki / kich_thuoc…; thêm "".r"" nếu chỉ phần bo R chưa chắc, vd ""khoet.r""), so: thứ tự mục trong ô (1, 2…; 0 = cả ô), noi_dung: tiếng Việt ngắn nêu giá trị đã lấy + vì sao (vd ""R10 bo 4 góc khoét — khách không ghi R, lấy theo hình"")}; đồng thời đưa ô đó vào khong_chac và thêm một van_de muc vang nhom khach hỏi khách xác nhận. Phần vẽ từ các mục này được tô VÀNG chờ người xác nhận. Không có → [].
- khong_chac: các ô vẫn chưa chắc sau khi xem ảnh. van_de: vấn đề MỚI thấy trên ảnh (cùng cách viết: tam, muc do / vang / info, nhom tu_xu_ly / noi_bo / khach, muc_qc, noi_dung một ý ~20 từ, lien_lac).
- bo_nghi_van: SỐ THỨ TỰ các ""Nghi vấn đã có"" mà ảnh phóng to đã trả lời được (vd đã đọc được vị trí lỗ) — để bỏ.
Không bịa: không đọc được thì giữ đầu vào và đưa vào khong_chac.";

        public static JsonObject Schema()
        {
            JsonObject s = Kieu("string"), n = Kieu("number"), cap = Mang(Mang(n));
            return Obj(("M", s), ("T", s), ("Q", s), ("kich_thuoc", s), ("kieu_td", s), ("td_x", s), ("td_y", s), ("bien_dang", s), ("mo_ta_bien_dang", s), ("tham_so", s), ("goc", s), ("khoet", s), ("mo_ta", s),
                       ("lo", Mang(Obj(("d", n), ("so_luong", n), ("taro", s), ("kieu", s), ("ghi_chu", s), ("cach_duong_be", n), ("vi_tri", cap), ("noi_tam", Kieu("boolean"))))),
                       ("ke_gaki", cap), ("dim_khach", Mang(Obj(("loai", s), ("gia_tri", n), ("tu", n), ("den", n), ("tai", n), ("diem", cap)))), ("dinh", cap), ("chua_chac", Mang(Obj(("o", s), ("so", n), ("noi_dung", s)))),
                       ("khong_chac", Mang(s)), ("van_de", Mang(PhongKiemTra.SchemaVanDe())), ("bo_nghi_van", Mang(n)));
        }

        /// <summary>Mục chua_chac còn hiệu lực: bỏ mục của ô người dùng đã sửa (✎ — coi là đã xác nhận). Gửi viec.json để ve.py tô vàng.</summary>
        public static List<object> ChuaChacConLai(TamAi t)
        {
            List<object> ds;
            try { ds = JsonLite.Parse(t.ChuaChacJson ?? "[]") as List<object> ?? new List<object>(); } catch (Exception) { ds = new List<object>(); }
            return ds.OfType<JsonObject>().Where(m => !t.DaSua.Contains(((m.GetString("o") ?? "").Split('.')[0]).Trim())).Cast<object>().ToList();
        }

        /// <summary>Câu "cần xác nhận" của các mục chua_chac còn hiệu lực (hiện ở tab DXF ảnh / hồ sơ).</summary>
        public static List<string> CanXacNhan(TamAi t) =>
            ChuaChacConLai(t).OfType<JsonObject>().Select(m => (m.GetString("noi_dung") ?? "").Trim()).Where(s => s.Length > 0).ToList();

        /// <summary>Tấm còn phải qua bước 2: chưa đọc kỹ và mẫu chưa có / người tự chọn (đơn đọc kiểu cũ đã có mẫu AI chọn → coi như xong).</summary>
        public static bool CanBuoc2(TamAi t) => t.Moi && !t.DaPhongTo && (t.BienDang.Length == 0 || t.DaSua.Contains("bien_dang"));

        /// <summary>Phần chữ: tấm (tên để AI ghi vào van_de.tam), dữ liệu lượt đọc cả trang, nghi vấn đã có (đánh số), thư viện mẫu để chọn.</summary>
        public static string DauVao(TamAi t, IEnumerable<(string Ten, string MoTa)> thuVien, IList<VanDe> nghiVan)
        {
            var sb = new StringBuilder();
            sb.Append("Tấm: ").Append(PhongKiemTra.TenTam(t)).Append(" (tệp ").Append(t.Tep).Append(", trang ").Append(t.Trang).Append(")\n");
            sb.Append("Dữ liệu lượt đọc cả trang:\n");
            foreach (var (o, v) in new[] { ("M", t.M), ("T", t.T), ("Q", t.Q), ("kich_thuoc", t.KichThuoc), ("mat", t.Mat), ("kieu_td", t.KieuTd), ("td_x", t.TdX), ("td_y", t.TdY),
                                           ("bien_dang", t.BienDang), ("tham_so", t.ThamSo), ("goc", t.Goc), ("khoet", t.Khoet), ("mo_ta", t.MoTa),
                                           ("ghi_chu_jp", t.GhiChuJp), ("lo", t.LoJson), ("ke_gaki", t.KeGakiJson), ("dim_khach", t.DimKhachJson), ("dinh", t.DinhJson) })
                sb.Append("- ").Append(o).Append(": ").Append(string.IsNullOrWhiteSpace(v) ? "(trống)" : v.Replace("\r", "").Replace("\n", " ")).Append('\n');
            if (t.DaSua.Count > 0) sb.Append("(Người dùng đã sửa — coi là đúng: ").Append(string.Join(", ", t.DaSua.OrderBy(x => x))).Append(")\n");
            sb.Append("\nNghi vấn đã có:\n");
            if (nghiVan.Count == 0) sb.Append("(không có)\n");
            for (int i = 0; i < nghiVan.Count; i++) sb.Append(i + 1).Append(". ").Append(nghiVan[i].NoiDung).Append('\n');
            sb.Append("\nThư viện biên dạng (bien_dang = tên):\n");
            var tv = (thuVien ?? Enumerable.Empty<(string, string)>()).ToList();
            if (tv.Count == 0) sb.Append("(trống — máy chưa có Python / ezdxf: để bien_dang rỗng)\n");
            foreach (var (ten, moTa) in tv) sb.Append("- ").Append(ten).Append(": ").Append(moTa).Append('\n');
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
            foreach (string o in new[] { "M", "T", "Q", "kich_thuoc", "kieu_td", "td_x", "td_y", "bien_dang", "mo_ta_bien_dang", "tham_so", "goc", "khoet", "mo_ta" })
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
            if (!t.DaSua.Contains("dinh") && nd.Get("dinh") is List<object> dn && dn.Count > 0)
            {
                string j = JsonLite.Serialize(dn);
                if (j != t.DinhJson) { t.DinhJson = j; doi.Add("dinh"); }
            }
            if (nd.Get("chua_chac") is List<object> cc) // [] cũng ghi: lượt đọc mới thấy không còn gì tự suy
            {
                string j = JsonLite.Serialize(cc);
                if (j != t.ChuaChacJson) { t.ChuaChacJson = j; doi.Add("chua_chac"); }
            }
            if (nd.Get("dim_khach") is List<object> dk && dk.Count > 0)
            {
                string j = JsonLite.Serialize(dk);
                if (j != t.DimKhachJson) { t.DimKhachJson = j; doi.Add("dim_khach"); }
            }
            if (nd.Get("khong_chac") is List<object> kc)
            {
                // chỉ giữ tên ô (AI hay ghi cả câu / "lo.d"): "lo.vi_tri y…" → lo; câu không ra tên ô thì bỏ — giải thích đã ở van_de
                var hopLe = new HashSet<string>(PhongDocBanVe.O.Concat(PhongDocBanVe.OTd).Concat(new[] { "lo", "ke_gaki", "dim_khach", "dinh", "tiet_dien", "canh" }));
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
