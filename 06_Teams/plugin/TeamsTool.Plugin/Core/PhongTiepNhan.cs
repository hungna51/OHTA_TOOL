using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một tệp của đơn đã tải về thư mục đơn (phòng 1).</summary>
    public sealed class TepDon
    {
        public string Ten = "", Nguon = "don"; // don = đính kèm dòng Lists; chat = tệp gửi trong chuỗi chat
        public string Loai = PhongTiepNhan.BanVe;
        public string MaBam = "";              // SHA-256 nội dung: tệp không đổi thì phòng 2 không gọi AI lại
    }

    /// <summary>Kết quả phòng 1, lưu &lt;thư mục đơn&gt;\ai\tiep_nhan.json.</summary>
    public sealed class TiepNhan
    {
        public string LoaiDon = PhongTiepNhan.Moi, CongTy = "", BanNhapBango = "";
        public List<TepDon> Tep = new List<TepDon>();
    }

    /// <summary>
    /// Phòng 1 — Tiếp nhận (spec 2026-11-11, QC I.1): xếp loại tệp, loại đơn, công ty đặc biệt, thư mục đơn.
    /// Logic thuần, test được; tải tệp / gộp PDF nằm ở OhtaTeams.
    /// </summary>
    public static class PhongTiepNhan
    {
        // Loại tệp
        public const string BanVe = "ban_ve", Ba_D = "3d", DonHang = "don_hang", Khac = "khac";
        public static readonly string[] LoaiTep = { BanVe, Ba_D, DonHang, Khac };

        // Loại đơn (QC I.1)
        public const string Moi = "moi", CanLienLac = "can_lien_lac", Repeat = "repeat", NuaRepeat = "nua_repeat";
        public static readonly string[] LoaiDon = { Moi, CanLienLac, Repeat, NuaRepeat };

        public const string ThuMucAi = "ai", ThuMucGocTep = "goc";

        /// <summary>
        /// Loại tệp theo đuôi + tên. PDF không đọc chữ được ở đây nên đoán theo tên
        /// (注文書 / 見積 / mail… = đơn hàng, còn lại = bản vẽ) — người dùng sửa được trên màn hình.
        /// </summary>
        public static string XepLoai(string ten)
        {
            string ext = (Path.GetExtension(ten ?? "") ?? "").ToLowerInvariant();
            string t = PhanTichTeams.BoDau(Path.GetFileNameWithoutExtension(ten ?? ""));
            switch (ext)
            {
                case ".dwg": case ".dxf": return BanVe;
                case ".step": case ".stp": case ".igs": case ".iges": case ".sldprt": return Ba_D;
                case ".jpg": case ".jpeg": case ".png": case ".bmp": case ".msg": case ".eml": return DonHang;
                case ".pdf":
                    return Co(t, "注文", "発注", "見積", "依頼", "mail", "メール", "order", "don hang", "bao gia") ? DonHang : BanVe;
                default: return Khac;
            }
        }

        /// <summary>
        /// Loại đơn đoán từ các cột của đơn (cột "Loại đơn", "Phân loại", 加工, 注意…): repeat / một phần repeat / cần liên lạc (pitchi, kakumaru, ensui) / mới.
        /// </summary>
        public static string DoanLoaiDon(IEnumerable<KeyValuePair<string, string>> cot)
        {
            string s = PhanTichTeams.BoDau(string.Join(" | ", (cot ?? Enumerable.Empty<KeyValuePair<string, string>>()).Select(kv => kv.Value)));
            bool rep = Co(s, "repeat", "リピート");
            if (rep && Co(s, "moi", "new", "一部", "mot phan")) return NuaRepeat;
            if (rep) return Repeat;
            return Co(s, "pitchi", "pitch", "ピッチ", "kakumaru", "角丸", "ensui", "円錐") ? CanLienLac : Moi;
        }

        // Bỏ dấu cả từ khoá: BoDau tách ピ thành ヒ + ゚ rồi bỏ dấu, nên phải so hai phía cùng kiểu
        private static bool Co(string daBoDau, params string[] khoa) => khoa.Any(k => daBoDau.Contains(PhanTichTeams.BoDau(k)));

        /// <summary>
        /// Công ty đặc biệt (QC III): dòng đầu khớp trong danh sách "khoá|tên hiện" (quy_chuan/cong_ty.txt — gitignore).
        /// So khoá (bỏ dấu, thường) với mọi cột của đơn. Không khớp → "".
        /// </summary>
        public static string DoanCongTy(IEnumerable<KeyValuePair<string, string>> cot, IEnumerable<string> dongCongTy)
        {
            string s = PhanTichTeams.BoDau(string.Join(" | ", (cot ?? Enumerable.Empty<KeyValuePair<string, string>>()).Select(kv => kv.Value)));
            foreach (string dong in dongCongTy ?? Enumerable.Empty<string>())
            {
                string d = (dong ?? "").Trim();
                if (d.Length == 0 || d.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] p = d.Split('|');
                string ten = p.Length > 1 ? p[1].Trim() : p[0].Trim();
                foreach (string khoa in p[0].Split(','))
                {
                    string k = PhanTichTeams.BoDau(khoa);
                    if (k.Length >= 2 && s.Contains(k)) return ten;
                }
            }
            return "";
        }

        /// <summary>Repeat hoàn toàn → dừng sau phòng 1, không gọi AI.</summary>
        public static bool DungSauTiepNhan(string loaiDon) => loaiDon == Repeat;

        /// <summary>
        /// PDF gộp vào bản nháp &lt;mã&gt;-番号.PDF (QC I.1.A): đơn hàng trước, bản vẽ sau, mỗi nhóm theo tên.
        /// </summary>
        public static List<TepDon> ThuTuBango(IEnumerable<TepDon> tep) =>
            (tep ?? Enumerable.Empty<TepDon>())
                .Where(t => string.Equals(Path.GetExtension(t.Ten), ".pdf", StringComparison.OrdinalIgnoreCase) && (t.Loai == DonHang || t.Loai == BanVe))
                .OrderBy(t => t.Loai == DonHang ? 0 : 1).ThenBy(t => t.Ten, StringComparer.OrdinalIgnoreCase).ToList();

        public static string TenBango(string title) => TenAnToan(title) + "-番号.PDF";

        public static string TenAnToan(string s) => string.Concat((s ?? "").Trim().Split(Path.GetInvalidFileNameChars()));

        /// <summary>
        /// Thư mục của đơn: thư mục có sẵn (≤ 3 cấp dưới gốc, tên chứa Title — cùng luật khớp bản vẽ của tab CAD),
        /// không có thì &lt;gốc&gt;\&lt;Title&gt;. Gốc rỗng / Title quá ngắn → null.
        /// </summary>
        public static string ThuMucDon(string goc, string title)
        {
            if (string.IsNullOrWhiteSpace(goc) || PhanTichTeams.BoDau(title ?? "").Length < PhanTichDangLam.TitleToiThieu) return null;
            if (Directory.Exists(goc))
            {
                var mucNay = new List<string> { goc };
                for (int cap = 0; cap < PhanTichDangLam.SauToiDa && mucNay.Count > 0; cap++)
                {
                    var con = new List<string>();
                    foreach (string m in mucNay)
                    {
                        string[] ds;
                        try { ds = Directory.GetDirectories(m); } catch (Exception) { continue; }
                        foreach (string d in ds.OrderBy(x => x.Length))
                        {
                            if (PhanTichDangLam.TenKhop(Path.GetFileName(d), title)) return d;
                            con.Add(d);
                        }
                    }
                    mucNay = con;
                }
            }
            return Path.Combine(goc, TenAnToan(title));
        }

        public static string MaBam(string file)
        {
            using (var sha = SHA256.Create())
            using (FileStream f = File.OpenRead(file))
                return BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "").ToLowerInvariant();
        }

        // ---------- tiep_nhan.json ----------

        public static string Ghi(TiepNhan tn)
        {
            var o = new JsonObject();
            o.Set("loai_don", tn.LoaiDon);
            o.Set("cong_ty", tn.CongTy);
            o.Set("ban_nhap_bango", tn.BanNhapBango);
            o.Set("tep", tn.Tep.Select(t =>
            {
                var x = new JsonObject();
                x.Set("ten", t.Ten); x.Set("nguon", t.Nguon); x.Set("loai", t.Loai); x.Set("ma_bam", t.MaBam);
                return (object)x;
            }).ToList());
            return JsonLite.Serialize(o);
        }

        /// <summary>JSON hỏng / thiếu → null (coi như chưa chạy).</summary>
        public static TiepNhan Doc(string json)
        {
            JsonObject o;
            try { o = JsonLite.Parse(json ?? "") as JsonObject; } catch (Exception) { return null; }
            if (o == null) return null;
            var tn = new TiepNhan
            {
                LoaiDon = LoaiDon.Contains(o.GetString("loai_don")) ? o.GetString("loai_don") : Moi,
                CongTy = o.GetString("cong_ty") ?? "",
                BanNhapBango = o.GetString("ban_nhap_bango") ?? "",
            };
            foreach (JsonObject x in (o.Get("tep") as List<object> ?? new List<object>()).OfType<JsonObject>())
                tn.Tep.Add(new TepDon
                {
                    Ten = x.GetString("ten") ?? "",
                    Nguon = x.GetString("nguon") ?? "don",
                    Loai = LoaiTep.Contains(x.GetString("loai")) ? x.GetString("loai") : Khac,
                    MaBam = x.GetString("ma_bam") ?? "",
                });
            return tn;
        }
    }
}
