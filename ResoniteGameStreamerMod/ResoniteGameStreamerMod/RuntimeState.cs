// RuntimeState.cs
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
        internal const string PixelDataMMFName = "ResonitePixelData";
        internal const string ClientAckMMFName = "ResoniteClientRenderConfirmation";
        internal const int ClientAckMMFSize = sizeof(Int32);

        internal static Canvas Canvas;
        internal static HorizontalLayout[] RowLayouts;

        internal static RawGraphic[][] RgbRows;
        internal static RawGraphic[][] GbRowsPerColor;

        internal static int[] PxData;
        internal static int PxDataLen = -1;
        internal static int[] RowPairs;
        internal static int RowPairsLen = 0;

        internal static MemoryMappedFile MmfPixel;
        internal static MemoryMappedViewStream MmfView;
        internal static BinaryReader Reader;
        internal static MemoryMappedFile MmfAck;

        internal static int LatestFrameTick = -1;

        internal static bool Initialized = false;
        internal static bool MutatingCanvas = false;
        internal static DateTime LastInitAttempt = DateTime.MinValue;

        internal static Dictionary<int, colorX> RgbColorCache = new(8192);

        // -------- Progressive application state (persists across updates) --------
        internal static bool FrameInProgress = false;   // true while we are chunking through a frame
        internal static int PxCursor = 0;               // position in PxData

        // RGB sub-state
        internal static bool RgbHasCurrentColor = false;
        internal static colorX RgbCurrentColor;
        internal static bool RgbSpanActive = false;
        internal static int RgbX = 0, RgbY = 0, RgbRemaining = 0;

        // GB sub-state
        internal static bool GbHasCurrentIndex = false; // color index header read
        internal static int GbCurrentIndex = 0;
        internal static bool GbSpanActive = false;
        internal static int GbX = 0, GbY = 0, GbRemaining = 0;

        internal static void BeginFrameChunking()
        {
            FrameInProgress = true;
            PxCursor = 0;
            RgbHasCurrentColor = false;
            RgbSpanActive = false;
            GbHasCurrentIndex = false;
            GbSpanActive = false;
        }

        internal static void EndFrameChunking()
        {
            FrameInProgress = false;
            PxCursor = 0;
            RgbHasCurrentColor = false;
            RgbSpanActive = false;
            GbHasCurrentIndex = false;
            GbSpanActive = false;
        }

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
                EndFrameChunking();
                ResoniteGameStreamerMod.LogMsg("[MMF] Disposed all handles.");
            }
            catch (Exception ex)
            {
                ResoniteGameStreamerMod.LogError($"[Cleanup] {ex}");
            }
        }
    }
}
