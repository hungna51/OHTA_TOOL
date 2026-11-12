using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using OhtaTool.Plugin.Core;
using TeamsTool.Core;

namespace TeamsTool.Services
{
    /// <summary>
    /// Phòng 1 + 2 của khu 🤖 AI (spec 2026-11-11, ADR 0010): tải tệp đơn về thư mục đơn, gộp bản nháp 番号.PDF,
    /// gọi Claude đọc từng bản vẽ. Kết quả ở &lt;thư mục đơn&gt;\ai\*.json.
    /// </summary>
    public static class AiDon
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>Thư mục OHTA_TOOL (cha của 06_Teams).</summary>
        public static string Goc => Path.GetDirectoryName(CaiDatTeams.ThuMucModule.TrimEnd('\\', '/'));
        public static string ThuMucQuyChuan => Path.Combine(Goc, "_apps", "OhtaTeams", "data", "quy_chuan");

        // ---------- Cài đặt ----------

        /// <summary>4 phòng AI: thư mục quy chuẩn riêng (quy_chuan\&lt;tên&gt;) và tên hiện.</summary>
        public static readonly string[] ThuMucPhong = { "1_tiep_nhan", "2_doc_ban_ve", "3_kiem_tra", "4_trien_khai" };
        public static readonly string[] TenPhong = { "1 · Tiếp nhận", "2 · Đọc bản vẽ", "3 · Kiểm tra", "4 · Triển khai" };

        /// <summary>
        /// Key Claude: phong = 0 → key chung 05_AI\data\ai.key (dùng chung với tab AI trên CAD);
        /// phong = 1..4 → ai.key.phong&lt;n&gt; cạnh đó (gitignore theo ai.key*).
        /// </summary>
        public static string FileKey(int phong = 0) => Path.Combine(Goc, "05_AI", "data", phong == 0 ? "ai.key" : "ai.key.phong" + phong);

        /// <summary>Bật = mỗi phòng một key riêng; tắt = mọi phòng dùng key chung.</summary>
        public static bool KeyRieng
        {
            get => CaiDatTeams.Doc("ai_key_rieng") == "1";
            set => CaiDatTeams.Ghi("ai_key_rieng", value ? "1" : "");
        }

        /// <summary>Dòng đầu không rỗng, không bắt đầu bằng # của file key; không có → null.</summary>
        public static string DocKeyFile(int phong)
        {
            string f = FileKey(phong);
            if (!File.Exists(f)) return null;
            return File.ReadAllLines(f).Select(s => s.Trim()).FirstOrDefault(s => s.Length > 0 && !s.StartsWith("#", StringComparison.Ordinal));
        }

        /// <summary>Key phòng dùng: đang bật key riêng và phòng có key → key phòng; còn lại → key chung.</summary>
        public static string DocKey(int phong) => (KeyRieng ? DocKeyFile(phong) : null) ?? DocKeyFile(0);

        /// <summary>Thay key (giữ các dòng # chú thích). phong = 0: key chung — tab AI trên CAD cũng dùng key mới.</summary>
        public static void GhiKey(string key, int phong = 0)
        {
            string f = FileKey(phong);
            var giu = File.Exists(f) ? File.ReadAllLines(f).Where(s => s.TrimStart().StartsWith("#", StringComparison.Ordinal)).ToList() : new List<string>();
            giu.Add(key.Trim());
            GhiAnToan(f, string.Join("\r\n", giu) + "\r\n");
        }

        /// <summary>Model chọn ở ⚙ → AI của OhtaTeams; để trống = theo tab AI trên CAD (registry), không phải Claude (vd. Ling) → Opus 5.5.</summary>
        public static string ModelRieng
        {
            get => CaiDatTeams.Doc("ai_model");
            set => CaiDatTeams.Ghi("ai_model", value ?? "");
        }

        public static string Model()
        {
            string m = ModelRieng;
            if (m.StartsWith("claude-", StringComparison.Ordinal)) return m;
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\OFT_Lisp\OhtaTool\AI")) m = k?.GetValue("model") as string; }
            catch (Exception) { }
            return m != null && m.StartsWith("claude-", StringComparison.Ordinal) ? m : PhongDocBanVe.ModelMacDinh;
        }

        /// <summary>Đã đồng ý gửi tệp đơn lên Claude API trên máy này chưa (hỏi 1 lần).</summary>
        public static bool DaDongY
        {
            get => CaiDatTeams.Doc("ai_dong_y") == "1";
            set => CaiDatTeams.Ghi("ai_dong_y", value ? "1" : "");
        }

        /// <summary>Thư mục quy chuẩn của phòng (1..4): quy_chuan\&lt;n_ten&gt;.</summary>
        public static string ThuMucQuyChuanPhong(int phong) => Path.Combine(ThuMucQuyChuan, ThuMucPhong[phong - 1]);

        /// <summary>
        /// Bộ quy chuẩn riêng của phòng (*.md trong quy_chuan\&lt;n_ten&gt;, gitignore) gửi kèm lời dặn.
        /// Máy chưa chia bộ (chưa có thư mục phòng) → mọi *.md ở gốc quy_chuan như trước; thiếu hết → rỗng.
        /// </summary>
        public static string QuyChuan(int phong)
        {
            string tm = ThuMucQuyChuanPhong(phong);
            if (!Directory.Exists(tm)) tm = ThuMucQuyChuan;
            if (!Directory.Exists(tm)) return "";
            return string.Join("\n\n", Directory.GetFiles(tm, "*.md").OrderBy(f => f).Select(f => File.ReadAllText(f, Encoding.UTF8)));
        }

        public static string[] DongCongTy()
        {
            string f = Path.Combine(ThuMucQuyChuan, "cong_ty.txt");
            return File.Exists(f) ? File.ReadAllLines(f, Encoding.UTF8) : new string[0];
        }

        // ---------- Đọc / ghi kết quả ----------

        public static string FileTiepNhan(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, "tiep_nhan.json");
        public static string FileChiTiet(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, "chi_tiet.json");
        public static string ThuMucTep(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, PhongTiepNhan.ThuMucGocTep);

