using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvTool.Core
{
    /// <summary>data\&lt;đơn&gt;-giavl.csv: giá vật liệu tạm của đơn. Thông số gia công luôn lấy từ database, không lưu ở đây.</summary>
    public sealed class GiaVlDon
    {
        public const string TieuDe = "Vat lieu,Do day,Gia VL";

        public List<DongBangGia> Dong = new List<DongBangGia>();

        public DongBangGia Tim(string vatLieu, double doDay) =>
            Dong.FirstOrDefault(x => x.VatLieu == vatLieu && x.DoDay == doDay);

        public void Dat(DongBangGia d)
        {
            Xoa(d.VatLieu, d.DoDay);
            Dong.Add(d);
        }

        public void Xoa(string vatLieu, double doDay) => Dong.RemoveAll(x => x.VatLieu == vatLieu && x.DoDay == doDay);

        /// <summary>
        /// Bảng giá để tính: dòng có giá tạm chỉ thay đơn giá vật liệu, mọi thông số khác giữ của database.
        /// Vật liệu + độ dày không có trong database thì giá tạm không có tác dụng.
        /// </summary>
        public BangGia Gop(BangGia b) => new BangGia
        {
            CapNhat = b.CapNhat, CongTyUuDai = b.CongTyUuDai, DacBiet = b.DacBiet, HeSoVL = b.HeSoVL,
            HeSoGiaCong = b.HeSoGiaCong, PitchiTrongLuong = b.PitchiTrongLuong, PitchiChieuDai = b.PitchiChieuDai, HeSoCuon = b.HeSoCuon,
            Dong = b.Dong.Select(x =>
            {
                DongBangGia t = Tim(x.VatLieu, x.DoDay);
                return t == null ? x : new DongBangGia
                {
                    VatLieu = x.VatLieu, DoDay = x.DoDay, DonGia = t.DonGia, GiaUon = x.GiaUon, GiaCat = x.GiaCat,
                    GiaMoLo = x.GiaMoLo, GiaTappu = x.GiaTappu, GiaVat = x.GiaVat, TyTrong = x.TyTrong,
                };
            }).ToList(),
        };

        /// <summary>Các cặp vật liệu + độ dày trong đơn và số dòng tấm của mỗi cặp. Độ dày không phải số thì bỏ qua.</summary>
        public static List<(string VatLieu, double DoDay, int SoTam)> DsVatLieu(IEnumerable<TamXuat> ds) =>
            ds.Select(t => (t.M, Ok: double.TryParse((t.T ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d), Day: d))
              .Where(x => x.Ok && !string.IsNullOrEmpty(x.M))
              .GroupBy(x => (x.M, x.Day))
              .Select(g => (g.Key.M, g.Key.Day, g.Count()))
              .OrderBy(x => x.M, StringComparer.Ordinal).ThenBy(x => x.Day)
              .ToList();

        private static string So(double d) => d.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Chưa có file → trống. File hỏng → ném lỗi để không ghi đè mất giá đã nhập.</summary>
        public static GiaVlDon DocFile(string file)
        {
            var kq = new GiaVlDon();
            if (!File.Exists(file)) return kq;
            string noiDung;
            using (var s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var r = new StreamReader(s, Encoding.UTF8)) noiDung = r.ReadToEnd();
            foreach (string dong in noiDung.TrimStart('\uFEFF').Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                // file của bản trước có thêm cột gia công: bỏ qua các cột đó
                if (dong.StartsWith("Vat lieu,Do day,Gia VL") || dong.Trim() == "") continue;
                List<string> o = CsvXuat.TachO(dong);
                if (o.Count < 3 || o[0] == "") throw new FormatException("Dòng giá vật liệu sai cột: " + dong);
                double day = double.Parse(o[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                double gia = double.Parse(o[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                if (!(gia > 0) || double.IsInfinity(gia)) throw new FormatException("Giá vật liệu không hợp lệ: " + dong);
                kq.Dat(new DongBangGia { VatLieu = o[0], DoDay = day, DonGia = gia });
            }
            return kq;
        }

        /// <summary>Ghi ra file tạm rồi thay (UTF-8 BOM, CRLF); file đang bị giữ thì ném IOException, file cũ còn nguyên.</summary>
        public void GhiFile(string file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var sb = new StringBuilder(TieuDe + "\r\n");
            foreach (DongBangGia d in Dong)
                sb.Append(string.Join(",", CsvXuat.O(d.VatLieu), So(d.DoDay), So(d.DonGia))).Append("\r\n");
            string tam = file + ".tmp";
            File.WriteAllText(tam, sb.ToString(), new UTF8Encoding(true));
            try { File.Copy(tam, file, true); }
            finally { File.Delete(tam); }
        }
    }
}
