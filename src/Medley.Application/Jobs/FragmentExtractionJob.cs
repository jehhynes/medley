using Hangfire;
using Hangfire.Console;
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
    /// Runs for a maximum of 10 minutes, then requeues itself if more work remains
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    [Mission]
    public async Task ProcessAllPendingSourcesAsync(PerformContext context, CancellationToken cancellationToken)
    {
        LogInfo(context, "Starting ProcessAllPendingSourcesAsync - processing all sources with NotStarted status");

        var startTime = DateTime.UtcNow;
        var maxRuntime = TimeSpan.FromMinutes(10);
        int processedCount = 0;
        Guid? currentSourceId = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check if we've exceeded the maximum runtime
                var elapsed = DateTime.UtcNow - startTime;
                if (elapsed >= maxRuntime)
                {
                    LogInfo(context, $"Maximum runtime of {maxRuntime.TotalMinutes} minutes reached after processing {processedCount} sources");
                    
                    // Check if there are more sources to process
                    var hasMoreSources = await _sourceRepository.Query()
                        .AnyAsync(s => s.ExtractionStatus == ExtractionStatus.NotStarted, cancellationToken);
                    
                    if (hasMoreSources)
                    {
                        LogInfo(context, "More sources remain - requeuing ProcessAllPendingSourcesAsync to continue");
                        _backgroundJobClient.Enqueue<FragmentExtractionJob>(
                            j => j.ProcessAllPendingSourcesAsync(default!, default));
                    }
                    
                    break;
                }

                // Find next source with NotStarted status
                var nextSourceId = await _sourceRepository.Query()
                    .Where(s => s.ExtractionStatus == ExtractionStatus.NotStarted)
                    .OrderByDescending(s => s.Date)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (nextSourceId == Guid.Empty)
                {
                    LogInfo(context, $"No more sources with NotStarted status. Successfully processed {processedCount} sources");
                    break;
                }

                currentSourceId = nextSourceId;
                processedCount++;
                LogInfo(context, $"Processing source {nextSourceId} ({processedCount} sources processed, {elapsed.TotalMinutes:F1} minutes elapsed)");

                // Process this source using the existing logic - let exceptions propagate to halt the batch
                await ExecuteAsync(nextSourceId, context, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                LogWarning(context, $"ProcessAllPendingSourcesAsync was cancelled after processing {processedCount} sources");
            }
        }
        catch (Exception ex)
        {
            // If we were processing a source when the error occurred, ensure its status is set to Failed
            if (currentSourceId.HasValue)
            {
                LogError(context, ex, $"Failed to process source {currentSourceId.Value} during batch processing");
                await SetExtractionStatusAsync(currentSourceId.Value, ExtractionStatus.Failed, cancellationToken);
            }
            
            // Re-throw to fail the batch job
            throw;
        }
    }

    /// <summary>
    /// Executes fragment extraction for a specific source
    /// </summary>
    /// <param name="sourceId">The source ID to extract fragments from</param>
    [AutomaticRetry(Attempts = 0)]
    [Mission]
    public async Task ExecuteAsync(Guid sourceId, PerformContext context, CancellationToken cancellationToken)
    {
        int fragmentCount = 0;
        bool success = false;

        // Set status to InProgress before starting extraction (separate transaction for immediate visibility)
        await SetExtractionStatusAsync(sourceId, ExtractionStatus.InProgress, cancellationToken);

        // Send InProgress notification
        await _notificationService.SendFragmentExtractionStartedAsync(sourceId);

        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                LogInfo(context, $"Fragment extraction job started for source {sourceId}");

                var result = await _fragmentExtractionService.ExtractFragmentsAsync(sourceId, cancellationToken);
                fragmentCount = result.FragmentCount;

                LogSuccess(context, $"Extracted {fragmentCount} fragments from source {sourceId}");

                // Set status to Completed within the same transaction as fragment creation
                // Zero fragments is a successful outcome, not a failure
                await SetExtractionStatusInTransactionAsync(sourceId, ExtractionStatus.Completed, cancellationToken);

                LogInfo(context, $"Fragment extraction job completed for source {sourceId}. Extracted {fragmentCount} fragments.");
                
                success = true;
            });

            // Enqueue confidence scoring job after successful extraction (outside transaction)
            if (success && fragmentCount > 0)
            {
                var currentJobId = context.BackgroundJob.Id;
                    
                var scoringJobId = _backgroundJobClient.Schedule<FragmentConfidenceScoringJob>(
                    j => j.ExecuteAsync(sourceId, default!, default),
                    TimeSpan.FromSeconds(10));
                LogInfo(context, $"Enqueued confidence scoring job for source {sourceId}");

                // Enqueue embedding generation job after confidence scoring completes
                _backgroundJobClient.Schedule<EmbeddingGenerationJob>(
                    j => j.GenerateFragmentEmbeddings(default!, default, sourceId, null),
                    TimeSpan.FromSeconds(10));
                LogInfo(context, $"Enqueued embedding generation job for source {sourceId}");
            }

            // Send success notification
            var successMessage = $"Successfully extracted {fragmentCount} fragment{(fragmentCount != 1 ? "s" : "")}";
            await _notificationService.SendFragmentExtractionCompleteAsync(sourceId, fragmentCount, success: true, successMessage);
        }
        catch (Exception ex)
        {
            // Get detailed error message including inner exceptions
            var detailedError = GetDetailedErrorMessage(ex);
            LogError(context, ex, $"Fragment extraction job failed for source {sourceId}: {detailedError}");
            
            // Set status to Failed (separate transaction since main transaction was rolled back)
            await SetExtractionStatusAsync(sourceId, ExtractionStatus.Failed, cancellationToken);

            // Send failure notification
            var failureMessage = $"Fragment extraction failed: {detailedError}";
            await _notificationService.SendFragmentExtractionCompleteAsync(sourceId, fragmentCount: 0, success: false, failureMessage);

            // Re-throw to fail the job
            throw;
        }
    }

    /// <summary>
    /// Gets detailed error message including all inner exceptions
    /// </summary>
    private string GetDetailedErrorMessage(Exception ex)
    {
        var messages = new List<string>();
        var currentException = ex;
        
        while (currentException != null)
        {
            messages.Add(currentException.Message);
            currentException = currentException.InnerException;
        }
        
        return string.Join(" --> ", messages);
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
            LogDebug($"Failed to update extraction status for source {sourceId}: {ex.Message}");
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
            
            LogDebug($"Updated extraction status for source {sourceId} to {status}");
        }
        else
        {
            LogDebug($"Source {sourceId} not found when updating extraction status");
        }
    }
}