        public static TiepNhan DocTiepNhan(string tm) => File.Exists(FileTiepNhan(tm)) ? PhongTiepNhan.Doc(File.ReadAllText(FileTiepNhan(tm), Encoding.UTF8)) : null;
        public static ChiTiet DocChiTiet(string tm) => File.Exists(FileChiTiet(tm)) ? PhongDocBanVe.Doc(File.ReadAllText(FileChiTiet(tm), Encoding.UTF8)) : null;
        public static void Ghi(string tm, TiepNhan tn) => GhiAnToan(FileTiepNhan(tm), PhongTiepNhan.Ghi(tn));
        public static void Ghi(string tm, ChiTiet ct) => GhiAnToan(FileChiTiet(tm), PhongDocBanVe.Ghi(ct));

        public static string FileKiemTra(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, "kiem_tra.json");
        public static KiemTra DocKiemTra(string tm) => File.Exists(FileKiemTra(tm)) ? PhongKiemTra.Doc(File.ReadAllText(FileKiemTra(tm), Encoding.UTF8)) : null;
        public static void Ghi(string tm, KiemTra kt) => GhiAnToan(FileKiemTra(tm), PhongKiemTra.Ghi(kt));

        /// <summary>Ghi file tạm rồi thay — tắt máy giữa chừng không làm hỏng kết quả cũ.</summary>
        private static void GhiAnToan(string f, string noiDung, Encoding enc = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            string tam = f + ".tmp";
            File.WriteAllText(tam, noiDung, enc ?? Utf8);
            if (File.Exists(f)) File.Replace(tam, f, null); else File.Move(tam, f);
        }

        // ---------- Phòng 1 ----------

        /// <summary>
        /// Tải tệp đính kèm của dòng + tệp gửi trong các chuỗi chat của đơn vào ai\goc\ (giữ tên gốc; trùng tên khác nội dung → thêm (2)).
        /// Giữ loại tệp người dùng đã sửa ở lần trước. Gộp bản nháp 番号.PDF.
        /// </summary>
        public static async Task<TiepNhan> TiepNhan(DonTeams d, string listUrl, string listId, string tm, Action<string> bao, CancellationToken ct)
        {
            TiepNhan cu = DocTiepNhan(tm);
            var tn = new TiepNhan
            {
                LoaiDon = cu?.LoaiDon ?? PhongTiepNhan.DoanLoaiDon(d.TatCa),
                CongTy = cu?.CongTy ?? PhongTiepNhan.DoanCongTy(d.TatCa, DongCongTy()),
            };
            string dir = ThuMucTep(tm);
            Directory.CreateDirectory(dir);

            var nguon = new List<(TepDinhKem Tep, string Nguon)>();
            if (d.CoTep)
            {
                bao("Phòng 1: đọc danh sách tệp đính kèm…");
                foreach (TepDinhKem t in await GraphTeams.TepCuaDon(listUrl, listId, d.Id, ct)) nguon.Add((t, "don"));
            }
            foreach (HoiThoai h in TraoDoi.DocHoiThoai(d.HoiThoaiMeta))
            {
                bao("Phòng 1: đọc tệp trong chat…");
                try
                {
                    var chuoi = await GraphTeams.TaiChuoi(h, ct);
                    if (chuoi != null) foreach (TinNhan tin in chuoi.Value.Tin) foreach (TepDinhKem t in tin.Tep) nguon.Add((t, "chat"));
                }
                catch (Exception ex) { bao("Không đọc được một chuỗi chat: " + ex.Message); } // chưa cấp quyền chat → vẫn làm tiếp với tệp đính kèm
            }

            int i = 0;
            foreach (var (t, loaiNguon) in nguon)
            {
                ct.ThrowIfCancellationRequested();
                bao("Phòng 1: tải " + (++i) + "/" + nguon.Count + " — " + t.Ten);
                string tamF = loaiNguon == "don" ? await GraphTeams.TaiTep(listUrl, d.Id, t, ct) : await GraphTeams.TaiTepChat(t, ct);
                string ma = PhongTiepNhan.MaBam(tamF);
                if (tn.Tep.Any(x => x.MaBam == ma)) continue; // cùng tệp gửi cả đính kèm lẫn chat
                string ten = TenKhongTrung(dir, PhongTiepNhan.TenAnToan(t.Ten), ma);
                string dich = Path.Combine(dir, ten);
                if (!File.Exists(dich)) File.Copy(tamF, dich); // đã có = cùng nội dung (TenKhongTrung) → không chép đè, kẻo lỗi khi tệp đang mở
                TepDon cuT = cu?.Tep.FirstOrDefault(x => x.Ten == ten);
                tn.Tep.Add(new TepDon { Ten = ten, Nguon = loaiNguon, MaBam = ma, Loai = cuT?.Loai ?? PhongTiepNhan.XepLoai(ten) });
            }

            GopBango(tn, tm, d.Title, bao);
            Ghi(tm, tn);
            return tn;
        }

        private static string TenKhongTrung(string dir, string ten, string ma)
        {
            string ext = Path.GetExtension(ten), than = Path.GetFileNameWithoutExtension(ten);
            for (int k = 1; ; k++)
            {
                string thu = k == 1 ? ten : than + " (" + k + ")" + ext;
                string f = Path.Combine(dir, thu);
                if (!File.Exists(f) || PhongTiepNhan.MaBam(f) == ma) return thu;
            }
        }

        /// <summary>Gộp lại bản nháp &lt;mã&gt;-番号.PDF theo loại tệp hiện tại (gọi lại khi người dùng đổi loại tệp).</summary>
        public static void GopBango(TiepNhan tn, string tm, string title, Action<string> bao)
        {
            var ds = PhongTiepNhan.ThuTuBango(tn.Tep).Select(t => Path.Combine(ThuMucTep(tm), t.Ten)).Where(File.Exists).ToList();
            tn.BanNhapBango = "";
            if (ds.Count == 0) return;
            string ra = Path.Combine(tm, PhongTiepNhan.ThuMucAi, PhongTiepNhan.TenBango(title));
            try
            {
                GopPdf(ds, ra);
                tn.BanNhapBango = Path.GetFileName(ra);
            }
            catch (Exception ex) { bao("Không gộp được 番号.PDF: " + ex.Message); }
        }

