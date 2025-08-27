// CanvasBuilder.cs
using Elements.Core; 
using FrooxEngine;
using FrooxEngine.UIX;
using Renderite.Shared;
using System;
using System.IO.MemoryMappedFiles;

namespace ResoniteGameStreamerMod
{
    internal static class CanvasBuilder
    {
        internal static void InitializeCanvas(Canvas cv)
        {
            cv.Size.Value = new float2(StreamerConfig.CanvasW, StreamerConfig.CanvasH);
            ResoniteGameStreamerMod.LogMsg($"[Canvas] Set size to {cv.Size.Value}");

            Slot background = cv.Slot.FindChild("Background") ?? throw new Exception("Missing child slot 'Background'");
            Slot image = background.FindChild("Image") ?? throw new Exception("Missing child slot 'Image'");
            Slot content = image.FindChild("Content") ?? throw new Exception("Missing child slot 'Content'");

            RuntimeState.PxData = new int[StreamerConfig.CanvasW * StreamerConfig.CanvasH];
            RuntimeState.RowPairs = new int[StreamerConfig.CanvasW * StreamerConfig.CanvasH];

            // Open pixel MMF
            RuntimeState.MmfPixel = MemoryMappedFile.OpenExisting(RuntimeState.PixelDataMMFName);
            RuntimeState.MmfView = RuntimeState.MmfPixel.CreateViewStream();
            RuntimeState.Reader = new System.IO.BinaryReader(RuntimeState.MmfView);
            RuntimeState.LatestFrameTick = -1;
            ResoniteGameStreamerMod.LogMsg($"[MMF] Opened '{RuntimeState.PixelDataMMFName}' for reading.");

            content.DestroyChildren();
            ResoniteGameStreamerMod.LogMsg("[Canvas] Cleared Content children.");

            int w = StreamerConfig.CanvasW, h = StreamerConfig.CanvasH;
            RuntimeState.RowLayouts = new HorizontalLayout[h];

            if (StreamerConfig.RgbMode)
            {
                RuntimeState.RgbRows = new RawGraphic[h][];
                for (int y = 0; y < h; y++)
                {
                    Slot row = content.AddSlot($"Row{y}");
                    row.AttachComponent<RectTransform>();
                    var layout = row.AttachComponent<HorizontalLayout>();
                    layout.PaddingTop.Value = y;
                    layout.PaddingBottom.Value = h - y - 1;
                    RuntimeState.RowLayouts[y] = layout;

                    var arr = new RawGraphic[w];
                    for (int x = 0; x < w; x++)
                    {
                        Slot p = row.AddSlot($"Px{x}");
                        p.AttachComponent<RectTransform>();
                        var g = p.AttachComponent<RawGraphic>();
                        g.Color.Value = new colorX(0, 0, 0, 1, ColorProfile.Linear);
                        arr[x] = g;
                    }
                    RuntimeState.RgbRows[y] = arr;
                }
                ResoniteGameStreamerMod.LogMsg("[Canvas] Built RGB canvas (1 RawGraphic per pixel).");
            }
            else
            {
                RuntimeState.GbRowsPerColor = new RawGraphic[h][];
                for (int y = 0; y < h; y++)
                {
                    Slot row = content.AddSlot($"Row{y}");
                    row.AttachComponent<RectTransform>();
                    var layout = row.AttachComponent<HorizontalLayout>();
                    layout.PaddingTop.Value = y;
                    layout.PaddingBottom.Value = h - y - 1;
                    RuntimeState.RowLayouts[y] = layout;

                    var arr = new RawGraphic[w * 4];
                    for (int x = 0; x < w; x++)
                    {
                        Slot p = row.AddSlot($"Px{x}");
                        p.AttachComponent<RectTransform>();
                        for (int k = 0; k < 4; k++)
                        {
                            var g = p.AttachComponent<RawGraphic>();
                            g.Color.Value = StreamerConfig.GB[k];
                            g.Enabled = (k == 3);
                            arr[x * 4 + k] = g;
                        }
                    }
                    RuntimeState.GbRowsPerColor[y] = arr;
                }
                ResoniteGameStreamerMod.LogMsg("[Canvas] Built GB canvas (4 RawGraphics per pixel).");
            }

            RuntimeState.Canvas = cv;
        }
    }
}
