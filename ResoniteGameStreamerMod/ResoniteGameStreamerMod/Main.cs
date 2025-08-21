using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using Renderite.Shared;
using ResoniteModLoader;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ResoniteGameStreamerMod
{
    public class ResoniteGameStreamerMod : ResoniteMod
    {
        public override string Author => "Ikubaysan";
        public override string Name => "ResoniteGameStreamerMod";
        public override string Version => "1.0.0";

        // -------------------- Config --------------------
        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> ENABLED =
            new ModConfigurationKey<bool>("enabled", "Enable mod", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> RGB_MODE =
            new ModConfigurationKey<bool>("rgb_mode", "RGB mode (true), or Greyscale GB mode (false)", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<int> CANVAS_SLOT_WIDTH =
            new ModConfigurationKey<int>("canvas_slot_width", "Pixel width of the canvas slot", () => 160);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<int> CANVAS_SLOT_HEIGHT =
            new ModConfigurationKey<int>("canvas_slot_height", "Pixel height of the canvas slot", () => 144);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> CANVAS_SLOT_NAME =
            new ModConfigurationKey<string>("canvas_slot_name", "Name of the canvas slot", () => "ResoniteGameStreamerUIXCanvasRGB");

        private static ModConfiguration Config;

        // Cached config for fast checks and to trigger reinit when changed
        private static bool enabledCached;
        private static bool rgbModeCached;
        private static int canvasW;
        private static int canvasH;
        private static string canvasName;

        // GB palette (4 shades)
        private static readonly colorX[] GameBoyColors = new colorX[4];

        private static bool _reInitializeNeeded;

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            UpdateCachedConfigOptions();
            Config.Save(true);

            Harmony harmony = new Harmony("com.ikubaysan.ResoniteGameStreamerMod");
            harmony.PatchAll();

            Msg("[Init] ResoniteGameStreamerMod loaded");
            InitializeGBPalette();
        }

        private static void InitializeGBPalette()
        {
            // Keep scaling consistent with your existing mods (divide by 1000f)
            GameBoyColors[0] = new colorX(255 / 1000f, 255 / 1000f, 255 / 1000f, 1, ColorProfile.Linear); // White
            GameBoyColors[1] = new colorX(192 / 1000f, 192 / 1000f, 192 / 1000f, 1, ColorProfile.Linear); // Light Gray
            GameBoyColors[2] = new colorX(96 / 1000f, 96 / 1000f, 96 / 1000f, 1, ColorProfile.Linear);    // Dark Gray
            GameBoyColors[3] = new colorX(0 / 1000f, 0 / 1000f, 0 / 1000f, 1, ColorProfile.Linear);       // Black
        }

        private static void UpdateCachedConfigOptions()
        {
            bool newEnabled = Config.GetValue(ENABLED);
            bool newRgbMode = Config.GetValue(RGB_MODE);
            int newW = Config.GetValue(CANVAS_SLOT_WIDTH);
            int newH = Config.GetValue(CANVAS_SLOT_HEIGHT);
            string newName = Config.GetValue(CANVAS_SLOT_NAME);

            bool dimsValid = newW >= 100 && newH >= 100 && newW <= 999 && newH <= 999;

            // Trigger reinit if any structural thing changes:
            bool trigger =
                (dimsValid && (newW != canvasW || newH != canvasH)) ||
                (newRgbMode != rgbModeCached) ||
                (newName != canvasName);

            _reInitializeNeeded = trigger;

            enabledCached = newEnabled;
            if (dimsValid)
            {
                canvasW = newW;
                canvasH = newH;
            }
            else
            {
                Warn($"[Config] Invalid canvas dimensions ({newW}x{newH}). Keeping previous {canvasW}x{canvasH}.");
            }
            rgbModeCached = newRgbMode;
            canvasName = newName;

            Msg($"[Config] enabled={enabledCached}, rgb_mode={rgbModeCached}, size={canvasW}x{canvasH}, slotName={canvasName}, reinit={_reInitializeNeeded}");
        }

        // -------------------- Patching --------------------
        private const string PixelDataMMFName = "ResonitePixelData";
        private const string ClientAckMMFName = "ResoniteClientRenderConfirmation";
        private const int ClientAckMMFSize = sizeof(Int32);

        class Patcher
        {
            private static bool initialized = false;
            private static Canvas _canvas;

            private static MemoryMappedFile _mmfPixel;
            private static MemoryMappedViewStream _mmfView;
            private static BinaryReader _reader;

            private static MemoryMappedFile _mmfAck;
            private static int latestFrameTick = -1;

            private static RawGraphic[][] rgbRaw;                  // RGB mode: 1 RawGraphic per pixel
            private static RawGraphic[][] gbRawPerColor;           // GB mode: 4 RawGraphics per pixel (flattened per row)
            private static HorizontalLayout[] rowLayouts;

            private static int[] pxData;
            private static int pxDataLen = -1;
            public static int[] rowPairs;
            private static int rowPairsLen;

            private static DateTime lastInitAttempt = DateTime.MinValue;
            private static bool mutatingCanvas = false;

            private static void EnsureAckMMF()
            {
                if (_mmfAck == null)
                {
                    _mmfAck = MemoryMappedFile.CreateOrOpen(ClientAckMMFName, ClientAckMMFSize);
                    Msg("[Ack] Created/Opened client ack MMF");
                }
            }

            [HarmonyPatch(typeof(Canvas), "FinishCanvasUpdate")]
            public static class CanvasFinishCanvasUpdatePatch
            {
                static void Postfix(Canvas __instance)
                {
                    if (!enabledCached) return;
                    if (__instance.Slot.Name != canvasName) return;

                    if (__instance != _canvas)
                    {
                        Msg($"[Canvas] Found target canvas '{__instance.Slot.Name}' (new instance).");
                        _canvas = __instance;
                        initialized = false;
                    }

                    if (!initialized || _reInitializeNeeded)
                    {
                        // Avoid thrashing on frequent updates
                        if ((DateTime.UtcNow - lastInitAttempt).TotalSeconds < 5) return;

                        lastInitAttempt = DateTime.UtcNow;
                        Msg($"[Canvas] Initializing (rgb_mode={rgbModeCached}, size={canvasW}x{canvasH}) ...");
                        try
                        {
                            InitializeCanvas(__instance);
                            initialized = true;
                            _reInitializeNeeded = false;
                            Msg("[Canvas] Initialization complete.");
                        }
                        catch (Exception ex)
                        {
                            Error($"[Canvas] Initialization failed: {ex}");
                            initialized = false;
                        }
                    }
                }

                static void InitializeCanvas(Canvas cv)
                {
                    // Size
                    cv.Size.Value = new float2(canvasW, canvasH);
                    Msg($"[Canvas] Set size to {cv.Size.Value}");

                    // Locate content slot
                    Slot background = cv.Slot.FindChild("Background");
                    if (background == null) throw new Exception("Missing child slot 'Background'");
                    Slot image = background.FindChild("Image");
                    if (image == null) throw new Exception("Missing child slot 'Image'");
                    Slot content = image.FindChild("Content");
                    if (content == null) throw new Exception("Missing child slot 'Content'");

                    // Prepare buffers before we destroy children (so we can early-fail if MMF missing)
                    pxData = new int[canvasW * canvasH];
                    rowPairs = new int[canvasW * canvasH];

                    // MMF open
                    _mmfPixel = MemoryMappedFile.OpenExisting(PixelDataMMFName);
                    _mmfView = _mmfPixel.CreateViewStream();
                    _reader = new BinaryReader(_mmfView);
                    latestFrameTick = -1;
                    Msg($"[MMF] Opened '{PixelDataMMFName}' for reading.");

                    // Wipe existing row slots
                    content.DestroyChildren();
                    Msg("[Canvas] Cleared Content children.");

                    // Build rows
                    rowLayouts = new HorizontalLayout[canvasH];

                    if (rgbModeCached)
                    {
                        // RGB mode: 1 RawGraphic per pixel
                        rgbRaw = new RawGraphic[canvasH][];
                        for (int y = 0; y < canvasH; y++)
                        {
                            Slot row = content.AddSlot($"Row{y}");
                            row.AttachComponent<RectTransform>();
                            var h = row.AttachComponent<HorizontalLayout>();
                            h.PaddingTop.Value = y;
                            h.PaddingBottom.Value = canvasH - y - 1;
                            rowLayouts[y] = h;

                            rgbRaw[y] = new RawGraphic[canvasW];
                            for (int x = 0; x < canvasW; x++)
                            {
                                Slot p = row.AddSlot($"Px{x}");
                                p.AttachComponent<RectTransform>();
                                var g = p.AttachComponent<RawGraphic>();
                                // init to black-ish to avoid bright flash
                                g.Color.Value = new colorX(0, 0, 0, 1, ColorProfile.Linear);
                                rgbRaw[y][x] = g;
                            }
                        }
                        Msg("[Canvas] Built RGB canvas (1 RawGraphic per pixel).");
                    }
                    else
                    {
                        // GB mode: 4 RawGraphics per pixel (precolored), toggle Enabled per color index
                        gbRawPerColor = new RawGraphic[canvasH][];
                        for (int y = 0; y < canvasH; y++)
                        {
                            Slot row = content.AddSlot($"Row{y}");
                            row.AttachComponent<RectTransform>();
                            var h = row.AttachComponent<HorizontalLayout>();
                            h.PaddingTop.Value = y;
                            h.PaddingBottom.Value = canvasH - y - 1;
                            rowLayouts[y] = h;

                            gbRawPerColor[y] = new RawGraphic[canvasW * 4];
                            for (int x = 0; x < canvasW; x++)
                            {
                                Slot p = row.AddSlot($"Px{x}");
                                p.AttachComponent<RectTransform>();

                                // create 4 pre-tinted graphics
                                for (int k = 0; k < 4; k++)
                                {
                                    var g = p.AttachComponent<RawGraphic>();
                                    g.Color.Value = GameBoyColors[k];
                                    g.Enabled = (k == 3); // default to black (index 3)
                                    gbRawPerColor[y][x * 4 + k] = g;
                                }
                            }
                        }
                        Msg("[Canvas] Built GB canvas (4 RawGraphics per pixel).");
                    }
                }
            }

            [HarmonyPatch(typeof(Canvas), "OnDestroy")]
            public static class CanvasOnDestroyPatch
            {
                static void Prefix(Canvas __instance)
                {
                    if (!enabledCached) return;
                    if (__instance.Slot.Name != canvasName) return;

                    Msg("[Canvas] OnDestroy detected; cleaning up resources.");
                    Cleanup();
                }

                private static void Cleanup()
                {
                    try
                    {
                        // Streams/readers
                        _reader?.Dispose(); _reader = null;
                        _mmfView?.Dispose(); _mmfView = null;
                        _mmfPixel?.Dispose(); _mmfPixel = null;
                        _mmfAck?.Dispose(); _mmfAck = null;
                        Msg("[MMF] Disposed all handles.");
                    }
                    catch (Exception ex)
                    {
                        Error($"[Cleanup] {ex}");
                    }
                }
            }

            [HarmonyPatch(typeof(FrooxEngine.Animator), "OnCommonUpdate")]
            public static class AnimatorOnCommonUpdatePatch
            {
                public static void Prefix()
                {
                    if (!initialized() || !enabledCached) return;

                    // Read once per frame
                    if (pxDataLen == -1)
                    {
                        ReadMMF();
                        if (pxDataLen == -1) return; // nothing to do
                    }

                    try
                    {
                        ApplyToCanvas();
                    }
                    catch (Exception ex)
                    {
                        Error($"[Frame] Failed to apply frame: {ex}");
                        setInitialized(false);
                    }
                    finally
                    {
                        pxDataLen = -1; // reset latch
                    }
                }

                // -------------- Small helpers kept inline-friendly --------------
                private static bool initialized() => Patcher.initialized && Patcher._canvas != null;
                private static void setInitialized(bool v) => Patcher.initialized = v;

                private static void ReadMMF()
                {
                    try
                    {
                        if (_mmfPixel == null)
                        {
                            Error("[MMF] Pixel MMF not initialized.");
                            pxDataLen = -1;
                            return;
                        }

                        // Reset to start and read header + payload
                        _mmfView.Seek(0, SeekOrigin.Begin);

                        short status = _reader.ReadInt16();
                        if (status == 0)
                        {
                            // not ready
                            pxDataLen = -1;
                            return;
                        }

                        int tick = _reader.ReadInt32();
                        if (tick == latestFrameTick)
                        {
                            // duplicate
                            pxDataLen = -1;
                            return;
                        }

                        latestFrameTick = tick;

                        rowPairsLen = _reader.ReadInt32();
                        for (int i = 0; i < rowPairsLen; i++)
                            rowPairs[i] = _reader.ReadInt16();

                        pxDataLen = _reader.ReadInt32();
                        for (int i = 0; i < pxDataLen; i++)
                            pxData[i] = _reader.ReadInt32();
                    }
                    catch (Exception ex)
                    {
                        Error($"[MMF] Read error: {ex.Message}");
                        pxDataLen = -1;
                    }
                }

                private static void ApplyToCanvas()
                {
                    mutatingCanvas = true;

                    if (rgbModeCached)
                    {
                        // RGB path: colorKey is 24-bit RGB packed as R*65536 + G*256 + B
                        int i = 0;
                        while (i < pxDataLen)
                        {
                            int colorKey = pxData[i++];

                            // Inline decode colorKey -> r,g,b in 0..255
                            int r = colorKey / (256 * 256);
                            int g = (colorKey / 256) % 256;
                            int b = colorKey % 256;

                            // Build colorX inline; match your previous scaling (/1000f)
                            colorX cx = new colorX(r / 1000f, g / 1000f, b / 1000f, 1, ColorProfile.Linear);

                            // Consume spans until negative delimiter
                            while (i < pxDataLen && pxData[i] >= 0)
                            {
                                int packed = pxData[i++];

                                // Unpack: 1,000,000 * xStart + 1,000 * y + span
                                int xStart = (packed / 1000000) % 1000;
                                int y = (packed / 1000) % 1000;
                                int span = packed % 1000;

                                int xEnd = xStart + span;

                                // Set pixels [xStart, xEnd)
                                RawGraphic[] row = rgbRaw[y];
                                for (int x = xStart; x < xEnd; x++)
                                {
                                    row[x].Color.Value = cx;
                                }
                            }
                            i++; // skip negative delimiter
                        }
                    }
                    else
                    {
                        // GB path: colorKey is 0..3, we toggle the one RawGraphic that matches index
                        int i = 0;
                        while (i < pxDataLen)
                        {
                            int colorIdx = pxData[i++]; // 0..3

                            while (i < pxDataLen && pxData[i] >= 0)
                            {
                                int packed = pxData[i++];

                                int xStart = (packed / 1000000) % 1000;
                                int y = (packed / 1000) % 1000;
                                int span = packed % 1000;

                                int xEnd = xStart + span;

                                RawGraphic[] row = gbRawPerColor[y];
                                for (int x = xStart; x < xEnd; x++)
                                {
                                    // Enable chosen index, disable others (0..3)
                                    int baseIdx = x * 4;
                                    for (int k = 0; k < 4; k++)
                                        row[baseIdx + k].Enabled = (k == colorIdx);
                                }
                            }
                            i++; // skip negative delimiter
                        }
                    }

                    // Apply row height/spacing tweaks (padding hack retained)
                    for (int j = 0; j < rowPairsLen; j += 2)
                    {
                        int rowIndex = rowPairs[j];
                        int rowHeight = rowPairs[j + 1];
                        rowLayouts[rowIndex].PaddingTop.Value = rowIndex - rowHeight;
                    }

                    // Ack to app so it can pace frames if desired
                    EnsureAckMMF();
                    using (var s = _mmfAck.CreateViewStream())
                    using (var w = new BinaryWriter(s))
                    {
                        w.Write(latestFrameTick);
                    }

                    mutatingCanvas = false;
                }
            }

            [HarmonyPatch(typeof(ResoniteModLoader.ModConfiguration), "FireConfigurationChangedEvent")]
            public static class ConfigChangedPatch
            {
                public static void Postfix()
                {
                    UpdateCachedConfigOptions();
                }
            }
        }
    }
}
