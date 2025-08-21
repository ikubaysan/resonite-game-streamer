using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ResoniteModLoader;
using System.Reflection;
using FrooxEngine;
using FrooxEngine.UIX;
using Elements.Core;
using System.IO.MemoryMappedFiles;
using System.IO;
using FrooxEngine.ProtoFlux.CoreNodes;
using Renderite.Shared;

namespace ResoniteGBMod
{
    public class ResoniteGBMod : ResoniteMod
    {
        public override string Author => "Ikubaysan";
        public override string Name => "ResoniteGBMod";
        public override string Version => "1.0.0";


        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabled", "Enable mod", () => true);
        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<int> CANVAS_SLOT_WIDTH = new ModConfigurationKey<int>("canvas_slot_width", "Pixel width of the canvas slot", () => 160);
        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<int> CANVAS_SLOT_HEIGHT = new ModConfigurationKey<int>("canvas_slot_height", "Pixel height of the canvas slot", () => 144);
        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> CANVAS_SLOT_NAME = new ModConfigurationKey<string>("canvas_slot_name", "Name of the canvas slot", () => "GBUIXCanvas");

        private static ModConfiguration Config; //If you use config settings, this will be where you interface with them

        private static bool enabledCachedConfigOption;
        private static int canvasSlotWidthCachedConfigOption;
        private static int canvasSlotHeightCachedConfigOption;
        private static string canvasSlotNameCachedConfigOption;

        private static colorX[] GameBoyColors = new colorX[4];

        // Fixed size array for all possible RGB values
        private static bool _allColorsInitialized = false;
        private static bool _reInitializeNeeded = false;

        public override void OnEngineInit()
        {
            Config = GetConfiguration(); //Get this mods' current ModConfiguration
            UpdateCachedConfigOptions();
            Config.Save(true); //If you'd like to save the default config values to file
            Harmony harmony = new Harmony("com.ikubaysan.ResoniteGBMod");
            harmony.PatchAll();

            Debug("a debug log from ResoniteGBMod...");
            Msg("a regular log from ResoniteGBMod...");
            Warn("a warn log from ResoniteGBMod...");
            Error("an error log from ResoniteGBMod...");

            if (enabledCachedConfigOption)
            {
                initializeAllColors();
            }
        }

        private static void initializeAllColors()
        {
            Msg("Initializing all colors");

            /*
            private static readonly Color[] GameBoyColors = new Color[] {
                Color.FromArgb(255, 255, 255), // White
                Color.FromArgb(192, 192, 192), // Light Gray
                Color.FromArgb(96, 96, 96),    // Dark Gray
                Color.FromArgb(0, 0, 0)        // Black
            };
            */

            GameBoyColors[0] = new colorX(255 / 1000f, 255 / 1000f, 255 / 1000f, 1, ColorProfile.Linear); // White
            GameBoyColors[1] = new colorX(192 / 1000f, 192 / 1000f, 192 / 1000f, 1, ColorProfile.Linear); // Light Gray
            GameBoyColors[2] = new colorX(96 / 1000f, 96 / 1000f, 96 / 1000f, 1, ColorProfile.Linear); // Dark Gray
            GameBoyColors[3] = new colorX(0 / 1000f, 0 / 1000f, 0 / 1000f, 1, ColorProfile.Linear); // Black

            _allColorsInitialized = true;
            Msg("Finished initializing all colors");
        }

