// PixelApplier.cs
using Elements.Core;
using FrooxEngine.UIX;
using Renderite.Shared;

namespace ResoniteGameStreamerMod
{
    internal static class PixelApplier
    {
        /// <summary>
        /// Apply up to StreamerConfig.MaxPixelsPerUpdate pixels of the current frame.
        /// Returns true if the frame is fully applied this call; false if more remains.
        /// </summary>
        internal static bool ApplyFrameChunk()
        {
            RuntimeState.MutatingCanvas = true;

            int budget = StreamerConfig.MaxPixelsPerUpdate;
            int applied = 0;

            if (StreamerConfig.RgbMode)
                applied = ApplyRGBChunk(budget);
            else
                applied = ApplyGreyscaleChunk(budget);

            bool finished = RuntimeState.PxCursor >= RuntimeState.PxDataLen;

            // Only when the WHOLE frame is done: apply row tweaks + ack
            if (finished)
            {
                // Apply row height/spacing tweaks once per completed frame
                for (int j = 0; j < RuntimeState.RowPairsLen; j += 2)
                {
                    int rowIndex = RuntimeState.RowPairs[j];
                    int rowHeight = RuntimeState.RowPairs[j + 1];
                    RuntimeState.RowLayouts[rowIndex].PaddingTop.Value = rowIndex - rowHeight;
                }

                MmfIO.AckTick();
                RuntimeState.MutatingCanvas = false;
                RuntimeState.EndFrameChunking();
                return true;
            }

            RuntimeState.MutatingCanvas = false;
            return false;
        }

        private static int ApplyRGBChunk(int budget)
        {
            int used = 0;
            var data = RuntimeState.PxData;
            int len = RuntimeState.PxDataLen;
            int i = RuntimeState.PxCursor;
            var cache = RuntimeState.RgbColorCache;

            while (i < len && used < budget)
            {
                // 1) Ensure we have a current color
                if (!RuntimeState.RgbHasCurrentColor)
                {
                    int colorKey = data[i++];

                    if (!cache.TryGetValue(colorKey, out colorX cx))
                    {
                        int r = (colorKey >> 16) & 0xFF;
                        int g = (colorKey >> 8) & 0xFF;
                        int b = colorKey & 0xFF;
                        cx = new colorX(r / 1000f, g / 1000f, b / 1000f, 1, ColorProfile.Linear);
                        cache[colorKey] = cx;
                    }

                    RuntimeState.RgbCurrentColor = cx;
                    RuntimeState.RgbHasCurrentColor = true;
                    RuntimeState.RgbSpanActive = false; // need to pull first span next
                }

                // 2) If no active span, fetch next (or consume delimiter)
                if (!RuntimeState.RgbSpanActive)
                {
                    if (i >= len) break;

                    int next = data[i];
                    if (next < 0)
                    {
                        // end-of-color delimiter
                        i++; // consume delimiter
                        RuntimeState.RgbHasCurrentColor = false;
                        continue; // will read next colorKey
                    }

                    // parse packed span
                    int packed = data[i++];
                    int xStart = (packed / 1_000_000) % 1_000;
                    int y = (packed / 1_000) % 1_000;
                    int span = packed % 1_000;

                    RuntimeState.RgbX = xStart;
                    RuntimeState.RgbY = y;
                    RuntimeState.RgbRemaining = span;
                    RuntimeState.RgbSpanActive = true;
                }

                // 3) Apply pixels of the current active span, up to budget
                {
                    RawGraphic[] row = RuntimeState.RgbRows[RuntimeState.RgbY];
                    int canDo = System.Math.Min(RuntimeState.RgbRemaining, budget - used);
                    int x = RuntimeState.RgbX;
                    int xEnd = x + canDo;
                    var color = RuntimeState.RgbCurrentColor;

                    for (; x < xEnd; x++)
                        row[x].Color.Value = color;

                    used += canDo;
                    RuntimeState.RgbX += canDo;
                    RuntimeState.RgbRemaining -= canDo;

                    if (RuntimeState.RgbRemaining == 0)
                    {
                        RuntimeState.RgbSpanActive = false; // fetch next span or delimiter
                    }
                }
            }

            RuntimeState.PxCursor = i;
            return used;
        }

        private static int ApplyGreyscaleChunk(int budget)
        {
            int used = 0;
            var data = RuntimeState.PxData;
            int len = RuntimeState.PxDataLen;
            int i = RuntimeState.PxCursor;

            while (i < len && used < budget)
            {
                // 1) Ensure we have current GB shade index
                if (!RuntimeState.GbHasCurrentIndex)
                {
                    if (i >= len) break;
                    RuntimeState.GbCurrentIndex = data[i++]; // 0..3
                    RuntimeState.GbHasCurrentIndex = true;
                    RuntimeState.GbSpanActive = false;
                }

                // 2) If no active span, fetch next (or delimiter)
                if (!RuntimeState.GbSpanActive)
                {
                    if (i >= len) break;

                    int next = data[i];
                    if (next < 0)
                    {
                        i++; // consume delimiter
                        RuntimeState.GbHasCurrentIndex = false;
                        continue; // next shade index
                    }

                    int packed = data[i++];
                    int xStart = (packed / 1_000_000) % 1_000;
                    int y = (packed / 1_000) % 1_000;
                    int span = packed % 1_000;

                    RuntimeState.GbX = xStart;
                    RuntimeState.GbY = y;
                    RuntimeState.GbRemaining = span;
                    RuntimeState.GbSpanActive = true;
                }

                // 3) Apply pixels of the current GB span
                {
                    RawGraphic[] row = RuntimeState.GbRowsPerColor[RuntimeState.GbY];
                    int idx = RuntimeState.GbCurrentIndex;

                    int canDo = System.Math.Min(RuntimeState.GbRemaining, budget - used);
                    int x = RuntimeState.GbX;
                    int xEnd = x + canDo;

                    for (; x < xEnd; x++)
                    {
                        int baseIdx = x * 4;
                        // enable only the chosen shade index
                        row[baseIdx + 0].Enabled = (0 == idx);
                        row[baseIdx + 1].Enabled = (1 == idx);
                        row[baseIdx + 2].Enabled = (2 == idx);
                        row[baseIdx + 3].Enabled = (3 == idx);
                    }

                    used += canDo;
                    RuntimeState.GbX += canDo;
                    RuntimeState.GbRemaining -= canDo;

                    if (RuntimeState.GbRemaining == 0)
                    {
                        RuntimeState.GbSpanActive = false;
                    }
                }
            }

            RuntimeState.PxCursor = i;
            return used;
        }
    }
}
