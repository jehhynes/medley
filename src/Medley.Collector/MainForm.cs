using Medley.Collector.Data;
using Medley.Collector.Models;
using Medley.Collector.Services;
using System.ComponentModel;
using System.Data;
using Zuby.ADGV;

namespace Medley.Collector;

public partial class MainForm : Form
{
    private readonly ApiKeyService _apiKeyService;
    private readonly MeetingTranscriptService _transcriptService;
    private readonly ConfigurationService _configurationService;
    private readonly TranscriptExportService _exportService;
    private int _totalRecordCount;
    private string? _lastSortColumn;
    private ListSortDirection _lastSortDirection = ListSortDirection.Ascending;
    private Image? _viewIcon;

    public MainForm()
    {
        InitializeComponent();
        _apiKeyService = new ApiKeyService();
        _transcriptService = new MeetingTranscriptService();
        _configurationService = new ConfigurationService();
        _exportService = new TranscriptExportService();

        // Wire up filter event - use BeginInvoke to ensure count updates after filter is applied
        dataGridViewTranscripts.FilterStringChanged += (s, e) =>
        {
            BeginInvoke(new Action(UpdateFilterCount));
        };

        // Wire up column header click for sorting through ADGV
        dataGridViewTranscripts.SortStringChanged += DataGridViewTranscripts_SortStringChanged;
        dataGridViewTranscripts.ColumnHeaderMouseClick += DataGridViewTranscripts_ColumnHeaderMouseClick;

        // Wire up cell formatting for row colors
        dataGridViewTranscripts.CellFormatting += DataGridViewTranscripts_CellFormatting;

        // Wire up keyboard shortcuts for batch editing
        dataGridViewTranscripts.KeyDown += DataGridViewTranscripts_KeyDown;
    }

    private void DataGridViewTranscripts_SortStringChanged(object? sender, EventArgs e)
    {
        // This event fires when ADGV applies a sort
        // We can use this to track the sort state if needed
    }

