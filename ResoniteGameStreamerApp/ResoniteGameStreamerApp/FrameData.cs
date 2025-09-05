using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResoniteGameStreamerApp
{
    public enum ColorMode { GREYSCALE, RGB }

    public class FrameData
    {
        public static Bitmap _cachedBitmap;
        public static Bitmap _simulatedCanvas;
        public static int[] rowContiguousSpanEndIndices;

        private static Dictionary<int, List<int>> rgbToSpans; // Map color key -> spans
        private static IntPtr cachedWindowHandle = IntPtr.Zero;
        private static string cachedWindowTitle = "";

        private static Dictionary<int, int> rowExpansionAmounts;
        private static List<int> contiguousRangePairs;
        private static Dictionary<int, List<Color>> cachedRowPixels;

        public static ColorMode ActiveColorMode = ColorMode.GREYSCALE; // default

        static FrameData()
        {
            Initialize();
        }

        public static void Initialize()
        {
            rowExpansionAmounts = null;
            contiguousRangePairs = new List<int>();
            cachedRowPixels = new Dictionary<int, List<Color>>();
        }

        // ---------------- GB palette ----------------
        private static readonly Color[] GameBoyColors = new Color[] {
            Color.FromArgb(255, 255, 255), // White
            Color.FromArgb(192, 192, 192), // Light Gray
            Color.FromArgb(96, 96, 96),    // Dark Gray
            Color.FromArgb(0, 0, 0)        // Black
        };

        private static int GetGameBoyColorIndex(Color originalColor)
        {
            int brightness = (int)(0.299 * originalColor.R + 0.587 * originalColor.G + 0.114 * originalColor.B);
            if (brightness > 192) return 0;
            if (brightness >= 128) return 1;
            if (brightness >= 64) return 2;
            return 3;
        }

        // ---------------- RGB helpers (NES-style) ----------------
        public static Int32 GetIndexFromColor(Color color)
            => color.R * 256 * 256 + color.G * 256 + color.B;

        public static Color GetColorFromIndex(int index)
        {
            int r = index / (256 * 256);
            int g = (index / 256) % 256;
            int b = index % 256;
            return Color.FromArgb(r, g, b);
        }

        private static Color GetColorFromOffset(byte[] bytes, int offset)
            => Color.FromArgb(bytes[offset + 2], bytes[offset + 1], bytes[offset]);

        // Packed x/y/span
        static int PackXYZ(int x, int y, int z) => 1000000000 + x * 1000000 + y * 1000 + z;

        static void UnpackXYZ(Int32 packedXYZ, out int X, out int Y, out int Z)
        {
            X = (packedXYZ / 1000000) % 1000;
            Y = (packedXYZ / 1000) % 1000;
            Z = packedXYZ % 1000;
        }

        private static int IdentifySpanRgb(byte[] bmpBytes, int x, int y, int stride, int width, int bytesPerPixel, Color targetColor)
        {
            int offset = y * stride + x * bytesPerPixel;
            while (x < width &&
                   bmpBytes[offset + 2] == targetColor.R &&
                   bmpBytes[offset + 1] == targetColor.G &&
                   bmpBytes[offset] == targetColor.B)
            {
                x++;
                offset = y * stride + x * bytesPerPixel;
            }
            return x;
        }

        private static void StoreSpanRgb(Dictionary<int, List<int>> dict, Color pixel, int packedXYZ)
        {
            int key = GetIndexFromColor(pixel);
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<int>();
                dict[key] = list;
            }
            list.Add(packedXYZ);
        }

        private static int IdentifySpanGb(byte[] bmpBytes, int x, int y, int stride, int width, int bytesPerPixel, int targetColorIndex)
        {
            int offset = y * stride + x * bytesPerPixel;
            while (x < width)
            {
                Color currentColor = Color.FromArgb(bmpBytes[offset + 2], bmpBytes[offset + 1], bmpBytes[offset]);
                if (GetGameBoyColorIndex(currentColor) != targetColorIndex) break;
                x++;
                offset += bytesPerPixel;
            }
            return x;
        }

        private static void StoreSpanGb(Dictionary<int, List<int>> dict, int colorIndex, int packedXYZ)
        {
            if (!dict.TryGetValue(colorIndex, out var list))
            {
                list = new List<int>();
                dict[colorIndex] = list;
            }
            list.Add(packedXYZ);
        }

        private static Bitmap CaptureWindow(string targetWindowTitle, int borderWidth, int titleBarHeight, double brightnessFactor, double darkenFactor)
        {
            IntPtr hWnd = IntPtr.Zero;
            NativeMethods.RECT rect = new NativeMethods.RECT { Top = 0, Left = 0, Right = 0, Bottom = 0 };
            bool cachedRectSet = false;

            if (cachedWindowTitle == targetWindowTitle)
            {
                hWnd = cachedWindowHandle;
                if (NativeMethods.GetWindowRect(hWnd, out rect)) cachedRectSet = true;
                else { cachedWindowHandle = IntPtr.Zero; cachedWindowTitle = ""; hWnd = NativeMethods.FindWindowByTitleSubstring(targetWindowTitle); }
            }
            else
            {
                hWnd = NativeMethods.FindWindowByTitleSubstring(targetWindowTitle);
            }

            if (hWnd != IntPtr.Zero) { cachedWindowHandle = hWnd; cachedWindowTitle = targetWindowTitle; }
            else { Console.WriteLine("Window with title " + targetWindowTitle + " not found"); return null; }

            if (!cachedRectSet) NativeMethods.GetWindowRect(hWnd, out rect);

            int adjustedTop = rect.Top + titleBarHeight;
            int adjustedLeft = rect.Left + borderWidth;
            int adjustedRight = rect.Right - borderWidth;
            int adjustedBottom = rect.Bottom;

            int width = adjustedRight - adjustedLeft;
            int height = adjustedBottom - adjustedTop;

            // Capture into a bitmap sized to CAPTURE (FRAME_*)
            Bitmap bmp = new Bitmap(MainForm.FRAME_WIDTH, MainForm.FRAME_HEIGHT, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Copy source screen region; this does not scale. If the window’s interior is not the same size
                // as FRAME_*, you may prefer DrawImage from a screen copy; keeping as-is to match existing behavior.
                g.CopyFromScreen(adjustedLeft, adjustedTop, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }

            if (ActiveColorMode == ColorMode.GREYSCALE)
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                                  ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                int bpp = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                byte[] buf = new byte[bmpData.Stride * bmpData.Height];
                Marshal.Copy(bmpData.Scan0, buf, 0, buf.Length);

                for (int y = 0; y < bmpData.Height; y++)
                {
                    int row = y * bmpData.Stride;
                    for (int x = 0; x < bmpData.Width; x++)
                    {
                        int o = row + x * bpp;
                        Color c = Color.FromArgb(buf[o + 2], buf[o + 1], buf[o]);
                        int idx = GetGameBoyColorIndex(c);
                        Color q = GameBoyColors[idx];
                        buf[o] = q.B; buf[o + 1] = q.G; buf[o + 2] = q.R;
                    }
                }

                Marshal.Copy(buf, 0, bmpData.Scan0, buf.Length);
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }

        // -------------- Generate (mode-aware, emitted-sized) --------------
        public static (List<int>, List<int>) GeneratePixelDataFromWindow(
            string targetWindowTitle,
            int borderWidth,
            int titleBarHeight,
            int width, int height,
            bool forceFullFrame,
            bool rowExpansionEnabled,
            double brightnessFactor,
            double darkenFactor)
        {
            Bitmap captured = CaptureWindow(targetWindowTitle, borderWidth, titleBarHeight, brightnessFactor, darkenFactor);
            if (captured == null) return (null, null);

            // If capture == emitted size, reuse the capture; otherwise scale once.
            Bitmap emitBmp;
            if (captured.Width == width && captured.Height == height)
            {
                emitBmp = captured; // no resize/blit; reuse as-is
            }
            else
            {
                emitBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(emitBmp))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.DrawImage(
                        captured,
                        new Rectangle(0, 0, width, height),
                        new Rectangle(0, 0, captured.Width, captured.Height),
                        GraphicsUnit.Pixel
                    );
                }
                captured.Dispose(); // only dispose when we created a new emitBmp
            }

            List<int> pixelDataList = new List<int>();
            rgbToSpans = new Dictionary<int, List<int>>();

            // Diff against cached (also emitted-sized)
            BitmapData currentBmpData = emitBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, emitBmp.PixelFormat);
            BitmapData cachedBmpData = _cachedBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, _cachedBitmap.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(emitBmp.PixelFormat) / 8;
            byte[] currentBmpBytes = new byte[width * height * bytesPerPixel];
            byte[] cachedBmpBytes = new byte[width * height * bytesPerPixel];

            Marshal.Copy(currentBmpData.Scan0, currentBmpBytes, 0, currentBmpBytes.Length);
            Marshal.Copy(cachedBmpData.Scan0, cachedBmpBytes, 0, cachedBmpBytes.Length);

            int stride = currentBmpData.Stride;
            int spanStart, offset;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width;)
                {
                    offset = y * stride + x * bytesPerPixel;

                    if (ActiveColorMode == ColorMode.RGB)
                    {
                        Color cur = GetColorFromOffset(currentBmpBytes, offset);
                        Color prev = GetColorFromOffset(cachedBmpBytes, offset);

                        if (forceFullFrame || cur.R != prev.R || cur.G != prev.G || cur.B != prev.B)
                        {
                            spanStart = x;
                            x = IdentifySpanRgb(currentBmpBytes, x, y, stride, width, bytesPerPixel, cur);
                            int spanLength = x - spanStart;
                            int packedXYZ = PackXYZ(spanStart, y, spanLength);
                            StoreSpanRgb(rgbToSpans, cur, packedXYZ);
                        }
                        else x++;
                    }
                    else
                    {
                        int curIdx = GetGameBoyColorIndex(GetColorFromOffset(currentBmpBytes, offset));
                        int prevIdx = GetGameBoyColorIndex(GetColorFromOffset(cachedBmpBytes, offset));

                        if (forceFullFrame || curIdx != prevIdx)
                        {
                            spanStart = x;
                            x = IdentifySpanGb(currentBmpBytes, x, y, stride, width, bytesPerPixel, curIdx);
                            int spanLength = x - spanStart;
                            int packedXYZ = PackXYZ(spanStart, y, spanLength);
                            StoreSpanGb(rgbToSpans, curIdx, packedXYZ);
                        }
                        else x++;
                    }
                }
            }

            contiguousRangePairs = new List<int>();

            foreach (var kvp in rgbToSpans)
            {
                pixelDataList.Add(kvp.Key);
                pixelDataList.AddRange(kvp.Value);
                pixelDataList.Add(-kvp.Value.Last());
            }

            emitBmp.UnlockBits(currentBmpData);
            _cachedBitmap.UnlockBits(cachedBmpData);
            _cachedBitmap = emitBmp; // cache emitted-sized frame (possibly the capture itself)

            return (pixelDataList, contiguousRangePairs);
        }

        // -------------- Row height application (unchanged) --------------
        private static void InitializeRowExpansionAmounts()
        {
            rowExpansionAmounts = new Dictionary<int, int>();
            for (int i = 0; i < MainForm.EMITTED_HEIGHT; i++)
                rowExpansionAmounts[i] = 1;
        }

        private static void SetRowHeight(int rowIndex, int rowHeight)
        {
            if (rowIndex < 0 || rowHeight < 1) return;
            rowExpansionAmounts[rowIndex] = rowHeight;
        }

        private static void ApplyRowHeight(Bitmap bitmap, int rowIndex, int rowHeight)
        {
            if (rowIndex < 0 || rowIndex >= bitmap.Height) return;
            if (rowHeight < 1 || rowIndex - rowHeight < -1) return;

            for (int y = rowIndex; y > rowIndex - rowHeight; y--)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, rowIndex);
                    bitmap.SetPixel(x, y, pixelColor);
                }
            }
        }

        private static void ApplyRowHeights(Bitmap bitmap)
        {
            foreach (var row in rowExpansionAmounts)
                ApplyRowHeight(_simulatedCanvas, row.Key, row.Value);
        }

        // -------------- Preview decode (mode-aware, emitted-sized) --------------
        public static Bitmap SetPixelDataToBitmap()
        {
            if (rowExpansionAmounts == null) InitializeRowExpansionAmounts();

            int i = 0;
            int nPixelsChanged = 0;

            while (i < MemoryMappedFileManager.readPixelDataLength)
            {
                int colorKey = MemoryMappedFileManager.readPixelData[i++];

                while (i < MemoryMappedFileManager.readPixelDataLength &&
                       MemoryMappedFileManager.readPixelData[i] >= 0)
                {
                    int packed = MemoryMappedFileManager.readPixelData[i++];
                    UnpackXYZ(packed, out int xStart, out int y, out int spanLength);

                    for (int x = xStart; x < xStart + spanLength; x++)
                    {
                        Color newPixelColor = (ActiveColorMode == ColorMode.RGB)
                            ? GetColorFromIndex(colorKey)      // full RGB
                            : GameBoyColors[colorKey];         // GB indexed
                        _simulatedCanvas.SetPixel(x, y, newPixelColor);
                        nPixelsChanged++;
                    }
                }
                i++; // skip negative delimiter
            }

            for (i = 0; i < MemoryMappedFileManager.readContiguousRangePairsLength; i += 2)
            {
                int rowIndex = MemoryMappedFileManager.readContiguousRangePairs[i];
                int rowHeight = MemoryMappedFileManager.readContiguousRangePairs[i + 1];
                SetRowHeight(rowIndex, rowHeight);
            }

            Console.WriteLine("Preview pixels changed: " + nPixelsChanged);
            MainForm.latestPreviewPixelsChangedCount = nPixelsChanged;

            ApplyRowHeights(_simulatedCanvas);
            return _simulatedCanvas;
        }
    }
}
