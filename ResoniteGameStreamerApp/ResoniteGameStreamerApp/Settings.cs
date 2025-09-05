using System;
using System.IO;
using System.Text.Json;

namespace ResoniteGameStreamerApp
{
    public sealed class AppSettings
    {
        public int FrameWidth { get; set; } = 240;
        public int FrameHeight { get; set; } = 160;
        public int TargetFramerate { get; set; } = 36;
        public string TargetWindowTitle { get; set; } = "mGBA";
        public string ColorMode { get; set; } = "RGB"; // "RGB" or "Greyscale"
        public int BorderWidth { get; set; } = 8;
        public int TitleBarHeight { get; set; } = 30;
        public int FullFrameIntervalSeconds { get; set; } = 30;
    }

    public static class SettingsManager
    {
        private const string FileName = "settings.json";
        private static string SettingsPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null) return s;
                }
            }
            catch { /* ignore and fall back to defaults */ }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Log the error
            }
        }
    }
}
