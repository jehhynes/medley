using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Medley.Application.Integrations.Interfaces;
using Medley.Application.Integrations.Models.Fellow;
using Medley.Application.Integrations.Services;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job service for syncing Fellow.ai transcripts
/// </summary>
public class FellowTranscriptSyncJob : BaseHangfireJob<FellowTranscriptSyncJob>
{
    private readonly IIntegrationService _integrationService;
    private readonly IRepository<Source> _sourceRepository;
    private readonly FellowIntegrationService _fellowService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public FellowTranscriptSyncJob(
        IIntegrationService integrationService,
        IRepository<Source> sourceRepository,
        FellowIntegrationService fellowService,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<FellowTranscriptSyncJob> logger) : base(unitOfWork, logger)
    {
        _integrationService = integrationService;
        _sourceRepository = sourceRepository;
        _fellowService = fellowService;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Syncs Fellow.ai transcripts for all connected integrations
    /// </summary>
    public async Task SyncTranscriptsAsync(PerformContext context, CancellationToken cancellationToken)
    {
        LogInfo(context, "Starting Fellow transcript sync");

        var fellowIntegrations = _integrationService.Query()
            .Where(i => i.Type == IntegrationType.Fellow && i.Status == ConnectionStatus.Connected)
            .ToList();

        if (fellowIntegrations.Count == 0)
        {
            LogInfo(context, "No connected Fellow integrations found. Skipping transcript sync.");
            return;
        }

        LogInfo(context, $"Found {fellowIntegrations.Count} connected Fellow integration(s)");

        foreach (var integration in fellowIntegrations)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogInfo(context, "Cancellation requested. Stopping transcript sync.");
                break;
            }

            try
            {
                await SyncIntegrationTranscriptsAsync(context, integration, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError(context, ex, $"Failed to sync transcripts for integration {integration.Id} ({integration.Name})");
                // Continue with other integrations
            }
        }

        LogInfo(context, "Fellow transcript sync completed");
    }

    /// <summary>
    /// Syncs transcripts for a specific integration
    /// </summary>
    private async Task SyncIntegrationTranscriptsAsync(PerformContext context, Integration integration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(integration.ApiKey) || string.IsNullOrWhiteSpace(integration.BaseUrl))
        {
            LogWarning(context, $"Integration {integration.Id} is missing ApiKey or BaseUrl. Skipping.");
            return;
        }

        LogInfo(context, $"Syncing transcripts for integration {integration.Id} ({integration.Name})");

        var options = new FellowRecordingsRequestOptions
        {
            Include = new FellowIncludeOptions { Transcript = true },
            Pagination = new FellowPaginationOptions { PageSize = 20 }
        };

