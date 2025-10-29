namespace Medley.Collector;

public partial class ProgressForm : Form
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public ProgressForm()
    {
        InitializeComponent();
        FormClosing += ProgressForm_FormClosing;
    }

    private void ProgressForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }

    public void UpdateStatus(string status)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            Invoke(new Action<string>(UpdateStatus), status);
            return;
        }

        lblStatus.Text = status;
        System.Windows.Forms.Application.DoEvents();
    }

    public void AppendLog(string message)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            Invoke(new Action<string>(AppendLog), message);
            return;
        }

        txtLog.AppendText(message + Environment.NewLine);
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
        System.Windows.Forms.Application.DoEvents();
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        Close();
    }
}
