using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsvTool.Core
{
    /// <summary>Một công ty trong danh sách chọn, kèm thông tin để hiện tag như web tính tiền.</summary>
    public sealed class MucCongTy
    {
        public string Ten { get; set; } = "";
        public string TiengNhat { get; set; } = "";
        public bool UuDai { get; set; }
        public CongTyDacBiet DacBiet { get; set; }

        public bool CoTiengNhat => TiengNhat != "";
        public bool LaDacBiet => DacBiet != null;
        public string NhanTamDon => DacBiet == null ? "" : "Tấm đơn: ¥" + DacBiet.DonGiaTamDon.ToString("0.##", CultureInfo.InvariantCulture) + "/kg";
        public string NhanTamBe => DacBiet == null ? "" : "Tấm bẻ: ¥" + DacBiet.DonGiaTamGiaCong.ToString("0.##", CultureInfo.InvariantCulture) + "/kg";
    }

    /// <summary>Tìm công ty theo tên phiên âm hoặc tên tiếng Nhật, kể cả tên dán vào có 株式会社, ㈱, 御中.</summary>
    public static class CongTyTim
    {
        private static readonly Regex RxBo = new Regex("株式会社|有限会社|合同会社|\\(株\\)|\\(有\\)|\\(同\\)|御中|様$|[\\s・,.\\-_'’]", RegexOptions.Compiled);

        /// <summary>Đưa về dạng so sánh: toàn/nửa độ rộng như nhau, không phân biệt hoa thường, bỏ loại hình công ty.</summary>
        public static string Chuan(string s) => RxBo.Replace((s ?? "").Normalize(NormalizationForm.FormKC).Trim(), "").ToLowerInvariant();

        public static List<MucCongTy> Ds(BangGia b)
        {
            if (b == null) return new List<MucCongTy>();
            return b.CongTyUuDai.OrderBy(x => x.Key, StringComparer.CurrentCulture).Select(x =>
            {
                string nhat = null;
                b.TenNhat?.TryGetValue(x.Key, out nhat);
                b.DacBiet.TryGetValue(x.Key, out CongTyDacBiet db);
                return new MucCongTy { Ten = x.Key, TiengNhat = (nhat ?? "").Trim(), UuDai = x.Value, DacBiet = db };
            }).ToList();
        }

        private static bool Khop(string ten, string q) => ten != "" && (ten.Contains(q) || (ten.Length >= 2 && q.Contains(ten)));

        /// <summary>Các công ty khớp từ khoá, khớp đúng cả tên đứng đầu. Từ khoá dài hơn tên (có thêm chi nhánh...) vẫn khớp.</summary>
        public static List<MucCongTy> Loc(IEnumerable<MucCongTy> ds, string tim)
        {
            string q = Chuan(tim);
            if (q == "") return ds.ToList();
            return ds.Where(m => Khop(Chuan(m.Ten), q) || Khop(Chuan(m.TiengNhat), q))
                     .OrderBy(m => Chuan(m.Ten) == q || Chuan(m.TiengNhat) == q ? 0 : 1).ToList();
        }

        /// <summary>Công ty duy nhất ứng với chữ đã nhập: khớp đúng tên, hoặc chỉ có 1 kết quả. Không chắc → null.</summary>
        public static MucCongTy TimDung(IEnumerable<MucCongTy> ds, string tim)
        {
            string q = Chuan(tim);
            if (q == "") return null;
            var kq = Loc(ds, tim);
            return kq.FirstOrDefault(m => Chuan(m.Ten) == q || Chuan(m.TiengNhat) == q) ?? (kq.Count == 1 ? kq[0] : null);
        }
    }
}
