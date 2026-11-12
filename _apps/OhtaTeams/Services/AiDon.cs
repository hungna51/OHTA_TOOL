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
            string key = DocKey(2) ?? throw new InvalidOperationException("Chưa có key Claude: ⚙ → tab 🤖 AI → dán key");
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
                PhongDocBanVe.PhanHoi ph = PhongDocBanVe.DocPhanHoi(await GuiClaude(key, PhongDocBanVe.CoFallback(model), body, ct));
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

            string key = dungAi ? DocKey(3) : null;
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
                    PhongKiemTra.PhanHoi ph = PhongKiemTra.DocPhanHoi(await GuiClaude(key, PhongDocBanVe.CoFallback(model), body, huy), nhom.Key);
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

        // ---------- Bản vẽ trải phẳng bằng text-to-cad ----------

        public static string ThuMucBanVe(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, "ban_ve");
        public static string Bango(string title, int stt) => title + "-" + stt; // trùng cột エクスポート用 của trien_khai.csv

        /// <summary>Lệnh cad của text-to-cad (texttocad.dev): trên PATH hoặc %USERPROFILE%\.local\bin\cad.cmd; chưa cài → null.</summary>
        public static string TimCad()
        {
            foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';').Where(d => d.Trim().Length > 0)
                                   .Concat(new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin") }))
                foreach (string ten in new[] { "cad.cmd", "cad.exe" })
                    try { string f = Path.Combine(dir.Trim().Trim('"'), ten); if (File.Exists(f)) return f; }
                    catch (ArgumentException) { } // PATH có ký tự lạ
            return null;
        }

        private static async Task<(int Ma, string Ra)> ChayCad(string cad, string thuMuc, CancellationToken huy, params string[] thamSo)
        {
            var psi = new System.Diagnostics.ProcessStartInfo(cad, string.Join(" ", thamSo.Select(a => "\"" + a + "\"")))
            {
                WorkingDirectory = thuMuc, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Utf8, StandardErrorEncoding = Utf8,
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                Task<string> ra = p.StandardOutput.ReadToEndAsync(), loi = p.StandardError.ReadToEndAsync();
                var xong = Task.Run(() => p.WaitForExit(180000));
                using (huy.Register(() => { try { p.Kill(); } catch (InvalidOperationException) { } }))
                    if (!await xong) { try { p.Kill(); } catch (InvalidOperationException) { } return (-1, "quá 3 phút"); }
                huy.ThrowIfCancellationRequested();
                return (p.ExitCode, (await ra) + (await loi));
            }
        }

        /// <summary>
        /// Vẽ trải phẳng từng tấm bằng text-to-cad: &lt;bango&gt;.py (build123d) → cad run → cad export &lt;bango&gt;.dxf → cad snapshot &lt;bango&gt;.png,
        /// trong ai\ban_ve. Tấm làm tay / thiếu X, Y thì bỏ. Trả số bản vẽ đã xuất.
        /// </summary>
        public static async Task<int> VeBanVe(IList<(TamAi Tam, TamTrienKhai Tk)> ds, string title, string tm, Action<string> bao, CancellationToken huy)
        {
            string cad = TimCad() ?? throw new InvalidOperationException("Máy chưa cài text-to-cad (lệnh cad). Cài: PowerShell → irm https://www.texttocad.dev/install.ps1 | iex");
            string dir = ThuMucBanVe(tm);
            Directory.CreateDirectory(dir);
            var loi = new List<string>();
            int xong = 0;
            for (int i = 0; i < ds.Count; i++)
            {
                huy.ThrowIfCancellationRequested();
                string ma = Bango(title, i + 1), script = PhongTrienKhai.ScriptCad(ma, ds[i].Tk);
                if (script == null) { loi.Add(ma + ": làm tay / chưa có X × Y — không vẽ"); continue; }
                bao("Phòng 4: text-to-cad vẽ " + (i + 1) + "/" + ds.Count + " — " + ma);
                string ten = PhongTrienKhai.TenScript(ma);
                File.WriteAllText(Path.Combine(dir, ten + ".py"), script, Utf8);
                var (m1, r1) = await ChayCad(cad, dir, huy, "run", ten + ".py");
                var id = System.Text.RegularExpressions.Regex.Match(r1, @"(?m)^\s*id:\s*(\S+)");
                if (m1 != 0 || !id.Success) { loi.Add(ma + ": cad run lỗi — " + DongCuoi(r1)); continue; }
                var (m2, r2) = await ChayCad(cad, dir, huy, "export", id.Groups[1].Value, ma + ".dxf");
                if (m2 != 0) { loi.Add(ma + ": cad export lỗi — " + DongCuoi(r2)); continue; }
                await ChayCad(cad, dir, huy, "snapshot", ma + ".dxf", ma + ".png", "--width", "900", "--height", "600"); // ảnh xem trước: lỗi thì thôi
                xong++;
            }
            if (loi.Count > 0) bao("Vẽ xong " + xong + " bản, có tấm chưa vẽ được:\n" + string.Join("\n", loi));
            return xong;
        }

        private static string DongCuoi(string s) => (s ?? "").Trim().Split('\n').LastOrDefault()?.Trim() ?? "";

        /// <summary>AI đọc tiết diện từng tấm (theo tệp bản vẽ) — giữ V người đã chọn ở lần trước.</summary>
        public static async Task<KetQuaTrienKhai> TrienKhaiDon(ChiTiet ct, KiemTra kt, string tm, Action<string> bao, CancellationToken huy)
        {
            NapTraV(lai: true);
            KetQuaTrienKhai cu = DocTrienKhai(tm);
            string key = DocKey(4) ?? throw new InvalidOperationException("Chưa có key Claude: ⚙ → tab 🤖 AI → dán key");
            string model = Model(), quyChuan = QuyChuan(4);
            var kq = new KetQuaTrienKhai { TuXacNhan = kt.XacNhan, TokenVao = cu?.TokenVao ?? 0, TokenRa = cu?.TokenRa ?? 0, Tien = cu?.Tien ?? 0 };
            var loi = new List<string>();
            var theoTep = ct.Tam.Where(t => t.Moi).GroupBy(t => t.Tep).ToList();
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
                string body = PhongTrienKhai.Body(model, quyChuan, pdf, PhongTrienKhai.DauVao(nhom.Key, nhom, kt.VanDe), dxf);
                PhongTrienKhai.PhanHoi ph = PhongTrienKhai.DocPhanHoi(await GuiClaude(key, PhongDocBanVe.CoFallback(model), body, huy));
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
