using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using ESPFlasher.Models;
using ESPFlasher.Services;

namespace ESPFlasher
{
    public partial class MainForm : Form
    {
        private readonly ILogger<MainForm> _logger;
        private GoogleDriveService? _googleDriveService;
        private DeviceDetectionService _deviceService = null!;
        private FirmwareDownloadService _downloadService = null!;
        private FirmwareLibraryService _libraryService = null!;
        private EspFlashingService _flashingService = null!;
        private SerialMonitorService _monitorService = null!;
        
        private List<FirmwareItem> _allFirmwares = new();
        private List<EspDevice> _espDevices = new();
        private FirmwareItem? _selectedFirmware;
        private CancellationTokenSource? _flashCancellationTokenSource;
        private const string SettingsFile = "flasher-settings.json";
        private const int MaxMonitorLines = 1000;
        
        private Label lblFirmwareFolder = null!;
        private TextBox txtFirmwareFolder = null!;
        private Button btnBrowseFirmwareFolder = null!;
        private Button btnOpenFirmwareFolder = null!;
        private string _firmwareFolder = string.Empty;

        public MainForm(ILogger<MainForm> logger)
        {
            _logger = logger;
            
            // Initialize firmware folder BEFORE InitializeServices (which needs it)
            _firmwareFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESPFlasher", "Firmware");
            Directory.CreateDirectory(_firmwareFolder);
            
            InitializeComponent();
            
            // Now set the text box after controls are initialized
            txtFirmwareFolder.Text = _firmwareFolder;
            
            InitializeServices();
            SetupEventHandlers();
            
            // Set application icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flash.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load application icon");
            }
            
            // Initialize UI state
            btnFlash.Enabled = false;
            btnRefreshDevices.Enabled = true;
            progressBarFlash.Visible = false;
            lblStatus.Text = "Ready";
            
            // Initialize monitor UI
            cmbBaudRate.SelectedItem = "115200";
            btnStartStopMonitor.Enabled = false;
        }

