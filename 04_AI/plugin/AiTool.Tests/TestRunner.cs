using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AiTool.Core;

namespace AiTool.Tests
{
    internal static partial class Program
    {
        /// <summary>Giả lập Claude: mỗi khung trong lô được 1 cảnh báo; khung chẵn = vàng, lẻ = đỏ.</summary>
        private sealed class GuiGia : IGuiYeuCau
        {
            public int SoLanGoi;
            public int NemLoiOLan = -1;

            public Task<string> Gui(string body, CancellationToken ct)
            {
                int lan = Interlocked.Increment(ref SoLanGoi);
                if (lan == NemLoiOLan)
                    throw new HttpRequestException("mat mang");
                var so = Regex.Matches(body, @"#KHUNG (\d+) ").Cast<Match>().Select(m => int.Parse(m.Groups[1].Value));
                string ds = string.Join(",", so.Select(k =>
                    "{\"khung\":" + k + ",\"muc_do\":\"" + (k % 2 == 0 ? "vang" : "do") + "\",\"loai_loi\":\"Q\",\"ly_do\":\"x\",\"goi_y\":\"\"}"));
                return Task.FromResult(PhanHoiMau("{\"canh_bao\":[" + ds + "]}"));
            }
        }

        private static List<KhungDuLieu> NhieuKhung(int n) =>
            Enumerable.Range(1, n).Select(i => new KhungDuLieu { SoThuTu = i, MaxX = 10, MaxY = 10 }).ToList();

        private static void TestRunner()
        {
            Console.WriteLine("=== KiemTraRunner ===");
            var gia = new GuiGia();
            KetQuaKiemTra kq = KiemTraRunner.Chay(NhieuKhung(23), "claude-sonnet-5-5", "LD", "VL", gia, null, CancellationToken.None).Result;
            Kiem("Runner_23Khung_3Lo", gia.SoLanGoi, 3);
            Kiem("23 canh bao", kq.CanhBao.Count, 23);
            Kiem("Khong loi lo", kq.LoiLo.Count, 0);
            Kiem("Runner_CongToken: 3 lo x 1000 vao", kq.TongSuDung.Vao, 3000L);
            Kiem("Runner_CongTien > 0", kq.TongTien > 0m, true);
            Kiem("Runner_SapDoTruocVang: 12 do dung truoc",
                 string.Join("", kq.CanhBao.Select(c => c.CB.Do ? "D" : "V")), new string('D', 12) + new string('V', 11));
            Kiem("Trong nhom do: theo so thu tu khung", string.Join(",", kq.CanhBao.Take(3).Select(c => c.Khung.SoThuTu)), "1,3,5");
            Kiem("Nhat ky: 3 cap body/phan hoi", kq.NhatKy.Count, 3);

            var loi = new GuiGia { NemLoiOLan = 2 };
            KetQuaKiemTra kq2 = KiemTraRunner.Chay(NhieuKhung(23), "claude-sonnet-5-5", "LD", "VL", loi, null, CancellationToken.None).Result;
            Kiem("Runner_MotLoLoi_LoKhacVanCoKetQua: 1 loi lo", kq2.LoiLo.Count, 1);
            Kiem("Loi lo co ly do", kq2.LoiLo[0].Contains("mat mang"), true);
            Kiem("Lo khac van co canh bao (it nhat 3 khung lo 3)", kq2.CanhBao.Count >= 3 && kq2.CanhBao.Count < 23, true);

            var tienDo = new List<(int, int)>();
            var p = new DongBo<(int, int)>(tienDo.Add);
            KiemTraRunner.Chay(NhieuKhung(5), "claude-sonnet-5-5", "LD", "VL", new GuiGia(), p, CancellationToken.None).Wait();
            Kiem("Tien do: bao xong 1/1", tienDo.LastOrDefault(), (1, 1));

            Kiem("Runner_ThieuKey_BaoLoi", KiemTraRunner.LoiTruocKhiChay(null, 3), "Chua co API key: tao file 04_AI\\data\\ai.key (xem README)");
            Kiem("Key rong cung bao loi", KiemTraRunner.LoiTruocKhiChay("  ", 3) != null, true);
            Kiem("Khong co khung", KiemTraRunner.LoiTruocKhiChay("sk", 0), "Khong thay khung tren layer da chon");
            Kiem("Hop le -> null", KiemTraRunner.LoiTruocKhiChay("sk", 1), null);

            string tam = Path.Combine(Path.GetTempPath(), "aitool_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tam);
            try
            {
                string f = NhatKy.Ghi(tam, "claude-sonnet-5-5", kq);
                string nd = File.ReadAllText(f);
                Kiem("Nhat ky: file nam trong log\\", Path.GetFileName(Path.GetDirectoryName(f)), "log");
                Kiem("NhatKy_KhongChuaKey", nd.Contains("sk-ant-"), false);
                Kiem("Nhat ky co model va tong token", nd.Contains("claude-sonnet-5-5") && nd.Contains("3000"), true);
            }
            finally
            {
                Directory.Delete(tam, true);
            }
            Console.WriteLine();
        }

        /// <summary>IProgress gọi đồng bộ (Progress&lt;T&gt; đẩy qua SynchronizationContext nên test không bắt kịp).</summary>
        private sealed class DongBo<T> : IProgress<T>
        {
            private readonly Action<T> _a;
            public DongBo(Action<T> a) { _a = a; }
            public void Report(T value) { lock (this) _a(value); }
        }
    }
}
