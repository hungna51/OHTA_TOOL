using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiTool.Core
{
    /// <summary>Gửi một body request, trả body response (kể cả JSON lỗi). Chỉ ném khi mất mạng / hết giờ / bị huỷ.</summary>
    public interface IGuiYeuCau
    {
        Task<string> Gui(string body, CancellationToken ct);
    }

    public sealed class KetQuaKiemTra
    {
        public List<(KhungDuLieu Khung, CanhBao CB)> CanhBao = new List<(KhungDuLieu, CanhBao)>();
        public List<string> LoiLo = new List<string>();
        public SuDung TongSuDung = new SuDung();
        public decimal TongTien;
        public List<(string Body, string PhanHoi)> NhatKy = new List<(string, string)>();
    }

    public static class KiemTraRunner
    {
        public const int KichThuocLo = 10;
        public const int SongSong = 3;

        /// <summary>Lỗi chặn trước khi gọi mạng (hiện trên tab, có dấu); null = chạy được.</summary>
        public static string LoiTruocKhiChay(string key, int soKhung, string layer)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "Chưa có API key: tạo file 05_AI\\data\\ai.key (xem README)";
            if (soKhung == 0)
                return "Không thấy khung trên layer " + layer;
            return null;
        }

        /// <summary>
        /// Chia lô KichThuocLo khung, chạy tối đa SongSong lô cùng lúc. Lô hỏng (mạng, API, phản
        /// hồi lạ) chỉ ghi vào LoiLo, các lô khác vẫn chạy. Cảnh báo đỏ đứng trước, rồi theo số thứ tự khung.
        /// </summary>
        public static async Task<KetQuaKiemTra> Chay(IList<KhungDuLieu> khung, string model, string loiDan, string khoiVatLieu,
                                                     IGuiYeuCau gui, IProgress<(int xong, int tong)> tienDo, CancellationToken ct)
        {
            List<List<KhungDuLieu>> lo = khung
                .Select((k, i) => (k, i)).GroupBy(x => x.i / KichThuocLo)
                .Select(g => g.Select(x => x.k).ToList()).ToList();
            var ketQua = new (KetQuaLo Kq, string Body, string PhanHoi)[lo.Count];
            int xong = 0;
            var sem = new SemaphoreSlim(SongSong);

            async Task ChayLo(int i)
            {
                string body = YeuCauApi.Body(model, loiDan, khoiVatLieu, DongChu.Lo(lo[i]));
                string phanHoi = null;
                KetQuaLo kq;
                bool coSlot = false;
                try
                {
                    await sem.WaitAsync(ct).ConfigureAwait(false);
                    coSlot = true;
                    phanHoi = await gui.Gui(body, ct).ConfigureAwait(false);
                    kq = PhanHoiApi.Doc(phanHoi, new HashSet<int>(lo[i].Select(k => k.SoThuTu)));
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    kq = new KetQuaLo { Loi = "đã dừng" };
                }
                catch (OperationCanceledException)
                {
                    // HttpClient.Timeout cũng ném TaskCanceledException nhưng người dùng không bấm Dừng
                    kq = new KetQuaLo { Loi = "hết giờ chờ AI (quá 180 giây)" };
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    kq = new KetQuaLo { Loi = "không kết nối được AI (mạng / proxy): " + ex.GetBaseException().Message };
                }
                catch (Exception ex)
                {
                    kq = new KetQuaLo { Loi = ex.Message };
                }
                finally
                {
                    if (coSlot) sem.Release();
                }
                ketQua[i] = (kq, body, phanHoi ?? "(" + kq.Loi + ")");
                tienDo?.Report((Interlocked.Increment(ref xong), lo.Count));
            }

            await Task.WhenAll(Enumerable.Range(0, lo.Count).Select(ChayLo)).ConfigureAwait(false);

            var tong = new KetQuaKiemTra();
            for (int i = 0; i < lo.Count; i++)
            {
                KetQuaLo kq = ketQua[i].Kq;
                tong.NhatKy.Add((ketQua[i].Body, ketQua[i].PhanHoi));
                tong.TongSuDung.Cong(kq.SuDung);
                if (kq.Loi != null)
                    tong.LoiLo.Add("Lô " + (i + 1) + ": " + kq.Loi);
                foreach (CanhBao cb in kq.CanhBao)
                    tong.CanhBao.Add((lo[i].First(k => k.SoThuTu == cb.Khung), cb));
            }
            tong.CanhBao = tong.CanhBao.OrderByDescending(x => x.CB.Do).ThenBy(x => x.Khung.SoThuTu).ToList();
            tong.TongTien = BangGia.Tien(model, tong.TongSuDung);
            return tong;
        }
    }
}
