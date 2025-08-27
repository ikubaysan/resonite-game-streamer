// Mod.cs
using Elements.Core;
using HarmonyLib;
using ResoniteModLoader;

namespace ResoniteGameStreamerMod
{
    public class ResoniteGameStreamerMod : ResoniteMod
    {
        public override string Author => "Ikubaysan";
        public override string Name => "ResoniteGameStreamerMod";
        public override string Version => "1.0.0";

        [AutoRegisterConfigKey]
        internal static readonly ModConfigurationKey<bool> ENABLED =
            new("enabled", "Enable mod", () => true);

        [AutoRegisterConfigKey]
        internal static readonly ModConfigurationKey<bool> RGB_MODE =
            new("rgb_mode", "RGB mode (true), or Greyscale GB mode (false)", () => true);

        [AutoRegisterConfigKey]
        internal static readonly ModConfigurationKey<int> CANVAS_SLOT_WIDTH =
            new("canvas_slot_width", "Pixel width of the canvas slot", () => 160);

        [AutoRegisterConfigKey]
        internal static readonly ModConfigurationKey<int> CANVAS_SLOT_HEIGHT =
            new("canvas_slot_height", "Pixel height of the canvas slot", () => 144);

        [AutoRegisterConfigKey]
        internal static readonly ModConfigurationKey<string> CANVAS_SLOT_NAME =
            new("canvas_slot_name", "Name of the canvas slot", () => "ResoniteGameStreamerUIXCanvasRGB");

        // NEW: throttle per Resonite update (pixels)
        [AutoRegisterConfigKey]
        internal static readonly ModConfigurationKey<int> MAX_PIXELS_PER_UPDATE =
            new("max_pixels_per_update", "Max pixels to apply per Resonite update (chunking)", () => 10000);

        internal static ModConfiguration Config;

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            StreamerConfig.InitializeFrom(Config);
            Config.Save(true);

            StreamerConfig.InitializeGBPalette();

            var harmony = new Harmony("com.ikubaysan.ResoniteGameStreamerMod");
            harmony.PatchAll();

            LogMsg("[Init] ResoniteGameStreamerMod loaded");
        }

        // ---------- static-safe logging helpers ----------
        internal static void LogMsg(string s) => ResoniteMod.Msg(s);
        internal static void LogWarn(string s) => ResoniteMod.Warn(s);
        internal static void LogError(string s) => ResoniteMod.Error(s);
    }
}
