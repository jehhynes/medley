using Medley.CollectorUtil.Data;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Medley.CollectorUtil.Services;

public class TranscriptDownloadService
{
    private readonly MeetingTranscriptService _transcriptService;
    private readonly FellowApiService _fellowApiService;
    private readonly ApiKeyService _apiKeyService;
    private const int MaxRetries = 3;
    private const int InitialDelayMs = 1000;

    public TranscriptDownloadService(
        MeetingTranscriptService transcriptService,
        FellowApiService fellowApiService,
        ApiKeyService apiKeyService)
    {
        _transcriptService = transcriptService;
        _fellowApiService = fellowApiService;
        _apiKeyService = apiKeyService;
    }

    public async Task<DownloadSummary> DownloadTranscriptsForApiKeyAsync(
        ApiKey apiKey,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var summary = new DownloadSummary();
        string? cursor = null;
        var pageNumber = 0;

        try
        {
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                pageNumber++;
                progress?.Report(new DownloadProgress
                {
                    ApiKeyName = apiKey.Name,
                    Message = $"Fetching page {pageNumber} for '{apiKey.Name}'..."
                });

                try
                {
                    var response = await FetchPageWithRetryAsync(apiKey, cursor, pageNumber, progress, cancellationToken);

                    if (response?.Recordings?.Data == null || response.Recordings.Data.Count == 0)
                    {
                        progress?.Report(new DownloadProgress
                        {
                            ApiKeyName = apiKey.Name,
                            Message = $"No more recordings found for '{apiKey.Name}'"
                        });
                        break;
                    }

                    foreach (var recording in response.Recordings.Data)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        summary.Processed++;

                        try
                        {
                            if (string.IsNullOrWhiteSpace(recording.Id))
                            {
                                progress?.Report(new DownloadProgress
                                {
                                    ApiKeyName = apiKey.Name,
                                    Message = "Skipping recording with no ID"
                                });
                                summary.Skipped++;
                                continue;
                            }

                            // Skip future meetings or meetings that started less than 2 hours ago
                            if (recording.StartedAt != null)
                            {
                                var now = DateTime.UtcNow;
                                var twoHoursAgo = now.AddHours(-2);

                                if (recording.StartedAt.Value > now)
                                {
                                    progress?.Report(new DownloadProgress
                                    {
                                        ApiKeyName = apiKey.Name,
                                        Message = $"Skipping future meeting: {recording.Title}"
                                    });
                                    summary.Skipped++;
                                    continue;
                                }

                                if (recording.StartedAt.Value > twoHoursAgo)
                                {
                                    progress?.Report(new DownloadProgress
                                    {
                                        ApiKeyName = apiKey.Name,
                                        Message = $"Skipping recent meeting (started less than 2 hours ago): {recording.Title}"
                                    });
                                    summary.Skipped++;
                                    continue;
                                }
                            }

                            // Process recording with retry logic
                            var result = await ProcessRecordingWithRetryAsync(
                                recording, apiKey, progress, cancellationToken);

                            switch (result)
                            {
                                case ProcessingResult.Created:
                                    summary.Created++;
                                    break;
                                case ProcessingResult.Skipped:
                                    summary.Skipped++;
                                    break;
                                case ProcessingResult.Error:
                                    summary.Errors++;
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            progress?.Report(new DownloadProgress
                            {
                                ApiKeyName = apiKey.Name,
                                Message = $"Error processing recording '{recording.Title}': {ex.Message}"
                            });
                            summary.Errors++;
                        }
                    }

                    progress?.Report(new DownloadProgress
                    {
                        ApiKeyName = apiKey.Name,
                        Message = $"Page {pageNumber} complete: {response.Recordings.Data.Count} recordings"
                    });

                    cursor = response.Recordings.PageInfo?.Cursor;
                }
                catch (Exception ex)
                {
                    progress?.Report(new DownloadProgress
                    {
                        ApiKeyName = apiKey.Name,
                        Message = $"Failed to fetch page {pageNumber} after {MaxRetries} retries: {ex.Message}"
                    });
                    summary.Errors++;
                    // Continue to next page instead of breaking
                    cursor = null;
                }

            } while (!string.IsNullOrWhiteSpace(cursor));

            progress?.Report(new DownloadProgress
            {
                ApiKeyName = apiKey.Name,
                Message = $"Completed '{apiKey.Name}': Processed={summary.Processed}, Created={summary.Created}, Skipped={summary.Skipped}, Errors={summary.Errors}"
            });

            // Mark API key as inactive if all downloads completed successfully (no errors)
            if (summary.Errors == 0 && summary.Processed > 0)
            {
                await _apiKeyService.SetApiKeyEnabledAsync(apiKey.Id, false);
                progress?.Report(new DownloadProgress
                {
                    ApiKeyName = apiKey.Name,
                    Message = $"API key '{apiKey.Name}' marked as inactive after successful download"
                });
            }
        }
        catch (OperationCanceledException)
        {
            summary.WasCancelled = true;
            progress?.Report(new DownloadProgress
            {
                ApiKeyName = apiKey.Name,
                Message = $"Cancelled '{apiKey.Name}': Processed={summary.Processed}, Created={summary.Created}, Skipped={summary.Skipped}, Errors={summary.Errors}"
            });
            throw;
        }

