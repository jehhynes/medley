using Hangfire;
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
        _logger.LogInformation("Starting Fellow transcript sync");

        var fellowIntegrations = _integrationService.Query()
            .Where(i => i.Type == IntegrationType.Fellow && i.Status == ConnectionStatus.Connected)
            .ToList();

        if (fellowIntegrations.Count == 0)
        {
            _logger.LogInformation("No connected Fellow integrations found. Skipping transcript sync.");
            return;
        }

        _logger.LogInformation("Found {Count} connected Fellow integration(s)", fellowIntegrations.Count);

        foreach (var integration in fellowIntegrations)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested. Stopping transcript sync.");
                break;
            }

            try
            {
                await SyncIntegrationTranscriptsAsync(context, integration, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync transcripts for integration {IntegrationId} ({DisplayName})",
                    integration.Id, integration.DisplayName);
                // Continue with other integrations
            }
        }

        _logger.LogInformation("Fellow transcript sync completed");
    }

    /// <summary>
    /// Syncs transcripts for a specific integration
    /// </summary>
    private async Task SyncIntegrationTranscriptsAsync(PerformContext context, Integration integration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(integration.ApiKey) || string.IsNullOrWhiteSpace(integration.BaseUrl))
        {
            _logger.LogWarning("Integration {IntegrationId} is missing ApiKey or BaseUrl. Skipping.",
                integration.Id);
            return;
        }

        _logger.LogInformation("Syncing transcripts for integration {IntegrationId} ({DisplayName})",
            integration.Id, integration.DisplayName);

        var options = new FellowRecordingsRequestOptions
        {
            Include = new FellowIncludeOptions { Transcript = true },
            Pagination = new FellowPaginationOptions { PageSize = 20 }
        };

        // Determine sync mode: initial or incremental
        if (!integration.InitialSyncCompleted)
        {
            _logger.LogInformation("Performing initial sync (no date filter) for integration {IntegrationId}",
                integration.Id);
            // No date filter for initial sync
        }
        else
        {
            // Incremental sync: fetch recordings created since last known source
            var lastSourceDate = await _sourceRepository.Query()
                .Where(s => s.Integration.Id == integration.Id && s.Date.HasValue)
                .OrderByDescending(s => s.Date)
                .Select(s => s.Date)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastSourceDate.HasValue)
            {
                options.Filters = new FellowRecordingFilters
                {
                    CreatedAtStart = lastSourceDate
                };
                _logger.LogInformation("Performing incremental sync from {StartDate} for integration {IntegrationId}",
                    lastSourceDate.Value, integration.Id);
            }
            else
            {
                _logger.LogInformation("No existing sources found. Performing full sync for integration {IntegrationId}",
                    integration.Id);
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
                _logger.LogInformation("Cancellation requested. Stopping page processing.");
                break;
            }

            pageNumber++;
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                options.Pagination!.Cursor = cursor;
            }

            var response = await _fellowService.ListRecordingsAsync(
                integration.ApiKey,
                integration.BaseUrl,
                options,
                cancellationToken);

            if (response?.Recordings?.Data == null || response.Recordings.Data.Count == 0)
            {
                _logger.LogDebug("No recordings returned for integration {IntegrationId}", integration.Id);
                break;
            }

            // Process this page in a transaction
            (int processedCount, int createdCount, int skippedCount, List<Guid> createdSourceIds) = await ProcessPage(integration, response, cancellationToken);

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
                _logger.LogDebug("Enqueued smart tag processing job for source {SourceId}", sourceId);
            }

            _logger.LogInformation(
                "Page {PageNumber} completed for integration {IntegrationId}. Processed: {Processed}, Created: {Created}, Skipped: {Skipped}",
                pageNumber, integration.Id, processedCount, createdCount, skippedCount);

            cursor = response.Recordings.PageInfo?.Cursor;

        } while (!string.IsNullOrWhiteSpace(cursor));

        // Mark initial sync as completed if this was the first run
        if (!integration.InitialSyncCompleted)
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                integration.InitialSyncCompleted = true;
                await _integrationService.SaveAsync(integration);
                _logger.LogInformation("Initial sync completed for integration {IntegrationId}. Flag set to true.",
                    integration.Id);
            });
        }

        _logger.LogInformation(
            "Sync completed for integration {IntegrationId} ({DisplayName}). Total - Processed: {Processed}, Created: {Created}, Skipped: {Skipped}",
            integration.Id, integration.DisplayName, totalProcessedCount, totalCreatedCount, totalSkippedCount);
    }

    private async Task<(int processedCount, int createdCount, int skippedCount, List<Guid> createdSourceIds)> ProcessPage(Integration integration, FellowRecordingsResponse response, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var pageProcessed = 0;
            var pageCreated = 0;
            var pageSkipped = 0;
            var createdSourceIds = new List<Guid>();

            foreach (var recording in response.Recordings!.Data!)
            {
                pageProcessed++;

                if (string.IsNullOrWhiteSpace(recording.Id))
                {
                    _logger.LogWarning("Recording has no ID. Skipping.");
                    pageSkipped++;
                    continue;
                }

                if (recording.StartedAt != null && recording.StartedAt.Value > DateTimeOffset.Now.AddHours(-2))
                {
                    _logger.LogWarning("Recording has not started yet or was started within the past 2 hours. Skipping.");
                    pageSkipped++;
                    continue;
                }

                // Check if source already exists
                var existingSource = await _sourceRepository.Query()
                    .FirstOrDefaultAsync(s => s.ExternalId == recording.Id, cancellationToken);

                if (existingSource != null)
                {
                    _logger.LogDebug("Source already exists for recording {RecordingId}. Skipping.", recording.Id);
                    pageSkipped++;
                    continue;
                }

                // Consolidate transcript by speaker
                string? consolidatedTranscript = null;
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
                    Name = recording.Title,
                    ExternalId = recording.Id,
                    Content = consolidatedTranscript,
                    MetadataJson = metadataJson,
                    Date = recording.StartedAt,
                    Integration = integration
                };

                await _sourceRepository.AddAsync(source);
                pageCreated++;
                createdSourceIds.Add(source.Id);

                _logger.LogDebug("Created source for recording {RecordingId} ({Title})",
                    recording.Id, recording.Title);
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
