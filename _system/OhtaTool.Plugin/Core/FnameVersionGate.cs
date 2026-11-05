using System.Collections.Generic;

namespace OhtaTool.Plugin.Core
{
    /// <summary>
    /// So bản đang chạy với phien_ban_toi_thieu của tab cau_hinh. Để riêng file (không nằm trong TamCore)
    /// vì dùng UpdateVersion — các plugin khác liên kết TamCore.cs không phải kéo theo UpdateCore.cs.
    /// </summary>
    public static class FnameVersionGate
    {
        /// <summary>Máy đang chạy bản thấp hơn phien_ban_toi_thieu trên Sheet -> phải cập nhật trước khi gửi dữ liệu lên Sheet.</summary>
        public static bool IsOutdated(string loadedVersion, IDictionary<string, string> config)
        {
            if (config == null || !config.TryGetValue(FnameSheetConfig.MinVersionKey, out string min) || !UpdateVersion.IsValid(min))
                return false;
            return UpdateVersion.IsValid(loadedVersion) && UpdateVersion.Compare(loadedVersion, min) < 0;
        }
    }
}
