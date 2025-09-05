using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.MemoryMappedFiles;
using System.Text.Json;
using System.IO;
using System.Globalization;

namespace ResoniteGameStreamerApp
{
    public partial class MainForm : Form
    {
        private Timer _timer;
        private Random _random;
        private int MAX_FRAME_WIDTH = 999;
        private int MAX_FRAME_HEIGHT = 999;
        public static int FRAME_WIDTH = 240;
        public static int FRAME_HEIGHT = 160;
        public static int EMITTED_WIDTH = 240;
        public static int EMITTED_HEIGHT = 160;
        private int TargetFramerate = 36;

        private int PixelDataMemoryMappedFileSize;

        private int fullFrameInterval = 30 * 1000; // ms
        private DateTime _lastFullFrameTime = DateTime.MinValue;
        private DateTime programStartTime;

        public static double brightnessFactor = 1.0;
        public double darkenFactor = 0.0;
        public string targetWindowTitle = "mGBA";
        private int titleBarHeight = 30;
        private int borderWidth = 8;

        private int _frameCounter = 0;
        private Timer _fpsTimer;

        private DateTime _lastTickTime = DateTime.Now;
        public static int latestPreviewPixelsChangedCount = 0;

        private AppSettings _settings = new AppSettings();
        private bool _isRestoringSettings = false;

        // Rolling average window (seconds) — change here, nowhere else.
        private const double AverageWindowSeconds = 10.0;

        // Rolling windows and sums
        private readonly Queue<DateTime> _avgFpsWindow = new Queue<DateTime>();
        private readonly Queue<(DateTime time, int pixels)> _avgPixelsWindow = new Queue<(DateTime, int)>();
        private long _avgPixelsSum = 0;

        // To avoid double-counting pixels for the same frame
        private int _lastPixelsOffsetCounted = int.MinValue;

