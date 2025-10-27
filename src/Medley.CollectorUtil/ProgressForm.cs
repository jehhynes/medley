namespace Medley.CollectorUtil;

public partial class ProgressForm : Form
{
    public ProgressForm()
    {
        InitializeComponent();
    }

    public void UpdateStatus(string status)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(UpdateStatus), status);
            return;
        }

        lblStatus.Text = status;
        Application.DoEvents();
    }

    public void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(AppendLog), message);
            return;
        }

        txtLog.AppendText(message + Environment.NewLine);
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
        Application.DoEvents();
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        Close();
    }
}
