namespace Medley.CollectorUtil
{
    partial class HelpForm
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
            textBoxHelp = new TextBox();
            buttonClose = new Button();
            SuspendLayout();
            // 
            // textBoxHelp
            // 
            textBoxHelp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxHelp.BackColor = SystemColors.Window;
            textBoxHelp.Font = new Font("Consolas", 10F);
            textBoxHelp.Location = new Point(12, 12);
            textBoxHelp.Multiline = true;
            textBoxHelp.Name = "textBoxHelp";
            textBoxHelp.ReadOnly = true;
            textBoxHelp.ScrollBars = ScrollBars.Vertical;
            textBoxHelp.Size = new Size(999, 597);
            textBoxHelp.TabIndex = 0;
            // 
            // buttonClose
            // 
            buttonClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClose.Location = new Point(916, 615);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(95, 35);
            buttonClose.TabIndex = 1;
            buttonClose.Text = "Close";
            buttonClose.UseVisualStyleBackColor = true;
            buttonClose.Click += buttonClose_Click;
            // 
            // HelpForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1023, 662);
            Controls.Add(buttonClose);
            Controls.Add(textBoxHelp);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(600, 400);
            Name = "HelpForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Help - Medley Collector";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBoxHelp;
        private Button buttonClose;
    }
}
