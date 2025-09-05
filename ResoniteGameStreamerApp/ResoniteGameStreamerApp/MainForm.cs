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

        public MainForm()
        {
            InitializeComponent();
            _random = new Random();
            programStartTime = DateTime.Now;

            // Load settings into fields first
            _settings = SettingsManager.Load();
            FRAME_WIDTH = _settings.FrameWidth;
            FRAME_HEIGHT = _settings.FrameHeight;
            TargetFramerate = _settings.TargetFramerate;
            targetWindowTitle = _settings.TargetWindowTitle ?? "mGBA";
            borderWidth = _settings.BorderWidth;
            titleBarHeight = _settings.TitleBarHeight;
            fullFrameInterval = Math.Max(1, _settings.FullFrameIntervalSeconds) * 1000;

            // Now reflect into controls, but suppress event handlers while we do it
            _isRestoringSettings = true;

            if (canvasWidthTextBox != null) canvasWidthTextBox.Text = _settings.FrameWidth.ToString();
            if (canvasHeightTextBox != null) canvasHeightTextBox.Text = _settings.FrameHeight.ToString();
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
            pictureBox1.Width = FRAME_WIDTH;
            pictureBox1.Height = FRAME_HEIGHT;

            FrameData._cachedBitmap = new Bitmap(FRAME_WIDTH, FRAME_HEIGHT);
            FrameData._simulatedCanvas = new Bitmap(FRAME_WIDTH, FRAME_HEIGHT);
            FrameData.rowContiguousSpanEndIndices = new int[FRAME_HEIGHT];

            MemoryMappedFileManager.readContiguousRangePairs = new int[FRAME_WIDTH * FRAME_HEIGHT];
            MemoryMappedFileManager.readPixelData = new int[FRAME_WIDTH * FRAME_HEIGHT];
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
                targetWindowTitle, borderWidth, titleBarHeight,
                FRAME_WIDTH, FRAME_HEIGHT, forceFullFrame,
                rowExpansionCheckBox.Checked, brightnessFactor, darkenFactor);

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

            if (previewCheckBox.Checked)
            {
                pictureBox1.Image = FrameData.SetPixelDataToBitmap();
                previewPixelsChangedCountLabel.Text = latestPreviewPixelsChangedCount.ToString();
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
                SettingsManager.Save(_settings);
                InitializeCanvas();
            }
        }

        private void consolePresetComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedValue = consolePresetComboBox.SelectedItem.ToString();
            if (selectedValue == "Greyscale")
            {
                targetWindowTextBox.Text = "mGBA";
                canvasWidthTextBox.Text = "160";
                canvasHeightTextBox.Text = "144";
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
    }
}
