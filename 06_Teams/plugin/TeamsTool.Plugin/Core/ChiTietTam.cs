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
        /// <summary>Mô tả chi tiết hình tấm (AI viết lúc đọc) — vào hồ sơ md, phòng 3 dùng khi AI viết mẫu vẽ.</summary>
        public string MoTa { get; set; } = "";
        /// <summary>Đơn nửa repeat: tấm mới (phòng 3–4 chỉ chạy tấm được tích).</summary>
        public bool Moi { get; set; } = true;
        /// <summary>Tiết diện (đọc cùng lượt đọc bản vẽ): phang / be / lam_tay; "" = tấm đọc bằng bản cũ, chưa có tiết diện.</summary>
        public string KieuTd { get; set; } = "";
        /// <summary>Tiết diện X / Y dạng chữ PhongTrienKhai.ChuTd ("50 b90 100 b90 50") — người sửa thẳng trên bảng.</summary>
        public string TdX { get; set; } = "";
        public string TdY { get; set; } = "";
        public string GhiChuTd { get; set; } = "";
        /// <summary>Lý do người sửa (không bắt buộc) — gửi kèm khi AI rút bài học.</summary>
        public string LyDoSua { get; set; } = "";
        /// <summary>Mẫu biên dạng ve_dxf AI chọn (chua_co = chưa có mẫu) + mô tả khi chưa có.</summary>
        public string BienDang = "", MoTaBienDang = "";
        /// <summary>Thông số riêng của mẫu vẽ dạng chữ "r1=45; r2=38" (tên theo THAM_SO của mẫu) — AI đọc, người sửa ở ✎.</summary>
        public string ThamSo = "";
        public string CanhJson = "[]", LoJson = "[]";
        /// <summary>Dấu khắc vạch ケガキ / マーキング: [{x, y}] mm trên hình trải phẳng, gốc góc trái dưới.</summary>
        public string KeGakiJson = "[]";
        /// <summary>Vùng của tấm trên trang (tỉ lệ 0–1, gốc trái trên): {"x0","y0","x1","y1"} — cắt ảnh tấm; "" = chưa có.</summary>
        public string VungJson = "";
        /// <summary>Kích thước khách ghi trên bản vẽ còn đúng trên hình trải phẳng: [{loai, gia_tri, tu, den}] — bản vẽ phải dim đủ.</summary>
        public string DimKhachJson = "[]";
        public HashSet<string> KhongChac = new HashSet<string>(); // ô AI không chắc / AI tự chọn
        public HashSet<string> DaSua = new HashSet<string>();     // ô người dùng đã sửa — chạy lại không ghi đè
        /// <summary>Ô → giá trị AI đọc trước khi người sửa (để AI rút bài học).</summary>
        public Dictionary<string, string> GiaTriAi = new Dictionary<string, string>();

        public string TomTatCanh => KieuTd.Length > 0 ? DemUon() : TomTat(CanhJson, "chỗ bẻ"); // bản mới: chỗ bẻ nằm trong tiết diện

        private string DemUon()
        {
            int n = (PhongTrienKhai.DocChuTd(TdX, out _)?.Uon.Count ?? 0) + (PhongTrienKhai.DocChuTd(TdY, out _)?.Uon.Count ?? 0);
            return n == 0 ? "" : n + " chỗ bẻ";
        }
        public string TomTatLo => TomTat(LoJson, "lỗ");
        public string TomTatKeGaki => TomTat(KeGakiJson, "ケガキ");
        public string TomTatDimKhach => TomTat(DimKhachJson, "dim");
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
        /// <summary>Lúc bấm ✓ Xác nhận ở phòng Kiểm tra & Hỏi → sang Triển khai ("yyyy-MM-dd HH:mm"); "" = chưa xác nhận.</summary>
        public string XacNhan = "";
        /// <summary>Nghi vấn AI thấy khi đọc bản vẽ (Nguon = "doc") — phòng Kiểm tra gộp với luật.</summary>
        public List<VanDe> NghiVan = new List<VanDe>();
        /// <summary>Tệp bản vẽ → ghi chú chung cả đơn AI đọc được trên tệp đó (lời nhắn, yêu cầu chung) — ho_so\_don.md.</summary>
        public Dictionary<string, string> GhiChuDon = new Dictionary<string, string>();
        /// <summary>Bài học AI đề xuất từ chỗ người sửa (chờ Admin duyệt) + chữ ký các chỗ sửa đã gửi rút bài học.</summary>
        public List<DeXuatBaiHoc> BaiHoc = new List<DeXuatBaiHoc>();
        public HashSet<string> DaRutBaiHoc = new HashSet<string>();
    }

    /// <summary>Phòng 2 — Đọc bản vẽ: model + JSON + dựng yêu cầu Claude + đọc phản hồi (logic thuần).</summary>
    public static class PhongDocBanVe
    {
        /// <summary>Các ô chữ (tên JSON ↔ thuộc tính).</summary>
        public static readonly string[] O = { "so_ban_ve", "ten", "M", "Z", "T", "Q", "mat", "kich_thuoc", "dung_sai", "ghi_chu_jp", "ghi_chu_vi", "mo_ta" };
        /// <summary>Ô chữ của tiết diện (lưu chi_tiet.json; AI trả dạng cấu trúc x / y, đổi sang chữ khi đọc).</summary>
        public static readonly string[] OTd = { "kieu_td", "td_x", "td_y", "ghi_chu_td", "bien_dang", "mo_ta_bien_dang", "tham_so" };

        public static string Lay(TamAi t, string o)
        {
            switch (o)
            {
                case "kieu_td": return t.KieuTd; case "td_x": return t.TdX; case "td_y": return t.TdY; case "ghi_chu_td": return t.GhiChuTd;
                case "bien_dang": return t.BienDang; case "mo_ta_bien_dang": return t.MoTaBienDang; case "tham_so": return t.ThamSo;
                case "lo": return t.LoJson; case "ke_gaki": return t.KeGakiJson; // JSON — sửa ở khung ✎ (không gửi AI dạng ô chữ)
                case "so_ban_ve": return t.SoBanVe; case "ten": return t.Ten; case "M": return t.M; case "Z": return t.Z;
                case "T": return t.T; case "Q": return t.Q; case "mat": return t.Mat; case "kich_thuoc": return t.KichThuoc;
                case "dung_sai": return t.DungSai; case "ghi_chu_jp": return t.GhiChuJp; case "ghi_chu_vi": return t.GhiChuVi; case "mo_ta": return t.MoTa;
                default: return "";
            }
        }

        public static void Dat(TamAi t, string o, string v)
        {
            v = v ?? "";
            switch (o)
            {
                case "kieu_td": t.KieuTd = v; break; case "td_x": t.TdX = v; break; case "td_y": t.TdY = v; break; case "ghi_chu_td": t.GhiChuTd = v; break;
                case "bien_dang": t.BienDang = v; break; case "mo_ta_bien_dang": t.MoTaBienDang = v; break; case "tham_so": t.ThamSo = v; break;
                case "lo": t.LoJson = v.Length > 0 ? v : "[]"; break; case "ke_gaki": t.KeGakiJson = v.Length > 0 ? v : "[]"; break;
                case "so_ban_ve": t.SoBanVe = v; break; case "ten": t.Ten = v; break; case "M": t.M = v; break; case "Z": t.Z = v; break;
                case "T": t.T = v; break; case "Q": t.Q = v; break; case "mat": t.Mat = v; break; case "kich_thuoc": t.KichThuoc = v; break;
                case "dung_sai": t.DungSai = v; break; case "ghi_chu_jp": t.GhiChuJp = v; break; case "ghi_chu_vi": t.GhiChuVi = v; break; case "mo_ta": t.MoTa = v; break;
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
                    foreach (var kv in c.GiaTriAi) t.GiaTriAi[kv.Key] = kv.Value;
                    t.LyDoSua = c.LyDoSua;
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
            o.Set("xac_nhan", ct.XacNhan ?? "");
            var mb = new JsonObject();
            foreach (var kv in ct.MaBamDaDoc) mb.Set(kv.Key, kv.Value);
            o.Set("ma_bam_da_doc", mb);
            o.Set("tam", ct.Tam.Select(t =>
            {
                var x = new JsonObject();
                x.Set("tep", t.Tep); x.Set("trang", t.Trang);
                foreach (string k in O.Concat(OTd)) x.Set(k, Lay(t, k));
                x.Set("ly_do_sua", t.LyDoSua ?? "");
                x.Set("canh", SafeParse(t.CanhJson));
                x.Set("lo", SafeParse(t.LoJson));
                x.Set("ke_gaki", SafeParse(t.KeGakiJson));
                if (!string.IsNullOrEmpty(t.VungJson)) x.Set("vung", SafeParse(t.VungJson));
                x.Set("dim_khach", SafeParse(t.DimKhachJson));
                x.Set("moi", t.Moi);
                x.Set("khong_chac", t.KhongChac.OrderBy(s => s).Cast<object>().ToList());
                x.Set("da_sua", t.DaSua.OrderBy(s => s).Cast<object>().ToList());
                var ai = new JsonObject();
                foreach (var kv in t.GiaTriAi) ai.Set(kv.Key, kv.Value);
                x.Set("gia_tri_ai", ai);
                return (object)x;
            }).ToList());
            o.Set("nghi_van", ct.NghiVan.Select(v => (object)PhongKiemTra.GhiVanDe(v)).ToList());
            var gcd = new JsonObject();
            foreach (var kv in ct.GhiChuDon) gcd.Set(kv.Key, kv.Value);
            o.Set("ghi_chu_don", gcd);
            o.Set("bai_hoc", ct.BaiHoc.Select(b => (object)PhongBaiHoc.GhiDeXuat(b)).ToList());
            o.Set("da_rut_bai_hoc", ct.DaRutBaiHoc.OrderBy(s => s).Cast<object>().ToList());
            return JsonLite.Serialize(o);
        }

        public static ChiTiet Doc(string json)
        {
            JsonObject o;
            try { o = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { return null; }
            if (o == null) return null;
            var ct = new ChiTiet { TokenVao = (long)So(o, "token_vao"), TokenRa = (long)So(o, "token_ra"), Tien = (decimal)So(o, "tien"), XacNhan = Chu(o.Get("xac_nhan")) };
            if (o.Get("ma_bam_da_doc") is JsonObject mb)
                foreach (var kv in mb.Items) ct.MaBamDaDoc[kv.Key] = kv.Value as string ?? "";
            foreach (JsonObject x in (o.Get("tam") as List<object> ?? new List<object>()).OfType<JsonObject>())
            {
                TamAi t = DocTam(x);
                t.Tep = Chu(x.Get("tep"));
                t.Moi = !(x.Get("moi") is bool b) || b;
                t.LyDoSua = Chu(x.Get("ly_do_sua"));
                t.DaSua.UnionWith(DsChu(x.Get("da_sua")));
                if (x.Get("gia_tri_ai") is JsonObject ai)
                    foreach (var kv in ai.Items) t.GiaTriAi[kv.Key] = Chu(kv.Value);
                ct.Tam.Add(t);
            }
            if (o.Get("ghi_chu_don") is JsonObject g) foreach (var kv in g.Items) ct.GhiChuDon[kv.Key] = Chu(kv.Value);
            foreach (JsonObject x in (o.Get("nghi_van") as List<object> ?? new List<object>()).OfType<JsonObject>()) ct.NghiVan.Add(PhongKiemTra.DocVanDe(x));
            foreach (JsonObject x in (o.Get("bai_hoc") as List<object> ?? new List<object>()).OfType<JsonObject>()) ct.BaiHoc.Add(PhongBaiHoc.DocDeXuat(x));
            ct.DaRutBaiHoc.UnionWith(DsChu(o.Get("da_rut_bai_hoc")));
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
        public static string Body(string model, string loiDan, string quyChuan, string tenTep, string pdfBase64, string tomTatDxf, string them = null)
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
            noiDung.Add(Khoi("Tệp: " + tenTep + (string.IsNullOrEmpty(them) ? "" : "\n" + them) + (string.IsNullOrEmpty(tomTatDxf) ? "" : "\n\n" + tomTatDxf), null));
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
- lo: mỗi nhóm lỗ (cùng cỡ, cùng kiểu): d (Ø mm; lỗ 皿 = Ø lỗ thông), so_luong, taro (vd ""M6"" nếu là lỗ ren, rỗng nếu không), ghi_chu, cach_duong_be = khoảng cách nhỏ nhất (mm) từ MÉP lỗ tới đường bẻ gần nhất, tính từ kích thước ghi trên bản vẽ (-1 nếu tấm không bẻ hoặc không tính được).
  kieu: rỗng = lỗ tròn thường; ""sara D"" = lỗ 皿 với D = Ø miệng côn (vd ""sara 12""); ""dai RxL G"" = lỗ dài / oval / slot rộng R, dài tổng L, trục dài nghiêng G độ so với trục x (vd ""dai 10x30 0"").
  vi_tri: TÂM từng lỗ dạng [[x, y], …] (so_luong cặp) trên hình TRẢI PHẲNG, gốc góc trái dưới của tấm (như ケガキ), tính từ kích thước ghi trên bản vẽ; không tính được → rỗng và hỏi trong van_de.
- Chỗ bẻ / nhấn / cuộn (góc, R, đoạn hai bên) đọc vào td_x / td_y ở phần TIẾT DIỆN — không liệt kê riêng.
- mat: bề mặt đặc biệt (HL, #400, 縞板/sima, ミガキ…), rỗng nếu thường. dung_sai: các dung sai ghi trên bản vẽ.
- ghi_chu_jp: ghi chú tiếng Nhật CỦA RIÊNG tấm này, giữ nguyên chữ (lời nhắn / yêu cầu chung cả đơn đưa vào ghi_chu_don, không lặp ở đây); ghi_chu_vi: dịch sang tiếng Việt, ngắn gọn.
- trang: số trang PDF chứa tấm (1 = trang đầu); DXF để rỗng.
- khong_chac: tên các ô bạn đọc không chắc hoặc tự chọn (so_ban_ve, ten, M, Z, T, Q, mat, kich_thuoc, dung_sai, canh, lo, tiet_dien). Không bịa: không đọc được thì để rỗng và đưa vào khong_chac.
- Trang không phải bản vẽ chi tiết (đơn đặt hàng, email, bảng kê) → không tạo tấm cho trang đó, TRỪ dòng hàng là TẤM CẮT HÌNH ĐƠN GIẢN không kèm bản vẽ (vd レーザー品(角), 切板, シャーリング品, 角板, 丸板, hoặc kích thước dạng ""T × rộng × dài"" / ""2.50 X 84.7 X 804.0"") → TẠO một tấm cho mỗi dòng:
  M = mác vật liệu (SUS304, SS400…), T = số đầu (độ dày), kich_thuoc = ""rộng x dài"", Q = số lượng của dòng, mat = bề mặt / film (2B, HL, #400, 両面ファイバーガード…),
  kieu_td = phang, td_x = dài, td_y = rộng (丸 = tròn: td_x = td_y = Ø, ghi rõ hình tròn ở mo_ta), bien_dang = phang (tròn: chọn mẫu tròn nếu thư viện có, không thì chua_co),
  ghi_chu_jp = ghi chú của dòng (vd 両面めん取り, 目方向の矢印を記載), ghi_chu_vi = dịch + ""tấm theo dòng đơn hàng, không có bản vẽ"", mo_ta = hình + kích thước + yêu cầu gia công của dòng.
  Ghi chú chung của đơn (hạn giao, 請書要, ステンシル不可…) vẫn vào ghi_chu_don.

TIẾT DIỆN của mỗi tấm (để trải phẳng — KHÔNG tự tính trải phẳng, chương trình tính theo bảng TV):
- kieu_td: phang (không uốn) / be (bẻ, ấn, cuộn tính được) / lam_tay (pitchi, kakumaru, ensui, ống co, hình 3D phức tạp — td_x, td_y để rỗng).
- td_x, td_y: tiết diện theo 2 hướng vuông góc của tấm, viết dạng CHỮ: các đoạn (số mm) xen các chỗ uốn, vd ""50 b90 100 b90 50"". Chỗ uốn: b<góc> = bẻ dao V (b90r2 nếu ghi R trong), a<góc>r<R> = ấn 1 lần (R trong ghi trên bản vẽ là bội 5, < 50), c<cung>r<R> = cuộn (cung 360 = tròn kín, 180 = nửa), p<góc> = pitchi. Góc = góc TRONG giữa 2 đoạn (90 = vuông, 135 = mở, 60 = nhọn).
  · Bẻ / ấn: đoạn là kích thước NGOÀI (tới giao điểm cạnh ngoài) như ghi trên bản vẽ. Cuộn: đoạn kề là phần thẳng tiếp tuyến (0 nếu không có).
  · Hướng không uốn: một số = chiều dài phẳng theo hướng đó. Tấm phẳng: td_x = ""dài"", td_y = ""rộng"". Hộp 3–4 cạnh: td_x và td_y đều có chỗ uốn (thành – đáy – thành).
- ghi_chu_td: tiếng Việt, ngắn (hướng bẻ V / N, chỗ chưa chắc). Không đọc được tiết diện → để rỗng, ghi lý do, thêm tiet_dien vào khong_chac.
- ke_gaki: mỗi dấu khắc vạch (マーキング / ケガキ / 罫書き — dấu vạch, KHÔNG phải lỗ) một cặp [x, y] = toạ độ tâm (mm) trên hình TRẢI PHẲNG, gốc ở góc trái dưới của tấm, x theo hướng td_x; không có → rỗng. Bản vẽ chỉ ghi số dấu mà thiếu vị trí → để rỗng và hỏi trong van_de.
- tham_so: khi bien_dang là mẫu có ""thông số"" riêng (liệt kê trong Thư viện biên dạng): chữ dạng ""r1=45; r2=38"" — đúng tên của mẫu, số đọc từ bản vẽ (mm). Không ghi trên bản vẽ → bỏ (chương trình dùng mặc định); bắt buộc mà không đọc được → hỏi trong van_de. Mẫu không có thông số riêng → rỗng.
- vung: [x0, y0, x1, y1] khung bao TRỌN hình vẽ của tấm trên trang PDF (mọi hình chiếu + dim + ghi chú của tấm, không gồm khung tên / ghi chú chung), tỉ lệ 0–1 theo bề rộng / cao trang, gốc trái TRÊN (x0, y0 = góc trái trên; x1, y1 = góc phải dưới). DXF: theo khung bao bản vẽ.
- mo_ta: tiếng Việt, mô tả CHI TIẾT hình tấm đủ để người khác vẽ lại không cần xem bản vẽ: hình dạng tổng thể, từng cạnh / cung (R, lõm / lồi, vị trí), lỗ (Ø, số, vị trí), ケガキ, chỗ bẻ (hướng, góc), chi tiết đặc biệt. Chỉ điều có trên bản vẽ, không suy đoán.
- dim_khach: MỌI kích thước khách ghi trên bản vẽ của tấm mà còn đúng nguyên trên hình TRẢI PHẲNG (tấm phẳng: tất cả; tấm bẻ: kích thước nằm trong mặt phẳng tấm — song song đường bẻ, vị trí lỗ / ケガキ trên cùng một mặt; KHÔNG gồm kích thước ngoài qua chỗ bẻ). loai: ngang (theo x) / doc (theo y) / r (bán kính) / phi (đường kính lỗ) / goc (độ); gia_tri đúng số khách ghi; tu, den = toạ độ hai đầu trên hình trải phẳng (ngang: x, doc: y, gốc góc trái dưới; r: tu, den = tâm x, y; phi / goc: 0). Bản vẽ OHTA phải dim đủ các kích thước này.
- bien_dang: tên đúng MỘT mẫu trong ""Thư viện biên dạng"" (phần người dùng) khớp hình trải phẳng; làm tay → rỗng; không mẫu nào khớp → ""chua_co"". mo_ta_bien_dang: chỉ khi chua_co — mô tả hình trải phẳng (viền, góc cắt, đường bẻ, lỗ / rãnh đặc biệt) đủ để viết mẫu vẽ mới; còn lại rỗng.

GHI CHÚ CHUNG (ghi_chu_don, cho cả tệp): lời nhắn / yêu cầu chung của khách không thuộc riêng tấm nào (vd ""t=12mm切り板～2枚"", ""下図の手配をお願いします"", yêu cầu đóng gói, hạn) — giữ chữ gốc, mỗi ý một dòng kèm dịch tiếng Việt ngắn trong ngoặc; không có → rỗng.

NGHI VẤN (van_de, cho cả tệp): vấn đề có thật trên bản vẽ cần hỏi / xác nhận trước khi triển khai, theo QUY CHUẨN — không bịa:
- Thiếu kích thước cần để triển khai (bao, cạnh bẻ, vị trí lỗ, góc), kích thước cộng không khớp tổng.
- Mâu thuẫn giữa các hình chiếu, giữa hình và ghi chú / bảng, giữa các tấm (vật liệu, độ dày, số lượng).
- Bẻ hộp 3–4 cạnh (c > 150, c > a hoặc b), bẻ C khi a < c (bẻ W), cạnh chéo / bẻ một phần cần slit (B-6), vát (D-2), sima / mặt đẹp / HL (B-5).
- Luật công ty đặc biệt nếu đơn thuộc công ty đó; vật liệu / Z không đúng bảng C-1, C-2.
- KHÔNG nêu những gì chương trình tự kiểm: ô đã đưa vào khong_chac, cạnh ngắn hơn Lmin, lỗ gần đường bẻ, R ấn 1 lần / cuộn, taro, độ dày cần xác nhận.
Mỗi vấn đề: tam (số bản vẽ hoặc tên tấm, rỗng nếu chung), muc (do = chặn triển khai; vang = cần xác nhận; info = chỉ ghi chú), nhom (tu_xu_ly = làm theo QC không cần hỏi; noi_bo = hỏi quản lý; khach = hỏi khách), muc_qc (vd B-6-1), lien_lac (true nếu là báo slit / phần thừa — bản 連絡します; false nếu là câu hỏi お問い合わせ).
noi_dung: tiếng Việt, MỘT ý, tối đa ~20 từ, đúng trọng tâm: chỗ nào + thiếu / sai gì (+ số). Nhóm khach / noi_bo viết thành câu hỏi trực tiếp (vd ""Vị trí lỗ M5 cách mép trái bao nhiêu?""). Không giải thích lý do, không lặp ý. Không có vấn đề → van_de rỗng.";

        /// <summary>Phần chữ thêm sau tên tệp: công ty đặc biệt + thư viện biên dạng ve_dxf (để AI chọn bien_dang).</summary>
        public static string DauVao(string congTy, IEnumerable<(string Ten, string MoTa)> thuVien)
        {
            var sb = new System.Text.StringBuilder("Công ty đặc biệt: " + (string.IsNullOrWhiteSpace(congTy) ? "(không)" : congTy) + "\n\nThư viện biên dạng (bien_dang = tên):\n");
            var tv = (thuVien ?? Enumerable.Empty<(string, string)>()).ToList();
            if (tv.Count == 0) sb.Append("(trống — để bien_dang rỗng)\n");
            foreach (var (ten, moTa) in tv) sb.Append("- ").Append(ten).Append(": ").Append(moTa).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }

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
            // Giữ schema GỌN (12/11: API báo "compiled grammar is too large"): tiết diện / thông số mẫu là chữ, toạ độ là mảng số,
            // không dùng enum — giá trị lạ chương trình tự kiểm khi đọc (luật, Mot()).
            JsonObject cap = Mang(Mang(n));
            // chỗ bẻ (canh) không hỏi riêng — luật suy từ td_x / td_y; lỗ dài / 皿 gom vào ô chữ "kieu" (tách lại khi đọc)
            tt.Add(("lo", Mang(Obj(("d", n), ("so_luong", n), ("taro", s), ("kieu", s), ("ghi_chu", s), ("cach_duong_be", n), ("vi_tri", cap)))));
            tt.Add(("khong_chac", Mang(s)));
            foreach (string o in OTd) tt.Add((o, s)); // kieu_td, td_x, td_y, ghi_chu_td, bien_dang, mo_ta_bien_dang, tham_so
            tt.Add(("ke_gaki", cap));
            tt.Add(("vung", Mang(n)));
            tt.Add(("dim_khach", Mang(Obj(("loai", s), ("gia_tri", n), ("tu", n), ("den", n)))));
            return Obj(("tam", Mang(Obj(tt.ToArray()))), ("van_de", Mang(PhongKiemTra.SchemaVanDe())), ("ghi_chu_don", s));
        }

        /// <summary>Kết quả đọc phản hồi: tấm (Tep gán bên ngoài), nghi vấn, token, lỗi (null = được).</summary>
        public sealed class PhanHoi
        {
            public List<TamAi> Tam = new List<TamAi>();
            public List<VanDe> VanDe = new List<VanDe>();
            public string GhiChuDon = "";
            public long Vao, GhiCache, DocCache, Ra;
            public string Loi;
        }

        /// <param name="tenTep">Tên tệp đang đọc — làm mã cho nghi vấn (giữ trạng thái khi đọc lại).</param>
        public static PhanHoi DocPhanHoi(string json, string tenTep = "")
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
            if (nd.Get("van_de") is List<object> vd) kq.VanDe = PhongKiemTra.DocDsVanDe(vd, tenTep, "doc");
            kq.GhiChuDon = (nd.GetString("ghi_chu_don") ?? "").Trim();
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
            foreach (string o in O.Concat(OTd)) Dat(t, o, Chu(x.Get(o)));
            // AI trả tiết diện dạng cấu trúc x / y → chữ; chi_tiet.json đã lưu dạng chữ td_x / td_y
            if (x.Get("x") is JsonObject tx) t.TdX = PhongTrienKhai.ChuTd(PhongTrienKhai.DocTd(tx));
            if (x.Get("y") is JsonObject ty) t.TdY = PhongTrienKhai.ChuTd(PhongTrienKhai.DocTd(ty));
            t.CanhJson = JsonLite.Serialize(x.Get("canh") as List<object> ?? new List<object>());
            // AI trả toạ độ dạng cặp [x, y] (schema gọn) → lưu dạng {x, y} như trước; dạng {x, y} cũ giữ nguyên
            var lo = x.Get("lo") as List<object> ?? new List<object>();
            foreach (JsonObject l in lo.OfType<JsonObject>())
            {
                if (l.Get("vi_tri") is List<object> vt) l.Set("vi_tri", Diem(vt));
                TachKieuLo(l);
            }
            t.LoJson = JsonLite.Serialize(lo);
            t.KeGakiJson = JsonLite.Serialize(Diem(x.Get("ke_gaki") as List<object> ?? new List<object>()));
            if (x.Get("vung") is List<object> vl && vl.Count == 4)
            {
                var vg4 = new JsonObject();
                string[] k = { "x0", "y0", "x1", "y1" };
                for (int i = 0; i < 4; i++) vg4.Set(k[i], vl[i]);
                t.VungJson = JsonLite.Serialize(vg4);
            }
            else t.VungJson = x.Get("vung") is JsonObject vg ? JsonLite.Serialize(vg) : "";
            if (x.Get("tham_so") is List<object> tsAi) // AI trả mảng {ten, gia_tri} → chữ "r1=45; r2=38"
                t.ThamSo = string.Join("; ", tsAi.OfType<JsonObject>().Where(o => (o.GetString("ten") ?? "").Trim().Length > 0).Select(o => o.GetString("ten").Trim() + "=" + Chu(o.Get("gia_tri"))));
            t.DimKhachJson = JsonLite.Serialize(x.Get("dim_khach") as List<object> ?? new List<object>());
            if (t.KieuTd.Length > 0 && t.KieuTd != "phang" && t.KieuTd != "be" && t.KieuTd != "lam_tay") // schema không còn enum
            {
                t.KieuTd = (t.TdX + t.TdY).Any(char.IsLetter) ? "be" : "phang";
                t.KhongChac.Add("tiet_dien");
            }
            t.KhongChac.UnionWith(DsChu(x.Get("khong_chac")));
            return t;
        }

        /// <summary>Ô chữ "kieu" của lỗ (schema gọn) → các trường ve.py / luật dùng: "sara 12" → sara, d_sara; "dai 10x30 0" → loai dai, rong, dai, goc.</summary>
        public static void TachKieuLo(JsonObject l)
        {
            string k = (l.GetString("kieu") ?? "").Trim().ToLowerInvariant();
            if (k.Length == 0) return;
            var so = System.Text.RegularExpressions.Regex.Matches(k.Replace(',', '.'), @"\d+(?:\.\d+)?").Cast<System.Text.RegularExpressions.Match>()
                          .Select(m => m.Value).ToList();
            if (k.StartsWith("sara", StringComparison.Ordinal) || k.StartsWith("皿", StringComparison.Ordinal))
            {
                l.Set("sara", true);
                if (so.Count > 0) l.Set("d_sara", new JsonNumber(so[0]));
            }
            else if (k.StartsWith("dai", StringComparison.Ordinal) && so.Count >= 2)
            {
                l.Set("loai", "dai");
                l.Set("rong", new JsonNumber(so[0]));
                l.Set("dai", new JsonNumber(so[1]));
                l.Set("goc", new JsonNumber(so.Count > 2 ? so[2] : "0"));
            }
        }

        /// <summary>[[x, y], …] → [{x, y}, …]; phần tử đã là {x, y} giữ nguyên, phần tử hỏng bỏ.</summary>
        private static List<object> Diem(List<object> ds)
        {
            var kq = new List<object>();
            foreach (object p in ds)
            {
                if (p is JsonObject o) { kq.Add(o); continue; }
                if (!(p is List<object> c) || c.Count < 2) continue;
                var d = new JsonObject();
                d.Set("x", c[0]); d.Set("y", c[1]);
                kq.Add(d);
            }
            return kq;
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
