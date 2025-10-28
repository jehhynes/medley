using Medley.CollectorUtil.Data;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Medley.CollectorUtil.Services;

public class TranscriptDownloadService
{
    private readonly MeetingTranscriptService _transcriptService;
    private readonly FellowApiService _fellowApiService;

    public TranscriptDownloadService(
        MeetingTranscriptService transcriptService,
        FellowApiService fellowApiService)
    {
        _transcriptService = transcriptService;
        _fellowApiService = fellowApiService;
    }

    public async Task<DownloadSummary> DownloadTranscriptsForApiKeyAsync(
        ApiKey apiKey,
        IProgress<DownloadProgress>? progress = null)
    {
        var summary = new DownloadSummary();
        string? cursor = null;
        var pageNumber = 0;

        do
        {
            pageNumber++;
            progress?.Report(new DownloadProgress
            {
                ApiKeyName = apiKey.Name,
                Message = $"Fetching page {pageNumber} for '{apiKey.Name}'..."
            });

            try
            {
                var response = await _fellowApiService.ListRecordingsAsync(apiKey.Key, cursor);

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

                        // Skip very recent meetings (within 2 hours)
                        if (recording.StartedAt != null && recording.StartedAt.Value > DateTimeOffset.Now.AddHours(-2))
                        {
                            progress?.Report(new DownloadProgress
                            {
                                ApiKeyName = apiKey.Name,
                                Message = $"Skipping recent meeting: {recording.Title}"
                            });
                            summary.Skipped++;
                            continue;
                        }

                        // Check if already exists for this API key
                        var alreadyExists = await _transcriptService.TranscriptExistsForApiKeyAsync(
                            recording.Id, apiKey.Id);

                        if (alreadyExists)
                        {
                            progress?.Report(new DownloadProgress
                            {
                                ApiKeyName = apiKey.Name,
                                Message = $"Already exists for this API key: {recording.Title}"
                            });
                            summary.Skipped++;
                            continue;
                        }

                        var transcript = CreateTranscriptFromRecording(recording);
                        await _transcriptService.SaveTranscriptAsync(transcript, apiKey);
                        summary.Created++;

                        progress?.Report(new DownloadProgress
                        {
                            ApiKeyName = apiKey.Name,
                            Message = $"Saved: {recording.Title}"
                        });
                    }
                    catch (Exception ex)
                    {
                        progress?.Report(new DownloadProgress
                        {
                            ApiKeyName = apiKey.Name,
                            Message = $"Error processing recording: {ex.Message}"
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
                    Message = $"Error fetching page {pageNumber}: {ex.Message}"
                });
                summary.Errors++;
                break;
            }

        } while (!string.IsNullOrWhiteSpace(cursor));

        progress?.Report(new DownloadProgress
        {
            ApiKeyName = apiKey.Name,
            Message = $"Completed '{apiKey.Name}': Processed={summary.Processed}, Created={summary.Created}, Skipped={summary.Skipped}, Errors={summary.Errors}"
        });

        return summary;
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
            Date = recording.StartedAt?.DateTime,
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
}
