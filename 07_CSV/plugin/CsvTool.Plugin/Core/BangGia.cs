using System;
using System.Collections.Generic;

namespace CsvTool.Core
{
    /// <summary>Một dòng bảng giá theo vật liệu + độ dày (bảng BangGiaVL của price-calc).</summary>
    public sealed class DongBangGia
    {
        public string VatLieu;
        public double DoDay, DonGia, GiaUon, GiaCat, GiaMoLo, GiaTappu, GiaVat, TyTrong = 7.85;
    }

    /// <summary>Công ty tính theo khối lượng (bảng CongTyDacBiet).</summary>
    public sealed class CongTyDacBiet
    {
        public double DonGiaTamDon, DonGiaTamGiaCong;
        public bool SoSanhGiaThuong;
    }

    /// <summary>Toàn bộ dữ liệu giá cần để tính. Repo public: chỉ có khuôn, không chứa giá thật.</summary>
    public sealed class BangGia
    {
        public List<DongBangGia> Dong = new List<DongBangGia>();
        public Dictionary<string, bool> CongTyUuDai = new Dictionary<string, bool>();        // tên → có ưu đãi
        public Dictionary<string, CongTyDacBiet> DacBiet = new Dictionary<string, CongTyDacBiet>();
        public Dictionary<string, double> HeSoVL = new Dictionary<string, double>();         // "Ưu đãi SS", "Không ưu đãi SUS"...
        public Dictionary<string, double> HeSoGiaCong = new Dictionary<string, double>();    // Pitchi, Non, NonLech, Kakumaru...
        public List<double> PitchiTrongLuong = new List<double>(), PitchiChieuDai = new List<double>();   // theo thuTu
        public List<double> HeSoCuon = new List<double>();
    }

    /// <summary>Nơi lấy bảng giá. Trả null khi chưa có nguồn.</summary>
    public interface INguonBangGia
    {
        BangGia Doc();
    }

    /// <summary>shortcut: chưa chốt nguồn bảng giá nên chưa tính được, thay lớp này khi chọn xong nguồn.</summary>
    public sealed class NguonChuaKetNoi : INguonBangGia
    {
        public BangGia Doc() => null;
    }
}
