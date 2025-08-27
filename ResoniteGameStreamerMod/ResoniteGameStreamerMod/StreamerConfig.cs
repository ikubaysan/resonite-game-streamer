// StreamerConfig.cs
using Elements.Core;
using Renderite.Shared;
using ResoniteModLoader;

namespace ResoniteGameStreamerMod
{
    internal static class StreamerConfig
    {
        internal static bool Enabled;
        internal static bool RgbMode;
        internal static int CanvasW;
        internal static int CanvasH;
        internal static string CanvasName;

        internal static int MaxPixelsPerUpdate = 20000;

        internal static bool ReInitializeNeeded;

        internal static readonly colorX[] GB = new colorX[4];

        internal static void InitializeGBPalette()
        {
            GB[0] = new colorX(255 / 1000f, 255 / 1000f, 255 / 1000f, 1, ColorProfile.Linear);
            GB[1] = new colorX(192 / 1000f, 192 / 1000f, 192 / 1000f, 1, ColorProfile.Linear);
            GB[2] = new colorX(96 / 1000f, 96 / 1000f, 96 / 1000f, 1, ColorProfile.Linear);
            GB[3] = new colorX(0 / 1000f, 0 / 1000f, 0 / 1000f, 1, ColorProfile.Linear);
        }

        internal static void InitializeFrom(ModConfiguration cfg)
        {
            UpdateFrom(cfg, firstTime: true);
        }

        internal static void UpdateFrom(ModConfiguration cfg, bool firstTime = false)
        {
            bool newEnabled = cfg.GetValue(ResoniteGameStreamerMod.ENABLED);
            bool newRgb = cfg.GetValue(ResoniteGameStreamerMod.RGB_MODE);
            int newW = cfg.GetValue(ResoniteGameStreamerMod.CANVAS_SLOT_WIDTH);
            int newH = cfg.GetValue(ResoniteGameStreamerMod.CANVAS_SLOT_HEIGHT);
            string newName = cfg.GetValue(ResoniteGameStreamerMod.CANVAS_SLOT_NAME);
            int newMax = cfg.GetValue(ResoniteGameStreamerMod.MAX_PIXELS_PER_UPDATE);

            if (newMax < 1000) newMax = 1000; // small safety floor
            MaxPixelsPerUpdate = newMax;

            bool dimsValid = newW is >= 100 and <= 999 && newH is >= 100 and <= 999;

            bool trigger = (!firstTime) && (
                (dimsValid && (newW != CanvasW || newH != CanvasH)) ||
                (newRgb != RgbMode) ||
                (newName != CanvasName)
            );

            ReInitializeNeeded = trigger;

            Enabled = newEnabled;
            if (dimsValid)
            {
                CanvasW = newW;
                CanvasH = newH;
            }
            else
            {
                ResoniteGameStreamerMod.LogWarn($"[Config] Invalid canvas dimensions ({newW}x{newH}). Keeping previous {CanvasW}x{CanvasH}.");
            }
            RgbMode = newRgb;
            CanvasName = newName;

            ResoniteGameStreamerMod.LogMsg($"[Config] enabled={Enabled}, rgb_mode={RgbMode}, size={CanvasW}x{CanvasH}, slotName={CanvasName}, maxPerUpdate={MaxPixelsPerUpdate}, reinit={ReInitializeNeeded}");
        }
    }
}
