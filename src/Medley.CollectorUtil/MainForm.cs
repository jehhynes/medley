namespace Medley.CollectorUtil;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {

    }

    private void apiKeysToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using (var apiKeysForm = new FellowApiKeys())
        {
            apiKeysForm.ShowDialog(this);
        }
    }
}