        // ---------- PDF (PdfSharp nạp từ _system\bin\ZwCAD, như tab PRINT — ADR 0005) ----------

        private static bool _resolver;

        public static void GopPdf(IList<string> vao, string ra)
        {
            if (!_resolver)
            {
                _resolver = true;
                string dll = Path.Combine(Goc, "_system", "bin", "ZwCAD");
                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    string ten = new AssemblyName(e.Name).Name;
                    Assembly da = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, ten, StringComparison.OrdinalIgnoreCase));
                    if (da != null) return da;
                    string f = Path.Combine(dll, ten + ".dll");
                    return File.Exists(f) ? Assembly.LoadFrom(f) : null;
                };
            }
            GopPdfLoi(vao, ra);
        }

        // NoInlining: JIT chỉ nạp PdfSharp khi vào hàm này, lúc resolver đã đăng ký
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void GopPdfLoi(IList<string> vao, string ra)
        {
            using (var dich = new PdfSharp.Pdf.PdfDocument())
            {
                foreach (string f in vao)
                    using (var nguon = PdfSharp.Pdf.IO.PdfReader.Open(f, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                        for (int i = 0; i < nguon.PageCount; i++) dich.AddPage(nguon.Pages[i]);
                dich.Save(ra);
            }
        }

        // ---------- Phòng 2 ----------

        /// <summary>
        /// Gửi từng bản vẽ (PDF / DXF) chưa đọc hoặc đã đổi nội dung lên Claude; tệp không đổi giữ kết quả cũ.
        /// tatCa = true: đọc lại mọi tệp (↻ Chạy lại). DWG / STEP bỏ qua (spec: chưa đọc hình học).
        /// </summary>
        public static async Task<ChiTiet> DocBanVe(TiepNhan tn, string tm, bool tatCa, Action<string> bao, CancellationToken ct)
        {
            string key = KeyPhong(2);
            string model = Model(), quyChuan = QuyChuan(2);
            ChiTiet cu = DocChiTiet(tm) ?? new ChiTiet();
            var ct2 = new ChiTiet { TokenVao = cu.TokenVao, TokenRa = cu.TokenRa, Tien = cu.Tien, MaBamDaDoc = new Dictionary<string, string>(cu.MaBamDaDoc) };

            var canDoc = tn.Tep.Where(t => t.Loai == PhongTiepNhan.BanVe && (t.Ten.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || t.Ten.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase)))
                               .Where(t => tatCa || !ct2.MaBamDaDoc.TryGetValue(t.Ten, out string mb) || mb != t.MaBam).ToList();
            var moi = new List<TamAi>();
            var docLai = new HashSet<string>();
            var loi = new List<string>();
            int i = 0;
            foreach (TepDon t in canDoc)
            {
                ct.ThrowIfCancellationRequested();
                bao("Phòng 2: AI đọc " + (++i) + "/" + canDoc.Count + " — " + t.Ten + " (mỗi tệp khoảng 1 phút)");
                string f = Path.Combine(ThuMucTep(tm), t.Ten);
                string pdf = null, dxf = null;
                if (t.Ten.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    if (new FileInfo(f).Length > PhongDocBanVe.PdfToiDa) { loi.Add(t.Ten + ": PDF quá lớn (> 22 MB), tách nhỏ rồi chạy lại"); continue; }
                    pdf = Convert.ToBase64String(File.ReadAllBytes(f));
                }
                else
                {
                    try { dxf = DocDxf.TomTat(t.Ten, DocDxf.DocFile(f)); }
                    catch (Exception ex) { loi.Add(t.Ten + ": " + ex.Message); continue; }
                }
                string body = PhongDocBanVe.Body(model, PhongDocBanVe.LoiDan, quyChuan, t.Ten, pdf, dxf);
                PhongDocBanVe.PhanHoi ph = PhongDocBanVe.DocPhanHoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, ct));
                ct2.TokenVao += ph.Vao + ph.GhiCache + ph.DocCache;
                ct2.TokenRa += ph.Ra;
                ct2.Tien += PhongDocBanVe.Tien(model, ph.Vao, ph.GhiCache, ph.DocCache, ph.Ra);
                if (ph.Loi != null) { loi.Add(t.Ten + ": " + ph.Loi); continue; }
                foreach (TamAi tam in ph.Tam) tam.Tep = t.Ten;
                moi.AddRange(ph.Tam);
                docLai.Add(t.Ten);
                ct2.MaBamDaDoc[t.Ten] = t.MaBam;
            }
            // Tệp đã bị bỏ khỏi đơn / đổi sang loại khác → bỏ tấm của nó
            var conLai = new HashSet<string>(tn.Tep.Where(t => t.Loai == PhongTiepNhan.BanVe).Select(t => t.Ten));
            ct2.Tam = PhongDocBanVe.Gop(cu.Tam.Where(x => conLai.Contains(x.Tep)).ToList(), moi, docLai);
            // Bảng đổi (đọc lại / bỏ tệp) → phải xác nhận lại trước khi sang phòng 3
            ct2.XacNhan = docLai.Count == 0 && ct2.Tam.Count == cu.Tam.Count ? cu.XacNhan : "";
            Ghi(tm, ct2);
            if (loi.Count > 0) bao("Có tệp chưa đọc được:\n" + string.Join("\n", loi));
            return ct2;
        }

        // ---------- Phòng 3 ----------

        /// <summary>
        /// Kiểm bảng tấm đã xác nhận: luật C# (bảng TV + catalog trong 01_Fname\data) rồi AI rà từng tệp bản vẽ theo QUY CHUẨN phòng 3
        /// (không có key → chỉ chạy luật). Giữ trạng thái người dùng đã chọn ở lần trước (theo mã vấn đề).
        /// </summary>
        public static async Task<KiemTra> KiemTraDon(TiepNhan tn, ChiTiet ct, string tm, bool dungAi, Action<string> bao, CancellationToken huy)
        {
            KiemTra cu = DocKiemTra(tm);
            List<PhongKiemTra.DongTv> tv = PhongKiemTra.DocTv(BangFname("trav_90"));
            string catalog = BangFname("catalog");
            var tam = ct.Tam.Where(t => t.Moi).ToList();

            bao("Phòng 3: chạy luật…");
            List<VanDe> luat = PhongKiemTra.Luat(tam, tv, catalog);
            var vanDe = new List<VanDe>(luat);
            var kt = new KiemTra { TuXacNhan = ct.XacNhan, TokenVao = cu?.TokenVao ?? 0, TokenRa = cu?.TokenRa ?? 0, Tien = cu?.Tien ?? 0 };
            var loi = new List<string>();

            string key = !dungAi ? null : NguonAi != AiCli.Api ? "" : DocKey(3); // CLI không cần key
            if (dungAi && key == null) loi.Add("Chưa có key Claude (⚙ → 🤖 AI) — chỉ chạy luật, AI chưa rà bản vẽ.");
            if (key != null)
            {
                string model = Model(), quyChuan = QuyChuan(3);
                var theoTep = tam.GroupBy(t => t.Tep).ToList();
                int i = 0;
                foreach (var nhom in theoTep)
                {
                    huy.ThrowIfCancellationRequested();
                    bao("Phòng 3: AI rà " + (++i) + "/" + theoTep.Count + " — " + nhom.Key + " (mỗi tệp khoảng 1 phút)");
                    string f = Path.Combine(ThuMucTep(tm), nhom.Key);
                    if (!File.Exists(f)) { loi.Add(nhom.Key + ": không còn tệp trong ai\\goc"); continue; }
                    string pdf = null, dxf = null;
                    try
                    {
                        if (f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            if (new FileInfo(f).Length > PhongDocBanVe.PdfToiDa) { loi.Add(nhom.Key + ": PDF quá lớn (> 22 MB)"); continue; }
                            pdf = Convert.ToBase64String(File.ReadAllBytes(f));
                        }
                        else dxf = DocDxf.TomTat(nhom.Key, DocDxf.DocFile(f));
                    }
                    catch (Exception ex) { loi.Add(nhom.Key + ": " + ex.Message); continue; }
                    var luatTep = luat.Where(v => nhom.Any(t => v.Ma.StartsWith("luat|" + t.Khoa + "|", StringComparison.Ordinal))).ToList();
                    string body = PhongKiemTra.Body(model, quyChuan, pdf, PhongKiemTra.DauVao(nhom.Key, tn?.CongTy, nhom, luatTep, cu?.VanDe), dxf);
                    PhongKiemTra.PhanHoi ph = PhongKiemTra.DocPhanHoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, huy), nhom.Key);
                    kt.TokenVao += ph.Vao + ph.GhiCache + ph.DocCache;
                    kt.TokenRa += ph.Ra;
                    kt.Tien += PhongDocBanVe.Tien(model, ph.Vao, ph.GhiCache, ph.DocCache, ph.Ra);
                    if (ph.Loi != null) { loi.Add(nhom.Key + ": " + ph.Loi); continue; }
                    vanDe.AddRange(ph.VanDe);
                }
            }
            // AI lỗi / không chạy: giữ vấn đề AI cũ của lần trước để không mất
            if (loi.Count > 0 && cu != null) vanDe.AddRange(cu.VanDe.Where(v => v.Nguon == "ai" && !vanDe.Any(x => x.Ma == v.Ma)));
            kt.VanDe = PhongKiemTra.Gop(cu?.VanDe, vanDe.GroupBy(v => v.Ma).Select(g => g.First()).ToList());
            kt.Luc = DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            Ghi(tm, kt);
            if (loi.Count > 0) bao("Kiểm xong, có chỗ chưa chạy được:\n" + string.Join("\n", loi));
            return kt;
        }

        // ---------- Phòng 4 ----------

        private static string ThuMucFname => Path.Combine(Goc, "01_Fname");

        /// <summary>Bảng dữ liệu Fname như tab Fname: bản Google Sheet trong cache máy, không có thì 01_Fname\data.</summary>
        private static string BangFname(string ten) => string.Join("\n", OhtaTool.Plugin.Core.FnameDataStore.ReadLines(ThuMucFname, ten));

        public static string FileTrienKhai(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, "trien_khai.json");
        public static string FileCsv(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, "trien_khai.csv");
        public static KetQuaTrienKhai DocTrienKhai(string tm) => File.Exists(FileTrienKhai(tm)) ? PhongTrienKhai.Doc(File.ReadAllText(FileTrienKhai(tm), Encoding.UTF8)) : null;
        public static void Ghi(string tm, KetQuaTrienKhai kq) => GhiAnToan(FileTrienKhai(tm), PhongTrienKhai.Ghi(kq));

        private static bool _daNapTraV;

        /// <summary>Nạp bảng TraV + catalog như tab Fname (một lần mỗi lần mở app; ↻ Triển khai lại thì nạp lại để lấy số mới).</summary>
        private static void NapTraV(bool lai = false)
        {
            if (_daNapTraV && !lai) return;
            OhtaTool.Plugin.Core.TraVCalculator.Load(ThuMucFname);
            OhtaTool.Plugin.Core.TraVCalculator.SetCatalog(OhtaTool.Plugin.Core.FnameCatalogData.LoadFromModuleDirectory(ThuMucFname));
            _daNapTraV = true;
        }

        /// <summary>
        /// Tính trải phẳng mọi tấm (V, δ từ TraV; Lmin từ trav_90; T thực từ tam / trav_90) rồi ghi trien_khai.csv. Trả (tấm, kết quả) theo thứ tự bảng.
        /// </summary>
        public static List<(TamAi Tam, TamTrienKhai Tk)> TinhLai(ChiTiet ct, KetQuaTrienKhai kq, string title, string tm)
        {
            NapTraV();
            List<PhongKiemTra.DongTv> tv = PhongKiemTra.DocTv(BangFname("trav_90"));
            string catalog = BangFname("catalog");
            var ra = new List<(TamAi, TamTrienKhai)>();
            foreach (TamAi tam in ct.Tam.Where(t => t.Moi))
            {
                TamTrienKhai tk = kq.Tam.FirstOrDefault(x => x.Khoa == tam.Khoa);
                if (tk == null) continue;
                List<UngVienV> TraV(double goc) => OhtaTool.Plugin.Core.TraVCalculator.GetBendOptionsForFname(tam.M, tam.T, goc)
                    .Select(c => new UngVienV { V = c.DieLabel, Chu = c.FormattedText, Delta = PhongTrienKhai.DeltaTuChu(c.FormattedText) ?? 0, CanhBao = c.Warning ?? "", Nguon = c.Source ?? "" })
                    .Where(u => u.Delta > 0).ToList();
                string loai = PhongKiemTra.LoaiTv(tam.M, tam.Mat, catalog, tv);
                double t = PhongKiemTra.So(tam.T) ?? 0;
                var lm = loai == null ? new List<PhongKiemTra.DongTv>() : PhongKiemTra.Lmin(tv, loai, t);
                double? Lmin(string v) => lm.FirstOrDefault(r => "V" + PhongTrienKhai.F(r.V) == v)?.Lmin;
                double tThuc = PhongKiemTra.So(OhtaTool.Plugin.Core.TraVCalculator.GetRealThickness(tam.M, tam.T)) ?? t;
                PhongTrienKhai.Tinh(tk, tam, TraV, tThuc, Lmin);
                ra.Add((tam, tk));
            }
            var csv = new StringBuilder(PhongTrienKhai.TieuDeCsv + "\r\n");
            for (int i = 0; i < ra.Count; i++) csv.Append(PhongTrienKhai.DongCsv(title, i + 1, ra[i].Item1, ra[i].Item2)).Append("\r\n");
            try { GhiAnToan(FileCsv(tm), csv.ToString(), new UTF8Encoding(true)); } // BOM: Excel mở đúng chữ Nhật
            catch (IOException) { } // CSV đang mở trong Excel: giữ bản cũ, bảng trên màn hình vẫn đúng
            return ra;
        }

        // ---------- Bản vẽ trải phẳng bằng ezdxf (thư viện biên dạng _apps\OhtaTeams\ve_dxf) ----------

        public static string ThuMucBanVe(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, "ban_ve");
        /// <summary>Mẫu AI đề xuất cho đơn này (vẽ thử) — chỉ máy Admin chép vào thư viện chung.</summary>
        public static string ThuVienThu(string tm) => Path.Combine(ThuMucBanVe(tm), "mau_thu");
        public static string ThuMucVeDxf => Path.Combine(Goc, "_apps", "OhtaTeams", "ve_dxf");
        public static string ThuVienChung => Path.Combine(ThuMucVeDxf, "thu_vien");
        /// <summary>Môi trường vẽ người dùng gửi (layer, kiểu chữ, kiểu dim) — nền của mọi bản vẽ.</summary>
        public static string MoiTruongVe => Path.Combine(ThuMucQuyChuanPhong(4), "moi_truong", "moi-truong-ve.dxf");

        /// <summary>Máy Admin như tab Fname: registry Fname\ApprovalIsAdmin = 1 (tab admin Google Sheet) hoặc có cài Kev.</summary>
        public static bool LaMayAdmin()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\OFT_Lisp\OhtaTool\Fname"))
                    if (k?.GetValue("ApprovalIsAdmin") is int v && v == 1) return true;
                return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OHTA_TOOL", "kev", ".venv", "Scripts", "python.exe"));
            }
            catch (Exception) { return false; }
        }

        /// <summary>Kết quả ve.py --kiem: python thật đang dùng, phiên bản ezdxf (null = chưa cài), có matplotlib (ảnh xem trước) không.</summary>
        public sealed class TinhTrangPython
        {
            public string Python, Ezdxf, Loi;
            public bool Matplotlib;
            public bool SanSang => Python != null && Ezdxf != null;
        }

        private static TinhTrangPython _python;

        /// <summary>Tìm Python (py -3, python) chạy được ve.py --kiem; nhớ kết quả tới khi gọi lại với lai = true (sau khi cài).</summary>
        public static async Task<TinhTrangPython> KiemPython(bool lai = false)
        {
            if (_python != null && !lai) return _python;
            string ve = Path.Combine(ThuMucVeDxf, "ve.py");
            var kq = new TinhTrangPython { Loi = "Máy chưa có Python 3 — cài từ python.org hoặc Microsoft Store (Python 3.12+), rồi mở lại OhtaTeams." };
            if (!File.Exists(ve)) kq.Loi = "Thiếu " + ve + " (cập nhật OHTA TOOL).";
            else
                foreach (var (exe, dau) in new[] { ("py", new[] { "-3" }), ("python", new string[0]) })
                {
                    try
                    {
                        var (ma, ra) = await ChayLenh(exe, ThuMucVeDxf, CancellationToken.None, dau.Concat(new[] { ve, "--kiem" }).ToArray());
                        if (ma != 0 || !(JsonLite.Parse(ra.Trim().Split('\n').Last()) is JsonObject o)) continue;
                        kq = new TinhTrangPython { Python = o.GetString("python"), Ezdxf = o.GetString("ezdxf"), Matplotlib = o.Get("matplotlib") is bool b && b };
                        if (kq.Ezdxf == null) kq.Loi = "Máy có Python nhưng chưa có ezdxf — bấm Cài ezdxf.";
                        break;
                    }
                    catch (Exception) { } // không có lệnh này: thử lệnh sau
                }
            _python = kq;
            return kq;
        }

        /// <summary>Cài ezdxf (+ matplotlib cho ảnh xem trước) bằng pip của Python đã tìm thấy. Trả tình trạng sau khi cài.</summary>
        public static async Task<TinhTrangPython> CaiEzdxf(Action<string> bao, CancellationToken huy)
        {
            TinhTrangPython tt = await KiemPython();
            if (tt.Python == null) throw new InvalidOperationException(tt.Loi);
            bao("Đang cài ezdxf + matplotlib (pip, cần mạng — khoảng 1 phút)…");
            var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, huy, "-m", "pip", "install", "--user", "--upgrade", "ezdxf", "matplotlib");
            if (ma != 0) throw new InvalidOperationException("pip lỗi: " + DongCuoi(ra));
            return await KiemPython(lai: true);
        }

        /// <summary>Mẫu biên dạng dùng được cho đơn: thư viện chung + mẫu thử của đơn (đè cùng tên). Python chưa sẵn sàng → rỗng.</summary>
        public static async Task<List<(string Ten, string MoTa, string Tep)>> DanhSachMau(string tm)
        {
            var ds = new List<(string, string, string)>();
            TinhTrangPython tt = await KiemPython();
            if (!tt.SanSang) return ds;
            var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, CancellationToken.None, Path.Combine(ThuMucVeDxf, "ve.py"), "--thu-vien", ThuVienChung, ThuVienThu(tm));
            if (ma != 0 || !(JsonLite.Parse(ra.Trim().Split('\n').Last()) is List<object> l)) return ds;
            foreach (JsonObject o in l.OfType<JsonObject>())
                ds.Add((o.GetString("ten") ?? "", o.GetString("mo_ta") ?? "", o.GetString("tep") ?? ""));
            return ds;
        }

        /// <summary>Kết quả vẽ một tấm (tep / anh null khi lỗi).</summary>
        public sealed class KetQuaVe
        {
            public string Bango, Tep, Anh, Loi;
        }

        /// <summary>
        /// Vẽ trải phẳng mọi tấm vẽ được bằng ve_dxf\ve.py trên môi trường moi-truong-ve.dxf → ai\ban_ve\&lt;bango&gt;.dxf (+ .png).
        /// Tệp đã sửa tay trên CAD không bị ghi đè (ve.py ghi &lt;bango&gt; (2).dxf).
        /// </summary>
        public static async Task<List<KetQuaVe>> VeBanVe(IList<(TamAi Tam, TamTrienKhai Tk)> ds, string title, string tm, Action<string> bao, CancellationToken huy)
        {
            TinhTrangPython tt = await KiemPython();
            if (!tt.SanSang) throw new InvalidOperationException(tt.Loi);
            if (!File.Exists(MoiTruongVe)) throw new InvalidOperationException("Thiếu môi trường vẽ " + MoiTruongVe + " (chép tay từ máy Admin).");
            string dir = ThuMucBanVe(tm);
            Directory.CreateDirectory(dir);
            string viec = Path.Combine(dir, "viec.json");
            File.WriteAllText(viec, PhongTrienKhai.ViecVe(title, ds, MoiTruongVe, dir, new[] { ThuVienChung, ThuVienThu(tm) }), Utf8);
            bao("Phòng 4: ezdxf đang vẽ " + ds.Count(x => !x.Tk.LamTay) + " tấm…");
            var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, huy, Path.Combine(ThuMucVeDxf, "ve.py"), viec);
            JsonObject o = null;
            try { o = JsonLite.Parse(ra.Trim().Split('\n').Last()) as JsonObject; } catch (Exception) { }
            if (ma != 0 || o == null) throw new InvalidOperationException("ve.py lỗi: " + DongCuoi(ra));
            return (o.Get("ket_qua") as List<object> ?? new List<object>()).OfType<JsonObject>()
                .Select(x => new KetQuaVe { Bango = x.GetString("bango"), Tep = x.GetString("tep"), Anh = x.GetString("anh"), Loi = x.GetString("loi") }).ToList();
        }

        /// <summary>
        /// AI viết mẫu biên dạng mới cho tấm chưa có mẫu (đọc lại bản vẽ khách + mô tả + ví dụ be.py), kiểm an toàn (KiemMau),
        /// lưu ai\ban_ve\mau_thu\&lt;ten&gt;.py và gán cho tấm. Trả mẫu (Loi khác null = không dùng được).
        /// </summary>
        public static async Task<PhongTrienKhai.MauMoi> DeXuatMau(TamAi tam, TamTrienKhai tk, KetQuaTrienKhai kq, string tm, Action<string> bao, CancellationToken huy)
        {
            string key = KeyPhong(4);
            string model = Model();
            var daCo = (await DanhSachMau(tm)).Select(m => m.Ten).ToList();
            string viDu = File.ReadAllText(Path.Combine(ThuVienChung, "be.py"), Encoding.UTF8);
            string f = Path.Combine(ThuMucTep(tm), tam.Tep), pdf = null;
            if (File.Exists(f) && f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && new FileInfo(f).Length <= PhongDocBanVe.PdfToiDa)
                pdf = Convert.ToBase64String(File.ReadAllBytes(f));
            bao("Phòng 4: AI viết mẫu vẽ mới cho " + tk.Ten + "…");
            string body = PhongTrienKhai.BodyMauMoi(model, pdf, PhongTrienKhai.DauVaoMauMoi(tam, tk, daCo, viDu));
            PhongTrienKhai.MauMoi m = PhongTrienKhai.DocMauMoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, huy), daCo);
            kq.TokenVao += m.Vao + m.GhiCache + m.DocCache;
            kq.TokenRa += m.Ra;
            kq.Tien += PhongDocBanVe.Tien(model, m.Vao, m.GhiCache, m.DocCache, m.Ra);
            if (m.Loi == null)
            {
                Directory.CreateDirectory(ThuVienThu(tm));
                File.WriteAllText(Path.Combine(ThuVienThu(tm), m.Ten + ".py"), m.Ma, Utf8);
                tk.BienDang = m.Ten;
            }
            Ghi(tm, kq);
            return m;
        }

        /// <summary>Chỉ máy Admin: chép mẫu thử của đơn vào thư viện chung (không đè mẫu đã có).</summary>
        public static string ThemVaoThuVien(string tm, string ten)
        {
            if (!LaMayAdmin()) throw new InvalidOperationException("Chỉ máy Admin được thêm mẫu vào thư viện chung.");
            string nguon = Path.Combine(ThuVienThu(tm), ten + ".py"), dich = Path.Combine(ThuVienChung, ten + ".py");
            if (!File.Exists(nguon)) throw new InvalidOperationException("Không thấy mẫu thử " + nguon);
            if (File.Exists(dich)) throw new InvalidOperationException("Thư viện đã có mẫu '" + ten + "'.");
            string loi = PhongTrienKhai.KiemMau(ten, File.ReadAllText(nguon, Encoding.UTF8), new string[0]);
            if (loi != null) throw new InvalidOperationException(loi);
            File.Copy(nguon, dich);
            return dich;
        }

        /// <summary>Chạy lệnh, đợi tối đa 3 phút, trả (mã thoát, stdout + stderr). Huỷ → giết tiến trình.</summary>
        private static Task<(int Ma, string Ra)> ChayLenh(string exe, string thuMuc, CancellationToken huy, params string[] thamSo) =>
            ChayLenh(exe, thuMuc, 180, huy, thamSo);

        private static async Task<(int Ma, string Ra)> ChayLenh(string exe, string thuMuc, int giayToiDa, CancellationToken huy, params string[] thamSo)
        {
            var psi = new System.Diagnostics.ProcessStartInfo(exe, string.Join(" ", thamSo.Select(AiCli.ThoatThamSo)))
            {
                WorkingDirectory = thuMuc, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Utf8, StandardErrorEncoding = Utf8,
            };
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                Task<string> ra = p.StandardOutput.ReadToEndAsync(), loi = p.StandardError.ReadToEndAsync();
                var xong = Task.Run(() => p.WaitForExit(giayToiDa * 1000));
                using (huy.Register(() => { try { p.Kill(); } catch (InvalidOperationException) { } }))
                    if (!await xong) { try { p.Kill(); } catch (InvalidOperationException) { } return (-1, "quá " + giayToiDa / 60 + " phút"); }
                huy.ThrowIfCancellationRequested();
                return (p.ExitCode, (await ra) + (await loi));
            }
        }

        private static string DongCuoi(string s) => (s ?? "").Trim().Split('\n').LastOrDefault()?.Trim() ?? "";

        /// <summary>AI đọc tiết diện từng tấm (theo tệp bản vẽ) — giữ V người đã chọn ở lần trước.</summary>
        public static async Task<KetQuaTrienKhai> TrienKhaiDon(ChiTiet ct, KiemTra kt, string tm, Action<string> bao, CancellationToken huy)
        {
            NapTraV(lai: true);
            KetQuaTrienKhai cu = DocTrienKhai(tm);
            string key = KeyPhong(4);
            string model = Model(), quyChuan = QuyChuan(4);
            var kq = new KetQuaTrienKhai { TuXacNhan = kt.XacNhan, TokenVao = cu?.TokenVao ?? 0, TokenRa = cu?.TokenRa ?? 0, Tien = cu?.Tien ?? 0 };
            var loi = new List<string>();
            var theoTep = ct.Tam.Where(t => t.Moi).GroupBy(t => t.Tep).ToList();
            // mẫu biên dạng để AI chọn (máy chưa có Python / ezdxf → rỗng: AI để trống, vẽ thì tự suy)
            var thuVien = (await DanhSachMau(tm)).Select(m => (m.Ten, m.MoTa)).ToList();
            int i = 0;
            foreach (var nhom in theoTep)
            {
                huy.ThrowIfCancellationRequested();
                bao("Phòng 4: AI đọc tiết diện " + (++i) + "/" + theoTep.Count + " — " + nhom.Key + " (mỗi tệp khoảng 1 phút)");
                string f = Path.Combine(ThuMucTep(tm), nhom.Key);
                string pdf = null, dxf = null;
                try
                {
                    if (f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        if (new FileInfo(f).Length > PhongDocBanVe.PdfToiDa) { loi.Add(nhom.Key + ": PDF quá lớn (> 22 MB)"); continue; }
                        pdf = Convert.ToBase64String(File.ReadAllBytes(f));
                    }
                    else dxf = DocDxf.TomTat(nhom.Key, DocDxf.DocFile(f));
                }
                catch (Exception ex) { loi.Add(nhom.Key + ": " + ex.Message); continue; }
                string body = PhongTrienKhai.Body(model, quyChuan, pdf, PhongTrienKhai.DauVao(nhom.Key, nhom, kt.VanDe, thuVien), dxf);
                PhongTrienKhai.PhanHoi ph = PhongTrienKhai.DocPhanHoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, huy));
                kq.TokenVao += ph.Vao + ph.GhiCache + ph.DocCache;
                kq.TokenRa += ph.Ra;
                kq.Tien += PhongDocBanVe.Tien(model, ph.Vao, ph.GhiCache, ph.DocCache, ph.Ra);
                if (ph.Loi != null) { loi.Add(nhom.Key + ": " + ph.Loi); continue; }
                var daDung = new HashSet<TamTrienKhai>();
                foreach (TamAi tam in nhom)
                {
                    TamTrienKhai tk = PhongTrienKhai.Ghep(tam, ph.Tam, daDung);
                    if (tk == null) { loi.Add(PhongKiemTra.TenTam(tam) + ": AI không trả tiết diện"); continue; }
                    tk.Khoa = tam.Khoa;
                    tk.Ten = PhongKiemTra.TenTam(tam);
                    tk.VChon = cu?.Tam.FirstOrDefault(x => x.Khoa == tam.Khoa)?.VChon ?? "";
                    kq.Tam.Add(tk);
                }
            }
            // AI lỗi ở tệp nào thì giữ kết quả cũ của tấm tệp đó
            if (cu != null) kq.Tam.AddRange(cu.Tam.Where(x => !kq.Tam.Any(y => y.Khoa == x.Khoa) && ct.Tam.Any(t => t.Moi && t.Khoa == x.Khoa)));
            kq.Luc = DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            Ghi(tm, kq);
            if (loi.Count > 0) bao("Triển khai xong, có chỗ chưa chạy được:\n" + string.Join("\n", loi));
            return kq;
        }

        private static readonly HttpClient Http = TaoHttp();

        private static HttpClient TaoHttp()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; // net48 có thể chưa bật TLS 1.2
            return new HttpClient { Timeout = TimeSpan.FromMinutes(10) }; // PDF nhiều trang + effort high: một lần gọi có thể vài phút
        }

        // ---------- Nguồn AI: key API / Claude Code CLI / Antigravity CLI ----------

        /// <summary>Nguồn AI chọn ở ⚙ → 🤖 AI: api (mặc định) / claude_code / antigravity.</summary>
        public static string NguonAi
        {
            get { string v = CaiDatTeams.Doc("ai_nguon", AiCli.Api); return v == AiCli.ClaudeCode || v == AiCli.Antigravity ? v : AiCli.Api; }
            set => CaiDatTeams.Ghi("ai_nguon", value ?? AiCli.Api);
        }

        /// <summary>Lệnh CLI của nguồn (claude / agy): trên PATH hoặc chỗ trình cài đặt để; chưa cài → null.</summary>
        public static string TimCli(string nguon)
        {
            string ten = nguon == AiCli.ClaudeCode ? "claude" : nguon == AiCli.Antigravity ? "agy" : null;
            if (ten == null) return null;
            string nha = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var cho = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';').Where(d => d.Trim().Length > 0).Select(d => d.Trim().Trim('"'))
                      .Concat(new[] { Path.Combine(nha, ".local", "bin"), Path.Combine(local, "agy", "bin"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm") });
            foreach (string dir in cho)
                foreach (string duoi in new[] { ".exe", ".cmd" })
                    try { string f = Path.Combine(dir, ten + duoi); if (File.Exists(f)) return f; }
                    catch (ArgumentException) { } // PATH có ký tự lạ
            return null;
        }

        /// <summary>Key cho phòng khi nguồn là API (thiếu → báo); nguồn CLI không cần key → "".</summary>
        public static string KeyPhong(int phong) =>
            NguonAi != AiCli.Api ? "" : DocKey(phong) ?? throw new InvalidOperationException("Chưa có key Claude: ⚙ → tab 🤖 AI → dán key (hoặc chọn Nguồn AI = Claude Code / Antigravity)");

        /// <summary>Gửi body /v1/messages theo nguồn đang chọn; trả phản hồi dạng API (CLI được đổi về cùng dạng).</summary>
        private static Task<string> GuiAi(string key, bool fallback, string body, CancellationToken ct) =>
            NguonAi == AiCli.Api ? GuiClaude(key, fallback, body, ct) : GuiCli(NguonAi, body, ct);

        /// <summary>
        /// Chạy claude -p / agy -p trong thư mục tạm: yeu_cau.md (lời dặn + quy chuẩn + phần người dùng) + ban_ve.pdf, chỉ cho đọc tệp,
        /// trả JSON theo schema → phản hồi dạng API (AiCli.DoiKetQua). Thư mục tạm xoá sau khi xong.
        /// </summary>
        private static async Task<string> GuiCli(string nguon, string body, CancellationToken ct)
        {
            string cli = TimCli(nguon) ?? throw new InvalidOperationException(
                (nguon == AiCli.ClaudeCode ? "Máy chưa cài Claude Code (lệnh claude)" : "Máy chưa cài Antigravity CLI (lệnh agy)") + " — cài, đăng nhập, hoặc đổi Nguồn AI ở ⚙ → 🤖 AI.");
            AiCli.YeuCau yc = AiCli.TachBody(body);
            string dir = Path.Combine(Path.GetTempPath(), "OHTA_TOOL", "ai_cli", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, AiCli.TepYeuCau), yc.HuongDan, Utf8);
                if (yc.PdfBase64 != null) File.WriteAllBytes(Path.Combine(dir, AiCli.TepPdf), Convert.FromBase64String(yc.PdfBase64));
                var (ma, ra) = await ChayLenh(cli, dir, 900, ct, AiCli.ThamSo(nguon, yc, dir).ToArray());
                return AiCli.DoiKetQua(ra);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } // CLI còn giữ tệp: để %TEMP% tự dọn
            }
        }

        /// <summary>POST /v1/messages như ClaudeHttp của tab AI: 429 / 5xx thử lại 2 lần; trả body kể cả JSON lỗi.</summary>
        private static async Task<string> GuiClaude(string key, bool fallback, string body, CancellationToken ct)
        {
            for (int lan = 0; ; lan++)
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, PhongDocBanVe.Url))
                {
                    if (!req.Headers.TryAddWithoutValidation("x-api-key", key)) throw new InvalidOperationException("API key có ký tự không hợp lệ (⚙ → 🤖 AI → dán lại key)");
                    req.Headers.TryAddWithoutValidation("anthropic-version", PhongDocBanVe.PhienBanApi);
                    if (fallback) req.Headers.TryAddWithoutValidation("anthropic-beta", PhongDocBanVe.BetaFallback);
                    req.Content = new StringContent(body, Utf8, "application/json");
                    using (HttpResponseMessage res = await Http.SendAsync(req, ct))
                    {
                        string text = await res.Content.ReadAsStringAsync();
                        int ma = (int)res.StatusCode;
                        if (!(ma == 429 || ma >= 500) || lan >= 2)
                            return res.IsSuccessStatusCode || text.TrimStart().StartsWith("{", StringComparison.Ordinal)
                                ? text : "{\"type\":\"error\",\"error\":{\"type\":\"http_error\",\"message\":\"Máy chủ AI trả lỗi HTTP " + ma + "\"}}";
                        TimeSpan cho = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2 << lan);
                        await Task.Delay(cho > TimeSpan.FromSeconds(60) ? TimeSpan.FromSeconds(60) : cho, ct);
                    }
                }
            }
        }
    }
}