        private static void UpdateCachedConfigOptions()
        {

            int newCanvasSlotWidth = Config.GetValue(CANVAS_SLOT_WIDTH);
            int newCanvasSlotHeight = Config.GetValue(CANVAS_SLOT_HEIGHT);
            bool dimensionsAreValid = newCanvasSlotWidth >= 100 && newCanvasSlotHeight >= 100 && newCanvasSlotWidth <= 999 && newCanvasSlotHeight <= 999;

            if (dimensionsAreValid && newCanvasSlotWidth != canvasSlotWidthCachedConfigOption || newCanvasSlotHeight != canvasSlotHeightCachedConfigOption)
            {
                Msg("Canvas dimensions have been modified and are valid, re-initialization of canvas is needed");
                _reInitializeNeeded = true;
            }
            else
            {
                Msg("Canvas dimensions have not been modified or are invalid, re-initialization of canvas is not needed");
                _reInitializeNeeded = false;
            }

            enabledCachedConfigOption = Config.GetValue(ENABLED);
            if (dimensionsAreValid)
            {
                canvasSlotWidthCachedConfigOption = Config.GetValue(CANVAS_SLOT_WIDTH);
                canvasSlotHeightCachedConfigOption = Config.GetValue(CANVAS_SLOT_HEIGHT);
            }
            else
            {
                Msg("Canvas dimensions are invalid, not updating canvasSlotWidthCachedConfigOption and canvasSlotHeightCachedConfigOption");
            }

            canvasSlotNameCachedConfigOption = Config.GetValue(CANVAS_SLOT_NAME);
            Msg("Updated cached config options");
            Msg("enabledCachedConfigOption: " + enabledCachedConfigOption);
            Msg("canvasSlotWidthCachedConfigOption: " + canvasSlotWidthCachedConfigOption);
            Msg("canvasSlotHeightCachedConfigOption: " + canvasSlotHeightCachedConfigOption);
            Msg("canvasSlotNameCachedConfigOption: " + canvasSlotNameCachedConfigOption);
        }

        class ReosoniteGBModPatcher
        {
            private static bool initialized = false;
            private static Canvas _latestCanvasInstance;
            private static MemoryMappedFile _memoryMappedFile;
            private const string MemoryMappedFileName = "ResonitePixelData";
            private static RawGraphic[][] rawGraphicComponentCache;
            private static HorizontalLayout[] horizontalLayoutComponentCache;
            private static int[] readPixelData;
            private static int readPixelDataLength = -1;

            private const string ClientRenderConfirmationMemoryMappedFileName = "ResoniteClientRenderConfirmation";
            private const int ClientRenderConfirmationMemoryMappedFileSize = sizeof(Int32);
            private static MemoryMappedFile _clientRenderConfirmationMemoryMappedFile;
            private static int latestReceivedFrameMillisecondsOffset = -1;
            private static DateTime latestInitializationAttempt = DateTime.MinValue;
            private static bool canvasModificationInProgress = false;

            public static int[] readContiguousRangePairs;
            private static int readContiguousRangePairsLength;

            private static MemoryMappedViewStream _memoryMappedViewStream;
            private static BinaryReader _binaryReader;


