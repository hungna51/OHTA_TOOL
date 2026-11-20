using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvTool.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        /// <summary>
        /// Test vàng: đọc &lt;đơn&gt;-s.dxf của đơn thật trong OneDrive\xxxxx (KHÔNG chép vào repo public),
        /// chạy EXPORT rồi so từng byte với &lt;đơn&gt;.csv 35 cột do lisp xuất. Máy không có thư mục → bỏ qua.
        /// </summary>
        private static void TestVang()
        {
            Console.WriteLine("[Vang - 3 don mau]");
            string goc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive - OFT", "xxxxx");
            foreach (string don in new[] { "129213", "125953", "127143" })
            {
                string thu = Path.Combine(goc, don), dxf = Path.Combine(thu, don + "-s.dxf"), csv = Path.Combine(thu, don + ".csv");
                if (!File.Exists(dxf) || !File.Exists(csv)) { Console.WriteLine("  BO QUA Vang_" + don + " (khong co file mau)"); continue; }

                var (khung, dimzin) = DxfThuGon.Doc(dxf, CaiDatCsv.LopKhungMacDinh, CaiDatCsv.LopDimMacDinh);
                var tam = new List<TamXuat>();
                var loi = new List<string>();
                foreach (var k in khung)
                {
                    var r = PhanTichKhung.Chay(k, CaiDatCsv.LopDimMacDinh, dimzin);
                    if (r.Tam != null) tam.Add(r.Tam); else loi.Add(r.Loi.Split('\n')[0]);
                }
                var conLai = khung.Where(k => PhanTichKhung.CoFname(k) && !PhanTichKhung.LaKhungChuThich(k)).ToList();
                Kiem("Vang_" + don + ": bo khung chu thich -> moi khung tam doc duoc F-name + dim (" + conLai.Count + "/" + khung.Count + ")",
                     string.Join(" | ", conLai.Select(k => PhanTichKhung.DocFnameDim(k, CaiDatCsv.LopDimMacDinh, dimzin).Loi).Where(l => l != null)), "");
                string[] mau = Encoding.UTF8.GetString(DxfThuGon.DocChiaSe(csv)).TrimStart('﻿').Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (mau[0] != CsvXuat.TieuDe35)
                {
                    Console.WriteLine("  BO QUA Vang_" + don + " (file " + don + ".csv khong con la CSV lisp 35 cot - da bi ghi de?)");
                    continue;
                }
                string khop = null, khacDau = null;
                foreach (KieuSapXep kieu in new[] { KieuSapXep.TraiSangPhai, KieuSapXep.TrenXuongDuoi, KieuSapXep.ChuCai })
                {
                    var ra = new[] { CsvXuat.TieuDe35 }.Concat(CsvXuat.SapXep(tam, kieu).Select(CsvXuat.Dong35)).Concat(new[] { "" }).ToArray();
                    if (ra.SequenceEqual(mau)) { khop = kieu.ToString(); break; }
                    // 125953: DXF ghi "T=6.0" nhưng lisp đã xuất "6" — F-name bị chuẩn hoá lại sau khi xuất CSV
                    // (DXF lưu sau CSV 2 giây). Chỉ cột Độ dày được phép lệch dạng số, mọi cột khác phải khớp.
                    if (ra.Length == mau.Length && ra.Zip(mau, CungTruDoDay).All(x => x)) { khop = kieu + " (do day lech dang so)"; break; }
                    if (khacDau == null)
                    {
                        int i = 0;
                        while (i < ra.Length && i < mau.Length && ra[i] == mau[i]) i++;
                        khacDau = "dong " + i + ": C# [" + (i < ra.Length ? ra[i] : "<het>") + "] lisp [" + (i < mau.Length ? mau[i] : "<het>") + "]";
                    }
                }
                Kiem("Vang_" + don + " (" + khung.Count + " khung, " + tam.Count + " tam" + (loi.Count > 0 ? ", loi: " + string.Join(" / ", loi.Distinct()) : "") + ")",
                     khop ?? khacDau, khop ?? "khop tung dong");
            }
        }

        private static bool CungTruDoDay(string a, string b)
        {
            if (a == b) return true;
            string[] x = a.Split(','), y = b.Split(',');
            if (x.Length != y.Length || x.Length < 3) return false;
            for (int i = 0; i < x.Length; i++)
                if (x[i] != y[i] && !(i == 2 && double.TryParse(x[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double p) &&
                                     double.TryParse(y[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double q) && p == q))
                    return false;
            return true;
        }
    }
}
