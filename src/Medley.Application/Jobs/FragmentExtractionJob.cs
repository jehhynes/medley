using Hangfire;
using Hangfire.MissionControl;
using Hangfire.Server;
using Hangfire.Storage;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for extracting fragments from source content using AI
/// Thin wrapper around FragmentExtractionService for Hangfire execution
/// </summary>
[MissionLauncher]
public class FragmentExtractionJob : BaseHangfireJob<FragmentExtractionJob>
{
    private readonly FragmentExtractionService _fragmentExtractionService;
    private readonly INotificationService _notificationService;
    private readonly IRepository<Source> _sourceRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public FragmentExtractionJob(
        FragmentExtractionService fragmentExtractionService,
        INotificationService notificationService,
        IRepository<Source> sourceRepository,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<FragmentExtractionJob> logger) : base(unitOfWork, logger)
    {
        _fragmentExtractionService = fragmentExtractionService;
        _notificationService = notificationService;
        _sourceRepository = sourceRepository;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Processes all sources with ExtractionStatus.NotStarted
    /// </summary>
    [Mission]
    public async Task ProcessAllPendingSourcesAsync(PerformContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ProcessAllPendingSourcesAsync - processing all sources with NotStarted status");

        int processedCount = 0;
        int successCount = 0;
        int failedCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Find next source with NotStarted status
            var nextSourceId = await _sourceRepository.Query()
                .Where(s => s.ExtractionStatus == ExtractionStatus.NotStarted)
                .OrderByDescending(s => s.Date)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextSourceId == Guid.Empty)
            {
                _logger.LogInformation("No more sources with NotStarted status. Processed {ProcessedCount} sources ({SuccessCount} succeeded, {FailedCount} failed)",
                    processedCount, successCount, failedCount);
                break;
            }

            processedCount++;
            _logger.LogInformation("Processing source {SourceId} ({ProcessedCount} of pending sources)", nextSourceId, processedCount);

            try
            {
                // Process this source using the existing logic
                await ExecuteAsync(nextSourceId, context, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process source {SourceId} during batch processing", nextSourceId);
                failedCount++;
                // Continue with next source
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("ProcessAllPendingSourcesAsync was cancelled after processing {ProcessedCount} sources ({SuccessCount} succeeded, {FailedCount} failed)",
                processedCount, successCount, failedCount);
        }
    }

    /// <summary>
    /// Executes fragment extraction for a specific source
    /// </summary>
    /// <param name="sourceId">The source ID to extract fragments from</param>
    [Mission]
    public async Task ExecuteAsync(Guid sourceId, PerformContext context, CancellationToken cancellationToken)
    {
        int fragmentCount = 0;
        bool success = false;
        string? errorMessage = null;

        // Set status to InProgress before starting extraction (separate transaction for immediate visibility)
        await SetExtractionStatusAsync(sourceId, ExtractionStatus.InProgress, cancellationToken);

        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                _logger.LogInformation("Fragment extraction job started for source {SourceId}", sourceId);

                var result = await _fragmentExtractionService.ExtractFragmentsAsync(sourceId, cancellationToken);
                fragmentCount = result.FragmentCount;

                // Set status to Completed within the same transaction as fragment creation
                // Zero fragments is a successful outcome, not a failure
                await SetExtractionStatusInTransactionAsync(sourceId, ExtractionStatus.Completed, cancellationToken);

                _logger.LogInformation("Fragment extraction job completed for source {SourceId}. Extracted {Count} fragments.", 
                    sourceId, fragmentCount);
                
                success = true;
            });

            // Enqueue confidence scoring job after successful extraction (outside transaction)
            if (success && fragmentCount > 0)
            {
                var currentJobId = context.BackgroundJob.Id;
                    
                var scoringJobId = _backgroundJobClient.ContinueJobWith<FragmentConfidenceScoringJob>(
                    currentJobId,
                    j => j.ExecuteAsync(sourceId, default!, default));
                _logger.LogInformation("Enqueued confidence scoring job for source {SourceId}", sourceId);

                // Enqueue embedding generation job after confidence scoring completes
                _backgroundJobClient.ContinueJobWith<EmbeddingGenerationJob>(
                    scoringJobId,
                    j => j.GenerateFragmentEmbeddings(default!, default, sourceId, null));
                _logger.LogInformation("Enqueued embedding generation job for source {SourceId}", sourceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fragment extraction job failed for source {SourceId}", sourceId);
            errorMessage = ex.Message;
            success = false;

            // Set status to Failed (separate transaction since main transaction was rolled back)
            await SetExtractionStatusAsync(sourceId, ExtractionStatus.Failed, cancellationToken);
        }
        finally
        {
            // Send SignalR notification regardless of success/failure
            var message = success 
                ? $"Successfully extracted {fragmentCount} fragment{(fragmentCount != 1 ? "s" : "")}" 
                : $"Fragment extraction failed: {errorMessage}";
            
            await _notificationService.SendFragmentExtractionCompleteAsync(sourceId, fragmentCount, success, message);
        }
    }

    /// <summary>
    /// Updates the extraction status for a source (uses separate transaction)
    /// </summary>
    private async Task SetExtractionStatusAsync(Guid sourceId, ExtractionStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                await SetExtractionStatusInTransactionAsync(sourceId, status, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update extraction status for source {SourceId}", sourceId);
            // Don't throw - status update failure shouldn't break the job
        }
    }

    /// <summary>
    /// Updates the extraction status for a source within the current transaction
    /// </summary>
    private async Task SetExtractionStatusInTransactionAsync(Guid sourceId, ExtractionStatus status, CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.Query()
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source != null)
        {
            source.ExtractionStatus = status;
            
            _logger.LogDebug("Updated extraction status for source {SourceId} to {Status}", sourceId, status);
        }
        else
        {
            _logger.LogWarning("Source {SourceId} not found when updating extraction status", sourceId);
        }
    }
}

