using WinReg = Microsoft.Win32;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Cài đặt chung của lệnh GRO trong HKCU\Software\OFT_Lisp\GRO — đúng khóa, định dạng
    /// ("1"/"0") và mặc định như gro--reg-read-* của lisp. Cửa sổ ⚙ Cài đặt GURO ghi khi bấm
    /// OK; c:grochay đọc lại mỗi lần chạy.
    /// </summary>
    public static class GroRegistry
    {
        public const string Khoa = @"Software\OFT_Lisp\GRO";

        public static GroSettings Doc()
        {
            using (WinReg.RegistryKey k = WinReg.Registry.CurrentUser.OpenSubKey(Khoa))
            {
                string DocChuoi(string ten)
                {
                    string v = k?.GetValue(ten) as string;
                    return string.IsNullOrEmpty(v) ? null : v;
                }
                bool Co(string ten, bool macDinh)
                {
                    string v = DocChuoi(ten);
                    return v == "1" ? true : v == "0" ? false : macDinh;
                }
                return new GroSettings
                {
                    UseVung = Co("use_vung", false),
                    UseTaoKhung = Co("use_tao_khung", true),
                    LayerKhungTam = TenKhungTam.ChuanHoa(DocChuoi("layer_khung_tam")),
                    UseInPdf = Co("use_in_pdf", false),
                    XepCum = Co("use_xep_cum", true),
                    DatMk = Co("use_dat_mk", true),
                    UseXln = Co("use_xln", true),
                    AutoDim = Co("auto_dim", true),
                };
            }
        }

        /// <summary>Lưu cài đặt của cửa sổ ⚙ (không gồm use_vung — ô đó ghi ngay trên tab).</summary>
        public static void GhiCaiDat(GroSettings s)
        {
            Ghi("use_tao_khung", s.UseTaoKhung);
            Ghi("layer_khung_tam", TenKhungTam.ChuanHoa(s.LayerKhungTam));
            Ghi("use_in_pdf", s.UseInPdf);
            Ghi("use_xep_cum", s.XepCum);
            Ghi("use_dat_mk", s.DatMk);
            Ghi("use_xln", s.UseXln);
            Ghi("auto_dim", s.AutoDim);
        }

        /// <summary>Tên layer khung tấm (mục 194), mặc định Khung_IN.</summary>
        public static string LayerKhungTam()
        {
            try { return Doc().LayerKhungTam; }
            catch { return GroSettings.KhungTamMacDinh; }
        }

        public static void Ghi(string ten, bool giaTri) => Ghi(ten, giaTri ? "1" : "0");

        public static void Ghi(string ten, string giaTri)
        {
            using (WinReg.RegistryKey k = WinReg.Registry.CurrentUser.CreateSubKey(Khoa))
            {
                k?.SetValue(ten, giaTri ?? "", WinReg.RegistryValueKind.String);
            }
        }
    }
}
