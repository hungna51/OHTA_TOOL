using System;
using System.Collections.Generic;
using System.Globalization;
using ZwCadPlugin.Models;

namespace ZwCadPlugin.Core
{
    /// <summary>
    /// Chuyển đổi giữa giá trị lisp (đã đổi sang object .NET) và GroSettings cho hàm
    /// (gro_net_dialog ...). Không phụ thuộc ZWCAD: lớp CAD chỉ đổi TypedValue sang
    /// object rồi gọi vào đây.
    ///
    /// Quy ước object: chuỗi = string, T = true, nil = null, số nguyên = int, số thực = double.
    ///
    /// CHÚ Ý hai thứ tự KHÁC NHAU (giữ đúng như lisp):
    ///   tham số  (gro-get-input): lay-k vung layer-vung in-pdf use-xln lay-xln auto-dim mau-dim tao-khung [giu-xln]
    ///                             [xep-cum dat-mk]                      (mục 122, vị trí 10..11)
    ///   kết quả  (nth 0..10 dlg): vung layer-vung lay-k in-pdf use-xln lay-xln auto-dim mau-dim tao-khung
    ///                             xep-cum dat-mk                        (mục 122, vị trí 9..10)
    /// Mục 170: bỏ "chế độ mã khắc" (tham số 12 / kết quả 11) — chỉ còn chế độ 2, chạy bằng C#.
    /// Lisp cũ còn gửi tham số 12 thì bị bỏ qua.
    /// </summary>
    public static class GroThamSo
    {
        /// <summary>Phần tử đầu của danh sách trả về khi người dùng bấm "Xem DSN".</summary>
        public const string NhanXemDsn = "XLN_DS";
        /// <summary>Trả về khi dialog .NET hỏng — lisp chuyển sang DCL.</summary>
        public const string NhanLoi = "LOI";

        public const int MauMacDinh = 4;

        public static GroSettings DocThamSo(IList<object> a, out bool giuXln)
        {
            object Lay(int i) => (a != null && i < a.Count) ? a[i] : null;

            giuXln = LaDung(Lay(9));
            // Mục 122: lisp cũ không truyền 3 giá trị này -> THIẾU thì lấy mặc định (khác nil = tắt)
            bool Co(int i, bool macDinh) => (a != null && i < a.Count) ? LaDung(a[i]) : macDinh;
            return new GroSettings
            {
                XepCum = Co(10, true),
                DatMk = Co(11, true),
                LayerKhung = LayChuoi(Lay(0)),
                UseVung = LaDung(Lay(1)),
                LayerVung = LayChuoi(Lay(2)),
                UseInPdf = LaDung(Lay(3)),
                UseXln = LaDung(Lay(4)),
                LayerXln = LayChuoi(Lay(5)),
                AutoDim = LaDung(Lay(6)),
                MauDim = LayMau(Lay(7)),
                UseTaoKhung = LaDung(Lay(8)),
            };
        }

        /// <summary>11 giá trị theo ĐÚNG thứ tự c:gro đọc bằng (nth 0..10 dlg).</summary>
        public static object[] KetQua(GroSettings s)
        {
            return new object[]
            {
                s.UseVung,
                RongThanhNull(s.LayerVung),
                RongThanhNull(s.LayerKhung),
                s.UseInPdf,
                s.UseXln,
                RongThanhNull(s.LayerXln),
                s.AutoDim,
                LaMauHopLe(s.MauDim) ? s.MauDim : MauMacDinh,
                s.UseTaoKhung,
                s.XepCum,
                s.DatMk,
            };
        }

        /// <summary>
        /// "Đúng" theo lisp: mọi thứ khác nil là T. Riêng chuỗi "0"/"" coi là sai, vì
        /// registry lưu cờ dạng "1"/"0" — phòng khi có nơi truyền nguyên chuỗi sang.
        /// </summary>
        public static bool LaDung(object o)
        {
            switch (o)
            {
                case null: return false;
                case bool b: return b;
                case string s: return s.Length > 0 && s != "0";
                case int i: return i != 0;
                case short h: return h != 0;
                case double d: return d != 0.0;
                default: return true;
            }
        }

        public static bool LaMauHopLe(int aci) => aci >= 1 && aci <= 255;

        private static int LayMau(object o)
        {
            int v;
            switch (o)
            {
                case int i: v = i; break;
                case short h: v = h; break;
                case double d: v = (int)Math.Round(d); break;
                case string s when int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p): v = p; break;
                default: v = MauMacDinh; break;
            }
            return LaMauHopLe(v) ? v : MauMacDinh;
        }

        private static string LayChuoi(object o) => RongThanhNull(o as string);

        private static string RongThanhNull(string s) => string.IsNullOrEmpty(s) ? null : s;
    }
}
