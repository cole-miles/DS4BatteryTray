using System;
using DS4BatteryTray.Core.Output;
using Microsoft.Win32;

namespace DS4BatteryTray
{
    internal static class LightBarSettingsStore
    {
        private const string RegistryPath = @"Software\ColeMiles\DS4BatteryTray";
        private const string ModeValueName = "LightBarMode";
        private const string ColorValueName = "LightBarColor";

        public static LightBarSettings Load(out string error)
        {
            LightBarSettings settings = new LightBarSettings();
            error = "";

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
                {
                    if (key == null)
                    {
                        return settings;
                    }

                    string modeText = Convert.ToString(key.GetValue(ModeValueName, ""));
                    LightBarMode mode;
                    if (Enum.TryParse(modeText, true, out mode) && Enum.IsDefined(typeof(LightBarMode), mode))
                    {
                        settings.Mode = mode;
                    }

                    object colorValue = key.GetValue(ColorValueName, null);
                    if (colorValue != null)
                    {
                        int rgb = Convert.ToInt32(colorValue);
                        if (rgb >= 0 && rgb <= 0xFFFFFF)
                        {
                            settings.StaticColor = RgbColor.FromRgbInteger(rgb);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = "Could not load light-bar settings: " + ex.Message;
            }

            return settings;
        }

        public static bool TrySave(LightBarSettings settings, out string error)
        {
            error = "";
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (key == null)
                    {
                        error = "Windows could not open the current-user settings key.";
                        return false;
                    }

                    key.SetValue(ModeValueName, settings.Mode.ToString(), RegistryValueKind.String);
                    key.SetValue(ColorValueName, settings.StaticColor.ToRgbInteger(), RegistryValueKind.DWord);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = "Could not save light-bar settings: " + ex.Message;
                return false;
            }
        }
    }
}
