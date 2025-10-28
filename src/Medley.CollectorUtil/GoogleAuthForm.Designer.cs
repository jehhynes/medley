namespace Medley.CollectorUtil
{
    partial class GoogleAuthForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GoogleAuthForm));
            labelTitle = new Label();
            labelClientId = new Label();
            textBoxClientId = new TextBox();
            labelClientSecret = new Label();
            textBoxClientSecret = new TextBox();
            buttonSave = new Button();
            buttonAuthenticate = new Button();
            labelStatus = new Label();
            buttonRevoke = new Button();
            labelInstructions = new Label();
            buttonAuthenticateBrowser = new Button();
            label1 = new Label();
            labelFolderId = new Label();
            textBoxFolderId = new TextBox();
            SuspendLayout();
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            labelTitle.Location = new Point(12, 9);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(295, 28);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "Google OAuth2 Configuration";
            // 
            // labelClientId
            // 
            labelClientId.AutoSize = true;
            labelClientId.Location = new Point(12, 120);
            labelClientId.Name = "labelClientId";
            labelClientId.Size = new Size(69, 20);
            labelClientId.TabIndex = 1;
            labelClientId.Text = "Client ID:";
            // 
            // textBoxClientId
            // 
            textBoxClientId.Location = new Point(12, 143);
            textBoxClientId.Name = "textBoxClientId";
            textBoxClientId.Size = new Size(560, 27);
            textBoxClientId.TabIndex = 2;
            // 
            // labelClientSecret
            // 
            labelClientSecret.AutoSize = true;
            labelClientSecret.Location = new Point(12, 183);
            labelClientSecret.Name = "labelClientSecret";
            labelClientSecret.Size = new Size(95, 20);
            labelClientSecret.TabIndex = 3;
            labelClientSecret.Text = "Client Secret:";
            // 
            // textBoxClientSecret
            // 
            textBoxClientSecret.Location = new Point(12, 206);
            textBoxClientSecret.Name = "textBoxClientSecret";
            textBoxClientSecret.PasswordChar = '●';
            textBoxClientSecret.Size = new Size(560, 27);
            textBoxClientSecret.TabIndex = 4;
            // 
            // labelFolderId
            // 
            labelFolderId.AutoSize = true;
            labelFolderId.Location = new Point(12, 246);
            labelFolderId.Name = "labelFolderId";
            labelFolderId.Size = new Size(73, 20);
            labelFolderId.TabIndex = 13;
            labelFolderId.Text = "Folder ID:";
            // 
            // textBoxFolderId
            // 
            textBoxFolderId.Location = new Point(12, 269);
            textBoxFolderId.Name = "textBoxFolderId";
            textBoxFolderId.PlaceholderText = "Optional - Leave blank to search all folders";
            textBoxFolderId.Size = new Size(560, 27);
            textBoxFolderId.TabIndex = 14;
            // 
            // buttonSave
            // 
            buttonSave.Location = new Point(12, 312);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(120, 35);
            buttonSave.TabIndex = 5;
            buttonSave.Text = "Save";
            buttonSave.UseVisualStyleBackColor = true;
            buttonSave.Click += buttonSave_Click;
            // 
            // buttonAuthenticate
            // 
            buttonAuthenticate.Location = new Point(12, 393);
            buttonAuthenticate.Name = "buttonAuthenticate";
            buttonAuthenticate.Size = new Size(150, 35);
            buttonAuthenticate.TabIndex = 6;
            buttonAuthenticate.Text = "OAuth Flow";
            buttonAuthenticate.UseVisualStyleBackColor = true;
            buttonAuthenticate.Click += buttonAuthenticate_Click;
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            labelStatus.Location = new Point(12, 363);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(239, 20);
            labelStatus.TabIndex = 7;
            labelStatus.Text = "OAuth Status: Not authenticated";
            // 
            // buttonRevoke
            // 
            buttonRevoke.Location = new Point(168, 393);
            buttonRevoke.Name = "buttonRevoke";
            buttonRevoke.Size = new Size(150, 35);
            buttonRevoke.TabIndex = 8;
            buttonRevoke.Text = "Revoke Access";
            buttonRevoke.UseVisualStyleBackColor = true;
            buttonRevoke.Click += buttonRevoke_Click;
            // 
            // labelInstructions
            // 
            labelInstructions.Location = new Point(12, 47);
            labelInstructions.Name = "labelInstructions";
            labelInstructions.Size = new Size(560, 60);
            labelInstructions.TabIndex = 9;
            labelInstructions.Text = resources.GetString("labelInstructions.Text");
            // 
            // buttonAuthenticateBrowser
            // 
            buttonAuthenticateBrowser.Location = new Point(12, 482);
            buttonAuthenticateBrowser.Name = "buttonAuthenticateBrowser";
            buttonAuthenticateBrowser.Size = new Size(200, 35);
            buttonAuthenticateBrowser.TabIndex = 10;
            buttonAuthenticateBrowser.Text = "Sign In with Browser";
            buttonAuthenticateBrowser.UseVisualStyleBackColor = true;
            buttonAuthenticateBrowser.Click += buttonAuthenticateBrowser_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label1.Location = new Point(12, 441);
            label1.Name = "label1";
            label1.Size = new Size(372, 28);
            label1.TabIndex = 12;
            label1.Text = "Google Browser-based Authentication";
            // 
            // GoogleAuthForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(584, 528);
            Controls.Add(textBoxFolderId);
            Controls.Add(labelFolderId);
            Controls.Add(label1);
            Controls.Add(buttonAuthenticateBrowser);
            Controls.Add(labelInstructions);
            Controls.Add(buttonRevoke);
            Controls.Add(labelStatus);
            Controls.Add(buttonAuthenticate);
            Controls.Add(buttonSave);
            Controls.Add(textBoxClientSecret);
            Controls.Add(labelClientSecret);
            Controls.Add(textBoxClientId);
            Controls.Add(labelClientId);
            Controls.Add(labelTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GoogleAuthForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Google Authentication";
            Load += GoogleAuthForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelTitle;
        private Label labelClientId;
        private TextBox textBoxClientId;
        private Label labelClientSecret;
        private TextBox textBoxClientSecret;
        private Button buttonSave;
        private Button buttonAuthenticate;
        private Label labelStatus;
        private Button buttonRevoke;
        private Label labelInstructions;
        private Button buttonAuthenticateBrowser;
        private Label label1;
        private Label labelFolderId;
        private TextBox textBoxFolderId;
    }
}
