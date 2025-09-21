using System;
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
        private FirestoreService? _firestoreService;
        private DeviceDetectionService _deviceService = null!;
        private FirmwareDownloadService _downloadService = null!;
        private EspFlashingService _flashingService = null!;
        
        private List<FirmwareVersion> _firmwareVersions = new();
        private List<EspDevice> _espDevices = new();
        private CancellationTokenSource? _flashCancellationTokenSource;

        public MainForm(ILogger<MainForm> logger)
        {
            _logger = logger;
            InitializeComponent();
            InitializeServices();
            SetupEventHandlers();
            
            // Initialize UI state
            btnFlash.Enabled = false;
            btnRefreshDevices.Enabled = true;
            progressBarFlash.Visible = false;
            lblStatus.Text = "Ready";
        }

        private void InitializeServices()
        {
            _deviceService = new DeviceDetectionService(_logger);
            _downloadService = new FirmwareDownloadService(_logger);
            _flashingService = new EspFlashingService(_logger);
        }

        private void SetupEventHandlers()
        {
            _downloadService.DownloadProgressChanged += OnDownloadProgressChanged;
            _flashingService.FlashProgressChanged += OnFlashProgressChanged;
            _flashingService.FlashStatusChanged += OnFlashStatusChanged;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await InitializeFirestoreAsync();
            await RefreshFirmwareVersionsAsync();
            await RefreshDevicesAsync();
        }

        private async Task InitializeFirestoreAsync()
        {
            try
            {
                lblStatus.Text = "Connecting to Firestore...";
                // Try to locate a service account JSON file
                var (configPath, projectId) = TryFindServiceAccountJson();
                if (configPath == null || projectId == null)
                {
                    MessageBox.Show(
                        "Service account JSON not found. Place your service account JSON as 'firebase-config.json' in the application folder, or any service-account JSON file.",
                        "Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    lblStatus.Text = "Firestore configuration missing";
                    return;
                }

                _firestoreService = new FirestoreService(projectId, configPath, _logger);
                
                var isConnected = await _firestoreService.TestConnectionAsync();
                if (isConnected)
                {
                    lblStatus.Text = "Connected to Firestore";
                    _logger.LogInformation("Firestore connection established");
                }
                else
                {
                    lblStatus.Text = "Firestore connection failed";
                    MessageBox.Show(
                        "Failed to connect to Firestore. Please check your configuration and internet connection.",
                        "Connection Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firestore");
                lblStatus.Text = "Firestore initialization failed";
                MessageBox.Show(
                    $"Failed to initialize Firestore: {ex.Message}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private (string? configPath, string? projectId) TryFindServiceAccountJson()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var preferredPath = Path.Combine(baseDir, "firebase-config.json");
                if (File.Exists(preferredPath))
                {
                    var pid = TryReadProjectId(preferredPath);
                    if (!string.IsNullOrWhiteSpace(pid)) return (preferredPath, pid);
                }

                // Fallback: look for any *.json that looks like a service account
                var jsonFiles = Directory.GetFiles(baseDir, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var jf in jsonFiles)
                {
                    var pid = TryReadProjectId(jf);
                    if (!string.IsNullOrWhiteSpace(pid)) return (jf, pid);
                }

                // Also check the project directory (one level up from bin/...)
                var projectDir = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName;
                if (!string.IsNullOrEmpty(projectDir) && Directory.Exists(projectDir))
                {
                    var projPreferred = Path.Combine(projectDir, "firebase-config.json");
                    if (File.Exists(projPreferred))
                    {
                        var pid = TryReadProjectId(projPreferred);
                        if (!string.IsNullOrWhiteSpace(pid)) return (projPreferred, pid);
                    }

                    foreach (var jf in Directory.GetFiles(projectDir, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var pid = TryReadProjectId(jf);
                        if (!string.IsNullOrWhiteSpace(pid)) return (jf, pid);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to locate service account JSON");
            }

            return (null, null);
        }

        private string? TryReadProjectId(string jsonPath)
        {
            try
            {
                var text = File.ReadAllText(jsonPath);
                var jo = JObject.Parse(text);
                if (!string.Equals((string?)jo["type"], "service_account", StringComparison.OrdinalIgnoreCase))
                    return null;
                var pid = (string?)jo["project_id"];
                return string.IsNullOrWhiteSpace(pid) ? null : pid;
            }
            catch
            {
                return null;
            }
        }

        private async Task RefreshFirmwareVersionsAsync()
        {
            if (_firestoreService == null) return;

            try
            {
                lblStatus.Text = "Loading firmware versions...";
                cmbFirmwareVersion.Items.Clear();
                
                _firmwareVersions = await _firestoreService.GetFirmwareVersionsAsync();
                
                foreach (var version in _firmwareVersions)
                {
                    cmbFirmwareVersion.Items.Add(version);
                }

                // Select the latest version by default
                var latestVersion = _firmwareVersions.FirstOrDefault(v => v.IsLatest) ?? _firmwareVersions.FirstOrDefault();
                if (latestVersion != null)
                {
                    cmbFirmwareVersion.SelectedItem = latestVersion;
                }

                lblStatus.Text = $"Loaded {_firmwareVersions.Count} firmware versions";
                _logger.LogInformation($"Loaded {_firmwareVersions.Count} firmware versions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load firmware versions");
                lblStatus.Text = "Failed to load firmware versions";
                MessageBox.Show(
                    $"Failed to load firmware versions: {ex.Message}",
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            btnFlash.Enabled = cmbFirmwareVersion.SelectedItem != null && 
                              listBoxDevices.SelectedItem != null &&
                              _flashCancellationTokenSource == null;
        }

        private async void btnFlash_Click(object sender, EventArgs e)
        {
            if (cmbFirmwareVersion.SelectedItem is not FirmwareVersion selectedFirmware ||
                listBoxDevices.SelectedItem is not EspDevice selectedDevice)
            {
                MessageBox.Show("Please select both a firmware version and a target device.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to flash firmware {selectedFirmware.Version} to device {selectedDevice.PortName}?\n\nThis will erase the current firmware on the device.",
                "Confirm Flash Operation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                _flashCancellationTokenSource = new CancellationTokenSource();
                btnFlash.Text = "Cancel";
                btnFlash.Enabled = true;
                progressBarFlash.Visible = true;
                progressBarFlash.Value = 0;

                // Step 1: Download firmware if needed
                string firmwarePath;
                if (_downloadService.IsFirmwareDownloaded(selectedFirmware))
                {
                    firmwarePath = _downloadService.GetLocalFirmwarePath(selectedFirmware);
                    lblStatus.Text = "Using cached firmware";
                }
                else
                {
                    lblStatus.Text = "Downloading firmware...";
                    firmwarePath = await _downloadService.DownloadFirmwareAsync(selectedFirmware);
                }

                // Step 2: Flash firmware
                var success = await _flashingService.FlashFirmwareAsync(
                    firmwarePath, 
                    selectedDevice, 
                    _flashCancellationTokenSource.Token);

                if (success)
                {
                    MessageBox.Show(
                        $"Firmware {selectedFirmware.Version} has been successfully flashed to {selectedDevice.PortName}!",
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

        private async void btnRefreshFirmware_Click(object sender, EventArgs e)
        {
            await RefreshFirmwareVersionsAsync();
        }

        private void cmbFirmwareVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFlashButtonState();
            
            if (cmbFirmwareVersion.SelectedItem is FirmwareVersion selectedFirmware)
            {
                var isDownloaded = _downloadService.IsFirmwareDownloaded(selectedFirmware);
                var status = isDownloaded ? "✓ Downloaded" : "⬇ Will download";
                lblFirmwareStatus.Text = $"{status} - {selectedFirmware.FileSize / 1024 / 1024:F1} MB";
            }
        }

        private void listBoxDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFlashButtonState();
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _flashCancellationTokenSource?.Cancel();
            _downloadService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
