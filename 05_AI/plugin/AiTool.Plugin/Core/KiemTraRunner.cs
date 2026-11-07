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
        /// <summary>Nhãn phân loại gia công theo số thứ tự khung (vd. BE, TAPU, BE - TAPU, PHANG).</summary>
        public Dictionary<int, string> NhanGiaCong = new Dictionary<int, string>();
        public int SoKhungQuaKev;
        public int SoKhungGuiClaude;
        public double ThoiGianKevMs;
    }

    public static class KiemTraRunner
    {
        public const int KichThuocLo = 10;
        public const int SongSong = 3;
        public const int SongSongKev = 4;

        public const string CheDoHybrid = "Hybrid (Kev + Claude/Ling)";
        public const string CheDoChiKev = "Chỉ Kev (Local/LAN)";
        public const string CheDoChiClaude = "Chỉ Claude / Ling";

        public static readonly string[] DanhSachCheDo = { CheDoHybrid, CheDoChiKev, CheDoChiClaude };

        /// <summary>Lỗi chặn trước khi gọi mạng (hiện trên tab, có dấu); null = chạy được.</summary>
        public static string LoiTruocKhiChay(string key, int soKhung, string layer) =>
            LoiTruocKhiChay(CheDoChiClaude, key, KevApi.UrlMacDinh, soKhung, layer);

        public static string LoiTruocKhiChay(string cheDo, string key, string kevUrl, int soKhung, string layer)
        {
            bool batBuocClaude = string.Equals(cheDo, CheDoChiClaude, StringComparison.Ordinal);
            bool canKev = !string.Equals(cheDo, CheDoChiClaude, StringComparison.Ordinal);

            if (batBuocClaude && string.IsNullOrWhiteSpace(key))
                return "Chưa có API key cho model đang chọn: bấm ⚙ nhập key Claude (ai.key) hoặc Ling (ling.key)";
            if (canKev && string.IsNullOrWhiteSpace(kevUrl))
                return "Chưa có địa chỉ Kev Server (bấm ⚙ để kiểm tra)";
            if (soKhung == 0)
                return "Không thấy khung trên layer " + layer;
            return null;
        }

        /// <summary>
        /// Chia lô KichThuocLo khung, chạy tối đa SongSong lô cùng lúc. Lô hỏng (mạng, API, phản
        /// hồi lạ) chỉ ghi vào LoiLo, các lô khác vẫn chạy. Cảnh báo đỏ đứng trước, rồi theo số thứ tự khung.
        /// </summary>
        public static async Task<KetQuaKiemTra> Chay(IList<KhungDuLieu> khung, string model, string loiDan, string khoiVatLieu,
                                                     IGuiYeuCau gui, IProgress<(int xong, int tong)> tienDo, CancellationToken ct,
                                                     CauHinhKev cauHinh = null)
        {
            CauHinhKev ch = cauHinh ?? CauHinhKev.DocTuBoNhoMacDinh();
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

            var tong = new KetQuaKiemTra { SoKhungGuiClaude = khung.Count };
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
            foreach (KhungDuLieu k in khung)
            {
                DacTrungKhung dt = KevApi.PhanTich(k, khoiVatLieu, ch);
                tong.NhanGiaCong[k.SoThuTu] = KevApi.GopNhanTheoCauHinh(dt, ch);
            }
            foreach (var cbLien in KevApi.KiemTraLienKhung(khung, khoiVatLieu, ch))
            {
                if (!tong.CanhBao.Any(x => x.Khung.SoThuTu == cbLien.Khung.SoThuTu && string.Equals(x.CB.LyDo, cbLien.CB.LyDo, StringComparison.Ordinal)))
                    tong.CanhBao.Add(cbLien);
            }
            tong.CanhBao = tong.CanhBao.OrderByDescending(x => x.CB.Do).ThenBy(x => x.Khung.SoThuTu).ToList();
            tong.TongTien = BangGia.Tien(model, tong.TongSuDung);
            return tong;
        }

        /// <summary>Chạy toàn bộ khung qua Kev Server (POST /v1/systemone): phân loại tấm gia công + chấm xác suất lỗi.</summary>
        public static async Task<(KetQuaKiemTra KetQua, List<KhungDuLieu> KhungNghiLoi)> ChayKev(
            IList<KhungDuLieu> khung, string khoiVatLieu, IGuiYeuCau guiKev,
            IProgress<(int xong, int tong)> tienDo, CancellationToken ct,
            CauHinhKev cauHinh = null)
        {
            CauHinhKev ch = cauHinh ?? CauHinhKev.DocTuBoNhoMacDinh();
            var kqTungKhung = new (KetQuaKhungKev Kq, string Body, string PhanHoi)[khung.Count];
            int xong = 0;
            var sem = new SemaphoreSlim(SongSongKev);

            async Task ChayMotKhung(int i)
            {
                KhungDuLieu k = khung[i];
                string body = KevApi.TaoBody(k, khoiVatLieu, ch);
                string phanHoi = null;
                KetQuaKhungKev kq;
                bool coSlot = false;
                try
                {
                    await sem.WaitAsync(ct).ConfigureAwait(false);
                    coSlot = true;
                    phanHoi = await guiKev.Gui(body, ct).ConfigureAwait(false);
                    kq = KevApi.DocPhanHoi(phanHoi, k, khoiVatLieu, ch);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    DacTrungKhung dt = KevApi.PhanTich(k, khoiVatLieu, ch);
                    kq = new KetQuaKhungKev
                    {
                        Khung = k.SoThuTu, Loi = "đã dừng",
                        NhanGiaCong = KevApi.GopNhanTheoCauHinh(dt, ch)
                    };
                }
                catch (Exception ex)
                {
                    DacTrungKhung dt = KevApi.PhanTich(k, khoiVatLieu, ch);
                    kq = new KetQuaKhungKev
                    {
                        Khung = k.SoThuTu,
                        Loi = "không kết nối được Kev Server (" + ex.GetBaseException().Message + ")",
                        NhanGiaCong = KevApi.GopNhanTheoCauHinh(dt, ch),
                        CanhBao = dt.CanhBaoCucBo,
                        NghiLoi = dt.CanhBaoCucBo.Count > 0
                    };
                }
                finally
                {
                    if (coSlot) sem.Release();
                }
                kqTungKhung[i] = (kq, body, phanHoi ?? "(" + kq.Loi + ")");
                tienDo?.Report((Interlocked.Increment(ref xong), khung.Count));
            }

            await Task.WhenAll(Enumerable.Range(0, khung.Count).Select(ChayMotKhung)).ConfigureAwait(false);

            var tong = new KetQuaKiemTra { SoKhungQuaKev = khung.Count };
            var nghiLoi = new List<KhungDuLieu>();
            string loiMangDau = null;

            for (int i = 0; i < khung.Count; i++)
            {
                KhungDuLieu k = khung[i];
                KetQuaKhungKev kq = kqTungKhung[i].Kq;
                tong.NhatKy.Add((kqTungKhung[i].Body, kqTungKhung[i].PhanHoi));
                tong.TongSuDung.Vao += kq.TokenVao;
                tong.TongSuDung.Ra += kq.TokenRa;
                tong.ThoiGianKevMs += kq.LatencyMs;
                tong.NhanGiaCong[k.SoThuTu] = kq.NhanGiaCong ?? ch.NhanMacDinh;

                if (kq.Loi != null && loiMangDau == null)
                    loiMangDau = kq.Loi;

                foreach (CanhBao cb in kq.CanhBao)
                    tong.CanhBao.Add((k, cb));

                if (kq.NghiLoi)
                    nghiLoi.Add(k);
            }

            foreach (var cbLien in KevApi.KiemTraLienKhung(khung, khoiVatLieu, ch))
            {
                if (!tong.CanhBao.Any(x => x.Khung.SoThuTu == cbLien.Khung.SoThuTu && string.Equals(x.CB.LyDo, cbLien.CB.LyDo, StringComparison.Ordinal)))
                    tong.CanhBao.Add(cbLien);
                if (!nghiLoi.Any(k => k.SoThuTu == cbLien.Khung.SoThuTu))
                    nghiLoi.Add(cbLien.Khung);
            }

            if (loiMangDau != null)
                tong.LoiLo.Add("Kev: " + loiMangDau + " — đã dùng bộ phân tích nội bộ dự phòng.");

            tong.CanhBao = tong.CanhBao.OrderByDescending(x => x.CB.Do).ThenBy(x => x.Khung.SoThuTu).ToList();
            return (tong, nghiLoi);
        }

        /// <summary>Chạy theo 1 trong 3 chế độ: Chỉ Kev, Hybrid (Kev lọc -> Claude giải thích), hoặc Chỉ Claude.</summary>
        public static async Task<KetQuaKiemTra> ChayTheoCheDo(
            string cheDo, IList<KhungDuLieu> khung, string model, string loiDan, string khoiVatLieu,
            IGuiYeuCau guiKev, IGuiYeuCau guiClaude, IProgress<(int xong, int tong)> tienDo, CancellationToken ct,
            CauHinhKev cauHinh = null)
        {
            CauHinhKev ch = cauHinh ?? CauHinhKev.DocTuBoNhoMacDinh();
            if (string.Equals(cheDo, CheDoChiClaude, StringComparison.Ordinal))
                return await Chay(khung, model, loiDan, khoiVatLieu, guiClaude, tienDo, ct, ch).ConfigureAwait(false);

            var (kqKev, khungNghi) = await ChayKev(khung, khoiVatLieu, guiKev, tienDo, ct, ch).ConfigureAwait(false);
            if (string.Equals(cheDo, CheDoChiKev, StringComparison.Ordinal) || guiClaude == null || khungNghi.Count == 0 || ct.IsCancellationRequested)
                return kqKev;

            KetQuaKiemTra kqClaude = await Chay(khungNghi, model, loiDan, khoiVatLieu, guiClaude, tienDo, ct, ch).ConfigureAwait(false);
            kqClaude.SoKhungQuaKev = khung.Count;
            kqClaude.SoKhungGuiClaude = khungNghi.Count;
            kqClaude.ThoiGianKevMs = kqKev.ThoiGianKevMs;
            foreach (var kv in kqKev.NhanGiaCong)
                kqClaude.NhanGiaCong[kv.Key] = kv.Value;
            foreach (string l in kqKev.LoiLo)
                kqClaude.LoiLo.Insert(0, l);
            kqClaude.NhatKy.InsertRange(0, kqKev.NhatKy);

            // Nếu lô Claude bị lỗi mạng hoặc sót cảnh báo cứng, giữ lại cảnh báo cục bộ từ bước Kev
            var khungDaCoClaude = new HashSet<int>(kqClaude.CanhBao.Select(x => x.Khung.SoThuTu));
            foreach (var cbKev in kqKev.CanhBao)
            {
                if (!khungDaCoClaude.Contains(cbKev.Khung.SoThuTu))
                    kqClaude.CanhBao.Add(cbKev);
            }
            kqClaude.CanhBao = kqClaude.CanhBao.OrderByDescending(x => x.CB.Do).ThenBy(x => x.Khung.SoThuTu).ToList();
            return kqClaude;
        }
    }
}
