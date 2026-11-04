using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    /// <summary>
    /// Ghi mỗi lần kiểm ra 05_AI\log\yyyyMMdd_HHmmss.json (thư mục trong .gitignore) để so sánh khi
    /// chỉnh lời dặn. Không ghi key: key chỉ nằm ở header HTTP, không có trong body.
    /// </summary>
    public static class NhatKy
    {
        public static string Ghi(string thuMucModule, string model, KetQuaKiemTra kq)
        {
            string thuMuc = Path.Combine(thuMucModule, "log");
            Directory.CreateDirectory(thuMuc);
            string f = Path.Combine(thuMuc, DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".json");
            for (int i = 2; File.Exists(f); i++)
                f = Path.Combine(thuMuc, DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" + i + ".json");

            var o = new JsonObject();
            o.Set("thoi_diem", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            o.Set("model", model);
            var t = new JsonObject();
            t.Set("vao", new JsonNumber(kq.TongSuDung.Vao.ToString(CultureInfo.InvariantCulture)));
            t.Set("ghi_cache", new JsonNumber(kq.TongSuDung.GhiCache.ToString(CultureInfo.InvariantCulture)));
            t.Set("doc_cache", new JsonNumber(kq.TongSuDung.DocCache.ToString(CultureInfo.InvariantCulture)));
            t.Set("ra", new JsonNumber(kq.TongSuDung.Ra.ToString(CultureInfo.InvariantCulture)));
            o.Set("token", t);
            o.Set("tien_usd", new JsonNumber(kq.TongTien.ToString("0.######", CultureInfo.InvariantCulture)));
            o.Set("so_canh_bao", kq.CanhBao.Count);
            o.Set("loi_lo", new List<object>(kq.LoiLo));
            var lo = new List<object>();
            foreach (var (body, phanHoi) in kq.NhatKy)
            {
                var x = new JsonObject();
                x.Set("body", body);
                x.Set("phan_hoi", phanHoi);
                lo.Add(x);
            }
            o.Set("lo", lo);
            File.WriteAllText(f, JsonLite.Serialize(o), new UTF8Encoding(false));
            return f;
        }
    }
}
