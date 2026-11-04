using System.Collections.Generic;

namespace AiTool.Core
{
    public enum LoaiDoiTuong { Line, Arc, Circle, Polyline, Text, Dim }

    /// <summary>Một đoạn của polyline: thẳng, hoặc cung (bulge đã đổi sang tâm + bán kính).</summary>
    public sealed class DoanPl
    {
        public double X1, Y1, X2, Y2;
        public bool LaCung;
        public double Cx, Cy, R;

        public DoanPl Sao() => (DoanPl)MemberwiseClone();
    }

    /// <summary>
    /// Một đối tượng CAD đã đổi sang mô hình thuần (không phụ thuộc SDK CAD). Trường nào không
    /// áp dụng cho loại đối tượng thì để mặc định. Góc tính bằng độ.
    /// </summary>
    public sealed class DoiTuong
    {
        public LoaiDoiTuong Loai;
        public string Layer;
        /// <summary>Hộp bao (GeometricExtents) — tâm của nó quyết định đối tượng thuộc khung nào.</summary>
        public double MinX, MinY, MaxX, MaxY;
        /// <summary>Line: hai đầu. Dim: hai điểm đo. Text: X1,Y1 = điểm chèn.</summary>
        public double X1, Y1, X2, Y2;
        /// <summary>Arc / Circle.</summary>
        public double Cx, Cy, R, GocDau, GocCuoi;
        /// <summary>Polyline.</summary>
        public bool Kin;
        public List<DoanPl> Doan;
        /// <summary>Text / MText (MText giữ nội dung thô, DongChu làm sạch).</summary>
        public string NoiDung;
        public double ChieuCao;
        /// <summary>Dim.</summary>
        public string LoaiDim;
        public double GiaTriDo;
        public string GhiDe;

        public double TamX => (MinX + MaxX) / 2;
        public double TamY => (MinY + MaxY) / 2;
    }

    /// <summary>
    /// Một khung (= một tấm). MinX..MaxY là toạ độ GỐC bản vẽ (để zoom); các đối tượng bên
    /// trong đã đổi gốc về góc dưới trái khung.
    /// </summary>
    public sealed class KhungDuLieu
    {
        public int SoThuTu;
        public double MinX, MinY, MaxX, MaxY;
        public List<DoiTuong> DoiTuong = new List<DoiTuong>();
    }
}
