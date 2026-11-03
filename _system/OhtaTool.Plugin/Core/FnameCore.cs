using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace OhtaTool.Plugin.Core
{
    [DataContract]
    public sealed class FnameCatalogData
    {
        [DataMember(Name = "materials", IsRequired = false)]
        public List<FnameMaterialSpec> Materials { get; set; } = new List<FnameMaterialSpec>();

        public FnameMaterialSpec FindMaterial(string mValue)
        {
            if (string.IsNullOrWhiteSpace(mValue) || Materials == null)
                return null;

            string target = mValue.Trim();
            return Materials.FirstOrDefault(x =>
                x != null && string.Equals(x.M?.Trim(), target, StringComparison.OrdinalIgnoreCase));
        }

        public List<string> GetDistinctZList()
        {
            var result = new List<string>();
            if (Materials == null)
                return result;

            foreach (FnameMaterialSpec spec in Materials)
            {
                string z = spec?.Z?.Trim();
                if (!string.IsNullOrEmpty(z) && !result.Contains(z, StringComparer.OrdinalIgnoreCase))
                    result.Add(z);
            }
            return result;
        }

        public static FnameCatalogData LoadFromModuleDirectory(string moduleDirectory, string cacheDirectory = null)
        {
            // Cộng thêm vật liệu máy này đã gửi bổ sung nhưng admin chưa duyệt (catalog_local.csv)
            string[] lines = FnameDataStore.ReadLines(moduleDirectory, "catalog", cacheDirectory);
            return Parse(FnameLocalAdditions.ApplyTo(lines, FnameLocalAdditions.Load(cacheDirectory)));
        }

        /// <summary>
        /// CSV cột M,Loai,Z,T_list (T_list cách nhau bằng dấu cách). Cột được tìm theo tên tiêu đề nên thứ tự cột
        /// trên Google Sheet tùy ý; không có dòng tiêu đề thì hiểu theo bố cục cũ M,Z,T_list (không có Loai).
        /// Dòng thiếu M bị bỏ qua.
        /// </summary>
        public static FnameCatalogData Parse(string[] lines)
        {
            var data = new FnameCatalogData();
            int iM = 0, iZ = 1, iT = 2, iLoai = -1;
            foreach (string line in lines)
            {
                string[] p = line.Split(',');
                if (IsHeaderRow(p))
                {
                    iM = iZ = iT = iLoai = -1;
                    for (int i = 0; i < p.Length; i++)
                    {
                        string h = p[i].Trim().ToUpperInvariant();
                        if (h == "M") iM = i;
                        else if (h == "Z") iZ = i;
                        else if (h == "T_LIST" || h == "T") iT = i;
                        else if (h == "LOAI" || h == "LOẠI" || h == "TYPE") iLoai = i;
                    }
                    continue;
                }

                string m = Cell(p, iM);
                if (m.Length == 0)
                    continue;
                string[] ts = Cell(p, iT).Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                var spec = new FnameMaterialSpec(m, Cell(p, iZ), ts) { Loai = Cell(p, iLoai) };
                data.Materials.Add(spec);
            }
            return data;
        }

        /// <summary>
        /// Tách chuỗi độ dày người dùng nhập ("2.3 3.2; 6") thành danh sách chuẩn ("2.3", "3.2", "6.0").
        /// Dấu phẩy, chấm phẩy, khoảng trắng đều là dấu ngăn cách; mỗi giá trị phải là số dương &lt;= 200.
        /// Số nguyên được viết kèm ".0" giống catalog ("6" -> "6.0").
        /// </summary>
        public static bool TryParseThicknessInput(string input, out List<string> thicknesses, out string error)
        {
            thicknesses = new List<string>();
            error = null;
            string s = ToHalfWidth(input ?? string.Empty);
            foreach (string raw in s.Split(new[] { ' ', ',', ';', '、', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) || d <= 0 || d > 200)
                {
                    error = $"Độ dày \"{raw}\" không hợp lệ (dùng dấu chấm thập phân, ví dụ 3.2).";
                    return false;
                }
                string f = d.ToString("0.0##", CultureInfo.InvariantCulture);
                if (!ContainsThickness(thicknesses, f))
                    thicknesses.Add(f);
            }
            if (thicknesses.Count == 0)
                error = "Chưa nhập độ dày T.";
            return error == null;
        }

        /// <summary>Gộp độ dày mới vào danh sách cũ, bỏ trùng (so theo giá trị số), xếp tăng dần, giữ cách viết cũ.</summary>
        public static List<string> MergeThicknesses(IEnumerable<string> existing, IEnumerable<string> added)
        {
            var all = new List<string>(existing ?? Enumerable.Empty<string>());
            foreach (string t in added ?? Enumerable.Empty<string>())
                if (!ContainsThickness(all, t))
                    all.Add(t);
            return all.OrderBy(t => double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : double.MaxValue).ToList();
        }

        public static bool ContainsThickness(IEnumerable<string> list, string t)
        {
            return FindCanonicalThickness(list, t) != null;
        }

        /// <summary>
        /// Độ dày ghi trên Fname luôn có ít nhất 1 chữ số sau dấu chấm: "6" -> "6.0", "3.2" giữ nguyên, "0.05" giữ nguyên.
        /// Không phải số thì trả lại như nhập.
        /// </summary>
        public static string FormatThickness(string t)
        {
            string trimmed = ToHalfWidth(t ?? string.Empty).Trim();
            return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v.ToString("0.0###", CultureInfo.InvariantCulture)
                : trimmed;
        }

        /// <summary>
        /// Tìm chuỗi độ dày chuẩn trong danh sách ứng với giá trị nhập (VD: nhập "6" khớp với "6.0" trong danh sách).
        /// Trả về null nếu không có trong danh sách.
        /// </summary>
        public static string FindCanonicalThickness(IEnumerable<string> list, string t)
        {
            if (list == null || string.IsNullOrWhiteSpace(t))
                return null;

            string trimmed = ToHalfWidth(t).Trim();
            string exact = list.FirstOrDefault(x => string.Equals(x?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
                return exact;

            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            {
                return list.FirstOrDefault(x =>
                    double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                    Math.Abs(y - v) < 1e-6);
            }

            return null;
        }

        /// <summary>
        /// Lọc danh sách tên vật liệu M= phù hợp theo chuỗi người dùng đang gõ (không phân biệt hoa thường).
        /// Ưu tiên các mác bắt đầu bằng từ khóa lên trước, sau đó đến các mác có chứa từ khóa.
        /// </summary>
        public List<string> GetMatchingMaterials(string query)
        {
            var all = new List<string>();
            if (Materials == null)
                return all;

            foreach (FnameMaterialSpec spec in Materials)
            {
                if (!string.IsNullOrWhiteSpace(spec?.M))
                    all.Add(spec.M);
            }

            string q = ToHalfWidth(query ?? string.Empty).Trim();
            if (q.Length == 0)
                return all;

            var startsWith = new List<string>();
            var contains = new List<string>();

            bool querySima = q.IndexOf("sima", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             q.IndexOf("shima", StringComparison.OrdinalIgnoreCase) >= 0;

            foreach (string m in all)
            {
                if (m.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                {
                    startsWith.Add(m);
                }
                else if (m.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         (querySima && FnameRecord.IsSimaMaterial(m)))
                {
                    contains.Add(m);
                }
            }

            startsWith.AddRange(contains);
            return startsWith;
        }

        /// <summary>
        /// Lọc danh sách độ dày T= của vật liệu M= phù hợp theo chuỗi đang gõ trong ô T=.
        /// </summary>
        public List<string> GetMatchingThicknesses(string mValue, string tQuery)
        {
            var result = new List<string>();
            FnameMaterialSpec spec = FindMaterial(mValue);
            if (spec?.Thicknesses == null)
                return result;

            string q = ToHalfWidth(tQuery ?? string.Empty).Trim();
            if (q.Length == 0)
            {
                result.AddRange(spec.Thicknesses);
                return result;
            }

            var startsWith = new List<string>();
            var contains = new List<string>();

            foreach (string t in spec.Thicknesses)
            {
                if (string.IsNullOrWhiteSpace(t))
                    continue;

                if (t.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                    startsWith.Add(t);
                else if (t.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    contains.Add(t);
            }

            startsWith.AddRange(contains);
            return startsWith;
        }

        /// <summary>
        /// Thêm / cập nhật 1 vật liệu vào các dòng CSV catalog (cùng quy tắc với Apps Script ghi lên Google Sheet):
        /// trùng M (không phân biệt hoa thường) thì gộp độ dày + ghi Loai (+ Z nếu có nhập); chưa có thì thêm dòng.
        /// Thiếu cột Loai / Z thì thêm cột vào cuối tiêu đề.
        /// </summary>
        public static string[] Upsert(string[] lines, string m, string loai, string z, IList<string> thicknesses)
        {
            var rows = (lines ?? new string[0]).Select(l => l.Split(',').ToList()).ToList();
            int hi = rows.FindIndex(r => IsHeaderRow(r.ToArray()));
            if (hi < 0)
            {
                rows.Insert(0, new List<string> { "M", "Z", "T_list" });
                hi = 0;
            }
            List<string> header = rows[hi];
            int Col(params string[] names) => header.FindIndex(h => names.Contains(h.Trim().ToUpperInvariant()));
            int iM = Col("M"), iZ = Col("Z"), iT = Col("T_LIST", "T"), iL = Col("LOAI", "LOẠI", "TYPE");
            if (iT < 0) { header.Add("T_list"); iT = header.Count - 1; }
            if (iZ < 0) { header.Add("Z"); iZ = header.Count - 1; }
            if (iL < 0) { header.Add("Loai"); iL = header.Count - 1; }

            void Set(List<string> r, int i, string v)
            {
                while (r.Count <= i) r.Add(string.Empty);
                r[i] = (v ?? string.Empty).Replace(",", " ").Trim();
            }

            string key = (m ?? string.Empty).Replace(",", " ").Trim();
            List<string> row = rows.Skip(hi + 1).FirstOrDefault(r => string.Equals(Cell(r.ToArray(), iM), key, StringComparison.OrdinalIgnoreCase));
            if (row == null)
            {
                row = new List<string>();
                Set(row, iM, key);
                Set(row, iZ, z);
                Set(row, iT, string.Join(" ", MergeThicknesses(null, thicknesses)));
                Set(row, iL, loai);
                rows.Add(row);
            }
            else
            {
                var old = Cell(row.ToArray(), iT).Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                Set(row, iT, string.Join(" ", MergeThicknesses(old, thicknesses)));
                Set(row, iL, loai);
                if (!string.IsNullOrWhiteSpace(z))
                    Set(row, iZ, z);
            }

            int width = header.Count;
            return rows.Select(r =>
            {
                while (r.Count < width) r.Add(string.Empty);
                return string.Join(",", r);
            }).ToArray();
        }

        private static string ToHalfWidth(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c >= '！' && c <= '～') sb.Append((char)(c - 0xFEE0));
                else if (c == '　') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool IsHeaderRow(string[] p)
        {
            return p.Any(c => string.Equals(c.Trim(), "M", StringComparison.OrdinalIgnoreCase));
        }

        private static string Cell(string[] p, int index)
        {
            return index >= 0 && index < p.Length ? p[index].Trim() : string.Empty;
        }
    }

    [DataContract]
    public sealed class FnameMaterialSpec
    {
        public FnameMaterialSpec()
        {
        }

        public FnameMaterialSpec(string m, string z, params string[] thicknesses)
        {
            M = m;
            Z = z;
            Thicknesses = thicknesses != null ? new List<string>(thicknesses) : new List<string>();
        }

        [DataMember(Name = "m", IsRequired = false)]
        public string M { get; set; }

        [DataMember(Name = "z", IsRequired = false)]
        public string Z { get; set; }

        /// <summary>
        /// Loại vật liệu (SS, SUS, AL, BON...) = mã cột Mat trong bảng TraV. Rỗng thì đoán từ tên mác M.
        /// </summary>
        [DataMember(Name = "loai", IsRequired = false)]
        public string Loai { get; set; } = string.Empty;

        [DataMember(Name = "thicknesses", IsRequired = false)]
        public List<string> Thicknesses { get; set; } = new List<string>();
    }

    /// <summary>
    /// Dữ liệu 5 trường cốt lõi N=, M=, T=, Q=, Z= kèm bộ nhớ giữ nguyên các dòng phụ (V=...) khi cập nhật.
    /// </summary>
    public sealed class FnameRecord
    {
        public string N { get; set; } = string.Empty;
        public string M { get; set; } = string.Empty;
        public string T { get; set; } = string.Empty;
        public string Q { get; set; } = string.Empty;
        public string Z { get; set; } = string.Empty;

        public List<string> BendLines { get; set; } = new List<string>();
        public List<string> PressLines { get; set; } = new List<string>();

        public List<string> ExtraBeforeZ { get; set; } = new List<string>();
        public List<string> ExtraAfterZ { get; set; } = new List<string>();

        public static bool IsSimaMaterial(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return text.IndexOf("縞板", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("SIMA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("SHIMA", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public List<string> BuildLines(bool includePreservedExtras = true)
        {
            var lines = new List<string>
            {
                "N=" + (N ?? string.Empty).Trim(),
                "M=" + (M ?? string.Empty).Trim(),
                "T=" + (T ?? string.Empty).Trim(),
                "Q=" + (Q ?? string.Empty).Trim()
            };

            if (BendLines != null)
            {
                foreach (string bend in BendLines)
                {
                    if (!string.IsNullOrWhiteSpace(bend))
                        lines.Add(bend.Trim());
                }
            }

            if (includePreservedExtras && ExtraBeforeZ != null)
            {
                foreach (string extra in ExtraBeforeZ)
                {
                    if (!string.IsNullOrWhiteSpace(extra))
                        lines.Add(extra.Trim());
                }
            }

            lines.Add("Z=" + (Z ?? string.Empty).Trim());

            // Với các tấm sima, dưới Z luôn có dòng 裏から見た図
            bool isSima = IsSimaMaterial(M) || IsSimaMaterial(Z);
            if (isSima)
            {
                lines.Add("裏から見た図");
            }

            if (includePreservedExtras && ExtraAfterZ != null)
            {
                foreach (string extra in ExtraAfterZ)
                {
                    if (!string.IsNullOrWhiteSpace(extra))
                    {
                        string trimmed = extra.Trim();
                        // Tránh trùng lặp nếu ExtraAfterZ đã có sẵn 裏から見た図
                        if (trimmed == "裏から見た図")
                            continue;
                        lines.Add(trimmed);
                    }
                }
            }

            // Dòng Nhấn 1 lần (PressLines) NẰM Ở DƯỚI CÙNG theo yêu cầu
            if (PressLines != null)
            {
                foreach (string press in PressLines)
                {
                    if (!string.IsNullOrWhiteSpace(press))
                        lines.Add(press.Trim());
                }
            }

            return lines;
        }

        public string BuildMTextContents(bool includePreservedExtras = true) =>
            string.Join("\\P", BuildLines(includePreservedExtras));

        public string BuildPreviewText(bool includePreservedExtras = true) =>
            string.Join(Environment.NewLine, BuildLines(includePreservedExtras));

        public bool Validate(out string warning)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(N)) missing.Add("N=");
            if (string.IsNullOrWhiteSpace(M)) missing.Add("M=");
            if (string.IsNullOrWhiteSpace(T)) missing.Add("T=");
            if (string.IsNullOrWhiteSpace(Q)) missing.Add("Q=");
            if (string.IsNullOrWhiteSpace(Z)) missing.Add("Z=");

            if (missing.Count > 0)
            {
                warning = "Đang để trống: " + string.Join(", ", missing);
                return false;
            }

            if (!double.TryParse(T.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double tVal) || tVal <= 0)
            {
                warning = $"Chiều dày T={T} không phải số dương hợp lệ.";
                return false;
            }

            if (!int.TryParse(Q.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int qVal) || qVal <= 0)
            {
                warning = $"Số lượng Q={Q} phải là số nguyên dương.";
                return false;
            }

            warning = string.Empty;
            return true;
        }

        /// <summary>
        /// Kiểm tra đầy đủ 5 trường cơ bản VÀ đối chiếu với dữ liệu chuẩn trong catalog.
        /// Không cho xuất Fname nếu M=, T= hoặc Z= không có trong dữ liệu catalog, đồng thời gợi ý vào Cài đặt để thêm.
        /// </summary>
        public bool ValidateAgainstCatalog(FnameCatalogData catalog, out string warning, out bool isMissingInCatalog)
        {
            isMissingInCatalog = false;
            if (!Validate(out warning))
                return false;

            if (catalog != null && catalog.Materials != null && catalog.Materials.Count > 0)
            {
                string mTrim = (M ?? string.Empty).Trim();
                FnameMaterialSpec spec = catalog.FindMaterial(mTrim);
                if (spec == null)
                {
                    isMissingInCatalog = true;
                    warning = $"Vật liệu M=\"{mTrim}\" chưa có trong dữ liệu! Vui lòng vào Cài đặt (⚙) -> Thêm / sửa vật liệu để bổ sung.";
                    return false;
                }

                string tTrim = (T ?? string.Empty).Trim();
                string canonicalT = FnameCatalogData.FindCanonicalThickness(spec.Thicknesses, tTrim);
                if (canonicalT == null)
                {
                    isMissingInCatalog = true;
                    warning = $"Độ dày T={tTrim} chưa có trong dữ liệu của vật liệu \"{spec.M}\"! Vui lòng vào Cài đặt (⚙) -> Thêm / sửa vật liệu để bổ sung.";
                    return false;
                }

                string zTrim = (Z ?? string.Empty).Trim();
                List<string> validZs = catalog.GetDistinctZList();
                if (validZs.Count > 0 && !validZs.Contains(zTrim, StringComparer.OrdinalIgnoreCase))
                {
                    isMissingInCatalog = true;
                    warning = $"Mã phôi Z=\"{zTrim}\" chưa có trong dữ liệu! Vui lòng vào Cài đặt (⚙) -> Thêm / sửa vật liệu để bổ sung.";
                    return false;
                }
            }

            warning = string.Empty;
            return true;
        }
    }

    public static class FnameParser
    {
        private static readonly Regex UnicodeEscapeRegex =
            new Regex(@"\\[Uu]\+([0-9A-Fa-f]{4})", RegexOptions.Compiled);

        private static readonly Regex FormatControlRegex =
            new Regex(@"\\([fFcChHwWqQtTaApP][^;]*;|[LlOoKk])", RegexOptions.Compiled);

        /// <summary>
        /// Gỡ sạch các mã định dạng MTEXT (\P, {\f...;}, \U+XXXX...) thành các dòng văn bản thuần.
        /// </summary>
        public static List<string> SplitAndCleanMText(string raw)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(raw))
                return result;

            // Giải mã \U+XXXX trước
            string text = UnicodeEscapeRegex.Replace(raw, match =>
            {
                int code = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return ((char)code).ToString();
            });

            // Bảo vệ dấu ngoặc nhọn escape \{ \} và \\
            const string EscBackslash = "\uE000";
            const string EscOpenBrace = "\uE001";
            const string EscCloseBrace = "\uE002";

            text = text.Replace(@"\\", EscBackslash)
                       .Replace(@"\{", EscOpenBrace)
                       .Replace(@"\}", EscCloseBrace);

            // Chuyển \P (in hoa - ngắt dòng MTEXT) thành \n
            text = text.Replace("\\P", "\n").Replace("\r\n", "\n").Replace('\r', '\n');

            // Gỡ mã điều khiển font/màu/căn lề và dấu ngoặc nhọn bao khối MTEXT
            text = FormatControlRegex.Replace(text, string.Empty);
            text = text.Replace("{", string.Empty).Replace("}", string.Empty);

            text = text.Replace(EscBackslash, "\\")
                       .Replace(EscOpenBrace, "{")
                       .Replace(EscCloseBrace, "}");

            string[] rawLines = text.Split('\n');
            foreach (string rawLine in rawLines)
            {
                string trimmed = rawLine.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Nếu dòng là chuỗi gộp phân tách dấu phẩy kiểu N=...,M=...,T=...,Q=...,Z=...
                if (LooksLikeCommaSeparatedFname(trimmed))
                {
                    foreach (string part in trimmed.Split(','))
                    {
                        string p = part.Trim();
                        if (!string.IsNullOrEmpty(p))
                            result.Add(p);
                    }
                }
                else
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        public static FnameRecord Parse(IEnumerable<string> rawEntityStrings)
        {
            var record = new FnameRecord();
            if (rawEntityStrings == null)
                return record;

            bool seenZ = false;
            foreach (string raw in rawEntityStrings)
            {
                foreach (string line in SplitAndCleanMText(raw))
                {
                    if (StartsWithKey(line, "N=", out string valN))
                    {
                        record.N = valN;
                    }
                    else if (StartsWithKey(line, "M=", out string valM))
                    {
                        record.M = valM;
                    }
                    else if (StartsWithKey(line, "T=", out string valT))
                    {
                        record.T = valT;
                    }
                    else if (StartsWithKey(line, "Q=", out string valQ))
                    {
                        record.Q = valQ;
                    }
                    else if (StartsWithKey(line, "Z=", out string valZ))
                    {
                        record.Z = valZ;
                        seenZ = true;
                    }
                    else if (line.IndexOf("発押し", StringComparison.Ordinal) >= 0)
                    {
                        record.PressLines.Add(line);
                    }
                    else if (IsBendLine(line))
                    {
                        record.BendLines.Add(line);
                    }
                    else
                    {
                        if (seenZ)
                            record.ExtraAfterZ.Add(line);
                        else
                            record.ExtraBeforeZ.Add(line);
                    }
                }
            }

            return record;
        }

        public static bool IsBendLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string s = line.Trim();
            if (s.IndexOf("発押し", StringComparison.Ordinal) >= 0) return false;
            return s.StartsWith("V=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("V1=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("V2=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("V3=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("U=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("シマ目", StringComparison.Ordinal);
        }

        /// <summary>
        /// Kiểm tra một dòng TEXT đơn lẻ trên CAD có phải là một dòng thành phần của cụm F-NAME không.
        /// </summary>
        public static bool IsFnameComponentLine(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return false;

            List<string> cleaned = SplitAndCleanMText(rawText);
            if (cleaned.Count == 0)
                return false;

            string s = cleaned[0];
            return s.StartsWith("N=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("M=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("T=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("Q=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("Z=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("V=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("V1=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("V2=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("V3=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("U=", StringComparison.OrdinalIgnoreCase) ||
                   s.StartsWith("シマ目", StringComparison.Ordinal) ||
                   s.StartsWith("裏から見た図", StringComparison.Ordinal) ||
                   s.IndexOf("発押し", StringComparison.Ordinal) >= 0 ||
                   s.StartsWith("内R", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// N= mẫu theo tên bản vẽ: "47079-S.dwg" -> "47079-1" (bỏ đuôi -s / -a, không phân biệt hoa thường).
        /// </summary>
        public static string DefaultPartNameFromDrawing(string drawingPath)
        {
            string name = Path.GetFileNameWithoutExtension(drawingPath ?? string.Empty).Trim();
            if (name.EndsWith("-s", StringComparison.OrdinalIgnoreCase) || name.EndsWith("-a", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 2);
            return name.Length == 0 ? string.Empty : name + "-1";
        }

        /// <summary>
        /// Kiểm tra xem tên bản vẽ hoặc đường dẫn có phải là tên tạm mặc định chưa lưu của CAD hay không
        /// (VD: Drawing1, Drawing2, Drawing1.dwg, 図面1.dwg, banve1.dwg, New Drawing, Untitled...).
        /// </summary>
        public static bool IsGenericDrawingName(string drawingPathOrName)
        {
            if (string.IsNullOrWhiteSpace(drawingPathOrName))
                return true;
            string name = Path.GetFileNameWithoutExtension(drawingPathOrName).Trim();
            if (string.IsNullOrEmpty(name))
                return true;
            return Regex.IsMatch(name, @"^(drawing|図面|banve|untitled|new(\s*drawing)?|document)(\s*\d+)?$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Kiểm tra xem tên chi tiết N= có phải là tên tạm sinh tự động từ bản vẽ chưa lưu hay không
        /// (VD: Drawing1-1, Drawing2-1, Drawing-1, 図面1-1...).
        /// </summary>
        public static bool IsGenericPartName(string partName)
        {
            if (string.IsNullOrWhiteSpace(partName))
                return true;
            string trimmed = partName.Trim();
            return Regex.IsMatch(trimmed, @"^(drawing|図面|banve|untitled|new(\s*drawing)?|document)(\s*\d+)?(-\d+)?$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Tăng tự động hậu tố số của N= (VD: "47079-1" -> "47079-2", "47079-09" -> "47079-10", "COVER" -> "COVER-1").
        /// </summary>
        public static string IncrementPartName(string currentN)
        {
            if (string.IsNullOrWhiteSpace(currentN))
                return "1";

            string trimmed = currentN.Trim();
            int dashIdx = trimmed.LastIndexOf('-');
            if (dashIdx >= 0 && dashIdx < trimmed.Length - 1)
            {
                string prefix = trimmed.Substring(0, dashIdx);
                string suffix = trimmed.Substring(dashIdx + 1);
                if (suffix.All(char.IsDigit) &&
                    int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
                {
                    string nextNum = (num + 1).ToString(CultureInfo.InvariantCulture);
                    if (suffix.Length > 1 && suffix.StartsWith("0", StringComparison.Ordinal) && nextNum.Length < suffix.Length)
                    {
                        nextNum = nextNum.PadLeft(suffix.Length, '0');
                    }
                    return prefix + "-" + nextNum;
                }
            }

            if (trimmed.All(char.IsDigit) &&
                int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pureNum))
            {
                return (pureNum + 1).ToString(CultureInfo.InvariantCulture);
            }

            return trimmed + "-1";
        }

        /// <summary>
        /// Tính chiều cao chữ F-NAME theo khoảng kéo chuột (deltaX, deltaY) tính từ điểm góc trên-trái (Click 1) tới con trỏ (Click 2).
        /// Đảm bảo cạnh dưới của dòng cuối cùng (Z=...) bám sát vị trí chuột khi kéo xuống / kéo chéo.
        /// </summary>
        public static double ComputeDraggedTextHeight(
            double deltaX,
            double deltaY,
            int lineCount,
            int maxLineCharLength,
            double minHeight = 0.5)
        {
            int safeLines = Math.Max(1, lineCount);
            int safeChars = Math.Max(6, maxLineCharLength);

            // Tổng chiều cao khối chữ gồm safeLines dòng với bước giãn dòng = 5/3 * H
            double blockHeightFactor = (safeLines - 1) * (5.0 / 3.0) + 1.0;
            // Bề rộng xấp xỉ của dòng dài nhất
            double blockWidthFactor = safeChars * 0.72;

            double hFromY = Math.Abs(deltaY) / blockHeightFactor;
            double hFromX = Math.Abs(deltaX) / blockWidthFactor;
            double rawHeight = Math.Max(hFromY, hFromX);

            double rounded = Math.Round(rawHeight, 1);
            return Math.Max(minHeight, rounded);
        }

        private static bool StartsWithKey(string line, string keyPrefix, out string value)
        {
            if (line != null && line.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                value = line.Substring(keyPrefix.Length).Trim();
                return true;
            }

            value = string.Empty;
            return false;
        }

        private static bool LooksLikeCommaSeparatedFname(string line)
        {
            if (line.IndexOf(',') < 0)
                return false;

            int keyHits = 0;
            if (line.IndexOf("N=", StringComparison.OrdinalIgnoreCase) >= 0) keyHits++;
            if (line.IndexOf("M=", StringComparison.OrdinalIgnoreCase) >= 0) keyHits++;
            if (line.IndexOf("T=", StringComparison.OrdinalIgnoreCase) >= 0) keyHits++;
            if (line.IndexOf("Q=", StringComparison.OrdinalIgnoreCase) >= 0) keyHits++;
            if (line.IndexOf("Z=", StringComparison.OrdinalIgnoreCase) >= 0) keyHits++;
            return keyHits >= 2;
        }
    }
}