        return summary;
    }

    private async Task<FellowRecordingsResponse?> FetchPageWithRetryAsync(
        ApiKey apiKey,
        string? cursor,
        int pageNumber,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await _fellowApiService.ListRecordingsAsync(apiKey.Key, cursor);
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                var delayMs = InitialDelayMs * (int)Math.Pow(2, attempt - 1);
                progress?.Report(new DownloadProgress
                {
                    ApiKeyName = apiKey.Name,
                    Message = $"Error fetching page {pageNumber} (attempt {attempt}/{MaxRetries}): {ex.Message}. Retrying in {delayMs}ms..."
                });
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        // If all retries failed, throw the exception
        throw new Exception($"Failed to fetch page {pageNumber} after {MaxRetries} attempts");
    }

    private async Task<ProcessingResult> ProcessRecordingWithRetryAsync(
        FellowRecording recording,
        ApiKey apiKey,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // Check if already exists for this API key
                var alreadyExists = await _transcriptService.TranscriptExistsForApiKeyAsync(
                    recording.Id!, apiKey.Id);

                if (alreadyExists)
                {
                    progress?.Report(new DownloadProgress
                    {
                        ApiKeyName = apiKey.Name,
                        Message = $"Already exists for this API key: {recording.Title}"
                    });
                    return ProcessingResult.Skipped;
                }

                var transcript = CreateTranscriptFromRecording(recording);
                await _transcriptService.SaveTranscriptAsync(transcript, apiKey);

                progress?.Report(new DownloadProgress
                {
                    ApiKeyName = apiKey.Name,
                    Message = $"Saved: {recording.Title}"
                });

                return ProcessingResult.Created;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                var delayMs = InitialDelayMs * (int)Math.Pow(2, attempt - 1);
                progress?.Report(new DownloadProgress
                {
                    ApiKeyName = apiKey.Name,
                    Message = $"Error saving '{recording.Title}' (attempt {attempt}/{MaxRetries}): {ex.Message}. Retrying in {delayMs}ms..."
                });
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (Exception ex)
            {
                progress?.Report(new DownloadProgress
                {
                    ApiKeyName = apiKey.Name,
                    Message = $"Failed to save '{recording.Title}' after {MaxRetries} attempts: {ex.Message}"
                });
                return ProcessingResult.Error;
            }
        }

        return ProcessingResult.Error;
    }

    private MeetingTranscript CreateTranscriptFromRecording(FellowRecording recording)
    {
        // Extract participants from transcript speakers
        // Strip letter suffixes like " - A", " - B", etc.
        var participants = recording.Transcript?.SpeechSegments != null
            ? string.Join(", ", recording.Transcript.SpeechSegments
                .Where(s => !string.IsNullOrWhiteSpace(s.Speaker))
                .Select(s => Regex.Replace(s.Speaker!, @"\s*-\s*[A-Z]$", ""))
                .Distinct()
                .OrderBy(s => s))
            : null;

        // Calculate length in minutes from StartedAt and EndedAt
        int? lengthInMinutes = null;
        if (recording.StartedAt.HasValue && recording.EndedAt.HasValue)
        {
            var duration = recording.EndedAt.Value - recording.StartedAt.Value;
            lengthInMinutes = (int)Math.Round(duration.TotalMinutes);
        }

        // Calculate transcript length (character count of all text)
        int? transcriptLength = null;
        if (recording.Transcript?.SpeechSegments != null)
        {
            transcriptLength = recording.Transcript.SpeechSegments
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .Sum(s => s.Text!.Length);
        }

        // Serialize full JSON - save the raw response content
        var fullJson = JsonSerializer.Serialize(recording, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        return new MeetingTranscript
        {
            Title = recording.Title ?? "Untitled Meeting",
            MeetingId = recording.Id!,
            Date = recording.StartedAt,
            Participants = participants,
            LengthInMinutes = lengthInMinutes,
            TranscriptLength = transcriptLength,
            FullJson = fullJson
        };
    }
}

public class DownloadProgress
{
    public string ApiKeyName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class DownloadSummary
{
    public int Processed { get; set; }
    public int Created { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public bool WasCancelled { get; set; }
}

public enum ProcessingResult
{
    Created,
    Skipped,
    Error
}
