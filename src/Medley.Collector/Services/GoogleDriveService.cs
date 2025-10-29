using Medley.Collector.Data;

namespace Medley.Collector.Services;

/// <summary>
/// Coordinator service that combines Google Drive API and Playwright to download transcripts
/// </summary>
public class GoogleDriveService
{
    private readonly GoogleDriveApiService _driveApiService;
    private readonly GoogleDriveDownloadService _transcriptDownloader;
    private readonly MeetingTranscriptService _transcriptService;

    public GoogleDriveService(
        GoogleDriveApiService driveApiService,
        GoogleDriveDownloadService transcriptDownloader,
        MeetingTranscriptService transcriptService)
    {
        _driveApiService = driveApiService;
        _transcriptDownloader = transcriptDownloader;
        _transcriptService = transcriptService;
    }

    /// <summary>
    /// Downloads transcripts from Google Drive
    /// </summary>
    public async Task<DownloadSummary> DownloadTranscriptsAsync(
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        var summary = new DownloadSummary();

        progress.Report(new DownloadProgress { Message = "Fetching Google Meet videos from Drive..." });

        // Step 1: Use Google Drive API to list videos
        var videos = await _driveApiService.ListGoogleMeetVideosAsync(cancellationToken);
        progress.Report(new DownloadProgress { Message = $"Found {videos.Count} Google Meet video(s)" });

        if (videos.Count == 0)
        {
            return summary;
        }

        // Step 2: Use an HttpClient to download transcripts for each video using saved browser cookies for authentication
        foreach (var video in videos)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            summary.Processed++;

            try
            {
                progress.Report(new DownloadProgress { Message = $"Processing: {video.Name}" });

                // Check if transcript already exists
                var existingTranscript = await _transcriptService.GetTranscriptByExternalIdAsync(video.Id, TranscriptSource.Google);
                if (existingTranscript != null)
                {
                    progress.Report(new DownloadProgress { Message = $"  Skipped (already exists): {video.Name}" });
                    summary.Skipped++;
                    continue;
                }

                // Download transcript using Playwright
                var transcript = await _transcriptDownloader.DownloadTranscriptAsync(video, cancellationToken);

                if (transcript != null)
                {
                    // Save to database
                    await _transcriptService.CreateTranscriptAsync(transcript);
                    progress.Report(new DownloadProgress { Message = $"  Downloaded: {video.Name}" });
                    summary.Created++;
                }
                else
                {
                    progress.Report(new DownloadProgress { Message = $"  No transcript available: {video.Name}" });
                    summary.Skipped++;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Authentication failed") || ex.Message.Contains("Browser authentication required"))
            {
                // Authentication errors should stop all processing
                progress.Report(new DownloadProgress { Message = $"  Authentication Error: {ex.Message}" });
                throw;
            }
            catch (Exception ex)
            {
                progress.Report(new DownloadProgress { Message = $"  Error: {video.Name} - {ex.Message}" });
                summary.Errors++;
            }
        }

        return summary;
    }
}
