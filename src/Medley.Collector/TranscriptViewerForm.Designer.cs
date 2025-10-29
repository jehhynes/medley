namespace Medley.Collector;

partial class TranscriptViewerForm
{
    private System.ComponentModel.IContainer components = null;
    private TabControl tabControl;
    private TabPage tabPageTranscript;
    private TabPage tabPageNote;
    private TextBox textBoxTranscript;
    private WebBrowser webBrowserNote;
    private TextBox textBoxAttendees;

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
        tabControl = new TabControl();
        tabPageTranscript = new TabPage();
        textBoxTranscript = new TextBox();
        tabPageNote = new TabPage();
        webBrowserNote = new WebBrowser();
        textBoxAttendees = new TextBox();
        tabControl.SuspendLayout();
        tabPageTranscript.SuspendLayout();
        tabPageNote.SuspendLayout();
        SuspendLayout();
        // 
        // tabControl
        // 
        tabControl.Controls.Add(tabPageTranscript);
        tabControl.Controls.Add(tabPageNote);
        tabControl.Dock = DockStyle.Fill;
        tabControl.Location = new Point(0, 0);
        tabControl.Margin = new Padding(3, 4, 3, 4);
        tabControl.Name = "tabControl";
        tabControl.SelectedIndex = 0;
        tabControl.Size = new Size(914, 800);
        tabControl.TabIndex = 0;
        // 
        // tabPageTranscript
        // 
        tabPageTranscript.Controls.Add(textBoxTranscript);
        tabPageTranscript.Location = new Point(4, 29);
        tabPageTranscript.Margin = new Padding(3, 4, 3, 4);
        tabPageTranscript.Name = "tabPageTranscript";
        tabPageTranscript.Padding = new Padding(3, 4, 3, 4);
        tabPageTranscript.Size = new Size(906, 767);
        tabPageTranscript.TabIndex = 0;
        tabPageTranscript.Text = "Transcript";
        tabPageTranscript.UseVisualStyleBackColor = true;
        // 
        // textBoxTranscript
        // 
        textBoxTranscript.Dock = DockStyle.Fill;
        textBoxTranscript.Font = new Font("Segoe UI", 10F);
        textBoxTranscript.Location = new Point(3, 4);
        textBoxTranscript.Margin = new Padding(3, 4, 3, 4);
        textBoxTranscript.Multiline = true;
        textBoxTranscript.Name = "textBoxTranscript";
        textBoxTranscript.ReadOnly = true;
        textBoxTranscript.ScrollBars = ScrollBars.Both;
        textBoxTranscript.Size = new Size(900, 759);
        textBoxTranscript.TabIndex = 0;
        // 
        // tabPageNote
        // 
        tabPageNote.Controls.Add(webBrowserNote);
        tabPageNote.Controls.Add(textBoxAttendees);
        tabPageNote.Location = new Point(4, 29);
        tabPageNote.Margin = new Padding(3, 4, 3, 4);
        tabPageNote.Name = "tabPageNote";
        tabPageNote.Padding = new Padding(3, 4, 3, 4);
        tabPageNote.Size = new Size(906, 767);
        tabPageNote.TabIndex = 1;
        tabPageNote.Text = "Note";
        tabPageNote.UseVisualStyleBackColor = true;
        // 
        // webBrowserNote
        // 
        webBrowserNote.Dock = DockStyle.Fill;
        webBrowserNote.Location = new Point(3, 51);
        webBrowserNote.Margin = new Padding(3, 4, 3, 4);
        webBrowserNote.MinimumSize = new Size(23, 27);
        webBrowserNote.Name = "webBrowserNote";
        webBrowserNote.Size = new Size(900, 712);
        webBrowserNote.TabIndex = 1;
        // 
        // textBoxAttendees
        // 
        textBoxAttendees.Dock = DockStyle.Top;
        textBoxAttendees.Font = new Font("Segoe UI", 9F);
        textBoxAttendees.Location = new Point(3, 4);
        textBoxAttendees.Multiline = true;
        textBoxAttendees.Name = "textBoxAttendees";
        textBoxAttendees.ReadOnly = true;
        textBoxAttendees.Size = new Size(900, 47);
        textBoxAttendees.TabIndex = 0;
        textBoxAttendees.Text = "Attendees:";
        // 
        // TranscriptViewerForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(914, 800);
        Controls.Add(tabControl);
        Margin = new Padding(3, 4, 3, 4);
        Name = "TranscriptViewerForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Transcript Viewer";
        tabControl.ResumeLayout(false);
        tabPageTranscript.ResumeLayout(false);
        tabPageTranscript.PerformLayout();
        tabPageNote.ResumeLayout(false);
        tabPageNote.PerformLayout();
        ResumeLayout(false);
    }
}
