using System;
using System.Collections.Generic;
using System.Linq;

namespace ZwCadPlugin.Core
{
    /// <summary>Chế độ chạy một bước GRO trong lúc chuyển lisp -> C# (mục 171).</summary>
    public enum CheDoBuoc
    {
        /// <summary>Chạy bản lisp như cũ.</summary>
        Lisp,
        /// <summary>C# chỉ tính (không ghi bản vẽ) + lisp chạy; so kết quả, in chỗ lệch, ÁP KẾT QUẢ LISP.</summary>
        AB,
        /// <summary>Áp kết quả C#.</summary>
        CS,
    }

    /// <summary>
    /// Mục 171 (GĐ 0): danh sách các bước GRO sẽ chuyển sang C# + đọc/ghi chế độ. Registry
    /// <c>HKCU\Software\OFT_Lisp\GRO\cs_&lt;buoc&gt;</c> = "lisp" / "ab" / "cs" (lớp CAD đọc/ghi;
    /// file này chỉ là quy tắc thuần để test).
    /// </summary>
    public static class GroCongTac
    {
        /// <summary>Mã bước (dùng trong lisp, registry, tên file dump) -> mô tả. Thứ tự = thứ tự giai đoạn.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> Buoc = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("phanloai", "GD1 doc + phan loai text, ghep so luong, ma khac"),
            new KeyValuePair<string, string>("sapxep",   "GD2 sap xep, gom hang, khung / vung"),
            new KeyValuePair<string, string>("xulynet",  "GD3 xu ly net + nhan Dim (3b, XLN)"),
            new KeyValuePair<string, string>("nhannet",  "GD3a phan loai net cho nhan Dim (3b)"),
            new KeyValuePair<string, string>("dimdai3b", "GD3b khop Dim dai khi xu ly net (3b)"),
            new KeyValuePair<string, string>("vongkin",  "GD3c do vong kin bien dang khi nhan Dim (3b)"),
            new KeyValuePair<string, string>("cungbogoc","GD3d bo cung bo goc bien dang khi khop dim goc (3b)"),
            new KeyValuePair<string, string>("doctext3b","GD3e doc + phan loai text so khi nhan Dim (3b)"),
            new KeyValuePair<string, string>("dimkhop3b","GD3e nang dim goc + leader + Dim dai + dim goc (3b)"),
            new KeyValuePair<string, string>("chuyenlayer3b","GD3e chuyen layer tung doi tuong (3b)"),
            new KeyValuePair<string, string>("datbango", "GD4 dat bango + ten ban ve"),
            new KeyValuePair<string, string>("quethinh", "GD5a quet + gom hinh cat"),
            new KeyValuePair<string, string>("ghephinh", "GD5b ghep hinh <-> text"),
            new KeyValuePair<string, string>("ghephc",   "GD5b-a ghep hinh chieu vao cum tam be (Hungarian + chieu tia)"),
            new KeyValuePair<string, string>("dimhinh",  "GD5c gan Dim cho hinh + cat tam gop"),
            new KeyValuePair<string, string>("dimdai",   "GD5c-a khop Dim dai khi thu thap Dim (khung)"),
            new KeyValuePair<string, string>("locdim",   "GD5c-b vong loc dau khi thu thap Dim (khung)"),
            new KeyValuePair<string, string>("khungin",  "GD5d danh sach chi tiet + Khung_IN (GROKHUNG)"),
            new KeyValuePair<string, string>("dsct",     "GD5d-a chia net cat / kegaki vao danh sach chi tiet"),
            new KeyValuePair<string, string>("dongkhung","GD5d-b dong khung + ve Khung_IN"),
            new KeyValuePair<string, string>("bocuc",    "GD6 ban chup, xep cum, tach mirror, tach va cham"),
            new KeyValuePair<string, string>("xepcum",   "GD6a xep cum mirror / gia cong sang cot phai"),
            new KeyValuePair<string, string>("tachgian", "GD6f tach va cham khung in tam don"),
            new KeyValuePair<string, string>("csv",      "GD7 CSV + XData + bao cao"),
            new KeyValuePair<string, string>("pdf",      "GD8 in PDF (GI)"),
        };

        public static bool LaBuoc(string ma) => ma != null && Buoc.Any(b => b.Key == ChuanMa(ma));

        public static string ChuanMa(string ma) => (ma ?? "").Trim().ToLowerInvariant();

        /// <summary>Chuỗi registry / lisp -> chế độ. Rỗng / lạ -> Lisp (an toàn: chưa chuyển thì chạy lisp).</summary>
        public static CheDoBuoc DocCheDo(string s)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "ab": return CheDoBuoc.AB;
                case "cs":
                case "c#": return CheDoBuoc.CS;
                default: return CheDoBuoc.Lisp;
            }
        }

        /// <summary>Mục 228: bước đã nghiệm thu A/B (130837-1) -> registry CHƯA đặt thì chạy cs (máy đội chưa từng gõ GROCS).</summary>
        public static readonly string[] MacDinhCs = { "ghephc", "cungbogoc", "doctext3b", "dimkhop3b", "chuyenlayer3b" };

        /// <summary>Chế độ của một bước từ giá trị registry: rỗng -> cs nếu bước đã nghiệm thu, còn lại như <see cref="DocCheDo(string)"/>.</summary>
        public static CheDoBuoc DocCheDo(string ma, string giaTri) =>
            string.IsNullOrWhiteSpace(giaTri) && MacDinhCs.Contains(ChuanMa(ma)) ? CheDoBuoc.CS : DocCheDo(giaTri);

        public static string TenCheDo(CheDoBuoc c) => c == CheDoBuoc.AB ? "ab" : c == CheDoBuoc.CS ? "cs" : "lisp";

        public static string TenKhoa(string ma) => "cs_" + ChuanMa(ma);
    }
}