            // OnAttach() is called whenever a new Canvas is created, but not when I spawn one from Inventory.
            // FinishCanvasUpdate() Also hits when I grab the canvas, but it's the only method I could find to detect spawning of a canvas.
            [HarmonyPatch(typeof(Canvas), "FinishCanvasUpdate")]
            public static class CanvasFinishCanvasUpdatePatcher
            {
                static void Postfix(Canvas __instance)
                {
                    if (!enabledCachedConfigOption) return;

                    if (__instance.Slot.Name != canvasSlotNameCachedConfigOption) return;

                    if (__instance != _latestCanvasInstance)
                    {
                        Msg("Found a Canvas with slot name " + __instance.Slot.Name + ", and it is not the same as _latestCanvasInstance");
                        _latestCanvasInstance = __instance;
                        initialized = false;
                        Msg("Set initialized to false");
                    }

                    if (!initialized || _reInitializeNeeded)
                    {
                        if ((DateTime.UtcNow - latestInitializationAttempt).TotalSeconds < 30) return;
                        Msg("Canvas must be initialized");

                        try
                        {
                            InitializeCanvas(__instance);
                        }
                        catch (Exception e)
                        {
                            Error("Failed to initialize canvas " + __instance.Slot.Name);
                            Error(e.ToString());
                            return;
                        }
                        initialized = true;
                        _reInitializeNeeded = false;
                    }
                }
                static void InitializeCanvas(Canvas __instance)
                {
                    // Retrieve the values of configuration keys at the time the method is called
                    int canvasSlotWidth = canvasSlotWidthCachedConfigOption;
                    int canvasSlotHeight = canvasSlotHeightCachedConfigOption;
                    string canvasSlotName = canvasSlotNameCachedConfigOption;

                    // Slot name matches the constant
                    Msg("Matched with the slot name: " + __instance.Slot.Name);

                    __instance.Slot.GetComponent<Canvas>().Size.Value = new float2(canvasSlotWidth, canvasSlotHeight);
                    Msg("Set the size of the canvas to: " + __instance.Slot.GetComponent<Canvas>().Size.Value);

                    Msg("Changed the slot name to: " + __instance.Slot.Name);

                    Slot backgroundSlot = __instance.Slot.FindChild("Background");
                    if (backgroundSlot == null)
                    {
                        Msg("Could not find the child slot: Background");
                        return;
                    }

                    Msg("Found the child slot: " + backgroundSlot.Name);

                    Slot imageSlot = backgroundSlot.FindChild("Image");
                    if (imageSlot == null)
                    {
                        Msg("Could not find the child slot: Image");
                        return;
                    }

                    Msg("Found the child slot: " + imageSlot.Name);

                    Slot contentSlot = imageSlot.FindChild("Content");
                    if (contentSlot == null)
                    {
                        Msg("Could not find the child slot: Content");
                        return;
                    }

                    Msg("Found the child slot: " + contentSlot.Name);

                    // Destroying all the children is expensive, and usually if we get to this point then the next thing that could go wrong
                    // is not being able to find the memory mapped file, so we'll attempt to find the memory mapped file first.
                    readPixelData = new int[canvasSlotWidthCachedConfigOption * canvasSlotHeightCachedConfigOption];
                    readContiguousRangePairs = new int[canvasSlotWidthCachedConfigOption * canvasSlotHeightCachedConfigOption];

                    _memoryMappedFile = MemoryMappedFile.OpenExisting(MemoryMappedFileName);
                    _memoryMappedViewStream = _memoryMappedFile.CreateViewStream();
                    _binaryReader = new BinaryReader(_memoryMappedViewStream);
                    latestReceivedFrameMillisecondsOffset = -1;
                    Msg("_memoryMappedFile has been newly initialized with " + MemoryMappedFileName);

                    // Delete all existing children of the content slot, which are HorizontalLayouts
                    contentSlot.DestroyChildren();

                    Msg("Destroyed all children of the content slot: " + contentSlot.Name);

                    // Create new HorizontalLayouts according to the height constant
                    Random rand = new Random();

                    // Initialize the cache
                    rawGraphicComponentCache = new RawGraphic[canvasSlotHeight][];
                    horizontalLayoutComponentCache = new HorizontalLayout[canvasSlotHeight];

                    // For the count of the height constant, call contentSlot.AddSlot
                    for (int i = 0; i < canvasSlotHeight; i++)
                    {
                        Slot horizontalLayoutSlot = contentSlot.AddSlot("HorizontalLayout" + i);
                        horizontalLayoutSlot.AttachComponent<RectTransform>();
                        HorizontalLayout horizontalLayoutComponent = horizontalLayoutSlot.AttachComponent<HorizontalLayout>();
                        horizontalLayoutComponentCache[i] = horizontalLayoutComponent;
                        horizontalLayoutComponent.PaddingTop.Value = i;
                        horizontalLayoutComponent.PaddingBottom.Value = canvasSlotHeight - i - 1;

                        // Create new slots for each column in the horizontal layout and add them to the cache
                        rawGraphicComponentCache[i] = new RawGraphic[canvasSlotWidth * GameBoyColors.Length];

                        // Add a slot for each column in the horizontal layout (each pixel)
                        for (int j = 0; j < canvasSlotWidth; j++)
                        {
                            Slot verticalSlot = horizontalLayoutSlot.AddSlot("VerticalSlot" + j);
                            verticalSlot.AttachComponent<RectTransform>();


                            // Create a Raw Graphic component for each color

                            for (int k = 0; k < GameBoyColors.Length; k++)
                            {
                                RawGraphic rawGraphicComponent = verticalSlot.AttachComponent<RawGraphic>();
                                rawGraphicComponentCache[i][j * GameBoyColors.Length + k] = rawGraphicComponent;
                                rawGraphicComponent.Color.Value = GameBoyColors[k];

                                // Enable the 4th color (black) by default
                                if (k == GameBoyColors.Length - 1)
                                    rawGraphicComponent.Enabled = true;
                                else 
                                    rawGraphicComponent.Enabled = false;
                            }
                        }
                    }
                    Msg("Created new HorizontalLayouts according to the height constant: " + canvasSlotHeight);

                    if (_allColorsInitialized)
                    {
                        Msg("All colors are already initialized");
                    }
                    else
                    {
                        Msg("All colors have not been initialized yet, so we must do so.");
                        initializeAllColors();
                    }

                }
            }


