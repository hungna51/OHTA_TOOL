using System;
using System.Collections.Generic;
using CsvTool.Core;

namespace CsvTool.Tests
{
    internal static partial class Program
    {
        private static Func<string, object> Tu(Dictionary<string, object> d) => k => d.TryGetValue(k, out object v) ? v : null;

        private static void TestCaiDat()
        {
            Console.WriteLine("[CaiDat]");
            var rong = new Dictionary<string, object>();

            var cu = new Dictionary<string, object> { ["csvmode"] = 2, ["use_default_layers"] = "0", ["pop_1"] = "KHUNG", ["pop_2"] = "DIM", ["pop_3"] = "TAPU",
                                                      ["spline_precision"] = "2.5", ["gap_tolerance"] = "0.05", ["calc_mode"] = 1 };
            var c = CaiDatCsv.Doc(Tu(rong), Tu(cu));
            Kiem("Doc_ChuaCoKhoaMoi_LayKhoaCu: SapXep", c.SapXep, KieuSapXep.TrenXuongDuoi);
            Kiem("Doc_ChuaCoKhoaMoi_LayKhoaCu: LopKhung", c.LopKhung, "KHUNG");
            Kiem("Doc_ChuaCoKhoaMoi_LayKhoaCu: so", c.BuocSpline + "|" + c.DungSaiKheHo + "|" + c.CheDoTinh, "2.5|0.05|1");

            var moi = new Dictionary<string, object>();
            c.SapXep = KieuSapXep.ChuCai;
            c.Ghi((k, v) => moi[k] = v);
            var c2 = CaiDatCsv.Doc(Tu(moi), Tu(cu));
            Kiem("Doc_CoKhoaMoi_BoQuaKhoaCu", c2.SapXep, KieuSapXep.ChuCai);

            var hong = CaiDatCsv.Doc(Tu(rong), Tu(new Dictionary<string, object> { ["csvmode"] = "x", ["gap_tolerance"] = "abc" }));
            Kiem("Doc_GiaTriHong: mac dinh", hong.SapXep + "|" + hong.DungSaiKheHo + "|" + hong.BuocSpline, "TraiSangPhai|0.01|1");
            Kiem("Doc_Rong: mac dinh dung lop mac dinh", hong.DungLopMacDinh, true);

            var md = CaiDatCsv.Doc(Tu(rong), Tu(new Dictionary<string, object> { ["use_default_layers"] = "1", ["pop_1"] = "KHAC" }));
            Kiem("DungLopMacDinh: bo qua lop da luu", md.LopKhungDung + "|" + md.LopDimDung + "|" + md.LopTapuDung, "Khung_IN|1-XY|TAPU_TMT");
            Kiem("DungLopMacDinh tat: dung lop da luu", c.LopKhungDung, "KHUNG");
        }
    }
}