    private void DataGridViewTranscripts_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0) return;

        var column = dataGridViewTranscripts.Columns[e.ColumnIndex];

        // Skip non-sortable columns (Id and ViewButton)
        if (column.Name == "Id" || column.Name == "ViewButton" || column.SortMode == DataGridViewColumnSortMode.NotSortable) return;

        // Check if the click is on the filter button area (right side of header)
        // The filter button is typically in the rightmost ~20 pixels of the header
        var headerCell = dataGridViewTranscripts.GetCellDisplayRectangle(e.ColumnIndex, -1, false);
        var filterButtonWidth = 20;
        var clickX = e.X;

        // If click is in the filter button area, don't sort
        if (clickX > headerCell.Width - filterButtonWidth)
        {
            return;
        }

        // Determine sort direction
        ListSortDirection direction;
        if (_lastSortColumn == column.Name)
        {
            // Same column - reverse direction
            direction = _lastSortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            // New column - start with ascending
            direction = ListSortDirection.Ascending;
        }

        // Clear all sorts from all columns
        foreach (DataGridViewColumn col in dataGridViewTranscripts.Columns)
        {
            col.HeaderCell.SortGlyphDirection = SortOrder.None;
        }

        // Clear ADGV's internal sort state
        dataGridViewTranscripts.CleanSort();

        // Apply sort through ADGV
        if (direction == ListSortDirection.Ascending)
        {
            dataGridViewTranscripts.SortASC(column);
        }
        else
        {
            dataGridViewTranscripts.SortDESC(column);
        }

        // Remember last sort
        _lastSortColumn = column.Name;
        _lastSortDirection = direction;
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        SetupDataGridView();

        await LoadTranscriptsAsync();
    }

    private void SetupDataGridView()
    {
        // Ensure filtering is enabled
        dataGridViewTranscripts.FilterAndSortEnabled = true;

        dataGridViewTranscripts.AutoGenerateColumns = false;
        dataGridViewTranscripts.Columns.Clear();

        // Hidden Id column for tracking
        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Id",
            HeaderText = "Id",
            Name = "Id",
            Visible = false
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = "IsSelected",
            HeaderText = "Export",
            Name = "IsSelected",
            FillWeight = 10,
            ReadOnly = false,
            SortMode = DataGridViewColumnSortMode.Automatic,
            ThreeState = true
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Title",
            HeaderText = "Title",
            Name = "Title",
            FillWeight = 30,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Automatic
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Date",
            HeaderText = "Date",
            Name = "Date",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "g" },
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Automatic
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Participants",
            HeaderText = "Participants",
            Name = "Participants",
            FillWeight = 25,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Automatic
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ApiKeyNames",
            HeaderText = "API Keys",
            Name = "ApiKeyNames",
            FillWeight = 20,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Automatic
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "LengthInMinutes",
            HeaderText = "Length (min)",
            Name = "LengthInMinutes",
            FillWeight = 10,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Automatic
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "TranscriptLength",
            HeaderText = "Length (chars)",
            Name = "TranscriptLength",
            FillWeight = 12,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Automatic,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" }
        });

        // Add View button column with icon from embedded resource
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var resourceName = "Medley.Collector.icons.icon-search-20.png";
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                _viewIcon = Image.FromStream(stream);
            }
        }

        var viewButtonColumn = new DataGridViewImageColumn
        {
            HeaderText = "Transcript",
            Name = "ViewButton",
            Image = _viewIcon,
            ImageLayout = DataGridViewImageCellLayout.Normal,
            FillWeight = 10,
            Width = 40,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        dataGridViewTranscripts.Columns.Add(viewButtonColumn);
    }

    private void UpdateFilterCount()
    {
        // Get the current filtered row count
        var currentCount = dataGridViewTranscripts.Rows.Count;

        toolStripLabelCount.Text = currentCount == _totalRecordCount
            ? $"{_totalRecordCount} transcript{(_totalRecordCount != 1 ? "s" : "")}"
            : $"{currentCount} of {_totalRecordCount} transcript{(_totalRecordCount != 1 ? "s" : "")}";
    }

    private async Task LoadTranscriptsAsync()
    {
        try
        {
            var transcripts = await _transcriptService.GetAllTranscriptsAsync();
            var viewModels = transcripts
                .OrderByDescending(t => t.Date)
                .Select(MeetingTranscriptViewModel.FromMeetingTranscript)
                .ToList();

            // Convert to DataTable for AutoFilter compatibility
            var dataTable = new DataTable();
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("IsSelected", typeof(bool));
            dataTable.Columns["IsSelected"].AllowDBNull = true;
            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("Date", typeof(DateTime));
            dataTable.Columns.Add("Participants", typeof(string));
            dataTable.Columns.Add("ApiKeyNames", typeof(string));
            dataTable.Columns.Add("LengthInMinutes", typeof(int));
            dataTable.Columns.Add("TranscriptLength", typeof(int));

            foreach (var vm in viewModels)
            {
                // Convert UTC date to local timezone for display
                DateTime? localDate = null;
                if (vm.Date.HasValue)
                {
                    localDate = DateTime.SpecifyKind(vm.Date.Value, DateTimeKind.Utc).ToLocalTime();
                }

                dataTable.Rows.Add(
                    vm.Id,
                    vm.IsSelected.HasValue ? (object)vm.IsSelected.Value : DBNull.Value,
                    vm.Title ?? string.Empty,
                    localDate.HasValue ? (object)localDate.Value : DBNull.Value,
                    vm.Participants ?? string.Empty,
                    vm.ApiKeyNames ?? string.Empty,
                    vm.LengthInMinutes.HasValue ? (object)vm.LengthInMinutes.Value : DBNull.Value,
                    vm.TranscriptLength.HasValue ? (object)vm.TranscriptLength.Value : DBNull.Value
                );
            }

            dataGridViewTranscripts.DataSource = dataTable;

            // Store total count for filter tracking
            _totalRecordCount = transcripts.Count;
            toolStripLabelCount.Text = $"{_totalRecordCount} transcript{(_totalRecordCount != 1 ? "s" : "")}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading transcripts: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void toolStripButtonRefresh_Click(object sender, EventArgs e)
    {
        await LoadTranscriptsAsync();
    }

    private void apiKeysToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using (var apiKeysForm = new FellowApiKeys())
        {
            apiKeysForm.ShowDialog(this);
        }
    }

    private void googleAuthToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using (var googleAuthForm = new GoogleAuthForm())
        {
            googleAuthForm.ShowDialog(this);
        }
    }

    private void helpToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using (var helpForm = new HelpForm())
        {
            helpForm.ShowDialog(this);
        }
    }

    private async void downloadToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            downloadToolStripMenuItem.Enabled = false;
            Cursor = Cursors.WaitCursor;

            // Load workspace configuration
            var workspace = await _configurationService.GetFellowWorkspaceAsync();
            if (string.IsNullOrWhiteSpace(workspace))
            {
                MessageBox.Show("Workspace not configured. Please configure your workspace in API Keys settings.", "Configuration Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var apiKeys = await _apiKeyService.GetEnabledApiKeysAsync();

            if (apiKeys.Count == 0)
            {
                MessageBox.Show("No enabled API keys found. Please add API keys first.", "No API Keys",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create FellowApiService with workspace
            var fellowApiService = new FellowApiService(workspace);
            var downloadService = new FellowDownloadService(_transcriptService, fellowApiService, _apiKeyService);

            var progressForm = new ProgressForm();
            progressForm.Show(this);

            var totalProcessed = 0;
            var totalCreated = 0;
            var totalSkipped = 0;
            var totalErrors = 0;
            var wasCancelled = false;

            try
            {
                foreach (var apiKey in apiKeys)
                {
                    try
                    {
                        progressForm.UpdateStatus($"Processing API Key: {apiKey.Name}");

                        var progress = new Progress<DownloadProgress>(p =>
                        {
                            progressForm.AppendLog($"  {p.Message}");
                        });

                        var summary = await downloadService.DownloadTranscriptsForApiKeyAsync(
                            apiKey, progress, progressForm.CancellationToken);

                        totalProcessed += summary.Processed;
                        totalCreated += summary.Created;
                        totalSkipped += summary.Skipped;
                        totalErrors += summary.Errors;
                    }
                    catch (OperationCanceledException)
                    {
                        wasCancelled = true;
                        progressForm.AppendLog($"Download cancelled by user.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        progressForm.AppendLog($"Error processing API key '{apiKey.Name}': {ex.Message}");
                        totalErrors++;
                    }
                }

                // Determine final status
                string statusTitle;
                string statusMessage;
                MessageBoxIcon statusIcon;

                if (wasCancelled)
                {
                    statusTitle = "Download Cancelled";
                    statusMessage = "Download was cancelled by user.";
                    statusIcon = MessageBoxIcon.Warning;
                    progressForm.UpdateStatus("Download Cancelled");
                }
                else if (totalErrors > 0 && totalCreated == 0)
                {
                    statusTitle = "Download Failed";
                    statusMessage = "Download failed with errors.";
                    statusIcon = MessageBoxIcon.Error;
                    progressForm.UpdateStatus("Download Failed");
                }
                else if (totalErrors > 0)
                {
                    statusTitle = "Download Completed with Errors";
                    statusMessage = "Download completed but some items failed.";
                    statusIcon = MessageBoxIcon.Warning;
                    progressForm.UpdateStatus("Download Completed with Errors");
                }
                else
                {
                    statusTitle = "Download Complete";
                    statusMessage = "Download completed successfully!";
                    statusIcon = MessageBoxIcon.Information;
                    progressForm.UpdateStatus("Download Complete!");
                }

                progressForm.AppendLog($"\nSummary:");
                progressForm.AppendLog($"Total Processed: {totalProcessed}");
                progressForm.AppendLog($"Total Created/Updated: {totalCreated}");
                progressForm.AppendLog($"Total Skipped: {totalSkipped}");
                progressForm.AppendLog($"Total Errors: {totalErrors}");

                MessageBox.Show(
                    $"{statusMessage}\n\nProcessed: {totalProcessed}\nCreated/Updated: {totalCreated}\nSkipped: {totalSkipped}\nErrors: {totalErrors}",
                    statusTitle,
                    MessageBoxButtons.OK,
                    statusIcon);

                // Refresh the grid
                await LoadTranscriptsAsync();
            }
            finally
            {
                progressForm.Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during download: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            downloadToolStripMenuItem.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private async void dataGridViewTranscripts_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        // Handle single checkbox value changes (multi-selection is handled in CellContentClick)
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            var column = dataGridViewTranscripts.Columns[e.ColumnIndex];
            if (column.Name == "IsSelected" && column is DataGridViewCheckBoxColumn)
            {
                // Only handle single row updates here
                var selectedRows = dataGridViewTranscripts.SelectedRows.Cast<DataGridViewRow>().ToList();
                var selectedCells = dataGridViewTranscripts.SelectedCells.Cast<DataGridViewCell>()
                    .Select(c => c.OwningRow)
                    .Distinct()
                    .ToList();

                var rowsSelected = selectedRows.Count > 0 ? selectedRows : selectedCells;

                // Skip if multiple rows are selected (handled by CellContentClick)
                if (rowsSelected.Count > 1 && rowsSelected.Any(r => r.Index == e.RowIndex))
                    return;

                // Single row update
                var idCell = dataGridViewTranscripts.Rows[e.RowIndex].Cells["Id"];
                var isSelectedCell = dataGridViewTranscripts.Rows[e.RowIndex].Cells["IsSelected"];

                if (idCell.Value != null && idCell.Value != DBNull.Value)
                {
                    var id = Convert.ToInt32(idCell.Value);
                    bool? newValue = isSelectedCell.Value == DBNull.Value || isSelectedCell.Value == null
                        ? null
                        : Convert.ToBoolean(isSelectedCell.Value);

                    await _transcriptService.UpdateTranscriptSelectionAsync(id, newValue);
                }
            }
        }
    }

    private async void toolStripButtonDeleteAll_Click(object sender, EventArgs e)
    {
        try
        {
            var count = await _transcriptService.GetTranscriptCountAsync();

            if (count == 0)
            {
                MessageBox.Show("No transcripts to delete.", "Delete All",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete all {count} transcript{(count != 1 ? "s" : "")}?\n\nThis action cannot be undone.",
                "Confirm Delete All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                deleteAllToolStripMenuItem.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var deletedCount = await _transcriptService.DeleteAllTranscriptsAsync();

                MessageBox.Show($"Successfully deleted {deletedCount} transcript{(deletedCount != 1 ? "s" : "")}.",
                    "Delete Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await LoadTranscriptsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting transcripts: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            deleteAllToolStripMenuItem.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private async void dataGridViewTranscripts_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        // Handle View icon clicks
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            var column = dataGridViewTranscripts.Columns[e.ColumnIndex];

            if (column.Name == "ViewButton")
            {
                await ShowTranscriptViewerAsync(e.RowIndex);
            }
        }
    }


    private async void downloadGoogleDriveToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            downloadGoogleDriveToolStripMenuItem.Enabled = false;
            Cursor = Cursors.WaitCursor;

            // Check if authenticated with Google
            var googleAuthService = new GoogleAuthService();
            var isAuthenticated = await googleAuthService.IsAuthenticatedAsync();

            if (!isAuthenticated)
            {
                MessageBox.Show("Not authenticated with Google. Please authenticate using OAuth Flow in Google Auth settings.", "Authentication Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if browser cookies are available
            var hasBrowserAuth = await GoogleAuthForm.HasBrowserAuthenticationAsync();
            if (!hasBrowserAuth)
            {
                MessageBox.Show("Browser authentication required. Please authenticate using the Browser Auth button in Google Auth settings.", "Browser Auth Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create services
            var driveApiService = new GoogleDriveApiService(googleAuthService, _configurationService);
            var transcriptDownloader = new GoogleDrivePlaywrightService(_configurationService);
            var downloadService = new GoogleDriveService(
                driveApiService,
                transcriptDownloader,
                _transcriptService);

            var progressForm = new ProgressForm();
            progressForm.Show(this);

            var totalProcessed = 0;
            var totalCreated = 0;
            var totalSkipped = 0;
            var totalErrors = 0;
            var wasCancelled = false;

            try
            {
                progressForm.UpdateStatus("Downloading from Google Drive...");

                var progress = new Progress<DownloadProgress>(p =>
                {
                    progressForm.AppendLog(p.Message);
                });

                var summary = await downloadService.DownloadTranscriptsAsync(
                    progress, progressForm.CancellationToken);

                totalProcessed = summary.Processed;
                totalCreated = summary.Created;
                totalSkipped = summary.Skipped;
                totalErrors = summary.Errors;
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                progressForm.AppendLog("Download cancelled by user.");
            }
            catch (Exception ex)
            {
                progressForm.AppendLog($"Error: {ex.Message}");
                totalErrors++;
            }

            // Determine final status
            string statusTitle;
            string statusMessage;
            MessageBoxIcon statusIcon;

            if (wasCancelled)
            {
                statusTitle = "Download Cancelled";
                statusMessage = "Download was cancelled by user.";
                statusIcon = MessageBoxIcon.Warning;
                progressForm.UpdateStatus("Download Cancelled");
            }
            else if (totalErrors > 0 && totalCreated == 0)
            {
                statusTitle = "Download Failed";
                statusMessage = "Download failed with errors.";
                statusIcon = MessageBoxIcon.Error;
                progressForm.UpdateStatus("Download Failed");
            }
            else if (totalErrors > 0)
            {
                statusTitle = "Download Completed with Errors";
                statusMessage = "Download completed but some items failed.";
                statusIcon = MessageBoxIcon.Warning;
                progressForm.UpdateStatus("Download Completed with Errors");
            }
            else
            {
                statusTitle = "Download Complete";
                statusMessage = "Download completed successfully!";
                statusIcon = MessageBoxIcon.Information;
                progressForm.UpdateStatus("Download Complete!");
            }

            progressForm.AppendLog($"\nSummary:");
            progressForm.AppendLog($"Total Processed: {totalProcessed}");
            progressForm.AppendLog($"Total Created/Updated: {totalCreated}");
            progressForm.AppendLog($"Total Skipped: {totalSkipped}");
            progressForm.AppendLog($"Total Errors: {totalErrors}");

            MessageBox.Show(
                $"{statusMessage}\n\nProcessed: {totalProcessed}\nCreated/Updated: {totalCreated}\nSkipped: {totalSkipped}\nErrors: {totalErrors}",
                statusTitle,
                MessageBoxButtons.OK,
                statusIcon);

            // Refresh the grid
            await LoadTranscriptsAsync();

            progressForm.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during download: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            downloadGoogleDriveToolStripMenuItem.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private async void exportSelectedToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            // Check for undecided transcripts
            var hasUndecided = await _transcriptService.HasUndecidedTranscriptsAsync();
            if (hasUndecided)
            {
                MessageBox.Show("Some transcripts have not been marked for export or exclusion (undecided state).\n\nPlease review all transcripts and mark them as either included or excluded before exporting.",
                    "Undecided Transcripts",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedTranscripts = await _transcriptService.GetSelectedTranscriptsAsync();

            if (selectedTranscripts.Count == 0)
            {
                MessageBox.Show("No transcripts selected. Please select transcripts to export.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Zip files (*.zip)|*.zip";
                saveFileDialog.Title = "Export Selected Transcripts";
                saveFileDialog.FileName = $"transcripts_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    exportSelectedToolStripMenuItem.Enabled = false;
                    Cursor = Cursors.WaitCursor;

                    await _exportService.ExportTranscriptsToZipAsync(selectedTranscripts, saveFileDialog.FileName);

                    MessageBox.Show($"Successfully exported {selectedTranscripts.Count} transcript{(selectedTranscripts.Count != 1 ? "s" : "")} to:\n{saveFileDialog.FileName}",
                        "Export Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting transcripts: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            exportSelectedToolStripMenuItem.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private async Task ShowTranscriptViewerAsync(int rowIndex)
    {
        try
        {
            var idCell = dataGridViewTranscripts.Rows[rowIndex].Cells["Id"];
            if (idCell.Value == null || idCell.Value == DBNull.Value)
                return;

            var id = Convert.ToInt32(idCell.Value);

            // Load the full transcript from database
            var transcript = await _transcriptService.GetTranscriptByIdAsync(id);
            if (transcript == null)
            {
                MessageBox.Show("Transcript not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var viewerForm = new TranscriptViewerForm(transcript))
            {
                viewerForm.ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error viewing transcript: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DataGridViewTranscripts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex >= 0 && e.RowIndex < dataGridViewTranscripts.Rows.Count)
        {
            var row = dataGridViewTranscripts.Rows[e.RowIndex];
            var isSelectedCell = row.Cells["IsSelected"];

            if (isSelectedCell.Value != null && isSelectedCell.Value != DBNull.Value)
            {
                bool isSelected = Convert.ToBoolean(isSelectedCell.Value);

                // Light green for selected (true), light red for not selected (false)
                e.CellStyle.BackColor = isSelected ? Color.LightGreen : Color.LightCoral;
                
                // Use darker selection colors that still show the background tint
                e.CellStyle.SelectionBackColor = isSelected 
                    ? Color.FromArgb(100, 200, 100)  // Darker green
                    : Color.FromArgb(200, 100, 100); // Darker red
                e.CellStyle.SelectionForeColor = Color.Black;
            }
        }
    }

    private async void DataGridViewTranscripts_KeyDown(object? sender, KeyEventArgs e)
    {
        // Handle keyboard shortcuts for batch editing
        bool? newValue = null;
        bool shouldUpdate = false;

        if (e.KeyCode == Keys.Space)
        {
            // Space = set to true
            newValue = true;
            shouldUpdate = true;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.X)
        {
            // X = set to false
            newValue = false;
            shouldUpdate = true;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.R)
        {
            // R = set to null (undecided/reset)
            newValue = null;
            shouldUpdate = true;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        if (shouldUpdate)
        {
            await UpdateSelectedRowsAsync(newValue);
        }
    }

    private async Task UpdateSelectedRowsAsync(bool? newValue)
    {
        // Get selected rows
        var selectedRows = dataGridViewTranscripts.SelectedRows.Cast<DataGridViewRow>().ToList();
        var selectedCells = dataGridViewTranscripts.SelectedCells.Cast<DataGridViewCell>()
            .Select(c => c.OwningRow)
            .Distinct()
            .ToList();

        // Use whichever selection method has more rows
        var rowsToUpdate = selectedRows.Count > 0 ? selectedRows : selectedCells;

        if (rowsToUpdate.Count == 0)
            return;

        // Get the DataTable
        var dataTable = dataGridViewTranscripts.DataSource as DataTable;
        if (dataTable == null)
            return;

        // Update all selected rows
        foreach (var row in rowsToUpdate)
        {
            var idCell = row.Cells["Id"];
            if (idCell.Value != null && idCell.Value != DBNull.Value)
            {
                var id = Convert.ToInt32(idCell.Value);

                // Find the DataRow by ID (not by index, since grid may be sorted)
                var dataRow = dataTable.AsEnumerable().FirstOrDefault(r => r.Field<int>("Id") == id);
                if (dataRow != null)
                {
                    dataRow["IsSelected"] = newValue.HasValue ? (object)newValue.Value : DBNull.Value;
                }

                // Update the database
                await _transcriptService.UpdateTranscriptSelectionAsync(id, newValue);
            }
        }

        // Refresh the grid to show updated colors
        dataGridViewTranscripts.Refresh();
    }
}
