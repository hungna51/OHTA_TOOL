using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một chỗ uốn trên tiết diện. goc = góc trong như trên bản vẽ (90 = vuông); cung = góc cung khi cuộn (360 = tròn kín).</summary>
    public sealed class Uon
    {
        public string Kieu = "be"; // be | an_1_lan | cuon | pitchi
        public double Goc = 90, R, Cung;
    }

    /// <summary>
    /// Tiết diện theo một hướng: các đoạn thẳng nối nhau bằng chỗ uốn (Doan.Count = Uon.Count + 1).
    /// be / an_1_lan: đoạn = kích thước NGOÀI tới góc ảo; cuon: đoạn = phần thẳng tiếp tuyến (0 nếu không có).
    /// Không uốn: một đoạn = chiều dài phẳng.
    /// </summary>
    public sealed class TietDien
    {
        public List<double> Doan = new List<double>();
        public List<Uon> Uon = new List<Uon>();
    }

    /// <summary>Một ứng viên V của bảng TV cho một góc (từ TraVCalculator.GetBendOptionsForFname).</summary>
    public sealed class UngVienV
    {
        public string V = "", Chu = "", CanhBao = "", Nguon = "";
        public double Delta;
    }

    /// <summary>Phòng 4: dữ liệu một tấm (AI đọc tiết diện + V người chọn) và kết quả tính.</summary>
    public sealed class TamTrienKhai
    {
        public string Khoa = "", Ten = "", Kieu = "phang", GhiChu = "", VChon = "";
        public TietDien X = new TietDien(), Y = new TietDien();
        // Kết quả (tính lại mỗi lần vẽ, không lưu)
        public double? XPhang, YPhang;
        public bool LamTay;
        public List<string> ChiTiet = new List<string>(), CanhBao = new List<string>();
        public List<string> CacV = new List<string>();
        public string KyHieu = "";
    }

    /// <summary>Kết quả phòng 4, lưu &lt;thư mục đơn&gt;\ai\trien_khai.json (+ trien_khai.csv).</summary>
    public sealed class KetQuaTrienKhai
    {
        public List<TamTrienKhai> Tam = new List<TamTrienKhai>();
        /// <summary>Lúc xác nhận phòng 3 mà lần triển khai này dùng — khác = phòng 3 đã đổi, chạy lại.</summary>
        public string TuXacNhan = "", Luc = "";
        public long TokenVao, TokenRa;
        public decimal Tien;
    }

    /// <summary>
    /// Phòng 4 — Triển khai (spec 2026-11-11): trải phẳng theo QC B-2 / B-2-4 / C-2 và bảng TV (δ do TraV tính như tab Fname).
    /// AI chỉ đọc tiết diện; mọi con số tính ở đây (thuần, có test).
    /// </summary>
    public static class PhongTrienKhai
    {
        /// <summary>Đọc "V=32-4(90°)" / "シマ目外 V32引代 4 (90°)" / "V12x-1.9(60°)" → δ. Không đọc được → null.</summary>
        public static double? DeltaTuChu(string chu)
        {
            var m = System.Text.RegularExpressions.Regex.Match(chu ?? "", @"(?:-|引代\s*)(\d+(?:\.\d+)?)\s*\(");
            return m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : (double?)null;
        }

        /// <summary>
        /// Trải phẳng một tấm. traV(góc) = các ứng viên V của M, T ở góc đó (thứ tự TraV). tThuc = T thực (ấn 1 lần, cuộn).
        /// lmin(V) = Lmin của V (null = không biết) — để bỏ V có Lmin dài hơn cạnh ngắn nhất khi tự chọn.
        /// </summary>
        public static void Tinh(TamTrienKhai tk, TamAi tam, Func<double, List<UngVienV>> traV, double tThuc, Func<string, double?> lmin = null)
        {
            tk.ChiTiet.Clear(); tk.CanhBao.Clear(); tk.CacV.Clear();
            tk.XPhang = tk.YPhang = null;
            tk.LamTay = tk.Kieu == "lam_tay";
            double T = PhongKiemTra.So(tam.T) ?? 0;

            var tatCaUon = tk.X.Uon.Concat(tk.Y.Uon).ToList();
            if (tatCaUon.Any(u => u.Kieu == "pitchi")) tk.LamTay = true;
            if (tk.LamTay)
            {
                tk.ChiTiet.Add("Làm tay (pitchi / kakumaru / ống co / hình phức tạp) — triển khai trên SolidWorks theo QC B-3, B-4, C-3.");
                tk.KyHieu = KyHieu(tam, tatCaUon);
                return;
            }

            // V của tấm: người đã chọn, không thì ứng viên đầu ở 90° có Lmin ≤ đoạn ngắn nhất cạnh chỗ bẻ
            var be = tatCaUon.Where(u => u.Kieu == "be").ToList();
            if (be.Count > 0)
            {
                List<UngVienV> uv90 = traV(be[0].Goc);
                tk.CacV.AddRange(uv90.Select(u => u.V).Distinct());
                if (tk.VChon.Length == 0 || !tk.CacV.Contains(tk.VChon))
                {
                    double ngan = DoanCanhBe(tk.X).Concat(DoanCanhBe(tk.Y)).DefaultIfEmpty(double.MaxValue).Min();
                    UngVienV hop = uv90.FirstOrDefault(u => !(lmin?.Invoke(u.V) is double l) || l <= ngan) ?? uv90.FirstOrDefault();
                    tk.VChon = hop?.V ?? "";
                }
            }

            tk.XPhang = Trai(tk.X, "X", tk, traV, T, tThuc);
            tk.YPhang = Trai(tk.Y, "Y", tk, traV, T, tThuc);
            if (tk.X.Doan.Count == 0) tk.CanhBao.Add("Chưa có tiết diện hướng X.");
            if (tk.Y.Doan.Count == 0) tk.CanhBao.Add("Chưa có tiết diện hướng Y.");
            tk.KyHieu = KyHieu(tam, tatCaUon);
        }

        /// <summary>Các đoạn kề một chỗ bẻ thường (để so Lmin).</summary>
        private static IEnumerable<double> DoanCanhBe(TietDien td)
        {
            for (int i = 0; i < td.Uon.Count && i + 1 < td.Doan.Count; i++)
                if (td.Uon[i].Kieu == "be") { yield return td.Doan[i]; yield return td.Doan[i + 1]; }
        }

        /// <summary>Chiều dài trải của một tiết diện; ghi từng chỗ uốn vào ChiTiet. Không tính được → null.</summary>
        private static double? Trai(TietDien td, string huong, TamTrienKhai tk, Func<double, List<UngVienV>> traV, double T, double tThuc)
        {
            if (td.Doan.Count == 0) return null;
            if (td.Doan.Count != td.Uon.Count + 1) { tk.CanhBao.Add("Tiết diện " + huong + ": số đoạn phải = số chỗ uốn + 1."); return null; }
            double tong = td.Doan.Sum();
            var dong = new StringBuilder(huong + ": " + string.Join(" + ", td.Doan.Select(F)));
            for (int i = 0; i < td.Uon.Count; i++)
            {
                Uon u = td.Uon[i];
                switch (u.Kieu)
                {
                    case "be":
                    {
                        List<UngVienV> uv = traV(u.Goc);
                        UngVienV chon = uv.FirstOrDefault(x => x.V == tk.VChon) ?? uv.FirstOrDefault();
                        if (chon == null) { tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": bảng TV không có góc " + F(u.Goc) + "° — tra tay / bẻ thử."); return null; }
                        tong -= 2 * chon.Delta;
                        dong.Append(" | " + F(u.Goc) + "° " + chon.V + " δ" + F(chon.Delta) + " → −" + F(2 * chon.Delta) + (chon.Nguon.Length > 0 ? " (" + chon.Nguon + ")" : ""));
                        if (chon.CanhBao.Length > 0) tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": " + chon.CanhBao);
                        if (chon.V != tk.VChon && tk.VChon.Length > 0) tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": góc " + F(u.Goc) + "° không có " + tk.VChon + ", dùng " + chon.V + ".");
                        break;
                    }
                    case "an_1_lan":
                    {
                        // QC B-2-4: đường trung bình O = 0.45 × T thực; bớt 2 đoạn lùi (R + T)·tan(θ/2), cộng cung (R + O)·θ
                        double theta = 180 - u.Goc, rad = theta * Math.PI / 180, lui = (u.R + T) * Math.Tan(rad / 2), cung = (u.R + 0.45 * tThuc) * rad;
                        tong += cung - 2 * lui;
                        dong.Append(" | ấn 1 lần R" + F(u.R) + " " + F(u.Goc) + "°: −2×" + F(lui) + " + cung " + F(cung));
                        break;
                    }
                    case "cuon":
                    {
                        // QC C-2: đường trung bình 0.5 × T thực; đoạn kề là phần thẳng nên không bớt
                        double cung = (u.R + 0.5 * tThuc) * u.Cung * Math.PI / 180;
                        if (u.Cung <= 0) { tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": cuộn chưa có góc cung."); return null; }
                        tong += cung;
                        dong.Append(" | cuộn R" + F(u.R) + " cung " + F(u.Cung) + "°: + " + F(cung));
                        break;
                    }
                    default:
                        tk.CanhBao.Add(huong + " chỗ " + (i + 1) + ": kiểu uốn '" + u.Kieu + "' chưa tính được.");
                        return null;
                }
            }
            double kq = Math.Round(tong, 1);
            tk.ChiTiet.Add(dong + " ⇒ " + F(kq));
            return kq;
        }

        /// <summary>Ký hiệu 曲げ (QC I.1.E): r cuộn, m nhấn / bẻ, t taro, s sara; nhiều loại → *.</summary>
        public static string KyHieu(TamAi tam, IEnumerable<Uon> uon)
        {
            var k = new List<char>();
            var ds = uon.ToList();
            if (ds.Any(u => u.Kieu == "cuon")) k.Add('r');
            if (ds.Any(u => u.Kieu != "cuon")) k.Add('m');
            List<object> lo;
            try { lo = JsonLite.Parse(tam.LoJson ?? "[]") as List<object> ?? new List<object>(); } catch (Exception) { lo = new List<object>(); }
            if (lo.OfType<JsonObject>().Any(l => (l.GetString("taro") ?? "").Trim().Length > 0)) k.Add('t');
            if (lo.OfType<JsonObject>().Any(l => l.Get("sara") is bool b && b)) k.Add('s');
            return k.Count == 0 ? "" : k.Count == 1 ? k[0].ToString() : "*";
        }

        // ---------- CSV + tóm tắt ----------

        public const string TieuDeCsv = "エクスポート用,数量,材質,板厚,曲げ,X,Y";

        /// <summary>
        /// Dòng CSV theo QC I.1.E. Bango tạm = &lt;title&gt;-&lt;số thứ tự&gt; (GRO đánh bango thật trên CAD).
        /// shortcut: chưa có cột 単価 (giá) — thêm khi có bảng giá.
        /// </summary>
        public static string DongCsv(string title, int stt, TamAi tam, TamTrienKhai tk) =>
            string.Join(",", new[]
            {
                title + "-" + stt, Csv(tam.Q), Csv(tam.M), Csv(tam.T), tk.KyHieu,
                tk.XPhang.HasValue ? F(tk.XPhang.Value) : "", tk.YPhang.HasValue ? F(tk.YPhang.Value) : "",
            });

        private static string Csv(string s) => (s ?? "").Replace(",", " ").Replace("\n", " ").Trim();

        /// <summary>Bảng tóm tắt gửi lên chuỗi đơn để người triển khai / check kiểm.</summary>
        public static string TomTat(string title, IList<(TamAi Tam, TamTrienKhai Tk)> ds)
        {
            var sb = new StringBuilder("【Triển khai AI — cần kiểm】" + title + "\n");
            for (int i = 0; i < ds.Count; i++)
            {
                var (tam, tk) = ds[i];
                sb.Append(i + 1).Append(". ").Append(PhongKiemTra.TenTam(tam)).Append(" — ").Append(tam.M).Append(" t").Append(tam.T).Append(" ×").Append(tam.Q);
                if (tk.LamTay) sb.Append(" — LÀM TAY");
                else sb.Append(" — ").Append(tk.XPhang.HasValue ? F(tk.XPhang.Value) : "?").Append(" × ").Append(tk.YPhang.HasValue ? F(tk.YPhang.Value) : "?");
                if (tk.VChon.Length > 0) sb.Append(" — ").Append(tk.VChon);
                if (tk.KyHieu.Length > 0) sb.Append(" [").Append(tk.KyHieu).Append(']');
                sb.Append('\n');
                foreach (string c in tk.ChiTiet) sb.Append("   ").Append(c).Append('\n');
                foreach (string c in tk.CanhBao) sb.Append("   ⚠ ").Append(c).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        // ---------- AI đọc tiết diện ----------

        public const int MaxTokens = 16000;

        public const string LoiDan =
@"Bạn là kỹ sư triển khai kim loại tấm của OHTA (phòng Triển khai). Đầu vào: bản vẽ khách (PDF hoặc tóm tắt DXF), bảng tấm đã kiểm (phòng Kiểm tra) và các câu trả lời đã có.
Với MỖI tấm trong bảng, đọc TIẾT DIỆN để trải phẳng (không tự tính trải phẳng — chương trình tính theo bảng TV):
- so_ban_ve, ten: đúng như bảng tấm (để ghép).
- kieu: phang (không uốn) / be (bẻ, ấn, cuộn tính được) / lam_tay (pitchi, kakumaru, ensui, ống co, hình 3D phức tạp — không đọc tiết diện).
- x, y: tiết diện theo 2 hướng vuông góc của tấm. doan = các đoạn theo thứ tự dọc tiết diện; uon = các chỗ uốn giữa 2 đoạn liền nhau (số uon = số doan − 1).
  · Bẻ / ấn: doan là kích thước NGOÀI (tới giao điểm cạnh ngoài), như kích thước ghi trên bản vẽ; uốn: kieu be (R nhỏ, bẻ dao V) hoặc an_1_lan (R trong ghi trên bản vẽ là bội 5, < 50); goc = góc trong giữa 2 đoạn (90 = vuông, 135 = mở, 60 = nhọn); r_trong = R trong (0 nếu không ghi); cung = 0.
  · Cuộn: uốn kieu cuon, r_trong = R trong, cung = góc cung (360 = tròn kín, 180 = nửa); doan kề là phần thẳng tiếp tuyến (0 nếu không có).
  · Hướng không uốn: một doan = chiều dài phẳng theo hướng đó, uon rỗng. Tấm phẳng: x = [dài], y = [rộng].
  · Hộp 3–4 cạnh: x và y đều có tiết diện (thành – đáy – thành).
- ghi_chu: tiếng Việt, ngắn (vd hướng bẻ V / N, chỗ chưa chắc). Dùng câu trả lời đã có làm dữ liệu đúng. Không bịa: không đọc được thì để doan rỗng và ghi lý do.";

        public static JsonObject Schema()
        {
            JsonObject s = Kieu("string"), n = Kieu("number");
            JsonObject uon = Obj(("kieu", LuaChon("be", "an_1_lan", "cuon", "pitchi")), ("goc", n), ("r_trong", n), ("cung", n));
            JsonObject td = Obj(("doan", Mang(n)), ("uon", Mang(uon)));
            return Obj(("tam", Mang(Obj(("so_ban_ve", s), ("ten", s), ("kieu", LuaChon("phang", "be", "lam_tay")), ("x", td), ("y", td), ("ghi_chu", s)))));
        }

        /// <summary>Phần chữ gửi kèm bản vẽ: bảng tấm của tệp + câu trả lời phòng 3.</summary>
        public static string DauVao(string tenTep, IEnumerable<TamAi> tam, IEnumerable<VanDe> vanDe)
        {
            var sb = new StringBuilder("Tệp: " + tenTep + "\n\nBảng tấm:\n");
            foreach (TamAi t in tam)
                sb.Append("- so_ban_ve=").Append(t.SoBanVe).Append(" ten=").Append(t.Ten).Append(" M=").Append(t.M).Append(" T=").Append(t.T).Append(" Q=").Append(t.Q)
                  .Append(" kích thước=").Append(t.KichThuoc).Append(" bẻ=").Append(t.CanhJson).Append(" ghi chú=").Append(t.GhiChuVi).Append('\n');
            var tl = (vanDe ?? Enumerable.Empty<VanDe>()).Where(v => v.TraLoi.Trim().Length > 0 || v.TrangThai == VanDe.BoQua).ToList();
            if (tl.Count > 0)
            {
                sb.Append("\nPhòng Kiểm tra — câu trả lời / quyết định (dữ liệu đúng):\n");
                foreach (VanDe v in tl)
                    sb.Append("- ").Append(v.Tam).Append(" | ").Append(v.NoiDung).Append(" | ").Append(v.TraLoi.Trim().Length > 0 ? "trả lời: " + v.TraLoi.Trim() : "bỏ qua: " + v.LyDo).Append('\n');
            }
            return sb.ToString();
        }

        public static string Body(string model, string quyChuan, string pdfBase64, string dauVao, string tomTatDxf)
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
            var nd = new List<object>();
            if (!string.IsNullOrEmpty(pdfBase64))
            {
                var src = new JsonObject();
                src.Set("type", "base64"); src.Set("media_type", "application/pdf"); src.Set("data", pdfBase64);
                var doc = new JsonObject();
                doc.Set("type", "document"); doc.Set("source", src);
                nd.Add(doc);
            }
            nd.Add(Khoi(dauVao + (string.IsNullOrEmpty(tomTatDxf) ? "" : "\n\n" + tomTatDxf), null));
            var msg = new JsonObject();
            msg.Set("role", "user");
            msg.Set("content", nd);
            body.Set("messages", new List<object> { msg });
            return JsonLite.Serialize(body);
        }

        public sealed class PhanHoi
        {
            /// <summary>so_ban_ve|ten (chữ thường) → tấm đọc được.</summary>
            public List<(string SoBanVe, string Ten, TamTrienKhai Tk)> Tam = new List<(string, string, TamTrienKhai)>();
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
            JsonObject nd;
            try { nd = text == null ? null : JsonLite.Parse(text) as JsonObject; } catch (Exception) { nd = null; }
            if (!(nd?.Get("tam") is List<object> ds)) { kq.Loi = p2.Loi ?? "Phản hồi AI không đúng mẫu"; return kq; }
            foreach (JsonObject x in ds.OfType<JsonObject>())
            {
                var tk = new TamTrienKhai
                {
                    Kieu = x.GetString("kieu") ?? "phang", GhiChu = x.GetString("ghi_chu") ?? "",
                    X = DocTd(x.Get("x") as JsonObject), Y = DocTd(x.Get("y") as JsonObject),
                };
                kq.Tam.Add((x.GetString("so_ban_ve") ?? "", x.GetString("ten") ?? "", tk));
            }
            return kq;
        }

        /// <summary>Ghép tấm AI đọc với bảng tấm: theo số bản vẽ, không có thì theo tên; còn lại theo thứ tự.</summary>
        public static TamTrienKhai Ghep(TamAi tam, List<(string SoBanVe, string Ten, TamTrienKhai Tk)> ds, HashSet<TamTrienKhai> daDung)
        {
            bool Bang(string a, string b) => a.Trim().Length > 0 && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
            var kq = ds.FirstOrDefault(x => !daDung.Contains(x.Tk) && Bang(x.SoBanVe, tam.SoBanVe)).Tk
                     ?? ds.FirstOrDefault(x => !daDung.Contains(x.Tk) && Bang(x.Ten, tam.Ten)).Tk
                     ?? ds.FirstOrDefault(x => !daDung.Contains(x.Tk)).Tk;
            if (kq != null) daDung.Add(kq);
            return kq;
        }

        // ---------- JSON trien_khai.json ----------

        public static string Ghi(KetQuaTrienKhai kq)
        {
            var o = new JsonObject();
            o.Set("tu_xac_nhan", kq.TuXacNhan ?? ""); o.Set("luc", kq.Luc ?? "");
            o.Set("token_vao", new JsonNumber(kq.TokenVao.ToString(CultureInfo.InvariantCulture)));
            o.Set("token_ra", new JsonNumber(kq.TokenRa.ToString(CultureInfo.InvariantCulture)));
            o.Set("tien", new JsonNumber(kq.Tien.ToString(CultureInfo.InvariantCulture)));
            o.Set("tam", kq.Tam.Select(t =>
            {
                var x = new JsonObject();
                x.Set("khoa", t.Khoa); x.Set("ten", t.Ten); x.Set("kieu", t.Kieu); x.Set("ghi_chu", t.GhiChu); x.Set("v_chon", t.VChon);
                x.Set("x", GhiTd(t.X)); x.Set("y", GhiTd(t.Y));
                return (object)x;
            }).ToList());
            return JsonLite.Serialize(o);
        }

        public static KetQuaTrienKhai Doc(string json)
        {
            JsonObject o;
            try { o = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { return null; }
            if (o == null) return null;
            var kq = new KetQuaTrienKhai
            {
                TuXacNhan = o.GetString("tu_xac_nhan") ?? "", Luc = o.GetString("luc") ?? "",
                TokenVao = (long)(SoJ(o, "token_vao") ?? 0), TokenRa = (long)(SoJ(o, "token_ra") ?? 0), Tien = (decimal)(SoJ(o, "tien") ?? 0),
            };
            foreach (JsonObject x in (o.Get("tam") as List<object> ?? new List<object>()).OfType<JsonObject>())
                kq.Tam.Add(new TamTrienKhai
                {
                    Khoa = x.GetString("khoa") ?? "", Ten = x.GetString("ten") ?? "", Kieu = x.GetString("kieu") ?? "phang",
                    GhiChu = x.GetString("ghi_chu") ?? "", VChon = x.GetString("v_chon") ?? "",
                    X = DocTd(x.Get("x") as JsonObject), Y = DocTd(x.Get("y") as JsonObject),
                });
            return kq;
        }

        private static JsonObject GhiTd(TietDien td)
        {
            var o = new JsonObject();
            o.Set("doan", td.Doan.Select(d => (object)new JsonNumber(F(d))).ToList());
            o.Set("uon", td.Uon.Select(u =>
            {
                var x = new JsonObject();
                x.Set("kieu", u.Kieu); x.Set("goc", new JsonNumber(F(u.Goc))); x.Set("r_trong", new JsonNumber(F(u.R))); x.Set("cung", new JsonNumber(F(u.Cung)));
                return (object)x;
            }).ToList());
            return o;
        }

        private static TietDien DocTd(JsonObject o)
        {
            var td = new TietDien();
            if (o == null) return td;
            foreach (object d in o.Get("doan") as List<object> ?? new List<object>())
                if (SoObj(d) is double v) td.Doan.Add(v);
            foreach (JsonObject u in (o.Get("uon") as List<object> ?? new List<object>()).OfType<JsonObject>())
                td.Uon.Add(new Uon { Kieu = u.GetString("kieu") ?? "be", Goc = SoJ(u, "goc") ?? 90, R = SoJ(u, "r_trong") ?? 0, Cung = SoJ(u, "cung") ?? 0 });
            return td;
        }

        // ---------- tiện ích ----------

        public static string F(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);
        private static double? SoObj(object v) => v is JsonNumber n && double.TryParse(n.Raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : v is string s ? PhongKiemTra.So(s) : null;
        private static double? SoJ(JsonObject o, string k) => SoObj(o.Get(k));
        private static JsonObject Kieu(string type) { var o = new JsonObject(); o.Set("type", type); return o; }
        private static JsonObject LuaChon(params string[] gt) { JsonObject o = Kieu("string"); o.Set("enum", gt.Cast<object>().ToList()); return o; }
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
