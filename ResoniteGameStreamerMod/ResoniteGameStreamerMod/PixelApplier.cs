using Elements.Core;
using FrooxEngine.UIX;
using Renderite.Shared;

namespace ResoniteGameStreamerMod
{
    internal static class PixelApplier
    {
        internal static void ApplyCurrentFrame()
        {
            RuntimeState.MutatingCanvas = true;

            if (StreamerConfig.RgbMode)
                ApplyRGB();
            else
                ApplyGB();

            // Apply row height/spacing tweaks (padding hack retained)
            for (int j = 0; j < RuntimeState.RowPairsLen; j += 2)
            {
                int rowIndex = RuntimeState.RowPairs[j];
                int rowHeight = RuntimeState.RowPairs[j + 1];
                RuntimeState.RowLayouts[rowIndex].PaddingTop.Value = rowIndex - rowHeight;
            }

            MmfIO.AckTick();
            RuntimeState.MutatingCanvas = false;
        }

        private static void ApplyRGB()
        {
            int i = 0;

            // local reference (slightly faster than repeated static access)
            var cache = RuntimeState.RgbColorCache;

            while (i < RuntimeState.PxDataLen)
            {
                int colorKey = RuntimeState.PxData[i++];

                // ----- memoized colorX lookup -----
                // Average-case O(1). First time we see a colorKey we construct once.
                if (!cache.TryGetValue(colorKey, out colorX cx))
                {
                    // Decode packed RGB (R<<16 | G<<8 | B) using bit-ops (faster than / and %).
                    int r = (colorKey >> 16) & 0xFF;
                    int g = (colorKey >> 8) & 0xFF;
                    int b = colorKey & 0xFF;

                    // Keep your existing 0..255 -> 0..0.255 scaling for consistency with the rest of the mod.
                    cx = new colorX(r / 1000f, g / 1000f, b / 1000f, 1, ColorProfile.Linear);
                    cache[colorKey] = cx;
                }


                /*
                Original code before caching of colorXes was:

                int colorKey = RuntimeState.PxData[i++];
                // decode packed RGB: R*65536 + G*256 + B
                int r = colorKey / (256 * 256);
                int g = (colorKey / 256) % 256;
                int b = colorKey % 256;
                var cx = new colorX(r / 1000f, g / 1000f, b / 1000f, 1, ColorProfile.Linear);
                 */

                // ----- apply spans for this color -----
                while (i < RuntimeState.PxDataLen && RuntimeState.PxData[i] >= 0)
                {
                    int packed = RuntimeState.PxData[i++];

                    // packed = xStart*1_000_000 + y*1_000 + span, where each field is 0..999
                    int xStart = (packed / 1_000_000) % 1_000;
                    int y = (packed / 1_000) % 1_000;
                    int span = packed % 1_000;

                    RawGraphic[] row = RuntimeState.RgbRows[y];
                    int xEnd = xStart + span;

                    // Tight inner loop: only pointer chasing + field set, no allocations.
                    for (int x = xStart; x < xEnd; x++)
                        row[x].Color.Value = cx;
                }

                i++; // skip negative delimiter
            }
        }


        private static void ApplyGB()
        {
            int i = 0;
            while (i < RuntimeState.PxDataLen)
            {
                int colorIdx = RuntimeState.PxData[i++]; // 0..3

                while (i < RuntimeState.PxDataLen && RuntimeState.PxData[i] >= 0)
                {
                    int packed = RuntimeState.PxData[i++];

                    int xStart = (packed / 1000000) % 1000;
                    int y = (packed / 1000) % 1000;
                    int span = packed % 1000;

                    int xEnd = xStart + span;
                    RawGraphic[] row = RuntimeState.GbRowsPerColor[y];
                    for (int x = xStart; x < xEnd; x++)
                    {
                        int baseIdx = x * 4;
                        for (int k = 0; k < 4; k++)
                            row[baseIdx + k].Enabled = (k == colorIdx);
                    }
                }
                i++; // skip negative delimiter
            }
        }
    }
}
