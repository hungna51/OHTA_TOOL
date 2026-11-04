using System;
using System.Collections.Generic;
using System.Linq;

namespace OhtaTool.Plugin.Core
{
    /// <summary>Bộ thông số in. null = "không có / không đặt" (để quy tắc ưu tiên bỏ qua).</summary>
    public sealed class PrintParams
    {
        public string Device, Media, StyleSheet;
        public bool? Lineweights, Merge, OpenAfter;

        public PrintParams Clone() => (PrintParams)MemberwiseClone();
    }

    /// <summary>Cấu hình in có tên do người dùng đặt (vd "Đen trắng").</summary>
    public sealed class PrintProfile
    {
        public string Name;
        public PrintParams Params = new PrintParams();
    }

    /// <summary>Toàn bộ cài đặt tab PRINT (lưu registry HKCU\Software\OFT_Lisp\IN).</summary>
    public sealed class PrintSettingsData
    {
        public PrintParams Defaults = new PrintParams();
        public List<string> FrameLayers = new List<string>();
        public List<PrintProfile> Profiles = new List<PrintProfile>();
        /// <summary>Tên cấu hình chọn lần trước; null = "Theo bản vẽ".</summary>
        public string LastProfile;

        public static PrintSettingsData CreateDefault(bool isZwcad) => new PrintSettingsData
        {
            Defaults = new PrintParams
            {
                Device = isZwcad ? "DWG to PDF.pc5" : "DWG To PDF.pc3",
                Media = "ISO_full_bleed_A3_(420.00_x_297.00_MM)",
                StyleSheet = "monochrome.ctb",
                Lineweights = true,
                Merge = true,
                OpenAfter = true
            },
            FrameLayers = new List<string> { "Khung_IN" },
            Profiles = new List<PrintProfile>(),
            LastProfile = null
        };
    }

    /// <summary>Những gì máy đang có: máy in, khổ giấy theo máy in, bảng kiểu in.</summary>
    public interface IPlotCatalog
    {
        IList<string> Devices { get; }
        IList<string> MediaFor(string device);
        IList<string> StyleSheets { get; }
    }

    /// <summary>Kết quả giải quyết: mọi trường của Values khác null.</summary>
    public sealed class ResolvedPrint
    {
        public PrintParams Values = new PrintParams();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// Quy tắc "ưu tiên bản vẽ": nguồn theo thứ tự cấu hình (nếu chọn) → bản vẽ → mặc định ⚙ → mục đầu
    /// tiên máy có. Nguồn có giá trị mà máy không có thì bỏ qua và cảnh báo.
    /// </summary>
    public static class PrintSettingsResolver
    {
        public const string FromDrawingName = "Theo bản vẽ";

        private const string LabelDrawing = "theo bản vẽ";
        private const string LabelDefault = "mặc định";
        private const string LabelMachine = "có sẵn trên máy";

        public static ResolvedPrint Resolve(PrintProfile profile, PrintParams drawing, PrintParams defaults, IPlotCatalog catalog)
        {
            var res = new ResolvedPrint();
            PrintParams prof = profile?.Params;
            drawing = drawing ?? new PrintParams();
            defaults = defaults ?? new PrintParams();

            res.Values.Device = Pick(prof?.Device, drawing.Device, defaults.Device,
                catalog?.Devices, "máy in", res.Warnings);
            res.Values.Media = Pick(prof?.Media, drawing.Media, defaults.Media,
                catalog?.MediaFor(res.Values.Device), "khổ giấy", res.Warnings);
            res.Values.StyleSheet = Pick(prof?.StyleSheet, drawing.StyleSheet, defaults.StyleSheet,
                catalog?.StyleSheets, "bảng ctb", res.Warnings);

            // Nét in không có ô cài đặt: chỉ theo page setup của bản vẽ, không đặt thì bật
            res.Values.Lineweights = drawing.Lineweights ?? true;
            res.Values.Merge = prof?.Merge ?? defaults.Merge ?? true;
            res.Values.OpenAfter = prof?.OpenAfter ?? defaults.OpenAfter ?? true;
            return res;
        }