        public MainForm()
        {
            InitializeComponent();
            _random = new Random();
            programStartTime = DateTime.Now;

            // Load settings into fields first
            _settings = SettingsManager.Load();
            FRAME_WIDTH = _settings.FrameWidth;
            FRAME_HEIGHT = _settings.FrameHeight;

            EMITTED_WIDTH = (_settings.EmittedFrameWidth >= 100 && _settings.EmittedFrameWidth <= 999)
                ? _settings.EmittedFrameWidth : FRAME_WIDTH;
            EMITTED_HEIGHT = (_settings.EmittedFrameHeight >= 100 && _settings.EmittedFrameHeight <= 999)
                ? _settings.EmittedFrameHeight : FRAME_HEIGHT;

            TargetFramerate = _settings.TargetFramerate;
            targetWindowTitle = _settings.TargetWindowTitle ?? "mGBA";
            borderWidth = _settings.BorderWidth;
            titleBarHeight = _settings.TitleBarHeight;
            fullFrameInterval = Math.Max(1, _settings.FullFrameIntervalSeconds) * 1000;

            // Now reflect into controls, but suppress event handlers while we do it
            _isRestoringSettings = true;

            if (canvasWidthTextBox != null) canvasWidthTextBox.Text = _settings.FrameWidth.ToString();
            if (canvasHeightTextBox != null) canvasHeightTextBox.Text = _settings.FrameHeight.ToString();

            if (emittedCanvasWidthTextBox != null) emittedCanvasWidthTextBox.Text = EMITTED_WIDTH.ToString();
            if (emittedCanvasHeightTextBox != null) emittedCanvasHeightTextBox.Text = EMITTED_HEIGHT.ToString();

            if (targetFramerateTextBox != null) targetFramerateTextBox.Text = _settings.TargetFramerate.ToString();
            if (targetWindowTextBox != null) targetWindowTextBox.Text = _settings.TargetWindowTitle;
            if (borderWidthTextBox != null) borderWidthTextBox.Text = _settings.BorderWidth.ToString();

            // BUGFIX: you had `if (titleBarHeight != null)` comparing an int to null
            if (titleBarHeightTextBox != null) titleBarHeightTextBox.Text = _settings.TitleBarHeight.ToString();

            if (fullFrameIntervalTextBox != null)
                fullFrameIntervalTextBox.Text = _settings.FullFrameIntervalSeconds.ToString();

            if (colorModeComboBox != null)
            {
                colorModeComboBox.Items.Clear();
                colorModeComboBox.Items.Add("RGB");
                colorModeComboBox.Items.Add("Greyscale");
                var idx = string.Equals(_settings.ColorMode, "Greyscale", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                colorModeComboBox.SelectedIndex = idx;
                FrameData.ActiveColorMode = idx == 0 ? ColorMode.RGB : ColorMode.GREYSCALE;
            }

            _isRestoringSettings = false;
        }

        private void InitializeCanvas()
        {
            PixelDataMemoryMappedFileSize = ((MAX_FRAME_WIDTH * MAX_FRAME_HEIGHT * 2) + 3) * sizeof(int);

            // Preview display uses emitted dimensions
            pictureBox1.Width = EMITTED_WIDTH;
            pictureBox1.Height = EMITTED_HEIGHT;

            // Decoding & diff happen at emitted dimensions
            FrameData._cachedBitmap = new Bitmap(EMITTED_WIDTH, EMITTED_HEIGHT);
            FrameData._simulatedCanvas = new Bitmap(EMITTED_WIDTH, EMITTED_HEIGHT);
            FrameData.rowContiguousSpanEndIndices = new int[EMITTED_HEIGHT];

            // Reader working buffers sized by emitted dims (worst case 1 int per pixel in your layout)
            MemoryMappedFileManager.readContiguousRangePairs = new int[EMITTED_WIDTH * EMITTED_HEIGHT];
            MemoryMappedFileManager.readPixelData = new int[EMITTED_WIDTH * EMITTED_HEIGHT];
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            _timer = new Timer();
            _timer.Interval = (int)((1.0 / TargetFramerate) * 1000);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _fpsTimer = new Timer();
            _fpsTimer.Interval = 1000;
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            InitializeCanvas();
            Console.WriteLine("Form loaded with Timer Interval: " + _timer.Interval);
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            publishedFPSLabel.Text = _frameCounter.ToString();
            _frameCounter = 0;
            PruneAndUpdateAverages(DateTime.Now);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!checkBox1.Checked) return;
            if (checkBox3.Checked && !(MemoryMappedFileManager.clientRenderConfirmed())) return;

            var startTickTime = DateTime.Now;

            bool forceFullFrame = false;
            if ((DateTime.Now - _lastFullFrameTime).TotalMilliseconds >= fullFrameInterval)
            {
                forceFullFrame = true;
                _lastFullFrameTime = DateTime.Now;
            }
            MemoryMappedFileManager._lastFrameTime = DateTime.Now;

            var (pixelData, contiguousRangePairs) = FrameData.GeneratePixelDataFromWindow(
                targetWindowTitle,
                borderWidth,
                titleBarHeight,
                EMITTED_WIDTH, EMITTED_HEIGHT,
                forceFullFrame,
                rowExpansionCheckBox.Checked,
                brightnessFactor,
                darkenFactor);

            if (pixelData == null) return;

            MemoryMappedFileManager.WritePixelDataToMemoryMappedFile(
                pixelData, contiguousRangePairs, PixelDataMemoryMappedFileSize, forceFullFrame);

            if (MemoryMappedFileManager._pixelDataMemoryMappedViewStream == null)
            {
                MemoryMappedFileManager._pixelDataMemoryMappedViewStream =
                    MemoryMappedFileManager._pixelDataMemoryMappedFile.CreateViewStream();
                MemoryMappedFileManager._pixelDataBinaryReader =
                    new BinaryReader(MemoryMappedFileManager._pixelDataMemoryMappedViewStream);
            }

            MemoryMappedFileManager.ReadPixelDataFromMemoryMappedFile();
            if (MemoryMappedFileManager.readPixelData == null) return;

            if (MemoryMappedFileManager.readPixelDataLength >= 0)
            {
                _avgFpsWindow.Enqueue(DateTime.Now);
            }

            // If preview is enabled, decode+draw now so latestPreviewPixelsChangedCount is up-to-date.
            if (previewCheckBox.Checked)
            {
                pictureBox1.Image = FrameData.SetPixelDataToBitmap();
            }

            // Count pixels for this frame regardless of preview state (no extra pass).
            int currentOffset = MemoryMappedFileManager.latestReceivedFrameMillisecondsOffset;
            if (currentOffset != _lastPixelsOffsetCounted && MemoryMappedFileManager.readPixelDataLength != -1)
            {
                int px = previewCheckBox.Checked
                    ? latestPreviewPixelsChangedCount                 // set by SetPixelDataToBitmap()
                    : FrameData.LastGeneratedChangedPixelsCount;      // tallied during span generation

                // Keep UI/rolling stats updated even with preview off
                previewPixelsChangedCountLabel.Text = px.ToString();
                latestPreviewPixelsChangedCount = px;

                _avgPixelsWindow.Enqueue((DateTime.Now, px));
                _avgPixelsSum += px;
                _lastPixelsOffsetCounted = currentOffset;
            }

            if (checkBox4.Checked)
            {
                MemoryMappedFileManager.WriteLatestReceivedFrameMillisecondsOffsetToMemoryMappedFile();
            }
            _frameCounter++;

            var endTickTime = DateTime.Now;
            var executionTime = (endTickTime - startTickTime).TotalMilliseconds;
            _timer.Interval = Math.Max(1, (int)((1.0 / TargetFramerate) * 1000) - (int)executionTime);
            _lastTickTime = endTickTime;
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SettingsManager.Save(_settings);
            MemoryMappedFileManager._pixelDataMemoryMappedFile?.Dispose();
        }

