namespace Medley.CollectorUtil
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
            dgvApiKeys = new DataGridView();
            colName = new DataGridViewTextBoxColumn();
            colKey = new DataGridViewTextBoxColumn();
            colEnabled = new DataGridViewCheckBoxColumn();
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
            dgvApiKeys.Columns.AddRange(new DataGridViewColumn[] { colName, colKey, colEnabled });
            dgvApiKeys.Location = new Point(12, 102);
            dgvApiKeys.Name = "dgvApiKeys";
            dgvApiKeys.RowHeadersWidth = 51;
            dgvApiKeys.Size = new Size(609, 233);
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
            colEnabled.DefaultCellStyle.NullValue = true;
            colEnabled.HeaderText = "Enabled";
            colEnabled.MinimumWidth = 6;
            colEnabled.Name = "colEnabled";
            colEnabled.Width = 80;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.Location = new Point(527, 341);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(94, 29);
            btnSave.TabIndex = 3;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(427, 341);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 29);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 79);
            label1.Name = "label1";
            label1.Size = new Size(234, 20);
            label1.TabIndex = 5;
            label1.Text = "Enter your Fellow.ai API keys:";
            // 
            // lblWorkspace
            // 
            lblWorkspace.AutoSize = true;
            lblWorkspace.Location = new Point(15, 15);
            lblWorkspace.Name = "lblWorkspace";
            lblWorkspace.Size = new Size(85, 20);
            lblWorkspace.TabIndex = 6;
            lblWorkspace.Text = "Workspace:";
            // 
            // txtWorkspace
            // 
            txtWorkspace.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtWorkspace.Location = new Point(15, 38);
            txtWorkspace.Name = "txtWorkspace";
            txtWorkspace.Size = new Size(606, 27);
            txtWorkspace.TabIndex = 0;
            // 
            // FellowApiKeys
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(633, 380);
            Controls.Add(txtWorkspace);
            Controls.Add(lblWorkspace);
            Controls.Add(label1);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(dgvApiKeys);
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
    }
}