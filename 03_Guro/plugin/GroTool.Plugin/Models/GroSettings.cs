namespace ZwCadPlugin.Models
{
    /// <summary>
    /// Chín giá trị của dialog gro — ĐÚNG thứ tự mà lisp gro-get-input trả về và c:gro
    /// đọc bằng (nth 0 dlg) ... (nth 8 dlg). Đổi thứ tự ở đây là lệch với lisp.
    /// </summary>
    public sealed class GroSettings
    {
        public bool UseVung { get; set; }          // 0  use_vung
        public string LayerVung { get; set; }      // 1  layer_vung
        public string LayerKhung { get; set; }     // 2  layer_khung
        public bool UseInPdf { get; set; }         // 3  use_in_pdf
        public bool UseXln { get; set; }           // 4  use_xln
        public string LayerXln { get; set; }       // 5  layer_xln
        public bool AutoDim { get; set; }          // 6  auto_dim
        public int MauDim { get; set; } = 4;       // 7  mau_dim (ACI)
        public bool UseTaoKhung { get; set; }      // 8  use_tao_khung
        // Muc 122 - noi tiep SAU 9 gia tri cu (DCL du phong chi tra 9, lisp giu gia tri da luu)
        public bool XepCum { get; set; } = true;   // 9  use_xep_cum: xep cum sang cot phai + tach mirror
        public bool DatMk { get; set; } = true;    // 10 use_dat_mk : dat ma khac + F-name vao trong hinh
        // Muc 170: bo "che do ma khac" (nth 11) - chi con che do 2 (truot goc tren-trai), chay bang C#.
    }
}
