using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TeamsTool.Services
{
    /// <summary>
    /// Hàng đợi 3 phòng AI (18/11): mỗi phòng chạy MỘT việc một lúc (tránh hết hạn mức AI), 3 phòng chạy song song —
    /// phòng 1 đọc xong đơn A thì A sang phòng 2 (chờ người), phòng 1 lấy ngay đơn B. Nhật ký việc làm + luồng suy nghĩ AI từng việc
    /// cho màn hình 🤖 Phòng AI. Hàng chỉ trong bộ nhớ (đóng app là mất — đơn vẫn giữ kết quả trong thư mục đơn).
    /// Mọi thay đổi chạy trên luồng giao diện (gọi từ luồng khác tự chuyển về).
    /// </summary>
    public static class HangAi
    {
        public static readonly string[] TenPhong = { "1 · Nhận & Đọc", "2 · Kiểm tra & Hỏi", "3 · Triển khai & Vẽ" };

        public sealed class Viec
        {
            public int Phong;
            public string DonId = "", DonTen = "", MoTa = "";
            public DateTime VaoHang = DateTime.Now;
            public DateTime? BatDau;
            public readonly CancellationTokenSource Huy = new CancellationTokenSource();
            /// <summary>Chữ AI suy nghĩ / viết ra (khi bật hiện suy nghĩ) — giữ phần cuối.</summary>
            public readonly StringBuilder SuyNghi = new StringBuilder();
            internal Func<Viec, Task> Lam;
            internal Action BoQua;
        }

        public sealed class DongNhatKy
        {
            public DateTime Luc = DateTime.Now;
            public int Phong;
            public string Don = "", Chu = "";
            public override string ToString() => Luc.ToString("HH:mm:ss") + " · P" + Phong + " · " + Don + " · " + Chu;
        }

        private static readonly List<Viec>[] _hang = { new List<Viec>(), new List<Viec>(), new List<Viec>() };
        private static readonly bool[] _dangChay = new bool[3];
        public static readonly List<DongNhatKy> NhatKy = new List<DongNhatKy>();

        /// <summary>Hàng / trạng thái việc đổi.</summary>
        public static event Action ThayDoi;
        public static event Action<DongNhatKy> CoNhatKy;
        public static event Action<Viec> CoSuyNghi;

        /// <summary>Việc của phòng (1–3): phần tử đầu đang chạy (BatDau khác null), sau là chờ.</summary>
        public static IReadOnlyList<Viec> Hang(int phong) => _hang[phong - 1];

        /// <summary>Việc đang chờ / chạy của đơn (null = không có).</summary>
        public static Viec CuaDon(string donId) => _hang.SelectMany(h => h).FirstOrDefault(v => v.DonId == donId);

        /// <summary>Đưa việc vào hàng phòng; tới lượt chạy lam(việc). boQua: gọi khi việc bị huỷ lúc còn chờ (lam không chạy).</summary>
        public static Viec Them(int phong, string donId, string donTen, string moTa, Func<Viec, Task> lam, Action boQua)
        {
            var v = new Viec { Phong = phong, DonId = donId, DonTen = donTen, MoTa = moTa, Lam = lam, BoQua = boQua };
            _hang[phong - 1].Add(v);
            Ghi(phong, donTen, (_hang[phong - 1].Count > 1 ? "⏳ vào hàng (thứ " + _hang[phong - 1].Count + "): " : "▶ ") + moTa);
            ThayDoi?.Invoke();
            // chạy sau khi Them trả về (người gọi đã cầm việc), trên luồng giao diện
            if (!_dangChay[phong - 1]) Application.Current?.Dispatcher.BeginInvoke(new Action(() => Chay(phong)));
            return v;
        }

        private static async void Chay(int phong)
        {
            if (_dangChay[phong - 1]) return;
            _dangChay[phong - 1] = true;
            try
            {
                List<Viec> hang = _hang[phong - 1];
                while (hang.Count > 0)
                {
                    Viec v = hang[0];
                    if (v.Huy.IsCancellationRequested) { hang.Remove(v); v.BoQua?.Invoke(); ThayDoi?.Invoke(); continue; }
                    v.BatDau = DateTime.Now;
                    ThayDoi?.Invoke();
                    try { await v.Lam(v); }
                    catch (Exception ex) { Ghi(phong, v.DonTen, "Lỗi: " + ex.Message); } // việc tự báo lỗi của nó; đây chỉ để hàng không kẹt
                    hang.Remove(v);
                    Ghi(phong, v.DonTen, "■ xong " + v.MoTa + " (" + ThoiGian(DateTime.Now - v.BatDau.Value) + ")");
                    ThayDoi?.Invoke();
                }
            }
            finally { _dangChay[phong - 1] = false; }
        }

        /// <summary>Huỷ việc của đơn: đang chờ → bỏ khỏi hàng; đang chạy → báo huỷ (việc tự dừng ở chỗ kiểm tra huỷ kế tiếp).</summary>
        public static void Huy(string donId)
        {
            Viec v = CuaDon(donId);
            if (v == null) return;
            v.Huy.Cancel();
            if (v.BatDau == null)
            {
                _hang[v.Phong - 1].Remove(v);
                v.BoQua?.Invoke();
                Ghi(v.Phong, v.DonTen, "✕ huỷ khi đang chờ: " + v.MoTa);
                ThayDoi?.Invoke();
            }
        }

        public static void Ghi(int phong, string don, string chu)
        {
            if (string.IsNullOrWhiteSpace(chu)) return;
            if (!TrenGiaoDien(() => Ghi(phong, don, chu))) return;
            var d = new DongNhatKy { Phong = phong, Don = don ?? "", Chu = chu.Trim().Replace("\r", "").Replace("\n", " ⏎ ") };
            if (NhatKy.Count > 0 && NhatKy[NhatKy.Count - 1].Chu == d.Chu && NhatKy[NhatKy.Count - 1].Don == d.Don) return; // Bao lặp lại cùng câu
            NhatKy.Add(d);
            if (NhatKy.Count > 2000) NhatKy.RemoveRange(0, 500);
            CoNhatKy?.Invoke(d);
        }

        public static void ThemSuyNghi(Viec v, string chu)
        {
            if (v == null || string.IsNullOrEmpty(chu)) return;
            if (!TrenGiaoDien(() => ThemSuyNghi(v, chu))) return;
            v.SuyNghi.Append(chu);
            if (v.SuyNghi.Length > 200000) v.SuyNghi.Remove(0, 60000);
            CoSuyNghi?.Invoke(v);
        }

        public static string ThoiGian(TimeSpan t) => t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");

        /// <summary>Đang trên luồng giao diện → true (làm luôn); không → chuyển về luồng giao diện rồi false.</summary>
        private static bool TrenGiaoDien(Action lam)
        {
            var dp = Application.Current?.Dispatcher;
            if (dp == null || dp.CheckAccess()) return true;
            dp.BeginInvoke(lam);
            return false;
        }
    }
}
