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

        /// <summary>Giá trị ô "a, b" → từng nhãn (choice nhiều / nhiều người).</summary>
        public static List<string> TachNhan(string giaTri) =>
            (giaTri ?? "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
    }
}
