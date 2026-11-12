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

        /// <summary>
        /// 4 bộ quy chuẩn (quy_chuan\&lt;tên&gt;) theo việc. Từ 12/11 còn 3 phòng: 1 Nhận &amp; Đọc gửi bộ đọc + kiểm tra + triển khai trong một lượt,
        /// 2 Kiểm tra &amp; Hỏi dùng bộ kiểm tra khi kiểm lại bằng chữ, 3 Triển khai &amp; Vẽ dùng bộ triển khai khi AI viết mẫu vẽ.
        /// </summary>
        public static readonly string[] ThuMucPhong = { "1_tiep_nhan", "2_doc_ban_ve", "3_kiem_tra", "4_trien_khai" };
        public static readonly string[] TenPhong = { "Tiếp nhận (không gửi AI)", "Đọc bản vẽ (phòng 1)", "Kiểm tra (phòng 1 + 2)", "Triển khai (phòng 1 + 3)" };
        /// <summary>Key riêng: tệp ai.key.phong2/3/4 (giữ tên cũ) ↔ phòng mới 1/2/3.</summary>
        public static readonly (int Tep, string Ten)[] KeyTheoPhong = { (2, "1 · Nhận & Đọc"), (3, "2 · Kiểm tra & Hỏi"), (4, "3 · Triển khai & Vẽ") };

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
            return string.Join("\n\n", Directory.GetFiles(tm, "*.md").Where(f => !string.Equals(Path.GetFileName(f), PhongBaiHoc.TenTep, StringComparison.OrdinalIgnoreCase))
                                                           .OrderBy(f => f).Select(f => File.ReadAllText(f, Encoding.UTF8)));
        }

        /// <summary>Quy chuẩn lượt đọc bản vẽ (đọc + kiểm tra + triển khai, bỏ trùng khi máy chưa chia bộ) + bài học đã duyệt.</summary>
        public static string QuyChuanDoc(bool kemBaiHoc = true)
        {
            string qc = string.Join("\n\n", new[] { 2, 3, 4 }.Select(QuyChuan).Where(s => s.Length > 0).Distinct());
            string bh = kemBaiHoc ? string.Join("\n", PhongBaiHoc.CacBaiHoc(DocBaiHoc()).Select(b => "- " + b)) : "";
            return bh.Length == 0 ? qc : qc + "\n\n## BÀI HỌC TỪ CÁC LẦN NGƯỜI SỬA (ưu tiên áp dụng)\n" + bh;
        }

        // ---------- Bài học ----------

        /// <summary>Thư mục bài học dùng chung (OneDrive / SharePoint) chọn ở ⚙ → 🤖 AI; "" = tệp trên máy (quy chuẩn phòng đọc).</summary>
        public static string ThuMucBaiHocChung
        {
            get => CaiDatTeams.Doc("ai_bai_hoc_dir");
            set => CaiDatTeams.Ghi("ai_bai_hoc_dir", (value ?? "").Trim().Trim('"'));
        }

        public static string FileBaiHoc
        {
            get
            {
                string chung = ThuMucBaiHocChung;
                return Path.Combine(chung.Length > 0 && Directory.Exists(chung) ? chung : ThuMucQuyChuanPhong(2), PhongBaiHoc.TenTep);
            }
        }

        public static string DocBaiHoc()
        {
            try { return File.Exists(FileBaiHoc) ? File.ReadAllText(FileBaiHoc, Encoding.UTF8) : ""; }
            catch (IOException) { return ""; } // thư mục chung đang đồng bộ: lần này đọc không có bài học
        }

        /// <summary>
        /// AI xem các chỗ người sửa (chưa gửi) → đề xuất bài học (chờ duyệt) vào ct.BaiHoc. kemPdf: gửi kèm bản vẽ (một lượt mỗi tệp),
        /// không thì một lượt chữ cho cả đơn. Trả số đề xuất mới.
        /// </summary>
        public static async Task<int> RutBaiHoc(TiepNhan tn, ChiTiet ct, string tm, bool kemPdf, Action<string> bao, CancellationToken huy)
        {
            List<ChoSua> choSua = PhongBaiHoc.CacChoSua(ct);
            if (choSua.Count == 0) return 0;
            string key = KeyPhong(3), model = Model(), quyChuan = QuyChuanDoc(kemBaiHoc: false);
            List<string> cu = PhongBaiHoc.CacBaiHoc(DocBaiHoc());
            var nhom = kemPdf ? choSua.GroupBy(c => c.Tam.Tep).Select(g => g.ToList()).ToList() : new List<List<ChoSua>> { choSua };
            int moi = 0, i = 0;
            var loi = new List<string>();
            foreach (List<ChoSua> ds in nhom)
            {
                huy.ThrowIfCancellationRequested();
                bao("AI rút bài học từ " + ds.Count + " chỗ sửa" + (kemPdf ? " — " + ds[0].Tam.Tep + " (" + (++i) + "/" + nhom.Count + ")" : "") + "…");
                string pdf = null, f = Path.Combine(ThuMucTep(tm), ds[0].Tam.Tep);
                if (kemPdf && f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && File.Exists(f) && new FileInfo(f).Length <= PhongDocBanVe.PdfToiDa)
                    pdf = Convert.ToBase64String(File.ReadAllBytes(f));
                string body = PhongBaiHoc.Body(model, quyChuan, pdf, PhongBaiHoc.DauVao(ds, tn?.CongTy, cu));
                PhongBaiHoc.PhanHoi ph = PhongBaiHoc.DocPhanHoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, huy));
                ct.TokenVao += ph.Vao + ph.GhiCache + ph.DocCache;
                ct.TokenRa += ph.Ra;
                ct.Tien += PhongDocBanVe.Tien(model, ph.Vao, ph.GhiCache, ph.DocCache, ph.Ra);
                if (ph.Loi != null) { loi.Add(ph.Loi); continue; }
                foreach (DeXuatBaiHoc d in ph.BaiHoc.Where(d => !ct.BaiHoc.Any(x => x.TrangThai != "bo" && x.NoiDung == d.NoiDung))) { ct.BaiHoc.Add(d); moi++; }
                ct.DaRutBaiHoc.UnionWith(ds.Select(c => c.ChuKy));
            }
            Ghi(tm, ct);
            if (loi.Count > 0) throw new InvalidOperationException("AI chưa rút được bài học: " + string.Join("; ", loi.Distinct()));
            return moi;
        }

        /// <summary>Chỉ máy Admin: ghi bài học đã duyệt vào bai_hoc.md (gộp vào dòng trùng ý nếu AI chỉ ra).</summary>
        public static void ThemBaiHoc(ChiTiet ct, string tm, DeXuatBaiHoc d)
        {
            if (!LaMayAdmin()) throw new InvalidOperationException("Chỉ máy Admin được thêm bài học.");
            string f = FileBaiHoc;
            GhiAnToan(f, PhongBaiHoc.ApDung(File.Exists(f) ? File.ReadAllText(f, Encoding.UTF8) : "", d));
            d.TrangThai = "them";
            Ghi(tm, ct);
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
            string model = Model(), quyChuan = QuyChuanDoc();
            ChiTiet cu = DocChiTiet(tm) ?? new ChiTiet();
            var ct2 = new ChiTiet
            {
                TokenVao = cu.TokenVao, TokenRa = cu.TokenRa, Tien = cu.Tien, MaBamDaDoc = new Dictionary<string, string>(cu.MaBamDaDoc),
                BaiHoc = cu.BaiHoc, DaRutBaiHoc = cu.DaRutBaiHoc,
            };
            // Một lượt đọc trả cả thông số + tiết diện + nghi vấn; mẫu biên dạng để AI chọn (máy chưa có Python / ezdxf → rỗng)
            string them = PhongDocBanVe.DauVao(tn.CongTy, (await DanhSachMau(tm)).Select(m => (m.Ten, m.MoTa + (m.ThamSoChu.Length > 0 ? " | thông số: " + m.ThamSoChu : ""))));
            var nghiVan = new List<VanDe>();

            var canDoc = tn.Tep.Where(t => t.Loai == PhongTiepNhan.BanVe && (t.Ten.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || t.Ten.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase)))
                               .Where(t => tatCa || !ct2.MaBamDaDoc.TryGetValue(t.Ten, out string mb) || mb != t.MaBam).ToList();
            var moi = new List<TamAi>();
            var docLai = new HashSet<string>();
            var loi = new List<string>();
            int i = 0;
            foreach (TepDon t in canDoc)
            {
                ct.ThrowIfCancellationRequested();
                bao("Phòng 1: AI đọc " + (++i) + "/" + canDoc.Count + " — " + t.Ten + " (thông số + tiết diện + nghi vấn, mỗi tệp 1–2 phút)");
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
                string body = PhongDocBanVe.Body(model, PhongDocBanVe.LoiDan, quyChuan, t.Ten, pdf, dxf, them);
                PhongDocBanVe.PhanHoi ph = PhongDocBanVe.DocPhanHoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, ct), t.Ten);
                ct2.TokenVao += ph.Vao + ph.GhiCache + ph.DocCache;
                ct2.TokenRa += ph.Ra;
                ct2.Tien += PhongDocBanVe.Tien(model, ph.Vao, ph.GhiCache, ph.DocCache, ph.Ra);
                if (ph.Loi != null) { loi.Add(t.Ten + ": " + ph.Loi); continue; }
                foreach (TamAi tam in ph.Tam) tam.Tep = t.Ten;
                moi.AddRange(ph.Tam);
                nghiVan.AddRange(ph.VanDe);
                ct2.GhiChuDon[t.Ten] = ph.GhiChuDon;
                docLai.Add(t.Ten);
                ct2.MaBamDaDoc[t.Ten] = t.MaBam;
            }
            // Tệp đã bị bỏ khỏi đơn / đổi sang loại khác → bỏ tấm của nó
            var conLai = new HashSet<string>(tn.Tep.Where(t => t.Loai == PhongTiepNhan.BanVe).Select(t => t.Ten));
            ct2.Tam = PhongDocBanVe.Gop(cu.Tam.Where(x => conLai.Contains(x.Tep)).ToList(), moi, docLai);
            // Thứ tự bản vẽ như danh sách tệp, trong bản vẽ giữ thứ tự AI đọc → tên tấm <đơn>-n đánh theo thứ tự này (OrderBy giữ ổn định)
            var thuTu = tn.Tep.Select((t, i) => (t.Ten, i)).GroupBy(x => x.Ten).ToDictionary(g => g.Key, g => g.First().i);
            ct2.Tam = ct2.Tam.OrderBy(x => thuTu.TryGetValue(x.Tep, out int i) ? i : int.MaxValue).ToList();
            foreach (var kv in cu.GhiChuDon.Where(kv => conLai.Contains(kv.Key) && !docLai.Contains(kv.Key))) ct2.GhiChuDon[kv.Key] = kv.Value;
            // Nghi vấn: tệp vừa đọc lại lấy mới, tệp không đọc lại giữ cũ (mã nghi vấn = "ai|<tệp>|…")
            bool CuaTep(VanDe v, string tep) => v.Ma.StartsWith("ai|" + tep.ToLowerInvariant() + "|", StringComparison.Ordinal);
            ct2.NghiVan = cu.NghiVan.Where(v => conLai.Any(f => CuaTep(v, f)) && !docLai.Any(f => CuaTep(v, f))).Concat(nghiVan).ToList();
            // Bảng đổi (đọc lại / bỏ tệp) → phải xác nhận lại trước khi sang Triển khai
            ct2.XacNhan = docLai.Count == 0 && ct2.Tam.Count == cu.Tam.Count ? cu.XacNhan : "";
            Ghi(tm, ct2);
            if (loi.Count > 0) bao("Có tệp chưa đọc được:\n" + string.Join("\n", loi));
            return ct2;
        }

        // ---------- Phòng 2: Kiểm tra & Hỏi ----------

        /// <summary>
        /// Luật C# (bảng TV + catalog như tab Fname) trên bảng tấm hiện tại + nghi vấn AI thấy khi đọc + vấn đề AI kiểm lại bằng chữ (giữ từ lần trước).
        /// Không gọi AI, chạy ngay — gọi sau mỗi lần đọc / sửa bảng. Giữ trạng thái, câu trả lời người dùng (theo mã vấn đề).
        /// </summary>
        public static KiemTra KiemLuat(ChiTiet ct, string tm, string title)
        {
            KiemTra cu = DocKiemTra(tm);
            var vanDe = PhongKiemTra.Luat(ct.Tam.Where(t => t.Moi), PhongKiemTra.DocTv(BangFname("trav_90")), BangFname("catalog"));
            vanDe.AddRange(ct.NghiVan);
            // "ai" = kiểm lại bằng chữ (và AI rà bản vẽ của bản 4 phòng cũ) — chỉ có trong kiem_tra.json
            if (cu != null) vanDe.AddRange(cu.VanDe.Where(v => v.Nguon == "ai"));
            var kt = new KiemTra
            {
                TokenVao = cu?.TokenVao ?? 0, TokenRa = cu?.TokenRa ?? 0, Tien = cu?.Tien ?? 0,
                VanDe = PhongKiemTra.Gop(cu?.VanDe, vanDe.GroupBy(v => v.Ma).Select(g => g.First()).ToList()),
                Luc = DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            };
            Ghi(tm, kt);
            GhiHoSo(ct, kt, tm, title);
            return kt;
        }

        // ---------- Hồ sơ tấm (ai\ho_so\<đơn>-n.md + .png) ----------

        public static string ThuMucHoSo(string tm) => Path.Combine(tm, PhongTiepNhan.ThuMucAi, PhongHoSo.ThuMuc);

        /// <summary>
        /// Ghi hồ sơ md mỗi tấm mới + _don.md từ chi_tiet.json / kiem_tra.json (ds = kết quả trải phẳng khi đã xác nhận).
        /// Gọi sau mỗi lần kiểm luật / trả lời / triển khai — chỉ ghi tệp có nội dung đổi.
        /// </summary>
        public static void GhiHoSo(ChiTiet ct, KiemTra kt, string tm, string title, IList<(TamAi Tam, TamTrienKhai Tk)> ds = null)
        {
            string dir = ThuMucHoSo(tm);
            Directory.CreateDirectory(dir);
            var vanDe = kt?.VanDe ?? new List<VanDe>();
            var danhSo = PhongHoSo.DanhSo(ct, title);
            foreach (var (t, ten) in danhSo)
            {
                string anh = File.Exists(Path.Combine(dir, ten + ".png")) ? ten + ".png" : null;
                TamTrienKhai tk = ds?.FirstOrDefault(x => ReferenceEquals(x.Tam, t)).Tk;
                GhiNeuDoi(Path.Combine(dir, ten + ".md"), PhongHoSo.Md(ten, t, vanDe.Where(v => PhongHoSo.CuaTam(v, t, ct)), tk, anh));
            }
            var chung = vanDe.Where(v => !danhSo.Any(x => PhongHoSo.CuaTam(v, x.Tam, ct)));
            GhiNeuDoi(Path.Combine(dir, PhongHoSo.TepDon), PhongHoSo.MdDon(title, DocTiepNhan(tm)?.CongTy, ct, chung));
        }

        private static void GhiNeuDoi(string f, string noiDung)
        {
            try
            {
                if (!File.Exists(f) || File.ReadAllText(f, Encoding.UTF8) != noiDung) GhiAnToan(f, noiDung);
            }
            catch (IOException) { } // tệp md đang mở khoá: lần sau ghi lại, dữ liệu gốc vẫn ở JSON
        }

        /// <summary>
        /// Cắt ảnh từng tấm mới từ bản vẽ khách (vùng AI đọc) → ho_so\<đơn>-n.png bằng ve.py --cat-anh (PyMuPDF / ezdxf).
        /// Máy chưa có Python / PyMuPDF → trả câu báo, không lỗi. Trả null khi cắt xong hết.
        /// </summary>
        public static async Task<string> CatAnh(ChiTiet ct, string tm, string title, CancellationToken huy)
        {
            TinhTrangPython tt = await KiemPython();
            if (tt.Python == null) return "Chưa cắt ảnh tấm: " + tt.Loi;
            string dir = ThuMucHoSo(tm);
            Directory.CreateDirectory(dir);
            var viec = new List<object>();
            foreach (var (t, ten) in PhongHoSo.DanhSo(ct, title))
            {
                string f = Path.Combine(ThuMucTep(tm), t.Tep);
                bool pdf = f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
                if (!File.Exists(f) || (pdf && !tt.Pymupdf) || (!pdf && tt.Ezdxf == null)) continue;
                var o = new JsonObject();
                o.Set("nguon", f);
                o.Set("trang", new JsonNumber(((int)(PhongKiemTra.So(t.Trang) ?? 1)).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                if (!string.IsNullOrEmpty(t.VungJson)) o.Set("vung", JsonLite.Parse(t.VungJson));
                o.Set("ra", Path.Combine(dir, ten + ".png"));
                viec.Add(o);
            }
            string bao = !tt.Pymupdf && ct.Tam.Any(t => t.Tep.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                ? "Chưa cắt ảnh tấm từ PDF: máy chưa có PyMuPDF — bấm 📐 Vẽ DXF ở phòng 3 để cài (hoặc pip install pymupdf)." : null;
            if (viec.Count == 0) return bao;
            string tep = Path.Combine(dir, "anh.json");
            File.WriteAllText(tep, JsonLite.Serialize(viec), Utf8);
            var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, huy, Path.Combine(ThuMucVeDxf, "ve.py"), "--cat-anh", tep);
            List<object> kq = null;
            try { kq = JsonLite.Parse(ra.Trim().Split('\n').Last()) as List<object>; } catch (Exception) { }
            if (ma != 0 || kq == null) return "Chưa cắt được ảnh tấm: " + DongCuoi(ra);
            var loi = kq.OfType<JsonObject>().Where(x => x.GetString("loi") != null).Select(x => Path.GetFileName(x.GetString("ra")) + ": " + x.GetString("loi")).ToList();
            return loi.Count > 0 ? "Có ảnh tấm chưa cắt được:\n" + string.Join("\n", loi) : bao;
        }

        /// <summary>
        /// Lượt AI đọc PHÓNG TO (12/11): mỗi tấm mới đọc (chưa DaPhongTo, có vùng + ảnh cắt từ PDF) gửi ảnh cắt 250 dpi + dữ liệu đã đọc
        /// cho AI đọc kỹ lại số, lỗ, ケガキ, dim khách; bỏ nghi vấn đã rõ, thêm nghi vấn mới. Trả câu báo (null = không có gì để làm).
        /// </summary>
        public static async Task<string> DocPhongTo(ChiTiet ct, string tm, string title, Action<string> bao, CancellationToken huy)
        {
            string dir = ThuMucHoSo(tm);
            var ds = PhongHoSo.DanhSo(ct, title)
                .Where(x => !x.Tam.DaPhongTo && !string.IsNullOrEmpty(x.Tam.VungJson) && x.Tam.Tep.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(dir, x.Ten + ".png")))
                .ToList();
            if (ds.Count == 0) return null;
            string key = KeyPhong(2), model = Model(), quyChuan = QuyChuan(2);
            var thuVien = await DanhSachMau(tm);
            var loi = new List<string>();
            int sua = 0, i = 0;
            foreach (var (t, ten) in ds)
            {
                huy.ThrowIfCancellationRequested();
                bao("Phòng 1: AI đọc phóng to " + (++i) + "/" + ds.Count + " — " + ten + " (ảnh cắt vùng tấm)…");
                // nghi vấn của tấm + nghi vấn chung của tệp (AI có thể đã trả lời được khi nhìn rõ)
                string dauTep = "ai|" + t.Tep.ToLowerInvariant() + "|";
                var nv = ct.NghiVan.Where(v => PhongHoSo.CuaTam(v, t, ct) || (v.Tam.Trim().Length == 0 && v.Ma.StartsWith(dauTep, StringComparison.Ordinal))).ToList();
                string moTaMau = thuVien.FirstOrDefault(m => m.Ten == t.BienDang)?.ThamSoChu ?? "";
                string body = PhongPhongTo.Body(model, quyChuan, Convert.ToBase64String(File.ReadAllBytes(Path.Combine(dir, ten + ".png"))), PhongPhongTo.DauVao(t, moTaMau, nv));
                PhongPhongTo.PhanHoi ph = PhongPhongTo.DocPhanHoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, huy));
                ct.TokenVao += ph.Vao + ph.GhiCache + ph.DocCache;
                ct.TokenRa += ph.Ra;
                ct.Tien += PhongDocBanVe.Tien(model, ph.Vao, ph.GhiCache, ph.DocCache, ph.Ra);
                if (ph.Loi != null) { loi.Add(ten + ": " + ph.Loi); continue; }
                sua += PhongPhongTo.ApDung(t, ph.Nd).Count;
                var daRo = PhongPhongTo.NghiVanDaRo(ph.Nd);
                for (int k = 0; k < nv.Count; k++) if (daRo.Contains(k + 1)) ct.NghiVan.Remove(nv[k]);
                if (ph.Nd.Get("van_de") is List<object> vd)
                    foreach (VanDe v in PhongKiemTra.DocDsVanDe(vd, t.Tep, "doc").Where(v => !ct.NghiVan.Any(x => x.Ma == v.Ma))) ct.NghiVan.Add(v);
                t.DaPhongTo = true;
            }
            Ghi(tm, ct);
            return "AI đọc phóng to " + (ds.Count - loi.Count) + " tấm, sửa " + sua + " ô." + (loi.Count > 0 ? "\nChưa đọc phóng to được: " + string.Join("; ", loi) : "");
        }

        /// <summary>
        /// 🔄 Cập nhật theo câu trả lời: AI đọc câu trả lời + thông số tấm (chỉ chữ — chạy được bằng Ling) và áp các ô cần đổi;
        /// câu đã áp → ✓ Đã xử lý; câu mơ hồ → AI hỏi lại. Chạy lại luật + ghi hồ sơ. Trả danh sách ô đã đổi.
        /// </summary>
        public static async Task<PhongCapNhat.KetQua> CapNhatTheoTraLoi(ChiTiet ct, string tm, string title, Action<string> bao, CancellationToken huy)
        {
            KiemTra kt = KiemLuat(ct, tm, title);
            List<VanDe> traLoi = PhongCapNhat.CauDaTraLoi(kt.VanDe.Where(v => v.ConMo));
            if (traLoi.Count == 0) throw new InvalidOperationException("Chưa có câu trả lời mới nào (câu còn mở có ô Trả lời) để cập nhật.");
            string key = KeyPhong(3), model = Model();
            var danhSo = PhongHoSo.DanhSo(ct, title);
            var thuVien = (await DanhSachMau(tm)).Select(m => m.Ten + ": " + m.MoTa + (m.ThamSoChu.Length > 0 ? " | thông số: " + m.ThamSoChu : ""));
            bao("Phòng 2: AI cập nhật thông số theo " + traLoi.Count + " câu trả lời (chỉ gửi chữ)…");
            string body = PhongCapNhat.Body(model, QuyChuan(3), PhongCapNhat.DauVao(danhSo, thuVien, traLoi));
            PhongCapNhat.KetQua kq = PhongCapNhat.ApDung(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, huy), danhSo);
            kt.TokenVao += kq.Vao + kq.GhiCache + kq.DocCache;
            kt.TokenRa += kq.Ra;
            kt.Tien += PhongDocBanVe.Tien(model, kq.Vao, kq.GhiCache, kq.DocCache, kq.Ra);
            if (kq.Loi != null) { Ghi(tm, kt); throw new InvalidOperationException("AI chưa cập nhật được: " + kq.Loi); }
            for (int i = 0; i < traLoi.Count; i++)
                if (kq.DaXuLy.Contains(i + 1)) traLoi[i].TrangThai = VanDe.Xong;
            kt.VanDe.AddRange(kq.VanDeMoi.Where(v => !kt.VanDe.Any(x => x.Ma == v.Ma)));
            Ghi(tm, kt);   // trạng thái câu đã áp + câu hỏi lại — KiemLuat dưới giữ theo mã
            Ghi(tm, ct);
            KiemLuat(ct, tm, title); // luật chạy lại trên thông số mới + ghi hồ sơ md
            return kq;
        }

        /// <summary>
        /// Kiểm lại sau khi có câu trả lời: luật + AI đọc bảng tấm và câu trả lời (CHỈ GỬI CHỮ, không gửi bản vẽ) theo QC kiểm tra,
        /// thêm vấn đề mới phát sinh. Chưa có câu trả lời nào → chỉ chạy luật.
        /// </summary>
        public static async Task<KiemTra> KiemLaiAi(TiepNhan tn, ChiTiet ct, string tm, string title, Action<string> bao, CancellationToken huy)
        {
            KiemTra kt = KiemLuat(ct, tm, title);
            if (!kt.VanDe.Any(v => v.TraLoi.Trim().Length > 0)) { bao("Chưa có câu trả lời nào — đã chạy lại luật, không gọi AI."); return kt; }
            string key = KeyPhong(3), model = Model();
            bao("Phòng 2: AI xem câu trả lời (chỉ gửi chữ)…");
            string body = PhongKiemTra.Body(model, QuyChuan(3), null, PhongKiemTra.DauVao("(cả đơn)", tn?.CongTy, ct.Tam.Where(t => t.Moi), kt.VanDe, kt.VanDe), null, PhongKiemTra.LoiDanChu);
            PhongKiemTra.PhanHoi ph = PhongKiemTra.DocPhanHoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, huy), "don");
            kt.TokenVao += ph.Vao + ph.GhiCache + ph.DocCache;
            kt.TokenRa += ph.Ra;
            kt.Tien += PhongDocBanVe.Tien(model, ph.Vao, ph.GhiCache, ph.DocCache, ph.Ra);
            if (ph.Loi != null) { Ghi(tm, kt); throw new InvalidOperationException("AI chưa kiểm lại được: " + ph.Loi); }
            var moi = ph.VanDe.Where(v => !kt.VanDe.Any(x => x.Ma == v.Ma)).GroupBy(v => v.Ma).Select(g => g.First()).ToList();
            kt.VanDe.AddRange(moi);
            Ghi(tm, kt);
            GhiHoSo(ct, kt, tm, title);
            bao("Kiểm lại xong: AI thêm " + moi.Count + " vấn đề mới.");
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
        /// Dữ liệu triển khai từ bảng tấm đã xác nhận (tiết diện đọc cùng lượt đọc, người đã sửa) — không gọi AI.
        /// Giữ V người đã chọn, token. Đơn đọc bằng bản cũ: tiết diện phòng Triển khai cũ đã đọc được chép vào bảng tấm.
        /// Chỉ ghi trien_khai.json khi đổi (giờ tệp dùng để báo DXF cũ).
        /// </summary>
        public static KetQuaTrienKhai TaoTrienKhai(ChiTiet ct, string tm)
        {
            KetQuaTrienKhai cu = DocTrienKhai(tm);
            var kq = new KetQuaTrienKhai { TuXacNhan = ct.XacNhan, Luc = cu?.Luc ?? "", TokenVao = cu?.TokenVao ?? 0, TokenRa = cu?.TokenRa ?? 0, Tien = cu?.Tien ?? 0 };
            bool doiCt = false;
            foreach (TamAi tam in ct.Tam.Where(t => t.Moi))
            {
                TamTrienKhai c = cu?.Tam.FirstOrDefault(x => x.Khoa == tam.Khoa);
                if (!PhongTrienKhai.CoTietDien(tam) && c != null && (c.Kieu == "lam_tay" || c.X.Doan.Count > 0 || c.Y.Doan.Count > 0))
                {
                    PhongTrienKhai.ChepVaoTam(c, tam);
                    doiCt = true;
                }
                TamTrienKhai tk = PhongTrienKhai.TuTam(tam);
                tk.VChon = c?.VChon ?? "";
                kq.Tam.Add(tk);
            }
            if (doiCt) Ghi(tm, ct);
            string json = PhongTrienKhai.Ghi(kq);
            if (cu == null || json != PhongTrienKhai.Ghi(cu))
            {
                kq.Luc = DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                Ghi(tm, kq);
            }
            return kq;
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
            public bool Matplotlib, Pymupdf;
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
                        kq = new TinhTrangPython { Python = o.GetString("python"), Ezdxf = o.GetString("ezdxf"), Matplotlib = o.Get("matplotlib") is bool b && b, Pymupdf = o.Get("pymupdf") is bool p && p };
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
            bao("Đang cài ezdxf + matplotlib + PyMuPDF (pip, cần mạng — khoảng 1 phút)…");
            var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, huy, "-m", "pip", "install", "--user", "--upgrade", "ezdxf", "matplotlib", "pymupdf");
            if (ma != 0) throw new InvalidOperationException("pip lỗi: " + DongCuoi(ra));
            return await KiemPython(lai: true);
        }

        /// <summary>Một mẫu biên dạng: tên, mô tả, tệp, thông số (tên, ý nghĩa, mặc định — null = bắt buộc), số ví dụ, ảnh hướng dẫn.</summary>
        public sealed class MauVe
        {
            public string Ten = "", MoTa = "", Tep = "", Anh, Loi;
            public List<(string Ten, string YNghia, string MacDinh)> ThamSo = new List<(string, string, string)>();
            public JsonObject ViDu = new JsonObject();
            /// <summary>Mẫu thử của đơn (chưa vào thư viện chung).</summary>
            public bool LaMauThu;

            /// <summary>Thông số riêng AI cần đọc (bỏ x, y… chương trình tự tính) — "r1 (ý nghĩa; mặc định y/2), …".</summary>
            public string ThamSoChu => string.Join(", ", ThamSo.Where(x => x.MacDinh != "tự tính")
                .Select(x => x.Ten + " (" + x.YNghia + (x.MacDinh == null ? "; bắt buộc" : "; mặc định " + x.MacDinh) + ")"));
        }

        /// <summary>Mẫu biên dạng dùng được cho đơn: thư viện chung + mẫu thử của đơn (đè cùng tên). Python chưa sẵn sàng → rỗng.</summary>
        public static async Task<List<MauVe>> DanhSachMau(string tm)
        {
            var ds = new List<MauVe>();
            TinhTrangPython tt = await KiemPython();
            if (!tt.SanSang) return ds;
            var thuMuc = new List<string> { ThuVienChung };
            if (tm != null) thuMuc.Add(ThuVienThu(tm));
            var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, CancellationToken.None, new[] { Path.Combine(ThuMucVeDxf, "ve.py"), "--thu-vien" }.Concat(thuMuc).ToArray());
            if (ma != 0 || !(JsonLite.Parse(ra.Trim().Split('\n').Last()) is List<object> l)) return ds;
            foreach (JsonObject o in l.OfType<JsonObject>())
            {
                var m = new MauVe
                {
                    Ten = o.GetString("ten") ?? "", MoTa = o.GetString("mo_ta") ?? "", Tep = o.GetString("tep") ?? "", Anh = o.GetString("anh"), Loi = o.GetString("loi"),
                    ViDu = o.Get("vi_du") as JsonObject ?? new JsonObject(),
                };
                m.LaMauThu = tm != null && m.Tep.StartsWith(ThuVienThu(tm), StringComparison.OrdinalIgnoreCase);
                foreach (List<object> ts in (o.Get("tham_so") as List<object> ?? new List<object>()).OfType<List<object>>())
                    m.ThamSo.Add((ts.Count > 0 ? ts[0] as string ?? "" : "", ts.Count > 1 ? ts[1] as string ?? "" : "", ts.Count > 2 ? ts[2] as string : null));
                ds.Add(m);
            }
            return ds;
        }

        /// <summary>Vẽ lại ảnh hướng dẫn &lt;ten&gt;.png cho mọi mẫu trong thư mục (thư viện chung / mẫu thử). Trả lỗi (rỗng = được).</summary>
        public static async Task<string> TaoAnhHuongDan(string thuMuc, CancellationToken huy)
        {
            TinhTrangPython tt = await KiemPython();
            if (!tt.SanSang) return tt.Loi;
            if (!File.Exists(MoiTruongVe)) return "Thiếu môi trường vẽ " + MoiTruongVe;
            var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, huy, Path.Combine(ThuMucVeDxf, "ve.py"), "--huong-dan", MoiTruongVe, thuMuc);
            if (ma != 0) return "ve.py lỗi: " + DongCuoi(ra);
            List<object> kq = null;
            try { kq = JsonLite.Parse(ra.Trim().Split('\n').Last()) as List<object>; } catch (Exception) { }
            return string.Join("; ", (kq ?? new List<object>()).OfType<JsonObject>().Where(x => x.GetString("loi") != null).Select(x => x.GetString("ten") + ": " + x.GetString("loi")));
        }

        /// <summary>
        /// Vẽ thử một mẫu với số người nhập (cửa sổ Thư viện mẫu): x, y, thông số riêng + đường bẻ / đoạn của số ví dụ.
        /// Ra %TEMP%\OHTA_TOOL\thu_mau\thu-&lt;ten&gt;.dxf (+ .png). Trả kết quả vẽ.
        /// </summary>
        public static async Task<KetQuaVe> VeThuMau(MauVe m, double x, double y, IDictionary<string, double> thamSo, string tm, CancellationToken huy)
        {
            TinhTrangPython tt = await KiemPython();
            if (!tt.SanSang) throw new InvalidOperationException(tt.Loi);
            if (!File.Exists(MoiTruongVe)) throw new InvalidOperationException("Thiếu môi trường vẽ " + MoiTruongVe + " (chép tay từ máy Admin).");
            string dir = Path.Combine(Path.GetTempPath(), "OHTA_TOOL", "thu_mau");
            Directory.CreateDirectory(dir);
            var tam = new JsonObject();
            tam.Set("bango", "thu-" + m.Ten);
            tam.Set("bien_dang", m.Ten);
            tam.Set("x", new JsonNumber(PhongTrienKhai.F(x)));
            tam.Set("y", new JsonNumber(PhongTrienKhai.F(y)));
            var ts = new JsonObject();
            foreach (var kv in thamSo) ts.Set(kv.Key, new JsonNumber(PhongTrienKhai.F(kv.Value)));
            tam.Set("tham_so", ts);
            foreach (string k in new[] { "be_x", "be_y", "doan_x", "doan_y" }) tam.Set(k, m.ViDu.Get(k) as List<object> ?? new List<object>());
            tam.Set("fname", new List<object> { "N=thu-" + m.Ten, "M=", "T=", "Q=" });
            var viec = new JsonObject();
            viec.Set("moi_truong", MoiTruongVe);
            viec.Set("thu_muc_ra", dir);
            viec.Set("thu_vien", new List<object> { ThuVienChung, tm == null ? "" : ThuVienThu(tm) });
            viec.Set("tam", new List<object> { tam });
            string tep = Path.Combine(dir, "viec.json");
            File.WriteAllText(tep, JsonLite.Serialize(viec), Utf8);
            File.Delete(Path.Combine(dir, ".ve_dxf.json")); // vẽ thử: luôn ghi đè, không giữ bản sửa tay
            var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, huy, Path.Combine(ThuMucVeDxf, "ve.py"), tep);
            JsonObject o = null;
            try { o = JsonLite.Parse(ra.Trim().Split('\n').Last()) as JsonObject; } catch (Exception) { }
            JsonObject x0 = (o?.Get("ket_qua") as List<object>)?.OfType<JsonObject>().FirstOrDefault();
            if (ma != 0 || x0 == null) throw new InvalidOperationException("ve.py lỗi: " + DongCuoi(ra));
            return new KetQuaVe
            {
                Bango = x0.GetString("bango"), Tep = x0.GetString("tep"), Anh = x0.GetString("anh"), Loi = x0.GetString("loi"),
                ThieuDim = (x0.Get("thieu_dim") as List<object> ?? new List<object>()).OfType<string>().ToList(),
                CanhBao = (x0.Get("canh_bao") as List<object> ?? new List<object>()).OfType<string>().ToList(),
            };
        }

        /// <summary>Kết quả vẽ một tấm (tep / anh null khi lỗi).</summary>
        public sealed class KetQuaVe
        {
            public string Bango, Tep, Anh, Loi;
            /// <summary>Dim khách chưa có trên bản vẽ mà không tự thêm được (Ø lỗ, góc…) — người vẽ bổ sung trên CAD.</summary>
            public List<string> ThieuDim = new List<string>();
            /// <summary>Khoét chạm / vượt viền, đè lỗ / khoét khác, mép mỏng hơn T (goc_khoet.py) — vẫn vẽ, người kiểm.</summary>
            public List<string> CanhBao = new List<string>();
        }

        /// <summary>
        /// Sơ đồ đỉnh (12/11): ảnh viền ngoài + khoét có ghi số đỉnh → ho_so\&lt;đơn&gt;-n-dinh.png cho mọi tấm đã có mẫu vẽ (tấm bẻ: trải phẳng
        /// tạm theo bảng TV). Người / AI chỉ đỉnh vát / bo theo số này. Máy chưa có Python / ezdxf / môi trường vẽ → trả câu báo, không lỗi.
        /// </summary>
        public static async Task<string> VeSoDo(ChiTiet ct, string tm, string title, CancellationToken huy)
        {
            TinhTrangPython tt = await KiemPython();
            if (!tt.SanSang) return "Chưa vẽ sơ đồ đỉnh: " + tt.Loi;
            if (!File.Exists(MoiTruongVe)) return "Chưa vẽ sơ đồ đỉnh: thiếu môi trường vẽ " + MoiTruongVe;
            var ds = TinhLai(ct, TaoTrienKhai(ct, tm), title, tm);
            if (ds.Count == 0) return null;
            string dir = ThuMucHoSo(tm);
            Directory.CreateDirectory(dir);
            string viec = Path.Combine(Path.GetTempPath(), "ohta_so_do_" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(viec, PhongTrienKhai.ViecVe(title, ds, MoiTruongVe, dir, new[] { ThuVienChung, ThuVienThu(tm) }), Utf8);
            try
            {
                var (ma, ra) = await ChayLenh(tt.Python, ThuMucVeDxf, huy, Path.Combine(ThuMucVeDxf, "ve.py"), "--so-do", viec);
                List<object> l = null;
                try { l = JsonLite.Parse(ra.Trim().Split('\n').Last()) as List<object>; } catch (Exception) { }
                if (ma != 0 || l == null) return "Sơ đồ đỉnh lỗi: " + DongCuoi(ra);
                foreach (JsonObject x in l.OfType<JsonObject>().Where(x => x.GetString("loi") != null)) // tấm chưa có mẫu / khoét sai: bỏ ảnh cũ
                    try { File.Delete(Path.Combine(dir, x.GetString("bango") + "-dinh.png")); } catch (IOException) { }
                return null;
            }
            finally { try { File.Delete(viec); } catch (IOException) { } }
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
                .Select(x => new KetQuaVe
                {
                    Bango = x.GetString("bango"), Tep = x.GetString("tep"), Anh = x.GetString("anh"), Loi = x.GetString("loi"),
                    ThieuDim = (x.Get("thieu_dim") as List<object> ?? new List<object>()).OfType<string>().ToList(),
                    CanhBao = (x.Get("canh_bao") as List<object> ?? new List<object>()).OfType<string>().ToList(),
                }).ToList();
        }

        /// <summary>
        /// AI viết mẫu biên dạng mới cho tấm chưa có mẫu (đọc lại bản vẽ khách + mô tả + ví dụ be.py), kiểm an toàn (KiemMau),
        /// lưu ai\ban_ve\mau_thu\&lt;ten&gt;.py và gán cho tấm. Trả mẫu (Loi khác null = không dùng được).
        /// </summary>
        public static async Task<PhongTrienKhai.MauMoi> DeXuatMau(ChiTiet ct, TamAi tam, TamTrienKhai tk, KetQuaTrienKhai kq, string tm, string title, Action<string> bao, CancellationToken huy)
        {
            string key = KeyPhong(4);
            string model = Model();
            var daCo = (await DanhSachMau(tm)).Select(m => m.Ten).ToList();
            string viDuF = Path.Combine(ThuVienChung, "dai_lom_loi.py"); // ví dụ có cung thật + dim tâm cung theo bản chuẩn
            string viDu = File.ReadAllText(File.Exists(viDuF) ? viDuF : Path.Combine(ThuVienChung, "be.py"), Encoding.UTF8);
            string f = Path.Combine(ThuMucTep(tm), tam.Tep), pdf = null;
            if (File.Exists(f) && f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && new FileInfo(f).Length <= PhongDocBanVe.PdfToiDa)
                pdf = Convert.ToBase64String(File.ReadAllBytes(f));
            bao("Phòng 3: AI viết mẫu vẽ mới cho " + tk.Ten + "…");
            string qcVe = Path.Combine(ThuMucVeDxf, "QUY_CHUAN_VE.md"); // quy ước vẽ theo bản chuẩn xưởng
            // Hồ sơ tấm đã xác nhận (thông số, mô tả, câu trả lời, trải phẳng) + ghi chú chung đơn
            string tenTam = PhongHoSo.DanhSo(ct, title).FirstOrDefault(x => ReferenceEquals(x.Tam, tam)).Ten ?? "";
            string hoSo = string.Join("\n\n", new[] { tenTam + ".md", PhongHoSo.TepDon }.Select(x => Path.Combine(ThuMucHoSo(tm), x)).Where(File.Exists).Select(x => File.ReadAllText(x, Encoding.UTF8)));
            string dauVao = PhongTrienKhai.DauVaoMauMoi(tam, tk, daCo, viDu) + (hoSo.Length > 0 ? "\n\nHỒ SƠ TẤM ĐÃ XÁC NHẬN (dữ liệu đúng — vẽ theo đây):\n" + hoSo : "");
            string body = PhongTrienKhai.BodyMauMoi(model, File.Exists(qcVe) ? File.ReadAllText(qcVe, Encoding.UTF8) : null, pdf, dauVao);
            PhongTrienKhai.MauMoi m = PhongTrienKhai.DocMauMoi(await GuiAi(key, PhongDocBanVe.CoFallback(model), body, huy), daCo);
            kq.TokenVao += m.Vao + m.GhiCache + m.DocCache;
            kq.TokenRa += m.Ra;
            kq.Tien += PhongDocBanVe.Tien(model, m.Vao, m.GhiCache, m.DocCache, m.Ra);
            if (m.Loi == null)
            {
                Directory.CreateDirectory(ThuVienThu(tm));
                File.WriteAllText(Path.Combine(ThuVienThu(tm), m.Ten + ".py"), m.Ma, Utf8);
                string loiAnh = await TaoAnhHuongDan(ThuVienThu(tm), huy); // ảnh hướng dẫn để kiểm ở 📚 Thư viện mẫu
                if (loiAnh.Length > 0) bao("Mẫu thử đã lưu, chưa vẽ được ảnh hướng dẫn: " + loiAnh);
                tk.BienDang = tam.BienDang = m.Ten; // biên dạng nằm trong bảng tấm — trien_khai.json dựng lại từ đó
                Ghi(tm, ct);
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
            string anh = Path.ChangeExtension(nguon, ".png"); // ảnh hướng dẫn đi kèm mẫu
            if (File.Exists(anh) && !File.Exists(Path.ChangeExtension(dich, ".png"))) File.Copy(anh, Path.ChangeExtension(dich, ".png"));
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
            psi.EnvironmentVariables["OPENBLAS_NUM_THREADS"] = "1"; // numpy (matplotlib) nhiều luồng có lúc hết bộ nhớ khi vẽ ảnh xem trước
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

        /// <summary>
        /// Chỉ cho đơn đọc bằng bản cũ (bảng tấm chưa có tiết diện): AI đọc bù tiết diện các tấm đó theo tệp bản vẽ.
        /// Kết quả vào trien_khai.json; TaoTrienKhai chép sang bảng tấm. Đơn đọc bằng bản mới không cần.
        /// </summary>
        public static async Task<KetQuaTrienKhai> TrienKhaiDon(ChiTiet ct, KiemTra kt, string tm, Action<string> bao, CancellationToken huy)
        {
            NapTraV(lai: true);
            KetQuaTrienKhai cu = DocTrienKhai(tm);
            string key = KeyPhong(4);
            string model = Model(), quyChuan = QuyChuan(4);
            var kq = new KetQuaTrienKhai { TuXacNhan = ct.XacNhan, TokenVao = cu?.TokenVao ?? 0, TokenRa = cu?.TokenRa ?? 0, Tien = cu?.Tien ?? 0 };
            var loi = new List<string>();
            var theoTep = ct.Tam.Where(t => t.Moi && !PhongTrienKhai.CoTietDien(t)).GroupBy(t => t.Tep).ToList();
            // mẫu biên dạng để AI chọn (máy chưa có Python / ezdxf → rỗng: AI để trống, vẽ thì tự suy)
            var thuVien = (await DanhSachMau(tm)).Select(m => (m.Ten, m.MoTa)).ToList();
            int i = 0;
            foreach (var nhom in theoTep)
            {
                huy.ThrowIfCancellationRequested();
                bao("Phòng 3: AI đọc bù tiết diện (đơn cũ) " + (++i) + "/" + theoTep.Count + " — " + nhom.Key + " (mỗi tệp khoảng 1 phút)");
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
                string body = PhongTrienKhai.Body(model, quyChuan, pdf, PhongTrienKhai.DauVao(nhom.Key, nhom, kt?.VanDe, thuVien), dxf);
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
            get { string v = CaiDatTeams.Doc("ai_nguon", AiCli.Api); return v == AiCli.ClaudeCode || v == AiCli.Antigravity || v == AiCli.Ling ? v : AiCli.Api; }
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
            NguonAi == AiCli.Ling ? DocKey(phong) ?? "" // Ling: key Claude chỉ dùng cho việc có PDF / ảnh (có thể không có)
            : NguonAi != AiCli.Api ? "" : DocKey(phong) ?? throw new InvalidOperationException("Chưa có key Claude: ⚙ → tab 🤖 AI → dán key (hoặc chọn Nguồn AI = Claude Code / Antigravity / Ling)");

        // ---------- Ling (dùng chung key + model với tab AI trên CAD) ----------

        public static string FileKeyLing => Path.Combine(Goc, "05_AI", "data", "ling.key");

        public static string KeyLing()
        {
            if (!File.Exists(FileKeyLing)) return null;
            return File.ReadAllLines(FileKeyLing).Select(x => x.Trim()).FirstOrDefault(x => x.Length > 0 && !x.StartsWith("#", StringComparison.Ordinal));
        }

        /// <summary>Model Ling như tab AI trên CAD (registry ling_model), không có → Ling-3.2-flash.</summary>
        public static string ModelLing()
        {
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\OFT_Lisp\OhtaTool\AI")) if (k?.GetValue("ling_model") is string m && m.Trim().Length > 0) return m.Trim(); }
            catch (Exception) { }
            return AiCli.ModelLingMacDinh;
        }

        // tbox trả 429 khi gọi dồn → Ling gửi từng lượt một
        private static readonly SemaphoreSlim MotLuotLing = new SemaphoreSlim(1, 1);

        /// <summary>POST chat/completions Ling (Bearer ling.key); 429 / 5xx thử lại 3 lần (5, 10, 20 s); trả phản hồi dạng Claude.</summary>
        private static async Task<string> GuiLing(string bodyClaude, CancellationToken ct)
        {
            string key = KeyLing() ?? throw new InvalidOperationException("Chưa có key Ling: " + FileKeyLing + " (nhập ở ⚙ của tab AI trên CAD) — hoặc đổi Nguồn AI ở ⚙ → 🤖 AI.");
            string body = AiCli.BodyLing(bodyClaude, ModelLing());
            await MotLuotLing.WaitAsync(ct);
            try
            {
                for (int lan = 0; ; lan++)
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Post, AiCli.UrlLing))
                    {
                        if (!req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + key)) throw new InvalidOperationException("Key Ling có ký tự không hợp lệ");
                        req.Content = new StringContent(body, Utf8, "application/json");
                        using (HttpResponseMessage res = await Http.SendAsync(req, ct))
                        {
                            string text = await res.Content.ReadAsStringAsync();
                            int ma = (int)res.StatusCode;
                            if (res.IsSuccessStatusCode) return AiCli.DoiPhanHoiLing(text);
                            if ((ma != 429 && ma < 500) || lan >= 3)
                                return "{\"type\":\"error\",\"error\":{\"type\":\"ling_error\",\"message\":" + JsonLite.Serialize("Ling trả lỗi HTTP " + ma + (ma == 429 ? " (quá giới hạn tần suất — đợi rồi thử lại)" : "")) + "}}";
                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5 << lan), ct);
                }
            }
            finally { MotLuotLing.Release(); }
        }

        /// <summary>Gửi body /v1/messages theo nguồn đang chọn; trả phản hồi dạng API (CLI được đổi về cùng dạng).</summary>
        private static Task<string> GuiAi(string key, bool fallback, string body, CancellationToken ct)
        {
            if (NguonAi == AiCli.Ling)
            {
                if (!AiCli.CoTep(body)) return GuiLing(body, ct);
                // Ling chỉ đọc chữ: việc có bản vẽ PDF / ảnh dùng key Claude của máy
                if (string.IsNullOrEmpty(key))
                    throw new InvalidOperationException("Ling không đọc được bản vẽ PDF / ảnh — cần key Claude cho lượt đọc bản vẽ (⚙ → 🤖 AI → dán key), hoặc đổi Nguồn AI sang Claude Code / Antigravity.");
                return GuiClaude(key, fallback, body, ct);
            }
            return NguonAi == AiCli.Api ? GuiClaude(key, fallback, body, ct) : GuiCli(NguonAi, body, ct);
        }

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
                if (yc.AnhBase64 != null) File.WriteAllBytes(Path.Combine(dir, AiCli.TepAnh), Convert.FromBase64String(yc.AnhBase64));
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