        private static string Pick(string fromProfile, string fromDrawing, string fromDefault,
            IList<string> available, string kind, List<string> warnings)
        {
            available = available ?? new List<string>();
            var sources = new[]
            {
                (Value: fromProfile, Label: (string)null),
                (Value: fromDrawing, Label: LabelDrawing),
                (Value: fromDefault, Label: LabelDefault)
            };

            string firstSkipped = null;
            foreach (var s in sources)
            {
                if (string.IsNullOrWhiteSpace(s.Value))
                    continue;
                string hit = available.FirstOrDefault(a => string.Equals(a, s.Value.Trim(), StringComparison.OrdinalIgnoreCase));
                if (hit != null)
                {
                    if (firstSkipped != null)
                        warnings.Add($"Không có {firstSkipped} → đang dùng {hit} ({s.Label})");
                    return hit;
                }
                if (firstSkipped == null)
                    firstSkipped = s.Value.Trim();
            }

            if (available.Count == 0)
            {
                warnings.Add($"Không có {kind} nào trên máy");
                return string.Empty;
            }

            string fallback = available[0];
            if (firstSkipped != null)
                warnings.Add($"Không có {firstSkipped} → đang dùng {fallback} ({LabelMachine})");
            return fallback;
        }
    }

    /// <summary>Mã hoá cài đặt thành chuỗi JSON (giá trị registry). JSON hỏng → giá trị rỗng, không ném lỗi.</summary>
    public static class PrintSettingsCodec
    {
        public static string ParamsToJson(PrintParams p) => JsonLite.Serialize(ParamsToObject(p ?? new PrintParams()));

        public static PrintParams ParamsFromJson(string json) => ParamsFromObject(TryParse(json) as JsonObject);

        public static string ProfilesToJson(IList<PrintProfile> list)
        {
            var arr = new List<object>();
            foreach (var pr in list ?? new List<PrintProfile>())
            {
                if (pr == null || string.IsNullOrWhiteSpace(pr.Name))
                    continue;
                var o = new JsonObject();
                o.Set("name", pr.Name);
                o.Set("params", ParamsToObject(pr.Params ?? new PrintParams()));
                arr.Add(o);
            }
            return JsonLite.Serialize(arr);
        }

        public static List<PrintProfile> ProfilesFromJson(string json)
        {
            var result = new List<PrintProfile>();
            if (!(TryParse(json) is List<object> arr))
                return result;
            foreach (var item in arr)
            {
                if (!(item is JsonObject o))
                    continue;
                string name = o.GetString("name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                result.Add(new PrintProfile { Name = name, Params = ParamsFromObject(o.Get("params") as JsonObject) });
            }
            return result;
        }

        public static string LayersToJson(IList<string> layers) =>
            JsonLite.Serialize((layers ?? new List<string>()).Where(l => !string.IsNullOrWhiteSpace(l)).Cast<object>().ToList());

        public static List<string> LayersFromJson(string json)
        {
            if (!(TryParse(json) is List<object> arr))
                return new List<string>();
            return arr.OfType<string>().Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        }

        private static object TryParse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try { return JsonLite.Parse(json); }
            catch (Exception) { return null; }
        }

        private static JsonObject ParamsToObject(PrintParams p)
        {
            var o = new JsonObject();
            o.Set("device", p.Device);
            o.Set("media", p.Media);
            o.Set("styleSheet", p.StyleSheet);
            o.Set("lineweights", p.Lineweights);
            o.Set("merge", p.Merge);
            o.Set("openAfter", p.OpenAfter);
            return o;
        }

        private static PrintParams ParamsFromObject(JsonObject o)
        {
            if (o == null)
                return new PrintParams();
            return new PrintParams
            {
                Device = o.GetString("device"),
                Media = o.GetString("media"),
                StyleSheet = o.GetString("styleSheet"),
                Lineweights = o.Get("lineweights") as bool?,
                Merge = o.Get("merge") as bool?,
                OpenAfter = o.Get("openAfter") as bool?
            };
        }
    }
}
