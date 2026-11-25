using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OhtaTool.Plugin.Core;

namespace AiTool.Core
{
    public enum LoaiDong { TienDo, ThuMuc, KetQua, Loi, Khac }

    /// <summary>
    /// Một dòng OhtaTeams.exe --thu-nhanh-* in ra (giao thức ở _apps\OhtaTeams\Services\ThuNhanhCli.cs):
    /// "@ chữ" tiến độ · "#TM thư mục" · "#KQ json" · "#LOI chữ"; dòng khác (python in ra…) là Khac.
    /// </summary>
    public static class DongExe
    {
        public static (LoaiDong Loai, string Chu) Doc(string dong)
        {
            dong = dong ?? "";
            if (dong.StartsWith("@ ", StringComparison.Ordinal)) return (LoaiDong.TienDo, dong.Substring(2));
            if (dong.StartsWith("#TM ", StringComparison.Ordinal)) return (LoaiDong.ThuMuc, dong.Substring(4).Trim());
            if (dong.StartsWith("#KQ ", StringComparison.Ordinal)) return (LoaiDong.KetQua, dong.Substring(4));
            if (dong.StartsWith("#LOI ", StringComparison.Ordinal)) return (LoaiDong.Loi, dong.Substring(5));
            return (LoaiDong.Khac, dong);
        }
    }

    /// <summary>Một lần thử (thư mục trong %LOCALAPPDATA%\OHTA_TOOL\thu_nhanh, dùng chung với cửa sổ Thử nhanh của OhtaTeams).</summary>
    public sealed class LanThu
    {
        public string ThuMuc, Gio = "", MoTa = "", ChiTiet = "", Anh, Xem, Dxf, ChiTietJson, Loi;
        public List<string> CanhBao = new List<string>();
        /// <summary>Hình chưa có mẫu biên dạng (chua_co / mẫu không có trong thư viện) → hiện nút ✨ AI viết mẫu.</summary>
        public bool CanMau;
        /// <summary>Giá trị AI tự suy vì khách không ghi số — phần vẽ từ đó tô vàng trong DXF, người dùng xác nhận.</summary>
        public List<string> CanXacNhan = new List<string>();

        public bool CoDxf => Dxf != null && File.Exists(Dxf);

        /// <summary>JSON sau "#KQ": một lần thử hoặc mảng (--thu-nhanh-ds). Hỏng → danh sách rỗng.</summary>
        public static List<LanThu> TuJson(string json)
        {
            object goc;
            try { goc = JsonLite.Parse(json); }
            catch (Exception) { return new List<LanThu>(); }
            IEnumerable<object> ds = goc is List<object> mang ? mang : new[] { goc };
            return ds.OfType<JsonObject>().Select(o => new LanThu
            {
                ThuMuc = o.GetString("thu_muc"),
                Gio = o.GetString("gio") ?? "",
                MoTa = o.GetString("mo_ta") ?? "",
                ChiTiet = o.GetString("chi_tiet") ?? "",
                Anh = o.GetString("anh"),
                Xem = o.GetString("xem"),
                Dxf = o.GetString("dxf"),
                ChiTietJson = o.GetString("chi_tiet_json"),
                Loi = o.GetString("loi"),
                CanhBao = (o.Get("canh_bao") as List<object>)?.OfType<string>().ToList() ?? new List<string>(),
                CanMau = o.Get("can_mau") is bool b && b,
                CanXacNhan = (o.Get("can_xac_nhan") as List<object>)?.OfType<string>().ToList() ?? new List<string>(),
            }).ToList();
        }
    }

    public static class DuongDan
    {
        /// <summary>OhtaTeams.exe nằm ở &lt;OHTA_TOOL&gt;\_apps\OhtaTeams\bin (cạnh thư mục tab 05_AI).</summary>
        public static string Exe(string thuMucModuleAi) =>
            Path.Combine(Directory.GetParent(thuMucModuleAi.TrimEnd('\\', '/')).FullName, "_apps", "OhtaTeams", "bin", "OhtaTeams.exe");

        /// <summary>Ghép tham số dòng lệnh Windows: có khoảng trắng / ngoặc kép thì bọc "…", \ cuối nhân đôi để không nuốt ngoặc kép.</summary>
        public static string ThamSo(params string[] ts) => string.Join(" ", ts.Select(Boc));

        private static string Boc(string s)
        {
            if (s.Length > 0 && s.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return s;
            var sb = new StringBuilder("\"");
            int gach = 0;
            foreach (char c in s)
            {
                if (c == '\\') { gach++; continue; }
                sb.Append('\\', c == '"' ? gach * 2 + 1 : gach).Append(c);
                gach = 0;
            }
            return sb.Append('\\', gach * 2).Append('"').ToString();
        }
    }
}
