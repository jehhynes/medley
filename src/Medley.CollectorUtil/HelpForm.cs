namespace Medley.CollectorUtil;

public partial class HelpForm : Form
{
    public HelpForm()
    {
        InitializeComponent();
        LoadHelpContent();
    }

    private void LoadHelpContent()
    {
        var helpText = @"MEDLEY COLLECTOR - HELP

OVERVIEW
Medley Collector helps you download, manage, and export meeting transcripts from Fellow.app.

GETTING STARTED
1. Configure API Keys: Click 'API Keys' to add your Fellow.app API keys and workspace
2. Download Transcripts: Click 'Download' to fetch transcripts from Fellow.app
3. Review & Select: Mark transcripts for export using the Export column
4. Export: Click 'Export' to save selected transcripts to a ZIP file

KEYBOARD SHORTCUTS
When rows are selected/highlighted:
  • SPACE    - Mark selected rows for export (green)
  • X        - Exclude selected rows from export (red)
  • R        - Reset selected rows to undecided (white)

MOUSE ACTIONS
  • Click checkbox - Cycle through: undecided → include → exclude → undecided
  • Click search icon - View full transcript details
  • Click column header - Sort by that column
  • Click filter icon - Filter column values

ROW COLORS
  • Green  - Marked for export (IsSelected = true)
  • Red    - Excluded from export (IsSelected = false)
  • White  - Undecided (IsSelected = null)

WORKFLOW
1. Download transcripts from Fellow.app using your API keys
2. Use filters and sorting to find relevant transcripts
3. Select multiple rows (Ctrl+Click or Shift+Click)
4. Use keyboard shortcuts to quickly mark/unmark transcripts
5. Export only includes transcripts marked as 'true' (green)
6. All transcripts must be decided (no white rows) before export

MENU OPTIONS
  • API Keys    - Manage Fellow.app API keys and workspace settings
  • Download    - Fetch new transcripts from Fellow.app
  • Refresh     - Reload transcripts from local database
  • Export      - Export selected transcripts to ZIP file
  • Delete All  - Remove all transcripts from local database
  • Help        - Show this help dialog

IMPORTANT BUSINESS RULES
  • API keys are automatically disabled after successful download completion
  • Disabled API keys must be manually re-enabled in API Keys settings to download again
  • Meetings that started less than 2 hours ago are skipped (transcripts may be incomplete)
  • Future meetings are skipped
  • Duplicate transcripts for the same API key are skipped
  • Download includes retry logic (3 attempts) for failed operations
  • Export creates a ZIP file with one JSON file per transcript
  • Export requires all transcripts to be marked (no undecided/white rows)
  • Transcript viewer consolidates consecutive segments by the same speaker

TIPS
  • Use Ctrl+Click to select multiple individual rows
  • Use Shift+Click to select a range of rows
  • Use column filters to narrow down transcripts
  • View transcript details before deciding to export
  • The status bar shows filtered vs total transcript count
  • Press ESC in transcript viewer to close it quickly
";

        textBoxHelp.Text = helpText;
        textBoxHelp.SelectionStart = 0;
        textBoxHelp.SelectionLength = 0;
    }

    private void buttonClose_Click(object sender, EventArgs e)
    {
        Close();
    }
}
