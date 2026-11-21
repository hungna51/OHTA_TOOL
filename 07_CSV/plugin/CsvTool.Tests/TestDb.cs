using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CsvTool.Core;

namespace CsvTool.Tests
{
    /// <summary>Client Postgres: véc-tơ chuẩn RFC 7677 + máy chủ giả nói đúng giao thức v3 trên loopback (không cần database thật).</summary>
    internal static partial class Program
    {
        private enum KieuGia { DungChuan, SaiChuKy, KhongHoiMatKhau, ChiMd5, SaiMatKhau }

        private static void TestDb()
        {
            Console.WriteLine("[Db - client Postgres]");

            const string dau = "n=user,r=rOprNGfwEbeRWgbNEkqO";
            const string may = "r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096";
            const string cuoi = "c=biws,r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0";
            var v = PostgresDoc.TinhScram("pencil", Convert.FromBase64String("W22ZaJ0SNY7soEsUEjb6gQ=="), 4096, dau + "," + may + "," + cuoi);
            Kiem("SCRAM RFC 7677: proof", Convert.ToBase64String(v.Proof), "dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=");
            Kiem("SCRAM RFC 7677: chu ky may chu", Convert.ToBase64String(v.ServerSignature), "6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4=");

            var k = KetNoiDb.Phan("postgresql://ohta_doc:p%40ss%3Aw@db.vi-du.test:6543/gia?schema=public&sslmode=require");
            Kiem("KetNoi: tach + giai ma", k.User + "|" + k.Password + "|" + k.Host + "|" + k.Port + "|" + k.Database + "|" + k.KiemChungChi, "ohta_doc|p@ss:w|db.vi-du.test|6543|gia|False");
            Kiem("KetNoi: mac dinh xac minh chung chi, cong 5432", KetNoiDb.Phan("postgres://a:b@h/d").KiemChungChi + "|" + KetNoiDb.Phan("postgres://a:b@h/d").Port, "True|5432");
            KiemNem<ArgumentException>("KetNoi: cam sslmode=disable", () => KetNoiDb.Phan("postgres://a:b@h/d?sslmode=disable"));
            string loiSai = "";
            try { KetNoiDb.Phan("mysql://nguoi:matkhaubimat@h/d"); } catch (ArgumentException ex) { loiSai = ex.Message; }
            Kiem("KetNoi: thong bao loi khong lo mat khau", loiSai != "" && !loiSai.Contains("matkhaubimat"), true);

            string ketQua = VoiMayGia(KieuGia.DungChuan, "dung-mk", db =>
            {
                var r = db.Hoi("SELECT 1");
                return r.Count + "|" + r[0][0] + "|" + (r[0][1] == null) + "|" + r[1][0];
            });
            Kiem("Dang nhap SCRAM + doc dong, NULL, UTF-8", ketQua, "2|SS400|True|株式会社");
            Kiem("Tu choi chu ky may chu sai", VoiMayGia(KieuGia.SaiChuKy, "dung-mk", db => "vao").Contains("Chữ ký"), true);
            Kiem("Tu choi may chu khong hoi mat khau", VoiMayGia(KieuGia.KhongHoiMatKhau, "dung-mk", db => "vao").Contains("không kiểm tra mật khẩu"), true);
            Kiem("Tu choi kieu dang nhap khac SCRAM", VoiMayGia(KieuGia.ChiMd5, "dung-mk", db => "vao").Contains("chưa hỗ trợ"), true);
            Kiem("Bao loi sai mat khau", VoiMayGia(KieuGia.SaiMatKhau, "dung-mk", db => "vao"), "Database báo lỗi: password authentication failed");

            var bang = NguonPostgres.DocBang(sql =>
                sql.Contains("\"BangGiaVL\"") ? new List<string[]> { new[] { "SS400", "6", "140", "900", "7", "45", "150", "1700", "7.85" } } :
                sql.Contains("\"CongTyDacBiet\"") ? new List<string[]> { new[] { "DB", "260", "330", "t" } } :
                sql.Contains("\"CongTy\"") ? new List<string[]> { new[] { "UD", "t" }, new[] { "UD", "f" }, new[] { "TH", "f" } } :
                sql.Contains("\"HeSoGiaVL\"") ? new List<string[]> { new[] { "Ưu đãi SS", "1.15" } } :
                sql.Contains("\"HeSoPitchiKakumaru\"") ? new List<string[]> { new[] { "trong_luong", "0.8" }, new[] { "chieu_dai", "0.75" } } :
                sql.Contains("\"HeSoCuon\"") ? new List<string[]> { new[] { "0.4" } } : new List<string[]>());
            Kiem("DocBang: map cot, ten trung lay dong dau", bang.Dong[0].GiaTappu + "|" + bang.CongTyUuDai["UD"] + "|" + bang.DacBiet["DB"].SoSanhGiaThuong + "|" + bang.PitchiTrongLuong[0] + "|" + bang.PitchiChieuDai[0],
                 "150|True|True|0.8|0.75");
            var khoiPhuc = BangGia.TuByte(bang.ThanhByte());
            Kiem("BangGia: khu hoi ban sao", khoiPhuc.Dong[0].TyTrong + "|" + khoiPhuc.HeSoVL["Ưu đãi SS"] + "|" + khoiPhuc.DacBiet["DB"].DonGiaTamGiaCong, "7.85|1.15|330");
            KiemNemBatKy("DocBang: bang gia trong -> bao loi", () => NguonPostgres.DocBang(sql => new List<string[]>()));
        }

