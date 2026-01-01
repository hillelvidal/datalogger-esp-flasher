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
        private FirestoreService? _firestoreService;
        private GoogleDriveService? _googleDriveService;
        private DeviceDetectionService _deviceService = null!;
        private FirmwareDownloadService _downloadService = null!;
        private EspFlashingService _flashingService = null!;
        private SerialMonitorService _monitorService = null!;
        
        private List<FirmwareVersion> _firmwareVersions = new();
        private List<EspDevice> _espDevices = new();
        private CancellationTokenSource? _flashCancellationTokenSource;
        private string? _localFirmwarePath;
        private string? _lastFirmwareFolder;
        private readonly Dictionary<string, string> _localFirmwareFolders = new();
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
            _flashingService = new EspFlashingService(_logger);
            _monitorService = new SerialMonitorService(_logger);
            
            // Initialize Google Drive service with your public folder
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
            // Load saved settings
            LoadSettings();
            
            // Try to load last firmware folder FIRST (priority over remote)
            if (!string.IsNullOrEmpty(_lastFirmwareFolder) && Directory.Exists(_lastFirmwareFolder))
            {
                LoadFirmwareFromFolder(_lastFirmwareFolder);
            }
            
            DiscoverLocalFirmwareFolders();
            
            // Don't auto-load Firebase - it's on-demand via Refresh button
            // This makes startup faster and local mode primary
            
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
                lblStatus.Text = "Loading firmware versions from cloud...";
                
                // Remember if we had local firmware selected
                var hadLocalFirmware = !string.IsNullOrEmpty(_localFirmwarePath);
                var localFirmwareName = hadLocalFirmware ? cmbFirmwareVersion.Text : null;
                
                cmbFirmwareVersion.Items.Clear();
                
                foreach (var localDisplay in _localFirmwareFolders.Keys)
                {
                    cmbFirmwareVersion.Items.Add(localDisplay);
                }
                
                _firmwareVersions = await _firestoreService.GetFirmwareVersionsAsync();
                
                foreach (var version in _firmwareVersions)
                {
                    cmbFirmwareVersion.Items.Add(version);
                }

                // Restore selection
                if (hadLocalFirmware && !string.IsNullOrEmpty(localFirmwareName) && _localFirmwareFolders.ContainsKey(localFirmwareName))
                {
                    cmbFirmwareVersion.SelectedItem = localFirmwareName;
                    lblStatus.Text = $"Loaded {_firmwareVersions.Count} cloud versions (local firmware kept)";
                }
                else
                {
                    // Select the latest version by default
                    var latestVersion = _firmwareVersions.FirstOrDefault(v => v.IsLatest) ?? _firmwareVersions.FirstOrDefault();
                    if (latestVersion != null)
                    {
                        cmbFirmwareVersion.SelectedItem = latestVersion;
                    }
                    lblStatus.Text = $"Loaded {_firmwareVersions.Count} firmware versions";
                }
                
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
                        lblStatus.Text = "Downloading firmware from Google Drive...";
                        firmwarePath = await _downloadService.DownloadFirmwareAsync(selectedFirmware);
                        
                        var downloadFolder = Path.GetDirectoryName(firmwarePath);
                        lblStatus.Text = $"Downloaded: {selectedFirmware.Version}";
                        
                        // Verify all required files exist
                        var bootloaderPath = Path.Combine(downloadFolder ?? "", "bootloader.bin");
                        var partitionsPath = Path.Combine(downloadFolder ?? "", "partitions.bin");
                        
                        var missingFiles = new List<string>();
                        if (!File.Exists(firmwarePath)) missingFiles.Add("firmware.bin");
                        if (!File.Exists(bootloaderPath)) missingFiles.Add("bootloader.bin");
                        if (!File.Exists(partitionsPath)) missingFiles.Add("partitions.bin");
                        
                        if (missingFiles.Count > 0)
                        {
                            MessageBox.Show(
                                $"Missing required files:\n{string.Join("\n", missingFiles)}\n\n" +
                                $"Flashing requires all 3 files:\n- firmware.bin\n- bootloader.bin\n- partitions.bin\n\n" +
                                $"Check your Google Drive folder and ensure all files are present.",
                                "Missing Files",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
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
            try
            {
                lblStatus.Text = "Loading firmware from cloud sources...";
                btnRefreshFirmware.Enabled = false;
                
                // Remember if we had local firmware selected
                var hadLocalFirmware = !string.IsNullOrEmpty(_localFirmwarePath);
                var localFirmwareName = hadLocalFirmware ? cmbFirmwareVersion.Text : null;
                
                cmbFirmwareVersion.Items.Clear();
                _firmwareVersions.Clear();
                _localFirmwareFolders.Clear();
                
                // Scan local firmware folder for subfolders
                lblStatus.Text = "Scanning local firmware folder...";
                ScanLocalFirmwareFolders();
                
                // Try Google Drive first (faster and doesn't require auth)
                if (_googleDriveService != null)
                {
                    try
                    {
                        lblStatus.Text = "Scanning Google Drive for firmware...";
                        var driveFirmware = await _googleDriveService.ScanFirmwareFoldersAsync();
                        _firmwareVersions.AddRange(driveFirmware);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load firmware from Google Drive");
                        MessageBox.Show(
                            $"Google Drive scan failed:\n{ex.Message}",
                            "Google Drive Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
                
                // Try Firebase/Firestore (optional)
                if (_firestoreService == null)
                {
                    lblStatus.Text = "Connecting to Firebase...";
                    await InitializeFirestoreAsync();
                }
                
                if (_firestoreService != null)
                {
                    try
                    {
                        lblStatus.Text = "Loading firmware from Firestore...";
                        var firestoreFirmware = await _firestoreService.GetFirmwareVersionsAsync();
                        _firmwareVersions.AddRange(firestoreFirmware);
                        _logger.LogInformation($"Loaded {firestoreFirmware.Count} firmware versions from Firestore");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load firmware from Firestore");
                    }
                }
                
                // Add all cloud firmware to dropdown
                foreach (var version in _firmwareVersions)
                {
                    cmbFirmwareVersion.Items.Add(version);
                }
                
                // Restore selection or select latest
                if (hadLocalFirmware && !string.IsNullOrEmpty(localFirmwareName) && _localFirmwareFolders.ContainsKey(localFirmwareName))
                {
                    cmbFirmwareVersion.SelectedItem = localFirmwareName;
                    lblStatus.Text = $"Loaded {_firmwareVersions.Count} cloud versions (local firmware kept)";
                }
                else
                {
                    var latestVersion = _firmwareVersions.FirstOrDefault(v => v.IsLatest) ?? _firmwareVersions.FirstOrDefault();
                    if (latestVersion != null)
                    {
                        cmbFirmwareVersion.SelectedItem = latestVersion;
                    }
                    lblStatus.Text = $"Loaded {_firmwareVersions.Count} firmware versions from cloud";
                }
                
                if (_firmwareVersions.Count == 0 && _localFirmwareFolders.Count == 0)
                {
                    MessageBox.Show(
                        "No firmware found in cloud sources.\n\n" +
                        "Sources checked:\n" +
                        "- Google Drive\n" +
                        "- Firebase (if configured)\n\n" +
                        "You can still use local firmware via 'Browse Folder'.",
                        "No Firmware Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh firmware");
                MessageBox.Show(
                    $"Failed to refresh firmware: {ex.Message}",
                    "Refresh Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                lblStatus.Text = "Failed to refresh firmware";
            }
            finally
            {
                btnRefreshFirmware.Enabled = true;
            }
        }

        private void cmbFirmwareVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFirmwareVersion.SelectedItem is FirmwareVersion selectedFirmware)
            {
                _localFirmwarePath = null;
                
                var isDownloaded = _downloadService.IsFirmwareDownloaded(selectedFirmware);
                var status = isDownloaded ? "✓ Downloaded" : "⬇ Will download";
                lblFirmwareStatus.Text = $"{status} - {selectedFirmware.FileSize / 1024 / 1024:F1} MB";
            }
            else if (cmbFirmwareVersion.SelectedItem is string localDisplay &&
                     _localFirmwareFolders.TryGetValue(localDisplay, out var folder))
            {
                var firmwarePath = Path.Combine(folder, "firmware.bin");
                var bootloaderPath = Path.Combine(folder, "bootloader.bin");
                var partitionsPath = Path.Combine(folder, "partitions.bin");
                
                var hasFirmware = File.Exists(firmwarePath);
                var hasBootloader = File.Exists(bootloaderPath);
                var hasPartitions = File.Exists(partitionsPath);
                
                if (!hasFirmware || !hasBootloader || !hasPartitions)
                {
                    MessageBox.Show(
                        $"Selected folder is missing required files.\n\nRequired: firmware.bin, bootloader.bin, partitions.bin\nFolder: {folder}",
                        "Incomplete Firmware Folder",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    _localFirmwarePath = null;
                    lblFirmwareStatus.Text = "Selected folder is missing required firmware files";
                }
                else
                {
                    _localFirmwarePath = firmwarePath;

                    var status = "✓ Complete flash (bootloader + partitions + app)";

                    lblFirmwareStatus.Text = status;
                    lblStatus.Text = $"Local firmware loaded: {Path.GetFileName(folder)}";

                    _logger.LogInformation($"Local firmware folder: {folder}");
                    _logger.LogInformation($"Firmware: {hasFirmware}, Bootloader: {hasBootloader}, Partitions: {hasPartitions}");
                }
            }

            UpdateFlashButtonState();
        }

        private void listBoxDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFlashButtonState();
        }

        private void ScanLocalFirmwareFolders()
        {
            if (!Directory.Exists(_firmwareFolder))
            {
                return;
            }

            try
            {
                var subfolders = Directory.GetDirectories(_firmwareFolder);
                
                foreach (var subfolder in subfolders)
                {
                    var folderName = Path.GetFileName(subfolder);
                    var firmwarePath = Path.Combine(subfolder, "firmware.bin");
                    var bootloaderPath = Path.Combine(subfolder, "bootloader.bin");
                    var partitionsPath = Path.Combine(subfolder, "partitions.bin");
                    
                    // Check if this subfolder contains all required files
                    if (File.Exists(firmwarePath) && File.Exists(bootloaderPath) && File.Exists(partitionsPath))
                    {
                        var displayName = $"📁 Local: {folderName}";
                        _localFirmwareFolders[displayName] = subfolder;
                        cmbFirmwareVersion.Items.Add(displayName);
                    }
                }
                
                if (_localFirmwareFolders.Count > 0)
                {
                    _logger.LogInformation($"Found {_localFirmwareFolders.Count} local firmware folders");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan local firmware folders");
            }
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
                
                lblStatus.Text = $"Firmware folder changed to: {Path.GetFileName(_firmwareFolder)}";
                
                // Auto-refresh to scan new folder
                btnRefreshFirmware_Click(sender, e);
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

        private void btnBrowseLocal_Click(object sender, EventArgs e)
        {
            var initialPath = _lastFirmwareFolder;
            if (string.IsNullOrEmpty(initialPath))
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var defaultRoot = Path.Combine(documents, "esp-flasher", "firmware");

                try
                {
                    if (!Directory.Exists(defaultRoot))
                    {
                        Directory.CreateDirectory(defaultRoot);
                    }
                }
                catch
                {
                }

                initialPath = Directory.Exists(defaultRoot)
                    ? defaultRoot
                    : documents;
            }

            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select folder containing firmware files (bootloader.bin, partitions.bin, firmware.bin)",
                ShowNewFolderButton = false,
                SelectedPath = initialPath
            };

            if (folderDialog.ShowDialog() == DialogResult.Yes)
            {
                var folder = folderDialog.SelectedPath;
                LoadFirmwareFromFolder(folder, showConfirmation: true);
                
                // Save this folder for next time
                _lastFirmwareFolder = folder;
                SaveSettings();
            }
        }
        
        private void LoadFirmwareFromFolder(string folder, bool showConfirmation = false)
        {
            var firmwarePath = Path.Combine(folder, "firmware.bin");
            var bootloaderPath = Path.Combine(folder, "bootloader.bin");
            var partitionsPath = Path.Combine(folder, "partitions.bin");
            
            bool hasFirmware = File.Exists(firmwarePath);
            bool hasBootloader = File.Exists(bootloaderPath);
            bool hasPartitions = File.Exists(partitionsPath);
            
            if (!hasFirmware || !hasBootloader || !hasPartitions)
            {
                MessageBox.Show(
                    $"Selected folder is missing required files.\n\nRequired: firmware.bin, bootloader.bin, partitions.bin\nFolder: {folder}",
                    "Incomplete Firmware Folder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            
            _localFirmwarePath = firmwarePath;
            var folderName = Path.GetFileName(folder);
            var displayName = folderName;
            
            _localFirmwareFolders[displayName] = folder;

            if (!cmbFirmwareVersion.Items.Contains(displayName))
            {
                cmbFirmwareVersion.Items.Insert(0, displayName);
            }

            cmbFirmwareVersion.SelectedItem = displayName;
            
            var status = hasBootloader && hasPartitions 
                ? "✓ Complete flash (bootloader + partitions + app)"
                : "⚠ App only (bootloader/partitions not found)";
                
            lblFirmwareStatus.Text = status;
            lblStatus.Text = $"Local firmware loaded: {folderName}";
            
            _logger.LogInformation($"Local firmware folder: {folder}");
            _logger.LogInformation($"Firmware: {hasFirmware}, Bootloader: {hasBootloader}, Partitions: {hasPartitions}");
            
            UpdateFlashButtonState();

            if (showConfirmation)
            {
                MessageBox.Show(
                    $"Firmware folder selected successfully.\n\nName: {folderName}\nPath: {folder}",
                    "Firmware Folder Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        
        private void DiscoverLocalFirmwareFolders()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var firmwareRoot = Path.Combine(baseDir, "firmware");
                if (Directory.Exists(firmwareRoot))
                {
                    roots.Add(firmwareRoot);
                }

                try
                {
                    var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var documentsFirmware = Path.Combine(documents, "esp-flasher", "firmware");
                    if (Directory.Exists(documentsFirmware))
                    {
                        roots.Add(documentsFirmware);
                    }
                }
                catch
                {
                }

                try
                {
                    var projectDir = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName;
                    if (!string.IsNullOrEmpty(projectDir))
                    {
                        var projectFirmware = Path.Combine(projectDir, "firmware");
                        if (Directory.Exists(projectFirmware))
                        {
                            roots.Add(projectFirmware);
                        }
                    }
                }
                catch
                {
                }

                foreach (var root in roots)
                {
                    foreach (var folder in Directory.GetDirectories(root))
                    {
                        var firmwarePath = Path.Combine(folder, "firmware.bin");
                        var bootloaderPath = Path.Combine(folder, "bootloader.bin");
                        var partitionsPath = Path.Combine(folder, "partitions.bin");

                        if (!File.Exists(firmwarePath) ||
                            !File.Exists(bootloaderPath) ||
                            !File.Exists(partitionsPath))
                        {
                            continue;
                        }

                        var folderName = Path.GetFileName(folder);
                        var displayName = folderName;

                        _localFirmwareFolders[displayName] = folder;

                        if (!cmbFirmwareVersion.Items.Contains(displayName))
                        {
                            cmbFirmwareVersion.Items.Add(displayName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover local firmware folders");
            }
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
