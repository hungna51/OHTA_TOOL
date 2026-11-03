namespace ZwCadPlugin.Models
{
    /// <summary>
    /// Cài đặt chung của lệnh GRO (mục 194) — lưu registry, dùng cho MỌI bản vẽ. Layer
    /// khung bao / vùng / gốc KHÔNG nằm ở đây: chúng chọn riêng từng bản vẽ trên tab Guro.
    /// </summary>
    public sealed class GroSettings
    {
        public const string KhungTamMacDinh = "Khung_IN";

        public bool UseVung { get; set; }                                // use_vung
        public bool UseTaoKhung { get; set; } = true;                    // use_tao_khung
        public string LayerKhungTam { get; set; } = KhungTamMacDinh;     // layer_khung_tam
        public bool UseInPdf { get; set; }                               // use_in_pdf
        public bool XepCum { get; set; } = true;                         // use_xep_cum
        public bool DatMk { get; set; } = true;                          // use_dat_mk
        public bool UseXln { get; set; } = true;                         // use_xln
        public bool AutoDim { get; set; } = true;                        // auto_dim: tắt = không xử lý nét Dim

        public GroSettings Sao() => (GroSettings)MemberwiseClone();
    }
}
