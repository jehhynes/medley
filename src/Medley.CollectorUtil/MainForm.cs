using Medley.CollectorUtil.Data;
using Medley.CollectorUtil.Services;
using System.ComponentModel;
using System.Data;
using System.IO.Compression;
using System.Text.Json;
using Zuby.ADGV;

namespace Medley.CollectorUtil;

public partial class MainForm : Form
{
    private readonly ApiKeyService _apiKeyService;
    private readonly MeetingTranscriptService _transcriptService;
    private readonly ConfigurationService _configurationService;
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

        // Wire up filter event - use BeginInvoke to ensure count updates after filter is applied
        dataGridViewTranscripts.FilterStringChanged += (s, e) =>
        {
            BeginInvoke(new Action(UpdateFilterCount));
        };

        // Wire up column header click for sorting through ADGV
        dataGridViewTranscripts.SortStringChanged += DataGridViewTranscripts_SortStringChanged;
        dataGridViewTranscripts.ColumnHeaderMouseClick += DataGridViewTranscripts_ColumnHeaderMouseClick;
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

        // Skip the hidden Id column only
        if (column.Name == "Id") return;

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
            HeaderText = "Selected",
            Name = "IsSelected",
            FillWeight = 10,
            ReadOnly = false,
            SortMode = DataGridViewColumnSortMode.Automatic
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

        // Add View button column with icon
        var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        _viewIcon = (Image?)resources.GetObject("icon-search-20");
        
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
            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("Date", typeof(DateTime));
            dataTable.Columns.Add("Participants", typeof(string));
            dataTable.Columns.Add("ApiKeyNames", typeof(string));

