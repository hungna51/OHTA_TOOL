using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CsvTool.Core;

namespace CsvTool.CadServices
{
    /// <summary>
    /// Đọc bảng giá thẳng từ database; đọc được thì lưu bản sao mã hoá (DPAPI, theo tài khoản Windows) ngoài OneDrive,
    /// mất mạng thì dùng bản sao và báo ngày cập nhật.
    /// </summary>
    public sealed class NguonGiaCoDem : INguonBangGia
    {
        private static readonly byte[] Muoi = System.Text.Encoding.ASCII.GetBytes("OHTA-CSV-BANGGIA");
        private static string FileDem => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "csv_bang_gia.bin");

        /// <summary>Lần đọc gần nhất phải dùng bản sao thì ghi lý do; đọc thẳng được thì null.</summary>
        public string GhiChu { get; private set; }

        public BangGia Doc()
        {
            GhiChu = null;
            string chuoi = CaiDatRegistry.DocKetNoi();
            if (chuoi == null) return null;
            BangGia b;
            try { b = new NguonPostgres(chuoi).Doc(); }
            catch (Exception ex) when (ex is IOException || ex is System.Net.Sockets.SocketException)
            {
                BangGia dem = DocDem();
                if (dem == null) throw;
                if (dem.GiaToiThieu <= 0 || dem.BuToiDa <= 0) throw new InvalidOperationException(BangGia.LoiThieuCaiDat);   // bản sao từ trước khi có CaiDatGia
                GhiChu = "Không nối được database, đang dùng bảng giá lúc " + dem.CapNhat.ToString("dd/MM HH:mm") + ".";
                return dem;
            }
            try { LuuDem(b); } catch (Exception) { }   // không lưu được bản sao vẫn tính được bằng giá vừa đọc
            return b;
        }

        private static void LuuDem(BangGia b)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FileDem));
            File.WriteAllBytes(FileDem, ProtectedData.Protect(b.ThanhByte(), Muoi, DataProtectionScope.CurrentUser));
        }

        private static BangGia DocDem()
        {
            try { return File.Exists(FileDem) ? BangGia.TuByte(ProtectedData.Unprotect(File.ReadAllBytes(FileDem), Muoi, DataProtectionScope.CurrentUser)) : null; }
            catch (Exception) { return null; }
        }

        /// <summary>Bản sao bảng giá, để hiện danh sách công ty ngay không phải chờ mạng.</summary>
        public static BangGia BangDem() => DocDem();

        public static void XoaDem()
        {
            try { File.Delete(FileDem); } catch (Exception) { }
        }
    }
}
