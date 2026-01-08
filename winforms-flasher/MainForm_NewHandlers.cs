using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESPFlasher.Models;

namespace ESPFlasher
{
    public partial class MainForm
    {
        private void btnScanLocal_Click(object sender, EventArgs e)
        {
            try
            {
                lblStatus.Text = "Scanning local firmware folder...";
                btnScanLocal.Enabled = false;
                
                var localItems = _libraryService.ScanLocalFirmwares();
                
                _allFirmwares = _libraryService.MergeAndSort(localItems, new List<FirmwareItem>());
                
                PopulateFirmwareListView();
                
                lblStatus.Text = $"Found {localItems.Count} local firmware(s)";
                _logger.LogInformation($"Scanned local: {localItems.Count} firmware(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan local firmwares");
                MessageBox.Show($"Failed to scan local firmwares:\n{ex.Message}", "Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Local scan failed";
            }
            finally
            {
                btnScanLocal.Enabled = true;
            }
        }
        
        private async void btnScanCloud_Click(object sender, EventArgs e)
        {
            if (_googleDriveService == null)
            {
                MessageBox.Show("Google Drive service is not available.", "Service Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                lblStatus.Text = "Scanning Google Drive for firmware...";
                btnScanCloud.Enabled = false;
                
                var cloudFirmwares = await _googleDriveService.ScanFirmwareFoldersAsync();
                
                var localItems = _libraryService.ScanLocalFirmwares();
                var cloudItems = _libraryService.ConvertCloudFirmwaresToItems(cloudFirmwares, localItems);
                
                _allFirmwares = _libraryService.MergeAndSort(localItems, cloudItems);
                
                PopulateFirmwareListView();
                
                lblStatus.Text = $"Found {localItems.Count} local, {cloudItems.Count} cloud firmware(s)";
                _logger.LogInformation($"Scanned cloud: {cloudFirmwares.Count} firmware(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan cloud firmwares");
                MessageBox.Show($"Failed to scan Google Drive:\n{ex.Message}", "Cloud Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Cloud scan failed";
            }
            finally
            {
                btnScanCloud.Enabled = true;
            }
        }
        
        private void PopulateFirmwareListView()
        {
            listViewFirmwares.Items.Clear();
            
            foreach (var firmware in _allFirmwares)
            {
                var item = new ListViewItem(firmware.ListViewText);
                item.SubItems.Add(firmware.DateText);
                item.Tag = firmware;
                
                if (firmware.Status == FirmwareStatus.Downloaded)
                {
                    item.ForeColor = System.Drawing.Color.Black;
                }
                else if (firmware.Status == FirmwareStatus.CloudOnly)
                {
                    item.ForeColor = System.Drawing.Color.Gray;
                }
                
                listViewFirmwares.Items.Add(item);
            }
            
            if (_selectedFirmware != null)
            {
                var matchingItem = listViewFirmwares.Items.Cast<ListViewItem>()
                    .FirstOrDefault(i => (i.Tag as FirmwareItem)?.Version == _selectedFirmware.Version);
                
                if (matchingItem != null)
                {
                    matchingItem.Selected = true;
                    matchingItem.EnsureVisible();
                }
            }
        }
        
        private void listViewFirmwares_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewFirmwares.SelectedItems.Count == 0)
            {
                _selectedFirmware = null;
                UpdateSelectedFirmwarePanel();
                UpdateFlashButtonState();
                return;
            }
            
            var selectedItem = listViewFirmwares.SelectedItems[0];
            _selectedFirmware = selectedItem.Tag as FirmwareItem;
            
            UpdateSelectedFirmwarePanel();
            UpdateFlashButtonState();
        }
        
        private void UpdateSelectedFirmwarePanel()
        {
            if (_selectedFirmware == null)
            {
                lblSelectedFirmwareName.Text = "No selection";
                lblSelectedFirmwareDate.Text = "";
                lblSelectedFirmwareStatus.Text = "";
                btnDownloadFirmware.Visible = false;
                return;
            }
            
            lblSelectedFirmwareName.Text = _selectedFirmware.DisplayName;
            lblSelectedFirmwareDate.Text = $"Date: {_selectedFirmware.DateText} | Source: {_selectedFirmware.SourceText}";
            
            if (_selectedFirmware.Status == FirmwareStatus.Downloaded)
            {
                lblSelectedFirmwareStatus.Text = "✓ Ready to flash";
                lblSelectedFirmwareStatus.ForeColor = System.Drawing.Color.Green;
                btnDownloadFirmware.Visible = false;
            }
            else if (_selectedFirmware.Status == FirmwareStatus.CloudOnly)
            {
                lblSelectedFirmwareStatus.Text = "⬇ Not downloaded yet";
                lblSelectedFirmwareStatus.ForeColor = System.Drawing.Color.Orange;
                btnDownloadFirmware.Visible = true;
            }
            else
            {
                lblSelectedFirmwareStatus.Text = "⚠ Incomplete";
                lblSelectedFirmwareStatus.ForeColor = System.Drawing.Color.Red;
                btnDownloadFirmware.Visible = false;
            }
        }
        
        private async void btnDownloadFirmware_Click(object sender, EventArgs e)
        {
            if (_selectedFirmware == null || _selectedFirmware.CloudMetadata == null)
            {
                return;
            }
            
            try
            {
                btnDownloadFirmware.Enabled = false;
                btnDownloadFirmware.Text = "Downloading...";
                progressBarFlash.Visible = true;
                progressBarFlash.Value = 0;
                
                lblStatus.Text = $"Downloading {_selectedFirmware.DisplayName}...";
                
                var firmwarePath = await _downloadService.DownloadFirmwareAsync(_selectedFirmware.CloudMetadata);
                
                lblStatus.Text = $"Downloaded {_selectedFirmware.DisplayName} successfully";
                
                MessageBox.Show(
                    $"Firmware downloaded successfully!\n\nVersion: {_selectedFirmware.DisplayName}\nPath: {firmwarePath}",
                    "Download Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                
                btnScanLocal_Click(sender, e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download firmware");
                MessageBox.Show($"Failed to download firmware:\n{ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Download failed";
            }
            finally
            {
                btnDownloadFirmware.Enabled = true;
                btnDownloadFirmware.Text = "⬇ Download";
                progressBarFlash.Visible = false;
            }
        }
        
        private new void UpdateFlashButtonState()
        {
            bool hasFirmware = _selectedFirmware != null && _selectedFirmware.Status == FirmwareStatus.Downloaded;
            bool hasDevice = listBoxDevices.SelectedItem != null;
            bool hasSharedFiles = _libraryService.HasSharedFiles();
            bool notFlashing = _flashCancellationTokenSource == null;
            
            btnFlash.Enabled = hasFirmware && hasDevice && hasSharedFiles && notFlashing;
            
            if (!hasSharedFiles && hasFirmware)
            {
                lblSelectedFirmwareStatus.Text = "⚠ Missing shared bootloader/partitions";
                lblSelectedFirmwareStatus.ForeColor = System.Drawing.Color.Red;
            }
        }
    }
}