        private void InitializeServices()
        {
            _deviceService = new DeviceDetectionService(_logger);
            _downloadService = new FirmwareDownloadService(_logger, _firmwareFolder);
            _libraryService = new FirmwareLibraryService(_logger, _firmwareFolder);
            _flashingService = new EspFlashingService(_logger);
            _monitorService = new SerialMonitorService(_logger);
            
            try
            {
                var googleDriveFolderUrl = "https://drive.google.com/drive/folders/1cThzyaIsmPvt0CIrETppBEbOmRr1REr-?usp=sharing";
                _googleDriveService = new GoogleDriveService(googleDriveFolderUrl, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Drive service");
                _googleDriveService = null;
            }
        }

        private void SetupEventHandlers()
        {
            _downloadService.DownloadProgressChanged += OnDownloadProgressChanged;
            _flashingService.FlashProgressChanged += OnFlashProgressChanged;
            _flashingService.FlashStatusChanged += OnFlashStatusChanged;
            _monitorService.DataReceived += OnMonitorDataReceived;
            _monitorService.StatusChanged += OnMonitorStatusChanged;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
            
            await RefreshDevicesAsync();
            
            btnScanLocal_Click(this, EventArgs.Empty);
            
            if (!_libraryService.HasSharedFiles())
            {
                lblStatus.Text = "Warning: Shared bootloader/partitions not found. Download firmware from cloud first.";
            }
        }


        private async Task RefreshDevicesAsync()
        {
            try
            {
                lblStatus.Text = "Scanning for ESP devices...";
                btnRefreshDevices.Enabled = false;
                listBoxDevices.Items.Clear();
                
                _espDevices = await _deviceService.DetectEspDevicesAsync();
                
                foreach (var device in _espDevices)
                {
                    listBoxDevices.Items.Add(device);
                }

                if (_espDevices.Count > 0)
                {
                    listBoxDevices.SelectedIndex = 0;
                    lblStatus.Text = $"Found {_espDevices.Count} ESP device(s)";
                }
                else
                {
                    lblStatus.Text = "No ESP devices found";
                }

                UpdateFlashButtonState();
                _logger.LogInformation($"Device scan completed: {_espDevices.Count} devices found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan for devices");
                lblStatus.Text = "Device scan failed";
                MessageBox.Show(
                    $"Failed to scan for devices: {ex.Message}",
                    "Scan Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnRefreshDevices.Enabled = true;
            }
        }

        private void UpdateFlashButtonState()
        {
            bool hasValidLocalFirmware = !string.IsNullOrEmpty(_localFirmwarePath);
            bool hasCloudFirmware = cmbFirmwareVersion.SelectedItem is FirmwareVersion;
            bool hasFirmware = hasValidLocalFirmware || hasCloudFirmware;
            bool hasDevice = listBoxDevices.SelectedItem != null;
            bool notFlashing = _flashCancellationTokenSource == null;
            
            btnFlash.Enabled = hasFirmware && hasDevice && notFlashing;
        }

        private async void btnFlash_Click(object sender, EventArgs e)
        {
            if (listBoxDevices.SelectedItem is not EspDevice selectedDevice)
            {
                MessageBox.Show("Please select a target device.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedFirmware == null || _selectedFirmware.Status != FirmwareStatus.Downloaded)
            {
                MessageBox.Show("Please select a downloaded firmware.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (!_libraryService.ValidateFirmwareForFlashing(_selectedFirmware))
            {
                MessageBox.Show(
                    "Cannot flash: Missing required files.\n\n" +
                    "Required:\n" +
                    "- firmware.bin (in firmware folder)\n" +
                    "- bootloader.bin (in _common folder)\n" +
                    "- partitions.bin (in _common folder)\n\n" +
                    "Download a firmware from cloud to get shared files.",
                    "Missing Files",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            
            var firmwarePath = _selectedFirmware.LocalPath;
            var firmwareName = _selectedFirmware.DisplayName;

            var prepResult = MessageBox.Show(
                $"Prepare ESP32-S3 for flashing:\n\n" +
                $"1. Hold BOOT button\n" +
                $"2. Press and release RESET button\n" +
                $"3. Release BOOT button\n\n" +
                $"Device: {selectedDevice.PortName}\n" +
                $"Firmware: {firmwareName}\n\n" +
                $"Ready to flash?",
                "Prepare Device for Flashing",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (prepResult != DialogResult.Yes)
                return;

            try
            {
                _flashCancellationTokenSource = new CancellationTokenSource();
                btnFlash.Text = "Cancel";
                btnFlash.Enabled = true;
                progressBarFlash.Visible = true;
                progressBarFlash.Value = 0;

                var success = await _flashingService.FlashFirmwareAsync(
                    firmwarePath, 
                    selectedDevice, 
                    _flashCancellationTokenSource.Token);

                if (success)
                {
                    MessageBox.Show(
                        $"Firmware '{firmwareName}' has been successfully flashed to {selectedDevice.PortName}!",
                        "Flash Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Firmware flashing failed. Please check the logs for more details.",
                        "Flash Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Flash operation cancelled";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flash operation failed");
                MessageBox.Show(
                    $"Flash operation failed: {ex.Message}",
                    "Flash Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _flashCancellationTokenSource?.Dispose();
                _flashCancellationTokenSource = null;
                btnFlash.Text = "Flash Firmware";
                progressBarFlash.Visible = false;
                UpdateFlashButtonState();
            }
        }

        private void btnFlash_CancelClick()
        {
            _flashCancellationTokenSource?.Cancel();
        }

        private async void btnRefreshDevices_Click(object sender, EventArgs e)
        {
            await RefreshDevicesAsync();
        }



        private void listBoxDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFlashButtonState();
        }


        private void btnBrowseFirmwareFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select parent folder for all firmware versions (subfolders will be scanned)",
                SelectedPath = _firmwareFolder,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _firmwareFolder = dialog.SelectedPath;
                txtFirmwareFolder.Text = _firmwareFolder;
                _downloadService.SetDownloadDirectory(_firmwareFolder);
                _libraryService = new FirmwareLibraryService(_logger, _firmwareFolder);
                
                lblStatus.Text = $"Firmware folder changed to: {Path.GetFileName(_firmwareFolder)}";
                
                btnScanLocal_Click(sender, e);
            }
        }

        private void btnOpenFirmwareFolder_Click(object sender, EventArgs e)
        {
            var firmwareFolder = txtFirmwareFolder.Text;
            
            if (!Directory.Exists(firmwareFolder))
            {
                Directory.CreateDirectory(firmwareFolder);
            }
            
            try
            {
                // Open log file if it exists, otherwise open folder
                var logFile = Path.Combine(firmwareFolder, "flasher.log");
                if (File.Exists(logFile))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logFile,
                        UseShellExecute = true
                    });
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = firmwareFolder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDownloadProgressChanged(object? sender, DownloadProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDownloadProgressChanged(sender, e));
                return;
            }

            progressBarFlash.Value = e.ProgressPercentage;
            lblStatus.Text = $"Downloading... {e.ProgressPercentage}% ({e.BytesDownloaded / 1024 / 1024:F1}/{e.TotalBytes / 1024 / 1024:F1} MB)";
        }

        private void OnFlashProgressChanged(object? sender, FlashProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnFlashProgressChanged(sender, e));
                return;
            }

            progressBarFlash.Value = e.ProgressPercentage;
            lblStatus.Text = e.StatusMessage;
        }

        private void OnFlashStatusChanged(object? sender, string status)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnFlashStatusChanged(sender, status));
                return;
            }

            lblStatus.Text = status;
        }

        
        private void LoadSettings()
        {
        }
        
        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    ["LastFirmwareFolder"] = _lastFirmwareFolder ?? ""
                };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SettingsFile, json);
                _logger.LogInformation("Settings saved");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save settings");
            }
        }

        private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabPageMonitor)
            {
                RefreshMonitorPorts();
            }
        }

        private void RefreshMonitorPorts()
        {
            cmbMonitorPort.Items.Clear();
            
            foreach (var device in _espDevices)
            {
                cmbMonitorPort.Items.Add(device.PortName);
            }
            
            if (cmbMonitorPort.Items.Count > 0)
            {
                cmbMonitorPort.SelectedIndex = 0;
                btnStartStopMonitor.Enabled = true;
            }
            else
            {
                btnStartStopMonitor.Enabled = false;
            }
        }

        private void btnStartStopMonitor_Click(object? sender, EventArgs e)
        {
            if (_monitorService.IsMonitoring)
            {
                StopMonitor();
            }
            else
            {
                StartMonitor();
            }
        }

        private void StartMonitor()
        {
            if (cmbMonitorPort.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port.", "Port Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbBaudRate.SelectedItem == null)
            {
                MessageBox.Show("Please select a baud rate.", "Baud Rate Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string portName = cmbMonitorPort.SelectedItem.ToString()!;
            int baudRate = int.Parse(cmbBaudRate.SelectedItem.ToString()!);

            bool success = _monitorService.StartMonitoring(portName, baudRate);
            
            if (success)
            {
                btnStartStopMonitor.Text = "Stop Monitor";
                btnStartStopMonitor.BackColor = Color.FromArgb(200, 0, 0);
                cmbMonitorPort.Enabled = false;
                cmbBaudRate.Enabled = false;
                txtMonitorOutput.Clear();
                AppendMonitorText($"=== Monitor started on {portName} at {baudRate} baud ==="+ Environment.NewLine);
            }
        }

        private void StopMonitor()
        {
            _monitorService.StopMonitoring();
            
            btnStartStopMonitor.Text = "Start Monitor";
            btnStartStopMonitor.BackColor = Color.FromArgb(0, 150, 0);
            cmbMonitorPort.Enabled = true;
            cmbBaudRate.Enabled = true;
            
            AppendMonitorText(Environment.NewLine + "=== Monitor stopped ===" + Environment.NewLine);
        }

        private void btnClearMonitor_Click(object? sender, EventArgs e)
        {
            txtMonitorOutput.Clear();
        }

        private void OnMonitorDataReceived(object? sender, string data)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnMonitorDataReceived(sender, data));
                return;
            }

            AppendMonitorText(data);
        }

        private void OnMonitorStatusChanged(object? sender, string status)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnMonitorStatusChanged(sender, status));
                return;
            }

            lblStatus.Text = status;
        }

        private void AppendMonitorText(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.TrimEnd('\r', '\n');
                
                // Check if line is part of ESP boot header (dashes or contains "Datalogger v")
                bool isBootHeader = trimmedLine.StartsWith("---") || 
                                   trimmedLine.Contains("Datalogger v") ||
                                   (trimmedLine.StartsWith("===") && trimmedLine.Length > 10);
                
                if (isBootHeader)
                {
                    txtMonitorOutput.SelectionStart = txtMonitorOutput.TextLength;
                    txtMonitorOutput.SelectionColor = Color.Yellow;
                    txtMonitorOutput.AppendText(trimmedLine + "\n");
                }
                else
                {
                    var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"^\[([^\]]+)\]\s+([A-Z]+)(:?.*)$");
                    
                    if (match.Success)
                    {
                        var prefix = match.Groups[1].Value;
                        var keyword = match.Groups[2].Value;
                        var rest = match.Groups[3].Value;
                        
                        txtMonitorOutput.SelectionStart = txtMonitorOutput.TextLength;
                        txtMonitorOutput.SelectionColor = Color.Gray;
                        txtMonitorOutput.AppendText($"[{prefix}] ");
                        
                        txtMonitorOutput.SelectionStart = txtMonitorOutput.TextLength;
                        txtMonitorOutput.SelectionColor = GetColorForKeyword(keyword);
                        txtMonitorOutput.AppendText(keyword);
                        
                        txtMonitorOutput.SelectionStart = txtMonitorOutput.TextLength;
                        txtMonitorOutput.SelectionColor = Color.LimeGreen;
                        txtMonitorOutput.AppendText(rest + "\n");
                    }
                    else
                    {
                        txtMonitorOutput.SelectionStart = txtMonitorOutput.TextLength;
                        txtMonitorOutput.SelectionColor = Color.LightGray;
                        txtMonitorOutput.AppendText(trimmedLine + "\n");
                    }
                }
            }
            
            if (txtMonitorOutput.Lines.Length > MaxMonitorLines)
            {
                var allLines = txtMonitorOutput.Lines;
                var newLines = allLines.Skip(allLines.Length - MaxMonitorLines).ToArray();
                txtMonitorOutput.Lines = newLines;
            }
            
            txtMonitorOutput.SelectionStart = txtMonitorOutput.Text.Length;
            txtMonitorOutput.ScrollToCaret();
        }
        
        private Color GetColorForKeyword(string keyword)
        {
            var hash = keyword.GetHashCode();
            
            var hue = Math.Abs(hash % 360);
            var saturation = 0.7;
            var brightness = 0.9;
            
            return ColorFromHSV(hue, saturation, brightness);
        }
        
        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _monitorService?.StopMonitoring();
            _flashCancellationTokenSource?.Cancel();
            _downloadService?.Dispose();
            _monitorService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
