using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamsTool.Core
{
    /// <summary>Quy tắc của bảng đơn dạng Lists trong OhtaTeams (logic thuần, test được).</summary>
    public static class BangDon
    {
        /// <summary>
        /// Màu dòng (quy tắc xưởng, Graph không trả định dạng màu của view Lists):
        /// "hong" = quá Hạn hoàn thành mà chưa xong (ưu tiên nhất); rồi theo Trạng thái: "do" = Câu hỏi - xác nhận,
        /// "vang" = Đang check, "xanh" = Đang triển khai; null = không tô. Trạng thái nhiều giá trị: lấy theo thứ tự trên.
        /// </summary>
        public static string MauDong(DonTeams d, DateTime homNay)
        {
            if (d.DaXong) return null;
            if (d.Han != null && d.Han.Value.Date < homNay.Date) return "hong";
            string tt = PhanTichTeams.BoDau(d.TrangThai);
            if (tt.Contains("cau hoi")) return "do";
            if (tt.Contains("dang check")) return "vang";
            if (tt.Contains("dang trien khai")) return "xanh";
            return null;
        }

        /// <summary>Màu nhãn Trạng thái như trên Lists (khoá = tên bỏ dấu): (nền, chữ).</summary>
        private static readonly Dictionary<string, (string Nen, string Chu)> MauTT = new Dictionary<string, (string, string)>
        {
            ["chua trien khai"] = ("#C9C9C9", "#2B2B2B"),
            ["dang trien khai"] = ("#74B9F2", "#1B1B1B"),
            ["cau hoi - xac nhan"] = ("#F0908E", "#1B1B1B"),
            ["dang check"] = ("#E2C18A", "#1B1B1B"),
            ["dang sua"] = ("#A8262E", "#FFFFFF"),
            ["lien lac"] = ("#12C9A8", "#1B1B1B"),
            ["da hoan thanh"] = ("#8EE38A", "#1B1B1B"),
            ["da nhap gia"] = ("#8A3B88", "#FFFFFF"),
            ["du lieu sua doi"] = ("#7C8DF2", "#1B1B1B"),
            ["stop"] = ("#B1262B", "#FFFFFF"),
        };

        /// <summary>Màu cố định của một giá trị Trạng thái (null = không phải trạng thái đã biết → màu tự gán).</summary>
        public static (string Nen, string Chu)? MauTrangThai(string giaTri) =>
            MauTT.TryGetValue(PhanTichTeams.BoDau(giaTri ?? ""), out var m) ? m : ((string, string)?)null;

        /// <summary>Chỉ số màu nhãn (0..soMau-1) cố định theo chữ: cùng giá trị luôn cùng màu.</summary>
        public static int MauNhan(string giaTri, int soMau)
        {
            int h = 17;
            foreach (char ch in giaTri ?? "") h = unchecked(h * 31 + ch);
            return (h & int.MaxValue) % soMau;
        }

        /// <summary>Thứ tự cột của view "tiến độ" trên Teams (khớp tên hiển thị bỏ dấu, chứa chuỗi).</summary>
        private static readonly string[] ThuTuView =
            { "title", "nguoi trien khai", "nguoi check 1", "加工", "trang thai", "han hoan thanh", "切断スタート", "納期", "担当者", "loai don", "phan loai", "cong ty", "注意" };

        /// <summary>
        /// Cột hiện mặc định: các cột theo thứ tự view tiến độ (mỗi cột dùng một lần), rồi các cột còn lại có giá trị
        /// ở ít nhất 1 đơn theo thứ tự danh sách, tối đa toiDa cột.
        /// </summary>
        public static List<string> CotMacDinh(List<CotList> cots, List<DonTeams> don, int toiDa = 13)
        {
            var coGiaTri = new HashSet<string>(don.SelectMany(d => d.TatCa.Select(kv => kv.Key)));
            var kq = new List<CotList>();
            foreach (string k in ThuTuView)
            {
                CotList c = cots.FirstOrDefault(x => !kq.Contains(x) && (x.Ten == "Title" ? k == "title" : PhanTichTeams.BoDau(x.TieuDe).Contains(k)));
                if (c != null) kq.Add(c);
            }
            kq.AddRange(cots.Where(c => !kq.Contains(c) && coGiaTri.Contains(c.TieuDe)));
            return kq.Take(toiDa).Select(c => c.Ten).ToList();
        }

        /// <summary>Nhóm theo Hạn hoàn thành: "Chưa được gán" (không hạn) trên cùng, rồi ngày tăng dần.</summary>
        public static List<IGrouping<DateTime?, DonTeams>> NhomTheoHan(IEnumerable<DonTeams> don) =>
            don.GroupBy(d => d.Han?.Date).OrderBy(g => g.Key ?? DateTime.MinValue).ToList();

        /// <summary>Kéo cột tu thả vào chỗ cột den: tu chiếm vị trí của den, các cột giữa dịch một bậc. Không đổi → null.</summary>
        public static List<string> DoiCho(List<string> cot, string tu, string den)
        {
            int a = cot.IndexOf(tu), b = cot.IndexOf(den);
            if (a < 0 || b < 0 || a == b) return null;
            var kq = new List<string>(cot);
            kq.RemoveAt(a);
            kq.Insert(b, tu);
            return kq;
        }

        /// <summary>Giá trị ô "a, b" → từng nhãn (choice nhiều / nhiều người).</summary>
        public static List<string> TachNhan(string giaTri) =>
            (giaTri ?? "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
    }
}
