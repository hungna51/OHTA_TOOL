using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace TeamsTool.Core
{
    /// <summary>Một bài học AI đề xuất từ chỗ người sửa. TrangThai: "" chờ duyệt / "them" đã ghi vào bai_hoc.md / "bo" bỏ.</summary>
    public sealed class DeXuatBaiHoc
    {
        public string NoiDung = "", GopVoi = "", LyDo = "", TrangThai = "";
    }

    /// <summary>Một ô người đã sửa khác giá trị AI đọc.</summary>
    public sealed class ChoSua
    {
        public TamAi Tam;
        public string O = "", Ai = "", Dung = "";
        /// <summary>Chữ ký để không gửi rút bài học hai lần cho cùng một chỗ sửa.</summary>
        public string ChuKy => Tam.Khoa + "|" + O + "|" + Dung;
    }

    /// <summary>
    /// Bài học đọc bản vẽ (11/12): người sửa bảng tấm → khi xác nhận, AI xem mình đọc sai ở đâu, đề xuất bài học
    /// (gộp với bài học trùng ý); máy Admin duyệt thì ghi vào bai_hoc.md — lần đọc sau gửi kèm quy chuẩn.
    /// </summary>
    public static class PhongBaiHoc
    {
        public const string TenTep = "bai_hoc.md";
        /// <summary>Quá số bài học này thì nhắc Admin dọn bớt.</summary>
        public const int NguongNhac = 60;
        public const string DauTep = "# Bài học đọc bản vẽ — AI rút từ chỗ người sửa, máy Admin duyệt (mỗi dòng \"- \" một bài học)";

        private static readonly Dictionary<string, string> NhanO = new Dictionary<string, string>
        {
            ["so_ban_ve"] = "số bản vẽ", ["ten"] = "tên tấm", ["M"] = "vật liệu M", ["Z"] = "chế độ cắt Z", ["T"] = "độ dày T", ["Q"] = "số lượng Q",
            ["mat"] = "bề mặt", ["kich_thuoc"] = "kích thước bao", ["dung_sai"] = "dung sai", ["ghi_chu_jp"] = "ghi chú JP", ["ghi_chu_vi"] = "ghi chú VI",
            ["kieu_td"] = "kiểu tiết diện", ["td_x"] = "tiết diện X", ["td_y"] = "tiết diện Y", ["ghi_chu_td"] = "ghi chú tiết diện",
        };

        /// <summary>Các ô tấm mới người đã sửa khác giá trị AI, chưa gửi rút bài học.</summary>
        public static List<ChoSua> CacChoSua(ChiTiet ct)
        {
            var ds = new List<ChoSua>();
            foreach (TamAi t in ct.Tam.Where(x => x.Moi))
                foreach (string o in t.DaSua.OrderBy(x => x))
                {
                    if (t.TheoTraLoi.Contains(o)) continue; // đổi theo câu trả lời của khách — không phải AI đọc sai
                    if (!t.GiaTriAi.TryGetValue(o, out string ai)) continue; // sửa từ bản cũ, không biết AI đọc gì
                    string dung = PhongDocBanVe.Lay(t, o);
                    var c = new ChoSua { Tam = t, O = o, Ai = ai, Dung = dung };
                    if (ai.Trim() != dung.Trim() && !ct.DaRutBaiHoc.Contains(c.ChuKy)) ds.Add(c);
                }
            return ds;
        }

        /// <summary>Các bài học trong tệp (dòng "- …").</summary>
        public static List<string> CacBaiHoc(string noiDung) =>
            (noiDung ?? "").Replace("\r", "").Split('\n').Select(s => s.Trim()).Where(s => s.StartsWith("- ", StringComparison.Ordinal))
                           .Select(s => s.Substring(2).Trim()).Where(s => s.Length > 0).ToList();

        /// <summary>Ghi bài học vào nội dung tệp: GopVoi trùng một dòng cũ → thay dòng đó; không thì thêm cuối. Tệp rỗng → thêm dòng đầu.</summary>
        public static string ApDung(string noiDung, DeXuatBaiHoc d)
        {
            var dong = (noiDung ?? "").Replace("\r", "").TrimEnd('\n').Split('\n').ToList();
            if (dong.Count == 1 && dong[0].Trim().Length == 0) dong = new List<string> { DauTep, "" };
            string moi = "- " + d.NoiDung.Trim().Replace("\n", " ");
            int i = d.GopVoi.Trim().Length == 0 ? -1
                  : dong.FindIndex(s => s.Trim().StartsWith("- ", StringComparison.Ordinal) && s.Trim().Substring(2).Trim() == d.GopVoi.Trim());
            if (i >= 0) dong[i] = moi; else dong.Add(moi);
            return string.Join("\n", dong) + "\n";
        }

        public const string LoiDan =
@"Bạn rà lại lần AI đọc bản vẽ kim loại tấm của OHTA. Lần đọc trước đã đọc sai một số ô; người triển khai đã sửa lại đúng (đầu vào). Với các chỗ sửa:
- Tìm nguyên nhân AI đọc sai (nhầm ký hiệu, bỏ sót ghi chú, hiểu sai quy ước bản vẽ / của công ty, nhầm hình chiếu, sai cách đo tiết diện…). Có bản vẽ đính kèm thì xem lại đúng chỗ đó.
- Viết BÀI HỌC tổng quát để lần sau đọc đúng: tiếng Việt, một câu ≤ 30 từ, dạng quy tắc (""Khi … thì …""), dùng được cho mọi bản vẽ cùng kiểu — không ghi số riêng của tấm này, không ghi tên tệp.
- Chỗ sửa chỉ là lựa chọn của khách / lỗi gõ / thông tin không có trên bản vẽ → không viết bài học cho chỗ đó.
- So với ""Bài học đã có"" và QUY CHUẨN: trùng ý → không thêm; gần ý / cần bổ sung → viết câu gộp mới và ghi gop_voi = NGUYÊN VĂN bài học cũ đó; ý mới → gop_voi rỗng.
- ly_do: một câu ngắn cho người duyệt: AI đã sai ở đâu.
Không có bài học nào đáng ghi → bai_hoc rỗng.";

        public static JsonObject Schema()
        {
            JsonObject s = Kieu("string");
            return Obj(("bai_hoc", Mang(Obj(("noi_dung", s), ("gop_voi", s), ("ly_do", s)))));
        }

        /// <summary>Phần chữ: các chỗ sửa (tệp, tấm, ô, AI đọc → đúng, lý do người sửa) + bài học đã có.</summary>
        public static string DauVao(IEnumerable<ChoSua> choSua, string congTy, IEnumerable<string> baiHocCu)
        {
            var ds = choSua.ToList();
            var sb = new StringBuilder("Công ty đặc biệt: " + (string.IsNullOrWhiteSpace(congTy) ? "(không)" : congTy) + "\n\nCác chỗ người sửa:\n");
            foreach (ChoSua c in ds)
                sb.Append("- ").Append(c.Tam.Tep).Append(" | ").Append(PhongKiemTra.TenTam(c.Tam)).Append(" | ").Append(NhanO.TryGetValue(c.O, out string n) ? n : c.O)
                  .Append(": AI đọc «").Append(c.Ai).Append("» → đúng «").Append(c.Dung).Append('»')
                  .Append(c.Tam.LyDoSua.Trim().Length > 0 ? " | lý do người sửa: " + c.Tam.LyDoSua.Trim() : "").Append('\n');
            if (ds.Any(c => c.O == "td_x" || c.O == "td_y"))
                sb.Append("(Tiết diện viết dạng chữ: ").Append(PhongTrienKhai.HuongDanTd).Append(")\n");
            var cu = (baiHocCu ?? Enumerable.Empty<string>()).ToList();
            sb.Append("\nBài học đã có:\n");
            if (cu.Count == 0) sb.Append("(chưa có)\n");
            foreach (string b in cu) sb.Append("- ").Append(b).Append('\n');
            return sb.ToString();
        }

        public static string Body(string model, string quyChuan, string pdfBase64, string dauVao) =>
            PhongTrienKhai.BodyChung(model, LoiDan, Schema(), quyChuan, pdfBase64, dauVao, null);

        public sealed class PhanHoi
        {
            public List<DeXuatBaiHoc> BaiHoc = new List<DeXuatBaiHoc>();
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
            if (!(nd?.Get("bai_hoc") is List<object> ds)) { kq.Loi = p2.Loi ?? "Phản hồi AI không đúng mẫu"; return kq; }
            foreach (JsonObject x in ds.OfType<JsonObject>())
            {
                string noiDung = (x.GetString("noi_dung") ?? "").Trim();
                if (noiDung.Length > 0)
                    kq.BaiHoc.Add(new DeXuatBaiHoc { NoiDung = noiDung, GopVoi = (x.GetString("gop_voi") ?? "").Trim(), LyDo = (x.GetString("ly_do") ?? "").Trim() });
            }
            return kq;
        }

        public static JsonObject GhiDeXuat(DeXuatBaiHoc d)
        {
            var o = new JsonObject();
            o.Set("noi_dung", d.NoiDung); o.Set("gop_voi", d.GopVoi); o.Set("ly_do", d.LyDo); o.Set("trang_thai", d.TrangThai);
            return o;
        }

        public static DeXuatBaiHoc DocDeXuat(JsonObject o) => new DeXuatBaiHoc
        {
            NoiDung = o.GetString("noi_dung") ?? "", GopVoi = o.GetString("gop_voi") ?? "", LyDo = o.GetString("ly_do") ?? "", TrangThai = o.GetString("trang_thai") ?? "",
        };

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
