using System;
using System.Drawing;
using System.Windows.Forms;

namespace ESPFlasher
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl = new TabControl();
            this.tabPageFlash = new TabPage();
            this.splitContainerFlash = new SplitContainer();
            this.groupBoxFirmwareLibrary = new GroupBox();
            this.listViewFirmwares = new ListView();
            this.panelFirmwareActions = new Panel();
            this.btnScanCloud = new Button();
            this.btnScanLocal = new Button();
            this.panelFirmwareFolder = new Panel();
            this.lblFirmwareFolder = new Label();
            this.txtFirmwareFolder = new TextBox();
            this.btnBrowseFirmwareFolder = new Button();
            this.btnOpenFirmwareFolder = new Button();
            this.groupBoxFlashOperation = new GroupBox();
            this.panelSelectedFirmware = new Panel();
            this.lblSelectedFirmwareTitle = new Label();
            this.lblSelectedFirmwareName = new Label();
            this.lblSelectedFirmwareDate = new Label();
            this.lblSelectedFirmwareStatus = new Label();
            this.btnDownloadFirmware = new Button();
            this.groupBoxDevices = new GroupBox();
            this.listBoxDevices = new ListBox();
            this.btnRefreshDevices = new Button();
            this.groupBoxFlash = new GroupBox();
            this.progressBarFlash = new ProgressBar();
            this.btnFlash = new Button();
            this.tabPageMonitor = new TabPage();
            this.groupBoxMonitorOutput = new GroupBox();
            this.txtMonitorOutput = new RichTextBox();
            this.groupBoxMonitorControl = new GroupBox();
            this.btnClearMonitor = new Button();
            this.btnStartStopMonitor = new Button();
            this.cmbBaudRate = new ComboBox();
            this.lblBaudRate = new Label();
            this.cmbMonitorPort = new ComboBox();
            this.lblMonitorPort = new Label();
            this.statusStrip = new StatusStrip();
            this.lblStatus = new ToolStripStatusLabel();
            this.menuStrip = new MenuStrip();
            this.fileToolStripMenuItem = new ToolStripMenuItem();
            this.clearCacheToolStripMenuItem = new ToolStripMenuItem();
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.exitToolStripMenuItem = new ToolStripMenuItem();
            this.helpToolStripMenuItem = new ToolStripMenuItem();
            this.aboutToolStripMenuItem = new ToolStripMenuItem();
            this.tabControl.SuspendLayout();
            this.tabPageFlash.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerFlash)).BeginInit();
            this.splitContainerFlash.Panel1.SuspendLayout();
            this.splitContainerFlash.Panel2.SuspendLayout();
            this.splitContainerFlash.SuspendLayout();
            this.groupBoxFirmwareLibrary.SuspendLayout();
            this.panelFirmwareActions.SuspendLayout();
            this.panelFirmwareFolder.SuspendLayout();
            this.groupBoxFlashOperation.SuspendLayout();
            this.panelSelectedFirmware.SuspendLayout();
            this.groupBoxDevices.SuspendLayout();
            this.groupBoxFlash.SuspendLayout();
            this.tabPageMonitor.SuspendLayout();
            this.groupBoxMonitorOutput.SuspendLayout();
            this.groupBoxMonitorControl.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.tabControl.Controls.Add(this.tabPageFlash);
            this.tabControl.Controls.Add(this.tabPageMonitor);
            this.tabControl.Location = new Point(12, 27);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new Size(560, 418);
            this.tabControl.TabIndex = 0;
            // 
            // tabPageFlash
            // 
            this.tabPageFlash.Controls.Add(this.splitContainerFlash);
            this.tabPageFlash.Location = new Point(4, 24);
            this.tabPageFlash.Name = "tabPageFlash";
            this.tabPageFlash.Padding = new Padding(3);
            this.tabPageFlash.Size = new Size(552, 390);
            this.tabPageFlash.TabIndex = 0;
            this.tabPageFlash.Text = "Flash Firmware";
            this.tabPageFlash.UseVisualStyleBackColor = true;
            // 
            // splitContainerFlash
            // 
            this.splitContainerFlash.Dock = DockStyle.Fill;
            this.splitContainerFlash.Location = new Point(3, 3);
            this.splitContainerFlash.Name = "splitContainerFlash";
            this.splitContainerFlash.Panel1.Controls.Add(this.groupBoxFirmwareLibrary);
            this.splitContainerFlash.Panel2.Controls.Add(this.groupBoxFlashOperation);
            this.splitContainerFlash.Size = new Size(546, 384);
            this.splitContainerFlash.SplitterDistance = 240;
            this.splitContainerFlash.TabIndex = 0;
            // 
            // groupBoxFirmwareLibrary
            // 
            this.groupBoxFirmwareLibrary.Dock = DockStyle.Fill;
            this.groupBoxFirmwareLibrary.Controls.Add(this.listViewFirmwares);
            this.groupBoxFirmwareLibrary.Controls.Add(this.panelFirmwareActions);
            this.groupBoxFirmwareLibrary.Controls.Add(this.panelFirmwareFolder);
            this.groupBoxFirmwareLibrary.Location = new Point(0, 0);
            this.groupBoxFirmwareLibrary.Name = "groupBoxFirmwareLibrary";
            this.groupBoxFirmwareLibrary.Padding = new Padding(8);
            this.groupBoxFirmwareLibrary.Size = new Size(240, 384);
            this.groupBoxFirmwareLibrary.TabIndex = 0;
            this.groupBoxFirmwareLibrary.TabStop = false;
            this.groupBoxFirmwareLibrary.Text = "Firmware Library";
            // 
            // listViewFirmwares
            // 
            this.listViewFirmwares.Dock = DockStyle.Fill;
            this.listViewFirmwares.FullRowSelect = true;
            this.listViewFirmwares.HideSelection = false;
            this.listViewFirmwares.Location = new Point(8, 64);
            this.listViewFirmwares.MultiSelect = false;
            this.listViewFirmwares.Name = "listViewFirmwares";
            this.listViewFirmwares.Size = new Size(224, 242);
            this.listViewFirmwares.TabIndex = 0;
            this.listViewFirmwares.UseCompatibleStateImageBehavior = false;
            this.listViewFirmwares.View = View.Details;
            this.listViewFirmwares.Columns.Add("Firmware", 140);
            this.listViewFirmwares.Columns.Add("Date", 80);
            this.listViewFirmwares.SelectedIndexChanged += this.listViewFirmwares_SelectedIndexChanged;
            // 
            // panelFirmwareActions
            // 
            this.panelFirmwareActions.Dock = DockStyle.Top;
            this.panelFirmwareActions.Controls.Add(this.btnScanCloud);
            this.panelFirmwareActions.Controls.Add(this.btnScanLocal);
            this.panelFirmwareActions.Location = new Point(8, 24);
            this.panelFirmwareActions.Name = "panelFirmwareActions";
            this.panelFirmwareActions.Size = new Size(224, 40);
            this.panelFirmwareActions.TabIndex = 1;
            // 
            // btnScanCloud
            // 
            this.btnScanCloud.Location = new Point(118, 5);
            this.btnScanCloud.Name = "btnScanCloud";
            this.btnScanCloud.Size = new Size(100, 28);
            this.btnScanCloud.TabIndex = 1;
            this.btnScanCloud.Text = "☁ Scan Cloud";
            this.btnScanCloud.UseVisualStyleBackColor = true;
            this.btnScanCloud.Click += this.btnScanCloud_Click;
            // 
            // btnScanLocal
            // 
            this.btnScanLocal.Location = new Point(5, 5);
            this.btnScanLocal.Name = "btnScanLocal";
            this.btnScanLocal.Size = new Size(100, 28);
            this.btnScanLocal.TabIndex = 0;
            this.btnScanLocal.Text = "📁 Scan Local";
            this.btnScanLocal.UseVisualStyleBackColor = true;
            this.btnScanLocal.Click += this.btnScanLocal_Click;
            // 
            // panelFirmwareFolder
            // 
            this.panelFirmwareFolder.Dock = DockStyle.Bottom;
            this.panelFirmwareFolder.Controls.Add(this.lblFirmwareFolder);
            this.panelFirmwareFolder.Controls.Add(this.txtFirmwareFolder);
            this.panelFirmwareFolder.Controls.Add(this.btnBrowseFirmwareFolder);
            this.panelFirmwareFolder.Controls.Add(this.btnOpenFirmwareFolder);
            this.panelFirmwareFolder.Location = new Point(8, 306);
            this.panelFirmwareFolder.Name = "panelFirmwareFolder";
            this.panelFirmwareFolder.Size = new Size(224, 70);
            this.panelFirmwareFolder.TabIndex = 2;
            // 
            // lblFirmwareFolder
            // 
            this.lblFirmwareFolder.AutoSize = true;
            this.lblFirmwareFolder.Location = new Point(3, 5);
            this.lblFirmwareFolder.Name = "lblFirmwareFolder";
            this.lblFirmwareFolder.Size = new Size(95, 15);
            this.lblFirmwareFolder.TabIndex = 0;
            this.lblFirmwareFolder.Text = "Firmware Folder:";
            // 
            // txtFirmwareFolder
            // 
            this.txtFirmwareFolder.Location = new Point(3, 23);
            this.txtFirmwareFolder.Name = "txtFirmwareFolder";
            this.txtFirmwareFolder.ReadOnly = true;
            this.txtFirmwareFolder.Size = new Size(218, 23);
            this.txtFirmwareFolder.TabIndex = 1;
            this.txtFirmwareFolder.BackColor = SystemColors.Control;
            // 
            // btnBrowseFirmwareFolder
            // 
            this.btnBrowseFirmwareFolder.Location = new Point(3, 48);
            this.btnBrowseFirmwareFolder.Name = "btnBrowseFirmwareFolder";
            this.btnBrowseFirmwareFolder.Size = new Size(105, 23);
            this.btnBrowseFirmwareFolder.TabIndex = 2;
            this.btnBrowseFirmwareFolder.Text = "Change...";
            this.btnBrowseFirmwareFolder.UseVisualStyleBackColor = true;
            this.btnBrowseFirmwareFolder.Click += this.btnBrowseFirmwareFolder_Click;
            // 
            // btnOpenFirmwareFolder
            // 
            this.btnOpenFirmwareFolder.Location = new Point(116, 48);
            this.btnOpenFirmwareFolder.Name = "btnOpenFirmwareFolder";
            this.btnOpenFirmwareFolder.Size = new Size(105, 23);
            this.btnOpenFirmwareFolder.TabIndex = 3;
            this.btnOpenFirmwareFolder.Text = "📁 Open";
            this.btnOpenFirmwareFolder.UseVisualStyleBackColor = true;
            this.btnOpenFirmwareFolder.Click += this.btnOpenFirmwareFolder_Click;
            // 
            // groupBoxFlashOperation
            // 
            this.groupBoxFlashOperation.Dock = DockStyle.Fill;
            this.groupBoxFlashOperation.Controls.Add(this.panelSelectedFirmware);
            this.groupBoxFlashOperation.Controls.Add(this.groupBoxDevices);
            this.groupBoxFlashOperation.Controls.Add(this.groupBoxFlash);
            this.groupBoxFlashOperation.Location = new Point(0, 0);
            this.groupBoxFlashOperation.Name = "groupBoxFlashOperation";
            this.groupBoxFlashOperation.Padding = new Padding(8);
            this.groupBoxFlashOperation.Size = new Size(302, 384);
            this.groupBoxFlashOperation.TabIndex = 1;
            this.groupBoxFlashOperation.TabStop = false;
            this.groupBoxFlashOperation.Text = "Flash Operation";
            // 
            // panelSelectedFirmware
            // 
            this.panelSelectedFirmware.Dock = DockStyle.Top;
            this.panelSelectedFirmware.Controls.Add(this.lblSelectedFirmwareTitle);
            this.panelSelectedFirmware.Controls.Add(this.lblSelectedFirmwareName);
            this.panelSelectedFirmware.Controls.Add(this.lblSelectedFirmwareDate);
            this.panelSelectedFirmware.Controls.Add(this.lblSelectedFirmwareStatus);
            this.panelSelectedFirmware.Controls.Add(this.btnDownloadFirmware);
            this.panelSelectedFirmware.Location = new Point(8, 24);
            this.panelSelectedFirmware.Name = "panelSelectedFirmware";
            this.panelSelectedFirmware.Size = new Size(286, 100);
            this.panelSelectedFirmware.TabIndex = 0;
            this.panelSelectedFirmware.BorderStyle = BorderStyle.FixedSingle;
            this.panelSelectedFirmware.BackColor = SystemColors.ControlLight;
            // 
            // lblSelectedFirmwareTitle
            // 
            this.lblSelectedFirmwareTitle.AutoSize = true;
            this.lblSelectedFirmwareTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblSelectedFirmwareTitle.Location = new Point(8, 8);
            this.lblSelectedFirmwareTitle.Name = "lblSelectedFirmwareTitle";
            this.lblSelectedFirmwareTitle.Size = new Size(115, 15);
            this.lblSelectedFirmwareTitle.TabIndex = 0;
            this.lblSelectedFirmwareTitle.Text = "Selected Firmware:";
            // 
            // lblSelectedFirmwareName
            // 
            this.lblSelectedFirmwareName.AutoSize = true;
            this.lblSelectedFirmwareName.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblSelectedFirmwareName.Location = new Point(8, 28);
            this.lblSelectedFirmwareName.Name = "lblSelectedFirmwareName";
            this.lblSelectedFirmwareName.Size = new Size(120, 19);
            this.lblSelectedFirmwareName.TabIndex = 1;
            this.lblSelectedFirmwareName.Text = "No selection";
            // 
            // lblSelectedFirmwareDate
            // 
            this.lblSelectedFirmwareDate.AutoSize = true;
            this.lblSelectedFirmwareDate.ForeColor = SystemColors.GrayText;
            this.lblSelectedFirmwareDate.Location = new Point(8, 50);
            this.lblSelectedFirmwareDate.Name = "lblSelectedFirmwareDate";
            this.lblSelectedFirmwareDate.Size = new Size(0, 15);
            this.lblSelectedFirmwareDate.TabIndex = 2;
            // 
            // lblSelectedFirmwareStatus
            // 
            this.lblSelectedFirmwareStatus.AutoSize = true;
            this.lblSelectedFirmwareStatus.Location = new Point(8, 68);
            this.lblSelectedFirmwareStatus.Name = "lblSelectedFirmwareStatus";
            this.lblSelectedFirmwareStatus.Size = new Size(0, 15);
            this.lblSelectedFirmwareStatus.TabIndex = 3;
            // 
            // btnDownloadFirmware
            // 
            this.btnDownloadFirmware.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnDownloadFirmware.BackColor = Color.FromArgb(0, 120, 215);
            this.btnDownloadFirmware.FlatStyle = FlatStyle.Flat;
            this.btnDownloadFirmware.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnDownloadFirmware.ForeColor = Color.White;
            this.btnDownloadFirmware.Location = new Point(170, 25);
            this.btnDownloadFirmware.Name = "btnDownloadFirmware";
            this.btnDownloadFirmware.Size = new Size(105, 28);
            this.btnDownloadFirmware.TabIndex = 4;
            this.btnDownloadFirmware.Text = "⬇ Download";
            this.btnDownloadFirmware.UseVisualStyleBackColor = false;
            this.btnDownloadFirmware.Visible = false;
            this.btnDownloadFirmware.Click += this.btnDownloadFirmware_Click;
            // 
            // groupBoxDevices (moved inside groupBoxFlashOperation)
            // 
            this.groupBoxDevices.Dock = DockStyle.Top;
            this.groupBoxDevices.Controls.Add(this.listBoxDevices);
            this.groupBoxDevices.Controls.Add(this.btnRefreshDevices);
            this.groupBoxDevices.Location = new Point(8, 124);
            this.groupBoxDevices.Name = "groupBoxDevices";
            this.groupBoxDevices.Size = new Size(286, 140);
            this.groupBoxDevices.TabIndex = 1;
            this.groupBoxDevices.TabStop = false;
            this.groupBoxDevices.Text = "Target Device";
            // 
            // btnRefreshDevices
            // 
            this.btnRefreshDevices.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnRefreshDevices.Location = new Point(200, 18);
            this.btnRefreshDevices.Name = "btnRefreshDevices";
            this.btnRefreshDevices.Size = new Size(75, 25);
            this.btnRefreshDevices.TabIndex = 1;
            this.btnRefreshDevices.Text = "Refresh";
            this.btnRefreshDevices.UseVisualStyleBackColor = true;
            this.btnRefreshDevices.Click += this.btnRefreshDevices_Click;
            // 
            // listBoxDevices
            // 
            this.listBoxDevices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.listBoxDevices.DisplayMember = "DisplayText";
            this.listBoxDevices.FormattingEnabled = true;
            this.listBoxDevices.ItemHeight = 15;
            this.listBoxDevices.Location = new Point(10, 48);
            this.listBoxDevices.Name = "listBoxDevices";
            this.listBoxDevices.Size = new Size(265, 79);
            this.listBoxDevices.TabIndex = 0;
            this.listBoxDevices.SelectedIndexChanged += this.listBoxDevices_SelectedIndexChanged;
            // 
            // groupBoxFlash
            // 
            this.groupBoxFlash.Dock = DockStyle.Bottom;
            this.groupBoxFlash.Controls.Add(this.progressBarFlash);
            this.groupBoxFlash.Controls.Add(this.btnFlash);
            this.groupBoxFlash.Location = new Point(8, 264);
            this.groupBoxFlash.Name = "groupBoxFlash";
            this.groupBoxFlash.Size = new Size(286, 112);
            this.groupBoxFlash.TabIndex = 2;
            this.groupBoxFlash.TabStop = false;
            this.groupBoxFlash.Text = "Flash";
            // 
            // progressBarFlash
            // 
            this.progressBarFlash.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.progressBarFlash.Location = new Point(10, 80);
            this.progressBarFlash.Name = "progressBarFlash";
            this.progressBarFlash.Size = new Size(265, 20);
            this.progressBarFlash.TabIndex = 1;
            this.progressBarFlash.Visible = false;
            // 
            // btnFlash
            // 
            this.btnFlash.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.btnFlash.BackColor = Color.FromArgb(0, 120, 215);
            this.btnFlash.FlatStyle = FlatStyle.Flat;
            this.btnFlash.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnFlash.ForeColor = Color.White;
            this.btnFlash.Location = new Point(10, 25);
            this.btnFlash.Name = "btnFlash";
            this.btnFlash.Size = new Size(265, 45);
            this.btnFlash.TabIndex = 0;
            this.btnFlash.Text = "Flash Firmware";
            this.btnFlash.UseVisualStyleBackColor = false;
            this.btnFlash.Click += this.btnFlash_Click;
            // 
            // tabPageMonitor
            // 
            this.tabPageMonitor.Controls.Add(this.groupBoxMonitorOutput);
            this.tabPageMonitor.Controls.Add(this.groupBoxMonitorControl);
            this.tabPageMonitor.Location = new Point(4, 24);
            this.tabPageMonitor.Name = "tabPageMonitor";
            this.tabPageMonitor.Padding = new Padding(3);
            this.tabPageMonitor.Size = new Size(552, 390);
            this.tabPageMonitor.TabIndex = 1;
            this.tabPageMonitor.Text = "Monitor";
            this.tabPageMonitor.UseVisualStyleBackColor = true;
            // 
            // groupBoxMonitorOutput
            // 
            this.groupBoxMonitorOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.groupBoxMonitorOutput.Controls.Add(this.txtMonitorOutput);
            this.groupBoxMonitorOutput.Location = new Point(6, 86);
            this.groupBoxMonitorOutput.Name = "groupBoxMonitorOutput";
            this.groupBoxMonitorOutput.Size = new Size(540, 298);
            this.groupBoxMonitorOutput.TabIndex = 1;
            this.groupBoxMonitorOutput.TabStop = false;
            this.groupBoxMonitorOutput.Text = "Serial Output";
            // 
            // txtMonitorOutput
            // 
            this.txtMonitorOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.txtMonitorOutput.BackColor = Color.Black;
            this.txtMonitorOutput.Font = new Font("Consolas", 9F);
            this.txtMonitorOutput.ForeColor = Color.LimeGreen;
            this.txtMonitorOutput.Location = new Point(6, 20);
            this.txtMonitorOutput.Name = "txtMonitorOutput";
            this.txtMonitorOutput.ReadOnly = true;
            this.txtMonitorOutput.Size = new Size(528, 272);
            this.txtMonitorOutput.TabIndex = 0;
            this.txtMonitorOutput.Text = "";
            this.txtMonitorOutput.WordWrap = false;
            // 
            // groupBoxMonitorControl
            // 
            this.groupBoxMonitorControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.groupBoxMonitorControl.Controls.Add(this.btnClearMonitor);
            this.groupBoxMonitorControl.Controls.Add(this.btnStartStopMonitor);
            this.groupBoxMonitorControl.Controls.Add(this.cmbBaudRate);
            this.groupBoxMonitorControl.Controls.Add(this.lblBaudRate);
            this.groupBoxMonitorControl.Controls.Add(this.cmbMonitorPort);
            this.groupBoxMonitorControl.Controls.Add(this.lblMonitorPort);
            this.groupBoxMonitorControl.Location = new Point(6, 6);
            this.groupBoxMonitorControl.Name = "groupBoxMonitorControl";
            this.groupBoxMonitorControl.Size = new Size(540, 74);
            this.groupBoxMonitorControl.TabIndex = 0;
            this.groupBoxMonitorControl.TabStop = false;
            this.groupBoxMonitorControl.Text = "Monitor Control";
            // 
            // btnClearMonitor
            // 
            this.btnClearMonitor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnClearMonitor.Location = new Point(459, 40);
            this.btnClearMonitor.Name = "btnClearMonitor";
            this.btnClearMonitor.Size = new Size(75, 25);
            this.btnClearMonitor.TabIndex = 5;
            this.btnClearMonitor.Text = "Clear";
            this.btnClearMonitor.UseVisualStyleBackColor = true;
            this.btnClearMonitor.Click += this.btnClearMonitor_Click;
            // 
            // btnStartStopMonitor
            // 
            this.btnStartStopMonitor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnStartStopMonitor.BackColor = Color.FromArgb(0, 150, 0);
            this.btnStartStopMonitor.FlatStyle = FlatStyle.Flat;
            this.btnStartStopMonitor.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnStartStopMonitor.ForeColor = Color.White;
            this.btnStartStopMonitor.Location = new Point(340, 40);
            this.btnStartStopMonitor.Name = "btnStartStopMonitor";
            this.btnStartStopMonitor.Size = new Size(113, 25);
            this.btnStartStopMonitor.TabIndex = 4;
            this.btnStartStopMonitor.Text = "Start Monitor";
            this.btnStartStopMonitor.UseVisualStyleBackColor = false;
            this.btnStartStopMonitor.Click += this.btnStartStopMonitor_Click;
            // 
            // cmbBaudRate
            // 
            this.cmbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbBaudRate.FormattingEnabled = true;
            this.cmbBaudRate.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" });
            this.cmbBaudRate.Location = new Point(180, 42);
            this.cmbBaudRate.Name = "cmbBaudRate";
            this.cmbBaudRate.Size = new Size(120, 23);
            this.cmbBaudRate.TabIndex = 3;
            // 
            // lblBaudRate
            // 
            this.lblBaudRate.AutoSize = true;
            this.lblBaudRate.Location = new Point(180, 24);
            this.lblBaudRate.Name = "lblBaudRate";
            this.lblBaudRate.Size = new Size(63, 15);
            this.lblBaudRate.TabIndex = 2;
            this.lblBaudRate.Text = "Baud Rate:";
            // 
            // cmbMonitorPort
            // 
            this.cmbMonitorPort.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbMonitorPort.FormattingEnabled = true;
            this.cmbMonitorPort.Location = new Point(15, 42);
            this.cmbMonitorPort.Name = "cmbMonitorPort";
            this.cmbMonitorPort.Size = new Size(150, 23);
            this.cmbMonitorPort.TabIndex = 1;
            // 
            // lblMonitorPort
            // 
            this.lblMonitorPort.AutoSize = true;
            this.lblMonitorPort.Location = new Point(15, 24);
            this.lblMonitorPort.Name = "lblMonitorPort";
            this.lblMonitorPort.Size = new Size(62, 15);
            this.lblMonitorPort.TabIndex = 0;
            this.lblMonitorPort.Text = "COM Port";
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new ToolStripItem[] { this.lblStatus });
            this.statusStrip.Location = new Point(0, 456);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new Size(584, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(39, 17);
            this.lblStatus.Text = "Ready";
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new ToolStripItem[] { this.fileToolStripMenuItem, this.helpToolStripMenuItem });
            this.menuStrip.Location = new Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new Size(584, 24);
            this.menuStrip.TabIndex = 2;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { this.clearCacheToolStripMenuItem, this.toolStripSeparator1, this.exitToolStripMenuItem });
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // clearCacheToolStripMenuItem
            // 
            this.clearCacheToolStripMenuItem.Name = "clearCacheToolStripMenuItem";
            this.clearCacheToolStripMenuItem.Size = new Size(135, 22);
            this.clearCacheToolStripMenuItem.Text = "&Clear Cache";
            this.clearCacheToolStripMenuItem.Click += this.clearCacheToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new Size(132, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new Size(135, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += this.exitToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { this.aboutToolStripMenuItem });
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new Size(107, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += this.aboutToolStripMenuItem_Click;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(584, 478);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new Size(600, 500);
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "ESP Datalogger Flasher";
            this.Load += this.MainForm_Load;
            this.tabControl.ResumeLayout(false);
            this.tabPageFlash.ResumeLayout(false);
            this.splitContainerFlash.Panel1.ResumeLayout(false);
            this.splitContainerFlash.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerFlash)).EndInit();
            this.splitContainerFlash.ResumeLayout(false);
            this.groupBoxFirmwareLibrary.ResumeLayout(false);
            this.panelFirmwareActions.ResumeLayout(false);
            this.panelFirmwareFolder.ResumeLayout(false);
            this.panelFirmwareFolder.PerformLayout();
            this.groupBoxFlashOperation.ResumeLayout(false);
            this.panelSelectedFirmware.ResumeLayout(false);
            this.panelSelectedFirmware.PerformLayout();
            this.groupBoxDevices.ResumeLayout(false);
            this.groupBoxFlash.ResumeLayout(false);
            this.tabPageMonitor.ResumeLayout(false);
            this.groupBoxMonitorOutput.ResumeLayout(false);
            this.groupBoxMonitorControl.ResumeLayout(false);
            this.groupBoxMonitorControl.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private TabControl tabControl;
        private TabPage tabPageFlash;
        private SplitContainer splitContainerFlash;
        private GroupBox groupBoxFirmwareLibrary;
        private ListView listViewFirmwares;
        private Panel panelFirmwareActions;
        private Button btnScanCloud;
        private Button btnScanLocal;
        private Panel panelFirmwareFolder;
        private GroupBox groupBoxFlashOperation;
        private Panel panelSelectedFirmware;
        private Label lblSelectedFirmwareTitle;
        private Label lblSelectedFirmwareName;
        private Label lblSelectedFirmwareDate;
        private Label lblSelectedFirmwareStatus;
        private Button btnDownloadFirmware;
        private GroupBox groupBoxDevices;
        private ListBox listBoxDevices;
        private Button btnRefreshDevices;
        private GroupBox groupBoxFlash;
        private Button btnFlash;
        private ProgressBar progressBarFlash;
        private TabPage tabPageMonitor;
        private GroupBox groupBoxMonitorControl;
        private Label lblMonitorPort;
        private ComboBox cmbMonitorPort;
        private Label lblBaudRate;
        private ComboBox cmbBaudRate;
        private Button btnStartStopMonitor;
        private Button btnClearMonitor;
        private GroupBox groupBoxMonitorOutput;
        private RichTextBox txtMonitorOutput;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem clearCacheToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;

        private void clearCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "This will delete all downloaded firmware files. Are you sure?",
                "Clear Cache",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _downloadService.ClearCache();
                MessageBox.Show("Cache cleared successfully.", "Cache Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnScanLocal_Click(sender, e);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "ESP Datalogger Flasher v1.0\n\n" +
                "A Windows Forms application for flashing ESP32 firmware\n" +
                "with Firestore integration and automatic device detection.\n\n" +
                "Built with .NET 8 and Windows Forms",
                "About ESP Datalogger Flasher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
