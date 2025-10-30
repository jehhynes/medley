namespace Medley.Collector
{
    partial class FellowApiKeys
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            dgvApiKeys = new DataGridView();
            colName = new DataGridViewTextBoxColumn();
            colKey = new DataGridViewTextBoxColumn();
            colEnabled = new DataGridViewCheckBoxColumn();
            colCreatedAt = new DataGridViewTextBoxColumn();
            colLastUsed = new DataGridViewTextBoxColumn();
            colDownloadedThrough = new DataGridViewTextBoxColumn();
            btnSave = new Button();
            btnCancel = new Button();
            label1 = new Label();
            lblWorkspace = new Label();
            txtWorkspace = new TextBox();
            ((System.ComponentModel.ISupportInitialize)dgvApiKeys).BeginInit();
            SuspendLayout();
            // 
            // dgvApiKeys
            // 
            dgvApiKeys.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvApiKeys.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvApiKeys.Columns.AddRange(new DataGridViewColumn[] { colName, colKey, colEnabled, colCreatedAt, colLastUsed, colDownloadedThrough });
            dgvApiKeys.Location = new Point(10, 76);
            dgvApiKeys.Margin = new Padding(3, 2, 3, 2);
            dgvApiKeys.Name = "dgvApiKeys";
            dgvApiKeys.RowHeadersWidth = 51;
            dgvApiKeys.Size = new Size(863, 175);
            dgvApiKeys.TabIndex = 2;
            // 
            // colName
            // 
            colName.HeaderText = "Name";
            colName.MinimumWidth = 6;
            colName.Name = "colName";
            colName.Width = 150;
            // 
            // colKey
            // 
            colKey.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colKey.HeaderText = "API Key";
            colKey.MinimumWidth = 6;
            colKey.Name = "colKey";
            // 
            // colEnabled
            // 
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.NullValue = true;
            colEnabled.DefaultCellStyle = dataGridViewCellStyle1;
            colEnabled.HeaderText = "Enabled";
            colEnabled.MinimumWidth = 6;
            colEnabled.Name = "colEnabled";
            colEnabled.Width = 80;
            // 
            // colCreatedAt
            // 
            colCreatedAt.HeaderText = "Created";
            colCreatedAt.MinimumWidth = 6;
            colCreatedAt.Name = "colCreatedAt";
            colCreatedAt.ReadOnly = true;
            colCreatedAt.Width = 120;
            // 
            // colLastUsed
            // 
            colLastUsed.HeaderText = "Last Used";
            colLastUsed.MinimumWidth = 6;
            colLastUsed.Name = "colLastUsed";
            colLastUsed.ReadOnly = true;
            colLastUsed.Width = 120;
            // 
            // colDownloadedThrough
            // 
            colDownloadedThrough.HeaderText = "Downloaded Through";
            colDownloadedThrough.MinimumWidth = 6;
            colDownloadedThrough.Name = "colDownloadedThrough";
            colDownloadedThrough.ReadOnly = true;
            colDownloadedThrough.Width = 140;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.Location = new Point(791, 256);
            btnSave.Margin = new Padding(3, 2, 3, 2);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(82, 22);
            btnSave.TabIndex = 3;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(704, 256);
            btnCancel.Margin = new Padding(3, 2, 3, 2);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(82, 22);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 59);
            label1.Name = "label1";
            label1.Size = new Size(160, 15);
            label1.TabIndex = 5;
            label1.Text = "Enter your Fellow.ai API keys:";
            // 
            // lblWorkspace
            // 
            lblWorkspace.AutoSize = true;
            lblWorkspace.Location = new Point(13, 11);
            lblWorkspace.Name = "lblWorkspace";
            lblWorkspace.Size = new Size(105, 15);
            lblWorkspace.TabIndex = 6;
            lblWorkspace.Text = "Fellow Workspace:";
            // 
            // txtWorkspace
            // 
            txtWorkspace.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtWorkspace.Location = new Point(13, 28);
            txtWorkspace.Margin = new Padding(3, 2, 3, 2);
            txtWorkspace.Name = "txtWorkspace";
            txtWorkspace.Size = new Size(861, 23);
            txtWorkspace.TabIndex = 0;
            // 
            // FellowApiKeys
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(884, 285);
            Controls.Add(txtWorkspace);
            Controls.Add(lblWorkspace);
            Controls.Add(label1);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(dgvApiKeys);
            Margin = new Padding(3, 2, 3, 2);
            Name = "FellowApiKeys";
            Text = "API Keys";
            Load += FellowApiKeys_Load;
            ((System.ComponentModel.ISupportInitialize)dgvApiKeys).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvApiKeys;
        private Button btnSave;
        private Button btnCancel;
        private Label label1;
        private Label lblWorkspace;
        private TextBox txtWorkspace;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colKey;
        private DataGridViewCheckBoxColumn colEnabled;
        private DataGridViewTextBoxColumn colCreatedAt;
        private DataGridViewTextBoxColumn colLastUsed;
        private DataGridViewTextBoxColumn colDownloadedThrough;
    }
}