        // Determine sync mode: initial or incremental
        if (!integration.InitialSyncCompleted)
        {
            LogInfo(context, $"Performing initial sync (no date filter) for integration {integration.Id}");
            // No date filter for initial sync
        }
        else
        {
            // Incremental sync: fetch recordings created since last known source
            var lastSourceDate = await _sourceRepository.Query()
                .Where(s => s.Integration.Id == integration.Id)
                .OrderByDescending(s => s.Date)
                .Select(s => (DateTimeOffset?)s.Date)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastSourceDate.HasValue)
            {
                options.Filters = new FellowRecordingFilters
                {
                    CreatedAtStart = lastSourceDate
                };
                LogInfo(context, $"Performing incremental sync from {lastSourceDate.Value} for integration {integration.Id}");
            }
            else
            {
                LogInfo(context, $"No existing sources found. Performing full sync for integration {integration.Id}");
            }
        }

        var totalProcessedCount = 0;
        var totalCreatedCount = 0;
        var totalSkippedCount = 0;
        string? cursor = null;
        var pageNumber = 0;

        do
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogInfo(context, "Cancellation requested. Stopping page processing.");
                break;
            }

            pageNumber++;
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                options.Pagination.Cursor = cursor;
            }

            var response = await _fellowService.ListRecordingsAsync(
                integration.ApiKey,
                integration.BaseUrl,
                options,
                cancellationToken);

            if (response?.Recordings?.Data == null || response.Recordings.Data.Count == 0)
            {
                LogDebug($"No recordings returned for integration {integration.Id}");
                break;
            }

            // Process this page in a transaction
            (int processedCount, int createdCount, int skippedCount, List<Guid> createdSourceIds) = await ProcessPage(integration, response, context, cancellationToken);

            totalProcessedCount += processedCount;
            totalCreatedCount += createdCount;
            totalSkippedCount += skippedCount;

            // Trigger smart tag processing for each newly created source (outside transaction)
            var currentJobId = context.BackgroundJob.Id;
            foreach (var sourceId in createdSourceIds)
            {
                _backgroundJobClient.ContinueJobWith<SmartTagProcessorJob>(
                    currentJobId,
                    j => j.ExecuteAsync(default!, default, sourceId));
                LogDebug($"Enqueued smart tag processing job for source {sourceId}");

                _backgroundJobClient.ContinueJobWith<SpeakerExtractionJob>(
                    currentJobId,
                    j => j.ExecuteAsync(default!, default, sourceId));
                LogDebug($"Enqueued speaker extraction job for source {sourceId}");
            }

            LogInfo(context, $"Page {pageNumber} completed for integration {integration.Id}. Processed: {processedCount}, Created: {createdCount}, Skipped: {skippedCount}");

            cursor = response.Recordings.PageInfo?.Cursor;

        } while (!string.IsNullOrWhiteSpace(cursor));

        // Mark initial sync as completed if this was the first run
        if (!integration.InitialSyncCompleted)
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                integration.InitialSyncCompleted = true;
                
                LogInfo(context, $"Initial sync completed for integration {integration.Id}. Flag set to true.");
            });
        }

        LogInfo(context, $"Sync completed for integration {integration.Id} ({integration.Name}). Total - Processed: {totalProcessedCount}, Created: {totalCreatedCount}, Skipped: {totalSkippedCount}");
    }

    private async Task<(int processedCount, int createdCount, int skippedCount, List<Guid> createdSourceIds)> ProcessPage(Integration integration, FellowRecordingsResponse response, PerformContext context, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var pageProcessed = 0;
            var pageCreated = 0;
            var pageSkipped = 0;
            var createdSourceIds = new List<Guid>();

            foreach (var recording in response.Recordings.Data)
            {
                pageProcessed++;

                if (string.IsNullOrWhiteSpace(recording.Id))
                {
                    LogWarning(context, "Recording has no ID. Skipping.");
                    pageSkipped++;
                    continue;
                }

                if (recording.StartedAt != null && recording.StartedAt.Value > DateTimeOffset.Now.AddHours(-2))
                {
                    LogWarning(context, "Recording has not started yet or was started within the past 2 hours. Skipping.");
                    pageSkipped++;
                    continue;
                }

                // Check if source already exists
                var existingSource = await _sourceRepository.Query()
                    .FirstOrDefaultAsync(s => s.ExternalId == recording.Id, cancellationToken);

                if (existingSource != null)
                {
                    LogDebug($"Source already exists for recording {recording.Id}. Skipping.");
                    pageSkipped++;
                    continue;
                }

                // Consolidate transcript by speaker
                string consolidatedTranscript = string.Empty;
                if (recording.Transcript?.SpeechSegments != null && recording.Transcript.SpeechSegments.Count > 0)
                {
                    consolidatedTranscript = ConsolidateTranscriptBySpeaker(recording.Transcript.SpeechSegments);
                }

                if (recording.Transcript != null)
                    recording.Transcript.SpeechSegments = null;

                string metadataJson = JsonSerializer.Serialize(recording);

                // Create new source
                var source = new Source
                {
                    Type = SourceType.Meeting,
                    MetadataType = SourceMetadataType.Collector_Fellow,
                    Name = recording.Title,
                    ExternalId = recording.Id,
                    Content = consolidatedTranscript,
                    MetadataJson = metadataJson,
                    Date = recording.StartedAt ?? DateTimeOffset.UtcNow,
                    Integration = integration
                };

                await _sourceRepository.AddAsync(source);
                pageCreated++;
                createdSourceIds.Add(source.Id);

                LogDebug($"Created source for recording {recording.Id} ({recording.Title})");
            }

            return (pageProcessed, pageCreated, pageSkipped, createdSourceIds);
        });
    }

    /// <summary>
    /// Consolidates transcript segments by speaker, combining consecutive segments from the same speaker
    /// </summary>
    private static string ConsolidateTranscriptBySpeaker(List<FellowSpeechSegment> segments)
    {
        if (segments == null || segments.Count == 0)
            return string.Empty;

        var result = new System.Text.StringBuilder();
        string? currentSpeaker = null;
        var currentTexts = new List<string>();

        foreach (var segment in segments)
        {
            if (segment.Speaker != currentSpeaker)
            {
                // Write out the previous speaker's consolidated text
                if (currentSpeaker != null && currentTexts.Count > 0)
                {
                    result.AppendLine($"{currentSpeaker}: {string.Join(" ", currentTexts)}");
                }

                // Start new speaker
                currentSpeaker = segment.Speaker;
                currentTexts.Clear();
            }

            // Add text to current speaker's segments
            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                currentTexts.Add(segment.Text.Trim());
            }
        }

        // Write out the last speaker's text
        if (currentSpeaker != null && currentTexts.Count > 0)
        {
            result.AppendLine($"{currentSpeaker}: {string.Join(" ", currentTexts)}");
        }

        return result.ToString().TrimEnd();
    }
}
