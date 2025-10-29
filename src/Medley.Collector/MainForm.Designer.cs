
namespace Medley.Collector
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            menuStrip1 = new MenuStrip();
            aPIKeysToolStripMenuItem = new ToolStripMenuItem();
            googleAuthToolStripMenuItem = new ToolStripMenuItem();
            downloadToolStripMenuItem = new ToolStripMenuItem();
            downloadGoogleDriveToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            refreshToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            viewActiveItemsToolStripMenuItem = new ToolStripMenuItem();
            viewArchivedItemsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            archiveSelectedToolStripMenuItem = new ToolStripMenuItem();
            archiveExcludedToolStripMenuItem = new ToolStripMenuItem();
            archiveExportedToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            restoreSelectedToolStripMenuItem = new ToolStripMenuItem();
            restoreAllToolStripMenuItem = new ToolStripMenuItem();
            exportSelectedToolStripMenuItem = new ToolStripMenuItem();
            deleteAllToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            dataGridViewTranscripts = new Zuby.ADGV.AdvancedDataGridView();
            statusStrip1 = new StatusStrip();
            toolStripLabelCount = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewTranscripts).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { aPIKeysToolStripMenuItem, googleAuthToolStripMenuItem, downloadToolStripMenuItem, downloadGoogleDriveToolStripMenuItem, viewToolStripMenuItem, exportSelectedToolStripMenuItem, deleteAllToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(5, 2, 0, 2);
            menuStrip1.RenderMode = ToolStripRenderMode.Professional;
            menuStrip1.Size = new Size(1486, 73);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // aPIKeysToolStripMenuItem
            // 
            aPIKeysToolStripMenuItem.Image = (Image)resources.GetObject("aPIKeysToolStripMenuItem.Image");
            aPIKeysToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            aPIKeysToolStripMenuItem.Name = "aPIKeysToolStripMenuItem";
            aPIKeysToolStripMenuItem.Size = new Size(64, 69);
            aPIKeysToolStripMenuItem.Text = "API Keys";
            aPIKeysToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            aPIKeysToolStripMenuItem.Click += apiKeysToolStripMenuItem_Click;
            // 
            // googleAuthToolStripMenuItem
            // 
            googleAuthToolStripMenuItem.Image = (Image)resources.GetObject("googleAuthToolStripMenuItem.Image");
            googleAuthToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            googleAuthToolStripMenuItem.Name = "googleAuthToolStripMenuItem";
            googleAuthToolStripMenuItem.Size = new Size(86, 69);
            googleAuthToolStripMenuItem.Text = "Google Auth";
            googleAuthToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            googleAuthToolStripMenuItem.Click += googleAuthToolStripMenuItem_Click;
            // 
            // downloadToolStripMenuItem
            // 
            downloadToolStripMenuItem.Image = (Image)resources.GetObject("downloadToolStripMenuItem.Image");
            downloadToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            downloadToolStripMenuItem.Name = "downloadToolStripMenuItem";
            downloadToolStripMenuItem.Size = new Size(73, 69);
            downloadToolStripMenuItem.Text = "Download";
            downloadToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            downloadToolStripMenuItem.Click += downloadToolStripMenuItem_Click;
            // 
            // downloadGoogleDriveToolStripMenuItem
            // 
            downloadGoogleDriveToolStripMenuItem.Image = (Image)resources.GetObject("downloadGoogleDriveToolStripMenuItem.Image");
            downloadGoogleDriveToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            downloadGoogleDriveToolStripMenuItem.Name = "downloadGoogleDriveToolStripMenuItem";
            downloadGoogleDriveToolStripMenuItem.Size = new Size(87, 69);
            downloadGoogleDriveToolStripMenuItem.Text = "Google Drive";
            downloadGoogleDriveToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            downloadGoogleDriveToolStripMenuItem.Click += downloadGoogleDriveToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { refreshToolStripMenuItem, toolStripSeparator1, viewActiveItemsToolStripMenuItem, viewArchivedItemsToolStripMenuItem, toolStripSeparator2, archiveSelectedToolStripMenuItem, archiveExcludedToolStripMenuItem, archiveExportedToolStripMenuItem, toolStripSeparator3, restoreSelectedToolStripMenuItem, restoreAllToolStripMenuItem });
            viewToolStripMenuItem.Image = (Image)resources.GetObject("viewToolStripMenuItem.Image");
            viewToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(62, 69);
            viewToolStripMenuItem.Text = "View";
            viewToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // refreshToolStripMenuItem
            // 
            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.Size = new Size(181, 22);
            refreshToolStripMenuItem.Text = "Refresh";
            refreshToolStripMenuItem.Click += toolStripButtonRefresh_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(178, 6);
            // 
            // viewActiveItemsToolStripMenuItem
            // 
            viewActiveItemsToolStripMenuItem.Checked = true;
            viewActiveItemsToolStripMenuItem.CheckState = CheckState.Checked;
            viewActiveItemsToolStripMenuItem.Name = "viewActiveItemsToolStripMenuItem";
            viewActiveItemsToolStripMenuItem.Size = new Size(181, 22);
            viewActiveItemsToolStripMenuItem.Text = "View Active Items";
            viewActiveItemsToolStripMenuItem.Click += viewActiveItemsToolStripMenuItem_Click;
            // 
            // viewArchivedItemsToolStripMenuItem
            // 
            viewArchivedItemsToolStripMenuItem.Name = "viewArchivedItemsToolStripMenuItem";
            viewArchivedItemsToolStripMenuItem.Size = new Size(181, 22);
            viewArchivedItemsToolStripMenuItem.Text = "View Archived Items";
            viewArchivedItemsToolStripMenuItem.Click += viewArchivedItemsToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(178, 6);
            // 
            // archiveSelectedToolStripMenuItem
            // 
            archiveSelectedToolStripMenuItem.Name = "archiveSelectedToolStripMenuItem";
            archiveSelectedToolStripMenuItem.Size = new Size(181, 22);
            archiveSelectedToolStripMenuItem.Text = "Archive Selected";
            archiveSelectedToolStripMenuItem.Click += archiveSelectedToolStripMenuItem_Click;
            // 
            // archiveExcludedToolStripMenuItem
            // 
            archiveExcludedToolStripMenuItem.Name = "archiveExcludedToolStripMenuItem";
            archiveExcludedToolStripMenuItem.Size = new Size(181, 22);
            archiveExcludedToolStripMenuItem.Text = "Archive Excluded";
            archiveExcludedToolStripMenuItem.Click += archiveExcludedToolStripMenuItem_Click;
            // 
            // archiveExportedToolStripMenuItem
            // 
            archiveExportedToolStripMenuItem.Name = "archiveExportedToolStripMenuItem";
            archiveExportedToolStripMenuItem.Size = new Size(181, 22);
            archiveExportedToolStripMenuItem.Text = "Archive Exported";
            archiveExportedToolStripMenuItem.Click += archiveExportedToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(178, 6);
            // 
            // restoreSelectedToolStripMenuItem
            // 
            restoreSelectedToolStripMenuItem.Enabled = false;
            restoreSelectedToolStripMenuItem.Name = "restoreSelectedToolStripMenuItem";
            restoreSelectedToolStripMenuItem.Size = new Size(181, 22);
            restoreSelectedToolStripMenuItem.Text = "Restore Selected";
            restoreSelectedToolStripMenuItem.Click += restoreSelectedToolStripMenuItem_Click;
            // 
            // restoreAllToolStripMenuItem
            // 
            restoreAllToolStripMenuItem.Enabled = false;
            restoreAllToolStripMenuItem.Name = "restoreAllToolStripMenuItem";
            restoreAllToolStripMenuItem.Size = new Size(181, 22);
            restoreAllToolStripMenuItem.Text = "Restore All";
            restoreAllToolStripMenuItem.Click += restoreAllToolStripMenuItem_Click;
            // 
            // exportSelectedToolStripMenuItem
            // 
            exportSelectedToolStripMenuItem.Image = (Image)resources.GetObject("exportSelectedToolStripMenuItem.Image");
            exportSelectedToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            exportSelectedToolStripMenuItem.Name = "exportSelectedToolStripMenuItem";
            exportSelectedToolStripMenuItem.Size = new Size(62, 69);
            exportSelectedToolStripMenuItem.Text = "Export";
            exportSelectedToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            exportSelectedToolStripMenuItem.Click += exportSelectedToolStripMenuItem_Click;
            // 
            // deleteAllToolStripMenuItem
            // 
            deleteAllToolStripMenuItem.Image = (Image)resources.GetObject("deleteAllToolStripMenuItem.Image");
            deleteAllToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            deleteAllToolStripMenuItem.Name = "deleteAllToolStripMenuItem";
            deleteAllToolStripMenuItem.Size = new Size(69, 69);
            deleteAllToolStripMenuItem.Text = "Delete All";
            deleteAllToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            deleteAllToolStripMenuItem.Click += toolStripButtonDeleteAll_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Image = (Image)resources.GetObject("helpToolStripMenuItem.Image");
            helpToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(62, 69);
            helpToolStripMenuItem.Text = "Help";
            helpToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            helpToolStripMenuItem.Click += helpToolStripMenuItem_Click;
            // 
            // dataGridViewTranscripts
            // 
            dataGridViewTranscripts.AllowUserToAddRows = false;
            dataGridViewTranscripts.AllowUserToDeleteRows = false;
            dataGridViewTranscripts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewTranscripts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewTranscripts.Dock = DockStyle.Fill;
            dataGridViewTranscripts.FilterAndSortEnabled = true;
            dataGridViewTranscripts.FilterStringChangedInvokeBeforeDatasourceUpdate = true;
            dataGridViewTranscripts.Location = new Point(0, 73);
            dataGridViewTranscripts.Margin = new Padding(3, 2, 3, 2);
            dataGridViewTranscripts.MaxFilterButtonImageHeight = 23;
            dataGridViewTranscripts.Name = "dataGridViewTranscripts";
            dataGridViewTranscripts.RightToLeft = RightToLeft.No;
            dataGridViewTranscripts.RowHeadersWidth = 51;
            dataGridViewTranscripts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewTranscripts.Size = new Size(1486, 695);
            dataGridViewTranscripts.SortStringChangedInvokeBeforeDatasourceUpdate = true;
            dataGridViewTranscripts.TabIndex = 1;
            dataGridViewTranscripts.CellClick += dataGridViewTranscripts_CellClick;
            dataGridViewTranscripts.CellValueChanged += dataGridViewTranscripts_CellValueChanged;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripLabelCount });
            statusStrip1.Location = new Point(0, 768);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 12, 0);
            statusStrip1.Size = new Size(1486, 22);
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripLabelCount
            // 
            toolStripLabelCount.Name = "toolStripLabelCount";
            toolStripLabelCount.Size = new Size(71, 17);
            toolStripLabelCount.Text = "0 transcripts";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1486, 790);
            Controls.Add(dataGridViewTranscripts);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(3, 2, 3, 2);
            Name = "MainForm";
            Text = "Medley Collector";
            Load += MainForm_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewTranscripts).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem aPIKeysToolStripMenuItem;
        private ToolStripMenuItem googleAuthToolStripMenuItem;
        private ToolStripMenuItem downloadToolStripMenuItem;
        private ToolStripMenuItem downloadGoogleDriveToolStripMenuItem;
        private ToolStripMenuItem refreshToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem viewActiveItemsToolStripMenuItem;
        private ToolStripMenuItem viewArchivedItemsToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem archiveSelectedToolStripMenuItem;
        private ToolStripMenuItem archiveExcludedToolStripMenuItem;
        private ToolStripMenuItem archiveExportedToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem restoreSelectedToolStripMenuItem;
        private ToolStripMenuItem restoreAllToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem exportSelectedToolStripMenuItem;
        private ToolStripMenuItem deleteAllToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private Zuby.ADGV.AdvancedDataGridView dataGridViewTranscripts;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripLabelCount;
    }
}
