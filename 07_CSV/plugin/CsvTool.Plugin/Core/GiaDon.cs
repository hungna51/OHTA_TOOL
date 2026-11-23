using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CsvTool.Core
{
    /// <summary>Thông tin tính giá của 1 tấm mà 35 cột data CSV không có chỗ lưu.</summary>
    [DataContract]
    public sealed class GiaTam
    {
        [DataMember] public bool TuDong;          // giá hiện tại do tool tính; false + có giá = giá sửa tay
        [DataMember] public string LoaiPitchi = "Pitchi";
        [DataMember] public double DonGiaCuon;    // 0 = theo r1/r2, âm = chọn Không, dương = đơn giá đã chọn
        [DataMember] public bool LamTron250;
        [DataMember] public bool TatTron250;      // tắt riêng tấm này khi cả đơn đang bật làm tròn 250
        [DataMember] public bool SuaBe;           // đường bẻ sửa tay: quét lại không đè
        [DataMember] public bool CheDoRieng;      // chế độ tính chọn riêng: quét lại, đổi chế độ đơn không đè
        [DataMember] public bool SuaSara;         // số lỗ Sara sửa tay: quét lại không đè
        public double TienVlHienTai;              // tiền vật liệu 1 tấm của lần tính gần nhất, chỉ để hiện, không lưu
    }

    /// <summary>data\&lt;đơn&gt;-gia.json: công ty + thông tin giá từng tấm, khoá theo tên N.</summary>
    [DataContract]
    public sealed class GiaDon
    {
        [DataMember] public string CongTy = "";
        [DataMember] public bool KhongTinhSauQuet;   // mặc định có tính giá sau quét
        [DataMember] public int? CheDoTinh;
        [DataMember] public bool LamTron250;         // giá dưới 250 tính thành 250 cho cả đơn
        [DataMember] public Dictionary<string, GiaTam> Tam = new Dictionary<string, GiaTam>();

        public GiaTam Lay(string ten)
        {
            if (!Tam.TryGetValue(ten, out GiaTam g)) Tam[ten] = g = new GiaTam();
            return g;
        }

        public bool Tron250(GiaTam g) => g?.TatTron250 != true && (LamTron250 || g?.LamTron250 == true);

        /// <summary>Quét lại: giữ đường bẻ và chế độ tính người dùng đã sửa riêng cho tấm.</summary>
        public void GiuSuaTay(string ten, ThongTinTam moi, ThongTinTam cu)
        {
            if (moi == cu || !Tam.TryGetValue(ten, out GiaTam g)) return;
            // tấm đã rời danh sách rồi quét lại: không còn số sửa tay để giữ, bỏ dấu cũ
            if (cu == null) { g.SuaBe = false; g.CheDoRieng = false; g.SuaSara = false; return; }
            if (g.SuaBe) moi.DuongBe = cu.DuongBe;
            if (g.CheDoRieng) moi.CheDoTinh = cu.CheDoTinh;
            if (g.SuaSara) moi.Sara = cu.Sara;
        }

        public void DoiTen(string cu, string moi)
        {
            // tên mới đang thuộc tấm khác: không đè mất cờ giá tay của tấm đó
            if (Tam.ContainsKey(moi) || !Tam.TryGetValue(cu, out GiaTam g)) return;
            Tam.Remove(cu);
            Tam[moi] = g;
        }

        private static DataContractJsonSerializer BoDoc() =>
            new DataContractJsonSerializer(typeof(GiaDon), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

        /// <summary>Chưa có file → đơn trống. File hỏng → ném lỗi để không ghi đè mất giá tay.</summary>
        public static GiaDon DocFile(string file)
        {
            if (!File.Exists(file)) return new GiaDon();
            using (var s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                var d = (GiaDon)BoDoc().ReadObject(s);
                d.CongTy = d.CongTy ?? "";
                d.Tam = d.Tam ?? new Dictionary<string, GiaTam>();
                foreach (GiaTam g in d.Tam.Values) g.LoaiPitchi = g.LoaiPitchi ?? "Pitchi";
                return d;
            }
        }

        /// <summary>Ghi ra file tạm rồi thay, hỏng giữa chừng thì file cũ còn nguyên.</summary>
        public void GhiFile(string file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            string tam = file + ".tmp";
            using (var s = new FileStream(tam, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var w = JsonReaderWriterFactory.CreateJsonWriter(s, new UTF8Encoding(false), false, true))
                BoDoc().WriteObject(w, this);
            File.Copy(tam, file, true);
            File.Delete(tam);
        }
    }
}