            [HarmonyPatch(typeof(Canvas), "OnDestroy")]
            public static class CanvasOnDestroyPatcher
            {
                static void Prefix(Canvas __instance)
                {
                    if (!enabledCachedConfigOption) return;

                    if (__instance.Slot.Name != canvasSlotNameCachedConfigOption) return;
                    Msg("OnDestroy() prefix patch called for canvas " + __instance.Slot.Name);

                    // Wait for canvasModificationInProgress to be false
                    if (canvasModificationInProgress)
                    {
                        Msg("canvasModificationInProgress is true, waiting for it to be false");
                        while (canvasModificationInProgress)
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                        Msg("canvasModificationInProgress is now false, continuing");
                    }
                    else
                    {
                        Msg("canvasModificationInProgress is false, no need to wait");
                    }
                    _latestCanvasInstance = null;
                    Msg("Set _latestCanvasInstance to null");
                    CleanupResources();
                    Msg("Called CleanupResources()");
                }

                static void CleanupResources()
                {
                    _binaryReader?.Dispose();
                    _binaryReader = null;
                    _memoryMappedViewStream?.Dispose();
                    _memoryMappedViewStream = null;
                    _memoryMappedFile?.Dispose();
                    _memoryMappedFile = null;
                }

            }



            [HarmonyPatch(typeof(FrooxEngine.Animator), "OnCommonUpdate")]
            public static class AnimatorOnCommonUpdatePatcher
            {
                public static void Prefix()
                {
                    if (!initialized || _latestCanvasInstance == null || !enabledCachedConfigOption) return;

                    if (readPixelDataLength == -1 && enabledCachedConfigOption)
                    {
                        ReadFromMemoryMappedFile();
                        // This can happen if ReadFromMemoryMappedFile() raised an exception
                        if (readPixelDataLength == -1) return;
                    }
                    try
                    {
                        SetPixelDataToCanvas(_latestCanvasInstance);
                    }
                    catch (Exception e)
                    {
                        // This will also hit if the canvas was deleted.
                        Error("Failed to update frame for initialized canvas " + _latestCanvasInstance.Slot.Name);
                        Error(e.ToString());
                        initialized = false;
                        Error("Set initialized to false.");
                    }
                    readPixelDataLength = -1;
                    return;
                }

