using Medley.CollectorUtil.Data;
using Medley.CollectorUtil.Services;
using System.Text.Json;

namespace Medley.CollectorUtil;

public partial class MainForm : Form
{
    private readonly ApiKeyService _apiKeyService;
    private readonly MeetingTranscriptService _transcriptService;
    private readonly ConfigurationService _configurationService;

    public MainForm()
    {
        InitializeComponent();
        _apiKeyService = new ApiKeyService();
        _transcriptService = new MeetingTranscriptService();
        _configurationService = new ConfigurationService();
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        SetupDataGridView();
        await LoadTranscriptsAsync();
    }

    private void SetupDataGridView()
    {
        dataGridViewTranscripts.AutoGenerateColumns = false;
        dataGridViewTranscripts.Columns.Clear();

        dataGridViewTranscripts.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = "IsSelected",
            HeaderText = "Selected",
            Name = "IsSelected",
            FillWeight = 10,
            ReadOnly = false
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Title",
            HeaderText = "Title",
            Name = "Title",
            FillWeight = 30,
            ReadOnly = true
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Date",
            HeaderText = "Date",
            Name = "Date",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "g" },
            ReadOnly = true
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Participants",
            HeaderText = "Participants",
            Name = "Participants",
            FillWeight = 25,
            ReadOnly = true
        });

        dataGridViewTranscripts.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ApiKeyNames",
            HeaderText = "API Keys",
            Name = "ApiKeyNames",
            FillWeight = 20,
            ReadOnly = true
        });
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
            dataGridViewTranscripts.DataSource = viewModels;
            toolStripLabelCount.Text = $"{transcripts.Count} transcript{(transcripts.Count != 1 ? "s" : "")}";
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

    private async void dataGridViewTranscripts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        // Handle checkbox value changes
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            var column = dataGridViewTranscripts.Columns[e.ColumnIndex];
            if (column.Name == "IsSelected" && column is DataGridViewCheckBoxColumn)
            {
                var viewModel = dataGridViewTranscripts.Rows[e.RowIndex].DataBoundItem as MeetingTranscriptViewModel;
                if (viewModel != null)
                {
                    var newValue = (bool)dataGridViewTranscripts.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    viewModel.IsSelected = newValue;

                    // Update the database
                    await _transcriptService.UpdateTranscriptSelectionAsync(viewModel.Id, newValue);
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
}
