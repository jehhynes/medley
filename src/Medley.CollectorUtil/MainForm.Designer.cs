
namespace Medley.CollectorUtil
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
            downloadToolStripMenuItem = new ToolStripMenuItem();
            refreshToolStripMenuItem = new ToolStripMenuItem();
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
            menuStrip1.Items.AddRange(new ToolStripItem[] { aPIKeysToolStripMenuItem, downloadToolStripMenuItem, refreshToolStripMenuItem, exportSelectedToolStripMenuItem, deleteAllToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.RenderMode = ToolStripRenderMode.Professional;
            menuStrip1.Size = new Size(1698, 78);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // aPIKeysToolStripMenuItem
            // 
            aPIKeysToolStripMenuItem.Image = (Image)resources.GetObject("aPIKeysToolStripMenuItem.Image");
            aPIKeysToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            aPIKeysToolStripMenuItem.Name = "aPIKeysToolStripMenuItem";
            aPIKeysToolStripMenuItem.Size = new Size(79, 74);
            aPIKeysToolStripMenuItem.Text = "API Keys";
            aPIKeysToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            aPIKeysToolStripMenuItem.Click += apiKeysToolStripMenuItem_Click;
            // 
            // downloadToolStripMenuItem
            // 
            downloadToolStripMenuItem.Image = (Image)resources.GetObject("downloadToolStripMenuItem.Image");
            downloadToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            downloadToolStripMenuItem.Name = "downloadToolStripMenuItem";
            downloadToolStripMenuItem.Size = new Size(92, 74);
            downloadToolStripMenuItem.Text = "Download";
            downloadToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            downloadToolStripMenuItem.Click += downloadToolStripMenuItem_Click;
            // 
            // refreshToolStripMenuItem
            // 
            refreshToolStripMenuItem.Image = (Image)resources.GetObject("refreshToolStripMenuItem.Image");
            refreshToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.Size = new Size(72, 74);
            refreshToolStripMenuItem.Text = "Refresh";
            refreshToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            refreshToolStripMenuItem.Click += toolStripButtonRefresh_Click;
            // 
            // exportSelectedToolStripMenuItem
            // 
            exportSelectedToolStripMenuItem.Image = (Image)resources.GetObject("exportSelectedToolStripMenuItem.Image");
            exportSelectedToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            exportSelectedToolStripMenuItem.Name = "exportSelectedToolStripMenuItem";
            exportSelectedToolStripMenuItem.Size = new Size(66, 74);
            exportSelectedToolStripMenuItem.Text = "Export";
            exportSelectedToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            exportSelectedToolStripMenuItem.Click += exportSelectedToolStripMenuItem_Click;
            // 
            // deleteAllToolStripMenuItem
            // 
            deleteAllToolStripMenuItem.Image = (Image)resources.GetObject("deleteAllToolStripMenuItem.Image");
            deleteAllToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            deleteAllToolStripMenuItem.Name = "deleteAllToolStripMenuItem";
            deleteAllToolStripMenuItem.Size = new Size(89, 74);
            deleteAllToolStripMenuItem.Text = "Delete All";
            deleteAllToolStripMenuItem.TextImageRelation = TextImageRelation.ImageAboveText;
            deleteAllToolStripMenuItem.Click += toolStripButtonDeleteAll_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Image = (Image)resources.GetObject("helpToolStripMenuItem.Image");
            helpToolStripMenuItem.ImageScaling = ToolStripItemImageScaling.None;
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(64, 74);
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
            dataGridViewTranscripts.Location = new Point(0, 78);
            dataGridViewTranscripts.MaxFilterButtonImageHeight = 23;
            dataGridViewTranscripts.Name = "dataGridViewTranscripts";
            dataGridViewTranscripts.RightToLeft = RightToLeft.No;
            dataGridViewTranscripts.RowHeadersWidth = 51;
            dataGridViewTranscripts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewTranscripts.Size = new Size(1698, 949);
            dataGridViewTranscripts.SortStringChangedInvokeBeforeDatasourceUpdate = true;
            dataGridViewTranscripts.TabIndex = 1;
            dataGridViewTranscripts.CellClick += dataGridViewTranscripts_CellClick;
            dataGridViewTranscripts.CellValueChanged += dataGridViewTranscripts_CellValueChanged;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripLabelCount });
            statusStrip1.Location = new Point(0, 1027);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1698, 26);
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripLabelCount
            // 
            toolStripLabelCount.Name = "toolStripLabelCount";
            toolStripLabelCount.Size = new Size(89, 20);
            toolStripLabelCount.Text = "0 transcripts";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1698, 1053);
            Controls.Add(dataGridViewTranscripts);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
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
        private ToolStripMenuItem downloadToolStripMenuItem;
        private ToolStripMenuItem refreshToolStripMenuItem;
        private ToolStripMenuItem exportSelectedToolStripMenuItem;
        private ToolStripMenuItem deleteAllToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private Zuby.ADGV.AdvancedDataGridView dataGridViewTranscripts;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripLabelCount;
    }
}
