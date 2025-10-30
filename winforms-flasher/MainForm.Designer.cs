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
            this.groupBoxFirmware = new GroupBox();
            this.lblFirmwareStatus = new Label();
            this.btnBrowseLocal = new Button();
            this.btnRefreshFirmware = new Button();
            this.cmbFirmwareVersion = new ComboBox();
            this.lblFirmwareVersion = new Label();
            this.groupBoxDevices = new GroupBox();
            this.btnRefreshDevices = new Button();
            this.listBoxDevices = new ListBox();
            this.lblDevices = new Label();
            this.groupBoxFlash = new GroupBox();
            this.progressBarFlash = new ProgressBar();
            this.btnFlash = new Button();
            this.statusStrip = new StatusStrip();
            this.lblStatus = new ToolStripStatusLabel();
            this.menuStrip = new MenuStrip();
            this.fileToolStripMenuItem = new ToolStripMenuItem();
            this.clearCacheToolStripMenuItem = new ToolStripMenuItem();
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.exitToolStripMenuItem = new ToolStripMenuItem();
            this.helpToolStripMenuItem = new ToolStripMenuItem();
            this.aboutToolStripMenuItem = new ToolStripMenuItem();
            this.groupBoxFirmware.SuspendLayout();
            this.groupBoxDevices.SuspendLayout();
            this.groupBoxFlash.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxFirmware
            // 
            this.groupBoxFirmware.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.groupBoxFirmware.Controls.Add(this.lblFirmwareStatus);
            this.groupBoxFirmware.Controls.Add(this.btnBrowseLocal);
            this.groupBoxFirmware.Controls.Add(this.btnRefreshFirmware);
            this.groupBoxFirmware.Controls.Add(this.cmbFirmwareVersion);
            this.groupBoxFirmware.Controls.Add(this.lblFirmwareVersion);
            this.groupBoxFirmware.Location = new Point(12, 35);
            this.groupBoxFirmware.Name = "groupBoxFirmware";
            this.groupBoxFirmware.Size = new Size(560, 100);
            this.groupBoxFirmware.TabIndex = 0;
            this.groupBoxFirmware.TabStop = false;
            this.groupBoxFirmware.Text = "Firmware Selection";
            // 
            // lblFirmwareStatus
            // 
            this.lblFirmwareStatus.AutoSize = true;
            this.lblFirmwareStatus.ForeColor = SystemColors.GrayText;
            this.lblFirmwareStatus.Location = new Point(15, 70);
            this.lblFirmwareStatus.Name = "lblFirmwareStatus";
            this.lblFirmwareStatus.Size = new Size(0, 15);
            this.lblFirmwareStatus.TabIndex = 3;
            // 
            // btnBrowseLocal
            // 
            this.btnBrowseLocal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnBrowseLocal.Location = new Point(340, 40);
            this.btnBrowseLocal.Name = "btnBrowseLocal";
            this.btnBrowseLocal.Size = new Size(120, 25);
            this.btnBrowseLocal.TabIndex = 4;
            this.btnBrowseLocal.Text = "📁 Browse Folder...";
            this.btnBrowseLocal.UseVisualStyleBackColor = true;
            this.btnBrowseLocal.Click += this.btnBrowseLocal_Click;
            // 
            // btnRefreshFirmware
            // 
            this.btnRefreshFirmware.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnRefreshFirmware.Location = new Point(470, 40);
            this.btnRefreshFirmware.Name = "btnRefreshFirmware";
            this.btnRefreshFirmware.Size = new Size(75, 25);
            this.btnRefreshFirmware.TabIndex = 2;
            this.btnRefreshFirmware.Text = "Refresh";
            this.btnRefreshFirmware.UseVisualStyleBackColor = true;
            this.btnRefreshFirmware.Click += this.btnRefreshFirmware_Click;
            // 
            // cmbFirmwareVersion
            // 
            this.cmbFirmwareVersion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.cmbFirmwareVersion.DisplayMember = "DisplayText";
            this.cmbFirmwareVersion.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbFirmwareVersion.FormattingEnabled = true;
            this.cmbFirmwareVersion.Location = new Point(15, 40);
            this.cmbFirmwareVersion.Name = "cmbFirmwareVersion";
            this.cmbFirmwareVersion.Size = new Size(440, 23);
            this.cmbFirmwareVersion.TabIndex = 1;
            this.cmbFirmwareVersion.SelectedIndexChanged += this.cmbFirmwareVersion_SelectedIndexChanged;
            // 
            // lblFirmwareVersion
            // 
            this.lblFirmwareVersion.AutoSize = true;
            this.lblFirmwareVersion.Location = new Point(15, 20);
            this.lblFirmwareVersion.Name = "lblFirmwareVersion";
            this.lblFirmwareVersion.Size = new Size(102, 15);
            this.lblFirmwareVersion.TabIndex = 0;
            this.lblFirmwareVersion.Text = "Firmware Version:";
            // 
            // groupBoxDevices
            // 
            this.groupBoxDevices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.groupBoxDevices.Controls.Add(this.btnRefreshDevices);
            this.groupBoxDevices.Controls.Add(this.listBoxDevices);
            this.groupBoxDevices.Controls.Add(this.lblDevices);
            this.groupBoxDevices.Location = new Point(12, 150);
            this.groupBoxDevices.Name = "groupBoxDevices";
            this.groupBoxDevices.Size = new Size(560, 200);
            this.groupBoxDevices.TabIndex = 1;
            this.groupBoxDevices.TabStop = false;
            this.groupBoxDevices.Text = "ESP Devices";
            // 
            // btnRefreshDevices
            // 
            this.btnRefreshDevices.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnRefreshDevices.Location = new Point(470, 20);
            this.btnRefreshDevices.Name = "btnRefreshDevices";
            this.btnRefreshDevices.Size = new Size(75, 25);
            this.btnRefreshDevices.TabIndex = 2;
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
            this.listBoxDevices.Location = new Point(15, 50);
            this.listBoxDevices.Name = "listBoxDevices";
            this.listBoxDevices.Size = new Size(530, 139);
            this.listBoxDevices.TabIndex = 1;
            this.listBoxDevices.SelectedIndexChanged += this.listBoxDevices_SelectedIndexChanged;
            // 
            // lblDevices
            // 
            this.lblDevices.AutoSize = true;
            this.lblDevices.Location = new Point(15, 25);
            this.lblDevices.Name = "lblDevices";
            this.lblDevices.Size = new Size(130, 15);
            this.lblDevices.TabIndex = 0;
            this.lblDevices.Text = "Connected ESP Devices:";
            // 
            // groupBoxFlash
            // 
            this.groupBoxFlash.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.groupBoxFlash.Controls.Add(this.progressBarFlash);
            this.groupBoxFlash.Controls.Add(this.btnFlash);
            this.groupBoxFlash.Location = new Point(12, 365);
            this.groupBoxFlash.Name = "groupBoxFlash";
            this.groupBoxFlash.Size = new Size(560, 80);
            this.groupBoxFlash.TabIndex = 2;
            this.groupBoxFlash.TabStop = false;
            this.groupBoxFlash.Text = "Flash Operation";
            // 
            // progressBarFlash
            // 
            this.progressBarFlash.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.progressBarFlash.Location = new Point(15, 50);
            this.progressBarFlash.Name = "progressBarFlash";
            this.progressBarFlash.Size = new Size(530, 20);
            this.progressBarFlash.TabIndex = 1;
            this.progressBarFlash.Visible = false;
            // 
            // btnFlash
            // 
            this.btnFlash.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.btnFlash.BackColor = Color.FromArgb(0, 120, 215);
            this.btnFlash.FlatStyle = FlatStyle.Flat;
            this.btnFlash.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnFlash.ForeColor = Color.White;
            this.btnFlash.Location = new Point(15, 20);
            this.btnFlash.Name = "btnFlash";
            this.btnFlash.Size = new Size(530, 30);
            this.btnFlash.TabIndex = 0;
            this.btnFlash.Text = "Flash Firmware";
            this.btnFlash.UseVisualStyleBackColor = false;
            this.btnFlash.Click += this.btnFlash_Click;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new ToolStripItem[] { this.lblStatus });
            this.statusStrip.Location = new Point(0, 456);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new Size(584, 22);
            this.statusStrip.TabIndex = 3;
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
            this.menuStrip.TabIndex = 4;
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
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.groupBoxFlash);
            this.Controls.Add(this.groupBoxDevices);
            this.Controls.Add(this.groupBoxFirmware);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new Size(600, 500);
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "ESP Datalogger Flasher";
            this.Load += this.MainForm_Load;
            this.groupBoxFirmware.ResumeLayout(false);
            this.groupBoxFirmware.PerformLayout();
            this.groupBoxDevices.ResumeLayout(false);
            this.groupBoxDevices.PerformLayout();
            this.groupBoxFlash.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private GroupBox groupBoxFirmware;
        private Label lblFirmwareVersion;
        private ComboBox cmbFirmwareVersion;
        private Button btnBrowseLocal;
        private Button btnRefreshFirmware;
        private Label lblFirmwareStatus;
        private GroupBox groupBoxDevices;
        private Label lblDevices;
        private ListBox listBoxDevices;
        private Button btnRefreshDevices;
        private GroupBox groupBoxFlash;
        private Button btnFlash;
        private ProgressBar progressBarFlash;
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
