namespace Medley.Collector;

partial class TranscriptViewerForm
{
    private System.ComponentModel.IContainer components = null;
    private TextBox textBoxTranscript;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        textBoxTranscript = new TextBox();
        SuspendLayout();
        
        // 
        // textBoxTranscript
        // 
        textBoxTranscript.Dock = DockStyle.Fill;
        textBoxTranscript.Font = new Font("Segoe UI", 10F);
        textBoxTranscript.Location = new Point(0, 0);
        textBoxTranscript.Multiline = true;
        textBoxTranscript.Name = "textBoxTranscript";
        textBoxTranscript.ReadOnly = true;
        textBoxTranscript.ScrollBars = ScrollBars.Both;
        textBoxTranscript.Size = new Size(800, 600);
        textBoxTranscript.TabIndex = 0;
        textBoxTranscript.WordWrap = true;
        
        // 
        // TranscriptViewerForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 600);
        Controls.Add(textBoxTranscript);
        Name = "TranscriptViewerForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Transcript Viewer";
        ResumeLayout(false);
        PerformLayout();
    }
}
