using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace CsvTool.Core
{
    /// <summary>Thông tin kết nối tách từ chuỗi postgresql://user:pass@host:port/db?sslmode=...</summary>
    public sealed class KetNoiDb
    {
        public string Host, Database, User, Password;
        public int Port = 5432;
        public bool KiemChungChi = true;   // sslmode=require của libpq: chỉ mã hoá, không xác minh chứng chỉ

        /// <summary>Lỗi không được chứa chuỗi kết nối vì có mật khẩu.</summary>
        public static KetNoiDb Phan(string chuoi)
        {
            const string sai = "Chuỗi kết nối không đúng dạng postgresql://tài-khoản:mật-khẩu@máy-chủ:cổng/tên-database";
            chuoi = (chuoi ?? "").Trim();
            if (!chuoi.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
                !chuoi.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(sai);
            Uri u;
            try { u = new Uri(chuoi); } catch (UriFormatException) { throw new ArgumentException(sai); }
            int c = u.UserInfo.IndexOf(':');
            if (c <= 0) throw new ArgumentException(sai);
            var k = new KetNoiDb
            {
                Host = u.Host, Port = u.Port > 0 ? u.Port : 5432, Database = Uri.UnescapeDataString(u.AbsolutePath.Trim('/')),
                User = Uri.UnescapeDataString(u.UserInfo.Substring(0, c)), Password = Uri.UnescapeDataString(u.UserInfo.Substring(c + 1)),
            };
            if (k.Host == "" || k.Database == "" || k.Password == "") throw new ArgumentException(sai);
            foreach (string q in u.Query.TrimStart('?').Split('&'))
            {
                string v = q.ToLowerInvariant();
                if (v == "sslmode=disable") throw new ArgumentException("Kết nối phải bật mã hoá SSL, không dùng sslmode=disable.");
                if (v == "sslmode=require" || v == "sslaccept=accept_invalid_certs") k.KiemChungChi = false;
            }
            return k;
        }
    }

    /// <summary>
    /// Client Postgres tối thiểu, chỉ để đọc: giao thức v3, truy vấn đơn, kết quả dạng chữ. Không dùng Npgsql vì kéo
    /// nhiều DLL dễ lệch phiên bản trong ZWCAD (không có binding redirect). Bắt buộc TLS và SCRAM-SHA-256,
    /// từ chối mọi kiểu đăng nhập khác và xác minh chữ ký của máy chủ.
    /// shortcut: mật khẩu không qua SASLprep, mật khẩu có ký tự Unicode lạ cần đổi sang ASCII nếu đăng nhập hỏng.
    /// </summary>
    public sealed class PostgresDoc : IDisposable
    {
        private const int HanKetNoiMs = 8000, HanDocMs = 15000, GoiToiDa = 16 * 1024 * 1024;
        private readonly Stream _s;
        private readonly IDisposable _tcp;

        public static PostgresDoc Mo(KetNoiDb k)
        {
            var tcp = new TcpClient();
            try
            {
                try { if (!tcp.ConnectAsync(k.Host, k.Port).Wait(HanKetNoiMs)) throw new IOException("Hết thời gian chờ kết nối tới " + k.Host + "."); }
                catch (AggregateException ex) { throw new IOException("Không kết nối được tới " + k.Host + ": " + ex.InnerException?.Message); }
                tcp.ReceiveTimeout = HanDocMs;
                tcp.SendTimeout = HanDocMs;
                NetworkStream ns = tcp.GetStream();
                ns.Write(new byte[] { 0, 0, 0, 8, 4, 210, 22, 47 }, 0, 8);   // SSLRequest
                if (ns.ReadByte() != 'S') throw new IOException("Máy chủ không hỗ trợ mã hoá SSL nên tool không gửi đăng nhập.");
                var ssl = new SslStream(ns, false, (s, cert, chain, loi) => !k.KiemChungChi || loi == SslPolicyErrors.None);
                // .NET 4.8 (ZWCAD) không nhận SslProtocols.None ở đây
                try { ssl.AuthenticateAsClient(k.Host, null, SslProtocols.Tls12, false); }
                catch (AuthenticationException) { throw new IOException("Không xác minh được chứng chỉ máy chủ. Nếu nhà cung cấp dùng chứng chỉ riêng, thêm ?sslmode=require vào chuỗi kết nối."); }
                return new PostgresDoc(ssl, k.User, k.Database, k.Password, tcp);
            }
            catch { tcp.Dispose(); throw; }
        }

        /// <summary>Stream đã sẵn sàng (TLS do Mo lo); test truyền thẳng stream vào máy chủ giả.</summary>
        public PostgresDoc(Stream s, string user, string database, string password, IDisposable tcp = null)
        {
            _s = s; _tcp = tcp;
            var m = new MemoryStream();
            GhiInt(m, 196608);
            foreach (string x in new[] { "user", user, "database", database, "application_name", "ohta-csv" }) GhiChuoi(m, x);
            m.WriteByte(0);
            GuiGoi(null, m.ToArray());
            DangNhap(password);
        }

        private void DangNhap(string password)
        {
            string nonce = Convert.ToBase64String(RandomBytes(18)), dauBare = "n=,r=" + nonce, cuoiKhongProof = null;
            byte[] chuKyMayChu = null;
            bool daXacMinh = false;
            while (true)
            {
                var (loai, p) = DocGoi();
                if (loai == 'E') throw new IOException(LoiTu(p));
                if (loai == 'Z') { if (!daXacMinh) throw new IOException("Máy chủ không xác thực đăng nhập."); return; }
                if (loai != 'R') continue;
                int ma = DocInt(p, 0);
                if (ma == 0) { if (!daXacMinh) throw new IOException("Máy chủ cho vào mà không kiểm tra mật khẩu, tool từ chối kết nối."); }
                else if (ma == 10)
                {
                    if (!TachChuoi(p, 4).Contains("SCRAM-SHA-256")) throw new IOException("Máy chủ không hỗ trợ đăng nhập SCRAM-SHA-256.");
                    byte[] dau = Encoding.ASCII.GetBytes("n,," + dauBare);
                    var m = new MemoryStream();
                    GhiChuoi(m, "SCRAM-SHA-256");
                    GhiInt(m, dau.Length);
                    m.Write(dau, 0, dau.Length);
                    GuiGoi('p', m.ToArray());
                }
                else if (ma == 11)
                {
                    string may = Encoding.UTF8.GetString(p, 4, p.Length - 4);
                    var f = may.Split(',').Where(x => x.Length > 2).ToDictionary(x => x.Substring(0, 1), x => x.Substring(2));
                    if (!f.TryGetValue("r", out string r) || !r.StartsWith(nonce, StringComparison.Ordinal) || r.Length <= nonce.Length ||
                        !f.TryGetValue("s", out string muoi) || !f.TryGetValue("i", out string lap) ||
                        !int.TryParse(lap, NumberStyles.None, CultureInfo.InvariantCulture, out int soLan) || soLan < 4096)
                        throw new IOException("Phản hồi đăng nhập của máy chủ không hợp lệ.");
                    cuoiKhongProof = "c=biws,r=" + r;
                    var (proof, ky) = TinhScram(password, Convert.FromBase64String(muoi), soLan, dauBare + "," + may + "," + cuoiKhongProof);
                    chuKyMayChu = ky;
                    GuiGoi('p', Encoding.ASCII.GetBytes(cuoiKhongProof + ",p=" + Convert.ToBase64String(proof)));
                }
                else if (ma == 12)
                {
                    string cuoi = Encoding.UTF8.GetString(p, 4, p.Length - 4);
                    if (chuKyMayChu == null || cuoi != "v=" + Convert.ToBase64String(chuKyMayChu))
                        throw new IOException("Chữ ký máy chủ sai, có thể không phải database thật. Đã dừng.");
                    daXacMinh = true;
                }
                else throw new IOException("Database yêu cầu kiểu đăng nhập chưa hỗ trợ (cần SCRAM-SHA-256).");
            }
        }

        /// <summary>SCRAM-SHA-256 (RFC 5802 / 7677): trả ClientProof và ServerSignature.</summary>
        public static (byte[] Proof, byte[] ServerSignature) TinhScram(string password, byte[] muoi, int soLan, string authMessage)
        {
#pragma warning disable SYSLIB0060
            byte[] salted;
            using (var k = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), muoi, soLan, HashAlgorithmName.SHA256)) salted = k.GetBytes(32);
#pragma warning restore SYSLIB0060
            byte[] clientKey = Hmac(salted, "Client Key"), serverKey = Hmac(salted, "Server Key"), stored;
            using (var h = SHA256.Create()) stored = h.ComputeHash(clientKey);
            byte[] ky = Hmac(stored, authMessage), proof = new byte[clientKey.Length];
            for (int i = 0; i < proof.Length; i++) proof[i] = (byte)(clientKey[i] ^ ky[i]);
            return (proof, Hmac(serverKey, authMessage));
        }

        private static byte[] Hmac(byte[] khoa, string s)
        {
            using (var h = new HMACSHA256(khoa)) return h.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        private static byte[] RandomBytes(int n)
        {
            var b = new byte[n];
            using (var r = RandomNumberGenerator.Create()) r.GetBytes(b);
            return b;
        }

        /// <summary>Truy vấn đơn, mỗi dòng là mảng chữ theo thứ tự cột (NULL = null).</summary>
        public List<string[]> Hoi(string sql)
        {
            var m = new MemoryStream();
            GhiChuoi(m, sql);
            GuiGoi('Q', m.ToArray());
            var kq = new List<string[]>();
            string loi = null;
            while (true)
            {
                var (loai, p) = DocGoi();
                if (loai == 'E') loi = LoiTu(p);
                else if (loai == 'Z') { if (loi != null) throw new IOException(loi); return kq; }
                else if (loai == 'D')
                {
                    int n = (p[0] << 8) | p[1], vt = 2;
                    var dong = new string[n];
                    for (int i = 0; i < n; i++)
                    {
                        int dai = DocInt(p, vt);
                        vt += 4;
                        if (dai < 0) continue;
                        dong[i] = Encoding.UTF8.GetString(p, vt, dai);
                        vt += dai;
                    }
                    kq.Add(dong);
                }
            }
        }

        private static string LoiTu(byte[] p)
        {
            for (int i = 0; i < p.Length && p[i] != 0;)
            {
                byte ma = p[i++];
                int j = Array.IndexOf(p, (byte)0, i);
                if (j < 0) break;
                if (ma == 'M') return "Database báo lỗi: " + Encoding.UTF8.GetString(p, i, j - i);
                i = j + 1;
            }
            return "Database báo lỗi.";
        }

        private static List<string> TachChuoi(byte[] p, int tu) =>
            Encoding.UTF8.GetString(p, tu, p.Length - tu).Split('\0').Where(x => x != "").ToList();

        private static int DocInt(byte[] b, int i) => (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];

        private static void GhiInt(Stream s, int v)
        {
            s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v);
        }

        private static void GhiChuoi(Stream s, string v)
        {
            byte[] b = Encoding.UTF8.GetBytes(v);
            s.Write(b, 0, b.Length);
            s.WriteByte(0);
        }

        private void GuiGoi(char? loai, byte[] than)
        {
            var m = new MemoryStream();
            if (loai != null) m.WriteByte((byte)loai.Value);
            GhiInt(m, than.Length + 4);
            m.Write(than, 0, than.Length);
            _s.Write(m.ToArray(), 0, (int)m.Length);
            _s.Flush();
        }

        private (char Loai, byte[] Than) DocGoi()
        {
            byte[] dau = DocDu(5);
            int dai = DocInt(dau, 1) - 4;
            if (dai < 0 || dai > GoiToiDa) throw new IOException("Dữ liệu từ database không hợp lệ.");
            return ((char)dau[0], DocDu(dai));
        }

        private byte[] DocDu(int n)
        {
            var b = new byte[n];
            for (int d = 0; d < n;)
            {
                int r = _s.Read(b, d, n - d);
                if (r <= 0) throw new IOException("Database đóng kết nối giữa chừng.");
                d += r;
            }
            return b;
        }

        public void Dispose()
        {
            try { GuiGoi('X', new byte[0]); } catch (Exception) { }
            _s.Dispose();
            _tcp?.Dispose();
        }
    }

    /// <summary>Đọc 7 bảng giá của price-calc trong 1 giao dịch chỉ đọc, các bảng nhất quán với nhau.</summary>
    public sealed class NguonPostgres : INguonBangGia
    {
        private readonly string _chuoi;
        public NguonPostgres(string chuoi) { _chuoi = chuoi; }

        public BangGia Doc()
        {
            using (var db = PostgresDoc.Mo(KetNoiDb.Phan(_chuoi)))
            {
                db.Hoi("BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ READ ONLY");
                var b = DocBang(db.Hoi);
                db.Hoi("COMMIT");
                return b;
            }
        }

        /// <summary>Tách khỏi kết nối để test được với dữ liệu giả.</summary>
        public static BangGia DocBang(Func<string, List<string[]>> hoi)
        {
            double S(string s) => s == null ? 0 : double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
            var b = new BangGia { CapNhat = DateTime.Now };
            foreach (var r in hoi("SELECT \"vatLieu\",\"doDay\",\"donGia\",\"giaUon\",\"giaCat\",\"giaMoLo\",\"giaTappu\",\"giaVat\",\"tyTrong\" FROM \"BangGiaVL\" ORDER BY id"))
                b.Dong.Add(new DongBangGia { VatLieu = r[0], DoDay = S(r[1]), DonGia = S(r[2]), GiaUon = S(r[3]), GiaCat = S(r[4]), GiaMoLo = S(r[5]), GiaTappu = S(r[6]), GiaVat = S(r[7]), TyTrong = S(r[8]) });
            // web dùng find(): tên trùng thì lấy dòng đầu
            foreach (var r in hoi("SELECT \"tenCongTy\",\"isUuDai\",\"tiengNhat\" FROM \"CongTy\" ORDER BY id"))
                if (!b.CongTyUuDai.ContainsKey(r[0])) { b.CongTyUuDai[r[0]] = r[1] == "t"; b.TenNhat[r[0]] = r[2] ?? ""; }
            foreach (var r in hoi("SELECT \"tenCongTy\",\"donGiaTamDon\",\"donGiaTamGiaCong\",\"soSanhGiaThuong\" FROM \"CongTyDacBiet\" ORDER BY id"))
                if (!b.DacBiet.ContainsKey(r[0])) b.DacBiet[r[0]] = new CongTyDacBiet { DonGiaTamDon = S(r[1]), DonGiaTamGiaCong = S(r[2]), SoSanhGiaThuong = r[3] == "t" };
            foreach (var r in hoi("SELECT \"tenLoai\",\"heSo\" FROM \"HeSoGiaVL\" ORDER BY id"))
                if (!b.HeSoVL.ContainsKey(r[0])) b.HeSoVL[r[0]] = S(r[1]);
            foreach (var r in hoi("SELECT \"tenLoai\",\"heSo\" FROM \"HeSoGiaCong\" ORDER BY id"))
            {
                string ten = (r[0] ?? "").Trim().Normalize();   // "Nón lệch" gõ trên web có thể ở dạng dấu tổ hợp
                if (!b.HeSoGiaCong.ContainsKey(ten)) b.HeSoGiaCong[ten] = S(r[1]);
            }
            foreach (var r in hoi("SELECT \"loai\",\"heSo\" FROM \"HeSoPitchiKakumaru\" ORDER BY \"thuTu\",id"))
                (r[0] == "trong_luong" ? b.PitchiTrongLuong : r[0] == "chieu_dai" ? b.PitchiChieuDai : new List<double>()).Add(S(r[1]));
            foreach (var r in hoi("SELECT \"heSo\" FROM \"HeSoCuon\" ORDER BY id")) b.HeSoCuon.Add(S(r[0]));
            if (b.Dong.Count == 0) throw new InvalidOperationException("Bảng giá trên database đang trống.");
            // không có số mặc định: thiếu bảng / thiếu số thì báo, không tính giá
            List<string[]> cd;
            try { cd = hoi("SELECT \"giaToiThieu\",\"buToiDa\" FROM \"CaiDatGia\" ORDER BY id LIMIT 1"); }
            // máy chủ trả lỗi SQL (thiếu bảng / cột) ≠ mất mạng: không để NguonGiaCoDem quay sang bản sao
            catch (IOException ex) when (ex.Message.StartsWith("Database báo lỗi")) { throw new InvalidOperationException(BangGia.LoiThieuCaiDat); }
            if (cd.Count == 0 || cd[0][0] == null || cd[0][1] == null || (b.GiaToiThieu = S(cd[0][0])) <= 0 || (b.BuToiDa = S(cd[0][1])) <= 0)
                throw new InvalidOperationException(BangGia.LoiThieuCaiDat);
            return b;
        }
    }
}