        private void PruneAndUpdateAverages(DateTime now)
        {
            var threshold = now - TimeSpan.FromSeconds(AverageWindowSeconds);

            // FPS window
            while (_avgFpsWindow.Count > 0 && _avgFpsWindow.Peek() < threshold)
                _avgFpsWindow.Dequeue();

            // Pixels window (maintain running sum)
            while (_avgPixelsWindow.Count > 0 && _avgPixelsWindow.Peek().time < threshold)
                _avgPixelsSum -= _avgPixelsWindow.Dequeue().pixels;

            // Compute and display
            double avgFps = _avgFpsWindow.Count / AverageWindowSeconds;
            avgFPSLabel.Text = avgFps.ToString("0.0");

            double avgPixelsPerFrame = _avgPixelsWindow.Count > 0
                ? (double)_avgPixelsSum / _avgPixelsWindow.Count
                : 0.0;
            avgPixelsLabel.Text = avgPixelsPerFrame.ToString("0");
        }


        private void targetFramerateTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings || _timer == null) return;  // <-- guard

            if (int.TryParse(targetFramerateTextBox.Text, out int selectedTargetFramerate)
                && selectedTargetFramerate <= 60 && selectedTargetFramerate >= 1)
            {
                TargetFramerate = selectedTargetFramerate;
                _settings.TargetFramerate = TargetFramerate;
                SettingsManager.Save(_settings);

                _timer.Interval = (int)((1.0 / TargetFramerate) * 1000);
                Console.WriteLine("TargetFramerate changed to " + TargetFramerate + " and Timer Interval set to " + _timer.Interval);
            }
        }

        private void brightnessTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!double.TryParse(brightnessTextBox.Text, out brightnessFactor) ||
                brightnessFactor < 0 || brightnessFactor > 2.0)
                return;
        }

        private void targetWindowTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings) return;
            targetWindowTitle = targetWindowTextBox.Text;
            _settings.TargetWindowTitle = targetWindowTitle;
            SettingsManager.Save(_settings);
        }

        private void fullFrameIntervalTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings) return;

            if (int.TryParse(fullFrameIntervalTextBox.Text, out int seconds) && seconds >= 1)
            {
                // runtime uses ms
                fullFrameInterval = seconds * 1000;

                // persist in seconds
                _settings.FullFrameIntervalSeconds = seconds;
                SettingsManager.Save(_settings);
            }
        }

        private void borderWidthTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings) return;
            if (int.TryParse(borderWidthTextBox.Text, out int selectedBorderWidth))
            {
                borderWidth = selectedBorderWidth;
                _settings.BorderWidth = borderWidth;
                SettingsManager.Save(_settings);
            }
        }

        private void canvasWidthTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings) return;
            if (int.TryParse(canvasWidthTextBox.Text, out int selectedCanvasWidth) &&
                selectedCanvasWidth >= 100 && selectedCanvasWidth <= 999)
            {
                FRAME_WIDTH = selectedCanvasWidth;
                _settings.FrameWidth = FRAME_WIDTH;

                // Auto-sync emitted WIDTH to match capture WIDTH
                EMITTED_WIDTH = FRAME_WIDTH;
                _settings.EmittedFrameWidth = EMITTED_WIDTH;

                // Update the emitted width textbox without triggering its handler
                _isRestoringSettings = true;
                if (emittedCanvasWidthTextBox != null)
                    emittedCanvasWidthTextBox.Text = EMITTED_WIDTH.ToString();
                _isRestoringSettings = false;

                SettingsManager.Save(_settings);
                InitializeCanvas();
            }
        }

        private void canvasHeightTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings) return;
            if (int.TryParse(canvasHeightTextBox.Text, out int selectedCanvasHeight) &&
                selectedCanvasHeight >= 100 && selectedCanvasHeight <= 999)
            {
                FRAME_HEIGHT = selectedCanvasHeight;
                _settings.FrameHeight = FRAME_HEIGHT;

                // Auto-sync emitted HEIGHT to match capture HEIGHT
                EMITTED_HEIGHT = FRAME_HEIGHT;
                _settings.EmittedFrameHeight = EMITTED_HEIGHT;

                // Update the emitted height textbox without triggering its handler
                _isRestoringSettings = true;
                if (emittedCanvasHeightTextBox != null)
                    emittedCanvasHeightTextBox.Text = EMITTED_HEIGHT.ToString();
                _isRestoringSettings = false;

                SettingsManager.Save(_settings);
                InitializeCanvas();
            }
        }

        private void consolePresetComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedValue = consolePresetComboBox.SelectedItem.ToString();
            if (selectedValue == "Gameboy")
            {
                targetWindowTextBox.Text = "mGBA";
                canvasWidthTextBox.Text = "160";
                canvasHeightTextBox.Text = "144";
                emittedCanvasWidthTextBox.Text = "160";
                emittedCanvasHeightTextBox.Text = "144";
            }

            if (selectedValue == "Doom")
            {
                targetWindowTextBox.Text = "Chocolate Doom";
                canvasWidthTextBox.Text = "318";
                canvasHeightTextBox.Text = "240";
                emittedCanvasWidthTextBox.Text = "318";
                emittedCanvasHeightTextBox.Text = "240";
            }
        }


        private void colorModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings) return;
            var sel = colorModeComboBox.SelectedItem?.ToString();
            FrameData.ActiveColorMode = string.Equals(sel, "RGB", StringComparison.OrdinalIgnoreCase)
                ? ColorMode.RGB
                : ColorMode.GREYSCALE;

            _settings.ColorMode = sel ?? "RGB";
            SettingsManager.Save(_settings);
            Console.WriteLine("Color mode: " + FrameData.ActiveColorMode);
        }
        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void emittedCanvasWidthTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings) return;
            if (int.TryParse(emittedCanvasWidthTextBox.Text, out int w) && w >= 100 && w <= 999)
            {
                EMITTED_WIDTH = w;
                _settings.EmittedFrameWidth = EMITTED_WIDTH;
                SettingsManager.Save(_settings);

                pictureBox1.Width = EMITTED_WIDTH;
                InitializeCanvas(); // reallocate decode/cached to new emitted size
            }
        }

        private void emittedCanvasHeightTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isRestoringSettings) return;
            if (int.TryParse(emittedCanvasHeightTextBox.Text, out int h) && h >= 100 && h <= 999)
            {
                EMITTED_HEIGHT = h;
                _settings.EmittedFrameHeight = EMITTED_HEIGHT;
                SettingsManager.Save(_settings);

                pictureBox1.Height = EMITTED_HEIGHT;
                InitializeCanvas(); // reallocate decode/cached to new emitted size
            }
        }

        private void setEmittedCanvasScaleFactorButton_Click(object sender, EventArgs e)
        {
            string raw = setEmittedCanvasScaleFactorTextBox?.Text?.Trim() ?? "";
            double scale;

            // Try invariant (1.5) first, then fallback to current culture (in case of comma decimals)
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out scale) &&
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out scale))
            {
                MessageBox.Show("Scale factor must be a number (e.g., 2 or 1.5).",
                    "Invalid scale factor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(scale > 0.0 && scale < 10.0))
            {
                MessageBox.Show("Scale factor must be > 0 and < 10.",
                    "Invalid scale factor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int newW = (int)Math.Round(FRAME_WIDTH * scale);
            int newH = (int)Math.Round(FRAME_HEIGHT * scale);

            // Respect the same emitted validation used elsewhere (100..999)
            if (newW < 100 || newW > 999 || newH < 100 || newH > 999)
            {
                MessageBox.Show(
                    $"Resulting emitted size would be {newW}×{newH}.\n" +
                    "Emitted width and height must be between 100 and 999.",
                    "Size out of range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Update UI textboxes without triggering handlers
            _isRestoringSettings = true;
            if (emittedCanvasWidthTextBox != null) emittedCanvasWidthTextBox.Text = newW.ToString();
            if (emittedCanvasHeightTextBox != null) emittedCanvasHeightTextBox.Text = newH.ToString();
            _isRestoringSettings = false;

            // Apply emitted sizes and persist them (scale factor itself is not saved)
            EMITTED_WIDTH = newW;
            EMITTED_HEIGHT = newH;
            _settings.EmittedFrameWidth = EMITTED_WIDTH;
            _settings.EmittedFrameHeight = EMITTED_HEIGHT;
            SettingsManager.Save(_settings);

            pictureBox1.Width = EMITTED_WIDTH;
            pictureBox1.Height = EMITTED_HEIGHT;
            InitializeCanvas();
        }

    }
}
