using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CsvTool.Core
{
    /// <summary>Một dòng bảng giá theo vật liệu + độ dày (bảng BangGiaVL của price-calc).</summary>
    [DataContract]
    public sealed class DongBangGia
    {
        [DataMember] public string VatLieu;
        [DataMember] public double DoDay, DonGia, GiaUon, GiaCat, GiaMoLo, GiaTappu, GiaVat, TyTrong = 7.85;
    }

    /// <summary>Công ty tính theo khối lượng (bảng CongTyDacBiet).</summary>
    [DataContract]
    public sealed class CongTyDacBiet
    {
        [DataMember] public double DonGiaTamDon, DonGiaTamGiaCong;
        [DataMember] public bool SoSanhGiaThuong;
    }

    /// <summary>Toàn bộ dữ liệu giá cần để tính. Repo public: chỉ có khuôn, không chứa giá thật.</summary>
    [DataContract]
    public sealed class BangGia
    {
        [DataMember] public DateTime CapNhat;
        [DataMember] public List<DongBangGia> Dong = new List<DongBangGia>();
        [DataMember] public Dictionary<string, bool> CongTyUuDai = new Dictionary<string, bool>();        // tên → có ưu đãi
        [DataMember] public Dictionary<string, CongTyDacBiet> DacBiet = new Dictionary<string, CongTyDacBiet>();
        [DataMember] public Dictionary<string, double> HeSoVL = new Dictionary<string, double>();         // "Ưu đãi SS", "Không ưu đãi SUS"...
        [DataMember] public Dictionary<string, double> HeSoGiaCong = new Dictionary<string, double>();    // Pitchi, Non, NonLech, Kakumaru...
        [DataMember] public List<double> PitchiTrongLuong = new List<double>(), PitchiChieuDai = new List<double>();   // theo thuTu
        [DataMember] public List<double> HeSoCuon = new List<double>();

        private static DataContractJsonSerializer BoDoc() =>
            new DataContractJsonSerializer(typeof(BangGia), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

        public byte[] ThanhByte()
        {
            using (var m = new MemoryStream()) { BoDoc().WriteObject(m, this); return m.ToArray(); }
        }

        public static BangGia TuByte(byte[] b)
        {
            using (var m = new MemoryStream(b)) return (BangGia)BoDoc().ReadObject(m);
        }
    }

    /// <summary>Nơi lấy bảng giá. Trả null khi chưa cấu hình nguồn.</summary>
    public interface INguonBangGia
    {
        BangGia Doc();
    }
}
