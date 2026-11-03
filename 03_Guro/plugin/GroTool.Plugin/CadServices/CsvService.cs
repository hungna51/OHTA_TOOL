using System;
using System.IO;
#if ZWCAD
using ZwSoft.ZwCAD.ApplicationServices;
#else
using Autodesk.AutoCAD.ApplicationServices;
#endif
using ZwCadPlugin.Core;

namespace ZwCadPlugin.CadServices
{
    /// <summary>Vị trí và đọc file CSV của bản vẽ hiện hành — cùng file mà lệnh GRO ghi.</summary>
    public static class CsvService
    {
        /// <summary>DWGPREFIX + tên đơn + ".csv", y như c:gro.</summary>
        public static string DuongDan()
        {
            string thuMuc = Application.GetSystemVariable("DWGPREFIX") as string ?? string.Empty;
            return Path.Combine(thuMuc, TenDonBanVe() + ".csv");
        }

        /// <summary>Tên đơn của bản vẽ hiện hành (biến `don` của c:gro) — tiền tố cột N / X của CSV.</summary>
        public static string TenDonBanVe()
        {
            string ten = Application.GetSystemVariable("DWGNAME") as string ?? string.Empty;
            return CsvGro.TenDon(ten);
        }

        /// <summary>
        /// Đọc để HIỂN THỊ (cột GC, đối chiếu Q). Cho phép người khác đang mở file
        /// (FileShare.ReadWrite) — Excel mở CSV vẫn đọc được nếu Excel không khoá đọc.
        /// </summary>
        public static CsvGro TryDoc(string duongDan, out string loi)
        {
            loi = null;
            if (!File.Exists(duongDan))
            {
                loi = "không thấy file CSV";
                return null;
            }
            try
            {
                using (var fs = new FileStream(duongDan, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    return CsvGro.Doc(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                loi = "không đọc được CSV (" + ex.Message + ")";
                return null;
            }
        }

        /// <summary>
        /// Mở CSV ĐỘC QUYỀN để ghi. Mở TRƯỚC khi sửa bản vẽ: Excel đang giữ file thì báo
        /// ngay và không sửa gì — tránh cảnh bản vẽ đã đổi mà CSV thì không (mục 27).
        /// </summary>
        public static FileStream MoDeGhi(string duongDan)
        {
            return new FileStream(duongDan, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }

        public static byte[] DocHet(FileStream fs)
        {
            fs.Position = 0;
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static void GhiDe(FileStream fs, byte[] data)
        {
            fs.Position = 0;
            fs.SetLength(0);
            fs.Write(data, 0, data.Length);
            fs.Flush(true);
        }
    }
}