                static void SetPixelDataToCanvas(Canvas __instance)
                {
                    int i = 0;
                    int colorIndex;
                    int packedxStartYSpan, xStart, y, spanLength;
                    int x, xEnd;
                    canvasModificationInProgress = true;

                    while (i < readPixelDataLength)
                    {
                        colorIndex = readPixelData[i++];
                        while (i < readPixelDataLength && readPixelData[i] >= 0)
                        {
                            packedxStartYSpan = readPixelData[i++];
                            xStart = (packedxStartYSpan / 1000000) % 1000;
                            y = (packedxStartYSpan / 1000) % 1000;

                            //Same as: xEnd = xStart + spanLength;
                            xEnd = ((packedxStartYSpan / 1000000) % 1000) + (packedxStartYSpan % 1000);

                            for (x = xStart; x < xEnd; x++)
                            {

                                for (int k = 0; k < GameBoyColors.Length; k++)
                                {
                                    // Set the color of the pixel by enabling the correct RawGraphic component
                                    if (k == colorIndex)
                                    {
                                        rawGraphicComponentCache[y][x * GameBoyColors.Length + k].Enabled = true;
                                    }
                                    else
                                    {
                                        rawGraphicComponentCache[y][x * GameBoyColors.Length + k].Enabled = false;
                                    }
                                }
                            }
                        }
                        i++; // Skip the negative delimiter. We've hit a new color.
                    }

                    for (i = 0; i < readContiguousRangePairsLength; i += 2)
                    {
                        int rowIndex = readContiguousRangePairs[i];
                        int rowHeight = readContiguousRangePairs[i + 1];
                        //SetRowHeight(rowIndex, rowHeight);
                        horizontalLayoutComponentCache[rowIndex].PaddingTop.Value = rowIndex - rowHeight;
                    }

                    WriteLatestReceivedFrameMillisecondsOffsetToMemoryMappedFile();
                    canvasModificationInProgress = false;
                }

                static void WriteLatestReceivedFrameMillisecondsOffsetToMemoryMappedFile()
                {
                    if (_clientRenderConfirmationMemoryMappedFile == null)
                    {
                        _clientRenderConfirmationMemoryMappedFile = MemoryMappedFile.CreateOrOpen(ClientRenderConfirmationMemoryMappedFileName, ClientRenderConfirmationMemoryMappedFileSize);
                    }
                    using (MemoryMappedViewStream stream = _clientRenderConfirmationMemoryMappedFile.CreateViewStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(latestReceivedFrameMillisecondsOffset);
                    }
                }

                static void ReadFromMemoryMappedFile()
                {
                    try
                    {
                        if (_memoryMappedFile == null)
                        {
                            Error("MemoryMappedFile not initialized");
                            readPixelDataLength = -1;
                            return;
                        }

                        // Reset the stream position to the beginning
                        _memoryMappedViewStream.Seek(0, SeekOrigin.Begin);

                        short status = _binaryReader.ReadInt16();
                        if (status == 0)
                        {
                            ////Msg("Data not ready yet");
                            readPixelDataLength = -1;
                            return;
                        }

                        int millisecondsOffset = _binaryReader.ReadInt32();
                        if (millisecondsOffset == latestReceivedFrameMillisecondsOffset)
                        {
                            //Msg("millisecondsOffset of " + millisecondsOffset + " is the same as latestReceivedFrameMillisecondsOffset of " + latestReceivedFrameMillisecondsOffset);
                            readPixelDataLength = -1;
                            return;
                        }

                        latestReceivedFrameMillisecondsOffset = millisecondsOffset;

                        // Read the count of contiguousRangePairs
                        readContiguousRangePairsLength = _binaryReader.ReadInt32();

                        // Now read the contiguousRangePairs, based on contiguousRangePairsCount
                        for (int i = 0; i < readContiguousRangePairsLength; i++)
                        {
                            readContiguousRangePairs[i] = _binaryReader.ReadInt16();
                        }

                        readPixelDataLength = _binaryReader.ReadInt32();

                        // Now read the pixel data, based on readPixelDataLength
                        for (int i = 0; i < readPixelDataLength; i++)
                        {
                            readPixelData[i] = _binaryReader.ReadInt32();
                        }
                    }
                    catch (Exception ex)
                    {
                        Error("Error reading from MemoryMappedFile: " + ex.Message);
                        Msg($"readPixelDataLength: {readPixelDataLength}");
                        Msg($"Length of readPixelData array: {readPixelData.Length}");
                        readPixelDataLength = -1;
                    }
                }
            }

            [HarmonyPatch(typeof(ResoniteModLoader.ModConfiguration), "FireConfigurationChangedEvent")]
            public static class FireConfigurationChangedEventPatcher
            {
                public static void Postfix()
                {
                    UpdateCachedConfigOptions();
                }
            }
        }
    }
}