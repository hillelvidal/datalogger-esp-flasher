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
        private string? _localFirmwarePath;
        private string? _lastFirmwareFolder;
        private const string SettingsFile = "flasher-settings.json";

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
            // Load saved settings
            LoadSettings();
            
            // Try to load last firmware folder FIRST (priority over remote)
            if (!string.IsNullOrEmpty(_lastFirmwareFolder) && Directory.Exists(_lastFirmwareFolder))
            {
                LoadFirmwareFromFolder(_lastFirmwareFolder);
            }
            
            await InitializeFirestoreAsync();
            
            // Only load remote firmware if no local firmware is selected
            if (string.IsNullOrEmpty(_localFirmwarePath))
            {
                await RefreshFirmwareVersionsAsync();
            }
            
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
            bool hasFirmware = !string.IsNullOrEmpty(_localFirmwarePath) || cmbFirmwareVersion.SelectedItem != null;
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

            // Check if we're using local firmware or Firebase firmware
            string firmwarePath;
            string firmwareName;
            
            if (!string.IsNullOrEmpty(_localFirmwarePath))
            {
                // Using local firmware
                firmwarePath = _localFirmwarePath;
                firmwareName = Path.GetFileName(_localFirmwarePath);
            }
            else if (cmbFirmwareVersion.SelectedItem is FirmwareVersion selectedFirmware)
            {
                // Using Firebase firmware
                firmwareName = selectedFirmware.Version;
                
                // Download if needed
                if (_downloadService.IsFirmwareDownloaded(selectedFirmware))
                {
                    firmwarePath = _downloadService.GetLocalFirmwarePath(selectedFirmware);
                    lblStatus.Text = "Using cached firmware";
                }
                else
                {
                    try
                    {
                        lblStatus.Text = "Downloading firmware...";
                        firmwarePath = await _downloadService.DownloadFirmwareAsync(selectedFirmware);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to download firmware");
                        MessageBox.Show($"Failed to download firmware: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a firmware version or browse for a local file.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show device preparation instructions
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

                // Flash firmware
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

        private void btnBrowseLocal_Click(object sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select folder containing firmware files (bootloader.bin, partitions.bin, firmware.bin)",
                ShowNewFolderButton = false,
                SelectedPath = _lastFirmwareFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (folderDialog.ShowDialog() == DialogResult.Yes)
            {
                var folder = folderDialog.SelectedPath;
                LoadFirmwareFromFolder(folder);
                
                // Save this folder for next time
                _lastFirmwareFolder = folder;
                SaveSettings();
            }
        }
        
        private void LoadFirmwareFromFolder(string folder)
        {
            var firmwarePath = Path.Combine(folder, "firmware.bin");
            var bootloaderPath = Path.Combine(folder, "bootloader.bin");
            var partitionsPath = Path.Combine(folder, "partitions.bin");
            
            bool hasFirmware = File.Exists(firmwarePath);
            bool hasBootloader = File.Exists(bootloaderPath);
            bool hasPartitions = File.Exists(partitionsPath);
            
            if (!hasFirmware)
            {
                MessageBox.Show(
                    $"firmware.bin not found in selected folder:\n{folder}\n\nPlease select a folder containing firmware.bin",
                    "Firmware Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            
            _localFirmwarePath = firmwarePath;
            var folderName = Path.GetFileName(folder);
            
            cmbFirmwareVersion.Items.Clear();
            cmbFirmwareVersion.Items.Add($"[Local] {folderName}");
            cmbFirmwareVersion.SelectedIndex = 0;
            
            var status = hasBootloader && hasPartitions 
                ? "✓ Complete flash (bootloader + partitions + app)"
                : "⚠ App only (bootloader/partitions not found)";
                
            lblFirmwareStatus.Text = status;
            lblStatus.Text = $"Local firmware loaded: {folderName}";
            
            _logger.LogInformation($"Local firmware folder: {folder}");
            _logger.LogInformation($"Firmware: {hasFirmware}, Bootloader: {hasBootloader}, Partitions: {hasPartitions}");
            
            UpdateFlashButtonState();
        }
        
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (settings != null && settings.ContainsKey("LastFirmwareFolder"))
                    {
                        _lastFirmwareFolder = settings["LastFirmwareFolder"];
                        _logger.LogInformation($"Loaded last firmware folder: {_lastFirmwareFolder}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load settings");
            }
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _flashCancellationTokenSource?.Cancel();
            _downloadService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