            foreach (var vm in viewModels)
            {
                dataTable.Rows.Add(
                    vm.Id,
                    vm.IsSelected,
                    vm.Title ?? string.Empty,
                    vm.Date.HasValue ? (object)vm.Date.Value : DBNull.Value,
                    vm.Participants ?? string.Empty,
                    vm.ApiKeyNames ?? string.Empty
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

    private async void downloadToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            downloadToolStripMenuItem.Enabled = false;
            Cursor = Cursors.WaitCursor;

            // Load workspace configuration
            var workspace = await _configurationService.GetWorkspaceAsync();
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

            var progressForm = new ProgressForm();
            progressForm.Show(this);

            var totalProcessed = 0;
            var totalCreated = 0;
            var totalSkipped = 0;
            var totalErrors = 0;

            foreach (var apiKey in apiKeys)
            {
                try
                {
                    progressForm.UpdateStatus($"Processing API Key: {apiKey.Name}");

                    var (processed, created, skipped, errors) = await DownloadTranscriptsForApiKeyAsync(
                        apiKey, fellowApiService, progressForm);

                    totalProcessed += processed;
                    totalCreated += created;
                    totalSkipped += skipped;
                    totalErrors += errors;
                }
                catch (Exception ex)
                {
                    progressForm.AppendLog($"Error processing API key '{apiKey.Name}': {ex.Message}");
                    totalErrors++;
                }
            }

            progressForm.UpdateStatus("Download Complete!");
            progressForm.AppendLog($"\nSummary:");
            progressForm.AppendLog($"Total Processed: {totalProcessed}");
            progressForm.AppendLog($"Total Created/Updated: {totalCreated}");
            progressForm.AppendLog($"Total Skipped: {totalSkipped}");
            progressForm.AppendLog($"Total Errors: {totalErrors}");

            MessageBox.Show(
                $"Download completed!\n\nProcessed: {totalProcessed}\nCreated/Updated: {totalCreated}\nSkipped: {totalSkipped}\nErrors: {totalErrors}",
                "Download Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Refresh the grid
            await LoadTranscriptsAsync();
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

    private async Task<(int processed, int created, int skipped, int errors)> DownloadTranscriptsForApiKeyAsync(
        ApiKey apiKey, FellowApiService fellowApiService, ProgressForm progressForm)
    {
        var processed = 0;
        var created = 0;
        var skipped = 0;
        var errors = 0;
        string? cursor = null;
        var pageNumber = 0;

        do
        {
            pageNumber++;
            progressForm.AppendLog($"  Fetching page {pageNumber} for '{apiKey.Name}'...");

            try
            {
                var response = await fellowApiService.ListRecordingsAsync(
                    apiKey.Key, cursor, pageSize: 20, includeTranscript: true);

                if (response?.Recordings?.Data == null || response.Recordings.Data.Count == 0)
                {
                    progressForm.AppendLog($"  No more recordings found for '{apiKey.Name}'");
                    break;
                }

                foreach (var recording in response.Recordings.Data)
                {
                    processed++;

                    try
                    {
                        if (string.IsNullOrWhiteSpace(recording.Id))
                        {
                            progressForm.AppendLog($"    Skipping recording with no ID");
                            skipped++;
                            continue;
                        }

                        // Skip very recent meetings (within 2 hours)
                        if (recording.StartedAt != null && recording.StartedAt.Value > DateTimeOffset.Now.AddHours(-2))
                        {
                            progressForm.AppendLog($"    Skipping recent meeting: {recording.Title}");
                            skipped++;
                            continue;
                        }

                        // Check if already exists for this API key
                        var alreadyExists = await _transcriptService.TranscriptExistsForApiKeyAsync(
                            recording.Id, apiKey.Id);

                        if (alreadyExists)
                        {
                            progressForm.AppendLog($"    Already exists for this API key: {recording.Title}");
                            skipped++;
                            continue;
                        }

                        // Extract participants from transcript speakers
                        // Strip letter suffixes like " - A", " - B", etc.
                        var participants = recording.Transcript?.SpeechSegments != null
                            ? string.Join(", ", recording.Transcript.SpeechSegments
                                .Where(s => !string.IsNullOrWhiteSpace(s.Speaker))
                                .Select(s => System.Text.RegularExpressions.Regex.Replace(s.Speaker!, @"\s*-\s*[A-Z]$", ""))
                                .Distinct()
                                .OrderBy(s => s))
                            : null;

                        // Serialize full JSON - save the raw response content
                        var fullJson = JsonSerializer.Serialize(recording, new JsonSerializerOptions
                        {
                            WriteIndented = false
                        });

                        var transcript = new MeetingTranscript
                        {
                            Title = recording.Title ?? "Untitled Meeting",
                            MeetingId = recording.Id,
                            Date = recording.StartedAt?.DateTime,
                            Participants = participants,
                            FullJson = fullJson
                        };

                        await _transcriptService.SaveTranscriptAsync(transcript, apiKey);
                        created++;

                        progressForm.AppendLog($"    Saved: {recording.Title}");
                    }
                    catch (Exception ex)
                    {
                        progressForm.AppendLog($"    Error processing recording: {ex.Message}");
                        errors++;
                    }
                }

                progressForm.AppendLog($"  Page {pageNumber} complete: {response.Recordings.Data.Count} recordings");

                cursor = response.Recordings.PageInfo?.Cursor;
            }
            catch (Exception ex)
            {
                progressForm.AppendLog($"  Error fetching page {pageNumber}: {ex.Message}");
                errors++;
                break;
            }

        } while (!string.IsNullOrWhiteSpace(cursor));

        progressForm.AppendLog($"Completed '{apiKey.Name}': Processed={processed}, Created={created}, Skipped={skipped}, Errors={errors}");

        return (processed, created, skipped, errors);
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

                if (idCell.Value != null && idCell.Value != DBNull.Value &&
                    isSelectedCell.Value != null && isSelectedCell.Value != DBNull.Value)
                {
                    var id = Convert.ToInt32(idCell.Value);
                    var newValue = Convert.ToBoolean(isSelectedCell.Value);

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

    private async void dataGridViewTranscripts_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
        // Handle checkbox clicks for multi-selection
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            var column = dataGridViewTranscripts.Columns[e.ColumnIndex];
            
            if (column.Name == "IsSelected" && column is DataGridViewCheckBoxColumn)
            {
                // Get the current value (before it changes)
                var clickedCell = dataGridViewTranscripts.Rows[e.RowIndex].Cells["IsSelected"];
                var currentValue = clickedCell.Value != null && clickedCell.Value != DBNull.Value
                    ? Convert.ToBoolean(clickedCell.Value)
                    : false;

                // The new value will be the opposite
                var newValue = !currentValue;

                // Check if multiple rows are selected
                var selectedRows = dataGridViewTranscripts.SelectedRows.Cast<DataGridViewRow>().ToList();
                var selectedCells = dataGridViewTranscripts.SelectedCells.Cast<DataGridViewCell>()
                    .Select(c => c.OwningRow)
                    .Distinct()
                    .ToList();

                // Use whichever selection method has more rows
                var rowsToUpdate = selectedRows.Count > 0 ? selectedRows : selectedCells;

                // If multiple rows are selected and the clicked row is among them, update all
                if (rowsToUpdate.Count > 1 && rowsToUpdate.Any(r => r.Index == e.RowIndex))
                {
                    // Get the DataTable
                    var dataTable = dataGridViewTranscripts.DataSource as DataTable;
                    if (dataTable != null)
                    {
                        foreach (var row in rowsToUpdate)
                        {
                            var idCell = row.Cells["Id"];
                            var isSelectedCell = row.Cells["IsSelected"];

                            if (idCell.Value != null && idCell.Value != DBNull.Value)
                            {
                                var id = Convert.ToInt32(idCell.Value);

                                // Find the DataRow by ID (not by index, since grid may be sorted)
                                var dataRow = dataTable.AsEnumerable().FirstOrDefault(r => r.Field<int>("Id") == id);
                                if (dataRow != null)
                                {
                                    dataRow["IsSelected"] = newValue;
                                }

                                // Update the database
                                await _transcriptService.UpdateTranscriptSelectionAsync(id, newValue);
                            }
                        }
                    }
                }
            }
        }
    }

    private async void exportSelectedToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
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

                    await ExportTranscriptsToZipAsync(selectedTranscripts, saveFileDialog.FileName);

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

    private async Task ExportTranscriptsToZipAsync(List<MeetingTranscript> transcripts, string zipFilePath)
    {
        await Task.Run(() =>
        {
            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var transcript in transcripts)
                {
                    if (string.IsNullOrWhiteSpace(transcript.FullJson))
                        continue;

                    // Create a safe filename from meeting ID and title
                    var safeTitle = string.IsNullOrWhiteSpace(transcript.Title)
                        ? "untitled"
                        : string.Join("_", transcript.Title.Split(Path.GetInvalidFileNameChars()));

                    var fileName = $"{transcript.MeetingId}_{safeTitle}.json";

                    // Ensure filename isn't too long (max 255 chars for most filesystems)
                    if (fileName.Length > 200)
                    {
                        fileName = fileName.Substring(0, 200) + ".json";
                    }

                    var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);

                    using (var entryStream = entry.Open())
                    using (var writer = new StreamWriter(entryStream))
                    {
                        // Pretty print the JSON for readability
                        var jsonDocument = JsonDocument.Parse(transcript.FullJson);
                        var prettyJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        writer.Write(prettyJson);
                    }
                }
            }
        });
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
}