        private static string VoiMayGia(KieuGia kieu, string matKhau, Func<PostgresDoc, string> lam)
        {
            var nghe = new TcpListener(IPAddress.Loopback, 0);
            nghe.Start();
            int cong = ((IPEndPoint)nghe.LocalEndpoint).Port;
            var luong = new Thread(() => { try { MayGia(nghe, kieu, matKhau); } catch (Exception) { } }) { IsBackground = true };
            luong.Start();
            try
            {
                using (var tcp = new TcpClient())
                {
                    tcp.Connect(IPAddress.Loopback, cong);
                    tcp.ReceiveTimeout = 5000;
                    using (var db = new PostgresDoc(tcp.GetStream(), "u", "d", matKhau)) return lam(db);
                }
            }
            catch (IOException ex) { return ex.Message; }
            finally { nghe.Stop(); }
        }

        private static void MayGia(TcpListener nghe, KieuGia kieu, string matKhau)
        {
            using (TcpClient c = nghe.AcceptTcpClient())
            {
                NetworkStream s = c.GetStream();
                byte[] Doc(int n) { var b = new byte[n]; for (int d = 0; d < n;) { int r = s.Read(b, d, n - d); if (r <= 0) throw new IOException(); d += r; } return b; }
                int Int(byte[] b, int i) => (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];
                byte[] BE(int v) => new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
                void Gui(char t, params byte[][] phan)
                {
                    byte[] than = phan.SelectMany(x => x).ToArray();
                    s.Write(new[] { (byte)t }.Concat(BE(than.Length + 4)).Concat(than).ToArray());
                }
                byte[] U(string x) => Encoding.UTF8.GetBytes(x);
                byte[] DocGoi() { Doc(1); return Doc(Int(Doc(4), 0) - 4); }

                Doc(Int(Doc(4), 0) - 4);   // startup
                if (kieu == KieuGia.KhongHoiMatKhau) { Gui('R', BE(0)); Gui('Z', U("I")); DocGoi(); return; }
                if (kieu == KieuGia.ChiMd5) { Gui('R', BE(5), new byte[] { 1, 2, 3, 4 }); DocGoi(); return; }
                Gui('R', BE(10), U("SCRAM-SHA-256\0\0"));
                byte[] p = DocGoi();
                int vt = Array.IndexOf(p, (byte)0) + 5;
                string dauBare = Encoding.ASCII.GetString(p, vt, p.Length - vt).Substring(3);
                string nonce = dauBare.Substring(dauBare.IndexOf("r=", StringComparison.Ordinal) + 2);
                string may = "r=" + nonce + "abcXYZ,s=" + Convert.ToBase64String(U("muoi-may-gia-16b")) + ",i=4096";
                Gui('R', BE(11), U(may));
                string cuoi = Encoding.ASCII.GetString(DocGoi());
                string khongProof = cuoi.Substring(0, cuoi.IndexOf(",p=", StringComparison.Ordinal));
                var mong = PostgresDoc.TinhScram(matKhau, U("muoi-may-gia-16b"), 4096, dauBare + "," + may + "," + khongProof);
                if (kieu == KieuGia.SaiMatKhau || cuoi != khongProof + ",p=" + Convert.ToBase64String(mong.Proof))
                {
                    Gui('E', U("SFATAL\0Mpassword authentication failed\0\0"));
                    return;
                }
                string ky = Convert.ToBase64String(kieu == KieuGia.SaiChuKy ? new byte[32] : mong.ServerSignature);
                Gui('R', BE(12), U("v=" + ky));
                Gui('R', BE(0));
                Gui('S', U("server_version\0" + "16\0"));
                Gui('Z', U("I"));
                DocGoi();   // truy vấn
                byte[] O(string x) => x == null ? BE(-1) : BE(U(x).Length).Concat(U(x)).ToArray();
                Gui('D', new byte[] { 0, 2 }, O("SS400"), O(null));
                Gui('D', new byte[] { 0, 2 }, O("株式会社"), O("x"));
                Gui('C', U("SELECT 2\0"));
                Gui('Z', U("I"));
                try { DocGoi(); } catch (IOException) { }
            }
        }
    }
}
