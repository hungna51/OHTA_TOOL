using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ZwCadPlugin.Core
{
    /// <summary>Kiểu một dòng trong báo cáo cuối lệnh GRO — quyết định màu trên tab Thông Tin.</summary>
    public enum LoaiDongThongTin
    {
        BinhThuong,
        TieuDe,
        CanhBao,
        Loi,
        ThanhCong,
        Trong,
    }

    /// <summary>Một dòng báo cáo đã phân loại (bind vào ItemsControl của tab Thông Tin).</summary>
    public sealed class DongThongTin
    {
        public DongThongTin(string text, LoaiDongThongTin loai, string goc = null)
        {
            Text = text;
            Loai = loai;
            Goc = goc ?? text;
        }

        /// <summary>Dòng hiển thị (đã dịch sang tiếng Việt có dấu nếu nhận ra mẫu).</summary>
        public string Text { get; }
        public LoaiDongThongTin Loai { get; }
        /// <summary>Dòng ASCII gốc lisp gửi sang.</summary>
        public string Goc { get; }
    }

    /// <summary>
    /// Báo cáo cuối lệnh GRO do lisp ghép (ASCII thuần, xuống dòng bằng "\n" — mục 105)
    /// -> danh sách dòng có phân loại. Lisp vẫn là nơi quyết định NỘI DUNG; ở đây chỉ
    /// nhận diện các dấu hiệu lisp vốn đã dùng ("***", "!!!", "---", "Error T!!!"...)
    /// để tô màu, không diễn giải lại số liệu.
    /// </summary>
    public static class ThongTinGro
    {
        private static readonly Regex DongTong = new Regex(@"^\d+ ghi chu -> \d+ tam", RegexOptions.IgnoreCase);

        public static List<DongThongTin> PhanDong(string baoCao)
        {
            var kq = new List<DongThongTin>();
            if (string.IsNullOrEmpty(baoCao)) return kq;

            string[] dong = baoCao.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string d in dong)
            {
                string goc = d.TrimEnd();
                // Phân loại trên chuỗi GỐC (dấu hiệu lisp), rồi mới dịch để hiển thị.
                kq.Add(new DongThongTin(Dich(goc), PhanLoai(goc), goc));
            }

            // Bỏ dòng trống ở hai đầu, gộp nhiều dòng trống liền nhau thành một.
            while (kq.Count > 0 && kq[0].Loai == LoaiDongThongTin.Trong) kq.RemoveAt(0);
            while (kq.Count > 0 && kq[kq.Count - 1].Loai == LoaiDongThongTin.Trong) kq.RemoveAt(kq.Count - 1);
            for (int i = kq.Count - 1; i > 0; i--)
            {
                if (kq[i].Loai == LoaiDongThongTin.Trong && kq[i - 1].Loai == LoaiDongThongTin.Trong)
                    kq.RemoveAt(i);
            }
            return kq;
        }

        public static LoaiDongThongTin PhanLoai(string dong)
        {
            if (string.IsNullOrWhiteSpace(dong)) return LoaiDongThongTin.Trong;
            string s = dong.Trim();
            string hoa = s.ToUpperInvariant();

            // Lỗi: dữ liệu bản vẽ / CSV đang sai lệch — người dùng PHẢI xử lý.
            if (hoa.StartsWith("ERROR T!!!") || hoa.StartsWith("ERROR Q!!!")
                || hoa.Contains("KHONG GHI DUOC FILE CSV") || hoa.Contains("GHI THAT BAI")
                || hoa.Contains("SO TAM NHO HON SO GHI CHU") || hoa.Contains("CAI DAT KHONG DUOC"))
                return LoaiDongThongTin.Loi;

            // Cảnh báo: lisp đánh dấu bằng "***" / "!!!" hoặc chữ "CANH BAO".
            if (s.StartsWith("***") || s.StartsWith("!!!") || hoa.Contains("CANH BAO"))
                return LoaiDongThongTin.CanhBao;

            // Tiêu đề: dòng tổng "20 ghi chu -> 28 tam:", và tiêu đề mục
            // "--- THONG BAO XU LY NET ... ---", "[XU LY NET]: ..."
            if (DongTong.IsMatch(s) || s.StartsWith("---") || s.StartsWith("[XU LY NET]"))
                return LoaiDongThongTin.TieuDe;

            if (hoa.Contains("DA DOI HET"))
                return LoaiDongThongTin.ThanhCong;

            return LoaiDongThongTin.BinhThuong;
        }

        // ------------------------------------------------------------------------------
        // DỊCH SANG TIẾNG VIỆT CÓ DẤU (mục 107)
        // Lisp BẮT BUỘC ghi ASCII không dấu (dòng lệnh / alert của ZWCAD đọc theo CP932 ->
        // mojibake), nên dịch ở đây theo MẪU CẢ DÒNG. Dòng không khớp mẫu nào giữ nguyên —
        // lisp thêm câu mới thì tab vẫn hiện, chỉ chưa dịch. Thêm mẫu: thêm một dòng vào MauDich.
        // ------------------------------------------------------------------------------
        private static readonly KeyValuePair<Regex, MatchEvaluator>[] MauDich =
        {
            M(@"^(\d+) ghi chu -> (\d+) tam:$", m => $"{m.Groups[1]} ghi chú → {m.Groups[2]} tấm:"),
            M(@"^(\d+) bango \(layer 0\), kieu chu (.*?)(?: \*\*\* (\d+) CAI DAT KHONG DUOC \*\*\*)?$",
              m => $"{m.Groups[1]} bango (layer 0), kiểu chữ {m.Groups[2]}" +
                   (m.Groups[3].Success ? $"  *** {m.Groups[3]} bango KHÔNG đặt được kiểu chữ ***" : "")),
            M(@"^(\d+) ma khac (.*) \(layer 3-KEGAKI\)$", m => $"{m.Groups[1]} mã khắc {m.Groups[2]} (layer 3-KEGAKI)"),
            M(@"^(\d+) dong CSV \(GHI DE\) -> (.*)$",
              m => $"{m.Groups[1]} dòng CSV (ghi đè) → " + m.Groups[2].Value.Replace("*** GHI THAT BAI ***", "*** GHI THẤT BẠI ***")),
            M(@"^\(da xoa (\d+) dong cu trong file CSV\)$", m => $"(đã xoá {m.Groups[1]} dòng cũ trong file CSV)"),
            M(@"^Da danh so tu 1 den (\d+)$", m => $"Đã đánh số từ 1 đến {m.Groups[1]}"),
            M(@"^Xep cum sang phai: (\d+) cum \(mirror (\d+), vat (\d+), be\+vat (\d+), be (\d+)\) - cot X=([0-9\.\-]+)(?: \((da tranh vat can)\))?$",
              m => $"Xếp cụm sang phải: {m.Groups[1]} cụm (mirror {m.Groups[2]}, vát {m.Groups[3]}, bẻ+vát {m.Groups[4]}, bẻ {m.Groups[5]}) — cột X={m.Groups[6]}" +
                   (m.Groups[7].Success ? " (đã né vật cản)" : "")),
            M(@"^!!! (\d+) doi tuong con sot tai vi tri cum cu \(kiem tra lai\)\.$",
              m => $"!!! {m.Groups[1]} đối tượng còn sót tại vị trí cụm cũ (cần kiểm tra lại)."),
            M(@"^Handle con sot:\s*(.*)$", m => $"Handle còn sót: {m.Groups[1]}"),
            M(@"^Tach cap mirror: (\d+) cap -> (\d+) tam \(ban nghich dat ben phai, khe ([0-9\.]+)mm\)$",
              m => $"Tách cặp mirror: {m.Groups[1]} cặp → {m.Groups[2]} tấm (bản nghịch đặt bên phải, khe {m.Groups[3]} mm)"),
            M(@"^!!! Bo qua tach mirror (\d+) cap: (.*)$",
              m => $"!!! Bỏ qua tách mirror {m.Groups[1]} cặp: " + m.Groups[2].Value
                       .Replace("khong co dong Honzu/Gyakuzu rieng", "không có dòng 本図/逆図 riêng")
                       .Replace("khong gom duoc hinh", "không gom được hình")
                       .Replace(" tam)", " tấm)")),
            M(@"^Dat ma khac vao hinh: (\d+)/(\d+) tam \(thu nho 1/2: (\d+), de KEGAKI: (\d+)\)$",
              m => $"Đặt mã khắc vào hình: {m.Groups[1]}/{m.Groups[2]} tấm (thu nhỏ 1/2: {m.Groups[3]}, đè ケガキ: {m.Groups[4]})"),
            M(@"^!!! Ma khac de KEGAKI \((\d+) tam\): bango (.*)$",
              m => $"!!! Mã khắc đè ケガキ ({m.Groups[1]} tấm): bango {m.Groups[2]}"),
            M(@"^!!! Chua dat duoc ma khac vao hinh \((\d+) tam\) - move thu cong: bango (.*)$",
              m => $"!!! Chưa đặt được mã khắc vào hình ({m.Groups[1]} tấm) — cần move thủ công: bango "
                   + m.Groups[2].Value.Replace("(khong tim thay bien kin)", "(không tìm thấy biên kín)")
                                    .Replace("(chung hinh voi bango ", "(chung hình với bango ")),
            // Mục 156 / 189 / 190: khắc chữ
            M(@"^Khac chu: doi (\d+) text trong hinh sang 3-KEGAKI - bango (.*)$",
              m => $"Khắc chữ: đã đổi {m.Groups[1]} chữ trong hình sang ケガキ (3-KEGAKI) — bango {m.Groups[2]}"),
            M(@"^!!! Khac chu: (\d+) text nam mot phan ngoai hinh / cham net cat 0-KAKOU - kiem tra: bango (.*)$",
              m => $"!!! Khắc chữ: {m.Groups[1]} chữ nằm một phần ngoài hình hoặc chạm nét cắt 0-KAKOU — cần kiểm tra: bango {m.Groups[2]}"),
            M(@"^!!! Tam khac chu \((\d+) tam\) khong co text nao trong hinh de doi sang 3-KEGAKI\.$",
              m => $"!!! Tấm khắc chữ ({m.Groups[1]} tấm) không có chữ nào trong hình để đổi sang ケガキ (3-KEGAKI)."),
            M(@"^!!! Con (\d+) cap khung in chong nhau - kiem tra va tach tay\.$",
              m => $"!!! Còn {m.Groups[1]} cặp khung in chồng nhau — kiểm tra và tách tay."),
            M(@"^Tach va cham khung in: da doi (\d+) luot cho (\d+) tam don\.$",
              m => $"Tách va chạm khung in: đã dời {m.Groups[1]} lượt cho {m.Groups[2]} tấm đơn."),
            M(@"^!!! SO TAM NHO HON SO GHI CHU - co tam bi mat, bao loi ngay!$",
              m => "!!! SỐ TẤM ÍT HƠN SỐ GHI CHÚ — có tấm bị mất, cần báo lỗi ngay!"),
            M(@"^Co canh bao - xem dong lenh\.$", m => "Có cảnh báo — xem chi tiết ở dòng lệnh."),
            // Mục 205: 3 nhóm cảnh báo bắt buộc nay nằm ngay trong báo cáo (trước chỉ in ra dòng lệnh)
            M(@"^!!! (\d+) ghi chu co ""t="" nhung KHONG nhan ra kieu viet so luong \(tam van tao, Q rong\):$",
              m => $"!!! {m.Groups[1]} ghi chú có \"t=\" nhưng KHÔNG nhận ra cách viết số lượng (tấm vẫn được tạo, Q trống):"),
            M(@"^!!! (\d+) dong so luong KHONG ghep duoc vao ghi chu nao:$",
              m => $"!!! {m.Groups[1]} dòng số lượng KHÔNG ghép được vào ghi chú nào:"),
            M(@"^!!! Doi tuong bi bo qua nhung CO chu t/T \(co the la ghi chu tam viet kieu khac\):$",
              m => "!!! Chữ bị bỏ qua nhưng CÓ chữ t/T (có thể là ghi chú tấm viết kiểu khác):"),
            M(@"^\[(.*)\]  tai X=(\S+) Y=(\S+)$", m => $"[{m.Groups[1]}]  tại X={m.Groups[2]} Y={m.Groups[3]}"),
            M(@"^(\*\*\*|!!!)\s*CANH BAO:\s*Co\s*(\d+)\s*tam\s*can xu ly doi net sang 4-SHT.*?-\s*bango:\s*(.*)$",
              m => $"*** CẢNH BÁO: Có {m.Groups[2]} tấm cần xử lý đổi nét sang 4-SHT (ghi chú \"xử lý SHT\") — bango: {m.Groups[3]}"),
            M(@"^\*\*\* CANH BAO: CO (\d+) CUM TEXT BI TRUNG LOAI SO LUONG \(HONZU/GYAKUZU\)!$",
              m => $"*** CẢNH BÁO: CÓ {m.Groups[1]} CỤM TEXT BỊ TRÙNG LOẠI SỐ LƯỢNG (本図 / 逆図)!"),
            M(@"^\+ Da khoanh khung chu nhat mau DO tren layer ""(.*)""\.$",
              m => $"+ Đã khoanh khung chữ nhật màu ĐỎ trên layer \"{m.Groups[1]}\"."),
            M(@"^\*\*\* CANH BAO: CO (\d+) HINH CO DON!$", m => $"*** CẢNH BÁO: CÓ {m.Groups[1]} HÌNH CÔ ĐƠN!"),
            M(@"^\+ Khong thuoc bat ky text thong tin nao hoac nam ngoai vung\.$",
              m => "+ Không thuộc text thông tin nào, hoặc nằm ngoài vùng."),
            M(@"^\+ Go lenh DST \(hoac DS\) de xem danh sach va zoom den tung tam\.$",
              m => "+ Xem ở tab Danh Sách Tấm (lệnh DST / DS) và zoom đến từng tấm."),
            M(@"^\[x\] Tu dong in nhanh PDF theo layer: (.*)$", m => $"[x] Tự động in nhanh PDF theo layer: {m.Groups[1]}"),
            M(@"^\[XU LY NET\]: Khong tim thay doi tuong nao tren layer goc ""(.*)"" ben trong cac khung bao da chon\.$",
              m => $"[XỬ LÝ NÉT]: Không tìm thấy đối tượng nào trên layer gốc \"{m.Groups[1]}\" bên trong các khung bao đã chọn."),
            M(@"^--- THONG BAO XU LY NET \(Layer ""(.*)""\) ---$", m => $"--- XỬ LÝ NÉT (layer gốc \"{m.Groups[1]}\") ---"),
            M(@"^\+ (\d+) net Continuous -> 0-KAKOU$", m => $"+ {m.Groups[1]} nét Continuous → 0-KAKOU"),
            M(@"^\+ (\d+) net Center1 -> 3-KEGAKI$", m => $"+ {m.Groups[1]} nét Center1 → 3-KEGAKI"),
            M(@"^\+ (\d+) text -> 1-SUNPOU$", m => $"+ {m.Groups[1]} text → 1-SUNPOU"),
            M(@"^-> DA DOI HET! Khong con doi tuong nao tren layer goc\.$",
              m => "→ ĐÃ ĐỔI HẾT! Không còn đối tượng nào trên layer gốc."),
            M(@"^-> CON LAI (\d+) DOI TUONG GIU NGUYEN \(KHONG DOI\):$",
              m => $"→ CÒN LẠI {m.Groups[1]} ĐỐI TƯỢNG GIỮ NGUYÊN (CHƯA ĐỔI):"),
            M(@"^\* Net (.*): (\d+) doi tuong$", m => $"* Nét {m.Groups[1]}: {m.Groups[2]} đối tượng"),
            M(@"^\(Xem tab Net Chua Doi de zoom kiem tra\)$", m => "(Xem tab Nét Chưa Đổi để zoom kiểm tra)"),
            M(@"^\(Bam OK se tu dong mo danh sach zoom kiem tra\)$", m => "(Bấm OK sẽ tự mở danh sách để zoom kiểm tra)"),
            M(@"^!!! (\d+) ghi chu KHONG TIM THAY HINH VE CAT GIA CONG:$",
              m => $"!!! {m.Groups[1]} ghi chú KHÔNG TÌM THẤY HÌNH VẼ CẮT GIA CÔNG:"),
            M(@"^Text #(\d+) (.*)  tai X=(\S+) Y=(\S+)$", m => $"Text #{m.Groups[1]} {m.Groups[2]}  tại X={m.Groups[3]} Y={m.Groups[4]}"),
            M(@"^Kiem tra lai net ve tren layer cat hoac vi tri dat text\.$",
              m => "Kiểm tra lại nét vẽ trên layer cắt hoặc vị trí đặt text."),
            M(@"^KHONG GHI DUOC FILE CSV$", m => "KHÔNG GHI ĐƯỢC FILE CSV"),
            M(@"^Bango / ma khac / F-name DA duoc tao tren ban ve, nhung file CSV thi KHONG ghi duoc\.$",
              m => "Bango / mã khắc / F-name ĐÃ được tạo trên bản vẽ, nhưng file CSV thì KHÔNG ghi được."),
            M(@"^Thuong la vi file dang bi MO boi Excel\.$", m => "Thường là do file đang được MỞ trong Excel."),
            M(@"^Hay DONG file CSV lai, UNDO lenh nay, roi chay lai\.$", m => "Hãy ĐÓNG file CSV, UNDO lệnh này rồi chạy lại."),
            M(@"^Error T!!!$", m => "Lỗi T!!! (độ dày không có trong danh sách độ dày chuẩn)"),
            M(@"^Error Q!!!$", m => "Lỗi Q!!! (số lượng trống hoặc không phải số nguyên)"),
        };

        private static KeyValuePair<Regex, MatchEvaluator> M(string mau, MatchEvaluator dich) =>
            new KeyValuePair<Regex, MatchEvaluator>(new Regex(mau, RegexOptions.CultureInvariant), dich);

        /// <summary>
        /// Dịch MỘT dòng báo cáo ASCII của lisp sang tiếng Việt có dấu; giữ nguyên thụt đầu
        /// dòng. Không khớp mẫu nào -> trả lại nguyên văn.
        /// </summary>
        public static string Dich(string dong)
        {
            if (string.IsNullOrWhiteSpace(dong)) return dong ?? string.Empty;
            int n = 0;
            while (n < dong.Length && dong[n] == ' ') n++;
            string thut = dong.Substring(0, n), than = GiaiEscape(dong.Substring(n).TrimEnd());
            foreach (var kv in MauDich)
            {
                Match m = kv.Key.Match(than);
                if (m.Success) return thut + kv.Value(m);
            }
            return thut + than;
        }

        private static readonly Regex REscape = new Regex(@"\\U\+([0-9A-Fa-f]{4})", RegexOptions.CultureInvariant);

        /// <summary>
        /// Mục 205: lisp gửi chữ Nhật trong báo cáo dạng \U+XXXX — chuỗi ASCII đi từ lisp sang
        /// .NET không phụ thuộc bảng mã của máy — đổi lại thành ký tự.
        /// </summary>
        public static string GiaiEscape(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf("\\U+", StringComparison.Ordinal) < 0) return s;
            return REscape.Replace(s, m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        }

        /// <summary>Số dòng cảnh báo + lỗi — hiện thành huy hiệu trên tiêu đề tab.</summary>
        public static int DemCanhBao(IEnumerable<DongThongTin> dong)
        {
            int n = 0;
            if (dong == null) return 0;
            foreach (var d in dong)
            {
                if (d.Loai == LoaiDongThongTin.CanhBao || d.Loai == LoaiDongThongTin.Loi) n++;
            }
            return n;
        }
    }
}
