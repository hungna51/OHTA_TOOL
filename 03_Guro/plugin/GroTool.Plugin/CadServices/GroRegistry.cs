using System;
using WinReg = Microsoft.Win32;
using ZwCadPlugin.Core;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.CadServices
{
    /// <summary>
    /// Thiết lập lệnh GRO trong HKCU\Software\OFT_Lisp\GRO — đúng khóa, định dạng
    /// ("1"/"0", số dạng chuỗi) và mặc định như gro--reg-read-* / gro--reg-write-* của lisp.
    /// Tab Guro (mục 192) ghi từng giá trị ngay khi người dùng đổi; c:gro đọc lại mỗi lần chạy.
    /// </summary>
    public static class GroRegistry
    {
        public const string Khoa = @"Software\OFT_Lisp\GRO";

        public static GroSettings Doc(out bool xlnTuDong)
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
                int.TryParse(DocChuoi("mau_dim"), out int mau);
                xlnTuDong = Co("xln_tu_dong", true);
                return new GroSettings
                {
                    UseInPdf = Co("use_in_pdf", false),
                    UseTaoKhung = Co("use_tao_khung", true),
                    UseVung = Co("use_vung", false),
                    LayerVung = DocChuoi("layer_vung"),
                    LayerKhung = DocChuoi("layer_khung"),
                    UseXln = Co("use_xln", true),
                    LayerXln = DocChuoi("layer_xln"),
                    AutoDim = Co("auto_dim", true),
                    MauDim = GroThamSo.LaMauHopLe(mau) ? mau : GroThamSo.MauMacDinh,
                    XepCum = Co("use_xep_cum", true),
                    DatMk = Co("use_dat_mk", true),
                };
            }
        }

        public static void Ghi(string ten, bool giaTri) => Ghi(ten, giaTri ? "1" : "0");

        public static void Ghi(string ten, int giaTri) => Ghi(ten, giaTri.ToString());

        public static void Ghi(string ten, string giaTri)
        {
            using (WinReg.RegistryKey k = WinReg.Registry.CurrentUser.CreateSubKey(Khoa))
            {
                k?.SetValue(ten, giaTri ?? "", WinReg.RegistryValueKind.String);
            }
        }
    }
}
