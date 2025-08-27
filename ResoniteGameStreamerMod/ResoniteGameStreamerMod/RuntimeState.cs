using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ResoniteGameStreamerMod
{
    internal static class RuntimeState
    {
        // MMF names & sizes
        internal const string PixelDataMMFName = "ResonitePixelData";
        internal const string ClientAckMMFName = "ResoniteClientRenderConfirmation";
        internal const int ClientAckMMFSize = sizeof(Int32);

        // Canvas & construction artifacts
        internal static Canvas Canvas;
        internal static HorizontalLayout[] RowLayouts;

        // RGB path
        internal static RawGraphic[][] RgbRows;

        // GB path (4 graphics per pixel)
        internal static RawGraphic[][] GbRowsPerColor;

        // Frame buffers
        internal static int[] PxData;
        internal static int PxDataLen = -1;
        internal static int[] RowPairs;
        internal static int RowPairsLen = 0;

        // MMF handles
        internal static MemoryMappedFile MmfPixel;
        internal static MemoryMappedViewStream MmfView;
        internal static BinaryReader Reader;
        internal static MemoryMappedFile MmfAck;

        // Frame tracking
        internal static int LatestFrameTick = -1;

        // Control flags / guards
        internal static bool Initialized = false;
        internal static bool MutatingCanvas = false;
        internal static DateTime LastInitAttempt = DateTime.MinValue;

        internal static Dictionary<int, colorX> RgbColorCache = new(8192);

        internal static void ResetFrameLatch() => PxDataLen = -1;

        internal static void DisposeAll()
        {
            try
            {
                Reader?.Dispose(); Reader = null;
                MmfView?.Dispose(); MmfView = null;
                MmfPixel?.Dispose(); MmfPixel = null;
                MmfAck?.Dispose(); MmfAck = null;
                RgbColorCache.Clear();
                ResoniteGameStreamerMod.Msg("[MMF] Disposed all handles.");
            }
            catch (Exception ex)
            {
                ResoniteGameStreamerMod.Error($"[Cleanup] {ex}");
            }
        }
    }
}
