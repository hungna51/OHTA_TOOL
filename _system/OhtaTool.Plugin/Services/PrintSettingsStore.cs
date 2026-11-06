using System;
using Microsoft.Win32;
using OhtaTool.Plugin.Core;

namespace OhtaTool.Plugin.Services
{
    /// <summary>Đọc/ghi cài đặt tab PRINT ở HKCU\Software\OFT_Lisp\IN (theo máy).</summary>
    public static class PrintSettingsStore
    {
        public const string RegSubKey = @"Software\OFT_Lisp\IN";

        private static bool IsZwcad =>
#if ZWCAD
            true;
#else
            false;
#endif

        public static PrintSettingsData Load()
        {
            var data = PrintSettingsData.CreateDefault(IsZwcad);
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegSubKey, writable: false))
                {
                    if (key == null)
                        return data;

                    if (key.GetValue("defaults") is string defJson)
                    {
                        PrintParams p = PrintSettingsCodec.ParamsFromJson(defJson);
                        // Trường nào registry thiếu thì giữ mặc định gốc
                        data.Defaults.Device = p.Device ?? data.Defaults.Device;
                        data.Defaults.Media = p.Media ?? data.Defaults.Media;
                        data.Defaults.StyleSheet = p.StyleSheet ?? data.Defaults.StyleSheet;
                        data.Defaults.Lineweights = p.Lineweights ?? data.Defaults.Lineweights;
                        data.Defaults.Merge = p.Merge ?? data.Defaults.Merge;
                        data.Defaults.OpenAfter = p.OpenAfter ?? data.Defaults.OpenAfter;
                    }
                    if (key.GetValue("frame_layers") is string layersJson)
                    {
                        var layers = PrintSettingsCodec.LayersFromJson(layersJson);
                        if (layers.Count > 0)
                            data.FrameLayers = layers;
                    }
                    if (key.GetValue("profiles") is string profJson)
                        data.Profiles = PrintSettingsCodec.ProfilesFromJson(profJson);
                    data.SkipOverwritePrompt = (key.GetValue("skip_overwrite_prompt") as string) == "1";
                    string last = key.GetValue("last_profile") as string;
                    data.LastProfile = string.IsNullOrWhiteSpace(last) ? null : last;
                }
            }
            catch (Exception)
            {
                // Registry hỏng / không đọc được -> dùng mặc định
            }
            return data;
        }

        public static void Save(PrintSettingsData data)
        {
            if (data == null)
                return;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegSubKey))
                {
                    key.SetValue("defaults", PrintSettingsCodec.ParamsToJson(data.Defaults));
                    key.SetValue("frame_layers", PrintSettingsCodec.LayersToJson(data.FrameLayers));
                    key.SetValue("profiles", PrintSettingsCodec.ProfilesToJson(data.Profiles));
                    key.SetValue("last_profile", data.LastProfile ?? string.Empty);
                    key.SetValue("skip_overwrite_prompt", data.SkipOverwritePrompt ? "1" : "0");
                }
            }
            catch (Exception ex)
            {
                OhtaTool.Plugin.Core.BugLog.Write("OHTA/PrintSettingsStore", ex);
                CadCompat.WriteMessage("\n[OI] Không ghi được cài đặt in: " + ex.Message + "\n");
            }
        }

        /// <summary>Chỉ ghi cấu hình chọn lần trước (đổi ô Cấu hình trên tab).</summary>
        public static void SaveLastProfile(string profileName)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegSubKey))
                    key.SetValue("last_profile", profileName ?? string.Empty);
            }
            catch (Exception)
            {
            }
        }
    }
}
