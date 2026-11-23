using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvTool.Core
{
    /// <summary>data\&lt;đơn&gt;-giavl.csv: giá tạm của vật liệu chưa có trong bảng giá, chỉ dùng cho đơn này.</summary>
    public sealed class GiaVlDon
    {
        public const string TieuDe = "Vat lieu,Do day,Gia VL,Gia cat,Gia uon,Gia mo lo,Gia tappu,Gia vat,Ty trong";

        public List<DongBangGia> Dong = new List<DongBangGia>();

        public DongBangGia Tim(string vatLieu, double doDay) =>
            Dong.FirstOrDefault(x => x.VatLieu == vatLieu && x.DoDay == doDay);

        public void Dat(DongBangGia d)
        {
            Xoa(d.VatLieu, d.DoDay);
            Dong.Add(d);
        }

        public void Xoa(string vatLieu, double doDay) => Dong.RemoveAll(x => x.VatLieu == vatLieu && x.DoDay == doDay);

        /// <summary>Bảng giá để tính: giá tạm đứng trước nên được ưu tiên, báo giá của đơn không tự đổi khi database đổi.</summary>
        public BangGia Gop(BangGia b) => new BangGia
        {
            CapNhat = b.CapNhat, Dong = Dong.Concat(b.Dong).ToList(), CongTyUuDai = b.CongTyUuDai, DacBiet = b.DacBiet, HeSoVL = b.HeSoVL,
            HeSoGiaCong = b.HeSoGiaCong, PitchiTrongLuong = b.PitchiTrongLuong, PitchiChieuDai = b.PitchiChieuDai, HeSoCuon = b.HeSoCuon,
        };

        /// <summary>Giá gia công gợi ý: đúng dòng trong bảng, không có thì SS400 có độ dày gần nhất, không có SS400 thì dòng gần nhất.</summary>
        public static DongBangGia GoiY(BangGia b, string vatLieu, double doDay)
        {
            var ds = b?.Dong ?? new List<DongBangGia>();
            DongBangGia g = ds.FirstOrDefault(x => x.VatLieu == vatLieu && x.DoDay == doDay)
                ?? ds.Where(x => x.VatLieu == "SS400").OrderBy(x => Math.Abs(x.DoDay - doDay)).FirstOrDefault()
                ?? ds.OrderBy(x => Math.Abs(x.DoDay - doDay)).FirstOrDefault();
            return new DongBangGia
            {
                VatLieu = vatLieu, DoDay = doDay, GiaCat = g?.GiaCat ?? 0, GiaUon = g?.GiaUon ?? 0, GiaMoLo = g?.GiaMoLo ?? 0,
                GiaTappu = g?.GiaTappu ?? 0, GiaVat = g?.GiaVat ?? 0, TyTrong = g != null && g.TyTrong > 0 ? g.TyTrong : 7.85,
            };
        }

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
            foreach (string dong in noiDung.TrimStart('﻿').Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (dong == TieuDe || dong.Trim() == "") continue;
                List<string> o = CsvXuat.TachO(dong);
                if (o.Count < 9 || o[0] == "") throw new FormatException("Dòng giá vật liệu sai cột: " + dong);
                double D(int i) => double.Parse(o[i], NumberStyles.Float, CultureInfo.InvariantCulture);
                kq.Dat(new DongBangGia
                {
                    VatLieu = o[0], DoDay = D(1), DonGia = D(2), GiaCat = D(3), GiaUon = D(4), GiaMoLo = D(5), GiaTappu = D(6), GiaVat = D(7), TyTrong = D(8),
                });
            }
            return kq;
        }

        /// <summary>Ghi ra file tạm rồi thay (UTF-8 BOM, CRLF); file đang bị giữ thì ném IOException, file cũ còn nguyên.</summary>
        public void GhiFile(string file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var sb = new StringBuilder(TieuDe + "\r\n");
            foreach (DongBangGia d in Dong)
                sb.Append(string.Join(",", CsvXuat.O(d.VatLieu), So(d.DoDay), So(d.DonGia), So(d.GiaCat), So(d.GiaUon), So(d.GiaMoLo), So(d.GiaTappu), So(d.GiaVat), So(d.TyTrong))).Append("\r\n");
            string tam = file + ".tmp";
            File.WriteAllText(tam, sb.ToString(), new UTF8Encoding(true));
            try { File.Copy(tam, file, true); }
            finally { File.Delete(tam); }
        }
    }
}
