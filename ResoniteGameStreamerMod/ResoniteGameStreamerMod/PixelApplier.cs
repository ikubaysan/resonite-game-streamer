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
            while (i < RuntimeState.PxDataLen)
            {
                int colorKey = RuntimeState.PxData[i++];

                // decode packed RGB: R*65536 + G*256 + B
                int r = colorKey / (256 * 256);
                int g = (colorKey / 256) % 256;
                int b = colorKey % 256;
                var cx = new colorX(r / 1000f, g / 1000f, b / 1000f, 1, ColorProfile.Linear);

                while (i < RuntimeState.PxDataLen && RuntimeState.PxData[i] >= 0)
                {
                    int packed = RuntimeState.PxData[i++];

                    int xStart = (packed / 1000000) % 1000;
                    int y = (packed / 1000) % 1000;
                    int span = packed % 1000;

                    int xEnd = xStart + span;
                    RawGraphic[] row = RuntimeState.RgbRows[y];
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
